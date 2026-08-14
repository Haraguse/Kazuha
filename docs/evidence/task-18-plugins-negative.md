# Task 18 - Operational Plugins (negative / fail-safe evidence)

Branch: `RyouYamada`

## Guarantees under test

- Timer: invalid input (`abc`, `00:-1:00`, `1:2:3:4`, empty) is rejected with a
  localized status and no countdown starts; terminating a running countdown
  cancels the token so no timer task outlives the window.
- Spotlight: with no selection the whole screen is dimmed; after clear the
  selection rect is null and dimming is restored.
- App launcher: a missing/empty path yields a typed `NotFound` result and NO
  process is ever started (the real `ProcessLaunchService` validates the path
  before `Process.Start`); duplicate paths are rejected.
- Status bar: empty presentation/media states render localized placeholders and
  never throw; the real sources degrade to empty snapshots on any monitor/SMTC
  failure.

All cases pass in `tests/Luminalium.Tests/OperationalPluginsTests.cs`
(full suite: passed 129, failed 0, skipped 0).

## Deliberately NOT claimed here

Real audio/notification delivery, live SMTC session state, actual process
launch side effects, and pixel-level spotlight rendering are environment-
dependent and belong to the Task 22 desktop QA harness. Tests use fakes so no
real process, audio, notification, or SMTC session is ever touched.