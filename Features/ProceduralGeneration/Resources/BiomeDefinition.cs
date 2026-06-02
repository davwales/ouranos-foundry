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
