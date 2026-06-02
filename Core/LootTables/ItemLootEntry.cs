using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Core.LootTables;

[GlobalClass]
public partial class ItemLootEntry : LootTableEntry
{
    [Export]
    public Resource? Item { get; set; }

    [Export]
    public int MinQuantity { get; set; } = 1;

    [Export]
    public int MaxQuantity { get; set; } = 1;

    internal override IReadOnlyList<LootResult> Resolve(SeededRng rng, RollContext context)
    {
        if (Item is null)
        {
            GD.PushError($"ItemLootEntry '{Id}': Item is null, returning empty result.");
            return [];
        }

        var minQty = Math.Max(0, MinQuantity);
        var maxQty = Math.Max(0, MaxQuantity);
        var quantity = minQty >= maxQty ? minQty : rng.Next(minQty, maxQty + 1);

        var result = new LootResult { Item = Item, Quantity = quantity };

        return [result];
    }
}
