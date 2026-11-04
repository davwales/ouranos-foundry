using System.IO;
using Godot;
using Ouranos.Foundry.Features.Audio.Nodes;
using Ouranos.Foundry.Features.Audio.Resources;
using Ouranos.Foundry.Features.Persistence.Nodes;

namespace Ouranos.Foundry.Demos.Audio;

/// <summary>
/// Spams sound effects into a 12-voice pool, crossfades between music tracks and a shuffled
/// playlist, and saves the volume sliders to a settings slot that is loaded on start.
/// </summary>
public partial class AudioDemo : Control
{
    private const string SettingsSlot = "settings";
    private const int SpamCount = 20;

    [Export]
    public AudioManager? Audio { get; private set; }

    [Export]
    public SaveManager? Settings { get; private set; }

    [Export]
    public AudioCue? Blip { get; private set; }

    [Export]
    public AudioCue? Zap { get; private set; }

    [Export]
    public Godot.Collections.Array<AudioStream> Tracks { get; private set; } = [];

    [Export]
    public MusicPlaylist? Playlist { get; private set; }

    [Export]
    public Container? SoundButtons { get; private set; }

    [Export]
    public Container? MusicButtons { get; private set; }

    [Export]
    public Container? VolumeControls { get; private set; }

    [Export]
    public CheckBox? MuteToggle { get; private set; }

    [Export]
    public Label? VoicesLabel { get; private set; }

    [Export]
    public Label? MusicLabel { get; private set; }

    public override void _Ready()
    {
        if (Audio is null || Settings is null)
        {
            GD.PushError("AudioDemo: Audio and Settings must be assigned.");
            return;
        }

        // Volumes are settings, not game state, so they get their own slot.
        Settings.Register(Audio);
        if (Settings.HasSave(SettingsSlot))
        {
            Settings.Load(SettingsSlot);
        }

        var sounds = Audio.Sounds;
        AddButton(SoundButtons, "Blip", () => sounds.Play(Blip));
        AddButton(SoundButtons, $"Spam {SpamCount} blips", () => Spam(() => sounds.Play(Blip)));
        AddButton(SoundButtons, $"Spam {SpamCount} zaps", () => Spam(() => sounds.Play(Zap)));
        AddButton(SoundButtons, "Blip left", () => sounds.Play2D(Blip, new Vector2(0, Size.Y / 2)));
        AddButton(
            SoundButtons,
            "Blip right",
            () => sounds.Play2D(Blip, new Vector2(Size.X, Size.Y / 2))
        );

        var music = Audio.Music;
        foreach (var track in Tracks)
        {
            AddButton(MusicButtons, TrackName(track), () => music.Play(track));
        }

        AddButton(MusicButtons, "Shuffled playlist", () => music.PlayPlaylist(Playlist));
        AddButton(MusicButtons, "Stop", () => music.Stop());
        music.TrackChanged += OnTrackChanged;
        OnTrackChanged(music.CurrentTrack);

        if (MuteToggle is not null)
        {
            MuteToggle.ButtonPressed = Audio.IsMuted("Master");
            MuteToggle.Toggled += OnMuteToggled;
        }

        foreach (var slider in VolumeControls?.GetChildren() ?? [])
        {
            if (slider is VolumeSlider volumeSlider)
            {
                volumeSlider.DragEnded += OnVolumeDragEnded;
            }
        }
    }

    public override void _ExitTree()
    {
        if (Audio is null || Settings is null)
        {
            return;
        }

        SaveSettings();
        Settings.Unregister(Audio);
        Audio.Music.TrackChanged -= OnTrackChanged;
        if (MuteToggle is not null)
        {
            MuteToggle.Toggled -= OnMuteToggled;
        }

        foreach (var slider in VolumeControls?.GetChildren() ?? [])
        {
            if (slider is VolumeSlider volumeSlider)
            {
                volumeSlider.DragEnded -= OnVolumeDragEnded;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (Audio is null || VoicesLabel is null)
        {
            return;
        }

        var sounds = Audio.Sounds;
        VoicesLabel.Text =
            $"Voices playing: {sounds.GetPlayingCount(null)} / {sounds.MaxVoices}"
            + $"   (zaps: {sounds.GetPlayingCount(Zap)} / {Zap?.MaxPolyphony})";
    }

    private static void Spam(System.Action play)
    {
        for (var i = 0; i < SpamCount; i++)
        {
            play();
        }
    }

    private static void AddButton(Container? container, string text, System.Action onPressed)
    {
        if (container is null)
        {
            return;
        }

        var button = new Button { Text = text };
        button.Connect(BaseButton.SignalName.Pressed, Callable.From(onPressed));
        container.AddChild(button);
    }

    private static string TrackName(AudioStream? track)
    {
        return track is null ? "nothing" : Path.GetFileNameWithoutExtension(track.ResourcePath);
    }

    private void OnTrackChanged(AudioStream? track)
    {
        if (MusicLabel is not null && Audio is not null)
        {
            var playlist = Audio.Music.CurrentPlaylist is null ? "" : " (playlist)";
            MusicLabel.Text = $"Now playing: {TrackName(track)}{playlist}";
        }
    }

    private void OnMuteToggled(bool muted)
    {
        Audio?.SetMuted("Master", muted);
        SaveSettings();
    }

    private void OnVolumeDragEnded(bool valueChanged)
    {
        if (valueChanged)
        {
            SaveSettings();
        }
    }

    private void SaveSettings()
    {
        Settings?.Save(SettingsSlot);
    }
}
