# Diagnostic: does UIA SelectionItemPattern.Select() or InvokePattern.Invoke()
# on the footer "设置" FANavigationViewItem actually navigate the shell?
# Tests each pattern against the real footer item and reports page markers.
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
    Write-Host "main window: $($window.Current.Name)"

    # Find the footer (bottom-most) 设置 ListItem.
    $all = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, '设置')))
    $footer = $null; $maxY = -1
    foreach ($el in $all) {
        try {
            if ($el.Current.ControlType.ProgrammaticName -ne 'ControlType.ListItem') { continue }
            if ($el.Current.IsOffscreen) { continue }
            $y = $el.Current.BoundingRectangle.Y
            if ($y -gt $maxY) { $footer = $el; $maxY = $y }
        }
        catch { }
    }
    if ($null -eq $footer) { Write-Host 'FAIL: footer 设置 ListItem not found'; exit 1 }
    Write-Host "footer 设置 ListItem Y=$maxY"

    # Show which patterns the item supports.
    $patterns = @()
    foreach ($p in @([System.Windows.Automation.SelectionItemPattern]::Pattern,
                     [System.Windows.Automation.InvokePattern]::Pattern,
                     [System.Windows.Automation.ExpandCollapsePattern]::Pattern)) {
        try { if ($null -ne $footer.GetCurrentPattern($p)) { $patterns += $p.ProgrammaticName } } catch { }
    }
    Write-Host "footer supports: $($patterns -join ', ')"

    $marker = { param($w) (Get-Names $w) -contains '主题模式' }

    # 1) Try SelectionItemPattern.Select()
    Write-Host '--- try SelectionItemPattern.Select() ---'
    try {
        $sel = $footer.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        $sel.Select()
        Start-Sleep -Milliseconds 1500
    }
    catch { Write-Host "  Select() threw: $($_.Exception.Message)" }
    if (& $marker $window) { Write-Host '  RESULT: NAVIGATED (主题模式 found)' }
    else { Write-Host '  RESULT: not navigated (still onboarding)' }

    # 2) Try InvokePattern.Invoke() on the same footer item
    Write-Host '--- try InvokePattern.Invoke() ---'
    try {
        $inv = $footer.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $inv.Invoke()
        Start-Sleep -Milliseconds 1500
    }
    catch { Write-Host "  Invoke() threw: $($_.Exception.Message)" }
    if (& $marker $window) { Write-Host '  RESULT: NAVIGATED (主题模式 found)' }
    else { Write-Host '  RESULT: not navigated (still onboarding)' }

    # 3) Try clicking the nav view itself via pattern on the NavigationView
    Write-Host '--- try SelectionItemPattern.Select() on the 概览 footer item first, then 设置 ---'
    # (概览 and 设置 are the only footer items; Select() the Overview one to force
    # the nav view's selection machinery, then Select() Settings again.)
    $all2 = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, '概览')))
    $overviewFooter = $null; $maxY2 = -1
    foreach ($el in $all2) {
        try {
            if ($el.Current.ControlType.ProgrammaticName -ne 'ControlType.ListItem') { continue }
            if ($el.Current.IsOffscreen) { continue }
            $y = $el.Current.BoundingRectangle.Y
            if ($y -gt $maxY2) { $overviewFooter = $el; $maxY2 = $y }
        }
        catch { }
    }
    if ($null -ne $overviewFooter) {
        Write-Host "overview footer ListItem Y=$maxY2"
        try {
            $selO = $overviewFooter.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
            $selO.Select()
            Start-Sleep -Milliseconds 800
            Write-Host '  selected Overview footer'
        }
        catch { Write-Host "  overview Select() threw: $($_.Exception.Message)" }
        try {
            $sel = $footer.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
            $sel.Select()
            Start-Sleep -Milliseconds 1500
            Write-Host '  selected Settings footer after Overview'
        }
        catch { Write-Host "  settings Select() #2 threw: $($_.Exception.Message)" }
    }
    if (& $marker $window) { Write-Host '  RESULT: NAVIGATED (主题模式 found)' }
    else { Write-Host '  RESULT: not navigated' }

    Write-Host '--- post-navigation names ---'
    (Get-Names $window) -join ' | '
}
finally {
    if ($null -ne $proc -and -not $proc.HasExited) {
        $closed = $proc.CloseMainWindow()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill(); $proc.WaitForExit() }
    }
}
