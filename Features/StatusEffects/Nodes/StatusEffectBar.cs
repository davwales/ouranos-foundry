#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.StatusEffects.Resources;
using Ouranos.Foundry.Features.StatusEffects.Types;

namespace Ouranos.Foundry.Features.StatusEffects.Nodes;

/// <summary>
/// Buff bar: one entry per active effect on the monitored component, showing its icon
/// (or name, without one), stack count and remaining time, with the description as a
/// tooltip. Override <see cref="CreateEntry"/> and <see cref="UpdateEntry"/> to restyle it.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class StatusEffectBar : HBoxContainer
{
    [Export]
    public StatusEffectComponent? Target { get; private set; }

    [Export]
    public Vector2 IconSize { get; private set; } = new(32, 32);

    private readonly Dictionary<StatusEffectInstance, Control> _entries = [];
    private StatusEffectComponent? _component;

    public override void _Ready()
    {
        if (Target is not null)
        {
            MonitorStatusEffectComponent(Target);
        }
    }

    public override void _ExitTree()
    {
        // The entries are leaving the tree with us, so they can't be removed now.
        foreach (var entry in _entries.Values)
        {
            entry.QueueFree();
        }

        _entries.Clear();
        MonitorStatusEffectComponent(null);
    }

    public override void _Process(double delta)
    {
        foreach (var (effect, entry) in _entries)
        {
            UpdateEntry(entry, effect);
        }
    }

    public void MonitorStatusEffectComponent(StatusEffectComponent? component)
    {
        if (_component == component)
        {
            return;
        }

        if (_component is not null && IsInstanceValid(_component))
        {
            _component.EffectApplied -= OnEffectApplied;
            _component.EffectRemoved -= OnEffectRemoved;
        }

        ClearEntries();
        _component = component;

        if (_component is null)
        {
            return;
        }

        _component.EffectApplied += OnEffectApplied;
        _component.EffectRemoved += OnEffectRemoved;

        foreach (var effect in _component.GetActiveEffects())
        {
            OnEffectApplied(effect);
        }
    }

    /// <summary>
    /// Builds the control shown for an effect. The default is the icon (or the display
    /// name, if there's no icon) above a label named "Label".
    /// </summary>
    protected virtual Control CreateEntry(StatusEffectInstance effect)
    {
        var definition = effect.Definition;
        var entry = new VBoxContainer { TooltipText = TooltipFor(definition) };

        if (definition.Icon is not null)
        {
            entry.AddChild(
                new TextureRect
                {
                    Texture = definition.Icon,
                    CustomMinimumSize = IconSize,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = MouseFilterEnum.Pass,
                }
            );
        }
        else
        {
            entry.AddChild(
                new Label
                {
                    Text = string.IsNullOrEmpty(definition.DisplayName)
                        ? definition.Id
                        : definition.DisplayName,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    MouseFilter = MouseFilterEnum.Pass,
                }
            );
        }

        entry.AddChild(
            new Label
            {
                Name = "Label",
                HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = MouseFilterEnum.Pass,
            }
        );
        return entry;
    }

    /// <summary>
    /// Refreshes an entry every frame. The default writes the stack count and remaining
    /// time into the entry's "Label" child.
    /// </summary>
    protected virtual void UpdateEntry(Control entry, StatusEffectInstance effect)
    {
        if (entry.GetNodeOrNull<Label>("Label") is { } label)
        {
            label.Text = FormatLabel(effect);
        }
    }

    /// <summary>
    /// "x3 4.2": the stack count when above 1, then the remaining time for timed effects,
    /// rounded up to whole units from 10 upwards.
    /// </summary>
    public static string FormatLabel(StatusEffectInstance effect)
    {
        var stacks = effect.Stacks > 1 ? $"x{effect.Stacks}" : "";
        if (!effect.IsTimed)
        {
            return stacks;
        }

        var remaining = Math.Max(effect.RemainingTime, 0);
        // From 9.95 one decimal would round up to "10.0", so switch to whole units there.
        var time = remaining >= 9.95 ? $"{Math.Ceiling(remaining):0}" : $"{remaining:0.0}";
        return stacks.Length > 0 ? $"{stacks} {time}" : time;
    }

    private static string TooltipFor(StatusEffectDefinition definition)
    {
        var name = string.IsNullOrEmpty(definition.DisplayName)
            ? definition.Id.ToString()
            : definition.DisplayName;
        return string.IsNullOrEmpty(definition.Description)
            ? name
            : $"{name}\n{definition.Description}";
    }

    private void OnEffectApplied(StatusEffectInstance effect)
    {
        // Instant effects are removed straight after this signal; don't show them.
        if (effect.Definition.DurationType == StatusEffectDurationType.Instant)
        {
            return;
        }

        if (_entries.ContainsKey(effect))
        {
            return;
        }

        var entry = CreateEntry(effect);
        _entries[effect] = entry;
        AddChild(entry);
        UpdateEntry(entry, effect);
    }

    private void OnEffectRemoved(StatusEffectInstance effect, StatusEffectRemovalReason reason)
    {
        if (_entries.Remove(effect, out var entry))
        {
            RemoveChild(entry);
            entry.Free();
        }
    }

    private void ClearEntries()
    {
        foreach (var entry in _entries.Values)
        {
            RemoveChild(entry);
            entry.Free();
        }

        _entries.Clear();
    }
}
