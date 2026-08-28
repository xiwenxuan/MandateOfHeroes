using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangBuildingPerformanceBudgetIds
    {
        public const string SchemaId =
            "mandate.luoyang-building-performance-budget.v1";
        public const string BudgetId =
            "LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1";
        public const int FacilityCount = 2084;
        public const int FacilityDefinitionCount = 61;
        public const int UniqueCellCount = 2084;
        public const int SpatialBatchEdgeCells = 8;
        public const int FullCitySpatialBatchCount = 64;
        public const int ResidentWindowEdgeCells = 24;
        public const int ResidentWindowCount = 11;
        public const int DensestWindowFirstColumn = 2040;
        public const int DensestWindowFirstRow = 1224;
        public const int DensestWindowFacilityCount = 549;
        public const int MaxResidentFacilityCount = 576;
        public const int MaxResidentSpatialBatchCount = 9;
        public const int MaxBuildingRendererBatchCount = 200;
        public const int MaxCombinedVertexCount = 250000;
        public const double MaxBatchBuildMilliseconds = 3000d;
        public const double MinRendererReductionRatio = 0.85d;
        public const int MinGridColumn = 2013;
        public const int MaxGridColumn = 2104;
        public const int MinGridRow = 1202;
        public const int MaxGridRow = 1266;
    }

    [Serializable]
    public sealed class LuoyangBuildingPerformanceBudgetCatalog
    {
        public string SchemaId;
        public string BudgetId;
        public int FacilityCount;
        public int FacilityDefinitionCount;
        public int UniqueCellCount;
        public int SpatialBatchEdgeCells;
        public int FullCitySpatialBatchCount;
        public int ResidentWindowEdgeCells;
        public int ResidentWindowCount;
        public int DensestWindowFirstColumn;
        public int DensestWindowFirstRow;
        public int DensestWindowFacilityCount;
        public int MaxResidentFacilityCount;
        public int MaxResidentSpatialBatchCount;
        public int MaxBuildingRendererBatchCount;
        public int MaxCombinedVertexCount;
        public double MaxBatchBuildMilliseconds;
        public double MinRendererReductionRatio;
    }

    [Serializable]
    public sealed class LuoyangBuildingPerformanceFacility
    {
        public string FacilityId;
        public string FacilityDefinitionId;
        public string ModelId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
        public float RotationDegrees;
    }

    public sealed class LuoyangBuildingSpatialBatch
    {
        public LuoyangBuildingSpatialBatch(int batchRow, int batchColumn,
            IReadOnlyList<LuoyangBuildingPerformanceFacility> facilities)
        {
            BatchRow = batchRow;
            BatchColumn = batchColumn;
            Facilities = facilities ?? throw new ArgumentNullException(
                nameof(facilities));
        }

        public int BatchRow { get; }
        public int BatchColumn { get; }
        public IReadOnlyList<LuoyangBuildingPerformanceFacility> Facilities { get; }
        public string BatchId => "building-batch." + BatchRow + "." + BatchColumn;
    }

    public sealed class LuoyangBuildingPerformancePlan
    {
        public LuoyangBuildingPerformancePlan(
            LuoyangBuildingPerformanceBudgetCatalog budget,
            IReadOnlyList<LuoyangBuildingPerformanceFacility> facilities,
            IReadOnlyList<LuoyangBuildingSpatialBatch> spatialBatches,
            int residentWindowCount)
        {
            Budget = budget ?? throw new ArgumentNullException(nameof(budget));
            Facilities = facilities ?? throw new ArgumentNullException(
                nameof(facilities));
            SpatialBatches = spatialBatches ?? throw new ArgumentNullException(
                nameof(spatialBatches));
            ResidentWindowCount = residentWindowCount;
        }

        public LuoyangBuildingPerformanceBudgetCatalog Budget { get; }
        public IReadOnlyList<LuoyangBuildingPerformanceFacility> Facilities { get; }
        public IReadOnlyList<LuoyangBuildingSpatialBatch> SpatialBatches { get; }
        public int ResidentWindowCount { get; }
    }

    public sealed class LuoyangBuildingResidentWindow
    {
        public LuoyangBuildingResidentWindow(int firstRow, int firstColumn,
            int edgeCells,
            IReadOnlyList<LuoyangBuildingPerformanceFacility> facilities,
            IReadOnlyList<LuoyangBuildingSpatialBatch> spatialBatches)
        {
            FirstRow = firstRow;
            FirstColumn = firstColumn;
            EdgeCells = edgeCells;
            Facilities = facilities ?? throw new ArgumentNullException(
                nameof(facilities));
            SpatialBatches = spatialBatches ?? throw new ArgumentNullException(
                nameof(spatialBatches));
        }

        public int FirstRow { get; }
        public int FirstColumn { get; }
        public int EdgeCells { get; }
        public IReadOnlyList<LuoyangBuildingPerformanceFacility> Facilities { get; }
        public IReadOnlyList<LuoyangBuildingSpatialBatch> SpatialBatches { get; }
    }

    public static class LuoyangBuildingPerformanceRules
    {
        public static void ValidateBudget(
            LuoyangBuildingPerformanceBudgetCatalog budget)
        {
            if (budget == null) throw new ArgumentNullException(nameof(budget));
            if (!string.Equals(budget.SchemaId,
                    LuoyangBuildingPerformanceBudgetIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(budget.BudgetId,
                    LuoyangBuildingPerformanceBudgetIds.BudgetId,
                    StringComparison.Ordinal) ||
                budget.FacilityCount !=
                    LuoyangBuildingPerformanceBudgetIds.FacilityCount ||
                budget.FacilityDefinitionCount !=
                    LuoyangBuildingPerformanceBudgetIds.FacilityDefinitionCount ||
                budget.UniqueCellCount !=
                    LuoyangBuildingPerformanceBudgetIds.UniqueCellCount ||
                budget.SpatialBatchEdgeCells !=
                    LuoyangBuildingPerformanceBudgetIds.SpatialBatchEdgeCells ||
                budget.FullCitySpatialBatchCount !=
                    LuoyangBuildingPerformanceBudgetIds.FullCitySpatialBatchCount ||
                budget.ResidentWindowEdgeCells !=
                    LuoyangBuildingPerformanceBudgetIds.ResidentWindowEdgeCells ||
                budget.ResidentWindowCount !=
                    LuoyangBuildingPerformanceBudgetIds.ResidentWindowCount ||
                budget.DensestWindowFirstColumn !=
                    LuoyangBuildingPerformanceBudgetIds.DensestWindowFirstColumn ||
                budget.DensestWindowFirstRow !=
                    LuoyangBuildingPerformanceBudgetIds.DensestWindowFirstRow ||
                budget.DensestWindowFacilityCount !=
                    LuoyangBuildingPerformanceBudgetIds.DensestWindowFacilityCount ||
                budget.MaxResidentFacilityCount !=
                    LuoyangBuildingPerformanceBudgetIds.MaxResidentFacilityCount ||
                budget.MaxResidentSpatialBatchCount !=
                    LuoyangBuildingPerformanceBudgetIds
                        .MaxResidentSpatialBatchCount ||
                budget.MaxBuildingRendererBatchCount !=
                    LuoyangBuildingPerformanceBudgetIds
                        .MaxBuildingRendererBatchCount ||
                budget.MaxCombinedVertexCount !=
                    LuoyangBuildingPerformanceBudgetIds.MaxCombinedVertexCount ||
                Math.Abs(budget.MaxBatchBuildMilliseconds -
                         LuoyangBuildingPerformanceBudgetIds
                             .MaxBatchBuildMilliseconds) > 0.000001d ||
                Math.Abs(budget.MinRendererReductionRatio -
                         LuoyangBuildingPerformanceBudgetIds
                             .MinRendererReductionRatio) > 0.000001d)
                throw new InvalidOperationException(
                    "Invalid Luoyang building performance budget.");

            if (budget.SpatialBatchEdgeCells <= 0 ||
                budget.ResidentWindowEdgeCells <= 0 ||
                budget.ResidentWindowEdgeCells % budget.SpatialBatchEdgeCells != 0 ||
                budget.MaxResidentFacilityCount >
                    budget.ResidentWindowEdgeCells * budget.ResidentWindowEdgeCells ||
                budget.MaxResidentSpatialBatchCount !=
                    (budget.ResidentWindowEdgeCells / budget.SpatialBatchEdgeCells) *
                    (budget.ResidentWindowEdgeCells / budget.SpatialBatchEdgeCells) ||
                budget.MaxBuildingRendererBatchCount <= 0 ||
                budget.MaxCombinedVertexCount <= 0 ||
                budget.MaxBatchBuildMilliseconds <= 0d ||
                budget.MinRendererReductionRatio <= 0d ||
                budget.MinRendererReductionRatio >= 1d)
                throw new InvalidOperationException(
                    "Invalid Luoyang building performance budget ranges.");
        }

        public static LuoyangBuildingPerformancePlan CreatePlan(
            LuoyangBuildingPerformanceBudgetCatalog budget,
            IEnumerable<LuoyangBuildingPerformanceFacility> source,
            HanBuildableFacilityModelCatalog models)
        {
            ValidateBudget(budget);
            HanBuildableFacilityModelCatalogRules.Validate(models);
            if (source == null) throw new ArgumentNullException(nameof(source));
            var modelIds = new HashSet<string>(models.Models.Select(item =>
                item.ModelId), StringComparer.Ordinal);
            var facilities = source.OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal).ToArray();
            if (facilities.Length != budget.FacilityCount)
                throw new InvalidOperationException(
                    "Luoyang building performance plan has the wrong Facility count.");

            var facilityIds = new HashSet<string>(StringComparer.Ordinal);
            var cellIds = new HashSet<ulong>();
            var definitions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var facility in facilities)
            {
                if (facility == null || string.IsNullOrWhiteSpace(facility.FacilityId) ||
                    !facilityIds.Add(facility.FacilityId) ||
                    string.IsNullOrWhiteSpace(facility.FacilityDefinitionId) ||
                    !modelIds.Contains(facility.ModelId ?? string.Empty) ||
                    facility.CellId64 == 0 || !cellIds.Add(facility.CellId64) ||
                    facility.GridColumn < LuoyangBuildingPerformanceBudgetIds.MinGridColumn ||
                    facility.GridColumn > LuoyangBuildingPerformanceBudgetIds.MaxGridColumn ||
                    facility.GridRow < LuoyangBuildingPerformanceBudgetIds.MinGridRow ||
                    facility.GridRow > LuoyangBuildingPerformanceBudgetIds.MaxGridRow ||
                    float.IsNaN(facility.RotationDegrees) ||
                    float.IsInfinity(facility.RotationDegrees) ||
                    facility.RotationDegrees < 0f ||
                    facility.RotationDegrees >= 360f ||
                    Math.Abs(facility.RotationDegrees % 90f) > 0.001f)
                    throw new InvalidOperationException(
                        "Invalid Luoyang building performance Facility.");
                definitions.Add(facility.FacilityDefinitionId);
            }
            if (cellIds.Count != budget.UniqueCellCount ||
                definitions.Count != budget.FacilityDefinitionCount)
                throw new InvalidOperationException(
                    "Luoyang building performance Facility coverage is incomplete.");

            var batches = facilities.GroupBy(item => new
                {
                    Row = item.GridRow / budget.SpatialBatchEdgeCells,
                    Column = item.GridColumn / budget.SpatialBatchEdgeCells
                })
                .OrderBy(group => group.Key.Row)
                .ThenBy(group => group.Key.Column)
                .Select(group => new LuoyangBuildingSpatialBatch(group.Key.Row,
                    group.Key.Column, group.ToArray())).ToArray();
            if (batches.Length != budget.FullCitySpatialBatchCount ||
                batches.Any(item => item.Facilities.Count <= 0 ||
                    item.Facilities.Count > budget.SpatialBatchEdgeCells *
                    budget.SpatialBatchEdgeCells))
                throw new InvalidOperationException(
                    "Invalid Luoyang building spatial batches.");

            var residentWindowCount = facilities.GroupBy(item => new
                {
                    Row = item.GridRow / budget.ResidentWindowEdgeCells,
                    Column = item.GridColumn / budget.ResidentWindowEdgeCells
                }).Count();
            if (residentWindowCount != budget.ResidentWindowCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang building resident-window count.");
            var plan = new LuoyangBuildingPerformancePlan(budget, facilities,
                batches, residentWindowCount);
            var densest = SelectResidentWindow(plan,
                budget.DensestWindowFirstRow, budget.DensestWindowFirstColumn);
            if (densest.Facilities.Count != budget.DensestWindowFacilityCount)
                throw new InvalidOperationException(
                    "Luoyang densest building window does not match the budget.");
            return plan;
        }

        public static LuoyangBuildingResidentWindow SelectDensestResidentWindow(
            LuoyangBuildingPerformancePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            return SelectResidentWindow(plan, plan.Budget.DensestWindowFirstRow,
                plan.Budget.DensestWindowFirstColumn);
        }

        public static LuoyangBuildingResidentWindow SelectResidentWindow(
            LuoyangBuildingPerformancePlan plan, int firstRow, int firstColumn)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var edge = plan.Budget.ResidentWindowEdgeCells;
            if (firstRow < 0 || firstColumn < 0)
                throw new ArgumentOutOfRangeException(nameof(firstRow));
            var facilities = plan.Facilities.Where(item =>
                    item.GridRow >= firstRow && item.GridRow < firstRow + edge &&
                    item.GridColumn >= firstColumn &&
                    item.GridColumn < firstColumn + edge)
                .ToArray();
            var keys = new HashSet<string>(facilities.Select(item =>
                (item.GridRow / plan.Budget.SpatialBatchEdgeCells) + ":" +
                (item.GridColumn / plan.Budget.SpatialBatchEdgeCells)),
                StringComparer.Ordinal);
            var batches = plan.SpatialBatches.Where(item => keys.Contains(
                item.BatchRow + ":" + item.BatchColumn)).ToArray();
            if (facilities.Length > plan.Budget.MaxResidentFacilityCount ||
                batches.Length > plan.Budget.MaxResidentSpatialBatchCount)
                throw new InvalidOperationException(
                    "Luoyang building resident window exceeds its budget.");
            return new LuoyangBuildingResidentWindow(firstRow, firstColumn, edge,
                facilities, batches);
        }
    }
}
