[CmdletBinding()]
param(
    [string]$ProjectPath,
    [ValidateSet("self-test", "build", "day", "month", "year")][string]$Mode = "self-test",
    [ValidateRange(1, 50000000)][long]$ExpectedPeople = 50000000,
    [long]$Seed = 14000015,
    [switch]$ResetIndexes,
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
    if (Test-Path -LiteralPath $taskkillPath) { try { & $taskkillPath /PID $ProcessId /T /F 2>$null | Out-Null } catch { } }
    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Quote-Arguments {
    param([string[]]$Values)
    return @($Values | ForEach-Object { if ($_ -match "\s") { '"' + ($_ -replace '"', '\"') + '"' } else { $_ } })
}

function Wait-OwnedProcess {
    param($Process, [int]$Seconds, [string]$Description)
    $deadline = (Get-Date).AddSeconds($Seconds)
    while (-not $Process.HasExited -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2; $Process.Refresh() }
    if (-not $Process.HasExited) { Stop-OwnedProcessTree -ProcessId $Process.Id; throw "$Description exceeded $Seconds seconds." }
    $Process.WaitForExit()
}

function Remove-OwnedDirectory {
    param([string]$Path, [string]$ExpectedParent, [string]$ExpectedPrefix)
    if (-not (Test-Path -LiteralPath $Path)) { return }
    $parent = [System.IO.Path]::GetFullPath($ExpectedParent).TrimEnd('\') + '\'
    $target = [System.IO.Path]::GetFullPath($Path)
    if (-not $target.StartsWith($parent, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not ([System.IO.Path]::GetFileName($target)).StartsWith($ExpectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a directory outside the M15-P5 full-living workspace."
    }
    Remove-Item -LiteralPath $target -Recurse -Force
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = Join-Path $PSScriptRoot ".." }
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$root = Join-Path $resolvedProject "tmp\m15-p5-full-living"
$bin = Join-Path $root "bin"
$logs = Join-Path $root "logs"
$results = Join-Path $root "results"
$sourceWorkspace = Join-Path $resolvedProject ("tmp\m15-p5-all-hot\all-hot-{0}" -f $Seed)
$indexWorkspace = Join-Path $root ("indexes-{0}-{1}" -f $Seed, $ExpectedPeople)
New-Item -ItemType Directory -Path $bin -Force | Out-Null
New-Item -ItemType Directory -Path $logs -Force | Out-Null
New-Item -ItemType Directory -Path $results -Force | Out-Null

if ($Mode -ne "self-test" -and -not (Test-Path -LiteralPath (Join-Path $sourceWorkspace "manifest.json"))) {
    throw "The required all-hot source workspace does not exist: $sourceWorkspace"
}
if ($ResetIndexes) {
    Remove-OwnedDirectory -Path $indexWorkspace -ExpectedParent $root -ExpectedPrefix "indexes-"
    Remove-OwnedDirectory -Path ($indexWorkspace + ".staging") -ExpectedParent $root -ExpectedPrefix "indexes-"
}

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
if (-not $compiler) { throw "A supported Visual Studio Roslyn compiler was not found." }
if (-not $newtonsoft) { throw "A supported Newtonsoft.Json assembly was not found." }

$stamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$sourceAllHot = Join-Path $resolvedProject "Tools\PopulationBenchmark\PopulationAllHot.cs"
$sourceScheduling = Join-Path $resolvedProject "Tools\PopulationBenchmark\PopulationAllHotScheduling.cs"
$exe = Join-Path $bin "PopulationAllHotScheduling.exe"
Copy-Item -LiteralPath $newtonsoft -Destination (Join-Path $bin "Newtonsoft.Json.dll") -Force
$compileOut = Join-Path $logs "compile-$stamp.out.log"
$compileErr = Join-Path $logs "compile-$stamp.err.log"
$compileExitPath = Join-Path $logs "compile-$stamp.exit.txt"
$compileRunner = Join-Path $PSScriptRoot "PopulationBenchmark\Run-Child.ps1"
$compileArgs = @(
    "/nologo", "/target:exe", "/platform:x64", "/langversion:latest", "/optimize+",
    "/main:Mandate.Tools.PopulationAllHotScheduling.PopulationAllHotSchedulingProgram",
    "/out:$exe", "/reference:$newtonsoft", $sourceAllHot, $sourceScheduling
)
$compileRunnerArgs = @(
    "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $compileRunner,
    "-ToolPath", $compiler, "-WorkingDirectory", $resolvedProject, "-ExitCodePath", $compileExitPath
) + $compileArgs
$compileProcess = Start-Process -FilePath "powershell.exe" -ArgumentList (Quote-Arguments $compileRunnerArgs) `
    -WorkingDirectory $resolvedProject -RedirectStandardOutput $compileOut -RedirectStandardError $compileErr `
    -PassThru -WindowStyle Hidden
Write-Host "compile PID: $($compileProcess.Id)"
Wait-OwnedProcess -Process $compileProcess -Seconds ([Math]::Min(60, $TimeoutSeconds)) -Description "M15-P5 full-living compilation"
$compileExitCode = if (Test-Path -LiteralPath $compileExitPath) { [int](Get-Content -Raw -LiteralPath $compileExitPath) } else { -1 }
if ($compileExitCode -ne 0) {
    if (Test-Path -LiteralPath $compileErr) { Get-Content -LiteralPath $compileErr -Tail 100 }
    throw "M15-P5 full-living compilation failed with exit code $compileExitCode."
}

$label = "$Mode-$ExpectedPeople"
$candidateOut = Join-Path $logs "$label-$stamp.out.log"
$candidateErr = Join-Path $logs "$label-$stamp.err.log"
$exitPath = Join-Path $logs "$label-$stamp.exit.txt"
$metricsPath = Join-Path $logs "$label-$stamp.child.json"
$progressPath = Join-Path $logs "$label-$stamp.progress.json"
$candidateResult = Join-Path $results "$label-$stamp.candidate.json"
$envelopePath = Join-Path $results "$label-$stamp.envelope.json"
$runWorkspace = Join-Path $root ("run-{0}-{1}-{2}" -f $Mode, $ExpectedPeople, $stamp)
$runner = Join-Path $PSScriptRoot "PopulationBenchmark\Run-ChildWithPeak.ps1"
if ($Mode -eq "self-test") {
    $toolArgs = @("--mode", "self-test", "--output", $candidateResult)
}
elseif ($Mode -eq "build") {
    $toolArgs = @(
        "--mode", $Mode, "--source-workspace", $sourceWorkspace, "--index-workspace", $indexWorkspace,
        "--expected-people", $ExpectedPeople.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        "--seed", $Seed.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        "--output", $candidateResult, "--progress", $progressPath
    )
}
else {
    $toolArgs = @(
        "--mode", $Mode, "--source-workspace", $sourceWorkspace, "--index-workspace", $indexWorkspace,
        "--run-workspace", $runWorkspace,
        "--expected-people", $ExpectedPeople.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        "--seed", $Seed.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        "--output", $candidateResult, "--progress", $progressPath
    )
}
$runnerArgs = @(
    "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $runner,
    "-ToolPath", $exe, "-WorkingDirectory", $resolvedProject,
    "-ExitCodePath", $exitPath, "-MetricsPath", $metricsPath
) + $toolArgs
$started = (Get-Date).ToUniversalTime()
$process = Start-Process -FilePath "powershell.exe" -ArgumentList (Quote-Arguments $runnerArgs) `
    -WorkingDirectory $resolvedProject -RedirectStandardOutput $candidateOut -RedirectStandardError $candidateErr `
    -PassThru -WindowStyle Hidden
Write-Host "$label PID: $($process.Id)"
Write-Host "stdout: $candidateOut"
Write-Host "stderr: $candidateErr"
$candidateTimeoutSeconds = [Math]::Min($TimeoutSeconds, 285)
$timedOut = $false
try { Wait-OwnedProcess -Process $process -Seconds $candidateTimeoutSeconds -Description "M15-P5 full-living $label" }
catch { $timedOut = $true }
$ended = (Get-Date).ToUniversalTime()
$exitCode = if ($timedOut) { -1 } elseif (Test-Path -LiteralPath $exitPath) { [int](Get-Content -Raw -LiteralPath $exitPath) } else { $process.ExitCode }
$childMetrics = if (Test-Path -LiteralPath $metricsPath) { Get-Content -Raw -LiteralPath $metricsPath | ConvertFrom-Json } else { $null }
$candidatePassed = $false
if (Test-Path -LiteralPath $candidateResult -PathType Leaf) {
    try { $candidatePassed = (Get-Content -Raw -LiteralPath $candidateResult | ConvertFrom-Json).status -ceq "passed" } catch { $candidatePassed = $false }
}
$lastPhase = $null
if (Test-Path -LiteralPath $progressPath -PathType Leaf) {
    try { $lastPhase = (Get-Content -Raw -LiteralPath $progressPath | ConvertFrom-Json).phase } catch { $lastPhase = $null }
}
$status = if ($timedOut) { "timed_out" } elseif ($exitCode -eq 0 -and $candidatePassed) { "passed" } else { "failed" }
$envelope = [ordered]@{
    schema_version = "m15.p5.full-living.envelope.v1"; status = $status; phase = $lastPhase; label = $label
    mode = $Mode; expected_people = $ExpectedPeople; master_seed = $Seed
    wrapper_pid = $process.Id; child_pid = if ($null -eq $childMetrics) { $null } else { $childMetrics.child_pid }
    started_at_utc = $started.ToString("o"); ended_at_utc = $ended.ToString("o")
    timeout_seconds = $candidateTimeoutSeconds; timed_out = $timedOut; exit_code = $exitCode
    peak_working_set_bytes = if ($null -eq $childMetrics) { $null } else { $childMetrics.peak_working_set_bytes }
    candidate_result = $candidateResult; stdout_log = $candidateOut; stderr_log = $candidateErr
    progress_file = $progressPath; source_workspace = $sourceWorkspace; index_workspace = $indexWorkspace
    run_workspace = if ($Mode -eq "build" -or $Mode -eq "self-test") { $null } else { $runWorkspace }
}
[System.IO.File]::WriteAllText($envelopePath, ($envelope | ConvertTo-Json -Depth 5), (New-Object System.Text.UTF8Encoding($false)))
if (Test-Path -LiteralPath $candidateOut) { Get-Content -LiteralPath $candidateOut -Tail 80 }
if (Test-Path -LiteralPath $candidateErr) { Get-Content -LiteralPath $candidateErr -Tail 80 }
Write-Host "Envelope: $envelopePath"
Write-Host "RESULT m15-p5-full-living=$status label=$label phase=$lastPhase timeout=$timedOut exit=$exitCode"
if ($status -ne "passed") { throw "M15-P5 full-living run did not complete: status=$status envelope=$envelopePath" }
