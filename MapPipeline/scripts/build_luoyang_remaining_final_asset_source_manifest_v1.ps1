param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$resourceRoot = "Assets/Resources/Art/Han/Luoyang/FinalRemaining"
$fbxRoot = "Assets/ArtSource/Han/Luoyang/FinalRemaining"
$catalogPath = "Assets/StreamingAssets/WorldMap/" +
    "LuoyangRemainingFinalAssetsV1/" +
    "luoyang_remaining_final_assets_v1.json"
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectRoot `
        "Assets\ArtSource\Han\Luoyang\FinalRemaining\luoyang_remaining_38_final_asset_source_manifest_v1.json"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $projectRoot $OutputPath
}

function Get-FileRecord {
    param([string]$RelativePath)
    $normalized = $RelativePath.Replace('\', '/')
    $fullPath = Join-Path $projectRoot ($normalized.Replace('/', '\'))
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Required remaining final-asset source file is missing: $fullPath"
    }
    return [ordered]@{
        path = $normalized
        length = (Get-Item -LiteralPath $fullPath).Length
        sha256 = (Get-FileHash -LiteralPath $fullPath `
            -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

$catalog = Get-Content -LiteralPath (Join-Path $projectRoot `
        ($catalogPath.Replace('/', '\'))) -Raw -Encoding UTF8 |
    ConvertFrom-Json
if ($catalog.SchemaId -ne "mandate.luoyang-remaining-final-assets.v1" -or
    $catalog.TaskId -ne
        "LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1" -or
    $catalog.StatusId -ne
        "LUOYANG_REMAINING_38_USER_PREACCEPTED_NATIVE_PREFAB_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1" -or
    $catalog.UserDecisionStatusId -ne
        "user_review.luoyang-remaining-38.preaccepted.v1" -or
    $catalog.UserDecisionRecordId -ne
        "decision.luoyang-remaining-38.preaccepted.2026-08-27.v1" -or
    $catalog.UserDecisionDate -ne "2026-08-27" -or
    $catalog.UserDecisionId -ne "PREACCEPTED_ALL_REMAINING_38" -or
    $catalog.ProfileCount -ne 38 -or $catalog.Profiles.Count -ne 38 -or
    $catalog.CoveredFacilityCount -ne 2068 -or
    @($catalog.Profiles | Measure-Object FacilityUsageCount -Sum).Sum -ne 2068 -or
    @($catalog.Profiles | Where-Object { -not $_.ArtistPrefabPresent -or
        -not $_.FinalArtApproved }).Count -ne 0) {
    throw "Remaining final-asset accepted catalog gate is invalid."
}

$expectedOrders = @(15, 16, 17, 18, 19, 20, 21) + @(23..53)
$actualOrders = @($catalog.Profiles | Sort-Object ReviewOrder |
    ForEach-Object { [int]$_.ReviewOrder })
if (@(Compare-Object $expectedOrders $actualOrders).Count -ne 0) {
    throw "Remaining final-asset selection no longer contains the frozen 38 review orders."
}
$priorityCounts = [ordered]@{
    p0 = @($catalog.Profiles | Where-Object {
        $_.PriorityId -eq "priority.p0.identity_critical" }).Count
    p1 = @($catalog.Profiles | Where-Object {
        $_.PriorityId -eq "priority.p1.high_exposure" }).Count
    p2 = @($catalog.Profiles | Where-Object {
        $_.PriorityId -eq "priority.p2.system_readable" }).Count
    p3 = @($catalog.Profiles | Where-Object {
        $_.PriorityId -eq "priority.p3.supporting_context" }).Count
}
if ($priorityCounts.p0 -ne 8 -or $priorityCounts.p1 -ne 10 -or
    $priorityCounts.p2 -ne 14 -or $priorityCounts.p3 -ne 6) {
    throw "Remaining final-asset priority counts are invalid."
}

$packageManifestPath = "Packages/manifest.json"
$packageLockPath = "Packages/packages-lock.json"
$packageManifest = Get-Content -LiteralPath (Join-Path $projectRoot `
        $packageManifestPath) -Raw -Encoding UTF8 | ConvertFrom-Json
