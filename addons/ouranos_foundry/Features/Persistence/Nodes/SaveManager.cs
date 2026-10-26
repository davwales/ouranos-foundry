#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.Persistence.Types;

namespace Ouranos.Foundry.Features.Persistence.Nodes;

/// <summary>
/// Writes every <c>ISaveable</c> into a named save slot and restores them from it. Saveables
/// are the nodes in <c>SaveGroup</c> plus any object passed to <c>Register</c>. A slot is one
/// file under <c>SaveDirectory</c>, written atomically (temp file, then rename), with a
/// versioned header; older saves go through <c>Migrate</c> before they are loaded.
/// <para>
/// The manager keeps the data of the last save or load, so entries whose saveable isn't
/// present (another level, an unspawned node) are carried into the next save rather than
/// dropped. <c>Restore</c> applies them to a saveable that appears later; <c>ClearData</c>
/// forgets them when a new game starts. Use it as an autoload, or as a node in a scene.
/// </para>
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class SaveManager : Node
{
    public const string DefaultSaveGroup = "saveable";

    // The layout of the file itself, as opposed to Version, the game's data version.
    private const int FileFormat = 1;

    private const string FormatKey = "format";
    private const string VersionKey = "version";
    private const string SavedAtKey = "saved_at";
    private const string MetadataKey = "metadata";
    private const string DataKey = "data";

    /// <summary>Where slot files are written. <c>user://</c> paths are per user and game.</summary>
    [Export]
    public string SaveDirectory { get; private set; } = "user://saves";

    [Export]
    public string FileExtension { get; private set; } = "json";

    /// <summary>Nodes in this group that implement <c>ISaveable</c> are saved and loaded.</summary>
    [Export]
    public string SaveGroup { get; private set; } = DefaultSaveGroup;

    /// <summary>
    /// The game's save data version, written into every save. Raise it when the shape of the
    /// saved data changes, and upgrade older saves in <c>Migrate</c>.
    /// </summary>
    [Export]
    public int Version { get; private set; } = 1;

    [Signal]
    public delegate void SavedEventHandler(string slot);

    [Signal]
    public delegate void LoadedEventHandler(string slot);

    [Signal]
    public delegate void SaveFailedEventHandler(string slot, string error);

    [Signal]
    public delegate void LoadFailedEventHandler(string slot, string error);

    private readonly List<ISaveable> _registered = [];

    // SaveKey to entry, from the last save or load.
    private Godot.Collections.Dictionary<string, Variant> _data = [];

    /// <summary>
    /// Adds a saveable that isn't a node in <c>SaveGroup</c>, such as a plain C# object or a
    /// resource. Call <c>Unregister</c> when it goes away.
    /// </summary>
    public void Register(ISaveable saveable)
    {
        if (!_registered.Contains(saveable))
        {
            _registered.Add(saveable);
        }
    }

    public void Unregister(ISaveable saveable)
    {
        _registered.Remove(saveable);
    }

    /// <summary>
    /// Saves every saveable into <paramref name="slot"/>, replacing what was there.
    /// <paramref name="metadata"/> (play time, location, …) is stored in the header, where
    /// <c>GetSlotInfo</c> reads it for a load menu. Emits <c>Saved</c> or <c>SaveFailed</c>.
    /// </summary>
    public bool Save(string slot, Godot.Collections.Dictionary<string, Variant>? metadata = null)
    {
        // A copy, so a failed save leaves the held data as it was.
        var data = _data.Duplicate();
        try
        {
            var path = GetGlobalSlotPath(slot);
            foreach (var (key, saveable) in CollectSaveables())
            {
                data[key] = saveable.Save();
            }

            var file = new Godot.Collections.Dictionary<string, Variant>
            {
                [FormatKey] = FileFormat,
                [VersionKey] = Version,
                [SavedAtKey] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                [MetadataKey] = metadata ?? [],
                [DataKey] = data,
            };
            WriteAtomically(path, Encode(file));
        }
        catch (Exception e)
        {
            GD.PushError($"SaveManager: saving slot '{slot}' failed: {e}");
            EmitSignal(SignalName.SaveFailed, slot, e.Message);
            return false;
        }

        _data = data;
        EmitSignal(SignalName.Saved, slot);
        return true;
    }

    /// <summary>
    /// Loads <paramref name="slot"/> into every saveable that has an entry in it. Saveables
    /// without one are left as they are. A saveable that throws doesn't stop the others; the
    /// load then reports every failure in <c>LoadFailed</c> and returns false.
    /// </summary>
    public bool Load(string slot)
    {
        try
        {
            var path = GetGlobalSlotPath(slot);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"No save in slot '{slot}'.");
            }

            _data = ReadData(Decode(File.ReadAllBytes(path)));
        }
        catch (Exception e)
        {
            GD.PushError($"SaveManager: loading slot '{slot}' failed: {e}");
            EmitSignal(SignalName.LoadFailed, slot, e.Message);
            return false;
        }

        var errors = new List<string>();
        foreach (var (key, saveable) in CollectSaveables())
        {
            if (!TryRestore(key, saveable, out var error) && error is not null)
            {
                errors.Add(error);
            }
        }

        if (errors.Count > 0)
        {
            EmitSignal(SignalName.LoadFailed, slot, string.Join("\n", errors));
            return false;
        }

        EmitSignal(SignalName.Loaded, slot);
        return true;
    }

    /// <summary>
    /// Loads the held entry of a saveable that appeared after the last <c>Load</c>, such as a
    /// node in a newly entered level. Returns false when there is no entry or it fails to load.
    /// </summary>
    public bool Restore(ISaveable saveable)
    {
        return ResolveKey(saveable) is { } key && TryRestore(key, saveable, out _);
    }

    /// <summary>Forgets the held data, e.g. when a new game starts.</summary>
    public void ClearData()
    {
        _data = [];
    }

    public bool HasSave(string slot)
    {
        return File.Exists(GetGlobalSlotPath(slot));
    }

    public bool DeleteSave(string slot)
    {
        var path = GetGlobalSlotPath(slot);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    /// <summary>The names of the saved slots, in ordinal order.</summary>
    public IReadOnlyList<string> GetSlots()
    {
        var directory = ProjectSettings.GlobalizePath(SaveDirectory);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return
        [
            .. Directory
                .EnumerateFiles(directory, $"*.{FileExtension}")
                .Select(Path.GetFileNameWithoutExtension)
                .OfType<string>()
                .Order(StringComparer.Ordinal),
        ];
    }

    /// <summary>A slot's header, or null when the slot is empty or unreadable.</summary>
    public SaveSlotInfo? GetSlotInfo(string slot)
    {
        try
        {
            var path = GetGlobalSlotPath(slot);
            if (!File.Exists(path))
            {
                return null;
            }

            var file = Decode(File.ReadAllBytes(path));
            return new SaveSlotInfo(
                slot,
                file[VersionKey].AsInt32(),
                DateTimeOffset.FromUnixTimeSeconds(file[SavedAtKey].AsInt64()).UtcDateTime,
                ToStringDictionary(file[MetadataKey], MetadataKey)
            );
        }
        catch (Exception e)
        {
            GD.PushWarning($"SaveManager: can't read slot '{slot}': {e.Message}");
            return null;
        }
    }

    /// <summary>The slot's file path, e.g. <c>user://saves/slot1.json</c>.</summary>
    public string GetSlotPath(string slot)
    {
        if (string.IsNullOrEmpty(slot) || !slot.IsValidFileName())
        {
            throw new ArgumentException($"'{slot}' is not a valid slot name.", nameof(slot));
        }

        return $"{SaveDirectory.TrimEnd('/')}/{slot}.{FileExtension}";
    }

    /// <summary>
    /// Upgrades the data of a save written with an older <c>Version</c>.
    /// <paramref name="data"/> maps each SaveKey to its entry dictionary. The default
    /// returns it unchanged.
    /// </summary>
    protected virtual Godot.Collections.Dictionary<string, Variant> Migrate(
        Godot.Collections.Dictionary<string, Variant> data,
        int fromVersion
    )
    {
        return data;
    }

    /// <summary>
    /// Turns a save file into bytes. The default writes JSON with Godot's
    /// <c>JSON.from_native</c> encoding, which keeps every value's type (int or float,
    /// <c>Vector2I</c>, packed arrays) and drops objects. Override for another format.
    /// </summary>
    protected virtual byte[] Encode(Godot.Collections.Dictionary<string, Variant> file)
    {
        return Encoding.UTF8.GetBytes(Json.Stringify(Json.FromNative(file)));
    }

    /// <summary>Reads what <c>Encode</c> wrote. Throws when the bytes are not a save file.</summary>
    protected virtual Godot.Collections.Dictionary<string, Variant> Decode(byte[] bytes)
    {
        using var json = new Json();
        if (json.Parse(Encoding.UTF8.GetString(bytes)) != Error.Ok)
        {
            throw new FormatException(
                $"Invalid JSON at line {json.GetErrorLine()}: {json.GetErrorMessage()}"
            );
        }

        return ToStringDictionary(Json.ToNative(json.Data), "The save file");
    }

    private Godot.Collections.Dictionary<string, Variant> ReadData(
        Godot.Collections.Dictionary<string, Variant> file
    )
    {
        var format = file[FormatKey].AsInt32();
        if (format > FileFormat)
        {
            throw new FormatException(
                $"The save uses file format {format}; this version of Foundry reads up to {FileFormat}."
            );
        }

        var version = file[VersionKey].AsInt32();
        if (version > Version)
        {
            throw new FormatException(
                $"The save is from data version {version}, newer than this game's {Version}."
            );
        }

        var data = ToStringDictionary(file[DataKey], DataKey);
        return version < Version ? Migrate(data, version) : data;
    }

    private bool TryRestore(string key, ISaveable saveable, out string? error)
    {
        error = null;
        if (!_data.TryGetValue(key, out var entry))
        {
            return false;
        }

        try
        {
            saveable.Load(ToStringDictionary(entry, $"The entry '{key}'"));
            return true;
        }
        catch (Exception e)
        {
            GD.PushError($"SaveManager: loading '{key}' failed: {e}");
            error = $"{key}: {e.Message}";
            return false;
        }
    }

    // Registered saveables first, then the group's nodes in tree order. Skips duplicate keys.
    private List<(string Key, ISaveable Saveable)> CollectSaveables()
    {
        var saveables = _registered.ToList();
        if (IsInsideTree())
        {
            foreach (var node in GetTree().GetNodesInGroup(SaveGroup))
            {
                if (node is ISaveable saveable)
                {
                    saveables.Add(saveable);
                }
                else
                {
                    GD.PushWarning(
                        $"SaveManager: {node.GetPath()} is in group '{SaveGroup}' but is not an ISaveable."
                    );
                }
            }
        }

        var byKey = new Dictionary<string, ISaveable>();
        foreach (var saveable in saveables)
        {
            if (ResolveKey(saveable) is not { } key)
            {
                continue;
            }

            if (!byKey.TryAdd(key, saveable) && !ReferenceEquals(byKey[key], saveable))
            {
                GD.PushError($"SaveManager: two saveables use the SaveKey '{key}'; skipping one.");
            }
        }

        return [.. byKey.Select(pair => (pair.Key, pair.Value))];
    }

    private static string? ResolveKey(ISaveable saveable)
    {
        if (saveable.SaveKey is { Length: > 0 } key)
        {
            return key;
        }

        if (saveable is Node node && node.IsInsideTree())
        {
            return node.GetPath();
        }

        GD.PushError($"SaveManager: {saveable} has no SaveKey and is not a node in the tree.");
        return null;
    }

    private string GetGlobalSlotPath(string slot)
    {
        return ProjectSettings.GlobalizePath(GetSlotPath(slot));
    }

    // Write and flush a temp file, then rename it over the slot: a crash mid-write leaves
    // the previous save intact.
    private static void WriteAtomically(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tempPath = path + ".tmp";
        using (var stream = File.Create(tempPath))
        {
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    // Copies into a String-keyed dictionary, whether the source was typed or not.
    private static Godot.Collections.Dictionary<string, Variant> ToStringDictionary(
        Variant value,
        string what
    )
    {
        if (value.VariantType != Variant.Type.Dictionary)
        {
            throw new FormatException($"{what} is not a dictionary.");
        }

        var result = new Godot.Collections.Dictionary<string, Variant>();
        foreach (var (key, item) in value.AsGodotDictionary())
        {
            result[key.AsString()] = item;
        }

        return result;
    }
}
