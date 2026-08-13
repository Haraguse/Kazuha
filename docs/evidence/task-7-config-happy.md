# Task 7 - Versioned Configuration (positive evidence)

Branch: `RyouYamada`

## Contract

- `LuminaliumConfig` is a fresh schema with `SchemaVersion=1` and typed
  `Appearance`, `General`, `Toolbar`, `Linkage`, `Overlay`, `PPT`, `SelfPen`,
  `Notifications`, and `Security` sections.
- JSON is written indented with stable existing section/key names and readable
  strings for closed enums such as `ThemeMode` and `ToolbarPosition`.
- UTF-8 input accepts the optional UTF-8 BOM while backups preserve the exact
  original bytes.
- `ConfigurationService` receives its settings directory from its constructor;
  no Python settings path is read or modified at runtime.
- A missing active file creates and atomically persists a valid default config.
  `_active` selects the base `settings.json` for empty/`default` markers and a
  named `<profile>.json` when that profile exists.
- `Save` writes UTF-8 JSON to a same-directory temporary file, flushes it to
  disk, then moves it over the active target. Temporary files are cleaned up.
- `Security.PasswordHash` is the only password-related field; the service
  accepts only algorithm-prefixed salted-hash-shaped values and has no
  plaintext password property or persistence API.

## Build / test

```
dotnet build .\Luminalium.sln -c Release
=> 0 warnings, 0 errors

dotnet test .\Luminalium.sln -c Release
=> passed 14, failed 0, skipped 0
```

## xUnit coverage

`ConfigurationServiceTests` maps the happy-path QA scenarios as follows:

- `FreshInstallCreatesVersionedConfigAndRoundTripsThemeAndAccent`
  - an empty injected directory creates `settings.json` with `SchemaVersion=1`
  - the absent `_active` marker resolves to the default profile; the test then
    writes an explicit `default` marker before round-trip persistence
  - persists and reloads dark theme, `#0078D4` accent, default theme id, and
    representative defaults
  - confirms the injected temp directory contains only `_active` and C# config
    files, with no Python settings path created
- `SaveAtomicallyReplacesContentAndLeavesNoTemporaryFile`
  - replaces prior content with a new config
  - verifies readable enum JSON, hash field round-trip, and no `.tmp` artifact
- `ActiveProfileMarkerLoadsNamedProfile`
  - `_active` naming `presentation` resolves `presentation.json`
  - verifies profile-specific theme and toolbar position values
- `PlaintextPasswordIsRejectedWhenProtectionIsEnabled`
  - rejects a plaintext password value before any settings file is written

## Extension boundary

`IConfigImporter` is the documented future import seam. Python configuration
import is explicitly out of scope for this release and is not called by the
runtime service.
