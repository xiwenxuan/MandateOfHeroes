[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$RunId,
    [ValidateRange(1, 32)]
    [int]$GroupCount = 12,
    [ValidateRange(1, 32)]
    [int]$GroupIndex = 1,
    [ValidateRange(30, 900)]
    [int]$TimeoutSeconds = 240,
    [ValidateSet("Standard", "SlowDeterminism", "AssetBuildIntegration", "AssetManifestIntegration")]
    [string]$TimeoutClass = "Standard",
    [switch]$UseGraphics,
    [switch]$ListOnly,
    [switch]$AggregateOnly,
    [string]$ProjectPath = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $PSScriptRoot
}
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$testRoot = Join-Path $resolvedProject "Assets\Tests\EditMode"
$safeRunner = Join-Path $resolvedProject "Tools\Run-UnityTestsSafe.ps1"
$runRoot = Join-Path $resolvedProject "tmp\unity-editmode-groups\$RunId"
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null

if ($GroupIndex -gt $GroupCount) {
    throw "GroupIndex $GroupIndex exceeds GroupCount $GroupCount."
}
if (-not (Test-Path -LiteralPath $safeRunner)) {
    throw "Safe Unity runner not found: $safeRunner"
}

$testFiles = @(Get-ChildItem -LiteralPath $testRoot -Filter "*.cs" -File |
    Sort-Object FullName)
if ($testFiles.Count -eq 0) {
    throw "No EditMode C# test files were found under $testRoot."
}

