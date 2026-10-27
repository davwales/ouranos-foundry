#nullable enable

using Godot;
using Ouranos.Foundry.Features.StatusEffects.Nodes;
using Ouranos.Foundry.Features.StatusEffects.Types;

namespace Ouranos.Foundry.Features.StatusEffects.Resources;

/// <summary>
/// One effect active on a <see cref="StatusEffectComponent"/>. The component creates and
/// updates it; read it from signals or <c>GetEffect</c>. Use <c>SetMeta</c> to keep
/// per-target state, such as a stat modifier to undo on removal.
/// </summary>
[GlobalClass]
public partial class StatusEffectInstance : Resource
{
    public StatusEffectInstance() { }

    internal StatusEffectInstance(StatusEffectDefinition definition, GodotObject? source)
    {
        Definition = definition;
        Source = source;
    }

    public StatusEffectDefinition Definition { get; } = new();

    /// <summary>
    /// Whoever applied the effect most recently: a node, an item resource, or null.
    /// A freed node stays referenced, so check <c>GodotObject.IsInstanceValid</c> before use.
    /// </summary>
    public GodotObject? Source { get; internal set; }

    public StringName Id => Definition.Id;

    public int Stacks { get; internal set; } = 1;

    /// <summary>
    /// Length of the current application, including time added by
    /// <see cref="StatusEffectStacking.StackDuration"/>. 0 unless the effect is timed.
    /// </summary>
    public double Duration { get; internal set; }

    /// <summary>
    /// Time left before the effect expires. 0 unless the effect is timed.
    /// </summary>
    public double RemainingTime { get; internal set; }

    public bool IsTimed => Definition.DurationType == StatusEffectDurationType.Timed;

    /// <summary>
    /// False once the effect has been removed.
    /// </summary>
    public bool IsActive { get; internal set; }

    internal double TimeSinceTick { get; set; }
}
