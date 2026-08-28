using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangP0LandmarkSecondBatchSource
    {
        public const string CatalogDirectory =
            "LuoyangP0LandmarkSecondBatchV1";
        public const string FileName =
            "luoyang_p0_landmark_second_batch_v1.json";

        public LuoyangP0LandmarkSecondBatchSource(string worldMapRoot,
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
                    "Luoyang P0 landmark second-batch catalog is missing.",
                    path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangP0LandmarkSecondBatchCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang P0 landmark second-batch catalog cannot be deserialized.");
            Plan = LuoyangP0LandmarkSecondBatchRules.CreatePlan(Catalog,
                models, landmarks, review);
        }

        public string Root { get; }
        public LuoyangP0LandmarkSecondBatchCatalog Catalog { get; }
        public LuoyangP0LandmarkSecondBatchPlan Plan { get; }
    }
}
