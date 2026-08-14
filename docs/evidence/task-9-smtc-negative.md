# Task 9 - SMTC in-process service (negative / fail-safe evidence)

Branch: `RyouYamada`

## Guarantee under test

SMTC is no longer an external helper protocol. No-session and unavailable paths
are typed results, and the 19041 WinRT adapter remains solution-listed only until
the App consumer task wires optional runtime loading.

## Negative paths

`SmtcServiceTests` verifies:

- `NoSessionReturnsEmptySnapshotSuccess` returns `SmtcSessionSnapshot.Empty` with
  `IsSuccess=true` and no error instead of throwing;
- `UnavailableTypedErrorIsForwardedWithoutThrowing` forwards
  `PlatformOperationErrorCode.Unavailable` from the adapter;
- `RepeatedReadsCallAdapterOncePerReadWithoutProcessState` proves there is no
  persistent helper process by asserting two reads produce exactly two adapter
  calls;
- pure helper tests cover empty metadata scoring and null genre fallback.

## Retired helper boundary

- `scripts/smtc_helper/` was removed from disk.
- `internalSMTCHelper/` was absent/removed.
- `Luminalium.sln` no longer lists `scripts\smtc_helper\SmtcHelper.csproj` and
  now lists `src\Luminalium.Smtc.Windows\Luminalium.Smtc.Windows.csproj`.
- `.gitignore` no longer carries helper-specific output exceptions.
- `tools/ForbiddenReferenceCheck.ps1` no longer allowlists the old external
  helper and now fails any direct `ProjectReference` to
  `Luminalium.Smtc.Windows`.
- The JSON-line protocol is retired with no stale source caller. The new Core
  service exposes typed snapshots and never reads or writes console JSON.

## Build / test / gate

```
dotnet build .\Luminalium.sln -c Release
=> 0 warnings, 0 errors

dotnet test .\Luminalium.sln -c Release
=> passed 25, failed 0, skipped 0

powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
=> Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Environment-dependent items not verified

- Live WinRT SMTC no-session/unavailable behavior depends on host OS media state
  and permissions; deterministic tests use fake adapters instead.
- Live WinRT SMTC session verification requires a real playing media session and
  is environment-dependent; it was not verified.
- App runtime loading is deferred to the consumer task, so startup behavior with
  optional assembly probing was not exercised here.
