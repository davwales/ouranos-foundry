using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.Audio.Nodes;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Audio;

[TestSuite]
[RequireGodotRuntime]
public class AudioManagerTests
{
    private const string TestBus = "AudioManagerTestBus";

    private static AudioManager CreateManager(bool createMissingBuses = false)
    {
        var manager = new AudioManager().With(
            AudioManager.PropertyName.CreateMissingBuses,
            createMissingBuses
        );
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(manager);
        return manager;
    }

    private static void AddBus(string name)
    {
        AudioServer.AddBus();
        AudioServer.SetBusName(AudioServer.BusCount - 1, name);
    }

    private static void RemoveBus(string name)
    {
        if (AudioServer.GetBusIndex(name) is var index and >= 0)
        {
            AudioServer.RemoveBus(index);
        }
    }

    [TestCase]
    public void Ready_WithoutChildren_CreatesASoundPoolAndAMusicPlayer()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var sounds = manager.Sounds;
        var music = manager.Music;

        // Assert
        sounds.GetParent().ShouldBe(manager);
        music.GetParent().ShouldBe(manager);
        manager.GetChildCount().ShouldBe(2);

        manager.Free();
    }

    [TestCase]
    public void Sounds_WhenAChildPoolExists_UsesIt()
    {
        // Arrange
        var manager = new AudioManager();
        var pool = new SoundPool();
        manager.AddChild(pool);

        // Act
        var sounds = manager.Sounds;

        // Assert
        sounds.ShouldBe(pool);

        manager.Free();
    }

    [TestCase]
    public void Ready_WithCreateMissingBuses_AddsTheMissingBusesRoutedToMaster()
    {
        // Arrange
        RemoveBus("SFX");
        RemoveBus("Music");

        // Act
        var manager = CreateManager(createMissingBuses: true);

        // Assert
        AudioServer.GetBusIndex("SFX").ShouldBeGreaterThan(0);
        AudioServer.GetBusIndex("Music").ShouldBeGreaterThan(0);
        AudioServer.GetBusSend(AudioServer.GetBusIndex("Music")).ToString().ShouldBe("Master");

        manager.Free();
        RemoveBus("SFX");
        RemoveBus("Music");
    }

    [TestCase]
    public void SetVolume_ClampsToZeroToOne()
    {
        // Arrange
        AddBus(TestBus);
        var manager = CreateManager();

        // Act
        manager.SetVolume(TestBus, 0.25f);
        var quarter = manager.GetVolume(TestBus);
        manager.SetVolume(TestBus, 3f);
        var clamped = manager.GetVolume(TestBus);

        // Assert
        quarter.ShouldBe(0.25f, 0.001f);
        clamped.ShouldBe(1f, 0.001f);

        manager.Free();
        RemoveBus(TestBus);
    }

    [TestCase]
    public void SetVolume_WhenTheBusIsMissing_ChangesNothing()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.SetVolume("NoSuchBus", 0.5f);
        manager.SetMuted("NoSuchBus", true);

        // Assert
        manager.GetVolume("NoSuchBus").ShouldBe(0f);
        manager.IsMuted("NoSuchBus").ShouldBeFalse();

        manager.Free();
    }

    [TestCase]
    public void SaveThenLoad_RestoresVolumesAndMutes()
    {
        // Arrange
        AddBus(TestBus);
        var manager = CreateManager();
        manager.SetVolume(TestBus, 0.3f);
        manager.SetMuted(TestBus, true);
        var data = manager.Save();
        manager.SetVolume(TestBus, 1f);
        manager.SetMuted(TestBus, false);

        // Act
        manager.Load(data);

        // Assert
        manager.GetVolume(TestBus).ShouldBe(0.3f, 0.001f);
        manager.IsMuted(TestBus).ShouldBeTrue();

        manager.Free();
        RemoveBus(TestBus);
    }

    [TestCase]
    public void Load_IgnoresBusesMissingFromTheLayout()
    {
        // Arrange
        var manager = CreateManager();
        var data = new Godot.Collections.Dictionary<string, Variant>
        {
            ["volumes"] = new Godot.Collections.Dictionary { ["Removed"] = 0.5f },
            ["muted"] = new Godot.Collections.Dictionary { ["Removed"] = true },
        };

        // Act & Assert
        Should.NotThrow(() => manager.Load(data));

        manager.Free();
    }
}
