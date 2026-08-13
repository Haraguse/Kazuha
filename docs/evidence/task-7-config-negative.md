# Task 7 - Versioned Configuration (negative / fail-safe evidence)

Branch: `RyouYamada`

## Guarantee under test

Malformed or semantically invalid C# configuration never reaches callers as an
unhandled JSON exception. The service returns a typed `ConfigurationLoadResult`
with a default model and `ConfigurationLoadWarning` containing a code, path,
message, optional field, and (for corruption) a backup path.

## Corruption recovery

`CorruptJsonIsBackedUpAndReturnsStructuredDefaultWarning` writes `{not-json` to
the active `settings.json`, calls `Load`, and verifies:

- the result is `RecoveredDefault`, not a successful load;
- warning code is `MalformedJson` and warning path is the original file;
- a sibling `.bak` recovery artifact exists;
- the artifact bytes exactly equal the original corrupt bytes;
- the original live file remains byte-for-byte unchanged before any later save;
- the returned default has `SchemaVersion=1`.

The service never destructively overwrites the corrupt file while backing it up.
If backup itself fails, the typed warning retains the original path and explains
the backup failure rather than swallowing the exception.

`PlaintextPasswordIsRejectedWhenProtectionIsEnabled` verifies that an enabled
password-protection section cannot save `plain-text-password`, and that no live
settings file is created by the rejected save.

## Validation boundary

The service requires the `SchemaVersion` metadata field and rejects unsupported
schema versions, null required sections, an enabled password-protection section
without a salted `PasswordHash`, and non-empty password values that lack an
algorithm-prefixed salted-hash shape. Unknown JSON properties are skipped by
the configured `System.Text.Json` options, while type mismatches, invalid enum
strings, malformed JSON, and unsupported values recover to defaults with
structured warnings. The malformed JSON and plaintext-password paths are
directly exercised by the Task 7 tests; the remaining validation branches are
implementation-level safeguards pending future targeted cases.

## Build / test

```
dotnet build .\Luminalium.sln -c Release
=> 0 warnings, 0 errors

dotnet test .\Luminalium.sln -c Release
=> passed 14, failed 0, skipped 0
```

The final solution suite includes the original 9 tests plus 5 Task 7
configuration tests. The service only accesses the constructor-injected
settings directory; no Python `settings.json` or `config\\*.json` file is read,
created, or modified by the service or these tests.
