using System;
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
}
