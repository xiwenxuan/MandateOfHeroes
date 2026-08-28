param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectRoot `
        "Assets\ArtSource\Han\Luoyang\P0Final\luoyang_p0_source_archive_manifest_v1.json"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $projectRoot $OutputPath
}

function Get-RelativeProjectPath {
    param([string]$Path)
    $resolved = [System.IO.Path]::GetFullPath($Path)
    $prefix = $projectRoot.TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($prefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside project root: $resolved"
    }
    return $resolved.Substring($prefix.Length).Replace('\', '/')
}

function Get-FileRecord {
    param([string]$RelativePath)
    $fullPath = Join-Path $projectRoot ($RelativePath.Replace('/', '\'))
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Required Unity-native source file is missing: $fullPath"
    }
    return [ordered]@{
        path = $RelativePath.Replace('\', '/')
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
    $catalog.UserReviewDecisionDate -ne "2026-08-27" -or
    $catalog.SourceArchiveStatusId -ne
        "source_archive.unity_native_complete.independent_dcc_fbx_missing.v1") {
    throw "P0 catalog user-decision or source-archive status is invalid."
}

$assetPaths = [System.Collections.Generic.List[string]]::new()
$assetPaths.Add("Assets/Editor/Mandate.Editor/LuoyangP0NativePrefabArtBuilder.cs")
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

$uniqueAssets = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
$sourceFiles = [System.Collections.Generic.List[object]]::new()
foreach ($assetPath in ($assetPaths | Sort-Object)) {
    if (-not $uniqueAssets.Add($assetPath)) {
        throw "Duplicate Unity-native source path: $assetPath"
    }
    $sourceFiles.Add((Get-FileRecord $assetPath))
    $sourceFiles.Add((Get-FileRecord ($assetPath + ".meta")))
}
if ($sourceFiles.Count -ne 32) {
    throw "Expected 32 Unity-native source and metadata files, found $($sourceFiles.Count)."
}

$pieces = [System.Collections.Generic.List[object]]::new()
$missingFbxCount = 0
foreach ($profile in ($catalog.Profiles | Sort-Object FacilityId)) {
    if (-not $profile.ArtistPrefabPresent -or $profile.FinalArtApproved -or
        $profile.CandidateStatusId -ne
        "candidate.native_prefab_refined_v2.user_accepted.source_archive_pending") {
        throw "Invalid accepted P0 profile status: $($profile.CandidateId)"
    }
    $fbxPath = [string]$profile.ArtistFbxTargetPath
    $fbxFullPath = Join-Path $projectRoot ($fbxPath.Replace('/', '\'))
    $fbxExists = Test-Path -LiteralPath $fbxFullPath -PathType Leaf
    if (-not $fbxExists) {
        $missingFbxCount++
    }
    $pieces.Add([ordered]@{
        facility_id = $profile.FacilityId
        candidate_id = $profile.CandidateId
        replacement_slot_id = $profile.ReplacementSlotId
        user_review_decision = "ACCEPTED"
        artist_prefab_resource_path = $profile.ArtistPrefabResourcePath
        independent_fbx_target_path = $fbxPath
        independent_fbx_exists = [bool]$fbxExists
        independent_fbx_status = if ($fbxExists) {
            "PRESENT_REQUIRES_CONSISTENCY_VALIDATION"
        } else {
            "MISSING_REQUIRED_FOR_FINAL_ART_ACTIVATION"
        }
        final_art_approved = $false
    })
}

$manifest = [ordered]@{
    schema_version = 1
    contract_id =
        "art_source.luoyang.p0-four-piece.user-acceptance-and-source-readiness.v1"
    task_id =
        "LUOYANG_P0_FOUR_PIECE_USER_ACCEPTANCE_AND_SOURCE_ARCHIVE_READINESS_V1"
    user_review_decision_status_id = $catalog.UserReviewDecisionStatusId
    user_review_decision_record_id = $catalog.UserReviewDecisionRecordId
    user_review_decision_date = $catalog.UserReviewDecisionDate
    source_archive_status_id = $catalog.SourceArchiveStatusId
    source_license_id = $catalog.SourceLicenseId
    user_review_decision = "ACCEPTED_ALL_FOUR"
    unity_native_source_file_count = $sourceFiles.Count
    independent_fbx_target_count = $pieces.Count
    independent_fbx_missing_count = $missingFbxCount
    final_art_activation_ready = ($missingFbxCount -eq 0)
    final_art_approved = $false
    unity_native_source_files = $sourceFiles
    pieces = $pieces
}

$outputDirectory = Split-Path -Parent $OutputPath
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$json = $manifest | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($OutputPath, $json + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Output ("RESULT status=passed accepted=4 source_files=" +
    "$($sourceFiles.Count) fbx_missing=$missingFbxCount final_art_approved=false")
Write-Output "Manifest: $OutputPath"
