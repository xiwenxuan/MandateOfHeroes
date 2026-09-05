using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void WorldAdministrative_CatalogNamesAndCellHierarchyLoad()
        {
            using (var source = new HanAdministrativeGeographySource(
                       WorldPackageRoot(), 184))
            {
                Assert.That(source.RegionCatalog.Regions.Count(item =>
                    item.Level == AdministrativeRegionLevel.Province),
                    Is.EqualTo(13));
                Assert.That(source.RegionCatalog.Regions.Count(item =>
                    item.Level == AdministrativeRegionLevel.
                        CommanderyEquivalent), Is.EqualTo(105));
                Assert.That(source.RegionCatalog.Regions.Count(item =>
                    item.Level == AdministrativeRegionLevel.County),
                    Is.EqualTo(1182));

                var luoyang = source.RegionCatalog.Get(
                    "admin.han140.sili.henan.luoyang");
                Assert.That(luoyang.DisplayName, Is.EqualTo("雒阳"));
                Assert.That(luoyang.GeometryStatus,
                    Is.EqualTo(AdministrativeGeometryStatus.Approximate));
                Assert.That(luoyang.Provisional, Is.True);
                source.RegionCatalog.ResolveCountyHierarchy(luoyang.Id,
                    out var county, out var commandery, out var province);
                Assert.That(county.Id, Is.EqualTo(luoyang.Id));
                Assert.That(commandery.Id,
                    Is.EqualTo("admin.han140.sili.henan"));
                Assert.That(province.Id,
                    Is.EqualTo("admin.han140.sili"));

                var assignment = source.ReadAssignment(1241, 2043);
                Assert.That(assignment.IsMapped, Is.True);
                source.RegionCatalog.ResolveCountyHierarchy(
                    assignment.CountyRegionId, out var mappedCounty,
                    out var mappedCommandery, out var mappedProvince);
                Assert.That(mappedCounty.Id,
                    Is.EqualTo(assignment.CountyRegionId));
                Assert.That(mappedCommandery.Id,
                    Is.EqualTo(assignment.CommanderyRegionId));
                Assert.That(mappedProvince.Id,
                    Is.EqualTo(assignment.ProvinceRegionId));
            }
        }

        [Test]
        public void WorldAdministrative_BoundaryBuilderIsDeterministicAndPreservesSemanticFlags()
        {
            var source = new SyntheticAdministrativeSource();
            var first = AdministrativeBoundaryTopologyBuilder.Build(source);
            var second = AdministrativeBoundaryTopologyBuilder.Build(source);

            Assert.That(first.DeterministicSummary,
                Is.EqualTo(second.DeterministicSummary));
            Assert.That(first.MappedCellCount, Is.EqualTo(6));
            Assert.That(first.SegmentCount, Is.GreaterThan(0));
            var segments = first.Chunks.SelectMany(item => item.Segments)
                .ToArray();
            Assert.That(segments.Any(item =>
                (item.Levels & AdministrativeBoundaryLevels.Province) != 0 &&
                (item.Levels & AdministrativeBoundaryLevels.
                    CommanderyEquivalent) != 0 &&
                (item.Levels & AdministrativeBoundaryLevels.County) != 0),
                Is.True);
            Assert.That(segments.All(item =>
                item.Direction == GlobalCellEdgeDirection.East ||
                item.Direction == GlobalCellEdgeDirection.South), Is.True);
            Assert.That(first.GetRegion("county.a").CellCount,
                Is.EqualTo(3));
        }

        [Test]
        public void WorldAdministrative_HistoricalNamesResolveAtStartAndThenFreeze()
        {
            const string id = "county.synthetic.capital";
            var fallbacks = new[]
            {
                new KeyValuePair<string, string>(id, "默认名")
            };
            var periods = new[]
            {
                new HistoricalNamePeriod(id, "早期名", 100, 179),
                new HistoricalNamePeriod(id, "晚期名", 180, 220)
            };
            var early = new FrozenWorldDisplayNameCatalog(150, fallbacks,
                periods);
            var late = new FrozenWorldDisplayNameCatalog(184, fallbacks,
                periods);
            var fallback = new FrozenWorldDisplayNameCatalog(300, fallbacks,
                periods);

            Assert.That(early.Resolve(id), Is.EqualTo("早期名"));
            Assert.That(late.Resolve(id), Is.EqualTo("晚期名"));
            Assert.That(fallback.Resolve(id), Is.EqualTo("默认名"));
            Assert.That(late.Resolve(id), Is.EqualTo("晚期名"),
                "同一局显示名不得随世界年份推进重新解析");
        }

        [Test]
        public void WorldAdministrative_CountyPlanningDoesNotMutateWorldSnapshot()
        {
            var world = CreateM26ProductWorld();
            var before = WorldSnapshotSerializer.Serialize(world);
            var source = new SyntheticAdministrativeSource();
            var topology = AdministrativeBoundaryTopologyBuilder.Build(source);
            var view = new AdministrativeMapViewState();
            var county = topology.GetRegion("county.a");

            view.Select(county.Region);
            view.EnterCountyPlanning(county);
            Assert.That(view.ViewMode,
                Is.EqualTo(AdministrativeMapViewMode.CountyPlanning));
            Assert.That(view.PlanningCountyId, Is.EqualTo("county.a"));
            Assert.That(view.LabelLevel, Is.EqualTo(
                AdministrativeMapLabelLevel.CurrentCountyAndNeighbors));
            view.ExitCountyPlanning();

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void WorldAdministrative_FormalTopologyCoversDeclaredRegions()
        {
            using (var source = new HanAdministrativeGeographySource(
                       WorldPackageRoot(), 184))
            {
                var stopwatch = Stopwatch.StartNew();
                var topology = AdministrativeBoundaryTopologyBuilder.Build(
                    source);
                stopwatch.Stop();
                Assert.That(topology.Rows,
                    Is.EqualTo(GlobalSpatialFoundationV1.Rows));
                Assert.That(topology.Columns,
                    Is.EqualTo(GlobalSpatialFoundationV1.Columns));
                // The formal catalog resolves all 1300 regions.  The authored
                // V1 raster currently places 1273 of them; the remaining 27
                // are reported as unresolved geometry instead of fabricated.
                Assert.That(topology.RegionSummaries.Count, Is.EqualTo(1273));
                Assert.That(source.RegionCatalog.Count -
                    topology.RegionSummaries.Count, Is.EqualTo(27));
                Assert.That(topology.GetRegion(
                    "admin.han140.sili.henan.luoyang").CellCount,
                    Is.GreaterThan(0));
                Assert.That(topology.ProvinceBoundaryCount,
                    Is.GreaterThan(0));
                Assert.That(topology.CommanderyBoundaryCount,
                    Is.GreaterThan(topology.ProvinceBoundaryCount));
                Assert.That(topology.CountyBoundaryCount,
                    Is.GreaterThan(topology.CommanderyBoundaryCount));
                Assert.That(topology.DeterministicSummary.Length,
                    Is.EqualTo(64));
                Console.WriteLine(
                    "ADMIN_TOPOLOGY mapped=" + topology.MappedCellCount +
                    " mapped_regions=" + topology.RegionSummaries.Count +
                    " unresolved_regions=" + (source.RegionCatalog.Count -
                        topology.RegionSummaries.Count) +
                    " segments=" + topology.SegmentCount +
                    " province=" + topology.ProvinceBoundaryCount +
                    " commandery=" + topology.CommanderyBoundaryCount +
                    " county=" + topology.CountyBoundaryCount +
                    " chunks=" + topology.Chunks.Count +
                    " hash=" + topology.DeterministicSummary +
                    " milliseconds=" + stopwatch.ElapsedMilliseconds);
            }
        }

        private sealed class SyntheticAdministrativeSource :
            ICellAdministrativeAssignmentSource
        {
            private readonly CellAdministrativeAssignment[,] _cells;

            public SyntheticAdministrativeSource()
            {
                var regions = new[]
                {
                    Region("province.one", AdministrativeRegionLevel.Province,
                        string.Empty),
                    Region("province.two", AdministrativeRegionLevel.Province,
                        string.Empty),
                    Region("commandery.one", AdministrativeRegionLevel.
                        CommanderyEquivalent, "province.one"),
                    Region("commandery.two", AdministrativeRegionLevel.
                        CommanderyEquivalent, "province.two"),
                    Region("county.a", AdministrativeRegionLevel.County,
                        "commandery.one"),
                    Region("county.b", AdministrativeRegionLevel.County,
                        "commandery.one"),
                    Region("county.c", AdministrativeRegionLevel.County,
                        "commandery.two")
                };
                RegionCatalog = new AdministrativeRegionCatalog(regions);
                var a = Assignment(0, 0, 0, "province.one",
                    "commandery.one", "county.a");
                var b = Assignment(0, 0, 1, "province.one",
                    "commandery.one", "county.b");
                var c = Assignment(1, 1, 2, "province.two",
                    "commandery.two", "county.c");
                _cells = new[,]
                {
                    { a, a, b },
                    { a, c, c }
                };
            }

            public int Rows => 2;
            public int Columns => 3;
            public int ChunkSize => 2;
            public string RevisionId => "synthetic.administrative.v1";
            public AdministrativeRegionCatalog RegionCatalog { get; }

            public CellAdministrativeAssignment ReadAssignment(int row,
                int column) => _cells[row, column];

            private static AdministrativeRegionDefinition Region(string id,
                AdministrativeRegionLevel level, string parent) =>
                new AdministrativeRegionDefinition(id, level,
                    level.ToString(), parent, "geo." + id, id,
                    AdministrativeGeometryStatus.Approximate,
                    "synthetic", "test", true);

            private static CellAdministrativeAssignment Assignment(
                ushort province, ushort commandery, ushort county,
                string provinceId, string commanderyId, string countyId) =>
                new CellAdministrativeAssignment(province, commandery, county,
                    ushort.MaxValue, provinceId, commanderyId, countyId);
        }
    }
}
