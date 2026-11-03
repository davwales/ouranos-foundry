#nullable enable

using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Core.Utils;

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
    /// If no entry has a positive weight, the first entry is returned.
    /// </summary>
    public string? Sample(SeededRng rng)
    {
        if (Entries.Count == 0)
        {
            return null;
        }

        var index = WeightedRandom.PickIndex(Entries, entry => entry.Weight, rng);
        return Entries[index < 0 ? 0 : index].SizeId;
    }
}
