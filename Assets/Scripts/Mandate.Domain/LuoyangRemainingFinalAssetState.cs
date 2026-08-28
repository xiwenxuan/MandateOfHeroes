using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangRemainingFinalAssetIds
    {
        public const string SchemaId =
            "mandate.luoyang-remaining-final-assets.v1";
        public const string TaskId =
            "LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1";
        public const string StatusId =
            "LUOYANG_REMAINING_38_USER_PREACCEPTED_NATIVE_PREFAB_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1";
        public const string UserDecisionStatusId =
            "user_review.luoyang-remaining-38.preaccepted.v1";
        public const string UserDecisionRecordId =
            "decision.luoyang-remaining-38.preaccepted.2026-08-27.v1";
        public const string UserDecisionDate = "2026-08-27";
        public const string UserDecisionId =
            "PREACCEPTED_ALL_REMAINING_38";
        public const string CandidateStatusId =
            "candidate.native_prefab_fbx_source_validated.user_preaccepted.final_art_activated.v1";
        public const string FinalArtApprovalStatusId =
            "final_art.user_preaccepted.fbx_source_validated.approved.v1";
        public const string SourceArchiveStatusId =
            "source_archive.unity_native_and_fbx.complete.v1";
        public const string SourceLicenseId =
            "license.project-original.unity-native-and-fbx.v1";
        public const string RuntimeModeId =
            "runtime.project_original.native_prefab_with_procedural_fallback.v1";
        public const int ProfileCount = 38;
        public const int CoveredFacilityCount = 2068;

        public static readonly IReadOnlyList<int> ActivatedReviewOrders =
            Enumerable.Range(0, 15).Concat(new[] { 22 }).ToArray();

        public static readonly IReadOnlyList<int> RemainingReviewOrders =
            Enumerable.Range(0, LuoyangFinalAssetReviewIds.AssetItemCount)
                .Except(ActivatedReviewOrders).ToArray();
    }

    [Serializable]
    public sealed class LuoyangRemainingFinalAssetCatalog
    {
        public string SchemaId;
        public string TaskId;
        public string StatusId;
        public string RegionalStyleId;
        public string ReplacementIdentityPolicyId;
        public string UserDecisionStatusId;
        public string UserDecisionRecordId;
        public string UserDecisionDate;
        public string UserDecisionId;
        public string CandidateStatusId;
        public string FinalArtApprovalStatusId;
        public string SourceArchiveStatusId;
        public string SourceLicenseId;
        public string RuntimeModeId;
        public int ProfileCount;
        public int CoveredFacilityCount;
        public List<LuoyangRemainingFinalAssetProfile> Profiles =
            new List<LuoyangRemainingFinalAssetProfile>();
    }

    [Serializable]
    public sealed class LuoyangRemainingFinalAssetProfile
    {
        public int ReviewOrder;
        public string DisplayName;
        public string SourceKitId;
        public string SourceProfileId;
        public string HistoricalBasisId;
        public string ModelId;
        public string AssetVariantId;
        public string ReplacementSlotId;
        public string AuditGroupId;
        public string PriorityId;
        public int FacilityUsageCount;
        public string RepresentativeFacilityId;
        public string RepresentativeFacilityDefinitionId;
        public ulong RepresentativeCellId64;
        public int RepresentativeGridColumn;
        public int RepresentativeGridRow;
        public string ArtistPrefabResourcePath;
        public string ArtistFbxTargetPath;
        public bool ArtistPrefabPresent;
        public bool FinalArtApproved;
    }

    public sealed class LuoyangRemainingFinalAssetPlan
    {
        public LuoyangRemainingFinalAssetPlan(
            LuoyangRemainingFinalAssetCatalog catalog,
            IReadOnlyDictionary<string, LuoyangRemainingFinalAssetProfile>
                profilesByAssetVariantId)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            ProfilesByAssetVariantId = profilesByAssetVariantId ??
                throw new ArgumentNullException(nameof(profilesByAssetVariantId));
        }

        public LuoyangRemainingFinalAssetCatalog Catalog { get; }
        public IReadOnlyDictionary<string, LuoyangRemainingFinalAssetProfile>
            ProfilesByAssetVariantId { get; }
    }

    public static class LuoyangRemainingFinalAssetRules
    {
        public static LuoyangRemainingFinalAssetPlan CreatePlan(
            LuoyangRemainingFinalAssetCatalog catalog,
            LuoyangFinalAssetReviewCatalog review)
        {
            ValidateHeader(catalog, review);
            var expectedOrders = new HashSet<int>(
                LuoyangRemainingFinalAssetIds.RemainingReviewOrders);
            var reviewByOrder = review.Items.ToDictionary(
                item => item.ReviewOrder);
            var foundOrders = new HashSet<int>();
            var assetIds = new HashSet<string>(StringComparer.Ordinal);
            var prefabPaths = new HashSet<string>(StringComparer.Ordinal);
            var fbxPaths = new HashSet<string>(StringComparer.Ordinal);
            var profiles = new Dictionary<string,
                LuoyangRemainingFinalAssetProfile>(StringComparer.Ordinal);

            foreach (var profile in catalog.Profiles)
            {
                if (profile == null ||
                    !expectedOrders.Contains(profile.ReviewOrder) ||
                    !foundOrders.Add(profile.ReviewOrder) ||
                    !reviewByOrder.TryGetValue(profile.ReviewOrder,
                        out var item) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    string.IsNullOrWhiteSpace(profile.HistoricalBasisId) ||
                    !profile.ArtistPrefabPresent ||
                    !profile.FinalArtApproved ||
                    string.IsNullOrWhiteSpace(profile.ArtistPrefabResourcePath) ||
                    !profile.ArtistPrefabResourcePath.StartsWith(
                        "Art/Han/Luoyang/FinalRemaining/",
                        StringComparison.Ordinal) ||
                    !prefabPaths.Add(profile.ArtistPrefabResourcePath) ||
                    string.IsNullOrWhiteSpace(profile.ArtistFbxTargetPath) ||
                    !profile.ArtistFbxTargetPath.StartsWith(
                        "Assets/ArtSource/Han/Luoyang/FinalRemaining/",
                        StringComparison.Ordinal) ||
                    !profile.ArtistFbxTargetPath.EndsWith(".fbx",
                        StringComparison.OrdinalIgnoreCase) ||
                    !fbxPaths.Add(profile.ArtistFbxTargetPath) ||
                    !MatchesReview(profile, item) ||
                    !assetIds.Add(profile.AssetVariantId))
                    throw new InvalidOperationException(
                        "Invalid Luoyang remaining final-asset profile at review order " +
                        profile?.ReviewOrder + ".");
                profiles.Add(profile.AssetVariantId, profile);
            }

            if (!foundOrders.SetEquals(expectedOrders) ||
                catalog.Profiles.Sum(item => item.FacilityUsageCount) !=
                    LuoyangRemainingFinalAssetIds.CoveredFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang remaining final-asset selection is incomplete.");

            RequirePriorityCount(catalog,
                LuoyangFinalAssetReviewIds.PriorityP0, 8);
            RequirePriorityCount(catalog,
                LuoyangFinalAssetReviewIds.PriorityP1, 10);
            RequirePriorityCount(catalog,
                LuoyangFinalAssetReviewIds.PriorityP2, 14);
            RequirePriorityCount(catalog,
                LuoyangFinalAssetReviewIds.PriorityP3, 6);
            return new LuoyangRemainingFinalAssetPlan(catalog, profiles);
        }

        private static void ValidateHeader(
            LuoyangRemainingFinalAssetCatalog catalog,
            LuoyangFinalAssetReviewCatalog review)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (review == null) throw new ArgumentNullException(nameof(review));
            if (!string.Equals(catalog.SchemaId,
                    LuoyangRemainingFinalAssetIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.TaskId,
                    LuoyangRemainingFinalAssetIds.TaskId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.StatusId,
                    LuoyangRemainingFinalAssetIds.StatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.RegionalStyleId,
                    review.RegionalStyleId, StringComparison.Ordinal) ||
                !string.Equals(catalog.ReplacementIdentityPolicyId,
                    LuoyangFinalAssetReviewIds.ReplacementIdentityPolicyId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.UserDecisionStatusId,
                    LuoyangRemainingFinalAssetIds.UserDecisionStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.UserDecisionRecordId,
                    LuoyangRemainingFinalAssetIds.UserDecisionRecordId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.UserDecisionDate,
                    LuoyangRemainingFinalAssetIds.UserDecisionDate,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.UserDecisionId,
                    LuoyangRemainingFinalAssetIds.UserDecisionId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.CandidateStatusId,
                    LuoyangRemainingFinalAssetIds.CandidateStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FinalArtApprovalStatusId,
                    LuoyangRemainingFinalAssetIds.FinalArtApprovalStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.SourceArchiveStatusId,
                    LuoyangRemainingFinalAssetIds.SourceArchiveStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.SourceLicenseId,
                    LuoyangRemainingFinalAssetIds.SourceLicenseId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.RuntimeModeId,
                    LuoyangRemainingFinalAssetIds.RuntimeModeId,
                    StringComparison.Ordinal) ||
                catalog.ProfileCount !=
                    LuoyangRemainingFinalAssetIds.ProfileCount ||
                catalog.CoveredFacilityCount !=
                    LuoyangRemainingFinalAssetIds.CoveredFacilityCount ||
                catalog.Profiles == null || catalog.Profiles.Count !=
                    LuoyangRemainingFinalAssetIds.ProfileCount ||
                review.AssetItemCount !=
                    LuoyangFinalAssetReviewIds.AssetItemCount ||
                review.OpeningFacilityCount !=
                    LuoyangFinalAssetReviewIds.OpeningFacilityCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang remaining final-asset catalog header.");
        }

        private static bool MatchesReview(
            LuoyangRemainingFinalAssetProfile profile,
            LuoyangFinalAssetReviewItem item) =>
            string.Equals(profile.SourceKitId, item.SourceKitId,
                StringComparison.Ordinal) &&
            string.Equals(profile.SourceProfileId, item.SourceProfileId,
                StringComparison.Ordinal) &&
            string.Equals(profile.ModelId, item.ModelId,
                StringComparison.Ordinal) &&
            string.Equals(profile.AssetVariantId, item.AssetVariantId,
                StringComparison.Ordinal) &&
            string.Equals(profile.ReplacementSlotId, item.ReplacementSlotId,
                StringComparison.Ordinal) &&
            string.Equals(profile.AuditGroupId, item.AuditGroupId,
                StringComparison.Ordinal) &&
            string.Equals(profile.PriorityId, item.PriorityId,
                StringComparison.Ordinal) &&
            profile.FacilityUsageCount == item.FacilityUsageCount &&
            string.Equals(profile.RepresentativeFacilityId,
                item.RepresentativeFacilityId, StringComparison.Ordinal) &&
            string.Equals(profile.RepresentativeFacilityDefinitionId,
                item.RepresentativeFacilityDefinitionId,
                StringComparison.Ordinal) &&
            profile.RepresentativeCellId64 == item.RepresentativeCellId64 &&
            profile.RepresentativeGridColumn == item.RepresentativeGridColumn &&
            profile.RepresentativeGridRow == item.RepresentativeGridRow;

        private static void RequirePriorityCount(
            LuoyangRemainingFinalAssetCatalog catalog, string priorityId,
            int expected)
        {
            if (catalog.Profiles.Count(item => string.Equals(item.PriorityId,
                    priorityId, StringComparison.Ordinal)) != expected)
                throw new InvalidOperationException(
                    "Invalid remaining final-asset priority count for " +
                    priorityId + ".");
        }
    }
}
