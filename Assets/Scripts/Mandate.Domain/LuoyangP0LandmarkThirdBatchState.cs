using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangP0LandmarkThirdBatchIds
    {
        public const string SchemaId =
            "mandate.luoyang-p0-landmark-third-batch.v1";
        public const string TaskId =
            "LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1";
        public const string FinalActivationTaskId =
            "LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1";
        public const string StatusId =
            "LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1";
        public const string SelectionPolicyId =
            "selection.lowest-review-order.remaining-p0.after-two-activated-batches.four.v1";
        public const string CandidateStatusId =
            "candidate.native_prefab_fbx_source_validated.user_accepted.final_art_activated.v1";
        public const string ReviewDecisionStatusId =
            "user_review.luoyang-p0-landmark-third-batch.accepted.v1";
        public const string UserReviewDecisionRecordId =
            "decision.luoyang-p0-landmark-third-batch.accepted.2026-08-27.v1";
        public const string UserReviewDecisionDate = "2026-08-27";
        public const string FinalArtApprovalStatusId =
            "final_art.user_accepted.fbx_source_validated.approved.v1";
        public const string SourceArchiveStatusId =
            "source_archive.unity_native_and_fbx_complete.v1";
        public const string SourceLicenseId =
            "license.project-original.unity-native-and-fbx.v1";
        public const string MaterialSetId =
            "material_set.han.luoyang.p0.landmark_third_batch.v1";
        public const string LodProfileId =
            "lod.han.luoyang.p0.landmark_third_batch.three_tier.v1";
        public const string RuntimeCandidateModeId =
            "runtime.project_original.native_prefab_with_procedural_fallback.v1";
        public const string FbxSourceToolchainId =
            "toolchain.unity-fbx-exporter.4.2.1";
        public const string FbxToolchainLicenseId =
            "license.unity-companion.v1";
        public const string FbxAnchorNameMappingId =
            "anchor_name.dot_to_underscore.unity_fbx_exporter.v1";
        public const int ProfileCount = 4;

        public static readonly IReadOnlyList<string> FacilityIds = new[]
        {
            LuoyangHistoricalLandmarkKitIds.Lingtai,
            LuoyangHistoricalLandmarkKitIds.Taicang,
            LuoyangHistoricalLandmarkKitIds.Arsenal,
            LuoyangHistoricalLandmarkKitIds.ZhuolongGarden
        };

        public static readonly IReadOnlyDictionary<string, int> ReviewOrders =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [LuoyangHistoricalLandmarkKitIds.Lingtai] = 6,
                [LuoyangHistoricalLandmarkKitIds.Taicang] = 7,
                [LuoyangHistoricalLandmarkKitIds.Arsenal] = 8,
                [LuoyangHistoricalLandmarkKitIds.ZhuolongGarden] = 9
            };
    }

    [Serializable]
    public sealed class LuoyangP0LandmarkThirdBatchCatalog
    {
        public string SchemaId;
        public string TaskId;
        public string StatusId;
        public string RegionalStyleId;
        public string SelectionPolicyId;
        public string ReplacementIdentityPolicyId;
        public string RuntimeCandidateModeId;
        public string ReviewDecisionStatusId;
        public string UserReviewDecisionRecordId;
        public string UserReviewDecisionDate;
        public string FinalArtApprovalStatusId;
        public string SourceArchiveStatusId;
        public string SourceLicenseId;
        public string MaterialSetId;
        public string LodProfileId;
        public string FbxSourceToolchainId;
        public string FbxToolchainLicenseId;
        public string FbxAnchorNameMappingId;
        public int ProfileCount;
        public List<LuoyangP0LandmarkThirdBatchProfile> Profiles =
            new List<LuoyangP0LandmarkThirdBatchProfile>();
    }

    [Serializable]
    public sealed class LuoyangP0LandmarkThirdBatchProfile
    {
        public string CandidateId;
        public string CandidateStatusId;
        public string DisplayName;
        public int ReviewOrder;
        public string FacilityId;
        public string SourceProfileId;
        public string ModelId;
        public string AssetVariantId;
        public string ReplacementSlotId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
        public string ArtistPrefabResourcePath;
        public string ArtistFbxTargetPath;
        public bool ArtistPrefabPresent;
        public bool FinalArtApproved;
        public List<LuoyangP0FinalAssetAnchor> Anchors =
            new List<LuoyangP0FinalAssetAnchor>();
    }

    public sealed class LuoyangP0LandmarkThirdBatchPlan
    {
        public LuoyangP0LandmarkThirdBatchPlan(
            LuoyangP0LandmarkThirdBatchCatalog catalog,
            IReadOnlyDictionary<string, LuoyangP0FinalAssetProfile>
                profilesByFacilityId)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            ProfilesByFacilityId = profilesByFacilityId ??
                                   throw new ArgumentNullException(
                                       nameof(profilesByFacilityId));
        }

        public LuoyangP0LandmarkThirdBatchCatalog Catalog { get; }
        public IReadOnlyDictionary<string, LuoyangP0FinalAssetProfile>
            ProfilesByFacilityId { get; }
    }

    public static class LuoyangP0LandmarkThirdBatchRules
    {
        public static LuoyangP0LandmarkThirdBatchPlan CreatePlan(
            LuoyangP0LandmarkThirdBatchCatalog catalog,
            HanBuildableFacilityModelCatalog models,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangFinalAssetReviewCatalog review)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            LuoyangHistoricalLandmarkKitRules.Validate(landmarks, models);
            if (review == null) throw new ArgumentNullException(nameof(review));
            ValidateHeader(catalog, models.RegionalStyleId);

            var landmarksByFacility = landmarks.Profiles.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            var reviewByFacility = review.Items.ToDictionary(
                item => item.RepresentativeFacilityId, StringComparer.Ordinal);
            var expected = new HashSet<string>(
                LuoyangP0LandmarkThirdBatchIds.FacilityIds,
                StringComparer.Ordinal);
            var found = new HashSet<string>(StringComparer.Ordinal);
            var candidateIds = new HashSet<string>(StringComparer.Ordinal);
            var prefabPaths = new HashSet<string>(StringComparer.Ordinal);
            var fbxPaths = new HashSet<string>(StringComparer.Ordinal);
            var result = new Dictionary<string, LuoyangP0FinalAssetProfile>(
                StringComparer.Ordinal);

            foreach (var profile in catalog.Profiles)
            {
                if (profile == null ||
                    !expected.Contains(profile.FacilityId ?? string.Empty) ||
                    !found.Add(profile.FacilityId) ||
                    string.IsNullOrWhiteSpace(profile.CandidateId) ||
                    !candidateIds.Add(profile.CandidateId) ||
                    !string.Equals(profile.CandidateStatusId,
                        LuoyangP0LandmarkThirdBatchIds.CandidateStatusId,
                        StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    !profile.ArtistPrefabPresent || !profile.FinalArtApproved ||
                    string.IsNullOrWhiteSpace(profile.ArtistPrefabResourcePath) ||
                    !profile.ArtistPrefabResourcePath.StartsWith(
                        "Art/Han/Luoyang/P0Batch3/", StringComparison.Ordinal) ||
                    !prefabPaths.Add(profile.ArtistPrefabResourcePath) ||
                    string.IsNullOrWhiteSpace(profile.ArtistFbxTargetPath) ||
                    !profile.ArtistFbxTargetPath.StartsWith(
                        "Assets/ArtSource/Han/Luoyang/P0Batch3/",
                        StringComparison.Ordinal) ||
                    !profile.ArtistFbxTargetPath.EndsWith(".fbx",
                        StringComparison.OrdinalIgnoreCase) ||
                    !fbxPaths.Add(profile.ArtistFbxTargetPath) ||
                    !landmarksByFacility.TryGetValue(profile.FacilityId,
                        out var landmark) ||
                    !reviewByFacility.TryGetValue(profile.FacilityId,
                        out var reviewItem))
                    throw new InvalidOperationException(
                        "Invalid Luoyang P0 landmark third-batch profile.");

                ValidateIdentity(profile, landmark, reviewItem);
                ValidateAnchors(profile, landmark);
                result.Add(profile.FacilityId,
                    ProjectRuntimeProfile(profile, landmark));
            }

            if (!found.SetEquals(expected))
                throw new InvalidOperationException(
                    "Luoyang P0 landmark third batch is incomplete.");
            return new LuoyangP0LandmarkThirdBatchPlan(catalog, result);
        }

        private static void ValidateHeader(
            LuoyangP0LandmarkThirdBatchCatalog catalog,
            string regionalStyleId)
        {
            if (!string.Equals(catalog.SchemaId,
                    LuoyangP0LandmarkThirdBatchIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.TaskId,
                    LuoyangP0LandmarkThirdBatchIds.TaskId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.StatusId,
                    LuoyangP0LandmarkThirdBatchIds.StatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.RegionalStyleId, regionalStyleId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.SelectionPolicyId,
                    LuoyangP0LandmarkThirdBatchIds.SelectionPolicyId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.ReplacementIdentityPolicyId,
                    LuoyangP0FinalAssetVerticalSliceIds
                        .ReplacementIdentityPolicyId, StringComparison.Ordinal) ||
                !string.Equals(catalog.RuntimeCandidateModeId,
                    LuoyangP0LandmarkThirdBatchIds.RuntimeCandidateModeId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.ReviewDecisionStatusId,
                    LuoyangP0LandmarkThirdBatchIds.ReviewDecisionStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.UserReviewDecisionRecordId,
                    LuoyangP0LandmarkThirdBatchIds.UserReviewDecisionRecordId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.UserReviewDecisionDate,
                    LuoyangP0LandmarkThirdBatchIds.UserReviewDecisionDate,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FinalArtApprovalStatusId,
                    LuoyangP0LandmarkThirdBatchIds.FinalArtApprovalStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.SourceArchiveStatusId,
                    LuoyangP0LandmarkThirdBatchIds.SourceArchiveStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.SourceLicenseId,
                    LuoyangP0LandmarkThirdBatchIds.SourceLicenseId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.MaterialSetId,
                    LuoyangP0LandmarkThirdBatchIds.MaterialSetId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.LodProfileId,
                    LuoyangP0LandmarkThirdBatchIds.LodProfileId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FbxSourceToolchainId,
                    LuoyangP0LandmarkThirdBatchIds.FbxSourceToolchainId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FbxToolchainLicenseId,
                    LuoyangP0LandmarkThirdBatchIds.FbxToolchainLicenseId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FbxAnchorNameMappingId,
                    LuoyangP0LandmarkThirdBatchIds.FbxAnchorNameMappingId,
                    StringComparison.Ordinal) ||
                catalog.ProfileCount !=
                    LuoyangP0LandmarkThirdBatchIds.ProfileCount ||
                catalog.Profiles == null || catalog.Profiles.Count !=
                    LuoyangP0LandmarkThirdBatchIds.ProfileCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang P0 landmark third-batch header.");
        }

        private static void ValidateIdentity(
            LuoyangP0LandmarkThirdBatchProfile profile,
            LuoyangHistoricalLandmarkProfile landmark,
            LuoyangFinalAssetReviewItem review)
        {
            var expectedOrder = LuoyangP0LandmarkThirdBatchIds
                .ReviewOrders[profile.FacilityId];
            if (profile.ReviewOrder != expectedOrder ||
                review.ReviewOrder != expectedOrder ||
                !string.Equals(review.PriorityId,
                    LuoyangFinalAssetReviewIds.PriorityP0,
                    StringComparison.Ordinal) ||
                !string.Equals(review.ReplacementStatusId,
                    LuoyangFinalAssetReviewIds.ReplacementStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.SourceProfileId, landmark.ProfileId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.SourceProfileId, review.SourceProfileId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.ModelId, landmark.BaseModelId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.ModelId, review.ModelId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.AssetVariantId, landmark.AssetVariantId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.AssetVariantId, review.AssetVariantId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.ReplacementSlotId,
                    profile.AssetVariantId, StringComparison.Ordinal) ||
                !string.Equals(profile.ReplacementSlotId,
                    review.ReplacementSlotId, StringComparison.Ordinal) ||
                profile.CellId64 != landmark.CellId64 ||
                profile.CellId64 != review.RepresentativeCellId64 ||
                profile.GridColumn != landmark.GridX ||
                profile.GridColumn != review.RepresentativeGridColumn ||
                profile.GridRow != landmark.GridY ||
                profile.GridRow != review.RepresentativeGridRow)
                throw new InvalidOperationException(
                    "Luoyang P0 landmark third batch changes frozen identity.");
        }

        private static void ValidateAnchors(
            LuoyangP0LandmarkThirdBatchProfile profile,
            LuoyangHistoricalLandmarkProfile landmark)
        {
            if (profile.Anchors == null || profile.Anchors.Count != 2)
                throw new InvalidOperationException(
                    "Luoyang P0 landmark third-batch anchors are incomplete.");
            var placement = profile.Anchors.SingleOrDefault(item =>
                item != null && string.Equals(item.RoleId,
                    LuoyangP0FinalAssetVerticalSliceIds.PlacementAnchorRoleId,
                    StringComparison.Ordinal));
            var entrance = profile.Anchors.SingleOrDefault(item =>
                item != null && string.Equals(item.RoleId,
                    LuoyangP0FinalAssetVerticalSliceIds.EntranceAnchorRoleId,
                    StringComparison.Ordinal));
            if (placement == null || entrance == null ||
                string.IsNullOrWhiteSpace(placement.AnchorId) ||
                string.IsNullOrWhiteSpace(entrance.AnchorId) ||
                string.Equals(placement.AnchorId, entrance.AnchorId,
                    StringComparison.Ordinal) ||
                !Near(placement.X, 0f) || !Near(placement.Y, 0f) ||
                !Near(placement.Z, 0f) ||
                !Near(entrance.X, landmark.EntranceX) ||
                !Near(entrance.Y, landmark.EntranceY) ||
                !Near(entrance.Z, landmark.EntranceZ))
                throw new InvalidOperationException(
                    "Luoyang P0 landmark third-batch anchors changed the source contract.");
        }

        private static LuoyangP0FinalAssetProfile ProjectRuntimeProfile(
            LuoyangP0LandmarkThirdBatchProfile source,
            LuoyangHistoricalLandmarkProfile landmark) =>
            new LuoyangP0FinalAssetProfile
            {
                CandidateId = source.CandidateId,
                CandidateStatusId = source.CandidateStatusId,
                DisplayName = source.DisplayName,
                FacilityId = source.FacilityId,
                SourceProfileId = source.SourceProfileId,
                ModelId = source.ModelId,
                AssetVariantId = source.AssetVariantId,
                ReplacementSlotId = source.ReplacementSlotId,
                CellId64 = source.CellId64,
                GridColumn = source.GridColumn,
                GridRow = source.GridRow,
                HistoricalConfidence = landmark.HistoricalConfidence,
                SpatialPrecision = landmark.SpatialPrecision,
                HistoricalBasis = landmark.HistoricalBasis,
                SourceIds = landmark.SourceIds.ToList(),
                AvailabilityIds = landmark.AvailabilityIds.ToList(),
                MaterialSetId = LuoyangP0LandmarkThirdBatchIds.MaterialSetId,
                LodProfileId = LuoyangP0LandmarkThirdBatchIds.LodProfileId,
                RuntimeCandidateModeId =
                    LuoyangP0LandmarkThirdBatchIds.RuntimeCandidateModeId,
                ArtistPrefabResourcePath = source.ArtistPrefabResourcePath,
                ArtistFbxTargetPath = source.ArtistFbxTargetPath,
                ArtistPrefabPresent = source.ArtistPrefabPresent,
                FinalArtApproved = source.FinalArtApproved,
                Anchors = source.Anchors.ToList(),
                Modules = landmark.Modules.ToList(),
                Lod1ModuleIds = landmark.Lod1ModuleIds.ToList(),
                Lod2ModuleIds = landmark.Lod2ModuleIds.ToList()
            };

        private static bool Near(float left, float right) =>
            Math.Abs(left - right) <= 0.0001f;
    }
}
