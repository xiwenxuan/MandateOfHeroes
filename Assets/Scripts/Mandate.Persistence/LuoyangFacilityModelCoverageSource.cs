using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangFacilityModelCoverageSource
    {
        public const string BaseCatalogDirectory = "HanBuildableFacilityModelKitV1";
        public const string CoverageCatalogDirectory = "LuoyangFacilityModelCoverageV1";
        public const string BindingFileName = "luoyang_facility_model_bindings_v1.json";

        public LuoyangFacilityModelCoverageSource(string worldMapRoot)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var baseCatalog = new HanBuildableFacilityModelCatalogSource(
                Path.Combine(Root, BaseCatalogDirectory)).Catalog;
            SupplementalCatalog = new HanBuildableFacilityModelCatalogSource(
                Path.Combine(Root, CoverageCatalogDirectory)).Catalog;
            CombinedCatalog = HanBuildableFacilityModelCatalogComposer.Combine(
                baseCatalog, SupplementalCatalog);

            var bindingPath = Path.Combine(Root, CoverageCatalogDirectory,
                BindingFileName);
            if (!File.Exists(bindingPath))
                throw new FileNotFoundException(
                    "Luoyang Facility model binding catalog is missing.", bindingPath);
            Bindings = JsonConvert.DeserializeObject<
                LuoyangFacilityModelBindingCatalog>(File.ReadAllText(bindingPath)) ??
                throw new InvalidDataException(
                    "Luoyang Facility model bindings cannot be deserialized.");
            LuoyangFacilityModelBindingRules.Validate(Bindings, CombinedCatalog);
        }

        public string Root { get; }
        public HanBuildableFacilityModelCatalog SupplementalCatalog { get; }
        public HanBuildableFacilityModelCatalog CombinedCatalog { get; }
        public LuoyangFacilityModelBindingCatalog Bindings { get; }
    }

    public sealed class LuoyangProductionBuildingKitSource
    {
        public const string CatalogDirectory = "LuoyangProductionBuildingKitV1";
        public const string FileName = "luoyang_production_building_kit_v1.json";

        public LuoyangProductionBuildingKitSource(string worldMapRoot,
            HanBuildableFacilityModelCatalog models)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var path = Path.Combine(Root, CatalogDirectory, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Luoyang production building kit catalog is missing.", path);
            Catalog = JsonConvert.DeserializeObject<
                LuoyangProductionBuildingKitCatalog>(File.ReadAllText(path)) ??
                throw new InvalidDataException(
                    "Luoyang production building kit catalog cannot be deserialized.");
            LuoyangProductionBuildingKitRules.Validate(Catalog, models);
        }

        public string Root { get; }
        public LuoyangProductionBuildingKitCatalog Catalog { get; }
    }

    public sealed class LuoyangHistoricalLandmarkKitSource
    {
        public const string CatalogDirectory = "LuoyangHistoricalLandmarkKitV1";
        public const string FileName = "luoyang_historical_landmark_kit_v1.json";

        public LuoyangHistoricalLandmarkKitSource(string worldMapRoot,
            HanBuildableFacilityModelCatalog models)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var path = Path.Combine(Root, CatalogDirectory, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Luoyang historical landmark kit catalog is missing.", path);
            Catalog = JsonConvert.DeserializeObject<
                LuoyangHistoricalLandmarkKitCatalog>(File.ReadAllText(path)) ??
                throw new InvalidDataException(
                    "Luoyang historical landmark kit catalog cannot be deserialized.");
            LuoyangHistoricalLandmarkKitRules.Validate(Catalog, models);
        }

        public string Root { get; }
        public LuoyangHistoricalLandmarkKitCatalog Catalog { get; }
    }
}
