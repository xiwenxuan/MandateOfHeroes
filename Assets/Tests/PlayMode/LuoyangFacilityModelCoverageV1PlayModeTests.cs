using System.Collections;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class LuoyangFacilityModelCoverageV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_FACILITY_MODEL_COVERAGE_AND_A_TIER_COMPOSITION_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator CompleteKit_PlacesThirtySixModelsAndCapturesEvidence()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);

            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangFacilityCoverageReview);
            yield return null;

            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.LuoyangFacilityCoveragePreviewVisible, Is.True);
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.EqualTo(36));
            Assert.That(controller.BuildableFacilityPlacements, Has.Count.EqualTo(36));
            Assert.That(controller.BuildableFacilityPlacements.Select(item => item.CellId.Value)
                .Distinct().Count(), Is.EqualTo(36));
            Assert.That(controller.BuildableFacilityPlacements.Select(item => item.ModelId),
                Is.EquivalentTo(LuoyangFacilityModelCoverageIds.AllModelIds));
            Assert.That(Object.FindObjectsOfType<HanBuildableFacilityModelInstance>(),
                Has.Length.EqualTo(36));
            Assert.That(controller.RuntimeBuildableFacilityRendererCount,
                Is.GreaterThan(220));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_FACILITY_MODEL_COVERAGE_V1_READY_FOR_USER_REVIEW"));

            Directory.CreateDirectory(EvidenceRoot);
            var path = Path.Combine(EvidenceRoot,
                "01_COMPLETE_THIRTY_SIX_MODEL_COVERAGE_ON_STRATEGIC_CELLS.png");
            controller.CaptureEvidence(path, 1600, 1000);
            yield return null;
            Assert.That(File.Exists(path), Is.True);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangFacilityCoveragePreviewVisible, Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
        }
    }

    public sealed class LuoyangProductionBuildingKitV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_PRODUCTION_BUILDING_KIT_V1", "Screenshots");

        [UnityTest]
        public IEnumerator HighFrequencyKit_UsesProductionMeshesAndThreeLodsInScene()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangFacilityCoverageReview);
            yield return null;

            var productionInstances = Object
                .FindObjectsOfType<HanBuildableFacilityModelInstance>()
                .Where(item => item.ProductionReady).ToArray();
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.EqualTo(36));
            Assert.That(controller.ProductionBuildableFacilityProfileCount,
                Is.EqualTo(10));
            Assert.That(controller.ProductionBuildableFacilityMeshCount,
                Is.EqualTo(8));
            Assert.That(controller.ProductionBuildingKitStatus, Is.EqualTo(
                "LUOYANG_PRODUCTION_BUILDING_KIT_V1_READY_FOR_USER_REVIEW"));
            Assert.That(productionInstances, Has.Length.EqualTo(10));
            Assert.That(productionInstances.Select(item => item.ModelId),
                Is.EquivalentTo(
                    LuoyangProductionBuildingKitIds.HighFrequencyModelIds));
            Assert.That(productionInstances.All(item =>
                item.GetComponent<LODGroup>()?.GetLODs().Length == 3), Is.True);
            Assert.That(productionInstances.SelectMany(item =>
                    item.GetComponentsInChildren<MeshFilter>(true))
                .Count(item => item.sharedMesh != null &&
                    item.sharedMesh.name.StartsWith("HAN_PRODUCTION_",
                        System.StringComparison.Ordinal)),
                Is.GreaterThan(80));

            Directory.CreateDirectory(EvidenceRoot);
            var path = Path.Combine(EvidenceRoot,
                "01_HIGH_FREQUENCY_PRODUCTION_KIT_ON_LUOYANG_REVIEW_GRID.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(path, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            Assert.That(File.Exists(path), Is.True);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
        }
    }

    public sealed class LuoyangHistoricalLandmarkDistinctSilhouettesV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator TenHistoricalFacilities_UseDistinctSilhouettesOnTheirCells()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangHistoricalLandmarkReview);
            yield return null;

            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.LuoyangHistoricalLandmarkPreviewVisible, Is.True);
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.EqualTo(10));
            Assert.That(controller.HistoricalLandmarkProfileCount, Is.EqualTo(10));
            Assert.That(controller.BuildableFacilityPlacements, Has.Count.EqualTo(10));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                    item.RuntimeBindingId),
                Is.EqualTo(LuoyangHistoricalLandmarkKitIds.FacilityIds));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                    item.CellId.Value),
                Is.EquivalentTo(LuoyangHistoricalLandmarkKitIds.CellIds.Values));
            Assert.That(instances, Has.Length.EqualTo(10));
            Assert.That(instances.All(item => item.HistoricalLandmarkReady), Is.True);
            Assert.That(instances.Select(item => item.HistoricalLandmarkSilhouetteId)
                .Distinct().Count(), Is.EqualTo(10));
            Assert.That(instances.All(item =>
                item.GetComponent<LODGroup>()?.GetLODs().Length == 3), Is.True);
            Assert.That(controller.RuntimeBuildableFacilityRendererCount,
                Is.GreaterThan(120));
            Assert.That(controller.HistoricalLandmarkKitStatus, Is.EqualTo(
                "LUOYANG_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1_READY_FOR_USER_REVIEW"));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1_READY_FOR_USER_REVIEW"));

            Directory.CreateDirectory(EvidenceRoot);
            var path = Path.Combine(EvidenceRoot,
                "01_TEN_A_TIER_LANDMARKS_ON_AUTHORITATIVE_LUOYANG_CELLS.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(path, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            Assert.That(File.Exists(path), Is.True);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangHistoricalLandmarkPreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
        }
    }

    public sealed class LuoyangGateIdentityV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1", "Screenshots");

        [UnityTest]
        public IEnumerator FourteenGates_UseIdentitiesDirectionsAndAuthoritativeCells()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangGateIdentityReview);
            yield return null;

            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.LuoyangGateIdentityPreviewVisible, Is.True);
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.EqualTo(14));
            Assert.That(controller.GateIdentityProfileCount, Is.EqualTo(14));
            Assert.That(controller.BuildableFacilityPlacements, Has.Count.EqualTo(14));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                    item.RuntimeBindingId),
                Is.EqualTo(LuoyangGateIdentityKitIds.FacilityIds));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                    item.CellId.Value),
                Is.EquivalentTo(LuoyangGateIdentityKitIds.CellIds.Values));
            Assert.That(instances, Has.Length.EqualTo(14));
            Assert.That(instances.All(item => item.GateIdentityReady), Is.True);
            Assert.That(instances.Select(item => item.GateIdentitySilhouetteId)
                .Distinct().Count(), Is.EqualTo(14));
            Assert.That(instances.All(item =>
                item.GetComponent<LODGroup>()?.GetLODs().Length == 3), Is.True);
            Assert.That(controller.BuildableFacilityPlacements.All(item =>
            {
                var facing = instances.Single(instance =>
                    instance.RuntimeBindingId == item.RuntimeBindingId).VisualFacing;
                return item.RotationDegrees ==
                    LuoyangGateIdentityKitIds.RotationForFacing(facing);
            }), Is.True);
            Assert.That(controller.RuntimeBuildableFacilityRendererCount,
                Is.GreaterThan(160));
            Assert.That(controller.GateIdentityKitStatus, Is.EqualTo(
                "LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1_READY_FOR_USER_REVIEW"));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1_READY_FOR_USER_REVIEW"));

            Directory.CreateDirectory(EvidenceRoot);
            var path = Path.Combine(EvidenceRoot,
                "01_FOURTEEN_GATE_IDENTITIES_ON_AUTHORITATIVE_LUOYANG_CELLS.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(path, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            Assert.That(File.Exists(path), Is.True);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangGateIdentityPreviewVisible, Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
        }
    }

    public sealed class LuoyangMediumFrequencyUrbanFabricV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_MEDIUM_FREQUENCY_URBAN_FABRIC_V1", "Screenshots");

        [UnityTest]
        public IEnumerator FifteenCells_ShowFiveMediumFrequencyUrbanFabricTypes()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangMediumFrequencyUrbanFabricReview);
            yield return null;

            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.LuoyangMediumFrequencyUrbanFabricPreviewVisible,
                Is.True);
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.RuntimeBuildableFacilityModelCount,
                Is.EqualTo(15));
            Assert.That(controller.MediumFrequencyUrbanFabricProfileCount,
                Is.EqualTo(5));
            Assert.That(controller.BuildableFacilityPlacements, Has.Count.EqualTo(15));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                    item.CellId.Value).Distinct().Count(), Is.EqualTo(15));
            Assert.That(controller.BuildableFacilityPlacements
                    .GroupBy(item => item.ModelId)
                    .All(group => group.Count() == 3), Is.True);
            Assert.That(instances, Has.Length.EqualTo(15));
            Assert.That(instances.All(item =>
                item.MediumFrequencyUrbanFabricReady), Is.True);
            Assert.That(instances.Select(item => item.UrbanFabricProfileId)
                .Distinct().Count(), Is.EqualTo(5));
            Assert.That(instances.All(item =>
                item.GetComponent<LODGroup>()?.GetLODs().Length == 3), Is.True);
            Assert.That(controller.RuntimeBuildableFacilityRendererCount,
                Is.GreaterThan(300));
            Assert.That(controller.MediumFrequencyUrbanFabricKitStatus, Is.EqualTo(
                "LUOYANG_MEDIUM_FREQUENCY_URBAN_FABRIC_V1_READY_FOR_USER_REVIEW"));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_MEDIUM_FREQUENCY_URBAN_FABRIC_V1_READY_FOR_USER_REVIEW"));

            Directory.CreateDirectory(EvidenceRoot);
            var path = Path.Combine(EvidenceRoot,
                "01_FIFTEEN_CELL_MEDIUM_FREQUENCY_URBAN_FABRIC.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(path, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            Assert.That(File.Exists(path), Is.True);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangMediumFrequencyUrbanFabricPreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
        }
    }

    public sealed class LuoyangBuildingWholeCityPerformanceAndBatchingV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1");

        [UnityTest]
        public IEnumerator Densest549FacilityWindow_UsesSpatialMaterialBatchesWithinBudget()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangBuildingPerformanceReview);
            yield return null;

            var metrics = controller.LuoyangBuildingBatchMetrics;
            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.LuoyangBuildingPerformancePreviewVisible,
                Is.True);
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.WholeCityBuildingFacilityPlanCount,
                Is.EqualTo(2084));
            Assert.That(controller.WholeCityBuildingSpatialBatchCount,
                Is.EqualTo(64));
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics.WithinBudget, Is.True);
            Assert.That(metrics.ResidentFacilityCount, Is.EqualTo(549));
            Assert.That(metrics.ResidentSpatialBatchCount, Is.EqualTo(9));
            Assert.That(metrics.BuildingRendererBatchCount,
                Is.LessThanOrEqualTo(200));
            Assert.That(metrics.CombinedVertexCount,
                Is.LessThanOrEqualTo(250000));
            Assert.That(metrics.BatchBuildMilliseconds,
                Is.LessThanOrEqualTo(3000d));
            Assert.That(metrics.RendererReductionRatio,
                Is.GreaterThanOrEqualTo(0.85d));
            Assert.That(controller.RuntimeBuildableFacilityRendererCount,
                Is.EqualTo(metrics.BuildingRendererBatchCount));
            Assert.That(Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>(), Is.Empty);
            Assert.That(controller.BuildableFacilityPlacements, Is.Empty);
            Assert.That(controller.LuoyangBuildingPerformanceStatus, Is.EqualTo(
                "LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1_READY_FOR_USER_REVIEW"));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.StatusId));

            Directory.CreateDirectory(EvidenceRoot);
            var metricsPath = Path.Combine(EvidenceRoot,
                "luoyang_building_batch_metrics_v1.json");
            File.WriteAllText(metricsPath, JsonUtility.ToJson(metrics, true));
            var screenshotRoot = Path.Combine(EvidenceRoot, "Screenshots");
            Directory.CreateDirectory(screenshotRoot);
            var screenshotPath = Path.Combine(screenshotRoot,
                "01_DENSEST_549_FACILITY_BATCHED_WINDOW.png");
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
            Assert.That(File.Exists(metricsPath), Is.True);
            Assert.That(new FileInfo(metricsPath).Length, Is.GreaterThan(300));
            Assert.That(File.Exists(screenshotPath), Is.True);
            Assert.That(new FileInfo(screenshotPath).Length,
                Is.GreaterThan(12000));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangBuildingPerformancePreviewVisible,
                Is.False);
            Assert.That(controller.LuoyangBuildingBatchMetrics, Is.Null);
            Assert.That(controller.RuntimeBuildableFacilityRendererCount,
                Is.Zero);
        }
    }

    public sealed class LuoyangCanalWellBridgeInfrastructureProductionV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_CANAL_WELL_BRIDGE_INFRASTRUCTURE_PRODUCTION_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator Actual37InfrastructureFacilities_RenderThreeReviewViewsAndCleanUp()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangInfrastructureOverview);
            yield return null;

            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.LuoyangInfrastructurePreviewVisible, Is.True);
            Assert.That(controller.InfrastructureProductionProfileCount,
                Is.EqualTo(3));
            Assert.That(controller.InfrastructureFacilityPlanCount,
                Is.EqualTo(37));
            Assert.That(controller.BuildableFacilityPlacements.Count,
                Is.EqualTo(37));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                item.CellId.Value).Distinct().Count(), Is.EqualTo(37));
            Assert.That(controller.BuildableFacilityPlacements.Count(item =>
                    item.ModelId ==
                    LuoyangInfrastructureProductionKitIds.CanalModel),
                Is.EqualTo(19));
            Assert.That(controller.BuildableFacilityPlacements.Count(item =>
                    item.ModelId ==
                    LuoyangInfrastructureProductionKitIds.WellModel),
                Is.EqualTo(16));
            Assert.That(controller.BuildableFacilityPlacements.Count(item =>
                    item.ModelId ==
                    LuoyangInfrastructureProductionKitIds.BridgeModel),
                Is.EqualTo(2));
            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances, Has.Length.EqualTo(37));
            Assert.That(instances.All(item =>
                item.InfrastructureProductionReady), Is.True);
            Assert.That(instances.All(item =>
                !string.IsNullOrWhiteSpace(item.InfrastructureTopologyId)),
                Is.True);
            Assert.That(instances.All(item =>
                item.GetComponent<LODGroup>()?.GetLODs().Length == 3), Is.True);
            Assert.That(controller.RuntimeBuildableFacilityRendererCount,
                Is.GreaterThan(200));
            Assert.That(instances.SelectMany(item =>
                item.GetComponentsInChildren<Collider>(true)), Is.Empty);
            Assert.That(controller.LuoyangInfrastructureProductionStatus,
                Is.EqualTo(
                    "LUOYANG_CANAL_WELL_BRIDGE_INFRASTRUCTURE_PRODUCTION_V1_READY_FOR_USER_REVIEW"));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_CANAL_WELL_BRIDGE_INFRASTRUCTURE_PRODUCTION_V1_READY_FOR_USER_REVIEW"));

            Directory.CreateDirectory(EvidenceRoot);
            var overviewPath = Path.Combine(EvidenceRoot,
                "01_ALL_37_INFRASTRUCTURE_ACTUAL_CELLS.png");
            var canalPath = Path.Combine(EvidenceRoot,
                "02_SEVENTEEN_CELL_CANAL_CORRIDOR.png");
            var bridgePath = Path.Combine(EvidenceRoot,
                "03_TWO_BRIDGES_TWO_CANALS_CHAIN.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(overviewPath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangInfrastructureCanalCorridor);
                yield return null;
                Assert.That(controller.LuoyangInfrastructurePreviewVisible,
                    Is.True);
                Assert.That(controller.BuildableFacilityPlacements.Count,
                    Is.EqualTo(37));
                controller.CaptureEvidence(canalPath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangInfrastructureBridgeChain);
                yield return null;
                Assert.That(controller.LuoyangInfrastructurePreviewVisible,
                    Is.True);
                Assert.That(controller.BuildableFacilityPlacements.Count,
                    Is.EqualTo(37));
                controller.CaptureEvidence(bridgePath, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            foreach (var path in new[] { overviewPath, canalPath, bridgePath })
            {
                Assert.That(File.Exists(path), Is.True, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000),
                    path);
            }

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangInfrastructurePreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
            Assert.That(controller.BuildableFacilityPlacements, Is.Empty);
        }
    }

    public sealed class LuoyangLowFrequencyDefenseProductionV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_LOW_FREQUENCY_DEFENSE_PRODUCTION_V1", "Screenshots");

        [UnityTest]
        public IEnumerator Actual28DefenseFacilities_RenderThreeReviewViewsAndCleanUp()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangLowFrequencyDefenseOverview);
            yield return null;

            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.LuoyangLowFrequencyDefensePreviewVisible,
                Is.True);
            Assert.That(controller.LowFrequencyDefenseProductionProfileCount,
                Is.EqualTo(5));
            Assert.That(controller.LowFrequencyDefenseFacilityPlanCount,
                Is.EqualTo(28));
            Assert.That(controller.BuildableFacilityPlacements.Count,
                Is.EqualTo(28));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                    item.CellId.Value).Distinct().Count(), Is.EqualTo(28));
            Assert.That(controller.BuildableFacilityPlacements.Count(item =>
                    item.ModelId == HanBuildableFacilityModelIds.CityGate),
                Is.EqualTo(16));
            Assert.That(controller.BuildableFacilityPlacements.Count(item =>
                    item.ModelId ==
                    LuoyangFacilityModelCoverageIds.PalaceGate),
                Is.EqualTo(2));
            Assert.That(controller.BuildableFacilityPlacements.Count(item =>
                    item.ModelId ==
                    LuoyangFacilityModelCoverageIds.FortifiedManor),
                Is.EqualTo(7));
            Assert.That(controller.BuildableFacilityPlacements.Count(item =>
                    item.ModelId ==
                    LuoyangFacilityModelCoverageIds.Beacon),
                Is.EqualTo(3));

            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances, Has.Length.EqualTo(28));
            Assert.That(instances.All(item =>
                item.LowFrequencyDefenseProductionReady), Is.True);
            Assert.That(instances.Count(item => item.GateIdentityReady),
                Is.EqualTo(14));
            Assert.That(instances.Count(item => item.LowFrequencyDefenseModeId ==
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .ProceduralModeId), Is.EqualTo(14));
            Assert.That(instances.Select(item => item.LowFrequencyDefenseProfileId)
                .Distinct().Count(), Is.EqualTo(5));
            Assert.That(instances.All(item =>
                item.GetComponent<LODGroup>()?.GetLODs().Length == 3), Is.True);
            Assert.That(instances.SelectMany(item =>
                item.GetComponentsInChildren<Collider>(true)), Is.Empty);
            Assert.That(controller.LuoyangLowFrequencyDefenseProductionStatus,
                Is.EqualTo(
                    "LUOYANG_LOW_FREQUENCY_DEFENSE_PRODUCTION_V1_READY_FOR_USER_REVIEW"));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_LOW_FREQUENCY_DEFENSE_PRODUCTION_V1_READY_FOR_USER_REVIEW"));

            Directory.CreateDirectory(EvidenceRoot);
            var overviewPath = Path.Combine(EvidenceRoot,
                "01_ALL_28_DEFENSE_FACILITIES_ACTUAL_CELLS.png");
            var manorPath = Path.Combine(EvidenceRoot,
                "02_SEVEN_MANORS_AND_FOUR_GENERIC_GATES.png");
            var beaconPath = Path.Combine(EvidenceRoot,
                "03_NORTHERN_BEACON_PAIR.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(overviewPath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangDefenseManorGateLine);
                yield return null;
                Assert.That(controller.LuoyangLowFrequencyDefensePreviewVisible,
                    Is.True);
                Assert.That(controller.BuildableFacilityPlacements.Count,
                    Is.EqualTo(28));
                controller.CaptureEvidence(manorPath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangDefenseNorthernBeaconPair);
                yield return null;
                Assert.That(controller.LuoyangLowFrequencyDefensePreviewVisible,
                    Is.True);
                Assert.That(controller.BuildableFacilityPlacements.Count,
                    Is.EqualTo(28));
                controller.CaptureEvidence(beaconPath, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            foreach (var path in new[] { overviewPath, manorPath, beaconPath })
            {
                Assert.That(File.Exists(path), Is.True, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000),
                    path);
            }

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangLowFrequencyDefensePreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
            Assert.That(controller.BuildableFacilityPlacements, Is.Empty);
        }
    }

    public sealed class LuoyangResourceAndAgricultureProductionV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_RESOURCE_AND_AGRICULTURE_PRODUCTION_V1", "Screenshots");

        [UnityTest]
        public IEnumerator Actual26Facilities_RenderFourReviewViewsAndCleanUp()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangResourceAgricultureOverview);
            yield return null;

            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.LuoyangResourceAgriculturePreviewVisible,
                Is.True);
            Assert.That(controller.ResourceAgricultureProductionProfileCount,
                Is.EqualTo(4));
            Assert.That(controller.ResourceAgricultureFacilityPlanCount,
                Is.EqualTo(26));
            Assert.That(controller.BuildableFacilityPlacements.Count,
                Is.EqualTo(26));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                    item.CellId.Value).Distinct().Count(), Is.EqualTo(26));
            Assert.That(controller.BuildableFacilityPlacements.Count(item =>
                    item.ModelId == LuoyangFacilityModelCoverageIds.Forestry),
                Is.EqualTo(9));
            Assert.That(controller.BuildableFacilityPlacements.Count(item =>
                    item.ModelId == LuoyangFacilityModelCoverageIds.MineQuarry),
                Is.EqualTo(11));
            Assert.That(controller.BuildableFacilityPlacements.Count(item =>
                    item.ModelId == LuoyangFacilityModelCoverageIds.RiceField),
                Is.EqualTo(6));

            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances, Has.Length.EqualTo(26));
            Assert.That(instances.All(item =>
                item.ResourceAgricultureProductionReady), Is.True);
            Assert.That(instances.Select(item =>
                item.ResourceAgricultureProfileId).Distinct().Count(),
                Is.EqualTo(4));
            Assert.That(instances.Select(item =>
                item.ResourceAgricultureAssetVariantId).Distinct().Count(),
                Is.EqualTo(4));
            Assert.That(instances.All(item => item.ResourceAgricultureEvidenceBasisId ==
                    LuoyangResourceAgricultureProductionKitIds.EvidenceBasisId),
                Is.True);
            Assert.That(instances.All(item =>
                item.GetComponent<LODGroup>()?.GetLODs().Length == 3), Is.True);
            Assert.That(instances.SelectMany(item =>
                item.GetComponentsInChildren<Collider>(true)), Is.Empty);
            Assert.That(controller.LuoyangResourceAgricultureProductionStatus,
                Is.EqualTo(
                    "LUOYANG_RESOURCE_AGRICULTURE_PRODUCTION_V1_READY_FOR_USER_REVIEW"));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_RESOURCE_AGRICULTURE_PRODUCTION_V1_READY_FOR_USER_REVIEW"));

            Directory.CreateDirectory(EvidenceRoot);
            var overviewPath = Path.Combine(EvidenceRoot,
                "01_ALL_26_RESOURCE_AGRICULTURE_ACTUAL_CELLS.png");
            var linePath = Path.Combine(EvidenceRoot,
                "02_FORESTRY_MINE_QUARRY_PRODUCTION_LINE.png");
            var quarryPath = Path.Combine(EvidenceRoot,
                "03_SOUTHERN_QUARRY_TERRACES.png");
            var ricePath = Path.Combine(EvidenceRoot,
                "04_SIX_BUNDED_RICE_FIELDS.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(overviewPath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangResourceExtractionLine);
                yield return null;
                Assert.That(controller.LuoyangResourceAgriculturePreviewVisible,
                    Is.True);
                controller.CaptureEvidence(linePath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangSouthernQuarryTerraces);
                yield return null;
                Assert.That(controller.LuoyangResourceAgriculturePreviewVisible,
                    Is.True);
                controller.CaptureEvidence(quarryPath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangRicePaddyBand);
                yield return null;
                Assert.That(controller.LuoyangResourceAgriculturePreviewVisible,
                    Is.True);
                controller.CaptureEvidence(ricePath, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            foreach (var path in new[]
                     { overviewPath, linePath, quarryPath, ricePath })
            {
                Assert.That(File.Exists(path), Is.True, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000),
                    path);
            }

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangResourceAgriculturePreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
            Assert.That(controller.BuildableFacilityPlacements, Is.Empty);
        }
    }

    public sealed class LuoyangFinalCivicRitualMedicalProductionClosureV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator Actual35Facilities_RenderFourReviewViewsAndCleanUp()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangFinalCivicOverview);
            yield return null;

            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.LuoyangFinalCivicPreviewVisible, Is.True);
            Assert.That(controller.FinalCivicProductionProfileCount,
                Is.EqualTo(12));
            Assert.That(controller.FinalCivicFacilityPlanCount, Is.EqualTo(35));
            Assert.That(controller.BuildableFacilityPlacements.Count,
                Is.EqualTo(35));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                    item.CellId.Value).Distinct().Count(), Is.EqualTo(35));

            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances, Has.Length.EqualTo(35));
            Assert.That(instances.All(item => item.FinalCivicProductionReady),
                Is.True);
            Assert.That(instances.Count(item => item.FinalCivicModeId ==
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .IdentityReuseModeId), Is.EqualTo(10));
            Assert.That(instances.Count(item => item.FinalCivicModeId ==
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .ProceduralModeId), Is.EqualTo(25));
            Assert.That(instances.Count(item => item.HistoricalLandmarkReady),
                Is.EqualTo(10));
            Assert.That(instances.Select(item => item.FinalCivicProfileId)
                .Distinct().Count(), Is.EqualTo(12));
            Assert.That(instances.All(item =>
                item.GetComponent<LODGroup>()?.GetLODs().Length == 3), Is.True);
            Assert.That(instances.SelectMany(item =>
                item.GetComponentsInChildren<Collider>(true)), Is.Empty);
            Assert.That(controller.LuoyangFinalCivicProductionStatus,
                Is.EqualTo(
                    "LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1_READY_FOR_USER_REVIEW"));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1_READY_FOR_USER_REVIEW"));

            Directory.CreateDirectory(EvidenceRoot);
            var overviewPath = Path.Combine(EvidenceRoot,
                "luoyang_final_civic_overview_v1.png");
            var clinicPath = Path.Combine(EvidenceRoot,
                "luoyang_clinic_line_v1.png");
            var ritualPath = Path.Combine(EvidenceRoot,
                "luoyang_ritual_hall_line_v1.png");
            var publicPath = Path.Combine(EvidenceRoot,
                "luoyang_public_courtyard_plaza_office_v1.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(overviewPath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangClinicLine);
                yield return null;
                Assert.That(controller.LuoyangFinalCivicPreviewVisible, Is.True);
                controller.CaptureEvidence(clinicPath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangRitualHallLine);
                yield return null;
                Assert.That(controller.LuoyangFinalCivicPreviewVisible, Is.True);
                controller.CaptureEvidence(ritualPath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangPublicCivicCluster);
                yield return null;
                Assert.That(controller.LuoyangFinalCivicPreviewVisible, Is.True);
                controller.CaptureEvidence(publicPath, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            foreach (var path in new[]
                     { overviewPath, clinicPath, ritualPath, publicPath })
            {
                Assert.That(File.Exists(path), Is.True, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000),
                    path);
            }

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangFinalCivicPreviewVisible, Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
            Assert.That(controller.BuildableFacilityPlacements, Is.Empty);
        }
    }

    public sealed class LuoyangWholeCityFinalAssetReviewV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator FiftyFourAssetSlots_RenderPriorityBoardsAndCleanUp()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangFinalAssetReviewAll);
            yield return null;

            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.LuoyangFinalAssetReviewPreviewVisible,
                Is.True);
            Assert.That(controller.FinalAssetReviewItemCount, Is.EqualTo(54));
            Assert.That(controller.FinalAssetReviewCoveredFacilityCount,
                Is.EqualTo(2084));
            Assert.That(controller.BuildableFacilityPlacements,
                Has.Count.EqualTo(54));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                item.CellId.Value).Distinct().Count(), Is.EqualTo(54));

            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances, Has.Length.EqualTo(54));
            Assert.That(instances.All(item => item.FinalAssetReviewReady),
                Is.True);
            Assert.That(instances.All(item => item.AssetId ==
                item.FinalAssetReviewReplacementSlotId), Is.True);
            Assert.That(instances.Sum(item =>
                item.FinalAssetReviewFacilityUsageCount), Is.EqualTo(2084));
            Assert.That(instances.Count(item => item.FinalAssetReviewPriorityId ==
                    LuoyangFinalAssetReviewIds.PriorityP0), Is.EqualTo(24));
            Assert.That(instances.Count(item => item.FinalAssetReviewPriorityId ==
                    LuoyangFinalAssetReviewIds.PriorityP1), Is.EqualTo(10));
            Assert.That(instances.Count(item => item.FinalAssetReviewPriorityId ==
                    LuoyangFinalAssetReviewIds.PriorityP2), Is.EqualTo(14));
            Assert.That(instances.Count(item => item.FinalAssetReviewPriorityId ==
                    LuoyangFinalAssetReviewIds.PriorityP3), Is.EqualTo(6));
            Assert.That(instances.All(item =>
                item.GetComponent<LODGroup>()?.GetLODs().Length == 3), Is.True);
            Assert.That(instances.SelectMany(item =>
                item.GetComponentsInChildren<Collider>(true)), Is.Empty);
            Assert.That(controller.LuoyangFinalAssetReviewStatus, Is.EqualTo(
                "LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1_READY_FOR_USER_REVIEW"));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1_READY_FOR_USER_REVIEW"));

            Directory.CreateDirectory(EvidenceRoot);
            var allPath = Path.Combine(EvidenceRoot,
                "luoyang_final_asset_review_all_54_v1.png");
            var p0Path = Path.Combine(EvidenceRoot,
                "luoyang_final_asset_review_p0_identity_24_v1.png");
            var p1Path = Path.Combine(EvidenceRoot,
                "luoyang_final_asset_review_p1_high_exposure_10_v1.png");
            var supportPath = Path.Combine(EvidenceRoot,
                "luoyang_final_asset_review_p2_p3_support_20_v1.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(allPath, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangFinalAssetReviewP0);
                yield return null;
                Assert.That(controller.LuoyangFinalAssetReviewPreviewVisible,
                    Is.True);
                controller.CaptureEvidence(p0Path, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangFinalAssetReviewP1);
                yield return null;
                controller.CaptureEvidence(p1Path, 1600, 1000);
                yield return null;
                controller.ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangFinalAssetReviewP2P3);
                yield return null;
                controller.CaptureEvidence(supportPath, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            foreach (var path in new[] { allPath, p0Path, p1Path, supportPath })
            {
                Assert.That(File.Exists(path), Is.True, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000),
                    path);
            }

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangFinalAssetReviewPreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
            Assert.That(controller.BuildableFacilityPlacements, Is.Empty);
        }
    }
}
