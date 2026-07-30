[CmdletBinding()]
param(
    [string]$DataRoot,
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:ValidationErrors = New-Object "System.Collections.Generic.List[string]"
$script:Utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)
$script:Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if ([string]::IsNullOrWhiteSpace($DataRoot)) {
    $DataRoot = Join-Path (Split-Path -Parent $PSScriptRoot) "Data\HistoricalPopulation"
}

function Add-ValidationError {
    param([string]$Message)

    [void]$script:ValidationErrors.Add($Message)
}

function Get-Utf8Text {
    param(
        [string]$Path,
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-ValidationError "$Label file is missing: $Path"
        return $null
    }

    try {
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        $text = $script:Utf8Strict.GetString($bytes)
        if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) {
            $text = $text.Substring(1)
        }

        return $text
    }
    catch {
        Add-ValidationError "$Label is not valid UTF-8: $($_.Exception.Message)"
        return $null
    }
}

function Test-ObjectFields {
    param(
        [object]$Value,
        [string[]]$Fields,
        [string]$Label
    )

    if ($null -eq $Value) {
        Add-ValidationError "$Label is missing."
        return $false
    }

    $propertyNames = @($Value.PSObject.Properties.Name)
    $valid = $true
    foreach ($field in $Fields) {
        if ($propertyNames -notcontains $field) {
            Add-ValidationError "$Label is missing required field '$field'."
            $valid = $false
        }
    }

    return $valid
}

function Get-ObjectProperty {
    param(
        [object]$Value,
        [string]$Name
    )

    if ($null -eq $Value) {
        return $null
    }

    $property = $Value.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Test-RequiredText {
    param(
        [object]$Value,
        [string]$Label
    )

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) {
        Add-ValidationError "$Label must not be empty."
        return $false
    }

    return $true
}

function Read-ContractCsv {
    param(
        [string]$Path,
        [string]$Label,
        [string[]]$ExpectedHeaders
    )

    $text = Get-Utf8Text -Path $Path -Label $Label
    if ($null -eq $text) {
        return [pscustomobject]@{
            Headers = @()
            Rows = @()
        }
    }

    $lines = @($text -split "\r?\n")
    if ($lines.Count -eq 0 -or [string]::IsNullOrWhiteSpace($lines[0])) {
        Add-ValidationError "$Label has no CSV header."
        return [pscustomobject]@{
            Headers = @()
            Rows = @()
        }
    }

    $actualHeaders = @($lines[0].Split(","))
    if (($actualHeaders -join ",") -cne ($ExpectedHeaders -join ",")) {
        Add-ValidationError "$Label header does not match the data contract. Expected '$($ExpectedHeaders -join ",")'."
    }

    try {
        $rows = @($text | ConvertFrom-Csv)
    }
    catch {
        Add-ValidationError "$Label is not valid CSV: $($_.Exception.Message)"
        $rows = @()
    }

    return [pscustomobject]@{
        Headers = $actualHeaders
        Rows = $rows
    }
}

