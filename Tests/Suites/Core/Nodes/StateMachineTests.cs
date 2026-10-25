using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Core.Nodes;
using Shouldly;

namespace Ouranos.Foundry.Tests.Core.Nodes;

[TestSuite]
[RequireGodotRuntime]
public class StateMachineTests
{
    private static (StateMachine machine, List<string> log) CreateMachine(
        params string[] stateNames
    )
    {
        var log = new List<string>();
        var machine = new StateMachine();
        foreach (var name in stateNames)
        {
            machine.AddChild(new RecordingState { Name = name, Log = log });
        }

        machine.StateChanged += (from, to) => log.Add($"StateChanged({from},{to})");
        return (machine, log);
    }

    private static RecordingState StateOf(StateMachine machine, string name)
    {
        return machine.GetNode<RecordingState>(name);
    }

    [TestCase]
    public void Ready_WhenNoInitialState_EntersFirstStateChild()
    {
        // Arrange
        var (machine, log) = CreateMachine("Idle", "Move");

        // Act
        machine._Ready();

        // Assert
        machine.CurrentState.ShouldBe(StateOf(machine, "Idle"));
        machine.PreviousState.ShouldBeNull();
        StateOf(machine, "Idle").IsActive.ShouldBeTrue();
        log.ShouldBe(["Idle.Enter()", "StateChanged(,Idle)"]);

        machine.Free();
    }

    [TestCase]
    public void Ready_WhenInitialStateSet_EntersIt()
    {
        // Arrange
        var (machine, _) = CreateMachine("Idle", "Move");
        machine.With(StateMachine.PropertyName.InitialState, StateOf(machine, "Move"));

        // Act
        machine._Ready();

        // Assert
        machine.CurrentState.ShouldBe(StateOf(machine, "Move"));

        machine.Free();
    }

    [TestCase]
    public void Ready_WhenNoStateChildren_HasNoCurrentState()
    {
        // Arrange
        var machine = new StateMachine();
        machine.AddChild(new Node { Name = "NotAState" });

        // Act
        machine._Ready();

        // Assert
        machine.CurrentState.ShouldBeNull();
        machine.States.Count.ShouldBe(0);

        machine.Free();
    }

    [TestCase]
    public void TransitionTo_WhenStateExists_RunsExitThenEnterThenSignal()
    {
        // Arrange
        var (machine, log) = CreateMachine("Idle", "Move");
        machine._Ready();
        log.Clear();

        // Act
        var result = machine.TransitionTo("Move");

        // Assert
        result.ShouldBeTrue();
        log.ShouldBe(["Idle.Exit(Move)", "Move.Enter(Idle)", "StateChanged(Idle,Move)"]);
        machine.CurrentState.ShouldBe(StateOf(machine, "Move"));
        machine.PreviousState.ShouldBe(StateOf(machine, "Idle"));
        StateOf(machine, "Idle").IsActive.ShouldBeFalse();
        StateOf(machine, "Move").IsActive.ShouldBeTrue();

        machine.Free();
    }

    [TestCase]
    public void TransitionTo_EmitsStateSignals()
    {
        // Arrange
        var (machine, _) = CreateMachine("Idle", "Move");
        machine._Ready();
        var signals = new List<string>();
        StateOf(machine, "Idle").Exited += next => signals.Add($"Idle exited to {next}");
        StateOf(machine, "Move").Entered += previous =>
            signals.Add($"Move entered from {previous}");

        // Act
        machine.TransitionTo("Move");

        // Assert
        signals.ShouldBe(["Idle exited to Move", "Move entered from Idle"]);

        machine.Free();
    }

    [TestCase]
    public void TransitionTo_WhenStateUnknown_ReturnsFalseAndKeepsState()
    {
        // Arrange
        var (machine, log) = CreateMachine("Idle");
        machine._Ready();
        log.Clear();

        // Act
        var result = machine.TransitionTo("Missing");

        // Assert
        result.ShouldBeFalse();
        machine.CurrentState.ShouldBe(StateOf(machine, "Idle"));
        log.ShouldBeEmpty();

        machine.Free();
    }

