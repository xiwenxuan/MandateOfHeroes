using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangP0FinalAssetVerticalSliceIds
    {
        public const string SchemaId =
            "mandate.luoyang-p0-final-asset-vertical-slice.v1";
        public const string TaskId =
            "LUOYANG_P0_FINAL_ASSET_FOUR_PIECE_VERTICAL_SLICE_V1";
        public const string MaterialSetId =
            "material_set.han.luoyang.p0.vertical_slice.v1";
        public const string LodProfileId =
            "lod.han.luoyang.p0.final_candidate.three_tier.v1";
        public const string ReplacementIdentityPolicyId =
            "replacement.identity.keep_model_asset_profile_facility.v1";
        public const string RuntimeCandidateModeId =
            "runtime.project_original.native_prefab_with_procedural_fallback.v1";
        public const string ArtistPrefabSourceStatusId =
            "artist_prefab.project_original_native_prefab_and_fbx_source_present";
        public const string FinalArtApprovalStatusId =
            "final_art.user_accepted.fbx_source_validated.approved.v1";
        public const string UserReviewDecisionStatusId =
            "user_review.luoyang-p0-four-piece.accepted.v1";
        public const string UserReviewDecisionRecordId =
            "decision.luoyang-p0-four-piece.accepted.2026-08-27.v1";
        public const string UserReviewDecisionDate = "2026-08-27";
        public const string SourceArchiveStatusId =
            "source_archive.unity_native_and_fbx_complete.v1";
        public const string SourceLicenseId =
            "license.project-original.unity-native-and-fbx.v1";
        public const string FbxSourceToolchainId =
            "toolchain.unity-fbx-exporter.4.2.1";
        public const string FbxToolchainLicenseId =
            "license.unity-companion.v1";
        public const string FbxAnchorNameMappingId =
            "anchor_name.dot_to_underscore.unity_fbx_exporter.v1";
        public const string CandidateStatusId =
            "candidate.native_prefab_refined_v2.user_accepted.fbx_source_validated.final";
        public const string PlacementAnchorRoleId = "anchor_role.placement";
        public const string EntranceAnchorRoleId = "anchor_role.entrance";
        public const string OuterPassageAnchorRoleId =
            "anchor_role.outer_passage";
        public const string InnerPassageAnchorRoleId =
            "anchor_role.inner_passage";
        public const int ProfileCount = 4;
        public const int MaterialCount = 6;

        public static readonly IReadOnlyList<string> FacilityIds = new[]
        {
            LuoyangHistoricalLandmarkKitIds.SouthPalace,
            LuoyangHistoricalLandmarkKitIds.Mingtang,
            LuoyangGateIdentityKitIds.Guangyangmen,
            LuoyangGateIdentityKitIds.NorthPalaceSouthGate
        };
    }

    [Serializable]
    public sealed class LuoyangP0FinalAssetVerticalSliceCatalog
    {
        public string SchemaId;
        public string TaskId;
        public string RegionalStyleId;
        public string ReplacementIdentityPolicyId;
        public string RuntimeCandidateModeId;
        public string ArtistPrefabSourceStatusId;
        public string FinalArtApprovalStatusId;
        public string UserReviewDecisionStatusId;
        public string UserReviewDecisionRecordId;
        public string UserReviewDecisionDate;
        public string SourceArchiveStatusId;
        public string SourceLicenseId;
        public string FbxSourceToolchainId;
        public string FbxToolchainLicenseId;
        public string FbxAnchorNameMappingId;
        public int ProfileCount;
        public int MaterialCount;
        public List<HanBuildableFacilityMaterialDefinition> Materials =
            new List<HanBuildableFacilityMaterialDefinition>();
        public List<LuoyangP0FinalAssetProfile> Profiles =
            new List<LuoyangP0FinalAssetProfile>();
    }

    [Serializable]
    public sealed class LuoyangP0FinalAssetProfile
    {
        public string CandidateId;
        public string CandidateStatusId;
        public string DisplayName;
        public string FacilityId;
        public string SourceProfileId;
        public string ModelId;
        public string AssetVariantId;
        public string ReplacementSlotId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
        public string HistoricalConfidence;
        public string SpatialPrecision;
        public string HistoricalBasis;
        public List<string> SourceIds = new List<string>();
        public List<string> AvailabilityIds = new List<string>();
        public string MaterialSetId;
        public string LodProfileId;
        public string RuntimeCandidateModeId;
        public string ArtistPrefabResourcePath;
        public string ArtistFbxTargetPath;
        public bool ArtistPrefabPresent;
        public bool FinalArtApproved;
        public List<LuoyangP0FinalAssetAnchor> Anchors =
            new List<LuoyangP0FinalAssetAnchor>();
        public List<HanBuildableFacilityModuleDefinition> Modules =
            new List<HanBuildableFacilityModuleDefinition>();
        public List<string> Lod1ModuleIds = new List<string>();
        public List<string> Lod2ModuleIds = new List<string>();
    }

    [Serializable]
    public sealed class LuoyangP0FinalAssetAnchor
    {
        public string AnchorId;
        public string RoleId;
        public float X;
        public float Y;
        public float Z;
    }

    public sealed class LuoyangP0FinalAssetVerticalSlicePlan
    {
        public LuoyangP0FinalAssetVerticalSlicePlan(
            LuoyangP0FinalAssetVerticalSliceCatalog catalog,
            IReadOnlyDictionary<string, LuoyangP0FinalAssetProfile>
                profilesByFacilityId)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            ProfilesByFacilityId = profilesByFacilityId ??
                                   throw new ArgumentNullException(
                                       nameof(profilesByFacilityId));
        }

        public LuoyangP0FinalAssetVerticalSliceCatalog Catalog { get; }
        public IReadOnlyDictionary<string, LuoyangP0FinalAssetProfile>
            ProfilesByFacilityId { get; }
    }

    public static class LuoyangP0FinalAssetVerticalSliceRules
    {
        private static readonly HashSet<string> AllowedPrimitives =
            new HashSet<string>(new[]
            {
                "cube", "cylinder", "han.rammed_block", "han.tile_slab",
                "han.terrain_pad", "han.wall_coping", "han.timber_beam",
                "han.hip_roof", "han.road_crown"
            }, StringComparer.Ordinal);

        public static LuoyangP0FinalAssetVerticalSlicePlan CreatePlan(
            LuoyangP0FinalAssetVerticalSliceCatalog catalog,
            HanBuildableFacilityModelCatalog models,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangFinalAssetReviewCatalog finalAssetReview)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            LuoyangHistoricalLandmarkKitRules.Validate(landmarks, models);
            LuoyangGateIdentityKitRules.Validate(gates, models);
            if (finalAssetReview == null)
                throw new ArgumentNullException(nameof(finalAssetReview));

            ValidateHeader(catalog, models.RegionalStyleId);
            var modelById = models.Models.ToDictionary(item => item.ModelId,
                StringComparer.Ordinal);
            var materialIds = ValidateMaterials(catalog.Materials);
            var landmarkByFacility = landmarks.Profiles.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            var gateByFacility = gates.Profiles.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            var reviewByFacility = finalAssetReview.Items.ToDictionary(
                item => item.RepresentativeFacilityId, StringComparer.Ordinal);
            var expectedFacilities = new HashSet<string>(
                LuoyangP0FinalAssetVerticalSliceIds.FacilityIds,
                StringComparer.Ordinal);
            var facilities = new HashSet<string>(StringComparer.Ordinal);
            var candidates = new HashSet<string>(StringComparer.Ordinal);
            var prefabPaths = new HashSet<string>(StringComparer.Ordinal);
            var result = new Dictionary<string, LuoyangP0FinalAssetProfile>(
                StringComparer.Ordinal);

            foreach (var profile in catalog.Profiles)
            {
                if (profile == null ||
                    !expectedFacilities.Contains(profile.FacilityId ?? string.Empty) ||
                    !facilities.Add(profile.FacilityId) ||
                    string.IsNullOrWhiteSpace(profile.CandidateId) ||
                    !candidates.Add(profile.CandidateId) ||
                    !string.Equals(profile.CandidateStatusId,
                        LuoyangP0FinalAssetVerticalSliceIds.CandidateStatusId,
                        StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    !modelById.TryGetValue(profile.ModelId ?? string.Empty,
                        out var model) ||
                    string.IsNullOrWhiteSpace(profile.SourceProfileId) ||
                    string.IsNullOrWhiteSpace(profile.AssetVariantId) ||
                    !string.Equals(profile.ReplacementSlotId,
                        profile.AssetVariantId, StringComparison.Ordinal) ||
                    profile.CellId64 == 0UL || profile.GridColumn < 0 ||
                    profile.GridRow < 0 ||
                    string.IsNullOrWhiteSpace(profile.HistoricalConfidence) ||
                    string.IsNullOrWhiteSpace(profile.SpatialPrecision) ||
                    string.IsNullOrWhiteSpace(profile.HistoricalBasis) ||
                    profile.SourceIds == null || profile.SourceIds.Count == 0 ||
                    profile.SourceIds.Any(string.IsNullOrWhiteSpace) ||
                    profile.SourceIds.Distinct(StringComparer.Ordinal).Count() !=
                    profile.SourceIds.Count ||
                    profile.AvailabilityIds == null ||
                    profile.AvailabilityIds.Count == 0 ||
                    profile.AvailabilityIds.Any(string.IsNullOrWhiteSpace) ||
                    profile.AvailabilityIds.Distinct(StringComparer.Ordinal)
                        .Count() != profile.AvailabilityIds.Count ||
                    !string.Equals(profile.MaterialSetId,
                        LuoyangP0FinalAssetVerticalSliceIds.MaterialSetId,
                        StringComparison.Ordinal) ||
                    !string.Equals(profile.LodProfileId,
                        LuoyangP0FinalAssetVerticalSliceIds.LodProfileId,
                        StringComparison.Ordinal) ||
                    !string.Equals(profile.RuntimeCandidateModeId,
                        LuoyangP0FinalAssetVerticalSliceIds.RuntimeCandidateModeId,
                        StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(profile.ArtistPrefabResourcePath) ||
                    !profile.ArtistPrefabResourcePath.StartsWith(
                        "Art/Han/Luoyang/P0Final/", StringComparison.Ordinal) ||
                    !prefabPaths.Add(profile.ArtistPrefabResourcePath) ||
                    string.IsNullOrWhiteSpace(profile.ArtistFbxTargetPath) ||
                    !profile.ArtistPrefabPresent || !profile.FinalArtApproved)
                    throw new InvalidOperationException(
                        "Invalid Luoyang P0 final-asset vertical-slice profile.");

                ValidateIdentity(profile, landmarkByFacility, gateByFacility,
                    reviewByFacility);
                ValidateGeometry(profile, model, materialIds);
                result.Add(profile.FacilityId, profile);
            }

            if (!facilities.SetEquals(expectedFacilities))
                throw new InvalidOperationException(
                    "Luoyang P0 final-asset vertical slice is incomplete.");
            return new LuoyangP0FinalAssetVerticalSlicePlan(catalog, result);
        }

        private static void ValidateHeader(
            LuoyangP0FinalAssetVerticalSliceCatalog catalog,
            string regionalStyleId)
        {
            if (!string.Equals(catalog.SchemaId,
                    LuoyangP0FinalAssetVerticalSliceIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.TaskId,
                    LuoyangP0FinalAssetVerticalSliceIds.TaskId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.RegionalStyleId, regionalStyleId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.ReplacementIdentityPolicyId,
                    LuoyangP0FinalAssetVerticalSliceIds
                        .ReplacementIdentityPolicyId, StringComparison.Ordinal) ||
                !string.Equals(catalog.RuntimeCandidateModeId,
                    LuoyangP0FinalAssetVerticalSliceIds.RuntimeCandidateModeId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.ArtistPrefabSourceStatusId,
                    LuoyangP0FinalAssetVerticalSliceIds
                        .ArtistPrefabSourceStatusId, StringComparison.Ordinal) ||
                !string.Equals(catalog.FinalArtApprovalStatusId,
                    LuoyangP0FinalAssetVerticalSliceIds
                        .FinalArtApprovalStatusId, StringComparison.Ordinal) ||
                !string.Equals(catalog.UserReviewDecisionStatusId,
                    LuoyangP0FinalAssetVerticalSliceIds
                        .UserReviewDecisionStatusId, StringComparison.Ordinal) ||
                !string.Equals(catalog.UserReviewDecisionRecordId,
                    LuoyangP0FinalAssetVerticalSliceIds
                        .UserReviewDecisionRecordId, StringComparison.Ordinal) ||
                !string.Equals(catalog.UserReviewDecisionDate,
                    LuoyangP0FinalAssetVerticalSliceIds.UserReviewDecisionDate,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.SourceArchiveStatusId,
                    LuoyangP0FinalAssetVerticalSliceIds.SourceArchiveStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.SourceLicenseId,
                    LuoyangP0FinalAssetVerticalSliceIds.SourceLicenseId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FbxSourceToolchainId,
                    LuoyangP0FinalAssetVerticalSliceIds.FbxSourceToolchainId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FbxToolchainLicenseId,
                    LuoyangP0FinalAssetVerticalSliceIds.FbxToolchainLicenseId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.FbxAnchorNameMappingId,
                    LuoyangP0FinalAssetVerticalSliceIds.FbxAnchorNameMappingId,
                    StringComparison.Ordinal) ||
                catalog.ProfileCount !=
                    LuoyangP0FinalAssetVerticalSliceIds.ProfileCount ||
                catalog.MaterialCount !=
                    LuoyangP0FinalAssetVerticalSliceIds.MaterialCount ||
                catalog.Materials == null || catalog.Materials.Count !=
                    LuoyangP0FinalAssetVerticalSliceIds.MaterialCount ||
                catalog.Profiles == null || catalog.Profiles.Count !=
                    LuoyangP0FinalAssetVerticalSliceIds.ProfileCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang P0 final-asset vertical-slice header.");
        }

        private static HashSet<string> ValidateMaterials(
            IEnumerable<HanBuildableFacilityMaterialDefinition> materials)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var material in materials)
                if (material == null ||
                    string.IsNullOrWhiteSpace(material.MaterialId) ||
                    !material.MaterialId.StartsWith("material.han.p0.",
                        StringComparison.Ordinal) ||
                    !result.Add(material.MaterialId) ||
                    !Unit(material.Red) || !Unit(material.Green) ||
                    !Unit(material.Blue) || !Unit(material.Alpha) ||
                    !Unit(material.Metallic) || !Unit(material.Smoothness))
                    throw new InvalidOperationException(
                        "Invalid Luoyang P0 final-asset material.");
            return result;
        }

        private static void ValidateIdentity(LuoyangP0FinalAssetProfile profile,
            IReadOnlyDictionary<string, LuoyangHistoricalLandmarkProfile> landmarks,
            IReadOnlyDictionary<string, LuoyangGateIdentityProfile> gates,
            IReadOnlyDictionary<string, LuoyangFinalAssetReviewItem> review)
        {
            if (!review.TryGetValue(profile.FacilityId, out var item) ||
                !string.Equals(item.PriorityId,
                    LuoyangFinalAssetReviewIds.PriorityP0,
                    StringComparison.Ordinal) ||
                !string.Equals(item.SourceProfileId, profile.SourceProfileId,
                    StringComparison.Ordinal) ||
                !string.Equals(item.ModelId, profile.ModelId,
                    StringComparison.Ordinal) ||
                !string.Equals(item.AssetVariantId, profile.AssetVariantId,
                    StringComparison.Ordinal) ||
                item.RepresentativeCellId64 != profile.CellId64 ||
                item.RepresentativeGridColumn != profile.GridColumn ||
                item.RepresentativeGridRow != profile.GridRow)
                throw new InvalidOperationException(
                    "Luoyang P0 candidate does not match its final-asset slot.");

            if (landmarks.TryGetValue(profile.FacilityId, out var landmark))
            {
                if (!string.Equals(landmark.ProfileId, profile.SourceProfileId,
                        StringComparison.Ordinal) ||
                    !string.Equals(landmark.BaseModelId, profile.ModelId,
                        StringComparison.Ordinal) ||
                    !string.Equals(landmark.AssetVariantId,
                        profile.AssetVariantId, StringComparison.Ordinal) ||
                    landmark.CellId64 != profile.CellId64 ||
                    !string.Equals(landmark.HistoricalConfidence,
                        profile.HistoricalConfidence, StringComparison.Ordinal) ||
                    !string.Equals(landmark.SpatialPrecision,
                        profile.SpatialPrecision, StringComparison.Ordinal) ||
                    !new HashSet<string>(landmark.SourceIds,
                            StringComparer.Ordinal).SetEquals(profile.SourceIds) ||
                    !new HashSet<string>(landmark.AvailabilityIds,
                            StringComparer.Ordinal).SetEquals(
                            profile.AvailabilityIds))
                    throw new InvalidOperationException(
                        "Luoyang P0 landmark candidate changes frozen identity.");
                return;
            }

            if (!gates.TryGetValue(profile.FacilityId, out var gate) ||
                !string.Equals(gate.ProfileId, profile.SourceProfileId,
                    StringComparison.Ordinal) ||
                !string.Equals(gate.BaseModelId, profile.ModelId,
                    StringComparison.Ordinal) ||
                !string.Equals(gate.AssetVariantId, profile.AssetVariantId,
                    StringComparison.Ordinal) || gate.CellId64 != profile.CellId64 ||
                !string.Equals(gate.HistoricalConfidence,
                    profile.HistoricalConfidence, StringComparison.Ordinal) ||
                !string.Equals(gate.SpatialPrecision,
                    profile.SpatialPrecision, StringComparison.Ordinal) ||
                !new HashSet<string>(gate.SourceIds,
                        StringComparer.Ordinal).SetEquals(profile.SourceIds) ||
                !new HashSet<string>(gate.AvailabilityIds,
                        StringComparer.Ordinal).SetEquals(profile.AvailabilityIds))
                throw new InvalidOperationException(
                    "Luoyang P0 gate candidate changes frozen identity.");
        }

        private static void ValidateGeometry(LuoyangP0FinalAssetProfile profile,
            HanBuildableFacilityModelDefinition model,
            HashSet<string> materialIds)
        {
            if (profile.Modules == null || profile.Modules.Count < 8 ||
                profile.Modules.Count > 32 || profile.Anchors == null ||
                profile.Anchors.Count < 2 || profile.Anchors.Count > 3 ||
                profile.Lod1ModuleIds == null ||
                profile.Lod1ModuleIds.Count == 0 ||
                profile.Lod2ModuleIds == null ||
                profile.Lod2ModuleIds.Count == 0)
                throw new InvalidOperationException(
                    "Luoyang P0 candidate geometry contract is incomplete.");

            var halfFootprint = model.StrategicFootprintRatio * 0.5f;
            var moduleIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var module in profile.Modules)
            {
                if (module == null ||
                    string.IsNullOrWhiteSpace(module.ModuleId) ||
                    !moduleIds.Add(module.ModuleId) ||
                    !AllowedPrimitives.Contains(module.PrimitiveId ?? string.Empty) ||
                    !materialIds.Contains(module.MaterialId ?? string.Empty) ||
                    !Finite(module.PositionX) || !Finite(module.PositionY) ||
                    !Finite(module.PositionZ) || !Finite(module.RotationX) ||
                    !Finite(module.RotationY) || !Finite(module.RotationZ) ||
                    !Finite(module.ScaleX) || !Finite(module.ScaleY) ||
                    !Finite(module.ScaleZ) || module.ScaleX <= 0f ||
                    module.ScaleY <= 0f || module.ScaleZ <= 0f ||
                    Math.Abs(module.PositionX) + module.ScaleX * 0.5f >
                    halfFootprint + 0.0001f ||
                    Math.Abs(module.PositionZ) + module.ScaleZ * 0.5f >
                    halfFootprint + 0.0001f)
                    throw new InvalidOperationException(
                        "Invalid Luoyang P0 candidate module.");
            }

            var lod1 = Set(profile.Lod1ModuleIds, moduleIds, "LOD1");
            var lod2 = Set(profile.Lod2ModuleIds, moduleIds, "LOD2");
            if (!lod2.IsSubsetOf(lod1))
                throw new InvalidOperationException(
                    "Luoyang P0 candidate LOD2 must be a subset of LOD1.");

            var anchorIds = new HashSet<string>(StringComparer.Ordinal);
            var roles = new HashSet<string>(StringComparer.Ordinal);
            foreach (var anchor in profile.Anchors)
                if (anchor == null || string.IsNullOrWhiteSpace(anchor.AnchorId) ||
                    !anchorIds.Add(anchor.AnchorId) ||
                    string.IsNullOrWhiteSpace(anchor.RoleId) ||
                    !roles.Add(anchor.RoleId) || !Finite(anchor.X) ||
                    !Finite(anchor.Y) || !Finite(anchor.Z) || anchor.Y < 0f ||
                    Math.Abs(anchor.X) > halfFootprint + 0.0001f ||
                    Math.Abs(anchor.Z) > halfFootprint + 0.0001f)
                    throw new InvalidOperationException(
                        "Invalid Luoyang P0 candidate anchor.");

            var isGate = LuoyangGateIdentityKitIds.FacilityIds.Contains(
                profile.FacilityId);
            var expectedRoles = isGate
                ? new HashSet<string>(new[]
                {
                    LuoyangP0FinalAssetVerticalSliceIds.PlacementAnchorRoleId,
                    LuoyangP0FinalAssetVerticalSliceIds.OuterPassageAnchorRoleId,
                    LuoyangP0FinalAssetVerticalSliceIds.InnerPassageAnchorRoleId
                }, StringComparer.Ordinal)
                : new HashSet<string>(new[]
                {
                    LuoyangP0FinalAssetVerticalSliceIds.PlacementAnchorRoleId,
                    LuoyangP0FinalAssetVerticalSliceIds.EntranceAnchorRoleId
                }, StringComparer.Ordinal);
            if (!roles.SetEquals(expectedRoles))
                throw new InvalidOperationException(
                    "Luoyang P0 candidate anchor roles are invalid.");
        }

        private static HashSet<string> Set(IEnumerable<string> values,
            HashSet<string> allowed, string level)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
                if (!allowed.Contains(value ?? string.Empty) || !result.Add(value))
                    throw new InvalidOperationException(
                        "Invalid Luoyang P0 candidate " + level + " module list.");
            return result;
        }

        private static bool Unit(float value) =>
            Finite(value) && value >= 0f && value <= 1f;

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
