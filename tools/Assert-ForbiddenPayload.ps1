<#
.SYNOPSIS
    Luminalium C# migration - Task 5 forbidden publish-output payload check.

.DESCRIPTION
    Machine-executable gate over a published C# output directory (PublishDir).
    Recursively scans every file and directory below PublishDir and exits
    non-zero when any payload from the legacy Python/PyInstaller, PySide/PyQt,
    Qt/QML, WebView2, VSTO, Kazuha bridge or external-plugin stack is present:

      Python / PyInstaller: python*.dll, libpython*.dll, python*.exe,
          python*.zip, base_library.zip, *.pyc / *.py, _internal/
      PySide / PyQt:        PySide*/ and PyQt*/ trees, shiboken*.dll,
          *.dist-info/ package metadata
      Qt / QML:             Qt5*/Qt6* native DLLs, qwindows.dll, libEGL.dll,
          libGLESv2.dll, QtWebEngineProcess.exe, qtwebengine*.pak,
          icudtl.dat, qml/, qtwebengine/, plugins/
      WebView2:             WebView2Loader.dll, WebView2LoaderStatic.dll,
          Microsoft.Web.WebView2.{Core,WinForms,Wpf}.dll, msedgewebview2.exe,
          webview2/, EBWebView/
      VSTO / Kazuha bridge: *.vsto, Kazuha.PowerPointBridge.dll
      External plugins:     external/, plugins/

    Matching is deterministic and case-insensitive: whole leaf names and whole
    directory segments are matched against explicit tokens (never bare
    substrings), so ordinary Avalonia output - Avalonia*.dll, the app's own
    assemblies, *.runtimeconfig.json / *.deps.json, fonts, images and the
    VC++ runtime (vcruntime140.dll, msvcp140.dll, api-ms-win-*.dll) - is
    never flagged.

    Exit codes: 0 = pass, 1 = violations found (every forbidden path is
    printed), 2 = setup error (PublishDir missing or not a directory).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File csharp/tools/Assert-ForbiddenPayload.ps1 -PublishDir C:\artifacts\publish\win-x64
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

$violations   = [System.Collections.Generic.List[string]]::new()
$scannedFiles = 0
$scannedDirs  = 0

# Whole-file-name tokens, matched against the leaf name only (deterministic,
# case-insensitive, never a bare substring).
$filePatterns = @(
    @{ Pattern = '^python\d+\.dll$';             What = 'CPython import library (python3.dll, python312.dll, PyInstaller / embedded Python)' }
    @{ Pattern = '^libpython\d+\.dll$';          What = 'embedded CPython runtime DLL (libpython312.dll)' }
    @{ Pattern = '^python\d*\.exe$';             What = 'Python interpreter' }
    @{ Pattern = '^pythonw\d*\.exe$';            What = 'windowed Python interpreter' }
    @{ Pattern = '^python\d+\.zip$';             What = 'PyInstaller bundled Python standard library' }
    @{ Pattern = '^base_library\.zip$';          What = 'PyInstaller base library archive' }
    @{ Pattern = '^Python\.Runtime\.dll$';       What = 'pythonnet bridge (Python.Runtime)' }
    @{ Pattern = '^PySide\d*\.dll$';             What = 'PySide binding shim DLL' }
    @{ Pattern = '^shiboken\d*(\.abi3)?\.dll$';  What = 'PySide shiboken runtime DLL' }
    @{ Pattern = '^Qt\d+[^.]*\.dll$';            What = 'Qt native DLL (Qt5*/Qt6*, e.g. Qt6Core.dll)' }
    @{ Pattern = '^QtWebEngineProcess\.exe$';    What = 'Qt WebEngine helper process' }
    @{ Pattern = '^qwindows\.dll$';              What = 'Qt Windows platform plugin' }
    @{ Pattern = '^libEGL\.dll$';                What = 'Qt ANGLE (EGL) native library' }
    @{ Pattern = '^libGLESv2\.dll$';             What = 'Qt ANGLE (GLESv2) native library' }
    @{ Pattern = '^qtwebengine.*\.pak$';         What = 'Qt WebEngine resource pack' }
    @{ Pattern = '^icudtl\.dat$';                What = 'Chromium ICU data (WebView2 / QtWebEngine)' }
    @{ Pattern = '^WebView2Loader(Static)?\.dll$';         What = 'WebView2 loader DLL' }
    @{ Pattern = '^Microsoft\.Web\.WebView2\.(Core|WinForms|Wpf)\.dll$'; What = 'WebView2 .NET binding assembly' }
    @{ Pattern = '^msedgewebview2\.exe$';        What = 'WebView2 browser process' }
    @{ Pattern = '^Kazuha\.PowerPointBridge\.dll$'; What = 'Kazuha PowerPoint bridge (VSTO)' }
    @{ Pattern = '\.vsto$';                      What = 'VSTO deployment manifest' }
    @{ Pattern = '\.qml$';                       What = 'Qt QML document' }
    @{ Pattern = '^qmldir$';                     What = 'Qt QML module descriptor' }
    @{ Pattern = '\.pyc$';                       What = 'compiled Python bytecode' }
    @{ Pattern = '\.py$';                        What = 'Python source file' }
)

