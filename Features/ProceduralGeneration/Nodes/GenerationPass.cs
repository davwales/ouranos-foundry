#nullable enable

using Godot;
using Ouranos.Foundry.Core.Utils;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

/// <summary>
/// A step of chunk generation. Runs per chunk, on a worker thread, in
/// <c>GenerationPipeline</c> child order. Treat <c>ctx.World</c> as read-only, write only
/// to the chunk and call no Godot API: see the Threading section of the feature README.
/// </summary>
[GlobalClass]
public abstract partial class GenerationPass : Node
{
    /// <summary>
    /// Salts <c>GenerationContext.Rng</c>, which is also keyed by the chunk's coordinates
    /// (<c>SeededRng.ForCoords</c>). Defaults to a stable hash of the full type
    /// name, so renaming or moving the pass changes its stream.
    /// </summary>
    protected virtual ulong RngSalt => StableId.Hash(GetType().FullName ?? GetType().Name);

    internal ulong Salt => RngSalt;

    public abstract void RunPass(GenerationContext ctx, ChunkBuffer chunk);
}
