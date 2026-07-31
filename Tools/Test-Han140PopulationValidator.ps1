[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}

$repositoryRootPath = [System.IO.Path]::GetFullPath($RepositoryRoot)
$validatorPath = Join-Path $repositoryRootPath "Tools\Validate-Han140PopulationData.ps1"
$productionDataPath = Join-Path $repositoryRootPath "Data\HistoricalPopulation"
$productionAuditPath = Join-Path $productionDataPath "han_140_audit_report.json"
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("MandateHan140-" + [guid]::NewGuid().ToString("N"))
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$passed = 0
$failed = 0

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Copy-InputData {
    param([string]$Destination)

    [void](New-Item -ItemType Directory -Path $Destination -Force)
    foreach ($name in @(
        "han_140_sources.json",
        "han_140_administrative_units.csv",
        "han_140_population_records.csv",
        "stable_population_regions.csv",
        "han_140_region_mapping.csv",
        "game_location_crosswalk.csv"
    )) {
        Copy-Item -LiteralPath (Join-Path $productionDataPath $name) -Destination (Join-Path $Destination $name)
    }
}

function Invoke-Validator {
    param(
        [string]$CaseRoot,
        [string]$OutputPath
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $outputLines = @(
            & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $validatorPath `
                -DataRoot $CaseRoot `
                -OutputPath $OutputPath 2>&1
        )
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = ($outputLines -join [Environment]::NewLine)
    }
}

function Invoke-TestCase {
    param(
        [string]$Name,
        [scriptblock]$Body
    )

    try {
        & $Body
        $script:passed++
        Write-Host "PASS $Name"
    }
    catch {
        $script:failed++
        Write-Host "FAIL $Name - $($_.Exception.Message)"
    }
}

function Assert-Rejected {
    param(
        [string]$Name,
        [scriptblock]$Mutate
    )

    $caseRoot = Join-Path $temporaryRoot $Name
    Copy-InputData -Destination $caseRoot
    & $Mutate $caseRoot
    $auditPath = Join-Path $caseRoot "test_audit.json"
    $result = Invoke-Validator -CaseRoot $caseRoot -OutputPath $auditPath
    Assert-True -Condition ($result.ExitCode -ne 0) -Message "$Name unexpectedly passed validation."
    Assert-True -Condition (-not (Test-Path -LiteralPath $auditPath)) -Message "$Name generated an audit report despite validation failure."
    Assert-True -Condition ($result.Output -match "han140-validation=failed") -Message "$Name did not emit a failed RESULT summary."
}

