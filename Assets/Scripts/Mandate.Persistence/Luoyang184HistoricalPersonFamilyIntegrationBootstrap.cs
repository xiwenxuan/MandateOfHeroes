using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json.Linq;

namespace Mandate.Persistence
{
    public sealed class Luoyang184HistoricalPersonFamilyIntegrationResult
    {
        public int HistoricalPersonCount;
        public int FamilyOrganizationCount;
        public int FamilyOrganizationMemberCount;
        public int FacilityCount;
        public int OfficeDefinitionCount;
        public int OfficeAssignmentCount;
        public int ActivePrimaryCenterCount;
        public int ActiveLocalCenterCount;
        public int DeferredCenterCount;
        public int RemovedMisassignedHistoricalMembershipCount;
        public int AddedPersonCount;
        public int AddedFacilityCount;
        public long InitializationElapsedMilliseconds;
        public long HistoricalQueryElapsedTicks;
        public long FamilyQueryElapsedTicks;
        public string ProtectedPackageDigest;
        public bool WasAlreadyIntegrated;
    }

    /// <summary>
    /// Projects the protected Luoyang-184 initialization/reference packages into
    /// one generic persisted world. It is deterministic and idempotent; package
    /// persons, households, facilities and cells are never regenerated.
    /// </summary>
    public sealed class Luoyang184HistoricalPersonFamilyIntegrationBootstrap
    {
        public const string IntegrationId =
            "integration.luoyang.184.historical_person_family.v1";
        public const string CanonicalPlaceId =
            "place.han140.sili.henan.luoyang";
        public const string GovernmentOrganizationId =
            "organization.government.han.luoyang";

        private readonly string metropolitanRoot;
        private readonly string historicalPersonRoot;

        public Luoyang184HistoricalPersonFamilyIntegrationBootstrap(
            string metropolitanRoot,
            string historicalPersonRoot)
        {
            this.metropolitanRoot = Path.GetFullPath(
                metropolitanRoot ?? throw new ArgumentNullException(nameof(metropolitanRoot)));
            this.historicalPersonRoot = Path.GetFullPath(
                historicalPersonRoot ?? throw new ArgumentNullException(nameof(historicalPersonRoot)));
        }

        public Luoyang184HistoricalPersonFamilyIntegrationResult Integrate(
            WorldState world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.SchemaVersion != WorldState.CurrentSchemaVersion)
                throw new InvalidOperationException(
                    "Migrate the world to the current schema before Luoyang integration.");
            var existing = world.HistoricalPersonFamilyIntegrations.SingleOrDefault(
                item => item.Id == IntegrationId);
            if (existing != null)
            {
                world.Validate();
                return BuildResult(world, existing, 0, true);
            }
            if (world.HistoricalIdentities.Count != 0 ||
                world.FamilyOrganizationProfiles.Count != 0 ||
                world.Facilities.Count != 0)
                throw new InvalidOperationException(
                    "Luoyang integration requires a world without conflicting imported facts.");

            var stopwatch = Stopwatch.StartNew();
            var store = new Luoyang184MetropolitanPopulationStore(metropolitanRoot);
            var packageFailures = store.Source.ValidatePackageFiles();
            if (packageFailures.Count != 0)
                throw new InvalidDataException(
                    "Protected Luoyang package validation failed: " +
                    string.Join(",", packageFailures));
            var historicalReader = new HanHistoricalPersonClanDatasetReader(
                historicalPersonRoot);
            var historicalFailures = historicalReader.ValidatePackageFiles();
            if (historicalFailures.Count != 0)
                throw new InvalidDataException(
                    "Historical person package validation failed: " +
                    string.Join(",", historicalFailures));

            var protectedDigest = ComputeProtectedPackageDigest(store.Source);
            world.PopulationStorage = store.OpenCurrent().ToDomainState();
            AddCanonicalPlace(world);
            AddGovernmentOrganization(world);
            AddFacilities(world, store);
            var removedMemberships = AddFamilyOrganizations(world, store);
            AddHistoricalPeopleAndOffices(world, store, historicalReader);
            AddDeferredFamilyCenters(world);

