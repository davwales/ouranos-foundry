using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Core.Nodes;

namespace Ouranos.Foundry.Tests.Core.Nodes;

/// <summary>
/// Appends every lifecycle call to a shared log and can request a transition on enter.
/// </summary>
public partial class RecordingState : State
{
    public List<string> Log { get; set; } = [];

    public StringName? TransitionOnEnter { get; set; }

    public override void Enter(StringName previousState)
    {
        Log.Add($"{Name}.Enter({previousState})");
        if (TransitionOnEnter is not null)
        {
            RequestTransition(TransitionOnEnter);
        }
    }

    public override void Exit(StringName nextState)
    {
        Log.Add($"{Name}.Exit({nextState})");
    }

    public override void Process(double delta)
    {
        Log.Add($"{Name}.Process");
    }

    public override void PhysicsProcess(double delta)
    {
        Log.Add($"{Name}.PhysicsProcess");
    }

    public override void HandleInput(InputEvent inputEvent)
    {
        Log.Add($"{Name}.HandleInput");
    }
}
