#nullable enable

using Godot;
using Ouranos.Foundry.Core.Utils;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

/// <summary>
/// A step of chunk generation. Runs per chunk, on a worker thread, in
/// <c>GenerationPipeline</c> child order. Treat <c>ctx.World</c> as read-only and
/// write only to the chunk.
/// </summary>
[GlobalClass]
public abstract partial class GenerationPass : Node
{
    /// <summary>
    /// Salts <c>GenerationContext.Rng</c>. Defaults to a stable hash of the full type
    /// name, so renaming or moving the pass changes its stream.
    /// </summary>
    public virtual ulong RngSalt => StableId.Hash(GetType().FullName ?? GetType().Name);

    public abstract void RunPass(GenerationContext ctx, ChunkData chunk);
}
