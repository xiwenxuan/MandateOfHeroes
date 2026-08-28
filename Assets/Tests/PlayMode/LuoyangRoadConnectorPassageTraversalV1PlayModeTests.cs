using System.Collections;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class LuoyangRoadConnectorPassageTraversalV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1");

        private static string PedestrianEvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1");

        private static string ClickWalkEvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1");

        [UnityTest]
        public IEnumerator DenseCityWindow_ShowsModeledConnectorsAndClosedGate()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<
                HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(),
                Is.True, controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangBuildingPerformanceReview);
            yield return null;
            yield return null;

            Assert.That(controller.ProductionStatus, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.StatusId));
            Assert.That(controller.LuoyangRoadConnectorPassageTraversalStatus,
                Is.EqualTo(LuoyangRoadConnectorPassageTraversalIds.StatusId));
            Assert.That(controller.LuoyangPassagePedestrianPresentationStatus,
                Is.EqualTo(
                    LuoyangPassagePedestrianPresentationIds.StatusId));
            Assert.That(controller.LuoyangRefinedRoadNavigationEdgeCount,
                Is.EqualTo(402));
            Assert.That(controller.LuoyangModeledRoadConnectorCount,
                Is.EqualTo(28));
            Assert.That(controller.LuoyangPassageTraversalCount,
                Is.EqualTo(20));
            Assert.That(controller.RuntimeLuoyangFacilitySelectionProxyCount,
                Is.EqualTo(549));
            Assert.That(controller.RuntimeLuoyangRoadNavigationEdgeCount,
                Is.GreaterThan(0));
            Assert.That(controller.RuntimeLuoyangModeledRoadConnectorEdgeCount,
                Is.GreaterThan(0));
            Assert.That(controller.RuntimeLuoyangPassagePresentationCount,
                Is.GreaterThan(0));
            Assert.That(controller.RuntimeLuoyangActivePedestrianBlockerCount,
                Is.Zero);
            Assert.That(GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime
                    .ModeledConnectorOverlayName), Is.Not.Null);
            Assert.That(GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime
                    .PassagePedestrianPresentationRootName), Is.Not.Null);

            const string gateId =
                LuoyangGateIdentityKitIds.NorthPalaceSouthGate;
            Assert.That(controller.SelectLuoyangFacility(gateId), Is.True);
            var passagePresentation = controller
                .GetLuoyangPassagePedestrianPresentation(gateId);
            Assert.That(passagePresentation.NavigationBlocker.isTrigger,
                Is.False);
            Assert.That(passagePresentation.NavigationBlocker.enabled,
                Is.False);
            Assert.That(passagePresentation.VisualStateId, Is.EqualTo(
                LuoyangPassagePedestrianPresentationIds.OpenVisualStateId));
            Assert.That(controller.FindLuoyangFacilityPath(gateId, gateId).Count,
                Is.EqualTo(1));
            Assert.That(controller.SetLuoyangPassageTraversalStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                184001, "passage.reason.playmode-close-review.v1"), Is.True);
            Assert.That(controller.GetLuoyangPassageTraversalStatus(gateId),
                Is.EqualTo(LuoyangRoadConnectorPassageTraversalIds
                    .ClosedStatusId));
            Assert.That(controller.FindLuoyangFacilityPath(gateId, gateId),
                Is.Empty);
            Assert.That(controller.RuntimeLuoyangPassageStateMarkerCount,
                Is.EqualTo(1));
            Assert.That(controller.RuntimeLuoyangActivePedestrianBlockerCount,
                Is.EqualTo(1));
            Assert.That(passagePresentation.NavigationBlocker.enabled,
                Is.True);
            Assert.That(passagePresentation.VisualStateId, Is.EqualTo(
                LuoyangPassagePedestrianPresentationIds.ClosedVisualStateId));
            var blocked = GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime
                    .BlockedPassageOverlayName);
            Assert.That(blocked, Is.Not.Null);
            Assert.That(blocked.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(blocked.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.GreaterThan(0));
            yield return null;
            yield return null;

            Directory.CreateDirectory(EvidenceRoot);
            var screenshotRoot = Path.Combine(EvidenceRoot, "Screenshots");
            Directory.CreateDirectory(screenshotRoot);
            var screenshotPath = Path.Combine(screenshotRoot,
                "01_DENSE_CITY_MODELED_CONNECTORS_AND_CLOSED_GATE.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(screenshotPath, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            Assert.That(File.Exists(screenshotPath), Is.True);
            Assert.That(new FileInfo(screenshotPath).Length,
                Is.GreaterThan(12000));
            AssertScreenshotHasVisualVariance(screenshotPath);

            var evidenceCamera = Camera.main ??
                                 Object.FindObjectOfType<Camera>();
            Assert.That(evidenceCamera, Is.Not.Null);
            var previousCameraPosition = evidenceCamera.transform.position;
            var previousCameraRotation = evidenceCamera.transform.rotation;
            var previousOrthographic = evidenceCamera.orthographic;
            var previousOrthographicSize = evidenceCamera.orthographicSize;
            var closeScreenshotRoot = Path.Combine(PedestrianEvidenceRoot,
                "Screenshots");
            Directory.CreateDirectory(closeScreenshotRoot);
            var closeScreenshotPath = Path.Combine(closeScreenshotRoot,
                "01_CLOSED_GATE_PEDESTRIAN_BLOCKER.png");
            try
            {
                var center = passagePresentation.transform.position +
                             Vector3.up * 0.12f;
                evidenceCamera.orthographic = true;
                evidenceCamera.orthographicSize = 0.95f;
                evidenceCamera.transform.position = center +
                    new Vector3(1.40f, 0.95f, -1.40f);
                evidenceCamera.transform.LookAt(center);
                controller.CaptureEvidence(closeScreenshotPath, 1200, 800);
            }
            finally
            {
                evidenceCamera.transform.position = previousCameraPosition;
                evidenceCamera.transform.rotation = previousCameraRotation;
                evidenceCamera.orthographic = previousOrthographic;
                evidenceCamera.orthographicSize = previousOrthographicSize;
            }
            yield return null;
            Assert.That(File.Exists(closeScreenshotPath), Is.True);
            Assert.That(new FileInfo(closeScreenshotPath).Length,
                Is.GreaterThan(8000));
            AssertScreenshotHasVisualVariance(closeScreenshotPath);

            Assert.That(controller.SetLuoyangPassageTraversalStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                184002, "passage.reason.playmode-damaged-review.v1"), Is.True);
            Assert.That(controller.RuntimeLuoyangActivePedestrianBlockerCount,
                Is.Zero);
            Assert.That(controller.RuntimeLuoyangDamagedPassagePresentationCount,
                Is.EqualTo(1));
            Assert.That(passagePresentation.NavigationBlocker.enabled,
                Is.False);
            Assert.That(passagePresentation.VisualStateId, Is.EqualTo(
                LuoyangPassagePedestrianPresentationIds.DamagedVisualStateId));
            Assert.That(controller.FindLuoyangFacilityPath(gateId, gateId).Count,
                Is.EqualTo(1));

            Assert.That(controller.SetLuoyangPassageTraversalStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                184003, "passage.reason.playmode-destroyed-review.v1"), Is.True);
            Assert.That(controller.RuntimeLuoyangActivePedestrianBlockerCount,
                Is.EqualTo(1));
            Assert.That(controller.RuntimeLuoyangDestroyedPassagePresentationCount,
                Is.EqualTo(1));
            Assert.That(passagePresentation.NavigationBlocker.enabled,
                Is.True);
            Assert.That(passagePresentation.VisualStateId, Is.EqualTo(
                LuoyangPassagePedestrianPresentationIds
                    .DestroyedVisualStateId));
            Assert.That(controller.FindLuoyangFacilityPath(gateId, gateId),
                Is.Empty);

            controller.ResetLuoyangPassageTraversalSession();
            Assert.That(controller.GetLuoyangPassageTraversalStatus(gateId),
                Is.EqualTo(LuoyangRoadConnectorPassageTraversalIds.OpenStatusId));
            Assert.That(controller.RuntimeLuoyangPassageStateMarkerCount,
                Is.Zero);
            Assert.That(controller.RuntimeLuoyangActivePedestrianBlockerCount,
                Is.Zero);
            Assert.That(controller.RuntimeLuoyangDamagedPassagePresentationCount,
                Is.Zero);
            Assert.That(controller.RuntimeLuoyangDestroyedPassagePresentationCount,
                Is.Zero);
            Assert.That(controller.FindLuoyangFacilityPath(gateId, gateId).Count,
                Is.EqualTo(1));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.RuntimeLuoyangFacilitySelectionProxyCount,
                Is.Zero);
            Assert.That(controller.RuntimeLuoyangPassagePresentationCount,
                Is.Zero);
            Assert.That(GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime.RootName), Is.Null);
        }

        [UnityTest]
        public IEnumerator BoundWorld_UsesCommandsAndSurvivesSnapshotRoundTrip()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<
                HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(),
                Is.True, controller.LastError);

            var world = WorldState.Create(184);
            var runtime = new WorldCommandRuntime();
            var initialization = controller.BindLuoyangPassageWorld(
                world, runtime);
            Assert.That(initialization.ProcessedCommands, Is.EqualTo(1));
            Assert.That(controller.LuoyangPassageWorldBound, Is.True);
            Assert.That(controller.PersistedLuoyangPassageTraversalCount,
                Is.EqualTo(20));

            const string gateId =
                LuoyangGateIdentityKitIds.NorthPalaceSouthGate;
            Assert.That(controller.SetLuoyangPassageTraversalStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                0, "passage.reason.playmode-persisted-close.v1"), Is.True);
            Assert.That(controller.GetLuoyangPassageTraversalStatus(gateId),
                Is.EqualTo(LuoyangRoadConnectorPassageTraversalIds
                    .ClosedStatusId));
            Assert.That(controller.FindLuoyangFacilityPath(gateId, gateId),
                Is.Empty);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.LuoyangPassageTraversals.Single(item =>
                    item.FacilityId == gateId).TraversalStatusId,
                Is.EqualTo(LuoyangRoadConnectorPassageTraversalIds
                    .ClosedStatusId));
            Assert.That(loaded.PersistentWorldCommands, Has.Count.EqualTo(2));
            Assert.That(loaded.WorldEventOutbox, Has.Count.EqualTo(2));

            Assert.Throws<System.InvalidOperationException>(() =>
                controller.ResetLuoyangPassageTraversalSession());
            controller.UnbindLuoyangPassageWorld();
            Assert.That(controller.LuoyangPassageWorldBound, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DenseCityWindow_ClickWalkActorUsesRouteAndStopsForClosedGate()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<
                HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(),
                Is.True, controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangBuildingPerformanceReview);
            yield return null;
            yield return null;

            Assert.That(controller.LuoyangClickToWalkPedestrianStatus,
                Is.EqualTo(LuoyangClickToWalkPedestrianIds.StatusId));
            Assert.That(GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime
                    .ClickWalkPedestrianRootName), Is.Not.Null);
            var actor = controller.GetLuoyangClickWalkPedestrian();
            Assert.That(actor.ActorId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.PreviewActorId));
            Assert.That(actor.CollisionProxy, Is.Not.Null);
            Assert.That(actor.CollisionProxy.isTrigger, Is.False);
            Assert.That(actor.MovementStateId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.ReadyStateId));

            const string gateId =
                LuoyangGateIdentityKitIds.NorthPalaceSouthGate;
            var approaches = controller.GetLuoyangPassageApproachFacilityIds(
                gateId);
            Assert.That(approaches.Count, Is.EqualTo(2));
            Assert.That(controller.PlaceLuoyangPedestrianAtFacility(
                approaches[0]), Is.True);
            var gatePresentation = controller
                .GetLuoyangPassagePedestrianPresentation(gateId);
            var ray = new Ray(gatePresentation.transform.position +
                              Vector3.up * 4f, Vector3.down);
            Assert.That(controller.TrySetLuoyangPedestrianDestination(ray),
                Is.True);
            Assert.That(controller.LuoyangPedestrianTargetFacilityId,
                Is.EqualTo(gateId));
            Assert.That(controller.LuoyangPedestrianRouteFacilityIds,
                Does.Contain(gateId));
            Assert.That(controller.LuoyangPedestrianIsWalking, Is.True);
            Assert.That(controller.LuoyangPedestrianRouteNodeCount,
                Is.GreaterThanOrEqualTo(2));
            Assert.That(controller.LuoyangPedestrianRouteDistanceMetres,
                Is.GreaterThan(0f));
            Assert.That(controller.LuoyangPedestrianEstimatedDurationSeconds,
                Is.GreaterThan(0f));
            var routeObject = GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime.ClickWalkRouteName);
            Assert.That(routeObject, Is.Not.Null);
            Assert.That(routeObject.GetComponent<MeshRenderer>().enabled,
                Is.True);
            Assert.That(routeObject.GetComponent<MeshFilter>().sharedMesh
                .vertexCount, Is.GreaterThan(0));

            Assert.That(controller.StepLuoyangPedestrian(100f), Is.True);
            Assert.That(controller.LuoyangPedestrianCurrentFacilityId,
                Is.EqualTo(gateId));
            Assert.That(actor.MovementStateId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.ArrivedStateId));

            Directory.CreateDirectory(ClickWalkEvidenceRoot);
            var screenshotRoot = Path.Combine(ClickWalkEvidenceRoot,
                "Screenshots");
            Directory.CreateDirectory(screenshotRoot);
            var screenshotPath = Path.Combine(screenshotRoot,
                "01_OPEN_GATE_CLICK_WALK_ROUTE_AND_ACTOR.png");
            var evidenceCamera = Camera.main ??
                                 Object.FindObjectOfType<Camera>();
            Assert.That(evidenceCamera, Is.Not.Null);
            var previousCameraPosition = evidenceCamera.transform.position;
            var previousCameraRotation = evidenceCamera.transform.rotation;
            var previousOrthographic = evidenceCamera.orthographic;
            var previousOrthographicSize = evidenceCamera.orthographicSize;
            try
            {
                var center = gatePresentation.transform.position +
                             Vector3.up * 0.10f;
                evidenceCamera.orthographic = true;
                evidenceCamera.orthographicSize = 0.82f;
                evidenceCamera.transform.position = center +
                    new Vector3(1.18f, 0.86f, -1.18f);
                evidenceCamera.transform.LookAt(center);
                controller.CaptureEvidence(screenshotPath, 1200, 800);
            }
            finally
            {
                evidenceCamera.transform.position = previousCameraPosition;
                evidenceCamera.transform.rotation = previousCameraRotation;
                evidenceCamera.orthographic = previousOrthographic;
                evidenceCamera.orthographicSize = previousOrthographicSize;
            }
            yield return null;
            Assert.That(File.Exists(screenshotPath), Is.True);
            Assert.That(new FileInfo(screenshotPath).Length,
                Is.GreaterThan(8000));
            AssertScreenshotHasVisualVariance(screenshotPath);

            Assert.That(controller.PlaceLuoyangPedestrianAtFacility(
                approaches[0]), Is.True);
            Assert.That(controller.SetLuoyangPedestrianDestination(
                approaches[1]), Is.True);
            Assert.That(controller.LuoyangPedestrianRouteFacilityIds,
                Does.Contain(gateId));
            Assert.That(controller.StepLuoyangPedestrian(0.5f), Is.True);
            Assert.That(controller.SetLuoyangPassageTraversalStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                190001, "passage.reason.click-walk-close.v1"), Is.True);
            Assert.That(controller.LuoyangPedestrianIsWalking, Is.False);
            Assert.That(controller.LuoyangPedestrianMovementStateId,
                Is.EqualTo(LuoyangClickToWalkPedestrianIds.BlockedStateId));
            Assert.That(controller.LuoyangPedestrianLastStopReasonId,
                Is.EqualTo(
                    LuoyangClickToWalkPedestrianIds.BlockedPassageReasonId));
            Assert.That(routeObject.GetComponent<MeshRenderer>().enabled,
                Is.False);
            Assert.That(gatePresentation.NavigationBlocker.enabled, Is.True);

            Assert.That(controller.SetLuoyangPassageTraversalStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                190002, "passage.reason.click-walk-damaged.v1"), Is.True);
            Assert.That(controller.PlaceLuoyangPedestrianAtFacility(
                approaches[0]), Is.True);
            Assert.That(controller.SetLuoyangPedestrianDestination(gateId),
                Is.True);
            Assert.That(controller.LuoyangPedestrianIsWalking, Is.True);
            Assert.That(gatePresentation.NavigationBlocker.enabled, Is.False);

            controller.SetWorldView();
            yield return null;
            Assert.That(GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime
                    .ClickWalkPedestrianRootName), Is.Null);
            Assert.That(controller.LuoyangPedestrianActorId, Is.Null);
        }

        private static void AssertScreenshotHasVisualVariance(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True);
                var sampledColors = texture.GetPixels32()
                    .Where((_, index) => index % 997 == 0)
                    .Select(item => item.r + ":" + item.g + ":" + item.b)
                    .Distinct().Count();
                Assert.That(sampledColors, Is.GreaterThan(32),
                    "Evidence screenshot is visually blank or nearly uniform.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
