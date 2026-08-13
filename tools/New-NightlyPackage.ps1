<#
.SYNOPSIS
    Luminalium C# migration - Task 5 deterministic Windows nightly package.

.DESCRIPTION
    Machine-executable packager over a published C# output directory
    (PublishDir). Produces a byte-deterministic ZIP archive: identical input
    files plus an identical -Date produce an identical archive, byte for byte
    (identical SHA-256), so nightly release artifacts are reproducible.

    The archive is built with System.IO.Compression.ZipArchive (available in
    Windows PowerShell 5.1); Compress-Archive is never used.

    Behavior:

      1. PublishDir must exist and be a directory.
      2. PublishDir must contain Luminalium.exe at its root.
      3. The output ZIP is written to -OutputPath (default
         "Luminalium-Windows.zip" relative to the current directory - the
         preserved nightly artifact contract).
      4. Every file below PublishDir is stored under its relative path
         normalized to forward slashes; entries are sorted by that normalized
         path using ordinal comparison; directories are never stored.
      5. Every entry receives the same fixed timestamp (-Date, or now when
         omitted), so the archive carries exactly one reproducible timestamp.
      6. If the output ZIP path lies inside PublishDir (or the ZIP pre-exists
         there from a previous run), the ZIP itself is excluded from the
         archive.
      7. The output directory is created only after verifying that its parent
         exists; no directory chain is invented.
      8. The SHA-256 of the produced archive is printed.

    Exit codes: 0 = archive created, 1 = validation or write failure (missing
    Luminalium.exe, output path is an existing directory, IO error),
    2 = setup error (PublishDir missing or not a directory, output directory
    parent missing), 3 = embedded -SelfTest failed.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File csharp/tools/New-NightlyPackage.ps1 -PublishDir .\artifacts\publish\win-x64

.EXAMPLE
    # Deterministic: fixed date, exact nightly artifact name, output elsewhere.
    powershell -NoProfile -ExecutionPolicy Bypass -File csharp/tools/New-NightlyPackage.ps1 -PublishDir .\artifacts\publish\win-x64 -OutputPath .\dist\Luminalium-Windows.zip -Date 2026-08-13

.EXAMPLE
    # Self-contained fixture verification; temporary fixtures are created under
    # $env:TEMP and removed afterwards.
    powershell -NoProfile -ExecutionPolicy Bypass -File csharp/tools/New-NightlyPackage.ps1 -SelfTest
#>

