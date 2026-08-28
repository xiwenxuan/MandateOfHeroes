param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectRoot `
        "Assets\ArtSource\Han\Luoyang\P0Final\luoyang_p0_final_source_archive_manifest_v1.json"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $projectRoot $OutputPath
}

function Get-FileRecord {
    param([string]$RelativePath)
    $normalized = $RelativePath.Replace('\', '/')
    $fullPath = Join-Path $projectRoot ($normalized.Replace('/', '\'))
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Required final-source file is missing: $fullPath"
    }
    return [ordered]@{
        path = $normalized
        length = (Get-Item -LiteralPath $fullPath).Length
        sha256 = (Get-FileHash -LiteralPath $fullPath `
            -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

$catalogPath = "Assets/StreamingAssets/WorldMap/" +
    "LuoyangP0FinalAssetVerticalSliceV1/" +
    "luoyang_p0_final_asset_vertical_slice_v1.json"
$catalogFullPath = Join-Path $projectRoot ($catalogPath.Replace('/', '\'))
$catalog = Get-Content -LiteralPath $catalogFullPath -Raw -Encoding UTF8 |
    ConvertFrom-Json
if ($catalog.ProfileCount -ne 4 -or $catalog.MaterialCount -ne 6 -or
    $catalog.Profiles.Count -ne 4) {
    throw "P0 catalog coverage is invalid."
}
if ($catalog.UserReviewDecisionStatusId -ne
        "user_review.luoyang-p0-four-piece.accepted.v1" -or
    $catalog.UserReviewDecisionRecordId -ne
        "decision.luoyang-p0-four-piece.accepted.2026-08-27.v1" -or
    $catalog.SourceArchiveStatusId -ne
        "source_archive.unity_native_and_fbx_complete.v1" -or
    $catalog.FinalArtApprovalStatusId -ne
        "final_art.user_accepted.fbx_source_validated.approved.v1" -or
    $catalog.FbxSourceToolchainId -ne
        "toolchain.unity-fbx-exporter.4.2.1" -or
    $catalog.FbxToolchainLicenseId -ne
        "license.unity-companion.v1" -or
    $catalog.FbxAnchorNameMappingId -ne
        "anchor_name.dot_to_underscore.unity_fbx_exporter.v1") {
    throw "P0 final-source catalog gate is invalid."
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
$assetPaths.Add("Assets/Editor/Mandate.Editor/LuoyangP0NativePrefabArtBuilder.cs")
$assetPaths.Add("Assets/Editor/Mandate.Editor/LuoyangP0FbxSourceExporter.cs")
$assetPaths.Add($catalogPath)
$assetPaths.AddRange([string[]]@(
    "Assets/Resources/Art/Han/Luoyang/P0Final/SouthPalace.prefab",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Mingtang.prefab",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Guangyangmen.prefab",
    "Assets/Resources/Art/Han/Luoyang/P0Final/NorthPalaceSouthGate.prefab"
))
$assetPaths.AddRange([string[]]@(
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/RammedEarth.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/Vermilion.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/GreyGreenTile.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/Stone.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/Timber.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/Bronze.mat"
))
$assetPaths.AddRange([string[]]@(
    "Assets/Resources/Art/Han/Luoyang/P0Final/Meshes/NativeBox.asset",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Meshes/NativeHanHipRoof.asset",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Meshes/NativeOctagonalPost.asset",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Meshes/NativeRoadCrown.asset"
))
foreach ($profile in $catalog.Profiles) {
    $assetPaths.Add([string]$profile.ArtistFbxTargetPath)
}

$uniqueAssets = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
$sourceFiles = [System.Collections.Generic.List[object]]::new()
foreach ($assetPath in ($assetPaths | Sort-Object)) {
    if (-not $uniqueAssets.Add($assetPath)) {
        throw "Duplicate final-source path: $assetPath"
    }
    $sourceFiles.Add((Get-FileRecord $assetPath))
    $sourceFiles.Add((Get-FileRecord ($assetPath + ".meta")))
}
if ($sourceFiles.Count -ne 42) {
    throw "Expected 42 final source and metadata files, found $($sourceFiles.Count)."
}

$pieces = [System.Collections.Generic.List[object]]::new()
foreach ($profile in ($catalog.Profiles | Sort-Object FacilityId)) {
    if (-not $profile.ArtistPrefabPresent -or
        -not $profile.FinalArtApproved -or
        $profile.CandidateStatusId -ne
        "candidate.native_prefab_refined_v2.user_accepted.fbx_source_validated.final") {
        throw "Invalid final P0 profile status: $($profile.CandidateId)"
    }
    $fbxRecord = Get-FileRecord ([string]$profile.ArtistFbxTargetPath)
    $anchorMappings = [System.Collections.Generic.List[object]]::new()
    foreach ($anchor in $profile.Anchors) {
        $anchorMappings.Add([ordered]@{
            stable_anchor_id = [string]$anchor.AnchorId
            fbx_node_name = ([string]$anchor.AnchorId).Replace('.', '_')
        })
    }
    $pieces.Add([ordered]@{
        facility_id = $profile.FacilityId
        candidate_id = $profile.CandidateId
        replacement_slot_id = $profile.ReplacementSlotId
        user_review_decision = "ACCEPTED"
        artist_prefab_resource_path = $profile.ArtistPrefabResourcePath
        fbx_source_path = $profile.ArtistFbxTargetPath
        fbx_length = $fbxRecord.length
        fbx_sha256 = $fbxRecord.sha256
        fbx_source_status = "PRESENT_UNITY_REIMPORT_VALIDATED"
        anchor_name_mapping_id = $catalog.FbxAnchorNameMappingId
        anchor_mappings = $anchorMappings
        final_art_approved = [bool]$profile.FinalArtApproved
    })
}

$manifest = [ordered]@{
    schema_version = 1
    contract_id =
        "art_source.luoyang.p0-four-piece.fbx-source-freeze-and-final-activation.v1"
    task_id =
        "LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1"
    user_review_decision_status_id = $catalog.UserReviewDecisionStatusId
    user_review_decision_record_id = $catalog.UserReviewDecisionRecordId
    user_review_decision_date = $catalog.UserReviewDecisionDate
    source_archive_status_id = $catalog.SourceArchiveStatusId
    final_art_approval_status_id = $catalog.FinalArtApprovalStatusId
    source_license_id = $catalog.SourceLicenseId
    fbx_source_toolchain_id = $catalog.FbxSourceToolchainId
    fbx_toolchain_license_id = $catalog.FbxToolchainLicenseId
    fbx_anchor_name_mapping_id = $catalog.FbxAnchorNameMappingId
    user_review_decision = "ACCEPTED_ALL_FOUR"
    final_source_file_count = $sourceFiles.Count
    toolchain_file_count = 2
    fbx_source_count = $pieces.Count
    fbx_missing_count = 0
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
    final_source_files = $sourceFiles
    pieces = $pieces
}

$outputDirectory = Split-Path -Parent $OutputPath
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$json = $manifest | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($OutputPath, $json + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Output ("RESULT status=passed accepted=4 source_files=" +
    "$($sourceFiles.Count) fbx_present=$($pieces.Count) " +
    "final_art_approved=true")
Write-Output "Manifest: $OutputPath"
