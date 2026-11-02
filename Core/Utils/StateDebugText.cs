#nullable enable

using Godot;

namespace Ouranos.Foundry.Core.Utils;

internal static class StateDebugText
{
    public static string Format(string prefix, StringName state)
    {
        var name = state.ToString();
        return prefix + (name.Length == 0 ? "(none)" : name);
    }
}
