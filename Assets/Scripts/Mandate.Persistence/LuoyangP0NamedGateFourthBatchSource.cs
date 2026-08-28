using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangP0NamedGateFourthBatchSource
    {
        public const string CatalogDirectory =
            "LuoyangP0NamedGateFourthBatchV1";
        public const string FileName =
            "luoyang_p0_named_gate_fourth_batch_v1.json";

        public LuoyangP0NamedGateFourthBatchSource(string worldMapRoot,
            HanBuildableFacilityModelCatalog models,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangFinalAssetReviewCatalog review)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var path = Path.Combine(Root, CatalogDirectory, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Luoyang P0 named-gate fourth-batch catalog is missing.",
                    path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangP0NamedGateFourthBatchCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang P0 named-gate fourth-batch catalog cannot be deserialized.");
            Plan = LuoyangP0NamedGateFourthBatchRules.CreatePlan(Catalog,
                models, gates, review);
        }

        public string Root { get; }
        public LuoyangP0NamedGateFourthBatchCatalog Catalog { get; }
        public LuoyangP0NamedGateFourthBatchPlan Plan { get; }
    }
}
