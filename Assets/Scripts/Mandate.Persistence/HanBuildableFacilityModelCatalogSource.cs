using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class HanBuildableFacilityModelCatalogSource
    {
        public const string FileName =
            "han_buildable_facility_model_catalog_v1.json";

        public HanBuildableFacilityModelCatalogSource(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
                throw new ArgumentException("Model catalog root is required.",
                    nameof(root));
            Root = Path.GetFullPath(root);
            var path = Path.Combine(Root, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Han buildable Facility model catalog is missing.", path);
            Catalog = JsonConvert.DeserializeObject<
                HanBuildableFacilityModelCatalog>(File.ReadAllText(path)) ??
                throw new InvalidDataException(
                    "Han buildable Facility model catalog cannot be deserialized.");
            HanBuildableFacilityModelCatalogRules.Validate(Catalog);
        }

        public string Root { get; }
        public HanBuildableFacilityModelCatalog Catalog { get; }
    }
}
