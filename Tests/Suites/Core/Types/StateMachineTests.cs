using System;
using System.Collections.Generic;
using GdUnit4;
using Ouranos.Foundry.Core.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Core.Types;

[TestSuite]
public class StateMachineTests
{
    private enum Mode
    {
        Idle,
        Move,
        Dash,
    }

    private static (StateMachine<Mode> machine, List<string> log) CreateMachine()
    {
        var log = new List<string>();
        var machine = new StateMachine<Mode>(Mode.Idle);
        foreach (var mode in Enum.GetValues<Mode>())
        {
            machine.Configure(
                mode,
                onEnter: previous => log.Add($"{mode}.Enter({previous})"),
                onExit: next => log.Add($"{mode}.Exit({next})"),
                onUpdate: _ => log.Add($"{mode}.Update")
            );
        }

        machine.StateChanged += (from, to) => log.Add($"StateChanged({from},{to})");
        return (machine, log);
    }

    [TestCase]
    public void Start_EntersInitialStateOnce()
    {
        // Arrange
        var (machine, log) = CreateMachine();

        // Act
        machine.Start();
        machine.Start();

        // Assert
        machine.IsStarted.ShouldBeTrue();
        machine.Current.ShouldBe(Mode.Idle);
        log.ShouldBe(["Idle.Enter(Idle)"]);
    }

    [TestCase]
    public void TransitionTo_RunsExitThenEnterThenEvent()
    {
        // Arrange
        var (machine, log) = CreateMachine();
        machine.Start();
        log.Clear();

        // Act
        var result = machine.TransitionTo(Mode.Move);

        // Assert
        result.ShouldBeTrue();
        machine.Current.ShouldBe(Mode.Move);
        log.ShouldBe(["Idle.Exit(Move)", "Move.Enter(Idle)", "StateChanged(Idle,Move)"]);
    }

    [TestCase]
    public void TransitionTo_WhenAlreadyCurrent_ReturnsFalse()
    {
        // Arrange
        var (machine, log) = CreateMachine();
        machine.Start();
        log.Clear();

        // Act
        var result = machine.TransitionTo(Mode.Idle);

        // Assert
        result.ShouldBeFalse();
        log.ShouldBeEmpty();
    }

    [TestCase]
    public void TransitionTo_WhenNotStarted_Throws()
    {
        // Arrange
        var (machine, _) = CreateMachine();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => machine.TransitionTo(Mode.Move));
    }

    [TestCase]
    public void TransitionTo_WhenRequestedDuringEnter_IsQueued()
    {
        // Arrange
        var log = new List<string>();
        var machine = new StateMachine<Mode>(Mode.Idle);
        machine.Configure(
            Mode.Dash,
            onEnter: _ =>
            {
                log.Add("Dash.Enter");
                machine.TransitionTo(Mode.Move);
                log.Add("Dash.Enter.End");
            }
        );
        machine.Configure(Mode.Move, onEnter: _ => log.Add("Move.Enter"));
        machine.Start();

        // Act
        machine.TransitionTo(Mode.Dash);

        // Assert
        log.ShouldBe(["Dash.Enter", "Dash.Enter.End", "Move.Enter"]);
        machine.Current.ShouldBe(Mode.Move);
    }

    [TestCase]
    public void TransitionTo_WhenTransitionsLoop_Throws()
    {
        // Arrange
        var machine = new StateMachine<Mode>(Mode.Idle);
        machine.Configure(Mode.Idle, onEnter: _ => machine.TransitionTo(Mode.Move));
        machine.Configure(Mode.Move, onEnter: _ => machine.TransitionTo(Mode.Idle));

        // Act & Assert
        Should.Throw<InvalidOperationException>(machine.Start);
    }

    [TestCase]
    public void Start_WhenInitialEnterRequestsTransition_AppliesItAfterEnter()
    {
        // Arrange
        var log = new List<string>();
        var machine = new StateMachine<Mode>(Mode.Idle);
        machine.Configure(
            Mode.Idle,
            onEnter: _ =>
            {
                machine.TransitionTo(Mode.Move);
                log.Add("Idle.Enter.End");
            }
        );
        machine.Configure(Mode.Move, onEnter: previous => log.Add($"Move.Enter({previous})"));

        // Act
        machine.Start();

        // Assert
        log.ShouldBe(["Idle.Enter.End", "Move.Enter(Idle)"]);
        machine.Current.ShouldBe(Mode.Move);
    }

    [TestCase]
    public void Update_RunsOnlyCurrentStateCallback()
    {
        // Arrange
        var (machine, log) = CreateMachine();
        machine.Start();
        machine.TransitionTo(Mode.Dash);
        log.Clear();

        // Act
        machine.Update(0.016);

        // Assert
        log.ShouldBe(["Dash.Update"]);
    }

    [TestCase]
    public void TransitionTo_WhenStateNotConfigured_StillChangesState()
    {
        // Arrange
        var machine = new StateMachine<string>("a");
        machine.Start();

        // Act
        var result = machine.TransitionTo("b");

        // Assert
        result.ShouldBeTrue();
        machine.Current.ShouldBe("b");
    }
}
