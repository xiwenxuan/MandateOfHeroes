using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class FacilityCapabilityIds
    {
        public const string FamilyManagement = "capability.family_management";
    }

    public enum FamilyCenterDesignation : byte
    {
        None,
        Primary,
        Local
    }

    public enum FamilyCenterOperationalStatus : byte
    {
        Deferred,
        Active,
        Disabled,
        Lost
    }

    public enum CivilMilitaryOfficeKind : byte
    {
        Civil,
        Military
    }

    [Serializable]
    public sealed class CanonicalPlaceCrosswalkState
    {
        public string Id;
        public string CanonicalPlaceId;
        public string AdministrativeUnitId;
        public string StrategicCityId;
        public string ScenarioLocationId;
        public string GeographicRegionId;
        public string WorldMapId;
        public ulong AnchorCellId64;
    }

    [Serializable]
    public sealed class HistoricalIdentityState
    {
        public string Id;
        public string HistoricalPersonId;
        public string PersonId;
        public uint PersonOrdinal;
        public string ScenarioId;
        public string CanonicalName;
        public string ClanId;
        public string BranchId;
        public string HouseholdId;
        public string ResidenceFacilityId;
        public string Confidence;
        public string SourceId;
    }

    [Serializable]
    public sealed class PersonLineageState
    {
        public string Id;
        public string PersonId;
        public string ClanId;
        public string BranchId;
        public string EvidenceLevel;
        public string ResearchStatus;
    }

    [Serializable]
    public sealed class FamilyOrganizationProfileState
    {
        public string Id;
        public string OrganizationId;
        public string SourceFamilyId;
        public string HeadPersonId;
        public string ClanId;
        public string BranchId;
        public string InventoryContainerId;
        public long FamilyAssets;
        public bool HistoricalClaim;
        public string MigrationStatus;
        public string CenterResearchStatus;
        public List<string> HouseholdIds = new List<string>();
        public List<string> FacilityIds = new List<string>();
        public List<string> UnresolvedFacilityClaimIds = new List<string>();
    }

    [Serializable]
    public sealed class FamilyOrganizationMemberState
    {
        public string Id;
        public string OrganizationId;
        public string PersonId;
        public string RoleId;
        public bool IsHistoricalMember;
        public string MembershipSource;
    }

    [Serializable]
    public sealed class OrganizationAssetState
    {
        public string Id;
        public string OrganizationId;
        public string AssetKindId;
        public string AssetReferenceId;
        public long Quantity;
        public string OwnerId;
    }

    [Serializable]
    public sealed class FamilyCenterState
    {
        public string Id;
        public string OrganizationId;
        public string FacilityId;
        public string ManagerPersonId;
        public string ManagementScopeId;
        public FamilyCenterDesignation Designation;
        public FamilyCenterOperationalStatus Status =
            FamilyCenterOperationalStatus.Deferred;
        public string ReadinessReason;
    }

    [Serializable]
    public sealed class CivilMilitaryOfficeDefinitionState
    {
        public string Id;
        public string DisplayName;
        public CivilMilitaryOfficeKind Kind;
        public string JurisdictionId;
        public string GovernmentOrganizationId;
        public string GovernmentFacilityId;
    }

    [Serializable]
    public sealed class CivilMilitaryOfficeAssignmentState
    {
        public string Id;
        public string OfficeDefinitionId;
        public string HolderPersonId;
        public string WorkplaceFacilityId;
        public string CurrentActivityId;
        public long StartedDay;
        public bool IsActive = true;
    }

    [Serializable]
    public sealed class PersonPrimaryActivityState
    {
        public string Id;
        public string PersonId;
        public string ActivityId;
        public string FacilityId;
        public string OfficeAssignmentId;
        public bool IsActive = true;
    }

    [Serializable]
    public sealed class HistoricalPersonFamilyIntegrationState
    {
        public string Id;
        public string ScenarioId;
        public string PopulationPackageId;
        public string ProtectedPackageDigest;
        public int PermanentPersonCount;
        public int HouseholdCount;
        public int FacilityCount;
        public int HistoricalPersonCount;
        public int FamilyOrganizationCount;
        public int AddedPersonCount;
        public int AddedFacilityCount;
        public bool ExternalPopulationReferencesVerified;
        public long InitializationElapsedMilliseconds;
    }

    public sealed class HistoricalPersonFamilyRuntimeIndex
    {
        private readonly Dictionary<string, HistoricalIdentityState>
            identityByHistoricalId;
        private readonly Dictionary<string, List<string>> membersByClanId;
        private readonly Dictionary<string, List<string>> membersByOrganizationId;
        private readonly Dictionary<string, FamilyCenterState> centerByFacilityId;

        public HistoricalPersonFamilyRuntimeIndex(WorldState world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            identityByHistoricalId = world.HistoricalIdentities.ToDictionary(
                item => item.HistoricalPersonId,
                StringComparer.Ordinal);
            membersByClanId = world.PersonLineages
                .Where(item => !string.IsNullOrEmpty(item.ClanId))
                .GroupBy(item => item.ClanId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.PersonId)
                        .OrderBy(item => item, StringComparer.Ordinal).ToList(),
                    StringComparer.Ordinal);
            membersByOrganizationId = world.FamilyOrganizationMembers
                .GroupBy(item => item.OrganizationId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.PersonId)
                        .OrderBy(item => item, StringComparer.Ordinal).ToList(),
                    StringComparer.Ordinal);
            centerByFacilityId = world.FamilyCenters
                .Where(item => !string.IsNullOrEmpty(item.FacilityId))
                .ToDictionary(item => item.FacilityId, StringComparer.Ordinal);
        }

        public bool TryGetIdentity(string historicalPersonId,
            out HistoricalIdentityState identity) =>
            identityByHistoricalId.TryGetValue(historicalPersonId, out identity);

        public IReadOnlyList<string> GetClanMembers(string clanId) =>
            membersByClanId.TryGetValue(clanId, out var members)
                ? members
                : Array.Empty<string>();

        public IReadOnlyList<string> GetOrganizationMembers(string organizationId) =>
            membersByOrganizationId.TryGetValue(organizationId, out var members)
                ? members
                : Array.Empty<string>();

        public bool TryGetCenterByFacility(string facilityId,
            out FamilyCenterState center) =>
            centerByFacilityId.TryGetValue(facilityId, out center);
    }

    public static class HistoricalPersonFamilyIntegrationRules
    {
        public static void ValidateWorld(WorldState world)
        {
            if (world.HistoricalIdentities == null || world.PersonLineages == null ||
                world.FamilyOrganizationProfiles == null ||
                world.FamilyOrganizationMembers == null ||
                world.FamilyCenters == null ||
                world.OrganizationAssets == null || world.CivilMilitaryOfficeDefinitions == null ||
                world.CivilMilitaryOfficeAssignments == null || world.PersonPrimaryActivities == null ||
                world.CanonicalPlaceCrosswalks == null || world.HistoricalPersonFamilyIntegrations == null ||
                world.Facilities == null || world.FacilityDefinitions == null)
            {
                throw new InvalidOperationException(
                    "Historical person/family integration collections cannot be null.");
            }

            RequireUnique(world.HistoricalIdentities, item => item.Id,
                "historical identity");
            RequireUnique(world.HistoricalIdentities,
                item => item.HistoricalPersonId, "historical person mapping");
            RequireUnique(world.HistoricalIdentities,
                item => item.PersonId, "historical runtime person mapping");
            RequireUnique(world.PersonLineages, item => item.Id, "person lineage");
            RequireUnique(world.PersonLineages, item => item.PersonId,
                "person lineage mapping");
            RequireUnique(world.FamilyOrganizationProfiles, item => item.Id,
                "family organization profile");
            RequireUnique(world.FamilyOrganizationProfiles,
                item => item.OrganizationId, "family organization profile mapping");
            RequireUnique(world.FamilyOrganizationMembers, item => item.Id,
                "family organization member");
            RequireUnique(world.FamilyCenters, item => item.Id, "family center");
            RequireUnique(world.OrganizationAssets, item => item.Id,
                "organization asset");
            RequireUnique(world.CivilMilitaryOfficeDefinitions, item => item.Id,
                "civil/military office definition");
            RequireUnique(world.CivilMilitaryOfficeAssignments, item => item.Id,
                "civil/military office assignment");
            RequireUnique(world.PersonPrimaryActivities, item => item.Id,
                "person primary activity");
            RequireUnique(world.CanonicalPlaceCrosswalks, item => item.Id,
                "canonical place crosswalk");
            RequireUnique(world.HistoricalPersonFamilyIntegrations,
                item => item.Id, "historical person/family integration");
            RequireUnique(world.FacilityDefinitions, item => item.Id,
                "facility definition");
            RequireUnique(world.Facilities, item => item.Id, "facility");

            var people = new HashSet<string>(
                world.People.Select(item => item.Id), StringComparer.Ordinal);
            var externallyVerified = world.HistoricalPersonFamilyIntegrations.Any(item =>
                item.ExternalPopulationReferencesVerified &&
                item.PermanentPersonCount == world.PopulationStorage.PermanentPersonCount);
            foreach (var identity in world.HistoricalIdentities)
            {
                if (!string.Equals(identity.HistoricalPersonId, identity.PersonId,
                        StringComparison.Ordinal) ||
                    identity.PersonOrdinal >= world.PopulationStorage.PermanentPersonCount ||
                    !people.Contains(identity.PersonId) && !externallyVerified)
                {
                    throw new InvalidOperationException(
                        "A historical identity must bind the existing same-ID runtime Person: " +
                        identity.HistoricalPersonId);
                }
            }

            var organizations = new HashSet<string>(
                world.Organizations.Select(item => item.Id), StringComparer.Ordinal);
            var facilities = world.Facilities.ToDictionary(
                item => item.Id, StringComparer.Ordinal);
            var definitions = world.FacilityDefinitions.ToDictionary(
                item => item.Id, StringComparer.Ordinal);
            var memberships = new HashSet<string>(
                world.FamilyOrganizationMembers.Select(item =>
                    item.OrganizationId + "\n" + item.PersonId),
                StringComparer.Ordinal);
            var activeActivities = world.PersonPrimaryActivities
                .Where(item => item.IsActive)
                .GroupBy(item => item.PersonId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            if (activeActivities.Any(item => item.Value > 1))
            {
                throw new InvalidOperationException(
                    "A Person may have at most one active primary activity.");
            }

            foreach (var profile in world.FamilyOrganizationProfiles)
            {
                if (!organizations.Contains(profile.OrganizationId))
                    throw new InvalidOperationException(
                        "Missing family organization " + profile.OrganizationId + ".");
            }

            foreach (var member in world.FamilyOrganizationMembers)
            {
                if (!organizations.Contains(member.OrganizationId) ||
                    string.IsNullOrWhiteSpace(member.PersonId))
                    throw new InvalidOperationException(
                        "Invalid family organization member " + member.Id + ".");
            }

            foreach (var center in world.FamilyCenters)
            {
                if (!organizations.Contains(center.OrganizationId))
                    throw new InvalidOperationException(
                        "Missing family center organization " + center.OrganizationId + ".");
                if (center.Status != FamilyCenterOperationalStatus.Active)
                {
                    if (center.Designation != FamilyCenterDesignation.None)
                        throw new InvalidOperationException(
                            "An inactive family center cannot be designated.");
                    continue;
                }

                if (center.Designation == FamilyCenterDesignation.None ||
                    string.IsNullOrEmpty(center.FacilityId) ||
                    string.IsNullOrEmpty(center.ManagerPersonId) ||
                    !facilities.TryGetValue(center.FacilityId, out var facility) ||
                    facility.LifecycleStatus != FacilityLifecycleStatus.Operational ||
                    !definitions.TryGetValue(facility.DefinitionId, out var definition) ||
                    !definition.CapabilityIds.Contains(FacilityCapabilityIds.FamilyManagement) ||
                    !(string.Equals(facility.OwnerId, center.OrganizationId,
                          StringComparison.Ordinal) ||
                      string.Equals(facility.ControllerId, center.OrganizationId,
                          StringComparison.Ordinal)) ||
                    !memberships.Contains(center.OrganizationId + "\n" +
                                          center.ManagerPersonId) ||
                    !activeActivities.TryGetValue(center.ManagerPersonId, out var activityCount) ||
                    activityCount != 1)
                {
                    throw new InvalidOperationException(
                        "An active FamilyCenter requires a real capable Facility, legal control, " +
                        "designation, member-manager and one active primary activity: " + center.Id);
                }
            }

            foreach (var grouping in world.FamilyCenters
                         .Where(item => item.Status == FamilyCenterOperationalStatus.Active &&
                                        item.Designation == FamilyCenterDesignation.Primary)
                         .GroupBy(item => item.OrganizationId, StringComparer.Ordinal))
            {
                if (grouping.Count() > 1)
                    throw new InvalidOperationException(
                        "A family organization may have at most one Primary center: " + grouping.Key);
            }

            foreach (var assignment in world.CivilMilitaryOfficeAssignments)
            {
                if (!people.Contains(assignment.HolderPersonId) &&
                    !world.HistoricalIdentities.Any(item =>
                        item.PersonId == assignment.HolderPersonId) ||
                    !world.CivilMilitaryOfficeDefinitions.Any(item =>
                        item.Id == assignment.OfficeDefinitionId) ||
                    !string.IsNullOrEmpty(assignment.WorkplaceFacilityId) &&
                    !facilities.ContainsKey(assignment.WorkplaceFacilityId))
                {
                    throw new InvalidOperationException(
                        "Invalid civil/military office assignment " + assignment.Id + ".");
                }
            }
        }

        private static void RequireUnique<T>(IEnumerable<T> values,
            Func<T, string> idSelector, string label)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                var id = idSelector(value);
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                    throw new InvalidOperationException(
                        "Invalid or duplicate " + label + " ID: " + id + ".");
            }
        }
    }
}
