# Changelog

All notable changes to Ouranos.Foundry are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

Versions are `<Godot version>.<N>`: the first three parts are the Godot version the release was built and tested against, and `N` counts Foundry releases for that Godot version (restarting at 0 when the Godot version changes). Because the version carries no semver signal for Foundry's own API, breaking changes are prefixed with **BREAKING**.

## [Unreleased]

## [4.6.3.0]

### Added
- Health: `HealthComponent` with `PlayerHealthBar`, `HeartsContainer`, `FloatingHealthBar2D`/`3D` and `GroupedHealthManager` monitors.
- LootTables: weighted, deterministic, nestable loot tables as Godot resources.
- ProceduralGeneration: seeded world blueprint and on-demand chunk generation pipeline.
- Packaged as a Godot addon under `addons/ouranos_foundry/`, published as an `addon` branch and a release zip.
