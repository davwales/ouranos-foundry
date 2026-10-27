using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.StatusEffects.Nodes;
using Ouranos.Foundry.Features.StatusEffects.Resources;
using Ouranos.Foundry.Features.StatusEffects.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.StatusEffects;

[TestSuite]
[RequireGodotRuntime]
public class StatusEffectBarTests
{
    private static (Node root, StatusEffectComponent component, StatusEffectBar bar) CreateBar()
    {
        var root = new Node();
        var component = new StatusEffectComponent().With(
            StatusEffectComponent.PropertyName.UpdateMode,
            (int)StatusEffectUpdateMode.Manual
        );
        var bar = new StatusEffectBar().With(StatusEffectBar.PropertyName.Target, component);
        root.AddChild(component);
        root.AddChild(bar);
        return (root, component, bar);
    }

    private static StatusEffectDefinition MakeEffect(
        string id = "rage",
        StatusEffectDurationType durationType = StatusEffectDurationType.Timed,
        StatusEffectStacking stacking = StatusEffectStacking.Refresh
    )
    {
        return new StatusEffectDefinition
        {
            Id = id,
            DisplayName = "Rage",
            Description = "Doubles damage.",
            DurationType = durationType,
            Duration = 10f,
            Stacking = stacking,
        };
    }

    private static string LabelText(Node entry)
    {
        return entry.GetNode<Label>("Label").Text;
    }

    [TestCase]
    public void Ready_WhenTargetHasEffects_ShowsAnEntryForEach()
    {
        // Arrange
        var (root, component, bar) = CreateBar();
        component.ApplyEffect(MakeEffect(id: "rage"));
        component.ApplyEffect(MakeEffect(id: "poison"));

        // Act
        bar._Ready();

        // Assert
        bar.GetChildCount().ShouldBe(2);

        root.Free();
    }

    [TestCase]
    public void EffectApplied_AddsEntryWithTooltipAndCountdown()
    {
        // Arrange
        var (root, component, bar) = CreateBar();
        bar._Ready();

        // Act
        component.ApplyEffect(MakeEffect());

        // Assert
        bar.GetChildCount().ShouldBe(1);
        var entry = bar.GetChild<Control>(0);
        entry.TooltipText.ShouldBe("Rage\nDoubles damage.");
        LabelText(entry).ShouldBe("10");

        root.Free();
    }

    [TestCase]
    public void Process_UpdatesCountdownAndStacks()
    {
        // Arrange
        var (root, component, bar) = CreateBar();
        bar._Ready();
        var definition = MakeEffect(stacking: StatusEffectStacking.StackIntensity);
        component.ApplyEffect(definition);
        component.ApplyEffect(definition);
        component.Advance(5.75);

        // Act
        bar._Process(0);

        // Assert
        LabelText(bar.GetChild(0)).ShouldBe("x2 4.3");

        root.Free();
    }

    [TestCase]
    public void EffectRemoved_RemovesItsEntry()
    {
        // Arrange
        var (root, component, bar) = CreateBar();
        bar._Ready();
        component.ApplyEffect(MakeEffect(id: "rage"));
        component.ApplyEffect(MakeEffect(id: "poison"));

        // Act
        component.RemoveEffect("rage");
        component.Advance(10);

        // Assert
        bar.GetChildCount().ShouldBe(0);

        root.Free();
    }

    [TestCase]
    public void EffectApplied_WhenInstant_AddsNoEntry()
    {
        // Arrange
        var (root, component, bar) = CreateBar();
        bar._Ready();

        // Act
        component.ApplyEffect(MakeEffect(durationType: StatusEffectDurationType.Instant));

        // Assert
        bar.GetChildCount().ShouldBe(0);

        root.Free();
    }

    [TestCase]
    public void MonitorStatusEffectComponent_WhenNull_ClearsEntriesAndStopsListening()
    {
        // Arrange
        var (root, component, bar) = CreateBar();
        bar._Ready();
        component.ApplyEffect(MakeEffect(id: "rage"));

        // Act
        bar.MonitorStatusEffectComponent(null);
        component.ApplyEffect(MakeEffect(id: "poison"));

        // Assert
        bar.GetChildCount().ShouldBe(0);

        root.Free();
    }

    [TestCase]
    public void ExitTree_WithEntries_FreesThemAndStopsListening()
    {
        // Arrange
        var (root, component, bar) = CreateBar();
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(root);
        component.ApplyEffect(MakeEffect(id: "rage"));

        // Act
        root.RemoveChild(bar);
        component.ApplyEffect(MakeEffect(id: "poison"));

        // Assert
        bar.GetChildCount().ShouldBe(1);
        bar.GetChild(0).IsQueuedForDeletion().ShouldBeTrue();

        bar.Free();
        root.Free();
    }

    [TestCase]
    public void FormatLabel_FormatsStacksAndRemainingTime()
    {
        // Arrange
        var component = new StatusEffectComponent();
        var timed = component.ApplyEffect(
            MakeEffect(id: "timed", stacking: StatusEffectStacking.StackIntensity)
        )!;
        var infinite = component.ApplyEffect(
            MakeEffect(
                id: "infinite",
                durationType: StatusEffectDurationType.Infinite,
                stacking: StatusEffectStacking.StackIntensity
            )
        )!;

        // Act
        var timedFresh = StatusEffectBar.FormatLabel(timed);
        component.Advance(0.03125);
        var timedJustUnderTen = StatusEffectBar.FormatLabel(timed);
        component.Advance(0.46875);
        var timedUnderTen = StatusEffectBar.FormatLabel(timed);
        var infiniteSingle = StatusEffectBar.FormatLabel(infinite);
        component.ApplyEffect(infinite.Definition);
        var infiniteStacked = StatusEffectBar.FormatLabel(infinite);

        // Assert
        timedFresh.ShouldBe("10");
        timedJustUnderTen.ShouldBe("10");
        timedUnderTen.ShouldBe("9.5");
        infiniteSingle.ShouldBe("");
        infiniteStacked.ShouldBe("x2");

        component.Free();
    }
}
