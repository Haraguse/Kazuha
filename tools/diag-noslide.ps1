# Diagnostic: with the WPS bridge token injected, does the overlay window
# actually appear after clicking OpenOverlay? Dumps all top-level windows of
# the process over time.
[CmdletBinding()]
param(
    [string]$AppPath = ''
)

$ErrorActionPreference = 'Stop'
if (-not $AppPath) {
    $AppPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'src\Luminalium.App\bin\Release\net10.0\Luminalium.exe'
}
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = (Resolve-Path -LiteralPath $AppPath).Path
$psi.EnvironmentVariables['LUMINALIUM_WPS_BRIDGE_TOKEN'] = 'qa-harness-token'
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$proc = [System.Diagnostics.Process]::Start($psi)

try {
    $byProcess = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    $navCondition = New-Object System.Windows.Automation.OrCondition(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'LuminaliumFrame')),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, 'OpenOverlay')))

    $window = $null
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Date) -lt $deadline -and $null -eq $window) {
        if ($proc.HasExited) { Write-Host 'FAIL: process exited early'; break }
        try {
            $windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $byProcess)
            foreach ($w in $windows) {
                try {
                    if ($null -ne $w.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $navCondition)) {
                        $window = $w
                        break
                    }
                }
                catch { }
            }
        }
        catch { }
        if ($null -eq $window) { Start-Sleep -Milliseconds 250 }
    }
    if ($null -eq $window) { Write-Host 'FAIL: main window not found'; exit 1 }
    Write-Host "main window: '$($window.Current.Name)' handle=$($window.Current.NativeWindowHandle)"

    $openOverlay = $window.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, 'OpenOverlay')))
    if ($null -eq $openOverlay) { Write-Host 'FAIL: OpenOverlay not found'; exit 1 }

    try {
        $invoke = $openOverlay.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        Write-Host 'Invoke() called OK'
    }
    catch {
        Write-Host "Invoke threw: $($_.Exception.GetType().FullName): $($_.Exception.Message)"
    }

    # Poll for a second top-level window (the overlay).
    for ($i = 0; $i -lt 12; $i++) {
        Start-Sleep -Milliseconds 500
        try {
            $windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $byProcess)
            Write-Host ("--- poll {0}: {1} top-level windows ---" -f $i, $windows.Count)
            foreach ($w in $windows) {
                $ovName = $w.Current.Name
                $cls = $w.Current.ClassName
                $handle = $w.Current.NativeWindowHandle
                Write-Host "  window: name='$ovName' class='$cls' handle=$handle offscreen=$($w.Current.IsOffscreen)"
            }
            # Stop once we see a second window.
            if ($windows.Count -ge 2) { break }
        }
        catch {
            Write-Host "poll $i threw: $($_.Exception.Message)"
        }
    }

    if ($proc.HasExited) { Write-Host "process exited with code $($proc.ExitCode)" }
    else { Write-Host 'process still alive' }
}
finally {
    if ($null -ne $proc -and -not $proc.HasExited) {
        $closed = $proc.CloseMainWindow()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill(); $proc.WaitForExit() }
    }
}
