# ProceduralGeneration

Seeded world generation in two stages: a **blueprint** of the whole world (regions, cities, roads) is generated once, then **chunks** are generated on demand as the player moves.

## Scene shape

```
WorldGenerator              entry point; owns WorldState, runs the stages
├── BlueprintPipeline       discovers BlueprintPass children in _Ready
│   ├── RegionBlueprintPass
│   ├── CityBlueprintPass   [RequiresPasses(Region)]
│   └── RoadBlueprintPass   [RequiresPasses(Region, City)]
├── GenerationPipeline      discovers GenerationPass children in _Ready
│   ├── TerrainGenerationPass
│   └── ...
└── ChunkManager            tracks the player; requests chunk load/unload
```

Child order is execution order. Add, remove or reorder passes in the scene tree.

## Pass types

| Base | Signature | Runs |
|------|-----------|------|
| `BlueprintPass` | `RunPass(WorldBlueprint)` | Once, on the main thread, from `WorldGenerator.GenerateBlueprint()` |
| `GenerationPass` | `RunPass(WorldBlueprint, ChunkData)` | Per chunk, **on a worker thread** (`Task.Run`) |

Write a new pass by subclassing one of these and adding it as a child of the matching pipeline. Concrete passes don't need `[Icon]`.

## Rules

- **Threading:** a `GenerationPass` must not touch the scene tree or any non-thread-safe Godot API. Treat `WorldBlueprint` as read-only and write only to the `ChunkData` you were given. `WorldGenerator` emits `ChunkGenerated` back on the main thread.
- **Ordering:** declare dependencies with `[RequiresPasses(typeof(OtherPass))]` from `Ouranos.Foundry.Core.Attributes`. `BlueprintPipeline` fails the run (`PassFailed` signal) on a violation. `GenerationPipeline` pushes an error in `_Ready`.
- **Determinism:** each pass creates `new SeededRng(worldState.Seed, RngSalt)` with its own `RngSalt` constant. Use `rng.Derive()` for sub-streams. Never use `System.Random` or `GD.Rand*`.
- **Debug drawing:** `Nodes/Diagnostics/` contains a `BlueprintDebugOverlay` (Node2D) with `DebugDrawer` children. Drawers call the protected `RequestRedraw()` to redraw their parent canvas. This is the one sanctioned child-to-parent call; see AGENTS.md.

## Signals (WorldGenerator)

`BlueprintGenerated`, `ChunkGenerated(coords, chunk)`, `ChunkUnloaded(coords)`, `GenerationFailed(coords, error)`.
