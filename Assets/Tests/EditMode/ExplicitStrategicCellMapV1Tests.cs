using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed class ExplicitStrategicCellMapV1Tests
    {
        [Test]
        public void Contract_UsesFrozenGlobalCellsWithoutCreatingSimulationSubCells()
        {
            Assert.That(ExplicitStrategicCellMapV1.ContractId,
                Is.EqualTo("presentation.han-world.explicit-strategic-cell-map.v1"));
            Assert.That(ExplicitStrategicCellMapV1.NationwideContractId,
                Is.EqualTo("presentation.han-world.nationwide-strategic-cell-grid-lod.v1"));
            Assert.That(ExplicitStrategicCellMapV1.SourceGridSchema,
                Is.EqualTo(GlobalSpatialFoundationV1.GridSchemaVersion));
            Assert.That(ExplicitStrategicCellMapV1.SourceCellSizeMetres,
                Is.EqualTo(GlobalSpatialFoundationV1.CellSizeMetres));
            Assert.That(ExplicitStrategicCellMapV1.CreatesSimulationSubCells, Is.False);
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            Assert.That(new CellNeighborService(grid).GetNeighbors(grid.ToCellId(1, 1)),
                Has.Count.EqualTo(8));
        }

        [Test]
        public void Geometry_BatchesFacesTerrainConformingEdgesHoverAndSelection()
        {
            var grid = new CellGridIndex(5, 6, 10000d, 20000d, 2000d);
            var hovered = grid.ToCellId(1, 2);
            var selected = grid.ToCellId(2, 3);
            var geometry = ExplicitStrategicCellMapV1.BuildGeometry(grid, 1, 1, 2, 3,
                new GlobalProjectedCoordinate(14000d, 16000d), 2000d,
                (x, y) => (float)((x + y) / 100000d), hovered, selected);

            Assert.That(geometry.VisibleCellIds, Has.Count.EqualTo(6));
            Assert.That(geometry.VisibleCellIds, Does.Contain(hovered));
            Assert.That(geometry.VisibleCellIds, Does.Contain(selected));
            Assert.That(geometry.FaceVertices, Has.Count.EqualTo(24));
            Assert.That(geometry.FaceTriangles, Has.Count.EqualTo(36));
            Assert.That(geometry.UniqueGridEdgeCount, Is.EqualTo(17));
            Assert.That(geometry.HighlightedOutlineCount, Is.EqualTo(2));
            Assert.That(geometry.EdgeVertices, Has.Count.EqualTo((17 + 8) * 4));
            Assert.That(geometry.FaceColours, Does.Contain(ExplicitStrategicCellMapV1.HoverFace));
            Assert.That(geometry.FaceColours, Does.Contain(ExplicitStrategicCellMapV1.SelectedFace));
            Assert.That(geometry.EdgeColours, Does.Contain(ExplicitStrategicCellMapV1.HoverEdge));
            Assert.That(geometry.EdgeColours, Does.Contain(ExplicitStrategicCellMapV1.SelectedEdge));
            Assert.That(geometry.FaceVertices.All(value => value.y > 0f), Is.True);

            var faces = geometry.CreateFaceMesh();
            var edges = geometry.CreateEdgeMesh();
            Assert.That(faces.subMeshCount, Is.EqualTo(1));
            Assert.That(edges.subMeshCount, Is.EqualTo(1));
            Assert.That(faces.vertexCount, Is.EqualTo(24));
            Assert.That(edges.vertexCount, Is.EqualTo(100));
            UnityEngine.Object.DestroyImmediate(faces);
            UnityEngine.Object.DestroyImmediate(edges);
        }

        [Test]
        public void NationwideOverview_CoversEveryCellWithBoundedVisualLodGeometry()
        {
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var geometry = ExplicitStrategicCellMapV1.BuildNationwideOverviewGeometry(grid,
                ExplicitStrategicCellMapV1.NationwideOverviewStepCells,
                new GlobalProjectedCoordinate(
                    (GlobalSpatialFoundationV1.GlobalMinX + GlobalSpatialFoundationV1.GlobalMaxX) * 0.5d,
                    (GlobalSpatialFoundationV1.GlobalMinY + GlobalSpatialFoundationV1.GlobalMaxY) * 0.5d),
                2000d, (x, y) => 0.2f);
            var rowSegments = (grid.Rows + 31) / 32;
            var columnSegments = (grid.Columns + 31) / 32;
            var expectedEdges = (rowSegments + 1) * columnSegments +
                                (columnSegments + 1) * rowSegments;

            Assert.That(geometry.CoveredCellCount, Is.EqualTo(grid.CellCount));
            Assert.That(geometry.DisplayStepCells, Is.EqualTo(32));
            Assert.That(geometry.VisibleCellIds, Is.Empty,
                "World LOD must not materialize a seven-million-entry presentation list.");
            Assert.That(geometry.FaceVertices, Is.Empty);
            Assert.That(geometry.UniqueGridEdgeCount, Is.EqualTo(expectedEdges));
            Assert.That(geometry.EdgeVertices, Has.Count.EqualTo(expectedEdges * 4));
            Assert.That(geometry.EdgeVertices.Count, Is.LessThan(65535));
            var mesh = geometry.CreateEdgeMesh();
            Assert.That(mesh.vertexCount, Is.EqualTo(expectedEdges * 4));
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        [Test]
        public void CameraRig_FreezesThreeReviewEntries()
        {
            foreach (var id in new[]
            {
                StrategicCellCameraRig.NationwideOverview,
                StrategicCellCameraRig.HenanYinOverview,
                StrategicCellCameraRig.LuoyangSelection,
                StrategicCellCameraRig.MountainTerrain,
                StrategicCellCameraRig.BuildableFacilityReview
            })
            {
                var preset = StrategicCellCameraRig.Get(id);
                Assert.That(preset.Id, Is.EqualTo(id));
                Assert.That(preset.Size, Is.GreaterThan(0f));
                if (id == StrategicCellCameraRig.NationwideOverview)
                {
                    Assert.That(preset.IsWorldView, Is.True);
                    Assert.That(preset.DetailLevel, Is.EqualTo(VisualTerrainDetailLevel.World));
                }
                else
                {
                    Assert.That(preset.IsWorldView, Is.False);
                    Assert.That(preset.Size, id ==
                        StrategicCellCameraRig.BuildableFacilityReview
                            ? Is.InRange(4.5f, 8f) : Is.InRange(8f, 20f));
                    Assert.That(preset.DetailLevel,
                        Is.GreaterThanOrEqualTo(VisualTerrainDetailLevel.City));
                }
            }
        }
    }
}
