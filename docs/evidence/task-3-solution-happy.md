# Task 3 - Solution build/test evidence (happy path)

Date: 2026-08-13 | Host: Windows | SDK: 10.0.302 (global.json pin)
Solution: `csharp/Luminalium.sln` (5 src projects + 1 xUnit test project + pre-existing `scripts/smtc_helper/SmtcHelper.csproj`)

## dotnet restore

```
dotnet restore csharp\Luminalium.sln
```
Exit code: **0** - all projects restored; package graph fully central-managed via `csharp/Directory.Packages.props` (Avalonia 12.1.1, FluentAvaloniaUI 3.0.2, CommunityToolkit.Mvvm 8.4.2, xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1).

## dotnet build

```
dotnet build csharp\Luminalium.sln --configuration Release --no-restore
```
Exit code: **0**
- Warnings: **0**
- Errors: **0**
- `Luminalium.Core`, `Luminalium.Plugins`, and `Luminalium.Updater` build against plain `net10.0`; `Luminalium.App`, `Luminalium.Platform.Windows`, and `Luminalium.Tests` retain `net10.0-windows10.0.17763.0` with the Windows 1809 floor.

## dotnet test

```
dotnet test csharp\Luminalium.sln --configuration Release --no-build
```
Exit code: **0** | Discovered: 1 test file | Passed: **4** | Failed: **0** | Skipped: **0**

Scaffold tests (`csharp/tests/Luminalium.Tests/SolutionScaffoldTests.cs`) cover solution identity, Windows floor constant, assembly boundary resolution, and host floor check. The host-floor test is guarded (`OperatingSystem.IsWindowsVersionAtLeast(10,0,17763)`) so it cannot falsely fail on unsupported hosts (non-Windows CI or Windows < 1809).

## Generated-output ignore coverage

`.gitignore` lines 17-20, verified with `git check-ignore` (exit 0):

| Pattern | Sample path | Ignored by |
|---|---|---|
| `csharp/**/bin/` | `csharp/src/Luminalium.App/bin/x.dll` | `.gitignore:17` |
| `csharp/**/obj/` | `csharp/src/Luminalium.App/obj/x.nuget.g.props` | `.gitignore:18` |
| `csharp/**/publish/` | `csharp/src/Luminalium.App/publish/x.dll` | `.gitignore:19` |
| `csharp/artifacts/` | `csharp/artifacts/x.dll` | `.gitignore:20` |

## Acceptance criteria

- [x] `dotnet sln list` shows all declared projects and `dotnet build <solution>` succeeds (exit 0, 0/0).
- [x] `dotnet test <solution>` discovers the xUnit project; 4 scaffold tests pass.
- [x] No WebView, Python, Linux-only, or deprecated VSTO package referenced (see task-3-solution-negative.md).
- [x] `git status --short` excludes `bin/`, `obj/`, and publish output (gitignore verified above).
