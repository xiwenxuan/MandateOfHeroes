using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangP0LandmarkThirdBatchSource
    {
        public const string CatalogDirectory =
            "LuoyangP0LandmarkThirdBatchV1";
        public const string FileName =
            "luoyang_p0_landmark_third_batch_v1.json";

        public LuoyangP0LandmarkThirdBatchSource(string worldMapRoot,
            HanBuildableFacilityModelCatalog models,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangFinalAssetReviewCatalog review)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var path = Path.Combine(Root, CatalogDirectory, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Luoyang P0 landmark third-batch catalog is missing.",
                    path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangP0LandmarkThirdBatchCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang P0 landmark third-batch catalog cannot be deserialized.");
            Plan = LuoyangP0LandmarkThirdBatchRules.CreatePlan(Catalog,
                models, landmarks, review);
        }

        public string Root { get; }
        public LuoyangP0LandmarkThirdBatchCatalog Catalog { get; }
        public LuoyangP0LandmarkThirdBatchPlan Plan { get; }
    }
}
