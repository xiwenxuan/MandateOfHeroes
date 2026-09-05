using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Presentation
{
    public enum CountyGoldenBlockArchetype : byte
    {
        ResidenceCourtyard,
        MarketFrontage,
        WorkshopYard,
        WarehouseCompound,
        CivicCourtyard
    }

    /// <summary>
    /// One render-only lot in the Luoyang golden-block prototype.  A lot may
    /// name a formal source Facility for audit and style selection, but it is
    /// never itself a Facility and owns no population, inventory or output.
    /// </summary>
    public sealed class CountyGoldenBlockLot
    {
        public float CenterRow { get; set; }
        public float CenterColumn { get; set; }
        public int RotationQuarterTurns { get; set; }
        public int Variant { get; set; }
        public CountyGoldenBlockArchetype Archetype { get; set; }
        public string PresentationProfileId { get; set; }
        public CountyBuildingModulePlan ModulePlan { get; set; }
        public string SourceFacilityId { get; set; }
        public bool IsDerivedPresentationOnly { get; set; }
    }

    /// <summary>
    /// Deterministically selects one 8x8 PlanningCell urban block and derives
    /// a compact street/compound presentation kit from the authoritative
    /// Luoyang layout.  The plan is disposable visual context, not world
    /// state and not a construction or save contract.
    /// </summary>
    public sealed class CountyGoldenBlockPresentationPlan
    {
        public const string Version =
            "mandate.luoyang.golden-block-presentation.v2";
        public const int BlockSizeCells = 8;
        public const int LotCount = 16;

        private readonly IReadOnlyList<CountyGoldenBlockLot> _lots;
        private readonly IReadOnlyList<string> _sourceFacilityIds;

        public CountyGoldenBlockPresentationPlan(
            Luoyang50mCountyLayoutPackage layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            var area = layout.UrbanAreaCandidate ??
                       throw new InvalidOperationException(
                           "Luoyang golden block requires an UrbanArea candidate.");
            var cityWall = layout.Fortifications.Where(item =>
                    string.Equals(item.DefinitionId,
                        "facility.fortification.city_wall",
                        StringComparison.Ordinal) ||
                    string.Equals(item.DefinitionId,
                        "facility.fortification.city_gate",
                        StringComparison.Ordinal))
                .ToArray();
            var minimumRow = cityWall.Length == 0 ? area.MinimumRow :
                cityWall.Min(item => item.LocalRow);
            var maximumRow = cityWall.Length == 0 ? area.MaximumRow :
                cityWall.Max(item => item.LocalRow);
            var minimumColumn = cityWall.Length == 0 ? area.MinimumColumn :
                cityWall.Min(item => item.LocalColumn);
            var maximumColumn = cityWall.Length == 0 ? area.MaximumColumn :
                cityWall.Max(item => item.LocalColumn);
            var candidates = layout.Facilities.Where(item =>
                    item.LocalRow >= minimumRow &&
                    item.LocalRow <= maximumRow &&
                    item.LocalColumn >= minimumColumn &&
                    item.LocalColumn <= maximumColumn &&
                    !CountyWorldSpacePresentationPlan
                        .IsSpecializedInfrastructure(item.DefinitionId) &&
                    !CountyWorldSpacePresentationPlan
                        .IsAgriculturalFacility(item))
                .ToArray();
            if (candidates.Length == 0)
                throw new InvalidOperationException(
                    "Luoyang golden block requires an urban Facility source.");

            var selected = candidates.GroupBy(item => Tuple.Create(
                    item.LocalRow / BlockSizeCells,
                    item.LocalColumn / BlockSizeCells))
                .Select(group => new
                {
                    Group = group.OrderBy(item => item.FacilityId,
                        StringComparer.Ordinal).ToArray(),
                    BucketRow = group.Key.Item1,
                    BucketColumn = group.Key.Item2,
                    Diversity = group.Select(item => Archetype(item.CategoryId,
                            item.DefinitionId)).Distinct().Count()
                })
                .OrderByDescending(item => item.Diversity)
                .ThenByDescending(item => item.Group.Length)
                .ThenBy(item => item.BucketRow)
                .ThenBy(item => item.BucketColumn)
                .First();

            MinimumRow = selected.BucketRow * BlockSizeCells;
            MinimumColumn = selected.BucketColumn * BlockSizeCells;
            MaximumRow = MinimumRow + BlockSizeCells - 1;
            MaximumColumn = MinimumColumn + BlockSizeCells - 1;
            _sourceFacilityIds = selected.Group.Select(item => item.FacilityId)
                .ToArray();
            _lots = BuildLots(selected.Group);
            StableSignature = CountyBuildingPresentationStableHash.Text(
                string.Join("|", new[]
            {
                Version, MinimumRow.ToString(), MinimumColumn.ToString(),
                string.Join(",", _sourceFacilityIds),
                string.Join(",", _lots.Select(item =>
                    ((int)item.Archetype) + ":" + item.Variant + ":" +
                    item.RotationQuarterTurns + ":" +
                    item.PresentationProfileId + ":" +
                    item.ModulePlan.StableSignature + ":" +
                    (item.SourceFacilityId ?? "context")))
            }));
        }

        public int MinimumRow { get; }
        public int MinimumColumn { get; }
        public int MaximumRow { get; }
        public int MaximumColumn { get; }
        public IReadOnlyList<CountyGoldenBlockLot> Lots => _lots;
        public CountyBuildingPresentationProfileCatalog Profiles =>
            CountyBuildingPresentationProfileCatalog.HanLuoyangV2;
        public IReadOnlyList<string> SourceFacilityIds => _sourceFacilityIds;
        public bool IsDerivedPresentationOnly => true;
        public ulong StableSignature { get; }

        public bool ContainsBucket(int bucketRow, int bucketColumn) =>
            bucketRow * BlockSizeCells == MinimumRow &&
            bucketColumn * BlockSizeCells == MinimumColumn;

        private IReadOnlyList<CountyGoldenBlockLot> BuildLots(
            IReadOnlyList<Luoyang50mLayoutFacility> source)
        {
            var byArchetype = source.GroupBy(item => Archetype(item.CategoryId,
                    item.DefinitionId))
                .ToDictionary(group => group.Key,
                    group => group.OrderBy(item => item.FacilityId,
                        StringComparer.Ordinal).ToArray());
            var required = Profiles.Profiles.Select(item => item.Archetype)
                .ToArray();
            var result = new List<CountyGoldenBlockLot>(LotCount);
            for (var index = 0; index < LotCount; index++)
            {
                var rowSlot = index / 4;
                var columnSlot = index % 4;
                var preferred = required[index % required.Length];
                Luoyang50mLayoutFacility formal = null;
                if (byArchetype.TryGetValue(preferred, out var matching) &&
                    matching.Length > 0)
                    formal = matching[index % matching.Length];
                else if (index >= required.Length && source.Count > 0)
                    formal = source[index % source.Count];
                var archetype = index < required.Length
                    ? preferred
                    : formal == null
                        ? preferred
                        : Archetype(formal.CategoryId, formal.DefinitionId);
                var sourceId = formal?.FacilityId ?? "context";
                var seed = CountyBuildingPresentationStableHash.Text(
                    sourceId + ":" + index);
                var profile = Profiles.Resolve(archetype);
                var variant = (int)((seed >> 7) % 6);
                result.Add(new CountyGoldenBlockLot
                {
                    CenterRow = MinimumRow + 0.95f + rowSlot * 2.02f,
                    CenterColumn = MinimumColumn + 0.95f +
                                   columnSlot * 2.02f,
                    RotationQuarterTurns = (int)(seed % 4),
                    Variant = variant,
                    Archetype = archetype,
                    PresentationProfileId = profile.ProfileId,
                    ModulePlan = profile.Resolve(sourceId, index),
                    SourceFacilityId = formal?.FacilityId,
                    IsDerivedPresentationOnly = true
                });
            }
            return result;
        }

        private static CountyGoldenBlockArchetype Archetype(string category,
            string definitionId) => CountyBuildingPresentationProfileCatalog
            .HanLuoyangV2.Resolve(definitionId, category).Archetype;
    }
}
