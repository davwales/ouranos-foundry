#nullable enable

using Godot;
using Ouranos.Foundry.Core.Utils;

namespace Ouranos.Foundry.Core.Nodes;

/// <summary>
/// A <see cref="Label"/> that shows a <see cref="StateMachine"/>'s current state.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class StateDebugLabel : Label
{
    [Export]
    public StateMachine? Machine { get; private set; }

    [Export]
    public string Prefix { get; private set; } = "State: ";

    private readonly StateMachineConnection _connection = new();

    public override void _Ready()
    {
        MonitorStateMachine(Machine);
    }

    public override void _EnterTree()
    {
        if (IsNodeReady())
        {
            MonitorStateMachine(Machine);
        }
    }

    public override void _ExitTree()
    {
        _connection.Disconnect();
    }

    public void MonitorStateMachine(StateMachine? machine)
    {
        Machine = machine;
        _connection.Monitor(machine, OnStateChanged);
    }

    private void OnStateChanged(StringName state)
    {
        Text = StateDebugText.Format(Prefix, state);
    }
}
