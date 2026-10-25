using Godot;

namespace Ouranos.Foundry.Demos.StateMachine;

public partial class IdleState : DemoPlayerState
{
    public override void PhysicsProcess(double delta)
    {
        if (ReadDirection() != Vector2.Zero)
        {
            RequestTransition("Move");
        }
    }
}
