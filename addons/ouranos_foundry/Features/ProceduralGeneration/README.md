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
- **Ordering:** declare dependencies with `[RequiresPasses(typeof(OtherPass))]` from `Ouranos.Foundry.Core.Attributes`. `BlueprintPipeline` fails the run on a violation without running any pass. `GenerationPipeline` pushes an error in `_Ready`.
- **Failing a pass:** throw (e.g. `InvalidOperationException`) instead of logging and returning. The blueprint run stops at the first failure: `BlueprintPipeline` emits `PassFailed`, and `GenerateBlueprint()` clears the partial blueprint, emits `BlueprintFailed` and returns false. A failing generation pass emits `GenerationFailed` for that chunk.
- **Chunk jobs:** at most `MaxConcurrentChunkJobs` (default 4) chunks generate at once. Unloading a chunk mid-generation cancels it between passes and discards the result. `ResetBlueprint()` cancels all jobs, unloads every chunk (`ChunkUnloaded`) and clears the `WorldState`; chunk requests are ignored until the blueprint is generated again.
- **Determinism:** each pass creates `new SeededRng(worldState.Seed, RngSalt)` with its own `RngSalt` constant. Use `rng.Derive()` for sub-streams. Never use `System.Random` or `GD.Rand*`.
- **Chunk size:** `ChunkConfig` (on `ProceduralGenerationConfig`) is the single source of truth. `ChunkSizeTiles` (default 16) is the chunk width in tiles and `TilePixelSize` (default 1) the tile width in world units. `WorldGenerator` pushes them into `WorldState.ChunkSizeTiles`, `ChunkManager.SetChunkSize` and each `ChunkDescriptor.ChunkSizeTiles`; with no `ChunkConfig` the defaults apply. Passes read `chunk.Descriptor.ChunkSizeTiles`.
- **Chunk coordinates floor:** convert with `ChunkMath.WorldToChunk` (tiles) or `ChunkMath.PixelToChunk` (world units), never `/`, which truncates toward zero. With 16-tile chunks, tiles -16..-1 are chunk -1 and 0..15 are chunk 0.
- **Debug drawing:** `Nodes/Diagnostics/` contains a `BlueprintDebugOverlay` (Node2D) with `DebugDrawer` children. Drawers call the protected `RequestRedraw()` to redraw their parent canvas. This is the one sanctioned child-to-parent call; see AGENTS.md.

## Signals (WorldGenerator)

`BlueprintGenerated`, `BlueprintFailed(passName, error)`, `ChunkGenerated(coords, chunk)`, `ChunkUnloaded(coords)`, `GenerationFailed(coords, error)`.
