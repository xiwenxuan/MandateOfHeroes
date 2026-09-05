[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$RunId,
    [ValidateRange(1, 32)]
    [int]$GroupCount = 12,
    [ValidateRange(1, 32)]
    [int]$GroupIndex = 1,
    [ValidateRange(30, 300)]
    [int]$TimeoutSeconds = 240,
    [ValidateRange(300, 900)]
    [int]$SlowTestTimeoutSeconds = 900,
    [switch]$PrepareOnly,
    [switch]$AggregateOnly,
    [string]$ProjectPath = ""
)

$ErrorActionPreference = "Stop"

# These tests intentionally execute multi-year deterministic world simulation.
# Keep this list exact and small: every other core test retains the ordinary
# timeout even when the slow-test allowance is raised.
$slowDeterminismTests = @(
    "FoodRuntime_FormalWorldIsDeterministicForOneYear",
    "IntegratedOneYearStabilityTests_FormalWorldHasNoEconomicInvariantFailure",
    "Simulation_SameSeedAndDurationProducesSameSnapshot",
    "Simulation_SaveResumeMatchesContinuousRun",
    "LuoyangLiving_365DayCropAndConservationRemainStable",
    "LuoyangT4_OneSevenThirtyOneYearThreeYearSixYearRemainValid",
    "OuterAgricultureLongRunTests_AllRecordsRunForOneWorldYearWithoutDuplicateHarvest",
    # Full 700k-person, 365-day integrated economy simulation. It was measured
    # above the ordinary 300-second gate and is therefore isolated exactly like
    # the existing long deterministic simulations.
    "Luoyang700kOneYearEconomyTests_AgricultureMarketFreightConsumptionAndConservationContinue"
)

# Windows PowerShell Start-Process fails when the host exposes both Path and
# PATH. Normalize only this runner process; do not modify user configuration.
$pathCandidates = @(
    [Environment]::GetEnvironmentVariable("Path", "Process"),
    [Environment]::GetEnvironmentVariable("PATH", "Process")
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
$processPath = $pathCandidates |
    Sort-Object Length -Descending |
    Select-Object -First 1
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
        [string[]]$ArgumentList = @(),
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [Parameter(Mandatory = $true)][string]$LogDirectory,
        [Parameter(Mandatory = $true)][int]$HardTimeoutSeconds
    )

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
    $stdoutPath = Join-Path $LogDirectory "$Name-$stamp.out.log"
    $stderrPath = Join-Path $LogDirectory "$Name-$stamp.err.log"
    $exitCodePath = Join-Path $LogDirectory "$Name-$stamp.exit.txt"
    $childRunnerPath = Join-Path $PSScriptRoot `
        "..\.codex\skills\mandate-unity-development\scripts\run-child.ps1"
    if (-not (Test-Path -LiteralPath $childRunnerPath)) {
        throw "Bounded child runner not found: $childRunnerPath"
    }

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
    if (-not $process.HasExited) {
        Stop-OwnedProcessTree -ProcessId $process.Id
        Write-Host "$Name exceeded $HardTimeoutSeconds seconds." `
            -ForegroundColor Red
        if (Test-Path -LiteralPath $stdoutPath) {
            Get-Content -LiteralPath $stdoutPath -Tail 80
        }
        if (Test-Path -LiteralPath $stderrPath) {
            Get-Content -LiteralPath $stderrPath -Tail 80
        }
        throw "$Name timed out."
    }

    $process.WaitForExit()
    if (-not (Test-Path -LiteralPath $exitCodePath)) {
        throw "$Name exited without an exit-code result file."
    }
    $exitCode = [int](Get-Content -Raw -LiteralPath $exitCodePath)
    if ($exitCode -ne 0) {
        if (Test-Path -LiteralPath $stdoutPath) {
            Get-Content -LiteralPath $stdoutPath -Tail 80
        }
        if (Test-Path -LiteralPath $stderrPath) {
            Get-Content -LiteralPath $stderrPath -Tail 80
        }
        throw "$Name failed with exit code $exitCode."
    }

    return [ordered]@{
        ExitCode = $exitCode
        Stdout = $stdoutPath
        Stderr = $stderrPath
    }
}

