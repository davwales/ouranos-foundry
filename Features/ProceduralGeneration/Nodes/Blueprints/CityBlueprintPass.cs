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
/// Places cities within each region based on biome and region size.
/// Cities are assigned sizes proportional to their region and given procedurally
/// generated names. Must run after RegionBlueprintPass.
/// </summary>
[RequiresPasses(typeof(RegionBlueprintPass))]
[GlobalClass]
public partial class CityBlueprintPass : BlueprintPass
{
    public override ulong RngSalt => 2000;

    public override void RunPass(GenerationContext ctx)
    {
        var (definitions, worldState, rng) = (ctx.Definitions, ctx.World, ctx.Rng);
        if (worldState.Regions.Count == 0)
        {
            throw new InvalidOperationException(
                $"{GetType().Name}: No regions found. Run RegionBlueprintPass first."
            );
        }

        var naming = definitions.Naming;
        var genConfig = definitions.Generation;
        var cityCount = 0;

        foreach (var region in worldState.Regions)
        {
            var cityCountInRegion = GetCityCountForRegion(rng, region, definitions, genConfig);
            var placedCities = new List<Vector2I>();

            for (var i = 0; i < cityCountInRegion; i++)
            {
                var position = FindCityPosition(rng, region, placedCities, worldState, genConfig);
                if (position is null)
                {
                    continue;
                }

                var city = CreateCity(rng, i, position.Value, region, definitions, naming);
                worldState.Cities.Add(city);
                region.CityIds.Add(city.Id);
                placedCities.Add(position.Value);
                cityCount++;
            }
        }

        worldState.RebuildLookups();
        ctx.Log($"Placed {cityCount} cities across {worldState.Regions.Count} regions.");
    }

    private static int GetCityCountForRegion(
        SeededRng rng,
        Region region,
        GenerationDefinitions definitions,
        GenerationConfig genConfig
    )
    {
        var baseCount = rng.Next(genConfig.MinCitiesPerRegion, genConfig.MaxCitiesPerRegion + 1);
        var biomeDef = definitions.GetBiome(region.BiomeId);
        return Math.Max(1, baseCount + biomeDef.CityCountModifier);
    }

    private static Vector2I? FindCityPosition(
        SeededRng rng,
        Region region,
        List<Vector2I> existingCities,
        WorldState worldState,
        GenerationConfig config
    )
    {
        var placeRadius = config.MinRegionSize;
        var worldMax = new Vector2I(worldState.Size - 1, worldState.Size - 1);

        for (var attempt = 0; attempt < config.CityPositionAttempts; attempt++)
        {
            var angle = rng.NextFloat() * Mathf.Tau;
            var distance = rng.NextFloat() * placeRadius;
            var offset = new Vector2I(
                (int)(Mathf.Cos(angle) * distance),
                (int)(Mathf.Sin(angle) * distance)
            );
            // Keep cities inside the world. A city can still land outside its own Voronoi
            // cell when MinRegionSize is large relative to the region spacing.
            var position = (region.Center + offset).Clamp(Vector2I.Zero, worldMax);

            var tooClose = false;
            foreach (var existing in existingCities)
            {
                if (position.DistanceTo(existing) < config.MinCityDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
            {
                continue;
            }

            foreach (var city in worldState.Cities)
            {
                if (position.DistanceTo(city.Position) < config.MinCityDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                return position;
            }
        }

        return null;
    }

    private City CreateCity(
        SeededRng rng,
        int cityIndex,
        Vector2I position,
        Region region,
        GenerationDefinitions definitions,
        NamingConfig naming
    )
    {
        var cityId = StableId.Create(region.Id, "city", (ulong)cityIndex);
        var biomeDef = definitions.GetBiome(region.BiomeId);
        var sizeId =
            biomeDef.CitySizeDistribution?.Sample(rng)
            ?? definitions.CitySizes.FirstOrDefault()?.Id
            ?? "village";
        var sizeDef = definitions.GetCitySize(sizeId);
        var name = GenerateCityName(rng, naming);
        var radius = sizeDef.Radius;
        var cityRng = rng.Derive();

        var pois = GeneratePois(cityRng, cityId, position, sizeDef, definitions);

        return new City(
            Id: cityId,
            Name: name,
            Position: position,
            SizeId: sizeId,
            RegionId: region.Id,
            PointsOfInterest: pois,
            RoadIds: [],
            Radius: radius
        );
    }

    private static string GenerateCityName(SeededRng rng, NamingConfig naming)
    {
        var prefix = rng.NextElement(naming.CityPrefixes);
        var suffix = rng.NextElement(naming.CitySuffixes);
        return $"{prefix}{suffix}";
    }

    private static List<PointOfInterest> GeneratePois(
        SeededRng rng,
        Guid cityId,
        Vector2I cityPosition,
        CitySizeDefinition sizeDef,
        GenerationDefinitions definitions
    )
    {
        var poiCount = sizeDef.PoiCount;
        var poiDefs = definitions.PoiTypes;
        var pois = new List<PointOfInterest>();

        if (poiDefs.Count == 0)
        {
            return pois;
        }

        for (var i = 0; i < poiCount; i++)
        {
            var poiTypeDef = rng.NextElement(poiDefs);
            var offset = new Vector2I(
                rng.Next(-sizeDef.Radius / 2, sizeDef.Radius / 2),
                rng.Next(-sizeDef.Radius / 2, sizeDef.Radius / 2)
            );

            var poiName = rng.NextElement(poiTypeDef.NameOptions);

            pois.Add(
                new PointOfInterest(
                    Id: StableId.Create(cityId, "poi", (ulong)i),
                    Name: poiName,
                    PoiTypeId: poiTypeDef.Id,
                    Position: cityPosition + offset,
                    CityId: cityId
                )
            );
        }

        return pois;
    }
}
