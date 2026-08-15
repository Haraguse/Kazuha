<#
.SYNOPSIS
    Luminalium Task 23 release metadata gate over a published C# output.

.DESCRIPTION
    Machine-executable gate over a published C# output directory (PublishDir).
    Asserts the shipped version metadata is present and valid in the release
    payload, so a corrupted / missing / malformed version.json can never be
    published (the "corrupt version payload" scan of Task 23).

    Checks performed against <PublishDir>\version.json:

      1. The file must exist at the publish root (the App csproj copies
         version.json with CopyToPublishDirectory=Always).
      2. It must be a single valid JSON object.
      3. All required opaque string fields must be present and non-empty:
         code_name, code_name_CN, version, versionnm, build, future_codename.
      4. The version reader contract (Task 6) treats "version" and "build" as
         opaque strings; they are validated for presence/emptiness only, never
         parsed as [System.Version] or numeric values.

    Matching the rest of the tools in this repo, the script is deterministic,
    prints a one-line verdict, and exits 0 on pass.

    Exit codes: 0 = pass, 1 = violations found (every problem is printed),
    2 = setup error (PublishDir missing or not a directory).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-ReleaseVersion.ps1 -PublishDir artifacts/publish
#>

[CmdletBinding()]
param(
    [string]$PublishDir
)

$ErrorActionPreference = 'Stop'

if (-not $PublishDir) {
    Write-Error 'PublishDir is required: pass -PublishDir <path to published output>.' -ErrorAction Continue
    exit 2
}
if (-not (Test-Path -LiteralPath $PublishDir -PathType Container)) {
    Write-Error "PublishDir does not exist or is not a directory: $PublishDir" -ErrorAction Continue
    exit 2
}
$publishRoot = (Resolve-Path -LiteralPath $PublishDir).Path

$problems = [System.Collections.Generic.List[string]]::new()

$versionFile = Join-Path $publishRoot 'version.json'
if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
    Write-Host "RELEASE VERSION CHECK FAILED: version.json not found in publish root: $versionFile" -ForegroundColor Red
    exit 1
}

# Read with encoding detection so UTF-8 (BOM or not) and UTF-16 payloads are
# decoded correctly; PowerShell 5.1's Get-Content would otherwise fall back to
# the system ANSI codepage for BOM-less UTF-8 and garble CJK field values.
$bytes = [System.IO.File]::ReadAllBytes($versionFile)
$encoding = $null
if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
    $encoding = New-Object System.Text.UTF8Encoding($true)      # UTF-8 with BOM
}
elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
    $encoding = [System.Text.Encoding]::Unicode                 # UTF-16 LE
}
elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) {
    $encoding = [System.Text.Encoding]::BigEndianUnicode        # UTF-16 BE
}
else {
    $encoding = New-Object System.Text.UTF8Encoding($false)     # UTF-8 without BOM
}

$doc = $null
try {
    $doc = [System.IO.File]::ReadAllText($versionFile, $encoding) | ConvertFrom-Json
}
catch {
    $problems.Add("version.json is not valid JSON: $($_.Exception.Message)")
}

if ($null -ne $doc) {
    $required = @('code_name', 'code_name_CN', 'version', 'versionnm', 'build', 'future_codename')
    foreach ($field in $required) {
        $value = $doc.PSObject.Properties[$field]
        if ($null -eq $value -or -not ($value.Value -is [string]) -or [string]::IsNullOrWhiteSpace([string]$value.Value)) {
            $problems.Add("version.json field '$field' is missing, null or empty (must be a non-empty opaque string)")
        }
    }
}

if ($problems.Count -gt 0) {
    Write-Host "RELEASE VERSION CHECK FAILED ($($problems.Count) problem(s)):" -ForegroundColor Red
    foreach ($p in $problems) { Write-Host "  - $p" -ForegroundColor Red }
    exit 1
}

Write-Host "Release version check passed: $versionFile is valid JSON with all required opaque fields (code_name, code_name_CN, version, versionnm, build, future_codename)."
exit 0
