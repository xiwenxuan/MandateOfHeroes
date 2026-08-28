param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$reviewPath = Join-Path $projectRoot `
    "Assets\StreamingAssets\WorldMap\LuoyangFinalAssetReviewManifestV1\luoyang_final_asset_review_manifest_v1.json"
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectRoot `
        "Assets\StreamingAssets\WorldMap\LuoyangRemainingFinalAssetsV1\luoyang_remaining_final_assets_v1.json"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $projectRoot $OutputPath
}

$review = Get-Content -LiteralPath $reviewPath -Raw -Encoding UTF8 |
    ConvertFrom-Json
if ($review.AssetItemCount -ne 54 -or $review.OpeningFacilityCount -ne 2084 -or
    $review.Items.Count -ne 54) {
    throw "The frozen 54-slot Luoyang final-asset manifest is invalid."
}

$activatedOrders = [System.Collections.Generic.HashSet[int]]::new()
foreach ($order in @(0..14) + 22) { [void]$activatedOrders.Add([int]$order) }
$profiles = [System.Collections.Generic.List[object]]::new()
foreach ($item in ($review.Items | Sort-Object ReviewOrder)) {
    if ($activatedOrders.Contains([int]$item.ReviewOrder)) { continue }
    $fileStem = "R{0:D2}_{1}" -f [int]$item.ReviewOrder,
        [string]$item.AssetVariantId
    $profiles.Add([ordered]@{
        ReviewOrder = [int]$item.ReviewOrder
        DisplayName = "Luoyang Final Asset " + [string]$item.AssetVariantId
        SourceKitId = [string]$item.SourceKitId
        SourceProfileId = [string]$item.SourceProfileId
        HistoricalBasisId = ([string]$item.SourceKitId) + ":" +
            ([string]$item.SourceProfileId)
        ModelId = [string]$item.ModelId
        AssetVariantId = [string]$item.AssetVariantId
        ReplacementSlotId = [string]$item.ReplacementSlotId
        AuditGroupId = [string]$item.AuditGroupId
        PriorityId = [string]$item.PriorityId
        FacilityUsageCount = [int]$item.FacilityUsageCount
        RepresentativeFacilityId = [string]$item.RepresentativeFacilityId
        RepresentativeFacilityDefinitionId =
            [string]$item.RepresentativeFacilityDefinitionId
        RepresentativeCellId64 = [uint64]$item.RepresentativeCellId64
        RepresentativeGridColumn = [int]$item.RepresentativeGridColumn
        RepresentativeGridRow = [int]$item.RepresentativeGridRow
        ArtistPrefabResourcePath =
            "Art/Han/Luoyang/FinalRemaining/" + $fileStem
        ArtistFbxTargetPath =
            "Assets/ArtSource/Han/Luoyang/FinalRemaining/" + $fileStem + ".fbx"
        ArtistPrefabPresent = $true
        FinalArtApproved = $true
    })
}

$coveredFacilityCount = 0
foreach ($profile in $profiles) {
    $coveredFacilityCount += [int]$profile['FacilityUsageCount']
}
if ($profiles.Count -ne 38 -or $coveredFacilityCount -ne 2068) {
    throw "Expected 38 remaining slots covering 2,068 Facilities."
}
$priorityCounts = @{}
foreach ($profile in $profiles) {
    $key = [string]$profile['PriorityId']
    if (-not $priorityCounts.ContainsKey($key)) { $priorityCounts[$key] = 0 }
    $priorityCounts[$key]++
}
if ($priorityCounts['priority.p0.identity_critical'] -ne 8 -or
    $priorityCounts['priority.p1.high_exposure'] -ne 10 -or
    $priorityCounts['priority.p2.system_readable'] -ne 14 -or
    $priorityCounts['priority.p3.supporting_context'] -ne 6) {
    throw "Remaining-slot priority counts are not 8/10/14/6."
}

$catalog = [ordered]@{
    SchemaId = "mandate.luoyang-remaining-final-assets.v1"
    TaskId = "LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1"
    StatusId =
        "LUOYANG_REMAINING_38_USER_PREACCEPTED_NATIVE_PREFAB_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1"
    RegionalStyleId = [string]$review.RegionalStyleId
    ReplacementIdentityPolicyId = [string]$review.ReplacementIdentityPolicyId
    UserDecisionStatusId = "user_review.luoyang-remaining-38.preaccepted.v1"
    UserDecisionRecordId =
        "decision.luoyang-remaining-38.preaccepted.2026-08-27.v1"
    UserDecisionDate = "2026-08-27"
    UserDecisionId = "PREACCEPTED_ALL_REMAINING_38"
    CandidateStatusId =
        "candidate.native_prefab_fbx_source_validated.user_preaccepted.final_art_activated.v1"
    FinalArtApprovalStatusId =
        "final_art.user_preaccepted.fbx_source_validated.approved.v1"
    SourceArchiveStatusId =
        "source_archive.unity_native_and_fbx.complete.v1"
    SourceLicenseId = "license.project-original.unity-native-and-fbx.v1"
    RuntimeModeId =
        "runtime.project_original.native_prefab_with_procedural_fallback.v1"
    ProfileCount = 38
    CoveredFacilityCount = 2068
    Profiles = $profiles
}

[System.IO.Directory]::CreateDirectory((Split-Path -Parent $OutputPath)) |
    Out-Null
$json = $catalog | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($OutputPath, $json + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "RESULT status=passed profiles=38 facilities=2068 priorities=8/10/14/6"
Write-Output "Catalog: $OutputPath"
