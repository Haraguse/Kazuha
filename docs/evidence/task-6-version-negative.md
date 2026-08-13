# Task 6 - Version / Identity Metadata (negative / fail-safe evidence)

Branch: `RyouYamada`

## Guarantee under test

Missing or malformed `version.json` must produce a deterministic, typed error
carrying field/path context and must never crash the process or fabricate
version data. The shell falls back to "Version metadata unavailable".

`VersionMetadataReader.Load` returns `VersionMetadataLoadResult`; it does not
throw to callers and never numeric-parses the opaque version string.

## Typed error surface

`VersionMetadataErrorCode`:
- `FileNotFound`  - path does not exist
- `ReadFailure`   - IO / access failure while reading
- `InvalidRoot`   - root JSON is not an object
- `MalformedJson` - JSON parse failure (carries line + byte position)
- `MissingField`  - required field absent (carries `Field`)
- `InvalidFieldType` - required field is not a JSON string (carries `Field`)
- `EmptyField`    - required field is empty (carries `Field`)

Every `VersionMetadataError` carries `Code`, `Path`, `Message`, and optional `Field`.

## xUnit coverage (all passing, part of the 9/9 suite)

- `MissingVersionReturnsTypedFieldError`
  - fixture omits `version`
  - asserts `Code == MissingField`, `Field == "version"`, `Path == fixture path`
- `MalformedJsonReturnsLocationAwareError`
  - fixture `{not-json`
  - asserts `Code == MalformedJson`, message contains line info, `Path == fixture path`
- `MissingFileReturnsTypedPathError`
  - non-existent path
  - asserts `Code == FileNotFound`, `Path == requested path`

```
dotnet test .\Luminalium.sln -c Release  => passed 9, failed 0, skipped 0
```

## Result

No fabricated version is ever displayed on failure; the reader reports a typed,
located error and the UI shows the neutral unavailable message.