    [TestCase]
    public void TransitionTo_WhenAlreadyCurrent_ReturnsFalseWithoutCallbacks()
    {
        // Arrange
        var (machine, log) = CreateMachine("Idle", "Move");
        machine._Ready();
        log.Clear();

        // Act
        var result = machine.TransitionTo("Idle");

        // Assert
        result.ShouldBeFalse();
        log.ShouldBeEmpty();

        machine.Free();
    }

    [TestCase]
    public void TransitionTo_WhenRequestedDuringEnter_QueuesUntilCurrentTransitionCompletes()
    {
        // Arrange
        var (machine, log) = CreateMachine("Idle", "Dash", "Move");
        StateOf(machine, "Dash").TransitionOnEnter = "Move";
        machine._Ready();
        log.Clear();

        // Act
        machine.TransitionTo("Dash");

        // Assert
        log.ShouldBe([
            "Idle.Exit(Dash)",
            "Dash.Enter(Idle)",
            "StateChanged(Idle,Dash)",
            "Dash.Exit(Move)",
            "Move.Enter(Dash)",
            "StateChanged(Dash,Move)",
        ]);
        machine.CurrentState.ShouldBe(StateOf(machine, "Move"));

        machine.Free();
    }

    [TestCase]
    public void TransitionTo_WhenRequestedFromStateChangedHandler_IsQueued()
    {
        // Arrange
        var (machine, log) = CreateMachine("Idle", "Move", "Dash");
        machine._Ready();
        machine.StateChanged += (_, to) =>
        {
            if (to == "Move")
            {
                machine.TransitionTo("Dash");
            }
        };
        log.Clear();

        // Act
        machine.TransitionTo("Move");

        // Assert
        log.ShouldBe([
            "Idle.Exit(Move)",
            "Move.Enter(Idle)",
            "StateChanged(Idle,Move)",
            "Move.Exit(Dash)",
            "Dash.Enter(Move)",
            "StateChanged(Move,Dash)",
        ]);

        machine.Free();
    }

    [TestCase]
    public void RequestTransition_FromChildState_ChangesState()
    {
        // Arrange
        var (machine, _) = CreateMachine("Idle", "Move");
        machine._Ready();

        // Act
        StateOf(machine, "Idle").RequestTransition("Move");

        // Assert
        machine.CurrentState.ShouldBe(StateOf(machine, "Move"));

        machine.Free();
    }

    [TestCase]
    public void TransitionTo_WhenCalledThroughGodot_ChangesStateAndExposesIsActive()
    {
        // Arrange
        var (machine, _) = CreateMachine("Idle", "Move");
        machine._Ready();

        // Act
        var result = machine.Call(StateMachine.MethodName.TransitionTo, "Move");

        // Assert
        result.AsBool().ShouldBeTrue();
        StateOf(machine, "Move").Get(State.PropertyName.IsActive).AsBool().ShouldBeTrue();

        machine.Free();
    }

    [TestCase]
    public void Callbacks_AreForwardedOnlyToCurrentState()
    {
        // Arrange
        var (machine, log) = CreateMachine("Idle", "Move");
        machine._Ready();
        log.Clear();
        var inputEvent = new InputEventAction { Action = "ui_accept", Pressed = true };

        // Act
        machine._Process(0.016);
        machine._PhysicsProcess(0.016);
        machine._UnhandledInput(inputEvent);

        // Assert
        log.ShouldBe(["Idle.Process", "Idle.PhysicsProcess", "Idle.HandleInput"]);

        machine.Free();
    }

    [TestCase]
    public void StateDebugLabel_ShowsCurrentStateAndUpdatesOnTransition()
    {
        // Arrange
        var (machine, _) = CreateMachine("Idle", "Move");
        machine._Ready();
        var label = new StateDebugLabel();
        machine.AddChild(label);

        // Act
        label.MonitorStateMachine(machine);
        var initialText = label.Text;
        machine.TransitionTo("Move");

        // Assert
        initialText.ShouldBe("State: Idle");
        label.Text.ShouldBe("State: Move");

        machine.Free();
    }

    [TestCase]
    public void StateDebugLabel_WhenNoMachine_ShowsNone()
    {
        // Arrange
        var label = new StateDebugLabel();

        // Act
        label._Ready();

        // Assert
        label.Text.ShouldBe("State: (none)");

        label.Free();
    }
}