function Get-IdList {
    param([object]$Value)

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) {
        return @()
    }

    return @(
        ([string]$Value).Split("|") |
            ForEach-Object { $_.Trim() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
}

function Test-IdFormat {
    param(
        [string]$Value,
        [string]$Pattern,
        [string]$Label
    )

    if (-not (Test-RequiredText -Value $Value -Label $Label)) {
        return $false
    }

    if ($Value -cnotmatch $Pattern) {
        Add-ValidationError "$Label has invalid ID '$Value'."
        return $false
    }

    return $true
}

function Test-EnumValue {
    param(
        [string]$Value,
        [string[]]$Allowed,
        [string]$Label,
        [switch]$AllowEmpty
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        if (-not $AllowEmpty) {
            Add-ValidationError "$Label must not be empty."
        }
        return
    }

    if ($Allowed -cnotcontains $Value) {
        Add-ValidationError "$Label has invalid value '$Value'. Allowed: $($Allowed -join ", ")."
    }
}

function Test-BooleanValue {
    param(
        [string]$Value,
        [string]$Label
    )

    if ($Value -cne "true" -and $Value -cne "false") {
        Add-ValidationError "$Label must be 'true' or 'false', found '$Value'."
    }
}

function Get-NullableNonNegativeInteger {
    param(
        [object]$Value,
        [string]$Label,
        [switch]$Required
    )

    $text = if ($null -eq $Value) { "" } else { ([string]$Value).Trim() }
    if ([string]::IsNullOrWhiteSpace($text)) {
        if ($Required) {
            Add-ValidationError "$Label must be a non-negative integer."
        }
        return $null
    }

    if ($text -cnotmatch "^[0-9]+$") {
        Add-ValidationError "$Label must be a non-negative integer without separators, found '$text'."
        return $null
    }

    $number = [long]0
    if (-not [long]::TryParse($text, [ref]$number)) {
        Add-ValidationError "$Label is outside the supported integer range: '$text'."
        return $null
    }

    return $number
}

function Get-RequiredYear {
    param(
        [object]$Value,
        [string]$Label
    )

    $number = Get-NullableNonNegativeInteger -Value $Value -Label $Label -Required
    if ($null -eq $number) {
        return $null
    }

    if ($number -lt 1 -or $number -gt 9999) {
        Add-ValidationError "$Label must be between 1 and 9999, found '$number'."
        return $null
    }

    return [int]$number
}

function Test-OptionalYearRange {
    param(
        [object]$FromValue,
        [object]$ToValue,
        [string]$Label
    )

    $fromText = if ($null -eq $FromValue) { "" } else { ([string]$FromValue).Trim() }
    $toText = if ($null -eq $ToValue) { "" } else { ([string]$ToValue).Trim() }
    if ([string]::IsNullOrWhiteSpace($fromText) -and [string]::IsNullOrWhiteSpace($toText)) {
        return
    }

    if ([string]::IsNullOrWhiteSpace($fromText) -or [string]::IsNullOrWhiteSpace($toText)) {
        Add-ValidationError "$Label must provide both valid_from_year and valid_to_year, or leave both empty."
        return
    }

    $fromYear = Get-RequiredYear -Value $fromText -Label "$Label.valid_from_year"
    $toYear = Get-RequiredYear -Value $toText -Label "$Label.valid_to_year"
    if ($null -ne $fromYear -and $null -ne $toYear -and $fromYear -gt $toYear) {
        Add-ValidationError "$Label has valid_from_year later than valid_to_year."
    }
}

function Test-RequiredHan140YearRange {
    param(
        [object]$FromValue,
        [object]$ToValue,
        [string]$Label
    )

    $fromYear = Get-RequiredYear -Value $FromValue -Label "$Label.valid_from_year"
    $toYear = Get-RequiredYear -Value $ToValue -Label "$Label.valid_to_year"
    if ($null -eq $fromYear -or $null -eq $toYear) {
        return
    }

    if ($fromYear -gt $toYear) {
        Add-ValidationError "$Label has valid_from_year later than valid_to_year."
        return
    }

    if ($fromYear -gt 140 -or $toYear -lt 140) {
        Add-ValidationError "$Label year range must include dataset year 140."
    }
}

function Test-NullableCoordinate {
    param(
        [object]$Value,
        [decimal]$Minimum,
        [decimal]$Maximum,
        [string]$Label
    )

    $text = if ($null -eq $Value) { "" } else { ([string]$Value).Trim() }
    if ([string]::IsNullOrWhiteSpace($text)) {
        return
    }

    $number = [decimal]0
    if (-not [decimal]::TryParse(
            $text,
            [System.Globalization.NumberStyles]::AllowDecimalPoint -bor [System.Globalization.NumberStyles]::AllowLeadingSign,
            [System.Globalization.CultureInfo]::InvariantCulture,
            [ref]$number)) {
        Add-ValidationError "$Label must be an invariant-culture decimal, found '$text'."
        return
    }

    if ($number -lt $Minimum -or $number -gt $Maximum) {
        Add-ValidationError "$Label must be between $Minimum and $Maximum, found '$number'."
    }
}

function Test-References {
    param(
        [string[]]$Ids,
        [System.Collections.Generic.HashSet[string]]$KnownIds,
        [string]$Label,
        [switch]$RequireAtLeastOne
    )

    if ($RequireAtLeastOne -and $Ids.Count -eq 0) {
        Add-ValidationError "$Label must reference at least one ID."
        return
    }

    foreach ($id in $Ids) {
        if (-not $KnownIds.Contains($id)) {
            Add-ValidationError "$Label references missing ID '$id'."
        }
    }
}

function Test-ParentCycles {
    param(
        [hashtable]$Parents,
        [string]$Label
    )

    foreach ($start in @($Parents.Keys | Sort-Object)) {
        $path = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::Ordinal)
        $current = $start
        while (-not [string]::IsNullOrWhiteSpace($current) -and $Parents.ContainsKey($current)) {
            if (-not $path.Add($current)) {
                Add-ValidationError "$Label contains a parent cycle involving '$current'."
                break
            }
            $current = [string]$Parents[$current]
        }
    }
}

