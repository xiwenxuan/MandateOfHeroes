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
        public List<ProductDefinition> Products = new List<ProductDefinition>();
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
    public sealed class ProductDefinition
    {
        public string Id;
        public string DisplayName;
        public string UnitId;
        public List<string> CategoryTags = new List<string>();
        public int BaseWeight = 1;
        public int PerishabilityBasisPoints;
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
        public int YieldBasisPoints = 10_000;
        public int LaborBasisPoints = 10_000;
        public string HistoricalStatus;
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
        private Dictionary<string, ProductDefinition> _products =
            new Dictionary<string, ProductDefinition>(StringComparer.Ordinal);
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
        public int ProductCount => _products.Count;
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
            var products = Copy(_products);
            var recipes = Copy(_recipes);
            var methods = Copy(_methods);
            var skills = Copy(_skills);
            var knowledge = Copy(_knowledge);
            var technologies = Copy(_technologies);
            AddDefinitions(crops, package.Crops, item => item.Id, "crop");
            AddDefinitions(
                varieties, package.CropVarieties, item => item.Id, "crop variety");
            AddDefinitions(products, package.Products, item => item.Id, "product");
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
                products,
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
            _products = products;
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
                ContentSchemaVersion = 2,
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
            if (manifest == null || manifest.ContentSchemaVersion != 2 ||
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
            for (var i = 0; i < world.AgricultureWorkOrders.Count; i++)
            {
                var order = world.AgricultureWorkOrders[i];
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
            Dictionary<string, ProductDefinition> products,
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

            foreach (var pair in products)
            {
                ValidateId(pair.Value.UnitId, $"unit for product {pair.Key}");
                if (pair.Value.BaseWeight <= 0 ||
                    pair.Value.PerishabilityBasisPoints < 0 ||
                    pair.Value.PerishabilityBasisPoints > 10_000)
                {
                    throw new ProductionContentException(
                        $"Product {pair.Key} has invalid physical values.");
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
            }

            foreach (var pair in methods)
            {
                var method = pair.Value;
                if (method.YieldBasisPoints <= 0 ||
                    method.LaborBasisPoints <= 0 ||
                    method.RecipeDefinitionIds == null ||
                    method.RecipeDefinitionIds.Count == 0)
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
        public const string WheatCropId = "crop.wheat";
        public const string PrototypeNorthernWheatVarietyId =
            "crop_variety.wheat.prototype_northern";
        public const string WheatSeedProductId = "product.wheat_seed";
        public const string WheatGrainProductId = "product.wheat_grain";
        public const string GrowWheatRecipeId = "recipe.field.grow_wheat";
        public const string PrototypeDrylandMethodId =
            "method.farming.prototype_dryland";
        public const string GrainUnitId = "unit.grain";
        public const string LaborDayUnitId = "unit.labor_day";

        public static ProductionContentPackageDefinition CreatePackage()
        {
            var package = new ProductionContentPackageDefinition
            {
                PackageId = PackageId,
                Version = "2.0.0",
                LoadOrder = 0,
                Required = true
            };
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
                CategoryTags = new List<string>
                {
                    "product.food",
                    "product.grain",
                    "product.market",
                    "product.military_supply"
                }
            });
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
            package.Methods.Add(new ProductionMethodDefinition
            {
                Id = PrototypeDrylandMethodId,
                DisplayName = "原型旱作法",
                RecipeDefinitionIds = new List<string> { GrowWheatRecipeId },
                YieldBasisPoints = 10_000,
                LaborBasisPoints = 10_000,
                HistoricalStatus = "gameplay_completion"
            });
            package.Skills.Add(new SkillDefinition
            {
                Id = CoreSkillIds.Agriculture,
                DisplayName = "农业",
                FieldId = "field.agriculture_water",
                HistoricalStatus = "system_bridge",
                SourceNote = "映射既有人物农业能力；后续子技艺继续使用稳定ID扩展。"
            });
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
