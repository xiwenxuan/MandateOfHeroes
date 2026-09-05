[CmdletBinding()]
param(
    [string]$ProjectRoot = "."
)

$ErrorActionPreference = "Stop"
if ($ProjectRoot -eq "." -and -not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}
$project = [IO.Path]::GetFullPath($ProjectRoot)
$worldMapRoot = Join-Path $project "Assets\StreamingAssets\WorldMap"
$outputDirectory = Join-Path $worldMapRoot "Luoyang50mCountyLayoutV1"
$outputPath = Join-Path $outputDirectory `
    "luoyang_50m_county_layout_v1.json"

$rows = 320
$columns = 640
$sourceMinRow = 1202
$sourceMaxRow = 1266
$sourceMinColumn = 2013
$sourceMaxColumn = 2104
$minimumStrategicRow = 1236
$minimumStrategicColumn = 2036

function Convert-ToLocalIndex {
    param(
        [int]$Value,
        [int]$Minimum,
        [int]$Maximum,
        [int]$TargetCount
    )
    return [int][Math]::Round(
        ($Value - $Minimum) * ($TargetCount - 1.0) / ($Maximum - $Minimum),
        [MidpointRounding]::AwayFromZero)
}

function Get-DistrictId {
    param([string]$DefinitionId)
    if ($DefinitionId.StartsWith("facility.fortification.") -or
        $DefinitionId.StartsWith("facility.military.")) {
        return "district.luoyang.defense-ring.v1"
    }
    if ($DefinitionId.StartsWith("facility.agriculture.") -or
        $DefinitionId.StartsWith("facility.resource.") -or
        $DefinitionId -eq "facility.residential.rural_hamlet") {
        return "district.luoyang.agricultural-resource-hinterland.v1"
    }
    if ($DefinitionId -in @(
            "facility.public.road",
            "facility.public.canal",
            "facility.public.bridge",
            "facility.public.well") -or
        $DefinitionId.StartsWith("facility.service.post_station") -or
        $DefinitionId.StartsWith("facility.service.caravan_yard")) {
        return "district.luoyang.water-transport-network.v1"
    }
    if ($DefinitionId.StartsWith("facility.residential.")) {
        return "district.luoyang.residential-wards.v1"
    }
    if ($DefinitionId.StartsWith("facility.commercial.") -or
        $DefinitionId.StartsWith("facility.industry.") -or
        $DefinitionId.StartsWith("facility.storage.") -or
        $DefinitionId -eq "facility.public.granary" -or
        $DefinitionId -eq "facility.service.inn") {
        return "district.luoyang.market-workshop-band.v1"
    }
    return "district.luoyang.palace-civic-core.v1"
}

function Get-Footprint {
    param([string]$CategoryId, [string]$DefinitionId)
    $width = 3000
    $depth = 3000
    $height = 650
    if ($DefinitionId.StartsWith("facility.fortification.")) {
        $width = 4000; $depth = 1000; $height = 1200
    }
    elseif ($DefinitionId -eq "facility.public.road") {
        $width = 4500; $depth = 1200; $height = 0
    }
    elseif ($CategoryId -in @("government", "ritual", "education", "military")) {
        $width = 4000; $depth = 4000; $height = 1000
    }
    elseif ($CategoryId -in @("agriculture", "resource_agriculture", "resource")) {
        $width = 4500; $depth = 4500; $height = 150
    }
    return [ordered]@{
        width_centimetres = $width
        depth_centimetres = $depth
        height_centimetres = $height
    }
}

function Get-DirectionId {
    param([int]$QuarterTurns)
    return @("north", "east", "south", "west")[$QuarterTurns % 4]
}

function Get-Cross {
    param($Origin, $A, $B)
    return (($A.column - $Origin.column) * ($B.row - $Origin.row)) -
        (($A.row - $Origin.row) * ($B.column - $Origin.column))
}

function Get-ConvexHull {
    param([object[]]$InputPoints)
    $points = @($InputPoints |
        Group-Object { "$($_.column),$($_.row)" } |
        ForEach-Object { $_.Group[0] } |
        Sort-Object @{ Expression = { $_.column } },
            @{ Expression = { $_.row } })
    if ($points.Count -le 1) { return @($points) }

    $lower = New-Object System.Collections.ArrayList
    foreach ($point in $points) {
        while ($lower.Count -ge 2 -and
            (Get-Cross $lower[$lower.Count - 2] $lower[$lower.Count - 1] $point) -le 0) {
            $lower.RemoveAt($lower.Count - 1)
        }
        [void]$lower.Add($point)
    }
    $upper = New-Object System.Collections.ArrayList
    for ($index = $points.Count - 1; $index -ge 0; $index--) {
        $point = $points[$index]
        while ($upper.Count -ge 2 -and
            (Get-Cross $upper[$upper.Count - 2] $upper[$upper.Count - 1] $point) -le 0) {
            $upper.RemoveAt($upper.Count - 1)
        }
        [void]$upper.Add($point)
    }
    $lower.RemoveAt($lower.Count - 1)
    $upper.RemoveAt($upper.Count - 1)
    return @($lower) + @($upper)
}

function New-Area {
    param([string]$AreaId, [string]$DistrictId, [object[]]$FacilityRows)
    $points = @($FacilityRows | ForEach-Object {
        [ordered]@{ row = $_.local_row; column = $_.local_column }
    })
    $hull = @(Get-ConvexHull $points)
    return [ordered]@{
        urban_area_id = $AreaId
        district_id = $DistrictId
        facility_count = $FacilityRows.Count
        minimum_row = [int](($points.row | Measure-Object -Minimum).Minimum)
        maximum_row = [int](($points.row | Measure-Object -Maximum).Maximum)
        minimum_column = [int](($points.column | Measure-Object -Minimum).Minimum)
        maximum_column = [int](($points.column | Measure-Object -Maximum).Maximum)
        hull_cells = $hull
        geometry_provenance_id =
            "spatial-geometry.facility-anchor-convex-hull.provisional.v1"
        status_id = "gameplay-reconstruction-review-candidate"
    }
}

$sourceConfigs = @(
    [ordered]@{
        relative_path = "Luoyang184UrbanInitializationV1/facilities.json"
        source_package_id = "source.runtime.luoyang184-urban-initialization.v1"
    },
    [ordered]@{
        relative_path = "Luoyang184MetropolitanInitializationV1/facilities.json"
        source_package_id = "source.runtime.luoyang184-metropolitan-initialization.v1"
    }
)

$sourceFiles = New-Object System.Collections.ArrayList
$facilityRows = New-Object System.Collections.ArrayList
foreach ($config in $sourceConfigs) {
    $path = Join-Path $worldMapRoot ($config.relative_path.Replace('/', '\'))
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required Facility source is missing: $path"
    }
    $root = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8) |
        ConvertFrom-Json
    [void]$sourceFiles.Add([ordered]@{
        relative_path = $config.relative_path
        source_package_id = $config.source_package_id
        facility_count = @($root.facilities).Count
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant()
    })
    foreach ($facility in $root.facilities) {
        $localRow = Convert-ToLocalIndex ([int]$facility.grid_y) `
            $sourceMinRow $sourceMaxRow $rows
        $localColumn = Convert-ToLocalIndex ([int]$facility.grid_x) `
            $sourceMinColumn $sourceMaxColumn $columns
        $turns = [int](([uint64]$facility.cell_id64) % 4)
        $footprint = Get-Footprint ([string]$facility.category_id) `
            ([string]$facility.definition_id)
        $sourceIds = @(
            @($facility.source_ids | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) +
            @($config.source_package_id) |
            Sort-Object -Unique
        )
        $candidateStrategicRow = $minimumStrategicRow +
            [Math]::Floor($localRow / 40)
        $candidateStrategicColumn = $minimumStrategicColumn +
            [Math]::Floor($localColumn / 40)
        [void]$facilityRows.Add([ordered]@{
            facility_id = [string]$facility.facility_id
            definition_id = [string]$facility.definition_id
            display_name = [string]$facility.display_name
            category_id = [string]$facility.category_id
            source_package_id = $config.source_package_id
            source_ids = $sourceIds
            source_cell_id64 = [uint64]$facility.cell_id64
            source_row = [int]$facility.grid_y
            source_column = [int]$facility.grid_x
            source_spatial_precision_id = [string]$facility.spatial_precision
            historical_confidence_id = [string]$facility.historical_confidence
            local_row = $localRow
            local_column = $localColumn
            width_centimetres = $footprint.width_centimetres
            depth_centimetres = $footprint.depth_centimetres
            height_centimetres = $footprint.height_centimetres
            rotation_quarter_turns = $turns
            entrance_direction_id = Get-DirectionId $turns
            district_id = Get-DistrictId ([string]$facility.definition_id)
            preserves_source_strategic_tile =
                ($candidateStrategicRow -eq [int]$facility.grid_y -and
                 $candidateStrategicColumn -eq [int]$facility.grid_x)
            placement_provenance_id =
                "spatial-provenance.gameplay-reconstruction.provisional.v1"
            footprint_provenance_id =
                "spatial-footprint.category-default.provisional.v1"
            entrance_provenance_id =
                "spatial-entrance.cell-id-quarter-turn.provisional.v1"
        })
    }
}
$facilities = @($facilityRows | Sort-Object source_cell_id64, facility_id)
if ($facilities.Count -ne 2084) {
    throw "Expected 2084 Facilities, got $($facilities.Count)."
}

