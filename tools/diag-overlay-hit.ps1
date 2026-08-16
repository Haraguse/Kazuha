# Diagnostic: identify what window is at the footer Settings item's click
# point and what z-order/topmost state the app window has, to understand why
# synthetic clicks never land on the app.
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

Add-Type -Namespace LuminaliumDiag5 -Name Native -MemberDefinition @'
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern System.IntPtr WindowFromPoint(int x, int y);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool GetWindowRect(System.IntPtr hWnd, out RECT lpRect);
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    public static extern int GetWindowText(System.IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, out uint lpdwProcessId);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern int GetClassName(System.IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool IsWindowVisible(System.IntPtr hWnd);
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
'@

$root = [System.Windows.Automation.AutomationElement]::RootElement
$HWND_TOPMOST = New-Object System.IntPtr -1
$HWND_NOTOPMOST = New-Object System.IntPtr -2
$SWP_NOSIZE = 0x0001
$SWP_NOMOVE = 0x0002
$SWP_SHOWWINDOW = 0x0040

function Get-WinInfo {
    param([System.IntPtr]$Hwnd)
    if ($Hwnd -eq [System.IntPtr]::Zero) { return '(null)' }
    $sb = New-Object System.Text.StringBuilder 256
    [void][LuminaliumDiag5.Native]::GetWindowText($Hwnd, $sb, 256)
    $title = $sb.ToString()
    $cb = New-Object System.Text.StringBuilder 256
    [void][LuminaliumDiag5.Native]::GetClassName($Hwnd, $cb, 256)
    $cls = $cb.ToString()
    $processId = 0
    [void][LuminaliumDiag5.Native]::GetWindowThreadProcessId($Hwnd, [ref]$processId)
    $rect = New-Object LuminaliumDiag5.Native+RECT
    [void][LuminaliumDiag5.Native]::GetWindowRect($Hwnd, [ref]$rect)
    $visible = [LuminaliumDiag5.Native]::IsWindowVisible($Hwnd)
    return "hwnd=$Hwnd title='$title' class='$cls' pid=$processId visible=$visible rect=($($rect.Left),$($rect.Top) $($rect.Right-$rect.Left)x$($rect.Bottom-$rect.Top))"
}

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
        if ($proc.HasExited) { break }
        try {
            $windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $byProcess)
            foreach ($w in $windows) {
                try {
                    if ($null -ne $w.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $navCondition)) { $window = $w; break }
                }
                catch { }
            }
        }
        catch { }
        if ($null -eq $window) { Start-Sleep -Milliseconds 250 }
    }
    if ($null -eq $window) { Write-Host 'FAIL: main window not found'; exit 1 }
    $hwnd = $window.Current.NativeWindowHandle
    Write-Host "app hwnd=$hwnd"

    # Maximize via WindowPattern
    try {
        $wp = $window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
        $wp.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Maximized)
    }
    catch { [void][LuminaliumDiag5.Native]::SetWindowPos($hwnd, [System.IntPtr]::Zero, 0, 0, 0, 0, ($SWP_NOSIZE -bor $SWP_NOMOVE)) }
    Start-Sleep -Milliseconds 1000

    # Footer 设置
    $all = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, '设置')))
    $footer = $null; $maxY = -1
    foreach ($el in $all) {
        try {
            if ($el.Current.ControlType.ProgrammaticName -ne 'ControlType.ListItem') { continue }
            $y = $el.Current.BoundingRectangle.Y
            if ($y -gt $maxY) { $footer = $el; $maxY = $y }
        }
        catch { }
    }
    if ($null -eq $footer) { Write-Host 'FAIL: footer 设置 not found'; exit 1 }
    $b = $footer.Current.BoundingRectangle
    $cx = [int]($b.X + $b.Width / 2)
    $cy = [int]($b.Y + $b.Height / 2)
    Write-Host "footer 设置 click=($cx,$cy)"

    Write-Host '--- enumerate ALL top-level windows of the app process ---'
    foreach ($w in ($root.FindAll([System.Windows.Automation.TreeScope]::Children, $byProcess))) {
        Write-Host ("  app top-level: name='{0}' hwnd={1}" -f $w.Current.Name, $w.Current.NativeWindowHandle)
    }

    Write-Host '--- enumerate ALL top-level windows of ALL processes (visible) ---'
    $allWin = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($w in $allWin) {
        try {
            $h = $w.Current.NativeWindowHandle
            if ($h -eq [System.IntPtr]::Zero) { continue }
            $title = $w.Current.Name
            if ([string]::IsNullOrWhiteSpace($title)) { continue }
            Write-Host ("  hwnd={0} pid={1} name='{2}'" -f $h, $w.Current.ProcessId, $title)
        }
        catch { }
    }

    Write-Host '--- hit test at click point, walk ancestors ---'
    $hit = [LuminaliumDiag5.Native]::WindowFromPoint($cx, $cy)
    Write-Host "WindowFromPoint(click) = $hit"
    Write-Host (Get-WinInfo $hit)
    $parent = [System.Windows.Automation.AutomationElement]::FromHandle($hit)
    if ($null -ne $parent) {
        Write-Host "UIA name of hit window: '$($parent.Current.Name)' pid=$($parent.Current.ProcessId) automationid='$($parent.Current.AutomationId)'"
    }

    # Is the app window topmost? Try moving to topmost and re-hit-test.
    Write-Host '--- set app to TOPMOST and re-hit-test ---'
    [void][LuminaliumDiag5.Native]::SetWindowPos($hwnd, $HWND_TOPMOST, 0, 0, 0, 0, ($SWP_NOSIZE -bor $SWP_NOMOVE -bor $SWP_SHOWWINDOW))
    Start-Sleep -Milliseconds 600
    $hit2 = [LuminaliumDiag5.Native]::WindowFromPoint($cx, $cy)
    Write-Host "after topmost WindowFromPoint = $hit2"
    Write-Host (Get-WinInfo $hit2)
    Write-Host "match app? $($hit2 -eq $hwnd)"

    # enumerate top-level windows again but ordered, to see z-order relative
    Write-Host '--- all app-process top-level windows (any) ---'
    $appTops = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $byProcess)
    foreach ($w in $appTops) {
        Write-Host ("  app top: name='{0}' hwnd={1}" -f $w.Current.Name, $w.Current.NativeWindowHandle)
    }
}
finally {
    if ($null -ne $window) {
        try { [void][LuminaliumDiag5.Native]::SetWindowPos($window.Current.NativeWindowHandle, $HWND_NOTOPMOST, 0, 0, 0, 0, ($SWP_NOSIZE -bor $SWP_NOMOVE)) } catch { }
    }
    if ($null -ne $proc -and -not $proc.HasExited) {
        $closed = $proc.CloseMainWindow()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill(); $proc.WaitForExit() }
    }
}
