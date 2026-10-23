using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

public partial class CountingBlueprintPass : BlueprintPass
{
    public int RunCount { get; private set; }

    public override void RunPass(GenerationContext ctx)
    {
        RunCount++;
    }
}
