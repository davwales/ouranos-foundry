using System.Linq;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.LootTables.Nodes;
using Ouranos.Foundry.Features.LootTables.Resources;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.LootTables;

[TestSuite]
[RequireGodotRuntime]
public class LootTableRollerTests
{
    private static LootTableData MakeTable()
    {
        var table = new LootTableData { MinRolls = 3, MaxRolls = 3 };
        for (var i = 0; i < 10; i++)
        {
            table.Entries.Add(
                new ItemLootEntry
                {
                    Id = $"item{i}",
                    Item = new Resource(),
                    MinQuantity = 1,
                    MaxQuantity = 100,
                }
            );
        }

        return table;
    }

    private static LootTableRoller CreateRoller(LootTableData table, long seed, bool autoSeed)
    {
        return new LootTableRoller()
            .With(LootTableRoller.PropertyName.Table, table)
            .With(LootTableRoller.PropertyName.Seed, seed)
            .With(LootTableRoller.PropertyName.AutoSeed, autoSeed);
    }

    private static string Describe(Godot.Collections.Array<LootResult> results)
    {
        return string.Join(",", results.Select(r => $"{r.Item?.GetInstanceId()}x{r.Quantity}"));
    }

    [TestCase]
    public void Roll_IsCallableThroughGodot()
    {
        // Arrange: Call goes through ClassDB, the same path GDScript uses.
        var roller = CreateRoller(MakeTable(), seed: 1, autoSeed: false);

        // Act
        var results = roller.Call(LootTableRoller.MethodName.Roll).AsGodotArray<LootResult>();

        // Assert
        results.Count.ShouldBe(3);

        roller.Free();
    }

    [TestCase]
    public void Roll_WhenSameSeed_ReturnsSameResults()
    {
        // Arrange
        var table = MakeTable();
        var a = CreateRoller(table, seed: 1234, autoSeed: false);
        var b = CreateRoller(table, seed: 1234, autoSeed: false);

        // Act
        var resultsA = Describe(a.Roll()) + Describe(a.Roll());
        var resultsB = Describe(b.Roll()) + Describe(b.Roll());

        // Assert
        resultsB.ShouldBe(resultsA);
        a.ActiveSeed.ShouldBe(1234);

        a.Free();
        b.Free();
    }

    [TestCase]
    public void Roll_WhenCalledRepeatedly_ContinuesTheStream()
    {
        // Arrange
        var roller = CreateRoller(MakeTable(), seed: 1234, autoSeed: false);

        // Act
        var first = Describe(roller.Roll());
        var second = Describe(roller.Roll());

        // Assert
        second.ShouldNotBe(first);

        roller.Free();
    }

    [TestCase]
    public void Roll_WhenAutoSeed_IgnoresSeedAndReportsActiveSeed()
    {
        // Arrange
        var a = CreateRoller(MakeTable(), seed: 1234, autoSeed: true);
        var b = CreateRoller(MakeTable(), seed: 1234, autoSeed: true);

        // Act
        a.Roll();
        b.Roll();

        // Assert
        a.ActiveSeed.ShouldNotBe(b.ActiveSeed);

        a.Free();
        b.Free();
    }

    [TestCase]
    public void SetSeed_DisablesAutoSeedAndRestartsTheStream()
    {
        // Arrange
        var table = MakeTable();
        var roller = CreateRoller(table, seed: 0, autoSeed: true);
        var reference = CreateRoller(table, seed: 99, autoSeed: false);
        roller.Roll();

        // Act
        roller.SetSeed(99);

        // Assert
        roller.AutoSeed.ShouldBeFalse();
        Describe(roller.Roll()).ShouldBe(Describe(reference.Roll()));

        roller.Free();
        reference.Free();
    }

    [TestCase]
    public void Roll_WhenNoTable_ReturnsEmpty()
    {
        // Arrange
        var roller = new LootTableRoller();

        // Act
        var results = roller.Roll();

        // Assert
        results.ShouldBeEmpty();

        roller.Free();
    }
}
