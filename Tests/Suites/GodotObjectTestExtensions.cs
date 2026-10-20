using Godot;

namespace Ouranos.Foundry.Tests;

internal static class GodotObjectTestExtensions
{
    /// <summary>
    /// Sets a property through Godot's property system — the same path the scene
    /// loader uses for [Export] values — so private setters stay private.
    /// </summary>
    public static T With<T>(this T target, StringName property, Variant value)
        where T : GodotObject
    {
        target.Set(property, value);
        return target;
    }
}
