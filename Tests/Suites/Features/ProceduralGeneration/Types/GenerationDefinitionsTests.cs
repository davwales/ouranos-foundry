using GdUnit4;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Types;

[TestSuite]
[RequireGodotRuntime]
public class GenerationDefinitionsTests
{
    private static ProceduralGenerationConfig MakeConfig()
    {
        return new()
        {
            BiomeDefinitions = [new BiomeDefinition { Id = "Forest", TerrainId = 4 }],
            CitySizeDefinitions = [new CitySizeDefinition { Id = "Town" }],
            PoiTypeDefinitions = [new PoiTypeDefinition { Id = "Tavern" }],
            RoadTypeDefinitions =
            [
                new RoadTypeDefinition { Id = "Highway" },
                new RoadTypeDefinition { Id = "Dirt" },
            ],
        };
    }

    [TestCase]
    public void Get_WithIdInAnotherCase_ReturnsTheDefinition()
    {
        // Arrange
        var config = MakeConfig();

        // Act
        var definitions = new GenerationDefinitions(config);

        // Assert
        definitions.GetBiome("forest").ShouldBeSameAs(config.BiomeDefinitions[0]);
        definitions.GetCitySize("TOWN").ShouldBeSameAs(config.CitySizeDefinitions[0]);
        definitions.GetPoiType("tavern").ShouldBeSameAs(config.PoiTypeDefinitions[0]);
        definitions.GetRoadType("highway").ShouldBeSameAs(config.RoadTypeDefinitions[0]);
    }

    [TestCase]
    public void Get_WithUnknownId_ReturnsTheDefault()
    {
        // Arrange
        var definitions = new GenerationDefinitions(MakeConfig());

        // Act & Assert
        definitions.GetBiome("lava").ShouldBeSameAs(BiomeDefinition.Default);
        definitions.GetCitySize("lava").ShouldBeSameAs(CitySizeDefinition.Default);
        definitions.GetPoiType("lava").ShouldBeSameAs(PoiTypeDefinition.Default);
        definitions.GetRoadType("lava").ShouldBeSameAs(RoadTypeDefinition.Default);
    }

    [TestCase]
    public void UnknownIds_WhenTheConfigRefersToMissingDefinitions_ListsThem()
    {
        // Arrange
        var config = MakeConfig();
        config.HubRoadTypeId = "canal";
        config.BiomeDefinitions[0].CitySizeDistribution = new CitySizeDistribution
        {
            Entries =
            [
                new WeightedSizeEntry { SizeId = "town" },
                new WeightedSizeEntry { SizeId = "megacity" },
            ],
        };

        // Act
        var definitions = new GenerationDefinitions(config);

        // Assert
        definitions.UnknownIds.ShouldBe(["road type 'canal'", "city size 'megacity'"]);
    }

    [TestCase]
    public void Snapshot_WhenTheConfigTablesChangeLater_KeepsTheOriginalDefinitions()
    {
        // Arrange
        var config = MakeConfig();
        var forest = config.BiomeDefinitions[0];
        var definitions = new GenerationDefinitions(config);

        // Act
        config.BiomeDefinitions.Clear();
        config.BiomeDefinitions.Add(new BiomeDefinition { Id = "Forest", TerrainId = 9 });

        // Assert
        definitions.GetBiome("forest").ShouldBeSameAs(forest);
        definitions.Biomes.ShouldBe([forest]);
    }
}
