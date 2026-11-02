#nullable enable

using System;
using Godot;
using Ouranos.Foundry.Core.Nodes;

namespace Ouranos.Foundry.Core.Utils;

/// <summary>
/// Subscribes a monitor to a <see cref="StateMachine"/>'s <c>StateChanged</c> signal
/// and keeps the handler so it can be unsubscribed again.
/// </summary>
internal sealed class StateMachineConnection
{
    private StateMachine? _machine;
    private StateMachine.StateChangedEventHandler? _handler;

    public StateMachine? Machine => _machine;

    /// <summary>
    /// Switches to <paramref name="machine"/> and immediately reports its current state
    /// (empty if none).
    /// </summary>
    public void Monitor(StateMachine? machine, Action<StringName> onStateChanged)
    {
        if (_machine != machine)
        {
            Disconnect();
            _machine = machine;

            if (_machine is not null)
            {
                _handler = (_, to) => onStateChanged(to);
                _machine.StateChanged += _handler;
            }
        }

        onStateChanged(_machine?.CurrentState?.Name ?? new StringName(""));
    }

    public void Disconnect()
    {
        if (_machine is not null && _handler is not null && GodotObject.IsInstanceValid(_machine))
        {
            _machine.StateChanged -= _handler;
        }

        _machine = null;
        _handler = null;
    }
}
