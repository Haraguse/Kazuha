# Luminalium C# Migration — Documentation

Frozen migration traceability artifacts for the C# version, produced by plan Task 4
(`../../.omo/plans/csharp-fluentavalonia-migration.md` — "Freeze feature parity and legacy capability inventory").

## Documents

| Document | Contents |
|---|---|
| [FEATURE_PARITY_MATRIX.md](./FEATURE_PARITY_MATRIX.md) | Every README feature (F-01..F-17), every built-in plugin (P-01..P-08), and every core capability (C-01..C-13) mapped to source references, destination C# task numbers, acceptance criteria, negative cases, and evidence slugs; intentional exclusions X-02..X-07 and deferred item X-01 (Linux staged after Windows-first delivery) with confirmed rationale. |
| [LEGACY_CAPABILITY_INVENTORY.md](./LEGACY_CAPABILITY_INVENTORY.md) | `plugins/webview_runner.py` capability catalog (W-01..W-38: page lifecycle, window hosting, dialogs, crash UI, titlebar, timer, spotlight, settings, logs, onboarding, bridge protocol), legacy surface inventory (L-01..L-28), `Luminalium.spec` payload/exclusion map, and the Task 24 retirement map. |

## Summary (validated)

| Item | Count |
|---|---|
| In-scope README feature rows (F-01..F-17) | 17 |
| In-scope built-in plugin rows (P-01..P-08) | 8 |
| In-scope core capability rows (C-01..C-13) | 13 |
| **In-scope rows total** | **38** |
| Intentional exclusions (X-02..X-07) | 6 |
| Deferred / staged (X-01: Linux platform, after Windows-first delivery) | 1 |
| webview_runner capabilities cataloged (W-01..W-38) | 38 |
| Legacy surfaces cataloged (L-01..L-28) | 28 |
| README feature bullets covered | 9 / 9 |
| Built-in plugin manifests covered | 8 / 8 |

## Evidence

- `.omo/evidence/task-4-parity-matrix.txt` — row completeness and count validation.
- `.omo/evidence/task-4-parity-negative.txt` — exclusion scan (forbidden legacy terms never appear as in-scope deliverables).
- [evidence/task-3-solution-happy.md](./evidence/task-3-solution-happy.md) — cross-platform library/Windows host restore, build, test, and ignore-rule evidence.
- [evidence/task-3-solution-negative.md](./evidence/task-3-solution-negative.md) — forbidden dependency and target-boundary gate evidence.
- [evidence/task-5-ci-happy.md](./evidence/task-5-ci-happy.md) — Windows-first restore, publish, payload scan, and deterministic ZIP evidence.
- [evidence/task-5-ci-negative.md](./evidence/task-5-ci-negative.md) — malformed metadata, forbidden payload, missing artifact, and deterministic-package negative paths.

## Blocked tasks

Tasks 11, 17-19, 21, 24-25 depend on this artifact (plan dependency matrix, Task 4 row).
