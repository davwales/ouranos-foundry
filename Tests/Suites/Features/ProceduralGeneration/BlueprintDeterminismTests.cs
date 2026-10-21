using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Blueprints;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration;

[TestSuite]
[RequireGodotRuntime]
public class BlueprintDeterminismTests
{
    private static ProceduralGenerationConfig MakeConfig()
    {
        return new()
        {
            NamingConfig = new NamingConfig(),
            GenerationConfig = new GenerationConfig(),
            BiomeDefinitions = [BiomeDefinition.Default],
            CitySizeDefinitions = [CitySizeDefinition.Default],
            PoiTypeDefinitions = [PoiTypeDefinition.Default],
            RoadTypeDefinitions = [RoadTypeDefinition.Default],
        };
    }

    private static WorldState GenerateBlueprint(ulong seed)
    {
        var config = MakeConfig();
        var world = new WorldState { Seed = seed };
        BlueprintPass[] passes =
        [
            new RegionBlueprintPass(),
            new CityBlueprintPass(),
            new RoadBlueprintPass(),
        ];

        foreach (var pass in passes)
        {
            // ProcGenConfig has a private setter (inspector-wired), so assign it reflectively.
            pass.GetType().GetProperty("ProcGenConfig")!.SetValue(pass, config);
            pass.RunPass(world);
            pass.Free();
        }

        return world;
    }

    [TestCase]
    public void SameSeed_ProducesIdenticalBlueprintIncludingIds()
    {
        // Arrange & Act
        var a = GenerateBlueprint(1234);
        var b = GenerateBlueprint(1234);

        // Assert
        a.Cities.Count.ShouldBeGreaterThan(0);
        a.Roads.Count.ShouldBeGreaterThan(0);

        Describe(a).ShouldBe(Describe(b));
    }

    [TestCase]
    public void DifferentSeeds_ProduceDifferentIds()
    {
        // Arrange & Act
        var a = GenerateBlueprint(1234);
        var b = GenerateBlueprint(5678);

        // Assert
        a.Regions.Select(r => r.Id).ShouldNotBe(b.Regions.Select(r => r.Id));
    }

    // Records holding List<T> compare lists by reference, so flatten everything to strings.
    internal static List<string> Describe(WorldState world)
    {
        var lines = new List<string>();

        foreach (var r in world.Regions)
        {
            lines.Add(
                $"region {r.Id} {r.Name} {r.Center} {r.BiomeId} "
                    + $"[{string.Join(",", r.CityIds)}] [{string.Join(",", r.RoadIds)}]"
            );
        }

        foreach (var c in world.Cities)
        {
            lines.Add(
                $"city {c.Id} {c.Name} {c.Position} {c.SizeId} {c.RegionId} {c.Radius} "
                    + $"[{string.Join(",", c.RoadIds)}]"
            );
            lines.AddRange(
                c.PointsOfInterest.Select(p =>
                    $"poi {p.Id} {p.Name} {p.PoiTypeId} {p.Position} {p.CityId}"
                )
            );
        }

        foreach (var road in world.Roads)
        {
            lines.Add(
                $"road {road.Id} {road.SourceCityId} {road.TargetCityId} {road.RoadTypeId} "
                    + $"[{string.Join(",", road.PathPoints)}]"
            );
        }

        return lines;
    }
}
