using System.Collections.Generic;
using GdUnit4;
using Ouranos.Foundry.Core.Resources;
using Shouldly;

namespace Ouranos.Foundry.Tests.Core.Resources;

[TestSuite]
[RequireGodotRuntime]
public class FoundryRandomTests
{
    private static FoundryRandom MakeRandom(long seed = 42, long salt = 0)
    {
        return new FoundryRandom { Seed = seed, Salt = salt };
    }

    [TestCase]
    public void Randf_WhenSameSeedAndSalt_ReturnsSameSequence()
    {
        // Arrange
        var a = MakeRandom();
        var b = MakeRandom();

        // Act & Assert
        for (var i = 0; i < 50; i++)
        {
            b.Randf().ShouldBe(a.Randf());
        }
    }

    [TestCase]
    public void Reset_RestartsTheSequence()
    {
        // Arrange
        var random = MakeRandom();
        var first = random.RandiRange(0, 1_000_000);

        // Act
        random.Reset();

        // Assert
        random.RandiRange(0, 1_000_000).ShouldBe(first);
    }

    [TestCase]
    public void Seed_WhenChanged_RestartsFromTheNewSeed()
    {
        // Arrange
        var random = MakeRandom(seed: 1);
        random.Randf();
        var expected = MakeRandom(seed: 2).Randf();

        // Act
        random.Seed = 2;

        // Assert
        random.Randf().ShouldBe(expected);
    }

    [TestCase]
    public void RandiRange_IsInclusiveAndAcceptsReversedBounds()
    {
        // Arrange
        var random = MakeRandom();
        var seen = new HashSet<int>();

        // Act
        for (var i = 0; i < 200; i++)
        {
            seen.Add(random.RandiRange(3, 1));
        }

        // Assert
        seen.ShouldBe([1, 2, 3], ignoreOrder: true);
    }

    [TestCase]
    public void RandfRange_WhenBoundsAreEqual_ReturnsThatValue()
    {
        // Act & Assert
        MakeRandom().RandfRange(2.5f, 2.5f).ShouldBe(2.5f);
    }

    [TestCase]
    public void RandWeighted_NeverPicksZeroWeights()
    {
        // Arrange
        var random = MakeRandom();

        // Act & Assert
        for (var i = 0; i < 200; i++)
        {
            random.RandWeighted([0f, 1f, 0f]).ShouldBe(1);
        }
    }

    [TestCase]
    public void Shuffle_IsDeterministicForASeed()
    {
        // Arrange
        Godot.Collections.Array a = [1, 2, 3, 4, 5, 6, 7, 8];
        Godot.Collections.Array b = [1, 2, 3, 4, 5, 6, 7, 8];

        // Act
        MakeRandom().Shuffle(a);
        MakeRandom().Shuffle(b);

        // Assert
        a.ShouldBe(b);
        a.Count.ShouldBe(8);
    }

    [TestCase]
    public void Methods_AreCallableThroughGodot()
    {
        // Arrange: Call goes through ClassDB, the same path GDScript uses.
        var random = MakeRandom();
        random.Set(FoundryRandom.PropertyName.Seed, 7L);
        var expected = MakeRandom(seed: 7).RandiRange(1, 6);

        // Act
        var result = random.Call(FoundryRandom.MethodName.RandiRange, 1, 6);

        // Assert
        result.AsInt32().ShouldBe(expected);
    }
}
