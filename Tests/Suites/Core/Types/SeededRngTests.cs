using System;
using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using Ouranos.Foundry.Core.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Core.Types;

[TestSuite]
public class SeededRngTests
{
    [TestCase]
    public void SameSeedAndSalt_ProducesSameSequence()
    {
        var a = new SeededRng(42, 100);
        var b = new SeededRng(42, 100);

        for (var i = 0; i < 100; i++)
        {
            b.Next().ShouldBe(a.Next());
        }
    }

    [TestCase]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var a = new SeededRng(42);
        var b = new SeededRng(99);

        var matchCount = 0;
        for (var i = 0; i < 100; i++)
        {
            if (a.Next() == b.Next())
            {
                matchCount++;
            }
        }

        // With different seeds, we should see very few collisions
        (matchCount < 10).ShouldBeTrue($"Expected <10 matches, got {matchCount}");
    }

    [TestCase]
    public void DifferentSalts_ProduceDifferentSequences()
    {
        var a = new SeededRng(42, 100);
        var b = new SeededRng(42, 200);

        var matchCount = 0;
        for (var i = 0; i < 100; i++)
        {
            if (a.Next() == b.Next())
            {
                matchCount++;
            }
        }

        (matchCount < 10).ShouldBeTrue($"Expected <10 matches, got {matchCount}");
    }

    [TestCase]
    public void Next_WithMaxValue_ReturnsValuesInRange()
    {
        var rng = new SeededRng(42);

        for (var i = 0; i < 1000; i++)
        {
            var value = rng.Next(10);
            value.ShouldBeInRange(0, 9);
        }
    }

    [TestCase]
    public void Next_WithMinMax_ReturnsValuesInRange()
    {
        var rng = new SeededRng(42);

        for (var i = 0; i < 1000; i++)
        {
            var value = rng.Next(5, 15);
            value.ShouldBeInRange(5, 14);
        }
    }

    [TestCase(0)]
    [TestCase(7)]
    [TestCase(-3)]
    public void Next_WhenMinEqualsMax_ReturnsMinWithoutAdvancing(int value)
    {
        // Arrange
        var rng = new SeededRng(42);
        var untouched = new SeededRng(42);

        // Act
        var result = rng.Next(value, value);

        // Assert
        result.ShouldBe(value);
        rng.Next().ShouldBe(untouched.Next());
    }

    [TestCase]
    public void Next_WhenMinExceedsMax_Throws()
    {
        // Arrange
        var rng = new SeededRng(42);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => rng.Next(5, 4));
    }

    [TestCase]
    public void NextDouble_ReturnsValuesBetweenZeroAndOne()
    {
        var rng = new SeededRng(42);

        for (var i = 0; i < 1000; i++)
        {
            var value = rng.NextDouble();
            value.ShouldBeInRange(0.0, 1.0);
        }
    }

    [TestCase]
    public void Derive_ProducesIndependentStreams()
    {
        var parent = new SeededRng(42);
        var child1 = parent.Derive();
        var child2 = parent.Derive();

        // Children should produce different sequences
        var matchCount = 0;
        for (var i = 0; i < 100; i++)
        {
            if (child1.Next() == child2.Next())
            {
                matchCount++;
            }
        }

        (matchCount < 10).ShouldBeTrue($"Expected <10 matches, got {matchCount}");
    }

    [TestCase]
    public void Derive_IsDeterministic()
    {
        var parent1 = new SeededRng(42);
        var parent2 = new SeededRng(42);

        var child1 = parent1.Derive();
        var child2 = parent2.Derive();

        for (var i = 0; i < 100; i++)
        {
            child2.Next().ShouldBe(child1.Next());
        }
    }

    [TestCase]
    public void NextElement_FromArray_ReturnsElementsFromArray()
    {
        var rng = new SeededRng(42);
        var array = new[] { "a", "b", "c" };

        for (var i = 0; i < 100; i++)
        {
            var element = rng.NextElement(array);
            array.ShouldContain(element);
        }
    }

    [TestCase]
    public void ForCoords_OverASampleGrid_GivesEveryCoordinateADistinctStream()
    {
        // Arrange
        var firstValues = new HashSet<double>();
        var count = 0;

        // Act
        for (var x = -32; x < 32; x++)
        {
            for (var y = -32; y < 32; y++)
            {
                firstValues.Add(SeededRng.ForCoords(1234, x, y, 7).NextDouble());
                count++;
            }
        }

        foreach (var (x, y) in new[] { (int.MinValue, 0), (int.MaxValue, 0), (0, int.MinValue) })
        {
            firstValues.Add(SeededRng.ForCoords(1234, x, y, 7).NextDouble());
            count++;
        }

        // Assert
        firstValues.Count.ShouldBe(count);
    }

    [TestCase]
    public void ForCoords_WithSameInputs_ProducesSameSequence()
    {
        // Arrange
        var a = SeededRng.ForCoords(42, -3, 5, 100);
        var b = SeededRng.ForCoords(42, -3, 5, 100);

        // Act & Assert
        for (var i = 0; i < 100; i++)
        {
            b.Next().ShouldBe(a.Next());
        }
    }

    [TestCase]
    public void ForCoords_WithDifferentSaltOrSeed_ProducesDifferentStreams()
    {
        // Arrange
        var baseline = First(SeededRng.ForCoords(42, 2, 3, 100));

        // Act
        var otherSalt = First(SeededRng.ForCoords(42, 2, 3, 101));
        var otherSeed = First(SeededRng.ForCoords(43, 2, 3, 100));
        var swappedCoords = First(SeededRng.ForCoords(42, 3, 2, 100));

        // Assert
        otherSalt.ShouldNotBe(baseline);
        otherSeed.ShouldNotBe(baseline);
        swappedCoords.ShouldNotBe(baseline);
    }

    private static int[] First(SeededRng rng)
    {
        return [rng.Next(), rng.Next(), rng.Next(), rng.Next()];
    }

    [TestCase]
    public void Next_WithMaxValue_IsUniformAcrossBuckets()
    {
        // Arrange
        var rng = new SeededRng(42);
        const int buckets = 7;
        const int samples = 140_000;
        var counts = new int[buckets];

        // Act
        for (var i = 0; i < samples; i++)
        {
            counts[rng.Next(buckets)]++;
        }

        // Assert
        AssertUniform(counts, samples);
    }

    [TestCase]
    public void Next_WithFullIntRange_IsBalancedAroundZero()
    {
        // Arrange: max - min overflowed int here before, which skewed or broke the results.
        var rng = new SeededRng(42);
        const int samples = 100_000;
        var lowerHalf = 0;

        // Act
        for (var i = 0; i < samples; i++)
        {
            if (rng.Next(int.MinValue, int.MaxValue) < 0)
            {
                lowerHalf++;
            }
        }

        // Assert
        (lowerHalf / (double)samples).ShouldBe(0.5, 0.01);
    }

    [TestCase]
    public void Next_WithFullIntRange_DoesNotOverflow()
    {
        // Arrange
        var rng = new SeededRng(42);
        var sawNegative = false;
        var sawPositive = false;

        // Act
        for (var i = 0; i < 1000; i++)
        {
            var value = rng.Next(int.MinValue, int.MaxValue);
            value.ShouldBeLessThan(int.MaxValue);
            sawNegative |= value < 0;
            sawPositive |= value > 0;
        }

        // Assert
        sawNegative.ShouldBeTrue();
        sawPositive.ShouldBeTrue();
    }

    [TestCase]
    public void Next_WithNegativeRange_ReturnsValuesInRange()
    {
        // Arrange
        var rng = new SeededRng(42);

        // Act & Assert
        for (var i = 0; i < 1000; i++)
        {
            rng.Next(-10, -5).ShouldBeInRange(-10, -6);
        }
    }

    [TestCase]
    public void Next_WhenMaxValueIsNotPositive_Throws()
    {
        // Arrange
        var rng = new SeededRng(42);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => rng.Next(0));
    }

    [TestCase]
    public void NextInclusive_ReturnsBothEndpoints()
    {
        // Arrange
        var rng = new SeededRng(42);
        var seen = new HashSet<int>();

        // Act
        for (var i = 0; i < 200; i++)
        {
            seen.Add(rng.NextInclusive(1, 3));
        }

        // Assert
        seen.OrderBy(v => v).ShouldBe([1, 2, 3]);
    }

    [TestCase]
    public void NextInclusive_WithFullIntRange_DoesNotOverflow()
    {
        // Arrange
        var rng = new SeededRng(42);

        // Act & Assert
        Should.NotThrow(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                rng.NextInclusive(int.MinValue, int.MaxValue);
            }
        });
        rng.NextInclusive(7, 7).ShouldBe(7);
    }

    [TestCase]
    public void NextBool_IsBalanced()
    {
        // Arrange
        var rng = new SeededRng(42);
        const int samples = 100_000;

        // Act
        var trues = Enumerable.Range(0, samples).Count(_ => rng.NextBool());

        // Assert
        (trues / (double)samples).ShouldBe(0.5, 0.01);
    }

    [TestCase]
    public void NextFloat_WithRange_ReturnsValuesInRange()
    {
        // Arrange
        var rng = new SeededRng(42);

        // Act & Assert
        for (var i = 0; i < 1000; i++)
        {
            var value = rng.NextFloat(-2.5f, 4f);
            value.ShouldBeGreaterThanOrEqualTo(-2.5f);
            value.ShouldBeLessThan(4f);
        }
    }

    [TestCase]
    public void NextFloat_WhenRangeIsEmptyOrInvalid_Throws()
    {
        // Arrange
        var rng = new SeededRng(42);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => rng.NextFloat(1f, 1f));
        Should.Throw<ArgumentOutOfRangeException>(() => rng.NextFloat(2f, 1f));
        Should.Throw<ArgumentOutOfRangeException>(() => rng.NextFloat(float.NaN, 1f));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            rng.NextFloat(float.MinValue, float.MaxValue)
        );
    }

    [TestCase]
    public void Shuffle_ProducesAPermutation()
    {
        // Arrange
        var rng = new SeededRng(42);
        var list = Enumerable.Range(0, 50).ToList();

        // Act
        rng.Shuffle(list);

        // Assert
        list.OrderBy(v => v).ShouldBe(Enumerable.Range(0, 50));
        list.ShouldNotBe(Enumerable.Range(0, 50));
    }

    [TestCase]
    public void Shuffle_IsDeterministic()
    {
        // Arrange
        var a = Enumerable.Range(0, 20).ToList();
        var b = Enumerable.Range(0, 20).ToList();

        // Act
        new SeededRng(7).Shuffle(a);
        new SeededRng(7).Shuffle(b);

        // Assert
        a.ShouldBe(b);
    }

    [TestCase]
    public void Shuffle_ProducesEveryPermutationEquallyOften()
    {
        // Arrange
        var rng = new SeededRng(42);
        const int samples = 60_000;
        var counts = new Dictionary<string, int>();

        // Act
        for (var i = 0; i < samples; i++)
        {
            var list = new[] { 'a', 'b', 'c' };
            rng.Shuffle(list);
            var key = new string(list);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        // Assert
        counts.Count.ShouldBe(6);
        AssertUniform([.. counts.Values], samples);
    }

    private static void AssertUniform(int[] counts, int samples)
    {
        var expected = samples / (double)counts.Length;
        foreach (var count in counts)
        {
            // ±5% of the expected count is ~10 standard deviations at these sample sizes,
            // yet far tighter than the skew a biased reduction of a small range would show.
            count.ShouldBeInRange((int)(expected * 0.95), (int)(expected * 1.05));
        }
    }
}
