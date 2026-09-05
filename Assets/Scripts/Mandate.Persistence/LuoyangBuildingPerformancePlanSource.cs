using System;
using System.Collections.Generic;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mandate.Persistence
{
    public sealed class LuoyangBuildingPerformancePlanSource
    {
        public const string BudgetDirectory =
            "LuoyangBuildingPerformanceBudgetV1";
        public const string BudgetFileName =
            "luoyang_building_performance_budget_v1.json";

        private static readonly string[] FacilityFiles =
        {
            Path.Combine("Luoyang184UrbanInitializationV1", "facilities.json"),
            Path.Combine("Luoyang184MetropolitanInitializationV1", "facilities.json")
        };

        public LuoyangBuildingPerformancePlanSource(string worldMapRoot,
            LuoyangFacilityModelBindingCatalog bindings,
            HanBuildableFacilityModelCatalog models)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var budgetPath = Path.Combine(Root, BudgetDirectory, BudgetFileName);
            if (!File.Exists(budgetPath))
                throw new FileNotFoundException(
                    "Luoyang building performance budget is missing.", budgetPath);
            Budget = JsonConvert.DeserializeObject<
                         LuoyangBuildingPerformanceBudgetCatalog>(
                         File.ReadAllText(budgetPath)) ??
                     throw new InvalidDataException(
                         "Luoyang building performance budget cannot be deserialized.");
            LuoyangBuildingPerformanceRules.ValidateBudget(Budget);
            var resolver = new LuoyangFacilityModelBindingResolver(bindings,
                models);
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var facilities = new List<LuoyangBuildingPerformanceFacility>(
                Budget.FacilityCount);
            foreach (var relativePath in FacilityFiles)
            {
                var path = Path.Combine(Root, relativePath);
                if (!File.Exists(path))
                    throw new FileNotFoundException(
                        "Luoyang opening Facility data is missing.", path);
                var root = JObject.Parse(File.ReadAllText(path));
                var array = root["facilities"] as JArray ??
                            throw new InvalidDataException(
                                "Luoyang opening Facility array is missing.");
                foreach (var token in array)
                {
                    var facilityId = Text(token, "facility_id");
                    var definitionId = Text(token, "definition_id");
                    var cellId64 = token["cell_id64"]?.Value<ulong>() ?? 0;
                    var gridColumn = token["grid_x"]?.Value<int>() ?? -1;
                    var gridRow = token["grid_y"]?.Value<int>() ?? -1;
                    var modelId = resolver.ResolveModelId(definitionId,
                        facilityId);
                    if (string.IsNullOrWhiteSpace(modelId))
                        throw new InvalidDataException(
                            "Luoyang opening Facility has no explicit model binding: " +
                            facilityId);
                    if (!grid.Contains(gridRow, gridColumn) ||
                        grid.ToCellId(gridRow, gridColumn).Value != cellId64)
                        throw new InvalidDataException(
                            "Luoyang opening Facility Cell/grid mismatch: " +
                            facilityId);
                    facilities.Add(new LuoyangBuildingPerformanceFacility
                    {
                        FacilityId = facilityId,
                        FacilityDefinitionId = definitionId,
                        DisplayName = Text(token, "display_name"),
                        CategoryId = OptionalText(token, "category_id"),
                        HistoricalConfidenceId = OptionalText(token,
                            "historical_confidence"),
                        SpatialPrecisionId = OptionalText(token,
                            "spatial_precision"),
                        ModelId = modelId,
                        CellId64 = cellId64,
                        GridColumn = gridColumn,
                        GridRow = gridRow,
                        RotationDegrees = cellId64 % 4 * 90f
                    });
                }
            }
            Plan = LuoyangBuildingPerformanceRules.CreatePlan(Budget, facilities,
                models);
        }

        public string Root { get; }
        public LuoyangBuildingPerformanceBudgetCatalog Budget { get; }
        public LuoyangBuildingPerformancePlan Plan { get; }

        private static string Text(JToken token, string property)
        {
            var value = token[property]?.Value<string>();
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException(
                    "Luoyang opening Facility is missing " + property + ".");
            return value;
        }

        private static string OptionalText(JToken token, string property) =>
            token[property]?.Value<string>()?.Trim() ?? string.Empty;
    }
}
