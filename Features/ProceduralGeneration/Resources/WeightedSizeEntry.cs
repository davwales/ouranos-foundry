namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// A weighted reference to a CitySizeDefinition, used in biome-level
/// city size distributions. The SizeId must match a CitySizeDefinition.Id.
/// </summary>
[GlobalClass]
public partial class WeightedSizeEntry : Resource
{
    [Export]
    public string SizeId { get; set; } = "";

    [Export]
    public float Weight { get; set; } = 1f;
}
