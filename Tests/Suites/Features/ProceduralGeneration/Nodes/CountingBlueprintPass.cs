using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

public partial class CountingBlueprintPass : BlueprintPass
{
    public int RunCount { get; private set; }

    public override void RunPass(WorldBlueprint worldBlueprint)
    {
        RunCount++;
    }
}
