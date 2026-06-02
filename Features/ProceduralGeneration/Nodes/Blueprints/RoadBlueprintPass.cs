using Ouranos.Foundry.Core.Types;
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
    [Export]
    public ProceduralGenerationConfig? ProcGenConfig { get; private set; }

    [Export]
    public string DefaultRoadTypeId { get; private set; } = "dirt";

    public override void RunPass(WorldBlueprint worldBlueprint)
    {
        if (worldBlueprint is not WorldState worldState)
        {
            GD.PrintErr("RoadBlueprintPass requires a WorldState.");
            return;
        }

        if (worldState.Cities.Count == 0)
        {
            GD.PrintErr("RoadBlueprintPass: No cities found. Run CityBlueprintPass first.");
            return;
        }

        if (ProcGenConfig is null)
        {
            GD.PushError(
                $"{Name}: ProceduralGenerationConfig is not assigned. Add it in the inspector."
            );
            return;
        }

        var config = ProcGenConfig;
        var genConfig = config.GenerationConfig ?? new GenerationConfig();
        var rng = new SeededRng(worldState.Seed, 3000);
        var roadCount = 0;

        roadCount += ConnectIntraRegionRoads(worldState, rng, config, genConfig);
        roadCount += ConnectInterRegionRoads(worldState, rng, config);

        worldState.RebuildLookups();
        worldState.RebuildSpatialIndex();

        GD.Print($"Placed {roadCount} roads connecting {worldState.Cities.Count} cities.");
    }

    private int ConnectIntraRegionRoads(
        WorldState worldState,
        SeededRng rng,
        ProceduralGenerationConfig config,
        GenerationConfig genConfig
    )
    {
        var roadCount = 0;

        foreach (var region in worldState.Regions)
        {
            var regionCities = region
                .CityIds.Select(id => worldState.CityById.GetValueOrDefault(id))
                .Where(c => c is not null)
                .ToList();

            if (regionCities.Count < 2)
            {
                continue;
            }

            var mstRoads = BuildMinimumSpanningTree(regionCities, rng, genConfig);
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

    private int ConnectInterRegionRoads(
        WorldState worldState,
        SeededRng rng,
        ProceduralGenerationConfig config
    )
    {
        var roadCount = 0;
        var hubRoads = ConnectRegionalHubs(worldState, rng, config);
        foreach (var road in hubRoads)
        {
            worldState.Roads.Add(road);
            roadCount++;
        }

        return roadCount;
    }

    private List<Road> BuildMinimumSpanningTree(
        List<City?> cities,
        SeededRng rng,
        GenerationConfig config
    )
    {
        var roads = new List<Road>();
        if (cities.Count < 2)
        {
            return roads;
        }

        var edges = new List<(float Distance, int From, int To)>();
        for (var i = 0; i < cities.Count; i++)
        {
            for (var j = i + 1; j < cities.Count; j++)
            {
                if (cities[i] is null || cities[j] is null)
                {
                    continue;
                }

                var dist = cities[i]!.Position.DistanceTo(cities[j]!.Position);
                edges.Add((dist, i, j));
            }
        }

        edges.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        RunKruskalMst(
            edges,
            cities.Count,
            (from, to) =>
            {
                var cityFrom = cities[from]!;
                var cityTo = cities[to]!;

                var road = CreateRoad(rng, cityFrom, cityTo, config, DefaultRoadTypeId);
                roads.Add(road);
            }
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
        ProceduralGenerationConfig config
    )
    {
        var roads = new List<Road>();
        var hubRoadDef = config.GetRoadTypeDefinition(config.HubRoadTypeId);

        var hubs = CollectRegionalHubs(world, rng);

        if (hubs.Count < 2)
        {
            return roads;
        }

        var edges = BuildHubEdges(hubs);

        edges.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        RunKruskalMst(
            edges,
            hubs.Count,
            (from, to) =>
            {
                var road = CreateRoad(
                    rng,
                    hubs[from],
                    hubs[to],
                    config.GenerationConfig ?? new GenerationConfig(),
                    hubRoadDef.Id
                );
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
            var regionCities = region
                .CityIds.Select(id => world.CityById.GetValueOrDefault(id))
                .Where(c => c is not null)
                .ToList();

            if (regionCities.Count == 0)
            {
                continue;
            }

            var hub = regionCities
                .OrderByDescending(c => c!.Radius)
                .ThenBy(_ => rng.Next())
                .First();
            hubs.Add(hub!);
        }

        return hubs;
    }

    private static List<(float Distance, int From, int To)> BuildHubEdges(List<City> hubs)
    {
        var edges = new List<(float Distance, int From, int To)>();
        for (var i = 0; i < hubs.Count; i++)
        {
            for (var j = i + 1; j < hubs.Count; j++)
            {
                var dist = hubs[i].Position.DistanceTo(hubs[j].Position);
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
            Id: Guid.NewGuid(),
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
