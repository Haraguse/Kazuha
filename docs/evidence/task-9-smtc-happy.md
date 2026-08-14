# Task 9 - SMTC in-process service (positive evidence)

Branch: `RyouYamada`

## Contract

- Core exposes `SmtcPlaybackStatus`, `SmtcSessionSnapshot`,
  `ISmtcSessionAdapter`, `ISmtcService`, `SmtcSessionService`, and
  deterministic `SmtcMetadata` helpers under `Luminalium.Core.Media`.
- `SmtcSessionService` is an in-process wrapper. Repeated reads forward to the
  adapter once per call; there is no helper executable, process launch, or
  JSON-line protocol path in the service.
- `Luminalium.Smtc.Windows` is a new solution-listed class library targeting
  `net10.0-windows10.0.19041.0` with min version `10.0.17763.0`. It references
  Core only and contains the WinRT adapter and optional factory.
- `WinRtSmtcSessionAdapter` enumerates
  `GlobalSystemMediaTransportControlsSessionManager.RequestAsync()`,
  `GetCurrentSession()`, and `GetSessions()`, then ranks sessions with the
  legacy ordering through Core helpers.
- Metadata parity preserved: title fallback is title, subtitle, album title,
  friendly source; artist fallback is artist, album artist, first genre;
  position and duration are normalized to non-negative millisecond values with
  position clamped to duration.
- Artwork parity preserved: thumbnail streams are capped at 8 MiB, content type
  is normalized or magic-byte detected, browser-unfriendly formats are converted
  to PNG through `BitmapDecoder`/`BitmapEncoder`, and data URLs use
  `data:{contentType};base64,...`.
- `SmtcWindowsFactory.TryCreateAdapter()` returns an optional adapter and catches
  platform/projection load failures. App runtime loading is deferred to the
  consumer task and is not wired here.

## Build / test / gate

```
dotnet build .\Luminalium.sln -c Release
=> 0 warnings, 0 errors

dotnet test .\Luminalium.sln -c Release
=> passed 25, failed 0, skipped 0

powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
=> Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## xUnit coverage

`SmtcServiceTests` verifies:

- happy adapter snapshot returns every field exactly through `GetCurrentSessionAsync`;
- empty snapshot returns a typed successful no-session result;
- typed `Unavailable` failures are forwarded without throwing;
- repeated reads call the fake adapter exactly twice, proving the service has no
  helper-process state to reuse or spawn;
- pure helper parity for status ranking, metadata score math, first non-empty
  fallback, genre fallback, friendly source mappings, content-type normalization,
  PNG/JPEG/WebP/BMP/GIF detection, SVG detection, and artwork key format.

## Manual surface check

```
dotnet run -c Release
=> success=True
=> title=Driver Title
=> calls=2
```

The temporary driver lived under `%TEMP%\opencode`, referenced Core, created a
fake adapter and `SmtcSessionService`, called both service entry points, and was
removed after the run.

## Environment-dependent items not verified

- Live WinRT SMTC session verification requires a real playing media session and
  is environment-dependent; it was not verified in this run.
- App runtime loading of `Luminalium.Smtc.Windows` is intentionally deferred to a
  later consumer task.
