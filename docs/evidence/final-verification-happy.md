# Final Verification Waves (F1-F4) Evidence

## Scope

Final verification waves of the native built-in feature migration and the
project plan (T1-T25). Applies to branch `RyouYamada` at HEAD
(`8755058 test: make update dialog progress assertion deterministic (F2)`),
which is `origin/RyouYamada` (f65401a) plus 7 local commits (T20..T25 + F2 fix).

## F1 - Plan Compliance Audit

- All 15 migration plan tasks (T1-T15) implemented; one atomic commit per
  slice, messages matching the plan verbatim, in dependency order. Commit
  mapping is recorded in `artifacts/feature-migration/final/F1-F4-audit.txt`
  (T1 `3ca6326` ... T15 `0928a4f`).
- Pre-existing documented finding: the T6 slice has no standalone commit; its
  ShellViewModel typed-state work was folded into T7 `670ab2c`. The T6
  deliverable is functionally present (ShellViewModel routes entirely via
  typed `BuiltInFeatureId` / `BuiltInFeatureCatalog`). History not rewritten.
- Must-have / must-not-have verified by the migration gate itself:
  `NativeFeatureMigrationGateTests.GatePassesAgainstActualRepositoryAfterNativeMigration`
  evaluates the real repository at HEAD and passes with zero findings. The gate
  blocks built-in plugin registration, placeholder command/view invocation,
  direct native window construction outside the host factory, out-of-seam
  legacy-projection consumption, and internal `plugin:` alias emission.
- `Luminalium.Plugins` csproj references only `Luminalium.Core` (no App
  reference); `ExternalPluginBoundaryTests` asserts the assembly does not
  reference `Luminalium.App`.

## F2 - Code Quality Review

```text
dotnet build Luminalium.sln -c Release --no-incremental
已成功生成。    0 个警告    0 个错误

dotnet test Luminalium.sln -c Release --no-build
已通过! - 失败:     0，通过:   347，已跳过:     0，总计:   347 - Luminalium.Tests.dll

powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no
plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference,
and all references resolve inside the repository root.

dotnet publish src/Luminalium.App/Luminalium.App.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishProfile=WindowsNightly -p:PublishDir=artifacts/publish
已成功生成。    0 个警告    0 个错误
Removing 8 debug symbol file(s) from publish output.

powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-ReleaseVersion.ps1 -PublishDir src/Luminalium.App/artifacts/publish
Release version check passed: version.json is valid JSON with all required opaque fields
(code_name, code_name_CN, version, versionnm, build, future_codename).

powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-ForbiddenPayload.ps1 -PublishDir src/Luminalium.App/artifacts/publish
Forbidden payload check passed: 235 file(s) and 0 directory(ies) ... contain no
Python/PyInstaller, PySide/PyQt, Qt/QML, WebView2, VSTO, Kazuha bridge or external-plugin payloads.
```

- C# quality: static baseline (Capture-MigrationBaseline) shows 0
  `CreateDefaultRegistry` hits, no production placeholder invocation, direct
  native window construction only inside `NativeBuiltInFeatureHostFactory`;
  `NativeFeatureMigrationGate.Evaluate(real repo)` PASS.
- F2 deterministic fix: `UpdateDialogAppliesStagedUpdateAndRequestsRestart`
  now uses an inline `SynchronizationContext` so progress reports arrive
  synchronously (commit `8755058`); the full suite was rerun green twice.

## F3 - Real Manual QA

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-DesktopQASmoke.ps1 -AppPath src/Luminalium.App/artifacts/publish/Luminalium.exe -Scenario all -EvidenceDir artifacts/qa-evidence -LaunchTimeoutSeconds 90
Running scenario: shell / cjk / touch / dpi / multi / theme / noslide / corrupt
Desktop QA harness result: 8 passed, 0 failed.
JSON evidence: .../desktop-qa-20260815T235522.json
```

- COM environment status captured in the evidence record:
  `Office=False WPS=False`. No live Office/WPS COM object is present on this
  machine; the `noslide` / `corrupt` scenarios assert typed
  `HostUnavailable` / no-slideshow states instead of crashing
  (`src/Luminalium.Presentation/PresentationErrors.cs`).
- All eight built-ins exercised via committed tests: catalog, host lifecycle
  (board/timer/spotlight/app_launcher/status_bar), shell navigation, and route
  parse/emit incl. unknown-route `NotFound`.

## F4 - Scope Fidelity Check

- Final committed diff (`f65401a..HEAD`) is limited to migration
  implementation files, project tasks T20-T25 (updater integration, tests,
  desktop QA harness, CI, retirement, `docs/RUNBOOK.md`), and
  evidence/tools/workflow files. Full file list reviewed.
- The paused defect-fix batch (App.axaml.cs, DialogService.cs worktree delta,
  LocalLogService.cs, RetryCloseDialogTypes.cs, RetryCloseDialogViewModel.cs,
  SettingsViewModel.cs, WpsBridgeAutomationAdapter.cs, OnboardingAndLogsTests.cs,
  StartupErrorCoordinatorTests.cs, WpsBridgeHostTests.cs, AsyncSerialGate.cs)
  was verified to NOT appear in any migration/project commit and remains
  uncommitted in the working tree. The T20 commit's `DialogService.cs` change
  (update-dialog result type) is disjoint from the worktree delta (RetryClose
  handling) — no mixing.
- No placeholders reachable through user-facing native paths; external plugin
  contracts retained independently; compatibility alias (`plugin:<id>` input
  parsing + projection) retained for the defined one-release boundary.

## Result

All four waves pass. Success criteria: built-ins owned by the App (not plugin
registry), external plugin contracts independent, existing IDs + one-release
legacy aliases compatible, typed native ownership/lifecycle centralized, no
placeholder reachable, and full Release tests/build/publish/static/runtime
verification green.
