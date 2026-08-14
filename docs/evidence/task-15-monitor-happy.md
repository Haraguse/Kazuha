# Task 15 Presentation Monitor Happy Path Evidence

## Built

- Added `Luminalium.Presentation` as a plain `net10.0` project. It references only
  `Luminalium.Core` and `Luminalium.Wps`.
- Added `PresentationState`, `PresentationPointerType`, `PresentationHostKind`,
  `PresentationErrorCode`, and presentation-specific typed operation results.
- Added adapter contracts for the monitor boundary: `IPresentationHost`,
  `IOfficeComAdapter`, `ISlideshowWindowAdapter`, `IWpsAutomationAdapter`, and
  `ICommandTimer`.
- Added `PresentationMonitor` orchestration for preferred-host ordering,
  connect/state refresh, protected-view restriction checks, page-turn token
  limiting, navigation, goto, annotation pen color, pointer restoration after
  navigation, and a typed zoom seam.
- Added `PowerPointPresentationHost` and `WpsPresentationHost`. PowerPoint uses
  COM first and Win32 key-post fallback. WPS uses the Task 10 bridge protocol
  only.
- Added `WpsBridgeAutomationAdapter`, consuming `WpsBridgeHost`, `WpsProtocol`,
  and `WpsRequestTracker` without changing `Luminalium.Wps`.
- Added `Luminalium.Presentation.Windows` as a
  `net10.0-windows10.0.17763.0` project. It references Presentation, Core, and
  Platform.Windows.
- Added `Win32SlideshowWindowAdapter` for window discovery, rect reads,
  foreground/focus helpers, and `PostMessageW` key posting.
- Added `PowerPointComAdapter` using late-bound COM only:
  `Type.GetTypeFromProgID("PowerPoint.Application")`, `Activator.CreateInstance`,
  and reflection `InvokeMember` with `BindingFlags.GetProperty`, `SetProperty`,
  and `InvokeMethod`. No Office interop, VSTO, or bridge package was added.

## Legacy Heuristics Ported

- PowerPoint slideshow class: `screenClass`.
- WPS slideshow classes: `wppSlideShowWindowClass`, `WPP SlideShow Window`, and
  `WPP SlideShow Window 8.0`.
- PowerPoint process matching: `powerpnt.exe`.
- WPS process matching: `wpp.exe` and `kwpp.exe`.
- Title hints: `slide show`, `slideshow`, `slide-show`, Simplified and
  Traditional Chinese slideshow hints, Japanese slideshow hints, Korean
  slideshow hints, French `diaporama`, German `bildschirmprasentation` /
  `bildschirmpräsentation`, Spanish `presentacion/presentación con diapositivas`,
  and Portuguese `apresentacao/apresentação de slides`.
- Preferred-kind discovery checks the foreground window first, then enumerates
  top-level windows, and accepts either a known slideshow class or a matching
  presentation process plus slideshow-looking title.
- Win32 rect tracking uses `GetWindowRect` and stores raw window coordinates.
- Key posting uses `WM_KEYDOWN` / `WM_KEYUP`, virtual-key scan codes from
  `MapVirtualKeyW`, repeat count `1`, and key-up flags equivalent to legacy
  `0xC0000000` lParam construction.
- COM slide state reads `SlideShowWindow.View.State`,
  `View.CurrentShowPosition`, fallback `View.Slide.SlideIndex`, and
  presentation `Slides.Count`.
- COM guarded reads treat missing/closed slideshow windows as typed
  `SlideshowClosed` rather than exceptions.
- Pointer type mapping preserves legacy PowerPoint values: `1` Arrow, `2` Pen,
  `3` Highlighter, `5` Eraser, unknown otherwise.
- Pen color uses Office BGR integer ordering (`R + (G << 8) + (B << 16)`) and the
  legacy PowerPoint palette grid for supported fallback colors, including
  `#FF0000` at row `1`, column `1`.
- Navigation preserves COM-first behavior for `Next`, `Previous`, and
  `GotoSlide`; Win32 fallback uses `VK_DOWN`/`VK_NEXT`, `VK_PRIOR`/`VK_UP`, and
  digit keys plus Enter for goto.
- WPS selection uses the Task 10 bridge messages `presentation_state_get`,
  `presentation_next`, `presentation_prev`, `presentation_goto`, and
  `presentation_pen_color_set`; no Linux or xdotool code was added.

## Deterministic Happy Tests

- `PresentationMonitorTests.HappyPathConnectsNavigatesAnnotatesZoomsAndReturnsExactState`
  verifies connect, Next, Previous, `GotoSlide(4)`, red pen color, pen pointer,
  zoom seam, and exact final state against an in-memory fake host.
- `PresentationMonitorTests.PointerRestoreAfterNavigationWithPenActive` behavior
  is covered by `NavigationRestoresPenPointerAfterHostResetsIt`: a host resets to
  Arrow during navigation and the monitor restores Pen afterward.
- `PresentationMonitorTests.PowerPointHostUsesWin32FallbackWhenComIsUnavailable`
  verifies PowerPoint host fallback to the fake slideshow window adapter and
  confirms `VK_DOWN` was posted.

## Verification

```text
dotnet build Luminalium.sln -c Release
已成功生成。
    0 个警告
    0 个错误
```

```text
dotnet test Luminalium.sln -c Release
已通过! - 失败:     0，通过:    84，已跳过:     0，总计:    84，持续时间: 1 s - Luminalium.Tests.dll (net10.0)
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Manual Library Surface Check

A disposable console driver under `%TEMP%\opencode\LuminaliumTask15Driver`
referenced `src\Luminalium.Presentation\Luminalium.Presentation.csproj`,
constructed `PresentationMonitor` with an in-memory `IPresentationHost`, and
executed connect, next, goto, pen color, pointer type, zoom, and state read.

```text
connect=True
next=True
goto=True
color=True;pen=#FF0000
pointer=True;tool=Pen
zoom=True;factor=1.5
state=PowerPoint:4/5:True
```

The temporary driver directory was removed after the run.
