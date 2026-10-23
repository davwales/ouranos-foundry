using System;
using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Generation;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes.Generation;

[TestSuite]
[RequireGodotRuntime]
public class CityGenerationPassTests
{
    private const int ChunkSize = 16;

    // Doesn't divide ChunkSize, so a grid restarting at each chunk border would show.
    private const int StreetSpacing = 5;

    [TestCase(0, 0, 1, 0)]
    [TestCase(0, 0, 0, 1)]
    [TestCase(-1, 0, 0, 0)]
    [TestCase(0, -1, 0, 0)]
    [TestCase(-2, -1, -1, -1)]
    public void RunPass_AdjacentChunksOfOneCity_StreetGridIsContinuousInWorldSpace(
        int ax,
        int ay,
        int bx,
        int by
    )
    {
        // Arrange: a city centered on the shared border, large enough to cover both chunks.
        var a = new Vector2I(ax, ay);
        var b = new Vector2I(bx, by);
        var center = (b * ChunkSize + a * ChunkSize + new Vector2I(ChunkSize, ChunkSize)) / 2;
        var city = new City(Guid.NewGuid(), "Seam", center, "town", Guid.Empty, [], [], 64);
        var world = new WorldState { ChunkSizeTiles = ChunkSize };
        world.Cities.Add(city);
        world.RebuildLookups();
        var config = MakeConfig();
        var pipeline = new GenerationPipeline();
        pipeline.AddChild(new TerrainGenerationPass());
        pipeline.AddChild(new CityGenerationPass());
        pipeline._Ready();

        // Act
        var chunkA = Generate(pipeline, config, world, city, a);
        var chunkB = Generate(pipeline, config, world, city, b);

        // Assert: every street-ring tile on both sides follows the same world-space grid.
        var terrain = config.TerrainConfig!;
        var streets = 0;
        foreach (var chunk in new[] { chunkA, chunkB })
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                for (var x = 0; x < ChunkSize; x++)
                {
                    var tile = chunk.GetTile(x, y);
                    if (tile == terrain.BuildingTerrainId)
                    {
                        continue;
                    }

                    var worldX = chunk.Coordinates.X * ChunkSize + x;
                    var worldY = chunk.Coordinates.Y * ChunkSize + y;
                    var onGrid = worldX % StreetSpacing == 0 || worldY % StreetSpacing == 0;
                    var expected = onGrid ? terrain.StreetTerrainId : terrain.CityTerrainId;
                    tile.ShouldBe(expected, $"tile at world ({worldX}, {worldY})");
                    streets += onGrid ? 1 : 0;
                }
            }
        }

        streets.ShouldBeGreaterThan(0);

        pipeline.Free();
    }

    private static ChunkBuffer Generate(
        GenerationPipeline pipeline,
        ProceduralGenerationConfig config,
        WorldState world,
        City city,
        Vector2I coords
    )
    {
        var chunk = new ChunkBuffer(
            new ChunkDescriptor(
                coords,
                ChunkType.City,
                new HashSet<Guid> { city.Id },
                new HashSet<Guid>(),
                ChunkSize
            )
        );
        var error = pipeline.RunPasses(config, new(config), world, chunk, LogLevel.None, _ => { });
        error.ShouldBeNull();
        return chunk;
    }

    private static ProceduralGenerationConfig MakeConfig()
    {
        return new()
        {
            TerrainConfig = new TerrainConfig
            {
                StreetGridSpacing = StreetSpacing,
                CityCenterRatio = 0.1f,
            },
        };
    }
}
