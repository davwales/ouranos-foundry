#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Core.Attributes;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Core.Utils;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Blueprints;

/// <summary>
/// Connects cities with roads, establishing travel routes through the world.
/// Within each region, cities are connected via a minimum spanning tree.
/// Inter-region roads connect regional hubs. Must run after CityBlueprintPass.
/// </summary>
[RequiresPasses(typeof(RegionBlueprintPass), typeof(CityBlueprintPass))]
[GlobalClass]
public partial class RoadBlueprintPass : BlueprintPass
{
    public override ulong RngSalt => 3000;

    public override void RunPass(GenerationContext ctx)
    {
        var (config, worldState, rng) = (ctx.Config, ctx.World, ctx.Rng);
        if (worldState.Cities.Count == 0)
        {
            throw new InvalidOperationException(
                $"{GetType().Name}: No cities found. Run CityBlueprintPass first."
            );
        }

        var genConfig = config.GenerationConfig ?? new GenerationConfig();
        var roadCount = ConnectIntraRegionRoads(worldState, rng, config, genConfig);
        roadCount += ConnectInterRegionRoads(worldState, rng, config, genConfig);

        worldState.RebuildLookups();
        worldState.RebuildSpatialIndex();

        ctx.Log($"Placed {roadCount} roads connecting {worldState.Cities.Count} cities.");
    }

    private static int ConnectIntraRegionRoads(
        WorldState worldState,
        SeededRng rng,
        ProceduralGenerationConfig config,
        GenerationConfig genConfig
    )
    {
        var roadCount = 0;

        foreach (var region in worldState.Regions)
        {
            var regionCities = CitiesOf(region, worldState);
            if (regionCities.Count < 2)
            {
                continue;
            }

            var mstRoads = BuildMinimumSpanningTree(
                regionCities,
                rng,
                genConfig,
                config.DefaultRoadTypeId
            );
            foreach (var road in mstRoads)
            {
                worldState.Roads.Add(road);
                region.RoadIds.Add(road.Id);

                if (worldState.CityById.TryGetValue(road.SourceCityId, out var source))
                {
                    source.RoadIds.Add(road.Id);
                }

                if (worldState.CityById.TryGetValue(road.TargetCityId, out var target))
                {
                    target.RoadIds.Add(road.Id);
                }

                roadCount++;
            }
        }

        return roadCount;
    }

    private static int ConnectInterRegionRoads(
        WorldState worldState,
        SeededRng rng,
        ProceduralGenerationConfig config,
        GenerationConfig genConfig
    )
    {
        var roadCount = 0;
        var hubRoads = ConnectRegionalHubs(worldState, rng, config, genConfig);
        foreach (var road in hubRoads)
        {
            worldState.Roads.Add(road);
            roadCount++;
        }

        return roadCount;
    }

    private static List<City> CitiesOf(Region region, WorldState world)
    {
        return
        [
            .. region.CityIds.Select(id => world.CityById.GetValueOrDefault(id)).OfType<City>(),
        ];
    }

    private static List<Road> BuildMinimumSpanningTree(
        List<City> cities,
        SeededRng rng,
        GenerationConfig config,
        string roadTypeId
    )
    {
        var roads = new List<Road>();
        var edges = BuildEdges(cities);
        edges.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        RunKruskalMst(
            edges,
            cities.Count,
            (from, to) => roads.Add(CreateRoad(rng, cities[from], cities[to], config, roadTypeId))
        );

        return roads;
    }

    private static void RunKruskalMst(
        List<(float Distance, int From, int To)> edges,
        int nodeCount,
        Action<int, int> onEdgeSelected
    )
    {
        var parent = new int[nodeCount];
        for (var i = 0; i < parent.Length; i++)
        {
            parent[i] = i;
        }

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }

            return x;
        }

        void Union(int x, int y)
        {
            var px = Find(x);
            var py = Find(y);
            if (px != py)
            {
                parent[px] = py;
            }
        }

        foreach (var (_, from, to) in edges)
        {
            if (Find(from) != Find(to))
            {
                Union(from, to);
                onEdgeSelected(from, to);
            }
        }
    }

    private static List<Road> ConnectRegionalHubs(
        WorldState world,
        SeededRng rng,
        ProceduralGenerationConfig config,
        GenerationConfig genConfig
    )
    {
        var roads = new List<Road>();
        var hubRoadDef = config.GetRoadTypeDefinition(config.HubRoadTypeId);

        var hubs = CollectRegionalHubs(world, rng);

        if (hubs.Count < 2)
        {
            return roads;
        }

        var edges = BuildEdges(hubs);

        edges.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        RunKruskalMst(
            edges,
            hubs.Count,
            (from, to) =>
            {
                var road = CreateRoad(rng, hubs[from], hubs[to], genConfig, hubRoadDef.Id);
                roads.Add(road);

                if (world.CityById.TryGetValue(hubs[from].Id, out var sourceCity))
                {
                    sourceCity.RoadIds.Add(road.Id);
                }

                if (world.CityById.TryGetValue(hubs[to].Id, out var targetCity))
                {
                    targetCity.RoadIds.Add(road.Id);
                }
            }
        );

        return roads;
    }

    private static List<City> CollectRegionalHubs(WorldState world, SeededRng rng)
    {
        var hubs = new List<City>();
        foreach (var region in world.Regions)
        {
            var regionCities = CitiesOf(region, world);
            if (regionCities.Count == 0)
            {
                continue;
            }

            var hub = regionCities.OrderByDescending(c => c.Radius).ThenBy(_ => rng.Next()).First();
            hubs.Add(hub);
        }

        return hubs;
    }

    private static List<(float Distance, int From, int To)> BuildEdges(List<City> cities)
    {
        var edges = new List<(float Distance, int From, int To)>();
        for (var i = 0; i < cities.Count; i++)
        {
            for (var j = i + 1; j < cities.Count; j++)
            {
                var dist = cities[i].Position.DistanceTo(cities[j].Position);
                edges.Add((dist, i, j));
            }
        }

        return edges;
    }

    private static Road CreateRoad(
        SeededRng rng,
        City source,
        City target,
        GenerationConfig config,
        string roadTypeId
    )
    {
        var pathPoints = GeneratePathPoints(source.Position, target.Position, rng, config);

        return new Road(
            Id: StableId.Create(source.Id, "road", target.Id),
            SourceCityId: source.Id,
            TargetCityId: target.Id,
            RoadTypeId: roadTypeId,
            PathPoints: pathPoints
        );
    }

    private static List<Vector2I> GeneratePathPoints(
        Vector2I from,
        Vector2I to,
        SeededRng rng,
        GenerationConfig config
    )
    {
        var points = new List<Vector2I> { from };

        var distance = from.DistanceTo(to);
        var waypointCount = Math.Max(2, (int)(distance / config.RoadWaypointSpacing));

        for (var i = 1; i < waypointCount; i++)
        {
            var t = (float)i / waypointCount;
            var x =
                (int)Mathf.Lerp(from.X, to.X, t)
                + rng.Next(-config.RoadWaypointJitter, config.RoadWaypointJitter);
            var y =
                (int)Mathf.Lerp(from.Y, to.Y, t)
                + rng.Next(-config.RoadWaypointJitter, config.RoadWaypointJitter);
            points.Add(new Vector2I(x, y));
        }

        points.Add(to);
        return points;
    }
}
