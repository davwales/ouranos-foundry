using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.LootTables;
using Shouldly;
using Xunit;

namespace Ouranos.Foundry.Tests.Features.LootTables;

public class LootTableDataTests
{
    private static ItemLootEntry MakeItemEntry(string id, float weight = 1f, Resource? item = null)
    {
        return new ItemLootEntry
        {
            Id = id,
            Weight = weight,
            Item = item,
        };
    }

    private static LootTableData MakeTable(
        string id = "test",
        int minRolls = 1,
        int maxRolls = 1,
        bool allowDuplicates = true,
        List<LootTableEntry>? entries = null
    )
    {
        var table = new LootTableData
        {
            Id = id,
            MinRolls = minRolls,
            MaxRolls = maxRolls,
            AllowDuplicates = allowDuplicates,
        };
        if (entries is not null)
        {
            foreach (var entry in entries)
            {
                table.Entries.Add(entry);
            }
        }
        return table;
    }

    [Fact]
    public void Roll_WhenEntriesAreEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var table = MakeTable();

        // Act
        var results = table.Roll(new SeededRng(42));

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void Roll_WhenSingleItemEntry_ShouldReturnOneResult()
    {
        // Arrange
        var item = new Resource();
        var entry = MakeItemEntry("sword", 1f, item);
        var table = MakeTable(entries: [entry]);

        // Act
        var results = table.Roll(new SeededRng(42));

        // Assert
        results.Count.ShouldBe(1);
        results[0].Item.ShouldBe(item);
    }

    [Fact]
    public void Roll_WhenMultipleRollsConfigured_ShouldReturnCorrectCount()
    {
        // Arrange
        var entry1 = MakeItemEntry("a", 1f, new Resource());
        var entry2 = MakeItemEntry("b", 1f, new Resource());
        var table = MakeTable(
            minRolls: 3,
            maxRolls: 3,
            allowDuplicates: true,
            entries: [entry1, entry2]
        );

        // Act
        var results = table.Roll(new SeededRng(42));

        // Assert
        results.Count.ShouldBe(3);
    }

    [Fact]
    public void Roll_WhenRollsOverrideProvided_ShouldOverrideConfiguredCount()
    {
        // Arrange
        var entry = MakeItemEntry("a", 1f, new Resource());
        var table = MakeTable(minRolls: 1, maxRolls: 1, allowDuplicates: true, entries: [entry]);

        // Act
        var results = table.Roll(new SeededRng(42), rollsOverride: 5);

        // Assert
        results.Count.ShouldBe(5);
    }

    [Fact]
    public void Roll_WhenDuplicatesNotAllowed_ShouldNotRepeatEntries()
    {
        // Arrange
        var entryA = MakeItemEntry("a", 1f, new Resource());
        var entryB = MakeItemEntry("b", 1f, new Resource());
        var entryC = MakeItemEntry("c", 1f, new Resource());
        var table = MakeTable(
            minRolls: 10,
            maxRolls: 10,
            allowDuplicates: false,
            entries: [entryA, entryB, entryC]
        );

        // Act
        var results = table.Roll(new SeededRng(42));

        // Assert
        results.Count.ShouldBeLessThanOrEqualTo(3);

        var ids = new HashSet<Resource?>();
        foreach (var result in results)
        {
            ids.Add(result.Item).ShouldBeTrue($"Duplicate item: {result.Item}");
        }
    }

    [Fact]
    public void Roll_WhenSubTableEntrySelected_ShouldResolveRecursively()
    {
        // Arrange
        var subItemResource = new Resource();
        var subItem = MakeItemEntry("sub_item", 1f, subItemResource);
        var subTable = MakeTable(id: "sub", entries: [subItem]);
        var subEntry = new SubTableLootEntry
        {
            Id = "sub_ref",
            Weight = 1f,
            SubTable = subTable,
        };
        var parentTable = MakeTable(id: "parent", entries: [subEntry]);

        // Act
        var results = parentTable.Roll(new SeededRng(42));

        // Assert
        results.Count.ShouldBe(1);
        results[0].Item.ShouldBe(subItemResource);
    }

