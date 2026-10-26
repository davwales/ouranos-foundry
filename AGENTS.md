# AGENTS.md: Ouranos.Foundry

Guidance for anyone changing this repository, whether an AI coding agent or a human contributor. It documents the conventions that tooling enforces or reviewers expect but that you can't infer from a single file. For a project overview and quick start, see `README.md`. Per-feature details live in `addons/ouranos_foundry/Features/<Name>/README.md`.

## Purpose & Principles

Ouranos.Foundry is a Godot 4.6 C# library of reusable game features. It must work for any genre (fantasy, sci-fi, …) and any presentation: first-person RPG, isometric ARPG, CRPG, idle, and more. Developer experience comes first, because a feature is only useful if developers want to use it.

- **Works with zero config.** Every export has a sensible default, and a feature does something useful when dropped into a scene.
- **Extensible from a consumer assembly.** Extension points are `public`/`protected` `virtual` or `abstract`, never `internal`. Extend through signals, virtual methods, abstract base passes/entries, or provider delegates (e.g. `HealthComponent.MaxHealthProvider`).
- **Genre- and perspective-agnostic.** Provide 2D and 3D variants where presentation differs (`FloatingHealthBar2D`/`3D`), and don't hard-code genre concepts.

## Commands

```sh
Automation/setup.sh                                   # one-time: dotnet tool restore + core.hooksPath=Automation
dotnet build OuranosFoundry.sln                       # always pass the .sln explicitly
GODOT_BIN=/path/to/Godot dotnet test Tests --settings Tests/.runsettings   # all tests (GdUnit4)
./Automation/format.sh                                # CSharpier + dotnet format style + analyzers
```

