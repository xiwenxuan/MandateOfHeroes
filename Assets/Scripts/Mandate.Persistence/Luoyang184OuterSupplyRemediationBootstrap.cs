using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;
using Newtonsoft.Json.Linq;

namespace Mandate.Persistence
{
    public sealed class Luoyang184OuterSupplyRemediationIntegrationResult
    {
        public int PermanentPersonCount;
        public int HouseholdCount;
        public int FacilityCount;
        public int AddedPersonCount;
        public int AddedHouseholdCount;
        public int AddedFacilityCount;
        public int AddedResidenceCapacity;
        public long ElapsedMilliseconds;
        public bool WasAlreadyIntegrated;
    }

    /// <summary>
    /// Extends the already-integrated protected Luoyang world with the additive
    /// outer-supply population package. Existing historical identities and the
    /// accepted 400K facts remain unchanged.
    /// </summary>
    public sealed class Luoyang184OuterSupplyRemediationBootstrap
    {
        private readonly Luoyang184OuterSupplyRemediationPopulationSource source;

        public Luoyang184OuterSupplyRemediationBootstrap(string rootPath)
        {
            source = new Luoyang184OuterSupplyRemediationPopulationSource(
                rootPath);
        }

        public Luoyang184OuterSupplyRemediationPopulationSource Source => source;

        public Luoyang184OuterSupplyRemediationIntegrationResult Integrate(
            WorldState world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.SchemaVersion != WorldState.CurrentSchemaVersion)
                throw new InvalidOperationException(
                    "Migrate the world before applying outer-supply remediation.");
            var integration = world.HistoricalPersonFamilyIntegrations
                .SingleOrDefault(item => item.Id ==
                    Luoyang184HistoricalPersonFamilyIntegrationBootstrap
                        .IntegrationId) ??
                throw new InvalidOperationException(
                    "The protected Luoyang integration must be applied first.");
            if (string.Equals(integration.PopulationPackageId,
                    Luoyang184OuterSupplyRemediationPopulationSource
                        .PopulationPackageId,
                    StringComparison.Ordinal))
            {
                world.Validate();
                return BuildResult(world, 0, true);
            }
            if (world.PopulationStorage.PermanentPersonCount != 400_000 ||
                world.Facilities.Count != 2_084)
                throw new InvalidOperationException(
                    "Outer-supply remediation requires the accepted 400K/2084 baseline.");
            var packageFailures = source.ValidatePackageFiles();
            if (packageFailures.Count != 0)
                throw new InvalidOperationException(
                    "Outer-supply remediation package validation failed: " +
                    string.Join(",", packageFailures));

            var stopwatch = Stopwatch.StartNew();
            var definitions = world.FacilityDefinitions.ToDictionary(
                item => item.Id, StringComparer.Ordinal);
            var facilityIds = new HashSet<string>(world.Facilities.Select(
                item => item.Id), StringComparer.Ordinal);
            var cellIds = new HashSet<ulong>(world.Facilities.Select(
                item => item.CellId64));
            var addedCapacity = 0;
            foreach (var token in source.AddedFacilityTokens.OrderBy(item =>
                         Integer(item, "global_facility_index")))
            {
                var id = Text(token, "facility_id");
                var definitionId = Text(token, "definition_id");
                var cellId = token["cell_id64"]?.Value<ulong>() ?? 0;
                if (!facilityIds.Add(id))
                    throw new InvalidOperationException(
                        "Duplicate remediation Facility ID: " + id);
                if (cellId == 0 || !cellIds.Add(cellId))
                    throw new InvalidOperationException(
                        "Remediation violates one Facility per Cell: " + cellId);
                if (!definitions.TryGetValue(definitionId,
                        out var definition))
                {
                    definition = new FacilityDefinitionState
                    {
                        Id = definitionId,
                        DisplayName = "外围乡里住区",
                        CategoryId = Text(token, "category_id")
                    };
                    definitions.Add(definitionId, definition);
                    world.FacilityDefinitions.Add(definition);
                }
                var capacity = Integer(token,
                    "residential_capacity_persons");
                definition.ResidentialCapacityPersons = Math.Max(
                    definition.ResidentialCapacityPersons, capacity);
                definition.WorkerCapacity = Math.Max(
                    definition.WorkerCapacity,
                    Integer(token, "worker_capacity"));
                foreach (var capability in token["capability_ids"]?.Values<string>() ??
                         Enumerable.Empty<string>())
                    if (!definition.CapabilityIds.Contains(capability))
                        definition.CapabilityIds.Add(capability);

                var residents = Integer(token, "current_residents");
                addedCapacity = checked(addedCapacity + capacity);
                world.Facilities.Add(new FacilityState
                {
                    Id = id,
                    DisplayName = Text(token, "display_name"),
                    DefinitionId = definitionId,
                    CellId64 = cellId,
                    OwnerId = Text(token, "owner_id"),
                    ControllerId = Text(token, "controller_id"),
                    AdministrativeControllerId = Text(token,
                        "administrative_controller_id"),
                    SettlementId = Text(token, "settlement_id"),
                    HistoricalConfidence =
                        HistoricalConfidenceLevel.GameplayReconstruction,
                    SpatialPrecision = HistoricalSpatialPrecision.Approximate,
                    SourceNote = Text(token, "data_origin"),
                    LifecycleStatus = FacilityLifecycleStatus.Operational,
                    PersonAssignmentAuthority =
                        FacilityPersonAssignmentAuthority
                            .ExternalPermanentPopulationPackage,
                    ResidentPersonCount = residents,
                    WorkerPersonCount = Integer(token, "current_workers"),
                    StorageCapacity = Long(token, "storage_capacity_units")
                });
            }

