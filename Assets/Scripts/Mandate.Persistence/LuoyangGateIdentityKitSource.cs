using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangGateIdentityKitSource
    {
        public const string CatalogDirectory = "LuoyangGateIdentityKitV1";
        public const string FileName = "luoyang_gate_identity_kit_v1.json";

        public LuoyangGateIdentityKitSource(string worldMapRoot,
            HanBuildableFacilityModelCatalog models)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var path = Path.Combine(Root, CatalogDirectory, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Luoyang gate identity kit catalog is missing.", path);
            Catalog = JsonConvert.DeserializeObject<LuoyangGateIdentityKitCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang gate identity kit catalog cannot be deserialized.");
            LuoyangGateIdentityKitRules.Validate(Catalog, models);
        }

        public string Root { get; }
        public LuoyangGateIdentityKitCatalog Catalog { get; }
    }
}
