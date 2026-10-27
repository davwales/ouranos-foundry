using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.StatusEffects.Nodes;
using Ouranos.Foundry.Features.StatusEffects.Resources;
using Ouranos.Foundry.Features.StatusEffects.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.StatusEffects;

[TestSuite]
[RequireGodotRuntime]
public class StatusEffectComponentTests
{
    private static (StatusEffectComponent component, List<string> log) CreateComponent(
        StatusEffectUpdateMode updateMode = StatusEffectUpdateMode.Manual
    )
    {
        var log = new List<string>();
        var component = new StatusEffectComponent().With(
            StatusEffectComponent.PropertyName.UpdateMode,
            (int)updateMode
        );
        component.EffectApplied += effect => log.Add($"Applied({effect.Id})");
        component.EffectRefreshed += effect => log.Add($"Refreshed({effect.Id})");
        component.EffectTicked += effect => log.Add($"Ticked({effect.Id})");
        component.EffectRemoved += (effect, reason) => log.Add($"Removed({effect.Id},{reason})");
        return (component, log);
    }

    private static StatusEffectDefinition MakeEffect(
        string id = "rage",
        StatusEffectDurationType durationType = StatusEffectDurationType.Timed,
        float duration = 10f,
        float tickInterval = 0f,
        StatusEffectStacking stacking = StatusEffectStacking.Refresh,
        int maxStacks = 0
    )
    {
        return new StatusEffectDefinition
        {
            Id = id,
            DurationType = durationType,
            Duration = duration,
            TickInterval = tickInterval,
            Stacking = stacking,
            MaxStacks = maxStacks,
        };
    }

    // ---- Lifecycle ----

    [TestCase]
    public void ApplyEffect_WhenNotActive_AddsItAndEmitsApplied()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var source = new Node();

        // Act
        var effect = component.ApplyEffect(MakeEffect(duration: 5f), source);

        // Assert
        effect.ShouldNotBeNull();
        effect.IsActive.ShouldBeTrue();
        effect.Stacks.ShouldBe(1);
        effect.Duration.ShouldBe(5d);
        effect.RemainingTime.ShouldBe(5d);
        effect.Source.ShouldBe(source);
        component.HasEffect("rage").ShouldBeTrue();
        component.GetEffect("rage").ShouldBe(effect);
        log.ShouldBe(["Applied(rage)"]);

