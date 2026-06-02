namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// Configurable name generation data for procedural entities. Set as an export
/// on blueprint passes so game-specific names can be configured in the editor.
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
}
