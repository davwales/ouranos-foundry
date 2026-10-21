using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Blueprints;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Generation;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

[TestSuite]
[RequireGodotRuntime]
public class WorldGeneratorTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private static ProceduralGenerationConfig MakeConfig(ChunkConfig chunkConfig)
    {
        return new()
        {
            ChunkConfig = chunkConfig,
            NamingConfig = new NamingConfig(),
            GenerationConfig = new GenerationConfig(),
            BiomeDefinitions = [BiomeDefinition.Default],
            CitySizeDefinitions = [CitySizeDefinition.Default],
            PoiTypeDefinitions = [PoiTypeDefinition.Default],
            RoadTypeDefinitions = [RoadTypeDefinition.Default],
        };
    }

    private static (Node root, WorldGenerator generator, ChunkManager manager) CreateGenerator(
        ChunkConfig chunkConfig,
        Vector2 playerPosition,
        BlueprintPipeline? blueprintPipeline = null,
        GenerationPipeline? generationPipeline = null,
        int maxConcurrentChunkJobs = 4
    )
    {
        var root = new Node();
        var player = new Node2D { Position = playerPosition };
        var manager = new ChunkManager().With(ChunkManager.PropertyName.Player, player);
        var generator = new WorldGenerator()
            .With(WorldGenerator.PropertyName.Config, MakeConfig(chunkConfig))
            .With(WorldGenerator.PropertyName.ChunkManager, manager)
            .With(WorldGenerator.PropertyName.MaxConcurrentChunkJobs, maxConcurrentChunkJobs);
        root.AddChild(player);
        root.AddChild(manager);
        root.AddChild(generator);

        if (blueprintPipeline is not null)
        {
            generator.With(WorldGenerator.PropertyName.BlueprintPipeline, blueprintPipeline);
            root.AddChild(blueprintPipeline);
        }

        if (generationPipeline is not null)
        {
            generator.With(WorldGenerator.PropertyName.GenerationPipeline, generationPipeline);
            root.AddChild(generationPipeline);
            // The tree isn't running, so cache the passes the way _Ready would.
            generationPipeline._Ready();
        }

        return (root, generator, manager);
    }

    private static GenerationPipeline MakeGenerationPipeline(params GenerationPass[] passes)
    {
        var pipeline = new GenerationPipeline();
        foreach (var pass in passes)
        {
            pipeline.AddChild(pass);
        }

        return pipeline;
    }

    private static BlueprintPipeline MakeRealBlueprintPipeline()
    {
        var pipeline = new BlueprintPipeline();
        pipeline.AddPass<RegionBlueprintPass>();
        pipeline.AddPass<CityBlueprintPass>();
        pipeline.AddPass<RoadBlueprintPass>();
        return pipeline;
    }

    [TestCase]
    public async Task Ready_WithChunkSizeTiles32_UsesItEverywhere()
    {
        // Arrange
        var chunkConfig = new ChunkConfig
        {
            ChunkSizeTiles = 32,
            TilePixelSize = 2,
            ViewDistance = 0,
        };
        // 64 px per chunk: x -1 is chunk -1, y 130 is chunk 2.
        var (root, generator, manager) = CreateGenerator(chunkConfig, new Vector2(-1, 130));
        var requested = new List<Vector2I>();
        void OnRequested(Vector2I coords)
        {
            requested.Add(coords);
        }

        manager.RequestChunkGeneration += OnRequested;

        // Act
        generator._Ready();
        var city = new City(Guid.NewGuid(), "Test", new(40, 70), "town", Guid.Empty, [], [], 5);
        generator.WorldState.Cities.Add(city);
        generator.WorldState.RebuildSpatialIndex();

        manager._Process(0);
        // Not ToSignal: its continuation runs inside the emit, and freeing the emitter there errors.
        await TestWait.Until(() => generator.IsChunkReady(new Vector2I(-1, 2)));

        // Assert
        generator.WorldState.ChunkSizeTiles.ShouldBe(32);
        generator.WorldState.ChunkToCityIds.Keys.ShouldBe(new List<Vector2I> { new(1, 2) });
        requested.ShouldBe(new List<Vector2I> { new(-1, 2) });

        var chunk = generator.GetChunk(new Vector2I(-1, 2)).ShouldNotBeNull();
        chunk.Descriptor.ChunkSizeTiles.ShouldBe(32);
        chunk.TerrainData.Length.ShouldBe(32 * 32);

        manager.RequestChunkGeneration -= OnRequested;
        root.Free();
    }

    [TestCase]
    public async Task UnloadChunk_WhileGenerating_DiscardsTheChunk()
    {
        // Arrange
        var gate = new GatedTestPass();
        var after = new GatedTestPass();
        after.Release.Set();
        var (root, generator, manager) = CreateGenerator(
            new ChunkConfig { ViewDistance = 0 },
            Vector2.Zero,
            generationPipeline: MakeGenerationPipeline(gate, after),
            maxConcurrentChunkJobs: 1
        );
        var generated = new List<Vector2I>();
        var failures = new List<string>();
        void OnGenerated(Vector2I coords, ChunkData chunk)
        {
            generated.Add(coords);
        }

        void OnFailed(Vector2I coords, string error)
        {
            failures.Add(error);
        }

        generator.ChunkGenerated += OnGenerated;
        generator.GenerationFailed += OnFailed;
        generator._Ready();

        // Act
        manager._Process(0);
        gate.Entered.Wait(WaitTimeout).ShouldBeTrue();
        generator.UnloadChunk(Vector2I.Zero);
        gate.Release.Set();

        // With one job slot, chunk (1, 0) only starts once the unloaded job has finished.
        manager.Player!.Position = new Vector2(16, 0);
        manager._Process(0);
        await TestWait.Until(() => generated.Count > 0);

        // Assert
        generated.ShouldBe(new List<Vector2I> { new(1, 0) });
        generator.IsChunkReady(Vector2I.Zero).ShouldBeFalse();
        failures.ShouldBeEmpty();
        after.EnteredCount.ShouldBe(1);

        generator.ChunkGenerated -= OnGenerated;
        generator.GenerationFailed -= OnFailed;
        root.Free();
    }

    [TestCase]
    public async Task MaxConcurrentChunkJobs_WhenOne_RunsOneJobAtATime()
    {
        // Arrange
        var gate = new GatedTestPass();
        var (root, generator, manager) = CreateGenerator(
            new ChunkConfig { ViewDistance = 1 },
            Vector2.Zero,
            generationPipeline: MakeGenerationPipeline(gate),
            maxConcurrentChunkJobs: 1
        );
        var generated = new List<Vector2I>();
        void OnGenerated(Vector2I coords, ChunkData chunk)
        {
            generated.Add(coords);
        }

        generator.ChunkGenerated += OnGenerated;
        generator._Ready();

        // Act
        manager._Process(0);
        gate.Entered.Wait(WaitTimeout).ShouldBeTrue();
        var enteredWhileGated = gate.EnteredCount;
        gate.Release.Set();
        await TestWait.Until(() => generated.Count == 9);

        // Assert
        enteredWhileGated.ShouldBe(1);
        gate.EnteredCount.ShouldBe(9);
        gate.MaxConcurrentRuns.ShouldBe(1);

        generator.ChunkGenerated -= OnGenerated;
        root.Free();
    }

    [TestCase]
    public async Task ResetBlueprint_ThenGenerateBlueprint_RegeneratesSameCountsAndChunks()
    {
        // Arrange
        var chunkConfig = new ChunkConfig { ViewDistance = 0 };
        var (root, generator, manager) = CreateGenerator(
            chunkConfig,
            Vector2.Zero,
            blueprintPipeline: MakeRealBlueprintPipeline()
        );
        var generated = new List<Vector2I>();
        var unloaded = new List<Vector2I>();
        void OnGenerated(Vector2I coords, ChunkData chunk)
        {
            generated.Add(coords);
        }

        void OnUnloaded(Vector2I coords)
        {
            unloaded.Add(coords);
        }

        generator.ChunkGenerated += OnGenerated;
        generator.ChunkUnloaded += OnUnloaded;
        generator._Ready();
        var world = generator.WorldState;
        var counts = (world.Regions.Count, world.Cities.Count, world.Roads.Count);
        manager._Process(0);
        await TestWait.Until(() => generated.Count == 1);

        // Act
        generator.ResetBlueprint();
        var resetState = (generator.IsBlueprintGenerated, generator.WorldState.Cities.Count);
        var regenerated = generator.GenerateBlueprint();
        var newWorld = generator.WorldState;
        manager._Process(0);
        await TestWait.Until(() => generated.Count == 2);

        // Assert
        counts.Item2.ShouldBeGreaterThan(0);
        unloaded.ShouldBe(new List<Vector2I> { Vector2I.Zero });
        resetState.ShouldBe((false, 0));
        regenerated.ShouldBeTrue();
        newWorld.ShouldNotBeSameAs(world);
        (newWorld.Regions.Count, newWorld.Cities.Count, newWorld.Roads.Count).ShouldBe(counts);
        newWorld.CityById.Count.ShouldBe(counts.Item2);
        generated.ShouldBe(new List<Vector2I> { Vector2I.Zero, Vector2I.Zero });

        generator.ChunkGenerated -= OnGenerated;
        generator.ChunkUnloaded -= OnUnloaded;
        root.Free();
    }

    [TestCase]
    public async Task ResetBlueprint_WhileAChunkIsGenerating_LeavesItsWorldUntouchedAndDropsIt()
    {
        // Arrange
        var gate = new GatedTestPass();
        var (root, generator, manager) = CreateGenerator(
            new ChunkConfig { ViewDistance = 0 },
            Vector2.Zero,
            blueprintPipeline: MakeRealBlueprintPipeline(),
            generationPipeline: MakeGenerationPipeline(gate),
            maxConcurrentChunkJobs: 1
        );
        var generated = new List<Vector2I>();
        void OnGenerated(Vector2I coords, ChunkData chunk)
        {
            generated.Add(coords);
        }

        generator.ChunkGenerated += OnGenerated;
        generator._Ready();
        var oldWorld = generator.WorldState;
        var oldCityCount = oldWorld.Cities.Count;
        manager._Process(0);
        gate.Entered.Wait(WaitTimeout).ShouldBeTrue();

        // Act
        generator.ResetBlueprint();
        generator.GenerateBlueprint();
        manager._Process(0);
        gate.Release.Set();
        // With one job slot, the second job starts only once the first has been handled.
        await TestWait.Until(() => generated.Count > 0);

        // Assert
        oldCityCount.ShouldBeGreaterThan(0);
        gate.Runs.Count.ShouldBe(2);
        gate.Runs[0].ShouldBe((oldWorld, oldCityCount, oldCityCount));
        oldWorld.Cities.Count.ShouldBe(oldCityCount);
        generator.WorldState.ShouldNotBeSameAs(oldWorld);
        gate.Runs[1].World.ShouldBeSameAs(generator.WorldState);
        generated.ShouldBe(new List<Vector2I> { Vector2I.Zero });

        generator.ChunkGenerated -= OnGenerated;
        root.Free();
    }

    [TestCase]
    public void GenerateBlueprint_WhenAPassThrows_StopsAndReportsFailure()
    {
        // Arrange
        var counting = new CountingBlueprintPass();
        var pipeline = new BlueprintPipeline();
        pipeline.AddPass(new ThrowingBlueprintPass());
        pipeline.AddPass(counting);
        var (root, generator, _) = CreateGenerator(
            new ChunkConfig(),
            Vector2.Zero,
            blueprintPipeline: pipeline
        );
        var failures = new List<(string PassName, string Error)>();
        var generatedCount = 0;
        void OnFailed(string passName, string error)
        {
            failures.Add((passName, error));
        }

        void OnGenerated()
        {
            generatedCount++;
        }

        generator.BlueprintFailed += OnFailed;
        generator.BlueprintGenerated += OnGenerated;

        // Act
        var result = generator.GenerateBlueprint();

        // Assert
        result.ShouldBeFalse();
        failures.ShouldBe([(nameof(ThrowingBlueprintPass), ThrowingBlueprintPass.Message)]);
        generatedCount.ShouldBe(0);
        counting.RunCount.ShouldBe(0);
        generator.IsBlueprintGenerated.ShouldBeFalse();

        generator.BlueprintFailed -= OnFailed;
        generator.BlueprintGenerated -= OnGenerated;
        root.Free();
    }

    [TestCase]
    public void GenerateBlueprint_WhenPassesAreMisordered_ReturnsFalseWithoutThrowing()
    {
        // Arrange
        var pipeline = new BlueprintPipeline();
        pipeline.AddPass<CityBlueprintPass>();
        pipeline.AddPass<RegionBlueprintPass>();
        var (root, generator, _) = CreateGenerator(
            new ChunkConfig(),
            Vector2.Zero,
            blueprintPipeline: pipeline
        );
        var passFailures = new List<string>();
        var blueprintFailures = new List<string>();
        void OnPassFailed(string passName, string error)
        {
            passFailures.Add(error);
        }

        void OnBlueprintFailed(string passName, string error)
        {
            blueprintFailures.Add(error);
        }

        pipeline.PassFailed += OnPassFailed;
        generator.BlueprintFailed += OnBlueprintFailed;

        // Act
        var result = Should.NotThrow(() => generator.GenerateBlueprint());

        // Assert
        result.ShouldBeFalse();
        passFailures.Count.ShouldBe(1);
        passFailures[0].ShouldContain("CityBlueprintPass requires RegionBlueprintPass");
        blueprintFailures.ShouldBe(passFailures);
        generator.WorldState.Regions.ShouldBeEmpty();

        pipeline.PassFailed -= OnPassFailed;
        generator.BlueprintFailed -= OnBlueprintFailed;
        root.Free();
    }

    [TestCase]
    public void GenerateBlueprint_WhenConfigIsMissing_ReportsFailure()
    {
        // Arrange
        var generator = new WorldGenerator();
        var failures = new List<string>();
        void OnFailed(string passName, string error)
        {
            failures.Add(passName);
        }

        generator.BlueprintFailed += OnFailed;

        // Act
        var result = generator.GenerateBlueprint();

        // Assert
        result.ShouldBeFalse();
        failures.ShouldBe([nameof(WorldGenerator)]);
        generator.IsBlueprintGenerated.ShouldBeFalse();

        generator.BlueprintFailed -= OnFailed;
        generator.Free();
    }

    [TestCase]
    public async Task ChunkGeneration_WhenAPassMissesItsDependency_FailsWithoutGenerating()
    {
        // Arrange
        var first = new GatedTestPass();
        first.Release.Set();
        var (root, generator, manager) = CreateGenerator(
            new ChunkConfig { ViewDistance = 0 },
            Vector2.Zero,
            generationPipeline: MakeGenerationPipeline(first, new CityGenerationPass())
        );
        var generated = new List<Vector2I>();
        var failures = new List<string>();
        void OnGenerated(Vector2I coords, ChunkData chunk)
        {
            generated.Add(coords);
        }

        void OnFailed(Vector2I coords, string error)
        {
            failures.Add(error);
        }

        generator.ChunkGenerated += OnGenerated;
        generator.GenerationFailed += OnFailed;
        generator._Ready();

        // Act
        manager._Process(0);
        await TestWait.Until(() => failures.Count > 0);

        // Assert
        failures.Count.ShouldBe(1);
        failures[0].ShouldContain("CityGenerationPass requires TerrainGenerationPass");
        generated.ShouldBeEmpty();
        generator.IsChunkReady(Vector2I.Zero).ShouldBeFalse();
        first.EnteredCount.ShouldBe(0);

        generator.ChunkGenerated -= OnGenerated;
        generator.GenerationFailed -= OnFailed;
        root.Free();
    }
}
