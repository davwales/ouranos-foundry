using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.Health.Nodes;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Health;

[TestSuite]
[RequireGodotRuntime]
public class FloatingHealthBar2DTests
{
    private static (Node root, HealthComponent component, FloatingHealthBar2D bar) CreateBar(
        float baseMaxHealth = 100f,
        float startingHealth = -1f,
        float displayDuration = 2.0f
    )
    {
        var root = new Node();
        var component = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, baseMaxHealth)
            .With(HealthComponent.PropertyName.StartingHealth, startingHealth);
        var bar = new FloatingHealthBar2D().With(
            FloatingHealthBar2D.PropertyName.DisplayDuration,
            displayDuration
        );
        root.AddChild(component);
        root.AddChild(bar);
        return (root, component, bar);
    }

    private static List<ColorRect> GetColorRectChildren(Node node)
    {
        var result = new List<ColorRect>();
        foreach (var child in node.GetChildren())
        {
            if (child is ColorRect cr)
            {
                result.Add(cr);
            }
        }

        return result;
    }

    [TestCase]
    public void Ready_ShouldStartInvisible()
    {
        // Arrange
        var (root, _, bar) = CreateBar();
        bar._Ready();

        // Assert
        bar.Visible.ShouldBeFalse();

        root.Free();
    }

    [TestCase]
    public void HealthDecreased_ShouldMakeBarVisible()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 100f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Act
        component.Damage(10f);

        // Assert
        bar.Visible.ShouldBeTrue();

        root.Free();
    }

    [TestCase]
    public void HealthChanged_ShouldUpdateFill()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 100f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        component.Damage(10f);

        // Act
        component.Damage(20f);

        // Assert
        var bars = GetColorRectChildren(bar);
        var fillBar = bars[1];
        fillBar.Size.X.ShouldBe(0.7f * 64f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void MaxHealthChanged_ShouldUpdateFillButNotMakeVisible()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 80f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Bar should still be invisible
        bar.Visible.ShouldBeFalse();

        // Act
        component.SetBaseMaxHealth(200f, clampCurrent: false);

        // Assert
        var bars = GetColorRectChildren(bar);
        var fillBar = bars[1];
        fillBar.Size.X.ShouldBe(0.4f * 64f, 0.01f);
        bar.Visible.ShouldBeFalse();

        root.Free();
    }

    [TestCase]
    public void MonitorHealthComponent_Null_ShouldHideBar()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 100f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        component.Damage(10f);
        bar.Visible.ShouldBeTrue();

        // Act
        bar.MonitorHealthComponent(null);

        // Assert
        bar.Visible.ShouldBeFalse();

        root.Free();
    }

    [TestCase]
    public void ExitTree_ShouldUnsubscribe_NoErrorsOnSubsequentHealthChanged()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 100f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Act
        bar._ExitTree();

        // Assert
        Should.NotThrow(() => component.Damage(10f));

        root.Free();
    }

    [TestCase]
    public void Target_ExportAutoWire_ShouldMonitorInReady()
    {
        // Arrange
        var root = new Node();
        var component = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, 100f)
            .With(HealthComponent.PropertyName.StartingHealth, 75f);
        var bar = new FloatingHealthBar2D().With(
            FloatingHealthBar2D.PropertyName.Target,
            component
        );
        root.AddChild(component);
        root.AddChild(bar);
        component._Ready();

        // Act
        bar._Ready();

        // Assert
        var bars = GetColorRectChildren(bar);
        var fillBar = bars[1];
        fillBar.Size.X.ShouldBe(0.75f * 64f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void Ready_ShouldCreateBarsWithExportedColors()
    {
        // Arrange
        var fullColor = new Color(0f, 1f, 0f, 1f);
        var emptyColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        var root = new Node();
        var bar = new FloatingHealthBar2D()
            .With(FloatingHealthBar2D.PropertyName.FullColor, fullColor)
            .With(FloatingHealthBar2D.PropertyName.EmptyColor, emptyColor);
        root.AddChild(bar);

        // Act
        bar._Ready();

        // Assert
        var bars = GetColorRectChildren(bar);
        bars[0].Color.ShouldBe(emptyColor);
        bars[1].Color.ShouldBe(fullColor);

        root.Free();
    }

    [TestCase]
    public void HealthDecreased_ShouldResetModulateToWhite()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 100f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        bar.Modulate = new Color(1f, 1f, 1f, 0.5f);

        // Act
        component.Damage(10f);

        // Assert
        bar.Modulate.ShouldBe(Colors.White);

        root.Free();
    }

    [TestCase]
    public void MonitorHealthComponent_ZeroMaxHealth_ShouldSetFillToZero()
    {
        // Arrange
        var root = new Node();
        var component = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, 0f)
            .With(HealthComponent.PropertyName.StartingHealth, 0f);
        component.MaxHealthProvider = () => 0f;
        var bar = new FloatingHealthBar2D();
        root.AddChild(component);
        root.AddChild(bar);
        component._Ready();
        bar._Ready();

        // Act
        bar.MonitorHealthComponent(component);

        // Assert
        var bars = GetColorRectChildren(bar);
        var fillBar = bars[1];
        fillBar.Size.X.ShouldBe(0f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void HealthChanged_HealOnly_ShouldNotMakeBarVisible()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 50f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Bar starts invisible
        bar.Visible.ShouldBeFalse();

        component.Heal(20f);

        // Assert
        bar.Visible.ShouldBeFalse();

        root.Free();
    }

    [TestCase]
    public void MonitorHealthComponent_ShouldUseBarSizeForFill()
    {
        // Arrange
        var root = new Node();
        var component = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, 100f)
            .With(HealthComponent.PropertyName.StartingHealth, 50f);
        var bar = new FloatingHealthBar2D().With(
            FloatingHealthBar2D.PropertyName.BarSize,
            new Vector2(100, 10)
        );
        root.AddChild(component);
        root.AddChild(bar);
        component._Ready();
        bar._Ready();

        // Act
        bar.MonitorHealthComponent(component);

        // Assert
        var bars = GetColorRectChildren(bar);
        var fillBar = bars[1];
        fillBar.Size.X.ShouldBe(50f, 0.01f);

        root.Free();
    }
}
