using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class PlanningCellInspection
    {
        public PlanningCellInspection(string countyId,
            PlanningCellCoord globalCell, int localRow, int localColumn,
            ushort elevationDecimetres, PlanningTerrainClass terrain,
            byte slopeBasis, bool buildable, PlanningLandUseClass landUse,
            byte waterState, byte irrigationState,
            IReadOnlyList<PlanningCellConnectionKind> fourPorts,
            IReadOnlyList<string> facilityIds,
            IReadOnlyList<string> fortificationIds,
            string portalId, int nearestRoadDistanceCentimetres)
        {
            CountyId = countyId;
            GlobalCell = globalCell;
            LocalRow = localRow;
            LocalColumn = localColumn;
            ElevationDecimetres = elevationDecimetres;
            Terrain = terrain;
            SlopeBasis = slopeBasis;
            Buildable = buildable;
            LandUse = landUse;
            WaterState = waterState;
            IrrigationState = irrigationState;
            FourPorts = fourPorts;
            FacilityIds = facilityIds;
            FortificationIds = fortificationIds;
            PortalId = portalId ?? string.Empty;
            NearestRoadDistanceCentimetres = nearestRoadDistanceCentimetres;
        }

        public string CountyId { get; }
        public PlanningCellCoord GlobalCell { get; }
        public int LocalRow { get; }
        public int LocalColumn { get; }
        public ushort ElevationDecimetres { get; }
        public PlanningTerrainClass Terrain { get; }
        public byte SlopeBasis { get; }
        public bool Buildable { get; }
        public PlanningLandUseClass LandUse { get; }
        public byte WaterState { get; }
        public byte IrrigationState { get; }
        public IReadOnlyList<PlanningCellConnectionKind> FourPorts { get; }
        public IReadOnlyList<string> FacilityIds { get; }
        public IReadOnlyList<string> FortificationIds { get; }
        public string PortalId { get; }
        public int NearestRoadDistanceCentimetres { get; }
    }

    public sealed class FacilityPlacementValidator
    {
        private sealed class ExistingFacilityBox
        {
            public string Id;
            public PlanningMetricBounds Bounds;
        }

        private sealed class WallLine
        {
            public string Id;
            public GlobalProjectedCoordinate First;
            public GlobalProjectedCoordinate Second;
        }

        private readonly CountySpatialPartition _partition;
        private readonly DualScaleCoordinateProjection _projection;
        private readonly Dictionary<int, List<ExistingFacilityBox>>
            _facilitiesByCell =
                new Dictionary<int, List<ExistingFacilityBox>>();
        private readonly Dictionary<int, List<WallLine>> _wallsByCell =
            new Dictionary<int, List<WallLine>>();
        private readonly HashSet<int> _roadCells = new HashSet<int>();
        private readonly HashSet<int> _waterCells = new HashSet<int>();
        private readonly Dictionary<int, string> _portalsByCell =
            new Dictionary<int, string>();
        private readonly Dictionary<int, List<DraftBuildingBlueprint>>
            _draftsByCell =
                new Dictionary<int, List<DraftBuildingBlueprint>>();
        private readonly Dictionary<int, List<string>>
            _linearDraftIdsByCell = new Dictionary<int, List<string>>();
        private CountyPlanningSession _indexedSession;
        private int _indexedSessionVersion = -1;

        public FacilityPlacementValidator(
            Luoyang50mCountySpatialPrototype prototype,
            Luoyang50mCountyLayoutPackage layoutPackage,
            DualScaleCoordinateProjection projection = null)
        {
            if (prototype == null)
                throw new ArgumentNullException(nameof(prototype));
            if (layoutPackage == null)
                throw new ArgumentNullException(nameof(layoutPackage));
            if (!string.Equals(prototype.Partition.CountyId,
                    layoutPackage.CountyId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Planning layout and partition county mismatch.");
            _partition = prototype.Partition;
            _projection = projection ?? new DualScaleCoordinateProjection();
            BuildStaticIndices();
        }

        public string CountyId => _partition.CountyId;
        public int IndexedFacilityCount { get; private set; }
        public int IndexedRoadCellCount => _roadCells.Count;
        public int IndexedWaterCellCount => _waterCells.Count;
        public int IndexedFortificationCount { get; private set; }
        public int LastFacilityCandidateCount { get; private set; }
        public int LastRoadCandidateCount { get; private set; }

        public PlanningFacilityFootprint CreateFootprint(
            FacilityPlacementProfile profile, int localRow, int localColumn,
            int rotationQuarterTurns)
        {
            var global = new PlanningCellCoord(
                _partition.MinimumCell.Row + localRow,
                _partition.MinimumCell.Column + localColumn);
            return new PlanningFacilityFootprint(profile,
                _projection.PlanningCellCenter(global),
                rotationQuarterTurns);
        }

        public bool TryPickPlanningCell(GlobalProjectedCoordinate position,
            out PlanningCellCoord globalCell, out int localRow,
            out int localColumn)
        {
            try
            {
                globalCell = _projection.ToPlanningCell(position);
                return _partition.TryToLocal(globalCell, out localRow,
                    out localColumn);
            }
            catch (ArgumentOutOfRangeException)
            {
                globalCell = default;
                localRow = -1;
                localColumn = -1;
                return false;
            }
        }

        public PlanningCellInspection InspectCell(int localRow,
            int localColumn)
        {
            var global = _partition.ToGlobalCell(localRow, localColumn);
            var key = Key(localRow, localColumn);
            var facilities = _facilitiesByCell.TryGetValue(key,
                    out var boxes)
                ? boxes.Select(value => value.Id).Distinct(
                    StringComparer.Ordinal).OrderBy(value => value,
                        StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
            var walls = _wallsByCell.TryGetValue(key, out var lines)
                ? lines.Select(value => value.Id).Distinct(
                    StringComparer.Ordinal).OrderBy(value => value,
                        StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
            var center = _projection.PlanningCellCenter(global);
            var nearest = FindNearestRoad(center, 500d);
            return new PlanningCellInspection(CountyId, global, localRow,
                localColumn,
                _partition.GroundElevationDecimetres(localRow, localColumn),
                _partition.Terrain(localRow, localColumn),
                _partition.SlopeBasis(localRow, localColumn),
                _partition.IsBuildable(localRow, localColumn),
                _partition.LandUse(localRow, localColumn),
                _partition.WaterState(localRow, localColumn),
                _partition.IrrigationState(localRow, localColumn),
                Enumerable.Range(0, 4).Select(direction =>
                    _partition.Connections.Get(localRow, localColumn,
                        (PlanningCellDirection)direction)).ToArray(),
                facilities, walls,
                _portalsByCell.TryGetValue(key, out var portal)
                    ? portal : string.Empty,
                nearest.HasValue
                    ? checked((int)Math.Round(nearest.Value.Distance * 100d))
                    : -1);
        }

        public PlacementValidationResult Validate(
            FacilityPlacementProfile profile, int localRow,
            int localColumn, int rotationQuarterTurns,
            CountyPlanningSession session = null,
            string ignoredDraftId = null)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (session != null && !string.Equals(session.CountyId, CountyId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Draft session belongs to another county.");
            EnsureDraftIndex(session);
            var footprint = CreateFootprint(profile, localRow, localColumn,
                rotationQuarterTurns);
            return Validate(footprint, session, ignoredDraftId);
        }

        public PlacementValidationResult Validate(
            PlanningFacilityFootprint footprint,
            CountyPlanningSession session = null,
            string ignoredDraftId = null)
        {
            if (footprint == null)
                throw new ArgumentNullException(nameof(footprint));
            EnsureDraftIndex(session);
            var profile = footprint.Profile;
            var blocking = new List<PlacementIssue>();
            var warnings = new List<PlacementIssue>();
            var collisions = new HashSet<string>(StringComparer.Ordinal);
            var covered = ResolveCoveredCells(footprint.Bounds);
            ushort minimumElevation = ushort.MaxValue;
            ushort maximumElevation = 0;
            byte maximumSlope = 0;
            var localCovered = new List<Tuple<PlanningCellCoord, int, int>>();
            foreach (var cell in covered)
            {
                if (!_partition.TryToLocal(cell, out var row, out var column))
                {
                    Add(blocking, PlacementReasonIds.OutsideCounty,
                        "建筑实体范围超出当前洛阳县域。", 10);
                    continue;
                }
                localCovered.Add(Tuple.Create(cell, row, column));
                var elevation = _partition.GroundElevationDecimetres(row,
                    column);
                minimumElevation = (ushort)Math.Min(minimumElevation,
                    elevation);
                maximumElevation = (ushort)Math.Max(maximumElevation,
                    elevation);
                var slope = _partition.SlopeBasis(row, column);
                maximumSlope = (byte)Math.Max(maximumSlope, slope);
                var terrain = _partition.Terrain(row, column);
                if (!_partition.IsBuildable(row, column))
                    Add(blocking, PlacementReasonIds.CellNotBuildable,
                        "所覆盖地块包含不可建设 Cell。", 20);
                if (!profile.AllowedTerrain.Contains(terrain) ||
                    profile.ForbiddenTerrain.Contains(terrain))
                    Add(blocking, PlacementReasonIds.TerrainForbidden,
                        "所覆盖地块地形不允许该类建筑落位。", 30);
                if (slope > profile.MaximumSlopeBasis)
                    Add(blocking, PlacementReasonIds.SlopeTooSteep,
                        "所覆盖地块坡度超过建筑上限。", 40);
                if (_waterCells.Contains(Key(row, column)) &&
                    !profile.AllowWaterOverlap)
                    Add(blocking, PlacementReasonIds.WaterOverlap,
                        "建筑实体与河渠或水体重叠。", 50);
                if (_roadCells.Contains(Key(row, column)))
                    Add(blocking, PlacementReasonIds.RoadOverlap,
                        "建筑实体占用了既有道路。", 60);
                if (_portalsByCell.TryGetValue(Key(row, column),
                        out var portalId))
                {
                    collisions.Add(portalId);
                    Add(blocking, PlacementReasonIds.PortalCorridorOverlap,
                        "建筑实体占用了县域边界通道。", 70,
                        new[] { portalId });
                }
            }

            if (maximumSlope > profile.MaximumSlopeBasis)
                Add(blocking, PlacementReasonIds.SlopeTooSteep,
                    "建筑实体范围内最大坡度超过上限。", 40);

            var expanded = footprint.Bounds.Expand(
                profile.RequiredClearanceCentimetres / 100d);
            LastFacilityCandidateCount = 0;
            foreach (var box in QueryFacilities(expanded))
            {
                LastFacilityCandidateCount++;
                if (!expanded.Intersects(box.Bounds)) continue;
                collisions.Add(box.Id);
                if (!profile.AllowExistingFacilityOverlap)
                    Add(blocking,
                        PlacementReasonIds.ExistingFacilityCollision,
                        "建筑实体或净距与既有 Facility 冲突。", 80,
                        new[] { box.Id });
            }

            foreach (var wall in QueryWalls(expanded))
            {
                if (!SegmentIntersectsBounds(wall.First, wall.Second,
                        expanded)) continue;
                collisions.Add(wall.Id);
                if (!profile.AllowFortificationOverlap)
                    Add(blocking, PlacementReasonIds.FortificationOverlap,
                        "建筑实体或净距与城墙/城门边界冲突。", 75,
                        new[] { wall.Id });
            }

            if (session != null)
            {
                foreach (var draft in QueryDrafts(expanded))
                {
                    if (!string.IsNullOrWhiteSpace(ignoredDraftId) &&
                        string.Equals(draft.DraftId, ignoredDraftId,
                            StringComparison.Ordinal)) continue;
                    if (!expanded.Intersects(draft.Bounds)) continue;
                    collisions.Add(draft.DraftId);
                    Add(blocking, PlacementReasonIds.DraftCollision,
                        "建筑草案与已有草案冲突。", 85,
                        new[] { draft.DraftId });
                }
                foreach (var cell in localCovered)
                {
                    var key = Key(cell.Item2, cell.Item3);
                    if (!_linearDraftIdsByCell.TryGetValue(key,
                            out var draftIds)) continue;
                    foreach (var draftId in draftIds.Distinct(
                                 StringComparer.Ordinal))
                    {
                        collisions.Add(draftId);
                        Add(blocking, PlacementReasonIds.DraftCollision,
                            "建筑草案与道路、城墙或水渠草案冲突。", 85,
                            new[] { draftId });
                    }
                }
            }

            var road = ValidateRoadAccess(footprint);
            if (road.Status != FacilityRoadAccessStatus.Connected &&
                road.Status != FacilityRoadAccessStatus.NotRequired)
            {
                var issue = RoadIssue(road.Status);
                if (profile.RoadAccessRequirement ==
                    FacilityRoadAccessRequirement.Required)
                    blocking.Add(issue);
                else if (profile.RoadAccessRequirement ==
                         FacilityRoadAccessRequirement.Optional)
                    warnings.Add(issue);
            }
            if (!profile.PlayerBuildable)
                Add(warnings,
                    PlacementReasonIds.MilitaryAuthorityRequired,
                    "该军用设施仅生成规划草案，正式建设仍需军政权限。", 200);
            else
                Add(warnings,
                    PlacementReasonIds.ConstructionPermissionDeferred,
                    "当前仅生成规划草案；用地、材料、劳力和审批在正式建设阶段结算。",
                    210);

            var state = blocking.Count > 0
                ? PlacementValidationState.Invalid
                : warnings.Any(value => value.Code ==
                    PlacementReasonIds.MilitaryAuthorityRequired)
                    ? PlacementValidationState.Conditional
                    : PlacementValidationState.Valid;
            return new PlacementValidationResult(state, blocking, warnings,
                covered, road, collisions.OrderBy(value => value,
                    StringComparer.Ordinal).ToArray(),
                minimumElevation == ushort.MaxValue
                    ? (ushort)0 : minimumElevation,
                maximumElevation, maximumSlope);
        }

        public FacilityRoadAccessResult ValidateRoadAccess(
            PlanningFacilityFootprint footprint)
        {
            var profile = footprint.Profile;
            var entrance = footprint.Entrances.Single(value => value.Primary);
            if (profile.RoadAccessRequirement ==
                    FacilityRoadAccessRequirement.None)
                return RoadResult(FacilityRoadAccessStatus.NotRequired,
                    entrance, null);
            var maximum = profile.MaximumEntranceToRoadDistanceCentimetres /
                100d;
            var nearest = FindNearestRoad(entrance.Position,
                Math.Max(300d, maximum * 4d));
            if (!nearest.HasValue)
                return RoadResult(FacilityRoadAccessStatus.NoRoad, entrance,
                    null);
            var candidate = nearest.Value;
            if (candidate.Distance > maximum)
                return RoadResult(FacilityRoadAccessStatus.TooFar, entrance,
                    candidate);
            var east = candidate.Connection.EastingMetres -
                       entrance.Position.EastingMetres;
            var north = candidate.Connection.NorthingMetres -
                        entrance.Position.NorthingMetres;
            DirectionVector(entrance.OutwardDirection, out var outwardEast,
                out var outwardNorth);
            if (east * outwardEast + north * outwardNorth < -0.01d)
                return RoadResult(FacilityRoadAccessStatus.WrongSide,
                    entrance, candidate);
            if (AccessPathBlocked(entrance.Position, candidate.Connection,
                    footprint.Bounds))
                return RoadResult(FacilityRoadAccessStatus.Blocked, entrance,
                    candidate);
            return RoadResult(FacilityRoadAccessStatus.Connected, entrance,
                candidate);
        }

        private void BuildStaticIndices()
        {
            foreach (var placement in _partition.FacilityPlacements.Values
                         .OrderBy(value => value.FacilityId,
                             StringComparer.Ordinal))
            {
                var box = new ExistingFacilityBox
                {
                    Id = placement.FacilityId,
                    Bounds = Bounds(placement)
                };
                foreach (var cell in ResolveCoveredCells(box.Bounds))
                {
                    if (!_partition.TryToLocal(cell, out var row,
                            out var column)) continue;
                    AddIndex(_facilitiesByCell, Key(row, column), box);
                }
                IndexedFacilityCount++;
            }
            for (var row = 0; row < _partition.Rows; row++)
            for (var column = 0; column < _partition.Columns; column++)
            {
                var key = Key(row, column);
                if (_partition.LandUse(row, column) ==
                    PlanningLandUseClass.Road)
                    _roadCells.Add(key);
                if (_partition.WaterState(row, column) > 0 ||
                    _partition.Terrain(row, column) ==
                    PlanningTerrainClass.Water)
                    _waterCells.Add(key);
            }
            foreach (var item in _partition.Fortifications.Values
                         .OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                var line = CreateWallLine(item);
                foreach (var cell in new[] { item.Edge.First,
                             item.Edge.Second })
                    if (_partition.TryToLocal(cell, out var row,
                            out var column))
                        AddIndex(_wallsByCell, Key(row, column), line);
                IndexedFortificationCount++;
            }
            foreach (var portal in _partition.Portals.Values)
                if (_partition.TryToLocal(portal.Cell,
                        out var row, out var column))
                    _portalsByCell[Key(row, column)] = portal.PortalId;
        }

        private void EnsureDraftIndex(CountyPlanningSession session)
        {
            if (ReferenceEquals(_indexedSession, session) &&
                (session == null || _indexedSessionVersion == session.Version))
                return;
            _draftsByCell.Clear();
            _linearDraftIdsByCell.Clear();
            _indexedSession = session;
            _indexedSessionVersion = session?.Version ?? -1;
            if (session == null) return;
            foreach (var draft in session.Drafts)
                foreach (var cell in ResolveCoveredCells(draft.Bounds))
                    if (_partition.TryToLocal(cell, out var row,
                            out var column))
                        AddIndex(_draftsByCell, Key(row, column), draft);
            foreach (var draft in session.RoadDrafts.Cast<CountyLinearDraft>()
                         .Concat(session.CanalDrafts))
                IndexLinearDraft(draft.DraftId, draft.Path);
            foreach (var draft in session.FortificationDrafts)
                IndexLinearDraft(draft.DraftId,
                    draft.Segments.Select(value => value.Cell));
        }

        private void IndexLinearDraft(string draftId,
            IEnumerable<PlanningCellCoord> cells)
        {
            foreach (var cell in cells.Distinct())
                if (_partition.TryToLocal(cell, out var row,
                        out var column))
                    AddIndex(_linearDraftIdsByCell, Key(row, column),
                        draftId);
        }

        private IEnumerable<ExistingFacilityBox> QueryFacilities(
            PlanningMetricBounds bounds) => QueryIndex(_facilitiesByCell,
                bounds).Distinct();

        private IEnumerable<WallLine> QueryWalls(
            PlanningMetricBounds bounds) => QueryIndex(_wallsByCell, bounds)
                .Distinct();

        private IEnumerable<DraftBuildingBlueprint> QueryDrafts(
            PlanningMetricBounds bounds) => QueryIndex(_draftsByCell, bounds)
                .Distinct();

        private IEnumerable<T> QueryIndex<T>(
            IReadOnlyDictionary<int, List<T>> index,
            PlanningMetricBounds bounds)
        {
            foreach (var cell in ResolveCoveredCells(bounds))
            {
                if (!_partition.TryToLocal(cell, out var row,
                        out var column) ||
                    !index.TryGetValue(Key(row, column), out var values))
                    continue;
                foreach (var value in values) yield return value;
            }
        }

        private IReadOnlyList<PlanningCellCoord> ResolveCoveredCells(
            PlanningMetricBounds bounds)
        {
            const double epsilon = 0.000001d;
            var northWest = _projection.ToPlanningCell(
                new GlobalProjectedCoordinate(bounds.MinimumEasting,
                    bounds.MaximumNorthing - epsilon));
            var southEast = _projection.ToPlanningCell(
                new GlobalProjectedCoordinate(bounds.MaximumEasting - epsilon,
                    bounds.MinimumNorthing + epsilon));
            var result = new List<PlanningCellCoord>();
            for (var row = northWest.Row; row <= southEast.Row; row++)
            for (var column = northWest.Column;
                 column <= southEast.Column; column++)
                result.Add(new PlanningCellCoord(row, column));
            return result;
        }

        private readonly struct RoadCandidate
        {
            public RoadCandidate(int row, int column, double distance,
                GlobalProjectedCoordinate connection)
            {
                Row = row;
                Column = column;
                Distance = distance;
                Connection = connection;
            }
            public int Row { get; }
            public int Column { get; }
            public double Distance { get; }
            public GlobalProjectedCoordinate Connection { get; }
            public string Id => "road.cell.luoyang.r" + Row.ToString("D3") +
                ".c" + Column.ToString("D3");
        }

        private RoadCandidate? FindNearestRoad(
            GlobalProjectedCoordinate point, double searchRadiusMetres)
        {
            if (!TryPickPlanningCell(point, out _, out var centerRow,
                    out var centerColumn)) return null;
            var radiusCells = Math.Max(1, (int)Math.Ceiling(
                searchRadiusMetres /
                DualScaleCountySpatialContractV1.PlanningCellSizeMetres));
            RoadCandidate? nearest = null;
            LastRoadCandidateCount = 0;
            for (var row = Math.Max(0, centerRow - radiusCells);
                 row <= Math.Min(_partition.Rows - 1,
                     centerRow + radiusCells); row++)
            for (var column = Math.Max(0, centerColumn - radiusCells);
                 column <= Math.Min(_partition.Columns - 1,
                     centerColumn + radiusCells); column++)
            {
                if (!_roadCells.Contains(Key(row, column))) continue;
                LastRoadCandidateCount++;
                var bounds = CellBounds(row, column);
                var connection = new GlobalProjectedCoordinate(
                    Clamp(point.EastingMetres, bounds.MinimumEasting,
                        bounds.MaximumEasting),
                    Clamp(point.NorthingMetres, bounds.MinimumNorthing,
                        bounds.MaximumNorthing));
                var distance = Distance(point, connection);
                var candidate = new RoadCandidate(row, column, distance,
                    connection);
                if (!nearest.HasValue || distance < nearest.Value.Distance -
                        0.000001d || Math.Abs(distance -
                        nearest.Value.Distance) <= 0.000001d &&
                    string.CompareOrdinal(candidate.Id,
                        nearest.Value.Id) < 0)
                    nearest = candidate;
            }
            return nearest;
        }

        private bool AccessPathBlocked(GlobalProjectedCoordinate first,
            GlobalProjectedCoordinate second,
            PlanningMetricBounds sourceFootprint)
        {
            foreach (var wall in QueryWalls(Bounds(first, second)))
                if (SegmentsIntersect(first, second, wall.First,
                        wall.Second)) return true;
            var distance = Distance(first, second);
            var steps = Math.Max(1, (int)Math.Ceiling(distance / 12.5d));
            for (var step = 1; step < steps; step++)
            {
                var t = step / (double)steps;
                var sample = new GlobalProjectedCoordinate(
                    first.EastingMetres + (second.EastingMetres -
                        first.EastingMetres) * t,
                    first.NorthingMetres + (second.NorthingMetres -
                        first.NorthingMetres) * t);
                if (!TryPickPlanningCell(sample, out _, out var row,
                        out var column)) return true;
                if (_waterCells.Contains(Key(row, column))) return true;
                foreach (var facility in QueryFacilities(
                             new PlanningMetricBounds(
                                 sample.EastingMetres - 0.1d,
                                 sample.EastingMetres + 0.1d,
                                 sample.NorthingMetres - 0.1d,
                                 sample.NorthingMetres + 0.1d)))
                    if (!sourceFootprint.Intersects(facility.Bounds) &&
                        facility.Bounds.Contains(sample)) return true;
            }
            return false;
        }

        private FacilityRoadAccessResult RoadResult(
            FacilityRoadAccessStatus status, PlanningFacilityEntrance entrance,
            RoadCandidate? candidate)
        {
            var accessCells = candidate.HasValue
                ? SampleAccessCells(entrance.Position,
                    candidate.Value.Connection)
                : Array.Empty<PlanningCellCoord>();
            return new FacilityRoadAccessResult(status,
                candidate?.Id ?? string.Empty,
                candidate.HasValue
                    ? LuoyangCountyPlanningIds.RoadClassGeneral
                    : string.Empty,
                candidate.HasValue
                    ? checked((int)Math.Round(candidate.Value.Distance * 100d))
                    : -1,
                entrance.Position,
                candidate?.Connection ?? entrance.Position, accessCells);
        }

        private IReadOnlyList<PlanningCellCoord> SampleAccessCells(
            GlobalProjectedCoordinate first,
            GlobalProjectedCoordinate second)
        {
            var result = new HashSet<PlanningCellCoord>();
            var steps = Math.Max(1, (int)Math.Ceiling(
                Distance(first, second) / 12.5d));
            for (var index = 0; index <= steps; index++)
            {
                var t = index / (double)steps;
                var sample = new GlobalProjectedCoordinate(
                    first.EastingMetres +
                    (second.EastingMetres - first.EastingMetres) * t,
                    first.NorthingMetres +
                    (second.NorthingMetres - first.NorthingMetres) * t);
                if (TryPickPlanningCell(sample, out var cell, out _, out _))
                    result.Add(cell);
            }
            return result.OrderBy(value => value).ToArray();
        }

        private PlacementIssue RoadIssue(FacilityRoadAccessStatus status)
        {
            switch (status)
            {
                case FacilityRoadAccessStatus.TooFar:
                    return new PlacementIssue(PlacementReasonIds.RoadTooFar,
                        "主要入口距离道路过远。", 100);
                case FacilityRoadAccessStatus.Blocked:
                    return new PlacementIssue(PlacementReasonIds.RoadBlocked,
                        "主要入口至道路的实体通路被阻断。", 101);
                case FacilityRoadAccessStatus.WrongSide:
                    return new PlacementIssue(
                        PlacementReasonIds.RoadWrongSide,
                        "最近道路位于主要入口背面，请旋转建筑。", 102);
                default:
                    return new PlacementIssue(PlacementReasonIds.RoadNoRoad,
                        "附近没有满足条件的道路。", 103);
            }
        }

        private WallLine CreateWallLine(
            FortificationSegmentSpatialState segment)
        {
            var firstCenter = _projection.PlanningCellCenter(
                segment.Edge.First);
            var secondCenter = _projection.PlanningCellCenter(
                segment.Edge.Second);
            var middle = new GlobalProjectedCoordinate(
                (firstCenter.EastingMetres + secondCenter.EastingMetres) *
                0.5d,
                (firstCenter.NorthingMetres + secondCenter.NorthingMetres) *
                0.5d);
            var half = DualScaleCountySpatialContractV1
                .PlanningCellSizeMetres * 0.5d;
            var rowsDiffer = segment.Edge.First.Row != segment.Edge.Second.Row;
            return new WallLine
            {
                Id = segment.Id,
                First = rowsDiffer
                    ? new GlobalProjectedCoordinate(
                        middle.EastingMetres - half,
                        middle.NorthingMetres)
                    : new GlobalProjectedCoordinate(middle.EastingMetres,
                        middle.NorthingMetres - half),
                Second = rowsDiffer
                    ? new GlobalProjectedCoordinate(
                        middle.EastingMetres + half,
                        middle.NorthingMetres)
                    : new GlobalProjectedCoordinate(middle.EastingMetres,
                        middle.NorthingMetres + half)
            };
        }

        private PlanningMetricBounds CellBounds(int localRow,
            int localColumn)
        {
            var center = _projection.PlanningCellCenter(
                _partition.ToGlobalCell(localRow, localColumn));
            var half = DualScaleCountySpatialContractV1
                .PlanningCellSizeMetres * 0.5d;
            return new PlanningMetricBounds(center.EastingMetres - half,
                center.EastingMetres + half, center.NorthingMetres - half,
                center.NorthingMetres + half);
        }

        private static PlanningMetricBounds Bounds(
            FacilitySpatialPlacement placement)
        {
            var width = placement.WidthCentimetres / 100d;
            var length = placement.DepthCentimetres / 100d;
            if ((placement.RotationQuarterTurns & 1) != 0)
            {
                var swap = width;
                width = length;
                length = swap;
            }
            return new PlanningMetricBounds(
                placement.Center.EastingMetres - width * 0.5d,
                placement.Center.EastingMetres + width * 0.5d,
                placement.Center.NorthingMetres - length * 0.5d,
                placement.Center.NorthingMetres + length * 0.5d);
        }

        private static PlanningMetricBounds Bounds(
            GlobalProjectedCoordinate first,
            GlobalProjectedCoordinate second) => new PlanningMetricBounds(
            Math.Min(first.EastingMetres, second.EastingMetres),
            Math.Max(first.EastingMetres, second.EastingMetres),
            Math.Min(first.NorthingMetres, second.NorthingMetres),
            Math.Max(first.NorthingMetres, second.NorthingMetres));

        private int Key(int localRow, int localColumn) =>
            checked(localRow * _partition.Columns + localColumn);

        private static void AddIndex<T>(IDictionary<int, List<T>> index,
            int key, T value)
        {
            if (!index.TryGetValue(key, out var values))
                index.Add(key, values = new List<T>());
            values.Add(value);
        }

        private static void Add(ICollection<PlacementIssue> issues,
            string code, string message, int priority,
            IEnumerable<string> relatedIds = null)
        {
            if (issues.Any(value => string.Equals(value.Code, code,
                    StringComparison.Ordinal))) return;
            issues.Add(new PlacementIssue(code, message, priority,
                relatedIds));
        }

        private static void DirectionVector(PlanningCellDirection direction,
            out double east, out double north)
        {
            east = direction == PlanningCellDirection.East ? 1d :
                direction == PlanningCellDirection.West ? -1d : 0d;
            north = direction == PlanningCellDirection.North ? 1d :
                direction == PlanningCellDirection.South ? -1d : 0d;
        }

        private static double Distance(GlobalProjectedCoordinate first,
            GlobalProjectedCoordinate second)
        {
            var east = first.EastingMetres - second.EastingMetres;
            var north = first.NorthingMetres - second.NorthingMetres;
            return Math.Sqrt(east * east + north * north);
        }

        private static double Clamp(double value, double minimum,
            double maximum) => Math.Max(minimum, Math.Min(maximum, value));

        private static bool SegmentIntersectsBounds(
            GlobalProjectedCoordinate first,
            GlobalProjectedCoordinate second, PlanningMetricBounds bounds)
        {
            if (bounds.Contains(first) || bounds.Contains(second)) return true;
            var northWest = new GlobalProjectedCoordinate(
                bounds.MinimumEasting, bounds.MaximumNorthing);
            var northEast = new GlobalProjectedCoordinate(
                bounds.MaximumEasting, bounds.MaximumNorthing);
            var southWest = new GlobalProjectedCoordinate(
                bounds.MinimumEasting, bounds.MinimumNorthing);
            var southEast = new GlobalProjectedCoordinate(
                bounds.MaximumEasting, bounds.MinimumNorthing);
            return SegmentsIntersect(first, second, northWest, northEast) ||
                   SegmentsIntersect(first, second, northEast, southEast) ||
                   SegmentsIntersect(first, second, southEast, southWest) ||
                   SegmentsIntersect(first, second, southWest, northWest);
        }

        private static bool SegmentsIntersect(
            GlobalProjectedCoordinate a, GlobalProjectedCoordinate b,
            GlobalProjectedCoordinate c, GlobalProjectedCoordinate d)
        {
            var first = Cross(a, b, c);
            var second = Cross(a, b, d);
            var third = Cross(c, d, a);
            var fourth = Cross(c, d, b);
            return first * second <= 0d && third * fourth <= 0d;
        }

        private static double Cross(GlobalProjectedCoordinate a,
            GlobalProjectedCoordinate b, GlobalProjectedCoordinate c) =>
            (b.EastingMetres - a.EastingMetres) *
            (c.NorthingMetres - a.NorthingMetres) -
            (b.NorthingMetres - a.NorthingMetres) *
            (c.EastingMetres - a.EastingMetres);
    }

    public sealed class CountyPlanningPerformanceSnapshot
    {
        public double CellPickP50Milliseconds { get; set; }
        public double CellPickP95Milliseconds { get; set; }
        public double ValidatorP50Milliseconds { get; set; }
        public double ValidatorP95Milliseconds { get; set; }
        public long ManagedAllocationBytes { get; set; }
        public int Samples { get; set; }
    }

    public static class CountyPlanningPerformanceBenchmark
    {
        public static CountyPlanningPerformanceSnapshot Measure(
            FacilityPlacementValidator validator,
            FacilityPlacementProfile profile, int row, int column,
            CountyPlanningSession session, int samples = 64)
        {
            if (samples < 8) throw new ArgumentOutOfRangeException(
                nameof(samples));
            var picks = new double[samples];
            var validations = new double[samples];
            var memoryBefore = GC.GetTotalMemory(false);
            var footprint = validator.CreateFootprint(profile, row, column, 0);
            for (var index = 0; index < samples; index++)
            {
                var timer = Stopwatch.StartNew();
                validator.TryPickPlanningCell(footprint.Center, out _, out _,
                    out _);
                timer.Stop();
                picks[index] = timer.Elapsed.TotalMilliseconds;
                timer.Restart();
                validator.Validate(profile, row, column, index % 4, session);
                timer.Stop();
                validations[index] = timer.Elapsed.TotalMilliseconds;
            }
            Array.Sort(picks);
            Array.Sort(validations);
            return new CountyPlanningPerformanceSnapshot
            {
                Samples = samples,
                CellPickP50Milliseconds = Percentile(picks, 0.50d),
                CellPickP95Milliseconds = Percentile(picks, 0.95d),
                ValidatorP50Milliseconds = Percentile(validations, 0.50d),
                ValidatorP95Milliseconds = Percentile(validations, 0.95d),
                ManagedAllocationBytes = Math.Max(0,
                    GC.GetTotalMemory(false) - memoryBefore)
            };
        }

        private static double Percentile(IReadOnlyList<double> values,
            double percentile) => values[Math.Min(values.Count - 1,
            Math.Max(0, (int)Math.Ceiling(values.Count * percentile) - 1))];
    }
}
