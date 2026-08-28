using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangFinalCivicRitualMedicalProductionKitSource
    {
        public const string CatalogDirectory =
            "LuoyangFinalCivicRitualMedicalProductionKitV1";
        public const string FileName =
            "luoyang_final_civic_ritual_medical_production_kit_v1.json";

        public LuoyangFinalCivicRitualMedicalProductionKitSource(
            string worldMapRoot, HanBuildableFacilityModelCatalog models,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
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
                    "Luoyang final civic production kit is missing.", path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangFinalCivicRitualMedicalProductionKitCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang final civic production kit cannot be deserialized.");
            LuoyangFinalCivicRitualMedicalProductionKitRules.Validate(Catalog,
                models, landmarks);

            var selectedIds = new HashSet<string>(Catalog.Profiles.SelectMany(
                item => item.FacilityIds), StringComparer.Ordinal);
            var facilities = fullCityPlan.Facilities.Where(item =>
                    selectedIds.Contains(item.FacilityId))
                .Select(item => new LuoyangFinalCivicRitualMedicalFacility
                {
                    FacilityId = item.FacilityId,
                    FacilityDefinitionId = item.FacilityDefinitionId,
                    ModelId = item.ModelId,
                    CellId64 = item.CellId64,
                    GridColumn = item.GridColumn,
                    GridRow = item.GridRow
                }).ToArray();
            Plan = LuoyangFinalCivicRitualMedicalProductionKitRules.CreatePlan(
                Catalog, facilities, landmarks);
        }

        public string Root { get; }
        public LuoyangFinalCivicRitualMedicalProductionKitCatalog Catalog
            { get; }
        public LuoyangFinalCivicRitualMedicalProductionPlan Plan { get; }
    }
}
