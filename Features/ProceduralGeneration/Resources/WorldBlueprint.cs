namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

[GlobalClass]
public partial class WorldBlueprint : Resource
{
    [Export]
    public ulong Seed { get; set; }

    public bool IsBlueprintGenerated { get; set; }
}
