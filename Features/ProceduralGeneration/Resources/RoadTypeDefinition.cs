using Godot;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

[GlobalClass]
public partial class RoadTypeDefinition : Resource
{
    [Export]
    public string Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    [Export]
    public int TerrainId { get; set; }

    [Export]
    public float DebugLineWidth { get; set; } = 1f;

    [Export]
    public Color DebugColor { get; set; } = Colors.White;

    /// <summary>
    /// Game-specific metadata dictionary. Use this to attach arbitrary key-value
    /// pairs (e.g., "travel_speed" → 2.5, "encounter_rate" → 0.3) without subclassing.
    /// The framework ignores this data; it exists for downstream game code.
    /// </summary>
    [Export]
    public Godot.Collections.Dictionary<string, Variant> CustomData { get; set; } = [];

    public static readonly RoadTypeDefinition Default = new()
    {
        Id = "dirt",
        DisplayName = "Dirt",
        TerrainId = 20,
        DebugLineWidth = 1f,
        DebugColor = new Color(0.6f, 0.4f, 0.2f, 0.7f),
    };
}
