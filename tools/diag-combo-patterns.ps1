# Diagnostic: after opening the Settings theme ComboBox, dump the patterns it
# exposes, read current selection through every available mechanism, select a
# differing option, and re-read. Goal: find a reliable way for the harness to
# assert the theme actually changed.
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

Add-Type -Namespace LuminaliumDiag7 -Name Native -MemberDefinition @'
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool PostMessage(System.IntPtr hWnd, uint Msg, System.IntPtr wParam, System.IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool ScreenToClient(System.IntPtr hWnd, ref POINT lpPoint);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(System.IntPtr hWnd);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    public struct POINT { public int X; public int Y; }
'@

$root = [System.Windows.Automation.AutomationElement]::RootElement
$HWND_TOPMOST = New-Object System.IntPtr -1
$HWND_NOTOPMOST = New-Object System.IntPtr -2
$SWP_NOSIZE = 0x0001
$SWP_NOMOVE = 0x0002
$WM_LBUTTONDOWN = 0x0201
$WM_LBUTTONUP = 0x0202
$MK_LBUTTON = 0x0001

function Click-ElementClient {
    param([System.Windows.Automation.AutomationElement]$Element, [System.IntPtr]$Handle)
    $b = $Element.Current.BoundingRectangle
    $pt = New-Object LuminaliumDiag7.Native+POINT
    $pt.X = [int][math]::Round($b.X + $b.Width / 2)
    $pt.Y = [int][math]::Round($b.Y + $b.Height / 2)
    [void][LuminaliumDiag7.Native]::ScreenToClient($Handle, [ref]$pt)
    $lParam = [System.IntPtr](($pt.Y -shl 16) -bor ($pt.X -band 0xFFFF))
    [void][LuminaliumDiag7.Native]::PostMessage($Handle, $WM_LBUTTONDOWN, [System.IntPtr]$MK_LBUTTON, $lParam)
    Start-Sleep -Milliseconds 80
    [void][LuminaliumDiag7.Native]::PostMessage($Handle, $WM_LBUTTONUP, [System.IntPtr]0, $lParam)
    Start-Sleep -Milliseconds 300
}

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

function Get-Patterns {
    param([System.Windows.Automation.AutomationElement]$Element)
    $list = @()
    foreach ($p in @([System.Windows.Automation.SelectionPattern]::Pattern,
                     [System.Windows.Automation.SelectionItemPattern]::Pattern,
                     [System.Windows.Automation.ExpandCollapsePattern]::Pattern,
                     [System.Windows.Automation.InvokePattern]::Pattern,
                     [System.Windows.Automation.ValuePattern]::Pattern,
                     [System.Windows.Automation.TextPattern]::Pattern)) {
        try { if ($null -ne $Element.GetCurrentPattern($p)) { $list += $p.ProgrammaticName } } catch { }
    }
    return ($list -join ', ')
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

    try {
        $wp = $window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
        $wp.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Maximized)
    }
    catch { }
    Start-Sleep -Milliseconds 800

    # Navigate to Settings via posted click.
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
    Click-ElementClient -Element $footer -Handle $hwnd
    Start-Sleep -Milliseconds 1200

    # Find the theme combo.
    $comboCond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, '主题模式')),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::ComboBox)))
    $combo = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $comboCond)
    if ($null -eq $combo) { Write-Host 'FAIL: theme combo not found'; exit 1 }
    Write-Host "combo patterns: $(Get-Patterns $combo)"

    # Read selection via SelectionPattern.
    try {
        $sel = $combo.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern)
        $cur = @($sel.Current.GetSelection())
        Write-Host "SelectionPattern.GetSelection count=$($cur.Count)"
        foreach ($c in $cur) { Write-Host "  selected: name='$($c.Current.Name)'" }
    }
    catch { Write-Host "SelectionPattern read threw: $($_.Exception.Message)" }

    # Read via ValuePattern.
    try {
        $val = $combo.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
        Write-Host "ValuePattern.Value='$($val.Current.Value)' IsReadOnly=$($val.Current.IsReadOnly)"
    }
    catch { Write-Host "ValuePattern read threw: $($_.Exception.Message)" }

    # Read via LegacyIAccessible (fallback).
    try {
        $legacy = $combo.GetCurrentPattern([System.Windows.Automation.LegacyIAccessiblePattern]::Pattern)
        Write-Host "LegacyIAccessible.Value='$($legacy.Current.Value)' Name='$($legacy.Current.Name)'"
    }
    catch { Write-Host "LegacyIAccessible threw: $($_.Exception.Message)" }

    # Children of combo (should be the selected item text / button).
    Write-Host '--- combo children ---'
    foreach ($c in ($combo.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition))) {
        Write-Host "  child: name='$($c.Current.Name)' type=$($c.Current.ControlType.ProgrammaticName) patterns=[$(Get-Patterns $c)]"
    }

    # Open the dropdown via posted click on the combo.
    Click-ElementClient -Element $combo -Handle $hwnd
    Start-Sleep -Milliseconds 600

    # Now search the whole desktop for popup ListItems.
    Write-Host '--- desktop ListItems after opening combo ---'
    $popupItems = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::ListItem)))
    foreach ($item in $popupItems) {
        $selS = ''
        try { $selS = $item.Current.IsSelected } catch { }
        Write-Host "  ListItem name='$($item.Current.Name)' selected=$selS patterns=[$(Get-Patterns $item)]"
    }

    # Pick a differing theme option.
    $target = $null
    foreach ($item in $popupItems) {
        $n = $item.Current.Name
        if ($n -in @('浅色', '深色', 'Light', 'Dark')) { $target = $item; break }
    }
    if ($null -ne $target) {
        Write-Host "target='$($target.Current.Name)'"
        # Try Select() via SelectionItemPattern
        try {
            $sil = $target.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
            $sil.Select()
            Write-Host 'SelectionItemPattern.Select() OK'
        }
        catch { Write-Host "Select() threw: $($_.Exception.Message)" }
        Start-Sleep -Milliseconds 800

        # Re-read selection after.
        try {
            $sel2 = $combo.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern)
            $cur2 = @($sel2.Current.GetSelection())
            Write-Host "after select: SelectionPattern count=$($cur2.Count)"
            foreach ($c in $cur2) { Write-Host "  selected: name='$($c.Current.Name)'" }
        }
        catch { Write-Host "SelectionPattern re-read threw: $($_.Exception.Message)" }
        try {
            $val2 = $combo.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
            Write-Host "after select: ValuePattern.Value='$($val2.Current.Value)'"
        }
        catch { }
    }
    else {
        Write-Host 'no target found in popup'
    }
}
finally {
    if ($null -ne $window) {
        try { [void][LuminaliumDiag7.Native]::SetWindowPos($window.Current.NativeWindowHandle, $HWND_NOTOPMOST, 0, 0, 0, 0, ($SWP_NOSIZE -bor $SWP_NOMOVE)) } catch { }
    }
    if ($null -ne $proc -and -not $proc.HasExited) {
        $closed = $proc.CloseMainWindow()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill(); $proc.WaitForExit() }
    }
}
