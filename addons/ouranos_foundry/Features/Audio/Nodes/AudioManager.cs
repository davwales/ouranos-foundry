#nullable enable

using Godot;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Features.Audio.Nodes;

/// <summary>
/// One place for a game's audio, meant as an autoload: sound effects through
/// <see cref="Sounds"/>, music through <see cref="Music"/>, and a linear 0..1 volume per
/// audio bus. It uses a child <see cref="SoundPool"/> and <see cref="MusicPlayer"/> when it
/// has them and creates them when it doesn't. Volumes and mutes are saved as an
/// <c>ISaveable</c>.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class AudioManager : Node, ISaveable
{
    private const string VolumesKey = "volumes";
    private const string MutedKey = "muted";

    [Export]
    public string SaveKey { get; private set; } = "audio";

    /// <summary>
    /// Adds the buses the sound pool and music player play on (by default <c>SFX</c> and
    /// <c>Music</c>) to the bus layout when it lacks them, routed to Master, so each gets
    /// its own volume without setting up a layout.
    /// </summary>
    [Export]
    public bool CreateMissingBuses { get; private set; } = true;

    private SoundPool? _sounds;
    private MusicPlayer? _music;

    public AudioManager()
    {
        // Music keeps playing and fading in pause menus. A scene can still override this.
        ProcessMode = ProcessModeEnum.Always;
    }

    public SoundPool Sounds => _sounds ??= FindOrCreate<SoundPool>("Sounds");

    public MusicPlayer Music => _music ??= FindOrCreate<MusicPlayer>("Music");

    public override void _Ready()
    {
        if (CreateMissingBuses)
        {
            EnsureBus(Sounds.Bus);
            EnsureBus(Music.Bus);
        }
    }

    /// <summary>Sets a bus's volume, from 0 (silent) to 1 (the bus at 0 dB).</summary>
    public void SetVolume(StringName bus, float volume)
    {
        if (GetBusIndex(bus) is { } index)
        {
            AudioServer.SetBusVolumeLinear(index, Mathf.Clamp(volume, 0f, 1f));
        }
    }

    public float GetVolume(StringName bus)
    {
        return GetBusIndex(bus) is { } index ? AudioServer.GetBusVolumeLinear(index) : 0f;
    }

    /// <summary>Mutes or unmutes a bus without changing its volume.</summary>
    public void SetMuted(StringName bus, bool muted)
    {
        if (GetBusIndex(bus) is { } index)
        {
            AudioServer.SetBusMute(index, muted);
        }
    }

    public bool IsMuted(StringName bus)
    {
        return GetBusIndex(bus) is { } index && AudioServer.IsBusMute(index);
    }

    /// <summary>The volume and mute of every bus, keyed by bus name.</summary>
    public virtual Godot.Collections.Dictionary<string, Variant> Save()
    {
        var volumes = new Godot.Collections.Dictionary<string, Variant>();
        var muted = new Godot.Collections.Dictionary<string, Variant>();
        for (var i = 0; i < AudioServer.BusCount; i++)
        {
            var name = AudioServer.GetBusName(i);
            volumes[name] = AudioServer.GetBusVolumeLinear(i);
            muted[name] = AudioServer.IsBusMute(i);
        }

        return new() { [VolumesKey] = volumes, [MutedKey] = muted };
    }

    /// <summary>Restores saved buses that exist in the current layout and ignores the rest.</summary>
    public virtual void Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        foreach (var (bus, volume) in data[VolumesKey].AsGodotDictionary())
        {
            if (AudioServer.GetBusIndex(bus.AsStringName()) >= 0)
            {
                SetVolume(bus.AsStringName(), volume.AsSingle());
            }
        }

        foreach (var (bus, muted) in data[MutedKey].AsGodotDictionary())
        {
            if (AudioServer.GetBusIndex(bus.AsStringName()) >= 0)
            {
                SetMuted(bus.AsStringName(), muted.AsBool());
            }
        }
    }

    private T FindOrCreate<T>(string name)
        where T : Node, new()
    {
        foreach (var child in GetChildren())
        {
            if (child is T existing)
            {
                return existing;
            }
        }

        var created = new T { Name = name };
        AddChild(created);
        return created;
    }

    private static void EnsureBus(StringName bus)
    {
        if (bus.IsEmpty || AudioServer.GetBusIndex(bus) >= 0)
        {
            return;
        }

        AudioServer.AddBus();
        var index = AudioServer.BusCount - 1;
        AudioServer.SetBusName(index, bus);
        AudioServer.SetBusSend(index, "Master");
    }

    private int? GetBusIndex(StringName bus)
    {
        var index = AudioServer.GetBusIndex(bus);
        if (index < 0)
        {
            GD.PushError($"AudioManager '{Name}': there is no audio bus named '{bus}'.");
            return null;
        }

        return index;
    }
}
