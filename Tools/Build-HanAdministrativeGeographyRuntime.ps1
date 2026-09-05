[CmdletBinding()]
param(
    [string]$ProjectPath = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $PSScriptRoot
}
$project = (Resolve-Path -LiteralPath $ProjectPath).Path
$dataRoot = Join-Path $project "Data\HistoricalPopulation"
$worldRoot = Join-Path $project "Assets\StreamingAssets\WorldMap\HanWorldV1"
$catalogPath = Join-Path $worldRoot "metadata\admin_catalog.json"
$outputPath = Join-Path $worldRoot "metadata\administrative_regions_v1.json"
$timelinePath = Join-Path $project (
    "Assets\StreamingAssets\HistoricalPopulation\Han135260V1\" +
    "administrative_timeline.json")

$units = @(Import-Csv -Encoding UTF8 -LiteralPath (
    Join-Path $dataRoot "han_140_administrative_units.csv"))
$mappings = @(Import-Csv -Encoding UTF8 -LiteralPath (
    Join-Path $dataRoot "han_140_region_mapping.csv"))
$stableRegions = @(Import-Csv -Encoding UTF8 -LiteralPath (
    Join-Path $dataRoot "stable_population_regions.csv"))
$catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath |
    ConvertFrom-Json
$timeline = Get-Content -Raw -Encoding UTF8 -LiteralPath $timelinePath |
    ConvertFrom-Json

$unitById = @{}
foreach ($unit in $units) {
    if ($unitById.ContainsKey($unit.admin_unit_id)) {
        throw "Duplicate administrative unit: $($unit.admin_unit_id)"
    }
    $unitById[$unit.admin_unit_id] = $unit
}
$mappingBySource = @{}
foreach ($mapping in $mappings) {
    if ($mappingBySource.ContainsKey($mapping.source_id)) {
        throw "Duplicate stable-geography mapping: $($mapping.source_id)"
    }
    $mappingBySource[$mapping.source_id] = $mapping
}
$stableById = @{}
foreach ($stable in $stableRegions) {
    $stableById[$stable.stable_region_id] = $stable
}

$orderedIds = @($catalog.provinces) + @($catalog.commanderies) +
    @($catalog.counties)
$regions = @(
    foreach ($id in $orderedIds) {
        if (-not $unitById.ContainsKey($id)) {
            throw "HanWorldV1 references missing administrative unit: $id"
        }
        $unit = $unitById[$id]
        $level = if ($unit.unit_type -eq "province") {
            "Province"
        }
        elseif ($unit.unit_type -eq "county") {
            "County"
        }
        else {
            "CommanderyEquivalent"
        }
        $parent = if ($level -eq "Province") {
            ""
        }
        else {
            $unit.parent_admin_unit_id
        }
        $stableId = ""
        $sourceGeometryStatus = "none"
        if ($mappingBySource.ContainsKey($id)) {
            $stableId = $mappingBySource[$id].target_id
            if ($stableById.ContainsKey($stableId)) {
                $sourceGeometryStatus = $stableById[$stableId].geometry_status
            }
        }
        [ordered]@{
            id = $id
            level = $level
            region_type = $unit.unit_type
            parent_region_id = $parent
            stable_geography_id = $stableId
            fallback_display_name = if (
                [string]::IsNullOrWhiteSpace($unit.name_140)) {
                $unit.canonical_name
            }
            else {
                $unit.name_140
            }
            # admin.bin is a deterministic gameplay assignment, not a
            # verified historical polygon. Keep that approximation explicit.
            geometry_status = "Approximate"
            source_geometry_status = $sourceGeometryStatus
            confidence = $unit.confidence
            provisional = $true
        }
    }
)

$namePeriods = @(
    foreach ($record in @($timeline.records)) {
        if (-not $unitById.ContainsKey($record.region_permanent_id)) {
            continue
        }
        [ordered]@{
            stable_id = $record.region_permanent_id
            display_name = $record.historical_name
            valid_from_year = [int]$record.valid_from_year
            valid_to_year = [int]$record.valid_to_year
        }
    }
)

$payload = [ordered]@{
    schema = "mandate.han-administrative-geography-runtime.v1"
    revision_id = "han140.admin-bin.administrative-geography.v1"
    province_count = @($catalog.provinces).Count
    commandery_equivalent_count = @($catalog.commanderies).Count
    county_count = @($catalog.counties).Count
    source_precision_note = (
        "Cell assignments are deterministic approximate/provisional gameplay " +
        "geometry derived from existing HanWorldV1 admin.bin; they are not " +
        "verified Han historical polygons.")
    regions = $regions
    name_periods = $namePeriods
}

$json = $payload | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText(
    $outputPath,
    $json + [Environment]::NewLine,
    (New-Object System.Text.UTF8Encoding($false)))
Write-Output (
    "RESULT provinces=$(@($catalog.provinces).Count) " +
    "commanderies=$(@($catalog.commanderies).Count) " +
    "counties=$(@($catalog.counties).Count) " +
    "name_periods=$($namePeriods.Count) output=$outputPath")
