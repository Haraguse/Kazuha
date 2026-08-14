# Task 16 Native Presentation Overlay Happy Path Evidence

## Built

- Added a native Avalonia `OverlayWindow` with `WindowDecorations="None"`,
  runtime `SystemDecorations=None`, transparent background, topmost behavior, and
  placement through Avalonia `Screens` only.
- Added a monitor-backed `OverlayViewModel` that accepts a `PresentationMonitor`
  constructed from injected `IPresentationHost` instances in tests.
- The app default overlay composes a WPS-only host through
  `WpsBridgeAutomationAdapter` and `WpsPresentationHost`; it does not reference
  `Luminalium.Presentation.Windows`, `Luminalium.Platform.Windows`, or Win32
  APIs from the overlay surface.
- Added `AnnotationCanvas`, `StrokeModel`, and `IAnnotationSink`; pointer, pen,
  and touch strokes are rendered with `DrawingContext` and recorded through the
  sink.
- Added `IOverlayScreenProvider` with an Avalonia `Screens` implementation and
  `IScreenshotService` with a minimal `RenderTargetBitmap` implementation.
- Added a footer `OpenOverlay` button in `MainWindow` plus tray-menu and keyboard
  entry points that open the overlay and return focus to the shell when closed.
- Overlay zoom is a `ScaleTransform` on the annotation canvas only. Window bounds
  are not changed by zoom transitions.
- Spotlight dimming uses an opacity mask with a movable transparent center.

## Deterministic Happy Tests

- `OverlayViewModelTests.FakeSlideshowConnectsNextAndGotoSlide` verifies fake
  slide state `1/5`, `NextSlide` moving to `2`, and `GotoSlide(4)` moving to
  slide `4` through `PresentationMonitor`.
- `OverlayViewModelTests.OneStrokeModelCallRecordsExpectedColorAndPointsThenClearEmpties`
  verifies one stroke record with normalized `#FF0000`, exact points, and clear.
- `OverlayViewModelTests.ZoomTransitionsAreDeterministicAndDoNotChangePlacementModel`
  verifies `1.25 -> 1.5 -> 1.0` zoom transitions and unchanged selected screen
  bounds.
- `OverlayViewModelTests.ScreenProviderSelectionPicksExpectedBounds` verifies the
  fixed screen seam selects the expected monitor bounds.

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

## Live UI Smoke

A UI Automation smoke run launched the Release app, invoked the footer button
with `AutomationProperties.Name="OpenOverlay"`, found the overlay toolbar, and
invoked `CloseOverlay`.

```text
Primary=2560x1600
PreviousSlide|enabled=False|help=No active slideshow|w=77|h=66
NextSlide|enabled=False|help=No active slideshow|w=80|h=66
Draw|enabled=False|help=No active slideshow|w=85|h=66
Spotlight|enabled=False|help=No active slideshow|w=78|h=66
Zoom|enabled=False|help=No active slideshow|w=91|h=66
Clear|enabled=True|help=Clear overlay annotations|w=84|h=66
Screenshot|enabled=False|help=No active slideshow|w=79|h=66
CloseOverlay|enabled=True|help=Close the overlay|w=87|h=66
```

The same run confirms every toolbar touch target is at least `44` effective
pixels high and the no-slideshow state is visible through the live accessibility
tree.

## Environment-Dependent Items

- Live pen/touch hardware drawing and live multi-monitor visual evidence are
  environment-dependent and remain Task 22 validation items.
- A screenshot file was captured under `%TEMP%\opencode`, but this runtime could
  not inspect image pixels directly; UI Automation was used for live surface
  verification instead.
