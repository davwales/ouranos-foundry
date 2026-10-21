#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

/// <summary>
/// Entry point for world generation. Orchestrates blueprint generation (once,
/// at startup or load) and chunk generation (on demand as the player moves).
/// Blueprint data is immutable after generation and safe to read from worker threads.
/// Chunk computation runs off-thread; scene tree modifications are deferred to main.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class WorldGenerator : Node
{
    [Export]
    public ProceduralGenerationConfig? Config { get; private set; }

    [Export]
    public WorldState WorldState { get; private set; } = new();

    [Export]
    public ChunkData InitialChunkData { get; private set; } = new();

    [Export]
    public ChunkManager? ChunkManager { get; private set; }

    [Export]
    public BlueprintPipeline? BlueprintPipeline { get; private set; }

    [Export]
    public GenerationPipeline? GenerationPipeline { get; private set; }

    /// <summary>
    /// How many chunks may run their generation passes at once. Further requests wait.
    /// </summary>
    [Export]
    public int MaxConcurrentChunkJobs { get; private set; } = 4;

    [Export]
    public LogLevel LogLevel { get; private set; } = LogLevel.Basic;

    public bool IsBlueprintGenerated => WorldState.IsBlueprintGenerated;

    public bool IsChunkReady(Vector2I coords)
    {
        return _generatedChunks.ContainsKey(coords);
    }

    public ChunkData? GetChunk(Vector2I coords)
    {
        return _generatedChunks.GetValueOrDefault(coords);
    }

    [Signal]
    public delegate void BlueprintGeneratedEventHandler();

    [Signal]
    public delegate void BlueprintFailedEventHandler(string passName, string error);

    [Signal]
    public delegate void ChunkGeneratedEventHandler(Vector2I coords, ChunkData chunk);

    [Signal]
    public delegate void ChunkUnloadedEventHandler(Vector2I coords);

    [Signal]
    public delegate void GenerationFailedEventHandler(Vector2I coords, string error);

    private readonly Dictionary<Vector2I, ChunkData> _generatedChunks = [];
    private readonly Dictionary<Vector2I, CancellationTokenSource> _inFlightChunks = [];
    private SemaphoreSlim _chunkJobSlots = new(1);
    private List<GenerationPass> _cachedGenerationPasses = [];

    // Defaults when no ChunkConfig is assigned.
    private ChunkConfig ResolvedChunkConfig => Config?.ChunkConfig ?? new();

    public override void _Ready()
    {
        _chunkJobSlots = new SemaphoreSlim(Math.Max(1, MaxConcurrentChunkJobs));
        CacheGenerationPasses();
        GenerateBlueprint();

        if (ChunkManager is not null)
        {
            ChunkManager.RequestChunkGeneration += OnChunkGenerationRequested;
            ChunkManager.RequestChunkUnload += OnChunkUnloadRequested;

            var chunkConfig = ResolvedChunkConfig;
            ChunkManager.SetViewDistance(chunkConfig.ViewDistance);
            ChunkManager.SetChunkSize(chunkConfig.ChunkSizeTiles, chunkConfig.TilePixelSize);
        }
    }

    private void CacheGenerationPasses()
    {
        if (GenerationPipeline is not null)
        {
            _cachedGenerationPasses = GenerationPipeline.GetPasses().ToList();
        }
    }

    /// <summary>
    /// Runs the blueprint passes. On failure the partial blueprint is cleared,
    /// <c>BlueprintFailed</c> is emitted and false is returned.
    /// </summary>
    public bool GenerateBlueprint()
    {
        if (WorldState.IsBlueprintGenerated)
        {
            return true;
        }

        Log("Generating world blueprint...", LogLevel.Basic);

        ValidateConfig();

        WorldState.ChunkSizeTiles = ResolvedChunkConfig.ChunkSizeTiles;

        if (BlueprintPipeline is not null)
        {
            var failedPass = string.Empty;
            var failure = string.Empty;
            void OnPassFailed(string passName, string error)
            {
                failedPass = passName;
                failure = error;
            }

            BlueprintPipeline.PassFailed += OnPassFailed;
            var succeeded = BlueprintPipeline.RunPasses(WorldState);
            BlueprintPipeline.PassFailed -= OnPassFailed;

            if (!succeeded)
            {
                WorldState.Clear();
                GD.PushError($"WorldGenerator: blueprint pass {failedPass} failed: {failure}");
                EmitSignal(SignalName.BlueprintFailed, failedPass, failure);
                return false;
            }
        }

        WorldState.IsBlueprintGenerated = true;
        ChunkManager?.ResetTracking();

        Log("World blueprint complete.", LogLevel.Basic);
        EmitSignal(SignalName.BlueprintGenerated);
        return true;
    }

    /// <summary>
    /// Cancels in-flight chunk jobs, unloads every generated chunk (emitting
    /// <c>ChunkUnloaded</c>) and clears the blueprint so it can be generated again.
    /// </summary>
    public void ResetBlueprint()
    {
        foreach (var coords in _inFlightChunks.Keys.Concat(_generatedChunks.Keys).ToList())
        {
            UnloadChunk(coords);
        }

        WorldState.Clear();
    }

    /// <summary>
    /// Unloads a generated chunk, or cancels its generation if it is still in flight.
    /// </summary>
    public void UnloadChunk(Vector2I coords)
    {
        if (_inFlightChunks.Remove(coords, out var job))
        {
            job.Cancel();
        }

        if (_generatedChunks.Remove(coords))
        {
            Log($"Chunk {coords} unloaded.", LogLevel.Verbose);
            EmitSignal(SignalName.ChunkUnloaded, coords);
        }
    }

    private async void OnChunkGenerationRequested(Vector2I chunkCoords)
    {
        if (
            !WorldState.IsBlueprintGenerated
            || _generatedChunks.ContainsKey(chunkCoords)
            || _inFlightChunks.ContainsKey(chunkCoords)
        )
        {
            return;
        }

        var job = new CancellationTokenSource();
        _inFlightChunks[chunkCoords] = job;

        try
        {
            var chunk = await GenerateChunkAsync(chunkCoords, job.Token);
            if (IsCurrentJob(chunkCoords, job))
            {
                _generatedChunks[chunkCoords] = chunk;
                Log(
                    $"Chunk {chunkCoords} generated. Type: {chunk.Type}, "
                        + $"Cities: {chunk.OverlappingCityIds.Count}, Roads: {chunk.OverlappingRoadIds.Count}",
                    LogLevel.Verbose
                );
                EmitSignal(SignalName.ChunkGenerated, chunkCoords, chunk);
            }
        }
        catch (OperationCanceledException)
        {
            // Unloaded or reset while generating: nothing to report.
        }
        catch (Exception e)
        {
            if (IsCurrentJob(chunkCoords, job))
            {
                Log($"Chunk generation failed for {chunkCoords}: {e}", LogLevel.Basic);
                EmitSignal(SignalName.GenerationFailed, chunkCoords, e.Message);
            }
        }
        finally
        {
            if (IsCurrentJob(chunkCoords, job))
            {
                _inFlightChunks.Remove(chunkCoords);
            }

            job.Dispose();
        }
    }

    // False once the chunk was unloaded (or unloaded and re-requested) mid-generation.
    private bool IsCurrentJob(Vector2I chunkCoords, CancellationTokenSource job)
    {
        return _inFlightChunks.GetValueOrDefault(chunkCoords) == job
            && !job.IsCancellationRequested;
    }

    private async Task<ChunkData> GenerateChunkAsync(
        Vector2I chunkCoords,
        CancellationToken cancellationToken
    )
    {
        var chunk = PrepareChunkData(chunkCoords);
        var jobSlots = _chunkJobSlots;

        await jobSlots.WaitAsync(cancellationToken);
        try
        {
            await Task.Run(
                () =>
                {
                    foreach (var pass in _cachedGenerationPasses)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        pass.RunPass(WorldState, chunk);
                    }

                    chunk.IsGenerated = true;
                },
                cancellationToken
            );
        }
        finally
        {
            jobSlots.Release();
        }

        return chunk;
    }

    /// Must be called on the main thread since it reads from WorldState.
    private ChunkData PrepareChunkData(Vector2I chunkCoords)
    {
        var chunkSizeTiles = ResolvedChunkConfig.ChunkSizeTiles;
        var chunk = InitialChunkData.Copy();

        var cityIds = WorldState.GetCitiesInChunk(chunkCoords);
        var roadIds = WorldState.GetRoadsInChunk(chunkCoords);

        var descriptor = new ChunkDescriptor(
            chunkCoords,
            DetermineChunkType(cityIds.Count > 0, roadIds.Count > 0),
            cityIds,
            roadIds,
            chunkSizeTiles
        );

        chunk.Descriptor = descriptor;
        chunk.TerrainData = new int[chunkSizeTiles * chunkSizeTiles];

        return chunk;
    }

    private static ChunkType DetermineChunkType(bool hasCities, bool hasRoads)
    {
        return (hasCities, hasRoads) switch
        {
            (true, true) => ChunkType.Mixed,
            (true, false) => ChunkType.City,
            (false, true) => ChunkType.Road,
            _ => ChunkType.Wilderness,
        };
    }

    private void OnChunkUnloadRequested(Vector2I chunkCoords)
    {
        UnloadChunk(chunkCoords);
    }

    private void ValidateConfig()
    {
        if (Config is null)
        {
            GD.PrintErr("WorldGenerator: ProceduralGenerationConfig is not set!");
            return;
        }

        if (Config.BiomeDefinitions.Count == 0)
        {
            GD.PrintErr(
                "WorldGenerator: BiomeDefinitions is empty. Add biome definitions to the config."
            );
        }

        if (Config.CitySizeDefinitions.Count == 0)
        {
            GD.PrintErr(
                "WorldGenerator: CitySizeDefinitions is empty. Add city size definitions to the config."
            );
        }

        if (Config.PoiTypeDefinitions.Count == 0)
        {
            GD.PrintErr(
                "WorldGenerator: PoiTypeDefinitions is empty. Add POI type definitions to the config."
            );
        }

        if (Config.RoadTypeDefinitions.Count == 0)
        {
            GD.PrintErr(
                "WorldGenerator: RoadTypeDefinitions is empty. Add road type definitions to the config."
            );
        }
    }

    private void Log(string message, LogLevel level)
    {
        if (LogLevel >= level)
        {
            GD.Print(message);
        }
    }
}