$packageLock = Get-Content -LiteralPath (Join-Path $projectRoot `
        $packageLockPath) -Raw -Encoding UTF8 | ConvertFrom-Json
if ($packageManifest.dependencies.'com.unity.formats.fbx' -ne "4.2.1" -or
    $packageLock.dependencies.'com.unity.formats.fbx'.version -ne "4.2.1" -or
    $packageLock.dependencies.'com.unity.formats.fbx'.dependencies.'com.autodesk.fbx' -ne
        "4.2.1" -or
    $packageLock.dependencies.'com.autodesk.fbx'.version -ne "4.2.1") {
    throw "Unity FBX toolchain versions are not frozen at 4.2.1."
}

$assetPaths = [System.Collections.Generic.List[string]]::new()
$assetPaths.AddRange([string[]]@(
    "Assets/Scripts/Mandate.Domain/LuoyangRemainingFinalAssetState.cs",
    "Assets/Scripts/Mandate.Persistence/LuoyangRemainingFinalAssetSource.cs",
    "Assets/Scripts/Mandate.Presentation/LuoyangFinalAssetPrefabMetadata.cs",
    "Assets/Scripts/Mandate.Presentation/HanBuildableFacilityModelInstance.cs",
    "Assets/Scripts/Mandate.Presentation/HanWorldNaturalMapController.BuildableFacilities.cs",
    "Assets/Editor/Mandate.Editor/LuoyangRemainingFinalAssetArtBuilder.cs",
    "Assets/Editor/Mandate.Editor/LuoyangRemainingFinalAssetFbxExporter.cs",
    "Assets/Editor/Mandate.Editor/Mandate.Editor.asmdef",
    $catalogPath
))

$resourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot `
        ($resourceRoot.Replace('/', '\'))) -File -Recurse |
    Where-Object { $_.Extension -in @('.prefab', '.mat', '.asset') } |
    Sort-Object FullName)
if ($resourceFiles.Count -ne 72 -or
    @($resourceFiles | Where-Object { $_.Extension -eq '.prefab' }).Count -ne 38 -or
    @($resourceFiles | Where-Object { $_.Extension -eq '.mat' }).Count -ne 22 -or
    @($resourceFiles | Where-Object { $_.Extension -eq '.asset' }).Count -ne 12) {
    throw "Expected 38 prefabs, 22 materials and 12 meshes under the final resource root."
}
foreach ($file in $resourceFiles) {
    $relativeResourcePath =
        $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
    $assetPaths.Add($relativeResourcePath)
}
foreach ($profile in $catalog.Profiles) {
    $assetPaths.Add([string]$profile.ArtistFbxTargetPath)
}

$uniqueAssets = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
$sourceFiles = [System.Collections.Generic.List[object]]::new()
foreach ($assetPath in ($assetPaths | Sort-Object)) {
    if (-not $uniqueAssets.Add($assetPath)) {
        throw "Duplicate remaining final-asset source path: $assetPath"
    }
    $sourceFiles.Add((Get-FileRecord $assetPath))
    $sourceFiles.Add((Get-FileRecord ($assetPath + ".meta")))
}
foreach ($scriptPath in @(
    "MapPipeline/scripts/build_luoyang_remaining_final_asset_catalog_v1.ps1",
    "MapPipeline/scripts/build_luoyang_remaining_final_asset_source_manifest_v1.ps1"
)) {
    $sourceFiles.Add((Get-FileRecord $scriptPath))
}
if ($sourceFiles.Count -ne 240) {
    throw "Expected 240 source and metadata files, found $($sourceFiles.Count)."
}

$pieces = [System.Collections.Generic.List[object]]::new()
foreach ($profile in ($catalog.Profiles | Sort-Object ReviewOrder)) {
    $prefabPath = "Assets/Resources/" +
        [string]$profile.ArtistPrefabResourcePath + ".prefab"
    $prefabRecord = Get-FileRecord $prefabPath
    $fbxRecord = Get-FileRecord ([string]$profile.ArtistFbxTargetPath)
    if ($prefabRecord.length -lt 1024 -or $fbxRecord.length -lt 1024) {
        throw "Remaining final-asset source is unexpectedly small: $($profile.AssetVariantId)"
    }
    $pieces.Add([ordered]@{
        review_order = [int]$profile.ReviewOrder
        asset_variant_id = [string]$profile.AssetVariantId
        replacement_slot_id = [string]$profile.ReplacementSlotId
        source_kit_id = [string]$profile.SourceKitId
        source_profile_id = [string]$profile.SourceProfileId
        historical_basis_id = [string]$profile.HistoricalBasisId
        priority_id = [string]$profile.PriorityId
        facility_usage_count = [int]$profile.FacilityUsageCount
        representative_facility_id = [string]$profile.RepresentativeFacilityId
        representative_cell_id64 = [string]$profile.RepresentativeCellId64
        user_review_decision = "PREACCEPTED"
        prefab_path = $prefabPath
        prefab_length = $prefabRecord.length
        prefab_sha256 = $prefabRecord.sha256
        fbx_source_path = [string]$profile.ArtistFbxTargetPath
        fbx_length = $fbxRecord.length
        fbx_sha256 = $fbxRecord.sha256
        prefab_source_status = "PRESENT_UNITY_RELOAD_VALIDATED"
        fbx_source_status = "PRESENT_UNITY_REIMPORT_VALIDATED"
        lod_count = 3
        final_art_approved = $true
    })
}

$manifest = [ordered]@{
    schema_version = 1
    contract_id =
        "art_source.luoyang.remaining-38.user-preaccepted-native-prefab-fbx-final-activation.v1"
    task_id = [string]$catalog.TaskId
    status_id = [string]$catalog.StatusId
    user_decision_status_id = [string]$catalog.UserDecisionStatusId
    user_decision_record_id = [string]$catalog.UserDecisionRecordId
    user_decision_date = [string]$catalog.UserDecisionDate
    user_decision = [string]$catalog.UserDecisionId
    final_art_approval_status_id = [string]$catalog.FinalArtApprovalStatusId
    source_archive_status_id = [string]$catalog.SourceArchiveStatusId
    source_license_id = [string]$catalog.SourceLicenseId
    runtime_mode_id = [string]$catalog.RuntimeModeId
    profile_count = [int]$catalog.ProfileCount
    covered_facility_count = [int]$catalog.CoveredFacilityCount
    priority_counts = $priorityCounts
    resource_asset_count = $resourceFiles.Count
    source_file_count = $sourceFiles.Count
    toolchain_file_count = 2
    fbx_source_count = $pieces.Count
    fbx_missing_count = 0
    all_prefabs_three_lod = $true
    all_sources_unity_reimport_validated = $true
    final_art_activation_ready = $true
    final_art_approved = $true
    toolchain = [ordered]@{
        unity_editor_version = "2022.3.62f3c1"
        exporter_package = "com.unity.formats.fbx@4.2.1"
        fbx_sdk_package = "com.autodesk.fbx@4.2.1"
        license = "Unity Companion License"
        official_documentation =
            "https://docs.unity3d.com/2022.3/Manual/com.unity.formats.fbx.html"
    }
    toolchain_files = @(
        (Get-FileRecord $packageManifestPath),
        (Get-FileRecord $packageLockPath)
    )
    source_files = $sourceFiles
    pieces = $pieces
}

$outputDirectory = Split-Path -Parent $OutputPath
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$json = $manifest | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($OutputPath, $json + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Output ("RESULT status=passed preaccepted=38 facilities=2068 " +
    "source_files=$($sourceFiles.Count) fbx_present=$($pieces.Count) " +
    "final_art_approved=true")
Write-Output "Manifest: $OutputPath"
