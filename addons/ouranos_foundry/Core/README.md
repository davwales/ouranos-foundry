# Core

Shared, feature-agnostic building blocks. Features may depend on Core; Core never depends on a feature.

## State machine

A generic state machine for movement profiles, AI, menus, game flow and so on. It comes as scene nodes, plus a pure C# variant for code that doesn't live in the scene tree.

### Pieces

| Type | Kind | Role |
|------|------|------|
| `StateMachine` | Node | Runs one `State` child at a time. Forwards `_Process`, `_PhysicsProcess` and `_UnhandledInput` to the current state. Emits `StateChanged(from, to)`. |
| `State` | Node | One state; its node name is the state name. Lifecycle: `Enter`, `Exit`, `Process`, `PhysicsProcess`, `HandleInput`. Emits `Entered`, `Exited` and `TransitionRequested`. |
| `StateDebugLabel` / `StateDebugLabel3D` | Label / Label3D | Shows the monitored machine's current state (`Prefix` + name). |
| `StateMachine<TState>` | C# class | Engine-free machine keyed by any value, usually an enum. |

### Usage

```
Player (CharacterBody2D)
└─ StateMachine          InitialState = Idle (defaults to the first State child)
   ├─ Idle   (State)
   ├─ Move   (State)
   └─ Dash   (State)
```

```csharp
public partial class MoveState : State
{
    public override void PhysicsProcess(double delta)
    {
        if (Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") == Vector2.Zero)
        {
            RequestTransition("Idle");
        }
    }
}
```

- States change state with `RequestTransition(name)`, which emits a signal the machine listens to, so a state never calls its parent. Owners call `machine.TransitionTo(name)`.
- `TransitionTo` returns `false` and changes nothing if the state is unknown (it also pushes an error) or already current.
- The order is `from.Exit(to)` → `CurrentState = to` → `to.Enter(from)` → `StateChanged(from, to)`. For the initial state, `from` is empty.
- A transition requested during `Exit`, `Enter` or a `StateChanged` handler is queued and runs after the current one completes, so transitions never nest. More than 32 chained transitions push an error and drop the rest.
- From GDScript, connect to a state's `Entered`/`Exited` signals, read `IsActive`, and call `RequestTransition(...)` / `TransitionTo(...)`. C# script members keep their PascalCase names. The lifecycle methods are C# virtuals that GDScript can't override.

The pure C# variant follows the same rules:

```csharp
var machine = new StateMachine<Mode>(Mode.Idle)
    .Configure(Mode.Idle, onEnter: previous => ..., onUpdate: delta => ...)
    .Configure(Mode.Move, onExit: next => ...);
machine.StateChanged += (from, to) => ...;
machine.Start();                  // enters Idle
machine.TransitionTo(Mode.Move);
machine.Update(delta);
```

### Extending

- Subclass `State` and override its lifecycle methods. Share wiring (the body being moved, an animation player) through a base state with `[Export]`s.
- Subclass `StateMachine` to add guards or logging. `CurrentState`, `PreviousState` and `States` (by name) are public.
- Only direct `State` children are registered when the machine is ready, and again whenever it re-enters the tree.

## Event bus

A global, typed message bus for game-wide events, such as an enemy dying, loot dropping or a quest finishing. Achievements, audio, analytics and UI can react without references to whoever raised the event.

### Pieces

| Type | Kind | Role |
|------|------|------|
| `EventBus` | Static C# class | `Subscribe<T>(handler)` / `Subscribe<T>(owner, handler)` return an `IDisposable`; `Publish<T>(evt)`; `SubscriberCount<T>()`. |
| `NamedEvent` | C# record | `(StringName Name, Variant Payload)`: the event type GDScript can send and receive. |
| `EventBusBridge` | Node | Re-emits every `NamedEvent` as `EventReceived(name, payload)` while it's in the tree, optionally filtered by `EventName`. `Publish(name, payload)` puts one on the bus. |

### Usage

Define an event per fact, as an immutable record, and publish it where the fact happens:

```csharp
public sealed record EnemyDefeated(string Enemy, int Experience);

health.HealthDepleted += () => EventBus.Publish(new EnemyDefeated(Name, 50));
```

Subscribe from any node, passing the node as the owner. The subscription ends by itself when the node exits the tree:

```csharp
public override void _EnterTree()
{
    EventBus.Subscribe<EnemyDefeated>(this, OnEnemyDefeated);
}
```

Plain C# objects dispose the returned subscription instead: `_subscription.Dispose()`.

From GDScript, use an `EventBusBridge`, either as a child node or as an autoload (add `res://addons/ouranos_foundry/Core/Nodes/EventBusBridge.cs` as, say, `Events`). C# code reaches GDScript by publishing a `NamedEvent`:

```gdscript
func _ready() -> void:
	Events.EventReceived.connect(_on_event)
	Events.Publish(&"bell_rung", null)

func _on_event(name: StringName, payload: Variant) -> void:
	if name == &"chest_opened":
		gold += payload
```

```csharp
EventBus.Publish(new NamedEvent("chest_opened", 10));
```

You can also drop a bridge into a scene with `EventName = chest_opened` and connect its `EventReceived` in the editor, without writing any code.

### Semantics

- **Typed by exact type.** `Publish<T>` reaches subscribers of `T` only, not subscribers of base types or interfaces.
- **Synchronous** on the publishing thread, in subscription order. Publish from the main thread when handlers touch the scene tree (procgen passes run on worker threads).
- **Re-entrancy.** A handler may publish, subscribe or unsubscribe. A subscription added during a publish receives the next event, not the current one. A subscription disposed during a publish isn't called again.
- **Failures.** A handler that throws is reported with `GD.PushError`, and the remaining handlers still run.
- **Lifetime.** An owned subscription ends when its owner exits the tree. Subscribe in `_EnterTree` (not `_Ready`) for nodes that leave and re-enter the tree, such as pooled ones. Whether or not it has an owner, a subscription never calls into a freed node: one whose owner, or whose handler's target, has been freed is dropped at the next publish.

### Bus or signal?

- **Use a signal** between a node and its parent or children, inside a feature, and whenever the listener already holds a reference ("signal up, call down"). Signals are explicit, visible in the editor and scoped to one emitter.
- **Use the bus** for facts that many unrelated systems care about, where the listener can't or shouldn't find the emitter: achievements counting kills across every enemy, an audio manager, analytics, a toast UI.
- **Publish facts, not commands.** `EnemyDefeated`, not `PlaySound`. A command has one intended receiver, so call it directly.
- **Library features don't publish to the bus.** They expose signals, and your game bridges the ones it cares about, as in the `HealthDepleted` example above. Your game owns its event types.
