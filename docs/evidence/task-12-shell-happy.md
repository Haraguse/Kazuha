# Task 12 Shell Happy Path Evidence

## Built

- `Luminalium.App` now starts a native FluentAvalonia shell with exactly one `FluentAvaloniaTheme` in `App.axaml`.
- The app resource dictionary defines the shell's CJK-capable font family and reusable spacing/type/touch tokens; page XAML consumes those tokens and FluentAvalonia dynamic resources.
- `MainWindow` is an `FAAppWindow` titled exactly `Luminalium`, with a custom titlebar, `FANavigationView`, `FAFrame`, and `ShellNavigationService` using VM-to-view page mapping.
- Navigation contains Overview, the 8 built-in plugins from `BuiltInPluginCatalog.CreateDefaultRegistry()`, and Settings pinned in the footer.
- Overview renders product/version/plugin metadata. Settings applies System/Light/Dark theme modes through `FluentAvaloniaTheme` and supports accent presets. Plugin pages render metadata and the Tasks 17-19 placeholder notice.
- `DialogService` shows an async `FAContentDialog` About dialog with owner attachment and observable error reporting.
- Startup installs an optional Avalonia `TrayIcon` using `Assets/logo.ico` with Show window, Open settings, and Exit native menu items. Tray failure is reported as shell state instead of crashing.
- A native splash window uses the product name, version, icon, and indeterminate progress, then closes when the main window opens.

## Verification

```text
dotnet build Luminalium.sln -c Release
已成功生成。
    0 个警告
    0 个错误
```

```text
dotnet test Luminalium.sln -c Release
已通过! - 失败:     0，通过:    53，已跳过:     0，总计:    53，持续时间: 886 ms - Luminalium.Tests.dll (net10.0)
```

The 53 passing tests include the new headless `ShellViewModelTests` coverage for catalog projection, navigation back-stack semantics, version metadata fallback, and theme mode transitions.

## Live UI Smoke

The built desktop executable was launched from:

```text
src\Luminalium.App\bin\Release\net10.0-windows10.0.17763.0\Luminalium.exe
```

Window smoke result:

```text
Title=Luminalium
ExitedBeforeClose=False
CloseMainWindow=True
ExitCode=0
```

Native UI Automation smoke result for Settings navigation and theme toggle:

```text
Title=Luminalium
SettingsFound=True
ThemeComboFound=True
LightFound=True
LightSelected=True
SelectedTextAfter=Light
CloseMainWindow=True
ExitCode=0
```

The live UI smoke for window title, Settings navigation, and theme toggle is recorded here as environment-dependent desktop behavior. It was exercised on this machine with Windows UI Automation; broader desktop visual coverage remains assigned to the Task 22 desktop QA harness.
