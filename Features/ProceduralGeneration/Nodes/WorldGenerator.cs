using System;
using System.Collections.Generic;
using System.Linq;
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
[Icon("res://Assets/icons/foundry_node.svg")]
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

    [Export]
    public LogLevel LogLevel { get; private set; } = LogLevel.Basic;

    public bool IsBlueprintGenerated => WorldState.IsBlueprintGenerated;

    public bool IsChunkReady(Vector2I coords) => _generatedChunks.ContainsKey(coords);

    public ChunkData? GetChunk(Vector2I coords) => _generatedChunks.GetValueOrDefault(coords);

    [Signal]
    public delegate void BlueprintGeneratedEventHandler();

    [Signal]
    public delegate void ChunkGeneratedEventHandler(Vector2I coords, ChunkData chunk);

    [Signal]
    public delegate void ChunkUnloadedEventHandler(Vector2I coords);

    [Signal]
    public delegate void GenerationFailedEventHandler(Vector2I coords, string error);

    private readonly Dictionary<Vector2I, ChunkData> _generatedChunks = [];
    private readonly HashSet<Vector2I> _inFlightChunks = [];
    private List<GenerationPass> _cachedGenerationPasses = [];

    public override void _Ready()
    {
        CacheGenerationPasses();
        GenerateBlueprint();

        if (ChunkManager is not null)
        {
            ChunkManager.RequestChunkGeneration += OnChunkGenerationRequested;
            ChunkManager.RequestChunkUnload += OnChunkUnloadRequested;

            var viewDistance = Config?.ChunkConfig?.ViewDistance ?? 3;
            ChunkManager.SetViewDistance(viewDistance);
        }
    }

    private void CacheGenerationPasses()
    {
        if (GenerationPipeline is not null)
        {
            _cachedGenerationPasses = GenerationPipeline.GetPasses().ToList();
        }
    }

    public void GenerateBlueprint()
    {
        if (WorldState.IsBlueprintGenerated)
        {
            return;
        }

        Log("Generating world blueprint...", LogLevel.Basic);

        ValidateConfig();

        BlueprintPipeline?.RunPasses(WorldState);
        WorldState.IsBlueprintGenerated = true;

        Log("World blueprint complete.", LogLevel.Basic);
        EmitSignal(SignalName.BlueprintGenerated);
    }

    public void ResetBlueprint()
    {
        WorldState.IsBlueprintGenerated = false;
    }

    public void UnloadChunk(Vector2I coords)
    {
        if (_generatedChunks.Remove(coords))
        {
            Log($"Chunk {coords} unloaded.", LogLevel.Verbose);
            EmitSignal(SignalName.ChunkUnloaded, coords);
        }
    }

    private async void OnChunkGenerationRequested(Vector2I chunkCoords)
    {
        if (_generatedChunks.ContainsKey(chunkCoords))
        {
            return;
        }

        if (!_inFlightChunks.Add(chunkCoords))
        {
            return;
        }

        try
        {
            await GenerateChunkAsync(chunkCoords);
        }
        catch (Exception e)
        {
            Log($"Chunk generation failed for {chunkCoords}: {e}", LogLevel.Basic);
            EmitSignal(SignalName.GenerationFailed, chunkCoords, e.Message);
        }
        finally
        {
            _inFlightChunks.Remove(chunkCoords);
        }
    }

    private async Task GenerateChunkAsync(Vector2I chunkCoords)
    {
        var chunk = PrepareChunkData(chunkCoords);

        await Task.Run(() =>
        {
            foreach (var pass in _cachedGenerationPasses)
            {
                pass.RunPass(WorldState, chunk);
            }

            chunk.IsGenerated = true;
        });

        _generatedChunks[chunkCoords] = chunk;

        Log(
            $"Chunk {chunkCoords} generated. Type: {chunk.Type}, "
                + $"Cities: {chunk.OverlappingCityIds.Count}, Roads: {chunk.OverlappingRoadIds.Count}",
            LogLevel.Verbose
        );
        EmitSignal(SignalName.ChunkGenerated, chunkCoords, chunk);
    }

    /// Must be called on the main thread since it reads from WorldState.
    private ChunkData PrepareChunkData(Vector2I chunkCoords)
    {
        var tileSize = Config?.ChunkConfig?.TileSize ?? 16;
        var chunk = InitialChunkData.Copy();

        var cityIds = WorldState.GetCitiesInChunk(chunkCoords);
        var roadIds = WorldState.GetRoadsInChunk(chunkCoords);

        var descriptor = new ChunkDescriptor(
            chunkCoords,
            DetermineChunkType(cityIds.Count > 0, roadIds.Count > 0),
            cityIds,
            roadIds,
            tileSize
        );

        chunk.Descriptor = descriptor;
        chunk.TerrainData = new int[tileSize * tileSize];

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
