using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangMediumFrequencyUrbanFabricKitIds
    {
        public const string SchemaId =
            "mandate.luoyang-medium-frequency-urban-fabric-kit.v1";
        public const string KitId =
            "LUOYANG_MEDIUM_FREQUENCY_URBAN_FABRIC_V1";
        public const string LodProfileId =
            "lod.han.strategy.urban_fabric.three_tier.v1";
        public const string MaterialSetId =
            "material_set.han.central_plains.shared.v1";
        public const int OpeningFacilityCount = 2084;
        public const int HighFrequencyProducedFacilityCount = 1800;
        public const int MediumFrequencyFacilityCount = 158;
        public const int ProducedOpeningFacilityCount = 1958;

        public const string Market = HanBuildableFacilityModelIds.Market;
        public const string CaravanYard =
            "model.han.luoyang.service.caravan_yard.v1";
        public const string School =
            "model.han.luoyang.education.school.v1";
        public const string LocalOffice =
            "model.han.luoyang.government.local_office.v1";
        public const string MilitaryCamp =
            "model.han.luoyang.military.camp.v1";

        public static readonly IReadOnlyList<string> ModelIds = new[]
        {
            Market, CaravanYard, School, LocalOffice, MilitaryCamp
        };

        public static readonly IReadOnlyDictionary<string, int> OpeningUsageCounts =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [Market] = 48,
                [CaravanYard] = 45,
                [School] = 39,
                [LocalOffice] = 16,
                [MilitaryCamp] = 10
            };

        public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>>
            FacilityDefinitionIds =
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
                {
                    [Market] = new[]
                    {
                        "facility.commercial.market",
                        "facility.commercial.shop_cluster"
                    },
                    [CaravanYard] = new[] { "facility.service.caravan_yard" },
                    [School] = new[] { "facility.service.school" },
                    [LocalOffice] = new[]
                    {
                        "facility.government.local_office",
                        "facility.public.county_office"
                    },
                    [MilitaryCamp] = new[]
                    {
                        "facility.military.barracks",
                        "facility.military.camp"
                    }
                };
    }

    [Serializable]
    public sealed class LuoyangMediumFrequencyUrbanFabricKitCatalog
    {
        public string SchemaId;
        public string KitId;
        public string RegionalStyleId;
        public int OpeningFacilityCount;
        public int MediumFrequencyFacilityCount;
        public int ProducedOpeningFacilityCount;
        public List<LuoyangMediumFrequencyUrbanFabricProfile> Profiles =
            new List<LuoyangMediumFrequencyUrbanFabricProfile>();
    }

    [Serializable]
    public sealed class LuoyangMediumFrequencyUrbanFabricProfile
    {
        public string ProfileId;
        public string ModelId;
        public string DisplayName;
        public List<string> FacilityDefinitionIds = new List<string>();
        public int OpeningUsageCount;
        public string AssetVariantId;
        public string FabricRoleId;
        public string DensityClassId;
        public string StreetInterfaceId;
        public string LodProfileId;
        public string MaterialSetId;
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

    public static class LuoyangMediumFrequencyUrbanFabricKitRules
    {
        private static readonly HashSet<string> AllowedPrimitives =
            new HashSet<string>(new[]
            {
                "cube", "cylinder", "han.rammed_block", "han.tile_slab",
                "han.terrain_pad", "han.foliage_cluster", "han.wall_coping",
                "han.timber_beam", "han.hip_roof"
            }, StringComparer.Ordinal);

        public static void Validate(
            LuoyangMediumFrequencyUrbanFabricKitCatalog fabric,
            HanBuildableFacilityModelCatalog models)
        {
            if (fabric == null) throw new ArgumentNullException(nameof(fabric));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            if (!string.Equals(fabric.SchemaId,
                    LuoyangMediumFrequencyUrbanFabricKitIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(fabric.KitId,
                    LuoyangMediumFrequencyUrbanFabricKitIds.KitId,
                    StringComparison.Ordinal) ||
                !string.Equals(fabric.RegionalStyleId, models.RegionalStyleId,
                    StringComparison.Ordinal) ||
                fabric.OpeningFacilityCount !=
                    LuoyangMediumFrequencyUrbanFabricKitIds.OpeningFacilityCount ||
                fabric.MediumFrequencyFacilityCount !=
                    LuoyangMediumFrequencyUrbanFabricKitIds
                        .MediumFrequencyFacilityCount ||
                fabric.ProducedOpeningFacilityCount !=
                    LuoyangMediumFrequencyUrbanFabricKitIds
                        .ProducedOpeningFacilityCount ||
                fabric.Profiles == null || fabric.Profiles.Count !=
                    LuoyangMediumFrequencyUrbanFabricKitIds.ModelIds.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang medium-frequency urban-fabric kit header.");

            var modelsById = models.Models.ToDictionary(item => item.ModelId,
                StringComparer.Ordinal);
            var materials = new HashSet<string>(
                models.Materials.Select(item => item.MaterialId), StringComparer.Ordinal);
            var expectedModels = new HashSet<string>(
                LuoyangMediumFrequencyUrbanFabricKitIds.ModelIds,
                StringComparer.Ordinal);
            var seenModels = new HashSet<string>(StringComparer.Ordinal);
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            var assetIds = new HashSet<string>(StringComparer.Ordinal);
            var usage = 0;

            foreach (var profile in fabric.Profiles)
            {
                if (profile == null ||
                    !expectedModels.Contains(profile.ModelId ?? string.Empty) ||
                    !seenModels.Add(profile.ModelId) ||
                    !modelsById.TryGetValue(profile.ModelId, out var model) ||
                    string.IsNullOrWhiteSpace(profile.ProfileId) ||
                    !profileIds.Add(profile.ProfileId) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    string.IsNullOrWhiteSpace(profile.AssetVariantId) ||
                    !assetIds.Add(profile.AssetVariantId) ||
                    string.IsNullOrWhiteSpace(profile.FabricRoleId) ||
                    string.IsNullOrWhiteSpace(profile.DensityClassId) ||
                    string.IsNullOrWhiteSpace(profile.StreetInterfaceId) ||
                    !string.Equals(profile.LodProfileId,
                        LuoyangMediumFrequencyUrbanFabricKitIds.LodProfileId,
                        StringComparison.Ordinal) ||
                    !string.Equals(profile.MaterialSetId,
                        LuoyangMediumFrequencyUrbanFabricKitIds.MaterialSetId,
                        StringComparison.Ordinal) ||
                    profile.OpeningUsageCount !=
                        LuoyangMediumFrequencyUrbanFabricKitIds
                            .OpeningUsageCounts[profile.ModelId] ||
                    profile.FacilityDefinitionIds == null ||
                    !new HashSet<string>(profile.FacilityDefinitionIds,
                        StringComparer.Ordinal).SetEquals(
                        LuoyangMediumFrequencyUrbanFabricKitIds
                            .FacilityDefinitionIds[profile.ModelId]) ||
                    profile.FacilityDefinitionIds.Distinct(StringComparer.Ordinal)
                        .Count() != profile.FacilityDefinitionIds.Count ||
                    profile.AvailabilityIds == null ||
                    profile.AvailabilityIds.Distinct(StringComparer.Ordinal).Count() !=
                        profile.AvailabilityIds.Count ||
                    !new HashSet<string>(profile.AvailabilityIds,
                        StringComparer.Ordinal).SetEquals(model.AvailabilityIds) ||
                    string.IsNullOrWhiteSpace(profile.PlacementAnchorId) ||
                    string.IsNullOrWhiteSpace(profile.EntranceAnchorId) ||
                    !Finite(profile.EntranceX) || !Finite(profile.EntranceY) ||
                    !Finite(profile.EntranceZ) || profile.EntranceY < 0f ||
                    profile.Modules == null || profile.Modules.Count < 6 ||
                    profile.Modules.Count > 32 || profile.Lod1ModuleIds == null ||
                    profile.Lod1ModuleIds.Count == 0 ||
                    profile.Lod2ModuleIds == null ||
                    profile.Lod2ModuleIds.Count == 0)
                    throw new InvalidOperationException(
                        "Invalid Luoyang medium-frequency urban-fabric profile.");

                usage += profile.OpeningUsageCount;
                var halfFootprint = model.StrategicFootprintRatio * 0.5f;
                if (Math.Abs(profile.EntranceX) > halfFootprint + 0.0001f ||
                    Math.Abs(profile.EntranceZ) > halfFootprint + 0.0001f)
                    throw new InvalidOperationException(
                        "Luoyang urban-fabric entrance exceeds its Cell footprint.");

                var moduleIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var module in profile.Modules)
                {
                    if (module == null || string.IsNullOrWhiteSpace(module.ModuleId) ||
                        !moduleIds.Add(module.ModuleId) ||
                        !AllowedPrimitives.Contains(module.PrimitiveId ?? string.Empty) ||
                        !materials.Contains(module.MaterialId ?? string.Empty) ||
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
                            "Invalid Luoyang medium-frequency urban-fabric module.");
                }

                var lod1 = ValidateLod(profile.Lod1ModuleIds, moduleIds, "LOD1");
                var lod2 = ValidateLod(profile.Lod2ModuleIds, moduleIds, "LOD2");
                if (!lod2.IsSubsetOf(lod1))
                    throw new InvalidOperationException(
                        "Luoyang urban-fabric LOD2 must be a subset of LOD1.");
            }

            if (!seenModels.SetEquals(expectedModels) || usage !=
                LuoyangMediumFrequencyUrbanFabricKitIds.MediumFrequencyFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang medium-frequency urban-fabric coverage is incomplete.");
        }

        private static HashSet<string> ValidateLod(IReadOnlyCollection<string> values,
            HashSet<string> modules, string level)
        {
            var result = new HashSet<string>(values ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            if (result.Count == 0 || result.Count != values.Count ||
                result.Any(item => !modules.Contains(item)))
                throw new InvalidOperationException(
                    "Invalid Luoyang urban-fabric " + level + " module list.");
            return result;
        }

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
