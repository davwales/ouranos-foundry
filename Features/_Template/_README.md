# Template Feature

Copy this folder and rename it to create a new feature:

```sh
cp -r Features/_Template Features/YourFeatureName
```

Then:
1. Rename all `Template*` classes to your feature name
2. Update namespaces (`_Template` → `YourFeatureName`)
3. Add `[Icon]` attributes to public nodes
4. Wire up a demo scene under `Demos/YourFeatureName/`
