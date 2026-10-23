using GdUnit4;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Core.Utils;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

[TestSuite]
[RequireGodotRuntime]
public class BlueprintPipelineTests
{
    private const ulong Seed = 42;

    [TestCase]
    public void RunPasses_WithCustomPasses_GivesEachTheConfigDefinitionsWorldAndItsOwnRng()
    {
        // Arrange
        var config = new ProceduralGenerationConfig();
        var definitions = new GenerationDefinitions(config);
        var world = new WorldState { Seed = Seed };
        var first = new RecordingBlueprintPass();
        var sameType = new RecordingBlueprintPass();
        var otherType = new OtherRecordingBlueprintPass();
        var pipeline = new BlueprintPipeline();
        pipeline.AddPass(first);
        pipeline.AddPass(sameType);
        pipeline.AddPass(otherType);
        var expectedRng = new SeededRng(
            Seed,
            StableId.Hash(typeof(RecordingBlueprintPass).FullName!)
        );
        int[] expectedDraws =
        [
            expectedRng.Next(),
            expectedRng.Next(),
            expectedRng.Next(),
            expectedRng.Next(),
        ];

        // Act
        var result = pipeline.RunPasses(config, definitions, world, LogLevel.None);

        // Assert
        result.ShouldBeNull();
        var context = first.Context.ShouldNotBeNull();
        context.Config.ShouldBeSameAs(config);
        context.Definitions.ShouldBeSameAs(definitions);
        context.World.ShouldBeSameAs(world);
        first.Draws.ShouldBe(expectedDraws);
        sameType.Draws.ShouldBe(first.Draws);
        otherType.Draws.ShouldNotBe(first.Draws);

        pipeline.Free();
    }
}
