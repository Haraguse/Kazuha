# Task 6 - Version / Identity Metadata (positive evidence)

Branch: `RyouYamada`
Host: Windows 11 build 26200 (validated Windows floor is 10.0.17763; see Task 2 limits)

## Contract

- `VersionMetadata` is a string-preserving record over all six `version.json` fields
  (`code_name`, `code_name_CN`, `version`, `versionnm`, `build`, `future_codename`).
- `VersionMetadataReader.Load` returns `VersionMetadataLoadResult` (typed success/error);
  it never numeric-parses the version and never throws to callers.
- `VersionMetadata.WithNightlyVersion(DateOnly)` rewrites only `version`
  (`yyyy.MM.dd-nightly`, invariant culture) and preserves every other field.
- `ProductIdentity` pins `DisplayName=Luminalium`,
  `WindowsAppUserModelId=Kazuha.Luminalium`, `WindowsArtifactName=Luminalium-Windows.zip`.
- `Luminalium.App` bundles `version.json` (Content, PreserveNewest / publish Always)
  and displays `versionnm | build` in the shell, with a safe
  "Version metadata unavailable" fallback.

## Build / test

```
dotnet build .\Luminalium.sln -c Release   => 0 warnings, 0 errors
dotnet test  .\Luminalium.sln -c Release   => passed 9, failed 0, skipped 0
```

New tests (`VersionMetadataTests`):
- opaque `1.4.0.9-EMERGENCY` version / `00611.1409` build survive load byte-for-byte
- nightly mutation changes only `version`
- missing `version` => typed `MissingField` error with field + path
- malformed JSON => typed `MalformedJson` error with line info + path
- missing file => typed `FileNotFound` error with path

## Publish bundling

```
dotnet publish .\src\Luminalium.App\Luminalium.App.csproj -c Release -r win-x64 --self-contained true
=> version.json PRESENT in publish output
```

Byte-identity (source vs publish), SHA256:
`C189ED53CD9CAAEAA8F6EE0101EBE0B40C7303B6F88DE95D83E96F574A0B5AC4` (equal).
The CJK bytes render garbled only in the GBK console; the file is unchanged.

## Not verified here

- Icon / splash / tray visual identity (validated later by shell tasks).
- Live nightly workflow mutation (covered by Task 5 `Set-NightlyVersion.ps1`);
  this task verifies the in-process C# mutation contract only.
