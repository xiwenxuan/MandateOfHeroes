using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangResourceAgricultureProductionKitIds
    {
        public const string SchemaId =
            "mandate.luoyang-resource-agriculture-production-kit.v1";
        public const string KitId =
            "LUOYANG_RESOURCE_AGRICULTURE_PRODUCTION_V1";
        public const string LodProfileId =
            "lod.han.strategy.resource_agriculture.three_tier.v1";
        public const string MaterialSetId =
            "material_set.han.central_plains.shared.v1";
        public const string EvidenceBasisId =
            "evidence.gameplay_reconstruction.generic_han_production_form.v1";

        public const int OpeningFacilityCount = 2084;
        public const int PreviouslyProducedFacilityCount = 2023;
        public const int ResourceAgricultureFacilityCount = 26;
        public const int ProducedOpeningFacilityCount = 2049;
        public const int RemainingFacilityCount = 35;
        public const int MinGridColumn = 2030;
        public const int MaxGridColumn = 2060;
        public const int MinGridRow = 1228;
        public const int MaxGridRow = 1256;

        public const string ForestryDefinition = "facility.resource.forestry";
        public const string QuarryDefinition = "facility.resource.quarry";
        public const string MineDefinition = "facility.resource.mine";
        public const string RiceFieldDefinition =
            "facility.agriculture.rice_field";

        public static readonly IReadOnlyList<string> FacilityDefinitionIds =
            new[]
            {
                ForestryDefinition, QuarryDefinition, MineDefinition,
                RiceFieldDefinition
            };

        public static readonly IReadOnlyDictionary<string, int>
            OpeningUsageByDefinition =
                new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    [ForestryDefinition] = 9,
                    [QuarryDefinition] = 6,
                    [MineDefinition] = 5,
                    [RiceFieldDefinition] = 6
                };

        public static readonly IReadOnlyDictionary<string, string>
            ModelByDefinition =
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [ForestryDefinition] =
                        LuoyangFacilityModelCoverageIds.Forestry,
                    [QuarryDefinition] =
                        LuoyangFacilityModelCoverageIds.MineQuarry,
                    [MineDefinition] =
                        LuoyangFacilityModelCoverageIds.MineQuarry,
                    [RiceFieldDefinition] =
                        LuoyangFacilityModelCoverageIds.RiceField
                };
    }

    [Serializable]
    public sealed class LuoyangResourceAgricultureProductionKitCatalog
    {
        public string SchemaId;
        public string KitId;
        public string RegionalStyleId;
        public int OpeningFacilityCount;
        public int PreviouslyProducedFacilityCount;
        public int ResourceAgricultureFacilityCount;
        public int ProducedOpeningFacilityCount;
        public List<LuoyangResourceAgricultureProductionProfile> Profiles =
            new List<LuoyangResourceAgricultureProductionProfile>();
    }

    [Serializable]
    public sealed class LuoyangResourceAgricultureProductionProfile
    {
        public string ProfileId;
        public string ModelId;
        public string DisplayName;
        public string FacilityDefinitionId;
        public int OpeningUsageCount;
        public string AssetVariantId;
        public string ProductionRoleId;
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
    public sealed class LuoyangResourceAgricultureFacility
    {
        public string FacilityId;
        public string FacilityDefinitionId;
        public string ModelId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
        public string ProfileId;
        public string AssetVariantId;
        public string ProductionRoleId;
        public string EvidenceBasisId;
    }

    public sealed class LuoyangResourceAgricultureProductionPlan
    {
        public LuoyangResourceAgricultureProductionPlan(
            LuoyangResourceAgricultureProductionKitCatalog catalog,
            IReadOnlyList<LuoyangResourceAgricultureFacility> facilities)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Facilities = facilities ?? throw new ArgumentNullException(
                nameof(facilities));
        }

        public LuoyangResourceAgricultureProductionKitCatalog Catalog { get; }
        public IReadOnlyList<LuoyangResourceAgricultureFacility> Facilities
            { get; }
    }

    public static class LuoyangResourceAgricultureProductionKitRules
    {
        private static readonly HashSet<string> AllowedPrimitives =
            new HashSet<string>(new[]
            {
                "cube", "cylinder", "han.rammed_block", "han.terrain_pad",
                "han.wall_coping", "han.road_crown", "han.tile_slab",
                "han.timber_beam", "han.hip_roof"
            }, StringComparer.Ordinal);

        public static void Validate(
            LuoyangResourceAgricultureProductionKitCatalog catalog,
            HanBuildableFacilityModelCatalog models)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            if (!string.Equals(catalog.SchemaId,
                    LuoyangResourceAgricultureProductionKitIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.KitId,
                    LuoyangResourceAgricultureProductionKitIds.KitId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.RegionalStyleId, models.RegionalStyleId,
                    StringComparison.Ordinal) ||
                catalog.OpeningFacilityCount !=
                    LuoyangResourceAgricultureProductionKitIds
                        .OpeningFacilityCount ||
                catalog.PreviouslyProducedFacilityCount !=
                    LuoyangResourceAgricultureProductionKitIds
                        .PreviouslyProducedFacilityCount ||
                catalog.ResourceAgricultureFacilityCount !=
                    LuoyangResourceAgricultureProductionKitIds
                        .ResourceAgricultureFacilityCount ||
                catalog.ProducedOpeningFacilityCount !=
                    LuoyangResourceAgricultureProductionKitIds
                        .ProducedOpeningFacilityCount ||
                catalog.Profiles == null ||
                catalog.Profiles.Count !=
                    LuoyangResourceAgricultureProductionKitIds
                        .FacilityDefinitionIds.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang resource/agriculture kit header.");

            var modelsById = models.Models.ToDictionary(item => item.ModelId,
                StringComparer.Ordinal);
            var materialIds = new HashSet<string>(models.Materials.Select(item =>
                item.MaterialId), StringComparer.Ordinal);
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            var assetIds = new HashSet<string>(StringComparer.Ordinal);
            var definitions = new HashSet<string>(StringComparer.Ordinal);
            var facilityIds = new HashSet<string>(StringComparer.Ordinal);
            var usage = 0;
            foreach (var profile in catalog.Profiles)
            {
                if (profile == null ||
                    string.IsNullOrWhiteSpace(profile.ProfileId) ||
                    !profileIds.Add(profile.ProfileId) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    !LuoyangResourceAgricultureProductionKitIds
                        .OpeningUsageByDefinition.TryGetValue(
                            profile.FacilityDefinitionId ?? string.Empty,
                            out var expectedUsage) ||
                    !definitions.Add(profile.FacilityDefinitionId) ||
                    profile.OpeningUsageCount != expectedUsage ||
                    !string.Equals(profile.ModelId,
                        LuoyangResourceAgricultureProductionKitIds
                            .ModelByDefinition[profile.FacilityDefinitionId],
                        StringComparison.Ordinal) ||
                    !modelsById.TryGetValue(profile.ModelId, out var model) ||
                    string.IsNullOrWhiteSpace(profile.AssetVariantId) ||
                    !assetIds.Add(profile.AssetVariantId) ||
                    string.IsNullOrWhiteSpace(profile.ProductionRoleId) ||
                    !string.Equals(profile.EvidenceBasisId,
                        LuoyangResourceAgricultureProductionKitIds
                            .EvidenceBasisId, StringComparison.Ordinal) ||
                    !string.Equals(profile.LodProfileId,
                        LuoyangResourceAgricultureProductionKitIds.LodProfileId,
                        StringComparison.Ordinal) ||
                    !string.Equals(profile.MaterialSetId,
                        LuoyangResourceAgricultureProductionKitIds.MaterialSetId,
                        StringComparison.Ordinal) ||
                    profile.AvailabilityIds == null ||
                    !new HashSet<string>(profile.AvailabilityIds,
                        StringComparer.Ordinal).SetEquals(model.AvailabilityIds) ||
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
                        "Invalid Luoyang resource/agriculture profile.");

                var half = model.StrategicFootprintRatio * 0.5f;
                if (Math.Abs(profile.EntranceX) > half + 0.0001f ||
                    Math.Abs(profile.EntranceZ) > half + 0.0001f)
                    throw new InvalidOperationException(
                        "Resource/agriculture entrance exceeds its Cell footprint.");
                foreach (var id in profile.FacilityIds)
                    if (string.IsNullOrWhiteSpace(id) || !facilityIds.Add(id))
                        throw new InvalidOperationException(
                            "Duplicate resource/agriculture Facility id.");
                ValidateModules(profile, materialIds, half);
                usage += expectedUsage;
            }

            if (!definitions.SetEquals(
                    LuoyangResourceAgricultureProductionKitIds
                        .FacilityDefinitionIds) ||
                facilityIds.Count !=
                    LuoyangResourceAgricultureProductionKitIds
                        .ResourceAgricultureFacilityCount ||
                usage != LuoyangResourceAgricultureProductionKitIds
                    .ResourceAgricultureFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang resource/agriculture coverage is incomplete.");
        }

        public static LuoyangResourceAgricultureProductionPlan CreatePlan(
            LuoyangResourceAgricultureProductionKitCatalog catalog,
            IEnumerable<LuoyangResourceAgricultureFacility> source)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = source.OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal).ToArray();
            if (values.Length !=
                LuoyangResourceAgricultureProductionKitIds
                    .ResourceAgricultureFacilityCount)
                throw new InvalidOperationException(
                    "Resource/agriculture plan has the wrong Facility count.");

            var profilesByFacility = catalog.Profiles
                .SelectMany(profile => profile.FacilityIds.Select(id =>
                    new { Id = id, Profile = profile }))
                .ToDictionary(item => item.Id, item => item.Profile,
                    StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var cells = new HashSet<ulong>();
            var usage = LuoyangResourceAgricultureProductionKitIds
                .FacilityDefinitionIds.ToDictionary(id => id, id => 0,
                    StringComparer.Ordinal);
            var result = new List<LuoyangResourceAgricultureFacility>(
                values.Length);
            foreach (var value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.FacilityId) ||
                    !ids.Add(value.FacilityId) || value.CellId64 == 0 ||
                    !cells.Add(value.CellId64) ||
                    value.GridColumn <
                        LuoyangResourceAgricultureProductionKitIds.MinGridColumn ||
                    value.GridColumn >
                        LuoyangResourceAgricultureProductionKitIds.MaxGridColumn ||
                    value.GridRow <
                        LuoyangResourceAgricultureProductionKitIds.MinGridRow ||
                    value.GridRow >
                        LuoyangResourceAgricultureProductionKitIds.MaxGridRow ||
                    !LuoyangResourceAgricultureProductionKitIds
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
                        "Invalid Luoyang resource/agriculture Facility.");

                usage[value.FacilityDefinitionId]++;
                result.Add(new LuoyangResourceAgricultureFacility
                {
                    FacilityId = value.FacilityId,
                    FacilityDefinitionId = value.FacilityDefinitionId,
                    ModelId = value.ModelId,
                    CellId64 = value.CellId64,
                    GridColumn = value.GridColumn,
                    GridRow = value.GridRow,
                    ProfileId = profile.ProfileId,
                    AssetVariantId = profile.AssetVariantId,
                    ProductionRoleId = profile.ProductionRoleId,
                    EvidenceBasisId = profile.EvidenceBasisId
                });
            }

            if (usage.Any(item => item.Value !=
                    LuoyangResourceAgricultureProductionKitIds
                        .OpeningUsageByDefinition[item.Key]))
                throw new InvalidOperationException(
                    "Resource/agriculture opening usage is incomplete.");
            return new LuoyangResourceAgricultureProductionPlan(catalog,
                result.ToArray());
        }

        private static void ValidateModules(
            LuoyangResourceAgricultureProductionProfile profile,
            HashSet<string> materialIds, float half)
        {
            if (profile.Modules == null || profile.Modules.Count < 6 ||
                profile.Modules.Count > 32 || profile.Lod1ModuleIds == null ||
                profile.Lod1ModuleIds.Count == 0 ||
                profile.Lod2ModuleIds == null ||
                profile.Lod2ModuleIds.Count == 0)
                throw new InvalidOperationException(
                    "Invalid resource/agriculture module family.");
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
                        "Invalid resource/agriculture module.");
            }
            var lod1 = ValidateLod(profile.Lod1ModuleIds, moduleIds, "LOD1");
            var lod2 = ValidateLod(profile.Lod2ModuleIds, moduleIds, "LOD2");
            if (!lod2.IsSubsetOf(lod1))
                throw new InvalidOperationException(
                    "Resource/agriculture LOD2 must be a subset of LOD1.");
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
                    "Invalid resource/agriculture " + level + " module list.");
            return result;
        }

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
