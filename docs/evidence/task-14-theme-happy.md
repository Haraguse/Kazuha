# Task 14 Monet Theme Happy Path Evidence

## Built

- `Luminalium.Theming` is a new plain `net10.0` project with only `Luminalium.Core` and `SkiaSharp` references. It contains QColor-compatible RGB/HSL structs, hex conversion, deterministic Monet color math, WCAG contrast clamping, palette records, provider contracts, SkiaSharp wallpaper sampling, and `MonetThemeService` orchestration.
- `Directory.Packages.props` now pins `SkiaSharp` at `3.119.4`, matching the version already resolved in the graph.
- `Luminalium.Platform.Windows.Theming.WindowsDesktopColorProviders` implements both theme provider contracts. Wallpaper lookup uses `SystemParametersInfoW(SPI_GETDESKWALLPAPER=0x0073)`; accent lookup reads `HKCU\Software\Microsoft\Windows\DWM\ColorizationColor` and falls back to `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Accent\AccentColorMenu`.
- `Luminalium.App` remains plain `net10.0`. It references `Luminalium.Theming` only, and copies the optional Windows provider assembly after build without a compile-time plain-to-Windows project reference.
- Settings now exposes `System (Monet)` alongside the existing accent presets. Selection computes a palette through `MonetThemeService` and applies it through `IShellThemeService.ApplyMonetPalette`.
- `AvaloniaShellThemeService` applies Monet through FluentAvalonia/theme resources only: `FluentAvaloniaTheme.CustomAccentColor`, `SystemAccentColor`, and dynamic background/surface/text resources such as `LayerFillColorDefaultBrush` and `CardBackgroundFillColorDefaultBrush`. Views continue to bind to the existing resource system and are not mutated directly.

## Algorithm Parity Notes

- Wallpaper analysis scales the decoded image to at most 100 px on the longest side, samples every second pixel, skips alpha `< 128`, and extracts the dominant hue bucket from colorful pixels using QColor-style HSL ranges (`h=0..359`, `s/l=0..255`).
- Hue buckets use `floor(h / 10) * 10`; the seed is the integer average RGB of samples in the most frequent bucket. If no colorful bucket exists, seed extraction falls back to the whole-sample average. Empty sample sets return no seed.
- Palette construction ports the legacy HSL proxy exactly, then applies the Task 14 requested deterministic contrast clamp so `primary` vs `background` is at least `4.5:1`. This clamp is the only intentional post-legacy adjustment.
- High contrast returns a fixed deterministic black/white/yellow palette. Fallback uses deterministic seed `#0078D4`.

## Deterministic Happy Tests

- `MonetThemingTests.ExtractSeedUsesDominantHueBucketAverage` pins the mostly-red bucket average to `#D21E1E`.
- `MonetThemingTests.BuildPalettePinsFallbackSeedColors` pins `#0078D4` to light `#0070C6/#F1F2F3/#FFFFFF/#000000` and dark `#41ADFF/#121416/#1C1E20/#FFFFFF`.
- `MonetThemingTests.BuildPaletteAppliesSaturationBoostAndLightnessClamps` covers very dark and very light seeds.
- `MonetThemingTests.ThemeServiceUsesAccentBeforeWallpaper` verifies accent wins over wallpaper.
- `MonetThemingTests.ThemeServiceUsesWallpaperWhenAccentIsMissing` generates an in-test PNG with SkiaSharp and verifies wallpaper analysis drives the seed.
- `MonetThemingTests.ThemeServiceFallsBackWhenWallpaperPathIsUnsafeOrOversized` verifies network wallpaper paths and oversized image files fall back before native decode.
- `ShellViewModelTests.MonetAccentOptionAppliesComputedPaletteThroughThemeService` verifies the Settings `System (Monet)` option routes through the shell theme service.
- `ShellViewModelTests.LaterAccentSelectionWinsOverSlowMonetComputation` verifies a slow Monet computation cannot overwrite a newer preset selection.

## Verification

```text
dotnet build Luminalium.sln -c Release
已成功生成。
    0 个警告
    0 个错误
```

```text
dotnet test Luminalium.sln -c Release
已通过! - 失败:     0，通过:    77，已跳过:     0，总计:    77，持续时间: 1 s - Luminalium.Tests.dll (net10.0)
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Live UI Smoke

The built desktop executable was launched from:

```text
src\Luminalium.App\bin\Release\net10.0\Luminalium.exe
```

Window smoke result:

```text
Title=Luminalium
WindowFound=True
SettingsFound=True
CloseMainWindow=True
ExitCode=0
```

Optional Windows provider copy check:

```text
dotnet publish src\Luminalium.App\Luminalium.App.csproj -c Release -o C:\Users\EVANEV~1\AppData\Local\Temp\opencode\luminalium-task14-publish
src\Luminalium.App\bin\Release\net10.0\Luminalium.Platform.Windows.dll => True
C:\Users\EVANEV~1\AppData\Local\Temp\opencode\luminalium-task14-publish\Luminalium.Platform.Windows.dll => True
```

## Environment-Dependent Declarations

- Live Windows accent/wallpaper extraction on this machine is environment-dependent; deterministic fake providers and generated image fixtures cover the orchestration and image-analysis behavior.
- Full-app visual screenshots and pixel QA are deferred to the Task 22 desktop harness. This task verifies the app starts, Settings is discoverable, and Monet resource application is covered through deterministic view-model/service tests.
