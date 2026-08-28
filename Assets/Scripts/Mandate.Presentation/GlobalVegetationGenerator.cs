using System;
using System.Collections.Generic;
using Mandate.Domain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mandate.Presentation
{
    public enum ForestPresentationLod
    {
        RegionCanopyCluster,
        CityIndividualTrees
    }

    public sealed class GlobalVegetationGenerator
    {
        public Mesh BuildCombinedMesh(IGlobalNaturalCellSource source, int firstRow,
            int firstColumn, int cellRows, int cellColumns,
            GlobalProjectedCoordinate floatingOrigin, double horizontalMetresPerUnit,
            double verticalMetresPerUnit,
            Func<double, double, float> heightProvider = null,
            int latticePerCell = 2,
            float canopyScale = 1f,
            ForestPresentationLod lod = ForestPresentationLod.CityIndividualTrees)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            latticePerCell = lod == ForestPresentationLod.RegionCanopyCluster
                ? Math.Max(1, Math.Min(2, latticePerCell))
                : Math.Max(3, Math.Min(6, latticePerCell));
            var densitySampler = new GlobalForestDensitySampler(source);
            var vertices = new List<Vector3>();
            var colours = new List<Color32>();
            var triangles = new List<int>();
            var firstLatticeRow = Math.Max(0, firstRow * latticePerCell);
            var firstLatticeColumn = Math.Max(0, firstColumn * latticePerCell);
            var lastLatticeRow = Math.Min(source.Rows * latticePerCell,
                (firstRow + cellRows) * latticePerCell);
            var lastLatticeColumn = Math.Min(source.Columns * latticePerCell,
                (firstColumn + cellColumns) * latticePerCell);
            for (var latticeRow = firstLatticeRow; latticeRow < lastLatticeRow; latticeRow++)
            {
                for (var latticeColumn = firstLatticeColumn; latticeColumn < lastLatticeColumn; latticeColumn++)
                {
                    var rowPosition = (latticeRow + 0.5d) / latticePerCell;
                    var columnPosition = (latticeColumn + 0.5d) / latticePerCell;
                    var density = densitySampler.Sample(rowPosition, columnPosition);
                    var acceptance = StableUnit(latticeRow, latticeColumn, 0x5B);
                    var minimumDensity = lod == ForestPresentationLod.RegionCanopyCluster ? 0.24d : 0.16d;
                    var acceptanceScale = lod == ForestPresentationLod.RegionCanopyCluster ? 0.19d : 0.62d;
                    if (density < minimumDensity || acceptance > density * acceptanceScale) continue;
                    var jitterX = StableJitter(latticeRow, latticeColumn, 0, 17) /
                                  latticePerCell * 0.82d;
                    var jitterY = StableJitter(latticeRow, latticeColumn, 0, 29) /
                                  latticePerCell * 0.82d;
                    var globalX = source.OriginX + (columnPosition + jitterX) * source.CellSizeMetres;
                    var globalY = source.OriginY - (rowPosition + jitterY) * source.CellSizeMetres;
                    var x = (float)((globalX - floatingOrigin.EastingMetres) / horizontalMetresPerUnit);
                    var z = (float)((globalY - floatingOrigin.NorthingMetres) / horizontalMetresPerUnit);
                    var y = heightProvider?.Invoke(globalX, globalY) ??
                            (float)(Math.Max(0d, source.ReadSample(
                                Math.Min(source.Rows - 1, (int)rowPosition),
                                Math.Min(source.Columns - 1, (int)columnPosition)).Cell.Elevation) /
                                    verticalMetresPerUnit);
                    var scaleNoise = (float)StableUnit(latticeRow, latticeColumn, 0xA7);
                    var scale = lod == ForestPresentationLod.RegionCanopyCluster
                        ? Mathf.Lerp(0.30f, 0.72f, (float)density) * Mathf.Lerp(0.85f, 1.20f, scaleNoise)
                        : Mathf.Lerp(0.085f, 0.19f, (float)density) * Mathf.Lerp(0.72f, 1.24f, scaleNoise);
                    scale *= Mathf.Clamp(canopyScale, 0.60f, 1.80f);
                    AddCanopy(vertices, colours, triangles, new Vector3(x, y + 0.015f, z), scale,
                        (float)density, latticeRow, latticeColumn, lod);
                }
            }
            var mesh = new Mesh { name = "Batched Natural Vegetation" };
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetColors(colours); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddCanopy(List<Vector3> vertices, List<Color32> colours,
            List<int> triangles, Vector3 root, float scale, float density, int row, int column,
            ForestPresentationLod lod)
        {
            var start = vertices.Count;
            var sides = lod == ForestPresentationLod.RegionCanopyCluster ? 11 : 7;
            var rotation = (float)(StableUnit(row, column, 0xD1) * Mathf.PI * 2f);
            var baseColour = Color32.Lerp(new Color32(67, 104, 55, 255),
                new Color32(35, 72, 42, 255), density);
            for (var side = 0; side < sides; side++)
            {
                var angle = rotation + side * Mathf.PI * 2f / sides;
                var radiusVariation = Mathf.Lerp(0.78f, 1.20f,
                    (float)StableUnit(row, column, (uint)(side * 31 + 7)));
                vertices.Add(root + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * scale * radiusVariation);
                colours.Add(baseColour);
            }
            var height = lod == ForestPresentationLod.RegionCanopyCluster
                ? scale * Mathf.Lerp(0.72f, 1.20f, density)
                : scale * Mathf.Lerp(3.4f, 5.2f, density);
            vertices.Add(root + new Vector3(0f, height, 0f));
            colours.Add(Color32.Lerp(baseColour, new Color32(76, 119, 62, 255), 0.35f));
            for (var side = 0; side < sides; side++)
            {
                triangles.Add(start + side);
                triangles.Add(start + (side + 1) % sides);
                triangles.Add(start + sides);
            }
        }

        private static double StableJitter(int row, int column, int instance, int salt)
        {
            unchecked
            {
                uint value = (uint)(row * 73856093) ^ (uint)(column * 19349663) ^
                             (uint)(instance * 83492791) ^ (uint)salt;
                value ^= value >> 13;
                return (value & 0xFFFFu) / 65535d - 0.5d;
            }
        }

        private static double StableUnit(int row, int column, uint salt)
        {
            unchecked
            {
                uint value = (uint)row * 0x8DA6B343u ^ (uint)column * 0xD8163841u ^ salt;
                value ^= value >> 13;
                value *= 0x85EBCA6Bu;
                value ^= value >> 16;
                return (value & 0xFFFFFFu) / 16777215d;
            }
        }
    }

    public sealed class GlobalForestDensitySampler
    {
        private readonly IGlobalNaturalCellSource _source;
        private readonly NaturalSurfaceClassifier _classifier = new NaturalSurfaceClassifier();

        public GlobalForestDensitySampler(IGlobalNaturalCellSource source) =>
            _source = source ?? throw new ArgumentNullException(nameof(source));

        public double Sample(double rowPosition, double columnPosition)
        {
            var row0 = Math.Max(0, Math.Min(_source.Rows - 1, (int)Math.Floor(rowPosition)));
            var column0 = Math.Max(0, Math.Min(_source.Columns - 1, (int)Math.Floor(columnPosition)));
            var row1 = Math.Min(_source.Rows - 1, row0 + 1);
            var column1 = Math.Min(_source.Columns - 1, column0 + 1);
            var rowT = Math.Max(0d, Math.Min(1d, rowPosition - Math.Floor(rowPosition)));
            var columnT = Math.Max(0d, Math.Min(1d, columnPosition - Math.Floor(columnPosition)));
            rowT = rowT * rowT * (3d - 2d * rowT);
            columnT = columnT * columnT * (3d - 2d * columnT);
            var a = BaseDensity(row0, column0);
            var b = BaseDensity(row0, column1);
            var c = BaseDensity(row1, column0);
            var d = BaseDensity(row1, column1);
            var interpolated = Lerp(Lerp(a, b, columnT), Lerp(c, d, columnT), rowT);
            var globalX = _source.OriginX + columnPosition * _source.CellSizeMetres;
            var globalY = _source.OriginY - rowPosition * _source.CellSizeMetres;
            var broad = TerrainSurfaceBlendController.ContinuousNoise(globalX / 34000d,
                globalY / 34000d, 0x3C19D2u);
            var fine = TerrainSurfaceBlendController.ContinuousNoise(globalX / 11000d,
                globalY / 11000d, 0x73B5A1u);
            var clearing = TerrainSurfaceBlendController.ContinuousNoise(globalX / 5200d,
                globalY / 5200d, 0xC1925Fu);
            var clearingFactor = clearing > 0.79d ? 0.12d : clearing > 0.70d ? 0.48d : 1d;
            return Math.Max(0d, Math.Min(1d, (interpolated * (0.52d + broad * 0.72d) +
                                                     (fine - 0.5d) * 0.20d) * clearingFactor));
        }

        private double BaseDensity(int row, int column)
        {
            var sample = _source.ReadSample(row, column);
            var surface = _classifier.Classify(sample);
            if ((sample.Cell.WaterClass & 7) != 0) return 0d;
            if (surface.PrimarySurfaceId == NaturalSurfaceIds.Forest) return 0.88d;
            if (surface.PrimarySurfaceId == NaturalSurfaceIds.SparseWoodland) return 0.56d;
            if (surface.SecondarySurfaceId == NaturalSurfaceIds.SparseWoodland) return 0.26d;
            var moisture = NeighbourWater(row, column) ? 0.08d : 0d;
            var mountainBelt = sample.Cell.SlopeClass >= 1 && sample.Cell.Elevation > 260 &&
                               sample.Cell.Elevation < 2200 ? 0.18d : 0.06d;
            return Math.Min(0.34d, mountainBelt + moisture);
        }

        private bool NeighbourWater(int row, int column)
        {
            for (var rowOffset = -1; rowOffset <= 1; rowOffset++)
            for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                var sampleRow = Math.Max(0, Math.Min(_source.Rows - 1, row + rowOffset));
                var sampleColumn = Math.Max(0, Math.Min(_source.Columns - 1, column + columnOffset));
                if ((_source.ReadSample(sampleRow, sampleColumn).Cell.WaterClass & 7) != 0) return true;
            }
            return false;
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    }
}
