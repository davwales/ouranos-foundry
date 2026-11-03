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

## Stats

Attributes such as strength, max health or move speed, with modifiers from gear, effects and talents.

### Pieces

| Type | Kind | Role |
|------|------|------|
| `StatDefinition` | Resource | One stat: `Id`, `DisplayName`, `BaseValue`, and the `MinValue`/`MaxValue` the final value is clamped to (unbounded by default). |
| `StatModifier` | Resource | A change to one stat: `StatId`, `Operation`, `Value`, `Priority`. Plain data that items and effects can hold and share. |
| `StatBlock` | Node | The stats of one entity: `GetValue`, `GetBaseValue`/`SetBaseValue`, `AddModifier`, `RemoveModifier`, `RemoveModifiersFrom`, `ClearModifiers`, `GetModifiers`, `Recalculate`. Emits `StatChanged(statId, oldValue, newValue)`. Implements `ISaveable`. |

### Usage

1. Create a `StatDefinition` per stat (**New Resource → StatDefinition**) and give it a unique `Id`.
2. Add a `StatBlock` to the entity and list the definitions in `Stats`.
3. Apply modifiers with the thing that grants them as the source, and remove them the same way:

```csharp
foreach (var modifier in sword.Modifiers)
{
    stats.AddModifier(modifier, source: sword);
}

stats.RemoveModifiersFrom(sword);   // on unequip

var damage = stats.GetValue("strength") * 2;
```

To drive another feature, read the stat and react to `StatChanged`. For example, max health:

```csharp
health.MaxHealthProvider = () => stats.GetValue("max_health");
stats.StatChanged += (id, _, _) =>
{
    if (id == "max_health")
    {
        health.RefreshMaxHealth();
    }
};
```

### Evaluation order

```
value = (base + Σ Add) × (1 + Σ PercentAdd) × Π Multiply
```

- The order is fixed, not the order modifiers were added in, so the same modifiers always give the same value. With base 10, +5, +20%, +30% and ×2: (10 + 5) × 1.5 × 2 = 45.
- `PercentAdd` values are fractions (0.2 = +20%) and add up with each other. `Multiply` values are factors (1.5 = ×1.5) and compound.
- If any `Override` modifier applies, its `Value` replaces the result and every other modifier is ignored. The highest `Priority` wins; on a tie, the most recently added one does. Other operations ignore `Priority`.
- The result is then clamped to the definition's `MinValue`/`MaxValue`. Base values aren't clamped, so removing a large debuff restores the real value.
- `StatChanged` fires only when a final value moves, with the old and new final values.

### Semantics

- **Sources** are any `GodotObject`: an item resource, a `StatusEffectInstance`, a node. Two items sharing one modifier resource are separate applications, removed separately by source. `RemoveModifier(modifier)` removes every application of that modifier, whatever its source.
- **Unknown stats:** `GetValue` and friends push an error and return 0; `AddModifier` returns false. Check `HasStat` for optional stats.
- **Stats** is read the first time the block is used. A duplicated or empty `Id` pushes an error and is skipped.
- **Saving** stores base values only. Modifiers belong to their sources (equipment, effects), which re-apply them when they are restored. Saved stats the block no longer has are ignored.
- From GDScript, call the same methods and connect to `StatChanged`. GDScript has to pass every argument, e.g. `stats.AddModifier(modifier, null)`.

### Extending

- Override `Calculate(stat, baseValue, modifiers)` for a custom formula, e.g. diminishing returns. The block still clamps the result and emits `StatChanged`. Call `Recalculate()` when the formula depends on something outside the block, or after editing an applied modifier in place.
- Override `AddModifier` to add immunities, or to scale modifiers before passing them to `base.AddModifier`.
- Derived stats (max health from vitality): listen to `StatChanged` and `SetBaseValue` on the derived stat.
- Status effects that modify stats: subclass `StatusEffectDefinition`, add its modifiers in `OnApplied` with the effect instance as the source, and `RemoveModifiersFrom(effect)` in `OnRemoved`.
