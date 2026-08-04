[CmdletBinding()]
param(
    [string]$ProjectPath,
    [ValidateRange(1182, 50000000)][int]$InitialLiving = 1000000,
    [ValidateRange(1, 200)][int]$Years = 50,
    [UInt64]$Seed = 14000024,
    [switch]$SubsistencePressure,
    [switch]$HouseholdMarketRelief,
    [switch]$HouseholdProduction,
    [switch]$PopulationResourceCalibration,
    [switch]$FormalInventoryBridge,
    [switch]$FoodProductProvenance,
    [switch]$FoodEcology,
    [string]$SubsistenceProfilePath,
    [string]$MarketReliefProfilePath,
    [string]$HouseholdProductionProfilePath,
    [string]$PopulationResourceCalibrationProfilePath,
    [string]$FormalInventoryBridgeProfilePath,
    [string]$FoodProductProvenanceProfilePath,
    [string]$FoodEcologyProfilePath,
    [string]$FoodContentExtensionPath,
    [switch]$SelfTest,
    [switch]$Reset,
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
    return @($Values | ForEach-Object {
        if ($_ -match "\s") { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
    })
}

function Wait-OwnedProcess {
    param(
        [Parameter(Mandatory = $true)]$Process,
        [Parameter(Mandatory = $true)][int]$Seconds,
        [Parameter(Mandatory = $true)][string]$Description
    )
    $deadline = (Get-Date).AddSeconds($Seconds)
    while (-not $Process.HasExited -and (Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        $Process.Refresh()
    }
    if (-not $Process.HasExited) {
        Stop-OwnedProcessTree -ProcessId $Process.Id
        throw "$Description exceeded $Seconds seconds."
    }
    $Process.WaitForExit()
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $PSScriptRoot ".."
}
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
if ($FoodEcology) { $FoodProductProvenance = $true }
if ($FoodProductProvenance) { $FormalInventoryBridge = $true }
if ($FormalInventoryBridge) { $PopulationResourceCalibration = $true }
if ($PopulationResourceCalibration) { $HouseholdProduction = $true }
if ($HouseholdProduction) { $HouseholdMarketRelief = $true }
if ($HouseholdMarketRelief) { $SubsistencePressure = $true }
$milestone = if ($FoodEcology) {
    "m24-p7"
} elseif ($FoodProductProvenance) {
    "m24-p6"
} elseif ($FormalInventoryBridge) {
    "m24-p5"
} elseif ($PopulationResourceCalibration) {
    "m24-p4"
} elseif ($HouseholdProduction) {
    "m24-p3"
} elseif ($HouseholdMarketRelief) {
    "m24-p2"
} elseif ($SubsistencePressure) { "m24-p1" } else { "m24-p0" }
$root = Join-Path $resolvedProject ("tmp\" + $milestone)
$bin = Join-Path $root "bin"
$logs = Join-Path $root "logs"
$results = Join-Path $root "results"
$workspace = Join-Path $root ("world-{0}-{1}-{2}{3}" -f `
    $InitialLiving,
    $Years,
    $Seed,
    $(if ($FoodEcology) {
        "-food-ecology"
    } elseif ($FoodProductProvenance) {
        "-food-product-provenance"
    } elseif ($FormalInventoryBridge) {
        "-formal-inventory-bridge"
    } elseif ($PopulationResourceCalibration) {
        "-population-resource-calibration"
    } elseif ($HouseholdProduction) {
        "-household-production"
    } elseif ($HouseholdMarketRelief) {
        "-market-relief"
    } elseif ($SubsistencePressure) { "-subsistence" } else { "" }))
New-Item -ItemType Directory -Path $bin -Force | Out-Null
New-Item -ItemType Directory -Path $logs -Force | Out-Null
New-Item -ItemType Directory -Path $results -Force | Out-Null

if ($Reset -and (Test-Path -LiteralPath $workspace)) {
    $resolvedRoot = [System.IO.Path]::GetFullPath($root).TrimEnd('\') + '\'
    $resolvedWorkspace = [System.IO.Path]::GetFullPath($workspace)
    if (-not $resolvedWorkspace.StartsWith(
            $resolvedRoot,
            [System.StringComparison]::OrdinalIgnoreCase) -or
        -not ([System.IO.Path]::GetFileName($resolvedWorkspace)).StartsWith(
            "world-",
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a workspace outside tmp\$milestone."
    }
    Remove-Item -LiteralPath $resolvedWorkspace -Recurse -Force
}

$compilerCandidates = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
)
$compiler = $compilerCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1
$newtonsoftCandidates = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Packages\Debugger\Visualizers\Newtonsoft.Json\net4.5\Newtonsoft.Json.dll",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\PrivateAssemblies\Newtonsoft.Json.13.0.3.0\Newtonsoft.Json.dll"
)
$newtonsoft = $newtonsoftCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1
if (-not $compiler) { throw "A supported Visual Studio Roslyn compiler was not found." }
if (-not $newtonsoft) { throw "A supported Newtonsoft.Json assembly was not found." }

$stamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$source = Join-Path $resolvedProject "Tools\PopulationBenchmark\PopulationFiftyYearWorld.cs"
$productionSource = Join-Path $resolvedProject "Tools\PopulationBenchmark\HouseholdProduction.cs"
$calibrationSource = Join-Path $resolvedProject "Tools\PopulationBenchmark\PopulationCalibration.cs"
$inventoryBridgeSource = Join-Path $resolvedProject "Tools\PopulationBenchmark\FormalInventoryBridge.cs"
$foodProductProvenanceSource = Join-Path $resolvedProject "Tools\PopulationBenchmark\FoodProductProvenance.cs"
$foodEcologySource = Join-Path $resolvedProject "Tools\PopulationBenchmark\FoodEcology.cs"
$exe = Join-Path $bin "PopulationFiftyYearWorld.exe"
Copy-Item -LiteralPath $newtonsoft -Destination (Join-Path $bin "Newtonsoft.Json.dll") -Force
$compileOut = Join-Path $logs "compile-$stamp.out.log"
$compileErr = Join-Path $logs "compile-$stamp.err.log"
$compileExitPath = Join-Path $logs "compile-$stamp.exit.txt"
$compileRunner = Join-Path $PSScriptRoot "PopulationBenchmark\Run-Child.ps1"
$compileArgs = @(
    "/nologo", "/target:exe", "/langversion:latest", "/optimize+",
    "/main:Mandate.Tools.PopulationFiftyYearWorld.PopulationFiftyYearWorldProgram",
    "/out:$exe", "/reference:$newtonsoft", $source, $productionSource,
    $calibrationSource, $inventoryBridgeSource, $foodProductProvenanceSource,
    $foodEcologySource
)
$compileRunnerArgs = @(
    "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $compileRunner,
    "-ToolPath", $compiler,
    "-WorkingDirectory", $resolvedProject,
    "-ExitCodePath", $compileExitPath
) + $compileArgs
$compileProcess = Start-Process `
    -FilePath "powershell.exe" `
    -ArgumentList (Quote-Arguments $compileRunnerArgs) `
    -WorkingDirectory $resolvedProject `
    -RedirectStandardOutput $compileOut `
    -RedirectStandardError $compileErr `
    -PassThru `
    -WindowStyle Hidden
Write-Host "compile PID: $($compileProcess.Id)"
Wait-OwnedProcess `
    -Process $compileProcess `
    -Seconds ([Math]::Min(60, $TimeoutSeconds)) `
    -Description "$milestone compilation"
$compileExitCode = if (Test-Path -LiteralPath $compileExitPath) {
    [int](Get-Content -Raw -LiteralPath $compileExitPath)
} else { -1 }
if ($compileExitCode -ne 0) {
    if (Test-Path -LiteralPath $compileErr) {
        Get-Content -LiteralPath $compileErr -Tail 120
    }
    throw "$milestone compilation failed with exit code $compileExitCode."
}

$label = if ($SelfTest) { "self-test" } else { "world-$InitialLiving-$Years-years" }
$candidateOut = Join-Path $logs "$label-$stamp.out.log"
$candidateErr = Join-Path $logs "$label-$stamp.err.log"
$exitPath = Join-Path $logs "$label-$stamp.exit.txt"
$metricsPath = Join-Path $logs "$label-$stamp.child.json"
$progressPath = Join-Path $logs "$label-$stamp.progress.json"
$candidateResult = Join-Path $results "$label-$stamp.candidate.json"
$envelopePath = Join-Path $results "$label-$stamp.envelope.json"
$runner = Join-Path $PSScriptRoot "PopulationBenchmark\Run-ChildWithPeak.ps1"
$profile = Join-Path $resolvedProject "Data\PopulationSimulation\demography_profile.han140_baseline_test.v1.json"
$subsistenceProfile = if ([string]::IsNullOrWhiteSpace($SubsistenceProfilePath)) {
    if ($PopulationResourceCalibration) {
        Join-Path $resolvedProject "Data\PopulationSimulation\subsistence_pressure_profile.han140_calibration_candidate3.v1.json"
    } else {
        Join-Path $resolvedProject "Data\PopulationSimulation\subsistence_pressure_profile.han140_baseline_test.v1.json"
    }
} else { (Resolve-Path -LiteralPath $SubsistenceProfilePath).Path }
$marketReliefProfile = if ([string]::IsNullOrWhiteSpace($MarketReliefProfilePath)) {
    Join-Path $resolvedProject "Data\PopulationSimulation\household_market_relief_profile.han140_baseline_test.v1.json"
} else { (Resolve-Path -LiteralPath $MarketReliefProfilePath).Path }
$householdProductionProfile = if ([string]::IsNullOrWhiteSpace($HouseholdProductionProfilePath)) {
    if ($PopulationResourceCalibration) {
        Join-Path $resolvedProject "Data\PopulationSimulation\household_production_profile.han140_calibration_candidate.v1.json"
    } else {
        Join-Path $resolvedProject "Data\PopulationSimulation\household_production_profile.han140_baseline_test.v1.json"
    }
} else { (Resolve-Path -LiteralPath $HouseholdProductionProfilePath).Path }
$calibrationProfile = if ([string]::IsNullOrWhiteSpace($PopulationResourceCalibrationProfilePath)) {
    Join-Path $resolvedProject "Data\PopulationSimulation\population_resource_calibration_profile.han140_candidate.v1.json"
} else { (Resolve-Path -LiteralPath $PopulationResourceCalibrationProfilePath).Path }
$inventoryBridgeProfile = if ([string]::IsNullOrWhiteSpace($FormalInventoryBridgeProfilePath)) {
    Join-Path $resolvedProject "Data\PopulationSimulation\formal_inventory_bridge_profile.v1.json"
} else { (Resolve-Path -LiteralPath $FormalInventoryBridgeProfilePath).Path }
$foodProductProvenanceProfile = if ([string]::IsNullOrWhiteSpace($FoodProductProvenanceProfilePath)) {
    if ($FoodEcology) {
        Join-Path $resolvedProject "Data\PopulationSimulation\food_product_provenance_profile.han_food_ecology.v1.json"
    } else {
        Join-Path $resolvedProject "Data\PopulationSimulation\food_product_provenance_profile.v1.json"
    }
} else { (Resolve-Path -LiteralPath $FoodProductProvenanceProfilePath).Path }
$foodEcologyProfile = if ([string]::IsNullOrWhiteSpace($FoodEcologyProfilePath)) {
    Join-Path $resolvedProject "Data\PopulationSimulation\food_ecology_profile.han140_candidate2.v1.json"
} else { (Resolve-Path -LiteralPath $FoodEcologyProfilePath).Path }
$foodContentExtension = if ([string]::IsNullOrWhiteSpace($FoodContentExtensionPath)) {
    Join-Path $resolvedProject "Data\PopulationSimulation\han_food_production_extension.v1.json"
} else { (Resolve-Path -LiteralPath $FoodContentExtensionPath).Path }
$productionContent = Join-Path $resolvedProject "Assets\Resources\Content\Core\Production\core-production.json"
$m12Input = Join-Path $resolvedProject "Data\HistoricalPopulation\han_140_m12_population_input.json"
$audit = Join-Path $resolvedProject "Data\HistoricalPopulation\han_140_audit_report.json"
$administrativeUnits = Join-Path $resolvedProject "Data\HistoricalPopulation\han_140_administrative_units.csv"

$toolArgs = @(
    "--output", $candidateResult,
    "--profile", $profile,
    "--m12-input", $m12Input,
    "--audit", $audit,
    "--administrative-units", $administrativeUnits,
    "--initial-living", $InitialLiving.ToString([System.Globalization.CultureInfo]::InvariantCulture),
    "--years", $Years.ToString([System.Globalization.CultureInfo]::InvariantCulture),
    "--seed", $Seed.ToString([System.Globalization.CultureInfo]::InvariantCulture)
)
if ($SubsistencePressure) {
    $toolArgs += @("--subsistence-pressure-profile", $subsistenceProfile)
}
if ($HouseholdMarketRelief) {
    $toolArgs += @("--household-market-relief-profile", $marketReliefProfile)
}
if ($HouseholdProduction) {
    $toolArgs += @(
        "--household-production-profile", $householdProductionProfile,
        "--production-content", $productionContent
    )
}
if ($PopulationResourceCalibration) {
    $toolArgs += @(
        "--population-resource-calibration-profile", $calibrationProfile
    )
}
if ($FormalInventoryBridge) {
    $toolArgs += @(
        "--formal-inventory-bridge-profile", $inventoryBridgeProfile
    )
}
if ($FoodProductProvenance) {
    $toolArgs += @(
        "--food-product-provenance-profile", $foodProductProvenanceProfile
    )
}
if ($FoodEcology) {
    $toolArgs += @(
        "--food-ecology-profile", $foodEcologyProfile,
        "--food-content-extension", $foodContentExtension
    )
}
if ($SelfTest) {
    $toolArgs = @("--self-test") + $toolArgs
} else {
    $toolArgs = @(
        "--workspace", $workspace,
        "--progress", $progressPath
    ) + $toolArgs
}

$runnerArgs = @(
    "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $runner,
    "-ToolPath", $exe,
    "-WorkingDirectory", $resolvedProject,
    "-ExitCodePath", $exitPath,
    "-MetricsPath", $metricsPath
) + $toolArgs
$started = (Get-Date).ToUniversalTime()
$process = Start-Process `
    -FilePath "powershell.exe" `
    -ArgumentList (Quote-Arguments $runnerArgs) `
    -WorkingDirectory $resolvedProject `
    -RedirectStandardOutput $candidateOut `
    -RedirectStandardError $candidateErr `
    -PassThru `
    -WindowStyle Hidden
Write-Host "$label PID: $($process.Id)"
Write-Host "stdout: $candidateOut"
Write-Host "stderr: $candidateErr"
$candidateTimeoutSeconds = [Math]::Min($TimeoutSeconds, 285)
$timedOut = $false
try {
    Wait-OwnedProcess `
        -Process $process `
        -Seconds $candidateTimeoutSeconds `
        -Description "$milestone $label"
} catch {
    $timedOut = $true
}
$ended = (Get-Date).ToUniversalTime()
$exitCode = if ($timedOut) {
    -1
} elseif (Test-Path -LiteralPath $exitPath) {
    [int](Get-Content -Raw -LiteralPath $exitPath)
} else { $process.ExitCode }
$childMetrics = if (Test-Path -LiteralPath $metricsPath) {
    Get-Content -Raw -LiteralPath $metricsPath | ConvertFrom-Json
} else { $null }
$candidatePassed = $false
if (Test-Path -LiteralPath $candidateResult -PathType Leaf) {
    try {
        $candidateJson = Get-Content -Raw -LiteralPath $candidateResult |
            ConvertFrom-Json
        $candidatePassed = $candidateJson.status -ceq "passed"
        if ($PopulationResourceCalibration -and -not $SelfTest) {
            $candidatePassed = $candidatePassed -and
                $candidateJson.calibration_passed -ceq $true
        }
    } catch { $candidatePassed = $false }
}
$lastPhase = $null
$lastYear = $null
if (Test-Path -LiteralPath $progressPath -PathType Leaf) {
    try {
        $progress = Get-Content -Raw -LiteralPath $progressPath | ConvertFrom-Json
        $lastPhase = $progress.phase
        $lastYear = $progress.year
    } catch { }
}
$status = if ($timedOut) {
    "timed_out"
} elseif ($exitCode -eq 0 -and $candidatePassed) {
    "passed"
} else { "failed" }
$envelope = [ordered]@{
    schema_version = "$milestone.envelope.v1"
    status = $status
    phase = $lastPhase
    completed_year = $lastYear
    label = $label
    initial_living_population = if ($SelfTest) { 10000 } else { $InitialLiving }
    years = if ($SelfTest) { 10 } else { $Years }
    master_seed = $Seed
    wrapper_pid = $process.Id
    child_pid = if ($null -eq $childMetrics) { $null } else { $childMetrics.child_pid }
    started_at_utc = $started.ToString("o")
    ended_at_utc = $ended.ToString("o")
    timeout_seconds = $candidateTimeoutSeconds
    timed_out = $timedOut
    exit_code = $exitCode
    peak_working_set_bytes = if ($null -eq $childMetrics) {
        $null
    } else { $childMetrics.peak_working_set_bytes }
    candidate_result = $candidateResult
    stdout_log = $candidateOut
    stderr_log = $candidateErr
    progress_file = if ($SelfTest) { $null } else { $progressPath }
    workspace = if ($SelfTest) { $null } else { $workspace }
}
[System.IO.File]::WriteAllText(
    $envelopePath,
    ($envelope | ConvertTo-Json -Depth 5),
    (New-Object System.Text.UTF8Encoding($false)))
if (Test-Path -LiteralPath $candidateOut) {
    Get-Content -LiteralPath $candidateOut -Tail 100
}
if (Test-Path -LiteralPath $candidateErr) {
    Get-Content -LiteralPath $candidateErr -Tail 120
}
Write-Host "Envelope: $envelopePath"
Write-Host "RESULT $milestone=$status label=$label phase=$lastPhase year=$lastYear timeout=$timedOut exit=$exitCode"
if ($status -ne "passed") {
    throw "$milestone run did not pass: status=$status envelope=$envelopePath"
}
