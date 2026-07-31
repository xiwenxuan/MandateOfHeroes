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
            "admin.han140.yizhou.hanzhong" = @(57344, 267402, "han140.p1.batch11.v1")
            "admin.han140.yizhou.ba" = @(310691, 1086049, "han140.p1.batch11.v1")
            "admin.han140.yizhou.guanghan" = @(139865, 509438, "han140.p1.batch11.v1")
            "admin.han140.yizhou.shu" = @(300452, 1350476, "han140.p1.batch11.v1")
            "admin.han140.yizhou.jianwei" = @(137713, 411378, "han140.p1.batch11.v1")
            "admin.han140.yizhou.zangke" = @(31523, 267253, "han140.p1.batch11.v1")
            "admin.han140.yizhou.yuexi" = @(130120, 623418, "han140.p1.batch11.v1")
            "admin.han140.yizhou.yizhou" = @(29036, 110802, "han140.p1.batch11.v1")
            "admin.han140.yizhou.yongchang" = @(231897, 1897344, "han140.p1.batch11.v1")
            "admin.han140.yizhou.guanghanshuguo" = @(37110, 205652, "han140.p1.batch11.v1")
            "admin.han140.yizhou.shushuguo" = @(111568, 475629, "han140.p1.batch11.v1")
            "admin.han140.yizhou.jianweishuguo" = @(7938, 37187, "han140.p1.batch11.v1")
            "admin.han140.liangzhou.longxi" = @(5628, 29637, "han140.p1.batch12.v1")
            "admin.han140.liangzhou.hanyang" = @(27423, 130138, "han140.p1.batch12.v1")
            "admin.han140.liangzhou.wudu" = @(20102, 81728, "han140.p1.batch12.v1")
            "admin.han140.liangzhou.jincheng" = @(3858, 18947, "han140.p1.batch12.v1")
            "admin.han140.liangzhou.anding" = @(6094, 29060, "han140.p1.batch12.v1")
            "admin.han140.liangzhou.beidi" = @(3122, 18637, "han140.p1.batch12.v1")
            "admin.han140.liangzhou.wuwei" = @(10042, 34226, "han140.p1.batch12.v1")
            "admin.han140.liangzhou.zhangye" = @(6552, 26040, "han140.p1.batch12.v1")
            "admin.han140.liangzhou.jiuquan" = @(12706, "", "han140.p1.batch12.v1")
            "admin.han140.liangzhou.dunhuang" = @(748, 29170, "han140.p1.batch12.v1")
            "admin.han140.liangzhou.zhangyeshuguo" = @(4656, 16952, "han140.p1.batch12.v1")
            "admin.han140.liangzhou.zhangyejuyanshuguo" = @(1560, 4733, "han140.p1.batch12.v1")
            "admin.han140.bingzhou.shangdang" = @(26222, 127403, "han140.p1.batch13.v1")
            "admin.han140.bingzhou.taiyuan" = @(30902, 200124, "han140.p1.batch13.v1")
            "admin.han140.bingzhou.shang" = @(5169, 28599, "han140.p1.batch13.v1")
            "admin.han140.bingzhou.xihe" = @(5698, 20838, "han140.p1.batch13.v1")
            "admin.han140.bingzhou.wuyuan" = @(4667, 22957, "han140.p1.batch13.v1")
            "admin.han140.bingzhou.yunzhong" = @(5351, 26430, "han140.p1.batch13.v1")
            "admin.han140.bingzhou.dingxiang" = @(3153, 13571, "han140.p1.batch13.v1")
            "admin.han140.bingzhou.yanmen" = @(31862, 249000, "han140.p1.batch13.v1")
            "admin.han140.bingzhou.shuofang" = @(1987, 7843, "han140.p1.batch13.v1")
            "admin.han140.jiaozhou.nanhai" = @(71477, 250282, "han140.p1.batch14.v1")
            "admin.han140.jiaozhou.cangwu" = @(111395, 466975, "han140.p1.batch14.v1")
            "admin.han140.jiaozhou.yulin" = @("", "", "han140.p1.batch14.v1")
            "admin.han140.jiaozhou.hepu" = @(23121, 86617, "han140.p1.batch14.v1")
            "admin.han140.jiaozhou.jiaozhi" = @("", "", "han140.p1.batch14.v1")
            "admin.han140.jiaozhou.jiuzhen" = @(46513, 209894, "han140.p1.batch14.v1")
            "admin.han140.jiaozhou.rinan" = @(18263, 100676, "han140.p1.batch14.v1")
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
        Assert-True -Condition ([int]$audit.row_counts.sources -eq 4) -Message "Audit source count is not 4."
        Assert-True -Condition ([int]$audit.row_counts.administrative_units -eq 133) -Message "Audit administrative unit count is not 133."
        Assert-True -Condition ([int]$audit.row_counts.population_records -eq 105) -Message "Audit population record count is not 105."
        Assert-True -Condition ([long]$audit.population_totals.raw_households -eq 9336665) -Message "Recorded raw household total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.raw_population -eq 47892413) -Message "Recorded raw population total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.explicit_corrected_households -eq 411892) -Message "Recorded explicit corrected household total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.explicit_corrected_population -eq 3195624) -Message "Recorded explicit corrected population total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.effective_households -eq 9716482) -Message "Recorded effective household total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.effective_population -eq 49207358) -Message "Recorded effective population total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.household_difference_from_anchor -eq 17852) -Message "Household anchor difference is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.population_difference_from_anchor -eq 57138) -Message "Population anchor difference is incorrect."
        Assert-True -Condition ([int]$audit.data_quality.records_missing_raw_households -eq 3) -Message "Raw household missing count is not 3."
        Assert-True -Condition ([int]$audit.data_quality.records_missing_raw_population -eq 4) -Message "Raw population missing count is not 4."
        Assert-True -Condition ([int]$audit.data_quality.records_with_corrections -eq 11) -Message "Correction record count is not 11."
        Assert-True -Condition ([int]$audit.row_counts.stable_regions -eq 168) -Message "Audit stable region count is not 168."
        Assert-True -Condition ([int]$audit.row_counts.region_mappings -eq 105) -Message "Audit region mapping count is not 105."
        Assert-True -Condition ([int]$audit.row_counts.game_location_crosswalks -eq 31) -Message "Audit game location crosswalk count is not 31."
        Assert-True -Condition ([int]$audit.data_quality.provisional_stable_regions -eq 168) -Message "Provisional stable region count is not 168."
        Assert-True -Condition ([int]$audit.data_quality.provisional_region_mappings -eq 105) -Message "Provisional region mapping count is not 105."
        Assert-True -Condition ([int]$audit.data_quality.unresolved_game_locations -eq 3) -Message "Unresolved game location count is not 3."
        Assert-True -Condition ([int]$audit.mapping_audit.mapped_admin_source_count -eq 105) -Message "Mapped administrative source count is not 105."
        Assert-True -Condition ([int]$audit.mapping_audit.weight_error_count -eq 0) -Message "Mapping weight error count is not 0."

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
        Assert-True -Condition ([long]$byId["admin.han140.liangzhou.jiuquan"].registered_population_corrected -eq 46631) -Message "Jiuquan corrected population is incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.liangzhou.dunhuang"].registered_households_corrected -eq 7748) -Message "Dunhuang corrected households are incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.jiaozhou.yulin"].registered_households_corrected -eq 12415) -Message "Yulin estimated households are incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.jiaozhou.yulin"].registered_population_corrected -eq 71162) -Message "Yulin estimated population is incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.jiaozhou.jiaozhi"].registered_households_corrected -eq 129416) -Message "Jiaozhi estimated households are incorrect."
        Assert-True -Condition ([long]$byId["admin.han140.jiaozhou.jiaozhi"].registered_population_corrected -eq 880560) -Message "Jiaozhi estimated population is incorrect."
        Assert-True -Condition ([string]$byId["admin.han140.jiaozhou.yulin"].evidence_grade -ceq "M") -Message "Yulin estimate is not marked as model evidence."
        Assert-True -Condition ([string]$byId["admin.han140.jiaozhou.jiaozhi"].evidence_grade -ceq "M") -Message "Jiaozhi estimate is not marked as model evidence."
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

    Invoke-TestCase -Name "Yizhou commandery and dependency slice complete with retained Yongchang anomaly" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $yizhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.yizhou" })
        $yizhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.yizhou.*" })
        $households = [long](($yizhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $population = [long](($yizhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $correctedRows = @(
            $yizhouPopulation |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
                }
        )
        $commanderies = @($yizhouAdmins | Where-Object { $_.unit_type -ceq "commandery" })
        $dependencies = @($yizhouAdmins | Where-Object { $_.unit_type -ceq "other" })
        $yongchang = @($yizhouPopulation | Where-Object { $_.admin_unit_id -ceq "admin.han140.yizhou.yongchang" })

        Assert-True -Condition ($yizhouAdmins.Count -eq 12) -Message "Yizhou does not contain exactly twelve direct commandery/dependency units."
        Assert-True -Condition ($yizhouPopulation.Count -eq 12) -Message "Yizhou does not contain exactly twelve population records."
        Assert-True -Condition ($commanderies.Count -eq 9) -Message "Yizhou must contain exactly nine commanderies."
        Assert-True -Condition ($dependencies.Count -eq 3) -Message "Yizhou must contain exactly three dependency units."
        Assert-True -Condition ($households -eq 1525257) -Message "Yizhou household total is incorrect."
        Assert-True -Condition ($population -eq 7242028) -Message "Yizhou population total is incorrect."
        Assert-True -Condition ($correctedRows.Count -eq 0) -Message "Yizhou must not invent population corrections."
        Assert-True -Condition ($yongchang.Count -eq 1) -Message "Yongchang population record is missing."
        Assert-True -Condition ([long]$yongchang[0].registered_population_raw -eq 1897344) -Message "Yongchang raw population anomaly was not retained."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$yongchang[0].registered_population_corrected)) -Message "Yongchang must not have an unsupported corrected population."
    }

    Invoke-TestCase -Name "Liangzhou commandery and dependency slice preserves source gaps and corrections" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $liangzhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.liangzhou" })
        $liangzhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.liangzhou.*" })
        $rawHouseholds = [long](($liangzhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $rawPopulation = [long](($liangzhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $effectiveHouseholds = [long]0
        $effectivePopulation = [long]0
        foreach ($row in $liangzhouPopulation) {
            $effectiveHouseholds += if ([string]::IsNullOrWhiteSpace([string]$row.registered_households_corrected)) {
                [long]$row.registered_households_raw
            }
            else {
                [long]$row.registered_households_corrected
            }
            $effectivePopulation += if ([string]::IsNullOrWhiteSpace([string]$row.registered_population_corrected)) {
                if ([string]::IsNullOrWhiteSpace([string]$row.registered_population_raw)) { [long]0 } else { [long]$row.registered_population_raw }
            }
            else {
                [long]$row.registered_population_corrected
            }
        }
        $correctedRows = @(
            $liangzhouPopulation |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
                }
        )
        $commanderies = @($liangzhouAdmins | Where-Object { $_.unit_type -ceq "commandery" })
        $dependencies = @($liangzhouAdmins | Where-Object { $_.unit_type -ceq "other" })
        $jiuquan = @($liangzhouPopulation | Where-Object { $_.admin_unit_id -ceq "admin.han140.liangzhou.jiuquan" })
        $dunhuang = @($liangzhouPopulation | Where-Object { $_.admin_unit_id -ceq "admin.han140.liangzhou.dunhuang" })

        Assert-True -Condition ($liangzhouAdmins.Count -eq 12) -Message "Liangzhou does not contain exactly twelve direct commandery/dependency units."
        Assert-True -Condition ($liangzhouPopulation.Count -eq 12) -Message "Liangzhou does not contain exactly twelve population records."
        Assert-True -Condition ($commanderies.Count -eq 10) -Message "Liangzhou must contain exactly ten commanderies."
        Assert-True -Condition ($dependencies.Count -eq 2) -Message "Liangzhou must contain exactly two dependency units."
        Assert-True -Condition ($rawHouseholds -eq 102491) -Message "Liangzhou raw household total is incorrect."
        Assert-True -Condition ($rawPopulation -eq 419268) -Message "Liangzhou raw population total is incorrect."
        Assert-True -Condition ($effectiveHouseholds -eq 109491) -Message "Liangzhou effective household total is incorrect."
        Assert-True -Condition ($effectivePopulation -eq 465899) -Message "Liangzhou effective population total is incorrect."
        Assert-True -Condition ($correctedRows.Count -eq 2) -Message "Liangzhou must contain exactly two correction records."
        Assert-True -Condition ($jiuquan.Count -eq 1) -Message "Jiuquan population record is missing."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$jiuquan[0].registered_population_raw)) -Message "Jiuquan raw population must remain missing."
        Assert-True -Condition ([long]$jiuquan[0].registered_population_corrected -eq 46631) -Message "Jiuquan corrected population is incorrect."
        Assert-True -Condition ($dunhuang.Count -eq 1) -Message "Dunhuang population record is missing."
        Assert-True -Condition ([long]$dunhuang[0].registered_households_raw -eq 748) -Message "Dunhuang raw households were not retained."
        Assert-True -Condition ([long]$dunhuang[0].registered_households_corrected -eq 7748) -Message "Dunhuang corrected households are incorrect."
    }

    Invoke-TestCase -Name "Bingzhou commandery slice complete without corrections" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $bingzhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.bingzhou" })
        $bingzhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.bingzhou.*" })
        $commanderyRows = @($bingzhouAdmins | Where-Object { $_.unit_type -ceq "commandery" })
        $correctedRows = @(
            $bingzhouPopulation |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
                    -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
                }
        )
        $rawHouseholds = [long](($bingzhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $rawPopulation = [long](($bingzhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $yanmen = @($bingzhouPopulation | Where-Object { $_.admin_unit_id -ceq "admin.han140.bingzhou.yanmen" })

        Assert-True -Condition ($bingzhouAdmins.Count -eq 9) -Message "Bingzhou does not contain exactly nine direct commanderies."
        Assert-True -Condition ($bingzhouPopulation.Count -eq 9) -Message "Bingzhou does not contain exactly nine population records."
        Assert-True -Condition ($commanderyRows.Count -eq 9) -Message "Every Bingzhou direct unit must be a commandery."
        Assert-True -Condition ($rawHouseholds -eq 115011) -Message "Bingzhou household total is incorrect."
        Assert-True -Condition ($rawPopulation -eq 696765) -Message "Bingzhou population total is incorrect."
        Assert-True -Condition ($correctedRows.Count -eq 0) -Message "Bingzhou must not contain correction records."
        Assert-True -Condition ($yanmen.Count -eq 1) -Message "Yanmen population record is missing."
        Assert-True -Condition ([long]$yanmen[0].registered_population_raw -eq 249000) -Message "Yanmen raw population was not retained."
    }

    Invoke-TestCase -Name "Jiaozhou commandery slice completes P1 with explicit source gaps" -Body {
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $jiaozhouAdmins = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.jiaozhou" })
        $jiaozhouPopulation = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.jiaozhou.*" })
        $commanderyRows = @($jiaozhouAdmins | Where-Object { $_.unit_type -ceq "commandery" })
        $rawHouseholds = [long](($jiaozhouPopulation | Measure-Object -Property registered_households_raw -Sum).Sum)
        $rawPopulation = [long](($jiaozhouPopulation | Measure-Object -Property registered_population_raw -Sum).Sum)
        $effectiveHouseholds = [long]0
        $effectivePopulation = [long]0
        foreach ($row in $jiaozhouPopulation) {
            $effectiveHouseholds += if ([string]::IsNullOrWhiteSpace([string]$row.registered_households_corrected)) {
                if ([string]::IsNullOrWhiteSpace([string]$row.registered_households_raw)) { [long]0 } else { [long]$row.registered_households_raw }
            }
            else {
                [long]$row.registered_households_corrected
            }
            $effectivePopulation += if ([string]::IsNullOrWhiteSpace([string]$row.registered_population_corrected)) {
                if ([string]::IsNullOrWhiteSpace([string]$row.registered_population_raw)) { [long]0 } else { [long]$row.registered_population_raw }
            }
            else {
                [long]$row.registered_population_corrected
            }
        }
        $estimatedRows = @($jiaozhouPopulation | Where-Object { $_.evidence_grade -ceq "M" })
        $yulin = @($jiaozhouPopulation | Where-Object { $_.admin_unit_id -ceq "admin.han140.jiaozhou.yulin" })
        $jiaozhi = @($jiaozhouPopulation | Where-Object { $_.admin_unit_id -ceq "admin.han140.jiaozhou.jiaozhi" })

        Assert-True -Condition ($jiaozhouAdmins.Count -eq 7) -Message "Jiaozhou does not contain exactly seven direct commanderies."
        Assert-True -Condition ($jiaozhouPopulation.Count -eq 7) -Message "Jiaozhou does not contain exactly seven population records."
        Assert-True -Condition ($commanderyRows.Count -eq 7) -Message "Every Jiaozhou direct unit must be a commandery."
        Assert-True -Condition ($populationRows.Count -eq 105) -Message "P1 does not contain exactly 105 population records."
        Assert-True -Condition ($rawHouseholds -eq 270769) -Message "Jiaozhou raw household total is incorrect."
        Assert-True -Condition ($rawPopulation -eq 1114444) -Message "Jiaozhou raw population total is incorrect."
        Assert-True -Condition ($effectiveHouseholds -eq 412600) -Message "Jiaozhou effective household total is incorrect."
        Assert-True -Condition ($effectivePopulation -eq 2066166) -Message "Jiaozhou effective population total is incorrect."
        Assert-True -Condition ($estimatedRows.Count -eq 2) -Message "Jiaozhou must contain exactly two model-estimated records."
        Assert-True -Condition ($yulin.Count -eq 1 -and $jiaozhi.Count -eq 1) -Message "Yulin or Jiaozhi population record is missing."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$yulin[0].registered_households_raw)) -Message "Yulin raw households must remain missing."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$yulin[0].registered_population_raw)) -Message "Yulin raw population must remain missing."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$jiaozhi[0].registered_households_raw)) -Message "Jiaozhi raw households must remain missing."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$jiaozhi[0].registered_population_raw)) -Message "Jiaozhi raw population must remain missing."
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

    Invoke-TestCase -Name "prototype corridor stable geography preserves hierarchy and population weights" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.north.china.hebei",
            "geo.region.north.china.hebei.centralsoutheastplain",
            "geo.region.north.china.hebei.centralwestplain",
            "geo.region.north.china.hebei.northwestplain",
            "geo.region.north.china.hebei.southcentralplain",
            "geo.region.north.china.hebei.southwestzhangheplain"
        )
        $expectedSourceIds = @(
            "admin.han140.jizhou.anping",
            "admin.han140.jizhou.julu",
            "admin.han140.jizhou.wei",
            "admin.han140.jizhou.zhongshan",
            "admin.han140.youzhou.zhuo"
        )
        $firstRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $firstMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($firstRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($firstMappingRows.source_id | Sort-Object)
        $parentRows = @($regionRows | Where-Object { $_.stable_region_id -ceq "geo.region.north.china.hebei" })

        Assert-True -Condition ($firstRegionRows.Count -eq 6) -Message "Prototype corridor slice must contain exactly six stable regions."
        Assert-True -Condition ($firstMappingRows.Count -eq 5) -Message "Prototype corridor slice must contain exactly five region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Stable region IDs do not match the prototype corridor contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Mapped administrative sources do not match the prototype corridor contract."
        Assert-True -Condition ($parentRows.Count -eq 1) -Message "Hebei macroregion parent is missing."
        foreach ($region in $firstRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $firstMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Hebei contiguous geography second batch preserves hierarchy and population weights" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.north.china.hebei.centraleastplain",
            "geo.region.north.china.hebei.centralfoothill",
            "geo.region.north.china.hebei.northcentralplain",
            "geo.region.north.china.hebei.southeastplain",
            "geo.region.north.china.hebei.southwesttaihangplain"
        )
        $expectedSourceIds = @(
            "admin.han140.jizhou.changshan",
            "admin.han140.jizhou.hejian",
            "admin.han140.jizhou.qinghe",
            "admin.han140.jizhou.zhao",
            "admin.han140.youzhou.guangyang"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)

        Assert-True -Condition ($batchRegionRows.Count -eq 5) -Message "P2 second batch must contain exactly five new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 5) -Message "P2 second batch must contain exactly five new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Second-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Second-batch administrative sources do not match the contract."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.parent_stable_region_id -ceq "geo.region.north.china.hebei") -Message "Stable region '$($region.stable_region_id)' has an unexpected parent."
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Bohai Yanshan Liaoxi corridor third batch preserves hierarchy and population weights" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.north.china.hebei.eastcoastalplain",
            "geo.region.north.china.yanshanliaoxi",
            "geo.region.north.china.yanshanliaoxi.centralfoothillplain",
            "geo.region.north.china.yanshanliaoxi.northeastcorridor",
            "geo.region.north.china.yanshanliaoxi.southwestplain"
        )
        $expectedSourceIds = @(
            "admin.han140.jizhou.bohai",
            "admin.han140.youzhou.liaoxi",
            "admin.han140.youzhou.youbeiping",
            "admin.han140.youzhou.yuyang"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $macroRows = @($regionRows | Where-Object { $_.stable_region_id -ceq "geo.region.north.china.yanshanliaoxi" })
        $yanshanChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.north.china.yanshanliaoxi" })
        $bohaiRows = @($regionRows | Where-Object { $_.stable_region_id -ceq "geo.region.north.china.hebei.eastcoastalplain" })
        $jizhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.jizhou.*" })

        Assert-True -Condition ($batchRegionRows.Count -eq 5) -Message "P2 third batch must contain exactly five new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 4) -Message "P2 third batch must contain exactly four new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Third-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Third-batch administrative sources do not match the contract."
        Assert-True -Condition ($macroRows.Count -eq 1) -Message "Yanshan-Liaoxi macroregion is missing."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$macroRows[0].parent_stable_region_id)) -Message "Yanshan-Liaoxi macroregion must be a root region."
        Assert-True -Condition ($yanshanChildRows.Count -eq 3) -Message "Yanshan-Liaoxi macroregion must contain exactly three direct children in this batch."
        Assert-True -Condition ($bohaiRows.Count -eq 1 -and [string]$bohaiRows[0].parent_stable_region_id -ceq "geo.region.north.china.hebei") -Message "Bohai stable region must extend the Hebei macroregion."
        Assert-True -Condition ($jizhouMappingRows.Count -eq 9) -Message "All nine Jizhou commandery/state population sources must be mapped after the third batch."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Youzhou north and northeast completion batch preserves hierarchy weights and source gaps" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.north.china.yanbeigreatwall",
            "geo.region.north.china.yanbeigreatwall.easternmountainbasin",
            "geo.region.north.china.yanbeigreatwall.westernbasin",
            "geo.region.northeast.asia.liaodongkoreanorth",
            "geo.region.northeast.asia.liaodongkoreanorth.easternmountainfrontier",
            "geo.region.northeast.asia.liaodongkoreanorth.koreanorthwestplain",
            "geo.region.northeast.asia.liaodongkoreanorth.liaoheriverplain",
            "geo.region.northeast.asia.liaodongkoreanorth.westernfrontier"
        )
        $expectedSourceIds = @(
            "admin.han140.youzhou.dai",
            "admin.han140.youzhou.lelang",
            "admin.han140.youzhou.liaodong",
            "admin.han140.youzhou.liaodongshuguo",
            "admin.han140.youzhou.shanggu",
            "admin.han140.youzhou.xuantu"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $yanbeiMacroRows = @($regionRows | Where-Object { $_.stable_region_id -ceq "geo.region.north.china.yanbeigreatwall" })
        $yanbeiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.north.china.yanbeigreatwall" })
        $northeastMacroRows = @($regionRows | Where-Object { $_.stable_region_id -ceq "geo.region.northeast.asia.liaodongkoreanorth" })
        $northeastChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.northeast.asia.liaodongkoreanorth" })
        $youzhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.youzhou.*" })
        $dependencyPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.youzhou.liaodongshuguo" })

        Assert-True -Condition ($batchRegionRows.Count -eq 8) -Message "P2 fourth batch must contain exactly eight new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 6) -Message "P2 fourth batch must contain exactly six new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Fourth-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Fourth-batch administrative sources do not match the contract."
        Assert-True -Condition ($yanbeiMacroRows.Count -eq 1 -and [string]::IsNullOrWhiteSpace([string]$yanbeiMacroRows[0].parent_stable_region_id)) -Message "Yanbei-Great-Wall macroregion must be a root region."
        Assert-True -Condition ($yanbeiChildRows.Count -eq 2) -Message "Yanbei-Great-Wall macroregion must contain exactly two direct children."
        Assert-True -Condition ($northeastMacroRows.Count -eq 1 -and [string]::IsNullOrWhiteSpace([string]$northeastMacroRows[0].parent_stable_region_id)) -Message "Liaodong-Korea-north macroregion must be a root region."
        Assert-True -Condition ($northeastChildRows.Count -eq 4) -Message "Liaodong-Korea-north macroregion must contain exactly four direct children."
        Assert-True -Condition ($youzhouMappingRows.Count -eq 11) -Message "All eleven Youzhou commandery/dependency population sources must be mapped after the fourth batch."
        Assert-True -Condition ($dependencyPopulationRows.Count -eq 1) -Message "Liaodong dependency population record is missing."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$dependencyPopulationRows[0].registered_households_raw)) -Message "Liaodong dependency raw households must remain missing."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$dependencyPopulationRows[0].registered_population_raw)) -Message "Liaodong dependency raw population must remain missing."
        Assert-True -Condition ([string]$dependencyPopulationRows[0].evidence_grade -ceq "M") -Message "Liaodong dependency estimate must remain model evidence."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Sili Heluo Hedong Guanzhong skeleton preserves hierarchy weights and source readings" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.central.china.heluo",
            "geo.region.central.china.heluo.luoyangbasin",
            "geo.region.central.china.heluo.northyellowriverplain",
            "geo.region.central.china.heluo.westyellowrivercorridor",
            "geo.region.north.china.southfenheyellowriver",
            "geo.region.north.china.southfenheyellowriver.centralbasin",
            "geo.region.northwest.china.guanzhong",
            "geo.region.northwest.china.guanzhong.centralweiriverplain",
            "geo.region.northwest.china.guanzhong.easternweiriverplain",
            "geo.region.northwest.china.guanzhong.westernweiriverplain"
        )
        $expectedSourceIds = @(
            "admin.han140.sili.hedong",
            "admin.han140.sili.henei",
            "admin.han140.sili.henan",
            "admin.han140.sili.hongnong",
            "admin.han140.sili.jingzhao",
            "admin.han140.sili.youfufeng",
            "admin.han140.sili.zuopingyi"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $heluoChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.central.china.heluo" })
        $hedongChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.north.china.southfenheyellowriver" })
        $guanzhongChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.northwest.china.guanzhong" })
        $siliMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.sili.*" })
        $henanPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.sili.henan" })
        $hongnongPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.sili.hongnong" })

        Assert-True -Condition ($batchRegionRows.Count -eq 10) -Message "P2 fifth batch must contain exactly ten new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 7) -Message "P2 fifth batch must contain exactly seven new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Fifth-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Fifth-batch administrative sources do not match the contract."
        Assert-True -Condition ($heluoChildRows.Count -eq 3) -Message "Heluo macroregion must contain exactly three direct children."
        Assert-True -Condition ($hedongChildRows.Count -eq 1) -Message "South-Fenhe-Yellow-River macroregion must contain exactly one direct child."
        Assert-True -Condition ($guanzhongChildRows.Count -eq 3) -Message "Guanzhong macroregion must contain exactly three direct children."
        Assert-True -Condition ($siliMappingRows.Count -eq 7) -Message "All seven Sili commandery/metropolitan population sources must be mapped after the fifth batch."
        Assert-True -Condition ($henanPopulationRows.Count -eq 1 -and [long]$henanPopulationRows[0].registered_population_raw -eq 1010827) -Message "Henan metropolitan raw population must preserve the volume 29 reading."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$henanPopulationRows[0].registered_population_corrected)) -Message "Henan metropolitan ODS variant must not become a correction."
        Assert-True -Condition ($hongnongPopulationRows.Count -eq 1 -and [long]$hongnongPopulationRows[0].registered_population_raw -eq 199113) -Message "Hongnong raw population must preserve the volume 29 reading."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$hongnongPopulationRows[0].registered_population_corrected)) -Message "Hongnong ODS variant must not become a correction."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Yuzhou central plains Huaiying Siyi skeleton preserves hierarchy weights and paired corrections" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.central.china.yingruhuai",
            "geo.region.central.china.yingruhuai.northwestplain",
            "geo.region.central.china.yingruhuai.centralplain",
            "geo.region.central.china.yingruhuai.southplain",
            "geo.region.central.china.suihuainorth",
            "geo.region.central.china.suihuainorth.northwestplain",
            "geo.region.central.china.suihuainorth.southeastplain",
            "geo.region.east.china.siyifoothill",
            "geo.region.east.china.siyifoothill.westernplain"
        )
        $expectedSourceIds = @(
            "admin.han140.yuzhou.chen",
            "admin.han140.yuzhou.liang",
            "admin.han140.yuzhou.lu",
            "admin.han140.yuzhou.pei",
            "admin.han140.yuzhou.runan",
            "admin.han140.yuzhou.yingchuan"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $yingruhuaiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.central.china.yingruhuai" })
        $suihuaiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.central.china.suihuainorth" })
        $siyiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.siyifoothill" })
        $yuzhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.yuzhou.*" })
        $peiPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.yuzhou.pei" })
        $chenPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.yuzhou.chen" })

        Assert-True -Condition ($batchRegionRows.Count -eq 9) -Message "P2 sixth batch must contain exactly nine new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 6) -Message "P2 sixth batch must contain exactly six new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Sixth-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Sixth-batch administrative sources do not match the contract."
        Assert-True -Condition ($yingruhuaiChildRows.Count -eq 3) -Message "Ying-Ru-Huai macroregion must contain exactly three direct children."
        Assert-True -Condition ($suihuaiChildRows.Count -eq 2) -Message "Sui-Huai-North macroregion must contain exactly two direct children."
        Assert-True -Condition ($siyiChildRows.Count -eq 1) -Message "Si-Yi foothill macroregion must contain exactly one direct child."
        Assert-True -Condition ($yuzhouMappingRows.Count -eq 6) -Message "All six Yuzhou commandery and kingdom population sources must be mapped after the sixth batch."
        Assert-True -Condition ($peiPopulationRows.Count -eq 1 -and [long]$peiPopulationRows[0].registered_population_raw -eq 251393) -Message "Pei Kingdom raw population must preserve the volume 30 reading."
        Assert-True -Condition ([long]$peiPopulationRows[0].registered_population_corrected -eq 1251393) -Message "Pei Kingdom corrected population must preserve the paired million-digit correction."
        Assert-True -Condition ([string]$peiPopulationRows[0].correction_code -ceq "suspected_transposed_million_digit") -Message "Pei Kingdom correction code is not preserved."
        Assert-True -Condition ($chenPopulationRows.Count -eq 1 -and [long]$chenPopulationRows[0].registered_population_raw -eq 1547572) -Message "Chen Kingdom raw population must preserve the volume 30 reading."
        Assert-True -Condition ([long]$chenPopulationRows[0].registered_population_corrected -eq 547572) -Message "Chen Kingdom corrected population must preserve the paired million-digit correction."
        Assert-True -Condition ([string]$chenPopulationRows[0].correction_code -ceq "suspected_transposed_million_digit") -Message "Chen Kingdom correction code is not preserved."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Yanzhou lower Yellow Ji Wensi Taiyi skeleton preserves hierarchy weights and Taishan correction" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.central.china.loweryellowjishui",
            "geo.region.central.china.loweryellowjishui.southwestplain",
            "geo.region.central.china.loweryellowjishui.northcentralplain",
            "geo.region.central.china.loweryellowjishui.southeastplain",
            "geo.region.east.china.wensishuiriverplain",
            "geo.region.east.china.wensishuiriverplain.northplain",
            "geo.region.east.china.wensishuiriverplain.centralplain",
            "geo.region.east.china.wensishuiriverplain.westplain",
            "geo.region.east.china.taiyifoothill",
            "geo.region.east.china.taiyifoothill.centralbasin",
            "geo.region.east.china.taiyifoothill.northwestplain"
        )
        $expectedSourceIds = @(
            "admin.han140.yanzhou.chenliu",
            "admin.han140.yanzhou.dong",
            "admin.han140.yanzhou.dongping",
            "admin.han140.yanzhou.jibei",
            "admin.han140.yanzhou.jiyin",
            "admin.han140.yanzhou.rencheng",
            "admin.han140.yanzhou.shanyang",
            "admin.han140.yanzhou.taishan"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $lowerYellowChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.central.china.loweryellowjishui" })
        $wensiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.wensishuiriverplain" })
        $taiyiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.taiyifoothill" })
        $yanzhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.yanzhou.*" })
        $taishanPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.yanzhou.taishan" })

        Assert-True -Condition ($batchRegionRows.Count -eq 11) -Message "P2 seventh batch must contain exactly eleven new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 8) -Message "P2 seventh batch must contain exactly eight new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Seventh-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Seventh-batch administrative sources do not match the contract."
        Assert-True -Condition ($lowerYellowChildRows.Count -eq 3) -Message "Lower-Yellow-Ji macroregion must contain exactly three direct children."
        Assert-True -Condition ($wensiChildRows.Count -eq 3) -Message "Wen-Si river plain macroregion must contain exactly three direct children."
        Assert-True -Condition ($taiyiChildRows.Count -eq 2) -Message "Tai-Yi foothill macroregion must contain exactly two direct children."
        Assert-True -Condition ($yanzhouMappingRows.Count -eq 8) -Message "All eight Yanzhou commandery and kingdom population sources must be mapped after the seventh batch."
        Assert-True -Condition ($taishanPopulationRows.Count -eq 1 -and [long]$taishanPopulationRows[0].registered_households_raw -eq 8929) -Message "Taishan raw households must preserve the volume 31 reading."
        Assert-True -Condition ([long]$taishanPopulationRows[0].registered_households_corrected -eq 108929) -Message "Taishan corrected households must preserve the missing-leading-digit correction."
        Assert-True -Condition ([long]$taishanPopulationRows[0].registered_population_raw -eq 437317) -Message "Taishan raw population must remain unchanged."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$taishanPopulationRows[0].registered_population_corrected)) -Message "Taishan population must not receive an unrelated correction."
        Assert-True -Condition ([string]$taishanPopulationRows[0].correction_code -ceq "suspected_missing_leading_digit") -Message "Taishan correction code is not preserved."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Xuzhou Yishu Sishui Jianghuai East skeleton preserves hierarchy weights and Langya correction" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.east.china.yishuhuaihai",
            "geo.region.east.china.yishuhuaihai.northwestfoothill",
            "geo.region.east.china.yishuhuaihai.southeastcoastalplain",
            "geo.region.east.china.sishuiriverplain",
            "geo.region.east.china.sishuiriverplain.northwestplain",
            "geo.region.east.china.sishuiriverplain.southeastplain",
            "geo.region.east.china.jianghuaieast",
            "geo.region.east.china.jianghuaieast.centralplain"
        )
        $expectedSourceIds = @(
            "admin.han140.xuzhou.donghai",
            "admin.han140.xuzhou.guangling",
            "admin.han140.xuzhou.langya",
            "admin.han140.xuzhou.pengcheng",
            "admin.han140.xuzhou.xiapi"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $yishuChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.yishuhuaihai" })
        $sishuiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.sishuiriverplain" })
        $jianghuaiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.jianghuaieast" })
        $xuzhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.xuzhou.*" })
        $langyaPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.xuzhou.langya" })

        Assert-True -Condition ($batchRegionRows.Count -eq 8) -Message "P2 eighth batch must contain exactly eight new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 5) -Message "P2 eighth batch must contain exactly five new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Eighth-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Eighth-batch administrative sources do not match the contract."
        Assert-True -Condition ($yishuChildRows.Count -eq 2) -Message "Yishu-Huaihai macroregion must contain exactly two direct children."
        Assert-True -Condition ($sishuiChildRows.Count -eq 2) -Message "Sishui river plain macroregion must contain exactly two direct children."
        Assert-True -Condition ($jianghuaiChildRows.Count -eq 1) -Message "Jianghuai East macroregion must contain exactly one direct child."
        Assert-True -Condition ($xuzhouMappingRows.Count -eq 5) -Message "All five Xuzhou commandery and kingdom population sources must be mapped after the eighth batch."
        Assert-True -Condition ($langyaPopulationRows.Count -eq 1 -and [long]$langyaPopulationRows[0].registered_households_raw -eq 20804) -Message "Langya raw households must preserve the volume 31 reading."
        Assert-True -Condition ([long]$langyaPopulationRows[0].registered_households_corrected -eq 120804) -Message "Langya corrected households must preserve the missing-leading-digit correction."
        Assert-True -Condition ([long]$langyaPopulationRows[0].registered_population_raw -eq 570967) -Message "Langya raw population must remain unchanged."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$langyaPopulationRows[0].registered_population_corrected)) -Message "Langya population must not receive an unrelated correction."
        Assert-True -Condition ([string]$langyaPopulationRows[0].correction_code -ceq "suspected_missing_leading_digit") -Message "Langya correction code is not preserved."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Qingzhou lower Yellow Ji Ziwei Jiaolai Jiaodong skeleton preserves hierarchy weights and Jinan variant" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $adminText = Get-Content -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv") -Raw -Encoding UTF8
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.east.china.loweryellowjieastplain",
            "geo.region.east.china.loweryellowjieastplain.northwestplain",
            "geo.region.east.china.loweryellowjieastplain.southwestfoothillplain",
            "geo.region.east.china.loweryellowjieastplain.northeastcoastalplain",
            "geo.region.east.china.ziweijiaolaiplain",
            "geo.region.east.china.ziweijiaolaiplain.westernfoothillplain",
            "geo.region.east.china.ziweijiaolaiplain.easternplain",
            "geo.region.east.china.jiaodongpeninsula",
            "geo.region.east.china.jiaodongpeninsula.northcoastalhills"
        )
        $expectedSourceIds = @(
            "admin.han140.qingzhou.jinan",
            "admin.han140.qingzhou.pingyuan",
            "admin.han140.qingzhou.lean",
            "admin.han140.qingzhou.beihai",
            "admin.han140.qingzhou.donglai",
            "admin.han140.qingzhou.qi"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $lowerYellowJiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.loweryellowjieastplain" })
        $ziweiJiaolaiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.ziweijiaolaiplain" })
        $jiaodongChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.jiaodongpeninsula" })
        $qingzhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.qingzhou.*" })
        $qingzhouPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.qingzhou.*" })
        $jinanAdminRows = @($adminRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.qingzhou.jinan" })
        $jinanCommanderyVariant = [string][char]0x6D4E + [char]0x5357 + [char]0x90E1
        $correctedQingzhouRows = @($qingzhouPopulationRows | Where-Object {
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.correction_code)
        })

        Assert-True -Condition ($batchRegionRows.Count -eq 9) -Message "P2 ninth batch must contain exactly nine new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 6) -Message "P2 ninth batch must contain exactly six new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Ninth-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Ninth-batch administrative sources do not match the contract."
        Assert-True -Condition ($lowerYellowJiChildRows.Count -eq 3) -Message "Lower-Yellow-Ji East macroregion must contain exactly three direct children."
        Assert-True -Condition ($ziweiJiaolaiChildRows.Count -eq 2) -Message "Ziwei-Jiaolai macroregion must contain exactly two direct children."
        Assert-True -Condition ($jiaodongChildRows.Count -eq 1) -Message "Jiaodong Peninsula macroregion must contain exactly one direct child."
        Assert-True -Condition ($qingzhouMappingRows.Count -eq 6) -Message "All six Qingzhou commandery and kingdom population sources must be mapped after the ninth batch."
        Assert-True -Condition ($qingzhouPopulationRows.Count -eq 6) -Message "Qingzhou must retain exactly six population records."
        Assert-True -Condition ($correctedQingzhouRows.Count -eq 0) -Message "Qingzhou must not turn the Jinan type variant into a population correction."
        Assert-True -Condition ($jinanAdminRows.Count -eq 1) -Message "Jinan administrative record is missing or duplicated."
        Assert-True -Condition ([string]$jinanAdminRows[0].unit_type -ceq "kingdom") -Message "Jinan must remain a kingdom in the 140 administrative slice."
        Assert-True -Condition ([string]$jinanAdminRows[0].confidence -ceq "medium") -Message "Jinan administrative type confidence must remain medium."
        Assert-True -Condition ($adminText.Contains($jinanCommanderyVariant)) -Message "Jinan commandery transcription variant must remain auditable."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Jingzhou Hanjiang Jianghan Dongting Nanling skeleton preserves hierarchy weights and source readings" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.central.china.hanjianguppermiddle",
            "geo.region.central.china.hanjianguppermiddle.northeastbasin",
            "geo.region.central.china.jianghanplain",
            "geo.region.central.china.jianghanplain.westernplain",
            "geo.region.central.china.jianghanplain.easternriverlake",
            "geo.region.south.china.dongtingxiangziyuanli",
            "geo.region.south.china.dongtingxiangziyuanli.northwestbasin",
            "geo.region.south.china.dongtingxiangziyuanli.northeastplain",
            "geo.region.south.china.nanlingnorth",
            "geo.region.south.china.nanlingnorth.southwestbasin",
            "geo.region.south.china.nanlingnorth.southeastfoothill"
        )
        $expectedSourceIds = @(
            "admin.han140.jingzhou.nanyang",
            "admin.han140.jingzhou.nan",
            "admin.han140.jingzhou.jiangxia",
            "admin.han140.jingzhou.wuling",
            "admin.han140.jingzhou.changsha",
            "admin.han140.jingzhou.lingling",
            "admin.han140.jingzhou.guiyang"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $hanjiangChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.central.china.hanjianguppermiddle" })
        $jianghanChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.central.china.jianghanplain" })
        $dongtingChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.south.china.dongtingxiangziyuanli" })
        $nanlingChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.south.china.nanlingnorth" })
        $jingzhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.jingzhou.*" })
        $jingzhouPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.jingzhou.*" })
        $correctedJingzhouRows = @($jingzhouPopulationRows | Where-Object {
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.correction_code)
        })
        $households = ($jingzhouPopulationRows | Measure-Object -Property registered_households_raw -Sum).Sum
        $population = ($jingzhouPopulationRows | Measure-Object -Property registered_population_raw -Sum).Sum

        Assert-True -Condition ($batchRegionRows.Count -eq 11) -Message "P2 tenth batch must contain exactly eleven new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 7) -Message "P2 tenth batch must contain exactly seven new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Tenth-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Tenth-batch administrative sources do not match the contract."
        Assert-True -Condition ($hanjiangChildRows.Count -eq 1) -Message "Hanjiang upper-middle macroregion must contain exactly one direct child."
        Assert-True -Condition ($jianghanChildRows.Count -eq 2) -Message "Jianghan Plain macroregion must contain exactly two direct children."
        Assert-True -Condition ($dongtingChildRows.Count -eq 2) -Message "Dongting-Xiang-Zi-Yuan-Li macroregion must contain exactly two direct children."
        Assert-True -Condition ($nanlingChildRows.Count -eq 2) -Message "Nanling North macroregion must contain exactly two direct children."
        Assert-True -Condition ($jingzhouMappingRows.Count -eq 7) -Message "All seven Jingzhou commandery population sources must be mapped after the tenth batch."
        Assert-True -Condition ($jingzhouPopulationRows.Count -eq 7) -Message "Jingzhou must retain exactly seven population records."
        Assert-True -Condition ([long]$households -eq 1399394) -Message "Jingzhou raw household total is incorrect."
        Assert-True -Condition ([long]$population -eq 6265952) -Message "Jingzhou raw population total is incorrect."
        Assert-True -Condition ($correctedJingzhouRows.Count -eq 0) -Message "Jingzhou must not invent population corrections."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Yangzhou Huainan lower Yangtze Taihu Qiantang Gan-Poyang skeleton preserves hierarchy weights and source readings" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.east.china.huainanyangtzenorth",
            "geo.region.east.china.huainanyangtzenorth.northcentralplain",
            "geo.region.east.china.huainanyangtzenorth.southwestfoothillriver",
            "geo.region.east.china.loweryangtzetaihu",
            "geo.region.east.china.loweryangtzetaihu.westernriverhills",
            "geo.region.east.china.loweryangtzetaihu.easterntaihuplain",
            "geo.region.southeast.china.qiantangzhejianghills",
            "geo.region.southeast.china.qiantangzhejianghills.eastcoastalriverhills",
            "geo.region.southeast.china.ganpoyang",
            "geo.region.southeast.china.ganpoyang.centralriverlakebasin"
        )
        $expectedSourceIds = @(
            "admin.han140.yangzhou.jiujiang",
            "admin.han140.yangzhou.lujiang",
            "admin.han140.yangzhou.danyang",
            "admin.han140.yangzhou.wu",
            "admin.han140.yangzhou.kuaiji",
            "admin.han140.yangzhou.yuzhang"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $huainanChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.huainanyangtzenorth" })
        $lowerYangtzeChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.east.china.loweryangtzetaihu" })
        $qiantangChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.southeast.china.qiantangzhejianghills" })
        $ganPoyangChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.southeast.china.ganpoyang" })
        $yangzhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.yangzhou.*" })
        $yangzhouPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.yangzhou.*" })
        $correctedYangzhouRows = @($yangzhouPopulationRows | Where-Object {
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.correction_code)
        })
        $households = ($yangzhouPopulationRows | Measure-Object -Property registered_households_raw -Sum).Sum
        $population = ($yangzhouPopulationRows | Measure-Object -Property registered_population_raw -Sum).Sum

        Assert-True -Condition ($batchRegionRows.Count -eq 10) -Message "P2 eleventh batch must contain exactly ten new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 6) -Message "P2 eleventh batch must contain exactly six new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Eleventh-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Eleventh-batch administrative sources do not match the contract."
        Assert-True -Condition ($huainanChildRows.Count -eq 2) -Message "Huainan-Yangtze North macroregion must contain exactly two direct children."
        Assert-True -Condition ($lowerYangtzeChildRows.Count -eq 2) -Message "Lower-Yangtze-Taihu macroregion must contain exactly two direct children."
        Assert-True -Condition ($qiantangChildRows.Count -eq 1) -Message "Qiantang-Zhejiang Hills macroregion must contain exactly one direct child."
        Assert-True -Condition ($ganPoyangChildRows.Count -eq 1) -Message "Gan-Poyang macroregion must contain exactly one direct child."
        Assert-True -Condition ($yangzhouMappingRows.Count -eq 6) -Message "All six Yangzhou commandery population sources must be mapped after the eleventh batch."
        Assert-True -Condition ($yangzhouPopulationRows.Count -eq 6) -Message "Yangzhou must retain exactly six population records."
        Assert-True -Condition ([long]$households -eq 1021096) -Message "Yangzhou raw household total is incorrect."
        Assert-True -Condition ([long]$population -eq 4338538) -Message "Yangzhou raw population total is incorrect."
        Assert-True -Condition ($correctedYangzhouRows.Count -eq 0) -Message "Yangzhou must not invent population corrections."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Yizhou Hanzhong Sichuan western mountains Yungui Hengduan skeleton preserves hierarchy weights and source readings" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.southwest.china.hanzhongqinba",
            "geo.region.southwest.china.hanzhongqinba.centralhanriverbasin",
            "geo.region.southwest.china.sichuanbasin",
            "geo.region.southwest.china.sichuanbasin.northwestchengduplain",
            "geo.region.southwest.china.sichuanbasin.centralchengduplain",
            "geo.region.southwest.china.sichuanbasin.easternfoldbasin",
            "geo.region.southwest.china.sichuanbasin.southernriverhills",
            "geo.region.southwest.china.westernsichuanmountains",
            "geo.region.southwest.china.westernsichuanmountains.northqiangcorridor",
            "geo.region.southwest.china.westernsichuanmountains.centralplateaucorridor",
            "geo.region.southwest.china.westernsichuanmountains.southmountaincorridor",
            "geo.region.southwest.china.yunguiplateau",
            "geo.region.southwest.china.yunguiplateau.northeastkarstplateau",
            "geo.region.southwest.china.yunguiplateau.centralyunnanbasin",
            "geo.region.southwest.china.hengduansouth",
            "geo.region.southwest.china.hengduansouth.northeastanningvalley",
            "geo.region.southwest.china.hengduansouth.southwestlancangfrontier"
        )
        $expectedSourceIds = @(
            "admin.han140.yizhou.hanzhong",
            "admin.han140.yizhou.ba",
            "admin.han140.yizhou.guanghan",
            "admin.han140.yizhou.shu",
            "admin.han140.yizhou.jianwei",
            "admin.han140.yizhou.zangke",
            "admin.han140.yizhou.yuexi",
            "admin.han140.yizhou.yizhou",
            "admin.han140.yizhou.yongchang",
            "admin.han140.yizhou.guanghanshuguo",
            "admin.han140.yizhou.shushuguo",
            "admin.han140.yizhou.jianweishuguo"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $macroRegionRows = @($regionRows | Where-Object { $_.region_type -ceq "macroregion" })
        $commanderyAreaRows = @($regionRows | Where-Object { $_.region_type -ceq "commandery_area" })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $hanzhongChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.southwest.china.hanzhongqinba" })
        $sichuanChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.southwest.china.sichuanbasin" })
        $westernMountainChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.southwest.china.westernsichuanmountains" })
        $yunguiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.southwest.china.yunguiplateau" })
        $hengduanChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.southwest.china.hengduansouth" })
        $yizhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.yizhou.*" })
        $yizhouPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.yizhou.*" })
        $yizhouAdminRows = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.yizhou" })
        $commanderyAdminRows = @($yizhouAdminRows | Where-Object { $_.unit_type -ceq "commandery" })
        $dependencyAdminRows = @($yizhouAdminRows | Where-Object { $_.unit_type -ceq "other" })
        $yongchangRows = @($yizhouPopulationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.yizhou.yongchang" })
        $correctedYizhouRows = @($yizhouPopulationRows | Where-Object {
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.correction_code)
        })
        $households = ($yizhouPopulationRows | Measure-Object -Property registered_households_raw -Sum).Sum
        $population = ($yizhouPopulationRows | Measure-Object -Property registered_population_raw -Sum).Sum

        Assert-True -Condition ($batchRegionRows.Count -eq 17) -Message "P2 twelfth batch must contain exactly seventeen new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 12) -Message "P2 twelfth batch must contain exactly twelve new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Twelfth-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Twelfth-batch administrative sources do not match the contract."
        Assert-True -Condition ($hanzhongChildRows.Count -eq 1) -Message "Hanzhong-Qinba macroregion must contain exactly one direct child."
        Assert-True -Condition ($sichuanChildRows.Count -eq 4) -Message "Sichuan Basin macroregion must contain exactly four direct children."
        Assert-True -Condition ($westernMountainChildRows.Count -eq 3) -Message "Western Sichuan Mountains macroregion must contain exactly three direct children."
        Assert-True -Condition ($yunguiChildRows.Count -eq 2) -Message "Yungui Plateau macroregion must contain exactly two direct children."
        Assert-True -Condition ($hengduanChildRows.Count -eq 2) -Message "Hengduan South macroregion must contain exactly two direct children."
        Assert-True -Condition ($yizhouMappingRows.Count -eq 12) -Message "All twelve Yizhou population sources must be mapped after the twelfth batch."
        Assert-True -Condition ($yizhouPopulationRows.Count -eq 12) -Message "Yizhou must retain exactly twelve population records."
        Assert-True -Condition ($commanderyAdminRows.Count -eq 9) -Message "Yizhou must retain nine commandery administrative sources."
        Assert-True -Condition ($dependencyAdminRows.Count -eq 3) -Message "Yizhou must retain three dependency administrative sources."
        Assert-True -Condition ([long]$households -eq 1525257) -Message "Yizhou raw household total is incorrect."
        Assert-True -Condition ([long]$population -eq 7242028) -Message "Yizhou raw population total is incorrect."
        Assert-True -Condition ($correctedYizhouRows.Count -eq 0) -Message "Yizhou must not invent population corrections."
        Assert-True -Condition ($yongchangRows.Count -eq 1 -and [long]$yongchangRows[0].registered_households_raw -eq 231897) -Message "Yongchang raw households were not preserved."
        Assert-True -Condition ([long]$yongchangRows[0].registered_population_raw -eq 1897344) -Message "Yongchang raw population anomaly was not preserved."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$yongchangRows[0].registered_population_corrected)) -Message "Yongchang must not receive an unsupported correction."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Liangzhou Longyou Hehuang Hexi Juyan skeleton preserves hierarchy weights gaps and corrections" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.northwest.china.longyouweishui",
            "geo.region.northwest.china.longyouweishui.centraltaoweivalleys",
            "geo.region.northwest.china.longyouweishui.southeastweishuibasin",
            "geo.region.northwest.china.longnanqinba",
            "geo.region.northwest.china.longnanqinba.centraljialingmountaincorridor",
            "geo.region.northwest.china.hehuangyellowupper",
            "geo.region.northwest.china.hehuangyellowupper.easternyellowriverbasin",
            "geo.region.northwest.china.loessnorthwest",
            "geo.region.northwest.china.loessnorthwest.southcentraljinghehills",
            "geo.region.northwest.china.loessnorthwest.northeastordosmargin",
            "geo.region.northwest.china.hexicorridor",
            "geo.region.northwest.china.hexicorridor.eastshiyanghebasin",
            "geo.region.northwest.china.hexicorridor.centralheiheoasis",
            "geo.region.northwest.china.hexicorridor.centralwestjiuquanoasis",
            "geo.region.northwest.china.hexicorridor.westdunhuangshulebasin",
            "geo.region.northwest.china.hexicorridor.centralnorthfrontiercorridor",
            "geo.region.northwest.china.juyanblackriverlower",
            "geo.region.northwest.china.juyanblackriverlower.northernterminaloasis"
        )
        $expectedSourceIds = @(
            "admin.han140.liangzhou.longxi",
            "admin.han140.liangzhou.hanyang",
            "admin.han140.liangzhou.wudu",
            "admin.han140.liangzhou.jincheng",
            "admin.han140.liangzhou.anding",
            "admin.han140.liangzhou.beidi",
            "admin.han140.liangzhou.wuwei",
            "admin.han140.liangzhou.zhangye",
            "admin.han140.liangzhou.jiuquan",
            "admin.han140.liangzhou.dunhuang",
            "admin.han140.liangzhou.zhangyeshuguo",
            "admin.han140.liangzhou.zhangyejuyanshuguo"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $macroRegionRows = @($regionRows | Where-Object { $_.region_type -ceq "macroregion" })
        $commanderyAreaRows = @($regionRows | Where-Object { $_.region_type -ceq "commandery_area" })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $longyouChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.northwest.china.longyouweishui" })
        $longnanChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.northwest.china.longnanqinba" })
        $hehuangChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.northwest.china.hehuangyellowupper" })
        $loessChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.northwest.china.loessnorthwest" })
        $hexiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.northwest.china.hexicorridor" })
        $juyanChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.northwest.china.juyanblackriverlower" })
        $liangzhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.liangzhou.*" })
        $liangzhouPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.liangzhou.*" })
        $liangzhouAdminRows = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.liangzhou" })
        $commanderyAdminRows = @($liangzhouAdminRows | Where-Object { $_.unit_type -ceq "commandery" })
        $dependencyAdminRows = @($liangzhouAdminRows | Where-Object { $_.unit_type -ceq "other" })
        $jiuquanRows = @($liangzhouPopulationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.liangzhou.jiuquan" })
        $dunhuangRows = @($liangzhouPopulationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.liangzhou.dunhuang" })
        $beidiAdminRows = @($liangzhouAdminRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.liangzhou.beidi" })
        $wuweiAdminRows = @($liangzhouAdminRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.liangzhou.wuwei" })
        $sikuWebTranscriptionText = -join @([char]0x56DB, [char]0x5E93, [char]0x7F51, [char]0x9875, [char]0x8F6C, [char]0x5F55)
        $publicCollationText = -join @([char]0x516C, [char]0x5F00, [char]0x6821, [char]0x52D8, [char]0x8F6C, [char]0x5F55)
        $correctedLiangzhouRows = @($liangzhouPopulationRows | Where-Object {
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected)
        })
        $rawHouseholds = [long](($liangzhouPopulationRows | Measure-Object -Property registered_households_raw -Sum).Sum)
        $rawPopulation = [long](($liangzhouPopulationRows | Measure-Object -Property registered_population_raw -Sum).Sum)
        $effectiveHouseholds = [long]0
        $effectivePopulation = [long]0
        foreach ($row in $liangzhouPopulationRows) {
            $effectiveHouseholds += if ([string]::IsNullOrWhiteSpace([string]$row.registered_households_corrected)) {
                [long]$row.registered_households_raw
            }
            else {
                [long]$row.registered_households_corrected
            }
            $effectivePopulation += if ([string]::IsNullOrWhiteSpace([string]$row.registered_population_corrected)) {
                if ([string]::IsNullOrWhiteSpace([string]$row.registered_population_raw)) { [long]0 } else { [long]$row.registered_population_raw }
            }
            else {
                [long]$row.registered_population_corrected
            }
        }

        Assert-True -Condition ($batchRegionRows.Count -eq 18) -Message "P2 thirteenth batch must contain exactly eighteen new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 12) -Message "P2 thirteenth batch must contain exactly twelve new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Thirteenth-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Thirteenth-batch administrative sources do not match the contract."
        Assert-True -Condition ($longyouChildRows.Count -eq 2) -Message "Longyou-Weishui macroregion must contain exactly two direct children."
        Assert-True -Condition ($longnanChildRows.Count -eq 1) -Message "Longnan-Qinba macroregion must contain exactly one direct child."
        Assert-True -Condition ($hehuangChildRows.Count -eq 1) -Message "Hehuang-Upper Yellow macroregion must contain exactly one direct child."
        Assert-True -Condition ($loessChildRows.Count -eq 2) -Message "Northwest Loess macroregion must contain exactly two direct children."
        Assert-True -Condition ($hexiChildRows.Count -eq 5) -Message "Hexi Corridor macroregion must contain exactly five direct children."
        Assert-True -Condition ($juyanChildRows.Count -eq 1) -Message "Juyan-Lower Black River macroregion must contain exactly one direct child."
        Assert-True -Condition ($liangzhouMappingRows.Count -eq 12) -Message "All twelve Liangzhou population sources must be mapped after the thirteenth batch."
        Assert-True -Condition ($liangzhouPopulationRows.Count -eq 12) -Message "Liangzhou must retain exactly twelve population records."
        Assert-True -Condition ($commanderyAdminRows.Count -eq 10) -Message "Liangzhou must retain ten commandery administrative sources."
        Assert-True -Condition ($dependencyAdminRows.Count -eq 2) -Message "Liangzhou must retain two dependency administrative sources."
        Assert-True -Condition ($rawHouseholds -eq 102491) -Message "Liangzhou raw household total is incorrect."
        Assert-True -Condition ($rawPopulation -eq 419268) -Message "Liangzhou raw population total is incorrect."
        Assert-True -Condition ($effectiveHouseholds -eq 109491) -Message "Liangzhou effective household total is incorrect."
        Assert-True -Condition ($effectivePopulation -eq 465899) -Message "Liangzhou effective population total is incorrect."
        Assert-True -Condition ($correctedLiangzhouRows.Count -eq 2) -Message "Liangzhou must retain exactly two correction records."
        Assert-True -Condition ($jiuquanRows.Count -eq 1 -and [string]::IsNullOrWhiteSpace([string]$jiuquanRows[0].registered_population_raw)) -Message "Jiuquan raw population gap was not preserved."
        Assert-True -Condition ([long]$jiuquanRows[0].registered_population_corrected -eq 46631) -Message "Jiuquan corrected population was not preserved."
        Assert-True -Condition ($dunhuangRows.Count -eq 1 -and [long]$dunhuangRows[0].registered_households_raw -eq 748) -Message "Dunhuang raw household anomaly was not preserved."
        Assert-True -Condition ([long]$dunhuangRows[0].registered_households_corrected -eq 7748) -Message "Dunhuang corrected households were not preserved."
        Assert-True -Condition ($beidiAdminRows.Count -eq 1 -and ([string]$beidiAdminRows[0].notes).Contains($sikuWebTranscriptionText)) -Message "Beidi transcription variant note was not preserved."
        Assert-True -Condition ($wuweiAdminRows.Count -eq 1 -and ([string]$wuweiAdminRows[0].notes).Contains($publicCollationText)) -Message "Wuwei transcription variant note was not preserved."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Bingzhou Taihang Fenhe northern Shaanxi Hetao Yinshan Yanmen skeleton preserves hierarchy weights and source readings" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.north.china.taihangshangdang",
            "geo.region.north.china.taihangshangdang.southwestbasin",
            "geo.region.north.china.fenriverluliang",
            "geo.region.north.china.fenriverluliang.northcentralbasin",
            "geo.region.north.china.fenriverluliang.westernluliangyellowvalleys",
            "geo.region.northwest.china.northshaanxiloess",
            "geo.region.northwest.china.northshaanxiloess.northeastwudinghills",
            "geo.region.north.china.hetaoyellowbend",
            "geo.region.north.china.hetaoyellowbend.northcentraloasisplain",
            "geo.region.north.china.hetaoyellowbend.southwestyellowriverplain",
            "geo.region.north.china.yinshansouth",
            "geo.region.north.china.yinshansouth.southeasttumochuanplain",
            "geo.region.north.china.yinshansouth.southcentralfoothillplain",
            "geo.region.north.china.yanmensanggan",
            "geo.region.north.china.yanmensanggan.centraldatongbasin"
        )
        $expectedSourceIds = @(
            "admin.han140.bingzhou.shangdang",
            "admin.han140.bingzhou.taiyuan",
            "admin.han140.bingzhou.shang",
            "admin.han140.bingzhou.xihe",
            "admin.han140.bingzhou.wuyuan",
            "admin.han140.bingzhou.yunzhong",
            "admin.han140.bingzhou.dingxiang",
            "admin.han140.bingzhou.yanmen",
            "admin.han140.bingzhou.shuofang"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $macroRegionRows = @($regionRows | Where-Object { $_.region_type -ceq "macroregion" })
        $commanderyAreaRows = @($regionRows | Where-Object { $_.region_type -ceq "commandery_area" })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $taihangChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.north.china.taihangshangdang" })
        $fenheChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.north.china.fenriverluliang" })
        $northShaanxiChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.northwest.china.northshaanxiloess" })
        $hetaoChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.north.china.hetaoyellowbend" })
        $yinshanChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.north.china.yinshansouth" })
        $yanmenChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.north.china.yanmensanggan" })
        $bingzhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.bingzhou.*" })
        $bingzhouPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.bingzhou.*" })
        $bingzhouAdminRows = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.bingzhou" })
        $commanderyAdminRows = @($bingzhouAdminRows | Where-Object { $_.unit_type -ceq "commandery" })
        $correctedBingzhouRows = @($bingzhouPopulationRows | Where-Object {
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_households_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.registered_population_corrected) -or
            -not [string]::IsNullOrWhiteSpace([string]$_.correction_code)
        })
        $yanmenRows = @($bingzhouPopulationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.bingzhou.yanmen" })
        $households = [long](($bingzhouPopulationRows | Measure-Object -Property registered_households_raw -Sum).Sum)
        $population = [long](($bingzhouPopulationRows | Measure-Object -Property registered_population_raw -Sum).Sum)

        Assert-True -Condition ($batchRegionRows.Count -eq 15) -Message "P2 fourteenth batch must contain exactly fifteen new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 9) -Message "P2 fourteenth batch must contain exactly nine new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Fourteenth-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Fourteenth-batch administrative sources do not match the contract."
        Assert-True -Condition ($taihangChildRows.Count -eq 1) -Message "Taihang-Shangdang macroregion must contain exactly one direct child."
        Assert-True -Condition ($fenheChildRows.Count -eq 2) -Message "Fenhe-Luliang macroregion must contain exactly two direct children."
        Assert-True -Condition ($northShaanxiChildRows.Count -eq 1) -Message "North Shaanxi Loess macroregion must contain exactly one direct child."
        Assert-True -Condition ($hetaoChildRows.Count -eq 2) -Message "Hetao-Yellow Bend macroregion must contain exactly two direct children."
        Assert-True -Condition ($yinshanChildRows.Count -eq 2) -Message "Yinshan South macroregion must contain exactly two direct children."
        Assert-True -Condition ($yanmenChildRows.Count -eq 1) -Message "Yanmen-Sanggan macroregion must contain exactly one direct child."
        Assert-True -Condition ($bingzhouMappingRows.Count -eq 9) -Message "All nine Bingzhou population sources must be mapped after the fourteenth batch."
        Assert-True -Condition ($bingzhouPopulationRows.Count -eq 9) -Message "Bingzhou must retain exactly nine population records."
        Assert-True -Condition ($bingzhouAdminRows.Count -eq 9 -and $commanderyAdminRows.Count -eq 9) -Message "Bingzhou must retain nine commandery administrative sources."
        Assert-True -Condition ($households -eq 115011) -Message "Bingzhou raw household total is incorrect."
        Assert-True -Condition ($population -eq 696765) -Message "Bingzhou raw population total is incorrect."
        Assert-True -Condition ($correctedBingzhouRows.Count -eq 0) -Message "Bingzhou must not invent population corrections."
        Assert-True -Condition ($yanmenRows.Count -eq 1 -and [long]$yanmenRows[0].registered_households_raw -eq 31862) -Message "Yanmen raw households were not preserved."
        Assert-True -Condition ([long]$yanmenRows[0].registered_population_raw -eq 249000) -Message "Yanmen raw population was not preserved."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "Jiaozhou Lingnan Pearl Beibu Red River completion batch preserves estimates and closes P2 national coverage" -Body {
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRegionIds = @(
            "geo.region.south.china.pearlriverdelta",
            "geo.region.south.china.pearlriverdelta.eastcentraldeltafoothills",
            "geo.region.south.china.xijiangyujiang",
            "geo.region.south.china.xijiangyujiang.easternxijiangbasin",
            "geo.region.south.china.xijiangyujiang.centralyujiangbasin",
            "geo.region.south.china.beibugulfcoast",
            "geo.region.south.china.beibugulfcoast.northeastcoastalplain",
            "geo.region.southeastasia.redriverdelta",
            "geo.region.southeastasia.redriverdelta.centralnortherndelta",
            "geo.region.southeastasia.northcentralvietnam",
            "geo.region.southeastasia.northcentralvietnam.northmariverbasin",
            "geo.region.southeastasia.northcentralvietnam.centralcoastalstrip"
        )
        $expectedSourceIds = @(
            "admin.han140.jiaozhou.nanhai",
            "admin.han140.jiaozhou.cangwu",
            "admin.han140.jiaozhou.yulin",
            "admin.han140.jiaozhou.hepu",
            "admin.han140.jiaozhou.jiaozhi",
            "admin.han140.jiaozhou.jiuzhen",
            "admin.han140.jiaozhou.rinan"
        )
        $batchRegionRows = @($regionRows | Where-Object { $expectedRegionIds -ccontains $_.stable_region_id })
        $batchMappingRows = @($mappingRows | Where-Object { $expectedSourceIds -ccontains $_.source_id })
        $macroRegionRows = @($regionRows | Where-Object { $_.region_type -ceq "macroregion" })
        $commanderyAreaRows = @($regionRows | Where-Object { $_.region_type -ceq "commandery_area" })
        $actualRegionIds = @($batchRegionRows.stable_region_id | Sort-Object)
        $actualSourceIds = @($batchMappingRows.source_id | Sort-Object)
        $allPopulationSourceIds = @($populationRows.admin_unit_id | Sort-Object)
        $allMappedSourceIds = @($mappingRows.source_id | Sort-Object)
        $pearlChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.south.china.pearlriverdelta" })
        $xijiangChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.south.china.xijiangyujiang" })
        $beibuChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.south.china.beibugulfcoast" })
        $redRiverChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.southeastasia.redriverdelta" })
        $northCentralVietnamChildRows = @($regionRows | Where-Object { $_.parent_stable_region_id -ceq "geo.region.southeastasia.northcentralvietnam" })
        $jiaozhouMappingRows = @($mappingRows | Where-Object { $_.source_id -clike "admin.han140.jiaozhou.*" })
        $jiaozhouPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -clike "admin.han140.jiaozhou.*" })
        $jiaozhouAdminRows = @($adminRows | Where-Object { $_.parent_admin_unit_id -ceq "admin.han140.jiaozhou" })
        $commanderyAdminRows = @($jiaozhouAdminRows | Where-Object { $_.unit_type -ceq "commandery" })
        $historicalEvidenceRows = @($jiaozhouPopulationRows | Where-Object { $_.evidence_grade -ceq "H" })
        $modelEvidenceRows = @($jiaozhouPopulationRows | Where-Object { $_.evidence_grade -ceq "M" })
        $yulinRows = @($jiaozhouPopulationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.jiaozhou.yulin" })
        $jiaozhiRows = @($jiaozhouPopulationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.jiaozhou.jiaozhi" })
        $rawHouseholds = [long](($jiaozhouPopulationRows | Measure-Object -Property registered_households_raw -Sum).Sum)
        $rawPopulation = [long](($jiaozhouPopulationRows | Measure-Object -Property registered_population_raw -Sum).Sum)
        $effectiveHouseholds = [long]0
        $effectivePopulation = [long]0
        foreach ($row in $jiaozhouPopulationRows) {
            $effectiveHouseholds += if ([string]::IsNullOrWhiteSpace([string]$row.registered_households_corrected)) {
                if ([string]::IsNullOrWhiteSpace([string]$row.registered_households_raw)) { [long]0 } else { [long]$row.registered_households_raw }
            }
            else {
                [long]$row.registered_households_corrected
            }
            $effectivePopulation += if ([string]::IsNullOrWhiteSpace([string]$row.registered_population_corrected)) {
                if ([string]::IsNullOrWhiteSpace([string]$row.registered_population_raw)) { [long]0 } else { [long]$row.registered_population_raw }
            }
            else {
                [long]$row.registered_population_corrected
            }
        }

        Assert-True -Condition ($batchRegionRows.Count -eq 12) -Message "P2 fifteenth batch must contain exactly twelve new stable regions."
        Assert-True -Condition ($batchMappingRows.Count -eq 7) -Message "P2 fifteenth batch must contain exactly seven new region mappings."
        Assert-True -Condition (($actualRegionIds -join "|") -ceq (($expectedRegionIds | Sort-Object) -join "|")) -Message "Fifteenth-batch stable region IDs do not match the contract."
        Assert-True -Condition (($actualSourceIds -join "|") -ceq (($expectedSourceIds | Sort-Object) -join "|")) -Message "Fifteenth-batch administrative sources do not match the contract."
        Assert-True -Condition (($allMappedSourceIds -join "|") -ceq ($allPopulationSourceIds -join "|")) -Message "P2 national coverage does not match all 105 population sources."
        Assert-True -Condition ($pearlChildRows.Count -eq 1) -Message "Pearl River Delta macroregion must contain exactly one direct child."
        Assert-True -Condition ($xijiangChildRows.Count -eq 2) -Message "Xijiang-Yujiang macroregion must contain exactly two direct children."
        Assert-True -Condition ($beibuChildRows.Count -eq 1) -Message "Beibu Gulf Coast macroregion must contain exactly one direct child."
        Assert-True -Condition ($redRiverChildRows.Count -eq 1) -Message "Red River Delta macroregion must contain exactly one direct child."
        Assert-True -Condition ($northCentralVietnamChildRows.Count -eq 2) -Message "North Central Vietnam macroregion must contain exactly two direct children."
        Assert-True -Condition ($jiaozhouMappingRows.Count -eq 7) -Message "All seven Jiaozhou population sources must be mapped after the fifteenth batch."
        Assert-True -Condition ($jiaozhouPopulationRows.Count -eq 7) -Message "Jiaozhou must retain exactly seven population records."
        Assert-True -Condition ($jiaozhouAdminRows.Count -eq 7 -and $commanderyAdminRows.Count -eq 7) -Message "Jiaozhou must retain seven commandery administrative sources."
        Assert-True -Condition ($historicalEvidenceRows.Count -eq 5 -and $modelEvidenceRows.Count -eq 2) -Message "Jiaozhou evidence grades must remain five H and two M."
        Assert-True -Condition ($rawHouseholds -eq 270769) -Message "Jiaozhou raw household total is incorrect."
        Assert-True -Condition ($rawPopulation -eq 1114444) -Message "Jiaozhou raw population total is incorrect."
        Assert-True -Condition ($effectiveHouseholds -eq 412600) -Message "Jiaozhou effective household total is incorrect."
        Assert-True -Condition ($effectivePopulation -eq 2066166) -Message "Jiaozhou effective population total is incorrect."
        Assert-True -Condition ($yulinRows.Count -eq 1 -and [string]::IsNullOrWhiteSpace([string]$yulinRows[0].registered_households_raw)) -Message "Yulin raw households must remain missing."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$yulinRows[0].registered_population_raw)) -Message "Yulin raw population must remain missing."
        Assert-True -Condition ([long]$yulinRows[0].registered_households_corrected -eq 12415 -and [long]$yulinRows[0].registered_population_corrected -eq 71162) -Message "Yulin M-grade estimate was not preserved."
        Assert-True -Condition ($jiaozhiRows.Count -eq 1 -and [string]::IsNullOrWhiteSpace([string]$jiaozhiRows[0].registered_households_raw)) -Message "Jiaozhi raw households must remain missing."
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$jiaozhiRows[0].registered_population_raw)) -Message "Jiaozhi raw population must remain missing."
        Assert-True -Condition ([long]$jiaozhiRows[0].registered_households_corrected -eq 129416 -and [long]$jiaozhiRows[0].registered_population_corrected -eq 880560) -Message "Jiaozhi M-grade estimate was not preserved."
        foreach ($region in $batchRegionRows) {
            Assert-True -Condition ([string]$region.geometry_status -ceq "provisional") -Message "Stable region '$($region.stable_region_id)' is not provisional geometry."
            Assert-True -Condition ([string]$region.provisional -ceq "true") -Message "Stable region '$($region.stable_region_id)' is not marked provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_latitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified latitude."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$region.centroid_longitude)) -Message "Stable region '$($region.stable_region_id)' must not contain an unverified longitude."
        }
        foreach ($mapping in $batchMappingRows) {
            Assert-True -Condition ([int]$mapping.weight_basis_points -eq 10000) -Message "Mapping for '$($mapping.source_id)' does not preserve 10000 basis points."
            Assert-True -Condition ([string]$mapping.mapping_method -ceq "single_provisional_commandery_bucket_v1") -Message "Mapping for '$($mapping.source_id)' uses an unexpected method."
            Assert-True -Condition ([string]$mapping.provisional -ceq "true") -Message "Mapping for '$($mapping.source_id)' is not marked provisional."
        }
    }

    Invoke-TestCase -Name "P3 runtime prototype and city crosswalk first batch preserves identity and population accounting" -Body {
        $sourceDocument = Get-Content -LiteralPath (Join-Path $productionDataPath "han_140_sources.json") -Raw -Encoding UTF8 | ConvertFrom-Json
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedRuntimeIds = @(
            "location.zhuo",
            "location.zhongshan",
            "location.anping",
            "location.xiaquyang",
            "location.guangzong",
            "location.ye"
        )
        $expectedPrototypeIds = @("L001", "L004", "L006", "L007", "L008", "L011")
        $expectedCityIds = @("C009", "C010", "C012")
        $firstBatchIds = @($expectedRuntimeIds + $expectedPrototypeIds + $expectedCityIds)
        $expectedCountyAdminIds = @(
            "admin.han140.youzhou.zhuo.zhuo",
            "admin.han140.jizhou.julu.xiaquyang",
            "admin.han140.jizhou.julu.guangzong",
            "admin.han140.jizhou.wei.ye"
        )
        $expectedCountyRegionIds = @(
            "geo.region.north.china.hebei.northwestplain.zhuocounty",
            "geo.region.north.china.hebei.southcentralplain.xiaquyangcounty",
            "geo.region.north.china.hebei.southcentralplain.guangzongcounty",
            "geo.region.north.china.hebei.southwestzhangheplain.yecounty"
        )
        $runtimeRows = @($crosswalkRows | Where-Object { $_.game_location_kind -ceq "runtime" })
        $firstBatchRows = @($crosswalkRows | Where-Object { $firstBatchIds -ccontains $_.game_location_id })
        $prototypeRows = @($crosswalkRows | Where-Object { $expectedPrototypeIds -ccontains $_.game_location_id })
        $cityRows = @($crosswalkRows | Where-Object { $expectedCityIds -ccontains $_.game_location_id })
        $approximateRows = @($firstBatchRows | Where-Object { $_.mapping_status -ceq "approximate" })
        $aggregateRows = @($firstBatchRows | Where-Object { $_.mapping_status -ceq "aggregate" })
        $countyAdminRows = @($adminRows | Where-Object { $expectedCountyAdminIds -ccontains $_.admin_unit_id })
        $countyRegionRows = @($regionRows | Where-Object { $expectedCountyRegionIds -ccontains $_.stable_region_id })
        $countyPopulationRows = @($populationRows | Where-Object { $expectedCountyAdminIds -ccontains $_.admin_unit_id })
        $juluPopulationRows = @($populationRows | Where-Object { $_.admin_unit_id -ceq "admin.han140.jizhou.julu" })
        $juluMappingRows = @($mappingRows | Where-Object { $_.source_id -ceq "admin.han140.jizhou.julu" })
        $sourceRows = @($sourceDocument.sources)
        $catalogSourceRows = @($sourceRows | Where-Object { $_.source_id -ceq "source.project.prototype_location_catalog.v1" })
        $byGameId = @{}
        foreach ($row in $firstBatchRows) {
            $byGameId[[string]$row.game_location_id] = $row
        }

        Assert-True -Condition ($catalogSourceRows.Count -eq 1) -Message "P3 project location catalog source is missing or duplicated."
        Assert-True -Condition ($populationRows.Count -eq 105) -Message "P3 must not add county population records."
        Assert-True -Condition ($mappingRows.Count -eq 105) -Message "P3 must not add or duplicate population mappings."
        Assert-True -Condition ($firstBatchRows.Count -eq 15) -Message "P3 first batch must contain exactly fifteen scoped crosswalk rows."
        Assert-True -Condition ($runtimeRows.Count -eq 6) -Message "All six runtime locations must be mapped."
        Assert-True -Condition ($prototypeRows.Count -eq 6) -Message "The six corresponding prototype catalog locations must be mapped."
        Assert-True -Condition ($cityRows.Count -eq 3) -Message "The three related city catalog locations must be mapped."
        Assert-True -Condition ((@($runtimeRows.game_location_id | Sort-Object) -join "|") -ceq (@($expectedRuntimeIds | Sort-Object) -join "|")) -Message "Runtime crosswalk IDs do not match the six public runtime IDs."
        Assert-True -Condition ((@($prototypeRows.game_location_id | Sort-Object) -join "|") -ceq (@($expectedPrototypeIds | Sort-Object) -join "|")) -Message "Prototype crosswalk IDs do not match the first-batch contract."
        Assert-True -Condition ((@($cityRows.game_location_id | Sort-Object) -join "|") -ceq (@($expectedCityIds | Sort-Object) -join "|")) -Message "City crosswalk IDs do not match the first-batch contract."
        Assert-True -Condition ($approximateRows.Count -eq 9 -and $aggregateRows.Count -eq 6) -Message "P3 mapping status counts must remain nine approximate and six aggregate."
        Assert-True -Condition ($countyAdminRows.Count -eq 4) -Message "All four P3 county administrative candidates must exist."
        Assert-True -Condition (@($countyAdminRows | Where-Object { $_.unit_type -cne "county" }).Count -eq 0) -Message "Every P3 county candidate must use the county unit type."
        Assert-True -Condition ($countyRegionRows.Count -eq 4) -Message "All four P3 county stable regions must exist."
        Assert-True -Condition (@($countyRegionRows | Where-Object { $_.region_type -cne "county_area" }).Count -eq 0) -Message "Every P3 county stable region must use county_area."
        Assert-True -Condition ($countyPopulationRows.Count -eq 0) -Message "County crosswalk entries must not become independent population records."
        Assert-True -Condition ($juluPopulationRows.Count -eq 1 -and $juluMappingRows.Count -eq 1) -Message "Julu population and population mapping must remain single-counted."
        Assert-True -Condition ([int]$juluMappingRows[0].weight_basis_points -eq 10000) -Message "Julu population mapping must remain exactly ten thousand basis points."

        $expectedParents = @{
            "admin.han140.youzhou.zhuo.zhuo" = "admin.han140.youzhou.zhuo"
            "admin.han140.jizhou.julu.xiaquyang" = "admin.han140.jizhou.julu"
            "admin.han140.jizhou.julu.guangzong" = "admin.han140.jizhou.julu"
            "admin.han140.jizhou.wei.ye" = "admin.han140.jizhou.wei"
        }
        foreach ($row in $countyAdminRows) {
            Assert-True -Condition ([string]$row.parent_admin_unit_id -ceq [string]$expectedParents[[string]$row.admin_unit_id]) -Message "County '$($row.admin_unit_id)' has the wrong parent."
        }

        $expectedRegionParents = @{
            "geo.region.north.china.hebei.northwestplain.zhuocounty" = "geo.region.north.china.hebei.northwestplain"
            "geo.region.north.china.hebei.southcentralplain.xiaquyangcounty" = "geo.region.north.china.hebei.southcentralplain"
            "geo.region.north.china.hebei.southcentralplain.guangzongcounty" = "geo.region.north.china.hebei.southcentralplain"
            "geo.region.north.china.hebei.southwestzhangheplain.yecounty" = "geo.region.north.china.hebei.southwestzhangheplain"
        }
        foreach ($row in $countyRegionRows) {
            Assert-True -Condition ([string]$row.parent_stable_region_id -ceq [string]$expectedRegionParents[[string]$row.stable_region_id]) -Message "County region '$($row.stable_region_id)' has the wrong parent."
            Assert-True -Condition ([string]$row.geometry_status -ceq "provisional" -and [string]$row.provisional -ceq "true") -Message "County region '$($row.stable_region_id)' must remain provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$row.centroid_latitude) -and [string]::IsNullOrWhiteSpace([string]$row.centroid_longitude)) -Message "County region '$($row.stable_region_id)' must not invent coordinates."
        }

        $aliasPairs = @(
            @("location.zhuo", "L001"),
            @("location.zhongshan", "L004"),
            @("location.anping", "L006"),
            @("location.xiaquyang", "L007"),
            @("location.guangzong", "L008"),
            @("location.ye", "L011"),
            @("location.ye", "C009"),
            @("location.zhongshan", "C012")
        )
        foreach ($pair in $aliasPairs) {
            $left = $byGameId[[string]$pair[0]]
            $right = $byGameId[[string]$pair[1]]
            Assert-True -Condition ([string]$left.stable_region_id -ceq [string]$right.stable_region_id) -Message "Crosswalk aliases '$($pair[0])' and '$($pair[1])' do not share stable geography."
            Assert-True -Condition ([string]$left.admin_unit_id -ceq [string]$right.admin_unit_id) -Message "Crosswalk aliases '$($pair[0])' and '$($pair[1])' do not share administrative identity."
        }

        Assert-True -Condition ([string]$byGameId["C010"].stable_region_id -ceq "geo.region.north.china.hebei.southcentralplain") -Message "C010 must remain a Julu commandery-area proxy."
        Assert-True -Condition ([string]$byGameId["C010"].admin_unit_id -ceq "admin.han140.jizhou.julu") -Message "C010 must reference Julu commandery rather than either county."
        foreach ($row in $firstBatchRows) {
            Assert-True -Condition ([string]$row.provisional -ceq "true") -Message "Crosswalk '$($row.game_location_id)' must remain provisional."
            Assert-True -Condition (([string]$row.source_ids).Contains("source.project.prototype_location_catalog.v1")) -Message "Crosswalk '$($row.game_location_id)' must cite the project catalog source."
            if ($row.game_location_kind -ceq "city_catalog") {
                Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$row.valid_from_year) -and [string]::IsNullOrWhiteSpace([string]$row.valid_to_year)) -Message "City catalog '$($row.game_location_id)' must not invent a fixed validity range."
            }
            else {
                Assert-True -Condition ([int]$row.valid_from_year -eq 184 -and [int]$row.valid_to_year -eq 184) -Message "Runtime and prototype mappings must be explicitly scoped to the 184 prototype."
            }
        }
    }

    Invoke-TestCase -Name "P3 remaining prototype catalog second batch preserves unresolved boundaries and completes L catalog" -Body {
        $sourceDocument = Get-Content -LiteralPath (Join-Path $productionDataPath "han_140_sources.json") -Raw -Encoding UTF8 | ConvertFrom-Json
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedSecondBatchIds = @("L002", "L003", "L005", "L009", "L010", "L012")
        $expectedAllPrototypeIds = 1..12 | ForEach-Object { "L{0:D3}" -f $_ }
        $countyAdminIds = @(
            "admin.han140.youzhou.guangyang.ji",
            "admin.han140.jizhou.julu.yingtao"
        )
        $countyRegionIds = @(
            "geo.region.north.china.hebei.northcentralplain.jicounty",
            "geo.region.north.china.hebei.southcentralplain.yingtaocounty"
        )
        $prototypeRows = @($crosswalkRows | Where-Object { $_.game_location_kind -ceq "prototype_catalog" })
        $secondBatchRows = @($crosswalkRows | Where-Object { $expectedSecondBatchIds -ccontains $_.game_location_id })
        $countyAdminRows = @($adminRows | Where-Object { $countyAdminIds -ccontains $_.admin_unit_id })
        $countyRegionRows = @($regionRows | Where-Object { $countyRegionIds -ccontains $_.stable_region_id })
        $countyPopulationRows = @($populationRows | Where-Object { $countyAdminIds -ccontains $_.admin_unit_id })
        $juluMappingRows = @($mappingRows | Where-Object { $_.source_id -ceq "admin.han140.jizhou.julu" })
        $byGameId = @{}
        foreach ($row in $secondBatchRows) {
            $byGameId[[string]$row.game_location_id] = $row
        }

        Assert-True -Condition (@($sourceDocument.sources).Count -eq 4) -Message "P3 second batch must retain four registered sources."
        Assert-True -Condition ($populationRows.Count -eq 105 -and $mappingRows.Count -eq 105) -Message "P3 second batch must not alter population record or mapping counts."
        Assert-True -Condition (@($crosswalkRows | Where-Object { $_.game_location_kind -ceq "runtime" }).Count -eq 6) -Message "Runtime crosswalk count must remain six."
        Assert-True -Condition ($prototypeRows.Count -eq 12) -Message "All twelve prototype catalog nodes must be classified."
        Assert-True -Condition ((@($prototypeRows.game_location_id | Sort-Object) -join "|") -ceq (@($expectedAllPrototypeIds | Sort-Object) -join "|")) -Message "Prototype catalog IDs must be exactly L001 through L012."
        Assert-True -Condition ($secondBatchRows.Count -eq 6) -Message "P3 second batch must contain exactly six scoped rows."
        Assert-True -Condition (@($secondBatchRows | Where-Object { $_.mapping_status -ceq "approximate" }).Count -eq 2) -Message "P3 second batch must contain two approximate mappings."
        Assert-True -Condition (@($secondBatchRows | Where-Object { $_.mapping_status -ceq "aggregate" }).Count -eq 2) -Message "P3 second batch must contain two aggregate mappings."
        Assert-True -Condition (@($secondBatchRows | Where-Object { $_.mapping_status -ceq "unresolved" }).Count -eq 2) -Message "P3 second batch must retain two explicit unresolved nodes."
        Assert-True -Condition ($countyAdminRows.Count -eq 2 -and @($countyAdminRows | Where-Object { $_.unit_type -cne "county" }).Count -eq 0) -Message "Ji and Yingtao must exist as county candidates."
        Assert-True -Condition ($countyRegionRows.Count -eq 2 -and @($countyRegionRows | Where-Object { $_.region_type -cne "county_area" }).Count -eq 0) -Message "Ji and Yingtao must have county-area stable identities."
        Assert-True -Condition ($countyPopulationRows.Count -eq 0) -Message "Second-batch counties must not become population records."
        Assert-True -Condition ($juluMappingRows.Count -eq 1 -and [int]$juluMappingRows[0].weight_basis_points -eq 10000) -Message "Julu population must remain single-counted at ten thousand basis points."

        $expectedAdminParents = @{
            "admin.han140.youzhou.guangyang.ji" = "admin.han140.youzhou.guangyang"
            "admin.han140.jizhou.julu.yingtao" = "admin.han140.jizhou.julu"
        }
        foreach ($row in $countyAdminRows) {
            Assert-True -Condition ([string]$row.parent_admin_unit_id -ceq [string]$expectedAdminParents[[string]$row.admin_unit_id]) -Message "County '$($row.admin_unit_id)' has the wrong parent."
        }
        $expectedRegionParents = @{
            "geo.region.north.china.hebei.northcentralplain.jicounty" = "geo.region.north.china.hebei.northcentralplain"
            "geo.region.north.china.hebei.southcentralplain.yingtaocounty" = "geo.region.north.china.hebei.southcentralplain"
        }
        foreach ($row in $countyRegionRows) {
            Assert-True -Condition ([string]$row.parent_stable_region_id -ceq [string]$expectedRegionParents[[string]$row.stable_region_id]) -Message "County region '$($row.stable_region_id)' has the wrong parent."
            Assert-True -Condition ([string]$row.geometry_status -ceq "provisional" -and [string]$row.provisional -ceq "true") -Message "County region '$($row.stable_region_id)' must remain provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$row.centroid_latitude) -and [string]::IsNullOrWhiteSpace([string]$row.centroid_longitude)) -Message "County region '$($row.stable_region_id)' must not invent coordinates."
        }

        Assert-True -Condition ([string]$byGameId["L002"].stable_region_id -ceq "geo.region.north.china.hebei.northcentralplain.jicounty" -and [string]$byGameId["L002"].admin_unit_id -ceq "admin.han140.youzhou.guangyang.ji") -Message "L002 must resolve to the provisional Ji county identity."
        Assert-True -Condition ([string]$byGameId["L002"].mapping_status -ceq "approximate" -and [string]$byGameId["L002"].relation_type -ceq "prototype_catalog_county_identity") -Message "L002 must use the county identity relation."
        Assert-True -Condition ([string]$byGameId["L003"].stable_region_id -ceq "geo.region.north.china.hebei.northcentralplain" -and [string]$byGameId["L003"].admin_unit_id -ceq "admin.han140.youzhou.guangyang") -Message "L003 must remain a Guangyang commandery-area proxy."
        Assert-True -Condition ([string]$byGameId["L009"].stable_region_id -ceq "geo.region.north.china.hebei.southcentralplain" -and [string]$byGameId["L009"].admin_unit_id -ceq "admin.han140.jizhou.julu") -Message "L009 must remain a Julu commandery-area proxy."
        Assert-True -Condition ([string]$byGameId["L009"].admin_unit_id -cne "admin.han140.jizhou.julu.yingtao") -Message "L009 must not be forced to equal Yingtao."
        Assert-True -Condition ([string]$byGameId["L010"].stable_region_id -ceq "geo.region.north.china.hebei.southcentralplain.yingtaocounty" -and [string]$byGameId["L010"].admin_unit_id -ceq "admin.han140.jizhou.julu.yingtao") -Message "L010 must resolve to the provisional Yingtao county identity."
        foreach ($id in @("L005", "L012")) {
            $row = $byGameId[$id]
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$row.stable_region_id) -and [string]::IsNullOrWhiteSpace([string]$row.admin_unit_id)) -Message "Unresolved node '$id' must not invent stable or administrative identity."
            Assert-True -Condition ([string]$row.mapping_status -ceq "unresolved" -and [string]$row.relation_type -ceq "prototype_catalog_unresolved") -Message "Node '$id' must remain explicitly unresolved."
            Assert-True -Condition ([string]$row.confidence -ceq "unknown" -and [string]$row.source_ids -ceq "source.project.prototype_location_catalog.v1") -Message "Unresolved node '$id' must cite only the project catalog and use unknown confidence."
        }
        foreach ($row in $secondBatchRows) {
            Assert-True -Condition ([int]$row.valid_from_year -eq 184 -and [int]$row.valid_to_year -eq 184) -Message "Second-batch node '$($row.game_location_id)' must be scoped to 184."
            Assert-True -Condition ([string]$row.provisional -ceq "true") -Message "Second-batch node '$($row.game_location_id)' must remain provisional."
        }
    }

    Invoke-TestCase -Name "P3 northern city catalog first batch maps county identities without duplicating population" -Body {
        $sourceDocument = Get-Content -LiteralPath (Join-Path $productionDataPath "han_140_sources.json") -Raw -Encoding UTF8 | ConvertFrom-Json
        $adminRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_administrative_units.csv"))
        $populationRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_population_records.csv"))
        $regionRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "stable_population_regions.csv"))
        $mappingRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "han_140_region_mapping.csv"))
        $crosswalkRows = @(Import-Csv -LiteralPath (Join-Path $productionDataPath "game_location_crosswalk.csv"))
        $expectedBatchIds = @("C001", "C002", "C003", "C004", "C005", "C006", "C007", "C008", "C011", "C013")
        $expectedAllCityIds = 1..13 | ForEach-Object { "C{0:D3}" -f $_ }
        $adminParents = @{
            "admin.han140.youzhou.liaodong.xiangping" = "admin.han140.youzhou.liaodong"
            "admin.han140.youzhou.lelang.chaoxian" = "admin.han140.youzhou.lelang"
            "admin.han140.youzhou.youbeiping.tuyin" = "admin.han140.youzhou.youbeiping"
            "admin.han140.bingzhou.taiyuan.jinyang" = "admin.han140.bingzhou.taiyuan"
            "admin.han140.bingzhou.shangdang.huguan" = "admin.han140.bingzhou.shangdang"
            "admin.han140.jizhou.bohai.nanpi" = "admin.han140.jizhou.bohai"
            "admin.han140.qingzhou.pingyuan.pingyuan" = "admin.han140.qingzhou.pingyuan"
            "admin.han140.jizhou.qinghe.ganling" = "admin.han140.jizhou.qinghe"
        }
        $regionParents = @{
            "geo.region.northeast.asia.liaodongkoreanorth.liaoheriverplain.xiangpingcounty" = "geo.region.northeast.asia.liaodongkoreanorth.liaoheriverplain"
            "geo.region.northeast.asia.liaodongkoreanorth.koreanorthwestplain.chaoxiancounty" = "geo.region.northeast.asia.liaodongkoreanorth.koreanorthwestplain"
            "geo.region.north.china.yanshanliaoxi.centralfoothillplain.tuyincounty" = "geo.region.north.china.yanshanliaoxi.centralfoothillplain"
            "geo.region.north.china.fenriverluliang.northcentralbasin.jinyangcounty" = "geo.region.north.china.fenriverluliang.northcentralbasin"
            "geo.region.north.china.taihangshangdang.southwestbasin.huguancounty" = "geo.region.north.china.taihangshangdang.southwestbasin"
            "geo.region.north.china.hebei.eastcoastalplain.nanpicounty" = "geo.region.north.china.hebei.eastcoastalplain"
            "geo.region.east.china.loweryellowjieastplain.northwestplain.pingyuancounty" = "geo.region.east.china.loweryellowjieastplain.northwestplain"
            "geo.region.north.china.hebei.southeastplain.ganlingcounty" = "geo.region.north.china.hebei.southeastplain"
        }
        $expectedResolved = @{
            "C001" = @("geo.region.northeast.asia.liaodongkoreanorth.liaoheriverplain.xiangpingcounty", "admin.han140.youzhou.liaodong.xiangping")
            "C002" = @("geo.region.northeast.asia.liaodongkoreanorth.koreanorthwestplain.chaoxiancounty", "admin.han140.youzhou.lelang.chaoxian")
            "C003" = @("geo.region.north.china.yanshanliaoxi.centralfoothillplain.tuyincounty", "admin.han140.youzhou.youbeiping.tuyin")
            "C004" = @("geo.region.north.china.hebei.northcentralplain.jicounty", "admin.han140.youzhou.guangyang.ji")
            "C005" = @("geo.region.north.china.fenriverluliang.northcentralbasin.jinyangcounty", "admin.han140.bingzhou.taiyuan.jinyang")
            "C006" = @("geo.region.north.china.taihangshangdang.southwestbasin.huguancounty", "admin.han140.bingzhou.shangdang.huguan")
            "C007" = @("geo.region.north.china.hebei.eastcoastalplain.nanpicounty", "admin.han140.jizhou.bohai.nanpi")
            "C008" = @("geo.region.east.china.loweryellowjieastplain.northwestplain.pingyuancounty", "admin.han140.qingzhou.pingyuan.pingyuan")
            "C011" = @("geo.region.north.china.hebei.southeastplain.ganlingcounty", "admin.han140.jizhou.qinghe.ganling")
        }
        $batchRows = @($crosswalkRows | Where-Object { $expectedBatchIds -ccontains $_.game_location_id })
        $cityRows = @($crosswalkRows | Where-Object { $_.game_location_kind -ceq "city_catalog" })
        $countyAdminRows = @($adminRows | Where-Object { $adminParents.ContainsKey([string]$_.admin_unit_id) })
        $countyRegionRows = @($regionRows | Where-Object { $regionParents.ContainsKey([string]$_.stable_region_id) })
        $countyPopulationRows = @($populationRows | Where-Object { $adminParents.ContainsKey([string]$_.admin_unit_id) })
        $byGameId = @{}
        foreach ($row in $batchRows) {
            $byGameId[[string]$row.game_location_id] = $row
        }

        Assert-True -Condition (@($sourceDocument.sources).Count -eq 4) -Message "P3 third batch must retain four registered sources."
        Assert-True -Condition ($adminRows.Count -eq 133) -Message "P3 third batch must produce one hundred thirty-three administrative units."
        Assert-True -Condition ($regionRows.Count -eq 168) -Message "P3 third batch must produce one hundred sixty-eight stable regions."
        Assert-True -Condition ($populationRows.Count -eq 105 -and $mappingRows.Count -eq 105) -Message "P3 third batch must not alter population record or mapping counts."
        Assert-True -Condition ($crosswalkRows.Count -eq 31) -Message "P3 third batch must produce thirty-one crosswalk rows."
        Assert-True -Condition (@($crosswalkRows | Where-Object { $_.game_location_kind -ceq "runtime" }).Count -eq 6) -Message "Runtime crosswalk count must remain six."
        Assert-True -Condition (@($crosswalkRows | Where-Object { $_.game_location_kind -ceq "prototype_catalog" }).Count -eq 12) -Message "Prototype crosswalk count must remain twelve."
        Assert-True -Condition ($cityRows.Count -eq 13) -Message "The first thirteen city catalog nodes must be classified."
        Assert-True -Condition ((@($cityRows.game_location_id | Sort-Object) -join "|") -ceq (@($expectedAllCityIds | Sort-Object) -join "|")) -Message "City catalog IDs must be exactly C001 through C013."
        Assert-True -Condition ($batchRows.Count -eq 10) -Message "P3 northern city batch must contain exactly ten scoped rows."
        Assert-True -Condition (@($crosswalkRows | Where-Object { $_.mapping_status -ceq "approximate" }).Count -eq 20) -Message "Crosswalk must contain twenty approximate mappings."
        Assert-True -Condition (@($crosswalkRows | Where-Object { $_.mapping_status -ceq "aggregate" }).Count -eq 8) -Message "Crosswalk must contain eight aggregate mappings."
        Assert-True -Condition (@($crosswalkRows | Where-Object { $_.mapping_status -ceq "unresolved" }).Count -eq 3) -Message "Crosswalk must contain three unresolved mappings."
        Assert-True -Condition ($countyAdminRows.Count -eq 8 -and @($countyAdminRows | Where-Object { $_.unit_type -cne "county" }).Count -eq 0) -Message "All eight northern county candidates must exist."
        Assert-True -Condition ($countyRegionRows.Count -eq 8 -and @($countyRegionRows | Where-Object { $_.region_type -cne "county_area" }).Count -eq 0) -Message "All eight northern county stable identities must exist."
        Assert-True -Condition ($countyPopulationRows.Count -eq 0) -Message "Northern county catalog nodes must not become population records."

        foreach ($row in $countyAdminRows) {
            Assert-True -Condition ([string]$row.parent_admin_unit_id -ceq [string]$adminParents[[string]$row.admin_unit_id]) -Message "County '$($row.admin_unit_id)' has the wrong parent."
        }
        foreach ($row in $countyRegionRows) {
            Assert-True -Condition ([string]$row.parent_stable_region_id -ceq [string]$regionParents[[string]$row.stable_region_id]) -Message "County region '$($row.stable_region_id)' has the wrong parent."
            Assert-True -Condition ([string]$row.geometry_status -ceq "provisional" -and [string]$row.provisional -ceq "true") -Message "County region '$($row.stable_region_id)' must remain provisional."
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$row.centroid_latitude) -and [string]::IsNullOrWhiteSpace([string]$row.centroid_longitude)) -Message "County region '$($row.stable_region_id)' must not invent coordinates."
        }
        foreach ($parentId in @($adminParents.Values | Sort-Object -Unique)) {
            $rows = @($mappingRows | Where-Object { $_.source_id -ceq $parentId })
            Assert-True -Condition ($rows.Count -eq 1 -and [int]$rows[0].weight_basis_points -eq 10000) -Message "Population mapping for '$parentId' must remain single-counted at ten thousand basis points."
        }
        foreach ($gameId in @($expectedResolved.Keys)) {
            $row = $byGameId[$gameId]
            Assert-True -Condition ([string]$row.stable_region_id -ceq [string]$expectedResolved[$gameId][0] -and [string]$row.admin_unit_id -ceq [string]$expectedResolved[$gameId][1]) -Message "City '$gameId' does not reference its expected county identity."
            Assert-True -Condition ([string]$row.mapping_status -ceq "approximate") -Message "City '$gameId' must remain an approximate mapping."
        }
        Assert-True -Condition ([string]$byGameId["C004"].relation_type -ceq "city_catalog_alias") -Message "C004 must reuse the existing Ji county identity as an alias."
        foreach ($gameId in @("C001", "C002", "C003", "C005", "C006", "C007", "C008", "C011")) {
            Assert-True -Condition ([string]$byGameId[$gameId].relation_type -ceq "city_catalog_county_identity") -Message "City '$gameId' must use the county identity relation."
        }
        $chengyang = $byGameId["C013"]
        Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$chengyang.stable_region_id) -and [string]::IsNullOrWhiteSpace([string]$chengyang.admin_unit_id)) -Message "C013 must not invent stable or administrative identity."
        Assert-True -Condition ([string]$chengyang.mapping_status -ceq "unresolved" -and [string]$chengyang.relation_type -ceq "city_catalog_unresolved") -Message "C013 must remain explicitly unresolved."
        Assert-True -Condition ([string]$chengyang.confidence -ceq "unknown" -and [string]$chengyang.source_ids -ceq "source.project.prototype_location_catalog.v1") -Message "C013 must cite only the project catalog and use unknown confidence."
        foreach ($row in $batchRows) {
            Assert-True -Condition ([string]::IsNullOrWhiteSpace([string]$row.valid_from_year) -and [string]::IsNullOrWhiteSpace([string]$row.valid_to_year)) -Message "City catalog '$($row.game_location_id)' must not invent a fixed validity range."
            Assert-True -Condition ([string]$row.provisional -ceq "true") -Message "City catalog '$($row.game_location_id)' must remain provisional."
        }
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

    Invoke-TestCase -Name "incomplete mapping weight rejected" -Body {
        Assert-Rejected -Name "incomplete-mapping-weight" -Mutate {
            param($caseRoot)
            $path = Join-Path $caseRoot "han_140_region_mapping.csv"
            $rows = @(Import-Csv -LiteralPath $path)
            $rows[0].weight_basis_points = "9999"
            $lines = @($rows | ConvertTo-Csv -NoTypeInformation)
            [System.IO.File]::WriteAllLines($path, $lines, $utf8NoBom)
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
