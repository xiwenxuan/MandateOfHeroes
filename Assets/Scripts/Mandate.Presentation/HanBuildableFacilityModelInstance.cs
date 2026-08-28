using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mandate.Presentation
{
    public sealed class HanBuildableFacilityModelInstance : MonoBehaviour
    {
        public string ModelId;
        public string AssetId;
        public string RuntimeBindingId;
        public ulong CellId64;
        public bool PreviewOnly;
        public bool ProductionReady;
        public bool TerrainConforming;
        public string ProductionProfileId;
        public string ProductionAssetVariantId;
        public string ProductionLodProfileId;
        public string PlacementAnchorId;
        public string EntranceAnchorId;
        public bool HistoricalLandmarkReady;
        public string HistoricalLandmarkProfileId;
        public string HistoricalLandmarkAssetVariantId;
        public string HistoricalLandmarkSilhouetteId;
        public string HistoricalLandmarkFacilityId;
        public string HistoricalConfidence;
        public string SpatialPrecision;
        public bool GateIdentityReady;
        public string GateIdentityProfileId;
        public string GateIdentityAssetVariantId;
        public string GateIdentitySilhouetteId;
        public string GateIdentityFacilityId;
        public string GateIdentityLodProfileId;
        public string GateClassId;
        public string GatehouseTypeId;
        public string FacilityDirection;
        public string VisualFacing;
        public string DirectionBasisId;
        public string OuterPassageAnchorId;
        public string InnerPassageAnchorId;
        public bool MediumFrequencyUrbanFabricReady;
        public string UrbanFabricProfileId;
        public string UrbanFabricAssetVariantId;
        public string UrbanFabricRoleId;
        public string UrbanFabricDensityClassId;
        public string UrbanFabricStreetInterfaceId;
        public string UrbanFabricLodProfileId;
        public bool InfrastructureProductionReady;
        public string InfrastructureProfileId;
        public string InfrastructureAssetVariantId;
        public string InfrastructureRoleId;
        public string InfrastructureAlignmentModeId;
        public string InfrastructureLodProfileId;
        public string InfrastructureTopologyId;
        public int InfrastructureConnectionMask;
        public bool LowFrequencyDefenseProductionReady;
        public string LowFrequencyDefenseProfileId;
        public string LowFrequencyDefenseAssetVariantId;
        public string LowFrequencyDefenseRoleId;
        public string LowFrequencyDefenseModeId;
        public string LowFrequencyDefenseFacingPolicyId;
        public string LowFrequencyDefenseLodProfileId;
        public bool ResourceAgricultureProductionReady;
        public string ResourceAgricultureProfileId;
        public string ResourceAgricultureAssetVariantId;
        public string ResourceAgricultureRoleId;
        public string ResourceAgricultureEvidenceBasisId;
        public string ResourceAgricultureLodProfileId;
        public bool FinalCivicProductionReady;
        public string FinalCivicProfileId;
        public string FinalCivicAssetVariantId;
        public string FinalCivicRoleId;
        public string FinalCivicModeId;
        public string FinalCivicEvidenceBasisId;
        public string FinalCivicLodProfileId;
        public bool FinalAssetReviewReady;
        public string FinalAssetReviewItemId;
        public string FinalAssetReviewAuditGroupId;
        public string FinalAssetReviewPriorityId;
        public string FinalAssetReviewReplacementSlotId;
        public int FinalAssetReviewFacilityUsageCount;
        public bool FinalAssetRuntimeReady;
        public string FinalAssetTaskStatusId;
        public int FinalAssetReviewOrder;
        public string FinalAssetReplacementSlotId;
        public string FinalAssetPrefabResourcePath;
        public bool FinalAssetArtistPrefabLoaded;
        public bool FinalAssetProceduralFallbackActive;
        public bool FinalAssetApproved;
        public bool P0FinalAssetVerticalSliceReady;
        public string P0FinalAssetCandidateId;
        public string P0FinalAssetCandidateStatusId;
        public string P0FinalAssetReplacementSlotId;
        public string P0FinalAssetMaterialSetId;
        public string P0FinalAssetLodProfileId;
        public string P0FinalAssetRuntimeModeId;
        public string P0FinalAssetPrefabResourcePath;
        public bool P0FinalAssetArtistPrefabLoaded;
        public bool P0FinalAssetProceduralFallbackActive;
        public bool P0FinalAssetFinalArtApproved;
        public bool P0LandmarkSecondBatchReady;
        public string P0LandmarkSecondBatchStatusId;
        public bool P0LandmarkThirdBatchReady;
        public string P0LandmarkThirdBatchStatusId;
        public bool P0NamedGateFourthBatchReady;
        public string P0NamedGateFourthBatchStatusId;
    }

    public sealed class HanBuildableFacilityModelPlacement
    {
        public HanBuildableFacilityModelPlacement(string modelId, WorldMapCellId cellId,
            string runtimeBindingId, float rotationDegrees)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model id is required.", nameof(modelId));
            if (string.IsNullOrWhiteSpace(runtimeBindingId))
                throw new ArgumentException("Runtime binding id is required.",
                    nameof(runtimeBindingId));
            if (float.IsNaN(rotationDegrees) || float.IsInfinity(rotationDegrees))
                throw new ArgumentOutOfRangeException(nameof(rotationDegrees));
            ModelId = modelId;
            CellId = cellId;
            RuntimeBindingId = runtimeBindingId;
            RotationDegrees = rotationDegrees;
        }

        public string ModelId { get; }
        public WorldMapCellId CellId { get; }
        public string RuntimeBindingId { get; }
        public float RotationDegrees { get; }
    }

    public static class HanBuildableFacilityPreviewPlan
    {
        public const int ModelCount = 7;

        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, int centerRow, int centerColumn)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            var placements = new List<HanBuildableFacilityModelPlacement>(ModelCount);
            Add(placements, grid, HanBuildableFacilityModelIds.Residence,
                centerRow - 3, centerColumn - 3, 0f);
            Add(placements, grid, HanBuildableFacilityModelIds.Warehouse,
                centerRow - 3, centerColumn, 90f);
            Add(placements, grid, HanBuildableFacilityModelIds.Workshop,
                centerRow - 3, centerColumn + 3, 180f);
            Add(placements, grid, HanBuildableFacilityModelIds.Market,
                centerRow, centerColumn - 3, 270f);
            Add(placements, grid, HanBuildableFacilityModelIds.FieldHospital,
                centerRow, centerColumn, 0f);
            Add(placements, grid, HanBuildableFacilityModelIds.CityWall,
                centerRow, centerColumn + 3, 90f);
            Add(placements, grid, HanBuildableFacilityModelIds.CityGate,
                centerRow + 3, centerColumn, 0f);
            return placements;
        }

        private static void Add(List<HanBuildableFacilityModelPlacement> placements,
            CellGridIndex grid, string modelId, int row, int column, float rotation)
        {
            if (!grid.Contains(row, column))
                throw new ArgumentOutOfRangeException(nameof(row),
                    "Buildable Facility preview exceeds the Global Cell grid.");
            placements.Add(new HanBuildableFacilityModelPlacement(modelId,
                grid.ToCellId(row, column), "preview." + modelId, rotation));
        }
    }

    public static class LuoyangFacilityModelCoveragePreviewPlan
    {
        public const int Columns = 6;
        public static int ModelCount => LuoyangFacilityModelCoverageIds.AllModelIds.Count;

        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, int centerRow, int centerColumn)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            var placements = new List<HanBuildableFacilityModelPlacement>(ModelCount);
            for (var index = 0; index < ModelCount; index++)
            {
                var row = centerRow - 5 + (index / Columns) * 2;
                var column = centerColumn - 5 + (index % Columns) * 2;
                if (!grid.Contains(row, column))
                    throw new ArgumentOutOfRangeException(nameof(centerRow),
                        "Luoyang Facility model coverage preview exceeds the Global Cell grid.");
                var modelId = LuoyangFacilityModelCoverageIds.AllModelIds[index];
                placements.Add(new HanBuildableFacilityModelPlacement(modelId,
                    grid.ToCellId(row, column), "coverage-preview." + modelId,
                    index % 4 * 90f));
            }
            return placements;
        }
    }

    public static class LuoyangHistoricalLandmarkPreviewPlan
    {
        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, LuoyangHistoricalLandmarkKitCatalog catalog)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var placements = new List<HanBuildableFacilityModelPlacement>(
                catalog.Profiles.Count);
            for (var index = 0; index < catalog.Profiles.Count; index++)
            {
                var profile = catalog.Profiles[index];
                if (!grid.Contains(profile.GridY, profile.GridX) ||
                    grid.ToCellId(profile.GridY, profile.GridX).Value != profile.CellId64)
                    throw new InvalidOperationException(
                        "Luoyang landmark profile does not match its authoritative Global Cell.");
                placements.Add(new HanBuildableFacilityModelPlacement(
                    profile.BaseModelId, new WorldMapCellId(profile.CellId64),
                    profile.FacilityId, index % 4 * 90f));
            }
            return placements;
        }
    }

    public static class LuoyangGateIdentityPreviewPlan
    {
        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, LuoyangGateIdentityKitCatalog catalog)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var placements = new List<HanBuildableFacilityModelPlacement>(
                catalog.Profiles.Count);
            foreach (var profile in catalog.Profiles)
            {
                if (!grid.Contains(profile.GridY, profile.GridX) ||
                    grid.ToCellId(profile.GridY, profile.GridX).Value != profile.CellId64)
                    throw new InvalidOperationException(
                        "Luoyang gate profile does not match its authoritative Global Cell.");
                placements.Add(new HanBuildableFacilityModelPlacement(
                    profile.BaseModelId, new WorldMapCellId(profile.CellId64),
                    profile.FacilityId,
                    LuoyangGateIdentityKitIds.RotationForFacing(profile.VisualFacing)));
            }
            return placements;
        }
    }

    public static class LuoyangMediumFrequencyUrbanFabricPreviewPlan
    {
        public const int Rows = 3;
        public const int Columns = 5;
        public const int PlacementCount = Rows * Columns;

        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, int centerRow, int centerColumn)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            var result = new List<HanBuildableFacilityModelPlacement>(PlacementCount);
            for (var row = 0; row < Rows; row++)
            for (var column = 0; column < Columns; column++)
            {
                var targetRow = centerRow + (row - 1) * 2;
                var targetColumn = centerColumn + (column - 2) * 2;
                if (!grid.Contains(targetRow, targetColumn))
                    throw new ArgumentOutOfRangeException(nameof(centerRow),
                        "Luoyang urban-fabric preview exceeds the Global Cell grid.");
                var modelIndex = (row * 2 + column) %
                                 LuoyangMediumFrequencyUrbanFabricKitIds.ModelIds.Count;
                var modelId = LuoyangMediumFrequencyUrbanFabricKitIds
                    .ModelIds[modelIndex];
                result.Add(new HanBuildableFacilityModelPlacement(modelId,
                    grid.ToCellId(targetRow, targetColumn),
                    "urban-fabric-preview." + row + "." + column + "." + modelId,
                    (row + column) % 4 * 90f));
            }
            return result;
        }
    }

    public static class LuoyangInfrastructureProductionPreviewPlan
    {
        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, LuoyangInfrastructureProductionPlan plan)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var result = new List<HanBuildableFacilityModelPlacement>(
                plan.Facilities.Count);
            foreach (var facility in plan.Facilities)
            {
                if (!grid.Contains(facility.GridRow, facility.GridColumn) ||
                    grid.ToCellId(facility.GridRow, facility.GridColumn).Value !=
                    facility.CellId64)
                    throw new InvalidOperationException(
                        "Luoyang infrastructure profile does not match its authoritative Global Cell.");
                result.Add(new HanBuildableFacilityModelPlacement(
                    facility.ModelId, new WorldMapCellId(facility.CellId64),
                    facility.FacilityId, facility.RotationDegrees));
            }
            return result;
        }
    }

    public static class LuoyangLowFrequencyDefenseProductionPreviewPlan
    {
        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid,
            LuoyangLowFrequencyDefenseProductionPlan plan)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var result = new List<HanBuildableFacilityModelPlacement>(
                plan.Facilities.Count);
            foreach (var facility in plan.Facilities)
            {
                if (!grid.Contains(facility.GridRow, facility.GridColumn) ||
                    grid.ToCellId(facility.GridRow, facility.GridColumn).Value !=
                    facility.CellId64)
                    throw new InvalidOperationException(
                        "Luoyang defense profile does not match its authoritative Global Cell.");
                result.Add(new HanBuildableFacilityModelPlacement(
                    facility.ModelId, new WorldMapCellId(facility.CellId64),
                    facility.FacilityId, facility.RotationDegrees));
            }
            return result;
        }
    }

    public static class LuoyangResourceAgricultureProductionPreviewPlan
    {
        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid,
            LuoyangResourceAgricultureProductionPlan plan)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var result = new List<HanBuildableFacilityModelPlacement>(
                plan.Facilities.Count);
            foreach (var facility in plan.Facilities)
            {
                if (!grid.Contains(facility.GridRow, facility.GridColumn) ||
                    grid.ToCellId(facility.GridRow, facility.GridColumn).Value !=
                    facility.CellId64)
                    throw new InvalidOperationException(
                        "Luoyang resource/agriculture profile does not match its authoritative Global Cell.");
                result.Add(new HanBuildableFacilityModelPlacement(
                    facility.ModelId, new WorldMapCellId(facility.CellId64),
                    facility.FacilityId, 0f));
            }
            return result;
        }
    }

    public static class LuoyangFinalCivicRitualMedicalProductionPreviewPlan
    {
        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid,
            LuoyangFinalCivicRitualMedicalProductionPlan plan)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var result = new List<HanBuildableFacilityModelPlacement>(
                plan.Facilities.Count);
            foreach (var facility in plan.Facilities)
            {
                if (!grid.Contains(facility.GridRow, facility.GridColumn) ||
                    grid.ToCellId(facility.GridRow, facility.GridColumn).Value !=
                    facility.CellId64)
                    throw new InvalidOperationException(
                        "Luoyang final civic profile does not match its authoritative Global Cell.");
                result.Add(new HanBuildableFacilityModelPlacement(
                    facility.ModelId, new WorldMapCellId(facility.CellId64),
                    facility.FacilityId, 0f));
            }
            return result;
        }
    }

    public static class LuoyangFinalAssetReviewPreviewPlan
    {
        public const int Columns = 6;
        public const int RowSpacing = 2;
        public const int ColumnSpacing = 2;
        public const int BoardCenterRow = 1243;
        public const int BoardCenterColumn = 2043;

        private static readonly IReadOnlyList<string> PriorityOrder = new[]
        {
            LuoyangFinalAssetReviewIds.PriorityP0,
            LuoyangFinalAssetReviewIds.PriorityP1,
            LuoyangFinalAssetReviewIds.PriorityP2,
            LuoyangFinalAssetReviewIds.PriorityP3
        };

        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, LuoyangFinalAssetReviewPlan plan,
            int centerRow, int centerColumn)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var result = new List<HanBuildableFacilityModelPlacement>(
                plan.Catalog.Items.Count);
            var rowOffset = 0;
            foreach (var priorityId in PriorityOrder)
            {
                var items = plan.Catalog.Items.Where(item => string.Equals(
                        item.PriorityId, priorityId, StringComparison.Ordinal))
                    .OrderBy(item => item.ReviewOrder).ToArray();
                for (var index = 0; index < items.Length; index++)
                {
                    var item = items[index];
                    var row = centerRow - 9 +
                              (rowOffset + index / Columns) * RowSpacing;
                    var column = centerColumn - 5 +
                                 (index % Columns) * ColumnSpacing;
                    if (!grid.Contains(row, column))
                        throw new ArgumentOutOfRangeException(nameof(centerRow),
                            "Luoyang final-asset review board exceeds the Global Cell grid.");
                    result.Add(new HanBuildableFacilityModelPlacement(
                        item.ModelId, grid.ToCellId(row, column),
                        item.RepresentativeFacilityId,
                        item.ReviewOrder % 4 * 90f));
                }
                rowOffset += (items.Length + Columns - 1) / Columns;
            }
            if (result.Count != LuoyangFinalAssetReviewIds.AssetItemCount)
                throw new InvalidOperationException(
                    "Luoyang final-asset review board is incomplete.");
            return result;
        }
    }

    public static class LuoyangP0FinalAssetVerticalSlicePreviewPlan
    {
        public const int BoardCenterRow = 1243;
        public const int BoardCenterColumn = 2043;
        public const int PlacementCount = 4;

        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, LuoyangP0FinalAssetVerticalSlicePlan plan)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var ordered = LuoyangP0FinalAssetVerticalSliceIds.FacilityIds
                .Select(id => plan.ProfilesByFacilityId[id]).ToArray();
            var result = new List<HanBuildableFacilityModelPlacement>(
                PlacementCount);
            for (var index = 0; index < ordered.Length; index++)
            {
                var row = BoardCenterRow + (index / 2) * 6 - 3;
                var column = BoardCenterColumn + (index % 2) * 6 - 3;
                if (!grid.Contains(row, column))
                    throw new InvalidOperationException(
                        "Luoyang P0 vertical-slice review board exceeds the Global Cell grid.");
                var profile = ordered[index];
                var rotation = string.Equals(profile.FacilityId,
                    LuoyangGateIdentityKitIds.Guangyangmen,
                    StringComparison.Ordinal) ? 90f : 0f;
                result.Add(new HanBuildableFacilityModelPlacement(profile.ModelId,
                    grid.ToCellId(row, column), profile.FacilityId, rotation));
            }
            return result;
        }
    }

    public static class LuoyangP0LandmarkSecondBatchPreviewPlan
    {
        public const int BoardCenterRow = 1243;
        public const int BoardCenterColumn = 2043;
        public const int PlacementCount = 4;

        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, LuoyangP0LandmarkSecondBatchPlan plan)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var ordered = LuoyangP0LandmarkSecondBatchIds.FacilityIds
                .Select(id => plan.ProfilesByFacilityId[id]).ToArray();
            var result = new List<HanBuildableFacilityModelPlacement>(
                PlacementCount);
            for (var index = 0; index < ordered.Length; index++)
            {
                var row = BoardCenterRow + (index / 2) * 6 - 3;
                var column = BoardCenterColumn + (index % 2) * 6 - 3;
                if (!grid.Contains(row, column))
                    throw new InvalidOperationException(
                        "Luoyang P0 landmark second-batch board exceeds the Global Cell grid.");
                var profile = ordered[index];
                result.Add(new HanBuildableFacilityModelPlacement(
                    profile.ModelId, grid.ToCellId(row, column),
                    profile.FacilityId, 0f));
            }
            return result;
        }
    }

    public static class LuoyangP0LandmarkThirdBatchPreviewPlan
    {
        public const int BoardCenterRow = 1243;
        public const int BoardCenterColumn = 2043;
        public const int PlacementCount = 4;

        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, LuoyangP0LandmarkThirdBatchPlan plan)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var ordered = LuoyangP0LandmarkThirdBatchIds.FacilityIds
                .Select(id => plan.ProfilesByFacilityId[id]).ToArray();
            var result = new List<HanBuildableFacilityModelPlacement>(
                PlacementCount);
            for (var index = 0; index < ordered.Length; index++)
            {
                var row = BoardCenterRow + (index / 2) * 6 - 3;
                var column = BoardCenterColumn + (index % 2) * 6 - 3;
                if (!grid.Contains(row, column))
                    throw new InvalidOperationException(
                        "Luoyang P0 landmark third-batch board exceeds the Global Cell grid.");
                var profile = ordered[index];
                result.Add(new HanBuildableFacilityModelPlacement(
                    profile.ModelId, grid.ToCellId(row, column),
                    profile.FacilityId, 0f));
            }
            return result;
        }
    }

    public static class LuoyangP0NamedGateFourthBatchPreviewPlan
    {
        public const int BoardCenterRow = 1243;
        public const int BoardCenterColumn = 2043;
        public const int PlacementCount = 4;

        public static IReadOnlyList<HanBuildableFacilityModelPlacement> Create(
            CellGridIndex grid, LuoyangP0NamedGateFourthBatchPlan plan)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var ordered = LuoyangP0NamedGateFourthBatchIds.FacilityIds
                .Select(id => plan.ProfilesByFacilityId[id]).ToArray();
            var result = new List<HanBuildableFacilityModelPlacement>(
                PlacementCount);
            for (var index = 0; index < ordered.Length; index++)
            {
                var row = BoardCenterRow + (index / 2) * 6 - 3;
                var column = BoardCenterColumn + (index % 2) * 6 - 3;
                if (!grid.Contains(row, column))
                    throw new InvalidOperationException(
                        "Luoyang P0 named-gate fourth-batch board exceeds the Global Cell grid.");
                var profile = ordered[index];
                var rotation = LuoyangGateIdentityKitIds.RotationForFacing(
                    LuoyangGateIdentityKitIds.VisualFacings[
                        profile.FacilityId]);
                result.Add(new HanBuildableFacilityModelPlacement(
                    profile.ModelId, grid.ToCellId(row, column),
                    profile.FacilityId, rotation));
            }
            return result;
        }
    }

    public sealed class HanBuildableFacilityBatchModule
    {
        public HanBuildableFacilityBatchModule(string moduleId, string primitiveId,
            string materialId, Mesh mesh, Material material,
            Matrix4x4 localMatrix)
        {
            ModuleId = moduleId;
            PrimitiveId = primitiveId;
            MaterialId = materialId;
            Mesh = mesh;
            Material = material;
            LocalMatrix = localMatrix;
        }

        public string ModuleId { get; }
        public string PrimitiveId { get; }
        public string MaterialId { get; }
        public Mesh Mesh { get; }
        public Material Material { get; }
        public Matrix4x4 LocalMatrix { get; }
    }

    public sealed class HanBuildableFacilityModelFactory : IDisposable
    {
        private readonly Dictionary<string, HanBuildableFacilityModelDefinition> _models =
            new Dictionary<string, HanBuildableFacilityModelDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, Material> _materials =
            new Dictionary<string, Material>(StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangProductionBuildingProfile>
            _productionProfiles =
                new Dictionary<string, LuoyangProductionBuildingProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangHistoricalLandmarkProfile>
            _historicalLandmarkProfiles =
                new Dictionary<string, LuoyangHistoricalLandmarkProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangGateIdentityProfile>
            _gateIdentityProfiles =
                new Dictionary<string, LuoyangGateIdentityProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangMediumFrequencyUrbanFabricProfile>
            _urbanFabricProfiles =
                new Dictionary<string, LuoyangMediumFrequencyUrbanFabricProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangInfrastructureProductionProfile>
            _infrastructureProfiles =
                new Dictionary<string, LuoyangInfrastructureProductionProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string,
                LuoyangLowFrequencyDefenseProductionProfile>
            _defenseProfilesByFacilityId =
                new Dictionary<string,
                    LuoyangLowFrequencyDefenseProductionProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string,
                LuoyangLowFrequencyDefenseProductionProfile>
            _proceduralDefenseProfilesByModelId =
                new Dictionary<string,
                    LuoyangLowFrequencyDefenseProductionProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string,
                LuoyangResourceAgricultureProductionProfile>
            _resourceProfilesByFacilityId =
                new Dictionary<string,
                    LuoyangResourceAgricultureProductionProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string,
                LuoyangResourceAgricultureProductionProfile>
            _resourceProfilesByDefinitionId =
                new Dictionary<string,
                    LuoyangResourceAgricultureProductionProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string,
                LuoyangResourceAgricultureProductionProfile>
            _uniqueResourceProfilesByModelId =
                new Dictionary<string,
                    LuoyangResourceAgricultureProductionProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string,
                LuoyangFinalCivicRitualMedicalProductionProfile>
            _finalCivicProfilesByFacilityId =
                new Dictionary<string,
                    LuoyangFinalCivicRitualMedicalProductionProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string,
                LuoyangFinalCivicRitualMedicalProductionProfile>
            _proceduralFinalCivicProfilesByDefinitionId =
                new Dictionary<string,
                    LuoyangFinalCivicRitualMedicalProductionProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string,
                LuoyangFinalCivicRitualMedicalProductionProfile>
            _uniqueProceduralFinalCivicProfilesByModelId =
                new Dictionary<string,
                    LuoyangFinalCivicRitualMedicalProductionProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangP0FinalAssetProfile>
            _p0FinalAssetProfilesByFacilityId =
                new Dictionary<string, LuoyangP0FinalAssetProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangP0FinalAssetProfile>
            _p0LandmarkSecondBatchProfilesByFacilityId =
                new Dictionary<string, LuoyangP0FinalAssetProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangP0FinalAssetProfile>
            _p0LandmarkThirdBatchProfilesByFacilityId =
                new Dictionary<string, LuoyangP0FinalAssetProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string, LuoyangP0FinalAssetProfile>
            _p0NamedGateFourthBatchProfilesByFacilityId =
                new Dictionary<string, LuoyangP0FinalAssetProfile>(
                    StringComparer.Ordinal);
        private readonly Dictionary<string,
                LuoyangRemainingFinalAssetProfile>
            _remainingFinalAssetProfilesByAssetVariantId =
                new Dictionary<string, LuoyangRemainingFinalAssetProfile>(
                    StringComparer.Ordinal);
        private readonly Func<string, GameObject> _artistPrefabLoader;
        private readonly HanProductionBuildingMeshLibrary _productionMeshes;
        private readonly Dictionary<string, Mesh> _batchPrimitiveMeshes =
            new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private bool _disposed;

        public HanBuildableFacilityModelFactory(HanBuildableFacilityModelCatalog catalog)
            : this(catalog, null, null, null, null, null)
        {
        }

        public HanBuildableFacilityModelFactory(HanBuildableFacilityModelCatalog catalog,
            LuoyangProductionBuildingKitCatalog production)
            : this(catalog, production, null, null, null, null)
        {
        }

        public HanBuildableFacilityModelFactory(HanBuildableFacilityModelCatalog catalog,
            LuoyangProductionBuildingKitCatalog production,
            LuoyangHistoricalLandmarkKitCatalog landmarks)
            : this(catalog, production, landmarks, null, null, null)
        {
        }

        public HanBuildableFacilityModelFactory(HanBuildableFacilityModelCatalog catalog,
            LuoyangProductionBuildingKitCatalog production,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates)
            : this(catalog, production, landmarks, gates, null, null)
        {
        }

        public HanBuildableFacilityModelFactory(HanBuildableFacilityModelCatalog catalog,
            LuoyangProductionBuildingKitCatalog production,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangMediumFrequencyUrbanFabricKitCatalog urbanFabric)
            : this(catalog, production, landmarks, gates, urbanFabric, null)
        {
        }

        public HanBuildableFacilityModelFactory(HanBuildableFacilityModelCatalog catalog,
            LuoyangProductionBuildingKitCatalog production,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangMediumFrequencyUrbanFabricKitCatalog urbanFabric,
            LuoyangInfrastructureProductionKitCatalog infrastructure)
            : this(catalog, production, landmarks, gates, urbanFabric,
                infrastructure, null)
        {
        }

        public HanBuildableFacilityModelFactory(HanBuildableFacilityModelCatalog catalog,
            LuoyangProductionBuildingKitCatalog production,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangMediumFrequencyUrbanFabricKitCatalog urbanFabric,
            LuoyangInfrastructureProductionKitCatalog infrastructure,
            LuoyangLowFrequencyDefenseProductionKitCatalog defense)
            : this(catalog, production, landmarks, gates, urbanFabric,
                infrastructure, defense, null)
        {
        }

        public HanBuildableFacilityModelFactory(HanBuildableFacilityModelCatalog catalog,
            LuoyangProductionBuildingKitCatalog production,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangMediumFrequencyUrbanFabricKitCatalog urbanFabric,
            LuoyangInfrastructureProductionKitCatalog infrastructure,
            LuoyangLowFrequencyDefenseProductionKitCatalog defense,
            LuoyangResourceAgricultureProductionKitCatalog resourceAgriculture)
            : this(catalog, production, landmarks, gates, urbanFabric,
                infrastructure, defense, resourceAgriculture, null)
        {
        }

        public HanBuildableFacilityModelFactory(HanBuildableFacilityModelCatalog catalog,
            LuoyangProductionBuildingKitCatalog production,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangMediumFrequencyUrbanFabricKitCatalog urbanFabric,
            LuoyangInfrastructureProductionKitCatalog infrastructure,
            LuoyangLowFrequencyDefenseProductionKitCatalog defense,
            LuoyangResourceAgricultureProductionKitCatalog resourceAgriculture,
            LuoyangFinalCivicRitualMedicalProductionKitCatalog finalCivic)
            : this(catalog, production, landmarks, gates, urbanFabric,
                infrastructure, defense, resourceAgriculture, finalCivic, null,
                null)
        {
        }

        public HanBuildableFacilityModelFactory(HanBuildableFacilityModelCatalog catalog,
            LuoyangProductionBuildingKitCatalog production,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangMediumFrequencyUrbanFabricKitCatalog urbanFabric,
            LuoyangInfrastructureProductionKitCatalog infrastructure,
            LuoyangLowFrequencyDefenseProductionKitCatalog defense,
            LuoyangResourceAgricultureProductionKitCatalog resourceAgriculture,
            LuoyangFinalCivicRitualMedicalProductionKitCatalog finalCivic,
            LuoyangP0FinalAssetVerticalSlicePlan p0FinalAssetPlan,
            Func<string, GameObject> artistPrefabLoader,
            LuoyangP0LandmarkSecondBatchPlan p0LandmarkSecondBatchPlan = null,
            LuoyangP0LandmarkThirdBatchPlan p0LandmarkThirdBatchPlan = null,
            LuoyangP0NamedGateFourthBatchPlan
                p0NamedGateFourthBatchPlan = null,
            LuoyangRemainingFinalAssetPlan remainingFinalAssetPlan = null)
        {
            HanBuildableFacilityModelCatalogRules.Validate(catalog);
            foreach (var model in catalog.Models) _models.Add(model.ModelId, model);
            foreach (var definition in catalog.Materials)
                _materials.Add(definition.MaterialId, CreateMaterial(definition));
            if (production != null)
            {
                LuoyangProductionBuildingKitRules.Validate(production, catalog);
                foreach (var profile in production.Profiles)
                    _productionProfiles.Add(profile.ModelId, profile);
            }
            if (landmarks != null)
            {
                LuoyangHistoricalLandmarkKitRules.Validate(landmarks, catalog);
                foreach (var profile in landmarks.Profiles)
                    _historicalLandmarkProfiles.Add(profile.FacilityId, profile);
            }
            if (gates != null)
            {
                LuoyangGateIdentityKitRules.Validate(gates, catalog);
                foreach (var profile in gates.Profiles)
                    _gateIdentityProfiles.Add(profile.FacilityId, profile);
            }
            if (urbanFabric != null)
            {
                LuoyangMediumFrequencyUrbanFabricKitRules.Validate(urbanFabric,
                    catalog);
                foreach (var profile in urbanFabric.Profiles)
                    _urbanFabricProfiles.Add(profile.ModelId, profile);
            }
            if (infrastructure != null)
            {
                LuoyangInfrastructureProductionKitRules.Validate(infrastructure,
                    catalog);
                foreach (var profile in infrastructure.Profiles)
                    _infrastructureProfiles.Add(profile.ModelId, profile);
            }
            if (defense != null)
            {
                LuoyangLowFrequencyDefenseProductionKitRules.Validate(defense,
                    catalog, gates);
                foreach (var profile in defense.Profiles)
                {
                    foreach (var facilityId in profile.FacilityIds)
                        _defenseProfilesByFacilityId.Add(facilityId, profile);
                    if (string.Equals(profile.ProductionModeId,
                            LuoyangLowFrequencyDefenseProductionKitIds
                                .ProceduralModeId, StringComparison.Ordinal))
                        _proceduralDefenseProfilesByModelId.Add(profile.ModelId,
                            profile);
                }
            }
            if (resourceAgriculture != null)
            {
                LuoyangResourceAgricultureProductionKitRules.Validate(
                    resourceAgriculture, catalog);
                foreach (var profile in resourceAgriculture.Profiles)
                {
                    _resourceProfilesByDefinitionId.Add(
                        profile.FacilityDefinitionId, profile);
                    foreach (var facilityId in profile.FacilityIds)
                        _resourceProfilesByFacilityId.Add(facilityId, profile);
                }
                foreach (var group in resourceAgriculture.Profiles.GroupBy(
                             item => item.ModelId, StringComparer.Ordinal))
                    if (group.Count() == 1)
                        _uniqueResourceProfilesByModelId.Add(group.Key,
                            group.Single());
            }
            if (finalCivic != null)
            {
                if (landmarks == null)
                    throw new InvalidOperationException(
                        "Final civic production requires the Luoyang landmark kit.");
                LuoyangFinalCivicRitualMedicalProductionKitRules.Validate(
                    finalCivic, catalog, landmarks);
                foreach (var profile in finalCivic.Profiles)
                {
                    foreach (var facilityId in profile.FacilityIds)
                        _finalCivicProfilesByFacilityId.Add(facilityId, profile);
                    if (string.Equals(profile.ProductionModeId,
                            LuoyangFinalCivicRitualMedicalProductionKitIds
                                .ProceduralModeId, StringComparison.Ordinal))
                        _proceduralFinalCivicProfilesByDefinitionId.Add(
                            profile.FacilityDefinitionId, profile);
                }
                foreach (var group in finalCivic.Profiles.GroupBy(
                             item => item.ModelId, StringComparer.Ordinal))
                    if (group.Count() == 1 && string.Equals(
                            group.Single().ProductionModeId,
                            LuoyangFinalCivicRitualMedicalProductionKitIds
                                .ProceduralModeId, StringComparison.Ordinal))
                        _uniqueProceduralFinalCivicProfilesByModelId.Add(
                            group.Key, group.Single());
            }
            if (p0FinalAssetPlan != null)
            {
                foreach (var definition in p0FinalAssetPlan.Catalog.Materials)
                {
                    if (_materials.ContainsKey(definition.MaterialId))
                        throw new InvalidOperationException(
                            "Duplicate Luoyang P0 final-asset material id: " +
                            definition.MaterialId);
                    _materials.Add(definition.MaterialId,
                        CreateMaterial(definition));
                }
                foreach (var pair in p0FinalAssetPlan.ProfilesByFacilityId)
                    _p0FinalAssetProfilesByFacilityId.Add(pair.Key, pair.Value);
            }
            if (p0LandmarkSecondBatchPlan != null)
            {
                foreach (var pair in p0LandmarkSecondBatchPlan
                             .ProfilesByFacilityId)
                {
                    if (_p0FinalAssetProfilesByFacilityId.ContainsKey(pair.Key))
                        throw new InvalidOperationException(
                            "Luoyang P0 second-batch Facility overlaps the activated first batch.");
                    _p0LandmarkSecondBatchProfilesByFacilityId.Add(pair.Key,
                        pair.Value);
                }
            }
            if (p0LandmarkThirdBatchPlan != null)
            {
                foreach (var pair in p0LandmarkThirdBatchPlan
                             .ProfilesByFacilityId)
                {
                    if (_p0FinalAssetProfilesByFacilityId.ContainsKey(pair.Key) ||
                        _p0LandmarkSecondBatchProfilesByFacilityId.ContainsKey(
                            pair.Key))
                        throw new InvalidOperationException(
                            "Luoyang P0 third-batch Facility overlaps an earlier activated batch.");
                    _p0LandmarkThirdBatchProfilesByFacilityId.Add(pair.Key,
                        pair.Value);
                }
            }
            if (p0NamedGateFourthBatchPlan != null)
            {
                foreach (var pair in p0NamedGateFourthBatchPlan
                             .ProfilesByFacilityId)
                {
                    if (_p0FinalAssetProfilesByFacilityId.ContainsKey(pair.Key) ||
                        _p0LandmarkSecondBatchProfilesByFacilityId.ContainsKey(
                            pair.Key) ||
                        _p0LandmarkThirdBatchProfilesByFacilityId.ContainsKey(
                            pair.Key))
                        throw new InvalidOperationException(
                            "Luoyang P0 named-gate fourth-batch Facility overlaps an earlier activated batch.");
                    _p0NamedGateFourthBatchProfilesByFacilityId.Add(pair.Key,
                        pair.Value);
                }
            }
            if (remainingFinalAssetPlan != null)
            {
                var activatedAssetIds = new HashSet<string>(
                    _p0FinalAssetProfilesByFacilityId.Values.Select(item =>
                        item.AssetVariantId), StringComparer.Ordinal);
                activatedAssetIds.UnionWith(
                    _p0LandmarkSecondBatchProfilesByFacilityId.Values.Select(
                        item => item.AssetVariantId));
                activatedAssetIds.UnionWith(
                    _p0LandmarkThirdBatchProfilesByFacilityId.Values.Select(
                        item => item.AssetVariantId));
                activatedAssetIds.UnionWith(
                    _p0NamedGateFourthBatchProfilesByFacilityId.Values.Select(
                        item => item.AssetVariantId));
                foreach (var pair in remainingFinalAssetPlan
                             .ProfilesByAssetVariantId)
                {
                    if (activatedAssetIds.Contains(pair.Key))
                        throw new InvalidOperationException(
                            "A Luoyang remaining final asset overlaps an already activated slot: " +
                            pair.Key + ".");
                    _remainingFinalAssetProfilesByAssetVariantId.Add(pair.Key,
                        pair.Value);
                }
            }
            _artistPrefabLoader = artistPrefabLoader ??
                                  (path => Resources.Load<GameObject>(path));
            if (production != null || landmarks != null || gates != null ||
                urbanFabric != null || infrastructure != null || defense != null ||
                resourceAgriculture != null || finalCivic != null ||
                p0FinalAssetPlan != null || p0LandmarkSecondBatchPlan != null ||
                p0LandmarkThirdBatchPlan != null ||
                p0NamedGateFourthBatchPlan != null ||
                remainingFinalAssetPlan != null)
                _productionMeshes = new HanProductionBuildingMeshLibrary();
        }

        public int MaterialCount => _materials.Count;
        public int ModelCount => _models.Count;
        public int ProductionProfileCount => _productionProfiles.Count;
        public int ProductionMeshCount => _productionMeshes?.Count ?? 0;
        public int HistoricalLandmarkProfileCount =>
            _historicalLandmarkProfiles.Count;
        public int GateIdentityProfileCount => _gateIdentityProfiles.Count;
        public int MediumFrequencyUrbanFabricProfileCount =>
            _urbanFabricProfiles.Count;
        public int InfrastructureProductionProfileCount =>
            _infrastructureProfiles.Count;
        public int LowFrequencyDefenseProductionProfileCount =>
            _defenseProfilesByFacilityId.Values.Select(item => item.ProfileId)
                .Distinct(StringComparer.Ordinal).Count();
        public int ResourceAgricultureProductionProfileCount =>
            _resourceProfilesByDefinitionId.Count;
        public int FinalCivicProductionProfileCount =>
            _finalCivicProfilesByFacilityId.Values.Select(item => item.ProfileId)
                .Distinct(StringComparer.Ordinal).Count();
        public int P0FinalAssetVerticalSliceProfileCount =>
            _p0FinalAssetProfilesByFacilityId.Count;
        public int P0LandmarkSecondBatchProfileCount =>
            _p0LandmarkSecondBatchProfilesByFacilityId.Count;
        public int P0LandmarkThirdBatchProfileCount =>
            _p0LandmarkThirdBatchProfilesByFacilityId.Count;
        public int P0NamedGateFourthBatchProfileCount =>
            _p0NamedGateFourthBatchProfilesByFacilityId.Count;
        public int RemainingFinalAssetProfileCount =>
            _remainingFinalAssetProfilesByAssetVariantId.Count;

        public HanBuildableFacilityModelDefinition GetModel(string modelId)
        {
            ThrowIfDisposed();
            if (!_models.TryGetValue(modelId ?? string.Empty, out var model))
                throw new KeyNotFoundException("Unknown Han buildable Facility model: " +
                                               modelId);
            return model;
        }

        private bool TryResolveP0FinalAssetProfile(string runtimeBindingId,
            out LuoyangP0FinalAssetProfile profile,
            out bool isLandmarkSecondBatch,
            out bool isLandmarkThirdBatch,
            out bool isNamedGateFourthBatch)
        {
            if (_p0FinalAssetProfilesByFacilityId.TryGetValue(
                    runtimeBindingId, out profile))
            {
                isLandmarkSecondBatch = false;
                isLandmarkThirdBatch = false;
                isNamedGateFourthBatch = false;
                return true;
            }
            isLandmarkSecondBatch =
                _p0LandmarkSecondBatchProfilesByFacilityId.TryGetValue(
                    runtimeBindingId, out profile);
            if (isLandmarkSecondBatch)
            {
                isLandmarkThirdBatch = false;
                isNamedGateFourthBatch = false;
                return true;
            }
            isLandmarkThirdBatch =
                _p0LandmarkThirdBatchProfilesByFacilityId.TryGetValue(
                    runtimeBindingId, out profile);
            if (isLandmarkThirdBatch)
            {
                isNamedGateFourthBatch = false;
                return true;
            }
            isNamedGateFourthBatch =
                _p0NamedGateFourthBatchProfilesByFacilityId.TryGetValue(
                    runtimeBindingId, out profile);
            return isNamedGateFourthBatch;
        }

        public IReadOnlyList<HanBuildableFacilityBatchModule> GetWorldBatchModules(
            string modelId, string runtimeBindingId)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(runtimeBindingId))
                throw new ArgumentException("Runtime binding id is required.",
                    nameof(runtimeBindingId));
            var definition = GetModel(modelId);
            TryResolveP0FinalAssetProfile(runtimeBindingId,
                out var p0FinalAsset, out _, out _, out _);
            _gateIdentityProfiles.TryGetValue(runtimeBindingId, out var gate);
            _historicalLandmarkProfiles.TryGetValue(runtimeBindingId,
                out var landmark);
            _urbanFabricProfiles.TryGetValue(modelId, out var urbanFabric);
            _infrastructureProfiles.TryGetValue(modelId,
                out var infrastructure);
            _defenseProfilesByFacilityId.TryGetValue(runtimeBindingId,
                out var defense);
            if (defense == null)
                _proceduralDefenseProfilesByModelId.TryGetValue(modelId,
                    out defense);
            var resourceAgriculture = ResolveResourceAgricultureProfile(modelId,
                runtimeBindingId);
            var finalCivic = ResolveFinalCivicProfile(modelId,
                runtimeBindingId);
            if (p0FinalAsset != null && !string.Equals(p0FinalAsset.ModelId,
                    modelId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang P0 final-asset batch binding does not match its frozen model.");
            if (gate != null && !string.Equals(gate.BaseModelId, modelId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang gate batch binding does not match its base model.");
            if (landmark != null && !string.Equals(landmark.BaseModelId, modelId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang landmark batch binding does not match its base model.");
            if (defense != null && !string.Equals(defense.ModelId, modelId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang defense batch binding does not match its base model.");
            if (defense != null && string.Equals(defense.ProductionModeId,
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .IdentityReuseModeId, StringComparison.Ordinal) &&
                gate == null)
                throw new InvalidOperationException(
                    "Luoyang defense identity reuse has no gate identity binding.");
            if (resourceAgriculture != null &&
                !string.Equals(resourceAgriculture.ModelId, modelId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang resource/agriculture batch binding does not match its base model.");
            if (finalCivic != null &&
                !string.Equals(finalCivic.ModelId, modelId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang final civic batch binding does not match its base model.");
            if (finalCivic != null && string.Equals(
                    finalCivic.ProductionModeId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .IdentityReuseModeId, StringComparison.Ordinal) &&
                landmark == null)
                throw new InvalidOperationException(
                    "Luoyang final civic identity reuse has no landmark binding.");

            IReadOnlyList<HanBuildableFacilityModuleDefinition> modules;
            HashSet<string> included = null;
            IReadOnlyDictionary<string, string> overrides = null;
            if (p0FinalAsset != null)
            {
                modules = p0FinalAsset.Modules;
                included = new HashSet<string>(p0FinalAsset.Lod2ModuleIds,
                    StringComparer.Ordinal);
            }
            else if (gate != null)
            {
                modules = gate.Modules;
                included = new HashSet<string>(gate.Lod2ModuleIds,
                    StringComparer.Ordinal);
            }
            else if (landmark != null)
            {
                modules = landmark.Modules;
                included = new HashSet<string>(landmark.Lod2ModuleIds,
                    StringComparer.Ordinal);
            }
            else if (urbanFabric != null)
            {
                modules = urbanFabric.Modules;
                included = new HashSet<string>(urbanFabric.Lod2ModuleIds,
                    StringComparer.Ordinal);
            }
            else if (infrastructure != null)
            {
                modules = infrastructure.Modules;
                included = new HashSet<string>(infrastructure.Lod2ModuleIds,
                    StringComparer.Ordinal);
            }
            else if (defense != null)
            {
                modules = defense.Modules;
                included = new HashSet<string>(defense.Lod2ModuleIds,
                    StringComparer.Ordinal);
            }
            else if (resourceAgriculture != null)
            {
                modules = resourceAgriculture.Modules;
                included = new HashSet<string>(
                    resourceAgriculture.Lod2ModuleIds, StringComparer.Ordinal);
            }
            else if (finalCivic != null)
            {
                modules = finalCivic.Modules;
                included = new HashSet<string>(finalCivic.Lod2ModuleIds,
                    StringComparer.Ordinal);
            }
            else if (_productionProfiles.TryGetValue(modelId, out var production))
            {
                modules = definition.Modules;
                included = new HashSet<string>(production.Lod2ModuleIds,
                    StringComparer.Ordinal);
                overrides = production.PrimitiveOverrides.ToDictionary(
                    item => item.ModuleId, item => item.PrimitiveId,
                    StringComparer.Ordinal);
            }
            else
                modules = definition.Modules;
            return CreateBatchModules(modules, included, overrides);
        }

        public HanBuildableFacilityModelInstance Create(string modelId, Transform parent,
            string runtimeBindingId, ulong cellId64, bool previewOnly)
        {
            ThrowIfDisposed();
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrWhiteSpace(runtimeBindingId))
                throw new ArgumentException("Runtime binding id is required.",
                    nameof(runtimeBindingId));

            var definition = GetModel(modelId);
            TryResolveP0FinalAssetProfile(runtimeBindingId,
                out var p0FinalAsset, out var isP0LandmarkSecondBatch,
                out var isP0LandmarkThirdBatch,
                out var isP0NamedGateFourthBatch);
            _gateIdentityProfiles.TryGetValue(runtimeBindingId, out var gate);
            _historicalLandmarkProfiles.TryGetValue(runtimeBindingId,
                out var landmark);
            _urbanFabricProfiles.TryGetValue(modelId, out var urbanFabric);
            _infrastructureProfiles.TryGetValue(modelId,
                out var infrastructure);
            _defenseProfilesByFacilityId.TryGetValue(runtimeBindingId,
                out var defense);
            if (defense == null)
                _proceduralDefenseProfilesByModelId.TryGetValue(modelId,
                    out defense);
            var resourceAgriculture = ResolveResourceAgricultureProfile(modelId,
                runtimeBindingId);
            var finalCivic = ResolveFinalCivicProfile(modelId,
                runtimeBindingId);
            _productionProfiles.TryGetValue(modelId, out var production);
            if (p0FinalAsset != null && !string.Equals(p0FinalAsset.ModelId,
                    modelId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang P0 final-asset runtime binding does not match its frozen model.");
            if (gate != null && !string.Equals(gate.BaseModelId, modelId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang gate runtime binding does not match its base model.");
            if (landmark != null && !string.Equals(landmark.BaseModelId, modelId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang landmark runtime binding does not match its base model.");
            if (defense != null && !string.Equals(defense.ModelId, modelId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang defense runtime binding does not match its base model.");
            if (defense != null && string.Equals(defense.ProductionModeId,
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .IdentityReuseModeId, StringComparison.Ordinal) &&
                gate == null)
                throw new InvalidOperationException(
                    "Luoyang defense identity reuse has no gate identity binding.");
            if (resourceAgriculture != null &&
                !string.Equals(resourceAgriculture.ModelId, modelId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang resource/agriculture runtime binding does not match its base model.");
            if (finalCivic != null &&
                !string.Equals(finalCivic.ModelId, modelId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang final civic runtime binding does not match its base model.");
            if (finalCivic != null && string.Equals(
                    finalCivic.ProductionModeId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .IdentityReuseModeId, StringComparison.Ordinal) &&
                landmark == null)
                throw new InvalidOperationException(
                    "Luoyang final civic identity reuse has no landmark binding.");
            var displayName = p0FinalAsset?.DisplayName ?? gate?.DisplayName ??
                              landmark?.DisplayName ??
                              urbanFabric?.DisplayName ??
                              infrastructure?.DisplayName ??
                              defense?.DisplayName ??
                              resourceAgriculture?.DisplayName ??
                              finalCivic?.DisplayName ??
                              definition.DisplayName;
            var assetId = p0FinalAsset?.AssetVariantId ??
                          gate?.AssetVariantId ?? landmark?.AssetVariantId ??
                          urbanFabric?.AssetVariantId ??
                          infrastructure?.AssetVariantId ??
                          defense?.AssetVariantId ??
                          resourceAgriculture?.AssetVariantId ??
                          finalCivic?.AssetVariantId ??
                          production?.AssetVariantId ??
                          definition.AssetId;
            _remainingFinalAssetProfilesByAssetVariantId.TryGetValue(assetId,
                out var remainingFinalAsset);
            if (remainingFinalAsset != null)
                displayName = remainingFinalAsset.DisplayName;
            var root = new GameObject(displayName + " [" + assetId + "]");
            root.transform.SetParent(parent, false);
            var instance = root.AddComponent<HanBuildableFacilityModelInstance>();
            instance.ModelId = definition.ModelId;
            instance.AssetId = assetId;
            instance.RuntimeBindingId = runtimeBindingId;
            instance.CellId64 = cellId64;
            instance.PreviewOnly = previewOnly;

            if (p0FinalAsset != null)
            {
                if (gate != null)
                {
                    ConfigureGateIdentityInstance(instance, gate);
                    if (defense != null)
                        ConfigureDefenseInstance(instance, defense, gate);
                }
                if (landmark != null)
                {
                    ConfigureHistoricalLandmarkInstance(instance, landmark);
                    if (finalCivic != null)
                        ConfigureFinalCivicInstance(instance, finalCivic,
                            landmark);
                }
                ConfigureP0FinalAssetInstance(instance, p0FinalAsset);
                if (isP0LandmarkSecondBatch)
                {
                    instance.P0LandmarkSecondBatchReady = true;
                    instance.P0LandmarkSecondBatchStatusId =
                        LuoyangP0LandmarkSecondBatchIds.StatusId;
                }
                if (isP0LandmarkThirdBatch)
                {
                    instance.P0LandmarkThirdBatchReady = true;
                    instance.P0LandmarkThirdBatchStatusId =
                        LuoyangP0LandmarkThirdBatchIds.StatusId;
                }
                if (isP0NamedGateFourthBatch)
                {
                    instance.P0NamedGateFourthBatchReady = true;
                    instance.P0NamedGateFourthBatchStatusId =
                        LuoyangP0NamedGateFourthBatchIds.StatusId;
                }
                if (TryCreateArtistPrefabHierarchy(p0FinalAsset,
                        root.transform))
                {
                    instance.P0FinalAssetArtistPrefabLoaded = true;
                    instance.P0FinalAssetFinalArtApproved =
                        p0FinalAsset.FinalArtApproved;
                    instance.FinalAssetArtistPrefabLoaded = true;
                    instance.FinalAssetApproved =
                        p0FinalAsset.FinalArtApproved;
                }
                else
                {
                    CreateP0FinalAssetCandidateHierarchy(p0FinalAsset,
                        root.transform);
                    instance.P0FinalAssetProceduralFallbackActive = true;
                    instance.P0FinalAssetFinalArtApproved = false;
                    instance.FinalAssetProceduralFallbackActive = true;
                    instance.FinalAssetApproved = false;
                }
            }
            else if (remainingFinalAsset != null)
            {
                if (gate != null)
                {
                    ConfigureGateIdentityInstance(instance, gate);
                    if (defense != null)
                        ConfigureDefenseInstance(instance, defense, gate);
                }
                if (landmark != null)
                {
                    ConfigureHistoricalLandmarkInstance(instance, landmark);
                    if (finalCivic != null)
                        ConfigureFinalCivicInstance(instance, finalCivic,
                            landmark);
                }
                if (urbanFabric != null)
                    ConfigureUrbanFabricInstance(instance, urbanFabric);
                if (infrastructure != null)
                    ConfigureInfrastructureInstance(instance, infrastructure);
                if (defense != null && gate == null)
                    ConfigureDefenseInstance(instance, defense, null);
                if (resourceAgriculture != null)
                    ConfigureResourceAgricultureInstance(instance,
                        resourceAgriculture);
                if (finalCivic != null && landmark == null)
                    ConfigureFinalCivicInstance(instance, finalCivic, null);
                if (production != null)
                    ConfigureProductionInstance(instance, production);
                ConfigureRemainingFinalAssetInstance(instance,
                    remainingFinalAsset);
                if (TryCreateRemainingFinalAssetPrefabHierarchy(
                        remainingFinalAsset, instance, root.transform))
                {
                    instance.FinalAssetArtistPrefabLoaded = true;
                    instance.FinalAssetApproved =
                        remainingFinalAsset.FinalArtApproved;
                }
                else
                {
                    if (gate != null)
                        CreateGateIdentityHierarchy(gate, root.transform);
                    else if (landmark != null)
                        CreateHistoricalLandmarkHierarchy(landmark,
                            root.transform);
                    else if (urbanFabric != null)
                        CreateUrbanFabricHierarchy(urbanFabric,
                            root.transform);
                    else if (infrastructure != null)
                        CreateInfrastructureHierarchy(infrastructure,
                            root.transform);
                    else if (defense != null)
                        CreateDefenseHierarchy(defense, root.transform);
                    else if (resourceAgriculture != null)
                        CreateResourceAgricultureHierarchy(resourceAgriculture,
                            root.transform);
                    else if (finalCivic != null)
                        CreateFinalCivicHierarchy(finalCivic, root.transform);
                    else if (production != null)
                        CreateProductionHierarchy(definition, production,
                            root.transform);
                    else
                        CreateModules(definition, root.transform, null, null);
                    instance.FinalAssetProceduralFallbackActive = true;
                    instance.FinalAssetApproved = false;
                }
            }
            else if (gate != null)
            {
                ConfigureGateIdentityInstance(instance, gate);
                if (defense != null)
                    ConfigureDefenseInstance(instance, defense, gate);
                CreateGateIdentityHierarchy(gate, root.transform);
            }
            else if (landmark != null)
            {
                ConfigureHistoricalLandmarkInstance(instance, landmark);
                if (finalCivic != null)
                    ConfigureFinalCivicInstance(instance, finalCivic,
                        landmark);
                CreateHistoricalLandmarkHierarchy(landmark, root.transform);
            }
            else if (urbanFabric != null)
            {
                ConfigureUrbanFabricInstance(instance, urbanFabric);
                CreateUrbanFabricHierarchy(urbanFabric, root.transform);
            }
            else if (infrastructure != null)
            {
                ConfigureInfrastructureInstance(instance, infrastructure);
                CreateInfrastructureHierarchy(infrastructure, root.transform);
            }
            else if (defense != null)
            {
                ConfigureDefenseInstance(instance, defense, null);
                CreateDefenseHierarchy(defense, root.transform);
            }
            else if (resourceAgriculture != null)
            {
                ConfigureResourceAgricultureInstance(instance,
                    resourceAgriculture);
                CreateResourceAgricultureHierarchy(resourceAgriculture,
                    root.transform);
            }
            else if (finalCivic != null)
            {
                ConfigureFinalCivicInstance(instance, finalCivic, null);
                CreateFinalCivicHierarchy(finalCivic, root.transform);
            }
            else if (production != null)
            {
                ConfigureProductionInstance(instance, production);
                CreateProductionHierarchy(definition, production, root.transform);
            }
            else
                CreateModules(definition, root.transform, null, null);
            return instance;
        }

        public void Dispose()
        {
            if (_disposed) return;
            foreach (var mesh in _batchPrimitiveMeshes.Values)
                if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            _batchPrimitiveMeshes.Clear();
            foreach (var material in _materials.Values)
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
            _materials.Clear();
            _models.Clear();
            _productionProfiles.Clear();
            _historicalLandmarkProfiles.Clear();
            _gateIdentityProfiles.Clear();
            _urbanFabricProfiles.Clear();
            _infrastructureProfiles.Clear();
            _defenseProfilesByFacilityId.Clear();
            _proceduralDefenseProfilesByModelId.Clear();
            _resourceProfilesByFacilityId.Clear();
            _resourceProfilesByDefinitionId.Clear();
            _uniqueResourceProfilesByModelId.Clear();
            _finalCivicProfilesByFacilityId.Clear();
            _proceduralFinalCivicProfilesByDefinitionId.Clear();
            _uniqueProceduralFinalCivicProfilesByModelId.Clear();
            _p0FinalAssetProfilesByFacilityId.Clear();
            _p0LandmarkSecondBatchProfilesByFacilityId.Clear();
            _p0LandmarkThirdBatchProfilesByFacilityId.Clear();
            _p0NamedGateFourthBatchProfilesByFacilityId.Clear();
            _remainingFinalAssetProfilesByAssetVariantId.Clear();
            _productionMeshes?.Dispose();
            _disposed = true;
        }

        private IReadOnlyList<HanBuildableFacilityBatchModule> CreateBatchModules(
            IReadOnlyList<HanBuildableFacilityModuleDefinition> modules,
            HashSet<string> includedModuleIds,
            IReadOnlyDictionary<string, string> primitiveOverrides)
        {
            var result = new List<HanBuildableFacilityBatchModule>();
            foreach (var module in modules)
            {
                if (includedModuleIds != null &&
                    !includedModuleIds.Contains(module.ModuleId)) continue;
                var primitiveId = primitiveOverrides != null &&
                                  primitiveOverrides.TryGetValue(module.ModuleId,
                                      out var overrideId)
                    ? overrideId : module.PrimitiveId;
                if (!_materials.TryGetValue(module.MaterialId, out var material))
                    throw new InvalidOperationException(
                        "Unknown Han building batch material: " + module.MaterialId);
                var scaleY = primitiveId == "cylinder"
                    ? module.ScaleY * 0.5f : module.ScaleY;
                var matrix = Matrix4x4.TRS(
                    new Vector3(module.PositionX, module.PositionY,
                        module.PositionZ),
                    Quaternion.Euler(module.RotationX, module.RotationY,
                        module.RotationZ),
                    new Vector3(module.ScaleX, scaleY, module.ScaleZ));
                result.Add(new HanBuildableFacilityBatchModule(module.ModuleId,
                    primitiveId, module.MaterialId,
                    GetBatchPrimitiveMesh(primitiveId), material, matrix));
            }
            if (result.Count == 0)
                throw new InvalidOperationException(
                    "Han building world batch has no renderable modules.");
            return result;
        }

        private Mesh GetBatchPrimitiveMesh(string primitiveId)
        {
            if (primitiveId.StartsWith("han.", StringComparison.Ordinal))
            {
                if (_productionMeshes == null)
                    throw new InvalidOperationException(
                        "Han production mesh library is not initialized.");
                return _productionMeshes.Get(primitiveId);
            }
            if (_batchPrimitiveMeshes.TryGetValue(primitiveId, out var mesh))
                return mesh;
            var type = primitiveId == "cylinder"
                ? PrimitiveType.Cylinder : primitiveId == "cube"
                    ? PrimitiveType.Cube
                    : throw new KeyNotFoundException(
                        "Unknown Han batch primitive: " + primitiveId);
            var temporary = GameObject.CreatePrimitive(type);
            try
            {
                var shared = temporary.GetComponent<MeshFilter>()?.sharedMesh;
                if (shared == null)
                    throw new InvalidOperationException(
                        "Unity primitive has no source mesh: " + primitiveId);
                mesh = UnityEngine.Object.Instantiate(shared);
                mesh.name = "HAN_BATCH_SOURCE_" + primitiveId;
                _batchPrimitiveMeshes.Add(primitiveId, mesh);
                return mesh;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        private static void ConfigureProductionInstance(
            HanBuildableFacilityModelInstance instance,
            LuoyangProductionBuildingProfile production)
        {
            instance.ProductionReady = true;
            instance.TerrainConforming = production.TerrainConforming;
            instance.ProductionProfileId = production.ProfileId;
            instance.ProductionAssetVariantId = production.AssetVariantId;
            instance.ProductionLodProfileId = production.LodProfileId;
            instance.PlacementAnchorId = production.PlacementAnchorId;
            instance.EntranceAnchorId = production.EntranceAnchorId;
        }

        private static void ConfigureHistoricalLandmarkInstance(
            HanBuildableFacilityModelInstance instance,
            LuoyangHistoricalLandmarkProfile landmark)
        {
            instance.HistoricalLandmarkReady = true;
            instance.HistoricalLandmarkProfileId = landmark.ProfileId;
            instance.HistoricalLandmarkAssetVariantId = landmark.AssetVariantId;
            instance.HistoricalLandmarkSilhouetteId = landmark.SilhouetteId;
            instance.HistoricalLandmarkFacilityId = landmark.FacilityId;
            instance.HistoricalConfidence = landmark.HistoricalConfidence;
            instance.SpatialPrecision = landmark.SpatialPrecision;
            instance.ProductionLodProfileId = landmark.LodProfileId;
            instance.PlacementAnchorId = landmark.PlacementAnchorId;
            instance.EntranceAnchorId = landmark.EntranceAnchorId;
        }

        private static void ConfigureGateIdentityInstance(
            HanBuildableFacilityModelInstance instance,
            LuoyangGateIdentityProfile gate)
        {
            instance.GateIdentityReady = true;
            instance.GateIdentityProfileId = gate.ProfileId;
            instance.GateIdentityAssetVariantId = gate.AssetVariantId;
            instance.GateIdentitySilhouetteId = gate.SilhouetteId;
            instance.GateIdentityFacilityId = gate.FacilityId;
            instance.GateIdentityLodProfileId = gate.LodProfileId;
            instance.GateClassId = gate.GateClassId;
            instance.GatehouseTypeId = gate.GatehouseTypeId;
            instance.FacilityDirection = gate.FacilityDirection;
            instance.VisualFacing = gate.VisualFacing;
            instance.DirectionBasisId = gate.DirectionBasisId;
            instance.PlacementAnchorId = gate.PlacementAnchorId;
            instance.OuterPassageAnchorId = gate.OuterPassageAnchorId;
            instance.InnerPassageAnchorId = gate.InnerPassageAnchorId;
            instance.HistoricalConfidence = gate.HistoricalConfidence;
            instance.SpatialPrecision = gate.SpatialPrecision;
        }

        private static void ConfigureUrbanFabricInstance(
            HanBuildableFacilityModelInstance instance,
            LuoyangMediumFrequencyUrbanFabricProfile profile)
        {
            instance.MediumFrequencyUrbanFabricReady = true;
            instance.UrbanFabricProfileId = profile.ProfileId;
            instance.UrbanFabricAssetVariantId = profile.AssetVariantId;
            instance.UrbanFabricRoleId = profile.FabricRoleId;
            instance.UrbanFabricDensityClassId = profile.DensityClassId;
            instance.UrbanFabricStreetInterfaceId = profile.StreetInterfaceId;
            instance.UrbanFabricLodProfileId = profile.LodProfileId;
            instance.PlacementAnchorId = profile.PlacementAnchorId;
            instance.EntranceAnchorId = profile.EntranceAnchorId;
        }

        private static void ConfigureInfrastructureInstance(
            HanBuildableFacilityModelInstance instance,
            LuoyangInfrastructureProductionProfile profile)
        {
            instance.InfrastructureProductionReady = true;
            instance.InfrastructureProfileId = profile.ProfileId;
            instance.InfrastructureAssetVariantId = profile.AssetVariantId;
            instance.InfrastructureRoleId = profile.InfrastructureRoleId;
            instance.InfrastructureAlignmentModeId = profile.AlignmentModeId;
            instance.InfrastructureLodProfileId = profile.LodProfileId;
            instance.PlacementAnchorId = profile.PlacementAnchorId;
        }

        private static void ConfigureDefenseInstance(
            HanBuildableFacilityModelInstance instance,
            LuoyangLowFrequencyDefenseProductionProfile profile,
            LuoyangGateIdentityProfile gate)
        {
            instance.LowFrequencyDefenseProductionReady = true;
            instance.LowFrequencyDefenseProfileId = profile.ProfileId;
            instance.LowFrequencyDefenseAssetVariantId =
                gate?.AssetVariantId ?? profile.AssetVariantId;
            instance.LowFrequencyDefenseRoleId = profile.DefenseRoleId;
            instance.LowFrequencyDefenseModeId = profile.ProductionModeId;
            instance.LowFrequencyDefenseFacingPolicyId = profile.FacingPolicyId;
            instance.LowFrequencyDefenseLodProfileId =
                gate?.LodProfileId ?? profile.LodProfileId;
            if (gate == null)
            {
                instance.PlacementAnchorId = profile.PlacementAnchorId;
                instance.EntranceAnchorId = profile.EntranceAnchorId;
            }
        }

        private static void ConfigureResourceAgricultureInstance(
            HanBuildableFacilityModelInstance instance,
            LuoyangResourceAgricultureProductionProfile profile)
        {
            instance.ResourceAgricultureProductionReady = true;
            instance.ResourceAgricultureProfileId = profile.ProfileId;
            instance.ResourceAgricultureAssetVariantId = profile.AssetVariantId;
            instance.ResourceAgricultureRoleId = profile.ProductionRoleId;
            instance.ResourceAgricultureEvidenceBasisId = profile.EvidenceBasisId;
            instance.ResourceAgricultureLodProfileId = profile.LodProfileId;
            instance.PlacementAnchorId = profile.PlacementAnchorId;
            instance.EntranceAnchorId = profile.EntranceAnchorId;
        }

        private static void ConfigureFinalCivicInstance(
            HanBuildableFacilityModelInstance instance,
            LuoyangFinalCivicRitualMedicalProductionProfile profile,
            LuoyangHistoricalLandmarkProfile landmark)
        {
            instance.FinalCivicProductionReady = true;
            instance.FinalCivicProfileId = profile.ProfileId;
            instance.FinalCivicAssetVariantId =
                landmark?.AssetVariantId ?? profile.AssetVariantId;
            instance.FinalCivicRoleId = profile.CivicRoleId;
            instance.FinalCivicModeId = profile.ProductionModeId;
            instance.FinalCivicEvidenceBasisId = profile.EvidenceBasisId;
            instance.FinalCivicLodProfileId =
                landmark?.LodProfileId ?? profile.LodProfileId;
            if (landmark == null)
            {
                instance.PlacementAnchorId = profile.PlacementAnchorId;
                instance.EntranceAnchorId = profile.EntranceAnchorId;
            }
        }

        private static void ConfigureP0FinalAssetInstance(
            HanBuildableFacilityModelInstance instance,
            LuoyangP0FinalAssetProfile profile)
        {
            instance.P0FinalAssetVerticalSliceReady = true;
            instance.P0FinalAssetCandidateId = profile.CandidateId;
            instance.P0FinalAssetCandidateStatusId = profile.CandidateStatusId;
            instance.P0FinalAssetReplacementSlotId = profile.ReplacementSlotId;
            instance.P0FinalAssetMaterialSetId = profile.MaterialSetId;
            instance.P0FinalAssetLodProfileId = profile.LodProfileId;
            instance.P0FinalAssetRuntimeModeId = profile.RuntimeCandidateModeId;
            instance.P0FinalAssetPrefabResourcePath =
                profile.ArtistPrefabResourcePath;
            instance.P0FinalAssetFinalArtApproved = false;
            instance.FinalAssetRuntimeReady = true;
            instance.FinalAssetTaskStatusId = profile.CandidateStatusId;
            instance.FinalAssetReplacementSlotId = profile.ReplacementSlotId;
            instance.FinalAssetPrefabResourcePath =
                profile.ArtistPrefabResourcePath;
            instance.FinalAssetApproved = false;
            var placement = profile.Anchors.Single(item => string.Equals(
                item.RoleId,
                LuoyangP0FinalAssetVerticalSliceIds.PlacementAnchorRoleId,
                StringComparison.Ordinal));
            instance.PlacementAnchorId = placement.AnchorId;
            var entrance = profile.Anchors.FirstOrDefault(item => string.Equals(
                item.RoleId,
                LuoyangP0FinalAssetVerticalSliceIds.EntranceAnchorRoleId,
                StringComparison.Ordinal));
            if (entrance != null) instance.EntranceAnchorId = entrance.AnchorId;
            var outer = profile.Anchors.FirstOrDefault(item => string.Equals(
                item.RoleId,
                LuoyangP0FinalAssetVerticalSliceIds.OuterPassageAnchorRoleId,
                StringComparison.Ordinal));
            if (outer != null) instance.OuterPassageAnchorId = outer.AnchorId;
            var inner = profile.Anchors.FirstOrDefault(item => string.Equals(
                item.RoleId,
                LuoyangP0FinalAssetVerticalSliceIds.InnerPassageAnchorRoleId,
                StringComparison.Ordinal));
            if (inner != null) instance.InnerPassageAnchorId = inner.AnchorId;
        }

        private static void ConfigureRemainingFinalAssetInstance(
            HanBuildableFacilityModelInstance instance,
            LuoyangRemainingFinalAssetProfile profile)
        {
            instance.FinalAssetRuntimeReady = true;
            instance.FinalAssetTaskStatusId =
                LuoyangRemainingFinalAssetIds.StatusId;
            instance.FinalAssetReviewOrder = profile.ReviewOrder;
            instance.FinalAssetReplacementSlotId =
                profile.ReplacementSlotId;
            instance.FinalAssetPrefabResourcePath =
                profile.ArtistPrefabResourcePath;
            instance.FinalAssetApproved = false;
        }

        private LuoyangResourceAgricultureProductionProfile
            ResolveResourceAgricultureProfile(string modelId,
                string runtimeBindingId)
        {
            if (_resourceProfilesByFacilityId.TryGetValue(
                    runtimeBindingId ?? string.Empty, out var profile))
                return profile;
            if (_resourceProfilesByDefinitionId.TryGetValue(
                    runtimeBindingId ?? string.Empty, out profile))
                return profile;
            _uniqueResourceProfilesByModelId.TryGetValue(modelId ?? string.Empty,
                out profile);
            return profile;
        }

        private LuoyangFinalCivicRitualMedicalProductionProfile
            ResolveFinalCivicProfile(string modelId, string runtimeBindingId)
        {
            if (_finalCivicProfilesByFacilityId.TryGetValue(
                    runtimeBindingId ?? string.Empty, out var profile))
                return profile;
            if (_proceduralFinalCivicProfilesByDefinitionId.TryGetValue(
                    runtimeBindingId ?? string.Empty, out profile))
                return profile;
            _uniqueProceduralFinalCivicProfilesByModelId.TryGetValue(
                modelId ?? string.Empty, out profile);
            return profile;
        }

        private bool TryCreateArtistPrefabHierarchy(
            LuoyangP0FinalAssetProfile profile, Transform root)
        {
            var prefab = _artistPrefabLoader(profile.ArtistPrefabResourcePath);
            if (prefab == null) return false;
            var value = UnityEngine.Object.Instantiate(prefab, root, false);
            value.name = "ARTIST_PREFAB." + profile.CandidateId;
            try
            {
                if (value.GetComponentsInChildren<Collider>(true).Length != 0)
                    throw new InvalidOperationException(
                        "Luoyang P0 artist prefab must not contain colliders.");
                var lodGroups = value.GetComponentsInChildren<LODGroup>(true);
                if (lodGroups.Length == 0 || lodGroups.Any(group =>
                        group.GetLODs().Length != 3 || group.GetLODs().Any(lod =>
                            lod.renderers == null || lod.renderers.Length == 0 ||
                            lod.renderers.Any(renderer => renderer == null ||
                                renderer.sharedMaterial == null))))
                    throw new InvalidOperationException(
                        "Luoyang P0 artist prefab must provide three populated LOD levels with materials.");
                var names = new HashSet<string>(value
                    .GetComponentsInChildren<Transform>(true)
                    .Select(item => item.name), StringComparer.Ordinal);
                if (profile.Anchors.Any(anchor =>
                        !names.Contains(anchor.AnchorId)))
                    throw new InvalidOperationException(
                        "Luoyang P0 artist prefab is missing a frozen anchor.");
                return true;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(value);
                throw;
            }
        }

        private bool TryCreateRemainingFinalAssetPrefabHierarchy(
            LuoyangRemainingFinalAssetProfile profile,
            HanBuildableFacilityModelInstance instance, Transform root)
        {
            var prefab = _artistPrefabLoader(profile.ArtistPrefabResourcePath);
            if (prefab == null) return false;
            var value = UnityEngine.Object.Instantiate(prefab, root, false);
            value.name = "FINAL_PREFAB.R" + profile.ReviewOrder;
            try
            {
                if (value.GetComponentsInChildren<Collider>(true).Length != 0)
                    throw new InvalidOperationException(
                        "Luoyang remaining final-asset prefab must not contain colliders.");
                var lodGroups = value.GetComponentsInChildren<LODGroup>(true);
                if (lodGroups.Length == 0 || lodGroups.Any(group =>
                        group.GetLODs().Length != 3 || group.GetLODs().Any(lod =>
                            lod.renderers == null || lod.renderers.Length == 0 ||
                            lod.renderers.Any(renderer => renderer == null ||
                                renderer.sharedMaterial == null))))
                    throw new InvalidOperationException(
                        "Luoyang remaining final-asset prefab must provide three populated LOD levels with materials.");
                var names = new HashSet<string>(value
                    .GetComponentsInChildren<Transform>(true)
                    .Select(item => item.name), StringComparer.Ordinal);
                var requiredAnchors = new[]
                    {
                        instance.PlacementAnchorId, instance.EntranceAnchorId,
                        instance.OuterPassageAnchorId,
                        instance.InnerPassageAnchorId
                    }
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.Ordinal);
                if (requiredAnchors.Any(anchor => !names.Contains(anchor)))
                    throw new InvalidOperationException(
                        "Luoyang remaining final-asset prefab is missing a stable source anchor.");
                return true;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(value);
                throw;
            }
        }

        private void CreateP0FinalAssetCandidateHierarchy(
            LuoyangP0FinalAssetProfile profile, Transform root)
        {
            foreach (var anchor in profile.Anchors)
                CreateAnchor(anchor.AnchorId,
                    new Vector3(anchor.X, anchor.Y, anchor.Z), root);
            var lod0 = CreateLodRoot("LOD0.p0-final-candidate", root);
            var lod1 = CreateLodRoot("LOD1.p0-final-strategy", root);
            var lod2 = CreateLodRoot("LOD2.p0-final-world", root);
            var lod0Renderers = CreateModules(profile.Modules, lod0, null, null);
            var lod1Renderers = CreateModules(profile.Modules, lod1,
                new HashSet<string>(profile.Lod1ModuleIds,
                    StringComparer.Ordinal), null);
            var lod2Renderers = CreateModules(profile.Modules, lod2,
                new HashSet<string>(profile.Lod2ModuleIds,
                    StringComparer.Ordinal), null);
            var group = root.gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(0.18f, lod0Renderers),
                new LOD(0.065f, lod1Renderers),
                new LOD(0.010f, lod2Renderers)
            });
            group.RecalculateBounds();
        }

        private void CreateUrbanFabricHierarchy(
            LuoyangMediumFrequencyUrbanFabricProfile profile, Transform root)
        {
            CreateAnchor(profile.PlacementAnchorId, Vector3.zero, root);
            CreateAnchor(profile.EntranceAnchorId,
                new Vector3(profile.EntranceX, profile.EntranceY,
                    profile.EntranceZ), root);
            var lod0 = CreateLodRoot("LOD0.urban-fabric", root);
            var lod1 = CreateLodRoot("LOD1.urban-block", root);
            var lod2 = CreateLodRoot("LOD2.urban-world", root);
            var lod0Renderers = CreateModules(profile.Modules, lod0, null, null);
            var lod1Renderers = CreateModules(profile.Modules, lod1,
                new HashSet<string>(profile.Lod1ModuleIds, StringComparer.Ordinal), null);
            var lod2Renderers = CreateModules(profile.Modules, lod2,
                new HashSet<string>(profile.Lod2ModuleIds, StringComparer.Ordinal), null);
            var group = root.gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(0.18f, lod0Renderers),
                new LOD(0.07f, lod1Renderers),
                new LOD(0.012f, lod2Renderers)
            });
            group.RecalculateBounds();
        }

        private void CreateInfrastructureHierarchy(
            LuoyangInfrastructureProductionProfile profile, Transform root)
        {
            CreateAnchor(profile.PlacementAnchorId, Vector3.zero, root);
            foreach (var anchor in profile.Anchors)
                CreateAnchor(anchor.AnchorId,
                    new Vector3(anchor.X, anchor.Y, anchor.Z), root);
            var lod0 = CreateLodRoot("LOD0.infrastructure", root);
            var lod1 = CreateLodRoot("LOD1.infrastructure-strategy", root);
            var lod2 = CreateLodRoot("LOD2.infrastructure-world", root);
            var lod0Renderers = CreateModules(profile.Modules, lod0, null, null);
            var lod1Renderers = CreateModules(profile.Modules, lod1,
                new HashSet<string>(profile.Lod1ModuleIds,
                    StringComparer.Ordinal), null);
            var lod2Renderers = CreateModules(profile.Modules, lod2,
                new HashSet<string>(profile.Lod2ModuleIds,
                    StringComparer.Ordinal), null);
            var group = root.gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(0.16f, lod0Renderers),
                new LOD(0.055f, lod1Renderers),
                new LOD(0.009f, lod2Renderers)
            });
            group.RecalculateBounds();
        }

        private void CreateDefenseHierarchy(
            LuoyangLowFrequencyDefenseProductionProfile profile,
            Transform root)
        {
            CreateAnchor(profile.PlacementAnchorId, Vector3.zero, root);
            CreateAnchor(profile.EntranceAnchorId,
                new Vector3(profile.EntranceX, profile.EntranceY,
                    profile.EntranceZ), root);
            var lod0 = CreateLodRoot("LOD0.defense", root);
            var lod1 = CreateLodRoot("LOD1.defense-strategy", root);
            var lod2 = CreateLodRoot("LOD2.defense-world", root);
            var lod0Renderers = CreateModules(profile.Modules, lod0, null, null);
            var lod1Renderers = CreateModules(profile.Modules, lod1,
                new HashSet<string>(profile.Lod1ModuleIds,
                    StringComparer.Ordinal), null);
            var lod2Renderers = CreateModules(profile.Modules, lod2,
                new HashSet<string>(profile.Lod2ModuleIds,
                    StringComparer.Ordinal), null);
            var group = root.gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(0.16f, lod0Renderers),
                new LOD(0.055f, lod1Renderers),
                new LOD(0.009f, lod2Renderers)
            });
            group.RecalculateBounds();
        }

        private void CreateResourceAgricultureHierarchy(
            LuoyangResourceAgricultureProductionProfile profile,
            Transform root)
        {
            CreateAnchor(profile.PlacementAnchorId, Vector3.zero, root);
            CreateAnchor(profile.EntranceAnchorId,
                new Vector3(profile.EntranceX, profile.EntranceY,
                    profile.EntranceZ), root);
            var lod0 = CreateLodRoot("LOD0.resource-agriculture", root);
            var lod1 = CreateLodRoot("LOD1.resource-agriculture-strategy", root);
            var lod2 = CreateLodRoot("LOD2.resource-agriculture-world", root);
            var lod0Renderers = CreateModules(profile.Modules, lod0, null, null);
            var lod1Renderers = CreateModules(profile.Modules, lod1,
                new HashSet<string>(profile.Lod1ModuleIds,
                    StringComparer.Ordinal), null);
            var lod2Renderers = CreateModules(profile.Modules, lod2,
                new HashSet<string>(profile.Lod2ModuleIds,
                    StringComparer.Ordinal), null);
            var group = root.gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(0.16f, lod0Renderers),
                new LOD(0.055f, lod1Renderers),
                new LOD(0.009f, lod2Renderers)
            });
            group.RecalculateBounds();
        }

        private void CreateFinalCivicHierarchy(
            LuoyangFinalCivicRitualMedicalProductionProfile profile,
            Transform root)
        {
            CreateAnchor(profile.PlacementAnchorId, Vector3.zero, root);
            CreateAnchor(profile.EntranceAnchorId,
                new Vector3(profile.EntranceX, profile.EntranceY,
                    profile.EntranceZ), root);
            var lod0 = CreateLodRoot("LOD0.final-civic", root);
            var lod1 = CreateLodRoot("LOD1.final-civic-strategy", root);
            var lod2 = CreateLodRoot("LOD2.final-civic-world", root);
            var lod0Renderers = CreateModules(profile.Modules, lod0, null, null);
            var lod1Renderers = CreateModules(profile.Modules, lod1,
                new HashSet<string>(profile.Lod1ModuleIds,
                    StringComparer.Ordinal), null);
            var lod2Renderers = CreateModules(profile.Modules, lod2,
                new HashSet<string>(profile.Lod2ModuleIds,
                    StringComparer.Ordinal), null);
            var group = root.gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(0.16f, lod0Renderers),
                new LOD(0.055f, lod1Renderers),
                new LOD(0.009f, lod2Renderers)
            });
            group.RecalculateBounds();
        }

        private void CreateGateIdentityHierarchy(
            LuoyangGateIdentityProfile gate, Transform root)
        {
            CreateAnchor(gate.PlacementAnchorId, Vector3.zero, root);
            CreateAnchor(gate.OuterPassageAnchorId,
                new Vector3(gate.OuterPassageX, gate.OuterPassageY,
                    gate.OuterPassageZ), root);
            CreateAnchor(gate.InnerPassageAnchorId,
                new Vector3(gate.InnerPassageX, gate.InnerPassageY,
                    gate.InnerPassageZ), root);

            var lod0 = CreateLodRoot("LOD0.gate-identity", root);
            var lod1 = CreateLodRoot("LOD1.gate-silhouette", root);
            var lod2 = CreateLodRoot("LOD2.gate-world", root);
            var lod0Renderers = CreateModules(gate.Modules, lod0, null, null);
            var lod1Renderers = CreateModules(gate.Modules, lod1,
                new HashSet<string>(gate.Lod1ModuleIds, StringComparer.Ordinal), null);
            var lod2Renderers = CreateModules(gate.Modules, lod2,
                new HashSet<string>(gate.Lod2ModuleIds, StringComparer.Ordinal), null);
            var group = root.gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(0.035f, lod0Renderers),
                new LOD(0.015f, lod1Renderers),
                new LOD(0.003f, lod2Renderers)
            });
            group.RecalculateBounds();
        }

        private void CreateHistoricalLandmarkHierarchy(
            LuoyangHistoricalLandmarkProfile landmark, Transform root)
        {
            CreateAnchor(landmark.PlacementAnchorId, Vector3.zero, root);
            CreateAnchor(landmark.EntranceAnchorId,
                new Vector3(landmark.EntranceX, landmark.EntranceY,
                    landmark.EntranceZ), root);

            var lod0 = CreateLodRoot("LOD0.landmark", root);
            var lod1 = CreateLodRoot("LOD1.silhouette", root);
            var lod2 = CreateLodRoot("LOD2.world", root);
            var lod0Renderers = CreateModules(landmark.Modules, lod0, null, null);
            var lod1Renderers = CreateModules(landmark.Modules, lod1,
                new HashSet<string>(landmark.Lod1ModuleIds,
                    StringComparer.Ordinal), null);
            var lod2Renderers = CreateModules(landmark.Modules, lod2,
                new HashSet<string>(landmark.Lod2ModuleIds,
                    StringComparer.Ordinal), null);
            var group = root.gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(0.55f, lod0Renderers),
                new LOD(0.22f, lod1Renderers),
                new LOD(0.04f, lod2Renderers)
            });
            group.RecalculateBounds();
        }

        private void CreateProductionHierarchy(
            HanBuildableFacilityModelDefinition definition,
            LuoyangProductionBuildingProfile production, Transform root)
        {
            CreateAnchor(production.PlacementAnchorId, Vector3.zero, root);
            CreateAnchor(production.EntranceAnchorId,
                new Vector3(production.EntranceX, production.EntranceY,
                    production.EntranceZ), root);

            var overrides = production.PrimitiveOverrides.ToDictionary(
                item => item.ModuleId, item => item.PrimitiveId,
                StringComparer.Ordinal);
            var lod0 = CreateLodRoot("LOD0.production", root);
            var lod1 = CreateLodRoot("LOD1.strategy", root);
            var lod2 = CreateLodRoot("LOD2.world", root);
            var lod0Renderers = CreateModules(definition, lod0, null, overrides);
            var lod1Renderers = CreateModules(definition, lod1,
                new HashSet<string>(production.Lod1ModuleIds,
                    StringComparer.Ordinal), overrides);
            var lod2Renderers = CreateModules(definition, lod2,
                new HashSet<string>(production.Lod2ModuleIds,
                    StringComparer.Ordinal), overrides);
            var group = root.gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(0.55f, lod0Renderers),
                new LOD(0.22f, lod1Renderers),
                new LOD(0.04f, lod2Renderers)
            });
            group.RecalculateBounds();
        }

        private Renderer[] CreateModules(HanBuildableFacilityModelDefinition definition,
            Transform parent, HashSet<string> includedModuleIds,
            IReadOnlyDictionary<string, string> primitiveOverrides)
        {
            return CreateModules(definition.Modules, parent, includedModuleIds,
                primitiveOverrides);
        }

        private Renderer[] CreateModules(
            IReadOnlyList<HanBuildableFacilityModuleDefinition> modules,
            Transform parent, HashSet<string> includedModuleIds,
            IReadOnlyDictionary<string, string> primitiveOverrides)
        {
            var renderers = new List<Renderer>();
            foreach (var module in modules)
            {
                if (includedModuleIds != null &&
                    !includedModuleIds.Contains(module.ModuleId)) continue;
                var primitiveId = primitiveOverrides != null &&
                                  primitiveOverrides.TryGetValue(module.ModuleId,
                                      out var overrideId)
                    ? overrideId : module.PrimitiveId;
                var customMesh = primitiveId.StartsWith("han.",
                    StringComparison.Ordinal);
                GameObject value;
                Renderer renderer;
                if (customMesh)
                {
                    value = new GameObject(module.ModuleId);
                    var filter = value.AddComponent<MeshFilter>();
                    filter.sharedMesh = _productionMeshes.Get(primitiveId);
                    renderer = value.AddComponent<MeshRenderer>();
                }
                else
                {
                    var primitive = primitiveId == "cylinder"
                        ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                    value = GameObject.CreatePrimitive(primitive);
                    value.name = module.ModuleId;
                    var collider = value.GetComponent<Collider>();
                    if (collider != null)
                        UnityEngine.Object.DestroyImmediate(collider);
                    renderer = value.GetComponent<Renderer>();
                }
                value.transform.SetParent(parent, false);
                value.transform.localPosition = new Vector3(module.PositionX,
                    module.PositionY, module.PositionZ);
                value.transform.localRotation = Quaternion.Euler(module.RotationX,
                    module.RotationY, module.RotationZ);
                value.transform.localScale = new Vector3(module.ScaleX,
                    primitiveId == "cylinder" ? module.ScaleY * 0.5f : module.ScaleY,
                    module.ScaleZ);
                renderer.sharedMaterial = _materials[module.MaterialId];
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderers.Add(renderer);
            }
            return renderers.ToArray();
        }

        private static Transform CreateLodRoot(string name, Transform parent)
        {
            var value = new GameObject(name);
            value.transform.SetParent(parent, false);
            return value.transform;
        }

        private static void CreateAnchor(string name, Vector3 position,
            Transform parent)
        {
            var value = new GameObject(name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
        }

        private static Material CreateMaterial(
            HanBuildableFacilityMaterialDefinition definition)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard") ?? Shader.Find("Diffuse") ??
                         Shader.Find("Sprites/Default");
            if (shader == null)
                throw new InvalidOperationException(
                    "No supported shader is available for the Han building model kit.");
            var material = new Material(shader)
                { name = definition.MaterialId };
            var color = new Color(definition.Red, definition.Green, definition.Blue,
                definition.Alpha);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", definition.Metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", definition.Smoothness);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", definition.Smoothness);
            return material;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
        }
    }

    internal sealed class HanProductionBuildingMeshLibrary : IDisposable
    {
        private readonly Dictionary<string, Mesh> _meshes =
            new Dictionary<string, Mesh>(StringComparer.Ordinal);

        public int Count => _meshes.Count;

        public Mesh Get(string primitiveId)
        {
            if (_meshes.TryGetValue(primitiveId, out var mesh)) return mesh;
            switch (primitiveId)
            {
                case "han.rammed_block":
                    mesh = CreateFrustum(primitiveId, 0.50f, 0.50f,
                        0.46f, 0.46f);
                    break;
                case "han.terrain_pad":
                    mesh = CreateFrustum(primitiveId, 0.46f, 0.46f,
                        0.50f, 0.50f);
                    break;
                case "han.wall_coping":
                    mesh = CreateFrustum(primitiveId, 0.50f, 0.50f,
                        0.38f, 0.42f);
                    break;
                case "han.field_ridge":
                    mesh = CreateExtrudedAlongZ(primitiveId, new[]
                    {
                        new Vector2(-0.50f, -0.50f),
                        new Vector2(0.50f, -0.50f),
                        new Vector2(0.50f, -0.18f),
                        new Vector2(0.30f, 0.50f),
                        new Vector2(-0.30f, 0.50f),
                        new Vector2(-0.50f, -0.18f)
                    });
                    break;
                case "han.road_crown":
                    mesh = CreateExtrudedAlongZ(primitiveId, new[]
                    {
                        new Vector2(-0.50f, -0.50f),
                        new Vector2(0.50f, -0.50f),
                        new Vector2(0.50f, 0.00f),
                        new Vector2(0.16f, 0.38f),
                        new Vector2(-0.16f, 0.38f),
                        new Vector2(-0.50f, 0.00f)
                    });
                    break;
                case "han.tile_slab":
                    mesh = CreateExtrudedAlongX(primitiveId, new[]
                    {
                        new Vector2(-0.50f, -0.32f),
                        new Vector2(0.50f, -0.32f),
                        new Vector2(0.50f, -0.02f),
                        new Vector2(0.34f, 0.18f),
                        new Vector2(0.00f, 0.34f),
                        new Vector2(-0.34f, 0.18f),
                        new Vector2(-0.50f, -0.02f)
                    });
                    break;
                case "han.foliage_cluster":
                    mesh = CreateFoliageCluster(primitiveId);
                    break;
                case "han.timber_beam":
                    mesh = CreateOctagonalBeam(primitiveId);
                    break;
                case "han.hip_roof":
                    mesh = CreateFrustum(primitiveId, 0.50f, 0.50f,
                        0.08f, 0.08f);
                    break;
                case "han.ritual_ring":
                    mesh = CreateRing(primitiveId, 16, 0.50f, 0.34f, 0.12f);
                    break;
                default:
                    throw new KeyNotFoundException(
                        "Unknown Han production primitive: " + primitiveId);
            }
            _meshes.Add(primitiveId, mesh);
            return mesh;
        }

        public void Dispose()
        {
            foreach (var mesh in _meshes.Values)
                if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            _meshes.Clear();
        }

        private static Mesh CreateFrustum(string id, float bottomHalfX,
            float bottomHalfZ, float topHalfX, float topHalfZ)
        {
            var vertices = new[]
            {
                new Vector3(-bottomHalfX, -0.5f, -bottomHalfZ),
                new Vector3(bottomHalfX, -0.5f, -bottomHalfZ),
                new Vector3(bottomHalfX, -0.5f, bottomHalfZ),
                new Vector3(-bottomHalfX, -0.5f, bottomHalfZ),
                new Vector3(-topHalfX, 0.5f, -topHalfZ),
                new Vector3(topHalfX, 0.5f, -topHalfZ),
                new Vector3(topHalfX, 0.5f, topHalfZ),
                new Vector3(-topHalfX, 0.5f, topHalfZ)
            };
            var triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };
            return FinalizeMesh(id, vertices, triangles);
        }

        private static Mesh CreateExtrudedAlongZ(string id,
            IReadOnlyList<Vector2> profile)
        {
            var count = profile.Count;
            var vertices = new Vector3[count * 2];
            for (var index = 0; index < count; index++)
            {
                vertices[index] = new Vector3(profile[index].x,
                    profile[index].y, -0.5f);
                vertices[count + index] = new Vector3(profile[index].x,
                    profile[index].y, 0.5f);
            }
            return FinalizeMesh(id, vertices, CreateExtrudedTriangles(count));
        }

        private static Mesh CreateExtrudedAlongX(string id,
            IReadOnlyList<Vector2> profile)
        {
            var count = profile.Count;
            var vertices = new Vector3[count * 2];
            for (var index = 0; index < count; index++)
            {
                vertices[index] = new Vector3(-0.5f, profile[index].y,
                    profile[index].x);
                vertices[count + index] = new Vector3(0.5f, profile[index].y,
                    profile[index].x);
            }
            var triangles = CreateExtrudedTriangles(count);
            for (var index = 0; index < triangles.Length; index += 3)
            {
                var swap = triangles[index + 1];
                triangles[index + 1] = triangles[index + 2];
                triangles[index + 2] = swap;
            }
            return FinalizeMesh(id, vertices, triangles);
        }

        private static int[] CreateExtrudedTriangles(int count)
        {
            var triangles = new List<int>((count - 2) * 6 + count * 6);
            for (var index = 1; index < count - 1; index++)
            {
                triangles.Add(0);
                triangles.Add(index + 1);
                triangles.Add(index);
                triangles.Add(count);
                triangles.Add(count + index);
                triangles.Add(count + index + 1);
            }
            for (var index = 0; index < count; index++)
            {
                var next = (index + 1) % count;
                triangles.Add(index);
                triangles.Add(next);
                triangles.Add(count + next);
                triangles.Add(index);
                triangles.Add(count + next);
                triangles.Add(count + index);
            }
            return triangles.ToArray();
        }

        private static Mesh CreateFoliageCluster(string id)
        {
            const int ringCount = 8;
            var vertices = new Vector3[ringCount + 2];
            vertices[0] = new Vector3(0f, 0.5f, 0f);
            vertices[1] = new Vector3(0f, -0.5f, 0f);
            for (var index = 0; index < ringCount; index++)
            {
                var angle = index * Mathf.PI * 2f / ringCount;
                vertices[index + 2] = new Vector3(Mathf.Cos(angle) * 0.5f,
                    Mathf.Sin(angle * 2f) * 0.08f,
                    Mathf.Sin(angle) * 0.5f);
            }
            var triangles = new List<int>(ringCount * 6);
            for (var index = 0; index < ringCount; index++)
            {
                var current = index + 2;
                var next = (index + 1) % ringCount + 2;
                triangles.Add(0);
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(1);
                triangles.Add(next);
                triangles.Add(current);
            }
            return FinalizeMesh(id, vertices, triangles.ToArray());
        }

        private static Mesh CreateOctagonalBeam(string id)
        {
            const int ringCount = 8;
            var vertices = new Vector3[ringCount * 2];
            for (var index = 0; index < ringCount; index++)
            {
                var angle = index * Mathf.PI * 2f / ringCount;
                var x = Mathf.Cos(angle) * 0.5f;
                var z = Mathf.Sin(angle) * 0.5f;
                vertices[index] = new Vector3(x, -0.5f, z);
                vertices[index + ringCount] = new Vector3(x, 0.5f, z);
            }
            return FinalizeMesh(id, vertices, CreateExtrudedTriangles(ringCount));
        }

        private static Mesh CreateRing(string id, int segments, float outerRadius,
            float innerRadius, float height)
        {
            var vertices = new Vector3[segments * 4];
            for (var index = 0; index < segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var x = Mathf.Cos(angle);
                var z = Mathf.Sin(angle);
                vertices[index] = new Vector3(x * outerRadius, -height * 0.5f,
                    z * outerRadius);
                vertices[segments + index] = new Vector3(x * innerRadius,
                    -height * 0.5f, z * innerRadius);
                vertices[segments * 2 + index] = new Vector3(x * outerRadius,
                    height * 0.5f, z * outerRadius);
                vertices[segments * 3 + index] = new Vector3(x * innerRadius,
                    height * 0.5f, z * innerRadius);
            }
            var triangles = new List<int>(segments * 24);
            for (var index = 0; index < segments; index++)
            {
                var next = (index + 1) % segments;
                AddQuad(triangles, segments * 2 + index, segments * 2 + next,
                    segments * 3 + index, segments * 3 + next);
                AddQuad(triangles, index, segments + index, next,
                    segments + next);
                AddQuad(triangles, index, next, segments * 2 + index,
                    segments * 2 + next);
                AddQuad(triangles, segments + next, segments + index,
                    segments * 3 + next, segments * 3 + index);
            }
            return FinalizeMesh(id, vertices, triangles.ToArray());
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c,
            int d)
        {
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(b);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(d);
        }

        private static Mesh FinalizeMesh(string id, Vector3[] vertices,
            int[] triangles)
        {
            var mesh = new Mesh { name = "HAN_PRODUCTION_" + id };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = new Vector2[vertices.Length];
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
