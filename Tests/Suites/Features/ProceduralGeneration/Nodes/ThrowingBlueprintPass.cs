using System;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

public partial class ThrowingBlueprintPass : BlueprintPass
{
    public const string Message = "ThrowingBlueprintPass: boom";

    public override void RunPass(WorldBlueprint worldBlueprint)
    {
        throw new InvalidOperationException(Message);
    }
}
