using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mandate.Domain
{
    public static class DualScaleCountySpatialContractV1
    {
        public const string ContractId =
            "mandate.spatial.dual-scale-county.candidate.v1";
        public const string PlanningGridSchemaVersion =
            "hanworld.planning-grid-50m.candidate.v1";
        public const int StrategicTileSizeMetres = 2_000;
        public const int PlanningCellSizeMetres = 50;
        public const int PlanningCellsPerStrategicAxis = 40;
        public const int PlanningCellsPerStrategicTile = 1_600;
        public const int DefaultChunkSizeCells = 16;

        public static CellGridIndex CreatePlanningCandidateGrid() =>
            new CellGridIndex(
                checked(GlobalSpatialFoundationV1.Rows *
                        PlanningCellsPerStrategicAxis),
                checked(GlobalSpatialFoundationV1.Columns *
                        PlanningCellsPerStrategicAxis),
                GlobalSpatialFoundationV1.OriginX,
                GlobalSpatialFoundationV1.OriginY,
                PlanningCellSizeMetres,
                PlanningGridSchemaVersion);
    }

    public readonly struct StrategicTileCoord :
        IEquatable<StrategicTileCoord>
    {
        public StrategicTileCoord(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }

        public bool Equals(StrategicTileCoord other) =>
            Row == other.Row && Column == other.Column;

        public override bool Equals(object obj) =>
            obj is StrategicTileCoord other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Row, Column);

        public override string ToString() => $"{Row}:{Column}";
    }

    public readonly struct PlanningCellCoord :
        IEquatable<PlanningCellCoord>, IComparable<PlanningCellCoord>
    {
        public PlanningCellCoord(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }

        public bool Equals(PlanningCellCoord other) =>
            Row == other.Row && Column == other.Column;

        public override bool Equals(object obj) =>
            obj is PlanningCellCoord other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Row, Column);

        public int CompareTo(PlanningCellCoord other)
        {
            var row = Row.CompareTo(other.Row);
            return row != 0 ? row : Column.CompareTo(other.Column);
        }

        public override string ToString() => $"{Row}:{Column}";
    }

    public sealed class DualScaleCoordinateProjection
    {
        public DualScaleCoordinateProjection(
            CellGridIndex strategicGrid = null,
            CellGridIndex planningGrid = null)
        {
            StrategicGrid = strategicGrid ??
                GlobalSpatialFoundationV1.CreateCellGrid();
            PlanningGrid = planningGrid ??
                DualScaleCountySpatialContractV1
                    .CreatePlanningCandidateGrid();
            if (Math.Abs(StrategicGrid.OriginX - PlanningGrid.OriginX) >
                    0.000001d ||
                Math.Abs(StrategicGrid.OriginY - PlanningGrid.OriginY) >
                    0.000001d ||
                Math.Abs(StrategicGrid.CellSize /
                    PlanningGrid.CellSize -
                    DualScaleCountySpatialContractV1
                        .PlanningCellsPerStrategicAxis) > 0.000001d)
                throw new ArgumentException(
                    "Strategic and planning grids are not exactly aligned.");
        }

        public CellGridIndex StrategicGrid { get; }
        public CellGridIndex PlanningGrid { get; }

        public PlanningCellCoord ToPlanningCell(
            GlobalProjectedCoordinate global)
        {
            if (!PlanningGrid.TryFromProjected(global.EastingMetres,
                    global.NorthingMetres, out var cellId) ||
                !PlanningGrid.TryDecode(cellId, out var row, out var column))
                throw new ArgumentOutOfRangeException(nameof(global));
            return new PlanningCellCoord(row, column);
        }

        public StrategicTileCoord ToStrategicTile(
            GlobalProjectedCoordinate global)
        {
            if (!StrategicGrid.TryFromProjected(global.EastingMetres,
                    global.NorthingMetres, out var cellId) ||
                !StrategicGrid.TryDecode(cellId, out var row, out var column))
                throw new ArgumentOutOfRangeException(nameof(global));
            return new StrategicTileCoord(row, column);
        }

        public StrategicTileCoord ToStrategicTile(PlanningCellCoord cell)
        {
            ValidatePlanningCell(cell);
            var ratio = DualScaleCountySpatialContractV1
                .PlanningCellsPerStrategicAxis;
            return new StrategicTileCoord(cell.Row / ratio,
                cell.Column / ratio);
        }

        public PlanningCellCoord StrategicTileMinimum(
            StrategicTileCoord tile)
        {
            ValidateStrategicTile(tile);
            var ratio = DualScaleCountySpatialContractV1
                .PlanningCellsPerStrategicAxis;
            return new PlanningCellCoord(
                checked(tile.Row * ratio),
                checked(tile.Column * ratio));
        }

        public GlobalProjectedCoordinate PlanningCellCenter(
            PlanningCellCoord cell)
        {
            ValidatePlanningCell(cell);
            PlanningGrid.GetCenter(cell.Row, cell.Column,
                out var x, out var y);
            return new GlobalProjectedCoordinate(x, y);
        }

        public WorldMapCellId StrategicCellId(StrategicTileCoord tile)
        {
            ValidateStrategicTile(tile);
            return StrategicGrid.ToCellId(tile.Row, tile.Column);
        }

        public WorldMapCellId PlanningCellId(PlanningCellCoord cell)
        {
            ValidatePlanningCell(cell);
            return PlanningGrid.ToCellId(cell.Row, cell.Column);
        }

        private void ValidateStrategicTile(StrategicTileCoord tile)
        {
            if (!StrategicGrid.Contains(tile.Row, tile.Column))
                throw new ArgumentOutOfRangeException(nameof(tile));
        }

        private void ValidatePlanningCell(PlanningCellCoord cell)
        {
            if (!PlanningGrid.Contains(cell.Row, cell.Column))
                throw new ArgumentOutOfRangeException(nameof(cell));
        }
    }

    public enum PlanningCellDirection : byte
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public enum PlanningCellConnectionKind : byte
    {
        Open = 0,
        BlockedByTerrain = 1,
        BlockedByWater = 2,
        BlockedByWall = 3,
        OpenByRoad = 4,
        OpenByBridge = 5,
        OpenByGate = 6,
        BlockedByClosedGate = 7,
        OpenThroughBreach = 8,
        TemporarilyBlocked = 9,
        OutsidePartition = 10
    }

    public static class PlanningCellDirections
    {
        public static PlanningCellDirection Opposite(
            PlanningCellDirection direction) =>
            (PlanningCellDirection)(((int)direction + 2) % 4);

        public static void Offset(PlanningCellDirection direction,
            out int row, out int column)
        {
            switch (direction)
            {
                case PlanningCellDirection.North:
                    row = -1; column = 0; return;
                case PlanningCellDirection.East:
                    row = 0; column = 1; return;
                case PlanningCellDirection.South:
                    row = 1; column = 0; return;
                case PlanningCellDirection.West:
                    row = 0; column = -1; return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }

    public sealed class PlanningCellConnectionGrid
    {
        private readonly byte[] _connections;

        public PlanningCellConnectionGrid(int rows, int columns)
        {
            if (rows <= 0 || columns <= 0)
                throw new ArgumentOutOfRangeException(nameof(rows));
            Rows = rows;
            Columns = columns;
            _connections = new byte[checked(rows * columns * 4)];
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            for (var direction = 0; direction < 4; direction++)
            {
                PlanningCellDirections.Offset(
                    (PlanningCellDirection)direction, out var dr,
                    out var dc);
                _connections[Index(row, column,
                    (PlanningCellDirection)direction)] = (byte)(Contains(
                        row + dr, column + dc)
                        ? PlanningCellConnectionKind.Open
                        : PlanningCellConnectionKind.OutsidePartition);
            }
        }

        public int Rows { get; }
        public int Columns { get; }
        public int PackedByteCount => _connections.Length;

        public PlanningCellConnectionKind Get(int row, int column,
            PlanningCellDirection direction)
        {
            Validate(row, column);
            return (PlanningCellConnectionKind)_connections[
                Index(row, column, direction)];
        }

        public void SetBetween(int row, int column,
            PlanningCellDirection direction,
            PlanningCellConnectionKind kind)
        {
            Validate(row, column);
            PlanningCellDirections.Offset(direction, out var dr, out var dc);
            var neighborRow = row + dr;
            var neighborColumn = column + dc;
            if (!Contains(neighborRow, neighborColumn))
                throw new InvalidOperationException(
                    "A county-internal connection requires two cells.");
            _connections[Index(row, column, direction)] = (byte)kind;
            _connections[Index(neighborRow, neighborColumn,
                PlanningCellDirections.Opposite(direction))] = (byte)kind;
        }

        public bool IsPassable(int row, int column,
            PlanningCellDirection direction)
        {
            switch (Get(row, column, direction))
            {
                case PlanningCellConnectionKind.Open:
                case PlanningCellConnectionKind.OpenByRoad:
                case PlanningCellConnectionKind.OpenByBridge:
                case PlanningCellConnectionKind.OpenByGate:
                case PlanningCellConnectionKind.OpenThroughBreach:
                    return true;
                default:
                    return false;
            }
        }

        public byte[] CopyPackedConnections() =>
            (byte[])_connections.Clone();

        private bool Contains(int row, int column) =>
            row >= 0 && row < Rows && column >= 0 && column < Columns;

        private void Validate(int row, int column)
        {
            if (!Contains(row, column))
                throw new ArgumentOutOfRangeException(nameof(row));
        }

        private int Index(int row, int column,
            PlanningCellDirection direction) =>
            checked((row * Columns + column) * 4 + (int)direction);
    }

    public enum PlanningTerrainClass : byte
    {
        Plains,
        Hill,
        Water,
        Forest,
        Marsh
    }

    public enum PlanningLandUseClass : byte
    {
        Unassigned,
        Road,
        Agriculture,
        Residential,
        Industry,
        Government,
        Military
    }

    public sealed class CountySpatialPartition
    {
        private readonly ushort[] _groundElevationDecimetres;
        private readonly byte[] _terrain;
        private readonly byte[] _slope;
        private readonly byte[] _buildability;
        private readonly byte[] _landUse;
        private readonly byte[] _water;
        private readonly byte[] _irrigation;
        private readonly Dictionary<string, FacilitySpatialPlacement>
            _facilityPlacements =
                new Dictionary<string, FacilitySpatialPlacement>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, CountyPortalSpatialState>
            _portals = new Dictionary<string, CountyPortalSpatialState>(
                StringComparer.Ordinal);
        private readonly Dictionary<string, FortificationSegmentSpatialState>
            _fortifications =
                new Dictionary<string, FortificationSegmentSpatialState>(
                    StringComparer.Ordinal);

        public CountySpatialPartition(string countyId,
            PlanningCellCoord minimumCell, int rows, int columns,
            int chunkSize = DualScaleCountySpatialContractV1
                .DefaultChunkSizeCells)
        {
            CountyId = new StableId(countyId).Value;
            if (rows <= 0 || columns <= 0 || chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(rows));
            MinimumCell = minimumCell;
            Rows = rows;
            Columns = columns;
            ChunkSize = chunkSize;
            var count = checked(rows * columns);
            _groundElevationDecimetres = new ushort[count];
            _terrain = new byte[count];
            _slope = new byte[count];
            _buildability = Enumerable.Repeat((byte)1, count).ToArray();
            _landUse = new byte[count];
            _water = new byte[count];
            _irrigation = new byte[count];
            Connections = new PlanningCellConnectionGrid(rows, columns);
        }

        public string CountyId { get; }
        public PlanningCellCoord MinimumCell { get; }
        public int Rows { get; }
        public int Columns { get; }
        public int ChunkSize { get; }
        public int PlanningCellCount => checked(Rows * Columns);
        public int ChunkCount => checked(
            ((Rows + ChunkSize - 1) / ChunkSize) *
            ((Columns + ChunkSize - 1) / ChunkSize));
        public int PackedArrayBytes => checked(
            _groundElevationDecimetres.Length * sizeof(ushort) +
            _terrain.Length + _slope.Length + _buildability.Length +
            _landUse.Length + _water.Length + _irrigation.Length +
            Connections.PackedByteCount);
        public PlanningCellConnectionGrid Connections { get; }
        public IReadOnlyDictionary<string, FacilitySpatialPlacement>
            FacilityPlacements => _facilityPlacements;
        public IReadOnlyDictionary<string, CountyPortalSpatialState> Portals =>
            _portals;
        public IReadOnlyDictionary<string, FortificationSegmentSpatialState>
            Fortifications => _fortifications;

        public PlanningCellCoord ToGlobalCell(int localRow, int localColumn)
        {
            ValidateLocal(localRow, localColumn);
            return new PlanningCellCoord(
                checked(MinimumCell.Row + localRow),
                checked(MinimumCell.Column + localColumn));
        }

        public bool TryToLocal(PlanningCellCoord global,
            out int localRow, out int localColumn)
        {
            localRow = global.Row - MinimumCell.Row;
            localColumn = global.Column - MinimumCell.Column;
            return localRow >= 0 && localRow < Rows &&
                   localColumn >= 0 && localColumn < Columns;
        }

        public void SetCell(int localRow, int localColumn,
            ushort groundElevationDecimetres,
            PlanningTerrainClass terrain, byte slopeBasis,
            bool buildable, PlanningLandUseClass landUse,
            byte waterState, byte irrigationState)
        {
            var index = Index(localRow, localColumn);
            _groundElevationDecimetres[index] = groundElevationDecimetres;
            _terrain[index] = (byte)terrain;
            _slope[index] = slopeBasis;
            _buildability[index] = buildable ? (byte)1 : (byte)0;
            _landUse[index] = (byte)landUse;
            _water[index] = waterState;
            _irrigation[index] = irrigationState;
        }

        public ushort GroundElevationDecimetres(int localRow,
            int localColumn) =>
            _groundElevationDecimetres[Index(localRow, localColumn)];

        public PlanningTerrainClass Terrain(int localRow, int localColumn) =>
            (PlanningTerrainClass)_terrain[Index(localRow, localColumn)];

        public PlanningLandUseClass LandUse(int localRow, int localColumn) =>
            (PlanningLandUseClass)_landUse[Index(localRow, localColumn)];

        public byte SlopeBasis(int localRow, int localColumn) =>
            _slope[Index(localRow, localColumn)];

        public bool IsBuildable(int localRow, int localColumn) =>
            _buildability[Index(localRow, localColumn)] != 0;

        public byte WaterState(int localRow, int localColumn) =>
            _water[Index(localRow, localColumn)];

        public byte IrrigationState(int localRow, int localColumn) =>
            _irrigation[Index(localRow, localColumn)];

        public void SetWaterState(int localRow, int localColumn,
            byte waterState) =>
            _water[Index(localRow, localColumn)] = waterState;

        public void SetLandUse(int localRow, int localColumn,
            PlanningLandUseClass landUse) =>
            _landUse[Index(localRow, localColumn)] = (byte)landUse;

        public void AddFacilityPlacement(FacilitySpatialPlacement placement)
        {
            if (placement == null)
                throw new ArgumentNullException(nameof(placement));
            if (!string.Equals(placement.CountyId, CountyId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Facility placement belongs to another county.");
            _facilityPlacements.Add(placement.FacilityId, placement);
        }

        public void AddPortal(CountyPortalSpatialState portal)
        {
            if (portal == null) throw new ArgumentNullException(nameof(portal));
            if (!string.Equals(portal.CountyId, CountyId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Portal belongs to another county.");
            _portals.Add(portal.PortalId, portal);
        }

        public void AddFortification(
            FortificationSegmentSpatialState fortification)
        {
            if (fortification == null)
                throw new ArgumentNullException(nameof(fortification));
            if (!string.Equals(fortification.CountyId, CountyId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Fortification belongs to another county.");
            _fortifications.Add(fortification.Id, fortification);
        }

        public string ComputeSpatialHash()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(CountyId);
                writer.Write(MinimumCell.Row);
                writer.Write(MinimumCell.Column);
                writer.Write(Rows);
                writer.Write(Columns);
                for (var i = 0; i < _groundElevationDecimetres.Length; i++)
                    writer.Write(_groundElevationDecimetres[i]);
                writer.Write(_terrain);
                writer.Write(_slope);
                writer.Write(_buildability);
                writer.Write(_landUse);
                writer.Write(_water);
                writer.Write(_irrigation);
                writer.Write(Connections.CopyPackedConnections());
                foreach (var item in _facilityPlacements
                             .OrderBy(item => item.Key,
                                 StringComparer.Ordinal))
                    item.Value.WriteDeterministic(writer);
                foreach (var item in _portals.OrderBy(item => item.Key,
                             StringComparer.Ordinal))
                    item.Value.WriteDeterministic(writer);
                foreach (var item in _fortifications.OrderBy(item => item.Key,
                             StringComparer.Ordinal))
                    item.Value.WriteDeterministic(writer);
                writer.Flush();
                using (var sha = SHA256.Create())
                    return string.Concat(sha.ComputeHash(stream.ToArray())
                        .Select(value => value.ToString("x2")));
            }
        }

        private int Index(int localRow, int localColumn)
        {
            ValidateLocal(localRow, localColumn);
            return checked(localRow * Columns + localColumn);
        }

        private void ValidateLocal(int localRow, int localColumn)
        {
            if (localRow < 0 || localRow >= Rows || localColumn < 0 ||
                localColumn >= Columns)
                throw new ArgumentOutOfRangeException(nameof(localRow));
        }
    }

    public sealed class FacilityEntranceSpatialState
    {
        public FacilityEntranceSpatialState(string id,
            GlobalProjectedCoordinate position,
            PlanningCellDirection outwardDirection)
        {
            Id = new StableId(id).Value;
            Position = position;
            OutwardDirection = outwardDirection;
        }

        public string Id { get; }
        public GlobalProjectedCoordinate Position { get; }
        public PlanningCellDirection OutwardDirection { get; }

        internal void WriteDeterministic(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Position.EastingMetres);
            writer.Write(Position.NorthingMetres);
            writer.Write((byte)OutwardDirection);
        }
    }

    public sealed class FacilitySpatialPlacement
    {
        private readonly List<FacilityEntranceSpatialState> _entrances;

        public FacilitySpatialPlacement(string facilityId, string countyId,
            GlobalProjectedCoordinate center, int widthCentimetres,
            int depthCentimetres, int rotationQuarterTurns,
            int structureHeightCentimetres, string collisionProfileId,
            IEnumerable<FacilityEntranceSpatialState> entrances)
        {
            FacilityId = new StableId(facilityId).Value;
            CountyId = new StableId(countyId).Value;
            if (widthCentimetres <= 0 || depthCentimetres <= 0 ||
                rotationQuarterTurns < 0 || rotationQuarterTurns > 3 ||
                structureHeightCentimetres < 0)
                throw new ArgumentOutOfRangeException(nameof(widthCentimetres));
            WidthCentimetres = widthCentimetres;
            DepthCentimetres = depthCentimetres;
            RotationQuarterTurns = rotationQuarterTurns;
            StructureHeightCentimetres = structureHeightCentimetres;
            CollisionProfileId = new StableId(collisionProfileId).Value;
            Center = center;
            _entrances = (entrances ?? throw new ArgumentNullException(
                    nameof(entrances))).ToList();
            if (_entrances.Count == 0 || _entrances.Any(item => item == null) ||
                _entrances.Select(item => item.Id).Distinct(
                    StringComparer.Ordinal).Count() != _entrances.Count)
                throw new ArgumentException(
                    "Facility placement needs unique physical entrances.");
        }

        public string FacilityId { get; }
        public string CountyId { get; }
        public GlobalProjectedCoordinate Center { get; }
        public int WidthCentimetres { get; }
        public int DepthCentimetres { get; }
        public int RotationQuarterTurns { get; }
        public int StructureHeightCentimetres { get; }
        public string CollisionProfileId { get; }
        public IReadOnlyList<FacilityEntranceSpatialState> Entrances =>
            _entrances;

        public IReadOnlyList<PlanningCellCoord> ResolveCoveredPlanningCells(
            DualScaleCoordinateProjection projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            var widthMetres = WidthCentimetres / 100d;
            var depthMetres = DepthCentimetres / 100d;
            if ((RotationQuarterTurns & 1) != 0)
            {
                var swap = widthMetres;
                widthMetres = depthMetres;
                depthMetres = swap;
            }
            var epsilon = 0.000001d;
            var west = Center.EastingMetres - widthMetres * 0.5d;
            var east = Center.EastingMetres + widthMetres * 0.5d - epsilon;
            var north = Center.NorthingMetres + depthMetres * 0.5d;
            var south = Center.NorthingMetres - depthMetres * 0.5d + epsilon;
            var northWest = projection.ToPlanningCell(
                new GlobalProjectedCoordinate(west, north));
            var southEast = projection.ToPlanningCell(
                new GlobalProjectedCoordinate(east, south));
            var cells = new List<PlanningCellCoord>();
            for (var row = northWest.Row; row <= southEast.Row; row++)
            for (var column = northWest.Column;
                 column <= southEast.Column; column++)
                cells.Add(new PlanningCellCoord(row, column));
            return cells;
        }

        public FacilityEntranceSpatialState Entrance(string entranceId) =>
            _entrances.Single(item => string.Equals(item.Id, entranceId,
                StringComparison.Ordinal));

        internal void WriteDeterministic(BinaryWriter writer)
        {
            writer.Write(FacilityId);
            writer.Write(CountyId);
            writer.Write(Center.EastingMetres);
            writer.Write(Center.NorthingMetres);
            writer.Write(WidthCentimetres);
            writer.Write(DepthCentimetres);
            writer.Write(RotationQuarterTurns);
            writer.Write(StructureHeightCentimetres);
            writer.Write(CollisionProfileId);
            foreach (var entrance in _entrances.OrderBy(item => item.Id,
                         StringComparer.Ordinal))
                entrance.WriteDeterministic(writer);
        }
    }

    public enum PersonSpatialModeV1 : byte
    {
        CountyLocal,
        InsideFacility,
        StrategicTransit,
        ArmyAttached
    }

    public sealed class PersonSpatialStateV1
    {
        private PersonSpatialStateV1(string personId,
            PersonSpatialModeV1 mode, string countyId,
            GlobalProjectedCoordinate localPosition, string facilityId,
            string routeId, string segmentId, int progressBasisPoints,
            string armyId)
        {
            PersonId = new StableId(personId).Value;
            Mode = mode;
            CountyId = countyId ?? string.Empty;
            LocalPosition = localPosition;
            FacilityId = facilityId ?? string.Empty;
            RouteId = routeId ?? string.Empty;
            SegmentId = segmentId ?? string.Empty;
            ProgressBasisPoints = progressBasisPoints;
            ArmyId = armyId ?? string.Empty;
            Validate();
        }

        public string PersonId { get; }
        public PersonSpatialModeV1 Mode { get; }
        public string CountyId { get; }
        public GlobalProjectedCoordinate LocalPosition { get; }
        public string FacilityId { get; }
        public string RouteId { get; }
        public string SegmentId { get; }
        public int ProgressBasisPoints { get; }
        public string ArmyId { get; }

        public static PersonSpatialStateV1 CountyLocal(string personId,
            string countyId, GlobalProjectedCoordinate position) =>
            new PersonSpatialStateV1(personId,
                PersonSpatialModeV1.CountyLocal,
                new StableId(countyId).Value, position,
                string.Empty, string.Empty, string.Empty, 0, string.Empty);

        public static PersonSpatialStateV1 InsideFacility(string personId,
            string facilityId) => new PersonSpatialStateV1(personId,
            PersonSpatialModeV1.InsideFacility, string.Empty, default,
            new StableId(facilityId).Value, string.Empty, string.Empty, 0,
            string.Empty);

        public static PersonSpatialStateV1 StrategicTransit(string personId,
            string routeId, string segmentId, int progressBasisPoints) =>
            new PersonSpatialStateV1(personId,
                PersonSpatialModeV1.StrategicTransit, string.Empty, default,
                string.Empty, new StableId(routeId).Value,
                new StableId(segmentId).Value, progressBasisPoints,
                string.Empty);

        public static PersonSpatialStateV1 ArmyAttached(string personId,
            string armyId) => new PersonSpatialStateV1(personId,
            PersonSpatialModeV1.ArmyAttached, string.Empty, default,
            string.Empty, string.Empty, string.Empty, 0,
            new StableId(armyId).Value);

        private void Validate()
        {
            var local = !string.IsNullOrWhiteSpace(CountyId);
            var facility = !string.IsNullOrWhiteSpace(FacilityId);
            var transit = !string.IsNullOrWhiteSpace(RouteId) &&
                          !string.IsNullOrWhiteSpace(SegmentId) &&
                          ProgressBasisPoints >= 0 &&
                          ProgressBasisPoints <= 10_000;
            var army = !string.IsNullOrWhiteSpace(ArmyId);
            var valid = Mode == PersonSpatialModeV1.CountyLocal && local &&
                        !facility && !transit && !army ||
                        Mode == PersonSpatialModeV1.InsideFacility &&
                        facility && !local && !transit && !army ||
                        Mode == PersonSpatialModeV1.StrategicTransit &&
                        transit && !local && !facility && !army ||
                        Mode == PersonSpatialModeV1.ArmyAttached && army &&
                        !local && !facility && !transit;
            if (!valid)
                throw new InvalidOperationException(
                    "A Person must have exactly one spatial authority.");
        }
    }

    public enum ArmySpatialModeV1 : byte
    {
        Strategic,
        CountyMaterialized
    }

    public sealed class ArmySpatialStateV1
    {
        private ArmySpatialStateV1(string armyId, ArmySpatialModeV1 mode,
            string routeId, string countyId,
            GlobalProjectedCoordinate localPosition)
        {
            ArmyId = new StableId(armyId).Value;
            Mode = mode;
            RouteId = routeId ?? string.Empty;
            CountyId = countyId ?? string.Empty;
            LocalPosition = localPosition;
            if (mode == ArmySpatialModeV1.Strategic !=
                    !string.IsNullOrWhiteSpace(RouteId) ||
                mode == ArmySpatialModeV1.CountyMaterialized !=
                    !string.IsNullOrWhiteSpace(CountyId))
                throw new InvalidOperationException(
                    "An Army must have one spatial authority.");
        }

        public string ArmyId { get; }
        public ArmySpatialModeV1 Mode { get; }
        public string RouteId { get; }
        public string CountyId { get; }
        public GlobalProjectedCoordinate LocalPosition { get; }

        public static ArmySpatialStateV1 Strategic(string armyId,
            string routeId) => new ArmySpatialStateV1(armyId,
            ArmySpatialModeV1.Strategic,
            new StableId(routeId).Value, string.Empty, default);

        public static ArmySpatialStateV1 CountyMaterialized(string armyId,
            string countyId, GlobalProjectedCoordinate position) =>
            new ArmySpatialStateV1(armyId,
                ArmySpatialModeV1.CountyMaterialized, string.Empty,
                new StableId(countyId).Value, position);
    }

    public sealed class CountyPortalSpatialState
    {
        public CountyPortalSpatialState(string portalId, string routeId,
            string countyId, string neighborCountyId,
            PlanningCellCoord cell, StrategicTileCoord strategicTile,
            string passageTypeId)
        {
            PortalId = new StableId(portalId).Value;
            RouteId = new StableId(routeId).Value;
            CountyId = new StableId(countyId).Value;
            NeighborCountyId = new StableId(neighborCountyId).Value;
            Cell = cell;
            StrategicTile = strategicTile;
            PassageTypeId = new StableId(passageTypeId).Value;
        }

        public string PortalId { get; }
        public string RouteId { get; }
        public string CountyId { get; }
        public string NeighborCountyId { get; }
        public PlanningCellCoord Cell { get; }
        public StrategicTileCoord StrategicTile { get; }
        public string PassageTypeId { get; }

        internal void WriteDeterministic(BinaryWriter writer)
        {
            writer.Write(PortalId);
            writer.Write(RouteId);
            writer.Write(CountyId);
            writer.Write(NeighborCountyId);
            writer.Write(Cell.Row);
            writer.Write(Cell.Column);
            writer.Write(StrategicTile.Row);
            writer.Write(StrategicTile.Column);
            writer.Write(PassageTypeId);
        }
    }

    public sealed class WorldRouteSpatialStateV1
    {
        private readonly List<CountyPortalSpatialState> _portals;

        public WorldRouteSpatialStateV1(string routeId,
            IEnumerable<CountyPortalSpatialState> portals)
        {
            RouteId = new StableId(routeId).Value;
            _portals = (portals ?? throw new ArgumentNullException(
                    nameof(portals))).OrderBy(item => item.PortalId,
                    StringComparer.Ordinal).ToList();
            if (_portals.Count < 2 || _portals.Any(item =>
                    !string.Equals(item.RouteId, RouteId,
                        StringComparison.Ordinal)))
                throw new ArgumentException(
                    "A world Route needs at least two matching Portals.");
        }

        public string RouteId { get; }
        public IReadOnlyList<CountyPortalSpatialState> Portals => _portals;
    }

    public readonly struct PlanningCellEdge :
        IEquatable<PlanningCellEdge>
    {
        public PlanningCellEdge(PlanningCellCoord cell,
            PlanningCellDirection direction)
        {
            PlanningCellDirections.Offset(direction, out var dr, out var dc);
            var neighbor = new PlanningCellCoord(cell.Row + dr,
                cell.Column + dc);
            if (cell.CompareTo(neighbor) <= 0)
            {
                First = cell;
                Second = neighbor;
            }
            else
            {
                First = neighbor;
                Second = cell;
            }
        }

        public PlanningCellCoord First { get; }
        public PlanningCellCoord Second { get; }

        public bool Equals(PlanningCellEdge other) =>
            First.Equals(other.First) && Second.Equals(other.Second);

        public override bool Equals(object obj) =>
            obj is PlanningCellEdge other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(First, Second);
    }

    public enum GatePassageStateV1 : byte
    {
        Open,
        Closed,
        Locked,
        Breached,
        Disabled,
        Destroyed
    }

    public sealed class FortificationSegmentSpatialState
    {
        public FortificationSegmentSpatialState(string id, string countyId,
            string definitionId, PlanningCellEdge edge, bool isGate,
            int heightCentimetres, int thicknessCentimetres,
            int maximumDurability, string ownerId, string controllerId,
            int garrisonCount = 0)
        {
            Id = new StableId(id).Value;
            CountyId = new StableId(countyId).Value;
            DefinitionId = new StableId(definitionId).Value;
            if (heightCentimetres <= 0 || thicknessCentimetres <= 0 ||
                maximumDurability <= 0 || garrisonCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumDurability));
            Edge = edge;
            IsGate = isGate;
            HeightCentimetres = heightCentimetres;
            ThicknessCentimetres = thicknessCentimetres;
            MaximumDurability = maximumDurability;
            Durability = maximumDurability;
            OwnerId = new StableId(ownerId).Value;
            ControllerId = new StableId(controllerId).Value;
            GarrisonCount = garrisonCount;
            GateState = isGate ? GatePassageStateV1.Closed :
                GatePassageStateV1.Locked;
        }

        public string Id { get; }
        public string CountyId { get; }
        public string DefinitionId { get; }
        public PlanningCellEdge Edge { get; }
        public bool IsGate { get; }
        public int HeightCentimetres { get; }
        public int ThicknessCentimetres { get; }
        public int MaximumDurability { get; }
        public int Durability { get; private set; }
        public int GarrisonCount { get; private set; }
        public bool GarrisonSurrendered { get; private set; }
        public string OwnerId { get; }
        public string ControllerId { get; private set; }
        public GatePassageStateV1 GateState { get; private set; }

        public PlanningCellConnectionKind PassageKind
        {
            get
            {
                if (Durability == 0 || GateState ==
                        GatePassageStateV1.Breached || GateState ==
                        GatePassageStateV1.Destroyed)
                    return PlanningCellConnectionKind.OpenThroughBreach;
                if (!IsGate)
                    return PlanningCellConnectionKind.BlockedByWall;
                return GateState == GatePassageStateV1.Open
                    ? PlanningCellConnectionKind.OpenByGate
                    : PlanningCellConnectionKind.BlockedByClosedGate;
            }
        }

        public void SetGateState(GatePassageStateV1 state)
        {
            if (!IsGate)
                throw new InvalidOperationException(
                    "Only a Gate segment has a Gate state.");
            if (Durability == 0 && state != GatePassageStateV1.Breached &&
                state != GatePassageStateV1.Destroyed)
                throw new InvalidOperationException(
                    "A breached Gate cannot be closed without rebuilding.");
            GateState = state;
        }

        public void ApplyDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Durability = Math.Max(0, Durability - amount);
            if (Durability == 0)
                GateState = GatePassageStateV1.Breached;
        }

        public void ApplyGarrisonLoss(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            GarrisonCount = Math.Max(0, GarrisonCount - amount);
        }

        public void Surrender() => GarrisonSurrendered = true;

        public bool TryOccupy(string controllerId)
        {
            if (GarrisonCount > 0 && !GarrisonSurrendered) return false;
            ControllerId = new StableId(controllerId).Value;
            return true;
        }

        internal void WriteDeterministic(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(CountyId);
            writer.Write(DefinitionId);
            writer.Write(Edge.First.Row);
            writer.Write(Edge.First.Column);
            writer.Write(Edge.Second.Row);
            writer.Write(Edge.Second.Column);
            writer.Write(IsGate);
            writer.Write(HeightCentimetres);
            writer.Write(ThicknessCentimetres);
            writer.Write(MaximumDurability);
            writer.Write(Durability);
            writer.Write(GarrisonCount);
            writer.Write(GarrisonSurrendered);
            writer.Write(OwnerId);
            writer.Write(ControllerId);
            writer.Write((byte)GateState);
        }
    }

    public sealed class FacilityDefenseStateV1
    {
        public FacilityDefenseStateV1(string facilityId, int garrisonCount,
            int moraleBasisPoints = 10_000)
        {
            FacilityId = new StableId(facilityId).Value;
            if (garrisonCount < 0 || moraleBasisPoints < 0 ||
                moraleBasisPoints > 10_000)
                throw new ArgumentOutOfRangeException(nameof(garrisonCount));
            GarrisonCount = garrisonCount;
            MoraleBasisPoints = moraleBasisPoints;
        }

        public string FacilityId { get; }
        public int GarrisonCount { get; private set; }
        public int MoraleBasisPoints { get; private set; }
        public bool HasSurrendered { get; private set; }

        public void ApplyLoss(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            GarrisonCount = Math.Max(0, GarrisonCount - amount);
        }

        public void Surrender()
        {
            HasSurrendered = true;
            MoraleBasisPoints = 0;
        }
    }

    public static class FacilityConflictRulesV1
    {
        public static void ApplyStructuralDamage(FacilityState facility,
            int conditionDamageBasisPoints)
        {
            if (facility == null) throw new ArgumentNullException(nameof(facility));
            if (conditionDamageBasisPoints < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(conditionDamageBasisPoints));
            facility.ConditionBasisPoints = Math.Max(0,
                facility.ConditionBasisPoints - conditionDamageBasisPoints);
            if (facility.ConditionBasisPoints == 0 &&
                facility.LifecycleStatus != FacilityLifecycleStatus.Destroyed)
                facility.LifecycleStatus = FacilityLifecycleStatus.Disabled;
        }

        public static void Destroy(FacilityState facility)
        {
            if (facility == null) throw new ArgumentNullException(nameof(facility));
            facility.ConditionBasisPoints = 0;
            facility.LifecycleStatus = FacilityLifecycleStatus.Destroyed;
        }

        public static bool TryRepair(FacilityState facility,
            int restoredConditionBasisPoints)
        {
            if (facility == null) throw new ArgumentNullException(nameof(facility));
            if (restoredConditionBasisPoints <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(restoredConditionBasisPoints));
            if (facility.LifecycleStatus == FacilityLifecycleStatus.Destroyed)
                return false;
            facility.ConditionBasisPoints = Math.Min(10_000,
                facility.ConditionBasisPoints + restoredConditionBasisPoints);
            if (facility.ConditionBasisPoints > 0)
                facility.LifecycleStatus = FacilityLifecycleStatus.Operational;
            return true;
        }

        public static bool TryOccupy(FacilityState facility,
            FacilityDefenseStateV1 defense, string newControllerId)
        {
            if (facility == null) throw new ArgumentNullException(nameof(facility));
            if (defense == null) throw new ArgumentNullException(nameof(defense));
            if (!string.Equals(facility.Id, defense.FacilityId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Facility defense identity mismatch.");
            if (defense.GarrisonCount > 0 && !defense.HasSurrendered)
                return false;
            facility.ControllerId = new StableId(newControllerId).Value;
            return true;
        }
    }

    public readonly struct EffectiveElevationSample
    {
        public EffectiveElevationSample(GlobalProjectedCoordinate position,
            int groundElevationCentimetres,
            int structureHeightCentimetres,
            int combatPositionHeightOffsetCentimetres)
        {
            Position = position;
            GroundElevationCentimetres = groundElevationCentimetres;
            StructureHeightCentimetres = structureHeightCentimetres;
            CombatPositionHeightOffsetCentimetres =
                combatPositionHeightOffsetCentimetres;
        }

        public GlobalProjectedCoordinate Position { get; }
        public int GroundElevationCentimetres { get; }
        public int StructureHeightCentimetres { get; }
        public int CombatPositionHeightOffsetCentimetres { get; }
        public int EffectiveElevationCentimetres => checked(
            GroundElevationCentimetres + StructureHeightCentimetres +
            CombatPositionHeightOffsetCentimetres);
    }

    public readonly struct SpatialOccluderV1
    {
        public SpatialOccluderV1(string id, double minimumEastingMetres,
            double maximumEastingMetres, double minimumNorthingMetres,
            double maximumNorthingMetres, int topElevationCentimetres)
        {
            Id = new StableId(id).Value;
            if (minimumEastingMetres > maximumEastingMetres ||
                minimumNorthingMetres > maximumNorthingMetres)
                throw new ArgumentException("Occluder bounds are invalid.");
            MinimumEastingMetres = minimumEastingMetres;
            MaximumEastingMetres = maximumEastingMetres;
            MinimumNorthingMetres = minimumNorthingMetres;
            MaximumNorthingMetres = maximumNorthingMetres;
            TopElevationCentimetres = topElevationCentimetres;
        }

        public string Id { get; }
        public double MinimumEastingMetres { get; }
        public double MaximumEastingMetres { get; }
        public double MinimumNorthingMetres { get; }
        public double MaximumNorthingMetres { get; }
        public int TopElevationCentimetres { get; }
    }

    public sealed class SpatialLineOfSightQueryV1
    {
        public bool HasLineOfSight(EffectiveElevationSample observer,
            EffectiveElevationSample target,
            IEnumerable<SpatialOccluderV1> occluders,
            out string blockingObjectId)
        {
            blockingObjectId = string.Empty;
            foreach (var occluder in (occluders ??
                         throw new ArgumentNullException(nameof(occluders)))
                     .OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                if (!TryIntersectionParameter(observer.Position,
                        target.Position, occluder, out var parameter) ||
                    parameter <= 0.000001d || parameter >= 0.999999d)
                    continue;
                var rayHeight = observer.EffectiveElevationCentimetres +
                    (target.EffectiveElevationCentimetres -
                     observer.EffectiveElevationCentimetres) * parameter;
                if (occluder.TopElevationCentimetres + 0.000001d < rayHeight)
                    continue;
                blockingObjectId = occluder.Id;
                return false;
            }
            return true;
        }

        private static bool TryIntersectionParameter(
            GlobalProjectedCoordinate origin,
            GlobalProjectedCoordinate target, SpatialOccluderV1 obstacle,
            out double parameter)
        {
            var tMin = 0d;
            var tMax = 1d;
            if (!Clip(origin.EastingMetres,
                    target.EastingMetres - origin.EastingMetres,
                    obstacle.MinimumEastingMetres,
                    obstacle.MaximumEastingMetres, ref tMin, ref tMax) ||
                !Clip(origin.NorthingMetres,
                    target.NorthingMetres - origin.NorthingMetres,
                    obstacle.MinimumNorthingMetres,
                    obstacle.MaximumNorthingMetres, ref tMin, ref tMax))
            {
                parameter = 0d;
                return false;
            }
            parameter = tMin;
            return true;
        }

        private static bool Clip(double origin, double delta, double minimum,
            double maximum, ref double tMin, ref double tMax)
        {
            if (Math.Abs(delta) < 0.000000001d)
                return origin >= minimum && origin <= maximum;
            var a = (minimum - origin) / delta;
            var b = (maximum - origin) / delta;
            if (a > b)
            {
                var swap = a;
                a = b;
                b = swap;
            }
            tMin = Math.Max(tMin, a);
            tMax = Math.Min(tMax, b);
            return tMin <= tMax;
        }
    }
}
