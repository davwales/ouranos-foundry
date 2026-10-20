# Template Feature

Copy this folder and rename it to create a new feature:

```sh
cp -r Features/_Template Features/YourFeatureName
```

Then:
1. Rename the `Template*` classes after their role (`*Component`, `*Manager`, `*Definition`, …). Don't use a `*Node` suffix.
2. Update namespaces (`_Template` → `YourFeatureName`) so they mirror the folder path.
3. Add `[Icon("res://Assets/icons/foundry_node.svg")]` to public nodes that users add to scenes.
4. Delete the copied `.uid` files and let Godot regenerate them (open the project, or run a headless import). Never hand-write UIDs.
5. Add a `README.md` covering the feature's pieces, usage and extension points.
6. Add tests under `Tests/Suites/Features/YourFeatureName/`. Mark a suite `[RequireGodotRuntime]` only if it creates Godot objects.
7. Wire up a demo scene under `Demos/YourFeatureName/`.
