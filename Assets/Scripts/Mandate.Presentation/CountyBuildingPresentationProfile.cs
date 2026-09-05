using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Presentation
{
    public enum CountyBuildingPresentationImportance : byte
    {
        Ordinary,
        Significant,
        Major,
        Landmark
    }

    public enum CountyBuildingRoofFamily : byte
    {
        DomesticGable,
        MarketCanopy,
        WorkshopLowGable,
        GranaryLongGable,
        CivicRaisedHip
    }

    public enum CountyBuildingFoundationFamily : byte
    {
        Earth,
        Formal,
        CivicTerrace
    }

    public enum CountyBuildingWallFamily : byte
    {
        Earth,
        Formal,
        TimberFence
    }

    public enum CountyBuildingGateFamily : byte
    {
        Domestic,
        Wide,
        Gatehouse
    }

    public enum CountyBuildingGroundTreatment : byte
    {
        DomesticEarth,
        MarketHardstand,
        WorkshopYard,
        LoadingApron,
        CivicCourt
    }

    public enum CountyBuildingModuleKind : byte
    {
        Hall,
        SideHouse,
        LongWarehouse,
        WorkshopShed,
        OpenShed,
        Gatehouse,
        MarketStall,
        MaterialStack,
        DomesticProp,
        CivicMarker,
        Tree
    }

    public enum CountyBuildingRoofShape : byte
    {
        None,
        Gable,
        LowGable,
        LongGable,
        Hip
    }

    /// <summary>
    /// One reusable, normalized module instruction. Coordinates are measured
    /// in PlanningCell presentation units (one unit is one formal 50 m Cell).
    /// The instruction is visual content only and never becomes a Facility.
    /// </summary>
    public sealed class CountyBuildingModuleTemplate
    {
        public CountyBuildingModuleTemplate(string moduleId,
            CountyBuildingModuleKind kind, bool mainBuilding, float offsetX,
            float offsetZ, float width, float depth, float height,
            CountyBuildingRoofShape roofShape = CountyBuildingRoofShape.None,
            int optionalModulo = 0, int optionalRemainder = 0)
        {
            if (string.IsNullOrWhiteSpace(moduleId))
                throw new ArgumentException("Module id is required.",
                    nameof(moduleId));
            if (width <= 0f || depth <= 0f || height <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (optionalModulo < 0 || optionalRemainder < 0 ||
                optionalModulo > 0 && optionalRemainder >= optionalModulo)
                throw new ArgumentOutOfRangeException(nameof(optionalModulo));
            ModuleId = moduleId;
            Kind = kind;
            MainBuilding = mainBuilding;
            OffsetX = offsetX;
            OffsetZ = offsetZ;
            Width = width;
            Depth = depth;
            Height = height;
            RoofShape = roofShape;
            OptionalModulo = optionalModulo;
            OptionalRemainder = optionalRemainder;
        }

        public string ModuleId { get; }
        public CountyBuildingModuleKind Kind { get; }
        public bool MainBuilding { get; }
        public float OffsetX { get; }
        public float OffsetZ { get; }
        public float Width { get; }
        public float Depth { get; }
        public float Height { get; }
        public CountyBuildingRoofShape RoofShape { get; }
        public int OptionalModulo { get; }
        public int OptionalRemainder { get; }

        public bool IsIncluded(ulong seed) => OptionalModulo == 0 ||
            (int)(seed % (ulong)OptionalModulo) == OptionalRemainder;
    }

    public sealed class CountyBuildingModulePlan
    {
        public CountyBuildingModulePlan(string profileId, ulong seed,
            int roofVariation, IReadOnlyList<CountyBuildingModuleTemplate>
                modules)
        {
            ProfileId = profileId;
            StableSeed = seed;
            RoofVariation = roofVariation;
            Modules = modules ?? Array.Empty<CountyBuildingModuleTemplate>();
            MainBuildingCount = Modules.Count(item => item.MainBuilding);
            SecondaryBuildingCount = Modules.Count(item =>
                !item.MainBuilding && HasBuildingMass(item.Kind));
            PropCount = Modules.Count(item => !HasBuildingMass(item.Kind) &&
                                              item.Kind !=
                                              CountyBuildingModuleKind.Tree);
            VegetationCount = Modules.Count(item => item.Kind ==
                CountyBuildingModuleKind.Tree);
            StableSignature = CountyBuildingPresentationStableHash.Text(
                profileId + ":" + seed + ":" + roofVariation + ":" +
                string.Join(",", Modules.Select(item => item.ModuleId)));
        }

        public string ProfileId { get; }
        public ulong StableSeed { get; }
        public int RoofVariation { get; }
        public IReadOnlyList<CountyBuildingModuleTemplate> Modules { get; }
        public int MainBuildingCount { get; }
        public int SecondaryBuildingCount { get; }
        public int PropCount { get; }
        public int VegetationCount { get; }
        public ulong StableSignature { get; }

        private static bool HasBuildingMass(CountyBuildingModuleKind kind) =>
            kind == CountyBuildingModuleKind.Hall ||
            kind == CountyBuildingModuleKind.SideHouse ||
            kind == CountyBuildingModuleKind.LongWarehouse ||
            kind == CountyBuildingModuleKind.WorkshopShed ||
            kind == CountyBuildingModuleKind.OpenShed ||
            kind == CountyBuildingModuleKind.Gatehouse;
    }

    /// <summary>
    /// Data-driven presentation content for a Facility compound. It defines
    /// modular massing, roof, foundation, enclosure, gate, yard and LOD
    /// language without changing the Facility, world schema or save data.
    /// </summary>
    public sealed class CountyBuildingPresentationProfile
    {
        public CountyBuildingPresentationProfile(string profileId,
            CountyGoldenBlockArchetype archetype,
            IReadOnlyList<string> facilityDefinitionIds,
            IReadOnlyList<string> categoryIds,
            CountyBuildingPresentationImportance importance,
            CountyBuildingRoofFamily roofFamily,
            IReadOnlyList<string> roofVariationSet,
            CountyBuildingFoundationFamily foundationFamily,
            CountyBuildingWallFamily wallFamily,
            CountyBuildingGateFamily gateFamily,
            CountyBuildingGroundTreatment groundTreatment,
            string propSetId, string vegetationStyleId, float density,
            float symmetry, string axisPreference, string farMode,
            string midMode, string nearMode, float scaleCalibration,
            string stableVariationRule,
            IReadOnlyList<CountyBuildingModuleTemplate> modules)
        {
            if (string.IsNullOrWhiteSpace(profileId) ||
                modules == null || modules.Count == 0 || density <= 0f ||
                scaleCalibration <= 0f)
                throw new ArgumentException(
                    "Invalid county building presentation profile.");
            ProfileId = profileId;
            Archetype = archetype;
            FacilityDefinitionIds = (facilityDefinitionIds ??
                Array.Empty<string>()).Where(item =>
                !string.IsNullOrWhiteSpace(item)).Distinct(
                StringComparer.Ordinal).ToArray();
            CategoryIds = (categoryIds ?? Array.Empty<string>()).Where(item =>
                !string.IsNullOrWhiteSpace(item)).Distinct(
                StringComparer.Ordinal).ToArray();
            Importance = importance;
            RoofFamily = roofFamily;
            RoofVariationSet = roofVariationSet ?? Array.Empty<string>();
            FoundationFamily = foundationFamily;
            WallFamily = wallFamily;
            GateFamily = gateFamily;
            GroundTreatment = groundTreatment;
            PropSetId = propSetId ?? string.Empty;
            VegetationStyleId = vegetationStyleId ?? string.Empty;
            Density = density;
            Symmetry = symmetry;
            AxisPreference = axisPreference ?? string.Empty;
            FarPresentationMode = farMode ?? string.Empty;
            MidPresentationMode = midMode ?? string.Empty;
            NearPresentationMode = nearMode ?? string.Empty;
            ScaleCalibration = scaleCalibration;
            StableVariationRule = stableVariationRule ?? string.Empty;
            Modules = modules.ToArray();
        }

        public string ProfileId { get; }
        public CountyGoldenBlockArchetype Archetype { get; }
        public IReadOnlyList<string> FacilityDefinitionIds { get; }
        public IReadOnlyList<string> CategoryIds { get; }
        public CountyBuildingPresentationImportance Importance { get; }
        public CountyBuildingRoofFamily RoofFamily { get; }
        public IReadOnlyList<string> RoofVariationSet { get; }
        public CountyBuildingFoundationFamily FoundationFamily { get; }
        public CountyBuildingWallFamily WallFamily { get; }
        public CountyBuildingGateFamily GateFamily { get; }
        public CountyBuildingGroundTreatment GroundTreatment { get; }
        public string PropSetId { get; }
        public string VegetationStyleId { get; }
        public float Density { get; }
        public float Symmetry { get; }
        public string AxisPreference { get; }
        public string FarPresentationMode { get; }
        public string MidPresentationMode { get; }
        public string NearPresentationMode { get; }
        public float ScaleCalibration { get; }
        public string StableVariationRule { get; }
        public IReadOnlyList<CountyBuildingModuleTemplate> Modules { get; }

        public CountyBuildingModulePlan Resolve(string stableSourceId,
            int variationSalt = 0)
        {
            var seed = CountyBuildingPresentationStableHash.Text(ProfileId +
                ":" + (stableSourceId ?? "context") + ":" + variationSalt);
            var modules = Modules.Where((item, index) => item.IsIncluded(
                seed >> (index % 17))).ToArray();
            var roofVariation = RoofVariationSet.Count == 0 ? 0 :
                (int)(seed % (ulong)RoofVariationSet.Count);
            return new CountyBuildingModulePlan(ProfileId, seed,
                roofVariation, modules);
        }
    }

    public sealed class CountyBuildingPresentationProfileCatalog
    {
        private static readonly CountyBuildingPresentationProfileCatalog
            HanLuoyangV2Value = new CountyBuildingPresentationProfileCatalog(
                BuildHanLuoyangV2());
        private readonly IReadOnlyDictionary<string,
            CountyBuildingPresentationProfile> _byDefinition;
        private readonly IReadOnlyDictionary<string,
            CountyBuildingPresentationProfile> _byCategory;
        private readonly IReadOnlyDictionary<CountyGoldenBlockArchetype,
            CountyBuildingPresentationProfile> _byArchetype;
        private readonly IReadOnlyDictionary<CountyFarAggregateKind,
            CountyBuildingPresentationProfile> _byFarAggregateKind;

        public CountyBuildingPresentationProfileCatalog(
            IReadOnlyList<CountyBuildingPresentationProfile> profiles)
        {
            Profiles = profiles ?? Array.Empty<
                CountyBuildingPresentationProfile>();
            if (Profiles.Count != Enum.GetValues(
                    typeof(CountyGoldenBlockArchetype)).Length ||
                Profiles.Select(item => item.ProfileId).Distinct(
                    StringComparer.Ordinal).Count() != Profiles.Count ||
                Profiles.Select(item => item.Archetype).Distinct().Count() !=
                Profiles.Count)
                throw new InvalidOperationException(
                    "Golden Block V2 requires one distinct profile per archetype.");
            _byDefinition = Profiles.SelectMany(profile =>
                    profile.FacilityDefinitionIds.Select(id => new
                        { id, profile }))
                .ToDictionary(item => item.id, item => item.profile,
                    StringComparer.Ordinal);
            _byCategory = Profiles.SelectMany(profile => profile.CategoryIds
                    .Select(id => new { id, profile }))
                .GroupBy(item => item.id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => group.First().profile, StringComparer.Ordinal);
            _byArchetype = Profiles.ToDictionary(item => item.Archetype);
            _byFarAggregateKind = new Dictionary<CountyFarAggregateKind,
                CountyBuildingPresentationProfile>
            {
                [CountyFarAggregateKind.Residential] =
                    _byArchetype[CountyGoldenBlockArchetype
                        .ResidenceCourtyard],
                [CountyFarAggregateKind.Commercial] =
                    _byArchetype[CountyGoldenBlockArchetype.MarketFrontage],
                [CountyFarAggregateKind.Workshop] =
                    _byArchetype[CountyGoldenBlockArchetype.WorkshopYard],
                [CountyFarAggregateKind.Storage] =
                    _byArchetype[CountyGoldenBlockArchetype
                        .WarehouseCompound],
                [CountyFarAggregateKind.Civic] =
                    _byArchetype[CountyGoldenBlockArchetype.CivicCourtyard],
                [CountyFarAggregateKind.Military] =
                    _byArchetype[CountyGoldenBlockArchetype.CivicCourtyard],
                [CountyFarAggregateKind.Mixed] =
                    _byArchetype[CountyGoldenBlockArchetype
                        .ResidenceCourtyard]
            };
        }

        public static CountyBuildingPresentationProfileCatalog HanLuoyangV2
            => HanLuoyangV2Value;
        public IReadOnlyList<CountyBuildingPresentationProfile> Profiles
            { get; }

        public CountyBuildingPresentationProfile Resolve(
            string facilityDefinitionId, string categoryId = null)
        {
            if (!string.IsNullOrWhiteSpace(facilityDefinitionId) &&
                _byDefinition.TryGetValue(facilityDefinitionId,
                    out var exact)) return exact;
            if (!string.IsNullOrWhiteSpace(categoryId) &&
                _byCategory.TryGetValue(categoryId, out var category))
                return category;
            return _byArchetype[
                CountyGoldenBlockArchetype.ResidenceCourtyard];
        }

        public CountyBuildingPresentationProfile Resolve(
            CountyGoldenBlockArchetype archetype) => _byArchetype[archetype];

        public CountyBuildingPresentationProfile Resolve(
            CountyFarAggregateKind aggregateKind) =>
            _byFarAggregateKind.TryGetValue(aggregateKind, out var profile)
                ? profile
                : _byArchetype[CountyGoldenBlockArchetype
                    .ResidenceCourtyard];

        private static IReadOnlyList<CountyBuildingPresentationProfile>
            BuildHanLuoyangV2() => new[]
            {
                Profile("presentation.building.han.residence.v2",
                    CountyGoldenBlockArchetype.ResidenceCourtyard,
                    new[]
                    {
                        "facility.residential.urban_quarter",
                        "facility.residential.rural_hamlet",
                        "facility.residential.family_manor",
                        "facility.historical.urban_ward"
                    },
                    new[] { "residential" },
                    CountyBuildingPresentationImportance.Ordinary,
                    CountyBuildingRoofFamily.DomesticGable,
                    CountyBuildingFoundationFamily.Earth,
                    CountyBuildingWallFamily.Earth,
                    CountyBuildingGateFamily.Domestic,
                    CountyBuildingGroundTreatment.DomesticEarth,
                    "props.domestic.life.v2", "vegetation.courtyard.tree.v2",
                    0.68f, 0.58f, "road-facing",
                    Modules(
                        M("main-hall", CountyBuildingModuleKind.Hall, true,
                            0f, 0.38f, 0.92f, 0.34f, 0.46f,
                            CountyBuildingRoofShape.Gable),
                        M("west-side-house",
                            CountyBuildingModuleKind.SideHouse, false,
                            -0.46f, -0.06f, 0.28f, 0.62f, 0.32f,
                            CountyBuildingRoofShape.Gable),
                        M("east-side-house",
                            CountyBuildingModuleKind.SideHouse, false,
                            0.46f, -0.06f, 0.28f, 0.62f, 0.32f,
                            CountyBuildingRoofShape.Gable, 2, 0),
                        M("domestic-stack",
                            CountyBuildingModuleKind.DomesticProp, false,
                            0.24f, -0.30f, 0.14f, 0.13f, 0.15f),
                        M("courtyard-tree", CountyBuildingModuleKind.Tree,
                            false, -0.25f, -0.28f, 0.24f, 0.24f, 0.34f))),
                Profile("presentation.building.han.market.v2",
                    CountyGoldenBlockArchetype.MarketFrontage,
                    new[]
                    {
                        "facility.commercial.market",
                        "facility.commercial.shop_cluster",
                        "facility.service.inn",
                        "facility.service.caravan_yard",
                        "facility.service.post_station"
                    },
                    new[] { "commercial" },
                    CountyBuildingPresentationImportance.Significant,
                    CountyBuildingRoofFamily.MarketCanopy,
                    CountyBuildingFoundationFamily.Formal,
                    CountyBuildingWallFamily.TimberFence,
                    CountyBuildingGateFamily.Wide,
                    CountyBuildingGroundTreatment.MarketHardstand,
                    "props.market.goods.v2", "vegetation.market.sparse.v2",
                    0.52f, 0.25f, "road-frontage",
                    Modules(
                        M("market-hall", CountyBuildingModuleKind.Hall, true,
                            0f, 0.48f, 1.10f, 0.28f, 0.42f,
                            CountyBuildingRoofShape.Gable),
                        M("west-open-shed", CountyBuildingModuleKind.OpenShed,
                            false, -0.48f, -0.04f, 0.30f, 0.42f, 0.25f,
                            CountyBuildingRoofShape.LowGable),
                        M("east-open-shed", CountyBuildingModuleKind.OpenShed,
                            false, 0.48f, -0.04f, 0.30f, 0.42f, 0.25f,
                            CountyBuildingRoofShape.LowGable),
                        M("stall-a", CountyBuildingModuleKind.MarketStall,
                            false, -0.34f, -0.36f, 0.22f, 0.18f, 0.20f),
                        M("stall-b", CountyBuildingModuleKind.MarketStall,
                            false, 0f, -0.36f, 0.22f, 0.18f, 0.20f),
                        M("stall-c", CountyBuildingModuleKind.MarketStall,
                            false, 0.34f, -0.36f, 0.22f, 0.18f, 0.20f))),
                Profile("presentation.building.han.workshop.v2",
                    CountyGoldenBlockArchetype.WorkshopYard,
                    new[] { "facility.industry.workshop" },
                    new[] { "industry", "resource" },
                    CountyBuildingPresentationImportance.Significant,
                    CountyBuildingRoofFamily.WorkshopLowGable,
                    CountyBuildingFoundationFamily.Earth,
                    CountyBuildingWallFamily.TimberFence,
                    CountyBuildingGateFamily.Wide,
                    CountyBuildingGroundTreatment.WorkshopYard,
                    "props.workshop.materials.v2",
                    "vegetation.workshop.none.v2", 0.62f, 0.12f,
                    "work-yard",
                    Modules(
                        M("workshop-main",
                            CountyBuildingModuleKind.WorkshopShed, true,
                            -0.12f, 0.40f, 0.94f, 0.36f, 0.40f,
                            CountyBuildingRoofShape.LowGable),
                        M("workshop-long-shed",
                            CountyBuildingModuleKind.OpenShed, false,
                            0.48f, -0.06f, 0.28f, 0.72f, 0.28f,
                            CountyBuildingRoofShape.LowGable),
                        M("material-a",
                            CountyBuildingModuleKind.MaterialStack, false,
                            -0.38f, -0.28f, 0.18f, 0.16f, 0.14f),
                        M("material-b",
                            CountyBuildingModuleKind.MaterialStack, false,
                            -0.12f, -0.30f, 0.16f, 0.13f, 0.18f),
                        M("material-c",
                            CountyBuildingModuleKind.MaterialStack, false,
                            0.12f, -0.30f, 0.13f, 0.13f, 0.20f))),
                Profile("presentation.building.han.granary.v2",
                    CountyGoldenBlockArchetype.WarehouseCompound,
                    new[]
                    {
                        "facility.storage.warehouse",
                        "facility.storage.granary",
                        "facility.commercial.warehouse",
                        "facility.public.granary"
                    },
                    new[] { "storage" },
                    CountyBuildingPresentationImportance.Major,
                    CountyBuildingRoofFamily.GranaryLongGable,
                    CountyBuildingFoundationFamily.Formal,
                    CountyBuildingWallFamily.Formal,
                    CountyBuildingGateFamily.Wide,
                    CountyBuildingGroundTreatment.LoadingApron,
                    "props.granary.loading.v2",
                    "vegetation.granary.sparse.v2", 0.76f, 0.72f,
                    "parallel-to-road",
                    Modules(
                        M("granary-west",
                            CountyBuildingModuleKind.LongWarehouse, true,
                            -0.40f, 0.08f, 0.34f, 1.12f, 0.44f,
                            CountyBuildingRoofShape.LongGable),
                        M("granary-centre",
                            CountyBuildingModuleKind.LongWarehouse, true,
                            0f, 0.08f, 0.34f, 1.12f, 0.44f,
                            CountyBuildingRoofShape.LongGable, 2, 0),
                        M("granary-east",
                            CountyBuildingModuleKind.LongWarehouse, true,
                            0.40f, 0.08f, 0.34f, 1.12f, 0.44f,
                            CountyBuildingRoofShape.LongGable),
                        M("loading-stack-a",
                            CountyBuildingModuleKind.MaterialStack, false,
                            -0.22f, -0.48f, 0.22f, 0.18f, 0.18f),
                        M("loading-stack-b",
                            CountyBuildingModuleKind.MaterialStack, false,
                            0.16f, -0.48f, 0.22f, 0.18f, 0.18f))),
                Profile("presentation.building.han.government.v2",
                    CountyGoldenBlockArchetype.CivicCourtyard,
                    new[] { "facility.government.local_office",
                        "facility.public.county_office",
                        "facility.government.court_hall",
                        "facility.service.school",
                        "facility.service.clinic" },
                    new[] { "government", "public", "ritual", "education",
                        "military" },
                    CountyBuildingPresentationImportance.Major,
                    CountyBuildingRoofFamily.CivicRaisedHip,
                    CountyBuildingFoundationFamily.CivicTerrace,
                    CountyBuildingWallFamily.Formal,
                    CountyBuildingGateFamily.Gatehouse,
                    CountyBuildingGroundTreatment.CivicCourt,
                    "props.civic.formal.v2", "vegetation.civic.paired.v2",
                    0.82f, 1f, "formal-axis",
                    Modules(
                        M("civic-main-hall", CountyBuildingModuleKind.Hall,
                            true, 0f, 0.44f, 1.16f, 0.48f, 0.62f,
                            CountyBuildingRoofShape.Hip),
                        M("civic-west-wing",
                            CountyBuildingModuleKind.SideHouse, false,
                            -0.48f, -0.02f, 0.28f, 0.58f, 0.36f,
                            CountyBuildingRoofShape.Gable),
                        M("civic-east-wing",
                            CountyBuildingModuleKind.SideHouse, false,
                            0.48f, -0.02f, 0.28f, 0.58f, 0.36f,
                            CountyBuildingRoofShape.Gable),
                        M("civic-gatehouse",
                            CountyBuildingModuleKind.Gatehouse, false,
                            0f, -0.58f, 0.50f, 0.20f, 0.40f,
                            CountyBuildingRoofShape.Hip),
                        M("civic-marker", CountyBuildingModuleKind.CivicMarker,
                            false, 0f, -0.20f, 0.10f, 0.10f, 0.28f),
                        M("civic-tree-west", CountyBuildingModuleKind.Tree,
                            false, -0.30f, -0.26f, 0.24f, 0.24f, 0.34f),
                        M("civic-tree-east", CountyBuildingModuleKind.Tree,
                            false, 0.30f, -0.26f, 0.24f, 0.24f, 0.34f)))
            };

        private static CountyBuildingPresentationProfile Profile(
            string id, CountyGoldenBlockArchetype archetype,
            IReadOnlyList<string> definitions,
            IReadOnlyList<string> categories,
            CountyBuildingPresentationImportance importance,
            CountyBuildingRoofFamily roof,
            CountyBuildingFoundationFamily foundation,
            CountyBuildingWallFamily wall, CountyBuildingGateFamily gate,
            CountyBuildingGroundTreatment ground, string props,
            string vegetation, float density, float symmetry, string axis,
            IReadOnlyList<CountyBuildingModuleTemplate> modules) =>
            new CountyBuildingPresentationProfile(id, archetype, definitions,
                categories, importance, roof,
                new[] { "warm-tile", "dark-tile", "weathered-tile" },
                foundation, wall, gate, ground, props, vegetation, density,
                symmetry, axis, "aggregate-silhouette",
                "compound-readable", "compound-modules", 1f,
                "fnv1a64(profile:source:salt)", modules);

        private static IReadOnlyList<CountyBuildingModuleTemplate> Modules(
            params CountyBuildingModuleTemplate[] modules) => modules;

        private static CountyBuildingModuleTemplate M(string id,
            CountyBuildingModuleKind kind, bool main, float x, float z,
            float width, float depth, float height,
            CountyBuildingRoofShape roof = CountyBuildingRoofShape.None,
            int optionalModulo = 0, int optionalRemainder = 0) =>
            new CountyBuildingModuleTemplate(id, kind, main, x, z, width,
                depth, height, roof, optionalModulo, optionalRemainder);
    }

    internal static class CountyBuildingPresentationStableHash
    {
        public static ulong Text(string value)
        {
            unchecked
            {
                var hash = 14695981039346656037UL;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 1099511628211UL;
                }
                return hash;
            }
        }
    }
}