function New-Network {
    param([object[]]$Members, [string]$Kind)
    $orderedMembers = @($Members | Sort-Object facility_id)
    $nodes = @($orderedMembers | ForEach-Object {
        [ordered]@{
            node_id = "$Kind-node.$($_.facility_id).v1"
            facility_id = $_.facility_id
            local_row = $_.local_row
            local_column = $_.local_column
        }
    })
    $bySourceCell = @{}
    foreach ($member in $orderedMembers) {
        $bySourceCell["$($member.source_row),$($member.source_column)"] = $member
    }
    $pairs = New-Object System.Collections.ArrayList
    foreach ($member in $orderedMembers) {
        foreach ($offset in @(@(0, 1), @(1, 0))) {
            $key = "$($member.source_row + $offset[0]),$($member.source_column + $offset[1])"
            if ($bySourceCell.ContainsKey($key)) {
                $other = $bySourceCell[$key]
                [void]$pairs.Add([ordered]@{ first = $member; second = $other })
            }
        }
    }
    $pairs = @($pairs | Sort-Object {
        "$($_.first.facility_id)|$($_.second.facility_id)"
    })
    $edges = New-Object System.Collections.ArrayList
    for ($index = 0; $index -lt $pairs.Count; $index++) {
        $pair = $pairs[$index]
        [void]$edges.Add([ordered]@{
            edge_id = "edge.$Kind.luoyang.$(($index + 1).ToString('D6')).v1"
            from_node_id = "$Kind-node.$($pair.first.facility_id).v1"
            to_node_id = "$Kind-node.$($pair.second.facility_id).v1"
            from_local_row = $pair.first.local_row
            from_local_column = $pair.first.local_column
            to_local_row = $pair.second.local_row
            to_local_column = $pair.second.local_column
            source_manhattan_distance = 1
            geometry_provenance_id =
                "spatial-geometry.source-cardinal-adjacency.provisional.v1"
        })
    }
    return [ordered]@{ nodes = $nodes; edges = @($edges) }
}

