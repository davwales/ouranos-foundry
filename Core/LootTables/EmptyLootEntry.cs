using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Core.LootTables;

[GlobalClass]
public partial class EmptyLootEntry : LootTableEntry
{
    internal override IReadOnlyList<LootResult> Resolve(SeededRng rng, RollContext context)
    {
        var result = new LootResult { Item = null, Quantity = 0 };

        return [result];
    }
}
