using System.Collections.Generic;
using Ouranos.Foundry.Core.LootTables;
using Ouranos.Foundry.Core.Types;
using Shouldly;
using Xunit;

namespace Ouranos.Foundry.Tests.Core.LootTables;

public class WeightedSelectionTests
{
    [Fact]
    public void Select_WhenSingleEntry_ShouldReturnIt()
    {
        // Arrange
        var entries = new List<LootTableEntry>
        {
            new ItemLootEntry { Weight = 1f, Id = "a" },
        };

        // Act
        var result = WeightedSelection.Select(entries, new SeededRng(42), new RollContext());

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe("a");
    }

    [Fact]
    public void Select_WhenAllWeightsAreZero_ShouldReturnNull()
    {
        // Arrange
        var entries = new List<LootTableEntry>
        {
            new ItemLootEntry { Weight = 0f },
            new ItemLootEntry { Weight = 0f },
        };

        // Act
        var result = WeightedSelection.Select(entries, new SeededRng(42), new RollContext());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Select_WhenUsingSameSeed_ShouldReturnSameEntry()
    {
        // Arrange
        var entries = new List<LootTableEntry>
        {
            new ItemLootEntry { Weight = 1f, Id = "a" },
            new ItemLootEntry { Weight = 1f, Id = "b" },
        };
        var rng1 = new SeededRng(42);
        var rng2 = new SeededRng(42);

        // Act
        var result1 = WeightedSelection.Select(entries, rng1, new RollContext());
        var result2 = WeightedSelection.Select(entries, rng2, new RollContext());

        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();
        result1.Id.ShouldBe(result2.Id);
    }

    [Fact]
    public void Select_WhenEntryHasNegativeWeight_ShouldTreatAsZero()
    {
        // Arrange
        var entries = new List<LootTableEntry>
        {
            new ItemLootEntry { Weight = -1f, Id = "negative" },
            new ItemLootEntry { Weight = 1f, Id = "positive" },
        };

        // Act & Assert
        foreach (var seed in new ulong[] { 1, 42, 99, 123, 9999 })
        {
            var result = WeightedSelection.Select(entries, new SeededRng(seed), new RollContext());
            result.ShouldNotBeNull();
            result.Id.ShouldBe("positive");
        }
    }
}
