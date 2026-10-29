#nullable enable

using Godot;

namespace Ouranos.Foundry.Core.Types;

/// <summary>
/// Something that writes its state into a save file and restores it from one. Lives in Core
/// so any feature can implement it without referencing the feature that saves it.
/// <para>
/// <c>Save()</c> returns plain data: Variant-compatible values (numbers, strings, vectors,
/// colours, packed arrays, arrays and dictionaries of them), never Godot objects or
/// resources, which a save file can't hold. <c>Load(data)</c> receives the same dictionary
/// back, with every value's type intact. Throw from <c>Load</c> when the data can't be used.
/// </para>
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// The key the data is stored under. It must be unique within a save and stable across
    /// runs. A Node may return an empty string to be keyed by its scene-tree path.
    /// </summary>
    string SaveKey { get; }

    Godot.Collections.Dictionary<string, Variant> Save();

    void Load(Godot.Collections.Dictionary<string, Variant> data);
}
