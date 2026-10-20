#nullable enable

using System.Collections.Generic;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.LootTables.Resources;
using Ouranos.Foundry.Features.LootTables.Types;

namespace Ouranos.Foundry.Features.LootTables.Utils;

internal static class WeightedSelection
{
    /// <summary>
    /// Returns null if no eligible entries or all weights &lt;= 0.
    /// </summary>
    public static LootTableEntry? Select(
        IReadOnlyList<LootTableEntry> entries,
        SeededRng rng,
        RollContext context
    )
    {
        var eligible = new List<LootTableEntry>();
        var totalWeight = 0f;

        foreach (var entry in entries)
        {
            if (!entry.IsEligible(context))
            {
                continue;
            }

            var weight = entry.EffectiveWeight;
            if (weight > 0f)
            {
                eligible.Add(entry);
                totalWeight += weight;
            }
        }

        if (eligible.Count == 0 || totalWeight <= 0f)
        {
            return null;
        }

        var roll = rng.NextFloat() * totalWeight;
        var cumulative = 0f;

        foreach (var entry in eligible)
        {
            cumulative += entry.EffectiveWeight;
            if (roll < cumulative)
            {
                return entry;
            }
        }

        // Floating-point safety fallback
        return eligible[^1];
    }
}
