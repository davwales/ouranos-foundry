using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.Audio.Nodes;
using Ouranos.Foundry.Features.Audio.Resources;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Audio;

[TestSuite]
[RequireGodotRuntime]
public class MusicPlayerTests
{
    private static (MusicPlayer music, List<AudioStream?> changes) CreatePlayer(float fade = 1f)
    {
        var music = new MusicPlayer().With(MusicPlayer.PropertyName.FadeDuration, fade);
        var changes = new List<AudioStream?>();
        music.TrackChanged += track => changes.Add(track);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(music);
        return (music, changes);
    }

    private static MusicPlaylist MakePlaylist(int tracks, bool loop = true, bool shuffle = false)
    {
        var playlist = new MusicPlaylist { Loop = loop, Shuffle = shuffle };
        for (var i = 0; i < tracks; i++)
        {
            playlist.Tracks.Add(AudioTestStreams.Silent());
        }

        return playlist;
    }

    private static AudioStreamPlayer PlayerOf(MusicPlayer music, AudioStream track)
    {
        return music
            .GetChildren()
            .OfType<AudioStreamPlayer>()
            .Single(p => p.Stream == track && p.Playing);
    }

    private static void EndCurrentTrack(MusicPlayer music)
    {
        PlayerOf(music, music.CurrentTrack!).Stop();
        music._Process(0);
    }

    [TestCase]
    public void Play_FadesTheTrackInOverTheFadeDuration()
    {
        // Arrange
        var (music, changes) = CreatePlayer(fade: 1f);
        var track = AudioTestStreams.Silent();

        // Act
        music.Play(track);
        music._Process(0.5);
        var halfway = PlayerOf(music, track).VolumeDb;
        music._Process(0.5);

        // Assert
        music.CurrentTrack.ShouldBe(track);
        halfway.ShouldBe(Mathf.LinearToDb(0.5f), 0.01f);
        PlayerOf(music, track).VolumeDb.ShouldBe(0f, 0.01f);
        changes.ShouldBe([track]);

        music.Free();
    }

    [TestCase]
    public void Play_WhenAnotherTrackIsPlaying_CrossfadesAndStopsIt()
    {
        // Arrange
        var (music, _) = CreatePlayer();
        var first = AudioTestStreams.Silent();
        var second = AudioTestStreams.Silent();
        music.Play(first, 0f);

        // Act
        music.Play(second, 1f);
        music._Process(0.5);
        var firstHalfway = PlayerOf(music, first).VolumeDb;
        var secondHalfway = PlayerOf(music, second).VolumeDb;
        music._Process(0.6);

        // Assert
        firstHalfway.ShouldBe(Mathf.LinearToDb(0.5f), 0.01f);
        secondHalfway.ShouldBe(Mathf.LinearToDb(0.5f), 0.01f);
        music.GetChildren().OfType<AudioStreamPlayer>().Count(p => p.Playing).ShouldBe(1);
        music.CurrentTrack.ShouldBe(second);

        music.Free();
    }

    [TestCase]
    public void Play_WhenTheTrackIsAlreadyPlaying_ChangesNothing()
    {
        // Arrange
        var (music, changes) = CreatePlayer();
        var track = AudioTestStreams.Silent();
        music.Play(track);

        // Act
        music.Play(track);

        // Assert
        changes.Count.ShouldBe(1);
        music.GetChildCount().ShouldBe(1);

        music.Free();
    }

    [TestCase]
    public void Play_WhenTheTrackIsFadingOut_FadesItBackIn()
    {
        // Arrange
        var (music, _) = CreatePlayer();
        var first = AudioTestStreams.Silent();
        music.Play(first, 0f);
        music.Play(AudioTestStreams.Silent(), 1f);
        music._Process(0.5);

        // Act
        music.Play(first, 1f);
        music._Process(0.5);

        // Assert
        music.GetChildCount().ShouldBe(2);
        PlayerOf(music, first).VolumeDb.ShouldBe(0f, 0.01f);

        music.Free();
    }

