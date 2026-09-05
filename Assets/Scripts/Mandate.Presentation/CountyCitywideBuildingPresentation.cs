using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Presentation
{
    /// <summary>
    /// Read-only presentation classification for one formal county Facility.
    /// It selects a visual language and stable module variant; it never creates
    /// a Facility, Cell, ownership record or save payload.
    /// </summary>
    public sealed class CountyCitywideBuildingLanguageEntry
    {
        public CountyCitywideBuildingLanguageEntry(
            Luoyang50mLayoutFacility facility,
            CountyBuildingPresentationProfile profile,
            CountyBuildingModulePlan modules,
            bool preservesFormalModelIdentity)
        {
            Facility = facility ?? throw new ArgumentNullException(
                nameof(facility));
            Profile = profile ?? throw new ArgumentNullException(
                nameof(profile));
            Modules = modules ?? throw new ArgumentNullException(
                nameof(modules));
            PreservesFormalModelIdentity = preservesFormalModelIdentity;
        }

        public Luoyang50mLayoutFacility Facility { get; }
        public CountyBuildingPresentationProfile Profile { get; }
        public CountyBuildingModulePlan Modules { get; }
        public bool PreservesFormalModelIdentity { get; }
        public string FacilityId => Facility.FacilityId;
        public string ProfileId => Profile.ProfileId;
    }

    /// <summary>
    /// Deterministic bridge between the Golden Block building language and the
    /// complete authoritative Luoyang county layout. The plan classifies every
    /// non-infrastructure, non-agricultural building Facility while keeping
    /// named landmarks on their existing formal model path.
    /// </summary>
    public sealed class CountyCitywideBuildingLanguagePlan
    {
        public const string Version =
            "county-citywide-building-language.v1";
        public const int MaximumSharedRendererCount = 12;

        public CountyCitywideBuildingLanguagePlan(
            Luoyang50mCountyLayoutPackage layout,
            IEnumerable<string> formalIdentityFacilityIds = null,
            CountyBuildingPresentationProfileCatalog catalog = null)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            Catalog = catalog ??
                CountyBuildingPresentationProfileCatalog.HanLuoyangV2;
            var formalIdentities = new HashSet<string>(
                formalIdentityFacilityIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            SourceFacilityCount = layout.Facilities.Count;
            LayoutFingerprint = layout.DeclaredLayoutFingerprint;

            Entries = layout.Facilities
                .Where(IsBuildingLanguageCandidate)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                .Select(item =>
                {
                    var profile = Catalog.Resolve(item.DefinitionId,
                        item.CategoryId);
                    return new CountyCitywideBuildingLanguageEntry(item,
                        profile, profile.Resolve(item.FacilityId,
                            item.RotationQuarterTurns),
                        formalIdentities.Contains(item.FacilityId));
                })
                .ToArray();
            ContextEntries = Entries.Where(item =>
                    !item.PreservesFormalModelIdentity)
                .ToArray();
            FacilityCountByProfile = Catalog.Profiles.ToDictionary(
                item => item.ProfileId,
                item => Entries.Count(entry => string.Equals(
                    entry.ProfileId, item.ProfileId,
                    StringComparison.Ordinal)), StringComparer.Ordinal);
            ModuleCount = ContextEntries.Sum(item => item.Modules.Modules.Count);
            StableSignature = CountyBuildingPresentationStableHash.Text(
                Version + "|" + LayoutFingerprint + "|" + string.Join("|",
                    Entries.Select(item => item.FacilityId + ":" +
                        item.ProfileId + ":" +
                        item.Modules.StableSignature + ":" +
                        (item.PreservesFormalModelIdentity ? "formal" :
                            "context"))));
        }

        public CountyBuildingPresentationProfileCatalog Catalog { get; }
        public int SourceFacilityCount { get; }
        public string LayoutFingerprint { get; }
        public IReadOnlyList<CountyCitywideBuildingLanguageEntry> Entries
            { get; }
        public IReadOnlyList<CountyCitywideBuildingLanguageEntry> ContextEntries
            { get; }
        public IReadOnlyDictionary<string, int> FacilityCountByProfile { get; }
        public int ModuleCount { get; }
        public ulong StableSignature { get; }
        public bool IsDerivedPresentationOnly => true;
        public bool CreatesWorldFacts => false;

        public static bool IsBuildingLanguageCandidate(
            Luoyang50mLayoutFacility facility)
        {
            if (facility == null || CountyWorldSpacePresentationPlan
                    .IsSpecializedInfrastructure(facility.DefinitionId) ||
                CountyWorldSpacePresentationPlan.IsAgriculturalFacility(
                    facility)) return false;
            return !string.Equals(facility.CategoryId, "road",
                StringComparison.Ordinal) &&
                   !string.Equals(facility.CategoryId, "fortification",
                       StringComparison.Ordinal);
        }
    }
}