- **Requirements:** a Godot 4.6.x .NET editor, the .NET 10 SDK and git-lfs.
- **Use a 4.6.x editor.** A newer editor rewrites `Godot.NET.Sdk/<version>` in `.csproj` files and leaves `*.csproj.old` backups. Revert that if it happens.
- **`GODOT_BIN`** must point at the Godot .NET executable itself, e.g. `/Applications/Godot_mono.app/Contents/MacOS/Godot` on macOS. Without it, only the engine-free suites can run (see [Testing](#testing)).
- **On a fresh clone,** import the test project once before running tests so `Tests/.godot/` exists: `dotnet build Tests && "$GODOT_BIN" --path Tests --headless --build-solutions --quit-after 200` (CI does the same).
- **Hooks** run from `Automation/` via `core.hooksPath`, which `Automation/setup.sh` sets.
- **Before finishing a task:** build the solution, run the full test suite with `GODOT_BIN` set (so the Godot-runtime suites run too), and run `./Automation/format.sh`.

## Architecture

- **The addon is the product.** Everything under `addons/ouranos_foundry/` is shipped to consumers, as the `addon` branch and the release zip. Everything else (`Demos/`, `Templates/`, `Tests/`, the root project) is development-only. Paths below such as `Features/` and `Core/` are relative to the addon folder.
- **Feature slices.** Every feature under `Features/<Name>/` is a self-contained vertical slice.
  - Features must not reference each other. Shared code lives in `Core/`.
  - When promoting code to Core, **move** it: delete the feature copy and update every reference in the same change. Never leave duplicates.
- **Demo isolation.** `Demos/` is non-library code and isn't shipped to consumers. Demo scripts wire features up through `[Export]`s and public APIs. They never modify feature code.
- **Node composition.** Build features from small, independent nodes wired in the scene tree. Avoid monolithic controller nodes.
- **Threading.** Anything run through `Task.Run` (e.g. `GenerationPass.RunPass`) must not touch the scene tree or non-thread-safe Godot APIs. Hand results back to the main thread before emitting signals or adding nodes.

## Project Layout

```
addons/ouranos_foundry/   Shipped addon (keep this path: icons are referenced by res:// path)
  Features/<Name>/
    Nodes/        Node subclasses only ([GlobalClass], partial)
    Resources/    Resource subclasses ([GlobalClass], partial)
    Types/        Plain C# types: records, enums, structs (no Godot base class)
    Utils/        Static helpers and plain helper classes
    README.md     Pieces, usage, extension points
  Core/           Shared, feature-agnostic code (e.g. SeededRng, LogLevel, Bresenham, StableId)
    Attributes/
    Types/
    Utils/
  Assets/icons/
  LICENSE, README.md
Demos/<Name>/{Scenes,Scripts,Resources}/
Templates/Feature/        Copy to start a new feature (compiled by the root project, not shipped)
Tests/                    GdUnit4 test project (own project.godot)
  Suites/         Test sources; mirrors source (Suites/Core/…, Suites/Features/<Name>/…)
```

- Subfolders under `Nodes/` and `Resources/` are allowed and produce nested namespaces (`Nodes/Generation/` → `…Nodes.Generation`).
- New features start by copying `Templates/Feature/`; follow its `_README.md`.
- **Never name a source folder `Debug`, `Release`, `bin`, `obj`, `x64`, `x86` and so on.** `.gitignore` silently drops them.

## Namespaces & Naming

- **File-scoped namespaces** (`csharp_style_namespace_declarations = file_scoped:warning`).
- **The namespace mirrors the folder path relative to `addons/ouranos_foundry/`:** `Features/LootTables/Resources/` → `Ouranos.Foundry.Features.LootTables.Resources`. There is one exception: `Demos/<Name>/Scripts/` → `Ouranos.Foundry.Demos.<Name>`.
- **Test namespaces:** `Ouranos.Foundry.Tests.<mirrored path>`, leaving out `Suites`. For example, `Tests/Suites/Core/Types/` → `Ouranos.Foundry.Tests.Core.Types`.
- **One top-level type per file, named after the file.** Private nested helper types are fine.
- **Name types by role.** Don't use a `*Node` or `*Helper` suffix.
  - Nodes: `*Component`, `*Manager`, `*Bar`, `*Container`, `*Drawer`, `*Generator`.
  - Pipelines: `*Pass` and `*Pipeline`.
  - Resources: `*Definition`/`*Config` for configuration, `*Data` for data containers (`LootTableData`, `ChunkData`).
  - Static utilities: name them by what they do (`Bresenham`, `WeightedSelection`).
- **Godot global class names must be unique across the assembly.** Godot's registry is flat.
- **Records are immutable DTOs:** `public sealed record City(Guid Id, string Name, …)`. Expose collections as `IReadOnlyList<T>`.

## Godot Integration

- **`[GlobalClass]` on every public Node/Resource subclass**, abstract bases included. Demo scripts are exempt.
- **`[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]`** goes on public nodes that users add to scenes directly. Dynamically discovered children (concrete passes, drawers) may omit it.
- **Exports:**
  - Nodes use `[Export] public T X { get; private set; }`. The inspector and scene loader still set private setters.
  - State a consumer may change at runtime gets a public method, not a public setter (`HealthComponent.SetInvulnerable`, `LootTableRoller.SetTable`).
  - Resources use `[Export] public T X { get; set; }`, because they're data containers.
  - Export properties, never fields.
  - A setter may have a body, e.g. `private set { field = value; RequestRedraw(); }`.
- **Exported collections:**
  - `List<T>` can't be exported.
  - Use `Godot.Collections.Array<T>` / `Godot.Collections.Dictionary<TKey, TValue>` for Resource and Node references.
  - `T[]` of Variant-compatible primitives (e.g. `string[]`) is fine.
- **Signals:**
  - Declare them as `[Signal] public delegate void EventNameEventHandler(...);`.
  - Emit with `EmitSignal(SignalName.EventName, ...)`.
  - Subscribe with C# `+=` in `_Ready` (or a `Monitor…` method) and unsubscribe with `-=` in `_ExitTree`. Always unsubscribe lambdas.
- **Signal up, call down.** Parents call methods on their children, and children communicate upward only through signals.
  - The one sanctioned exception: a `DebugDrawer` subclass may ask its parent canvas to redraw through the protected `RequestRedraw()` helper.
  - Reading `GetParent().Name` for display is fine.
- **Editor tools.** Don't add `[Tool]` unless the runtime logic is guarded with `Engine.IsEditorHint()`.

### Scene, resource & UID files

- **Never hand-write UIDs** (`uid://…`). Godot generates them.
  - After adding or renaming `.cs` files, open the project in a 4.6 editor or run `godot --headless --import` so the `.uid` files are generated.
  - Commit each `.uid` alongside its `.cs` file.
- **Prefer editing `.tscn`/`.tres` in the editor.** When hand-editing, keep `ext_resource` ids, `uid`s and `path`s consistent, and update paths when moving scripts.
- **Don't commit incidental `project.godot` changes** (e.g. `run/main_scene` from running a different demo).

## Code Style

Tooling enforces the following:

| Rule | Where |
|------|-------|
| Whitespace and line width | CSharpier (pre-commit, CI) |
| File-scoped namespaces, block-bodied methods, braces everywhere, `is null`, no unused usings | `.editorconfig`: build (`EnforceCodeStyleInBuild`), `dotnet format` (pre-commit, CI) |

- **Expression bodies:** expression-bodied **properties** are fine; **methods** are not.
- **Null checks:** use `is null` / `is not null`, never `== null`.
- **Modern C# is welcome:** primary constructors, collection expressions (`[]`, `[.. x]`) and C# 14 `field`-backed properties.
- **No global usings.** Every file declares its own `using` directives (`System`, `System.Collections.Generic`, `System.Linq`, `Godot`, …). The addon compiles inside consumer assemblies, so a global using would either be missing there or leak into the consumer's code. `dotnet format` removes unused ones.
- **`#nullable enable`** goes at the top of every file under `addons/`, so that annotations work in consumer projects that don't enable nullable.
- **XML doc comments are optional.** Use them for non-obvious behaviour, constraints or algorithmic intent, and escape `<`/`>` as `&lt;`/`&gt;`.
- **Nullable** is enabled in every project; addon files also carry `#nullable enable` (see above).

### Errors & logging

- **Misconfiguration** (missing resource, invalid value): use `GD.PushError(...)` plus a safe fallback (an empty result or early return). Never throw from Godot lifecycle methods.
- **Recoverable or suspicious state:** use `GD.PushWarning(...)`.
- **Library code doesn't spam `GD.Print`.** Verbose output goes through a `LogLevel`-gated logger (see `WorldGenerator`).

## Testing

There is one test project: `Tests/OuranosFoundry.Tests.csproj`, using GdUnit4Net and Shouldly.

- **Engine-free suites** (pure Types, Utils, Core logic) are marked `[TestSuite]` and run in-process. They don't need Godot or `GODOT_BIN`.
- **Suites that create Godot objects** (`new SomeNode()`, `new SomeResource()`, signals) also need `[RequireGodotRuntime]`. They run inside Godot, so `GODOT_BIN` must point at a Godot .NET executable. Creating a Godot object outside the engine crashes the test host.
- Mark each test `[TestCase]`. Assertions use Shouldly.
- **How the project works:**
  - Its build symlinks `addons/` into `Tests/` and compiles it into the test assembly. Internals are therefore visible and `res://` paths match.
  - Its own `project.godot` makes the main editor skip `Tests/`.
  - Godot generates `.uid` files for test scripts; these are gitignored.
- **Configuring exports:** use `node.With(Type.PropertyName.X, value)` (`Tests/Suites/GodotObjectTestExtensions.cs`). It goes through Godot's property system, as the scene loader does, so `private set` stays private.
- **Free the nodes you create** (`root.Free()`), in every test. Leaked nodes holding C# signal handlers made the Godot test host crash on shutdown (exit 134/139) on some CI hosts, failing green runs. Godot's "ObjectDB instances leaked at exit" warning means a test is missing a `Free()`.
- **Naming:**
  - Test classes are `{ClassName}Tests`.
  - Test methods are `{Method}_{Scenario}_{ExpectedOutcome}`, e.g. `Roll_WhenEntriesAreEmpty_ReturnsEmpty`. Older tests use a `Should…` prefix, which is fine.
- **Structure:** use Arrange/Act/Assert with `// Arrange`, `// Act`, `// Assert` comments, plus private factory helpers (`MakeTable()`, `CreateComponent()`).
- **Test behaviour, not conventions.** Conventions are enforced by review and this document, not by tests.
- **Mocking:** NSubstitute is preferred over Moq, if mocking is ever needed.

## Git, Hooks & CI

- **Branches:** `feat/…`, `fix/…`, `refactor/…`, `ci/…`. PRs target `main` on GitHub (`gh`).
- **Commits:** Conventional Commits (`feat:`, `fix:`, `refactor:`, `ci:`, `test:`, `docs:`).
- **Never bypass hooks** with `--no-verify`.

| Stage | What runs |
|-------|-----------|
| `pre-commit` | CSharpier check → `dotnet format style --verify-no-changes` → `dotnet format analyzers --verify-no-changes`. Fix failures with `./Automation/format.sh`. |
| `pre-push` | `dotnet build OuranosFoundry.sln` → Git LFS pre-push (only when `.gitattributes` tracks LFS files) |
| CI (`.github/workflows/ci.yml`, on PRs and as part of Release) | lint → build → test (GdUnit4 under headless Godot 4.6.3) and package (`package.yml`: subtree split + zip, each installed into a generated game by `Automation/consumer-smoke.sh`) |
| Release (`.github/workflows/release.yml`, on every push to `main`) | CI → if the addon or Godot version changed: `Automation/next-version.sh` → tag `v…`, push the `addon` branch (`git subtree split`) and `addon-v…` tag → GitHub release with generated notes and the addon zip |

To check the packaged addon locally: `GODOT_BIN=… Automation/consumer-smoke.sh zip <zip>`, or `… submodule <repo> [branch]` (needs `git subtree` to create the branch).

### Versioning & releases

- **Versions are `<Godot version>.<N>`** (e.g. `4.6.3.0`), and nobody sets them by hand. Merging to `main` releases automatically: `Automation/next-version.sh` takes the Godot version from `Godot.NET.Sdk/x.y.z` in `OuranosFoundry.csproj` and bumps `N` past the highest `v<godot>.*` tag, starting at 0 for a new Godot version.
- **Upgrading Godot** means changing every pin together: both `.csproj` files and `setup-godot` in `ci.yml` and `package.yml`. `next-version.sh` fails the release if they disagree.
- **Release notes are generated from PR titles,** so give PRs Conventional Commit titles. Mark breaking API changes with `!` (`feat!: …`), because the version number can't express them.
- **Pushes that don't touch `addons/ouranos_foundry/`** (docs, demos, CI) don't publish a release, unless the Godot version changed.
- **Never create release tags or commit to the `addon` branch by hand.** The release workflow owns them.

**Git LFS:** binary assets (png, jpg, ogg, wav, glb, fbx, ttf, …) are tracked through `.gitattributes`. Make sure `git lfs install` has been run.

## Domain Patterns

### RNG (all features)
- Use `new SeededRng(seed, salt)` (`Core/Types/SeededRng.cs`) for all randomness, with a named salt, not a magic number. Procgen passes override `public virtual ulong RngSalt` (`public override ulong RngSalt => 2000;`) and use `ctx.Rng`.
- Use `SeededRng.ForCoords(seed, x, y, salt)` for a stream keyed by coordinates (e.g. per chunk).
- Call `rng.Derive()` to create independent sub-streams for nested or parallel work.
- Never use `System.Random`, `GD.Randi()`/`GD.Randf()` or `RandomNumberGenerator` in features. Demos may seed from the clock.

### Per-feature
- **Loot tables:** `Features/LootTables/README.md`. Covers the entry hierarchy, `IsEligible`/`Resolve` overrides, and the depth and cycle limits.
- **Procedural generation:** `Features/ProceduralGeneration/README.md`. Covers the two pass types, `[RequiresPasses]`, and threading.
- **Health:** `Features/Health/README.md`. Covers the component, UI monitors, `MaxHealthProvider` and serialization.
- **Persistence:** `Features/Persistence/README.md`. Covers `ISaveable` (in Core, so any feature can implement it), `SaveManager` slots, the file format and migrations.

## Build & Dependencies

- **SDK:** `Godot.NET.Sdk/4.6.3` targeting `net10.0`, with `EnableDynamicLoading` (plugin assembly).
- **The main project excludes `Tests/**` from compilation.** The test project compiles library sources itself, so no `InternalsVisibleTo` is needed.
- **.NET tools** (CSharpier) are pinned in `.config/dotnet-tools.json`.

## Don'ts

- Don't hand-write UIDs or leave `.cs` files without a generated `.uid`.
- Don't duplicate a type between Core and a feature, or across features.
- Don't use `internal set` (or public `set`) on Node exports, and don't make extension points `internal`.
- Don't reference one feature from another feature, or a feature from Core.
- Don't use `System.Random` or `GD.Rand*` in features.
- Don't create Godot objects in a suite without `[RequireGodotRuntime]`.
- Don't use `--no-verify`, and don't commit `*.csproj.old` or incidental `project.godot` churn.
- Don't add global usings, and don't put demo, test or template code under `addons/ouranos_foundry/`.
