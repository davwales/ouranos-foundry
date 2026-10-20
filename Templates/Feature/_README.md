# Template Feature

Copy this folder and rename it to create a new feature:

```sh
cp -r Templates/Feature addons/ouranos_foundry/Features/YourFeatureName
```

Then:
1. Rename the `Template*` classes after their role (`*Component`, `*Manager`, `*Definition`, …). Don't use a `*Node` suffix.
2. Update namespaces (`_Template` → `YourFeatureName`) so they mirror the folder path under `addons/ouranos_foundry/`. Keep the `#nullable enable` line and the explicit `using` directives in every file: the addon compiles in consumer projects that have neither nullable nor global usings enabled.
3. Add `[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]` to public nodes that users add to scenes.
4. Delete the copied `.uid` files and let Godot regenerate them (open the project, or run a headless import). Never hand-write UIDs.
5. Add a `README.md` covering the feature's pieces, usage and extension points.
6. Add tests under `Tests/Suites/Features/YourFeatureName/`. Mark a suite `[RequireGodotRuntime]` only if it creates Godot objects.
7. Wire up a demo scene under `Demos/YourFeatureName/`.
8. Add the feature to the feature tables in `README.md` and `addons/ouranos_foundry/README.md`, and add an entry to `addons/ouranos_foundry/CHANGELOG.md`.
