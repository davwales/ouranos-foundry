using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.Health.Nodes;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Health;

[TestSuite]
[RequireGodotRuntime]
public class PlayerHealthBarTests
{
    private static (Node root, HealthComponent component, PlayerHealthBar bar) CreateBar(
        float baseMaxHealth = 100f,
        float startingHealth = -1f,
        float catchUpDuration = 0.5f,
        Vector2? barSize = null
    )
    {
        var root = new Node();
        var component = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, baseMaxHealth)
            .With(HealthComponent.PropertyName.StartingHealth, startingHealth);
        var bar = new PlayerHealthBar()
            .With(PlayerHealthBar.PropertyName.CatchUpDuration, catchUpDuration)
            .With(PlayerHealthBar.PropertyName.BarSize, barSize ?? new Vector2(300, 24));
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
    public void MonitorHealthComponent_ShouldSetInitialFill()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 200f, startingHealth: 100f);
        component._Ready();
        bar._Ready();

        // Act
        bar.MonitorHealthComponent(component);

        // Assert
        var bars = GetColorRectChildren(bar);
        var mainBar = bars[2];
        mainBar.Size.X.ShouldBe(0.5f * 300f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void HealthChanged_Damage_ShouldDropMainBarImmediately()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 100f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Act
        component.Damage(50f);

        // Assert
        var bars = GetColorRectChildren(bar);
        var mainBar = bars[2];
        mainBar.Size.X.ShouldBe(0.5f * 300f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void HealthChanged_Heal_ShouldJumpBothBarsToNewFill()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 50f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Act
        component.Heal(30f);

        // Assert
        var bars = GetColorRectChildren(bar);
        var lagBar = bars[1];
        var mainBar = bars[2];

        mainBar.Size.X.ShouldBe(0.8f * 300f, 0.01f);
        lagBar.Size.X.ShouldBe(0.8f * 300f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void HealthChanged_Damage_LagBarStaysAtOldPosition()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 100f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Act
        component.Damage(50f);

        // Assert
        var bars = GetColorRectChildren(bar);
        var lagBar = bars[1];
        var mainBar = bars[2];

        mainBar.Size.X.ShouldBe(0.5f * 300f, 0.01f);
        lagBar.Size.X.ShouldBe(1.0f * 300f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void MaxHealthChanged_ShouldRecalculateFill()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 80f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Act
        component.SetBaseMaxHealth(200f, clampCurrent: false);

        // Assert
        var bars = GetColorRectChildren(bar);
        var mainBar = bars[2];
        mainBar.Size.X.ShouldBe(0.4f * 300f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void MonitorHealthComponent_Null_ShouldZeroBothBars()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 80f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Act
        bar.MonitorHealthComponent(null);

        // Assert
        var bars = GetColorRectChildren(bar);
        var lagBar = bars[1];
        var mainBar = bars[2];

        mainBar.Size.X.ShouldBe(0f, 0.01f);
        lagBar.Size.X.ShouldBe(0f, 0.01f);

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
        var bar = new PlayerHealthBar().With(PlayerHealthBar.PropertyName.Target, component);
        root.AddChild(component);
        root.AddChild(bar);
        component._Ready();

        // Act
        bar._Ready();

        // Assert
        var bars = GetColorRectChildren(bar);
        var mainBar = bars[2];
        mainBar.Size.X.ShouldBe(0.75f * 300f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void CatchUpDuration_Zero_LagBarStaysOnDamage()
    {
        // Arrange
        var (root, component, bar) = CreateBar(
            baseMaxHealth: 100f,
            startingHealth: 100f,
            catchUpDuration: 0f
        );
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Act
        component.Damage(50f);

        // Assert — when CatchUpDuration is 0, _Process short-circuits,
        // so lag bar stays at old position (no catch-up runs)
        var bars = GetColorRectChildren(bar);
        var lagBar = bars[1];
        var mainBar = bars[2];

        mainBar.Size.X.ShouldBe(0.5f * 300f, 0.01f);
        lagBar.Size.X.ShouldBe(1.0f * 300f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void HealthChanged_MultipleDamages_LagBarStaysAtHighest()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 100f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Act
        component.Damage(30f);
        component.Damage(30f);

        // Assert
        var bars = GetColorRectChildren(bar);
        var mainBar = bars[2];
        mainBar.Size.X.ShouldBe(0.4f * 300f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void Ready_ShouldCreateBarsWithExportedColors()
    {
        // Arrange
        var mainColor = new Color(0f, 1f, 0f, 1f);
        var lagColor = new Color(0f, 0.5f, 0f, 1f);
        var bgColor = new Color(0f, 0f, 0f, 1f);

        var root = new Node();
        var component = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, 100f)
            .With(HealthComponent.PropertyName.StartingHealth, 100f);
        var bar = new PlayerHealthBar()
            .With(PlayerHealthBar.PropertyName.MainBarColor, mainColor)
            .With(PlayerHealthBar.PropertyName.LagBarColor, lagColor)
            .With(PlayerHealthBar.PropertyName.BackgroundColor, bgColor);
        root.AddChild(component);
        root.AddChild(bar);
        component._Ready();

        // Act
        bar._Ready();

        // Assert
        var bars = GetColorRectChildren(bar);
        bars[0].Color.ShouldBe(bgColor);
        bars[1].Color.ShouldBe(lagColor);
        bars[2].Color.ShouldBe(mainColor);

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

        var bar = new PlayerHealthBar();
        root.AddChild(component);
        root.AddChild(bar);
        component._Ready();
        bar._Ready();

        // Act
        bar.MonitorHealthComponent(component);

        // Assert
        var bars = GetColorRectChildren(bar);
        var mainBar = bars[2];
        mainBar.Size.X.ShouldBe(0f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void HealthChanged_DamageThenHeal_LagBarSyncsOnHeal()
    {
        // Arrange
        var (root, component, bar) = CreateBar(baseMaxHealth: 100f, startingHealth: 100f);
        component._Ready();
        bar._Ready();
        bar.MonitorHealthComponent(component);

        // Act
        component.Damage(60f);
        component.Heal(30f);

        // Assert
        var bars = GetColorRectChildren(bar);
        var lagBar = bars[1];
        var mainBar = bars[2];

        mainBar.Size.X.ShouldBe(0.7f * 300f, 0.01f);
        lagBar.Size.X.ShouldBe(0.7f * 300f, 0.01f);

        root.Free();
    }
}
