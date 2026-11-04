using System.Linq;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.Audio.Resources;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Audio;

[TestSuite]
[RequireGodotRuntime]
public class AudioCueTests
{
    private static AudioCue MakeCue(int streams = 3)
    {
        var cue = new AudioCue
        {
            VolumeDb = -6f,
            VolumeVarianceDb = 3f,
            PitchScale = 1.5f,
            PitchVariance = 0.2f,
        };
        for (var i = 0; i < streams; i++)
        {
            cue.Streams.Add(AudioTestStreams.Silent(0.1f));
        }

        return cue;
    }

    [TestCase]
    public void Pick_WithTheSameRngState_GivesTheSamePicks()
    {
        // Arrange
        var cue = MakeCue();
        var first = new SeededRng(42, 7);
        var second = new SeededRng(42, 7);

        // Act
        var a = Enumerable.Range(0, 20).Select(_ => cue.Pick(first)).ToList();
        var b = Enumerable.Range(0, 20).Select(_ => cue.Pick(second)).ToList();

        // Assert
        a.ShouldBe(b);
    }

    [TestCase]
    public void Pick_WithVariance_StaysWithinTheConfiguredRanges()
    {
        // Arrange
        var cue = MakeCue();
        var rng = new SeededRng(1);

        // Act
        var picks = Enumerable.Range(0, 200).Select(_ => cue.Pick(rng)!).ToList();

        // Assert
        picks.ShouldAllBe(p => p.VolumeDb >= -9f && p.VolumeDb <= -3f);
        picks.ShouldAllBe(p => p.PitchScale >= 1.2f && p.PitchScale <= 1.8f);
        picks.Select(p => p.Stream).Distinct().Count().ShouldBe(3);
        picks.Select(p => p.PitchScale).Distinct().Count().ShouldBeGreaterThan(1);
    }

    [TestCase]
    public void Pick_WithoutVariance_UsesTheBaseValues()
    {
        // Arrange
        var cue = new AudioCue { VolumeDb = -3f, PitchScale = 0.5f };
        cue.Streams.Add(AudioTestStreams.Silent(0.1f));

        // Act
        var pick = cue.Pick(new SeededRng(5));

        // Assert
        pick.ShouldNotBeNull();
        pick.Stream.ShouldBe(cue.Streams[0]);
        pick.VolumeDb.ShouldBe(-3f);
        pick.PitchScale.ShouldBe(0.5f);
    }

    [TestCase]
    public void Pick_WhenThereAreNoStreams_ReturnsNull()
    {
        // Arrange
        var cue = new AudioCue();

        // Act
        var pick = cue.Pick(new SeededRng(0));

        // Assert
        pick.ShouldBeNull();
    }

    [TestCase]
    public void Pick_WhenTheStreamSlotIsEmpty_ReturnsNull()
    {
        // Arrange
        var cue = new AudioCue();
        cue.Streams.Add(null!);

        // Act
        var pick = cue.Pick(new SeededRng(0));

        // Assert
        pick.ShouldBeNull();
    }
}