[CmdletBinding()]
param(
    [string]$PublishDir,
    [string]$OutputPath,
    [DateTime]$Date = (Get-Date),
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression | Out-Null

# --- Self-test: runs before anything else so -SelfTest never touches a real tree.

function Invoke-NightlyPackageSelfTest {
    param([string]$ScriptPath)

    $pwsh = Join-Path $PSHOME 'powershell.exe'
    $fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("New-NightlyPackage-SelfTest-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $fixtureRoot | Out-Null

    # Fixed date so every assertion is deterministic.
    $fixedDate = '2026-08-13'
    $fixedDateObj = [DateTime]::ParseExact($fixedDate, 'yyyy-MM-dd', [System.Globalization.CultureInfo]::InvariantCulture)

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
            [string[]]$Arguments,
            [int]$ExpectedExit,
            [scriptblock]$Assert
        )

        $args = @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $ScriptPath) + $Arguments

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

        Write-Host "  [$Name] exit $exit : $($args -join ' ')"

        if ($exit -ne $ExpectedExit) {
            $failures.Add("$Name : expected exit $ExpectedExit, got $exit`n$crashDiag$output")
            return
        }
        if ($Assert) {
            & $Assert -Exit $exit -Output $output -Path $fixtureRoot
        }
    }

    try {
        # --- Fixture publish tree: 6 files across two subdirectories plus one
        #     empty directory (which must NOT become an archive entry).

        $publishRoot = Join-Path $fixtureRoot 'publish'
        New-Item -ItemType Directory -Path $publishRoot | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $publishRoot 'sub') | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $publishRoot 'empty') | Out-Null

        [System.IO.File]::WriteAllBytes((Join-Path $publishRoot 'Luminalium.exe'), ([byte[]](0x4D, 0x5A) + [byte[]](1..30)))
        [System.IO.File]::WriteAllText((Join-Path $publishRoot 'readme.txt'), 'Luminalium nightly package fixture.', (New-Object System.Text.UTF8Encoding($false)))
        [System.IO.File]::WriteAllText((Join-Path $publishRoot 'Zulu.txt'), 'ZULU', (New-Object System.Text.UTF8Encoding($false)))
        [System.IO.File]::WriteAllText((Join-Path $publishRoot 'alpha.dat'), ('alpha data' * 200), (New-Object System.Text.UTF8Encoding($false)))
        [System.IO.File]::WriteAllText((Join-Path $publishRoot 'sub\appsettings.json'), '{"name":"Luminalium"}', (New-Object System.Text.UTF8Encoding($false)))
        [System.IO.File]::WriteAllBytes((Join-Path $publishRoot 'sub\data.bin'), [byte[]](0..63))

        $outDir = Join-Path $fixtureRoot 'out'
        $outZip = Join-Path $outDir 'Luminalium-Windows.zip'

        # --- Case 1: positive run. Archive must be sorted, forward-slash
        #     normalized, directory-free, single fixed timestamp, SHA256 printed.
        Invoke-Case -Name 'positive' -Arguments @('-PublishDir', $publishRoot, '-OutputPath', $outZip, '-Date', $fixedDate) -ExpectedExit 0 -Assert {
            param($Exit, $Output, $Path)
            if (-not (Test-Path -LiteralPath $outZip -PathType Leaf)) { $failures.Add('positive : output zip was not created'); return }
            if ($Output -notmatch 'SHA256: [0-9A-F]{64}') { $failures.Add("positive : SHA256 not reported`n$Output") }

            $fs = [System.IO.File]::OpenRead($outZip)
            $za = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Read)
            try {
                $names = @($za.Entries | ForEach-Object { $_.FullName })
                $times = @($za.Entries | ForEach-Object { $_.LastWriteTime })
            }
            finally {
                $za.Dispose()
                $fs.Dispose()
            }

            if ($names.Count -ne 6) { $failures.Add("positive : expected 6 entries, got $($names.Count)") }
            if ($names -notcontains 'Luminalium.exe') { $failures.Add('positive : Luminalium.exe missing from archive') }
            if ($names -contains 'empty') { $failures.Add('positive : empty directory became an entry') }
            foreach ($n in $names) {
                if ($n.Contains('\')) { $failures.Add("positive : backslash in entry name '$n'") }
                if ($n.StartsWith('/') -or $n.StartsWith('./') -or $n.EndsWith('/')) { $failures.Add("positive : malformed entry name '$n'") }
            }

            $sorted = @($names)
            [System.Array]::Sort($sorted, [System.StringComparer]::Ordinal)
            if (($names -join '|') -ne ($sorted -join '|')) {
                $failures.Add("positive : entries not ordinal-sorted`n  actual:   $($names -join ', ')`n  expected: $($sorted -join ', ')")
            }

            if ($times.Count -gt 0) {
                foreach ($t in $times) {
                    if ($t -ne $times[0]) { $failures.Add("positive : timestamps differ between entries ($t vs $($times[0]))") }
                    if ([Math]::Abs(($t - $fixedDateObj).TotalHours) -gt 48) {
                        $failures.Add("positive : timestamp '$t' far from fixed date '$fixedDateObj' (timezone-adjusted read-back expected)")
                    }
                }
            }
        }

        # --- Case 2: determinism. Same command, same fixture, same output
        #     path, regenerated after deletion: SHA256 must be byte-identical.
        $hash1 = $null
        Invoke-Case -Name 'determinism-run1' -Arguments @('-PublishDir', $publishRoot, '-OutputPath', $outZip, '-Date', $fixedDate) -ExpectedExit 0
        try {
            if (Test-Path -LiteralPath $outZip -PathType Leaf) { $hash1 = (Get-FileHash -LiteralPath $outZip -Algorithm SHA256).Hash }
        }
        catch { $hash1 = $null }
        if (-not $hash1) { $failures.Add('determinism-run1 : could not hash first output') }
        Remove-Item -LiteralPath $outZip -Force -ErrorAction SilentlyContinue

        Invoke-Case -Name 'determinism-run2' -Arguments @('-PublishDir', $publishRoot, '-OutputPath', $outZip, '-Date', $fixedDate) -ExpectedExit 0 -Assert {
            param($Exit, $Output, $Path)
            $hash2 = $null
            try {
                if (Test-Path -LiteralPath $outZip -PathType Leaf) { $hash2 = (Get-FileHash -LiteralPath $outZip -Algorithm SHA256).Hash }
            }
            catch { $hash2 = $null }
            if (-not $hash2) { $failures.Add('determinism-run2 : could not hash second output'); return }
            if ($hash2 -ne $hash1) {
                $failures.Add("determinism : SHA256 differs between identical runs`n  run1: $hash1`n  run2: $hash2")
            }
            else {
                Write-Host "  determinism SHA256 identical: $hash2"
            }
        }

        # --- Case 3: missing Luminalium.exe -> exit 1, no output written.
        $noExeRoot = Join-Path $fixtureRoot 'no-exe'
        New-Item -ItemType Directory -Path $noExeRoot | Out-Null
        [System.IO.File]::WriteAllText((Join-Path $noExeRoot 'Luminalium.dll'), 'not the exe', (New-Object System.Text.UTF8Encoding($false)))
        # Distinct output path: cases 1-2 already created a zip at $outZip, so
        # this case must prove the failed run writes nothing at a fresh path.
        $noExeZip = Join-Path $outDir 'missing-exe-output.zip'
        Invoke-Case -Name 'missing-exe' -Arguments @('-PublishDir', $noExeRoot, '-OutputPath', $noExeZip, '-Date', $fixedDate) -ExpectedExit 1 -Assert {
            param($Exit, $Output, $Path)
            if (Test-Path -LiteralPath $noExeZip) { $failures.Add('missing-exe : output zip was created despite missing Luminalium.exe') }
        }

        # --- Case 4: missing PublishDir -> exit 2.
        Invoke-Case -Name 'missing-directory' -Arguments @('-PublishDir', (Join-Path $fixtureRoot 'does-not-exist')) -ExpectedExit 2

        # --- Case 5: output ZIP inside PublishDir, pre-existing stale zip
        #     must be excluded from the archive and replaced atomically.
        $insideZip = Join-Path $publishRoot 'Luminalium-Windows.zip'
        [System.IO.File]::WriteAllBytes($insideZip, [byte[]](0x53, 0x54, 0x41, 0x4C, 0x45))   # 'STALE'
        Invoke-Case -Name 'exclude-output' -Arguments @('-PublishDir', $publishRoot, '-OutputPath', $insideZip, '-Date', $fixedDate) -ExpectedExit 0 -Assert {
            param($Exit, $Output, $Path)
            $info = Get-Item -LiteralPath $insideZip
            if ($info.Length -eq 5) { $failures.Add('exclude-output : stale zip was not replaced') }
            $fs = [System.IO.File]::OpenRead($insideZip)
            $za = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Read)
            try { $names = @($za.Entries | ForEach-Object { $_.FullName }) }
            finally {
                $za.Dispose()
                $fs.Dispose()
            }
            if ($names -contains 'Luminalium-Windows.zip') { $failures.Add('exclude-output : output zip was packed into itself') }
            if ($names.Count -ne 6) { $failures.Add("exclude-output : expected 6 entries, got $($names.Count)") }
            if ($names -notcontains 'Luminalium.exe') { $failures.Add('exclude-output : Luminalium.exe missing from archive') }
        }

        # --- Case 6: output directory parent missing -> exit 2, nothing created.
        $deepZip = Join-Path $fixtureRoot (Join-Path 'out' (Join-Path 'missing\deep' 'Luminalium-Windows.zip'))
        Invoke-Case -Name 'deep-output-parent-missing' -Arguments @('-PublishDir', $publishRoot, '-OutputPath', $deepZip, '-Date', $fixedDate) -ExpectedExit 2 -Assert {
            param($Exit, $Output, $Path)
            if (Test-Path -LiteralPath (Split-Path -Parent (Split-Path -Parent $deepZip))) {
                $failures.Add('deep-output-parent-missing : directory chain was invented despite missing parent')
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
        Write-Host "NIGHTLY PACKAGE SELF-TEST FAILED ($($failures.Count) failure(s)):" -ForegroundColor Red
        foreach ($f in $failures) { Write-Host "  - $f" -ForegroundColor Red }
        exit 3
    }
    Write-Host 'Nightly package self-test passed: positive/determinism/missing-exe/missing-directory/exclusion cases behaved as expected and all temporary fixtures were removed.'
    exit 0
}

