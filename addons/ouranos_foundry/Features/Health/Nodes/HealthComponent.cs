#nullable enable

using System;
using Godot;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Features.Health.Nodes;

[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class HealthComponent : Node, ISaveable
{
    /// <summary>
    /// The key a <c>SaveManager</c> stores this component under. Empty: its node path.
    /// </summary>
    [Export]
    public string SaveKey { get; private set; } = "";

    [Export]
    public float BaseMaxHealth { get; private set; } = 100f;

    /// <summary>
    /// -1 = full health on spawn, any positive value = explicit starting health.
    /// </summary>
    [Export]
    public float StartingHealth { get; private set; } = -1f;

    /// <summary>
    /// If assigned, called every time MaxHealth is read. If null, falls back to BaseMaxHealth.
    /// </summary>
    public Func<float>? MaxHealthProvider { get; set; }

    [Export]
    public bool IsInvulnerable { get; private set; }

    [Export]
    public bool AllowRevival { get; private set; }

    public float MaxHealth => MaxHealthProvider?.Invoke() ?? BaseMaxHealth;

    private float _currentHealth;

    // isDead is ALWAYS derived: _currentHealth <= 0.0f. Never stored independently.

    public float CurrentHealth => _currentHealth;

    public bool IsDead => _currentHealth <= 0f;

    public float HealthFraction => MaxHealth > 0f ? _currentHealth / MaxHealth : 0f;

    [Signal]
    public delegate void MaxHealthChangedEventHandler(float max);

    [Signal]
    public delegate void HealthChangedEventHandler(float current, float max);

    [Signal]
    public delegate void HealthIncreasedEventHandler(float amount, float current);

    [Signal]
    public delegate void HealthDecreasedEventHandler(float amount, float current, Node? source);

    [Signal]
    public delegate void HealthDepletedEventHandler();

    [Signal]
    public delegate void HealthRestoredEventHandler();

    public override void _Ready()
    {
        base._Ready();

        if (StartingHealth < 0f)
        {
            _currentHealth = MaxHealth;
        }
        else
        {
            _currentHealth = Mathf.Clamp(StartingHealth, 0f, MaxHealth);
        }

        EmitSignal(SignalName.MaxHealthChanged, MaxHealth);
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
    }

    public void SetInvulnerable(bool isInvulnerable)
    {
        IsInvulnerable = isInvulnerable;
    }

    public float RefreshMaxHealth()
    {
        var max = MaxHealth;
        var clamped = 0f;

        if (_currentHealth > max)
        {
            clamped = _currentHealth - max;
            _currentHealth = max;
        }

        EmitSignal(SignalName.MaxHealthChanged, max);
        EmitSignal(SignalName.HealthChanged, _currentHealth, max);

        return clamped;
    }

    public float SetBaseMaxHealth(float value, bool clampCurrent = true)
    {
        BaseMaxHealth = Mathf.Max(1f, value);

        if (clampCurrent && _currentHealth > MaxHealth)
        {
            _currentHealth = MaxHealth;
        }

        EmitSignal(SignalName.MaxHealthChanged, MaxHealth);
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
        return MaxHealth;
    }

    /// <summary>source is optional — for damage attribution / kill tracking.</summary>
    public float Damage(float amount, Node? source = null)
    {
        if (amount <= 0.0f)
        {
            return 0.0f;
        }

        if (_currentHealth <= 0.0f)
        {
            return 0.0f;
        }

        if (IsInvulnerable)
        {
            return 0.0f;
        }

        var max = MaxHealth;
        var effective = Mathf.Min(amount, _currentHealth);
        _currentHealth -= effective;

        var sourceVariant = source != null ? (Variant)source : default;
        EmitSignal(SignalName.HealthDecreased, effective, _currentHealth, sourceVariant);
        EmitSignal(SignalName.HealthChanged, _currentHealth, max);

        if (_currentHealth <= 0.0f)
        {
            EmitSignal(SignalName.HealthDepleted);
        }

        return effective;
    }

    /// <summary>Returns 0 if no-op or dead without revival.</summary>
    public float Heal(float amount)
    {
        if (amount <= 0.0f)
        {
            return 0.0f;
        }

        var wasDead = _currentHealth <= 0.0f;

        if (wasDead && !AllowRevival)
        {
            return 0.0f;
        }

        var max = MaxHealth;
        var effective = Mathf.Min(amount, max - _currentHealth);

        if (effective <= 0.0f)
        {
            return 0.0f;
        }

        _currentHealth += effective;

        EmitSignal(SignalName.HealthIncreased, effective, _currentHealth);
        EmitSignal(SignalName.HealthChanged, _currentHealth, max);

        if (wasDead && _currentHealth > 0.0f)
        {
            EmitSignal(SignalName.HealthRestored);
        }

        return effective;
    }

    public virtual Godot.Collections.Dictionary<string, Variant> Serialize()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { "current_health", _currentHealth },
            { "max_health", MaxHealth },
        };
    }

    public virtual void Deserialize(Godot.Collections.Dictionary<string, Variant> data)
    {
        if (data.TryGetValue("current_health", out var currentVariant))
        {
            _currentHealth = Mathf.Clamp(currentVariant.As<float>(), 0f, MaxHealth);
        }
        else
        {
            _currentHealth = 0f;
        }

        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
    }

    Godot.Collections.Dictionary<string, Variant> ISaveable.Save()
    {
        return Serialize();
    }

    void ISaveable.Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        Deserialize(data);
    }
}
