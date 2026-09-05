using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class LuoyangCountyVisualConstructionInteractionV1Tests
    {
        [Test]
        public void PlanningPresentation_ExposesFormalToolbarStateGhostAndOverlays()
        {
            var root = new GameObject("County Visual Planning Test");
            try
            {
                var planning = root.AddComponent<
                    LuoyangCountyPlanningPresentationController>();
                Assert.That(planning.Begin(
                    Luoyang50mCountySpatialPrototypeIds.CountyId), Is.True,
                    planning.LastError);

                Assert.That(planning.ToolState.PrimaryTool,
                    Is.EqualTo(CountyPlanningPrimaryTool.Building));
                Assert.That(planning.ShouldDrawBuildingGhost, Is.True);
                Assert.That(planning.CurrentFootprint, Is.Not.Null);
                Assert.That(planning.CurrentFootprint.Entrances, Is.Not.Empty);
                Assert.That(planning.MapOverlays.AdministrativeVisible,
                    Is.True);
                Assert.That(planning.MapOverlays.RoadsVisible, Is.True);
                Assert.That(planning.MapOverlays.RiversVisible, Is.True);
                Assert.That(planning.MapOverlays.GridVisible, Is.True);

                Assert.That(planning.SetOverlayVisible("roads", false),
                    Is.True);
                Assert.That(planning.MapOverlays.RoadsVisible, Is.False);
                Assert.That(planning.SetOverlayVisible("terrain", true),
                    Is.True);
                Assert.That(planning.MapOverlays.TerrainAnalysisVisible,
                    Is.True);
                planning.ActivateTool(CountyPlanningPrimaryTool.Road);
                Assert.That(planning.ToolState.PrimaryTool,
                    Is.EqualTo(CountyPlanningPrimaryTool.Road));
                Assert.That(planning.ShouldDrawBuildingGhost, Is.False);
                planning.CancelPlanningTool();
                Assert.That(planning.ToolState.PrimaryTool,
                    Is.EqualTo(CountyPlanningPrimaryTool.None));
                Assert.That(planning.PlanningCellGameObjectCount,
                    Is.EqualTo(0));
                Assert.That(WorldState.CurrentSchemaVersion, Is.EqualTo(79));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
