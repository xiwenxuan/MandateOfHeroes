param(
    [int]$TimeoutSeconds = 300,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$scriptPath = Join-Path $PSScriptRoot 'scripts\build_luoyang_population_stress_v1.py'
$auditPath = Join-Path $PSScriptRoot 'scripts\validate_luoyang_population_stress_v1.py'
$logRoot = Join-Path $projectRoot 'tmp\luoyang-population-stress-v1'
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$stdout = Join-Path $logRoot "build-$stamp.stdout.log"
$stderr = Join-Path $logRoot "build-$stamp.stderr.log"
$arguments = @($scriptPath, '--project-root', $projectRoot)
if ($Clean) { $arguments += '--clean' }
$info = [System.Diagnostics.ProcessStartInfo]::new()
$info.FileName = 'python.exe'
$info.Arguments = ($arguments | ForEach-Object { '"' + ($_ -replace '"', '\"') + '"' }) -join ' '
$info.WorkingDirectory = $projectRoot
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.RedirectStandardOutput = $true
$info.RedirectStandardError = $true
$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $info
if (-not $process.Start()) { throw 'Luoyang population stress build failed to start.' }
$stdoutTask = $process.StandardOutput.ReadToEndAsync()
$stderrTask = $process.StandardError.ReadToEndAsync()
$started = Get-Date
try {
    while (-not $process.HasExited) {
        if (((Get-Date) - $started).TotalSeconds -ge $TimeoutSeconds) {
            & C:\Windows\System32\taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null
            throw "Luoyang population stress build exceeded $TimeoutSeconds seconds. See $stdout and $stderr"
        }
        Start-Sleep -Seconds 1
        $process.Refresh()
    }
    $stdoutTask.GetAwaiter().GetResult() | Set-Content -LiteralPath $stdout -Encoding UTF8
    $stderrTask.GetAwaiter().GetResult() | Set-Content -LiteralPath $stderr -Encoding UTF8
    if ($process.ExitCode -ne 0) {
        Get-Content -LiteralPath $stderr -Tail 100
        throw "Build failed with exit code $($process.ExitCode). See $stderr"
    }
    & python $auditPath --project-root $projectRoot
    if ($LASTEXITCODE -ne 0) { throw "Independent stress audit failed with exit code $LASTEXITCODE" }
}
finally {
    if (-not $process.HasExited) { & C:\Windows\System32\taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null }
}
Get-Content -LiteralPath $stdout
