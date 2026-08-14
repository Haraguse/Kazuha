# Task 8 - Windows Platform Services (positive evidence)

Branch: `RyouYamada`

## Contract

- Core exposes platform-neutral service contracts and typed results under
  `Luminalium.Core.Platform`; it stays `net10.0` with no P/Invoke, registry, or
  Windows-specific APIs.
- Windows-only behavior lives under `Luminalium.Platform.Windows.Platform` on
  `net10.0-windows10.0.17763.0` and references Core, never the reverse.
- Registry URL protocol and autostart commands preserve the legacy strings:
  `"<exe>" "%1"` for `luminalium:` and `"<exe>" --autostart` for Run.
- URL protocol registration writes HKCU `Software\Classes\luminalium`, the
  empty `URL Protocol` marker, `shell\open\command`, and `DefaultIcon`. A second
  registration with the same command returns a typed already-applied success.
- Autostart writes HKCU
  `Software\Microsoft\Windows\CurrentVersion\Run` value `Luminalium`, and a
  second enable returns a typed already-applied success.
- Monitor enumeration is behind `IMonitorAdapter`; Win32 uses
  `EnumDisplayMonitors`, `GetMonitorInfoW`, and `GetDpiForMonitor`, while tests
  use a fake adapter with deterministic monitor data.
- Machine identity reads HKLM `SOFTWARE\Microsoft\Cryptography\MachineGuid`,
  normalizes by trimming and lowercasing, and returns a process-lifetime cached
  fallback GUID with a structured warning if unavailable.
- DWM and borderless window style calls return typed results. Unsupported DWM
  HRESULTs such as the dark-mode attribute on older Windows map to
  `UnsupportedVersion` instead of throwing.
- Toast support checks HKCU
  `Software\Classes\AppUserModelId\Kazuha.Luminalium`; `Show` only validates
  support and sets the process AUMID. Live WinRT toast firing is intentionally
  deferred.

## Build / test

```
dotnet build .\Luminalium.sln -c Release
=> 0 warnings, 0 errors

dotnet test .\Luminalium.sln -c Release
=> passed 20, failed 0, skipped 0
```

## xUnit coverage

`PlatformServicesTests` maps the happy-path QA scenarios as follows:

- `UrlProtocolRegisterIsIdempotentAndUnregisterToleratesMissingKeys`
  - registers `luminalium:` using fake HKCU storage
  - verifies the exact command, `URL Protocol` marker, and default icon string
  - verifies registering again returns success with `AlreadyApplied=true`
- `AutostartEnableAndDisableAreIdempotent`
  - enables the Run value with the exact legacy autostart command
  - verifies enabling again is a typed already-applied success
  - verifies disabling removes the value successfully
- `MonitorServiceReturnsStableMonitorsAndDisplayMathRoundTrips`
  - returns two configured monitors: 3840x2160 at 1.5 primary and 1920x1080 at
    1.0 secondary
  - verifies monitor bounds, primary status, scale factor, and deterministic
    physical/logical point and rectangle round-trips
- `MachineIdentityReadsNormalizedMachineGuidAndCachesFallbackWarning`
  - reads a configured `MachineGuid` exactly after trim/lowercase normalization
  - verifies the missing-key path returns a generated GUID and reuses the same
    cached fallback on the second call
- `ToastNotificationCapabilityReportsAumidRegistrationState`
  - reports supported when the AUMID key exists
  - reports unsupported with a typed capability reason when absent
- `WindowStylesReturnTypedFailuresAndDarkTitleBarTypedResult`
  - verifies `IntPtr.Zero` returns a typed failure
  - creates a hidden native test window and verifies dark titlebar returns a
    typed result, either success or `UnsupportedVersion`

## Environment-dependent items not verified

- Live WinRT toast firing is not verified; Task 8 only exposes capability checks
  and process AUMID setting, while actual toast firing is deferred.
- Start Menu shortcut registration for unpackaged toast delivery is not verified
  and is deferred to later shell tasks.
- Live registry mutation on a clean user profile is not verified; tests use an
  in-memory fake registry adapter to prove parity strings and idempotency.
- Live multi-monitor hardware is not verified; monitor tests use fake adapter
  data so they remain deterministic on single-monitor or headless systems.
