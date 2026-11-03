using System.Linq;
using GdUnit4;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Resources;

[TestSuite]
[RequireGodotRuntime]
public class CitySizeDistributionTests
{
    private static CitySizeDistribution MakeDistribution(params (string Id, float Weight)[] entries)
    {
        var distribution = new CitySizeDistribution();
        foreach (var (id, weight) in entries)
        {
            distribution.Entries.Add(new WeightedSizeEntry { SizeId = id, Weight = weight });
        }

        return distribution;
    }

    [TestCase]
    public void Sample_WhenEmpty_ReturnsNull()
    {
        // Act & Assert
        MakeDistribution().Sample(new SeededRng(42)).ShouldBeNull();
    }

    [TestCase]
    public void Sample_WhenNoWeightIsPositive_ReturnsFirstEntry()
    {
        // Arrange
        var distribution = MakeDistribution(("village", 0f), ("town", -1f));

        // Act & Assert
        distribution.Sample(new SeededRng(42)).ShouldBe("village");
    }

    [TestCase]
    public void Sample_NeverPicksNegativeWeights()
    {
        // Arrange
        var distribution = MakeDistribution(("ruin", -5f), ("village", 1f));
        var rng = new SeededRng(42);

        // Act
        var samples = Enumerable.Range(0, 200).Select(_ => distribution.Sample(rng));

        // Assert
        samples.ShouldAllBe(id => id == "village");
    }

    [TestCase]
    public void Sample_SelectsInProportionToWeight()
    {
        // Arrange
        var distribution = MakeDistribution(("village", 3f), ("city", 1f));
        var rng = new SeededRng(42);
        const int samples = 20_000;

        // Act
        var cities = Enumerable.Range(0, samples).Count(_ => distribution.Sample(rng) == "city");

        // Assert
        (cities / (double)samples).ShouldBe(0.25, 0.015);
    }
}
