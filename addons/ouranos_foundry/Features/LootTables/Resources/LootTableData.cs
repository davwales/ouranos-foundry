#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Core.Utils;
using Ouranos.Foundry.Features.LootTables.Types;

namespace Ouranos.Foundry.Features.LootTables.Resources;

[GlobalClass]
public partial class LootTableData : Resource
{
    private const int MaxRecursionDepth = 10;

    [Export]
    public string Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    [Export]
    public string Description { get; set; } = "";

    [Export]
    public int MinRolls { get; set; } = 1;

    [Export]
    public int MaxRolls { get; set; } = 1;

    [Export]
    public bool AllowDuplicates { get; set; }

    [Export]
    public Godot.Collections.Array<LootTableEntry> Entries { get; set; } = [];

    public IReadOnlyList<LootResult> Roll(SeededRng rng, int? rollsOverride = null)
    {
        return Roll(rng, new RollContext(), rollsOverride);
    }

    /// <summary>
    /// Rolls this table as part of an outer roll. Custom entries that nest tables
    /// should call this with <c>context.Deeper()</c> so depth and cycle checks apply.
    /// </summary>
    public IReadOnlyList<LootResult> Roll(
        SeededRng rng,
        RollContext context,
        int? rollsOverride = null
    )
    {
        if (context.Depth > MaxRecursionDepth)
        {
            GD.PushError(
                $"LootTableData '{Id}': Max recursion depth ({MaxRecursionDepth}) exceeded."
            );
            return [];
        }

        if (context.IsRolling(this))
        {
            GD.PushError($"LootTableData '{Id}': Circular sub-table reference detected.");
            return [];
        }

        if (Entries.Count == 0)
        {
            return [];
        }

        context = context.Entering(this);
        var results = new List<LootResult>();
        var consumedEntries = new HashSet<LootTableEntry>(ReferenceEqualityComparer.Instance);
        var rollCount = GetRollCount(rng, rollsOverride);

        for (var i = 0; i < rollCount; i++)
        {
            var derivedRng = rng.Derive();
            var eligible = GetEligibleEntries(context, consumedEntries);
            var entry = WeightedRandom.Pick(eligible, e => e.EffectiveWeight, derivedRng);

            if (entry is null)
            {
                continue;
            }

            if (entry is EmptyLootEntry)
            {
                context.OnEmptyDropSelected?.Invoke(this);
            }

            results.AddRange(entry.Resolve(derivedRng, context));

            if (!AllowDuplicates)
            {
                consumedEntries.Add(entry);
            }
        }

        return results.AsReadOnly();
    }

    public IReadOnlyList<ProbabilityEntry> GetProbabilities()
    {
        return GetProbabilities(new RollContext());
    }

    public IReadOnlyList<ProbabilityEntry> GetProbabilities(RollContext context)
    {
        var eligible = new List<LootTableEntry>();
        var totalWeight = 0f;

        foreach (var entry in Entries)
        {
            if (!entry.IsEligible(context))
            {
                continue;
            }

            eligible.Add(entry);
            totalWeight += entry.EffectiveWeight;
        }

        var probabilities = new List<ProbabilityEntry>();

        foreach (var entry in eligible)
        {
            var probability = totalWeight > 0f ? entry.EffectiveWeight / totalWeight : 0f;

            probabilities.Add(
                new ProbabilityEntry
                {
                    EntryId = entry.Id,
                    Probability = probability,
                    Entry = entry,
                }
            );
        }

        return probabilities.AsReadOnly();
    }

    private List<LootTableEntry> GetEligibleEntries(
        RollContext context,
        HashSet<LootTableEntry> consumedEntries
    )
    {
        var eligible = new List<LootTableEntry>(Entries.Count);
        foreach (var entry in Entries)
        {
            if (!consumedEntries.Contains(entry) && entry.IsEligible(context))
            {
                eligible.Add(entry);
            }
        }

        return eligible;
    }

    private int GetRollCount(SeededRng rng, int? rollsOverride)
    {
        if (rollsOverride.HasValue)
        {
            return Math.Max(0, rollsOverride.Value);
        }

        var minRolls = MinRolls;
        var maxRolls = MaxRolls;
        if (minRolls > maxRolls)
        {
            (minRolls, maxRolls) = (maxRolls, minRolls);
        }

        return rng.Next(minRolls, maxRolls + 1);
    }
}
