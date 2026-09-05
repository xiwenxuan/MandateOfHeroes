using System.Collections;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class
        McfViewScaleCountySandboxPresentationLodV1PlayModeTests
    {
        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator McfRoute_UsesOneWorldAndScaleAwarePresentation()
        {
            yield return SceneManager.LoadSceneAsync("PlayableDemo",
                LoadSceneMode.Single);
            yield return null;
            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            if (dashboard.DirectGame == null ||
                !dashboard.DirectGame.IsActive)
                Assert.That(dashboard.StartRecommendedLuoyangExperience(),
                    Is.True);
            yield return null;
            var game = dashboard.DirectGame;
            var worldBefore = WorldSnapshotSerializer.Serialize(
                game.BoundWorld);

            Assert.That(game.ShowWorldView(), Is.True, game.LastMessage);
            var map = game.NaturalMap;
            Assert.That(game.ViewMode, Is.EqualTo(
                LuoyangPlayableViewMode.World));
            Assert.That(map.CellOverlayVisible, Is.False,
                "The 2 km strategic grid is an optional overlay.");
            Assert.That(map.AdministrativeOverlayVisible, Is.True,
                "The strategic world view keeps administrative context.");
            Assert.That(map.StrategicMapPresentationVisible, Is.True);
            Assert.That(map.TransportOverlayVisible, Is.False);
            Assert.That(map.VisibleStrategicRoadRouteCount,
                Is.GreaterThan(0).And.LessThan(
                    map.StrategicRoadSourceRouteCount));
            map.SetTransportOverlayVisible(true);
            Assert.That(map.VisibleStrategicRoadRouteCount,
                Is.EqualTo(map.StrategicRoadSourceRouteCount));

            Assert.That(game.ShowCountyView(), Is.True, game.LastMessage);
            var planning = game.CountyPlanning;
            Assert.That(map.AdministrativeOverlayVisible, Is.False,
                "The 2 km administrative fill must not occlude the local " +
                "50 m county world-space presentation.");
            Assert.That(map.StrategicMapPresentationVisible, Is.False,
                "The 2 km world roots must not share the county camera.");
            yield return null;
            Assert.That(map.PresentationCamera.orthographic, Is.False,
                "The county world-space owns a perspective camera.");
            Assert.That(map.PresentationCamera.fieldOfView,
                Is.EqualTo(27f).Within(0.01f));
            Assert.That(RenderSettings.fog, Is.False,
                "Strategic-map fog must not flatten the 50 m county view.");
            Assert.That(map.PresentationCamera.backgroundColor,
                Is.EqualTo(new Color(0.08f, 0.09f, 0.06f)));
            Assert.That(planning.PresentationLod, Is.EqualTo(
                CountyMapPresentationLod.Far));
            Assert.That(planning.PresentationSnapshot.VisibleFacilities,
                Is.LessThan(planning.FacilityCount));
            Assert.That(planning.ShouldShowPlanningGrid, Is.False);

            Assert.That(game.ShowCountySubView(
                CountySubViewMode.UrbanArea), Is.True);
            Assert.That(planning.PresentationLod, Is.EqualTo(
                CountyMapPresentationLod.Mid));
            Assert.That(planning.ShouldShowPlanningGrid, Is.False);

            Assert.That(game.ShowCountySubView(
                CountySubViewMode.Planning), Is.True);
            Assert.That(planning.PresentationLod, Is.EqualTo(
                CountyMapPresentationLod.Near));
            Assert.That(planning.ShouldShowPlanningGrid, Is.True);
            Assert.That(planning.LastLodTransitionMilliseconds,
                Is.LessThan(100d));

            Assert.That(game.ShowPersonView(), Is.True, game.LastMessage);
            Assert.That(game.ViewMode, Is.EqualTo(
                LuoyangPlayableViewMode.Person));
            Assert.That(planning.IsActive, Is.False);
            Assert.That(WorldSnapshotSerializer.Serialize(game.BoundWorld),
                Is.EqualTo(worldBefore),
                "M/C/F camera and LOD changes must not mutate world state.");
        }
    }
}
