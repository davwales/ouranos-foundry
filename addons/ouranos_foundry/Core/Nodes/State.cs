#nullable enable

using Godot;

namespace Ouranos.Foundry.Core.Nodes;

/// <summary>
/// One state of a <see cref="StateMachine"/>. Add states as direct children of the
/// machine; the node name is the state name. Override the lifecycle methods in C#,
/// or connect to <c>Entered</c>/<c>Exited</c> from GDScript or the editor.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class State : Node
{
    [Signal]
    public delegate void EnteredEventHandler(StringName previousState);

    [Signal]
    public delegate void ExitedEventHandler(StringName nextState);

    /// <summary>
    /// Asks the owning machine to change state. States never call the machine directly.
    /// </summary>
    [Signal]
    public delegate void TransitionRequestedEventHandler(StringName stateName);

    /// <summary>
    /// True between this state's enter and exit.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Called when the machine enters this state. <paramref name="previousState"/> is
    /// empty for the initial state.
    /// </summary>
    public virtual void Enter(StringName previousState) { }

    public virtual void Exit(StringName nextState) { }

    /// <summary>
    /// Called from the machine's <c>_Process</c> while this state is current.
    /// </summary>
    public virtual void Process(double delta) { }

    /// <summary>
    /// Called from the machine's <c>_PhysicsProcess</c> while this state is current.
    /// </summary>
    public virtual void PhysicsProcess(double delta) { }

    /// <summary>
    /// Called from the machine's <c>_UnhandledInput</c> while this state is current.
    /// </summary>
    public virtual void HandleInput(InputEvent inputEvent) { }

    public void RequestTransition(StringName stateName)
    {
        EmitSignal(SignalName.TransitionRequested, stateName);
    }

    internal void EnterState(StringName previousState)
    {
        IsActive = true;
        Enter(previousState);
        EmitSignal(SignalName.Entered, previousState);
    }

    internal void ExitState(StringName nextState)
    {
        Exit(nextState);
        IsActive = false;
        EmitSignal(SignalName.Exited, nextState);
    }
}
