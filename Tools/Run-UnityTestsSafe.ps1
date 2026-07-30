param(
    [ValidateSet("EditMode", "PlayMode")]
    [string]$TestPlatform = "EditMode",
    [ValidateRange(30, 1800)]
    [int]$TimeoutSeconds = 300,
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$UnityPath =
        "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity executable not found: $UnityPath"
}

$existingUnity = Get-Process -Name Unity -ErrorAction SilentlyContinue
if ($null -ne $existingUnity) {
    $ids = ($existingUnity | Select-Object -ExpandProperty Id) -join ", "
    throw "Unity is already running (PID: $ids). Close the editor before batch tests."
}

$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$outputDirectory = Join-Path $resolvedProject "tmp"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$resultPath = Join-Path $outputDirectory "unity-$TestPlatform-$stamp.xml"
$logPath = Join-Path $outputDirectory "unity-$TestPlatform-$stamp.log"

$arguments = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $resolvedProject,
    "-runTests",
    "-testPlatform", $TestPlatform,
    "-testResults", $resultPath,
    "-logFile", $logPath,
    "-quit"
)

$process = Start-Process `
    -FilePath $UnityPath `
    -ArgumentList $arguments `
    -PassThru `
    -WindowStyle Hidden

Write-Host "Unity test PID: $($process.Id)"
Write-Host "Log: $logPath"
Write-Host "Result: $resultPath"

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while (-not $process.HasExited -and (Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    $process.Refresh()
}

if (-not $process.HasExited) {
    & "C:\Windows\System32\taskkill.exe" `
        /PID $process.Id `
        /T `
        /F 2>$null | Out-Null
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    Write-Error "Unity tests exceeded $TimeoutSeconds seconds and PID $($process.Id) was terminated."
    if (Test-Path -LiteralPath $logPath) {
        Get-Content -LiteralPath $logPath -Tail 80
    }
    exit 124
}

if (Test-Path -LiteralPath $logPath) {
    Get-Content -LiteralPath $logPath -Tail 80
}

if (-not (Test-Path -LiteralPath $resultPath)) {
    Write-Error "Unity exited with code $($process.ExitCode) without a test result file."
    exit 2
}

Write-Host "Unity exited with code $($process.ExitCode)."
exit $process.ExitCode
