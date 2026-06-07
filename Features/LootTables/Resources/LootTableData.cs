using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Features.LootTables;

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

    public IReadOnlyList<LootResult> Roll(SeededRng rng, int? rollsOverride = null) =>
        Roll(rng, new RollContext(), rollsOverride);

    internal IReadOnlyList<LootResult> Roll(
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

        if (Entries.Count == 0)
        {
            return [];
        }

        var results = new List<LootResult>();
        var consumedEntryIds = new HashSet<string>();
        var rollCount = GetRollCount(rng, rollsOverride);

        for (var i = 0; i < rollCount; i++)
        {
            var derivedRng = rng.Derive();
            var eligible = GetEligibleEntries(AllowDuplicates, consumedEntryIds);
            var entry = WeightedSelection.Select(eligible, derivedRng, context);

            if (entry is null)
            {
                continue;
            }

            results.AddRange(entry.Resolve(derivedRng, context));

            if (!AllowDuplicates)
            {
                consumedEntryIds.Add(entry.Id);
            }
        }

        return results.AsReadOnly();
    }

    public IReadOnlyList<ProbabilityEntry> GetProbabilities() =>
        GetProbabilities(new RollContext());

    internal IReadOnlyList<ProbabilityEntry> GetProbabilities(RollContext context)
    {
        var eligible = new List<LootTableEntry>();
        var totalWeight = 0f;

        foreach (var entry in Entries)
        {
            if (!entry.IsEligible(context))
            {
                continue;
            }

            var weight = entry.Weight > 0f ? entry.Weight : 0f;
            eligible.Add(entry);
            totalWeight += weight;
        }

        var probabilities = new List<ProbabilityEntry>();

        foreach (var entry in eligible)
        {
            var clampedWeight = entry.Weight > 0f ? entry.Weight : 0f;
            var probability = totalWeight > 0f ? clampedWeight / totalWeight : 0f;

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
        bool allowDuplicates,
        HashSet<string> consumedEntryIds
    )
    {
        if (allowDuplicates)
        {
            return [.. Entries];
        }

        var eligible = new List<LootTableEntry>(Entries.Count);
        foreach (var entry in Entries)
        {
            if (!consumedEntryIds.Contains(entry.Id))
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
