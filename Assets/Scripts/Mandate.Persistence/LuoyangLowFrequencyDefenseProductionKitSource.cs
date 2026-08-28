using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangLowFrequencyDefenseProductionKitSource
    {
        public const string CatalogDirectory =
            "LuoyangLowFrequencyDefenseProductionKitV1";
        public const string FileName =
            "luoyang_low_frequency_defense_production_kit_v1.json";

        public LuoyangLowFrequencyDefenseProductionKitSource(
            string worldMapRoot, HanBuildableFacilityModelCatalog models,
            LuoyangGateIdentityKitCatalog gates,
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
                    "Luoyang low-frequency defense production kit is missing.",
                    path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangLowFrequencyDefenseProductionKitCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang low-frequency defense production kit cannot be deserialized.");
            LuoyangLowFrequencyDefenseProductionKitRules.Validate(Catalog,
                models, gates);

            var facilities = fullCityPlan.Facilities.Where(item =>
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .ModelByDefinition.ContainsKey(
                            item.FacilityDefinitionId))
                .Select(item => new LuoyangLowFrequencyDefenseFacility
                {
                    FacilityId = item.FacilityId,
                    FacilityDefinitionId = item.FacilityDefinitionId,
                    ModelId = item.ModelId,
                    CellId64 = item.CellId64,
                    GridColumn = item.GridColumn,
                    GridRow = item.GridRow
                }).ToArray();
            Plan = LuoyangLowFrequencyDefenseProductionKitRules.CreatePlan(
                Catalog, facilities, gates);
        }

        public string Root { get; }
        public LuoyangLowFrequencyDefenseProductionKitCatalog Catalog { get; }
        public LuoyangLowFrequencyDefenseProductionPlan Plan { get; }
    }
}