function New-OrdinalIdSet {
    return New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::Ordinal)
}

try {
    $resolvedDataRoot = [System.IO.Path]::GetFullPath($DataRoot)
}
catch {
    Write-Error "Invalid DataRoot '$DataRoot': $($_.Exception.Message)"
    exit 2
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $resolvedDataRoot "han_140_audit_report.json"
}

try {
    $resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
}
catch {
    Write-Error "Invalid OutputPath '$OutputPath': $($_.Exception.Message)"
    exit 2
}

$sourcePath = Join-Path $resolvedDataRoot "han_140_sources.json"
$adminPath = Join-Path $resolvedDataRoot "han_140_administrative_units.csv"
$populationPath = Join-Path $resolvedDataRoot "han_140_population_records.csv"
$regionPath = Join-Path $resolvedDataRoot "stable_population_regions.csv"
$mappingPath = Join-Path $resolvedDataRoot "han_140_region_mapping.csv"
$crosswalkPath = Join-Path $resolvedDataRoot "game_location_crosswalk.csv"

$adminHeaders = @(
    "admin_unit_id",
    "parent_admin_unit_id",
    "unit_type",
    "name_140",
    "canonical_name",
    "seat_admin_unit_id",
    "valid_from_year",
    "valid_to_year",
    "source_ids",
    "confidence",
    "notes"
)
$populationHeaders = @(
    "admin_unit_id",
    "registered_households_raw",
    "registered_population_raw",
    "registered_households_corrected",
    "registered_population_corrected",
    "correction_code",
    "correction_note",
    "evidence_grade",
    "source_ids",
    "source_locator",
    "model_version"
)
$regionHeaders = @(
    "stable_region_id",
    "parent_stable_region_id",
    "region_type",
    "canonical_name",
    "modern_reference",
    "centroid_latitude",
    "centroid_longitude",
    "geometry_status",
    "confidence",
    "provisional",
    "notes"
)
$mappingHeaders = @(
    "source_id",
    "target_id",
    "relation_type",
    "valid_from_year",
    "valid_to_year",
    "weight_basis_points",
    "mapping_method",
    "confidence",
    "provisional",
    "notes"
)
$crosswalkHeaders = @(
    "game_location_id",
    "game_location_kind",
    "stable_region_id",
    "admin_unit_id",
    "mapping_status",
    "relation_type",
    "valid_from_year",
    "valid_to_year",
    "source_ids",
    "confidence",
    "provisional",
    "notes"
)

$sourceDocument = $null
$sourceText = Get-Utf8Text -Path $sourcePath -Label "sources"
if ($null -ne $sourceText) {
    try {
        $sourceDocument = $sourceText | ConvertFrom-Json
    }
    catch {
        Add-ValidationError "sources JSON is invalid: $($_.Exception.Message)"
    }
}

