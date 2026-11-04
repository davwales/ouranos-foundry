using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.Audio.Nodes;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Audio;

[TestSuite]
[RequireGodotRuntime]
public class VolumeSliderTests
{
    private const string TestBus = "VolumeSliderTestBus";

    private static VolumeSlider CreateSlider()
    {
        AudioServer.AddBus();
        AudioServer.SetBusName(AudioServer.BusCount - 1, TestBus);
        var slider = new VolumeSlider().With(VolumeSlider.PropertyName.Bus, TestBus);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(slider);
        slider._Process(0);
        return slider;
    }

    private static void Cleanup(VolumeSlider slider)
    {
        slider.Free();
        AudioServer.RemoveBus(AudioServer.GetBusIndex(TestBus));
    }

    [TestCase]
    public void Value_WhenChanged_SetsTheBusVolume()
    {
        // Arrange
        var slider = CreateSlider();

        // Act
        slider.Value = 0.4;

        // Assert
        AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex(TestBus)).ShouldBe(0.4f, 0.001f);

        Cleanup(slider);
    }

    [TestCase]
    public void Process_WhenTheBusChangesElsewhere_FollowsIt()
    {
        // Arrange
        var slider = CreateSlider();
        slider.Value.ShouldBe(1d);

        // Act
        AudioServer.SetBusVolumeLinear(AudioServer.GetBusIndex(TestBus), 0.2f);
        slider._Process(0);

        // Assert
        slider.Value.ShouldBe(0.2, 0.001);

        Cleanup(slider);
    }

    [TestCase]
    public void Process_WhenTheBusIsMissing_DisablesTheSlider()
    {
        // Arrange
        var slider = new VolumeSlider().With(VolumeSlider.PropertyName.Bus, "NoSuchBus");
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(slider);

        // Act
        slider._Process(0);

        // Assert
        slider.Editable.ShouldBeFalse();

        slider.Free();
    }
}
