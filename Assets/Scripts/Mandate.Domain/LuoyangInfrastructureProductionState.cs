using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangInfrastructureProductionKitIds
    {
        public const string SchemaId =
            "mandate.luoyang-infrastructure-production-kit.v1";
        public const string KitId =
            "LUOYANG_CANAL_WELL_BRIDGE_INFRASTRUCTURE_PRODUCTION_V1";
        public const string LodProfileId =
            "lod.han.strategy.infrastructure.three_tier.v1";
        public const string MaterialSetId =
            "material_set.han.central_plains.shared.v1";
        public const int OpeningFacilityCount = 2084;
        public const int PreviouslyProducedFacilityCount = 1958;
        public const int InfrastructureFacilityCount = 37;
        public const int ProducedOpeningFacilityCount = 1995;
        public const int WaterwayFacilityCount = 21;
        public const int WaterwayComponentCount = 2;
        public const int WaterwayEndpointCount = 4;
        public const int WaterwayStraightCount = 17;
        public const int WellIsolatedCount = 16;
        public const int MinGridColumn = 2018;
        public const int MaxGridColumn = 2079;
        public const int MinGridRow = 1206;
        public const int MaxGridRow = 1266;

        public const int ConnectionEast = 1;
        public const int ConnectionNorth = 2;
        public const int ConnectionWest = 4;
        public const int ConnectionSouth = 8;

        public const string TopologyIsolated =
            "infrastructure.topology.point.isolated";
        public const string TopologyEndpoint =
            "infrastructure.topology.waterway.endpoint";
        public const string TopologyStraight =
            "infrastructure.topology.waterway.straight";
        public const string TopologyTurn =
            "infrastructure.topology.waterway.turn";
        public const string TopologyTee =
            "infrastructure.topology.waterway.tee";
        public const string TopologyCross =
            "infrastructure.topology.waterway.cross";

        public const string CanalModel =
            "model.han.luoyang.public.canal.v1";
        public const string WellModel =
            "model.han.luoyang.public.well.v1";
        public const string BridgeModel =
            "model.han.luoyang.public.bridge.v1";

        public static readonly IReadOnlyList<string> ModelIds = new[]
        {
            CanalModel, WellModel, BridgeModel
        };

        public static readonly IReadOnlyDictionary<string, int> OpeningUsageCounts =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [CanalModel] = 19,
                [WellModel] = 16,
                [BridgeModel] = 2
            };

        public static readonly IReadOnlyDictionary<string, string>
            FacilityDefinitionIds =
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [CanalModel] = "facility.public.canal",
                    [WellModel] = "facility.public.well",
                    [BridgeModel] = "facility.public.bridge"
                };

        public static readonly IReadOnlyDictionary<string, string>
            ModelsByDefinition = FacilityDefinitionIds.ToDictionary(
                item => item.Value, item => item.Key, StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class LuoyangInfrastructureProductionKitCatalog
    {
        public string SchemaId;
        public string KitId;
        public string RegionalStyleId;
        public int OpeningFacilityCount;
        public int InfrastructureFacilityCount;
        public int ProducedOpeningFacilityCount;
        public List<LuoyangInfrastructureProductionProfile> Profiles =
            new List<LuoyangInfrastructureProductionProfile>();
    }

    [Serializable]
    public sealed class LuoyangInfrastructureProductionProfile
    {
        public string ProfileId;
        public string ModelId;
        public string DisplayName;
        public string FacilityDefinitionId;
        public int OpeningUsageCount;
        public string AssetVariantId;
        public string InfrastructureRoleId;
        public string AlignmentModeId;
        public string LodProfileId;
        public string MaterialSetId;
        public List<string> AvailabilityIds = new List<string>();
        public string PlacementAnchorId;
        public List<LuoyangInfrastructureAnchorDefinition> Anchors =
            new List<LuoyangInfrastructureAnchorDefinition>();
        public List<HanBuildableFacilityModuleDefinition> Modules =
            new List<HanBuildableFacilityModuleDefinition>();
        public List<string> Lod1ModuleIds = new List<string>();
        public List<string> Lod2ModuleIds = new List<string>();
    }

    [Serializable]
    public sealed class LuoyangInfrastructureAnchorDefinition
    {
        public string AnchorId;
        public string RoleId;
        public float X;
        public float Y;
        public float Z;
    }

    [Serializable]
    public sealed class LuoyangInfrastructureFacility
    {
        public string FacilityId;
        public string FacilityDefinitionId;
        public string ModelId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
        public int ConnectionMask;
        public string TopologyId;
        public float RotationDegrees;
    }

    public sealed class LuoyangInfrastructureProductionPlan
    {
        public LuoyangInfrastructureProductionPlan(
            LuoyangInfrastructureProductionKitCatalog catalog,
            IReadOnlyList<LuoyangInfrastructureFacility> facilities,
            int waterwayComponentCount, int waterwayEndpointCount,
            int waterwayStraightCount)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Facilities = facilities ?? throw new ArgumentNullException(
                nameof(facilities));
            WaterwayComponentCount = waterwayComponentCount;
            WaterwayEndpointCount = waterwayEndpointCount;
            WaterwayStraightCount = waterwayStraightCount;
        }

        public LuoyangInfrastructureProductionKitCatalog Catalog { get; }
        public IReadOnlyList<LuoyangInfrastructureFacility> Facilities { get; }
        public int WaterwayComponentCount { get; }
        public int WaterwayEndpointCount { get; }
        public int WaterwayStraightCount { get; }
    }

    public static class LuoyangInfrastructureProductionKitRules
    {
        private static readonly HashSet<string> AllowedPrimitives =
            new HashSet<string>(new[]
            {
                "cube", "cylinder", "han.rammed_block", "han.terrain_pad",
                "han.wall_coping", "han.road_crown", "han.tile_slab",
                "han.timber_beam", "han.hip_roof", "han.ritual_ring"
            }, StringComparer.Ordinal);

        public static void Validate(
            LuoyangInfrastructureProductionKitCatalog infrastructure,
            HanBuildableFacilityModelCatalog models)
        {
            if (infrastructure == null)
                throw new ArgumentNullException(nameof(infrastructure));
            HanBuildableFacilityModelCatalogRules.Validate(models);
            if (!string.Equals(infrastructure.SchemaId,
                    LuoyangInfrastructureProductionKitIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(infrastructure.KitId,
                    LuoyangInfrastructureProductionKitIds.KitId,
                    StringComparison.Ordinal) ||
                !string.Equals(infrastructure.RegionalStyleId,
                    models.RegionalStyleId, StringComparison.Ordinal) ||
                infrastructure.OpeningFacilityCount !=
                    LuoyangInfrastructureProductionKitIds.OpeningFacilityCount ||
                infrastructure.InfrastructureFacilityCount !=
                    LuoyangInfrastructureProductionKitIds
                        .InfrastructureFacilityCount ||
                infrastructure.ProducedOpeningFacilityCount !=
                    LuoyangInfrastructureProductionKitIds
                        .ProducedOpeningFacilityCount ||
                infrastructure.Profiles == null ||
                infrastructure.Profiles.Count !=
                    LuoyangInfrastructureProductionKitIds.ModelIds.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang infrastructure production kit header.");

            var modelsById = models.Models.ToDictionary(item => item.ModelId,
                StringComparer.Ordinal);
            var materialIds = new HashSet<string>(models.Materials.Select(item =>
                item.MaterialId), StringComparer.Ordinal);
            var expectedModels = new HashSet<string>(
                LuoyangInfrastructureProductionKitIds.ModelIds,
                StringComparer.Ordinal);
            var seenModels = new HashSet<string>(StringComparer.Ordinal);
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            var assetIds = new HashSet<string>(StringComparer.Ordinal);
            var usage = 0;
            foreach (var profile in infrastructure.Profiles)
            {
                if (profile == null ||
                    !expectedModels.Contains(profile.ModelId ?? string.Empty) ||
                    !seenModels.Add(profile.ModelId) ||
                    !modelsById.TryGetValue(profile.ModelId, out var model) ||
                    string.IsNullOrWhiteSpace(profile.ProfileId) ||
                    !profileIds.Add(profile.ProfileId) ||
                    string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    !string.Equals(profile.FacilityDefinitionId,
                        LuoyangInfrastructureProductionKitIds
                            .FacilityDefinitionIds[profile.ModelId],
                        StringComparison.Ordinal) ||
                    profile.OpeningUsageCount !=
                        LuoyangInfrastructureProductionKitIds
                            .OpeningUsageCounts[profile.ModelId] ||
                    string.IsNullOrWhiteSpace(profile.AssetVariantId) ||
                    !assetIds.Add(profile.AssetVariantId) ||
                    string.IsNullOrWhiteSpace(profile.InfrastructureRoleId) ||
                    string.IsNullOrWhiteSpace(profile.AlignmentModeId) ||
                    !string.Equals(profile.LodProfileId,
                        LuoyangInfrastructureProductionKitIds.LodProfileId,
                        StringComparison.Ordinal) ||
                    !string.Equals(profile.MaterialSetId,
                        LuoyangInfrastructureProductionKitIds.MaterialSetId,
                        StringComparison.Ordinal) ||
                    profile.AvailabilityIds == null ||
                    profile.AvailabilityIds.Distinct(StringComparer.Ordinal)
                        .Count() != profile.AvailabilityIds.Count ||
                    !new HashSet<string>(profile.AvailabilityIds,
                        StringComparer.Ordinal).SetEquals(model.AvailabilityIds) ||
                    string.IsNullOrWhiteSpace(profile.PlacementAnchorId) ||
                    profile.Anchors == null || profile.Anchors.Count < 2 ||
                    profile.Modules == null || profile.Modules.Count < 6 ||
                    profile.Modules.Count > 32 ||
                    profile.Lod1ModuleIds == null ||
                    profile.Lod1ModuleIds.Count == 0 ||
                    profile.Lod2ModuleIds == null ||
                    profile.Lod2ModuleIds.Count == 0)
                    throw new InvalidOperationException(
                        "Invalid Luoyang infrastructure production profile.");

                usage += profile.OpeningUsageCount;
                var halfFootprint = model.StrategicFootprintRatio * 0.5f;
                var anchorIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var anchor in profile.Anchors)
                    if (anchor == null ||
                        string.IsNullOrWhiteSpace(anchor.AnchorId) ||
                        !anchorIds.Add(anchor.AnchorId) ||
                        string.IsNullOrWhiteSpace(anchor.RoleId) ||
                        !Finite(anchor.X) || !Finite(anchor.Y) ||
                        !Finite(anchor.Z) || anchor.Y < 0f ||
                        Math.Abs(anchor.X) > halfFootprint + 0.0001f ||
                        Math.Abs(anchor.Z) > halfFootprint + 0.0001f)
                        throw new InvalidOperationException(
                            "Invalid Luoyang infrastructure anchor.");

                var moduleIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var module in profile.Modules)
                {
                    if (module == null ||
                        string.IsNullOrWhiteSpace(module.ModuleId) ||
                        !moduleIds.Add(module.ModuleId) ||
                        !AllowedPrimitives.Contains(module.PrimitiveId ?? "") ||
                        !materialIds.Contains(module.MaterialId ?? "") ||
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
                            "Invalid Luoyang infrastructure module.");
                }

                var lod1 = ValidateLod(profile.Lod1ModuleIds, moduleIds, "LOD1");
                var lod2 = ValidateLod(profile.Lod2ModuleIds, moduleIds, "LOD2");
                if (!lod2.IsSubsetOf(lod1))
                    throw new InvalidOperationException(
                        "Luoyang infrastructure LOD2 must be a subset of LOD1.");
            }

            if (!seenModels.SetEquals(expectedModels) || usage !=
                LuoyangInfrastructureProductionKitIds.InfrastructureFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang infrastructure production coverage is incomplete.");
        }

        public static LuoyangInfrastructureProductionPlan CreatePlan(
            LuoyangInfrastructureProductionKitCatalog catalog,
            IEnumerable<LuoyangInfrastructureFacility> source)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = source.OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal).ToArray();
            if (values.Length !=
                LuoyangInfrastructureProductionKitIds.InfrastructureFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang infrastructure plan has the wrong Facility count.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var cells = new HashSet<ulong>();
            var usage = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var modelId in LuoyangInfrastructureProductionKitIds.ModelIds)
                usage[modelId] = 0;
            var waterwayCells = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.FacilityId) ||
                    !ids.Add(value.FacilityId) || value.CellId64 == 0 ||
                    !cells.Add(value.CellId64) ||
                    value.GridColumn <
                        LuoyangInfrastructureProductionKitIds.MinGridColumn ||
                    value.GridColumn >
                        LuoyangInfrastructureProductionKitIds.MaxGridColumn ||
                    value.GridRow <
                        LuoyangInfrastructureProductionKitIds.MinGridRow ||
                    value.GridRow >
                        LuoyangInfrastructureProductionKitIds.MaxGridRow ||
                    !LuoyangInfrastructureProductionKitIds.ModelsByDefinition
                        .TryGetValue(value.FacilityDefinitionId ?? "",
                            out var expectedModel) ||
                    !string.Equals(value.ModelId, expectedModel,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Invalid Luoyang infrastructure Facility.");
                usage[value.ModelId]++;
                if (!string.Equals(value.ModelId,
                        LuoyangInfrastructureProductionKitIds.WellModel,
                        StringComparison.Ordinal))
                    waterwayCells.Add(Key(value.GridRow, value.GridColumn));
            }
            if (usage.Any(item => item.Value !=
                    LuoyangInfrastructureProductionKitIds
                        .OpeningUsageCounts[item.Key]))
                throw new InvalidOperationException(
                    "Luoyang infrastructure Facility usage is incomplete.");

            var facilities = new List<LuoyangInfrastructureFacility>(
                values.Length);
            var endpoints = 0;
            var straights = 0;
            foreach (var value in values)
            {
                var mask = 0;
                var topology = LuoyangInfrastructureProductionKitIds
                    .TopologyIsolated;
                var rotation = 0f;
                if (!string.Equals(value.ModelId,
                        LuoyangInfrastructureProductionKitIds.WellModel,
                        StringComparison.Ordinal))
                {
                    if (waterwayCells.Contains(Key(value.GridRow,
                            value.GridColumn + 1)))
                        mask |= LuoyangInfrastructureProductionKitIds.ConnectionEast;
                    if (waterwayCells.Contains(Key(value.GridRow - 1,
                            value.GridColumn)))
                        mask |= LuoyangInfrastructureProductionKitIds.ConnectionNorth;
                    if (waterwayCells.Contains(Key(value.GridRow,
                            value.GridColumn - 1)))
                        mask |= LuoyangInfrastructureProductionKitIds.ConnectionWest;
                    if (waterwayCells.Contains(Key(value.GridRow + 1,
                            value.GridColumn)))
                        mask |= LuoyangInfrastructureProductionKitIds.ConnectionSouth;
                    var degree = ConnectionCount(mask);
                    if (degree == 0)
                        throw new InvalidOperationException(
                            "Luoyang infrastructure waterway cannot be isolated.");
                    topology = TopologyFor(mask, degree);
                    rotation = RotationFor(mask);
                    if (degree == 1) endpoints++;
                    if (string.Equals(topology,
                            LuoyangInfrastructureProductionKitIds.TopologyStraight,
                            StringComparison.Ordinal))
                        straights++;
                }
                facilities.Add(new LuoyangInfrastructureFacility
                {
                    FacilityId = value.FacilityId,
                    FacilityDefinitionId = value.FacilityDefinitionId,
                    ModelId = value.ModelId,
                    CellId64 = value.CellId64,
                    GridColumn = value.GridColumn,
                    GridRow = value.GridRow,
                    ConnectionMask = mask,
                    TopologyId = topology,
                    RotationDegrees = rotation
                });
            }

            var components = CountComponents(waterwayCells);
            if (waterwayCells.Count !=
                    LuoyangInfrastructureProductionKitIds.WaterwayFacilityCount ||
                components !=
                    LuoyangInfrastructureProductionKitIds.WaterwayComponentCount ||
                endpoints !=
                    LuoyangInfrastructureProductionKitIds.WaterwayEndpointCount ||
                straights !=
                    LuoyangInfrastructureProductionKitIds.WaterwayStraightCount ||
                facilities.Count(item => string.Equals(item.TopologyId,
                    LuoyangInfrastructureProductionKitIds.TopologyIsolated,
                    StringComparison.Ordinal)) !=
                    LuoyangInfrastructureProductionKitIds.WellIsolatedCount)
                throw new InvalidOperationException(
                    "Luoyang infrastructure topology does not match the opening data.");
            return new LuoyangInfrastructureProductionPlan(catalog,
                facilities.ToArray(), components, endpoints, straights);
        }

        private static HashSet<string> ValidateLod(
            IReadOnlyCollection<string> values, HashSet<string> modules,
            string level)
        {
            var result = new HashSet<string>(values ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            if (result.Count == 0 || result.Count != values.Count ||
                result.Any(item => !modules.Contains(item)))
                throw new InvalidOperationException(
                    "Invalid Luoyang infrastructure " + level +
                    " module list.");
            return result;
        }

        private static int CountComponents(HashSet<string> source)
        {
            var remaining = new HashSet<string>(source, StringComparer.Ordinal);
            var components = 0;
            while (remaining.Count > 0)
            {
                components++;
                var queue = new Queue<string>();
                var first = remaining.First();
                remaining.Remove(first);
                queue.Enqueue(first);
                while (queue.Count > 0)
                {
                    var parts = queue.Dequeue().Split(':');
                    var row = int.Parse(parts[0]);
                    var column = int.Parse(parts[1]);
                    foreach (var neighbor in new[]
                             {
                                 Key(row, column + 1), Key(row - 1, column),
                                 Key(row, column - 1), Key(row + 1, column)
                             })
                        if (remaining.Remove(neighbor)) queue.Enqueue(neighbor);
                }
            }
            return components;
        }

        private static string TopologyFor(int mask, int degree)
        {
            if (degree == 1)
                return LuoyangInfrastructureProductionKitIds.TopologyEndpoint;
            if (degree == 2)
            {
                var horizontal = LuoyangInfrastructureProductionKitIds
                    .ConnectionEast |
                    LuoyangInfrastructureProductionKitIds.ConnectionWest;
                var vertical = LuoyangInfrastructureProductionKitIds
                    .ConnectionNorth |
                    LuoyangInfrastructureProductionKitIds.ConnectionSouth;
                return mask == horizontal || mask == vertical
                    ? LuoyangInfrastructureProductionKitIds.TopologyStraight
                    : LuoyangInfrastructureProductionKitIds.TopologyTurn;
            }
            if (degree == 3)
                return LuoyangInfrastructureProductionKitIds.TopologyTee;
            if (degree == 4)
                return LuoyangInfrastructureProductionKitIds.TopologyCross;
            throw new InvalidOperationException(
                "Invalid Luoyang infrastructure connection degree.");
        }

        private static float RotationFor(int mask)
        {
            var horizontal = (mask & (LuoyangInfrastructureProductionKitIds
                .ConnectionEast |
                LuoyangInfrastructureProductionKitIds.ConnectionWest)) != 0;
            var vertical = (mask & (LuoyangInfrastructureProductionKitIds
                .ConnectionNorth |
                LuoyangInfrastructureProductionKitIds.ConnectionSouth)) != 0;
            return vertical && !horizontal ? 90f : 0f;
        }

        private static int ConnectionCount(int mask)
        {
            var count = 0;
            for (var value = mask; value != 0; value >>= 1)
                count += value & 1;
            return count;
        }

        private static string Key(int row, int column) => row + ":" + column;

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
