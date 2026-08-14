<#
.SYNOPSIS
    Luminalium C# migration - forbidden dependency, target-platform and
    source-boundary check (Task 3; Issue #96 cross-platform alignment;
    Task 9 SMTC in-process adapter boundary).

.DESCRIPTION
    Machine-executable gate over the new C# stack. Scans every project file
    under the repository root (plus Luminalium.sln) and exits
    non-zero when any of the following holds:

      1. A PackageReference/ProjectReference matches a forbidden dependency
         token: WebView, Python.Runtime, IronPython, PySide,
         Kazuha.PowerPointBridge, VSTO (case-insensitive, matches the
         Task 3 "Must NOT do" list).
      2. A project declares a Linux-only TargetFramework. Linux delivery is
         staged for a later issue; until then the new stack must not contain
         a Linux-only production project.
      3. A ProjectReference escapes the source boundary: it must resolve
         inside the repository root.
      4. Luminalium.sln references a project outside the repository root.
      5. A plain (non-Windows) TargetFramework project references a
         Windows-targeted project. Issue #96 keeps Core, Plugins and Updater
         on plain net10.0 while App, Platform.Windows and the test project
         stay net10.0-windows10.0.17763.0; the ProjectReference graph must
         keep Windows-targeted projects referencing plain ones, never the
         reverse.
      6. No project references Luminalium.Smtc.Windows directly. The SMTC
         adapter is solution-listed for build validation and loaded optionally
         by a later consumer task.

    Exit codes: 0 = pass, 1 = violations found (each printed once), 2 = setup
    error (solution missing).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
#>

[CmdletBinding()]
param(
    [string]$Root
)

$ErrorActionPreference = 'Stop'

# $PSScriptRoot is not available inside param() defaults, so resolve here.
if (-not $Root) { $Root = Join-Path $PSScriptRoot '..' }

$csharpRoot = (Resolve-Path $Root).Path
$solution   = Join-Path $csharpRoot 'Luminalium.sln'

# Task 3 "Must NOT do" forbidden dependency tokens.
$forbiddenTokens = @(
    'WebView',
    'Python.Runtime',
    'IronPython',
    'PySide',
    'Kazuha.PowerPointBridge',
    'VSTO'
)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$smtcWindowsProject = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'src\Luminalium.Smtc.Windows\Luminalium.Smtc.Windows.csproj'))

$violations = [System.Collections.Generic.List[string]]::new()

function Test-IsUnderPath {
    param([string]$Child, [string]$Parent)
    $childFull  = [System.IO.Path]::GetFullPath($Child)
    $parentFull = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    return $childFull.StartsWith($parentFull, [System.StringComparison]::OrdinalIgnoreCase)
}

# Rules 1-3, 5-6: scan every project file under the repository root, skipping generated trees.
Get-ChildItem -Path $csharpRoot -Recurse -Filter *.csproj -File | Where-Object {
    $_.FullName -notmatch '\\(bin|obj|publish|artifacts)\\[^\\]*$'
} | ForEach-Object {
    $project = $_
    $content = Get-Content -LiteralPath $project.FullName -Raw

    # Rule 2: Linux-only production TargetFramework.
    if ($content -match '<TargetFramework>(?<tfm>[^<]*linux[^<]*)</TargetFramework>') {
        $violations.Add("$($project.FullName): Linux-only TargetFramework '$($Matches['tfm'])'; Linux delivery is staged for a later issue and must not exist in the new stack yet")
    }

    # The project's own TargetFramework, used by Rule 5 to classify the
    # project as plain (cross-platform net10.0) or Windows-targeted.
    $ownTfm = ''
    if ($content -match '<TargetFramework>(?<tfm>[^<]+)</TargetFramework>') { $ownTfm = $Matches['tfm'] }
    $projectIsPlain = ($ownTfm -ne '') -and ($ownTfm -notmatch 'windows')

    foreach ($line in ($content -split "`r?`n")) {
        if ($line -match '<(?<type>PackageReference|ProjectReference)\s+Include="(?<ref>[^"]+)"') {
            $refType = $Matches['type']
            $ref     = $Matches['ref']

            # Rule 1: forbidden dependency tokens.
            foreach ($token in $forbiddenTokens) {
                if ($ref.IndexOf($token, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                    $violations.Add("$($project.FullName): forbidden $refType '$ref' matches token '$token'")
                }
            }

            # Rule 3: ProjectReference source boundary.
            if ($refType -eq 'ProjectReference') {
                $resolved = [System.IO.Path]::GetFullPath((Join-Path $project.DirectoryName $ref))
                $insideCsharp = Test-IsUnderPath -Child $resolved -Parent $csharpRoot
                if (-not $insideCsharp) {
                    $violations.Add("$($project.FullName): ProjectReference '$ref' escapes the repository-root boundary")
                }

                # Rule 6: Luminalium.Smtc.Windows is optional runtime surface and
                # must not be ProjectReferenced by Core/App/Platform.Windows/Tests.
                if ([System.IO.Path]::GetFullPath($resolved).Equals($smtcWindowsProject, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $violations.Add("$($project.FullName): must not ProjectReference Luminalium.Smtc.Windows; it is solution-listed only and loaded optionally by a later consumer task")
                }

                # Rule 5: plain (non-Windows) project must not reference a
                # Windows-targeted project. Windows-targeted projects may
                # reference plain projects, never the reverse.
                if ($projectIsPlain) {
                    $refTfm = ''
                    $refProjectPath = Join-Path $project.DirectoryName $ref
                    if (Test-Path -LiteralPath $refProjectPath) {
                        $refContent = Get-Content -LiteralPath $refProjectPath -Raw
                        if ($refContent -match '<TargetFramework>(?<tfm>[^<]+)</TargetFramework>') {
                            $refTfm = $Matches['tfm']
                        }
                    }
                    if ($refTfm -match 'windows') {
                        $violations.Add("$($project.FullName): plain TargetFramework '$ownTfm' must not ProjectReference Windows-targeted project '$ref' (TFM '$refTfm'); Windows-targeted projects may reference plain projects, never the reverse")
                    }
                }
            }
        }
    }
}

# Rule 4: solution-level source boundary.
if (Test-Path -LiteralPath $solution) {
    foreach ($line in (Get-Content -LiteralPath $solution)) {
        if ($line -match '^Project\("[^"]+"\)\s*=\s*"[^"]+"\s*,\s*"(?<path>[^"]+\.csproj)"') {
            $slnProj = $Matches['path']
            $resolved = [System.IO.Path]::GetFullPath((Join-Path (Split-Path $solution) $slnProj))
            $insideCsharp = Test-IsUnderPath -Child $resolved -Parent $csharpRoot
            if (-not $insideCsharp) {
                $violations.Add("${solution}: solution project '$slnProj' escapes the repository-root boundary")
            }
        }
    }
}
else {
    Write-Error "Solution not found: $solution"
    exit 2
}

if ($violations.Count -gt 0) {
    Write-Host "FORBIDDEN REFERENCE CHECK FAILED ($($violations.Count) violation(s)):" -ForegroundColor Red
    foreach ($v in $violations) { Write-Host "  - $v" -ForegroundColor Red }
    exit 1
}

Write-Host 'Forbidden reference check passed: no forbidden dependencies, no Linux-only project, no plain-to-Windows ProjectReference, no direct Luminalium.Smtc.Windows ProjectReference, and all references resolve inside the repository root.'
exit 0
