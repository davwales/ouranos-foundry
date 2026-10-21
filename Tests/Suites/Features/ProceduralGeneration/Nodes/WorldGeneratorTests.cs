using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

[TestSuite]
[RequireGodotRuntime]
public class WorldGeneratorTests
{
    private static ProceduralGenerationConfig MakeConfig(ChunkConfig chunkConfig)
    {
        return new()
        {
            ChunkConfig = chunkConfig,
            NamingConfig = new NamingConfig(),
            GenerationConfig = new GenerationConfig(),
            BiomeDefinitions = [BiomeDefinition.Default],
            CitySizeDefinitions = [CitySizeDefinition.Default],
            PoiTypeDefinitions = [PoiTypeDefinition.Default],
            RoadTypeDefinitions = [RoadTypeDefinition.Default],
        };
    }

    private static (Node root, WorldGenerator generator, ChunkManager manager) CreateGenerator(
        ChunkConfig chunkConfig,
        Vector2 playerPosition
    )
    {
        var root = new Node();
        var player = new Node2D { Position = playerPosition };
        var manager = new ChunkManager().With(ChunkManager.PropertyName.Player, player);
        var generator = new WorldGenerator()
            .With(WorldGenerator.PropertyName.Config, MakeConfig(chunkConfig))
            .With(WorldGenerator.PropertyName.ChunkManager, manager);
        root.AddChild(player);
        root.AddChild(manager);
        root.AddChild(generator);
        return (root, generator, manager);
    }

    [TestCase]
    public async Task Ready_WithChunkSizeTiles32_UsesItEverywhere()
    {
        // Arrange
        var chunkConfig = new ChunkConfig
        {
            ChunkSizeTiles = 32,
            TilePixelSize = 2,
            ViewDistance = 0,
        };
        // 64 px per chunk: x -1 is chunk -1, y 130 is chunk 2.
        var (root, generator, manager) = CreateGenerator(chunkConfig, new Vector2(-1, 130));
        var requested = new List<Vector2I>();
        void OnRequested(Vector2I coords)
        {
            requested.Add(coords);
        }

        manager.RequestChunkGeneration += OnRequested;

        // Act
        generator._Ready();
        var city = new City(Guid.NewGuid(), "Test", new(40, 70), "town", Guid.Empty, [], [], 5);
        generator.WorldState.Cities.Add(city);
        generator.WorldState.RebuildSpatialIndex();

        var generated = generator.ToSignal(generator, WorldGenerator.SignalName.ChunkGenerated);
        manager._Process(0);
        await generated;

        // Assert
        generator.WorldState.ChunkSizeTiles.ShouldBe(32);
        generator.WorldState.ChunkToCityIds.Keys.ShouldBe(new List<Vector2I> { new(1, 2) });
        requested.ShouldBe(new List<Vector2I> { new(-1, 2) });

        var chunk = generator.GetChunk(new Vector2I(-1, 2)).ShouldNotBeNull();
        chunk.Descriptor.ChunkSizeTiles.ShouldBe(32);
        chunk.TerrainData.Length.ShouldBe(32 * 32);

        manager.RequestChunkGeneration -= OnRequested;
        root.Free();
    }
}
