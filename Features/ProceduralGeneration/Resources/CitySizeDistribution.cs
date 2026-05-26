using Godot;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// Weighted probability distribution for determining city sizes.
/// Uses WeightedSizeEntry references to CitySizeDefinition IDs.
/// </summary>
[GlobalClass]
public partial class CitySizeDistribution : Resource
{
    [Export]
    public Godot.Collections.Array<WeightedSizeEntry> Entries { get; set; } = [];

    /// <summary>
    /// Samples a city size ID from this distribution using the provided RNG.
    /// Returns the SizeId of the selected entry, or null if no entries exist.
    /// </summary>
    public string? Sample(SeededRng rng)
    {
        if (Entries.Count == 0)
        {
            return null;
        }

        var total = 0f;
        foreach (var entry in Entries)
        {
            total += entry.Weight;
        }

        if (total <= 0f)
        {
            return Entries[0].SizeId;
        }

        var roll = rng.NextFloat() * total;
        var cumulative = 0f;
        foreach (var entry in Entries)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
            {
                return entry.SizeId;
            }
        }

        return Entries[^1].SizeId;
    }
}
