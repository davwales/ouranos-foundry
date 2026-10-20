# LootTables

Weighted, deterministic loot rolling authored as Godot resources.

## Pieces

| Type | Kind | Role |
|------|------|------|
| `LootTableData` | Resource | A table: roll count (`MinRolls`/`MaxRolls`), `AllowDuplicates`, `Entries`. |
| `LootTableEntry` | Resource (abstract) | Base entry with `Id`, `DisplayName`, `Weight`. |
| `ItemLootEntry` | Resource | Drops `Item` × random `MinQuantity..MaxQuantity`. |
| `SubTableLootEntry` | Resource | Rolls another table; optional `MinRollsOverride`/`MaxRollsOverride` (`-1` = use the sub-table's own). |
| `EmptyLootEntry` | Resource | Explicit "nothing" outcome. Produces no `LootResult`; `LootTableRoller` emits `EmptyDropSelected` instead. |
| `LootResult` | Resource | One result: `Item` + `Quantity`. |
| `LootTableRoller` | Node | Scene-friendly wrapper; emits `TableRolled`, and `EmptyDropSelected` with the (sub-)table that picked an empty entry. Swap tables at runtime with `SetTable()`. |

## API

```csharp
var results = table.Roll(new SeededRng(seed));            // configured roll count
var three   = table.Roll(new SeededRng(seed), rollsOverride: 3);
var odds    = table.GetProbabilities();                    // per-entry probability
```

Rolling is deterministic for a given seed. Each roll and each sub-table uses `rng.Derive()`.

## Extending

Subclass `LootTableEntry` in any assembly:

```csharp
[GlobalClass]
public partial class LevelGatedEntry : ItemLootEntry
{
    [Export] public int MinLevel { get; set; }

    public override bool IsEligible(RollContext context) { ... }
}
```

- `IsEligible(RollContext)`: return `false` to exclude the entry from this roll.
- `Resolve(SeededRng, RollContext)`: produce results.
- Entries that roll nested tables call `table.Roll(rng, context.Deeper(), ...)` so the depth and cycle checks apply.

## Safety limits

- **Depth:** nesting deeper than 10 sub-table levels pushes an error and returns empty for that branch.
- **Cycles:** if a table appears again in its own roll chain, that branch pushes an error and returns empty. Results from other branches are kept.
- **Bad config:** null `Item`/`SubTable`, or all weights ≤ 0, pushes an error or returns empty. It never throws.
