# Task 16 Native Presentation Overlay Negative Path Evidence

## Built-In Safe States

- When `PresentationMonitor.GetStateAsync` returns no active slideshow, the
  overlay sets `HasActiveSlideshow=false`, hides drawing/spotlight activity, and
  reports `No active slideshow`.
- `NextSlide`, `Draw`, and `Zoom` have `CanExecute=false` in that state and their
  toolbar controls expose `AutomationProperties.HelpText="No active slideshow"`.
- Invoking disabled `NextSlide`, `Draw`, and `Zoom` commands performs no monitor
  calls after the initial state read.
- Screenshot is disabled with no active slideshow. `Clear` and `CloseOverlay`
  remain available so the overlay can be safely cleaned up and dismissed.
- The overlay has no HTML, QML, WebView, P/Invoke, `DllImport`, or Win32 view code.

## Deterministic Negative Tests

- `OverlayViewModelTests.NoSlideshowDisablesUnsafeCommandsAndSkipsMonitorCalls`
  verifies disabled command `CanExecute`, exact help text, and zero fake-host
  command calls for Next/Draw/Zoom after the unavailable state is established.
- Existing `PresentationMonitorTests` continue to cover typed unavailable,
  slideshow-closed, protected-view, rate-limit, and fallback monitor errors.

## Verification

```text
dotnet build Luminalium.sln -c Release
已成功生成。
    0 个警告
    0 个错误
```

```text
dotnet test Luminalium.sln -c Release
已通过! - 失败:     0，通过:    89，已跳过:     0，总计:    89，持续时间: 1 s - Luminalium.Tests.dll (net10.0)
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Live UI Negative Smoke

A UI Automation smoke run opened the overlay against the current environment,
where no WPS slideshow was active. It verified disabled state and touch target
geometry from the live controls:

```text
NextSlide|enabled=False|help=No active slideshow|w=80|h=66
Draw|enabled=False|help=No active slideshow|w=85|h=66
Zoom|enabled=False|help=No active slideshow|w=91|h=66
```

## Not Proven Here

- Live WPS/Office slideshow control, live pen/touch hardware, and multi-monitor
  visual inspection depend on available hardware and a running slideshow. Those
  remain environment-dependent checks for Task 22.
