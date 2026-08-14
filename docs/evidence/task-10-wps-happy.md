# Task 10 - WPS WebSocket bridge (positive evidence)

Branch: `RyouYamada`

## Contract

- Added `src/Luminalium.Wps` as a plain `net10.0` class library with nullable
  and implicit usings enabled. It references `Luminalium.Core` only.
- Added central package pin `JsonSchema.Net` `9.4.0`. The version was selected
  after `dotnet package search JsonSchema.Net --take 5 --format json` reported
  `latestVersion` `9.4.0` from nuget.org; `dotnet list` resolved the requested
  and installed top-level package to `9.4.0`.
- The 13 WPS protocol schemas are embedded via
  `<EmbeddedResource Include="Protocol\*.schema.json" />` and loaded from the
  `Luminalium.Wps` assembly manifest resource stream at runtime.
- The embedded schemas are treated as the compatibility source of truth and were
  not modified. They are byte-identical copies of the Luminalium2WPS submodule
  protocol at commit `edb858c9ff5c59f56829d6c6c05e85a9f93e95cd`.
- `WpsProtocol` discovers 12 message types from schema `message_type` `const` or
  `enum` values, rejects duplicate message types during construction, generates
  32-character lowercase hex GUID message IDs, and validates JSON Schema draft
  2020-12 with cross-file `$ref` registry support and `unevaluatedProperties`.
- `WpsBridgeHost` scans ports `3892..3902` inclusively, serves
  `ws://127.0.0.1:{port}/ws`, is idempotent when already listening, accepts one
  client at a time, replaces the prior client on a second connection, serializes
  outbound sends per connection, and releases the bound port after `StopAsync`.
- `WpsRequestTracker` provides request correlation with duplicate, unknown, and
  timeout typed-result paths.

## xUnit coverage

`WpsBridgeHostTests` verifies:

- `HappyPathBindsDefaultRangeReceivesHelloAndSendsEnvelope`: host binds in
  `3892..3902`, client connects to `/ws`, a valid `hello` is decoded and raised
  through `OnMessageReceived`, and `SendAsync` reaches the client with exact
  `message_type`, `message_id`, `ts`, and object `payload` fields.
- `CorrelationUsesExplicitMessageIdsAndTrackerCompletesOrTimesOut`: explicit
  outbound IDs are preserved, direct tracker completion correlates a
  `command_result` payload by command ID, duplicate IDs fail, unknown IDs fail,
  and timeouts return typed failures.
- `SecondConnectionReplacesFirstClient`: a second `/ws` connection closes the
  first client and becomes the active connection.
- `StopClosesClientAndReleasesPortForImmediateRebind`: start is idempotent,
  send-without-client returns typed `Unavailable`, `StopAsync` closes the client,
  restarting on the same port succeeds, and an independent `HttpListener` can
  bind the released port after final stop.
- `SchemaCodecDiscoversValidatesAndRoundTripsContractTypes`: encode/decode
  round-trip, generated defaults, unknown type, invalid JSON, non-object root,
  missing `message_type`, expected 12 message types, and duplicate detection.

## Build / test / gate

```
dotnet build .\Luminalium.sln -c Release
=> 0 warnings, 0 errors

dotnet test .\Luminalium.sln -c Release
=> passed 32, failed 0, skipped 0

powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
=> Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Manual surface check

```
dotnet run -c Release
=> start=True;port=3892
=> received=hello
=> send=True;id=abcdefabcdefabcdefabcdefabcdefab
=> client=presentation_state_get;id=abcdefabcdefabcdefabcdefabcdefab
=> stop=True
=> rebind=True
=> final_stop=True
```

The temporary driver lived under `%TEMP%\opencode`, referenced
`src\Luminalium.Wps\Luminalium.Wps.csproj`, used a real `ClientWebSocket`, and
was removed after the run.

## Environment-dependent items not verified

- The TypeScript add-in build (`npm run build` in Luminalium2WPS) is
  environment-dependent and was not run on this branch. The add-in stays on
  `origin/default` untouched.
- No WPS/Office installation is required for this task; verification uses only
  loopback WebSocket traffic on `127.0.0.1`.