            stopwatch.Stop();
            var integration = new HistoricalPersonFamilyIntegrationState
            {
                Id = IntegrationId,
                ScenarioId = store.Source.Manifest.ScenarioId,
                PopulationPackageId = Luoyang184MetropolitanPopulationStore.PackageId,
                ProtectedPackageDigest = protectedDigest,
                PermanentPersonCount = store.Source.Manifest.PersonCount,
                HouseholdCount = store.Source.Manifest.HouseholdCount,
                FacilityCount = store.Source.Manifest.FacilityCount,
                HistoricalPersonCount = store.Source.Manifest.HistoricalPersonCount,
                FamilyOrganizationCount = 15,
                AddedPersonCount = 0,
                AddedFacilityCount = 0,
                ExternalPopulationReferencesVerified = true,
                InitializationElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            };
            world.HistoricalPersonFamilyIntegrations.Add(integration);

            ValidateExternalReferences(world, store);
            world.Validate();
            var result = BuildResult(world, integration, removedMemberships, false);
            MeasureQueries(world, result);
            return result;
        }

        public void ValidateExternalReferences(WorldState world,
            Luoyang184MetropolitanPopulationStore store)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (store == null) throw new ArgumentNullException(nameof(store));
            PopulationStorageWorldAdapter.ValidateAttachedPackage(world, store);
            var facilityIds = new HashSet<string>(
                world.Facilities.Select(item => item.Id), StringComparer.Ordinal);
            foreach (var identity in world.HistoricalIdentities)
            {
                if (!store.TryGetOrdinal(identity.PersonId, out var ordinal) ||
                    ordinal != identity.PersonOrdinal ||
                    !store.TryReadCore(identity.PersonId, out var core) ||
                    !string.Equals(core.FamilyId, identity.HouseholdId,
                        StringComparison.Ordinal) ||
                    !string.Equals(store.GetFacilityId(
                            store.Source.ReadPersons(checked((int)ordinal), 1)
                                .Single().ResidenceFacilityIndex),
                        identity.ResidenceFacilityId, StringComparison.Ordinal) ||
                    !facilityIds.Contains(identity.ResidenceFacilityId))
                {
                    throw new InvalidDataException(
                        "Invalid historical Person/Household/Residence mapping: " +
                        identity.HistoricalPersonId);
                }
            }

            foreach (var member in world.FamilyOrganizationMembers)
            {
                if (!store.TryReadCore(member.PersonId, out _))
                    throw new InvalidDataException(
                        "Family organization references a non-existent permanent Person: " +
                        member.PersonId);
            }

