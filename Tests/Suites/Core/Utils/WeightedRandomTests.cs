using System.Collections.Generic;
using GdUnit4;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Core.Utils;
using Shouldly;

namespace Ouranos.Foundry.Tests.Core.Utils;

[TestSuite]
public class WeightedRandomTests
{
    [TestCase]
    public void Pick_WhenSingleEntry_ReturnsIt()
    {
        // Arrange
        string[] items = ["a"];

        // Act
        var result = WeightedRandom.Pick(items, _ => 1f, new SeededRng(42));

        // Assert
        result.ShouldBe("a");
    }

    [TestCase]
    public void Pick_WhenEmpty_ReturnsDefault()
    {
        // Act
        var result = WeightedRandom.Pick(new List<string>(), _ => 1f, new SeededRng(42));

        // Assert
        result.ShouldBeNull();
    }

    [TestCase]
    public void PickIndex_WhenNoWeightIsPositive_ReturnsMinusOne()
    {
        // Act
        var result = WeightedRandom.PickIndex(
            [0f, -1f, float.NaN, float.PositiveInfinity],
            new SeededRng(42)
        );

        // Assert
        result.ShouldBe(-1);
    }

    [TestCase]
    public void PickIndex_NeverPicksInvalidWeights()
    {
        // Arrange
        var rng = new SeededRng(42);
        float[] weights = [-1f, 1f, float.NaN, 0f, float.PositiveInfinity, 1f];

        // Act & Assert
        for (var i = 0; i < 1000; i++)
        {
            WeightedRandom.PickIndex(weights, rng).ShouldBeOneOf(1, 5);
        }
    }

    [TestCase]
    public void PickIndex_SelectsInProportionToWeight()
    {
        // Arrange
        var rng = new SeededRng(42);
        float[] weights = [1f, 2f, 7f];
        const int samples = 100_000;
        var counts = new int[weights.Length];

        // Act
        for (var i = 0; i < samples; i++)
        {
            counts[WeightedRandom.PickIndex(weights, rng)]++;
        }

        // Assert
        (counts[0] / (double)samples).ShouldBe(0.1, 0.01);
        (counts[1] / (double)samples).ShouldBe(0.2, 0.01);
        (counts[2] / (double)samples).ShouldBe(0.7, 0.01);
    }

    [TestCase]
    public void PickIndex_WhenUsingSameSeed_ReturnsSameSequence()
    {
        // Arrange
        float[] weights = [1f, 1f, 1f, 1f];
        var a = new SeededRng(42);
        var b = new SeededRng(42);

        // Act & Assert
        for (var i = 0; i < 100; i++)
        {
            WeightedRandom.PickIndex(weights, b).ShouldBe(WeightedRandom.PickIndex(weights, a));
        }
    }

    [TestCase]
    public void PickIndex_ConsumesExactlyOneDraw()
    {
        // Arrange
        var picked = new SeededRng(42);
        var reference = new SeededRng(42);

        // Act
        WeightedRandom.PickIndex([1f, 2f], picked);
        reference.NextDouble();

        // Assert
        picked.Next().ShouldBe(reference.Next());
    }
}
