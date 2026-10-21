using System;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

public partial class ThrowingGenerationPass : GenerationPass
{
    public const string Message = "boom";

    public override void RunPass(GenerationContext ctx, ChunkData chunk)
    {
        throw new InvalidOperationException(Message);
    }
}
