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
/// Chunk passes run off-thread on a plain <c>ChunkBuffer</c>; the result is wrapped in a
/// <c>ChunkData</c>, logged and emitted back on the main thread. A <c>WorldState</c> is
/// never changed once its blueprint is generated: a reset or failed run replaces it, so
/// each chunk job reads the world and <c>Definitions</c> it was dispatched with.
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

    /// <summary>
    /// The snapshot of <c>Config</c>'s definitions that the last blueprint run built and
    /// handed to every pass. Null until <c>GenerateBlueprint</c> runs with a config.
    /// </summary>
    public GenerationDefinitions? Definitions { get; private set; }

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

    private const string MissingConfigMessage =
        "Config is not set. Assign a ProceduralGenerationConfig in the inspector.";

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
    /// Validates the setup, then runs the blueprint passes. When <c>Validate()</c> reports
    /// anything, no pass runs and <c>BlueprintFailed("WorldGenerator", messages)</c> is
    /// emitted. When a pass fails, the partial blueprint is discarded and
    /// <c>BlueprintFailed</c> is emitted. Either way it returns false.
    /// </summary>
    public bool GenerateBlueprint()
    {
        if (WorldState.IsBlueprintGenerated)
        {
            return true;
        }

        var errors = Validate();
        if (Config is null || errors.Count > 0)
        {
            return FailBlueprint(nameof(WorldGenerator), string.Join("\n", errors));
        }

        Log("Generating world blueprint...", LogLevel.Basic);

        Definitions = new GenerationDefinitions(Config);
        foreach (var warning in Config.GetWarnings(Definitions))
        {
            GD.PushWarning($"WorldGenerator: {warning}");
        }

        WorldState.ChunkSizeTiles = ResolvedChunkConfig.ChunkSizeTiles;

        if (BlueprintPipeline?.RunPasses(Config, Definitions, WorldState, LogLevel) is { } failure)
        {
            ReplaceWorldState();
            return FailBlueprint(failure.PassName, failure.Error.Message, failure.Error);
        }

        WorldState.IsBlueprintGenerated = true;
        ChunkManager?.ResetTracking();

        Log("World blueprint complete.", LogLevel.Basic);
        EmitSignal(SignalName.BlueprintGenerated);
        return true;
    }

    /// <summary>
    /// Returns a message for each problem that stops <c>GenerateBlueprint</c>: a missing
    /// <c>Config</c>, or what <c>Config.Validate()</c> and <c>WorldState.Validate()</c>
    /// report. Empty when generation can run. Override to add checks of your own.
    /// </summary>
    public virtual IReadOnlyList<string> Validate()
    {
        if (Config is null)
        {
            return [MissingConfigMessage];
        }

        return [.. Config.Validate(), .. WorldState.Validate()];
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
    /// <c>ChunkUnloaded</c>) and replaces <c>WorldState</c> with an empty one (same seed,
    /// size and chunk size) so the blueprint can be generated again.
    /// </summary>
    public void ResetBlueprint()
    {
        foreach (var coords in _inFlightChunks.Keys.Concat(_generatedChunks.Keys).ToList())
        {
            UnloadChunk(coords);
        }

        ReplaceWorldState();
    }

    // A cancelled job may still be reading the old instance, so never clear it in place.
    // Duplicate() keeps the script type and [Export]s; the blueprint lists start empty.
    private void ReplaceWorldState()
    {
        WorldState = (WorldState)WorldState.Duplicate();
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
            || Definitions is null
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
            var chunk = await GenerateChunkAsync(Config, Definitions, chunkCoords, job.Token);
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

    // The worker lambda only calls plain C#: passes write to a ChunkBuffer and log into a
    // list that is printed here, back on the main thread.
    private async Task<ChunkData> GenerateChunkAsync(
        ProceduralGenerationConfig config,
        GenerationDefinitions definitions,
        Vector2I chunkCoords,
        CancellationToken cancellationToken
    )
    {
        var (pipeline, world, logLevel, jobSlots) = (
            GenerationPipeline,
            WorldState,
            LogLevel,
            _chunkJobSlots
        );
        var chunk = PrepareChunk(world, chunkCoords);
        var logs = new List<string>();

        await jobSlots.WaitAsync(cancellationToken);
        (string PassName, Exception Error)? failure;
        try
        {
            failure = await Task.Run(
                () =>
                    pipeline?.RunPasses(
                        config,
                        definitions,
                        world,
                        chunk,
                        logLevel,
                        logs.Add,
                        cancellationToken
                    ),
                cancellationToken
            );
        }
        finally
        {
            jobSlots.Release();
            logs.ForEach(GD.Print);
        }

        // Wraps the original exception so the log line keeps its stack trace.
        return failure is { } f
            ? throw new InvalidOperationException($"{f.PassName}: {f.Error.Message}", f.Error)
            : new ChunkData(chunk);
    }

    private ChunkBuffer PrepareChunk(WorldState world, Vector2I chunkCoords)
    {
        var chunkSizeTiles = ResolvedChunkConfig.ChunkSizeTiles;
        var cityIds = world.GetCitiesInChunk(chunkCoords);
        var roadIds = world.GetRoadsInChunk(chunkCoords);

        var descriptor = new ChunkDescriptor(
            chunkCoords,
            DetermineChunkType(cityIds.Count > 0, roadIds.Count > 0),
            cityIds,
            roadIds,
            chunkSizeTiles
        );

        return new ChunkBuffer(descriptor);
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

    private void Log(string message, LogLevel level)
    {
        if (LogLevel >= level)
        {
            GD.Print(message);
        }
    }
}
