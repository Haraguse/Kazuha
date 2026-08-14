# Task 8 - Windows Platform Services (negative / fail-safe evidence)

Branch: `RyouYamada`

## Guarantee under test

Platform service failures are surfaced as typed `PlatformOperationResult` values
instead of unhandled exceptions. Core contracts remain platform-neutral, and
Windows-only P/Invoke/registry code is isolated to `Luminalium.Platform.Windows`.

## Negative paths

`UrlProtocolRegisterIsIdempotentAndUnregisterToleratesMissingKeys` verifies:

- unregistering before any protocol keys exist succeeds;
- after unregister, `IsRegistered` reports `false` rather than failing;
- repeated registration with an already-correct command returns success with
  `AlreadyApplied=true` and avoids a rewrite.

`AutostartEnableAndDisableAreIdempotent` verifies:

- repeated enable returns a typed already-applied success for the exact command;
- repeated disable succeeds when the Run value is already absent.

`MachineIdentityReadsNormalizedMachineGuidAndCachesFallbackWarning` verifies:

- missing HKLM `MachineGuid` does not throw;
- the service returns a generated GUID plus a structured warning;
- the fallback value is cached for the process lifetime.

`ToastNotificationCapabilityReportsAumidRegistrationState` verifies:

- an absent HKCU AUMID registration key reports unsupported as a typed
  capability result;
- `Show` returns typed `Unavailable` when unsupported;
- no WinRT toast firing is attempted from this service.

`WindowStylesReturnTypedFailuresAndDarkTitleBarTypedResult` verifies:

- `IntPtr.Zero` never reaches DWM and returns a deterministic typed failure;
- a real hidden HWND returns a typed dark-titlebar result. On newer Windows this
  can succeed; on unsupported Windows builds it can return `UnsupportedVersion`.

## Build / test

```
dotnet build .\Luminalium.sln -c Release
=> 0 warnings, 0 errors

dotnet test .\Luminalium.sln -c Release
=> passed 20, failed 0, skipped 0
```

The final solution suite includes the existing 14 tests plus 6 Task 8 platform
service tests.

## Environment-dependent items not verified

- Live toasts are not verified. The service intentionally stops at capability
  status and process AUMID setup; WinRT toast firing is deferred.
- Live registry behavior on a clean profile is not verified. The exact paths,
  values, deletion tolerance, and idempotency are covered with fake registry
  storage to avoid mutating the developer profile during tests.
- Live multi-monitor hardware is not verified. Monitor tests use a fake adapter
  because hardware layout, DPI awareness mode, and CI desktop state vary by host.
- Live DWM behavior can vary by Windows build. The test asserts typed results and
  accepts either success or `UnsupportedVersion` for the dark-titlebar path.