        source.Free();
        component.Free();
    }

    [TestCase]
    public void ApplyEffect_WhenDefinitionIsNull_ReturnsNull()
    {
        // Arrange
        var (component, log) = CreateComponent();

        // Act
        var effect = component.ApplyEffect(null);

        // Assert
        effect.ShouldBeNull();
        component.GetActiveEffects().Length.ShouldBe(0);
        log.ShouldBeEmpty();

        component.Free();
    }

    [TestCase]
    public void ApplyEffect_WhenIdIsEmpty_ReturnsNull()
    {
        // Arrange
        var (component, log) = CreateComponent();

        // Act
        var effect = component.ApplyEffect(MakeEffect(id: ""));

        // Assert
        effect.ShouldBeNull();
        component.GetActiveEffects().Length.ShouldBe(0);
        log.ShouldBeEmpty();

        component.Free();
    }

    [TestCase]
    public void Advance_WhenDurationRunsOut_RemovesEffectAsExpired()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var effect = component.ApplyEffect(MakeEffect(duration: 10f))!;

        // Act
        component.Advance(9.75);
        var activeBeforeExpiry = component.HasEffect("rage");
        var remainingBeforeExpiry = effect.RemainingTime;
        component.Advance(0.25);

        // Assert
        activeBeforeExpiry.ShouldBeTrue();
        remainingBeforeExpiry.ShouldBe(0.25d);
        component.HasEffect("rage").ShouldBeFalse();
        effect.IsActive.ShouldBeFalse();
        effect.RemainingTime.ShouldBe(0d);
        log.ShouldBe(["Applied(rage)", "Removed(rage,Expired)"]);

        component.Free();
    }

    [TestCase]
    public void Advance_WhenEffectIsInfinite_NeverExpires()
    {
        // Arrange
        var (component, log) = CreateComponent();
        component.ApplyEffect(MakeEffect(durationType: StatusEffectDurationType.Infinite));

        // Act
        component.Advance(100_000);

        // Assert
        component.HasEffect("rage").ShouldBeTrue();
        log.ShouldBe(["Applied(rage)"]);

        component.Free();
    }

    [TestCase]
    public void ApplyEffect_WhenInstant_EmitsAppliedThenExpiredAndKeepsNothing()
    {
        // Arrange
        var (component, log) = CreateComponent();

        // Act
        var effect = component.ApplyEffect(
            MakeEffect(id: "heal", durationType: StatusEffectDurationType.Instant)
        );

        // Assert
        effect.ShouldNotBeNull();
        effect.IsActive.ShouldBeFalse();
        component.HasEffect("heal").ShouldBeFalse();
        log.ShouldBe(["Applied(heal)", "Removed(heal,Expired)"]);

        component.Free();
    }

    [TestCase]
    public void RemoveEffect_WhenActive_RemovesItAndReturnsTrue()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var effect = component.ApplyEffect(
            MakeEffect(durationType: StatusEffectDurationType.Infinite)
        )!;

        // Act
        var removed = component.RemoveEffect("rage");

        // Assert
        removed.ShouldBeTrue();
        effect.IsActive.ShouldBeFalse();
        component.HasEffect("rage").ShouldBeFalse();
        component.GetEffect("rage").ShouldBeNull();
        log.ShouldBe(["Applied(rage)", "Removed(rage,Removed)"]);

        component.Free();
    }

    [TestCase]
    public void RemoveEffect_WhenNotActive_ReturnsFalse()
    {
        // Arrange
        var (component, log) = CreateComponent();

        // Act
        var removed = component.RemoveEffect("rage");

        // Assert
        removed.ShouldBeFalse();
        log.ShouldBeEmpty();

        component.Free();
    }

    [TestCase]
    public void ClearEffects_RemovesEveryEffectInApplicationOrder()
    {
        // Arrange
        var (component, log) = CreateComponent();
        component.ApplyEffect(MakeEffect(id: "rage"));
        component.ApplyEffect(MakeEffect(id: "poison"));
        log.Clear();

        // Act
        component.ClearEffects();

        // Assert
        component.GetActiveEffects().Length.ShouldBe(0);
        log.ShouldBe(["Removed(rage,Removed)", "Removed(poison,Removed)"]);

        component.Free();
    }

    [TestCase]
    public void GetActiveEffects_ReturnsSnapshotSafeToRemoveFrom()
    {
        // Arrange
        var (component, _) = CreateComponent();
        component.ApplyEffect(MakeEffect(id: "rage"));
        component.ApplyEffect(MakeEffect(id: "poison"));
        component.ApplyEffect(MakeEffect(id: "haste"));
        var ids = new List<string>();

        // Act
        foreach (var effect in component.GetActiveEffects())
        {
            ids.Add(effect.Id);
            component.RemoveEffect(effect.Id);
        }

        // Assert
        ids.ShouldBe(["rage", "poison", "haste"]);
        component.GetActiveEffects().Length.ShouldBe(0);

        component.Free();
    }

    [TestCase]
    public void Hooks_RunJustBeforeTheirSignals()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var definition = new RecordingEffectDefinition
        {
            Id = "poison",
            Duration = 1f,
            TickInterval = 1f,
            Log = log,
        };

        // Act
        component.ApplyEffect(definition);
        component.ApplyEffect(definition);
        component.Advance(1);

        // Assert
        log.ShouldBe([
            "OnApplied",
            "Applied(poison)",
            "OnRefreshed",
            "Refreshed(poison)",
            "OnTicked",
            "Ticked(poison)",
            "OnRemoved(Expired)",
            "Removed(poison,Expired)",
        ]);

        component.Free();
    }

    // ---- Stacking ----

    [TestCase]
    public void ApplyEffect_WhenRefreshAndActive_RestartsDurationAndUpdatesSource()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var definition = MakeEffect(duration: 10f, stacking: StatusEffectStacking.Refresh);
        var first = component.ApplyEffect(definition)!;
        component.Advance(4);
        var secondSource = new Node();

        // Act
        var second = component.ApplyEffect(definition, secondSource);

        // Assert
        second.ShouldBe(first);
        first.Stacks.ShouldBe(1);
        first.RemainingTime.ShouldBe(10d);
        first.Duration.ShouldBe(10d);
        first.Source.ShouldBe(secondSource);
        component.GetActiveEffects().Length.ShouldBe(1);
        log.ShouldBe(["Applied(rage)", "Refreshed(rage)"]);

        secondSource.Free();
        component.Free();
    }

    [TestCase]
    public void ApplyEffect_WhenStackIntensity_AddsStackAndRestartsDuration()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var definition = MakeEffect(
            id: "poison",
            duration: 10f,
            stacking: StatusEffectStacking.StackIntensity
        );
        var effect = component.ApplyEffect(definition)!;
        component.Advance(4);

        // Act
        component.ApplyEffect(definition);
        component.ApplyEffect(definition);

        // Assert
        effect.Stacks.ShouldBe(3);
        effect.RemainingTime.ShouldBe(10d);
        log.ShouldBe(["Applied(poison)", "Refreshed(poison)", "Refreshed(poison)"]);

        component.Free();
    }

    [TestCase]
    public void ApplyEffect_WhenStackIntensityAtMaxStacks_KeepsStacksButRestartsDuration()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var definition = MakeEffect(
            id: "poison",
            duration: 10f,
            stacking: StatusEffectStacking.StackIntensity,
            maxStacks: 2
        );
        var effect = component.ApplyEffect(definition)!;
        component.ApplyEffect(definition);
        component.Advance(4);

        // Act
        component.ApplyEffect(definition);

        // Assert
        effect.Stacks.ShouldBe(2);
        effect.RemainingTime.ShouldBe(10d);
        log.ShouldBe(["Applied(poison)", "Refreshed(poison)", "Refreshed(poison)"]);

        component.Free();
    }

    [TestCase]
    public void ApplyEffect_WhenStackDuration_ExtendsRemainingTime()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var definition = MakeEffect(
            id: "haste",
            duration: 10f,
            stacking: StatusEffectStacking.StackDuration
        );
        var effect = component.ApplyEffect(definition)!;
        component.Advance(4);

        // Act
        component.ApplyEffect(definition);

        // Assert
        effect.Stacks.ShouldBe(1);
        effect.RemainingTime.ShouldBe(16d);
        effect.Duration.ShouldBe(20d);
        log.ShouldBe(["Applied(haste)", "Refreshed(haste)"]);

        component.Free();
    }

    [TestCase]
    public void ApplyEffect_WhenIgnore_ReturnsNullAndChangesNothing()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var definition = MakeEffect(duration: 10f, stacking: StatusEffectStacking.Ignore);
        var source = new Node();
        var effect = component.ApplyEffect(definition, source)!;
        component.Advance(4);

        // Act
        var result = component.ApplyEffect(definition);

        // Assert
        result.ShouldBeNull();
        effect.RemainingTime.ShouldBe(6d);
        effect.Source.ShouldBe(source);
        log.ShouldBe(["Applied(rage)"]);

        source.Free();
        component.Free();
    }

    [TestCase]
    public void ApplyEffect_WhenInstantAndSameIdActive_LeavesActiveEffectAlone()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var active = component.ApplyEffect(MakeEffect(id: "fire", duration: 10f))!;

        // Act
        component.ApplyEffect(
            MakeEffect(id: "fire", durationType: StatusEffectDurationType.Instant)
        );

        // Assert
        component.GetEffect("fire").ShouldBe(active);
        active.RemainingTime.ShouldBe(10d);
        log.ShouldBe(["Applied(fire)", "Applied(fire)", "Removed(fire,Expired)"]);

        component.Free();
    }

    // ---- Tick timing ----

    [TestCase]
    public void Advance_InSmallSteps_TicksEveryIntervalAndBeforeExpiry()
    {
        // Arrange
        var (component, log) = CreateComponent();
        component.ApplyEffect(MakeEffect(id: "poison", duration: 3f, tickInterval: 1f));
        log.Clear();
        var ticksAfterEachSecond = new List<int>();

        // Act
        for (var second = 0; second < 3; second++)
        {
            for (var step = 0; step < 4; step++)
            {
                component.Advance(0.25);
            }

            ticksAfterEachSecond.Add(log.FindAll(entry => entry == "Ticked(poison)").Count);
        }

        // Assert
        ticksAfterEachSecond.ShouldBe([1, 2, 3]);
        log.ShouldBe([
            "Ticked(poison)",
            "Ticked(poison)",
            "Ticked(poison)",
            "Removed(poison,Expired)",
        ]);

        component.Free();
    }

    [TestCase]
    public void Advance_WhenDeltaOvershootsExpiry_TicksOnlyUpToExpiry()
    {
        // Arrange
        var (component, log) = CreateComponent();
        component.ApplyEffect(MakeEffect(id: "poison", duration: 2f, tickInterval: 0.5f));
        log.Clear();

        // Act
        component.Advance(10);

        // Assert
        log.ShouldBe([
            "Ticked(poison)",
            "Ticked(poison)",
            "Ticked(poison)",
            "Ticked(poison)",
            "Removed(poison,Expired)",
        ]);

        component.Free();
    }

    [TestCase]
    public void Advance_WhenInfinite_TicksForEveryElapsedInterval()
    {
        // Arrange
        var (component, log) = CreateComponent();
        component.ApplyEffect(
            MakeEffect(
                id: "regen",
                durationType: StatusEffectDurationType.Infinite,
                tickInterval: 0.5f
            )
        );
        log.Clear();

        // Act
        component.Advance(0.75);
        var ticksAfterFirstAdvance = log.Count;
        component.Advance(1.25);

        // Assert
        ticksAfterFirstAdvance.ShouldBe(1);
        log.Count.ShouldBe(4);
        component.HasEffect("regen").ShouldBeTrue();

        component.Free();
    }

    [TestCase]
    public void Advance_WhenTickIntervalIsZero_NeverTicks()
    {
        // Arrange
        var (component, log) = CreateComponent();
        component.ApplyEffect(MakeEffect(duration: 5f, tickInterval: 0f));
        log.Clear();

        // Act
        component.Advance(5);

        // Assert
        log.ShouldBe(["Removed(rage,Expired)"]);

        component.Free();
    }

    [TestCase]
    public void ApplyEffect_WhenRefreshed_KeepsTickPhase()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var definition = MakeEffect(id: "poison", duration: 10f, tickInterval: 1f);
        component.ApplyEffect(definition);
        component.Advance(0.75);
        component.ApplyEffect(definition);
        log.Clear();

        // Act
        component.Advance(0.25);

        // Assert
        log.ShouldBe(["Ticked(poison)"]);

        component.Free();
    }

    [TestCase]
    public void Advance_WhenTickHandlerRemovesEffect_StopsTickingWithoutExpiring()
    {
        // Arrange
        var (component, log) = CreateComponent();
        component.ApplyEffect(MakeEffect(id: "poison", duration: 2f, tickInterval: 0.5f));
        component.EffectTicked += effect => component.RemoveEffect(effect.Id);
        log.Clear();

        // Act
        component.Advance(10);

        // Assert
        log.ShouldBe(["Ticked(poison)", "Removed(poison,Removed)"]);

        component.Free();
    }

    [TestCase]
    public void Advance_WhenTickHandlerReappliesEffect_KeepsItActive()
    {
        // Arrange
        var (component, log) = CreateComponent();
        var definition = MakeEffect(id: "poison", duration: 1f, tickInterval: 1f);
        var effect = component.ApplyEffect(definition)!;
        component.EffectTicked += _ => component.ApplyEffect(definition);
        log.Clear();

        // Act
        component.Advance(1);

        // Assert
        effect.IsActive.ShouldBeTrue();
        effect.RemainingTime.ShouldBe(1d);
        log.ShouldBe(["Ticked(poison)", "Refreshed(poison)"]);

        component.Free();
    }

    [TestCase]
    public void Process_WhenUpdateModeIsProcess_AdvancesByDelta()
    {
        // Arrange
        var (component, _) = CreateComponent(StatusEffectUpdateMode.Process);
        var effect = component.ApplyEffect(MakeEffect(duration: 10f))!;

        // Act
        component._Process(1);
        component._PhysicsProcess(1);

        // Assert
        effect.RemainingTime.ShouldBe(9d);

        component.Free();
    }

    [TestCase]
    public void PhysicsProcess_WhenUpdateModeIsPhysics_AdvancesByDelta()
    {
        // Arrange
        var (component, _) = CreateComponent(StatusEffectUpdateMode.Physics);
        var effect = component.ApplyEffect(MakeEffect(duration: 10f))!;

        // Act
        component._Process(1);
        component._PhysicsProcess(1);

        // Assert
        effect.RemainingTime.ShouldBe(9d);

        component.Free();
    }

    [TestCase]
    public void Process_WhenUpdateModeIsManual_DoesNotAdvance()
    {
        // Arrange
        var (component, _) = CreateComponent(StatusEffectUpdateMode.Manual);
        var effect = component.ApplyEffect(MakeEffect(duration: 10f))!;

        // Act
        component._Process(1);
        component._PhysicsProcess(1);

        // Assert
        effect.RemainingTime.ShouldBe(10d);

        component.Free();
    }
}
