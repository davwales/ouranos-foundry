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
    private const ulong RngSalt = 4000;

    private SeededRng? _rng;

    [Export]
    public LootTableData? Table { get; private set; }

    /// <summary>
    /// Seed for the parameterless <see cref="Roll()"/>. Ignored while <see cref="AutoSeed"/> is on.
    /// </summary>
    [Export]
    public long Seed { get; private set; }

    /// <summary>
    /// When on, the parameterless <see cref="Roll()"/> picks a fresh seed for each roller
    /// instead of using <see cref="Seed"/>, so every play session rolls differently.
    /// </summary>
    [Export]
    public bool AutoSeed { get; private set; } = true;

    /// <summary>
    /// The seed the parameterless <see cref="Roll()"/> stream started from: <see cref="Seed"/>,
    /// or the generated one when <see cref="AutoSeed"/> is on. Log it to reproduce a session.
    /// </summary>
    public long ActiveSeed { get; private set; }

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

    /// <summary>
    /// Turns <see cref="AutoSeed"/> off and restarts the <see cref="Roll()"/> stream from <paramref name="seed"/>.
    /// </summary>
    public void SetSeed(long seed)
    {
        Seed = seed;
        AutoSeed = false;
        _rng = null;
    }

    /// <summary>
    /// Rolls the table with the roller's own stream (see <see cref="Seed"/> and
    /// <see cref="AutoSeed"/>). Successive calls continue the stream, so they roll
    /// differently but reproducibly. Callable from GDScript.
    /// </summary>
    public Godot.Collections.Array<LootResult> Roll()
    {
        if (_rng is null)
        {
            ActiveSeed = AutoSeed
                ? (long)SeededRng.Mix(Time.GetTicksUsec() ^ GetInstanceId())
                : Seed;
            _rng = new SeededRng((ulong)ActiveSeed, RngSalt);
        }

        return [.. Roll(_rng)];
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

        EmitSignal(SignalName.TableRolled, Table, new Godot.Collections.Array<LootResult>(results));
        return results;
    }
}
