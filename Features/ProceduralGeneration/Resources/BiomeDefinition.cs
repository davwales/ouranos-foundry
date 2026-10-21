#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

[GlobalClass]
public partial class BiomeDefinition : Resource
{
    [Export]
    public string Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    [Export]
    public int TerrainId { get; set; }

    [Export]
    public int TerrainVariantCount { get; set; } = 3;

    [Export]
    public int CityCountModifier { get; set; }

    [Export]
    public CitySizeDistribution? CitySizeDistribution { get; set; }

    [Export]
    public Color DebugColor { get; set; } = Colors.White;

    /// <summary>
    /// Game-specific metadata dictionary. Use this to attach arbitrary key-value
    /// pairs (e.g., "monster_element" → "fire", "difficulty" → 3) without subclassing.
    /// The framework ignores this data; it exists for downstream game code.
    /// </summary>
    [Export]
    public Godot.Collections.Dictionary<string, Variant> CustomData { get; set; } = new();

    /// <summary>
    /// Returns a message for each invalid value. A <c>TerrainVariantCount</c> of 0 is valid:
    /// the biome's terrain then has no noise.
    /// </summary>
    public virtual IEnumerable<string> Validate()
    {
        var owner = $"{nameof(BiomeDefinition)} '{Id}'";
        var entries = CitySizeDistribution?.Entries ?? [];
        string?[] messages =
        [
            ConfigValidation.AtLeast(owner, nameof(TerrainVariantCount), TerrainVariantCount, 0),
            .. entries
                .Select((entry, i) => (entry, i))
                .Where(e => e.entry is null)
                .Select(e => $"{owner}.{nameof(CitySizeDistribution)}.Entries[{e.i}] is empty."),
        ];
        return messages.OfType<string>();
    }

    public static readonly BiomeDefinition Default = new()
    {
        Id = "plains",
        DisplayName = "Plains",
        TerrainId = 0,
        TerrainVariantCount = 3,
        CityCountModifier = 0,
        DebugColor = new Color(0.4f, 0.8f, 0.2f),
    };
}
