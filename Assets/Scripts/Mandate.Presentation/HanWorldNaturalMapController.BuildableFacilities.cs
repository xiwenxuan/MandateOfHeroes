using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using UnityEngine;

namespace Mandate.Presentation
{
    public sealed partial class HanWorldNaturalMapController
    {
        private readonly List<HanBuildableFacilityModelPlacement>
            _buildableFacilityPlacements =
                new List<HanBuildableFacilityModelPlacement>();
        private GameObject _buildableFacilityRoot;
        private HanBuildableFacilityModelCatalog _buildableFacilityCatalog;
        private LuoyangProductionBuildingKitCatalog _productionBuildingKitCatalog;
        private LuoyangHistoricalLandmarkKitCatalog _historicalLandmarkKitCatalog;
        private LuoyangGateIdentityKitCatalog _gateIdentityKitCatalog;
        private LuoyangMediumFrequencyUrbanFabricKitCatalog
            _mediumFrequencyUrbanFabricKitCatalog;
        private LuoyangInfrastructureProductionKitCatalog
            _infrastructureProductionKitCatalog;
        private LuoyangInfrastructureProductionPlan
            _luoyangInfrastructureProductionPlan;
        private LuoyangLowFrequencyDefenseProductionKitCatalog
            _lowFrequencyDefenseProductionKitCatalog;
        private LuoyangLowFrequencyDefenseProductionPlan
            _luoyangLowFrequencyDefenseProductionPlan;
        private LuoyangResourceAgricultureProductionKitCatalog
            _resourceAgricultureProductionKitCatalog;
        private LuoyangResourceAgricultureProductionPlan
            _luoyangResourceAgricultureProductionPlan;
        private LuoyangFinalCivicRitualMedicalProductionKitCatalog
            _finalCivicProductionKitCatalog;
        private LuoyangFinalCivicRitualMedicalProductionPlan
            _luoyangFinalCivicProductionPlan;
        private LuoyangFinalAssetReviewCatalog _finalAssetReviewCatalog;
        private LuoyangFinalAssetReviewPlan _luoyangFinalAssetReviewPlan;
        private LuoyangP0FinalAssetVerticalSliceCatalog
            _p0FinalAssetVerticalSliceCatalog;
        private LuoyangP0FinalAssetVerticalSlicePlan
            _luoyangP0FinalAssetVerticalSlicePlan;
        private LuoyangP0LandmarkSecondBatchCatalog
            _p0LandmarkSecondBatchCatalog;
        private LuoyangP0LandmarkSecondBatchPlan
            _luoyangP0LandmarkSecondBatchPlan;
        private LuoyangP0LandmarkThirdBatchCatalog
            _p0LandmarkThirdBatchCatalog;
        private LuoyangP0LandmarkThirdBatchPlan
            _luoyangP0LandmarkThirdBatchPlan;
        private LuoyangP0NamedGateFourthBatchCatalog
            _p0NamedGateFourthBatchCatalog;
        private LuoyangP0NamedGateFourthBatchPlan
            _luoyangP0NamedGateFourthBatchPlan;
        private LuoyangRemainingFinalAssetCatalog
            _remainingFinalAssetCatalog;
        private LuoyangRemainingFinalAssetPlan
            _luoyangRemainingFinalAssetPlan;
        private readonly Dictionary<string, LuoyangFinalAssetReviewItem>
            _finalAssetReviewItemsByAssetId =
                new Dictionary<string, LuoyangFinalAssetReviewItem>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangInfrastructureFacility>
            _luoyangInfrastructureFacilitiesById =
                new Dictionary<string, LuoyangInfrastructureFacility>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangLowFrequencyDefenseFacility>
            _luoyangLowFrequencyDefenseFacilitiesById =
                new Dictionary<string, LuoyangLowFrequencyDefenseFacility>(
                    StringComparer.Ordinal);
        private LuoyangBuildingPerformancePlan _luoyangBuildingPerformancePlan;
        private LuoyangWholeCityCompositionPlan
            _luoyangWholeCityCompositionPlan;
        private LuoyangFacilityInteractionNavigationPlan
            _luoyangFacilityInteractionNavigationPlan;
        private LuoyangRoadTraversalRefinementPlan
            _luoyangRoadTraversalRefinementPlan;
        private LuoyangHumanScaleLocalMapPlan
            _luoyangHumanScaleLocalMapPlan;
        private LuoyangPassageTraversalSession
            _luoyangPassageTraversalSession;
        private WorldState _luoyangPassageWorld;
        private WorldCommandRuntime _luoyangPassageWorldCommandRuntime;
        private LuoyangPassageWorldCommandSystem
            _luoyangPassageWorldCommandSystem;
        private LuoyangFormalPlayerMovementSystem
            _luoyangFormalPlayerMovementSystem;
        private LuoyangFormalPlayerMovementService
            _luoyangFormalPlayerMovementService;
        private WorldSimulator _luoyangFormalMovementSimulator;
        private LuoyangFacilityInteractionNavigationRuntime
            _luoyangFacilityInteractionNavigationRuntime;
        private LuoyangHumanScaleStreamingRuntime
            _luoyangHumanScaleStreamingRuntime;
        private GlobalProjectedCoordinate _luoyangHumanScaleFloatingOrigin;
        private bool _hasLuoyangHumanScaleFloatingOrigin;
        private string _selectedLuoyangFacilityId;
        private LuoyangBuildingPerformanceBatchRenderer
            _luoyangBuildingPerformanceBatchRenderer;
        private LuoyangBuildingBatchMetrics _luoyangBuildingBatchMetrics;
        private HanBuildableFacilityModelFactory _buildableFacilityFactory;
        private bool _buildableFacilityPreviewVisible;
        private bool _luoyangFacilityCoveragePreviewVisible;
        private bool _luoyangHistoricalLandmarkPreviewVisible;
        private bool _luoyangGateIdentityPreviewVisible;
        private bool _luoyangMediumFrequencyUrbanFabricPreviewVisible;
        private bool _luoyangBuildingPerformancePreviewVisible;
        private bool _luoyangInfrastructurePreviewVisible;
        private bool _luoyangLowFrequencyDefensePreviewVisible;
        private bool _luoyangResourceAgriculturePreviewVisible;
        private bool _luoyangFinalCivicPreviewVisible;
        private bool _luoyangFinalAssetReviewPreviewVisible;
        private bool _luoyangP0FinalAssetVerticalSlicePreviewVisible;
        private bool _luoyangP0LandmarkSecondBatchPreviewVisible;
        private bool _luoyangP0LandmarkThirdBatchPreviewVisible;
        private bool _luoyangP0NamedGateFourthBatchPreviewVisible;

