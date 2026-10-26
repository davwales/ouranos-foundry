# Templates

Templates for every issue and pull request in this repository.

| Template | Use for |
|----------|---------|
| [issues/feature.md](issues/feature.md) | New capability or behaviour change |
| [issues/bug.md](issues/bug.md) | Something broken |
| [issues/tech-debt.md](issues/tech-debt.md) | Cleanup with no user-facing change |
| [pull-request.md](pull-request.md) | Every pull request |

## Conventions

- **Issue titles:** `<Area> | <Title>`, where Area is `Core`, a feature name (`Health`, `LootTables`, `Persistence`, `ProceduralGeneration`, …) or `Tooling`.
- **Labels:** the area label (`core`, `feature:<name>`, `infra`) where one exists, the type (`bug`, `enhancement`, `refactor`) and a priority (`priority:P1`–`P3`).
- **PR titles:** Conventional Commits, `<type>[!]: <summary>`. They become the release notes, so mark breaking API changes with `!`.
- **Keep it short.** A few bullets per section; skip anything the diff or code already says.
