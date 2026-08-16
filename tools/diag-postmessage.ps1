# Diagnostic: can navigation be driven by PostMessage WM_LBUTTONDOWN/UP sent
# directly to the app window (client coords)? This bypasses any third-party
# topmost overlay (e.g. Dock_64's MyFinderApp layer) that intercepts real
# mouse clicks.
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

Add-Type -Namespace LuminaliumDiag6 -Name Native -MemberDefinition @'
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool PostMessage(System.IntPtr hWnd, uint Msg, System.IntPtr wParam, System.IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool GetClientRect(System.IntPtr hWnd, out RECT lpRect);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool ClientToScreen(System.IntPtr hWnd, ref POINT lpPoint);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool ScreenToClient(System.IntPtr hWnd, ref POINT lpPoint);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern System.IntPtr SetForegroundWindow(System.IntPtr hWnd);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern System.IntPtr GetForegroundWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    public struct POINT { public int X; public int Y; }
'@

$root = [System.Windows.Automation.AutomationElement]::RootElement
$HWND_TOPMOST = New-Object System.IntPtr -1
$HWND_NOTOPMOST = New-Object System.IntPtr -2
$SWP_NOSIZE = 0x0001
$SWP_NOMOVE = 0x0002
$SWP_SHOWWINDOW = 0x0040
$WM_LBUTTONDOWN = 0x0201
$WM_LBUTTONUP = 0x0202
$MK_LBUTTON = 0x0001

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

function Send-MouseToClient {
    param([System.IntPtr]$Hwnd, [int]$ClientX, [int]$ClientY)
    # lParam packs x in low word, y in high word (client coords, relative to window client area)
    $lParam = [System.IntPtr](($ClientY -shl 16) -bor ($ClientX -band 0xFFFF))
    [void][LuminaliumDiag6.Native]::PostMessage($Hwnd, $WM_LBUTTONDOWN, [System.IntPtr]$MK_LBUTTON, $lParam)
    Start-Sleep -Milliseconds 80
    [void][LuminaliumDiag6.Native]::PostMessage($Hwnd, $WM_LBUTTONUP, [System.IntPtr]0, $lParam)
    Start-Sleep -Milliseconds 300
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

    # Maximize so footer nav is inside the client area.
    try {
        $wp = $window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
        $wp.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Maximized)
    }
    catch { }
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
    $screenX = [int]($b.X + $b.Width / 2)
    $screenY = [int]($b.Y + $b.Height / 2)
    Write-Host "footer 设置 screen=($screenX,$screenY)"

    # Convert screen -> client coords.
    $pt = New-Object LuminaliumDiag6.Native+POINT
    $pt.X = $screenX; $pt.Y = $screenY
    [void][LuminaliumDiag6.Native]::ScreenToClient($hwnd, [ref]$pt)
    Write-Host "client coords=($($pt.X),$($pt.Y))"

    # Bring to foreground so the click has context.
    [void][LuminaliumDiag6.Native]::SetForegroundWindow($hwnd)
    Start-Sleep -Milliseconds 300
    [void][LuminaliumDiag6.Native]::SetWindowPos($hwnd, $HWND_TOPMOST, 0, 0, 0, 0, ($SWP_NOSIZE -bor $SWP_NOMOVE -bor $SWP_SHOWWINDOW))
    Start-Sleep -Milliseconds 300

    Write-Host "fg match? $([LuminaliumDiag6.Native]::GetForegroundWindow() -eq $hwnd)"

    # Send synthetic mouse down/up via PostMessage.
    Send-MouseToClient -Hwnd $hwnd -ClientX $pt.X -ClientY $pt.Y
    Start-Sleep -Milliseconds 800

    $names = Get-Names -Window $window
    Write-Host "settings marker ('主题模式'): $($names -contains '主题模式')"
    Write-Host '--- names ---'
    $names -join ' | '

    # If that failed, try SendMessage instead of PostMessage.
    if (-not ($names -contains '主题模式')) {
        Write-Host 'PostMessage failed, retrying with real click via mouse_event at screen coords (may be intercepted by overlay)'
    }
}
finally {
    if ($null -ne $window) {
        try { [void][LuminaliumDiag6.Native]::SetWindowPos($window.Current.NativeWindowHandle, $HWND_NOTOPMOST, 0, 0, 0, 0, ($SWP_NOSIZE -bor $SWP_NOMOVE)) } catch { }
    }
    if ($null -ne $proc -and -not $proc.HasExited) {
        $closed = $proc.CloseMainWindow()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill(); $proc.WaitForExit() }
    }
}
