using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

[TestSuite]
[RequireGodotRuntime]
public class GenerationPipelineTests
{
    [TestCase]
    public async Task RunPasses_OnAWorkerThread_SendsLogLinesToTheLogSink()
    {
        // Arrange
        var config = new ProceduralGenerationConfig();
        var definitions = new GenerationDefinitions(config);
        var world = new WorldState();
        var chunk = new ChunkBuffer(
            new ChunkDescriptor(
                new Vector2I(2, 3),
                ChunkType.Wilderness,
                new HashSet<Guid>(),
                new HashSet<Guid>()
            )
        );
        var pipeline = new GenerationPipeline();
        pipeline.AddChild(new LoggingGenerationPass());
        pipeline._Ready();
        var logs = new List<string>();

        // Act
        var error = await Task.Run(() =>
            pipeline.RunPasses(config, definitions, world, chunk, LogLevel.Basic, logs.Add)
        );

        // Assert
        error.ShouldBeNull();
        logs.ShouldBe(["basic (2, 3)"]);

        pipeline.Free();
    }

    [TestCase]
    public void RunPasses_OnDifferentChunks_GivesEachChunkItsOwnDeterministicRng()
    {
        // Arrange
        var config = new ProceduralGenerationConfig();
        var definitions = new GenerationDefinitions(config);
        var world = new WorldState { Seed = 1234 };
        var pipeline = new GenerationPipeline();
        pipeline.AddChild(new RngSamplingGenerationPass());
        pipeline._Ready();
        Vector2I[] coords = [new(0, 0), new(1, 0), new(0, 1), new(-1, 0), new(-1, -1)];
        int[] Sample(Vector2I c)
        {
            var chunk = MakeChunk(c);
            pipeline
                .RunPasses(config, definitions, world, chunk, LogLevel.None, _ => { })
                .ShouldBeNull();
            return chunk.TerrainData[..RngSamplingGenerationPass.SampleCount];
        }

        // Act
        var first = coords.Select(Sample).ToList();
        var second = coords.Select(Sample).ToList();

        // Assert
        second.ShouldBe(first);
        first.Select(s => string.Join(",", s)).Distinct().Count().ShouldBe(coords.Length);

        pipeline.Free();
    }

    private static ChunkBuffer MakeChunk(Vector2I coords)
    {
        return new ChunkBuffer(
            new ChunkDescriptor(
                coords,
                ChunkType.Wilderness,
                new HashSet<Guid>(),
                new HashSet<Guid>()
            )
        );
    }
}