function Get-SourceFingerprint {
    param([Parameter(Mandatory = $true)][string]$Root)

    $files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    foreach ($directory in @(
        "Assets\Scripts",
        "Assets\Tests\EditMode"
    )) {
        $absolute = Join-Path $Root $directory
        if (Test-Path -LiteralPath $absolute) {
            Get-ChildItem -LiteralPath $absolute -Recurse -File |
                Where-Object {
                    $_.Extension -eq ".cs" -or $_.Extension -eq ".asmdef"
                } |
                ForEach-Object { $files.Add($_) }
        }
    }
    $files.Add((Get-Item -LiteralPath (
        Join-Path $Root "Tools\CoreTestRunner.cs")))

    $facts = @($files | Sort-Object FullName | ForEach-Object {
        [ordered]@{
            path = $_.FullName.Substring($Root.Length + 1).Replace('\', '/')
            sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash
        }
    })
    $fingerprintText = ($facts | ForEach-Object {
        "$($_.path):$($_.sha256)"
    }) -join "`n"
    $bytes = [Text.Encoding]::UTF8.GetBytes($fingerprintText)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $fingerprint = ([BitConverter]::ToString(
            $sha.ComputeHash($bytes))).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }

    return [ordered]@{
        Fingerprint = $fingerprint
        Facts = $facts
    }
}

function Get-BinaryFacts {
    param(
        [Parameter(Mandatory = $true)][string]$BinaryDirectory,
        [Parameter(Mandatory = $true)][string]$CoreRunnerPath
    )

    $paths = @(
        $CoreRunnerPath,
        (Join-Path $BinaryDirectory "Mandate.Domain.dll"),
        (Join-Path $BinaryDirectory "Mandate.Persistence.dll"),
        (Join-Path $BinaryDirectory "Mandate.Simulation.dll"),
        (Join-Path $BinaryDirectory "Mandate.Domain.Tests.dll")
    )
    return @($paths | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) {
            throw "Required core-test binary not found: $_"
        }
        [ordered]@{
            path = [IO.Path]::GetFileName($_)
            sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $_).Hash
        }
    })
}

