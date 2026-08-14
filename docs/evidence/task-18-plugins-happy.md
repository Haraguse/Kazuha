# Task 18 - Operational Plugins (positive evidence)

Branch: `RyouYamada`

## What was built

Four operational plugins ported to native Avalonia, each with an injectable
service contract so behavior is verifiable headlessly:

- **Timer** (`ViewModels/TimerViewModel`, `Views/TimerWindow`): `HH:MM:SS`
  countdown driven by an injectable `IClockService`; async `StartCommand`,
  pause/reset, cancel-on-terminate (no timer task outlives the window), and
  localized finished cue/notification via `IAudioCueService` /
  `INotificationCueService` (no-op singletons by default).
- **Spotlight** (`ViewModels/SpotlightViewModel`, `Views/SpotlightWindow`):
  full-screen dim with a normalized rectangular hole; `IsDimmed` excludes the
  selection rect; clear-selection restores full dim.
- **App Launcher** (`ViewModels/AppLauncherViewModel`, `Views/AppLauncherWindow`):
  add/launch/remove entries with duplicate detection; launching goes through
  `IProcessLaunchService`, which validates the path BEFORE `Process.Start` and
  returns a typed `NotFound` result instead of throwing.
- **Status Bar** (`ViewModels/StatusBarViewModel`, `Views/StatusBarWindow`):
  slide N/M + media title/artist from injectable `IPresentationStatusSource` /
  `IMediaStatusSource`; empty states render localized placeholders and never throw.
- Shell wiring: `MainWindow` opens all four plugin windows; `Timer.*`,
  `Spotlight.*`, `Launcher.*`, `StatusBar.*` keys added to zh-CN and en-US.

## Verification

```
dotnet build Luminalium.sln -c Release   => 0 warnings, 0 errors
dotnet test  Luminalium.sln -c Release   => passed 129, failed 0, skipped 0
tools/ForbiddenReferenceCheck.ps1        => pass
```

`OperationalPluginsTests` (headless, all external deps are fakes) covers:
- Timer: 3-second countdown finishes with the finished cue fired exactly once;
  terminate cancels the running countdown with no task outliving the window;
  10-case `TryParseInput` theory (HH:MM:SS / MM:SS / SS, negatives, overflow,
  malformed).
- Spotlight: selection rect excludes itself from dimming; clear removes it.
- Launcher: missing path => typed error and ZERO processes started; valid path
  => exactly one start; empty path is a no-op.
- Status bar: slide 1/5 renders; empty media shows the placeholder without
  throwing; title/artist renders.

## UI Automation smoke (this machine)

Opening each plugin window surfaces its automation names (e.g. `TimerStart`,
`SpotlightClear`, `LauncherAdd`, `StatusBarRefresh`). Live interaction
and pixel-level visual verification remain environment-dependent (Task 22
desktop harness).