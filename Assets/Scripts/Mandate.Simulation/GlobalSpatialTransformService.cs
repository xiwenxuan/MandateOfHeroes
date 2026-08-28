using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class GlobalSpatialTransformService
    {
        private const double SemiMajor = 6378137d;
        private const double EccentricitySquared = 0.0066943799901413165d;
        private const double StandardParallel1 = 25d;
        private const double StandardParallel2 = 47d;
        private const double LatitudeOfOrigin = 0d;
        private const double CentralMeridian = 105d;
        private readonly double _n;
        private readonly double _c;
        private readonly double _rho0;

        public GlobalSpatialTransformService(CellGridIndex cells = null, int canonicalChunkSizeCells = 16)
        {
            Cells = cells ?? GlobalSpatialFoundationV1.CreateCellGrid();
            Chunks = new GlobalChunkGridIndex(Cells, canonicalChunkSizeCells);
            var phi1 = Radians(StandardParallel1);
            var phi2 = Radians(StandardParallel2);
            var m1 = M(phi1);
            var m2 = M(phi2);
            var q1 = Q(phi1);
            var q2 = Q(phi2);
            _n = (m1 * m1 - m2 * m2) / (q2 - q1);
            _c = m1 * m1 + _n * q1;
            _rho0 = Rho(Q(Radians(LatitudeOfOrigin)));
        }

        public CellGridIndex Cells { get; }
        public GlobalChunkGridIndex Chunks { get; }
        public GlobalChunkGridIndex AggregationBlocks => Chunks;

        public GlobalProjectedCoordinate GeographicToGlobal(GeographicCoordinate geographic)
        {
            var phi = Radians(geographic.LatitudeDegrees);
            var theta = _n * Radians(geographic.LongitudeDegrees - CentralMeridian);
            var rho = Rho(Q(phi));
            return new GlobalProjectedCoordinate(rho * Math.Sin(theta), _rho0 - rho * Math.Cos(theta));
        }

        public GeographicCoordinate GlobalToGeographic(GlobalProjectedCoordinate global)
        {
            var dx = global.EastingMetres;
            var dy = _rho0 - global.NorthingMetres;
            var rho = Math.Sqrt(dx * dx + dy * dy);
            if (_n < 0d) rho = -rho;
            var theta = Math.Atan2(dx, dy);
            var q = (_c - Math.Pow(rho * _n / SemiMajor, 2d)) / _n;
            var phi = Math.Asin(Math.Max(-1d, Math.Min(1d, q / 2d)));
            for (var index = 0; index < 12; index++)
            {
                var sin = Math.Sin(phi);
                var oneMinus = 1d - EccentricitySquared * sin * sin;
                var next = phi + oneMinus * oneMinus / (2d * Math.Cos(phi)) *
                    (q / (1d - EccentricitySquared) - sin / oneMinus +
                     0.5d / Math.Sqrt(EccentricitySquared) * Math.Log(
                         (1d - Math.Sqrt(EccentricitySquared) * sin) /
                         (1d + Math.Sqrt(EccentricitySquared) * sin)));
                if (Math.Abs(next - phi) < 1e-13d) { phi = next; break; }
                phi = next;
            }
            return new GeographicCoordinate(
                CentralMeridian + Degrees(theta / _n), Degrees(phi));
        }

        public bool TryGlobalToCell(GlobalProjectedCoordinate global, out WorldMapCellId cellId) =>
            Cells.TryFromProjected(global.EastingMetres, global.NorthingMetres, out cellId);

        public GlobalProjectedCoordinate CellToGlobalCenter(WorldMapCellId cellId)
        {
            if (!Cells.TryDecode(cellId, out var row, out var column))
                throw new ArgumentOutOfRangeException(nameof(cellId));
            Cells.GetCenter(row, column, out var x, out var y);
            return new GlobalProjectedCoordinate(x, y);
        }

        public GlobalChunkId CellToGlobalChunk(WorldMapCellId cellId)
        {
            if (!Cells.TryDecode(cellId, out var row, out var column))
                throw new ArgumentOutOfRangeException(nameof(cellId));
            return Chunks.FromCell(row, column);
        }

        public GlobalChunkId CellToSimulationAggregationBlock(WorldMapCellId cellId) =>
            CellToGlobalChunk(cellId);

        public LocalPlanarCoordinate GlobalToRegionLocal(GlobalProjectedCoordinate global,
            GlobalProjectedCoordinate regionOrigin) => new LocalPlanarCoordinate(
            global.EastingMetres - regionOrigin.EastingMetres,
            global.NorthingMetres - regionOrigin.NorthingMetres);

        public GlobalProjectedCoordinate RegionLocalToGlobal(LocalPlanarCoordinate local,
            GlobalProjectedCoordinate regionOrigin) => new GlobalProjectedCoordinate(
            local.XMetres + regionOrigin.EastingMetres,
            local.YMetres + regionOrigin.NorthingMetres);

        public LocalPlanarCoordinate GlobalToChunkLocal(GlobalProjectedCoordinate global, GlobalChunkId chunk)
        {
            Chunks.GetGlobalOrigin(chunk, out var x, out var y);
            return new LocalPlanarCoordinate(global.EastingMetres - x, global.NorthingMetres - y);
        }

        public LocalPlanarCoordinate GlobalToAggregationBlockLocal(
            GlobalProjectedCoordinate global, GlobalChunkId block) => GlobalToChunkLocal(global, block);

        public GlobalProjectedCoordinate ChunkLocalToGlobal(LocalPlanarCoordinate local, GlobalChunkId chunk)
        {
            Chunks.GetGlobalOrigin(chunk, out var x, out var y);
            return new GlobalProjectedCoordinate(local.XMetres + x, local.YMetres + y);
        }

        public GlobalProjectedCoordinate AggregationBlockLocalToGlobal(
            LocalPlanarCoordinate local, GlobalChunkId block) => ChunkLocalToGlobal(local, block);

        public UnityLocalPosition GlobalToUnityLocal(GlobalProjectedCoordinate global,
            GlobalProjectedCoordinate floatingOrigin, double elevationMetres = 0d) =>
            new UnityLocalPosition(global.EastingMetres - floatingOrigin.EastingMetres,
                elevationMetres, global.NorthingMetres - floatingOrigin.NorthingMetres);

        public GlobalProjectedCoordinate UnityLocalToGlobal(UnityLocalPosition local,
            GlobalProjectedCoordinate floatingOrigin) => new GlobalProjectedCoordinate(
            local.XMetres + floatingOrigin.EastingMetres,
            local.ZMetres + floatingOrigin.NorthingMetres);

        private static double Radians(double degrees) => degrees * Math.PI / 180d;
        private static double Degrees(double radians) => radians * 180d / Math.PI;
        private static double M(double phi)
        {
            var sin = Math.Sin(phi);
            return Math.Cos(phi) / Math.Sqrt(1d - EccentricitySquared * sin * sin);
        }
        private static double Q(double phi)
        {
            var e = Math.Sqrt(EccentricitySquared);
            var sin = Math.Sin(phi);
            return (1d - EccentricitySquared) * (sin / (1d - EccentricitySquared * sin * sin) -
                Math.Log((1d - e * sin) / (1d + e * sin)) / (2d * e));
        }
        private double Rho(double q) => SemiMajor * Math.Sqrt(Math.Max(0d, _c - _n * q)) / _n;
    }
}
