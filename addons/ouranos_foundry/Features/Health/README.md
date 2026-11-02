# Health

A health component plus interchangeable UI monitors. The component knows nothing about UI. Monitors subscribe to its signals.

## Pieces

| Type | Kind | Role |
|------|------|------|
| `HealthComponent` | Node | Health state: `Damage`, `Heal`, `SetBaseMaxHealth`, `RefreshMaxHealth`, `SetInvulnerable`, `Serialize`/`Deserialize`. |
| `PlayerHealthBar` | Control | HUD bar with a lagging "catch-up" bar. |
| `HeartsContainer` | HBoxContainer | Zelda-style hearts (`HealthPointsPerHeart`, optional per-heart `HeartVisuals`). |
| `FloatingHealthBar2D` / `FloatingHealthBar3D` | Node2D / Node3D | Over-head bars that show on damage for `DisplayDuration`. |
| `GroupedHealthManager` | Control | Party/boss list: one row per registered component (`RegisterComponent`, `UnregisterComponent`). |
| `HeartVisualDefinition` | record | Colours/textures for full, half and empty hearts. |

## Usage

Assign a monitor's `Target` export in the inspector, or call `MonitorHealthComponent(component)` from a parent. Monitors connect in `_Ready` and disconnect in `_ExitTree`; the shared `Utils/HealthMonitorConnection` handles the subscription bookkeeping.

## Semantics

- `IsDead` is derived from `CurrentHealth <= 0`. It is never stored.
- `StartingHealth = -1` means spawn at full health.
- `Damage` is a no-op when dead or invulnerable. `Heal` is a no-op when dead unless `AllowRevival` is set.
- Both return the **effective** amount applied, after clamping.
- Signals: `HealthChanged(current, max)`, `HealthIncreased`, `HealthDecreased(amount, current, source)`, `HealthDepleted`, `HealthRestored`, `MaxHealthChanged`.

## Extending

- **Dynamic max health** (buffs, level scaling): set `MaxHealthProvider = () => ...`, then call `RefreshMaxHealth()` when the inputs change.
- **Custom UI:** subscribe to the component's signals in your own Control. Use the existing monitors as reference.
- **Saving:** `Serialize()` returns a Godot `Dictionary` (`current_health`, `max_health`), and `Deserialize(dict)` restores it. Both are virtual, so a subclass can save more. The component is an `ISaveable`: add it to the `saveable` group and a Persistence `SaveManager` saves it, keyed by `SaveKey` or, when that is empty, its node path.
