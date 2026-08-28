using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangRemainingFinalAssetSource
    {
        public const string CatalogDirectory =
            "LuoyangRemainingFinalAssetsV1";
        public const string FileName =
            "luoyang_remaining_final_assets_v1.json";

        public LuoyangRemainingFinalAssetSource(string worldMapRoot,
            LuoyangFinalAssetReviewCatalog review)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var path = Path.Combine(Root, CatalogDirectory, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Luoyang remaining final-asset catalog is missing.", path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangRemainingFinalAssetCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang remaining final-asset catalog cannot be deserialized.");
            Plan = LuoyangRemainingFinalAssetRules.CreatePlan(Catalog, review);
        }

        public string Root { get; }
        public LuoyangRemainingFinalAssetCatalog Catalog { get; }
        public LuoyangRemainingFinalAssetPlan Plan { get; }
    }
}
