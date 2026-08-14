# Task 15 Presentation Monitor Environment-Dependent Evidence

## Live Office / WPS Status

- Live Office COM and WPS slideshow verification was not performed on this
  machine.
- Fake coverage is not proof of live COM compatibility.
- The PowerPoint adapter is intentionally late-bound and version-agnostic at
  compile time. The legacy README states Office 2010+ support; this task did not
  verify Office 2010+ or Microsoft 365 behavior on a live machine.
- The WPS path consumes the Task 10 WebSocket bridge protocol. Live verification
  requires WPS 2019+ with the Luminalium2WPS bridge client connected and a running
  slideshow. That live WPS setup was not verified here.
- Missing Office/WPS must surface as typed `HostUnavailable` and must not crash
  the Luminalium shell.

## Required Live Verification Machine

- Windows 10 1809 or later.
- Installed Microsoft Office PowerPoint 2010 or later, or Microsoft 365
  PowerPoint.
- Installed WPS 2019 or later for WPS bridge verification.
- A normal, non-Protected-View presentation and a Protected View or read-only
  presentation for restriction checks.
- A running slideshow window visible on the desktop.

## Live PowerPoint Verification Procedure

1. Build the solution with `dotnet build Luminalium.sln -c Release`.
2. Start PowerPoint and begin a slideshow from a normal presentation.
3. Construct `PowerPointPresentationHost` with `PowerPointComAdapter` and
   `Win32SlideshowWindowAdapter`, then construct `PresentationMonitor` with that
   host.
4. Call `ConnectAsync` and `GetStateAsync`; expect `HostKind=PowerPoint`,
   `IsSlideShow=True`, `CurrentSlide >= 1`, and `SlideCount >= CurrentSlide`.
5. Call `NavigateNextAsync`, `NavigatePreviousAsync`, and `GotoSlideAsync(1)`;
   expect the slideshow to move and the state to refresh without exceptions.
6. Call `SetPointerTypeAsync(Pen)`, navigate once, and verify the pointer returns
   to pen mode after navigation.
7. Call `ApplyPenColorAsync("#FF0000")`; expect the COM pointer color path or the
   palette fallback to apply red without crashing.
8. Close the slideshow and call `GetStateAsync`; expect typed `SlideshowClosed` or
   `HostUnavailable` instead of an exception.
9. Open a Protected View or read-only presentation and call `GetStateAsync`; expect
   `IsProtectedView` and/or `IsReadOnly` to reflect the restriction. Navigation in
   Protected View should return `ProtectedViewRestricted`.

## Live WPS Verification Procedure

1. Build the solution with `dotnet build Luminalium.sln -c Release`.
2. Start the WPS bridge host through `WpsBridgeAutomationAdapter.ConnectAsync`.
3. Start WPS 2019+ with the Luminalium2WPS bridge client and connect it to the
   host URL exposed by Task 10 (`ws://127.0.0.1:{port}/ws`).
4. Begin a WPS slideshow.
5. Call `GetStateAsync`; expect bridge `presentation_state` to populate
   `CurrentSlide`, `SlideCount`, `IsSlideShow`, optional pointer type, and pen
   color.
6. Call `NavigateNextAsync`, `NavigatePreviousAsync`, `GotoSlideAsync(1)`, and
   `ApplyPenColorAsync("#FF0000")`; expect matching `command_result` responses.
7. Disconnect the WPS client and repeat a state read; expect typed
   `HostUnavailable`, `CommandRejected`, or `SlideshowClosed`, not a crash.

## Deterministic Coverage Available Here

- The in-repo deterministic coverage verifies presentation monitor orchestration,
  typed failures, restrictions, pointer restore, rate limiting, Win32 fallback
  selection, and WPS protocol seams through fakes and Task 10 protocol types.
- Live COM, live Office modal/busy behavior, real slideshow focus behavior, and
  WPS add-in behavior remain environment-dependent follow-up checks on a machine
  with Office/WPS installed and configured.
