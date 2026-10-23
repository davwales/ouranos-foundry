using System;
using System.Collections.Generic;
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
}
