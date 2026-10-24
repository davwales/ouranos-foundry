using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
/// Pins the blueprint and chunk output of the built-in passes for a fixed seed, so a
/// refactor can prove it changes nothing. When a change is meant to alter the output,
/// replace <see cref="ExpectedDigest"/> with the digest printed in the failure message.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class GoldenOutputTests
{
    private const ulong Seed = 1234;
    private const string ExpectedDigest =
        "0c162c043d5443f76b561ed2146f206616be610a82328f6a09071cd7605306aa";

    [TestCase]
    public async Task Generate_WithFixedSeed_MatchesGoldenDigest()
    {
        // Arrange
        var (root, generator, manager) = CreateGenerator();

        // Act
        generator._Ready();
        var lines = BlueprintDeterminismTests.Describe(generator.WorldState);
        foreach (var coords in ChunksToDescribe(generator.WorldState))
        {
            manager.Player!.Position = new Vector2(coords.X * 16 + 8, coords.Y * 16 + 8);
            manager._Process(0);
            await TestWait.Until(() => generator.IsChunkReady(coords));
            lines.Add(DescribeChunk(generator.GetChunk(coords)!));
        }

        var digest = Digest(lines);

        // Assert
        lines.ShouldContain(line => line.StartsWith("chunk (-1, -1)"));
        lines.ShouldContain(line => line.StartsWith("chunk") && !line.Contains("Wilderness"));
        digest.ShouldBe(
            ExpectedDigest,
            $"Procgen output changed for seed {Seed}. If that is intended, set "
                + $"{nameof(GoldenOutputTests)}.{nameof(ExpectedDigest)} to \"{digest}\"."
        );

        root.Free();
    }

    // A negative chunk, the origin, and chunks under the first city and the first road.
    private static List<Vector2I> ChunksToDescribe(WorldState world)
    {
        List<Vector2I> chunks =
        [
            new(-1, -1),
            Vector2I.Zero,
            ChunkMath.WorldToChunk(world.Cities[0].Position, world.ChunkSizeTiles),
            ChunkMath.WorldToChunk(world.Roads[0].PathPoints[1], world.ChunkSizeTiles),
        ];
        return [.. chunks.Distinct()];
    }

    private static string DescribeChunk(ChunkData chunk)
    {
        return $"chunk {chunk.Coordinates} {chunk.Type} "
            + $"[{string.Join(",", chunk.CityIds.Order())}] "
            + $"[{string.Join(",", chunk.RoadIds.Order())}] "
            + string.Join(",", chunk.TerrainData);
    }

    private static string Digest(IEnumerable<string> lines)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", lines)));
        return Convert.ToHexStringLower(bytes);
    }

    private static ProceduralGenerationConfig MakeConfig()
    {
        return new()
        {
            ChunkConfig = new ChunkConfig { ViewDistance = 0 },
            TerrainConfig = new TerrainConfig(),
            NamingConfig = new NamingConfig(),
            GenerationConfig = new GenerationConfig(),
            BiomeDefinitions =
            [
                BiomeDefinition.Default,
                new BiomeDefinition
                {
                    Id = "forest",
                    TerrainId = 4,
                    TerrainVariantCount = 2,
                    CityCountModifier = -1,
                },
            ],
            CitySizeDefinitions =
            [
                CitySizeDefinition.Default,
                new CitySizeDefinition
                {
                    Id = "town",
                    Radius = 14,
                    PoiCount = 6,
                },
            ],
            PoiTypeDefinitions =
            [
                PoiTypeDefinition.Default,
                new PoiTypeDefinition
                {
                    Id = "tavern",
                    TerrainIdOffset = 1,
                    NameOptions = ["The Prancing Pony", "The Green Dragon"],
                },
            ],
            RoadTypeDefinitions =
            [
                RoadTypeDefinition.Default,
                new RoadTypeDefinition { Id = "highway", TerrainId = 21 },
            ],
        };
    }

    private static (Node root, WorldGenerator generator, ChunkManager manager) CreateGenerator()
    {
        var config = MakeConfig();
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
            .With(WorldGenerator.PropertyName.WorldState, new WorldState { Seed = Seed })
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