# Whole-directory-segment tokens, matched against every ancestor segment of a
# file and against every directory under PublishDir itself.
$dirPatterns = @(
    @{ Pattern = '^_internal$';     What = 'PyInstaller one-folder bundle directory' }
    @{ Pattern = '^PySide\d*$';     What = 'PySide binding package directory' }
    @{ Pattern = '^PyQt\d*$';       What = 'PyQt binding package directory' }
    @{ Pattern = '^shiboken\d*$';   What = 'PySide shiboken runtime directory' }
    @{ Pattern = '^qml$';           What = 'Qt QML module tree' }
    @{ Pattern = '^qtwebengine$';   What = 'Qt WebEngine resources directory' }
    @{ Pattern = '^plugins$';       What = 'Qt / external plugin payload directory' }
    @{ Pattern = '^external$';      What = 'bundled external payload directory' }
    @{ Pattern = '^webview2$';      What = 'WebView2 fixed-version runtime directory' }
    @{ Pattern = '^EBWebView$';     What = 'WebView2 browser-process data directory' }
    @{ Pattern = '\.dist-info$';    What = 'Python wheel metadata directory (e.g. PySide6-6.5.0.dist-info)' }
)

function Test-TokenMatch {
    param([string]$Name, [string]$Pattern)
    return [System.Text.RegularExpressions.Regex]::IsMatch($Name, $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

# Every forbidden directory itself (also catches empty payload directories).
Get-ChildItem -LiteralPath $publishRoot -Recurse -Directory -Force | ForEach-Object {
    $scannedDirs++
    foreach ($d in $dirPatterns) {
        if (Test-TokenMatch -Name $_.Name -Pattern $d.Pattern) {
            $violations.Add("$($_.FullName): forbidden payload directory matches '$($d.Pattern.Trim('^$'))' ($($d.What))")
            break
        }
    }
}

# Every forbidden file, by leaf name and by ancestor directory segment.
Get-ChildItem -LiteralPath $publishRoot -Recurse -File -Force | ForEach-Object {
    $scannedFiles++
    $rel   = $_.FullName.Substring($publishRoot.Length).TrimStart('\')
    $parts = $rel.Split('\')

    $dirSegments = @()
    if ($parts.Length -gt 1) { $dirSegments = $parts[0..($parts.Length - 2)] }

    foreach ($p in $filePatterns) {
        if (Test-TokenMatch -Name $_.Name -Pattern $p.Pattern) {
            $violations.Add("$($_.FullName): forbidden payload matches '$($p.Pattern)' ($($p.What))")
        }
    }
    foreach ($d in $dirPatterns) {
        foreach ($seg in $dirSegments) {
            if (Test-TokenMatch -Name $seg -Pattern $d.Pattern) {
                $violations.Add("$($_.FullName): inside forbidden payload directory '$seg' ($($d.What))")
                break
            }
        }
    }
}

# Deterministic output ordering regardless of filesystem enumeration order.
$violations.Sort()

if ($violations.Count -gt 0) {
    Write-Host "FORBIDDEN PAYLOAD CHECK FAILED ($($violations.Count) violation(s)):" -ForegroundColor Red
    foreach ($v in $violations) { Write-Host "  - $v" -ForegroundColor Red }
    exit 1
}

Write-Host "Forbidden payload check passed: $scannedFiles file(s) and $scannedDirs directory(ies) under $publishRoot contain no Python/PyInstaller, PySide/PyQt, Qt/QML, WebView2, VSTO, Kazuha bridge or external-plugin payloads."
exit 0
