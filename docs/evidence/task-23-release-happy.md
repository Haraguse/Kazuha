# Task 23 Release Workflow Evidence

## Built

- `tools/Assert-ReleaseVersion.ps1` is a new machine-executable release metadata gate. It asserts the shipped `version.json` is present at the publish root and is a single valid JSON object with all required opaque string fields (`code_name`, `code_name_CN`, `version`, `versionnm`, `build`, `future_codename`). It reads with BOM/encoding detection (UTF-8 BOM / UTF-16 LE / UTF-16 BE / UTF-8 without BOM) so CJK field values decode correctly under PowerShell 5.1. This covers the Task 23 "corrupt version payload" scan.
- `.github/workflows/nightly-release.yml` now runs the full required CI gate chain on the publish output, in order:
  1. `Set-NightlyVersion` (version metadata)
  2. `dotnet publish` (win-x64, self-contained)
  3. `Assert-ReleaseVersion` (release version metadata gate — new)
  4. `Assert-ForbiddenPayload` (Python/PyInstaller/WebView/QML/VSTO/external-plugin scan)
  5. `Invoke-DesktopQASmoke -Scenario all` (desktop UI smoke gate — new, from Task 22)
  6. Upload desktop QA evidence artifact (`Luminalium-DesktopQA`)
  7. `New-NightlyPackage` (deterministic ZIP)
  8. Upload `Luminalium-Windows.zip` and update the `nightly` release

## Release QA (local equivalent of the CI chain)

```text
dotnet publish src/Luminalium.App/Luminalium.App.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishProfile=WindowsNightly -p:PublishDir=artifacts/publish
已成功生成。    0 个警告    0 个错误
Removing 8 debug symbol file(s) from publish output.
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-ReleaseVersion.ps1 -PublishDir src/Luminalium.App/artifacts/publish
Release version check passed: version.json is valid JSON with all required opaque fields (code_name, code_name_CN, version, versionnm, build, future_codename).
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-ForbiddenPayload.ps1 -PublishDir src/Luminalium.App/artifacts/publish
Forbidden payload check passed: 235 file(s) and 0 directory(ies) ... contain no Python/PyInstaller, PySide/PyQt, Qt/QML, WebView2, VSTO, Kazuha bridge or external-plugin payloads.
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-DesktopQASmoke.ps1 -AppPath src/Luminalium.App/artifacts/publish/Luminalium.exe -Scenario all -EvidenceDir artifacts/qa-evidence -LaunchTimeoutSeconds 90
Desktop QA harness result: 8 passed, 0 failed.   (shell, cjk, touch, dpi, multi, theme, noslide, corrupt)
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/New-NightlyPackage.ps1 -PublishDir src/Luminalium.App/artifacts/publish -OutputPath artifacts/Luminalium-Windows.zip -Date 2026-08-15
Nightly package created: artifacts/Luminalium-Windows.zip
  entries: 235 (sorted by normalized relative path), uncompressed bytes: 114253784
  fixed timestamp: 08/15/2026 00:00:00 (all entries)
  SHA256: 64576F0714924DA7F53C349A61D6C95B983D852608EE4B2699310FBF3B515324
```

## Package runnability check

The exact `Luminalium-Windows.zip` was extracted to a fresh directory and the
package executable was launched against it:

```text
System.IO.Compression.ZipFile.ExtractToDirectory(Luminalium-Windows.zip, artifacts/zip-verify)
Luminalium.exe present: True

powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-DesktopQASmoke.ps1 -AppPath artifacts/zip-verify/Luminalium.exe -Scenario shell -EvidenceDir artifacts/qa-evidence
Desktop QA harness result: 1 passed, 0 failed.
```

The released package is a clean, runnable bundle: main window appears with the
exact title "Luminalium", navigation present, screenshot captured, clean
shutdown (ExitCode 0).

## Verification

```text
dotnet build Luminalium.sln -c Release
已成功生成。    0 个警告    0 个错误
```

```text
dotnet test Luminalium.sln -c Release --no-build
已通过! - 失败:     0，通过:   347，已跳过:     0，总计:   347，持续时间: 2 s - Luminalium.Tests.dll (net10.0)
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Environment-Dependent Declarations

- The desktop UI smoke gate needs an interactive desktop session; on GitHub-hosted Windows runners the harness runs in a headless-safe mode (no Office/WPS required) and asserts typed HostUnavailable / no-slideshow states rather than crashing.
- The ZIP SHA-256 is deterministic for identical publish output + the fixed `-Date 2026-08-15`; a nightly publish with a different date yields a different hash by design.
