#nullable enable

using Godot;
using Ouranos.Foundry.Features.StatusEffects.Nodes;
using Ouranos.Foundry.Features.StatusEffects.Types;

namespace Ouranos.Foundry.Features.StatusEffects.Resources;

/// <summary>
/// Describes a status effect: a buff, debuff, damage or heal over time, crowd control,
/// aura and so on. The definition only says how long the effect lasts, how it ticks and
/// how it stacks. What it does is up to your game: react to the
/// <see cref="StatusEffectComponent"/> signals, or subclass this and override the hooks.
/// </summary>
[GlobalClass]
public partial class StatusEffectDefinition : Resource
{
    /// <summary>
    /// Identifies the effect on a component. Applying an effect whose id is already
    /// active follows <see cref="Stacking"/>.
    /// </summary>
    [Export]
    public StringName Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = "";

    [Export]
    public Texture2D? Icon { get; set; }

    [Export]
    public StatusEffectDurationType DurationType { get; set; } = StatusEffectDurationType.Timed;

    /// <summary>
    /// How long a <see cref="StatusEffectDurationType.Timed"/> effect lasts, in seconds, or in
    /// the unit you advance a manually updated component by.
    /// </summary>
    [Export(PropertyHint.Range, "0,3600,0.01,or_greater,suffix:s")]
    public float Duration { get; set; } = 10f;

    /// <summary>
    /// Time between ticks, e.g. for damage or healing over time. 0 means the effect never ticks.
    /// </summary>
    [Export(PropertyHint.Range, "0,60,0.01,or_greater,suffix:s")]
    public float TickInterval { get; set; }

    [Export]
    public StatusEffectStacking Stacking { get; set; } = StatusEffectStacking.Refresh;

    /// <summary>
    /// Stack cap for <see cref="StatusEffectStacking.StackIntensity"/>. 0 means no cap.
    /// </summary>
    [Export(PropertyHint.Range, "0,99,1,or_greater")]
    public int MaxStacks { get; set; }

    /// <summary>
    /// Called when the effect is first applied, just before <c>EffectApplied</c>.
    /// The definition is shared by every target, so keep per-target state on the target
    /// or the effect instance, not on the definition.
    /// </summary>
    public virtual void OnApplied(StatusEffectComponent target, StatusEffectInstance effect) { }

    /// <summary>
    /// Called when re-applying changes the active effect, just before <c>EffectRefreshed</c>.
    /// </summary>
    public virtual void OnRefreshed(StatusEffectComponent target, StatusEffectInstance effect) { }

    /// <summary>
    /// Called every <see cref="TickInterval"/>, just before <c>EffectTicked</c>.
    /// </summary>
    public virtual void OnTicked(StatusEffectComponent target, StatusEffectInstance effect) { }

    /// <summary>
    /// Called when the effect ends, just before <c>EffectRemoved</c>.
    /// </summary>
    public virtual void OnRemoved(
        StatusEffectComponent target,
        StatusEffectInstance effect,
        StatusEffectRemovalReason reason
    ) { }
}
