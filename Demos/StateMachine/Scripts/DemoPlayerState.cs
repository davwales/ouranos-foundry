using Godot;
using Ouranos.Foundry.Core.Nodes;

namespace Ouranos.Foundry.Demos.StateMachine;

/// <summary>
/// Shared wiring for the demo's states: the body they move and the colour they show.
/// </summary>
public partial class DemoPlayerState : State
{
    [Export]
    public Node2D? Player { get; private set; }

    [Export]
    public ColorRect? Body { get; private set; }

    [Export]
    public Color Tint { get; private set; } = Colors.White;

    public override void Enter(StringName previousState)
    {
        Body?.Color = Tint;
    }

    protected static Vector2 ReadDirection()
    {
        return Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
    }
}
