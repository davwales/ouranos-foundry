using System.Collections.Generic;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Blueprints;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Generation;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration;

/// <summary>
/// Feeds each inspector edge value through the real blueprint and chunk passes: generation
/// either completes, or fails through <c>BlueprintFailed</c> with a message naming the
/// field before any pass runs. Nothing may throw.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class ConfigEdgeCaseTests
{
    [TestCase("RoadWaypointJitter=0", "")]
    [TestCase("Radius=1", "")]
    [TestCase("Radius=0", "")]
    [TestCase("Size=1", "")]
    [TestCase("Size=5", "")]
    [TestCase("MinCities=MaxCities", "")]
    [TestCase("MinCities=-1", "")]
    [TestCase("MaxPlacementAttempts=0", "")]
    [TestCase("TerrainVariantCount=0", "")]
    [TestCase("EmptyOptionalTables", "")]
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
    public async Task Generate_WithEdgeValue_CompletesOrReportsTheField(
        string edgeCase,
        string expectedError
    )
    {
        // Arrange
        var (root, generator, manager) = CreateGenerator(edgeCase);
        var blueprintFailures = new List<(string PassName, string Error)>();
        var chunkFailures = new List<string>();
        void OnBlueprintFailed(string passName, string error)
        {
            blueprintFailures.Add((passName, error));
        }

        void OnGenerationFailed(Vector2I coords, string error)
        {
            chunkFailures.Add(error);
        }

        generator.BlueprintFailed += OnBlueprintFailed;
        generator.GenerationFailed += OnGenerationFailed;

        // Act
        Should.NotThrow(() => generator._Ready());
        var world = generator.WorldState;
        if (generator.IsBlueprintGenerated)
        {
            // Generate the chunk under the first city, so every chunk pass has work to do.
            var city = world.Cities[0].Position;
            var coords = ChunkMath.WorldToChunk(city, world.ChunkSizeTiles);
            manager.Player!.Position = new Vector2(city.X, city.Y);
            manager._Process(0);
            await TestWait.Until(() => generator.IsChunkReady(coords) || chunkFailures.Count > 0);
        }

        // Assert
        if (expectedError == "")
        {
            blueprintFailures.ShouldBeEmpty();
            world.Cities.ShouldNotBeEmpty();
            world.Cities.ShouldAllBe(c =>
                c.Position.X >= 0
                && c.Position.Y >= 0
                && c.Position.X < world.Size
                && c.Position.Y < world.Size
            );
            chunkFailures.ShouldBeEmpty();
        }
        else
        {
            blueprintFailures.Count.ShouldBe(1);
            blueprintFailures[0].PassName.ShouldBe(nameof(WorldGenerator));
            blueprintFailures[0].Error.ShouldBe(expectedError);
            world.Regions.ShouldBeEmpty();
        }

        generator.BlueprintFailed -= OnBlueprintFailed;
        generator.GenerationFailed -= OnGenerationFailed;
        root.Free();
    }

    private static (Node root, WorldGenerator generator, ChunkManager manager) CreateGenerator(
        string edgeCase
    )
    {
        var config = ConfigEdgeCases.MakeValidConfig();
        var world = new WorldState { Seed = 1234 };
        ConfigEdgeCases.Apply(edgeCase, config, world);

        var root = new Node();
        var player = new Node2D();
        var manager = new ChunkManager().With(ChunkManager.PropertyName.Player, player);

        var blueprintPipeline = new BlueprintPipeline();
        blueprintPipeline.AddPass<RegionBlueprintPass>();
        blueprintPipeline.AddPass<CityBlueprintPass>();
        blueprintPipeline.AddPass<RoadBlueprintPass>();

        var generationPipeline = new GenerationPipeline();
        generationPipeline.AddChild(new TerrainGenerationPass());
        generationPipeline.AddChild(new CityGenerationPass());
        generationPipeline.AddChild(new RoadGenerationPass());
        generationPipeline.AddChild(new PoiPlacementPass());
        // The tree isn't running, so cache the passes the way _Ready would.
        generationPipeline._Ready();

        var generator = new WorldGenerator()
            .With(WorldGenerator.PropertyName.Config, config)
            .With(WorldGenerator.PropertyName.WorldState, world)
            .With(WorldGenerator.PropertyName.ChunkManager, manager)
            .With(WorldGenerator.PropertyName.BlueprintPipeline, blueprintPipeline)
            .With(WorldGenerator.PropertyName.GenerationPipeline, generationPipeline);

        root.AddChild(player);
        root.AddChild(manager);
        root.AddChild(blueprintPipeline);
        root.AddChild(generationPipeline);
        root.AddChild(generator);
        return (root, generator, manager);
    }
}
