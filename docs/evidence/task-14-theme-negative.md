# Task 14 Monet Theme Negative Path Evidence

## Built-In Safe States

- The app does not require a wallpaper file. `MonetThemeService` tries accent first, then wallpaper path/image analysis, then returns the deterministic fallback palette with structured warnings.
- The cross-platform theming project contains no P/Invoke and no registry access. Windows-only interop is isolated to `Luminalium.Platform.Windows.Theming`.
- Provider failures use `PlatformOperationResult`/`PlatformOperationWarning`; malformed images, missing paths, denied registry/image reads, and unavailable wallpaper/accent state do not throw through the app surface.
- `MonetImageAnalyzer` treats malformed/unsupported image data as typed failure and skips transparent pixels. Empty or all-transparent images produce a typed no-samples failure.
- Wallpaper analysis rejects network paths, reparse points, oversized encoded files, excessive source dimensions, and excessive source pixel counts before sampling. SkiaSharp decode now probes encoded bounds through `SKCodec` and decodes directly into the bounded analysis bitmap.
- `ApplyMonetPalette` uses FluentAvalonia/app resource dictionaries. Preset and system accent paths clear the Monet overrides before applying their own accent mode, so stale Monet resources do not remain active.
- Accent option updates are request-versioned so a slow Monet palette computation cannot apply after the user chooses a newer accent preset.

## Deterministic Negative Tests

- `MonetThemingTests.ExtractSeedFallsBackToWholeAverageWhenSamplesAreDesaturated` verifies desaturated-only samples fall back to whole-image average and empty samples return no seed.
- `MonetThemingTests.EnsureContrastAdjustsLowContrastPrimaryAndLeavesHighContrastPrimary` verifies a low-contrast primary is adjusted until `primary` vs `background` is at least `4.5:1`, and a high-contrast palette is unchanged.
- `MonetThemingTests.ThemeServiceFallsBackWithWarningWhenProvidersAreMissing` verifies missing accent and missing wallpaper produce fallback plus warning.
- `MonetThemingTests.ThemeServiceFallsBackWhenWallpaperImageIsMalformed` writes a malformed image fixture and verifies typed fallback plus an unsupported-format warning.
- `MonetThemingTests.ThemeServiceFallsBackWhenWallpaperPathIsUnsafeOrOversized` verifies unsafe network paths and oversized files return fallback with structured warnings.
- `MonetThemingTests.ThemeServiceReturnsFixedHighContrastPaletteWhenRequested` verifies high contrast bypasses system providers and returns the fixed palette.
- `ShellViewModelTests.LaterAccentSelectionWinsOverSlowMonetComputation` verifies stale async Monet work is ignored after a later preset selection.

## Verification

```text
dotnet build Luminalium.sln -c Release
已成功生成。
    0 个警告
    0 个错误
```

```text
dotnet test Luminalium.sln -c Release
已通过! - 失败:     0，通过:    77，已跳过:     0，总计:    77，持续时间: 998 ms - Luminalium.Tests.dll (net10.0)
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Environment-Dependent Declarations

- Live Windows accent and wallpaper extraction can vary by registry policy, user settings, wallpaper path type, desktop state, and access permissions; failures are treated as typed warnings and fall back deterministically.
- Full-app visual screenshots are environment-dependent and remain assigned to the Task 22 desktop harness. Palette math, contrast, fallback, high-contrast behavior, and Settings-to-theme-service routing are deterministic and fixture-backed in this task.
- One UI Automation attempt could discover the main window and Settings navigation item but could not reliably select the Settings footer item in this workstation session. The app launch/close smoke passed, and the `System (Monet)` setting path is covered by headless view-model tests.
