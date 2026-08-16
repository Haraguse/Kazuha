# Diagnostic: understand the Settings theme ComboBox UIA structure - what
# patterns the combo supports, how the dropdown popup is exposed, and how to
# read/change the selected theme option.
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
Add-Type -Namespace LuminaliumDiag3 -Name Native -MemberDefinition @'
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, System.UIntPtr dwExtraInfo);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(System.IntPtr hWnd);
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
'@
$root = [System.Windows.Automation.AutomationElement]::RootElement
$script:LD = 0x0002
$script:LU = 0x0004

# Background processes cannot normally activate a window; use the
# AttachThreadInput trick and verify the foreground actually moved.
function Set-ForegroundWindowRobust {
    param([System.IntPtr]$Handle)
    for ($i = 0; $i -lt 6; $i++) {
        if ([LuminaliumDiag3.Native]::GetForegroundWindow() -eq $Handle) { return $true }
        [void][LuminaliumDiag3.Native]::ShowWindow($Handle, 9) # SW_RESTORE
        $fg = [LuminaliumDiag3.Native]::GetForegroundWindow()
        $fgThread = 0; [void][LuminaliumDiag3.Native]::GetWindowThreadProcessId($fg, [ref]$fgThread)
        $appThread = 0; [void][LuminaliumDiag3.Native]::GetWindowThreadProcessId($Handle, [ref]$appThread)
        $selfThread = [LuminaliumDiag3.Native]::GetCurrentThreadId()
        $attached = $false
        if ($fgThread -ne 0 -and $fgThread -ne $appThread) {
            $attached = [LuminaliumDiag3.Native]::AttachThreadInput($fgThread, $selfThread, $true)
        }
        try {
            [void][LuminaliumDiag3.Native]::BringWindowToTop($Handle)
            [void][LuminaliumDiag3.Native]::SetForegroundWindow($Handle)
        }
        finally {
            if ($attached) { [void][LuminaliumDiag3.Native]::AttachThreadInput($fgThread, $selfThread, $false) }
        }
        Start-Sleep -Milliseconds 150
    }
    return ([LuminaliumDiag3.Native]::GetForegroundWindow() -eq $Handle)
}

