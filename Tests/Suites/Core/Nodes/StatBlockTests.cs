using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Core.Nodes;
using Ouranos.Foundry.Core.Resources;
using Ouranos.Foundry.Core.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Core.Nodes;

[TestSuite]
[RequireGodotRuntime]
public class StatBlockTests
{
    private static StatDefinition MakeStat(
        string id = "strength",
        float baseValue = 10f,
        float min = float.NegativeInfinity,
        float max = float.PositiveInfinity
    )
    {
        return new StatDefinition
        {
            Id = id,
            BaseValue = baseValue,
            MinValue = min,
            MaxValue = max,
        };
    }

    private static StatModifier MakeModifier(
        StatModifierOperation operation,
        float value,
        string statId = "strength",
        int priority = 0
    )
    {
        return new StatModifier
        {
            StatId = statId,
            Operation = operation,
            Value = value,
            Priority = priority,
        };
    }

    private static (StatBlock block, List<string> log) CreateBlock(params StatDefinition[] stats)
    {
        var log = new List<string>();
        var block = new StatBlock().With(
            StatBlock.PropertyName.Stats,
            new Godot.Collections.Array<StatDefinition>(stats.Length > 0 ? stats : [MakeStat()])
        );
        block.StatChanged += (id, oldValue, newValue) => log.Add($"{id}:{oldValue}->{newValue}");
        return (block, log);
    }

    [TestCase]
    public void GetValue_WithoutModifiers_ReturnsBaseValue()
    {
        // Arrange
        var (block, _) = CreateBlock(MakeStat(baseValue: 12f));

        // Act
        var value = block.GetValue("strength");

        // Assert
        value.ShouldBe(12f);
        block.GetBaseValue("strength").ShouldBe(12f);

        block.Free();
    }

    [TestCase]
    public void GetValue_WithEveryOperation_AddsThenPercentsThenMultiplies()
    {
        // Arrange
        var (block, _) = CreateBlock(MakeStat(baseValue: 10f));

        // Act: added out of order on purpose; the order of operations is fixed.
        block.AddModifier(MakeModifier(StatModifierOperation.Multiply, 2f));
        block.AddModifier(MakeModifier(StatModifierOperation.PercentAdd, 0.25f));
        block.AddModifier(MakeModifier(StatModifierOperation.Add, 5f));
        block.AddModifier(MakeModifier(StatModifierOperation.PercentAdd, 0.25f));
        block.AddModifier(MakeModifier(StatModifierOperation.Add, 5f));
        block.AddModifier(MakeModifier(StatModifierOperation.Multiply, 1.5f));

        // Assert: (10 + 5 + 5) × (1 + 0.25 + 0.25) × 2 × 1.5
        block.GetValue("strength").ShouldBe(90f);

        block.Free();
    }

    [TestCase]
    public void GetValue_WithOverrides_UsesHighestPriorityAndIgnoresOthers()
    {
        // Arrange
        var (block, _) = CreateBlock(MakeStat(baseValue: 10f));
        block.AddModifier(MakeModifier(StatModifierOperation.Add, 100f));

        // Act
        block.AddModifier(MakeModifier(StatModifierOperation.Override, 3f, priority: 5));
        block.AddModifier(MakeModifier(StatModifierOperation.Override, 1f, priority: 1));

        // Assert
        block.GetValue("strength").ShouldBe(3f);

        block.Free();
    }

    [TestCase]
    public void GetValue_WithTiedOverrides_UsesMostRecentlyAdded()
    {
        // Arrange
        var (block, _) = CreateBlock();

        // Act
        block.AddModifier(MakeModifier(StatModifierOperation.Override, 3f));
        block.AddModifier(MakeModifier(StatModifierOperation.Override, 7f));

        // Assert
        block.GetValue("strength").ShouldBe(7f);

        block.Free();
    }

    [TestCase]
    public void GetValue_OutsideLimits_IsClampedButBaseValueIsNot()
    {
        // Arrange
        var (block, _) = CreateBlock(MakeStat(baseValue: 50f, min: 0f, max: 100f));

        // Act
        block.AddModifier(MakeModifier(StatModifierOperation.Multiply, 3f));
        var high = block.GetValue("strength");
        block.SetBaseValue("strength", -20f);

        // Assert
        high.ShouldBe(100f);
        block.GetValue("strength").ShouldBe(0f);
        block.GetBaseValue("strength").ShouldBe(-20f);

        block.Free();
    }

