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
/// As an <c>ISaveable</c> it saves the blueprint and the tiles changed in its chunks.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class WorldGenerator : Node, ISaveable
{
    /// <summary>
    /// The key a <c>SaveManager</c> stores the world under. Empty: this node's path.
    /// </summary>
    [Export]
    public string SaveKey { get; private set; } = "";

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

    // EditedTiles of chunks that unloaded, applied again when they regenerate.
    private readonly Dictionary<Vector2I, Dictionary<int, int>> _unloadedChunkEdits = [];
    private SemaphoreSlim _chunkJobSlots = new(1);

    private const string BlueprintKey = "blueprint";
    private const string ChunkSizeTilesKey = "chunk_size_tiles";
    private const string ChunkEditsKey = "chunk_edits";

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

        return BuildBlueprint(
            "Generating world blueprint...",
            (config, definitions) =>
                BlueprintPipeline?.RunPasses(config, definitions, WorldState, LogLevel)
        )
            is null;
    }

    // Validates, snapshots the definitions and fills WorldState. Returns the error, or null
    // once the blueprint is complete and BlueprintGenerated is emitted.
    private string? BuildBlueprint(
        string startMessage,
        Func<
            ProceduralGenerationConfig,
            GenerationDefinitions,
            (string PassName, Exception Error)?
        > fill
    )
    {
        var errors = Validate();
        if (Config is null || errors.Count > 0)
        {
            return FailBlueprint(nameof(WorldGenerator), string.Join("\n", errors));
        }

        Log(startMessage, LogLevel.Basic);

        Definitions = new GenerationDefinitions(Config);
        foreach (var warning in Config.GetWarnings(Definitions))
        {
            GD.PushWarning($"WorldGenerator: {warning}");
        }

        WorldState.ChunkSizeTiles = ResolvedChunkConfig.ChunkSizeTiles;

        if (fill(Config, Definitions) is { } failure)
        {
            ReplaceWorldState();
            return FailBlueprint(failure.PassName, failure.Error.Message, failure.Error);
        }

        WorldState.IsBlueprintGenerated = true;
        ChunkManager?.ResetTracking();

        Log("World blueprint complete.", LogLevel.Basic);
        EmitSignal(SignalName.BlueprintGenerated);
        return null;
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

    private string FailBlueprint(string passName, string error, Exception? exception = null)
    {
        GD.PushError(
            $"WorldGenerator: blueprint pass {passName} failed: {exception?.ToString() ?? error}"
        );
        EmitSignal(SignalName.BlueprintFailed, passName, error);
        return error;
    }

    /// <summary>
    /// Cancels in-flight chunk jobs, unloads every generated chunk (emitting
    /// <c>ChunkUnloaded</c>), forgets every chunk's edited tiles and replaces
    /// <c>WorldState</c> with an empty one (same seed, size and chunk size) so the blueprint
    /// can be generated again.
    /// </summary>
    public void ResetBlueprint()
    {
        foreach (var coords in _inFlightChunks.Keys.Concat(_generatedChunks.Keys).ToList())
        {
            UnloadChunk(coords);
        }

        _unloadedChunkEdits.Clear();
        ReplaceWorldState();
    }

    /// <summary>
    /// Saves the blueprint and the tiles changed with <c>ChunkData.SetTile</c>, in loaded
    /// and unloaded chunks alike. Generated terrain isn't saved: it regenerates from the
    /// seed. Override it, together with <c>Load</c>, to save more.
    /// </summary>
    public virtual Godot.Collections.Dictionary<string, Variant> Save()
    {
        var data = new Godot.Collections.Dictionary<string, Variant>
        {
            [ChunkSizeTilesKey] = ResolvedChunkConfig.ChunkSizeTiles,
            [ChunkEditsKey] = SaveChunkEdits(),
        };

        if (WorldState.IsBlueprintGenerated)
        {
            data[BlueprintKey] = WorldState.SaveBlueprint();
        }

        return data;
    }

    /// <summary>
    /// Replaces the world with a saved one: resets it (see <c>ResetBlueprint</c>), loads the
    /// saved blueprint, or generates one when the save has none, then restores the edited
    /// tiles. Emits <c>BlueprintGenerated</c>, after which <c>ChunkManager</c> streams the
    /// chunks in again. On failure it emits <c>BlueprintFailed</c> and throws.
    /// </summary>
    public virtual void Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        // Read the edits first, so malformed data throws before the world is reset.
        var edits = LoadChunkEdits(data);
        ResetBlueprint();
        if (!data.TryGetValue(BlueprintKey, out var blueprint))
        {
            if (!GenerateBlueprint())
            {
                throw new InvalidOperationException("The blueprint failed to generate.");
            }
        }
        else if (
            BuildBlueprint("Loading world blueprint...", (_, _) => LoadBlueprint(blueprint)) is
            { } error
        )
        {
            throw new InvalidOperationException(error);
        }

        foreach (var (coords, tiles) in edits)
        {
            _unloadedChunkEdits[coords] = tiles;
        }
    }

    private Dictionary<Vector2I, Dictionary<int, int>> LoadChunkEdits(
        Godot.Collections.Dictionary<string, Variant> data
    )
    {
        var savedSize = data[ChunkSizeTilesKey].AsInt32();
        if (savedSize != ResolvedChunkConfig.ChunkSizeTiles)
        {
            GD.PushWarning(
                $"WorldGenerator: the save's chunks are {savedSize} tiles wide, not "
                    + $"{ResolvedChunkConfig.ChunkSizeTiles}; its edited tiles are dropped."
            );
            return [];
        }

        var edits = new Dictionary<Vector2I, Dictionary<int, int>>();
        foreach (var chunk in data[ChunkEditsKey].AsGodotArray())
        {
            var edit = chunk.AsGodotDictionary();
            edits[edit["coords"].AsVector2I()] = edit["tiles"]
                .AsInt32Array()
                .Chunk(2)
                .Where(pair => pair.Length == 2)
                .ToDictionary(pair => pair[0], pair => pair[1]);
        }

        return edits;
    }

    private (string PassName, Exception Error)? LoadBlueprint(Variant blueprint)
    {
        try
        {
            WorldState.LoadBlueprint(blueprint.AsGodotDictionary<string, Variant>());
            return null;
        }
        catch (Exception e)
        {
            return (nameof(WorldGenerator), e);
        }
    }

    // One entry per chunk with edits, sorted by coordinates so equal worlds save equally.
    private Godot.Collections.Array SaveChunkEdits()
    {
        var edits = _generatedChunks
            .Select(pair => (Coords: pair.Key, Tiles: pair.Value.EditedTiles))
            .Concat(
                _unloadedChunkEdits.Select(pair =>
                    (Coords: pair.Key, Tiles: (IReadOnlyDictionary<int, int>)pair.Value)
                )
            )
            .Where(edit => edit.Tiles.Count > 0)
            .OrderBy(edit => edit.Coords.X)
            .ThenBy(edit => edit.Coords.Y);

        var array = new Godot.Collections.Array();
        foreach (var (coords, tiles) in edits)
        {
            array.Add(
                new Godot.Collections.Dictionary<string, Variant>
                {
                    ["coords"] = coords,
                    ["tiles"] = tiles
                        .OrderBy(tile => tile.Key)
                        .SelectMany(tile => new[] { tile.Key, tile.Value })
                        .ToArray(),
                }
            );
        }

        return array;
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

        if (_generatedChunks.Remove(coords, out var chunk))
        {
            if (chunk.EditedTiles.Count > 0)
            {
                _unloadedChunkEdits[coords] = new(chunk.EditedTiles);
            }

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
            var buffer = await GenerateChunkAsync(Config, Definitions, chunkCoords, job.Token);
            if (IsCurrentJob(chunkCoords, job))
            {
                _unloadedChunkEdits.Remove(chunkCoords, out var edits);
                var chunk = new ChunkData(buffer, edits);
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
    private async Task<ChunkBuffer> GenerateChunkAsync(
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
            : chunk;
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

        return new ChunkBuffer(descriptor, world.GetPointsOfInterestInChunk(chunkCoords));
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
