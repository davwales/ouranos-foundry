#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Core.Utils;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

[GlobalClass]
public partial class WorldState : Resource
{
    [Export]
    public ulong Seed { get; set; }

    public bool IsBlueprintGenerated { get; set; }

    [Export]
    public int Size { get; set; } = 1000;

    /// <summary>
    /// Chunk size in tiles, used to bucket the spatial index. Set by WorldGenerator
    /// from <c>ChunkConfig.ChunkSizeTiles</c> before the blueprint passes run.
    /// </summary>
    public int ChunkSizeTiles { get; set; } = 16;

    public List<Region> Regions { get; private set; } = [];

    public List<City> Cities { get; private set; } = [];

    public List<Road> Roads { get; private set; } = [];

    /// <summary>
    /// Spatial index mapping chunk coordinates to city IDs that overlap that chunk.
    /// Built during blueprint generation and used by generation passes.
    /// </summary>
    public Dictionary<Vector2I, HashSet<Guid>> ChunkToCityIds { get; private set; } = [];

    /// <summary>
    /// Spatial index mapping chunk coordinates to road IDs that overlap that chunk.
    /// Built during blueprint generation and used by generation passes.
    /// </summary>
    public Dictionary<Vector2I, HashSet<Guid>> ChunkToRoadIds { get; private set; } = [];

    /// <summary>
    /// Fast lookup from city ID to city data.
    /// </summary>
    public Dictionary<Guid, City> CityById { get; private set; } = [];

    /// <summary>
    /// Fast lookup from road ID to road data.
    /// </summary>
    public Dictionary<Guid, Road> RoadById { get; private set; } = [];

    /// <summary>
    /// Removes all blueprint data and resets <c>IsBlueprintGenerated</c>, so the
    /// blueprint can be generated again without duplicating anything.
    /// </summary>
    public void Clear()
    {
        Regions.Clear();
        Cities.Clear();
        Roads.Clear();
        CityById.Clear();
        RoadById.Clear();
        ChunkToCityIds.Clear();
        ChunkToRoadIds.Clear();
        IsBlueprintGenerated = false;
    }

    /// <summary>
    /// Rebuilds lookup dictionaries after modifying Regions, Cities, or Roads lists.
    /// Must be called after each blueprint pass completes.
    /// </summary>
    public void RebuildLookups()
    {
        CityById.Clear();
        foreach (var city in Cities)
        {
            CityById[city.Id] = city;
        }

        RoadById.Clear();
        foreach (var road in Roads)
        {
            RoadById[road.Id] = road;
        }
    }

    /// <summary>
    /// Rebuilds the spatial index mapping chunk coordinates to overlapping entities.
    /// Must be called after cities and roads have been placed in blueprint passes.
    /// </summary>
    public void RebuildSpatialIndex()
    {
        ChunkToCityIds.Clear();
        ChunkToRoadIds.Clear();

        foreach (var city in Cities)
        {
            var radius = new Vector2I(city.Radius, city.Radius);
            var minChunk = ChunkMath.WorldToChunk(city.Position - radius, ChunkSizeTiles);
            var maxChunk = ChunkMath.WorldToChunk(city.Position + radius, ChunkSizeTiles);

            for (var x = minChunk.X; x <= maxChunk.X; x++)
            {
                for (var y = minChunk.Y; y <= maxChunk.Y; y++)
                {
                    var chunkCoord = new Vector2I(x, y);
                    if (!ChunkToCityIds.TryGetValue(chunkCoord, out var set))
                    {
                        set = [];
                        ChunkToCityIds[chunkCoord] = set;
                    }

                    set.Add(city.Id);
                }
            }
        }

        foreach (var road in Roads)
        {
            for (var i = 0; i < road.PathPoints.Count - 1; i++)
            {
                var from = road.PathPoints[i];
                var to = road.PathPoints[i + 1];

                foreach (var point in Bresenham.GetPoints(from, to))
                {
                    var chunkCoord = ChunkMath.WorldToChunk(point, ChunkSizeTiles);
                    if (!ChunkToRoadIds.TryGetValue(chunkCoord, out var set))
                    {
                        set = [];
                        ChunkToRoadIds[chunkCoord] = set;
                    }

                    set.Add(road.Id);
                }
            }
        }
    }

    /// <summary>
    /// Returns all city IDs whose footprint overlaps the given chunk coordinate.
    /// </summary>
    public IReadOnlySet<Guid> GetCitiesInChunk(Vector2I chunkCoord)
    {
        return ChunkToCityIds.GetValueOrDefault(chunkCoord, []);
    }

    /// <summary>
    /// Returns all road IDs whose path overlaps the given chunk coordinate.
    /// </summary>
    public IReadOnlySet<Guid> GetRoadsInChunk(Vector2I chunkCoord)
    {
        return ChunkToRoadIds.GetValueOrDefault(chunkCoord, []);
    }

    /// <summary>
    /// Finds the region whose center is nearest to the given world position
    /// using Voronoi nearest-center partitioning (O(n) in region count).
    /// </summary>
    public Region? FindRegionAt(Vector2I position)
    {
        Region? nearest = null;
        var minDist = float.MaxValue;
        foreach (var region in Regions)
        {
            var dist = position.DistanceSquaredTo(region.Center);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = region;
            }
        }
        return nearest;
    }
}
