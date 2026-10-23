using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

/// <summary>Records the context it ran with and the first values of its RNG stream.</summary>
public partial class RecordingBlueprintPass : BlueprintPass
{
    public GenerationContext? Context { get; private set; }

    public int[] Draws { get; private set; } = [];

    public override void RunPass(GenerationContext ctx)
    {
        Context = ctx;
        Draws = [ctx.Rng.Next(), ctx.Rng.Next(), ctx.Rng.Next(), ctx.Rng.Next()];
    }
}
