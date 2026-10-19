using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Features.LootTables;

[GlobalClass]
public partial class EmptyLootEntry : LootTableEntry
{
    /// <summary>
    /// Produces no results. The owning table reports the selection through
    /// <see cref="RollContext.OnEmptyDropSelected"/>.
    /// </summary>
    internal override IReadOnlyList<LootResult> Resolve(SeededRng rng, RollContext context) => [];
}
