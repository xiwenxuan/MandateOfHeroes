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
    public sealed class LuoyangCountyWorldSpacePresentationV1PlayModeTests
    {
        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator CountyRoute_UsesWorldSpaceAndPreservesWorldState()
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
            var snapshot = WorldSnapshotSerializer.Serialize(game.BoundWorld);

            Assert.That(game.ShowCountyView(), Is.True, game.LastMessage);
            yield return null;
            var planning = game.CountyPlanning;
            var world = planning.WorldSpacePresentation;
            Assert.That(planning.UsesWorldSpacePresentation, Is.True,
                planning.LastError);
            Assert.That(world.IsVisible, Is.True);
            Assert.That(world.DebugVisible, Is.False);
            Assert.That(world.Summary.FacilityCount, Is.EqualTo(2084));
            Assert.That(world.Summary.PresentationVersion,
                Is.EqualTo(CountyWorldSpacePresentationPlan.Version));
            Assert.That(world.Summary.FarAggregateCount,
                Is.GreaterThan(0).And.LessThan(
                    world.Summary.FarSuppressedOrdinaryFacilityCount));
            Assert.That(world.FarOrdinaryFacilityDetailObjectCount, Is.Zero);
            Assert.That(world.WorldRoot.Find("Urban Fabric/Far Aggregates")
                .gameObject.activeInHierarchy, Is.True);
            Assert.That(world.WorldRoot.Find("Facilities/Far Landmark Models")
                .gameObject.activeInHierarchy, Is.True);

            Assert.That(game.ShowCountyUrbanAreaView(), Is.True);
            yield return null;
            Assert.That(planning.PresentationLod, Is.EqualTo(
                CountyMapPresentationLod.Mid));
            Assert.That(game.ShowCountyPlanningSubView(), Is.True);
            yield return null;
            Assert.That(planning.PresentationLod, Is.EqualTo(
                CountyMapPresentationLod.Near));
            Assert.That(world.PlanningGridGameObjectCount,
                Is.LessThanOrEqualTo(1));
            Assert.That(world.DetailedFacilityObjectCount,
                Is.LessThanOrEqualTo(96));
            Assert.That(WorldSnapshotSerializer.Serialize(game.BoundWorld),
                Is.EqualTo(snapshot));
        }
    }
}
