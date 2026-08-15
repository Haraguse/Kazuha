# Task 25 Documentation Evidence

## Built

- `docs/RUNBOOK.md` — new handoff runbook (Chinese) covering the seven required
  areas: solution structure, SDK/toolchain, commands, release, configuration,
  exclusions (deferred/excluded features and their warnings), and COM
  prerequisites.

## Clean-checkout command validation

The documented command sequence was executed at the repository root and each
command's output matched the runbook:

```text
dotnet restore Luminalium.sln
正在确定要还原的项目…
所有项目均是最新的，无法还原。   (exit 0)

dotnet build Luminalium.sln --configuration Release
已成功生成。    0 个警告    0 个错误

dotnet test Luminalium.sln --configuration Release --no-build
已通过! - 失败:     0，通过:   347，已跳过:     0，总计:   347 - Luminalium.Tests.dll (net10.0)

powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
Forbidden reference check passed: no forbidden dependencies, no Linux-only project,
no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference,
and all references resolve inside the repository root.

powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-ForbiddenPayload.ps1 -PublishDir src/Luminalium.App/artifacts/publish
Forbidden payload check passed: 235 file(s) and 0 directory(ies) ... contain no
Python/PyInstaller, PySide/PyQt, Qt/QML, WebView2, VSTO, Kazuha bridge or external-plugin payloads.

powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-DesktopQASmoke.ps1 -AppPath src/Luminalium.App/artifacts/publish/Luminalium.exe -Scenario all -EvidenceDir artifacts/qa-evidence -LaunchTimeoutSeconds 90
Running scenario: shell / cjk / touch / dpi / multi / theme / noslide / corrupt
Desktop QA harness result: 8 passed, 0 failed.
```

## Deferred / excluded feature warnings asserted

- No Office/WPS host present: the desktop QA `noslide` / `corrupt` scenarios
  pass through the typed no-slideshow path rather than crashing; the runbook
  documents `HostUnavailable` / `SlideshowClosed` as the expected typed states
  (`src/Luminalium.Presentation/PresentationErrors.cs`).
- X-01 Linux deferred, X-02 external plugins excluded, X-04 Python config
  import excluded, X-05 WebView/HTML/QML retired, X-06 experimental rendering
  excluded, X-07 Python runtime removed — all documented in the runbook §6 and
  cross-referenced to `docs/FEATURE_PARITY_MATRIX.md` /
  `docs/LEGACY_CAPABILITY_INVENTORY.md`; the T24 full-tree scan confirmed 0
  remaining legacy runtime paths.
- The forbidden-payload scan (also in CI) flags any Python/PySide/Qt/QML/
  WebView2/VSTO/external-plugin payload, so deferred/excluded surfaces cannot
  re-enter the release tree.

## Verification

Same gates as Task 24: Release build 0 warnings / 0 errors, 347/347 tests,
`ForbiddenReferenceCheck` pass, publish-tree forbidden-payload pass, desktop QA
8/8 pass.
