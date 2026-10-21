#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// Configurable terrain ID mappings for generation passes. Allows game-specific
/// tileset mappings to be configured via the editor rather than hard-coded.
/// </summary>
[GlobalClass]
public partial class TerrainConfig : Resource
{
    /// <summary>
    /// Default terrain ID used when no region is found for a position.
    /// </summary>
    [Export]
    public int DefaultTerrainId { get; set; } = 0;

    [Export]
    public float TerrainNoiseChance { get; set; } = 0.15f;

    [Export]
    public int CityTerrainId { get; set; } = 10;

    [Export]
    public int BuildingTerrainId { get; set; } = 11;

    [Export]
    public int StreetTerrainId { get; set; } = 12;

    [Export]
    public int PoiTerrainIdBase { get; set; } = 30;

    [Export]
    public float CityCenterRatio { get; set; } = 0.3f;

    [Export]
    public int StreetGridSpacing { get; set; } = 3;

    /// <summary>
    /// Returns a message for each value the chunk passes can't generate with. Empty when
    /// the config is valid. Override to add checks for a subclass's own fields.
    /// </summary>
    public virtual IEnumerable<string> Validate()
    {
        const string owner = nameof(TerrainConfig);
        string?[] messages =
        [
            ConfigValidation.AtLeast(owner, nameof(StreetGridSpacing), StreetGridSpacing, 1),
        ];
        return messages.OfType<string>();
    }
}
