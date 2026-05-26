using Godot;

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
}