    [Fact]
    public void Roll_WhenSubTableReferencesItself_ShouldReturnEmpty()
    {
        // Arrange
        var circularEntry = new SubTableLootEntry { Id = "circular", Weight = 1f };
        var table = MakeTable(id: "circular_table", entries: [circularEntry]);
        circularEntry.SubTable = table;

        // Act
        var results = table.Roll(new SeededRng(42));

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void Roll_WhenMinRollsExceedsMaxRolls_ShouldSwapAndRollCorrectCount()
    {
        // Arrange
        var item = new Resource();
        var entry = MakeItemEntry("a", 1f, item);
        var table = MakeTable(minRolls: 5, maxRolls: 1, entries: [entry]);

        // Act
        var results = table.Roll(new SeededRng(42));

        // Assert
        results.Count.ShouldBe(5);
        foreach (var r in results)
        {
            r.Item.ShouldBe(item);
        }
    }

    [Fact]
    public void Roll_WhenSubTableHasRollOverrides_ShouldUseOverrideValues()
    {
        // Arrange
        var subItemResource = new Resource();
        var subItem = MakeItemEntry("sub_item", 1f, subItemResource);
        var subTable = MakeTable(id: "child", minRolls: 1, maxRolls: 1, entries: [subItem]);
        var subEntry = new SubTableLootEntry
        {
            Id = "sub_ref",
            Weight = 1f,
            SubTable = subTable,
            MinRollsOverride = 3,
            MaxRollsOverride = 3,
        };
        var parentTable = MakeTable(id: "parent", entries: [subEntry]);

        // Act
        var results = parentTable.Roll(new SeededRng(42));

        // Assert
        results.Count.ShouldBe(3);
        foreach (var r in results)
        {
            r.Item.ShouldBe(subItemResource);
        }
    }

    [Fact]
    public void Roll_WhenEntriesExhaustedWithNoDuplicates_ShouldReturnPartialResults()
    {
        // Arrange
        var entryA = MakeItemEntry("a", 1f, new Resource());
        var entryB = MakeItemEntry("b", 1f, new Resource());
        var table = MakeTable(allowDuplicates: false, entries: [entryA, entryB]);

        // Act
        var results = table.Roll(new SeededRng(42), rollsOverride: 5);

        // Assert
        results.Count.ShouldBe(2);
        var ids = new HashSet<Resource?>();
        foreach (var r in results)
        {
            ids.Add(r.Item).ShouldBeTrue($"Duplicate item: {r.Item}");
        }
    }

    [Fact]
    public void Roll_WhenEmptyDropEntrySelected_ShouldReturnEmptyResult()
    {
        // Arrange
        var emptyEntry = new EmptyLootEntry { Id = "nothing", Weight = 1f };
        var table = MakeTable(entries: [emptyEntry]);

        // Act
        var results = table.Roll(new SeededRng(42));

        // Assert
        results.Count.ShouldBe(1);
        results[0].Item.ShouldBeNull();
        results[0].Quantity.ShouldBe(0);
    }

    [Fact]
    public void Roll_WhenSameSeedUsed_ShouldReturnIdenticalResults()
    {
        // Arrange
        var entry1 = MakeItemEntry("a", 1f, new Resource());
        var entry2 = MakeItemEntry("b", 1f, new Resource());
        var entry3 = MakeItemEntry("c", 1f, new Resource());
        var table = MakeTable(
            minRolls: 5,
            maxRolls: 5,
            allowDuplicates: true,
            entries: [entry1, entry2, entry3]
        );

        // Act
        var results1 = table.Roll(new SeededRng(12345));
        var results2 = table.Roll(new SeededRng(12345));

        // Assert
        results1.Count.ShouldBe(results2.Count);
        for (var i = 0; i < results1.Count; i++)
        {
            results1[i].Item.ShouldBe(results2[i].Item);
            results1[i].Quantity.ShouldBe(results2[i].Quantity);
        }
    }

    [Fact]
    public void GetProbabilities_WhenEqualWeights_ShouldReturnEqualProbabilities()
    {
        // Arrange
        var entryA = MakeItemEntry("a", 1f);
        var entryB = MakeItemEntry("b", 1f);
        var table = MakeTable(entries: [entryA, entryB]);

        // Act
        var probs = table.GetProbabilities();

        // Assert
        probs.Count.ShouldBe(2);
        probs[0].Probability.ShouldBe(0.5f, tolerance: 0.001f);
        probs[1].Probability.ShouldBe(0.5f, tolerance: 0.001f);
    }

    [Fact]
    public void GetProbabilities_WhenEntryHasZeroWeight_ShouldReturnZeroProbability()
    {
        // Arrange
        var entryA = MakeItemEntry("a", 1f);
        var entryB = MakeItemEntry("b", 0f);
        var table = MakeTable(entries: [entryA, entryB]);

        // Act
        var probs = table.GetProbabilities();

        // Assert
        var probA = probs.FirstOrDefault(p => p.EntryId == "a");
        var probB = probs.FirstOrDefault(p => p.EntryId == "b");

        probA.ShouldNotBeNull();
        probB.ShouldNotBeNull();
        probA.Probability.ShouldBe(1.0f, tolerance: 0.001f);
        probB.Probability.ShouldBe(0.0f, tolerance: 0.001f);
    }
}