try {
    Assert-True -Condition (Test-Path -LiteralPath $validatorPath -PathType Leaf) -Message "Validator script is missing."
    Assert-True -Condition (Test-Path -LiteralPath $productionDataPath -PathType Container) -Message "Production data directory is missing."
    [void](New-Item -ItemType Directory -Path $temporaryRoot -Force)

    Invoke-TestCase -Name "valid production dataset" -Body {
        $caseRoot = Join-Path $temporaryRoot "valid"
        Copy-InputData -Destination $caseRoot
        $firstAudit = Join-Path $caseRoot "audit_first.json"
        $result = Invoke-Validator -CaseRoot $caseRoot -OutputPath $firstAudit
        Assert-True -Condition ($result.ExitCode -eq 0) -Message "Valid dataset failed: $($result.Output)"
        Assert-True -Condition (Test-Path -LiteralPath $firstAudit -PathType Leaf) -Message "Valid dataset did not create an audit report."
        Assert-True -Condition ($result.Output -match "han140-validation=passed") -Message "Valid dataset did not emit a passing RESULT summary."
    }

    Invoke-TestCase -Name "recorded facts and audit totals" -Body {
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $audit = Get-Content -Raw -Encoding UTF8 -LiteralPath $productionAuditPath | ConvertFrom-Json
        $expected = @{
            "admin.han140.sili.hedong" = @(93543, 570803, "han140.p1.batch4.v1")
            "admin.han140.sili.henan" = @(208486, 1010827, "han140.p1.batch4.v1")
            "admin.han140.sili.henei" = @(159770, 801558, "han140.p1.batch4.v1")
            "admin.han140.sili.hongnong" = @(46815, 199113, "han140.p1.batch4.v1")
            "admin.han140.sili.jingzhao" = @(53299, 285574, "han140.p1.batch4.v1")
            "admin.han140.sili.youfufeng" = @(17352, 93091, "han140.p1.batch4.v1")
            "admin.han140.sili.zuopingyi" = @(37090, 145195, "han140.p1.batch4.v1")
            "admin.han140.jizhou.anping" = @(91440, 655118, "han140.p1.batch1.v1")
            "admin.han140.jizhou.bohai" = @(132389, 1106500, "han140.p1.batch2.v1")
            "admin.han140.jizhou.changshan" = @(97500, 631184, "han140.p1.batch2.v1")
            "admin.han140.jizhou.hejian" = @(93754, 634421, "han140.p1.batch2.v1")
            "admin.han140.jizhou.julu" = @(109517, 602096, "han140.p1.batch1.v1")
            "admin.han140.jizhou.qinghe" = @(123964, 760418, "han140.p1.batch2.v1")
            "admin.han140.jizhou.wei" = @(129310, 695606, "han140.p1.batch1.v1")
            "admin.han140.jizhou.zhao" = @(32719, 188381, "han140.p1.batch2.v1")
            "admin.han140.jizhou.zhongshan" = @(97412, 658195, "han140.p1.batch1.v1")
            "admin.han140.youzhou.dai" = @(20123, 126188, "han140.p1.batch3.v1")
            "admin.han140.youzhou.guangyang" = @(44550, 280600, "han140.p1.batch1.v1")
            "admin.han140.youzhou.lelang" = @(61492, 257050, "han140.p1.batch3.v1")
            "admin.han140.youzhou.liaodong" = @(64158, 81714, "han140.p1.batch3.v1")
            "admin.han140.youzhou.liaodongshuguo" = @("", "", "han140.p1.batch3.v1")
            "admin.han140.youzhou.liaoxi" = @(14150, 81714, "han140.p1.batch3.v1")
            "admin.han140.youzhou.shanggu" = @(10352, 51204, "han140.p1.batch3.v1")
            "admin.han140.youzhou.xuantu" = @(1594, 43163, "han140.p1.batch3.v1")
            "admin.han140.youzhou.youbeiping" = @(9170, 53475, "han140.p1.batch3.v1")
            "admin.han140.youzhou.yuyang" = @(68456, 435740, "han140.p1.batch3.v1")
            "admin.han140.youzhou.zhuo" = @(102218, 633754, "han140.p1.batch1.v1")
        }
        $expectedIds = @($expected.Keys | Sort-Object)
        $actualIds = @($populationRows.admin_unit_id | Sort-Object)

        Assert-True -Condition (($actualIds -join "|") -ceq ($expectedIds -join "|")) -Message "Production population IDs do not match the recorded batches."
        foreach ($row in $populationRows) {
            $values = $expected[[string]$row.admin_unit_id]
            if ([string]::IsNullOrWhiteSpace([string]$values[0])) {
                Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$row.registered_households_raw)) -Message "Raw households should be missing for '$($row.admin_unit_id)'."
            }
            else {
                Assert-True -Condition ([long]$row.registered_households_raw -eq [long]$values[0]) -Message "Households differ for '$($row.admin_unit_id)'."
            }
            if ([string]::IsNullOrWhiteSpace([string]$values[1])) {
                Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$row.registered_population_raw)) -Message "Raw population should be missing for '$($row.admin_unit_id)'."
            }
            else {
                Assert-True -Condition ([long]$row.registered_population_raw -eq [long]$values[1]) -Message "Population differs for '$($row.admin_unit_id)'."
            }
            Assert-True -Condition ([string]$row.model_version -ceq [string]$values[2]) -Message "Model version differs for '$($row.admin_unit_id)'."
        }
        Assert-True -Condition ([int]$audit.row_counts.sources -eq 3) -Message "Audit source count is not 3."
        Assert-True -Condition ([int]$audit.row_counts.administrative_units -eq 31) -Message "Audit administrative unit count is not 31."
        Assert-True -Condition ([int]$audit.row_counts.population_records -eq 27) -Message "Audit population record count is not 27."
        Assert-True -Condition ([long]$audit.population_totals.raw_households -eq 1920623) -Message "Recorded raw household total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.raw_population -eq 11082682) -Message "Recorded raw population total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.effective_households -eq 1951609) -Message "Recorded effective household total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.effective_population -eq 11399274) -Message "Recorded effective population total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.household_difference_from_anchor -eq -7747021) -Message "Household anchor difference is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.population_difference_from_anchor -eq -37750946) -Message "Population anchor difference is incorrect."
        Assert-True -Condition ([int]$audit.data_quality.records_missing_raw_households -eq 1) -Message "Raw household missing count is not 1."
        Assert-True -Condition ([int]$audit.data_quality.records_missing_raw_population -eq 1) -Message "Raw population missing count is not 1."
        Assert-True -Condition ([int]$audit.data_quality.records_with_corrections -eq 3) -Message "Correction record count is not 3."

        $byId = @{}
        foreach ($row in $populationRows) {
            $byId[[string]$row.admin_unit_id] = $row
        }
        Assert-True -Condition ([long]$byId["admin.han140.youzhou.liaodong"].registered_population_corrected -eq 281714) -Message "Liaodong corrected population is incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.youzhou.xuantu"].registered_households_corrected -eq 9594) -Message "Xuantu corrected households are incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.youzhou.liaodongshuguo"].registered_households_corrected -eq 22986) -Message "Liaodong dependency estimated households are incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.youzhou.liaodongshuguo"].registered_population_corrected -eq 116592) -Message "Liaodong dependency estimated population is incorrect."
        Assert-True -Condition ([string]$byId["admin.han140.youzhou.liaodongshuguo"].evidence_grade -ceq "M") -Message "Liaodong dependency estimate is not marked as model evidence."
    }

    Invoke-TestCase -Name "Jizhou commandery slice complete" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $jizhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.jizhou" })
        $jizhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.jizhou.*" })
        $households = [long](($jizhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $population = [long](($jizhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)

        Assert-True -Condition ($jizhouAdmins.Count -eq 9) -Message "Jizhou does not contain exactly nine direct commandery/state units."
        Assert-True -Condition ($jizhouPopulation.Count -eq 9) -Message "Jizhou does not contain exactly nine population records."
        Assert-True -Condition ($households -eq 908005) -Message "Jizhou household total is incorrect."
        Assert-True -Condition ($population -eq 5931919) -Message "Jizhou population total is incorrect."
    }

    Invoke-TestCase -Name "Sili commandery and metropolitan slice complete" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $siliAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.sili" })
        $siliPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.sili.*" })
        $households = [long](($siliPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $population = [long](($siliPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $correctedRows = @(
            $siliPopulation |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
                }
        )

        Assert-True -Condition ($siliAdmins.Count -eq 7) -Message "Sili does not contain exactly seven direct administrative units."
        Assert-True -Condition ($siliPopulation.Count -eq 7) -Message "Sili does not contain exactly seven population records."
        Assert-True -Condition ($households -eq 616355) -Message "Sili household total is incorrect."
        Assert-True -Condition ($population -eq 3106161) -Message "Sili population total is incorrect."
        Assert-True -Condition ($correctedRows.Count -eq 0) -Message "Sili source transcription differences must not create corrected values."
    }

    Invoke-TestCase -Name "Youzhou commandery slice complete with explicit source gap" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $youzhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.youzhou" })
        $youzhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.youzhou.*" })
        $rawHouseholds = [long](($youzhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $rawPopulation = [long](($youzhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $effectiveHouseholds = [long]0
        $effectivePopulation = [long]0
        foreach ($row in $youzhouPopulation) {
            $effectiveHouseholds += if ([string]::IsNullOrWhiteSpace([string]$row.registered_households_corrected)) {
                [long]$row.registered_households_raw
            }
            else {
                [long]$row.registered_households_corrected
            }
            $effectivePopulation += if ([string]::IsNullOrWhiteSpace([string]$row.registered_population_corrected)) {
                [long]$row.registered_population_raw
            }
            else {
                [long]$row.registered_population_corrected
            }
        }

        Assert-True -Condition ($youzhouAdmins.Count -eq 11) -Message "Youzhou does not contain exactly eleven direct commandery/dependency units."
        Assert-True -Condition ($youzhouPopulation.Count -eq 11) -Message "Youzhou does not contain exactly eleven population records."
        Assert-True -Condition ($rawHouseholds -eq 396263) -Message "Youzhou raw household total is incorrect."
        Assert-True -Condition ($rawPopulation -eq 2044602) -Message "Youzhou raw population total is incorrect."
        Assert-True -Condition ($effectiveHouseholds -eq 427249) -Message "Youzhou effective household total is incorrect."
        Assert-True -Condition ($effectivePopulation -eq 2361194) -Message "Youzhou effective population total is incorrect."
    }

    Invoke-TestCase -Name "deterministic audit output" -Body {
        $caseRoot = Join-Path $temporaryRoot "deterministic"
        Copy-InputData -Destination $caseRoot
        $firstAudit = Join-Path $caseRoot "audit_first.json"
        $secondAudit = Join-Path $caseRoot "audit_second.json"
        $first = Invoke-Validator -CaseRoot $caseRoot -OutputPath $firstAudit
        $second = Invoke-Validator -CaseRoot $caseRoot -OutputPath $secondAudit
        Assert-True -Condition ($first.ExitCode -eq 0 -and $second.ExitCode -eq 0) -Message "Deterministic audit setup failed."
        $firstHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $firstAudit).Hash
        $secondHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $secondAudit).Hash
        Assert-True -Condition ($firstHash -ceq $secondHash) -Message "Repeated validation produced different audit files."
    }

    Invoke-TestCase -Name "duplicate source ID rejected" -Body {
        Assert-Rejected -Name "duplicate-source" -Mutate {
            param($caseRoot)
            $path = Join-Path $caseRoot "han_140_sources.json"
            $document = Get-Content -Raw -Encoding UTF8 -LiteralPath $path | ConvertFrom-Json
            $document.sources = @($document.sources) + @($document.sources[0])
            $json = $document | ConvertTo-Json -Depth 8
            [System.IO.File]::WriteAllText($path, $json + [Environment]::NewLine, $utf8NoBom)
        }
    }

    Invoke-TestCase -Name "invalid year rejected" -Body {
        Assert-Rejected -Name "invalid-year" -Mutate {
            param($caseRoot)
            $path = Join-Path $caseRoot "han_140_administrative_units.csv"
            [System.IO.File]::AppendAllText(
                $path,
                "admin.han140.invalid,,commandery,测试郡,测试郡,,0,140,source.hou_han_shu.jun_guo_zhi,low,negative fixture" + [Environment]::NewLine,
                $utf8NoBom
            )
        }
    }

    Invoke-TestCase -Name "negative population rejected" -Body {
        Assert-Rejected -Name "negative-population" -Mutate {
            param($caseRoot)
            $adminPath = Join-Path $caseRoot "han_140_administrative_units.csv"
            [System.IO.File]::AppendAllText(
                $adminPath,
                "admin.han140.invalid,,commandery,测试郡,测试郡,,1,9999,source.hou_han_shu.jun_guo_zhi,low,negative fixture" + [Environment]::NewLine,
                $utf8NoBom
            )
            $populationPath = Join-Path $caseRoot "han_140_population_records.csv"
            [System.IO.File]::AppendAllText(
                $populationPath,
                "admin.han140.invalid,1,-1,,,fixture,negative population,H,source.hou_han_shu.jun_guo_zhi,test locator,han140.p1.batch1.v1" + [Environment]::NewLine,
                $utf8NoBom
            )
        }
    }

    Invoke-TestCase -Name "invalid model version rejected" -Body {
        Assert-Rejected -Name "invalid-model-version" -Mutate {
            param($caseRoot)
            $path = Join-Path $caseRoot "han_140_population_records.csv"
            $text = [System.IO.File]::ReadAllText($path)
            $text = $text.Replace("han140.p1.batch1.v1", "invalid-model-version")
            [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
        }
    }

    Invoke-TestCase -Name "missing source rejected" -Body {
        Assert-Rejected -Name "missing-source" -Mutate {
            param($caseRoot)
            $adminPath = Join-Path $caseRoot "han_140_administrative_units.csv"
            [System.IO.File]::AppendAllText(
                $adminPath,
                "admin.han140.invalid,,commandery,测试郡,测试郡,,1,9999,source.missing,low,missing source fixture" + [Environment]::NewLine,
                $utf8NoBom
            )
        }
    }

    Invoke-TestCase -Name "checked-in audit is current" -Body {
        $caseRoot = Join-Path $temporaryRoot "checked-in-audit"
        Copy-InputData -Destination $caseRoot
        $generatedAudit = Join-Path $caseRoot "generated_audit.json"
        $result = Invoke-Validator -CaseRoot $caseRoot -OutputPath $generatedAudit
        Assert-True -Condition ($result.ExitCode -eq 0) -Message "Could not regenerate production audit."
        $expectedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $productionAuditPath).Hash
        $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $generatedAudit).Hash
        Assert-True -Condition ($expectedHash -ceq $actualHash) -Message "Checked-in audit does not match validator output."
    }
}
finally {
    $tempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    $resolvedTemporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($tempBase, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}

Write-Host "RESULT han140-tests passed=$passed failed=$failed"
if ($failed -gt 0) {
    exit 1
}

exit 0
