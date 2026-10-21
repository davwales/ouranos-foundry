#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

[GlobalClass]
public partial class PoiTypeDefinition : Resource
{
    [Export]
    public string Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    [Export]
    public int TerrainIdOffset { get; set; }

    [Export]
    public string[] NameOptions { get; set; } = [];

    /// <summary>
    /// Game-specific metadata dictionary. Use this to attach arbitrary key-value
    /// pairs (e.g., "is_gym" → true, "services" → ["heal", "shop"]) without subclassing.
    /// The framework ignores this data; it exists for downstream game code.
    /// </summary>
    [Export]
    public Godot.Collections.Dictionary<string, Variant> CustomData { get; set; } = [];

    /// <summary>
    /// Returns a message for each invalid value: each POI's name is picked from
    /// <c>NameOptions</c>, so it may not be empty.
    /// </summary>
    public virtual IEnumerable<string> Validate()
    {
        var owner = $"{nameof(PoiTypeDefinition)} '{Id}'";
        string?[] messages =
        [
            ConfigValidation.NotEmpty(owner, nameof(NameOptions), NameOptions?.Length ?? 0),
        ];
        return messages.OfType<string>();
    }

    public static readonly PoiTypeDefinition Default = new()
    {
        Id = "landmark",
        DisplayName = "Landmark",
        TerrainIdOffset = 0,
        NameOptions = ["Unknown Location"],
    };
}