$roadNetwork = New-Network @($facilities | Where-Object {
    $_.definition_id -eq "facility.public.road"
}) "road"
$canalNetwork = New-Network @($facilities | Where-Object {
    $_.definition_id -eq "facility.public.canal"
}) "canal"

$fortifications = New-Object System.Collections.ArrayList
foreach ($facility in @($facilities | Where-Object {
    $_.definition_id.StartsWith("facility.fortification.")
} | Sort-Object facility_id)) {
    $isGate = $facility.definition_id.IndexOf("gate",
        [StringComparison]::OrdinalIgnoreCase) -ge 0
    [void]$fortifications.Add([ordered]@{
        edge_id = "$($facility.facility_id).edge.50m-layout.v1"
        facility_id = $facility.facility_id
        definition_id = $facility.definition_id
        local_row = $facility.local_row
        local_column = $facility.local_column
        direction_id = if ($facility.local_column + 1 -lt $columns) { "east" } else { "west" }
        is_gate = $isGate
        height_centimetres = if ($isGate) { 900 } else { 1200 }
        thickness_centimetres = if ($isGate) { 500 } else { 350 }
        maximum_durability = 100
        geometry_provenance_id =
            "spatial-geometry.facility-edge.provisional.v1"
    })
}

function Get-NearestRoad {
    param([int]$TargetRow, [int]$TargetColumn)
    return @($roadNetwork.nodes | Sort-Object @{ Expression = {
        [Math]::Abs($_.local_row - $TargetRow) +
        [Math]::Abs($_.local_column - $TargetColumn)
    } }, @{ Expression = { $_.facility_id } })[0]
}

