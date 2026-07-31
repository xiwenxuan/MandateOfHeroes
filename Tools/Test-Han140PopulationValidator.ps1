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
            "admin.han140.yuzhou.chen" = @(112653, 1547572, "han140.p1.batch5.v1")
            "admin.han140.yuzhou.liang" = @(83300, 431283, "han140.p1.batch5.v1")
            "admin.han140.yuzhou.lu" = @(78447, 411590, "han140.p1.batch5.v1")
            "admin.han140.yuzhou.pei" = @(200495, 251393, "han140.p1.batch5.v1")
            "admin.han140.yuzhou.runan" = @(404448, 2100788, "han140.p1.batch5.v1")
            "admin.han140.yuzhou.yingchuan" = @(263440, 1436513, "han140.p1.batch5.v1")
            "admin.han140.yanzhou.chenliu" = @(177529, 869433, "han140.p1.batch6.v1")
            "admin.han140.yanzhou.dong" = @(136088, 603393, "han140.p1.batch6.v1")
            "admin.han140.yanzhou.dongping" = @(79012, 448270, "han140.p1.batch6.v1")
            "admin.han140.yanzhou.jibei" = @(45689, 235897, "han140.p1.batch6.v1")
            "admin.han140.yanzhou.jiyin" = @(133715, 657554, "han140.p1.batch6.v1")
            "admin.han140.yanzhou.rencheng" = @(36442, 194156, "han140.p1.batch6.v1")
            "admin.han140.yanzhou.shanyang" = @(109898, 606091, "han140.p1.batch6.v1")
            "admin.han140.yanzhou.taishan" = @(8929, 437317, "han140.p1.batch6.v1")
            "admin.han140.xuzhou.donghai" = @(148784, 706416, "han140.p1.batch7.v1")
            "admin.han140.xuzhou.guangling" = @(83907, 410190, "han140.p1.batch7.v1")
            "admin.han140.xuzhou.langya" = @(20804, 570967, "han140.p1.batch7.v1")
            "admin.han140.xuzhou.pengcheng" = @(86170, 493027, "han140.p1.batch7.v1")
            "admin.han140.xuzhou.xiapi" = @(136389, 611083, "han140.p1.batch7.v1")
            "admin.han140.qingzhou.beihai" = @(158641, 853604, "han140.p1.batch8.v1")
            "admin.han140.qingzhou.donglai" = @(104297, 484393, "han140.p1.batch8.v1")
            "admin.han140.qingzhou.jinan" = @(78544, 453308, "han140.p1.batch8.v1")
            "admin.han140.qingzhou.lean" = @(74400, 424075, "han140.p1.batch8.v1")
            "admin.han140.qingzhou.pingyuan" = @(155588, 1002658, "han140.p1.batch8.v1")
            "admin.han140.qingzhou.qi" = @(64415, 491765, "han140.p1.batch8.v1")
            "admin.han140.jingzhou.nanyang" = @(528551, 2439618, "han140.p1.batch9.v1")
            "admin.han140.jingzhou.nan" = @(162570, 747604, "han140.p1.batch9.v1")
            "admin.han140.jingzhou.jiangxia" = @(58434, 265464, "han140.p1.batch9.v1")
            "admin.han140.jingzhou.lingling" = @(212284, 1001578, "han140.p1.batch9.v1")
            "admin.han140.jingzhou.guiyang" = @(135029, 501403, "han140.p1.batch9.v1")
            "admin.han140.jingzhou.wuling" = @(46672, 250913, "han140.p1.batch9.v1")
            "admin.han140.jingzhou.changsha" = @(255854, 1059372, "han140.p1.batch9.v1")
            "admin.han140.yangzhou.jiujiang" = @(89436, 432426, "han140.p1.batch10.v1")
            "admin.han140.yangzhou.danyang" = @(136518, 630545, "han140.p1.batch10.v1")
            "admin.han140.yangzhou.lujiang" = @(101392, 424683, "han140.p1.batch10.v1")
            "admin.han140.yangzhou.kuaiji" = @(123090, 481196, "han140.p1.batch10.v1")
            "admin.han140.yangzhou.wu" = @(164164, 700782, "han140.p1.batch10.v1")
            "admin.han140.yangzhou.yuzhang" = @(406496, 1668906, "han140.p1.batch10.v1")
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
        Assert-True -Condition ([int]$audit.row_counts.administrative_units -eq 75) -Message "Audit administrative unit count is not 75."
        Assert-True -Condition ([int]$audit.row_counts.population_records -eq 65) -Message "Audit population record count is not 65."
        Assert-True -Condition ([long]$audit.population_totals.raw_households -eq 7323137) -Message "Recorded raw household total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.raw_population -eq 38419908) -Message "Recorded raw population total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.effective_households -eq 7554123) -Message "Recorded effective household total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.effective_population -eq 38736500) -Message "Recorded effective population total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.household_difference_from_anchor -eq -2144507) -Message "Household anchor difference is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.population_difference_from_anchor -eq -10413720) -Message "Population anchor difference is incorrect."
        Assert-True -Condition ([int]$audit.data_quality.records_missing_raw_households -eq 1) -Message "Raw household missing count is not 1."
        Assert-True -Condition ([int]$audit.data_quality.records_missing_raw_population -eq 1) -Message "Raw population missing count is not 1."
        Assert-True -Condition ([int]$audit.data_quality.records_with_corrections -eq 7) -Message "Correction record count is not 7."

        $byId = @{}
        foreach ($row in $populationRows) {
            $byId[[string]$row.admin_unit_id] = $row
        }
        Assert-True -Condition ([long]$byId["admin.han140.youzhou.liaodong"].registered_population_corrected -eq 281714) -Message "Liaodong corrected population is incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.youzhou.xuantu"].registered_households_corrected -eq 9594) -Message "Xuantu corrected households are incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.youzhou.liaodongshuguo"].registered_households_corrected -eq 22986) -Message "Liaodong dependency estimated households are incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.youzhou.liaodongshuguo"].registered_population_corrected -eq 116592) -Message "Liaodong dependency estimated population is incorrect."
        Assert-True -Condition ([string]$byId["admin.han140.youzhou.liaodongshuguo"].evidence_grade -ceq "M") -Message "Liaodong dependency estimate is not marked as model evidence."
        Assert-True -Condition ([long]$byId["admin.han140.yuzhou.pei"].registered_population_corrected -eq 1251393) -Message "Pei corrected population is incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.yuzhou.chen"].registered_population_corrected -eq 547572) -Message "Chen corrected population is incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.yanzhou.taishan"].registered_households_corrected -eq 108929) -Message "Taishan corrected households are incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.xuzhou.langya"].registered_households_corrected -eq 120804) -Message "Langya corrected households are incorrect."
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

    Invoke-TestCase -Name "Yuzhou commandery and kingdom slice complete with paired correction" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $yuzhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.yuzhou" })
        $yuzhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.yuzhou.*" })
        $rawHouseholds = [long](($yuzhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $rawPopulation = [long](($yuzhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $effectivePopulation = [long]0
        foreach ($row in $yuzhouPopulation) {
            $effectivePopulation += if ([string]::IsNullOrWhiteSpace([string]$row.registered_population_corrected)) {
                [long]$row.registered_population_raw
            }
            else {
                [long]$row.registered_population_corrected
            }
        }
        $correctedRows = @(
            $yuzhouPopulation |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
                }
        )

        Assert-True -Condition ($yuzhouAdmins.Count -eq 6) -Message "Yuzhou does not contain exactly six direct commandery/kingdom units."
        Assert-True -Condition ($yuzhouPopulation.Count -eq 6) -Message "Yuzhou does not contain exactly six population records."
        Assert-True -Condition ($rawHouseholds -eq 1142783) -Message "Yuzhou household total is incorrect."
        Assert-True -Condition ($rawPopulation -eq 6179139) -Message "Yuzhou raw population total is incorrect."
        Assert-True -Condition ($effectivePopulation -eq 6179139) -Message "Yuzhou effective population total is incorrect."
        Assert-True -Condition ($correctedRows.Count -eq 2) -Message "Yuzhou must contain exactly the paired Pei/Chen corrections."
    }

    Invoke-TestCase -Name "Yanzhou commandery and kingdom slice complete with Taishan correction" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $yanzhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.yanzhou" })
        $yanzhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.yanzhou.*" })
        $rawHouseholds = [long](($yanzhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $population = [long](($yanzhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $effectiveHouseholds = [long]0
        foreach ($row in $yanzhouPopulation) {
            $effectiveHouseholds += if ([string]::IsNullOrWhiteSpace([string]$row.registered_households_corrected)) {
                [long]$row.registered_households_raw
            }
            else {
                [long]$row.registered_households_corrected
            }
        }
        $correctedRows = @(
            $yanzhouPopulation |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
                }
        )

        Assert-True -Condition ($yanzhouAdmins.Count -eq 8) -Message "Yanzhou does not contain exactly eight direct commandery/kingdom units."
        Assert-True -Condition ($yanzhouPopulation.Count -eq 8) -Message "Yanzhou does not contain exactly eight population records."
        Assert-True -Condition ($rawHouseholds -eq 727302) -Message "Yanzhou raw household total is incorrect."
        Assert-True -Condition ($effectiveHouseholds -eq 827302) -Message "Yanzhou effective household total is incorrect."
        Assert-True -Condition ($population -eq 4052111) -Message "Yanzhou population total is incorrect."
        Assert-True -Condition ($correctedRows.Count -eq 1) -Message "Yanzhou must contain exactly the Taishan correction."
    }

    Invoke-TestCase -Name "Xuzhou commandery and kingdom slice complete with Langya correction" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $xuzhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.xuzhou" })
        $xuzhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.xuzhou.*" })
        $rawHouseholds = [long](($xuzhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $population = [long](($xuzhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $effectiveHouseholds = [long]0
        foreach ($row in $xuzhouPopulation) {
            $effectiveHouseholds += if ([string]::IsNullOrWhiteSpace([string]$row.registered_households_corrected)) {
                [long]$row.registered_households_raw
            }
            else {
                [long]$row.registered_households_corrected
            }
        }
        $correctedRows = @(
            $xuzhouPopulation |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
                }
        )

        Assert-True -Condition ($xuzhouAdmins.Count -eq 5) -Message "Xuzhou does not contain exactly five direct commandery/kingdom units."
        Assert-True -Condition ($xuzhouPopulation.Count -eq 5) -Message "Xuzhou does not contain exactly five population records."
        Assert-True -Condition ($rawHouseholds -eq 476054) -Message "Xuzhou raw household total is incorrect."
        Assert-True -Condition ($effectiveHouseholds -eq 576054) -Message "Xuzhou effective household total is incorrect."
        Assert-True -Condition ($population -eq 2791683) -Message "Xuzhou population total is incorrect."
        Assert-True -Condition ($correctedRows.Count -eq 1) -Message "Xuzhou must contain exactly the Langya correction."
    }

    Invoke-TestCase -Name "Qingzhou commandery and kingdom slice complete with Jinan variant" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $qingzhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.qingzhou" })
        $qingzhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.qingzhou.*" })
        $households = [long](($qingzhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $population = [long](($qingzhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $correctedRows = @(
            $qingzhouPopulation |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
                }
        )
        $jinan = @($qingzhouAdmins | Where-Object { $_.admin_unit_id -ceq "admin.han140.qingzhou.jinan" })

        Assert-True -Condition ($qingzhouAdmins.Count -eq 6) -Message "Qingzhou does not contain exactly six direct commandery/kingdom units."
        Assert-True -Condition ($qingzhouPopulation.Count -eq 6) -Message "Qingzhou does not contain exactly six population records."
        Assert-True -Condition ($households -eq 635885) -Message "Qingzhou household total is incorrect."
        Assert-True -Condition ($population -eq 3709803) -Message "Qingzhou population total is incorrect."
        Assert-True -Condition ($correctedRows.Count -eq 0) -Message "Qingzhou must not turn the Jinan type variant into a population correction."
        Assert-True -Condition ($jinan.Count -eq 1) -Message "Jinan administrative unit is missing."
        Assert-True -Condition ([string]$jinan[0].unit_type -ceq "kingdom") -Message "Jinan must be recorded as a kingdom for the 140 snapshot."
        Assert-True -Condition ([string]$jinan[0].confidence -ceq "medium") -Message "Jinan type variant must retain medium confidence."
    }

    Invoke-TestCase -Name "Jingzhou commandery slice complete" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $jingzhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.jingzhou" })
        $jingzhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.jingzhou.*" })
        $households = [long](($jingzhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $population = [long](($jingzhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $correctedRows = @(
            $jingzhouPopulation |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
                }
        )
        $nonCommanderies = @($jingzhouAdmins | Where-Object { $_.unit_type -cne "commandery" })

        Assert-True -Condition ($jingzhouAdmins.Count -eq 7) -Message "Jingzhou does not contain exactly seven direct commanderies."
        Assert-True -Condition ($jingzhouPopulation.Count -eq 7) -Message "Jingzhou does not contain exactly seven population records."
        Assert-True -Condition ($households -eq 1399394) -Message "Jingzhou household total is incorrect."
        Assert-True -Condition ($population -eq 6265952) -Message "Jingzhou population total is incorrect."
        Assert-True -Condition ($correctedRows.Count -eq 0) -Message "Jingzhou must not contain population corrections."
        Assert-True -Condition ($nonCommanderies.Count -eq 0) -Message "All seven Jingzhou units must be commanderies in the 140 snapshot."
    }

    Invoke-TestCase -Name "Yangzhou commandery slice complete" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $yangzhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.yangzhou" })
        $yangzhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.yangzhou.*" })
        $households = [long](($yangzhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $population = [long](($yangzhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $correctedRows = @(
            $yangzhouPopulation |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
                }
        )
        $nonCommanderies = @($yangzhouAdmins | Where-Object { $_.unit_type -cne "commandery" })

        Assert-True -Condition ($yangzhouAdmins.Count -eq 6) -Message "Yangzhou does not contain exactly six direct commanderies."
        Assert-True -Condition ($yangzhouPopulation.Count -eq 6) -Message "Yangzhou does not contain exactly six population records."
        Assert-True -Condition ($households -eq 1021096) -Message "Yangzhou household total is incorrect."
        Assert-True -Condition ($population -eq 4338538) -Message "Yangzhou population total is incorrect."
        Assert-True -Condition ($correctedRows.Count -eq 0) -Message "Yangzhou must not contain population corrections."
        Assert-True -Condition ($nonCommanderies.Count -eq 0) -Message "All six Yangzhou units must be commanderies in the 140 snapshot."
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