function Set-MSBuildSdkFallback {
    param([Parameter(Mandatory = $true)][string]$MSBuildPath)

    $currentSdkPath = [Environment]::GetEnvironmentVariable(
        "MSBuildSDKsPath", [EnvironmentVariableTarget]::Process)
    if (-not [string]::IsNullOrWhiteSpace($currentSdkPath) -and
        (Test-Path -LiteralPath (Join-Path $currentSdkPath `
            "Microsoft.NET.Sdk\Sdk\Sdk.props"))) {
        return $currentSdkPath
    }

    $sdkPath = @(
        "C:\Program Files\dotnet\sdk",
        "C:\Program Files (x86)\dotnet\sdk"
    ) |
        Where-Object { Test-Path -LiteralPath $_ } |
        ForEach-Object { Get-ChildItem -LiteralPath $_ -Directory } |
        Where-Object {
            Test-Path -LiteralPath (Join-Path $_.FullName `
                "Sdks\Microsoft.NET.Sdk\Sdk\Sdk.props")
        } |
        Sort-Object {
            $parsedVersion = [version]"0.0"
            [version]::TryParse($_.Name, [ref]$parsedVersion) | Out-Null
            $parsedVersion
        } -Descending |
        Select-Object -First 1 -ExpandProperty FullName
    if (-not $sdkPath) {
        throw "$MSBuildPath requires Microsoft.NET.Sdk, but no complete dotnet SDK installation was found."
    }

    $resolvedSdkPath = Join-Path $sdkPath "Sdks"
    [Environment]::SetEnvironmentVariable(
        "MSBuildSDKsPath", $resolvedSdkPath,
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "MSBuildEnableWorkloadResolver", "false",
        [EnvironmentVariableTarget]::Process)
    return $resolvedSdkPath
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $PSScriptRoot
}
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$runRoot = Join-Path $resolvedProject "tmp\core-test-groups\$RunId"
$manifestPath = Join-Path $runRoot "manifest.json"
$binaryDirectory = Join-Path $resolvedProject "Temp\bin\Debug"
$coreRunnerPath = Join-Path $binaryDirectory "CoreTestRunner.exe"
$projectVersionPath = Join-Path $resolvedProject `
    "ProjectSettings\ProjectVersion.txt"
$versionMatch = Select-String -LiteralPath $projectVersionPath `
    -Pattern '^m_EditorVersion:\s*(\S+)' | Select-Object -First 1
if ($null -eq $versionMatch) {
    throw "Unity editor version is absent from ProjectVersion.txt."
}
$unityEditorVersion = $versionMatch.Matches[0].Groups[1].Value
$coreRuntimePath =
    "C:\Program Files\Unity\Hub\Editor\$unityEditorVersion\Editor\Data\MonoBleedingEdge\bin\mono.exe"
if (-not (Test-Path -LiteralPath $coreRuntimePath)) {
    throw "Unity Mono runtime not found: $coreRuntimePath"
}
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null

if ($GroupIndex -gt $GroupCount) {
    throw "GroupIndex $GroupIndex exceeds GroupCount $GroupCount."
}
if ($PrepareOnly -and $AggregateOnly) {
    throw "PrepareOnly and AggregateOnly cannot be combined."
}

$source = Get-SourceFingerprint -Root $resolvedProject

if ($PrepareOnly) {
    $solutionPath = Join-Path $resolvedProject "MandateOfHeroes.sln"
    $coreRunnerSource = Join-Path $resolvedProject "Tools\CoreTestRunner.cs"
    $msbuildCandidates = @(
        "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    )
    $msbuildPath = $msbuildCandidates |
        Where-Object { Test-Path -LiteralPath $_ } |
        Select-Object -First 1
    if (-not $msbuildPath) {
        throw "MSBuild.exe was not found in the supported locations."
    }
    Set-MSBuildSdkFallback -MSBuildPath $msbuildPath | Out-Null

    Invoke-BoundedProcess `
        -Name "compile" `
        -FilePath $msbuildPath `
        -ArgumentList @(
            $solutionPath,
            "/restore",
            "/t:Build",
            "/p:Configuration=Debug",
            "/nologo",
            "/verbosity:minimal"
        ) `
        -WorkingDirectory $resolvedProject `
        -LogDirectory $runRoot `
        -HardTimeoutSeconds $TimeoutSeconds | Out-Null

    $cscPath = Join-Path (Split-Path -Parent $msbuildPath) "Roslyn\csc.exe"
    if (-not (Test-Path -LiteralPath $cscPath)) {
        throw "C# compiler not found: $cscPath"
    }
    Invoke-BoundedProcess `
        -Name "core-runner-compile" `
        -FilePath $cscPath `
        -ArgumentList @(
            "/nologo",
            "/target:exe",
            "/out:$coreRunnerPath",
            $coreRunnerSource
        ) `
        -WorkingDirectory $resolvedProject `
        -LogDirectory $runRoot `
        -HardTimeoutSeconds $TimeoutSeconds | Out-Null

    $nunitPath = Join-Path $resolvedProject (
        "Library\PackageCache\com.unity.ext.nunit@1.0.6\" +
        "net35\unity-custom\nunit.framework.dll")
    if (-not (Test-Path -LiteralPath $nunitPath)) {
        throw "Unity NUnit framework not found: $nunitPath"
    }
    Copy-Item -LiteralPath $nunitPath `
        -Destination (Join-Path $binaryDirectory "nunit.framework.dll") -Force

    $listResult = Invoke-BoundedProcess `
        -Name "core-test-list" `
        -FilePath $coreRuntimePath `
        -ArgumentList @($coreRunnerPath, $resolvedProject,
            $binaryDirectory, "--list") `
        -WorkingDirectory $resolvedProject `
        -LogDirectory $runRoot `
        -HardTimeoutSeconds $TimeoutSeconds
    $tests = @(Get-Content -LiteralPath $listResult.Stdout |
        Where-Object { $_ -match '^TEST (.+)$' } |
        ForEach-Object { $Matches[1] } |
        Sort-Object -Unique)
    $listSummary = Select-String -LiteralPath $listResult.Stdout `
        -Pattern '^RESULT listed=(\d+)$'
    if ($null -eq $listSummary -or $tests.Count -eq 0) {
        throw "Core-test discovery did not produce a valid non-empty result."
    }
    if ([int]$listSummary.Matches[0].Groups[1].Value -ne $tests.Count) {
        throw "Core-test discovery contains duplicate or missing names."
    }

    $groups = @{}
    for ($group = 1; $group -le $GroupCount; $group++) {
        $groups[$group] = New-Object 'System.Collections.Generic.List[string]'
    }
    for ($index = 0; $index -lt $tests.Count; $index++) {
        $groups[($index % $GroupCount) + 1].Add($tests[$index])
    }

    $manifest = [ordered]@{
        schemaVersion = 1
        runId = $RunId
        generatedAt = (Get-Date).ToString('o')
        sourceFingerprint = $source.Fingerprint
        groupCount = $GroupCount
        totalExpected = $tests.Count
        sourceFiles = $source.Facts
        binaries = Get-BinaryFacts `
            -BinaryDirectory $binaryDirectory `
            -CoreRunnerPath $coreRunnerPath
        groups = @(
            for ($group = 1; $group -le $GroupCount; $group++) {
                [ordered]@{
                    index = $group
                    expectedCount = $groups[$group].Count
                    tests = @($groups[$group])
                }
            }
        )
    }
    $manifest | ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath $manifestPath -Encoding UTF8
    Write-Host (
        "RESULT core-test-groups status=prepared total=$($tests.Count) " +
        "groups=$GroupCount fingerprint=$($source.Fingerprint)")
    Write-Host "Manifest: $manifestPath"
    exit 0
}

if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Core-test group manifest not found; run -PrepareOnly first: $manifestPath"
}
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
if ($manifest.runId -ne $RunId -or [int]$manifest.groupCount -ne $GroupCount) {
    throw "Core-test manifest identity or group count does not match this invocation."
}
if ($manifest.sourceFingerprint -ne $source.Fingerprint) {
    throw "Core-test sources changed after group preparation."
}
$currentBinaries = Get-BinaryFacts `
    -BinaryDirectory $binaryDirectory `
    -CoreRunnerPath $coreRunnerPath
foreach ($expectedBinary in @($manifest.binaries)) {
    $actualBinary = $currentBinaries |
        Where-Object { $_.path -eq $expectedBinary.path } |
        Select-Object -First 1
    if ($null -eq $actualBinary -or
        $actualBinary.sha256 -ne $expectedBinary.sha256) {
        throw "Core-test binary changed after preparation: $($expectedBinary.path)"
    }
}

if (-not $AggregateOnly) {
    $groupDefinition = @($manifest.groups) |
        Where-Object { [int]$_.index -eq $GroupIndex } |
        Select-Object -First 1
    if ($null -eq $groupDefinition) {
        throw "Core-test group $GroupIndex is absent from the manifest."
    }
    $expected = @($groupDefinition.tests)
    $groupDirectory = Join-Path $runRoot "group-$GroupIndex"
    New-Item -ItemType Directory -Path $groupDirectory -Force | Out-Null
    $metadataPath = Join-Path $groupDirectory "expected.json"
    [ordered]@{
        runId = $RunId
        groupIndex = $GroupIndex
        sourceFingerprint = $source.Fingerprint
        createdAt = (Get-Date).ToString('o')
        expectedTests = $expected
    } | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host (
        "Core-test group $GroupIndex/${GroupCount}: $($expected.Count) tests")
    # Keep every external runner below the hard timeout even when the complete
    # suite contains a few intentionally expensive simulation tests.
    $chunkSize = 1
    $actualItems = New-Object 'System.Collections.Generic.List[string]'
    $failureItems = New-Object 'System.Collections.Generic.List[string]'
    $executionLogs = New-Object 'System.Collections.Generic.List[object]'
    for ($offset = 0; $offset -lt $expected.Count; $offset += $chunkSize) {
        $last = [Math]::Min($offset + $chunkSize - 1,
            $expected.Count - 1)
        $chunk = @($expected[$offset..$last])
        $chunkIndex = [int]($offset / $chunkSize) + 1
        $isSlowDeterminismTest = $chunk.Count -eq 1 -and
            $chunk[0] -in $slowDeterminismTests
        $testClass = if ($isSlowDeterminismTest) {
            "slow-determinism"
        }
        else {
            "regular"
        }
        $hardTimeout = if ($isSlowDeterminismTest) {
            $SlowTestTimeoutSeconds
        }
        else {
            $TimeoutSeconds
        }
        Write-Host (
            "Core-test classification group=$GroupIndex chunk=$chunkIndex " +
            "class=$testClass timeout=$hardTimeout test=$($chunk -join ',')")
        $filter = "exact:" + ($chunk -join ';')
        $execution = Invoke-BoundedProcess `
            -Name "core-tests-group-$GroupIndex-chunk-$chunkIndex" `
            -FilePath $coreRuntimePath `
            -ArgumentList @($coreRunnerPath, $resolvedProject,
                $binaryDirectory, $filter) `
            -WorkingDirectory $resolvedProject `
            -LogDirectory $groupDirectory `
            -HardTimeoutSeconds $hardTimeout
        $chunkActual = @(Get-Content -LiteralPath $execution.Stdout |
            Where-Object { $_ -match '^PASS (.+)$' } |
            ForEach-Object { $Matches[1] } |
            Sort-Object -Unique)
        $chunkFailures = @(Get-Content -LiteralPath $execution.Stdout |
            Where-Object { $_ -match '^FAIL ' })
        $chunkSummary = Select-String -LiteralPath $execution.Stdout `
            -Pattern '^RESULT passed=(\d+) failed=(\d+)$'
        $chunkMissing = @($chunk | Where-Object {
            $_ -notin $chunkActual
        })
        $chunkUnexpected = @($chunkActual | Where-Object {
            $_ -notin $chunk
        })
        if ($null -eq $chunkSummary -or $chunkFailures.Count -gt 0 -or
            $chunkMissing.Count -gt 0 -or $chunkUnexpected.Count -gt 0 -or
            [int]$chunkSummary.Matches[0].Groups[1].Value -ne
                $chunk.Count -or
            [int]$chunkSummary.Matches[0].Groups[2].Value -ne 0) {
            throw (
                "Core-test group $GroupIndex chunk $chunkIndex mismatch: " +
                "expected=$($chunk.Count) actual=$($chunkActual.Count) " +
                "missing=$($chunkMissing.Count) " +
                "unexpected=$($chunkUnexpected.Count) " +
                "failures=$($chunkFailures.Count).")
        }
        foreach ($name in $chunkActual) { $actualItems.Add($name) }
        foreach ($failure in $chunkFailures) {
            $failureItems.Add($failure)
        }
        $executionLogs.Add([ordered]@{
            chunk = $chunkIndex
            classification = $testClass
            timeoutSeconds = $hardTimeout
            tests = $chunk
            stdout = $execution.Stdout
            stderr = $execution.Stderr
        })
    }
    $actual = @($actualItems | Sort-Object -Unique)
    $failures = @($failureItems)
    $missing = @($expected | Where-Object { $_ -notin $actual })
    $unexpected = @($actual | Where-Object { $_ -notin $expected })
    if ($failures.Count -gt 0 -or $missing.Count -gt 0 -or
        $unexpected.Count -gt 0 -or $actual.Count -ne $expected.Count) {
        throw (
            "Core-test group $GroupIndex result mismatch: " +
            "expected=$($expected.Count) actual=$($actual.Count) " +
            "missing=$($missing.Count) unexpected=$($unexpected.Count) " +
            "failures=$($failures.Count).")
    }

    $result = [ordered]@{
        schemaVersion = 1
        runId = $RunId
        groupIndex = $GroupIndex
        sourceFingerprint = $source.Fingerprint
        completedAt = (Get-Date).ToString('o')
        status = "passed"
        passed = $actual.Count
        failed = 0
        executionLogs = $executionLogs
        tests = $actual
    }
    $resultPath = Join-Path $groupDirectory "result.json"
    $result | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath $resultPath -Encoding UTF8
    Write-Host (
        "RESULT core-test-group index=$GroupIndex status=passed " +
        "passed=$($actual.Count) failed=0")
    Write-Host "Group result: $resultPath"
    exit 0
}

$allActual = New-Object 'System.Collections.Generic.List[string]'
$aggregateGroups = New-Object 'System.Collections.Generic.List[object]'
for ($group = 1; $group -le $GroupCount; $group++) {
    $resultPath = Join-Path $runRoot "group-$group\result.json"
    if (-not (Test-Path -LiteralPath $resultPath)) {
        throw "Core-test group $group has no result: $resultPath"
    }
    $result = Get-Content -Raw -LiteralPath $resultPath | ConvertFrom-Json
    if ($result.status -ne "passed" -or
        $result.sourceFingerprint -ne $source.Fingerprint -or
        [int]$result.groupIndex -ne $group) {
        throw "Core-test group $group result does not match this aggregate."
    }
    $expected = @($manifest.groups[$group - 1].tests | Sort-Object -Unique)
    $actual = @($result.tests | Sort-Object -Unique)
    $missing = @($expected | Where-Object { $_ -notin $actual })
    $unexpected = @($actual | Where-Object { $_ -notin $expected })
    if ($missing.Count -gt 0 -or $unexpected.Count -gt 0) {
        throw (
            "Core-test group $group coverage mismatch: " +
            "missing=$($missing.Count) unexpected=$($unexpected.Count).")
    }
    foreach ($name in $actual) {
        $allActual.Add($name)
    }
    $aggregateGroups.Add([ordered]@{
        index = $group
        passed = [int]$result.passed
        failed = [int]$result.failed
        resultPath = $resultPath
    })
}

$duplicates = @($allActual | Group-Object | Where-Object Count -gt 1)
$uniqueActual = @($allActual | Sort-Object -Unique)
if ($duplicates.Count -gt 0 -or
    $uniqueActual.Count -ne [int]$manifest.totalExpected) {
    throw (
        "Core-test aggregate coverage mismatch: " +
        "expected=$($manifest.totalExpected) " +
        "actualUnique=$($uniqueActual.Count) duplicates=$($duplicates.Count).")
}

$aggregate = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    sourceFingerprint = $source.Fingerprint
    status = "passed"
    total = $uniqueActual.Count
    passed = $uniqueActual.Count
    failed = 0
    groups = $aggregateGroups
}
$aggregatePath = Join-Path $runRoot "aggregate.json"
$aggregate | ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath $aggregatePath -Encoding UTF8
Write-Host (
    "RESULT core-test-groups status=passed total=$($uniqueActual.Count) " +
    "passed=$($uniqueActual.Count) failed=0")
Write-Host "Aggregate: $aggregatePath"
exit 0
