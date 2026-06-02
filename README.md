# Ouranos.Foundry

Shared Godot C# library for building games. Features are self-contained vertical
slices that consuming projects can use independently.

## Quick Start

1. **Prerequisites:** Godot 4.4+, .NET 10 SDK, git-lfs
2. **Open** `project.godot` in the Godot editor
3. **Run** the ProceduralGeneration demo: press F5 (or F6 to run the main scene)
4. **New feature?** Copy `Features/_Template/` → `Features/YourName/`

## Project Layout

```
Features/<Name>/        # Each feature is a self-contained vertical slice
  Nodes/                # Godot node scripts ([GlobalClass], inspector-wired)
  Resources/            # Resource subclasses (data/config assets, wired in editor)
  Types/                # Plain C# types (records, enums, structs - no Godot base class)
  Utils/                # Static helper classes specific to this feature
Core/                   # Shared code used across multiple features
  Types/                # Shared types (SeededRng, LogLevel)
  Utils/                # Shared utilities (Bresenham, math helpers)
  Attributes/           # Shared attributes (RequiresPassesAttribute)
Demos/<Name>/           # Runnable demo scenes (not library code, not shipped to consumers)
Tests/<Name>/           # Per-feature test projects
```

## Conventions

- **All public nodes and resources** must use `[GlobalClass]` to appear in the Godot editor
- **Namespaces** mirror folder structure: `Ouranos.Foundry.Features.<Feature>.<Subdir>`
- **Pipeline pattern**: abstract base passes + concrete implementations wired as children in the scene tree
- **Signal up / call down**: parent nodes can invoke code in their children, but children can only emit signals
- **Node preference**: where possible, features should be constructed out of smaller independent nodes to promote reuse

## Commands

```sh
# Build
dotnet build ouranos-foundry.sln

# Format
dotnet tool restore --verbosity quiet
dotnet csharpier check .
dotnet format style ouranos-foundry.sln
dotnet format analyzers ouranos-foundry.sln
```
