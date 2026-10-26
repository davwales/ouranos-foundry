using System;
using System.Collections.Generic;
using System.IO;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.Persistence.Nodes;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Persistence.Nodes;

[TestSuite]
[RequireGodotRuntime]
public class SaveManagerTests
{
    private const string Slot = "slot1";

    // Each test saves into a directory of its own, in the tree so groups and paths work.
    private static (Node root, T manager, string directory) CreateManager<T>(int version = 1)
        where T : SaveManager, new()
    {
        var directory = $"user://save_manager_tests/{Guid.NewGuid():N}";
        var root = new Node { Name = $"Root{Guid.NewGuid():N}" };
        var manager = new T()
            .With(SaveManager.PropertyName.SaveDirectory, directory)
            .With(SaveManager.PropertyName.Version, version);
        root.AddChild(manager);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(root);
        return (root, manager, directory);
    }

    private static (Node root, SaveManager manager, string directory) CreateManager(int version = 1)
    {
        return CreateManager<SaveManager>(version);
    }

    private static SampleSaveable AddSaveable(Node root, string name, string key = "")
    {
        var saveable = new SampleSaveable { Name = name, SaveKey = key };
        saveable.AddToGroup(SaveManager.DefaultSaveGroup);
        root.AddChild(saveable);
        return saveable;
    }

    private static void Cleanup(Node root, string directory)
    {
        root.Free();
        var path = ProjectSettings.GlobalizePath(directory);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [TestCase]
    public void Load_AfterSave_RestoresEveryValueWithItsType()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();
        var saveable = AddSaveable(root, "Player", "player");
        saveable.Count = 7;
        saveable.Ratio = 2f;
        saveable.Label = "hero";
        saveable.Cell = new Vector2I(-3, 4);
        saveable.Values = [1, 2, 3];
        manager.Save(Slot).ShouldBeTrue();
        saveable.Count = 0;
        saveable.Ratio = 0f;
        saveable.Label = "";
        saveable.Cell = Vector2I.Zero;
        saveable.Values = [];

        // Act
        var loaded = manager.Load(Slot);

        // Assert
        loaded.ShouldBeTrue();
        saveable.Count.ShouldBe(7);
        saveable.Ratio.ShouldBe(2f);
        saveable.Label.ShouldBe("hero");
        saveable.Cell.ShouldBe(new Vector2I(-3, 4));
        saveable.Values.ShouldBe([1, 2, 3]);

        Cleanup(root, directory);
    }

    [TestCase]
    public void Save_WithAnEmptySaveKey_KeysTheNodeByItsPath()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();
        var saveable = AddSaveable(root, "Unkeyed");
        saveable.Count = 5;
        manager.Save(Slot);
        saveable.Free();
        var other = new SampleSaveable { Name = "Unkeyed" };
        root.AddChild(other);

        // Act
        var restored = manager.Restore(other);

        // Assert
        restored.ShouldBeTrue();
        other.Count.ShouldBe(5);

