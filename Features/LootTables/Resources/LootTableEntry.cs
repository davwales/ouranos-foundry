using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Features.LootTables;

[GlobalClass]
public abstract partial class LootTableEntry : Resource
{
    [Export]
    public string Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    [Export]
    public float Weight { get; set; } = 1f;

    /// <summary>
    /// The weight used for selection; negative weights count as zero.
    /// </summary>
    internal float EffectiveWeight => Weight > 0f ? Weight : 0f;

    internal virtual bool IsEligible(RollContext context) => true;

    internal abstract IReadOnlyList<LootResult> Resolve(SeededRng rng, RollContext context);
}
