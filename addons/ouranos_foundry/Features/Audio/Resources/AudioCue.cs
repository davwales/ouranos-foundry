#nullable enable

using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.Audio.Types;

namespace Ouranos.Foundry.Features.Audio.Resources;

/// <summary>
/// A sound effect: one or more streams to pick from at random, with volume and pitch
/// variance so repeated plays don't sound identical. Play it through a <c>SoundPool</c>.
/// </summary>
[GlobalClass]
public partial class AudioCue : Resource
{
    /// <summary>Each play picks one of these at random. Add variations of the same sound.</summary>
    [Export]
    public Godot.Collections.Array<AudioStream> Streams { get; set; } = [];

    [Export(PropertyHint.Range, "-80,24,0.1,suffix:dB")]
    public float VolumeDb { get; set; }

    /// <summary>Each play shifts the volume by up to this much, up or down.</summary>
    [Export(PropertyHint.Range, "0,24,0.1,suffix:dB")]
    public float VolumeVarianceDb { get; set; }

    [Export(PropertyHint.Range, "0.01,4,0.01")]
    public float PitchScale { get; set; } = 1f;

    /// <summary>Each play scales the pitch by up to this fraction, up or down: 0.1 is ±10%.</summary>
    [Export(PropertyHint.Range, "0,0.9,0.01")]
    public float PitchVariance { get; set; }

    /// <summary>The bus to play on. Empty uses the pool's <c>Bus</c>.</summary>
    [Export]
    public StringName Bus { get; set; } = "";

    /// <summary>
    /// How many plays of this cue may sound at once. Playing it again at the limit restarts
    /// its oldest voice. 0 means no limit other than the pool's <c>MaxVoices</c>.
    /// </summary>
    [Export(PropertyHint.Range, "0,64,1,or_greater")]
    public int MaxPolyphony { get; set; }

    /// <summary>
    /// Picks a stream and varies its volume and pitch. The same <paramref name="rng"/> state
    /// gives the same pick. Returns null (with an error) when there is nothing to play.
    /// Override for custom selection, e.g. never repeating the last stream.
    /// </summary>
    public virtual AudioCuePick? Pick(SeededRng rng)
    {
        if (Streams.Count == 0)
        {
            GD.PushError($"AudioCue '{ResourcePath}': has no streams.");
            return null;
        }

        // Always draw all three values, so a cue consumes the same amount of the stream
        // whatever its settings.
        var stream = Streams[rng.Next(Streams.Count)];
        var volume = VolumeDb + Spread(rng) * VolumeVarianceDb;
        var pitch = PitchScale * (1f + Spread(rng) * PitchVariance);
        if (stream is null)
        {
            GD.PushError($"AudioCue '{ResourcePath}': has an empty stream slot.");
            return null;
        }

        return new AudioCuePick(stream, volume, Mathf.Max(pitch, 0.01f));
    }

    // Uniform in [-1, 1).
    private static float Spread(SeededRng rng)
    {
        return rng.NextFloat() * 2f - 1f;
    }
}
