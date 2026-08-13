<#
.SYNOPSIS
    Luminalium C# migration - Task 5 nightly version bump for version.json.

.DESCRIPTION
    Machine-executable gate and mutation tool over the repository version.json
    (or an explicit -Path target). It validates the file, then replaces ONLY
    the "version" field with "yyyy.MM.dd-nightly"; every other field keeps its
    original opaque string value byte-for-byte (leading zeros such as
    "00611.1409", suffixes such as "-EMERGENCY", CJK text, formatting and line
    endings are untouched).

    Validation performed before any write:

      1. The target must exist and be valid JSON containing a single object.
      2. All required fields must be present and non-empty strings:
         code_name, code_name_CN, version, versionnm, build, future_codename.
      3. "version" and "build" are treated as opaque strings: they are never
         parsed as numeric values or [System.Version].

    The mutation is verified before writing: the result is re-parsed, the
    resulting "version" must equal the computed nightly value, and a masked
    diff proves no other byte of the file changed. On any failure nothing is
    written and the script exits non-zero.

    Exit codes: 0 = validated and (unless -DryRun) mutated, 1 = validation or
    write failure, 2 = setup error (target path missing), 3 = embedded
    -SelfTest failed.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File csharp/tools/Set-NightlyVersion.ps1

.EXAMPLE
    # Deterministic date input: same input, same output, every time.
    powershell -NoProfile -ExecutionPolicy Bypass -File csharp/tools/Set-NightlyVersion.ps1 -Date 2026-08-13

.EXAMPLE
    # Validate only; report what would change without touching the file.
    powershell -NoProfile -ExecutionPolicy Bypass -File csharp/tools/Set-NightlyVersion.ps1 -Path .\fixture.json -DryRun

.EXAMPLE
    # Self-contained fixture verification; temporary fixtures are created under
    # $env:TEMP and removed afterwards.
    powershell -NoProfile -ExecutionPolicy Bypass -File csharp/tools/Set-NightlyVersion.ps1 -SelfTest
#>

