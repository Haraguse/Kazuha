<#
.SYNOPSIS
    Luminalium C# migration - native feature migration usage baseline (Task 4).

.DESCRIPTION
    Scans the production source tree (src/) for each legacy built-in feature
    usage category that BuiltInFeatureMigrationDiagnostics tracks and records a
    deterministic, normalized report under artifacts/feature-migration/baseline/.

    Categories mirror BuiltInFeatureDiagnosticKind:
      LegacyAliasUsed          - `plugin:` route aliases / legacy navigation tags.
      LegacyProjectionUsed     - BuiltInPluginCatalog.CreateDefaultRegistry()
                                 and BuiltInFeatureLegacyProjection consumption.
      PlaceholderFactoryUsed   - PlaceholderPluginCommand / PlaceholderPluginViewFactory.
      DirectNativeConstruction - direct `new BoardWindow/TimerWindow/...` outside
                                 the native host factory.
      DuplicateRegistration    - BuiltInPluginRegistry.Register calls / registry use.
      ActivationFailed         - explicit failure-returning activation paths.

    The report is normalized (sorted, no timestamps) so re-running on unchanged
    source produces an identical report. This is a baseline snapshot only; it
    must not be used by itself to claim zero usage (see Task 13 gate).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/Capture-MigrationBaseline.ps1
#>

[CmdletBinding()]
param(
    [string]$Root
)

$ErrorActionPreference = 'Stop'

if (-not $Root) { $Root = Join-Path $PSScriptRoot '..' }
$repoRoot = (Resolve-Path $Root).Path

$srcRoot  = Join-Path $repoRoot 'src'
$outDir   = Join-Path $repoRoot 'artifacts\feature-migration\baseline'
$outFile  = Join-Path $outDir 'usage-baseline.txt'

if (-not (Test-Path -LiteralPath $srcRoot)) {
    Write-Error "Source tree not found: $srcRoot"
    exit 2
}

# Category -> regex pattern (production source only; bin/obj excluded).
$categories = [ordered]@{
    'LegacyAliasUsed'          = 'plugin:'
    'LegacyProjectionUsed'     = 'CreateDefaultRegistry|BuiltInFeatureLegacyProjection'
    'PlaceholderFactoryUsed'   = 'PlaceholderPluginCommand|PlaceholderPluginViewFactory'
    'DirectNativeConstruction' = 'new\s+(BoardWindow|TimerWindow|SpotlightWindow|AppLauncherWindow|StatusBarWindow)\s*\('
    'DuplicateRegistration'    = '\.Register\(|BuiltInPluginRegistry'
    'ActivationFailed'         = 'ActivationFailed|BuiltInFeatureActivationErrorCode'
}

$files = Get-ChildItem -Path $srcRoot -Recurse -Filter *.cs -File | Where-Object {
    $_.FullName -notmatch '\\(bin|obj)\\[^\\]*$'
} | Sort-Object -Property FullName

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('Luminalium native feature migration usage baseline (Task 4)')
$lines.Add('Scan root: ' + $srcRoot)
$lines.Add('Deterministic normalized report - re-running on unchanged source yields identical output.')
$lines.Add('')

$grandTotal = 0
foreach ($entry in $categories.GetEnumerator()) {
    $category = $entry.Key
    $pattern   = $entry.Value
    $lines.Add("## $category")

    $categoryTotal = 0
    $fileHits = [System.Collections.Generic.List[object]]::new()
    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $matches = [regex]::Matches($content, $pattern)
        if ($matches.Count -gt 0) {
            $full = [System.IO.Path]::GetFullPath($file.FullName)
            $root = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
            $rel = $full.Substring($root.Length).Replace('\', '/')
            $fileHits.Add([pscustomobject]@{ File = $rel; Count = $matches.Count })
            $categoryTotal += $matches.Count
        }
    }

    if ($fileHits.Count -eq 0) {
        $lines.Add('  (none)')
    }
    else {
        foreach ($hit in ($fileHits | Sort-Object -Property File)) {
            $lines.Add("  $($hit.File): $($hit.Count)")
        }
    }
    $lines.Add("  TOTAL: $categoryTotal")
    $lines.Add('')
    $grandTotal += $categoryTotal
}

$lines.Add("GRAND TOTAL (production source): $grandTotal")

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$lines | Set-Content -LiteralPath $outFile -Encoding UTF8

Write-Host "Migration usage baseline written to $outFile (grand total $grandTotal)."
exit 0
