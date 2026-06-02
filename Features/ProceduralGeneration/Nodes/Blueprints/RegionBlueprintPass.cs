using Ouranos.Foundry.Core.Types;
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
    [Export]
    public ProceduralGenerationConfig? ProcGenConfig { get; private set; }

    [Export]
    public NamingConfig? NamingConfig { get; private set; }

    public override void RunPass(WorldBlueprint worldBlueprint)
    {
        if (worldBlueprint is not WorldState worldState)
        {
            GD.PrintErr("RegionBlueprintPass requires a WorldState.");
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

        var naming = NamingConfig ?? config.NamingConfig;
        if (naming is null)
        {
            GD.PushError(
                $"{Name}: No NamingConfig available. Assign it in the inspector or add one to ProceduralGenerationConfig."
            );
            return;
        }
        var genConfig = config.GenerationConfig ?? new GenerationConfig();
        var rng = new SeededRng(worldState.Seed, 1000);

        GD.Print(
            $"Placing {genConfig.RegionCount} regions in {worldState.Size}x{worldState.Size} world..."
        );

        var worldSize = worldState.Size;
        var regionCenters = GenerateRegionCenters(rng, worldSize, genConfig);

        for (var i = 0; i < regionCenters.Count; i++)
        {
            var center = regionCenters[i];
            var biomeDef = rng.NextElement(config.BiomeDefinitions);
            var name = GenerateName(rng, naming);

            var region = new Region(
                Id: Guid.NewGuid(),
                Name: name,
                Center: center,
                BiomeId: biomeDef.Id,
                CityIds: [],
                RoadIds: []
            );

            worldState.Regions.Add(region);
        }

        GD.Print($"Placed {worldState.Regions.Count} regions.");
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
