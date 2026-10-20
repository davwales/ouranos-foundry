#nullable enable

using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.LootTables.Types;

namespace Ouranos.Foundry.Features.LootTables.Resources;

/// <summary>
/// The sub-table is rolled with its own derived RNG stream for determinism.
/// </summary>
[GlobalClass]
public partial class SubTableLootEntry : LootTableEntry
{
    [Export]
    public LootTableData? SubTable { get; set; }

    [Export]
    public int MinRollsOverride { get; set; } = -1;

    [Export]
    public int MaxRollsOverride { get; set; } = -1;

    public override IReadOnlyList<LootResult> Resolve(SeededRng rng, RollContext context)
    {
        if (SubTable is null)
        {
            GD.PushError($"SubTableLootEntry '{Id}': SubTable is null, returning empty result.");
            return [];
        }

        var derivedRng = rng.Derive();
        var deeperContext = context.Deeper();

        int? rollsOverride = null;
        if (MinRollsOverride >= 0)
        {
            var min = MinRollsOverride;
            var max = MaxRollsOverride >= MinRollsOverride ? MaxRollsOverride : MinRollsOverride;
            rollsOverride = derivedRng.Next(min, max + 1);
        }

        var subResults = SubTable.Roll(derivedRng, deeperContext, rollsOverride);

        return subResults;
    }
}
