using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangResourceAgricultureProductionKitSource
    {
        public const string CatalogDirectory =
            "LuoyangResourceAgricultureProductionKitV1";
        public const string FileName =
            "luoyang_resource_agriculture_production_kit_v1.json";

        public LuoyangResourceAgricultureProductionKitSource(
            string worldMapRoot, HanBuildableFacilityModelCatalog models,
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
                    "Luoyang resource/agriculture production kit is missing.",
                    path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangResourceAgricultureProductionKitCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang resource/agriculture kit cannot be deserialized.");
            LuoyangResourceAgricultureProductionKitRules.Validate(Catalog,
                models);

            var facilities = fullCityPlan.Facilities.Where(item =>
                    LuoyangResourceAgricultureProductionKitIds
                        .ModelByDefinition.ContainsKey(
                            item.FacilityDefinitionId))
                .Select(item => new LuoyangResourceAgricultureFacility
                {
                    FacilityId = item.FacilityId,
                    FacilityDefinitionId = item.FacilityDefinitionId,
                    ModelId = item.ModelId,
                    CellId64 = item.CellId64,
                    GridColumn = item.GridColumn,
                    GridRow = item.GridRow
                }).ToArray();
            Plan = LuoyangResourceAgricultureProductionKitRules.CreatePlan(
                Catalog, facilities);
        }

        public string Root { get; }
        public LuoyangResourceAgricultureProductionKitCatalog Catalog { get; }
        public LuoyangResourceAgricultureProductionPlan Plan { get; }
    }
}
