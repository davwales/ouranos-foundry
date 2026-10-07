# Ouranos.Foundry

Shared Godot C# library for building games. Features are self-contained vertical slices that consuming projects can use independently.

## Quick Start

1. **Prerequisites:** Godot 4.6.x (.NET build), .NET 10 SDK, git-lfs
2. **Set up** the clone: `Automation/setup.sh` (restores .NET tools and installs the git hooks)
3. **Open** `project.godot` in the Godot editor
4. **Run** a demo: open a scene under `Demos/` and press F6, or press F5 for the main scene
5. **New feature?** Copy `Features/_Template/` → `Features/YourName/` and follow its `_README.md`

## Features

| Feature | Summary |
|---------|---------|
| [Health](Features/Health/README.md) | Health component with HUD bar, hearts, floating and grouped UI monitors |
| [LootTables](Features/LootTables/README.md) | Weighted, deterministic, nestable loot tables as Godot resources |
| [ProceduralGeneration](Features/ProceduralGeneration/README.md) | Seeded world blueprint and on-demand chunk generation pipeline |

## Project Layout

```
Features/<Name>/        # Self-contained vertical slice: Nodes/, Resources/, Types/, Utils/, README.md
Core/                   # Shared across features: Attributes/, Types/, Utils/
Demos/<Name>/           # Runnable demo scenes (not library code, not shipped to consumers)
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
