using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

[GlobalClass]
public abstract partial class GenerationPass : Node
{
    public abstract void RunPass(WorldBlueprint worldBlueprint, ChunkData chunk);
}
