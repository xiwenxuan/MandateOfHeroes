using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Mandate.Domain
{
    public enum ProductionControlMode : byte
    {
        PersonalLabor,
        DirectAssignment,
        WorkOrder,
        TargetInstruction,
        DelegatedPolicy
    }

    public enum ProductionOrderStatus : byte
    {
        Active,
        Completed,
        Failed,
        Cancelled
    }

    public enum ProductionLedgerEntryType : byte
    {
        InputCommitted,
        LaborCommitted,
        ProductStored,
        ProductLost
    }

    [Serializable]
    public sealed class ProductionContentPackageDefinition
    {
        public string PackageId;
        public string Version;
        public int LoadOrder;
        public bool Required = true;
        public List<CropDefinition> Crops = new List<CropDefinition>();
        public List<CropVarietyDefinition> CropVarieties =
            new List<CropVarietyDefinition>();
        public List<QualityDimensionDefinition> QualityDimensions =
            new List<QualityDimensionDefinition>();
        public List<ProductDefinition> Products = new List<ProductDefinition>();
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<FoodDefinition> Foods;
        public List<RecipeDefinition> Recipes = new List<RecipeDefinition>();
        public List<ProductionMethodDefinition> Methods =
            new List<ProductionMethodDefinition>();
        public List<SkillDefinition> Skills = new List<SkillDefinition>();
        public List<KnowledgeDefinition> Knowledge =
            new List<KnowledgeDefinition>();
        public List<TechnologyDefinition> Technologies =
            new List<TechnologyDefinition>();
    }

    [Serializable]
    public sealed class CropDefinition
    {
        public string Id;
        public string DisplayName;
        public List<string> UsageTags = new List<string>();
        public string HistoricalStatus;
        public string SourceNote;
    }

    [Serializable]
    public sealed class CropVarietyDefinition
    {
        public string Id;
        public string CropDefinitionId;
        public string DisplayName;
        public string Provenance;
        public List<string> TraitIds = new List<string>();
    }

    [Serializable]
    public sealed class QualityDimensionDefinition
    {
        public string Id;
        public string DisplayName;
        public string HistoricalStatus;
        public string SourceNote;
    }

    [Serializable]
    public sealed class ProductDefinition
    {
        public string Id;
        public string DisplayName;
        public string UnitId;
        public List<string> CategoryTags = new List<string>();
        public List<string> QualityDimensionIds = new List<string>();
        public int BaseWeight = 1;
        public int PerishabilityBasisPoints;
    }

    [Serializable]
    public sealed class FoodDefinition
    {
        public string ProductDefinitionId;
        public int OpeningShareBasisPoints;
        public int NutritionBasisPoints = 10_000;
        public int VolumeBasisPoints = 10_000;
        public int SpoilageSensitivityBasisPoints = 10_000;
        public int MarketValueBasisPoints = 10_000;
        public int ConsumptionPriority;
        public string SourceLayer;
        public string SourceNote;
    }

    [Serializable]
    public sealed class ProductionQuantityDefinition
    {
        public string ProductDefinitionId;
        public long QuantityPerLandUnit;
    }

    [Serializable]
    public sealed class RecipeDefinition
    {
        public string Id;
        public string DisplayName;
        public string CropDefinitionId;
        public int DurationDays;
        public List<string> FacilityTags = new List<string>();
        public List<ProductionQuantityDefinition> Inputs =
            new List<ProductionQuantityDefinition>();
        public List<ProductionQuantityDefinition> Outputs =
            new List<ProductionQuantityDefinition>();
    }

    [Serializable]
    public sealed class ProductionMethodDefinition
    {
        public string Id;
        public string DisplayName;
        public List<string> RecipeDefinitionIds = new List<string>();
        public string PracticeSkillDefinitionId;
        public int PracticeDifficultyBasisPoints = 10_000;
        public List<QualityDimensionModifierDefinition>
            QualityDimensionModifiers =
                new List<QualityDimensionModifierDefinition>();
        public int YieldBasisPoints = 10_000;
        public int LaborBasisPoints = 10_000;
        public string HistoricalStatus;
    }

    [Serializable]
    public sealed class QualityDimensionModifierDefinition
    {
        public string QualityDimensionId;
        public int ModifierBasisPoints;
    }

    [Serializable]
    public sealed class ProductionContentPackageManifestState
    {
        public string PackageId;
        public string Version;
        public int LoadOrder;
        public bool Required;
        public string ContentHash;
    }

    [Serializable]
    public sealed class ProductionContentManifestState
    {
        public int ContentSchemaVersion = 1;
        public string ResolvedHash;
        public List<ProductionContentPackageManifestState> Packages =
            new List<ProductionContentPackageManifestState>();
    }

    public sealed class ProductionContentException : InvalidOperationException
    {
        public ProductionContentException(string message)
            : base(message)
        {
        }
    }

    public sealed class ProductionContentRegistry
    {
        private Dictionary<string, CropDefinition> _crops =
            new Dictionary<string, CropDefinition>(StringComparer.Ordinal);
        private Dictionary<string, CropVarietyDefinition> _varieties =
            new Dictionary<string, CropVarietyDefinition>(StringComparer.Ordinal);
        private Dictionary<string, QualityDimensionDefinition>
            _qualityDimensions =
                new Dictionary<string, QualityDimensionDefinition>(
                    StringComparer.Ordinal);
        private Dictionary<string, ProductDefinition> _products =
            new Dictionary<string, ProductDefinition>(StringComparer.Ordinal);
        private Dictionary<string, FoodDefinition> _foods =
            new Dictionary<string, FoodDefinition>(StringComparer.Ordinal);
        private Dictionary<string, RecipeDefinition> _recipes =
            new Dictionary<string, RecipeDefinition>(StringComparer.Ordinal);
        private Dictionary<string, ProductionMethodDefinition> _methods =
            new Dictionary<string, ProductionMethodDefinition>(StringComparer.Ordinal);
        private Dictionary<string, SkillDefinition> _skills =
            new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
        private Dictionary<string, KnowledgeDefinition> _knowledge =
            new Dictionary<string, KnowledgeDefinition>(StringComparer.Ordinal);
        private Dictionary<string, TechnologyDefinition> _technologies =
            new Dictionary<string, TechnologyDefinition>(StringComparer.Ordinal);
        private List<RegisteredPackage> _packages = new List<RegisteredPackage>();

        public int CropCount => _crops.Count;
        public int CropVarietyCount => _varieties.Count;
        public int QualityDimensionCount => _qualityDimensions.Count;
        public int ProductCount => _products.Count;
        public int FoodCount => _foods.Count;
        public int RecipeCount => _recipes.Count;
        public int MethodCount => _methods.Count;
        public int SkillCount => _skills.Count;
        public int KnowledgeCount => _knowledge.Count;
        public int TechnologyCount => _technologies.Count;

        public string ResolvedHash => ComputeResolvedHash(_packages);

        public static ProductionContentRegistry CreateCore()
        {
            var registry = new ProductionContentRegistry();
            registry.Register(CoreProductionContent.CreatePackage());
            return registry;
        }

        public static ProductionContentRegistry FromJson(params string[] packageJson)
        {
            if (packageJson == null || packageJson.Length == 0)
            {
                throw new ArgumentException(
                    "At least one production content package is required.",
                    nameof(packageJson));
            }

            var registry = new ProductionContentRegistry();
            for (var i = 0; i < packageJson.Length; i++)
            {
                registry.Register(ProductionContentJson.DeserializePackage(
                    packageJson[i]));
            }

            return registry;
        }

        public void Register(ProductionContentPackageDefinition package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            ValidateId(package.PackageId, "content package");
            if (string.IsNullOrWhiteSpace(package.Version))
            {
                throw new ProductionContentException(
                    $"Content package {package.PackageId} has no version.");
            }

            for (var i = 0; i < _packages.Count; i++)
            {
                if (_packages[i].Definition.PackageId == package.PackageId)
                {
                    throw new ProductionContentException(
                        $"Duplicate production content package {package.PackageId}.");
                }
            }

            var crops = Copy(_crops);
            var varieties = Copy(_varieties);
            var qualityDimensions = Copy(_qualityDimensions);
            var products = Copy(_products);
            var foods = Copy(_foods);
            var recipes = Copy(_recipes);
            var methods = Copy(_methods);
            var skills = Copy(_skills);
            var knowledge = Copy(_knowledge);
            var technologies = Copy(_technologies);
            AddDefinitions(crops, package.Crops, item => item.Id, "crop");
            AddDefinitions(
                varieties, package.CropVarieties, item => item.Id, "crop variety");
            AddDefinitions(
                qualityDimensions,
                package.QualityDimensions,
                item => item.Id,
                "quality dimension");
            AddDefinitions(products, package.Products, item => item.Id, "product");
            ValidateFoodPackage(package.Foods);
            AddOptionalDefinitions(
                foods,
                package.Foods,
                item => item.ProductDefinitionId,
                "food product");
            AddDefinitions(recipes, package.Recipes, item => item.Id, "recipe");
            AddDefinitions(methods, package.Methods, item => item.Id, "method");
            AddDefinitions(skills, package.Skills, item => item.Id, "skill");
            AddDefinitions(
                knowledge, package.Knowledge, item => item.Id, "knowledge");
            AddDefinitions(
                technologies,
                package.Technologies,
                item => item.Id,
                "technology");
            ValidateDefinitions(
                crops,
                varieties,
                qualityDimensions,
                products,
                foods,
                recipes,
                methods,
                skills,
                knowledge,
                technologies);

            var packages = new List<RegisteredPackage>(_packages)
            {
                new RegisteredPackage(package, ComputePackageHash(package))
            };
            packages.Sort(ComparePackages);
            _crops = crops;
            _varieties = varieties;
            _qualityDimensions = qualityDimensions;
            _products = products;
            _foods = foods;
            _recipes = recipes;
            _methods = methods;
            _skills = skills;
            _knowledge = knowledge;
            _technologies = technologies;
            _packages = packages;
        }

        public CropDefinition GetCrop(string id)
        {
            return Get(_crops, id, "crop");
        }

        public CropVarietyDefinition GetCropVariety(string id)
        {
            return Get(_varieties, id, "crop variety");
        }

        public ProductDefinition GetProduct(string id)
        {
            return Get(_products, id, "product");
        }

        public FoodDefinition GetFood(string productDefinitionId)
        {
            return Get(_foods, productDefinitionId, "food product");
        }

        public bool TryGetFood(
            string productDefinitionId,
            out FoodDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(productDefinitionId))
            {
                definition = null;
                return false;
            }

            return _foods.TryGetValue(productDefinitionId, out definition);
        }

        private void RequireMarketProduct(
            string productDefinitionId,
            string ownerKind,
            string ownerId)
        {
            var product = GetProduct(productDefinitionId);
            if (!product.CategoryTags.Contains("product.market"))
            {
                throw new ProductionContentException(
                    $"{ownerKind} {ownerId} references non-market product " +
                    $"{productDefinitionId}.");
            }
        }

        public IReadOnlyList<FoodDefinition> GetFoodsInStableOrder()
        {
            var result = new List<FoodDefinition>(_foods.Values);
            result.Sort((left, right) => string.CompareOrdinal(
                left.ProductDefinitionId,
                right.ProductDefinitionId));
            return result;
        }

        public QualityDimensionDefinition GetQualityDimension(string id)
        {
            return Get(_qualityDimensions, id, "quality dimension");
        }

        public RecipeDefinition GetRecipe(string id)
        {
            return Get(_recipes, id, "recipe");
        }

        public ProductionMethodDefinition GetMethod(string id)
        {
            return Get(_methods, id, "production method");
        }

        public SkillDefinition GetSkill(string id)
        {
            return Get(_skills, id, "skill");
        }

        public KnowledgeDefinition GetKnowledge(string id)
        {
            return Get(_knowledge, id, "knowledge");
        }

        public TechnologyDefinition GetTechnology(string id)
        {
            return Get(_technologies, id, "technology");
        }

        public ProductionContentManifestState CreateManifest()
        {
            var manifest = new ProductionContentManifestState
            {
                ContentSchemaVersion = 3,
                ResolvedHash = ResolvedHash
            };
            for (var i = 0; i < _packages.Count; i++)
            {
                var package = _packages[i];
                manifest.Packages.Add(new ProductionContentPackageManifestState
                {
                    PackageId = package.Definition.PackageId,
                    Version = package.Definition.Version,
                    LoadOrder = package.Definition.LoadOrder,
                    Required = package.Definition.Required,
                    ContentHash = package.ContentHash
                });
            }

            return manifest;
        }

        public void ValidateManifest(ProductionContentManifestState manifest)
        {
            if (manifest == null || manifest.ContentSchemaVersion != 3 ||
                manifest.Packages == null)
            {
                throw new ProductionContentException(
                    "World has no supported production content manifest.");
            }

            var expected = CreateManifest();
            if (manifest.Packages.Count != expected.Packages.Count)
            {
                throw new ProductionContentException(
                    BuildManifestMismatchMessage(manifest, expected));
            }

            for (var i = 0; i < expected.Packages.Count; i++)
            {
                var actualPackage = manifest.Packages[i];
                var expectedPackage = expected.Packages[i];
                if (actualPackage == null ||
                    actualPackage.PackageId != expectedPackage.PackageId ||
                    actualPackage.Version != expectedPackage.Version ||
                    actualPackage.LoadOrder != expectedPackage.LoadOrder ||
                    actualPackage.ContentHash != expectedPackage.ContentHash)
                {
                    throw new ProductionContentException(
                        BuildManifestMismatchMessage(manifest, expected));
                }
            }

            if (manifest.ResolvedHash != expected.ResolvedHash)
            {
                throw new ProductionContentException(
                    $"Production content hash mismatch: world={manifest.ResolvedHash}, " +
                    $"loaded={expected.ResolvedHash}.");
            }
        }

        public void ValidateWorldReferences(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            ValidateManifest(world.ProductionContentManifest);
            for (var i = 0; i < world.Commodities.Count; i++)
            {
                var commodity = world.Commodities[i];
                if (string.IsNullOrEmpty(commodity.ProductDefinitionId))
                {
                    continue;
                }
                var product = GetProduct(commodity.ProductDefinitionId);
                if (product.BaseWeight != commodity.UnitWeight)
                {
                    throw new ProductionContentException(
                        $"Commodity {commodity.Id} disagrees with product " +
                        $"{product.Id} about unit weight.");
                }
            }
            var agricultureOrdersById =
                new Dictionary<string, AgricultureWorkOrderState>(
                    StringComparer.Ordinal);
            for (var i = 0; i < world.AgricultureWorkOrders.Count; i++)
            {
                var order = world.AgricultureWorkOrders[i];
                agricultureOrdersById.Add(order.Id, order);
                var crop = GetCrop(order.CropDefinitionId);
                var variety = GetCropVariety(order.CropVarietyDefinitionId);
                var recipe = GetRecipe(order.RecipeDefinitionId);
                var method = GetMethod(order.MethodDefinitionId);
                var seed = GetProduct(order.SeedProductDefinitionId);
                var harvest = GetProduct(order.HarvestProductDefinitionId);
                if (variety.CropDefinitionId != crop.Id ||
                    recipe.CropDefinitionId != crop.Id ||
                    !method.RecipeDefinitionIds.Contains(recipe.Id) ||
                    recipe.Inputs.Count != 1 || recipe.Outputs.Count != 1 ||
                    recipe.Inputs[0].ProductDefinitionId != seed.Id ||
                    recipe.Outputs[0].ProductDefinitionId != harvest.Id ||
                    seed.UnitId != order.UnitId || harvest.UnitId != order.UnitId)
                {
                    throw new ProductionContentException(
                        $"Agriculture work order {order.Id} has incompatible content references.");
                }

                if (order.AppliedTechnologyIds == null)
                {
                    throw new ProductionContentException(
                        $"Agriculture work order {order.Id} has no technology snapshot.");
                }

                for (var technologyIndex = 0;
                     technologyIndex < order.AppliedTechnologyIds.Count;
                     technologyIndex++)
                {
                    GetTechnology(order.AppliedTechnologyIds[technologyIndex]);
                }
            }

            for (var i = 0; i < world.ProductionLedgerEntries.Count; i++)
            {
                var entry = world.ProductionLedgerEntries[i];
                if (entry.Type == ProductionLedgerEntryType.LaborCommitted)
                {
                    if (!string.IsNullOrEmpty(entry.ProductDefinitionId) ||
                        entry.UnitId != CoreProductionContent.LaborDayUnitId)
                    {
                        throw new ProductionContentException(
                            $"Labor ledger entry {entry.Id} has invalid content references.");
                    }

                    continue;
                }

                var product = GetProduct(entry.ProductDefinitionId);
                if (product.UnitId != entry.UnitId)
                {
                    throw new ProductionContentException(
                        $"Production ledger entry {entry.Id} has an invalid unit.");
                }
            }

            for (var personIndex = 0;
                 personIndex < world.People.Count;
                 personIndex++)
            {
                var person = world.People[personIndex];
                for (var i = 0; i < person.SkillMasteries.Count; i++)
                {
                    GetSkill(person.SkillMasteries[i].SkillDefinitionId);
                }

                for (var i = 0; i < person.KnowledgeMasteries.Count; i++)
                {
                    GetKnowledge(
                        person.KnowledgeMasteries[i].KnowledgeDefinitionId);
                }

                for (var i = 0; i < person.TechnologyMasteries.Count; i++)
                {
                    GetTechnology(
                        person.TechnologyMasteries[i].TechnologyDefinitionId);
                }
            }

            for (var i = 0; i < world.ResearchProjects.Count; i++)
            {
                GetTechnology(
                    world.ResearchProjects[i].TechnologyDefinitionId);
            }

            for (var i = 0; i < world.TechnologyApplications.Count; i++)
            {
                GetTechnology(
                    world.TechnologyApplications[i].TechnologyDefinitionId);
            }

            for (var i = 0; i < world.ResearchLedgerEntries.Count; i++)
            {
                var entry = world.ResearchLedgerEntries[i];
                if (!string.IsNullOrEmpty(entry.KnowledgeDefinitionId))
                {
                    GetKnowledge(entry.KnowledgeDefinitionId);
                }

                if (!string.IsNullOrEmpty(entry.TechnologyDefinitionId))
                {
                    GetTechnology(entry.TechnologyDefinitionId);
                }
            }

            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                var product = GetProduct(batch.ProductDefinitionId);
                if (product.UnitId != batch.UnitId ||
                    product.BaseWeight != batch.UnitWeight ||
                    !ProductQualityRules.MatchesDefinition(batch, product))
                {
                    throw new ProductionContentException(
                        $"Product batch {batch.Id} has invalid physical or quality content.");
                }

                if (!string.IsNullOrEmpty(batch.CropVarietyDefinitionId))
                {
                    var variety = GetCropVariety(
                        batch.CropVarietyDefinitionId);
                    if (!product.CategoryTags.Contains("product.seed"))
                    {
                        if (!agricultureOrdersById.TryGetValue(
                                batch.SourceWorkOrderId ?? string.Empty,
                                out var sourceOrder) ||
                            sourceOrder.HarvestProductDefinitionId != product.Id ||
                            sourceOrder.CropVarietyDefinitionId != variety.Id ||
                            sourceOrder.CropDefinitionId !=
                                variety.CropDefinitionId)
                        {
                            throw new ProductionContentException(
                                $"Batch {batch.Id} declares an unrelated crop variety.");
                        }
                    }
                }
            }

            for (var i = 0; i < world.FormalMarketOrders.Count; i++)
            {
                var order = world.FormalMarketOrders[i];
                RequireMarketProduct(
                    order.ProductDefinitionId, "market order", order.Id);
            }

            for (var i = 0; i < world.FormalMarketTrades.Count; i++)
            {
                var trade = world.FormalMarketTrades[i];
                RequireMarketProduct(
                    trade.ProductDefinitionId, "market trade", trade.Id);
            }

            for (var i = 0; i < world.FormalMarketPrices.Count; i++)
            {
                var price = world.FormalMarketPrices[i];
                RequireMarketProduct(
                    price.ProductDefinitionId, "market price", price.Id);
            }

            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                var freight = world.CivilianFreights[i];
                var product = GetProduct(freight.ProductDefinitionId);
                var food = GetFood(freight.ProductDefinitionId);
                if (freight.ProductPerishabilityBasisPoints !=
                        product.PerishabilityBasisPoints ||
                    freight.FoodSpoilageSensitivityBasisPoints !=
                        food.SpoilageSensitivityBasisPoints ||
                    freight.CargoUnitWeight != product.BaseWeight)
                {
                    throw new ProductionContentException(
                        $"Civilian freight {freight.Id} has invalid content snapshots.");
                }
            }

            for (var i = 0; i < world.ProcessingWorkOrders.Count; i++)
            {
                var order = world.ProcessingWorkOrders[i];
                var recipe = GetRecipe(order.RecipeDefinitionId);
                var method = GetMethod(order.MethodDefinitionId);
                if (!string.IsNullOrEmpty(recipe.CropDefinitionId) ||
                    !method.RecipeDefinitionIds.Contains(recipe.Id) ||
                    order.PracticeSkillDefinitionId !=
                        method.PracticeSkillDefinitionId)
                {
                    throw new ProductionContentException(
                        $"Processing work order {order.Id} has incompatible content.");
                }
            }

            for (var i = 0; i < world.ResourceBodies.Count; i++)
            {
                GetProduct(world.ResourceBodies[i].OutputProductDefinitionId);
            }

            for (var i = 0; i < world.ProductionPracticeLedgerEntries.Count; i++)
            {
                GetSkill(world.ProductionPracticeLedgerEntries[i]
                    .SkillDefinitionId);
            }

            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                var transaction = world.InventoryTransactions[i];
                for (var lineIndex = 0;
                     lineIndex < transaction.Lines.Count;
                     lineIndex++)
                {
                    var line = transaction.Lines[lineIndex];
                    if (GetProduct(line.ProductDefinitionId).UnitId != line.UnitId)
                    {
                        throw new ProductionContentException(
                            $"Inventory transaction {transaction.Id} has an invalid unit.");
                    }
                }
            }
        }

        private static string BuildManifestMismatchMessage(
            ProductionContentManifestState actual,
            ProductionContentManifestState expected)
        {
            var actualIds = ManifestIds(actual);
            var expectedIds = ManifestIds(expected);
            return "Production content packages do not match. " +
                   $"World requires [{actualIds}], loaded [{expectedIds}].";
        }

        private static string ManifestIds(ProductionContentManifestState manifest)
        {
            if (manifest == null || manifest.Packages == null)
            {
                return string.Empty;
            }

            var ids = new List<string>();
            for (var i = 0; i < manifest.Packages.Count; i++)
            {
                ids.Add(manifest.Packages[i]?.PackageId ?? "<null>");
            }

            return string.Join(",", ids.ToArray());
        }

        private static void ValidateDefinitions(
            Dictionary<string, CropDefinition> crops,
            Dictionary<string, CropVarietyDefinition> varieties,
            Dictionary<string, QualityDimensionDefinition> qualityDimensions,
            Dictionary<string, ProductDefinition> products,
            Dictionary<string, FoodDefinition> foods,
            Dictionary<string, RecipeDefinition> recipes,
            Dictionary<string, ProductionMethodDefinition> methods,
            Dictionary<string, SkillDefinition> skills,
            Dictionary<string, KnowledgeDefinition> knowledge,
            Dictionary<string, TechnologyDefinition> technologies)
        {
            foreach (var pair in crops)
            {
                if (string.IsNullOrWhiteSpace(pair.Value.DisplayName))
                {
                    throw new ProductionContentException(
                        $"Crop {pair.Key} has no display name.");
                }
            }

            foreach (var pair in varieties)
            {
                var variety = pair.Value;
                if (!crops.ContainsKey(variety.CropDefinitionId))
                {
                    throw new ProductionContentException(
                        $"Crop variety {variety.Id} references missing crop " +
                        $"{variety.CropDefinitionId}.");
                }
            }

            foreach (var pair in qualityDimensions)
            {
                if (string.IsNullOrWhiteSpace(pair.Value.DisplayName))
                {
                    throw new ProductionContentException(
                        $"Quality dimension {pair.Key} has no display name.");
                }
            }

            foreach (var pair in products)
            {
                ValidateId(pair.Value.UnitId, $"unit for product {pair.Key}");
                if (pair.Value.BaseWeight <= 0 ||
                    pair.Value.PerishabilityBasisPoints < 0 ||
                    pair.Value.PerishabilityBasisPoints > 10_000 ||
                    pair.Value.QualityDimensionIds == null ||
                    pair.Value.QualityDimensionIds.Count == 0)
                {
                    throw new ProductionContentException(
                        $"Product {pair.Key} has invalid physical values.");
                }


                var dimensionIds = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < pair.Value.QualityDimensionIds.Count; i++)
                {
                    var dimensionId = pair.Value.QualityDimensionIds[i];
                    if (!qualityDimensions.ContainsKey(dimensionId) ||
                        !dimensionIds.Add(dimensionId))
                    {
                        throw new ProductionContentException(
                            $"Product {pair.Key} has invalid quality dimension " +
                            $"{dimensionId}.");
                    }
                }
            }

            foreach (var pair in foods)
            {
                var food = pair.Value;
                if (!products.TryGetValue(
                        food.ProductDefinitionId,
                        out var product) ||
                    product.CategoryTags == null ||
                    !product.CategoryTags.Contains("product.food") ||
                    food.OpeningShareBasisPoints < 0 ||
                    food.OpeningShareBasisPoints > 10_000 ||
                    food.NutritionBasisPoints < 1_000 ||
                    food.NutritionBasisPoints > 20_000 ||
                    food.VolumeBasisPoints < 1_000 ||
                    food.VolumeBasisPoints > 30_000 ||
                    food.SpoilageSensitivityBasisPoints < 0 ||
                    food.SpoilageSensitivityBasisPoints > 30_000 ||
                    food.MarketValueBasisPoints < 1_000 ||
                    food.MarketValueBasisPoints > 30_000 ||
                    food.ConsumptionPriority < 0 ||
                    string.IsNullOrWhiteSpace(food.SourceLayer) ||
                    string.IsNullOrWhiteSpace(food.SourceNote))
                {
                    throw new ProductionContentException(
                        $"Food definition for {pair.Key} is invalid.");
                }
            }

            foreach (var pair in recipes)
            {
                var recipe = pair.Value;
                if (!string.IsNullOrEmpty(recipe.CropDefinitionId) &&
                    !crops.ContainsKey(recipe.CropDefinitionId))
                {
                    throw new ProductionContentException(
                        $"Recipe {recipe.Id} references missing crop " +
                        $"{recipe.CropDefinitionId}.");
                }

                if (recipe.DurationDays <= 0 || recipe.Inputs == null ||
                    recipe.Inputs.Count == 0 || recipe.Outputs == null ||
                    recipe.Outputs.Count == 0)
                {
                    throw new ProductionContentException(
                        $"Recipe {recipe.Id} must consume inputs, take time, and produce outputs.");
                }

                ValidateQuantities(recipe.Id, recipe.Inputs, products, "input");
                ValidateQuantities(recipe.Id, recipe.Outputs, products, "output");
                RejectDirectFreeGrowth(recipe, products);
                ValidateProcessingMass(recipe, products);
            }

            foreach (var pair in methods)
            {
                var method = pair.Value;
                if (method.YieldBasisPoints <= 0 ||
                    method.LaborBasisPoints <= 0 ||
                    method.PracticeDifficultyBasisPoints <= 0 ||
                    method.PracticeDifficultyBasisPoints > 20_000 ||
                    !skills.ContainsKey(method.PracticeSkillDefinitionId) ||
                    method.RecipeDefinitionIds == null ||
                    method.RecipeDefinitionIds.Count == 0 ||
                    method.QualityDimensionModifiers == null)
                {
                    throw new ProductionContentException(
                        $"Production method {method.Id} has invalid factors or no recipes.");
                }

                for (var i = 0; i < method.RecipeDefinitionIds.Count; i++)
                {
                    var recipeId = method.RecipeDefinitionIds[i];
                    if (!recipes.ContainsKey(recipeId))
                    {
                        throw new ProductionContentException(
                            $"Production method {method.Id} references missing recipe {recipeId}.");
                    }
                }


                var modifiedDimensionIds = new HashSet<string>(
                    StringComparer.Ordinal);
                for (var i = 0;
                     i < method.QualityDimensionModifiers.Count;
                     i++)
                {
                    var modifier = method.QualityDimensionModifiers[i];
                    if (modifier == null ||
                        !qualityDimensions.ContainsKey(
                            modifier.QualityDimensionId) ||
                        !modifiedDimensionIds.Add(
                            modifier.QualityDimensionId) ||
                        modifier.ModifierBasisPoints < -5_000 ||
                        modifier.ModifierBasisPoints > 5_000)
                    {
                        throw new ProductionContentException(
                            $"Production method {method.Id} has an invalid " +
                            $"quality modifier at index {i}.");
                    }
                }
            }

            foreach (var pair in skills)
            {
                if (string.IsNullOrWhiteSpace(pair.Value.DisplayName) ||
                    string.IsNullOrWhiteSpace(pair.Value.FieldId))
                {
                    throw new ProductionContentException(
                        $"Skill {pair.Key} has no name or field.");
                }

                ValidateId(pair.Value.FieldId, $"field for skill {pair.Key}");
            }

            foreach (var pair in knowledge)
            {
                if (string.IsNullOrWhiteSpace(pair.Value.DisplayName) ||
                    string.IsNullOrWhiteSpace(pair.Value.FieldId))
                {
                    throw new ProductionContentException(
                        $"Knowledge {pair.Key} has no name or field.");
                }

                ValidateId(
                    pair.Value.FieldId, $"field for knowledge {pair.Key}");
            }

            foreach (var pair in technologies)
            {
                var technology = pair.Value;
                if (string.IsNullOrWhiteSpace(technology.DisplayName) ||
                    string.IsNullOrWhiteSpace(technology.FieldId) ||
                    !skills.ContainsKey(technology.RequiredSkillDefinitionId) ||
                    technology.RequiredSkillBasisPoints < 0 ||
                    technology.RequiredSkillBasisPoints > 10_000 ||
                    technology.RequiredKnowledgeMasteryBasisPoints < 0 ||
                    technology.RequiredKnowledgeMasteryBasisPoints > 10_000 ||
                    technology.ResearchPointsRequired <= 0 ||
                    technology.FundingCost < 0 ||
                    technology.ApplicationFundingCost < 0 ||
                    technology.RequiredKnowledgeDefinitionIds == null ||
                    technology.ResearchFacilityTags == null ||
                    technology.ResearchFacilityTags.Count == 0 ||
                    technology.Effects == null ||
                    technology.Effects.Count == 0)
                {
                    throw new ProductionContentException(
                        $"Technology {pair.Key} has an invalid research contract.");
                }

                ValidateId(
                    technology.FieldId,
                    $"field for technology {technology.Id}");
                for (var i = 0;
                     i < technology.RequiredKnowledgeDefinitionIds.Count;
                     i++)
                {
                    var knowledgeId =
                        technology.RequiredKnowledgeDefinitionIds[i];
                    if (!knowledge.ContainsKey(knowledgeId))
                    {
                        throw new ProductionContentException(
                            $"Technology {technology.Id} references missing " +
                            $"knowledge {knowledgeId}.");
                    }
                }

                for (var i = 0; i < technology.ResearchFacilityTags.Count; i++)
                {
                    ValidateId(
                        technology.ResearchFacilityTags[i],
                        $"research facility tag for technology {technology.Id}");
                }

                var effectIds = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < technology.Effects.Count; i++)
                {
                    var effect = technology.Effects[i];
                    if (effect == null || !effectIds.Add(effect.Id) ||
                        string.IsNullOrWhiteSpace(effect.TargetFacilityTag) ||
                        !recipes.ContainsKey(effect.RecipeDefinitionId) ||
                        !methods.ContainsKey(effect.MethodDefinitionId) ||
                        !methods[effect.MethodDefinitionId]
                            .RecipeDefinitionIds.Contains(
                                effect.RecipeDefinitionId) ||
                        effect.YieldBasisPoints <= 0 ||
                        effect.YieldBasisPoints > 30_000 ||
                        effect.LaborBasisPoints <= 0 ||
                        effect.LaborBasisPoints > 30_000)
                    {
                        throw new ProductionContentException(
                            $"Technology {technology.Id} has invalid effect " +
                            $"at index {i}.");
                    }

                    ValidateId(effect.Id, "technology effect");
                    ValidateId(
                        effect.TargetFacilityTag,
                        $"target facility tag for effect {effect.Id}");
                }
            }
        }

        private static void RejectDirectFreeGrowth(
            RecipeDefinition recipe,
            Dictionary<string, ProductDefinition> products)
        {
            for (var outputIndex = 0; outputIndex < recipe.Outputs.Count; outputIndex++)
            {
                var output = recipe.Outputs[outputIndex];
                long matchingInput = 0;
                for (var inputIndex = 0; inputIndex < recipe.Inputs.Count; inputIndex++)
                {
                    var input = recipe.Inputs[inputIndex];
                    if (input.ProductDefinitionId == output.ProductDefinitionId)
                    {
                        matchingInput += input.QuantityPerLandUnit;
                    }
                }

                if (matchingInput > 0 &&
                    output.QuantityPerLandUnit >= matchingInput &&
                    recipe.Inputs.Count == 1 && recipe.Outputs.Count == 1)
                {
                    throw new ProductionContentException(
                        $"Recipe {recipe.Id} creates a direct free quantity cycle for " +
                        $"{products[output.ProductDefinitionId].Id}.");
                }
            }
        }

        private static void ValidateProcessingMass(
            RecipeDefinition recipe,
            Dictionary<string, ProductDefinition> products)
        {
            if (!string.IsNullOrEmpty(recipe.CropDefinitionId))
            {
                return;
            }

            long inputWeight = 0;
            long outputWeight = 0;
            for (var i = 0; i < recipe.Inputs.Count; i++)
            {
                var quantity = recipe.Inputs[i];
                inputWeight = checked(inputWeight +
                    quantity.QuantityPerLandUnit *
                    products[quantity.ProductDefinitionId].BaseWeight);
            }

            for (var i = 0; i < recipe.Outputs.Count; i++)
            {
                var quantity = recipe.Outputs[i];
                outputWeight = checked(outputWeight +
                    quantity.QuantityPerLandUnit *
                    products[quantity.ProductDefinitionId].BaseWeight);
            }

            if (inputWeight != outputWeight)
            {
                throw new ProductionContentException(
                    $"Processing recipe {recipe.Id} does not conserve product weight.");
            }
        }

        private static void ValidateQuantities(
            string recipeId,
            IList<ProductionQuantityDefinition> quantities,
            Dictionary<string, ProductDefinition> products,
            string role)
        {
            for (var i = 0; i < quantities.Count; i++)
            {
                var quantity = quantities[i];
                if (quantity == null ||
                    !products.ContainsKey(quantity.ProductDefinitionId) ||
                    quantity.QuantityPerLandUnit <= 0)
                {
                    throw new ProductionContentException(
                        $"Recipe {recipeId} has invalid {role} at index {i}.");
                }
            }
        }

        private static Dictionary<string, T> Copy<T>(Dictionary<string, T> source)
        {
            return new Dictionary<string, T>(source, StringComparer.Ordinal);
        }

        private static void AddDefinitions<T>(
            IDictionary<string, T> target,
            IList<T> definitions,
            Func<T, string> idSelector,
            string kind)
            where T : class
        {
            if (definitions == null)
            {
                throw new ProductionContentException(
                    $"Production content package has a null {kind} collection.");
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i] ??
                    throw new ProductionContentException(
                        $"Production content package contains a null {kind}.");
                var id = idSelector(definition);
                ValidateId(id, kind);
                if (target.ContainsKey(id))
                {
                    throw new ProductionContentException(
                        $"Duplicate {kind} definition {id}.");
                }

                target.Add(id, definition);
            }
        }

        private static void AddOptionalDefinitions<T>(
            IDictionary<string, T> target,
            IList<T> definitions,
            Func<T, string> idSelector,
            string kind)
            where T : class
        {
            if (definitions != null)
            {
                AddDefinitions(target, definitions, idSelector, kind);
            }
        }

        private static void ValidateFoodPackage(IList<FoodDefinition> foods)
        {
            if (foods == null || foods.Count == 0)
            {
                return;
            }

            long openingShare = 0;
            for (var i = 0; i < foods.Count; i++)
            {
                if (foods[i] == null)
                {
                    throw new ProductionContentException(
                        "Production content package contains a null food definition.");
                }

                openingShare = checked(
                    openingShare + foods[i].OpeningShareBasisPoints);
            }

            if (openingShare != 0 && openingShare != 10_000)
            {
                throw new ProductionContentException(
                    "Non-zero food opening shares in a package must total 10000.");
            }
        }

        private static T Get<T>(IDictionary<string, T> definitions, string id, string kind)
        {
            ValidateId(id, kind);
            if (!definitions.TryGetValue(id, out var definition))
            {
                throw new ProductionContentException($"Missing {kind} definition {id}.");
            }

            return definition;
        }

        private static void ValidateId(string id, string kind)
        {
            try
            {
                _ = new StableId(id);
            }
            catch (ArgumentException exception)
            {
                throw new ProductionContentException(
                    $"Invalid {kind} ID '{id}': {exception.Message}");
            }

            if (id.IndexOf('.') <= 0)
            {
                throw new ProductionContentException(
                    $"{kind} ID '{id}' must use a namespace.");
            }
        }

        private static int ComparePackages(RegisteredPackage left, RegisteredPackage right)
        {
            var order = left.Definition.LoadOrder.CompareTo(right.Definition.LoadOrder);
            return order != 0
                ? order
                : string.CompareOrdinal(
                    left.Definition.PackageId, right.Definition.PackageId);
        }

        private static string ComputePackageHash(
            ProductionContentPackageDefinition package)
        {
            return ComputeHash(JsonConvert.SerializeObject(package, Formatting.None));
        }

        private static string ComputeResolvedHash(IList<RegisteredPackage> packages)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < packages.Count; i++)
            {
                builder.Append(packages[i].Definition.LoadOrder).Append('|')
                    .Append(packages[i].Definition.PackageId).Append('|')
                    .Append(packages[i].Definition.Version).Append('|')
                    .Append(packages[i].ContentHash).Append('\n');
            }

            return ComputeHash(builder.ToString());
        }

        private static string ComputeHash(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            byte[] digest;
            using (var sha256 = SHA256.Create())
            {
                digest = sha256.ComputeHash(bytes);
            }

            var result = new StringBuilder(digest.Length * 2);
            for (var i = 0; i < digest.Length; i++)
            {
                result.Append(digest[i].ToString("x2"));
            }

            return result.ToString();
        }

        private sealed class RegisteredPackage
        {
            public readonly ProductionContentPackageDefinition Definition;
            public readonly string ContentHash;

            public RegisteredPackage(
                ProductionContentPackageDefinition definition,
                string contentHash)
            {
                Definition = definition;
                ContentHash = contentHash;
            }
        }
    }

    public static class ProductionContentJson
    {
        private static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Include
            };

        public static ProductionContentPackageDefinition DeserializePackage(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException(
                    "Production content JSON cannot be empty.", nameof(json));
            }

            try
            {
                return JsonConvert.DeserializeObject<ProductionContentPackageDefinition>(
                           json, Settings)
                       ?? throw new ProductionContentException(
                           "Production content JSON contained no package.");
            }
            catch (JsonException exception)
            {
                throw new ProductionContentException(
                    $"Invalid production content JSON: {exception.Message}");
            }
        }

        public static string SerializePackage(
            ProductionContentPackageDefinition package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            return JsonConvert.SerializeObject(package, Formatting.Indented, Settings);
        }
    }

    public static class CoreProductionContent
    {
        public const string PackageId = "content.core.production";
        public const string PurityQualityDimensionId = "quality.purity";
        public const string IntegrityQualityDimensionId = "quality.integrity";
        public const string UniformityQualityDimensionId = "quality.uniformity";
        public const string DurabilityQualityDimensionId = "quality.durability";
        public const string WorkmanshipQualityDimensionId = "quality.workmanship";
        public const string HealthQualityDimensionId = "quality.health";
        public const string HygieneQualityDimensionId = "quality.hygiene";
        public const string NutritionQualityDimensionId = "quality.nutrition";
        public const string ViabilityQualityDimensionId = "quality.viability";
        public const string WheatCropId = "crop.wheat";
        public const string PrototypeNorthernWheatVarietyId =
            "crop_variety.wheat.prototype_northern";
        public const string WheatSeedProductId = "product.wheat_seed";
        public const string WheatGrainProductId = "product.wheat_grain";
        public const string WheatFlourProductId = "product.wheat_flour";
        public const string WheatBranProductId = "product.wheat_bran";
        public const string DryRationProductId = "product.dry_ration";
        public const string PlainClothProductId = "product.textile.plain_cloth";
        public const string RawMedicinalPlantProductId =
            "product.raw.medicinal_plants";
        public const string HerbalMedicineMaterialProductId =
            "product.medicine.herbal_material";
        public const string IronMaterialProductId = "product.material.iron";
        public const string TimberMaterialProductId = "product.material.timber";
        public const string LeatherMaterialProductId = "product.material.leather";
        public const string HornMaterialProductId = "product.material.horn";
        public const string IronOreProductId = "product.raw.iron_ore";
        public const string CharcoalProductId = "product.material.charcoal";
        public const string WoodAshProductId = "product.byproduct.wood_ash";
        public const string SlagProductId = "product.byproduct.smelting_slag";
        public const string PastureFodderProductId =
            "product.fodder.pasture_grass";
        public const string LiveSheepProductId = "product.livestock.sheep";
        public const string FreshMuttonProductId = "product.food.fresh_mutton";
        public const string RawHideProductId = "product.raw.sheep_hide";
        public const string RawHornProductId = "product.raw.sheep_horn";
        public const string AnimalBoneProductId = "product.byproduct.animal_bone";
        public const string OffalProductId = "product.food.offal";
        public const string TanningBarkProductId = "product.raw.tanning_bark";
        public const string TanningWasteProductId =
            "product.byproduct.tanning_waste";
        public const string HornScrapProductId =
            "product.byproduct.horn_scrap";
        public const string RingSwordProductId =
            "product.equipment.han_ring_sword";
        public const string WoodenShieldProductId =
            "product.equipment.wooden_shield";
        public const string LongSpearProductId =
            "product.equipment.long_spear";
        public const string HornBowProductId =
            "product.equipment.horn_bow";
        public const string ArrowBundleProductId =
            "product.equipment.arrow_bundle";
        public const string LamellarArmorProductId =
            "product.equipment.lamellar_armor";
        public const string GrowWheatRecipeId = "recipe.field.grow_wheat";
        public const string HandMillWheatRecipeId =
            "recipe.processing.hand_mill_wheat";
        public const string MakeDryRationRecipeId =
            "recipe.processing.make_dry_ration";
        public const string DryMedicinalPlantsRecipeId =
            "recipe.primary_processing.dry_sort_medicinal_plants";
        public const string ForgeRingSwordRecipeId =
            "recipe.manufacturing.forge_ring_sword";
        public const string MakeWoodenShieldRecipeId =
            "recipe.manufacturing.make_wooden_shield";
        public const string ForgeLongSpearRecipeId =
            "recipe.manufacturing.forge_long_spear";
        public const string MakeHornBowRecipeId =
            "recipe.manufacturing.make_horn_bow";
        public const string MakeArrowBundleRecipeId =
            "recipe.manufacturing.make_arrow_bundle";
        public const string MakeLamellarArmorRecipeId =
            "recipe.manufacturing.make_lamellar_armor";
        public const string BurnCharcoalRecipeId =
            "recipe.primary_processing.burn_charcoal";
        public const string SmeltBloomeryIronRecipeId =
            "recipe.primary_processing.smelt_bloomery_iron";
        public const string BreedSheepRecipeId =
            "recipe.livestock.breed_sheep";
        public const string SlaughterSheepRecipeId =
            "recipe.livestock.slaughter_sheep";
        public const string VegetableTanHideRecipeId =
            "recipe.primary_processing.vegetable_tan_hide";
        public const string FinishHornRecipeId =
            "recipe.primary_processing.finish_horn";
        public const string PrototypeDrylandMethodId =
            "method.farming.prototype_dryland";
        public const string HandMillingMethodId =
            "method.processing.hand_milling";
        public const string DryRationMethodId =
            "method.processing.dry_ration";
        public const string HerbalDryingMethodId =
            "method.primary_processing.herbal_drying_sorting";
        public const string BlacksmithingMethodId =
            "method.manufacturing.blacksmithing";
        public const string WoodworkingMethodId =
            "method.manufacturing.woodworking";
        public const string BowmakingMethodId =
            "method.manufacturing.bowmaking";
        public const string ArmoringMethodId =
            "method.manufacturing.armoring";
        public const string EarthKilnCharcoalMethodId =
            "method.primary_processing.earth_kiln_charcoal";
        public const string BloomerySmeltingMethodId =
            "method.primary_processing.bloomery_smelting";
        public const string PastureBreedingMethodId =
            "method.livestock.pasture_breeding";
        public const string ManualSlaughterMethodId =
            "method.livestock.manual_slaughter";
        public const string VegetableTanningMethodId =
            "method.primary_processing.vegetable_tanning";
        public const string HornFinishingMethodId =
            "method.primary_processing.horn_finishing";
        public const string BlacksmithFacilityTag =
            "facility.blacksmith_workshop";
        public const string WoodworkingFacilityTag =
            "facility.woodworking_workshop";
        public const string BowmakingFacilityTag =
            "facility.bowmaking_workshop";
        public const string ArmoringFacilityTag =
            "facility.armoring_workshop";
        public const string IronMiningFacilityTag =
            "facility.resource_extraction.iron_mine";
        public const string LoggingFacilityTag =
            "facility.resource_extraction.logging_camp";
        public const string CharcoalKilnFacilityTag =
            "facility.primary_processing.charcoal_kiln";
        public const string BloomeryFacilityTag =
            "facility.primary_processing.bloomery";
        public const string PastureForageFacilityTag =
            "facility.resource_extraction.pasture_forage";
        public const string BarkHarvestingFacilityTag =
            "facility.resource_extraction.bark_harvesting";
        public const string HerbGatheringFacilityTag =
            "facility.resource_extraction.herb_gathering";
        public const string HerbDryingFacilityTag =
            "facility.primary_processing.herb_drying";
        public const string PastureFacilityTag = "facility.livestock.pasture";
        public const string SlaughterYardFacilityTag =
            "facility.livestock.slaughter_yard";
        public const string TanneryFacilityTag =
            "facility.primary_processing.tannery";
        public const string HornWorkshopFacilityTag =
            "facility.primary_processing.horn_workshop";
        public const string GrainUnitId = "unit.grain";
        public const string LaborDayUnitId = "unit.labor_day";
        public const string ItemUnitId = "unit.item";

        public static ProductionContentPackageDefinition CreatePackage()
        {
            var package = new ProductionContentPackageDefinition
            {
                PackageId = PackageId,
                Version = "11.0.0",
                LoadOrder = 0,
                Required = true
            };
            AddQualityDimension(
                package, PurityQualityDimensionId, "纯净度");
            AddQualityDimension(
                package, IntegrityQualityDimensionId, "完整度");
            AddQualityDimension(
                package, UniformityQualityDimensionId, "均一度");
            AddQualityDimension(
                package, DurabilityQualityDimensionId, "耐久度");
            AddQualityDimension(
                package, WorkmanshipQualityDimensionId, "工艺度");
            AddQualityDimension(
                package, HealthQualityDimensionId, "健康度");
            AddQualityDimension(
                package, HygieneQualityDimensionId, "洁净度");
            AddQualityDimension(
                package, NutritionQualityDimensionId, "营养度");
            AddQualityDimension(
                package, ViabilityQualityDimensionId, "活力度");
            package.Crops.Add(new CropDefinition
            {
                Id = WheatCropId,
                DisplayName = "小麦",
                HistoricalStatus = "historical_core",
                SourceNote = "东汉核心历史作物；原型参数为原创玩法补全。",
                UsageTags = new List<string>
                {
                    "usage.staple",
                    "usage.seed",
                    "usage.fodder",
                    "usage.military_supply"
                }
            });
            package.CropVarieties.Add(new CropVarietyDefinition
            {
                Id = PrototypeNorthernWheatVarietyId,
                CropDefinitionId = WheatCropId,
                DisplayName = "北方小麦原型品种",
                Provenance = "gameplay_completion",
                TraitIds = new List<string> { "trait.crop.prototype_dryland" }
            });
            package.Products.Add(new ProductDefinition
            {
                Id = WheatSeedProductId,
                DisplayName = "小麦种子",
                UnitId = GrainUnitId,
                BaseWeight = 1,
                PerishabilityBasisPoints = 1_000,
                QualityDimensionIds = QualityDimensions(
                    PurityQualityDimensionId,
                    ViabilityQualityDimensionId),
                CategoryTags = new List<string>
                {
                    "product.seed",
                    "product.agriculture_input"
                }
            });
            package.Products.Add(new ProductDefinition
            {
                Id = WheatGrainProductId,
                DisplayName = "麦粒",
                UnitId = GrainUnitId,
                BaseWeight = 1,
                PerishabilityBasisPoints = 600,
                QualityDimensionIds = QualityDimensions(
                    PurityQualityDimensionId,
                    IntegrityQualityDimensionId),
                CategoryTags = new List<string>
                {
                    "product.food",
                    "product.grain",
                    "product.market",
                    "product.military_supply"
                }
            });
            package.Products.Add(new ProductDefinition
            {
                Id = WheatFlourProductId,
                DisplayName = "小麦面粉",
                UnitId = GrainUnitId,
                BaseWeight = 1,
                PerishabilityBasisPoints = 800,
                QualityDimensionIds = QualityDimensions(
                    PurityQualityDimensionId,
                    UniformityQualityDimensionId),
                CategoryTags = new List<string>
                {
                    "product.food",
                    "product.processed",
                    "product.market"
                }
            });
            package.Products.Add(new ProductDefinition
            {
                Id = WheatBranProductId,
                DisplayName = "麦麸",
                UnitId = GrainUnitId,
                BaseWeight = 1,
                PerishabilityBasisPoints = 700,
                QualityDimensionIds = QualityDimensions(
                    PurityQualityDimensionId,
                    NutritionQualityDimensionId),
                CategoryTags = new List<string>
                {
                    "product.byproduct",
                    "product.fodder",
                    "product.market"
                }
            });
            package.Products.Add(new ProductDefinition
            {
                Id = DryRationProductId,
                DisplayName = "干粮",
                UnitId = GrainUnitId,
                BaseWeight = 1,
                PerishabilityBasisPoints = 250,
                QualityDimensionIds = QualityDimensions(
                    HygieneQualityDimensionId,
                    NutritionQualityDimensionId),
                CategoryTags = new List<string>
                {
                    "product.food",
                    "product.processed",
                    "product.market",
                    "product.military_supply"
                }
            });
            AddOpenProduct(
                package,
                PlainClothProductId,
                "素布",
                2,
                0,
                "product.textile",
                "product.market",
                "product.household_good");
            AddOpenProduct(
                package,
                RawMedicinalPlantProductId,
                "野生药草",
                1,
                2_000,
                "product.raw",
                "product.medicine_input",
                "product.market");
            AddOpenProduct(
                package,
                HerbalMedicineMaterialProductId,
                "草药材",
                1,
                1_200,
                "product.medicine",
                "product.medical_input",
                "product.market");
            AddMaterialProduct(package, IronMaterialProductId, "铁料");
            AddMaterialProduct(package, TimberMaterialProductId, "木料");
            AddMaterialProduct(package, LeatherMaterialProductId, "皮革");
            AddMaterialProduct(package, HornMaterialProductId, "角料");
            AddMaterialProduct(package, IronOreProductId, "铁矿石");
            AddMaterialProduct(package, CharcoalProductId, "木炭");
            AddByproduct(package, WoodAshProductId, "草木灰");
            AddByproduct(package, SlagProductId, "炉渣");
            AddOpenProduct(
                package, PastureFodderProductId, "牧草", 1, 900,
                "product.fodder", "product.market", "product.livestock_input");
            AddOpenProduct(
                package, LiveSheepProductId, "活羊", 10, 0,
                "product.livestock", "product.market", "product.living_asset");
            AddOpenProduct(
                package, FreshMuttonProductId, "鲜羊肉", 1, 2_500,
                "product.food", "product.market", "product.perishable");
            AddOpenProduct(
                package, RawHideProductId, "生羊皮", 1, 1_500,
                "product.raw", "product.market", "product.tanning_input");
            AddOpenProduct(
                package, RawHornProductId, "生羊角", 1, 0,
                "product.raw", "product.market", "product.horn_input");
            AddOpenProduct(
                package, AnimalBoneProductId, "兽骨", 1, 0,
                "product.byproduct", "product.material", "product.market");
            AddOpenProduct(
                package, OffalProductId, "下水", 1, 3_000,
                "product.food", "product.byproduct", "product.perishable");
            AddOpenProduct(
                package, TanningBarkProductId, "鞣料树皮", 1, 0,
                "product.raw", "product.market", "product.tanning_input");
            AddOpenProduct(
                package, TanningWasteProductId, "鞣制废料", 1, 0,
                "product.byproduct", "product.waste");
            AddOpenProduct(
                package, HornScrapProductId, "角边料", 1, 0,
                "product.byproduct", "product.material", "product.market");
            AddMilitaryEquipmentProduct(
                package, RingSwordProductId, "环首刀", 3,
                "product.equipment.melee");
            AddMilitaryEquipmentProduct(
                package, WoodenShieldProductId, "木盾", 5,
                "product.equipment.shield");
            AddMilitaryEquipmentProduct(
                package, LongSpearProductId, "长矛", 5,
                "product.equipment.melee");
            AddMilitaryEquipmentProduct(
                package, HornBowProductId, "角弓", 2,
                "product.equipment.ranged");
            AddMilitaryEquipmentProduct(
                package, ArrowBundleProductId, "箭束", 2,
                "product.equipment.ammunition");
            AddMilitaryEquipmentProduct(
                package, LamellarArmorProductId, "札甲", 10,
                "product.equipment.armor");
            package.Recipes.Add(new RecipeDefinition
            {
                Id = GrowWheatRecipeId,
                DisplayName = "种植小麦",
                CropDefinitionId = WheatCropId,
                DurationDays = 180,
                FacilityTags = new List<string> { "facility.farmland" },
                Inputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = WheatSeedProductId,
                        QuantityPerLandUnit = 2
                    }
                },
                Outputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = WheatGrainProductId,
                        QuantityPerLandUnit = 15
                    }
                }
            });
            package.Recipes.Add(new RecipeDefinition
            {
                Id = HandMillWheatRecipeId,
                DisplayName = "手工磨麦",
                DurationDays = 2,
                FacilityTags = new List<string>
                {
                    VillageFacilityTags.HouseholdGranary
                },
                Inputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = WheatGrainProductId,
                        QuantityPerLandUnit = 10
                    }
                },
                Outputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = WheatFlourProductId,
                        QuantityPerLandUnit = 8
                    },
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = WheatBranProductId,
                        QuantityPerLandUnit = 2
                    }
                }
            });
            package.Recipes.Add(new RecipeDefinition
            {
                Id = MakeDryRationRecipeId,
                DisplayName = "制作干粮",
                DurationDays = 1,
                FacilityTags = new List<string>
                {
                    VillageFacilityTags.HouseholdGranary
                },
                Inputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = WheatFlourProductId,
                        QuantityPerLandUnit = 8
                    }
                },
                Outputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = DryRationProductId,
                        QuantityPerLandUnit = 8
                    }
                }
            });
            package.Recipes.Add(new RecipeDefinition
            {
                Id = DryMedicinalPlantsRecipeId,
                DisplayName = "晾晒拣选药草",
                DurationDays = 2,
                FacilityTags = new List<string>
                {
                    HerbDryingFacilityTag
                },
                Inputs = new List<ProductionQuantityDefinition>
                {
                    Quantity(RawMedicinalPlantProductId, 1)
                },
                Outputs = new List<ProductionQuantityDefinition>
                {
                    Quantity(HerbalMedicineMaterialProductId, 1)
                }
            });
            AddManufacturingRecipe(
                package, ForgeRingSwordRecipeId, "锻造环首刀", 6,
                BlacksmithFacilityTag,
                RingSwordProductId,
                IronMaterialProductId, 2,
                TimberMaterialProductId, 1);
            AddManufacturingRecipe(
                package, MakeWoodenShieldRecipeId, "制作木盾", 4,
                WoodworkingFacilityTag,
                WoodenShieldProductId,
                TimberMaterialProductId, 4,
                LeatherMaterialProductId, 1);
            AddManufacturingRecipe(
                package, ForgeLongSpearRecipeId, "锻造长矛", 5,
                BlacksmithFacilityTag,
                LongSpearProductId,
                IronMaterialProductId, 2,
                TimberMaterialProductId, 3);
            AddManufacturingRecipe(
                package, MakeHornBowRecipeId, "制作角弓", 7,
                BowmakingFacilityTag,
                HornBowProductId,
                TimberMaterialProductId, 1,
                HornMaterialProductId, 1);
            AddManufacturingRecipe(
                package, MakeArrowBundleRecipeId, "制作箭束", 2,
                WoodworkingFacilityTag,
                ArrowBundleProductId,
                TimberMaterialProductId, 1,
                IronMaterialProductId, 1);
            AddManufacturingRecipe(
                package, MakeLamellarArmorRecipeId, "制作札甲", 12,
                ArmoringFacilityTag,
                LamellarArmorProductId,
                IronMaterialProductId, 8,
                LeatherMaterialProductId, 2);
            AddPrimaryProcessingRecipe(
                package,
                BurnCharcoalRecipeId,
                "烧制木炭",
                3,
                CharcoalKilnFacilityTag,
                TimberMaterialProductId,
                2,
                CharcoalProductId,
                1,
                WoodAshProductId,
                1);
            AddPrimaryProcessingRecipe(
                package,
                SmeltBloomeryIronRecipeId,
                "块炼铁冶炼",
                5,
                BloomeryFacilityTag,
                IronOreProductId,
                3,
                IronMaterialProductId,
                2,
                SlagProductId,
                2,
                CharcoalProductId,
                1);
            package.Recipes.Add(new RecipeDefinition
            {
                Id = BreedSheepRecipeId,
                DisplayName = "牧场繁育羊群",
                DurationDays = 30,
                FacilityTags = new List<string> { PastureFacilityTag },
                Inputs = new List<ProductionQuantityDefinition>
                {
                    Quantity(LiveSheepProductId, 1),
                    Quantity(PastureFodderProductId, 10)
                },
                Outputs = new List<ProductionQuantityDefinition>
                {
                    Quantity(LiveSheepProductId, 2)
                }
            });
            package.Recipes.Add(new RecipeDefinition
            {
                Id = SlaughterSheepRecipeId,
                DisplayName = "屠宰羊只",
                DurationDays = 1,
                FacilityTags = new List<string> { SlaughterYardFacilityTag },
                Inputs = new List<ProductionQuantityDefinition>
                {
                    Quantity(LiveSheepProductId, 1)
                },
                Outputs = new List<ProductionQuantityDefinition>
                {
                    Quantity(FreshMuttonProductId, 5),
                    Quantity(RawHideProductId, 2),
                    Quantity(RawHornProductId, 1),
                    Quantity(AnimalBoneProductId, 1),
                    Quantity(OffalProductId, 1)
                }
            });
            package.Recipes.Add(new RecipeDefinition
            {
                Id = VegetableTanHideRecipeId,
                DisplayName = "植物鞣制羊皮",
                DurationDays = 7,
                FacilityTags = new List<string> { TanneryFacilityTag },
                Inputs = new List<ProductionQuantityDefinition>
                {
                    Quantity(RawHideProductId, 2),
                    Quantity(TanningBarkProductId, 1)
                },
                Outputs = new List<ProductionQuantityDefinition>
                {
                    Quantity(LeatherMaterialProductId, 2),
                    Quantity(TanningWasteProductId, 1)
                }
            });
            package.Recipes.Add(new RecipeDefinition
            {
                Id = FinishHornRecipeId,
                DisplayName = "整理角料",
                DurationDays = 3,
                FacilityTags = new List<string> { HornWorkshopFacilityTag },
                Inputs = new List<ProductionQuantityDefinition>
                {
                    Quantity(RawHornProductId, 2)
                },
                Outputs = new List<ProductionQuantityDefinition>
                {
                    Quantity(HornMaterialProductId, 1),
                    Quantity(HornScrapProductId, 1)
                }
            });
            package.Methods.Add(new ProductionMethodDefinition
            {
                Id = PrototypeDrylandMethodId,
                DisplayName = "原型旱作法",
                RecipeDefinitionIds = new List<string> { GrowWheatRecipeId },
                PracticeSkillDefinitionId = CoreSkillIds.Agriculture,
                PracticeDifficultyBasisPoints = 10_000,
                YieldBasisPoints = 10_000,
                LaborBasisPoints = 10_000,
                HistoricalStatus = "gameplay_completion"
            });
            package.Methods.Add(new ProductionMethodDefinition
            {
                Id = HandMillingMethodId,
                DisplayName = "手工磨制",
                RecipeDefinitionIds = new List<string>
                {
                    HandMillWheatRecipeId
                },
                PracticeSkillDefinitionId = CoreSkillIds.FoodProcessing,
                PracticeDifficultyBasisPoints = 7_000,
                QualityDimensionModifiers = QualityModifiers(
                    UniformityQualityDimensionId, 150,
                    PurityQualityDimensionId, 50),
                YieldBasisPoints = 10_000,
                LaborBasisPoints = 10_000,
                HistoricalStatus = "historical_inference"
            });
            package.Methods.Add(new ProductionMethodDefinition
            {
                Id = DryRationMethodId,
                DisplayName = "干粮制作",
                RecipeDefinitionIds = new List<string>
                {
                    MakeDryRationRecipeId
                },
                PracticeSkillDefinitionId = CoreSkillIds.FoodProcessing,
                PracticeDifficultyBasisPoints = 8_000,
                QualityDimensionModifiers = QualityModifiers(
                    HygieneQualityDimensionId, 150,
                    NutritionQualityDimensionId, 50),
                YieldBasisPoints = 10_000,
                LaborBasisPoints = 10_000,
                HistoricalStatus = "historical_inference"
            });
            AddManufacturingMethod(
                package,
                HerbalDryingMethodId,
                "药草晾晒与拣选",
                DryMedicinalPlantsRecipeId);
            AddManufacturingMethod(
                package, BlacksmithingMethodId, "锻打",
                ForgeRingSwordRecipeId, ForgeLongSpearRecipeId);
            AddManufacturingMethod(
                package, WoodworkingMethodId, "木作",
                MakeWoodenShieldRecipeId, MakeArrowBundleRecipeId);
            AddManufacturingMethod(
                package, BowmakingMethodId, "制弓",
                MakeHornBowRecipeId);
            AddManufacturingMethod(
                package, ArmoringMethodId, "制甲",
                MakeLamellarArmorRecipeId);
            AddManufacturingMethod(
                package, EarthKilnCharcoalMethodId, "土窑烧炭",
                BurnCharcoalRecipeId);
            AddManufacturingMethod(
                package, BloomerySmeltingMethodId, "块炼炉冶炼",
                SmeltBloomeryIronRecipeId);
            AddManufacturingMethod(
                package, PastureBreedingMethodId, "牧场繁育",
                BreedSheepRecipeId);
            AddManufacturingMethod(
                package, ManualSlaughterMethodId, "手工屠宰",
                SlaughterSheepRecipeId);
            AddManufacturingMethod(
                package, VegetableTanningMethodId, "植物鞣革",
                VegetableTanHideRecipeId);
            AddManufacturingMethod(
                package, HornFinishingMethodId, "角料整理",
                FinishHornRecipeId);
            package.Skills.Add(new SkillDefinition
            {
                Id = CoreSkillIds.Agriculture,
                DisplayName = "农业",
                FieldId = "field.agriculture_water",
                HistoricalStatus = "system_bridge",
                SourceNote = "映射既有人物农业能力；后续子技艺继续使用稳定ID扩展。"
            });
            AddProductionSkill(
                package,
                CoreSkillIds.FoodProcessing,
                "食物加工",
                "field.production.food_processing");
            AddProductionSkill(
                package,
                CoreSkillIds.Metalworking,
                "金属工艺",
                "field.production.metalworking");
            AddProductionSkill(
                package,
                CoreSkillIds.Woodworking,
                "木作工艺",
                "field.production.woodworking");
            AddProductionSkill(
                package,
                CoreSkillIds.Bowmaking,
                "弓角工艺",
                "field.production.bowmaking");
            AddProductionSkill(
                package,
                CoreSkillIds.Armoring,
                "甲作工艺",
                "field.production.armoring");
            AddProductionSkill(
                package,
                CoreSkillIds.Husbandry,
                "畜牧与屠宰",
                "field.production.husbandry");
            AddProductionSkill(
                package,
                CoreSkillIds.Tanning,
                "鞣革工艺",
                "field.production.tanning");
            AddProductionSkill(
                package,
                CoreSkillIds.HerbalProcessing,
                "药草加工",
                "field.production.herbal_processing");
            package.Knowledge.Add(new KnowledgeDefinition
            {
                Id = CoreKnowledgeIds.SeasonalObservation,
                DisplayName = "农时观察",
                FieldId = "field.agriculture_water",
                HistoricalStatus = "historical_inference",
                SourceNote = "基于长期农业实践的通用知识抽象；具体参数为原创玩法补全。"
            });
            package.Technologies.Add(CreateAgricultureTechnology(
                CoreTechnologyIds.SeedSelection,
                "选种法",
                "按籽粒状态筛选并保存下一季种子。",
                4_000,
                8_000,
                20,
                5,
                "effect.agriculture.seed_selection",
                10_300,
                10_000));
            package.Technologies.Add(CreateAgricultureTechnology(
                CoreTechnologyIds.RidgeSowing,
                "垄作播种",
                "通过垄沟组织播种与排水，换取更稳定的田间表现。",
                5_000,
                12_000,
                30,
                8,
                "effect.agriculture.ridge_sowing",
                10_500,
                10_500));
            package.Technologies.Add(CreateAgricultureTechnology(
                CoreTechnologyIds.CoordinatedFieldwork,
                "协同农作",
                "改进家庭与乡里劳作排程，降低同一工单的劳动需求。",
                4_500,
                9_000,
                25,
                6,
                "effect.agriculture.coordinated_fieldwork",
                10_000,
                9_000));
            return package;
        }

        private static void AddMilitaryEquipmentProduct(
            ProductionContentPackageDefinition package,
            string id,
            string displayName,
            int weight,
            string categoryTag)
        {
            package.Products.Add(new ProductDefinition
            {
                Id = id,
                DisplayName = displayName,
                UnitId = ItemUnitId,
                BaseWeight = weight,
                PerishabilityBasisPoints = 0,
                QualityDimensionIds = QualityDimensions(
                    DurabilityQualityDimensionId,
                    WorkmanshipQualityDimensionId),
                CategoryTags = new List<string>
                {
                    "product.equipment",
                    "product.market",
                    "product.military_supply",
                    categoryTag
                }
            });
        }

        private static void AddMaterialProduct(
            ProductionContentPackageDefinition package,
            string id,
            string displayName)
        {
            List<string> qualityDimensions;
            switch (id)
            {
                case TimberMaterialProductId:
                    qualityDimensions = QualityDimensions(
                        IntegrityQualityDimensionId,
                        DurabilityQualityDimensionId);
                    break;
                case LeatherMaterialProductId:
                    qualityDimensions = QualityDimensions(
                        PurityQualityDimensionId,
                        DurabilityQualityDimensionId);
                    break;
                case HornMaterialProductId:
                    qualityDimensions = QualityDimensions(
                        IntegrityQualityDimensionId,
                        WorkmanshipQualityDimensionId);
                    break;
                default:
                    qualityDimensions = QualityDimensions(
                        PurityQualityDimensionId,
                        IntegrityQualityDimensionId);
                    break;
            }

            package.Products.Add(new ProductDefinition
            {
                Id = id,
                DisplayName = displayName,
                UnitId = ItemUnitId,
                BaseWeight = 1,
                PerishabilityBasisPoints = 0,
                QualityDimensionIds = qualityDimensions,
                CategoryTags = new List<string>
                {
                    "product.material",
                    "product.market",
                    "product.manufacturing_input"
                }
            });
        }

        private static void AddByproduct(
            ProductionContentPackageDefinition package,
            string id,
            string displayName)
        {
            package.Products.Add(new ProductDefinition
            {
                Id = id,
                DisplayName = displayName,
                UnitId = ItemUnitId,
                BaseWeight = 1,
                PerishabilityBasisPoints = 0,
                QualityDimensionIds = QualityDimensions(
                    PurityQualityDimensionId,
                    IntegrityQualityDimensionId),
                CategoryTags = new List<string>
                {
                    "product.byproduct",
                    "product.material",
                    "product.market"
                }
            });
        }

        private static void AddOpenProduct(
            ProductionContentPackageDefinition package,
            string id,
            string displayName,
            int baseWeight,
            int perishabilityBasisPoints,
            params string[] categoryTags)
        {
            var tags = new List<string>(categoryTags);
            package.Products.Add(new ProductDefinition
            {
                Id = id,
                DisplayName = displayName,
                UnitId = ItemUnitId,
                BaseWeight = baseWeight,
                PerishabilityBasisPoints = perishabilityBasisPoints,
                QualityDimensionIds = QualityDimensionsForOpenProduct(tags),
                CategoryTags = tags
            });
        }

        private static List<string> QualityDimensionsForOpenProduct(
            List<string> categoryTags)
        {
            if (categoryTags.Contains("product.livestock"))
            {
                return QualityDimensions(
                    HealthQualityDimensionId,
                    UniformityQualityDimensionId);
            }

            if (categoryTags.Contains("product.food"))
            {
                return QualityDimensions(
                    HygieneQualityDimensionId,
                    NutritionQualityDimensionId);
            }

            if (categoryTags.Contains("product.fodder"))
            {
                return QualityDimensions(
                    NutritionQualityDimensionId,
                    PurityQualityDimensionId);
            }

            return QualityDimensions(
                PurityQualityDimensionId,
                IntegrityQualityDimensionId);
        }

        private static List<string> QualityDimensions(params string[] ids)
        {
            return new List<string>(ids);
        }

        private static void AddQualityDimension(
            ProductionContentPackageDefinition package,
            string id,
            string displayName)
        {
            package.QualityDimensions.Add(new QualityDimensionDefinition
            {
                Id = id,
                DisplayName = displayName,
                HistoricalStatus = "gameplay_completion",
                SourceNote = "多维品质的原创玩法抽象；具体数值由世界事实确定。"
            });
        }

        private static ProductionQuantityDefinition Quantity(
            string productDefinitionId,
            long quantity)
        {
            return new ProductionQuantityDefinition
            {
                ProductDefinitionId = productDefinitionId,
                QuantityPerLandUnit = quantity
            };
        }

        private static void AddPrimaryProcessingRecipe(
            ProductionContentPackageDefinition package,
            string id,
            string displayName,
            int durationDays,
            string facilityTag,
            string firstInputProductId,
            long firstInputQuantity,
            string firstOutputProductId,
            long firstOutputQuantity,
            string secondOutputProductId,
            long secondOutputQuantity,
            string secondInputProductId = null,
            long secondInputQuantity = 0)
        {
            var inputs = new List<ProductionQuantityDefinition>
            {
                new ProductionQuantityDefinition
                {
                    ProductDefinitionId = firstInputProductId,
                    QuantityPerLandUnit = firstInputQuantity
                }
            };
            if (!string.IsNullOrEmpty(secondInputProductId))
            {
                inputs.Add(new ProductionQuantityDefinition
                {
                    ProductDefinitionId = secondInputProductId,
                    QuantityPerLandUnit = secondInputQuantity
                });
            }

            package.Recipes.Add(new RecipeDefinition
            {
                Id = id,
                DisplayName = displayName,
                DurationDays = durationDays,
                FacilityTags = new List<string> { facilityTag },
                Inputs = inputs,
                Outputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = firstOutputProductId,
                        QuantityPerLandUnit = firstOutputQuantity
                    },
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = secondOutputProductId,
                        QuantityPerLandUnit = secondOutputQuantity
                    }
                }
            });
        }

        private static void AddManufacturingRecipe(
            ProductionContentPackageDefinition package,
            string id,
            string displayName,
            int durationDays,
            string facilityTag,
            string outputProductId,
            string firstInputProductId,
            long firstInputQuantity,
            string secondInputProductId,
            long secondInputQuantity)
        {
            package.Recipes.Add(new RecipeDefinition
            {
                Id = id,
                DisplayName = displayName,
                DurationDays = durationDays,
                FacilityTags = new List<string> { facilityTag },
                Inputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = firstInputProductId,
                        QuantityPerLandUnit = firstInputQuantity
                    },
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = secondInputProductId,
                        QuantityPerLandUnit = secondInputQuantity
                    }
                },
                Outputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = outputProductId,
                        QuantityPerLandUnit = 1
                    }
                }
            });
        }

        private static void AddManufacturingMethod(
            ProductionContentPackageDefinition package,
            string id,
            string displayName,
            params string[] recipeIds)
        {
            string skillId;
            int difficulty;
            List<QualityDimensionModifierDefinition> modifiers;
            switch (id)
            {
                case BlacksmithingMethodId:
                    skillId = CoreSkillIds.Metalworking;
                    difficulty = 12_000;
                    modifiers = QualityModifiers(
                        WorkmanshipQualityDimensionId, 200,
                        DurabilityQualityDimensionId, 100);
                    break;
                case BloomerySmeltingMethodId:
                    skillId = CoreSkillIds.Metalworking;
                    difficulty = 13_000;
                    modifiers = QualityModifiers(
                        PurityQualityDimensionId, 250,
                        IntegrityQualityDimensionId, 100);
                    break;
                case WoodworkingMethodId:
                    skillId = CoreSkillIds.Woodworking;
                    difficulty = 10_000;
                    modifiers = QualityModifiers(
                        WorkmanshipQualityDimensionId, 150,
                        DurabilityQualityDimensionId, 100);
                    break;
                case EarthKilnCharcoalMethodId:
                    skillId = CoreSkillIds.Woodworking;
                    difficulty = 8_000;
                    modifiers = QualityModifiers(
                        PurityQualityDimensionId, 150,
                        UniformityQualityDimensionId, 100);
                    break;
                case BowmakingMethodId:
                case HornFinishingMethodId:
                    skillId = CoreSkillIds.Bowmaking;
                    difficulty = id == BowmakingMethodId ? 14_000 : 9_000;
                    modifiers = QualityModifiers(
                        WorkmanshipQualityDimensionId, 250,
                        DurabilityQualityDimensionId, 100);
                    break;
                case ArmoringMethodId:
                    skillId = CoreSkillIds.Armoring;
                    difficulty = 15_000;
                    modifiers = QualityModifiers(
                        DurabilityQualityDimensionId, 200,
                        WorkmanshipQualityDimensionId, 200);
                    break;
                case VegetableTanningMethodId:
                    skillId = CoreSkillIds.Tanning;
                    difficulty = 11_000;
                    modifiers = QualityModifiers(
                        DurabilityQualityDimensionId, 200,
                        PurityQualityDimensionId, 100);
                    break;
                case PastureBreedingMethodId:
                    skillId = CoreSkillIds.Husbandry;
                    difficulty = 9_000;
                    modifiers = QualityModifiers(
                        HealthQualityDimensionId, 150,
                        UniformityQualityDimensionId, 50);
                    break;
                case ManualSlaughterMethodId:
                    skillId = CoreSkillIds.Husbandry;
                    difficulty = 8_000;
                    modifiers = QualityModifiers(
                        HygieneQualityDimensionId, 200,
                        IntegrityQualityDimensionId, 100);
                    break;
                case HerbalDryingMethodId:
                    skillId = CoreSkillIds.HerbalProcessing;
                    difficulty = 8_000;
                    modifiers = QualityModifiers(
                        PurityQualityDimensionId, 200,
                        IntegrityQualityDimensionId, 100);
                    break;
                default:
                    throw new ProductionContentException(
                        $"Core method {id} has no practice contract.");
            }

            package.Methods.Add(new ProductionMethodDefinition
            {
                Id = id,
                DisplayName = displayName,
                RecipeDefinitionIds = new List<string>(recipeIds),
                PracticeSkillDefinitionId = skillId,
                PracticeDifficultyBasisPoints = difficulty,
                QualityDimensionModifiers = modifiers,
                YieldBasisPoints = 10_000,
                LaborBasisPoints = 10_000,
                HistoricalStatus = "historical_inference"
            });
        }

        private static List<QualityDimensionModifierDefinition>
            QualityModifiers(
                string firstDimensionId,
                int firstModifier,
                string secondDimensionId = null,
                int secondModifier = 0)
        {
            var result = new List<QualityDimensionModifierDefinition>
            {
                new QualityDimensionModifierDefinition
                {
                    QualityDimensionId = firstDimensionId,
                    ModifierBasisPoints = firstModifier
                }
            };
            if (!string.IsNullOrEmpty(secondDimensionId))
            {
                result.Add(new QualityDimensionModifierDefinition
                {
                    QualityDimensionId = secondDimensionId,
                    ModifierBasisPoints = secondModifier
                });
            }

            return result;
        }

        private static void AddProductionSkill(
            ProductionContentPackageDefinition package,
            string id,
            string displayName,
            string fieldId)
        {
            package.Skills.Add(new SkillDefinition
            {
                Id = id,
                DisplayName = displayName,
                FieldId = fieldId,
                HistoricalStatus = "system_bridge",
                SourceNote = "由真实生产工单积累的开放式专门技艺。"
            });
        }

        private static TechnologyDefinition CreateAgricultureTechnology(
            string id,
            string displayName,
            string description,
            int requiredSkill,
            int researchPoints,
            long fundingCost,
            long applicationFundingCost,
            string effectId,
            int yieldBasisPoints,
            int laborBasisPoints)
        {
            return new TechnologyDefinition
            {
                Id = id,
                DisplayName = displayName,
                FieldId = "field.agriculture_water",
                Description = description,
                HistoricalStatus = "historical_inference",
                SourceNote = "历史农业实践的机制抽象；数值为原创玩法补全。",
                RequiredSkillDefinitionId = CoreSkillIds.Agriculture,
                RequiredSkillBasisPoints = requiredSkill,
                RequiredKnowledgeMasteryBasisPoints = 5_000,
                ResearchPointsRequired = researchPoints,
                FundingCost = fundingCost,
                ApplicationFundingCost = applicationFundingCost,
                RequiredKnowledgeDefinitionIds = new List<string>
                {
                    CoreKnowledgeIds.SeasonalObservation
                },
                ResearchFacilityTags = new List<string>
                {
                    VillageFacilityTags.AssemblyHall,
                    VillageFacilityTags.Farmland
                },
                Effects = new List<TechnologyEffectDefinition>
                {
                    new TechnologyEffectDefinition
                    {
                        Id = effectId,
                        TargetFacilityTag = VillageFacilityTags.Farmland,
                        RecipeDefinitionId = GrowWheatRecipeId,
                        MethodDefinitionId = PrototypeDrylandMethodId,
                        YieldBasisPoints = yieldBasisPoints,
                        LaborBasisPoints = laborBasisPoints
                    }
                }
            };
        }
    }

    [Serializable]
    public sealed class AgricultureWorkOrderState
    {
        public string Id;
        public string VillageId;
        public string FamilyId;
        public string FieldFacilityId;
        public string StorageFacilityId;
        public string ManagerPersonId;
        public string CropDefinitionId;
        public string CropVarietyDefinitionId;
        public string RecipeDefinitionId;
        public string MethodDefinitionId;
        public string SeedProductDefinitionId;
        public string HarvestProductDefinitionId;
        public string UnitId;
        public ProductionControlMode ControlMode;
        public ProductionOrderStatus Status;
        public long CreatedDay;
        public long PlantingDay;
        public long HarvestDay;
        public long SettledDay = -1;
        public int LandUnits;
        public long SeedQuantityCommitted;
        public int RequiredLaborDays;
        public int AssignedLaborDays;
        public int TechnologyYieldBasisPoints = 10_000;
        public int TechnologyLaborBasisPoints = 10_000;
        public long ProducedQuantity;
        public long StoredQuantity;
        public long LostQuantity;
        public List<string> AssignedWorkerIds = new List<string>();
        public List<string> AppliedTechnologyIds = new List<string>();
    }

    [Serializable]
    public sealed class ProductionLedgerEntryState
    {
        public string Id;
        public string WorkOrderId;
        public string VillageId;
        public string FamilyId;
        public string FacilityId;
        public string PersonId;
        public string ProductDefinitionId;
        public string UnitId;
        public long Day;
        public ProductionLedgerEntryType Type;
        public long Quantity;
        public long FamilySeedGrainDelta;
        public long FamilyGrainDelta;
        public long FacilityInventoryDelta;
        public string Summary;
    }
}
