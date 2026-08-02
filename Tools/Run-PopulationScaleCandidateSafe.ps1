[CmdletBinding()]
param(
    [string]$ProjectPath,
    [Parameter(Mandatory = $true)][ValidateSet("sqlite", "binary", "hybrid")][string]$Backend,
    [Parameter(Mandatory = $true)][ValidateSet(100000, 1000000)][int]$PersonCount,
    [long]$Seed = 14000015,
    [ValidateSet("M15-P3", "M15-P4")][string]$Stage = "M15-P3",
    [ValidateRange(30, 300)][int]$TimeoutSeconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$processPath = $env:Path
[Environment]::SetEnvironmentVariable("PATH", $null, "Process")
[Environment]::SetEnvironmentVariable("Path", $processPath, "Process")

function Stop-OwnedProcessTree {
    param([Parameter(Mandatory = $true)][int]$ProcessId)
    $taskkillPath = "C:\Windows\System32\taskkill.exe"
    if (Test-Path -LiteralPath $taskkillPath) {
        try { & $taskkillPath /PID $ProcessId /T /F 2>$null | Out-Null } catch { }
    }
    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Quote-Arguments {
    param([string[]]$Values)
    return @($Values | ForEach-Object { if ($_ -match "\s") { '"' + ($_ -replace '"', '\"') + '"' } else { $_ } })
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = Join-Path $PSScriptRoot ".." }
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$stageFolder = $Stage.ToLowerInvariant()
$root = Join-Path $resolvedProject ("tmp\" + $stageFolder)
$bin = Join-Path $root "bin"
$logs = Join-Path $root "logs"
$results = Join-Path $root "results"
New-Item -ItemType Directory -Path $bin -Force | Out-Null
New-Item -ItemType Directory -Path $logs -Force | Out-Null
New-Item -ItemType Directory -Path $results -Force | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"

$compilerCandidates = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
$newtonsoftCandidates = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Packages\Debugger\Visualizers\Newtonsoft.Json\net4.5\Newtonsoft.Json.dll",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\PrivateAssemblies\Newtonsoft.Json.13.0.3.0\Newtonsoft.Json.dll"
)
$newtonsoft = $newtonsoftCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
$sqlite = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Data\MonoBleedingEdge\lib\mono\4.5\Mono.Data.Sqlite.dll"
if (-not $compiler) { throw "A supported Visual Studio Roslyn compiler was not found." }
if (-not $newtonsoft) { throw "A supported Newtonsoft.Json assembly was not found." }
if (-not (Test-Path -LiteralPath $sqlite -PathType Leaf)) { throw "Mono.Data.Sqlite was not found: $sqlite" }
$sources = @(
    (Join-Path $resolvedProject "Tools\PopulationBenchmark\PopulationBenchmark.cs"),
    (Join-Path $resolvedProject "Tools\PopulationBenchmark\BackendStores.cs"),
    (Join-Path $resolvedProject "Tools\PopulationBenchmark\PopulationScheduling.cs")
)
$exe = Join-Path $bin "PopulationScaleCandidate.exe"
Copy-Item -LiteralPath $newtonsoft -Destination (Join-Path $bin "Newtonsoft.Json.dll") -Force
Copy-Item -LiteralPath $sqlite -Destination (Join-Path $bin "Mono.Data.Sqlite.dll") -Force
$compileOut = Join-Path $logs "compile-$stamp.out.log"
$compileErr = Join-Path $logs "compile-$stamp.err.log"
$compileExitPath = Join-Path $logs "compile-$stamp.exit.txt"
$compileArgs = @(
    "/nologo", "/target:exe", "/langversion:latest", "/optimize+",
    "/main:Mandate.Tools.PopulationBenchmark.PopulationSchedulingProgram",
    "/out:$exe", "/reference:$newtonsoft", "/reference:$sqlite", "/reference:System.Data.dll"
) + $sources
$compileRunner = Join-Path $PSScriptRoot "PopulationBenchmark\Run-Child.ps1"
$compileRunnerArgs = @(
    "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $compileRunner,
    "-ToolPath", $compiler, "-WorkingDirectory", $resolvedProject, "-ExitCodePath", $compileExitPath
) + $compileArgs
$compileProcess = Start-Process -FilePath "powershell.exe" -ArgumentList (Quote-Arguments $compileRunnerArgs) -WorkingDirectory $resolvedProject `
    -RedirectStandardOutput $compileOut -RedirectStandardError $compileErr -PassThru -WindowStyle Hidden
Write-Host "compile PID: $($compileProcess.Id)"
$compileDeadline = (Get-Date).AddSeconds($TimeoutSeconds)
while (-not $compileProcess.HasExited -and (Get-Date) -lt $compileDeadline) { Start-Sleep -Seconds 2; $compileProcess.Refresh() }
if (-not $compileProcess.HasExited) { Stop-OwnedProcessTree -ProcessId $compileProcess.Id; throw "P3 compilation exceeded $TimeoutSeconds seconds." }
$compileProcess.WaitForExit()
if (-not (Test-Path -LiteralPath $compileExitPath)) { throw "P3 compilation exited without an exit-code file." }
$compileExitCode = [int](Get-Content -Raw -LiteralPath $compileExitPath)
if ($compileExitCode -ne 0) { Get-Content -LiteralPath $compileErr -Tail 100; throw "P3 compilation failed with exit code $compileExitCode." }

$candidateOut = Join-Path $logs "$Backend-$PersonCount-$stamp.out.log"
$candidateErr = Join-Path $logs "$Backend-$PersonCount-$stamp.err.log"
$exitPath = Join-Path $logs "$Backend-$PersonCount-$stamp.exit.txt"
$metricsPath = Join-Path $logs "$Backend-$PersonCount-$stamp.child.json"
$progressPath = Join-Path $logs "$Backend-$PersonCount-$stamp.progress.json"
$metadataPath = Join-Path $logs "$Backend-$PersonCount-$stamp.run.json"
$candidateResult = Join-Path $results "$Backend-$PersonCount-$stamp.candidate.json"
$envelopePath = Join-Path $results "$Backend-$PersonCount-$stamp.envelope.json"
$workspace = Join-Path $root "work-$Backend-$PersonCount-$stamp"
$runner = Join-Path $PSScriptRoot "PopulationBenchmark\Run-ChildWithPeak.ps1"
$toolArgs = @(
    "--project-root", $resolvedProject, "--count", $PersonCount.ToString([System.Globalization.CultureInfo]::InvariantCulture),
    "--seed", $Seed.ToString([System.Globalization.CultureInfo]::InvariantCulture), "--workspace", $workspace,
    "--output", $candidateResult, "--backend", $Backend, "--stage", $Stage, "--progress", $progressPath
)
$runnerArgs = @(
    "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $runner,
    "-ToolPath", $exe, "-WorkingDirectory", $resolvedProject, "-ExitCodePath", $exitPath, "-MetricsPath", $metricsPath
) + $toolArgs
$started = (Get-Date).ToUniversalTime()
$process = Start-Process -FilePath "powershell.exe" -ArgumentList (Quote-Arguments $runnerArgs) -WorkingDirectory $resolvedProject `
    -RedirectStandardOutput $candidateOut -RedirectStandardError $candidateErr -PassThru -WindowStyle Hidden
Write-Host "$Backend-$PersonCount PID: $($process.Id)"
Write-Host "stdout: $candidateOut"
Write-Host "stderr: $candidateErr"
$candidateTimeoutSeconds = [Math]::Min($TimeoutSeconds, 285)
$deadline = (Get-Date).AddSeconds($candidateTimeoutSeconds)
while (-not $process.HasExited -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2; $process.Refresh() }
$timedOut = -not $process.HasExited
if ($timedOut) {
    if (Test-Path -LiteralPath $metricsPath) {
        try {
            $partialMetrics = Get-Content -Raw -LiteralPath $metricsPath | ConvertFrom-Json
            if ($null -ne $partialMetrics.child_pid) { Stop-Process -Id ([int]$partialMetrics.child_pid) -Force -ErrorAction SilentlyContinue }
        }
        catch { }
    }
    Stop-OwnedProcessTree -ProcessId $process.Id
}
else { $process.WaitForExit() }
$ended = (Get-Date).ToUniversalTime()
$exitCode = if ($timedOut) { -1 } elseif (Test-Path -LiteralPath $exitPath) { [int](Get-Content -Raw -LiteralPath $exitPath) } else { $process.ExitCode }
$childMetrics = if (Test-Path -LiteralPath $metricsPath) { Get-Content -Raw -LiteralPath $metricsPath | ConvertFrom-Json } else { $null }
$candidatePassed = $false
if (Test-Path -LiteralPath $candidateResult -PathType Leaf) {
    try { $candidatePassed = (Get-Content -Raw -LiteralPath $candidateResult | ConvertFrom-Json).status -ceq "passed" } catch { $candidatePassed = $false }
}
if (-not $timedOut -and -not $candidatePassed -and $exitCode -eq 0) { $exitCode = 1 }
$status = if ($timedOut) { "timed_out" } elseif ($exitCode -eq 0 -and $candidatePassed) { "passed" } else { "failed" }
$lastPhase = $null
if (Test-Path -LiteralPath $progressPath -PathType Leaf) {
    try { $lastPhase = (Get-Content -Raw -LiteralPath $progressPath | ConvertFrom-Json).phase } catch { $lastPhase = $null }
}
$envelope = [ordered]@{
    schema_version = ($stageFolder + ".envelope.v1"); stage = $Stage; status = $status; last_observed_phase = $lastPhase
    backend = $Backend; person_count = $PersonCount; master_seed = $Seed; wrapper_pid = $process.Id
    child_pid = if ($null -eq $childMetrics) { $null } else { $childMetrics.child_pid }
    started_at_utc = $started.ToString("o"); ended_at_utc = $ended.ToString("o")
    timeout_seconds = $candidateTimeoutSeconds; timed_out = $timedOut; exit_code = $exitCode
    peak_working_set_bytes = if ($null -eq $childMetrics) { $null } else { $childMetrics.peak_working_set_bytes }
    candidate_result = $candidateResult; stdout_log = $candidateOut; stderr_log = $candidateErr; progress_file = $progressPath; run_metadata = $metadataPath
}
[System.IO.File]::WriteAllText($envelopePath, ($envelope | ConvertTo-Json -Depth 5), (New-Object System.Text.UTF8Encoding($false)))
[System.IO.File]::WriteAllText($metadataPath, ($envelope | ConvertTo-Json -Depth 5), (New-Object System.Text.UTF8Encoding($false)))
if (Test-Path -LiteralPath $candidateOut) { Get-Content -LiteralPath $candidateOut -Tail 80 }
if (Test-Path -LiteralPath $candidateErr) { Get-Content -LiteralPath $candidateErr -Tail 80 }
Write-Host "Envelope: $envelopePath"
Write-Host "RESULT $stageFolder=$status backend=$Backend people=$PersonCount phase=$lastPhase timeout=$timedOut exit=$exitCode"
if ($status -ne "passed") { throw "$Stage candidate did not pass: status=$status envelope=$envelopePath" }
