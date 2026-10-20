#nullable enable

using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.LootTables.Resources;
using Ouranos.Foundry.Features.LootTables.Types;

namespace Ouranos.Foundry.Features.LootTables.Nodes;

[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class LootTableRoller : Node
{
    [Export]
    public LootTableData? Table { get; private set; }

    [Signal]
    public delegate void TableRolledEventHandler(
        LootTableData table,
        Godot.Collections.Array<LootResult> results
    );

    /// <summary>
    /// Emitted once per empty outcome, with the table (or sub-table) that selected it.
    /// Empty outcomes produce no <see cref="LootResult"/>.
    /// </summary>
    [Signal]
    public delegate void EmptyDropSelectedEventHandler(LootTableData table);

    public void SetTable(LootTableData? table)
    {
        Table = table;
    }

    public IReadOnlyList<LootResult> Roll(SeededRng rng, int? rollsOverride = null)
    {
        if (Table is null)
        {
            GD.PushError("LootTableRoller: No LootTableData assigned.");
            return [];
        }

        var context = new RollContext
        {
            OnEmptyDropSelected = table => EmitSignal(SignalName.EmptyDropSelected, table),
        };
        var results = Table.Roll(rng, context, rollsOverride);

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