$sourceRequiredFields = @(
    "source_id",
    "source_type",
    "title",
    "author_or_editor",
    "edition_or_host",
    "publication_or_access_date",
    "url_or_bibliographic_locator",
    "license_or_public_domain_note",
    "evidence_scope",
    "notes"
)
$sourceIds = New-OrdinalIdSet
$sourceRows = @()
$anchorHouseholds = [long]0
$anchorPopulation = [long]0

if ($null -ne $sourceDocument -and
    (Test-ObjectFields -Value $sourceDocument -Fields @("schema_version", "dataset_year", "national_anchor", "sources") -Label "sources root")) {
    $schemaVersion = [string](Get-ObjectProperty -Value $sourceDocument -Name "schema_version")
    if ($schemaVersion -cne "han140.sources.v1") {
        Add-ValidationError "sources.schema_version must be 'han140.sources.v1', found '$schemaVersion'."
    }

    $datasetYear = Get-NullableNonNegativeInteger -Value (Get-ObjectProperty -Value $sourceDocument -Name "dataset_year") -Label "sources.dataset_year" -Required
    if ($null -ne $datasetYear -and $datasetYear -ne 140) {
        Add-ValidationError "sources.dataset_year must be 140, found '$datasetYear'."
    }

    $sourceRows = @(Get-ObjectProperty -Value $sourceDocument -Name "sources")
    foreach ($source in $sourceRows) {
        $id = [string](Get-ObjectProperty -Value $source -Name "source_id")
        if (-not (Test-ObjectFields -Value $source -Fields $sourceRequiredFields -Label "source '$id'")) {
            continue
        }

        if (Test-IdFormat -Value $id -Pattern "^source\.[a-z0-9]+(?:[._-][a-z0-9]+)*$" -Label "source.source_id") {
            if (-not $sourceIds.Add($id)) {
                Add-ValidationError "Duplicate source ID '$id'."
            }
        }

        Test-EnumValue -Value ([string](Get-ObjectProperty -Value $source -Name "source_type")) `
            -Allowed @("primary_text", "modern_research", "project_model", "reference_index") `
            -Label "source '$id'.source_type"

        foreach ($field in $sourceRequiredFields | Where-Object { $_ -notin @("source_id", "source_type") }) {
            Test-RequiredText -Value (Get-ObjectProperty -Value $source -Name $field) -Label "source '$id'.$field" | Out-Null
        }
    }

    $anchor = Get-ObjectProperty -Value $sourceDocument -Name "national_anchor"
    if (Test-ObjectFields -Value $anchor -Fields @("registered_households", "registered_population", "source_ids") -Label "national_anchor") {
        $anchorHouseholdsValue = Get-NullableNonNegativeInteger `
            -Value (Get-ObjectProperty -Value $anchor -Name "registered_households") `
            -Label "national_anchor.registered_households" -Required
        $anchorPopulationValue = Get-NullableNonNegativeInteger `
            -Value (Get-ObjectProperty -Value $anchor -Name "registered_population") `
            -Label "national_anchor.registered_population" -Required
        if ($null -ne $anchorHouseholdsValue) {
            $anchorHouseholds = $anchorHouseholdsValue
            if ($anchorHouseholds -ne 9698630) {
                Add-ValidationError "National household anchor must be 9698630, found '$anchorHouseholds'."
            }
        }
        if ($null -ne $anchorPopulationValue) {
            $anchorPopulation = $anchorPopulationValue
            if ($anchorPopulation -ne 49150220) {
                Add-ValidationError "National population anchor must be 49150220, found '$anchorPopulation'."
            }
        }

        $anchorSourceIds = @(
            @(Get-ObjectProperty -Value $anchor -Name "source_ids") |
                ForEach-Object { [string]$_ }
        )
        Test-References -Ids $anchorSourceIds -KnownIds $sourceIds -Label "national_anchor.source_ids" -RequireAtLeastOne
    }
}

$adminTable = Read-ContractCsv -Path $adminPath -Label "administrative units" -ExpectedHeaders $adminHeaders
$populationTable = Read-ContractCsv -Path $populationPath -Label "population records" -ExpectedHeaders $populationHeaders
$regionTable = Read-ContractCsv -Path $regionPath -Label "stable regions" -ExpectedHeaders $regionHeaders
$mappingTable = Read-ContractCsv -Path $mappingPath -Label "region mappings" -ExpectedHeaders $mappingHeaders
$crosswalkTable = Read-ContractCsv -Path $crosswalkPath -Label "game location crosswalks" -ExpectedHeaders $crosswalkHeaders

