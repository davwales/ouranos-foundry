#nullable enable

using System;
using System.Collections.Generic;

namespace Ouranos.Foundry.Core.Types;

/// <summary>
/// Engine-free state machine keyed by any value (typically an enum). Register
/// callbacks with <see cref="Configure"/>, call <see cref="Start"/> once, then drive
/// it with <see cref="TransitionTo"/> and <see cref="Update"/>. Same transition rules
/// as the <c>StateMachine</c> node: same-state transitions are ignored, and
/// transitions requested from a callback are queued until the current one completes.
/// </summary>
public sealed class StateMachine<TState>(
    TState initialState,
    IEqualityComparer<TState>? comparer = null
)
    where TState : notnull
{
    private const int MaxChainedTransitions = 32;

    private readonly IEqualityComparer<TState> _comparer =
        comparer ?? EqualityComparer<TState>.Default;
    private readonly Dictionary<TState, Callbacks> _callbacks = new(
        comparer ?? EqualityComparer<TState>.Default
    );
    private readonly Queue<TState> _pending = new();
    private bool _transitioning;

    public TState Current { get; private set; } = initialState;

    public bool IsStarted { get; private set; }

    /// <summary>
    /// Raised after every transition with (from, to). Not raised by <see cref="Start"/>.
    /// </summary>
    public event Action<TState, TState>? StateChanged;

    /// <summary>
    /// Sets the callbacks for <paramref name="state"/>, replacing any previous ones.
    /// <paramref name="onEnter"/> receives the previous state and <paramref name="onExit"/> the next.
    /// </summary>
    public StateMachine<TState> Configure(
        TState state,
        Action<TState>? onEnter = null,
        Action<TState>? onExit = null,
        Action<double>? onUpdate = null
    )
    {
        _callbacks[state] = new Callbacks(onEnter, onExit, onUpdate);
        return this;
    }

    /// <summary>
    /// Enters the initial state. Its enter callback receives the initial state itself.
    /// </summary>
    public void Start()
    {
        if (IsStarted)
        {
            return;
        }

        IsStarted = true;
        Run(() => Get(Current)?.OnEnter?.Invoke(Current));
    }

    /// <summary>
    /// Returns false if <paramref name="next"/> is already current. During another
    /// transition the change is queued and this returns true.
    /// </summary>
    public bool TransitionTo(TState next)
    {
        if (!IsStarted)
        {
            throw new InvalidOperationException("Call Start() before TransitionTo().");
        }

        if (_transitioning)
        {
            _pending.Enqueue(next);
            return true;
        }

        if (_comparer.Equals(next, Current))
        {
            return false;
        }

        Run(() => Apply(next));
        return true;
    }

    /// <summary>
    /// Runs the current state's update callback.
    /// </summary>
    public void Update(double delta)
    {
        if (IsStarted)
        {
            Get(Current)?.OnUpdate?.Invoke(delta);
        }
    }

    private void Run(Action first)
    {
        _transitioning = true;
        try
        {
            first();

            var chained = 0;
            while (_pending.TryDequeue(out var queued))
            {
                if (++chained > MaxChainedTransitions)
                {
                    throw new InvalidOperationException(
                        $"More than {MaxChainedTransitions} chained transitions; check for a transition loop."
                    );
                }

                Apply(queued);
            }
        }
        finally
        {
            _pending.Clear();
            _transitioning = false;
        }
    }

    private void Apply(TState next)
    {
        var previous = Current;
        if (_comparer.Equals(next, previous))
        {
            return;
        }

        Get(previous)?.OnExit?.Invoke(next);
        Current = next;
        Get(next)?.OnEnter?.Invoke(previous);
        StateChanged?.Invoke(previous, next);
    }

    private Callbacks? Get(TState state)
    {
        return _callbacks.TryGetValue(state, out var callbacks) ? callbacks : null;
    }

    private sealed record Callbacks(
        Action<TState>? OnEnter,
        Action<TState>? OnExit,
        Action<double>? OnUpdate
    );
}
