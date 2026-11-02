#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.StatusEffects.Resources;
using Ouranos.Foundry.Features.StatusEffects.Types;

namespace Ouranos.Foundry.Features.StatusEffects.Nodes;

/// <summary>
/// Holds the status effects active on its owner, counts down their durations and ticks
/// them. It doesn't know what any effect does: games react to its signals, or to the
/// hooks on <see cref="StatusEffectDefinition"/>.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class StatusEffectComponent : Node
{
    [Export]
    public StatusEffectUpdateMode UpdateMode { get; private set; } = StatusEffectUpdateMode.Process;

    [Signal]
    public delegate void EffectAppliedEventHandler(StatusEffectInstance effect);

    /// <summary>
    /// Re-applying the effect restarted it, added a stack or extended it.
    /// </summary>
    [Signal]
    public delegate void EffectRefreshedEventHandler(StatusEffectInstance effect);

    [Signal]
    public delegate void EffectTickedEventHandler(StatusEffectInstance effect);

    [Signal]
    public delegate void EffectRemovedEventHandler(
        StatusEffectInstance effect,
        StatusEffectRemovalReason reason
    );

    // In application order, so UI lists effects in the order they were gained.
    private readonly List<StatusEffectInstance> _effects = [];

    public override void _Process(double delta)
    {
        if (UpdateMode == StatusEffectUpdateMode.Process)
        {
            Advance(delta);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (UpdateMode == StatusEffectUpdateMode.Physics)
        {
            Advance(delta);
        }
    }

    /// <summary>
    /// Applies the effect, or re-applies it according to its stacking policy if one with
    /// the same id is active. Returns the affected effect, or null if nothing changed.
    /// Override to add immunities or resistances.
    /// </summary>
    public virtual StatusEffectInstance? ApplyEffect(
        StatusEffectDefinition? definition,
        GodotObject? source = null
    )
    {
        if (definition is null)
        {
            GD.PushError($"StatusEffectComponent '{Name}': cannot apply a null effect.");
            return null;
        }

        if (definition.Id.IsEmpty)
        {
            GD.PushError(
                $"StatusEffectComponent '{Name}': effect '{definition.ResourcePath}' has no Id."
            );
            return null;
        }

        var existing = GetEffect(definition.Id);
        if (existing is null || definition.DurationType == StatusEffectDurationType.Instant)
        {
            return Add(definition, source);
        }

        var existingDefinition = existing.Definition;
        switch (existingDefinition.Stacking)
        {
            case StatusEffectStacking.Ignore:
                return null;
            case StatusEffectStacking.Refresh:
                Restart(existing);
                break;
            case StatusEffectStacking.StackIntensity:
                if (
                    existingDefinition.MaxStacks <= 0
                    || existing.Stacks < existingDefinition.MaxStacks
                )
                {
                    existing.Stacks++;
                }

                Restart(existing);
                break;
            case StatusEffectStacking.StackDuration:
                if (existing.IsTimed)
                {
                    existing.Duration += existingDefinition.Duration;
                    existing.RemainingTime += existingDefinition.Duration;
                }

                break;
        }

        existing.Source = source;
        existingDefinition.OnRefreshed(this, existing);
        EmitSignal(SignalName.EffectRefreshed, existing);
        return existing;
    }

    /// <summary>
    /// Removes the active effect with this id. Returns false if there is none.
    /// </summary>
    public bool RemoveEffect(StringName id)
    {
        var effect = GetEffect(id);
        if (effect is null)
        {
            return false;
        }

        Remove(effect, StatusEffectRemovalReason.Removed);
        return true;
    }

    /// <summary>
    /// Removes every active effect.
    /// </summary>
    public void ClearEffects()
    {
        foreach (var effect in _effects.ToArray())
        {
            if (effect.IsActive)
            {
                Remove(effect, StatusEffectRemovalReason.Removed);
            }
        }
    }

    public bool HasEffect(StringName id)
    {
        return GetEffect(id) is not null;
    }

    public StatusEffectInstance? GetEffect(StringName id)
    {
        foreach (var effect in _effects)
        {
            if (effect.Id == id)
            {
                return effect;
            }
        }

        return null;
    }

    /// <summary>
    /// A snapshot of the active effects in the order they were applied. It's safe to
    /// remove effects while iterating it.
    /// </summary>
    public StatusEffectInstance[] GetActiveEffects()
    {
        return [.. _effects];
    }

    /// <summary>
    /// Counts down durations and fires ticks. Called automatically unless
    /// <see cref="UpdateMode"/> is <see cref="StatusEffectUpdateMode.Manual"/>.
    /// A tick that falls exactly on the expiry time fires before the effect expires.
    /// </summary>
    public void Advance(double delta)
    {
        if (delta <= 0 || _effects.Count == 0)
        {
            return;
        }

        // Signal handlers may apply or remove effects, so iterate a snapshot.
        foreach (var effect in _effects.ToArray())
        {
            if (effect.IsActive)
            {
                AdvanceEffect(effect, delta);
            }
        }
    }

    private void AdvanceEffect(StatusEffectInstance effect, double delta)
    {
        var definition = effect.Definition;
        var elapsed = delta;

        if (effect.IsTimed)
        {
            // Never tick past the expiry.
            elapsed = Math.Min(delta, Math.Max(effect.RemainingTime, 0));
            effect.RemainingTime -= elapsed;
        }

        if (definition.TickInterval > 0)
        {
            effect.TimeSinceTick += elapsed;
            while (effect.IsActive && effect.TimeSinceTick >= definition.TickInterval)
            {
                effect.TimeSinceTick -= definition.TickInterval;
                definition.OnTicked(this, effect);
                EmitSignal(SignalName.EffectTicked, effect);
            }
        }

        // A tick handler may have removed or refreshed the effect.
        if (effect.IsActive && effect.IsTimed && effect.RemainingTime <= 0)
        {
            Remove(effect, StatusEffectRemovalReason.Expired);
        }
    }

    private StatusEffectInstance Add(StatusEffectDefinition definition, GodotObject? source)
    {
        var effect = new StatusEffectInstance(definition, source) { IsActive = true };
        Restart(effect);

        var isInstant = definition.DurationType == StatusEffectDurationType.Instant;
        if (!isInstant)
        {
            _effects.Add(effect);
        }

        definition.OnApplied(this, effect);
        EmitSignal(SignalName.EffectApplied, effect);

        if (isInstant && effect.IsActive)
        {
            Remove(effect, StatusEffectRemovalReason.Expired);
        }

        return effect;
    }

    private static void Restart(StatusEffectInstance effect)
    {
        if (effect.IsTimed)
        {
            effect.Duration = effect.Definition.Duration;
            effect.RemainingTime = effect.Definition.Duration;
        }
    }

    private void Remove(StatusEffectInstance effect, StatusEffectRemovalReason reason)
    {
        _effects.Remove(effect);
        effect.IsActive = false;
        effect.Definition.OnRemoved(this, effect, reason);
        EmitSignal(SignalName.EffectRemoved, effect, (int)reason);
    }
}
