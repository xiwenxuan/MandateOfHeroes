using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void Luoyang50mLayoutPackage_FreezesCompleteRuntimeAuthority()
        {
            var source = new Luoyang50mCountyLayoutPackageSource(
                Luoyang50mWorldMapRoot);
            var package = source.Package;

            Assert.That(package.SchemaId,
                Is.EqualTo(Luoyang50mCountyLayoutIds.SchemaId));
            Assert.That(package.PackageId,
                Is.EqualTo(Luoyang50mCountyLayoutIds.PackageId));
            Assert.That(package.IsRuntimeAuthoritative, Is.True);
            Assert.That(package.IsHistoricallyExact, Is.False);
            Assert.That(package.Facilities.Count, Is.EqualTo(2084));
            Assert.That(package.RoadNodes.Count, Is.EqualTo(359));
            Assert.That(package.RoadEdges.Count, Is.EqualTo(334));
            Assert.That(package.CanalNodes.Count, Is.EqualTo(19));
            Assert.That(package.CanalEdges.Count, Is.EqualTo(17));
            Assert.That(package.Fortifications.Count, Is.EqualTo(144));
            Assert.That(package.Portals.Count, Is.EqualTo(4));
            Assert.That(package.DistrictAreas.Count, Is.EqualTo(6));
            Assert.That(package.UrbanAreaCandidate.FacilityCount,
                Is.EqualTo(2084));
            Assert.That(source.PackageFileSha256.Length, Is.EqualTo(64));
        }

        [Test]
        public void Luoyang50mLayoutPackage_PreservesFormalFacilityIdentityAndSourceAnchors()
        {
            var package = new Luoyang50mCountyLayoutPackageSource(
                Luoyang50mWorldMapRoot).Package;
            var coverage = new LuoyangFacilityModelCoverageSource(
                Luoyang50mWorldMapRoot);
            var formal = new LuoyangBuildingPerformancePlanSource(
                Luoyang50mWorldMapRoot, coverage.Bindings,
                coverage.CombinedCatalog).Plan.Facilities.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);

            Assert.That(package.Facilities.All(item =>
                formal.TryGetValue(item.FacilityId, out var source) &&
                item.DefinitionId == source.FacilityDefinitionId &&
                item.SourceCellId64 == source.CellId64 &&
                item.SourceRow == source.GridRow &&
                item.SourceColumn == source.GridColumn &&
                item.SourceSpatialPrecisionId == source.SpatialPrecisionId &&
                item.HistoricalConfidenceId ==
                source.HistoricalConfidenceId), Is.True);
            Assert.That(package.Facilities.Count(item =>
                item.PreservesSourceStrategicTile), Is.EqualTo(1));
        }

        [Test]
        public void Luoyang50mPrototype_ConsumesFrozenLayoutInsteadOfRescalingAtRuntime()
        {
            var source = new Luoyang50mCountySpatialPrototypeSource(
                Luoyang50mWorldMapRoot);
            var package = source.LayoutPackage;

            Assert.That(source.Prototype.LayoutPackageId,
                Is.EqualTo(package.PackageId));
            Assert.That(source.Prototype.DeclaredLayoutFingerprint,
                Is.EqualTo(package.DeclaredLayoutFingerprint));
            Assert.That(source.Prototype.RuntimeLayoutHash,
                Is.EqualTo(package.RuntimeDeterministicHash));
            Assert.That(source.Prototype.RoadNetworkEdgeCount,
                Is.EqualTo(334));
            Assert.That(source.Prototype.CanalNetworkEdgeCount,
                Is.EqualTo(17));
            Assert.That(source.Prototype.DistrictAreaCount, Is.EqualTo(6));
            Assert.That(source.Prototype.Facilities.All(candidate =>
            {
                var layout = package.FacilitiesById[candidate.FacilityId];
                source.Prototype.Partition.TryToLocal(candidate.CandidateCell,
                    out var row, out var column);
                return row == layout.LocalRow &&
                       column == layout.LocalColumn &&
                       candidate.WidthCentimetres == layout.WidthCentimetres &&
                       candidate.DepthCentimetres == layout.DepthCentimetres &&
                       candidate.DistrictId == layout.DistrictId;
            }), Is.True);
        }

        [Test]
        public void Luoyang50mLayoutPackage_NetworkAndAreaGeometryIsDeterministic()
        {
            var first = new Luoyang50mCountyLayoutPackageSource(
                Luoyang50mWorldMapRoot);
            var second = new Luoyang50mCountyLayoutPackageSource(
                Luoyang50mWorldMapRoot);

            Assert.That(second.PackageFileSha256,
                Is.EqualTo(first.PackageFileSha256));
            Assert.That(second.Package.RuntimeDeterministicHash,
                Is.EqualTo(first.Package.RuntimeDeterministicHash));
            Assert.That(first.Package.RoadEdges.All(item =>
                item.FromLocalRow == item.ToLocalRow ||
                item.FromLocalColumn == item.ToLocalColumn), Is.True);
            Assert.That(first.Package.CanalEdges.All(item =>
                item.FromLocalRow == item.ToLocalRow ||
                item.FromLocalColumn == item.ToLocalColumn), Is.True);
            Assert.That(first.Package.DistrictAreas.All(item =>
                item.HullCells.Count >= 3 &&
                item.StatusId == Luoyang50mCountyLayoutIds.StatusId), Is.True);
            Assert.That(first.Package.Portals.Select(item => item.SideId)
                .OrderBy(item => item).ToArray(), Is.EqualTo(new[]
            {
                "east", "north", "south", "west"
            }));
        }
    }
}