            if (world.Facilities.Count != 2084 ||
                world.HistoricalIdentities.Count != 25 ||
                world.FamilyOrganizationProfiles.Count != 15 ||
                world.FamilyCenters.Any(item =>
                    item.Status == FamilyCenterOperationalStatus.Active) ||
                world.FacilityDefinitions.Any(item =>
                    item.CapabilityIds.Contains(FacilityCapabilityIds.FamilyManagement)))
            {
                throw new InvalidDataException(
                    "Luoyang integration conservation/center baseline failed.");
            }
        }

        private static void AddCanonicalPlace(WorldState world)
        {
            world.Locations.Add(new LocationState
            {
                Id = CanonicalPlaceId,
                DisplayName = "洛阳",
                Kind = LocationKind.RegionalSeat,
                Terrain = TerrainKind.Plains,
                Features = LocationFeature.Government | LocationFeature.Market |
                           LocationFeature.Garrison | LocationFeature.Fortification,
                StrategicImportance = 5,
                Population = 400000,
                PublicOrderBasisPoints = 5000
            });
            world.CanonicalPlaceCrosswalks.Add(new CanonicalPlaceCrosswalkState
            {
                Id = "place_crosswalk.han140.luoyang.v1",
                CanonicalPlaceId = CanonicalPlaceId,
                AdministrativeUnitId = "admin.han140.sili.henan.luoyang",
                StrategicCityId = "c027",
                ScenarioLocationId = "location.capital.luoyang",
                GeographicRegionId =
                    "geo.region.central.china.heluo.luoyangbasin.county.luoyang",
                WorldMapId = "hanworldv1",
                AnchorCellId64 = 4114717
            });
        }

        private static void AddGovernmentOrganization(WorldState world)
        {
            world.Organizations.Add(new OrganizationState
            {
                Id = GovernmentOrganizationId,
                DisplayName = "东汉洛阳官府",
                Type = OrganizationType.Government,
                HeadquartersLocationId = CanonicalPlaceId,
                LeaderPersonId = string.Empty,
                Treasury = 0,
                ReputationBasisPoints = 5000
            });
        }

        private void AddFacilities(WorldState world,
            Luoyang184MetropolitanPopulationStore store)
        {
            var urbanRoot = Path.GetFullPath(Path.Combine(
                metropolitanRoot,
                store.Source.MetropolitanManifest.BasePackageRelativePath));
            var urban = ReadArray(Path.Combine(urbanRoot, "facilities.json"),
                "facilities");
            var metro = ReadArray(Path.Combine(metropolitanRoot, "facilities.json"),
                "facilities");
            var ordered = urban.Concat(metro).OrderBy(item =>
                item["global_facility_index"]?.Value<int>() ??
                urban.IndexOf(item)).ToList();
            if (ordered.Count != store.Source.Manifest.FacilityCount)
                throw new InvalidDataException("Facility projection count mismatch.");

            var definitions = new Dictionary<string, FacilityDefinitionState>(
                StringComparer.Ordinal);
            for (var index = 0; index < ordered.Count; index++)
            {
                var token = ordered[index];
                var id = Text(token, "facility_id");
                if (!string.Equals(id, store.GetFacilityId(checked((uint)index)),
                        StringComparison.Ordinal))
                    throw new InvalidDataException(
                        "Facility index/ID mismatch at " + index + ".");
                var definitionId = Text(token, "definition_id");
                if (!definitions.TryGetValue(definitionId, out var definition))
                {
                    definition = new FacilityDefinitionState
                    {
                        Id = definitionId,
                        DisplayName = Text(token, "display_name"),
                        CategoryId = Text(token, "category_id")
                    };
                    definitions.Add(definitionId, definition);
                }
                definition.ResidentialCapacityPersons = Math.Max(
                    definition.ResidentialCapacityPersons,
                    Integer(token, "recommended_residential_capacity",
                        Integer(token, "residential_capacity_persons", 0)));
                definition.WorkerCapacity = Math.Max(
                    definition.WorkerCapacity,
                    Integer(token, "recommended_worker_capacity",
                        Integer(token, "worker_capacity", 0)));
                definition.MinimumWorkersForNormalOperation = Math.Max(
                    definition.MinimumWorkersForNormalOperation,
                    Integer(token, "minimum_workers_for_normal_operation", 0));
                AddDistinct(definition.CapabilityIds, Strings(token, "capability_ids"));
                AddDistinct(definition.PurposeIds, Strings(token, "purpose_ids"));
                AddDistinct(definition.FutureHookIds, Strings(token, "future_hook_ids"));
                AddDistinct(definition.AllowedResidentTypeIds,
                    Strings(token, "allowed_resident_type_ids"));

                world.Facilities.Add(new FacilityState
                {
                    Id = id,
                    DisplayName = Text(token, "display_name"),
                    DefinitionId = definitionId,
                    CellId64 = token["cell_id64"]?.Value<ulong>() ?? 0,
                    OwnerId = Text(token, "owner_id"),
                    ControllerId = Text(token, "controller_id"),
                    AdministrativeControllerId =
                        Text(token, "administrative_controller_id"),
                    SettlementId = Text(token, "settlement_id"),
                    HistoricalConfidence = ParseConfidence(
                        Text(token, "historical_confidence")),
                    SpatialPrecision = ParsePrecision(
                        Text(token, "spatial_precision")),
                    SourceNote = Text(token, "data_origin"),
                    LifecycleStatus = token["active"] != null &&
                                      !token["active"].Value<bool>()
                        ? FacilityLifecycleStatus.Disabled
                        : FacilityLifecycleStatus.Operational,
                    PersonAssignmentAuthority =
                        FacilityPersonAssignmentAuthority
                            .ExternalPermanentPopulationPackage,
                    ResidentPersonCount = Integer(token, "current_residents", 0),
                    WorkerPersonCount = Integer(token, "current_workers", 0),
                    StudentPersonCount = Integer(token, "current_students", 0),
                    StorageCapacity = Long(token, "storage_capacity_units",
                        Long(token, "storage_capacity", 0))
                });
            }
            world.FacilityDefinitions.AddRange(definitions.Values
                .OrderBy(item => item.Id, StringComparer.Ordinal));
        }

        private int AddFamilyOrganizations(WorldState world,
            Luoyang184MetropolitanPopulationStore store)
        {
            var urbanRoot = Path.GetFullPath(Path.Combine(
                metropolitanRoot,
                store.Source.MetropolitanManifest.BasePackageRelativePath));
            var urban = ReadArray(Path.Combine(urbanRoot, "family_organizations.json"),
                "organizations");
            var metro = ReadArray(Path.Combine(metropolitanRoot,
                "family_organizations.json"), "organizations");
            var removed = 0;
            foreach (var token in urban.Concat(metro))
            {
                var organizationId = Text(token, "family_organization_id");
                var sourceFamilyId = Text(token, "source_family_id");
                var headId = Text(token, "head_person_id");
                var explicitHistorical = new HashSet<string>(
                    Strings(token, "historical_member_person_ids"),
                    StringComparer.Ordinal);
                var allowedHistorical = GetAllowedHistoricalMembers(
                    organizationId, explicitHistorical);
                var ordinals = GetMemberOrdinals(token);
                var accepted = new List<uint>(ordinals.Count);
                foreach (var ordinal in ordinals.OrderBy(item => item))
                {
                    var personId = store.Source.GetPersonId(ordinal);
                    if (IsHistoricalId(personId) &&
                        !allowedHistorical.Contains(personId))
                    {
                        removed++;
                        continue;
                    }
                    accepted.Add(ordinal);
                }
                if (!accepted.Select(store.Source.GetPersonId).Contains(headId))
                    throw new InvalidDataException(
                        "Family organization migration removed its head: " + organizationId);

                world.Organizations.Add(new OrganizationState
                {
                    Id = organizationId,
                    DisplayName = Text(token, "family_name"),
                    Type = OrganizationType.Family,
                    HeadquartersLocationId = CanonicalPlaceId,
                    LeaderPersonId = string.Empty,
                    Treasury = Long(token, "family_treasury", 0),
                    ReputationBasisPoints = 5000
                });
                var profile = new FamilyOrganizationProfileState
                {
                    Id = "family_profile." + organizationId,
                    OrganizationId = organizationId,
                    SourceFamilyId = sourceFamilyId,
                    HeadPersonId = headId,
                    ClanId = ClanForFamily(sourceFamilyId),
                    BranchId = organizationId.EndsWith(".f088",
                        StringComparison.Ordinal)
                        ? "branch.han.v1.f415.eastern_han_mainline"
                        : string.Empty,
                    InventoryContainerId =
                        Text(token, "family_inventory_container_id"),
                    FamilyAssets = Long(token, "family_assets", 0),
                    HistoricalClaim = token["historical_claim"] == null ||
                                      token["historical_claim"].Value<bool>(),
                    MigrationStatus = urban.Contains(token)
                        ? "MIGRATED_CORRECTED"
                        : "RETAINED_GENERATED",
                    CenterResearchStatus = "DEFERRED_NO_QUALIFIED_FACILITY"
                };
                var sourceFacilityClaims = Strings(token,
                    "family_facility_ids").ToList();
                var householdIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var range in ContiguousRanges(accepted))
                {
                    foreach (var record in store.Source.ReadPersons(
                                 checked((int)range.Item1), range.Item2))
                    {
                        var personId = store.Source.GetPersonId(record.Ordinal);
                        var isHistorical = IsHistoricalId(personId);
                        world.FamilyOrganizationMembers.Add(
                            new FamilyOrganizationMemberState
                            {
                                Id = "family_member." + organizationId + "." +
                                     personId.ToLowerInvariant(),
                                OrganizationId = organizationId,
                                PersonId = personId,
                                RoleId = personId == headId
                                    ? "role.family_head"
                                    : "role.family_member",
                                IsHistoricalMember = isHistorical,
                                MembershipSource = isHistorical
                                    ? "historical_reviewed"
                                    : "protected_initialization_package"
                            });
                        householdIds.Add(store.GetHouseholdId(
                            record.HouseholdOrdinal));
                    }
                }
                profile.HouseholdIds.AddRange(householdIds.OrderBy(
                    item => item, StringComparer.Ordinal));
                world.FamilyOrganizationProfiles.Add(profile);

                if (profile.FamilyAssets > 0)
                {
                    world.OrganizationAssets.Add(new OrganizationAssetState
                    {
                        Id = "organization_asset." + organizationId + ".capital",
                        OrganizationId = organizationId,
                        AssetKindId = "asset.family_capital",
                        AssetReferenceId = profile.InventoryContainerId,
                        Quantity = profile.FamilyAssets,
                        OwnerId = organizationId
                    });
                }
                foreach (var facilityId in sourceFacilityClaims)
                {
                    var facility = world.Facilities.Single(item =>
                        item.Id == facilityId);
                    if (!string.Equals(facility.OwnerId, organizationId,
                            StringComparison.Ordinal) &&
                        !string.Equals(facility.ControllerId, organizationId,
                            StringComparison.Ordinal))
                    {
                        profile.UnresolvedFacilityClaimIds.Add(facilityId);
                        profile.MigrationStatus =
                            "RETAINED_WITH_UNRESOLVED_FACILITY_CLAIMS";
                        continue;
                    }
                    profile.FacilityIds.Add(facilityId);
                    world.OrganizationAssets.Add(new OrganizationAssetState
                    {
                        Id = "organization_asset." + organizationId + ".facility." +
                             facilityId,
                        OrganizationId = organizationId,
                        AssetKindId = "asset.facility",
                        AssetReferenceId = facilityId,
                        Quantity = 1,
                        OwnerId = organizationId
                    });
                }
            }
            if (world.FamilyOrganizationProfiles.Count != 15)
                throw new InvalidDataException("Family organization count mismatch.");
            return removed;
        }

        private void AddHistoricalPeopleAndOffices(WorldState world,
            Luoyang184MetropolitanPopulationStore store,
            HanHistoricalPersonClanDatasetReader historicalReader)
        {
            var urbanRoot = Path.GetFullPath(Path.Combine(
                metropolitanRoot,
                store.Source.MetropolitanManifest.BasePackageRelativePath));
            var overlays = ReadArray(Path.Combine(urbanRoot,
                "historical_persons.json"), "people");
            var catalogs = JObject.Parse(File.ReadAllText(
                Path.Combine(metropolitanRoot, "catalogs.json"), Encoding.UTF8));
            var offices = catalogs["offices"].Values<string>().ToList();
            var definitions = new Dictionary<string,
                CivilMilitaryOfficeDefinitionState>(StringComparer.Ordinal);
            foreach (var overlay in overlays.OrderBy(item =>
                         item["ordinal"].Value<uint>()))
            {
                var personId = Text(overlay, "person_id");
                var ordinal = overlay["ordinal"].Value<uint>();
                if (!string.Equals(store.Source.GetPersonId(ordinal), personId,
                        StringComparison.Ordinal))
                    throw new InvalidDataException(
                        "Historical overlay no longer matches permanent Person: " + personId);
                var personRecord = store.Source.ReadPersons(
                    checked((int)ordinal), 1).Single();
                var master = historicalReader.GetPerson(personId);
                var householdId = store.GetHouseholdId(
                    personRecord.HouseholdOrdinal);
                var residenceId = store.GetFacilityId(
                    personRecord.ResidenceFacilityIndex);
                var workplaceId = store.GetFacilityId(
                    personRecord.WorkFacilityIndex);
                world.HistoricalIdentities.Add(new HistoricalIdentityState
                {
                    Id = "historical_identity." + personId.ToLowerInvariant(),
                    HistoricalPersonId = personId,
                    PersonId = personId,
                    PersonOrdinal = ordinal,
                    ScenarioId = store.Source.Manifest.ScenarioId,
                    CanonicalName = master.CanonicalName,
                    ClanId = master.ClanId,
                    BranchId = master.LineageBranchId,
                    HouseholdId = householdId,
                    ResidenceFacilityId = residenceId,
                    Confidence = Text(overlay, "confidence"),
                    SourceId = master.SourceId
                });
                world.PersonLineages.Add(new PersonLineageState
                {
                    Id = "person_lineage." + personId.ToLowerInvariant(),
                    PersonId = personId,
                    ClanId = master.ClanId,
                    BranchId = master.LineageBranchId,
                    EvidenceLevel = master.EvidenceLevel,
                    ResearchStatus = master.ResearchStatus
                });
                var activityId = store.GetActivityId(personRecord.ActivityIndex);
                world.PersonPrimaryActivities.Add(new PersonPrimaryActivityState
                {
                    Id = "person_activity." + personId.ToLowerInvariant() + ".primary",
                    PersonId = personId,
                    ActivityId = activityId,
                    FacilityId = workplaceId,
                    OfficeAssignmentId = string.Empty,
                    IsActive = true
                });
                AddOffice(world, definitions, offices, personId, workplaceId,
                    activityId, personRecord.CivilOfficeIndex,
                    CivilMilitaryOfficeKind.Civil);
                AddOffice(world, definitions, offices, personId, workplaceId,
                    activityId, personRecord.MilitaryOfficeIndex,
                    CivilMilitaryOfficeKind.Military);
            }
            world.CivilMilitaryOfficeDefinitions.AddRange(definitions.Values
                .OrderBy(item => item.Id, StringComparer.Ordinal));
            foreach (var activity in world.PersonPrimaryActivities)
            {
                var assignment = world.CivilMilitaryOfficeAssignments.FirstOrDefault(
                    item => item.HolderPersonId == activity.PersonId && item.IsActive);
                activity.OfficeAssignmentId = assignment?.Id ?? string.Empty;
            }
            if (world.HistoricalIdentities.Count != 25)
                throw new InvalidDataException("Historical Person mapping count mismatch.");
        }

        private static void AddOffice(WorldState world,
            IDictionary<string, CivilMilitaryOfficeDefinitionState> definitions,
            IReadOnlyList<string> officeCatalog, string personId,
            string workplaceId, string activityId, ushort officeIndex,
            CivilMilitaryOfficeKind kind)
        {
            if (officeIndex == 0 || officeIndex >= officeCatalog.Count) return;
            var sourceOfficeId = officeCatalog[officeIndex];
            if (string.IsNullOrEmpty(workplaceId))
            {
                workplaceId = sourceOfficeId == "office.emperor" ||
                              sourceOfficeId == "office.empress"
                    ? "facility.instance.luoyang.184.north_palace"
                    : kind == CivilMilitaryOfficeKind.Military
                        ? "facility.instance.luoyang.184.barracks.2035.-2"
                        : "facility.instance.luoyang.184.central_offices_east";
            }
            var prefix = kind == CivilMilitaryOfficeKind.Civil
                ? "civil_office."
                : "military_office.";
            var definitionId = prefix + sourceOfficeId.Substring("office.".Length);
            if (!definitions.ContainsKey(definitionId))
            {
                definitions.Add(definitionId,
                    new CivilMilitaryOfficeDefinitionState
                    {
                        Id = definitionId,
                        DisplayName = sourceOfficeId,
                        Kind = kind,
                        JurisdictionId = CanonicalPlaceId,
                        GovernmentOrganizationId = GovernmentOrganizationId,
                        GovernmentFacilityId = workplaceId
                    });
            }
            world.CivilMilitaryOfficeAssignments.Add(
                new CivilMilitaryOfficeAssignmentState
                {
                    Id = "office_assignment." + definitionId + "." +
                         personId.ToLowerInvariant(),
                    OfficeDefinitionId = definitionId,
                    HolderPersonId = personId,
                    WorkplaceFacilityId = workplaceId,
                    CurrentActivityId = activityId,
                    StartedDay = 0,
                    IsActive = true
                });
        }

        private static void AddDeferredFamilyCenters(WorldState world)
        {
            foreach (var profile in world.FamilyOrganizationProfiles.OrderBy(
                         item => item.OrganizationId, StringComparer.Ordinal))
            {
                world.FamilyCenters.Add(new FamilyCenterState
                {
                    Id = "family_center." + profile.OrganizationId,
                    OrganizationId = profile.OrganizationId,
                    FacilityId = string.Empty,
                    ManagerPersonId = string.Empty,
                    ManagementScopeId = CanonicalPlaceId,
                    Designation = FamilyCenterDesignation.None,
                    Status = FamilyCenterOperationalStatus.Deferred,
                    ReadinessReason =
                        "No existing Facility satisfies capability, ownership/control, " +
                        "manager and designation prerequisites."
                });
            }
        }

        private static Luoyang184HistoricalPersonFamilyIntegrationResult BuildResult(
            WorldState world, HistoricalPersonFamilyIntegrationState integration,
            int removedMemberships, bool alreadyIntegrated)
        {
            return new Luoyang184HistoricalPersonFamilyIntegrationResult
            {
                HistoricalPersonCount = world.HistoricalIdentities.Count,
                FamilyOrganizationCount = world.FamilyOrganizationProfiles.Count,
                FamilyOrganizationMemberCount = world.FamilyOrganizationMembers.Count,
                FacilityCount = world.Facilities.Count,
                OfficeDefinitionCount = world.CivilMilitaryOfficeDefinitions.Count,
                OfficeAssignmentCount = world.CivilMilitaryOfficeAssignments.Count,
                ActivePrimaryCenterCount = world.FamilyCenters.Count(item =>
                    item.Status == FamilyCenterOperationalStatus.Active &&
                    item.Designation == FamilyCenterDesignation.Primary),
                ActiveLocalCenterCount = world.FamilyCenters.Count(item =>
                    item.Status == FamilyCenterOperationalStatus.Active &&
                    item.Designation == FamilyCenterDesignation.Local),
                DeferredCenterCount = world.FamilyCenters.Count(item =>
                    item.Status == FamilyCenterOperationalStatus.Deferred),
                RemovedMisassignedHistoricalMembershipCount = removedMemberships,
                AddedPersonCount = integration.AddedPersonCount,
                AddedFacilityCount = integration.AddedFacilityCount,
                InitializationElapsedMilliseconds =
                    integration.InitializationElapsedMilliseconds,
                ProtectedPackageDigest = integration.ProtectedPackageDigest,
                WasAlreadyIntegrated = alreadyIntegrated
            };
        }

        private static void MeasureQueries(WorldState world,
            Luoyang184HistoricalPersonFamilyIntegrationResult result)
        {
            var index = new HistoricalPersonFamilyRuntimeIndex(world);
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < 10000; i++)
                index.TryGetIdentity("P0038", out _);
            stopwatch.Stop();
            result.HistoricalQueryElapsedTicks = stopwatch.ElapsedTicks;
            stopwatch.Restart();
            for (var i = 0; i < 10000; i++)
                _ = index.GetOrganizationMembers(
                    "family_organization.luoyang.184.f088").Count;
            stopwatch.Stop();
            result.FamilyQueryElapsedTicks = stopwatch.ElapsedTicks;
        }

        private static HashSet<string> GetAllowedHistoricalMembers(
            string organizationId, HashSet<string> sourceMembers)
        {
            if (organizationId.EndsWith(".f088", StringComparison.Ordinal))
                return new HashSet<string>(new[] { "P0037", "P0038", "P0039", "P0040" },
                    StringComparer.Ordinal);
            if (organizationId.EndsWith(".f036", StringComparison.Ordinal))
                return new HashSet<string>(new[] { "P0035", "P0036" },
                    StringComparer.Ordinal);
            return sourceMembers;
        }

        private static string ClanForFamily(string familyId)
        {
            switch (familyId)
            {
                case "F036": return "clan.han.v1.f036";
                case "F077": return "clan.han.v1.f077";
                case "F081": return "clan.han.v1.f081";
                case "F092": return "clan.han.v1.f092";
                default: return string.Empty;
            }
        }

        private static List<uint> GetMemberOrdinals(JObject token)
        {
            var result = new HashSet<uint>();
            foreach (var value in token["member_ordinals"] ?? new JArray())
                result.Add(value.Value<uint>());
            foreach (var range in token["member_ordinal_ranges"] ?? new JArray())
            {
                var start = range["start"].Value<uint>();
                var count = range["count"].Value<int>();
                for (var offset = 0; offset < count; offset++)
                    result.Add(checked(start + (uint)offset));
            }
            return result.OrderBy(item => item).ToList();
        }

        private static IEnumerable<Tuple<uint, int>> ContiguousRanges(
            IReadOnlyList<uint> ordinals)
        {
            if (ordinals.Count == 0) yield break;
            var start = ordinals[0];
            var previous = start;
            for (var index = 1; index < ordinals.Count; index++)
            {
                if (ordinals[index] == previous + 1)
                {
                    previous = ordinals[index];
                    continue;
                }
                yield return Tuple.Create(start, checked((int)(previous - start + 1)));
                start = previous = ordinals[index];
            }
            yield return Tuple.Create(start, checked((int)(previous - start + 1)));
        }

        private static string ComputeProtectedPackageDigest(
            Luoyang184MetropolitanInitializationReader source)
        {
            var lines = source.MetropolitanManifest.BasePackageFiles
                .Select(item => "urban/" + item.Path + ":" + item.Sha256)
                .Concat(source.MetropolitanManifest.Files.Select(item =>
                    "metropolitan/" + item.Path + ":" + item.Sha256))
                .OrderBy(item => item, StringComparer.Ordinal);
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(string.Join("\n", lines));
                return string.Concat(sha.ComputeHash(bytes).Select(value =>
                    value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static List<JObject> ReadArray(string path, string property) =>
            JObject.Parse(File.ReadAllText(path, Encoding.UTF8))[property]
                .Children<JObject>().ToList();

        private static string Text(JObject token, string property) =>
            token[property]?.Type == JTokenType.Null
                ? string.Empty
                : token[property]?.Value<string>() ?? string.Empty;

        private static int Integer(JObject token, string property, int fallback) =>
            token[property]?.Type == JTokenType.Integer
                ? token[property].Value<int>()
                : fallback;

        private static long Long(JObject token, string property, long fallback) =>
            token[property]?.Type == JTokenType.Integer
                ? token[property].Value<long>()
                : fallback;

        private static IEnumerable<string> Strings(JObject token, string property) =>
            token[property]?.Values<string>() ?? Enumerable.Empty<string>();

        private static void AddDistinct(List<string> target,
            IEnumerable<string> values)
        {
            foreach (var value in values)
                if (!target.Contains(value)) target.Add(value);
        }

        private static bool IsHistoricalId(string personId) =>
            personId != null && personId.Length == 5 && personId[0] == 'P';

        private static HistoricalConfidenceLevel ParseConfidence(string value) =>
            Enum.TryParse(value, out HistoricalConfidenceLevel result)
                ? result
                : HistoricalConfidenceLevel.GameplayReconstruction;

        private static HistoricalSpatialPrecision ParsePrecision(string value) =>
            Enum.TryParse(value, out HistoricalSpatialPrecision result)
                ? result
                : HistoricalSpatialPrecision.Approximate;
    }
}
