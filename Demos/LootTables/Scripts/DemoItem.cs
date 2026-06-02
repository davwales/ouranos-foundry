namespace Ouranos.Foundry.Demos.LootTables;

[GlobalClass]
public partial class DemoItem : Resource
{
    [Export]
    public string DisplayName { get; set; } = "Unknown Item";

    [Export]
    public string Icon { get; set; } = "";
}
