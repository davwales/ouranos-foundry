using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

/// <summary>Logs one Basic and one Verbose line naming the chunk.</summary>
public partial class LoggingGenerationPass : GenerationPass
{
    public override void RunPass(GenerationContext ctx, ChunkBuffer chunk)
    {
        ctx.Log($"basic {chunk.Coordinates}", LogLevel.Basic);
        ctx.Log($"verbose {chunk.Coordinates}", LogLevel.Verbose);
    }
}
