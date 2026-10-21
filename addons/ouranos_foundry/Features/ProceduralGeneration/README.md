# ProceduralGeneration

Seeded world generation in two stages: a **blueprint** of the whole world (regions, cities, roads) is generated once, then **chunks** are generated on demand as the player moves.

## Scene shape

```
WorldGenerator              entry point; owns Config and WorldState, runs the stages
├── BlueprintPipeline       discovers BlueprintPass children in _Ready
│   ├── RegionBlueprintPass
│   ├── CityBlueprintPass   [RequiresPasses(Region)]
│   └── RoadBlueprintPass   [RequiresPasses(Region, City)]
├── GenerationPipeline      discovers GenerationPass children in _Ready
│   ├── TerrainGenerationPass
│   └── ...                 City/Road/PoiPlacement: [RequiresPasses(Terrain)]
└── ChunkManager            tracks the player; requests chunk load/unload
```

Child order is execution order. Add, remove or reorder passes in the scene tree.

## Pass types

| Base | Signature | Runs |
|------|-----------|------|
| `BlueprintPass` | `RunPass(GenerationContext ctx)` | Once, on the main thread, from `WorldGenerator.GenerateBlueprint()` |
| `GenerationPass` | `RunPass(GenerationContext ctx, ChunkBuffer chunk)` | Per chunk, **on a worker thread** (`Task.Run`); see [Threading](#threading) |

Write a new pass by subclassing one of these and adding it as a child of the matching pipeline. Concrete passes don't need `[Icon]`. The pipeline hands each pass a `GenerationContext`:

- `ctx.Config`: the `ProceduralGenerationConfig` assigned to `WorldGenerator.Config`. Passes have no config exports of their own.
- `ctx.Definitions`: a `GenerationDefinitions` snapshot of the config, built once per blueprint run: the biome, city size, POI type and road type definitions in config order (`Biomes`, …), case-insensitive lookups by Id that fall back to the type's `Default` (`GetBiome(id)`, `GetCitySize`, `GetPoiType`, `GetRoadType`), and the `Terrain`, `Naming` and `Generation` sub-configs (defaults when unset). `WorldGenerator` warns once per run about Ids the config refers to that no definition has.
- `ctx.World`: the `WorldState` being generated.
- `ctx.Rng`: a `SeededRng` of the pass's own (see Determinism below).
- `ctx.Log(message, level)`: logs only when `WorldGenerator.LogLevel` is at least `level`.

```csharp
[RequiresPasses(typeof(CityBlueprintPass))]
[GlobalClass]
public partial class RuinsBlueprintPass : BlueprintPass
{
    public override void RunPass(GenerationContext ctx)
    {
        foreach (var city in ctx.World.Cities)
        {
            if (ctx.Rng.NextDouble() < 0.1)
            {
                // ... record a ruin near the city
            }
        }

        ctx.Log("Placed ruins.", LogLevel.Verbose);
    }
}
```

## Threading

Blueprint passes run on the main thread. Generation passes run on worker threads, several chunks at once, so a `GenerationPass` follows these rules:

- **May read:** `ctx.World` (a blueprint never changes once generated: `ResetBlueprint()` replaces the `WorldState` instead of clearing it, so a cancelled job keeps reading the world it started with), `ctx.Definitions`, and plain C# fields and `[Export]` properties on the pass itself, on `ctx.Config` and on definition resources.
- **Writes only** to the `ChunkBuffer` it was given. `WorldGenerator` wraps the buffer in the `ChunkData` that `ChunkGenerated` emits and `GetChunk` returns, back on the main thread.
- **Must not:** read Godot collections (`Godot.Collections.Array`/`Dictionary`, such as `ctx.Config.BiomeDefinitions` or a definition's `CustomData`; use `ctx.Definitions`), touch the scene tree or Node members (`Name`, `GetParent()`, `GetNode()`), emit signals, or call `GD.*`.
- **Errors:** throw. The chunk fails with `GenerationFailed(coords, message)`, reported on the main thread.
- **Logging:** use `ctx.Log`. A chunk job's lines are buffered and printed on the main thread when the job finishes.
- **Randomness:** `ctx.Rng` is unique to the pass and the chunk (see Determinism below).

Chunk jobs use `Task.Run` rather than Godot's `WorkerThreadPool`, because it works directly with C# delegates and cancellation tokens.

## Rules

- **Ordering:** declare dependencies with `[RequiresPasses(typeof(OtherPass))]` from `Ouranos.Foundry.Core.Attributes`. Both pipelines fail on a violation without running any pass: `BlueprintPipeline` emits `PassFailed` (and `WorldGenerator` `BlueprintFailed`); `GenerationPipeline` pushes an error in `_Ready` and every chunk then fails with `GenerationFailed`.
- **Failing a pass:** throw (e.g. `InvalidOperationException`) instead of logging and returning. The blueprint run stops at the first failure: `BlueprintPipeline` emits `PassFailed`, and `GenerateBlueprint()` discards the partial blueprint (replacing the `WorldState`), emits `BlueprintFailed` and returns false. A failing generation pass emits `GenerationFailed` for that chunk.
- **Chunk jobs:** at most `MaxConcurrentChunkJobs` (default 4) chunks generate at once. Unloading a chunk mid-generation cancels it between passes and discards the result. `ResetBlueprint()` cancels all jobs, unloads every chunk (`ChunkUnloaded`) and replaces the `WorldState` with an empty one (same `Seed`, `Size` and `ChunkSizeTiles`); chunk requests are ignored until the blueprint is generated again.
- **Config:** assign one `ProceduralGenerationConfig` to `WorldGenerator.Config`. Without it, `GenerateBlueprint()` emits `BlueprintFailed` and no chunks generate.
- **Validation:** `GenerateBlueprint()` first calls `WorldGenerator.Validate()`, which collects `Config.Validate()` (each sub-config's and definition's `Validate()`) and `WorldState.Validate()`. If anything is reported, no pass runs: it emits `BlueprintFailed("WorldGenerator", messages)` with one line per problem, naming the resource, the field and the value (e.g. `GenerationConfig.MinCitiesPerRegion (5) is greater than MaxCitiesPerRegion (3).`). Errors are values the passes can't generate with: negative or zero counts and sizes, min above max, empty name lists or POI `NameOptions`, no biomes, and empty slots in a definition table. Settings that generation works around (an empty city size, POI type or road type table, or an Id no definition has) are pushed as warnings from `Config.GetWarnings()` instead. Degenerate ranges such as `RoadWaypointJitter = 0`, a city `Radius` below 2 or a biome `TerrainVariantCount` of 0 are valid. Every `Validate()` is `public virtual`, so a subclass can add checks for its own fields. Validation runs at runtime only: `WorldGenerator` is not a `[Tool]` script, because that would require every resource and node it exports, including your subclasses of them, to be `[Tool]` as well.
- **World bounds:** cities are clamped to `0..WorldState.Size - 1`. They can still land outside their own region's Voronoi cell when `MinRegionSize` is large relative to `MinRegionSeparation`.
- **Determinism:** a blueprint pass's `ctx.Rng` is `new SeededRng(World.Seed, RngSalt)`. A chunk pass's is `SeededRng.ForCoords(World.Seed, chunk.X, chunk.Y, RngSalt)`, a stream unique to (seed, pass, chunk), so chunks never share noise and a chunk regenerates identically. `RngSalt` is a `protected virtual` property that defaults to a stable hash of the pass's full type name, so **renaming or moving a custom pass changes its stream**; override it with a constant to pin it. Use `rng.Derive()` for sub-streams, and `SeededRng.ForCoords` for any other coordinate-keyed stream. Never use `System.Random` or `GD.Rand*`.
- **Chunk type:** `WorldGenerator` sets `chunk.Type` from the cities and roads overlapping the chunk before any pass runs.
- **Points of interest:** `ChunkData.PointsOfInterest` (and `ChunkBuffer.PointsOfInterest` in a pass) lists every blueprint POI whose position lies inside the chunk, whichever passes run. `WorldGenerator` fills it before the passes run; instantiate a scene per POI from `ChunkGenerated`. The spatial index includes each POI's chunk under its city, so a POI outside its city's radius is still found.
- **World coordinates:** a chunk's tile (`x`, `y`) is world tile `chunk.Coordinates * ChunkSizeTiles + (x, y)`. Base anything that must line up across chunk borders, such as the street grid, on world coordinates.
- **Chunk size:** `ChunkConfig` (on `ProceduralGenerationConfig`) is the single source of truth. `ChunkSizeTiles` (default 16) is the chunk width in tiles and `TilePixelSize` (default 1) the tile width in world units. `WorldGenerator` pushes them into `WorldState.ChunkSizeTiles`, `ChunkManager.SetChunkSize` and each `ChunkDescriptor.ChunkSizeTiles`; with no `ChunkConfig` the defaults apply. Passes read `chunk.Descriptor.ChunkSizeTiles`.
- **Chunk coordinates floor:** convert with `ChunkMath.WorldToChunk` (tiles) or `ChunkMath.PixelToChunk` (world units), never `/`, which truncates toward zero. With 16-tile chunks, tiles -16..-1 are chunk -1 and 0..15 are chunk 0.
- **Debug drawing:** `Nodes/Diagnostics/` contains a `BlueprintDebugOverlay` (Node2D) with `DebugDrawer` children, which draw from the `WorldState` and `WorldGenerator.Definitions`. Drawers call the protected `RequestRedraw()` to redraw their parent canvas. This is the one sanctioned child-to-parent call; see AGENTS.md.

## Signals (WorldGenerator)

`BlueprintGenerated`, `BlueprintFailed(passName, error)`, `ChunkGenerated(coords, chunk)`, `ChunkUnloaded(coords)`, `GenerationFailed(coords, error)`.
