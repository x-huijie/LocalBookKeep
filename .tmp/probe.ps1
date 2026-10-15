$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Find-Window([string]$name, [int]$timeoutSec = 20) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    while ((Get-Date) -lt $deadline) {
        $w = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
        if ($w) { return $w }
        Start-Sleep -Milliseconds 300
    }
    return $null
}

$win = Find-Window "本地记账 LocalBookKeep"
if (-not $win) { Write-Host "MAIN WINDOW NOT FOUND"; exit 1 }
Write-Host "MAIN OK: $($win.Current.Name)"

$btnType = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Button)
foreach ($b in $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnType)) {
    Write-Host "BUTTON: [$($b.Current.Name)]"
}

$editType = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Edit)
foreach ($e in $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editType)) {
    Write-Host "EDIT: [$($e.Current.Name)] autoid=[$($e.Current.AutomationId)]"
}
