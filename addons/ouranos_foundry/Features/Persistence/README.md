# Persistence

Save slots for any game state. Anything that implements `ISaveable` can be saved: Foundry nodes such as `HealthComponent` and `WorldGenerator`, or your own nodes, resources and plain C# objects. `SaveManager` collects them, writes one file per slot and loads them back.

## Pieces

| Type | Kind | Role |
|------|------|------|
| `ISaveable` | interface (`Core/Types`) | `SaveKey`, `Save()` → `Dictionary<string, Variant>`, `Load(data)`. It lives in Core, so any feature can implement it without referencing this one. |
| `SaveManager` | Node | Saves and loads slots: `Save(slot, metadata?)`, `Load(slot)`, `Restore(saveable)`, `ClearData()`, `HasSave`, `DeleteSave`, `GetSlots`, `GetSlotInfo`, `GetSlotPath`. |
| `SaveSlotInfo` | record | A slot's header: `Version`, `SavedAt` (UTC) and the `Metadata` passed to `Save`. |

## Usage

1. Add a `SaveManager`: as an autoload (*Project Settings → Globals → Autoload*, pointing at `SaveManager.cs`), or as a node in your main scene.
2. Make things saveable:
   - **Nodes:** add them to the `saveable` group (`SaveManager.SaveGroup`) in the editor's *Node → Groups* tab. `HealthComponent` and `WorldGenerator` already implement `ISaveable`.
   - **Anything else** (a plain C# quest log, a settings resource): call `saveManager.Register(obj)`, and `Unregister` when it goes away.
3. Call `saveManager.Save("slot1")` and `saveManager.Load("slot1")`. Both return false and emit `SaveFailed`/`LoadFailed` on failure; on success they emit `Saved`/`Loaded`.

```csharp
public partial class Inventory : Node, ISaveable
{
    public string SaveKey => "inventory";   // "" on a node means "use my node path"

    public Godot.Collections.Dictionary<string, Variant> Save()
    {
        return new() { ["gold"] = Gold, ["items"] = ItemIds.ToArray() };
    }

    public void Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        Gold = data["gold"].AsInt32();
        ItemIds = [.. data["items"].AsStringArray()];
    }
}
```

## Semantics

- **Keys.** Each saveable's data is stored under its `SaveKey`, which must be unique and stable across runs. A node with an empty `SaveKey` is keyed by its scene-tree path, which is stable for nodes placed in a scene but not for nodes spawned at runtime with generated names; give those an explicit key. When two saveables share a key, the first is saved and an error is pushed for the second.
- **Data.** `Save()` returns Variant-compatible values: numbers, strings, vectors, colours, packed arrays, and arrays and dictionaries of them. Godot objects and resources aren't saved. `Load` gets every value back with its type intact (an `int` stays an `int`, a `Vector2I` stays a `Vector2I`). Throw from `Load` when the data can't be used.
- **Loading.** `Load` restores every saveable that has an entry in the slot and leaves the others alone. When a saveable throws, the others still load; `LoadFailed` then lists each failure as `key: message`. A slot that is missing, corrupt or from a newer version loads nothing.
- **Absent saveables.** The manager keeps the data of the last save or load. An entry whose saveable isn't present (it belongs to another level, or hasn't spawned yet) is carried into the next save rather than dropped. Call `Restore(saveable)` to load a saveable that appears later, e.g. from a level's `_Ready`, and `ClearData()` when a new game starts.
- **Atomic writes.** A slot is written to `<slot>.json.tmp`, flushed to disk and renamed over the old file, so a crash mid-save leaves the previous save intact. A failed save writes nothing.
- **Files.** Slots are `SaveDirectory/<slot>.FileExtension` (default `user://saves/<slot>.json`). A slot name must be a valid file name.

## File format and versions

```
{ format, version, saved_at, metadata: {…}, data: { <SaveKey>: { … }, … } }
```

- `format` is the layout of the file itself, owned by Foundry. `version` is your game's data version, `SaveManager.Version` (default 1).
- **Migrations:** when you change the shape of saved data, raise `Version` and override `Migrate(data, fromVersion)` in a `SaveManager` subclass. It runs on every save older than `Version`, before anything is loaded; `data` maps each SaveKey to its entry. A save newer than `Version` fails to load.
- **Encoding:** the default is JSON through Godot's `JSON.from_native`, which records each value's type (`"i:42"`, `{"type": "Vector2i", "args": [1, 2]}`). Override `Encode`/`Decode` for another format, such as compressed or encrypted bytes.
- `metadata` holds whatever you pass to `Save(slot, metadata)` (location, play time, …). Read it for a load menu with `GetSlotInfo(slot)`, alongside `SavedAt`.

## Extending

- **Any state:** implement `ISaveable`. Features implement it from Core and never reference this feature.
- **Migrations and formats:** subclass `SaveManager` and override `Migrate`, `Encode` or `Decode`.
- **Foundry features:** `HealthComponent` saves its current health (`Serialize`/`Deserialize`, both virtual). `WorldGenerator` saves the procedural world blueprint and the tiles changed in its chunks; see the ProceduralGeneration README.
