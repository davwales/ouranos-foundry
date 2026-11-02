# StatusEffects

Timed, ticking and stacking status effects: buffs, debuffs, damage or healing over time, crowd control, auras, or anything else your game needs. The engine handles only the bookkeeping: durations, ticks, stacks and lifecycle signals. It assumes nothing about what an effect does, so UI, VFX, audio and gameplay code can each react without knowing about each other.

## Pieces

| Type | Kind | Role |
|------|------|------|
| `StatusEffectDefinition` | Resource | One effect: `Id`, `DisplayName`, `Description`, `Icon`, `DurationType`, `Duration`, `TickInterval`, `Stacking`, `MaxStacks`. Optional hooks: `OnApplied`, `OnRefreshed`, `OnTicked`, `OnRemoved`. |
| `StatusEffectComponent` | Node | The effects active on its owner: `ApplyEffect`, `RemoveEffect`, `ClearEffects`, `HasEffect`, `GetEffect`, `GetActiveEffects`, `Advance`. |
| `StatusEffectInstance` | Resource | One active effect: `Definition`, `Id`, `Source`, `Stacks`, `Duration`, `RemainingTime`, `IsTimed`, `IsActive`. Created by the component. |
| `StatusEffectBar` | HBoxContainer | Buff bar: an icon (or name), stack count, countdown and description tooltip per active effect. |

## Usage

1. Add a `StatusEffectComponent` to anything that can be affected, e.g. as a child of the player or an enemy.
2. Create a `StatusEffectDefinition` resource per effect (**New Resource → StatusEffectDefinition**) and give it a unique `Id`.
3. Apply it from gameplay code, and give it meaning by reacting to the signals:

```csharp
effects.ApplyEffect(poison, source: attacker);

effects.EffectTicked += effect =>
{
    if (effect.Id == "poison")
    {
        health.Damage(2f * effect.Stacks);
    }
};

var damage = effects.HasEffect("rage") ? baseDamage * 2 : baseDamage;
```

For a buff bar, add a `StatusEffectBar` and set its `Target` to the component, or call `MonitorStatusEffectComponent(component)`.

From GDScript, connect to the same signals and call the same methods, e.g. `effects.ApplyEffect(poison, null)`. GDScript has to pass every argument, because C# default arguments aren't exposed to it.

## Semantics

- **One instance per `Id`** on a component. Applying an effect whose id is already active follows the *active* effect's `Stacking`:

  | `Stacking` | Re-applying… |
  |------------|--------------|
  | `Refresh` (default) | restarts the duration. |
  | `StackIntensity` | adds a stack (up to `MaxStacks`; 0 means no cap) and restarts the duration. At the cap it still restarts the duration. |
  | `StackDuration` | adds another `Duration` to `RemainingTime` (and to `Duration`). |
  | `Ignore` | changes nothing: no signal, and `ApplyEffect` returns null. |

  Every other re-application emits `EffectRefreshed` and updates `Source` to the latest applier.
- **`DurationType`:** `Timed` effects expire after `Duration`. `Infinite` effects last until removed. `Instant` effects emit `EffectApplied` and then `EffectRemoved(Expired)` in the same call, never appear in `GetActiveEffects`, and don't touch an active effect with the same id.
- **Ticks:** with `TickInterval > 0`, the effect ticks every `TickInterval` from when it was first applied. Refreshing doesn't reset the tick phase, so re-applying can't skip or double a tick. A large delta fires every tick it covers, but never past the expiry. A tick that falls exactly on the expiry fires before the effect expires.
- **Timing:** `UpdateMode` picks what advances time: `Process` (default) or `Physics` delta, or `Manual`, where only your calls to `Advance(amount)` do. Use `Manual` for turn-based games: `Advance(1)` once per turn makes `Duration` and `TickInterval` count turns. Pausing the tree (or the component's `ProcessMode`) pauses its effects.
- **Signals:** `EffectApplied(effect)`, `EffectRefreshed(effect)`, `EffectTicked(effect)`, `EffectRemoved(effect, reason)`. `reason` is `Expired` or `Removed` (`RemoveEffect`/`ClearEffects`). Every `EffectApplied` is eventually followed by exactly one `EffectRemoved`, unless the component is freed first.
- Signal handlers and hooks may apply or remove effects, including the one being processed. `GetActiveEffects` returns a snapshot in application order, so it's safe to remove effects while iterating it.
- `ApplyEffect` pushes an error and returns null for a null definition or an empty `Id`.

## Extending

- **Self-contained effects:** subclass `StatusEffectDefinition`, add your own exports and override the hooks. Each hook runs just before its signal and receives the component and the instance. A definition is shared by every target, so keep per-target state on the instance (e.g. `effect.SetMeta("modifier", …)`), not on the definition. The demo's `HealOverTimeEffect` is an example.
- **Categories** (debuffs, magic, poison) for cleanses or immunities: subclass the definition with the fields your game needs, then filter `GetActiveEffects()`.
- **Immunity and resistance:** subclass `StatusEffectComponent` and override `ApplyEffect`, returning null to block an effect or calling `base.ApplyEffect` otherwise.
- **Stat modifiers:** add the modifier in `OnApplied`/`EffectApplied` (scaled by `Stacks` if you like), update it in `OnRefreshed`, and remove it in `OnRemoved`/`EffectRemoved`.
- **Custom buff bar:** subclass `StatusEffectBar` and override `CreateEntry` and `UpdateEntry`, or build your own Control from the signals. `StatusEffectBar.FormatLabel(effect)` gives the default "x3 4.2" text.
