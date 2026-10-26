using System;
using System.IO;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.Health.Nodes;
using Ouranos.Foundry.Features.Persistence.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Blueprints;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Tests.Features.ProceduralGeneration;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Persistence;

/// <summary>
/// Saves features through a <c>SaveManager</c> to disk, then loads them into a fresh scene,
/// as a game does after quitting and starting again.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class SaveRoundTripTests
{
    private const string Slot = "slot1";

    private static (
        Node root,
        SaveManager manager,
        WorldGenerator world,
        HealthComponent health
    ) CreateGame(string directory, ulong seed)
    {
        var config = new ProceduralGenerationConfig
        {
            NamingConfig = new NamingConfig(),
            GenerationConfig = new GenerationConfig(),
            BiomeDefinitions = [BiomeDefinition.Default],
            CitySizeDefinitions = [CitySizeDefinition.Default],
            PoiTypeDefinitions = [PoiTypeDefinition.Default],
            RoadTypeDefinitions = [RoadTypeDefinition.Default],
        };
        var pipeline = new BlueprintPipeline();
        pipeline.AddPass<RegionBlueprintPass>();
        pipeline.AddPass<CityBlueprintPass>();
        pipeline.AddPass<RoadBlueprintPass>();
        var world = new WorldGenerator()
            .With(WorldGenerator.PropertyName.SaveKey, "world")
            .With(WorldGenerator.PropertyName.Config, config)
            .With(WorldGenerator.PropertyName.WorldState, new WorldState { Seed = seed })
            .With(WorldGenerator.PropertyName.BlueprintPipeline, pipeline);
        world.AddChild(pipeline);
        world.AddToGroup(SaveManager.DefaultSaveGroup);

        var health = new HealthComponent().With(HealthComponent.PropertyName.SaveKey, "player");
        health.AddToGroup(SaveManager.DefaultSaveGroup);

        var manager = new SaveManager().With(SaveManager.PropertyName.SaveDirectory, directory);

        var root = new Node();
        root.AddChild(manager);
        root.AddChild(world);
        root.AddChild(health);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(root);
        return (root, manager, world, health);
    }

    [TestCase]
    public void Load_InAFreshGame_RestoresTheWorldBlueprintAndHealth()
    {
        // Arrange
        var directory = $"user://save_round_trip_tests/{Guid.NewGuid():N}";
        var (firstRoot, firstManager, firstWorld, firstHealth) = CreateGame(directory, 1234);
        firstHealth.Damage(35f);
        firstManager.Save(Slot).ShouldBeTrue();
        var expectedWorld = BlueprintDeterminismTests.Describe(firstWorld.WorldState);
        firstRoot.Free();

        var (root, manager, world, health) = CreateGame(directory, 5678);
        var freshWorld = BlueprintDeterminismTests.Describe(world.WorldState);

        // Act
        var loaded = manager.Load(Slot);

        // Assert
        loaded.ShouldBeTrue();
        expectedWorld.ShouldNotBeEmpty();
        freshWorld.ShouldNotBe(expectedWorld);
        BlueprintDeterminismTests.Describe(world.WorldState).ShouldBe(expectedWorld);
        world.WorldState.Seed.ShouldBe(1234UL);
        world.IsBlueprintGenerated.ShouldBeTrue();
        health.CurrentHealth.ShouldBe(65f);

        root.Free();
        Directory.Delete(ProjectSettings.GlobalizePath(directory), recursive: true);
    }
}
