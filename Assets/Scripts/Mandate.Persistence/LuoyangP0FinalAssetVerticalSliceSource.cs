using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangP0FinalAssetVerticalSliceSource
    {
        public const string CatalogDirectory =
            "LuoyangP0FinalAssetVerticalSliceV1";
        public const string FileName =
            "luoyang_p0_final_asset_vertical_slice_v1.json";

        public LuoyangP0FinalAssetVerticalSliceSource(string worldMapRoot,
            HanBuildableFacilityModelCatalog models,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangFinalAssetReviewCatalog finalAssetReview)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var path = Path.Combine(Root, CatalogDirectory, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Luoyang P0 final-asset vertical-slice catalog is missing.",
                    path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangP0FinalAssetVerticalSliceCatalog>(
                          File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang P0 final-asset catalog cannot be deserialized.");
            Plan = LuoyangP0FinalAssetVerticalSliceRules.CreatePlan(Catalog,
                models, landmarks, gates, finalAssetReview);
        }

        public string Root { get; }
        public LuoyangP0FinalAssetVerticalSliceCatalog Catalog { get; }
        public LuoyangP0FinalAssetVerticalSlicePlan Plan { get; }
    }
}
