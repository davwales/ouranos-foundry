using Godot;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Demos.Persistence;

/// <summary>
/// A game-side <c>ISaveable</c>: saves where <c>Target</c> is. In the scene it is in the
/// "saveable" group, so the <c>SaveManager</c> finds it without any code.
/// </summary>
public partial class SavedPosition : Node, ISaveable
{
    [Export]
    public Node2D? Target { get; private set; }

    // Empty: keyed by this node's path.
    public string SaveKey => "";

    public Godot.Collections.Dictionary<string, Variant> Save()
    {
        return new() { ["position"] = Target?.Position ?? Vector2.Zero };
    }

    public void Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        if (Target is not null)
        {
            Target.Position = data["position"].AsVector2();
        }
    }
}
