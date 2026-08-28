param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectRoot `
        "Assets\ArtSource\Han\Luoyang\P0Batch2\luoyang_p0_landmark_second_batch_source_manifest_v1.json"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $projectRoot $OutputPath
}

function Get-FileRecord {
    param([string]$RelativePath)
    $normalized = $RelativePath.Replace('\', '/')
    $fullPath = Join-Path $projectRoot ($normalized.Replace('/', '\'))
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Required second-batch source file is missing: $fullPath"
    }
    return [ordered]@{
        path = $normalized
        length = (Get-Item -LiteralPath $fullPath).Length
        sha256 = (Get-FileHash -LiteralPath $fullPath `
            -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

$catalogPath = "Assets/StreamingAssets/WorldMap/" +
    "LuoyangP0LandmarkSecondBatchV1/" +
    "luoyang_p0_landmark_second_batch_v1.json"
$catalog = Get-Content -LiteralPath (Join-Path $projectRoot `
        ($catalogPath.Replace('/', '\'))) -Raw -Encoding UTF8 |
    ConvertFrom-Json
if ($catalog.ProfileCount -ne 4 -or $catalog.Profiles.Count -ne 4 -or
    $catalog.StatusId -ne
        "LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1" -or
    $catalog.ReviewDecisionStatusId -ne
        "user_review.luoyang-p0-landmark-second-batch.accepted.v1" -or
    $catalog.UserReviewDecisionRecordId -ne
        "decision.luoyang-p0-landmark-second-batch.accepted.2026-08-27.v1" -or
    $catalog.UserReviewDecisionDate -ne "2026-08-27" -or
    $catalog.FinalArtApprovalStatusId -ne
        "final_art.user_accepted.fbx_source_validated.approved.v1" -or
    $catalog.SourceArchiveStatusId -ne
        "source_archive.unity_native_and_fbx_complete.v1") {
    throw "Second-batch accepted final-activation catalog gate is invalid."
}

$expectedOrders = @(1, 2, 3, 5)
$actualOrders = @($catalog.Profiles | Sort-Object ReviewOrder |
    ForEach-Object { [int]$_.ReviewOrder })
if (@(Compare-Object $expectedOrders $actualOrders).Count -ne 0) {
    throw "Second-batch selection no longer contains review orders 1,2,3,5."
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
$assetPaths.Add("Assets/Scripts/Mandate.Domain/LuoyangP0LandmarkSecondBatchState.cs")
$assetPaths.Add("Assets/Scripts/Mandate.Persistence/LuoyangP0LandmarkSecondBatchSource.cs")
$assetPaths.Add("Assets/Editor/Mandate.Editor/LuoyangP0LandmarkSecondBatchArtBuilder.cs")
$assetPaths.Add("Assets/Editor/Mandate.Editor/LuoyangP0LandmarkSecondBatchFbxExporter.cs")
$assetPaths.Add($catalogPath)
$assetPaths.AddRange([string[]]@(
    "Assets/Resources/Art/Han/Luoyang/P0Batch2/NorthPalace.prefab",
    "Assets/Resources/Art/Han/Luoyang/P0Batch2/YonganPalace.prefab",
    "Assets/Resources/Art/Han/Luoyang/P0Batch2/Taixue.prefab",
    "Assets/Resources/Art/Han/Luoyang/P0Batch2/Biyong.prefab",
    "Assets/Resources/Art/Han/Luoyang/P0Batch2/Materials/Water.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Batch2/Materials/Foliage.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Batch2/Meshes/NativeWaterDisc.asset",
    "Assets/Resources/Art/Han/Luoyang/P0Batch2/Meshes/NativeTreeCanopy.asset",
    "Assets/Resources/Art/Han/Luoyang/P0Batch2/Meshes/NativeRitualRing.asset"
))
$assetPaths.AddRange([string[]]@(
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/RammedEarth.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/Vermilion.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/GreyGreenTile.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/Stone.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/Timber.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Materials/Bronze.mat",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Meshes/NativeBox.asset",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Meshes/NativeHanHipRoof.asset",
    "Assets/Resources/Art/Han/Luoyang/P0Final/Meshes/NativeOctagonalPost.asset"
))
foreach ($profile in $catalog.Profiles) {
    $assetPaths.Add([string]$profile.ArtistFbxTargetPath)
}

$uniqueAssets = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
$sourceFiles = [System.Collections.Generic.List[object]]::new()
foreach ($assetPath in ($assetPaths | Sort-Object)) {
    if (-not $uniqueAssets.Add($assetPath)) {
        throw "Duplicate second-batch source path: $assetPath"
    }
    $sourceFiles.Add((Get-FileRecord $assetPath))
    $sourceFiles.Add((Get-FileRecord ($assetPath + ".meta")))
}
if ($sourceFiles.Count -ne 54) {
    throw "Expected 54 source and metadata files, found $($sourceFiles.Count)."
}

$pieces = [System.Collections.Generic.List[object]]::new()
foreach ($profile in ($catalog.Profiles | Sort-Object ReviewOrder)) {
    if (-not $profile.ArtistPrefabPresent -or -not $profile.FinalArtApproved -or
        $profile.CandidateStatusId -ne
        "candidate.native_prefab_fbx_source_validated.user_accepted.final_art_activated.v1") {
        throw "Invalid accepted final-activation profile status: $($profile.CandidateId)"
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
        review_order = [int]$profile.ReviewOrder
        facility_id = [string]$profile.FacilityId
        candidate_id = [string]$profile.CandidateId
        replacement_slot_id = [string]$profile.ReplacementSlotId
        user_review_decision = "ACCEPTED"
        artist_prefab_resource_path = [string]$profile.ArtistPrefabResourcePath
        fbx_source_path = [string]$profile.ArtistFbxTargetPath
        fbx_length = $fbxRecord.length
        fbx_sha256 = $fbxRecord.sha256
        fbx_source_status = "PRESENT_UNITY_REIMPORT_VALIDATED"
        anchor_name_mapping_id = [string]$catalog.FbxAnchorNameMappingId
        anchor_mappings = $anchorMappings
        final_art_approved = $true
    })
}

$manifest = [ordered]@{
    schema_version = 1
    contract_id =
        "art_source.luoyang.p0-landmark-second-batch.user-accepted-fbx-source-validated-final-activation.v1"
    task_id =
        "LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1"
    status_id = [string]$catalog.StatusId
    selection_policy_id = [string]$catalog.SelectionPolicyId
    review_decision_status_id = [string]$catalog.ReviewDecisionStatusId
    user_review_decision_record_id =
        [string]$catalog.UserReviewDecisionRecordId
    user_review_decision_date = [string]$catalog.UserReviewDecisionDate
    final_art_approval_status_id = [string]$catalog.FinalArtApprovalStatusId
    source_archive_status_id = [string]$catalog.SourceArchiveStatusId
    source_license_id = [string]$catalog.SourceLicenseId
    fbx_source_toolchain_id = [string]$catalog.FbxSourceToolchainId
    fbx_toolchain_license_id = [string]$catalog.FbxToolchainLicenseId
    fbx_anchor_name_mapping_id = [string]$catalog.FbxAnchorNameMappingId
    user_review_decision = "ACCEPTED_ALL_FOUR"
    source_file_count = $sourceFiles.Count
    toolchain_file_count = 2
    fbx_source_count = $pieces.Count
    fbx_missing_count = 0
    user_review_ready = $true
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

Write-Output ("RESULT status=passed accepted=4 source_files=" +
    "$($sourceFiles.Count) fbx_present=$($pieces.Count) " +
    "final_art_approved=true")
Write-Output "Manifest: $OutputPath"
