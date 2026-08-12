[CmdletBinding()]
param([ValidateRange(30, 300)][int]$TimeoutSeconds = 300)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$logRoot = Join-Path $repo 'tmp\luoyang-184-historical-v1'
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
        Get-Content -LiteralPath $stderr -Tail 100
        throw "$Name failed with exit code $($process.ExitCode). See $stderr."
    }
    Write-Host "PASS $Name ($stdout)"
}

$baseWorld = Join-Path $repo 'Assets\StreamingAssets\WorldMap\LuoyangWorldV1\luoyang_world.json'
$basePopulation = Join-Path $repo 'MapData\LuoyangWorld_V1\population\recommended_persons.jsonl'
if (-not (Test-Path -LiteralPath $baseWorld) -or -not (Test-Path -LiteralPath $basePopulation)) {
    throw 'MASTER-MAP-V1 Luoyang baseline is missing. Run MapPipeline\Build-LuoyangWorldV1.ps1 first.'
}

Invoke-BoundedPythonStage 'luoyang-184-generation' (Join-Path $PSScriptRoot 'scripts\build_luoyang_184_historical_v1.py')
Invoke-BoundedPythonStage 'luoyang-184-validation' (Join-Path $PSScriptRoot 'scripts\validate_luoyang_184_historical_v1.py')
Write-Host 'PASS LUOYANG-184-HISTORICAL-V1 one-click generation and data validation.'