$adminIds = New-OrdinalIdSet
$adminParents = @{}
foreach ($row in $adminTable.Rows) {
    $id = [string]$row.admin_unit_id
    if (Test-IdFormat -Value $id -Pattern "^admin\.han140\.[a-z0-9]+(?:\.[a-z0-9]+)*$" -Label "administrative unit ID") {
        if (-not $adminIds.Add($id)) {
            Add-ValidationError "Duplicate administrative unit ID '$id'."
        }
    }

    Test-EnumValue -Value ([string]$row.unit_type) `
        -Allowed @("empire", "province", "commandery", "kingdom", "county", "other") `
        -Label "administrative unit '$id'.unit_type"
    Test-RequiredText -Value $row.name_140 -Label "administrative unit '$id'.name_140" | Out-Null
    Test-RequiredText -Value $row.canonical_name -Label "administrative unit '$id'.canonical_name" | Out-Null
    Test-RequiredHan140YearRange -FromValue $row.valid_from_year -ToValue $row.valid_to_year -Label "administrative unit '$id'"
    Test-EnumValue -Value ([string]$row.confidence) `
        -Allowed @("high", "medium", "low", "unknown") `
        -Label "administrative unit '$id'.confidence"

    $parentId = ([string]$row.parent_admin_unit_id).Trim()
    $adminParents[$id] = $parentId
}

foreach ($row in $adminTable.Rows) {
    $id = [string]$row.admin_unit_id
    $parentId = ([string]$row.parent_admin_unit_id).Trim()
    if (-not [string]::IsNullOrWhiteSpace($parentId) -and -not $adminIds.Contains($parentId)) {
        Add-ValidationError "Administrative unit '$id' references missing parent '$parentId'."
    }

    $seatId = ([string]$row.seat_admin_unit_id).Trim()
    if (-not [string]::IsNullOrWhiteSpace($seatId) -and -not $adminIds.Contains($seatId)) {
        Add-ValidationError "Administrative unit '$id' references missing seat '$seatId'."
    }

    Test-References -Ids (Get-IdList -Value $row.source_ids) -KnownIds $sourceIds `
        -Label "administrative unit '$id'.source_ids" -RequireAtLeastOne
}
Test-ParentCycles -Parents $adminParents -Label "administrative unit hierarchy"

$populationIds = New-OrdinalIdSet
$rawHouseholdsTotal = [long]0
$rawPopulationTotal = [long]0
$explicitCorrectedHouseholdsTotal = [long]0
$explicitCorrectedPopulationTotal = [long]0
$effectiveHouseholdsTotal = [long]0
$effectivePopulationTotal = [long]0
$missingRawHouseholds = 0
$missingRawPopulation = 0
$recordsWithCorrections = 0

foreach ($row in $populationTable.Rows) {
    $id = [string]$row.admin_unit_id
    if (-not $adminIds.Contains($id)) {
        Add-ValidationError "Population record references missing administrative unit '$id'."
    }
    if (-not $populationIds.Add($id)) {
        Add-ValidationError "Duplicate population record for administrative unit '$id'."
    }

    $rawHouseholds = Get-NullableNonNegativeInteger -Value $row.registered_households_raw -Label "population '$id'.registered_households_raw"
    $rawPopulation = Get-NullableNonNegativeInteger -Value $row.registered_population_raw -Label "population '$id'.registered_population_raw"
    $correctedHouseholds = Get-NullableNonNegativeInteger -Value $row.registered_households_corrected -Label "population '$id'.registered_households_corrected"
    $correctedPopulation = Get-NullableNonNegativeInteger -Value $row.registered_population_corrected -Label "population '$id'.registered_population_corrected"

    if ($null -eq $rawHouseholds) {
        $missingRawHouseholds++
    }
    else {
        $rawHouseholdsTotal += $rawHouseholds
    }
    if ($null -eq $rawPopulation) {
        $missingRawPopulation++
    }
    else {
        $rawPopulationTotal += $rawPopulation
    }
    if ($null -ne $correctedHouseholds) {
        $explicitCorrectedHouseholdsTotal += $correctedHouseholds
    }
    if ($null -ne $correctedPopulation) {
        $explicitCorrectedPopulationTotal += $correctedPopulation
    }

    if ($null -ne $correctedHouseholds -or $null -ne $correctedPopulation) {
        $recordsWithCorrections++
        Test-RequiredText -Value $row.correction_code -Label "population '$id'.correction_code" | Out-Null
        Test-RequiredText -Value $row.correction_note -Label "population '$id'.correction_note" | Out-Null
    }
    elseif (-not [string]::IsNullOrWhiteSpace([string]$row.correction_code) -or
        -not [string]::IsNullOrWhiteSpace([string]$row.correction_note)) {
        Add-ValidationError "Population '$id' has correction metadata without a corrected value."
    }

    if ($null -ne $correctedHouseholds) {
        $effectiveHouseholdsTotal += $correctedHouseholds
    }
    elseif ($null -ne $rawHouseholds) {
        $effectiveHouseholdsTotal += $rawHouseholds
    }

    if ($null -ne $correctedPopulation) {
        $effectivePopulationTotal += $correctedPopulation
    }
    elseif ($null -ne $rawPopulation) {
        $effectivePopulationTotal += $rawPopulation
    }

    Test-EnumValue -Value ([string]$row.evidence_grade) `
        -Allowed @("H", "R", "M", "I", "R/M") `
        -Label "population '$id'.evidence_grade"
    Test-References -Ids (Get-IdList -Value $row.source_ids) -KnownIds $sourceIds `
        -Label "population '$id'.source_ids" -RequireAtLeastOne
    Test-RequiredText -Value $row.source_locator -Label "population '$id'.source_locator" | Out-Null
    if ([string]$row.model_version -cne "han140.p0.v1") {
        Add-ValidationError "Population '$id'.model_version must be 'han140.p0.v1'."
    }
}