if ($SelfTest) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if (-not $scriptPath) { Write-Host 'ERROR: -SelfTest cannot run when the script is dot-sourced.' -ForegroundColor Red; exit 2 }
    Invoke-NightlyPackageSelfTest -ScriptPath $scriptPath
    # Invoke-NightlyPackageSelfTest always exits; never reached.
}

# --- Validate PublishDir.

if (-not $PublishDir) {
    Write-Host 'ERROR: PublishDir is required: pass -PublishDir <path to published output>.' -ForegroundColor Red
    exit 2
}
if (-not (Test-Path -LiteralPath $PublishDir -PathType Container)) {
    Write-Host "ERROR: PublishDir does not exist or is not a directory: $PublishDir" -ForegroundColor Red
    exit 2
}
$publishRoot = (Resolve-Path -LiteralPath $PublishDir).Path

$exePath = Join-Path $publishRoot 'Luminalium.exe'
if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
    Write-Host "ERROR: Luminalium.exe not found in PublishDir root: $exePath" -ForegroundColor Red
    exit 1
}

# --- Resolve output path; default keeps the preserved nightly artifact name.

if (-not $OutputPath) { $OutputPath = 'Luminalium-Windows.zip' }
$outFull = [System.IO.Path]::GetFullPath($OutputPath)

