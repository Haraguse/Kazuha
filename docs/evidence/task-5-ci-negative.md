# Task 5 - CI negative-path evidence

Date: 2026-08-13 | Host: Windows

## Tool self-tests

`csharp/tools/New-NightlyPackage.ps1 -SelfTest` passed all temporary-fixture cases and removed its fixture tree:

- Positive publish tree: exit 0.
- Repeated identical input/date: exit 0 twice; SHA256 identical at `99BC3F4F991EA64C59B867D9A97566A3AF5EDBC6086EE12CC692796747CD6559`.
- Missing `Luminalium.exe`: exit 1.
- Missing `PublishDir`: exit 2.
- Existing output ZIP inside `PublishDir`: exit 0; output was excluded from itself.
- Missing output-directory parent: exit 2; no directory chain invented.

`csharp/tools/Set-NightlyVersion.ps1 -SelfTest` also passed valid, malformed, missing-field, opaque-string, and temporary-fixture cleanup cases.

## Forbidden payload gate

`csharp/tools/Assert-ForbiddenPayload.ps1` returns exit 1 when a publish tree contains legacy payload sentinels such as Python/PyInstaller files, QML, WebView2, VSTO, `plugins/`, or `external/`. A clean 228-file Windows publish tree returned exit 0.

## Workflow failure gates

- `actions/upload-artifact@v4` uses `if-no-files-found: error`, so a missing ZIP fails the build.
- `ncipollo/release-action@v1.21.0` uses `artifactErrorsFailBuild: true`, so a missing or unreadable release asset fails the release job.
- The workflow has no Linux job or Linux artifact input; Linux remains Deferred/Staged rather than silently shipping an unverified artifact.
