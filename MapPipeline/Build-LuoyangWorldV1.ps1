[CmdletBinding()]
param([ValidateRange(30, 300)][int]$TimeoutSeconds = 300)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$logRoot = Join-Path $repo 'tmp\map-pipeline-v1'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null

function Invoke-BoundedPythonStage {
    param([string]$Name, [string]$Script)
    $stdout = Join-Path $logRoot "$Name.out.log"
    $stderr = Join-Path $logRoot "$Name.err.log"
    $info = [System.Diagnostics.ProcessStartInfo]::new()
    $info.FileName = 'python.exe'
    $info.Arguments = "`"$Script`""
    $info.WorkingDirectory = $repo
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $info
    if (-not $process.Start()) { throw "$Name failed to start." }
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while (-not $process.HasExited -and (Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 1
        $process.Refresh()
    }
    if (-not $process.HasExited) {
        & C:\Windows\System32\taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null
        throw "$Name exceeded the $TimeoutSeconds second hard timeout. See $stdout and $stderr."
    }
    $stdoutTask.GetAwaiter().GetResult() | Set-Content -LiteralPath $stdout -Encoding UTF8
    $stderrTask.GetAwaiter().GetResult() | Set-Content -LiteralPath $stderr -Encoding UTF8
    if ($process.ExitCode -ne 0) {
        Get-Content -LiteralPath $stderr -Tail 80
        throw "$Name failed with exit code $($process.ExitCode)."
    }
    Write-Host "PASS $Name ($stdout)"
}

$env:MANDATE_WORLD_MAP_VERSION = 'HanWorldV1'
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Build-HanWorldMap.ps1') -TimeoutSeconds $TimeoutSeconds
if ($LASTEXITCODE -ne 0) { throw "HanWorldV1 pipeline failed with exit code $LASTEXITCODE." }
Invoke-BoundedPythonStage 'luoyang-world-generation' (Join-Path $PSScriptRoot 'scripts\build_luoyang_world_v1.py')
Invoke-BoundedPythonStage 'luoyang-world-validation' (Join-Path $PSScriptRoot 'scripts\validate_luoyang_world_v1.py')
Write-Host 'PASS MASTER-MAP-V1 one-click reproduction.'