[CmdletBinding()]
param(
    [string]$Path,
    [DateTime]$Date = (Get-Date),
    [switch]$DryRun,
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

# --- Self-test: runs before anything else so -SelfTest never touches a real file.

function Invoke-NightlyVersionSelfTest {
    param([string]$ScriptPath)

    $pwsh = Join-Path $PSHOME 'powershell.exe'
    $fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("Set-NightlyVersion-SelfTest-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $fixtureRoot | Out-Null

    # Fixed date so every assertion is deterministic.
    $fixedDate = '2026-08-13'
    $fixedDateObj = [DateTime]::ParseExact($fixedDate, 'yyyy-MM-dd', [System.Globalization.CultureInfo]::InvariantCulture)
    $expectedNightly = $fixedDateObj.ToString('yyyy.MM.dd', [System.Globalization.CultureInfo]::InvariantCulture) + '-nightly'
    $cjk = '桃缶（河原木桃香）'

    $failures = [System.Collections.Generic.List[string]]::new()

    function Invoke-ChildPowerShell {
        # Runs a child powershell.exe. $ErrorActionPreference is forced to
        # 'Continue' around native calls: with 'Stop', any stderr output from
        # the child (e.g. a crash report) would become a terminating error and
        # kill this self-test with no diagnostics.
        param([string[]]$ChildArgs)
        $prevEAP = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $out = & $pwsh @ChildArgs 2>&1 | Out-String
            return @{ Exit = $LASTEXITCODE; Output = $out }
        }
        catch {
            return @{ Exit = -1; Output = "invocation error: $($_.Exception.Message)" }
        }
        finally {
            $ErrorActionPreference = $prevEAP
        }
    }

    function Invoke-Case {
        param(
            [string]$Name,
            [string]$FixturePath,
            [string[]]$Arguments,
            [int]$ExpectedExit,
            [scriptblock]$Assert
        )

        $args = @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $ScriptPath, '-Path', $FixturePath) + $Arguments

        # Some AV AMSI providers are broken and crash intermittently while
        # scanning larger scripts (exit -1073741819, AccessViolationException
        # in AmsiScanBuffer). The crash happens before any script code runs, so
        # a retry is safe; genuine failures reproduce on every attempt.
        $result = $null
        $crashDiag = ''
        for ($attempt = 1; $attempt -le 3; $attempt++) {
            $result = Invoke-ChildPowerShell -ChildArgs $args
            if ($result.Exit -ne -1073741819 -and $result.Output -notmatch 'AccessViolationException|AmsiScanBuffer') { break }
            $crashDiag += "attempt ${attempt}: AMSI crash (exit $($result.Exit))`n"
        }

        $exit = $result.Exit
        $output = $result.Output
        $text = if (Test-Path -LiteralPath $FixturePath) { [System.IO.File]::ReadAllText($FixturePath) } else { $null }

        if ($exit -ne $ExpectedExit) {
            $failures.Add("$Name : expected exit $ExpectedExit, got $exit`n$crashDiag$output")
            return
        }
        if ($Assert) {
            & $Assert -Exit $exit -Output $output -Text $text -Path $FixturePath
        }
    }

    try {
        # --- Case 1: valid, pretty, CRLF, no BOM, opaque suffixes, extra unknown field.
        $validPretty = @"
{
  "code_name": "Momokan",
  "code_name_CN": "$cjk",
  "version": "1.4.0.9-EMERGENCY",
  "versionnm": "1.4.0.9-EMERGENCY",
  "build": "00611.1409",
  "future_codename": "RyouYamada",
  "extra_opaque": "00777.9999-特製"
}
"@.Replace("`n", "`r`n")
        $p1 = Join-Path $fixtureRoot 'valid-pretty.json'
        [System.IO.File]::WriteAllText($p1, $validPretty, (New-Object System.Text.UTF8Encoding($false)))

        Invoke-Case -Name 'valid-pretty' -FixturePath $p1 -Arguments @('-Date', $fixedDate) -ExpectedExit 0 -Assert {
            param($Exit, $Output, $Text, $Path)
            $obj = [System.IO.File]::ReadAllText($Path) | ConvertFrom-Json
            if ($obj.version -ne $expectedNightly) { $failures.Add("valid-pretty : version '$($obj.version)' != '$expectedNightly'") }
            if ($obj.code_name -ne 'Momokan' -or $obj.code_name_CN -ne $cjk -or $obj.versionnm -ne '1.4.0.9-EMERGENCY' -or $obj.build -ne '00611.1409' -or $obj.future_codename -ne 'RyouYamada' -or $obj.extra_opaque -ne '00777.9999-特製') { $failures.Add('valid-pretty : non-version fields were altered') }
            if ([System.IO.File]::ReadAllText($Path) -notmatch "`r`n") { $failures.Add('valid-pretty : CRLF line endings not preserved') }
        }

        # --- Case 2: valid, minified, single line, no BOM.
        $p2 = Join-Path $fixtureRoot 'valid-min.json'
        [System.IO.File]::WriteAllText($p2, '{"code_name":"A","code_name_CN":"甲","version":"2.0","versionnm":"2.0","build":"001","future_codename":"B"}', (New-Object System.Text.UTF8Encoding($false)))

        Invoke-Case -Name 'valid-min' -FixturePath $p2 -Arguments @('-Date', $fixedDate) -ExpectedExit 0 -Assert {
            param($Exit, $Output, $Text, $Path)
            $bytes = [System.IO.File]::ReadAllBytes($Path)
            $obj = [System.IO.File]::ReadAllText($Path) | ConvertFrom-Json
            if ($obj.version -ne $expectedNightly) { $failures.Add("valid-min : version '$($obj.version)' != '$expectedNightly'") }
            if ($bytes[0] -ne 0x7B) { $failures.Add('valid-min : BOM was added to a BOM-less file') }
            if ($bytes.Length -ne [System.Text.Encoding]::UTF8.GetByteCount([System.IO.File]::ReadAllText($Path))) { $failures.Add('valid-min : unexpected byte length after mutation') }
        }

        # --- Case 3: valid, UTF-8 BOM preserved.
        $p3 = Join-Path $fixtureRoot 'valid-bom.json'
        [System.IO.File]::WriteAllText($p3, '{"code_name":"C","code_name_CN":"丙","version":"3.0-beta","versionnm":"3.0-beta","build":"00300","future_codename":"D"}', (New-Object System.Text.UTF8Encoding($true)))

        Invoke-Case -Name 'valid-bom' -FixturePath $p3 -Arguments @('-Date', $fixedDate) -ExpectedExit 0 -Assert {
            param($Exit, $Output, $Text, $Path)
            $bytes = [System.IO.File]::ReadAllBytes($Path)
            if (-not ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)) { $failures.Add('valid-bom : UTF-8 BOM not preserved') }
        }

        # --- Case 4: malformed JSON -> exit 1, file untouched.
        $p4 = Join-Path $fixtureRoot 'malformed.json'
        [System.IO.File]::WriteAllText($p4, '{ "code_name": "Momokan", "version": "1.0" ', (New-Object System.Text.UTF8Encoding($false)))

        Invoke-Case -Name 'malformed' -FixturePath $p4 -ExpectedExit 1 -Assert {
            param($Exit, $Output, $Text, $Path)
            if ($Text -ne '{ "code_name": "Momokan", "version": "1.0" ') { $failures.Add('malformed : file was modified despite invalid JSON') }
        }

        # --- Case 5: missing required "version" field -> exit 1, file untouched.
        $p5 = Join-Path $fixtureRoot 'missing-version.json'
        [System.IO.File]::WriteAllText($p5, '{"code_name":"E","code_name_CN":"戊","versionnm":"1.0","build":"001","future_codename":"F"}', (New-Object System.Text.UTF8Encoding($false)))

        Invoke-Case -Name 'missing-version' -FixturePath $p5 -ExpectedExit 1 -Assert {
            param($Exit, $Output, $Text, $Path)
            if ($Text -match '"version"') { $failures.Add('missing-version : file was modified despite missing version') }
        }

        # --- Case 6: missing required "build" field -> exit 1.
        $p6 = Join-Path $fixtureRoot 'missing-build.json'
        [System.IO.File]::WriteAllText($p6, '{"code_name":"G","code_name_CN":"庚","version":"1.0","versionnm":"1.0","future_codename":"H"}', (New-Object System.Text.UTF8Encoding($false)))

        Invoke-Case -Name 'missing-build' -FixturePath $p6 -ExpectedExit 1

        # --- Case 7: "version" present but not a string (numeric) -> exit 1.
        $p7 = Join-Path $fixtureRoot 'nonstring-version.json'
        [System.IO.File]::WriteAllText($p7, '{"code_name":"I","code_name_CN":"壬","version":42,"versionnm":"1.0","build":"001","future_codename":"J"}', (New-Object System.Text.UTF8Encoding($false)))

        Invoke-Case -Name 'nonstring-version' -FixturePath $p7 -ExpectedExit 1

        # --- Case 8: JSON root is an array, not an object -> exit 1.
        $p8 = Join-Path $fixtureRoot 'array-root.json'
        [System.IO.File]::WriteAllText($p8, '[1, 2, 3]', (New-Object System.Text.UTF8Encoding($false)))

        Invoke-Case -Name 'array-root' -FixturePath $p8 -ExpectedExit 1

        # --- Case 9: missing target path -> exit 2.
        Invoke-Case -Name 'missing-path' -FixturePath (Join-Path $fixtureRoot 'does-not-exist.json') -ExpectedExit 2

        # --- Case 10: -DryRun validates and reports, but writes nothing -> exit 0, bytes unchanged.
        $p10 = Join-Path $fixtureRoot 'dryrun.json'
        [System.IO.File]::WriteAllText($p10, '{"code_name":"K","code_name_CN":"癸","version":"1.0-old","versionnm":"1.0-old","build":"00123","future_codename":"L"}', (New-Object System.Text.UTF8Encoding($false)))
        $p10before = [System.IO.File]::ReadAllBytes($p10)

        Invoke-Case -Name 'dryrun' -FixturePath $p10 -Arguments @('-Date', $fixedDate, '-DryRun') -ExpectedExit 0 -Assert {
            param($Exit, $Output, $Text, $Path)
            $after = [System.IO.File]::ReadAllBytes($Path)
            if ($after.Length -ne $p10before.Length) { $failures.Add('dryrun : file was modified despite -DryRun'); return }
            for ($i = 0; $i -lt $after.Length; $i++) {
                if ($after[$i] -ne $p10before[$i]) { $failures.Add("dryrun : byte $i changed despite -DryRun"); break }
            }
        }
    }
    finally {
        # Temporary fixtures are removed afterwards (also on early failure).
        if (Test-Path -LiteralPath $fixtureRoot) {
            Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    if ($failures.Count -gt 0) {
        Write-Host "NIGHTLY VERSION SELF-TEST FAILED ($($failures.Count) failure(s)):" -ForegroundColor Red
        foreach ($f in $failures) { Write-Host "  - $f" -ForegroundColor Red }
        exit 3
    }
    Write-Host 'Nightly version self-test passed: valid/malformed/missing cases behaved as expected and all temporary fixtures were removed.'
    exit 0
}

if ($SelfTest) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if (-not $scriptPath) { Write-Host 'ERROR: -SelfTest cannot run when the script is dot-sourced.' -ForegroundColor Red; exit 2 }
    Invoke-NightlyVersionSelfTest -ScriptPath $scriptPath
    # Invoke-NightlyVersionSelfTest always exits; never reached.
}

# --- Resolve target path (default: repository-root version.json).

if (-not $Path) { $Path = Join-Path (Join-Path $PSScriptRoot '..\..') 'version.json' }

if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    Write-Host "ERROR: version.json not found: $Path" -ForegroundColor Red
    exit 2
}

$fullPath = (Resolve-Path -LiteralPath $Path).Path

$requiredFields = @('code_name', 'code_name_CN', 'version', 'versionnm', 'build', 'future_codename')

# --- Read with encoding detection so BOM/encoding is preserved on write.

$bytes = [System.IO.File]::ReadAllBytes($fullPath)
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

$content = [System.IO.File]::ReadAllText($fullPath, $encoding)

# --- Validate JSON and required fields.

$json = $null
try {
    $json = $content | ConvertFrom-Json
}
catch {
    Write-Host "ERROR: '$fullPath' is not valid JSON: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

if (-not ($json -is [System.Management.Automation.PSCustomObject])) {
    Write-Host "ERROR: '$fullPath' must contain a JSON object, not an array or scalar." -ForegroundColor Red
    exit 1
}

$missing = [System.Collections.Generic.List[string]]::new()
foreach ($field in $requiredFields) {
    $prop = $json.PSObject.Properties[$field]
    if ($null -eq $prop -or -not ($prop.Value -is [string]) -or [string]::IsNullOrEmpty($prop.Value)) {
        $missing.Add($field)
    }
}
if ($missing.Count -gt 0) {
    Write-Host "ERROR: '$fullPath' is missing required field(s): $($missing -join ', '). Required fields: $($requiredFields -join ', ')." -ForegroundColor Red
    exit 1
}

# --- Compute the nightly version. "version"/"build" are opaque strings: the
#     date is formatted, never parsed from the existing version.

$nightly = $Date.ToString('yyyy.MM.dd', [System.Globalization.CultureInfo]::InvariantCulture) + '-nightly'

# --- Replace ONLY the root-level "version" value. The ([{,]) anchor requires a
#     structural brace/comma immediately before the key, so an escaped
#     `"version":` appearing inside some other string value can never match.

$pattern = '([{,])(\s*"version"\s*:\s*)"(?:[^"\\]|\\.)*"'

if (-not $content -match $pattern) {
    Write-Host "ERROR: could not locate the ""version"" field in '$fullPath'." -ForegroundColor Red
    exit 1
}

$oldVersion = $json.version
$masked = '###LUMINALIUM_NIGHTLY_VERSION###'
$newContent = [regex]::Replace($content, $pattern, ('$1$2"' + $nightly + '"'), 1)

# --- Verify the mutation before writing: result must re-parse, the version
#     must match, and a masked diff must prove nothing else changed.

$newJson = $null
try {
    $newJson = $newContent | ConvertFrom-Json
}
catch {
    Write-Host "ERROR: mutation produced invalid JSON in '$fullPath'; nothing was written. $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

if ($newJson.version -ne $nightly) {
    Write-Host "ERROR: mutation verification failed in '$fullPath'; resulting version '$($newJson.version)' != '$nightly'; nothing was written." -ForegroundColor Red
    exit 1
}

foreach ($field in $requiredFields) {
    if ($field -eq 'version') { continue }
    if ($newJson.PSObject.Properties[$field].Value -ne $json.PSObject.Properties[$field].Value) {
        Write-Host "ERROR: mutation verification failed in '$fullPath'; field '$field' changed; nothing was written." -ForegroundColor Red
        exit 1
    }
}

if ([regex]::Replace($content, $pattern, $masked, 1) -ne [regex]::Replace($newContent, $pattern, $masked, 1)) {
    Write-Host "ERROR: mutation verification failed in '$fullPath'; masked diff shows changes beyond the version field; nothing was written." -ForegroundColor Red
    exit 1
}

# --- Write (unless -DryRun) and report.

if ($DryRun) {
    Write-Host "Dry run: '$fullPath' validated; would update version '$oldVersion' -> '$nightly'."
    exit 0
}

[System.IO.File]::WriteAllText($fullPath, $newContent, $encoding)

Write-Host "Nightly version updated: '$fullPath' version '$oldVersion' -> '$nightly'. All other fields preserved byte-for-byte."
exit 0
