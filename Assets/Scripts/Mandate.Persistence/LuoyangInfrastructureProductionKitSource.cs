using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangInfrastructureProductionKitSource
    {
        public const string CatalogDirectory =
            "LuoyangInfrastructureProductionKitV1";
        public const string FileName =
            "luoyang_infrastructure_production_kit_v1.json";

        public LuoyangInfrastructureProductionKitSource(string worldMapRoot,
            HanBuildableFacilityModelCatalog models,
            LuoyangBuildingPerformancePlan fullCityPlan)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            if (fullCityPlan == null)
                throw new ArgumentNullException(nameof(fullCityPlan));
            Root = Path.GetFullPath(worldMapRoot);
            var path = Path.Combine(Root, CatalogDirectory, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Luoyang infrastructure production kit is missing.", path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangInfrastructureProductionKitCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang infrastructure production kit cannot be deserialized.");
            LuoyangInfrastructureProductionKitRules.Validate(Catalog, models);

            var facilities = fullCityPlan.Facilities.Where(item =>
                    LuoyangInfrastructureProductionKitIds.ModelsByDefinition
                        .ContainsKey(item.FacilityDefinitionId))
                .Select(item => new LuoyangInfrastructureFacility
                {
                    FacilityId = item.FacilityId,
                    FacilityDefinitionId = item.FacilityDefinitionId,
                    ModelId = item.ModelId,
                    CellId64 = item.CellId64,
                    GridColumn = item.GridColumn,
                    GridRow = item.GridRow
                }).ToArray();
            Plan = LuoyangInfrastructureProductionKitRules.CreatePlan(Catalog,
                facilities);
        }

        public string Root { get; }
        public LuoyangInfrastructureProductionKitCatalog Catalog { get; }
        public LuoyangInfrastructureProductionPlan Plan { get; }
    }
}
