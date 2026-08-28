using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class HanWorldTerrainGenerator
    {
        private readonly IGlobalNaturalCellSource _source;
        private readonly NaturalSurfaceClassifier _classifier;

        public HanWorldTerrainGenerator(IGlobalNaturalCellSource source,
            NaturalSurfaceClassifier classifier = null)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _classifier = classifier ?? new NaturalSurfaceClassifier();
        }

        public NaturalTerrainMeshData GenerateTile(TerrainTileDefinition tile,
            double elevationExaggeration = 1.35d)
        {
            if (elevationExaggeration <= 0d || elevationExaggeration > 8d)
                throw new ArgumentOutOfRangeException(nameof(elevationExaggeration));
            var vertexRows = tile.CellRows + 1;
            var vertexColumns = tile.CellColumns + 1;
            var vertices = new NaturalTerrainVertex[vertexRows * vertexColumns];
            long reads = 0;
            for (var localRow = 0; localRow < vertexRows; localRow++)
            {
                for (var localColumn = 0; localColumn < vertexColumns; localColumn++)
                {
                    var gridVertexRow = tile.FirstRow + localRow;
                    var gridVertexColumn = tile.FirstColumn + localColumn;
                    var sample = SampleGridVertex(gridVertexRow, gridVertexColumn, ref reads);
                    var x = _source.OriginX + gridVertexColumn * _source.CellSizeMetres;
                    var y = _source.OriginY - gridVertexRow * _source.CellSizeMetres;
                    vertices[localRow * vertexColumns + localColumn] = new NaturalTerrainVertex(
                        x, y, sample.Cell.Elevation, EnhanceElevation(sample.Cell.Elevation,
                            elevationExaggeration), _classifier.Classify(sample));
                }
            }

            var triangles = new int[tile.CellRows * tile.CellColumns * 6];
            var triangle = 0;
            for (var row = 0; row < tile.CellRows; row++)
            {
                for (var column = 0; column < tile.CellColumns; column++)
                {
                    var topLeft = row * vertexColumns + column;
                    var topRight = topLeft + 1;
                    var bottomLeft = topLeft + vertexColumns;
                    var bottomRight = bottomLeft + 1;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = bottomLeft;
                    triangles[triangle++] = topRight;
                    triangles[triangle++] = topRight;
                    triangles[triangle++] = bottomLeft;
                    triangles[triangle++] = bottomRight;
                }
            }

            return new NaturalTerrainMeshData
            {
                Tile = tile,
                Vertices = vertices,
                Triangles = triangles,
                SourceCellReadCount = reads
            };
        }

        public NaturalTerrainMeshData GenerateWindow(int firstRow, int firstColumn,
            int cellRows, int cellColumns, int sampleStep, double elevationExaggeration = 1.35d)
        {
            if (firstRow < 0 || firstColumn < 0 || cellRows <= 0 || cellColumns <= 0 || sampleStep <= 0)
                throw new ArgumentOutOfRangeException(nameof(firstRow));
            var lastRow = Math.Min(_source.Rows - 1, firstRow + cellRows - 1);
            var lastColumn = Math.Min(_source.Columns - 1, firstColumn + cellColumns - 1);
            var rowSegments = (lastRow - firstRow + sampleStep) / sampleStep;
            var columnSegments = (lastColumn - firstColumn + sampleStep) / sampleStep;
            var vertexRows = rowSegments + 1;
            var vertexColumns = columnSegments + 1;
            var vertices = new NaturalTerrainVertex[vertexRows * vertexColumns];
            long reads = 0;
            for (var localRow = 0; localRow < vertexRows; localRow++)
            {
                var gridVertexRow = Math.Min(lastRow + 1, firstRow + localRow * sampleStep);
                for (var localColumn = 0; localColumn < vertexColumns; localColumn++)
                {
                    var gridVertexColumn = Math.Min(lastColumn + 1, firstColumn + localColumn * sampleStep);
                    var sample = SampleGridVertex(gridVertexRow, gridVertexColumn, ref reads);
                    vertices[localRow * vertexColumns + localColumn] = new NaturalTerrainVertex(
                        _source.OriginX + gridVertexColumn * _source.CellSizeMetres,
                        _source.OriginY - gridVertexRow * _source.CellSizeMetres,
                        sample.Cell.Elevation,
                        EnhanceElevation(sample.Cell.Elevation, elevationExaggeration),
                        _classifier.Classify(sample));
                }
            }
            var triangles = new int[rowSegments * columnSegments * 6];
            var triangle = 0;
            for (var row = 0; row < rowSegments; row++)
                for (var column = 0; column < columnSegments; column++)
                {
                    var topLeft = row * vertexColumns + column;
                    var topRight = topLeft + 1;
                    var bottomLeft = topLeft + vertexColumns;
                    var bottomRight = bottomLeft + 1;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = bottomLeft;
                    triangles[triangle++] = topRight;
                    triangles[triangle++] = topRight;
                    triangles[triangle++] = bottomLeft;
                    triangles[triangle++] = bottomRight;
                }
            return new NaturalTerrainMeshData
            {
                Tile = new TerrainTileDefinition(new TerrainTileId(firstRow / sampleStep,
                        firstColumn / sampleStep), firstRow, lastRow, firstColumn, lastColumn,
                    _source.OriginX + firstColumn * _source.CellSizeMetres,
                    _source.OriginY - (lastRow + 1) * _source.CellSizeMetres,
                    _source.OriginX + (lastColumn + 1) * _source.CellSizeMetres,
                    _source.OriginY - firstRow * _source.CellSizeMetres),
                Vertices = vertices,
                Triangles = triangles,
                SourceCellReadCount = reads
            };
        }

        private NaturalMapCellSample SampleGridVertex(int gridVertexRow, int gridVertexColumn,
            ref long reads)
        {
            long elevationSum = 0;
            var elevationCount = 0;
            WorldMapCellRecord representative = default;
            var hasRepresentative = false;
            for (var rowOffset = -1; rowOffset <= 0; rowOffset++)
            {
                var row = gridVertexRow + rowOffset;
                if (row < 0 || row >= _source.Rows) continue;
                for (var columnOffset = -1; columnOffset <= 0; columnOffset++)
                {
                    var column = gridVertexColumn + columnOffset;
                    if (column < 0 || column >= _source.Columns) continue;
                    var sample = _source.ReadSample(row, column);
                    reads++;
                    if (!hasRepresentative || sample.Cell.WaterClass != 0)
                    {
                        representative = sample.Cell;
                        hasRepresentative = true;
                    }
                    if (sample.Cell.Elevation > -32000)
                    {
                        elevationSum += sample.Cell.Elevation;
                        elevationCount++;
                    }
                }
            }
            if (!hasRepresentative)
                throw new InvalidOperationException("Grid vertex has no adjacent source cell.");
            var elevation = (short)(elevationCount == 0
                ? 0
                : Math.Round(elevationSum / (double)elevationCount));
            representative = new WorldMapCellRecord(representative.Id, representative.Row,
                representative.Column, representative.CenterX, representative.CenterY, elevation,
                representative.TerrainClass, representative.SlopeClass, representative.WaterClass,
                representative.ProvinceCode, representative.CommanderyCode,
                representative.CountyCode, representative.RoadClass,
                representative.GridSchemaVersion);
            return new NaturalMapCellSample(representative, elevation);
        }

        private static double EnhanceElevation(double sourceElevation, double exaggeration)
        {
            if (sourceElevation <= 0d) return Math.Max(-60d, sourceElevation * 0.12d);
            var lowRelief = Math.Min(sourceElevation, 300d) * (0.65d + 0.35d * exaggeration);
            var mountain = Math.Max(0d, sourceElevation - 300d) * exaggeration;
            return lowRelief + mountain;
        }
    }
}
