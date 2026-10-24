using Godot;

namespace Ouranos.Foundry.Demos.StateMachine;

public partial class DashState : DemoPlayerState
{
    [Export]
    public float Speed { get; private set; } = 900f;

    [Export]
    public float Duration { get; private set; } = 0.15f;

    [Export]
    public MoveState? Move { get; private set; }

    private Vector2 _direction;
    private double _remaining;

    public override void Enter(StringName previousState)
    {
        base.Enter(previousState);
        var input = ReadDirection();
        _direction =
            input != Vector2.Zero ? input.Normalized() : Move?.LastDirection ?? Vector2.Right;
        _remaining = Duration;
    }

    public override void PhysicsProcess(double delta)
    {
        Player?.Position += _direction * Speed * (float)delta;
        _remaining -= delta;
        if (_remaining <= 0)
        {
            RequestTransition(ReadDirection() == Vector2.Zero ? "Idle" : "Move");
        }
    }
}