$testNames = New-Object 'System.Collections.Generic.List[string]'
$sourceFacts = New-Object 'System.Collections.Generic.List[object]'
foreach ($file in $testFiles) {
    $text = Get-Content -Raw -LiteralPath $file.FullName
    $namespaceMatch = [regex]::Match(
        $text,
        '(?m)^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)')
    $classMatches = [regex]::Matches(
        $text,
        '(?m)^\s*public\s+(?:(?:sealed|static|partial)\s+)*class\s+([A-Za-z_][A-Za-z0-9_]*)')
    if (-not $namespaceMatch.Success -or $classMatches.Count -eq 0) {
        continue
    }

    $namespaceName = $namespaceMatch.Groups[1].Value
    for ($classIndex = 0; $classIndex -lt $classMatches.Count;
         $classIndex++) {
        $classMatch = $classMatches[$classIndex]
        $className = $classMatch.Groups[1].Value
        $classStart = $classMatch.Index
        $classEnd = if ($classIndex + 1 -lt $classMatches.Count) {
            $classMatches[$classIndex + 1].Index
        }
        else {
            $text.Length
        }
        $classText = $text.Substring($classStart, $classEnd - $classStart)
        $methodMatches = [regex]::Matches(
            $classText,
            '(?ms)\[Test\]\s*(?:\[[^\]]+\]\s*)*public\s+void\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(')
        foreach ($methodMatch in $methodMatches) {
            $testNames.Add(
                "$namespaceName.$className.$($methodMatch.Groups[1].Value)")
        }
    }
    $sourceFacts.Add([ordered]@{
        path = $file.FullName.Substring($resolvedProject.Length + 1).Replace('\', '/')
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash
    })
}

$orderedTests = @($testNames | Sort-Object -Unique)
if ($orderedTests.Count -eq 0) {
    throw "No [Test] public void EditMode tests could be discovered."
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
if ($TimeoutSeconds -gt 300 -and $TimeoutClass -eq "Standard") {
    throw (
        "TimeoutSeconds above 300 requires the explicit " +
        "classified timeout class.")
}
if ($TimeoutClass -in @("AssetBuildIntegration", "AssetManifestIntegration") -and
    $TimeoutSeconds -gt 600) {
    throw "$TimeoutClass is capped at 600 seconds."
}

$fingerprintText = ($sourceFacts | ForEach-Object {
    "$($_.path):$($_.sha256)"
}) -join "`n"
$fingerprintBytes = [Text.Encoding]::UTF8.GetBytes($fingerprintText)
$sha = [Security.Cryptography.SHA256]::Create()
try {
    $sourceFingerprint = ([BitConverter]::ToString(
        $sha.ComputeHash($fingerprintBytes))).Replace('-', '')
}
finally {
    $sha.Dispose()
}

$groups = @{}
for ($group = 1; $group -le $GroupCount; $group++) {
    $groups[$group] = New-Object 'System.Collections.Generic.List[string]'
}
for ($index = 0; $index -lt $orderedTests.Count; $index++) {
    $targetGroup = ($index % $GroupCount) + 1
    $groups[$targetGroup].Add($orderedTests[$index])
}

$manifest = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    generatedAt = (Get-Date).ToString('o')
    sourceFingerprint = $sourceFingerprint
    groupCount = $GroupCount
    totalExpected = $orderedTests.Count
    sourceFiles = $sourceFacts
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
$manifestPath = Join-Path $runRoot "manifest.json"
$manifest | ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $manifestPath -Encoding UTF8

if ($ListOnly) {
    Write-Host (
        "RESULT unity-editmode-groups status=listed total=$($orderedTests.Count) " +
        "groups=$GroupCount fingerprint=$sourceFingerprint")
    for ($group = 1; $group -le $GroupCount; $group++) {
        Write-Host "Group $group expected=$($groups[$group].Count)"
    }
    Write-Host "Manifest: $manifestPath"
    exit 0
}

if (-not $AggregateOnly) {
    $classifiedTests = if ($TimeoutClass -eq "SlowDeterminism") {
        $slowDeterminismTests
    }
    elseif ($TimeoutClass -eq "AssetBuildIntegration") {
        $assetBuildIntegrationTests
    }
    elseif ($TimeoutClass -eq "AssetManifestIntegration") {
        $assetManifestIntegrationTests
    }
    else {
        @()
    }
    $groupClassifiedTests = @($groups[$GroupIndex] | Where-Object {
        $_ -in $classifiedTests
    })
    if ($TimeoutClass -ne "Standard" -and
        $groupClassifiedTests.Count -eq 0) {
        throw (
            "Group $GroupIndex has no classified $TimeoutClass tests; " +
            "retain the standard 300-second ceiling.")
    }
    $groupDirectory = Join-Path $runRoot "group-$GroupIndex"
    New-Item -ItemType Directory -Path $groupDirectory -Force | Out-Null
    $groupMetadataPath = Join-Path $groupDirectory "expected.json"
    [ordered]@{
        runId = $RunId
        groupIndex = $GroupIndex
        sourceFingerprint = $sourceFingerprint
        createdAt = (Get-Date).ToString('o')
        timeoutClass = $TimeoutClass
        timeoutSeconds = $TimeoutSeconds
        classifiedTests = $groupClassifiedTests
        slowDeterminismTests = if ($TimeoutClass -eq "SlowDeterminism") {
            $groupClassifiedTests
        } else { @() }
        expectedTests = @($groups[$GroupIndex])
    } | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath $groupMetadataPath -Encoding UTF8

    # Unity Test Framework accepts multiple exact full names separated by
    # semicolons. Its log renders that list with commas, but commas in the CLI
    # value are treated as part of one unmatched filter.
    $filter = @($groups[$GroupIndex]) -join ';'
    Write-Host (
        "Unity EditMode group $GroupIndex/${GroupCount}: " +
        "$($groups[$GroupIndex].Count) tests")
    Write-Host "Run root: $runRoot"
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $safeRunner,
        '-Mode', 'EditModeTests',
        '-TestFilter', $filter,
        '-TimeoutSeconds', $TimeoutSeconds,
        '-TimeoutClass', $TimeoutClass,
        '-StartupTimeoutSeconds', 45,
        '-ResultExitGraceSeconds', 15,
        '-ProjectPath', $resolvedProject,
        '-OutputDirectory', $groupDirectory
    )
    if ($UseGraphics) {
        $arguments += '-UseGraphics'
    }
    & powershell.exe @arguments
    exit $LASTEXITCODE
}

$aggregateGroups = New-Object 'System.Collections.Generic.List[object]'
$allActual = New-Object 'System.Collections.Generic.List[string]'
$aggregateFailed = 0
for ($group = 1; $group -le $GroupCount; $group++) {
    $groupDirectory = Join-Path $runRoot "group-$group"
    $metadataPath = Join-Path $groupDirectory "expected.json"
    if (-not (Test-Path -LiteralPath $metadataPath)) {
        throw "Group $group has no execution metadata: $metadataPath"
    }
    $metadataFile = Get-Item -LiteralPath $metadataPath
    $metadata = Get-Content -Raw -LiteralPath $metadataPath | ConvertFrom-Json
    if ($metadata.sourceFingerprint -ne $sourceFingerprint) {
        throw "Group $group was run against a different EditMode source fingerprint."
    }
    $xmlFile = Get-ChildItem -LiteralPath $groupDirectory -Filter "*.xml" -File |
        Where-Object { $_.LastWriteTime -ge $metadataFile.LastWriteTime } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -eq $xmlFile) {
        throw "Group $group has no XML newer than its execution metadata."
    }

    [xml]$xml = Get-Content -Raw -LiteralPath $xmlFile.FullName
    $testRun = $xml.'test-run'
    if ($null -eq $testRun) {
        throw "Group $group XML has no test-run root: $($xmlFile.FullName)"
    }
    $actual = @($xml.SelectNodes('//test-case') | ForEach-Object {
        [string]$_.fullname
    } | Sort-Object -Unique)
    $expected = @($metadata.expectedTests | Sort-Object -Unique)
    $missing = @($expected | Where-Object { $_ -notin $actual })
    $unexpected = @($actual | Where-Object { $_ -notin $expected })
    if ($missing.Count -gt 0 -or $unexpected.Count -gt 0) {
        throw (
            "Group $group test-set mismatch: missing=$($missing.Count) " +
            "unexpected=$($unexpected.Count).")
    }
    foreach ($name in $actual) {
        $allActual.Add($name)
    }
    $aggregateFailed += [int]$testRun.failed
    $aggregateGroups.Add([ordered]@{
        index = $group
        xmlPath = $xmlFile.FullName
        total = [int]$testRun.total
        passed = [int]$testRun.passed
        failed = [int]$testRun.failed
        skipped = [int]$testRun.skipped
        durationSeconds = [double]$testRun.duration
        result = [string]$testRun.result
    })
}

