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
            Assert.That(GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime
                    .ModeledConnectorOverlayName), Is.Not.Null);

            const string gateId =
                LuoyangGateIdentityKitIds.NorthPalaceSouthGate;
            Assert.That(controller.SelectLuoyangFacility(gateId), Is.True);
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

            controller.ResetLuoyangPassageTraversalSession();
            Assert.That(controller.GetLuoyangPassageTraversalStatus(gateId),
                Is.EqualTo(LuoyangRoadConnectorPassageTraversalIds.OpenStatusId));
            Assert.That(controller.RuntimeLuoyangPassageStateMarkerCount,
                Is.Zero);
            Assert.That(controller.FindLuoyangFacilityPath(gateId, gateId).Count,
                Is.EqualTo(1));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.RuntimeLuoyangFacilitySelectionProxyCount,
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
