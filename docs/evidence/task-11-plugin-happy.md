# Task 11 - Built-in Plugin Contract & Registry (positive evidence)

Branch: `RyouYamada`

## Contract

- `Luminalium.Plugins` (net10.0, cross-platform) defines the split contract:
  - `PluginMetadata` (Id / DisplayName / PluginType / IconKey / Description / Version)
  - `IPluginCommand` (`ExecuteAsync(PluginContext, CancellationToken)` -> typed `PluginExecutionResult`)
  - `IPluginViewFactory` (`Avalonia.Controls.Control CreateView()`)
  - `PluginContext`: bounded context with cancellation token + `IPluginHost` self-termination surface only. No app/window object.
  - Metadata + commands are usable without views (extension seam for future non-UI consumers).
- `BuiltInPluginRegistry`: registration order deterministic, duplicate id => typed `DuplicateRegistrationError` keeping the original active, `TerminateAllAsync` isolates per-plugin failures in an aggregate result.
- `BuiltInPluginCatalog.CreateDefaultRegistry()` is the single registration authority; no directory scanning, no Python import.

## Catalog (verified against legacy manifests)

| # | Id | DisplayName | Type | IconKey |
|---|---|---|---|---|
| 1 | settings | (empty by design) | Toolbar | settings.svg |
| 2 | onboarding | Onboarding | Window | (empty) |
| 3 | board | 板中板 - Luminalium | Window | board-in-board.svg |
| 4 | timer | Timer | Toolbar | timer.svg |
| 5 | spotlight | Spotlight | Toolbar | spotlight.svg |
| 6 | app_launcher | App Launcher | ToolbarMulti | apps.svg |
| 7 | logs | 日志 - Luminalium | Window | debug.svg |
| 8 | status_bar | Status Bar | StatusBar | (empty) |

Placeholder commands return typed `NotSupported`; placeholder views render a TextBlock; both carry
"Implemented in Tasks 17-19." Real plugin behavior is explicitly deferred.

## Build / test / gate

```
dotnet build Luminalium.sln -c Release   => 0 warnings, 0 errors
dotnet test  Luminalium.sln -c Release   => passed 48, failed 0, skipped 0
tools/ForbiddenReferenceCheck.ps1        => pass
```

`BuiltInPluginRegistryTests` covers: 8-entry catalog with exact order and manifest mapping,
duplicate-id rejection with original preserved, id validation, per-plugin termination with error
isolation, execute lifecycle double-activation guard, idempotent terminate, and order stability
across two fresh registries.
