using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

/// <summary>Writes the first values of <c>ctx.Rng</c> into the chunk's first tiles.</summary>
public partial class RngSamplingGenerationPass : GenerationPass
{
    public const int SampleCount = 4;

    public override void RunPass(GenerationContext ctx, ChunkBuffer chunk)
    {
        for (var i = 0; i < SampleCount; i++)
        {
            chunk.TerrainData[i] = ctx.Rng.Next();
        }
    }
}
