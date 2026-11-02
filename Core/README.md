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