        public bool BuildableFacilityPreviewVisible =>
            _buildableFacilityPreviewVisible;
        public bool LuoyangFacilityCoveragePreviewVisible =>
            _luoyangFacilityCoveragePreviewVisible;
        public bool LuoyangHistoricalLandmarkPreviewVisible =>
            _luoyangHistoricalLandmarkPreviewVisible;
        public bool LuoyangGateIdentityPreviewVisible =>
            _luoyangGateIdentityPreviewVisible;
        public bool LuoyangMediumFrequencyUrbanFabricPreviewVisible =>
            _luoyangMediumFrequencyUrbanFabricPreviewVisible;
        public bool LuoyangBuildingPerformancePreviewVisible =>
            _luoyangBuildingPerformancePreviewVisible;
        public bool LuoyangInfrastructurePreviewVisible =>
            _luoyangInfrastructurePreviewVisible;
        public bool LuoyangLowFrequencyDefensePreviewVisible =>
            _luoyangLowFrequencyDefensePreviewVisible;
        public bool LuoyangResourceAgriculturePreviewVisible =>
            _luoyangResourceAgriculturePreviewVisible;
        public bool LuoyangFinalCivicPreviewVisible =>
            _luoyangFinalCivicPreviewVisible;
        public bool LuoyangFinalAssetReviewPreviewVisible =>
            _luoyangFinalAssetReviewPreviewVisible;
        public bool LuoyangP0FinalAssetVerticalSlicePreviewVisible =>
            _luoyangP0FinalAssetVerticalSlicePreviewVisible;
        public bool LuoyangP0LandmarkSecondBatchPreviewVisible =>
            _luoyangP0LandmarkSecondBatchPreviewVisible;
        public bool LuoyangP0LandmarkThirdBatchPreviewVisible =>
            _luoyangP0LandmarkThirdBatchPreviewVisible;
        public bool LuoyangP0NamedGateFourthBatchPreviewVisible =>
            _luoyangP0NamedGateFourthBatchPreviewVisible;
        public int RuntimeBuildableFacilityModelCount =>
            _buildableFacilityRoot == null
                ? 0 : _buildableFacilityRoot.transform.childCount;
        public int RuntimeBuildableFacilityRendererCount =>
            _buildableFacilityRoot == null
                ? 0 : _buildableFacilityRoot.GetComponentsInChildren<Renderer>().Length;
        public int BuildableFacilityMaterialCount =>
            _buildableFacilityFactory?.MaterialCount ?? 0;
        public int ProductionBuildableFacilityProfileCount =>
            _buildableFacilityFactory?.ProductionProfileCount ?? 0;
        public int ProductionBuildableFacilityMeshCount =>
            _buildableFacilityFactory?.ProductionMeshCount ?? 0;
        public int HistoricalLandmarkProfileCount =>
            _buildableFacilityFactory?.HistoricalLandmarkProfileCount ?? 0;
        public int GateIdentityProfileCount =>
            _buildableFacilityFactory?.GateIdentityProfileCount ?? 0;
        public int MediumFrequencyUrbanFabricProfileCount =>
            _buildableFacilityFactory?.MediumFrequencyUrbanFabricProfileCount ?? 0;
        public int InfrastructureProductionProfileCount =>
            _buildableFacilityFactory?.InfrastructureProductionProfileCount ?? 0;
        public int InfrastructureFacilityPlanCount =>
            _luoyangInfrastructureProductionPlan?.Facilities.Count ?? 0;
        public int LowFrequencyDefenseProductionProfileCount =>
            _buildableFacilityFactory?
                .LowFrequencyDefenseProductionProfileCount ?? 0;
        public int LowFrequencyDefenseFacilityPlanCount =>
            _luoyangLowFrequencyDefenseProductionPlan?.Facilities.Count ?? 0;
        public int ResourceAgricultureProductionProfileCount =>
            _buildableFacilityFactory?
                .ResourceAgricultureProductionProfileCount ?? 0;
        public int ResourceAgricultureFacilityPlanCount =>
            _luoyangResourceAgricultureProductionPlan?.Facilities.Count ?? 0;
        public int FinalCivicProductionProfileCount =>
            _buildableFacilityFactory?.FinalCivicProductionProfileCount ?? 0;
        public int FinalCivicFacilityPlanCount =>
            _luoyangFinalCivicProductionPlan?.Facilities.Count ?? 0;
        public int FinalAssetReviewItemCount =>
            _finalAssetReviewCatalog?.Items.Count ?? 0;
        public int FinalAssetReviewCoveredFacilityCount =>
            _luoyangFinalAssetReviewPlan?.FacilityAssetVariants.Count ?? 0;
        public int P0FinalAssetVerticalSliceProfileCount =>
            _buildableFacilityFactory?.P0FinalAssetVerticalSliceProfileCount ?? 0;
        public int P0LandmarkSecondBatchProfileCount =>
            _buildableFacilityFactory?.P0LandmarkSecondBatchProfileCount ?? 0;
        public int P0LandmarkThirdBatchProfileCount =>
            _buildableFacilityFactory?.P0LandmarkThirdBatchProfileCount ?? 0;
        public int P0NamedGateFourthBatchProfileCount =>
            _buildableFacilityFactory?.P0NamedGateFourthBatchProfileCount ?? 0;
        public int RemainingFinalAssetProfileCount =>
            _buildableFacilityFactory?.RemainingFinalAssetProfileCount ?? 0;
        public int WholeCityBuildingFacilityPlanCount =>
            _luoyangBuildingPerformancePlan?.Facilities.Count ?? 0;
        public int WholeCityBuildingSpatialBatchCount =>
            _luoyangBuildingPerformancePlan?.SpatialBatches.Count ?? 0;
        public int WholeCityCompositionFacilityAnchorCount =>
            _luoyangWholeCityCompositionPlan?.Anchors.Count ?? 0;
        public int WholeCityCompositionDistrictCount =>
            _luoyangWholeCityCompositionPlan?.FacilityCountByDistrict.Count ?? 0;
        public int WholeCityCompositionAssetVariantCount =>
            _luoyangWholeCityCompositionPlan?.Anchors
                .Select(item => item.AssetVariantId).Distinct(
                    StringComparer.Ordinal).Count() ?? 0;
        public int WholeCityCompositionDenseResidentAnchorCount =>
            _luoyangWholeCityCompositionPlan == null ? 0 :
                LuoyangWholeCityCompositionRules.SelectDensestResidentAnchors(
                    _luoyangWholeCityCompositionPlan,
                    _luoyangBuildingPerformancePlan).Count;
        public bool WholeCityCompositionCreatesSimulationSubCells =>
            _luoyangWholeCityCompositionPlan?.CreatesSimulationSubCells ?? false;
        public int LuoyangFacilitySelectionProxyPlanCount =>
            _luoyangFacilityInteractionNavigationPlan?.SelectionProxies.Count ?? 0;
        public int RuntimeLuoyangFacilitySelectionProxyCount =>
            _luoyangFacilityInteractionNavigationRuntime?.ResidentProxyCount ?? 0;
        public int LuoyangRoadNavigationNodeCount =>
            _luoyangFacilityInteractionNavigationPlan?.NavigationNodes.Count ?? 0;
        public int LuoyangRoadNavigationEdgeCount =>
            _luoyangFacilityInteractionNavigationPlan?.NavigationEdges.Count ?? 0;
        public int LuoyangRefinedRoadNavigationEdgeCount =>
            _luoyangRoadTraversalRefinementPlan?.NavigationEdges.Count ?? 0;
        public int LuoyangHumanScaleLocalSpaceCount =>
            _luoyangHumanScaleLocalMapPlan?.LocalSpaces.Count ?? 0;
        public int LuoyangHumanScaleNavigationNodeCount =>
            _luoyangHumanScaleLocalMapPlan?.Nodes.Count ?? 0;
        public int LuoyangHumanScaleNavigationEdgeCount =>
            _luoyangHumanScaleLocalMapPlan?.Edges.Count ?? 0;
        public int LuoyangHumanScaleTransitionCount =>
            _luoyangHumanScaleLocalMapPlan?.Transitions.Count ?? 0;
        public string LuoyangHumanScaleLocalMapStatus =>
            _luoyangHumanScaleLocalMapPlan == null
                ? "NOT_READY"
                : _luoyangHumanScaleLocalMapPlan.StatusId;
        public int LuoyangModeledRoadConnectorCount =>
            _luoyangRoadTraversalRefinementPlan?.ModeledConnectors.Count ?? 0;
        public int LuoyangPassageTraversalCount =>
            _luoyangPassageTraversalSession?.Records.Count ?? 0;
        public bool LuoyangPassageWorldBound =>
            _luoyangPassageWorld != null;
        public int PersistedLuoyangPassageTraversalCount =>
            _luoyangPassageWorld?.LuoyangPassageTraversals.Count ?? 0;
        public int RuntimeLuoyangRoadNavigationEdgeCount =>
            _luoyangFacilityInteractionNavigationRuntime?
                .ResidentNavigationEdgeCount ?? 0;
        public int RuntimeLuoyangModeledRoadConnectorEdgeCount =>
            _luoyangFacilityInteractionNavigationRuntime?
                .ResidentModeledConnectorEdgeCount ?? 0;
        public int RuntimeLuoyangPassageStateMarkerCount =>
            _luoyangFacilityInteractionNavigationRuntime?
                .ResidentPassageMarkerCount ?? 0;
        public int RuntimeLuoyangPassagePresentationCount =>
            _luoyangFacilityInteractionNavigationRuntime?
                .ResidentPassagePresentationCount ?? 0;
        public int RuntimeLuoyangActivePedestrianBlockerCount =>
            _luoyangFacilityInteractionNavigationRuntime?
                .ActivePedestrianBlockerCount ?? 0;
        public int RuntimeLuoyangDamagedPassagePresentationCount =>
            _luoyangFacilityInteractionNavigationRuntime?
                .DamagedPassagePresentationCount ?? 0;
        public int RuntimeLuoyangDestroyedPassagePresentationCount =>
            _luoyangFacilityInteractionNavigationRuntime?
                .DestroyedPassagePresentationCount ?? 0;
        public int RuntimeLuoyangActiveRepairScaffoldCount =>
            _luoyangFacilityInteractionNavigationRuntime?
                .ActiveRepairScaffoldCount ?? 0;
        public int RuntimeLuoyangHumanScaleResidentCellCount =>
            _luoyangHumanScaleStreamingRuntime?.ResidentCellCount ?? 0;
        public int RuntimeLuoyangHumanScaleGameObjectCount =>
            _luoyangHumanScaleStreamingRuntime?.ResidentGameObjectCount ?? 0;
        public int RuntimeLuoyangHumanScaleMeshCount =>
            _luoyangHumanScaleStreamingRuntime?.ResidentMeshCount ?? 0;
        public int RuntimeLuoyangHumanScaleColliderCount =>
            _luoyangHumanScaleStreamingRuntime?.ResidentColliderCount ?? 0;
        public long RuntimeLuoyangHumanScaleLoadMilliseconds =>
            _luoyangHumanScaleStreamingRuntime?.LastLoadMilliseconds ?? 0L;
        public long RuntimeLuoyangHumanScaleUnloadMilliseconds =>
            _luoyangHumanScaleStreamingRuntime?.LastUnloadMilliseconds ?? 0L;
        public int LuoyangRoadComponentCountBeforeConnectors =>
            _luoyangFacilityInteractionNavigationPlan?
                .RoadComponentCountBeforeConnectors ?? 0;
        public string SelectedLuoyangFacilityId => _selectedLuoyangFacilityId;
        public string LuoyangFacilityInteractionNavigationStatus =>
            _luoyangFacilityInteractionNavigationPlan == null
                ? "NOT_READY"
                : _luoyangFacilityInteractionNavigationPlan.StatusId;
        public string LuoyangRoadConnectorPassageTraversalStatus =>
            _luoyangRoadTraversalRefinementPlan == null
                ? "NOT_READY"
                : _luoyangRoadTraversalRefinementPlan.StatusId;
        public string LuoyangPassagePedestrianPresentationStatus =>
            _luoyangRoadTraversalRefinementPlan == null
                ? "NOT_READY"
                : LuoyangPassagePedestrianPresentationIds.StatusId;
        public string LuoyangClickToWalkPedestrianStatus =>
            _luoyangRoadTraversalRefinementPlan == null
                ? "NOT_READY"
                : LuoyangClickToWalkPedestrianIds.StatusId;
        public string LuoyangPedestrianActorId =>
            _luoyangFacilityInteractionNavigationRuntime?.PedestrianActorId;
        public string LuoyangPedestrianCurrentFacilityId =>
            _luoyangFacilityInteractionNavigationRuntime?
                .PedestrianCurrentFacilityId;
        public string LuoyangPedestrianTargetFacilityId =>
            _luoyangFacilityInteractionNavigationRuntime?
                .PedestrianTargetFacilityId;
        public string LuoyangPedestrianMovementStateId =>
            _luoyangFacilityInteractionNavigationRuntime?
                .PedestrianMovementStateId;
        public string LuoyangPedestrianLastStopReasonId =>
            _luoyangFacilityInteractionNavigationRuntime?
                .PedestrianLastStopReasonId;
        public bool LuoyangPedestrianIsWalking =>
            _luoyangFacilityInteractionNavigationRuntime?
                .PedestrianIsWalking ?? false;
        public int LuoyangPedestrianRouteNodeCount =>
            _luoyangFacilityInteractionNavigationRuntime?
                .PedestrianRouteNodeCount ?? 0;
        public float LuoyangPedestrianRouteDistanceMetres =>
            _luoyangFacilityInteractionNavigationRuntime?
                .PedestrianRouteDistanceMetres ?? 0f;
        public float LuoyangPedestrianEstimatedDurationSeconds =>
            _luoyangFacilityInteractionNavigationRuntime?
                .PedestrianEstimatedDurationSeconds ?? 0f;
        public IReadOnlyList<string> LuoyangPedestrianRouteFacilityIds =>
            _luoyangFacilityInteractionNavigationRuntime?
                .PedestrianRouteFacilityIds ?? Array.Empty<string>();
        public LuoyangBuildingBatchMetrics LuoyangBuildingBatchMetrics =>
            _luoyangBuildingBatchMetrics;
        public string ProductionBuildingKitStatus =>
            _productionBuildingKitCatalog == null
                ? "NOT_READY"
                : "LUOYANG_PRODUCTION_BUILDING_KIT_V1_READY_FOR_USER_REVIEW";
        public string BuildableFacilityModelCatalogSchemaId =>
            _buildableFacilityCatalog?.SchemaId;
        public string HistoricalLandmarkKitStatus =>
            _historicalLandmarkKitCatalog == null
                ? "NOT_READY"
                : "LUOYANG_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1_READY_FOR_USER_REVIEW";
        public string GateIdentityKitStatus => _gateIdentityKitCatalog == null
            ? "NOT_READY"
            : "LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1_READY_FOR_USER_REVIEW";
        public string MediumFrequencyUrbanFabricKitStatus =>
            _mediumFrequencyUrbanFabricKitCatalog == null
                ? "NOT_READY"
                : "LUOYANG_MEDIUM_FREQUENCY_URBAN_FABRIC_V1_READY_FOR_USER_REVIEW";
        public string LuoyangBuildingPerformanceStatus =>
            _luoyangBuildingPerformancePlan == null
                ? "NOT_READY"
                : "LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1_READY_FOR_USER_REVIEW";
        public string LuoyangWholeCityCompositionStatus =>
            _luoyangWholeCityCompositionPlan == null
                ? "NOT_READY"
                : _luoyangWholeCityCompositionPlan.StatusId;
        public string LuoyangInfrastructureProductionStatus =>
            _luoyangInfrastructureProductionPlan == null
                ? "NOT_READY"
                : "LUOYANG_CANAL_WELL_BRIDGE_INFRASTRUCTURE_PRODUCTION_V1_READY_FOR_USER_REVIEW";
        public string LuoyangLowFrequencyDefenseProductionStatus =>
            _luoyangLowFrequencyDefenseProductionPlan == null
                ? "NOT_READY"
                : "LUOYANG_LOW_FREQUENCY_DEFENSE_PRODUCTION_V1_READY_FOR_USER_REVIEW";
        public string LuoyangResourceAgricultureProductionStatus =>
            _luoyangResourceAgricultureProductionPlan == null
                ? "NOT_READY"
                : "LUOYANG_RESOURCE_AGRICULTURE_PRODUCTION_V1_READY_FOR_USER_REVIEW";
        public string LuoyangFinalCivicProductionStatus =>
            _luoyangFinalCivicProductionPlan == null
                ? "NOT_READY"
                : "LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1_READY_FOR_USER_REVIEW";
        public string LuoyangFinalAssetReviewStatus =>
            _luoyangFinalAssetReviewPlan == null
                ? "NOT_READY"
                : "LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1_READY_FOR_USER_REVIEW";
        public string LuoyangP0FinalAssetVerticalSliceStatus =>
            _luoyangP0FinalAssetVerticalSlicePlan == null
                ? "NOT_READY"
                : "LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1";
        public string LuoyangP0LandmarkSecondBatchStatus =>
            _luoyangP0LandmarkSecondBatchPlan == null
                ? "NOT_READY"
                : LuoyangP0LandmarkSecondBatchIds.StatusId;
        public string LuoyangP0LandmarkThirdBatchStatus =>
            _luoyangP0LandmarkThirdBatchPlan == null
                ? "NOT_READY"
                : LuoyangP0LandmarkThirdBatchIds.StatusId;
        public string LuoyangP0NamedGateFourthBatchStatus =>
            _luoyangP0NamedGateFourthBatchPlan == null
                ? "NOT_READY"
                : LuoyangP0NamedGateFourthBatchIds.StatusId;
        public string LuoyangRemainingFinalAssetStatus =>
            _luoyangRemainingFinalAssetPlan == null
                ? "NOT_READY"
                : LuoyangRemainingFinalAssetIds.StatusId;
        public IReadOnlyList<HanBuildableFacilityModelPlacement>
            BuildableFacilityPlacements => _buildableFacilityPlacements;

