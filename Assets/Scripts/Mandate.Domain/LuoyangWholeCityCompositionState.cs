using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangWholeCityCompositionIds
    {
        public const string ContractId =
            "presentation.luoyang.whole-city-composition-and-terrain-integration.v1";
        public const string TaskId =
            "LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1";
        public const string StatusId =
            "LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW";
        public const string RegionalStyleId =
            "regional_style.central_plains.han.v1";

        public const string PalaceCivicDistrictId =
            "district.luoyang.palace-civic-core.v1";
        public const string ResidentialWardDistrictId =
            "district.luoyang.residential-wards.v1";
        public const string MarketWorkshopDistrictId =
            "district.luoyang.market-workshop-band.v1";
        public const string DefenseDistrictId =
            "district.luoyang.defense-ring.v1";
        public const string WaterTransportDistrictId =
            "district.luoyang.water-transport-network.v1";
        public const string AgriculturalResourceDistrictId =
            "district.luoyang.agricultural-resource-hinterland.v1";

        public const int FacilityAnchorCount = 2084;
        public const int DistrictCount = 6;
        public const int AssetVariantCount = 54;
        public const int DensestResidentAnchorCount = 549;
        public const float FrontageOffsetMetres = 320f;
        public const float MaximumLocalOffsetMetres = 420f;
        public const bool CreatesSimulationSubCells = false;

        public static readonly IReadOnlyList<string> DistrictIds = new[]
        {
            PalaceCivicDistrictId,
            ResidentialWardDistrictId,
            MarketWorkshopDistrictId,
            DefenseDistrictId,
            WaterTransportDistrictId,
            AgriculturalResourceDistrictId
        };
    }

    [Serializable]
    public sealed class LuoyangWholeCityCompositionAnchor
    {
        public string FacilityId;
        public string FacilityDefinitionId;
        public string ModelId;
        public string AssetVariantId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
        public string DistrictId;
        public string SurfaceProfileId;
        public string ConnectionProfileId;
        public float VisualLocalEastMetres;
        public float VisualLocalNorthMetres;
        public float RotationDegrees;
        public float Scale;
        public bool CorridorAligned;
        public bool TerrainGroundingRequired;
    }

    public sealed class LuoyangWholeCityCompositionPlan
    {
        public LuoyangWholeCityCompositionPlan(
            IReadOnlyList<LuoyangWholeCityCompositionAnchor> anchors,
            IReadOnlyDictionary<string, LuoyangWholeCityCompositionAnchor>
                anchorsByFacilityId,
            IReadOnlyDictionary<string, int> facilityCountByDistrict)
        {
            Anchors = anchors ?? throw new ArgumentNullException(nameof(anchors));
            AnchorsByFacilityId = anchorsByFacilityId ??
                                  throw new ArgumentNullException(
                                      nameof(anchorsByFacilityId));
            FacilityCountByDistrict = facilityCountByDistrict ??
                                      throw new ArgumentNullException(
                                          nameof(facilityCountByDistrict));
        }

        public string ContractId => LuoyangWholeCityCompositionIds.ContractId;
        public string TaskId => LuoyangWholeCityCompositionIds.TaskId;
        public string StatusId => LuoyangWholeCityCompositionIds.StatusId;
        public string RegionalStyleId =>
            LuoyangWholeCityCompositionIds.RegionalStyleId;
        public bool CreatesSimulationSubCells =>
            LuoyangWholeCityCompositionIds.CreatesSimulationSubCells;
        public IReadOnlyList<LuoyangWholeCityCompositionAnchor> Anchors { get; }
        public IReadOnlyDictionary<string, LuoyangWholeCityCompositionAnchor>
            AnchorsByFacilityId { get; }
        public IReadOnlyDictionary<string, int> FacilityCountByDistrict { get; }
    }

    public static class LuoyangWholeCityCompositionRules
    {
        public static LuoyangWholeCityCompositionPlan CreatePlan(
            LuoyangBuildingPerformancePlan wholeCity,
            LuoyangFinalAssetReviewPlan finalAssets)
        {
            if (wholeCity == null)
                throw new ArgumentNullException(nameof(wholeCity));
            if (finalAssets == null)
                throw new ArgumentNullException(nameof(finalAssets));
            if (wholeCity.Facilities.Count !=
                    LuoyangWholeCityCompositionIds.FacilityAnchorCount ||
                finalAssets.FacilityAssetVariants.Count !=
                    LuoyangWholeCityCompositionIds.FacilityAnchorCount ||
                finalAssets.FacilityAssetVariants.Values.Distinct(
                    StringComparer.Ordinal).Count() !=
                    LuoyangWholeCityCompositionIds.AssetVariantCount)
                throw new InvalidOperationException(
                    "Luoyang whole-city composition requires all Facilities and final assets.");

            var corridorCells = wholeCity.Facilities
                .Where(item => IsCorridor(item.FacilityDefinitionId))
                .GroupBy(item => CorridorFamily(item.FacilityDefinitionId),
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => new HashSet<long>(group.Select(item =>
                        CellKey(item.GridRow, item.GridColumn))),
                    StringComparer.Ordinal);
            var roads = wholeCity.Facilities.Where(item => string.Equals(
                    item.FacilityDefinitionId, "facility.public.road",
                    StringComparison.Ordinal))
                .OrderBy(item => item.CellId64).ToArray();
            var anchors = new List<LuoyangWholeCityCompositionAnchor>(
                wholeCity.Facilities.Count);
            foreach (var facility in wholeCity.Facilities)
            {
                if (!finalAssets.FacilityAssetVariants.TryGetValue(
                        facility.FacilityId, out var assetVariantId))
                    throw new InvalidOperationException(
                        "Luoyang Facility is missing its final asset variant: " +
                        facility.FacilityId);
                var corridor = IsCorridor(facility.FacilityDefinitionId);
                var districtId = ResolveDistrictId(
                    facility.FacilityDefinitionId);
                var connection = corridor
                    ? ResolveCorridor(facility, corridorCells[
                        CorridorFamily(facility.FacilityDefinitionId)])
                    : ResolveFrontage(facility, roads);
                anchors.Add(new LuoyangWholeCityCompositionAnchor
                {
                    FacilityId = facility.FacilityId,
                    FacilityDefinitionId = facility.FacilityDefinitionId,
                    ModelId = facility.ModelId,
                    AssetVariantId = assetVariantId,
                    CellId64 = facility.CellId64,
                    GridColumn = facility.GridColumn,
                    GridRow = facility.GridRow,
                    DistrictId = districtId,
                    SurfaceProfileId = ResolveSurfaceProfileId(
                        facility.FacilityDefinitionId, districtId),
                    ConnectionProfileId = connection.ProfileId,
                    VisualLocalEastMetres = connection.LocalEastMetres,
                    VisualLocalNorthMetres = connection.LocalNorthMetres,
                    RotationDegrees = connection.RotationDegrees,
                    Scale = ResolveScale(districtId, facility.FacilityId),
                    CorridorAligned = corridor,
                    TerrainGroundingRequired = true
                });
            }

            var ordered = anchors.OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                .ToArray();
            var byId = ordered.ToDictionary(item => item.FacilityId,
                StringComparer.Ordinal);
            var byDistrict = ordered.GroupBy(item => item.DistrictId,
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(),
                    StringComparer.Ordinal);
            var plan = new LuoyangWholeCityCompositionPlan(ordered, byId,
                byDistrict);
            Validate(plan, wholeCity);
            return plan;
        }

        public static IReadOnlyList<LuoyangWholeCityCompositionAnchor>
            SelectDensestResidentAnchors(
                LuoyangWholeCityCompositionPlan composition,
                LuoyangBuildingPerformancePlan wholeCity)
        {
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));
            var window = LuoyangBuildingPerformanceRules
                .SelectDensestResidentWindow(wholeCity);
            var anchors = window.Facilities.Select(item =>
                composition.AnchorsByFacilityId[item.FacilityId]).ToArray();
            if (anchors.Length !=
                LuoyangWholeCityCompositionIds.DensestResidentAnchorCount)
                throw new InvalidOperationException(
                    "Luoyang dense composition window has the wrong anchor count.");
            return anchors;
        }

        public static void Validate(LuoyangWholeCityCompositionPlan plan,
            LuoyangBuildingPerformancePlan wholeCity)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (wholeCity == null)
                throw new ArgumentNullException(nameof(wholeCity));
            if (plan.CreatesSimulationSubCells ||
                plan.Anchors.Count !=
                    LuoyangWholeCityCompositionIds.FacilityAnchorCount ||
                plan.AnchorsByFacilityId.Count != plan.Anchors.Count ||
                plan.FacilityCountByDistrict.Count !=
                    LuoyangWholeCityCompositionIds.DistrictCount ||
                LuoyangWholeCityCompositionIds.DistrictIds.Any(id =>
                    !plan.FacilityCountByDistrict.TryGetValue(id,
                        out var count) || count <= 0))
                throw new InvalidOperationException(
                    "Invalid Luoyang whole-city composition coverage.");

            var facilitiesById = wholeCity.Facilities.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            var cells = new HashSet<ulong>();
            var assets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var anchor in plan.Anchors)
            {
                if (anchor == null ||
                    !facilitiesById.TryGetValue(anchor.FacilityId,
                        out var facility) ||
                    anchor.CellId64 != facility.CellId64 ||
                    anchor.GridColumn != facility.GridColumn ||
                    anchor.GridRow != facility.GridRow ||
                    !cells.Add(anchor.CellId64) ||
                    string.IsNullOrWhiteSpace(anchor.AssetVariantId) ||
                    string.IsNullOrWhiteSpace(anchor.DistrictId) ||
                    string.IsNullOrWhiteSpace(anchor.SurfaceProfileId) ||
                    string.IsNullOrWhiteSpace(anchor.ConnectionProfileId) ||
                    !anchor.TerrainGroundingRequired ||
                    !Finite(anchor.VisualLocalEastMetres) ||
                    !Finite(anchor.VisualLocalNorthMetres) ||
                    Math.Abs(anchor.VisualLocalEastMetres) >
                        LuoyangWholeCityCompositionIds.MaximumLocalOffsetMetres ||
                    Math.Abs(anchor.VisualLocalNorthMetres) >
                        LuoyangWholeCityCompositionIds.MaximumLocalOffsetMetres ||
                    !Finite(anchor.RotationDegrees) ||
                    anchor.RotationDegrees < 0f ||
                    anchor.RotationDegrees >= 360f ||
                    Math.Abs(anchor.RotationDegrees % 90f) > 0.001f ||
                    !Finite(anchor.Scale) || anchor.Scale < 0.80f ||
                    anchor.Scale > 1.20f ||
                    (anchor.CorridorAligned &&
                     (Math.Abs(anchor.VisualLocalEastMetres) > 0.001f ||
                      Math.Abs(anchor.VisualLocalNorthMetres) > 0.001f)))
                    throw new InvalidOperationException(
                        "Invalid Luoyang whole-city composition anchor: " +
                        anchor?.FacilityId);
                assets.Add(anchor.AssetVariantId);
            }
            if (cells.Count != wholeCity.Facilities.Count ||
                assets.Count !=
                    LuoyangWholeCityCompositionIds.AssetVariantCount)
                throw new InvalidOperationException(
                    "Luoyang whole-city composition identity coverage is incomplete.");
        }

        private static string ResolveDistrictId(string definitionId)
        {
            if (definitionId.StartsWith("facility.fortification.",
                    StringComparison.Ordinal) ||
                definitionId.StartsWith("facility.military.",
                    StringComparison.Ordinal))
                return LuoyangWholeCityCompositionIds.DefenseDistrictId;
            if (definitionId.StartsWith("facility.agriculture.",
                    StringComparison.Ordinal) ||
                definitionId.StartsWith("facility.resource.",
                    StringComparison.Ordinal) ||
                definitionId == "facility.residential.rural_hamlet")
                return LuoyangWholeCityCompositionIds
                    .AgriculturalResourceDistrictId;
            if (definitionId == "facility.public.road" ||
                definitionId == "facility.public.canal" ||
                definitionId == "facility.public.bridge" ||
                definitionId == "facility.public.well" ||
                definitionId.StartsWith("facility.service.post_station",
                    StringComparison.Ordinal) ||
                definitionId.StartsWith("facility.service.caravan_yard",
                    StringComparison.Ordinal))
                return LuoyangWholeCityCompositionIds.WaterTransportDistrictId;
            if (definitionId.StartsWith("facility.residential.",
                    StringComparison.Ordinal))
                return LuoyangWholeCityCompositionIds.ResidentialWardDistrictId;
            if (definitionId.StartsWith("facility.commercial.",
                    StringComparison.Ordinal) ||
                definitionId.StartsWith("facility.industry.",
                    StringComparison.Ordinal) ||
                definitionId.StartsWith("facility.storage.",
                    StringComparison.Ordinal) ||
                definitionId == "facility.public.granary" ||
                definitionId == "facility.service.inn")
                return LuoyangWholeCityCompositionIds.MarketWorkshopDistrictId;
            return LuoyangWholeCityCompositionIds.PalaceCivicDistrictId;
        }

        private static string ResolveSurfaceProfileId(string definitionId,
            string districtId)
        {
            if (definitionId == "facility.public.road")
                return "surface.luoyang.rammed-earth-road.v1";
            if (definitionId == "facility.public.canal" ||
                definitionId == "facility.public.bridge" ||
                definitionId == "facility.public.well")
                return "surface.luoyang.water-infrastructure.v1";
            if (districtId == LuoyangWholeCityCompositionIds.DefenseDistrictId)
                return "surface.luoyang.fortification-earthwork.v1";
            if (districtId == LuoyangWholeCityCompositionIds
                    .AgriculturalResourceDistrictId)
                return "surface.luoyang.field-and-resource-ground.v1";
            if (districtId == LuoyangWholeCityCompositionIds
                    .PalaceCivicDistrictId)
                return "surface.luoyang.civic-courtyard.v1";
            return "surface.luoyang.urban-compacted-earth.v1";
        }

        private static bool IsCorridor(string definitionId) =>
            definitionId == "facility.public.road" ||
            definitionId == "facility.public.canal" ||
            definitionId == "facility.fortification.city_wall" ||
            definitionId == "facility.fortification.palace_wall" ||
            definitionId == "facility.military.wall";

        private static string CorridorFamily(string definitionId)
        {
            if (definitionId == "facility.public.road") return "road";
            if (definitionId == "facility.public.canal") return "canal";
            return "wall";
        }

        private static AnchorConnection ResolveCorridor(
            LuoyangBuildingPerformanceFacility facility,
            ISet<long> corridorCells)
        {
            var north = corridorCells.Contains(CellKey(facility.GridRow - 1,
                facility.GridColumn));
            var south = corridorCells.Contains(CellKey(facility.GridRow + 1,
                facility.GridColumn));
            var west = corridorCells.Contains(CellKey(facility.GridRow,
                facility.GridColumn - 1));
            var east = corridorCells.Contains(CellKey(facility.GridRow,
                facility.GridColumn + 1));
            var vertical = north || south;
            var horizontal = east || west;
            var degree = (north ? 1 : 0) + (south ? 1 : 0) +
                         (east ? 1 : 0) + (west ? 1 : 0);
            var shape = degree >= 4 ? "cross" : degree == 3 ? "junction" :
                degree == 2 && vertical && horizontal ? "corner" :
                degree == 2 ? "straight" : degree == 1 ? "end" : "isolated";
            var rotation = horizontal && !vertical ? 90f :
                vertical ? 0f : facility.RotationDegrees;
            return new AnchorConnection(0f, 0f, rotation,
                "connection.luoyang.corridor." + shape + ".v1");
        }

        private static AnchorConnection ResolveFrontage(
            LuoyangBuildingPerformanceFacility facility,
            IReadOnlyList<LuoyangBuildingPerformanceFacility> roads)
        {
            if (roads.Count == 0)
                return new AnchorConnection(0f, 0f,
                    facility.RotationDegrees,
                    "connection.luoyang.frontage.unresolved.v1");
            LuoyangBuildingPerformanceFacility nearest = null;
            var nearestDistance = int.MaxValue;
            foreach (var road in roads)
            {
                var distance = Math.Abs(road.GridColumn - facility.GridColumn) +
                               Math.Abs(road.GridRow - facility.GridRow);
                if (distance < nearestDistance ||
                    distance == nearestDistance &&
                    (nearest == null || road.CellId64 < nearest.CellId64))
                {
                    nearest = road;
                    nearestDistance = distance;
                }
            }
            var deltaColumn = nearest.GridColumn - facility.GridColumn;
            var deltaRow = nearest.GridRow - facility.GridRow;
            if (Math.Abs(deltaColumn) >= Math.Abs(deltaRow) && deltaColumn != 0)
            {
                var east = deltaColumn > 0;
                return new AnchorConnection(
                    east ? LuoyangWholeCityCompositionIds.FrontageOffsetMetres :
                        -LuoyangWholeCityCompositionIds.FrontageOffsetMetres,
                    0f, east ? 90f : 270f,
                    "connection.luoyang.frontage.nearest-road.v1");
            }
            if (deltaRow != 0)
            {
                var north = deltaRow < 0;
                return new AnchorConnection(0f,
                    north ? LuoyangWholeCityCompositionIds.FrontageOffsetMetres :
                        -LuoyangWholeCityCompositionIds.FrontageOffsetMetres,
                    north ? 0f : 180f,
                    "connection.luoyang.frontage.nearest-road.v1");
            }
            return new AnchorConnection(0f, 0f, facility.RotationDegrees,
                "connection.luoyang.frontage.on-road-cell.v1");
        }

        private static float ResolveScale(string districtId, string facilityId)
        {
            var baseline = districtId ==
                           LuoyangWholeCityCompositionIds.PalaceCivicDistrictId
                ? 1.10f
                : districtId == LuoyangWholeCityCompositionIds.DefenseDistrictId
                    ? 1.06f
                    : districtId == LuoyangWholeCityCompositionIds
                        .AgriculturalResourceDistrictId
                        ? 0.92f
                        : 0.98f;
            var variation = ((int)(StableHash(facilityId) % 5) - 2) * 0.01f;
            return (float)Math.Round(baseline + variation, 2);
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        private static long CellKey(int row, int column) =>
            ((long)row << 32) | (uint)column;

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private readonly struct AnchorConnection
        {
            public AnchorConnection(float localEastMetres,
                float localNorthMetres, float rotationDegrees,
                string profileId)
            {
                LocalEastMetres = localEastMetres;
                LocalNorthMetres = localNorthMetres;
                RotationDegrees = rotationDegrees;
                ProfileId = profileId;
            }

            public float LocalEastMetres { get; }
            public float LocalNorthMetres { get; }
            public float RotationDegrees { get; }
            public string ProfileId { get; }
        }
    }
}