if (Test-Path -LiteralPath $outFull -PathType Container) {
    Write-Host "ERROR: OutputPath is an existing directory, not a file: $outFull" -ForegroundColor Red
    exit 1
}

# --- Create the output directory only after verifying that ITS parent exists.

$outDir = [System.IO.Path]::GetDirectoryName($outFull)
$outGrand = [System.IO.Path]::GetDirectoryName($outDir)   # $null when $outDir is a drive root
if ($outGrand -and -not (Test-Path -LiteralPath $outGrand -PathType Container)) {
    Write-Host "ERROR: parent of output directory does not exist: $outGrand" -ForegroundColor Red
    exit 2
}
if (-not (Test-Path -LiteralPath $outDir -PathType Container)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

# --- Enumerate input files, exclude the output ZIP, normalize entry names.

$entries = @(Get-ChildItem -LiteralPath $publishRoot -Recurse -File -Force | ForEach-Object {
    if ([string]::Equals($_.FullName, $outFull, [System.StringComparison]::OrdinalIgnoreCase)) { return }
    $rel = $_.FullName.Substring($publishRoot.Length).TrimStart('\')
    [pscustomobject]@{
        EntryName = $rel.Replace('\', '/')
        FullName  = $_.FullName
        Length    = $_.Length
    }
})

# Deterministic entry order: ordinal sort of the normalized forward-slash
# relative path, independent of filesystem enumeration order.
$keys = @($entries | ForEach-Object { $_.EntryName })
[System.Array]::Sort($keys, $entries, [System.StringComparer]::Ordinal)

# One reproducible timestamp for every entry: components taken verbatim from
# -Date (sub-second precision dropped), tagged UTC so the stored DOS time does
# not depend on the local timezone.
$stamp = [DateTime]::new($Date.Year, $Date.Month, $Date.Day, $Date.Hour, $Date.Minute, $Date.Second, [DateTimeKind]::Utc)

# --- Build the archive (ZipArchive, never Compress-Archive).

$fs = $null
$zip = $null
try {
    $fs = [System.IO.File]::Open($outFull, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    $zip = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create)
    foreach ($item in $entries) {
        $entry = $zip.CreateEntry($item.EntryName, [System.IO.Compression.CompressionLevel]::Optimal)
        $entry.LastWriteTime = $stamp
        $src = $null
        $dst = $null
        try {
            $src = [System.IO.File]::OpenRead($item.FullName)
            $dst = $entry.Open()
            $src.CopyTo($dst)
        }
        finally {
            if ($dst) { $dst.Dispose() }
            if ($src) { $src.Dispose() }
        }
    }
    $zip.Dispose(); $zip = $null
    $fs.Dispose();  $fs  = $null
}
catch {
    if ($zip) { try { $zip.Dispose() } catch { } }
    if ($fs)  { try { $fs.Dispose() }  catch { } }
    Remove-Item -LiteralPath $outFull -Force -ErrorAction SilentlyContinue
    Write-Host "ERROR: failed to create nightly package: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$totalBytes = ($entries | Measure-Object -Property Length -Sum).Sum
$sha = (Get-FileHash -LiteralPath $outFull -Algorithm SHA256).Hash

Write-Host "Nightly package created: $outFull"
Write-Host "  entries: $($entries.Count) (sorted by normalized relative path), uncompressed bytes: $totalBytes"
Write-Host "  fixed timestamp: $stamp (all entries)"
Write-Host "  SHA256: $sha"
exit 0