$regionIds = New-OrdinalIdSet
$regionParents = @{}
$provisionalRegions = 0
foreach ($row in $regionTable.Rows) {
    $id = [string]$row.stable_region_id
    if (Test-IdFormat -Value $id -Pattern "^geo\.region\.[a-z0-9]+(?:\.[a-z0-9]+)*$" -Label "stable region ID") {
        if (-not $regionIds.Add($id)) {
            Add-ValidationError "Duplicate stable region ID '$id'."
        }
    }

    Test-EnumValue -Value ([string]$row.region_type) `
        -Allowed @("macroregion", "province_area", "commandery_area", "county_area", "city_circle", "other") `
        -Label "stable region '$id'.region_type"
    Test-RequiredText -Value $row.canonical_name -Label "stable region '$id'.canonical_name" | Out-Null
    Test-NullableCoordinate -Value $row.centroid_latitude -Minimum -90 -Maximum 90 -Label "stable region '$id'.centroid_latitude"
    Test-NullableCoordinate -Value $row.centroid_longitude -Minimum -180 -Maximum 180 -Label "stable region '$id'.centroid_longitude"
    Test-EnumValue -Value ([string]$row.geometry_status) `
        -Allowed @("none", "approximate", "provisional", "verified") `
        -Label "stable region '$id'.geometry_status"
    Test-EnumValue -Value ([string]$row.confidence) `
        -Allowed @("high", "medium", "low", "unknown") `
        -Label "stable region '$id'.confidence"
    Test-BooleanValue -Value ([string]$row.provisional) -Label "stable region '$id'.provisional"
    if ([string]$row.provisional -ceq "true") {
        $provisionalRegions++
    }

    $regionParents[$id] = ([string]$row.parent_stable_region_id).Trim()
}

