#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.Audio.Resources;
using Ouranos.Foundry.Features.Audio.Types;

namespace Ouranos.Foundry.Features.Audio.Nodes;

/// <summary>
/// Plays one-shot sound effects on a pool of reused players: non-positional
/// (<c>Play</c>), or at a position in 2D or 3D (<c>Play2D</c>, <c>Play3D</c>). Players are
/// created as needed, up to <c>MaxVoices</c>. When every voice is busy, or a cue is at its
/// <c>MaxPolyphony</c>, the oldest voice is restarted with the new sound.
/// <para>
/// Positional players are children of the pool, so they play in the pool's viewport.
/// </para>
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class SoundPool : Node
{
    private const ulong RngSalt = 0xA0D10;

    /// <summary>The most players the pool creates; also the most sounds playing at once.</summary>
    [Export(PropertyHint.Range, "1,256,1,or_greater")]
    public int MaxVoices { get; private set; } = 32;

    /// <summary>The bus for cues that don't name their own. A missing bus plays on Master.</summary>
    [Export]
    public StringName Bus { get; private set; } = "SFX";

    private readonly List<Voice> _voices = [];
    private SeededRng _rng = new(0, RngSalt);
    private ulong _playCount;

    /// <summary>Uses <paramref name="rng"/> for every later pick, e.g. to make variations replayable.</summary>
    public void SetRng(SeededRng rng)
    {
        _rng = rng;
    }

    /// <summary>Plays <paramref name="cue"/> without position. Returns false when nothing played.</summary>
    public bool Play(AudioCue? cue)
    {
        return Start(cue, SoundPositioning.None, Vector3.Zero);
    }

    /// <summary>Plays <paramref name="cue"/> at a global 2D position.</summary>
    public bool Play2D(AudioCue? cue, Vector2 position)
    {
        return Start(cue, SoundPositioning.Positional2D, new Vector3(position.X, position.Y, 0));
    }

    /// <summary>Plays <paramref name="cue"/> at a global 3D position.</summary>
    public bool Play3D(AudioCue? cue, Vector3 position)
    {
        return Start(cue, SoundPositioning.Positional3D, position);
    }

    /// <summary>Stops every playing voice of <paramref name="cue"/>.</summary>
    public void Stop(AudioCue? cue)
    {
        foreach (var voice in _voices.Where(voice => voice.Cue == cue))
        {
            voice.Stop();
        }
    }

    public void StopAll()
    {
        foreach (var voice in _voices)
        {
            voice.Stop();
        }
    }

    /// <summary>How many voices are playing <paramref name="cue"/>, or any cue when null.</summary>
    public int GetPlayingCount(AudioCue? cue)
    {
        return _voices.Count(voice => voice.IsPlaying && (cue is null || voice.Cue == cue));
    }

    /// <summary>
    /// Creates a pooled player: an <c>AudioStreamPlayer</c>, <c>AudioStreamPlayer2D</c> or
    /// <c>AudioStreamPlayer3D</c> for <paramref name="positioning"/>. Override to set
    /// attenuation, max distance or panning for your game; return the same player type.
    /// </summary>
    protected virtual Node CreatePlayer(SoundPositioning positioning)
    {
        return positioning switch
        {
            SoundPositioning.Positional2D => new AudioStreamPlayer2D(),
            SoundPositioning.Positional3D => new AudioStreamPlayer3D(),
            _ => new AudioStreamPlayer(),
        };
    }

    private bool Start(AudioCue? cue, SoundPositioning kind, Vector3 position)
    {
        if (cue is null)
        {
            GD.PushError($"SoundPool '{Name}': cannot play a null cue.");
            return false;
        }

        if (!IsInsideTree())
        {
            GD.PushError($"SoundPool '{Name}': must be inside the tree to play sounds.");
            return false;
        }

        if (cue.Pick(_rng) is not { } pick)
        {
            return false;
        }

        var voice = TakeVoice(cue, kind);
        voice.Cue = cue;
        voice.Order = ++_playCount;
        voice.Play(
            pick.Stream,
            pick.VolumeDb,
            pick.PitchScale,
            cue.Bus.IsEmpty ? Bus : cue.Bus,
            position
        );
        return true;
    }

    private Voice TakeVoice(AudioCue cue, SoundPositioning kind)
    {
        var voice = FindVoice(cue, kind);
        if (voice is not null && voice.Kind == kind)
        {
            voice.Stop();
            return voice;
        }

        // A new voice, or one of another kind whose player has to be replaced.
        var replacement = new Voice(kind, CreatePlayer(kind));
        AddChild(replacement.Player);
        if (voice is null)
        {
            _voices.Add(replacement);
        }
        else
        {
            _voices[_voices.IndexOf(voice)] = replacement;
            voice.Player.QueueFree();
        }

        return replacement;
    }

    // The voice to reuse, or null to create one.
    private Voice? FindVoice(AudioCue cue, SoundPositioning kind)
    {
        if (cue.MaxPolyphony > 0)
        {
            var ofCue = _voices.Where(v => v.IsPlaying && v.Cue == cue).ToList();
            if (ofCue.Count >= cue.MaxPolyphony)
            {
                return Oldest(ofCue);
            }
        }

        var idle = _voices.Where(v => !v.IsPlaying).ToList();
        if (idle.Find(v => v.Kind == kind) is { } sameKind)
        {
            return sameKind;
        }

        if (_voices.Count < Mathf.Max(1, MaxVoices))
        {
            return null;
        }

        return idle.FirstOrDefault() ?? Oldest(_voices);
    }

    private static Voice Oldest(IEnumerable<Voice> voices)
    {
        return voices.MinBy(voice => voice.Order)!;
    }

    private sealed class Voice(SoundPositioning kind, Node player)
    {
        public SoundPositioning Kind { get; } = kind;
        public Node Player { get; } = player;
        public AudioCue? Cue { get; set; }
        public ulong Order { get; set; }

        public bool IsPlaying =>
            Player switch
            {
                AudioStreamPlayer p => p.Playing,
                AudioStreamPlayer2D p => p.Playing,
                AudioStreamPlayer3D p => p.Playing,
                _ => false,
            };

        public void Play(
            AudioStream stream,
            float volumeDb,
            float pitch,
            StringName bus,
            Vector3 at
        )
        {
            switch (Player)
            {
                case AudioStreamPlayer p:
                    (p.Stream, p.VolumeDb, p.PitchScale, p.Bus) = (stream, volumeDb, pitch, bus);
                    p.Play();
                    break;
                case AudioStreamPlayer2D p:
                    (p.Stream, p.VolumeDb, p.PitchScale, p.Bus) = (stream, volumeDb, pitch, bus);
                    p.GlobalPosition = new Vector2(at.X, at.Y);
                    p.Play();
                    break;
                case AudioStreamPlayer3D p:
                    (p.Stream, p.VolumeDb, p.PitchScale, p.Bus) = (stream, volumeDb, pitch, bus);
                    p.GlobalPosition = at;
                    p.Play();
                    break;
            }
        }

        public void Stop()
        {
            switch (Player)
            {
                case AudioStreamPlayer p:
                    p.Stop();
                    break;
                case AudioStreamPlayer2D p:
                    p.Stop();
                    break;
                case AudioStreamPlayer3D p:
                    p.Stop();
                    break;
            }
        }
    }
}
