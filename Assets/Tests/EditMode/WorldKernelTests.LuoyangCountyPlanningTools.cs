using System;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        private sealed class PlanningContext
        {
            public Luoyang50mCountySpatialPrototypeSource Source;
            public FacilityPlacementProfileCatalog Profiles;
            public FacilityPlacementValidator Validator;
        }

        [Test]
        public void LuoyangCountyPlanningProfiles_ReuseSixExistingContracts()
        {
            var context = CreatePlanningContext();

            Assert.That(context.Profiles.Profiles.Count, Is.EqualTo(6));
            Assert.That(context.Profiles.Profiles.Select(value =>
                value.FacilityDefinitionId), Is.EquivalentTo(new[]
            {
                "facility.residential.urban_quarter",
                "facility.storage.warehouse",
                "facility.industry.workshop",
                "facility.government.local_office",
                "facility.military.beacon",
                "facility.commercial.market"
            }));
            Assert.That(context.Profiles.Profiles.All(value =>
                value.AllowedRotationQuarterTurns.SequenceEqual(
                    new[] { 0, 1, 2, 3 })), Is.True);
            Assert.That(WorldState.CurrentSchemaVersion, Is.EqualTo(79));
        }

        [Test]
        public void LuoyangCountyPlanningFootprints_UsePhysicalSizeAndRotate()
        {
            var context = CreatePlanningContext();
            var large = context.Profiles.ProfilesByDefinitionId[
                "facility.commercial.market"];
            var first = context.Validator.CreateFootprint(large, 160, 320,
                0);
            var rotated = context.Validator.CreateFootprint(large, 160, 320,
                1);

            Assert.That(first.WidthMetres, Is.EqualTo(110d));
            Assert.That(first.LengthMetres, Is.EqualTo(80d));
            Assert.That(rotated.WidthMetres, Is.EqualTo(80d));
            Assert.That(rotated.LengthMetres, Is.EqualTo(110d));
            Assert.That(first.Entrances.Single(value => value.Primary)
                .OutwardDirection, Is.EqualTo(PlanningCellDirection.South));
            Assert.That(rotated.Entrances.Single(value => value.Primary)
                .OutwardDirection, Is.EqualTo(PlanningCellDirection.West));
        }

        [Test]
        public void LuoyangCountyPlanningValidator_RejectsOutsideCounty()
        {
            var context = CreatePlanningContext();
            var residence = Residence(context);

            var result = context.Validator.Validate(residence, -1, -1, 0);

            Assert.That(result.State,
                Is.EqualTo(PlacementValidationState.Invalid));
            Assert.That(result.BlockingReasons.Select(value => value.Code),
                Does.Contain(PlacementReasonIds.OutsideCounty));
        }

        [Test]
        public void LuoyangCountyPlanningValidator_FindsRealRoadConnection()
        {
            var context = CreatePlanningContext();
            var residence = Residence(context);
            var placement = FindConnected(context, residence);

            var result = context.Validator.Validate(residence,
                placement.Row, placement.Column, placement.Rotation);

            Assert.That(result.State, Is.EqualTo(
                PlacementValidationState.Valid));
            Assert.That(result.RoadAccessResult.Status, Is.EqualTo(
                FacilityRoadAccessStatus.Connected));
            Assert.That(result.RoadAccessResult.RoadClassId, Is.EqualTo(
                LuoyangCountyPlanningIds.RoadClassGeneral));
            Assert.That(context.Validator.LastFacilityCandidateCount,
                Is.LessThan(2084));
        }

        [Test]
        public void LuoyangCountyPlanningValidator_RejectsRealFacilityWaterAndWall()
        {
            var context = CreatePlanningContext();
            var residence = Residence(context);
            var projection = new DualScaleCoordinateProjection();
            var existing = context.Source.Prototype.Partition
                .FacilityPlacements.Values.OrderBy(value => value.FacilityId,
                    StringComparer.Ordinal).First();
            var global = projection.ToPlanningCell(existing.Center);
            context.Source.Prototype.Partition.TryToLocal(global,
                out var facilityRow, out var facilityColumn);
            var facilityResult = context.Validator.Validate(residence,
                facilityRow, facilityColumn, 0);
            var water = FindCell(context, (row, column) => context.Source
                .Prototype.Partition.WaterState(row, column) > 0);
            var waterResult = context.Validator.Validate(residence,
                water.Row, water.Column, 0);
            var wall = context.Source.Prototype.Partition.Fortifications
                .Values.OrderBy(value => value.Id,
                    StringComparer.Ordinal).First();
            context.Source.Prototype.Partition.TryToLocal(wall.Edge.First,
                out var wallRow, out var wallColumn);
            var warehouse = context.Profiles.ProfilesByDefinitionId[
                "facility.storage.warehouse"];
            var wallResult = context.Validator.Validate(warehouse, wallRow,
                wallColumn, 0);

            Assert.That(facilityResult.BlockingReasons.Select(value =>
                    value.Code), Does.Contain(
                PlacementReasonIds.ExistingFacilityCollision));
            Assert.That(waterResult.BlockingReasons.Select(value =>
                    value.Code), Does.Contain(PlacementReasonIds.WaterOverlap));
            Assert.That(wallResult.BlockingReasons.Select(value =>
                    value.Code), Does.Contain(
                PlacementReasonIds.FortificationOverlap));
        }

        [Test]
        public void LuoyangCountyPlanningValidator_ReportsStableRoadFailure()
        {
            var context = CreatePlanningContext();
            var residence = Residence(context);
            PlacementValidationResult failure = null;
            for (var row = 8; row < 312 && failure == null; row += 8)
            for (var column = 8; column < 632; column += 8)
            {
                var result = context.Validator.Validate(residence, row,
                    column, 0);
                if (result.BlockingReasons.Any(value => value.Code ==
                        PlacementReasonIds.RoadNoRoad || value.Code ==
                        PlacementReasonIds.RoadTooFar || value.Code ==
                        PlacementReasonIds.RoadWrongSide))
                    failure = result;
            }

            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.RoadAccessResult.Status,
                Is.EqualTo(FacilityRoadAccessStatus.NoRoad)
                    .Or.EqualTo(FacilityRoadAccessStatus.TooFar)
                    .Or.EqualTo(FacilityRoadAccessStatus.WrongSide));
            Assert.That(failure.BlockingReasons, Is.Ordered.By("Priority"));
        }

        [Test]
        public void LuoyangCountyPlanningDrafts_CollideUndoRedoAndStayDeterministic()
        {
            var context = CreatePlanningContext();
            var residence = Residence(context);
            var placement = FindConnected(context, residence);
            var session = new CountyPlanningSession(context.Validator.CountyId);
            var footprint = context.Validator.CreateFootprint(residence,
                placement.Row, placement.Column, placement.Rotation);
            var valid = context.Validator.Validate(footprint, session);
            var first = session.CreateDraft(residence, footprint, valid);
            var hash = session.ComputeDeterministicHash();
            var collision = context.Validator.Validate(footprint, session);

            Assert.That(first.ProvenanceId, Is.EqualTo(
                LuoyangCountyPlanningIds.DraftProvenanceId));
            Assert.That(collision.BlockingReasons.Select(value => value.Code),
                Does.Contain(PlacementReasonIds.DraftCollision));
            Assert.That(session.Undo(), Is.SameAs(first));
            Assert.That(session.Drafts, Is.Empty);
            Assert.That(session.Redo(), Is.SameAs(first));
            Assert.That(session.ComputeDeterministicHash(), Is.EqualTo(hash));
        }

        [Test]
        public void LuoyangCountyPlanningDrafts_DoNotMutateFormalWorldOrSpatialFacts()
        {
            var context = CreatePlanningContext();
            var residence = Residence(context);
            var placement = FindConnected(context, residence);
            var world = WorldState.Create(184001UL);
            var absoluteDay = world.AbsoluteDay;
            var facilityCount = world.Facilities.Count;
            var peopleCount = world.People.Count;
            var partitionHash = context.Source.Prototype.Partition
                .ComputeSpatialHash();
            var session = new CountyPlanningSession(context.Validator.CountyId);
            var footprint = context.Validator.CreateFootprint(residence,
                placement.Row, placement.Column, placement.Rotation);
            session.CreateDraft(residence, footprint,
                context.Validator.Validate(footprint, session));

            Assert.That(world.AbsoluteDay, Is.EqualTo(absoluteDay));
            Assert.That(world.Facilities.Count, Is.EqualTo(facilityCount));
            Assert.That(world.People.Count, Is.EqualTo(peopleCount));
            Assert.That(context.Source.Prototype.Partition.ComputeSpatialHash(),
                Is.EqualTo(partitionHash));
            Assert.That(session.Drafts.Count, Is.EqualTo(1));
        }

        [Test]
        public void LuoyangCountyPlanningMilitaryProfile_IsConditionalNotPlayerGranted()
        {
            var context = CreatePlanningContext();
            var beacon = context.Profiles.ProfilesByDefinitionId[
                "facility.military.beacon"];
            var placement = FindConnected(context, beacon, true);
            var result = context.Validator.Validate(beacon, placement.Row,
                placement.Column, placement.Rotation);

            Assert.That(beacon.PlayerBuildable, Is.False);
            Assert.That(result.State,
                Is.EqualTo(PlacementValidationState.Conditional));
            Assert.That(result.Warnings.Select(value => value.Code),
                Does.Contain(PlacementReasonIds.MilitaryAuthorityRequired));
        }

        [Test]
        public void LuoyangCountyPlanningBenchmark_UsesBoundedSpatialCandidates()
        {
            var context = CreatePlanningContext();
            var residence = Residence(context);
            var placement = FindConnected(context, residence);
            var performance = CountyPlanningPerformanceBenchmark.Measure(
                context.Validator, residence, placement.Row,
                placement.Column,
                new CountyPlanningSession(context.Validator.CountyId), 16);

            Assert.That(performance.Samples, Is.EqualTo(16));
            Assert.That(performance.CellPickP95Milliseconds,
                Is.GreaterThanOrEqualTo(0d));
            Assert.That(performance.ValidatorP95Milliseconds,
                Is.GreaterThanOrEqualTo(0d));
            Assert.That(context.Validator.IndexedFacilityCount,
                Is.EqualTo(2084));
            Assert.That(context.Validator.IndexedRoadCellCount,
                Is.GreaterThan(0));
            Assert.That(context.Validator.IndexedFortificationCount,
                Is.EqualTo(144));
        }

        private static PlanningContext CreatePlanningContext()
        {
            var source = new Luoyang50mCountySpatialPrototypeSource(
                Luoyang50mWorldMapRoot);
            return new PlanningContext
            {
                Source = source,
                Profiles = new LuoyangFacilityPlacementProfileSource(
                    Luoyang50mWorldMapRoot).Catalog,
                Validator = new FacilityPlacementValidator(source.Prototype,
                    source.LayoutPackage)
            };
        }

        private static FacilityPlacementProfile Residence(
            PlanningContext context) => context.Profiles
            .ProfilesByDefinitionId["facility.residential.urban_quarter"];

        private static (int Row, int Column, int Rotation) FindConnected(
            PlanningContext context, FacilityPlacementProfile profile,
            bool conditionalAllowed = false)
        {
            var partition = context.Source.Prototype.Partition;
            for (var row = 4; row < partition.Rows - 4; row++)
            for (var column = 4; column < partition.Columns - 4; column++)
            {
                if (partition.LandUse(row, column) !=
                    PlanningLandUseClass.Road) continue;
                for (var distance = 1; distance <= 4; distance++)
                {
                    var candidates = new[]
                    {
                        (row - distance, column, 0),
                        (row, column + distance, 1),
                        (row + distance, column, 2),
                        (row, column - distance, 3)
                    };
                    foreach (var candidate in candidates)
                    {
                        var result = context.Validator.Validate(profile,
                            candidate.Item1, candidate.Item2, candidate.Item3);
                        if (result.RoadAccessResult.Status ==
                                FacilityRoadAccessStatus.Connected &&
                            (result.State == PlacementValidationState.Valid ||
                             conditionalAllowed && result.State ==
                             PlacementValidationState.Conditional))
                            return (candidate.Item1, candidate.Item2,
                                candidate.Item3);
                    }
                }
            }
            throw new AssertionException(
                "No connected real Luoyang planning fixture was found.");
        }

        private static PlanningCellCoord FindCell(PlanningContext context,
            Func<int, int, bool> predicate)
        {
            var partition = context.Source.Prototype.Partition;
            for (var row = 0; row < partition.Rows; row++)
            for (var column = 0; column < partition.Columns; column++)
                if (predicate(row, column))
                    return new PlanningCellCoord(row, column);
            throw new AssertionException("Required real Cell was not found.");
        }
    }
}