foreach ($row in $regionTable.Rows) {
    $id = [string]$row.stable_region_id
    $parentId = ([string]$row.parent_stable_region_id).Trim()
    if (-not [string]::IsNullOrWhiteSpace($parentId) -and -not $regionIds.Contains($parentId)) {
        Add-ValidationError "Stable region '$id' references missing parent '$parentId'."
    }
}
Test-ParentCycles -Parents $regionParents -Label "stable region hierarchy"

$mappingKeys = New-OrdinalIdSet
$mappingWeights = @{}
$provisionalMappings = 0
foreach ($row in $mappingTable.Rows) {
    $sourceId = [string]$row.source_id
    $targetId = [string]$row.target_id
    $key = "$sourceId|$targetId|$([string]$row.relation_type)"
    if (-not $mappingKeys.Add($key)) {
        Add-ValidationError "Duplicate region mapping '$key'."
    }
    if (-not $adminIds.Contains($sourceId)) {
        Add-ValidationError "Region mapping references missing administrative source '$sourceId'."
    }
    if (-not $regionIds.Contains($targetId)) {
        Add-ValidationError "Region mapping references missing stable target '$targetId'."
    }

    Test-RequiredText -Value $row.relation_type -Label "region mapping '$key'.relation_type" | Out-Null
    Test-RequiredHan140YearRange -FromValue $row.valid_from_year -ToValue $row.valid_to_year -Label "region mapping '$key'"
    $weight = Get-NullableNonNegativeInteger -Value $row.weight_basis_points -Label "region mapping '$key'.weight_basis_points" -Required
    if ($null -ne $weight) {
        if ($weight -gt 10000) {
            Add-ValidationError "Region mapping '$key'.weight_basis_points must not exceed 10000."
        }
        if (-not $mappingWeights.ContainsKey($sourceId)) {
            $mappingWeights[$sourceId] = [long]0
        }
        $mappingWeights[$sourceId] = [long]$mappingWeights[$sourceId] + $weight
    }
    Test-RequiredText -Value $row.mapping_method -Label "region mapping '$key'.mapping_method" | Out-Null
    Test-EnumValue -Value ([string]$row.confidence) `
        -Allowed @("high", "medium", "low", "unknown") `
        -Label "region mapping '$key'.confidence"
    Test-BooleanValue -Value ([string]$row.provisional) -Label "region mapping '$key'.provisional"
    if ([string]$row.provisional -ceq "true") {
        $provisionalMappings++
    }
}

$weightErrorCount = 0
foreach ($sourceId in @($mappingWeights.Keys | Sort-Object)) {
    if ([long]$mappingWeights[$sourceId] -ne 10000) {
        $weightErrorCount++
        Add-ValidationError "Region mappings for '$sourceId' sum to $($mappingWeights[$sourceId]) basis points instead of 10000."
    }
}

