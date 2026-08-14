# Task 10 - WPS WebSocket bridge (negative / fail-safe evidence)

Branch: `RyouYamada`

## Guarantee under test

The host-side bridge rejects malformed or incompatible Luminalium2WPS wire
messages without disconnecting the active client, returns typed failures for
local host/request-tracker errors, and keeps the new WPS project inside the
plain `net10.0` Core-only dependency boundary.

## Negative paths

`WpsBridgeHostTests` verifies:

- invalid JSON receives a protocol-error envelope instead of crashing the host;
- unknown `message_type` receives a protocol-error envelope;
- schema-invalid `hello` missing `plugin_version` receives a protocol-error
  envelope with `payload.detail.validation_error` populated;
- after malformed messages, the same WebSocket connection can still send a valid
  `hello` and have it delivered through `OnMessageReceived`;
- a `/other` WebSocket connection is closed by the host without an application
  protocol error envelope;
- sending from the host while no client is connected returns typed
  `PlatformOperationErrorCode.Unavailable` with message
  `WPS bridge is not connected`;
- request tracker duplicate IDs, unknown completions, and timeouts return typed
  failures;
- schema codec rejects invalid JSON, non-object roots, missing or non-string
  `message_type`, unknown types, and duplicate discovered schema message types.

## Protocol error envelope

Malformed inbound messages produce the legacy-compatible error message shape:

```
{
  "message_type": "error",
  "payload": {
    "code": "INVALID_STATE",
    "message": "Invalid WPS bridge message",
    "detail": {
      "validation_error": "<formatted error>"
    }
  }
}
```

Validation errors are formatted as `jsonPath: message`, sorted by path, limited
to five entries, and suffixed with `... N more validation errors` when needed.

## Dependency and source-boundary checks

- `Luminalium.Wps` targets plain `net10.0` and references only
  `..\Luminalium.Core\Luminalium.Core.csproj`.
- No WebView, Python, Linux-only, VSTO, or WPS/Office automation package was
  introduced.
- `Luminalium2WPS` is absent from this branch by design and was not modified.
- The 13 schema files under `src\Luminalium.Wps\Protocol` were not modified.
  They are byte-identical copies of the Luminalium2WPS submodule protocol at
  commit `edb858c9ff5c59f56829d6c6c05e85a9f93e95cd`.

## Build / test / gate

```
dotnet build .\Luminalium.sln -c Release
=> 0 warnings, 0 errors

dotnet test .\Luminalium.sln -c Release
=> passed 32, failed 0, skipped 0

powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
=> Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Environment-dependent items not verified

- The TypeScript add-in build (`npm run build` in Luminalium2WPS) is
  environment-dependent and was not run on this branch. The add-in stays on
  `origin/default` untouched.
- Live WPS/Office integration was not exercised; the host bridge is verified via
  deterministic loopback WebSocket tests and a disposable manual driver.
