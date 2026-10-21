#nullable enable

using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Core.Utils;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Blueprints;

/// <summary>
/// Partitions the world into regions using Voronoi-like partitioning based on
/// seeded random points. Assigns biomes and names to each region.
/// Must be the first blueprint pass - other passes rely on region data.
/// </summary>
[GlobalClass]
public partial class RegionBlueprintPass : BlueprintPass
{
    protected override ulong RngSalt => 1000;

    public override void RunPass(GenerationContext ctx)
    {
        var (config, worldState, rng) = (ctx.Config, ctx.World, ctx.Rng);
        var naming = config.NamingConfig ?? new NamingConfig();
        var genConfig = config.GenerationConfig ?? new GenerationConfig();

        var worldSize = worldState.Size;
        var regionCenters = GenerateRegionCenters(rng, worldSize, genConfig);

        for (var i = 0; i < regionCenters.Count; i++)
        {
            var center = regionCenters[i];
            var biomeDef = rng.NextElement(config.BiomeDefinitions);
            var name = GenerateName(rng, naming);

            var region = new Region(
                Id: StableId.Create(worldState.Seed, "region", (ulong)i),
                Name: name,
                Center: center,
                BiomeId: biomeDef.Id,
                CityIds: [],
                RoadIds: []
            );

            worldState.Regions.Add(region);
        }

        ctx.Log($"Placed {worldState.Regions.Count} regions.");
    }

    private static List<Vector2I> GenerateRegionCenters(
        SeededRng rng,
        int worldSize,
        GenerationConfig config
    )
    {
        var margin = worldSize / 10;
        var centers = new List<Vector2I>();

        for (var i = 0; i < config.RegionCount; i++)
        {
            var placed = false;
            for (var attempt = 0; attempt < config.MaxPlacementAttempts; attempt++)
            {
                var x = rng.Next(margin, worldSize - margin);
                var y = rng.Next(margin, worldSize - margin);
                var candidate = new Vector2I(x, y);

                var tooClose = false;
                foreach (var existing in centers)
                {
                    if (candidate.DistanceTo(existing) < config.MinRegionSeparation)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    centers.Add(candidate);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                var x = rng.Next(margin, worldSize - margin);
                var y = rng.Next(margin, worldSize - margin);
                centers.Add(new Vector2I(x, y));
            }
        }

        return centers;
    }

    private static string GenerateName(SeededRng rng, NamingConfig naming)
    {
        var prefix = rng.NextElement(naming.RegionPrefixes);
        var suffix = rng.NextElement(naming.RegionSuffixes);
        return $"{prefix} {suffix}";
    }
}
