using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    [TestFixture]
    public sealed class DualScaleWorld50mArchitectureV1UnityTests
    {
        private GameObject _root;
        private DualScaleSpatialArchitectureValidationController _controller;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("DualScale Unity Test");
            _controller = _root.AddComponent<
                DualScaleSpatialArchitectureValidationController>();
            Assert.That(_controller.TryInitialize(), Is.True,
                _controller.LastError);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void Presentation_UsesPackedCellsWithoutGameObjectPerCell()
        {
            Assert.That(_controller.PlanningCellCount, Is.EqualTo(6_400));
            Assert.That(_controller.PlanningCellGameObjectCount, Is.Zero);
            Assert.That(_controller.RuntimePlanningCellRenderObjectCount,
                Is.EqualTo(2));
            Assert.That(_controller.RuntimeChunkCount, Is.GreaterThan(0));
            Assert.That(Object.FindObjectsOfType<Transform>().Count(item =>
                item.name.StartsWith("PlanningCell_")), Is.Zero);
        }

        [Test]
        public void Presentation_ContainsRequiredFacilityFootprintsAndColliders()
        {
            var markers = Object.FindObjectsOfType<
                DualScaleFacilityVisualMarker>();
            Assert.That(markers.Length,
                Is.EqualTo(_controller.Scenario.World.Facilities.Count));
            Assert.That(markers.All(marker =>
                marker.GetComponent<Collider>() != null), Is.True);
            Assert.That(markers.Select(marker => marker.FacilityId).Distinct()
                .Count(), Is.EqualTo(markers.Length));
        }

        [Test]
        public void Presentation_SwitchesScaleWithoutChangingWorldSummary()
        {
            var before = _controller.WorldSummaryHash;
            _controller.ShowStrategicView();
            Assert.That(_controller.StrategicViewVisible, Is.True);
            _controller.ShowCountyDetailView();
            Assert.That(_controller.StrategicViewVisible, Is.False);
            Assert.That(_controller.WorldSummaryHash, Is.EqualTo(before));
        }

        [Test]
        public void Presentation_DebugOverlaysAreControllable()
        {
            _controller.SetPlanningGridVisible(false);
            _controller.SetFourPortDebugVisible(true);
            _controller.SetHighObserver(true);
            Assert.That(_controller.PlanningGridVisible, Is.False);
            Assert.That(_controller.FourPortDebugVisible, Is.True);
            Assert.That(_controller.HighObserverEnabled, Is.True);
        }

        [Test]
        public void Presentation_LowAndHighLosViewsHaveDifferentResults()
        {
            _controller.SetHighObserver(false);
            Assert.That(_controller.CurrentLosVisible, Is.False);
            _controller.SetHighObserver(true);
            Assert.That(_controller.CurrentLosVisible, Is.True);
        }

        [Test]
        public void Presentation_FacilitySelectionUsesSameFacilityIdentity()
        {
            _controller.SetSelectedFacility(
                DualScaleSpatialValidationScenarioFactory
                    .StorehouseFacilityId);
            Assert.That(_controller.SelectedFacilityId,
                Is.EqualTo(_controller.Scenario.Facility(
                    DualScaleSpatialValidationScenarioFactory
                        .StorehouseFacilityId).Id));
        }

        [Test]
        public void Presentation_LoadHandlesExposeHotAndWarmResidency()
        {
            Assert.That(_controller.WestCountyLoadHandle.Level,
                Is.EqualTo(Mandate.Simulation.CountySpatialLoadLevel.Hot));
            Assert.That(_controller.EastCountyLoadHandle.Level,
                Is.EqualTo(Mandate.Simulation.CountySpatialLoadLevel.Warm));
            Assert.That(_controller.WestCountyLoadHandle
                .ResidentPlanningCellCount, Is.EqualTo(3_200));
            Assert.That(_controller.EastCountyLoadHandle
                .ResidentPlanningCellCount, Is.Zero);
        }
    }
}
