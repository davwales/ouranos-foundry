using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Core.Nodes;
using Ouranos.Foundry.Core.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Core.Nodes;

[TestSuite]
[RequireGodotRuntime]
public class EventBusBridgeTests
{
    private static Node TreeRoot => ((SceneTree)Engine.GetMainLoop()).Root;

    private static (EventBusBridge bridge, List<string> log) CreateBridge(string eventName = "")
    {
        var log = new List<string>();
        var bridge = new EventBusBridge().With(EventBusBridge.PropertyName.EventName, eventName);
        bridge.EventReceived += (name, payload) => log.Add($"{name}:{payload}");
        return (bridge, log);
    }

    [TestCase]
    public void EventReceived_WhenCSharpPublishesANamedEvent_IsEmitted()
    {
        // Arrange
        var (bridge, log) = CreateBridge();
        TreeRoot.AddChild(bridge);

        // Act
        EventBus.Publish(new NamedEvent("bridge_test_chest_opened", 3));

        // Assert
        log.ShouldBe(["bridge_test_chest_opened:3"]);

        bridge.Free();
    }

    [TestCase]
    public void EventReceived_WithEventName_OnlyEmitsMatchingEvents()
    {
        // Arrange
        var (bridge, log) = CreateBridge("bridge_test_boss_defeated");
        TreeRoot.AddChild(bridge);

        // Act
        EventBus.Publish(new NamedEvent("bridge_test_coin"));
        EventBus.Publish(new NamedEvent("bridge_test_boss_defeated", "Hydra"));

        // Assert
        log.ShouldBe(["bridge_test_boss_defeated:Hydra"]);

        bridge.Free();
    }

    [TestCase]
    public void Publish_CalledThroughGodot_ReachesCSharpSubscribers()
    {
        // Arrange
        var (bridge, _) = CreateBridge();
        var received = new List<NamedEvent>();
        using var subscription = EventBus.Subscribe<NamedEvent>(received.Add);

        // Act: the path GDScript takes.
        bridge.Call(EventBusBridge.MethodName.Publish, "bridge_test_level_up", 5);

        // Assert
        received.Count.ShouldBe(1);
        received[0].Name.ToString().ShouldBe("bridge_test_level_up");
        received[0].Payload.AsInt32().ShouldBe(5);

        bridge.Free();
    }

    [TestCase]
    public void EventReceived_AfterLeavingTheTree_IsNotEmitted()
    {
        // Arrange
        var (bridge, log) = CreateBridge();
        TreeRoot.AddChild(bridge);
        TreeRoot.RemoveChild(bridge);

        // Act
        EventBus.Publish(new NamedEvent("bridge_test_ignored"));

        // Assert
        log.ShouldBeEmpty();

        bridge.Free();
    }

    [TestCase]
    public void EventReceived_AfterReenteringTheTree_IsEmittedOnce()
    {
        // Arrange
        var (bridge, log) = CreateBridge();
        TreeRoot.AddChild(bridge);
        TreeRoot.RemoveChild(bridge);
        TreeRoot.AddChild(bridge);

        // Act
        EventBus.Publish(new NamedEvent("bridge_test_again"));

        // Assert
        log.ShouldBe(["bridge_test_again:"]);

        bridge.Free();
    }
}