$northRoad = Get-NearestRoad 0 ([int]($columns / 2))
$southRoad = Get-NearestRoad ($rows - 1) ([int]($columns / 2))
$westRoad = Get-NearestRoad ([int]($rows / 2)) 0
$eastRoad = Get-NearestRoad ([int]($rows / 2)) ($columns - 1)
$portals = @(
    [ordered]@{ side_id="north"; local_row=0; local_column=$northRoad.local_column; inward_direction_id="south"; anchor_facility_id=$northRoad.facility_id },
    [ordered]@{ side_id="south"; local_row=$rows-1; local_column=$southRoad.local_column; inward_direction_id="north"; anchor_facility_id=$southRoad.facility_id },
    [ordered]@{ side_id="west"; local_row=$westRoad.local_row; local_column=0; inward_direction_id="east"; anchor_facility_id=$westRoad.facility_id },
    [ordered]@{ side_id="east"; local_row=$eastRoad.local_row; local_column=$columns-1; inward_direction_id="west"; anchor_facility_id=$eastRoad.facility_id }
) | ForEach-Object {
    [ordered]@{
        portal_id = "portal.candidate.luoyang.$($_.side_id).v1"
        route_id = "route.candidate.luoyang.$($_.side_id).v1"
        side_id = $_.side_id
        local_row = $_.local_row
        local_column = $_.local_column
        inward_direction_id = $_.inward_direction_id
        anchor_facility_id = $_.anchor_facility_id
        neighbor_county_id = "county.neighbor.unknown.$($_.side_id).v1"
        passage_type_id = "portal.passage.official-road.provisional.v1"
        geometry_provenance_id =
            "spatial-geometry.nearest-road-boundary-portal.provisional.v1"
    }
}

