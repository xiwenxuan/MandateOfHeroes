using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangFinalCivicRitualMedicalProductionKitIds
    {
        public const string SchemaId =
            "mandate.luoyang-final-civic-ritual-medical-production-kit.v1";
        public const string KitId =
            "LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1";
        public const string ProceduralLodProfileId =
            "lod.han.strategy.final_civic_ritual_medical.three_tier.v1";
        public const string MaterialSetId =
            "material_set.han.central_plains.shared.v1";
        public const string IdentityReuseModeId =
            "production.mode.identity_reuse";
        public const string ProceduralModeId =
            "production.mode.procedural";
        public const string HistoricalEvidenceBasisId =
            "evidence.existing_luoyang_landmark_identity.v1";
        public const string GenericEvidenceBasisId =
            "evidence.gameplay_reconstruction.generic_han_civic_form.v1";

        public const int OpeningFacilityCount = 2084;
        public const int PreviouslyProducedFacilityCount = 2049;
        public const int ClosureFacilityCount = 35;
        public const int IdentityReuseFacilityCount = 10;
        public const int ProceduralFacilityCount = 25;
        public const int ProfileCount = 12;
        public const int ProducedOpeningFacilityCount = 2084;
        public const int RemainingFacilityCount = 0;
        public const int MinGridColumn = 2024;
        public const int MaxGridColumn = 2064;
        public const int MinGridRow = 1210;
        public const int MaxGridRow = 1264;

        public const string ClinicDefinition = "facility.service.clinic";
        public const string RitualHallDefinition =
            "facility.public.ritual_hall";
        public const string CourtyardDefinition = "facility.public.courtyard";
        public const string PlazaDefinition = "facility.public.plaza";
        public const string CourtHallDefinition =
            "facility.government.court_hall";
        public const string CentralOfficeDefinition =
            "facility.historical.central_office";
        public const string AcademyDefinition = "facility.education.academy";
        public const string ImperialGardenDefinition =
            "facility.historical.imperial_garden";
        public const string ObservatoryDefinition =
            "facility.public.observatory";
        public const string GranaryDefinition = "facility.storage.granary";
        public const string ArsenalRuntimeDefinition =
            "facility.storage.warehouse";

        public static readonly IReadOnlyList<string> FacilityDefinitionIds =
            new[]
            {
                ClinicDefinition, RitualHallDefinition, CourtyardDefinition,
                PlazaDefinition, CourtHallDefinition, CentralOfficeDefinition,
                AcademyDefinition, ImperialGardenDefinition,
                ObservatoryDefinition, GranaryDefinition,
                ArsenalRuntimeDefinition
            };

        public static readonly IReadOnlyDictionary<string, int>
            OpeningUsageByDefinition =
                new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    [ClinicDefinition] = 9,
                    [RitualHallDefinition] = 8,
                    [CourtyardDefinition] = 4,
                    [PlazaDefinition] = 4,
                    [CourtHallDefinition] = 3,
                    [CentralOfficeDefinition] = 2,
                    [AcademyDefinition] = 1,
                    [ImperialGardenDefinition] = 1,
                    [ObservatoryDefinition] = 1,
                    [GranaryDefinition] = 1,
                    [ArsenalRuntimeDefinition] = 1
                };

        public static readonly IReadOnlyDictionary<string, string>
            ModelByDefinition =
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [ClinicDefinition] = LuoyangFacilityModelCoverageIds.Clinic,
                    [RitualHallDefinition] =
                        LuoyangFacilityModelCoverageIds.RitualHall,
                    [CourtyardDefinition] = LuoyangFacilityModelCoverageIds.Plaza,
                    [PlazaDefinition] = LuoyangFacilityModelCoverageIds.Plaza,
                    [CourtHallDefinition] =
                        LuoyangFacilityModelCoverageIds.PalaceComplex,
                    [CentralOfficeDefinition] =
                        LuoyangFacilityModelCoverageIds.CentralOffice,
                    [AcademyDefinition] =
                        LuoyangFacilityModelCoverageIds.ImperialAcademy,
                    [ImperialGardenDefinition] =
                        LuoyangFacilityModelCoverageIds.ImperialGarden,
                    [ObservatoryDefinition] =
                        LuoyangFacilityModelCoverageIds.Observatory,
                    [GranaryDefinition] =
                        LuoyangFacilityModelCoverageIds.ImperialGranary,
                    [ArsenalRuntimeDefinition] =
                        LuoyangFacilityModelCoverageIds.Arsenal
                };

        public static bool IsIdentityFacility(string facilityId) =>
            LuoyangHistoricalLandmarkKitIds.FacilityIds.Contains(
                facilityId ?? string.Empty, StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class LuoyangFinalCivicRitualMedicalProductionKitCatalog
    {
        public string SchemaId;
        public string KitId;
        public string RegionalStyleId;
        public int OpeningFacilityCount;
        public int PreviouslyProducedFacilityCount;
        public int ClosureFacilityCount;
        public int IdentityReuseFacilityCount;
        public int ProceduralFacilityCount;
        public int ProducedOpeningFacilityCount;
        public List<LuoyangFinalCivicRitualMedicalProductionProfile> Profiles =
            new List<LuoyangFinalCivicRitualMedicalProductionProfile>();
    }

    [Serializable]
    public sealed class LuoyangFinalCivicRitualMedicalProductionProfile
    {
        public string ProfileId;
        public string ModelId;
        public string DisplayName;
        public string FacilityDefinitionId;
        public int OpeningUsageCount;
        public string ProductionModeId;
        public string ReuseKitId;
        public string AssetVariantId;
        public string CivicRoleId;
        public string EvidenceBasisId;
        public string LodProfileId;
        public string MaterialSetId;
        public List<string> AvailabilityIds = new List<string>();
        public string PlacementAnchorId;
        public string EntranceAnchorId;
        public float EntranceX;
        public float EntranceY;
        public float EntranceZ;
        public List<string> FacilityIds = new List<string>();
        public List<HanBuildableFacilityModuleDefinition> Modules =
            new List<HanBuildableFacilityModuleDefinition>();
        public List<string> Lod1ModuleIds = new List<string>();
        public List<string> Lod2ModuleIds = new List<string>();
    }

    [Serializable]
    public sealed class LuoyangFinalCivicRitualMedicalFacility
    {
        public string FacilityId;
        public string FacilityDefinitionId;
        public string ModelId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
        public string ProfileId;
        public string ProductionModeId;
        public string AssetVariantId;
        public string CivicRoleId;
        public string EvidenceBasisId;
    }

    public sealed class LuoyangFinalCivicRitualMedicalProductionPlan
    {
        public LuoyangFinalCivicRitualMedicalProductionPlan(
            LuoyangFinalCivicRitualMedicalProductionKitCatalog catalog,
            IReadOnlyList<LuoyangFinalCivicRitualMedicalFacility> facilities,
            int identityReuseCount, int proceduralCount)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Facilities = facilities ?? throw new ArgumentNullException(
                nameof(facilities));
            IdentityReuseCount = identityReuseCount;
            ProceduralCount = proceduralCount;
        }

        public LuoyangFinalCivicRitualMedicalProductionKitCatalog Catalog
            { get; }
        public IReadOnlyList<LuoyangFinalCivicRitualMedicalFacility> Facilities
            { get; }
        public int IdentityReuseCount { get; }
        public int ProceduralCount { get; }
    }

    public static class LuoyangFinalCivicRitualMedicalProductionKitRules
    {
        private static readonly HashSet<string> AllowedPrimitives =
            new HashSet<string>(new[]
            {
                "cube", "cylinder", "han.rammed_block", "han.terrain_pad",
                "han.wall_coping", "han.road_crown", "han.tile_slab",
                "han.timber_beam", "han.hip_roof"
            }, StringComparer.Ordinal);

        public static void Validate(
            LuoyangFinalCivicRitualMedicalProductionKitCatalog catalog,
            HanBuildableFacilityModelCatalog models,
            LuoyangHistoricalLandmarkKitCatalog landmarks)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            LuoyangHistoricalLandmarkKitRules.Validate(landmarks, models);
            if (!string.Equals(catalog.SchemaId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.KitId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds.KitId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.RegionalStyleId, models.RegionalStyleId,
                    StringComparison.Ordinal) ||
                catalog.OpeningFacilityCount != 2084 ||
                catalog.PreviouslyProducedFacilityCount != 2049 ||
                catalog.ClosureFacilityCount != 35 ||
                catalog.IdentityReuseFacilityCount != 10 ||
                catalog.ProceduralFacilityCount != 25 ||
                catalog.ProducedOpeningFacilityCount != 2084 ||
                catalog.Profiles == null ||
                catalog.Profiles.Count !=
                    LuoyangFinalCivicRitualMedicalProductionKitIds.ProfileCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang final civic production kit header.");

            var modelsById = models.Models.ToDictionary(item => item.ModelId,
                StringComparer.Ordinal);
            var landmarksById = landmarks.Profiles.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            var materialIds = new HashSet<string>(models.Materials.Select(item =>
                item.MaterialId), StringComparer.Ordinal);
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            var assetIds = new HashSet<string>(StringComparer.Ordinal);
            var facilityIds = new HashSet<string>(StringComparer.Ordinal);
            var usage = LuoyangFinalCivicRitualMedicalProductionKitIds
                .FacilityDefinitionIds.ToDictionary(id => id, id => 0,
                    StringComparer.Ordinal);
            var identityCount = 0;
            var proceduralCount = 0;
            foreach (var profile in catalog.Profiles)
            {
                if (profile == null ||
                    string.IsNullOrWhiteSpace(profile.ProfileId) ||
                    !profileIds.Add(profile.ProfileId) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    !usage.ContainsKey(profile.FacilityDefinitionId ?? string.Empty) ||
                    profile.OpeningUsageCount <= 0 ||
                    !string.Equals(profile.ModelId,
                        LuoyangFinalCivicRitualMedicalProductionKitIds
                            .ModelByDefinition[profile.FacilityDefinitionId],
                        StringComparison.Ordinal) ||
                    !modelsById.TryGetValue(profile.ModelId, out var model) ||
                    string.IsNullOrWhiteSpace(profile.AssetVariantId) ||
                    !assetIds.Add(profile.AssetVariantId) ||
                    string.IsNullOrWhiteSpace(profile.CivicRoleId) ||
                    string.IsNullOrWhiteSpace(profile.EvidenceBasisId) ||
                    !string.Equals(profile.MaterialSetId,
                        LuoyangFinalCivicRitualMedicalProductionKitIds.MaterialSetId,
                        StringComparison.Ordinal) ||
                    profile.AvailabilityIds == null ||
                    profile.AvailabilityIds.Count == 0 ||
                    profile.AvailabilityIds.Distinct(StringComparer.Ordinal)
                        .Count() != profile.AvailabilityIds.Count ||
                    string.IsNullOrWhiteSpace(profile.PlacementAnchorId) ||
                    string.IsNullOrWhiteSpace(profile.EntranceAnchorId) ||
                    !Finite(profile.EntranceX) || !Finite(profile.EntranceY) ||
                    !Finite(profile.EntranceZ) || profile.EntranceY < 0f ||
                    profile.FacilityIds == null ||
                    profile.FacilityIds.Count != profile.OpeningUsageCount ||
                    profile.FacilityIds.Distinct(StringComparer.Ordinal).Count() !=
                        profile.FacilityIds.Count)
                    throw new InvalidOperationException(
                        "Invalid Luoyang final civic production profile.");

                var half = model.StrategicFootprintRatio * 0.5f;
                if (Math.Abs(profile.EntranceX) > half + 0.0001f ||
                    Math.Abs(profile.EntranceZ) > half + 0.0001f)
                    throw new InvalidOperationException(
                        "Final civic entrance exceeds its Cell footprint.");
                foreach (var id in profile.FacilityIds)
                    if (string.IsNullOrWhiteSpace(id) || !facilityIds.Add(id))
                        throw new InvalidOperationException(
                            "Duplicate final civic Facility id.");

                var reuse = string.Equals(profile.ProductionModeId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .IdentityReuseModeId, StringComparison.Ordinal);
                var procedural = string.Equals(profile.ProductionModeId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .ProceduralModeId, StringComparison.Ordinal);
                if (reuse == procedural)
                    throw new InvalidOperationException(
                        "Invalid final civic production mode.");
                if (reuse)
                {
                    ValidateIdentityReuse(profile, landmarksById);
                    identityCount += profile.OpeningUsageCount;
                }
                else
                {
                    ValidateProcedural(profile, model, materialIds, half);
                    proceduralCount += profile.OpeningUsageCount;
                }
                usage[profile.FacilityDefinitionId] += profile.OpeningUsageCount;
            }

            if (facilityIds.Count != 35 || identityCount != 10 ||
                proceduralCount != 25 ||
                usage.Any(item => item.Value !=
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .OpeningUsageByDefinition[item.Key]))
                throw new InvalidOperationException(
                    "Luoyang final civic production coverage is incomplete.");
        }

        public static LuoyangFinalCivicRitualMedicalProductionPlan CreatePlan(
            LuoyangFinalCivicRitualMedicalProductionKitCatalog catalog,
            IEnumerable<LuoyangFinalCivicRitualMedicalFacility> source,
            LuoyangHistoricalLandmarkKitCatalog landmarks)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (landmarks == null) throw new ArgumentNullException(nameof(landmarks));
            var values = source.OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal).ToArray();
            if (values.Length != 35)
                throw new InvalidOperationException(
                    "Final civic plan has the wrong Facility count.");

            var profilesByFacility = catalog.Profiles
                .SelectMany(profile => profile.FacilityIds.Select(id =>
                    new { Id = id, Profile = profile }))
                .ToDictionary(item => item.Id, item => item.Profile,
                    StringComparer.Ordinal);
            var landmarksById = landmarks.Profiles.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var cells = new HashSet<ulong>();
            var usage = LuoyangFinalCivicRitualMedicalProductionKitIds
                .FacilityDefinitionIds.ToDictionary(id => id, id => 0,
                    StringComparer.Ordinal);
            var result = new List<LuoyangFinalCivicRitualMedicalFacility>(35);
            var identityCount = 0;
            var proceduralCount = 0;
            foreach (var value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.FacilityId) ||
                    !ids.Add(value.FacilityId) || value.CellId64 == 0 ||
                    !cells.Add(value.CellId64) ||
                    value.GridColumn < 2024 || value.GridColumn > 2064 ||
                    value.GridRow < 1210 || value.GridRow > 1264 ||
                    !LuoyangFinalCivicRitualMedicalProductionKitIds
                        .ModelByDefinition.TryGetValue(
                            value.FacilityDefinitionId ?? string.Empty,
                            out var expectedModel) ||
                    !string.Equals(value.ModelId, expectedModel,
                        StringComparison.Ordinal) ||
                    !profilesByFacility.TryGetValue(value.FacilityId,
                        out var profile) ||
                    !string.Equals(profile.FacilityDefinitionId,
                        value.FacilityDefinitionId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Invalid Luoyang final civic Facility.");

                var reuse = string.Equals(profile.ProductionModeId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .IdentityReuseModeId, StringComparison.Ordinal);
                var assetVariant = profile.AssetVariantId;
                if (reuse)
                {
                    if (!landmarksById.TryGetValue(value.FacilityId,
                            out var landmark) ||
                        !string.Equals(landmark.BaseModelId, value.ModelId,
                            StringComparison.Ordinal) ||
                        landmark.CellId64 != value.CellId64)
                        throw new InvalidOperationException(
                            "Final civic landmark identity reuse is unresolved.");
                    assetVariant = landmark.AssetVariantId;
                    identityCount++;
                }
                else
                    proceduralCount++;

                usage[value.FacilityDefinitionId]++;
                result.Add(new LuoyangFinalCivicRitualMedicalFacility
                {
                    FacilityId = value.FacilityId,
                    FacilityDefinitionId = value.FacilityDefinitionId,
                    ModelId = value.ModelId,
                    CellId64 = value.CellId64,
                    GridColumn = value.GridColumn,
                    GridRow = value.GridRow,
                    ProfileId = profile.ProfileId,
                    ProductionModeId = profile.ProductionModeId,
                    AssetVariantId = assetVariant,
                    CivicRoleId = profile.CivicRoleId,
                    EvidenceBasisId = profile.EvidenceBasisId
                });
            }

            if (identityCount != 10 || proceduralCount != 25 ||
                usage.Any(item => item.Value !=
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .OpeningUsageByDefinition[item.Key]))
                throw new InvalidOperationException(
                    "Final civic opening usage is incomplete.");
            return new LuoyangFinalCivicRitualMedicalProductionPlan(catalog,
                result.ToArray(), identityCount, proceduralCount);
        }

        private static void ValidateIdentityReuse(
            LuoyangFinalCivicRitualMedicalProductionProfile profile,
            IReadOnlyDictionary<string, LuoyangHistoricalLandmarkProfile>
                landmarksById)
        {
            if (!string.Equals(profile.ReuseKitId,
                    LuoyangHistoricalLandmarkKitIds.KitId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.EvidenceBasisId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .HistoricalEvidenceBasisId, StringComparison.Ordinal) ||
                !string.Equals(profile.LodProfileId,
                    LuoyangHistoricalLandmarkKitIds.LodProfileId,
                    StringComparison.Ordinal) ||
                profile.Modules == null || profile.Modules.Count != 0 ||
                profile.Lod1ModuleIds == null ||
                profile.Lod1ModuleIds.Count != 0 ||
                profile.Lod2ModuleIds == null ||
                profile.Lod2ModuleIds.Count != 0)
                throw new InvalidOperationException(
                    "Invalid final civic landmark reuse profile.");

            foreach (var id in profile.FacilityIds)
                if (!landmarksById.TryGetValue(id, out var landmark) ||
                    !string.Equals(landmark.BaseModelId, profile.ModelId,
                        StringComparison.Ordinal) ||
                    !new HashSet<string>(landmark.AvailabilityIds,
                        StringComparer.Ordinal).SetEquals(
                        profile.AvailabilityIds))
                    throw new InvalidOperationException(
                        "Final civic landmark does not match its reuse profile.");
        }

        private static void ValidateProcedural(
            LuoyangFinalCivicRitualMedicalProductionProfile profile,
            HanBuildableFacilityModelDefinition model,
            HashSet<string> materialIds, float half)
        {
            if (!string.IsNullOrEmpty(profile.ReuseKitId) ||
                !string.Equals(profile.EvidenceBasisId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .GenericEvidenceBasisId, StringComparison.Ordinal) ||
                !string.Equals(profile.LodProfileId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .ProceduralLodProfileId, StringComparison.Ordinal) ||
                !new HashSet<string>(profile.AvailabilityIds,
                    StringComparer.Ordinal).SetEquals(model.AvailabilityIds) ||
                profile.Modules == null || profile.Modules.Count < 6 ||
                profile.Modules.Count > 32 || profile.Lod1ModuleIds == null ||
                profile.Lod1ModuleIds.Count == 0 ||
                profile.Lod2ModuleIds == null ||
                profile.Lod2ModuleIds.Count == 0)
                throw new InvalidOperationException(
                    "Invalid final civic procedural profile.");

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
                        half + 0.0001f ||
                    Math.Abs(module.PositionZ) + module.ScaleZ * 0.5f >
                        half + 0.0001f)
                    throw new InvalidOperationException(
                        "Invalid final civic procedural module.");
            }
            var lod1 = ValidateLod(profile.Lod1ModuleIds, moduleIds, "LOD1");
            var lod2 = ValidateLod(profile.Lod2ModuleIds, moduleIds, "LOD2");
            if (!lod2.IsSubsetOf(lod1))
                throw new InvalidOperationException(
                    "Final civic LOD2 must be a subset of LOD1.");
        }

        private static HashSet<string> ValidateLod(
            IReadOnlyCollection<string> values, HashSet<string> modules,
            string level)
        {
            var result = new HashSet<string>(values ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            if (values == null || result.Count == 0 ||
                result.Count != values.Count ||
                result.Any(item => !modules.Contains(item)))
                throw new InvalidOperationException(
                    "Invalid final civic " + level + " module list.");
            return result;
        }

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
