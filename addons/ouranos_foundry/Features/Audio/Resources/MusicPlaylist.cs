#nullable enable

using Godot;

namespace Ouranos.Foundry.Features.Audio.Resources;

/// <summary>Tracks for a <c>MusicPlayer</c> to play one after another, crossfading between them.</summary>
[GlobalClass]
public partial class MusicPlaylist : Resource
{
    [Export]
    public Godot.Collections.Array<AudioStream> Tracks { get; set; } = [];

    /// <summary>Plays the tracks in random order, never the same one twice in a row.</summary>
    [Export]
    public bool Shuffle { get; set; }

    /// <summary>Starts over after the last track. Off, the music stops after it.</summary>
    [Export]
    public bool Loop { get; set; } = true;
}
