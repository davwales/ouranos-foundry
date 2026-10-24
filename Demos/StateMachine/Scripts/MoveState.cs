using Godot;

namespace Ouranos.Foundry.Demos.StateMachine;

public partial class MoveState : DemoPlayerState
{
    [Export]
    public float Speed { get; private set; } = 220f;

    public Vector2 LastDirection { get; private set; } = Vector2.Right;

    public override void PhysicsProcess(double delta)
    {
        var direction = ReadDirection();
        if (direction == Vector2.Zero)
        {
            RequestTransition("Idle");
            return;
        }

        LastDirection = direction.Normalized();
        Player?.Position += direction * Speed * (float)delta;
    }

    public override void HandleInput(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed("ui_accept"))
        {
            RequestTransition("Dash");
        }
    }
}