        Cleanup(root, directory);
    }

    [TestCase]
    public void Load_WithARegisteredSaveable_RestoresIt()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();
        var settings = new Settings { Volume = 0.25 };
        manager.Register(settings);
        manager.Save(Slot);
        settings.Volume = 1;

        // Act
        manager.Load(Slot);

        // Assert
        settings.Volume.ShouldBe(0.25);

        manager.Unregister(settings);
        Cleanup(root, directory);
    }

    [TestCase]
    public void Save_WhenASaveableIsAbsent_KeepsItsEntryFromTheLastLoad()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();
        var level1 = AddSaveable(root, "Level1", "level1");
        var level2 = AddSaveable(root, "Level2", "level2");
        level1.Count = 1;
        level2.Count = 2;
        manager.Save(Slot);
        level2.Free();
        manager.Load(Slot);
        level1.Count = 10;

        // Act
        manager.Save(Slot);

        // Assert
        manager.ClearData();
        manager.Load(Slot);
        var returning = AddSaveable(root, "Level2Again", "level2");
        manager.Restore(returning).ShouldBeTrue();
        returning.Count.ShouldBe(2);
        level1.Count.ShouldBe(10);

        Cleanup(root, directory);
    }

    [TestCase]
    public void Load_WhenASaveableHasNoEntry_LeavesItAlone()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();
        manager.Save(Slot);
        var late = AddSaveable(root, "Late", "late");
        late.Count = 3;

        // Act
        var loaded = manager.Load(Slot);

        // Assert
        loaded.ShouldBeTrue();
        late.Count.ShouldBe(3);
        late.LoadCount.ShouldBe(0);

        Cleanup(root, directory);
    }

    [TestCase]
    public void Save_WhenASaveableThrows_KeepsThePreviousSaveAndReportsFailure()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();
        var saveable = AddSaveable(root, "Player", "player");
        saveable.Count = 1;
        manager.Save(Slot);
        saveable.Count = 2;
        saveable.ThrowOnSave = true;
        var failures = new List<string>();
        void OnFailed(string slot, string error)
        {
            failures.Add(error);
        }

        manager.SaveFailed += OnFailed;

        // Act
        var saved = manager.Save(Slot);

        // Assert
        saved.ShouldBeFalse();
        failures.ShouldBe(["Save exploded."]);
        saveable.ThrowOnSave = false;
        manager.Load(Slot);
        saveable.Count.ShouldBe(1);
        Directory
            .GetFiles(ProjectSettings.GlobalizePath(directory))
            .ShouldBe([ProjectSettings.GlobalizePath(manager.GetSlotPath(Slot))]);

        manager.SaveFailed -= OnFailed;
        Cleanup(root, directory);
    }

    [TestCase]
    public void Load_WhenOneSaveableThrows_LoadsTheOthersAndReportsFailure()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();
        var broken = AddSaveable(root, "Broken", "broken");
        var healthy = AddSaveable(root, "Healthy", "healthy");
        healthy.Count = 4;
        manager.Save(Slot);
        healthy.Count = 0;
        broken.ThrowOnLoad = true;
        var failures = new List<string>();
        void OnFailed(string slot, string error)
        {
            failures.Add(error);
        }

        manager.LoadFailed += OnFailed;

        // Act
        var loaded = manager.Load(Slot);

        // Assert
        loaded.ShouldBeFalse();
        healthy.Count.ShouldBe(4);
        failures.ShouldBe(["broken: Load exploded."]);

        manager.LoadFailed -= OnFailed;
        Cleanup(root, directory);
    }

    [TestCase]
    public void Load_FromAnOlderVersion_MigratesTheDataFirst()
    {
        // Arrange
        var (oldRoot, oldManager, directory) = CreateManager();
        AddSaveable(oldRoot, "Player", "player").Label = "hero";
        oldManager.Save(Slot);
        oldRoot.Free();
        var (root, manager, _) = CreateManager<MigratingSaveManager>(version: 2);
        manager.With(SaveManager.PropertyName.SaveDirectory, directory);
        var saveable = AddSaveable(root, "Player", "player");

        // Act
        var loaded = manager.Load(Slot);

        // Assert
        loaded.ShouldBeTrue();
        manager.MigratedFrom.ShouldBe(1);
        saveable.Label.ShouldBe("migrated hero");

        Cleanup(root, directory);
    }

    [TestCase]
    public void Load_FromTheSameVersion_DoesNotMigrate()
    {
        // Arrange
        var (root, manager, directory) = CreateManager<MigratingSaveManager>(version: 2);
        AddSaveable(root, "Player", "player").Label = "hero";
        manager.Save(Slot);

        // Act
        manager.Load(Slot);

        // Assert
        manager.MigratedFrom.ShouldBeNull();

        Cleanup(root, directory);
    }

    [TestCase]
    public void Load_FromANewerVersion_FailsWithoutLoading()
    {
        // Arrange
        var (newRoot, newManager, directory) = CreateManager(version: 3);
        AddSaveable(newRoot, "Player", "player").Count = 9;
        newManager.Save(Slot);
        newRoot.Free();
        var (root, manager, _) = CreateManager(version: 2);
        manager.With(SaveManager.PropertyName.SaveDirectory, directory);
        var saveable = AddSaveable(root, "Player", "player");

        // Act
        var loaded = manager.Load(Slot);

        // Assert
        loaded.ShouldBeFalse();
        saveable.LoadCount.ShouldBe(0);

        Cleanup(root, directory);
    }

    [TestCase]
    public void Load_WhenTheFileIsCorrupt_FailsWithoutLoading()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();
        var saveable = AddSaveable(root, "Player", "player");
        manager.Save(Slot);
        File.WriteAllText(ProjectSettings.GlobalizePath(manager.GetSlotPath(Slot)), "{ nope");

        // Act
        var loaded = manager.Load(Slot);

        // Assert
        loaded.ShouldBeFalse();
        saveable.LoadCount.ShouldBe(0);

        Cleanup(root, directory);
    }

    [TestCase]
    public void Load_WhenTheSlotIsEmpty_ReportsFailure()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();
        var failures = new List<string>();
        void OnFailed(string slot, string error)
        {
            failures.Add(slot);
        }

        manager.LoadFailed += OnFailed;

        // Act
        var loaded = manager.Load("missing");

        // Assert
        loaded.ShouldBeFalse();
        failures.ShouldBe(["missing"]);

        manager.LoadFailed -= OnFailed;
        Cleanup(root, directory);
    }

    [TestCase]
    public void Save_WithAnInvalidSlotName_Fails()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();

        // Act
        var saved = manager.Save("../escape");

        // Assert
        saved.ShouldBeFalse();
        Directory.Exists(ProjectSettings.GlobalizePath(directory)).ShouldBeFalse();

        Cleanup(root, directory);
    }

    [TestCase]
    public void GetSlotInfo_AfterSave_ReturnsTheHeaderAndMetadata()
    {
        // Arrange
        var (root, manager, directory) = CreateManager(version: 4);
        var before = DateTime.UtcNow.AddSeconds(-1);
        manager.Save(Slot, new() { ["location"] = "Harbor" });

        // Act
        var info = manager.GetSlotInfo(Slot);

        // Assert
        info.ShouldNotBeNull();
        info.Slot.ShouldBe(Slot);
        info.Version.ShouldBe(4);
        info.SavedAt.ShouldBeGreaterThan(before);
        info.Metadata["location"].AsString().ShouldBe("Harbor");
        manager.GetSlotInfo("missing").ShouldBeNull();

        Cleanup(root, directory);
    }

    [TestCase]
    public void Slots_AfterSavingAndDeleting_AreListedAndRemoved()
    {
        // Arrange
        var (root, manager, directory) = CreateManager();
        manager.GetSlots().ShouldBeEmpty();
        manager.Save("b");
        manager.Save("a");

        // Act
        var listed = manager.GetSlots();
        var deleted = manager.DeleteSave("a");

        // Assert
        listed.ShouldBe(["a", "b"]);
        deleted.ShouldBeTrue();
        manager.HasSave("a").ShouldBeFalse();
        manager.HasSave("b").ShouldBeTrue();
        manager.DeleteSave("a").ShouldBeFalse();

        Cleanup(root, directory);
    }

    private sealed class Settings : ISaveable
    {
        public double Volume { get; set; }

        public string SaveKey => "settings";

        public Godot.Collections.Dictionary<string, Variant> Save()
        {
            return new() { ["volume"] = Volume };
        }

        public void Load(Godot.Collections.Dictionary<string, Variant> data)
        {
            Volume = data["volume"].AsDouble();
        }
    }
}
