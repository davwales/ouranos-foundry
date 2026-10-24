# Ouranos.Foundry addon

Reusable game features for Godot 4 C#: genre- and perspective-agnostic, and they work with zero configuration.

| Feature | Summary |
|---------|---------|
| [Health](Features/Health/README.md) | Health component with HUD bar, hearts, floating and grouped UI monitors |
| [LootTables](Features/LootTables/README.md) | Weighted, deterministic, nestable loot tables as Godot resources |
| [ProceduralGeneration](Features/ProceduralGeneration/README.md) | Seeded world blueprint and on-demand chunk generation pipeline |
| [StateMachine](Core/README.md#state-machine) (Core) | `StateMachine`/`State` nodes, debug labels and a pure C# `StateMachine<TState>` |

## Requirements

- A Godot .NET editor matching the first three parts of the release version. Release `4.6.3.N` targets Godot 4.6.3.
- A C# project targeting `net10.0`, or an older target with `<LangVersion>14</LangVersion>`.

## Installing

Keep this folder at `res://addons/ouranos_foundry/`, because the editor icons are referenced by that path. Then build the C# project. No plugin needs enabling.

See the [main README](https://github.com/davwales/ouranos-foundry#using-foundry-in-your-game) for submodule and zip installation, and the [releases page](https://github.com/davwales/ouranos-foundry/releases) for release notes.

## License

[MIT](LICENSE)
