using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        MapViewRoutingCountyRenameAndInkWorldMapPrototypeV1PlayModeTests
    {
        [Serializable]
        private sealed class StylePerformanceEvidence
        {
            public string UnityVersion;
            public NaturalMapPerformanceSnapshot Current;
            public NaturalMapPerformanceSnapshot Ink;
            public double CurrentFramesPerSecond;
            public double InkFramesPerSecond;
            public int AdministrativeChunks;
            public int AdministrativeSegments;
            public int SettlementMarkers;
        }

        private static readonly string EvidenceRoot = Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "Evidence",
            "MapViewRoutingCountyRenameAndInkWorldMapPrototypeV1");
        private static readonly string StrategicDioramaEvidenceRoot =
            Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Evidence",
                "HanWorldColored3DStrategicDioramaPrototypeV1");

        [Serializable]
        private sealed class StrategicDioramaPerformanceEvidence
        {
            public string UnityVersion;
            public NaturalMapPerformanceSnapshot Natural;
            public NaturalMapPerformanceSnapshot Diorama;
            public int SettlementMarkers;
            public int SettlementRenderObjects;
            public int FarGridObjects;
            public int NearGridObjects;
        }

        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator HanWorldColored3DStrategicDioramaPrototype_FormalRouteAndContextualLod()
        {
            yield return SceneManager.LoadSceneAsync(
                "PlayableDemo", LoadSceneMode.Single);
            yield return null;
            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            if (dashboard.DirectGame == null || !dashboard.DirectGame.IsActive)
                Assert.That(dashboard.StartRecommendedLuoyangExperience(),
                    Is.True);
            yield return null;
            var game = dashboard.DirectGame;
            var map = game.NaturalMap;
            Assert.That(map.HasRuntimeReferences, Is.True, map.LastError);
            var worldBefore = WorldSnapshotSerializer.Serialize(
                game.BoundWorld);
            var player = new PlayerSession(game.BoundWorld).ControlledPerson;
            Assert.That(map.TryResolveCountyIdForLocation(player.LocationId,
                out var countyId), Is.True);
            Directory.CreateDirectory(StrategicDioramaEvidenceRoot);
            var evidence = new StrategicDioramaPerformanceEvidence
            {
                UnityVersion = Application.unityVersion
            };

            Assert.That(game.ShowWorldView(), Is.True, game.LastMessage);
            Assert.That(game.IsStrategicDioramaWorldStyle, Is.True);
            Assert.That(map.ActiveArtProfileId, Is.EqualTo(
                HanWorldArtProfileCatalog.StrategicDioramaId));
            Assert.That(map.ProductionStatus, Is.EqualTo(
                "HAN_COLOURED_3D_STRATEGIC_DIORAMA_V1_READY_FOR_USER_REVIEW"));
            Assert.That(map.StrategicGridLod,
                Is.EqualTo(StrategicCellGridLod.Off));
            Assert.That(map.StrategicDioramaSettlementCount,
                Is.GreaterThan(0));
            Assert.That(map.StrategicDioramaSettlementRenderObjectCount,
                Is.EqualTo(1));

            map.SetWorldView();
            Assert.That(map.PresentationCamera.transform.eulerAngles.x,
                Is.EqualTo(58f).Within(0.2f));
            evidence.FarGridObjects = map.RuntimeCellOverlayObjectCount;
            CaptureDiorama(map, "01_diorama_world_far.png");

            Assert.That(map.FocusWorldNearCounty(countyId, 330f), Is.True);
            Assert.That(map.StrategicGridLod,
                Is.EqualTo(StrategicCellGridLod.Off));
            CaptureDiorama(map, "02_diorama_world_mid.png");

            Assert.That(map.FocusWorldNearCounty(countyId, 180f), Is.True);
            Assert.That(map.StrategicGridLod,
                Is.EqualTo(StrategicCellGridLod.ExactCell));
            Assert.That(map.StrategicGridStepCells, Is.EqualTo(1));
            evidence.NearGridObjects = map.RuntimeCellOverlayObjectCount;
            CaptureDiorama(map, "03_diorama_world_near_grid.png");

            map.SetAdministrativeLabelLevel(
                AdministrativeMapLabelLevel.CommanderyEquivalent);
            Assert.That(map.AdministrativeRenderedSegmentCount,
                Is.GreaterThan(0));
            CaptureDiorama(map, "04_diorama_commandery_lod.png");

            Assert.That(game.SetWorldMapStrategicDioramaStyle(false), Is.True);
            Assert.That(map.ActiveArtStyle, Is.EqualTo(
                HanWorldArtStyle.ChineseSemiRealistic));
            yield return SampleFrames();
            evidence.Natural = map.GetPerformanceSnapshot(
                Time.unscaledDeltaTime * 1000f);
            CaptureDiorama(map, "05_natural_world_reference.png");

            Assert.That(game.SetWorldMapStrategicDioramaStyle(true), Is.True);
            Assert.That(game.IsStrategicDioramaWorldStyle, Is.True);
            yield return SampleFrames();
            evidence.Diorama = map.GetPerformanceSnapshot(
                Time.unscaledDeltaTime * 1000f);
            evidence.SettlementMarkers =
                map.StrategicDioramaSettlementCount;
            evidence.SettlementRenderObjects =
                map.StrategicDioramaSettlementRenderObjectCount;
            CaptureDiorama(map, "06_diorama_player_default.png");
            File.WriteAllText(Path.Combine(StrategicDioramaEvidenceRoot,
                    "performance-comparison.json"),
                JsonUtility.ToJson(evidence, true));

            Assert.That(WorldSnapshotSerializer.Serialize(game.BoundWorld),
                Is.EqualTo(worldBefore));
        }

        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator MapViewRoutingCountyRenameAndInkWorldMapPrototype_FormalFlowAndEvidence()
        {
            yield return SceneManager.LoadSceneAsync(
                "PlayableDemo", LoadSceneMode.Single);
            yield return null;
            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            if (dashboard.DirectGame == null || !dashboard.DirectGame.IsActive)
                Assert.That(dashboard.StartRecommendedLuoyangExperience(),
                    Is.True);
            yield return null;
            var game = dashboard.DirectGame;
            var map = game.NaturalMap;
            Assert.That(map.HasRuntimeReferences, Is.True, map.LastError);
            Directory.CreateDirectory(EvidenceRoot);
            var worldBefore = WorldSnapshotSerializer.Serialize(
                game.BoundWorld);
            var player = new PlayerSession(game.BoundWorld).ControlledPerson;
            var playerCell = player.CurrentCellId64;
            var playerFacility = player.CurrentFacilityId;
            Assert.That(map.TryResolveCountyIdForLocation(player.LocationId,
                out var playerCountyId), Is.True);
            Assert.That(playerCountyId, Is.EqualTo(
                Luoyang50mCountySpatialPrototypeIds.CountyId));
            var evidence = new StylePerformanceEvidence
            {
                UnityVersion = Application.unityVersion
            };

            Assert.That(game.ShowWorldView(), Is.True, game.LastMessage);
            Assert.That(game.ViewMode, Is.EqualTo(
                LuoyangPlayableViewMode.World));
            Assert.That(game.SetWorldMapInkStyle(false), Is.True);
            map.SetWorldView();
            Capture(map, "01_current_world_far.png");
            yield return SampleFrames();
            evidence.CurrentFramesPerSecond =
                1d / Math.Max(0.0001d, Time.unscaledDeltaTime);
            evidence.Current = map.GetPerformanceSnapshot(
                (float)(1000d / Math.Max(0.001d,
                    evidence.CurrentFramesPerSecond)));

            Assert.That(game.SetWorldMapInkStyle(true), Is.True);
            map.SetWorldView();
            Capture(map, "02_ink_world_far.png");
            yield return SampleFrames();
            evidence.InkFramesPerSecond =
                1d / Math.Max(0.0001d, Time.unscaledDeltaTime);
            evidence.Ink = map.GetPerformanceSnapshot(
                (float)(1000d / Math.Max(0.001d,
                    evidence.InkFramesPerSecond)));
            Assert.That(map.RuntimeStrategicRoadMeshCount, Is.EqualTo(1));
            Assert.That(map.GetVisibleSettlementMarkers().Count,
                Is.GreaterThan(0));

            Assert.That(game.SetWorldMapInkStyle(false), Is.True);
            Assert.That(map.FocusWorldNearCounty(playerCountyId, 330f),
                Is.True);
            Capture(map, "03_current_world_mid.png");
            Assert.That(game.SetWorldMapInkStyle(true), Is.True);
            Assert.That(map.FocusWorldNearCounty(playerCountyId, 330f),
                Is.True);
            Capture(map, "04_ink_world_mid.png");
            Assert.That(game.SetWorldMapInkStyle(false), Is.True);
            Assert.That(map.FocusWorldNearCounty(playerCountyId, 180f),
                Is.True);
            Capture(map, "05_current_world_near.png");
            Assert.That(game.SetWorldMapInkStyle(true), Is.True);
            Assert.That(map.FocusWorldNearCounty(playerCountyId, 180f),
                Is.True);
            Capture(map, "06_ink_world_near.png");
            Capture(map, "07_m_world_view.png");

            map.SetAdministrativeLabelLevel(
                AdministrativeMapLabelLevel.County);
            Assert.That(map.AdministrativeSelection, Is.Null);
            Assert.That(game.ShowCountyView(), Is.True, game.LastMessage);
            Assert.That(game.ViewMode, Is.EqualTo(
                LuoyangPlayableViewMode.County));
            Assert.That(game.ObservedCountyId, Is.EqualTo(
                Luoyang50mCountySpatialPrototypeIds.CountyId));
            Assert.That(game.CountySubView, Is.EqualTo(
                CountySubViewMode.Overview));
            Assert.That(game.CountyPlanning.FacilityCount,
                Is.EqualTo(2_084));
            CaptureCounty(game, "08_c_county_view.png");
            CaptureCounty(game, "09_county_overview.png");

            Assert.That(game.ShowCountySubView(
                CountySubViewMode.UrbanArea), Is.True);
            var overviewRows = game.CountyPlanning.ViewRows;
            CaptureCounty(game, "10_county_luoyang_urban.png");
            Assert.That(overviewRows, Is.LessThan(320f));
            Assert.That(game.ShowCountySubView(
                CountySubViewMode.Planning), Is.True);
            CaptureCounty(game, "11_county_planning.png");
            var rect = new Rect(0f, 0f, 800f, 400f);
            var rowBeforePan = game.CountyPlanning.ViewMinimumRow;
            Assert.That(game.PanCountyPlanningViewByGuiDelta(
                new Vector2(80f, -40f), rect), Is.True);
            Assert.That(game.CountyPlanning.ViewMinimumRow,
                Is.Not.EqualTo(rowBeforePan));
            var rotationBefore = game.CountyPlanning.ViewRotationDegrees;
            Assert.That(game.RotateCountyPlanningViewByGuiDelta(
                new Vector2(30f, 0f)), Is.True);
            Assert.That(game.CountyPlanning.ViewRotationDegrees,
                Is.Not.EqualTo(rotationBefore));
            Assert.That(game.CountyPlanning.ZoomViewport(1f,
                new Vector2(0.5f, 0.5f)), Is.True);

            Assert.That(game.ShowPersonView(), Is.True, game.LastMessage);
            Assert.That(game.ViewMode, Is.EqualTo(
                LuoyangPlayableViewMode.Person));
            Assert.That(new PlayerSession(game.BoundWorld).ControlledPerson
                .CurrentCellId64, Is.EqualTo(playerCell));
            Assert.That(new PlayerSession(game.BoundWorld).ControlledPerson
                .CurrentFacilityId, Is.EqualTo(playerFacility));
            Capture(map, "12_f_person_view.png");

            Assert.That(game.ShowWorldView(), Is.True);
            Assert.That(game.SetWorldMapInkStyle(true), Is.True);
            map.SetAdministrativeLabelLevel(
                AdministrativeMapLabelLevel.County);
            Capture(map, "13_ink_admin_boundaries.png");
            Assert.That(map.FocusWorldNearCounty(playerCountyId, 180f),
                Is.True);
            Capture(map, "14_ink_river_road.png");
            evidence.AdministrativeChunks =
                map.AdministrativeRenderedChunkCount;
            evidence.AdministrativeSegments =
                map.AdministrativeRenderedSegmentCount;
            evidence.SettlementMarkers =
                map.GetVisibleSettlementMarkers().Count;
            File.WriteAllText(Path.Combine(EvidenceRoot,
                    "performance-comparison.json"),
                JsonUtility.ToJson(evidence, true));

            Assert.That(WorldSnapshotSerializer.Serialize(game.BoundWorld),
                Is.EqualTo(worldBefore));
        }

        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator MapViewRoutingCountyRenameAndInkWorldMapPrototype_DeprecatedMapCityWrapperIsSafe()
        {
            var root = new GameObject("Deprecated city wrapper check");
            var cameraObject = new GameObject("Deprecated wrapper camera");
            var camera = cameraObject.AddComponent<Camera>();
            var map = root.AddComponent<HanWorldNaturalMapController>();
            map.SetPresentationCamera(camera);
            Assert.That(map.HasRuntimeReferences, Is.False);
            Assert.DoesNotThrow(() => map.SetWorldView());
            Assert.That(map.HasRuntimeReferences, Is.True, map.LastError);
#pragma warning disable CS0618
            Assert.DoesNotThrow(() =>
                map.ShowPlayableLuoyangCityOverview());
#pragma warning restore CS0618
            Assert.That(map.LuoyangPlayablePresentationMode,
                Is.Not.EqualTo(LuoyangPlayablePresentationMode.CityOverview));
            Assert.That(map.AdministrativeMapViewState.ViewMode,
                Is.EqualTo(AdministrativeMapViewMode.CountyPlanning));
            Assert.That(map.AdministrativeMapViewState.PlanningCountyId,
                Is.EqualTo(Luoyang50mCountySpatialPrototypeIds.CountyId));
            Object.Destroy(root);
            Object.Destroy(cameraObject);
            yield return null;
        }

        private static void Capture(HanWorldNaturalMapController map,
            string fileName)
        {
            var path = Path.Combine(EvidenceRoot, fileName);
            if (File.Exists(path)) File.Delete(path);
            map.CaptureEvidence(path, 1280, 720);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(10_000),
                fileName);
            AssertDioramaVisualContent(path);
        }

        private static void AssertDioramaVisualContent(string path)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                Assert.That(image.LoadImage(File.ReadAllBytes(path)), Is.True,
                    path);
                var pixels = image.GetPixels32();
                var minimum = 255;
                var maximum = 0;
                var distinct = new HashSet<int>();
                var step = Math.Max(1, pixels.Length / 4096);
                for (var index = 0; index < pixels.Length; index += step)
                {
                    var pixel = pixels[index];
                    var luminance = (pixel.r * 299 + pixel.g * 587 +
                                     pixel.b * 114) / 1000;
                    minimum = Math.Min(minimum, luminance);
                    maximum = Math.Max(maximum, luminance);
                    distinct.Add((pixel.r / 8 << 10) |
                                 (pixel.g / 8 << 5) | pixel.b / 8);
                }
                Assert.That(maximum - minimum, Is.GreaterThan(18),
                    "Flat strategic-diorama evidence: " + path);
                Assert.That(distinct.Count, Is.GreaterThan(24),
                    "Insufficient strategic-diorama detail: " + path);
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }

        private static void CaptureDiorama(
            HanWorldNaturalMapController map, string fileName)
        {
            var path = Path.Combine(StrategicDioramaEvidenceRoot, fileName);
            if (File.Exists(path)) File.Delete(path);
            map.CaptureEvidence(path, 1280, 720);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(10_000),
                fileName);
        }

        private static void CaptureCounty(PlayableLuoyangGameController game,
            string fileName)
        {
            var path = Path.Combine(EvidenceRoot, fileName);
            if (File.Exists(path)) File.Delete(path);
            game.CountyPlanning.CaptureEvidence(path, 1280, 720);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(10_000),
                fileName);
        }

        private static IEnumerator SampleFrames()
        {
            const int frameCount = 30;
            for (var frame = 0; frame < frameCount; frame++)
                yield return null;
        }
    }
}
