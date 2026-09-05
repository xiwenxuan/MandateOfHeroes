using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum CountySpatialLoadLevel : byte
    {
        Cold,
        Warm,
        Hot
    }

    public sealed class CountySpatialCacheHandle
    {
        public CountySpatialCacheHandle(string countyId,
            CountySpatialLoadLevel level, int residentPlanningCellCount,
            int residentChunkCount, int indexedFacilityCount,
            int residentPortalCount, string sourceSpatialHash,
            double buildMilliseconds, long managedAllocationBytes)
        {
            CountyId = countyId;
            Level = level;
            ResidentPlanningCellCount = residentPlanningCellCount;
            ResidentChunkCount = residentChunkCount;
            IndexedFacilityCount = indexedFacilityCount;
            ResidentPortalCount = residentPortalCount;
            SourceSpatialHash = sourceSpatialHash;
            BuildMilliseconds = buildMilliseconds;
            ManagedAllocationBytes = managedAllocationBytes;
        }

        public string CountyId { get; }
        public CountySpatialLoadLevel Level { get; }
        public int ResidentPlanningCellCount { get; }
        public int ResidentChunkCount { get; }
        public int IndexedFacilityCount { get; }
        public int ResidentPortalCount { get; }
        public string SourceSpatialHash { get; }
        public double BuildMilliseconds { get; }
        public long ManagedAllocationBytes { get; }
    }

    public sealed class CountySpatialLoadCoordinator
    {
        private readonly Dictionary<string, CountySpatialCacheHandle> _handles =
            new Dictionary<string, CountySpatialCacheHandle>(
                StringComparer.Ordinal);
        private readonly DualScaleCoordinateProjection _projection;

        public CountySpatialLoadCoordinator(
            DualScaleCoordinateProjection projection)
        {
            _projection = projection ?? throw new ArgumentNullException(
                nameof(projection));
        }

        public CountySpatialCacheHandle SetLevel(
            CountySpatialPartition partition, CountySpatialLoadLevel level)
        {
            if (partition == null)
                throw new ArgumentNullException(nameof(partition));
            var sourceHash = partition.ComputeSpatialHash();
            var memoryBefore = GC.GetTotalMemory(false);
            var timer = Stopwatch.StartNew();
            var indexedFacilities = 0;
            if (level == CountySpatialLoadLevel.Hot)
            {
                foreach (var placement in partition.FacilityPlacements
                             .OrderBy(item => item.Key,
                                 StringComparer.Ordinal))
                {
                    _ = placement.Value.ResolveCoveredPlanningCells(
                        _projection).Count;
                    indexedFacilities++;
                }
            }
            else if (level == CountySpatialLoadLevel.Warm)
            {
                indexedFacilities = partition.FacilityPlacements.Count(item =>
                    item.Value.StructureHeightCentimetres >= 1_000);
            }
            timer.Stop();
            var handle = new CountySpatialCacheHandle(partition.CountyId,
                level,
                level == CountySpatialLoadLevel.Hot
                    ? partition.PlanningCellCount : 0,
                level == CountySpatialLoadLevel.Hot
                    ? partition.ChunkCount : 0,
                indexedFacilities,
                level == CountySpatialLoadLevel.Cold
                    ? 0 : partition.Portals.Count,
                sourceHash,
                timer.Elapsed.TotalMilliseconds,
                Math.Max(0, GC.GetTotalMemory(false) - memoryBefore));
            _handles[partition.CountyId] = handle;
            if (!string.Equals(sourceHash, partition.ComputeSpatialHash(),
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Loading a county changed its authoritative spatial state.");
            return handle;
        }

        public CountySpatialCacheHandle Get(string countyId) =>
            _handles.TryGetValue(new StableId(countyId).Value,
                out var value) ? value : null;

        public CountySpatialCacheHandle Unload(
            CountySpatialPartition partition) =>
            SetLevel(partition, CountySpatialLoadLevel.Cold);
    }

    public sealed class PersonSpatialTransitionServiceV1
    {
        public PersonSpatialStateV1 EnterFacility(PersonState person,
            PersonSpatialStateV1 current,
            FacilitySpatialPlacement placement, string entranceId)
        {
            ValidatePerson(person, current);
            if (current.Mode != PersonSpatialModeV1.CountyLocal ||
                !string.Equals(current.CountyId, placement.CountyId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A Person must be local to enter this Facility.");
            var entrance = placement.Entrance(entranceId);
            if (DistanceSquared(current.LocalPosition, entrance.Position) >
                0.01d)
                throw new InvalidOperationException(
                    "A Person must reach the physical Entrance first.");
            person.CurrentFacilityId = placement.FacilityId;
            person.LocationPrecisionId =
                "person-location.inside-facility.v1";
            return PersonSpatialStateV1.InsideFacility(person.Id,
                placement.FacilityId);
        }

        public PersonSpatialStateV1 ExitFacility(PersonState person,
            PersonSpatialStateV1 current,
            FacilitySpatialPlacement placement, string entranceId)
        {
            ValidatePerson(person, current);
            if (current.Mode != PersonSpatialModeV1.InsideFacility ||
                !string.Equals(current.FacilityId, placement.FacilityId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A Person is not inside this Facility.");
            var entrance = placement.Entrance(entranceId);
            person.CurrentFacilityId = string.Empty;
            person.LocationPrecisionId = "person-location.county-local.v1";
            WriteFormalCountyLocalPosition(person, placement.CountyId,
                entrance.Position, new DualScaleCoordinateProjection());
            return PersonSpatialStateV1.CountyLocal(person.Id,
                placement.CountyId, entrance.Position);
        }

        public PersonSpatialStateV1 BeginStrategicTransit(PersonState person,
            PersonSpatialStateV1 current, CountyPortalSpatialState portal,
            string segmentId, DualScaleCoordinateProjection projection)
        {
            ValidatePerson(person, current);
            if (current.Mode != PersonSpatialModeV1.CountyLocal ||
                !string.Equals(current.CountyId, portal.CountyId,
                    StringComparison.Ordinal) ||
                !projection.ToPlanningCell(current.LocalPosition)
                    .Equals(portal.Cell))
                throw new InvalidOperationException(
                    "A Person must reach the matching CountyPortal.");
            person.CurrentFacilityId = string.Empty;
            person.LocationPrecisionId =
                "person-location.strategic-transit.v1";
            return PersonSpatialStateV1.StrategicTransit(person.Id,
                portal.RouteId, segmentId, 0);
        }

        public PersonSpatialStateV1 ArriveFromStrategicTransit(
            PersonState person, PersonSpatialStateV1 current,
            CountyPortalSpatialState destination,
            DualScaleCoordinateProjection projection)
        {
            ValidatePerson(person, current);
            if (current.Mode != PersonSpatialModeV1.StrategicTransit ||
                !string.Equals(current.RouteId, destination.RouteId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Strategic transit does not match the destination Portal.");
            var position = projection.PlanningCellCenter(destination.Cell);
            person.LocationPrecisionId = "person-location.county-local.v1";
            WriteFormalCountyLocalPosition(person, destination.CountyId,
                position, projection);
            return PersonSpatialStateV1.CountyLocal(person.Id,
                destination.CountyId, position);
        }

        private static void ValidatePerson(PersonState person,
            PersonSpatialStateV1 current)
        {
            if (person == null || current == null || !string.Equals(
                    person.Id, current.PersonId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Person spatial identity mismatch.");
        }

        private static double DistanceSquared(GlobalProjectedCoordinate first,
            GlobalProjectedCoordinate second)
        {
            var east = first.EastingMetres - second.EastingMetres;
            var north = first.NorthingMetres - second.NorthingMetres;
            return east * east + north * north;
        }

        private static void WriteFormalCountyLocalPosition(PersonState person,
            string countyId, GlobalProjectedCoordinate position,
            DualScaleCoordinateProjection projection)
        {
            var tile = projection.ToStrategicTile(position);
            projection.StrategicGrid.GetCenter(tile.Row, tile.Column,
                out var centerEasting, out var centerNorthing);
            var half = DualScaleCountySpatialContractV1
                .StrategicTileSizeMetres * 0.5d;
            person.LocationId = countyId;
            person.CurrentCellId64 = projection.StrategicCellId(tile).Value;
            person.CurrentLocalEastCentimetres = checked((int)Math.Round(
                (position.EastingMetres - (centerEasting - half)) * 100d));
            person.CurrentLocalNorthCentimetres = checked((int)Math.Round(
                ((centerNorthing + half) - position.NorthingMetres) * 100d));
        }
    }

    public sealed class ArmySpatialTransitionServiceV1
    {
        public ArmySpatialStateV1 Materialize(ArmyState army,
            ArmySpatialStateV1 current, CountyPortalSpatialState portal,
            DualScaleCoordinateProjection projection)
        {
            ValidateArmy(army, current);
            if (current.Mode != ArmySpatialModeV1.Strategic ||
                !string.Equals(current.RouteId, portal.RouteId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Army strategic position does not match the Portal.");
            return ArmySpatialStateV1.CountyMaterialized(army.Id,
                portal.CountyId,
                projection.PlanningCellCenter(portal.Cell));
        }

        public ArmySpatialStateV1 ReturnToStrategic(ArmyState army,
            ArmySpatialStateV1 current, CountyPortalSpatialState portal)
        {
            ValidateArmy(army, current);
            if (current.Mode != ArmySpatialModeV1.CountyMaterialized ||
                !string.Equals(current.CountyId, portal.CountyId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Army is not materialized in this County.");
            return ArmySpatialStateV1.Strategic(army.Id, portal.RouteId);
        }

        private static void ValidateArmy(ArmyState army,
            ArmySpatialStateV1 current)
        {
            if (army == null || current == null || !string.Equals(army.Id,
                    current.ArmyId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Army spatial identity mismatch.");
        }
    }

    public sealed class DualScaleSpatialValidationScenario
    {
        public DualScaleSpatialValidationScenario(WorldState world,
            DualScaleCoordinateProjection projection,
            CountySpatialPartition westCounty,
            CountySpatialPartition eastCounty,
            WorldRouteSpatialStateV1 route,
            PersonSpatialStateV1 personSpatial,
            ArmySpatialStateV1 armySpatial,
            FacilityDefenseStateV1 arrowTowerDefense)
        {
            World = world;
            Projection = projection;
            WestCounty = westCounty;
            EastCounty = eastCounty;
            Route = route;
            PersonSpatial = personSpatial;
            ArmySpatial = armySpatial;
            ArrowTowerDefense = arrowTowerDefense ??
                throw new ArgumentNullException(nameof(arrowTowerDefense));
        }

        public WorldState World { get; }
        public DualScaleCoordinateProjection Projection { get; }
        public CountySpatialPartition WestCounty { get; }
        public CountySpatialPartition EastCounty { get; }
        public WorldRouteSpatialStateV1 Route { get; }
        public PersonSpatialStateV1 PersonSpatial { get; set; }
        public ArmySpatialStateV1 ArmySpatial { get; set; }
        public FacilityDefenseStateV1 ArrowTowerDefense { get; }
        public int PlanningCellCount => WestCounty.PlanningCellCount +
                                        EastCounty.PlanningCellCount;

        public FacilityState Facility(string id) => World.Facilities.Single(
            item => string.Equals(item.Id, id, StringComparison.Ordinal));

        public FacilitySpatialPlacement Placement(string id)
        {
            if (WestCounty.FacilityPlacements.TryGetValue(id, out var west))
                return west;
            return EastCounty.FacilityPlacements[id];
        }
    }

    public static class DualScaleSpatialValidationScenarioFactory
    {
        public const string WestCountyId = "county.validation.west.v1";
        public const string EastCountyId = "county.validation.east.v1";
        public const string RouteId = "route.validation.dual-scale.v1";
        public const string PersonId = "person.validation.dual-scale.v1";
        public const string ArmyId = "army.validation.dual-scale.v1";
        public const string HouseFacilityId =
            "facility.validation.residence.v1";
        public const string StorehouseFacilityId =
            "facility.validation.storehouse.v1";
        public const string ArrowTowerFacilityId =
            "facility.validation.arrow-tower.v1";
        public const string WatchtowerFacilityId =
            "facility.validation.watchtower.v1";
        public const string SiegePlatformFacilityId =
            "facility.validation.siege-platform.v1";

        public static DualScaleSpatialValidationScenario Create()
        {
            var projection = new DualScaleCoordinateProjection();
            var strategicMinimum = new StrategicTileCoord(1240, 2042);
            var planningMinimum = projection.StrategicTileMinimum(
                strategicMinimum);
            var west = new CountySpatialPartition(WestCountyId,
                planningMinimum, 80, 40);
            var east = new CountySpatialPartition(EastCountyId,
                new PlanningCellCoord(planningMinimum.Row,
                    planningMinimum.Column + 40), 80, 40);
            PopulateTerrain(west, 0);
            PopulateTerrain(east, 40);
            BuildRoad(west);
            BuildRoad(east);

            var world = new WorldState
            {
                MasterSeed = 0xD5A150UL,
                AbsoluteDay = 67,
                Segment = (byte)DaySegment.Day
            };
            world.Routes.Add(new RouteState
            {
                Id = RouteId,
                FromLocationId = WestCountyId,
                ToLocationId = EastCountyId,
                DistanceKilometers = 4,
                SecurityBasisPoints = 7_500
            });

            AddFacility(world, west, projection, HouseFacilityId,
                "民居院落", "facility.residential.courtyard.v1",
                52, 18, 2_000, 3_000, 0, 650);
            AddFacility(world, west, projection, StorehouseFacilityId,
                "跨格仓库", "facility.storage.warehouse.v1",
                49, 23, 7_000, 4_500, 1, 900);
            AddFacility(world, west, projection, ArrowTowerFacilityId,
                "箭塔", "facility.military.arrow-tower.v1",
                35, 30, 2_500, 2_500, 0, 1_800);
            AddFacility(world, west, projection, WatchtowerFacilityId,
                "瞭望台", "facility.military.watchtower.v1",
                31, 27, 2_000, 2_000, 0, 2_200);
            AddFacility(world, west, projection, SiegePlatformFacilityId,
                "攻城高台", "facility.military.siege-platform.v1",
                26, 15, 6_000, 5_000, 0, 3_000);

            BuildFortification(west);
            var westPortal = new CountyPortalSpatialState(
                "portal.validation.west-east.official-road.west.v1",
                RouteId, WestCountyId, EastCountyId,
                west.ToGlobalCell(40, 39),
                projection.ToStrategicTile(west.ToGlobalCell(40, 39)),
                "portal.passage.official-road.v1");
            var eastPortal = new CountyPortalSpatialState(
                "portal.validation.west-east.official-road.east.v1",
                RouteId, EastCountyId, WestCountyId,
                east.ToGlobalCell(40, 0),
                projection.ToStrategicTile(east.ToGlobalCell(40, 0)),
                "portal.passage.official-road.v1");
            west.AddPortal(westPortal);
            east.AddPortal(eastPortal);
            var route = new WorldRouteSpatialStateV1(RouteId,
                new[] { westPortal, eastPortal });

            var houseEntrance = west.FacilityPlacements[HouseFacilityId]
                .Entrances[0].Position;
            var person = new PersonState
            {
                Id = PersonId,
                DisplayName = "验证人物",
                LocationId = WestCountyId,
                CurrentCellId64 = projection.StrategicCellId(
                    projection.ToStrategicTile(
                        projection.ToPlanningCell(houseEntrance))).Value,
                LocationPrecisionId = "person-location.county-local.v1"
            };
            world.People.Add(person);
            world.PlayerPersonId = person.Id;
            world.Inventories.Add(new InventoryStackState
            {
                Id = "inventory.validation.person.v1",
                OwnerPersonId = person.Id,
                CommodityId = "commodity.validation.grain.v1",
                Quantity = 20,
                AverageUnitCost = 2
            });
            var army = new ArmyState
            {
                Id = ArmyId,
                DisplayName = "验证军队",
                OrganizationId = "organization.validation.defender.v1",
                CommanderPersonId = person.Id,
                LocationId = WestCountyId,
                Troops = 100,
                MaximumTroops = 100,
                Provisions = 500
            };
            world.Armies.Add(army);

            return new DualScaleSpatialValidationScenario(world, projection,
                west, east, route,
                PersonSpatialStateV1.CountyLocal(person.Id, WestCountyId,
                    houseEntrance),
                ArmySpatialStateV1.Strategic(army.Id, RouteId),
                new FacilityDefenseStateV1(ArrowTowerFacilityId, 24, 8_500));
        }

        private static void PopulateTerrain(CountySpatialPartition partition,
            int overallColumnOffset)
        {
            for (var row = 0; row < partition.Rows; row++)
            for (var column = 0; column < partition.Columns; column++)
            {
                var overallColumn = overallColumnOffset + column;
                var hillDistance = Math.Abs(overallColumn - 13) +
                                   Math.Abs(row - 22);
                var hill = Math.Max(0, 18 - hillDistance);
                var elevation = checked((ushort)(1_000 + hill * 6));
                partition.SetCell(row, column, elevation,
                    hill > 2 ? PlanningTerrainClass.Hill :
                    PlanningTerrainClass.Plains,
                    checked((byte)Math.Min(255, hill * 8)), true,
                    PlanningLandUseClass.Unassigned, 0, 0);
            }
        }

        private static void BuildRoad(CountySpatialPartition partition)
        {
            const int roadRow = 40;
            for (var column = 0; column < partition.Columns; column++)
                partition.SetLandUse(roadRow, column,
                    PlanningLandUseClass.Road);
            for (var column = 0; column < partition.Columns - 1; column++)
                partition.Connections.SetBetween(roadRow, column,
                    PlanningCellDirection.East,
                    PlanningCellConnectionKind.OpenByRoad);
        }

        private static void AddFacility(WorldState world,
            CountySpatialPartition partition,
            DualScaleCoordinateProjection projection, string id,
            string displayName, string definitionId,
            int localRow, int localColumn, int widthCentimetres,
            int depthCentimetres, int quarterTurns,
            int heightCentimetres)
        {
            var cell = partition.ToGlobalCell(localRow, localColumn);
            var center = projection.PlanningCellCenter(cell);
            var entrance = new FacilityEntranceSpatialState(
                id + ".entrance.south", new GlobalProjectedCoordinate(
                    center.EastingMetres,
                    center.NorthingMetres - depthCentimetres / 200d),
                PlanningCellDirection.South);
            var placement = new FacilitySpatialPlacement(id,
                partition.CountyId, center, widthCentimetres,
                depthCentimetres, quarterTurns, heightCentimetres,
                "collision.rectangle.solid.v1", new[] { entrance });
            partition.AddFacilityPlacement(placement);
            world.Facilities.Add(new FacilityState
            {
                Id = id,
                DisplayName = displayName,
                DefinitionId = definitionId,
                CellId64 = projection.StrategicCellId(
                    projection.ToStrategicTile(cell)).Value,
                OwnerId = "family.validation.owner.v1",
                ControllerId = "organization.validation.defender.v1",
                AdministrativeControllerId =
                    "organization.validation.defender.v1",
                SettlementId = partition.CountyId,
                ConditionBasisPoints = 10_000,
                LifecycleStatus = FacilityLifecycleStatus.Operational
            });
        }

        private static void BuildFortification(
            CountySpatialPartition west)
        {
            for (var row = 32; row <= 48; row++)
            {
                var edge = new PlanningCellEdge(
                    west.ToGlobalCell(row, 30),
                    PlanningCellDirection.East);
                var gate = row == 40;
                var segment = new FortificationSegmentSpatialState(
                    gate
                        ? "fortification.validation.gate.v1"
                        : $"fortification.validation.wall.{row:00}.v1",
                    west.CountyId,
                    gate ? "fortification.gate.rammed-earth.v1" :
                    "fortification.wall.rammed-earth.v1",
                    edge, gate, gate ? 900 : 1_200,
                    gate ? 500 : 350, 100,
                    "organization.validation.defender.v1",
                    "organization.validation.defender.v1",
                    gate ? 10 : 0);
                west.AddFortification(segment);
                west.Connections.SetBetween(row, 30,
                    PlanningCellDirection.East, segment.PassageKind);
            }
        }
    }

    public sealed class DualScaleWorldSummaryV1 :
        IEquatable<DualScaleWorldSummaryV1>
    {
        public string ProductionSummary { get; private set; }
        public string InventorySummary { get; private set; }
        public string PersonSummary { get; private set; }
        public string FacilitySummary { get; private set; }
        public string WorldSummary { get; private set; }

        public static DualScaleWorldSummaryV1 Create(WorldState world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var result = new DualScaleWorldSummaryV1
            {
                ProductionSummary = Hash(string.Join("|", world.Facilities
                    .OrderBy(item => item.Id, StringComparer.Ordinal)
                    .Select(item => $"{item.Id}:{item.WorkerPersonCount}:" +
                                    $"{item.LifecycleStatus}"))),
                InventorySummary = Hash(string.Join("|", world.Inventories
                    .OrderBy(item => item.Id, StringComparer.Ordinal)
                    .Select(item => $"{item.Id}:{item.Quantity}:" +
                                    $"{item.AverageUnitCost}"))),
                PersonSummary = Hash(string.Join("|", world.People
                    .OrderBy(item => item.Id, StringComparer.Ordinal)
                    .Select(item => $"{item.Id}:{item.IsAlive}:" +
                                    $"{item.Wealth}:{item.LocationId}:" +
                                    $"{item.CurrentFacilityId}"))),
                FacilitySummary = Hash(string.Join("|", world.Facilities
                    .OrderBy(item => item.Id, StringComparer.Ordinal)
                    .Select(item => $"{item.Id}:{item.OwnerId}:" +
                                    $"{item.ControllerId}:" +
                                    $"{item.ConditionBasisPoints}")))
            };
            result.WorldSummary = Hash($"{world.SchemaVersion}|" +
                $"{world.AbsoluteDay}|{world.Segment}|" +
                $"{result.ProductionSummary}|{result.InventorySummary}|" +
                $"{result.PersonSummary}|{result.FacilitySummary}");
            return result;
        }

        public bool Equals(DualScaleWorldSummaryV1 other) =>
            other != null &&
            string.Equals(ProductionSummary, other.ProductionSummary,
                StringComparison.Ordinal) &&
            string.Equals(InventorySummary, other.InventorySummary,
                StringComparison.Ordinal) &&
            string.Equals(PersonSummary, other.PersonSummary,
                StringComparison.Ordinal) &&
            string.Equals(FacilitySummary, other.FacilitySummary,
                StringComparison.Ordinal) &&
            string.Equals(WorldSummary, other.WorldSummary,
                StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            Equals(obj as DualScaleWorldSummaryV1);

        public override int GetHashCode() =>
            WorldSummary == null ? 0 : WorldSummary.GetHashCode();

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(
                        value ?? string.Empty))
                    .Select(item => item.ToString("x2")));
        }
    }
}
