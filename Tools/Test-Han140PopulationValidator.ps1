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
            "admin.han140.jizhou.anping" = @(91440, 655118, "han140.p1.batch1.v1")
            "admin.han140.jizhou.bohai" = @(132389, 1106500, "han140.p1.batch2.v1")
            "admin.han140.jizhou.changshan" = @(97500, 631184, "han140.p1.batch2.v1")
            "admin.han140.jizhou.hejian" = @(93754, 634421, "han140.p1.batch2.v1")
            "admin.han140.jizhou.julu" = @(109517, 602096, "han140.p1.batch1.v1")
            "admin.han140.jizhou.qinghe" = @(123964, 760418, "han140.p1.batch2.v1")
            "admin.han140.jizhou.wei" = @(129310, 695606, "han140.p1.batch1.v1")
            "admin.han140.jizhou.zhao" = @(32719, 188381, "han140.p1.batch2.v1")
            "admin.han140.jizhou.zhongshan" = @(97412, 658195, "han140.p1.batch1.v1")
            "admin.han140.youzhou.guangyang" = @(44550, 280600, "han140.p1.batch1.v1")
            "admin.han140.youzhou.zhuo" = @(102218, 633754, "han140.p1.batch1.v1")
        }
        $expectedIds = @($expected.Keys | Sort-Object)
        $actualIds = @($populationRows.admin_unit_id | Sort-Object)

        Assert-True -Condition (($actualIds -join "|") -ceq ($expectedIds -join "|")) -Message "Production population IDs do not match the recorded batches."
        foreach ($row in $populationRows) {
            $values = $expected[[string]$row.admin_unit_id]
            Assert-True -Condition ([long]$row.registered_households_raw -eq [long]$values[0]) -Message "Households differ for '$($row.admin_unit_id)'."
            Assert-True -Condition ([long]$row.registered_population_raw -eq [long]$values[1]) -Message "Population differs for '$($row.admin_unit_id)'."
            Assert-True -Condition ([string]$row.model_version -ceq [string]$values[2]) -Message "Model version differs for '$($row.admin_unit_id)'."
        }
        Assert-True -Condition ([int]$audit.row_counts.sources -eq 3) -Message "Audit source count is not 3."
        Assert-True -Condition ([int]$audit.row_counts.administrative_units -eq 14) -Message "Audit administrative unit count is not 14."
        Assert-True -Condition ([int]$audit.row_counts.population_records -eq 11) -Message "Audit population record count is not 11."
        Assert-True -Condition ([long]$audit.population_totals.raw_households -eq 1054773) -Message "Recorded household total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.raw_population -eq 6846273) -Message "Recorded population total is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.household_difference_from_anchor -eq -8643857) -Message "Household anchor difference is incorrect."
        Assert-True -Condition ([long]$audit.population_totals.population_difference_from_anchor -eq -42303947) -Message "Population anchor difference is incorrect."
        Assert-True -Condition ([int]$audit.data_quality.records_with_corrections -eq 0) -Message "Recorded batches unexpectedly contain corrections."
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
