using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mandate.Persistence
{
    public sealed class Luoyang184LivingWorldCheckpointResult
    {
        public string CheckpointPath;
        public string ManifestPath;
        public string Sha256;
        public string DeterministicStateSha256;
        public long Bytes;
    }

    /// <summary>
    /// Writable derived overlay for the protected Luoyang source package.
    /// The gzip JSON format is deliberately explicit and migration-gated; the
    /// protected population/facility files are never opened for writing.
    /// </summary>
    public sealed class Luoyang184LivingWorldCheckpointStore
    {
        public const string CheckpointFileName = "living_world_v1.json.gz";
        public const string ManifestFileName = "manifest.json";

        public Luoyang184LivingWorldCheckpointResult Save(
            Luoyang184LivingWorldRuntimeState runtime,
            string generationDirectory)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (string.IsNullOrWhiteSpace(generationDirectory))
                throw new ArgumentException("A generation directory is required.",
                    nameof(generationDirectory));
            var root = Path.GetFullPath(generationDirectory);
            Directory.CreateDirectory(root);
            var checkpoint = Path.Combine(root, CheckpointFileName);
            var temporary = checkpoint + ".tmp";
            if (File.Exists(temporary)) File.Delete(temporary);
            using (var file = new FileStream(temporary, FileMode.CreateNew,
                       FileAccess.Write, FileShare.None))
            using (var gzip = new GZipStream(file, CompressionLevel.Optimal,
                       false))
            using (var text = new StreamWriter(gzip, new UTF8Encoding(false)))
            using (var json = new JsonTextWriter(text))
            {
                var serializer = JsonSerializer.Create(new JsonSerializerSettings
                {
                    Formatting = Formatting.None,
                    NullValueHandling = NullValueHandling.Include
                });
                serializer.Serialize(json, runtime);
            }
            if (File.Exists(checkpoint)) File.Delete(checkpoint);
            File.Move(temporary, checkpoint);
            var digest = Sha256(checkpoint);
            var deterministicStateDigest =
                ComputeDeterministicStateSha256(runtime);
            var manifest = new
            {
                schema = "mandate.luoyang-184.living-world-checkpoint.v6",
                format_version = runtime.Version,
                source_package_id = runtime.SourcePackageId,
                protected_package_digest = runtime.ProtectedPackageDigest,
                absolute_day = runtime.AbsoluteDay,
                person_count = runtime.Workforce.Count,
                household_count = runtime.Households.Count,
                facility_count = runtime.Facilities.Count,
                crop_count = runtime.Crops.Count,
                inventory_count = runtime.Inventories.Count,
                checkpoint_file = CheckpointFileName,
                checkpoint_bytes = new FileInfo(checkpoint).Length,
                checkpoint_sha256 = digest,
                deterministic_state_sha256 = deterministicStateDigest
            };
            var manifestPath = Path.Combine(root, ManifestFileName);
            File.WriteAllText(manifestPath,
                JsonConvert.SerializeObject(manifest, Formatting.Indented),
                new UTF8Encoding(false));
            return new Luoyang184LivingWorldCheckpointResult
            {
                CheckpointPath = checkpoint,
                ManifestPath = manifestPath,
                Sha256 = digest,
                DeterministicStateSha256 = deterministicStateDigest,
                Bytes = new FileInfo(checkpoint).Length
            };
        }

        public Luoyang184LivingWorldRuntimeState Load(string checkpointPath)
        {
            if (string.IsNullOrWhiteSpace(checkpointPath))
                throw new ArgumentException("A checkpoint path is required.",
                    nameof(checkpointPath));
            using (var file = File.OpenRead(Path.GetFullPath(checkpointPath)))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress,
                       false))
            using (var text = new StreamReader(gzip, Encoding.UTF8, true))
            using (var json = new JsonTextReader(text))
            {
                var serializer = JsonSerializer.CreateDefault();
                var runtime = serializer.Deserialize<
                    Luoyang184LivingWorldRuntimeState>(json) ??
                    throw new InvalidDataException(
                        "Living-world checkpoint contained no runtime state.");
                return Migrate(runtime);
            }
        }

        private static Luoyang184LivingWorldRuntimeState Migrate(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            if (runtime.Version < 1 ||
                runtime.Version >
                    Luoyang184LivingWorldRuntimeState.FormatVersion)
                throw new InvalidDataException(
                    "Unsupported Luoyang living-world checkpoint version " +
                    runtime.Version + ".");
            if (runtime.Version == 1)
            {
                runtime.ExternalSuppliers ??=
                    new System.Collections.Generic.List<
                        LuoyangExternalSupplierRuntimeState>();
                runtime.SupplyOrders ??=
                    new System.Collections.Generic.List<
                        LuoyangSupplyOrderRuntimeState>();
                runtime.Shipments ??=
                    new System.Collections.Generic.List<
                        LuoyangShipmentRuntimeState>();
                runtime.Version = 2;
            }
            if (runtime.Version == 2)
            {
                runtime.MarketTrades ??= new System.Collections.Generic.List<
                    LuoyangMarketTradeRuntimeState>();
                runtime.IntelligentAgents ??= new System.Collections.Generic.List<
                    LuoyangIntelligentAgentRuntimeState>();
                runtime.DecisionAudits ??= new System.Collections.Generic.List<
                    LuoyangDecisionAuditState>();
                runtime.DecisionScheduleBuckets ??=
                    new System.Collections.Generic.List<
                        LuoyangDecisionScheduleBucketState>();
                runtime.FamilyOrganizations ??= new System.Collections.Generic.List<
                    LuoyangFamilyOrganizationRuntimeState>();
                runtime.ConstructionProjects ??=
                    new System.Collections.Generic.List<
                        LuoyangCompactConstructionProjectState>();
                runtime.GovernmentEconomy ??=
                    new LuoyangGovernmentEconomyRuntimeState();
                foreach (var order in runtime.SupplyOrders)
                {
                    if (order.UnitPrice <= 0) order.UnitPrice = 1;
                }
                runtime.Version = 3;
            }
            if (runtime.Version == 3)
            {
                runtime.CellProperties ??= new System.Collections.Generic.List<
                    LuoyangCellPropertyRuntimeState>();
                runtime.CellPropertyTransfers ??= new System.Collections.Generic.List<
                    LuoyangCellPropertyTransferRuntimeState>();
                foreach (var facility in runtime.Facilities)
                {
                    if (facility.CellId64 == 0) continue;
                    if (runtime.CellProperties.Exists(item =>
                            item.CellId64 == facility.CellId64)) continue;
                    runtime.CellProperties.Add(new LuoyangCellPropertyRuntimeState
                    {
                        CellId64 = facility.CellId64,
                        OwnerId = facility.OwnerId,
                        AdministrativeControllerId =
                            "organization.government.han.luoyang",
                        BuildingRightHolderId = facility.OwnerId,
                        FacilityId = facility.FacilityId
                    });
                }
                foreach (var project in runtime.ConstructionProjects)
                {
                    var target = runtime.Facilities.Find(item =>
                        item.FacilityId == project.TargetFacilityId);
                    project.Kind = LuoyangCompactConstructionKind.Expansion;
                    project.CellId64 = target?.CellId64 ?? 0;
                    project.OwnerId = target?.OwnerId ?? string.Empty;
                    project.FacilityDefinitionId = target?.DefinitionId ?? string.Empty;
                    project.LegacyImported = true;
                    project.MigrationNote =
                        "Imported from v3 compact project; v3 did not record " +
                        "material classes or labourer ordinals.";
                    project.Materials ??= new System.Collections.Generic.List<
                        LuoyangCompactConstructionMaterialState>();
                    if (project.MaterialQuantityMilliunits > 0 &&
                        !string.IsNullOrWhiteSpace(project.MaterialInventoryId))
                        project.Materials.Add(new LuoyangCompactConstructionMaterialState
                        {
                            InventoryId = project.MaterialInventoryId,
                            ProductId = project.MaterialProductId,
                            ConsumedMilliunits = project.MaterialQuantityMilliunits
                        });
                }
                runtime.RequiresSourceRehydration = runtime.Facilities.Exists(
                    item => item.CellId64 == 0);
                runtime.MigrationWarnings ??= new System.Collections.Generic.List<string>();
                if (runtime.RequiresSourceRehydration)
                    runtime.MigrationWarnings.Add(
                        "v3 checkpoint lacks Facility Cell IDs; reload the protected " +
                        "source package and create a current checkpoint before continuing simulation.");
                runtime.Version = 4;
            }
            if (runtime.Version == 4)
            {
                runtime.FamilyAssets ??= new System.Collections.Generic.List<
                    LuoyangFamilyAssetRuntimeState>();
                runtime.PersonDevelopment ??= new System.Collections.Generic.List<
                    LuoyangPersonDevelopmentRuntimeState>();
                runtime.Offices ??= new System.Collections.Generic.List<
                    LuoyangOfficeRuntimeState>();
                runtime.Taxes ??= new System.Collections.Generic.List<
                    LuoyangTaxRuntimeState>();
                runtime.Forces ??= new System.Collections.Generic.List<
                    LuoyangMilitaryForceRuntimeState>();
                runtime.SocialPressureHistory ??= new System.Collections.Generic.List<
                    LuoyangSocialPressureRuntimeState>();
                runtime.HistoricalEvents ??= new System.Collections.Generic.List<
                    LuoyangHistoricalEventRuntimeState>();
                runtime.PlayerCommands ??= new System.Collections.Generic.List<
                    LuoyangPlayerCommandRuntimeState>();
                runtime.MigrationWarnings ??= new System.Collections.Generic.List<string>();
                runtime.Version = 5;
            }
            if (runtime.Version == 5)
            {
                const string luoyang = "location.capital.luoyang";
                foreach (var person in runtime.Workforce)
                {
                    if (string.IsNullOrWhiteSpace(person.CurrentLocationId))
                        person.CurrentLocationId = luoyang;
                    person.TransitArrivalDay = -1;
                }
                foreach (var development in runtime.PersonDevelopment)
                    if (string.IsNullOrWhiteSpace(development.CurrentLocationId))
                        development.CurrentLocationId = luoyang;
                foreach (var inventory in runtime.Inventories)
                {
                    if (string.IsNullOrWhiteSpace(inventory.CurrentLocationId))
                        inventory.CurrentLocationId = luoyang;
                    inventory.TransitArrivalDay = -1;
                }
                foreach (var force in runtime.Forces)
                {
                    if (string.IsNullOrWhiteSpace(force.CurrentLocationId))
                        force.CurrentLocationId = luoyang;
                    force.TransitArrivalDay = -1;
                }
                if (string.IsNullOrWhiteSpace(
                        runtime.GovernmentEconomy.CurrentLocationId))
                    runtime.GovernmentEconomy.CurrentLocationId = luoyang;
                if (string.IsNullOrWhiteSpace(
                        runtime.GovernmentEconomy.GranaryInventoryId))
                {
                    var template = runtime.Inventories.Find(item =>
                        item.OwnerKind == LuoyangInventoryOwnerKind.Government) ??
                        runtime.Inventories.Find(item =>
                            item.OwnerKind == LuoyangInventoryOwnerKind.Market);
                    if (template != null)
                    {
                        var granary = new LuoyangInventoryBalanceState
                        {
                            Id = "inventory.government.luoyang.184.grain_tax",
                            OwnerKind = LuoyangInventoryOwnerKind.Government,
                            OwnerId = runtime.GovernmentEconomy.OrganizationId,
                            FacilityId = template.FacilityId,
                            ProductId = "product.reference.food_equivalent",
                            CurrentLocationId = luoyang,
                            TransitArrivalDay = -1,
                            CapacityMilliunits = Math.Max(1_000_000L,
                                template.CapacityMilliunits)
                        };
                        if (!runtime.Inventories.Exists(item =>
                                item.Id == granary.Id))
                            runtime.Inventories.Add(granary);
                        runtime.GovernmentEconomy.GranaryInventoryId = granary.Id;
                    }
                }
                runtime.CurrentLocalPopulation = runtime.Workforce.Count;
                runtime.Version = 6;
            }
            return runtime;
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
            {
                var digest = hash.ComputeHash(stream);
                var builder = new StringBuilder(digest.Length * 2);
                foreach (var value in digest) builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
        }

        public static string ComputeDeterministicStateSha256(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            using (var hash = SHA256.Create())
            using (var crypto = new CryptoStream(Stream.Null, hash,
                       CryptoStreamMode.Write))
            using (var text = new StreamWriter(crypto,
                       new UTF8Encoding(false), 4096, true))
            using (var json = new JsonTextWriter(text))
            {
                var serializer = JsonSerializer.Create(
                    new JsonSerializerSettings
                    {
                        Formatting = Formatting.None,
                        NullValueHandling = NullValueHandling.Include,
                        ContractResolver = new AuthorityStateContractResolver()
                    });
                serializer.Serialize(json, runtime);
                json.Flush();
                text.Flush();
                crypto.FlushFinalBlock();
                var digest = hash.Hash;
                var builder = new StringBuilder(digest.Length * 2);
                foreach (var value in digest) builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
        }

        private sealed class AuthorityStateContractResolver :
            DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(
                System.Reflection.MemberInfo member,
                MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                if (member.DeclaringType ==
                        typeof(Luoyang184LivingWorldRuntimeState) &&
                    member.Name == nameof(
                        Luoyang184LivingWorldRuntimeState.Performance))
                    property.Ignored = true;
                return property;
            }
        }
    }

    public static class Luoyang184LivingWorldEvidenceStore
    {
        public static void Write(string path, object evidence)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("An evidence path is required.",
                    nameof(path));
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
            File.WriteAllText(fullPath,
                JsonConvert.SerializeObject(evidence, Formatting.Indented),
                new UTF8Encoding(false));
        }
    }
}
