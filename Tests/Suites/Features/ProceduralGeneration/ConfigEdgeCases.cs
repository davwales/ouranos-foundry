using System;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration;

/// <summary>
/// A valid config, and the inspector edge values from issue #13 (plus others found since)
/// that the validation and edge-case suites apply to it by name.
/// </summary>
internal static class ConfigEdgeCases
{
    /// <summary>A valid config built from fresh instances, so a case never edits a Default.</summary>
    public static ProceduralGenerationConfig MakeValidConfig()
    {
        return new()
        {
            ChunkConfig = new ChunkConfig { ViewDistance = 0 },
            TerrainConfig = new TerrainConfig(),
            NamingConfig = new NamingConfig(),
            GenerationConfig = new GenerationConfig(),
            BiomeDefinitions = [new BiomeDefinition { Id = "plains" }],
            CitySizeDefinitions = [new CitySizeDefinition { Id = "village" }],
            PoiTypeDefinitions = [new PoiTypeDefinition { Id = "inn", NameOptions = ["Inn"] }],
            RoadTypeDefinitions =
            [
                new RoadTypeDefinition { Id = "dirt" },
                new RoadTypeDefinition { Id = "highway" },
            ],
        };
    }

    /// <summary>Applies the named edge value to <paramref name="config"/> or <paramref name="world"/>.</summary>
    public static void Apply(string edgeCase, ProceduralGenerationConfig config, WorldState world)
    {
        var generation = config.GenerationConfig!;
        var naming = config.NamingConfig!;
        var chunks = config.ChunkConfig!;
        var biome = config.BiomeDefinitions[0];
        var citySize = config.CitySizeDefinitions[0];
        switch (edgeCase)
        {
            // Degenerate values that now generate.
            case "RoadWaypointJitter=0":
                generation.RoadWaypointJitter = 0;
                break;
            case "Radius=1":
                citySize.Radius = 1;
                break;
            case "Radius=0":
                citySize.Radius = 0;
                break;
            case "Size=1":
                world.Size = 1;
                break;
            case "Size=5":
                world.Size = 5;
                break;
            case "MinCities=MaxCities":
                generation.MinCitiesPerRegion = 2;
                generation.MaxCitiesPerRegion = 2;
                break;
            case "MinCities=-1":
                generation.MinCitiesPerRegion = -1;
                break;
            case "MaxPlacementAttempts=0":
                generation.MaxPlacementAttempts = 0;
                break;
            case "TerrainVariantCount=0":
                biome.TerrainVariantCount = 0;
                break;
            case "EmptyOptionalTables":
                config.CitySizeDefinitions.Clear();
                config.PoiTypeDefinitions.Clear();
                config.RoadTypeDefinitions.Clear();
                break;

            // Values that validation rejects.
            case "RegionCount=0":
                generation.RegionCount = 0;
                break;
            case "MinCities>MaxCities":
                generation.MinCitiesPerRegion = 5;
                generation.MaxCitiesPerRegion = 3;
                break;
            case "CityPositionAttempts=0":
                generation.CityPositionAttempts = 0;
                break;
            case "RoadWaypointSpacing=0":
                generation.RoadWaypointSpacing = 0;
                break;
            case "RoadWaypointJitter=-1":
                generation.RoadWaypointJitter = -1;
                break;
            case "Size=0":
                world.Size = 0;
                break;
            case "EmptyRegionPrefixes":
                naming.RegionPrefixes = [];
                break;
            case "EmptyCitySuffixes":
                naming.CitySuffixes = [];
                break;
            case "EmptyNameOptions":
                config.PoiTypeDefinitions[0].NameOptions = [];
                break;
            case "Radius=-2":
                citySize.Radius = -2;
                break;
            case "TerrainVariantCount=-1":
                biome.TerrainVariantCount = -1;
                break;
            case "EmptyDistributionEntry":
                biome.CitySizeDistribution = new CitySizeDistribution { Entries = [null!] };
                break;
            case "NoBiomes":
                config.BiomeDefinitions.Clear();
                break;
            case "EmptyBiomeSlot":
                config.BiomeDefinitions.Add(null!);
                break;
            case "EmptyRoadTypeSlot":
                config.RoadTypeDefinitions.Add(null!);
                break;
            case "ChunkSizeTiles=0":
                chunks.ChunkSizeTiles = 0;
                break;
            case "TilePixelSize=0":
                chunks.TilePixelSize = 0;
                break;
            case "ViewDistance=-1":
                chunks.ViewDistance = -1;
                break;
            case "StreetGridSpacing=0":
                config.TerrainConfig!.StreetGridSpacing = 0;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(edgeCase), edgeCase, null);
        }
    }
}
