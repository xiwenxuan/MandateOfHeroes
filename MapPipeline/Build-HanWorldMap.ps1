[CmdletBinding()]
param([ValidateRange(30, 300)][int]$TimeoutSeconds = 300)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$runner = Join-Path $PSScriptRoot 'scripts\Invoke-QgisPython.ps1'
$logRoot = Join-Path $repo 'tmp\map-pipeline'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null

function Invoke-MapStage {
    param([string]$Name, [string]$Script)
    $stdout = Join-Path $logRoot "$Name.out.log"
    $stderr = Join-Path $logRoot "$Name.err.log"
    $scriptPath = Join-Path $PSScriptRoot "scripts\$Script"
    $processInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $processInfo.FileName = 'powershell.exe'
    $processInfo.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$runner`" `"$scriptPath`""
    $processInfo.WorkingDirectory = $repo
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $processInfo
    if (-not $process.Start()) {
        throw "$Name failed to start."
    }
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

Invoke-MapStage 'environment-audit' 'audit_environment.py'
Invoke-MapStage 'master-map' 'build_master_map.py'
Invoke-MapStage 'cell-grid' 'build_cell_grid.py'
Invoke-MapStage 'pipeline-validation' 'validate_pipeline.py'
