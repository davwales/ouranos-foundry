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

    // Defaults when no ChunkConfig is assigned.
    private ChunkConfig ResolvedChunkConfig => Config?.ChunkConfig ?? new();

    public override void _Ready()
    {
        if (MaxConcurrentChunkJobs < 1)
        {
            GD.PushError(
                $"WorldGenerator: MaxConcurrentChunkJobs must be at least 1 (got {MaxConcurrentChunkJobs}). Using 1."
            );
        }

        _chunkJobSlots = new SemaphoreSlim(Math.Max(1, MaxConcurrentChunkJobs));
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

    public override void _ExitTree()
    {
        // Cancelled jobs are no longer current, so their continuations never touch this node.
        foreach (var job in _inFlightChunks.Values)
        {
            job.Cancel();
        }

        _inFlightChunks.Clear();

        if (ChunkManager is not null)
        {
            ChunkManager.RequestChunkGeneration -= OnChunkGenerationRequested;
            ChunkManager.RequestChunkUnload -= OnChunkUnloadRequested;
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

        if (Config is null)
        {
            return FailBlueprint(
                nameof(WorldGenerator),
                "Config is not set. Assign a ProceduralGenerationConfig in the inspector."
            );
        }

        Log("Generating world blueprint...", LogLevel.Basic);

        ValidateConfig(Config);

        WorldState.ChunkSizeTiles = ResolvedChunkConfig.ChunkSizeTiles;

        if (BlueprintPipeline?.RunPasses(Config, WorldState, LogLevel) is { } failure)
        {
            WorldState.Clear();
            return FailBlueprint(failure.PassName, failure.Error.Message, failure.Error);
        }

        WorldState.IsBlueprintGenerated = true;
        ChunkManager?.ResetTracking();

        Log("World blueprint complete.", LogLevel.Basic);
        EmitSignal(SignalName.BlueprintGenerated);
        return true;
    }

    private bool FailBlueprint(string passName, string error, Exception? exception = null)
    {
        GD.PushError(
            $"WorldGenerator: blueprint pass {passName} failed: {exception?.ToString() ?? error}"
        );
        EmitSignal(SignalName.BlueprintFailed, passName, error);
        return false;
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
            Config is null
            || !WorldState.IsBlueprintGenerated
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
            var chunk = await GenerateChunkAsync(Config, chunkCoords, job.Token);
            if (IsCurrentJob(chunkCoords, job))
            {
                _generatedChunks[chunkCoords] = chunk;
                Log(
                    $"Chunk {chunkCoords} generated. Type: {chunk.Type}, "
                        + $"Cities: {chunk.CityIds.Count}, Roads: {chunk.RoadIds.Count}",
                    LogLevel.Verbose
                );
                EmitSignal(SignalName.ChunkGenerated, chunkCoords, chunk);
            }
        }
        catch (OperationCanceledException) when (job.IsCancellationRequested)
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
        ProceduralGenerationConfig config,
        Vector2I chunkCoords,
        CancellationToken cancellationToken
    )
    {
        var chunk = PrepareChunkData(chunkCoords);
        var (pipeline, world, logLevel, jobSlots) = (
            GenerationPipeline,
            WorldState,
            LogLevel,
            _chunkJobSlots
        );

        await jobSlots.WaitAsync(cancellationToken);
        (string PassName, Exception Error)? failure;
        try
        {
            failure = await Task.Run(
                () => pipeline?.RunPasses(config, world, logLevel, chunk, cancellationToken),
                cancellationToken
            );
        }
        finally
        {
            jobSlots.Release();
        }

        // Wraps the original exception so the log line keeps its stack trace.
        return failure is { } f
            ? throw new InvalidOperationException($"{f.PassName}: {f.Error.Message}", f.Error)
            : chunk;
    }

    /// Must be called on the main thread since it reads from WorldState.
    private ChunkData PrepareChunkData(Vector2I chunkCoords)
    {
        var chunkSizeTiles = ResolvedChunkConfig.ChunkSizeTiles;
        var cityIds = WorldState.GetCitiesInChunk(chunkCoords);
        var roadIds = WorldState.GetRoadsInChunk(chunkCoords);

        var descriptor = new ChunkDescriptor(
            chunkCoords,
            DetermineChunkType(cityIds.Count > 0, roadIds.Count > 0),
            cityIds,
            roadIds,
            chunkSizeTiles
        );

        return new ChunkData
        {
            Descriptor = descriptor,
            TerrainData = new int[chunkSizeTiles * chunkSizeTiles],
        };
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

    private static void ValidateConfig(ProceduralGenerationConfig config)
    {
        if (config.BiomeDefinitions.Count == 0)
        {
            GD.PrintErr(
                "WorldGenerator: BiomeDefinitions is empty. Add biome definitions to the config."
            );
        }

        if (config.CitySizeDefinitions.Count == 0)
        {
            GD.PrintErr(
                "WorldGenerator: CitySizeDefinitions is empty. Add city size definitions to the config."
            );
        }

        if (config.PoiTypeDefinitions.Count == 0)
        {
            GD.PrintErr(
                "WorldGenerator: PoiTypeDefinitions is empty. Add POI type definitions to the config."
            );
        }

        if (config.RoadTypeDefinitions.Count == 0)
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
