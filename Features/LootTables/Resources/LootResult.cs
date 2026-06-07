namespace Ouranos.Foundry.Features.LootTables;

[GlobalClass]
public partial class LootResult : Resource
{
    [Export]
    public Resource? Item { get; set; }

    [Export]
    public int Quantity { get; set; }
}
