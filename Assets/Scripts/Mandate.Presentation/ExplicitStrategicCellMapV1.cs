using System;
using System.Collections.Generic;
using Mandate.Domain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mandate.Presentation
{
    public static class ExplicitStrategicCellMapV1
    {
        public const string ContractId = "presentation.han-world.explicit-strategic-cell-map.v1";
        public const string NationwideContractId =
            "presentation.han-world.nationwide-strategic-cell-grid-lod.v1";
        public const string SourceGridSchema = "hanworld.square-grid.v1";
        public const int SourceCellSizeMetres = 2000;
        public const int ReviewWindowCells = 24;
        public const int NationwideOverviewStepCells = 32;
        public const bool CreatesSimulationSubCells = false;

        public static readonly Color32 NormalFace = new Color32(205, 188, 132, 22);
        public static readonly Color32 HoverFace = new Color32(72, 184, 164, 92);
        public static readonly Color32 SelectedFace = new Color32(239, 168, 55, 132);
        public static readonly Color32 GridEdge = new Color32(47, 38, 27, 188);
        public static readonly Color32 HoverEdge = new Color32(92, 226, 200, 235);
        public static readonly Color32 SelectedEdge = new Color32(255, 195, 70, 255);
        public static readonly Color32 NationwideGuideEdge = new Color32(56, 47, 32, 158);

        public static StrategicCellOverlayGeometry BuildNationwideOverviewGeometry(
            CellGridIndex grid, int stepCells, GlobalProjectedCoordinate floatingOrigin,
            double horizontalMetresPerUnit, Func<double, double, float> heightSampler)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (stepCells <= 1) throw new ArgumentOutOfRangeException(nameof(stepCells));
            if (horizontalMetresPerUnit <= 0d)
                throw new ArgumentOutOfRangeException(nameof(horizontalMetresPerUnit));
            if (heightSampler == null) throw new ArgumentNullException(nameof(heightSampler));

            var geometry = new StrategicCellOverlayGeometry
            {
                CoveredCellCount = grid.CellCount,
                DisplayStepCells = stepCells
            };
            var rowSegments = (grid.Rows + stepCells - 1) / stepCells;
            var columnSegments = (grid.Columns + stepCells - 1) / stepCells;
            var edgeWidthMetres = grid.CellSize * stepCells * 0.055d;
            for (var rowSegment = 0; rowSegment <= rowSegments; rowSegment++)
            {
                var row = Math.Min(grid.Rows, rowSegment * stepCells);
                var y = grid.OriginY - row * grid.CellSize;
                for (var columnSegment = 0; columnSegment < columnSegments; columnSegment++)
                {
                    var firstColumn = columnSegment * stepCells;
                    var lastColumn = Math.Min(grid.Columns, firstColumn + stepCells);
                    AddEdgeRibbon(geometry, grid.OriginX + firstColumn * grid.CellSize, y,
                        grid.OriginX + lastColumn * grid.CellSize, y, edgeWidthMetres,
                        floatingOrigin, horizontalMetresPerUnit, heightSampler,
                        NationwideGuideEdge, 0.048f);
                    geometry.UniqueGridEdgeCount++;
                }
            }
            for (var columnSegment = 0; columnSegment <= columnSegments; columnSegment++)
            {
                var column = Math.Min(grid.Columns, columnSegment * stepCells);
                var x = grid.OriginX + column * grid.CellSize;
                for (var rowSegment = 0; rowSegment < rowSegments; rowSegment++)
                {
                    var firstRow = rowSegment * stepCells;
                    var lastRow = Math.Min(grid.Rows, firstRow + stepCells);
                    AddEdgeRibbon(geometry, x, grid.OriginY - firstRow * grid.CellSize,
                        x, grid.OriginY - lastRow * grid.CellSize, edgeWidthMetres,
                        floatingOrigin, horizontalMetresPerUnit, heightSampler,
                        NationwideGuideEdge, 0.048f);
                    geometry.UniqueGridEdgeCount++;
                }
            }
            return geometry;
        }

        public static StrategicCellOverlayGeometry BuildGeometry(CellGridIndex grid,
            int firstRow, int firstColumn, int rows, int columns,
            GlobalProjectedCoordinate floatingOrigin, double horizontalMetresPerUnit,
            Func<double, double, float> heightSampler, WorldMapCellId? hoveredCell,
            WorldMapCellId? selectedCell)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (rows <= 0 || columns <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
            if (horizontalMetresPerUnit <= 0d)
                throw new ArgumentOutOfRangeException(nameof(horizontalMetresPerUnit));
            if (heightSampler == null) throw new ArgumentNullException(nameof(heightSampler));

            var minRow = Math.Max(0, firstRow);
            var minColumn = Math.Max(0, firstColumn);
            var maxRow = Math.Min(grid.Rows, firstRow + rows);
            var maxColumn = Math.Min(grid.Columns, firstColumn + columns);
            var geometry = new StrategicCellOverlayGeometry();
            if (minRow >= maxRow || minColumn >= maxColumn) return geometry;

            var insetMetres = grid.CellSize * 0.055d;
            for (var row = minRow; row < maxRow; row++)
            {
                for (var column = minColumn; column < maxColumn; column++)
                {
                    var id = grid.ToCellId(row, column);
                    var faceColour = id == selectedCell ? SelectedFace :
                        id == hoveredCell ? HoverFace : NormalFace;
                    AddCellFace(geometry, grid, row, column, insetMetres, floatingOrigin,
                        horizontalMetresPerUnit, heightSampler, faceColour);
                    geometry.VisibleCellIds.Add(id);
                }
            }

            var edgeWidthMetres = grid.CellSize * 0.024d;
            for (var row = minRow; row <= maxRow; row++)
            {
                var y = grid.OriginY - row * grid.CellSize;
                for (var column = minColumn; column < maxColumn; column++)
                {
                    AddEdgeRibbon(geometry, grid.OriginX + column * grid.CellSize, y,
                        grid.OriginX + (column + 1) * grid.CellSize, y, edgeWidthMetres,
                        floatingOrigin, horizontalMetresPerUnit, heightSampler, GridEdge, 0.035f);
                    geometry.UniqueGridEdgeCount++;
                }
            }
            for (var column = minColumn; column <= maxColumn; column++)
            {
                var x = grid.OriginX + column * grid.CellSize;
                for (var row = minRow; row < maxRow; row++)
                {
                    AddEdgeRibbon(geometry, x, grid.OriginY - row * grid.CellSize,
                        x, grid.OriginY - (row + 1) * grid.CellSize, edgeWidthMetres,
                        floatingOrigin, horizontalMetresPerUnit, heightSampler, GridEdge, 0.035f);
                    geometry.UniqueGridEdgeCount++;
                }
            }

            AddHighlightOutline(geometry, grid, hoveredCell, HoverEdge, minRow, minColumn,
                maxRow, maxColumn, floatingOrigin, horizontalMetresPerUnit, heightSampler);
            if (!Nullable.Equals(hoveredCell, selectedCell))
                AddHighlightOutline(geometry, grid, selectedCell, SelectedEdge, minRow, minColumn,
                    maxRow, maxColumn, floatingOrigin, horizontalMetresPerUnit, heightSampler);
            else if (selectedCell.HasValue)
                AddHighlightOutline(geometry, grid, selectedCell, SelectedEdge, minRow, minColumn,
                    maxRow, maxColumn, floatingOrigin, horizontalMetresPerUnit, heightSampler);
            return geometry;
        }

        private static void AddCellFace(StrategicCellOverlayGeometry geometry, CellGridIndex grid,
            int row, int column, double insetMetres, GlobalProjectedCoordinate floatingOrigin,
            double horizontalMetresPerUnit, Func<double, double, float> heightSampler,
            Color32 colour)
        {
            var left = grid.OriginX + column * grid.CellSize + insetMetres;
            var right = grid.OriginX + (column + 1) * grid.CellSize - insetMetres;
            var top = grid.OriginY - row * grid.CellSize - insetMetres;
            var bottom = grid.OriginY - (row + 1) * grid.CellSize + insetMetres;
            var start = geometry.FaceVertices.Count;
            geometry.FaceVertices.Add(ToLocal(left, top, 0.018f, floatingOrigin,
                horizontalMetresPerUnit, heightSampler));
            geometry.FaceVertices.Add(ToLocal(right, top, 0.018f, floatingOrigin,
                horizontalMetresPerUnit, heightSampler));
            geometry.FaceVertices.Add(ToLocal(left, bottom, 0.018f, floatingOrigin,
                horizontalMetresPerUnit, heightSampler));
            geometry.FaceVertices.Add(ToLocal(right, bottom, 0.018f, floatingOrigin,
                horizontalMetresPerUnit, heightSampler));
            for (var index = 0; index < 4; index++) geometry.FaceColours.Add(colour);
            geometry.FaceTriangles.Add(start);
            geometry.FaceTriangles.Add(start + 2);
            geometry.FaceTriangles.Add(start + 1);
            geometry.FaceTriangles.Add(start + 1);
            geometry.FaceTriangles.Add(start + 2);
            geometry.FaceTriangles.Add(start + 3);
        }

        private static void AddHighlightOutline(StrategicCellOverlayGeometry geometry,
            CellGridIndex grid, WorldMapCellId? id, Color32 colour, int minRow, int minColumn,
            int maxRow, int maxColumn, GlobalProjectedCoordinate floatingOrigin,
            double horizontalMetresPerUnit, Func<double, double, float> heightSampler)
        {
            if (!id.HasValue || !grid.TryDecode(id.Value, out var row, out var column) ||
                row < minRow || row >= maxRow || column < minColumn || column >= maxColumn) return;
            var left = grid.OriginX + column * grid.CellSize;
            var right = left + grid.CellSize;
            var top = grid.OriginY - row * grid.CellSize;
            var bottom = top - grid.CellSize;
            var width = grid.CellSize * 0.052d;
            AddEdgeRibbon(geometry, left, top, right, top, width, floatingOrigin,
                horizontalMetresPerUnit, heightSampler, colour, 0.065f);
            AddEdgeRibbon(geometry, right, top, right, bottom, width, floatingOrigin,
                horizontalMetresPerUnit, heightSampler, colour, 0.065f);
            AddEdgeRibbon(geometry, right, bottom, left, bottom, width, floatingOrigin,
                horizontalMetresPerUnit, heightSampler, colour, 0.065f);
            AddEdgeRibbon(geometry, left, bottom, left, top, width, floatingOrigin,
                horizontalMetresPerUnit, heightSampler, colour, 0.065f);
            geometry.HighlightedOutlineCount++;
        }

        private static void AddEdgeRibbon(StrategicCellOverlayGeometry geometry,
            double ax, double ay, double bx, double by, double widthMetres,
            GlobalProjectedCoordinate floatingOrigin, double horizontalMetresPerUnit,
            Func<double, double, float> heightSampler, Color32 colour, float lift)
        {
            var dx = bx - ax;
            var dy = by - ay;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0.0001d) return;
            var px = -dy / length * widthMetres * 0.5d;
            var py = dx / length * widthMetres * 0.5d;
            var start = geometry.EdgeVertices.Count;
            geometry.EdgeVertices.Add(ToLocal(ax + px, ay + py, lift, floatingOrigin,
                horizontalMetresPerUnit, heightSampler));
            geometry.EdgeVertices.Add(ToLocal(ax - px, ay - py, lift, floatingOrigin,
                horizontalMetresPerUnit, heightSampler));
            geometry.EdgeVertices.Add(ToLocal(bx + px, by + py, lift, floatingOrigin,
                horizontalMetresPerUnit, heightSampler));
            geometry.EdgeVertices.Add(ToLocal(bx - px, by - py, lift, floatingOrigin,
                horizontalMetresPerUnit, heightSampler));
            for (var index = 0; index < 4; index++) geometry.EdgeColours.Add(colour);
            geometry.EdgeTriangles.Add(start);
            geometry.EdgeTriangles.Add(start + 1);
            geometry.EdgeTriangles.Add(start + 2);
            geometry.EdgeTriangles.Add(start + 2);
            geometry.EdgeTriangles.Add(start + 1);
            geometry.EdgeTriangles.Add(start + 3);
        }

        private static Vector3 ToLocal(double x, double y, float lift,
            GlobalProjectedCoordinate floatingOrigin, double horizontalMetresPerUnit,
            Func<double, double, float> heightSampler) => new Vector3(
            (float)((x - floatingOrigin.EastingMetres) / horizontalMetresPerUnit),
            heightSampler(x, y) + lift,
            (float)((y - floatingOrigin.NorthingMetres) / horizontalMetresPerUnit));
    }

    public sealed class StrategicCellOverlayGeometry
    {
        public readonly List<Vector3> FaceVertices = new List<Vector3>();
        public readonly List<Color32> FaceColours = new List<Color32>();
        public readonly List<int> FaceTriangles = new List<int>();
        public readonly List<Vector3> EdgeVertices = new List<Vector3>();
        public readonly List<Color32> EdgeColours = new List<Color32>();
        public readonly List<int> EdgeTriangles = new List<int>();
        public readonly List<WorldMapCellId> VisibleCellIds = new List<WorldMapCellId>();
        public int UniqueGridEdgeCount;
        public int HighlightedOutlineCount;
        public ulong CoveredCellCount;
        public int DisplayStepCells = 1;

        public Mesh CreateFaceMesh()
        {
            var mesh = NewMesh("Explicit Strategic Cell Faces", FaceVertices.Count);
            mesh.SetVertices(FaceVertices);
            mesh.SetColors(FaceColours);
            mesh.SetTriangles(FaceTriangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public Mesh CreateEdgeMesh()
        {
            var mesh = NewMesh("Explicit Strategic Cell Edges", EdgeVertices.Count);
            mesh.SetVertices(EdgeVertices);
            mesh.SetColors(EdgeColours);
            mesh.SetTriangles(EdgeTriangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh NewMesh(string name, int vertexCount)
        {
            var mesh = new Mesh { name = name };
            if (vertexCount > 65535) mesh.indexFormat = IndexFormat.UInt32;
            return mesh;
        }
    }

    public sealed class StrategicCellCameraPreset
    {
        public StrategicCellCameraPreset(string id, int row, int column, float size,
            float pitch, float yaw, VisualTerrainDetailLevel detailLevel,
            bool isWorldView = false)
        {
            Id = id;
            Row = row;
            Column = column;
            Size = size;
            Pitch = pitch;
            Yaw = yaw;
            DetailLevel = detailLevel;
            IsWorldView = isWorldView;
        }

        public string Id { get; }
        public int Row { get; }
        public int Column { get; }
        public float Size { get; }
        public float Pitch { get; }
        public float Yaw { get; }
        public VisualTerrainDetailLevel DetailLevel { get; }
        public bool IsWorldView { get; }
    }

    public static class StrategicCellCameraRig
    {
        public const string NationwideOverview = "strategic-cell.nationwide.overview";
        public const string HenanYinOverview = "strategic-cell.henan-yin.overview";
        public const string LuoyangSelection = "strategic-cell.luoyang.selection";
        public const string MountainTerrain = "strategic-cell.henan-mountain.terrain";
        public const string BuildableFacilityReview =
            "strategic-cell.luoyang.buildable-facility-review";
        public const string LuoyangFacilityCoverageReview =
            "strategic-cell.luoyang.facility-model-coverage-review";
        public const string LuoyangHistoricalLandmarkReview =
            "strategic-cell.luoyang.historical-landmark-review";
        public const string LuoyangGateIdentityReview =
            "strategic-cell.luoyang.gate-identity-review";
        public const string LuoyangMediumFrequencyUrbanFabricReview =
            "strategic-cell.luoyang.medium-frequency-urban-fabric-review";
        public const string LuoyangBuildingPerformanceReview =
            "strategic-cell.luoyang.building-performance-review";
        public const string LuoyangInfrastructureOverview =
            "strategic-cell.luoyang.infrastructure-overview";
        public const string LuoyangInfrastructureCanalCorridor =
            "strategic-cell.luoyang.infrastructure-canal-corridor";
        public const string LuoyangInfrastructureBridgeChain =
            "strategic-cell.luoyang.infrastructure-bridge-chain";
        public const string LuoyangLowFrequencyDefenseOverview =
            "strategic-cell.luoyang.low-frequency-defense-overview";
        public const string LuoyangDefenseManorGateLine =
            "strategic-cell.luoyang.defense-manor-gate-line";
        public const string LuoyangDefenseNorthernBeaconPair =
            "strategic-cell.luoyang.defense-northern-beacon-pair";
        public const string LuoyangResourceAgricultureOverview =
            "strategic-cell.luoyang.resource-agriculture-overview";
        public const string LuoyangResourceExtractionLine =
            "strategic-cell.luoyang.resource-extraction-line";
        public const string LuoyangSouthernQuarryTerraces =
            "strategic-cell.luoyang.southern-quarry-terraces";
        public const string LuoyangRicePaddyBand =
            "strategic-cell.luoyang.rice-paddy-band";
        public const string LuoyangFinalCivicOverview =
            "strategic-cell.luoyang.final-civic-overview";
        public const string LuoyangClinicLine =
            "strategic-cell.luoyang.clinic-line";
        public const string LuoyangRitualHallLine =
            "strategic-cell.luoyang.ritual-hall-line";
        public const string LuoyangPublicCivicCluster =
            "strategic-cell.luoyang.public-civic-cluster";
        public const string LuoyangFinalAssetReviewAll =
            "strategic-cell.luoyang.final-asset-review-all";
        public const string LuoyangFinalAssetReviewP0 =
            "strategic-cell.luoyang.final-asset-review-p0";
        public const string LuoyangFinalAssetReviewP1 =
            "strategic-cell.luoyang.final-asset-review-p1";
        public const string LuoyangFinalAssetReviewP2P3 =
            "strategic-cell.luoyang.final-asset-review-p2-p3";
        public const string LuoyangP0FinalAssetVerticalSliceOverview =
            "strategic-cell.luoyang.p0-final-asset-vertical-slice-overview";
        public const string LuoyangP0SouthPalaceCloseup =
            "strategic-cell.luoyang.p0-south-palace-closeup";
        public const string LuoyangP0MingtangCloseup =
            "strategic-cell.luoyang.p0-mingtang-closeup";
        public const string LuoyangP0GuangyangmenCloseup =
            "strategic-cell.luoyang.p0-guangyangmen-closeup";
        public const string LuoyangP0NorthPalaceGateCloseup =
            "strategic-cell.luoyang.p0-north-palace-gate-closeup";
        public const string LuoyangP0SouthPalaceRearOblique =
            "strategic-cell.luoyang.p0-south-palace-rear-oblique";
        public const string LuoyangP0SouthPalaceLowOblique =
            "strategic-cell.luoyang.p0-south-palace-low-oblique";
        public const string LuoyangP0MingtangRearOblique =
            "strategic-cell.luoyang.p0-mingtang-rear-oblique";
        public const string LuoyangP0MingtangLowOblique =
            "strategic-cell.luoyang.p0-mingtang-low-oblique";
        public const string LuoyangP0GuangyangmenRearOblique =
            "strategic-cell.luoyang.p0-guangyangmen-rear-oblique";
        public const string LuoyangP0GuangyangmenLowOblique =
            "strategic-cell.luoyang.p0-guangyangmen-low-oblique";
        public const string LuoyangP0NorthPalaceGateRearOblique =
            "strategic-cell.luoyang.p0-north-palace-gate-rear-oblique";
        public const string LuoyangP0NorthPalaceGateLowOblique =
            "strategic-cell.luoyang.p0-north-palace-gate-low-oblique";
        public const string LuoyangP0LandmarkSecondBatchOverview =
            "strategic-cell.luoyang.p0-landmark-second-batch-overview";
        public const string LuoyangP0NorthPalaceCloseup =
            "strategic-cell.luoyang.p0-north-palace-closeup";
        public const string LuoyangP0YonganPalaceCloseup =
            "strategic-cell.luoyang.p0-yongan-palace-closeup";
        public const string LuoyangP0TaixueCloseup =
            "strategic-cell.luoyang.p0-taixue-closeup";
        public const string LuoyangP0BiyongCloseup =
            "strategic-cell.luoyang.p0-biyong-closeup";
        public const string LuoyangP0NorthPalaceRearOblique =
            "strategic-cell.luoyang.p0-north-palace-rear-oblique";
        public const string LuoyangP0NorthPalaceLowOblique =
            "strategic-cell.luoyang.p0-north-palace-low-oblique";
        public const string LuoyangP0YonganPalaceRearOblique =
            "strategic-cell.luoyang.p0-yongan-palace-rear-oblique";
        public const string LuoyangP0YonganPalaceLowOblique =
            "strategic-cell.luoyang.p0-yongan-palace-low-oblique";
        public const string LuoyangP0TaixueRearOblique =
            "strategic-cell.luoyang.p0-taixue-rear-oblique";
        public const string LuoyangP0TaixueLowOblique =
            "strategic-cell.luoyang.p0-taixue-low-oblique";
        public const string LuoyangP0BiyongRearOblique =
            "strategic-cell.luoyang.p0-biyong-rear-oblique";
        public const string LuoyangP0BiyongLowOblique =
            "strategic-cell.luoyang.p0-biyong-low-oblique";
        public const string LuoyangP0LandmarkThirdBatchOverview =
            "strategic-cell.luoyang.p0-landmark-third-batch-overview";
        public const string LuoyangP0LingtaiCloseup =
            "strategic-cell.luoyang.p0-lingtai-closeup";
        public const string LuoyangP0TaicangCloseup =
            "strategic-cell.luoyang.p0-taicang-closeup";
        public const string LuoyangP0ArsenalCloseup =
            "strategic-cell.luoyang.p0-arsenal-closeup";
        public const string LuoyangP0ZhuolongGardenCloseup =
            "strategic-cell.luoyang.p0-zhuolong-garden-closeup";
        public const string LuoyangP0NamedGateFourthBatchOverview =
            "strategic-cell.luoyang.p0-named-gate-fourth-batch-overview";
        public const string LuoyangP0GumenCloseup =
            "strategic-cell.luoyang.p0-gumen-closeup";
        public const string LuoyangP0GumenRearOblique =
            "strategic-cell.luoyang.p0-gumen-rear-oblique";
        public const string LuoyangP0GumenLowOblique =
            "strategic-cell.luoyang.p0-gumen-low-oblique";
        public const string LuoyangP0JinmenCloseup =
            "strategic-cell.luoyang.p0-jinmen-closeup";
        public const string LuoyangP0JinmenRearOblique =
            "strategic-cell.luoyang.p0-jinmen-rear-oblique";
        public const string LuoyangP0JinmenLowOblique =
            "strategic-cell.luoyang.p0-jinmen-low-oblique";
        public const string LuoyangP0KaiyangmenCloseup =
            "strategic-cell.luoyang.p0-kaiyangmen-closeup";
        public const string LuoyangP0KaiyangmenRearOblique =
            "strategic-cell.luoyang.p0-kaiyangmen-rear-oblique";
        public const string LuoyangP0KaiyangmenLowOblique =
            "strategic-cell.luoyang.p0-kaiyangmen-low-oblique";
        public const string LuoyangP0MaomenCloseup =
            "strategic-cell.luoyang.p0-maomen-closeup";
        public const string LuoyangP0MaomenRearOblique =
            "strategic-cell.luoyang.p0-maomen-rear-oblique";
        public const string LuoyangP0MaomenLowOblique =
            "strategic-cell.luoyang.p0-maomen-low-oblique";

        public static bool IsLuoyangInfrastructureReview(string id) =>
            id == LuoyangInfrastructureOverview ||
            id == LuoyangInfrastructureCanalCorridor ||
            id == LuoyangInfrastructureBridgeChain;

        public static bool IsLuoyangLowFrequencyDefenseReview(string id) =>
            id == LuoyangLowFrequencyDefenseOverview ||
            id == LuoyangDefenseManorGateLine ||
            id == LuoyangDefenseNorthernBeaconPair;

        public static bool IsLuoyangResourceAgricultureReview(string id) =>
            id == LuoyangResourceAgricultureOverview ||
            id == LuoyangResourceExtractionLine ||
            id == LuoyangSouthernQuarryTerraces ||
            id == LuoyangRicePaddyBand;

        public static bool IsLuoyangFinalCivicReview(string id) =>
            id == LuoyangFinalCivicOverview ||
            id == LuoyangClinicLine ||
            id == LuoyangRitualHallLine ||
            id == LuoyangPublicCivicCluster;

        public static bool IsLuoyangFinalAssetReview(string id) =>
            id == LuoyangFinalAssetReviewAll ||
            id == LuoyangFinalAssetReviewP0 ||
            id == LuoyangFinalAssetReviewP1 ||
            id == LuoyangFinalAssetReviewP2P3;

        public static bool IsLuoyangP0FinalAssetVerticalSlice(string id) =>
            id == LuoyangP0FinalAssetVerticalSliceOverview ||
            LuoyangP0MultiAngleReviewRig.ContainsCamera(id);

        public static bool IsLuoyangP0LandmarkSecondBatch(string id) =>
            id == LuoyangP0LandmarkSecondBatchOverview ||
            LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.ContainsCamera(id);

        public static bool IsLuoyangP0LandmarkThirdBatch(string id) =>
            id == LuoyangP0LandmarkThirdBatchOverview ||
            id == LuoyangP0LingtaiCloseup ||
            id == LuoyangP0TaicangCloseup ||
            id == LuoyangP0ArsenalCloseup ||
            id == LuoyangP0ZhuolongGardenCloseup;

        public static bool IsLuoyangP0NamedGateFourthBatch(string id) =>
            id == LuoyangP0NamedGateFourthBatchOverview ||
            LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.ContainsCamera(id);

        public static StrategicCellCameraPreset Get(string id)
        {
            switch (id)
            {
                case NationwideOverview:
                    return new StrategicCellCameraPreset(id,
                        GlobalSpatialFoundationV1.Rows / 2,
                        GlobalSpatialFoundationV1.Columns / 2,
                        1160f, 68f, 0f, VisualTerrainDetailLevel.World, true);
                case HenanYinOverview:
                    return new StrategicCellCameraPreset(id, 1247, 1992, 17.5f, 57f, -18f,
                        VisualTerrainDetailLevel.City);
                case LuoyangSelection:
                    return new StrategicCellCameraPreset(id, 1241, 2043, 10.5f, 54f, -20f,
                        VisualTerrainDetailLevel.ClosePreview);
                case MountainTerrain:
                    return new StrategicCellCameraPreset(id, 1390, 1710, 13.5f, 59f, -25f,
                        VisualTerrainDetailLevel.City);
                case BuildableFacilityReview:
                    return new StrategicCellCameraPreset(id, 1241, 2043, 5.1f, 54f, -20f,
                        VisualTerrainDetailLevel.ClosePreview);
                case LuoyangFacilityCoverageReview:
                    return new StrategicCellCameraPreset(id, 1241, 2043, 10.8f, 54f,
                        -20f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangHistoricalLandmarkReview:
                    return new StrategicCellCameraPreset(id, 1246, 2043, 12.8f, 57f,
                        -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangGateIdentityReview:
                    return new StrategicCellCameraPreset(id, 1241, 2043, 13.5f, 58f,
                        0f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangMediumFrequencyUrbanFabricReview:
                    return new StrategicCellCameraPreset(id, 1246, 2043, 6.6f, 55f,
                        -12f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangBuildingPerformanceReview:
                    return new StrategicCellCameraPreset(id, 1235, 2051, 14.2f,
                        58f, -12f, VisualTerrainDetailLevel.City);
                case LuoyangInfrastructureOverview:
                    return new StrategicCellCameraPreset(id, 1236, 2048, 39.5f,
                        60f, -10f, VisualTerrainDetailLevel.City);
                case LuoyangInfrastructureCanalCorridor:
                    return new StrategicCellCameraPreset(id, 1227, 2037, 9.8f,
                        56f, -6f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangInfrastructureBridgeChain:
                    return new StrategicCellCameraPreset(id, 1254, 2054, 3.5f,
                        54f, -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangLowFrequencyDefenseOverview:
                    return new StrategicCellCameraPreset(id, 1233, 2045, 30.5f,
                        60f, -10f, VisualTerrainDetailLevel.City);
                case LuoyangDefenseManorGateLine:
                    return new StrategicCellCameraPreset(id, 1223, 2033, 10.8f,
                        56f, -5f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangDefenseNorthernBeaconPair:
                    return new StrategicCellCameraPreset(id, 1216, 2064, 3.4f,
                        53f, -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangResourceAgricultureOverview:
                    return new StrategicCellCameraPreset(id, 1244, 2045, 27.0f,
                        60f, -10f, VisualTerrainDetailLevel.City);
                case LuoyangResourceExtractionLine:
                    return new StrategicCellCameraPreset(id, 1253, 2047, 12.0f,
                        56f, -6f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangSouthernQuarryTerraces:
                    return new StrategicCellCameraPreset(id, 1228, 2031, 4.0f,
                        54f, -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangRicePaddyBand:
                    return new StrategicCellCameraPreset(id, 1256, 2054, 5.8f,
                        53f, -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangFinalCivicOverview:
                    return new StrategicCellCameraPreset(id, 1237, 2044, 46.0f,
                        60f, -10f, VisualTerrainDetailLevel.City);
                case LuoyangClinicLine:
                    return new StrategicCellCameraPreset(id, 1255, 2050, 8.2f,
                        53f, -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangRitualHallLine:
                    return new StrategicCellCameraPreset(id, 1237, 2044, 45.0f,
                        59f, -8f, VisualTerrainDetailLevel.City);
                case LuoyangPublicCivicCluster:
                    return new StrategicCellCameraPreset(id, 1241, 2041, 14.5f,
                        56f, -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangFinalAssetReviewAll:
                    return new StrategicCellCameraPreset(id, 1243, 2043, 21.0f,
                        58f, -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangFinalAssetReviewP0:
                    return new StrategicCellCameraPreset(id, 1237, 2043, 10.0f,
                        56f, -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangFinalAssetReviewP1:
                    return new StrategicCellCameraPreset(id, 1243, 2043, 7.0f,
                        54f, -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangFinalAssetReviewP2P3:
                    return new StrategicCellCameraPreset(id, 1249, 2043, 10.5f,
                        56f, -8f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0FinalAssetVerticalSliceOverview:
                    return new StrategicCellCameraPreset(id, 1243, 2043, 8.8f,
                        52f, -10f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0SouthPalaceCloseup:
                    return new StrategicCellCameraPreset(id, 1240, 2040, 1.35f,
                        41f, -18f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0SouthPalaceRearOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2040, 1.35f,
                        40f, 162f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0SouthPalaceLowOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2040, 1.52f,
                        30f, -38f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0MingtangCloseup:
                    return new StrategicCellCameraPreset(id, 1240, 2046, 1.30f,
                        40f, -24f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0MingtangRearOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2046, 1.30f,
                        40f, 156f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0MingtangLowOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2046, 1.48f,
                        30f, -44f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0GuangyangmenCloseup:
                    return new StrategicCellCameraPreset(id, 1246, 2040, 1.42f,
                        42f, -16f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0GuangyangmenRearOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2040, 1.42f,
                        41f, 164f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0GuangyangmenLowOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2040, 1.62f,
                        31f, -36f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0NorthPalaceGateCloseup:
                    return new StrategicCellCameraPreset(id, 1246, 2046, 1.42f,
                        42f, -14f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0NorthPalaceGateRearOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2046, 1.42f,
                        41f, 166f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0NorthPalaceGateLowOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2046, 1.62f,
                        31f, -34f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0LandmarkSecondBatchOverview:
                    return new StrategicCellCameraPreset(id, 1243, 2043, 8.8f,
                        52f, -10f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0NorthPalaceCloseup:
                    return new StrategicCellCameraPreset(id, 1240, 2040, 1.48f,
                        41f, -20f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0NorthPalaceRearOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2040, 1.48f,
                        41f, 160f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0NorthPalaceLowOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2040, 1.62f,
                        31f, -40f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0YonganPalaceCloseup:
                    return new StrategicCellCameraPreset(id, 1240, 2046, 1.48f,
                        40f, -26f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0YonganPalaceRearOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2046, 1.48f,
                        40f, 154f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0YonganPalaceLowOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2046, 1.62f,
                        31f, -46f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0TaixueCloseup:
                    return new StrategicCellCameraPreset(id, 1246, 2040, 1.56f,
                        41f, -18f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0TaixueRearOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2040, 1.56f,
                        41f, 162f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0TaixueLowOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2040, 1.70f,
                        32f, -38f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0BiyongCloseup:
                    return new StrategicCellCameraPreset(id, 1246, 2046, 1.52f,
                        40f, -24f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0BiyongRearOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2046, 1.52f,
                        40f, 156f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0BiyongLowOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2046, 1.68f,
                        31f, -44f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0LandmarkThirdBatchOverview:
                    return new StrategicCellCameraPreset(id, 1243, 2043, 8.8f,
                        52f, -10f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0LingtaiCloseup:
                    return new StrategicCellCameraPreset(id, 1240, 2040, 1.72f,
                        39f, -22f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0TaicangCloseup:
                    return new StrategicCellCameraPreset(id, 1240, 2046, 1.58f,
                        41f, -24f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0ArsenalCloseup:
                    return new StrategicCellCameraPreset(id, 1246, 2040, 1.58f,
                        41f, -18f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0ZhuolongGardenCloseup:
                    return new StrategicCellCameraPreset(id, 1246, 2046, 1.62f,
                        40f, -26f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0NamedGateFourthBatchOverview:
                    return new StrategicCellCameraPreset(id, 1243, 2043, 8.8f,
                        52f, -10f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0GumenCloseup:
                    return new StrategicCellCameraPreset(id, 1240, 2040, 1.72f,
                        39f, 158f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0GumenRearOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2040, 1.72f,
                        39f, -22f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0GumenLowOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2040, 1.88f,
                        31f, 138f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0JinmenCloseup:
                    return new StrategicCellCameraPreset(id, 1240, 2046, 1.68f,
                        40f, -24f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0JinmenRearOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2046, 1.68f,
                        40f, 156f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0JinmenLowOblique:
                    return new StrategicCellCameraPreset(id, 1240, 2046, 1.84f,
                        31f, -44f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0KaiyangmenCloseup:
                    return new StrategicCellCameraPreset(id, 1246, 2040, 1.82f,
                        39f, -18f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0KaiyangmenRearOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2040, 1.82f,
                        39f, 162f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0KaiyangmenLowOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2040, 1.98f,
                        31f, -38f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0MaomenCloseup:
                    return new StrategicCellCameraPreset(id, 1246, 2046, 1.62f,
                        40f, -112f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0MaomenRearOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2046, 1.62f,
                        40f, 68f, VisualTerrainDetailLevel.ClosePreview);
                case LuoyangP0MaomenLowOblique:
                    return new StrategicCellCameraPreset(id, 1246, 2046, 1.78f,
                        31f, -132f, VisualTerrainDetailLevel.ClosePreview);
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id,
                        "Unknown strategic cell camera preset.");
            }
        }
    }

    public static class LuoyangP0NamedGateFourthBatchMultiAngleReviewRig
    {
        public const string ContractId =
            "presentation.luoyang.p0-named-gate-fourth-batch.multi-angle-review.v1";
        public const int PieceCount = 4;
        public const int AngleCount = 3;

        private static readonly string[,] CameraIds =
        {
            {
                StrategicCellCameraRig.LuoyangP0GumenCloseup,
                StrategicCellCameraRig.LuoyangP0GumenRearOblique,
                StrategicCellCameraRig.LuoyangP0GumenLowOblique
            },
            {
                StrategicCellCameraRig.LuoyangP0JinmenCloseup,
                StrategicCellCameraRig.LuoyangP0JinmenRearOblique,
                StrategicCellCameraRig.LuoyangP0JinmenLowOblique
            },
            {
                StrategicCellCameraRig.LuoyangP0KaiyangmenCloseup,
                StrategicCellCameraRig.LuoyangP0KaiyangmenRearOblique,
                StrategicCellCameraRig.LuoyangP0KaiyangmenLowOblique
            },
            {
                StrategicCellCameraRig.LuoyangP0MaomenCloseup,
                StrategicCellCameraRig.LuoyangP0MaomenRearOblique,
                StrategicCellCameraRig.LuoyangP0MaomenLowOblique
            }
        };

        private static readonly string[] PieceLabels =
        {
            "GUMEN", "JINMEN", "KAIYANGMEN", "MAOMEN"
        };

        private static readonly string[] AngleLabels =
        {
            "FRONT OBLIQUE", "REAR OBLIQUE", "LOW OBLIQUE"
        };

        public static string GetCameraId(int pieceIndex, int angleIndex)
        {
            ValidatePieceIndex(pieceIndex);
            ValidateAngleIndex(angleIndex);
            return CameraIds[pieceIndex, angleIndex];
        }

        public static string GetPieceLabel(int pieceIndex)
        {
            ValidatePieceIndex(pieceIndex);
            return PieceLabels[pieceIndex];
        }

        public static string GetAngleLabel(int angleIndex)
        {
            ValidateAngleIndex(angleIndex);
            return AngleLabels[angleIndex];
        }

        public static bool ContainsCamera(string cameraId) =>
            TryGetIndexes(cameraId, out _, out _);

        public static bool TryGetIndexes(string cameraId, out int pieceIndex,
            out int angleIndex)
        {
            for (var piece = 0; piece < PieceCount; piece++)
            for (var angle = 0; angle < AngleCount; angle++)
                if (string.Equals(CameraIds[piece, angle], cameraId,
                        StringComparison.Ordinal))
                {
                    pieceIndex = piece;
                    angleIndex = angle;
                    return true;
                }
            pieceIndex = -1;
            angleIndex = -1;
            return false;
        }

        private static void ValidatePieceIndex(int pieceIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= PieceCount)
                throw new ArgumentOutOfRangeException(nameof(pieceIndex));
        }

        private static void ValidateAngleIndex(int angleIndex)
        {
            if (angleIndex < 0 || angleIndex >= AngleCount)
                throw new ArgumentOutOfRangeException(nameof(angleIndex));
        }
    }

    public static class LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
    {
        public const string ContractId =
            "presentation.luoyang.p0-landmark-second-batch.multi-angle-review.v1";
        public const int PieceCount = 4;
        public const int AngleCount = 3;

        private static readonly string[,] CameraIds =
        {
            {
                StrategicCellCameraRig.LuoyangP0NorthPalaceCloseup,
                StrategicCellCameraRig.LuoyangP0NorthPalaceRearOblique,
                StrategicCellCameraRig.LuoyangP0NorthPalaceLowOblique
            },
            {
                StrategicCellCameraRig.LuoyangP0YonganPalaceCloseup,
                StrategicCellCameraRig.LuoyangP0YonganPalaceRearOblique,
                StrategicCellCameraRig.LuoyangP0YonganPalaceLowOblique
            },
            {
                StrategicCellCameraRig.LuoyangP0TaixueCloseup,
                StrategicCellCameraRig.LuoyangP0TaixueRearOblique,
                StrategicCellCameraRig.LuoyangP0TaixueLowOblique
            },
            {
                StrategicCellCameraRig.LuoyangP0BiyongCloseup,
                StrategicCellCameraRig.LuoyangP0BiyongRearOblique,
                StrategicCellCameraRig.LuoyangP0BiyongLowOblique
            }
        };

        private static readonly string[] PieceLabels =
        {
            "NORTH PALACE", "YONGAN PALACE", "TAIXUE", "BIYONG"
        };

        private static readonly string[] AngleLabels =
        {
            "FRONT OBLIQUE", "REAR OBLIQUE", "LOW OBLIQUE"
        };

        public static string GetCameraId(int pieceIndex, int angleIndex)
        {
            ValidatePieceIndex(pieceIndex);
            ValidateAngleIndex(angleIndex);
            return CameraIds[pieceIndex, angleIndex];
        }

        public static string GetPieceLabel(int pieceIndex)
        {
            ValidatePieceIndex(pieceIndex);
            return PieceLabels[pieceIndex];
        }

        public static string GetAngleLabel(int angleIndex)
        {
            ValidateAngleIndex(angleIndex);
            return AngleLabels[angleIndex];
        }

        public static bool ContainsCamera(string cameraId) =>
            TryGetIndexes(cameraId, out _, out _);

        public static bool TryGetIndexes(string cameraId, out int pieceIndex,
            out int angleIndex)
        {
            for (var piece = 0; piece < PieceCount; piece++)
            for (var angle = 0; angle < AngleCount; angle++)
                if (string.Equals(CameraIds[piece, angle], cameraId,
                        StringComparison.Ordinal))
                {
                    pieceIndex = piece;
                    angleIndex = angle;
                    return true;
                }
            pieceIndex = -1;
            angleIndex = -1;
            return false;
        }

        private static void ValidatePieceIndex(int pieceIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= PieceCount)
                throw new ArgumentOutOfRangeException(nameof(pieceIndex));
        }

        private static void ValidateAngleIndex(int angleIndex)
        {
            if (angleIndex < 0 || angleIndex >= AngleCount)
                throw new ArgumentOutOfRangeException(nameof(angleIndex));
        }
    }

    public static class LuoyangP0MultiAngleReviewRig
    {
        public const string ContractId =
            "presentation.luoyang.p0-four-piece.multi-angle-review.v1";
        public const int PieceCount = 4;
        public const int AngleCount = 3;

        private static readonly string[,] CameraIds =
        {
            {
                StrategicCellCameraRig.LuoyangP0SouthPalaceCloseup,
                StrategicCellCameraRig.LuoyangP0SouthPalaceRearOblique,
                StrategicCellCameraRig.LuoyangP0SouthPalaceLowOblique
            },
            {
                StrategicCellCameraRig.LuoyangP0MingtangCloseup,
                StrategicCellCameraRig.LuoyangP0MingtangRearOblique,
                StrategicCellCameraRig.LuoyangP0MingtangLowOblique
            },
            {
                StrategicCellCameraRig.LuoyangP0GuangyangmenCloseup,
                StrategicCellCameraRig.LuoyangP0GuangyangmenRearOblique,
                StrategicCellCameraRig.LuoyangP0GuangyangmenLowOblique
            },
            {
                StrategicCellCameraRig.LuoyangP0NorthPalaceGateCloseup,
                StrategicCellCameraRig.LuoyangP0NorthPalaceGateRearOblique,
                StrategicCellCameraRig.LuoyangP0NorthPalaceGateLowOblique
            }
        };

        private static readonly string[] PieceLabels =
        {
            "SOUTH PALACE", "MINGTANG", "GUANGYANGMEN",
            "NORTH PALACE SOUTH GATE"
        };

        private static readonly string[] AngleLabels =
        {
            "FRONT OBLIQUE", "REAR OBLIQUE", "LOW OBLIQUE"
        };

        public static string GetCameraId(int pieceIndex, int angleIndex)
        {
            ValidatePieceIndex(pieceIndex);
            ValidateAngleIndex(angleIndex);
            return CameraIds[pieceIndex, angleIndex];
        }

        public static string GetPieceLabel(int pieceIndex)
        {
            ValidatePieceIndex(pieceIndex);
            return PieceLabels[pieceIndex];
        }

        public static string GetAngleLabel(int angleIndex)
        {
            ValidateAngleIndex(angleIndex);
            return AngleLabels[angleIndex];
        }

        public static bool ContainsCamera(string cameraId) =>
            TryGetIndexes(cameraId, out _, out _);

        public static bool TryGetIndexes(string cameraId, out int pieceIndex,
            out int angleIndex)
        {
            for (var piece = 0; piece < PieceCount; piece++)
            for (var angle = 0; angle < AngleCount; angle++)
                if (string.Equals(CameraIds[piece, angle], cameraId,
                        StringComparison.Ordinal))
                {
                    pieceIndex = piece;
                    angleIndex = angle;
                    return true;
                }
            pieceIndex = -1;
            angleIndex = -1;
            return false;
        }

        private static void ValidatePieceIndex(int pieceIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= PieceCount)
                throw new ArgumentOutOfRangeException(nameof(pieceIndex));
        }

        private static void ValidateAngleIndex(int angleIndex)
        {
            if (angleIndex < 0 || angleIndex >= AngleCount)
                throw new ArgumentOutOfRangeException(nameof(angleIndex));
        }
    }
}
