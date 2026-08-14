# Task 15 Presentation Monitor Negative Path Evidence

## Fail-Safe Behavior

- Every external boundary returns `PresentationOperationResult` or
  `PresentationOperationResult<T>` with `PresentationError`; monitor methods also
  guard adapter exceptions and map them to typed errors.
- `PresentationErrorCode` covers `HostUnavailable`, `ComTimeout`,
  `BitnessMismatch`, `DisconnectedHost`, `SlideshowClosed`,
  `ProtectedViewRestricted`, `CommandRejected`, and `Unsupported`.
- Reflection-wrapped COM faults are unwrapped from `TargetInvocationException`
  before mapping.
- PowerPoint COM activation failure is typed `HostUnavailable`; type-load,
  bad-image, and COM bitness failures map to `BitnessMismatch` when applicable.
- RPC rejection / busy COM HRESULTs `0x80010001` and `0x8001010A` map to
  `ComTimeout`.
- Disconnected COM HRESULTs `0x800706BA` and `0x80010108` map to
  `DisconnectedHost`.
- Missing or inactive slideshow windows map to `SlideshowClosed` and never crash
  the shell.
- Protected View sets `PresentationState.IsProtectedView` and returns
  `ProtectedViewRestricted` for navigation/goto operations.
- Read-only state is surfaced as `PresentationState.IsReadOnly`; it does not
  crash and does not hide the slideshow state.
- Page-turn limiting is deterministic through the `ICommandTimer` seam.
- Zoom is exposed as a typed operation seam. The built-in PowerPoint and WPS hosts
  return `Unsupported` because overlay zoom lives outside this monitor boundary.
- WPS bridge disconnects clear the cached slideshow-running flag without throwing.

## Deterministic Negative Tests

- `NavigationRateLimitingReturnsTypedCommandRejectedResult` uses a blocking fake
  timer and verifies the second Next returns typed `CommandRejected`.
- `ExternalExceptionsMapToTypedPresentationErrors` verifies fake COM timeout,
  type-load / bitness, and unavailable-host failures map to the expected
  `PresentationErrorCode` values.
- `SlideshowClosedMidOperationReturnsTypedClosedError` verifies a slideshow that
  closes during Next returns `SlideshowClosed`.
- `ProtectedViewSetsStateFlagsAndRestrictsNavigationWithoutCrash` verifies
  protected view and read-only flags are cached and navigation returns typed
  `ProtectedViewRestricted`.
- `PowerPointHostUsesWin32FallbackWhenComIsUnavailable` verifies COM absence does
  not crash and Win32 fallback key posting is used.

## Dependency and Source-Boundary Checks

- `Luminalium.Presentation` is plain `net10.0` and references only Core and WPS.
- `Luminalium.Presentation.Windows` is Windows-targeted and references
  Presentation, Core, and Platform.Windows.
- No Office interop NuGet packages were added.
- No VSTO, `scripts/ppt_vsto_bridge`, Python, Linux, xdotool, or WebView
  dependency was introduced.
- `Luminalium.Wps`, `Luminalium.Core`, `Luminalium.Platform.Windows`, and
  `Luminalium.App` were not modified.

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

## Not Proven By Fakes

- Fake adapters prove orchestration, typed error mapping, and deterministic
  fallback behavior. They are not proof that a live Office/WPS installation accepts
  every COM or bridge command.
