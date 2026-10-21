using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Resources;

/// <summary>
/// <c>Validate()</c> on the config, its sub-configs and definitions, and on
/// <c>WorldState</c>, through the aggregate the generator calls.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class ProceduralGenerationConfigTests
{
    [TestCase]
    public void Validate_WithValidConfig_ReturnsNothing()
    {
        // Arrange
        var config = ConfigEdgeCases.MakeValidConfig();

        // Act
        var messages = config.Validate().ToList();

        // Assert
        messages.ShouldBeEmpty();
        new WorldState().Validate().ShouldBeEmpty();
    }

    [TestCase]
    public void Validate_WhenSubConfigsAreUnset_ReturnsNothing()
    {
        // Arrange
        var config = new ProceduralGenerationConfig { BiomeDefinitions = [new BiomeDefinition()] };

        // Act
        var messages = config.Validate().ToList();

        // Assert
        messages.ShouldBeEmpty();
    }

    [TestCase("RegionCount=0", "GenerationConfig.RegionCount (0) must be at least 1.")]
    [TestCase(
        "MinCities>MaxCities",
        "GenerationConfig.MinCitiesPerRegion (5) is greater than MaxCitiesPerRegion (3)."
    )]
    [TestCase(
        "CityPositionAttempts=0",
        "GenerationConfig.CityPositionAttempts (0) must be at least 1."
    )]
    [TestCase(
        "RoadWaypointSpacing=0",
        "GenerationConfig.RoadWaypointSpacing (0) must be at least 1."
    )]
    [TestCase(
        "RoadWaypointJitter=-1",
        "GenerationConfig.RoadWaypointJitter (-1) must be at least 0."
    )]
    [TestCase("Size=0", "WorldState.Size (0) must be at least 1.")]
    [TestCase("EmptyRegionPrefixes", "NamingConfig.RegionPrefixes is empty.")]
    [TestCase("EmptyCitySuffixes", "NamingConfig.CitySuffixes is empty.")]
    [TestCase("EmptyNameOptions", "PoiTypeDefinition 'inn'.NameOptions is empty.")]
    [TestCase("Radius=-2", "CitySizeDefinition 'village'.Radius (-2) must be at least 0.")]
    [TestCase(
        "TerrainVariantCount=-1",
        "BiomeDefinition 'plains'.TerrainVariantCount (-1) must be at least 0."
    )]
    [TestCase(
        "EmptyDistributionEntry",
        "BiomeDefinition 'plains'.CitySizeDistribution.Entries[0] is empty."
    )]
    [TestCase("NoBiomes", "ProceduralGenerationConfig.BiomeDefinitions is empty.")]
    [TestCase("EmptyBiomeSlot", "ProceduralGenerationConfig.BiomeDefinitions[1] is empty.")]
    [TestCase("EmptyRoadTypeSlot", "ProceduralGenerationConfig.RoadTypeDefinitions[2] is empty.")]
    [TestCase("ChunkSizeTiles=0", "ChunkConfig.ChunkSizeTiles (0) must be at least 1.")]
    [TestCase("TilePixelSize=0", "ChunkConfig.TilePixelSize (0) must be at least 1.")]
    [TestCase("ViewDistance=-1", "ChunkConfig.ViewDistance (-1) must be at least 0.")]
    [TestCase("StreetGridSpacing=0", "TerrainConfig.StreetGridSpacing (0) must be at least 1.")]
    public void Validate_WithInvalidValue_ReturnsOneMessageNamingTheField(
        string edgeCase,
        string expected
    )
    {
        // Arrange
        var config = ConfigEdgeCases.MakeValidConfig();
        var world = new WorldState();
        ConfigEdgeCases.Apply(edgeCase, config, world);

        // Act
        List<string> messages = [.. config.Validate(), .. world.Validate()];

        // Assert
        messages.ShouldBe([expected]);
    }

    [TestCase]
    public void GetWarnings_WithEmptyTablesAndUnknownIds_ListsEach()
    {
        // Arrange
        var config = ConfigEdgeCases.MakeValidConfig();
        config.PoiTypeDefinitions.Clear();
        config.HubRoadTypeId = "canal";

        // Act
        var warnings = config.GetWarnings().ToList();

        // Assert
        warnings.ShouldBe([
            "ProceduralGenerationConfig.PoiTypeDefinitions is empty, so cities get no points of interest.",
            "ProceduralGenerationConfig refers to unknown road type 'canal'. The default is used.",
        ]);
    }

    [TestCase]
    public void GetWarnings_WithValidConfig_ReturnsNothing()
    {
        // Arrange
        var config = ConfigEdgeCases.MakeValidConfig();

        // Act
        var warnings = config.GetWarnings().ToList();

        // Assert
        warnings.ShouldBeEmpty();
    }
}
