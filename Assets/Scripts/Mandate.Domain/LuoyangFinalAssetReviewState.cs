using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangFinalAssetReviewIds
    {
        public const string SchemaId =
            "mandate.luoyang-final-asset-review-manifest.v1";
        public const string ManifestId =
            "LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1";
        public const string ReplacementIdentityPolicyId =
            "replacement.identity.keep_model_asset_profile_facility.v1";
        public const string CurrentSourceLicenseId =
            "license.project-original.procedural.v1";
        public const string TargetAssetFormatId =
            "asset.prefab_fbx_pbr_lod_artist_authored.v1";
        public const string ReplacementStatusId =
            "replacement.status.candidate_required";

        public const string PriorityP0 = "priority.p0.identity_critical";
        public const string PriorityP1 = "priority.p1.high_exposure";
        public const string PriorityP2 = "priority.p2.system_readable";
        public const string PriorityP3 = "priority.p3.supporting_context";

        public const int OpeningFacilityCount = 2084;
        public const int AssetItemCount = 54;
        public const int AuditGroupCount = 9;
        public const int P0ItemCount = 24;
        public const int P1ItemCount = 10;
        public const int P2ItemCount = 14;
        public const int P3ItemCount = 6;
        public const int P0FacilityCount = 24;
        public const int P1FacilityCount = 1800;
        public const int P2FacilityCount = 226;
        public const int P3FacilityCount = 34;

        public static readonly IReadOnlyDictionary<string, int>
            ExpectedItemCountByPriority = new Dictionary<string, int>(
                StringComparer.Ordinal)
            {
                [PriorityP0] = P0ItemCount,
                [PriorityP1] = P1ItemCount,
                [PriorityP2] = P2ItemCount,
                [PriorityP3] = P3ItemCount
            };

        public static readonly IReadOnlyDictionary<string, int>
            ExpectedFacilityCountByPriority = new Dictionary<string, int>(
                StringComparer.Ordinal)
            {
                [PriorityP0] = P0FacilityCount,
                [PriorityP1] = P1FacilityCount,
                [PriorityP2] = P2FacilityCount,
                [PriorityP3] = P3FacilityCount
            };
    }

    [Serializable]
    public sealed class LuoyangFinalAssetReviewCatalog
    {
        public string SchemaId;
        public string ManifestId;
        public string RegionalStyleId;
        public int OpeningFacilityCount;
        public int AssetItemCount;
        public int AuditGroupCount;
        public string ReplacementIdentityPolicyId;
        public string CurrentSourceLicenseId;
        public string TargetAssetFormatId;
        public List<LuoyangFinalAssetAuditGroup> AuditGroups =
            new List<LuoyangFinalAssetAuditGroup>();
        public List<LuoyangFinalAssetReviewItem> Items =
            new List<LuoyangFinalAssetReviewItem>();
    }

    [Serializable]
    public sealed class LuoyangFinalAssetAuditGroup
    {
        public string AuditGroupId;
        public string DisplayName;
        public string PriorityId;
        public string PriorityReasonId;
        public int AssetItemCount;
        public int FacilityCount;
        public int SilhouetteReadinessScore;
        public int ProportionReadinessScore;
        public int MaterialReadinessScore;
        public int VariationReadinessScore;
        public List<string> FindingIds = new List<string>();
        public List<string> RequiredDeliverableIds = new List<string>();
    }

    [Serializable]
    public sealed class LuoyangFinalAssetReviewItem
    {
        public string ItemId;
        public int ReviewOrder;
        public string SourceKitId;
        public string SourceProfileId;
        public string ModelId;
        public string AssetVariantId;
        public int FacilityUsageCount;
        public string RepresentativeFacilityId;
        public string RepresentativeFacilityDefinitionId;
        public ulong RepresentativeCellId64;
        public int RepresentativeGridColumn;
        public int RepresentativeGridRow;
        public string AuditGroupId;
        public string PriorityId;
        public string ReplacementSlotId;
        public string ReplacementStatusId;
    }

    public sealed class LuoyangFinalAssetReviewPlan
    {
        public LuoyangFinalAssetReviewPlan(
            LuoyangFinalAssetReviewCatalog catalog,
            IReadOnlyDictionary<string, string> facilityAssetVariants)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            FacilityAssetVariants = facilityAssetVariants ??
                                    throw new ArgumentNullException(
                                        nameof(facilityAssetVariants));
        }

        public LuoyangFinalAssetReviewCatalog Catalog { get; }
        public IReadOnlyDictionary<string, string> FacilityAssetVariants { get; }
    }

    public static class LuoyangFinalAssetReviewRules
    {
        private sealed class ResolvedAsset
        {
            public string SourceKitId;
            public string SourceProfileId;
            public string ModelId;
            public string AssetVariantId;
        }

        public static LuoyangFinalAssetReviewPlan CreatePlan(
            LuoyangFinalAssetReviewCatalog catalog,
            LuoyangProductionBuildingKitCatalog production,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangMediumFrequencyUrbanFabricKitCatalog urbanFabric,
            LuoyangInfrastructureProductionKitCatalog infrastructure,
            LuoyangLowFrequencyDefenseProductionKitCatalog defense,
            LuoyangResourceAgricultureProductionKitCatalog resourceAgriculture,
            LuoyangFinalCivicRitualMedicalProductionKitCatalog finalCivic,
            LuoyangBuildingPerformancePlan wholeCity)
        {
            ValidateHeaderAndItems(catalog);
            if (production == null || landmarks == null || gates == null ||
                urbanFabric == null || infrastructure == null || defense == null ||
                resourceAgriculture == null || finalCivic == null ||
                wholeCity == null)
                throw new ArgumentNullException(nameof(wholeCity),
                    "Every accepted Luoyang production catalog and the whole-city plan are required.");

            var productionByModel = production.Profiles.ToDictionary(
                item => item.ModelId, item => Resolve(
                    LuoyangProductionBuildingKitIds.KitId, item.ProfileId,
                    item.ModelId, item.AssetVariantId), StringComparer.Ordinal);
            var landmarksByFacility = landmarks.Profiles.ToDictionary(
                item => item.FacilityId, item => Resolve(
                    LuoyangHistoricalLandmarkKitIds.KitId, item.ProfileId,
                    item.BaseModelId, item.AssetVariantId), StringComparer.Ordinal);
            var gatesByFacility = gates.Profiles.ToDictionary(
                item => item.FacilityId, item => Resolve(
                    LuoyangGateIdentityKitIds.KitId, item.ProfileId,
                    item.BaseModelId, item.AssetVariantId), StringComparer.Ordinal);
            var urbanByModel = urbanFabric.Profiles.ToDictionary(
                item => item.ModelId, item => Resolve(
                    LuoyangMediumFrequencyUrbanFabricKitIds.KitId,
                    item.ProfileId, item.ModelId, item.AssetVariantId),
                StringComparer.Ordinal);
            var infrastructureByModel = infrastructure.Profiles.ToDictionary(
                item => item.ModelId, item => Resolve(
                    LuoyangInfrastructureProductionKitIds.KitId,
                    item.ProfileId, item.ModelId, item.AssetVariantId),
                StringComparer.Ordinal);
            var defenseByFacility = MapFacilities(defense.Profiles,
                item => item.FacilityIds, item => Resolve(
                    LuoyangLowFrequencyDefenseProductionKitIds.KitId,
                    item.ProfileId, item.ModelId, item.AssetVariantId));
            var resourceByFacility = MapFacilities(resourceAgriculture.Profiles,
                item => item.FacilityIds, item => Resolve(
                    LuoyangResourceAgricultureProductionKitIds.KitId,
                    item.ProfileId, item.ModelId, item.AssetVariantId));
            var finalByFacility = MapFacilities(finalCivic.Profiles,
                item => item.FacilityIds, item => Resolve(
                    LuoyangFinalCivicRitualMedicalProductionKitIds.KitId,
                    item.ProfileId, item.ModelId, item.AssetVariantId));

            var itemsByAsset = catalog.Items.ToDictionary(
                item => item.AssetVariantId, StringComparer.Ordinal);
            var countsByAsset = catalog.Items.ToDictionary(
                item => item.AssetVariantId, _ => 0, StringComparer.Ordinal);
            var facilityAssets = new Dictionary<string, string>(
                StringComparer.Ordinal);
            var facilitiesById = wholeCity.Facilities.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);

            foreach (var facility in wholeCity.Facilities)
            {
                var resolved = ResolveFacility(facility, gatesByFacility,
                    landmarksByFacility, urbanByModel, infrastructureByModel,
                    defenseByFacility, resourceByFacility, finalByFacility,
                    productionByModel);
                if (!itemsByAsset.TryGetValue(resolved.AssetVariantId,
                        out var item) ||
                    !string.Equals(item.SourceKitId, resolved.SourceKitId,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.SourceProfileId,
                        resolved.SourceProfileId, StringComparison.Ordinal) ||
                    !string.Equals(item.ModelId, resolved.ModelId,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.ModelId, facility.ModelId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Luoyang final-asset manifest does not match runtime resolution for " +
                        facility.FacilityId + ".");
                countsByAsset[resolved.AssetVariantId]++;
                facilityAssets.Add(facility.FacilityId,
                    resolved.AssetVariantId);
            }

            foreach (var item in catalog.Items)
            {
                if (countsByAsset[item.AssetVariantId] != item.FacilityUsageCount ||
                    !facilitiesById.TryGetValue(item.RepresentativeFacilityId,
                        out var representative) ||
                    !facilityAssets.TryGetValue(item.RepresentativeFacilityId,
                        out var representativeAsset) ||
                    !string.Equals(representativeAsset, item.AssetVariantId,
                        StringComparison.Ordinal) ||
                    !string.Equals(representative.FacilityDefinitionId,
                        item.RepresentativeFacilityDefinitionId,
                        StringComparison.Ordinal) ||
                    representative.CellId64 != item.RepresentativeCellId64 ||
                    representative.GridColumn != item.RepresentativeGridColumn ||
                    representative.GridRow != item.RepresentativeGridRow)
                    throw new InvalidOperationException(
                        "Luoyang final-asset usage or representative Facility is invalid: " +
                        item.ItemId);
            }

            return new LuoyangFinalAssetReviewPlan(catalog, facilityAssets);
        }

        private static void ValidateHeaderAndItems(
            LuoyangFinalAssetReviewCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (!string.Equals(catalog.SchemaId,
                    LuoyangFinalAssetReviewIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.ManifestId,
                    LuoyangFinalAssetReviewIds.ManifestId,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(catalog.RegionalStyleId) ||
                catalog.OpeningFacilityCount !=
                    LuoyangFinalAssetReviewIds.OpeningFacilityCount ||
                catalog.AssetItemCount !=
                    LuoyangFinalAssetReviewIds.AssetItemCount ||
                catalog.AuditGroupCount !=
                    LuoyangFinalAssetReviewIds.AuditGroupCount ||
                !string.Equals(catalog.ReplacementIdentityPolicyId,
                    LuoyangFinalAssetReviewIds.ReplacementIdentityPolicyId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.CurrentSourceLicenseId,
                    LuoyangFinalAssetReviewIds.CurrentSourceLicenseId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.TargetAssetFormatId,
                    LuoyangFinalAssetReviewIds.TargetAssetFormatId,
                    StringComparison.Ordinal) ||
                catalog.AuditGroups == null || catalog.AuditGroups.Count !=
                    LuoyangFinalAssetReviewIds.AuditGroupCount ||
                catalog.Items == null || catalog.Items.Count !=
                    LuoyangFinalAssetReviewIds.AssetItemCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang final-asset review manifest header.");

            var groupIds = new HashSet<string>(StringComparer.Ordinal);
            var groupsById = new Dictionary<string, LuoyangFinalAssetAuditGroup>(
                StringComparer.Ordinal);
            foreach (var group in catalog.AuditGroups)
            {
                if (group == null || string.IsNullOrWhiteSpace(group.AuditGroupId) ||
                    !groupIds.Add(group.AuditGroupId) ||
                    string.IsNullOrWhiteSpace(group.DisplayName) ||
                    !LuoyangFinalAssetReviewIds.ExpectedItemCountByPriority
                        .ContainsKey(group.PriorityId ?? string.Empty) ||
                    string.IsNullOrWhiteSpace(group.PriorityReasonId) ||
                    group.AssetItemCount <= 0 || group.FacilityCount <= 0 ||
                    !Score(group.SilhouetteReadinessScore) ||
                    !Score(group.ProportionReadinessScore) ||
                    !Score(group.MaterialReadinessScore) ||
                    !Score(group.VariationReadinessScore) ||
                    group.FindingIds == null || group.FindingIds.Count == 0 ||
                    group.FindingIds.Distinct(StringComparer.Ordinal).Count() !=
                    group.FindingIds.Count ||
                    group.RequiredDeliverableIds == null ||
                    group.RequiredDeliverableIds.Count == 0 ||
                    group.RequiredDeliverableIds.Distinct(StringComparer.Ordinal)
                        .Count() != group.RequiredDeliverableIds.Count)
                    throw new InvalidOperationException(
                        "Invalid Luoyang final-asset audit group.");
                groupsById.Add(group.AuditGroupId, group);
            }

            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            var assetIds = new HashSet<string>(StringComparer.Ordinal);
            var orders = new HashSet<int>();
            foreach (var item in catalog.Items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId) ||
                    !itemIds.Add(item.ItemId) || item.ReviewOrder < 0 ||
                    item.ReviewOrder >= LuoyangFinalAssetReviewIds.AssetItemCount ||
                    !orders.Add(item.ReviewOrder) ||
                    string.IsNullOrWhiteSpace(item.SourceKitId) ||
                    string.IsNullOrWhiteSpace(item.SourceProfileId) ||
                    string.IsNullOrWhiteSpace(item.ModelId) ||
                    string.IsNullOrWhiteSpace(item.AssetVariantId) ||
                    !assetIds.Add(item.AssetVariantId) ||
                    item.FacilityUsageCount <= 0 ||
                    string.IsNullOrWhiteSpace(item.RepresentativeFacilityId) ||
                    string.IsNullOrWhiteSpace(
                        item.RepresentativeFacilityDefinitionId) ||
                    item.RepresentativeCellId64 == 0UL ||
                    item.RepresentativeGridColumn < 0 ||
                    item.RepresentativeGridRow < 0 ||
                    !groupsById.TryGetValue(item.AuditGroupId ?? string.Empty,
                        out var group) ||
                    !string.Equals(item.PriorityId, group.PriorityId,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.ReplacementSlotId, item.AssetVariantId,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.ReplacementStatusId,
                        LuoyangFinalAssetReviewIds.ReplacementStatusId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Invalid Luoyang final-asset review item.");
            }

            if (orders.Count != LuoyangFinalAssetReviewIds.AssetItemCount ||
                catalog.Items.Sum(item => item.FacilityUsageCount) !=
                LuoyangFinalAssetReviewIds.OpeningFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang final-asset review order or usage coverage is incomplete.");

            foreach (var group in catalog.AuditGroups)
            {
                var members = catalog.Items.Where(item => string.Equals(
                    item.AuditGroupId, group.AuditGroupId,
                    StringComparison.Ordinal)).ToArray();
                if (members.Length != group.AssetItemCount ||
                    members.Sum(item => item.FacilityUsageCount) !=
                    group.FacilityCount)
                    throw new InvalidOperationException(
                        "Luoyang final-asset audit group totals are invalid: " +
                        group.AuditGroupId);
            }

            foreach (var pair in
                     LuoyangFinalAssetReviewIds.ExpectedItemCountByPriority)
            {
                var items = catalog.Items.Where(item => string.Equals(
                    item.PriorityId, pair.Key, StringComparison.Ordinal)).ToArray();
                if (items.Length != pair.Value ||
                    items.Sum(item => item.FacilityUsageCount) !=
                    LuoyangFinalAssetReviewIds
                        .ExpectedFacilityCountByPriority[pair.Key])
                    throw new InvalidOperationException(
                        "Luoyang final-asset priority totals are invalid: " +
                        pair.Key);
            }
        }

        private static ResolvedAsset ResolveFacility(
            LuoyangBuildingPerformanceFacility facility,
            IReadOnlyDictionary<string, ResolvedAsset> gates,
            IReadOnlyDictionary<string, ResolvedAsset> landmarks,
            IReadOnlyDictionary<string, ResolvedAsset> urban,
            IReadOnlyDictionary<string, ResolvedAsset> infrastructure,
            IReadOnlyDictionary<string, ResolvedAsset> defense,
            IReadOnlyDictionary<string, ResolvedAsset> resources,
            IReadOnlyDictionary<string, ResolvedAsset> finalCivic,
            IReadOnlyDictionary<string, ResolvedAsset> production)
        {
            if (gates.TryGetValue(facility.FacilityId, out var result) ||
                landmarks.TryGetValue(facility.FacilityId, out result) ||
                urban.TryGetValue(facility.ModelId, out result) ||
                infrastructure.TryGetValue(facility.ModelId, out result) ||
                defense.TryGetValue(facility.FacilityId, out result) ||
                resources.TryGetValue(facility.FacilityId, out result) ||
                finalCivic.TryGetValue(facility.FacilityId, out result) ||
                production.TryGetValue(facility.ModelId, out result))
                return result;
            throw new InvalidOperationException(
                "Opening Facility has no final visual asset resolution: " +
                facility.FacilityId);
        }

        private static Dictionary<string, ResolvedAsset> MapFacilities<T>(
            IEnumerable<T> profiles,
            Func<T, IEnumerable<string>> facilityIds,
            Func<T, ResolvedAsset> resolver)
        {
            var result = new Dictionary<string, ResolvedAsset>(
                StringComparer.Ordinal);
            foreach (var profile in profiles)
            {
                var resolved = resolver(profile);
                foreach (var facilityId in facilityIds(profile))
                    result.Add(facilityId, resolved);
            }
            return result;
        }

        private static ResolvedAsset Resolve(string kitId, string profileId,
            string modelId, string assetVariantId) => new ResolvedAsset
        {
            SourceKitId = kitId,
            SourceProfileId = profileId,
            ModelId = modelId,
            AssetVariantId = assetVariantId
        };

        private static bool Score(int value) => value >= 1 && value <= 5;
    }
}
