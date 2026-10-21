#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// Configurable name generation data for procedural entities. Set it on
/// <c>ProceduralGenerationConfig.NamingConfig</c>; without one the defaults below apply.
/// </summary>
[GlobalClass]
public partial class NamingConfig : Resource
{
    [Export]
    public string[] RegionPrefixes { get; set; } =
    ["North", "South", "East", "West", "Upper", "Lower", "Greater", "Lesser"];

    [Export]
    public string[] RegionSuffixes { get; set; } =
    ["Hills", "Plains", "Reach", "March", "Hollow", "Vale", "Heath", "Dales"];

    [Export]
    public string[] CityPrefixes { get; set; } =
    ["Iron", "Storm", "Raven", "Silver", "Mist", "Thorn", "Ash", "Gold"];

    [Export]
    public string[] CitySuffixes { get; set; } =
    ["haven", "hold", "gate", "ford", "watch", "bridge", "dale", "keep"];

    /// <summary>
    /// Returns a message for each empty name list: every name is built from one prefix and
    /// one suffix, so no list may be empty. Override to add checks for a subclass's fields.
    /// </summary>
    public virtual IEnumerable<string> Validate()
    {
        const string owner = nameof(NamingConfig);
        string?[] messages =
        [
            ConfigValidation.NotEmpty(owner, nameof(RegionPrefixes), RegionPrefixes?.Length ?? 0),
            ConfigValidation.NotEmpty(owner, nameof(RegionSuffixes), RegionSuffixes?.Length ?? 0),
            ConfigValidation.NotEmpty(owner, nameof(CityPrefixes), CityPrefixes?.Length ?? 0),
            ConfigValidation.NotEmpty(owner, nameof(CitySuffixes), CitySuffixes?.Length ?? 0),
        ];
        return messages.OfType<string>();
    }
}