            world.PopulationStorage = source.OpenCurrent().ToDomainState();
            integration.PopulationPackageId =
                Luoyang184OuterSupplyRemediationPopulationSource
                    .PopulationPackageId;
            integration.ProtectedPackageDigest = source.ProtectedPackageDigest;
            integration.PermanentPersonCount = source.PersonCount;
            integration.HouseholdCount = source.HouseholdCount;
            integration.FacilityCount = source.FacilityCount;
            integration.AddedPersonCount = source.AddedPersonCount;
            integration.AddedFacilityCount = source.AddedFacilityCount;
            integration.ExternalPopulationReferencesVerified = true;
            stopwatch.Stop();
            integration.InitializationElapsedMilliseconds = checked(
                integration.InitializationElapsedMilliseconds +
                stopwatch.ElapsedMilliseconds);

            PopulationStorageWorldAdapter.ValidateAttachedPackage(world, source);
            world.Validate();
            return new Luoyang184OuterSupplyRemediationIntegrationResult
            {
                PermanentPersonCount = source.PersonCount,
                HouseholdCount = source.HouseholdCount,
                FacilityCount = source.FacilityCount,
                AddedPersonCount = source.AddedPersonCount,
                AddedHouseholdCount = source.AddedHouseholdCount,
                AddedFacilityCount = source.AddedFacilityCount,
                AddedResidenceCapacity = addedCapacity,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                WasAlreadyIntegrated = false
            };
        }

        private Luoyang184OuterSupplyRemediationIntegrationResult BuildResult(
            WorldState world, long elapsed, bool alreadyIntegrated) =>
            new Luoyang184OuterSupplyRemediationIntegrationResult
            {
                PermanentPersonCount = checked((int)world.PopulationStorage
                    .PermanentPersonCount),
                HouseholdCount = source.HouseholdCount,
                FacilityCount = world.Facilities.Count,
                AddedPersonCount = source.AddedPersonCount,
                AddedHouseholdCount = source.AddedHouseholdCount,
                AddedFacilityCount = source.AddedFacilityCount,
                AddedResidenceCapacity = source.AddedFacilityTokens.Sum(item =>
                    Integer(item, "residential_capacity_persons")),
                ElapsedMilliseconds = elapsed,
                WasAlreadyIntegrated = alreadyIntegrated
            };

        private static string Text(JToken token, string name) =>
            token[name]?.Value<string>() ?? string.Empty;

        private static int Integer(JToken token, string name) =>
            token[name]?.Value<int>() ?? 0;

        private static long Long(JToken token, string name) =>
            token[name]?.Value<long>() ?? 0;
    }
}
