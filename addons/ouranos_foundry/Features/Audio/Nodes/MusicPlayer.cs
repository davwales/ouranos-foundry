#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.Audio.Resources;

namespace Ouranos.Foundry.Features.Audio.Nodes;

/// <summary>
/// Plays music, crossfading whenever the track changes: a single track with <c>Play</c>, or a
/// <see cref="MusicPlaylist"/> with <c>PlayPlaylist</c>, which crossfades into each next
/// track as the current one ends. Asking for what is already playing changes nothing, so
/// re-entering an area doesn't restart its music.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class MusicPlayer : Node
{
    /// <summary>Pass as a fade duration to use <see cref="FadeDuration"/>.</summary>
    public const float DefaultFade = -1f;

    private const ulong RngSalt = 0xA0D11;
    private const float SilentDb = -80f;

    [Export]
    public StringName Bus { get; private set; } = "Music";

    /// <summary>Seconds a crossfade, fade-in or fade-out takes unless a call says otherwise.</summary>
    [Export(PropertyHint.Range, "0,10,0.05,or_greater,suffix:s")]
    public float FadeDuration { get; private set; } = 1.5f;

    /// <summary>Played when the node is ready, if <see cref="Autoplay"/> is on.</summary>
    [Export]
    public MusicPlaylist? Playlist { get; private set; }

    [Export]
    public bool Autoplay { get; private set; }

    /// <summary>The music changed to <paramref name="track"/>, or stopped when it is null.</summary>
    [Signal]
    public delegate void TrackChangedEventHandler(AudioStream? track);

    private readonly List<Channel> _channels = [];
    private readonly List<AudioStream> _order = [];
    private Channel? _current;
    private int _orderIndex;
    private double _lastPosition;
    private SeededRng _rng = new(0, RngSalt);

    /// <summary>The track playing or fading in, or null when the music is stopped.</summary>
    public AudioStream? CurrentTrack => _current?.Player.Stream;

    /// <summary>The playlist being played, or null when playing a single track or nothing.</summary>
    public MusicPlaylist? CurrentPlaylist { get; private set; }

    public override void _Ready()
    {
        if (Autoplay && Playlist is not null)
        {
            PlayPlaylist(Playlist, 0f);
        }
    }

    public override void _Process(double delta)
    {
        foreach (var channel in _channels)
        {
            channel.Update((float)delta);
        }

        if (CurrentPlaylist is not null && _current is not null && IsTrackEnding(_current.Player))
        {
            var next = NextTrack();
            if (next is null)
            {
                CurrentPlaylist = null;
            }

            Switch(next, FadeDuration, restart: true);
        }
    }

    /// <summary>Uses <paramref name="rng"/> for later shuffles, e.g. to make them replayable.</summary>
    public void SetRng(SeededRng rng)
    {
        _rng = rng;
    }

    /// <summary>
    /// Crossfades to <paramref name="track"/> over <paramref name="fadeDuration"/> seconds
    /// (<see cref="DefaultFade"/>: <see cref="FadeDuration"/>), leaving any playlist. Loop
    /// the stream itself (its import settings) to repeat it. Null stops the music.
    /// </summary>
    public void Play(AudioStream? track, float fadeDuration = DefaultFade)
    {
        CurrentPlaylist = null;
        Switch(track, fadeDuration, restart: false);
    }

    /// <summary>
    /// Crossfades to the first track of <paramref name="playlist"/> and plays the rest in turn.
    /// Does nothing if that playlist is already playing.
    /// </summary>
    public void PlayPlaylist(MusicPlaylist? playlist, float fadeDuration = DefaultFade)
    {
        if (playlist is null || !playlist.Tracks.Any(track => track is not null))
        {
            GD.PushError($"MusicPlayer '{Name}': the playlist is missing or has no tracks.");
            return;
        }

        if (playlist == CurrentPlaylist)
        {
            return;
        }

        CurrentPlaylist = playlist;
        _order.Clear();
        _orderIndex = 0;
        Switch(NextTrack(), fadeDuration, restart: true);
    }

    /// <summary>Fades the music out over <paramref name="fadeDuration"/> seconds.</summary>
    public void Stop(float fadeDuration = DefaultFade)
    {
        Play(null, fadeDuration);
    }

    private void Switch(AudioStream? track, float fadeDuration, bool restart)
    {
        if (!restart && track == CurrentTrack)
        {
            return;
        }

        var fade = fadeDuration < 0 ? FadeDuration : fadeDuration;
        _current?.FadeTo(0f, fade);
        _current = null;
        _lastPosition = 0;

        if (track is not null)
        {
            // Fade a track that is still fading out back in, rather than restarting it.
            var fadingOut = restart
                ? null
                : _channels.Find(c => c.Player.Stream == track && c.Player.Playing);
            _current = fadingOut ?? StartChannel(track, fade);
            _current.FadeTo(1f, fade);
        }

        EmitSignal(SignalName.TrackChanged, track!);
    }

    private Channel StartChannel(AudioStream track, float fade)
    {
        var channel = _channels.Find(c => !c.Player.Playing);
        if (channel is null)
        {
            channel = new Channel(new AudioStreamPlayer());
            _channels.Add(channel);
            AddChild(channel.Player);
        }

        channel.Player.Stream = track;
        channel.Player.Bus = Bus;
        channel.Gain = fade > 0 ? 0f : 1f;
        channel.Update(0f);
        channel.Player.Play();
        return channel;
    }

    // True once the track has ended, or is close enough to its end to start crossfading
    // into the next. A looping stream never ends, so a wrap back to the start counts too.
    private bool IsTrackEnding(AudioStreamPlayer player)
    {
        var position = player.GetPlaybackPosition();
        var wrapped = position < _lastPosition;
        _lastPosition = position;

        var length = player.Stream?.GetLength() ?? 0;
        var crossfade = Mathf.Min(FadeDuration, length / 2);
        return !player.Playing || wrapped || (length > 0 && length - position <= crossfade);
    }

    // The playlist's next track in play order, reshuffling at the start of each pass. Null
    // when a non-looping playlist has finished.
    private AudioStream? NextTrack()
    {
        if (CurrentPlaylist is not { } playlist)
        {
            return null;
        }

        if (_orderIndex >= _order.Count)
        {
            var last = _order.LastOrDefault();
            if (_order.Count > 0 && !playlist.Loop)
            {
                return null;
            }

            _order.Clear();
            _order.AddRange(playlist.Tracks.Where(track => track is not null));
            if (playlist.Shuffle)
            {
                Shuffle(_order, last);
            }

            _orderIndex = 0;
        }

        return _order.Count > 0 ? _order[_orderIndex++] : null;
    }

    // Fisher-Yates, then keeps the previous pass's last track from playing twice in a row.
    private void Shuffle(List<AudioStream> tracks, AudioStream? previous)
    {
        for (var i = tracks.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (tracks[i], tracks[j]) = (tracks[j], tracks[i]);
        }

        if (tracks.Count > 1 && tracks[0] == previous)
        {
            (tracks[0], tracks[^1]) = (tracks[^1], tracks[0]);
        }
    }

    private sealed class Channel(AudioStreamPlayer player)
    {
        private float _target;
        private float _rate;

        public AudioStreamPlayer Player { get; } = player;

        // Linear amplitude, 0 to 1.
        public float Gain { get; set; }

        // A duration of 0 jumps straight to the target.
        public void FadeTo(float target, float duration)
        {
            _target = target;
            _rate = duration > 0 ? 1f / duration : 0f;
            if (duration <= 0)
            {
                Gain = target;
                Update(0f);
            }
        }

        public void Update(float delta)
        {
            Gain = Mathf.MoveToward(Gain, _target, _rate * delta);

            Player.VolumeDb = Gain > 0 ? Mathf.Max(Mathf.LinearToDb(Gain), SilentDb) : SilentDb;
            if (Gain <= 0 && _target <= 0 && Player.Playing)
            {
                Player.Stop();
            }
        }
    }
}
