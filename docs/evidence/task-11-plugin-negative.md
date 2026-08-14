# Task 11 - Built-in Plugin Contract & Registry (negative evidence)

Branch: `RyouYamada`

## Guarantees under test

- A plugin whose `ExecuteAsync` is called twice concurrently receives a typed
  activation error; `TerminateAsync` stays idempotent.
- A duplicate id registration is rejected with a typed `DuplicateRegistrationError`;
  `Get(id)` still returns the ORIGINAL registration.
- Null / whitespace ids are rejected with typed validation errors (no exceptions escape).
- `TerminateAllAsync` terminates every plugin even when one plugin's `TerminateAsync`
  throws; the failure is collected in a typed aggregate result and the remaining
  plugins still terminate exactly once.
- Empty DisplayName (settings) and empty IconKey (status_bar) are allowed and
  do NOT fail registration.

All cases pass in `tests/Luminalium.Tests/BuiltInPluginRegistryTests.cs`
(full suite: passed 48, failed 0, skipped 0).

## Deliberately NOT claimed here

Real timer / spotlight / board / settings / logs / onboarding / status-bar / launcher
behavior and their native views are implemented in Tasks 17-19. The catalog ships
placeholder commands (typed `NotSupported`) and placeholder views only.
No external plugin discovery, no Python import, no public SDK, no phone remote.