function Click-Screen {
    param([double]$X, [double]$Y)
    [void][LuminaliumDiag3.Native]::SetCursorPos([int][math]::Round($X), [int][math]::Round($Y))
    Start-Sleep -Milliseconds 60
    [LuminaliumDiag3.Native]::mouse_event($script:LD, 0, 0, 0, [System.UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [LuminaliumDiag3.Native]::mouse_event($script:LU, 0, 0, 0, [System.UIntPtr]::Zero)
    Start-Sleep -Milliseconds 120
}

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = (Resolve-Path -LiteralPath $AppPath).Path
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$proc = [System.Diagnostics.Process]::Start($psi)

function Get-Patterns {
    param([System.Windows.Automation.AutomationElement]$Element)
    $list = @()
    foreach ($p in @([System.Windows.Automation.SelectionPattern]::Pattern,
                     [System.Windows.Automation.SelectionItemPattern]::Pattern,
                     [System.Windows.Automation.ExpandCollapsePattern]::Pattern,
                     [System.Windows.Automation.InvokePattern]::Pattern,
                     [System.Windows.Automation.ValuePattern]::Pattern)) {
        try {
            if ($null -ne $Element.GetCurrentPattern($p)) { $list += $p.ProgrammaticName }
        }
        catch { }
    }
    return ($list -join ', ')
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

    # Navigate to Settings via real click.
    $fgOk = Set-ForegroundWindowRobust -Handle $window.Current.NativeWindowHandle
    Write-Host "foreground set: $fgOk (fg=$([LuminaliumDiag3.Native]::GetForegroundWindow()) target=$($window.Current.NativeWindowHandle))"
    Start-Sleep -Milliseconds 300
    $settingsItem = $null
    $settingsY = -1
    $allItems = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    Write-Host '--- all ListItems ---'
    foreach ($el in $allItems) {
        try {
            if ($el.Current.ControlType.ProgrammaticName -eq 'ControlType.ListItem') {
                $r = $el.Current.BoundingRectangle
                Write-Host ("  ListItem name='{0}' offscreen={1} bounds=({2},{3} {4}x{5})" -f $el.Current.Name, $el.Current.IsOffscreen, $r.X, $r.Y, $r.Width, $r.Height)
                # There can be TWO "设置" ListItems in the UIA tree (a stale/
                # phantom one near the top of the pane and the real footer
                # item). Pick the bottom-most (largest Y) - the live footer
                # navigation item - in the same pass.
                if ($el.Current.Name -eq '设置' -and -not $el.Current.IsOffscreen) {
                    $y = $r.Y
                    if ($y -gt $settingsY) { $settingsItem = $el; $settingsY = $y }
                }
                elseif ($el.Current.Name -like '*设*' -or $el.Current.Name -like '*置*') {
                    $codes = ($el.Current.Name.ToCharArray() | ForEach-Object { [int]$_ }) -join ','
                    $litCodes = ('设置'.ToCharArray() | ForEach-Object { [int]$_ }) -join ','
                    Write-Host "    NEAR-MATCH name='$($el.Current.Name)' codes=[$codes] literal codes=[$litCodes]"
                }
            }
        }
        catch { }
    }
    Write-Host '--- window bounds ---'
    $wr = $window.Current.BoundingRectangle
    Write-Host "  window bounds=($($wr.X),$($wr.Y) $($wr.Width)x$($wr.Height))"
    if ($null -eq $settingsItem) { Write-Host 'FAIL: settings ListItem not found'; exit 1 }
    $b = $settingsItem.Current.BoundingRectangle
    $clickX = $b.X + $b.Width / 2
    $clickY = $b.Y + $b.Height / 2
    Write-Host "settings bounds=($($b.X),$($b.Y) $($b.Width)x$($b.Height)) -> click($clickX,$clickY)"
    Click-Screen $clickX $clickY

    # Poll for the theme combo; navigation + page load is asynchronous.
    # The combo's AutomationProperties.Name is bound to ThemeModeLabel ("主题模式"),
    # but the label TextBlock shares the same name. Search for a ComboBox specifically.
    $combo = $null
    $comboDeadline = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $comboDeadline -and $null -eq $combo) {
        try {
            $combo = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
                (New-Object System.Windows.Automation.AndCondition(
                    (New-Object System.Windows.Automation.PropertyCondition(
                        [System.Windows.Automation.AutomationElement]::NameProperty, '主题模式')),
                    (New-Object System.Windows.Automation.PropertyCondition(
                        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                        [System.Windows.Automation.ControlType]::ComboBox)))))
        }
        catch { }
        if ($null -eq $combo) { Start-Sleep -Milliseconds 250 }
    }
    if ($null -eq $combo) {
        Write-Host 'FAIL: theme combo not found'
        Write-Host '--- post-nav names ---'
        $names = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition) |
            ForEach-Object { try { $_.Current.Name } catch { '' } } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique
        Write-Host ($names -join ' | ')
        exit 1
    }
    Write-Host "combo: name='$($combo.Current.Name)' type=$($combo.Current.ControlType.ProgrammaticName)"
    Write-Host "combo patterns: $(Get-Patterns $combo)"
    Write-Host "combo bounds: $($combo.Current.BoundingRectangle)"
    Write-Host '--- combo children (before expand) ---'
    foreach ($c in ($combo.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition))) {
        Write-Host "  child: name='$($c.Current.Name)' type=$($c.Current.ControlType.ProgrammaticName) patterns=[$(Get-Patterns $c)]"
    }

    # Open the combo with a real click.
    $cb = $combo.Current.BoundingRectangle
    Click-Screen ($cb.X + $cb.Width / 2) ($cb.Y + $cb.Height / 2)
    Start-Sleep -Milliseconds 800

    Write-Host '--- after opening combo: search whole desktop for popup items ---'
    # Avalonia ComboBox dropdown is an owned popup window, NOT a child of the
    # main window, so search the UIA root (all top-level windows) for items.
    $popupItems = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::ListItem)))
    foreach ($item in $popupItems) {
        $sel = ''
        try { $sel = $item.Current.IsSelected } catch { }
        Write-Host "  ListItem: name='$($item.Current.Name)' patterns=[$(Get-Patterns $item)] selected=$sel bounds=$($item.Current.BoundingRectangle)"
    }

    # Try selecting an item that differs from current via SelectionItemPattern.
    $target = $null
    foreach ($item in $popupItems) {
        $n = $item.Current.Name
        if ($n -in @('浅色', '深色', '系统', 'Light', 'Dark')) { $target = $item; break }
    }
    if ($null -ne $target) {
        Write-Host "attempting SelectionItemPattern.Select() on '$($target.Current.Name)'"
        try {
            $sel = $target.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
            $sel.Select()
            Write-Host '  Select() OK'
        }
        catch {
            Write-Host "  Select() threw: $($_.Exception.Message)"
            # fallback: real click
            $tb = $target.Current.BoundingRectangle
            Click-Screen ($tb.X + $tb.Width / 2) ($tb.Y + $tb.Height / 2)
            Write-Host '  clicked target instead'
        }
        Start-Sleep -Milliseconds 800
    }
    else {
        Write-Host 'no selectable target found in popup'
    }
}
finally {
    if ($null -ne $proc -and -not $proc.HasExited) {
        $closed = $proc.CloseMainWindow()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill(); $proc.WaitForExit() }
    }
}
