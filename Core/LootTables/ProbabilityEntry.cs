namespace Ouranos.Foundry.Core.LootTables;

[GlobalClass]
public partial class ProbabilityEntry : Resource
{
    [Export]
    public string EntryId { get; set; } = "";

    [Export]
    public float Probability { get; set; }

    [Export]
    public LootTableEntry? Entry { get; set; }
}
