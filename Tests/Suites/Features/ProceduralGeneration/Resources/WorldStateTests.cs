using System;
using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Resources;

[TestSuite]
[RequireGodotRuntime]
public class WorldStateTests
{
    private static City MakeCity(Vector2I position, int radius)
    {
        return new City(Guid.NewGuid(), "Test", position, "town", Guid.Empty, [], [], radius);
    }

    private static Road MakeRoad(params Vector2I[] points)
    {
        return new Road(Guid.NewGuid(), Guid.Empty, Guid.Empty, "road", [.. points]);
    }

    [TestCase]
    public void RebuildSpatialIndex_CitySpanningNegativeTile_IndexesIntoNegativeChunk()
    {
        // Arrange: footprint covers tiles x -1..5, y 5..11.
        var world = new WorldState { ChunkSizeTiles = 16 };
        var city = MakeCity(new Vector2I(2, 8), 3);
        world.Cities.Add(city);

        // Act
        world.RebuildSpatialIndex();

        // Assert
        world.ChunkToCityIds.Keys.ShouldBe(
            new List<Vector2I> { new(-1, 0), new(0, 0) },
            ignoreOrder: true
        );
        world.GetCitiesInChunk(new Vector2I(-1, 0)).ShouldContain(city.Id);
    }

    [TestCase]
    public void RebuildSpatialIndex_RoadThroughNegativeTiles_IndexesIntoNegativeChunks()
    {
        // Arrange: tiles x -20..3 at y -3.
        var world = new WorldState { ChunkSizeTiles = 16 };
        var road = MakeRoad(new Vector2I(-20, -3), new Vector2I(3, -3));
        world.Roads.Add(road);

        // Act
        world.RebuildSpatialIndex();

        // Assert
        world.ChunkToRoadIds.Keys.ShouldBe(
            new List<Vector2I> { new(-2, -1), new(-1, -1), new(0, -1) },
            ignoreOrder: true
        );
        world.GetRoadsInChunk(new Vector2I(-2, -1)).ShouldContain(road.Id);
    }
}
