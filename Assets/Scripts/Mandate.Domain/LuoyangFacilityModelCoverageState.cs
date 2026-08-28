using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangFacilityModelCoverageIds
    {
        public const string DryField = "model.han.luoyang.agriculture.dry_field.v1";
        public const string RiceField = "model.han.luoyang.agriculture.rice_field.v1";
        public const string Garden = "model.han.luoyang.agriculture.garden.v1";
        public const string Pasture = "model.han.luoyang.agriculture.pasture.v1";
        public const string GovernmentOffice = "model.han.luoyang.government.local_office.v1";
        public const string CentralOffice = "model.han.luoyang.government.central_office.v1";
        public const string PalaceComplex = "model.han.luoyang.palace.complex.v1";
        public const string School = "model.han.luoyang.education.school.v1";
        public const string ImperialAcademy = "model.han.luoyang.education.imperial_academy.v1";
        public const string MilitaryCamp = "model.han.luoyang.military.camp.v1";
        public const string Beacon = "model.han.luoyang.military.beacon.v1";
        public const string FortifiedManor = "model.han.luoyang.military.fortified_manor.v1";
        public const string CaravanYard = "model.han.luoyang.service.caravan_yard.v1";
        public const string InnPost = "model.han.luoyang.service.inn_post.v1";
        public const string Clinic = "model.han.luoyang.service.clinic.v1";
        public const string Well = "model.han.luoyang.public.well.v1";
        public const string Canal = "model.han.luoyang.public.canal.v1";
        public const string Bridge = "model.han.luoyang.public.bridge.v1";
        public const string Plaza = "model.han.luoyang.public.plaza.v1";
        public const string Forestry = "model.han.luoyang.resource.forestry.v1";
        public const string MineQuarry = "model.han.luoyang.resource.mine_quarry.v1";
        public const string RitualHall = "model.han.luoyang.ritual.hall.v1";
        public const string Observatory = "model.han.luoyang.ritual.observatory.v1";
        public const string PalaceWall = "model.han.luoyang.fortification.palace_wall.v1";
        public const string PalaceGate = "model.han.luoyang.fortification.palace_gate.v1";
        public const string ImperialGranary = "model.han.luoyang.storage.imperial_granary.v1";
        public const string Arsenal = "model.han.luoyang.military.arsenal.v1";
        public const string ImperialGarden = "model.han.luoyang.public.imperial_garden.v1";
        public const string RoadSegment = "model.han.luoyang.public.road_segment.v1";

        public const string DryFieldAsset = "HAN_AGRICULTURE_DRY_FIELD_A";
        public const string RiceFieldAsset = "HAN_AGRICULTURE_RICE_FIELD_A";
        public const string GardenAsset = "HAN_AGRICULTURE_GARDEN_A";
        public const string PastureAsset = "HAN_AGRICULTURE_PASTURE_A";
        public const string GovernmentOfficeAsset = "HAN_GOVERNMENT_OFFICE_A";
        public const string CentralOfficeAsset = "HAN_CENTRAL_OFFICE_A";
        public const string PalaceComplexAsset = "HAN_PALACE_A";
        public const string SchoolAsset = "HAN_EDUCATION_SCHOOL_A";
        public const string ImperialAcademyAsset = "HAN_IMPERIAL_ACADEMY_A";
        public const string MilitaryCampAsset = "HAN_MILITARY_CAMP_A";
        public const string BeaconAsset = "HAN_MILITARY_BEACON_A";
        public const string FortifiedManorAsset = "HAN_FORTIFIED_MANOR_A";
        public const string CaravanYardAsset = "HAN_CARAVAN_YARD_A";
        public const string InnPostAsset = "HAN_INN_POST_A";
        public const string ClinicAsset = "HAN_CLINIC_A";
        public const string WellAsset = "HAN_PUBLIC_WELL_A";
        public const string CanalAsset = "HAN_PUBLIC_CANAL_A";
        public const string BridgeAsset = "HAN_PUBLIC_BRIDGE_A";
        public const string PlazaAsset = "HAN_PUBLIC_PLAZA_A";
        public const string ForestryAsset = "HAN_FORESTRY_A";
        public const string MineQuarryAsset = "HAN_MINE_QUARRY_A";
        public const string RitualHallAsset = "HAN_RITUAL_HALL_A";
        public const string ObservatoryAsset = "HAN_OBSERVATORY_A";
        public const string PalaceWallAsset = "HAN_PALACE_WALL_A";
        public const string PalaceGateAsset = "HAN_PALACE_GATE_A";
        public const string ImperialGranaryAsset = "HAN_IMPERIAL_GRANARY_A";
        public const string ArsenalAsset = "HAN_ARSENAL_A";
        public const string ImperialGardenAsset = "HAN_IMPERIAL_GARDEN_A";
        public const string RoadSegmentAsset = "HAN_ROAD_SEGMENT_A";

        public static readonly IReadOnlyList<string> SupplementalModelIds = new[]
        {
            DryField, RiceField, Garden, Pasture,
            GovernmentOffice, CentralOffice, PalaceComplex,
            School, ImperialAcademy,
            MilitaryCamp, Beacon, FortifiedManor,
            CaravanYard, InnPost, Clinic,
            Well, Canal, Bridge, Plaza,
            Forestry, MineQuarry,
            RitualHall, Observatory,
            PalaceWall, PalaceGate,
            ImperialGranary, Arsenal, ImperialGarden, RoadSegment
        };

        public static readonly IReadOnlyList<string> AllModelIds =
            HanBuildableFacilityModelIds.AllModelIds
                .Concat(SupplementalModelIds).ToArray();
    }

    [Serializable]
    public sealed class LuoyangFacilityModelBindingCatalog
    {
        public string SchemaId;
        public List<LuoyangFacilityDefinitionModelBinding> DefinitionBindings =
            new List<LuoyangFacilityDefinitionModelBinding>();
        public List<LuoyangFacilityInstanceModelOverride> FacilityOverrides =
            new List<LuoyangFacilityInstanceModelOverride>();
    }

    [Serializable]
    public sealed class LuoyangFacilityDefinitionModelBinding
    {
        public string FacilityDefinitionId;
        public string ModelId;
    }

    [Serializable]
    public sealed class LuoyangFacilityInstanceModelOverride
    {
        public string FacilityId;
        public string ModelId;
        public string HistoricalBasis;
    }

    public static class LuoyangFacilityModelBindingRules
    {
        public const string SchemaId = "mandate.luoyang-facility-model-bindings.v1";

        public static void Validate(LuoyangFacilityModelBindingCatalog bindings,
            HanBuildableFacilityModelCatalog models)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            if (!string.Equals(bindings.SchemaId, SchemaId, StringComparison.Ordinal) ||
                bindings.DefinitionBindings == null ||
                bindings.DefinitionBindings.Count == 0 ||
                bindings.FacilityOverrides == null)
                throw new InvalidOperationException(
                    "Invalid Luoyang Facility model binding catalog header.");

            var modelIds = new HashSet<string>(models.Models.Select(item => item.ModelId),
                StringComparer.Ordinal);
            var definitionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in bindings.DefinitionBindings)
                if (binding == null ||
                    string.IsNullOrWhiteSpace(binding.FacilityDefinitionId) ||
                    !definitionIds.Add(binding.FacilityDefinitionId) ||
                    !modelIds.Contains(binding.ModelId ?? string.Empty))
                    throw new InvalidOperationException(
                        "Invalid Luoyang Facility definition model binding.");

            var facilityIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in bindings.FacilityOverrides)
                if (binding == null || string.IsNullOrWhiteSpace(binding.FacilityId) ||
                    !facilityIds.Add(binding.FacilityId) ||
                    !modelIds.Contains(binding.ModelId ?? string.Empty) ||
                    string.IsNullOrWhiteSpace(binding.HistoricalBasis))
                    throw new InvalidOperationException(
                        "Invalid Luoyang Facility instance model override.");
        }
    }

    public sealed class LuoyangFacilityModelBindingResolver
    {
        private readonly Dictionary<string, string> _definitionModels =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _facilityModels =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public LuoyangFacilityModelBindingResolver(
            LuoyangFacilityModelBindingCatalog bindings,
            HanBuildableFacilityModelCatalog models)
        {
            LuoyangFacilityModelBindingRules.Validate(bindings, models);
            foreach (var binding in bindings.DefinitionBindings)
                _definitionModels.Add(binding.FacilityDefinitionId, binding.ModelId);
            foreach (var binding in bindings.FacilityOverrides)
                _facilityModels.Add(binding.FacilityId, binding.ModelId);
        }

        public string ResolveModelId(string facilityDefinitionId,
            string facilityId = null)
        {
            if (!string.IsNullOrEmpty(facilityId) &&
                _facilityModels.TryGetValue(facilityId, out var overrideModel))
                return overrideModel;
            return _definitionModels.TryGetValue(facilityDefinitionId ?? string.Empty,
                out var modelId) ? modelId : null;
        }

        public bool CoversDefinition(string facilityDefinitionId) =>
            _definitionModels.ContainsKey(facilityDefinitionId ?? string.Empty);
    }

    public static class HanBuildableFacilityModelCatalogComposer
    {
        public static HanBuildableFacilityModelCatalog Combine(
            params HanBuildableFacilityModelCatalog[] catalogs)
        {
            if (catalogs == null || catalogs.Length == 0)
                throw new ArgumentException("At least one model catalog is required.",
                    nameof(catalogs));
            var combined = new HanBuildableFacilityModelCatalog
            {
                SchemaId = HanBuildableFacilityModelCatalogRules.SchemaId,
                RegionalStyleId = catalogs[0]?.RegionalStyleId
            };
            foreach (var catalog in catalogs)
            {
                HanBuildableFacilityModelCatalogRules.Validate(catalog);
                if (!string.Equals(combined.RegionalStyleId, catalog.RegionalStyleId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Cannot combine Han model catalogs with different regional styles.");
                combined.Materials.AddRange(catalog.Materials);
                combined.Models.AddRange(catalog.Models);
            }
            HanBuildableFacilityModelCatalogRules.Validate(combined);
            return combined;
        }
    }

    public static class LuoyangProductionBuildingKitIds
    {
        public const string SchemaId =
            "mandate.luoyang-production-building-kit.v1";
        public const string KitId = "LUOYANG_PRODUCTION_BUILDING_KIT_V1";
        public const string LodProfileId =
            "lod.han.strategy.building.three_tier.v1";
        public const string MaterialSetId =
            "material_set.han.central_plains.shared.v1";
        public const int OpeningFacilityCount = 2084;
        public const int CoveredOpeningFacilityCount = 1800;

        public static readonly IReadOnlyList<string> HighFrequencyModelIds = new[]
        {
            HanBuildableFacilityModelIds.Residence,
            LuoyangFacilityModelCoverageIds.DryField,
            LuoyangFacilityModelCoverageIds.RoadSegment,
            HanBuildableFacilityModelIds.Workshop,
            LuoyangFacilityModelCoverageIds.Garden,
            HanBuildableFacilityModelIds.Warehouse,
            HanBuildableFacilityModelIds.CityWall,
            LuoyangFacilityModelCoverageIds.PalaceWall,
            LuoyangFacilityModelCoverageIds.InnPost,
            LuoyangFacilityModelCoverageIds.Pasture
        };

        public static readonly IReadOnlyDictionary<string, int> OpeningUsageByModelId =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [HanBuildableFacilityModelIds.Residence] = 552,
                [LuoyangFacilityModelCoverageIds.DryField] = 361,
                [LuoyangFacilityModelCoverageIds.RoadSegment] = 359,
                [HanBuildableFacilityModelIds.Workshop] = 94,
                [LuoyangFacilityModelCoverageIds.Garden] = 92,
                [HanBuildableFacilityModelIds.Warehouse] = 85,
                [HanBuildableFacilityModelIds.CityWall] = 76,
                [LuoyangFacilityModelCoverageIds.PalaceWall] = 70,
                [LuoyangFacilityModelCoverageIds.InnPost] = 60,
                [LuoyangFacilityModelCoverageIds.Pasture] = 51
            };
    }

    [Serializable]
    public sealed class LuoyangProductionBuildingKitCatalog
    {
        public string SchemaId;
        public string KitId;
        public string RegionalStyleId;
        public int OpeningFacilityCount;
        public int CoveredOpeningFacilityCount;
        public List<LuoyangProductionBuildingProfile> Profiles =
            new List<LuoyangProductionBuildingProfile>();
    }

    [Serializable]
    public sealed class LuoyangProductionBuildingProfile
    {
        public string ProfileId;
        public string ModelId;
        public string AssetVariantId;
        public string LodProfileId;
        public string MaterialSetId;
        public string PlacementAnchorId;
        public string EntranceAnchorId;
        public float EntranceX;
        public float EntranceY;
        public float EntranceZ;
        public bool TerrainConforming;
        public int OpeningUsageCount;
        public List<LuoyangProductionPrimitiveOverride> PrimitiveOverrides =
            new List<LuoyangProductionPrimitiveOverride>();
        public List<string> Lod1ModuleIds = new List<string>();
        public List<string> Lod2ModuleIds = new List<string>();
    }

    [Serializable]
    public sealed class LuoyangProductionPrimitiveOverride
    {
        public string ModuleId;
        public string PrimitiveId;
    }

    public static class LuoyangProductionBuildingKitRules
    {
        private static readonly HashSet<string> AllowedProductionPrimitives =
            new HashSet<string>(new[]
            {
                "han.rammed_block",
                "han.tile_slab",
                "han.field_ridge",
                "han.terrain_pad",
                "han.road_crown",
                "han.foliage_cluster",
                "han.wall_coping",
                "han.timber_beam"
            }, StringComparer.Ordinal);

        public static void Validate(LuoyangProductionBuildingKitCatalog production,
            HanBuildableFacilityModelCatalog models)
        {
            if (production == null) throw new ArgumentNullException(nameof(production));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            if (!string.Equals(production.SchemaId,
                    LuoyangProductionBuildingKitIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(production.KitId,
                    LuoyangProductionBuildingKitIds.KitId,
                    StringComparison.Ordinal) ||
                !string.Equals(production.RegionalStyleId, models.RegionalStyleId,
                    StringComparison.Ordinal) ||
                production.OpeningFacilityCount !=
                    LuoyangProductionBuildingKitIds.OpeningFacilityCount ||
                production.CoveredOpeningFacilityCount !=
                    LuoyangProductionBuildingKitIds.CoveredOpeningFacilityCount ||
                production.Profiles == null ||
                production.Profiles.Count !=
                    LuoyangProductionBuildingKitIds.HighFrequencyModelIds.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang production building kit header.");

            var modelsById = models.Models.ToDictionary(item => item.ModelId,
                StringComparer.Ordinal);
            var expectedIds = new HashSet<string>(
                LuoyangProductionBuildingKitIds.HighFrequencyModelIds,
                StringComparer.Ordinal);
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            var assetVariantIds = new HashSet<string>(StringComparer.Ordinal);
            var actualIds = new HashSet<string>(StringComparer.Ordinal);
            var usageTotal = 0;
            foreach (var profile in production.Profiles)
            {
                if (profile == null ||
                    string.IsNullOrWhiteSpace(profile.ProfileId) ||
                    !profileIds.Add(profile.ProfileId) ||
                    string.IsNullOrWhiteSpace(profile.AssetVariantId) ||
                    !assetVariantIds.Add(profile.AssetVariantId) ||
                    !expectedIds.Contains(profile.ModelId ?? string.Empty) ||
                    !actualIds.Add(profile.ModelId) ||
                    !string.Equals(profile.LodProfileId,
                        LuoyangProductionBuildingKitIds.LodProfileId,
                        StringComparison.Ordinal) ||
                    !string.Equals(profile.MaterialSetId,
                        LuoyangProductionBuildingKitIds.MaterialSetId,
                        StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(profile.PlacementAnchorId) ||
                    string.IsNullOrWhiteSpace(profile.EntranceAnchorId) ||
                    !Finite(profile.EntranceX) || !Finite(profile.EntranceY) ||
                    !Finite(profile.EntranceZ) || profile.EntranceY < 0f ||
                    profile.OpeningUsageCount <= 0 ||
                    !LuoyangProductionBuildingKitIds.OpeningUsageByModelId.TryGetValue(
                        profile.ModelId ?? string.Empty, out var expectedUsage) ||
                    profile.OpeningUsageCount != expectedUsage ||
                    profile.PrimitiveOverrides == null ||
                    profile.PrimitiveOverrides.Count == 0 ||
                    profile.Lod1ModuleIds == null || profile.Lod1ModuleIds.Count == 0 ||
                    profile.Lod2ModuleIds == null || profile.Lod2ModuleIds.Count == 0 ||
                    profile.Lod2ModuleIds.Count > profile.Lod1ModuleIds.Count)
                    throw new InvalidOperationException(
                        "Invalid Luoyang production building profile.");

                var model = modelsById[profile.ModelId];
                var moduleIds = new HashSet<string>(
                    model.Modules.Select(item => item.ModuleId), StringComparer.Ordinal);
                var halfFootprint = model.StrategicFootprintRatio * 0.5f;
                if (Math.Abs(profile.EntranceX) > halfFootprint + 0.0001f ||
                    Math.Abs(profile.EntranceZ) > halfFootprint + 0.0001f)
                    throw new InvalidOperationException(
                        "Luoyang production entrance anchor exceeds its Cell footprint.");

                var overrideIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in profile.PrimitiveOverrides)
                    if (item == null || !moduleIds.Contains(item.ModuleId ?? string.Empty) ||
                        !overrideIds.Add(item.ModuleId) ||
                        !AllowedProductionPrimitives.Contains(
                            item.PrimitiveId ?? string.Empty))
                        throw new InvalidOperationException(
                            "Invalid Luoyang production primitive override.");

                var lod1 = ValidateLodModules(profile.Lod1ModuleIds, moduleIds,
                    "LOD1");
                var lod2 = ValidateLodModules(profile.Lod2ModuleIds, moduleIds,
                    "LOD2");
                if (!lod2.IsSubsetOf(lod1))
                    throw new InvalidOperationException(
                        "Luoyang production LOD2 modules must be a subset of LOD1.");
                usageTotal += profile.OpeningUsageCount;
            }

            if (!actualIds.SetEquals(expectedIds) ||
                usageTotal != production.CoveredOpeningFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang production building kit coverage does not match the frozen opening world.");
        }

        private static HashSet<string> ValidateLodModules(IEnumerable<string> values,
            HashSet<string> moduleIds, string level)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
                if (!moduleIds.Contains(value ?? string.Empty) || !result.Add(value))
                    throw new InvalidOperationException(
                        "Invalid Luoyang production " + level + " module list.");
            return result;
        }

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public static class LuoyangHistoricalLandmarkKitIds
    {
        public const string SchemaId =
            "mandate.luoyang-historical-landmark-kit.v1";
        public const string KitId =
            "LUOYANG_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1";
        public const string LodProfileId =
            "lod.han.strategy.landmark.three_tier.v1";
        public const string MaterialSetId =
            "material_set.han.central_plains.shared.v1";

        public const string SouthPalace =
            "facility.instance.luoyang.184.south_palace";
        public const string NorthPalace =
            "facility.instance.luoyang.184.north_palace";
        public const string YonganPalace =
            "facility.instance.luoyang.184.yongan_palace";
        public const string Taixue =
            "facility.instance.luoyang.184.taixue";
        public const string Mingtang =
            "facility.instance.luoyang.184.mingtang";
        public const string Biyong =
            "facility.instance.luoyang.184.biyong";
        public const string Lingtai =
            "facility.instance.luoyang.184.lingtai";
        public const string Taicang =
            "facility.instance.luoyang.184.taicang";
        public const string Arsenal =
            "facility.instance.luoyang.184.arsenal";
        public const string ZhuolongGarden =
            "facility.instance.luoyang.184.zhuolong_garden";

        public static readonly IReadOnlyList<string> FacilityIds = new[]
        {
            SouthPalace, NorthPalace, YonganPalace, Taixue, Mingtang,
            Biyong, Lingtai, Taicang, Arsenal, ZhuolongGarden
        };

        public static readonly IReadOnlyDictionary<string, string> BaseModelIds =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [SouthPalace] = LuoyangFacilityModelCoverageIds.PalaceComplex,
                [NorthPalace] = LuoyangFacilityModelCoverageIds.PalaceComplex,
                [YonganPalace] = LuoyangFacilityModelCoverageIds.PalaceComplex,
                [Taixue] = LuoyangFacilityModelCoverageIds.ImperialAcademy,
                [Mingtang] = LuoyangFacilityModelCoverageIds.RitualHall,
                [Biyong] = LuoyangFacilityModelCoverageIds.RitualHall,
                [Lingtai] = LuoyangFacilityModelCoverageIds.Observatory,
                [Taicang] = LuoyangFacilityModelCoverageIds.ImperialGranary,
                [Arsenal] = LuoyangFacilityModelCoverageIds.Arsenal,
                [ZhuolongGarden] = LuoyangFacilityModelCoverageIds.ImperialGarden
            };

        public static readonly IReadOnlyDictionary<string, ulong> CellIds =
            new Dictionary<string, ulong>(StringComparer.Ordinal)
            {
                [SouthPalace] = 4127973UL,
                [NorthPalace] = 4098147UL,
                [YonganPalace] = 4101458UL,
                [Taixue] = 4154491UL,
                [Mingtang] = 4161110UL,
                [Biyong] = 4161116UL,
                [Lingtai] = 4161107UL,
                [Taicang] = 4134598UL,
                [Arsenal] = 4134604UL,
                [ZhuolongGarden] = 4101464UL
            };

        public static readonly IReadOnlyDictionary<string, int> GridX =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [SouthPalace] = 2043, [NorthPalace] = 2043,
                [YonganPalace] = 2040, [Taixue] = 2049,
                [Mingtang] = 2040, [Biyong] = 2046,
                [Lingtai] = 2037, [Taicang] = 2040,
                [Arsenal] = 2046, [ZhuolongGarden] = 2046
            };

        public static readonly IReadOnlyDictionary<string, int> GridY =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [SouthPalace] = 1245, [NorthPalace] = 1236,
                [YonganPalace] = 1237, [Taixue] = 1253,
                [Mingtang] = 1255, [Biyong] = 1255,
                [Lingtai] = 1255, [Taicang] = 1247,
                [Arsenal] = 1247, [ZhuolongGarden] = 1237
            };
    }

    [Serializable]
    public sealed class LuoyangHistoricalLandmarkKitCatalog
    {
        public string SchemaId;
        public string KitId;
        public string RegionalStyleId;
        public List<LuoyangHistoricalLandmarkProfile> Profiles =
            new List<LuoyangHistoricalLandmarkProfile>();
    }

    [Serializable]
    public sealed class LuoyangHistoricalLandmarkProfile
    {
        public string ProfileId;
        public string FacilityId;
        public string DisplayName;
        public string BaseModelId;
        public string AssetVariantId;
        public string SilhouetteId;
        public string LodProfileId;
        public string MaterialSetId;
        public ulong CellId64;
        public int GridX;
        public int GridY;
        public string HistoricalConfidence;
        public string SpatialPrecision;
        public string HistoricalBasis;
        public List<string> SourceIds = new List<string>();
        public List<string> AvailabilityIds = new List<string>();
        public string PlacementAnchorId;
        public string EntranceAnchorId;
        public float EntranceX;
        public float EntranceY;
        public float EntranceZ;
        public List<HanBuildableFacilityModuleDefinition> Modules =
            new List<HanBuildableFacilityModuleDefinition>();
        public List<string> Lod1ModuleIds = new List<string>();
        public List<string> Lod2ModuleIds = new List<string>();
    }

    public static class LuoyangHistoricalLandmarkKitRules
    {
        private static readonly HashSet<string> AllowedPrimitives =
            new HashSet<string>(new[]
            {
                "cube", "cylinder", "han.rammed_block", "han.tile_slab",
                "han.terrain_pad", "han.foliage_cluster", "han.wall_coping",
                "han.timber_beam", "han.hip_roof", "han.ritual_ring"
            }, StringComparer.Ordinal);

        private static readonly HashSet<string> AllowedAvailability =
            new HashSet<string>(new[]
            {
                "Government", "Military", "HistoricalInit", "Event"
            }, StringComparer.Ordinal);

        public static void Validate(LuoyangHistoricalLandmarkKitCatalog landmarks,
            HanBuildableFacilityModelCatalog models)
        {
            if (landmarks == null) throw new ArgumentNullException(nameof(landmarks));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            if (!string.Equals(landmarks.SchemaId,
                    LuoyangHistoricalLandmarkKitIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(landmarks.KitId,
                    LuoyangHistoricalLandmarkKitIds.KitId,
                    StringComparison.Ordinal) ||
                !string.Equals(landmarks.RegionalStyleId, models.RegionalStyleId,
                    StringComparison.Ordinal) ||
                landmarks.Profiles == null ||
                landmarks.Profiles.Count !=
                    LuoyangHistoricalLandmarkKitIds.FacilityIds.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang historical landmark kit header.");

            var modelsById = models.Models.ToDictionary(item => item.ModelId,
                StringComparer.Ordinal);
            var materialIds = new HashSet<string>(
                models.Materials.Select(item => item.MaterialId),
                StringComparer.Ordinal);
            var expectedFacilityIds = new HashSet<string>(
                LuoyangHistoricalLandmarkKitIds.FacilityIds,
                StringComparer.Ordinal);
            var facilityIds = new HashSet<string>(StringComparer.Ordinal);
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            var assetVariantIds = new HashSet<string>(StringComparer.Ordinal);
            var silhouetteIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var profile in landmarks.Profiles)
            {
                if (profile == null ||
                    !expectedFacilityIds.Contains(profile.FacilityId ?? string.Empty) ||
                    !facilityIds.Add(profile.FacilityId) ||
                    string.IsNullOrWhiteSpace(profile.ProfileId) ||
                    !profileIds.Add(profile.ProfileId) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    string.IsNullOrWhiteSpace(profile.AssetVariantId) ||
                    !assetVariantIds.Add(profile.AssetVariantId) ||
                    string.IsNullOrWhiteSpace(profile.SilhouetteId) ||
                    !silhouetteIds.Add(profile.SilhouetteId) ||
                    !string.Equals(profile.LodProfileId,
                        LuoyangHistoricalLandmarkKitIds.LodProfileId,
                        StringComparison.Ordinal) ||
                    !string.Equals(profile.MaterialSetId,
                        LuoyangHistoricalLandmarkKitIds.MaterialSetId,
                        StringComparison.Ordinal) ||
                    !LuoyangHistoricalLandmarkKitIds.BaseModelIds.TryGetValue(
                        profile.FacilityId ?? string.Empty, out var expectedModelId) ||
                    !string.Equals(profile.BaseModelId, expectedModelId,
                        StringComparison.Ordinal) ||
                    !modelsById.TryGetValue(profile.BaseModelId ?? string.Empty,
                        out var model) ||
                    profile.CellId64 !=
                        LuoyangHistoricalLandmarkKitIds.CellIds[profile.FacilityId] ||
                    profile.GridX !=
                        LuoyangHistoricalLandmarkKitIds.GridX[profile.FacilityId] ||
                    profile.GridY !=
                        LuoyangHistoricalLandmarkKitIds.GridY[profile.FacilityId] ||
                    string.IsNullOrWhiteSpace(profile.HistoricalConfidence) ||
                    string.IsNullOrWhiteSpace(profile.SpatialPrecision) ||
                    string.IsNullOrWhiteSpace(profile.HistoricalBasis) ||
                    profile.SourceIds == null || profile.SourceIds.Count == 0 ||
                    profile.SourceIds.Any(string.IsNullOrWhiteSpace) ||
                    profile.SourceIds.Distinct(StringComparer.Ordinal).Count() !=
                        profile.SourceIds.Count ||
                    profile.AvailabilityIds == null ||
                    !profile.AvailabilityIds.Contains("HistoricalInit") ||
                    profile.AvailabilityIds.Any(item =>
                        !AllowedAvailability.Contains(item ?? string.Empty)) ||
                    profile.AvailabilityIds.Distinct(StringComparer.Ordinal).Count() !=
                        profile.AvailabilityIds.Count ||
                    string.IsNullOrWhiteSpace(profile.PlacementAnchorId) ||
                    string.IsNullOrWhiteSpace(profile.EntranceAnchorId) ||
                    !Finite(profile.EntranceX) || !Finite(profile.EntranceY) ||
                    !Finite(profile.EntranceZ) || profile.EntranceY < 0f ||
                    profile.Modules == null || profile.Modules.Count < 5 ||
                    profile.Modules.Count > 64 ||
                    profile.Lod1ModuleIds == null ||
                    profile.Lod1ModuleIds.Count == 0 ||
                    profile.Lod2ModuleIds == null ||
                    profile.Lod2ModuleIds.Count == 0)
                    throw new InvalidOperationException(
                        "Invalid Luoyang historical landmark profile.");

                var halfFootprint = model.StrategicFootprintRatio * 0.5f;
                if (Math.Abs(profile.EntranceX) > halfFootprint + 0.0001f ||
                    Math.Abs(profile.EntranceZ) > halfFootprint + 0.0001f)
                    throw new InvalidOperationException(
                        "Luoyang landmark entrance exceeds its Cell footprint.");

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
                        module.ScaleX > 1f || module.ScaleY > 1f ||
                        module.ScaleZ > 1f || module.PositionY < 0f ||
                        Math.Abs(module.PositionX) + module.ScaleX * 0.5f >
                            halfFootprint + 0.0001f ||
                        Math.Abs(module.PositionZ) + module.ScaleZ * 0.5f >
                            halfFootprint + 0.0001f)
                        throw new InvalidOperationException(
                            "Invalid Luoyang historical landmark module.");
                }

                var lod1 = ValidateLodModules(profile.Lod1ModuleIds, moduleIds,
                    "LOD1");
                var lod2 = ValidateLodModules(profile.Lod2ModuleIds, moduleIds,
                    "LOD2");
                if (!lod2.IsSubsetOf(lod1))
                    throw new InvalidOperationException(
                        "Luoyang landmark LOD2 modules must be a subset of LOD1.");
            }

            if (!facilityIds.SetEquals(expectedFacilityIds))
                throw new InvalidOperationException(
                    "Luoyang historical landmark Facility coverage is incomplete.");
        }

        private static HashSet<string> ValidateLodModules(IEnumerable<string> values,
            HashSet<string> moduleIds, string level)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
                if (!moduleIds.Contains(value ?? string.Empty) || !result.Add(value))
                    throw new InvalidOperationException(
                        "Invalid Luoyang landmark " + level + " module list.");
            return result;
        }

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
