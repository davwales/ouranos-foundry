using Godot;
using Ouranos.Foundry.Core.Resources;

namespace Ouranos.Foundry.Demos.Stats;

/// <summary>
/// Anything that grants modifiers while it's active: a piece of gear, a buff, a talent.
/// The set itself is the source the modifiers are added with, so removing it is one call.
/// </summary>
public partial class ModifierSet : Resource
{
    [Export]
    public string DisplayName { get; set; } = "";

    [Export]
    public Godot.Collections.Array<StatModifier> Modifiers { get; set; } = [];
}
