using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

[GlobalClass]
public abstract partial class BlueprintPass : Node
{
    public abstract void RunPass(WorldBlueprint worldBlueprint);
}
