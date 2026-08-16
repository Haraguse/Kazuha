<#
    One-off diagnostic for the T22 noslide scenario: capture the exact
    exception (type + inner chain + full stack) thrown when the OpenOverlay
    button is invoked through UIA, and report whether the app process is still
    alive and whether an overlay window appeared.
#>
[CmdletBinding()]
param(
    [string]$AppPath = (Join-Path $PSScriptRoot '..\src\Luminalium.App\bin\Release\net10.0\Luminalium.exe')
)

$ErrorActionPreference = 'Stop'
$AppPath = (Resolve-Path -LiteralPath $AppPath).Path

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $AppPath
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$proc = [System.Diagnostics.Process]::Start($psi)

function Get-MainWindow {
    param([System.Diagnostics.Process]$Process)
    $byProcess = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $Process.Id)
    $navCondition = New-Object System.Windows.Automation.OrCondition(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'LuminaliumFrame')),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, 'OpenOverlay')))
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Date) -lt $deadline) {
        if ($Process.HasExited) { return $null }
        try {
            $windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $byProcess)
            foreach ($w in $windows) {
                try {
                    if ($null -ne $w.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $navCondition)) {
                        return $w
                    }
                }
                catch { }
            }
        }
        catch { }
        Start-Sleep -Milliseconds 250
    }
    return $null
}

try {
    $window = Get-MainWindow -Process $proc
    if ($null -eq $window) { Write-Host 'FAIL: main window not found'; exit 1 }
    Write-Host "main window: $($window.Current.Name)"

    $openOverlay = $null
    try {
        $openOverlay = $window.FindFirst(
            [System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::NameProperty, 'OpenOverlay')))
    }
    catch { Write-Host "FAIL: find openoverlay threw: $($_.Exception.Message)" }

    if ($null -eq $openOverlay) { Write-Host 'FAIL: OpenOverlay button not found'; exit 1 }
    Write-Host "openoverlay: $($openOverlay.Current.Name), enabled=$($openOverlay.Current.IsEnabled)"

    try {
        $invoke = $openOverlay.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        Write-Host "invoke pattern: $(if ($null -eq $invoke) { 'NULL' } else { 'OK' })"
        if ($null -ne $invoke) {
            $invoke.Invoke()
            Write-Host 'invoke returned without throwing'
        }
    }
    catch {
        Write-Host "EXCEPTION type: $($_.Exception.GetType().FullName)"
        Write-Host "EXCEPTION msg: $($_.Exception.Message)"
        Write-Host "STACK: $($_.ScriptStackTrace)"
        $inner = $_.Exception.InnerException
        while ($null -ne $inner) {
            Write-Host "INNER: $($inner.GetType().FullName): $($inner.Message)"
            $inner = $inner.InnerException
        }
    }

    Start-Sleep -Milliseconds 1200
    Write-Host "process alive: $(-not $proc.HasExited)"
    if (-not $proc.HasExited) {
        $byProcess = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
        try {
            $windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $byProcess)
            foreach ($w in $windows) {
                Write-Host "window: '$($w.Current.Name)' handle=$($w.Current.NativeWindowHandle)"
            }
        }
        catch { Write-Host "enumerate windows threw: $($_.Exception.Message)" }
    }
}
finally {
    if ($null -ne $proc -and -not $proc.HasExited) {
        $closed = $proc.CloseMainWindow()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill(); $proc.WaitForExit() }
    }
}
