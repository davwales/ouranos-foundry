# Ouranos.Foundry

Shared Godot C# library for building games. Features are self-contained vertical slices that consuming projects can use independently. Licensed under [MIT](LICENSE).

## Quick Start

1. **Prerequisites:** Godot 4.6.x (.NET build), .NET 10 SDK, git-lfs
2. **Set up** the clone: `Automation/setup.sh` (restores .NET tools and installs the git hooks)
3. **Open** `project.godot` in the Godot editor
4. **Run** a demo: open a scene under `Demos/` and press F6, or press F5 for the main scene
5. **New feature?** Copy `Templates/Feature/` → `addons/ouranos_foundry/Features/YourName/` and follow its `_README.md`

## Features

| Feature | Summary |
|---------|---------|
| [Health](addons/ouranos_foundry/Features/Health/README.md) | Health component with HUD bar, hearts, floating and grouped UI monitors |
| [LootTables](addons/ouranos_foundry/Features/LootTables/README.md) | Weighted, deterministic, nestable loot tables as Godot resources |
| [ProceduralGeneration](addons/ouranos_foundry/Features/ProceduralGeneration/README.md) | Seeded world blueprint and on-demand chunk generation pipeline |

## Using Foundry in your game

Foundry ships as source in a Godot addon folder, `addons/ouranos_foundry/`. Godot only registers C# node and resource scripts that are compiled into the game's own assembly, so there is no NuGet package. Your game's `.csproj` picks up the addon's `.cs` files automatically.

**Requirements**

- A Godot .NET editor whose version matches the release (see [Versioning](#versioning)).
- A C# project targeting `net10.0`. Foundry uses C# 14 features, so an older target framework also needs `<LangVersion>14</LangVersion>`.
- You don't need global usings or `<Nullable>enable</Nullable>`. The addon files carry their own `using` and `#nullable` directives.

**Install as a git submodule** (recommended, since it makes upgrades easy):

```sh
git submodule add -b addon https://github.com/davwales/ouranos-foundry.git addons/ouranos_foundry
git -C addons/ouranos_foundry checkout addon-v4.6.3.0   # pin a release
git add addons/ouranos_foundry && git commit -m "chore: add Ouranos.Foundry 4.6.3.0"
```

The `addon` branch contains only the addon folder, so no demos, tests or project files end up in your game.

**Or install from a release zip.** Download `ouranos_foundry-v<version>.zip` from the [releases page](https://github.com/davwales/ouranos-foundry/releases) and extract it into your project root. It contains `addons/ouranos_foundry/`.

**Then** build the C# project (`dotnet build`, or **Build** in the editor). Foundry's nodes (`HealthComponent`, `LootTableRoller`, `WorldGenerator`, …) now appear in the **Create New Node** dialog, and its resources in **New Resource**. You don't need to enable anything under *Project Settings → Plugins*.

**Upgrading.** Read the [CHANGELOG](addons/ouranos_foundry/CHANGELOG.md) for **BREAKING** entries first. Then move the submodule to the new tag (`git -C addons/ouranos_foundry fetch --tags && git -C addons/ouranos_foundry checkout addon-v<version>`), or delete the folder and extract the new zip.

**Known limitation.** Godot keeps one flat registry of global class names. A class in your game with the same name as a Foundry class (for example, your own `HealthComponent`) conflicts with it, even in a different namespace. Rename your class if that happens.

### Versioning

A release version is `<Godot version>.<N>`: `4.6.3.0` is the first Foundry release built and tested against Godot 4.6.3, and `4.6.3.1` the next. Pick the newest release whose first three parts match your editor. Foundry's own breaking changes are marked **BREAKING** in the CHANGELOG, because the version number can't express them.

Releases are cut by pushing a `v<version>` tag on `main` after updating `<Version>` in `OuranosFoundry.csproj` and the CHANGELOG. `Automation/check-release.sh` verifies that the tag matches the pinned Godot versions. The release workflow then publishes the `addon` branch, an `addon-v<version>` tag and the zip. Every PR already builds both and installs them into a generated Godot game (`Automation/consumer-smoke.sh`), so a release can't ship an addon that doesn't build.

## Project Layout

```
addons/ouranos_foundry/ # The addon: everything consumers get
  Features/<Name>/      # Self-contained vertical slice: Nodes/, Resources/, Types/, Utils/, README.md
  Core/                 # Shared across features: Attributes/, Types/, Utils/
  Assets/               # Editor icons
  CHANGELOG.md
Demos/<Name>/           # Runnable demo scenes (not library code, not shipped to consumers)
Templates/Feature/      # Starting point for new features (not shipped)
Tests/                  # GdUnit4 test project; suites live in Tests/Suites/ (mirrors source)
```

## Commands

```sh
Automation/setup.sh                                                       # one-time: tools + git hooks
dotnet build OuranosFoundry.sln
GODOT_BIN=/path/to/Godot dotnet test Tests --settings Tests/.runsettings   # all tests
./Automation/format.sh                                                    # format everything
```

Suites that don't create Godot objects run without `GODOT_BIN`.
