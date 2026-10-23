#nullable enable

using Godot;
using Ouranos.Foundry.Core.Utils;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

/// <summary>
/// A step of the world blueprint. Runs once, on the main thread, in
/// <c>BlueprintPipeline</c> child order.
/// </summary>
[GlobalClass]
public abstract partial class BlueprintPass : Node
{
    /// <summary>
    /// Salts <c>GenerationContext.Rng</c>. Defaults to a stable hash of the full type
    /// name, so renaming or moving the pass changes its stream.
    /// </summary>
    public virtual ulong RngSalt => StableId.Hash(GetType().FullName ?? GetType().Name);

    public abstract void RunPass(GenerationContext ctx);
}