        public void SetBuildableFacilityPreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the building preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                _luoyangP0LandmarkSecondBatchPreviewVisible = false;
                _luoyangP0LandmarkThirdBatchPreviewVisible = false;
                _luoyangP0NamedGateFourthBatchPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(StrategicCellCameraRig.BuildableFacilityReview);
        }

        public void SetLuoyangFacilityCoveragePreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang model coverage preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangFacilityCoverageReview);
        }

        public void SetLuoyangHistoricalLandmarkPreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang landmark preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangHistoricalLandmarkReview);
        }

        public void SetLuoyangGateIdentityPreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang gate preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(StrategicCellCameraRig.LuoyangGateIdentityReview);
        }

        public void SetLuoyangMediumFrequencyUrbanFabricPreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang urban-fabric preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangMediumFrequencyUrbanFabricReview);
        }

        public void SetLuoyangBuildingPerformancePreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang building performance preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangBuildingPerformanceReview);
        }

        public void SetLuoyangInfrastructurePreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang infrastructure preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangInfrastructureOverview);
        }

        public void SetLuoyangLowFrequencyDefensePreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang defense preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangLowFrequencyDefenseOverview);
        }

        public void SetLuoyangResourceAgriculturePreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang resource/agriculture preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangResourceAgricultureOverview);
        }

        public void SetLuoyangFinalCivicPreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang final civic preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangFinalCivicOverview);
        }

        public void SetLuoyangFinalAssetReviewPreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang final-asset review preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangFacilityCoveragePreviewVisible = false;
                _luoyangHistoricalLandmarkPreviewVisible = false;
                _luoyangGateIdentityPreviewVisible = false;
                _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
                _luoyangBuildingPerformancePreviewVisible = false;
                _luoyangInfrastructurePreviewVisible = false;
                _luoyangLowFrequencyDefensePreviewVisible = false;
                _luoyangResourceAgriculturePreviewVisible = false;
                _luoyangFinalCivicPreviewVisible = false;
                _luoyangFinalAssetReviewPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangFinalAssetReviewAll);
        }

        public void SetLuoyangP0FinalAssetVerticalSlicePreviewVisible(
            bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang P0 final-asset preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangP0FinalAssetVerticalSlicePreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangP0FinalAssetVerticalSliceOverview);
        }

        public void SetLuoyangP0LandmarkSecondBatchPreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang P0 landmark second-batch preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangP0LandmarkSecondBatchPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangP0LandmarkSecondBatchOverview);
        }

        public void SetLuoyangP0LandmarkThirdBatchPreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang P0 landmark third-batch preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangP0LandmarkThirdBatchPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangP0LandmarkThirdBatchOverview);
        }

        public void SetLuoyangP0NamedGateFourthBatchPreviewVisible(bool visible)
        {
            if (!IsReady)
                throw new InvalidOperationException(
                    "Natural map must be initialized before changing the Luoyang P0 named-gate fourth-batch preview.");
            if (!visible)
            {
                _buildableFacilityPreviewVisible = false;
                _luoyangP0NamedGateFourthBatchPreviewVisible = false;
                RefreshBuildableFacilityPreview();
                return;
            }
            ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangP0NamedGateFourthBatchOverview);
        }

        public HanBuildableFacilityModelInstance PlaceBuildableFacilityModel(
            HanBuildableFacilityModelPlacement placement, bool previewOnly = false)
        {
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            if (_buildableFacilityFactory == null || _buildableFacilityRoot == null)
                throw new InvalidOperationException(
                    "Han buildable Facility model kit is not initialized.");
            if (View != HanNaturalMapView.Region)
                throw new InvalidOperationException(
                    "Buildable Facility models can only be placed in Region view.");

            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            if (!grid.TryDecode(placement.CellId, out var row, out var column))
                throw new ArgumentOutOfRangeException(nameof(placement));
            foreach (var current in _buildableFacilityPlacements)
                if (current.CellId == placement.CellId)
                    throw new InvalidOperationException(
                        "A buildable Facility model already occupies this Global Cell.");

            var sample = _source.ReadSample(row, column).Cell;
            var instance = _buildableFacilityFactory.Create(placement.ModelId,
                _buildableFacilityRoot.transform, placement.RuntimeBindingId,
                placement.CellId.Value, previewOnly);
            if (_finalAssetReviewItemsByAssetId.TryGetValue(instance.AssetId,
                    out var finalAssetReview))
            {
                instance.FinalAssetReviewReady = true;
                instance.FinalAssetReviewItemId = finalAssetReview.ItemId;
                instance.FinalAssetReviewAuditGroupId =
                    finalAssetReview.AuditGroupId;
                instance.FinalAssetReviewPriorityId =
                    finalAssetReview.PriorityId;
                instance.FinalAssetReviewReplacementSlotId =
                    finalAssetReview.ReplacementSlotId;
                instance.FinalAssetReviewFacilityUsageCount =
                    finalAssetReview.FacilityUsageCount;
            }
            if (instance.InfrastructureProductionReady &&
                _luoyangInfrastructureFacilitiesById.TryGetValue(
                    placement.RuntimeBindingId, out var infrastructure))
            {
                instance.InfrastructureTopologyId = infrastructure.TopologyId;
                instance.InfrastructureConnectionMask =
                    infrastructure.ConnectionMask;
            }
            if (instance.LowFrequencyDefenseProductionReady &&
                _luoyangLowFrequencyDefenseFacilitiesById.TryGetValue(
                    placement.RuntimeBindingId, out var defense))
            {
                instance.VisualFacing = defense.VisualFacing;
                instance.DirectionBasisId = defense.DirectionBasisId;
            }
            instance.transform.localPosition = new Vector3(
                (float)((sample.CenterX - _floatingOrigin.EastingMetres) /
                        HorizontalMetresPerUnit),
                GetPresentationHeightForGlobal(sample.CenterX, sample.CenterY) + 0.02f,
                (float)((sample.CenterY - _floatingOrigin.NorthingMetres) /
                        HorizontalMetresPerUnit));
            instance.transform.localRotation = Quaternion.Euler(0f,
                placement.RotationDegrees, 0f);
            if (previewOnly && instance.GateIdentityReady)
                instance.transform.localScale = Vector3.one * 1.65f;
            if (previewOnly && instance.MediumFrequencyUrbanFabricReady)
                instance.transform.localScale = Vector3.one * 1.15f;
            if (previewOnly && instance.InfrastructureProductionReady)
                instance.transform.localScale = Vector3.one * 1.28f;
            if (previewOnly && instance.LowFrequencyDefenseProductionReady &&
                !instance.GateIdentityReady)
                instance.transform.localScale = Vector3.one * 1.42f;
            if (previewOnly && instance.ResourceAgricultureProductionReady)
                instance.transform.localScale = Vector3.one * 1.34f;
            if (previewOnly && instance.FinalCivicProductionReady &&
                !instance.HistoricalLandmarkReady)
                instance.transform.localScale = Vector3.one * 1.34f;
            _buildableFacilityPlacements.Add(placement);
            return instance;
        }

        private void InitializeBuildableFacilityModelKit()
        {
            _buildableFacilityRoot = NewRoot(
                "Han Buildable Facility Model Kit V1");
            var source = new LuoyangFacilityModelCoverageSource(Path.Combine(
                Application.streamingAssetsPath, "WorldMap"));
            _buildableFacilityCatalog = source.CombinedCatalog;
            var production = new LuoyangProductionBuildingKitSource(Path.Combine(
                Application.streamingAssetsPath, "WorldMap"),
                _buildableFacilityCatalog);
            _productionBuildingKitCatalog = production.Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(Path.Combine(
                Application.streamingAssetsPath, "WorldMap"),
                _buildableFacilityCatalog);
            _historicalLandmarkKitCatalog = landmarks.Catalog;
            var gates = new LuoyangGateIdentityKitSource(Path.Combine(
                    Application.streamingAssetsPath, "WorldMap"),
                _buildableFacilityCatalog);
            _gateIdentityKitCatalog = gates.Catalog;
            var urbanFabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                _buildableFacilityCatalog);
            _mediumFrequencyUrbanFabricKitCatalog = urbanFabric.Catalog;
            var performance = new LuoyangBuildingPerformancePlanSource(
                Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                source.Bindings, _buildableFacilityCatalog);
            _luoyangBuildingPerformancePlan = performance.Plan;
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                _buildableFacilityCatalog, _luoyangBuildingPerformancePlan);
            _infrastructureProductionKitCatalog = infrastructure.Catalog;
            _luoyangInfrastructureProductionPlan = infrastructure.Plan;
            _luoyangInfrastructureFacilitiesById.Clear();
            foreach (var facility in infrastructure.Plan.Facilities)
                _luoyangInfrastructureFacilitiesById.Add(facility.FacilityId,
                    facility);
            var defense =
                new LuoyangLowFrequencyDefenseProductionKitSource(
                    Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                    _buildableFacilityCatalog, _gateIdentityKitCatalog,
                    _luoyangBuildingPerformancePlan);
            _lowFrequencyDefenseProductionKitCatalog = defense.Catalog;
            _luoyangLowFrequencyDefenseProductionPlan = defense.Plan;
            _luoyangLowFrequencyDefenseFacilitiesById.Clear();
            foreach (var facility in defense.Plan.Facilities)
                _luoyangLowFrequencyDefenseFacilitiesById.Add(
                    facility.FacilityId, facility);
            var resourceAgriculture =
                new LuoyangResourceAgricultureProductionKitSource(
                    Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                    _buildableFacilityCatalog,
                    _luoyangBuildingPerformancePlan);
            _resourceAgricultureProductionKitCatalog =
                resourceAgriculture.Catalog;
            _luoyangResourceAgricultureProductionPlan =
                resourceAgriculture.Plan;
            var finalCivic =
                new LuoyangFinalCivicRitualMedicalProductionKitSource(
                    Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                    _buildableFacilityCatalog, _historicalLandmarkKitCatalog,
                    _luoyangBuildingPerformancePlan);
            _finalCivicProductionKitCatalog = finalCivic.Catalog;
            _luoyangFinalCivicProductionPlan = finalCivic.Plan;
            var finalAssetReview = new LuoyangFinalAssetReviewManifestSource(
                Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                _productionBuildingKitCatalog, _historicalLandmarkKitCatalog,
                _gateIdentityKitCatalog, _mediumFrequencyUrbanFabricKitCatalog,
                _infrastructureProductionKitCatalog,
                _lowFrequencyDefenseProductionKitCatalog,
                _resourceAgricultureProductionKitCatalog,
                _finalCivicProductionKitCatalog,
                _luoyangBuildingPerformancePlan);
            _finalAssetReviewCatalog = finalAssetReview.Catalog;
            _luoyangFinalAssetReviewPlan = finalAssetReview.Plan;
            _luoyangWholeCityCompositionPlan =
                LuoyangWholeCityCompositionRules.CreatePlan(
                    _luoyangBuildingPerformancePlan,
                    _luoyangFinalAssetReviewPlan);
            _luoyangFacilityInteractionNavigationPlan =
                LuoyangFacilityInteractionNavigationRules.CreatePlan(
                    _luoyangBuildingPerformancePlan,
                    _luoyangWholeCityCompositionPlan);
            _luoyangRoadTraversalRefinementPlan =
                LuoyangRoadConnectorPassageTraversalRules.CreatePlan(
                    _luoyangFacilityInteractionNavigationPlan);
            _luoyangHumanScaleLocalMapPlan =
                LuoyangHumanScaleLocalMapRules.CreatePlan(
                    _luoyangBuildingPerformancePlan,
                    _luoyangWholeCityCompositionPlan,
                    _luoyangRoadTraversalRefinementPlan);
            if (_luoyangPassageWorld != null)
            {
                _luoyangPassageWorldCommandSystem =
                    new LuoyangPassageWorldCommandSystem(
                        _luoyangRoadTraversalRefinementPlan);
                _luoyangPassageWorldCommandSystem.ValidatePersistedPlan(
                    _luoyangPassageWorld);
                _luoyangPassageTraversalSession =
                    LuoyangRoadConnectorPassageTraversalRules
                        .CreateSessionFromWorldState(
                            _luoyangRoadTraversalRefinementPlan,
                            _luoyangPassageWorld);
            }
            else
            {
                _luoyangPassageTraversalSession =
                    LuoyangRoadConnectorPassageTraversalRules
                        .CreateInitialSession(
                            _luoyangRoadTraversalRefinementPlan);
            }
            _finalAssetReviewItemsByAssetId.Clear();
            foreach (var item in _finalAssetReviewCatalog.Items)
                _finalAssetReviewItemsByAssetId.Add(item.AssetVariantId, item);
            var p0FinalAsset = new LuoyangP0FinalAssetVerticalSliceSource(
                Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                _buildableFacilityCatalog, _historicalLandmarkKitCatalog,
                _gateIdentityKitCatalog, _finalAssetReviewCatalog);
            _p0FinalAssetVerticalSliceCatalog = p0FinalAsset.Catalog;
            _luoyangP0FinalAssetVerticalSlicePlan = p0FinalAsset.Plan;
            var p0LandmarkSecondBatch =
                new LuoyangP0LandmarkSecondBatchSource(
                    Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                    _buildableFacilityCatalog,
                    _historicalLandmarkKitCatalog,
                    _finalAssetReviewCatalog);
            _p0LandmarkSecondBatchCatalog =
                p0LandmarkSecondBatch.Catalog;
            _luoyangP0LandmarkSecondBatchPlan =
                p0LandmarkSecondBatch.Plan;
            var p0LandmarkThirdBatch =
                new LuoyangP0LandmarkThirdBatchSource(
                    Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                    _buildableFacilityCatalog,
                    _historicalLandmarkKitCatalog,
                    _finalAssetReviewCatalog);
            _p0LandmarkThirdBatchCatalog = p0LandmarkThirdBatch.Catalog;
            _luoyangP0LandmarkThirdBatchPlan = p0LandmarkThirdBatch.Plan;
            var p0NamedGateFourthBatch =
                new LuoyangP0NamedGateFourthBatchSource(
                    Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                    _buildableFacilityCatalog, _gateIdentityKitCatalog,
                    _finalAssetReviewCatalog);
            _p0NamedGateFourthBatchCatalog =
                p0NamedGateFourthBatch.Catalog;
            _luoyangP0NamedGateFourthBatchPlan =
                p0NamedGateFourthBatch.Plan;
            var remainingFinalAssets = new LuoyangRemainingFinalAssetSource(
                Path.Combine(Application.streamingAssetsPath, "WorldMap"),
                _finalAssetReviewCatalog);
            _remainingFinalAssetCatalog = remainingFinalAssets.Catalog;
            _luoyangRemainingFinalAssetPlan = remainingFinalAssets.Plan;
            _buildableFacilityFactory =
                new HanBuildableFacilityModelFactory(_buildableFacilityCatalog,
                    _productionBuildingKitCatalog, _historicalLandmarkKitCatalog,
                    _gateIdentityKitCatalog,
                    _mediumFrequencyUrbanFabricKitCatalog,
                    _infrastructureProductionKitCatalog,
                    _lowFrequencyDefenseProductionKitCatalog,
                    _resourceAgricultureProductionKitCatalog,
                    _finalCivicProductionKitCatalog,
                    _luoyangP0FinalAssetVerticalSlicePlan, null,
                    _luoyangP0LandmarkSecondBatchPlan,
                    _luoyangP0LandmarkThirdBatchPlan,
                    _luoyangP0NamedGateFourthBatchPlan,
                    _luoyangRemainingFinalAssetPlan);
        }

        private void RefreshBuildableFacilityPreview()
        {
            if (_buildableFacilityRoot == null) return;
            _luoyangFacilityInteractionNavigationRuntime?.Dispose();
            _luoyangFacilityInteractionNavigationRuntime = null;
            _luoyangHumanScaleStreamingRuntime?.Dispose();
            _luoyangHumanScaleStreamingRuntime = null;
            _selectedLuoyangFacilityId = null;
            _luoyangBuildingPerformanceBatchRenderer?.Dispose();
            _luoyangBuildingPerformanceBatchRenderer = null;
            _luoyangBuildingBatchMetrics = null;
            if (_vegetationRoot != null)
                _vegetationRoot.SetActive(!_buildableFacilityPreviewVisible);
            ClearChildren(_buildableFacilityRoot.transform);
            _buildableFacilityPlacements.Clear();
            if (!_buildableFacilityPreviewVisible || View != HanNaturalMapView.Region)
                return;
            if (_luoyangBuildingPerformancePreviewVisible)
            {
                var window = LuoyangBuildingPerformanceRules
                    .SelectDensestResidentWindow(_luoyangBuildingPerformancePlan);
                _luoyangBuildingPerformanceBatchRenderer =
                    new LuoyangBuildingPerformanceBatchRenderer();
                _luoyangBuildingBatchMetrics =
                    _luoyangBuildingPerformanceBatchRenderer.Build(
                        _buildableFacilityRoot.transform,
                        _luoyangBuildingPerformancePlan, window,
                        _buildableFacilityFactory,
                        BuildingPerformanceLocalPosition,
                        BuildingPerformanceRotation,
                        BuildingPerformanceScale);
                _luoyangFacilityInteractionNavigationRuntime =
                    LuoyangFacilityInteractionNavigationRuntime.Build(
                        _luoyangFacilityInteractionNavigationPlan,
                        _luoyangRoadTraversalRefinementPlan,
                        _luoyangPassageTraversalSession,
                        _luoyangPassageWorld,
                        window.Facilities, BuildingPerformanceLocalPosition,
                        BuildingPerformanceRotation,
                        BuildingPerformanceCellPosition,
                        (float)HorizontalMetresPerUnit,
                        (float)VerticalMetresPerUnit);
                if (_luoyangPassageWorld != null &&
                    _luoyangFormalPlayerMovementService != null)
                {
                    if (_luoyangFormalPlayerMovementSystem
                            .UsesHumanScaleLocalMap)
                        SetHumanScaleFloatingOrigin(new PlayerSession(
                            _luoyangPassageWorld).ControlledPerson
                            .CurrentCellId64);
                    _luoyangFacilityInteractionNavigationRuntime
                        .BindFormalMovement(_luoyangPassageWorld,
                            _luoyangFormalPlayerMovementService,
                            _luoyangFormalPlayerMovementSystem
                                .UsesHumanScaleLocalMap
                                ? _luoyangHumanScaleLocalMapPlan : null,
                            _luoyangFormalPlayerMovementSystem
                                .UsesHumanScaleLocalMap
                                ? HumanScaleLocalWorldPosition : null);
                    if (_luoyangFormalPlayerMovementSystem
                            .UsesHumanScaleLocalMap)
                    {
                        var person = new PlayerSession(_luoyangPassageWorld)
                            .ControlledPerson;
                        _luoyangHumanScaleStreamingRuntime =
                            LuoyangHumanScaleStreamingRuntime.Build(
                                _luoyangHumanScaleLocalMapPlan,
                                HumanScaleLocalWorldPosition,
                                person.CurrentCellId64,
                                _buildableFacilityRoot.transform);
                    }
                }
                return;
            }
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var plan = _luoyangP0NamedGateFourthBatchPreviewVisible
                ? LuoyangP0NamedGateFourthBatchPreviewPlan.Create(grid,
                    _luoyangP0NamedGateFourthBatchPlan)
                : _luoyangP0LandmarkThirdBatchPreviewVisible
                ? LuoyangP0LandmarkThirdBatchPreviewPlan.Create(grid,
                    _luoyangP0LandmarkThirdBatchPlan)
                : _luoyangP0LandmarkSecondBatchPreviewVisible
                ? LuoyangP0LandmarkSecondBatchPreviewPlan.Create(grid,
                    _luoyangP0LandmarkSecondBatchPlan)
                : _luoyangP0FinalAssetVerticalSlicePreviewVisible
                ? LuoyangP0FinalAssetVerticalSlicePreviewPlan.Create(grid,
                    _luoyangP0FinalAssetVerticalSlicePlan)
                : _luoyangFinalAssetReviewPreviewVisible
                ? LuoyangFinalAssetReviewPreviewPlan.Create(grid,
                    _luoyangFinalAssetReviewPlan,
                    LuoyangFinalAssetReviewPreviewPlan.BoardCenterRow,
                    LuoyangFinalAssetReviewPreviewPlan.BoardCenterColumn)
                : _luoyangFinalCivicPreviewVisible
                ? LuoyangFinalCivicRitualMedicalProductionPreviewPlan.Create(
                    grid, _luoyangFinalCivicProductionPlan)
                : _luoyangResourceAgriculturePreviewVisible
                ? LuoyangResourceAgricultureProductionPreviewPlan.Create(grid,
                    _luoyangResourceAgricultureProductionPlan)
                : _luoyangLowFrequencyDefensePreviewVisible
                ? LuoyangLowFrequencyDefenseProductionPreviewPlan.Create(grid,
                    _luoyangLowFrequencyDefenseProductionPlan)
                : _luoyangInfrastructurePreviewVisible
                ? LuoyangInfrastructureProductionPreviewPlan.Create(grid,
                    _luoyangInfrastructureProductionPlan)
                : _luoyangMediumFrequencyUrbanFabricPreviewVisible
                ? LuoyangMediumFrequencyUrbanFabricPreviewPlan.Create(grid,
                    _focusRow, _focusColumn)
                : _luoyangGateIdentityPreviewVisible
                ? LuoyangGateIdentityPreviewPlan.Create(grid,
                    _gateIdentityKitCatalog)
                : _luoyangHistoricalLandmarkPreviewVisible
                ? LuoyangHistoricalLandmarkPreviewPlan.Create(grid,
                    _historicalLandmarkKitCatalog)
                : _luoyangFacilityCoveragePreviewVisible
                    ? LuoyangFacilityModelCoveragePreviewPlan.Create(grid, _focusRow,
                        _focusColumn)
                    : HanBuildableFacilityPreviewPlan.Create(grid, _focusRow,
                        _focusColumn);
            foreach (var placement in plan)
                PlaceBuildableFacilityModel(placement, true);
        }

        private void DisposeBuildableFacilityModelKit()
        {
            _luoyangFacilityInteractionNavigationRuntime?.Dispose();
            _luoyangFacilityInteractionNavigationRuntime = null;
            _luoyangHumanScaleStreamingRuntime?.Dispose();
            _luoyangHumanScaleStreamingRuntime = null;
            _hasLuoyangHumanScaleFloatingOrigin = false;
            _selectedLuoyangFacilityId = null;
            _luoyangBuildingPerformanceBatchRenderer?.Dispose();
            _luoyangBuildingPerformanceBatchRenderer = null;
            _luoyangBuildingBatchMetrics = null;
            _buildableFacilityPlacements.Clear();
            _luoyangFacilityCoveragePreviewVisible = false;
            _luoyangHistoricalLandmarkPreviewVisible = false;
            _luoyangGateIdentityPreviewVisible = false;
            _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
            _luoyangBuildingPerformancePreviewVisible = false;
            _luoyangInfrastructurePreviewVisible = false;
            _luoyangLowFrequencyDefensePreviewVisible = false;
            _luoyangResourceAgriculturePreviewVisible = false;
            _luoyangFinalCivicPreviewVisible = false;
            _luoyangFinalAssetReviewPreviewVisible = false;
            _luoyangP0FinalAssetVerticalSlicePreviewVisible = false;
            _luoyangP0LandmarkSecondBatchPreviewVisible = false;
            _luoyangP0LandmarkThirdBatchPreviewVisible = false;
            _luoyangP0NamedGateFourthBatchPreviewVisible = false;
            _buildableFacilityFactory?.Dispose();
            _buildableFacilityFactory = null;
            _buildableFacilityCatalog = null;
            _productionBuildingKitCatalog = null;
            _historicalLandmarkKitCatalog = null;
            _gateIdentityKitCatalog = null;
            _mediumFrequencyUrbanFabricKitCatalog = null;
            _infrastructureProductionKitCatalog = null;
            _luoyangInfrastructureProductionPlan = null;
            _luoyangInfrastructureFacilitiesById.Clear();
            _lowFrequencyDefenseProductionKitCatalog = null;
            _luoyangLowFrequencyDefenseProductionPlan = null;
            _luoyangLowFrequencyDefenseFacilitiesById.Clear();
            _resourceAgricultureProductionKitCatalog = null;
            _luoyangResourceAgricultureProductionPlan = null;
            _finalCivicProductionKitCatalog = null;
            _luoyangFinalCivicProductionPlan = null;
            _finalAssetReviewCatalog = null;
            _luoyangFinalAssetReviewPlan = null;
            _luoyangWholeCityCompositionPlan = null;
            _luoyangFacilityInteractionNavigationPlan = null;
            _luoyangRoadTraversalRefinementPlan = null;
            _luoyangHumanScaleLocalMapPlan = null;
            _luoyangPassageTraversalSession = null;
            _p0FinalAssetVerticalSliceCatalog = null;
            _luoyangP0FinalAssetVerticalSlicePlan = null;
            _p0LandmarkSecondBatchCatalog = null;
            _luoyangP0LandmarkSecondBatchPlan = null;
            _p0LandmarkThirdBatchCatalog = null;
            _luoyangP0LandmarkThirdBatchPlan = null;
            _p0NamedGateFourthBatchCatalog = null;
            _luoyangP0NamedGateFourthBatchPlan = null;
            _remainingFinalAssetCatalog = null;
            _luoyangRemainingFinalAssetPlan = null;
            _finalAssetReviewItemsByAssetId.Clear();
            _luoyangBuildingPerformancePlan = null;
        }

        public bool TrySelectLuoyangFacility(Ray ray)
        {
            if (_luoyangFacilityInteractionNavigationRuntime == null)
                return false;
            var hit = Physics.RaycastAll(ray, _camera == null
                    ? float.MaxValue : _camera.farClipPlane,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Collide)
                .Select(item => new
                {
                    Hit = item,
                    Instance = item.collider.GetComponent<
                        LuoyangFacilitySelectionProxyInstance>()
                })
                .Where(item => item.Instance != null)
                .OrderBy(item => item.Hit.distance)
                .FirstOrDefault();
            if (hit == null)
            {
                ClearLuoyangFacilitySelection();
                return false;
            }
            return SelectLuoyangFacility(hit.Instance.FacilityId);
        }

        public bool SelectLuoyangFacility(string facilityId)
        {
            if (_luoyangFacilityInteractionNavigationRuntime == null ||
                !_luoyangFacilityInteractionNavigationRuntime.TrySelect(
                    facilityId))
            {
                _selectedLuoyangFacilityId = null;
                return false;
            }
            _selectedLuoyangFacilityId = facilityId;
            return true;
        }

        public void ClearLuoyangFacilitySelection()
        {
            _selectedLuoyangFacilityId = null;
            _luoyangFacilityInteractionNavigationRuntime?.ClearSelection();
        }

        public bool PlaceLuoyangPedestrianAtFacility(string facilityId,
            string actorId = null) =>
            _luoyangFacilityInteractionNavigationRuntime?
                .TryPlacePedestrianAtFacility(facilityId, actorId) ?? false;

        public bool SetLuoyangPedestrianDestination(string facilityId) =>
            _luoyangFacilityInteractionNavigationRuntime?
                .TrySetPedestrianDestination(facilityId) ?? false;

        public bool TrySetLuoyangPedestrianDestination(Ray ray)
        {
            if (_luoyangFacilityInteractionNavigationRuntime == null)
                return false;
            var hit = Physics.RaycastAll(ray, _camera == null
                    ? float.MaxValue : _camera.farClipPlane,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)
                .Where(item => item.collider.GetComponentInParent<
                    LuoyangClickWalkPedestrianInstance>() == null)
                .OrderBy(item => item.distance)
                .FirstOrDefault();
            if (hit.collider != null)
            {
                if (_luoyangHumanScaleStreamingRuntime != null &&
                    _luoyangHumanScaleStreamingRuntime.TryResolveProxy(
                        hit.collider, out var localTarget))
                    return SetLuoyangPedestrianDestination(
                        localTarget.FacilityId);
                if (_luoyangHumanScaleStreamingRuntime != null)
                    return TryResolveHumanScaleGroundTarget(hit.point,
                               out localTarget) &&
                           SetLuoyangPedestrianDestination(
                               localTarget.FacilityId);
                return _luoyangFacilityInteractionNavigationRuntime
                    .TrySetPedestrianDestination(hit.point);
            }
            var ground = new Plane(Vector3.up, Vector3.zero);
            return ground.Raycast(ray, out var distance) &&
                   _luoyangFacilityInteractionNavigationRuntime
                       .TrySetPedestrianDestination(ray.GetPoint(distance));
        }

        public bool StepLuoyangPedestrian(float deltaSeconds)
        {
            if (_luoyangPassageWorld != null &&
                _luoyangHumanScaleStreamingRuntime != null)
                _luoyangHumanScaleStreamingRuntime.MoveWindow(
                    new PlayerSession(_luoyangPassageWorld)
                        .ControlledPerson.CurrentCellId64);
            return _luoyangFacilityInteractionNavigationRuntime?
                .StepPedestrian(deltaSeconds) ?? false;
        }

        public IReadOnlyList<string> GetLuoyangPassageApproachFacilityIds(
            string facilityId)
        {
            if (_luoyangRoadTraversalRefinementPlan == null ||
                string.IsNullOrWhiteSpace(facilityId) ||
                !_luoyangRoadTraversalRefinementPlan
                    .NavigationNodesByFacilityId.TryGetValue(facilityId,
                        out var passage)) return Array.Empty<string>();
            var nodeById = _luoyangRoadTraversalRefinementPlan.NavigationNodes
                .ToDictionary(item => item.NodeId, StringComparer.Ordinal);
            return _luoyangRoadTraversalRefinementPlan.NavigationEdges.Where(
                    item => string.Equals(item.EdgeProfileId,
                        LuoyangRoadConnectorPassageTraversalIds
                            .PassageApproachEdgeProfileId,
                        StringComparison.Ordinal) &&
                        (item.FromNodeId == passage.NodeId ||
                         item.ToNodeId == passage.NodeId))
                .Select(item => item.FromNodeId == passage.NodeId
                    ? nodeById[item.ToNodeId].FacilityId
                    : nodeById[item.FromNodeId].FacilityId)
                .OrderBy(item => item, StringComparer.Ordinal).ToArray();
        }

        public LuoyangClickWalkPedestrianInstance
            GetLuoyangClickWalkPedestrian()
        {
            if (_luoyangFacilityInteractionNavigationRuntime?
                    .PedestrianInstance == null)
                throw new InvalidOperationException(
                    "The Luoyang click-to-walk pedestrian is not active.");
            return _luoyangFacilityInteractionNavigationRuntime
                .PedestrianInstance;
        }

        public string GetLuoyangPassageTraversalStatus(string facilityId) =>
            _luoyangPassageTraversalSession?.Get(facilityId).TraversalStatusId;

        public LuoyangPassagePedestrianPresentationInstance
            GetLuoyangPassagePedestrianPresentation(string facilityId)
        {
            if (_luoyangFacilityInteractionNavigationRuntime == null)
                throw new InvalidOperationException(
                    "The Luoyang passage presentation runtime is not active.");
            return _luoyangFacilityInteractionNavigationRuntime
                .GetPassagePresentation(facilityId);
        }

        public WorldCommandExecutionReport BindLuoyangPassageWorld(
            WorldState world,
            WorldCommandRuntime commandRuntime,
            bool initializeIfMissing = true)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (commandRuntime == null)
                throw new ArgumentNullException(nameof(commandRuntime));
            if (_luoyangRoadTraversalRefinementPlan == null)
                throw new InvalidOperationException(
                    "Luoyang refined road traversal plan is not initialized.");

            var commandSystem = new LuoyangPassageWorldCommandSystem(
                _luoyangRoadTraversalRefinementPlan);
            commandSystem.RegisterHandlers(commandRuntime);
            var report = new WorldCommandExecutionReport(0, 0, 0);
            if (world.LuoyangPassageTraversals.Count == 0)
            {
                if (!initializeIfMissing)
                    throw new InvalidOperationException(
                        "The persisted Luoyang passage set is not initialized.");
                commandSystem.EnsureInitialized(world, commandRuntime);
                report = commandRuntime.ProcessDue(world);
                commandRuntime.DispatchPublishedEvents(world);
            }
            commandSystem.ValidatePersistedPlan(world);

            _luoyangPassageWorld = world;
            _luoyangPassageWorldCommandRuntime = commandRuntime;
            _luoyangPassageWorldCommandSystem = commandSystem;
            if (!string.IsNullOrWhiteSpace(world.PlayerPersonId))
            {
                _luoyangFormalPlayerMovementSystem =
                    new LuoyangFormalPlayerMovementSystem(
                        _luoyangRoadTraversalRefinementPlan, null,
                        HasCompleteHumanScaleFacilityWorld(world)
                            ? _luoyangHumanScaleLocalMapPlan
                            : null);
                _luoyangFormalPlayerMovementSystem.RegisterHandlers(
                    commandRuntime);
                var initialFacilityId =
                    SelectInitialFormalPlayerFacilityId(world);
                if (_luoyangFormalPlayerMovementSystem.EnsureInitialized(
                        world, commandRuntime, initialFacilityId))
                {
                    commandRuntime.ProcessDue(world);
                    commandRuntime.DispatchPublishedEvents(world);
                }
                _luoyangFormalMovementSimulator = new WorldSimulator(
                    world.MasterSeed, null,
                    new WorldStatePersonRepository(world), commandRuntime);
                _luoyangFormalPlayerMovementService =
                    new LuoyangFormalPlayerMovementService(
                        _luoyangFormalPlayerMovementSystem, commandRuntime,
                        _luoyangFormalMovementSimulator);
            }
            RefreshPersistedLuoyangPassageProjection();
            if (_luoyangFormalPlayerMovementService != null)
            {
                if (_luoyangFormalPlayerMovementSystem
                        .UsesHumanScaleLocalMap)
                    SetHumanScaleFloatingOrigin(new PlayerSession(world)
                        .ControlledPerson.CurrentCellId64);
                _luoyangFacilityInteractionNavigationRuntime?
                    .BindFormalMovement(world,
                        _luoyangFormalPlayerMovementService,
                        _luoyangFormalPlayerMovementSystem
                            .UsesHumanScaleLocalMap
                            ? _luoyangHumanScaleLocalMapPlan : null,
                        _luoyangFormalPlayerMovementSystem
                            .UsesHumanScaleLocalMap
                            ? HumanScaleLocalWorldPosition : null);
                if (_luoyangFacilityInteractionNavigationRuntime != null &&
                    _luoyangFormalPlayerMovementSystem
                        .UsesHumanScaleLocalMap)
                {
                    var person = new PlayerSession(world).ControlledPerson;
                    _luoyangHumanScaleStreamingRuntime?.Dispose();
                    _luoyangHumanScaleStreamingRuntime =
                        LuoyangHumanScaleStreamingRuntime.Build(
                            _luoyangHumanScaleLocalMapPlan,
                            HumanScaleLocalWorldPosition,
                            person.CurrentCellId64,
                            _buildableFacilityRoot?.transform);
                }
            }
            return report;
        }

        public void UnbindLuoyangPassageWorld()
        {
            _luoyangPassageWorld = null;
            _luoyangPassageWorldCommandRuntime = null;
            _luoyangPassageWorldCommandSystem = null;
            _luoyangFormalPlayerMovementSystem = null;
            _luoyangFormalPlayerMovementService = null;
            _luoyangFormalMovementSimulator = null;
            _luoyangHumanScaleStreamingRuntime?.Dispose();
            _luoyangHumanScaleStreamingRuntime = null;
            _hasLuoyangHumanScaleFloatingOrigin = false;
            _luoyangFacilityInteractionNavigationRuntime?
                .UnbindFormalMovement();
            if (_luoyangRoadTraversalRefinementPlan == null)
            {
                _luoyangPassageTraversalSession = null;
                return;
            }
            _luoyangPassageTraversalSession =
                LuoyangRoadConnectorPassageTraversalRules.CreateInitialSession(
                    _luoyangRoadTraversalRefinementPlan);
            _luoyangFacilityInteractionNavigationRuntime?
                .RefreshTraversalState(_luoyangPassageTraversalSession,
                    _luoyangPassageWorld);
        }

        public bool SetLuoyangPassageTraversalStatus(string facilityId,
            string statusId, long absoluteTick, string reasonId)
        {
            if (_luoyangPassageTraversalSession == null)
                throw new InvalidOperationException(
                    "Luoyang passage traversal session is not initialized.");
            if (_luoyangPassageWorld != null)
            {
                var expectedTick = checked(
                    _luoyangPassageWorld.AbsoluteDay * 4L +
                    _luoyangPassageWorld.Segment);
                if (absoluteTick != expectedTick)
                    throw new InvalidOperationException(
                        "A persisted Luoyang passage command must use the " +
                        "bound world's current deterministic tick.");
                var enqueued = _luoyangPassageWorldCommandSystem
                    .EnqueueTransition(
                        _luoyangPassageWorld,
                        _luoyangPassageWorldCommandRuntime,
                        facilityId,
                        statusId,
                        reasonId,
                        LuoyangPassageTraversalWorldContractIds
                            .PresentationBridgeIssuerId);
                if (!enqueued) return false;
                _luoyangPassageWorldCommandRuntime.ProcessDue(
                    _luoyangPassageWorld);
                _luoyangPassageWorldCommandRuntime.DispatchPublishedEvents(
                    _luoyangPassageWorld);
                RefreshPersistedLuoyangPassageProjection();
                return true;
            }
            var changed = _luoyangPassageTraversalSession.SetStatus(facilityId,
                statusId, absoluteTick, reasonId);
            if (changed)
                _luoyangFacilityInteractionNavigationRuntime?
                    .RefreshTraversalState(_luoyangPassageTraversalSession,
                        _luoyangPassageWorld);
            return changed;
        }

        public void ResetLuoyangPassageTraversalSession()
        {
            if (_luoyangPassageWorld != null)
                throw new InvalidOperationException(
                    "Persisted Luoyang passage history cannot be reset from " +
                    "Presentation. Issue explicit passage transition commands.");
            if (_luoyangRoadTraversalRefinementPlan == null)
                throw new InvalidOperationException(
                    "Luoyang refined road traversal plan is not initialized.");
            _luoyangPassageTraversalSession =
                LuoyangRoadConnectorPassageTraversalRules.CreateInitialSession(
                    _luoyangRoadTraversalRefinementPlan);
            _luoyangFacilityInteractionNavigationRuntime?
                .RefreshTraversalState(_luoyangPassageTraversalSession,
                    _luoyangPassageWorld);
        }

        private void RefreshPersistedLuoyangPassageProjection()
        {
            if (_luoyangPassageWorld == null ||
                _luoyangRoadTraversalRefinementPlan == null)
                return;
            _luoyangPassageTraversalSession =
                LuoyangRoadConnectorPassageTraversalRules
                    .CreateSessionFromWorldState(
                        _luoyangRoadTraversalRefinementPlan,
                        _luoyangPassageWorld);
            _luoyangFacilityInteractionNavigationRuntime?
                .RefreshTraversalState(_luoyangPassageTraversalSession,
                    _luoyangPassageWorld);
        }

        private string SelectInitialFormalPlayerFacilityId(WorldState world)
        {
            var controlled = new PlayerSession(world).ControlledPerson;
            if (!string.IsNullOrWhiteSpace(controlled.CurrentFacilityId) &&
                _luoyangRoadTraversalRefinementPlan
                    .NavigationNodesByFacilityId.ContainsKey(
                        controlled.CurrentFacilityId))
                return controlled.CurrentFacilityId;
            var residentWindow = LuoyangBuildingPerformanceRules
                .SelectDensestResidentWindow(_luoyangBuildingPerformancePlan);
            var visible = new HashSet<string>(residentWindow.Facilities.Select(
                item => item.FacilityId), StringComparer.Ordinal);
            return _luoyangRoadTraversalRefinementPlan.NavigationNodes
                .Where(item => visible.Contains(item.FacilityId) &&
                    string.Equals(item.FacilityDefinitionId,
                        "facility.public.road", StringComparison.Ordinal))
                .OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                .First().FacilityId;
        }

        private bool HasCompleteHumanScaleFacilityWorld(WorldState world)
        {
            if (_luoyangHumanScaleLocalMapPlan == null) return false;
            var formalIds = new HashSet<string>(world.Facilities.Where(item =>
                    item != null).Select(item => item.Id),
                StringComparer.Ordinal);
            return _luoyangHumanScaleLocalMapPlan.FacilityCapabilities.All(
                item => formalIds.Contains(item.FacilityId));
        }

        public IReadOnlyList<string> FindLuoyangFacilityPath(
            string fromFacilityId, string toFacilityId)
        {
            if (_luoyangRoadTraversalRefinementPlan == null ||
                _luoyangPassageTraversalSession == null)
                return Array.Empty<string>();
            return LuoyangRoadConnectorPassageTraversalRules.FindFacilityPath(
                _luoyangRoadTraversalRefinementPlan,
                _luoyangPassageTraversalSession, fromFacilityId, toFacilityId);
        }

        private Vector3 BuildingPerformanceLocalPosition(
            LuoyangBuildingPerformanceFacility facility)
        {
            var sample = _source.ReadSample(facility.GridRow,
                facility.GridColumn).Cell;
            if (_luoyangWholeCityCompositionPlan == null ||
                !_luoyangWholeCityCompositionPlan.AnchorsByFacilityId
                    .TryGetValue(facility.FacilityId, out var anchor))
                throw new InvalidOperationException(
                    "Luoyang whole-city composition anchor is missing.");
            var easting = sample.CenterX + anchor.VisualLocalEastMetres;
            var northing = sample.CenterY + anchor.VisualLocalNorthMetres;
            return new Vector3(
                (float)((easting - _floatingOrigin.EastingMetres) /
                        HorizontalMetresPerUnit),
                GetPresentationHeightForGlobal(easting, northing) +
                0.02f,
                (float)((northing - _floatingOrigin.NorthingMetres) /
                        HorizontalMetresPerUnit));
        }

        private Vector3 HumanScaleLocalWorldPosition(double easting,
            double northing)
        {
            if (!_hasLuoyangHumanScaleFloatingOrigin)
                throw new InvalidOperationException(
                    "The Luoyang human-scale floating origin is not set.");
            var position = _luoyangHumanScaleLocalMapPlan.WorldScale
                .WorldToUnity(new GlobalProjectedCoordinate(easting,
                    northing), 0d, _luoyangHumanScaleFloatingOrigin);
            return new Vector3((float)position.XMetres, 0.02f,
                (float)position.ZMetres);
        }

        private void SetHumanScaleFloatingOrigin(ulong cellId64)
        {
            var space = _luoyangHumanScaleLocalMapPlan.LocalSpacesByCellId[
                cellId64];
            _luoyangHumanScaleFloatingOrigin = new GlobalProjectedCoordinate(
                space.OriginEastingMetres + space.WidthMetres * 0.5d,
                space.OriginNorthingMetres + space.HeightMetres * 0.5d);
            _hasLuoyangHumanScaleFloatingOrigin = true;
        }

        private bool TryResolveHumanScaleGroundTarget(Vector3 unityPosition,
            out LuoyangResolvedLocalTarget target)
        {
            target = null;
            if (_luoyangHumanScaleLocalMapPlan == null ||
                !_hasLuoyangHumanScaleFloatingOrigin)
                return false;
            var world = _luoyangHumanScaleLocalMapPlan.WorldScale.UnityToWorld(
                new UnityLocalPosition(unityPosition.x, unityPosition.y,
                    unityPosition.z), _luoyangHumanScaleFloatingOrigin);
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            if (!grid.TryFromProjected(world.EastingMetres,
                    world.NorthingMetres, out var cellId) ||
                !_luoyangHumanScaleLocalMapPlan.LocalSpacesByCellId
                    .TryGetValue(cellId.Value, out var space))
                return false;
            target = new LuoyangLocalTargetResolver(
                _luoyangHumanScaleLocalMapPlan).ResolveGround(cellId.Value,
                world.EastingMetres - space.OriginEastingMetres,
                world.NorthingMetres - space.OriginNorthingMetres);
            return target.IsValid;
        }

        private Vector3 BuildingPerformanceCellPosition(int row, int column)
        {
            var cell = _source.ReadSample(row, column).Cell;
            return new Vector3(
                (float)((cell.CenterX - _floatingOrigin.EastingMetres) /
                        HorizontalMetresPerUnit),
                GetPresentationHeightForGlobal(cell.CenterX, cell.CenterY) +
                0.02f,
                (float)((cell.CenterY - _floatingOrigin.NorthingMetres) /
                        HorizontalMetresPerUnit));
        }

        private float BuildingPerformanceRotation(
            LuoyangBuildingPerformanceFacility facility)
        {
            if (_luoyangLowFrequencyDefenseFacilitiesById.TryGetValue(
                    facility.FacilityId, out var defense))
                return defense.RotationDegrees;
            if (_luoyangInfrastructureFacilitiesById.TryGetValue(
                    facility.FacilityId, out var infrastructure))
                return infrastructure.RotationDegrees;
            var gate = _gateIdentityKitCatalog?.Profiles.FirstOrDefault(item =>
                string.Equals(item.FacilityId, facility.FacilityId,
                    StringComparison.Ordinal));
            if (gate != null)
                return
                LuoyangGateIdentityKitIds.RotationForFacing(gate.VisualFacing);
            if (_luoyangWholeCityCompositionPlan != null &&
                _luoyangWholeCityCompositionPlan.AnchorsByFacilityId
                    .TryGetValue(facility.FacilityId, out var anchor))
                return anchor.RotationDegrees;
            return facility.RotationDegrees;
        }

        private Vector3 BuildingPerformanceScale(
            LuoyangBuildingPerformanceFacility facility)
        {
            if (_luoyangWholeCityCompositionPlan == null ||
                !_luoyangWholeCityCompositionPlan.AnchorsByFacilityId
                    .TryGetValue(facility.FacilityId, out var anchor))
                return Vector3.one;
            return Vector3.one * anchor.Scale;
        }
    }
}
