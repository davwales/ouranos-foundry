using Ouranos.Foundry.Core.Types;
using Xunit;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Types;

public class SeededRngTests
{
    [Fact]
    public void SameSeedAndSalt_ProducesSameSequence()
    {
        var a = new SeededRng(42, 100);
        var b = new SeededRng(42, 100);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(a.Next(), b.Next());
        }
    }

    [Fact]
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
        Assert.True(matchCount < 10, $"Expected <10 matches, got {matchCount}");
    }

    [Fact]
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

        Assert.True(matchCount < 10, $"Expected <10 matches, got {matchCount}");
    }

    [Fact]
    public void Next_WithMaxValue_ReturnsValuesInRange()
    {
        var rng = new SeededRng(42);

        for (var i = 0; i < 1000; i++)
        {
            var value = rng.Next(10);
            Assert.InRange(value, 0, 9);
        }
    }

    [Fact]
    public void Next_WithMinMax_ReturnsValuesInRange()
    {
        var rng = new SeededRng(42);

        for (var i = 0; i < 1000; i++)
        {
            var value = rng.Next(5, 15);
            Assert.InRange(value, 5, 14);
        }
    }

    [Fact]
    public void NextDouble_ReturnsValuesBetweenZeroAndOne()
    {
        var rng = new SeededRng(42);

        for (var i = 0; i < 1000; i++)
        {
            var value = rng.NextDouble();
            Assert.InRange(value, 0.0, 1.0);
        }
    }

    [Fact]
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

        Assert.True(matchCount < 10, $"Expected <10 matches, got {matchCount}");
    }

    [Fact]
    public void Derive_IsDeterministic()
    {
        var parent1 = new SeededRng(42);
        var parent2 = new SeededRng(42);

        var child1 = parent1.Derive();
        var child2 = parent2.Derive();

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(child1.Next(), child2.Next());
        }
    }

    [Fact]
    public void NextElement_FromArray_ReturnsElementsFromArray()
    {
        var rng = new SeededRng(42);
        var array = new[] { "a", "b", "c" };

        for (var i = 0; i < 100; i++)
        {
            var element = rng.NextElement(array);
            Assert.Contains(element, array);
        }
    }
}
