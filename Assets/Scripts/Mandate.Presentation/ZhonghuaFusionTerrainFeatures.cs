using System;
using System.Collections.Generic;
using Mandate.Domain;
using UnityEngine;

namespace Mandate.Presentation
{
    /// <summary>
    /// Clean-room, presentation-only feature extraction over an already generated terrain mesh.
    /// Results are deterministic and never alter authoritative elevations, surfaces or Global Cells.
    /// </summary>
    public sealed class ZhonghuaFusionTerrainFeatureSet
    {
        public readonly List<Vector4> Primary = new List<Vector4>();
        public readonly List<Vector4> Secondary = new List<Vector4>();
        public int VertexRows;
        public int VertexColumns;
        public int MountainVertices;
        public int ValleyVertices;
        public int ForestVertices;
        public int RiverValleyVertices;
    }

    public static class ZhonghuaFusionTerrainFeatureAnalyzer
    {
        public static ZhonghuaFusionTerrainFeatureSet Analyze(NaturalTerrainMeshData data)
        {
            if (data?.Vertices == null || data.Vertices.Length == 0)
                throw new ArgumentException("Terrain mesh must contain vertices.", nameof(data));
            var columns = DetectVertexColumns(data.Vertices);
            if (columns <= 1 || data.Vertices.Length % columns != 0)
                throw new InvalidOperationException("Terrain vertex grid is not rectangular.");
            var rows = data.Vertices.Length / columns;
            var result = new ZhonghuaFusionTerrainFeatureSet
                { VertexRows = rows, VertexColumns = columns };
            result.Primary.Capacity = data.Vertices.Length;
            result.Secondary.Capacity = data.Vertices.Length;

            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                var index = row * columns + column;
                var center = data.Vertices[index];
                var elevation = center.SourceElevationMetres;
                var left = E(data, rows, columns, row, column - 1);
                var right = E(data, rows, columns, row, column + 1);
                var north = E(data, rows, columns, row - 1, column);
                var south = E(data, rows, columns, row + 1, column);
                var neighbourMean = (left + right + north + south) * 0.25d;
                var spacingX = Math.Max(1d, Math.Abs(data.Vertices[Math.Min(index + 1,
                    row * columns + columns - 1)].GlobalX - center.GlobalX));
                var spacingY = Math.Max(1d, Math.Abs(data.Vertices[Math.Min(index + columns,
                    data.Vertices.Length - 1)].GlobalY - center.GlobalY));
                var gradientX = (right - left) / (2d * spacingX);
                var gradientY = (south - north) / (2d * spacingY);
                var slope = Clamp01(Math.Sqrt(gradientX * gradientX + gradientY * gradientY) / 0.42d);
                LocalRange(data, rows, columns, row, column, out var localMin, out var localMax);
                var relief = Clamp01((localMax - localMin) / 720d);
                var convexity = elevation - neighbourMean;
                var ridge = Clamp01(Math.Max(0d, convexity) / 170d) * (0.35d + 0.65d * Math.Max(slope, relief));
                var valley = Clamp01(Math.Max(0d, -convexity) / 135d) * (0.30d + 0.70d * Math.Max(slope, relief));
                var heightMass = SmoothStep(220d, 1320d, elevation);
                var mountain = Clamp01(heightMass * 0.68d + slope * 0.18d + relief * 0.34d);
                var plain = Clamp01((1d - mountain) * (1d - slope * 0.82d) * (1d - relief * 0.70d));
                var forest = SurfaceWeight(center.Surface, NaturalSurfaceIds.Forest,
                    NaturalSurfaceIds.SparseWoodland);
                var water = SurfaceWeight(center.Surface, NaturalSurfaceIds.River,
                    NaturalSurfaceIds.Lake, NaturalSurfaceIds.Riverbank, NaturalSurfaceIds.Wetland);
                var riverValley = Clamp01(Math.Max(water, valley * (0.55d + 0.45d * plain)));
                var basin = Clamp01(valley * plain * 1.35d);

                var primary = new Vector4((float)ridge, (float)valley, (float)mountain, (float)plain);
                var secondary = new Vector4((float)forest, (float)riverValley, (float)relief, (float)basin);
                result.Primary.Add(primary);
                result.Secondary.Add(secondary);
                if (mountain >= 0.45d) result.MountainVertices++;
                if (valley >= 0.30d) result.ValleyVertices++;
                if (forest >= 0.30d) result.ForestVertices++;
                if (riverValley >= 0.30d) result.RiverValleyVertices++;
            }
            return result;
        }

        private static int DetectVertexColumns(NaturalTerrainVertex[] vertices)
        {
            var firstY = vertices[0].GlobalY;
            var columns = 1;
            while (columns < vertices.Length && Math.Abs(vertices[columns].GlobalY - firstY) < 0.001d)
                columns++;
            return columns;
        }

        private static double E(NaturalTerrainMeshData data, int rows, int columns, int row, int column)
        {
            row = Math.Max(0, Math.Min(rows - 1, row));
            column = Math.Max(0, Math.Min(columns - 1, column));
            return data.Vertices[row * columns + column].SourceElevationMetres;
        }

        private static void LocalRange(NaturalTerrainMeshData data, int rows, int columns,
            int row, int column, out double minimum, out double maximum)
        {
            minimum = double.MaxValue;
            maximum = double.MinValue;
            for (var rowOffset = -1; rowOffset <= 1; rowOffset++)
            for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                var elevation = E(data, rows, columns, row + rowOffset, column + columnOffset);
                minimum = Math.Min(minimum, elevation);
                maximum = Math.Max(maximum, elevation);
            }
        }

        private static double SurfaceWeight(NaturalSurfaceBlend blend, params string[] ids)
        {
            var result = 0d;
            foreach (var id in ids)
            {
                if (blend.PrimarySurfaceId == id) result = Math.Max(result, 1d - blend.SecondaryWeight);
                if (blend.SecondarySurfaceId == id) result = Math.Max(result, blend.SecondaryWeight);
            }
            return result;
        }

        private static double SmoothStep(double minimum, double maximum, double value)
        {
            var t = Clamp01((value - minimum) / (maximum - minimum));
            return t * t * (3d - 2d * t);
        }

        private static double Clamp01(double value) => Math.Max(0d, Math.Min(1d, value));
    }
}