    [TestCase]
    public void GetValue_WhenBaseValueOutsideLimits_StartsClamped()
    {
        // Arrange
        var (block, _) = CreateBlock(MakeStat(baseValue: 5f, min: 0f, max: 1f));

        // Act
        var value = block.GetValue("strength");

        // Assert
        value.ShouldBe(1f);

        block.Free();
    }

    [TestCase]
    public void RemoveModifiersFrom_RemovesOnlyThatSourcesModifiersAcrossStats()
    {
        // Arrange
        var (block, _) = CreateBlock(MakeStat("strength", 10f), MakeStat("speed", 5f));
        var sword = new Resource();
        var boots = new Resource();
        block.AddModifier(MakeModifier(StatModifierOperation.Add, 5f), sword);
        block.AddModifier(MakeModifier(StatModifierOperation.Add, 1f, "speed"), sword);
        block.AddModifier(MakeModifier(StatModifierOperation.Add, 2f, "speed"), boots);

        // Act
        var removed = block.RemoveModifiersFrom(sword);

        // Assert
        removed.ShouldBe(2);
        block.GetValue("strength").ShouldBe(10f);
        block.GetValue("speed").ShouldBe(7f);
        block.RemoveModifiersFrom(sword).ShouldBe(0);

        block.Free();
    }

    [TestCase]
    public void RemoveModifiersFrom_WhenModifierIsShared_KeepsOtherSourcesApplications()
    {
        // Arrange
        var (block, _) = CreateBlock();
        var ringBonus = MakeModifier(StatModifierOperation.Add, 3f);
        var leftRing = new Resource();
        var rightRing = new Resource();
        block.AddModifier(ringBonus, leftRing);
        block.AddModifier(ringBonus, rightRing);

        // Act
        block.RemoveModifiersFrom(leftRing);

        // Assert
        block.GetValue("strength").ShouldBe(13f);

        block.Free();
    }

    [TestCase]
    public void RemoveModifier_RemovesEveryApplicationOfThatModifier()
    {
        // Arrange
        var (block, _) = CreateBlock();
        var haste = MakeModifier(StatModifierOperation.Add, 3f);
        var other = MakeModifier(StatModifierOperation.Add, 1f);
        block.AddModifier(haste);
        block.AddModifier(haste);
        block.AddModifier(other);

        // Act
        var removed = block.RemoveModifier(haste);

        // Assert
        removed.ShouldBeTrue();
        block.GetValue("strength").ShouldBe(11f);
        block.GetModifiers("strength").ShouldBe([other]);
        block.RemoveModifier(haste).ShouldBeFalse();

        block.Free();
    }

    [TestCase]
    public void ClearModifiers_RestoresBaseValues()
    {
        // Arrange
        var (block, _) = CreateBlock(MakeStat("strength", 10f), MakeStat("speed", 5f));
        block.AddModifier(MakeModifier(StatModifierOperation.Add, 5f));
        block.AddModifier(MakeModifier(StatModifierOperation.Multiply, 2f, "speed"));

        // Act
        block.ClearModifiers();

        // Assert
        block.GetValue("strength").ShouldBe(10f);
        block.GetValue("speed").ShouldBe(5f);

        block.Free();
    }

    [TestCase]
    public void StatChanged_EmitsOldAndNewFinalValues_OnlyWhenTheValueMoves()
    {
        // Arrange
        var (block, log) = CreateBlock(MakeStat(baseValue: 10f, max: 12f));
        var bonus = MakeModifier(StatModifierOperation.Add, 5f);

        // Act
        block.AddModifier(bonus); // 10 -> 12 (clamped)
        block.AddModifier(MakeModifier(StatModifierOperation.Add, 1f)); // still 12
        block.SetBaseValue("strength", 0f); // 12 -> 6
        block.RemoveModifier(bonus); // 6 -> 1

        // Assert
        log.ShouldBe(["strength:10->12", "strength:12->6", "strength:6->1"]);

        block.Free();
    }

    [TestCase]
    public void AddModifier_WhenStatIsUnknown_ReturnsFalse()
    {
        // Arrange
        var (block, log) = CreateBlock();

        // Act
        var added = block.AddModifier(MakeModifier(StatModifierOperation.Add, 1f, "luck"));

        // Assert
        added.ShouldBeFalse();
        block.HasStat("luck").ShouldBeFalse();
        log.ShouldBeEmpty();

        block.Free();
    }

