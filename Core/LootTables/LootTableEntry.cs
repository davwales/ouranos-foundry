using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Core.LootTables;

[GlobalClass]
public abstract partial class LootTableEntry : Resource
{
    [Export]
    public string Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    [Export]
    public float Weight { get; set; } = 1f;

    internal virtual bool IsEligible(RollContext context) => true;

    internal abstract IReadOnlyList<LootResult> Resolve(SeededRng rng, RollContext context);
}
