# Task 24 Legacy Retirement Evidence

## Retirement (T24 退役)

The legacy production paths listed in the Task 24 retirement map
(`docs/LEGACY_CAPABILITY_INVENTORY.md` §4) were already retired by the
preceding tasks, and a fresh full-tree scan confirms nothing remains:

```text
Source-tree legacy scan: no .py/.pyw/.qml/.js/.ts/.vsto or webview files found anywhere on disk.
Tracked legacy references (git ls-files): 0
```

Specifically retired and confirmed absent from the tracked tree and the
working tree:

- `main.py`, `build_pyinstaller.py`, `Luminalium.spec`, `pyproject.toml`, `requirements.txt`, `uv.lock`
- `plugins/webview_runner.py`, `plugins/qwebchannel.js`, `plugins/webview_window_utils.py`, `plugins/in_process_window_handle.py`
- `ppt_assistant/` (HTML/QML/SMTC/Python runtime surfaces), `plugins/builtins/*/*.html`, `plugins/builtins/board/*.qml`, `LinuxOverlay.qml`
- `ppt_assistant/rendering/` experimental backends, `plugins/external/` external discovery
- `scripts/ppt_vsto_bridge/deploy/` (documented, never consumed)
- Python/PySide/Linux steps in `.github/workflows/nightly-release.yml` (replaced by the Windows-only .NET workflow, Task 5/23)

The only remaining textual matches for legacy terms are the catalog /
consistency documents themselves (`LEGACY_CAPABILITY_INVENTORY.md`,
`FEATURE_PARITY_MATRIX.md`), which are intentionally preserved as the
retirement record — never as implementation deliverables.

## Preservation (T24 退役保护)

Confirmed intact and untouched by the retirement:

- WPS architecture: `src/Luminalium.Wps/` (host, protocol, JSON schemas) + `src/Luminalium.Presentation/WpsBridgeAutomationAdapter.cs` + `tests/Luminalium.Tests/WpsBridgeHostTests.cs`
- Version metadata: `version.json` (opaque `code_name` / `code_name_CN` / `version` / `versionnm` / `build` / `future_codename`)
- Consistency matrix: `docs/FEATURE_PARITY_MATRIX.md`, `docs/LEGACY_CAPABILITY_INVENTORY.md`
- Tests: `tests/Luminalium.Tests/` (347 tests)
- Assets: `src/Luminalium.App/Assets/logo.ico`, product identity
- User data / config / splash payloads continue to be produced at runtime by the C# app

## Retirement QA (T24 退役 QA)

### Source tree scan

```text
# manual recursive scan (Get-ChildItem -Recurse -File | legacy-extension filter)
Source-tree legacy scan: no .py/.pyw/.qml/.js/.ts/.vsto or webview files found anywhere on disk.
Tracked legacy references (git ls-files): 0
```

### Publish tree scan

The clean C# publish (`src/Luminalium.App/artifacts/publish`, 235 files) was
scanned with the machine-executable forbidden-payload gate that also runs in CI:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-ForbiddenPayload.ps1 -PublishDir src/Luminalium.App/artifacts/publish
Forbidden payload check passed: 235 file(s) and 0 directory(ies) ... contain no Python/PyInstaller, PySide/PyQt, Qt/QML, WebView2, VSTO, Kazuha bridge or external-plugin payloads.
```

### Clean C# publish launch

The retired publish tree was launched through the full desktop QA harness
(no Office/WPS, headless-safe mode) covering all eight scenarios:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-DesktopQASmoke.ps1 -AppPath src/Luminalium.App/artifacts/publish/Luminalium.exe -Scenario all -EvidenceDir artifacts/qa-evidence -LaunchTimeoutSeconds 90
Running scenario: shell / cjk / touch / dpi / multi / theme / noslide / corrupt
Desktop QA harness result: 8 passed, 0 failed.
```

The app launches as a clean .NET/Avalonia bundle with no legacy runtime path:
no Python/PyInstaller runtime, no Qt/WebEngine/WebView2, no QML/HTML payload.

## Verification

```text
dotnet build Luminalium.sln --configuration Release
已成功生成。    0 个警告    0 个错误
```

```text
dotnet test Luminalium.sln --configuration Release --no-build
已通过! - 失败:     0，通过:   347，已跳过:     0，总计:   347，持续时间: 1 s - Luminalium.Tests.dll (net10.0)
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Environment-Dependent Declarations

- The desktop UI smoke gate needs an interactive desktop session; on
  GitHub-hosted Windows runners the harness runs in a headless-safe mode (no
  Office/WPS required) and asserts typed HostUnavailable / no-slideshow states
  rather than crashing.
- COM environment state remains explicit: with no Office/WPS host present the
  harness verifies the no-slideshow path rather than asserting a fake
  slideshow.
