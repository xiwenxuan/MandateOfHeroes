using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangFinalAssetReviewManifestSource
    {
        public const string CatalogDirectory = "LuoyangFinalAssetReviewManifestV1";
        public const string FileName =
            "luoyang_final_asset_review_manifest_v1.json";

        public LuoyangFinalAssetReviewManifestSource(string worldMapRoot,
            LuoyangProductionBuildingKitCatalog production,
            LuoyangHistoricalLandmarkKitCatalog landmarks,
            LuoyangGateIdentityKitCatalog gates,
            LuoyangMediumFrequencyUrbanFabricKitCatalog urbanFabric,
            LuoyangInfrastructureProductionKitCatalog infrastructure,
            LuoyangLowFrequencyDefenseProductionKitCatalog defense,
            LuoyangResourceAgricultureProductionKitCatalog resourceAgriculture,
            LuoyangFinalCivicRitualMedicalProductionKitCatalog finalCivic,
            LuoyangBuildingPerformancePlan wholeCity)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("WorldMap root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var path = Path.Combine(Root, CatalogDirectory, FileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Luoyang final-asset review manifest is missing.", path);
            Catalog = JsonConvert.DeserializeObject<
                          LuoyangFinalAssetReviewCatalog>(File.ReadAllText(path)) ??
                      throw new InvalidDataException(
                          "Luoyang final-asset review manifest cannot be deserialized.");
            Plan = LuoyangFinalAssetReviewRules.CreatePlan(Catalog, production,
                landmarks, gates, urbanFabric, infrastructure, defense,
                resourceAgriculture, finalCivic, wholeCity);
        }

        public string Root { get; }
        public LuoyangFinalAssetReviewCatalog Catalog { get; }
        public LuoyangFinalAssetReviewPlan Plan { get; }
    }
}
