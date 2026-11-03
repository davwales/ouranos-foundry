#nullable enable

using Godot;

namespace Ouranos.Foundry.Core.Resources;

/// <summary>
/// Describes one stat, such as strength, max health or move speed. A <c>StatBlock</c> holds a
/// base value and modifiers per definition; the definition itself is shared and never changes
/// at runtime.
/// </summary>
[GlobalClass]
public partial class StatDefinition : Resource
{
    /// <summary>Identifies the stat on a <c>StatBlock</c> and in <c>StatModifier.StatId</c>.</summary>
    [Export]
    public StringName Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    /// <summary>The base value a <c>StatBlock</c> starts with.</summary>
    [Export]
    public float BaseValue { get; set; }

    /// <summary>The final value is clamped to at least this. Defaults to no limit.</summary>
    [Export]
    public float MinValue { get; set; } = float.NegativeInfinity;

    /// <summary>The final value is clamped to at most this. Defaults to no limit.</summary>
    [Export]
    public float MaxValue { get; set; } = float.PositiveInfinity;
}
