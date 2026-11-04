using System.Linq;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.Audio.Nodes;
using Ouranos.Foundry.Features.Audio.Resources;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Audio;

[TestSuite]
[RequireGodotRuntime]
public class SoundPoolTests
{
    private static SoundPool CreatePool(int maxVoices = 32)
    {
        var pool = new SoundPool().With(SoundPool.PropertyName.MaxVoices, maxVoices);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(pool);
        return pool;
    }

    private static AudioCue MakeCue(int maxPolyphony = 0, string bus = "")
    {
        var cue = new AudioCue { MaxPolyphony = maxPolyphony, Bus = bus };
        cue.Streams.Add(AudioTestStreams.Silent());
        return cue;
    }

    [TestCase]
    public void Play_WhenBelowTheLimits_PlaysEachOnItsOwnVoice()
    {
        // Arrange
        var pool = CreatePool();
        var cue = MakeCue();

        // Act
        var played = Enumerable.Range(0, 3).Select(_ => pool.Play(cue)).ToList();

        // Assert
        played.ShouldAllBe(p => p);
        pool.GetPlayingCount(cue).ShouldBe(3);
        pool.GetChildCount().ShouldBe(3);

        pool.Free();
    }

    [TestCase]
    public void Play_WhenTheCueIsAtMaxPolyphony_ReusesItsVoices()
    {
        // Arrange
        var pool = CreatePool();
        var capped = MakeCue(maxPolyphony: 2);
        var other = MakeCue();

        // Act
        for (var i = 0; i < 5; i++)
        {
            pool.Play(capped);
        }

        pool.Play(other);

        // Assert
        pool.GetPlayingCount(capped).ShouldBe(2);
        pool.GetPlayingCount(other).ShouldBe(1);
        pool.GetChildCount().ShouldBe(3);

        pool.Free();
    }

    [TestCase]
    public void Play_WhenThePoolIsFull_StealsAVoice()
    {
        // Arrange
        var pool = CreatePool(maxVoices: 3);
        var first = MakeCue();
        var second = MakeCue();

        // Act
        pool.Play(first);
        pool.Play(second);
        pool.Play(second);
        var played = pool.Play(second);

        // Assert: the oldest voice, playing the first cue, was taken.
        played.ShouldBeTrue();
        pool.GetPlayingCount(null).ShouldBe(3);
        pool.GetPlayingCount(first).ShouldBe(0);
        pool.GetChildCount().ShouldBe(3);

        pool.Free();
    }

    [TestCase]
    public void Play_WhenAVoiceHasFinished_ReusesIt()
    {
        // Arrange
        var pool = CreatePool();
        var cue = MakeCue();
        pool.Play(cue);
        pool.Stop(cue);

        // Act
        pool.Play(cue);

        // Assert
        pool.GetChildCount().ShouldBe(1);
        pool.GetPlayingCount(cue).ShouldBe(1);

        pool.Free();
    }

    [TestCase]
    public void Play2DAndPlay3D_PlayPositionalPlayersAtThePosition()
    {
        // Arrange
        var pool = CreatePool();
        var cue = MakeCue();

        // Act
        pool.Play2D(cue, new Vector2(10, 20));
        pool.Play3D(cue, new Vector3(1, 2, 3));

        // Assert
        pool.GetChildren()
            .OfType<AudioStreamPlayer2D>()
            .Single()
            .GlobalPosition.ShouldBe(new Vector2(10, 20));
        pool.GetChildren()
            .OfType<AudioStreamPlayer3D>()
            .Single()
            .GlobalPosition.ShouldBe(new Vector3(1, 2, 3));
        pool.GetPlayingCount(cue).ShouldBe(2);

        pool.Free();
    }

    [TestCase]
    public void Play_WhenFullOfAnotherKind_ReplacesTheVoice()
    {
        // Arrange
        var pool = CreatePool(maxVoices: 1);
        var cue = MakeCue();
        pool.Play(cue);

        // Act
        pool.Play2D(cue, Vector2.Zero);

        // Assert
        pool.GetPlayingCount(null).ShouldBe(1);
        pool.GetChildren().OfType<AudioStreamPlayer2D>().Single().Playing.ShouldBeTrue();

        pool.Free();
    }

    [TestCase]
    public void Play_UsesTheCueBusOrElseThePoolBus()
    {
        // Arrange: players report Master for a bus that doesn't exist, so add one.
        const string cueBus = "SoundPoolTestBus";
        AudioServer.AddBus();
        AudioServer.SetBusName(AudioServer.BusCount - 1, cueBus);
        var pool = CreatePool().With(SoundPool.PropertyName.Bus, "Master");

        // Act
        pool.Play(MakeCue(bus: cueBus));
        pool.Play(MakeCue());

        // Assert
        var buses = pool.GetChildren().OfType<AudioStreamPlayer>().Select(p => p.Bus.ToString());
        buses.ShouldBe([cueBus, "Master"]);

        pool.Free();
        AudioServer.RemoveBus(AudioServer.GetBusIndex(cueBus));
    }

    [TestCase]
    public void Play_WhenTheCueIsNullOrEmpty_ReturnsFalse()
    {
        // Arrange
        var pool = CreatePool();

        // Act
        var nullPlayed = pool.Play(null);
        var emptyPlayed = pool.Play(new AudioCue());

        // Assert
        nullPlayed.ShouldBeFalse();
        emptyPlayed.ShouldBeFalse();
        pool.GetChildCount().ShouldBe(0);

        pool.Free();
    }

    [TestCase]
    public void Play_WhenOutsideTheTree_ReturnsFalse()
    {
        // Arrange
        var pool = new SoundPool();

        // Act
        var played = pool.Play(MakeCue());

        // Assert
        played.ShouldBeFalse();

        pool.Free();
    }

    [TestCase]
    public void Stop_StopsOnlyThatCue()
    {
        // Arrange
        var pool = CreatePool();
        var stopped = MakeCue();
        var kept = MakeCue();
        pool.Play(stopped);
        pool.Play(stopped);
        pool.Play(kept);

        // Act
        pool.Stop(stopped);

        // Assert
        pool.GetPlayingCount(stopped).ShouldBe(0);
        pool.GetPlayingCount(kept).ShouldBe(1);

        pool.Free();
    }

    [TestCase]
    public void StopAll_StopsEveryVoice()
    {
        // Arrange
        var pool = CreatePool();
        pool.Play(MakeCue());
        pool.Play2D(MakeCue(), Vector2.Zero);

        // Act
        pool.StopAll();

        // Assert
        pool.GetPlayingCount(null).ShouldBe(0);

        pool.Free();
    }
}
