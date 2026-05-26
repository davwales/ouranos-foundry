using Godot;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// Configurable parameters for city and region generation. Set as an export
/// on blueprint passes so game-specific settings can be configured in the editor.
/// </summary>
[GlobalClass]
public partial class GenerationConfig : Resource
{
    [Export]
    public int RegionCount { get; set; } = 8;

    /// <summary>
    /// Half-size radius for region generation and city placement.
    /// IMPORTANT: Must be <= MinRegionSeparation / 2 to ensure cities are always
    /// placed within their true Voronoi cell. If MinRegionSeparation is too small,
    /// cities may be placed closer to a neighboring region's center than their own.
    /// </summary>
    [Export]
    public int MinRegionSize { get; set; } = 80;

    [Export]
    public int MinCitiesPerRegion { get; set; } = 1;

    [Export]
    public int MaxCitiesPerRegion { get; set; } = 4;

    [Export]
    public int MinCityDistance { get; set; } = 40;

    [Export]
    public int CityPositionAttempts { get; set; } = 20;

    [Export]
    public int RoadWaypointSpacing { get; set; } = 50;

    [Export]
    public int RoadWaypointJitter { get; set; } = 10;

    /// <summary>
    /// Minimum distance between region centers during placement.
    /// IMPORTANT: Should be >= MinRegionSize * 2 for clean Voronoi partitioning.
    /// Lower values may cause city placement outside true Voronoi cells.
    /// </summary>
    [Export]
    public int MinRegionSeparation { get; set; } = 160;

    /// <summary>
    /// Maximum number of attempts to place a region center before
    /// accepting the last attempt even if it violates separation.
    /// </summary>
    [Export]
    public int MaxPlacementAttempts { get; set; } = 50;
}
