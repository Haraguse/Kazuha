# Task 13 Updater Negative Path Evidence

## Built-In Safe States

- Dev-mode detection returns a non-available update status before feed polling when `.dev` exists next to the app executable or the layout is not a published `Luminalium.exe` layout.
- A held update mutex returns typed `UpdateErrorCode.MutexHeld`; a second replacement cannot proceed concurrently.
- Staged content is validated before any install file is backed up or copied.
- Checksum mismatch returns typed `UpdateErrorCode.ChecksumMismatch` from download before replacement can be called.
- Structurally invalid zips return typed `UpdateErrorCode.ValidationFailed`; the original installation remains untouched.
- Replacement copy failures trigger rollback from the retained backup. Successful rollback returns typed `UpdateErrorCode.RollbackSucceeded` with the original files restored byte-for-byte.
- Running app timeout and replacement lock retries report typed failures instead of deleting or overwriting the current installation.

## Deterministic Negative Tests

- `UpdateServiceTests.BadChecksumFailsBeforeReplacementAndKeepsOriginalFiles` serves a valid archive with a bad `.sha256` sidecar on `127.0.0.1`; download fails with `ChecksumMismatch`, and temp install files remain unchanged.
- `UpdateServiceTests.StructurallyInvalidZipFailsValidationBeforeReplacement` uses a zip missing root `Luminalium.exe`; validation fails with `ValidationFailed`, and temp install files remain unchanged.
- `UpdateServiceTests.ReplacementFailureRestoresOriginalFilesAndReturnsRollbackResult` injects an install-copy failure through `IUpdateFileSystem`; the coordinator restores old `Luminalium.exe` and `version.json`, deletes the partially introduced new file, and returns `RollbackSucceeded`.
- `UpdateServiceTests.ConcurrentReplacementReturnsTypedMutexFailure` starts one coordinator replacement under a named mutex and verifies a second coordinator returns `MutexHeld`.
- `UpdateServiceTests.FeedPreservesOpaqueVersionSemantics` verifies opaque strings are never numeric-parsed: equal `1.4.0.9-EMERGENCY` is up to date, forced equal is available, differing opaque tag is available, and `nightly` is available when current is a different nightly-shaped string.

## Verification

```text
dotnet build Luminalium.sln -c Release
已成功生成。
    0 个警告
    0 个错误
```

```text
dotnet test Luminalium.sln -c Release
已通过! - 失败:     0，通过:    63，已跳过:     0，总计:    63，持续时间: 997 ms - Luminalium.Tests.dll (net10.0)
```

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.
```

## Environment-Dependent Declarations

- Live GitHub release polling and mirror fallback were not verified against the real `SECTL/Luminalium` release feed in this task run.
- Real Windows file-lock behavior against a live installed Luminalium executable was not verified; lock-sensitive replacement is represented by deterministic process-wait, mutex, retry, and file-system fault-injection tests.
- One parallel invocation of `tools/ForbiddenReferenceCheck.ps1` crashed in Windows PowerShell 5.1 AMSI before script execution; rerunning the exact required command sequentially passed.
