using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.Health.Nodes;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Health;

[TestSuite]
[RequireGodotRuntime]
public class HealthComponentTests
{
    private static (Node root, HealthComponent component) CreateComponent(
        float baseMaxHealth = 100f,
        float startingHealth = -1f
    )
    {
        var root = new Node();
        var component = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, baseMaxHealth)
            .With(HealthComponent.PropertyName.StartingHealth, startingHealth);
        root.AddChild(component);
        return (root, component);
    }

    [TestCase]
    public void Damage_WhenAmountPositive_ShouldReduceHealthAndReturnDamage()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();

        // Act
        var result = component.Damage(30f);

        // Assert
        result.ShouldBe(30f);
        component.CurrentHealth.ShouldBe(70f);
    }

    [TestCase]
    public void Damage_WhenAmountExceedsHealth_ShouldClampAndReturnRemaining()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 10f);
        component._Ready();

        // Act
        var result = component.Damage(30f);

        // Assert
        result.ShouldBe(10f);
        component.CurrentHealth.ShouldBe(0f);
    }

    [TestCase]
    public void Damage_WhenDead_ShouldReturnZeroAndNotChangeHealth()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 0f);
        component._Ready();

        // Act
        var result = component.Damage(10f);

        // Assert
        result.ShouldBe(0f);
        component.CurrentHealth.ShouldBe(0f);
    }

    [TestCase]
    public void Damage_WhenInvulnerable_ShouldReturnZeroAndNotReduceHealth()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();
        component.SetInvulnerable(true);

        // Act
        var result = component.Damage(30f);

        // Assert
        result.ShouldBe(0f);
        component.CurrentHealth.ShouldBe(100f);
        component.IsInvulnerable.ShouldBe(true);
    }

    [TestCase]
    public void Damage_WhenAmountZeroOrNegative_ShouldReturnZero()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();

        // Act
        var resultZero = component.Damage(0f);
        var resultNegative = component.Damage(-5f);

        // Assert
        resultZero.ShouldBe(0f);
        resultNegative.ShouldBe(0f);
    }

    [TestCase]
    public void Heal_WhenAmountPositive_ShouldIncreaseHealthAndReturnHealing()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 30f);
        component._Ready();

        // Act
        var result = component.Heal(20f);

        // Assert
        result.ShouldBe(20f);
        component.CurrentHealth.ShouldBe(50f);
    }

    [TestCase]
    public void Heal_WhenWouldExceedMax_ShouldClampAndReturnActual()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 90f);
        component._Ready();

        // Act
        var result = component.Heal(50f);

        // Assert
        result.ShouldBe(10f);
        component.CurrentHealth.ShouldBe(100f);
    }

    [TestCase]
    public void Heal_WhenDeadWithoutRevival_ShouldReturnZero()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 0f);
        component._Ready();
        component.Set(HealthComponent.PropertyName.AllowRevival, false);

        // Act
        var result = component.Heal(50f);

        // Assert
        result.ShouldBe(0f);
        component.CurrentHealth.ShouldBe(0f);
        component.AllowRevival.ShouldBe(false);
    }

    [TestCase]
    public void Heal_WhenDeadWithRevival_ShouldRestoreHealth()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 0f);
        component._Ready();
        component.Set(HealthComponent.PropertyName.AllowRevival, true);

        // Act
        var result = component.Heal(50f);

        // Assert
        result.ShouldBe(50f);
        component.CurrentHealth.ShouldBe(50f);
        component.AllowRevival.ShouldBe(true);
    }

    [TestCase]
    public void Heal_WhenAlreadyAtMax_ShouldReturnZero()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();

        // Act
        var result = component.Heal(20f);

        // Assert
        result.ShouldBe(0f);
        component.CurrentHealth.ShouldBe(100f);
    }

    [TestCase]
    public void MaxHealth_WhenProviderAssigned_ShouldUseProviderValue()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f);
        component.MaxHealthProvider = () => 200f;

        // Act
        var max = component.MaxHealth;

        // Assert
        max.ShouldBe(200f);
    }

    [TestCase]
    public void MaxHealth_WhenProviderNull_ShouldUseBaseMaxHealth()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f);
        component.MaxHealthProvider = null;

        // Act
        var max = component.MaxHealth;

        // Assert
        max.ShouldBe(100f);
    }

    [TestCase]
    public void StartingHealth_WhenNegative_ShouldInitializeToMaxHealth()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: -1f);

        // Act
        component._Ready();

        // Assert
        component.CurrentHealth.ShouldBe(100f);
    }

    [TestCase]
    public void StartingHealth_WhenPositive_ShouldInitializeToSpecifiedValue()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 50f);

        // Act
        component._Ready();

        // Assert
        component.CurrentHealth.ShouldBe(50f);
    }

    [TestCase]
    public void StartingHealth_WhenExceedsMax_ShouldClampToMax()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 200f);

        // Act
        component._Ready();

        // Assert
        component.CurrentHealth.ShouldBe(100f);
    }

    [TestCase]
    public void Serialize_ShouldReturnCorrectDictionary()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 70f);
        component._Ready();

        // Act
        var data = component.Serialize();

        // Assert
        data.ContainsKey("current_health").ShouldBeTrue();
        data.ContainsKey("max_health").ShouldBeTrue();
        data["current_health"].As<float>().ShouldBe(70f);
        data["max_health"].As<float>().ShouldBe(100f);
    }

    [TestCase]
    public void Deserialize_WhenValidData_ShouldRestoreState()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();

        var data = new Godot.Collections.Dictionary<string, Variant>
        {
            { "current_health", 50f },
            { "max_health", 100f },
            { "is_dead", false },
        };

        // Act
        component.Deserialize(data);

        // Assert
        component.CurrentHealth.ShouldBe(50f);
    }

    [TestCase]
    public void Deserialize_WhenHealthExceedsMax_ShouldClampToMax()
    {
        // Arrange
        var (_, component) = CreateComponent();

        var data = new Godot.Collections.Dictionary<string, Variant>
        {
            { "current_health", 150f },
            { "max_health", 100f },
        };

        // Act
        component.Deserialize(data);

        // Assert
        component.CurrentHealth.ShouldBe(100f);
    }

    [TestCase]
    public void Deserialize_WhenKeysMissing_ShouldUseDefaults()
    {
        // Arrange
        var (_, component) = CreateComponent();

        var data = new Godot.Collections.Dictionary<string, Variant>();

        // Act
        component.Deserialize(data);

        // Assert
        component.CurrentHealth.ShouldBe(0f);
    }

    [TestCase]
    public void Deserialize_IsDeadKey_ShouldBeIgnored()
    {
        // Arrange
        var (_, component) = CreateComponent();

        var data = new Godot.Collections.Dictionary<string, Variant> { { "current_health", 50f } };

        // Act
        component.Deserialize(data);

        // Assert
        component.CurrentHealth.ShouldBe(50f);
        component.IsDead.ShouldBeFalse();
    }

    [TestCase]
    public void IsDead_WhenHealthAboveZero_ShouldBeFalse()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 50f);
        component._Ready();

        // Act & Assert
        component.IsDead.ShouldBeFalse();
    }

    [TestCase]
    public void IsDead_WhenHealthZero_ShouldBeTrue()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 0f);
        component._Ready();

        // Act & Assert
        component.IsDead.ShouldBeTrue();
    }

    [TestCase]
    public void HealthFraction_ShouldReturnRatioOfCurrentToMax()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 30f);
        component._Ready();

        // Act & Assert
        component.HealthFraction.ShouldBe(0.3f);
    }

    [TestCase]
    public void HealthFraction_WhenDead_ShouldReturnZero()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 0f);
        component._Ready();

        // Act & Assert
        component.HealthFraction.ShouldBe(0f);
    }

    [TestCase]
    public void HealthFraction_WhenMaxHealthIsZero_ShouldReturnZero()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 0f);
        component.MaxHealthProvider = () => 0f;
        component._Ready();

        // Act & Assert
        component.HealthFraction.ShouldBe(0f);
    }

    [TestCase]
    public void RefreshMaxHealth_WhenCurrentWithinMax_ShouldEmitSignalsAndReturnZero()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 50f);
        component._Ready();

        float? receivedMax = null;
        float? receivedChangedCurrent = null;
        float? receivedChangedMax = null;
        component.MaxHealthChanged += max => receivedMax = max;
        component.HealthChanged += (current, max) =>
        {
            receivedChangedCurrent = current;
            receivedChangedMax = max;
        };

        // Act
        var clamped = component.RefreshMaxHealth();

        // Assert
        clamped.ShouldBe(0f);
        receivedMax.ShouldBe(100f);
        receivedChangedCurrent.ShouldBe(50f);
        receivedChangedMax.ShouldBe(100f);
    }

    [TestCase]
    public void RefreshMaxHealth_WhenCurrentExceedsMax_ShouldClampAndReturnAmountClamped()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 100f);
        component._Ready();
        component.MaxHealthProvider = () => 60f;

        // Act
        var clamped = component.RefreshMaxHealth();

        // Assert
        clamped.ShouldBe(40f);
        component.CurrentHealth.ShouldBe(60f);
    }

    [TestCase]
    public void SetBaseMaxHealth_ShouldUpdateBaseAndReturnNewMax()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 50f);
        component._Ready();

        // Act
        var newMax = component.SetBaseMaxHealth(200f);

        // Assert
        newMax.ShouldBe(200f);
        component.BaseMaxHealth.ShouldBe(200f);
    }

    [TestCase]
    public void SetBaseMaxHealth_ShouldEnforceMinimumOfOne()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();

        // Act
        var newMax = component.SetBaseMaxHealth(-10f);

        // Assert
        newMax.ShouldBe(1f);
        component.BaseMaxHealth.ShouldBe(1f);
    }

    [TestCase]
    public void SetBaseMaxHealth_WithClampCurrent_ShouldClampHealth()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 80f);
        component._Ready();

        // Act
        component.SetBaseMaxHealth(50f, clampCurrent: true);

        // Assert
        component.CurrentHealth.ShouldBe(50f);
    }

    [TestCase]
    public void SetBaseMaxHealth_WithoutClampCurrent_ShouldNotClampHealth()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 80f);
        component._Ready();

        // Act
        component.SetBaseMaxHealth(50f, clampCurrent: false);

        // Assert
        component.CurrentHealth.ShouldBe(80f);
    }

    [TestCase]
    public void SetBaseMaxHealth_ShouldEmitMaxHealthChangedSignal()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();

        float? receivedMax = null;
        component.MaxHealthChanged += max => receivedMax = max;

        // Act
        component.SetBaseMaxHealth(200f);

        // Assert
        receivedMax.ShouldBe(200f);
    }

    [TestCase]
    public void Ready_ShouldEmitMaxHealthChangedSignal()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 70f);
        float? receivedMax = null;
        component.MaxHealthChanged += max => receivedMax = max;

        // Act
        component._Ready();

        // Assert
        receivedMax.ShouldBe(100f);
    }

    [TestCase]
    public void Ready_ShouldEmitHealthChangedSignal()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 70f);
        float? receivedCurrent = null;
        float? receivedMax = null;
        component.HealthChanged += (current, max) =>
        {
            receivedCurrent = current;
            receivedMax = max;
        };

        // Act
        component._Ready();

        // Assert
        receivedCurrent.ShouldBe(70f);
        receivedMax.ShouldBe(100f);
    }

    [TestCase]
    public void Ready_WithDefaultStartingHealth_ShouldEmitHealthChangedAtMax()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: -1f);
        float? receivedCurrent = null;
        component.HealthChanged += (current, _) => receivedCurrent = current;

        // Act
        component._Ready();

        // Assert
        receivedCurrent.ShouldBe(100f);
    }

    [TestCase]
    public void IsInvulnerable_WhenSetViaExport_ShouldBlockDamage()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();
        component.SetInvulnerable(true);

        // Act
        var result = component.Damage(30f);

        // Assert
        result.ShouldBe(0f);
        component.CurrentHealth.ShouldBe(100f);
    }

    [TestCase]
    public void AllowRevival_WhenSetViaExport_ShouldEnableHealingWhenDead()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 0f);
        component._Ready();
        component.Set(HealthComponent.PropertyName.AllowRevival, true);

        // Act
        var result = component.Heal(50f);

        // Assert
        result.ShouldBe(50f);
        component.CurrentHealth.ShouldBe(50f);
    }

    [TestCase]
    public void Damage_ShouldEmitHealthDecreasedSignal()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();

        float? receivedAmount = null;
        float? receivedCurrent = null;
        component.HealthDecreased += (amount, current, _) =>
        {
            receivedAmount = amount;
            receivedCurrent = current;
        };

        // Act
        component.Damage(30f);

        // Assert
        receivedAmount.ShouldBe(30f);
        receivedCurrent.ShouldBe(70f);
    }

    [TestCase]
    public void Damage_ShouldEmitHealthChangedSignal()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();

        float? receivedCurrent = null;
        float? receivedMax = null;
        component.HealthChanged += (current, max) =>
        {
            receivedCurrent = current;
            receivedMax = max;
        };

        // Act
        component.Damage(30f);

        // Assert
        receivedCurrent.ShouldBe(70f);
        receivedMax.ShouldBe(100f);
    }

    [TestCase]
    public void Damage_WhenHealthReachesZero_ShouldEmitHealthDepletedSignal()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 10f);
        component._Ready();

        var depletedFired = false;
        component.HealthDepleted += () => depletedFired = true;

        // Act
        component.Damage(10f);

        // Assert
        depletedFired.ShouldBeTrue();
    }

    [TestCase]
    public void Heal_ShouldEmitHealthIncreasedSignal()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 30f);
        component._Ready();

        float? receivedAmount = null;
        float? receivedCurrent = null;
        component.HealthIncreased += (amount, current) =>
        {
            receivedAmount = amount;
            receivedCurrent = current;
        };

        // Act
        component.Heal(20f);

        // Assert
        receivedAmount.ShouldBe(20f);
        receivedCurrent.ShouldBe(50f);
    }

    [TestCase]
    public void Heal_WhenReviving_ShouldEmitHealthRestoredSignal()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 0f);
        component._Ready();
        component.Set(HealthComponent.PropertyName.AllowRevival, true);

        var restoredFired = false;
        component.HealthRestored += () => restoredFired = true;

        // Act
        component.Heal(50f);

        // Assert
        restoredFired.ShouldBeTrue();
    }

    [TestCase]
    public void SignalOrder_Damage_ShouldBeDecreasedThenChanged()
    {
        // Arrange
        var (_, component) = CreateComponent();
        component._Ready();

        var signalOrder = new List<string>();
        component.HealthDecreased += (_, _, _) => signalOrder.Add("health_decreased");
        component.HealthChanged += (_, _) => signalOrder.Add("health_changed");

        // Act
        component.Damage(30f);

        // Assert
        signalOrder.Count.ShouldBe(2);
        signalOrder[0].ShouldBe("health_decreased");
        signalOrder[1].ShouldBe("health_changed");
    }

    [TestCase]
    public void SignalOrder_Damage_WhenHealthReachesZero_ShouldBeDecreasedThenChangedThenDepleted()
    {
        // Arrange
        var (_, component) = CreateComponent(startingHealth: 30f);
        component._Ready();

        var signalOrder = new List<string>();
        component.HealthDecreased += (_, _, _) => signalOrder.Add("health_decreased");
        component.HealthChanged += (_, _) => signalOrder.Add("health_changed");
        component.HealthDepleted += () => signalOrder.Add("health_depleted");

        // Act
        component.Damage(30f);

        // Assert
        signalOrder.Count.ShouldBe(3);
        signalOrder[0].ShouldBe("health_decreased");
        signalOrder[1].ShouldBe("health_changed");
        signalOrder[2].ShouldBe("health_depleted");
    }

    [TestCase]
    public void SignalOrder_Heal_WhenReviving_ShouldBeIncreasedThenChangedThenRestored()
    {
        // Arrange
        var (root, component) = CreateComponent(startingHealth: 0f);
        component._Ready();
        component.Set(HealthComponent.PropertyName.AllowRevival, true);

        var signalOrder = new List<string>();
        component.HealthIncreased += (_, _) => signalOrder.Add("health_increased");
        component.HealthChanged += (_, _) => signalOrder.Add("health_changed");
        component.HealthRestored += () => signalOrder.Add("health_restored");

        // Act
        component.Heal(50f);

        // Assert
        signalOrder.Count.ShouldBe(3);
        signalOrder[0].ShouldBe("health_increased");
        signalOrder[1].ShouldBe("health_changed");
        signalOrder[2].ShouldBe("health_restored");
    }

    [TestCase]
    public void Damage_ShouldForwardSourceInHealthDecreasedSignal()
    {
        // Arrange
        var (root, component) = CreateComponent();
        component._Ready();

        var source = new Node();
        root.AddChild(source);

        Node? receivedSource = null;
        component.HealthDecreased += (_, _, source) => receivedSource = source;

        // Act
        component.Damage(30f, source);

        // Assert
        receivedSource.ShouldBe(source);
    }

    [TestCase]
    public void SetBaseMaxHealth_WithMaxHealthProvider_ShouldUseProviderMax()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f);
        component._Ready();
        component.MaxHealthProvider = () => 200f;

        // Act
        var newMax = component.SetBaseMaxHealth(50f);

        // Assert
        newMax.ShouldBe(200f);
        component.BaseMaxHealth.ShouldBe(50f);
    }

    [TestCase]
    public void Deserialize_ShouldEmitHealthChangedSignal()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f);
        component._Ready();

        float? receivedCurrent = null;
        float? receivedMax = null;
        component.HealthChanged += (current, max) =>
        {
            receivedCurrent = current;
            receivedMax = max;
        };

        var data = new Godot.Collections.Dictionary<string, Variant> { { "current_health", 50f } };

        // Act
        component.Deserialize(data);

        // Assert
        receivedCurrent.ShouldBe(50f);
        receivedMax.ShouldBe(100f);
    }

    [TestCase]
    public void RefreshMaxHealth_WhenNothingChanged_ShouldStillEmitSignals()
    {
        // Arrange
        var (_, component) = CreateComponent(baseMaxHealth: 100f, startingHealth: 50f);
        component._Ready();

        var maxHealthChangedFired = false;
        var healthChangedFired = false;
        component.MaxHealthChanged += _ => maxHealthChangedFired = true;
        component.HealthChanged += (_, _) => healthChangedFired = true;

        // Act
        component.RefreshMaxHealth();

        // Assert
        maxHealthChangedFired.ShouldBeTrue();
        healthChangedFired.ShouldBeTrue();
    }
}
