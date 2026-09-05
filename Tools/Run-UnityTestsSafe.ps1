param(
    [ValidateSet("Auto", "EngineSmoke", "ProjectLoadSmoke", "EditModeTests", "PlayModeTests")]
    [string]$Mode = "Auto",
    [ValidateSet("EditMode", "PlayMode")]
    [string]$TestPlatform = "EditMode",
    [ValidateRange(30, 900)]
    [int]$TimeoutSeconds = 300,
    [ValidateSet("Standard", "SlowDeterminism", "AssetBuildIntegration", "AssetManifestIntegration")]
    [string]$TimeoutClass = "Standard",
    [ValidateRange(10, 120)]
    [int]$StartupTimeoutSeconds = 45,
    [ValidateRange(5, 60)]
    [int]$ResultExitGraceSeconds = 15,
    [string]$TestFilter = "",
    [switch]$UseGraphics,
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = "",
    [string]$UnityPath =
        "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"

# Stable result codes used by the verification wrapper.
$ResultCode = @{
    Passed = 0
    TestFailed = 3
    ProjectLock = 120
    LaunchFailed = 121
    CompilationFailed = 122
    InvalidResult = 123
    TimedOut = 124
    StartupTimedOut = 125
    MissingResult = 126
}

if (-not (Test-Path -LiteralPath $UnityPath)) {
    Write-Error "Unity executable not found: $UnityPath"
    exit $ResultCode.LaunchFailed
}

if ($Mode -eq "Auto") {
    $Mode = if ($TestPlatform -eq "PlayMode") {
        "PlayModeTests"
    }
    else {
        "EditModeTests"
    }
}
if ($Mode -eq "EditModeTests") {
    $TestPlatform = "EditMode"
}
elseif ($Mode -eq "PlayModeTests") {
    $TestPlatform = "PlayMode"
}

$slowDeterminismTests = @(
    "Mandate.Tests.WorldKernelTests.Simulation_SaveResumeMatchesContinuousRun",
    "Mandate.Tests.WorldKernelTests.FoodRuntime_FormalWorldIsDeterministicForOneYear",
    "Mandate.Tests.WorldKernelTests.IntegratedOneYearStabilityTests_FormalWorldHasNoEconomicInvariantFailure",
    "Mandate.Tests.WorldKernelTests.LuoyangLiving_365DayCropAndConservationRemainStable",
    "Mandate.Tests.WorldKernelTests.LuoyangT4_OneSevenThirtyOneYearThreeYearSixYearRemainValid",
    "Mandate.Tests.WorldKernelTests.OuterAgricultureLongRunTests_AllRecordsRunForOneWorldYearWithoutDuplicateHarvest"
)
$assetBuildIntegrationTests = @(
    "Mandate.Tests.EditMode.LuoyangP0NativePrefabArtDeliveryV1Tests.BuildAssets_CreatesFourReplaceableThreeLodPrefabs"
)
$assetManifestIntegrationTests = @(
    "Mandate.Tests.EditMode.LuoyangRemainingFinalAssetV1Tests.SourceManifest_Freezes240FilesAnd38ValidatedFbxSources"
)
$matchedSlowDeterminismTests = @()
$matchedTimeoutClassTests = @()
if ($TimeoutSeconds -gt 300) {
    if ($TimeoutClass -eq "Standard") {
        throw (
            "TimeoutSeconds above 300 requires the explicit " +
            "classified timeout class.")
    }
    if ($Mode -ne "EditModeTests" -or
        [string]::IsNullOrWhiteSpace($TestFilter)) {
        throw (
            "$TimeoutClass is only valid for explicitly filtered " +
            "EditMode tests.")
    }
    $requestedTests = @($TestFilter.Split(';') | ForEach-Object {
        $_.Trim()
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($TimeoutClass -eq "SlowDeterminism") {
        $classifiedTests = $slowDeterminismTests
    }
    elseif ($TimeoutClass -eq "AssetBuildIntegration") {
        if ($TimeoutSeconds -gt 600) {
            throw "AssetBuildIntegration is capped at 600 seconds."
        }
        $classifiedTests = $assetBuildIntegrationTests
    }
    else {
        if ($TimeoutSeconds -gt 600) {
            throw "AssetManifestIntegration is capped at 600 seconds."
        }
        $classifiedTests = $assetManifestIntegrationTests
    }
    $matchedTimeoutClassTests = @($requestedTests | Where-Object {
        $_ -in $classifiedTests
    })
    if ($TimeoutClass -eq "SlowDeterminism") {
        $matchedSlowDeterminismTests = $matchedTimeoutClassTests
    }
    if ($matchedTimeoutClassTests.Count -eq 0) {
        throw (
            "$TimeoutClass requires at least one classified exact test: " +
            ($classifiedTests -join ', '))
    }
}

$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $resolvedProject "tmp\unity-validation"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $resolvedProject $OutputDirectory
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$runName = if ($Mode -match "Tests$") {
    "unity-$TestPlatform-$stamp"
}
else {
    "unity-$Mode-$stamp"
}
$logPath = Join-Path $OutputDirectory "$runName.log"
$stdoutPath = Join-Path $OutputDirectory "$runName.stdout.log"
$stderrPath = Join-Path $OutputDirectory "$runName.stderr.log"
$summaryPath = Join-Path $OutputDirectory "$runName.summary.json"
$resultPath = if ($Mode -match "Tests$") {
    Join-Path $OutputDirectory "$runName.xml"
}
else {
    ""
}

$startedAt = Get-Date
$endedAt = $null
$process = $null
$exitCode = $null
$reason = ""
$testSummary = $null
$environmentPathNormalized = $false
$argumentText = ""
$resultReadyAt = $null
$forcedCleanupAfterResult = $false

function Stop-OwnedProcessTree {
    param([System.Diagnostics.Process]$OwnedProcess)

    if ($null -eq $OwnedProcess) {
        return
    }
    try {
        & "C:\Windows\System32\taskkill.exe" `
            /PID $OwnedProcess.Id `
            /T `
            /F 2>$null | Out-Null
    }
    catch {
        # Continue to the exact owned PID fallback below.
    }
    Stop-Process -Id $OwnedProcess.Id -Force -ErrorAction SilentlyContinue
}

function Get-NonEmptyFileLength {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return 0L
    }
    return (Get-Item -LiteralPath $Path).Length
}

function ConvertTo-ProcessArgument {
    param([string]$Value)

    if ($null -eq $Value) {
        return '""'
    }
    # The controlled arguments used here never end with a backslash. Quoting
    # every value keeps spaces in Unity and project paths intact.
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Write-RunSummary {
    param(
        [string]$RunStatus,
        [string]$RunReason,
        [Nullable[int]]$RunExitCode
    )

    $script:endedAt = Get-Date
    $logInfo = if (Test-Path -LiteralPath $script:logPath) {
        Get-Item -LiteralPath $script:logPath
    }
    else {
        $null
    }
    $summary = [ordered]@{
        schemaVersion = 1
        mode = $script:Mode
        testPlatform = if ($script:Mode -match "Tests$") { $script:TestPlatform } else { $null }
        testFilter = if ([string]::IsNullOrWhiteSpace($script:TestFilter)) { $null } else { $script:TestFilter }
        status = $RunStatus
        reason = $RunReason
        resultCode = $RunExitCode
        unityExitCode = $script:exitCode
        pid = if ($null -eq $script:process) { $null } else { $script:process.Id }
        startedAt = $script:startedAt.ToString("o")
        endedAt = $script:endedAt.ToString("o")
        durationSeconds = [Math]::Round(($script:endedAt - $script:startedAt).TotalSeconds, 3)
        unityPath = $script:UnityPath
        commandArguments = $script:argumentText
        projectPath = if ($script:Mode -eq "EngineSmoke") { $null } else { $script:resolvedProject }
        useGraphics = [bool]$script:UseGraphics
        timeoutSeconds = $script:TimeoutSeconds
        timeoutClass = $script:TimeoutClass
        matchedTimeoutClassTests = @($script:matchedTimeoutClassTests)
        matchedSlowDeterminismTests = @($script:matchedSlowDeterminismTests)
        startupTimeoutSeconds = $script:StartupTimeoutSeconds
        resultExitGraceSeconds = $script:ResultExitGraceSeconds
        forcedCleanupAfterResult = $script:forcedCleanupAfterResult
        environmentPathNormalized = $script:environmentPathNormalized
        logPath = $script:logPath
        logLength = if ($null -eq $logInfo) { 0 } else { $logInfo.Length }
        logLastWriteTime = if ($null -eq $logInfo) { $null } else { $logInfo.LastWriteTime.ToString("o") }
        stdoutPath = $script:stdoutPath
        stderrPath = $script:stderrPath
        resultPath = if ([string]::IsNullOrWhiteSpace($script:resultPath)) { $null } else { $script:resultPath }
        tests = $script:testSummary
    }
    $summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $script:summaryPath -Encoding UTF8

    Write-Host "RESULT mode=$($script:Mode) status=$RunStatus code=$RunExitCode duration=$($summary.durationSeconds)s"
    Write-Host "Log: $($script:logPath)"
    Write-Host "Summary: $($script:summaryPath)"
    if (-not [string]::IsNullOrWhiteSpace($script:resultPath)) {
        Write-Host "Result: $($script:resultPath)"
    }
}

$existingUnity = Get-Process -Name Unity -ErrorAction SilentlyContinue
if ($null -ne $existingUnity) {
    $ids = ($existingUnity | Select-Object -ExpandProperty Id) -join ", "
    $reason = "Unity is already running (PID: $ids). Close the editor before batch tests."
    Write-RunSummary -RunStatus "blocked" -RunReason $reason -RunExitCode $ResultCode.ProjectLock
    Write-Error $reason
    exit $ResultCode.ProjectLock
}

$arguments = @("-accept-apiupdate", "-batchmode")
if (-not $UseGraphics) {
    $arguments += "-nographics"
}

switch ($Mode) {
    "EngineSmoke" {
        $arguments += @("-quit", "-logFile", $logPath)
    }
    "ProjectLoadSmoke" {
        $arguments += @("-projectPath", $resolvedProject, "-quit", "-logFile", $logPath)
    }
    default {
        $arguments += @(
            "-projectPath", $resolvedProject,
            "-runTests",
            "-testPlatform", $TestPlatform,
            "-testResults", $resultPath,
            "-logFile", $logPath
        )
        if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
            $arguments += @("-testFilter", $TestFilter)
        }
    }
}

# Codex can inject both `Path` and `PATH` into the same Windows process. The
# Win32 environment is case-insensitive, but PowerShell/.NET can still expose
# both entries; Start-Process then inherits an ambiguous environment and Unity
# may wait before creating its log or starting the licensing client. Collapse
# only those duplicate case variants inside this runner process. Keep the
# longest value because the Codex PATH is the normal Path plus its sandbox bin.
$processEnvironment = [Environment]::GetEnvironmentVariables(
    [EnvironmentVariableTarget]::Process)
$pathEntries = @(
    foreach ($key in $processEnvironment.Keys) {
        if (([string]$key) -ieq "Path") {
            [ordered]@{
                key = [string]$key
                value = [string]$processEnvironment[$key]
            }
        }
    }
)
$environmentPathNormalized = $pathEntries.Count -gt 1
if ($environmentPathNormalized) {
    $selectedPath = [string](
        $pathEntries |
            Sort-Object { $_.value.Length } -Descending |
            Select-Object -First 1
    ).value

    # SetEnvironmentVariable removes one case variant at a time in this
    # duplicated state, so re-enumerate until none remain before restoring one.
    for ($attempt = 0; $attempt -lt 8; $attempt++) {
        $currentEnvironment = [Environment]::GetEnvironmentVariables(
            [EnvironmentVariableTarget]::Process)
        $currentPathKey = @($currentEnvironment.Keys | Where-Object {
            ([string]$_) -ieq "Path"
        } | Select-Object -First 1)
        if ($currentPathKey.Count -eq 0) {
            break
        }
        [Environment]::SetEnvironmentVariable(
            [string]$currentPathKey[0],
            $null,
            [EnvironmentVariableTarget]::Process)
    }
    [Environment]::SetEnvironmentVariable(
        "Path",
        $selectedPath,
        [EnvironmentVariableTarget]::Process)
}

$argumentText = (($arguments | ForEach-Object {
    ConvertTo-ProcessArgument -Value ([string]$_)
}) -join ' ')
try {
    # Unity 2022.3 China build can finish the test and write XML but remain in
    # shutdown when its native stdout/stderr are redirected to managed pipes.
    # Unity's explicit -logFile is the authoritative output. Keep separate
    # evidence files that explain why the inherited streams are not redirected.
    Set-Content -LiteralPath $stdoutPath -Encoding UTF8 -Value (
        "Standard output was inherited to avoid a Unity native shutdown pipe hang; " +
        "use the Unity logPath from the JSON summary.")
    Set-Content -LiteralPath $stderrPath -Encoding UTF8 -Value (
        "Standard error was inherited to avoid a Unity native shutdown pipe hang; " +
        "use the Unity logPath from the JSON summary.")
    $process = Start-Process `
        -FilePath $UnityPath `
        -ArgumentList $arguments `
        -WorkingDirectory $(if ($Mode -eq "EngineSmoke") {
            Split-Path -Parent $UnityPath
        }
        else {
            $resolvedProject
        }) `
        -PassThru `
        -WindowStyle Hidden
}
catch {
    $reason = "Unity process could not be launched: $($_.Exception.Message)"
    Write-RunSummary -RunStatus "blocked" -RunReason $reason -RunExitCode $ResultCode.LaunchFailed
    Write-Error $reason
    exit $ResultCode.LaunchFailed
}

Write-Host "Unity PID: $($process.Id)"
Write-Host "Mode: $Mode"
Write-Host "Log: $logPath"
if (-not [string]::IsNullOrWhiteSpace($resultPath)) {
    Write-Host "Result: $resultPath"
}

$deadline = $startedAt.AddSeconds($TimeoutSeconds)
$startupDeadline = $startedAt.AddSeconds(
    [Math]::Min($StartupTimeoutSeconds, $TimeoutSeconds)
)

while (-not $process.HasExited -and
       (Get-NonEmptyFileLength -Path $logPath) -eq 0 -and
       (Get-Date) -lt $startupDeadline) {
    Start-Sleep -Seconds 2
    $process.Refresh()
}

if (-not $process.HasExited -and (Get-NonEmptyFileLength -Path $logPath) -eq 0) {
    Stop-OwnedProcessTree -OwnedProcess $process
    $reason = "Unity did not create a non-empty startup log within $StartupTimeoutSeconds seconds."
    Write-RunSummary -RunStatus "blocked" -RunReason $reason -RunExitCode $ResultCode.StartupTimedOut
    Write-Error "$reason PID $($process.Id) was terminated."
    exit $ResultCode.StartupTimedOut
}

while (-not $process.HasExited -and (Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    $process.Refresh()

    if ($Mode -match "Tests$" -and
        (Test-Path -LiteralPath $resultPath) -and
        (Get-Item -LiteralPath $resultPath).Length -gt 0) {
        try {
            [xml]$candidateXml = Get-Content -Raw -LiteralPath $resultPath
            $candidateRun = $candidateXml.'test-run'
            if ($null -ne $candidateRun -and [int]$candidateRun.total -gt 0) {
                if ($null -eq $resultReadyAt) {
                    $resultReadyAt = Get-Date
                    Write-Host (
                        "Unity result XML is complete; allowing " +
                        "$ResultExitGraceSeconds seconds for natural shutdown.")
                }
                elseif (((Get-Date) - $resultReadyAt).TotalSeconds -ge
                        $ResultExitGraceSeconds) {
                    Stop-OwnedProcessTree -OwnedProcess $process
                    $forcedCleanupAfterResult = $true
                    Write-Host (
                        "Unity did not exit after the completed result XML; " +
                        "the owned process tree was terminated after the grace period.")
                    break
                }
            }
        }
        catch {
            # The test runner may still be writing the XML. Retry on the next poll.
        }
    }
}

if (-not $process.HasExited -and $null -ne $resultReadyAt) {
    Stop-OwnedProcessTree -OwnedProcess $process
    $forcedCleanupAfterResult = $true
    Write-Host (
        "Unity reached the hard deadline after producing complete result XML; " +
        "the owned process tree was terminated before result evaluation.")
}

if ($forcedCleanupAfterResult) {
    try {
        [void]$process.WaitForExit(5000)
        $process.Refresh()
    }
    catch {
        # The exact owned PID has already been terminated; XML remains authoritative.
    }
}

if ($forcedCleanupAfterResult -and -not $process.HasExited) {
    $reason = "The owned Unity process did not terminate after result cleanup."
    Write-RunSummary -RunStatus "blocked" -RunReason $reason -RunExitCode $ResultCode.TimedOut
    Write-Error $reason
    exit $ResultCode.TimedOut
}

if (-not $process.HasExited -and -not $forcedCleanupAfterResult) {
    Stop-OwnedProcessTree -OwnedProcess $process
    $reason = "Unity exceeded the $TimeoutSeconds second hard timeout."
    Write-RunSummary -RunStatus "blocked" -RunReason $reason -RunExitCode $ResultCode.TimedOut
    if (Test-Path -LiteralPath $logPath) {
        Get-Content -LiteralPath $logPath -Tail 80
    }
    Write-Error "$reason PID $($process.Id) was terminated."
    exit $ResultCode.TimedOut
}

$process.WaitForExit()
$process.Refresh()
$exitCode = $process.ExitCode
$logText = if (Test-Path -LiteralPath $logPath) {
    Get-Content -Raw -LiteralPath $logPath
}
else {
    ""
}

if ([string]::IsNullOrWhiteSpace($logText)) {
    $reason = "Unity exited with code $exitCode without a non-empty log."
    Write-RunSummary -RunStatus "blocked" -RunReason $reason -RunExitCode $ResultCode.InvalidResult
    Write-Error $reason
    exit $ResultCode.InvalidResult
}

if ($logText -match '(?m)^\s*error CS\d+' -or
    $logText -match 'Scripts have compiler errors' -or
    $logText -match 'Compilation failed') {
    $reason = "Unity reported script compilation errors."
    Write-RunSummary -RunStatus "failed" -RunReason $reason -RunExitCode $ResultCode.CompilationFailed
    Get-Content -LiteralPath $logPath -Tail 80
    Write-Error $reason
    exit $ResultCode.CompilationFailed
}

if ($Mode -notmatch "Tests$") {
    if ($exitCode -ne 0) {
        $reason = "Unity $Mode exited with code $exitCode."
        Write-RunSummary -RunStatus "failed" -RunReason $reason -RunExitCode $exitCode
        Get-Content -LiteralPath $logPath -Tail 80
        Write-Error $reason
        exit $exitCode
    }

    Write-RunSummary -RunStatus "passed" -RunReason "$Mode completed." -RunExitCode $ResultCode.Passed
    exit $ResultCode.Passed
}

if (-not (Test-Path -LiteralPath $resultPath) -or
    (Get-Item -LiteralPath $resultPath).Length -eq 0) {
    $reason = "Unity exited with code $exitCode without a non-empty test result XML."
    Write-RunSummary -RunStatus "blocked" -RunReason $reason -RunExitCode $ResultCode.MissingResult
    Get-Content -LiteralPath $logPath -Tail 80
    Write-Error $reason
    exit $ResultCode.MissingResult
}

try {
    [xml]$resultXml = Get-Content -Raw -LiteralPath $resultPath
    $testRun = $resultXml.'test-run'
    if ($null -eq $testRun) {
        throw "The XML does not contain a test-run root."
    }
    $testSummary = [ordered]@{
        total = [int]$testRun.total
        passed = [int]$testRun.passed
        failed = [int]$testRun.failed
        skipped = [int]$testRun.skipped
        inconclusive = [int]$testRun.inconclusive
        durationSeconds = [double]$testRun.duration
        result = [string]$testRun.result
    }
}
catch {
    $reason = "Unity produced an invalid test result XML: $($_.Exception.Message)"
    Write-RunSummary -RunStatus "failed" -RunReason $reason -RunExitCode $ResultCode.InvalidResult
    Write-Error $reason
    exit $ResultCode.InvalidResult
}

if ($testSummary.total -le 0) {
    $reason = "Unity result XML contains no executed tests."
    Write-RunSummary -RunStatus "failed" -RunReason $reason -RunExitCode $ResultCode.InvalidResult
    Write-Error $reason
    exit $ResultCode.InvalidResult
}

if ($testSummary.failed -gt 0 -or $testSummary.result -notmatch '^Passed') {
    $reason = "Unity tests failed: total=$($testSummary.total) passed=$($testSummary.passed) failed=$($testSummary.failed)."
    Write-RunSummary -RunStatus "failed" -RunReason $reason -RunExitCode $ResultCode.TestFailed
    Write-Error $reason
    exit $ResultCode.TestFailed
}

if ($exitCode -ne 0 -and -not $forcedCleanupAfterResult) {
    $reason = "Unity tests passed in XML but Unity exited with code $exitCode."
    Write-RunSummary -RunStatus "failed" -RunReason $reason -RunExitCode $exitCode
    Write-Error $reason
    exit $exitCode
}

$reason = if ($forcedCleanupAfterResult) {
    "Unity tests passed in complete XML: total=$($testSummary.total) " +
    "passed=$($testSummary.passed) failed=0; the owned Unity process tree " +
    "was terminated after a $ResultExitGraceSeconds second shutdown grace period."
}
else {
    "Unity tests passed: total=$($testSummary.total) passed=$($testSummary.passed) failed=0."
}
Write-RunSummary -RunStatus "passed" -RunReason $reason -RunExitCode $ResultCode.Passed
exit $ResultCode.Passed
