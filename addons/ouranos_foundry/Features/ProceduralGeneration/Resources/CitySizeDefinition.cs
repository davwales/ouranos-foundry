#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

[GlobalClass]
public partial class CitySizeDefinition : Resource
{
    [Export]
    public string Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    [Export]
    public int Radius { get; set; } = 8;

    [Export]
    public int PoiCount { get; set; } = 4;

    /// <summary>
    /// Game-specific metadata dictionary. Use this to attach arbitrary key-value
    /// pairs (e.g., "has_gym" → true, "population" → 500) without subclassing.
    /// The framework ignores this data; it exists for downstream game code.
    /// </summary>
    [Export]
    public Godot.Collections.Dictionary<string, Variant> CustomData { get; set; } = [];

    /// <summary>
    /// Returns a message for each invalid value. Empty when the definition is valid.
    /// </summary>
    public virtual IEnumerable<string> Validate()
    {
        var owner = $"{nameof(CitySizeDefinition)} '{Id}'";
        string?[] messages = [ConfigValidation.AtLeast(owner, nameof(Radius), Radius, 0)];
        return messages.OfType<string>();
    }

    public static readonly CitySizeDefinition Default = new()
    {
        Id = "village",
        DisplayName = "Village",
        Radius = 8,
        PoiCount = 4,
    };
}
