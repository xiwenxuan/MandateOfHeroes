using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Presentation
{
    public enum VisualTerrainDetailLevel
    {
        World,
        Region,
        City,
        ClosePreview
    }

    public readonly struct VisualTerrainDetailProfile
    {
        public VisualTerrainDetailProfile(VisualTerrainDetailLevel level, int subdivisionsPerCell,
            double microReliefAmplitudeMetres)
        {
            Level = level;
            SubdivisionsPerCell = subdivisionsPerCell;
            MicroReliefAmplitudeMetres = microReliefAmplitudeMetres;
        }

        public VisualTerrainDetailLevel Level { get; }
        public int SubdivisionsPerCell { get; }
        public double MicroReliefAmplitudeMetres { get; }
        public double VisualSampleSpacingMetres => 2000d / SubdivisionsPerCell;
        public bool CreatesSimulationSubCells => false;
    }

    public static class VisualTerrainDetailCatalog
    {
        public const string ContractId = "presentation.han-world.visual-terrain-detail.v2";

        public static VisualTerrainDetailProfile Get(VisualTerrainDetailLevel level)
        {
            switch (level)
            {
                case VisualTerrainDetailLevel.World:
                    return new VisualTerrainDetailProfile(level, 1, 0d);
                case VisualTerrainDetailLevel.Region:
                    return new VisualTerrainDetailProfile(level, 2, 5d);
                case VisualTerrainDetailLevel.City:
                    return new VisualTerrainDetailProfile(level, 4, 12d);
                case VisualTerrainDetailLevel.ClosePreview:
                    return new VisualTerrainDetailProfile(level, 8, 18d);
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }

    /// <summary>
    /// Refines a derived presentation tile without creating cells or changing authoritative DEM facts.
    /// Source elevations are only interpolated; deterministic micro relief is written exclusively to
    /// PresentationElevationMetres and is continuous in global projected coordinates.
    /// </summary>
    public sealed class VisualTerrainDetailGenerator
    {
        public NaturalTerrainMeshData Refine(NaturalTerrainMeshData source,
            VisualTerrainDetailProfile profile)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (profile.SubdivisionsPerCell < 1 || profile.SubdivisionsPerCell > 16)
                throw new ArgumentOutOfRangeException(nameof(profile));
            var sourceRows = source.Tile.CellRows + 1;
            var sourceColumns = source.Tile.CellColumns + 1;
            if (source.Vertices == null || source.Vertices.Length != sourceRows * sourceColumns)
                throw new ArgumentException("Terrain tile is not a regular authoritative Cell-vertex grid.",
                    nameof(source));

            var factor = profile.SubdivisionsPerCell;
            var rows = source.Tile.CellRows * factor + 1;
            var columns = source.Tile.CellColumns * factor + 1;
            var vertices = new NaturalTerrainVertex[rows * columns];
            for (var row = 0; row < rows; row++)
            {
                var sourceRow = Math.Min(source.Tile.CellRows - 1, row / factor);
                var rowT = row == rows - 1 ? 1d : row % factor / (double)factor;
                for (var column = 0; column < columns; column++)
                {
                    var sourceColumn = Math.Min(source.Tile.CellColumns - 1, column / factor);
                    var columnT = column == columns - 1 ? 1d : column % factor / (double)factor;
                    var topLeft = source.Vertices[sourceRow * sourceColumns + sourceColumn];
                    var topRight = source.Vertices[sourceRow * sourceColumns + sourceColumn + 1];
                    var bottomLeft = source.Vertices[(sourceRow + 1) * sourceColumns + sourceColumn];
                    var bottomRight = source.Vertices[(sourceRow + 1) * sourceColumns + sourceColumn + 1];
                    var globalX = Bilinear(topLeft.GlobalX, topRight.GlobalX, bottomLeft.GlobalX,
                        bottomRight.GlobalX, columnT, rowT);
                    var globalY = Bilinear(topLeft.GlobalY, topRight.GlobalY, bottomLeft.GlobalY,
                        bottomRight.GlobalY, columnT, rowT);
                    var authoritative = Bilinear(topLeft.SourceElevationMetres,
                        topRight.SourceElevationMetres, bottomLeft.SourceElevationMetres,
                        bottomRight.SourceElevationMetres, columnT, rowT);
                    var presentation = Bilinear(topLeft.PresentationElevationMetres,
                        topRight.PresentationElevationMetres, bottomLeft.PresentationElevationMetres,
                        bottomRight.PresentationElevationMetres, columnT, rowT);
                    var surface = BlendSurface(topLeft, topRight, bottomLeft, bottomRight,
                        columnT, rowT);
                    presentation += MicroRelief(globalX, globalY, surface,
                        profile.MicroReliefAmplitudeMetres);
                    vertices[row * columns + column] = new NaturalTerrainVertex(globalX, globalY,
                        authoritative, presentation, surface);
                }
            }

            var triangles = new int[(rows - 1) * (columns - 1) * 6];
            var triangle = 0;
            for (var row = 0; row < rows - 1; row++)
            for (var column = 0; column < columns - 1; column++)
            {
                var topLeft = row * columns + column;
                var topRight = topLeft + 1;
                var bottomLeft = topLeft + columns;
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
                Tile = source.Tile,
                Vertices = vertices,
                Triangles = triangles,
                SourceCellReadCount = source.SourceCellReadCount
            };
        }

        public static double MicroRelief(double globalX, double globalY, NaturalSurfaceBlend surface,
            double amplitudeMetres)
        {
            if (amplitudeMetres <= 0d) return 0d;
            var broad = TerrainSurfaceBlendController.ContinuousNoise(globalX / 1700d,
                globalY / 1700d, 0x7D41A9u) - 0.5d;
            var fine = TerrainSurfaceBlendController.ContinuousNoise(globalX / 620d,
                globalY / 620d, 0x19CE53u) - 0.5d;
            var waterAttenuation = surface.PrimarySurfaceId == NaturalSurfaceIds.River ||
                                   surface.PrimarySurfaceId == NaturalSurfaceIds.Lake ||
                                   surface.PrimarySurfaceId == NaturalSurfaceIds.Sea ? 0.08d :
                surface.PrimarySurfaceId == NaturalSurfaceIds.Riverbank ||
                surface.PrimarySurfaceId == NaturalSurfaceIds.Wetland ? 0.32d : 1d;
            return (broad * 0.72d + fine * 0.28d) * amplitudeMetres * waterAttenuation;
        }

        private static NaturalSurfaceBlend BlendSurface(NaturalTerrainVertex topLeft,
            NaturalTerrainVertex topRight, NaturalTerrainVertex bottomLeft,
            NaturalTerrainVertex bottomRight, double x, double y)
        {
            var values = new[] { topLeft.Surface, topRight.Surface, bottomLeft.Surface, bottomRight.Surface };
            var weights = new[] { (1d - x) * (1d - y), x * (1d - y), (1d - x) * y, x * y };
            var surfaces = new Dictionary<string, double>(StringComparer.Ordinal);
            var landforms = new Dictionary<string, double>(StringComparer.Ordinal);
            for (var index = 0; index < values.Length; index++)
            {
                AddWeight(surfaces, values[index].PrimarySurfaceId,
                    weights[index] * (1d - values[index].SecondaryWeight));
                AddWeight(surfaces, values[index].SecondarySurfaceId,
                    weights[index] * values[index].SecondaryWeight);
                AddWeight(landforms, values[index].LandformId, weights[index]);
            }
            var primary = MaximumKey(surfaces, null);
            var secondary = MaximumKey(surfaces, primary) ?? primary;
            var primaryWeight = surfaces[primary];
            var secondaryWeight = secondary == primary ? 0d : surfaces[secondary];
            var blend = secondaryWeight <= 0d ? 0d : secondaryWeight /
                Math.Max(0.000001d, primaryWeight + secondaryWeight);
            return new NaturalSurfaceBlend(primary, secondary, MaximumKey(landforms, null), blend);
        }

        private static void AddWeight(IDictionary<string, double> values, string key, double amount)
        {
            if (string.IsNullOrWhiteSpace(key) || amount <= 0d) return;
            values[key] = values.TryGetValue(key, out var existing) ? existing + amount : amount;
        }

        private static string MaximumKey(IReadOnlyDictionary<string, double> values, string excluded)
        {
            string selected = null;
            var maximum = double.MinValue;
            foreach (var pair in values)
            {
                if (string.Equals(pair.Key, excluded, StringComparison.Ordinal)) continue;
                if (pair.Value <= maximum) continue;
                maximum = pair.Value;
                selected = pair.Key;
            }
            return selected;
        }

        private static double Bilinear(double topLeft, double topRight, double bottomLeft,
            double bottomRight, double x, double y) =>
            Lerp(Lerp(topLeft, topRight, x), Lerp(bottomLeft, bottomRight, x), y);

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    }
}
