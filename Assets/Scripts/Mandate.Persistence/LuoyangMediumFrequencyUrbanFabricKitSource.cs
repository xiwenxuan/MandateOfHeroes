using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangMediumFrequencyUrbanFabricKitSource
    {
        public const string CatalogDirectory =
            "LuoyangMediumFrequencyUrbanFabricKitV1";
        public const string FileName =
            "luoyang_medium_frequency_urban_fabric_kit_v1.json";

        public LuoyangMediumFrequencyUrbanFabricKitSource(string worldMapRoot,
            HanBuildableFacilityModelCatalog models)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var path = Path.Combine(Root, CatalogDirectory, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Luoyang medium-frequency urban-fabric kit is missing.", path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangMediumFrequencyUrbanFabricKitCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang medium-frequency urban-fabric kit cannot be deserialized.");
            LuoyangMediumFrequencyUrbanFabricKitRules.Validate(Catalog, models);
        }

        public string Root { get; }
        public LuoyangMediumFrequencyUrbanFabricKitCatalog Catalog { get; }
    }
}
