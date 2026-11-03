using System;
using System.Collections.Generic;
using GdUnit4;
using Godot;
using Ouranos.Foundry.Core.Types;
using Shouldly;

namespace Ouranos.Foundry.Tests.Core.Types;

// The bus is global, so every test publishes its own event types.
[TestSuite]
[RequireGodotRuntime]
public class EventBusTests
{
    private static Node TreeRoot => ((SceneTree)Engine.GetMainLoop()).Root;

    [TestCase]
    public void Publish_WithSubscriber_DeliversTheEvent()
    {
        // Arrange
        var received = new List<int>();
        using var subscription = EventBus.Subscribe<Delivered>(evt => received.Add(evt.Value));

        // Act
        EventBus.Publish(new Delivered(7));

        // Assert
        received.ShouldBe([7]);
    }

    [TestCase]
    public void Publish_OnlyReachesSubscribersOfThePublishedType()
    {
        // Arrange
        var received = new List<string>();
        using var a = EventBus.Subscribe<TypedA>(_ => received.Add("A"));
        using var b = EventBus.Subscribe<TypedB>(_ => received.Add("B"));

        // Act
        EventBus.Publish(new TypedA());

        // Assert
        received.ShouldBe(["A"]);
    }

    [TestCase]
    public void Publish_RunsHandlersInSubscriptionOrder()
    {
        // Arrange
        var received = new List<int>();
        using var first = EventBus.Subscribe<Ordered>(_ => received.Add(1));
        using var second = EventBus.Subscribe<Ordered>(_ => received.Add(2));
        using var third = EventBus.Subscribe<Ordered>(_ => received.Add(3));

        // Act
        EventBus.Publish(new Ordered());

        // Assert
        received.ShouldBe([1, 2, 3]);
    }

    [TestCase]
    public void Dispose_StopsDeliveryAndIsIdempotent()
    {
        // Arrange
        var count = 0;
        var subscription = EventBus.Subscribe<Disposed>(_ => count++);
        EventBus.Publish(new Disposed());

        // Act
        subscription.Dispose();
        subscription.Dispose();
        EventBus.Publish(new Disposed());

        // Assert
        count.ShouldBe(1);
        EventBus.SubscriberCount<Disposed>().ShouldBe(0);
    }

    [TestCase]
    public void Publish_WhenAHandlerThrows_StillRunsTheOthers()
    {
        // Arrange
        var count = 0;
        using var failing = EventBus.Subscribe<Throwing>(_ =>
            throw new InvalidOperationException("expected by the test")
        );
        using var counting = EventBus.Subscribe<Throwing>(_ => count++);

        // Act
        EventBus.Publish(new Throwing());

        // Assert
        count.ShouldBe(1);
    }

    [TestCase]
    public void Publish_WhenAHandlerDisposesALaterSubscription_SkipsIt()
    {
        // Arrange
        var received = new List<string>();
        IDisposable? later = null;
        using var first = EventBus.Subscribe<Unsubscribing>(_ =>
        {
            received.Add("first");
            later?.Dispose();
        });
        later = EventBus.Subscribe<Unsubscribing>(_ => received.Add("later"));

        // Act
        EventBus.Publish(new Unsubscribing());

        // Assert
        received.ShouldBe(["first"]);
    }

    [TestCase]
    public void Publish_WhenAHandlerSubscribes_DeliversFromTheNextEvent()
    {
        // Arrange
        var received = new List<string>();
        IDisposable? added = null;
        using var first = EventBus.Subscribe<Subscribing>(_ =>
            added ??= EventBus.Subscribe<Subscribing>(_ => received.Add("added"))
        );

        // Act
        EventBus.Publish(new Subscribing());
        var afterFirst = received.Count;
        EventBus.Publish(new Subscribing());

        // Assert
        afterFirst.ShouldBe(0);
        received.ShouldBe(["added"]);

        added?.Dispose();
    }

    [TestCase]
    public void Subscribe_WithOwner_EndsWhenTheOwnerExitsTheTree()
    {
        // Arrange
        var owner = new Node();
        TreeRoot.AddChild(owner);
        var count = 0;
        EventBus.Subscribe<OwnerExits>(owner, _ => count++);
        EventBus.Publish(new OwnerExits());

        // Act
        TreeRoot.RemoveChild(owner);
        EventBus.Publish(new OwnerExits());

        // Assert
        count.ShouldBe(1);
        EventBus.SubscriberCount<OwnerExits>().ShouldBe(0);

        owner.Free();
    }

    [TestCase]
    public void Subscribe_WithOwner_WhenOwnerIsFreedOutsideTheTree_DeliversNothing()
    {
        // Arrange
        var owner = new Node();
        var count = 0;
        EventBus.Subscribe<OwnerFreed>(owner, _ => count++);

        // Act
        owner.Free();
        EventBus.Publish(new OwnerFreed());

        // Assert
        count.ShouldBe(0);
        EventBus.SubscriberCount<OwnerFreed>().ShouldBe(0);
    }

    [TestCase]
    public void Subscribe_WithAFreedNodesMethod_DeliversNothing()
    {
        // Arrange
        var recorder = new EventRecorder();
        EventBus.Subscribe<HandlerFreed>(recorder.Record);

        // Act
        recorder.Free();
        EventBus.Publish(new HandlerFreed());

        // Assert
        recorder.Received.ShouldBeEmpty();
        EventBus.SubscriberCount<HandlerFreed>().ShouldBe(0);
    }

    [TestCase]
    public void Dispose_WithOwner_DisconnectsFromTheOwner()
    {
        // Arrange
        var owner = new Node();
        var before = owner.GetSignalConnectionList(Node.SignalName.TreeExiting).Count;
        var subscription = EventBus.Subscribe<OwnerDisposed>(owner, _ => { });

        // Act
        subscription.Dispose();

        // Assert
        owner.GetSignalConnectionList(Node.SignalName.TreeExiting).Count.ShouldBe(before);

        owner.Free();
    }

    private sealed record Delivered(int Value);

    private sealed record TypedA;

    private sealed record TypedB;

    private sealed record Ordered;

    private sealed record Disposed;

    private sealed record Throwing;

    private sealed record Unsubscribing;

    private sealed record Subscribing;

    private sealed record OwnerExits;

    private sealed record OwnerFreed;

    private sealed record HandlerFreed;

    private sealed record OwnerDisposed;
}
