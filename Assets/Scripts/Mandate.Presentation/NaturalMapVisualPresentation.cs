using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mandate.Domain;
using Mandate.Simulation;
using UnityEngine;

namespace Mandate.Presentation
{
    public enum NaturalTerrainLodLevel
    {
        World,
        RegionMid,
        FormalTile
    }

    public sealed class NaturalTerrainLodSet
    {
        public NaturalTerrainMeshData FarOrWorld;
        public readonly List<NaturalTerrainMeshData> FormalTiles = new List<NaturalTerrainMeshData>();
        public double GenerationMilliseconds;
    }

    public sealed class WorldTerrainLodController
    {
        private readonly IGlobalNaturalCellSource _source;
        private readonly HanWorldTerrainGenerator _generator;
        private readonly TerrainTileIndex _tileIndex;

        public WorldTerrainLodController(IGlobalNaturalCellSource source,
            HanWorldTerrainGenerator generator, TerrainTileIndex tileIndex)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _tileIndex = tileIndex ?? throw new ArgumentNullException(nameof(tileIndex));
        }

        public NaturalTerrainLodSet GenerateWorld(int sampleStep, double exaggeration)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new NaturalTerrainLodSet
            {
                FarOrWorld = _generator.GenerateWindow(0, 0, _source.Rows, _source.Columns,
                    sampleStep, exaggeration)
            };
            stopwatch.Stop();
            result.GenerationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            return result;
        }

        public NaturalTerrainLodSet GenerateRegion(int centerRow, int centerColumn,
            int residentTileRadius, int farSpanCells, int farSampleStep, double exaggeration)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new NaturalTerrainLodSet();
            var half = Math.Max(24, farSpanCells) / 2;
            var firstRow = Math.Max(0, centerRow - half);
            var firstColumn = Math.Max(0, centerColumn - half);
            var rows = Math.Min(_source.Rows - firstRow, Math.Max(24, farSpanCells));
            var columns = Math.Min(_source.Columns - firstColumn, Math.Max(24, farSpanCells));
            result.FarOrWorld = _generator.GenerateWindow(firstRow, firstColumn, rows, columns,
                Math.Max(1, farSampleStep), exaggeration);

            var centerTile = _tileIndex.FromCell(centerRow, centerColumn);
            var radius = Math.Max(1, residentTileRadius);
            for (var tileRow = Math.Max(0, centerTile.Row - radius);
                 tileRow <= Math.Min(_tileIndex.TileRows - 1, centerTile.Row + radius); tileRow++)
            for (var tileColumn = Math.Max(0, centerTile.Column - radius);
                 tileColumn <= Math.Min(_tileIndex.TileColumns - 1, centerTile.Column + radius); tileColumn++)
                result.FormalTiles.Add(_generator.GenerateTile(
                    _tileIndex.Get(new TerrainTileId(tileRow, tileColumn)), exaggeration));
            stopwatch.Stop();
            result.GenerationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            return result;
        }
    }

    public sealed class TerrainSurfaceBlendController
    {
        public Color32 Evaluate(NaturalTerrainVertex vertex)
        {
            var primary = Palette(vertex.Surface.PrimarySurfaceId);
            var secondary = Palette(vertex.Surface.SecondarySurfaceId);
            var colour = Color.Lerp(primary, secondary, (float)vertex.Surface.SecondaryWeight);
            var broad = ContinuousNoise(vertex.GlobalX / 96000d, vertex.GlobalY / 96000d, 0x51F2A3u);
            var fine = ContinuousNoise(vertex.GlobalX / 26000d, vertex.GlobalY / 26000d, 0xA9C31Du);
            var variation = (float)((broad - 0.5d) * 0.12d + (fine - 0.5d) * 0.05d);
            if (vertex.Surface.LandformId == NaturalLandformIds.Mountain) variation -= 0.06f;
            else if (vertex.Surface.LandformId == NaturalLandformIds.Valley) variation += 0.035f;
            colour = new Color(Mathf.Clamp01(colour.r + variation),
                Mathf.Clamp01(colour.g + variation * 0.82f),
                Mathf.Clamp01(colour.b + variation * 0.55f), 1f);
            return colour;
        }

        public static double ContinuousNoise(double x, double y, uint salt)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);
            var fx = x - ix;
            var fy = y - iy;
            fx = fx * fx * (3d - 2d * fx);
            fy = fy * fy * (3d - 2d * fy);
            var a = Hash(ix, iy, salt);
            var b = Hash(ix + 1, iy, salt);
            var c = Hash(ix, iy + 1, salt);
            var d = Hash(ix + 1, iy + 1, salt);
            return Lerp(Lerp(a, b, fx), Lerp(c, d, fx), fy);
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private static double Hash(int x, int y, uint salt)
        {
            unchecked
            {
                uint value = (uint)x * 0x8DA6B343u ^ (uint)y * 0xD8163841u ^ salt;
                value ^= value >> 13;
                value *= 0x85EBCA6Bu;
                value ^= value >> 16;
                return (value & 0xFFFFFFu) / 16777215d;
            }
        }

        private static Color Palette(string id)
        {
            if (id == NaturalSurfaceIds.Sea) return new Color32(64, 119, 143, 255);
            if (id == NaturalSurfaceIds.River || id == NaturalSurfaceIds.Lake) return new Color32(62, 132, 163, 255);
            if (id == NaturalSurfaceIds.Wetland) return new Color32(91, 126, 103, 255);
            if (id == NaturalSurfaceIds.Riverbank) return new Color32(151, 145, 103, 255);
            if (id == NaturalSurfaceIds.Forest) return new Color32(47, 83, 48, 255);
            if (id == NaturalSurfaceIds.SparseWoodland) return new Color32(78, 112, 64, 255);
            if (id == NaturalSurfaceIds.Rock) return new Color32(108, 105, 96, 255);
            if (id == NaturalSurfaceIds.BareLand) return new Color32(139, 123, 91, 255);
            if (id == NaturalSurfaceIds.Sand) return new Color32(184, 161, 111, 255);
            return new Color32(119, 142, 78, 255);
        }
    }

    public readonly struct NaturalMapCameraPreset
    {
        public NaturalMapCameraPreset(string id, int row, int column, float size, float pitch, float yaw)
        {
            Id = id; Row = row; Column = column; Size = size; Pitch = pitch; Yaw = yaw;
        }
        public string Id { get; }
        public int Row { get; }
        public int Column { get; }
        public float Size { get; }
        public float Pitch { get; }
        public float Yaw { get; }
    }

    public static class VisualAcceptanceCameraRig
    {
        public const string WorldFull = "CAM_WORLD_FULL";
        public const string WorldNorthChina = "CAM_WORLD_NORTH_CHINA";
        public const string HenanRegion = "CAM_HENAN_YIN_REGION";
        public const string HenanMountain = "CAM_HENAN_MOUNTAIN";
        public const string HenanRiver = "CAM_HENAN_RIVER";
        public const string HenanForest = "CAM_HENAN_FOREST";
        public const string TileSeam = "CAM_TILE_SEAM_TEST";

        public static NaturalMapCameraPreset Get(string id)
        {
            switch (id)
            {
                case WorldFull: return new NaturalMapCameraPreset(id, 1088, 1657, 1160f, 68f, 0f);
                case WorldNorthChina: return new NaturalMapCameraPreset(id, 1110, 2090, 520f, 66f, -5f);
                case HenanRegion: return new NaturalMapCameraPreset(id, 1247, 1992, 34f, 58f, -12f);
                case HenanMountain: return new NaturalMapCameraPreset(id, 1390, 1710, 26f, 56f, -18f);
                case HenanRiver: return new NaturalMapCameraPreset(id, 1209, 2148, 22f, 58f, -10f);
                case HenanForest: return new NaturalMapCameraPreset(id, 1460, 1970, 22f, 55f, 12f);
                case TileSeam: return new NaturalMapCameraPreset(id, 1241, 2043, 17f, 55f, -16f);
                default: throw new ArgumentException("Unknown natural-map camera preset: " + id, nameof(id));
            }
        }
    }

    public sealed class NaturalMapPerformanceSnapshot
    {
        public string Mode;
        public string ArtProfileId;
        public double TerrainGenerationMilliseconds;
        public double ObservedFrameMilliseconds;
        public double CpuFrameMilliseconds;
        public double GpuFrameMilliseconds;
        public int ResidentTerrainMeshes;
        public long TerrainMeshBytes;
        public int VegetationDrawBatches;
        public int RiverMeshCount;
        public long ManagedGcDeltaBytes;
        public double WorldRegionTransitionMilliseconds;
        public int DrawCalls;
        public int MaterialCount;
        public int ShaderVariantCount;
        public string VisualDetailLevel;
        public long VisualTerrainVertices;
        public int RiverAdaptiveSamples;
        public int RiverBevelFallbacks;
    }
}