$duplicates = @($allActual | Group-Object | Where-Object Count -gt 1)
$uniqueActual = @($allActual | Sort-Object -Unique)
if ($duplicates.Count -gt 0 -or $uniqueActual.Count -ne $orderedTests.Count) {
    throw (
        "Aggregate test coverage mismatch: expected=$($orderedTests.Count) " +
        "actualUnique=$($uniqueActual.Count) duplicates=$($duplicates.Count).")
}

$aggregate = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    sourceFingerprint = $sourceFingerprint
    status = if ($aggregateFailed -eq 0) { 'passed' } else { 'failed' }
    total = (@($aggregateGroups | ForEach-Object {
        [int]$_['total']
    }) | Measure-Object -Sum).Sum
    passed = (@($aggregateGroups | ForEach-Object {
        [int]$_['passed']
    }) | Measure-Object -Sum).Sum
    failed = $aggregateFailed
    groups = $aggregateGroups
}
$aggregatePath = Join-Path $runRoot "aggregate.json"
$aggregate | ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath $aggregatePath -Encoding UTF8
Write-Host (
    "RESULT unity-editmode-groups status=$($aggregate.status) " +
    "total=$($aggregate.total) passed=$($aggregate.passed) failed=$aggregateFailed")
Write-Host "Aggregate: $aggregatePath"
if ($aggregateFailed -ne 0) {
    exit 3
}
exit 0
