#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Core.Resources;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Core.Nodes;

/// <summary>
/// The stats of one character, item or building: a base value per <see cref="StatDefinition"/>
/// plus the <see cref="StatModifier"/>s applied to it. Final values are cached and recalculated
/// whenever a base value or modifier changes, emitting <c>StatChanged</c> when one moves.
/// <para>
/// Modifiers are added with a source (the item, effect or talent that grants them) so they
/// can be removed together with <see cref="RemoveModifiersFrom"/>. Only base values are saved:
/// modifiers belong to their sources, which re-apply them when they are restored.
/// </para>
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class StatBlock : Node, ISaveable
{
    private const string BaseValuesKey = "base_values";

    /// <summary>
    /// The key a <c>SaveManager</c> stores this block under. Empty: its node path.
    /// </summary>
    [Export]
    public string SaveKey { get; private set; } = "";

    /// <summary>The stats this block has. Read once, the first time the block is used.</summary>
    [Export]
    public Godot.Collections.Array<StatDefinition> Stats { get; private set; } = [];

    [Signal]
    public delegate void StatChangedEventHandler(StringName statId, float oldValue, float newValue);

    private Dictionary<StringName, StatState>? _states;

    private Dictionary<StringName, StatState> States => _states ??= CreateStates();

    public bool HasStat(StringName statId)
    {
        return States.ContainsKey(statId);
    }

    /// <summary>The final value: base value, then modifiers, then the definition's clamp.</summary>
    public float GetValue(StringName statId)
    {
        return Find(statId)?.Value ?? 0f;
    }

    public float GetBaseValue(StringName statId)
    {
        return Find(statId)?.BaseValue ?? 0f;
    }

    /// <summary>Sets the unmodified value, e.g. on level-up. Starts at the definition's <c>BaseValue</c>.</summary>
    public void SetBaseValue(StringName statId, float value)
    {
        var state = Find(statId);
        if (state is not null)
        {
            state.BaseValue = value;
            Recalculate(state);
        }
    }

    /// <summary>
    /// Applies the modifier to its stat. Returns false if this block doesn't have that stat.
    /// Override to add immunities or to scale modifiers.
    /// </summary>
    public virtual bool AddModifier(StatModifier? modifier, GodotObject? source = null)
    {
        if (modifier is null)
        {
            GD.PushError($"StatBlock '{Name}': cannot add a null modifier.");
            return false;
        }

        var state = Find(modifier.StatId);
        if (state is null)
        {
            return false;
        }

        state.Modifiers.Add(new(modifier, source));
        Recalculate(state);
        return true;
    }

    /// <summary>Removes every application of the modifier. Returns false if it wasn't applied.</summary>
    public bool RemoveModifier(StatModifier modifier)
    {
        return RemoveWhere(applied => applied.Modifier == modifier) > 0;
    }

    /// <summary>Removes every modifier added with this source. Returns how many were removed.</summary>
    public int RemoveModifiersFrom(GodotObject source)
    {
        return RemoveWhere(applied => applied.Source == source);
    }

    public void ClearModifiers()
    {
        RemoveWhere(_ => true);
    }

    /// <summary>The modifiers applied to a stat, in the order they were added.</summary>
    public StatModifier[] GetModifiers(StringName statId)
    {
        return Find(statId)?.Modifiers.Select(applied => applied.Modifier).ToArray() ?? [];
    }

    /// <summary>
    /// Recalculates every stat, e.g. after editing an applied modifier in place or when an
    /// overridden <see cref="Calculate"/> depends on something outside the block.
    /// </summary>
    public void Recalculate()
    {
        foreach (var state in States.Values)
        {
            Recalculate(state);
        }
    }

    /// <summary>
    /// Computes a stat's value before clamping:
    /// <c>(base + Σ Add) × (1 + Σ PercentAdd) × Π Multiply</c>, or the winning
    /// <c>Override</c>. <paramref name="modifiers"/> are in the order they were added.
    /// Override for custom formulas.
    /// </summary>
    protected virtual float Calculate(
        StatDefinition stat,
        float baseValue,
        IReadOnlyList<StatModifier> modifiers
    )
    {
        var flat = baseValue;
        var percent = 1f;
        var factor = 1f;
        StatModifier? winningOverride = null;

        foreach (var modifier in modifiers)
        {
            switch (modifier.Operation)
            {
                case StatModifierOperation.Add:
                    flat += modifier.Value;
                    break;
                case StatModifierOperation.PercentAdd:
                    percent += modifier.Value;
                    break;
                case StatModifierOperation.Multiply:
                    factor *= modifier.Value;
                    break;
                case StatModifierOperation.Override:
                    if (winningOverride is null || modifier.Priority >= winningOverride.Priority)
                    {
                        winningOverride = modifier;
                    }

                    break;
            }
        }

        return winningOverride?.Value ?? flat * percent * factor;
    }

    public virtual Godot.Collections.Dictionary<string, Variant> Save()
    {
        var baseValues = new Godot.Collections.Dictionary<string, Variant>();
        foreach (var (id, state) in States)
        {
            baseValues[id] = state.BaseValue;
        }

        return new() { [BaseValuesKey] = baseValues };
    }

    /// <summary>Restores base values. Stats missing from the data keep theirs.</summary>
    public virtual void Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        if (!data.TryGetValue(BaseValuesKey, out var baseValues))
        {
            return;
        }

        foreach (var (id, value) in baseValues.AsGodotDictionary<string, float>())
        {
            if (HasStat(id))
            {
                SetBaseValue(id, value);
            }
        }
    }

    private StatState? Find(StringName statId)
    {
        if (States.TryGetValue(statId, out var state))
        {
            return state;
        }

        GD.PushError($"StatBlock '{Name}' has no stat '{statId}'.");
        return null;
    }

    private int RemoveWhere(Predicate<AppliedModifier> match)
    {
        var removed = 0;
        foreach (var state in States.Values)
        {
            var count = state.Modifiers.RemoveAll(match);
            if (count > 0)
            {
                removed += count;
                Recalculate(state);
            }
        }

        return removed;
    }

    private void Recalculate(StatState state)
    {
        var oldValue = state.Value;
        state.Value = Evaluate(state);
        if (state.Value != oldValue)
        {
            EmitSignal(SignalName.StatChanged, state.Definition.Id, oldValue, state.Value);
        }
    }

    private float Evaluate(StatState state)
    {
        var definition = state.Definition;
        var modifiers = state.Modifiers.Select(applied => applied.Modifier).ToList();
        return Mathf.Clamp(
            Calculate(definition, state.BaseValue, modifiers),
            definition.MinValue,
            definition.MaxValue
        );
    }

    private Dictionary<StringName, StatState> CreateStates()
    {
        var states = new Dictionary<StringName, StatState>();
        foreach (var definition in Stats)
        {
            if (definition is null || definition.Id.IsEmpty)
            {
                GD.PushError($"StatBlock '{Name}': every stat needs a definition with an Id.");
            }
            else if (states.ContainsKey(definition.Id))
            {
                GD.PushError($"StatBlock '{Name}': stat '{definition.Id}' is listed twice.");
            }
            else
            {
                var state = new StatState(definition);
                state.Value = Evaluate(state);
                states[definition.Id] = state;
            }
        }

        return states;
    }

    private sealed record AppliedModifier(StatModifier Modifier, GodotObject? Source);

    private sealed class StatState(StatDefinition definition)
    {
        public StatDefinition Definition { get; } = definition;

        public float BaseValue { get; set; } = definition.BaseValue;

        public float Value { get; set; }

        public List<AppliedModifier> Modifiers { get; } = [];
    }
}
