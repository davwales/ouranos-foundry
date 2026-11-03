#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Ouranos.Foundry.Core.Types;

/// <summary>
/// A global, typed message bus for game-wide events that many unrelated listeners care about
/// (an enemy died, loot dropped, a quest finished), so achievements, audio and UI can react
/// without references to whoever raised them. Inside a feature, and between a node and its
/// children, prefer signals.
/// <para>
/// Events are delivered synchronously, on the publishing thread, to subscribers of exactly the
/// published type <c>T</c>, in subscription order. Publish from the main thread
/// when handlers touch the scene tree. A handler that throws is logged and the rest still run.
/// </para>
/// </summary>
public static class EventBus
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Type, Subscription[]> Subscriptions = [];

    /// <summary>
    /// Calls <paramref name="handler"/> for every <typeparamref name="T"/> published until the
    /// returned subscription is disposed. If the handler belongs to a Godot object that is
    /// freed, the subscription ends by itself.
    /// </summary>
    public static IDisposable Subscribe<T>(Action<T> handler)
    {
        return Add(new Subscription(typeof(T), handler, null));
    }

    /// <summary>
    /// Like <see cref="Subscribe{T}(Action{T})"/>, but also ends the subscription when
    /// <paramref name="owner"/> exits the scene tree, so a node needn't unsubscribe. Subscribe in
    /// <c>_EnterTree</c> for nodes that can leave and re-enter the tree.
    /// </summary>
    public static IDisposable Subscribe<T>(Node owner, Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var subscription = new Subscription(typeof(T), handler, owner);
        owner.Connect(
            Node.SignalName.TreeExiting,
            subscription.OnOwnerExiting,
            (uint)GodotObject.ConnectFlags.OneShot
        );
        return Add(subscription);
    }

    public static void Publish<T>(T evt)
    {
        Subscription[]? subscriptions;
        lock (Gate)
        {
            Subscriptions.TryGetValue(typeof(T), out subscriptions);
        }

        foreach (var subscription in subscriptions ?? [])
        {
            // Earlier handlers may have disposed later subscriptions, or freed their owners.
            if (!subscription.IsLive)
            {
                subscription.Dispose();
                continue;
            }

            try
            {
                ((Action<T>)subscription.Handler)(evt);
            }
            catch (Exception e)
            {
                GD.PushError($"EventBus: a {typeof(T).Name} handler threw: {e}");
            }
        }
    }

    /// <summary>How many subscriptions to <typeparamref name="T"/> are active.</summary>
    public static int SubscriberCount<T>()
    {
        lock (Gate)
        {
            return Subscriptions.TryGetValue(typeof(T), out var subscriptions)
                ? subscriptions.Count(subscription => subscription.IsLive)
                : 0;
        }
    }

    private static Subscription Add(Subscription subscription)
    {
        // Copy on write, so Publish can iterate without holding the lock.
        var type = subscription.Type;
        lock (Gate)
        {
            Subscriptions[type] = Subscriptions.TryGetValue(type, out var existing)
                ? [.. existing, subscription]
                : [subscription];
        }

        return subscription;
    }

    private static void Remove(Subscription subscription)
    {
        lock (Gate)
        {
            if (!Subscriptions.TryGetValue(subscription.Type, out var existing))
            {
                return;
            }

            var remaining = existing.Where(other => other != subscription).ToArray();
            if (remaining.Length > 0)
            {
                Subscriptions[subscription.Type] = remaining;
            }
            else
            {
                Subscriptions.Remove(subscription.Type);
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Node? _owner;

        // The object whose lifetime bounds the subscription: the owner, else the handler's target.
        private readonly GodotObject? _anchor;
        private bool _disposed;

        public Subscription(Type type, Delegate handler, Node? owner)
        {
            Type = type;
            Handler = handler;
            _owner = owner;
            _anchor = owner ?? handler.Target as GodotObject;
            if (owner is not null)
            {
                OnOwnerExiting = Callable.From(Dispose);
            }
        }

        public Type Type { get; }

        public Delegate Handler { get; }

        public Callable OnOwnerExiting { get; }

        public bool IsLive =>
            !_disposed && (_anchor is null || GodotObject.IsInstanceValid(_anchor));

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Remove(this);
            if (
                _owner is not null
                && GodotObject.IsInstanceValid(_owner)
                && _owner.IsConnected(Node.SignalName.TreeExiting, OnOwnerExiting)
            )
            {
                _owner.Disconnect(Node.SignalName.TreeExiting, OnOwnerExiting);
            }
        }
    }
}
