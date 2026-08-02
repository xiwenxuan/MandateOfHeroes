[CmdletBinding()]
param(
    [string]$ProjectPath,
    [ValidateRange(1, 1000000)]
    [int]$PersonCount = 10000,
    [long]$Seed = 14000015,
    [ValidateRange(30, 300)]
    [int]$TimeoutSeconds = 300,
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# PowerShell 5 Start-Process fails when the host exposes both Path and PATH.
$processPath = $env:Path
[Environment]::SetEnvironmentVariable("PATH", $null, "Process")
[Environment]::SetEnvironmentVariable("Path", $processPath, "Process")

function Stop-OwnedProcessTree {
    param([Parameter(Mandatory = $true)][int]$ProcessId)

    $taskkillPath = "C:\Windows\System32\taskkill.exe"
    if (Test-Path -LiteralPath $taskkillPath) {
        & $taskkillPath /PID $ProcessId /T /F 2>$null | Out-Null
    }
    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Invoke-BoundedProcess {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [Parameter(Mandatory = $true)][string]$LogDirectory,
        [Parameter(Mandatory = $true)][int]$HardTimeoutSeconds
    )

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
    $stdoutPath = Join-Path $LogDirectory "$Name-$stamp.out.log"
    $stderrPath = Join-Path $LogDirectory "$Name-$stamp.err.log"
    $exitCodePath = Join-Path $LogDirectory "$Name-$stamp.exit.txt"
    $childRunnerPath = Join-Path $PSScriptRoot "PopulationBenchmark\Run-Child.ps1"
    $runnerArguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $childRunnerPath,
        "-ToolPath", $FilePath,
        "-WorkingDirectory", $WorkingDirectory,
        "-ExitCodePath", $exitCodePath
    ) + $ArgumentList
    $runnerArguments = @(
        $runnerArguments | ForEach-Object {
            if ($_ -match "\s") {
                '"' + ($_ -replace '"', '\"') + '"'
            }
            else {
                $_
            }
        }
    )
    $startedAt = (Get-Date).ToUniversalTime()
    $process = Start-Process `
        -FilePath "powershell.exe" `
        -ArgumentList $runnerArguments `
        -WorkingDirectory $WorkingDirectory `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -PassThru `
        -WindowStyle Hidden

    Write-Host "$Name PID: $($process.Id)"
    Write-Host "$Name stdout: $stdoutPath"
    Write-Host "$Name stderr: $stderrPath"

    $deadline = (Get-Date).AddSeconds($HardTimeoutSeconds)
    while (-not $process.HasExited -and (Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        $process.Refresh()
    }

    $timedOut = -not $process.HasExited
    if ($timedOut) {
        Stop-OwnedProcessTree -ProcessId $process.Id
        $exitCode = -1
    }
    else {
        $process.WaitForExit()
        if (-not (Test-Path -LiteralPath $exitCodePath -PathType Leaf)) {
            throw "$Name exited without an exit-code result file."
        }
        $exitCode = [int](Get-Content -Raw -LiteralPath $exitCodePath)
    }

    $endedAt = (Get-Date).ToUniversalTime()
    $metadata = [ordered]@{
        schema_version = "m15.p0.process-run.v1"
        name = $Name
        pid = $process.Id
        started_at_utc = $startedAt.ToString("o")
        ended_at_utc = $endedAt.ToString("o")
        timeout_seconds = $HardTimeoutSeconds
        timed_out = $timedOut
        exit_code = $exitCode
        stdout_log = $stdoutPath
        stderr_log = $stderrPath
    }
    $metadataPath = Join-Path $LogDirectory "$Name-$stamp.run.json"
    [System.IO.File]::WriteAllText(
        $metadataPath,
        ($metadata | ConvertTo-Json -Depth 4),
        (New-Object System.Text.UTF8Encoding($false)))

    if (Test-Path -LiteralPath $stdoutPath) {
        Get-Content -LiteralPath $stdoutPath -Tail 80 | ForEach-Object { Write-Host $_ }
    }
    if (Test-Path -LiteralPath $stderrPath) {
        Get-Content -LiteralPath $stderrPath -Tail 80 | ForEach-Object { Write-Host $_ }
    }
    if ($timedOut) {
        throw "$Name exceeded the $HardTimeoutSeconds-second hard timeout; only PID $($process.Id) and its process tree were terminated."
    }
    if ($exitCode -ne 0) {
        throw "$Name failed with exit code $exitCode. Metadata: $metadataPath"
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        StdoutPath = $stdoutPath
        StderrPath = $stderrPath
        MetadataPath = $metadataPath
    }
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $PSScriptRoot ".."
}
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$sourcePath = Join-Path $resolvedProject "Tools\PopulationBenchmark\PopulationBenchmark.cs"
$newtonsoftCandidates = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Packages\Debugger\Visualizers\Newtonsoft.Json\net4.5\Newtonsoft.Json.dll",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\PrivateAssemblies\Newtonsoft.Json.13.0.3.0\Newtonsoft.Json.dll"
)
$newtonsoftPath = $newtonsoftCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
$workingRoot = Join-Path $resolvedProject "tmp\m15-p0"
$binaryRoot = Join-Path $workingRoot "bin"
$logRoot = Join-Path $workingRoot "logs"
New-Item -ItemType Directory -Path $binaryRoot -Force | Out-Null
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null

if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Benchmark source is missing: $sourcePath"
}
if (-not $newtonsoftPath) {
    throw "A .NET Framework Newtonsoft.Json assembly was not found in the supported Visual Studio locations."
}

$compilerCandidates = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
)
$compilerPath = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compilerPath) {
    throw "A supported Visual Studio Roslyn compiler was not found."
}

$executablePath = Join-Path $binaryRoot "PopulationBenchmark.exe"
Copy-Item -LiteralPath $newtonsoftPath -Destination (Join-Path $binaryRoot "Newtonsoft.Json.dll") -Force

$compileArguments = @(
    "/nologo",
    "/target:exe",
    "/langversion:latest",
    "/optimize+",
    "/out:$executablePath",
    "/reference:$newtonsoftPath",
    $sourcePath
)
Invoke-BoundedProcess `
    -Name "compile" `
    -FilePath $compilerPath `
    -ArgumentList $compileArguments `
    -WorkingDirectory $resolvedProject `
    -LogDirectory $logRoot `
    -HardTimeoutSeconds $TimeoutSeconds | Out-Null

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $resultStamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputPath = Join-Path $workingRoot "m15-p0-10k-$resultStamp.json"
}
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
$benchmarkArguments = @(
    "--project-root", $resolvedProject,
    "--count", $PersonCount.ToString([System.Globalization.CultureInfo]::InvariantCulture),
    "--seed", $Seed.ToString([System.Globalization.CultureInfo]::InvariantCulture),
    "--output", $resolvedOutput
)
$run = Invoke-BoundedProcess `
    -Name "benchmark-$PersonCount" `
    -FilePath $executablePath `
    -ArgumentList $benchmarkArguments `
    -WorkingDirectory $resolvedProject `
    -LogDirectory $logRoot `
    -HardTimeoutSeconds $TimeoutSeconds

if (-not (Test-Path -LiteralPath $resolvedOutput -PathType Leaf)) {
    throw "Benchmark exited without a result file: $resolvedOutput"
}
$result = Get-Content -Raw -LiteralPath $resolvedOutput | ConvertFrom-Json
if ($result.status -cne "passed" -or -not $result.determinism.passed) {
    throw "Benchmark result did not contain a passing deterministic summary: $resolvedOutput"
}

Write-Host "Benchmark metadata: $($run.MetadataPath)"
Write-Host "Benchmark result: $resolvedOutput"
Write-Host "RESULT m15-p0=passed people=$PersonCount deterministic=true"
