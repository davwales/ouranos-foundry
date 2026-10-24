#nullable enable

using System.Collections.Generic;
using Godot;

namespace Ouranos.Foundry.Core.Nodes;

/// <summary>
/// Runs one of its direct <see cref="State"/> children at a time and forwards
/// process, physics and unhandled input to it. Transitions requested while another
/// transition is running are queued and applied after it completes.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class StateMachine : Node
{
    private const int MaxChainedTransitions = 32;

    /// <summary>
    /// The state entered on ready. Defaults to the first <see cref="State"/> child.
    /// </summary>
    [Export]
    public State? InitialState { get; private set; }

    /// <summary>
    /// Emitted after every transition. <paramref name="from"/> is empty for the initial state.
    /// </summary>
    [Signal]
    public delegate void StateChangedEventHandler(StringName from, StringName to);

    private readonly Dictionary<StringName, State> _states = [];
    private readonly Queue<State> _pending = new();
    private bool _transitioning;

    public State? CurrentState { get; private set; }

    public State? PreviousState { get; private set; }

    public IReadOnlyDictionary<StringName, State> States => _states;

    public override void _Ready()
    {
        RegisterStates();

        var initial = InitialState;
        if (initial is not null && !_states.ContainsValue(initial))
        {
            GD.PushError(
                $"StateMachine '{Name}': InitialState '{initial.Name}' is not a direct child; using the first State."
            );
            initial = null;
        }

        initial ??= FirstState();
        if (initial is null)
        {
            GD.PushWarning($"StateMachine '{Name}': has no State children.");
            return;
        }

        Run(initial);
    }

    public override void _EnterTree()
    {
        // _Ready runs only once, so re-register when the machine is re-added to the tree.
        if (IsNodeReady())
        {
            RegisterStates();
        }
    }

    public override void _ExitTree()
    {
        UnregisterStates();
    }

    public override void _Process(double delta)
    {
        CurrentState?.Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        CurrentState?.PhysicsProcess(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        CurrentState?.HandleInput(@event);
    }

    /// <summary>
    /// Changes to the named state. Returns false if the state is unknown or already
    /// current. During another transition the change is queued and this returns true.
    /// </summary>
    public bool TransitionTo(StringName stateName)
    {
        if (!_states.TryGetValue(stateName, out var next))
        {
            GD.PushError($"StateMachine '{Name}': unknown state '{stateName}'.");
            return false;
        }

        if (_transitioning)
        {
            _pending.Enqueue(next);
            return true;
        }

        if (next == CurrentState)
        {
            return false;
        }

        Run(next);
        return true;
    }

    private void Run(State next)
    {
        _transitioning = true;
        try
        {
            Apply(next);

            var chained = 0;
            while (_pending.TryDequeue(out var queued))
            {
                if (++chained > MaxChainedTransitions)
                {
                    GD.PushError(
                        $"StateMachine '{Name}': more than {MaxChainedTransitions} chained transitions; dropping the rest."
                    );
                    break;
                }

                Apply(queued);
            }
        }
        finally
        {
            _pending.Clear();
            _transitioning = false;
        }
    }

    private void Apply(State next)
    {
        if (next == CurrentState)
        {
            return;
        }

        var previous = CurrentState;
        var from = previous?.Name ?? new StringName("");

        previous?.ExitState(next.Name);
        PreviousState = previous;
        CurrentState = next;
        next.EnterState(from);
        EmitSignal(SignalName.StateChanged, from, next.Name);
    }

    private State? FirstState()
    {
        foreach (var child in GetChildren())
        {
            if (child is State state)
            {
                return state;
            }
        }

        return null;
    }

    private void RegisterStates()
    {
        UnregisterStates();

        foreach (var child in GetChildren())
        {
            if (child is State state)
            {
                _states[state.Name] = state;
                state.TransitionRequested += OnTransitionRequested;
            }
        }
    }

    private void UnregisterStates()
    {
        foreach (var state in _states.Values)
        {
            if (IsInstanceValid(state))
            {
                state.TransitionRequested -= OnTransitionRequested;
            }
        }

        _states.Clear();
    }

    private void OnTransitionRequested(StringName stateName)
    {
        TransitionTo(stateName);
    }
}
