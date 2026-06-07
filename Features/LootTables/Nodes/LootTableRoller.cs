using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Features.LootTables;

[GlobalClass]
[Icon("res://Assets/icons/foundry_node.svg")]
public partial class LootTableRoller : Node
{
    [Export]
    public LootTableData? Table { get; set; }

    [Signal]
    public delegate void TableRolledEventHandler(
        LootTableData table,
        Godot.Collections.Array<LootResult> results
    );

    public IReadOnlyList<LootResult> Roll(SeededRng rng, int? rollsOverride = null)
    {
        if (Table == null)
        {
            GD.PushError("LootTableRoller: No LootTableData assigned.");
            return [];
        }

        var results = Table.Roll(rng, rollsOverride: rollsOverride);

        // Convert to Godot array for signal compatibility
        var godotResults = new Godot.Collections.Array<LootResult>();
        foreach (var result in results)
        {
            godotResults.Add(result);
        }

        EmitSignal(SignalName.TableRolled, Table, godotResults);
        return results;
    }
}
