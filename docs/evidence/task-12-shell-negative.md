# Task 12 Shell Negative Path Evidence

## Built-In Safe States

- Version metadata load failures use `ShellViewModel.VersionUnavailableText` (`Version metadata unavailable`) and do not block shell startup.
- `DialogService` guards concurrent dialog opens and reports `ShellErrorKind.DialogAlreadyOpen` through observable shell state instead of throwing.
- Dialog owner failures report `ShellErrorKind.DialogFailed` through observable shell state.
- Tray installation is optional. Unsupported or unavailable tray behavior reports `Tray icon unavailable` plus a startup error string without crashing the app.
- The `FAFrame` navigation stack is disabled and history is owned by `ShellViewModel`, so duplicate same-page navigation does not add a back-stack entry.

## Headless Negative Tests

The new `ShellViewModelTests` verify malformed version metadata falls back safely:

```text
ShellViewModel.LoadVersionDisplay("malformed.json") => Version metadata unavailable
new ShellViewModel(malformedPath).VersionDisplay => Version metadata unavailable
```

Navigation negative behavior is covered by the same-page test:

```text
initial CanGoBack=False
navigate timer plugin => CanGoBack=True
navigate timer plugin again => no duplicate history
GoBack() => CurrentPage is Overview, CanGoBack=False
```

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

## Live UI Negative Smoke

An intermediate parallel `dotnet test` plus GUI startup attempt exhausted MSBuild/process memory in this workstation session and failed before meaningful UI validation. The checks were rerun sequentially afterward and passed:

```text
Title=Luminalium
CloseMainWindow=True
ExitCode=0
```

The live UI smoke for Settings navigation and theme toggle is environment-dependent and is recorded for the Task 22 desktop QA harness. On this machine, a Windows UI Automation pass found Settings, opened it, found the Theme mode combo box, selected Light, and closed the app cleanly with `ExitCode=0`.
