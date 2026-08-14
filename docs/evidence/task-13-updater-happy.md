# Task 13 Updater Happy Path Evidence

## Built

- `Luminalium.Updater` now contains a typed C# update pipeline for feed checks, downloads, validation, staging, replacement, rollback, and orchestration.
- `GitHubUpdateFeedClient` checks `https://api.github.com/repos/SECTL/Luminalium/releases/latest` with the legacy mirror list (`github`, `ghproxy`, `moeyy`, `ghproxy2`, `idayer`, `kkgithub`), Chrome user agent, and per-mirror timeout.
- Release versions are opaque strings. Availability is `force` or tag differs from the current `versionnm` using ordinal-ignore-case comparison; `nightly` is available when it differs from the current string.
- Asset selection prefers `Luminalium-Windows.zip`, then the first `.zip`, `.exe`, or `.7z` asset.
- `UpdateDownloader` writes `%LOCALAPPDATA%\Luminalium\update_cache\{tag}\update.zip`, reports progress, retries three times, verifies `.sha256` when present, and falls back to structural validation when the sidecar is missing.
- `UpdateValidator` opens the staged zip before replacement, requires root `Luminalium.exe` and `version.json`, blocks unsafe paths, and rejects Python/PyInstaller, PySide/PyQt, Qt/QML, WebView2, VSTO, Kazuha bridge, and external-plugin payload markers in the same spirit as `tools/Assert-ForbiddenPayload.ps1`.
- `UpdateReplacementCoordinator` acquires named mutex `Luminalium_Updater`, validates staged content before touching install files, waits for any running `Luminalium.exe` in the target install directory, backs up current files to `{install}.backup-{timestamp}`, copies staged files over the install, and retains the backup.
- `UpdateOrchestrator` ties check/download/validate/replace together and short-circuits development layouts when `.dev` exists next to the process executable or `Luminalium.exe` is missing.
- `SettingsPage` now has a native FluentAvalonia Updates section with `Check for updates` and observable status text. `DialogService` shows `UpdateDialogContent` with progress/status/error driven by `UpdateDialogViewModel`; no PySide6/Python UI path remains in the new app surface.

## Deterministic Happy Tests

- `UpdateServiceTests.DownloadValidateAndReplaceHappyPathUsesLocalFixtures` serves a valid in-test zip and matching `.sha256` from `HttpListener` on `127.0.0.1`, asserts progress reaches 100, validates the zip, replaces a temp install, and verifies the backup retained the old executable.
- `UpdateServiceTests.FeedPreservesOpaqueVersionSemantics` covers up-to-date, forced, differing opaque tag, and nightly availability semantics with a fake `HttpMessageHandler` and no real network.

## Verification

```text
dotnet build Luminalium.sln -c Release
已成功生成。
    0 个警告
    0 个错误
```

```text
dotnet test Luminalium.sln -c Release
已通过! - 失败:     0，通过:    63，已跳过:     0，总计:    63，持续时间: 997 ms - Luminalium.Tests.dll (net10.0)
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Live UI Smoke

The built desktop executable was launched from:

```text
src\Luminalium.App\bin\Release\net10.0-windows10.0.17763.0\Luminalium.exe
```

Window smoke result:

```text
Title=Luminalium
WindowFound=True
CloseMainWindow=True
ExitCode=0
```

UI Automation found the native Settings navigation item after startup. The Settings update surface is also covered by compiled AXAML and view-model tests; broader desktop interaction automation remains environment-dependent.

## Environment-Dependent Declarations

- Live GitHub release polling was not verified against the real release feed; feed behavior is covered by local deterministic fixtures and fake handlers.
- Real Windows file-lock behavior against a live running installed Luminalium process was not verified; process-wait, mutex, replacement, and rollback behavior are covered deterministically with temp installs and injectable file-system fakes.
