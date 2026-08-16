# Diagnostic: understand why clicking the Settings navigation item does not
# navigate the shell to the Settings page. Dumps the matched element's control
# type, supported patterns, then attempts Select() and dumps the page state.
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

Add-Type -Namespace LuminaliumDiag -Name Native -MemberDefinition @'
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, System.UIntPtr dwExtraInfo);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(System.IntPtr hWnd);
'@

$script:MouseLeftDown = 0x0002
$script:MouseLeftUp = 0x0004

function Click-Screen {
    param([double]$X, [double]$Y)
    [void][LuminaliumDiag.Native]::SetCursorPos([int][math]::Round($X), [int][math]::Round($Y))
    Start-Sleep -Milliseconds 60
    [LuminaliumDiag.Native]::mouse_event($script:MouseLeftDown, 0, 0, 0, [System.UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [LuminaliumDiag.Native]::mouse_event($script:MouseLeftUp, 0, 0, 0, [System.UIntPtr]::Zero)
    Start-Sleep -Milliseconds 120
}
$root = [System.Windows.Automation.AutomationElement]::RootElement

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = (Resolve-Path -LiteralPath $AppPath).Path
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
    Write-Host "main window: $($window.Current.Name)"

    # Find every element named 设置 (both nav item and any text).
    $nameCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, '设置')
    $all = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $nameCondition)
    Write-Host "elements named '设置': $($all.Count)"
    foreach ($el in $all) {
        $ct = $el.Current.ControlType.ProgrammaticName
        Write-Host "  - ControlType=$ct AutomationId='$($el.Current.AutomationId)'"
        $supports = @()
        foreach ($p in @([System.Windows.Automation.SelectionItemPattern]::Pattern,
                         [System.Windows.Automation.InvokePattern]::Pattern,
                         [System.Windows.Automation.TogglePattern]::Pattern)) {
            $peer = $null
            try { $peer = $el.GetCurrentPattern($p) } catch { }
            if ($null -ne $peer) { $supports += $p.ProgrammaticName }
        }
        Write-Host "    supports: $($supports -join ', ')"
    }

    $targets = @($all | Where-Object {
        $_.Current.ControlType.ProgrammaticName -eq 'ControlType.ListItem'
    })
    Write-Host "ListItem targets: $($targets.Count)"

    # Bring the window to the foreground so mouse clicks land on it.
    try { [void][LuminaliumDiag.Native]::SetForegroundWindow($window.Current.NativeWindowHandle) } catch { }
    Start-Sleep -Milliseconds 300

    foreach ($i in 0..($targets.Count - 1)) {
        $target = $targets[$i]
        $bounds = $target.Current.BoundingRectangle
        $visible = $bounds.Width -gt 0 -and $bounds.Height -gt 0 -and -not $target.Current.IsOffscreen
        Write-Host "=== mouse-click try #$i (visible=$visible, bounds=$($bounds.X),$($bounds.Y) $($bounds.Width)x$($bounds.Height)) ==="
        if (-not $visible) { continue }
        $cx = $bounds.X + $bounds.Width / 2
        $cy = $bounds.Y + $bounds.Height / 2
        Click-Screen $cx $cy

        Start-Sleep -Milliseconds 1200
        $desc = $null
        try { $desc = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition) } catch { }
        $names = @($desc | ForEach-Object { $_.Current.Name } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
        Write-Host "  settings marker ('主题模式'): $($names -contains '主题模式') | onboarding marker: $($names -contains '标记为已完成')"

        # If we navigated to settings, stop.
        if ($names -contains '主题模式') { break }
    }

    # Dump page state: look for onboarding vs settings content markers.
    $desc = $null
    try { $desc = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition) } catch { }
    $names = @($desc | ForEach-Object { $_.Current.Name } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    Write-Host '--- post-navigation names ---'
    $names -join ' | '
    Write-Host '--- markers ---'
    Write-Host "onboarding marker ('标记为已完成'): $($names -contains '标记为已完成')"
    Write-Host "settings page marker ('主题模式'): $($names -contains '主题模式')"
    Write-Host "settings page marker ('About'/关于): $($names -contains '关于')"
}
finally {
    if ($null -ne $proc -and -not $proc.HasExited) {
        $closed = $proc.CloseMainWindow()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill(); $proc.WaitForExit() }
    }
}
