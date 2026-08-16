# Diagnostic: verify screen geometry, DPI, and whether a synthetic mouse click
# actually lands on the app window at the footer "设置" item's coordinates.
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
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type -Namespace LuminaliumDiag4 -Name Native -MemberDefinition @'
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, System.UIntPtr dwExtraInfo);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern System.IntPtr SetForegroundWindow(System.IntPtr hWnd);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern System.IntPtr GetForegroundWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool BringWindowToTop(System.IntPtr hWnd);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, out uint lpdwProcessId);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern System.IntPtr WindowFromPoint(int x, int y);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool GetWindowRect(System.IntPtr hWnd, out RECT lpRect);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
'@

$script:LD = 0x0002
$script:LU = 0x0004
$HWND_TOPMOST = New-Object System.IntPtr -1
$HWND_NOTOPMOST = New-Object System.IntPtr -2
$SWP_NOSIZE = 0x0001
$SWP_NOMOVE = 0x0002
$SWP_SHOWWINDOW = 0x0040

$root = [System.Windows.Automation.AutomationElement]::RootElement

# Screen geometry (physical pixels)
foreach ($s in [System.Windows.Forms.Screen]::AllScreens) {
    Write-Host ("Screen: bounds=({0},{1} {2}x{3}) primary={4} dpi={5}x{6} scale={7}" -f `
        $s.Bounds.X, $s.Bounds.Y, $s.Bounds.Width, $s.Bounds.Height, $s.Primary, `
        $s.BitsPerPixel, '', $s.Primary)
}
$ws = [System.Windows.Forms.SystemInformation]::VirtualScreen
Write-Host "VirtualScreen: $($ws.ToString())"

function Click-Screen {
    param([double]$X, [double]$Y)
    [void][LuminaliumDiag4.Native]::SetCursorPos([int][math]::Round($X), [int][math]::Round($Y))
    Start-Sleep -Milliseconds 60
    [LuminaliumDiag4.Native]::mouse_event($script:LD, 0, 0, 0, [System.UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [LuminaliumDiag4.Native]::mouse_event($script:LU, 0, 0, 0, [System.UIntPtr]::Zero)
    Start-Sleep -Milliseconds 120
}

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = (Resolve-Path -LiteralPath $AppPath).Path
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$proc = [System.Diagnostics.Process]::Start($psi)

function Get-Names {
    param([System.Windows.Automation.AutomationElement]$Window)
    $names = @()
    try {
        $names = $Window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition) |
            ForEach-Object { try { $_.Current.Name } catch { '' } } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique
    }
    catch { }
    return $names
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
    Write-Host "main window hwnd=$hwnd name='$($window.Current.Name)'"

    $wr = New-Object LuminaliumDiag4.Native+RECT
    [void][LuminaliumDiag4.Native]::GetWindowRect($hwnd, [ref]$wr)
    Write-Host "Win32 window rect=($($wr.Left),$($wr.Top) $($wr.Right-$wr.Left)x$($wr.Bottom-$wr.Top))"
    Write-Host "UIA window rect=$($window.Current.BoundingRectangle)"

    # Maximize via UIA WindowPattern if available (like harness).
    try {
        $wp = $window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
        $wp.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Maximized)
        Write-Host 'maximized via UIA WindowPattern'
    }
    catch { Write-Host "no WindowPattern: $($_.Exception.Message)" }
    Start-Sleep -Milliseconds 800
    [void][LuminaliumDiag4.Native]::GetWindowRect($hwnd, [ref]$wr)
    Write-Host "after maximize rect=($($wr.Left),$($wr.Top) $($wr.Right-$wr.Left)x$($wr.Bottom-$wr.Top))"

    # Footer 设置 item
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
    $cx = $b.X + $b.Width / 2
    $cy = $b.Y + $b.Height / 2
    Write-Host "footer 设置 bounds=$($b) click=($cx,$cy)"

    # Before click: what window is at the click point?
    $hitBefore = [LuminaliumDiag4.Native]::WindowFromPoint([int]$cx, [int]$cy)
    Write-Host "WindowFromPoint(click) before click = $hitBefore (app hwnd=$hwnd) match=$($hitBefore -eq $hwnd)"

    # Force window to topmost + foreground.
    [void][LuminaliumDiag4.Native]::SetWindowPos($hwnd, $HWND_TOPMOST, 0, 0, 0, 0, ($SWP_NOSIZE -bor $SWP_NOMOVE -bor $SWP_SHOWWINDOW))
    Start-Sleep -Milliseconds 300
    [void][LuminaliumDiag4.Native]::ShowWindow($hwnd, 9) # SW_RESTORE
    $fg = [LuminaliumDiag4.Native]::GetForegroundWindow()
    Write-Host "fg before click after topmost: $fg (match=$($fg -eq $hwnd))"
    [void][LuminaliumDiag4.Native]::SetForegroundWindow($hwnd)
    Start-Sleep -Milliseconds 200
    $fg2 = [LuminaliumDiag4.Native]::GetForegroundWindow()
    Write-Host "fg after SetForegroundWindow: $fg2 (match=$($fg2 -eq $hwnd))"

    # After topmost, re-check hit test at click point.
    $hitAfter = [LuminaliumDiag4.Native]::WindowFromPoint([int]$cx, [int]$cy)
    Write-Host "WindowFromPoint(click) after topmost = $hitAfter (match=$($hitAfter -eq $hwnd))"

    if ($hitAfter -eq $hwnd) {
        Write-Host 'clicking...'
        Click-Screen $cx $cy
        Start-Sleep -Milliseconds 1500
        $names = Get-Names -Window $window
        Write-Host "settings marker ('主题模式'): $($names -contains '主题模式')"
        if ($names -contains '主题模式') {
            Write-Host 'RESULT: NAVIGATED after topmost+click'
            # now try toggling the theme via real click on combo
        }
        else {
            Write-Host 'RESULT: not navigated'
        }
    }
    else {
        Write-Host 'SKIP click: hit-test not on app window even after topmost'
    }

    Write-Host '--- final names ---'
    (Get-Names -Window $window) -join ' | '
}
finally {
    # Restore topmost flag off.
    if ($null -ne $window) {
        try { [void][LuminaliumDiag4.Native]::SetWindowPos($window.Current.NativeWindowHandle, $HWND_NOTOPMOST, 0, 0, 0, 0, ($SWP_NOSIZE -bor $SWP_NOMOVE)) } catch { }
    }
    if ($null -ne $proc -and -not $proc.HasExited) {
        $closed = $proc.CloseMainWindow()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill(); $proc.WaitForExit() }
    }
}
