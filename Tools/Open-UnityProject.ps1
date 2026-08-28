[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ProjectPath = '',
    [string]$UnityPath,
    [switch]$SkipPreflight
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $scriptDirectory
}

$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$preflightPath = Join-Path $scriptDirectory 'Inspect-UnityProjectWorkspace.ps1'

if (-not $SkipPreflight) {
    & $preflightPath -ProjectPath $resolvedProject
    if ($LASTEXITCODE -ne 0) {
        throw 'Unity project preflight failed. Fix the reported errors before opening the editor.'
    }
}

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $versionPath = Join-Path $resolvedProject 'ProjectSettings\ProjectVersion.txt'
    $versionMatch = Select-String -LiteralPath $versionPath -Pattern '^m_EditorVersion:\s*(\S+)' | Select-Object -First 1
    if ($null -eq $versionMatch) {
        throw 'Unable to read m_EditorVersion from ProjectSettings/ProjectVersion.txt.'
    }

    $unityVersion = $versionMatch.Matches[0].Groups[1].Value
    $UnityPath = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
}

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity editor was not found: $UnityPath"
}

$escapedProject = [regex]::Escape($resolvedProject)
$savedWhatIfPreference = $WhatIfPreference
$WhatIfPreference = $false
try {
    $matchingProcess = @(
        Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -match $escapedProject }
    ) | Select-Object -First 1
}
finally {
    $WhatIfPreference = $savedWhatIfPreference
}

if ($null -ne $matchingProcess) {
    Write-Host "Unity already has this project open (PID $($matchingProcess.ProcessId))."
    exit 0
}

Write-Host "Opening Unity project with: $UnityPath"
Write-Host "Project: $resolvedProject"
if ($PSCmdlet.ShouldProcess($resolvedProject, "Open with $UnityPath")) {
    Start-Process -FilePath $UnityPath -ArgumentList @('-projectPath', "`"$resolvedProject`"") -WorkingDirectory (Split-Path -Parent $UnityPath)
}
