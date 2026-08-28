using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangLowFrequencyDefenseProductionKitIds
    {
        public const string SchemaId =
            "mandate.luoyang-low-frequency-defense-production-kit.v1";
        public const string KitId =
            "LUOYANG_LOW_FREQUENCY_DEFENSE_PRODUCTION_V1";
        public const string ProceduralLodProfileId =
            "lod.han.strategy.defense.three_tier.v1";
        public const string MaterialSetId =
            "material_set.han.central_plains.shared.v1";
        public const string IdentityReuseModeId =
            "defense.production.reuse_gate_identity";
        public const string ProceduralModeId =
            "defense.production.procedural";
        public const string GenericGateDefaultFacingPolicyId =
            "presentation.default_south.unoriented_facility";
        public const string NonDirectionalFacingPolicyId =
            "defense.facing.non_directional";

        public const int OpeningFacilityCount = 2084;
        public const int PreviouslyProducedFacilityCount = 1995;
        public const int DefenseFacilityCount = 28;
        public const int IdentityReuseFacilityCount = 14;
        public const int ProceduralFacilityCount = 14;
        public const int ProducedOpeningFacilityCount = 2023;
        public const int RemainingFacilityCount = 61;
        public const int MinGridColumn = 2025;
        public const int MaxGridColumn = 2065;
        public const int MinGridRow = 1216;
        public const int MaxGridRow = 1250;

        public const string CityGateDefinition =
            "facility.fortification.city_gate";
        public const string PalaceGateDefinition =
            "facility.fortification.palace_gate";
        public const string MilitaryGateDefinition = "facility.military.gate";
        public const string FortifiedManorDefinition =
            "facility.military.fortified_manor";
        public const string BeaconDefinition = "facility.military.beacon";

        public static readonly IReadOnlyList<string> FacilityDefinitionIds =
            new[]
            {
                CityGateDefinition, PalaceGateDefinition,
                MilitaryGateDefinition, FortifiedManorDefinition,
                BeaconDefinition
            };

        public static readonly IReadOnlyDictionary<string, int>
            OpeningUsageByDefinition =
                new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    [CityGateDefinition] = 12,
                    [PalaceGateDefinition] = 2,
                    [MilitaryGateDefinition] = 4,
                    [FortifiedManorDefinition] = 7,
                    [BeaconDefinition] = 3
                };

        public static readonly IReadOnlyDictionary<string, string>
            ModelByDefinition =
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [CityGateDefinition] = HanBuildableFacilityModelIds.CityGate,
                    [PalaceGateDefinition] =
                        LuoyangFacilityModelCoverageIds.PalaceGate,
                    [MilitaryGateDefinition] =
                        HanBuildableFacilityModelIds.CityGate,
                    [FortifiedManorDefinition] =
                        LuoyangFacilityModelCoverageIds.FortifiedManor,
                    [BeaconDefinition] = LuoyangFacilityModelCoverageIds.Beacon
                };

        public static bool IsIdentityReuseDefinition(string definitionId) =>
            string.Equals(definitionId, CityGateDefinition,
                StringComparison.Ordinal) ||
            string.Equals(definitionId, PalaceGateDefinition,
                StringComparison.Ordinal);
    }

    [Serializable]
    public sealed class LuoyangLowFrequencyDefenseProductionKitCatalog
    {
        public string SchemaId;
        public string KitId;
        public string RegionalStyleId;
        public int OpeningFacilityCount;
        public int PreviouslyProducedFacilityCount;
        public int DefenseFacilityCount;
        public int ProducedOpeningFacilityCount;
        public List<LuoyangLowFrequencyDefenseProductionProfile> Profiles =
            new List<LuoyangLowFrequencyDefenseProductionProfile>();
    }

    [Serializable]
    public sealed class LuoyangLowFrequencyDefenseProductionProfile
    {
        public string ProfileId;
        public string ModelId;
        public string DisplayName;
        public string FacilityDefinitionId;
        public int OpeningUsageCount;
        public string ProductionModeId;
        public string ReuseKitId;
        public string AssetVariantId;
        public string DefenseRoleId;
        public string FacingPolicyId;
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
    public sealed class LuoyangLowFrequencyDefenseFacility
    {
        public string FacilityId;
        public string FacilityDefinitionId;
        public string ModelId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
        public string ProfileId;
        public string ProductionModeId;
        public string DefenseRoleId;
        public string FacingPolicyId;
        public string VisualFacing;
        public string DirectionBasisId;
        public float RotationDegrees;
    }

    public sealed class LuoyangLowFrequencyDefenseProductionPlan
    {
        public LuoyangLowFrequencyDefenseProductionPlan(
            LuoyangLowFrequencyDefenseProductionKitCatalog catalog,
            IReadOnlyList<LuoyangLowFrequencyDefenseFacility> facilities,
            int identityReuseCount, int proceduralCount)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Facilities = facilities ?? throw new ArgumentNullException(
                nameof(facilities));
            IdentityReuseCount = identityReuseCount;
            ProceduralCount = proceduralCount;
        }

        public LuoyangLowFrequencyDefenseProductionKitCatalog Catalog { get; }
        public IReadOnlyList<LuoyangLowFrequencyDefenseFacility> Facilities
            { get; }
        public int IdentityReuseCount { get; }
        public int ProceduralCount { get; }
    }

    public static class LuoyangLowFrequencyDefenseProductionKitRules
    {
        private static readonly HashSet<string> AllowedPrimitives =
            new HashSet<string>(new[]
            {
                "cube", "cylinder", "han.rammed_block", "han.terrain_pad",
                "han.wall_coping", "han.road_crown", "han.tile_slab",
                "han.timber_beam", "han.hip_roof"
            }, StringComparer.Ordinal);

        public static void Validate(
            LuoyangLowFrequencyDefenseProductionKitCatalog defense,
            HanBuildableFacilityModelCatalog models,
            LuoyangGateIdentityKitCatalog gates)
        {
            if (defense == null) throw new ArgumentNullException(nameof(defense));
            if (gates == null) throw new ArgumentNullException(nameof(gates));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            LuoyangGateIdentityKitRules.Validate(gates, models);
            if (!string.Equals(defense.SchemaId,
                    LuoyangLowFrequencyDefenseProductionKitIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(defense.KitId,
                    LuoyangLowFrequencyDefenseProductionKitIds.KitId,
                    StringComparison.Ordinal) ||
                !string.Equals(defense.RegionalStyleId, models.RegionalStyleId,
                    StringComparison.Ordinal) ||
                defense.OpeningFacilityCount !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .OpeningFacilityCount ||
                defense.PreviouslyProducedFacilityCount !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .PreviouslyProducedFacilityCount ||
                defense.DefenseFacilityCount !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .DefenseFacilityCount ||
                defense.ProducedOpeningFacilityCount !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .ProducedOpeningFacilityCount ||
                defense.Profiles == null ||
                defense.Profiles.Count !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .FacilityDefinitionIds.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang low-frequency defense kit header.");

            var modelsById = models.Models.ToDictionary(item => item.ModelId,
                StringComparer.Ordinal);
            var materialIds = new HashSet<string>(models.Materials.Select(item =>
                item.MaterialId), StringComparer.Ordinal);
            var gatesById = gates.Profiles.ToDictionary(item => item.FacilityId,
                StringComparer.Ordinal);
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            var assetVariantIds = new HashSet<string>(StringComparer.Ordinal);
            var definitions = new HashSet<string>(StringComparer.Ordinal);
            var facilityIds = new HashSet<string>(StringComparer.Ordinal);
            var identityCount = 0;
            var proceduralCount = 0;
            var usage = 0;
            foreach (var profile in defense.Profiles)
            {
                if (profile == null ||
                    string.IsNullOrWhiteSpace(profile.ProfileId) ||
                    !profileIds.Add(profile.ProfileId) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    !LuoyangLowFrequencyDefenseProductionKitIds
                        .OpeningUsageByDefinition.TryGetValue(
                            profile.FacilityDefinitionId ?? string.Empty,
                            out var expectedUsage) ||
                    !definitions.Add(profile.FacilityDefinitionId) ||
                    profile.OpeningUsageCount != expectedUsage ||
                    !string.Equals(profile.ModelId,
                        LuoyangLowFrequencyDefenseProductionKitIds
                            .ModelByDefinition[profile.FacilityDefinitionId],
                        StringComparison.Ordinal) ||
                    !modelsById.TryGetValue(profile.ModelId, out var model) ||
                    string.IsNullOrWhiteSpace(profile.AssetVariantId) ||
                    !assetVariantIds.Add(profile.AssetVariantId) ||
                    string.IsNullOrWhiteSpace(profile.DefenseRoleId) ||
                    string.IsNullOrWhiteSpace(profile.FacingPolicyId) ||
                    !string.Equals(profile.MaterialSetId,
                        LuoyangLowFrequencyDefenseProductionKitIds.MaterialSetId,
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
                    profile.FacilityIds.Count != expectedUsage ||
                    profile.FacilityIds.Distinct(StringComparer.Ordinal).Count() !=
                    profile.FacilityIds.Count)
                    throw new InvalidOperationException(
                        "Invalid Luoyang low-frequency defense profile.");

                var halfFootprint = model.StrategicFootprintRatio * 0.5f;
                if (Math.Abs(profile.EntranceX) > halfFootprint + 0.0001f ||
                    Math.Abs(profile.EntranceZ) > halfFootprint + 0.0001f)
                    throw new InvalidOperationException(
                        "Luoyang defense entrance exceeds its Cell footprint.");
                foreach (var facilityId in profile.FacilityIds)
                    if (string.IsNullOrWhiteSpace(facilityId) ||
                        !facilityIds.Add(facilityId))
                        throw new InvalidOperationException(
                            "Duplicate Luoyang defense Facility id.");

                var reuse = string.Equals(profile.ProductionModeId,
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .IdentityReuseModeId, StringComparison.Ordinal);
                var procedural = string.Equals(profile.ProductionModeId,
                    LuoyangLowFrequencyDefenseProductionKitIds.ProceduralModeId,
                    StringComparison.Ordinal);
                if (reuse == procedural ||
                    reuse != LuoyangLowFrequencyDefenseProductionKitIds
                        .IsIdentityReuseDefinition(profile.FacilityDefinitionId))
                    throw new InvalidOperationException(
                        "Invalid Luoyang defense production mode.");

                if (reuse)
                {
                    ValidateIdentityReuse(profile, gatesById);
                    identityCount += profile.OpeningUsageCount;
                }
                else
                {
                    ValidateProcedural(profile, model, materialIds,
                        halfFootprint);
                    proceduralCount += profile.OpeningUsageCount;
                }
                usage += profile.OpeningUsageCount;
            }

            if (!definitions.SetEquals(
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .FacilityDefinitionIds) ||
                facilityIds.Count !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .DefenseFacilityCount ||
                identityCount !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .IdentityReuseFacilityCount ||
                proceduralCount !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .ProceduralFacilityCount ||
                usage != LuoyangLowFrequencyDefenseProductionKitIds
                    .DefenseFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang low-frequency defense coverage is incomplete.");
        }

        public static LuoyangLowFrequencyDefenseProductionPlan CreatePlan(
            LuoyangLowFrequencyDefenseProductionKitCatalog catalog,
            IEnumerable<LuoyangLowFrequencyDefenseFacility> source,
            LuoyangGateIdentityKitCatalog gates)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (gates == null) throw new ArgumentNullException(nameof(gates));
            var values = source.OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal).ToArray();
            if (values.Length !=
                LuoyangLowFrequencyDefenseProductionKitIds.DefenseFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang defense plan has the wrong Facility count.");

            var profilesByFacilityId = catalog.Profiles
                .SelectMany(profile => profile.FacilityIds.Select(id =>
                    new { Id = id, Profile = profile }))
                .ToDictionary(item => item.Id, item => item.Profile,
                    StringComparer.Ordinal);
            var gatesById = gates.Profiles.ToDictionary(item => item.FacilityId,
                StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var cells = new HashSet<ulong>();
            var usage = LuoyangLowFrequencyDefenseProductionKitIds
                .FacilityDefinitionIds.ToDictionary(id => id, id => 0,
                    StringComparer.Ordinal);
            var result = new List<LuoyangLowFrequencyDefenseFacility>(
                values.Length);
            var identityCount = 0;
            var proceduralCount = 0;
            foreach (var value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.FacilityId) ||
                    !ids.Add(value.FacilityId) || value.CellId64 == 0 ||
                    !cells.Add(value.CellId64) ||
                    value.GridColumn <
                        LuoyangLowFrequencyDefenseProductionKitIds.MinGridColumn ||
                    value.GridColumn >
                        LuoyangLowFrequencyDefenseProductionKitIds.MaxGridColumn ||
                    value.GridRow <
                        LuoyangLowFrequencyDefenseProductionKitIds.MinGridRow ||
                    value.GridRow >
                        LuoyangLowFrequencyDefenseProductionKitIds.MaxGridRow ||
                    !LuoyangLowFrequencyDefenseProductionKitIds
                        .ModelByDefinition.TryGetValue(
                            value.FacilityDefinitionId ?? string.Empty,
                            out var expectedModel) ||
                    !string.Equals(value.ModelId, expectedModel,
                        StringComparison.Ordinal) ||
                    !profilesByFacilityId.TryGetValue(value.FacilityId,
                        out var profile) ||
                    !string.Equals(profile.FacilityDefinitionId,
                        value.FacilityDefinitionId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Invalid Luoyang defense Facility.");

                usage[value.FacilityDefinitionId]++;
                var reuse = string.Equals(profile.ProductionModeId,
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .IdentityReuseModeId, StringComparison.Ordinal);
                var visualFacing = string.Empty;
                var directionBasis = profile.FacingPolicyId;
                var rotation = 0f;
                if (reuse)
                {
                    if (!gatesById.TryGetValue(value.FacilityId, out var gate) ||
                        !string.Equals(gate.BaseModelId, value.ModelId,
                            StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "Luoyang defense identity reuse is unresolved.");
                    visualFacing = gate.VisualFacing;
                    directionBasis = gate.DirectionBasisId;
                    rotation = LuoyangGateIdentityKitIds.RotationForFacing(
                        gate.VisualFacing);
                    identityCount++;
                }
                else
                {
                    if (string.Equals(value.FacilityDefinitionId,
                            LuoyangLowFrequencyDefenseProductionKitIds
                                .MilitaryGateDefinition,
                            StringComparison.Ordinal))
                        visualFacing = "south";
                    proceduralCount++;
                }

                result.Add(new LuoyangLowFrequencyDefenseFacility
                {
                    FacilityId = value.FacilityId,
                    FacilityDefinitionId = value.FacilityDefinitionId,
                    ModelId = value.ModelId,
                    CellId64 = value.CellId64,
                    GridColumn = value.GridColumn,
                    GridRow = value.GridRow,
                    ProfileId = profile.ProfileId,
                    ProductionModeId = profile.ProductionModeId,
                    DefenseRoleId = profile.DefenseRoleId,
                    FacingPolicyId = profile.FacingPolicyId,
                    VisualFacing = visualFacing,
                    DirectionBasisId = directionBasis,
                    RotationDegrees = rotation
                });
            }

            if (usage.Any(item => item.Value !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .OpeningUsageByDefinition[item.Key]) ||
                identityCount !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .IdentityReuseFacilityCount ||
                proceduralCount !=
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .ProceduralFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang defense opening usage is incomplete.");
            return new LuoyangLowFrequencyDefenseProductionPlan(catalog,
                result.ToArray(), identityCount, proceduralCount);
        }

        private static void ValidateIdentityReuse(
            LuoyangLowFrequencyDefenseProductionProfile profile,
            IReadOnlyDictionary<string, LuoyangGateIdentityProfile> gatesById)
        {
            if (!string.Equals(profile.ReuseKitId,
                    LuoyangGateIdentityKitIds.KitId,
                    StringComparison.Ordinal) ||
                !string.Equals(profile.LodProfileId,
                    LuoyangGateIdentityKitIds.LodProfileId,
                    StringComparison.Ordinal) ||
                profile.Modules == null || profile.Modules.Count != 0 ||
                profile.Lod1ModuleIds == null ||
                profile.Lod1ModuleIds.Count != 0 ||
                profile.Lod2ModuleIds == null ||
                profile.Lod2ModuleIds.Count != 0)
                throw new InvalidOperationException(
                    "Invalid Luoyang defense gate-identity reuse profile.");

            foreach (var facilityId in profile.FacilityIds)
            {
                if (!gatesById.TryGetValue(facilityId, out var gate) ||
                    !string.Equals(gate.BaseModelId, profile.ModelId,
                        StringComparison.Ordinal) ||
                    !new HashSet<string>(gate.AvailabilityIds,
                        StringComparer.Ordinal).SetEquals(profile.AvailabilityIds))
                    throw new InvalidOperationException(
                        "Luoyang defense gate identity does not match its reuse profile.");
            }
        }

        private static void ValidateProcedural(
            LuoyangLowFrequencyDefenseProductionProfile profile,
            HanBuildableFacilityModelDefinition model,
            HashSet<string> materialIds, float halfFootprint)
        {
            if (!string.IsNullOrEmpty(profile.ReuseKitId) ||
                !string.Equals(profile.LodProfileId,
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .ProceduralLodProfileId, StringComparison.Ordinal) ||
                !new HashSet<string>(profile.AvailabilityIds,
                    StringComparer.Ordinal).SetEquals(model.AvailabilityIds) ||
                profile.Modules == null || profile.Modules.Count < 6 ||
                profile.Modules.Count > 32 || profile.Lod1ModuleIds == null ||
                profile.Lod1ModuleIds.Count == 0 ||
                profile.Lod2ModuleIds == null ||
                profile.Lod2ModuleIds.Count == 0)
                throw new InvalidOperationException(
                    "Invalid Luoyang procedural defense profile.");

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
                        "Invalid Luoyang defense module.");
            }

            var lod1 = ValidateLod(profile.Lod1ModuleIds, moduleIds, "LOD1");
            var lod2 = ValidateLod(profile.Lod2ModuleIds, moduleIds, "LOD2");
            if (!lod2.IsSubsetOf(lod1))
                throw new InvalidOperationException(
                    "Luoyang defense LOD2 must be a subset of LOD1.");
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
                    "Invalid Luoyang defense " + level + " module list.");
            return result;
        }

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
