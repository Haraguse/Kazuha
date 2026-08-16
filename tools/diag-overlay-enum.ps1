# Diagnostic: enumerate ALL top-level windows (Win32) for the Luminalium
# process after clicking OpenOverlay, including hidden/layered/owned windows
# that UIA may not expose.
[CmdletBinding()]
param(
    [string]$AppPath = ''
)

$ErrorActionPreference = 'Stop'
if (-not $AppPath) {
    $AppPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'src\Luminalium.App\bin\Release\net10.0\Luminalium.exe'
}

Add-Type -Namespace LuminaliumDiag2 -Name Native -MemberDefinition @'
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, System.IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    public static extern int GetWindowText(System.IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    public static extern int GetClassName(System.IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool IsWindowVisible(System.IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, out uint lpdwProcessId);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool GetWindowRect(System.IntPtr hWnd, out RECT lpRect);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    public delegate bool EnumWindowsProc(System.IntPtr hWnd, System.IntPtr lParam);
'@

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = (Resolve-Path -LiteralPath $AppPath).Path
$psi.EnvironmentVariables['LUMINALIUM_WPS_BRIDGE_TOKEN'] = 'qa-harness-token'
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$proc = [System.Diagnostics.Process]::Start($psi)

function Get-WindowsForProcess {
    param([int]$ProcessId)
    $result = [System.Collections.Generic.List[object]]::new()
    $callback = [LuminaliumDiag2.Native+EnumWindowsProc]{
        param($hWnd, $lParam)
        $wid = 0
        [void][LuminaliumDiag2.Native]::GetWindowThreadProcessId($hWnd, [ref]$wid)
        if ($wid -eq $ProcessId) {
            $title = New-Object System.Text.StringBuilder 512
            $class = New-Object System.Text.StringBuilder 256
            [void][LuminaliumDiag2.Native]::GetWindowText($hWnd, $title, 512)
            [void][LuminaliumDiag2.Native]::GetClassName($hWnd, $class, 256)
            $rect = New-Object LuminaliumDiag2.Native+RECT
            [void][LuminaliumDiag2.Native]::GetWindowRect($hWnd, [ref]$rect)
            $result.Add([pscustomobject]@{
                Handle = $hWnd
                Visible = [LuminaliumDiag2.Native]::IsWindowVisible($hWnd)
                Title = $title.ToString()
                Class = $class.ToString()
                Bounds = "$($rect.Left),$($rect.Top) $($rect.Right - $rect.Left)x$($rect.Bottom - $rect.Top)"
            })
        }
        return $true
    }
    [void][LuminaliumDiag2.Native]::EnumWindows($callback, [System.IntPtr]::Zero)
    return $result
}

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
        if ($proc.HasExited) { break }
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
    Write-Host '--- windows BEFORE clicking OpenOverlay ---'
    (Get-WindowsForProcess -ProcessId $proc.Id) | Format-Table Handle, Visible, Title, Class, Bounds -AutoSize

    $openOverlay = $window.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, 'OpenOverlay')))
    $invoke = $openOverlay.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $invoke.Invoke()
    Write-Host 'Invoke() called OK'

    Start-Sleep -Milliseconds 2000
    Write-Host '--- windows AFTER clicking OpenOverlay ---'
    (Get-WindowsForProcess -ProcessId $proc.Id) | Format-Table Handle, Visible, Title, Class, Bounds -AutoSize

    # Try AutomationElement.FromHandle on the overlay window.
    $overlay = $null
    foreach ($w in (Get-WindowsForProcess -ProcessId $proc.Id)) {
        if ($w.Title -like '*overlay*' -and $w.Visible) {
            $overlay = [System.Windows.Automation.AutomationElement]::FromHandle($w.Handle)
            Write-Host "FromHandle overlay: name='$($overlay.Current.Name)' class=$($overlay.Current.ClassName)"
            break
        }
    }
    if ($null -ne $overlay) {
        $desc = $overlay.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        Write-Host "overlay descendants: $($desc.Count)"
        $recs = @($desc | ForEach-Object {
            $ct = ''
            try { $ct = $_.Current.ControlType.ProgrammaticName } catch { }
            [pscustomobject]@{ Name = $_.Current.Name; Type = $ct; Enabled = $_.Current.IsEnabled; Offscreen = $_.Current.IsOffscreen }
        })
        $recs | Where-Object { $_.Name } | Format-Table -AutoSize
    }
    else {
        Write-Host 'FromHandle overlay not found'
    }

    Start-Sleep -Milliseconds 3000
    Write-Host '--- windows after 5s total ---'
    (Get-WindowsForProcess -ProcessId $proc.Id) | Format-Table Handle, Visible, Title, Class, Bounds -AutoSize

    if ($proc.HasExited) { Write-Host "process exited code=$($proc.ExitCode)" }
    else { Write-Host 'process still alive' }
}
finally {
    if ($null -ne $proc -and -not $proc.HasExited) {
        $closed = $proc.CloseMainWindow()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill(); $proc.WaitForExit() }
    }
}