$districtIds = @(
    "district.luoyang.palace-civic-core.v1",
    "district.luoyang.residential-wards.v1",
    "district.luoyang.market-workshop-band.v1",
    "district.luoyang.defense-ring.v1",
    "district.luoyang.water-transport-network.v1",
    "district.luoyang.agricultural-resource-hinterland.v1"
)
$districtAreas = @($districtIds | ForEach-Object {
    $districtId = $_
    New-Area "urban-area.candidate.luoyang.$(($districtId -split '\.')[2]).v1" `
        $districtId @($facilities | Where-Object { $_.district_id -eq $districtId })
})
$urbanArea = New-Area "urban-area.candidate.luoyang.overall.v1" `
    "district.luoyang.all.v1" $facilities

$package = [ordered]@{
    schema_id = "mandate.luoyang.county-layout-50m.schema.v1"
    package_id = "mandate.luoyang.county-layout-50m.runtime-authority.v1"
    status_id = "gameplay-reconstruction-review-candidate"
    county_id = "admin.han140.sili.henan.luoyang"
    historical_placement_gate_id =
        "historical-placement.pending-authoritative-50m-source.v1"
    semantics = [ordered]@{
        runtime_authoritative = $true
        historically_exact = $false
        mutates_world_state = $false
        changes_save_schema = $false
        statement =
            "Authoritative for deterministic runtime layout input; provisional for historical placement."
    }
    grid = [ordered]@{
        row_count = $rows
        column_count = $columns
        cell_size_metres = 50
        county_area_square_kilometres = 512
        chunk_size_cells = 16
        minimum_strategic_row = $minimumStrategicRow
        minimum_strategic_column = $minimumStrategicColumn
        strategic_row_count = 8
        strategic_column_count = 16
        source_minimum_row = $sourceMinRow
        source_maximum_row = $sourceMaxRow
        source_minimum_column = $sourceMinColumn
        source_maximum_column = $sourceMaxColumn
    }
    source_contract_ids = @(
        "mandate.luoyang.building-performance-budget.v1",
        "mandate.luoyang.whole-city-composition.v1",
        "mandate.luoyang.county-spatial-50m.prototype.v1"
    )
    source_files = @($sourceFiles)
    counts = [ordered]@{
        facility_count = $facilities.Count
        road_node_count = $roadNetwork.nodes.Count
        road_edge_count = $roadNetwork.edges.Count
        canal_node_count = $canalNetwork.nodes.Count
        canal_edge_count = $canalNetwork.edges.Count
        fortification_edge_count = $fortifications.Count
        portal_count = $portals.Count
        district_area_count = $districtAreas.Count
    }
    facilities = $facilities
    road_nodes = $roadNetwork.nodes
    road_edges = $roadNetwork.edges
    canal_nodes = $canalNetwork.nodes
    canal_edges = $canalNetwork.edges
    fortification_edges = @($fortifications)
    portals = $portals
    district_areas = $districtAreas
    urban_area_candidate = $urbanArea
    layout_fingerprint_sha256 = ""
}

$fingerprintLines = New-Object System.Collections.ArrayList
[void]$fingerprintLines.Add("H|$($package.schema_id)|$($package.package_id)|$($package.status_id)|$($package.county_id)|$rows|$columns|50|$minimumStrategicRow|$minimumStrategicColumn|$($facilities.Count)|$($roadNetwork.nodes.Count)|$($roadNetwork.edges.Count)|$($canalNetwork.nodes.Count)|$($canalNetwork.edges.Count)|$($fortifications.Count)|$($portals.Count)|$($districtAreas.Count)")
foreach ($item in $facilities) {
    [void]$fingerprintLines.Add((@(
        "F", $item.facility_id, $item.definition_id, $item.source_package_id,
        ($item.source_ids -join ','), $item.source_cell_id64, $item.source_row,
        $item.source_column, $item.local_row, $item.local_column,
        $item.width_centimetres, $item.depth_centimetres,
        $item.height_centimetres, $item.rotation_quarter_turns,
        $item.entrance_direction_id, $item.district_id,
        $item.source_spatial_precision_id, $item.historical_confidence_id,
        $item.placement_provenance_id, $item.footprint_provenance_id,
        $item.entrance_provenance_id
    ) -join '|'))
}
$fingerprintText = $fingerprintLines -join "`n"
$sha = [Security.Cryptography.SHA256]::Create()
try {
    $bytes = [Text.Encoding]::UTF8.GetBytes($fingerprintText)
    $package.layout_fingerprint_sha256 =
        ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
}
finally { $sha.Dispose() }

[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$json = $package | ConvertTo-Json -Depth 12
[IO.File]::WriteAllText($outputPath, $json + "`n",
    (New-Object Text.UTF8Encoding($false)))

Write-Host "Generated: $outputPath"
Write-Host "Facilities: $($facilities.Count)"
Write-Host "Road nodes/edges: $($roadNetwork.nodes.Count)/$($roadNetwork.edges.Count)"
Write-Host "Canal nodes/edges: $($canalNetwork.nodes.Count)/$($canalNetwork.edges.Count)"
Write-Host "Fortifications/portals/areas: $($fortifications.Count)/$($portals.Count)/$($districtAreas.Count)"
Write-Host "Layout fingerprint: $($package.layout_fingerprint_sha256)"
