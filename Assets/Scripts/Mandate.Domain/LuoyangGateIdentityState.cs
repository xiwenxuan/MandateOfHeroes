using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangGateIdentityKitIds
    {
        public const string SchemaId = "mandate.luoyang-gate-identity-kit.v1";
        public const string KitId =
            "LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1";
        public const string LodProfileId = "lod.han.strategy.gate.three_tier.v1";
        public const string MaterialSetId =
            "material_set.han.central_plains.shared.v1";
        public const string CityGateClassId = "gate_class.city";
        public const string PalaceGateClassId = "gate_class.palace";

        public const string Guangyangmen =
            "facility.instance.luoyang.184.gate.guangyangmen";
        public const string Gumen = "facility.instance.luoyang.184.gate.gumen";
        public const string Jinmen = "facility.instance.luoyang.184.gate.jinmen";
        public const string Kaiyangmen =
            "facility.instance.luoyang.184.gate.kaiyangmen";
        public const string Maomen = "facility.instance.luoyang.184.gate.maomen";
        public const string Pingchengmen =
            "facility.instance.luoyang.184.gate.pingchengmen";
        public const string Shangdongmen =
            "facility.instance.luoyang.184.gate.shangdongmen";
        public const string Shangximen =
            "facility.instance.luoyang.184.gate.shangximen";
        public const string Xiamen = "facility.instance.luoyang.184.gate.xiamen";
        public const string Xiaoyuanmen =
            "facility.instance.luoyang.184.gate.xiaoyuanmen";
        public const string Yongmen = "facility.instance.luoyang.184.gate.yongmen";
        public const string Zhongdongmen =
            "facility.instance.luoyang.184.gate.zhongdongmen";
        public const string NorthPalaceSouthGate =
            "facility.instance.luoyang.184.north_palace_gate.1240.2043";
        public const string SouthPalaceNorthGate =
            "facility.instance.luoyang.184.south_palace_gate.1242.2043";

        public static readonly IReadOnlyList<string> FacilityIds = new[]
        {
            Guangyangmen, Gumen, Jinmen, Kaiyangmen, Maomen, Pingchengmen,
            Shangdongmen, Shangximen, Xiamen, Xiaoyuanmen, Yongmen,
            Zhongdongmen, NorthPalaceSouthGate, SouthPalaceNorthGate
        };

        public static readonly IReadOnlyDictionary<string, string> BaseModelIds =
            FacilityIds.ToDictionary(id => id,
                id => id == NorthPalaceSouthGate || id == SouthPalaceNorthGate
                    ? LuoyangFacilityModelCoverageIds.PalaceGate
                    : HanBuildableFacilityModelIds.CityGate,
                StringComparer.Ordinal);

        public static readonly IReadOnlyDictionary<string, string> GateClassIds =
            FacilityIds.ToDictionary(id => id,
                id => id == NorthPalaceSouthGate || id == SouthPalaceNorthGate
                    ? PalaceGateClassId : CityGateClassId,
                StringComparer.Ordinal);

        public static readonly IReadOnlyDictionary<string, ulong> CellIds =
            new Dictionary<string, ulong>(StringComparer.Ordinal)
            {
                [Guangyangmen] = 4131278UL, [Gumen] = 4084888UL,
                [Jinmen] = 4144537UL, [Kaiyangmen] = 4144549UL,
                [Maomen] = 4131296UL, [Pingchengmen] = 4144545UL,
                [Shangdongmen] = 4098156UL, [Shangximen] = 4098138UL,
                [Xiamen] = 4084894UL, [Xiaoyuanmen] = 4144541UL,
                [Yongmen] = 4114708UL, [Zhongdongmen] = 4114726UL,
                [NorthPalaceSouthGate] = 4111403UL,
                [SouthPalaceNorthGate] = 4118031UL
            };

        public static readonly IReadOnlyDictionary<string, int> GridX =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [Guangyangmen] = 2034, [Gumen] = 2040, [Jinmen] = 2037,
                [Kaiyangmen] = 2049, [Maomen] = 2052, [Pingchengmen] = 2045,
                [Shangdongmen] = 2052, [Shangximen] = 2034, [Xiamen] = 2046,
                [Xiaoyuanmen] = 2041, [Yongmen] = 2034,
                [Zhongdongmen] = 2052, [NorthPalaceSouthGate] = 2043,
                [SouthPalaceNorthGate] = 2043
            };

        public static readonly IReadOnlyDictionary<string, int> GridY =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [Guangyangmen] = 1246, [Gumen] = 1232, [Jinmen] = 1250,
                [Kaiyangmen] = 1250, [Maomen] = 1246,
                [Pingchengmen] = 1250, [Shangdongmen] = 1236,
                [Shangximen] = 1236, [Xiamen] = 1232,
                [Xiaoyuanmen] = 1250, [Yongmen] = 1241,
                [Zhongdongmen] = 1241, [NorthPalaceSouthGate] = 1240,
                [SouthPalaceNorthGate] = 1242
            };

        public static readonly IReadOnlyDictionary<string, string>
            FacilityDirections = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [Guangyangmen] = "west", [Gumen] = "north",
                [Jinmen] = "south", [Kaiyangmen] = "south",
                [Maomen] = "east", [Pingchengmen] = "south",
                [Shangdongmen] = "east", [Shangximen] = "west",
                [Xiamen] = "north", [Xiaoyuanmen] = "south",
                [Yongmen] = "west", [Zhongdongmen] = "east",
                [NorthPalaceSouthGate] = string.Empty,
                [SouthPalaceNorthGate] = string.Empty
            };

        public static readonly IReadOnlyDictionary<string, string> VisualFacings =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [Guangyangmen] = "west", [Gumen] = "north",
                [Jinmen] = "south", [Kaiyangmen] = "south",
                [Maomen] = "east", [Pingchengmen] = "south",
                [Shangdongmen] = "east", [Shangximen] = "west",
                [Xiamen] = "north", [Xiaoyuanmen] = "south",
                [Yongmen] = "west", [Zhongdongmen] = "east",
                [NorthPalaceSouthGate] = "south",
                [SouthPalaceNorthGate] = "north"
            };

        public static float RotationForFacing(string facing)
        {
            switch (facing)
            {
                case "south": return 0f;
                case "west": return 90f;
                case "north": return 180f;
                case "east": return 270f;
                default: throw new ArgumentOutOfRangeException(nameof(facing));
            }
        }
    }

    [Serializable]
    public sealed class LuoyangGateIdentityKitCatalog
    {
        public string SchemaId;
        public string KitId;
        public string RegionalStyleId;
        public List<LuoyangGateIdentityProfile> Profiles =
            new List<LuoyangGateIdentityProfile>();
    }

    [Serializable]
    public sealed class LuoyangGateIdentityProfile
    {
        public string ProfileId;
        public string FacilityId;
        public string DisplayName;
        public string GateClassId;
        public string GatehouseTypeId;
        public string BaseModelId;
        public string AssetVariantId;
        public string SilhouetteId;
        public string LodProfileId;
        public string MaterialSetId;
        public ulong CellId64;
        public int GridX;
        public int GridY;
        public string FacilityDirection;
        public string VisualFacing;
        public string DirectionBasisId;
        public string WallConnectionAxis;
        public string HistoricalConfidence;
        public string SpatialPrecision;
        public string HistoricalBasis;
        public List<string> SourceIds = new List<string>();
        public List<string> AvailabilityIds = new List<string>();
        public string PlacementAnchorId;
        public string OuterPassageAnchorId;
        public string InnerPassageAnchorId;
        public float OuterPassageX;
        public float OuterPassageY;
        public float OuterPassageZ;
        public float InnerPassageX;
        public float InnerPassageY;
        public float InnerPassageZ;
        public List<HanBuildableFacilityModuleDefinition> Modules =
            new List<HanBuildableFacilityModuleDefinition>();
        public List<string> Lod1ModuleIds = new List<string>();
        public List<string> Lod2ModuleIds = new List<string>();
    }

    public static class LuoyangGateIdentityKitRules
    {
        private static readonly HashSet<string> AllowedPrimitives =
            new HashSet<string>(new[]
            {
                "cube", "cylinder", "han.rammed_block", "han.tile_slab",
                "han.terrain_pad", "han.foliage_cluster", "han.wall_coping",
                "han.timber_beam", "han.hip_roof"
            }, StringComparer.Ordinal);

        private static readonly HashSet<string> AllowedAvailability =
            new HashSet<string>(new[]
            {
                "Government", "Military", "HistoricalInit", "Event"
            }, StringComparer.Ordinal);

        public static void Validate(LuoyangGateIdentityKitCatalog gates,
            HanBuildableFacilityModelCatalog models)
        {
            if (gates == null) throw new ArgumentNullException(nameof(gates));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            if (!string.Equals(gates.SchemaId, LuoyangGateIdentityKitIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(gates.KitId, LuoyangGateIdentityKitIds.KitId,
                    StringComparison.Ordinal) ||
                !string.Equals(gates.RegionalStyleId, models.RegionalStyleId,
                    StringComparison.Ordinal) || gates.Profiles == null ||
                gates.Profiles.Count != LuoyangGateIdentityKitIds.FacilityIds.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang gate identity kit header.");

            var modelsById = models.Models.ToDictionary(item => item.ModelId,
                StringComparer.Ordinal);
            var materialIds = new HashSet<string>(
                models.Materials.Select(item => item.MaterialId), StringComparer.Ordinal);
            var expectedIds = new HashSet<string>(
                LuoyangGateIdentityKitIds.FacilityIds, StringComparer.Ordinal);
            var facilityIds = new HashSet<string>(StringComparer.Ordinal);
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            var assetIds = new HashSet<string>(StringComparer.Ordinal);
            var silhouettes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var profile in gates.Profiles)
            {
                if (profile == null ||
                    !expectedIds.Contains(profile.FacilityId ?? string.Empty) ||
                    !facilityIds.Add(profile.FacilityId) ||
                    string.IsNullOrWhiteSpace(profile.ProfileId) ||
                    !profileIds.Add(profile.ProfileId) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    !LuoyangGateIdentityKitIds.GateClassIds.TryGetValue(
                        profile.FacilityId, out var gateClassId) ||
                    !string.Equals(profile.GateClassId, gateClassId,
                        StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(profile.GatehouseTypeId) ||
                    !LuoyangGateIdentityKitIds.BaseModelIds.TryGetValue(
                        profile.FacilityId, out var baseModelId) ||
                    !string.Equals(profile.BaseModelId, baseModelId,
                        StringComparison.Ordinal) ||
                    !modelsById.TryGetValue(profile.BaseModelId ?? string.Empty,
                        out var model) ||
                    string.IsNullOrWhiteSpace(profile.AssetVariantId) ||
                    !assetIds.Add(profile.AssetVariantId) ||
                    string.IsNullOrWhiteSpace(profile.SilhouetteId) ||
                    !silhouettes.Add(profile.SilhouetteId) ||
                    !string.Equals(profile.LodProfileId,
                        LuoyangGateIdentityKitIds.LodProfileId,
                        StringComparison.Ordinal) ||
                    !string.Equals(profile.MaterialSetId,
                        LuoyangGateIdentityKitIds.MaterialSetId,
                        StringComparison.Ordinal) ||
                    profile.CellId64 != LuoyangGateIdentityKitIds.CellIds[profile.FacilityId] ||
                    profile.GridX != LuoyangGateIdentityKitIds.GridX[profile.FacilityId] ||
                    profile.GridY != LuoyangGateIdentityKitIds.GridY[profile.FacilityId] ||
                    !string.Equals(profile.FacilityDirection ?? string.Empty,
                        LuoyangGateIdentityKitIds.FacilityDirections[profile.FacilityId],
                        StringComparison.Ordinal) ||
                    !string.Equals(profile.VisualFacing,
                        LuoyangGateIdentityKitIds.VisualFacings[profile.FacilityId],
                        StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(profile.DirectionBasisId) ||
                    (profile.WallConnectionAxis != "east_west" &&
                     profile.WallConnectionAxis != "north_south") ||
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
                    string.IsNullOrWhiteSpace(profile.OuterPassageAnchorId) ||
                    string.IsNullOrWhiteSpace(profile.InnerPassageAnchorId) ||
                    !Finite(profile.OuterPassageX) || !Finite(profile.OuterPassageY) ||
                    !Finite(profile.OuterPassageZ) || !Finite(profile.InnerPassageX) ||
                    !Finite(profile.InnerPassageY) || !Finite(profile.InnerPassageZ) ||
                    profile.OuterPassageY < 0f || profile.InnerPassageY < 0f ||
                    profile.Modules == null || profile.Modules.Count < 5 ||
                    profile.Modules.Count > 32 || profile.Lod1ModuleIds == null ||
                    profile.Lod1ModuleIds.Count == 0 || profile.Lod2ModuleIds == null ||
                    profile.Lod2ModuleIds.Count == 0)
                    throw new InvalidOperationException(
                        "Invalid Luoyang gate identity profile.");

                var halfFootprint = model.StrategicFootprintRatio * 0.5f;
                if (Math.Abs(profile.OuterPassageX) > halfFootprint + 0.0001f ||
                    Math.Abs(profile.OuterPassageZ) > halfFootprint + 0.0001f ||
                    Math.Abs(profile.InnerPassageX) > halfFootprint + 0.0001f ||
                    Math.Abs(profile.InnerPassageZ) > halfFootprint + 0.0001f ||
                    profile.OuterPassageZ >= profile.InnerPassageZ)
                    throw new InvalidOperationException(
                        "Luoyang gate passage anchors exceed the Cell footprint.");

                var moduleIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var module in profile.Modules)
                {
                    if (module == null || string.IsNullOrWhiteSpace(module.ModuleId) ||
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
                            "Invalid Luoyang gate identity module.");
                }

                var lod1 = ValidateLod(profile.Lod1ModuleIds, moduleIds, "LOD1");
                var lod2 = ValidateLod(profile.Lod2ModuleIds, moduleIds, "LOD2");
                if (!lod2.IsSubsetOf(lod1))
                    throw new InvalidOperationException(
                        "Luoyang gate LOD2 modules must be a subset of LOD1.");
            }

            if (!facilityIds.SetEquals(expectedIds))
                throw new InvalidOperationException(
                    "Luoyang gate identity kit does not cover the frozen facilities.");
        }

        private static HashSet<string> ValidateLod(IEnumerable<string> values,
            HashSet<string> moduleIds, string level)
        {
            var result = new HashSet<string>(values ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            if (result.Count == 0 || result.Any(item => !moduleIds.Contains(item)) ||
                result.Count != values.Count())
                throw new InvalidOperationException(
                    "Invalid Luoyang gate " + level + " module list.");
            return result;
        }

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
