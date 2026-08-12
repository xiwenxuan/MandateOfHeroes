namespace Mandate.Domain
{
    public readonly struct WorldMapCellRecord
    {
        public WorldMapCellRecord(
            WorldMapCellId id, int row, int column, double centerX, double centerY,
            short elevation, byte terrainClass, byte slopeClass, byte waterClass,
            ushort provinceCode, ushort commanderyCode, ushort countyCode, byte roadClass,
            string gridSchemaVersion = "hanworld.square-grid.v1")
        {
            Id = id;
            Row = row;
            Column = column;
            CenterX = centerX;
            CenterY = centerY;
            Elevation = elevation;
            TerrainClass = terrainClass;
            SlopeClass = slopeClass;
            WaterClass = waterClass;
            ProvinceCode = provinceCode;
            CommanderyCode = commanderyCode;
            CountyCode = countyCode;
            RoadClass = roadClass;
            Address = new WorldMapCellAddress(gridSchemaVersion, column, row, id.Value);
        }

        public WorldMapCellId Id { get; }
        public WorldMapCellAddress Address { get; }
        public ulong CellId64 => Address.CellId64;
        public string GridSchemaVersion => Address.GridSchemaVersion;
        public int GridX => Address.GridX;
        public int GridY => Address.GridY;
        public int Row { get; }
        public int Column { get; }
        public double CenterX { get; }
        public double CenterY { get; }
        public short Elevation { get; }
        public byte TerrainClass { get; }
        public byte SlopeClass { get; }
        public byte WaterClass { get; }
        public ushort ProvinceCode { get; }
        public ushort CommanderyCode { get; }
        public ushort CountyCode { get; }
        public byte RoadClass { get; }
        public bool Buildable => WaterClass == 0 && SlopeClass < 2 && TerrainClass < 4;
        public bool Passable => WaterClass == 0 && SlopeClass < 3;

        // Runtime world facts intentionally remain outside the immutable authored map package.
        public string OwnerId => null;
        public string FacilityId => null;
        public string ForceId => null;
    }
}
