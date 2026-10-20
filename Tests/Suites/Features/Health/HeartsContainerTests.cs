using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.Health.Nodes;
using Ouranos.Foundry.Features.Health.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Health;

[TestSuite]
[RequireGodotRuntime]
public class HeartsContainerTests
{
    private static (
        Node root,
        HealthComponent component,
        HeartsContainer container
    ) CreateContainer(
        float baseMaxHealth = 100f,
        float startingHealth = -1f,
        float healthPointsPerHeart = 20f
    )
    {
        var root = new Node();
        var component = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, baseMaxHealth)
            .With(HealthComponent.PropertyName.StartingHealth, startingHealth);
        var container = new HeartsContainer().With(
            HeartsContainer.PropertyName.HealthPointsPerHeart,
            healthPointsPerHeart
        );
        root.AddChild(component);
        root.AddChild(container);
        return (root, component, container);
    }

    private static List<T> GetChildrenOfType<T>(Node node)
        where T : Node
    {
        var result = new List<T>();
        foreach (var child in node.GetChildren())
        {
            if (child is T typed)
            {
                result.Add(typed);
            }
        }

        return result;
    }

    [TestCase]
    public void RebuildHearts_FullHealth_ShouldCreateAllFullHearts()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 60f,
            startingHealth: 60f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChildCount().ShouldBe(3);
        for (int i = 0; i < container.GetChildCount(); i++)
        {
            var heart = container.GetChild(i);
            heart.ShouldBeOfType<ColorRect>();
            ((ColorRect)heart).Color.ShouldBe(new Color(0.9f, 0.1f, 0.1f, 1f));
        }

        root.Free();
    }

    [TestCase]
    public void RebuildHearts_HalfHealth_ShouldCreateHalfHeart()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 40f,
            startingHealth: 10f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChildCount().ShouldBe(2);
        ((ColorRect)container.GetChild(0)).Color.ShouldBe(new Color(0.45f, 0.05f, 0.05f, 1f));
        ((ColorRect)container.GetChild(1)).Color.ShouldBe(new Color(0.15f, 0.15f, 0.15f, 1f));

        root.Free();
    }

    [TestCase]
    public void RebuildHearts_MixedHearts_ShouldShowFullHalfEmpty()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 60f,
            startingHealth: 50f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChildCount().ShouldBe(3);
        ((ColorRect)container.GetChild(0)).Color.ShouldBe(new Color(0.9f, 0.1f, 0.1f, 1f));
        ((ColorRect)container.GetChild(1)).Color.ShouldBe(new Color(0.9f, 0.1f, 0.1f, 1f));
        ((ColorRect)container.GetChild(2)).Color.ShouldBe(new Color(0.45f, 0.05f, 0.05f, 1f));

        root.Free();
    }

    [TestCase]
    public void RebuildHearts_ZeroHealth_ShouldCreateAllEmptyHearts()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 40f,
            startingHealth: 0f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChildCount().ShouldBe(2);
        ((ColorRect)container.GetChild(0)).Color.ShouldBe(new Color(0.15f, 0.15f, 0.15f, 1f));
        ((ColorRect)container.GetChild(1)).Color.ShouldBe(new Color(0.15f, 0.15f, 0.15f, 1f));

        root.Free();
    }

    [TestCase]
    public void RebuildHearts_NonDivisibleMax_ShouldCeilDivide()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 50f,
            startingHealth: 50f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChildCount().ShouldBe(3);

        root.Free();
    }

    [TestCase]
    public void RebuildHearts_ZeroPointsPerHeart_ShouldRenderNoHearts()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 100f,
            startingHealth: 100f,
            healthPointsPerHeart: 0f
        );
        component._Ready();
        container._Ready();

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChildCount().ShouldBe(0);

        root.Free();
    }

    [TestCase]
    public void RebuildHearts_NegativePointsPerHeart_ShouldRenderNoHearts()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 100f,
            startingHealth: 100f,
            healthPointsPerHeart: -5f
        );
        component._Ready();
        container._Ready();

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChildCount().ShouldBe(0);

        root.Free();
    }

    [TestCase]
    public void MonitorHealthComponent_ShouldCreateHeartsForCurrentHealth()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 100f,
            startingHealth: 80f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChildCount().ShouldBe(5);

        root.Free();
    }

    [TestCase]
    public void MonitorHealthComponent_Null_ShouldClearHearts()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 60f,
            startingHealth: 60f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();
        container.MonitorHealthComponent(component);
        container.GetChildCount().ShouldBe(3);

        // Act
        container.MonitorHealthComponent(null);

        // Assert
        container.GetChildCount().ShouldBe(0);

        root.Free();
    }

    [TestCase]
    public void HealthChanged_Signal_ShouldTriggerRebuild()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 60f,
            startingHealth: 60f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();
        container.MonitorHealthComponent(component);
        container.GetChildCount().ShouldBe(3);

        // Act
        component.Damage(30f);

        // Assert
        ((ColorRect)container.GetChild(0)).Color.ShouldBe(new Color(0.9f, 0.1f, 0.1f, 1f));
        ((ColorRect)container.GetChild(1)).Color.ShouldBe(new Color(0.45f, 0.05f, 0.05f, 1f));
        ((ColorRect)container.GetChild(2)).Color.ShouldBe(new Color(0.15f, 0.15f, 0.15f, 1f));

        root.Free();
    }

    [TestCase]
    public void MaxHealthChanged_Signal_ShouldTriggerRebuild()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 60f,
            startingHealth: 60f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();
        container.MonitorHealthComponent(component);
        container.GetChildCount().ShouldBe(3);

        // Act
        component.SetBaseMaxHealth(100f, clampCurrent: false);

        // Assert
        container.GetChildCount().ShouldBe(5);

        root.Free();
    }

    [TestCase]
    public void HeartVisuals_WithFullTexture_ShouldCreateTextureRect()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 20f,
            startingHealth: 20f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        var texture = new PlaceholderTexture2D();
        container.HeartVisuals = new List<HeartVisualDefinition> { new(FullTexture: texture) };

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChild(0).ShouldBeOfType<TextureRect>();
        ((TextureRect)container.GetChild(0)).Texture.ShouldBe(texture);

        root.Free();
    }

    [TestCase]
    public void HeartVisuals_WithCustomColor_ShouldCreateColorRectWithThatColor()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 20f,
            startingHealth: 20f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        var customColor = new Color(1f, 0f, 0f, 1f);
        container.HeartVisuals = new List<HeartVisualDefinition> { new(FullColor: customColor) };

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChild(0).ShouldBeOfType<ColorRect>();
        ((ColorRect)container.GetChild(0)).Color.ShouldBe(customColor);

        root.Free();
    }

    [TestCase]
    public void HeartVisuals_ShorterThanHeartCount_LaterHeartsUseDefaults()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 60f,
            startingHealth: 60f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        var customColor = new Color(1f, 0f, 0f, 1f);
        container.HeartVisuals = new List<HeartVisualDefinition> { new(FullColor: customColor) };

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChild(0).ShouldBeOfType<ColorRect>();
        ((ColorRect)container.GetChild(0)).Color.ShouldBe(customColor);

        container.GetChild(1).ShouldBeOfType<ColorRect>();
        ((ColorRect)container.GetChild(1)).Color.ShouldBe(new Color(0.9f, 0.1f, 0.1f, 1f));

        container.GetChild(2).ShouldBeOfType<ColorRect>();
        ((ColorRect)container.GetChild(2)).Color.ShouldBe(new Color(0.9f, 0.1f, 0.1f, 1f));

        root.Free();
    }

    [TestCase]
    public void ExitTree_ShouldUnsubscribe_NoErrorsOnSubsequentHealthChanged()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 60f,
            startingHealth: 60f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();
        container.MonitorHealthComponent(component);

        // Act
        container._ExitTree();

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
            .With(HealthComponent.PropertyName.BaseMaxHealth, 40f)
            .With(HealthComponent.PropertyName.StartingHealth, 40f);
        var container = new HeartsContainer()
            .With(HeartsContainer.PropertyName.HealthPointsPerHeart, 20f)
            .With(HeartsContainer.PropertyName.Target, component);
        root.AddChild(component);
        root.AddChild(container);
        component._Ready();

        // Act
        container._Ready();

        // Assert
        container.GetChildCount().ShouldBe(2);

        root.Free();
    }

    [TestCase]
    public void HeartVisuals_TextureTakesPriorityOverColor_ShouldCreateTextureRect()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 20f,
            startingHealth: 20f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        var texture = new PlaceholderTexture2D();
        var customColor = new Color(1f, 0f, 0f, 1f);
        container.HeartVisuals = new List<HeartVisualDefinition>
        {
            new(FullColor: customColor, FullTexture: texture),
        };

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChild(0).ShouldBeOfType<TextureRect>();

        root.Free();
    }

    [TestCase]
    public void HeartVisuals_EmptyTexture_ShouldCreateTextureRectForEmptyHeart()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 40f,
            startingHealth: 0f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        var emptyTexture = new PlaceholderTexture2D();
        container.HeartVisuals = new List<HeartVisualDefinition>
        {
            new(),
            new(EmptyTexture: emptyTexture),
        };

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        container.GetChild(0).ShouldBeOfType<ColorRect>();
        container.GetChild(1).ShouldBeOfType<TextureRect>();
        ((TextureRect)container.GetChild(1)).Texture.ShouldBe(emptyTexture);

        root.Free();
    }

    [TestCase]
    public void RebuildHearts_EachHeart_ShouldHaveCustomMinimumSize()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 20f,
            startingHealth: 20f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();

        // Act
        container.MonitorHealthComponent(component);

        // Assert
        var heart = (Control)container.GetChild(0);
        heart.CustomMinimumSize.ShouldBe(new Vector2(24, 24));

        root.Free();
    }

    [TestCase]
    public void HealthChanged_DamageThenHeal_ShouldUpdateHearts()
    {
        // Arrange
        var (root, component, container) = CreateContainer(
            baseMaxHealth: 40f,
            startingHealth: 40f,
            healthPointsPerHeart: 20f
        );
        component._Ready();
        container._Ready();
        container.MonitorHealthComponent(component);

        // Act
        component.Damage(20f);

        // Assert
        container.GetChildCount().ShouldBe(2);
        ((ColorRect)container.GetChild(0)).Color.ShouldBe(new Color(0.9f, 0.1f, 0.1f, 1f));
        ((ColorRect)container.GetChild(1)).Color.ShouldBe(new Color(0.15f, 0.15f, 0.15f, 1f));

        component.Heal(20f);

        // Assert
        ((ColorRect)container.GetChild(0)).Color.ShouldBe(new Color(0.9f, 0.1f, 0.1f, 1f));
        ((ColorRect)container.GetChild(1)).Color.ShouldBe(new Color(0.9f, 0.1f, 0.1f, 1f));

        root.Free();
    }
}
