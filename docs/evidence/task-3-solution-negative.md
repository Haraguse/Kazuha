# Task 3 - Forbidden dependency / source-boundary evidence (negative path)

Date: 2026-08-13 | Gate: `csharp/tools/ForbiddenReferenceCheck.ps1` (machine-executable, PowerShell 5.1)

## Run on the real tree

```
powershell -NoProfile -ExecutionPolicy Bypass -File csharp\tools\ForbiddenReferenceCheck.ps1
```
Exit code: **0** - "Forbidden reference check passed: no forbidden dependencies, no Linux-only project, all references resolve inside csharp/ except scripts\smtc_helper\SmtcHelper.csproj."

Scanned: every `*.csproj` under `csharp/` (src, tests, validation; generated `bin/obj/publish/artifacts` trees excluded) plus `csharp/Luminalium.sln` project entries.

## Rules enforced

1. **Forbidden dependency tokens** in `PackageReference`/`ProjectReference` `Include` (case-insensitive): `WebView`, `Python.Runtime`, `IronPython`, `PySide`, `Kazuha.PowerPointBridge`, `VSTO`.
2. **No Linux-only TargetFramework** - production cross-platform project is forbidden in the new stack.
3. **ProjectReference source boundary** - must resolve inside `csharp/` or to the single pre-existing external helper `scripts/smtc_helper/SmtcHelper.csproj` (anchored to repository root, not the scanned root).
4. **Solution boundary** - `Luminalium.sln` may reference projects inside `csharp/` plus `scripts\smtc_helper\SmtcHelper.csproj` only.

## Findings on the real tree

- Forbidden dependency matches: **0**
- Linux-only TFMs: **0**
- Boundary escapes: **0** (the only external reference is the allowed `scripts/smtc_helper/SmtcHelper.csproj`)

## Negative fixture (gate proves it fails when violations exist)

Synthetic tree with a `net10.0-linux` project referencing `Avalonia.WebView`, `Python.Runtime`, and an out-of-tree `ProjectReference`:

```
FORBIDDEN REFERENCE CHECK FAILED (4 violation(s)):
  - ...\Bad.csproj: Linux-only TargetFramework 'net10.0-linux'...
  - ...\Bad.csproj: forbidden PackageReference 'Avalonia.WebView' matches token 'WebView'
  - ...\Bad.csproj: forbidden PackageReference 'Python.Runtime' matches token 'Python.Runtime'
  - ...\Bad.csproj: ProjectReference '..\..\outside\Legacy.csproj' escapes csharp/ boundary...
```
Exit code: **1** (fixture removed after the run).

## Acceptance criteria

- [x] No WebView, Python, Linux-only, or deprecated VSTO package is referenced.
- [x] Task 3 "Forbidden dependencies are rejected" QA scenario is machine-executable via the gate above.