    [TestCase]
    public void Stop_FadesOutAndReportsNoTrack()
    {
        // Arrange
        var (music, changes) = CreatePlayer();
        var track = AudioTestStreams.Silent();
        music.Play(track, 0f);

        // Act
        music.Stop(1f);
        music._Process(1.1);

        // Assert
        music.CurrentTrack.ShouldBeNull();
        music.GetChildren().OfType<AudioStreamPlayer>().ShouldAllBe(p => !p.Playing);
        changes.ShouldBe([track, null]);

        music.Free();
    }

    [TestCase]
    public void PlayPlaylist_WhenATrackEnds_PlaysTheNextAndLoops()
    {
        // Arrange
        var (music, _) = CreatePlayer();
        var playlist = MakePlaylist(2);

        // Act
        music.PlayPlaylist(playlist);
        var first = music.CurrentTrack;
        EndCurrentTrack(music);
        var second = music.CurrentTrack;
        EndCurrentTrack(music);

        // Assert
        first.ShouldBe(playlist.Tracks[0]);
        second.ShouldBe(playlist.Tracks[1]);
        music.CurrentTrack.ShouldBe(playlist.Tracks[0]);
        music.CurrentPlaylist.ShouldBe(playlist);

        music.Free();
    }

    [TestCase]
    public void PlayPlaylist_WithoutLoop_StopsAfterTheLastTrack()
    {
        // Arrange
        var (music, changes) = CreatePlayer();
        var playlist = MakePlaylist(1, loop: false);
        music.PlayPlaylist(playlist);

        // Act
        EndCurrentTrack(music);

        // Assert
        music.CurrentTrack.ShouldBeNull();
        music.CurrentPlaylist.ShouldBeNull();
        changes.ShouldBe([playlist.Tracks[0], null]);

        music.Free();
    }

    [TestCase]
    public void PlayPlaylist_WithShuffle_PlaysEveryTrackOncePerPass()
    {
        // Arrange
        var (music, _) = CreatePlayer();
        music.SetRng(new SeededRng(3));
        var playlist = MakePlaylist(5, shuffle: true);
        var played = new List<AudioStream>();

        // Act
        music.PlayPlaylist(playlist);
        for (var i = 0; i < 10; i++)
        {
            played.Add(music.CurrentTrack!);
            EndCurrentTrack(music);
        }

        // Assert
        played.Take(5).ShouldBe(playlist.Tracks, ignoreOrder: true);
        played.Skip(5).ShouldBe(playlist.Tracks, ignoreOrder: true);
        played.Zip(played.Skip(1)).ShouldAllBe(pair => pair.First != pair.Second);

        music.Free();
    }

    [TestCase]
    public void PlayPlaylist_WhenAlreadyPlayingIt_ChangesNothing()
    {
        // Arrange
        var (music, changes) = CreatePlayer();
        var playlist = MakePlaylist(2);
        music.PlayPlaylist(playlist);
        EndCurrentTrack(music);

        // Act
        music.PlayPlaylist(playlist);

        // Assert
        music.CurrentTrack.ShouldBe(playlist.Tracks[1]);
        changes.Count.ShouldBe(2);

        music.Free();
    }

    [TestCase]
    public void PlayPlaylist_WhenItHasNoTracks_DoesNothing()
    {
        // Arrange
        var (music, changes) = CreatePlayer();

        // Act
        music.PlayPlaylist(new MusicPlaylist());

        // Assert
        music.CurrentPlaylist.ShouldBeNull();
        changes.ShouldBeEmpty();

        music.Free();
    }

    [TestCase]
    public void Ready_WithAutoplay_StartsThePlaylist()
    {
        // Arrange
        var playlist = MakePlaylist(2);
        var music = new MusicPlayer()
            .With(MusicPlayer.PropertyName.Playlist, playlist)
            .With(MusicPlayer.PropertyName.Autoplay, true);

        // Act
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(music);

        // Assert
        music.CurrentTrack.ShouldBe(playlist.Tracks[0]);

        music.Free();
    }
}
