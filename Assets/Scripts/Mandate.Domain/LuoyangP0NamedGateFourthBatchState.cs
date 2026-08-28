using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangP0NamedGateFourthBatchIds
    {
        public const string SchemaId =
            "mandate.luoyang-p0-named-gate-fourth-batch.v1";
        public const string TaskId =
            "LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1";
        public const string FinalActivationTaskId =
            "LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1";
        public const string StatusId =
            "LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1";
        public const string SelectionPolicyId =
            "selection.lowest-review-order.remaining-p0.after-three-activated-batches.four.v1";
        public const string CandidateStatusId =
            "candidate.native_prefab_fbx_source_validated.user_accepted.final_art_activated.v1";
        public const string ReviewDecisionStatusId =
            "user_review.luoyang-p0-named-gate-fourth-batch.accepted.v1";
        public const string UserReviewDecisionRecordId =
            "decision.luoyang-p0-named-gate-fourth-batch.accepted.2026-08-27.v1";
        public const string UserReviewDecisionDate = "2026-08-27";
        public const string FinalArtApprovalStatusId =
            "final_art.user_accepted.fbx_source_validated.approved.v1";
        public const string SourceArchiveStatusId =
            "source_archive.unity_native_and_fbx_complete.v1";
        public const string SourceLicenseId =
            "license.project-original.unity-native-and-fbx.v1";
        public const string MaterialSetId =
            "material_set.han.luoyang.p0.named_gate_fourth_batch.v1";
        public const string LodProfileId =
            "lod.han.luoyang.p0.named_gate_fourth_batch.three_tier.v1";
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
            LuoyangGateIdentityKitIds.Gumen,
            LuoyangGateIdentityKitIds.Jinmen,
            LuoyangGateIdentityKitIds.Kaiyangmen,
            LuoyangGateIdentityKitIds.Maomen
        };

        public static readonly IReadOnlyDictionary<string, int> ReviewOrders =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [LuoyangGateIdentityKitIds.Gumen] = 11,
                [LuoyangGateIdentityKitIds.Jinmen] = 12,
                [LuoyangGateIdentityKitIds.Kaiyangmen] = 13,
                [LuoyangGateIdentityKitIds.Maomen] = 14
            };
    }

    [Serializable]
    public sealed class LuoyangP0NamedGateFourthBatchCatalog
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
        public List<LuoyangP0NamedGateFourthBatchProfile> Profiles =
            new List<LuoyangP0NamedGateFourthBatchProfile>();
    }

    [Serializable]
    public sealed class LuoyangP0NamedGateFourthBatchProfile
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

    public sealed class LuoyangP0NamedGateFourthBatchPlan
    {
        public LuoyangP0NamedGateFourthBatchPlan(
            LuoyangP0NamedGateFourthBatchCatalog catalog,
            IReadOnlyDictionary<string, LuoyangP0FinalAssetProfile>
                profilesByFacilityId)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            ProfilesByFacilityId = profilesByFacilityId ??
                                   throw new ArgumentNullException(
                                       nameof(profilesByFacilityId));
        }

        public LuoyangP0NamedGateFourthBatchCatalog Catalog { get; }
        public IReadOnlyDictionary<string, LuoyangP0FinalAssetProfile>
            ProfilesByFacilityId { get; }
    }

    public static class LuoyangP0NamedGateFourthBatchRules
    {
        public static LuoyangP0NamedGateFourthBatchPlan CreatePlan(
            LuoyangP0NamedGateFourthBatchCatalog catalog,
            HanBuildableFacilityModelCatalog models,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangFinalAssetReviewCatalog review)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            LuoyangGateIdentityKitRules.Validate(gates, models);
            if (review == null) throw new ArgumentNullException(nameof(review));
            ValidateHeader(catalog, models.RegionalStyleId);

            var gatesByFacility = gates.Profiles.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            var reviewByFacility = review.Items.ToDictionary(
                item => item.RepresentativeFacilityId, StringComparer.Ordinal);
            var expected = new HashSet<string>(
                LuoyangP0NamedGateFourthBatchIds.FacilityIds,
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
                        LuoyangP0NamedGateFourthBatchIds.CandidateStatusId,
                        StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    !profile.ArtistPrefabPresent || !profile.FinalArtApproved ||
                    string.IsNullOrWhiteSpace(profile.ArtistPrefabResourcePath) ||
                    !profile.ArtistPrefabResourcePath.StartsWith(
                        "Art/Han/Luoyang/P0Batch4/", StringComparison.Ordinal) ||
                    !prefabPaths.Add(profile.ArtistPrefabResourcePath) ||
                    string.IsNullOrWhiteSpace(profile.ArtistFbxTargetPath) ||
                    !profile.ArtistFbxTargetPath.StartsWith(
                        "Assets/ArtSource/Han/Luoyang/P0Batch4/",
                        StringComparison.Ordinal) ||
                    !profile.ArtistFbxTargetPath.EndsWith(".fbx",
                        StringComparison.OrdinalIgnoreCase) ||
                    !fbxPaths.Add(profile.ArtistFbxTargetPath) ||
                    !gatesByFacility.TryGetValue(profile.FacilityId,
                        out var gate) ||
                    !reviewByFacility.TryGetValue(profile.FacilityId,
                        out var reviewItem))
                    throw new InvalidOperationException(
                        "Invalid Luoyang P0 named-gate fourth-batch profile.");

                ValidateIdentity(profile, gate, reviewItem);
                ValidateAnchors(profile, gate);
                result.Add(profile.FacilityId,
                    ProjectRuntimeProfile(profile, gate));
            }

            if (!found.SetEquals(expected))
                throw new InvalidOperationException(
                    "Luoyang P0 named-gate fourth batch is incomplete.");
            return new LuoyangP0NamedGateFourthBatchPlan(catalog, result);
        }

        private static void ValidateHeader(
            LuoyangP0NamedGateFourthBatchCatalog catalog,
            string regionalStyleId)
        {
            if (!string.Equals(catalog.SchemaId,
                    LuoyangP0NamedGateFourthBatchIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.TaskId,
                    LuoyangP0NamedGateFourthBatchIds.TaskId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.StatusId,
                    LuoyangP0NamedGateFourthBatchIds.StatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.RegionalStyleId, regionalStyleId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.SelectionPolicyId,
                    LuoyangP0NamedGateFourthBatchIds.SelectionPolicyId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.ReplacementIdentityPolicyId,
                    LuoyangP0FinalAssetVerticalSliceIds
                        .ReplacementIdentityPolicyId, StringComparison.Ordinal) ||
                !string.Equals(catalog.RuntimeCandidateModeId,
                    LuoyangP0NamedGateFourthBatchIds.RuntimeCandidateModeId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.ReviewDecisionStatusId,
                    LuoyangP0NamedGateFourthBatchIds.ReviewDecisionStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.UserReviewDecisionRecordId,
                    LuoyangP0NamedGateFourthBatchIds
                        .UserReviewDecisionRecordId, StringComparison.Ordinal) ||
                !string.Equals(catalog.UserReviewDecisionDate,
                    LuoyangP0NamedGateFourthBatchIds.UserReviewDecisionDate,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FinalArtApprovalStatusId,
                    LuoyangP0NamedGateFourthBatchIds.FinalArtApprovalStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.SourceArchiveStatusId,
                    LuoyangP0NamedGateFourthBatchIds.SourceArchiveStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.SourceLicenseId,
                    LuoyangP0NamedGateFourthBatchIds.SourceLicenseId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.MaterialSetId,
                    LuoyangP0NamedGateFourthBatchIds.MaterialSetId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.LodProfileId,
                    LuoyangP0NamedGateFourthBatchIds.LodProfileId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FbxSourceToolchainId,
                    LuoyangP0NamedGateFourthBatchIds.FbxSourceToolchainId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FbxToolchainLicenseId,
                    LuoyangP0NamedGateFourthBatchIds.FbxToolchainLicenseId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FbxAnchorNameMappingId,
                    LuoyangP0NamedGateFourthBatchIds.FbxAnchorNameMappingId,
                    StringComparison.Ordinal) ||
                catalog.ProfileCount !=
                    LuoyangP0NamedGateFourthBatchIds.ProfileCount ||
                catalog.Profiles == null || catalog.Profiles.Count !=
                    LuoyangP0NamedGateFourthBatchIds.ProfileCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang P0 named-gate fourth-batch header.");
        }

        private static void ValidateIdentity(
            LuoyangP0NamedGateFourthBatchProfile profile,
            LuoyangGateIdentityProfile gate,
            LuoyangFinalAssetReviewItem review)
        {
            var expectedOrder = LuoyangP0NamedGateFourthBatchIds
                .ReviewOrders[profile.FacilityId];
            if (profile.ReviewOrder != expectedOrder ||
                review.ReviewOrder != expectedOrder ||
                !string.Equals(review.PriorityId,
                    LuoyangFinalAssetReviewIds.PriorityP0,
                    StringComparison.Ordinal) ||
                !string.Equals(review.ReplacementStatusId,
                    LuoyangFinalAssetReviewIds.ReplacementStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.SourceProfileId, gate.ProfileId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.SourceProfileId, review.SourceProfileId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.ModelId, gate.BaseModelId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.ModelId, review.ModelId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.AssetVariantId, gate.AssetVariantId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.AssetVariantId, review.AssetVariantId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.ReplacementSlotId,
                    profile.AssetVariantId, StringComparison.Ordinal) ||
                !string.Equals(profile.ReplacementSlotId,
                    review.ReplacementSlotId, StringComparison.Ordinal) ||
                profile.CellId64 != gate.CellId64 ||
                profile.CellId64 != review.RepresentativeCellId64 ||
                profile.GridColumn != gate.GridX ||
                profile.GridColumn != review.RepresentativeGridColumn ||
                profile.GridRow != gate.GridY ||
                profile.GridRow != review.RepresentativeGridRow)
                throw new InvalidOperationException(
                    "Luoyang P0 named-gate fourth batch changes frozen identity.");
        }

        private static void ValidateAnchors(
            LuoyangP0NamedGateFourthBatchProfile profile,
            LuoyangGateIdentityProfile gate)
        {
            if (profile.Anchors == null || profile.Anchors.Count != 3)
                throw new InvalidOperationException(
                    "Luoyang P0 named-gate fourth-batch anchors are incomplete.");
            var placement = profile.Anchors.SingleOrDefault(item =>
                item != null && string.Equals(item.RoleId,
                    LuoyangP0FinalAssetVerticalSliceIds.PlacementAnchorRoleId,
                    StringComparison.Ordinal));
            var outer = profile.Anchors.SingleOrDefault(item =>
                item != null && string.Equals(item.RoleId,
                    LuoyangP0FinalAssetVerticalSliceIds.OuterPassageAnchorRoleId,
                    StringComparison.Ordinal));
            var inner = profile.Anchors.SingleOrDefault(item =>
                item != null && string.Equals(item.RoleId,
                    LuoyangP0FinalAssetVerticalSliceIds.InnerPassageAnchorRoleId,
                    StringComparison.Ordinal));
            if (placement == null || outer == null || inner == null ||
                !string.Equals(placement.AnchorId, gate.PlacementAnchorId,
                    StringComparison.Ordinal) ||
                !string.Equals(outer.AnchorId, gate.OuterPassageAnchorId,
                    StringComparison.Ordinal) ||
                !string.Equals(inner.AnchorId, gate.InnerPassageAnchorId,
                    StringComparison.Ordinal) ||
                !Near(placement.X, 0f) || !Near(placement.Y, 0f) ||
                !Near(placement.Z, 0f) ||
                !Near(outer.X, gate.OuterPassageX) ||
                !Near(outer.Y, gate.OuterPassageY) ||
                !Near(outer.Z, gate.OuterPassageZ) ||
                !Near(inner.X, gate.InnerPassageX) ||
                !Near(inner.Y, gate.InnerPassageY) ||
                !Near(inner.Z, gate.InnerPassageZ))
                throw new InvalidOperationException(
                    "Luoyang P0 named-gate fourth-batch anchors changed the gate contract.");
        }

        private static LuoyangP0FinalAssetProfile ProjectRuntimeProfile(
            LuoyangP0NamedGateFourthBatchProfile source,
            LuoyangGateIdentityProfile gate) =>
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
                HistoricalConfidence = gate.HistoricalConfidence,
                SpatialPrecision = gate.SpatialPrecision,
                HistoricalBasis = gate.HistoricalBasis,
                SourceIds = gate.SourceIds.ToList(),
                AvailabilityIds = gate.AvailabilityIds.ToList(),
                MaterialSetId =
                    LuoyangP0NamedGateFourthBatchIds.MaterialSetId,
                LodProfileId = LuoyangP0NamedGateFourthBatchIds.LodProfileId,
                RuntimeCandidateModeId =
                    LuoyangP0NamedGateFourthBatchIds.RuntimeCandidateModeId,
                ArtistPrefabResourcePath = source.ArtistPrefabResourcePath,
                ArtistFbxTargetPath = source.ArtistFbxTargetPath,
                ArtistPrefabPresent = source.ArtistPrefabPresent,
                FinalArtApproved = source.FinalArtApproved,
                Anchors = source.Anchors.ToList(),
                Modules = gate.Modules.ToList(),
                Lod1ModuleIds = gate.Lod1ModuleIds.ToList(),
                Lod2ModuleIds = gate.Lod2ModuleIds.ToList()
            };

        private static bool Near(float left, float right) =>
            Math.Abs(left - right) <= 0.0001f;
    }
}
