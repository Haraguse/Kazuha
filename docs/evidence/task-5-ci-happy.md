# Task 5 - Windows-first CI and package evidence

Date: 2026-08-13 | Host: Windows | SDK: 10.0.302
Workflow: `.github/workflows/nightly-release.yml`

## Contract

- `build-windows` runs on `windows-latest` and uses `csharp/global.json`.
- The release path is `.NET restore -> build -> test -> publish -> payload scan -> deterministic ZIP`.
- The current release has one artifact, `Luminalium-Windows.zip`, under the preserved `nightly` tag.
- Linux build/release steps are removed from the current release path and recorded as Deferred/Staged for a later platform phase; Linux is not claimed as implemented.
- `Luminalium2WPS` remains a separate compatibility boundary.

## Local equivalent

```text
dotnet restore csharp/src/Luminalium.App/Luminalium.App.csproj -r win-x64
dotnet publish csharp/src/Luminalium.App/Luminalium.App.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishProfile=WindowsNightly -p:PublishDir=csharp/artifacts/publish
powershell -NoProfile -ExecutionPolicy Bypass -File csharp/tools/Assert-ForbiddenPayload.ps1 -PublishDir csharp/artifacts/publish
powershell -NoProfile -ExecutionPolicy Bypass -File csharp/tools/New-NightlyPackage.ps1 -PublishDir csharp/artifacts/publish -OutputPath csharp/artifacts/Luminalium-Windows.zip -Date 2026-08-13
```

Results:

- Restore: exit 0; RID-specific `win-x64` assets restored.
- Publish: exit 0; 0 warnings and 0 errors; `Luminalium.exe` present.
- Payload scan: exit 0; 228 files scanned, 0 forbidden payload violations.
- Package: exit 0; 228 sorted entries; `Luminalium-Windows.zip` created.
- SHA256: `12A3390B047F7612017223E1F63D74D224D399D70CE88BA0CB2EE053830B9CAA`.
- Self-contained directory output retained Avalonia native libraries and did not use `--no-build`.

## Workflow structure

The parsed workflow contains exactly two jobs: `build-windows` and `release`. The release job depends only on `build-windows`, downloads the named artifact to `release/`, verifies the exact ZIP path, and uses `ncipollo/release-action@v1.21.0` with `artifactErrorsFailBuild: true` and `replacesArtifacts: true`.
