#nullable enable

using Godot;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Core.Resources;

/// <summary>
/// A change to one stat, e.g. +5 strength or ×1.5 move speed. Modifiers are plain data that
/// items, effects and talents can hold and share: who applied one is given to
/// <c>StatBlock.AddModifier</c> as its source, not stored here.
/// </summary>
[GlobalClass]
public partial class StatModifier : Resource
{
    [Export]
    public StringName StatId { get; set; } = "";

    [Export]
    public StatModifierOperation Operation { get; set; } = StatModifierOperation.Add;

    /// <summary>
    /// The amount: a flat value for <c>Add</c> and <c>Override</c>, a fraction for
    /// <c>PercentAdd</c> (0.1 = +10%) and a factor for <c>Multiply</c> (1.5 = ×1.5).
    /// </summary>
    [Export]
    public float Value { get; set; }

    /// <summary>Picks between <c>Override</c> modifiers: the highest wins. Other operations ignore it.</summary>
    [Export]
    public int Priority { get; set; }
}