$crosswalkIds = New-OrdinalIdSet
$unresolvedGameLocations = 0
foreach ($row in $crosswalkTable.Rows) {
    $gameId = [string]$row.game_location_id
    $kind = [string]$row.game_location_kind
    $pattern = switch ($kind) {
        "runtime" { "^location\.[a-z0-9]+(?:\.[a-z0-9]+)*$" }
        "prototype_catalog" { "^L[0-9]{3}$" }
        "city_catalog" { "^C[0-9]{3}$" }
        default { "^(?!)$" }
    }
    Test-EnumValue -Value $kind `
        -Allowed @("runtime", "prototype_catalog", "city_catalog") `
        -Label "game location '$gameId'.game_location_kind"
    if (-not (Test-IdFormat -Value $gameId -Pattern $pattern -Label "game location ID")) {
        continue
    }
    if (-not $crosswalkIds.Add($gameId)) {
        Add-ValidationError "Duplicate game location crosswalk ID '$gameId'."
    }

    $status = [string]$row.mapping_status
    Test-EnumValue -Value $status `
        -Allowed @("exact", "aggregate", "approximate", "unresolved") `
        -Label "game location '$gameId'.mapping_status"
    Test-EnumValue -Value ([string]$row.confidence) `
        -Allowed @("high", "medium", "low", "unknown") `
        -Label "game location '$gameId'.confidence"
    Test-BooleanValue -Value ([string]$row.provisional) -Label "game location '$gameId'.provisional"
    Test-OptionalYearRange -FromValue $row.valid_from_year -ToValue $row.valid_to_year -Label "game location '$gameId'"

    $stableId = ([string]$row.stable_region_id).Trim()
    if (-not [string]::IsNullOrWhiteSpace($stableId) -and -not $regionIds.Contains($stableId)) {
        Add-ValidationError "Game location '$gameId' references missing stable region '$stableId'."
    }
    $adminId = ([string]$row.admin_unit_id).Trim()
    if (-not [string]::IsNullOrWhiteSpace($adminId) -and -not $adminIds.Contains($adminId)) {
        Add-ValidationError "Game location '$gameId' references missing administrative unit '$adminId'."
    }

    if ($status -ceq "unresolved") {
        $unresolvedGameLocations++
    }
    else {
        if ([string]::IsNullOrWhiteSpace($stableId)) {
            Add-ValidationError "Resolved game location '$gameId' must reference a stable region."
        }
        Test-References -Ids (Get-IdList -Value $row.source_ids) -KnownIds $sourceIds `
            -Label "game location '$gameId'.source_ids" -RequireAtLeastOne
    }
}

if ($script:ValidationErrors.Count -gt 0) {
    $orderedErrors = @($script:ValidationErrors | Sort-Object -Unique)
    foreach ($validationError in $orderedErrors) {
        Write-Error $validationError -ErrorAction Continue
    }
    Write-Host "RESULT han140-validation=failed errors=$($orderedErrors.Count)"
    exit 1
}

$audit = [ordered]@{
    schema_version = "han140.audit.v1"
    dataset_year = 140
    validation_status = "passed"
    national_anchor = [ordered]@{
        registered_households = $anchorHouseholds
        registered_population = $anchorPopulation
    }
    row_counts = [ordered]@{
        sources = $sourceRows.Count
        administrative_units = $adminTable.Rows.Count
        population_records = $populationTable.Rows.Count
        stable_regions = $regionTable.Rows.Count
        region_mappings = $mappingTable.Rows.Count
        game_location_crosswalks = $crosswalkTable.Rows.Count
    }
    population_totals = [ordered]@{
        raw_households = $rawHouseholdsTotal
        raw_population = $rawPopulationTotal
        explicit_corrected_households = $explicitCorrectedHouseholdsTotal
        explicit_corrected_population = $explicitCorrectedPopulationTotal
        effective_households = $effectiveHouseholdsTotal
        effective_population = $effectivePopulationTotal
        household_difference_from_anchor = $effectiveHouseholdsTotal - $anchorHouseholds
        population_difference_from_anchor = $effectivePopulationTotal - $anchorPopulation
    }
    data_quality = [ordered]@{
        records_missing_raw_households = $missingRawHouseholds
        records_missing_raw_population = $missingRawPopulation
        records_with_corrections = $recordsWithCorrections
        provisional_stable_regions = $provisionalRegions
        provisional_region_mappings = $provisionalMappings
        unresolved_game_locations = $unresolvedGameLocations
    }
    mapping_audit = [ordered]@{
        mapped_admin_source_count = $mappingWeights.Count
        weight_error_count = $weightErrorCount
    }
    registered_source_ids = @($sourceIds | Sort-Object)
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory)) {
    [void](New-Item -ItemType Directory -Path $outputDirectory -Force)
}

$json = ($audit | ConvertTo-Json -Depth 8) -replace "`r`n", "`n"
[System.IO.File]::WriteAllText($resolvedOutputPath, $json + "`n", $script:Utf8NoBom)

Write-Host (
    "RESULT han140-validation=passed sources={0} admin={1} population={2} regions={3} mappings={4} crosswalks={5}" -f
    $sourceRows.Count,
    $adminTable.Rows.Count,
    $populationTable.Rows.Count,
    $regionTable.Rows.Count,
    $mappingTable.Rows.Count,
    $crosswalkTable.Rows.Count
)
exit 0