    [TestCase]
    public void Stats_WhenListedTwice_KeepsTheFirst()
    {
        // Arrange
        var (block, _) = CreateBlock(MakeStat(baseValue: 1f), MakeStat(baseValue: 2f));

        // Act
        var value = block.GetValue("strength");

        // Assert
        value.ShouldBe(1f);

        block.Free();
    }

    [TestCase]
    public void Recalculate_AfterEditingAnAppliedModifier_PicksUpTheChange()
    {
        // Arrange
        var (block, log) = CreateBlock();
        var bonus = MakeModifier(StatModifierOperation.Add, 1f);
        block.AddModifier(bonus);
        bonus.Value = 4f;

        // Act
        block.Recalculate();

        // Assert
        block.GetValue("strength").ShouldBe(14f);
        log.ShouldBe(["strength:10->11", "strength:11->14"]);

        block.Free();
    }

    [TestCase]
    public void Calculate_WhenOverridden_IsUsedAndStillClamped()
    {
        // Arrange
        var block = new SquaringStatBlock().With(
            StatBlock.PropertyName.Stats,
            new Godot.Collections.Array<StatDefinition>([MakeStat(baseValue: 3f, max: 50f)])
        );

        // Act
        var initial = block.GetValue("strength");
        block.SetBaseValue("strength", 9f);

        // Assert
        initial.ShouldBe(9f);
        block.GetValue("strength").ShouldBe(50f);

        block.Free();
    }

    [TestCase]
    public void SaveAndLoad_RoundTripBaseValuesButNotModifiers()
    {
        // Arrange
        var (saved, _) = CreateBlock(MakeStat("strength", 10f), MakeStat("speed", 5f));
        saved.SetBaseValue("strength", 14f);
        saved.AddModifier(MakeModifier(StatModifierOperation.Add, 5f));
        var data = ((ISaveable)saved).Save();
        var (loaded, log) = CreateBlock(MakeStat("strength", 10f), MakeStat("speed", 5f));

        // Act
        ((ISaveable)loaded).Load(data);

        // Assert
        loaded.GetBaseValue("strength").ShouldBe(14f);
        loaded.GetValue("strength").ShouldBe(14f);
        loaded.GetBaseValue("speed").ShouldBe(5f);
        log.ShouldBe(["strength:10->14"]);

        saved.Free();
        loaded.Free();
    }

    [TestCase]
    public void Load_IgnoresStatsTheBlockDoesNotHave()
    {
        // Arrange
        var (block, _) = CreateBlock();
        var data = new Godot.Collections.Dictionary<string, Variant>
        {
            ["base_values"] = new Godot.Collections.Dictionary<string, Variant>
            {
                ["strength"] = 3f,
                ["removed_stat"] = 7f,
            },
        };

        // Act
        ((ISaveable)block).Load(data);

        // Assert
        block.GetBaseValue("strength").ShouldBe(3f);
        block.HasStat("removed_stat").ShouldBeFalse();

        block.Free();
    }

    [TestCase]
    public void Methods_CalledThroughGodot_WorkLikeFromGdscript()
    {
        // Arrange
        var (block, log) = CreateBlock();
        var source = new Resource();

        // Act
        block.Call(
            StatBlock.MethodName.AddModifier,
            MakeModifier(StatModifierOperation.Add, 2f),
            source
        );
        var value = block.Call(StatBlock.MethodName.GetValue, "strength").AsSingle();
        block.Call(StatBlock.MethodName.RemoveModifiersFrom, source);

        // Assert
        value.ShouldBe(12f);
        log.ShouldBe(["strength:10->12", "strength:12->10"]);

        block.Free();
    }

    [TestCase]
    public void StatDefinition_WithUnboundedLimits_SurvivesASaveAndReload()
    {
        // Arrange
        var path = "user://stat_definition_test.tres";
        ResourceSaver.Save(MakeStat(min: 0f), path).ShouldBe(Error.Ok);

        // Act
        var loaded = ResourceLoader.Load<StatDefinition>(
            path,
            cacheMode: ResourceLoader.CacheMode.Ignore
        );

        // Assert
        loaded.MinValue.ShouldBe(0f);
        float.IsPositiveInfinity(loaded.MaxValue).ShouldBeTrue();

        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
    }
}
