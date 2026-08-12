using System;
using System.Collections.Generic;
using System.IO;
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
        public void WorldTime_CarriesSegmentsIntoNextDay()
        {
            var start = new WorldTime(12, DaySegment.Dusk);
            var result = start.AdvanceSegments(3);

            Assert.That(result.AbsoluteDay, Is.EqualTo(13));
            Assert.That(result.Segment, Is.EqualTo(DaySegment.Day));
        }

        [Test]
        public void NamedRandom_SameCompleteKeyReturnsSameValue()
        {
            var first = new NamedRandom(184);
            var second = new NamedRandom(184);
            var personId = new StableId("person.liu_bei");

            var a = first.NextUInt64("travel", personId, 42, "road_event", 0);
            var b = second.NextUInt64("travel", personId, 42, "road_event", 0);

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void NamedRandom_UnrelatedPurposeDoesNotShiftExistingDraw()
        {
            var random = new NamedRandom(184);
            var personId = new StableId("person.liu_bei");
            var before = random.NextUInt64("travel", personId, 42, "road_event", 0);

            _ = random.NextUInt64("market", personId, 42, "grain_price", 0);
            _ = random.NextUInt64("market", personId, 42, "grain_price", 1);

            var after = random.NextUInt64("travel", personId, 42, "road_event", 0);
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void Snapshot_RoundTripPreservesMinimalWorld()
        {
            var world = BuildMinimalWorld();
            world.AdvanceOneDay();

            var json = WorldSnapshotSerializer.Serialize(world);
            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion, Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MasterSeed, Is.EqualTo(world.MasterSeed));
            Assert.That(loaded.AbsoluteDay, Is.EqualTo(1));
            Assert.That(loaded.Revision, Is.EqualTo(1));
            Assert.That(loaded.People.Count, Is.EqualTo(2));
            Assert.That(loaded.Locations.Count, Is.EqualTo(1));
            Assert.That(loaded.Families.Count, Is.EqualTo(1));
            Assert.That(loaded.Families[0].HeadPersonId, Is.EqualTo("person.liu_bei"));
        }

        [Test]
        public void Validation_RejectsMissingLocationReference()
        {
            var world = BuildMinimalWorld();
            world.People[0].LocationId = "location.missing";

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Simulation_SameSeedAndDurationProducesSameSnapshot()
        {
            var first = PrototypeWorldFactory.Create184World(184);
            var second = PrototypeWorldFactory.Create184World(184);

            new WorldSimulator(first.MasterSeed).AdvanceDays(first, 365);
            new WorldSimulator(second.MasterSeed).AdvanceDays(second, 365);

            Assert.That(
                WorldSnapshotSerializer.Serialize(first),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(second)));
        }

        [Test]
        public void Simulation_SaveResumeMatchesContinuousRun()
        {
            var continuous = PrototypeWorldFactory.Create184World(184);
            var resumed = PrototypeWorldFactory.Create184World(184);

            new WorldSimulator(continuous.MasterSeed).AdvanceDays(continuous, 730);

            var firstHalfSimulator = new WorldSimulator(resumed.MasterSeed);
            firstHalfSimulator.AdvanceDays(resumed, 365);
            var saved = WorldSnapshotSerializer.Serialize(resumed);
            resumed = WorldSnapshotSerializer.Deserialize(saved);
            new WorldSimulator(resumed.MasterSeed).AdvanceDays(resumed, 365);

            Assert.That(
                WorldSnapshotSerializer.Serialize(resumed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(continuous)));
        }

        [Test]
        public void NpcAi_SameSeedAndStateProducesSameDecision()
        {
            var person = PrototypeWorldFactory.Create184World(184).People[0];
            var first = new NpcDecisionSystem(184).ChooseMonthlyFocus(person, 12);
            var second = new NpcDecisionSystem(184).ChooseMonthlyFocus(person, 12);

            Assert.That(first.SelectedFocus, Is.EqualTo(second.SelectedFocus));
            Assert.That(first.RankedScores[0].Score, Is.EqualTo(second.RankedScores[0].Score));
        }

        [Test]
        public void NpcAi_SevereScarcityOverridesOrdinaryAmbition()
        {
            var person = PrototypeWorldFactory.Create184World(184).People[0];
            person.Provisions = 0;
            person.Needs.Livelihood = 5_000;
            person.Needs.Status = 5_000;
            person.Personality.Ambition = 10_000;

            var decision = new NpcDecisionSystem(184).ChooseMonthlyFocus(person, 12);

            Assert.That(decision.SelectedFocus, Is.EqualTo(NpcMonthlyFocus.MaintainLivelihood));
        }

        [Test]
        public void NpcAi_FamilyCrisisAndFamilyDutySelectCare()
        {
            var person = PrototypeWorldFactory.Create184World(184).People[0];
            person.Needs.Family = 9_000;
            person.Personality.FamilyDuty = 10_000;

            var decision = new NpcDecisionSystem(184).ChooseMonthlyFocus(person, 12);

            Assert.That(decision.SelectedFocus, Is.EqualTo(NpcMonthlyFocus.CareForFamily));
        }

        [Test]
        public void NpcAction_ScarcityFocusPlansValidWork()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            person.Provisions = 0;
            var decision = new NpcDecisionSystem(184).ChooseMonthlyFocus(person, 12);

            var command = new NpcActionPlanner().Plan(world, person, decision);
            var validation = new NpcActionValidator().Validate(world, command);

            Assert.That(command.ActionType, Is.EqualTo(NpcActionType.Work));
            Assert.That(validation.IsValid, Is.True, validation.Error);
        }

        [Test]
        public void NpcAction_BoldHealthyPersonRespondsToWarByEnlisting()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            person.Needs.WarPressure = 10_000;
            person.Personality.RiskTolerance = 8_000;
            var decision = new NpcDecisionSystem(184).ChooseMonthlyFocus(person, 12);

            var command = new NpcActionPlanner().Plan(world, person, decision);

            Assert.That(decision.SelectedFocus, Is.EqualTo(NpcMonthlyFocus.RespondToWar));
            Assert.That(command.ActionType, Is.EqualTo(NpcActionType.Enlist));
        }

        [Test]
        public void NpcAction_CautiousPersonRespondsToWarByFleeing()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            person.Needs.WarPressure = 10_000;
            person.Personality.RiskTolerance = 2_000;
            world.Locations[1].PublicOrderBasisPoints = 9_000;
            var decision = new NpcDecisionSystem(184).ChooseMonthlyFocus(person, 12);

            var command = new NpcActionPlanner().Plan(world, person, decision);
            var validation = new NpcActionValidator().Validate(world, command);

            Assert.That(command.ActionType, Is.EqualTo(NpcActionType.Flee));
            Assert.That(command.TargetId.Value, Is.EqualTo(world.Locations[1].Id));
            Assert.That(validation.IsValid, Is.True, validation.Error);
        }

        [Test]
        public void Travel_FootJourneyFromZhuoToZhongshanTakesFiveDays()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            var travel = new TravelSystem();
            travel.StartJourney(
                world,
                new StableId(person.Id),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhongshan"),
                TravelMode.Foot);

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 19);
            Assert.That(world.Journeys.Count, Is.EqualTo(1));
            Assert.That(person.LocationId, Is.EqualTo("location.zhuo"));

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 1);
            Assert.That(world.Journeys.Count, Is.EqualTo(0));
            Assert.That(person.LocationId, Is.EqualTo("location.zhongshan"));
            Assert.That(world.AbsoluteDay, Is.EqualTo(5));
        }

        [Test]
        public void Travel_SaveResumePreservesJourneyProgress()
        {
            var continuous = PrototypeWorldFactory.Create184World(184);
            var resumed = PrototypeWorldFactory.Create184World(184);
            var travel = new TravelSystem();
            travel.StartJourney(
                continuous,
                new StableId("person.liu_bei"),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhongshan"),
                TravelMode.Foot);
            travel.StartJourney(
                resumed,
                new StableId("person.liu_bei"),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhongshan"),
                TravelMode.Foot);

            new WorldSimulator(184).AdvanceSegments(continuous, 20);
            new WorldSimulator(184).AdvanceSegments(resumed, 8);
            resumed = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(resumed));
            new WorldSimulator(184).AdvanceSegments(resumed, 12);

            Assert.That(
                WorldSnapshotSerializer.Serialize(resumed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(continuous)));
        }

        [Test]
        public void NpcActionResolver_WorkChangesWealthAndProvisionsDeterministically()
        {
            var first = PrototypeWorldFactory.Create184World(184);
            var second = PrototypeWorldFactory.Create184World(184);
            var firstPerson = first.People[0];
            var secondPerson = second.People[0];
            firstPerson.Provisions = 0;
            secondPerson.Provisions = 0;
            var firstDecision = new NpcDecisionSystem(184).ChooseMonthlyFocus(firstPerson, 1);
            var secondDecision = new NpcDecisionSystem(184).ChooseMonthlyFocus(secondPerson, 1);
            var planner = new NpcActionPlanner();

            var firstOutcome = new NpcActionResolver(184).Resolve(
                first,
                planner.Plan(first, firstPerson, firstDecision),
                1);
            new NpcActionResolver(184).Resolve(
                second,
                planner.Plan(second, secondPerson, secondDecision),
                1);

            Assert.That(firstOutcome.Status, Is.EqualTo(NpcActionResolutionStatus.Completed));
            Assert.That(firstPerson.Wealth, Is.GreaterThan(0));
            Assert.That(firstPerson.Provisions, Is.GreaterThan(0));
            Assert.That(firstPerson.Wealth, Is.EqualTo(secondPerson.Wealth));
            Assert.That(firstPerson.Provisions, Is.EqualTo(secondPerson.Provisions));
        }

        [Test]
        public void NpcActionResolver_FleeStartsARealJourney()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            person.Needs.WarPressure = 10_000;
            person.Personality.RiskTolerance = 1_000;
            world.Locations[1].PublicOrderBasisPoints = 9_000;
            var decision = new NpcDecisionSystem(184).ChooseMonthlyFocus(person, 1);
            var command = new NpcActionPlanner().Plan(world, person, decision);

            var outcome = new NpcActionResolver(184).Resolve(world, command, 1);

            Assert.That(outcome.Status, Is.EqualTo(NpcActionResolutionStatus.StartedJourney));
            Assert.That(world.Journeys.Count, Is.EqualTo(1));
            Assert.That(world.Journeys[0].PersonId, Is.EqualTo(person.Id));
        }

        [Test]
        public void NpcActionResolver_VisitUpdatesDirectionalRelationships()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var actor = world.People[0];
            actor.Needs.Relationships = 10_000;
            actor.Personality.Sociability = 10_000;
            var beforeOutgoing = world.Relationships[0].Affection;
            var beforeResponse = world.Relationships[1].Affection;
            var decision = new NpcDecisionSystem(184).ChooseMonthlyFocus(actor, 1);
            var command = new NpcActionPlanner().Plan(world, actor, decision);

            var outcome = new NpcActionResolver(184).Resolve(world, command, 1);

            Assert.That(command.ActionType, Is.EqualTo(NpcActionType.Visit));
            Assert.That(outcome.Status, Is.EqualTo(NpcActionResolutionStatus.Completed));
            Assert.That(world.Relationships[0].Affection, Is.GreaterThan(beforeOutgoing));
            Assert.That(world.Relationships[1].Affection, Is.GreaterThan(beforeResponse));
            Assert.That(
                world.Relationships[0].Affection - beforeOutgoing,
                Is.Not.EqualTo(world.Relationships[1].Affection - beforeResponse));
        }

        [Test]
        public void Snapshot_RoundTripPreservesRelationships()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.Relationships.Count, Is.EqualTo(world.Relationships.Count));
            Assert.That(
                loaded.Relationships[0].FromPersonId,
                Is.EqualTo(world.Relationships[0].FromPersonId));
            Assert.That(
                loaded.Relationships[0].Affection,
                Is.EqualTo(world.Relationships[0].Affection));
        }

        [Test]
        public void NpcActionResolver_SeekOfficeJoinsCountyGovernment()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            person.Needs.Status = 10_000;
            person.Personality.Ambition = 10_000;
            var decision = new NpcDecisionSystem(184).ChooseMonthlyFocus(person, 1);
            var command = new NpcActionPlanner().Plan(world, person, decision);

            var outcome = new NpcActionResolver(184).Resolve(world, command, 1);

            Assert.That(command.ActionType, Is.EqualTo(NpcActionType.SeekOffice));
            Assert.That(outcome.Status, Is.EqualTo(NpcActionResolutionStatus.Completed));
            Assert.That(
                world.Memberships.Exists(
                    membership =>
                        membership.PersonId == person.Id &&
                        membership.OrganizationId == "organization.zhuo_county_office"),
                Is.True);
        }

        [Test]
        public void NpcActionResolver_EnlistJoinsLocalMilitary()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            person.Needs.WarPressure = 10_000;
            person.Personality.RiskTolerance = 9_000;
            var decision = new NpcDecisionSystem(184).ChooseMonthlyFocus(person, 1);
            var command = new NpcActionPlanner().Plan(world, person, decision);

            var outcome = new NpcActionResolver(184).Resolve(world, command, 1);

            Assert.That(command.ActionType, Is.EqualTo(NpcActionType.Enlist));
            Assert.That(outcome.Status, Is.EqualTo(NpcActionResolutionStatus.Completed));
            Assert.That(
                world.Memberships.Exists(
                    membership =>
                        membership.PersonId == person.Id &&
                        membership.OrganizationId == "organization.youzhou_field_force"),
                Is.True);
        }

        [Test]
        public void TaskSystem_ClerkTaskCompletesAfterThreeLocalDays()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            var join = new OrganizationSystem().TryJoinAtCurrentLocation(
                world,
                new StableId(person.Id),
                OrganizationType.Government);
            Assert.That(join.Success, Is.True, join.Message);

            var accepted = new TaskSystem().TryAccept(
                world,
                new StableId(person.Id),
                new StableId("task_definition.verify_households"));
            Assert.That(accepted.Success, Is.True, accepted.Message);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 3);

            Assert.That(accepted.Task.Status, Is.EqualTo(TaskStatus.Completed));
            Assert.That(accepted.Task.RewardClaimed, Is.True);
            Assert.That(person.Wealth, Is.EqualTo(100));
            Assert.That(person.Provisions, Is.EqualTo(12));
        }

        [Test]
        public void TaskSystem_MilitaryDeliveryCompletesOnArrival()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            var targetArmy = world.Armies.Find(
                item => item.Id == "army.youzhou_reinforcement");
            var armyProvisionsBefore = targetArmy.Provisions;
            var join = new OrganizationSystem().TryJoinAtCurrentLocation(
                world,
                new StableId(person.Id),
                OrganizationType.Military);
            Assert.That(join.Success, Is.True, join.Message);

            var accepted = new TaskSystem().TryAccept(
                world,
                new StableId(person.Id),
                new StableId("task_definition.deliver_military_grain"));
            Assert.That(accepted.Success, Is.True, accepted.Message);
            new TravelSystem().StartJourney(
                world,
                new StableId(person.Id),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhongshan"),
                TravelMode.Foot);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 5);

            Assert.That(person.LocationId, Is.EqualTo("location.zhongshan"));
            Assert.That(accepted.Task.Status, Is.EqualTo(TaskStatus.Completed));
            Assert.That(person.Wealth, Is.EqualTo(300));
            Assert.That(person.Provisions, Is.EqualTo(11));
            Assert.That(
                targetArmy.Provisions,
                Is.EqualTo(armyProvisionsBefore + 1_000));
            Assert.That(world.MilitarySupplies.Count, Is.EqualTo(1));
            Assert.That(
                world.MilitarySupplies[0].Type,
                Is.EqualTo(MilitarySupplyType.TaskDelivery));
        }

        [Test]
        public void HistoricalEvents_OutbreakAppliesEffectsOnlyOnce()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            world.AbsoluteDay = 30;
            var system = new HistoricalEventSystem();
            var guangzong = world.Locations.Find(
                location => location.Id == "location.guangzong");
            var initialOrder = guangzong.PublicOrderBasisPoints;
            var initialPrice = guangzong.GrainPrice;

            var first = system.ResolveEligibleEvents(world);
            var orderAfterFirst = guangzong.PublicOrderBasisPoints;
            var priceAfterFirst = guangzong.GrainPrice;
            var second = system.ResolveEligibleEvents(world);

            Assert.That(first.Count, Is.EqualTo(1));
            Assert.That(first[0].DefinitionId,
                Is.EqualTo("historical_event.yellow_turban_outbreak"));
            Assert.That(orderAfterFirst, Is.EqualTo(initialOrder - 2_500));
            Assert.That(priceAfterFirst, Is.EqualTo(initialPrice + 30));
            Assert.That(second.Count, Is.EqualTo(0));
            Assert.That(guangzong.PublicOrderBasisPoints, Is.EqualTo(orderAfterFirst));
            Assert.That(guangzong.GrainPrice, Is.EqualTo(priceAfterFirst));
        }

        [Test]
        public void HistoricalEvents_ResolveInPrerequisiteOrderByDayNinety()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 90);

            Assert.That(world.HistoricalAnchors.Count, Is.EqualTo(3));
            Assert.That(
                world.HistoricalAnchors.TrueForAll(
                    anchor => anchor.Status == HistoricalAnchorStatus.Resolved),
                Is.True);
            Assert.That(
                world.HistoricalAnchors.Find(
                    anchor =>
                        anchor.DefinitionId == "historical_event.lu_zhi_recalled")
                    .CausalEventIds,
                Does.Contain("historical_event.guangzong_siege"));
        }

        [Test]
        public void Snapshot_RoundTripPreservesHistoricalTimeline()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 60);
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.HistoricalAnchors.Count, Is.EqualTo(3));
            Assert.That(
                loaded.HistoricalAnchors.Find(
                    anchor =>
                        anchor.DefinitionId == "historical_event.guangzong_siege")
                    .Status,
                Is.EqualTo(HistoricalAnchorStatus.Resolved));
        }

        [Test]
        public void HistoricalEvents_OutbreakUnlocksFiveCrisisTasks()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var availableBefore = world.TaskDefinitions.FindAll(
                definition => definition.IsAvailable).Count;

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 30);
            var availableAfter = world.TaskDefinitions.FindAll(
                definition => definition.IsAvailable).Count;

            Assert.That(availableBefore, Is.EqualTo(3));
            Assert.That(availableAfter, Is.EqualTo(8));
            Assert.That(
                world.TaskDefinitions.Find(
                    definition =>
                        definition.Id == "task_definition.recruit_volunteers")
                    .IsAvailable,
                Is.True);
        }

        [Test]
        public void DynamicTask_PublicVolunteerRecruitmentNeedsNoMembership()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            var beforeOutbreak = new TaskSystem().TryAccept(
                world,
                new StableId(person.Id),
                new StableId("task_definition.recruit_volunteers"));
            Assert.That(beforeOutbreak.Success, Is.False);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 30);
            var accepted = new TaskSystem().TryAccept(
                world,
                new StableId(person.Id),
                new StableId("task_definition.recruit_volunteers"));
            Assert.That(accepted.Success, Is.True, accepted.Message);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 2);

            Assert.That(accepted.Task.Status, Is.EqualTo(TaskStatus.Completed));
            Assert.That(person.Wealth, Is.EqualTo(150));
        }

        [Test]
        public void LifeSimulation_MonthlyUpkeepCreatesHouseholdDebt()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var family = world.Families.Find(
                item => item.Id == "family.zhuo_farm_household");

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 30);

            Assert.That(family.Wealth, Is.EqualTo(0));
            Assert.That(family.Debt, Is.EqualTo(10));
            Assert.That(
                world.LifeEvents.Exists(
                    item =>
                        item.Type == LifeEventType.HouseholdDebt &&
                        item.FamilyId == family.Id),
                Is.True);
        }

        [Test]
        public void LifeSimulation_DeadFamilyHeadIsSucceededByLivingSpouse()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var family = world.Families.Find(
                item => item.Id == "family.zhuo_farm_household");
            var formerHead = world.People.Find(
                item => item.Id == family.HeadPersonId);
            world.AbsoluteDay = 30;
            formerHead.HealthBasisPoints = 0;

            new LifeSimulationSystem(world.MasterSeed).ResolveMonthly(world);

            Assert.That(formerHead.IsAlive, Is.False);
            Assert.That(
                family.HeadPersonId,
                Is.EqualTo("person.generated.farmer_002"));
            Assert.That(
                world.LifeEvents.Exists(
                    item =>
                        item.Type == LifeEventType.Death &&
                        item.PrimaryPersonId == formerHead.Id),
                Is.True);
            Assert.That(
                world.LifeEvents.Exists(
                    item =>
                        item.Type == LifeEventType.Succession &&
                        item.PrimaryPersonId == family.HeadPersonId),
                Is.True);
        }

        [Test]
        public void Snapshot_RoundTripPreservesFamilyFinancesAndLifeEvents()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 30);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var family = loaded.Families.Find(
                item => item.Id == "family.zhuo_farm_household");

            Assert.That(family.Debt, Is.EqualTo(10));
            Assert.That(loaded.LifeEvents.Count, Is.GreaterThan(0));
            Assert.That(
                loaded.LifeEvents.Exists(
                    item => item.Type == LifeEventType.HouseholdDebt),
                Is.True);
        }

        [Test]
        public void Market_PrototypeHasFiveGoodsAtEveryLocation()
        {
            var world = PrototypeWorldFactory.Create184World(184);

            Assert.That(world.Commodities.Count, Is.EqualTo(5));
            Assert.That(
                world.MarketListings.Count,
                Is.EqualTo(world.Locations.Count * world.Commodities.Count));
            Assert.That(
                world.MarketListings.Exists(
                    item =>
                        item.LocationId == "location.zhongshan" &&
                        item.CommodityId == "commodity.horses" &&
                        item.Price == 520),
                Is.True);
        }

        [Test]
        public void Trading_BuyTravelAndSellCreatesProfitableMerchantRoute()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var merchant = world.People.Find(
                item => item.Id == "person.zhang_shiping");
            var trading = new TradingSystem();
            var startingWealth = merchant.Wealth;

            var bought = trading.Buy(
                world,
                new StableId(merchant.Id),
                new StableId("commodity.horses"),
                2);
            Assert.That(bought.Success, Is.True, bought.Message);

            new TravelSystem().StartJourney(
                world,
                new StableId(merchant.Id),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhuo"),
                TravelMode.Caravan);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 6);

            var sold = trading.Sell(
                world,
                new StableId(merchant.Id),
                new StableId("commodity.horses"),
                2);

            Assert.That(merchant.LocationId, Is.EqualTo("location.zhuo"));
            Assert.That(sold.Success, Is.True, sold.Message);
            Assert.That(sold.RealizedProfit, Is.GreaterThan(0));
            Assert.That(merchant.Wealth, Is.GreaterThan(startingWealth));
            Assert.That(
                trading.GetQuantity(world, merchant.Id, "commodity.horses"),
                Is.EqualTo(0));
            Assert.That(world.TradeRecords.Count, Is.EqualTo(2));
        }

        [Test]
        public void Trading_RejectsCargoBeyondCapacityWithoutChangingWorld()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var merchant = world.People.Find(
                item => item.Id == "person.zhang_shiping");
            merchant.Wealth = 100_000;
            var listing = world.MarketListings.Find(
                item =>
                    item.LocationId == merchant.LocationId &&
                    item.CommodityId == "commodity.horses");
            var stockBefore = listing.Stock;

            var result = new TradingSystem().Buy(
                world,
                new StableId(merchant.Id),
                new StableId("commodity.horses"),
                13);

            Assert.That(result.Success, Is.False);
            Assert.That(merchant.Wealth, Is.EqualTo(100_000));
            Assert.That(listing.Stock, Is.EqualTo(stockBefore));
            Assert.That(world.Inventories.Count, Is.EqualTo(0));
        }

        [Test]
        public void Snapshot_RoundTripPreservesMarketCargoAndTradeLedger()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var merchant = world.People.Find(
                item => item.Id == "person.zhang_shiping");
            var result = new TradingSystem().Buy(
                world,
                new StableId(merchant.Id),
                new StableId("commodity.cloth"),
                5);
            Assert.That(result.Success, Is.True, result.Message);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.Commodities.Count, Is.EqualTo(5));
            Assert.That(loaded.Inventories.Count, Is.EqualTo(1));
            Assert.That(loaded.Inventories[0].Quantity, Is.EqualTo(5));
            Assert.That(loaded.Inventories[0].AverageUnitCost, Is.EqualTo(165));
            Assert.That(loaded.TradeRecords.Count, Is.EqualTo(1));
            Assert.That(loaded.TradeRecords[0].IsPurchase, Is.True);
        }

        [Test]
        public void HistoricalEvents_OutbreakMobilizesYellowTurbanArmy()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var yellowTurban = world.Armies.Find(
                item => item.Id == "army.yellow_turban_guangzong");
            Assert.That(yellowTurban.IsMobilized, Is.False);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 30);

            Assert.That(yellowTurban.IsMobilized, Is.True);
        }

        [Test]
        public void ArmyMarch_XiaquyangToGuangzongTakesEightDaysAndConsumesSupplies()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var army = world.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var startingProvisions = army.Provisions;
            new ArmySystem().StartMarch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(army.Id),
                new StableId("route.xiaquyang_guangzong"),
                new StableId("location.guangzong"));

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 7);
            Assert.That(world.ArmyMarches.Count, Is.EqualTo(1));
            Assert.That(army.LocationId, Is.EqualTo("location.xiaquyang"));

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);

            Assert.That(world.ArmyMarches.Count, Is.EqualTo(0));
            Assert.That(army.LocationId, Is.EqualTo("location.guangzong"));
            Assert.That(army.Provisions, Is.LessThan(startingProvisions));
        }

        [Test]
        public void BattleResolver_SameSeedProducesSameBattleAndAppliesLosses()
        {
            var first = BuildGuangzongBattleWorld();
            var second = BuildGuangzongBattleWorld();
            var firstLocation = first.Locations.Find(
                item => item.Id == "location.guangzong");
            var orderBefore = firstLocation.PublicOrderBasisPoints;

            var firstOutcome = new BattleResolver(first.MasterSeed).Resolve(
                first,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            var secondOutcome = new BattleResolver(second.MasterSeed).Resolve(
                second,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));

            Assert.That(
                firstOutcome.Record.AttackerCasualties,
                Is.EqualTo(secondOutcome.Record.AttackerCasualties));
            Assert.That(
                firstOutcome.Record.DefenderCasualties,
                Is.EqualTo(secondOutcome.Record.DefenderCasualties));
            Assert.That(
                firstOutcome.Record.Result,
                Is.EqualTo(secondOutcome.Record.Result));
            Assert.That(firstOutcome.Record.AttackerCasualties, Is.GreaterThan(0));
            Assert.That(firstOutcome.Record.DefenderCasualties, Is.GreaterThan(0));
            Assert.That(firstOutcome.Record.AttackerWounded, Is.GreaterThan(0));
            Assert.That(firstOutcome.Record.DefenderWounded, Is.GreaterThan(0));
            Assert.That(firstLocation.PublicOrderBasisPoints, Is.LessThan(orderBefore));
        }

        [Test]
        public void Snapshot_RoundTripPreservesArmiesMarchesAndBattleHistory()
        {
            var world = BuildGuangzongBattleWorld();
            new BattleResolver(world.MasterSeed).Resolve(
                world,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.Armies.Count, Is.EqualTo(3));
            Assert.That(loaded.Battles.Count, Is.EqualTo(1));
            Assert.That(
                loaded.Battles[0].AttackerArmyId,
                Is.EqualTo("army.han_jizhou_vanguard"));
            Assert.That(loaded.Battles[0].AttackerCasualties, Is.GreaterThan(0));
        }

        [Test]
        public void MilitarySupply_MerchantCanSellCarriedGrainToLocalArmy()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var merchant = world.People.Find(
                item => item.Id == "person.zhang_shiping");
            var army = world.Armies.Find(
                item => item.Id == "army.youzhou_reinforcement");
            var organization = world.Organizations.Find(
                item => item.Id == army.OrganizationId);
            var startingWealth = merchant.Wealth;
            var startingArmyProvisions = army.Provisions;
            var startingTreasury = organization.Treasury;
            var bought = new TradingSystem().Buy(
                world,
                new StableId(merchant.Id),
                new StableId("commodity.grain"),
                10);
            Assert.That(bought.Success, Is.True, bought.Message);

            var supplied = new MilitarySupplySystem().SellGrainToArmy(
                world,
                new StableId(merchant.Id),
                new StableId(army.Id),
                10);

            Assert.That(supplied.Success, Is.True, supplied.Message);
            Assert.That(supplied.RealizedProfit, Is.GreaterThan(0));
            Assert.That(merchant.Wealth, Is.GreaterThan(startingWealth));
            Assert.That(
                army.Provisions,
                Is.EqualTo(
                    startingArmyProvisions +
                    10 * MilitarySupplySystem.ProvisionsPerGrainUnit));
            Assert.That(organization.Treasury, Is.LessThan(startingTreasury));
            Assert.That(world.Inventories.Count, Is.EqualTo(0));
            Assert.That(world.MilitarySupplies.Count, Is.EqualTo(1));
            Assert.That(
                world.MilitarySupplies[0].Type,
                Is.EqualTo(MilitarySupplyType.MerchantSale));
        }

        [Test]
        public void MilitarySupply_LocalPurchaseLinksTreasuryMarketAndArmy()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var army = world.Armies.Find(
                item => item.Id == "army.youzhou_reinforcement");
            var organization = world.Organizations.Find(
                item => item.Id == army.OrganizationId);
            var listing = world.MarketListings.Find(
                item =>
                    item.LocationId == army.LocationId &&
                    item.CommodityId == "commodity.grain");
            var provisionsBefore = army.Provisions;
            var treasuryBefore = organization.Treasury;
            var stockBefore = listing.Stock;
            var priceBefore = listing.Price;

            var result = new MilitarySupplySystem().PurchaseLocalGrain(
                world,
                new StableId(army.Id),
                20);

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(
                army.Provisions,
                Is.EqualTo(
                    provisionsBefore +
                    20 * MilitarySupplySystem.ProvisionsPerGrainUnit));
            Assert.That(organization.Treasury, Is.LessThan(treasuryBefore));
            Assert.That(listing.Stock, Is.EqualTo(stockBefore - 20));
            Assert.That(listing.Price, Is.GreaterThan(priceBefore));
        }

        [Test]
        public void Snapshot_RoundTripPreservesMilitarySupplyLedger()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var army = world.Armies.Find(
                item => item.Id == "army.youzhou_reinforcement");
            new MilitarySupplySystem().PurchaseLocalGrain(
                world,
                new StableId(army.Id),
                10);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.MilitarySupplies.Count, Is.EqualTo(1));
            Assert.That(
                loaded.MilitarySupplies[0].ArmyId,
                Is.EqualTo(army.Id));
            Assert.That(
                loaded.MilitarySupplies[0].Type,
                Is.EqualTo(MilitarySupplyType.LocalMarketPurchase));
            Assert.That(
                loaded.MilitarySupplies[0].ProvisionsAdded,
                Is.EqualTo(100));
        }

        [Test]
        public void MedicalTreatment_WithHerbsRecoversWoundedDeterministically()
        {
            var first = BuildGuangzongBattleWorld();
            var second = BuildGuangzongBattleWorld();
            new BattleResolver(first.MasterSeed).Resolve(
                first,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            new BattleResolver(second.MasterSeed).Resolve(
                second,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            var firstPhysician = first.People.Find(
                item => item.Id == "person.generated.physician_001");
            var secondPhysician = second.People.Find(
                item => item.Id == "person.generated.physician_001");
            var firstArmy = first.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var secondArmy = second.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var woundedBefore = firstArmy.WoundedTroops;
            var troopsBefore = firstArmy.Troops;
            var equipmentIssuesBefore = first.MilitaryEquipmentIssues.Count;
            var equipmentTransactionsBefore =
                first.MilitaryEquipmentTransactions.Count;
            var equipmentStocksBefore =
                EquipmentStockQuantity(first);
            Assert.That(woundedBefore, Is.GreaterThan(0));
            var firstMedicineBefore = ArmyMedicineQuantity(first, firstArmy);
            var secondMedicineBefore = ArmyMedicineQuantity(second, secondArmy);

            var firstResult = new MedicalSystem(first.MasterSeed).TreatArmyWounded(
                first,
                new StableId(firstPhysician.Id),
                new StableId(firstArmy.Id),
                25);
            var secondResult = new MedicalSystem(second.MasterSeed).TreatArmyWounded(
                second,
                new StableId(secondPhysician.Id),
                new StableId(secondArmy.Id),
                25);

            Assert.That(firstResult.Success, Is.True, firstResult.Message);
            Assert.That(firstResult.RecoveredTroops, Is.GreaterThan(0));
            Assert.That(
                firstResult.RecoveredTroops,
                Is.EqualTo(secondResult.RecoveredTroops));
            Assert.That(
                firstArmy.WoundedTroops,
                Is.EqualTo(woundedBefore - firstResult.RecoveredTroops));
            Assert.That(
                firstArmy.Troops,
                Is.EqualTo(troopsBefore + firstResult.RecoveredTroops));
            Assert.That(first.MedicalTreatments.Count, Is.EqualTo(1));
            Assert.That(
                first.MilitaryEquipmentIssues.Count,
                Is.EqualTo(equipmentIssuesBefore));
            Assert.That(
                first.MilitaryEquipmentTransactions.Count,
                Is.EqualTo(equipmentTransactionsBefore));
            Assert.That(
                EquipmentStockQuantity(first),
                Is.EqualTo(equipmentStocksBefore));
            Assert.That(
                ArmyMedicineQuantity(first, firstArmy),
                Is.EqualTo(firstMedicineBefore - firstResult.HerbsConsumed));
            Assert.That(
                ArmyMedicineQuantity(second, secondArmy),
                Is.EqualTo(secondMedicineBefore - secondResult.HerbsConsumed));
            Assert.That(
                first.MilitaryMedicalServices.Count,
                Is.EqualTo(firstResult.RecoveredTroops));
            Assert.That(
                first.MilitaryMedicalCases.Count,
                Is.EqualTo(firstResult.RecoveredTroops));
        }

        [Test]
        public void MedicalTreatment_WithoutHerbsLeavesWoundedUnchanged()
        {
            var world = BuildGuangzongBattleWorld();
            new BattleResolver(world.MasterSeed).Resolve(
                world,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            var physician = world.People.Find(
                item => item.Id == "person.generated.physician_001");
            var army = world.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var woundedBefore = army.WoundedTroops;
            RemoveArmyMedicine(world, army);

            var result = new MedicalSystem(world.MasterSeed).TreatArmyWounded(
                world,
                new StableId(physician.Id),
                new StableId(army.Id),
                20);

            Assert.That(result.Success, Is.False);
            Assert.That(army.WoundedTroops, Is.EqualTo(woundedBefore));
            Assert.That(world.MedicalTreatments.Count, Is.EqualTo(0));
        }

        [Test]
        public void MedicalTreatment_InjectedRepositoryTracksRecoveredPatientsOnly()
        {
            var inline = BuildGuangzongBattleWorld();
            var accessed = BuildGuangzongBattleWorld();
            new BattleResolver(inline.MasterSeed).Resolve(
                inline,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            new BattleResolver(accessed.MasterSeed).Resolve(
                accessed,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            var inlinePhysician = inline.People.Find(
                item => item.Id == "person.generated.physician_001");
            var accessedPhysician = accessed.People.Find(
                item => item.Id == "person.generated.physician_001");
            var inlineArmy = inline.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var accessedArmy = accessed.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var repository = new WorldStatePersonRepository(accessed);

            var inlineResult = new MedicalSystem(inline.MasterSeed)
                .TreatArmyWounded(
                    inline,
                    new StableId(inlinePhysician.Id),
                    new StableId(inlineArmy.Id),
                    25);
            var accessedResult = new MedicalSystem(
                accessed.MasterSeed, repository).TreatArmyWounded(
                    accessed,
                    new StableId(accessedPhysician.Id),
                    new StableId(accessedArmy.Id),
                    25);

            Assert.That(accessedResult.Success, Is.True, accessedResult.Message);
            Assert.That(
                accessedResult.RecoveredTroops,
                Is.EqualTo(inlineResult.RecoveredTroops));
            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            var changedPeople = repository.GetChangedPersonIds();
            Assert.That(changedPeople.Count, Is.EqualTo(
                accessedResult.RecoveredTroops + 1));
            Assert.That(changedPeople, Does.Contain(accessedPhysician.Id));
            for (var i = 0; i < changedPeople.Count; i++)
            {
                if (changedPeople[i] == accessedPhysician.Id)
                {
                    continue;
                }
                var service = accessed.MilitaryServices.Find(
                    item => item.PersonId == changedPeople[i]);
                Assert.That(service, Is.Not.Null);
                Assert.That(
                    service.Status,
                    Is.EqualTo(MilitaryServiceStatus.Active));
                Assert.That(
                    repository.GetRequired(changedPeople[i]).HealthBasisPoints,
                    Is.GreaterThanOrEqualTo(6_000));
            }

            Assert.That(repository.GetAddedPersonIds(), Is.Empty);
        }

        [Test]
        public void MedicalTreatment_FailureReadsStayClean()
        {
            var world = BuildGuangzongBattleWorld();
            new BattleResolver(world.MasterSeed).Resolve(
                world,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            var physician = world.People.Find(
                item => item.Id == "person.generated.physician_001");
            var army = world.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var repository = new WorldStatePersonRepository(world);
            RemoveArmyMedicine(world, army);
            var before = WorldSnapshotSerializer.Serialize(world);

            var result = new MedicalSystem(
                world.MasterSeed, repository).TreatArmyWounded(
                    world,
                    new StableId(physician.Id),
                    new StableId(army.Id),
                    20);

            Assert.That(result.Success, Is.False);
            Assert.That(
                WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            Assert.That(repository.GetAddedPersonIds(), Is.Empty);
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
        }

        [Test]
        public void Snapshot_RoundTripPreservesMedicalTreatmentHistory()
        {
            var world = BuildGuangzongBattleWorld();
            new BattleResolver(world.MasterSeed).Resolve(
                world,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            var physician = world.People.Find(
                item => item.Id == "person.generated.physician_001");
            var army = world.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            new MedicalSystem(world.MasterSeed).TreatArmyWounded(
                world,
                new StableId(physician.Id),
                new StableId(army.Id),
                25);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.MedicalTreatments.Count, Is.EqualTo(1));
            Assert.That(
                loaded.MedicalTreatments[0].PhysicianPersonId,
                Is.EqualTo(physician.Id));
            Assert.That(
                loaded.MedicalTreatments[0].RecoveredTroops,
                Is.GreaterThan(0));
            Assert.That(
                loaded.MilitaryMedicalServices.Count,
                Is.EqualTo(world.MilitaryMedicalServices.Count));
            Assert.That(
                loaded.MilitaryMedicalCases.Count,
                Is.EqualTo(world.MilitaryMedicalCases.Count));
        }

        [Test]
        public void MilitaryMedical_PrototypeCreatesOneOrganizationStorePerArmy()
        {
            var world = PrototypeWorldFactory.Create184World(184_025);

            Assert.That(world.MilitaryMedicalInitialized, Is.True);
            Assert.That(
                world.Armies.TrueForAll(army =>
                    !string.IsNullOrEmpty(
                        army.MedicalInventoryContainerId) &&
                    ArmyMedicineQuantity(world, army) ==
                        MilitaryMedicalRules
                            .PrototypeOpeningMedicineQuantity),
                Is.True);
            var containerIds = new HashSet<string>();
            for (var i = 0; i < world.Armies.Count; i++)
                containerIds.Add(
                    world.Armies[i].MedicalInventoryContainerId);
            Assert.That(containerIds.Count, Is.EqualTo(world.Armies.Count));
            world.Validate();
        }

        [Test]
        public void MilitaryMedical_DailyWorkLimitIsSharedAndSecondAttemptIsAtomic()
        {
            var world = BuildGuangzongBattleWorld();
            var army = world.Armies.Find(item =>
                item.Id == "army.han_jizhou_vanguard");
            var physician = world.People.Find(item =>
                item.Id == "person.generated.physician_001");
            new MilitaryServiceSystem().ApplyCasualties(
                world,
                new StableId(army.Id),
                12,
                12,
                25);

            var result = new MedicalSystem(world.MasterSeed)
                .TreatArmyWounded(
                    world,
                    new StableId(physician.Id),
                    new StableId(army.Id),
                    12);
            var afterFirst = WorldSnapshotSerializer.Serialize(world);
            var rejected = new MedicalSystem(world.MasterSeed)
                .TreatArmyWounded(
                    world,
                    new StableId(physician.Id),
                    new StableId(army.Id),
                    1);

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.PatientsTreated, Is.EqualTo(8));
            Assert.That(rejected.Success, Is.False);
            Assert.That(
                WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(afterFirst));
        }

        [Test]
        public void MilitaryMedical_TamperedInventorySourceIsRejected()
        {
            var world = BuildGuangzongBattleWorld();
            new BattleResolver(world.MasterSeed).Resolve(
                world,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            var physician = world.People.Find(item =>
                item.Id == "person.generated.physician_001");
            var army = world.Armies.Find(item =>
                item.Id == "army.han_jizhou_vanguard");
            new MedicalSystem(world.MasterSeed).TreatArmyWounded(
                world,
                new StableId(physician.Id),
                new StableId(army.Id),
                1);
            var service = world.MilitaryMedicalServices[0];
            world.InventoryTransactions.Find(item =>
                item.Id == service.InventoryTransactionId)
                .SourceMilitaryMedicalServiceId = "military_medical_service.missing";

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFortySixWithoutFabricatingMilitaryMedicine()
        {
            var world = WorldState.Create(184_027);
            world.AbsoluteDay = 40;
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 46");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryMedicalInitialized, Is.False);
            Assert.That(loaded.MilitaryMedicalCases, Is.Empty);
            Assert.That(loaded.MilitaryMedicalServices, Is.Empty);
            Assert.That(
                loaded.MilitaryMedicalContractActivationDay,
                Is.EqualTo(41));
        }

        [Test]
        public void MilitaryMedicalResupply_CommercialFreightPaysLosesAndReceivesRealBatch()
        {
            var world = PrepareMerchantLogisticsWorld();
            AddMilitaryMedicalLogisticsBatch(world, 40);
            var army = world.Armies.Find(item =>
                item.Id == "army.youzhou_reinforcement");
            var buyer = world.Organizations.Find(item =>
                item.Id == army.OrganizationId);
            var supplier = world.Organizations.Find(item =>
                item.Id == "organization.zhongshan_merchants");
            var medicineBefore = ArmyMedicineQuantity(world, army);
            var buyerMoneyBefore = buyer.Treasury;
            var supplierMoneyBefore = supplier.Treasury;
            StartYouzhouArmyToAnping(world);

            var order = new MilitaryMedicalResupplySystem().Dispatch(
                world, MedicalResupplyRequest(30, 4, true));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 18);

            Assert.That(order.Status,
                Is.EqualTo(MilitaryLogisticsStatus.Delivered));
            Assert.That(order.DeliveryPolicyId, Is.EqualTo(
                MilitaryLogisticsDeliveryPolicyIds.ArmyInventoryContainer));
            Assert.That(order.TargetInventoryContainerId,
                Is.EqualTo(army.MedicalInventoryContainerId));
            Assert.That(order.NaturalLossQuantity, Is.GreaterThan(0));
            Assert.That(
                ArmyMedicineQuantity(world, army),
                Is.EqualTo(medicineBefore + order.DeliveredCargoQuantity));
            Assert.That(buyer.Treasury,
                Is.EqualTo(buyerMoneyBefore - order.TotalPaid));
            Assert.That(supplier.Treasury,
                Is.EqualTo(supplierMoneyBefore + order.TotalPaid));
            Assert.That(world.InventoryTransactions.Exists(item =>
                item.Type == InventoryTransactionType
                    .MilitaryLogisticsDelivered &&
                item.SourceMilitaryLogisticsOrderId == order.Id), Is.True);
            Assert.That(world.MilitarySupplies.Exists(item =>
                item.SourceLogisticsOrderId == order.Id), Is.False);
            Assert.That(
                new MilitaryLogisticsSystem().Audit(world, order.Id)
                    .IsBalanced,
                Is.True);
            world.Validate();
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryLogisticsOrders.Find(item =>
                item.Id == order.Id).TargetInventoryContainerId,
                Is.EqualTo(army.MedicalInventoryContainerId));
            loaded.Validate();
        }

        [Test]
        public void MilitaryMedicalResupply_CapacityAllowsOnlyAuditedPartialReceipt()
        {
            var world = PrepareMerchantLogisticsWorld();
            AddMilitaryMedicalLogisticsBatch(world, 20);
            var army = world.Armies.Find(item =>
                item.Id == "army.youzhou_reinforcement");
            var medicalContainer = world.InventoryContainers.Find(item =>
                item.Id == army.MedicalInventoryContainerId);
            medicalContainer.CapacityWeight =
                ArmyMedicineQuantity(world, army) + 2;
            StartYouzhouArmyToAnping(world);
            var order = new MilitaryMedicalResupplySystem().Dispatch(
                world, MedicalResupplyRequest(10, 4, false));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 18);
            new MilitaryLogisticsSystem().ResolveArrivals(world);

            var delivered = new MilitaryLogisticsSystem().DeliverPartial(
                world, order.Id, 10);
            var afterFirst = WorldSnapshotSerializer.Serialize(world);

            Assert.That(delivered, Is.EqualTo(2));
            Assert.That(order.Status,
                Is.EqualTo(MilitaryLogisticsStatus.AwaitingArmy));
            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryLogisticsSystem().DeliverPartial(
                    world, order.Id, 1));
            Assert.That(
                WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(afterFirst));
            world.Validate();
        }

        [Test]
        public void MilitaryMedicalResupply_RejectsNonMedicineWithoutMutation()
        {
            var world = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var before = WorldSnapshotSerializer.Serialize(world);
            var request = MedicalResupplyRequest(10, 4, true);
            request.SourceMedicineBatchId = new StableId(
                "product_batch.logistics.merchant_cargo");

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryMedicalResupplySystem().Dispatch(
                    world, request));
            Assert.That(
                WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void MilitaryMedicalResupply_TamperedDestinationIsRejected()
        {
            var world = PrepareMerchantLogisticsWorld();
            AddMilitaryMedicalLogisticsBatch(world, 20);
            StartYouzhouArmyToAnping(world);
            var order = new MilitaryMedicalResupplySystem().Dispatch(
                world, MedicalResupplyRequest(10, 4, false));
            var otherArmy = world.Armies.Find(item =>
                item.Id != order.TargetArmyId);
            order.TargetInventoryContainerId =
                otherArmy.MedicalInventoryContainerId;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFortySevenToLegacyProvisionDelivery()
        {
            var world = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            new MilitaryLogisticsSystem().Dispatch(
                world,
                MerchantLogisticsRequest(
                    MilitarySupplyAcquisitionMethodIds.CommercialPurchase,
                    3));
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace(
                    "\"SchemaVersion\": " +
                        WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 47")
                .Replace(
                    "\"DeliveryPolicyId\": \"" +
                        MilitaryLogisticsDeliveryPolicyIds.ArmyProvisions +
                        "\",",
                    "\"DeliveryPolicyId\": null,")
                .Replace(
                    "\"TargetInventoryContainerId\": \"\"",
                    "\"TargetInventoryContainerId\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryLogisticsOrders[0].DeliveryPolicyId,
                Is.EqualTo(
                    MilitaryLogisticsDeliveryPolicyIds.ArmyProvisions));
            Assert.That(loaded.MilitaryLogisticsOrders[0]
                .TargetInventoryContainerId, Is.Empty);
            Assert.That(loaded.InventoryTransactions.Exists(item =>
                item.Type == InventoryTransactionType
                    .MilitaryLogisticsDelivered), Is.False);
            loaded.Validate();
        }

        [Test]
        public void MilitaryMedicalEvacuation_DispatchTravelsAndReceivesWithoutHealing()
        {
            var world = BuildMilitaryEvacuationWorld(
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver);
            var patient = world.People.Find(item =>
                item.Id == patientService.PersonId);
            var healthBefore = patient.HealthBasisPoints;
            var troopsBefore = army.Troops;

            var evacuation = new MilitaryMedicalEvacuationSystem().Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(patientService.Id),
                teamServices.ConvertAll(item => new StableId(item.Id)),
                new StableId("route.zhongshan_anping"),
                new StableId("location.anping"),
                new StableId(receiver.Id));

            Assert.That(world.Journeys.Count(item =>
                item.Id == evacuation.PatientJourneyId ||
                evacuation.TeamMembers.Exists(member =>
                    member.JourneyId == item.Id)), Is.EqualTo(3));
            Assert.That(army.Troops, Is.EqualTo(troopsBefore - 2));
            Assert.That(army.WoundedTroops, Is.EqualTo(1));
            Assert.That(teamServices.TrueForAll(item =>
                item.Status ==
                    MilitaryServiceStatus.MedicalEvacuationDuty), Is.True);

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 13);

            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.AwaitingReception));
            Assert.That(patient.LocationId, Is.EqualTo("location.anping"));
            Assert.That(teamServices.TrueForAll(service =>
                world.People.Find(item => item.Id == service.PersonId)
                    .LocationId == "location.anping"), Is.True);
            new MilitaryMedicalEvacuationSystem().Receive(
                world,
                new StableId(evacuation.Id),
                new StableId(receiver.Id));

            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.Received));
            Assert.That(evacuation.ReceivingPersonId, Is.EqualTo(receiver.Id));
            Assert.That(patient.HealthBasisPoints, Is.EqualTo(healthBefore));
            Assert.That(patientService.Status,
                Is.EqualTo(MilitaryServiceStatus.Wounded));
            Assert.That(world.MilitaryMedicalServices, Is.Empty);
            var receivedSnapshot = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() =>
                new TravelSystem().StartJourney(
                    world,
                    new StableId(patient.Id),
                    new StableId("route.anping_xiaquyang"),
                    new StableId("location.xiaquyang"),
                    TravelMode.Foot));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(receivedSnapshot));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryMedicalEvacuations.Count,
                Is.EqualTo(1));
            Assert.That(loaded.MilitaryMedicalEvacuations[0].Status,
                Is.EqualTo(MilitaryMedicalEvacuationStatus.Received));
            loaded.Validate();
        }

        [Test]
        public void MilitaryMedicalEvacuation_ArmyMarchDoesNotTeleportDetachedPeople()
        {
            var world = BuildMilitaryEvacuationWorld(
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver);
            var evacuation = new MilitaryMedicalEvacuationSystem().Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(patientService.Id),
                teamServices.ConvertAll(item => new StableId(item.Id)),
                new StableId("route.zhongshan_anping"),
                new StableId("location.anping"),
                new StableId(receiver.Id));
            new ArmySystem().StartMarch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(army.Id),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhuo"));

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 28);

            Assert.That(army.LocationId, Is.EqualTo("location.zhuo"));
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.AwaitingReception));
            Assert.That(world.People.Find(item =>
                item.Id == patientService.PersonId).LocationId,
                Is.EqualTo("location.anping"));
            Assert.That(teamServices.TrueForAll(service =>
                world.People.Find(item => item.Id == service.PersonId)
                    .LocationId == "location.anping"), Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryMedicalEvacuation_SourceArmyCannotTreatPatientRemotely()
        {
            var world = BuildMilitaryEvacuationWorld(
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver);
            var physicianService = world.MilitaryServices.Find(item =>
                item.ArmyId == army.Id &&
                item.Role == MilitaryServiceRole.Medic &&
                item.Status == MilitaryServiceStatus.Active);
            var physician = world.People.Find(item =>
                item.Id == physicianService.PersonId);
            physician.MedicalSkillBasisPoints = 7_500;
            physician.ProfessionalSkills.Medicine = 7_500;
            new MilitaryMedicalEvacuationSystem().Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(patientService.Id),
                teamServices.ConvertAll(item => new StableId(item.Id)),
                new StableId("route.zhongshan_anping"),
                new StableId("location.anping"),
                new StableId(receiver.Id));
            var before = WorldSnapshotSerializer.Serialize(world);

            var result = new MedicalSystem(world.MasterSeed).TreatArmyWounded(
                world,
                new StableId(physician.Id),
                new StableId(army.Id),
                1);

            Assert.That(result.Success, Is.False);
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void MilitaryMedicalEvacuation_InvalidTeamIsRejectedAtomically()
        {
            var world = BuildMilitaryEvacuationWorld(
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryMedicalEvacuationSystem().Dispatch(
                    world,
                    new StableId(army.CommanderPersonId),
                    new StableId(patientService.Id),
                    new List<StableId>
                    {
                        new StableId(teamServices[0].Id),
                        new StableId(teamServices[0].Id)
                    },
                    new StableId("route.zhongshan_anping"),
                    new StableId("location.anping"),
                    new StableId(receiver.Id)));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void Snapshot_MigratesVersionFortyEightWithoutFabricatingEvacuation()
        {
            var world = PrototypeWorldFactory.Create184World(184_049);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 48");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryMedicalEvacuations, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void MilitaryMedicalEvacuation_TamperedJourneyIsRejected()
        {
            var world = BuildMilitaryEvacuationWorld(
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver);
            var evacuation = new MilitaryMedicalEvacuationSystem().Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(patientService.Id),
                teamServices.ConvertAll(item => new StableId(item.Id)),
                new StableId("route.zhongshan_anping"),
                new StableId("location.anping"),
                new StableId(receiver.Id));
            world.Journeys.Find(item =>
                item.Id == evacuation.PatientJourneyId).RouteId =
                "route.anping_xiaquyang";

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void MilitaryRearMedicalCare_TreatsReturnsAndRejoinsWithoutTeleporting()
        {
            var world = BuildRearMedicalWorld(
                1,
                5,
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver,
                out var site);
            var troopsBeforeDispatch = army.Troops;
            var medicineBefore = world.ProductBatches.Find(item =>
                item.InventoryContainerId == site.MedicineInventoryContainerId)
                .Quantity;
            var evacuation = DispatchAndReceiveEvacuation(
                world, army, patientService, teamServices, receiver);
            world.MilitarySurgeryContractActivationDay = checked(
                world.AbsoluteDay + 1);
            var rear = new MilitaryRearMedicalSystem();
            var admission = rear.Admit(
                world,
                new StableId(evacuation.Id),
                new StableId(site.Id),
                new StableId(receiver.Id));

            var treatment = rear.TreatInpatient(
                world, new StableId(admission.Id));

            Assert.That(treatment.MedicineUnitsConsumed, Is.EqualTo(1));
            Assert.That(world.ProductBatches.Find(item =>
                item.Id == treatment.SourceMedicineBatchId).Quantity,
                Is.EqualTo(medicineBefore - 1));
            Assert.That(patientService.Status,
                Is.EqualTo(MilitaryServiceStatus.Wounded));
            Assert.That(world.People.Find(item =>
                item.Id == patientService.PersonId).HealthBasisPoints,
                Is.EqualTo(MilitaryMedicalRules.ReturnToDutyHealthBasisPoints));
            Assert.That(army.WoundedTroops, Is.EqualTo(1));

            var readySnapshot = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() => rear.StartReturn(
                world,
                new StableId(evacuation.Id),
                new StableId("route.anping_xiaquyang")));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(readySnapshot));
            rear.StartReturn(
                world,
                new StableId(evacuation.Id),
                new StableId("route.zhongshan_anping"));
            var returningSnapshot = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() =>
                new ArmySystem().StartMarch(
                    world,
                    new StableId(army.CommanderPersonId),
                    new StableId(army.Id),
                    new StableId("route.zhuo_zhongshan"),
                    new StableId("location.zhuo")));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(returningSnapshot));

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 13);

            Assert.That(evacuation.Status,
                Is.EqualTo(MilitaryMedicalEvacuationStatus.Completed));
            Assert.That(admission.Status,
                Is.EqualTo(MilitaryRearMedicalAdmissionStatus.Completed));
            Assert.That(patientService.Status,
                Is.EqualTo(MilitaryServiceStatus.Active));
            Assert.That(teamServices.TrueForAll(item =>
                item.Status == MilitaryServiceStatus.Active), Is.True);
            Assert.That(army.WoundedTroops, Is.Zero);
            Assert.That(army.Troops, Is.EqualTo(troopsBeforeDispatch + 1));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryRearMedicalSites.Count, Is.EqualTo(1));
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0].Status,
                Is.EqualTo(MilitaryRearMedicalAdmissionStatus.Completed));
            Assert.That(loaded.MilitaryMedicalEvacuations[0].Status,
                Is.EqualTo(MilitaryMedicalEvacuationStatus.Completed));
            loaded.Validate();

            Assert.DoesNotThrow(() => new TravelSystem().StartJourney(
                world,
                new StableId(patientService.PersonId),
                new StableId("route.zhongshan_anping"),
                new StableId("location.anping"),
                TravelMode.Foot));
        }

        [Test]
        public void MilitaryRearMedicalCare_BedCapacityRejectsSecondAdmissionAtomically()
        {
            var world = BuildRearMedicalWorld(
                1,
                5,
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver,
                out var site);
            var first = DispatchAndReceiveEvacuation(
                world, army, patientService, teamServices, receiver);
            var rear = new MilitaryRearMedicalSystem();
            rear.Admit(
                world,
                new StableId(first.Id),
                new StableId(site.Id),
                new StableId(receiver.Id));

            var eligible = world.MilitaryServices.FindAll(item =>
                item.ArmyId == army.Id &&
                item.Role == MilitaryServiceRole.Soldier &&
                item.Status == MilitaryServiceStatus.Active);
            eligible.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var secondPatient = eligible[0];
            secondPatient.Status = MilitaryServiceStatus.Wounded;
            secondPatient.LastStatusChangeDay = world.AbsoluteDay;
            world.People.Find(item =>
                item.Id == secondPatient.PersonId).HealthBasisPoints = 4_000;
            var secondTeam = new List<MilitaryServiceState>
            {
                eligible[1],
                eligible[2]
            };
            new MilitaryServiceSystem().SynchronizeArmyCaches(world, army.Id);
            world.Validate();
            var second = DispatchAndReceiveEvacuation(
                world, army, secondPatient, secondTeam, receiver);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() => rear.Admit(
                world,
                new StableId(second.Id),
                new StableId(site.Id),
                new StableId(receiver.Id)));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void MilitaryRearMedicalCare_MissingMedicineRejectsTreatmentAtomically()
        {
            var world = BuildRearMedicalWorld(
                1,
                0,
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver,
                out var site);
            var evacuation = DispatchAndReceiveEvacuation(
                world, army, patientService, teamServices, receiver);
            world.MilitarySurgeryContractActivationDay = checked(
                world.AbsoluteDay + 1);
            var rear = new MilitaryRearMedicalSystem();
            var admission = rear.Admit(
                world,
                new StableId(evacuation.Id),
                new StableId(site.Id),
                new StableId(receiver.Id));
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                rear.TreatInpatient(world, new StableId(admission.Id)));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void MilitaryRearMedicalCare_TamperedTreatmentSourceIsRejected()
        {
            var world = BuildRearMedicalWorld(
                1,
                5,
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver,
                out var site);
            var evacuation = DispatchAndReceiveEvacuation(
                world, army, patientService, teamServices, receiver);
            var rear = new MilitaryRearMedicalSystem();
            var admission = rear.Admit(
                world,
                new StableId(evacuation.Id),
                new StableId(site.Id),
                new StableId(receiver.Id));
            var treatment = rear.TreatInpatient(
                world, new StableId(admission.Id));
            world.InventoryTransactions.Find(item =>
                item.Id == treatment.InventoryTransactionId)
                .SourceMilitaryRearMedicalTreatmentId = string.Empty;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFortyNineWithoutFabricatingRearCare()
        {
            var world = BuildMilitaryEvacuationWorld(
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver);
            var evacuation = DispatchAndReceiveEvacuation(
                world, army, patientService, teamServices, receiver);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 49");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryRearMedicalSites, Is.Empty);
            Assert.That(loaded.MilitaryRearMedicalAdmissions, Is.Empty);
            Assert.That(loaded.MilitaryRearMedicalTreatments, Is.Empty);
            Assert.That(loaded.MilitaryMedicalEvacuations[0].Id,
                Is.EqualTo(evacuation.Id));
            Assert.That(loaded.MilitaryMedicalEvacuations[0]
                .ReturnStartedDay, Is.EqualTo(-1));
            loaded.Validate();
        }

        [Test]
        public void MilitaryFieldHospital_ConstructionConsumesFormalResourcesAndLabor()
        {
            var world = BuildFieldHospitalWorld(
                out var army,
                out var project,
                out var site,
                out var materialContainer);

            Assert.That(project.Status,
                Is.EqualTo(MilitaryFieldHospitalConstructionStatus.Completed));
            Assert.That(project.CompletedLaborDays,
                Is.EqualTo(MilitaryMedicalRules.FieldHospitalRequiredLaborDays));
            Assert.That(site.KindId,
                Is.EqualTo(MilitaryRearMedicalSiteKindIds.FieldHospital));
            Assert.That(site.LocationId, Is.EqualTo(army.LocationId));
            Assert.That(site.SupportInventoryContainerId,
                Is.EqualTo(materialContainer.Id));
            Assert.That(world.Locations.Find(item =>
                item.Id == site.LocationId).Features & LocationFeature.Clinic,
                Is.EqualTo(LocationFeature.None));
            Assert.That(world.ProductBatches.Find(item =>
                item.Id == "product_batch.field_hospital.timber").Quantity,
                Is.EqualTo(10));
            Assert.That(world.ProductBatches.Find(item =>
                item.Id == "product_batch.field_hospital.leather").Quantity,
                Is.EqualTo(5));
            Assert.That(world.MilitaryFieldHospitalConstructionWork.Count,
                Is.EqualTo(3));
            Assert.That(world.InventoryTransactions.Find(item =>
                    item.Id == project.InventoryTransactionId).Type,
                Is.EqualTo(InventoryTransactionType
                    .MilitaryFieldHospitalConstructionConsumed));
            world.Validate();
        }

        [Test]
        public void MilitaryFieldHospital_OverdueMaintenanceDisablesAndRestoresSite()
        {
            var world = BuildFieldHospitalWorld(
                out var army,
                out var project,
                out var site,
                out var materialContainer);
            var organization = world.Organizations.Find(item =>
                item.Id == army.OrganizationId);
            var treasuryBefore = organization.Treasury;
            var timberBefore = world.ProductBatches.Find(item =>
                item.Id == "product_batch.field_hospital.timber").Quantity;

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 11);

            Assert.That(world.AbsoluteDay, Is.GreaterThan(site.NextMaintenanceDay));
            Assert.That(site.IsOperational, Is.False);
            var maintenance = new MilitaryFieldHospitalSystem().Maintain(
                world,
                new StableId(site.Id),
                new StableId(army.CommanderPersonId));

            Assert.That(site.IsOperational, Is.True);
            Assert.That(site.LastMaintenanceDay, Is.EqualTo(world.AbsoluteDay));
            Assert.That(site.NextMaintenanceDay, Is.EqualTo(
                world.AbsoluteDay +
                MilitaryMedicalRules.FieldHospitalMaintenanceIntervalDays));
            Assert.That(organization.Treasury, Is.EqualTo(
                treasuryBefore -
                MilitaryMedicalRules.FieldHospitalMaintenanceMoney));
            Assert.That(world.ProductBatches.Find(item =>
                    item.Id == maintenance.SourceTimberBatchId).Quantity,
                Is.EqualTo(
                    timberBefore -
                    MilitaryMedicalRules.FieldHospitalMaintenanceTimberUnits));
            Assert.That(world.InventoryTransactions.Find(item =>
                    item.Id == maintenance.InventoryTransactionId).Type,
                Is.EqualTo(InventoryTransactionType
                    .MilitaryFieldHospitalMaintenanceConsumed));
            world.Validate();
        }

        [Test]
        public void MilitaryFieldHospital_RequiresStabilizationThenRecovery()
        {
            var world = BuildFieldHospitalWorld(
                out var army,
                out var project,
                out var site,
                out var materialContainer);
            AddOrganizationProductBatch(
                world,
                "product_batch.field_hospital.medicine",
                site.OwnerOrganizationId,
                site.MedicineInventoryContainerId,
                site.LocationId,
                CoreProductionContent.HerbalMedicineMaterialProductId,
                3,
                army.CommanderPersonId);
            RelocateArmyForFieldHospitalTest(
                world, army, "location.zhuo");
            var eligible = world.MilitaryServices.FindAll(item =>
                item.ArmyId == army.Id &&
                item.Role == MilitaryServiceRole.Soldier &&
                item.Status == MilitaryServiceStatus.Active);
            eligible.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var patient = eligible[0];
            patient.Status = MilitaryServiceStatus.Wounded;
            patient.LastStatusChangeDay = world.AbsoluteDay;
            world.People.Find(item => item.Id == patient.PersonId)
                .HealthBasisPoints = 4_000;
            var team = new List<MilitaryServiceState>
            {
                eligible[1],
                eligible[2]
            };
            new MilitaryServiceSystem().SynchronizeArmyCaches(world, army.Id);
            var receiver = world.People.Find(item =>
                item.Id == "person.generated.physician_001");
            receiver.MedicalSkillBasisPoints = 7_500;
            receiver.ProfessionalSkills.Medicine = 7_500;
            new PopulationLedgerSystem().MoveIndependentPerson(
                world, receiver, site.LocationId);
            var evacuationSystem = new MilitaryMedicalEvacuationSystem();
            var evacuation = evacuationSystem.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(patient.Id),
                team.ConvertAll(item => new StableId(item.Id)),
                new StableId("route.zhuo_zhongshan"),
                new StableId(site.LocationId),
                new StableId(receiver.Id));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 28);
            evacuationSystem.Receive(
                world,
                new StableId(evacuation.Id),
                new StableId(receiver.Id));
            world.MilitarySurgeryContractActivationDay = checked(
                world.AbsoluteDay + 1);
            var rear = new MilitaryRearMedicalSystem();
            var admission = rear.Admit(
                world,
                new StableId(evacuation.Id),
                new StableId(site.Id),
                new StableId(receiver.Id));

            var stabilization = rear.TreatInpatient(
                world, new StableId(admission.Id));

            Assert.That(stabilization.StageIndex, Is.Zero);
            Assert.That(stabilization.TreatmentProtocolId,
                Is.EqualTo(MilitaryRearMedicalTreatmentProtocolIds
                    .FieldStabilization));
            Assert.That(admission.Status,
                Is.EqualTo(MilitaryRearMedicalAdmissionStatus.InTreatment));
            Assert.That(evacuation.Status,
                Is.EqualTo(MilitaryMedicalEvacuationStatus.Admitted));
            Assert.That(world.People.Find(item =>
                    item.Id == patient.PersonId).HealthBasisPoints,
                Is.EqualTo(MilitaryMedicalRules
                    .FieldStabilizationHealthBasisPoints));

            var recovery = rear.TreatInpatient(
                world, new StableId(admission.Id));

            Assert.That(recovery.StageIndex, Is.EqualTo(1));
            Assert.That(recovery.TreatmentProtocolId,
                Is.EqualTo(MilitaryRearMedicalTreatmentProtocolIds
                    .FieldRecovery));
            Assert.That(admission.CompletedTreatmentStages, Is.EqualTo(2));
            Assert.That(admission.TreatmentIds,
                Is.EqualTo(new[] { stabilization.Id, recovery.Id }));
            Assert.That(admission.Status,
                Is.EqualTo(MilitaryRearMedicalAdmissionStatus.ReadyForReturn));
            Assert.That(evacuation.Status,
                Is.EqualTo(MilitaryMedicalEvacuationStatus.ReadyForReturn));
            Assert.That(world.People.Find(item =>
                    item.Id == patient.PersonId).HealthBasisPoints,
                Is.EqualTo(MilitaryMedicalRules.ReturnToDutyHealthBasisPoints));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0]
                .CompletedTreatmentStages, Is.EqualTo(2));
            loaded.Validate();
        }

        [Test]
        public void MilitaryFieldHospital_TamperedConstructionSourceIsRejected()
        {
            var world = BuildFieldHospitalWorld(
                out var army,
                out var project,
                out var site,
                out var materialContainer);
            world.InventoryTransactions.Find(item =>
                    item.Id == project.InventoryTransactionId)
                .SourceMilitaryFieldHospitalConstructionProjectId =
                    string.Empty;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFiftyWithoutFabricatingFieldHospitals()
        {
            var world = BuildRearMedicalWorld(
                1,
                3,
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver,
                out var site);
            var evacuation = DispatchAndReceiveEvacuation(
                world, army, patientService, teamServices, receiver);
            world.MilitarySurgeryContractActivationDay = checked(
                world.AbsoluteDay + 1);
            var rear = new MilitaryRearMedicalSystem();
            var admission = rear.Admit(
                world,
                new StableId(evacuation.Id),
                new StableId(site.Id),
                new StableId(receiver.Id));
            var treatment = rear.TreatInpatient(
                world, new StableId(admission.Id));
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 50");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryFieldHospitalConstructionProjects,
                Is.Empty);
            Assert.That(loaded.MilitaryFieldHospitalConstructionWork, Is.Empty);
            Assert.That(loaded.MilitaryFieldHospitalMaintenance, Is.Empty);
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0]
                .RequiredTreatmentStages, Is.EqualTo(1));
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0].TreatmentIds,
                Is.EqualTo(new[] { treatment.Id }));
            loaded.Validate();
        }

        [Test]
        public void ComplexMilitaryInjury_InfectionAddsFrozenControlStage()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                8,
                out var admission,
                out var injury,
                out var site);
            var medicine = world.ProductBatches.Find(item =>
                item.InventoryContainerId == site.MedicineInventoryContainerId);
            var medicineBefore = medicine.Quantity;
            var rear = new MilitaryRearMedicalSystem();

            var stabilization = rear.TreatInpatient(
                world, new StableId(admission.Id));
            var surgery = rear.TreatInpatient(
                world, new StableId(admission.Id));
            var infectionControl = rear.TreatInpatient(
                world, new StableId(admission.Id));

            Assert.That(injury.InjuryProfileId,
                Is.EqualTo(MilitaryInjuryProfileIds.Penetrating));
            Assert.That(injury.InfectionRiskBasisPoints,
                Is.GreaterThanOrEqualTo(
                    MilitaryMedicalRules.InfectionRiskThresholdBasisPoints));
            Assert.That(admission.TreatmentPlanProtocolIds,
                Is.EqualTo(new[]
                {
                    MilitaryRearMedicalTreatmentProtocolIds
                        .FieldStabilization,
                    MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery,
                    MilitaryRearMedicalTreatmentProtocolIds.InfectionControl,
                    MilitaryRearMedicalTreatmentProtocolIds.FieldRecovery
                }));
            Assert.That(stabilization.StageIndex, Is.Zero);
            Assert.That(surgery.StageIndex, Is.EqualTo(1));
            Assert.That(infectionControl.StageIndex, Is.EqualTo(2));
            Assert.That(infectionControl.MedicineUnitsConsumed,
                Is.EqualTo(MilitaryMedicalRules
                    .InfectionControlMedicineUnits));
            Assert.That(infectionControl.WorkMinutes,
                Is.EqualTo(MilitaryMedicalRules
                    .InfectionControlWorkMinutes));
            Assert.That(injury.InfectionStatus,
                Is.EqualTo(MilitaryInfectionStatus.Controlled));
            Assert.That(injury.InfectionControlTreatmentId,
                Is.EqualTo(infectionControl.Id));
            Assert.That(admission.Status,
                Is.EqualTo(MilitaryRearMedicalAdmissionStatus.InTreatment));

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            var recovery = rear.TreatInpatient(
                world, new StableId(admission.Id));

            Assert.That(recovery.StageIndex, Is.EqualTo(3));
            Assert.That(admission.Status,
                Is.EqualTo(MilitaryRearMedicalAdmissionStatus.ReadyForReturn));
            Assert.That(admission.DischargePolicyId, Is.EqualTo(
                MilitaryRearMedicalDischargePolicyIds
                    .MedicalRetirementAtCareSite));
            Assert.That(injury.PermanentOutcomeId, Is.EqualTo(
                MilitaryInjuryOutcomeIds.PermanentMobilityImpairment));
            Assert.That(medicine.Quantity, Is.EqualTo(medicineBefore - 7));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryInjuryEpisodes[0].InfectionStatus,
                Is.EqualTo(MilitaryInfectionStatus.Controlled));
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0]
                .TreatmentPlanProtocolIds, Is.EqualTo(
                    admission.TreatmentPlanProtocolIds));
            loaded.Validate();
        }

        [Test]
        public void ComplexMilitaryInjury_InsufficientInfectionMedicineIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                5,
                out var admission,
                out var injury,
                out var site);
            var rear = new MilitaryRearMedicalSystem();
            rear.TreatInpatient(world, new StableId(admission.Id));
            rear.TreatInpatient(world, new StableId(admission.Id));
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                rear.TreatInpatient(world, new StableId(admission.Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            Assert.That(injury.InfectionStatus,
                Is.EqualTo(MilitaryInfectionStatus.Active));
        }

        [Test]
        public void ComplexMilitaryInjury_TamperedFrozenPlanIsRejected()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                6,
                out var admission,
                out var injury,
                out var site);
            admission.TreatmentPlanProtocolIds[2] =
                MilitaryRearMedicalTreatmentProtocolIds.FieldRecovery;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void ComplexMilitaryInjury_DataProfileNeedsNoSchemaChange()
        {
            var schemaBefore = WorldState.CurrentSchemaVersion;
            var world = BuildInfectedFieldHospitalAdmission(
                6,
                out var admission,
                out var injury,
                out var site,
                new MilitaryInjuryProfileDefinitionState
                {
                    Id = "mod.example.injury_profile.arrow_wound",
                    DisplayName = "箭创",
                    MinimumAdmissionHealthBasisPoints = 0,
                    MaximumAdmissionHealthBasisPoints = 2_500,
                    SelectionPriority = 200
                });

            Assert.That(world.SchemaVersion, Is.EqualTo(schemaBefore));
            Assert.That(injury.InjuryProfileId,
                Is.EqualTo("mod.example.injury_profile.arrow_wound"));
            world.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionFiftyOneWithoutInventingInjury()
        {
            var world = BuildRearMedicalWorld(
                1,
                3,
                out var army,
                out var patientService,
                out var teamServices,
                out var receiver,
                out var site);
            var evacuation = DispatchAndReceiveEvacuation(
                world, army, patientService, teamServices, receiver);
            world.MilitarySurgeryContractActivationDay = checked(
                world.AbsoluteDay + 1);
            var rear = new MilitaryRearMedicalSystem();
            var admission = rear.Admit(
                world,
                new StableId(evacuation.Id),
                new StableId(site.Id),
                new StableId(receiver.Id));
            rear.TreatInpatient(world, new StableId(admission.Id));
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 51");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryInjuryEpisodes, Is.Empty);
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0]
                .InjuryEpisodeId, Is.Empty);
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0]
                .TreatmentPlanProtocolIds, Is.EqualTo(new[]
                {
                    MilitaryRearMedicalTreatmentProtocolIds
                        .InpatientHerbalRecovery
                }));
            Assert.That(loaded.MilitaryInjuryContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            loaded.Validate();
        }

        [Test]
        public void TraumaSurgery_PermanentImpairmentRetiresPatientAndReturnsTeam()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var patient = world.People.Find(item =>
                item.Id == admission.PatientPersonId);
            var patientService = world.MilitaryServices.Find(item =>
                item.Id == admission.PatientMilitaryServiceId);
            var rear = new MilitaryRearMedicalSystem();

            rear.TreatInpatient(world, new StableId(admission.Id));
            var surgery = rear.TreatInpatient(
                world, new StableId(admission.Id));
            rear.TreatInpatient(world, new StableId(admission.Id));
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            rear.TreatInpatient(world, new StableId(admission.Id));

            Assert.That(surgery.TreatmentProtocolId, Is.EqualTo(
                MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery));
            Assert.That(surgery.MedicineUnitsConsumed, Is.EqualTo(3));
            Assert.That(surgery.WorkMinutes, Is.EqualTo(240));
            Assert.That(injury.SurgeryTreatmentId, Is.EqualTo(surgery.Id));
            Assert.That(injury.PermanentOutcomeId, Is.EqualTo(
                MilitaryInjuryOutcomeIds.PermanentMobilityImpairment));
            Assert.That(injury.LaborCapacityBeforeBasisPoints,
                Is.EqualTo(10_000));
            Assert.That(injury.LaborCapacityAfterBasisPoints,
                Is.EqualTo(7_000));
            Assert.That(patient.LaborCapacityBasisPoints, Is.EqualTo(7_000));
            Assert.That(patient.PermanentLaborCapacityPenaltyBasisPoints,
                Is.EqualTo(3_000));

            rear.StartReturn(
                world,
                new StableId(evacuation.Id),
                new StableId("route.zhuo_zhongshan"));
            Assert.That(evacuation.PatientReturnJourneyId, Is.Empty);
            Assert.That(patient.LocationId, Is.EqualTo(site.LocationId));

            var simulator = new WorldSimulator(world.MasterSeed);
            for (var i = 0;
                 i < 40 && evacuation.Status !=
                     MilitaryMedicalEvacuationStatus.Completed;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }

            Assert.That(evacuation.Status,
                Is.EqualTo(MilitaryMedicalEvacuationStatus.Completed));
            Assert.That(patientService.Status,
                Is.EqualTo(MilitaryServiceStatus.Retired));
            Assert.That(patient.LocationId, Is.EqualTo(site.LocationId));
            for (var i = 0; i < evacuation.TeamMembers.Count; i++)
            {
                var memberService = world.MilitaryServices.Find(item =>
                    item.Id == evacuation.TeamMembers[i].MilitaryServiceId);
                Assert.That(memberService.Status,
                    Is.EqualTo(MilitaryServiceStatus.Active));
            }
            world.Validate();
        }

        [Test]
        public void TraumaSurgery_UnqualifiedPhysicianIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            var rear = new MilitaryRearMedicalSystem();
            rear.TreatInpatient(world, new StableId(admission.Id));
            var physician = world.People.Find(item =>
                item.Id == admission.PhysicianPersonId);
            physician.MedicalSkillBasisPoints = 4_500;
            physician.ProfessionalSkills.Medicine = 4_500;
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                rear.TreatInpatient(world, new StableId(admission.Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            Assert.That(injury.SurgeryTreatmentId, Is.Empty);
        }

        [Test]
        public void TraumaSurgery_TamperedPermanentOutcomeIsRejected()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            var rear = new MilitaryRearMedicalSystem();
            rear.TreatInpatient(world, new StableId(admission.Id));
            rear.TreatInpatient(world, new StableId(admission.Id));
            injury.PermanentLaborCapacityPenaltyBasisPoints = 2_999;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void TraumaSurgery_DataProcedureNeedsNoSchemaChange()
        {
            var schemaBefore = WorldState.CurrentSchemaVersion;
            var procedure = new MilitarySurgicalProcedureDefinitionState
            {
                Id = "mod.example.surgical_procedure.arrow_extraction",
                DisplayName = "箭镞取出",
                MinimumSeverityBasisPoints = 5_000,
                MinimumPhysicianSkillBasisPoints = 5_500,
                WorkMinutes = 200,
                MedicineUnits = 2,
                TargetHealthBasisPoints = 4_800,
                PermanentImpairmentSeverityBasisPoints = 8_500,
                PermanentImpairmentLaborPenaltyBasisPoints = 2_500
            };
            var profile = new MilitaryInjuryProfileDefinitionState
            {
                Id = "mod.example.injury_profile.arrow_wound.surgical",
                DisplayName = "箭创",
                MinimumAdmissionHealthBasisPoints = 0,
                MaximumAdmissionHealthBasisPoints = 2_500,
                SelectionPriority = 300,
                SurgicalProcedureId = procedure.Id
            };
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site,
                profile,
                procedure);

            Assert.That(world.SchemaVersion, Is.EqualTo(schemaBefore));
            Assert.That(injury.InjuryProfileId, Is.EqualTo(profile.Id));
            Assert.That(injury.SurgicalProcedureId, Is.EqualTo(procedure.Id));
            Assert.That(admission.TreatmentPlanProtocolIds, Does.Contain(
                MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery));
            world.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionFiftyTwoWithoutInventingSurgery()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            world.MilitarySurgeryContractActivationDay = checked(
                world.AbsoluteDay + 1);
            injury.SurgicalProcedureId = string.Empty;
            admission.TreatmentPlanProtocolIds.RemoveAt(1);
            admission.RequiredTreatmentStages--;
            world.Validate();
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 52");

            var loaded = WorldSnapshotSerializer.Deserialize(json);
            var loadedInjury = loaded.MilitaryInjuryEpisodes[0];

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitarySurgicalProcedures, Has.Count.EqualTo(1));
            Assert.That(loadedInjury.SurgicalProcedureId, Is.Empty);
            Assert.That(loadedInjury.SurgeryTreatmentId, Is.Empty);
            Assert.That(loadedInjury.PermanentOutcomeId, Is.Empty);
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0]
                .TreatmentPlanProtocolIds, Does.Not.Contain(
                    MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery));
            Assert.That(loaded.MilitaryMedicalEvacuations[0]
                .PatientReturnPolicyId, Is.EqualTo(
                    MilitaryMedicalEvacuationPatientReturnPolicyIds
                        .ReturnWithTeam));
            Assert.That(loaded.MilitarySurgeryContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            loaded.Validate();
        }

        [Test]
        public void MedicalTransfer_ReservesBedMedicineAndContinuesFrozenCare()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 10,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var frozenPlan = new List<string>(
                admission.TreatmentPlanProtocolIds);
            var batch = world.ProductBatches.Find(item =>
                item.InventoryContainerId ==
                    destinationSite.MedicineInventoryContainerId &&
                item.ProductDefinitionId == CoreProductionContent
                    .HerbalMedicineMaterialProductId);
            var openingQuantity = batch.Quantity;
            var transferSystem = new MilitaryMedicalTransferSystem();

            var transfer = transferSystem.Dispatch(
                world,
                new StableId(world.Armies.Find(item =>
                    item.Id == evacuation.SourceArmyId).CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));

            Assert.That(transfer.ReservedMedicineUnits, Is.EqualTo(7));
            Assert.That(batch.ReservedQuantity, Is.EqualTo(7));
            Assert.That(world.Journeys.FindAll(item =>
                item.Id == transfer.PatientJourneyId ||
                transfer.TeamMembers.Exists(member =>
                    member.JourneyId == item.Id)), Has.Count.EqualTo(3));
            Assert.That(admission.RearMedicalSiteId,
                Is.EqualTo(sourceSite.Id));
            Assert.That(evacuation.CurrentCareLocationId,
                Is.EqualTo(sourceSite.LocationId));

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 20);
            Assert.That(transfer.Status,
                Is.EqualTo(MilitaryMedicalTransferStatus.AwaitingReception));
            transferSystem.Receive(
                world,
                new StableId(transfer.Id),
                new StableId(receiver.Id));

            Assert.That(admission.RearMedicalSiteId,
                Is.EqualTo(destinationSite.Id));
            Assert.That(admission.PhysicianPersonId, Is.EqualTo(receiver.Id));
            Assert.That(evacuation.CurrentCareLocationId,
                Is.EqualTo(destinationSite.LocationId));
            Assert.That(admission.InjuryEpisodeId, Is.EqualTo(injury.Id));
            Assert.That(admission.TreatmentPlanProtocolIds,
                Is.EqualTo(frozenPlan));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var loadedAdmission = loaded.MilitaryRearMedicalAdmissions.Find(
                item => item.Id == admission.Id);
            var loadedTransfer = loaded.MilitaryMedicalTransfers.Find(item =>
                item.Id == transfer.Id);
            var rear = new MilitaryRearMedicalSystem();
            rear.TreatInpatient(loaded, new StableId(loadedAdmission.Id));
            rear.TreatInpatient(loaded, new StableId(loadedAdmission.Id));
            rear.TreatInpatient(loaded, new StableId(loadedAdmission.Id));
            new WorldSimulator(loaded.MasterSeed).AdvanceDays(loaded, 1);
            rear.TreatInpatient(loaded, new StableId(loadedAdmission.Id));

            var loadedBatch = loaded.ProductBatches.Find(item =>
                item.Id == batch.Id);
            Assert.That(loadedTransfer.ConsumedReservedMedicineUnits,
                Is.EqualTo(7));
            Assert.That(loadedBatch.ReservedQuantity, Is.Zero);
            Assert.That(loadedBatch.Quantity,
                Is.EqualTo(openingQuantity - 7));
            Assert.That(loadedAdmission.Status,
                Is.EqualTo(MilitaryRearMedicalAdmissionStatus.ReadyForReturn));
            var loadedEvacuation = loaded.MilitaryMedicalEvacuations.Find(
                item => item.Id == loadedAdmission.EvacuationId);
            var loadedArmy = loaded.Armies.Find(item =>
                item.Id == loadedEvacuation.SourceArmyId);
            RelocateSourceArmyWithoutEvacuationParty(
                loaded, loadedArmy, "location.xiaquyang");
            rear.StartReturn(
                loaded,
                new StableId(loadedEvacuation.Id),
                new StableId("route.anping_xiaquyang"));
            new WorldSimulator(loaded.MasterSeed).AdvanceSegments(loaded, 20);
            Assert.That(loadedEvacuation.Status,
                Is.EqualTo(MilitaryMedicalEvacuationStatus.Completed));
            Assert.That(loaded.MilitaryServices.Find(item =>
                    item.Id == loadedAdmission.PatientMilitaryServiceId).Status,
                Is.EqualTo(MilitaryServiceStatus.Retired));
            for (var i = 0; i < loadedEvacuation.TeamMembers.Count; i++)
            {
                Assert.That(loaded.MilitaryServices.Find(item =>
                        item.Id == loadedEvacuation.TeamMembers[i]
                            .MilitaryServiceId).Status,
                    Is.EqualTo(MilitaryServiceStatus.Active));
            }
            loaded.Validate();
        }

        [Test]
        public void MedicalTransfer_InsufficientDestinationMedicineIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 6,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryMedicalTransferSystem().Dispatch(
                    world,
                    new StableId(world.Armies.Find(item =>
                        item.Id == evacuation.SourceArmyId).CommanderPersonId),
                    new StableId(admission.Id),
                    new StableId(destinationSite.Id),
                    new StableId("route.zhongshan_anping"),
                    new StableId(receiver.Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void MedicalTransfer_TamperedReservationIsRejected()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 10,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var transfer = new MilitaryMedicalTransferSystem().Dispatch(
                world,
                new StableId(world.Armies.Find(item =>
                    item.Id == evacuation.SourceArmyId).CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));
            world.InventoryTransactions.Find(item =>
                item.Id == transfer.ReservationInventoryTransactionId)
                .Lines[0].ReservedQuantityDelta--;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFiftyThreeWithoutInventingTransfer()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 53");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryMedicalTransfers, Is.Empty);
            Assert.That(loaded.MilitaryMedicalTransferContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0]
                .MedicalTransferId, Is.Empty);
            Assert.That(loaded.MilitaryMedicalEvacuations[0]
                .CurrentCareLocationId, Is.EqualTo(
                    loaded.MilitaryMedicalEvacuations[0]
                        .DestinationLocationId));
            loaded.Validate();
        }

        [Test]
        public void PostTreatmentMedicalTransfer_PreservesSourceCareAndCompletesRemainingPlan()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var sourcePhysicianId = admission.PhysicianPersonId;
            var rear = new MilitaryRearMedicalSystem();
            var sourceTreatment = rear.TreatInpatient(
                world, new StableId(admission.Id));
            var sourceTransaction = world.InventoryTransactions.Find(item =>
                item.Id == sourceTreatment.InventoryTransactionId);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 6,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var transferSystem = new MilitaryMedicalTransferSystem();

            var transfer = transferSystem.Dispatch(
                world,
                new StableId(world.Armies.Find(item =>
                    item.Id == evacuation.SourceArmyId).CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));

            Assert.That(transfer.CompletedTreatmentStagesAtDispatch,
                Is.EqualTo(1));
            Assert.That(transfer.ReservedMedicineUnits, Is.EqualTo(6));
            Assert.That(sourceTreatment.RearMedicalSiteId,
                Is.EqualTo(sourceSite.Id));
            Assert.That(sourceTreatment.PhysicianPersonId,
                Is.EqualTo(sourcePhysicianId));
            Assert.That(sourceTransaction.Lines[0].ReservedQuantityDelta,
                Is.Zero);

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 20);
            transferSystem.Receive(
                world,
                new StableId(transfer.Id),
                new StableId(receiver.Id));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var loadedAdmission = loaded.MilitaryRearMedicalAdmissions.Find(
                item => item.Id == admission.Id);
            var loadedTransfer = loaded.MilitaryMedicalTransfers.Find(item =>
                item.Id == transfer.Id);
            var loadedRear = new MilitaryRearMedicalSystem();

            loadedRear.TreatInpatient(
                loaded, new StableId(loadedAdmission.Id));
            loadedRear.TreatInpatient(
                loaded, new StableId(loadedAdmission.Id));
            new WorldSimulator(loaded.MasterSeed).AdvanceDays(loaded, 1);
            loadedRear.TreatInpatient(
                loaded, new StableId(loadedAdmission.Id));

            Assert.That(loadedAdmission.CompletedTreatmentStages,
                Is.EqualTo(loadedAdmission.RequiredTreatmentStages));
            Assert.That(loadedTransfer.ConsumedReservedMedicineUnits,
                Is.EqualTo(6));
            var loadedReservedBatch = loaded.ProductBatches.Find(item =>
                item.Id == loadedTransfer.ReservedMedicineBatchId);
            Assert.That(loadedReservedBatch.ReservedQuantity, Is.Zero);
            var priorTreatment = loaded.MilitaryRearMedicalTreatments.Find(
                item => item.AdmissionId == loadedAdmission.Id &&
                    item.StageIndex == 0);
            Assert.That(priorTreatment.RearMedicalSiteId,
                Is.EqualTo(sourceSite.Id));
            Assert.That(priorTreatment.PhysicianPersonId,
                Is.EqualTo(sourcePhysicianId));
            var destinationTreatments = loaded.MilitaryRearMedicalTreatments
                .FindAll(item => item.AdmissionId == loadedAdmission.Id &&
                    item.StageIndex >=
                        loadedTransfer.CompletedTreatmentStagesAtDispatch);
            Assert.That(destinationTreatments, Has.Count.EqualTo(3));
            Assert.That(destinationTreatments.TrueForAll(item =>
                item.RearMedicalSiteId == destinationSite.Id &&
                item.PhysicianPersonId == receiver.Id &&
                item.SourceMedicineBatchId ==
                    loadedTransfer.ReservedMedicineBatchId), Is.True);
            loaded.Validate();
        }

        [Test]
        public void PostTreatmentMedicalTransfer_TransitDeathKeepsPriorCareAndReleasesRemainingReservation()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var sourceTreatment = new MilitaryRearMedicalSystem()
                .TreatInpatient(world, new StableId(admission.Id));
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 6,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            var transfer = new MilitaryMedicalTransferSystem().Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                sourceSite,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out army);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);

            new MilitaryWoundDeathSystem().ResolveMedicalTransferDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                new StableId(world
                    .MilitaryInpatientDeteriorationPolicies[0].Id));

            Assert.That(transfer.CompletedTreatmentStagesAtDispatch,
                Is.EqualTo(1));
            Assert.That(transfer.ConsumedReservedMedicineUnits, Is.Zero);
            Assert.That(transfer.ReleasedReservedMedicineUnits,
                Is.EqualTo(6));
            Assert.That(world.ProductBatches.Find(item =>
                item.Id == transfer.ReservedMedicineBatchId)
                .ReservedQuantity, Is.Zero);
            Assert.That(world.MilitaryRearMedicalTreatments.Find(item =>
                item.Id == sourceTreatment.Id).RearMedicalSiteId,
                Is.EqualTo(sourceSite.Id));
            Assert.That(world.MilitaryMedicalTransferDeathClosures[0]
                .ReleasedReservedMedicineUnits, Is.EqualTo(6));
            world.Validate();
        }

        [Test]
        public void PostTreatmentMedicalTransfer_ActivationBoundaryIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            new MilitaryRearMedicalSystem().TreatInpatient(
                world, new StableId(admission.Id));
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 6,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            world.MilitaryPostTreatmentTransferContractActivationDay =
                checked(world.AbsoluteDay + 1);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryMedicalTransferSystem().Dispatch(
                    world,
                    new StableId(world.Armies.Find(item =>
                        item.Id == evacuation.SourceArmyId).CommanderPersonId),
                    new StableId(admission.Id),
                    new StableId(destinationSite.Id),
                    new StableId("route.zhongshan_anping"),
                    new StableId(receiver.Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void PostTreatmentMedicalTransfer_InsufficientRemainingMedicineIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            new MilitaryRearMedicalSystem().TreatInpatient(
                world, new StableId(admission.Id));
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 5,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryMedicalTransferSystem().Dispatch(
                    world,
                    new StableId(world.Armies.Find(item =>
                        item.Id == evacuation.SourceArmyId).CommanderPersonId),
                    new StableId(admission.Id),
                    new StableId(destinationSite.Id),
                    new StableId("route.zhongshan_anping"),
                    new StableId(receiver.Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void PostTreatmentMedicalTransfer_TamperedDispatchStageIsRejected()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            new MilitaryRearMedicalSystem().TreatInpatient(
                world, new StableId(admission.Id));
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 6,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var transfer = new MilitaryMedicalTransferSystem().Dispatch(
                world,
                new StableId(world.Armies.Find(item =>
                    item.Id == evacuation.SourceArmyId).CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));

            transfer.CompletedTreatmentStagesAtDispatch = 0;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionSixtyTwoWithoutInventingPostTreatmentTransfer()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 7,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            new MilitaryMedicalTransferSystem().Dispatch(
                world,
                new StableId(world.Armies.Find(item =>
                    item.Id == evacuation.SourceArmyId).CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 62");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryPostTreatmentTransferContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            Assert.That(loaded.MilitaryMedicalTransfers, Has.Count.EqualTo(1));
            Assert.That(loaded.MilitaryMedicalTransfers[0]
                .CompletedTreatmentStagesAtDispatch, Is.Zero);
            loaded.Validate();
        }

        [Test]
        public void RepeatedMedicalTransfer_ClosesEachReservationAndPreservesSegmentResponsibility()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var rear = new MilitaryRearMedicalSystem();
            var sourceTreatment = rear.TreatInpatient(
                world, new StableId(admission.Id));
            var firstSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 8,
                out var firstReceiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            var transfers = new MilitaryMedicalTransferSystem();
            var first = transfers.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(firstSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(firstReceiver.Id));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 20);
            transfers.Receive(
                world, new StableId(first.Id),
                new StableId(firstReceiver.Id));
            var firstSiteTreatment = rear.TreatInpatient(
                world, new StableId(admission.Id));
            var secondSite = BuildSecondMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 5,
                out var secondReceiver);

            var second = transfers.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(secondSite.Id),
                new StableId("route.anping_xiaquyang"),
                new StableId(secondReceiver.Id));

            Assert.That(first.SequenceIndex, Is.Zero);
            Assert.That(first.NextMedicalTransferId, Is.EqualTo(second.Id));
            Assert.That(second.SequenceIndex, Is.EqualTo(1));
            Assert.That(second.PreviousMedicalTransferId, Is.EqualTo(first.Id));
            Assert.That(admission.MedicalTransferId, Is.EqualTo(second.Id));
            Assert.That(first.ConsumedReservedMedicineUnits, Is.EqualTo(3));
            Assert.That(first.ReleasedReservedMedicineUnits, Is.EqualTo(3));
            Assert.That(world.ProductBatches.Find(item =>
                item.Id == first.ReservedMedicineBatchId).ReservedQuantity,
                Is.Zero);
            Assert.That(second.ReservedMedicineUnits, Is.EqualTo(3));

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 20);
            transfers.Receive(
                world, new StableId(second.Id),
                new StableId(secondReceiver.Id));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var loadedAdmission = loaded.MilitaryRearMedicalAdmissions.Find(
                item => item.Id == admission.Id);
            var loadedRear = new MilitaryRearMedicalSystem();
            loadedRear.TreatInpatient(
                loaded, new StableId(loadedAdmission.Id));
            loadedRear.TreatInpatient(
                loaded, new StableId(loadedAdmission.Id));

            Assert.That(loadedAdmission.CompletedTreatmentStages,
                Is.EqualTo(loadedAdmission.RequiredTreatmentStages));
            var loadedSecond = loaded.MilitaryMedicalTransfers.Find(item =>
                item.Id == second.Id);
            Assert.That(loadedSecond.ConsumedReservedMedicineUnits,
                Is.EqualTo(3));
            Assert.That(loaded.MilitaryRearMedicalTreatments.Find(item =>
                item.Id == sourceTreatment.Id).RearMedicalSiteId,
                Is.EqualTo(sourceSite.Id));
            Assert.That(loaded.MilitaryRearMedicalTreatments.Find(item =>
                item.Id == firstSiteTreatment.Id).RearMedicalSiteId,
                Is.EqualTo(firstSite.Id));
            Assert.That(loaded.MilitaryRearMedicalTreatments.FindAll(item =>
                item.AdmissionId == admission.Id && item.StageIndex >= 2)
                .TrueForAll(item =>
                    item.RearMedicalSiteId == secondSite.Id &&
                    item.PhysicianPersonId == secondReceiver.Id &&
                    item.SourceMedicineBatchId ==
                        loadedSecond.ReservedMedicineBatchId), Is.True);
            loaded.Validate();
        }

        [Test]
        public void RepeatedMedicalTransfer_InsufficientNextMedicineIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var firstSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 8,
                out var firstReceiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            var transfers = new MilitaryMedicalTransferSystem();
            var first = transfers.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(firstSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(firstReceiver.Id));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 20);
            transfers.Receive(
                world, new StableId(first.Id),
                new StableId(firstReceiver.Id));
            var secondSite = BuildSecondMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 5,
                out var secondReceiver);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() => transfers.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(secondSite.Id),
                new StableId("route.anping_xiaquyang"),
                new StableId(secondReceiver.Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            Assert.That(first.NextMedicalTransferId, Is.Empty);
            Assert.That(first.ReleasedReservedMedicineUnits, Is.Zero);
        }

        [Test]
        public void RepeatedMedicalTransfer_ActivationBoundaryIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var firstSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 8,
                out var firstReceiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            var transfers = new MilitaryMedicalTransferSystem();
            var first = transfers.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(firstSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(firstReceiver.Id));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 20);
            transfers.Receive(
                world, new StableId(first.Id),
                new StableId(firstReceiver.Id));
            var secondSite = BuildSecondMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 8,
                out var secondReceiver);
            world.MilitaryRepeatedMedicalTransferContractActivationDay =
                checked(world.AbsoluteDay + 1);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() => transfers.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(secondSite.Id),
                new StableId("route.anping_xiaquyang"),
                new StableId(secondReceiver.Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void RepeatedMedicalTransfer_TamperedResponsibilityChainIsRejected()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var firstSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 8,
                out var firstReceiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            var transfers = new MilitaryMedicalTransferSystem();
            var first = transfers.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(firstSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(firstReceiver.Id));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 20);
            transfers.Receive(
                world, new StableId(first.Id),
                new StableId(firstReceiver.Id));
            var secondSite = BuildSecondMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 8,
                out var secondReceiver);
            var second = transfers.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(secondSite.Id),
                new StableId("route.anping_xiaquyang"),
                new StableId(secondReceiver.Id));

            second.PreviousMedicalTransferId = string.Empty;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void RepeatedMedicalTransfer_CurrentLegDeathPreservesEarlierCareAndClosesReservations()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var sourceTreatment = new MilitaryRearMedicalSystem()
                .TreatInpatient(world, new StableId(admission.Id));
            var firstSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 8,
                out var firstReceiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            var transfers = new MilitaryMedicalTransferSystem();
            var first = transfers.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(firstSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(firstReceiver.Id));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 20);
            transfers.Receive(
                world, new StableId(first.Id),
                new StableId(firstReceiver.Id));
            var secondSite = BuildSecondMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 8,
                out var secondReceiver);
            var second = transfers.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(secondSite.Id),
                new StableId("route.anping_xiaquyang"),
                new StableId(secondReceiver.Id));
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                firstSite,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out army);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);

            new MilitaryWoundDeathSystem().ResolveMedicalTransferDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                new StableId(world
                    .MilitaryInpatientDeteriorationPolicies[0].Id));

            Assert.That(first.ReleasedReservedMedicineUnits,
                Is.EqualTo(first.ReservedMedicineUnits));
            Assert.That(second.ReleasedReservedMedicineUnits,
                Is.EqualTo(second.ReservedMedicineUnits));
            Assert.That(world.ProductBatches.Find(item =>
                item.Id == first.ReservedMedicineBatchId).ReservedQuantity,
                Is.Zero);
            Assert.That(world.ProductBatches.Find(item =>
                item.Id == second.ReservedMedicineBatchId).ReservedQuantity,
                Is.Zero);
            Assert.That(world.MilitaryRearMedicalTreatments.Find(item =>
                item.Id == sourceTreatment.Id).RearMedicalSiteId,
                Is.EqualTo(sourceSite.Id));
            Assert.That(world.MilitaryMedicalTransferDeathClosures[0]
                .MedicalTransferId, Is.EqualTo(second.Id));
            world.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionSixtyThreeWithoutInventingRepeatedTransfer()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var firstSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 8,
                out var firstReceiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var transfer = new MilitaryMedicalTransferSystem().Dispatch(
                world,
                new StableId(world.Armies.Find(item =>
                    item.Id == evacuation.SourceArmyId).CommanderPersonId),
                new StableId(admission.Id),
                new StableId(firstSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(firstReceiver.Id));
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 63");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryMedicalTransfers, Has.Count.EqualTo(1));
            Assert.That(loaded.MilitaryMedicalTransfers[0].SequenceIndex,
                Is.Zero);
            Assert.That(loaded.MilitaryMedicalTransfers[0]
                .PreviousMedicalTransferId, Is.Empty);
            Assert.That(loaded.MilitaryMedicalTransfers[0]
                .NextMedicalTransferId, Is.Empty);
            Assert.That(loaded.MilitaryRepeatedMedicalTransferContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            loaded.Validate();
        }

        [Test]
        public void WoundDeath_ClosesPermanentPersonInheritanceAndCompensation()
        {
            var world = BuildCompletedRetiredWoundDeathWorld(
                true,
                out var admission,
                out var injury,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            var familyBefore = family.Wealth;
            var patientWealthBefore = patient.Wealth;
            var treasuryBefore = organization.Treasury;
            var populationBefore = world.PopulationTransactions.FindAll(
                item => item.Type == PopulationTransactionType.Death).Count;
            var policy = world.MilitaryWoundDeathPolicies[0];
            var expectedCompensation = policy.BaseCompensationMoney +
                policy.CompensationPerRankMoney * service.Rank;

            var death = new MilitaryWoundDeathSystem()
                .ResolvePostTreatmentDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(policy.Id));

            Assert.That(patient.IsAlive, Is.False);
            Assert.That(patient.Wealth, Is.Zero);
            Assert.That(service.Status, Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(family.HeadPersonId, Is.EqualTo(successor.Id));
            Assert.That(family.Wealth, Is.EqualTo(
                familyBefore + patientWealthBefore + expectedCompensation));
            Assert.That(organization.Treasury, Is.EqualTo(
                treasuryBefore - expectedCompensation));
            Assert.That(death.InjuryEpisodeId, Is.EqualTo(injury.Id));
            Assert.That(world.MilitaryFamilyInheritances, Has.Count.EqualTo(1));
            Assert.That(world.MilitarySurvivorCompensations,
                Has.Count.EqualTo(1));
            Assert.That(world.PopulationTransactions.FindAll(
                    item => item.Type == PopulationTransactionType.Death).Count,
                Is.EqualTo(populationBefore + 1));
            Assert.That(world.LifeEvents.Find(item =>
                item.Id == death.DeathLifeEventId), Is.Not.Null);
            Assert.That(world.LifeEvents.Find(item =>
                item.Id == death.SuccessionLifeEventId), Is.Not.Null);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryWoundDeaths, Has.Count.EqualTo(1));
            Assert.That(loaded.MilitaryFamilyInheritances,
                Has.Count.EqualTo(1));
            Assert.That(loaded.MilitarySurvivorCompensations,
                Has.Count.EqualTo(1));
            Assert.That(loaded.People.Find(item => item.Id == patient.Id)
                .IsAlive, Is.False);
            loaded.Validate();
        }

        [Test]
        public void WoundDeath_InsufficientTreasuryIsAtomic()
        {
            var world = BuildCompletedRetiredWoundDeathWorld(
                true,
                out var admission,
                out var injury,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            var policy = world.MilitaryWoundDeathPolicies[0];
            organization.Treasury = policy.BaseCompensationMoney +
                policy.CompensationPerRankMoney * service.Rank - 1;
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem().ResolvePostTreatmentDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(policy.Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void WoundDeath_WaitingPeriodIsAtomic()
        {
            var world = BuildCompletedRetiredWoundDeathWorld(
                false,
                out var admission,
                out var injury,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            var policy = world.MilitaryWoundDeathPolicies[0];
            policy.MinimumDaysAfterCareCompletion = 10;
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem().ResolvePostTreatmentDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(policy.Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void WoundDeath_TamperedCompensationIsRejected()
        {
            var world = BuildCompletedRetiredWoundDeathWorld(
                true,
                out var admission,
                out var injury,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            var policy = world.MilitaryWoundDeathPolicies[0];
            new MilitaryWoundDeathSystem().ResolvePostTreatmentDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(policy.Id));
            world.MilitarySurvivorCompensations[0].FamilyWealthAfter--;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void WoundDeath_DataPolicyNeedsNoSchemaChange()
        {
            var schemaBefore = WorldState.CurrentSchemaVersion;
            var world = BuildCompletedRetiredWoundDeathWorld(
                true,
                out var admission,
                out var injury,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            var policy = new MilitaryWoundDeathPolicyDefinitionState
            {
                Id = "mod.example.wound_death_policy.veteran_compensation",
                DisplayName = "重伤老卒抚恤",
                MinimumSeverityBasisPoints = 8_000,
                MaximumPostTreatmentHealthBasisPoints = 6_000,
                MinimumDaysAfterCareCompletion = 1,
                BaseCompensationMoney = 333,
                CompensationPerRankMoney = 7
            };
            world.MilitaryWoundDeathPolicies.Add(policy);

            new MilitaryWoundDeathSystem().ResolvePostTreatmentDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(policy.Id));

            Assert.That(world.SchemaVersion, Is.EqualTo(schemaBefore));
            Assert.That(world.MilitarySurvivorCompensations[0].PolicyId,
                Is.EqualTo(policy.Id));
            Assert.That(world.MilitarySurvivorCompensations[0].Amount,
                Is.EqualTo(333 + 7 * service.Rank));
            world.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionFiftyFourWithoutInventingWoundDeath()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 54");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryWoundDeathPolicies,
                Has.Count.EqualTo(
                    MilitaryWoundDeathPolicyCatalog.CreateCore().Count));
            Assert.That(loaded.MilitaryWoundDeathPolicies.Exists(item =>
                item.Id == MilitaryWoundDeathPolicyIds
                    .SevereOriginalEvacuationComplication), Is.True);
            Assert.That(loaded.MilitaryWoundDeathPolicies.Exists(item =>
                item.Id == MilitaryWoundDeathPolicyIds
                    .SevereReturnJourneyComplication), Is.True);
            Assert.That(loaded.MilitaryWoundDeathPolicies.Exists(item =>
                item.Id == MilitaryWoundDeathPolicyIds
                    .SevereAwaitingTeamRejoinComplication), Is.True);
            Assert.That(loaded.MilitaryWoundDeaths, Is.Empty);
            Assert.That(loaded.MilitaryFamilyInheritances, Is.Empty);
            Assert.That(loaded.MilitarySurvivorCompensations, Is.Empty);
            Assert.That(loaded.MilitaryWoundDeathContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            loaded.Validate();
        }

        [Test]
        public void WoundDeath_ReadyForReturnDeathKeepsBodyAndReturnsTeam()
        {
            var world = BuildReadyForReturnWoundDeathWorld(
                true,
                out var admission,
                out var injury,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var careLocationId = evacuation.CurrentCareLocationId;
            var policy = world.MilitaryWoundDeathPolicies[0];

            var death = new MilitaryWoundDeathSystem()
                .ResolveReadyForReturnDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(policy.Id));

            Assert.That(patient.IsAlive, Is.False);
            Assert.That(patient.LocationId, Is.EqualTo(careLocationId));
            Assert.That(service.Status, Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(death.DeathContextId, Is.EqualTo(
                MilitaryWoundDeathContextIds.ReadyForReturnAtCareSite));
            Assert.That(admission.DischargePolicyId, Is.EqualTo(
                MilitaryRearMedicalDischargePolicyIds.DeathAtCareSite));
            Assert.That(evacuation.PatientReturnPolicyId, Is.EqualTo(
                MilitaryMedicalEvacuationPatientReturnPolicyIds
                    .RemainAtCareSiteAfterDeath));
            Assert.That(world.MilitaryMedicalDeathResponsibilities,
                Has.Count.EqualTo(1));
            var responsibility =
                world.MilitaryMedicalDeathResponsibilities[0];
            Assert.That(responsibility.WoundDeathId, Is.EqualTo(death.Id));
            Assert.That(responsibility.RearMedicalSiteId,
                Is.EqualTo(admission.RearMedicalSiteId));
            Assert.That(responsibility.CareOrganizationId,
                Is.EqualTo(organization.Id));
            Assert.That(responsibility.ResponsiblePhysicianPersonId,
                Is.EqualTo(admission.PhysicianPersonId));

            new MilitaryRearMedicalSystem().StartReturn(
                world,
                new StableId(evacuation.Id),
                new StableId("route.zhuo_zhongshan"));

            Assert.That(evacuation.PatientReturnJourneyId, Is.Empty);
            Assert.That(evacuation.TeamMembers, Has.All.Matches<
                MilitaryMedicalEvacuationTeamMemberState>(item =>
                    !string.IsNullOrEmpty(item.ReturnJourneyId)));
            var simulator = new WorldSimulator(world.MasterSeed);
            for (var i = 0;
                 i < 40 && evacuation.Status !=
                    MilitaryMedicalEvacuationStatus.Completed;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }

            Assert.That(evacuation.Status,
                Is.EqualTo(MilitaryMedicalEvacuationStatus.Completed));
            Assert.That(service.Status, Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(patient.LocationId, Is.EqualTo(careLocationId));
            for (var i = 0; i < evacuation.TeamMembers.Count; i++)
            {
                var member = evacuation.TeamMembers[i];
                Assert.That(world.MilitaryServices.Find(item =>
                    item.Id == member.MilitaryServiceId).Status,
                    Is.EqualTo(MilitaryServiceStatus.Active));
            }

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryMedicalDeathResponsibilities,
                Has.Count.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void WoundDeath_UnfinishedTreatmentDeathIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var patient = world.People.Find(item =>
                item.Id == admission.PatientPersonId);
            var successor = world.People.Find(item =>
                item.Id == evacuation.TeamMembers[0].PersonId);
            patient.FamilyId = "family.test.unfinished_wound_death";
            successor.FamilyId = patient.FamilyId;
            world.Families.Add(new FamilyState
            {
                Id = patient.FamilyId,
                DisplayName = "Unfinished Treatment Family",
                HeadPersonId = patient.Id,
                Wealth = 1_000,
                LocationId = site.LocationId,
                MemberIds = new List<string> { patient.Id, successor.Id }
            });
            world.Validate();
            var before = WorldSnapshotSerializer.Serialize(world);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem().ResolveReadyForReturnDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(world.MilitaryWoundDeathPolicies[0].Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void WoundDeath_ReadyForReturnWaitingPeriodIsAtomic()
        {
            var world = BuildReadyForReturnWoundDeathWorld(
                false,
                out var admission,
                out var injury,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem().ResolveReadyForReturnDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(world.MilitaryWoundDeathPolicies[0].Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void WoundDeath_TamperedMedicalResponsibilityIsRejected()
        {
            var world = BuildReadyForReturnWoundDeathWorld(
                true,
                out var admission,
                out var injury,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            new MilitaryWoundDeathSystem().ResolveReadyForReturnDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(world.MilitaryWoundDeathPolicies[0].Id));
            world.MilitaryMedicalDeathResponsibilities[0]
                .ResponsiblePhysicianPersonId = successor.Id;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFiftyFiveWoundDeathContext()
        {
            var world = BuildCompletedRetiredWoundDeathWorld(
                true,
                out var admission,
                out var injury,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            new MilitaryWoundDeathSystem().ResolvePostTreatmentDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(world.MilitaryWoundDeathPolicies[0].Id));
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace(
                    "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 55")
                .Replace(
                    "\"DeathContextId\": \"" +
                    MilitaryWoundDeathContextIds
                        .PostReturnMedicalRetirement + "\"",
                    "\"DeathContextId\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryWoundDeaths[0].DeathContextId,
                Is.EqualTo(
                    MilitaryWoundDeathContextIds.PostReturnMedicalRetirement));
            Assert.That(loaded.MilitaryWoundDeaths[0].MedicalResponsibilityId,
                Is.Empty);
            Assert.That(loaded.MilitaryMedicalDeathResponsibilities, Is.Empty);
            Assert.That(
                loaded.MilitaryMedicalDeathResponsibilityContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            loaded.Validate();
        }

        [Test]
        public void WoundDeath_InTreatmentDeathClosesCareAndReturnsTeam()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                site,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);

            var death = new MilitaryWoundDeathSystem()
                .ResolveInTreatmentDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                    new StableId(world
                        .MilitaryInpatientDeteriorationPolicies[0].Id));

            var closure = world.MilitaryInpatientDeathClosures[0];
            Assert.That(death.DeathContextId, Is.EqualTo(
                MilitaryWoundDeathContextIds.InTreatmentAtCareSite));
            Assert.That(closure.OpeningHealthBasisPoints,
                Is.EqualTo(injury.AdmissionHealthBasisPoints));
            Assert.That(closure.ClosingHealthBasisPoints,
                Is.EqualTo(death.HealthAtDeathBasisPoints));
            Assert.That(closure.CompletedTreatmentStagesAtDeath, Is.Zero);
            Assert.That(closure.RequiredTreatmentStagesAtDeath,
                Is.EqualTo(admission.RequiredTreatmentStages));
            Assert.That(closure.NextTreatmentProtocolId,
                Is.EqualTo(admission.TreatmentPlanProtocolIds[0]));
            Assert.That(admission.Status,
                Is.EqualTo(MilitaryRearMedicalAdmissionStatus.Discharged));
            Assert.That(admission.DischargedDay, Is.EqualTo(death.Day));
            Assert.That(evacuation.Status,
                Is.EqualTo(MilitaryMedicalEvacuationStatus.ReadyForReturn));
            Assert.That(patient.LocationId, Is.EqualTo(site.LocationId));
            Assert.That(service.Status, Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(world.MilitaryMedicalDeathResponsibilities,
                Has.Count.EqualTo(1));

            new MilitaryRearMedicalSystem().StartReturn(
                world,
                new StableId(evacuation.Id),
                new StableId("route.zhuo_zhongshan"));
            Assert.That(evacuation.PatientReturnJourneyId, Is.Empty);
            var simulator = new WorldSimulator(world.MasterSeed);
            for (var i = 0;
                 i < 40 && evacuation.Status !=
                    MilitaryMedicalEvacuationStatus.Completed;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }
            Assert.That(evacuation.Status,
                Is.EqualTo(MilitaryMedicalEvacuationStatus.Completed));
            Assert.That(service.Status, Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(patient.LocationId, Is.EqualTo(site.LocationId));
            for (var i = 0; i < evacuation.TeamMembers.Count; i++)
            {
                Assert.That(world.MilitaryServices.Find(item =>
                    item.Id == evacuation.TeamMembers[i]
                        .MilitaryServiceId).Status,
                    Is.EqualTo(MilitaryServiceStatus.Active));
            }
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryInpatientDeathClosures,
                Has.Count.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void WoundDeath_InTreatmentDeathReleasesOnlyUnusedTransferMedicine()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 10,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            var transferSystem = new MilitaryMedicalTransferSystem();
            var transfer = transferSystem.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 20);
            transferSystem.Receive(
                world,
                new StableId(transfer.Id),
                new StableId(receiver.Id));
            new MilitaryRearMedicalSystem().TreatInpatient(
                world, new StableId(admission.Id));
            var batch = world.ProductBatches.Find(item =>
                item.Id == transfer.ReservedMedicineBatchId);
            var quantityAfterTreatment = batch.Quantity;
            var reservedBeforeDeath = batch.ReservedQuantity;
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                destinationSite,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out army);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);

            new MilitaryWoundDeathSystem().ResolveInTreatmentDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                new StableId(world
                    .MilitaryInpatientDeteriorationPolicies[0].Id));

            var closure = world.MilitaryInpatientDeathClosures[0];
            Assert.That(transfer.ConsumedReservedMedicineUnits, Is.EqualTo(1));
            Assert.That(closure.ReleasedReservedMedicineUnits,
                Is.EqualTo(reservedBeforeDeath));
            Assert.That(transfer.ReleasedReservedMedicineUnits,
                Is.EqualTo(reservedBeforeDeath));
            Assert.That(batch.Quantity, Is.EqualTo(quantityAfterTreatment));
            Assert.That(batch.ReservedQuantity, Is.Zero);
            var release = world.InventoryTransactions.Find(item =>
                item.Id == closure.ReservationReleaseInventoryTransactionId);
            Assert.That(release.Type, Is.EqualTo(
                InventoryTransactionType
                    .MilitaryMedicalTransferMedicineReleased));
            Assert.That(release.Lines[0].QuantityDelta, Is.Zero);
            Assert.That(release.Lines[0].ReservedQuantityDelta,
                Is.EqualTo(-reservedBeforeDeath));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            loaded.Validate();
            loaded.InventoryTransactions.Find(item =>
                    item.Id == closure.ReservationReleaseInventoryTransactionId)
                .Lines[0].ReservedQuantityDelta++;
            Assert.Throws<InvalidOperationException>(() => loaded.Validate());
        }

        [Test]
        public void WoundDeath_InTreatmentWaitingPeriodIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                site,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem().ResolveInTreatmentDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                    new StableId(world
                        .MilitaryInpatientDeteriorationPolicies[0].Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void InpatientDeath_ActiveMedicalTransferIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 10,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            new MilitaryMedicalTransferSystem().Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                sourceSite,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out army);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem().ResolveInTreatmentDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                    new StableId(world
                        .MilitaryInpatientDeteriorationPolicies[0].Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void InpatientDeath_DataPolicyNeedsNoSchemaChange()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                site,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            var schemaBefore = world.SchemaVersion;
            var policy = new
                MilitaryInpatientDeteriorationPolicyDefinitionState
            {
                Id = "mod.example.inpatient_deterioration.severe_fever",
                DisplayName = "高热恶化",
                MinimumSeverityBasisPoints = 8_000,
                MinimumDaysAfterAdmission = 1,
                HealthLossBasisPoints = 500,
                MaximumClosingHealthBasisPoints = 1_000
            };
            world.MilitaryInpatientDeteriorationPolicies.Add(policy);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);

            new MilitaryWoundDeathSystem().ResolveInTreatmentDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                new StableId(policy.Id));

            Assert.That(world.SchemaVersion, Is.EqualTo(schemaBefore));
            Assert.That(world.MilitaryInpatientDeathClosures[0]
                .DeteriorationPolicyId, Is.EqualTo(policy.Id));
            world.Validate();
        }

        [Test]
        public void WoundDeath_TamperedInpatientClosureIsRejected()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var site);
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                site,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            new MilitaryWoundDeathSystem().ResolveInTreatmentDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                new StableId(world
                    .MilitaryInpatientDeteriorationPolicies[0].Id));
            world.MilitaryInpatientDeathClosures[0]
                .OpeningHealthBasisPoints++;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFiftySixWithoutInventingInpatientDeath()
        {
            var world = BuildReadyForReturnWoundDeathWorld(
                true,
                out var admission,
                out var injury,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out var army);
            new MilitaryWoundDeathSystem().ResolveReadyForReturnDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(world.MilitaryWoundDeathPolicies[0].Id));
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 56");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryInpatientDeteriorationPolicies,
                Has.Count.EqualTo(1));
            Assert.That(loaded.MilitaryInpatientDeathClosures, Is.Empty);
            Assert.That(loaded.MilitaryWoundDeaths[0]
                .InpatientDeathClosureId, Is.Empty);
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0]
                .InpatientDeathClosureId, Is.Empty);
            Assert.That(loaded.MilitaryInpatientDeathContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            loaded.Validate();
        }

        [Test]
        public void WoundDeath_DuringMedicalTransferReleasesResourcesAndCarriesCorpse()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 10,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            var transfer = new MilitaryMedicalTransferSystem().Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                sourceSite,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out army);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            var patientJourney = world.Journeys.Find(item =>
                item.Id == transfer.PatientJourneyId);
            Assert.That(patientJourney, Is.Not.Null);
            var remainingAtDeath = patientJourney.RemainingKilometers;
            var medicineBatch = world.ProductBatches.Find(item =>
                item.Id == transfer.ReservedMedicineBatchId);
            var medicineQuantity = medicineBatch.Quantity;

            var death = new MilitaryWoundDeathSystem()
                .ResolveMedicalTransferDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                    new StableId(world
                        .MilitaryInpatientDeteriorationPolicies[0].Id));

            var closure = world.MilitaryMedicalTransferDeathClosures[0];
            Assert.That(death.DeathContextId, Is.EqualTo(
                MilitaryWoundDeathContextIds.DuringCrossFacilityTransfer));
            Assert.That(closure.OccurredInTransit, Is.True);
            Assert.That(closure.RemainingKilometersAtDeath,
                Is.EqualTo(remainingAtDeath));
            Assert.That(transfer.Status, Is.EqualTo(
                MilitaryMedicalTransferStatus.DeceasedInTransit));
            Assert.That(world.Journeys.Find(item =>
                item.Id == transfer.PatientJourneyId), Is.Not.Null);
            Assert.That(patient.IsAlive, Is.False);
            Assert.That(patient.Provisions, Is.GreaterThanOrEqualTo(0));
            var corpseProvisions = patient.Provisions;
            Assert.That(medicineBatch.Quantity, Is.EqualTo(medicineQuantity));
            Assert.That(medicineBatch.ReservedQuantity, Is.Zero);
            Assert.That(transfer.ReleasedReservedMedicineUnits,
                Is.EqualTo(transfer.ReservedMedicineUnits));
            Assert.That(admission.Status, Is.EqualTo(
                MilitaryRearMedicalAdmissionStatus.Discharged));
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.Admitted));

            var simulator = new WorldSimulator(world.MasterSeed);
            for (var i = 0;
                 i < 40 && transfer.Status !=
                     MilitaryMedicalTransferStatus.ClosedAfterPatientDeath;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }
            Assert.That(transfer.Status, Is.EqualTo(
                MilitaryMedicalTransferStatus.ClosedAfterPatientDeath));
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.ReadyForReturn));
            Assert.That(patient.LocationId,
                Is.EqualTo(destinationSite.LocationId));
            Assert.That(patient.Provisions, Is.EqualTo(corpseProvisions));

            RelocateSourceArmyWithoutEvacuationParty(
                world, army, "location.xiaquyang");
            new MilitaryRearMedicalSystem().StartReturn(
                world,
                new StableId(evacuation.Id),
                new StableId("route.anping_xiaquyang"));
            for (var i = 0;
                 i < 40 && evacuation.Status !=
                     MilitaryMedicalEvacuationStatus.Completed;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.Completed));
            Assert.That(service.Status,
                Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(patient.LocationId,
                Is.EqualTo(destinationSite.LocationId));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryMedicalTransferDeathClosures,
                Has.Count.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void WoundDeath_AwaitingTransferReceptionClosesWithoutHandoff()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 10,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            var transfer = new MilitaryMedicalTransferSystem().Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                sourceSite,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out army);
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 20);
            Assert.That(transfer.Status, Is.EqualTo(
                MilitaryMedicalTransferStatus.AwaitingReception));

            var death = new MilitaryWoundDeathSystem()
                .ResolveMedicalTransferDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                    new StableId(world
                        .MilitaryInpatientDeteriorationPolicies[0].Id));

            var closure = world.MilitaryMedicalTransferDeathClosures[0];
            Assert.That(closure.OccurredInTransit, Is.False);
            Assert.That(closure.RemainingKilometersAtDeath, Is.Zero);
            Assert.That(transfer.Status, Is.EqualTo(
                MilitaryMedicalTransferStatus.ClosedAfterPatientDeath));
            Assert.That(string.IsNullOrEmpty(transfer.ReceivingPersonId),
                Is.True);
            Assert.That(transfer.ResponsibilityTransferredDay, Is.EqualTo(-1));
            Assert.That(admission.PhysicianPersonId,
                Is.EqualTo(transfer.SourcePhysicianPersonId));
            Assert.That(admission.RearMedicalSiteId,
                Is.EqualTo(destinationSite.Id));
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.ReadyForReturn));
            Assert.That(death.DeathLocationId,
                Is.EqualTo(destinationSite.LocationId));
            world.Validate();
        }

        [Test]
        public void MedicalTransferDeath_WaitingPeriodIsAtomic()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 10,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            new MilitaryMedicalTransferSystem().Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                sourceSite,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out army);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem()
                    .ResolveMedicalTransferDeath(
                        world,
                        new StableId(admission.Id),
                        new StableId(army.CommanderPersonId),
                        new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                        new StableId(world
                            .MilitaryInpatientDeteriorationPolicies[0].Id)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void MedicalTransferDeath_TamperedTransitClosureIsRejected()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var destinationSite = BuildMedicalTransferDestination(
                world, sourceSite.OwnerOrganizationId, 2, 10,
                out var receiver);
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            var army = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            new MilitaryMedicalTransferSystem().Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(admission.Id),
                new StableId(destinationSite.Id),
                new StableId("route.zhongshan_anping"),
                new StableId(receiver.Id));
            AttachInpatientWoundDeathFamily(
                world,
                admission,
                sourceSite,
                out var patient,
                out var successor,
                out var family,
                out var organization,
                out var service,
                out army);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            new MilitaryWoundDeathSystem().ResolveMedicalTransferDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(world.MilitaryWoundDeathPolicies[0].Id),
                new StableId(world
                    .MilitaryInpatientDeteriorationPolicies[0].Id));
            world.MilitaryMedicalTransferDeathClosures[0]
                .RouteId = "route.tampered";

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFiftySevenWithoutInventingTransferDeath()
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var admission,
                out var injury,
                out var sourceSite);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 57");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryMedicalTransferDeathClosures,
                Is.Empty);
            Assert.That(loaded.MilitaryMedicalTransferDeathContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            Assert.That(loaded.MilitaryRearMedicalAdmissions[0]
                .MedicalTransferDeathClosureId, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void WoundDeath_DuringOriginalEvacuationCarriesCorpseAndReturnsTeam()
        {
            var world = BuildOriginalEvacuationDeathWorld(
                false,
                true,
                out var evacuation,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices,
                out var receiver);
            var patientJourney = world.Journeys.Find(item =>
                item.Id == evacuation.PatientJourneyId);
            Assert.That(patientJourney, Is.Not.Null);
            var remainingAtDeath = patientJourney.RemainingKilometers;
            var corpseProvisions = patient.Provisions;

            var death = new MilitaryWoundDeathSystem()
                .ResolveOriginalEvacuationDeath(
                    world,
                    new StableId(evacuation.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(MilitaryWoundDeathPolicyIds
                        .SevereOriginalEvacuationComplication),
                    new StableId(
                        MilitaryOriginalEvacuationDeteriorationPolicyIds
                            .SevereUntreatedTransitComplication));

            var closure = world
                .MilitaryOriginalEvacuationDeathClosures[0];
            Assert.That(death.DeathContextId, Is.EqualTo(
                MilitaryWoundDeathContextIds.DuringOriginalEvacuation));
            Assert.That(closure.OccurredInTransit, Is.True);
            Assert.That(closure.RemainingKilometersAtDeath,
                Is.EqualTo(remainingAtDeath));
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.DeceasedInTransit));
            Assert.That(patient.IsAlive, Is.False);
            Assert.That(patientService.Status,
                Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(world.MilitaryRearMedicalAdmissions, Is.Empty);
            Assert.That(world.MilitaryInjuryEpisodes, Is.Empty);
            Assert.That(world.MilitaryMedicalDeathResponsibilities[0]
                .SourceArmyId, Is.EqualTo(army.Id));
            Assert.That(world.MilitaryMedicalDeathResponsibilities[0]
                .ResponsiblePhysicianPersonId, Is.Empty);
            Assert.That(string.IsNullOrEmpty(
                evacuation.ReceivingPersonId), Is.True);

            var simulator = new WorldSimulator(world.MasterSeed);
            for (var i = 0;
                 i < 40 && evacuation.Status ==
                     MilitaryMedicalEvacuationStatus.DeceasedInTransit;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.ReadyForReturn));
            Assert.That(patient.LocationId,
                Is.EqualTo(evacuation.DestinationLocationId));
            Assert.That(patient.Provisions, Is.EqualTo(corpseProvisions));
            Assert.That(receiver.Id,
                Is.EqualTo(evacuation.DesignatedReceivingPersonId));
            Assert.That(string.IsNullOrEmpty(
                evacuation.ReceivingPersonId), Is.True);

            new MilitaryRearMedicalSystem().StartReturn(
                world,
                new StableId(evacuation.Id),
                new StableId(evacuation.RouteId));
            for (var i = 0;
                 i < 40 && evacuation.Status !=
                     MilitaryMedicalEvacuationStatus.Completed;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.Completed));
            Assert.That(teamServices.TrueForAll(item =>
                item.Status == MilitaryServiceStatus.Active), Is.True);
            Assert.That(patient.LocationId,
                Is.EqualTo(evacuation.DestinationLocationId));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryOriginalEvacuationDeathClosures,
                Has.Count.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void WoundDeath_AwaitingOriginalEvacuationReceptionDoesNotHandoff()
        {
            var world = BuildOriginalEvacuationDeathWorld(
                true,
                true,
                out var evacuation,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices,
                out var receiver);
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.AwaitingReception));

            var death = new MilitaryWoundDeathSystem()
                .ResolveOriginalEvacuationDeath(
                    world,
                    new StableId(evacuation.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(MilitaryWoundDeathPolicyIds
                        .SevereOriginalEvacuationComplication),
                    new StableId(
                        MilitaryOriginalEvacuationDeteriorationPolicyIds
                            .SevereUntreatedTransitComplication));

            var closure = world
                .MilitaryOriginalEvacuationDeathClosures[0];
            Assert.That(closure.OccurredInTransit, Is.False);
            Assert.That(closure.RemainingKilometersAtDeath, Is.Zero);
            Assert.That(death.DeathLocationId,
                Is.EqualTo(evacuation.DestinationLocationId));
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.ReadyForReturn));
            Assert.That(string.IsNullOrEmpty(
                evacuation.ReceivingPersonId), Is.True);
            Assert.That(evacuation.ReceivedDay, Is.EqualTo(-1));
            Assert.That(world.MilitaryRearMedicalAdmissions, Is.Empty);
            Assert.That(world.MilitaryInjuryEpisodes, Is.Empty);
            Assert.That(world.MilitaryMedicalDeathResponsibilities[0]
                .ResponsiblePhysicianPersonId, Is.EqualTo(string.Empty));
            world.Validate();
        }

        [Test]
        public void OriginalEvacuationDeath_WaitingPeriodIsAtomic()
        {
            var world = BuildOriginalEvacuationDeathWorld(
                false,
                false,
                out var evacuation,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices,
                out var receiver);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem()
                    .ResolveOriginalEvacuationDeath(
                        world,
                        new StableId(evacuation.Id),
                        new StableId(army.CommanderPersonId),
                        new StableId(MilitaryWoundDeathPolicyIds
                            .SevereOriginalEvacuationComplication),
                        new StableId(
                            MilitaryOriginalEvacuationDeteriorationPolicyIds
                                .SevereUntreatedTransitComplication)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void OriginalEvacuationDeath_TamperedClosureIsRejected()
        {
            var world = BuildOriginalEvacuationDeathWorld(
                false,
                true,
                out var evacuation,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices,
                out var receiver);
            new MilitaryWoundDeathSystem().ResolveOriginalEvacuationDeath(
                world,
                new StableId(evacuation.Id),
                new StableId(army.CommanderPersonId),
                new StableId(MilitaryWoundDeathPolicyIds
                    .SevereOriginalEvacuationComplication),
                new StableId(
                    MilitaryOriginalEvacuationDeteriorationPolicyIds
                        .SevereUntreatedTransitComplication));
            world.MilitaryOriginalEvacuationDeathClosures[0]
                .RouteId = "route.tampered";

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFiftyEightWithoutInventingOriginalEvacuationDeath()
        {
            var world = PrototypeWorldFactory.Create184World(184_059);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 58");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryOriginalEvacuationDeathClosures,
                Is.Empty);
            Assert.That(loaded
                .MilitaryOriginalEvacuationDeathContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            Assert.That(loaded
                .MilitaryOriginalEvacuationDeteriorationPolicies,
                Is.Not.Empty);
            Assert.That(loaded.MilitaryMedicalEvacuations.TrueForAll(item =>
                string.IsNullOrEmpty(
                    item.OriginalEvacuationDeathClosureId)), Is.True);
            loaded.Validate();
        }

        [Test]
        public void PatientReturnDeath_ReturnsCorpseAndClosesRejoin()
        {
            var world = BuildPatientReturnDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var injury,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices);
            var journey = world.Journeys.Find(item =>
                item.Id == evacuation.PatientReturnJourneyId);
            var remainingAtDeath = journey.RemainingKilometers;
            var provisionsAtDeath = patient.Provisions;

            var death = new MilitaryWoundDeathSystem()
                .ResolvePatientReturnJourneyDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(MilitaryWoundDeathPolicyIds
                        .SevereReturnJourneyComplication),
                    new StableId(
                        MilitaryPatientReturnDeteriorationPolicyIds
                            .SevereTravelRelapse));

            var closure = world.MilitaryPatientReturnDeathClosures[0];
            Assert.That(death.DeathContextId, Is.EqualTo(
                MilitaryWoundDeathContextIds.DuringPatientReturnJourney));
            Assert.That(closure.RemainingKilometersAtDeath,
                Is.EqualTo(remainingAtDeath));
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus
                    .PatientDeceasedReturningToArmy));
            Assert.That(evacuation.PatientReturnPolicyId, Is.EqualTo(
                MilitaryMedicalEvacuationPatientReturnPolicyIds
                    .ReturnCorpseWithTeam));
            Assert.That(patient.IsAlive, Is.False);
            Assert.That(patientService.Status,
                Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(world.MilitaryMedicalDeathResponsibilities[0]
                .ResponsibilityPolicyId, Is.EqualTo(
                    MilitaryMedicalDeathResponsibilityPolicyIds
                        .LastCareTeamDuringAuthorizedReturn));

            var frozenSnapshot = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() =>
                new ArmySystem().StartMarch(
                    world,
                    new StableId(army.CommanderPersonId),
                    new StableId(army.Id),
                    new StableId("route.zhuo_zhongshan"),
                    new StableId("location.zhuo")));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(frozenSnapshot));

            var simulator = new WorldSimulator(world.MasterSeed);
            simulator.AdvanceSegments(world, 1);
            Assert.That(patient.Provisions, Is.EqualTo(provisionsAtDeath));
            for (var i = 0;
                 i < 40 && evacuation.Status !=
                     MilitaryMedicalEvacuationStatus.Completed;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }

            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.Completed));
            Assert.That(admission.Status, Is.EqualTo(
                MilitaryRearMedicalAdmissionStatus.Completed));
            Assert.That(patient.LocationId, Is.EqualTo(army.LocationId));
            Assert.That(patientService.Status,
                Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(teamServices.TrueForAll(item =>
                item.Status == MilitaryServiceStatus.Active), Is.True);
            Assert.That(patient.Provisions, Is.EqualTo(provisionsAtDeath));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryPatientReturnDeathClosures,
                Has.Count.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void PatientReturnDeath_WaitingPeriodIsAtomic()
        {
            var world = BuildPatientReturnDeathWorld(
                false,
                out var evacuation,
                out var admission,
                out var injury,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem()
                    .ResolvePatientReturnJourneyDeath(
                        world,
                        new StableId(admission.Id),
                        new StableId(army.CommanderPersonId),
                        new StableId(MilitaryWoundDeathPolicyIds
                            .SevereReturnJourneyComplication),
                        new StableId(
                            MilitaryPatientReturnDeteriorationPolicyIds
                                .SevereTravelRelapse)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void PatientReturnDeath_TamperedClosureIsRejected()
        {
            var world = BuildPatientReturnDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var injury,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices);
            new MilitaryWoundDeathSystem().ResolvePatientReturnJourneyDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(MilitaryWoundDeathPolicyIds
                    .SevereReturnJourneyComplication),
                new StableId(
                    MilitaryPatientReturnDeteriorationPolicyIds
                        .SevereTravelRelapse));
            world.MilitaryPatientReturnDeathClosures[0]
                .ReturnRouteId = "route.tampered";

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void PatientReturnDeath_RejectsMissingPatientJourneyAtomically()
        {
            var world = BuildPatientReturnDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var injury,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices);
            world.Journeys.RemoveAll(item =>
                item.Id == evacuation.PatientReturnJourneyId);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem()
                    .ResolvePatientReturnJourneyDeath(
                        world,
                        new StableId(admission.Id),
                        new StableId(army.CommanderPersonId),
                        new StableId(MilitaryWoundDeathPolicyIds
                            .SevereReturnJourneyComplication),
                        new StableId(
                            MilitaryPatientReturnDeteriorationPolicyIds
                                .SevereTravelRelapse)));
            Assert.That(world.MilitaryWoundDeaths, Is.Empty);
            Assert.That(patient.IsAlive, Is.True);
        }

        [Test]
        public void Snapshot_MigratesVersionFiftyNineWithoutInventingPatientReturnDeath()
        {
            var world = PrototypeWorldFactory.Create184World(184_060);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 59");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryPatientReturnDeathClosures,
                Is.Empty);
            Assert.That(loaded
                .MilitaryPatientReturnDeathContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            Assert.That(loaded.MilitaryPatientReturnDeteriorationPolicies,
                Is.Not.Empty);
            Assert.That(loaded.MilitaryMedicalEvacuations.TrueForAll(item =>
                string.IsNullOrEmpty(
                    item.PatientReturnDeathClosureId)), Is.True);
            loaded.Validate();
        }

        [Test]
        public void PatientArrivalWaitingTeamDeath_LeavesCorpseAndClosesAfterTeamRejoin()
        {
            var world = BuildPatientArrivalWaitingTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var injury,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices);
            var patientReturnJourneyId = evacuation.PatientReturnJourneyId;

            var death = new MilitaryWoundDeathSystem()
                .ResolvePatientArrivalWaitingTeamDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(MilitaryWoundDeathPolicyIds
                        .SevereAwaitingTeamRejoinComplication),
                    new StableId(
                        MilitaryPatientReturnDeteriorationPolicyIds
                            .SeverePostJourneyRelapse));

            var closure = world.MilitaryPatientReturnDeathClosures[0];
            Assert.That(death.DeathContextId, Is.EqualTo(
                MilitaryWoundDeathContextIds
                    .AwaitingReturnTeamRejoinAtArmy));
            Assert.That(death.DeathLocationId, Is.EqualTo(army.LocationId));
            Assert.That(closure.PatientJourneyCompletedBeforeDeath, Is.True);
            Assert.That(closure.RemainingKilometersAtDeath, Is.Zero);
            Assert.That(closure.TeamJourneySnapshotsAtDeath,
                Has.Count.EqualTo(evacuation.TeamMembers.Count));
            Assert.That(closure.TeamJourneySnapshotsAtDeath.Exists(item =>
                item.RemainingKilometersAtDeath > 0), Is.True);
            Assert.That(world.Journeys.Exists(item =>
                item.Id == patientReturnJourneyId), Is.False);
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus
                    .PatientDeceasedAwaitingTeamRejoin));
            Assert.That(evacuation.PatientReturnPolicyId, Is.EqualTo(
                MilitaryMedicalEvacuationPatientReturnPolicyIds
                    .CorpseAtArmyAwaitingTeamRejoin));
            Assert.That(patient.LocationId, Is.EqualTo(army.LocationId));
            Assert.That(patient.IsAlive, Is.False);
            Assert.That(patientService.Status,
                Is.EqualTo(MilitaryServiceStatus.Dead));

            var frozenSnapshot = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() =>
                new ArmySystem().StartMarch(
                    world,
                    new StableId(army.CommanderPersonId),
                    new StableId(army.Id),
                    new StableId("route.zhuo_zhongshan"),
                    new StableId("location.zhuo")));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(frozenSnapshot));

            var simulator = new WorldSimulator(world.MasterSeed);
            for (var i = 0;
                 i < 20 && evacuation.Status !=
                     MilitaryMedicalEvacuationStatus.Completed;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }

            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.Completed));
            Assert.That(admission.Status, Is.EqualTo(
                MilitaryRearMedicalAdmissionStatus.Completed));
            Assert.That(patient.LocationId, Is.EqualTo(army.LocationId));
            Assert.That(patientService.Status,
                Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(teamServices.TrueForAll(item =>
                item.Status == MilitaryServiceStatus.Active), Is.True);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryPatientReturnDeathClosures,
                Has.Count.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void PatientArrivalWaitingTeamDeath_WaitingPeriodIsAtomic()
        {
            var world = BuildPatientArrivalWaitingTeamDeathWorld(
                false,
                out var evacuation,
                out var admission,
                out var injury,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem()
                    .ResolvePatientArrivalWaitingTeamDeath(
                        world,
                        new StableId(admission.Id),
                        new StableId(army.CommanderPersonId),
                        new StableId(MilitaryWoundDeathPolicyIds
                            .SevereAwaitingTeamRejoinComplication),
                        new StableId(
                            MilitaryPatientReturnDeteriorationPolicyIds
                                .SeverePostJourneyRelapse)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void PatientArrivalWaitingTeamDeath_RejectsNoOutstandingTeamJourneyAtomically()
        {
            var world = BuildPatientArrivalWaitingTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var injury,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices);
            var population = new PopulationLedgerSystem();
            for (var i = 0; i < evacuation.TeamMembers.Count; i++)
            {
                var member = evacuation.TeamMembers[i];
                world.Journeys.RemoveAll(item =>
                    item.Id == member.ReturnJourneyId);
                population.MoveIndependentPerson(
                    world,
                    world.People.Find(item => item.Id == member.PersonId),
                    evacuation.ReturnDestinationLocationId,
                    false);
            }
            world.Validate();
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryWoundDeathSystem()
                    .ResolvePatientArrivalWaitingTeamDeath(
                        world,
                        new StableId(admission.Id),
                        new StableId(army.CommanderPersonId),
                        new StableId(MilitaryWoundDeathPolicyIds
                            .SevereAwaitingTeamRejoinComplication),
                        new StableId(
                            MilitaryPatientReturnDeteriorationPolicyIds
                                .SeverePostJourneyRelapse)));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void PatientArrivalWaitingTeamDeath_TamperedTeamSnapshotIsRejected()
        {
            var world = BuildPatientArrivalWaitingTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var injury,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices);
            new MilitaryWoundDeathSystem()
                .ResolvePatientArrivalWaitingTeamDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(MilitaryWoundDeathPolicyIds
                        .SevereAwaitingTeamRejoinComplication),
                    new StableId(
                        MilitaryPatientReturnDeteriorationPolicyIds
                            .SeverePostJourneyRelapse));
            world.MilitaryPatientReturnDeathClosures[0]
                .TeamJourneySnapshotsAtDeath[0].ReturnJourneyId =
                    "journey.tampered";

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionSixtyWithoutInventingPatientArrivalWaitingTeamDeath()
        {
            var world = PrototypeWorldFactory.Create184World(184_061);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 60");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryPatientReturnDeathClosures, Is.Empty);
            Assert.That(loaded
                .MilitaryPatientArrivalWaitingTeamDeathContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            Assert.That(loaded.MilitaryWoundDeathPolicies.Exists(item =>
                item.Id == MilitaryWoundDeathPolicyIds
                    .SevereAwaitingTeamRejoinComplication), Is.True);
            Assert.That(loaded
                .MilitaryPatientReturnDeteriorationPolicies.Exists(item =>
                    item.Id == MilitaryPatientReturnDeteriorationPolicyIds
                        .SeverePostJourneyRelapse), Is.True);
            loaded.Validate();
        }

        [Test]
        public void ReturnTeamDeath_LivingPatientCompletesCorpseAndSurvivorRejoin()
        {
            var world = BuildReturnTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var army,
                out var patient,
                out var deceasedMember,
                out var deceasedService,
                out var teamServices);
            var provisionsBefore = deceasedMember.Provisions;

            var death = new MilitaryReturnTeamDeathSystem()
                .ResolveReturnJourneyDeath(
                    world,
                    new StableId(evacuation.Id),
                    new StableId(deceasedMember.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(MilitaryReturnTeamDeathPolicyIds
                        .ReturnJourneyFatality));

            Assert.That(death.RemainingKilometersAtDeath,
                Is.GreaterThan(0));
            Assert.That(death.CorpseArrivedDay, Is.EqualTo(-1));
            Assert.That(deceasedMember.IsAlive, Is.False);
            Assert.That(deceasedService.Status,
                Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(world.Journeys.Exists(item =>
                item.Id == death.ReturnJourneyId), Is.True);
            Assert.That(world.MilitaryFamilyInheritances.Exists(item =>
                item.ReturnTeamDeathId == death.Id &&
                string.IsNullOrEmpty(item.WoundDeathId)), Is.True);
            Assert.That(world.MilitarySurvivorCompensations.Exists(item =>
                item.ReturnTeamDeathId == death.Id &&
                string.IsNullOrEmpty(item.WoundDeathId)), Is.True);
            new TravelSystem().ConsumeDailyTravelProvisions(world);
            Assert.That(deceasedMember.Provisions,
                Is.EqualTo(provisionsBefore));
            world.Validate();

            var frozen = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() =>
                new ArmySystem().StartMarch(
                    world,
                    new StableId(army.CommanderPersonId),
                    new StableId(army.Id),
                    new StableId("route.zhuo_zhongshan"),
                    new StableId("location.zhuo")));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(frozen));

            var simulator = new WorldSimulator(world.MasterSeed);
            world.Journeys.Find(item =>
                item.Id == death.ReturnJourneyId).RemainingKilometers =
                    TravelSystem.KilometersPerSegment(TravelMode.Foot);
            world.Journeys.Find(item =>
                item.Id == evacuation.PatientReturnJourneyId)
                    .RemainingKilometers = checked(
                        TravelSystem.KilometersPerSegment(TravelMode.Foot) * 2);
            for (var i = 1; i < evacuation.TeamMembers.Count; i++)
            {
                world.Journeys.Find(item =>
                    item.Id == evacuation.TeamMembers[i].ReturnJourneyId)
                        .RemainingKilometers = checked(
                            TravelSystem.KilometersPerSegment(TravelMode.Foot) *
                            2);
            }
            simulator.AdvanceSegments(world, 1);
            Assert.That(death.CorpseArrivedDay,
                Is.EqualTo(world.AbsoluteDay));
            Assert.That(evacuation.Status, Is.Not.EqualTo(
                MilitaryMedicalEvacuationStatus.Completed));
            for (var i = 0;
                 i < 20 && evacuation.Status !=
                     MilitaryMedicalEvacuationStatus.Completed;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }

            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus.Completed));
            Assert.That(admission.Status, Is.EqualTo(
                MilitaryRearMedicalAdmissionStatus.Completed));
            Assert.That(patient.IsAlive, Is.True);
            Assert.That(deceasedMember.LocationId,
                Is.EqualTo(army.LocationId));
            Assert.That(death.CorpseArrivedDay,
                Is.GreaterThanOrEqualTo(death.Day));
            Assert.That(deceasedService.Status,
                Is.EqualTo(MilitaryServiceStatus.Dead));
            Assert.That(teamServices.FindAll(item =>
                item.Id != deceasedService.Id).TrueForAll(item =>
                    item.Status == MilitaryServiceStatus.Active), Is.True);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MilitaryReturnTeamDeaths,
                Has.Count.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void ReturnTeamDeath_PatientCorpseJourneyKeepsBothDeaths()
        {
            var world = BuildReturnTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var army,
                out var patient,
                out var deceasedMember,
                out var deceasedService,
                out var teamServices);
            new MilitaryWoundDeathSystem().ResolvePatientReturnJourneyDeath(
                world,
                new StableId(admission.Id),
                new StableId(army.CommanderPersonId),
                new StableId(MilitaryWoundDeathPolicyIds
                    .SevereReturnJourneyComplication),
                new StableId(MilitaryPatientReturnDeteriorationPolicyIds
                    .SevereTravelRelapse));

            var teamDeath = new MilitaryReturnTeamDeathSystem()
                .ResolveReturnJourneyDeath(
                    world,
                    new StableId(evacuation.Id),
                    new StableId(deceasedMember.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(MilitaryReturnTeamDeathPolicyIds
                        .ReturnJourneyFatality));

            Assert.That(patient.IsAlive, Is.False);
            Assert.That(deceasedMember.IsAlive, Is.False);
            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus
                    .PatientDeceasedReturningToArmy));
            Assert.That(world.MilitaryWoundDeaths, Has.Count.EqualTo(1));
            Assert.That(world.MilitaryReturnTeamDeaths,
                Has.Count.EqualTo(1));
            Assert.That(teamDeath.EvacuationId, Is.EqualTo(evacuation.Id));
            world.Validate();
        }

        [Test]
        public void ReturnTeamDeath_AwaitingTeamPatientDeathKeepsBothDeaths()
        {
            var world = BuildPatientArrivalWaitingTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var injury,
                out var army,
                out var patient,
                out var patientService,
                out var teamServices);
            AddReturnTeamMembersToPatientFamily(
                world, evacuation, patient.FamilyId);
            new MilitaryWoundDeathSystem()
                .ResolvePatientArrivalWaitingTeamDeath(
                    world,
                    new StableId(admission.Id),
                    new StableId(army.CommanderPersonId),
                    new StableId(MilitaryWoundDeathPolicyIds
                        .SevereAwaitingTeamRejoinComplication),
                    new StableId(
                        MilitaryPatientReturnDeteriorationPolicyIds
                            .SeverePostJourneyRelapse));
            var deceasedMember = world.People.Find(item =>
                item.Id == evacuation.TeamMembers[0].PersonId);

            new MilitaryReturnTeamDeathSystem().ResolveReturnJourneyDeath(
                world,
                new StableId(evacuation.Id),
                new StableId(deceasedMember.Id),
                new StableId(army.CommanderPersonId),
                new StableId(MilitaryReturnTeamDeathPolicyIds
                    .ReturnJourneyFatality));

            Assert.That(evacuation.Status, Is.EqualTo(
                MilitaryMedicalEvacuationStatus
                    .PatientDeceasedAwaitingTeamRejoin));
            Assert.That(patient.LocationId, Is.EqualTo(army.LocationId));
            Assert.That(patient.IsAlive, Is.False);
            Assert.That(deceasedMember.IsAlive, Is.False);
            Assert.That(world.MilitaryWoundDeaths, Has.Count.EqualTo(1));
            Assert.That(world.MilitaryReturnTeamDeaths,
                Has.Count.EqualTo(1));
            world.Validate();
        }

        [Test]
        public void ReturnTeamDeath_WaitingPeriodIsAtomic()
        {
            var world = BuildReturnTeamDeathWorld(
                false,
                out var evacuation,
                out var admission,
                out var army,
                out var patient,
                out var deceasedMember,
                out var deceasedService,
                out var teamServices);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryReturnTeamDeathSystem()
                    .ResolveReturnJourneyDeath(
                        world,
                        new StableId(evacuation.Id),
                        new StableId(deceasedMember.Id),
                        new StableId(army.CommanderPersonId),
                        new StableId(MilitaryReturnTeamDeathPolicyIds
                            .ReturnJourneyFatality)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void ReturnTeamDeath_MissingJourneyIsAtomic()
        {
            var world = BuildReturnTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var army,
                out var patient,
                out var deceasedMember,
                out var deceasedService,
                out var teamServices);
            var before = WorldSnapshotSerializer.Serialize(world);
            var journeyIndex = world.Journeys.FindIndex(item =>
                item.Id == evacuation.TeamMembers[0].ReturnJourneyId);
            var removedJourney = world.Journeys[journeyIndex];
            world.Journeys.RemoveAt(journeyIndex);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryReturnTeamDeathSystem()
                    .ResolveReturnJourneyDeath(
                        world,
                        new StableId(evacuation.Id),
                        new StableId(deceasedMember.Id),
                        new StableId(army.CommanderPersonId),
                        new StableId(MilitaryReturnTeamDeathPolicyIds
                            .ReturnJourneyFatality)));

            world.Journeys.Insert(journeyIndex, removedJourney);
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void ReturnTeamDeath_TamperedRouteIsRejected()
        {
            var world = BuildReturnTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var army,
                out var patient,
                out var deceasedMember,
                out var deceasedService,
                out var teamServices);
            new MilitaryReturnTeamDeathSystem().ResolveReturnJourneyDeath(
                world,
                new StableId(evacuation.Id),
                new StableId(deceasedMember.Id),
                new StableId(army.CommanderPersonId),
                new StableId(MilitaryReturnTeamDeathPolicyIds
                    .ReturnJourneyFatality));
            world.MilitaryReturnTeamDeaths[0].ReturnRouteId =
                "route.tampered";

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void ReturnTeamDeath_InsufficientTreasuryIsAtomic()
        {
            var world = BuildReturnTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var army,
                out var patient,
                out var deceasedMember,
                out var deceasedService,
                out var teamServices);
            world.Organizations.Find(item =>
                item.Id == army.OrganizationId).Treasury = 0;
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryReturnTeamDeathSystem()
                    .ResolveReturnJourneyDeath(
                        world,
                        new StableId(evacuation.Id),
                        new StableId(deceasedMember.Id),
                        new StableId(army.CommanderPersonId),
                        new StableId(MilitaryReturnTeamDeathPolicyIds
                            .ReturnJourneyFatality)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void ReturnTeamDeath_InsufficientAuthorityIsAtomic()
        {
            var world = BuildReturnTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var army,
                out var patient,
                out var deceasedMember,
                out var deceasedService,
                out var teamServices);
            var unauthorizedPerson = world.People.Find(item =>
                item.Id == evacuation.TeamMembers[1].PersonId);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryReturnTeamDeathSystem()
                    .ResolveReturnJourneyDeath(
                        world,
                        new StableId(evacuation.Id),
                        new StableId(deceasedMember.Id),
                        new StableId(unauthorizedPerson.Id),
                        new StableId(MilitaryReturnTeamDeathPolicyIds
                            .ReturnJourneyFatality)));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void Snapshot_MigratesVersionSixtyOneWithoutInventingReturnTeamDeath()
        {
            var world = BuildReturnTeamDeathWorld(
                true,
                out var evacuation,
                out var admission,
                out var army,
                out var patient,
                out var deceasedMember,
                out var deceasedService,
                out var teamServices);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 61");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryReturnTeamDeaths, Is.Empty);
            Assert.That(loaded.MilitaryReturnTeamDeathPolicies.Exists(item =>
                item.Id == MilitaryReturnTeamDeathPolicyIds
                    .ReturnJourneyFatality), Is.True);
            Assert.That(loaded.MilitaryReturnTeamDeathContractActivationDay,
                Is.EqualTo(loaded.AbsoluteDay + 1));
            Assert.That(loaded.MilitaryMedicalEvacuations.TrueForAll(item =>
                item.TeamMembers.TrueForAll(member =>
                    string.IsNullOrEmpty(member.ReturnDeathId))), Is.True);
            Assert.That(loaded.MilitaryFamilyInheritances.TrueForAll(item =>
                string.IsNullOrEmpty(item.ReturnTeamDeathId)), Is.True);
            Assert.That(loaded.MilitarySurvivorCompensations.TrueForAll(item =>
                string.IsNullOrEmpty(item.ReturnTeamDeathId)), Is.True);
            loaded.Validate();
        }

        [Test]
        public void NewGame_CustomCharacterCreatesPlayableHouseholdAndPosition()
        {
            var request = new NewGameCharacterRequest
            {
                DisplayName = "玄德",
                Age = 22,
                Gender = PersonGender.Male,
                Identity = StartingIdentity.Soldier
            };

            var world = new NewGameSetupService().CreateCustom184World(request, 184);
            var player = world.People.Find(
                item => item.Id == NewGameSetupService.CustomPlayerPersonId);
            var household = world.Families.Find(
                item => item.HeadPersonId == NewGameSetupService.CustomPlayerPersonId);
            var membership = world.Memberships.Find(
                item => item.PersonId == NewGameSetupService.CustomPlayerPersonId);

            Assert.That(world.PlayerPersonId, Is.EqualTo(player.Id));
            Assert.That(player.DisplayName, Is.EqualTo("玄德"));
            Assert.That(player.BirthDay, Is.EqualTo(-22 * 360L));
            Assert.That(player.LocationId, Is.EqualTo("location.zhongshan"));
            Assert.That(household, Is.Not.Null);
            Assert.That(household.MemberIds, Does.Contain(player.Id));
            Assert.That(membership.PositionId, Is.EqualTo("position.youzhou_soldier"));
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void NewGame_EachStartingIdentityReceivesItsWorldRole()
        {
            var identities = new[]
            {
                StartingIdentity.Soldier,
                StartingIdentity.CountyClerk,
                StartingIdentity.Merchant,
                StartingIdentity.Physician,
                StartingIdentity.Farmer,
                StartingIdentity.Scholar
            };
            var expectedLocations = new[]
            {
                "location.zhongshan",
                "location.zhuo",
                "location.zhongshan",
                "location.guangzong",
                "location.zhuo",
                "location.zhuo"
            };
            var expectedPositions = new[]
            {
                "position.youzhou_soldier",
                "position.zhuo_county_clerk",
                "position.zhongshan_trader",
                "position.guangzong_physician",
                "position.zhuo_farmer",
                "position.zhuo_scholar"
            };

            for (var i = 0; i < identities.Length; i++)
            {
                var world = new NewGameSetupService().CreateCustom184World(
                    new NewGameCharacterRequest
                    {
                        DisplayName = "测试人物" + i,
                        Age = 18 + i,
                        Gender = i % 2 == 0 ? PersonGender.Male : PersonGender.Female,
                        Identity = identities[i]
                    },
                    184);
                var player = world.People.Find(
                    item => item.Id == world.PlayerPersonId);
                var membership = world.Memberships.Find(
                    item => item.PersonId == world.PlayerPersonId);

                Assert.That(player.LocationId, Is.EqualTo(expectedLocations[i]));
                Assert.That(membership.PositionId, Is.EqualTo(expectedPositions[i]));
                Assert.DoesNotThrow(world.Validate);
            }
        }

        [Test]
        public void NewGame_CanControlAnyExistingWorldPerson()
        {
            var service = new NewGameSetupService();
            var world = service.CreateExisting184World("person.zhang_shiping", 184);

            Assert.That(world.PlayerPersonId, Is.EqualTo("person.zhang_shiping"));
            Assert.That(
                world.People.Find(item => item.Id == world.PlayerPersonId).DisplayName,
                Is.EqualTo("张世平"));
            Assert.That(
                world.People.Exists(
                    item => item.Id == NewGameSetupService.CustomPlayerPersonId),
                Is.False);
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void NewGame_RejectsInvalidCustomCharacter()
        {
            var service = new NewGameSetupService();

            Assert.Throws<ArgumentException>(
                () => service.CreateCustom184World(
                    new NewGameCharacterRequest
                    {
                        DisplayName = " ",
                        Age = 18,
                        Gender = PersonGender.Male
                    },
                    184));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => service.CreateCustom184World(
                    new NewGameCharacterRequest
                    {
                        DisplayName = "幼童",
                        Age = 10,
                        Gender = PersonGender.Male
                    },
                    184));
            Assert.Throws<InvalidOperationException>(
                () => service.CreateExisting184World("person.missing", 184));
        }

        [Test]
        public void M26_NewGame_BackgroundAndLocationCreateConcreteHousehold()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "异乡行商",
                    Age = 24,
                    Gender = PersonGender.Female,
                    Identity = StartingIdentity.Merchant,
                    BackgroundId = StartingBackgroundIds.SupportedHousehold,
                    StartingLocationId = "location.zhuo"
                },
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var family = world.Families.Find(item => item.Id == player.FamilyId);

            Assert.That(player.LocationId, Is.EqualTo("location.zhuo"));
            Assert.That(player.BirthLocationId, Is.EqualTo("location.zhuo"));
            Assert.That(player.Wealth, Is.GreaterThan(2_000));
            Assert.That(family.LocationId, Is.EqualTo("location.zhuo"));
            Assert.That(family.MemberIds, Does.Contain(player.Id));
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_NewGame_ExistingFarmerKeepsOriginalWorldFacts()
        {
            var baseline = PrototypeWorldFactory.Create184World(184);
            var count = baseline.People.Count;
            var baselineFamily = baseline.Families.Find(item =>
                item.Id == "family.zhuo_farm_household");
            var world = new NewGameSetupService().CreateExisting184World(
                "person.generated.farmer_001",
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var family = world.Families.Find(item => item.Id == player.FamilyId);

            Assert.That(world.People.Count, Is.EqualTo(count));
            Assert.That(family, Is.Not.Null);
            Assert.That(family.LocationId, Is.EqualTo(baselineFamily.LocationId));
            Assert.That(family.VillageId, Is.EqualTo(baselineFamily.VillageId));
            Assert.That(family.Grain, Is.EqualTo(baselineFamily.Grain));
            Assert.That(family.SeedGrain, Is.EqualTo(baselineFamily.SeedGrain));
            Assert.That(family.FarmlandUnits,
                Is.EqualTo(baselineFamily.FarmlandUnits));
            Assert.That(world.Villages.Count, Is.EqualTo(baseline.Villages.Count));
            Assert.That(world.VillageFacilities.Count,
                Is.EqualTo(baseline.VillageFacilities.Count));
            Assert.That(world.Memberships.Count,
                Is.EqualTo(baseline.Memberships.Count));
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_NewGame_SoldierOnlyAcceptsRealArmyStartingLocation()
        {
            var service = new NewGameSetupService();
            var preview = PrototypeWorldFactory.Create184World(184);
            var army = preview.Armies.Find(item =>
                item.Id == "army.youzhou_reinforcement");
            var legal = service.GetLegalStartingLocationIds(
                preview, StartingIdentity.Soldier);

            Assert.That(legal, Is.EqualTo(new[] { army.LocationId }));
            Assert.Throws<ArgumentException>(() => service.CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "错地新卒",
                    Age = 20,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Soldier,
                    StartingLocationId = "location.guangzong"
                },
                184));

            var world = service.CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "集结新卒",
                    Age = 20,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Soldier,
                    StartingLocationId = army.LocationId
                },
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            Assert.That(player.LocationId, Is.EqualTo(army.LocationId));
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_PlayerAction_FarmerCreatesFormalSeasonOrder()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "春耕者",
                    Age = 20,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Farmer
                },
                184);
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));

            var result = actions.Execute(
                world, world.PlayerPersonId, PlayerActionIds.FarmStart);

            Assert.That(result.Success, Is.True);
            Assert.That(result.DaysAdvanced, Is.EqualTo(1));
            Assert.That(world.AgricultureWorkOrders.Exists(item =>
                item.ManagerPersonId == world.PlayerPersonId &&
                item.ControlMode == ProductionControlMode.PersonalLabor &&
                item.Status == ProductionOrderStatus.Active), Is.True);
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_PlayerAction_FarmerCompletesHarvestAndBatchBridge()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "麦收者",
                    Age = 22,
                    Gender = PersonGender.Female,
                    Identity = StartingIdentity.Farmer
                },
                184);
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));
            actions.Execute(world, world.PlayerPersonId, PlayerActionIds.FarmStart);

            var result = actions.Execute(
                world, world.PlayerPersonId, PlayerActionIds.FarmComplete);
            var order = world.AgricultureWorkOrders.Find(item =>
                item.ManagerPersonId == world.PlayerPersonId);

            Assert.That(result.Success, Is.True);
            Assert.That(order.Status, Is.EqualTo(ProductionOrderStatus.Completed));
            Assert.That(order.StoredQuantity, Is.GreaterThan(0));
            Assert.That(world.InventoryTransactions.Exists(item =>
                item.SourceWorkOrderId == order.Id), Is.True);
            Assert.That(world.ProductBatches.Exists(item =>
                item.SourceTransactionId != string.Empty &&
                item.OwnerFamilyId == order.FamilyId), Is.True);
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_PlayerAction_MerchantBuyAndSellUseWorldMarketLedger()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "布商",
                    Age = 26,
                    Gender = PersonGender.Female,
                    Identity = StartingIdentity.Merchant
                },
                184);
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));

            var buy = actions.Execute(
                world, world.PlayerPersonId, PlayerActionIds.TradeBuy);
            var sell = actions.Execute(
                world, world.PlayerPersonId, PlayerActionIds.TradeSell);

            Assert.That(buy.Success, Is.True);
            Assert.That(sell.Success, Is.True);
            Assert.That(world.TradeRecords.Count, Is.EqualTo(2));
            Assert.That(world.TradeRecords[0].IsPurchase, Is.True);
            Assert.That(world.TradeRecords[1].IsPurchase, Is.False);
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_PlayerAction_MerchantBuyAvailabilityUsesLiveMarketCost()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "试价布商",
                    Age = 26,
                    Gender = PersonGender.Female,
                    Identity = StartingIdentity.Merchant,
                    StartingLocationId = "location.guangzong"
                },
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            player.Wealth = 420;
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));

            var option = actions.QueryActions(world, player.Id).Single(item =>
                item.Id == PlayerActionIds.TradeBuy);
            var rejected = actions.Execute(
                world, player.Id, PlayerActionIds.TradeBuy);

            Assert.That(option.IsAvailable, Is.False);
            Assert.That(option.UnavailableReason, Does.Contain("440"));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.DaysAdvanced, Is.EqualTo(0));
            Assert.That(world.TradeRecords, Is.Empty);

            player.Wealth = 440;
            option = actions.QueryActions(world, player.Id).Single(item =>
                item.Id == PlayerActionIds.TradeBuy);
            Assert.That(option.IsAvailable, Is.True);
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_PlayerAction_ScholarStudyChangesSkillAndWorldTime()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "乡学士人",
                    Age = 19,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Scholar
                },
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));

            var result = actions.Execute(
                world, player.Id, PlayerActionIds.Study);

            Assert.That(result.Success, Is.True);
            Assert.That(result.DaysAdvanced, Is.EqualTo(30));
            var record = world.LearningRecords.Find(item =>
                item.StudentPersonId == player.Id);
            Assert.That(record, Is.Not.Null);
            Assert.That(record.SkillGain, Is.GreaterThan(0));
            Assert.That(record.SkillAfter, Is.GreaterThan(record.SkillBefore));
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_PlayerAction_TaskConstructionAndHomeCareShareWorldFacts()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "涿县书佐",
                    Age = 28,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.CountyClerk
                },
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));

            Assert.That(actions.Execute(
                world, player.Id, PlayerActionIds.AcceptTask).Success, Is.True);
            Assert.That(actions.Execute(
                world, player.Id, PlayerActionIds.WorkTask).Success, Is.True);
            Assert.That(actions.Execute(
                world, player.Id, PlayerActionIds.AbandonTask).Success, Is.True);
            Assert.That(world.Tasks[0].Status, Is.EqualTo(TaskStatus.Abandoned));

            for (var i = 0; i < 4 &&
                 !world.ConstructionProjects.Exists(item => item.IsCompleted); i++)
            {
                Assert.That(actions.Execute(
                    world, player.Id, PlayerActionIds.Construction).Success,
                    Is.True);
            }
            var completed = world.ConstructionProjects.Find(
                item => item.IsCompleted);
            Assert.That(completed, Is.Not.Null);
            Assert.That(
                world.Locations.Find(item => item.Id == player.LocationId).Features &
                completed.TargetFeature,
                Is.EqualTo(completed.TargetFeature));

            player.HealthBasisPoints = 6_000;
            var provisions = player.Provisions;
            var care = actions.Execute(
                world, player.Id, PlayerActionIds.HomeRest);
            Assert.That(care.Success, Is.True);
            Assert.That(care.DaysAdvanced, Is.EqualTo(7));
            Assert.That(player.Provisions, Is.EqualTo(provisions - 2));
            Assert.That(player.HealthBasisPoints, Is.GreaterThan(6_000));
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_PlayerAction_SoldierMarchesAndResolvesLocalBattle()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "幽州新卒",
                    Age = 21,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Soldier
                },
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));

            Assert.That(actions.Execute(
                world, player.Id, PlayerActionIds.ArmyAdvance).Success, Is.True);
            Assert.That(player.LocationId, Is.EqualTo("location.anping"));
            Assert.That(actions.Execute(
                world, player.Id, PlayerActionIds.ArmyAdvance).Success, Is.True);
            Assert.That(player.LocationId, Is.EqualTo("location.guangzong"));
            var battle = actions.Execute(
                world, player.Id, PlayerActionIds.Battle);

            Assert.That(battle.Success, Is.True);
            Assert.That(world.Battles.Count, Is.EqualTo(1));
            Assert.That(world.Battles[0].LocationId,
                Is.EqualTo("location.guangzong"));
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_PlayerAction_EventChoicePersistsAcrossSnapshot()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "乡里善人",
                    Age = 31,
                    Gender = PersonGender.Female,
                    Identity = StartingIdentity.CountyClerk
                },
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var provisions = player.Provisions;
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));

            var result = actions.Execute(
                world, player.Id, PlayerActionIds.LocalReliefHelp);
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(result.Success, Is.True);
            Assert.That(player.Provisions, Is.EqualTo(provisions - 2));
            Assert.That(loaded.LifeEvents.Exists(item =>
                item.Id == result.WorldEventId), Is.True);
            Assert.That(new PlayerActionService(
                    new WorldSimulator(loaded.MasterSeed))
                .QueryActions(loaded, loaded.PlayerPersonId)
                .Any(item => item.Id == PlayerActionIds.LocalReliefHelp),
                Is.False);
            Assert.DoesNotThrow(loaded.Validate);
        }

        [Test]
        public void M26_PlayerAction_HistoricalRumorRequiresRelevantLocation()
        {
            var service = new NewGameSetupService();
            var remote = service.CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "涿县书佐",
                    Age = 30,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.CountyClerk,
                    StartingLocationId = "location.zhuo"
                },
                184);
            remote.AbsoluteDay = 10;
            var remoteActions = new PlayerActionService(
                new WorldSimulator(remote.MasterSeed));
            Assert.That(remoteActions.QueryActions(
                    remote, remote.PlayerPersonId).Any(item =>
                        item.Id == PlayerActionIds.HistoricalReport),
                Is.False);

            var local = service.CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "广宗访客",
                    Age = 30,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.CountyClerk,
                    StartingLocationId = "location.guangzong"
                },
                184);
            local.AbsoluteDay = 10;
            var localActions = new PlayerActionService(
                new WorldSimulator(local.MasterSeed));
            Assert.That(localActions.QueryActions(
                    local, local.PlayerPersonId).Any(item =>
                        item.Id == PlayerActionIds.HistoricalReport),
                Is.True);
        }

        [Test]
        public void M26_PlayerAction_FieldCareFailsWithoutPhysicianAndConsumesNoDay()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "待治新卒",
                    Age = 20,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Soldier
                },
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var service = world.MilitaryServices.Find(item =>
                item.PersonId == player.Id);
            service.Status = MilitaryServiceStatus.Wounded;
            player.HealthBasisPoints = 4_000;
            for (var personIndex = 0;
                 personIndex < world.People.Count;
                 personIndex++)
            {
                world.People[personIndex].MedicalSkillBasisPoints = 0;
                world.People[personIndex].ProfessionalSkills.Medicine = 0;
            }
            new MilitaryServiceSystem().SynchronizeArmyCaches(
                world, service.ArmyId);
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));

            var option = actions.QueryActions(world, player.Id).Single(item =>
                item.Id == PlayerActionIds.FieldCare);
            var result = actions.Execute(
                world, player.Id, PlayerActionIds.FieldCare);

            Assert.That(option.IsAvailable, Is.False);
            Assert.That(option.UnavailableReason, Does.Contain("医者"));
            Assert.That(result.Success, Is.False);
            Assert.That(result.DaysAdvanced, Is.EqualTo(0));
            Assert.That(world.AbsoluteDay, Is.EqualTo(0));
            Assert.That(service.Status, Is.EqualTo(MilitaryServiceStatus.Wounded));
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26_PlayerAction_FieldCareTargetsControlledPerson()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "受伤新卒",
                    Age = 20,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Soldier
                },
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var service = world.MilitaryServices.Find(item =>
                item.PersonId == player.Id);
            var army = world.Armies.Find(item => item.Id == service.ArmyId);
            var other = world.MilitaryServices.Find(item =>
                item.ArmyId == army.Id &&
                item.PersonId != player.Id &&
                item.PersonId != army.CommanderPersonId &&
                item.Status == MilitaryServiceStatus.Active);
            var otherPerson = world.People.Find(item => item.Id == other.PersonId);
            service.Status = MilitaryServiceStatus.Wounded;
            player.HealthBasisPoints = 4_000;
            other.Status = MilitaryServiceStatus.Wounded;
            otherPerson.HealthBasisPoints = 4_000;
            new MilitaryServiceSystem().SynchronizeArmyCaches(world, army.Id);

            var physician = world.People.Find(item =>
                item.Id == "person.generated.physician_001");
            new PopulationLedgerSystem().MoveIndependentPerson(
                world, physician, army.LocationId, false);
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));

            var option = actions.QueryActions(world, player.Id).Single(item =>
                item.Id == PlayerActionIds.FieldCare);
            Assert.That(option.IsAvailable, Is.True,
                option.UnavailableReason);
            var result = actions.Execute(
                world, player.Id, PlayerActionIds.FieldCare);

            Assert.That(result.Success, Is.True, result.Summary);
            Assert.That(result.DaysAdvanced, Is.EqualTo(1));
            Assert.That(world.MilitaryMedicalCases.Exists(item =>
                item.PatientPersonId == player.Id), Is.True);
            Assert.That(service.Status, Is.EqualTo(MilitaryServiceStatus.Active));
            Assert.That(player.HealthBasisPoints, Is.GreaterThan(4_000));
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void Snapshot_RoundTripPreservesControlledPlayer()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "行医者",
                    Age = 30,
                    Gender = PersonGender.Female,
                    Identity = StartingIdentity.Physician
                },
                184);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(
                loaded.PlayerPersonId,
                Is.EqualTo(NewGameSetupService.CustomPlayerPersonId));
            Assert.That(
                loaded.People.Find(item => item.Id == loaded.PlayerPersonId).DisplayName,
                Is.EqualTo("行医者"));
            Assert.DoesNotThrow(loaded.Validate);
        }

        [Test]
        public void PrototypeWorld_MapPositionsAreUniqueAndInBounds()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var positions = new System.Collections.Generic.HashSet<string>();

            Assert.That(world.Locations.Count, Is.EqualTo(6));
            for (var i = 0; i < world.Locations.Count; i++)
            {
                var location = world.Locations[i];
                Assert.That(location.MapXBasisPoints, Is.InRange(0, 10_000));
                Assert.That(location.MapYBasisPoints, Is.InRange(0, 10_000));
                Assert.That(
                    positions.Add(
                        location.MapXBasisPoints + "|" + location.MapYBasisPoints),
                    Is.True,
                    $"地图地点坐标重叠：{location.DisplayName}");
            }
        }

        [Test]
        public void Snapshot_RoundTripPreservesRegionMapPositions()
        {
            var world = new NewGameSetupService().CreateExisting184World(
                "person.liu_bei",
                184);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.Locations.Count, Is.EqualTo(world.Locations.Count));
            for (var i = 0; i < world.Locations.Count; i++)
            {
                Assert.That(
                    loaded.Locations[i].MapXBasisPoints,
                    Is.EqualTo(world.Locations[i].MapXBasisPoints));
                Assert.That(
                    loaded.Locations[i].MapYBasisPoints,
                    Is.EqualTo(world.Locations[i].MapYBasisPoints));
            }
        }

        [Test]
        public void PrototypeWorld_LocationsDeclareGeographyAndFacilities()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var kinds = new System.Collections.Generic.HashSet<LocationKind>();
            var terrains = new System.Collections.Generic.HashSet<TerrainKind>();
            var hasMarket = false;
            var hasFarmland = false;

            for (var i = 0; i < world.Locations.Count; i++)
            {
                var location = world.Locations[i];
                Assert.That(location.Kind, Is.Not.EqualTo(LocationKind.Unknown));
                Assert.That(location.Terrain, Is.Not.EqualTo(TerrainKind.Unknown));
                Assert.That(location.StrategicImportance, Is.InRange(1, 5));
                kinds.Add(location.Kind);
                terrains.Add(location.Terrain);
                hasMarket |=
                    (location.Features & LocationFeature.Market) != 0;
                hasFarmland |=
                    (location.Features & LocationFeature.Farmland) != 0;
            }

            Assert.That(kinds.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(terrains.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(hasMarket, Is.True);
            Assert.That(hasFarmland, Is.True);
        }

        [Test]
        public void Snapshot_RoundTripPreservesLocationGeographyMetadata()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            for (var i = 0; i < world.Locations.Count; i++)
            {
                Assert.That(
                    loaded.Locations[i].Kind,
                    Is.EqualTo(world.Locations[i].Kind));
                Assert.That(
                    loaded.Locations[i].Terrain,
                    Is.EqualTo(world.Locations[i].Terrain));
                Assert.That(
                    loaded.Locations[i].Features,
                    Is.EqualTo(world.Locations[i].Features));
                Assert.That(
                    loaded.Locations[i].StrategicImportance,
                    Is.EqualTo(world.Locations[i].StrategicImportance));
            }
        }

        [Test]
        public void Validation_RejectsInvalidLocationGeographyMetadata()
        {
            var world = BuildMinimalWorld();
            world.Locations[0].StrategicImportance = 0;

            Assert.Throws<System.InvalidOperationException>(world.Validate);
        }

        [Test]
        public void MapPerspective_RecommendsViewForEachStartingIdentity()
        {
            var identities = new[]
            {
                StartingIdentity.Soldier,
                StartingIdentity.CountyClerk,
                StartingIdentity.Merchant,
                StartingIdentity.Physician
            };
            var expected = new[]
            {
                MapPerspective.Military,
                MapPerspective.Administration,
                MapPerspective.Commerce,
                MapPerspective.Medicine
            };
            var setup = new NewGameSetupService();

            for (var i = 0; i < identities.Length; i++)
            {
                var world = setup.CreateCustom184World(
                    new NewGameCharacterRequest
                    {
                        DisplayName = "视角测试",
                        Age = 20,
                        Gender = PersonGender.Male,
                        Identity = identities[i]
                    },
                    184);
                Assert.That(
                    MapPerspectiveSystem.RecommendForPlayer(
                        world,
                        world.PlayerPersonId),
                    Is.EqualTo(expected[i]));
            }
        }

        [Test]
        public void MapPerspective_CommerceAndMedicineExposeDifferentInformation()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var location = world.Locations.Find(
                item => item.Id == "location.guangzong");
            var patient = world.People.Find(
                item => item.Id == "person.generated.physician_001");
            var army = world.Armies.Find(
                item => item.Id == "army.yellow_turban_guangzong");
            patient.HealthBasisPoints = 8_000;
            new MilitaryServiceSystem().ApplyCasualties(
                world,
                new StableId(army.Id),
                20,
                20,
                1);

            var commerce = MapPerspectiveSystem.Inspect(
                world,
                location,
                MapPerspective.Commerce);
            var medicine = MapPerspectiveSystem.Inspect(
                world,
                location,
                MapPerspective.Medicine);

            Assert.That(commerce.PrimaryMetric, Does.StartWith("粮"));
            Assert.That(
                commerce.VisibleFeatures & LocationFeature.Market,
                Is.Not.EqualTo(LocationFeature.None));
            var unhealthyPeople = world.People.FindAll(
                item =>
                    item.LocationId == location.Id &&
                    item.IsAlive &&
                    item.HealthBasisPoints < 9_000).Count;
            Assert.That(
                medicine.PrimaryMetric,
                Does.EndWith((unhealthyPeople + army.WoundedTroops).ToString()));
            Assert.That(medicine.SecondaryMetric, Does.Contain("药"));
            Assert.That(
                medicine.VisibleFeatures & LocationFeature.Clinic,
                Is.Not.EqualTo(LocationFeature.None));
        }

        [Test]
        public void MapPerspective_MilitaryDoesNotCountArmyAlreadyMarching()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            new ArmySystem().StartMarch(
                world,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("route.xiaquyang_guangzong"),
                new StableId("location.guangzong"));
            var origin = world.Locations.Find(
                item => item.Id == "location.xiaquyang");

            var information = MapPerspectiveSystem.Inspect(
                world,
                origin,
                MapPerspective.Military);

            Assert.That(information.PrimaryMetric, Is.EqualTo("兵0"));
        }

        [Test]
        public void Construction_ContributionCompletesProjectAndAddsFacility()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "营造者",
                    Age = 25,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Merchant
                },
                184);
            var player = world.People.Find(
                item => item.Id == world.PlayerPersonId);
            var wealthBefore = player.Wealth;
            var system = new ConstructionSystem();
            var project = system.StartProject(
                world,
                new StableId(player.Id),
                new StableId(player.LocationId),
                LocationFeature.Clinic);

            var result = system.Contribute(
                world,
                new StableId(project.Id),
                new StableId(player.Id),
                100,
                80);
            var location = world.Locations.Find(
                item => item.Id == player.LocationId);

            Assert.That(result.Completed, Is.True);
            Assert.That(project.IsCompleted, Is.True);
            Assert.That(player.Wealth, Is.EqualTo(wealthBefore - 100));
            Assert.That(
                location.Features & LocationFeature.Clinic,
                Is.EqualTo(LocationFeature.Clinic));
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void Construction_SnapshotPreservesActiveProjectProgress()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "筑城者",
                    Age = 25,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Soldier
                },
                184);
            var player = world.People.Find(
                item => item.Id == world.PlayerPersonId);
            var system = new ConstructionSystem();
            var project = system.StartProject(
                world,
                new StableId(player.Id),
                new StableId(player.LocationId),
                LocationFeature.Fortification);
            system.Contribute(
                world,
                new StableId(project.Id),
                new StableId(player.Id),
                20,
                20);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var loadedProject = loaded.ConstructionProjects[0];

            Assert.That(loadedProject.Progress, Is.EqualTo(24));
            Assert.That(loadedProject.MoneyInvested, Is.EqualTo(20));
            Assert.That(loadedProject.IsCompleted, Is.False);
            Assert.That(
                loaded.Locations.Find(item => item.Id == player.LocationId)
                    .Features & LocationFeature.Fortification,
                Is.EqualTo(LocationFeature.None));
        }

        [Test]
        public void Construction_RejectsExistingFacilityAndDuplicateProject()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "营造者",
                    Age = 25,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Merchant
                },
                184);
            var player = world.People.Find(
                item => item.Id == world.PlayerPersonId);
            var system = new ConstructionSystem();

            Assert.Throws<System.InvalidOperationException>(
                () => system.StartProject(
                    world,
                    new StableId(player.Id),
                    new StableId(player.LocationId),
                    LocationFeature.Market));
            system.StartProject(
                world,
                new StableId(player.Id),
                new StableId(player.LocationId),
                LocationFeature.Clinic);
            Assert.Throws<System.InvalidOperationException>(
                () => system.StartProject(
                    world,
                    new StableId(player.Id),
                    new StableId(player.LocationId),
                    LocationFeature.Clinic));
        }

        [Test]
        public void Construction_EachStartingIdentityHasSuggestedLocalProject()
        {
            var identities = new[]
            {
                StartingIdentity.Soldier,
                StartingIdentity.CountyClerk,
                StartingIdentity.Merchant,
                StartingIdentity.Physician
            };
            var setup = new NewGameSetupService();

            for (var i = 0; i < identities.Length; i++)
            {
                var world = setup.CreateCustom184World(
                    new NewGameCharacterRequest
                    {
                        DisplayName = "营造建议",
                        Age = 25,
                        Gender = PersonGender.Male,
                        Identity = identities[i]
                    },
                    184);
                var player = world.People.Find(
                    item => item.Id == world.PlayerPersonId);
                var location = world.Locations.Find(
                    item => item.Id == player.LocationId);
                var perspective = MapPerspectiveSystem.RecommendForPlayer(
                    world,
                    player.Id);
                var feature = ConstructionSystem.RecommendFeature(
                    location,
                    perspective);

                Assert.That(feature, Is.Not.EqualTo(LocationFeature.None));
                Assert.That(
                    location.Features & feature,
                    Is.EqualTo(LocationFeature.None));
            }
        }

        [Test]
        public void PopulationLedger_PrototypeOpeningPopulationIsBalanced()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var audit = new PopulationLedgerSystem().Audit(world);
            long locationPopulation = 0;
            for (var i = 0; i < world.Locations.Count; i++)
            {
                locationPopulation += world.Locations[i].Population;
            }

            Assert.That(world.PopulationLedgerInitialized, Is.True);
            Assert.That(world.PopulationCohorts.Count, Is.GreaterThan(0));
            Assert.That(audit.IsBalanced, Is.True);
            Assert.That(audit.ActualPopulation, Is.EqualTo(locationPopulation));
            Assert.That(
                audit.AbstractPopulation + audit.IndependentPopulation,
                Is.EqualTo(audit.ActualPopulation));
        }

        [Test]
        public void PopulationLedger_CustomPlayerMaterializesWithoutCreatingPerson()
        {
            var baseline = PrototypeWorldFactory.Create184World(184);
            var baselinePopulation =
                new PopulationLedgerSystem().Audit(baseline).ActualPopulation;
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "人口账测试",
                    Age = 20,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Merchant
                },
                184);
            var audit = new PopulationLedgerSystem().Audit(world);

            Assert.That(audit.ActualPopulation, Is.EqualTo(baselinePopulation));
            Assert.That(
                world.PopulationTransactions.Exists(
                    item =>
                        item.Type ==
                        PopulationTransactionType.Instantiation &&
                        item.PersonId == world.PlayerPersonId),
                Is.True);
        }

        [Test]
        public void PopulationLedger_TravelMovesOnePersonWithoutChangingWorldTotal()
        {
            var world = new NewGameSetupService().CreateExisting184World(
                "person.liu_bei",
                184);
            var populationSystem = new PopulationLedgerSystem();
            var totalBefore = populationSystem.Audit(world).ActualPopulation;
            var zhuoBefore = world.Locations.Find(
                item => item.Id == "location.zhuo").Population;
            var zhongshanBefore = world.Locations.Find(
                item => item.Id == "location.zhongshan").Population;
            new TravelSystem().StartJourney(
                world,
                new StableId("person.liu_bei"),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhongshan"),
                TravelMode.Foot);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 5);

            var audit = populationSystem.Audit(world);
            Assert.That(audit.ActualPopulation, Is.EqualTo(totalBefore));
            Assert.That(
                world.Locations.Find(item => item.Id == "location.zhuo")
                    .Population,
                Is.EqualTo(zhuoBefore - 1));
            Assert.That(
                world.Locations.Find(item => item.Id == "location.zhongshan")
                    .Population,
                Is.EqualTo(zhongshanBefore + 1));
            Assert.That(
                world.PopulationTransactions.Exists(
                    item =>
                        item.Type == PopulationTransactionType.Migration &&
                        item.PersonId == "person.liu_bei"),
                Is.True);
        }

        [Test]
        public void PopulationLedger_CohortMigrationPreservesGlobalPopulation()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var system = new PopulationLedgerSystem();
            var source = world.PopulationCohorts.Find(
                item =>
                    item.LocationId == "location.zhuo" &&
                    item.Occupation == PopulationOccupation.Agriculture);
            var totalBefore = system.Audit(world).ActualPopulation;

            system.TransferCohort(
                world,
                new StableId(source.Id),
                new StableId("location.guangzong"),
                100);

            var audit = system.Audit(world);
            Assert.That(audit.IsBalanced, Is.True);
            Assert.That(audit.ActualPopulation, Is.EqualTo(totalBefore));
            Assert.That(
                world.PopulationTransactions.Exists(
                    item =>
                        item.Type == PopulationTransactionType.Migration &&
                        item.Quantity == 100),
                Is.True);
        }

        [Test]
        public void PopulationLedger_BirthAndDeathHaveTraceableNetEffect()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var system = new PopulationLedgerSystem();
            var totalBefore = system.Audit(world).ActualPopulation;
            var child = new PersonState
            {
                Id = "person.generated.population_test_child",
                DisplayName = "人口测试新生儿",
                LocationId = "location.zhuo",
                BirthDay = world.AbsoluteDay,
                Gender = PersonGender.Female,
                Provisions = 0
            };
            world.People.Add(child);
            system.RecordBirth(world, child);

            Assert.That(
                system.Audit(world).ActualPopulation,
                Is.EqualTo(totalBefore + 1));
            system.RecordDeath(world, child);
            var audit = system.Audit(world);
            Assert.That(audit.ActualPopulation, Is.EqualTo(totalBefore));
            Assert.That(audit.Births, Is.EqualTo(1));
            Assert.That(audit.Deaths, Is.EqualTo(1));
            Assert.That(audit.IsBalanced, Is.True);
        }

        [Test]
        public void PopulationLedger_RepositoryTracksMaterializationMigrationAndDeath()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var repository = new WorldStatePersonRepository(world);
            var system = new PopulationLedgerSystem(repository);
            var person = new PersonState
            {
                Id = "person.generated.repository_population_test",
                DisplayName = "仓储人口测试人物",
                LocationId = "location.zhuo",
                BirthLocationId = "location.zhuo",
                BirthDay = world.AbsoluteDay - 20 * 360L,
                Gender = PersonGender.Female
            };

            system.MaterializePerson(
                world, person, PopulationOccupation.Agriculture);

            Assert.That(
                repository.GetAddedPersonIds(),
                Is.EqualTo(new[] { person.Id }));
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
            repository.AcceptAddedPeople(new[] { person.Id });

            system.MoveIndependentPerson(
                world, person, "location.zhongshan");
            Assert.That(
                repository.GetChangedPersonIds(),
                Is.EqualTo(new[] { person.Id }));
            var changedBeforeAudit = repository.GetChangedPersonIds();
            Assert.That(system.Audit(world).IsBalanced, Is.True);
            Assert.That(
                repository.GetChangedPersonIds(),
                Is.EqualTo(changedBeforeAudit));
            repository.AcceptChanges(new[] { person.Id });

            system.MoveIndependentPerson(
                world, person, "location.zhongshan");
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);

            system.RecordDeath(world, person);
            Assert.That(person.IsAlive, Is.False);
            Assert.That(
                repository.GetChangedPersonIds(),
                Is.EqualTo(new[] { person.Id }));
            Assert.That(system.Audit(world).IsBalanced, Is.True);
        }

        [Test]
        public void Snapshot_MigratesVersionOneWorldToPopulationLedger()
        {
            const string legacyJson =
                "{" +
                "\"SchemaVersion\":1," +
                "\"MasterSeed\":184," +
                "\"AbsoluteDay\":0," +
                "\"Segment\":0," +
                "\"Revision\":0," +
                "\"Locations\":[{" +
                "\"Id\":\"location.zhuo\"," +
                "\"DisplayName\":\"涿县\"," +
                "\"Population\":20000," +
                "\"PublicOrderBasisPoints\":5000," +
                "\"GrainPrice\":100," +
                "\"MapXBasisPoints\":1600," +
                "\"MapYBasisPoints\":1300" +
                "}]," +
                "\"People\":[{" +
                "\"Id\":\"person.liu_bei\"," +
                "\"DisplayName\":\"刘备\"," +
                "\"LocationId\":\"location.zhuo\"," +
                "\"PopulationOriginLocationId\":\"location.zhuo\"," +
                "\"BirthDay\":-5000," +
                "\"IsAlive\":true," +
                "\"HealthBasisPoints\":10000" +
                "}]" +
                "}";

            var loaded = WorldSnapshotSerializer.Deserialize(legacyJson);
            var audit = new PopulationLedgerSystem().Audit(loaded);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.PopulationLedgerInitialized, Is.True);
            Assert.That(audit.IsBalanced, Is.True);
            Assert.That(
                audit.ActualPopulation,
                Is.EqualTo(20_000));
        }

        [Test]
        public void PopulationLedger_ValidationRejectsTamperedLocationSummary()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            world.Locations[0].Population++;

            Assert.Throws<System.InvalidOperationException>(world.Validate);
        }

        [Test]
        public void Snapshot_RoundTripPreservesPopulationLedgerAndTransactions()
        {
            var world = new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "人口存档",
                    Age = 20,
                    Gender = PersonGender.Female,
                    Identity = StartingIdentity.Physician
                },
                184);
            var source = world.PopulationCohorts.Find(
                item =>
                    item.LocationId == "location.zhuo" &&
                    item.Occupation == PopulationOccupation.Agriculture);
            new PopulationLedgerSystem().TransferCohort(
                world,
                new StableId(source.Id),
                new StableId("location.guangzong"),
                50);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var audit = new PopulationLedgerSystem().Audit(loaded);

            Assert.That(
                loaded.PopulationCohorts.Count,
                Is.EqualTo(world.PopulationCohorts.Count));
            Assert.That(
                loaded.PopulationTransactions.Count,
                Is.EqualTo(world.PopulationTransactions.Count));
            Assert.That(audit.IsBalanced, Is.True);
            Assert.That(
                audit.ActualPopulation,
                Is.EqualTo(world.PopulationOpeningTotal));
        }

        [Test]
        public void CharacterAbility_PrototypeInitializesEveryPerson()
        {
            var world = PrototypeWorldFactory.Create184World(184);

            Assert.That(
                world.People.TrueForAll(
                    person =>
                        person.AbilityProfileInitialized &&
                        person.Aptitudes != null &&
                        person.ProfessionalSkills != null),
                Is.True);
            Assert.That(
                world.People.Find(item => item.Id == "person.guan_yu")
                    .ProfessionalSkills.MartialArts,
                Is.GreaterThan(
                    world.People.Find(item => item.Id == "person.liu_bei")
                        .ProfessionalSkills.MartialArts));
        }

        [Test]
        public void CharacterAbility_SameSeedAndIdAreDeterministic()
        {
            var first = PrototypeWorldFactory.Create184World(184)
                .People.Find(item => item.Id == "person.liu_bei");
            var second = PrototypeWorldFactory.Create184World(184)
                .People.Find(item => item.Id == "person.liu_bei");

            Assert.That(
                second.Aptitudes.Constitution,
                Is.EqualTo(first.Aptitudes.Constitution));
            Assert.That(
                second.Aptitudes.Reasoning,
                Is.EqualTo(first.Aptitudes.Reasoning));
            Assert.That(
                second.ProfessionalSkills.Military,
                Is.EqualTo(first.ProfessionalSkills.Military));
            Assert.That(second.LifeGoal, Is.EqualTo(first.LifeGoal));
        }

        [Test]
        public void CharacterAbility_DifferentSeedChangesNaturalAptitude()
        {
            var first = PrototypeWorldFactory.Create184World(184)
                .People.Find(item => item.Id == "person.liu_bei");
            var second = PrototypeWorldFactory.Create184World(185)
                .People.Find(item => item.Id == "person.liu_bei");

            var allEqual =
                first.Aptitudes.Constitution == second.Aptitudes.Constitution &&
                first.Aptitudes.Strength == second.Aptitudes.Strength &&
                first.Aptitudes.Dexterity == second.Aptitudes.Dexterity &&
                first.Aptitudes.Perception == second.Aptitudes.Perception &&
                first.Aptitudes.Memory == second.Aptitudes.Memory &&
                first.Aptitudes.Reasoning == second.Aptitudes.Reasoning &&
                first.Aptitudes.Willpower == second.Aptitudes.Willpower &&
                first.Aptitudes.Affinity == second.Aptitudes.Affinity;

            Assert.That(allEqual, Is.False);
        }

        [Test]
        public void CharacterAbility_ReinitializationDoesNotRerollPerson()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People.Find(item => item.Id == "person.liu_bei");
            var constitution = person.Aptitudes.Constitution;
            var military = person.ProfessionalSkills.Military;

            var changed = CharacterAbilityBootstrap.InitializePerson(
                world.MasterSeed,
                person,
                CharacterBackgroundKind.Farmer);

            Assert.That(changed, Is.False);
            Assert.That(person.Aptitudes.Constitution, Is.EqualTo(constitution));
            Assert.That(person.ProfessionalSkills.Military, Is.EqualTo(military));
        }

        [Test]
        public void CharacterAbility_CustomIdentityAppliesProfessionalBackground()
        {
            var setup = new NewGameSetupService();
            var soldier = setup.CreateCustom184World(
                NewGameRequest(StartingIdentity.Soldier), 184)
                .People.Find(item => item.Id == NewGameSetupService.CustomPlayerPersonId);
            var clerk = setup.CreateCustom184World(
                NewGameRequest(StartingIdentity.CountyClerk), 184)
                .People.Find(item => item.Id == NewGameSetupService.CustomPlayerPersonId);
            var merchant = setup.CreateCustom184World(
                NewGameRequest(StartingIdentity.Merchant), 184)
                .People.Find(item => item.Id == NewGameSetupService.CustomPlayerPersonId);
            var physician = setup.CreateCustom184World(
                NewGameRequest(StartingIdentity.Physician), 184)
                .People.Find(item => item.Id == NewGameSetupService.CustomPlayerPersonId);

            Assert.That(
                soldier.ProfessionalSkills.Military,
                Is.GreaterThan(clerk.ProfessionalSkills.Military));
            Assert.That(
                clerk.ProfessionalSkills.Administration,
                Is.GreaterThan(soldier.ProfessionalSkills.Administration));
            Assert.That(
                merchant.ProfessionalSkills.Commerce,
                Is.GreaterThan(clerk.ProfessionalSkills.Commerce));
            Assert.That(
                physician.ProfessionalSkills.Medicine,
                Is.GreaterThan(merchant.ProfessionalSkills.Medicine));
        }

        [Test]
        public void CharacterAbility_FiveDimensionsRecalculateFromCurrentHealth()
        {
            var person = PrototypeWorldFactory.Create184World(184)
                .People.Find(item => item.Id == "person.guan_yu");
            var healthy = StrategicAttributeCalculator.Calculate(person);

            person.HealthBasisPoints = 4_000;
            var wounded = StrategicAttributeCalculator.Calculate(person);

            Assert.That(wounded.Martial, Is.LessThan(healthy.Martial));
            Assert.That(wounded.Leadership, Is.EqualTo(healthy.Leadership));
            Assert.That(wounded.Strategy, Is.EqualTo(healthy.Strategy));
            Assert.That(
                wounded.Administration,
                Is.EqualTo(healthy.Administration));
            Assert.That(wounded.Charisma, Is.EqualTo(healthy.Charisma));
        }

        [Test]
        public void CharacterAbility_ChildInheritsAptitudeButNotProfession()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var father = world.People.Find(
                item => item.Id == "person.generated.farmer_001");
            var mother = world.People.Find(
                item => item.Id == "person.generated.farmer_002");
            var child = new PersonState
            {
                Id = "person.generated.child.ability_test",
                DisplayName = "能力测试新生儿",
                LocationId = father.LocationId,
                BirthDay = world.AbsoluteDay,
                FatherPersonId = father.Id,
                MotherPersonId = mother.Id
            };

            CharacterAbilityBootstrap.InitializeChild(
                world, child, father, mother);

            var constitutionMean =
                (father.Aptitudes.Constitution + mother.Aptitudes.Constitution) / 2;
            Assert.That(
                child.Aptitudes.Constitution,
                Is.InRange(
                    Math.Max(1_500, constitutionMean - 1_200),
                    Math.Min(9_000, constitutionMean + 1_200)));
            Assert.That(child.ProfessionalSkills.Agriculture, Is.AtMost(300));
            Assert.That(child.ProfessionalSkills.Medicine, Is.AtMost(300));
            Assert.That(child.LifeGoal, Is.EqualTo(LifeGoalKind.Unknown));
        }

        [Test]
        public void Snapshot_MigratesVersionTwoCharacterAbilities()
        {
            const string legacyJson =
                "{" +
                "\"SchemaVersion\":2," +
                "\"MasterSeed\":184," +
                "\"AbsoluteDay\":0," +
                "\"Segment\":0," +
                "\"Revision\":0," +
                "\"PopulationLedgerInitialized\":true," +
                "\"PopulationOpeningTotal\":1," +
                "\"Locations\":[{" +
                "\"Id\":\"location.zhuo\"," +
                "\"DisplayName\":\"涿县\"," +
                "\"Population\":1," +
                "\"PublicOrderBasisPoints\":5000," +
                "\"GrainPrice\":100" +
                "}]," +
                "\"People\":[{" +
                "\"Id\":\"person.liu_bei\"," +
                "\"DisplayName\":\"刘备\"," +
                "\"LocationId\":\"location.zhuo\"," +
                "\"PopulationOriginLocationId\":\"location.zhuo\"," +
                "\"BirthDay\":-5000," +
                "\"IsAlive\":true," +
                "\"HealthBasisPoints\":10000" +
                "}]" +
                "}";

            var loaded = WorldSnapshotSerializer.Deserialize(legacyJson);
            var person = loaded.People[0];

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(person.AbilityProfileInitialized, Is.True);
            Assert.That(person.Aptitudes.Willpower, Is.GreaterThan(0));
            Assert.That(person.ProfessionalSkills.Military, Is.GreaterThan(0));
        }

        [Test]
        public void Snapshot_RoundTripPreservesCharacterAbilityAndSummary()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var original = world.People.Find(item => item.Id == "person.lu_zhi");
            var originalSummary = StrategicAttributeCalculator.Calculate(original);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var restored = loaded.People.Find(item => item.Id == "person.lu_zhi");
            var restoredSummary = StrategicAttributeCalculator.Calculate(restored);

            Assert.That(
                restored.Aptitudes.Reasoning,
                Is.EqualTo(original.Aptitudes.Reasoning));
            Assert.That(
                restored.ProfessionalSkills.Scholarship,
                Is.EqualTo(original.ProfessionalSkills.Scholarship));
            Assert.That(
                restoredSummary.Leadership,
                Is.EqualTo(originalSummary.Leadership));
            Assert.That(
                restoredSummary.Administration,
                Is.EqualTo(originalSummary.Administration));
        }

        [Test]
        public void CharacterAbility_ValidationRejectsOutOfRangeValue()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            world.People[0].Aptitudes.Memory = 10_001;

            Assert.Throws<InvalidOperationException>(world.Validate);
        }

        [Test]
        public void Education_GrowsOnlyAfterThirtyRealDaysAndOnlyOnce()
        {
            var world = PrepareEducationWorld(1_000, 8_000);
            var student = FindTestStudent(world);
            var before = student.ProfessionalSkills.Military;
            var system = new EducationSystem();
            system.StartPlan(
                world,
                new StableId(student.Id),
                ProfessionalDiscipline.Military,
                10,
                "person.liu_bei");

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 29);
            Assert.That(
                student.ProfessionalSkills.Military,
                Is.EqualTo(before));
            Assert.That(world.LearningRecords.Count, Is.EqualTo(0));

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            var after = student.ProfessionalSkills.Military;
            Assert.That(after, Is.GreaterThan(before));
            Assert.That(world.LearningRecords.Count, Is.EqualTo(1));
            Assert.That(world.EducationPlans[0].TotalStudyDays, Is.EqualTo(10));

            system.ResolveDuePlans(world);
            Assert.That(student.ProfessionalSkills.Military, Is.EqualTo(after));
            Assert.That(world.LearningRecords.Count, Is.EqualTo(1));
        }

        [Test]
        public void Education_PersonalFeeTransfersToTeacher()
        {
            var world = PrepareEducationWorld(1_000, 8_000);
            var student = FindTestStudent(world);
            var teacher = world.People.Find(
                item => item.Id == "person.liu_bei");
            var system = new EducationSystem();
            var plan = system.StartPlan(
                world,
                new StableId(student.Id),
                ProfessionalDiscipline.Military,
                12,
                teacher.Id);
            var studentBefore = student.Wealth;
            var teacherBefore = teacher.Wealth;

            ResolveEducationAtDay(world, 30, system);

            Assert.That(
                student.Wealth,
                Is.EqualTo(studentBefore - plan.MonthlyFee));
            Assert.That(
                teacher.Wealth,
                Is.EqualTo(teacherBefore + plan.MonthlyFee));
            Assert.That(plan.TotalFeesPaid, Is.EqualTo(plan.MonthlyFee));
            Assert.That(
                world.LearningRecords[0].FeePaid,
                Is.EqualTo(plan.MonthlyFee));
        }

        [Test]
        public void Education_FamilyFundingConsumesRealFamilyWealth()
        {
            var world = PrepareEducationWorld(1_000, 8_000);
            var student = FindTestStudent(world);
            var family = world.Families.Find(
                item => item.Id == "family.zhuo_farm_household");
            family.Wealth = 5_000;
            var teacher = world.People.Find(
                item => item.Id == "person.liu_bei");
            var system = new EducationSystem();
            var plan = system.StartPlan(
                world,
                new StableId(student.Id),
                ProfessionalDiscipline.Military,
                10,
                teacher.Id,
                EducationFundingSource.Family,
                family.Id);
            var familyBefore = family.Wealth;
            var studentBefore = student.Wealth;

            ResolveEducationAtDay(world, 30, system);

            Assert.That(
                family.Wealth,
                Is.EqualTo(familyBefore - plan.MonthlyFee));
            Assert.That(student.Wealth, Is.EqualTo(studentBefore));
        }

        [Test]
        public void Education_InsufficientFundsProducesNoGrowthOrPayment()
        {
            var world = PrepareEducationWorld(1_000, 8_000);
            var student = FindTestStudent(world);
            var teacher = world.People.Find(
                item => item.Id == "person.liu_bei");
            student.Wealth = 0;
            var system = new EducationSystem();
            system.StartPlan(
                world,
                new StableId(student.Id),
                ProfessionalDiscipline.Military,
                10,
                teacher.Id);
            var skillBefore = student.ProfessionalSkills.Military;
            var teacherWealth = teacher.Wealth;

            ResolveEducationAtDay(world, 30, system);

            Assert.That(
                student.ProfessionalSkills.Military,
                Is.EqualTo(skillBefore));
            Assert.That(teacher.Wealth, Is.EqualTo(teacherWealth));
            Assert.That(
                world.LearningRecords[0].Outcome,
                Is.EqualTo(LearningOutcomeKind.InsufficientFunds));
            Assert.That(world.EducationPlans[0].TotalStudyDays, Is.EqualTo(0));
        }

        [Test]
        public void Education_InjectedRepositoryPreservesFactsAndTracksParticipants()
        {
            var inline = PrepareEducationWorld(1_000, 8_000);
            var accessed = PrepareEducationWorld(1_000, 8_000);
            var repository = new WorldStatePersonRepository(accessed);
            var inlineSystem = new EducationSystem();
            var accessedSystem = new EducationSystem(repository);

            StartMilitaryStudy(inline, inlineSystem, "person.liu_bei");
            StartMilitaryStudy(accessed, accessedSystem, "person.liu_bei");
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);

            ResolveEducationAtDay(inline, 30, inlineSystem);
            ResolveEducationAtDay(accessed, 30, accessedSystem);

            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            Assert.That(
                repository.GetChangedPersonIds(),
                Is.EqualTo(new[]
                {
                    "person.generated.farmer_001",
                    "person.liu_bei"
                }));
            Assert.That(repository.GetAddedPersonIds(), Is.Empty);
        }

        [Test]
        public void Education_ReadOnlyPlanningAndFailedSettlementStayClean()
        {
            var world = PrepareEducationWorld(1_000, 8_000);
            var student = FindTestStudent(world);
            student.Wealth = 0;
            var repository = new WorldStatePersonRepository(world);
            var system = new EducationSystem(repository);

            var teacher = system.FindBestTeacher(
                world,
                student.Id,
                ProfessionalDiscipline.Military);
            Assert.That(teacher, Is.Not.Null);
            system.StartPlan(
                world,
                new StableId(student.Id),
                ProfessionalDiscipline.Military,
                10,
                teacher.Id);
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);

            ResolveEducationAtDay(world, 30, system);

            Assert.That(
                world.LearningRecords[0].Outcome,
                Is.EqualTo(LearningOutcomeKind.InsufficientFunds));
            Assert.That(repository.GetAddedPersonIds(), Is.Empty);
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
        }

        [Test]
        public void Education_InjectedSimulatorMatchesInlineMonthlyLearning()
        {
            var inline = PrepareEducationWorld(1_000, 8_000);
            var accessed = PrepareEducationWorld(1_000, 8_000);
            StartMilitaryStudy(
                inline, new EducationSystem(), "person.liu_bei");
            StartMilitaryStudy(
                accessed, new EducationSystem(), "person.liu_bei");
            var repository = new WorldStatePersonRepository(accessed);

            new WorldSimulator(inline.MasterSeed).AdvanceDays(inline, 30);
            new WorldSimulator(
                accessed.MasterSeed, null, repository).AdvanceDays(accessed, 30);

            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            Assert.That(
                repository.GetChangedPersonIds(),
                Does.Contain("person.generated.farmer_001"));
            Assert.That(
                repository.GetChangedPersonIds(),
                Does.Contain("person.liu_bei"));
        }

        [Test]
        public void Education_HigherAptitudeProducesMoreGrowth()
        {
            var lowWorld = PrepareEducationWorld(1_000, 8_000);
            var highWorld = PrepareEducationWorld(1_000, 8_000);
            SetMilitaryAptitude(FindTestStudent(lowWorld), 2_000);
            SetMilitaryAptitude(FindTestStudent(highWorld), 8_000);
            var lowSystem = new EducationSystem();
            var highSystem = new EducationSystem();
            StartMilitaryStudy(lowWorld, lowSystem, string.Empty);
            StartMilitaryStudy(highWorld, highSystem, string.Empty);

            ResolveEducationAtDay(lowWorld, 30, lowSystem);
            ResolveEducationAtDay(highWorld, 30, highSystem);

            Assert.That(
                highWorld.LearningRecords[0].SkillGain,
                Is.GreaterThan(lowWorld.LearningRecords[0].SkillGain));
        }

        [Test]
        public void Education_DiminishingReturnsReduceHighSkillGrowth()
        {
            var lowWorld = PrepareEducationWorld(1_000, 9_500);
            var highWorld = PrepareEducationWorld(5_000, 9_500);
            var lowSystem = new EducationSystem();
            var highSystem = new EducationSystem();
            StartMilitaryStudy(lowWorld, lowSystem, "person.liu_bei");
            StartMilitaryStudy(highWorld, highSystem, "person.liu_bei");

            ResolveEducationAtDay(lowWorld, 30, lowSystem);
            ResolveEducationAtDay(highWorld, 30, highSystem);

            Assert.That(
                lowWorld.LearningRecords[0].SkillGain,
                Is.GreaterThan(highWorld.LearningRecords[0].SkillGain));
            Assert.That(
                lowWorld.LearningRecords[0].DiminishingFactorBasisPoints,
                Is.GreaterThan(
                    highWorld.LearningRecords[0]
                        .DiminishingFactorBasisPoints));
        }

        [Test]
        public void Education_MatchingPracticePositionImprovesGrowth()
        {
            var theoryWorld = PrepareEducationWorld(4_000, 9_500);
            var practiceWorld = PrepareEducationWorld(4_000, 9_500);
            AddMilitaryPracticeMembership(practiceWorld);
            var theorySystem = new EducationSystem();
            var practiceSystem = new EducationSystem();
            StartMilitaryStudy(
                theoryWorld, theorySystem, "person.liu_bei");
            StartMilitaryStudy(
                practiceWorld,
                practiceSystem,
                "person.liu_bei",
                "position.youzhou_soldier");

            ResolveEducationAtDay(theoryWorld, 30, theorySystem);
            ResolveEducationAtDay(practiceWorld, 30, practiceSystem);

            Assert.That(
                practiceWorld.LearningRecords[0].SkillGain,
                Is.GreaterThan(theoryWorld.LearningRecords[0].SkillGain));
            Assert.That(
                practiceWorld.LearningRecords[0].PracticeFactorBasisPoints,
                Is.EqualTo(12_000));
        }

        [Test]
        public void Education_ExpertStageRequiresRealPracticePosition()
        {
            var world = PrepareEducationWorld(6_100, 9_500);
            var student = FindTestStudent(world);
            var system = new EducationSystem();

            Assert.Throws<InvalidOperationException>(
                () => system.StartPlan(
                    world,
                    new StableId(student.Id),
                    ProfessionalDiscipline.Military,
                    10,
                    "person.liu_bei"));

            AddMilitaryPracticeMembership(world);
            var plan = system.StartPlan(
                world,
                new StableId(student.Id),
                ProfessionalDiscipline.Military,
                10,
                "person.liu_bei",
                EducationFundingSource.Personal,
                string.Empty,
                "position.youzhou_soldier");
            Assert.That(plan.PracticePositionId, Is.Not.Empty);
        }

        [Test]
        public void Education_SelfStudyStopsAtThirty()
        {
            var world = PrepareEducationWorld(2_950, 8_000);
            var student = FindTestStudent(world);
            var system = new EducationSystem();
            var plan = system.StartPlan(
                world,
                new StableId(student.Id),
                ProfessionalDiscipline.Military,
                20);

            ResolveEducationAtDay(world, 30, system);

            Assert.That(
                student.ProfessionalSkills.Military,
                Is.EqualTo(EducationSystem.SelfStudyLimitBasisPoints));
            Assert.That(plan.Status, Is.EqualTo(EducationPlanStatus.Completed));
        }

        [Test]
        public void Education_DeadTeacherSuspendsPlanWithoutGrowth()
        {
            var world = PrepareEducationWorld(1_000, 8_000);
            var student = FindTestStudent(world);
            var teacher = world.People.Find(
                item => item.Id == "person.liu_bei");
            var system = new EducationSystem();
            var plan = system.StartPlan(
                world,
                new StableId(student.Id),
                ProfessionalDiscipline.Military,
                10,
                teacher.Id);
            new PopulationLedgerSystem().RecordDeath(world, teacher);
            var skillBefore = student.ProfessionalSkills.Military;

            ResolveEducationAtDay(world, 30, system);

            Assert.That(
                student.ProfessionalSkills.Military,
                Is.EqualTo(skillBefore));
            Assert.That(plan.Status, Is.EqualTo(EducationPlanStatus.Suspended));
            Assert.That(
                world.LearningRecords[0].Outcome,
                Is.EqualTo(LearningOutcomeKind.TeacherUnavailable));
        }

        [Test]
        public void Education_TravelingStudentSkipsStudyMonth()
        {
            var world = PrepareEducationWorld(1_000, 8_000);
            var student = FindTestStudent(world);
            var system = new EducationSystem();
            system.StartPlan(
                world,
                new StableId(student.Id),
                ProfessionalDiscipline.Military,
                10,
                "person.liu_bei");
            new TravelSystem().StartJourney(
                world,
                new StableId(student.Id),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhongshan"),
                TravelMode.Foot);
            var skillBefore = student.ProfessionalSkills.Military;

            ResolveEducationAtDay(world, 30, system);

            Assert.That(
                student.ProfessionalSkills.Military,
                Is.EqualTo(skillBefore));
            Assert.That(
                world.LearningRecords[0].Outcome,
                Is.EqualTo(LearningOutcomeKind.StudentUnavailable));
            Assert.That(world.LearningRecords[0].FeePaid, Is.EqualTo(0));
        }

        [Test]
        public void Education_IdenticalStateProducesIdenticalGrowth()
        {
            var first = PrepareEducationWorld(1_000, 8_000);
            var second = PrepareEducationWorld(1_000, 8_000);
            var firstSystem = new EducationSystem();
            var secondSystem = new EducationSystem();
            StartMilitaryStudy(first, firstSystem, "person.liu_bei");
            StartMilitaryStudy(second, secondSystem, "person.liu_bei");

            ResolveEducationAtDay(first, 30, firstSystem);
            ResolveEducationAtDay(second, 30, secondSystem);

            Assert.That(
                second.LearningRecords[0].SkillGain,
                Is.EqualTo(first.LearningRecords[0].SkillGain));
            Assert.That(
                second.LearningRecords[0].DiminishingFactorBasisPoints,
                Is.EqualTo(
                    first.LearningRecords[0].DiminishingFactorBasisPoints));
            Assert.That(
                WorldSnapshotSerializer.Serialize(second),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(first)));
        }

        [Test]
        public void Education_RejectsInvalidFamilyAndPracticeReferences()
        {
            var world = PrepareEducationWorld(1_000, 8_000);
            var student = FindTestStudent(world);
            var system = new EducationSystem();

            Assert.Throws<InvalidOperationException>(
                () => system.StartPlan(
                    world,
                    new StableId(student.Id),
                    ProfessionalDiscipline.Military,
                    10,
                    "person.liu_bei",
                    EducationFundingSource.Family,
                    "family.missing"));
            Assert.Throws<InvalidOperationException>(
                () => system.StartPlan(
                    world,
                    new StableId(student.Id),
                    ProfessionalDiscipline.Military,
                    10,
                    "person.liu_bei",
                    EducationFundingSource.Personal,
                    string.Empty,
                    "position.missing"));
        }

        [Test]
        public void Education_RejectsDuplicatePlanAndFourthStudent()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var teacher = world.People.Find(
                item => item.Id == "person.liu_bei");
            ProfessionalSkillAccess.Set(
                teacher.ProfessionalSkills,
                ProfessionalDiscipline.Military,
                9_500);
            var studentIds = new[]
            {
                "person.guan_yu",
                "person.zhang_fei",
                "person.jian_yong",
                "person.generated.farmer_001"
            };
            var system = new EducationSystem();
            for (var i = 0; i < studentIds.Length; i++)
            {
                var student = world.People.Find(
                    item => item.Id == studentIds[i]);
                student.Wealth = 5_000;
                ProfessionalSkillAccess.Set(
                    student.ProfessionalSkills,
                    ProfessionalDiscipline.Military,
                    1_000);
            }

            system.StartPlan(
                world,
                new StableId(studentIds[0]),
                ProfessionalDiscipline.Military,
                10,
                teacher.Id);
            Assert.Throws<InvalidOperationException>(
                () => system.StartPlan(
                    world,
                    new StableId(studentIds[0]),
                    ProfessionalDiscipline.Scholarship,
                    10));
            system.StartPlan(
                world,
                new StableId(studentIds[1]),
                ProfessionalDiscipline.Military,
                10,
                teacher.Id);
            system.StartPlan(
                world,
                new StableId(studentIds[2]),
                ProfessionalDiscipline.Military,
                10,
                teacher.Id);
            Assert.Throws<InvalidOperationException>(
                () => system.StartPlan(
                    world,
                    new StableId(studentIds[3]),
                    ProfessionalDiscipline.Military,
                    10,
                    teacher.Id));
        }

        [Test]
        public void Education_SnapshotRoundTripPreservesPlanAndRecords()
        {
            var world = PrepareEducationWorld(1_000, 8_000);
            var system = new EducationSystem();
            StartMilitaryStudy(world, system, "person.liu_bei");
            ResolveEducationAtDay(world, 30, system);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.EducationPlans.Count, Is.EqualTo(1));
            Assert.That(loaded.LearningRecords.Count, Is.EqualTo(1));
            Assert.That(
                loaded.EducationPlans[0].TotalStudyDays,
                Is.EqualTo(world.EducationPlans[0].TotalStudyDays));
            Assert.That(
                loaded.LearningRecords[0].SkillGain,
                Is.EqualTo(world.LearningRecords[0].SkillGain));
        }

        [Test]
        public void Snapshot_MigratesVersionThreeEducationCollections()
        {
            const string legacyJson =
                "{" +
                "\"SchemaVersion\":3," +
                "\"MasterSeed\":184," +
                "\"AbsoluteDay\":0," +
                "\"Segment\":0," +
                "\"Revision\":0," +
                "\"PopulationLedgerInitialized\":true," +
                "\"PopulationOpeningTotal\":1," +
                "\"Locations\":[{" +
                "\"Id\":\"location.zhuo\"," +
                "\"DisplayName\":\"涿县\"," +
                "\"Population\":1," +
                "\"PublicOrderBasisPoints\":5000," +
                "\"GrainPrice\":100" +
                "}]," +
                "\"People\":[{" +
                "\"Id\":\"person.liu_bei\"," +
                "\"DisplayName\":\"刘备\"," +
                "\"LocationId\":\"location.zhuo\"," +
                "\"PopulationOriginLocationId\":\"location.zhuo\"," +
                "\"BirthDay\":-5000," +
                "\"IsAlive\":true," +
                "\"HealthBasisPoints\":10000" +
                "}]" +
                "}";

            var loaded = WorldSnapshotSerializer.Deserialize(legacyJson);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.EducationPlans, Is.Not.Null);
            Assert.That(loaded.LearningRecords, Is.Not.Null);
            Assert.That(loaded.EducationPlans.Count, Is.EqualTo(0));
        }

        [Test]
        public void MilitaryService_PrototypeCreatesRealPersonnelAndFormations()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var populationAudit = new PopulationLedgerSystem().Audit(world);
            var military = new MilitaryServiceSystem();

            Assert.That(world.MilitaryServiceInitialized, Is.True);
            Assert.That(world.MilitaryServices.Count, Is.EqualTo(240));
            Assert.That(world.MilitaryFormations.Count, Is.EqualTo(9));
            Assert.That(
                world.People.FindAll(
                    item => item.Id.StartsWith("person.military.")).Count,
                Is.EqualTo(237));
            Assert.That(populationAudit.IsBalanced, Is.True);
            Assert.That(
                populationAudit.ActualPopulation,
                Is.EqualTo(populationAudit.OpeningPopulation));
            for (var i = 0; i < world.Armies.Count; i++)
            {
                var audit = military.AuditArmy(
                    world, new StableId(world.Armies[i].Id));
                Assert.That(audit.Total, Is.EqualTo(80));
                Assert.That(audit.Available, Is.EqualTo(80));
                Assert.That(world.Armies[i].Troops, Is.EqualTo(80));
            }
        }

        [Test]
        public void MilitaryService_InjectedRepositoryTracksPrototypeRecruits()
        {
            var inline = PrototypeWorldFactory.Create184World(184);
            WorldStatePersonRepository repository = null;
            var accessed = PrototypeWorldFactory.Create184World(
                184,
                world =>
                {
                    repository = new WorldStatePersonRepository(world);
                    return repository;
                });

            Assert.That(repository, Is.Not.Null);
            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            var addedPeople = repository.GetAddedPersonIds();
            Assert.That(addedPeople.Count, Is.EqualTo(237));
            for (var i = 0; i < addedPeople.Count; i++)
            {
                Assert.That(
                    addedPeople[i],
                    Does.StartWith("person.military."));
            }

            Assert.That(
                repository.GetChangedPersonIds(),
                Is.EqualTo(new[] { "person.zou_jing" }));
            Assert.That(
                repository.GetRequired("person.zou_jing").LocationId,
                Is.EqualTo("location.zhongshan"));
        }

        [Test]
        public void MilitaryEquipment_PrototypeCreatesAuditableArmoriesAndIssues()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var equipment = new MilitaryEquipmentSystem();

            Assert.That(
                world.SchemaVersion, Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(world.MilitaryEquipmentInitialized, Is.True);
            Assert.That(world.MilitaryEquipmentDefinitions.Count, Is.EqualTo(6));
            Assert.That(
                world.MilitaryArmoryStocks.Count,
                Is.EqualTo(world.Armies.Count * 6));
            Assert.That(world.MilitaryEquipmentIssues.Count, Is.GreaterThan(0));
            Assert.That(
                world.MilitaryEquipmentTransactions.Count,
                Is.GreaterThan(world.MilitaryEquipmentIssues.Count));
            for (var i = 0; i < world.Armies.Count; i++)
            {
                var audit = equipment.AuditArmy(world, world.Armies[i].Id);
                Assert.That(audit.Opening, Is.GreaterThan(0));
                Assert.That(audit.IsBalanced, Is.True);
            }

            world.Validate();
        }

        [Test]
        public void MilitaryEquipment_TroopTypeDerivesFromEquipmentAndAbility()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var equipment = new MilitaryEquipmentSystem();
            MilitaryServiceState archer = null;
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                var troop = equipment.DeriveTroop(world, service);
                if (troop.TroopTypeId == MilitaryEquipmentSystem.ArcherTroopId)
                {
                    archer = service;
                    Assert.That(troop.MeetsMinimumEquipment, Is.True);
                    break;
                }
            }

            Assert.That(archer, Is.Not.Null);
            var person = world.People.Find(item => item.Id == archer.PersonId);
            person.Aptitudes.Dexterity = 0;
            person.Aptitudes.Perception = 0;

            var reduced = equipment.DeriveTroop(world, archer);

            Assert.That(
                reduced.TroopTypeId,
                Is.EqualTo(MilitaryEquipmentSystem.LightInfantryTroopId));
            Assert.That(reduced.MeetsMinimumEquipment, Is.False);
        }

        [Test]
        public void MilitaryEquipment_ReadinessExcludesSupportAndAggregatesFormations()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var equipment = new MilitaryEquipmentSystem();
            var army = world.Armies[0];
            var armyReport = equipment.BuildReadinessReport(world, army.Id);
            var formationCombat = 0;
            var formationReady = 0;
            var activeServices = 0;
            for (var i = 0; i < world.MilitaryFormations.Count; i++)
            {
                var formation = world.MilitaryFormations[i];
                if (formation.ArmyId != army.Id)
                {
                    continue;
                }

                var report = equipment.BuildReadinessReport(
                    world, army.Id, formation.Id);
                formationCombat += report.CombatMembers;
                formationReady += report.ReadyMembers;
            }

            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.ArmyId == army.Id &&
                    (service.Status == MilitaryServiceStatus.Active ||
                     service.Status == MilitaryServiceStatus.Mustering))
                {
                    activeServices++;
                }
            }

            var troopCount = 0;
            foreach (var pair in armyReport.TroopCounts)
            {
                troopCount += pair.Value;
            }

            Assert.That(armyReport.CombatMembers, Is.EqualTo(formationCombat));
            Assert.That(armyReport.ReadyMembers, Is.EqualTo(formationReady));
            Assert.That(troopCount, Is.EqualTo(activeServices));
            Assert.That(armyReport.CombatMembers, Is.LessThan(activeServices));
            Assert.That(armyReport.ReadinessBasisPoints, Is.InRange(0, 10_000));
        }

        [Test]
        public void MilitaryEquipment_ReadinessAppliesBoundedBattlePowerModifier()
        {
            const long basePower = 100_000;

            Assert.That(
                BattleResolver.ApplyEquipmentReadinessModifier(
                    basePower, 10_000),
                Is.EqualTo(basePower));
            Assert.That(
                BattleResolver.ApplyEquipmentReadinessModifier(
                    basePower, 5_000),
                Is.EqualTo(90_000));
            Assert.That(
                BattleResolver.ApplyEquipmentReadinessModifier(
                    basePower, 0),
                Is.EqualTo(80_000));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BattleResolver.ApplyEquipmentReadinessModifier(
                    basePower, 10_001));
        }

        [Test]
        public void MilitaryEquipment_BattleRecordsReadinessAndDisposesDeadIssues()
        {
            var world = BuildGuangzongBattleWorld();
            var equipment = new MilitaryEquipmentSystem();
            var attacker = "army.han_jizhou_vanguard";
            var defender = "army.yellow_turban_guangzong";
            var attackerReadiness = equipment.BuildReadinessReport(
                world, attacker).ReadinessBasisPoints;
            var defenderReadiness = equipment.BuildReadinessReport(
                world, defender).ReadinessBasisPoints;
            var transactionCount = world.MilitaryEquipmentTransactions.Count;

            var outcome = new BattleResolver(world.MasterSeed).Resolve(
                world,
                new StableId("person.guo_dian"),
                new StableId(attacker),
                new StableId(defender));

            Assert.That(
                outcome.Record.AttackerEquipmentReadinessBasisPoints,
                Is.EqualTo(attackerReadiness));
            Assert.That(
                outcome.Record.DefenderEquipmentReadinessBasisPoints,
                Is.EqualTo(defenderReadiness));
            Assert.That(
                world.MilitaryEquipmentTransactions.Count,
                Is.GreaterThan(transactionCount));
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.Status != MilitaryServiceStatus.Dead)
                {
                    continue;
                }

                Assert.That(
                    world.MilitaryEquipmentIssues.Exists(
                        item => item.MilitaryServiceId == service.Id),
                    Is.False);
            }

            Assert.That(equipment.AuditArmy(world, attacker).IsBalanced, Is.True);
            Assert.That(equipment.AuditArmy(world, defender).IsBalanced, Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryEquipment_DataDrivenDefinitionDoesNotChangeSchema()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var schema = world.SchemaVersion;
            const string equipmentId = "mod.example.equipment.test_staff";
            world.MilitaryEquipmentDefinitions.Add(
                new MilitaryEquipmentDefinitionState
                {
                    Id = equipmentId,
                    DisplayName = "试制长杖",
                    CategoryId = "mod.example.equipment_category.staff",
                    SlotId = "equipment_slot.main_hand",
                    ProductDefinitionId =
                        "product.mod_example.equipment.test_staff",
                    UnitWeight = 4,
                    MaximumConditionBasisPoints = 10_000,
                    MeleePowerBasisPoints = 2_000
                });
            for (var i = 0; i < world.Armies.Count; i++)
            {
                world.MilitaryArmoryStocks.Add(new MilitaryArmoryStockState
                {
                    Id = "armory_stock." + world.Armies[i].Id + "." +
                         equipmentId,
                    ArmyId = world.Armies[i].Id,
                    EquipmentDefinitionId = equipmentId,
                    AverageConditionBasisPoints = 10_000
                });
            }

            world.Validate();

            Assert.That(world.SchemaVersion, Is.EqualTo(schema));
            Assert.That(
                world.MilitaryEquipmentDefinitions.Exists(
                    item => item.Id == equipmentId),
                Is.True);
        }

        [Test]
        public void MilitaryEquipment_ValidationRejectsStockAndLedgerTampering()
        {
            var negativeWorld = PrototypeWorldFactory.Create184World(184);
            negativeWorld.MilitaryArmoryStocks[0].AvailableQuantity = -1;
            Assert.Throws<InvalidOperationException>(negativeWorld.Validate);

            var openingWorld = PrototypeWorldFactory.Create184World(184);
            openingWorld.MilitaryArmoryStocks[0].OpeningQuantity++;
            Assert.Throws<InvalidOperationException>(openingWorld.Validate);

            var issueWorld = PrototypeWorldFactory.Create184World(184);
            issueWorld.MilitaryEquipmentIssues[0].PersonId = "person.liu_bei";
            Assert.Throws<InvalidOperationException>(issueWorld.Validate);
        }

        [Test]
        public void Snapshot_MigratesVersionTwelveWithoutFabricatingEquipment()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var people = world.People.Count;
            var families = world.Families.Count;
            var services = world.MilitaryServices.Count;
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 12");
            var equipmentStart = json.IndexOf(
                "  \"MilitaryEquipmentInitialized\":",
                StringComparison.Ordinal);
            var villagesStart = json.IndexOf(
                "  \"Villages\":", StringComparison.Ordinal);
            Assert.That(equipmentStart, Is.GreaterThan(0));
            Assert.That(villagesStart, Is.GreaterThan(equipmentStart));
            json = json.Substring(0, equipmentStart) +
                   json.Substring(villagesStart);

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(
                loaded.SchemaVersion, Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryEquipmentInitialized, Is.False);
            Assert.That(loaded.MilitaryEquipmentDefinitions, Is.Empty);
            Assert.That(loaded.MilitaryArmoryStocks, Is.Empty);
            Assert.That(loaded.MilitaryEquipmentIssues, Is.Empty);
            Assert.That(loaded.MilitaryEquipmentTransactions, Is.Empty);
            Assert.That(loaded.People.Count, Is.EqualTo(people));
            Assert.That(loaded.Families.Count, Is.EqualTo(families));
            Assert.That(loaded.MilitaryServices.Count, Is.EqualTo(services));
            loaded.Validate();
        }

        [Test]
        public void MilitaryAuthority_RecordsAuthorizedAndRejectedOrders()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var army = world.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var officer = world.MilitaryServices.Find(
                item =>
                    item.ArmyId == army.Id &&
                    item.Role == MilitaryServiceRole.Officer);
            var soldier = world.MilitaryServices.Find(
                item =>
                    item.ArmyId == army.Id &&
                    item.Role == MilitaryServiceRole.Soldier);
            var authority = new MilitaryAuthoritySystem();

            var commanderOrder = authority.IssueOrder(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(army.Id),
                MilitaryOrderType.March,
                MilitaryAuthorityLevel.Army,
                targetLocationId: "location.guangzong");
            var officerOrder = authority.IssueOrder(
                world,
                new StableId(officer.PersonId),
                new StableId(army.Id),
                MilitaryOrderType.March,
                MilitaryAuthorityLevel.Army,
                targetLocationId: "location.guangzong");
            var soldierOrder = authority.IssueOrder(
                world,
                new StableId(soldier.PersonId),
                new StableId(army.Id),
                MilitaryOrderType.Engage,
                MilitaryAuthorityLevel.Army,
                targetArmyId: "army.yellow_turban_guangzong");
            var outsiderOrder = authority.IssueOrder(
                world,
                new StableId("person.liu_bei"),
                new StableId(army.Id),
                MilitaryOrderType.Retreat,
                MilitaryAuthorityLevel.Army);

            Assert.That(
                commanderOrder.Result,
                Is.EqualTo(MilitaryOrderResult.Authorized));
            Assert.That(
                officerOrder.ActualAuthority,
                Is.EqualTo(MilitaryAuthorityLevel.Formation));
            Assert.That(
                officerOrder.Result,
                Is.EqualTo(MilitaryOrderResult.Rejected));
            Assert.That(
                soldierOrder.ActualAuthority,
                Is.EqualTo(MilitaryAuthorityLevel.Self));
            Assert.That(
                outsiderOrder.ActualAuthority,
                Is.EqualTo(MilitaryAuthorityLevel.None));
            Assert.That(world.MilitaryOrders.Count, Is.EqualTo(4));
        }

        [Test]
        public void MilitaryAuthority_UnavailableCommanderCannotCommand()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var army = world.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var commander = world.MilitaryServices.Find(
                item => item.PersonId == army.CommanderPersonId);
            commander.Status = MilitaryServiceStatus.Wounded;
            commander.LastStatusChangeDay = world.AbsoluteDay;
            new MilitaryServiceSystem().SynchronizeArmyCaches(world, army.Id);
            world.Validate();

            var order = new MilitaryAuthoritySystem().IssueOrder(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(army.Id),
                MilitaryOrderType.March,
                MilitaryAuthorityLevel.Army,
                targetLocationId: "location.guangzong");

            Assert.That(order.Result, Is.EqualTo(MilitaryOrderResult.Rejected));
            Assert.That(
                order.ActualAuthority,
                Is.EqualTo(MilitaryAuthorityLevel.None));
        }

        [Test]
        public void Battle_CasualtiesMapToPeopleAndPopulationLedger()
        {
            var world = BuildGuangzongBattleWorld();
            var military = new MilitaryServiceSystem();
            var population = new PopulationLedgerSystem();
            var deathsBefore = population.Audit(world).Deaths;

            var outcome = new BattleResolver(world.MasterSeed).Resolve(
                world,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            var totalDead =
                outcome.Record.AttackerCasualties -
                outcome.Record.AttackerWounded +
                outcome.Record.DefenderCasualties -
                outcome.Record.DefenderWounded;
            var hanAudit = military.AuditArmy(
                world, new StableId(outcome.Record.AttackerArmyId));
            var yellowAudit = military.AuditArmy(
                world, new StableId(outcome.Record.DefenderArmyId));
            var populationAudit = population.Audit(world);

            Assert.That(
                hanAudit.Wounded + yellowAudit.Wounded,
                Is.EqualTo(
                    outcome.Record.AttackerWounded +
                    outcome.Record.DefenderWounded));
            Assert.That(
                hanAudit.Dead + yellowAudit.Dead,
                Is.EqualTo(totalDead));
            Assert.That(
                populationAudit.Deaths - deathsBefore,
                Is.EqualTo(totalDead));
            Assert.That(populationAudit.IsBalanced, Is.True);
        }

        [Test]
        public void Battle_InjectedRepositoryTracksEveryConcreteCasualty()
        {
            var inline = BuildGuangzongBattleWorld();
            var accessed = BuildGuangzongBattleWorld();
            var repository = new WorldStatePersonRepository(accessed);

            var inlineOutcome = new BattleResolver(inline.MasterSeed).Resolve(
                inline,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            var accessedOutcome = new BattleResolver(
                accessed.MasterSeed, repository).Resolve(
                accessed,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));

            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            Assert.That(
                accessedOutcome.Record.AttackerCasualties,
                Is.EqualTo(inlineOutcome.Record.AttackerCasualties));
            Assert.That(
                accessedOutcome.Record.DefenderCasualties,
                Is.EqualTo(inlineOutcome.Record.DefenderCasualties));
            var expectedCasualties =
                accessedOutcome.Record.AttackerCasualties +
                accessedOutcome.Record.DefenderCasualties;
            var changedPeople = repository.GetChangedPersonIds();
            Assert.That(changedPeople.Count, Is.EqualTo(expectedCasualties));
            for (var i = 0; i < changedPeople.Count; i++)
            {
                var service = accessed.MilitaryServices.Find(
                    item => item.PersonId == changedPeople[i]);
                Assert.That(service, Is.Not.Null);
                Assert.That(
                    service.Status == MilitaryServiceStatus.Wounded ||
                    service.Status == MilitaryServiceStatus.Dead,
                    Is.True);
                var person = repository.GetRequired(changedPeople[i]);
                if (service.Status == MilitaryServiceStatus.Wounded)
                {
                    Assert.That(
                        person.HealthBasisPoints,
                        Is.LessThanOrEqualTo(4_000));
                }
                else
                {
                    Assert.That(person.IsAlive, Is.False);
                }
            }

            Assert.That(repository.GetAddedPersonIds(), Is.Empty);
        }

        [Test]
        public void ArmyMarch_InjectedRepositoryTracksArrivingPersonnel()
        {
            var inline = PrototypeWorldFactory.Create184World(184);
            var accessed = PrototypeWorldFactory.Create184World(184);
            var repository = new WorldStatePersonRepository(accessed);
            var inlineArmy = inline.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var accessedArmy = accessed.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            new ArmySystem().StartMarch(
                inline,
                new StableId(inlineArmy.CommanderPersonId),
                new StableId(inlineArmy.Id),
                new StableId("route.xiaquyang_guangzong"),
                new StableId("location.guangzong"));
            new ArmySystem(repository).StartMarch(
                accessed,
                new StableId(accessedArmy.CommanderPersonId),
                new StableId(accessedArmy.Id),
                new StableId("route.xiaquyang_guangzong"),
                new StableId("location.guangzong"));

            new WorldSimulator(inline.MasterSeed).AdvanceDays(inline, 8);
            new WorldSimulator(
                accessed.MasterSeed,
                personRepository: repository).AdvanceDays(accessed, 8);

            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            Assert.That(accessed.ArmyMarches, Is.Empty);
            var changedPeople = repository.GetChangedPersonIds();
            Assert.That(changedPeople.Count, Is.EqualTo(accessedArmy.Troops));
            for (var i = 0; i < changedPeople.Count; i++)
            {
                Assert.That(
                    repository.GetRequired(changedPeople[i]).LocationId,
                    Is.EqualTo("location.guangzong"));
            }

            Assert.That(repository.GetAddedPersonIds(), Is.Empty);
        }

        [Test]
        public void Starvation_DesertionChangesSpecificServiceMembers()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var army = world.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            new ArmySystem().StartMarch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(army.Id),
                new StableId("route.xiaquyang_guangzong"),
                new StableId("location.guangzong"));
            army.Provisions = 0;
            var troopsBefore = army.Troops;

            new ArmySystem().ConsumeDailyMarchSupplies(world);
            var audit = new MilitaryServiceSystem().AuditArmy(
                world, new StableId(army.Id));

            Assert.That(audit.Deserters, Is.EqualTo(1));
            Assert.That(army.Troops, Is.EqualTo(troopsBefore - 1));
            Assert.That(
                world.MilitaryServices.Exists(
                    item =>
                        item.ArmyId == army.Id &&
                        item.Status == MilitaryServiceStatus.Deserter),
                Is.True);
        }

        [Test]
        public void MilitaryEquipment_DesertionRemovesCarriedEquipmentFromArmyAssets()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var army = world.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var issuesBefore = world.MilitaryEquipmentIssues.FindAll(
                item => item.ArmyId == army.Id).Count;
            var transactionsBefore = world.MilitaryEquipmentTransactions.Count;

            var applied = new MilitaryServiceSystem().ApplyDesertion(
                world, new StableId(army.Id), army.Troops, 77);

            Assert.That(applied, Is.EqualTo(80));
            Assert.That(issuesBefore, Is.GreaterThan(0));
            Assert.That(
                world.MilitaryEquipmentIssues.Exists(
                    item => item.ArmyId == army.Id),
                Is.False);
            Assert.That(
                world.MilitaryEquipmentTransactions.FindAll(
                    item =>
                        item.Type == MilitaryEquipmentTransactionType.Loss &&
                        item.FromArmyId == army.Id).Count,
                Is.GreaterThan(0));
            Assert.That(
                world.MilitaryEquipmentTransactions.Count,
                Is.GreaterThan(transactionsBefore));
            Assert.That(
                new MilitaryEquipmentSystem().AuditArmy(world, army.Id)
                    .IsBalanced,
                Is.True);
            world.Validate();
        }

        [Test]
        public void Starvation_DesertionDoesNotDirtyUnchangedPeople()
        {
            var inline = PrototypeWorldFactory.Create184World(184);
            var accessed = PrototypeWorldFactory.Create184World(184);
            var repository = new WorldStatePersonRepository(accessed);
            var inlineArmy = inline.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var accessedArmy = accessed.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            var inlineSystem = new ArmySystem();
            var accessedSystem = new ArmySystem(repository);
            inlineSystem.StartMarch(
                inline,
                new StableId(inlineArmy.CommanderPersonId),
                new StableId(inlineArmy.Id),
                new StableId("route.xiaquyang_guangzong"),
                new StableId("location.guangzong"));
            accessedSystem.StartMarch(
                accessed,
                new StableId(accessedArmy.CommanderPersonId),
                new StableId(accessedArmy.Id),
                new StableId("route.xiaquyang_guangzong"),
                new StableId("location.guangzong"));
            inlineArmy.Provisions = 0;
            accessedArmy.Provisions = 0;

            inlineSystem.ConsumeDailyMarchSupplies(inline);
            accessedSystem.ConsumeDailyMarchSupplies(accessed);

            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            Assert.That(repository.GetAddedPersonIds(), Is.Empty);
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
        }

        [Test]
        public void MilitarySnapshot_RoundTripPreservesFactsAndOrders()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var army = world.Armies.Find(
                item => item.Id == "army.han_jizhou_vanguard");
            new MilitaryAuthoritySystem().IssueOrder(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(army.Id),
                MilitaryOrderType.Resupply,
                MilitaryAuthorityLevel.Army);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryServiceInitialized, Is.True);
            Assert.That(
                loaded.MilitaryServices.Count,
                Is.EqualTo(world.MilitaryServices.Count));
            Assert.That(
                loaded.MilitaryFormations.Count,
                Is.EqualTo(world.MilitaryFormations.Count));
            Assert.That(loaded.MilitaryOrders.Count, Is.EqualTo(1));
            Assert.That(loaded.MilitaryEquipmentInitialized, Is.True);
            Assert.That(
                loaded.MilitaryEquipmentDefinitions.Count,
                Is.EqualTo(world.MilitaryEquipmentDefinitions.Count));
            Assert.That(
                loaded.MilitaryArmoryStocks.Count,
                Is.EqualTo(world.MilitaryArmoryStocks.Count));
            Assert.That(
                loaded.MilitaryEquipmentIssues.Count,
                Is.EqualTo(world.MilitaryEquipmentIssues.Count));
            Assert.That(
                loaded.MilitaryEquipmentTransactions.Count,
                Is.EqualTo(world.MilitaryEquipmentTransactions.Count));
            loaded.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionFourAbstractArmyWithoutFabrication()
        {
            const string legacyJson =
                "{" +
                "\"SchemaVersion\":4," +
                "\"MasterSeed\":184," +
                "\"AbsoluteDay\":0," +
                "\"Segment\":0," +
                "\"Revision\":0," +
                "\"Locations\":[{" +
                "\"Id\":\"location.legacy\"," +
                "\"DisplayName\":\"旧城\"," +
                "\"Kind\":2," +
                "\"Terrain\":1," +
                "\"StrategicImportance\":1," +
                "\"Population\":1000," +
                "\"PublicOrderBasisPoints\":5000," +
                "\"GrainPrice\":100" +
                "}]," +
                "\"People\":[{" +
                "\"Id\":\"person.legacy_commander\"," +
                "\"DisplayName\":\"旧将\"," +
                "\"LocationId\":\"location.legacy\"," +
                "\"BirthDay\":-5000," +
                "\"IsAlive\":true," +
                "\"HealthBasisPoints\":10000" +
                "}]," +
                "\"Organizations\":[{" +
                "\"Id\":\"organization.legacy\"," +
                "\"DisplayName\":\"旧军\"," +
                "\"Type\":1," +
                "\"HeadquartersLocationId\":\"location.legacy\"," +
                "\"LeaderPersonId\":\"person.legacy_commander\"," +
                "\"ReputationBasisPoints\":5000" +
                "}]," +
                "\"Armies\":[{" +
                "\"Id\":\"army.legacy\"," +
                "\"DisplayName\":\"旧军\"," +
                "\"OrganizationId\":\"organization.legacy\"," +
                "\"CommanderPersonId\":\"person.legacy_commander\"," +
                "\"LocationId\":\"location.legacy\"," +
                "\"Troops\":5000," +
                "\"WoundedTroops\":100," +
                "\"MaximumTroops\":6000," +
                "\"MoraleBasisPoints\":5000," +
                "\"TrainingBasisPoints\":5000," +
                "\"Provisions\":1000," +
                "\"IsMobilized\":true" +
                "}]" +
                "}";

            var loaded = WorldSnapshotSerializer.Deserialize(legacyJson);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.MilitaryServiceInitialized, Is.False);
            Assert.That(loaded.MilitaryServices.Count, Is.EqualTo(0));
            Assert.That(loaded.Armies.Count, Is.EqualTo(1));
            Assert.That(loaded.Armies[0].Troops, Is.EqualTo(5000));
        }

        [Test]
        public void MilitaryValidation_RejectsDuplicateServiceAndCacheTamper()
        {
            var duplicateWorld = PrototypeWorldFactory.Create184World(184);
            var source = duplicateWorld.MilitaryServices[0];
            duplicateWorld.MilitaryServices.Add(new MilitaryServiceState
            {
                Id = source.Id + ".duplicate",
                PersonId = source.PersonId,
                ArmyId = source.ArmyId,
                FormationId = source.FormationId,
                Role = source.Role,
                Rank = source.Rank,
                Status = source.Status,
                DisciplineBasisPoints = source.DisciplineBasisPoints,
                LoyaltyBasisPoints = source.LoyaltyBasisPoints,
                ServiceExperienceBasisPoints =
                    source.ServiceExperienceBasisPoints,
                EnlistedDay = source.EnlistedDay,
                LastStatusChangeDay = source.LastStatusChangeDay
            });
            Assert.Throws<InvalidOperationException>(duplicateWorld.Validate);

            var cacheWorld = PrototypeWorldFactory.Create184World(184);
            cacheWorld.Armies[0].Troops--;
            Assert.Throws<InvalidOperationException>(cacheWorld.Validate);
        }

        [Test]
        public void VillagePrototype_CreatesThreeHundredPermanentPeople()
        {
            var world = VillagePrototypeFactory.Create();
            var village = world.Villages[0];
            var audit = new VillageLifeSystem(world.MasterSeed).Audit(
                world, village.Id);
            var personIds = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < world.People.Count; i++)
            {
                Assert.That(personIds.Add(world.People[i].Id), Is.True);
                Assert.That(world.People[i].FamilyId, Is.Not.Empty);
                Assert.That(world.People[i].BirthLocationId, Is.EqualTo(village.LocationId));
            }

            Assert.That(world.People.Count, Is.EqualTo(300));
            Assert.That(world.Families.Count, Is.InRange(40, 100));
            Assert.That(audit.PermanentPeople, Is.EqualTo(300));
            Assert.That(audit.LivingResidents, Is.EqualTo(300));
            Assert.That(audit.HouseholdMembers, Is.EqualTo(300));
            Assert.That(audit.AbstractPopulation, Is.EqualTo(0));
            Assert.That(audit.IsValid, Is.True);
            Assert.That(world.PopulationCohorts.Count, Is.EqualTo(0));
            world.Validate();
        }

        [Test]
        public void VillagePrototype_SameSeedProducesSamePermanentFacts()
        {
            var first = VillagePrototypeFactory.Create(240, 7_777);
            var second = VillagePrototypeFactory.Create(240, 7_777);

            Assert.That(
                WorldSnapshotSerializer.Serialize(first),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(second)));
        }

        [Test]
        public void VillageLife_WorldSimulatorRunsMonthlySettlement()
        {
            var world = VillagePrototypeFactory.Create(200, 7_778);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 30);

            Assert.That(world.Villages[0].LastSettlementDay, Is.EqualTo(30));
            Assert.That(world.VillageLedgerEntries.Count, Is.GreaterThan(0));
            Assert.That(world.AbsoluteDay, Is.EqualTo(30));
            world.Validate();
        }

        [Test]
        public void VillageLife_InjectedRepositoryPreservesOneYearWorldFacts()
        {
            var inline = VillagePrototypeFactory.Create(200, 21_202);
            var accessed = VillagePrototypeFactory.Create(200, 21_202);
            var repository = new WorldStatePersonRepository(accessed);
            var inlineVillage = new VillageLifeSystem(inline.MasterSeed);
            var accessedVillage = new VillageLifeSystem(
                accessed.MasterSeed, null, repository);
            var inlineLife = new LifeSimulationSystem(inline.MasterSeed);
            var accessedLife = new LifeSimulationSystem(
                accessed.MasterSeed, repository);

            for (var month = 1; month <= 12; month++)
            {
                inline.AbsoluteDay = month * 30L;
                accessed.AbsoluteDay = month * 30L;
                inlineVillage.ResolveMonthly(inline);
                accessedVillage.ResolveMonthly(accessed);
                inlineLife.ResolveMonthly(inline);
                accessedLife.ResolveMonthly(accessed);
                VillageLifeSystem.RefreshAllCaches(inline);
                VillageLifeSystem.RefreshAllCaches(accessed, repository);
            }

            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            Assert.That(repository.GetChangedPersonIds(), Is.Not.Empty);
            Assert.That(
                new PopulationLedgerSystem(repository).Audit(accessed).IsBalanced,
                Is.True);
            Assert.That(
                accessedVillage.Audit(accessed, accessed.Villages[0].Id).IsValid,
                Is.True);
        }

        [Test]
        public void VillageLife_AuditsAndAttentionReportsDoNotDirtyRepository()
        {
            var world = VillagePrototypeFactory.Create(200, 21_203);
            var repository = new WorldStatePersonRepository(world);
            var village = new VillageLifeSystem(
                world.MasterSeed, null, repository);

            var audit = village.Audit(world, world.Villages[0].Id);
            var report = village.BuildAttentionReport(
                world,
                world.Villages[0].Id,
                VillageAttentionLevel.Deep);
            var populationAudit =
                new PopulationLedgerSystem(repository).Audit(world);
            VillageLifeSystem.RefreshAllCaches(
                world, repository);

            Assert.That(audit.IsValid, Is.True);
            Assert.That(report.PermanentPeople, Is.EqualTo(200));
            Assert.That(populationAudit.IsBalanced, Is.True);
            Assert.That(repository.GetAddedPersonIds(), Is.Empty);
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
        }

        [Test]
        public void VillageLife_OneYearClosesHouseholdLoopWithoutDeletingPeople()
        {
            var world = VillagePrototypeFactory.Create();
            var originalIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < world.People.Count; i++)
            {
                originalIds.Add(world.People[i].Id);
            }

            SimulateVillageMonths(world, 12);

            for (var i = 0; i < world.People.Count; i++)
            {
                originalIds.Remove(world.People[i].Id);
            }

            var types = new HashSet<VillageLedgerEntryType>();
            for (var i = 0; i < world.VillageLedgerEntries.Count; i++)
            {
                types.Add(world.VillageLedgerEntries[i].Type);
            }

            var audit = new VillageLifeSystem(world.MasterSeed).Audit(
                world, world.Villages[0].Id);
            var populationAudit = new PopulationLedgerSystem().Audit(world);
            Assert.That(originalIds, Is.Empty);
            Assert.That(world.People.Count, Is.GreaterThanOrEqualTo(300));
            Assert.That(types, Does.Contain(VillageLedgerEntryType.Planting));
            Assert.That(types, Does.Contain(VillageLedgerEntryType.Harvest));
            Assert.That(types, Does.Contain(VillageLedgerEntryType.FoodConsumption));
            Assert.That(types, Does.Contain(VillageLedgerEntryType.GrainRelief));
            Assert.That(types, Does.Contain(VillageLedgerEntryType.TaxPayment));
            Assert.That(types, Does.Contain(VillageLedgerEntryType.Corvee));
            Assert.That(types, Does.Contain(VillageLedgerEntryType.Levy));
            Assert.That(types, Does.Contain(VillageLedgerEntryType.Marriage));
            Assert.That(types, Does.Contain(VillageLedgerEntryType.Migration));
            Assert.That(world.ProductBatches.Exists(item =>
                    item.ProductDefinitionId ==
                        CoreProductionContent.HerbalMedicineMaterialProductId),
                Is.True);
            Assert.That(
                world.LifeEvents.Exists(item => item.Type == LifeEventType.Birth),
                Is.True);
            Assert.That(audit.IsValid, Is.True);
            Assert.That(populationAudit.IsBalanced, Is.True);
            world.Validate();
        }

        [Test]
        public void VillageLife_DeathKeepsPermanentPersonAndTransfersHeadship()
        {
            var world = VillagePrototypeFactory.Create(200, 10_002);
            var family = world.Families[0];
            var formerHead = world.People.Find(
                person => person.Id == family.HeadPersonId);
            formerHead.HealthBasisPoints = 0;
            world.AbsoluteDay = 30;

            new LifeSimulationSystem(world.MasterSeed).ResolveMonthly(world);
            VillageLifeSystem.RefreshAllCaches(world);

            Assert.That(formerHead.IsAlive, Is.False);
            Assert.That(world.People.Exists(item => item.Id == formerHead.Id), Is.True);
            Assert.That(family.HeadPersonId, Is.Not.EqualTo(formerHead.Id));
            Assert.That(
                world.LifeEvents.Exists(
                    item =>
                        item.Type == LifeEventType.Death &&
                        item.PrimaryPersonId == formerHead.Id),
                Is.True);
            Assert.That(
                world.LifeEvents.Exists(
                    item =>
                        item.Type == LifeEventType.Succession &&
                        item.FamilyId == family.Id),
                Is.True);
            world.Validate();
        }

        [Test]
        public void VillageLife_MissingBlacksmithCausesGreaterToolLoss()
        {
            var staffed = VillagePrototypeFactory.Create(200, 8_888);
            var missing = VillagePrototypeFactory.Create(200, 8_888);
            var smithy = missing.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.Smithy);
            var smith = missing.People.Find(
                item => item.Id == smithy.ManagerPersonId);
            new PopulationLedgerSystem().MoveIndependentPerson(
                missing, smith, missing.Villages[0].ParentLocationId);

            SimulateVillageMonths(staffed, 6);
            SimulateVillageMonths(missing, 6);

            Assert.That(
                AverageToolCondition(missing),
                Is.LessThan(AverageToolCondition(staffed)));
            Assert.That(
                missing.VillageLedgerEntries.Exists(
                    item => item.Type == VillageLedgerEntryType.ToolRepair),
                Is.False);
            Assert.That(
                staffed.VillageLedgerEntries.Exists(
                    item => item.Type == VillageLedgerEntryType.ToolRepair),
                Is.True);
        }

        [Test]
        public void VillageAttention_ChangesReportDetailButNotWorldFacts()
        {
            var world = VillagePrototypeFactory.Create(200, 9_999);
            SimulateVillageMonths(world, 2);
            var before = WorldSnapshotSerializer.Serialize(world);
            var system = new VillageLifeSystem(world.MasterSeed);
            var none = system.BuildAttentionReport(
                world, world.Villages[0].Id, VillageAttentionLevel.None);
            var deep = system.BuildAttentionReport(
                world, world.Villages[0].Id, VillageAttentionLevel.Deep);
            var after = WorldSnapshotSerializer.Serialize(world);

            Assert.That(none.PermanentPeople, Is.EqualTo(deep.PermanentPeople));
            Assert.That(none.LivingResidents, Is.EqualTo(deep.LivingResidents));
            Assert.That(none.FamilyGrain, Is.EqualTo(deep.FamilyGrain));
            Assert.That(none.HouseholdDetails.Count, Is.EqualTo(0));
            Assert.That(deep.HouseholdDetails.Count, Is.GreaterThan(0));
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void VillageSnapshot_RoundTripPreservesPermanentHouseholdFacts()
        {
            var world = VillagePrototypeFactory.Create(200, 10_001);
            SimulateVillageMonths(world, 12);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.People.Count, Is.EqualTo(world.People.Count));
            Assert.That(loaded.Families.Count, Is.EqualTo(world.Families.Count));
            Assert.That(loaded.Villages.Count, Is.EqualTo(1));
            Assert.That(
                loaded.VillageFacilities.Count,
                Is.EqualTo(world.VillageFacilities.Count));
            Assert.That(
                loaded.VillageLedgerEntries.Count,
                Is.EqualTo(world.VillageLedgerEntries.Count));
            Assert.That(
                loaded.People[0].FamilyId,
                Is.EqualTo(world.People[0].FamilyId));
            loaded.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionFiveFamilyReferencesToCurrent()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 5");

            var loaded = WorldSnapshotSerializer.Deserialize(json);
            var family = loaded.Families[0];
            var member = loaded.People.Find(
                person => person.Id == family.MemberIds[0]);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.Villages, Is.Not.Null);
            Assert.That(loaded.VillageFacilities, Is.Not.Null);
            Assert.That(loaded.VillageLedgerEntries, Is.Not.Null);
            Assert.That(member.FamilyId, Is.EqualTo(family.Id));
            Assert.That(member.BirthLocationId, Is.Not.Empty);
            loaded.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionSixToInlinePopulationContract()
        {
            var world = BuildMinimalWorld();
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 6");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(
                loaded.PopulationStorage.Mode,
                Is.EqualTo(PopulationStorageMode.InlineSnapshot));
            Assert.That(
                loaded.PopulationStorage.PermanentPersonCount,
                Is.EqualTo(loaded.People.Count));
            Assert.That(
                loaded.PopulationStorage.LivingPersonCount,
                Is.EqualTo(loaded.People.Count));
            Assert.That(loaded.PopulationStorage.PackageId, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void ProductionContent_CoreResourceMatchesBuiltInRegistry()
        {
            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "Resources",
                "Content",
                "Core",
                "Production",
                "core-production.json");
            var fromResource = ProductionContentRegistry.FromJson(
                File.ReadAllText(path));
            var builtIn = ProductionContentRegistry.CreateCore();

            Assert.That(fromResource.ResolvedHash, Is.EqualTo(builtIn.ResolvedHash));
            Assert.That(fromResource.CropCount, Is.EqualTo(1));
            Assert.That(fromResource.CropVarietyCount, Is.EqualTo(1));
            Assert.That(fromResource.QualityDimensionCount, Is.EqualTo(9));
            Assert.That(fromResource.ProductCount, Is.EqualTo(32));
            Assert.That(fromResource.RecipeCount, Is.EqualTo(16));
            Assert.That(fromResource.MethodCount, Is.EqualTo(14));
            Assert.That(fromResource.SkillCount, Is.EqualTo(9));
            Assert.That(fromResource.KnowledgeCount, Is.EqualTo(1));
            Assert.That(fromResource.TechnologyCount, Is.EqualTo(3));
            Assert.That(
                fromResource.GetRecipe(CoreProductionContent.GrowWheatRecipeId)
                    .Outputs[0].ProductDefinitionId,
                Is.EqualTo(CoreProductionContent.WheatGrainProductId));
        }

        [Test]
        public void ProductionContent_RejectsDuplicateMissingAndFreeDefinitions()
        {
            var registry = ProductionContentRegistry.CreateCore();
            var duplicate = new ProductionContentPackageDefinition
            {
                PackageId = "content.test.duplicate",
                Version = "1.0.0",
                LoadOrder = 100,
                Crops = new List<CropDefinition>
                {
                    new CropDefinition
                    {
                        Id = CoreProductionContent.WheatCropId,
                        DisplayName = "重复小麦"
                    }
                }
            };
            Assert.Throws<ProductionContentException>(
                () => registry.Register(duplicate));
            Assert.That(registry.CropCount, Is.EqualTo(1));

            var missing = BuildTestProductionPackage("content.test.missing");
            missing.Recipes[0].Outputs[0].ProductDefinitionId =
                "product.test.missing";
            Assert.Throws<ProductionContentException>(
                () => registry.Register(missing));

            var free = BuildTestProductionPackage("content.test.free");
            free.Recipes[0].Outputs[0].ProductDefinitionId =
                free.Recipes[0].Inputs[0].ProductDefinitionId;
            free.Recipes[0].Outputs[0].QuantityPerLandUnit =
                free.Recipes[0].Inputs[0].QuantityPerLandUnit;
            Assert.Throws<ProductionContentException>(
                () => registry.Register(free));
            Assert.That(registry.CropCount, Is.EqualTo(1));
        }

        [Test]
        public void ProductionContent_ModPackageNeedsNoEnumOrSchemaChange()
        {
            var registry = ProductionContentRegistry.CreateCore();
            registry.Register(ProductionContentJson.DeserializePackage(
                TestProductionModJson()));
            var world = BuildMinimalWorld();
            world.ProductionContentManifest = registry.CreateManifest();
            var schema = world.SchemaVersion;

            var json = WorldSnapshotSerializer.Serialize(world, registry);
            var loaded = WorldSnapshotSerializer.Deserialize(json, registry);

            Assert.That(schema, Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.SchemaVersion, Is.EqualTo(schema));
            Assert.That(registry.GetCrop("crop.mod_test.example").DisplayName,
                Is.EqualTo("测试作物"));
            Assert.That(registry.GetProduct("product.mod_test.example_harvest"),
                Is.Not.Null);
            Assert.That(loaded.ProductionContentManifest.Packages.Count,
                Is.EqualTo(2));
        }

        [Test]
        public void ProductionContent_HanFoodExtensionLoadsFiveCropsAndSixFoods()
        {
            var registry = LoadHanFoodProductionContent();

            Assert.That(registry.CropCount, Is.EqualTo(5));
            Assert.That(registry.CropVarietyCount, Is.EqualTo(5));
            Assert.That(registry.ProductCount, Is.EqualTo(40));
            Assert.That(registry.FoodCount, Is.EqualTo(6));
            Assert.That(registry.RecipeCount, Is.EqualTo(20));
            Assert.That(registry.MethodCount, Is.EqualTo(18));
            Assert.That(
                registry.GetFood("product.soybean").NutritionBasisPoints,
                Is.EqualTo(12_500));
            Assert.That(
                registry.GetFood(CoreProductionContent.DryRationProductId)
                    .VolumeBasisPoints,
                Is.EqualTo(8_000));
            Assert.That(registry.CreateManifest().Packages.Count, Is.EqualTo(2));
        }

        [Test]
        public void ProductionContent_HanFoodRejectsNonFoodReference()
        {
            var package = ProductionContentJson.DeserializePackage(
                File.ReadAllText(HanFoodProductionContentPath()));
            package.Foods[0].ProductDefinitionId =
                CoreProductionContent.IronMaterialProductId;
            var registry = ProductionContentRegistry.CreateCore();

            Assert.Throws<ProductionContentException>(
                () => registry.Register(package));
            Assert.That(registry.FoodCount, Is.EqualTo(0));
        }

        [Test]
        public void Snapshot_MissingProductionModReportsOriginalPackageId()
        {
            var registry = ProductionContentRegistry.CreateCore();
            registry.Register(ProductionContentJson.DeserializePackage(
                TestProductionModJson()));
            var world = BuildMinimalWorld();
            world.ProductionContentManifest = registry.CreateManifest();
            var json = WorldSnapshotSerializer.Serialize(world, registry);

            var exception = Assert.Throws<ProductionContentException>(
                () => WorldSnapshotSerializer.Deserialize(json));

            Assert.That(exception.Message, Does.Contain("content.mod_test.production"));
            Assert.That(json, Does.Contain("content.mod_test.production"));
        }

        [Test]
        public void Research_MissingKnowledgeRejectsWithoutSpendingFamilyWealth()
        {
            var world = VillagePrototypeFactory.Create(200, 21_001);
            var lead = world.People.Find(
                item => item.VillageOccupation == VillageOccupation.Farmer);
            var family = world.Families.Find(item => item.Id == lead.FamilyId);
            var facility = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.AssemblyHall);
            lead.ProfessionalSkills.Agriculture = 6_000;
            family.Wealth = 1_000;
            var before = WorldSnapshotSerializer.Serialize(world);
            var system = new ResearchSystem();

            Assert.Throws<InvalidOperationException>(() => system.StartProject(
                world,
                CoreTechnologyIds.SeedSelection,
                lead.Id,
                facility.Id,
                ResearchControlMode.WorkOrder));

            Assert.That(WorldSnapshotSerializer.Serialize(world), Is.EqualTo(before));
        }

        [Test]
        public void Research_ControlModesUseTheSameProgressFormula()
        {
            int? expectedProgress = null;
            foreach (var mode in new[]
                     {
                         ResearchControlMode.PersonalLabor,
                         ResearchControlMode.DelegatedPolicy
                     })
            {
                var world = VillagePrototypeFactory.Create(200, 21_002);
                var lead = world.People.Find(
                    item => item.VillageOccupation == VillageOccupation.Farmer);
                var family = world.Families.Find(
                    item => item.Id == lead.FamilyId);
                var facility = world.VillageFacilities.Find(
                    item => item.Kind == VillageFacilityKind.AssemblyHall);
                lead.ProfessionalSkills.Agriculture = 6_000;
                family.Wealth = 1_000;
                var system = new ResearchSystem();
                system.GrantKnowledge(
                    world,
                    lead.Id,
                    CoreKnowledgeIds.SeasonalObservation,
                    6_000,
                    "source.test.practice");
                var project = system.StartProject(
                    world,
                    CoreTechnologyIds.SeedSelection,
                    lead.Id,
                    facility.Id,
                    mode);
                world.AbsoluteDay = 1;

                system.ResolveDailyProjects(world);

                if (expectedProgress.HasValue)
                {
                    Assert.That(
                        project.ProgressResearchPoints,
                        Is.EqualTo(expectedProgress.Value));
                }
                else
                {
                    expectedProgress = project.ProgressResearchPoints;
                }

                Assert.That(project.ProgressResearchPoints, Is.GreaterThan(0));
                world.Validate();
            }
        }

        [Test]
        public void Research_CompletedTechnologyAppliesOnlyToTargetWorkOrder()
        {
            var world = VillagePrototypeFactory.Create(200, 21_003);
            var lead = world.People.Find(
                item => item.VillageOccupation == VillageOccupation.Farmer);
            var family = world.Families.Find(item => item.Id == lead.FamilyId);
            var researchFacility = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.AssemblyHall);
            var field = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.Farmland);
            var storage = world.VillageFacilities.Find(
                item =>
                    item.Kind == VillageFacilityKind.HouseholdGranary &&
                    item.OwnerFamilyId == family.Id);
            lead.ProfessionalSkills.Agriculture = 6_000;
            family.Wealth = 1_000;
            var research = new ResearchSystem();
            research.GrantKnowledge(
                world,
                lead.Id,
                CoreKnowledgeIds.SeasonalObservation,
                6_000,
                "source.test.practice");
            var project = research.StartProject(
                world,
                CoreTechnologyIds.SeedSelection,
                lead.Id,
                researchFacility.Id,
                ResearchControlMode.DelegatedPolicy);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 30);

            Assert.That(project.Status, Is.EqualTo(ResearchProjectStatus.Completed));
            Assert.That(
                SkillMasteryAccess.HasTechnology(
                    lead, CoreTechnologyIds.SeedSelection),
                Is.True);
            var application = research.ApplyTechnology(
                world,
                CoreTechnologyIds.SeedSelection,
                field.Id,
                lead.Id);
            var agriculture = new AgricultureProductionSystem(world.MasterSeed);
            var order = agriculture.CreateOrder(
                world,
                world.Villages[0].Id,
                family.Id,
                field.Id,
                storage.Id,
                lead.Id,
                CoreProductionContent.WheatCropId,
                CoreProductionContent.PrototypeNorthernWheatVarietyId,
                CoreProductionContent.GrowWheatRecipeId,
                CoreProductionContent.PrototypeDrylandMethodId,
                ProductionControlMode.TargetInstruction,
                1,
                AvailableAgricultureWorkers(world, family),
                world.AbsoluteDay + 180);

            Assert.That(order.TechnologyYieldBasisPoints, Is.EqualTo(10_300));
            Assert.That(
                order.AppliedTechnologyIds,
                Is.EqualTo(new[] { CoreTechnologyIds.SeedSelection }));
            application.IsActive = false;
            world.AbsoluteDay = order.HarvestDay;
            agriculture.ResolveDueOrders(world, world.Villages[0].Id);

            Assert.That(order.Status, Is.EqualTo(ProductionOrderStatus.Completed));
            Assert.That(order.ProducedQuantity, Is.GreaterThan(0));
            Assert.That(order.TechnologyYieldBasisPoints, Is.EqualTo(10_300));
            Assert.That(
                world.ResearchLedgerEntries.Exists(
                    item => item.Type ==
                        ResearchLedgerEntryType.TechnologyApplied &&
                        item.TechnologyApplicationId == application.Id),
                Is.True);
            world.Validate();
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var loadedLead = loaded.People.Find(item => item.Id == lead.Id);
            Assert.That(loaded.ResearchProjects.Count, Is.EqualTo(1));
            Assert.That(loaded.TechnologyApplications.Count, Is.EqualTo(1));
            Assert.That(loaded.ResearchLedgerEntries.Count, Is.GreaterThan(3));
            Assert.That(
                SkillMasteryAccess.HasTechnology(
                    loadedLead, CoreTechnologyIds.SeedSelection),
                Is.True);
            Assert.That(
                loaded.AgricultureWorkOrders[0].AppliedTechnologyIds,
                Is.EqualTo(new[] { CoreTechnologyIds.SeedSelection }));
            loaded.Validate();
        }

        [Test]
        public void Agriculture_AllControlModesUseTheSameSettlementRules()
        {
            long? expectedHarvest = null;
            foreach (ProductionControlMode mode in Enum.GetValues(
                         typeof(ProductionControlMode)))
            {
                var world = VillagePrototypeFactory.Create(200, 20_001);
                world.AbsoluteDay = 90;
                var family = world.Families[0];
                var field = world.VillageFacilities.Find(
                    item => item.Kind == VillageFacilityKind.Farmland);
                var storage = world.VillageFacilities.Find(
                    item =>
                        item.Kind == VillageFacilityKind.HouseholdGranary &&
                        item.OwnerFamilyId == family.Id);
                var workers = AvailableAgricultureWorkers(world, family);
                var system = new AgricultureProductionSystem(world.MasterSeed);
                var order = system.CreateOrder(
                    world,
                    world.Villages[0].Id,
                    family.Id,
                    field.Id,
                    storage.Id,
                    family.HeadPersonId,
                    CoreProductionContent.WheatCropId,
                    CoreProductionContent.PrototypeNorthernWheatVarietyId,
                    CoreProductionContent.GrowWheatRecipeId,
                    CoreProductionContent.PrototypeDrylandMethodId,
                    mode,
                    family.FarmlandUnits,
                    workers,
                    270);
                world.AbsoluteDay = 270;

                system.ResolveDueOrders(world, world.Villages[0].Id);

                Assert.That(order.Status, Is.EqualTo(
                    ProductionOrderStatus.Completed));
                Assert.That(order.ProducedQuantity, Is.GreaterThan(0));
                if (expectedHarvest.HasValue)
                {
                    Assert.That(order.ProducedQuantity, Is.EqualTo(
                        expectedHarvest.Value));
                }
                else
                {
                    expectedHarvest = order.ProducedQuantity;
                }

                Assert.That(system.Audit(world).IsBalanced, Is.True);
                world.Validate();
            }
        }

        [Test]
        public void Agriculture_InsufficientSeedRejectsWithoutChangingWorld()
        {
            var world = VillagePrototypeFactory.Create(200, 20_002);
            world.AbsoluteDay = 90;
            var family = world.Families[0];
            var field = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.Farmland);
            var storage = world.VillageFacilities.Find(
                item =>
                    item.Kind == VillageFacilityKind.HouseholdGranary &&
                    item.OwnerFamilyId == family.Id);
            family.SeedGrain = 0;
            storage.InventoryUnits = family.Grain;
            var before = WorldSnapshotSerializer.Serialize(world);
            var system = new AgricultureProductionSystem(world.MasterSeed);

            Assert.Throws<InvalidOperationException>(() => system.CreateOrder(
                world,
                world.Villages[0].Id,
                family.Id,
                field.Id,
                storage.Id,
                family.HeadPersonId,
                CoreProductionContent.WheatCropId,
                CoreProductionContent.PrototypeNorthernWheatVarietyId,
                CoreProductionContent.GrowWheatRecipeId,
                CoreProductionContent.PrototypeDrylandMethodId,
                ProductionControlMode.DirectAssignment,
                1,
                AvailableAgricultureWorkers(world, family),
                270));

            Assert.That(WorldSnapshotSerializer.Serialize(world), Is.EqualTo(before));
        }

        [Test]
        public void Agriculture_InjectedRepositoryPreservesProductionFacts()
        {
            var inline = VillagePrototypeFactory.Create(200, 21_301);
            var accessed = VillagePrototypeFactory.Create(200, 21_301);
            inline.AbsoluteDay = 90;
            accessed.AbsoluteDay = 90;
            var inlineFamily = inline.Families[0];
            var accessedFamily = accessed.Families[0];
            var inlineField = inline.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.Farmland);
            var accessedField = accessed.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.Farmland);
            var inlineStorage = inline.VillageFacilities.Find(
                item =>
                    item.Kind == VillageFacilityKind.HouseholdGranary &&
                    item.OwnerFamilyId == inlineFamily.Id);
            var accessedStorage = accessed.VillageFacilities.Find(
                item =>
                    item.Kind == VillageFacilityKind.HouseholdGranary &&
                    item.OwnerFamilyId == accessedFamily.Id);
            var repository = new WorldStatePersonRepository(accessed);
            var inlineSystem = new AgricultureProductionSystem(
                inline.MasterSeed);
            var accessedSystem = new AgricultureProductionSystem(
                accessed.MasterSeed, null, repository);

            inlineSystem.CreateOrder(
                inline,
                inline.Villages[0].Id,
                inlineFamily.Id,
                inlineField.Id,
                inlineStorage.Id,
                inlineFamily.HeadPersonId,
                CoreProductionContent.WheatCropId,
                CoreProductionContent.PrototypeNorthernWheatVarietyId,
                CoreProductionContent.GrowWheatRecipeId,
                CoreProductionContent.PrototypeDrylandMethodId,
                ProductionControlMode.WorkOrder,
                inlineFamily.FarmlandUnits,
                AvailableAgricultureWorkers(inline, inlineFamily),
                270);
            accessedSystem.CreateOrder(
                accessed,
                accessed.Villages[0].Id,
                accessedFamily.Id,
                accessedField.Id,
                accessedStorage.Id,
                accessedFamily.HeadPersonId,
                CoreProductionContent.WheatCropId,
                CoreProductionContent.PrototypeNorthernWheatVarietyId,
                CoreProductionContent.GrowWheatRecipeId,
                CoreProductionContent.PrototypeDrylandMethodId,
                ProductionControlMode.WorkOrder,
                accessedFamily.FarmlandUnits,
                AvailableAgricultureWorkers(accessed, accessedFamily),
                270);
            inline.AbsoluteDay = 270;
            accessed.AbsoluteDay = 270;

            inlineSystem.ResolveDueOrders(inline, inline.Villages[0].Id);
            accessedSystem.ResolveDueOrders(accessed, accessed.Villages[0].Id);

            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            Assert.That(accessedSystem.Audit(accessed).IsBalanced, Is.True);
            Assert.That(repository.GetAddedPersonIds(), Is.Empty);
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
        }

        [Test]
        public void Agriculture_RepositoryRejectsMissingManagerWithoutDirtyingPeople()
        {
            var world = VillagePrototypeFactory.Create(200, 21_302);
            world.AbsoluteDay = 90;
            var family = world.Families[0];
            var field = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.Farmland);
            var storage = world.VillageFacilities.Find(
                item =>
                    item.Kind == VillageFacilityKind.HouseholdGranary &&
                    item.OwnerFamilyId == family.Id);
            var repository = new WorldStatePersonRepository(world);
            var system = new AgricultureProductionSystem(
                world.MasterSeed, null, repository);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() => system.CreateOrder(
                world,
                world.Villages[0].Id,
                family.Id,
                field.Id,
                storage.Id,
                "person.missing.agriculture_manager",
                CoreProductionContent.WheatCropId,
                CoreProductionContent.PrototypeNorthernWheatVarietyId,
                CoreProductionContent.GrowWheatRecipeId,
                CoreProductionContent.PrototypeDrylandMethodId,
                ProductionControlMode.DirectAssignment,
                1,
                AvailableAgricultureWorkers(world, family),
                270));

            Assert.That(WorldSnapshotSerializer.Serialize(world), Is.EqualTo(before));
            Assert.That(repository.GetAddedPersonIds(), Is.Empty);
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
        }

        [Test]
        public void Agriculture_StorageOverflowIsLostAndAudited()
        {
            var world = VillagePrototypeFactory.Create(200, 20_003);
            world.AbsoluteDay = 90;
            var family = world.Families[0];
            var field = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.Farmland);
            var storage = world.VillageFacilities.Find(
                item =>
                    item.Kind == VillageFacilityKind.HouseholdGranary &&
                    item.OwnerFamilyId == family.Id);
            var system = new AgricultureProductionSystem(world.MasterSeed);
            var order = system.CreateOrder(
                world,
                world.Villages[0].Id,
                family.Id,
                field.Id,
                storage.Id,
                family.HeadPersonId,
                CoreProductionContent.WheatCropId,
                CoreProductionContent.PrototypeNorthernWheatVarietyId,
                CoreProductionContent.GrowWheatRecipeId,
                CoreProductionContent.PrototypeDrylandMethodId,
                ProductionControlMode.WorkOrder,
                family.FarmlandUnits,
                AvailableAgricultureWorkers(world, family),
                270);
            storage.Capacity = checked((int)storage.InventoryUnits + 1);
            world.AbsoluteDay = 270;

            system.ResolveDueOrders(world, world.Villages[0].Id);

            Assert.That(order.StoredQuantity, Is.EqualTo(1));
            Assert.That(order.LostQuantity, Is.GreaterThan(0));
            Assert.That(storage.InventoryUnits, Is.EqualTo(storage.Capacity));
            Assert.That(
                world.ProductionLedgerEntries.Exists(
                    item =>
                        item.WorkOrderId == order.Id &&
                        item.Type == ProductionLedgerEntryType.ProductLost &&
                        item.ProductDefinitionId ==
                        CoreProductionContent.WheatGrainProductId &&
                        item.Quantity == order.LostQuantity),
                Is.True);
            Assert.That(system.Audit(world).IsBalanced, Is.True);
            world.Validate();
        }

        [Test]
        public void Agriculture_MissingDefinitionIsRejectedBeforeSnapshotWrite()
        {
            var world = VillagePrototypeFactory.Create(200, 20_005);
            world.AbsoluteDay = 90;
            var family = world.Families[0];
            var field = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.Farmland);
            var storage = world.VillageFacilities.Find(
                item =>
                    item.Kind == VillageFacilityKind.HouseholdGranary &&
                    item.OwnerFamilyId == family.Id);
            var system = new AgricultureProductionSystem(world.MasterSeed);
            var order = system.CreateOrder(
                world,
                world.Villages[0].Id,
                family.Id,
                field.Id,
                storage.Id,
                family.HeadPersonId,
                CoreProductionContent.WheatCropId,
                CoreProductionContent.PrototypeNorthernWheatVarietyId,
                CoreProductionContent.GrowWheatRecipeId,
                CoreProductionContent.PrototypeDrylandMethodId,
                ProductionControlMode.WorkOrder,
                1,
                AvailableAgricultureWorkers(world, family),
                270);
            order.RecipeDefinitionId = "recipe.missing";

            var exception = Assert.Throws<ProductionContentException>(
                () => WorldSnapshotSerializer.Serialize(world));

            Assert.That(exception.Message, Does.Contain("recipe.missing"));
        }

        [Test]
        public void Agriculture_VillageYearUsesPersistedWorkOrdersAndRoundTrips()
        {
            var world = VillagePrototypeFactory.Create(200, 20_004);

            SimulateVillageMonths(world, 12);

            var audit = new AgricultureProductionSystem(world.MasterSeed)
                .Audit(world);
            Assert.That(
                world.AgricultureWorkOrders.Count,
                Is.EqualTo(world.Families.Count));
            Assert.That(audit.ActiveOrders, Is.EqualTo(0));
            Assert.That(audit.CompletedOrders,
                Is.EqualTo(world.AgricultureWorkOrders.Count));
            Assert.That(audit.IsBalanced, Is.True);
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(
                loaded.AgricultureWorkOrders.Count,
                Is.EqualTo(world.AgricultureWorkOrders.Count));
            Assert.That(
                loaded.ProductionLedgerEntries.Count,
                Is.EqualTo(world.ProductionLedgerEntries.Count));
            Assert.That(
                loaded.AgricultureWorkOrders[0].CropDefinitionId,
                Is.EqualTo(CoreProductionContent.WheatCropId));
            Assert.That(
                loaded.AgricultureWorkOrders[0].RecipeDefinitionId,
                Is.EqualTo(CoreProductionContent.GrowWheatRecipeId));
            Assert.That(
                new AgricultureProductionSystem(loaded.MasterSeed)
                    .Audit(loaded).IsBalanced,
                Is.True);
            loaded.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionSevenToProductionCollections()
        {
            var world = BuildMinimalWorld();
            var populationMode = world.PopulationStorage.Mode;
            var personIds = new List<string>();
            for (var i = 0; i < world.People.Count; i++)
            {
                personIds.Add(world.People[i].Id);
            }

            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 7");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.PopulationStorage.Mode, Is.EqualTo(populationMode));
            Assert.That(loaded.AgricultureWorkOrders, Is.Not.Null);
            Assert.That(loaded.ProductionLedgerEntries, Is.Not.Null);
            Assert.That(loaded.ProductionContentManifest, Is.Not.Null);
            Assert.That(
                loaded.ProductionContentManifest.ResolvedHash,
                Is.EqualTo(ProductionContentRegistry.CreateCore().ResolvedHash));
            for (var i = 0; i < personIds.Count; i++)
            {
                Assert.That(loaded.People[i].Id, Is.EqualTo(personIds[i]));
            }

            loaded.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionEightToResearchCollections()
        {
            var world = BuildMinimalWorld();
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 8")
                .Replace(
                    "\"ContentSchemaVersion\": 3",
                    "\"ContentSchemaVersion\": 1")
                .Replace("\"SkillMasteries\": []", "\"SkillMasteries\": null")
                .Replace(
                    "\"KnowledgeMasteries\": []",
                    "\"KnowledgeMasteries\": null")
                .Replace(
                    "\"TechnologyMasteries\": []",
                    "\"TechnologyMasteries\": null")
                .Replace("\"ResearchProjects\": []", "\"ResearchProjects\": null")
                .Replace(
                    "\"TechnologyApplications\": []",
                    "\"TechnologyApplications\": null")
                .Replace(
                    "\"ResearchLedgerEntries\": []",
                    "\"ResearchLedgerEntries\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.ResearchProjects, Is.Not.Null);
            Assert.That(loaded.TechnologyApplications, Is.Not.Null);
            Assert.That(loaded.ResearchLedgerEntries, Is.Not.Null);
            Assert.That(loaded.People[0].SkillMasteries, Is.Not.Null);
            Assert.That(loaded.People[0].KnowledgeMasteries, Is.Not.Null);
            Assert.That(loaded.People[0].TechnologyMasteries, Is.Not.Null);
            Assert.That(
                loaded.ProductionContentManifest.ContentSchemaVersion,
                Is.EqualTo(3));
            loaded.Validate();
        }

        [Test]
        public void ProductInventory_LegacyConversionPreservesPhysicalStock()
        {
            var world = VillagePrototypeFactory.Create(200, 22_001);
            var family = world.Families[0];
            var storage = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.HouseholdGranary &&
                        item.OwnerFamilyId == family.Id);
            var originalGrain = family.Grain;
            var originalSeed = family.SeedGrain;
            var originalPhysical = storage.InventoryUnits;
            var openingTransactionCount = world.InventoryTransactions.Count;
            var system = new ProductInventorySystem();

            var grain = system.ConvertLegacyBalanceToBatch(
                world,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                CoreProductionContent.WheatGrainProductId,
                10);
            var seed = system.ConvertLegacyBalanceToBatch(
                world,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                CoreProductionContent.WheatSeedProductId,
                2,
                CoreProductionContent.PrototypeNorthernWheatVarietyId);

            Assert.That(family.Grain, Is.EqualTo(originalGrain - 10));
            Assert.That(family.SeedGrain, Is.EqualTo(originalSeed - 2));
            Assert.That(grain.Quantity, Is.EqualTo(10));
            Assert.That(seed.Quantity, Is.EqualTo(2));
            Assert.That(seed.SeedVigorBasisPoints, Is.GreaterThan(0));
            Assert.That(storage.InventoryUnits, Is.EqualTo(originalPhysical));
            Assert.That(world.InventoryTransactions.Count,
                Is.EqualTo(openingTransactionCount + 2));
            world.Validate();
        }

        [Test]
        public void ProductInventory_CompletedHarvestMaterializesOnceWithoutDuplication()
        {
            var world = VillagePrototypeFactory.Create(200, 22_004);
            world.AbsoluteDay = 90;
            var family = world.Families[0];
            var field = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.Farmland);
            var storage = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.HouseholdGranary &&
                        item.OwnerFamilyId == family.Id);
            var agriculture = new AgricultureProductionSystem(world.MasterSeed);
            var order = agriculture.CreateOrder(
                world,
                world.Villages[0].Id,
                family.Id,
                field.Id,
                storage.Id,
                family.HeadPersonId,
                CoreProductionContent.WheatCropId,
                CoreProductionContent.PrototypeNorthernWheatVarietyId,
                CoreProductionContent.GrowWheatRecipeId,
                CoreProductionContent.PrototypeDrylandMethodId,
                ProductionControlMode.WorkOrder,
                family.FarmlandUnits,
                AvailableAgricultureWorkers(world, family),
                270);
            world.AbsoluteDay = order.HarvestDay;
            agriculture.ResolveDueOrders(world, world.Villages[0].Id);
            var physicalBefore = storage.InventoryUnits;
            var grainBefore = family.Grain;
            var seedBefore = family.SeedGrain;

            var batches = new ProductInventorySystem()
                .ConvertCompletedAgricultureHarvestToBatches(world, order.Id);

            Assert.That(batches.Count, Is.EqualTo(2));
            Assert.That(batches[0].Quantity + batches[1].Quantity,
                Is.EqualTo(order.StoredQuantity));
            Assert.That(batches[0].SourceWorkOrderId, Is.EqualTo(order.Id));
            Assert.That(batches[1].SourceWorkOrderId, Is.EqualTo(order.Id));
            Assert.That(family.Grain + family.SeedGrain,
                Is.EqualTo(grainBefore + seedBefore - order.StoredQuantity));
            Assert.That(storage.InventoryUnits, Is.EqualTo(physicalBefore));
            Assert.That(
                ProductInventorySystem.CalculatePhysicalInventoryUnits(
                    world, storage.Id, family.Id),
                Is.EqualTo(physicalBefore));
            Assert.That(world.InventoryTransactions.Find(item =>
                item.SourceWorkOrderId == order.Id).Lines.Count,
                Is.EqualTo(2));
            Assert.Throws<InvalidOperationException>(() =>
                new ProductInventorySystem()
                    .ConvertCompletedAgricultureHarvestToBatches(world, order.Id));
            world.Validate();
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.ProductBatches.FindAll(item =>
                item.SourceWorkOrderId == order.Id).Count, Is.EqualTo(2));
            Assert.That(loaded.InventoryTransactions.FindAll(item =>
                item.SourceWorkOrderId == order.Id).Count, Is.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void FoodInventory_ConsumesStablePriorityAndRoundTripsV29()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_101);
            world.ProductionContentManifest = content.CreateManifest();
            var family = world.Families[0];
            var storage = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.HouseholdGranary &&
                        item.OwnerFamilyId == family.Id);
            var inventory = new ProductInventorySystem(content);
            var wheat = inventory.CreateFamilyOpeningBatch(
                world,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                CoreProductionContent.WheatGrainProductId,
                10);
            var rice = inventory.CreateFamilyOpeningBatch(
                world,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                "product.rice_grain",
                10);
            var ration = inventory.CreateFamilyOpeningBatch(
                world,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                CoreProductionContent.DryRationProductId,
                10);
            var food = new FoodInventorySystem(content);
            var opening = food.SummarizeFamilyGranary(
                world, family.Id, storage.Id);
            Assert.That(opening.PhysicalQuantity, Is.EqualTo(30));
            Assert.That(opening.NutritionBasisUnits, Is.EqualTo(315_000));
            Assert.That(opening.VolumeBasisUnits, Is.EqualTo(285_000));
            Assert.That(opening.MarketValueBasisUnits, Is.EqualTo(330_000));
            new ProcessingProductionSystem(content).CreateOrder(
                world,
                CoreProductionContent.HandMillWheatRecipeId,
                CoreProductionContent.HandMillingMethodId,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                ProductionControlMode.WorkOrder,
                1);
            var physicalBefore = storage.InventoryUnits;

            var result = food.ConsumeFamilyFood(
                world,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                200_000);

            Assert.That(result.Fulfilled, Is.True);
            Assert.That(result.ConsumedPhysicalQuantity, Is.EqualTo(19));
            Assert.That(result.ProvidedNutritionBasisUnits, Is.EqualTo(204_500));
            Assert.That(ration.Quantity, Is.EqualTo(0));
            Assert.That(rice.Quantity, Is.EqualTo(1));
            Assert.That(wheat.Quantity, Is.EqualTo(10));
            Assert.That(wheat.ReservedQuantity, Is.EqualTo(10));
            Assert.That(storage.InventoryUnits, Is.EqualTo(physicalBefore - 19));
            Assert.That(
                food.CalculateTransportQuantityCapacity(
                    CoreProductionContent.DryRationProductId,
                    80),
                Is.EqualTo(100));
            var transaction = world.InventoryTransactions.Find(
                item => item.Id == result.InventoryTransactionId);
            Assert.That(transaction.Type,
                Is.EqualTo(InventoryTransactionType.FoodConsumed));
            Assert.That(transaction.Lines.Count, Is.EqualTo(2));
            Assert.That(transaction.Lines[0].BatchId, Is.EqualTo(ration.Id));
            Assert.That(transaction.Lines[1].BatchId, Is.EqualTo(rice.Id));
            world.Validate();

            var json = WorldSnapshotSerializer.Serialize(world, content);
            var loaded = WorldSnapshotSerializer.Deserialize(json, content);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.ProductionContentManifest.Packages.Count,
                Is.EqualTo(2));
            Assert.That(loaded.ProductBatches.Find(
                item => item.Id == wheat.Id).ReservedQuantity, Is.EqualTo(10));
            Assert.That(loaded.InventoryTransactions.Find(
                item => item.Id == result.InventoryTransactionId).Lines.Count,
                Is.EqualTo(2));
            loaded.Validate();
            Assert.Throws<ProductionContentException>(
                () => WorldSnapshotSerializer.Deserialize(json));
        }

        [Test]
        public void FoodStocks_V28MigrationPreservesLegacyAuthorityAndBalances()
        {
            var world = VillagePrototypeFactory.Create(200, 25_201);
            long familyFood = 0;
            for (var i = 0; i < world.Families.Count; i++)
            {
                familyFood += world.Families[i].Grain;
            }
            var villageFood = world.Villages[0].PublicGranaryGrain;
            var countyFood = world.CountyGovernances[0].CountyGranaryGrain;
            var v28 = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 28");

            var migrated = WorldSnapshotSerializer.Deserialize(v28);

            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.FoodInventoryAuthorityMode,
                Is.EqualTo(FoodInventoryAuthorityMode.LegacyScalar));
            Assert.That(migrated.Villages[0]
                .PublicGranaryInventoryContainerId, Is.Empty);
            Assert.That(migrated.CountyGovernances[0]
                .GranaryInventoryContainerId, Is.Empty);
            long migratedFamilyFood = 0;
            for (var i = 0; i < migrated.Families.Count; i++)
            {
                migratedFamilyFood += migrated.Families[i].Grain;
            }
            Assert.That(migratedFamilyFood, Is.EqualTo(familyFood));
            Assert.That(migrated.Villages[0].PublicGranaryGrain,
                Is.EqualTo(villageFood));
            Assert.That(migrated.CountyGovernances[0].CountyGranaryGrain,
                Is.EqualTo(countyFood));
            migrated.Validate();
        }

        [Test]
        public void FoodStocks_FormalizationConservesFamilyVillageAndCountyFood()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_202);
            world.ProductionContentManifest = content.CreateManifest();
            long familyFood = 0;
            var positiveFamilyStocks = 0;
            for (var i = 0; i < world.Families.Count; i++)
            {
                familyFood += world.Families[i].Grain;
                if (world.Families[i].Grain > 0)
                {
                    positiveFamilyStocks++;
                }
            }
            var villageFood = world.Villages[0].PublicGranaryGrain;
            var countyFood = world.CountyGovernances[0].CountyGranaryGrain;
            var expected = familyFood + villageFood + countyFood;
            var system = new FoodStockFormalizationSystem(content);

            var result = system.FormalizeLegacyStocks(world);
            var audit = system.Audit(world);

            Assert.That(result.FamilyFoodQuantity, Is.EqualTo(familyFood));
            Assert.That(result.VillageGranaryFoodQuantity,
                Is.EqualTo(villageFood));
            Assert.That(result.CountyGranaryFoodQuantity,
                Is.EqualTo(countyFood));
            Assert.That(result.TotalFormalizedQuantity, Is.EqualTo(expected));
            Assert.That(result.FamilyTransactions,
                Is.EqualTo(positiveFamilyStocks));
            Assert.That(result.VillageContainers, Is.EqualTo(1));
            Assert.That(result.CountyContainers, Is.EqualTo(1));
            Assert.That(world.FoodInventoryAuthorityMode,
                Is.EqualTo(FoodInventoryAuthorityMode.FormalProductBatches));
            Assert.That(world.Families.TrueForAll(item => item.Grain == 0),
                Is.True);
            Assert.That(world.Villages[0].PublicGranaryGrain, Is.Zero);
            Assert.That(world.CountyGovernances[0].CountyGranaryGrain, Is.Zero);
            Assert.That(audit.IsValid, Is.True);
            Assert.That(audit.FormalizedBatchQuantity, Is.EqualTo(expected));
            Assert.That(world.ProductBatches.FindAll(item =>
                item.SourceTransactionId != null &&
                world.InventoryTransactions.Exists(transaction =>
                    transaction.Id == item.SourceTransactionId &&
                    transaction.Type == InventoryTransactionType
                        .LegacyFoodStockFormalized)).Count,
                Is.GreaterThan(6));
            Assert.Throws<InvalidOperationException>(() =>
                system.FormalizeLegacyStocks(world));
            world.Validate();

            var json = WorldSnapshotSerializer.Serialize(world, content);
            var loaded = WorldSnapshotSerializer.Deserialize(json, content);
            var loadedAudit = system.Audit(loaded);
            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.FoodInventoryAuthorityMode,
                Is.EqualTo(FoodInventoryAuthorityMode.FormalProductBatches));
            Assert.That(loadedAudit.IsValid, Is.True);
            Assert.That(loadedAudit.FormalizedBatchQuantity,
                Is.EqualTo(expected));
            loaded.Validate();
        }

        [Test]
        public void FoodStocks_FormalAuthorityRejectsResidualLegacyFood()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_203);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            world.Families[0].Grain = 1;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void FoodRuntime_TransferPreservesProductQualityAndProvenance()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_301);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var family = world.Families.Find(item => item.Grain == 0 &&
                world.ProductBatches.Exists(batch =>
                    batch.OwnerFamilyId == item.Id && batch.Quantity >= 3));
            var storage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == family.Id);
            var village = world.Villages[0];
            world.InventoryContainers.Find(item => item.Id ==
                    village.PublicGranaryInventoryContainerId)
                .CapacityWeight += 10;
            var food = new FoodInventorySystem(content);
            var batchCountBefore = world.ProductBatches.Count;
            var transactionCountBefore = world.InventoryTransactions.Count;
            var familyFoodBefore = food.SummarizeFamilyGranary(
                world, family.Id, storage.Id).PhysicalQuantity;
            Assert.Throws<InvalidOperationException>(() =>
                food.TransferFamilyToContainer(
                    world,
                    family.Id,
                    storage.Id,
                    village.PublicGranaryInventoryContainerId,
                    family.HeadPersonId,
                    3,
                    InventoryTransactionType.FoodVillageReliefTransferred,
                    village.Id));
            Assert.That(world.ProductBatches.Count, Is.EqualTo(batchCountBefore));
            Assert.That(world.InventoryTransactions.Count,
                Is.EqualTo(transactionCountBefore));
            Assert.That(food.SummarizeFamilyGranary(
                    world, family.Id, storage.Id).PhysicalQuantity,
                Is.EqualTo(familyFoodBefore));

            var result = food.TransferFamilyToContainer(
                world,
                family.Id,
                storage.Id,
                village.PublicGranaryInventoryContainerId,
                family.HeadPersonId,
                3,
                InventoryTransactionType.FoodTaxTransferred,
                village.Id);

            Assert.That(result.TransferredPhysicalQuantity, Is.EqualTo(3));
            var transaction = world.InventoryTransactions.Find(item =>
                item.Id == result.InventoryTransactionId);
            Assert.That(transaction.Lines.Count % 2, Is.Zero);
            for (var i = 0; i < transaction.Lines.Count; i += 2)
            {
                var source = world.ProductBatches.Find(item =>
                    item.Id == transaction.Lines[i].BatchId);
                var destination = world.ProductBatches.Find(item =>
                    item.Id == transaction.Lines[i + 1].BatchId);
                Assert.That(destination.ProductDefinitionId,
                    Is.EqualTo(source.ProductDefinitionId));
                Assert.That(destination.OriginLocationId,
                    Is.EqualTo(source.OriginLocationId));
                Assert.That(destination.SourceWorkOrderId,
                    Is.EqualTo(source.SourceWorkOrderId));
                Assert.That(destination.CropVarietyDefinitionId,
                    Is.EqualTo(source.CropVarietyDefinitionId));
                Assert.That(destination.ProducedDay,
                    Is.EqualTo(source.ProducedDay));
                Assert.That(destination.QualityBasisPoints,
                    Is.EqualTo(source.QualityBasisPoints));
                Assert.That(destination.FreshnessBasisPoints,
                    Is.EqualTo(source.FreshnessBasisPoints));
                Assert.That(destination.QualityDimensions.Count,
                    Is.EqualTo(source.QualityDimensions.Count));
            }
            Assert.That(world.Families.TrueForAll(item => item.Grain == 0),
                Is.True);
            Assert.That(village.PublicGranaryGrain, Is.Zero);
            world.Validate();
            var tampered = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world, content), content);
            var tamperedTransaction = tampered.InventoryTransactions.Find(item =>
                item.Id == result.InventoryTransactionId);
            var positiveLine = tamperedTransaction.Lines.Find(item =>
                item.QuantityDelta > 0);
            positiveLine.ProductDefinitionId = "product.rice_grain";
            tampered.ProductBatches.Find(item => item.Id == positiveLine.BatchId)
                .ProductDefinitionId = "product.rice_grain";
            Assert.Throws<InvalidOperationException>(() => tampered.Validate());
        }

        [Test]
        public void FoodRuntime_FormalHarvestCreatesAuthoritativeFoodBatch()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_302);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            world.AbsoluteDay = 90;
            var family = world.Families[0];
            var field = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.Farmland);
            var storage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == family.Id);
            var agriculture = new AgricultureProductionSystem(
                world.MasterSeed, content);
            var order = agriculture.CreateOrder(
                world,
                world.Villages[0].Id,
                family.Id,
                field.Id,
                storage.Id,
                family.HeadPersonId,
                CoreProductionContent.WheatCropId,
                CoreProductionContent.PrototypeNorthernWheatVarietyId,
                CoreProductionContent.GrowWheatRecipeId,
                CoreProductionContent.PrototypeDrylandMethodId,
                ProductionControlMode.WorkOrder,
                family.FarmlandUnits,
                AvailableAgricultureWorkers(world, family),
                270);
            var legacyFoodBefore = family.Grain;

            world.AbsoluteDay = order.HarvestDay;
            agriculture.ResolveDueOrders(world, world.Villages[0].Id);

            Assert.That(family.Grain, Is.EqualTo(legacyFoodBefore));
            var transaction = world.InventoryTransactions.Find(item =>
                item.Type == InventoryTransactionType.FoodHarvested &&
                item.SourceWorkOrderId == order.Id);
            Assert.That(transaction, Is.Not.Null);
            var batch = world.ProductBatches.Find(item =>
                item.SourceTransactionId == transaction.Id);
            Assert.That(batch.ProductDefinitionId,
                Is.EqualTo(order.HarvestProductDefinitionId));
            Assert.That(batch.CropVarietyDefinitionId,
                Is.EqualTo(order.CropVarietyDefinitionId));
            Assert.That(batch.Quantity,
                Is.EqualTo(order.StoredQuantity -
                    Math.Min(order.StoredQuantity / 8,
                        order.LandUnits * 2L)));
            world.Validate();
        }

        [Test]
        public void FoodRuntime_FormalConsumptionTaxRemittanceAndReliefStayOffLegacyScalars()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_303);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var village = world.Villages[0];
            var governance = world.CountyGovernances[0];
            world.InventoryContainers.Find(item => item.Id ==
                    village.PublicGranaryInventoryContainerId)
                .CapacityWeight += 1_000;
            world.InventoryContainers.Find(item => item.Id ==
                    governance.GranaryInventoryContainerId)
                .CapacityWeight += 1_000;
            world.AbsoluteDay = 300;
            for (var i = 0; i < world.Families.Count; i++)
            {
                world.Families[i].LastHarvestGrain = i == 0 ? 100 : 0;
            }

            new VillageLifeSystem(world.MasterSeed, content)
                .ResolveMonthly(world);
            new CountyGovernanceSystem(content).ResolveMonthly(world);

            Assert.That(world.InventoryTransactions.Exists(item =>
                item.Type == InventoryTransactionType.FoodConsumed), Is.True);
            Assert.That(world.InventoryTransactions.Exists(item =>
                item.Type == InventoryTransactionType.FoodTaxTransferred),
                Is.True);
            Assert.That(world.InventoryTransactions.Exists(item =>
                item.Type == InventoryTransactionType.FoodTaxRemitted),
                Is.True);
            Assert.That(world.Families.TrueForAll(item => item.Grain == 0),
                Is.True);
            Assert.That(village.PublicGranaryGrain, Is.Zero);
            Assert.That(governance.CountyGranaryGrain, Is.Zero);
            world.Validate();

            var countyBefore = new FoodInventorySystem(content)
                .SummarizeContainer(
                    world, governance.GranaryInventoryContainerId)
                .PhysicalQuantity;
            var villageBefore = new FoodInventorySystem(content)
                .SummarizeContainer(
                    world, village.PublicGranaryInventoryContainerId)
                .PhysicalQuantity;
            village.FoodSecurityBasisPoints = 0;
            governance.NextSettlementDay = 330;
            world.AbsoluteDay = 330;

            new CountyGovernanceSystem(content).ResolveMonthly(world);

            var countyAfter = new FoodInventorySystem(content)
                .SummarizeContainer(
                    world, governance.GranaryInventoryContainerId)
                .PhysicalQuantity;
            var villageAfter = new FoodInventorySystem(content)
                .SummarizeContainer(
                    world, village.PublicGranaryInventoryContainerId)
                .PhysicalQuantity;
            Assert.That(world.InventoryTransactions.Exists(item =>
                item.Day == 330 && item.Type == InventoryTransactionType
                    .FoodCountyReliefTransferred), Is.True);
            Assert.That(countyAfter + villageAfter,
                Is.EqualTo(countyBefore + villageBefore));
            Assert.That(village.PublicGranaryGrain, Is.Zero);
            Assert.That(governance.CountyGranaryGrain, Is.Zero);
            world.Validate();
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world, content), content);
            loaded.Validate();
        }

        [Test]
        public void FoodRuntime_FormalWorldIsDeterministicForOneYear()
        {
            var content = LoadHanFoodProductionContent();
            var left = VillagePrototypeFactory.Create(200, 25_304);
            var right = VillagePrototypeFactory.Create(200, 25_304);
            left.ProductionContentManifest = content.CreateManifest();
            right.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(left);
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(right);

            new WorldSimulator(left.MasterSeed, content)
                .AdvanceDays(left, 360);
            new WorldSimulator(right.MasterSeed, content)
                .AdvanceDays(right, 360);

            Assert.That(left.Families.TrueForAll(item => item.Grain == 0),
                Is.True);
            Assert.That(right.Families.TrueForAll(item => item.Grain == 0),
                Is.True);
            Assert.That(left.Villages.TrueForAll(item =>
                item.PublicGranaryGrain == 0), Is.True);
            Assert.That(right.Villages.TrueForAll(item =>
                item.PublicGranaryGrain == 0), Is.True);
            Assert.That(left.CountyGovernances.TrueForAll(item =>
                item.CountyGranaryGrain == 0), Is.True);
            Assert.That(right.CountyGovernances.TrueForAll(item =>
                item.CountyGranaryGrain == 0), Is.True);
            left.Validate();
            right.Validate();
            Assert.That(WorldSnapshotSerializer.Serialize(left, content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(right, content)));
        }

        [Test]
        public void FormalMarket_CheaperSellMovesReservedFoodAndMoney()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_401);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var market = new FormalCountyMarketSystem(content);
            var governance = world.CountyGovernances[0];
            var candidates = world.Families.FindAll(family =>
                world.ProductBatches.Exists(batch =>
                    batch.OwnerFamilyId == family.Id &&
                    batch.ProductDefinitionId ==
                        CoreProductionContent.WheatGrainProductId &&
                    batch.Quantity >= 6));
            Assert.That(candidates.Count, Is.GreaterThanOrEqualTo(3));
            var expensiveSeller = candidates[0];
            var cheapSeller = candidates[1];
            var buyer = candidates[2];
            var expensiveStorage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == expensiveSeller.Id);
            var cheapStorage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == cheapSeller.Id);
            var buyerStorage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == buyer.Id);
            buyerStorage.Capacity += 10_000;
            buyer.Wealth = 10_000;
            var buyerWealthBefore = buyer.Wealth;
            var cheapSellerWealthBefore = cheapSeller.Wealth;
            var expensiveOrder = market.CreateSellOrder(
                world,
                governance.Id,
                expensiveSeller.Id,
                expensiveStorage.Id,
                CoreProductionContent.WheatGrainProductId,
                6,
                8,
                0,
                world.AbsoluteDay + 5);
            var cheapOrder = market.CreateSellOrder(
                world,
                governance.Id,
                cheapSeller.Id,
                cheapStorage.Id,
                CoreProductionContent.WheatGrainProductId,
                6,
                6,
                0,
                world.AbsoluteDay + 5);
            var buyOrder = market.CreateBuyOrder(
                world,
                governance.Id,
                buyer.Id,
                buyerStorage.Id,
                CoreProductionContent.WheatGrainProductId,
                6,
                9,
                0,
                world.AbsoluteDay + 5);

            market.ResolveDaily(world);

            Assert.That(buyOrder.Status,
                Is.EqualTo(FormalMarketOrderStatus.Filled));
            Assert.That(cheapOrder.Status,
                Is.EqualTo(FormalMarketOrderStatus.Filled));
            Assert.That(expensiveOrder.Status,
                Is.EqualTo(FormalMarketOrderStatus.Active));
            Assert.That(world.FormalMarketTrades.Count, Is.EqualTo(1));
            var trade = world.FormalMarketTrades[0];
            Assert.That(trade.SellOrderId, Is.EqualTo(cheapOrder.Id));
            Assert.That(trade.Quantity, Is.EqualTo(6));
            Assert.That(trade.UnitPrice, Is.EqualTo(6));
            Assert.That(buyer.Wealth, Is.EqualTo(buyerWealthBefore - 36));
            Assert.That(cheapSeller.Wealth,
                Is.EqualTo(cheapSellerWealthBefore + 36));
            var delivery = world.InventoryTransactions.Find(item =>
                item.Id == trade.InventoryTransactionId);
            Assert.That(delivery.Type,
                Is.EqualTo(InventoryTransactionType.FoodMarketTransferred));
            Assert.That(delivery.Lines.Exists(line =>
                line.OwnerFamilyId == buyer.Id && line.QuantityDelta == 6),
                Is.True);
            Assert.That(world.FormalMarketPrices[0].LastTradeUnitPrice,
                Is.EqualTo(6));
            Assert.That(world.FormalMarketPrices[0]
                .CumulativeTradedQuantity, Is.EqualTo(6));
            world.Validate();

            var json = WorldSnapshotSerializer.Serialize(world, content);
            var loaded = WorldSnapshotSerializer.Deserialize(json, content);
            Assert.That(loaded.FormalMarketOrders.Count, Is.EqualTo(3));
            Assert.That(loaded.FormalMarketTrades.Count, Is.EqualTo(1));
            Assert.That(loaded.FormalMarketPrices.Count, Is.EqualTo(1));
            loaded.Validate();
            loaded.FormalMarketTrades[0].MoneyTransferred++;
            Assert.Throws<InvalidOperationException>(() => loaded.Validate());
        }

        [Test]
        public void FormalMarket_PartialCapacityAndCancellationReleaseAssets()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_402);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var candidates = world.Families.FindAll(family =>
                world.ProductBatches.Exists(batch =>
                    batch.OwnerFamilyId == family.Id && batch.Quantity >= 8));
            Assert.That(candidates.Count, Is.GreaterThanOrEqualTo(2));
            var seller = candidates[0];
            var buyer = candidates[1];
            var sellerStorage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == seller.Id);
            var buyerStorage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == buyer.Id);
            var sellerBatch = world.ProductBatches.Find(item =>
                item.OwnerFamilyId == seller.Id && item.Quantity >= 8);
            buyerStorage.Capacity = checked((int)(
                buyerStorage.InventoryUnits + sellerBatch.UnitWeight * 3));
            buyer.Wealth = 10_000;
            var buyerWealthBefore = buyer.Wealth;
            var sellerWealthBefore = seller.Wealth;
            var market = new FormalCountyMarketSystem(content);
            var sell = market.CreateSellOrder(
                world,
                world.CountyGovernances[0].Id,
                seller.Id,
                sellerStorage.Id,
                sellerBatch.ProductDefinitionId,
                8,
                5,
                0,
                world.AbsoluteDay + 5);
            var buy = market.CreateBuyOrder(
                world,
                world.CountyGovernances[0].Id,
                buyer.Id,
                buyerStorage.Id,
                sellerBatch.ProductDefinitionId,
                8,
                10,
                0,
                world.AbsoluteDay + 5);

            market.ResolveDaily(world);

            Assert.That(buy.FilledQuantity, Is.EqualTo(3));
            Assert.That(sell.FilledQuantity, Is.EqualTo(3));
            Assert.That(buy.Status,
                Is.EqualTo(FormalMarketOrderStatus.Active));
            Assert.That(sell.Status,
                Is.EqualTo(FormalMarketOrderStatus.Active));
            market.CancelOrder(world, buy.Id, "buyer withdrew");
            market.CancelOrder(world, sell.Id, "seller withdrew");
            Assert.That(buyer.Wealth, Is.EqualTo(buyerWealthBefore - 15));
            Assert.That(seller.Wealth, Is.EqualTo(sellerWealthBefore + 15));
            Assert.That(buy.EscrowMoney, Is.Zero);
            Assert.That(sell.BatchReservations.TrueForAll(item =>
                item.RemainingQuantity == 0), Is.True);
            Assert.That(world.InventoryTransactions.Exists(item =>
                item.Type == InventoryTransactionType
                    .FoodMarketReservationReleased), Is.True);
            world.Validate();
        }

        [Test]
        public void FormalMarket_RejectedOrdersDoNotMutateWorld()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_403);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var family = world.Families[0];
            var storage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == family.Id);
            var batch = world.ProductBatches.Find(item =>
                item.OwnerFamilyId == family.Id);
            family.Wealth = 0;
            var before = WorldSnapshotSerializer.Serialize(world, content);
            var market = new FormalCountyMarketSystem(content);

            Assert.Throws<InvalidOperationException>(() =>
                market.CreateSellOrder(
                    world,
                    world.CountyGovernances[0].Id,
                    family.Id,
                    storage.Id,
                    batch.ProductDefinitionId,
                    batch.Quantity + 1,
                    1,
                    0,
                    world.AbsoluteDay + 1));
            Assert.Throws<InvalidOperationException>(() =>
                market.CreateBuyOrder(
                    world,
                    world.CountyGovernances[0].Id,
                    family.Id,
                    storage.Id,
                    batch.ProductDefinitionId,
                    1,
                    1,
                    0,
                    world.AbsoluteDay + 1));
            var foreignGovernance = new CountyGovernanceState
            {
                Id = "county_governance.foreign_test",
                CountyLocationId = world.Locations.Find(location =>
                    location.Id != world.CountyGovernances[0]
                        .CountyLocationId).Id
            };
            world.CountyGovernances.Add(foreignGovernance);
            Assert.Throws<InvalidOperationException>(() =>
                market.CreateBuyOrder(
                    world,
                    foreignGovernance.Id,
                    family.Id,
                    storage.Id,
                    batch.ProductDefinitionId,
                    1,
                    1,
                    0,
                    world.AbsoluteDay + 1));
            world.CountyGovernances.Remove(foreignGovernance);
            Assert.That(WorldSnapshotSerializer.Serialize(world, content),
                Is.EqualTo(before));
        }

        [Test]
        public void FormalMarket_NonCrossingOrdersExpireAndReturnAssets()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_405);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var candidates = world.Families.FindAll(family =>
                world.ProductBatches.Exists(batch =>
                    batch.OwnerFamilyId == family.Id && batch.Quantity >= 5));
            Assert.That(candidates.Count, Is.GreaterThanOrEqualTo(2));
            var seller = candidates[0];
            var buyer = candidates[1];
            var sellerStorage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == seller.Id);
            var buyerStorage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == buyer.Id);
            buyerStorage.Capacity += 10_000;
            buyer.Wealth = 1_000;
            var buyerWealthBefore = buyer.Wealth;
            var market = new FormalCountyMarketSystem(content);
            var productId = world.ProductBatches.Find(item =>
                item.OwnerFamilyId == seller.Id && item.Quantity >= 5)
                .ProductDefinitionId;
            var sell = market.CreateSellOrder(
                world,
                world.CountyGovernances[0].Id,
                seller.Id,
                sellerStorage.Id,
                productId,
                5,
                10,
                0,
                world.AbsoluteDay + 1);
            var buy = market.CreateBuyOrder(
                world,
                world.CountyGovernances[0].Id,
                buyer.Id,
                buyerStorage.Id,
                productId,
                5,
                5,
                0,
                world.AbsoluteDay + 1);

            market.ResolveDaily(world);
            Assert.That(world.FormalMarketTrades, Is.Empty);
            world.AbsoluteDay += 2;
            market.ResolveDaily(world);

            Assert.That(sell.Status,
                Is.EqualTo(FormalMarketOrderStatus.Expired));
            Assert.That(buy.Status,
                Is.EqualTo(FormalMarketOrderStatus.Expired));
            Assert.That(buyer.Wealth, Is.EqualTo(buyerWealthBefore));
            Assert.That(sell.BatchReservations.TrueForAll(item =>
                item.RemainingQuantity == 0), Is.True);
            Assert.That(world.FormalMarketTrades, Is.Empty);
            world.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionTwentyNineWithoutMarketFabrication()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_404);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var v29 = WorldSnapshotSerializer.Serialize(world, content)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 29");

            var migrated = WorldSnapshotSerializer.Deserialize(v29, content);

            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.FormalMarketOrders, Is.Empty);
            Assert.That(migrated.FormalMarketTrades, Is.Empty);
            Assert.That(migrated.FormalMarketPrices, Is.Empty);
            Assert.That(migrated.InventoryTransactions.TrueForAll(item =>
                string.IsNullOrEmpty(item.SourceFormalMarketOrderId)), Is.True);
            migrated.Validate();
        }

        [Test]
        public void FormalMarketCommand_PersistedWorkSettlesOnceAndPublishesEvent()
        {
            var fixture = PrepareFormalMarketCommandWorld(25_801);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            var eventHandler = new FormalMarketEventRecorder();
            runtime.RegisterEventHandler(eventHandler);
            Assert.That(
                fixture.Scheduler.EnsureDueCommand(fixture.World, runtime),
                Is.True);
            Assert.That(
                fixture.Scheduler.EnsureDueCommand(fixture.World, runtime),
                Is.False);
            Assert.That(fixture.World.PersistentWorldCommands.Count,
                Is.EqualTo(1));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World,
                    fixture.Content),
                fixture.Content);
            var resumedMarket = new FormalCountyMarketSystem(fixture.Content);
            var resumedScheduler = new FormalMarketDailyCommandScheduler(
                resumedMarket);
            var resumedRuntime = new WorldCommandRuntime();
            resumedRuntime.RegisterHandler(
                resumedScheduler.CreateCommandHandler());
            var resumedEvents = new FormalMarketEventRecorder();
            resumedRuntime.RegisterEventHandler(resumedEvents);

            var report = resumedRuntime.ProcessDue(loaded);
            resumedRuntime.DispatchPublishedEvents(loaded);
            var repeated = resumedRuntime.ProcessDue(loaded);

            Assert.That(report.ProcessedCommands, Is.EqualTo(1));
            Assert.That(report.CommittedTransactions, Is.EqualTo(1));
            Assert.That(report.PublishedEvents, Is.EqualTo(1));
            Assert.That(repeated.ProcessedCommands, Is.Zero);
            Assert.That(loaded.FormalMarketTrades.Count, Is.EqualTo(1));
            Assert.That(
                loaded.PersistentWorldCommands[0].Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));
            Assert.That(
                loaded.WorldCommandBatchResults[0].Transactions[0]
                    .TransactionKindId,
                Is.EqualTo(
                    FormalMarketDailyCommandScheduler.TransactionKindId));
            Assert.That(
                loaded.WorldEventOutbox[0].DispatchStatus,
                Is.EqualTo(WorldEventDispatchStatus.Dispatched));
            Assert.That(resumedEvents.HandledEventIds,
                Is.EqualTo(new[]
                {
                    FormalMarketDailyCommandScheduler.DailyEventId(
                        loaded.AbsoluteDay)
                }));
            loaded.Validate();
        }

        [Test]
        public void FormalMarketCommand_DateDriftRejectsWithoutBusinessMutation()
        {
            var fixture = PrepareFormalMarketCommandWorld(25_802);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommand(fixture.World, runtime);
            var sellerWealth = fixture.Seller.Wealth;
            var buyerWealth = fixture.Buyer.Wealth;
            var inventoryTransactions =
                fixture.World.InventoryTransactions.Count;
            var sellerRemaining = fixture.SellOrder.RemainingQuantity;
            var buyerRemaining = fixture.BuyOrder.RemainingQuantity;
            fixture.World.AbsoluteDay++;

            Assert.Throws<InvalidOperationException>(() =>
                runtime.ProcessDue(fixture.World));

            Assert.That(fixture.World.FormalMarketTrades, Is.Empty);
            Assert.That(fixture.Seller.Wealth, Is.EqualTo(sellerWealth));
            Assert.That(fixture.Buyer.Wealth, Is.EqualTo(buyerWealth));
            Assert.That(fixture.World.InventoryTransactions.Count,
                Is.EqualTo(inventoryTransactions));
            Assert.That(fixture.SellOrder.RemainingQuantity,
                Is.EqualTo(sellerRemaining));
            Assert.That(fixture.BuyOrder.RemainingQuantity,
                Is.EqualTo(buyerRemaining));
            Assert.That(
                fixture.World.PersistentWorldCommands[0].Status,
                Is.EqualTo(PersistentWorldCommandStatus.Pending));
            Assert.That(
                fixture.World.WorldCommandBatchResults[0].Outcome,
                Is.EqualTo(WorldCommandBatchOutcome.Rejected));
            Assert.That(fixture.World.WorldEventOutbox, Is.Empty);
            fixture.World.Validate();
        }

        [Test]
        public void FormalMarketCommand_NoWorkDoesNotCreateEmptyCommand()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_803);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var market = new FormalCountyMarketSystem(content);
            var scheduler = new FormalMarketDailyCommandScheduler(market);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());

            Assert.That(scheduler.EnsureDueCommand(world, runtime), Is.False);
            Assert.That(world.PersistentWorldCommands, Is.Empty);
            Assert.That(world.WorldCommandBatchResults, Is.Empty);
        }

        [Test]
        public void FormalMarketCommand_WorldSimulatorUsesPersistentPipeline()
        {
            var fixture = PrepareFormalMarketCommandWorld(25_804);
            var simulator = new WorldSimulator(
                fixture.World.MasterSeed,
                fixture.Content);

            simulator.AdvanceDays(fixture.World, 1);

            Assert.That(fixture.World.FormalMarketTrades.Count,
                Is.EqualTo(1));
            var command = fixture.World.PersistentWorldCommands.Find(item =>
                item.CommandTypeId ==
                    FormalMarketDailyCommandScheduler.CommandTypeId);
            Assert.That(command, Is.Not.Null);
            Assert.That(command.Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));
            Assert.That(fixture.World.WorldCommandBatchResults.Exists(item =>
                item.CommandIds.Contains(command.Id) &&
                item.Transactions.Exists(transaction =>
                    transaction.TransactionKindId ==
                        FormalMarketDailyCommandScheduler.TransactionKindId)),
                Is.True);
            Assert.That(fixture.World.WorldEventOutbox.Exists(item =>
                item.EventTypeId ==
                    FormalMarketDailyCommandScheduler.EventTypeId &&
                item.DispatchStatus ==
                    WorldEventDispatchStatus.Dispatched),
                Is.True);
            fixture.World.Validate();
        }

        [Test]
        public void FormalMarketCommand_IdenticalWorldsRemainDeterministic()
        {
            var left = PrepareFormalMarketCommandWorld(25_805);
            var right = PrepareFormalMarketCommandWorld(25_805);

            new WorldSimulator(left.World.MasterSeed, left.Content)
                .AdvanceDays(left.World, 1);
            new WorldSimulator(right.World.MasterSeed, right.Content)
                .AdvanceDays(right.World, 1);

            Assert.That(
                WorldSnapshotSerializer.Serialize(right.World, right.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    left.World,
                    left.Content)));
        }

        [Test]
        public void CivilianFreightPlanningCommand_PersistedWorkDispatchesOnceAndPublishesEvent()
        {
            var fixture = PrepareCivilianFreightWorld(25_901, 1_000);
            ConfigureCivilianFreightRouteChoices(fixture.World);
            fixture.FreightSystem.RegisterCarrier(
                fixture.World,
                NewCivilianCarrierRegistration(
                    fixture,
                    CivilianFreightRoutePolicyIds.ShortestKnown));
            var scheduler = new CivilianFreightPlanningCommandScheduler(
                fixture.FreightSystem);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            Assert.That(scheduler.EnsureDueCommand(fixture.World, runtime),
                Is.True);
            Assert.That(scheduler.EnsureDueCommand(fixture.World, runtime),
                Is.False);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World,
                    fixture.Content),
                fixture.Content);
            var resumedFreight = new CivilianFreightSystem(
                loaded.MasterSeed,
                fixture.Content);
            var resumedScheduler =
                new CivilianFreightPlanningCommandScheduler(resumedFreight);
            var resumedRuntime = new WorldCommandRuntime();
            resumedRuntime.RegisterHandler(
                resumedScheduler.CreateCommandHandler());
            var recorder = new CivilianFreightPlanningEventRecorder();
            resumedRuntime.RegisterEventHandler(recorder);

            var report = resumedRuntime.ProcessDue(loaded);
            resumedRuntime.DispatchPublishedEvents(loaded);
            var repeated = resumedRuntime.ProcessDue(loaded);

            Assert.That(report.ProcessedCommands, Is.EqualTo(1));
            Assert.That(report.CommittedTransactions, Is.EqualTo(1));
            Assert.That(report.PublishedEvents, Is.EqualTo(1));
            Assert.That(repeated.ProcessedCommands, Is.Zero);
            Assert.That(loaded.CivilianFreightDemands.Count, Is.EqualTo(1));
            Assert.That(loaded.CivilianCarrierOffers.Count, Is.EqualTo(1));
            Assert.That(loaded.CivilianFreights.Count, Is.EqualTo(1));
            Assert.That(loaded.PersistentWorldCommands[0].Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));
            Assert.That(
                loaded.WorldCommandBatchResults[0].Transactions[0]
                    .TransactionKindId,
                Is.EqualTo(
                    CivilianFreightPlanningCommandScheduler.TransactionKindId));
            Assert.That(loaded.WorldEventOutbox[0].DispatchStatus,
                Is.EqualTo(WorldEventDispatchStatus.Dispatched));
            Assert.That(recorder.HandledEventIds, Is.EqualTo(new[]
            {
                CivilianFreightPlanningCommandScheduler.DailyEventId(
                    loaded.AbsoluteDay)
            }));
            loaded.Validate();
        }

        [Test]
        public void CivilianFreightPlanningCommand_DateDriftRejectsWithoutBusinessMutation()
        {
            var fixture = PrepareCivilianFreightWorld(25_902, 1_000);
            ConfigureCivilianFreightRouteChoices(fixture.World);
            fixture.FreightSystem.RegisterCarrier(
                fixture.World,
                NewCivilianCarrierRegistration(
                    fixture,
                    CivilianFreightRoutePolicyIds.ShortestKnown));
            var scheduler = new CivilianFreightPlanningCommandScheduler(
                fixture.FreightSystem);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            scheduler.EnsureDueCommand(fixture.World, runtime);
            var inventoryTransactions =
                fixture.World.InventoryTransactions.Count;
            var buyerWealth = fixture.Buyer.Wealth;
            var sellerWealth = fixture.Seller.Wealth;
            fixture.World.AbsoluteDay++;

            Assert.Throws<InvalidOperationException>(() =>
                runtime.ProcessDue(fixture.World));

            Assert.That(fixture.World.CivilianFreightDemands, Is.Empty);
            Assert.That(fixture.World.CivilianCarrierOffers, Is.Empty);
            Assert.That(fixture.World.CivilianFreights, Is.Empty);
            Assert.That(fixture.World.InventoryTransactions.Count,
                Is.EqualTo(inventoryTransactions));
            Assert.That(fixture.Buyer.Wealth, Is.EqualTo(buyerWealth));
            Assert.That(fixture.Seller.Wealth, Is.EqualTo(sellerWealth));
            Assert.That(fixture.World.PersistentWorldCommands[0].Status,
                Is.EqualTo(PersistentWorldCommandStatus.Pending));
            Assert.That(fixture.World.WorldCommandBatchResults[0].Outcome,
                Is.EqualTo(WorldCommandBatchOutcome.Rejected));
            Assert.That(fixture.World.WorldEventOutbox, Is.Empty);
            fixture.World.Validate();
        }

        [Test]
        public void CivilianFreightPlanningCommand_NoWorkDoesNotCreateEmptyCommand()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_903);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var freight = new CivilianFreightSystem(
                world.MasterSeed,
                content);
            var scheduler = new CivilianFreightPlanningCommandScheduler(
                freight);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());

            Assert.That(scheduler.EnsureDueCommand(world, runtime), Is.False);
            Assert.That(world.PersistentWorldCommands, Is.Empty);
            Assert.That(world.WorldCommandBatchResults, Is.Empty);
        }

        [Test]
        public void CivilianFreightPlanningCommand_WorldSimulatorUsesPersistentPipeline()
        {
            var fixture = PrepareCivilianFreightWorld(25_904, 1_000);
            ConfigureCivilianFreightRouteChoices(fixture.World);
            fixture.FreightSystem.RegisterCarrier(
                fixture.World,
                NewCivilianCarrierRegistration(
                    fixture,
                    CivilianFreightRoutePolicyIds.ShortestKnown));
            var simulator = new WorldSimulator(
                fixture.World.MasterSeed,
                fixture.Content);

            simulator.AdvanceDays(fixture.World, 1);

            Assert.That(fixture.World.CivilianFreights.Count, Is.EqualTo(1));
            var command = fixture.World.PersistentWorldCommands.Find(item =>
                item.CommandTypeId ==
                    CivilianFreightPlanningCommandScheduler.CommandTypeId);
            Assert.That(command, Is.Not.Null);
            Assert.That(command.Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));
            Assert.That(fixture.World.WorldCommandBatchResults.Exists(item =>
                item.CommandIds.Contains(command.Id) &&
                item.Transactions.Exists(transaction =>
                    transaction.TransactionKindId ==
                        CivilianFreightPlanningCommandScheduler
                            .TransactionKindId)),
                Is.True);
            Assert.That(fixture.World.WorldEventOutbox.Exists(item =>
                item.EventTypeId ==
                    CivilianFreightPlanningCommandScheduler.EventTypeId &&
                item.DispatchStatus ==
                    WorldEventDispatchStatus.Dispatched),
                Is.True);
            fixture.World.Validate();
        }

        [Test]
        public void CivilianFreightPlanningCommand_IdenticalWorldsRemainDeterministic()
        {
            var left = PrepareCivilianFreightWorld(25_905, 1_000);
            var right = PrepareCivilianFreightWorld(25_905, 1_000);
            ConfigureCivilianFreightRouteChoices(left.World);
            ConfigureCivilianFreightRouteChoices(right.World);
            left.FreightSystem.RegisterCarrier(
                left.World,
                NewCivilianCarrierRegistration(
                    left,
                    CivilianFreightRoutePolicyIds.ShortestKnown));
            right.FreightSystem.RegisterCarrier(
                right.World,
                NewCivilianCarrierRegistration(
                    right,
                    CivilianFreightRoutePolicyIds.ShortestKnown));

            new WorldSimulator(left.World.MasterSeed, left.Content)
                .AdvanceDays(left.World, 1);
            new WorldSimulator(right.World.MasterSeed, right.Content)
                .AdvanceDays(right.World, 1);

            Assert.That(
                WorldSnapshotSerializer.Serialize(right.World, right.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    left.World,
                    left.Content)));
        }

        [Test]
        public void FormalHouseholdFoodCommand_PersistedWorkConsumesOnceAndPublishesShortfall()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1001);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            Assert.That(
                fixture.Scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.EqualTo(1));
            Assert.That(
                fixture.Scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.Zero);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World,
                    fixture.Content),
                fixture.Content);
            var resumedVillageLife = new VillageLifeSystem(
                loaded.MasterSeed,
                fixture.Content);
            var resumedScheduler =
                new FormalHouseholdFoodMonthlyCommandScheduler(
                    resumedVillageLife);
            var resumedRuntime = new WorldCommandRuntime();
            resumedRuntime.RegisterHandler(
                resumedScheduler.CreateCommandHandler());
            var recorder = new FormalHouseholdFoodShortfallEventRecorder();
            resumedRuntime.RegisterEventHandler(recorder);
            var ledgerBefore = loaded.VillageLedgerEntries.Count;

            var report = resumedRuntime.ProcessDue(loaded);
            resumedRuntime.DispatchPublishedEvents(loaded);
            var repeated = resumedRuntime.ProcessDue(loaded);

            Assert.That(report.ProcessedCommands, Is.EqualTo(1));
            Assert.That(report.CommittedTransactions, Is.EqualTo(1));
            Assert.That(report.PublishedEvents, Is.EqualTo(1));
            Assert.That(repeated.ProcessedCommands, Is.Zero);
            Assert.That(loaded.VillageLedgerEntries.Count,
                Is.GreaterThan(ledgerBefore));
            Assert.That(loaded.Families.Exists(item =>
                item.FoodSecurityBasisPoints < 10_000), Is.True);
            Assert.That(loaded.PersistentWorldCommands[0].Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));
            Assert.That(
                loaded.WorldCommandBatchResults[0].Transactions[0]
                    .TransactionKindId,
                Is.EqualTo(
                    FormalHouseholdFoodMonthlyCommandScheduler
                        .TransactionKindId));
            Assert.That(loaded.WorldEventOutbox[0].DispatchStatus,
                Is.EqualTo(WorldEventDispatchStatus.Dispatched));
            Assert.That(recorder.HandledEventIds, Is.EqualTo(new[]
            {
                FormalHouseholdFoodMonthlyCommandScheduler
                    .MonthlyShortfallEventId(
                        loaded.AbsoluteDay,
                        loaded.Villages[0].Id)
            }));
            loaded.Validate();
        }

        [Test]
        public void FormalHouseholdFoodCommand_DateDriftRejectsWithoutBusinessMutation()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1002);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            var inventoryTransactions =
                fixture.World.InventoryTransactions.Count;
            var villageLedger = fixture.World.VillageLedgerEntries.Count;
            var totalHealth = TotalLivingHealth(fixture.World);
            var totalFood = TotalProductQuantity(fixture.World);
            fixture.World.AbsoluteDay++;

            Assert.Throws<InvalidOperationException>(() =>
                runtime.ProcessDue(fixture.World));

            Assert.That(fixture.World.InventoryTransactions.Count,
                Is.EqualTo(inventoryTransactions));
            Assert.That(fixture.World.VillageLedgerEntries.Count,
                Is.EqualTo(villageLedger));
            Assert.That(TotalLivingHealth(fixture.World),
                Is.EqualTo(totalHealth));
            Assert.That(TotalProductQuantity(fixture.World),
                Is.EqualTo(totalFood));
            Assert.That(fixture.World.PersistentWorldCommands[0].Status,
                Is.EqualTo(PersistentWorldCommandStatus.Pending));
            Assert.That(fixture.World.WorldCommandBatchResults[0].Outcome,
                Is.EqualTo(WorldCommandBatchOutcome.Rejected));
            Assert.That(fixture.World.WorldEventOutbox, Is.Empty);
            fixture.World.Validate();
        }

        [Test]
        public void FormalHouseholdFoodCommand_NoDueWorkDoesNotCreateEmptyCommand()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1003);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.World.AbsoluteDay = 29;

            Assert.That(
                fixture.Scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.Zero);
            Assert.That(fixture.World.PersistentWorldCommands, Is.Empty);

            fixture.World.AbsoluteDay = 30;
            fixture.World.Villages[0].LastSettlementDay = 30;
            Assert.That(
                fixture.Scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.Zero);
            Assert.That(fixture.World.PersistentWorldCommands, Is.Empty);
        }

        [Test]
        public void FormalHouseholdFoodCommand_WorldSimulatorConsumesThroughPersistentPipeline()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1004);
            fixture.World.AbsoluteDay = 29;
            var simulator = new WorldSimulator(
                fixture.World.MasterSeed,
                fixture.Content);

            simulator.AdvanceDays(fixture.World, 1);

            var command = fixture.World.PersistentWorldCommands.Find(item =>
                item.CommandTypeId ==
                    FormalHouseholdFoodMonthlyCommandScheduler.CommandTypeId);
            Assert.That(command, Is.Not.Null);
            Assert.That(command.Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));
            Assert.That(fixture.World.Villages[0].LastSettlementDay,
                Is.EqualTo(30));
            Assert.That(fixture.World.WorldCommandBatchResults.Exists(item =>
                item.CommandIds.Contains(command.Id) &&
                item.Transactions.Exists(transaction =>
                    transaction.TransactionKindId ==
                        FormalHouseholdFoodMonthlyCommandScheduler
                            .TransactionKindId)),
                Is.True);
            Assert.That(fixture.World.WorldEventOutbox.Exists(item =>
                item.EventTypeId ==
                    FormalHouseholdFoodMonthlyCommandScheduler
                        .ShortfallEventTypeId &&
                item.DispatchStatus ==
                    WorldEventDispatchStatus.Dispatched),
                Is.True);
            AssertSingleFoodConsumptionLedgerPerFamily(
                fixture.World,
                fixture.World.Villages[0].Id,
                30);
            fixture.World.Validate();
        }

        [Test]
        public void FormalHouseholdFoodCommand_IdenticalWorldsRemainDeterministic()
        {
            var left = PrepareFormalHouseholdFoodCommandWorld(25_1005);
            var right = PrepareFormalHouseholdFoodCommandWorld(25_1005);
            left.World.AbsoluteDay = 29;
            right.World.AbsoluteDay = 29;

            new WorldSimulator(left.World.MasterSeed, left.Content)
                .AdvanceDays(left.World, 1);
            new WorldSimulator(right.World.MasterSeed, right.Content)
                .AdvanceDays(right.World, 1);

            Assert.That(
                WorldSnapshotSerializer.Serialize(right.World, right.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    left.World,
                    left.Content)));
        }

        [Test]
        public void HouseholdReliefPickup_MonthlyShortfallDeliversConcreteFoodOnce()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1601);
            var monthlyRuntime = new WorldCommandRuntime();
            monthlyRuntime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(
                fixture.World, monthlyRuntime);
            monthlyRuntime.ProcessDue(fixture.World);

            Assert.That(fixture.World.HouseholdReliefPickups, Is.Not.Empty);
            Assert.That(
                fixture.World.HouseholdReliefConsumptions.Count,
                Is.EqualTo(fixture.World.HouseholdReliefPickups.Count));
            var requests = new List<HouseholdReliefPickupState>(
                fixture.World.HouseholdReliefPickups);
            requests.Sort(CompareHouseholdReliefPickupPriority);
            var request = requests[0];
            var openingFamilyFood = TotalFamilyFood(
                fixture.World, request.FamilyId);
            TransferCountyFoodToVillage(
                fixture.World, fixture.Content, 1);
            var pickupScheduler = new HouseholdReliefPickupCommandScheduler(
                new HouseholdReliefPickupSystem(fixture.Content));
            var pickupRuntime = new WorldCommandRuntime();
            pickupRuntime.RegisterHandler(
                pickupScheduler.CreateCommandHandler());

            Assert.That(
                pickupScheduler.EnsureDueCommands(
                    fixture.World, pickupRuntime),
                Is.EqualTo(1));
            var report = pickupRuntime.ProcessDue(fixture.World);
            var repeated = pickupRuntime.ProcessDue(fixture.World);

            Assert.That(report.CommittedTransactions, Is.EqualTo(1));
            Assert.That(report.PublishedEvents, Is.EqualTo(1));
            Assert.That(repeated.ProcessedCommands, Is.Zero);
            Assert.That(request.DeliveredPhysicalQuantity, Is.EqualTo(1));
            Assert.That(request.DeliveredNutritionBasisUnits, Is.GreaterThan(0));
            Assert.That(request.InventoryTransactionIds, Has.Count.EqualTo(1));
            Assert.That(TotalFamilyFood(fixture.World, request.FamilyId),
                Is.EqualTo(openingFamilyFood + 1));
            Assert.That(fixture.World.InventoryTransactions.Find(item =>
                    item.Id == request.InventoryTransactionIds[0]).Type,
                Is.EqualTo(
                    InventoryTransactionType.FoodVillageReliefTransferred));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content),
                fixture.Content);
            Assert.That(loaded.HouseholdReliefPickups.Find(item =>
                    item.Id == request.Id).DeliveredPhysicalQuantity,
                Is.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void HouseholdReliefPickup_BlockedHouseholdCreatesNoEmptyCommand()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1602);
            var monthlyRuntime = new WorldCommandRuntime();
            monthlyRuntime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(
                fixture.World, monthlyRuntime);
            monthlyRuntime.ProcessDue(fixture.World);
            TransferCountyFoodToVillage(
                fixture.World, fixture.Content, 1);
            for (var i = 0;
                 i < fixture.World.HouseholdReliefPickups.Count;
                 i++)
            {
                var request = fixture.World.HouseholdReliefPickups[i];
                var storage = fixture.World.VillageFacilities.Find(item =>
                    item.Kind == VillageFacilityKind.HouseholdGranary &&
                    item.OwnerFamilyId == request.FamilyId);
                storage.Capacity = (int)storage.InventoryUnits;
            }
            var scheduler = new HouseholdReliefPickupCommandScheduler(
                new HouseholdReliefPickupSystem(fixture.Content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());

            Assert.That(
                scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.Zero);
            Assert.That(fixture.World.PersistentWorldCommands.Exists(item =>
                item.CommandTypeId ==
                    HouseholdReliefPickupCommandScheduler.CommandTypeId),
                Is.False);
            Assert.That(fixture.World.HouseholdReliefPickups.TrueForAll(item =>
                item.Status == HouseholdReliefPickupStatus.Waiting), Is.True);
            fixture.World.Validate();
        }

        [Test]
        public void HouseholdReliefPickup_WorldSimulatorRunsSegmentPipelineDeterministically()
        {
            var left = PrepareFormalHouseholdFoodCommandWorld(25_1603);
            var right = PrepareFormalHouseholdFoodCommandWorld(25_1603);
            SeedMonthlyShortfallAndVillageFood(left, 2);
            SeedMonthlyShortfallAndVillageFood(right, 2);

            new WorldSimulator(left.World.MasterSeed, left.Content)
                .AdvanceSegments(left.World, 1);
            new WorldSimulator(right.World.MasterSeed, right.Content)
                .AdvanceSegments(right.World, 1);

            Assert.That(left.World.HouseholdReliefPickups.Exists(item =>
                item.DeliveredPhysicalQuantity > 0), Is.True);
            Assert.That(left.World.HouseholdReliefConsumptions.Exists(item =>
                item.ConsumedPhysicalQuantity > 0), Is.True);
            Assert.That(left.World.PersistentWorldCommands.Exists(item =>
                item.CommandTypeId ==
                    HouseholdReliefPickupCommandScheduler.CommandTypeId &&
                item.Status == PersistentWorldCommandStatus.Completed),
                Is.True);
            Assert.That(left.World.PersistentWorldCommands.Exists(item =>
                item.CommandTypeId ==
                    HouseholdReliefConsumptionCommandScheduler.CommandTypeId &&
                item.Status == PersistentWorldCommandStatus.Completed),
                Is.True);
            Assert.That(
                WorldSnapshotSerializer.Serialize(right.World, right.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    left.World, left.Content)));
        }

        [Test]
        public void HouseholdReliefPickup_ValidationRejectsTamperedReceipt()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1604);
            SeedMonthlyShortfallAndVillageFood(fixture, 1);
            new WorldSimulator(fixture.World.MasterSeed, fixture.Content)
                .AdvanceSegments(fixture.World, 1);
            var request = fixture.World.HouseholdReliefPickups.Find(item =>
                item.DeliveredPhysicalQuantity > 0);
            request.DeliveredPhysicalQuantity++;

            Assert.Throws<InvalidOperationException>(() =>
                fixture.World.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionThirtySevenToEmptyHouseholdReliefPickups()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_1605);
            world.ProductionContentManifest = content.CreateManifest();
            var json = WorldSnapshotSerializer.Serialize(world, content)
                .Replace(
                    "\"SchemaVersion\": " +
                        WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 37")
                .Replace(
                    "\"HouseholdReliefPickups\": []",
                    "\"HouseholdReliefPickups\": null")
                .Replace(
                    "\"HouseholdReliefConsumptions\": []",
                    "\"HouseholdReliefConsumptions\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(json, content);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.HouseholdReliefPickups, Is.Empty);
            Assert.That(loaded.HouseholdReliefConsumptions, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void HouseholdReliefConsumption_PickupDoesNotHealUntilTracedFoodIsEaten()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1701);
            var monthlyRuntime = new WorldCommandRuntime();
            monthlyRuntime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(
                fixture.World, monthlyRuntime);
            monthlyRuntime.ProcessDue(fixture.World);

            var orderedPickups = new List<HouseholdReliefPickupState>(
                fixture.World.HouseholdReliefPickups);
            orderedPickups.Sort(CompareHouseholdReliefPickupPriority);
            var pickup = orderedPickups[0];
            var claim = fixture.World.HouseholdReliefConsumptions.Find(item =>
                item.PickupId == pickup.Id);
            var affected = claim.AffectedPeople[0];
            var person = fixture.World.People.Find(item =>
                item.Id == affected.PersonId);
            var healthAfterShortfall = person.HealthBasisPoints;
            var livelihoodAfterShortfall = person.Needs.Livelihood;
            var physicalFood =
                (pickup.RequestedNutritionBasisUnits + 9_799L) / 9_800L + 1;
            TransferCountyFoodToVillage(
                fixture.World, fixture.Content, physicalFood);

            var pickupScheduler = new HouseholdReliefPickupCommandScheduler(
                new HouseholdReliefPickupSystem(fixture.Content));
            var pickupRuntime = new WorldCommandRuntime();
            pickupRuntime.RegisterHandler(
                pickupScheduler.CreateCommandHandler());
            pickupScheduler.EnsureDueCommands(
                fixture.World, pickupRuntime);
            pickupRuntime.ProcessDue(fixture.World);

            Assert.That(claim.ConsumedPhysicalQuantity, Is.Zero);
            Assert.That(person.HealthBasisPoints,
                Is.EqualTo(healthAfterShortfall));
            Assert.That(person.Needs.Livelihood,
                Is.EqualTo(livelihoodAfterShortfall));
            var foodAfterPickup = TotalFamilyFood(
                fixture.World, claim.FamilyId);

            var consumptionScheduler =
                new HouseholdReliefConsumptionCommandScheduler(
                    new HouseholdReliefConsumptionSystem(fixture.Content));
            var consumptionRuntime = new WorldCommandRuntime();
            consumptionRuntime.RegisterHandler(
                consumptionScheduler.CreateCommandHandler());
            Assert.That(
                consumptionScheduler.EnsureDueCommands(
                    fixture.World, consumptionRuntime),
                Is.EqualTo(1));
            var report = consumptionRuntime.ProcessDue(fixture.World);
            var repeated = consumptionRuntime.ProcessDue(fixture.World);

            Assert.That(report.CommittedTransactions, Is.EqualTo(1));
            Assert.That(repeated.ProcessedCommands, Is.Zero);
            Assert.That(claim.Status,
                Is.EqualTo(HouseholdReliefConsumptionStatus.Fulfilled));
            Assert.That(claim.ConsumedPhysicalQuantity, Is.GreaterThan(0));
            long affectedConsumedNutrition = 0;
            for (var affectedIndex = 0;
                 affectedIndex < claim.AffectedPeople.Count;
                 affectedIndex++)
            {
                affectedConsumedNutrition = checked(
                    affectedConsumedNutrition +
                    claim.AffectedPeople[affectedIndex]
                        .ConsumedNutritionBasisUnits);
            }
            Assert.That(
                affectedConsumedNutrition +
                claim.PreparedNutritionBasisUnits,
                Is.EqualTo(claim.ConsumedNutritionBasisUnits));
            Assert.That(
                TotalFamilyFood(fixture.World, claim.FamilyId),
                Is.LessThan(foodAfterPickup));
            Assert.That(person.HealthBasisPoints,
                Is.EqualTo(
                    healthAfterShortfall +
                    affected.AppliedHealthDamageBasisPoints),
                $"person={affected.PersonId} allocated=" +
                $"{affected.AllocatedNutritionBasisUnits} consumed=" +
                $"{affected.ConsumedNutritionBasisUnits} recovered=" +
                $"{affected.RecoveredHealthBasisPoints} applied=" +
                $"{affected.AppliedHealthDamageBasisPoints}");
            Assert.That(person.Needs.Livelihood,
                Is.EqualTo(
                    livelihoodAfterShortfall -
                    affected.AppliedLivelihoodPressureBasisPoints),
                $"person={affected.PersonId} allocated=" +
                $"{affected.AllocatedNutritionBasisUnits} consumed=" +
                $"{affected.ConsumedNutritionBasisUnits} recovered=" +
                $"{affected.RecoveredLivelihoodBasisPoints} applied=" +
                $"{affected.AppliedLivelihoodPressureBasisPoints} opening=" +
                $"{livelihoodAfterShortfall}");
            Assert.That(claim.InventoryTransactionIds, Is.Not.Empty);
            var consumptionTransaction =
                fixture.World.InventoryTransactions.Find(item =>
                    item.Id == claim.InventoryTransactionIds[0]);
            Assert.That(
                consumptionTransaction.SourceHouseholdReliefConsumptionId,
                Is.EqualTo(claim.Id));
            Assert.That(consumptionTransaction.Lines.TrueForAll(line =>
            {
                var batch = fixture.World.ProductBatches.Find(item =>
                    item.Id == line.BatchId);
                return line.QuantityDelta >= 0 ||
                    pickup.InventoryTransactionIds.Contains(
                        batch.SourceTransactionId);
            }), Is.True);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content),
                fixture.Content);
            Assert.That(loaded.HouseholdReliefConsumptions.Find(item =>
                    item.Id == claim.Id).ConsumedPhysicalQuantity,
                Is.EqualTo(claim.ConsumedPhysicalQuantity));
            loaded.Validate();
        }

        [Test]
        public void HouseholdReliefConsumption_DoesNotHealAffectedPersonAwayOnLevy()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1702);
            var village = fixture.World.Villages[0];
            var monthlyRuntime = new WorldCommandRuntime();
            monthlyRuntime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(
                fixture.World, monthlyRuntime);
            monthlyRuntime.ProcessDue(fixture.World);
            var orderedPickups = new List<HouseholdReliefPickupState>(
                fixture.World.HouseholdReliefPickups);
            orderedPickups.Sort(CompareHouseholdReliefPickupPriority);
            HouseholdReliefConsumptionState target = null;
            for (var i = 0; i < orderedPickups.Count && target == null; i++)
            {
                var candidate = fixture.World.HouseholdReliefConsumptions.Find(
                    item => item.PickupId == orderedPickups[i].Id);
                if (candidate != null && candidate.AffectedPeople.Count >= 2)
                {
                    target = candidate;
                }
            }
            Assert.That(target, Is.Not.Null, "multi-person relief claim");
            var excludedAffected = target.AffectedPeople[1];
            var excluded = fixture.World.People.Find(item =>
                item.Id == excludedAffected.PersonId);
            var originalDuty = excluded.LocalDuty;
            excluded.LocalDuty = LocalDutyKind.Levy;
            var openingHealth = excluded.HealthBasisPoints;
            var openingLivelihood = excluded.Needs.Livelihood;
            long nutritionThroughTarget = 0;
            for (var i = 0; i < orderedPickups.Count; i++)
            {
                nutritionThroughTarget = checked(
                    nutritionThroughTarget +
                    orderedPickups[i].RequestedNutritionBasisUnits);
                if (orderedPickups[i].FamilyId == target.FamilyId)
                {
                    break;
                }
            }
            TransferCountyFoodToVillage(
                fixture.World,
                fixture.Content,
                (nutritionThroughTarget + 9_799L) / 9_800L + 1);
            var pickup = new HouseholdReliefPickupSystem(fixture.Content);
            pickup.Resolve(fixture.World, village.Id);
            var consumption = new HouseholdReliefConsumptionSystem(
                fixture.Content);
            consumption.Resolve(fixture.World, village.Id);

            Assert.That(target.ConsumedPhysicalQuantity, Is.GreaterThan(0));
            Assert.That(excluded.HealthBasisPoints, Is.EqualTo(openingHealth));
            Assert.That(excluded.Needs.Livelihood,
                Is.EqualTo(openingLivelihood));
            Assert.That(excludedAffected.ConsumedNutritionBasisUnits,
                Is.Zero);
            Assert.That(target.RemainingNutritionBasisUnits,
                Is.EqualTo(excludedAffected.AllocatedNutritionBasisUnits));
            Assert.That(target.Status,
                Is.EqualTo(
                    HouseholdReliefConsumptionStatus.PartiallyConsumed));

            excluded.LocalDuty = originalDuty;
            consumption.Resolve(fixture.World, village.Id);

            Assert.That(target.Status,
                Is.EqualTo(HouseholdReliefConsumptionStatus.Fulfilled));
            Assert.That(excludedAffected.ConsumedNutritionBasisUnits,
                Is.GreaterThanOrEqualTo(
                    excludedAffected.AllocatedNutritionBasisUnits));
            Assert.That(excluded.HealthBasisPoints,
                Is.EqualTo(
                    openingHealth +
                    excludedAffected.AppliedHealthDamageBasisPoints));
            Assert.That(excluded.Needs.Livelihood,
                Is.EqualTo(
                    openingLivelihood -
                    excludedAffected.AppliedLivelihoodPressureBasisPoints));
            fixture.World.Validate();
        }

        [Test]
        public void HouseholdReliefAllocation_MonthlyShortfallClosesExactPersonQuotas()
        {
            var left = PrepareFormalHouseholdFoodCommandWorld(25_1801);
            var right = PrepareFormalHouseholdFoodCommandWorld(25_1801);
            var fixtures = new[] { left, right };
            for (var fixtureIndex = 0;
                 fixtureIndex < fixtures.Length;
                 fixtureIndex++)
            {
                var runtime = new WorldCommandRuntime();
                runtime.RegisterHandler(
                    fixtures[fixtureIndex].Scheduler.CreateCommandHandler());
                fixtures[fixtureIndex].Scheduler.EnsureDueCommands(
                    fixtures[fixtureIndex].World, runtime);
                runtime.ProcessDue(fixtures[fixtureIndex].World);
                Assert.That(
                    fixtures[fixtureIndex].World.HouseholdReliefConsumptions,
                    Is.Not.Empty);
                for (var claimIndex = 0;
                     claimIndex < fixtures[fixtureIndex].World
                        .HouseholdReliefConsumptions.Count;
                     claimIndex++)
                {
                    var claim = fixtures[fixtureIndex].World
                        .HouseholdReliefConsumptions[claimIndex];
                    Assert.That(claim.AllocationPolicyId,
                        Is.EqualTo(
                            HouseholdReliefAllocationPolicyIds
                                .ProportionalIndividualNeed));
                    long allocated = 0;
                    for (var affectedIndex = 0;
                         affectedIndex < claim.AffectedPeople.Count;
                         affectedIndex++)
                    {
                        var affected = claim.AffectedPeople[affectedIndex];
                        Assert.That(affected.RequiredNutritionBasisUnits,
                            Is.EqualTo(20_000L).Or.EqualTo(30_000L));
                        Assert.That(affected.AllocatedNutritionBasisUnits,
                            Is.GreaterThanOrEqualTo(0));
                        Assert.That(affected.ConsumedNutritionBasisUnits,
                            Is.Zero);
                        allocated = checked(
                            allocated +
                            affected.AllocatedNutritionBasisUnits);
                    }
                    Assert.That(allocated,
                        Is.EqualTo(claim.RequestedNutritionBasisUnits));
                }
                fixtures[fixtureIndex].World.Validate();
            }

            Assert.That(
                WorldSnapshotSerializer.Serialize(right.World, right.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    left.World, left.Content)));
        }

        [Test]
        public void HouseholdReliefAllocation_ValidationRejectsUnclosedPersonQuota()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1802);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            fixture.World.HouseholdReliefConsumptions[0]
                .AffectedPeople[0].AllocatedNutritionBasisUnits++;

            Assert.Throws<InvalidOperationException>(() =>
                fixture.World.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionThirtyNineToLegacySharedReliefAllocation()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1803);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            var json = WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content)
                .Replace(
                    "\"SchemaVersion\": " +
                        WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 39");

            var loaded = WorldSnapshotSerializer.Deserialize(
                json, fixture.Content);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.HouseholdReliefConsumptions, Is.Not.Empty);
            for (var claimIndex = 0;
                 claimIndex < loaded.HouseholdReliefConsumptions.Count;
                 claimIndex++)
            {
                var claim = loaded.HouseholdReliefConsumptions[claimIndex];
                Assert.That(claim.AllocationPolicyId,
                    Is.EqualTo(
                        HouseholdReliefAllocationPolicyIds
                            .LegacyHouseholdShared));
                for (var affectedIndex = 0;
                     affectedIndex < claim.AffectedPeople.Count;
                     affectedIndex++)
                {
                    var affected = claim.AffectedPeople[affectedIndex];
                    Assert.That(affected.RequiredNutritionBasisUnits,
                        Is.EqualTo(-1));
                    Assert.That(affected.AllocatedNutritionBasisUnits,
                        Is.EqualTo(-1));
                    Assert.That(affected.ConsumedNutritionBasisUnits,
                        Is.EqualTo(-1));
                }
            }
            loaded.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionThirtyEightWithoutInventingReliefRecovery()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_1703);
            world.ProductionContentManifest = content.CreateManifest();
            var json = WorldSnapshotSerializer.Serialize(world, content)
                .Replace(
                    "\"SchemaVersion\": " +
                        WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 38")
                .Replace(
                    "\"HouseholdReliefConsumptions\": []",
                    "\"HouseholdReliefConsumptions\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(json, content);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.HouseholdReliefConsumptions, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void HouseholdReliefPriority_ScarceFoodServesMoreSevereHouseholdFirst()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1901);
            var village = fixture.World.Villages[0];
            FamilyState partiallyFedFamily = null;
            for (var householdIndex = village.HouseholdIds.Count - 1;
                 householdIndex >= 0 && partiallyFedFamily == null;
                 householdIndex--)
            {
                var candidateId = village.HouseholdIds[householdIndex];
                partiallyFedFamily = fixture.World.Families.Find(item =>
                    item.Id == candidateId &&
                    item.MemberIds.Exists(personId =>
                        fixture.World.People.Exists(person =>
                            person.Id == personId &&
                            person.IsAlive &&
                            person.LocationId == village.LocationId &&
                            person.LocalDuty != LocalDutyKind.Levy)));
            }
            Assert.That(partiallyFedFamily, Is.Not.Null,
                "Selected household must exist.");
            var storage = fixture.World.VillageFacilities.Find(item =>
                item.VillageId == village.Id &&
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == partiallyFedFamily.Id);
            Assert.That(storage, Is.Not.Null,
                "Selected household granary must exist.");
            TransferCountyFoodToVillage(fixture.World, fixture.Content, 1);
            new FoodInventorySystem(fixture.Content)
                .TransferContainerToFamilyByNutrition(
                    fixture.World,
                    village.PublicGranaryInventoryContainerId,
                    partiallyFedFamily.Id,
                    storage.Id,
                    partiallyFedFamily.HeadPersonId,
                    1,
                    InventoryTransactionType.FoodVillageReliefTransferred,
                    village.Id);

            var monthlyRuntime = new WorldCommandRuntime();
            monthlyRuntime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(
                fixture.World, monthlyRuntime);
            monthlyRuntime.ProcessDue(fixture.World);
            var lowerSeverity = fixture.World.HouseholdReliefPickups.Find(item =>
                item.FamilyId == partiallyFedFamily.Id);
            Assert.That(lowerSeverity, Is.Not.Null,
                "Partially fed household must still produce a shortfall.");
            Assert.That(lowerSeverity.ShortfallSeverityBasisPoints,
                Is.LessThan(10_000));

            TransferCountyFoodToVillage(fixture.World, fixture.Content, 1);
            var result = new HouseholdReliefPickupSystem(fixture.Content)
                .Resolve(fixture.World, village.Id);
            var served = fixture.World.HouseholdReliefPickups.Find(item =>
                item.DeliveredPhysicalQuantity > 0);

            Assert.That(result.DeliveredPhysicalQuantity, Is.EqualTo(1));
            Assert.That(served, Is.Not.Null,
                "Scarce relief must reach one waiting household.");
            Assert.That(served.FamilyId,
                Is.Not.EqualTo(partiallyFedFamily.Id));
            Assert.That(served.ShortfallSeverityBasisPoints,
                Is.GreaterThan(lowerSeverity.ShortfallSeverityBasisPoints));
            fixture.World.Validate();
        }

        [Test]
        public void HouseholdReliefPriority_RecordsCountyAuthoritySnapshotAndRoundTrips()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1902);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            var village = fixture.World.Villages[0];
            var organization = fixture.World.Organizations.Find(item =>
                item.Id == village.HouseholdReliefAuthorityOrganizationId);
            var pickup = fixture.World.HouseholdReliefPickups[0];

            Assert.That(pickup.PriorityPolicyId,
                Is.EqualTo(HouseholdReliefPriorityPolicyIds
                    .NeedSeverityVulnerability));
            Assert.That(pickup.AuthorizationPolicyId,
                Is.EqualTo(HouseholdReliefAuthorizationPolicyIds
                    .CountyGovernmentLeader));
            Assert.That(pickup.AuthorizingOrganizationId,
                Is.EqualTo(organization.Id));
            Assert.That(pickup.AuthorizingPersonId,
                Is.EqualTo(organization.LeaderPersonId));
            Assert.That(pickup.AuthorizedDay, Is.EqualTo(30));
            Assert.That(pickup.ShortfallSeverityBasisPoints,
                Is.InRange(1, 10_000));
            Assert.That(pickup.AffectedPersonCountAtAuthorization,
                Is.GreaterThan(0));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content),
                fixture.Content);
            var loadedPickup = loaded.HouseholdReliefPickups.Find(item =>
                item.Id == pickup.Id);
            Assert.That(loadedPickup.AuthorizingOrganizationId,
                Is.EqualTo(pickup.AuthorizingOrganizationId));
            Assert.That(loadedPickup.AuthorizingPersonId,
                Is.EqualTo(pickup.AuthorizingPersonId));
            Assert.That(loadedPickup.ShortfallSeverityBasisPoints,
                Is.EqualTo(pickup.ShortfallSeverityBasisPoints));
            loaded.Validate();
        }

        [Test]
        public void HouseholdReliefPriority_EmergencyPolicyDoesNotInventAuthority()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1903);
            var village = fixture.World.Villages[0];
            village.HouseholdReliefAuthorizationPolicyId =
                HouseholdReliefAuthorizationPolicyIds.EmergencySystem;
            village.HouseholdReliefAuthorityOrganizationId = string.Empty;
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);

            Assert.That(fixture.World.HouseholdReliefPickups, Is.Not.Empty);
            Assert.That(fixture.World.HouseholdReliefPickups.TrueForAll(item =>
                item.AuthorizationPolicyId ==
                    HouseholdReliefAuthorizationPolicyIds.EmergencySystem &&
                string.IsNullOrEmpty(item.AuthorizingOrganizationId) &&
                string.IsNullOrEmpty(item.AuthorizingPersonId)), Is.True);
            fixture.World.Validate();
        }

        [Test]
        public void HouseholdReliefPriority_ValidationRejectsTamperedSeverity()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1904);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            var pickup = fixture.World.HouseholdReliefPickups[0];
            pickup.ShortfallSeverityBasisPoints =
                pickup.ShortfallSeverityBasisPoints == 10_000
                    ? 9_999
                    : pickup.ShortfallSeverityBasisPoints + 1;

            Assert.Throws<InvalidOperationException>(() =>
                fixture.World.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFortyWithoutInventingReliefPriorityAuthority()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1905);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            var json = WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content)
                .Replace(
                    "\"SchemaVersion\": " +
                        WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 40");

            var loaded = WorldSnapshotSerializer.Deserialize(
                json, fixture.Content);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.Villages.TrueForAll(item =>
                item.HouseholdReliefPriorityPolicyId ==
                    HouseholdReliefPriorityPolicyIds
                        .NeedSeverityVulnerability), Is.True);
            Assert.That(loaded.HouseholdReliefPickups, Is.Not.Empty);
            Assert.That(loaded.HouseholdReliefPickups.TrueForAll(item =>
                item.PriorityPolicyId ==
                    HouseholdReliefPriorityPolicyIds
                        .LegacySettlementFamilyOrder &&
                item.AuthorizationPolicyId ==
                    HouseholdReliefAuthorizationPolicyIds.LegacySystem &&
                string.IsNullOrEmpty(item.AuthorizingOrganizationId) &&
                string.IsNullOrEmpty(item.AuthorizingPersonId) &&
                item.AuthorizedDay == -1 &&
                item.ShortfallSeverityBasisPoints == -1 &&
                item.VulnerableAffectedPersonCount == -1 &&
                item.AffectedPersonCountAtAuthorization == -1), Is.True);
            loaded.Validate();
        }

        [Test]
        public void HouseholdReliefConsumption_ValidationRejectsOverRecovery()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1704);
            var monthlyRuntime = new WorldCommandRuntime();
            monthlyRuntime.RegisterHandler(
                fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(
                fixture.World, monthlyRuntime);
            monthlyRuntime.ProcessDue(fixture.World);
            var affected = fixture.World.HouseholdReliefConsumptions[0]
                .AffectedPeople[0];
            affected.RecoveredHealthBasisPoints =
                affected.AppliedHealthDamageBasisPoints + 1;

            Assert.Throws<InvalidOperationException>(() =>
                fixture.World.Validate());
        }

        [Test]
        public void HouseholdReliefConsumption_PartialFoodWaitsWithoutEmptyCommand()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_1705);
            SeedMonthlyShortfallAndVillageFood(fixture, 1);
            var pickup = new HouseholdReliefPickupSystem(fixture.Content);
            pickup.Resolve(fixture.World, fixture.World.Villages[0].Id);
            var claim = fixture.World.HouseholdReliefConsumptions.Find(item =>
                item.PickupId == fixture.World.HouseholdReliefPickups.Find(
                    request => request.DeliveredPhysicalQuantity > 0).Id);
            var consumption = new HouseholdReliefConsumptionSystem(
                fixture.Content);

            var result = consumption.Resolve(
                fixture.World, fixture.World.Villages[0].Id);
            var consumedPhysical = claim.ConsumedPhysicalQuantity;
            var healthRecovery = claim.AffectedPeople[0]
                .RecoveredHealthBasisPoints;
            var scheduler = new HouseholdReliefConsumptionCommandScheduler(
                consumption);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());

            Assert.That(result.ConsumedPhysicalQuantity, Is.EqualTo(1));
            Assert.That(claim.Status,
                Is.EqualTo(
                    HouseholdReliefConsumptionStatus.PartiallyConsumed));
            Assert.That(claim.RemainingNutritionBasisUnits,
                Is.GreaterThan(0));
            Assert.That(
                scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.Zero);
            Assert.That(claim.ConsumedPhysicalQuantity,
                Is.EqualTo(consumedPhysical));
            Assert.That(claim.AffectedPeople[0].RecoveredHealthBasisPoints,
                Is.EqualTo(healthRecovery));
            fixture.World.Validate();
        }

        [Test]
        public void HouseholdReliefCare_DependentRecipientUsesStableFamilyCaregiverAndRoundTrips()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_2001);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            var village = fixture.World.Villages[0];
            HouseholdReliefConsumptionState targetClaim = null;
            HouseholdReliefAffectedPersonState targetAffected = null;
            PersonState expectedCaregiver = null;
            var orderedPickups = new List<HouseholdReliefPickupState>(
                fixture.World.HouseholdReliefPickups);
            orderedPickups.Sort(CompareHouseholdReliefPickupPriority);
            for (var claimIndex = 0;
                 claimIndex < orderedPickups.Count &&
                 targetClaim == null;
                 claimIndex++)
            {
                var claim = fixture.World.HouseholdReliefConsumptions.Find(
                    item => item.PickupId == orderedPickups[claimIndex].Id);
                var family = fixture.World.Families.Find(item =>
                    item.Id == claim.FamilyId);
                var memberIds = new List<string>(family.MemberIds);
                memberIds.Sort(StringComparer.Ordinal);
                for (var affectedIndex = 0;
                     affectedIndex < claim.AffectedPeople.Count &&
                     targetClaim == null;
                     affectedIndex++)
                {
                    var affected = claim.AffectedPeople[affectedIndex];
                    if (!affected.RequiresCaregiverDelivery ||
                        affected.AllocatedNutritionBasisUnits <= 0)
                    {
                        continue;
                    }
                    for (var memberIndex = 0;
                         memberIndex < memberIds.Count;
                         memberIndex++)
                    {
                        var member = fixture.World.People.Find(item =>
                            item.Id == memberIds[memberIndex]);
                        var age = Math.Max(
                            0L,
                            (fixture.World.AbsoluteDay - member.BirthDay) /
                            360L);
                        if (member.Id != affected.PersonId &&
                            member.IsAlive &&
                            member.LocationId == village.LocationId &&
                            member.LocalDuty != LocalDutyKind.Levy &&
                            age >= 15L && age <= 60L)
                        {
                            targetClaim = claim;
                            targetAffected = affected;
                            expectedCaregiver = member;
                            break;
                        }
                    }
                }
            }
            Assert.That(targetClaim, Is.Not.Null,
                "The prototype must include a dependent with a caregiver.");

            long requiredNutrition = 0;
            for (var i = 0; i < orderedPickups.Count; i++)
            {
                requiredNutrition = checked(
                    requiredNutrition + orderedPickups[i]
                        .RequestedNutritionBasisUnits);
                if (orderedPickups[i].Id == targetClaim.PickupId)
                {
                    break;
                }
            }
            TransferCountyFoodToVillage(
                fixture.World,
                fixture.Content,
                (requiredNutrition + 9_799L) / 9_800L + 1);
            new HouseholdReliefPickupSystem(fixture.Content).Resolve(
                fixture.World, village.Id);
            var targetNutritionProfile = fixture.World.PersonNutritionProfiles
                .Find(item => item.PersonId == targetAffected.PersonId);
            var openingNutritionDebt =
                targetNutritionProfile.NutritionDebtBasisUnits;
            new HouseholdReliefConsumptionSystem(fixture.Content).Resolve(
                fixture.World, village.Id);

            Assert.That(targetAffected.ConsumedNutritionBasisUnits,
                Is.GreaterThan(0));
            var deliveries = fixture.World.HouseholdReliefCareDeliveries
                .FindAll(item =>
                    item.HouseholdReliefConsumptionId == targetClaim.Id &&
                    item.RecipientPersonId == targetAffected.PersonId);
            Assert.That(deliveries, Is.Not.Empty);
            Assert.That(deliveries.TrueForAll(item =>
                item.CaregiverPersonId == expectedCaregiver.Id), Is.True);
            var traced = deliveries.Find(item =>
                item.SourceKindId ==
                    HouseholdReliefCareDeliverySourceIds
                        .TracedFoodTransaction);
            Assert.That(traced, Is.Not.Null);
            var transaction = fixture.World.InventoryTransactions.Find(item =>
                item.Id == traced.SourceInventoryTransactionId);
            Assert.That(transaction.ActorPersonId,
                Is.EqualTo(expectedCaregiver.Id));
            Assert.That(transaction.HouseholdReliefRecipientPersonId,
                Is.EqualTo(targetAffected.PersonId));
            Assert.That(targetNutritionProfile.NutritionDebtBasisUnits,
                Is.EqualTo(openingNutritionDebt - Math.Min(
                    openingNutritionDebt,
                    targetAffected.ConsumedNutritionBasisUnits)));
            Assert.That(fixture.World.PersonNutritionLedgerEntries.Exists(item =>
                item.Kind == NutritionLedgerEntryKind.ReliefNutritionCredit &&
                item.PersonId == targetAffected.PersonId &&
                item.SourceHouseholdReliefConsumptionId == targetClaim.Id),
                Is.True);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content),
                fixture.Content);
            Assert.That(loaded.HouseholdReliefCareDeliveries.Find(item =>
                    item.Id == traced.Id).CaregiverPersonId,
                Is.EqualTo(expectedCaregiver.Id));
            loaded.Validate();
        }

        [Test]
        public void LongTermNutrition_TwoDeficitMonthsCreateDeterministicIllnessAndRoundTrip()
        {
            var first = BuildMinimalWorld();
            var second = BuildMinimalWorld();
            ResolveTwoNutritionDeficitMonths(first, "person.liu_bei");
            ResolveTwoNutritionDeficitMonths(second, "person.liu_bei");

            var profile = first.PersonNutritionProfiles[0];
            var episode = first.NutritionConditionEpisodes[0];
            Assert.That(profile.NutritionDebtBasisUnits, Is.EqualTo(60_000));
            Assert.That(profile.DiseaseRiskBasisPoints, Is.EqualTo(8_500));
            Assert.That(profile.ConsecutiveDeficitMonths, Is.EqualTo(2));
            Assert.That(profile.ActiveConditionEpisodeId,
                Is.EqualTo(episode.Id));
            Assert.That(episode.ConditionId,
                Is.EqualTo(NutritionConditionIds.MalnutritionIllness));
            Assert.That(episode.AppliedHealthDamageBasisPoints,
                Is.EqualTo(425));
            Assert.That(first.People.Find(item =>
                item.Id == profile.PersonId).HealthBasisPoints,
                Is.EqualTo(9_575));
            Assert.That(WorldSnapshotSerializer.Serialize(second),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(first)));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(first));
            Assert.That(loaded.PersonNutritionProfiles[0]
                .NutritionDebtBasisUnits, Is.EqualTo(60_000));
            Assert.That(loaded.NutritionConditionEpisodes[0]
                .AppliedHealthDamageBasisPoints, Is.EqualTo(425));
            loaded.Validate();
        }

        [Test]
        public void LongTermNutrition_AdequateMonthsRepayDebtAndResolveEpisode()
        {
            var world = BuildMinimalWorld();
            ResolveTwoNutritionDeficitMonths(world, "person.liu_bei");
            var system = new LongTermNutritionSystem();
            for (var day = 90L; day <= 180L; day += 30L)
            {
                world.AbsoluteDay = day;
                system.RecordMonthlySettlement(
                    world,
                    day,
                    new List<FormalHouseholdFoodPersonSettlementResult>
                    {
                        new FormalHouseholdFoodPersonSettlementResult
                        {
                            PersonId = "person.liu_bei",
                            RequiredNutritionBasisUnits = 30_000
                        }
                    });
            }

            var profile = world.PersonNutritionProfiles[0];
            var episode = world.NutritionConditionEpisodes[0];
            Assert.That(profile.NutritionDebtBasisUnits, Is.Zero);
            Assert.That(profile.DiseaseRiskBasisPoints, Is.Zero);
            Assert.That(profile.ConsecutiveAdequateMonths, Is.EqualTo(4));
            Assert.That(profile.ActiveConditionEpisodeId, Is.Empty);
            Assert.That(episode.EndDay, Is.EqualTo(180));
            Assert.That(episode.RecoveredHealthBasisPoints,
                Is.EqualTo(episode.AppliedHealthDamageBasisPoints));
            Assert.That(world.People.Find(item =>
                item.Id == profile.PersonId).HealthBasisPoints,
                Is.EqualTo(10_000));
            world.Validate();
        }

        [Test]
        public void LongTermNutrition_ValidationRejectsTamperedDebtClosure()
        {
            var world = BuildMinimalWorld();
            ResolveTwoNutritionDeficitMonths(world, "person.liu_bei");
            world.PersonNutritionLedgerEntries[1]
                .ClosingNutritionDebtBasisUnits++;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFortyTwoWithoutInventingNutritionHistory()
        {
            var world = BuildMinimalWorld();
            ResolveTwoNutritionDeficitMonths(world, "person.liu_bei");
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace(
                    "\"SchemaVersion\": " +
                        WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 42");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.PersonNutritionProfiles, Is.Empty);
            Assert.That(loaded.PersonNutritionLedgerEntries, Is.Empty);
            Assert.That(loaded.NutritionConditionEpisodes, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void CivilianMedical_DiagnosisPersistsWithoutMedicineAndDoesNotHeal()
        {
            var world = BuildCivilianMedicalWorld(false, false);
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            var episode = world.NutritionConditionEpisodes[0];
            var openingHealth = patient.HealthBasisPoints;
            var openingDebt = world.PersonNutritionProfiles[0]
                .NutritionDebtBasisUnits;
            var system = new CivilianMedicalSystem();

            var diagnosis = system.DiagnoseNutritionCondition(
                world, episode.Id, physician.Id, patient.Id);
            var treatment = system.TreatNutritionCondition(
                world, diagnosis.MedicalCaseId, physician.Id, patient.Id);

            Assert.That(diagnosis.Success, Is.True);
            Assert.That(treatment.Success, Is.False);
            Assert.That(world.CivilianMedicalCases.Count, Is.EqualTo(1));
            Assert.That(world.CivilianMedicalTreatments, Is.Empty);
            Assert.That(patient.HealthBasisPoints, Is.EqualTo(openingHealth));
            Assert.That(world.PersonNutritionProfiles[0]
                .NutritionDebtBasisUnits, Is.EqualTo(openingDebt));
            world.Validate();
        }

        [Test]
        public void CivilianMedical_TreatmentConsumesBatchWithoutRepayingDebtAndRoundTrips()
        {
            var world = BuildCivilianMedicalWorld(true, false);
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            var profile = world.PersonNutritionProfiles[0];
            var episode = world.NutritionConditionEpisodes[0];
            var medicine = world.ProductBatches.Find(item =>
                item.ProductDefinitionId ==
                    CoreProductionContent.HerbalMedicineMaterialProductId);
            var openingQuantity = medicine.Quantity;
            var openingHealth = patient.HealthBasisPoints;
            var openingDebt = profile.NutritionDebtBasisUnits;
            var openingRisk = profile.DiseaseRiskBasisPoints;
            var system = new CivilianMedicalSystem();
            var diagnosis = system.DiagnoseNutritionCondition(
                world, episode.Id, physician.Id, patient.Id);

            var treatment = system.TreatNutritionCondition(
                world, diagnosis.MedicalCaseId, physician.Id, patient.Id);

            Assert.That(treatment.Success, Is.True);
            Assert.That(medicine.Quantity,
                Is.EqualTo(openingQuantity - 1));
            Assert.That(patient.HealthBasisPoints,
                Is.EqualTo(openingHealth +
                    treatment.RecoveredHealthBasisPoints));
            Assert.That(profile.NutritionDebtBasisUnits,
                Is.EqualTo(openingDebt));
            Assert.That(profile.DiseaseRiskBasisPoints,
                Is.EqualTo(openingRisk));
            Assert.That(world.InventoryTransactions.Exists(item =>
                    item.Id == treatment.InventoryTransactionId &&
                    item.Type ==
                        InventoryTransactionType.MedicalTreatmentConsumed),
                Is.True);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.CivilianMedicalCases.Count, Is.EqualTo(1));
            Assert.That(loaded.CivilianMedicalTreatments.Count, Is.EqualTo(1));
            Assert.That(loaded.CivilianMedicalTreatments[0]
                .OpeningNutritionDebtBasisUnits,
                Is.EqualTo(loaded.CivilianMedicalTreatments[0]
                    .ClosingNutritionDebtBasisUnits));
            loaded.Validate();
        }

        [Test]
        public void CivilianMedical_MinorRequiresHouseholdAdultAuthorization()
        {
            var world = BuildCivilianMedicalWorld(false, true);
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            var episode = world.NutritionConditionEpisodes[0];
            var system = new CivilianMedicalSystem();

            var rejected = system.DiagnoseNutritionCondition(
                world, episode.Id, physician.Id, patient.Id);
            var accepted = system.DiagnoseNutritionCondition(
                world, episode.Id, physician.Id, physician.Id);

            Assert.That(rejected.Success, Is.False);
            Assert.That(accepted.Success, Is.True);
            Assert.That(world.CivilianMedicalCases[0].AuthorizationPolicyId,
                Is.EqualTo(CivilianMedicalAuthorizationPolicyIds
                    .HouseholdAdultCaregiver));
            Assert.That(world.CivilianMedicalCases[0].AuthorizingPersonId,
                Is.EqualTo(physician.Id));
            world.Validate();
        }

        [Test]
        public void CivilianMedical_ValidationRejectsTamperedTreatmentClosure()
        {
            var world = BuildCivilianMedicalWorld(true, false);
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            var system = new CivilianMedicalSystem();
            var diagnosis = system.DiagnoseNutritionCondition(
                world,
                world.NutritionConditionEpisodes[0].Id,
                physician.Id,
                patient.Id);
            system.TreatNutritionCondition(
                world, diagnosis.MedicalCaseId, physician.Id, patient.Id);
            world.CivilianMedicalTreatments[0]
                .ClosingNutritionDebtBasisUnits++;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFortyThreeWithoutInventingMedicalHistory()
        {
            var world = BuildMinimalWorld();
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace(
                    "\"SchemaVersion\": " +
                        WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 43");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.CivilianMedicalCases, Is.Empty);
            Assert.That(loaded.CivilianMedicalTreatments, Is.Empty);
            Assert.That(loaded.ProductionContentManifest.Packages[0].Version,
                Is.EqualTo("11.0.0"));
            loaded.Validate();
        }

        [Test]
        public void CivilianMedical_FormalServiceCreatesPrescriptionWorkAndSkillAudit()
        {
            var world = BuildCivilianMedicalWorld(true, false);
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            var skillBefore = physician.ProfessionalSkills.Medicine;
            var system = new CivilianMedicalSystem();
            var diagnosis = system.DiagnoseNutritionCondition(
                world,
                world.NutritionConditionEpisodes[0].Id,
                physician.Id,
                patient.Id);

            var treatment = system.TreatNutritionCondition(
                world, diagnosis.MedicalCaseId, physician.Id, patient.Id);

            Assert.That(treatment.Success, Is.True);
            Assert.That(world.CivilianMedicalPrescriptions.Count,
                Is.EqualTo(1));
            Assert.That(world.CivilianMedicalServices.Count, Is.EqualTo(1));
            var prescription = world.CivilianMedicalPrescriptions[0];
            var service = world.CivilianMedicalServices[0];
            Assert.That(prescription.Items.Count, Is.EqualTo(1));
            Assert.That(prescription.Items[0].ProductDefinitionId,
                Is.EqualTo(CoreProductionContent
                    .HerbalMedicineMaterialProductId));
            Assert.That(service.TreatmentId, Is.EqualTo(treatment.TreatmentId));
            Assert.That(service.WorkMinutes,
                Is.EqualTo(CivilianMedicalRules.TreatmentWorkMinutes));
            Assert.That(service.TotalFee, Is.EqualTo(0));
            Assert.That(service.PaymentPolicyId,
                Is.EqualTo(CivilianMedicalPaymentPolicyIds
                    .SameHouseholdCare));
            Assert.That(physician.ProfessionalSkills.Medicine,
                Is.GreaterThan(skillBefore));
            Assert.That(service.PhysicianMedicalSkillAfterBasisPoints,
                Is.EqualTo(physician.ProfessionalSkills.Medicine));
            Assert.That(world.CivilianMedicalCases[0].Status,
                Is.EqualTo(CivilianMedicalCaseStatus.Closed));
            Assert.That(world.CivilianMedicalCases[0].ClosureReasonId,
                Is.EqualTo(CivilianMedicalCaseClosureReasonIds
                    .InjuryRecovered));
            Assert.That(prescription.IsActive, Is.False);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.CivilianMedicalPrescriptions.Count,
                Is.EqualTo(1));
            Assert.That(loaded.CivilianMedicalServices.Count, Is.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void CivilianMedical_CrossHouseholdFeeClosesWithoutCreatingMoney()
        {
            var world = BuildCivilianMedicalWorld(true, false);
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            var payer = MoveMedicalPatientToSeparateHousehold(world, 1_000);
            var payee = world.Families.Find(item => item.Id == physician.FamilyId);
            var payerBefore = payer.Wealth;
            var payeeBefore = payee.Wealth;
            var system = new CivilianMedicalSystem();
            var diagnosis = system.DiagnoseNutritionCondition(
                world,
                world.NutritionConditionEpisodes[0].Id,
                physician.Id,
                patient.Id);

            var treatment = system.TreatNutritionCondition(
                world, diagnosis.MedicalCaseId, physician.Id, patient.Id);

            Assert.That(treatment.Success, Is.True);
            Assert.That(treatment.FeePaid,
                Is.EqualTo(CivilianMedicalRules.RecommendedTreatmentFee(7_500)));
            Assert.That(payer.Wealth, Is.EqualTo(payerBefore - treatment.FeePaid));
            Assert.That(payee.Wealth, Is.EqualTo(payeeBefore + treatment.FeePaid));
            Assert.That(payer.Wealth + payee.Wealth,
                Is.EqualTo(payerBefore + payeeBefore));
            Assert.That(world.CivilianMedicalServices[0].PaymentPolicyId,
                Is.EqualTo(CivilianMedicalPaymentPolicyIds.HouseholdDirect));
            world.Validate();
        }

        [Test]
        public void CivilianMedical_InsufficientFundsPreservesPrescriptionWithoutPartialTreatment()
        {
            var world = BuildCivilianMedicalWorld(true, false);
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            var payer = MoveMedicalPatientToSeparateHousehold(world, 1);
            var payee = world.Families.Find(item => item.Id == physician.FamilyId);
            var medicine = world.ProductBatches.Find(item =>
                item.ProductDefinitionId ==
                    CoreProductionContent.HerbalMedicineMaterialProductId);
            var healthBefore = patient.HealthBasisPoints;
            var skillBefore = physician.ProfessionalSkills.Medicine;
            var medicineBefore = medicine.Quantity;
            var payerBefore = payer.Wealth;
            var payeeBefore = payee.Wealth;
            var system = new CivilianMedicalSystem();
            var diagnosis = system.DiagnoseNutritionCondition(
                world,
                world.NutritionConditionEpisodes[0].Id,
                physician.Id,
                patient.Id);

            var treatment = system.TreatNutritionCondition(
                world, diagnosis.MedicalCaseId, physician.Id, patient.Id);

            Assert.That(treatment.Success, Is.False);
            Assert.That(world.CivilianMedicalPrescriptions.Count,
                Is.EqualTo(1));
            Assert.That(world.CivilianMedicalServices, Is.Empty);
            Assert.That(world.CivilianMedicalTreatments, Is.Empty);
            Assert.That(patient.HealthBasisPoints, Is.EqualTo(healthBefore));
            Assert.That(physician.ProfessionalSkills.Medicine,
                Is.EqualTo(skillBefore));
            Assert.That(medicine.Quantity, Is.EqualTo(medicineBefore));
            Assert.That(payer.Wealth, Is.EqualTo(payerBefore));
            Assert.That(payee.Wealth, Is.EqualTo(payeeBefore));
            world.Validate();
        }

        [Test]
        public void CivilianMedical_DailyWorkLimitDefersFifthPatientUntilNextDay()
        {
            var world = BuildCivilianMedicalWorld(true, false);
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            var patientIds = new List<string> { "person.liu_bei" };
            for (var i = 1; i <= 4; i++)
            {
                patientIds.Add(AddCivilianMedicalPatient(world, i));
            }
            var system = new CivilianMedicalSystem();
            CivilianMedicalTreatmentResult fifth = null;
            for (var i = 0; i < patientIds.Count; i++)
            {
                var patient = world.People.Find(item => item.Id == patientIds[i]);
                var episode = world.NutritionConditionEpisodes.Find(item =>
                    item.PersonId == patient.Id);
                var diagnosis = system.DiagnoseNutritionCondition(
                    world, episode.Id, physician.Id, patient.Id);
                var treatment = system.TreatNutritionCondition(
                    world, diagnosis.MedicalCaseId, physician.Id, patient.Id);
                if (i < 4)
                {
                    Assert.That(treatment.Success, Is.True, patient.Id);
                }
                else
                {
                    fifth = treatment;
                }
            }

            Assert.That(fifth, Is.Not.Null);
            Assert.That(fifth.Success, Is.False);
            Assert.That(world.CivilianMedicalServices.Count, Is.EqualTo(4));
            var workMinutes = 0;
            for (var i = 0; i < world.CivilianMedicalServices.Count; i++)
            {
                var service = world.CivilianMedicalServices[i];
                if (service.Day == world.AbsoluteDay &&
                    service.PhysicianPersonId == physician.Id)
                {
                    workMinutes += service.WorkMinutes;
                }
            }
            Assert.That(workMinutes,
                Is.EqualTo(CivilianMedicalRules
                    .MaximumDailyPhysicianWorkMinutes));

            world.AbsoluteDay++;
            var deferredCase = world.CivilianMedicalCases.Find(item =>
                item.PatientPersonId == patientIds[4]);
            var deferred = system.TreatNutritionCondition(
                world,
                deferredCase.Id,
                physician.Id,
                patientIds[4]);
            Assert.That(deferred.Success, Is.True);
            Assert.That(world.CivilianMedicalServices.Count, Is.EqualTo(5));
            world.Validate();
        }

        [Test]
        public void CivilianMedical_PatientDeathClosesCaseAndRejectsTreatment()
        {
            var world = BuildCivilianMedicalWorld(true, false);
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            var system = new CivilianMedicalSystem();
            var diagnosis = system.DiagnoseNutritionCondition(
                world,
                world.NutritionConditionEpisodes[0].Id,
                physician.Id,
                patient.Id);
            patient.IsAlive = false;

            var closed = system.ReconcileCasesForResidents(
                world,
                new HashSet<string>(StringComparer.Ordinal) { patient.Id });
            var treatment = system.TreatNutritionCondition(
                world, diagnosis.MedicalCaseId, physician.Id, patient.Id);

            Assert.That(closed, Is.EqualTo(1));
            Assert.That(treatment.Success, Is.False);
            Assert.That(world.CivilianMedicalCases[0].ClosureReasonId,
                Is.EqualTo(CivilianMedicalCaseClosureReasonIds.PatientDied));
            Assert.That(world.CivilianMedicalPrescriptions[0].IsActive,
                Is.False);
            Assert.That(world.CivilianMedicalServices, Is.Empty);
            world.Validate();
        }

        [Test]
        public void CivilianMedical_ValidationRejectsTamperedServiceFeeClosure()
        {
            var world = BuildCivilianMedicalWorld(true, false);
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            var system = new CivilianMedicalSystem();
            var diagnosis = system.DiagnoseNutritionCondition(
                world,
                world.NutritionConditionEpisodes[0].Id,
                physician.Id,
                patient.Id);
            system.TreatNutritionCondition(
                world, diagnosis.MedicalCaseId, physician.Id, patient.Id);
            world.CivilianMedicalServices[0].TotalFee++;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFortyFiveWithoutInventingMedicalServiceHistory()
        {
            var world = BuildMinimalWorld();
            world.AbsoluteDay = 80;
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace(
                    "\"SchemaVersion\": " +
                        WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 45");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.CivilianMedicalPrescriptions, Is.Empty);
            Assert.That(loaded.CivilianMedicalServices, Is.Empty);
            Assert.That(loaded.CivilianMedicalServiceContractActivationDay,
                Is.EqualTo(81));
            loaded.Validate();
        }

        [Test]
        public void HouseholdReliefCare_NoEligibleCaregiverPreservesDependentShare()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_2002);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            var village = fixture.World.Villages[0];
            var orderedPickups = new List<HouseholdReliefPickupState>(
                fixture.World.HouseholdReliefPickups);
            orderedPickups.Sort(CompareHouseholdReliefPickupPriority);
            HouseholdReliefConsumptionState claim = null;
            for (var i = 0; i < orderedPickups.Count && claim == null; i++)
            {
                var candidate = fixture.World.HouseholdReliefConsumptions.Find(
                    item => item.PickupId == orderedPickups[i].Id);
                if (candidate.AffectedPeople.Exists(affected =>
                        affected.RequiresCaregiverDelivery &&
                        affected.AllocatedNutritionBasisUnits > 0))
                {
                    claim = candidate;
                }
            }
            Assert.That(claim, Is.Not.Null, "dependent relief claim");
            var dependent = claim.AffectedPeople.Find(item =>
                item.RequiresCaregiverDelivery &&
                item.AllocatedNutritionBasisUnits > 0);
            var family = fixture.World.Families.Find(item =>
                item.Id == claim.FamilyId);
            var duties = new Dictionary<string, LocalDutyKind>();
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var member = fixture.World.People.Find(item =>
                    item.Id == family.MemberIds[i]);
                if (member.Id == dependent.PersonId)
                {
                    continue;
                }
                duties.Add(member.Id, member.LocalDuty);
                member.LocalDuty = LocalDutyKind.Levy;
            }

            long requiredNutrition = 0;
            for (var i = 0; i < orderedPickups.Count; i++)
            {
                requiredNutrition = checked(
                    requiredNutrition + orderedPickups[i]
                        .RequestedNutritionBasisUnits);
                if (orderedPickups[i].Id == claim.PickupId)
                {
                    break;
                }
            }
            TransferCountyFoodToVillage(
                fixture.World,
                fixture.Content,
                (requiredNutrition + 9_799L) / 9_800L + 1);
            new HouseholdReliefPickupSystem(fixture.Content).Resolve(
                fixture.World, village.Id);
            var health = fixture.World.People.Find(item =>
                item.Id == dependent.PersonId).HealthBasisPoints;
            new HouseholdReliefConsumptionSystem(fixture.Content).Resolve(
                fixture.World, village.Id);

            Assert.That(dependent.ConsumedNutritionBasisUnits, Is.Zero);
            Assert.That(claim.RemainingNutritionBasisUnits,
                Is.GreaterThanOrEqualTo(
                    dependent.AllocatedNutritionBasisUnits));
            Assert.That(fixture.World.HouseholdReliefCareDeliveries.Exists(
                item => item.RecipientPersonId == dependent.PersonId),
                Is.False);
            Assert.That(fixture.World.InventoryTransactions.Exists(item =>
                item.HouseholdReliefRecipientPersonId == dependent.PersonId),
                Is.False);
            Assert.That(fixture.World.People.Find(item =>
                item.Id == dependent.PersonId).HealthBasisPoints,
                Is.EqualTo(health));

            foreach (var pair in duties)
                fixture.World.People.Find(item => item.Id == pair.Key)
                    .LocalDuty = pair.Value;
            fixture.World.Validate();
        }

        [Test]
        public void HouseholdReliefCare_ValidationRejectsRecipientAsCaregiver()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_2003);
            SeedMonthlyShortfallAndVillageFood(fixture, 100);
            new HouseholdReliefPickupSystem(fixture.Content).Resolve(
                fixture.World, fixture.World.Villages[0].Id);
            new HouseholdReliefConsumptionSystem(fixture.Content).Resolve(
                fixture.World, fixture.World.Villages[0].Id);
            var delivery = fixture.World.HouseholdReliefCareDeliveries[0];
            delivery.CaregiverPersonId = delivery.RecipientPersonId;

            Assert.Throws<InvalidOperationException>(() =>
                fixture.World.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionFortyOneWithoutInventingCareDelivery()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(25_2004);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            var json = WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content)
                .Replace(
                    "\"SchemaVersion\": " +
                        WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 41")
                .Replace(
                    "\"HouseholdReliefCareDeliveries\": []",
                    "\"HouseholdReliefCareDeliveries\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(
                json, fixture.Content);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.HouseholdReliefCareDeliveries, Is.Empty);
            Assert.That(loaded.HouseholdReliefConsumptions.TrueForAll(claim =>
                claim.CareDeliveryPolicyId ==
                    HouseholdReliefCareDeliveryPolicyIds.LegacySelfService &&
                claim.AffectedPeople.TrueForAll(affected =>
                    !affected.RequiresCaregiverDelivery)), Is.True);
            Assert.That(loaded.InventoryTransactions.TrueForAll(item =>
                string.IsNullOrEmpty(
                    item.HouseholdReliefRecipientPersonId)), Is.True);

            var orderedPickups = new List<HouseholdReliefPickupState>(
                loaded.HouseholdReliefPickups);
            orderedPickups.Sort(CompareHouseholdReliefPickupPriority);
            TransferCountyFoodToVillage(
                loaded,
                fixture.Content,
                (orderedPickups[0].RequestedNutritionBasisUnits + 9_799L) /
                    9_800L + 1);
            new HouseholdReliefPickupSystem(fixture.Content).Resolve(
                loaded, loaded.Villages[0].Id);
            var served = loaded.HouseholdReliefPickups.Find(item =>
                item.DeliveredPhysicalQuantity > 0);
            new HouseholdReliefConsumptionSystem(fixture.Content).Resolve(
                loaded, loaded.Villages[0].Id);
            var continued = loaded.HouseholdReliefConsumptions.Find(item =>
                item.PickupId == served.Id);
            Assert.That(continued.ConsumedPhysicalQuantity,
                Is.GreaterThan(0));
            Assert.That(continued.InventoryTransactionIds.TrueForAll(id =>
                string.IsNullOrEmpty(loaded.InventoryTransactions.Find(
                    item => item.Id == id)
                    .HouseholdReliefRecipientPersonId)), Is.True);
            loaded.Validate();
        }

        [Test]
        public void FormalPublicFoodCommand_PersistedWorkTaxesRemitsRelievesOnce()
        {
            var fixture = PrepareFormalPublicFoodCommandWorld(25_1101);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            Assert.That(
                fixture.Scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.EqualTo(1));
            Assert.That(
                fixture.Scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.Zero);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World,
                    fixture.Content),
                fixture.Content);
            var villageLife = new VillageLifeSystem(
                loaded.MasterSeed,
                fixture.Content);
            var county = new CountyGovernanceSystem(fixture.Content);
            var scheduler = new FormalPublicFoodMonthlyCommandScheduler(
                county,
                villageLife);
            var resumed = new WorldCommandRuntime();
            resumed.RegisterHandler(scheduler.CreateCommandHandler());
            var recorder = new FormalPublicFoodEventRecorder();
            resumed.RegisterEventHandler(recorder);

            var report = resumed.ProcessDue(loaded);
            resumed.DispatchPublishedEvents(loaded);
            var repeated = resumed.ProcessDue(loaded);

            Assert.That(report.ProcessedCommands, Is.EqualTo(1));
            Assert.That(report.CommittedTransactions, Is.EqualTo(1));
            Assert.That(report.PublishedEvents, Is.EqualTo(1));
            Assert.That(repeated.ProcessedCommands, Is.Zero);
            Assert.That(loaded.InventoryTransactions.Exists(item =>
                item.Day == 300 && item.Type ==
                    InventoryTransactionType.FoodTaxTransferred), Is.True);
            Assert.That(loaded.InventoryTransactions.Exists(item =>
                item.Day == 300 && item.Type ==
                    InventoryTransactionType.FoodTaxRemitted), Is.True);
            Assert.That(loaded.InventoryTransactions.Exists(item =>
                item.Day == 300 && item.Type ==
                    InventoryTransactionType.FoodCountyReliefTransferred),
                Is.True);
            Assert.That(loaded.CountyGovernances[0].TotalGrainTaxReceived,
                Is.GreaterThan(0));
            Assert.That(loaded.CountyGovernances[0].TotalReliefGrain,
                Is.GreaterThan(0));
            Assert.That(loaded.PersistentWorldCommands[0].Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));
            Assert.That(recorder.HandledEventIds, Is.EqualTo(new[]
            {
                FormalPublicFoodMonthlyCommandScheduler.MonthlyEventId(
                    300,
                    loaded.CountyGovernances[0].Id)
            }));
            loaded.Validate();
        }

        [Test]
        public void FormalPublicFoodCommand_DateDriftRejectsWithoutBusinessMutation()
        {
            var fixture = PrepareFormalPublicFoodCommandWorld(25_1102);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            var inventoryTransactions =
                fixture.World.InventoryTransactions.Count;
            var villageLedger = fixture.World.VillageLedgerEntries.Count;
            var countyLedger = fixture.World.CountyFiscalLedgerEntries.Count;
            var totalFood = TotalProductQuantity(fixture.World);
            var totalTax = fixture.World.Villages[0].TaxGrainCollected;
            fixture.World.AbsoluteDay++;

            Assert.Throws<InvalidOperationException>(() =>
                runtime.ProcessDue(fixture.World));

            Assert.That(fixture.World.InventoryTransactions.Count,
                Is.EqualTo(inventoryTransactions));
            Assert.That(fixture.World.VillageLedgerEntries.Count,
                Is.EqualTo(villageLedger));
            Assert.That(fixture.World.CountyFiscalLedgerEntries.Count,
                Is.EqualTo(countyLedger));
            Assert.That(TotalProductQuantity(fixture.World),
                Is.EqualTo(totalFood));
            Assert.That(fixture.World.Villages[0].TaxGrainCollected,
                Is.EqualTo(totalTax));
            Assert.That(fixture.World.WorldEventOutbox, Is.Empty);
            Assert.That(fixture.World.WorldCommandBatchResults[0].Outcome,
                Is.EqualTo(WorldCommandBatchOutcome.Rejected));
            fixture.World.Validate();
        }

        [Test]
        public void FormalPublicFoodCommand_NoDueWorkDoesNotCreateEmptyCommand()
        {
            var fixture = PrepareFormalPublicFoodCommandWorld(25_1103);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.World.AbsoluteDay = 299;

            Assert.That(
                fixture.Scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.Zero);
            Assert.That(fixture.World.PersistentWorldCommands, Is.Empty);

            fixture.World.AbsoluteDay = 300;
            fixture.World.CountyGovernances[0].LastSettlementDay = 300;
            Assert.That(
                fixture.Scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.Zero);
            Assert.That(fixture.World.PersistentWorldCommands, Is.Empty);
        }

        [Test]
        public void FormalPublicFoodCommand_WorldSimulatorUsesPersistentPipelineWithoutDoubleTax()
        {
            var fixture = PrepareFormalPublicFoodCommandWorld(25_1104);
            fixture.World.AbsoluteDay = 299;
            fixture.World.Villages[0].LastSettlementDay = 270;
            fixture.World.Villages[0].NextSettlementDay = 300;
            var simulator = new WorldSimulator(
                fixture.World.MasterSeed,
                fixture.Content);

            simulator.AdvanceDays(fixture.World, 1);

            var command = fixture.World.PersistentWorldCommands.Find(item =>
                item.CommandTypeId ==
                    FormalPublicFoodMonthlyCommandScheduler.CommandTypeId);
            Assert.That(command, Is.Not.Null);
            Assert.That(command.Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));
            Assert.That(fixture.World.CountyGovernances[0].LastSettlementDay,
                Is.EqualTo(300));
            Assert.That(fixture.World.WorldCommandBatchResults.Exists(item =>
                item.CommandIds.Contains(command.Id) &&
                item.Transactions.Exists(transaction =>
                    transaction.TransactionKindId ==
                        FormalPublicFoodMonthlyCommandScheduler
                            .TransactionKindId)), Is.True);
            Assert.That(fixture.World.WorldEventOutbox.Exists(item =>
                item.EventTypeId ==
                    FormalPublicFoodMonthlyCommandScheduler.EventTypeId &&
                item.DispatchStatus ==
                    WorldEventDispatchStatus.Dispatched), Is.True);
            var assessedFamilies = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0;
                 i < fixture.World.VillageLedgerEntries.Count;
                 i++)
            {
                var entry = fixture.World.VillageLedgerEntries[i];
                if (entry.Day == 300 && entry.Type ==
                    VillageLedgerEntryType.TaxAssessment)
                {
                    Assert.That(assessedFamilies.Add(entry.FamilyId), Is.True,
                        $"Family {entry.FamilyId} was taxed twice.");
                }
            }
            Assert.That(assessedFamilies.Count, Is.GreaterThan(0));
            fixture.World.Validate();
        }

        [Test]
        public void FormalPublicFoodCommand_IdenticalWorldsRemainDeterministic()
        {
            var left = PrepareFormalPublicFoodCommandWorld(25_1105);
            var right = PrepareFormalPublicFoodCommandWorld(25_1105);
            left.World.AbsoluteDay = 299;
            right.World.AbsoluteDay = 299;
            left.World.Villages[0].LastSettlementDay = 270;
            right.World.Villages[0].LastSettlementDay = 270;
            left.World.Villages[0].NextSettlementDay = 300;
            right.World.Villages[0].NextSettlementDay = 300;

            new WorldSimulator(left.World.MasterSeed, left.Content)
                .AdvanceDays(left.World, 1);
            new WorldSimulator(right.World.MasterSeed, right.Content)
                .AdvanceDays(right.World, 1);

            Assert.That(
                WorldSnapshotSerializer.Serialize(right.World, right.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    left.World,
                    left.Content)));
        }

        [Test]
        public void PublicReliefProcurement_PersistedCommandBuysReservedFoodOnce()
        {
            var fixture = PreparePublicReliefProcurementWorld(
                25_1201, true);
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content),
                fixture.Content);
            loaded.AbsoluteDay = 31;
            loaded.Segment = (byte)DaySegment.Dawn;
            var scheduler = new PublicReliefProcurementCommandScheduler(
                new PublicReliefProcurementSystem(fixture.Content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            runtime.RegisterEventHandler(
                scheduler.CreateProjectionHandler());
            var treasuryBefore = loaded.Organizations.Find(item =>
                item.Id == loaded.CountyGovernances[0]
                    .GovernmentOrganizationId).Treasury;
            var sellerBefore = loaded.Families.Find(item =>
                item.Id == fixture.SellerFamilyId).Wealth;

            var report = runtime.ProcessDue(loaded);
            runtime.DispatchPublishedEvents(loaded);
            var repeated = runtime.ProcessDue(loaded);

            Assert.That(report.ProcessedCommands, Is.EqualTo(1));
            Assert.That(report.CommittedTransactions, Is.EqualTo(1));
            Assert.That(report.PublishedEvents, Is.EqualTo(2));
            Assert.That(repeated.ProcessedCommands, Is.Zero);
            Assert.That(loaded.PublicReliefProcurementTrades.Count,
                Is.EqualTo(1));
            var trade = loaded.PublicReliefProcurementTrades[0];
            Assert.That(trade.Quantity, Is.GreaterThan(0));
            Assert.That(loaded.Organizations.Find(item =>
                    item.Id == trade.BuyerOrganizationId).Treasury,
                Is.EqualTo(treasuryBefore - trade.MoneyTransferred));
            Assert.That(loaded.Families.Find(item =>
                    item.Id == trade.SellerFamilyId).Wealth,
                Is.EqualTo(sellerBefore + trade.MoneyTransferred));
            Assert.That(loaded.InventoryTransactions.Exists(item =>
                item.Id == trade.InventoryTransactionId &&
                item.Type == InventoryTransactionType
                    .FoodPublicReliefProcurementTransferred), Is.True);
            Assert.That(loaded.WorldEventOutbox.Exists(item =>
                item.Id == PublicReliefProcurementCommandScheduler.EventId(
                    31, trade.CountyGovernanceId) &&
                item.DispatchStatus ==
                    WorldEventDispatchStatus.Dispatched), Is.True);
            loaded.Validate();
            var roundTripJson = WorldSnapshotSerializer.Serialize(
                loaded, fixture.Content);
            var roundTrip = WorldSnapshotSerializer.Deserialize(
                roundTripJson,
                fixture.Content);
            Assert.That(roundTrip.PublicReliefProcurementTrades.Count,
                Is.EqualTo(1));
            roundTrip.Validate();
        }

        [Test]
        public void PublicReliefProcurement_NoSellerAuditsUnfilledWithoutFabrication()
        {
            var fixture = PreparePublicReliefProcurementWorld(
                25_1202, false);
            fixture.World.AbsoluteDay = 31;
            fixture.World.Segment = (byte)DaySegment.Dawn;
            var scheduler = new PublicReliefProcurementCommandScheduler(
                new PublicReliefProcurementSystem(fixture.Content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            var stockBefore = TotalProductQuantity(fixture.World);
            var treasuryBefore = fixture.World.Organizations.Find(item =>
                item.Id == fixture.World.CountyGovernances[0]
                    .GovernmentOrganizationId).Treasury;

            runtime.ProcessDue(fixture.World);

            Assert.That(fixture.World.PublicReliefProcurementTrades,
                Is.Empty);
            Assert.That(TotalProductQuantity(fixture.World),
                Is.EqualTo(stockBefore));
            Assert.That(fixture.World.Organizations.Find(item =>
                    item.Id == fixture.World.CountyGovernances[0]
                        .GovernmentOrganizationId).Treasury,
                Is.EqualTo(treasuryBefore));
            Assert.That(fixture.World.CountyFiscalLedgerEntries.Exists(item =>
                item.Day == 31 && item.Type ==
                    CountyFiscalEntryType.GrainProcurementUnfilled &&
                item.Amount > 0), Is.True);
            fixture.World.Validate();
        }

        [Test]
        public void PublicReliefProcurement_AuthorityLossRejectsBusinessMutation()
        {
            var fixture = PreparePublicReliefProcurementWorld(
                25_1203, true);
            fixture.World.AbsoluteDay = 31;
            fixture.World.Segment = (byte)DaySegment.Dawn;
            var governance = fixture.World.CountyGovernances[0];
            var government = fixture.World.Organizations.Find(item =>
                item.Id == governance.GovernmentOrganizationId);
            new PopulationLedgerSystem().RecordDeath(
                fixture.World,
                fixture.World.People.Find(item =>
                    item.Id == government.LeaderPersonId));
            var scheduler = new PublicReliefProcurementCommandScheduler(
                new PublicReliefProcurementSystem(fixture.Content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            var stockBefore = TotalProductQuantity(fixture.World);
            var treasuryBefore = government.Treasury;
            var tradesBefore =
                fixture.World.PublicReliefProcurementTrades.Count;

            Assert.Throws<InvalidOperationException>(() =>
                runtime.ProcessDue(fixture.World));

            Assert.That(TotalProductQuantity(fixture.World),
                Is.EqualTo(stockBefore));
            Assert.That(government.Treasury, Is.EqualTo(treasuryBefore));
            Assert.That(fixture.World.PublicReliefProcurementTrades.Count,
                Is.EqualTo(tradesBefore));
            var procurementCommand = fixture.World.PersistentWorldCommands
                .Find(item => item.CommandTypeId ==
                    PublicReliefProcurementCommandScheduler.CommandTypeId);
            Assert.That(fixture.World.WorldCommandBatchResults.Exists(item =>
                item.Outcome == WorldCommandBatchOutcome.Rejected &&
                item.CommandIds.Contains(procurementCommand.Id)), Is.True);
            fixture.World.Validate();
        }

        [Test]
        public void PublicReliefProcurement_IdenticalWorldsRemainDeterministic()
        {
            var left = PreparePublicReliefProcurementWorld(25_1204, true);
            var right = PreparePublicReliefProcurementWorld(25_1204, true);
            left.World.AbsoluteDay = 31;
            right.World.AbsoluteDay = 31;
            left.World.Segment = (byte)DaySegment.Dawn;
            right.World.Segment = (byte)DaySegment.Dawn;

            var leftSimulator = new WorldSimulator(
                left.World.MasterSeed, left.Content);
            var rightSimulator = new WorldSimulator(
                right.World.MasterSeed, right.Content);
            leftSimulator.CommandRuntime.ProcessDue(left.World);
            rightSimulator.CommandRuntime.ProcessDue(right.World);

            Assert.That(
                WorldSnapshotSerializer.Serialize(right.World, right.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    left.World, left.Content)));
        }

        [Test]
        public void PublicReliefProcurement_ValidationRejectsTamperedTradeMoney()
        {
            var fixture = PreparePublicReliefProcurementWorld(
                25_1205, true);
            fixture.World.AbsoluteDay = 31;
            fixture.World.Segment = (byte)DaySegment.Dawn;
            new WorldSimulator(fixture.World.MasterSeed, fixture.Content)
                .CommandRuntime.ProcessDue(fixture.World);
            fixture.World.PublicReliefProcurementTrades[0]
                .MoneyTransferred++;

            Assert.Throws<InvalidOperationException>(() =>
                fixture.World.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionThirtyThreeToEmptyPublicReliefProcurement()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_1206);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var json = WorldSnapshotSerializer.Serialize(world, content)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 33");

            var loaded = WorldSnapshotSerializer.Deserialize(
                json,
                content);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.PublicReliefProcurementTrades, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void PublicReliefExternalProcurement_KnownRouteDispatchesAndDeliversGovernmentCargo()
        {
            var fixture = PrepareCivilianFreightWorld(25_1301, 20);
            var destination = fixture.World.CountyGovernances.Find(item =>
                item.Id == "county_governance.freight_destination");
            var government = fixture.World.Organizations.Find(item =>
                item.Id == destination.GovernmentOrganizationId);
            government.Treasury = 100_000;
            fixture.World.Routes.Add(new RouteState
            {
                Id = "route.freight_destination_village_county",
                FromLocationId = "location.freight_destination_village",
                ToLocationId = destination.CountyLocationId,
                DistanceKilometers = 10,
                Bidirectional = true,
                SecurityBasisPoints = 9_000
            });
            fixture.FreightSystem.RegisterCarrier(
                fixture.World,
                new CivilianCarrierRegistrationRequest
                {
                    CarrierPersonId = fixture.Carrier.Id,
                    TransportInventoryContainerId = fixture.Transport.Id,
                    BaseFee = 10,
                    FeePerKilometer = 1,
                    FeePerHundredUnits = 1,
                    MaximumDistanceKilometers = 100,
                    KnownRouteIds = new List<string>
                    {
                        "route.freight_origin_destination",
                        "route.freight_destination_village_county"
                    }
                });
            SeedFormalReliefShortfall(
                fixture.World, destination.Id, 10);
            var runtime = CreatePublicReliefProcurementRuntime(fixture);

            runtime.DispatchPublishedEvents(fixture.World);
            fixture.World.AbsoluteDay = 1;
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            fixture.World.AbsoluteDay = 2;
            var treasuryBefore = government.Treasury;
            runtime.ProcessDue(fixture.World);

            var trade = fixture.World.PublicReliefProcurementTrades.Find(item =>
                !string.IsNullOrEmpty(item.CivilianFreightId));
            Assert.That(trade, Is.Not.Null);
            var freight = fixture.World.CivilianFreights.Find(item =>
                item.Id == trade.CivilianFreightId);
            Assert.That(freight.BuyerFamilyId, Is.Empty);
            Assert.That(freight.BuyerOrganizationId, Is.EqualTo(government.Id));
            Assert.That(freight.PlannedRouteIds.Count, Is.EqualTo(2));
            Assert.That(government.Treasury, Is.EqualTo(
                treasuryBefore - trade.MoneyTransferred - trade.FreightFee));
            Assert.That(fixture.World.ProductBatches.Exists(item =>
                item.SourceTransactionId ==
                    freight.DispatchInventoryTransactionId &&
                item.OwnerOrganizationId == government.Id &&
                item.InventoryContainerId == fixture.Transport.Id), Is.True);

            var travel = new TravelSystem();
            for (var i = 0;
                 i < 20 && freight.Status != CivilianFreightStatus.Completed;
                 i++)
            {
                travel.AdvanceJourneysOneSegment(fixture.World);
                fixture.FreightSystem.ResolveArrivals(fixture.World);
            }
            Assert.That(freight.Status, Is.EqualTo(
                CivilianFreightStatus.Completed));
            Assert.That(fixture.World.ProductBatches.Exists(item =>
                item.OwnerOrganizationId == government.Id &&
                item.InventoryContainerId ==
                    destination.GranaryInventoryContainerId &&
                item.Quantity > 0), Is.True);
            fixture.World.Validate();
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content),
                fixture.Content);
            loaded.Validate();
            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
        }

        [Test]
        public void PublicReliefExternalProcurement_UnknownRouteAuditsWithoutDispatch()
        {
            var fixture = PrepareCivilianFreightWorld(25_1302, 20);
            var destination = fixture.World.CountyGovernances.Find(item =>
                item.Id == "county_governance.freight_destination");
            fixture.World.Organizations.Find(item =>
                item.Id == destination.GovernmentOrganizationId).Treasury =
                100_000;
            fixture.FreightSystem.RegisterCarrier(
                fixture.World,
                new CivilianCarrierRegistrationRequest
                {
                    CarrierPersonId = fixture.Carrier.Id,
                    TransportInventoryContainerId = fixture.Transport.Id,
                    BaseFee = 10,
                    FeePerKilometer = 1,
                    FeePerHundredUnits = 1,
                    MaximumDistanceKilometers = 100,
                    KnownRouteIds = new List<string>
                    {
                        "route.freight_origin_destination"
                    }
                });
            SeedFormalReliefShortfall(fixture.World, destination.Id, 10);
            var runtime = CreatePublicReliefProcurementRuntime(fixture);

            runtime.DispatchPublishedEvents(fixture.World);
            fixture.World.AbsoluteDay = 1;
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            fixture.World.AbsoluteDay = 2;
            var quantityBefore = TotalProductQuantity(fixture.World);
            runtime.ProcessDue(fixture.World);

            Assert.That(fixture.World.CivilianFreights, Is.Empty);
            Assert.That(TotalProductQuantity(fixture.World),
                Is.EqualTo(quantityBefore));
            Assert.That(fixture.World.CountyFiscalLedgerEntries.Exists(item =>
                item.Day == 2 && item.Type ==
                    CountyFiscalEntryType
                        .GrainExternalProcurementUnfilled &&
                item.Amount == 10), Is.True);
            fixture.World.Validate();
        }

        [Test]
        public void PublicReliefExternalProcurement_IdenticalWorldsRemainDeterministic()
        {
            var left = PrepareExternalReliefProcurementWorld(25_1304, true);
            var right = PrepareExternalReliefProcurementWorld(25_1304, true);

            RunExternalReliefProcurementToDispatch(left);
            RunExternalReliefProcurementToDispatch(right);

            Assert.That(
                WorldSnapshotSerializer.Serialize(right.World, right.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    left.World, left.Content)));
        }

        [Test]
        public void Snapshot_MigratesVersionThirtyFourPublicReliefAndFreightBuyerMode()
        {
            var fixture = PrepareCivilianFreightWorld(25_1303, 10);
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);
            var json = WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 34")
                .Replace("\"BuyerOrganizationId\": \"\"", "\"BuyerOrganizationId\": null")
                .Replace("\"DestinationInventoryContainerId\": \"\"", "\"DestinationInventoryContainerId\": null")
                .Replace("\"PublicReliefProcurementTradeId\": \"\"", "\"PublicReliefProcurementTradeId\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(
                json, fixture.Content);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            var migrated = loaded.CivilianFreights.Find(item =>
                item.Id == freight.Id);
            Assert.That(migrated.BuyerOrganizationId, Is.Empty);
            Assert.That(migrated.PublicReliefProcurementTradeId, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void PublicReliefArrivalRecovery_ActualArrivalDistributesOnceAndRoundTrips()
        {
            var fixture = PrepareExternalReliefProcurementWorld(
                25_1401, true);
            RunExternalReliefProcurementToDispatch(fixture);
            var freight = fixture.World.CivilianFreights[0];
            CompleteCivilianFreight(fixture, freight);
            var recoverySystem = new PublicReliefArrivalRecoverySystem(
                fixture.World.MasterSeed, fixture.Content);
            var scheduler =
                new PublicReliefArrivalRecoveryCommandScheduler(
                    recoverySystem);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            runtime.RegisterEventHandler(
                scheduler.CreateProjectionHandler());

            Assert.That(
                scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.EqualTo(1));
            var report = runtime.ProcessDue(fixture.World);
            var repeated = scheduler.EnsureDueCommands(
                fixture.World, runtime);

            Assert.That(report.CommittedTransactions, Is.EqualTo(1));
            Assert.That(repeated, Is.EqualTo(0));
            Assert.That(fixture.World.PublicReliefRecoveries.Count,
                Is.EqualTo(1));
            var recovery = fixture.World.PublicReliefRecoveries[0];
            Assert.That(recovery.Status,
                Is.EqualTo(PublicReliefRecoveryStatus.Fulfilled));
            Assert.That(recovery.TotalRecoveredQuantity, Is.EqualTo(10));
            Assert.That(recovery.RemainingQuantity, Is.Zero);
            Assert.That(recovery.FreightReports.Count, Is.EqualTo(1));
            Assert.That(
                recovery.FreightReports[0].DeliveredQuantity,
                Is.EqualTo(freight.DeliveredQuantity));
            Assert.That(fixture.World.ProductBatches.Exists(item =>
                item.InventoryContainerId ==
                    "inventory.freight_destination_public_granary" &&
                item.Quantity > 0), Is.True);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content),
                fixture.Content);
            Assert.That(loaded.PublicReliefRecoveries.Count,
                Is.EqualTo(1));
            Assert.That(
                loaded.PublicReliefRecoveries[0].TotalRecoveredQuantity,
                Is.EqualTo(10));
            loaded.Validate();
        }

        [Test]
        public void PublicReliefArrivalRecovery_NaturalLossCreatesOneBudgetedSupplement()
        {
            var fixture = PrepareExternalReliefProcurementWorld(
                25_1402, true);
            AddBackupReliefCarrier(fixture);
            RunExternalReliefProcurementToDispatch(fixture);
            var initial = fixture.World.CivilianFreights[0];
            fixture.Content.GetProduct(initial.ProductDefinitionId)
                .PerishabilityBasisPoints = 10_000;
            initial.ProductPerishabilityBasisPoints = 10_000;
            initial.FoodSpoilageSensitivityBasisPoints = 10_000;
            fixture.World.AbsoluteDay = 3;
            fixture.FreightSystem.ResolveDailyTransit(fixture.World);
            CompleteCivilianFreight(fixture, initial);
            Assert.That(initial.NaturalLossQuantity, Is.GreaterThan(0));
            Assert.That(initial.DeliveredQuantity,
                Is.LessThan(initial.DispatchedQuantity));

            var recoverySystem = new PublicReliefArrivalRecoverySystem(
                fixture.World.MasterSeed, fixture.Content);
            var scheduler =
                new PublicReliefArrivalRecoveryCommandScheduler(
                    recoverySystem);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            runtime.RegisterEventHandler(
                scheduler.CreateProjectionHandler());
            scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);

            var recovery = fixture.World.PublicReliefRecoveries[0];
            Assert.That(recovery.SupplementalAttemptCount, Is.EqualTo(1));
            Assert.That(recovery.SupplementalFreightId, Is.Not.Empty);
            Assert.That(recovery.Status,
                Is.EqualTo(
                    PublicReliefRecoveryStatus.SupplementalInTransit));
            Assert.That(fixture.World.CivilianFreights.Count,
                Is.EqualTo(2));
            var supplemental = fixture.World.CivilianFreights.Find(item =>
                item.Id == recovery.SupplementalFreightId);
            Assert.That(supplemental.IsSupplementalPublicReliefFreight,
                Is.True);
            Assert.That(supplemental.DispatchedQuantity,
                Is.EqualTo(recovery.SupplementalRequestedQuantity));

            CompleteCivilianFreight(fixture, supplemental);
            scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            Assert.That(recovery.Status,
                Is.EqualTo(PublicReliefRecoveryStatus.Fulfilled));
            Assert.That(recovery.FreightReports.Count, Is.EqualTo(2));
            Assert.That(recovery.FreightReports[0].ExceptionCode,
                Does.Contain("natural_loss"));
            Assert.That(recovery.SupplementalAttemptCount, Is.EqualTo(1));
            Assert.That(
                scheduler.EnsureDueCommands(fixture.World, runtime),
                Is.EqualTo(0));
            fixture.World.Validate();
        }

        [Test]
        public void PublicReliefArrivalRecovery_NoCarrierExhaustsWithoutFabrication()
        {
            var fixture = PrepareExternalReliefProcurementWorld(
                25_1407, true);
            RunExternalReliefProcurementToDispatch(fixture);
            var initial = fixture.World.CivilianFreights[0];
            fixture.Content.GetProduct(initial.ProductDefinitionId)
                .PerishabilityBasisPoints = 10_000;
            initial.ProductPerishabilityBasisPoints = 10_000;
            initial.FoodSpoilageSensitivityBasisPoints = 10_000;
            fixture.World.AbsoluteDay = 3;
            fixture.FreightSystem.ResolveDailyTransit(fixture.World);
            CompleteCivilianFreight(fixture, initial);

            ResolvePublicReliefArrivalRecovery(fixture);

            var recovery = fixture.World.PublicReliefRecoveries[0];
            Assert.That(recovery.Status,
                Is.EqualTo(PublicReliefRecoveryStatus.Exhausted));
            Assert.That(recovery.SupplementalAttemptCount, Is.EqualTo(1));
            Assert.That(recovery.SupplementalFreightId, Is.Empty);
            Assert.That(fixture.World.CivilianFreights.Count, Is.EqualTo(1));
            Assert.That(recovery.RemainingQuantity,
                Is.EqualTo(initial.NaturalLossQuantity));
            Assert.That(fixture.World.CountyFiscalLedgerEntries.Exists(item =>
                item.Day == fixture.World.AbsoluteDay &&
                item.Type ==
                    CountyFiscalEntryType
                        .GrainExternalProcurementUnfilled &&
                item.Amount == recovery.RemainingQuantity), Is.True);
            fixture.World.Validate();
        }

        [Test]
        public void PublicReliefArrivalRecovery_ValidationRejectsTamperedTotals()
        {
            var fixture = PrepareExternalReliefProcurementWorld(
                25_1403, true);
            RunExternalReliefProcurementToDispatch(fixture);
            CompleteCivilianFreight(
                fixture, fixture.World.CivilianFreights[0]);
            var scheduler =
                new PublicReliefArrivalRecoveryCommandScheduler(
                    new PublicReliefArrivalRecoverySystem(
                        fixture.World.MasterSeed, fixture.Content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            runtime.RegisterEventHandler(
                scheduler.CreateProjectionHandler());
            scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);

            fixture.World.PublicReliefRecoveries[0]
                .TotalRecoveredQuantity++;
            Assert.Throws<InvalidOperationException>(
                () => fixture.World.Validate());
        }

        [Test]
        public void PublicReliefArrivalRecovery_IdenticalWorldsRemainDeterministic()
        {
            var left = PrepareExternalReliefProcurementWorld(
                25_1405, true);
            var right = PrepareExternalReliefProcurementWorld(
                25_1405, true);
            RunExternalReliefProcurementToDispatch(left);
            RunExternalReliefProcurementToDispatch(right);
            CompleteCivilianFreight(left, left.World.CivilianFreights[0]);
            CompleteCivilianFreight(right, right.World.CivilianFreights[0]);
            ResolvePublicReliefArrivalRecovery(left);
            ResolvePublicReliefArrivalRecovery(right);

            Assert.That(
                WorldSnapshotSerializer.Serialize(right.World, right.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    left.World, left.Content)));
        }

        [Test]
        public void PublicReliefArrivalRecovery_WorldSimulatorRunsSameSegment()
        {
            var fixture = PrepareExternalReliefProcurementWorld(
                25_1406, true);
            RunExternalReliefProcurementToDispatch(fixture);
            CompleteCivilianFreight(
                fixture, fixture.World.CivilianFreights[0]);

            new WorldSimulator(
                fixture.World.MasterSeed, fixture.Content)
                .AdvanceSegments(fixture.World, 1);

            Assert.That(fixture.World.PublicReliefRecoveries.Count,
                Is.EqualTo(1));
            Assert.That(
                fixture.World.PublicReliefRecoveries[0].Status,
                Is.EqualTo(PublicReliefRecoveryStatus.Fulfilled));
            Assert.That(fixture.World.PersistentWorldCommands.Exists(item =>
                item.CommandTypeId ==
                    PublicReliefArrivalRecoveryCommandScheduler
                        .CommandTypeId &&
                item.Status == PersistentWorldCommandStatus.Completed),
                Is.True);
        }

        [Test]
        public void Snapshot_MigratesVersionThirtyFiveToEmptyPublicReliefRecovery()
        {
            var fixture = PrepareCivilianFreightWorld(25_1404, 10);
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);
            var json = WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 35")
                .Replace("\"PublicReliefRecoveries\": []",
                    "\"PublicReliefRecoveries\": null")
                .Replace("\"PublicReliefRecoveryId\": \"\"",
                    "\"PublicReliefRecoveryId\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(
                json, fixture.Content);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.PublicReliefRecoveries, Is.Empty);
            Assert.That(loaded.CivilianFreights.Find(item =>
                item.Id == freight.Id).PublicReliefRecoveryId, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void FoodStorageLoss_DueStaticBatchCreatesAuditedInventoryLoss()
        {
            var fixture = PrepareFoodStorageLossWorld(25_1501);
            var batch = fixture.World.ProductBatches.Find(item =>
                !string.IsNullOrEmpty(item.OwnerFamilyId) &&
                !string.IsNullOrEmpty(item.StorageFacilityId) &&
                item.Quantity > 0);
            batch.NextFoodStorageAssessmentDay = fixture.World.AbsoluteDay;
            var quantityBefore = batch.Quantity;
            var freshnessBefore = batch.FreshnessBasisPoints;

            ResolveFoodStorageLoss(fixture.World, fixture.Content);

            var loss = fixture.World.FoodStorageLosses.Find(item =>
                item.BatchId == batch.Id);
            Assert.That(loss, Is.Not.Null);
            Assert.That(loss.QuantityLost, Is.GreaterThan(0));
            Assert.That(batch.Quantity,
                Is.EqualTo(quantityBefore - loss.QuantityLost));
            Assert.That(batch.FreshnessBasisPoints,
                Is.LessThan(freshnessBefore));
            Assert.That(batch.NextFoodStorageAssessmentDay,
                Is.EqualTo(fixture.World.AbsoluteDay + 30));
            Assert.That(fixture.World.InventoryTransactions.Exists(item =>
                item.Id == loss.InventoryTransactionId &&
                item.Type == InventoryTransactionType
                    .FoodStorageNaturalLoss &&
                item.Lines.Count == 1 &&
                item.Lines[0].QuantityDelta == -loss.QuantityLost),
                Is.True);
            fixture.World.Validate();
        }

        [Test]
        public void FoodStorageLoss_ProtectionAndReservationRemainAuthoritative()
        {
            var fixture = PrepareFoodStorageLossWorld(25_1502);
            var batch = fixture.World.ProductBatches.Find(item =>
                !string.IsNullOrEmpty(item.OwnerFamilyId) &&
                !string.IsNullOrEmpty(item.StorageFacilityId) &&
                item.Quantity >= 2);
            var facility = fixture.World.VillageFacilities.Find(item =>
                item.Id == batch.StorageFacilityId);
            facility.FoodStorageProtectionBasisPoints = 0;
            var market = new FormalCountyMarketSystem(fixture.Content);
            var reserved = batch.Quantity - 1;
            market.CreateSellOrder(
                fixture.World,
                fixture.World.CountyGovernances[0].Id,
                batch.OwnerFamilyId,
                batch.StorageFacilityId,
                batch.ProductDefinitionId,
                reserved,
                9,
                0,
                fixture.World.AbsoluteDay + 5);
            batch.NextFoodStorageAssessmentDay = fixture.World.AbsoluteDay;

            ResolveFoodStorageLoss(fixture.World, fixture.Content);

            var loss = fixture.World.FoodStorageLosses.Find(item =>
                item.BatchId == batch.Id);
            Assert.That(loss.QuantityLost, Is.EqualTo(1));
            Assert.That(batch.Quantity, Is.EqualTo(reserved));
            Assert.That(batch.ReservedQuantity, Is.EqualTo(reserved));
            Assert.That(loss.ReservedQuantity, Is.EqualTo(reserved));
            fixture.World.Validate();
        }

        [Test]
        public void FoodStorageLoss_MovingCivilianCargoIsNotDoubleCharged()
        {
            var fixture = PrepareCivilianFreightWorld(25_1503, 10);
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);
            var cargo = fixture.World.ProductBatches.Find(item =>
                item.InventoryContainerId ==
                    freight.TransportInventoryContainerId &&
                item.Quantity > 0);
            cargo.NextFoodStorageAssessmentDay = fixture.World.AbsoluteDay;

            var scheduled = ResolveFoodStorageLoss(
                fixture.World, fixture.Content);

            Assert.That(scheduled, Is.Zero);
            Assert.That(fixture.World.FoodStorageLosses.Exists(item =>
                item.BatchId == cargo.Id), Is.False);
            fixture.World.Validate();
        }

        [Test]
        public void FoodStorageLoss_RoundTripAndRepeatedSchedulingAreIdempotent()
        {
            var fixture = PrepareFoodStorageLossWorld(25_1504);
            var batch = fixture.World.ProductBatches.Find(item =>
                !string.IsNullOrEmpty(item.OwnerFamilyId) &&
                !string.IsNullOrEmpty(item.StorageFacilityId) &&
                item.Quantity > 0);
            batch.NextFoodStorageAssessmentDay = fixture.World.AbsoluteDay;
            Assert.That(ResolveFoodStorageLoss(
                fixture.World, fixture.Content), Is.EqualTo(1));
            Assert.That(ResolveFoodStorageLoss(
                fixture.World, fixture.Content), Is.Zero);

            var json = WorldSnapshotSerializer.Serialize(
                fixture.World, fixture.Content);
            var loaded = WorldSnapshotSerializer.Deserialize(
                json, fixture.Content);

            Assert.That(WorldSnapshotSerializer.Serialize(
                loaded, fixture.Content), Is.EqualTo(json));
            Assert.That(loaded.FoodStorageLosses.Count, Is.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void FoodStorageLoss_TamperedAuditIsRejected()
        {
            var fixture = PrepareFoodStorageLossWorld(25_1505);
            var batch = fixture.World.ProductBatches.Find(item =>
                !string.IsNullOrEmpty(item.OwnerFamilyId) &&
                !string.IsNullOrEmpty(item.StorageFacilityId) &&
                item.Quantity > 0);
            batch.NextFoodStorageAssessmentDay = fixture.World.AbsoluteDay;
            ResolveFoodStorageLoss(fixture.World, fixture.Content);

            fixture.World.FoodStorageLosses[0]
                .EffectiveLossBasisPoints++;

            Assert.Throws<InvalidOperationException>(
                () => fixture.World.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionThirtySixWithoutBackdatingStorageLoss()
        {
            var fixture = PrepareFoodStorageLossWorld(25_1506);
            var json = WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content)
                .Replace(
                    "\"SchemaVersion\": " +
                        WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 36")
                .Replace("\"FoodStorageLosses\": []",
                    "\"FoodStorageLosses\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(
                json, fixture.Content);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.FoodStorageLosses, Is.Empty);
            Assert.That(loaded.ProductBatches.TrueForAll(item =>
                item.NextFoodStorageAssessmentDay ==
                    loaded.AbsoluteDay + 30), Is.True);
            loaded.Validate();
        }

        [Test]
        public void CivilianFreight_CrossCountyDeliveryKeepsCargoAndProvisionsSeparate()
        {
            var fixture = PrepareCivilianFreightWorld(25_501, 1_000);
            var buyerWealthBefore = fixture.Buyer.Wealth;
            var sellerWealthBefore = fixture.Seller.Wealth;
            var carrierWealthBefore = fixture.CarrierFamily.Wealth;
            var provisionsBefore = fixture.Carrier.Provisions;
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World,
                fixture.Request);

            Assert.That(fixture.Buyer.Wealth,
                Is.EqualTo(buyerWealthBefore + 1_000 - 100));
            Assert.That(fixture.Seller.Wealth,
                Is.EqualTo(sellerWealthBefore + 2_000));
            Assert.That(freight.RemainingCargoQuantity, Is.EqualTo(1_000));
            Assert.That(fixture.World.ProductBatches.Exists(batch =>
                batch.OwnerFamilyId == fixture.Buyer.Id &&
                batch.InventoryContainerId == fixture.Transport.Id &&
                batch.Quantity == 1_000), Is.True);

            new WorldSimulator(fixture.World.MasterSeed, fixture.Content)
                .AdvanceSegments(fixture.World, 5);

            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            Assert.That(freight.NaturalLossQuantity,
                Is.GreaterThan(0));
            Assert.That(freight.DispatchedQuantity, Is.EqualTo(
                freight.DeliveredQuantity + freight.NaturalLossQuantity));
            Assert.That(freight.RemainingCargoQuantity, Is.Zero);
            Assert.That(fixture.Carrier.Provisions,
                Is.EqualTo(provisionsBefore - 1));
            Assert.That(fixture.CarrierFamily.Wealth,
                Is.EqualTo(carrierWealthBefore + 100));
            Assert.That(fixture.BuyerStorage.InventoryUnits,
                Is.EqualTo(freight.DeliveredQuantity));
            Assert.That(fixture.World.CivilianFreightLedgerEntries.Exists(
                entry => entry.Type ==
                    CivilianFreightLedgerType.NaturalLoss), Is.True);
            fixture.World.Validate();

            var json = WorldSnapshotSerializer.Serialize(
                fixture.World, fixture.Content);
            var loaded = WorldSnapshotSerializer.Deserialize(
                json, fixture.Content);
            Assert.That(loaded.CivilianFreights.Count, Is.EqualTo(1));
            Assert.That(loaded.CivilianFreights[0].Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            loaded.Validate();

            loaded.CivilianFreights[0].NaturalLossQuantity++;
            Assert.Throws<InvalidOperationException>(() => loaded.Validate());
        }

        [Test]
        public void CivilianFreight_DestinationCapacitySupportsPartialReceipt()
        {
            var fixture = PrepareCivilianFreightWorld(25_502, 1_000);
            fixture.BuyerStorage.Capacity = 400;
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World,
                fixture.Request);

            new WorldSimulator(fixture.World.MasterSeed, fixture.Content)
                .AdvanceSegments(fixture.World, 5);

            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.AwaitingReceipt));
            Assert.That(freight.DeliveredQuantity, Is.EqualTo(400));
            Assert.That(freight.RemainingCargoQuantity,
                Is.GreaterThan(0));
            Assert.That(freight.FreightFeeEscrow, Is.EqualTo(100));
            Assert.That(freight.FreightFeePaid, Is.Zero);

            fixture.BuyerStorage.Capacity = 2_000;
            fixture.FreightSystem.ResolveArrivals(fixture.World);

            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            Assert.That(freight.RemainingCargoQuantity, Is.Zero);
            Assert.That(freight.FreightFeePaid, Is.EqualTo(100));
            fixture.World.Validate();
        }

        [Test]
        public void CivilianFreight_InvalidCapacityDoesNotMutateWorld()
        {
            var fixture = PrepareCivilianFreightWorld(25_503, 1_000);
            fixture.Transport.CapacityWeight = 999;
            var before = WorldSnapshotSerializer.Serialize(
                fixture.World, fixture.Content);

            Assert.Throws<InvalidOperationException>(() =>
                fixture.FreightSystem.Dispatch(
                    fixture.World,
                    fixture.Request));

            Assert.That(WorldSnapshotSerializer.Serialize(
                fixture.World, fixture.Content), Is.EqualTo(before));
        }

        [Test]
        public void CivilianFreight_IdenticalWorldsResolveDeterministically()
        {
            var left = PrepareCivilianFreightWorld(25_504, 1_000);
            var right = PrepareCivilianFreightWorld(25_504, 1_000);
            left.FreightSystem.Dispatch(left.World, left.Request);
            right.FreightSystem.Dispatch(right.World, right.Request);

            new WorldSimulator(left.World.MasterSeed, left.Content)
                .AdvanceSegments(left.World, 5);
            new WorldSimulator(right.World.MasterSeed, right.Content)
                .AdvanceSegments(right.World, 5);

            Assert.That(
                WorldSnapshotSerializer.Serialize(left.World, left.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    right.World, right.Content)));
        }

        [Test]
        public void Snapshot_MigratesVersionThirtyToEmptyCivilianFreight()
        {
            var fixture = PrepareCivilianFreightWorld(25_505, 1_000);
            var versionThirty = WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 30");

            var migrated = WorldSnapshotSerializer.Deserialize(
                versionThirty, fixture.Content);

            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.CivilianFreights, Is.Empty);
            Assert.That(migrated.CivilianFreightLedgerEntries, Is.Empty);
            Assert.That(migrated.InventoryTransactions.TrueForAll(item =>
                string.IsNullOrEmpty(item.SourceCivilianFreightId)), Is.True);
            migrated.Validate();
        }

        [Test]
        public void CivilianFreightPlanning_UsesKnownShortestMultiLegRoute()
        {
            var fixture = PrepareCivilianFreightWorld(25_601, 1_000);
            ConfigureCivilianFreightRouteChoices(fixture.World);
            fixture.FreightSystem.RegisterCarrier(
                fixture.World,
                NewCivilianCarrierRegistration(
                    fixture,
                    CivilianFreightRoutePolicyIds.ShortestKnown));

            fixture.FreightSystem.ProcessDailyPlanning(fixture.World);

            Assert.That(fixture.World.CivilianFreightDemands.Count,
                Is.EqualTo(1));
            Assert.That(fixture.World.CivilianCarrierOffers.Count,
                Is.EqualTo(1));
            Assert.That(fixture.World.CivilianFreights.Count,
                Is.EqualTo(1));
            var freight = fixture.World.CivilianFreights[0];
            Assert.That(freight.PlannedRouteIds, Is.EqualTo(new[]
            {
                "route.freight.short.1",
                "route.freight.short.2"
            }));
            Assert.That(freight.PlannedRouteIds,
                Does.Not.Contain("route.freight_origin_destination"));

            new WorldSimulator(fixture.World.MasterSeed, fixture.Content)
                .AdvanceSegments(fixture.World, 4);

            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            Assert.That(freight.CurrentRouteIndex, Is.EqualTo(1));
            Assert.That(fixture.Carrier.LocationId,
                Is.EqualTo(freight.DestinationLocationId));
            Assert.That(fixture.Transport.LocationId,
                Is.EqualTo(freight.DestinationLocationId));
            fixture.World.Validate();
        }

        [Test]
        public void CivilianFreightPlanning_SafestPolicyChoosesLongerKnownPath()
        {
            var fixture = PrepareCivilianFreightWorld(25_602, 1_000);
            ConfigureCivilianFreightRouteChoices(fixture.World);
            Assert.That(fixture.FreightSystem.GenerateDemands(fixture.World),
                Is.EqualTo(1));
            fixture.World.CivilianFreightDemands[0].RoutePolicyId =
                CivilianFreightRoutePolicyIds.SafestKnown;
            fixture.FreightSystem.RegisterCarrier(
                fixture.World,
                NewCivilianCarrierRegistration(
                    fixture,
                    CivilianFreightRoutePolicyIds.SafestKnown));

            Assert.That(fixture.FreightSystem.GenerateOffers(fixture.World),
                Is.EqualTo(1));

            var offer = fixture.World.CivilianCarrierOffers[0];
            Assert.That(offer.PlannedRouteIds, Is.EqualTo(new[]
            {
                "route.freight.safe.1",
                "route.freight.safe.2"
            }));
            Assert.That(offer.TotalDistanceKilometers, Is.EqualTo(24));
            Assert.That(offer.MinimumSecurityBasisPoints, Is.EqualTo(9_000));
            fixture.World.Validate();
        }

        [Test]
        public void CivilianFreightPlanning_RepeatedPlanningDoesNotDuplicateOrders()
        {
            var fixture = PrepareCivilianFreightWorld(25_603, 1_000);
            ConfigureCivilianFreightRouteChoices(fixture.World);

            Assert.That(fixture.FreightSystem.GenerateDemands(fixture.World),
                Is.EqualTo(1));
            Assert.That(fixture.FreightSystem.GenerateDemands(fixture.World),
                Is.Zero);

            Assert.That(fixture.World.CivilianFreightDemands.Count,
                Is.EqualTo(1));
            Assert.That(fixture.World.CivilianFreightDemands[0].Status,
                Is.EqualTo(CivilianFreightDemandStatus.Active));
            fixture.World.Validate();
        }

        [Test]
        public void CivilianFreightPlanning_SelectsStableLowestCarrierOffer()
        {
            var fixture = PrepareCivilianFreightWorld(25_606, 1_000);
            ConfigureCivilianFreightRouteChoices(fixture.World);
            var expensive = NewCivilianCarrierRegistration(
                fixture,
                CivilianFreightRoutePolicyIds.ShortestKnown);
            expensive.BaseFee = 100;
            fixture.FreightSystem.RegisterCarrier(fixture.World, expensive);
            var cheaper = AddSecondCivilianFreightCarrier(fixture, 10);
            fixture.FreightSystem.RegisterCarrier(fixture.World, cheaper);

            fixture.FreightSystem.ProcessDailyPlanning(fixture.World);

            Assert.That(fixture.World.CivilianCarrierOffers.Count,
                Is.EqualTo(2));
            var accepted = fixture.World.CivilianCarrierOffers.Find(item =>
                item.Status == CivilianCarrierOfferStatus.Accepted);
            var rejected = fixture.World.CivilianCarrierOffers.Find(item =>
                item.Status == CivilianCarrierOfferStatus.Rejected);
            Assert.That(accepted.CarrierPersonId,
                Is.EqualTo("person.freight_carrier_alt"));
            Assert.That(accepted.QuotedFreightFee,
                Is.LessThan(rejected.QuotedFreightFee));
            Assert.That(fixture.World.CivilianFreights[0].CarrierPersonId,
                Is.EqualTo(accepted.CarrierPersonId));
            fixture.World.Validate();
        }

        [Test]
        public void CivilianFreightPlanning_IdenticalWorldsRemainDeterministic()
        {
            var left = PrepareCivilianFreightWorld(25_604, 1_000);
            var right = PrepareCivilianFreightWorld(25_604, 1_000);
            ConfigureCivilianFreightRouteChoices(left.World);
            ConfigureCivilianFreightRouteChoices(right.World);
            left.FreightSystem.RegisterCarrier(
                left.World,
                NewCivilianCarrierRegistration(
                    left,
                    CivilianFreightRoutePolicyIds.ShortestKnown));
            right.FreightSystem.RegisterCarrier(
                right.World,
                NewCivilianCarrierRegistration(
                    right,
                    CivilianFreightRoutePolicyIds.ShortestKnown));

            left.FreightSystem.ProcessDailyPlanning(left.World);
            right.FreightSystem.ProcessDailyPlanning(right.World);
            new WorldSimulator(left.World.MasterSeed, left.Content)
                .AdvanceSegments(left.World, 4);
            new WorldSimulator(right.World.MasterSeed, right.Content)
                .AdvanceSegments(right.World, 4);

            Assert.That(
                WorldSnapshotSerializer.Serialize(left.World, left.Content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    right.World, right.Content)));
        }

        [Test]
        public void Snapshot_MigratesVersionThirtyOneFreightToSingleRoutePlan()
        {
            var fixture = PrepareCivilianFreightWorld(25_605, 1_000);
            fixture.FreightSystem.Dispatch(fixture.World, fixture.Request);
            var versionThirtyOne = WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 31");

            var migrated = WorldSnapshotSerializer.Deserialize(
                versionThirtyOne, fixture.Content);

            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.CivilianFreightDemands, Is.Empty);
            Assert.That(migrated.CivilianCarrierRegistrations, Is.Empty);
            Assert.That(migrated.CivilianCarrierOffers, Is.Empty);
            Assert.That(migrated.CivilianFreights[0].PlannedRouteIds,
                Is.EqualTo(new[] { "route.freight_origin_destination" }));
            Assert.That(migrated.CivilianFreights[0].CurrentRouteIndex,
                Is.Zero);
            migrated.Validate();
        }

        [Test]
        public void ProductInventory_MarketableQuantityIncludesOrganizationContainer()
        {
            var world = PrototypeWorldFactory.Create184World(25_102);
            var quantity = new ProductInventorySystem().MarketableQuantity(
                world,
                "location.zhongshan",
                CoreProductionContent.IronMaterialProductId);

            Assert.That(quantity, Is.GreaterThan(0));
            world.Validate();
        }

        [Test]
        public void Processing_WheatToDryRationIsBalancedAndRoundTrips()
        {
            var world = VillagePrototypeFactory.Create(200, 22_002);
            var openingTransactionCount = world.InventoryTransactions.Count;
            var family = world.Families[0];
            var storage = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.HouseholdGranary &&
                        item.OwnerFamilyId == family.Id);
            var inventory = new ProductInventorySystem();
            inventory.ConvertLegacyBalanceToBatch(
                world,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                CoreProductionContent.WheatGrainProductId,
                10);
            var processing = new ProcessingProductionSystem();
            var milling = processing.CreateOrder(
                world,
                CoreProductionContent.HandMillWheatRecipeId,
                CoreProductionContent.HandMillingMethodId,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                ProductionControlMode.WorkOrder,
                1);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 2);

            Assert.That(milling.Status, Is.EqualTo(ProductionOrderStatus.Completed));
            Assert.That(
                world.ProductBatches.Find(
                    item => item.ProductDefinitionId ==
                            CoreProductionContent.WheatFlourProductId).Quantity,
                Is.EqualTo(8));
            Assert.That(
                world.ProductBatches.Find(
                    item => item.ProductDefinitionId ==
                            CoreProductionContent.WheatBranProductId).Quantity,
                Is.EqualTo(2));
            var rationOrder = processing.CreateOrder(
                world,
                CoreProductionContent.MakeDryRationRecipeId,
                CoreProductionContent.DryRationMethodId,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                ProductionControlMode.DelegatedPolicy,
                1);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);

            Assert.That(
                rationOrder.Status,
                Is.EqualTo(ProductionOrderStatus.Completed));
            Assert.That(
                inventory.MarketableQuantity(
                    world,
                    family.LocationId,
                    CoreProductionContent.DryRationProductId),
                Is.EqualTo(8));
            Assert.That(
                ProductInventorySystem.CalculatePhysicalInventoryUnits(
                    world, storage.Id, family.Id),
                Is.EqualTo(storage.InventoryUnits));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.ProcessingWorkOrders.Count, Is.EqualTo(2));
            Assert.That(loaded.InventoryTransactions.Count,
                Is.EqualTo(openingTransactionCount + 5));
            Assert.That(
                loaded.ProductBatches.Find(
                    item => item.ProductDefinitionId ==
                            CoreProductionContent.DryRationProductId).Quantity,
                Is.EqualTo(8));
            loaded.Validate();
        }

        [Test]
        public void Processing_InsufficientInputHasNoSideEffects()
        {
            var world = VillagePrototypeFactory.Create(200, 22_003);
            var family = world.Families[0];
            var storage = world.VillageFacilities.Find(
                item => item.Kind == VillageFacilityKind.HouseholdGranary &&
                        item.OwnerFamilyId == family.Id);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new ProcessingProductionSystem().CreateOrder(
                    world,
                    CoreProductionContent.HandMillWheatRecipeId,
                    CoreProductionContent.HandMillingMethodId,
                    family.Id,
                    storage.Id,
                    family.HeadPersonId,
                    ProductionControlMode.DirectAssignment,
                    1));

            Assert.That(WorldSnapshotSerializer.Serialize(world), Is.EqualTo(before));
        }

        [Test]
        public void Processing_ControlModesProduceTheSameInventoryFacts()
        {
            List<string> expected = null;
            foreach (var mode in new[]
                     {
                         ProductionControlMode.PersonalLabor,
                         ProductionControlMode.DelegatedPolicy
                     })
            {
                var world = VillagePrototypeFactory.Create(200, 22_004);
                var family = world.Families[0];
                var storage = world.VillageFacilities.Find(
                    item => item.Kind == VillageFacilityKind.HouseholdGranary &&
                            item.OwnerFamilyId == family.Id);
                new ProductInventorySystem().ConvertLegacyBalanceToBatch(
                    world,
                    family.Id,
                    storage.Id,
                    family.HeadPersonId,
                    CoreProductionContent.WheatGrainProductId,
                    10);
                new ProcessingProductionSystem().CreateOrder(
                    world,
                    CoreProductionContent.HandMillWheatRecipeId,
                    CoreProductionContent.HandMillingMethodId,
                    family.Id,
                    storage.Id,
                    family.HeadPersonId,
                    mode,
                    1);
                new WorldSimulator(world.MasterSeed).AdvanceDays(world, 2);
                var facts = new List<string>();
                for (var i = 0; i < world.ProductBatches.Count; i++)
                {
                    facts.Add(
                        world.ProductBatches[i].ProductDefinitionId + "|" +
                        world.ProductBatches[i].Quantity + "|" +
                        world.ProductBatches[i].ReservedQuantity);
                }

                facts.Sort(StringComparer.Ordinal);
                if (expected == null)
                {
                    expected = facts;
                }
                else
                {
                    Assert.That(facts, Is.EqualTo(expected));
                }
            }
        }

        [Test]
        public void Snapshot_MigratesVersionNineToInventoryCollections()
        {
            var world = BuildMinimalWorld();
            var originalPeople = world.People.Count;
            var originalFamilies = world.Families.Count;
            var originalStorageMode = world.PopulationStorage.Mode;
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 9")
                .Replace("\"ProductBatches\": []", "\"ProductBatches\": null")
                .Replace(
                    "\"InventoryTransactions\": []",
                    "\"InventoryTransactions\": null")
                .Replace(
                    "\"ProcessingWorkOrders\": []",
                    "\"ProcessingWorkOrders\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.ProductBatches, Is.Not.Null);
            Assert.That(loaded.InventoryTransactions, Is.Not.Null);
            Assert.That(loaded.ProcessingWorkOrders, Is.Not.Null);
            Assert.That(loaded.People.Count, Is.EqualTo(originalPeople));
            Assert.That(loaded.Families.Count, Is.EqualTo(originalFamilies));
            Assert.That(loaded.PopulationStorage.Mode, Is.EqualTo(originalStorageMode));
            loaded.Validate();
        }

        [Test]
        public void Snapshot_VersionNineMigrationPreservesLoadedModManifest()
        {
            var registry = ProductionContentRegistry.CreateCore();
            registry.Register(ProductionContentJson.DeserializePackage(
                TestProductionModJson()));
            var world = BuildMinimalWorld();
            world.ProductionContentManifest = registry.CreateManifest();
            var json = WorldSnapshotSerializer.Serialize(world, registry)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 9")
                .Replace("\"ProductBatches\": []", "\"ProductBatches\": null")
                .Replace(
                    "\"InventoryTransactions\": []",
                    "\"InventoryTransactions\": null")
                .Replace(
                    "\"ProcessingWorkOrders\": []",
                    "\"ProcessingWorkOrders\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(json, registry);

            Assert.That(loaded.ProductionContentManifest.Packages.Count,
                Is.EqualTo(2));
            Assert.That(
                loaded.ProductionContentManifest.ResolvedHash,
                Is.EqualTo(registry.ResolvedHash));
        }

        [Test]
        public void PopulationStore_RoundTripPreservesCoreDetailAndAttachment()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = VillagePrototypeFactory.Create(200, 15_001);
                var store = new PartitionedPopulationStore(root);
                var manifest = PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.village",
                    8,
                    1);

                PopulationStorageWorldAdapter.ValidateAttachedPackage(world, store);
                var original = world.People[17];
                Assert.That(
                    store.TryReadCore(original.Id, out var core),
                    Is.True);
                Assert.That(core.Matches(original), Is.True);
                Assert.That(
                    store.TryReadDetail(original.Id, out var detail),
                    Is.True);
                Assert.That(detail.Id, Is.EqualTo(original.Id));
                Assert.That(detail.FamilyId, Is.EqualTo(original.FamilyId));
                Assert.That(
                    detail.ProfessionalSkills.Agriculture,
                    Is.EqualTo(original.ProfessionalSkills.Agriculture));
                Assert.That(manifest.PermanentPersonCount, Is.EqualTo(200));
                Assert.That(manifest.DetailExtensionCount, Is.EqualTo(200));
                Assert.That(world.PopulationStorage.ManifestSha256, Is.Not.Empty);

                var loaded = WorldSnapshotSerializer.Deserialize(
                    WorldSnapshotSerializer.Serialize(world));
                Assert.That(
                    loaded.PopulationStorage.Mode,
                    Is.EqualTo(PopulationStorageMode.PartitionedPackage));
                Assert.That(
                    loaded.PopulationStorage.ManifestSha256,
                    Is.EqualTo(world.PopulationStorage.ManifestSha256));
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void PopulationResidency_DemotionCannotDiscardDirtyDetail()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = BuildMinimalWorld();
                var store = new PartitionedPopulationStore(root);
                PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.residency",
                    4,
                    1);
                var session = new PopulationResidencySession(store);
                var person = session.Promote("person.liu_bei");

                session.DemoteUnchanged(person.Id);
                Assert.That(session.HotCount, Is.EqualTo(0));
                person = session.Promote(person.Id);
                person.Wealth++;

                Assert.Throws<InvalidOperationException>(
                    () => session.DemoteUnchanged(person.Id));
                Assert.That(session.HotCount, Is.EqualTo(1));
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void Attention_ReasonsAggregateAndRoundTripWithoutChangingPeople()
        {
            var world = BuildMinimalWorld();
            world.PlayerPersonId = "person.liu_bei";
            var target = world.People.Find(item => item.Id == "person.guan_yu");
            var originalWealth = target.Wealth;
            var originalRelationshipCount = world.Relationships.Count;
            var system = new AttentionSystem();

            system.SetReason(
                world,
                world.PlayerPersonId,
                AttentionTargetKind.Person,
                target.Id,
                AttentionSystem.ManualReasonId,
                AttentionLevel.Normal);
            system.SetReason(
                world,
                world.PlayerPersonId,
                AttentionTargetKind.Person,
                target.Id,
                "attention.reason.active_event",
                AttentionLevel.Deep);
            Assert.That(
                system.GetEffectiveLevel(
                    world,
                    world.PlayerPersonId,
                    AttentionTargetKind.Person,
                    target.Id),
                Is.EqualTo(AttentionLevel.Deep));

            system.ClearReason(
                world,
                world.PlayerPersonId,
                AttentionTargetKind.Person,
                target.Id,
                "attention.reason.active_event");

            Assert.That(
                system.GetEffectiveLevel(
                    world,
                    world.PlayerPersonId,
                    AttentionTargetKind.Person,
                    target.Id),
                Is.EqualTo(AttentionLevel.Normal));
            Assert.That(world.AttentionFocuses.Count, Is.EqualTo(1));
            Assert.That(world.AttentionLedgerEntries.Count, Is.EqualTo(3));
            Assert.That(target.Wealth, Is.EqualTo(originalWealth));
            Assert.That(
                world.Relationships.Count,
                Is.EqualTo(originalRelationshipCount));
            world.Validate();

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.AttentionFocuses.Count, Is.EqualTo(1));
            Assert.That(loaded.AttentionLedgerEntries.Count, Is.EqualTo(3));
            Assert.That(
                loaded.AttentionFocuses[0].ReasonId,
                Is.EqualTo(AttentionSystem.ManualReasonId));
            loaded.Validate();
        }

        [Test]
        public void Attention_LocalRelationshipNetworkIsBoundedAndDoesNotAddEdges()
        {
            var world = BuildMinimalWorld();
            var family = world.Families[0];
            family.MemberIds.Add("person.guan_yu");
            world.People[0].FamilyId = family.Id;
            world.People[1].FamilyId = family.Id;
            world.Relationships.Add(new RelationshipState
            {
                Id = "relationship.person.liu_bei.person.guan_yu",
                FromPersonId = "person.liu_bei",
                ToPersonId = "person.guan_yu",
                Affection = 5_000,
                Trust = 5_000,
                Respect = 5_000
            });
            var relationshipCount = world.Relationships.Count;
            var system = new AttentionSystem();

            var first = system.BuildLocalRelationshipNetwork(
                world, "person.liu_bei", 2);
            var second = system.BuildLocalRelationshipNetwork(
                world, "person.liu_bei", 2);

            Assert.That(first.PersonIds.Count, Is.EqualTo(2));
            Assert.That(first.PersonIds, Is.EqualTo(second.PersonIds));
            Assert.That(
                first.ExplicitRelationshipIds,
                Is.EqualTo(second.ExplicitRelationshipIds));
            Assert.That(first.FamilyIds, Does.Contain(family.Id));
            Assert.That(world.Relationships.Count, Is.EqualTo(relationshipCount));
            world.Validate();
        }

        [Test]
        public void Attention_ResidencyPlanPromotesDemotesAndRetainsDirtyPeople()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = BuildMinimalWorld();
                world.PlayerPersonId = "person.liu_bei";
                world.Relationships.Add(new RelationshipState
                {
                    Id = "relationship.person.guan_yu.person.liu_bei",
                    FromPersonId = "person.guan_yu",
                    ToPersonId = "person.liu_bei",
                    Affection = 5_000,
                    Trust = 5_000,
                    Respect = 5_000
                });
                var store = new PartitionedPopulationStore(root);
                PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.attention_residency",
                    4,
                    1);
                var attention = new AttentionSystem();
                attention.SetReason(
                    world,
                    world.PlayerPersonId,
                    AttentionTargetKind.Person,
                    "person.guan_yu",
                    AttentionSystem.ManualReasonId,
                    AttentionLevel.Deep);
                var session = new PopulationResidencySession(store);

                var promoted = session.ReconcileAttention(
                    attention.BuildResidencyPlan(world, world.PlayerPersonId, 2)
                        .HotPersonIds);
                Assert.That(promoted.PromotedPersonIds.Count, Is.EqualTo(2));
                Assert.That(session.HotCount, Is.EqualTo(2));

                attention.ClearReason(
                    world,
                    world.PlayerPersonId,
                    AttentionTargetKind.Person,
                    "person.guan_yu",
                    AttentionSystem.ManualReasonId);
                var demoted = session.ReconcileAttention(
                    attention.BuildResidencyPlan(world, world.PlayerPersonId, 2)
                        .HotPersonIds);
                Assert.That(
                    demoted.DemotedPersonIds,
                    Does.Contain("person.guan_yu"));
                Assert.That(session.HotCount, Is.EqualTo(1));

                var dirty = session.Promote("person.guan_yu");
                dirty.Wealth++;
                var retained = session.ReconcileAttention(
                    attention.BuildResidencyPlan(world, world.PlayerPersonId, 2)
                        .HotPersonIds);
                Assert.That(
                    retained.DirtyRetainedPersonIds,
                    Does.Contain("person.guan_yu"));
                Assert.That(session.HotCount, Is.EqualTo(2));
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void Snapshot_MigratesVersionTenToEmptyAttentionCollections()
        {
            var world = BuildMinimalWorld();
            var originalPeople = world.People.Count;
            var originalPersonId = world.People[0].Id;
            var originalPersonWealth = world.People[0].Wealth;
            var originalFamilies = world.Families.Count;
            var originalFamilyMembers = world.Families[0].MemberIds.Count;
            var originalRelationships = world.Relationships.Count;
            var storageMode = world.PopulationStorage.Mode;
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 10")
                .Replace("\"AttentionFocuses\": []", "\"AttentionFocuses\": null")
                .Replace(
                    "\"AttentionLedgerEntries\": []",
                    "\"AttentionLedgerEntries\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.AttentionFocuses, Is.Empty);
            Assert.That(loaded.AttentionLedgerEntries, Is.Empty);
            Assert.That(loaded.People.Count, Is.EqualTo(originalPeople));
            Assert.That(loaded.People[0].Id, Is.EqualTo(originalPersonId));
            Assert.That(
                loaded.People[0].Wealth,
                Is.EqualTo(originalPersonWealth));
            Assert.That(loaded.Families.Count, Is.EqualTo(originalFamilies));
            Assert.That(
                loaded.Families[0].MemberIds.Count,
                Is.EqualTo(originalFamilyMembers));
            Assert.That(
                loaded.Relationships.Count,
                Is.EqualTo(originalRelationships));
            Assert.That(loaded.PopulationStorage.Mode, Is.EqualTo(storageMode));
            loaded.Validate();
        }

        [Test]
        public void CountyGovernance_AnnualSettlementMovesRealMoneyAndGrain()
        {
            var world = VillagePrototypeFactory.Create(300, 22_100);
            for (var i = 0; i < world.Families.Count; i++)
            {
                world.Families[i].LastHarvestGrain = 100;
            }
            world.AbsoluteDay = 300;
            new VillageLifeSystem(world.MasterSeed).ResolveMonthly(world);
            VillageLifeSystem.RefreshAllCaches(world);
            var governance = world.CountyGovernances[0];
            var organization = world.Organizations.Find(
                item => item.Id == governance.GovernmentOrganizationId);
            var moneyBefore = organization.Treasury;
            for (var i = 0; i < world.Families.Count; i++)
            {
                moneyBefore += world.Families[i].Wealth;
            }

            var grainBefore = governance.CountyGranaryGrain;
            for (var i = 0; i < world.Villages.Count; i++)
            {
                grainBefore += world.Villages[i].PublicGranaryGrain;
            }

            new CountyGovernanceSystem().ResolveMonthly(world);

            var moneyAfter = organization.Treasury;
            for (var i = 0; i < world.Families.Count; i++)
            {
                moneyAfter += world.Families[i].Wealth;
            }

            var grainAfter = governance.CountyGranaryGrain;
            for (var i = 0; i < world.Villages.Count; i++)
            {
                grainAfter += world.Villages[i].PublicGranaryGrain;
            }

            Assert.That(governance.TotalMoneyTaxCollected, Is.GreaterThan(0));
            Assert.That(governance.TotalGrainTaxReceived, Is.GreaterThan(0));
            Assert.That(world.CountyHouseholdTaxes.Count,
                Is.EqualTo(world.Families.Count));
            Assert.That(moneyAfter, Is.EqualTo(moneyBefore));
            Assert.That(grainAfter, Is.EqualTo(grainBefore));
            world.Validate();
        }

        [Test]
        public void CountyGovernance_SameDaySettlementIsIdempotent()
        {
            var world = VillagePrototypeFactory.Create(200, 22_101);
            world.AbsoluteDay = 30;
            var system = new CountyGovernanceSystem();

            system.ResolveMonthly(world);
            var snapshot = WorldSnapshotSerializer.Serialize(world);
            var ledgerCount = world.CountyFiscalLedgerEntries.Count;
            system.ResolveMonthly(world);

            Assert.That(world.CountyFiscalLedgerEntries.Count,
                Is.EqualTo(ledgerCount));
            Assert.That(
                WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(snapshot));
        }

        [Test]
        public void CountyGovernance_LowFoodMovesCountyReliefIntoVillage()
        {
            var world = VillagePrototypeFactory.Create(200, 22_102);
            var governance = world.CountyGovernances[0];
            var village = world.Villages[0];
            village.FoodSecurityBasisPoints = 2_000;
            governance.CountyGranaryGrain = 100;
            var countyBefore = governance.CountyGranaryGrain;
            var villageBefore = village.PublicGranaryGrain;
            world.AbsoluteDay = 30;

            new CountyGovernanceSystem().ResolveMonthly(world);

            Assert.That(governance.TotalReliefGrain, Is.GreaterThan(0));
            Assert.That(governance.CountyGranaryGrain,
                Is.LessThan(countyBefore));
            Assert.That(village.PublicGranaryGrain,
                Is.GreaterThan(villageBefore));
            Assert.That(
                governance.CountyGranaryGrain + village.PublicGranaryGrain,
                Is.EqualTo(countyBefore + villageBefore));
            world.Validate();
        }

        [Test]
        public void CountyGovernance_GentryComplianceChangesActualRevenue()
        {
            var lowCompliance = VillagePrototypeFactory.Create(200, 22_103);
            var highCompliance = VillagePrototypeFactory.Create(200, 22_103);
            for (var i = 0; i < lowCompliance.CountyGentryHouses.Count; i++)
            {
                lowCompliance.CountyGentryHouses[i].TaxComplianceBasisPoints = 0;
                highCompliance.CountyGentryHouses[i].TaxComplianceBasisPoints =
                    10_000;
            }

            lowCompliance.AbsoluteDay = 300;
            highCompliance.AbsoluteDay = 300;
            var system = new CountyGovernanceSystem();
            system.ResolveMonthly(lowCompliance);
            system.ResolveMonthly(highCompliance);

            Assert.That(
                highCompliance.CountyGovernances[0].TotalMoneyTaxCollected,
                Is.GreaterThan(
                    lowCompliance.CountyGovernances[0].TotalMoneyTaxCollected));
            lowCompliance.Validate();
            highCompliance.Validate();
        }

        [Test]
        public void CountyGovernance_SnapshotRoundTripPreservesFiscalFacts()
        {
            var world = VillagePrototypeFactory.Create(200, 22_104);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 300);

            var json = WorldSnapshotSerializer.Serialize(world);
            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(
                WorldSnapshotSerializer.Serialize(loaded),
                Is.EqualTo(json));
            Assert.That(loaded.CountyGovernances[0].TotalMoneyTaxCollected,
                Is.GreaterThan(0));
            Assert.That(loaded.CountyFiscalLedgerEntries, Is.Not.Empty);
        }

        [Test]
        public void CountyGovernance_SameSeedAndDurationProduceSameSnapshot()
        {
            var first = VillagePrototypeFactory.Create(200, 22_106);
            var second = VillagePrototypeFactory.Create(200, 22_106);

            new WorldSimulator(first.MasterSeed).AdvanceDays(first, 300);
            new WorldSimulator(second.MasterSeed).AdvanceDays(second, 300);

            Assert.That(
                WorldSnapshotSerializer.Serialize(second),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(first)));
        }

        [Test]
        public void Snapshot_MigratesVersionElevenToEmptyCountyCollections()
        {
            var world = BuildMinimalWorld();
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 11")
                .Replace("\"CountyGovernances\": []", "\"CountyGovernances\": null")
                .Replace("\"CountyGentryHouses\": []", "\"CountyGentryHouses\": null")
                .Replace("\"CountyHouseholdTaxes\": []", "\"CountyHouseholdTaxes\": null")
                .Replace(
                    "\"CountyFiscalLedgerEntries\": []",
                    "\"CountyFiscalLedgerEntries\": null");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.CountyGovernances, Is.Empty);
            Assert.That(loaded.CountyGentryHouses, Is.Empty);
            Assert.That(loaded.CountyHouseholdTaxes, Is.Empty);
            Assert.That(loaded.CountyFiscalLedgerEntries, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void CountyGovernance_UnbalancedFiscalEntryIsRejected()
        {
            var world = VillagePrototypeFactory.Create(200, 22_105);
            world.CountyFiscalLedgerEntries.Add(
                new CountyFiscalLedgerEntryState
                {
                    Id = "county_fiscal.invalid",
                    Day = 0,
                    Type = CountyFiscalEntryType.HouseholdPayment,
                    CountyGovernanceId = world.CountyGovernances[0].Id,
                    FamilyId = world.Families[0].Id,
                    FamilyMoneyDelta = -10,
                    GovernmentMoneyDelta = 9,
                    Amount = 10
                });

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void PopulationStore_TamperedPartitionIsRejected()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = BuildMinimalWorld();
                var store = new PartitionedPopulationStore(root);
                var manifest = PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.tamper",
                    2,
                    1);
                var path = Path.Combine(
                    root,
                    manifest.Partitions[0].CoreRelativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
                using (var stream = new FileStream(
                           path,
                           FileMode.Append,
                           FileAccess.Write,
                           FileShare.None))
                {
                    stream.WriteByte(0x5A);
                }

                Assert.Throws<InvalidOperationException>(
                    () => new PartitionedPopulationStore(root).OpenCurrent());
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void PopulationStore_InputOrderDoesNotChangePartitionContent()
        {
            var firstRoot = NewPopulationStoreTestRoot();
            var secondRoot = NewPopulationStoreTestRoot();
            try
            {
                var world = VillagePrototypeFactory.Create(200, 15_002);
                var firstCheckpoint = PopulationCheckpoint.FromInlineWorld(
                    world,
                    "population.test.determinism",
                    8,
                    1);
                var secondCheckpoint = PopulationCheckpoint.FromInlineWorld(
                    world,
                    "population.test.determinism",
                    8,
                    1);
                secondCheckpoint.People.Reverse();
                secondCheckpoint.DetailExtensions.Reverse();

                var first = new PartitionedPopulationStore(firstRoot)
                    .CommitCheckpoint(firstCheckpoint);
                var second = new PartitionedPopulationStore(secondRoot)
                    .CommitCheckpoint(secondCheckpoint);

                Assert.That(
                    second.ManifestSha256,
                    Is.EqualTo(first.ManifestSha256));
                for (var i = 0; i < first.Partitions.Count; i++)
                {
                    Assert.That(
                        second.Partitions[i].CoreSha256,
                        Is.EqualTo(first.Partitions[i].CoreSha256));
                    Assert.That(
                        second.Partitions[i].DetailSha256,
                        Is.EqualTo(first.Partitions[i].DetailSha256));
                }
            }
            finally
            {
                DeletePopulationStoreTestRoot(firstRoot);
                DeletePopulationStoreTestRoot(secondRoot);
            }
        }

        [Test]
        public void PopulationStore_NewRevisionPersistsChangedHotDetail()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = BuildMinimalWorld();
                var store = new PartitionedPopulationStore(root);
                PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.revision",
                    2,
                    1);
                world.People[0].Wealth = 4321;
                world.People.Add(new PersonState
                {
                    Id = "person.test.newborn",
                    DisplayName = "新生儿",
                    LocationId = "location.zhuo",
                    BirthLocationId = "location.zhuo",
                    BirthDay = world.AbsoluteDay,
                    Gender = PersonGender.Female
                });
                Assert.Throws<InvalidOperationException>(
                    () => WorldSnapshotSerializer.Serialize(world));
                var secondManifest = PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.revision",
                    2,
                    2);

                Assert.That(secondManifest.StorageRevision, Is.EqualTo(2));
                Assert.That(secondManifest.PermanentPersonCount, Is.EqualTo(3));
                Assert.That(
                    new PartitionedPopulationStore(root).TryReadDetail(
                        world.People[0].Id,
                        out var persisted),
                    Is.True);
                Assert.That(persisted.Wealth, Is.EqualTo(4321));
                Assert.That(
                    Directory.Exists(Path.Combine(
                        root,
                        "generations",
                        "generation-00000000000000000001")),
                    Is.True);
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void PersonRepository_ReadsStayCleanAndFirstSystemsTrackUpdates()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var person = world.People[0];
            var repository = new WorldStatePersonRepository(world);

            Assert.That(repository.GetRequired(person.Id), Is.SameAs(person));
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
            repository.GetRequiredForUpdate(person.Id);
            repository.GetRequiredForUpdate("person.guan_yu");
            Assert.That(
                repository.GetChangedPersonIds(),
                Is.EqualTo(new[] { "person.guan_yu", person.Id }));
            repository.AcceptChanges(
                new[] { person.Id, "person.guan_yu" });

            var travel = new TravelSystem(repository);
            travel.StartJourney(
                world,
                new StableId(person.Id),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhongshan"),
                TravelMode.Foot);
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
            travel.ConsumeDailyTravelProvisions(world);
            Assert.That(
                repository.GetChangedPersonIds(),
                Is.EqualTo(new[] { person.Id }));
            repository.AcceptChanges(new[] { person.Id });

            var relationship = new RelationshipSystem(
                world.MasterSeed, repository);
            relationship.ResolveVisit(
                world,
                new StableId(person.Id),
                new StableId("person.guan_yu"),
                1);
            Assert.That(
                repository.GetChangedPersonIds(),
                Is.EqualTo(new[] { person.Id }));
            repository.AcceptChanges(new[] { person.Id });

            world.Journeys.Clear();
            var join = new OrganizationSystem().TryJoinAtCurrentLocation(
                world,
                new StableId(person.Id),
                OrganizationType.Government);
            Assert.That(join.Success, Is.True, join.Message);
            var tasks = new TaskSystem(repository);
            var accepted = tasks.TryAccept(
                world,
                new StableId(person.Id),
                new StableId("task_definition.verify_households"));
            Assert.That(accepted.Success, Is.True, accepted.Message);
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
            tasks.ResolveDailyProgress(world);
            tasks.ResolveDailyProgress(world);
            tasks.ResolveDailyProgress(world);
            Assert.That(accepted.Task.Status, Is.EqualTo(TaskStatus.Completed));
            Assert.That(
                repository.GetChangedPersonIds(),
                Is.EqualTo(new[] { person.Id }));
        }

        [Test]
        public void PersonRepository_InjectedSimulatorMatchesInlineSimulation()
        {
            var inline = PrototypeWorldFactory.Create184World(184);
            var accessed = PrototypeWorldFactory.Create184World(184);
            new TravelSystem().StartJourney(
                inline,
                new StableId("person.liu_bei"),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhongshan"),
                TravelMode.Foot);
            var repository = new WorldStatePersonRepository(accessed);
            new TravelSystem(repository).StartJourney(
                accessed,
                new StableId("person.liu_bei"),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhongshan"),
                TravelMode.Foot);

            new WorldSimulator(184).AdvanceDays(inline, 5);
            new WorldSimulator(184, null, repository).AdvanceDays(accessed, 5);

            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            Assert.That(
                repository.GetChangedPersonIds(),
                Does.Contain("person.liu_bei"));
        }

        [Test]
        public void PopulationStore_IncrementalCheckpointRewritesOnlyDirtyPartition()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = VillagePrototypeFactory.Create(200, 21_001);
                var store = new PartitionedPopulationStore(root);
                var first = PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.incremental",
                    8,
                    1);
                var person = world.People[17];
                var session = new PopulationResidencySession(store);
                _ = session.Promote(person.Id);
                var repository = new WorldStatePersonRepository(world);
                repository.GetRequiredForUpdate(person.Id).Wealth += 777;
                var result = new PopulationPersonCheckpointCoordinator(
                    store, session).CommitChangedPeople(
                        world, repository, 2);

                Assert.That(result.Manifest.StorageRevision, Is.EqualTo(2));
                Assert.That(result.RewrittenPartitionCount, Is.EqualTo(1));
                Assert.That(result.CommittedPersonIds, Is.EqualTo(new[] { person.Id }));
                Assert.That(repository.GetChangedPersonIds(), Is.Empty);
                Assert.That(world.PopulationStorage.StorageRevision, Is.EqualTo(2));
                Assert.That(
                    result.Manifest.PermanentPersonCount,
                    Is.EqualTo(first.PermanentPersonCount));
                Assert.That(
                    result.Manifest.LivingPersonCount,
                    Is.EqualTo(first.LivingPersonCount));
                var changedPartitions = 0;
                for (var i = 0; i < first.Partitions.Count; i++)
                {
                    var changed = first.Partitions[i].CoreRelativePath !=
                                      result.Manifest.Partitions[i].CoreRelativePath ||
                                  first.Partitions[i].DetailRelativePath !=
                                      result.Manifest.Partitions[i].DetailRelativePath;
                    if (changed)
                    {
                        changedPartitions++;
                    }
                    else
                    {
                        Assert.That(
                            result.Manifest.Partitions[i].CoreSha256,
                            Is.EqualTo(first.Partitions[i].CoreSha256));
                        Assert.That(
                            result.Manifest.Partitions[i].DetailSha256,
                            Is.EqualTo(first.Partitions[i].DetailSha256));
                    }
                }

                Assert.That(changedPartitions, Is.EqualTo(1));
                Assert.That(
                    Directory.GetFiles(
                        Path.Combine(
                            root,
                            "generations",
                            "generation-00000000000000000002"),
                        "*.bin").Length,
                    Is.EqualTo(2));
                Assert.That(store.TryReadCore(person.Id, out var core), Is.True);
                Assert.That(store.TryReadDetail(person.Id, out var detail), Is.True);
                Assert.That(core.Matches(detail), Is.True);
                Assert.That(detail.Wealth, Is.EqualTo(person.Wealth));
                PopulationStorageWorldAdapter.ValidateAttachedPackage(
                    world, store);
                session.DemoteUnchanged(person.Id);
                Assert.That(session.HotCount, Is.EqualTo(0));
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void PopulationStore_FailedIncrementalCheckpointKeepsPointerAndDirtySet()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = BuildMinimalWorld();
                var store = new PartitionedPopulationStore(root);
                PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.incremental_failure",
                    2,
                    1);
                var repository = new WorldStatePersonRepository(world);
                repository.GetRequiredForUpdate("person.liu_bei").Wealth++;
                var originalWorldRevision =
                    world.PopulationStorage.StorageRevision;
                var coordinator = new PopulationPersonCheckpointCoordinator(store);

                Assert.Throws<InvalidOperationException>(
                    () => coordinator.CommitChangedPeople(
                        world, repository, 1));

                Assert.That(store.OpenCurrent().StorageRevision, Is.EqualTo(1));
                Assert.That(
                    world.PopulationStorage.StorageRevision,
                    Is.EqualTo(originalWorldRevision));
                Assert.That(
                    repository.GetChangedPersonIds(),
                    Is.EqualTo(new[] { "person.liu_bei" }));
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void PopulationStore_IncrementalCheckpointRejectsUnknownAndDuplicatePeople()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = BuildMinimalWorld();
                var store = new PartitionedPopulationStore(root);
                PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.incremental_validation",
                    2,
                    1);
                var unknown = new PopulationIncrementalCheckpoint
                {
                    StorageRevision = 2,
                    ChangedPeople =
                    {
                        new PersonState
                        {
                            Id = "person.unknown_incremental",
                            DisplayName = "Unknown",
                            LocationId = "location.zhuo"
                        }
                    }
                };
                Assert.Throws<InvalidOperationException>(
                    () => store.CommitIncrementalCheckpoint(unknown));
                Assert.That(store.OpenCurrent().StorageRevision, Is.EqualTo(1));

                var duplicate = new PopulationIncrementalCheckpoint
                {
                    StorageRevision = 2
                };
                duplicate.ChangedPeople.Add(world.People[0]);
                duplicate.ChangedPeople.Add(world.People[0]);
                Assert.Throws<InvalidOperationException>(
                    () => store.CommitIncrementalCheckpoint(duplicate));
                Assert.That(store.OpenCurrent().StorageRevision, Is.EqualTo(1));
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void PersonRepository_TracksAddedPeopleSeparatelyFromUpdates()
        {
            var world = BuildMinimalWorld();
            var repository = new WorldStatePersonRepository(world);
            var newcomer = BuildIncrementalNewPerson();

            repository.Add(newcomer);
            repository.GetRequiredForUpdate(newcomer.Id).Wealth = 25;

            Assert.That(world.People, Does.Contain(newcomer));
            Assert.That(
                repository.GetAddedPersonIds(),
                Is.EqualTo(new[] { newcomer.Id }));
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
            Assert.Throws<InvalidOperationException>(
                () => repository.Add(newcomer));
        }

        [Test]
        public void LifeSimulation_InjectedRepositoryPreservesResultsAndTracksBirths()
        {
            var inline = VillagePrototypeFactory.Create();
            var accessed = VillagePrototypeFactory.Create();
            var repository = new WorldStatePersonRepository(accessed);
            var inlineLife = new LifeSimulationSystem(inline.MasterSeed);
            var accessedLife = new LifeSimulationSystem(
                accessed.MasterSeed, repository);

            for (var month = 1; month <= 12; month++)
            {
                inline.AbsoluteDay = month * 30L;
                accessed.AbsoluteDay = month * 30L;
                inlineLife.ResolveMonthly(inline);
                accessedLife.ResolveMonthly(accessed);
            }

            Assert.That(
                WorldSnapshotSerializer.Serialize(accessed),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(inline)));
            Assert.That(repository.GetAddedPersonIds(), Is.Not.Empty);
            Assert.That(
                repository.GetAddedPersonIds().Count,
                Is.EqualTo(accessed.People.Count - 300));
            Assert.That(repository.GetChangedPersonIds(), Is.Not.Empty);
        }

        [Test]
        public void PopulationStore_IncrementalCheckpointAddsPermanentPerson()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = BuildMinimalWorld();
                var store = new PartitionedPopulationStore(root);
                var first = PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.incremental_addition",
                    4,
                    1);
                var repository = new WorldStatePersonRepository(world);
                var newcomer = BuildIncrementalNewPerson();
                repository.Add(newcomer);

                Assert.Throws<InvalidOperationException>(
                    () => WorldSnapshotSerializer.Serialize(world));

                var session = new PopulationResidencySession(store);
                var result = new PopulationPersonCheckpointCoordinator(
                    store, session).CommitPendingPeople(
                        world, repository, 2);

                Assert.That(result.RewrittenPartitionCount, Is.EqualTo(1));
                Assert.That(result.AddedPersonIds, Is.EqualTo(new[] { newcomer.Id }));
                Assert.That(result.ChangedPersonIds, Is.Empty);
                Assert.That(result.CommittedPersonIds, Is.EqualTo(new[] { newcomer.Id }));
                Assert.That(repository.GetAddedPersonIds(), Is.Empty);
                Assert.That(
                    result.Manifest.PermanentPersonCount,
                    Is.EqualTo(first.PermanentPersonCount + 1));
                Assert.That(
                    result.Manifest.LivingPersonCount,
                    Is.EqualTo(first.LivingPersonCount + 1));
                Assert.That(
                    result.Manifest.DetailExtensionCount,
                    Is.EqualTo(first.DetailExtensionCount + 1));
                Assert.That(store.TryReadCore(newcomer.Id, out var core), Is.True);
                Assert.That(store.TryReadDetail(newcomer.Id, out var detail), Is.True);
                Assert.That(core.Matches(detail), Is.True);
                Assert.That(
                    WorldSnapshotSerializer.Deserialize(
                        WorldSnapshotSerializer.Serialize(world)).People.Count,
                    Is.EqualTo(world.People.Count));
                Assert.That(session.Promote(newcomer.Id).Id, Is.EqualTo(newcomer.Id));
                session.DemoteUnchanged(newcomer.Id);
                Assert.That(session.HotCount, Is.EqualTo(0));
                PopulationStorageWorldAdapter.ValidateAttachedPackage(world, store);
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void PopulationStore_FailedAdditionKeepsPointerAndPendingPerson()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = BuildMinimalWorld();
                var store = new PartitionedPopulationStore(root);
                PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.incremental_addition_failure",
                    2,
                    1);
                var repository = new WorldStatePersonRepository(world);
                var newcomer = BuildIncrementalNewPerson();
                repository.Add(newcomer);
                var originalWorldRevision =
                    world.PopulationStorage.StorageRevision;

                Assert.Throws<InvalidOperationException>(
                    () => new PopulationPersonCheckpointCoordinator(store)
                        .CommitPendingPeople(world, repository, 1));

                Assert.That(store.OpenCurrent().StorageRevision, Is.EqualTo(1));
                Assert.That(
                    world.PopulationStorage.StorageRevision,
                    Is.EqualTo(originalWorldRevision));
                Assert.That(
                    repository.GetAddedPersonIds(),
                    Is.EqualTo(new[] { newcomer.Id }));
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void PopulationStore_IncrementalCheckpointRejectsExistingPersonAsAddition()
        {
            var root = NewPopulationStoreTestRoot();
            try
            {
                var world = BuildMinimalWorld();
                var store = new PartitionedPopulationStore(root);
                PopulationStorageWorldAdapter.CommitInlineWorld(
                    world,
                    store,
                    "population.test.incremental_existing_addition",
                    2,
                    1);
                var checkpoint = new PopulationIncrementalCheckpoint
                {
                    StorageRevision = 2
                };
                checkpoint.AddedPeople.Add(world.People[0]);

                Assert.Throws<InvalidOperationException>(
                    () => store.CommitIncrementalCheckpoint(checkpoint));
                Assert.That(store.OpenCurrent().StorageRevision, Is.EqualTo(1));

                checkpoint.ChangedPeople.Add(world.People[0]);
                Assert.Throws<InvalidOperationException>(
                    () => store.CommitIncrementalCheckpoint(checkpoint));
                Assert.That(store.OpenCurrent().StorageRevision, Is.EqualTo(1));
            }
            finally
            {
                DeletePopulationStoreTestRoot(root);
            }
        }

        [Test]
        public void EquipmentManufacturing_OrganizationWorkshopSettlesBalancedBatch()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var system = new ProcessingProductionSystem();
            var iron = world.ProductBatches.Find(item =>
                item.ProductDefinitionId ==
                CoreProductionContent.IronMaterialProductId);
            var timber = world.ProductBatches.Find(item =>
                item.ProductDefinitionId ==
                CoreProductionContent.TimberMaterialProductId);
            var ironBefore = iron.Quantity;
            var timberBefore = timber.Quantity;

            var order = system.CreateOrganizationOrder(
                world,
                CoreProductionContent.ForgeLongSpearRecipeId,
                CoreProductionContent.BlacksmithingMethodId,
                "organization.zhongshan_merchants",
                MilitaryEquipmentRepairSystem.PrototypeWorkshopSiteId,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                "person.su_shuang",
                ProductionControlMode.WorkOrder,
                2);

            Assert.That(order.FinishDay, Is.EqualTo(world.AbsoluteDay + 10));
            Assert.That(iron.ReservedQuantity, Is.EqualTo(4));
            Assert.That(timber.ReservedQuantity, Is.EqualTo(6));
            world.AbsoluteDay = order.FinishDay - 1;
            system.ResolveDueOrders(world);
            Assert.That(order.Status, Is.EqualTo(ProductionOrderStatus.Active));

            world.AbsoluteDay = order.FinishDay;
            system.ResolveDueOrders(world);

            Assert.That(order.Status, Is.EqualTo(ProductionOrderStatus.Completed));
            Assert.That(iron.Quantity, Is.EqualTo(ironBefore - 4));
            Assert.That(timber.Quantity, Is.EqualTo(timberBefore - 6));
            Assert.That(order.OutputBatchIds.Count, Is.EqualTo(1));
            var output = world.ProductBatches.Find(item =>
                item.Id == order.OutputBatchIds[0]);
            Assert.That(output.ProductDefinitionId,
                Is.EqualTo(CoreProductionContent.LongSpearProductId));
            Assert.That(output.Quantity, Is.EqualTo(2));
            Assert.That(output.InventoryContainerId,
                Is.EqualTo(
                    MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId));
            Assert.That(output.SourceWorkOrderId, Is.EqualTo(order.Id));
            world.Validate();
        }

        [Test]
        public void EquipmentManufacturing_InvalidManagerDoesNotReserveMaterial()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new ProcessingProductionSystem().CreateOrganizationOrder(
                    world,
                    CoreProductionContent.ForgeLongSpearRecipeId,
                    CoreProductionContent.BlacksmithingMethodId,
                    "organization.zhongshan_merchants",
                    MilitaryEquipmentRepairSystem.PrototypeWorkshopSiteId,
                    MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                    "person.zhang_shiping",
                    ProductionControlMode.DelegatedPolicy,
                    1));

            Assert.That(
                WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void EquipmentManufacturing_WorkshopBatchCanEnterProcurementJourney()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var processing = new ProcessingProductionSystem();
            var workOrder = processing.CreateOrganizationOrder(
                world,
                CoreProductionContent.ForgeLongSpearRecipeId,
                CoreProductionContent.BlacksmithingMethodId,
                "organization.zhongshan_merchants",
                MilitaryEquipmentRepairSystem.PrototypeWorkshopSiteId,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                "person.su_shuang",
                ProductionControlMode.TargetInstruction,
                5);
            world.AbsoluteDay = workOrder.FinishDay;
            processing.ResolveDueOrders(world);
            var caravan = world.InventoryContainers.Find(item =>
                item.Id == MilitaryProcurementSystem.PrototypeContainerId);
            caravan.CapacityWeight = 200;
            new ArmySystem().StartMarch(
                world,
                new StableId("person.zou_jing"),
                new StableId("army.youzhou_reinforcement"),
                new StableId("route.zhongshan_anping"),
                new StableId("location.anping"));

            var order = new MilitaryProcurementSystem().CreateOrderAndDispatch(
                world,
                new StableId("person.zou_jing"),
                new StableId("person.zhang_shiping"),
                new StableId("army.youzhou_reinforcement"),
                new StableId(MilitaryEquipmentSystem.LongSpearId),
                5,
                20,
                new StableId("route.zhongshan_anping"),
                new StableId("location.anping"));

            Assert.That(order.InventoryContainerId,
                Is.EqualTo(
                    MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId));
            Assert.That(order.SourceBatchId,
                Is.EqualTo(workOrder.OutputBatchIds[0]));
            Assert.That(order.Status,
                Is.EqualTo(MilitaryProcurementStatus.InTransit));
            world.Validate();
        }

        [Test]
        public void EquipmentRepair_ReservesMaterialAndReturnsDamagedStock()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var issue = world.MilitaryEquipmentIssues.Find(item =>
                item.ArmyId == "army.youzhou_reinforcement" &&
                item.EquipmentDefinitionId ==
                    MilitaryEquipmentSystem.LongSpearId);
            Assert.That(issue, Is.Not.Null);
            var stock = world.MilitaryArmoryStocks.Find(item =>
                item.ArmyId == issue.ArmyId &&
                item.EquipmentDefinitionId == issue.EquipmentDefinitionId);
            world.MilitaryEquipmentIssues.Remove(issue);
            stock.DamagedQuantity++;
            world.MilitaryEquipmentTransactions.Add(
                new MilitaryEquipmentTransactionState
                {
                    Id = "equipment_transaction.test.damage_for_repair",
                    Day = world.AbsoluteDay,
                    Type = MilitaryEquipmentTransactionType.Damage,
                    EquipmentDefinitionId = issue.EquipmentDefinitionId,
                    Quantity = 1,
                    FromArmyId = issue.ArmyId,
                    MilitaryServiceId = issue.MilitaryServiceId,
                    Summary = "Test equipment recovered damaged."
                });
            world.Validate();
            var iron = world.ProductBatches.Find(item =>
                item.ProductDefinitionId ==
                CoreProductionContent.IronMaterialProductId);
            var ironBefore = iron.Quantity;
            var availableBefore = stock.AvailableQuantity;
            var system = new MilitaryEquipmentRepairSystem();

            var order = system.CreateOrder(
                world,
                issue.ArmyId,
                issue.EquipmentDefinitionId,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopSiteId,
                "person.su_shuang",
                ProductionControlMode.DirectAssignment,
                1);

            Assert.That(stock.ReservedDamagedQuantity, Is.EqualTo(1));
            Assert.That(iron.ReservedQuantity, Is.EqualTo(1));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var loadedOrder = loaded.MilitaryEquipmentRepairOrders.Find(
                item => item.Id == order.Id);
            loaded.AbsoluteDay = loadedOrder.FinishDay;
            system.ResolveDueOrders(loaded);
            var loadedStock = loaded.MilitaryArmoryStocks.Find(item =>
                item.Id == stock.Id);
            var loadedIron = loaded.ProductBatches.Find(item =>
                item.Id == iron.Id);

            Assert.That(loadedOrder.Status,
                Is.EqualTo(ProductionOrderStatus.Completed));
            Assert.That(loadedStock.DamagedQuantity, Is.EqualTo(0));
            Assert.That(loadedStock.ReservedDamagedQuantity, Is.EqualTo(0));
            Assert.That(loadedStock.AvailableQuantity,
                Is.EqualTo(availableBefore + 1));
            Assert.That(loadedIron.Quantity, Is.EqualTo(ironBefore - 1));
            Assert.That(loadedIron.ReservedQuantity, Is.EqualTo(0));
            Assert.That(loaded.MilitaryEquipmentTransactions.Exists(item =>
                item.SourceRepairOrderId == loadedOrder.Id &&
                item.Type == MilitaryEquipmentTransactionType.Repair),
                Is.True);
            loaded.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionFourteenWithoutFabricatingWorkshopOrders()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 14");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.ProductionSites, Is.Empty);
            Assert.That(loaded.MilitaryEquipmentRepairOrders, Is.Empty);
            Assert.That(loaded.ProcessingWorkOrders, Is.Empty);
            Assert.That(loaded.MilitaryEquipmentDefinitions.TrueForAll(item =>
                !string.IsNullOrEmpty(
                    item.RepairMaterialProductDefinitionId)), Is.True);
            loaded.Validate();
        }

        [Test]
        public void ResourceExtraction_ReservesThenSettlesTraceableBatch()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var repository = new WorldStatePersonRepository(world);
            var system = new UpstreamResourceProductionSystem(
                null, repository);
            var resource = world.ResourceBodies.Find(item =>
                item.Id == UpstreamResourceProductionSystem.PrototypeIronBodyId);
            var remainingBefore = resource.RemainingQuantity;

            var order = system.CreateOrder(
                world,
                resource.Id,
                UpstreamResourceProductionSystem.PrototypeIronMineSiteId,
                "person.su_shuang",
                new[] { "person.su_shuang" },
                ProductionControlMode.WorkOrder,
                12);

            Assert.That(resource.ReservedQuantity, Is.EqualTo(12));
            Assert.That(resource.RemainingQuantity, Is.EqualTo(remainingBefore));
            Assert.That(repository.GetChangedPersonIds(), Is.Empty);
            Assert.That(order.FinishDay, Is.EqualTo(world.AbsoluteDay + 15));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var loadedOrder = loaded.ResourceExtractionOrders.Find(item =>
                item.Id == order.Id);
            var loadedResource = loaded.ResourceBodies.Find(item =>
                item.Id == resource.Id);
            loaded.AbsoluteDay = loadedOrder.FinishDay - 1;
            system.ResolveDueOrders(loaded);
            Assert.That(loadedOrder.Status,
                Is.EqualTo(ProductionOrderStatus.Active));

            loaded.AbsoluteDay = loadedOrder.FinishDay;
            system.ResolveDueOrders(loaded);

            Assert.That(loadedOrder.Status,
                Is.EqualTo(ProductionOrderStatus.Completed));
            Assert.That(loadedResource.RemainingQuantity,
                Is.EqualTo(remainingBefore - 12));
            Assert.That(loadedResource.ReservedQuantity, Is.EqualTo(0));
            var output = loaded.ProductBatches.Find(item =>
                item.Id == loadedOrder.OutputBatchId);
            Assert.That(output.ProductDefinitionId,
                Is.EqualTo(CoreProductionContent.IronOreProductId));
            Assert.That(output.Quantity, Is.EqualTo(12));
            Assert.That(output.SourceWorkOrderId, Is.EqualTo(loadedOrder.Id));
            Assert.That(loaded.ResourceExtractionLedgerEntries.FindAll(item =>
                item.ResourceExtractionOrderId == loadedOrder.Id).Count,
                Is.EqualTo(2));
            Assert.That(loaded.InventoryTransactions.Exists(item =>
                item.SourceResourceExtractionOrderId == loadedOrder.Id &&
                item.Type ==
                    InventoryTransactionType.ResourceExtractionSettled),
                Is.True);
            loaded.Validate();
        }

        [Test]
        public void ResourceExtraction_InvalidWorkersDoNotMutateWorld()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new UpstreamResourceProductionSystem().CreateOrder(
                    world,
                    UpstreamResourceProductionSystem.PrototypeIronBodyId,
                    UpstreamResourceProductionSystem.PrototypeIronMineSiteId,
                    "person.su_shuang",
                    new[] { "person.su_shuang", "person.su_shuang" },
                    ProductionControlMode.DirectAssignment,
                    5));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void HerbalSupply_FamilyExtractionAndProcessingAreTraceable()
        {
            var world = VillagePrototypeFactory.Create(200, 25_230_001UL);
            var village = world.Villages[0];
            var resource = world.ResourceBodies.Find(item =>
                item.Id == HerbalMedicineSupplySystem.ResourceBodyId(
                    village.Id));
            var storage = world.VillageFacilities.Find(item =>
                item.CapabilityTags.Contains(
                    CoreProductionContent.HerbGatheringFacilityTag));
            var family = world.Families.Find(item =>
                item.Id == storage.OwnerFamilyId);
            var worker = world.People.Find(item =>
                item.FamilyId == family.Id && item.IsAlive &&
                item.LaborCapacityBasisPoints > 0);
            var extraction = new UpstreamResourceProductionSystem();
            var remainingBefore = resource.RemainingQuantity;

            var extractionOrder = extraction.CreateFamilyOrder(
                world,
                resource.Id,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                new[] { worker.Id },
                ProductionControlMode.WorkOrder,
                5);
            world.AbsoluteDay = extractionOrder.FinishDay;
            extraction.ResolveDueOrders(world);

            var raw = world.ProductBatches.Find(item =>
                item.SourceWorkOrderId == extractionOrder.Id);
            Assert.That(extractionOrder.Status,
                Is.EqualTo(ProductionOrderStatus.Completed));
            Assert.That(resource.RemainingQuantity,
                Is.EqualTo(remainingBefore - 5));
            Assert.That(raw.OwnerFamilyId, Is.EqualTo(family.Id));
            Assert.That(raw.StorageFacilityId, Is.EqualTo(storage.Id));
            Assert.That(raw.ProductDefinitionId,
                Is.EqualTo(CoreProductionContent.RawMedicinalPlantProductId));

            var processing = new ProcessingProductionSystem();
            var processingOrder = processing.CreateOrder(
                world,
                CoreProductionContent.DryMedicinalPlantsRecipeId,
                CoreProductionContent.HerbalDryingMethodId,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                ProductionControlMode.WorkOrder,
                5);
            world.AbsoluteDay = processingOrder.FinishDay;
            processing.ResolveDueOrders(world);

            var medicine = world.ProductBatches.Find(item =>
                item.SourceWorkOrderId == processingOrder.Id &&
                item.ProductDefinitionId == CoreProductionContent
                    .HerbalMedicineMaterialProductId);
            Assert.That(processingOrder.Status,
                Is.EqualTo(ProductionOrderStatus.Completed));
            Assert.That(raw.Quantity, Is.Zero);
            Assert.That(medicine.Quantity, Is.EqualTo(5));
            Assert.That(medicine.UnitWeight, Is.EqualTo(raw.UnitWeight));
            Assert.That(world.ResourceExtractionLedgerEntries.FindAll(item =>
                item.ResourceExtractionOrderId == extractionOrder.Id).Count,
                Is.EqualTo(2));
            world.Validate();
        }

        [Test]
        public void HerbalSupply_InvalidFacilityAndMixedOwnershipAreRejected()
        {
            var world = VillagePrototypeFactory.Create(200, 25_230_005UL);
            var village = world.Villages[0];
            var resource = world.ResourceBodies.Find(item =>
                item.Id == HerbalMedicineSupplySystem.ResourceBodyId(
                    village.Id));
            var storage = world.VillageFacilities.Find(item =>
                item.CapabilityTags.Contains(
                    CoreProductionContent.HerbGatheringFacilityTag));
            var family = world.Families.Find(item =>
                item.Id == storage.OwnerFamilyId);
            var worker = world.People.Find(item =>
                item.FamilyId == family.Id && item.IsAlive &&
                item.LaborCapacityBasisPoints > 0);
            storage.CapabilityTags.Remove(
                CoreProductionContent.HerbGatheringFacilityTag);
            var before = WorldSnapshotSerializer.Serialize(world);
            var extraction = new UpstreamResourceProductionSystem();

            Assert.Throws<InvalidOperationException>(() =>
                extraction.CreateFamilyOrder(
                    world,
                    resource.Id,
                    family.Id,
                    storage.Id,
                    family.HeadPersonId,
                    new[] { worker.Id },
                    ProductionControlMode.WorkOrder,
                    5));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));

            storage.CapabilityTags.Add(
                CoreProductionContent.HerbGatheringFacilityTag);
            storage.CapabilityTags.Sort(StringComparer.Ordinal);
            var order = extraction.CreateFamilyOrder(
                world,
                resource.Id,
                family.Id,
                storage.Id,
                family.HeadPersonId,
                new[] { worker.Id },
                ProductionControlMode.WorkOrder,
                5);
            order.OwnerOrganizationId =
                "organization.invalid_mixed_extraction_owner";
            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void FormalMarket_TransfersHerbalMedicineBetweenFamilies()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_230_002UL);
            var openingMedicine = world.ProductBatches.Find(item =>
                item.ProductDefinitionId == CoreProductionContent
                    .HerbalMedicineMaterialProductId);
            world.InventoryTransactions.RemoveAll(item =>
                item.Id == openingMedicine.SourceTransactionId);
            world.ProductBatches.Remove(openingMedicine);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var sellerStorage = world.VillageFacilities.Find(item =>
                item.CapabilityTags.Contains(
                    CoreProductionContent.HerbGatheringFacilityTag));
            var seller = world.Families.Find(item =>
                item.Id == sellerStorage.OwnerFamilyId);
            var clinic = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.Clinic);
            var physician = world.People.Find(item =>
                item.Id == clinic.ManagerPersonId);
            var buyer = world.Families.Find(item =>
                item.Id == physician.FamilyId);
            var buyerStorage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == buyer.Id);
            buyerStorage.Capacity += 100;
            buyer.Wealth = 10_000;
            new ProductInventorySystem(content).CreateFamilyOpeningBatch(
                world,
                seller.Id,
                sellerStorage.Id,
                seller.HeadPersonId,
                CoreProductionContent.HerbalMedicineMaterialProductId,
                5);
            var sellerWealth = seller.Wealth;
            var buyerWealth = buyer.Wealth;
            var market = new FormalCountyMarketSystem(content);
            market.CreateSellOrder(
                world,
                world.CountyGovernances[0].Id,
                seller.Id,
                sellerStorage.Id,
                CoreProductionContent.HerbalMedicineMaterialProductId,
                5,
                7,
                0,
                world.AbsoluteDay + 5);
            market.CreateBuyOrder(
                world,
                world.CountyGovernances[0].Id,
                buyer.Id,
                buyerStorage.Id,
                CoreProductionContent.HerbalMedicineMaterialProductId,
                5,
                9,
                0,
                world.AbsoluteDay + 5);

            market.ResolveDaily(world);

            var delivered = world.ProductBatches.Find(item =>
                item.OwnerFamilyId == buyer.Id &&
                item.StorageFacilityId == buyerStorage.Id &&
                item.ProductDefinitionId == CoreProductionContent
                    .HerbalMedicineMaterialProductId);
            Assert.That(delivered, Is.Not.Null);
            Assert.That(delivered.Quantity, Is.EqualTo(5));
            Assert.That(seller.Wealth, Is.EqualTo(sellerWealth + 35));
            Assert.That(buyer.Wealth, Is.EqualTo(buyerWealth - 35));
            Assert.That(world.InventoryTransactions.Exists(item =>
                item.Type == InventoryTransactionType.FoodMarketTransferred &&
                item.Lines.Exists(line =>
                    line.ProductDefinitionId == CoreProductionContent
                        .HerbalMedicineMaterialProductId)), Is.True);

            var patient = world.People.Find(item =>
                item.FamilyId == buyer.Id && item.Id != physician.Id);
            patient.BirthDay = -7_200;
            ResolveTwoNutritionDeficitMonths(world, patient.Id);
            var episode = world.NutritionConditionEpisodes.Find(item =>
                item.PersonId == patient.Id && item.EndDay == -1);
            var medical = new CivilianMedicalSystem(content);
            var diagnosis = medical.DiagnoseNutritionCondition(
                world, episode.Id, physician.Id, patient.Id);
            var treatment = medical.TreatNutritionCondition(
                world, diagnosis.MedicalCaseId, physician.Id, patient.Id);
            Assert.That(diagnosis.Success, Is.True);
            Assert.That(treatment.Success, Is.True);
            Assert.That(delivered.Quantity, Is.EqualTo(4));
            world.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionFortyFourWithoutFabricatingSupply()
        {
            var world = VillagePrototypeFactory.Create(200, 25_230_003UL);
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                world.VillageFacilities[i].CapabilityTags.Clear();
            }
            var resourceCount = world.ResourceBodies.Count;
            var extractionCount = world.ResourceExtractionOrders.Count;
            var processingCount = world.ProcessingWorkOrders.Count;
            var marketOrderCount = world.FormalMarketOrders.Count;
            var transactionCount = world.InventoryTransactions.Count;
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 44");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.ResourceBodies.Count,
                Is.EqualTo(resourceCount));
            Assert.That(loaded.ResourceExtractionOrders.Count,
                Is.EqualTo(extractionCount));
            Assert.That(loaded.ProcessingWorkOrders.Count,
                Is.EqualTo(processingCount));
            Assert.That(loaded.FormalMarketOrders.Count,
                Is.EqualTo(marketOrderCount));
            Assert.That(loaded.InventoryTransactions.Count,
                Is.EqualTo(transactionCount));
            Assert.That(loaded.VillageFacilities.TrueForAll(item =>
                item.CapabilityTags.Contains(
                    VillageFacilityTags.FromKind(item.Kind))), Is.True);
            loaded.Validate();
        }

        [Test]
        public void HerbalSupply_AutomaticLoopIsDeterministicAndRestocksLocally()
        {
            var content = LoadHanFoodProductionContent();
            var first = PrepareAutomaticHerbalSupplyWorld(
                content, 25_230_004UL);
            var second = PrepareAutomaticHerbalSupplyWorld(
                content, 25_230_004UL);

            new WorldSimulator(first.MasterSeed, content)
                .AdvanceDays(first, 45);
            new WorldSimulator(second.MasterSeed, content)
                .AdvanceDays(second, 45);

            var physician = first.People.Find(item =>
                item.VillageOccupation == VillageOccupation.Physician);
            Assert.That(first.FormalMarketTrades.Exists(item =>
                item.ProductDefinitionId == CoreProductionContent
                    .HerbalMedicineMaterialProductId), Is.True);
            Assert.That(first.ProductBatches.Exists(item =>
                item.OwnerFamilyId == physician.FamilyId &&
                item.ProductDefinitionId == CoreProductionContent
                    .HerbalMedicineMaterialProductId &&
                item.Quantity > 0), Is.True);
            Assert.That(first.ResourceExtractionOrders.TrueForAll(order =>
                order.ResourceBodyId != HerbalMedicineSupplySystem
                    .ResourceBodyId(first.Villages[0].Id) ||
                !string.IsNullOrEmpty(order.OwnerFamilyId)), Is.True);
            Assert.That(
                WorldSnapshotSerializer.Serialize(first, content),
                Is.EqualTo(
                    WorldSnapshotSerializer.Serialize(second, content)));
            first.Validate();
        }

        [Test]
        public void UpstreamProduction_ExtractsCarbonizesSmeltsAndMakesSpears()
        {
            var world = PrototypeWorldFactory.Create184World(184);

            ExecutePrototypeUpstreamChain(world);

            var smelt = world.ProcessingWorkOrders.Find(item =>
                item.RecipeDefinitionId ==
                    CoreProductionContent.SmeltBloomeryIronRecipeId);
            var iron = world.ProductBatches.Find(item =>
                item.SourceWorkOrderId == smelt.Id &&
                item.ProductDefinitionId ==
                    CoreProductionContent.IronMaterialProductId);
            var slag = world.ProductBatches.Find(item =>
                item.SourceWorkOrderId == smelt.Id &&
                item.ProductDefinitionId == CoreProductionContent.SlagProductId);
            var spearOrder = world.ProcessingWorkOrders.Find(item =>
                item.RecipeDefinitionId ==
                    CoreProductionContent.ForgeLongSpearRecipeId);
            var spears = world.ProductBatches.Find(item =>
                item.SourceWorkOrderId == spearOrder.Id);

            Assert.That(iron.Quantity, Is.EqualTo(4));
            Assert.That(slag.Quantity, Is.EqualTo(8));
            Assert.That(spears.ProductDefinitionId,
                Is.EqualTo(CoreProductionContent.LongSpearProductId));
            Assert.That(spears.Quantity, Is.EqualTo(2));
            world.Validate();
        }

        [Test]
        public void UpstreamProduction_SameCommandsProduceSameSnapshot()
        {
            var first = PrototypeWorldFactory.Create184World(184);
            var second = PrototypeWorldFactory.Create184World(184);

            ExecutePrototypeUpstreamChain(first);
            ExecutePrototypeUpstreamChain(second);

            Assert.That(WorldSnapshotSerializer.Serialize(first),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(second)));
        }

        [Test]
        public void Snapshot_MigratesVersionFifteenWithoutFabricatingResources()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 15");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.ResourceBodies, Is.Empty);
            Assert.That(loaded.ResourceExtractionOrders, Is.Empty);
            Assert.That(loaded.ResourceExtractionLedgerEntries, Is.Empty);
            loaded.Validate();
        }

        [Test]
        public void LivestockProduction_PrototypeHasTraceableFlockAndFacilities()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var flock = world.ProductBatches.Find(item =>
                item.Id == LivestockProductionSystem.PrototypeOpeningFlockBatchId);

            Assert.That(flock, Is.Not.Null);
            Assert.That(flock.ProductDefinitionId,
                Is.EqualTo(CoreProductionContent.LiveSheepProductId));
            Assert.That(flock.Quantity, Is.EqualTo(30));
            Assert.That(flock.UnitWeight, Is.EqualTo(10));
            Assert.That(world.ResourceBodies.Exists(item =>
                item.Id ==
                    UpstreamResourceProductionSystem.PrototypePastureForageBodyId),
                Is.True);
            Assert.That(world.ResourceBodies.Exists(item =>
                item.Id ==
                    UpstreamResourceProductionSystem.PrototypeTanningBarkBodyId),
                Is.True);
            Assert.That(world.ProductionSites.Exists(item =>
                item.Id == LivestockProductionSystem.PrototypePastureSiteId),
                Is.True);
            Assert.That(world.InventoryTransactions.Exists(item =>
                item.Id == flock.SourceTransactionId &&
                item.Type == InventoryTransactionType.OpeningBalance), Is.True);
            world.Validate();
        }

        [Test]
        public void LivestockProduction_BreedsSlaughtersProcessesAndMakesEquipment()
        {
            var world = PrototypeWorldFactory.Create184World(184);

            ExecutePrototypeLivestockChain(world);

            Assert.That(QuantityFromRecipe(
                    world,
                    CoreProductionContent.BreedSheepRecipeId,
                    CoreProductionContent.LiveSheepProductId),
                Is.EqualTo(2));
            Assert.That(QuantityFromRecipe(
                    world,
                    CoreProductionContent.SlaughterSheepRecipeId,
                    CoreProductionContent.FreshMuttonProductId),
                Is.EqualTo(10));
            Assert.That(QuantityFromRecipe(
                    world,
                    CoreProductionContent.SlaughterSheepRecipeId,
                    CoreProductionContent.RawHideProductId),
                Is.EqualTo(4));
            Assert.That(QuantityFromRecipe(
                    world,
                    CoreProductionContent.SlaughterSheepRecipeId,
                    CoreProductionContent.RawHornProductId),
                Is.EqualTo(2));
            Assert.That(QuantityFromRecipe(
                    world,
                    CoreProductionContent.VegetableTanHideRecipeId,
                    CoreProductionContent.LeatherMaterialProductId),
                Is.EqualTo(4));
            Assert.That(QuantityFromRecipe(
                    world,
                    CoreProductionContent.FinishHornRecipeId,
                    CoreProductionContent.HornMaterialProductId),
                Is.EqualTo(1));
            Assert.That(QuantityFromRecipe(
                    world,
                    CoreProductionContent.MakeWoodenShieldRecipeId,
                    CoreProductionContent.WoodenShieldProductId),
                Is.EqualTo(1));
            Assert.That(QuantityFromRecipe(
                    world,
                    CoreProductionContent.MakeHornBowRecipeId,
                    CoreProductionContent.HornBowProductId),
                Is.EqualTo(1));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            loaded.Validate();
        }

        [Test]
        public void LivestockProduction_InvalidManagerDoesNotMutateWorld()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new LivestockProductionSystem().CreateSlaughterOrder(
                    world,
                    "person.zhang_shiping",
                    ProductionControlMode.DirectAssignment,
                    1));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void LivestockProduction_SameCommandsProduceSameSnapshot()
        {
            var first = PrototypeWorldFactory.Create184World(184);
            var second = PrototypeWorldFactory.Create184World(184);

            ExecutePrototypeLivestockChain(first);
            ExecutePrototypeLivestockChain(second);

            Assert.That(WorldSnapshotSerializer.Serialize(first),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(second)));
        }

        [Test]
        public void Snapshot_MigratesVersionSixteenWithoutFabricatingLivestock()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            RemoveM23P3Prototype(world);
            world.Validate();
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 16");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.ProductBatches.Exists(item =>
                item.ProductDefinitionId ==
                    CoreProductionContent.LiveSheepProductId), Is.False);
            Assert.That(loaded.ResourceBodies.Exists(item =>
                item.OutputProductDefinitionId ==
                    CoreProductionContent.PastureFodderProductId), Is.False);
            Assert.That(loaded.ProductionSites.Exists(item =>
                item.Id == LivestockProductionSystem.PrototypePastureSiteId),
                Is.False);
            loaded.Validate();
        }

        [Test]
        public void ProductQuality_CoreProductsAndMethodsDeclareOpenContracts()
        {
            var content = ProductionContentRegistry.CreateCore();
            var sword = content.GetProduct(
                CoreProductionContent.RingSwordProductId);
            var blacksmithing = content.GetMethod(
                CoreProductionContent.BlacksmithingMethodId);

            Assert.That(content.QualityDimensionCount, Is.EqualTo(9));
            Assert.That(sword.QualityDimensionIds, Is.EqualTo(new[]
            {
                CoreProductionContent.DurabilityQualityDimensionId,
                CoreProductionContent.WorkmanshipQualityDimensionId
            }));
            Assert.That(blacksmithing.PracticeSkillDefinitionId,
                Is.EqualTo(CoreSkillIds.Metalworking));
            Assert.That(blacksmithing.PracticeDifficultyBasisPoints,
                Is.GreaterThan(10_000));
            Assert.That(blacksmithing.QualityDimensionModifiers.Count,
                Is.EqualTo(2));
        }

        [Test]
        public void ArtisanPractice_RealOrderGrowsTrackedSkillAndShapesQuality()
        {
            var lowWorld = PrototypeWorldFactory.Create184World(184);
            var highWorld = PrototypeWorldFactory.Create184World(184);
            var lowManager = lowWorld.People.Find(item =>
                item.Id == "person.su_shuang");
            var highManager = highWorld.People.Find(item =>
                item.Id == "person.su_shuang");
            lowManager.ProfessionalSkills.Craft = 2_000;
            highManager.ProfessionalSkills.Craft = 8_000;
            var lowSystem = new ProcessingProductionSystem();
            var repository = new WorldStatePersonRepository(highWorld);
            var highSystem = new ProcessingProductionSystem(null, repository);

            var lowOrder = CreatePrototypeSpearOrder(lowWorld, lowSystem);
            var highOrder = CreatePrototypeSpearOrder(highWorld, highSystem);
            Assert.That(lowOrder.ManagerSkillBasisPointsAtStart,
                Is.EqualTo(2_000));
            Assert.That(highOrder.ManagerSkillBasisPointsAtStart,
                Is.EqualTo(8_000));
            Assert.That(highWorld.ProductionPracticeLedgerEntries, Is.Empty);
            Assert.That(SkillMasteryAccess.Get(
                    highManager, CoreSkillIds.Metalworking),
                Is.EqualTo(0));
            lowWorld.AbsoluteDay = lowOrder.FinishDay;
            highWorld.AbsoluteDay = highOrder.FinishDay;
            lowSystem.ResolveDueOrders(lowWorld);
            highSystem.ResolveDueOrders(highWorld);

            var lowOutput = lowWorld.ProductBatches.Find(item =>
                item.SourceWorkOrderId == lowOrder.Id);
            var highOutput = highWorld.ProductBatches.Find(item =>
                item.SourceWorkOrderId == highOrder.Id);
            Assert.That(highOutput.QualityBasisPoints,
                Is.GreaterThan(lowOutput.QualityBasisPoints));
            Assert.That(highOutput.QualityDimensions[0].ValueBasisPoints,
                Is.Not.EqualTo(
                    highOutput.QualityDimensions[1].ValueBasisPoints));
            Assert.That(highOrder.PracticeGainBasisPoints,
                Is.GreaterThan(0));
            Assert.That(highWorld.ProductionPracticeLedgerEntries.Count,
                Is.EqualTo(1));
            Assert.That(repository.GetChangedPersonIds(),
                Does.Contain(highManager.Id));
            Assert.That(SkillMasteryAccess.Get(
                    highManager, CoreSkillIds.Metalworking),
                Is.GreaterThan(8_000));
            highWorld.Validate();
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(highWorld));
            Assert.That(loaded.ProductionPracticeLedgerEntries.Count,
                Is.EqualTo(1));
            loaded.Validate();
        }

        [Test]
        public void ArtisanPractice_OrderQualityUsesSkillSnapshotAtCreation()
        {
            var baseline = PrototypeWorldFactory.Create184World(184);
            var changed = PrototypeWorldFactory.Create184World(184);
            var baselineManager = baseline.People.Find(item =>
                item.Id == "person.su_shuang");
            var changedManager = changed.People.Find(item =>
                item.Id == "person.su_shuang");
            baselineManager.ProfessionalSkills.Craft = 7_000;
            changedManager.ProfessionalSkills.Craft = 7_000;
            var baselineSystem = new ProcessingProductionSystem();
            var changedSystem = new ProcessingProductionSystem();
            var baselineOrder = CreatePrototypeSpearOrder(
                baseline, baselineSystem);
            var changedOrder = CreatePrototypeSpearOrder(
                changed, changedSystem);
            changedManager.ProfessionalSkills.Craft = 1_000;
            baseline.AbsoluteDay = baselineOrder.FinishDay;
            changed.AbsoluteDay = changedOrder.FinishDay;
            baselineSystem.ResolveDueOrders(baseline);
            changedSystem.ResolveDueOrders(changed);

            var baselineOutput = baseline.ProductBatches.Find(item =>
                item.SourceWorkOrderId == baselineOrder.Id);
            var changedOutput = changed.ProductBatches.Find(item =>
                item.SourceWorkOrderId == changedOrder.Id);
            Assert.That(changedOutput.QualityBasisPoints,
                Is.EqualTo(baselineOutput.QualityBasisPoints));
            Assert.That(changedOutput.QualityDimensions.Count,
                Is.EqualTo(baselineOutput.QualityDimensions.Count));
            for (var i = 0; i < changedOutput.QualityDimensions.Count; i++)
            {
                Assert.That(
                    changedOutput.QualityDimensions[i].QualityDimensionId,
                    Is.EqualTo(baselineOutput.QualityDimensions[i]
                        .QualityDimensionId));
                Assert.That(
                    changedOutput.QualityDimensions[i].ValueBasisPoints,
                    Is.EqualTo(baselineOutput.QualityDimensions[i]
                        .ValueBasisPoints));
            }

            Assert.That(
                changed.ProductionPracticeLedgerEntries[0]
                    .MasteryBeforeBasisPoints,
                Is.EqualTo(1_000));
        }

        [Test]
        public void ProductQuality_ValidationRejectsTamperedSummary()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            world.ProductBatches[0].QualityBasisPoints--;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void ArtisanPractice_ValidationRejectsTamperedLedgerQuality()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var processing = new ProcessingProductionSystem();
            var order = CreatePrototypeSpearOrder(world, processing);
            world.AbsoluteDay = order.FinishDay;
            processing.ResolveDueOrders(world);
            world.ProductionPracticeLedgerEntries[0]
                .OutputQualityBasisPoints--;

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void Snapshot_MigratesVersionSeventeenWithoutInventingPractice()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var summaries = world.ProductBatches.ConvertAll(item =>
                item.QualityBasisPoints);
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 17");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.ProductionPracticeLedgerEntries, Is.Empty);
            Assert.That(loaded.ProductBatches.ConvertAll(item =>
                    item.QualityBasisPoints),
                Is.EqualTo(summaries));
            Assert.That(loaded.ProductBatches.TrueForAll(item =>
                    item.QualityDimensions.Count > 0),
                Is.True);
            loaded.Validate();
        }

        [Test]
        public void MilitaryProcurement_PrototypeCreatesMappedSupplierStock()
        {
            var world = PrototypeWorldFactory.Create184World(184);

            Assert.That(
                world.InventoryContainers.Count,
                Is.EqualTo(3 + world.Armies.Count));
            Assert.That(
                world.InventoryContainers.Exists(item =>
                    item.Id == MerchantTownOperationSystem
                        .ZhongshanWarehouseContainerId),
                Is.True);
            Assert.That(
                world.InventoryContainers.Exists(item =>
                    item.Id == MilitaryProcurementSystem.PrototypeContainerId),
                Is.True);
            Assert.That(
                world.ProductBatches.FindAll(batch =>
                    batch.InventoryContainerId ==
                    MilitaryProcurementSystem.PrototypeContainerId).Count,
                Is.EqualTo(6));
            for (var i = 0;
                 i < world.MilitaryEquipmentDefinitions.Count;
                 i++)
            {
                Assert.That(
                    world.MilitaryEquipmentDefinitions[i].ProductDefinitionId,
                    Does.StartWith("product.equipment."));
            }

            world.Validate();
        }

        [Test]
        public void MilitaryProcurement_DispatchWaitsThenReceivesIntoArmory()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var armyId = new StableId("army.youzhou_reinforcement");
            var routeId = new StableId("route.zhongshan_anping");
            var destinationId = new StableId("location.anping");
            new ArmySystem().StartMarch(
                world,
                new StableId("person.zou_jing"),
                armyId,
                routeId,
                destinationId);
            var buyer = world.Organizations.Find(item =>
                item.Id == "organization.youzhou_field_force");
            var supplier = world.Organizations.Find(item =>
                item.Id == "organization.zhongshan_merchants");
            var batch = world.ProductBatches.Find(item =>
                item.ProductDefinitionId ==
                CoreProductionContent.LongSpearProductId);
            var stock = world.MilitaryArmoryStocks.Find(item =>
                item.ArmyId == armyId.Value &&
                item.EquipmentDefinitionId ==
                MilitaryEquipmentSystem.LongSpearId);
            var buyerBefore = buyer.Treasury;
            var supplierBefore = supplier.Treasury;
            var batchBefore = batch.Quantity;
            var stockBefore = stock.AvailableQuantity;
            var procurement = new MilitaryProcurementSystem();

            var order = procurement.CreateOrderAndDispatch(
                world,
                new StableId("person.zou_jing"),
                new StableId("person.zhang_shiping"),
                armyId,
                new StableId(MilitaryEquipmentSystem.LongSpearId),
                2,
                25,
                routeId,
                destinationId);

            Assert.That(batch.Quantity, Is.EqualTo(batchBefore - 2));
            Assert.That(buyer.Treasury, Is.EqualTo(buyerBefore - 50));
            Assert.That(supplier.Treasury, Is.EqualTo(supplierBefore + 50));
            Assert.That(order.Status, Is.EqualTo(
                MilitaryProcurementStatus.InTransit));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 15);
            Assert.That(order.Status, Is.EqualTo(
                MilitaryProcurementStatus.AwaitingArmy));
            Assert.That(stock.AvailableQuantity, Is.EqualTo(stockBefore));

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 3);

            Assert.That(order.Status, Is.EqualTo(
                MilitaryProcurementStatus.Delivered));
            Assert.That(stock.AvailableQuantity, Is.EqualTo(stockBefore + 2));
            Assert.That(procurement.Audit(world, order.Id).IsBalanced, Is.True);
            Assert.That(
                new MilitaryEquipmentSystem().AuditArmy(world, armyId.Value)
                    .IsBalanced,
                Is.True);
            Assert.That(
                world.MilitaryEquipmentTransactions.Exists(item =>
                    item.SourceProcurementOrderId == order.Id &&
                    item.Type ==
                    MilitaryEquipmentTransactionType.ProcurementReceipt),
                Is.True);
        }

        [Test]
        public void MilitaryProcurement_RejectsInsufficientFundsWithoutMutation()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var buyer = world.Organizations.Find(item =>
                item.Id == "organization.youzhou_field_force");
            buyer.Treasury = 0;
            var batch = world.ProductBatches.Find(item =>
                item.ProductDefinitionId ==
                CoreProductionContent.LongSpearProductId);
            var quantityBefore = batch.Quantity;

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryProcurementSystem().CreateOrderAndDispatch(
                    world,
                    new StableId("person.zou_jing"),
                    new StableId("person.zhang_shiping"),
                    new StableId("army.youzhou_reinforcement"),
                    new StableId(MilitaryEquipmentSystem.LongSpearId),
                    1,
                    25,
                    new StableId("route.zhongshan_anping"),
                    new StableId("location.anping")));

            Assert.That(world.MilitaryProcurementOrders, Is.Empty);
            Assert.That(world.Journeys, Is.Empty);
            Assert.That(batch.Quantity, Is.EqualTo(quantityBefore));
        }

        [Test]
        public void MilitaryProcurement_RejectsAuthorityRouteAndStockBeforeDispatch()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var system = new MilitaryProcurementSystem();
            var batch = world.ProductBatches.Find(item =>
                item.ProductDefinitionId ==
                CoreProductionContent.LongSpearProductId);
            var quantityBefore = batch.Quantity;

            Assert.Throws<InvalidOperationException>(() =>
                system.CreateOrderAndDispatch(
                    world,
                    new StableId("person.liu_bei"),
                    new StableId("person.zhang_shiping"),
                    new StableId("army.youzhou_reinforcement"),
                    new StableId(MilitaryEquipmentSystem.LongSpearId),
                    1,
                    25,
                    new StableId("route.zhongshan_anping"),
                    new StableId("location.anping")));
            Assert.Throws<InvalidOperationException>(() =>
                system.CreateOrderAndDispatch(
                    world,
                    new StableId("person.zou_jing"),
                    new StableId("person.zhang_shiping"),
                    new StableId("army.youzhou_reinforcement"),
                    new StableId(MilitaryEquipmentSystem.LongSpearId),
                    1,
                    25,
                    new StableId("route.anping_xiaquyang"),
                    new StableId("location.anping")));
            Assert.Throws<InvalidOperationException>(() =>
                system.CreateOrderAndDispatch(
                    world,
                    new StableId("person.zou_jing"),
                    new StableId("person.zhang_shiping"),
                    new StableId("army.youzhou_reinforcement"),
                    new StableId(MilitaryEquipmentSystem.LongSpearId),
                    5,
                    25,
                    new StableId("route.zhongshan_anping"),
                    new StableId("location.anping")));

            Assert.That(world.MilitaryProcurementOrders, Is.Empty);
            Assert.That(world.Journeys, Is.Empty);
            Assert.That(batch.Quantity, Is.EqualTo(quantityBefore));
            world.Validate();
        }

        [Test]
        public void MilitaryProcurement_SameCommandsProduceSameSnapshot()
        {
            var first = PrototypeWorldFactory.Create184World(184);
            var second = PrototypeWorldFactory.Create184World(184);
            ExecutePrototypeProcurement(first);
            ExecutePrototypeProcurement(second);

            Assert.That(
                WorldSnapshotSerializer.Serialize(first),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(second)));
        }

        [Test]
        public void MilitaryProcurement_SnapshotRoundTripPreservesDelivery()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            ExecutePrototypeProcurement(world);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.MilitaryProcurementOrders.Count, Is.EqualTo(1));
            Assert.That(
                loaded.MilitaryProcurementOrders[0].Status,
                Is.EqualTo(MilitaryProcurementStatus.Delivered));
            Assert.That(
                loaded.MilitaryProcurementLedgerEntries.Count,
                Is.EqualTo(2));
            loaded.Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionThirteenWithoutProcurementFabrication()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 13");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.InventoryContainers, Is.Empty);
            Assert.That(loaded.MilitaryProcurementOrders, Is.Empty);
            Assert.That(loaded.MilitaryProcurementLedgerEntries, Is.Empty);
            Assert.That(
                loaded.ProductBatches.Exists(batch =>
                    !string.IsNullOrEmpty(batch.OwnerOrganizationId)),
                Is.False);
            Assert.That(
                loaded.MilitaryEquipmentDefinitions.TrueForAll(definition =>
                    !string.IsNullOrEmpty(definition.ProductDefinitionId)),
                Is.True);
            loaded.Validate();
        }

        [Test]
        public void MilitaryLogistics_CommercialFreightConsumesOwnSupplyLosesCargoAndDelivers()
        {
            var world = PrepareMerchantLogisticsWorld();
            var army = world.Armies.Find(item =>
                item.Id == "army.youzhou_reinforcement");
            var carrier = world.People.Find(item =>
                item.Id == "person.zhang_shiping");
            var buyer = world.Organizations.Find(item =>
                item.Id == army.OrganizationId);
            var supplier = world.Organizations.Find(item =>
                item.Id == "organization.zhongshan_merchants");
            var buyerBefore = buyer.Treasury;
            var supplierBefore = supplier.Treasury;
            var carrierProvisionsBefore = carrier.Provisions;
            var armyProvisionsBefore = army.Provisions;
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsSystem();

            var order = system.Dispatch(
                world,
                MerchantLogisticsRequest(
                    MilitarySupplyAcquisitionMethodIds.CommercialPurchase,
                    3));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 18);

            Assert.That(order.Status,
                Is.EqualTo(MilitaryLogisticsStatus.Delivered));
            Assert.That(order.NaturalLossQuantity, Is.GreaterThan(0));
            Assert.That(order.ConvoyProvisionsConsumed, Is.GreaterThan(0));
            Assert.That(carrier.Provisions,
                Is.EqualTo(carrierProvisionsBefore));
            Assert.That(order.DeliveredCargoQuantity +
                order.NaturalLossQuantity,
                Is.EqualTo(order.DispatchedCargoQuantity));
            var supplyRecord = world.MilitarySupplies.Find(item =>
                item.SourceLogisticsOrderId == order.Id);
            Assert.That(supplyRecord, Is.Not.Null);
            Assert.That(supplyRecord.ProvisionsAdded,
                Is.EqualTo(order.DeliveredCargoQuantity *
                    MilitarySupplySystem.ProvisionsPerGrainUnit));
            Assert.That(army.Provisions,
                Is.EqualTo(armyProvisionsBefore -
                    4 * Math.Max(1, army.Troops / 100) +
                    supplyRecord.ProvisionsAdded));
            Assert.That(buyer.Treasury,
                Is.EqualTo(buyerBefore - order.TotalPaid));
            Assert.That(supplier.Treasury,
                Is.EqualTo(supplierBefore + order.TotalPaid));
            Assert.That(system.Audit(world, order.Id).IsBalanced, Is.True);
            Assert.That(world.MilitarySupplies.Exists(item =>
                item.SourceLogisticsOrderId == order.Id), Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_InternalArmyHaulNeedsNoPurchasePayment()
        {
            var world = PrepareArmyLogisticsWorld();
            var army = world.Armies.Find(item =>
                item.Id == "army.youzhou_reinforcement");
            var organization = world.Organizations.Find(item =>
                item.Id == army.OrganizationId);
            var treasuryBefore = organization.Treasury;
            StartYouzhouArmyToAnping(world);
            var request = ArmyLogisticsRequest(
                MilitarySupplyAcquisitionMethodIds.InternalDepotTransfer,
                0);
            var system = new MilitaryLogisticsSystem();

            var order = system.Dispatch(world, request);
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 18);

            Assert.That(order.Status,
                Is.EqualTo(MilitaryLogisticsStatus.Delivered));
            Assert.That(order.SourceOrganizationId,
                Is.EqualTo(order.BuyerOrganizationId));
            Assert.That(order.CarrierOrganizationId,
                Is.EqualTo(order.BuyerOrganizationId));
            Assert.That(order.TotalPaid, Is.Zero);
            Assert.That(organization.Treasury, Is.EqualTo(treasuryBefore));
            Assert.That(system.Audit(world, order.Id).IsBalanced, Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_PlunderTransfersRealBatchAndHarmsPublicOrder()
        {
            var world = PrepareArmyLogisticsWorld(includeMerchantCargo: true);
            var armyOrganization = world.Organizations.Find(item =>
                item.Id == "organization.youzhou_field_force");
            var merchantOrganization = world.Organizations.Find(item =>
                item.Id == "organization.zhongshan_merchants");
            var sourceBatch = world.ProductBatches.Find(item =>
                item.Id == "product_batch.logistics.merchant_cargo");
            var location = world.Locations.Find(item =>
                item.Id == "location.zhongshan");
            var armyMoneyBefore = armyOrganization.Treasury;
            var merchantMoneyBefore = merchantOrganization.Treasury;
            var sourceBefore = sourceBatch.Quantity;
            var publicOrderBefore = location.PublicOrderBasisPoints;
            StartYouzhouArmyToAnping(world);
            var request = ArmyLogisticsRequest(
                MilitarySupplyAcquisitionMethodIds.Plunder,
                0);
            request.SourceCargoBatchId = new StableId(
                "product_batch.logistics.merchant_cargo");
            request.CargoQuantity = 50;
            var system = new MilitaryLogisticsSystem();

            var order = system.Dispatch(world, request);

            Assert.That(sourceBatch.Quantity, Is.EqualTo(sourceBefore - 50));
            Assert.That(order.TotalPaid, Is.Zero);
            Assert.That(armyOrganization.Treasury,
                Is.EqualTo(armyMoneyBefore));
            Assert.That(merchantOrganization.Treasury,
                Is.EqualTo(merchantMoneyBefore));
            Assert.That(location.PublicOrderBasisPoints,
                Is.LessThan(publicOrderBefore));
            Assert.That(order.OriginPublicOrderDelta, Is.LessThan(0));
            Assert.That(system.Audit(world, order.Id).IsBalanced, Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_RejectsUnauthorizedDispatchWithoutMutation()
        {
            var world = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var batch = world.ProductBatches.Find(item =>
                item.Id == "product_batch.logistics.merchant_cargo");
            var quantityBefore = batch.Quantity;
            var buyer = world.Organizations.Find(item =>
                item.Id == "organization.youzhou_field_force");
            var treasuryBefore = buyer.Treasury;
            var request = MerchantLogisticsRequest(
                MilitarySupplyAcquisitionMethodIds.CommercialPurchase,
                3);
            request.IssuerPersonId = new StableId("person.liu_bei");

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryLogisticsSystem().Dispatch(world, request));

            Assert.That(world.MilitaryLogisticsOrders, Is.Empty);
            Assert.That(world.MilitaryLogisticsLedgerEntries, Is.Empty);
            Assert.That(world.Journeys, Is.Empty);
            Assert.That(batch.Quantity, Is.EqualTo(quantityBefore));
            Assert.That(buyer.Treasury, Is.EqualTo(treasuryBefore));
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_SnapshotRoundTripAndV18MigrationAreSafe()
        {
            var first = PrepareMerchantLogisticsWorld();
            var second = PrepareMerchantLogisticsWorld();
            ExecutePrototypeMilitaryLogistics(first);
            ExecutePrototypeMilitaryLogistics(second);

            var firstJson = WorldSnapshotSerializer.Serialize(first);
            Assert.That(firstJson,
                Is.EqualTo(WorldSnapshotSerializer.Serialize(second)));
            var loaded = WorldSnapshotSerializer.Deserialize(firstJson);
            Assert.That(loaded.MilitaryLogisticsOrders.Count, Is.EqualTo(1));
            Assert.That(
                new MilitaryLogisticsSystem().Audit(
                    loaded, loaded.MilitaryLogisticsOrders[0].Id).IsBalanced,
                Is.True);
            loaded.Validate();

            var legacyWorld = PrototypeWorldFactory.Create184World(184);
            var legacyJson = WorldSnapshotSerializer.Serialize(legacyWorld)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 18");
            var migrated = WorldSnapshotSerializer.Deserialize(legacyJson);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.MilitaryLogisticsOrders, Is.Empty);
            Assert.That(migrated.MilitaryLogisticsLedgerEntries, Is.Empty);
            migrated.Validate();
        }

        [Test]
        public void MilitaryLogistics_MultiLegHandoffMovesRealCustodyAndDelivers()
        {
            var world = PrepareMultiLegLogisticsWorld();
            var request = MultiLegLogisticsRequest();
            var system = new MilitaryLogisticsSystem();
            var secondProvisionBatch = world.ProductBatches.Find(item =>
                item.Id == "product_batch.logistics.handoff_provisions");
            var secondQuantityBefore = secondProvisionBatch.Quantity;

            var order = system.Dispatch(world, request);
            AdvanceLogisticsUntilNotInTransit(world, order, 40);

            Assert.That(order.Status,
                Is.EqualTo(MilitaryLogisticsStatus.AwaitingHandoff));
            Assert.That(secondProvisionBatch.ReservedQuantity,
                Is.EqualTo(12));
            system.Handoff(world, order.Id);
            Assert.That(order.CurrentLegSequence, Is.EqualTo(1));
            Assert.That(order.CarrierPersonId,
                Is.EqualTo("person.su_shuang"));
            Assert.That(secondProvisionBatch.Quantity,
                Is.EqualTo(secondQuantityBefore - 12));
            Assert.That(secondProvisionBatch.ReservedQuantity, Is.Zero);

            AdvanceLogisticsUntilNotInTransit(world, order, 60);
            system.ResolveArrivals(world);

            Assert.That(order.Status,
                Is.EqualTo(MilitaryLogisticsStatus.Delivered));
            Assert.That(order.DeliveredCargoQuantity,
                Is.GreaterThan(0));
            Assert.That(world.MilitaryLogisticsLegs.FindAll(leg =>
                leg.LogisticsOrderId == order.Id).TrueForAll(leg =>
                    leg.Status == MilitaryLogisticsLegStatus.Completed),
                Is.True);
            Assert.That(system.Audit(world, order.Id).IsBalanced, Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_HandoffRejectsAbsentCarrierWithoutMutation()
        {
            var world = PrepareMultiLegLogisticsWorld();
            var system = new MilitaryLogisticsSystem();
            var order = system.Dispatch(world, MultiLegLogisticsRequest());
            AdvanceLogisticsUntilNotInTransit(world, order, 40);
            var nextCarrier = world.People.Find(item =>
                item.Id == "person.su_shuang");
            new PopulationLedgerSystem().MoveIndependentPerson(
                world, nextCarrier, "location.zhongshan");
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                system.Handoff(world, order.Id));

            Assert.That(
                WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_ManualReceiptSupportsBalancedPartialDelivery()
        {
            var world = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var request = MerchantLogisticsRequest(
                MilitarySupplyAcquisitionMethodIds.CommercialPurchase, 3);
            request.AutoDeliverAtFinal = false;
            var system = new MilitaryLogisticsSystem();
            var order = system.Dispatch(world, request);
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 18);
            system.ResolveArrivals(world);
            var available = order.RemainingCargoQuantity;

            var first = system.DeliverPartial(world, order.Id, 30);
            Assert.That(first, Is.EqualTo(Math.Min(30, available)));
            if (order.RemainingCargoQuantity > 0)
            {
                Assert.That(order.Status,
                    Is.EqualTo(MilitaryLogisticsStatus.AwaitingArmy));
                system.DeliverPartial(
                    world, order.Id, order.RemainingCargoQuantity);
            }

            Assert.That(order.Status,
                Is.EqualTo(MilitaryLogisticsStatus.Delivered));
            Assert.That(order.DeliveredCargoQuantity,
                Is.EqualTo(available));
            Assert.That(system.Audit(world, order.Id).IsBalanced, Is.True);
            Assert.That(world.MilitarySupplies.FindAll(record =>
                record.SourceLogisticsOrderId == order.Id).Count,
                Is.GreaterThanOrEqualTo(1));
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_V20RoundTripAndV19MigrationPreserveContracts()
        {
            var world = PrepareMultiLegLogisticsWorld();
            var order = new MilitaryLogisticsSystem().Dispatch(
                world, MultiLegLogisticsRequest());
            var repeated = PrepareMultiLegLogisticsWorld();
            new MilitaryLogisticsSystem().Dispatch(
                repeated, MultiLegLogisticsRequest());
            var json = WorldSnapshotSerializer.Serialize(world);
            Assert.That(
                WorldSnapshotSerializer.Serialize(repeated),
                Is.EqualTo(json));
            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.MilitaryLogisticsLegs.Count,
                Is.EqualTo(2));
            Assert.That(loaded.MilitaryLogisticsOrders[0]
                .FinalDestinationLocationId,
                Is.EqualTo("location.guangzong"));
            Assert.That(
                WorldSnapshotSerializer.Serialize(loaded),
                Is.EqualTo(json));
            loaded.Validate();

            var legacyWorld = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(legacyWorld);
            new MilitaryLogisticsSystem().Dispatch(
                legacyWorld,
                MerchantLogisticsRequest(
                    MilitarySupplyAcquisitionMethodIds.CommercialPurchase,
                    3));
            var legacyJson = WorldSnapshotSerializer.Serialize(legacyWorld)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 19");
            var migrated = WorldSnapshotSerializer.Deserialize(legacyJson);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.MilitaryLogisticsLegs, Is.Empty);
            Assert.That(migrated.MilitaryLogisticsOrders.Count,
                Is.EqualTo(1));
            Assert.That(migrated.MilitaryLogisticsOrders[0]
                .FinalDestinationLocationId,
                Is.EqualTo(migrated.MilitaryLogisticsOrders[0]
                    .DestinationLocationId));
            Assert.That(migrated.MilitaryLogisticsOrders[0]
                .PlannedLegCount, Is.Zero);
            Assert.That(order.PlannedLegCount, Is.EqualTo(2));
            migrated.Validate();
        }

        [Test]
        public void MilitaryLogistics_UnescortedAttackSeizesAuditedCargo()
        {
            var world = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            world.Routes.Find(item =>
                item.Id == "route.zhongshan_anping")
                .SecurityBasisPoints = 0;
            var request = MerchantLogisticsRequest(
                MilitarySupplyAcquisitionMethodIds.CommercialPurchase, 3);
            request.RiskPolicyId = MilitaryLogisticsRiskPolicyIds.Standard;
            request.ThreatOrganizationId =
                "organization.taiping_yellow_turban";
            var system = new MilitaryLogisticsSystem();
            var order = system.Dispatch(world, request);

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 4);

            var incidents = world.MilitaryLogisticsIncidents.FindAll(item =>
                item.LogisticsOrderId == order.Id);
            Assert.That(incidents, Has.Count.EqualTo(1));
            Assert.That(incidents[0].OutcomeId,
                Is.EqualTo(
                    MilitaryLogisticsIncidentOutcomeIds.CargoSeized));
            Assert.That(incidents[0].ThreatOrganizationId,
                Is.EqualTo("organization.taiping_yellow_turban"));
            Assert.That(incidents[0].SeizedCargoQuantity,
                Is.GreaterThan(0));
            Assert.That(order.HostileLossQuantity,
                Is.EqualTo(incidents[0].SeizedCargoQuantity));
            Assert.That(system.Audit(world, order.Id).IsBalanced, Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_RealEscortTravelsAndRepelsAttack()
        {
            var world = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            world.Routes.Find(item =>
                item.Id == "route.zhongshan_anping")
                .SecurityBasisPoints = 0;
            var escort = world.People.Find(item =>
                item.Id == "person.su_shuang");
            MaximizeLogisticsEscortAbility(escort);
            var request = MerchantLogisticsRequest(
                MilitarySupplyAcquisitionMethodIds.CommercialPurchase, 3);
            request.RiskPolicyId = MilitaryLogisticsRiskPolicyIds.Standard;
            request.ThreatOrganizationId =
                "organization.taiping_yellow_turban";
            request.EscortPersonIds.Add(escort.Id);
            var system = new MilitaryLogisticsSystem();
            var order = system.Dispatch(world, request);
            var escortState = world.MilitaryLogisticsEscorts.Find(item =>
                item.LogisticsOrderId == order.Id &&
                item.PersonId == escort.Id);

            Assert.That(escortState.Status,
                Is.EqualTo(MilitaryLogisticsEscortStatus.InTransit));
            Assert.That(world.Journeys.Exists(item =>
                item.Id == escortState.JourneyId &&
                item.PersonId == escort.Id), Is.True);

            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 4);

            var incident = world.MilitaryLogisticsIncidents.Find(item =>
                item.LogisticsOrderId == order.Id);
            Assert.That(incident.OutcomeId,
                Is.EqualTo(MilitaryLogisticsIncidentOutcomeIds.Repelled));
            Assert.That(incident.EscortPower,
                Is.GreaterThanOrEqualTo(incident.ThreatPower));
            Assert.That(incident.SeizedCargoQuantity, Is.Zero);
            Assert.That(order.HostileLossQuantity, Is.Zero);
            Assert.That(system.Audit(world, order.Id).IsBalanced, Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_RejectsInvalidEscortWithoutMutation()
        {
            var world = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var request = MerchantLogisticsRequest(
                MilitarySupplyAcquisitionMethodIds.CommercialPurchase, 3);
            request.RiskPolicyId = MilitaryLogisticsRiskPolicyIds.Standard;
            request.ThreatOrganizationId =
                "organization.taiping_yellow_turban";
            request.EscortPersonIds.Add("person.liu_bei");
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryLogisticsSystem().Dispatch(world, request));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_V22RoundTripAndV20MigrationAreSafe()
        {
            var first = PrepareEscortRiskWorld();
            var second = PrepareEscortRiskWorld();
            var firstJson = WorldSnapshotSerializer.Serialize(first);
            Assert.That(WorldSnapshotSerializer.Serialize(second),
                Is.EqualTo(firstJson));
            var loaded = WorldSnapshotSerializer.Deserialize(firstJson);
            Assert.That(WorldSnapshotSerializer.Serialize(loaded),
                Is.EqualTo(firstJson));
            Assert.That(loaded.MilitaryLogisticsEscorts, Has.Count.EqualTo(1));
            loaded.Validate();

            var legacyWorld = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(legacyWorld);
            new MilitaryLogisticsSystem().Dispatch(
                legacyWorld,
                MerchantLogisticsRequest(
                    MilitarySupplyAcquisitionMethodIds.CommercialPurchase,
                    3));
            var legacyJson = WorldSnapshotSerializer.Serialize(legacyWorld)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 20");
            var migrated = WorldSnapshotSerializer.Deserialize(legacyJson);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.MilitaryLogisticsEscorts, Is.Empty);
            Assert.That(migrated.MilitaryLogisticsIncidents, Is.Empty);
            Assert.That(migrated.MilitaryLogisticsLegs, Has.Count.EqualTo(1));
            Assert.That(migrated.MilitaryLogisticsLegs[0].RiskPolicyId,
                Is.EqualTo(MilitaryLogisticsRiskPolicyIds.None));
            Assert.That(migrated.MilitaryLogisticsOrders[0]
                .HostileLossQuantity, Is.Zero);
            migrated.Validate();
        }

        [Test]
        public void MilitaryLogistics_AttackCreatesClashAndRealInjury()
        {
            var world = PrepareSeizedLogisticsWorld();
            var incident = world.MilitaryLogisticsIncidents[0];
            var clash = world.MilitaryLogisticsClashes.Find(item =>
                item.IncidentId == incident.Id &&
                item.TypeId ==
                    MilitaryLogisticsClashTypeIds.InitialDefense);

            Assert.That(clash, Is.Not.Null);
            Assert.That(clash.OutcomeId,
                Is.EqualTo(
                    MilitaryLogisticsClashOutcomeIds.AttackersSeizedCargo));
            Assert.That(clash.DefenderPersonIds,
                Does.Contain("person.zhang_shiping"));
            Assert.That(clash.Injuries, Is.Not.Empty);
            var injury = clash.Injuries[0];
            var injured = world.People.Find(item =>
                item.Id == injury.PersonId);
            Assert.That(injury.HealthAfterBasisPoints,
                Is.LessThan(injury.HealthBeforeBasisPoints));
            Assert.That(injured.HealthBasisPoints,
                Is.EqualTo(injury.HealthAfterBasisPoints));
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_AuthorizedArmyRecoversSeizedCargo()
        {
            var world = PrepareSeizedLogisticsWorld();
            var incident = world.MilitaryLogisticsIncidents[0];
            var order = world.MilitaryLogisticsOrders[0];
            var seized = incident.SeizedCargoQuantity;
            var remainingBefore = order.RemainingCargoQuantity;
            var participants = PrepareStrongRecoveryParty(world, 4);
            var system = new MilitaryLogisticsSystem();

            var recovered = system.AttemptArmyRecovery(
                world,
                new StableId("person.zou_jing"),
                incident.Id,
                participants);

            Assert.That(recovered, Is.EqualTo(seized));
            Assert.That(incident.RecoveredCargoQuantity,
                Is.EqualTo(seized));
            Assert.That(order.RemainingCargoQuantity,
                Is.EqualTo(remainingBefore + seized));
            Assert.That(order.HostileLossQuantity, Is.Zero);
            Assert.That(order.RecoveredCargoQuantity,
                Is.EqualTo(seized));
            Assert.That(world.MilitaryLogisticsClashes.Exists(item =>
                item.IncidentId == incident.Id &&
                item.OutcomeId ==
                    MilitaryLogisticsClashOutcomeIds.CargoRecovered),
                Is.True);
            Assert.That(system.Audit(world, order.Id).IsBalanced, Is.True);
            Assert.That(system.Audit(world, order.Id).RecoveredCargo,
                Is.EqualTo(seized));
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_WeakRecoveryFailsOnceAndKeepsCustody()
        {
            var world = PrepareSeizedLogisticsWorld();
            var incident = world.MilitaryLogisticsIncidents[0];
            var order = world.MilitaryLogisticsOrders[0];
            var participant = world.MilitaryServices.Find(item =>
                item.ArmyId == order.TargetArmyId &&
                item.PersonId != "person.zou_jing" &&
                (item.Status == MilitaryServiceStatus.Active ||
                 item.Status == MilitaryServiceStatus.Mustering));
            MinimizeLogisticsCombatAbility(world.People.Find(item =>
                item.Id == participant.PersonId));
            var hostileBefore = order.HostileLossQuantity;
            var system = new MilitaryLogisticsSystem();

            var recovered = system.AttemptArmyRecovery(
                world,
                new StableId("person.zou_jing"),
                incident.Id,
                new[] { participant.PersonId });

            Assert.That(recovered, Is.Zero);
            Assert.That(order.HostileLossQuantity,
                Is.EqualTo(hostileBefore));
            var clash = world.MilitaryLogisticsClashes.Find(item =>
                item.IncidentId == incident.Id &&
                item.TypeId ==
                    MilitaryLogisticsClashTypeIds.RecoveryAttempt);
            Assert.That(clash.OutcomeId,
                Is.EqualTo(
                    MilitaryLogisticsClashOutcomeIds.RecoveryFailed));
            Assert.That(clash.Injuries, Is.Not.Empty);
            Assert.That(participant.Status,
                Is.EqualTo(MilitaryServiceStatus.Wounded));
            Assert.That(system.Audit(world, order.Id).IsBalanced, Is.True);
            var afterFailure = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() =>
                system.AttemptArmyRecovery(
                    world,
                    new StableId("person.zou_jing"),
                    incident.Id,
                    new[] { participant.PersonId }));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(afterFailure));
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_RecoveryRejectsInvalidPartyWithoutMutation()
        {
            var world = PrepareSeizedLogisticsWorld();
            var incident = world.MilitaryLogisticsIncidents[0];
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                new MilitaryLogisticsSystem().AttemptArmyRecovery(
                    world,
                    new StableId("person.zou_jing"),
                    incident.Id,
                    new[] { "person.zhang_shiping" }));

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            world.Validate();
        }

        [Test]
        public void MilitaryLogistics_V22RoundTripAndV21MigrationPreserveCustody()
        {
            var first = PrepareRecoveredLogisticsWorld();
            var second = PrepareRecoveredLogisticsWorld();
            var json = WorldSnapshotSerializer.Serialize(first);
            Assert.That(WorldSnapshotSerializer.Serialize(second),
                Is.EqualTo(json));
            var loaded = WorldSnapshotSerializer.Deserialize(json);
            Assert.That(WorldSnapshotSerializer.Serialize(loaded),
                Is.EqualTo(json));
            Assert.That(loaded.MilitaryLogisticsClashes.Count,
                Is.EqualTo(2));
            loaded.Validate();

            var legacy = PrepareSeizedLogisticsWorld();
            var injury = legacy.MilitaryLogisticsClashes[0].Injuries[0];
            legacy.People.Find(item => item.Id == injury.PersonId)
                .HealthBasisPoints = injury.HealthBeforeBasisPoints;
            legacy.MilitaryLogisticsClashes.Clear();
            legacy.Validate();
            var legacyJson = WorldSnapshotSerializer.Serialize(legacy)
                .Replace("\"SchemaVersion\": " + WorldState.CurrentSchemaVersion, "\"SchemaVersion\": 21");
            var migrated = WorldSnapshotSerializer.Deserialize(legacyJson);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.MilitaryLogisticsClashes, Is.Empty);
            Assert.That(migrated.MilitaryLogisticsIncidents[0]
                .RecoveredCargoQuantity, Is.Zero);
            Assert.That(migrated.MilitaryLogisticsOrders[0]
                .HostileLossQuantity,
                Is.EqualTo(migrated.MilitaryLogisticsIncidents[0]
                    .SeizedCargoQuantity));
            migrated.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_SelectsByStablePreference()
        {
            var preferences = new[]
            {
                MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                MilitaryLogisticsDelegationCarrierPreferenceIds.SafestRoute
            };
            var expectedCarriers = new[]
            {
                "person.zhang_shiping",
                "person.su_shuang"
            };
            for (var caseIndex = 0; caseIndex < preferences.Length; caseIndex++)
            {
                var world = PrepareDelegatedLogisticsWorld();
                StartYouzhouArmyToAnping(world);
                var system = new MilitaryLogisticsDelegationSystem();
                var goal = system.CreateGoal(
                    world,
                    DelegatedLogisticsGoal(
                        preferences[caseIndex], 1_000, 10));
                system.SubmitOffer(
                    world,
                    goal.Id,
                    DelegatedMerchantOffer(
                        "person.zhang_shiping",
                        "product_batch.logistics.merchant_cargo",
                        "product_batch.logistics.merchant_provisions",
                        "route.zhongshan_anping",
                        2));
                system.SubmitOffer(
                    world,
                    goal.Id,
                    DelegatedMerchantOffer(
                        "person.su_shuang",
                        "product_batch.delegation.su_cargo",
                        "product_batch.delegation.su_provisions",
                        "route.zhongshan_anping.safe_delegation_test",
                        4));

                var order = system.EvaluateAndDispatch(world, goal.Id);

                Assert.That(order, Is.Not.Null);
                Assert.That(order.CarrierPersonId,
                    Is.EqualTo(expectedCarriers[caseIndex]));
                Assert.That(goal.Status,
                    Is.EqualTo(MilitaryLogisticsDelegationStatus.Dispatched));
                Assert.That(goal.LogisticsOrderId, Is.EqualTo(order.Id));
                Assert.That(goal.CommittedCost, Is.EqualTo(order.TotalPaid));
                Assert.That(new MilitaryLogisticsSystem().Audit(world, order.Id)
                    .IsBalanced, Is.True);
                world.Validate();
            }
        }

        [Test]
        public void MilitaryLogisticsDelegation_OwnOrganizationPreferenceWins()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds
                        .OwnOrganizationFirst,
                    1_000,
                    10));
            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    1));
            var armyOffer = DelegatedMerchantOffer(
                "person.zou_jing",
                "product_batch.logistics.merchant_cargo",
                "product_batch.delegation.army_provisions",
                "route.zhongshan_anping",
                5);
            armyOffer.CarrierOrganizationId =
                "organization.youzhou_field_force";
            armyOffer.LossBearerOrganizationId =
                "organization.youzhou_field_force";
            system.SubmitOffer(world, goal.Id, armyOffer);

            var order = system.EvaluateAndDispatch(world, goal.Id);

            Assert.That(order.CarrierPersonId, Is.EqualTo("person.zou_jing"));
            Assert.That(order.CarrierOrganizationId,
                Is.EqualTo("organization.youzhou_field_force"));
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_BudgetExceptionCanBeRetried()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    100,
                    5));
            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    3));

            var rejected = system.EvaluateAndDispatch(world, goal.Id);

            Assert.That(rejected, Is.Null);
            Assert.That(goal.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.NeedsAttention));
            Assert.That(world.MilitaryLogisticsOrders, Is.Empty);
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == goal.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.BudgetExceeded),
                Is.True);

            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.su_shuang",
                    "product_batch.delegation.su_cargo",
                    "product_batch.delegation.su_provisions",
                    "route.zhongshan_anping.safe_delegation_test",
                    1));
            var accepted = system.EvaluateAndDispatch(world, goal.Id);

            Assert.That(accepted, Is.Not.Null);
            Assert.That(accepted.TotalPaid, Is.EqualTo(100));
            Assert.That(goal.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Dispatched));
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_InvalidatedOfferReportsWithoutFreight()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var carrier = world.People.Find(item =>
                item.Id == "person.zhang_shiping");
            new PopulationLedgerSystem().MoveIndependentPerson(
                world, carrier, "location.anping");

            var order = system.EvaluateAndDispatch(world, goal.Id);

            Assert.That(order, Is.Null);
            Assert.That(world.MilitaryLogisticsOrders, Is.Empty);
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == goal.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.OfferInvalidated),
                Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_RechecksAuthorityAndCreationIsAtomic()
        {
            var world = PrepareDelegatedLogisticsWorld();
            var system = new MilitaryLogisticsDelegationSystem();
            var unauthorized = DelegatedLogisticsGoal(
                MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                1_000,
                10);
            unauthorized.IssuerPersonId = new StableId("person.liu_bei");
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                system.CreateGoal(world, unauthorized));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));

            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            TransferPrototypeArmyCommandAwayFromZouJing(world);

            var order = system.EvaluateAndDispatch(world, goal.Id);

            Assert.That(order, Is.Null);
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == goal.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.AuthorityLost),
                Is.True);
            Assert.That(world.MilitaryLogisticsOrders, Is.Empty);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_V28RoundTripAndLegacyMigrationsAreSafe()
        {
            var world = PrepareDelegatedLogisticsWorld();
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var json = WorldSnapshotSerializer.Serialize(world);
            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(WorldSnapshotSerializer.Serialize(loaded),
                Is.EqualTo(json));
            Assert.That(loaded.MilitaryLogisticsDelegationGoals,
                Has.Count.EqualTo(1));
            Assert.That(loaded.MilitaryLogisticsDelegationOffers,
                Has.Count.EqualTo(1));
            Assert.That(loaded.MilitaryLogisticsDelegationGoals[0]
                    .FulfillmentPolicyId,
                Is.EqualTo(MilitaryLogisticsDelegationFulfillmentPolicyIds
                    .FullReceiptRequired));
            Assert.That(loaded.MilitaryLogisticsDelegationGoals[0]
                .OutstandingCargoQuantity, Is.EqualTo(100));
            Assert.That(loaded.MilitaryLogisticsDelegationGoals[0]
                    .ReplacementProcurementPolicyId,
                Is.EqualTo(MilitaryLogisticsReplacementProcurementPolicyIds
                    .WaitForCustodyResolution));
            Assert.That(loaded.MilitaryLogisticsDelegationOffers[0]
                    .LiabilityPolicyId,
                Is.EqualTo(MilitaryLogisticsLiabilityPolicyIds
                    .LossBearerCompensates));
            loaded.Validate();

            var versionTwentySevenJson = json.Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 27");
            var migratedTwentySeven = WorldSnapshotSerializer.Deserialize(
                versionTwentySevenJson);
            Assert.That(migratedTwentySeven.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migratedTwentySeven
                    .MilitaryLogisticsDelegationGoals[0]
                    .ReplacementProcurementPolicyId,
                Is.EqualTo(MilitaryLogisticsReplacementProcurementPolicyIds
                    .LegacyUnrestricted));
            Assert.That(migratedTwentySeven
                    .MilitaryLogisticsDelegationGoals[0]
                    .CompensationReceived,
                Is.Zero);
            Assert.That(migratedTwentySeven
                    .MilitaryLogisticsDelegationOffers[0]
                    .LiabilityPolicyId,
                Is.EqualTo(MilitaryLogisticsLiabilityPolicyIds
                    .LegacyNoRetroactiveSettlement));
            Assert.That(migratedTwentySeven
                    .MilitaryLogisticsLiabilitySettlements,
                Is.Empty);
            migratedTwentySeven.Validate();

            var versionTwentySixJson = json.Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 26");
            var migratedTwentySix = WorldSnapshotSerializer.Deserialize(
                versionTwentySixJson);
            var migratedTwentySixRoot = migratedTwentySix
                .MilitaryLogisticsDelegationGoals[0];
            Assert.That(migratedTwentySixRoot.FulfillmentPolicyId,
                Is.EqualTo(MilitaryLogisticsDelegationFulfillmentPolicyIds
                    .FullReceiptRequired));
            Assert.That(migratedTwentySixRoot.ReceivedCargoQuantity, Is.Zero);
            Assert.That(migratedTwentySixRoot.OutstandingCargoQuantity,
                Is.EqualTo(migratedTwentySixRoot.RequestedCargoQuantity));
            Assert.That(migratedTwentySixRoot.CompletedLogisticsOrderIds,
                Is.Empty);
            Assert.That(migratedTwentySix
                .MilitaryLogisticsDelegationOffers[0].LogisticsOrderId,
                Is.Empty);
            migratedTwentySix.Validate();

            var versionTwentyFiveJson = json.Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 25");
            var migratedTwentyFive = WorldSnapshotSerializer.Deserialize(
                versionTwentyFiveJson);
            var migratedTwentyFiveRoot = migratedTwentyFive
                .MilitaryLogisticsDelegationGoals[0];
            Assert.That(migratedTwentyFive.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migratedTwentyFiveRoot.UnassignedCargoQuantity,
                Is.Zero);
            Assert.That(migratedTwentyFiveRoot.AvailableBudgetReserve,
                Is.Zero);
            Assert.That(migratedTwentyFiveRoot.CancelledDay, Is.EqualTo(-1));
            Assert.That(migratedTwentyFiveRoot.CancelledByPersonId, Is.Empty);
            Assert.That(migratedTwentyFiveRoot.CancellationReasonId, Is.Empty);
            Assert.That(migratedTwentyFiveRoot.ReplacesGoalId, Is.Empty);
            Assert.That(migratedTwentyFiveRoot.ReplacementGoalIds, Is.Empty);
            migratedTwentyFive.Validate();

            var versionTwentyFourJson = json.Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 24");
            var migratedTwentyFour = WorldSnapshotSerializer.Deserialize(
                versionTwentyFourJson);
            var migratedRoot = migratedTwentyFour
                .MilitaryLogisticsDelegationGoals[0];
            Assert.That(migratedTwentyFour.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migratedRoot.ParentGoalId, Is.Empty);
            Assert.That(migratedRoot.DelegationDepth, Is.Zero);
            Assert.That(migratedRoot.AssigneePersonId,
                Is.EqualTo(migratedRoot.IssuerPersonId));
            Assert.That(migratedRoot.DelegatedByPersonId, Is.Empty);
            Assert.That(migratedRoot.AssigneeAuthorityAtDelegation,
                Is.EqualTo(MilitaryAuthorityLevel.Army));
            Assert.That(migratedRoot.ChildGoalIds, Is.Empty);
            Assert.That(migratedTwentyFour
                    .MilitaryLogisticsDelegationReports[0].RelatedGoalId,
                Is.Empty);
            migratedTwentyFour.Validate();

            var versionTwentyThreeJson = json.Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 23");
            var migratedTwentyThree = WorldSnapshotSerializer.Deserialize(
                versionTwentyThreeJson);
            Assert.That(migratedTwentyThree.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migratedTwentyThree
                    .MilitaryLogisticsDelegationGoals[0].NextEvaluationDay,
                Is.EqualTo(5));
            Assert.That(migratedTwentyThree
                    .MilitaryLogisticsDelegationGoals[0].FulfilledDay,
                Is.EqualTo(-1));
            Assert.That(migratedTwentyThree
                    .MilitaryLogisticsDelegationOffers[0].ValidUntilDay,
                Is.EqualTo(30));
            Assert.That(migratedTwentyThree
                    .MilitaryLogisticsDelegationOffers[0].ClosedDay,
                Is.EqualTo(-1));
            migratedTwentyThree.Validate();

            var legacyJson = json.Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 22");
            var migrated = WorldSnapshotSerializer.Deserialize(legacyJson);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.MilitaryLogisticsDelegationGoals, Is.Empty);
            Assert.That(migrated.MilitaryLogisticsDelegationOffers, Is.Empty);
            Assert.That(migrated.MilitaryLogisticsDelegationReports, Is.Empty);
            migrated.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_DailyScheduleDispatchesAutomatically()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var request = DelegatedLogisticsGoal(
                MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                1_000,
                10);
            request.ReportIntervalDays = 1;
            var goal = system.CreateGoal(world, request);
            var offerRequest = DelegatedMerchantOffer(
                "person.zhang_shiping",
                "product_batch.logistics.merchant_cargo",
                "product_batch.logistics.merchant_provisions",
                "route.zhongshan_anping",
                2);
            offerRequest.ValidUntilDay = 10;
            system.SubmitOffer(world, goal.Id, offerRequest);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);

            Assert.That(goal.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Dispatched));
            Assert.That(world.MilitaryLogisticsOrders, Has.Count.EqualTo(1));
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == goal.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.Dispatched),
                Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_WithdrawalDoesNotTouchInventoryOrMoney()
        {
            var world = PrepareDelegatedLogisticsWorld();
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            var offer = system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var cargo = world.ProductBatches.Find(item =>
                item.Id == offer.SourceCargoBatchId);
            var quantityBefore = cargo.Quantity;
            var reservedBefore = cargo.ReservedQuantity;
            var buyerMoneyBefore = world.Organizations.Find(item =>
                item.Id == "organization.youzhou_field_force").Treasury;

            system.WithdrawOffer(
                world,
                goal.Id,
                offer.Id,
                new StableId("person.zhang_shiping"));

            Assert.That(offer.Status,
                Is.EqualTo(MilitaryLogisticsDelegationOfferStatus.Withdrawn));
            Assert.That(offer.ClosedDay, Is.EqualTo(world.AbsoluteDay));
            Assert.That(cargo.Quantity, Is.EqualTo(quantityBefore));
            Assert.That(cargo.ReservedQuantity, Is.EqualTo(reservedBefore));
            Assert.That(world.Organizations.Find(item =>
                    item.Id == "organization.youzhou_field_force").Treasury,
                Is.EqualTo(buyerMoneyBefore));
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.RelatedOfferId == offer.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.OfferWithdrawn),
                Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_ExpiredOfferCanBeReplacedAndRetried()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var request = DelegatedLogisticsGoal(
                MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                1_000,
                10);
            request.ReportIntervalDays = 5;
            var goal = system.CreateGoal(world, request);
            var expiringRequest = DelegatedMerchantOffer(
                "person.zhang_shiping",
                "product_batch.logistics.merchant_cargo",
                "product_batch.logistics.merchant_provisions",
                "route.zhongshan_anping",
                2);
            expiringRequest.ValidUntilDay = 1;
            var expiredOffer = system.SubmitOffer(
                world, goal.Id, expiringRequest);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 2);

            Assert.That(expiredOffer.Status,
                Is.EqualTo(MilitaryLogisticsDelegationOfferStatus.Expired));
            Assert.That(world.MilitaryLogisticsOrders, Is.Empty);
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.RelatedOfferId == expiredOffer.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.OfferExpired),
                Is.True);

            var replacement = DelegatedMerchantOffer(
                "person.su_shuang",
                "product_batch.delegation.su_cargo",
                "product_batch.delegation.su_provisions",
                "route.zhongshan_anping.safe_delegation_test",
                3);
            replacement.ValidUntilDay = 20;
            system.SubmitOffer(world, goal.Id, replacement);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);

            Assert.That(goal.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Dispatched));
            Assert.That(world.MilitaryLogisticsOrders, Has.Count.EqualTo(1));
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_ReportsProgressAndRealDeliveryCompletion()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var request = DelegatedLogisticsGoal(
                MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                1_000,
                10);
            request.ReportIntervalDays = 1;
            var goal = system.CreateGoal(world, request);
            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var order = system.EvaluateAndDispatch(world, goal.Id);
            var simulator = new WorldSimulator(world.MasterSeed);

            simulator.AdvanceDays(world, 1);

            Assert.That(order.Status,
                Is.Not.EqualTo(MilitaryLogisticsStatus.Delivered));
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == goal.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.Progress &&
                item.LogisticsOrderId == order.Id), Is.True);

            simulator.AdvanceDays(world, 10);

            Assert.That(order.Status,
                Is.EqualTo(MilitaryLogisticsStatus.Delivered));
            Assert.That(goal.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.NeedsAttention));
            Assert.That(goal.ReceivedCargoQuantity,
                Is.EqualTo(order.DeliveredCargoQuantity));
            Assert.That(goal.OutstandingCargoQuantity,
                Is.EqualTo(order.NaturalLossQuantity +
                    order.HostileLossQuantity +
                    order.CargoConsumedAsProvisionsQuantity));
            Assert.That(goal.CompletedLogisticsOrderIds,
                Is.EqualTo(new[] { order.Id }));
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == goal.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.DeliveryShortfall &&
                item.LogisticsOrderId == order.Id), Is.True);

            var zeroLossContent = ZeroPerishabilityContent();
            var supplementalSystem =
                new MilitaryLogisticsDelegationSystem(zeroLossContent);
            supplementalSystem.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.su_shuang",
                    "product_batch.delegation.su_cargo",
                    "product_batch.delegation.su_provisions",
                    "route.zhongshan_anping.safe_delegation_test",
                    3));
            var supplementalOrder = supplementalSystem.EvaluateAndDispatch(
                world, goal.Id);
            new WorldSimulator(world.MasterSeed, zeroLossContent)
                .AdvanceDays(world, 10);

            Assert.That(goal.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Fulfilled));
            Assert.That(goal.ReceivedCargoQuantity,
                Is.EqualTo(goal.RequestedCargoQuantity));
            Assert.That(goal.OutstandingCargoQuantity, Is.Zero);
            Assert.That(goal.CompletedLogisticsOrderIds,
                Is.EqualTo(new[] { order.Id, supplementalOrder.Id }));
            Assert.That(goal.CommittedCost,
                Is.EqualTo(order.TotalPaid + supplementalOrder.TotalPaid));
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == goal.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds
                        .SupplementalDispatched &&
                item.LogisticsOrderId == supplementalOrder.Id), Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_V26FulfilledShortfallKeepsLegacyClosure()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            var offer = system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var order = system.EvaluateAndDispatch(world, goal.Id);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 12);
            Assert.That(order.DeliveredCargoQuantity,
                Is.LessThan(goal.RequestedCargoQuantity));

            goal.Status = MilitaryLogisticsDelegationStatus.Fulfilled;
            goal.FulfilledDay = world.AbsoluteDay;
            goal.SelectedOfferId = offer.Id;
            goal.LogisticsOrderId = order.Id;
            goal.ReceivedCargoQuantity = 0;
            goal.OutstandingCargoQuantity = 0;
            goal.CompletedLogisticsOrderIds.Clear();
            offer.Status = MilitaryLogisticsDelegationOfferStatus.Selected;
            offer.ClosedDay = -1;
            offer.LogisticsOrderId = string.Empty;
            world.SchemaVersion = 26;
            var jsonConvertType = Type.GetType(
                "Newtonsoft.Json.JsonConvert, Newtonsoft.Json");
            Assert.That(jsonConvertType, Is.Not.Null);
            var serializeObject = jsonConvertType.GetMethod(
                "SerializeObject", new[] { typeof(object) });
            Assert.That(serializeObject, Is.Not.Null);
            var legacyJson = (string)serializeObject.Invoke(
                null, new object[] { world });

            var migrated = WorldSnapshotSerializer.Deserialize(legacyJson);
            var migratedGoal = migrated.MilitaryLogisticsDelegationGoals.Find(
                item => item.Id == goal.Id);
            var migratedOffer = migrated.MilitaryLogisticsDelegationOffers.Find(
                item => item.Id == offer.Id);

            Assert.That(migratedGoal.FulfillmentPolicyId,
                Is.EqualTo(MilitaryLogisticsDelegationFulfillmentPolicyIds
                    .LegacyOrderCompletion));
            Assert.That(migratedGoal.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Fulfilled));
            Assert.That(migratedGoal.ReceivedCargoQuantity,
                Is.EqualTo(order.DeliveredCargoQuantity));
            Assert.That(migratedGoal.OutstandingCargoQuantity, Is.Zero);
            Assert.That(migratedGoal.CompletedLogisticsOrderIds,
                Is.EqualTo(new[] { order.Id }));
            Assert.That(migratedGoal.SelectedOfferId, Is.Empty);
            Assert.That(migratedGoal.LogisticsOrderId, Is.Empty);
            Assert.That(migratedOffer.Status,
                Is.EqualTo(MilitaryLogisticsDelegationOfferStatus.Completed));
            Assert.That(migratedOffer.LogisticsOrderId, Is.EqualTo(order.Id));
            migrated.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_SupplementCannotExceedOriginalBudget()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    200,
                    10));
            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var firstOrder = system.EvaluateAndDispatch(world, goal.Id);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 12);
            Assert.That(goal.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.NeedsAttention));
            Assert.That(goal.CommittedCost, Is.EqualTo(200));

            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.su_shuang",
                    "product_batch.delegation.su_cargo",
                    "product_batch.delegation.su_provisions",
                    "route.zhongshan_anping.safe_delegation_test",
                    3));
            var beforeOrderCount = world.MilitaryLogisticsOrders.Count;
            var supplementalOrder = system.EvaluateAndDispatch(world, goal.Id);

            Assert.That(supplementalOrder, Is.Null);
            Assert.That(world.MilitaryLogisticsOrders,
                Has.Count.EqualTo(beforeOrderCount));
            Assert.That(goal.CommittedCost, Is.EqualTo(firstOrder.TotalPaid));
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == goal.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.BudgetExceeded),
                Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_CarrierCompensatesLossAndRestoresNetBudget()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    200,
                    10));
            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var firstOrder = system.EvaluateAndDispatch(world, goal.Id);
            var payer = world.Organizations.Find(item =>
                item.Id == "organization.zhongshan_merchants");
            var payee = world.Organizations.Find(item =>
                item.Id == "organization.youzhou_field_force");
            var payerBeforeSettlement = payer.Treasury;
            var payeeBeforeSettlement = payee.Treasury;

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 12);

            var settlement = world.MilitaryLogisticsLiabilitySettlements
                .Find(item => item.LogisticsOrderId == firstOrder.Id);
            Assert.That(world.MilitaryLogisticsLiabilitySettlements,
                Has.Count.EqualTo(1));
            var expectedDue = Math.Min(
                firstOrder.TotalPaid,
                firstOrder.UnitPrice *
                (firstOrder.NaturalLossQuantity +
                 firstOrder.HostileLossQuantity));
            Assert.That(expectedDue, Is.GreaterThan(0));
            Assert.That(settlement, Is.Not.Null);
            Assert.That(settlement.AmountDue, Is.EqualTo(expectedDue));
            Assert.That(settlement.AmountPaid, Is.EqualTo(expectedDue));
            Assert.That(settlement.OutstandingAmount, Is.Zero);
            Assert.That(settlement.Status,
                Is.EqualTo(
                    MilitaryLogisticsLiabilitySettlementStatus.Settled));
            Assert.That(payer.Treasury,
                Is.EqualTo(payerBeforeSettlement - expectedDue));
            Assert.That(payee.Treasury,
                Is.EqualTo(payeeBeforeSettlement + expectedDue));
            Assert.That(goal.CompensationReceived, Is.EqualTo(expectedDue));

            var zeroLossContent = ZeroPerishabilityContent();
            var supplementalSystem =
                new MilitaryLogisticsDelegationSystem(zeroLossContent);
            var replacement = DelegatedMerchantOffer(
                "person.su_shuang",
                "product_batch.delegation.su_cargo",
                "product_batch.delegation.su_provisions",
                "route.zhongshan_anping.safe_delegation_test",
                2);
            replacement.CargoQuantity = goal.OutstandingCargoQuantity;
            supplementalSystem.SubmitOffer(world, goal.Id, replacement);

            var supplementalOrder = supplementalSystem.EvaluateAndDispatch(
                world, goal.Id);

            Assert.That(supplementalOrder, Is.Not.Null);
            Assert.That(
                goal.CommittedCost - goal.CompensationReceived,
                Is.LessThanOrEqualTo(goal.BudgetLimit));
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_LiabilityArrearsCanBeCollectedLater()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var order = system.EvaluateAndDispatch(world, goal.Id);
            var payer = world.Organizations.Find(item =>
                item.Id == "organization.zhongshan_merchants");
            var payee = world.Organizations.Find(item =>
                item.Id == "organization.youzhou_field_force");
            payer.Treasury = 1;

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 12);

            var settlement = world.MilitaryLogisticsLiabilitySettlements
                .Find(item => item.LogisticsOrderId == order.Id);
            Assert.That(settlement, Is.Not.Null);
            Assert.That(settlement.AmountDue, Is.GreaterThan(1));
            Assert.That(settlement.AmountPaid, Is.EqualTo(1));
            Assert.That(settlement.OutstandingAmount,
                Is.EqualTo(settlement.AmountDue - 1));
            Assert.That(settlement.Status,
                Is.EqualTo(
                    MilitaryLogisticsLiabilitySettlementStatus.InArrears));
            Assert.That(goal.CompensationReceived, Is.EqualTo(1));

            payer.Treasury = settlement.OutstandingAmount;
            var payeeBeforeCollection = payee.Treasury;
            var outstandingBeforeCollection = settlement.OutstandingAmount;
            var collected = system.CollectOutstandingLiability(
                world,
                settlement.Id,
                new StableId("person.zou_jing"));

            Assert.That(collected, Is.EqualTo(outstandingBeforeCollection));
            Assert.That(payer.Treasury, Is.Zero);
            Assert.That(payee.Treasury,
                Is.EqualTo(payeeBeforeCollection + collected));
            Assert.That(settlement.OutstandingAmount, Is.Zero);
            Assert.That(settlement.Status,
                Is.EqualTo(
                    MilitaryLogisticsLiabilitySettlementStatus.Settled));
            Assert.That(goal.CompensationReceived,
                Is.EqualTo(settlement.AmountDue));
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == goal.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.LiabilityPayment),
                Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_SeizedCargoRequiresReplacementAuthorization()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            world.Routes.Find(item =>
                    item.Id == "route.zhongshan_anping")
                .SecurityBasisPoints = 0;
            var request = DelegatedLogisticsGoal(
                MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                1_000,
                10);
            request.RiskPolicyId = MilitaryLogisticsRiskPolicyIds.Standard;
            request.ThreatOrganizationId =
                "organization.taiping_yellow_turban";
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(world, request);
            system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var firstOrder = system.EvaluateAndDispatch(world, goal.Id);

            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 12);

            Assert.That(firstOrder.Status,
                Is.EqualTo(MilitaryLogisticsStatus.Delivered));
            Assert.That(firstOrder.HostileLossQuantity, Is.GreaterThan(0));
            Assert.That(goal.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.NeedsAttention));
            var replacement = DelegatedMerchantOffer(
                "person.su_shuang",
                "product_batch.delegation.su_cargo",
                "product_batch.delegation.su_provisions",
                "route.zhongshan_anping.safe_delegation_test",
                3);
            replacement.CargoQuantity = goal.OutstandingCargoQuantity;
            var beforeRejectedOffer = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                system.SubmitOffer(world, goal.Id, replacement));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(beforeRejectedOffer));

            system.AuthorizeReplacementProcurement(
                world,
                goal.Id,
                new StableId("person.zou_jing"),
                "military_logistics.replacement_reason.operational_necessity");
            var authorizedOffer = system.SubmitOffer(
                world, goal.Id, replacement);
            var supplementalOrder = system.EvaluateAndDispatch(world, goal.Id);

            Assert.That(authorizedOffer, Is.Not.Null);
            Assert.That(supplementalOrder, Is.Not.Null);
            Assert.That(goal.ReplacementProcurementPolicyId,
                Is.EqualTo(MilitaryLogisticsReplacementProcurementPolicyIds
                    .ExplicitAuthorization));
            Assert.That(goal.AuthorizedReplacementQuantity,
                Is.EqualTo(goal.ConsumedReplacementAuthorizationQuantity));
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == goal.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds
                        .ReplacementAuthorized),
                Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_V27ActiveOrderMigratesWithoutRetroactiveSettlement()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var goal = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            var offer = system.SubmitOffer(
                world,
                goal.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var order = system.EvaluateAndDispatch(world, goal.Id);
            var legacyJson = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 27");

            var migrated = WorldSnapshotSerializer.Deserialize(legacyJson);
            var migratedGoal = migrated.MilitaryLogisticsDelegationGoals.Find(
                item => item.Id == goal.Id);
            var migratedOffer = migrated.MilitaryLogisticsDelegationOffers.Find(
                item => item.Id == offer.Id);
            var migratedOrder = migrated.MilitaryLogisticsOrders.Find(
                item => item.Id == order.Id);

            Assert.That(migratedGoal.ReplacementProcurementPolicyId,
                Is.EqualTo(MilitaryLogisticsReplacementProcurementPolicyIds
                    .LegacyUnrestricted));
            Assert.That(migratedGoal.CompensationReceived, Is.Zero);
            Assert.That(migratedOffer.LiabilityPolicyId,
                Is.EqualTo(MilitaryLogisticsLiabilityPolicyIds
                    .LegacyNoRetroactiveSettlement));
            Assert.That(migratedOrder.LiabilityPolicyId,
                Is.EqualTo(MilitaryLogisticsLiabilityPolicyIds
                    .LegacyNoRetroactiveSettlement));
            Assert.That(migrated.MilitaryLogisticsLiabilitySettlements,
                Is.Empty);
            migrated.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_DueGoalsUseStableIdOrder()
        {
            var world = PrepareDelegatedLogisticsWorld();
            var system = new MilitaryLogisticsDelegationSystem();
            var firstRequest = DelegatedLogisticsGoal(
                MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                1_000,
                10);
            firstRequest.ReportIntervalDays = 1;
            var secondRequest = DelegatedLogisticsGoal(
                MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                1_000,
                10);
            secondRequest.ReportIntervalDays = 1;
            var first = system.CreateGoal(world, firstRequest);
            var second = system.CreateGoal(world, secondRequest);
            world.AdvanceOneDay();

            system.ProcessDue(world);

            var reportedGoalIds = new List<string>();
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationReports.Count;
                 i++)
            {
                var report = world.MilitaryLogisticsDelegationReports[i];
                if (report.Day == world.AbsoluteDay && report.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.NoOffer)
                {
                    reportedGoalIds.Add(report.GoalId);
                }
            }
            Assert.That(reportedGoalIds,
                Is.EqualTo(new[] { first.Id, second.Id }));
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_SplitsByStableAssigneeAndBudget()
        {
            var world = PrepareDelegatedLogisticsWorld();
            var system = new MilitaryLogisticsDelegationSystem();
            var root = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            var commanders = DelegatedFormationCommanderIds(world);
            var children = system.DelegateGoal(
                world,
                root.Id,
                new StableId(root.AssigneePersonId),
                new[]
                {
                    DelegatedSubgoal(commanders[1], 60, 600),
                    DelegatedSubgoal(commanders[0], 40, 400)
                });

            Assert.That(root.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Delegated));
            Assert.That(children.ConvertAll(item => item.AssigneePersonId),
                Is.EqualTo(commanders));
            Assert.That(children.ConvertAll(item => item.Id),
                Is.EqualTo(root.ChildGoalIds));
            Assert.That(children[0].ParentGoalId, Is.EqualTo(root.Id));
            Assert.That(children[0].DelegationDepth, Is.EqualTo(1));
            Assert.That(children[0].DelegatedByPersonId,
                Is.EqualTo(root.AssigneePersonId));
            Assert.That(children[0].AssigneeAuthorityAtDelegation,
                Is.EqualTo(MilitaryAuthorityLevel.Formation));
            Assert.That(children[0].TargetArmyId,
                Is.EqualTo(root.TargetArmyId));
            Assert.That(children[0].ProductDefinitionId,
                Is.EqualTo(root.ProductDefinitionId));
            Assert.That(children[0].RequestedCargoQuantity +
                        children[1].RequestedCargoQuantity,
                Is.EqualTo(root.RequestedCargoQuantity));
            Assert.That(children[0].BudgetLimit + children[1].BudgetLimit,
                Is.EqualTo(root.BudgetLimit));
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == root.Id && item.RelatedGoalId == children[0].Id &&
                item.TypeId == MilitaryLogisticsDelegationReportTypeIds
                    .SubgoalCreated), Is.True);
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == children[0].Id && item.RelatedGoalId == root.Id &&
                item.TypeId == MilitaryLogisticsDelegationReportTypeIds
                    .GoalCreated), Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                system.SubmitOffer(
                    world,
                    root.Id,
                    DelegatedMerchantOffer(
                        "person.zhang_shiping",
                        "product_batch.logistics.merchant_cargo",
                        "product_batch.logistics.merchant_provisions",
                        "route.zhongshan_anping",
                        2)));
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_TwoLevelsCompleteBottomUp()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var content = ZeroPerishabilityContent();
            var system = new MilitaryLogisticsDelegationSystem(content);
            var request = DelegatedLogisticsGoal(
                MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                1_000,
                10);
            request.ReportIntervalDays = 1;
            var root = system.CreateGoal(world, request);
            var formationCommander = DelegatedFormationCommanderIds(world)[0];
            var middle = system.DelegateGoal(
                world,
                root.Id,
                new StableId(root.AssigneePersonId),
                new[] { DelegatedSubgoal(formationCommander, 100, 1_000) })[0];
            var leafAssignee = DelegatedSelfAssigneeId(
                world, formationCommander);
            var leaf = system.DelegateGoal(
                world,
                middle.Id,
                new StableId(middle.AssigneePersonId),
                new[] { DelegatedSubgoal(leafAssignee, 100, 1_000) })[0];
            system.SubmitOffer(
                world,
                leaf.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var simulator = new WorldSimulator(world.MasterSeed, content);

            simulator.AdvanceDays(world, 1);
            Assert.That(leaf.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Dispatched));
            simulator.AdvanceDays(world, 10);

            Assert.That(leaf.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Fulfilled));
            Assert.That(middle.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Fulfilled));
            Assert.That(root.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Fulfilled));
            Assert.That(middle.FulfilledDay,
                Is.GreaterThanOrEqualTo(leaf.FulfilledDay));
            Assert.That(root.FulfilledDay,
                Is.GreaterThanOrEqualTo(middle.FulfilledDay));
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == root.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.Fulfilled),
                Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_InvalidSplitAndDepthAreAtomic()
        {
            var world = PrepareDelegatedLogisticsWorld();
            var system = new MilitaryLogisticsDelegationSystem();
            var root = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            var commanders = DelegatedFormationCommanderIds(world);
            var beforeInvalidQuantity = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                system.DelegateGoal(
                    world,
                    root.Id,
                    new StableId(root.AssigneePersonId),
                    new[]
                    {
                        DelegatedSubgoal(commanders[0], 50, 500),
                        DelegatedSubgoal(commanders[1], 40, 400)
                    }));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(beforeInvalidQuantity));

            var middle = system.DelegateGoal(
                world,
                root.Id,
                new StableId(root.AssigneePersonId),
                new[] { DelegatedSubgoal(commanders[0], 100, 1_000) })[0];
            var beforePeer = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() =>
                system.DelegateGoal(
                    world,
                    middle.Id,
                    new StableId(middle.AssigneePersonId),
                    new[] { DelegatedSubgoal(commanders[1], 100, 1_000) }));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(beforePeer));

            var leaf = system.DelegateGoal(
                world,
                middle.Id,
                new StableId(middle.AssigneePersonId),
                new[]
                {
                    DelegatedSubgoal(
                        DelegatedSelfAssigneeId(world, commanders[0]),
                        100,
                        1_000)
                })[0];
            var beforeTooDeep = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() =>
                system.DelegateGoal(
                    world,
                    leaf.Id,
                    new StableId(leaf.AssigneePersonId),
                    new[] { DelegatedSubgoal("person.zou_jing", 100, 1_000) }));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(beforeTooDeep));
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_AssigneeAuthorityLossStopsDispatch()
        {
            var world = PrepareDelegatedLogisticsWorld();
            var system = new MilitaryLogisticsDelegationSystem();
            var root = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            var formationCommander = DelegatedFormationCommanderIds(world)[0];
            var child = system.DelegateGoal(
                world,
                root.Id,
                new StableId(root.AssigneePersonId),
                new[] { DelegatedSubgoal(formationCommander, 100, 1_000) })[0];
            system.SubmitOffer(
                world,
                child.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var formation = world.MilitaryFormations.Find(item =>
                item.CommanderPersonId == formationCommander &&
                item.Kind == MilitaryFormationKind.Unit);
            var replacementCommanderId = DelegatedSelfAssigneeId(
                world, formationCommander);
            formation.CommanderPersonId = replacementCommanderId;
            world.MilitaryServices.Find(item =>
                item.PersonId == replacementCommanderId &&
                item.ArmyId == formation.ArmyId).Role =
                MilitaryServiceRole.Officer;
            world.Validate();

            var order = system.EvaluateAndDispatch(world, child.Id);

            Assert.That(order, Is.Null);
            Assert.That(world.MilitaryLogisticsOrders, Is.Empty);
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == child.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds
                        .AssigneeUnavailable), Is.True);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_CancellationRecoversAllocationAndClosesOffers()
        {
            var world = PrepareDelegatedLogisticsWorld();
            var system = new MilitaryLogisticsDelegationSystem();
            var root = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            root.ReportIntervalDays = 1;
            var commanders = DelegatedFormationCommanderIds(world);
            var children = system.DelegateGoal(
                world,
                root.Id,
                new StableId(root.AssigneePersonId),
                new[]
                {
                    DelegatedSubgoal(commanders[0], 40, 400),
                    DelegatedSubgoal(commanders[1], 60, 600)
                });
            var offer = system.SubmitOffer(
                world,
                children[0].Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            var cargo = world.ProductBatches.Find(item =>
                item.Id == offer.SourceCargoBatchId);
            var quantityBefore = cargo.Quantity;
            var reservedBefore = cargo.ReservedQuantity;
            var treasuryBefore = world.Organizations.Find(item =>
                item.Id == "organization.youzhou_field_force").Treasury;

            system.CancelUncommittedSubgoal(
                world,
                root.Id,
                children[0].Id,
                new StableId(root.AssigneePersonId),
                MilitaryLogisticsCancellationReasonIds.NoViableOffer);

            Assert.That(children[0].Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Cancelled));
            Assert.That(children[0].CancelledDay,
                Is.EqualTo(world.AbsoluteDay));
            Assert.That(children[0].CancelledByPersonId,
                Is.EqualTo(root.AssigneePersonId));
            Assert.That(children[0].CancellationReasonId,
                Is.EqualTo(
                    MilitaryLogisticsCancellationReasonIds.NoViableOffer));
            Assert.That(offer.Status,
                Is.EqualTo(
                    MilitaryLogisticsDelegationOfferStatus.GoalCancelled));
            Assert.That(offer.ClosedDay, Is.EqualTo(world.AbsoluteDay));
            Assert.That(root.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.NeedsAttention));
            Assert.That(root.UnassignedCargoQuantity, Is.EqualTo(40));
            Assert.That(root.AvailableBudgetReserve, Is.EqualTo(400));
            Assert.That(cargo.Quantity, Is.EqualTo(quantityBefore));
            Assert.That(cargo.ReservedQuantity, Is.EqualTo(reservedBefore));
            Assert.That(world.Organizations.Find(item =>
                    item.Id == "organization.youzhou_field_force").Treasury,
                Is.EqualTo(treasuryBefore));
            Assert.That(world.MilitaryLogisticsOrders, Is.Empty);
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == root.Id && item.RelatedGoalId == children[0].Id &&
                item.TypeId == MilitaryLogisticsDelegationReportTypeIds
                    .AllocationRecovered), Is.True);

            world.AdvanceOneDay();
            system.ProcessDue(world);
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == root.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.AllocationGap),
                Is.True);
            Assert.That(world.MilitaryLogisticsDelegationReports.Exists(item =>
                item.GoalId == root.Id && item.TypeId ==
                    MilitaryLogisticsDelegationReportTypeIds.NoOffer),
                Is.False);
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_ReassignmentIsStableAndCompletes()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var content = ZeroPerishabilityContent();
            var system = new MilitaryLogisticsDelegationSystem(content);
            var request = DelegatedLogisticsGoal(
                MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                1_000,
                10);
            request.ReportIntervalDays = 1;
            var root = system.CreateGoal(world, request);
            var commanders = DelegatedFormationCommanderIds(world);
            var cancelled = system.DelegateGoal(
                world,
                root.Id,
                new StableId(root.AssigneePersonId),
                new[] { DelegatedSubgoal(commanders[0], 100, 1_000) })[0];
            system.CancelUncommittedSubgoal(
                world,
                root.Id,
                cancelled.Id,
                new StableId(root.AssigneePersonId),
                MilitaryLogisticsCancellationReasonIds.SuperiorReassignment);

            var replacements = system.ReassignCancelledSubgoal(
                world,
                root.Id,
                cancelled.Id,
                new StableId(root.AssigneePersonId),
                new[]
                {
                    DelegatedSubgoal(commanders[1], 60, 600),
                    DelegatedSubgoal(commanders[0], 40, 400)
                });

            Assert.That(replacements.ConvertAll(item => item.AssigneePersonId),
                Is.EqualTo(commanders));
            Assert.That(replacements[0].ReplacesGoalId,
                Is.EqualTo(cancelled.Id));
            Assert.That(cancelled.ReplacementGoalIds,
                Is.EqualTo(replacements.ConvertAll(item => item.Id)));
            Assert.That(root.UnassignedCargoQuantity, Is.Zero);
            Assert.That(root.AvailableBudgetReserve, Is.Zero);
            Assert.That(root.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Delegated));
            Assert.That(root.ChildGoalIds, Has.Count.EqualTo(3));

            system.SubmitOffer(
                world,
                replacements[0].Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            system.SubmitOffer(
                world,
                replacements[1].Id,
                DelegatedMerchantOffer(
                    "person.su_shuang",
                    "product_batch.delegation.su_cargo",
                    "product_batch.delegation.su_provisions",
                    "route.zhongshan_anping.safe_delegation_test",
                    3));
            var simulator = new WorldSimulator(world.MasterSeed, content);
            simulator.AdvanceDays(world, 1);
            Assert.That(replacements.TrueForAll(item => item.Status ==
                    MilitaryLogisticsDelegationStatus.Dispatched), Is.True);
            simulator.AdvanceDays(world, 10);

            Assert.That(replacements.TrueForAll(item => item.Status ==
                    MilitaryLogisticsDelegationStatus.Fulfilled), Is.True);
            Assert.That(root.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Fulfilled));
            Assert.That(cancelled.Status,
                Is.EqualTo(MilitaryLogisticsDelegationStatus.Cancelled));
            var json = WorldSnapshotSerializer.Serialize(world);
            Assert.That(WorldSnapshotSerializer.Serialize(
                    WorldSnapshotSerializer.Deserialize(json)),
                Is.EqualTo(json));
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_CancellationRejectsUnauthorizedAndDispatched()
        {
            var world = PrepareDelegatedLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var system = new MilitaryLogisticsDelegationSystem();
            var root = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            var child = system.DelegateGoal(
                world,
                root.Id,
                new StableId(root.AssigneePersonId),
                new[]
                {
                    DelegatedSubgoal(
                        DelegatedFormationCommanderIds(world)[0],
                        100,
                        1_000)
                })[0];
            var beforeUnauthorized = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() =>
                system.CancelUncommittedSubgoal(
                    world,
                    root.Id,
                    child.Id,
                    new StableId(child.AssigneePersonId),
                    MilitaryLogisticsCancellationReasonIds
                        .SuperiorReassignment));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(beforeUnauthorized));

            system.SubmitOffer(
                world,
                child.Id,
                DelegatedMerchantOffer(
                    "person.zhang_shiping",
                    "product_batch.logistics.merchant_cargo",
                    "product_batch.logistics.merchant_provisions",
                    "route.zhongshan_anping",
                    2));
            system.EvaluateAndDispatch(world, child.Id);
            var beforeDispatched = WorldSnapshotSerializer.Serialize(world);
            Assert.Throws<InvalidOperationException>(() =>
                system.CancelUncommittedSubgoal(
                    world,
                    root.Id,
                    child.Id,
                    new StableId(root.AssigneePersonId),
                    MilitaryLogisticsCancellationReasonIds.NoViableOffer));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(beforeDispatched));
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_InvalidReassignmentIsAtomic()
        {
            var world = PrepareDelegatedLogisticsWorld();
            var system = new MilitaryLogisticsDelegationSystem();
            var root = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            var commander = DelegatedFormationCommanderIds(world)[0];
            var cancelled = system.DelegateGoal(
                world,
                root.Id,
                new StableId(root.AssigneePersonId),
                new[] { DelegatedSubgoal(commander, 100, 1_000) })[0];
            system.CancelUncommittedSubgoal(
                world,
                root.Id,
                cancelled.Id,
                new StableId(root.AssigneePersonId),
                MilitaryLogisticsCancellationReasonIds.NoViableOffer);
            var before = WorldSnapshotSerializer.Serialize(world);

            Assert.Throws<InvalidOperationException>(() =>
                system.ReassignCancelledSubgoal(
                    world,
                    root.Id,
                    cancelled.Id,
                    new StableId(root.AssigneePersonId),
                    new[] { DelegatedSubgoal(commander, 90, 900) }));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            Assert.Throws<InvalidOperationException>(() =>
                system.ReassignCancelledSubgoal(
                    world,
                    root.Id,
                    cancelled.Id,
                    new StableId(root.AssigneePersonId),
                    new[] { DelegatedSubgoal(commander, 100, 1_001) }));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            Assert.Throws<InvalidOperationException>(() =>
                system.ReassignCancelledSubgoal(
                    world,
                    root.Id,
                    cancelled.Id,
                    new StableId(root.AssigneePersonId),
                    new[]
                    {
                        DelegatedSubgoal(
                            root.AssigneePersonId,
                            100,
                            1_000)
                    }));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            world.Validate();
        }

        [Test]
        public void MilitaryLogisticsDelegation_V25MigrationDerivesBudgetReserve()
        {
            var world = PrepareDelegatedLogisticsWorld();
            var system = new MilitaryLogisticsDelegationSystem();
            var root = system.CreateGoal(
                world,
                DelegatedLogisticsGoal(
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost,
                    1_000,
                    10));
            var commanders = DelegatedFormationCommanderIds(world);
            system.DelegateGoal(
                world,
                root.Id,
                new StableId(root.AssigneePersonId),
                new[]
                {
                    DelegatedSubgoal(commanders[0], 40, 300),
                    DelegatedSubgoal(commanders[1], 60, 500)
                });
            var v25 = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 25");

            var migrated = WorldSnapshotSerializer.Deserialize(v25);
            var migratedRoot = migrated.MilitaryLogisticsDelegationGoals.Find(
                item => item.Id == root.Id);

            Assert.That(migratedRoot.UnassignedCargoQuantity, Is.Zero);
            Assert.That(migratedRoot.AvailableBudgetReserve, Is.EqualTo(200));
            Assert.That(migratedRoot.ReplacementGoalIds, Is.Empty);
            migrated.Validate();
        }

        private static MilitaryLogisticsSubgoalRequest DelegatedSubgoal(
            string assigneePersonId,
            int quantity,
            long budget)
        {
            return new MilitaryLogisticsSubgoalRequest
            {
                AssigneePersonId = new StableId(assigneePersonId),
                RequestedCargoQuantity = quantity,
                MaximumUnitPrice = 10,
                BudgetLimit = budget,
                DeadlineDay = 30,
                ReportIntervalDays = 1,
                CarrierPreferenceId =
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost
            };
        }

        private static ProductionContentRegistry LoadHanFoodProductionContent()
        {
            var registry = ProductionContentRegistry.CreateCore();
            registry.Register(ProductionContentJson.DeserializePackage(
                File.ReadAllText(HanFoodProductionContentPath())));
            return registry;
        }

        private static string HanFoodProductionContentPath()
        {
            return Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "Resources",
                "Content",
                "Scenario",
                "HanFood",
                "han-food-production.json");
        }

        private static ProductionContentRegistry ZeroPerishabilityContent()
        {
            var content = ProductionContentRegistry.CreateCore();
            content.GetProduct(CoreProductionContent.DryRationProductId)
                .PerishabilityBasisPoints = 0;
            return content;
        }

        private static List<string> DelegatedFormationCommanderIds(
            WorldState world)
        {
            var result = world.MilitaryFormations.FindAll(item =>
                    item.ArmyId == "army.youzhou_reinforcement" &&
                    item.Kind == MilitaryFormationKind.Unit)
                .ConvertAll(item => item.CommanderPersonId);
            result.Sort(StringComparer.Ordinal);
            Assert.That(result.Count, Is.GreaterThanOrEqualTo(2));
            return result;
        }

        private static string DelegatedSelfAssigneeId(
            WorldState world,
            string formationCommanderId)
        {
            var formation = world.MilitaryFormations.Find(item =>
                item.ArmyId == "army.youzhou_reinforcement" &&
                item.Kind == MilitaryFormationKind.Unit &&
                item.CommanderPersonId == formationCommanderId);
            Assert.That(formation, Is.Not.Null);
            var candidates = world.MilitaryServices.FindAll(item =>
                    item.ArmyId == formation.ArmyId &&
                    item.FormationId == formation.Id &&
                    item.PersonId != formationCommanderId &&
                    (item.Status == MilitaryServiceStatus.Active ||
                     item.Status == MilitaryServiceStatus.Mustering))
                .ConvertAll(item => item.PersonId);
            candidates.Sort(StringComparer.Ordinal);
            Assert.That(candidates, Is.Not.Empty);
            return candidates[0];
        }

        private static WorldState PrepareDelegatedLogisticsWorld()
        {
            var world = PrepareMerchantLogisticsWorld();
            var secondCarrier = world.People.Find(item =>
                item.Id == "person.su_shuang");
            const string secondContainerId =
                "inventory_container.delegation.su_shuang";
            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = secondContainerId,
                KindId = MilitaryProcurementSystem.CaravanContainerKindId,
                OwnerOrganizationId = "organization.zhongshan_merchants",
                CarrierPersonId = secondCarrier.Id,
                LocationId = "location.zhongshan",
                CapacityWeight = 2_000
            });
            AddMilitaryLogisticsBatch(
                world,
                "product_batch.delegation.su_cargo",
                "organization.zhongshan_merchants",
                secondContainerId,
                200);
            AddMilitaryLogisticsBatch(
                world,
                "product_batch.delegation.su_provisions",
                "organization.zhongshan_merchants",
                secondContainerId,
                40);
            const string armyContainerId =
                "inventory_container.delegation.youzhou";
            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = armyContainerId,
                KindId = MilitaryProcurementSystem.CaravanContainerKindId,
                OwnerOrganizationId = "organization.youzhou_field_force",
                CarrierPersonId = "person.zou_jing",
                LocationId = "location.zhongshan",
                CapacityWeight = 2_000
            });
            AddMilitaryLogisticsBatch(
                world,
                "product_batch.delegation.army_provisions",
                "organization.youzhou_field_force",
                armyContainerId,
                40);
            world.Routes.Find(item =>
                    item.Id == "route.zhongshan_anping")
                .SecurityBasisPoints = 2_000;
            world.Routes.Add(new RouteState
            {
                Id = "route.zhongshan_anping.safe_delegation_test",
                FromLocationId = "location.zhongshan",
                ToLocationId = "location.anping",
                DistanceKilometers = 130,
                Bidirectional = true,
                SecurityBasisPoints = 9_000
            });
            world.Validate();
            return world;
        }

        private static MilitaryLogisticsDelegationGoalRequest
            DelegatedLogisticsGoal(
                string preferenceId,
                long budget,
                long maximumUnitPrice)
        {
            return new MilitaryLogisticsDelegationGoalRequest
            {
                IssuerPersonId = new StableId("person.zou_jing"),
                TargetArmyId = new StableId("army.youzhou_reinforcement"),
                DestinationLocationId = new StableId("location.anping"),
                ProductDefinitionId = CoreProductionContent.DryRationProductId,
                RequestedCargoQuantity = 100,
                MaximumUnitPrice = maximumUnitPrice,
                BudgetLimit = budget,
                DeadlineDay = 30,
                ReportIntervalDays = 5,
                CarrierPreferenceId = preferenceId
            };
        }

        private static MilitaryLogisticsDelegationOfferRequest
            DelegatedMerchantOffer(
                string carrierPersonId,
                string cargoBatchId,
                string provisionBatchId,
                string routeId,
                long unitPrice)
        {
            return new MilitaryLogisticsDelegationOfferRequest
            {
                CarrierPersonId = new StableId(carrierPersonId),
                SourceCargoBatchId = new StableId(cargoBatchId),
                SourceProvisionBatchId = provisionBatchId,
                RouteId = new StableId(routeId),
                AcquisitionMethodId =
                    MilitarySupplyAcquisitionMethodIds.CommercialPurchase,
                CarrierOrganizationId =
                    "organization.zhongshan_merchants",
                LossBearerOrganizationId =
                    "organization.zhongshan_merchants",
                CargoQuantity = 100,
                ConvoyProvisionQuantity = 20,
                DailyConvoyProvisionUse = 2,
                UnitPrice = unitPrice
            };
        }

        private static void TransferPrototypeArmyCommandAwayFromZouJing(
            WorldState world)
        {
            var army = world.Armies.Find(item =>
                item.Id == "army.youzhou_reinforcement");
            var root = world.MilitaryFormations.Find(item =>
                item.ArmyId == army.Id &&
                item.Kind == MilitaryFormationKind.Army);
            var oldCommander = world.MilitaryServices.Find(item =>
                item.PersonId == "person.zou_jing" &&
                item.ArmyId == army.Id);
            var replacement = world.MilitaryServices.Find(item =>
                item.ArmyId == army.Id &&
                item.Role == MilitaryServiceRole.Soldier &&
                item.PersonId != oldCommander.PersonId &&
                (item.Status == MilitaryServiceStatus.Active ||
                 item.Status == MilitaryServiceStatus.Mustering));
            oldCommander.Role = MilitaryServiceRole.Soldier;
            replacement.Role = MilitaryServiceRole.Commander;
            replacement.FormationId = root.Id;
            army.CommanderPersonId = replacement.PersonId;
            root.CommanderPersonId = replacement.PersonId;
            world.Validate();
        }

        private static WorldState PrepareSeizedLogisticsWorld()
        {
            var world = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            world.Routes.Find(item =>
                item.Id == "route.zhongshan_anping")
                .SecurityBasisPoints = 0;
            var request = MerchantLogisticsRequest(
                MilitarySupplyAcquisitionMethodIds.CommercialPurchase, 3);
            request.RiskPolicyId = MilitaryLogisticsRiskPolicyIds.Standard;
            request.ThreatOrganizationId =
                "organization.taiping_yellow_turban";
            new MilitaryLogisticsSystem().Dispatch(world, request);
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 4);
            Assert.That(world.MilitaryLogisticsIncidents[0].OutcomeId,
                Is.EqualTo(
                    MilitaryLogisticsIncidentOutcomeIds.CargoSeized));
            world.Validate();
            return world;
        }

        private static WorldState PrepareRecoveredLogisticsWorld()
        {
            var world = PrepareSeizedLogisticsWorld();
            var participants = PrepareStrongRecoveryParty(world, 4);
            new MilitaryLogisticsSystem().AttemptArmyRecovery(
                world,
                new StableId("person.zou_jing"),
                world.MilitaryLogisticsIncidents[0].Id,
                participants);
            world.Validate();
            return world;
        }

        private static List<string> PrepareStrongRecoveryParty(
            WorldState world,
            int count)
        {
            var ids = world.MilitaryServices.FindAll(item =>
                item.ArmyId == "army.youzhou_reinforcement" &&
                item.PersonId != "person.zou_jing" &&
                (item.Status == MilitaryServiceStatus.Active ||
                 item.Status == MilitaryServiceStatus.Mustering))
                .ConvertAll(item => item.PersonId);
            ids.Sort(StringComparer.Ordinal);
            ids = ids.GetRange(0, count);
            for (var i = 0; i < ids.Count; i++)
            {
                MaximizeLogisticsEscortAbility(world.People.Find(item =>
                    item.Id == ids[i]));
            }
            return ids;
        }

        private static void MinimizeLogisticsCombatAbility(PersonState person)
        {
            person.Aptitudes.Constitution = 0;
            person.Aptitudes.Strength = 0;
            person.Aptitudes.Dexterity = 0;
            person.Aptitudes.Reasoning = 0;
            person.Aptitudes.Willpower = 0;
            person.Aptitudes.Affinity = 0;
            person.ProfessionalSkills.Military = 0;
            person.ProfessionalSkills.MartialArts = 0;
            person.ProfessionalSkills.Administration = 0;
            person.ProfessionalSkills.Negotiation = 0;
        }

        private static WorldState PrepareEscortRiskWorld()
        {
            var world = PrepareMerchantLogisticsWorld();
            StartYouzhouArmyToAnping(world);
            var escort = world.People.Find(item =>
                item.Id == "person.su_shuang");
            MaximizeLogisticsEscortAbility(escort);
            var request = MerchantLogisticsRequest(
                MilitarySupplyAcquisitionMethodIds.CommercialPurchase, 3);
            request.RiskPolicyId = MilitaryLogisticsRiskPolicyIds.Standard;
            request.ThreatOrganizationId =
                "organization.taiping_yellow_turban";
            request.EscortPersonIds.Add(escort.Id);
            new MilitaryLogisticsSystem().Dispatch(world, request);
            world.Validate();
            return world;
        }

        private static void MaximizeLogisticsEscortAbility(PersonState person)
        {
            person.HealthBasisPoints = 10_000;
            person.Aptitudes.Constitution = 10_000;
            person.Aptitudes.Strength = 10_000;
            person.Aptitudes.Dexterity = 10_000;
            person.Aptitudes.Reasoning = 10_000;
            person.Aptitudes.Willpower = 10_000;
            person.Aptitudes.Affinity = 10_000;
            person.ProfessionalSkills.Military = 10_000;
            person.ProfessionalSkills.MartialArts = 10_000;
            person.ProfessionalSkills.Administration = 10_000;
            person.ProfessionalSkills.Negotiation = 10_000;
        }

        private static WorldState PrepareMultiLegLogisticsWorld()
        {
            var world = PrepareMerchantLogisticsWorld();
            var secondCarrier = world.People.Find(item =>
                item.Id == "person.su_shuang");
            new PopulationLedgerSystem().MoveIndependentPerson(
                world, secondCarrier, "location.anping");
            const string secondContainerId =
                "inventory_container.zhongshan_merchants.handoff_001";
            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = secondContainerId,
                KindId = MilitaryProcurementSystem.CaravanContainerKindId,
                OwnerOrganizationId = "organization.zhongshan_merchants",
                CarrierPersonId = secondCarrier.Id,
                LocationId = "location.anping",
                CapacityWeight = 2_000
            });
            AddMilitaryLogisticsBatch(
                world,
                "product_batch.logistics.handoff_provisions",
                "organization.zhongshan_merchants",
                secondContainerId,
                40);
            world.Routes.Add(new RouteState
            {
                Id = "route.zhongshan_guangzong.logistics_test",
                FromLocationId = "location.zhongshan",
                ToLocationId = "location.guangzong",
                DistanceKilometers = 170,
                Bidirectional = true,
                SecurityBasisPoints = 5_000
            });
            new ArmySystem().StartMarch(
                world,
                new StableId("person.zou_jing"),
                new StableId("army.youzhou_reinforcement"),
                new StableId("route.zhongshan_guangzong.logistics_test"),
                new StableId("location.guangzong"));
            world.Validate();
            return world;
        }

        private static MilitaryLogisticsDispatchRequest
            MultiLegLogisticsRequest()
        {
            var request = MerchantLogisticsRequest(
                MilitarySupplyAcquisitionMethodIds.CommercialPurchase, 3);
            request.AdditionalLegs.Add(new MilitaryLogisticsLegRequest
            {
                CarrierPersonId = new StableId("person.su_shuang"),
                CarrierOrganizationId =
                    "organization.zhongshan_merchants",
                RouteId = new StableId("route.anping_guangzong"),
                DestinationLocationId = new StableId("location.guangzong"),
                SourceProvisionBatchId =
                    "product_batch.logistics.handoff_provisions",
                ConvoyProvisionQuantity = 12,
                DailyConvoyProvisionUse = 2
            });
            return request;
        }

        private static void AdvanceLogisticsUntilNotInTransit(
            WorldState world,
            MilitaryLogisticsOrderState order,
            int maximumSegments)
        {
            var simulator = new WorldSimulator(world.MasterSeed);
            for (var i = 0;
                 i < maximumSegments &&
                 order.Status == MilitaryLogisticsStatus.InTransit;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }

            Assert.That(order.Status,
                Is.Not.EqualTo(MilitaryLogisticsStatus.InTransit),
                "Logistics leg did not complete within the bounded test window.");
        }

        private static void ExecutePrototypeMilitaryLogistics(WorldState world)
        {
            StartYouzhouArmyToAnping(world);
            new MilitaryLogisticsSystem().Dispatch(
                world,
                MerchantLogisticsRequest(
                    MilitarySupplyAcquisitionMethodIds.CommercialPurchase,
                    3));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 18);
        }

        private static WorldState PrepareMerchantLogisticsWorld()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var container = world.InventoryContainers.Find(item =>
                item.Id == MilitaryProcurementSystem.PrototypeContainerId);
            container.CapacityWeight = 2_000;
            AddMilitaryLogisticsBatch(
                world,
                "product_batch.logistics.merchant_cargo",
                "organization.zhongshan_merchants",
                container.Id,
                200);
            AddMilitaryLogisticsBatch(
                world,
                "product_batch.logistics.merchant_provisions",
                "organization.zhongshan_merchants",
                container.Id,
                40);
            world.Validate();
            return world;
        }

        private static WorldState PrepareArmyLogisticsWorld(
            bool includeMerchantCargo = false)
        {
            var world = PrototypeWorldFactory.Create184World(184);
            const string containerId =
                "inventory_container.youzhou_field_force.train_001";
            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = containerId,
                KindId = MilitaryProcurementSystem.CaravanContainerKindId,
                OwnerOrganizationId = "organization.youzhou_field_force",
                CarrierPersonId = "person.zou_jing",
                LocationId = "location.zhongshan",
                CapacityWeight = 2_000
            });
            AddMilitaryLogisticsBatch(
                world,
                "product_batch.logistics.army_cargo",
                "organization.youzhou_field_force",
                containerId,
                200);
            AddMilitaryLogisticsBatch(
                world,
                "product_batch.logistics.army_provisions",
                "organization.youzhou_field_force",
                containerId,
                40);
            if (includeMerchantCargo)
            {
                var merchantContainer = world.InventoryContainers.Find(item =>
                    item.Id == MilitaryProcurementSystem.PrototypeContainerId);
                merchantContainer.CapacityWeight = 2_000;
                AddMilitaryLogisticsBatch(
                    world,
                    "product_batch.logistics.merchant_cargo",
                    "organization.zhongshan_merchants",
                    merchantContainer.Id,
                    100);
            }

            world.Validate();
            return world;
        }

        private static MilitaryLogisticsDispatchRequest
            MerchantLogisticsRequest(string methodId, long unitPrice)
        {
            return new MilitaryLogisticsDispatchRequest
            {
                IssuerPersonId = new StableId("person.zou_jing"),
                CarrierPersonId = new StableId("person.zhang_shiping"),
                TargetArmyId = new StableId("army.youzhou_reinforcement"),
                SourceCargoBatchId = new StableId(
                    "product_batch.logistics.merchant_cargo"),
                SourceProvisionBatchId =
                    "product_batch.logistics.merchant_provisions",
                RouteId = new StableId("route.zhongshan_anping"),
                DestinationLocationId = new StableId("location.anping"),
                AcquisitionMethodId = methodId,
                CarrierOrganizationId =
                    "organization.zhongshan_merchants",
                LossBearerOrganizationId =
                    "organization.zhongshan_merchants",
                CargoQuantity = 100,
                ConvoyProvisionQuantity = 20,
                DailyConvoyProvisionUse = 2,
                UnitPrice = unitPrice
            };
        }

        private static MilitaryLogisticsDispatchRequest
            ArmyLogisticsRequest(string methodId, long unitPrice)
        {
            return new MilitaryLogisticsDispatchRequest
            {
                IssuerPersonId = new StableId("person.zou_jing"),
                CarrierPersonId = new StableId("person.zou_jing"),
                TargetArmyId = new StableId("army.youzhou_reinforcement"),
                SourceCargoBatchId = new StableId(
                    "product_batch.logistics.army_cargo"),
                SourceProvisionBatchId =
                    "product_batch.logistics.army_provisions",
                RouteId = new StableId("route.zhongshan_anping"),
                DestinationLocationId = new StableId("location.anping"),
                AcquisitionMethodId = methodId,
                CarrierOrganizationId =
                    "organization.youzhou_field_force",
                LossBearerOrganizationId =
                    "organization.youzhou_field_force",
                CargoQuantity = 100,
                ConvoyProvisionQuantity = 20,
                DailyConvoyProvisionUse = 2,
                UnitPrice = unitPrice
            };
        }

        private static void StartYouzhouArmyToAnping(WorldState world)
        {
            new ArmySystem().StartMarch(
                world,
                new StableId("person.zou_jing"),
                new StableId("army.youzhou_reinforcement"),
                new StableId("route.zhongshan_anping"),
                new StableId("location.anping"));
        }

        private static void AddMilitaryLogisticsBatch(
            WorldState world,
            string batchId,
            string ownerOrganizationId,
            string containerId,
            int quantity)
        {
            var transactionId =
                "inventory_transaction." + batchId + ".opening";
            var product = ProductionContentRegistry.CreateCore().GetProduct(
                CoreProductionContent.DryRationProductId);
            var batch = new ProductBatchState
            {
                Id = batchId,
                ProductDefinitionId = product.Id,
                OwnerOrganizationId = ownerOrganizationId,
                InventoryContainerId = containerId,
                OriginLocationId = "location.zhongshan",
                SourceTransactionId = transactionId,
                UnitId = product.UnitId,
                UnitWeight = product.BaseWeight,
                ProducedDay = world.AbsoluteDay,
                Quantity = quantity,
                QualityBasisPoints = 8_500,
                FreshnessBasisPoints = 9_500,
                QualityDimensions = ProductQualityRules.CreateUniform(
                    product, 8_500)
            };
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = transactionId,
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.OpeningBalance,
                ActorPersonId = "person.zhang_shiping",
                Summary = "Military logistics test opening balance.",
                Lines =
                {
                    new InventoryTransactionLineState
                    {
                        BatchId = batch.Id,
                        ProductDefinitionId = batch.ProductDefinitionId,
                        OwnerOrganizationId = batch.OwnerOrganizationId,
                        InventoryContainerId = batch.InventoryContainerId,
                        UnitId = batch.UnitId,
                        QuantityDelta = quantity
                    }
                }
            });
        }

        private static void AddMilitaryMedicalLogisticsBatch(
            WorldState world,
            int quantity)
        {
            const string batchId =
                "product_batch.logistics.merchant_medicine";
            var transactionId =
                "inventory_transaction." + batchId + ".opening";
            var product = ProductionContentRegistry.CreateCore().GetProduct(
                CoreProductionContent.HerbalMedicineMaterialProductId);
            var container = world.InventoryContainers.Find(item =>
                item.Id == MilitaryProcurementSystem.PrototypeContainerId);
            var batch = new ProductBatchState
            {
                Id = batchId,
                ProductDefinitionId = product.Id,
                OwnerOrganizationId =
                    "organization.zhongshan_merchants",
                InventoryContainerId = container.Id,
                OriginLocationId = "location.zhongshan",
                SourceTransactionId = transactionId,
                UnitId = product.UnitId,
                UnitWeight = product.BaseWeight,
                ProducedDay = world.AbsoluteDay,
                Quantity = quantity,
                QualityBasisPoints = 8_500,
                FreshnessBasisPoints = 9_500,
                QualityDimensions = ProductQualityRules.CreateUniform(
                    product, 8_500)
            };
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = transactionId,
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.OpeningBalance,
                ActorPersonId = "person.zhang_shiping",
                Summary = "Military medicine logistics test opening balance.",
                Lines =
                {
                    new InventoryTransactionLineState
                    {
                        BatchId = batch.Id,
                        ProductDefinitionId = batch.ProductDefinitionId,
                        OwnerOrganizationId = batch.OwnerOrganizationId,
                        InventoryContainerId = batch.InventoryContainerId,
                        UnitId = batch.UnitId,
                        QuantityDelta = quantity
                    }
                }
            });
            world.Validate();
        }

        private static MilitaryMedicalResupplyRequest
            MedicalResupplyRequest(
                int quantity,
                long unitPrice,
                bool autoDeliver)
        {
            return new MilitaryMedicalResupplyRequest
            {
                IssuerPersonId = new StableId("person.zou_jing"),
                CarrierPersonId = new StableId("person.zhang_shiping"),
                TargetArmyId = new StableId(
                    "army.youzhou_reinforcement"),
                SourceMedicineBatchId = new StableId(
                    "product_batch.logistics.merchant_medicine"),
                SourceProvisionBatchId =
                    "product_batch.logistics.merchant_provisions",
                RouteId = new StableId("route.zhongshan_anping"),
                DestinationLocationId = new StableId("location.anping"),
                AcquisitionMethodId = MilitarySupplyAcquisitionMethodIds
                    .CommercialPurchase,
                CarrierOrganizationId =
                    "organization.zhongshan_merchants",
                LossBearerOrganizationId =
                    "organization.zhongshan_merchants",
                MedicineQuantity = quantity,
                ConvoyProvisionQuantity = 8,
                DailyConvoyProvisionUse = 1,
                UnitPrice = unitPrice,
                AutoDeliverAtFinal = autoDeliver
            };
        }

        private static void ExecutePrototypeProcurement(WorldState world)
        {
            var armyId = new StableId("army.youzhou_reinforcement");
            var routeId = new StableId("route.zhongshan_anping");
            var destinationId = new StableId("location.anping");
            new ArmySystem().StartMarch(
                world,
                new StableId("person.zou_jing"),
                armyId,
                routeId,
                destinationId);
            new MilitaryProcurementSystem().CreateOrderAndDispatch(
                world,
                new StableId("person.zou_jing"),
                new StableId("person.zhang_shiping"),
                armyId,
                new StableId(MilitaryEquipmentSystem.LongSpearId),
                2,
                25,
                routeId,
                destinationId);
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 18);
        }

        private static void ExecutePrototypeUpstreamChain(WorldState world)
        {
            RemoveOpeningProduct(
                world, CoreProductionContent.IronMaterialProductId);
            RemoveOpeningProduct(
                world, CoreProductionContent.TimberMaterialProductId);
            world.Validate();
            var extraction = new UpstreamResourceProductionSystem();
            var ironOrder = extraction.CreateOrder(
                world,
                UpstreamResourceProductionSystem.PrototypeIronBodyId,
                UpstreamResourceProductionSystem.PrototypeIronMineSiteId,
                "person.su_shuang",
                new[] { "person.su_shuang" },
                ProductionControlMode.WorkOrder,
                12);
            var timberOrder = extraction.CreateOrder(
                world,
                UpstreamResourceProductionSystem.PrototypeForestBodyId,
                UpstreamResourceProductionSystem.PrototypeLoggingSiteId,
                "person.zhang_shiping",
                new[] { "person.zhang_shiping" },
                ProductionControlMode.WorkOrder,
                20);
            world.AbsoluteDay = Math.Max(
                ironOrder.FinishDay, timberOrder.FinishDay);
            extraction.ResolveDueOrders(world);

            var processing = new ProcessingProductionSystem();
            var charcoal = processing.CreateOrganizationOrder(
                world,
                CoreProductionContent.BurnCharcoalRecipeId,
                CoreProductionContent.EarthKilnCharcoalMethodId,
                "organization.zhongshan_merchants",
                UpstreamResourceProductionSystem.PrototypeCharcoalKilnSiteId,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                "person.zhang_shiping",
                ProductionControlMode.WorkOrder,
                6);
            world.AbsoluteDay = charcoal.FinishDay;
            processing.ResolveDueOrders(world);
            var smelt = processing.CreateOrganizationOrder(
                world,
                CoreProductionContent.SmeltBloomeryIronRecipeId,
                CoreProductionContent.BloomerySmeltingMethodId,
                "organization.zhongshan_merchants",
                UpstreamResourceProductionSystem.PrototypeBloomerySiteId,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                "person.su_shuang",
                ProductionControlMode.WorkOrder,
                4);
            world.AbsoluteDay = smelt.FinishDay;
            processing.ResolveDueOrders(world);
            var spears = processing.CreateOrganizationOrder(
                world,
                CoreProductionContent.ForgeLongSpearRecipeId,
                CoreProductionContent.BlacksmithingMethodId,
                "organization.zhongshan_merchants",
                MilitaryEquipmentRepairSystem.PrototypeWorkshopSiteId,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                "person.su_shuang",
                ProductionControlMode.WorkOrder,
                2);
            world.AbsoluteDay = spears.FinishDay;
            processing.ResolveDueOrders(world);
        }

        private static ProcessingWorkOrderState CreatePrototypeSpearOrder(
            WorldState world,
            ProcessingProductionSystem processing)
        {
            return processing.CreateOrganizationOrder(
                world,
                CoreProductionContent.ForgeLongSpearRecipeId,
                CoreProductionContent.BlacksmithingMethodId,
                "organization.zhongshan_merchants",
                MilitaryEquipmentRepairSystem.PrototypeWorkshopSiteId,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                "person.su_shuang",
                ProductionControlMode.WorkOrder,
                1);
        }

        private static void ExecutePrototypeLivestockChain(WorldState world)
        {
            RemoveOpeningProduct(
                world, CoreProductionContent.LeatherMaterialProductId);
            RemoveOpeningProduct(
                world, CoreProductionContent.HornMaterialProductId);
            world.Validate();
            var extraction = new UpstreamResourceProductionSystem();
            var fodder = extraction.CreateOrder(
                world,
                UpstreamResourceProductionSystem.PrototypePastureForageBodyId,
                UpstreamResourceProductionSystem.PrototypePastureForageSiteId,
                "person.zhang_shiping",
                new[] { "person.zhang_shiping" },
                ProductionControlMode.WorkOrder,
                20);
            var bark = extraction.CreateOrder(
                world,
                UpstreamResourceProductionSystem.PrototypeTanningBarkBodyId,
                UpstreamResourceProductionSystem.PrototypeBarkHarvestingSiteId,
                "person.su_shuang",
                new[] { "person.su_shuang" },
                ProductionControlMode.WorkOrder,
                2);
            world.AbsoluteDay = Math.Max(fodder.FinishDay, bark.FinishDay);
            extraction.ResolveDueOrders(world);

            var livestock = new LivestockProductionSystem();
            var processing = new ProcessingProductionSystem();
            var husbandry = livestock.CreateHusbandryOrder(
                world,
                "person.zhang_shiping",
                ProductionControlMode.TargetInstruction,
                1);
            world.AbsoluteDay = husbandry.FinishDay - 1;
            processing.ResolveDueOrders(world);
            Assert.That(husbandry.Status,
                Is.EqualTo(ProductionOrderStatus.Active));
            world.AbsoluteDay = husbandry.FinishDay;
            processing.ResolveDueOrders(world);

            var slaughter = livestock.CreateSlaughterOrder(
                world,
                "person.su_shuang",
                ProductionControlMode.WorkOrder,
                2);
            world.AbsoluteDay = slaughter.FinishDay;
            processing.ResolveDueOrders(world);
            var tanning = livestock.CreateTanningOrder(
                world,
                "person.zhang_shiping",
                ProductionControlMode.WorkOrder,
                2);
            var horn = livestock.CreateHornFinishingOrder(
                world,
                "person.su_shuang",
                ProductionControlMode.WorkOrder,
                1);
            world.AbsoluteDay = Math.Max(tanning.FinishDay, horn.FinishDay);
            processing.ResolveDueOrders(world);

            var shield = processing.CreateOrganizationOrder(
                world,
                CoreProductionContent.MakeWoodenShieldRecipeId,
                CoreProductionContent.WoodworkingMethodId,
                "organization.zhongshan_merchants",
                MilitaryEquipmentRepairSystem.PrototypeWorkshopSiteId,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                "person.su_shuang",
                ProductionControlMode.WorkOrder,
                1);
            var bow = processing.CreateOrganizationOrder(
                world,
                CoreProductionContent.MakeHornBowRecipeId,
                CoreProductionContent.BowmakingMethodId,
                "organization.zhongshan_merchants",
                MilitaryEquipmentRepairSystem.PrototypeWorkshopSiteId,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                "person.su_shuang",
                ProductionControlMode.WorkOrder,
                1);
            world.AbsoluteDay = Math.Max(shield.FinishDay, bow.FinishDay);
            processing.ResolveDueOrders(world);
            world.Validate();
        }

        private static long QuantityFromRecipe(
            WorldState world,
            string recipeId,
            string productDefinitionId)
        {
            var order = world.ProcessingWorkOrders.Find(item =>
                item.RecipeDefinitionId == recipeId);
            Assert.That(order, Is.Not.Null);
            long quantity = 0;
            var transaction = world.InventoryTransactions.Find(item =>
                item.SourceWorkOrderId == order.Id &&
                item.Type == InventoryTransactionType.RecipeSettled);
            Assert.That(transaction, Is.Not.Null);
            for (var i = 0; i < transaction.Lines.Count; i++)
            {
                var line = transaction.Lines[i];
                if (line.ProductDefinitionId == productDefinitionId &&
                    line.QuantityDelta > 0)
                {
                    quantity += line.QuantityDelta;
                }
            }

            return quantity;
        }

        private static void RemoveM23P3Prototype(WorldState world)
        {
            world.ResourceBodies.RemoveAll(item =>
                item.Id ==
                    UpstreamResourceProductionSystem.PrototypePastureForageBodyId ||
                item.Id ==
                    UpstreamResourceProductionSystem.PrototypeTanningBarkBodyId);
            world.ProductionSites.RemoveAll(item =>
                item.Id ==
                    UpstreamResourceProductionSystem.PrototypePastureForageSiteId ||
                item.Id ==
                    UpstreamResourceProductionSystem.PrototypeBarkHarvestingSiteId ||
                item.Id == LivestockProductionSystem.PrototypePastureSiteId ||
                item.Id == LivestockProductionSystem.PrototypeSlaughterYardSiteId ||
                item.Id == LivestockProductionSystem.PrototypeTannerySiteId ||
                item.Id == LivestockProductionSystem.PrototypeHornWorkshopSiteId);
            var flock = world.ProductBatches.Find(item =>
                item.Id == LivestockProductionSystem.PrototypeOpeningFlockBatchId);
            if (flock != null)
            {
                world.ProductBatches.Remove(flock);
                world.InventoryTransactions.RemoveAll(item =>
                    item.Id == flock.SourceTransactionId);
            }
        }

        private static void RemoveOpeningProduct(
            WorldState world,
            string productDefinitionId)
        {
            var batch = world.ProductBatches.Find(item =>
                item.ProductDefinitionId == productDefinitionId &&
                item.Id.StartsWith(
                    "product_batch.prototype_material.",
                    StringComparison.Ordinal));
            if (batch == null)
            {
                return;
            }

            world.ProductBatches.Remove(batch);
            world.InventoryTransactions.RemoveAll(item =>
                item.Id == batch.SourceTransactionId);
        }

        private static List<string> AvailableAgricultureWorkers(
            WorldState world,
            FamilyState family)
        {
            var result = new List<string>();
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var person = world.People.Find(
                    item => item.Id == family.MemberIds[i]);
                if (person.IsAlive &&
                    person.LocalDuty == LocalDutyKind.None &&
                    person.LaborCapacityBasisPoints > 0)
                {
                    result.Add(person.Id);
                }
            }

            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static ProductionContentPackageDefinition
            BuildTestProductionPackage(string packageId)
        {
            var package = new ProductionContentPackageDefinition
            {
                PackageId = packageId,
                Version = "1.0.0",
                LoadOrder = 100,
                Required = false
            };
            package.Crops.Add(new CropDefinition
            {
                Id = "crop.mod_test.example",
                DisplayName = "测试作物",
                HistoricalStatus = "test_only",
                SourceNote = "自动测试内容，不进入正式历史内容。",
                UsageTags = new List<string> { "usage.test" }
            });
            package.CropVarieties.Add(new CropVarietyDefinition
            {
                Id = "crop_variety.mod_test.example",
                CropDefinitionId = "crop.mod_test.example",
                DisplayName = "测试品种",
                Provenance = "test_only"
            });
            package.Products.Add(new ProductDefinition
            {
                Id = "product.mod_test.example_seed",
                DisplayName = "测试种子",
                UnitId = CoreProductionContent.GrainUnitId,
                QualityDimensionIds = new List<string>
                {
                    CoreProductionContent.PurityQualityDimensionId,
                    CoreProductionContent.ViabilityQualityDimensionId
                },
                CategoryTags = new List<string> { "product.seed" }
            });
            package.Products.Add(new ProductDefinition
            {
                Id = "product.mod_test.example_harvest",
                DisplayName = "测试收获物",
                UnitId = CoreProductionContent.GrainUnitId,
                QualityDimensionIds = new List<string>
                {
                    CoreProductionContent.PurityQualityDimensionId,
                    CoreProductionContent.IntegrityQualityDimensionId
                },
                CategoryTags = new List<string> { "product.test" }
            });
            package.Recipes.Add(new RecipeDefinition
            {
                Id = "recipe.mod_test.grow_example",
                DisplayName = "种植测试作物",
                CropDefinitionId = "crop.mod_test.example",
                DurationDays = 30,
                FacilityTags = new List<string> { "facility.farmland" },
                Inputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = "product.mod_test.example_seed",
                        QuantityPerLandUnit = 1
                    }
                },
                Outputs = new List<ProductionQuantityDefinition>
                {
                    new ProductionQuantityDefinition
                    {
                        ProductDefinitionId = "product.mod_test.example_harvest",
                        QuantityPerLandUnit = 2
                    }
                }
            });
            package.Methods.Add(new ProductionMethodDefinition
            {
                Id = "method.mod_test.example",
                DisplayName = "测试方法",
                RecipeDefinitionIds = new List<string>
                {
                    "recipe.mod_test.grow_example"
                },
                PracticeSkillDefinitionId = CoreSkillIds.Agriculture,
                PracticeDifficultyBasisPoints = 10_000,
                YieldBasisPoints = 10_000,
                LaborBasisPoints = 10_000,
                HistoricalStatus = "test_only"
            });
            return package;
        }

        private static int EquipmentStockQuantity(WorldState world)
        {
            var result = 0;
            for (var i = 0; i < world.MilitaryArmoryStocks.Count; i++)
            {
                result += world.MilitaryArmoryStocks[i].AvailableQuantity;
                result += world.MilitaryArmoryStocks[i].DamagedQuantity;
            }

            return result;
        }

        private static string TestProductionModJson()
        {
            return ProductionContentJson.SerializePackage(
                BuildTestProductionPackage("content.mod_test.production"));
        }

        private static void SimulateVillageMonths(WorldState world, int months)
        {
            var village = new VillageLifeSystem(world.MasterSeed);
            var life = new LifeSimulationSystem(world.MasterSeed);
            for (var month = 1; month <= months; month++)
            {
                world.AbsoluteDay = month * 30L;
                village.ResolveMonthly(world);
                life.ResolveMonthly(world);
                VillageLifeSystem.RefreshAllCaches(world);
                world.Validate();
            }
        }

        private static string NewPopulationStoreTestRoot()
        {
            return Path.Combine(
                Path.GetTempPath(),
                "mandate-population-store-tests",
                Guid.NewGuid().ToString("N"));
        }

        private static void DeletePopulationStoreTestRoot(string root)
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }

        private static int AverageToolCondition(WorldState world)
        {
            var total = 0;
            for (var i = 0; i < world.Families.Count; i++)
            {
                total += world.Families[i].ToolConditionBasisPoints;
            }

            return total / world.Families.Count;
        }

        private static void ConfigureCivilianFreightRouteChoices(
            WorldState world)
        {
            var direct = world.Routes.Find(item =>
                item.Id == "route.freight_origin_destination");
            direct.DistanceKilometers = 1;
            direct.SecurityBasisPoints = 10_000;
            world.Locations.Add(new LocationState
            {
                Id = "location.freight_short_midpoint",
                DisplayName = "Known Short Midpoint",
                Kind = LocationKind.Village,
                Features = LocationFeature.Market,
                MapXBasisPoints = 4_500,
                MapYBasisPoints = 5_300
            });
            world.Locations.Add(new LocationState
            {
                Id = "location.freight_safe_midpoint",
                DisplayName = "Known Safe Midpoint",
                Kind = LocationKind.Village,
                Features = LocationFeature.Market,
                MapXBasisPoints = 5_000,
                MapYBasisPoints = 5_800
            });
            world.Routes.Add(new RouteState
            {
                Id = "route.freight.short.1",
                FromLocationId = "location.freight_origin_village",
                ToLocationId = "location.freight_short_midpoint",
                DistanceKilometers = 10,
                SecurityBasisPoints = 7_000
            });
            world.Routes.Add(new RouteState
            {
                Id = "route.freight.short.2",
                FromLocationId = "location.freight_short_midpoint",
                ToLocationId = "location.freight_destination_village",
                DistanceKilometers = 10,
                SecurityBasisPoints = 7_000
            });
            world.Routes.Add(new RouteState
            {
                Id = "route.freight.safe.1",
                FromLocationId = "location.freight_origin_village",
                ToLocationId = "location.freight_safe_midpoint",
                DistanceKilometers = 12,
                SecurityBasisPoints = 9_000
            });
            world.Routes.Add(new RouteState
            {
                Id = "route.freight.safe.2",
                FromLocationId = "location.freight_safe_midpoint",
                ToLocationId = "location.freight_destination_village",
                DistanceKilometers = 12,
                SecurityBasisPoints = 9_000
            });
            world.Validate();
        }

        private static CivilianCarrierRegistrationRequest
            NewCivilianCarrierRegistration(
                CivilianFreightFixture fixture,
                string routePolicyId)
        {
            return new CivilianCarrierRegistrationRequest
            {
                CarrierPersonId = fixture.Carrier.Id,
                TransportInventoryContainerId = fixture.Transport.Id,
                BaseFee = 10,
                FeePerKilometer = 2,
                FeePerHundredUnits = 1,
                MaximumDistanceKilometers = 100,
                RoutePolicyId = routePolicyId,
                KnownRouteIds = new List<string>
                {
                    "route.freight.short.1",
                    "route.freight.short.2",
                    "route.freight.safe.1",
                    "route.freight.safe.2"
                }
            };
        }

        private static CivilianCarrierRegistrationRequest
            AddSecondCivilianFreightCarrier(
                CivilianFreightFixture fixture,
                long baseFee)
        {
            var person = NewFreightPerson(
                "person.freight_carrier_alt",
                "Alternate Freight Carrier",
                "family.freight_carrier_alt",
                "location.freight_origin_village");
            person.Provisions = 10;
            var family = NewFreightFamily(
                person,
                person.LocationId,
                "village.freight_origin",
                500);
            fixture.World.People.Add(person);
            fixture.World.Families.Add(family);
            var village = fixture.World.Villages.Find(item =>
                item.Id == "village.freight_origin");
            village.HouseholdIds.Add(family.Id);
            village.HouseholdCount++;
            village.LivingResidentCount++;
            village.WorkingResidentCount++;
            fixture.World.Locations.Find(item =>
                item.Id == person.LocationId).Population++;
            fixture.World.VillageFacilities.Add(NewFreightGranary(
                "facility.freight_carrier_alt_granary",
                village.Id,
                family,
                person,
                20_000));
            var container = NewFreightContainer(
                "inventory.freight_carrier_alt_cart",
                "inventory.civilian_cart",
                family.Id,
                string.Empty,
                person.Id,
                person.LocationId,
                20_000);
            fixture.World.InventoryContainers.Add(container);
            fixture.World.Validate();
            var request = NewCivilianCarrierRegistration(
                fixture,
                CivilianFreightRoutePolicyIds.ShortestKnown);
            request.CarrierPersonId = person.Id;
            request.TransportInventoryContainerId = container.Id;
            request.BaseFee = baseFee;
            return request;
        }

        private static FormalMarketCommandFixture
            PrepareFormalMarketCommandWorld(ulong seed)
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, seed);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var candidates = world.Families.FindAll(family =>
                world.ProductBatches.Exists(batch =>
                    batch.OwnerFamilyId == family.Id &&
                    batch.ProductDefinitionId ==
                        CoreProductionContent.WheatGrainProductId &&
                    batch.Quantity >= 6));
            Assert.That(candidates.Count, Is.GreaterThanOrEqualTo(2));
            var seller = candidates[0];
            var buyer = candidates[1];
            var sellerStorage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == seller.Id);
            var buyerStorage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == buyer.Id);
            buyerStorage.Capacity += 10_000;
            buyer.Wealth = 10_000;
            var market = new FormalCountyMarketSystem(content);
            var sell = market.CreateSellOrder(
                world,
                world.CountyGovernances[0].Id,
                seller.Id,
                sellerStorage.Id,
                CoreProductionContent.WheatGrainProductId,
                6,
                6,
                0,
                world.AbsoluteDay + 5);
            var buy = market.CreateBuyOrder(
                world,
                world.CountyGovernances[0].Id,
                buyer.Id,
                buyerStorage.Id,
                CoreProductionContent.WheatGrainProductId,
                6,
                9,
                0,
                world.AbsoluteDay + 5);
            world.Validate();
            return new FormalMarketCommandFixture
            {
                World = world,
                Content = content,
                Market = market,
                Scheduler = new FormalMarketDailyCommandScheduler(market),
                Seller = seller,
                Buyer = buyer,
                SellOrder = sell,
                BuyOrder = buy
            };
        }

        private static FormalHouseholdFoodCommandFixture
            PrepareFormalHouseholdFoodCommandWorld(ulong seed)
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, seed);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var food = new FoodInventorySystem(content);
            var village = world.Villages[0];
            for (var householdIndex = 0;
                 householdIndex < village.HouseholdIds.Count;
                 householdIndex++)
            {
                var family = world.Families.Find(item =>
                    item.Id == village.HouseholdIds[householdIndex]);
                var storage = world.VillageFacilities.Find(item =>
                    item.Kind == VillageFacilityKind.HouseholdGranary &&
                    item.OwnerFamilyId == family.Id);
                food.ConsumeFamilyFood(
                    world,
                    family.Id,
                    storage.Id,
                    family.HeadPersonId,
                    1_000_000_000_000L);
            }
            world.AbsoluteDay = 30;
            world.Validate();
            var villageLife = new VillageLifeSystem(
                world.MasterSeed,
                content);
            return new FormalHouseholdFoodCommandFixture
            {
                World = world,
                Content = content,
                VillageLife = villageLife,
                Scheduler =
                    new FormalHouseholdFoodMonthlyCommandScheduler(
                        villageLife)
            };
        }

        private static void SeedMonthlyShortfallAndVillageFood(
            FormalHouseholdFoodCommandFixture fixture,
            long physicalQuantity)
        {
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            TransferCountyFoodToVillage(
                fixture.World, fixture.Content, physicalQuantity);
        }

        private static int CompareHouseholdReliefPickupPriority(
            HouseholdReliefPickupState left,
            HouseholdReliefPickupState right)
        {
            var byDay = left.SettlementDay.CompareTo(right.SettlementDay);
            if (byDay != 0)
            {
                return byDay;
            }
            var byVillage = string.CompareOrdinal(
                left.VillageId, right.VillageId);
            if (byVillage != 0)
            {
                return byVillage;
            }
            if (left.PriorityPolicyId ==
                    HouseholdReliefPriorityPolicyIds.NeedSeverityVulnerability &&
                right.PriorityPolicyId ==
                    HouseholdReliefPriorityPolicyIds.NeedSeverityVulnerability)
            {
                var bySeverity = right.ShortfallSeverityBasisPoints.CompareTo(
                    left.ShortfallSeverityBasisPoints);
                if (bySeverity != 0)
                {
                    return bySeverity;
                }
                var byVulnerability =
                    right.VulnerableAffectedPersonCount.CompareTo(
                        left.VulnerableAffectedPersonCount);
                if (byVulnerability != 0)
                {
                    return byVulnerability;
                }
                var byAffected =
                    right.AffectedPersonCountAtAuthorization.CompareTo(
                        left.AffectedPersonCountAtAuthorization);
                if (byAffected != 0)
                {
                    return byAffected;
                }
            }
            return string.CompareOrdinal(left.FamilyId, right.FamilyId);
        }

        private static void TransferCountyFoodToVillage(
            WorldState world,
            ProductionContentRegistry content,
            long physicalQuantity)
        {
            var village = world.Villages[0];
            var governance = world.CountyGovernances.Find(item =>
                item.CountyLocationId == village.ParentLocationId);
            var government = world.Organizations.Find(item =>
                item.Id == governance.GovernmentOrganizationId);
            var destination = world.InventoryContainers.Find(item =>
                item.Id == village.PublicGranaryInventoryContainerId);
            destination.CapacityWeight = checked(
                destination.CapacityWeight + 100_000L);
            var food = new FoodInventorySystem(content);
            Assert.That(food.SummarizeContainer(
                    world, governance.GranaryInventoryContainerId)
                .PhysicalQuantity, Is.GreaterThanOrEqualTo(physicalQuantity));
            var transfer = food.TransferContainerToContainer(
                world,
                governance.GranaryInventoryContainerId,
                village.PublicGranaryInventoryContainerId,
                government.LeaderPersonId,
                physicalQuantity,
                InventoryTransactionType.FoodCountyReliefTransferred,
                village.Id,
                governance.Id);
            Assert.That(transfer.TransferredPhysicalQuantity,
                Is.EqualTo(physicalQuantity));
        }

        private static long TotalFamilyFood(
            WorldState world,
            string familyId)
        {
            long total = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (world.ProductBatches[i].OwnerFamilyId == familyId)
                {
                    total = checked(total + world.ProductBatches[i].Quantity);
                }
            }
            return total;
        }

        private static FormalPublicFoodCommandFixture
            PrepareFormalPublicFoodCommandWorld(ulong seed)
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, seed);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var village = world.Villages[0];
            var governance = world.CountyGovernances[0];
            world.InventoryContainers.Find(item => item.Id ==
                    village.PublicGranaryInventoryContainerId)
                .CapacityWeight += 100_000;
            world.InventoryContainers.Find(item => item.Id ==
                    governance.GranaryInventoryContainerId)
                .CapacityWeight += 100_000;
            for (var i = 0; i < village.HouseholdIds.Count; i++)
            {
                var family = world.Families.Find(item =>
                    item.Id == village.HouseholdIds[i]);
                family.LastHarvestGrain = i == 0 ? 1_000 : 100;
            }
            village.FoodSecurityBasisPoints = 0;
            village.LastSettlementDay = 300;
            village.NextSettlementDay = 330;
            world.AbsoluteDay = 300;
            world.Validate();
            content.ValidateWorldReferences(world);
            var villageLife = new VillageLifeSystem(
                world.MasterSeed,
                content);
            var county = new CountyGovernanceSystem(content);
            return new FormalPublicFoodCommandFixture
            {
                World = world,
                Content = content,
                VillageLife = villageLife,
                CountyGovernance = county,
                Scheduler = new FormalPublicFoodMonthlyCommandScheduler(
                    county,
                    villageLife)
            };
        }

        private static PublicReliefProcurementFixture
            PreparePublicReliefProcurementWorld(
                ulong seed,
                bool createSeller)
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, seed);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var village = world.Villages[0];
            var governance = world.CountyGovernances[0];
            var government = world.Organizations.Find(item =>
                item.Id == governance.GovernmentOrganizationId);
            government.Treasury = 100_000;
            world.InventoryContainers.Find(item => item.Id ==
                    village.PublicGranaryInventoryContainerId)
                .CapacityWeight += 1_000_000;
            world.InventoryContainers.Find(item => item.Id ==
                    governance.GranaryInventoryContainerId)
                .CapacityWeight += 1_000_000;
            world.AbsoluteDay = 30;
            world.Segment = (byte)DaySegment.Dawn;
            village.FoodSecurityBasisPoints = 0;
            village.LastSettlementDay = 30;
            village.NextSettlementDay = 60;

            var seller = world.Families.Find(item =>
                item.Id == village.HouseholdIds[0]);
            var storage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == seller.Id);
            if (createSeller)
            {
                var batch = world.ProductBatches.Find(item =>
                    item.OwnerFamilyId == seller.Id &&
                    item.StorageFacilityId == storage.Id &&
                    item.Quantity - item.ReservedQuantity >= 5 &&
                    content.TryGetFood(item.ProductDefinitionId, out _));
                Assert.That(batch, Is.Not.Null);
                new FormalCountyMarketSystem(content).CreateSellOrder(
                    world,
                    governance.Id,
                    seller.Id,
                    storage.Id,
                    batch.ProductDefinitionId,
                    5,
                    2,
                    0,
                    40);
            }

            var inventory = new FoodInventorySystem(content);
            var countyStock = inventory.SummarizeContainer(
                world, governance.GranaryInventoryContainerId)
                .PhysicalQuantity;
            if (countyStock > 0)
            {
                inventory.TransferContainerToContainer(
                    world,
                    governance.GranaryInventoryContainerId,
                    village.PublicGranaryInventoryContainerId,
                    government.LeaderPersonId,
                    countyStock,
                    InventoryTransactionType.FoodCountyReliefTransferred,
                    village.Id,
                    governance.Id);
            }

            var villageLife = new VillageLifeSystem(
                world.MasterSeed, content);
            var county = new CountyGovernanceSystem(content);
            var publicFood = new FormalPublicFoodMonthlyCommandScheduler(
                county, villageLife);
            var procurement = new PublicReliefProcurementCommandScheduler(
                new PublicReliefProcurementSystem(content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(publicFood.CreateCommandHandler());
            runtime.RegisterEventHandler(
                publicFood.CreateProjectionHandler());
            runtime.RegisterEventHandler(procurement.CreateTriggerHandler());
            Assert.That(publicFood.EnsureDueCommands(world, runtime),
                Is.EqualTo(1));
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            Assert.That(world.PersistentWorldCommands.Exists(item =>
                item.CommandTypeId ==
                    PublicReliefProcurementCommandScheduler.CommandTypeId &&
                item.Status == PersistentWorldCommandStatus.Pending), Is.True);
            world.Validate();
            return new PublicReliefProcurementFixture
            {
                World = world,
                Content = content,
                SellerFamilyId = seller.Id
            };
        }

        private static long TotalLivingHealth(WorldState world)
        {
            long result = 0;
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].IsAlive)
                {
                    result += world.People[i].HealthBasisPoints;
                }
            }
            return result;
        }

        private static long TotalProductQuantity(WorldState world)
        {
            long result = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                result = checked(result + world.ProductBatches[i].Quantity);
            }
            return result;
        }

        private static void AssertSingleFoodConsumptionLedgerPerFamily(
            WorldState world,
            string villageId,
            long day)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var count = 0;
            for (var i = 0; i < world.VillageLedgerEntries.Count; i++)
            {
                var entry = world.VillageLedgerEntries[i];
                if (entry.Day != day ||
                    entry.VillageId != villageId ||
                    entry.Type != VillageLedgerEntryType.FoodConsumption)
                {
                    continue;
                }
                Assert.That(seen.Add(entry.FamilyId), Is.True,
                    $"Family {entry.FamilyId} consumed twice on day {day}.");
                count++;
            }
            Assert.That(count, Is.GreaterThan(0));
        }

        private static CivilianFreightFixture PrepareCivilianFreightWorld(
            ulong seed,
            long quantity)
        {
            var content = LoadHanFoodProductionContent();
            var world = WorldState.Create(seed);
            world.ProductionContentManifest = content.CreateManifest();
            var originCounty = new LocationState
            {
                Id = "location.freight_origin_county",
                DisplayName = "Freight Origin County",
                Kind = LocationKind.CountySeat,
                Features = LocationFeature.Government |
                    LocationFeature.Market,
                MapXBasisPoints = 2_000,
                MapYBasisPoints = 5_000,
                Population = 2
            };
            var destinationCounty = new LocationState
            {
                Id = "location.freight_destination_county",
                DisplayName = "Freight Destination County",
                Kind = LocationKind.CountySeat,
                Features = LocationFeature.Government |
                    LocationFeature.Market,
                MapXBasisPoints = 8_000,
                MapYBasisPoints = 5_000,
                Population = 1
            };
            var originVillageLocation = new LocationState
            {
                Id = "location.freight_origin_village",
                DisplayName = "Freight Origin Village",
                Kind = LocationKind.Village,
                Features = LocationFeature.Farmland,
                ParentLocationId = originCounty.Id,
                MapXBasisPoints = 2_500,
                MapYBasisPoints = 5_500,
                Population = 2
            };
            var destinationVillageLocation = new LocationState
            {
                Id = "location.freight_destination_village",
                DisplayName = "Freight Destination Village",
                Kind = LocationKind.Village,
                Features = LocationFeature.Farmland,
                ParentLocationId = destinationCounty.Id,
                MapXBasisPoints = 7_500,
                MapYBasisPoints = 5_500,
                Population = 1
            };
            world.Locations.Add(originCounty);
            world.Locations.Add(destinationCounty);
            world.Locations.Add(originVillageLocation);
            world.Locations.Add(destinationVillageLocation);

            var sellerPerson = NewFreightPerson(
                "person.freight_seller",
                "Freight Seller",
                "family.freight_seller",
                originVillageLocation.Id);
            var buyerPerson = NewFreightPerson(
                "person.freight_buyer",
                "Freight Buyer",
                "family.freight_buyer",
                destinationVillageLocation.Id);
            var carrierPerson = NewFreightPerson(
                "person.freight_carrier",
                "Freight Carrier",
                "family.freight_carrier",
                originVillageLocation.Id);
            carrierPerson.Provisions = 10;
            world.People.Add(sellerPerson);
            world.People.Add(buyerPerson);
            world.People.Add(carrierPerson);

            var seller = NewFreightFamily(
                sellerPerson,
                originVillageLocation.Id,
                "village.freight_origin",
                1_000);
            var buyer = NewFreightFamily(
                buyerPerson,
                destinationVillageLocation.Id,
                "village.freight_destination",
                10_000);
            var carrierFamily = NewFreightFamily(
                carrierPerson,
                originVillageLocation.Id,
                "village.freight_origin",
                500);
            world.Families.Add(seller);
            world.Families.Add(buyer);
            world.Families.Add(carrierFamily);

            var originGovernment = new OrganizationState
            {
                Id = "organization.freight_origin_government",
                DisplayName = "Origin Government",
                Type = OrganizationType.Government,
                HeadquartersLocationId = originCounty.Id,
                LeaderPersonId = sellerPerson.Id
            };
            var destinationGovernment = new OrganizationState
            {
                Id = "organization.freight_destination_government",
                DisplayName = "Destination Government",
                Type = OrganizationType.Government,
                HeadquartersLocationId = destinationCounty.Id,
                LeaderPersonId = buyerPerson.Id
            };
            world.Organizations.Add(originGovernment);
            world.Organizations.Add(destinationGovernment);

            var originVillage = new VillageState
            {
                Id = "village.freight_origin",
                DisplayName = "Freight Origin Village",
                LocationId = originVillageLocation.Id,
                ParentLocationId = originCounty.Id,
                HouseholdIds = { seller.Id, carrierFamily.Id },
                HouseholdCount = 2,
                LivingResidentCount = 2,
                WorkingResidentCount = 2,
                PublicGranaryInventoryContainerId =
                    "inventory.freight_origin_public_granary"
            };
            var destinationVillage = new VillageState
            {
                Id = "village.freight_destination",
                DisplayName = "Freight Destination Village",
                LocationId = destinationVillageLocation.Id,
                ParentLocationId = destinationCounty.Id,
                HouseholdIds = { buyer.Id },
                HouseholdCount = 1,
                LivingResidentCount = 1,
                WorkingResidentCount = 1,
                PublicGranaryInventoryContainerId =
                    "inventory.freight_destination_public_granary"
            };
            world.Villages.Add(originVillage);
            world.Villages.Add(destinationVillage);

            var sellerStorage = NewFreightGranary(
                "facility.freight_seller_granary",
                originVillage.Id,
                seller,
                sellerPerson,
                20_000);
            var buyerStorage = NewFreightGranary(
                "facility.freight_buyer_granary",
                destinationVillage.Id,
                buyer,
                buyerPerson,
                20_000);
            var carrierStorage = NewFreightGranary(
                "facility.freight_carrier_granary",
                originVillage.Id,
                carrierFamily,
                carrierPerson,
                20_000);
            world.VillageFacilities.Add(sellerStorage);
            world.VillageFacilities.Add(buyerStorage);
            world.VillageFacilities.Add(carrierStorage);

            var originGovernance = new CountyGovernanceState
            {
                Id = "county_governance.freight_origin",
                CountyLocationId = originCounty.Id,
                GovernmentOrganizationId = originGovernment.Id,
                AdministratorFamilyId = seller.Id,
                GranaryInventoryContainerId =
                    "inventory.freight_origin_county_granary"
            };
            var destinationGovernance = new CountyGovernanceState
            {
                Id = "county_governance.freight_destination",
                CountyLocationId = destinationCounty.Id,
                GovernmentOrganizationId = destinationGovernment.Id,
                AdministratorFamilyId = buyer.Id,
                GranaryInventoryContainerId =
                    "inventory.freight_destination_county_granary"
            };
            world.CountyGovernances.Add(originGovernance);
            world.CountyGovernances.Add(destinationGovernance);

            world.InventoryContainers.Add(NewFreightContainer(
                originVillage.PublicGranaryInventoryContainerId,
                "inventory.village_public_granary",
                string.Empty,
                originGovernment.Id,
                string.Empty,
                originVillageLocation.Id,
                20_000));
            world.InventoryContainers.Add(NewFreightContainer(
                destinationVillage.PublicGranaryInventoryContainerId,
                "inventory.village_public_granary",
                string.Empty,
                destinationGovernment.Id,
                string.Empty,
                destinationVillageLocation.Id,
                20_000));
            world.InventoryContainers.Add(NewFreightContainer(
                originGovernance.GranaryInventoryContainerId,
                "inventory.county_granary",
                string.Empty,
                originGovernment.Id,
                string.Empty,
                originCounty.Id,
                20_000));
            world.InventoryContainers.Add(NewFreightContainer(
                destinationGovernance.GranaryInventoryContainerId,
                "inventory.county_granary",
                string.Empty,
                destinationGovernment.Id,
                string.Empty,
                destinationCounty.Id,
                20_000));
            var transport = NewFreightContainer(
                "inventory.freight_carrier_cart",
                "inventory.civilian_cart",
                carrierFamily.Id,
                string.Empty,
                carrierPerson.Id,
                originVillageLocation.Id,
                20_000);
            world.InventoryContainers.Add(transport);
            world.Routes.Add(new RouteState
            {
                Id = "route.freight_origin_destination",
                FromLocationId = originVillageLocation.Id,
                ToLocationId = destinationVillageLocation.Id,
                DistanceKilometers = 30,
                Bidirectional = true,
                SecurityBasisPoints = 8_000
            });

            new ProductInventorySystem(content).CreateFamilyOpeningBatch(
                world,
                seller.Id,
                sellerStorage.Id,
                sellerPerson.Id,
                CoreProductionContent.WheatGrainProductId,
                quantity);
            world.FoodInventoryAuthorityMode =
                FoodInventoryAuthorityMode.FormalProductBatches;
            world.Validate();
            content.ValidateWorldReferences(world);

            var market = new FormalCountyMarketSystem(content);
            var sell = market.CreateSellOrder(
                world,
                originGovernance.Id,
                seller.Id,
                sellerStorage.Id,
                CoreProductionContent.WheatGrainProductId,
                quantity,
                2,
                0,
                10);
            var buy = market.CreateBuyOrder(
                world,
                destinationGovernance.Id,
                buyer.Id,
                buyerStorage.Id,
                CoreProductionContent.WheatGrainProductId,
                quantity,
                3,
                0,
                10);
            world.Validate();

            return new CivilianFreightFixture
            {
                World = world,
                Content = content,
                Seller = seller,
                Buyer = buyer,
                CarrierFamily = carrierFamily,
                Carrier = carrierPerson,
                SellerStorage = sellerStorage,
                BuyerStorage = buyerStorage,
                Transport = transport,
                FreightSystem = new CivilianFreightSystem(seed, content),
                Request = new CivilianFreightDispatchRequest
                {
                    BuyOrderId = buy.Id,
                    SellOrderId = sell.Id,
                    CarrierPersonId = carrierPerson.Id,
                    TransportInventoryContainerId = transport.Id,
                    RouteId = "route.freight_origin_destination",
                    Quantity = quantity,
                    FreightFee = 100
                }
            };
        }

        private static void SeedFormalReliefShortfall(
            WorldState world,
            string governanceId,
            long amount)
        {
            var commandId = "test.public_relief_shortfall.command";
            var resultId = "test.public_relief_shortfall.result";
            var eventId = "test.public_relief_shortfall.event";
            var transactionId =
                FormalPublicFoodMonthlyCommandScheduler.MonthlyTransactionId(
                    0, governanceId);
            var governance = world.CountyGovernances.Find(item =>
                item.Id == governanceId);
            var village = world.Villages.Find(item =>
                item.ParentLocationId == governance.CountyLocationId);
            world.CountyFiscalLedgerEntries.Add(
                new CountyFiscalLedgerEntryState
                {
                    Id = "county_fiscal.test_relief_shortfall",
                    Day = 0,
                    Type = CountyFiscalEntryType.GrainReliefShortfall,
                    CountyGovernanceId = governanceId,
                    FamilyId = string.Empty,
                    VillageId = village.Id,
                    Amount = amount,
                    Summary = "Test committed county relief shortfall."
                });
            world.PersistentWorldCommands.Add(
                new PersistentWorldCommandState
                {
                    Id = commandId,
                    CommandTypeId =
                        FormalPublicFoodMonthlyCommandScheduler.CommandTypeId,
                    IssuerId = "system.test_public_relief_shortfall",
                    CreatedDay = 0,
                    DueDay = 0,
                    Status = PersistentWorldCommandStatus.Completed,
                    AttemptCount = 1,
                    LastAttemptResultId = resultId,
                    CompletedDay = 0,
                    CompletionResultId = resultId
                });
            world.WorldEventOutbox.Add(
                new WorldEventOutboxState
                {
                    Id = eventId,
                    EventTypeId =
                        FormalPublicFoodMonthlyCommandScheduler
                            .ReliefShortfallEventTypeId,
                    SourceTransactionId = transactionId,
                    Day = 0,
                    Segment = (byte)DaySegment.Dawn,
                    DispatchStatus = WorldEventDispatchStatus.Pending
                });
            world.WorldCommandBatchResults.Add(
                new WorldCommandBatchResultState
                {
                    Id = resultId,
                    Outcome = WorldCommandBatchOutcome.Succeeded,
                    Day = 0,
                    Segment = (byte)DaySegment.Dawn,
                    CommandIds = new List<string> { commandId },
                    Transactions = new List<WorldTransactionExecutionState>
                    {
                        new WorldTransactionExecutionState
                        {
                            TransactionId = transactionId,
                            TransactionKindId =
                                FormalPublicFoodMonthlyCommandScheduler
                                    .TransactionKindId,
                            Priority = 4
                        }
                    },
                    PublishedEventIds = new List<string> { eventId }
                });
            world.Validate();
        }

        private static CivilianFreightFixture
            PrepareExternalReliefProcurementWorld(
                ulong seed,
                bool knowDestinationCountyRoute)
        {
            var fixture = PrepareCivilianFreightWorld(seed, 20);
            var destination = fixture.World.CountyGovernances.Find(item =>
                item.Id == "county_governance.freight_destination");
            fixture.World.Organizations.Find(item =>
                item.Id == destination.GovernmentOrganizationId).Treasury =
                100_000;
            fixture.World.Routes.Add(new RouteState
            {
                Id = "route.freight_destination_village_county",
                FromLocationId = "location.freight_destination_village",
                ToLocationId = destination.CountyLocationId,
                DistanceKilometers = 10,
                Bidirectional = true,
                SecurityBasisPoints = 9_000
            });
            var knownRoutes = new List<string>
            {
                "route.freight_origin_destination"
            };
            if (knowDestinationCountyRoute)
            {
                knownRoutes.Add(
                    "route.freight_destination_village_county");
            }
            fixture.FreightSystem.RegisterCarrier(
                fixture.World,
                new CivilianCarrierRegistrationRequest
                {
                    CarrierPersonId = fixture.Carrier.Id,
                    TransportInventoryContainerId = fixture.Transport.Id,
                    BaseFee = 10,
                    FeePerKilometer = 1,
                    FeePerHundredUnits = 1,
                    MaximumDistanceKilometers = 100,
                    KnownRouteIds = knownRoutes
                });
            SeedFormalReliefShortfall(
                fixture.World, destination.Id, 10);
            return fixture;
        }

        private static void RunExternalReliefProcurementToDispatch(
            CivilianFreightFixture fixture)
        {
            var runtime = CreatePublicReliefProcurementRuntime(fixture);
            runtime.DispatchPublishedEvents(fixture.World);
            fixture.World.AbsoluteDay = 1;
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            fixture.World.AbsoluteDay = 2;
            runtime.ProcessDue(fixture.World);
            fixture.World.Validate();
        }

        private static void CompleteCivilianFreight(
            CivilianFreightFixture fixture,
            CivilianFreightState freight)
        {
            var travel = new TravelSystem();
            for (var i = 0;
                 i < 64 &&
                 freight.Status != CivilianFreightStatus.Completed;
                 i++)
            {
                travel.AdvanceJourneysOneSegment(fixture.World);
                fixture.FreightSystem.ResolveArrivals(fixture.World);
            }
            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
        }

        private static void ResolvePublicReliefArrivalRecovery(
            CivilianFreightFixture fixture)
        {
            var scheduler =
                new PublicReliefArrivalRecoveryCommandScheduler(
                    new PublicReliefArrivalRecoverySystem(
                        fixture.World.MasterSeed, fixture.Content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            runtime.RegisterEventHandler(
                scheduler.CreateProjectionHandler());
            scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
        }

        private static void AddBackupReliefCarrier(
            CivilianFreightFixture fixture)
        {
            var person = NewFreightPerson(
                "person.freight_carrier_backup",
                "Backup Freight Carrier",
                fixture.CarrierFamily.Id,
                "location.freight_origin_village");
            person.Provisions = 10;
            fixture.World.People.Add(person);
            fixture.CarrierFamily.MemberIds.Add(person.Id);
            var transport = NewFreightContainer(
                "inventory.freight_carrier_backup_cart",
                "inventory.civilian_cart",
                fixture.CarrierFamily.Id,
                string.Empty,
                person.Id,
                person.LocationId,
                20_000);
            fixture.World.InventoryContainers.Add(transport);
            fixture.FreightSystem.RegisterCarrier(
                fixture.World,
                new CivilianCarrierRegistrationRequest
                {
                    CarrierPersonId = person.Id,
                    TransportInventoryContainerId = transport.Id,
                    BaseFee = 100,
                    FeePerKilometer = 1,
                    FeePerHundredUnits = 1,
                    MaximumDistanceKilometers = 100,
                    KnownRouteIds = new List<string>
                    {
                        "route.freight_origin_destination",
                        "route.freight_destination_village_county"
                    }
                });
        }

        private static WorldCommandRuntime CreatePublicReliefProcurementRuntime(
            CivilianFreightFixture fixture)
        {
            var local = new PublicReliefProcurementCommandScheduler(
                new PublicReliefProcurementSystem(fixture.Content));
            var external =
                new PublicReliefExternalProcurementCommandScheduler(
                    new PublicReliefExternalProcurementSystem(
                        fixture.World.MasterSeed, fixture.Content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(local.CreateCommandHandler());
            runtime.RegisterHandler(external.CreateCommandHandler());
            runtime.RegisterEventHandler(local.CreateTriggerHandler());
            runtime.RegisterEventHandler(local.CreateProjectionHandler());
            runtime.RegisterEventHandler(external.CreateTriggerHandler());
            runtime.RegisterEventHandler(external.CreateProjectionHandler());
            return runtime;
        }

        private static PersonState NewFreightPerson(
            string id,
            string displayName,
            string familyId,
            string locationId)
        {
            return new PersonState
            {
                Id = id,
                DisplayName = displayName,
                FamilyId = familyId,
                LocationId = locationId,
                BirthLocationId = locationId,
                BirthDay = -8_000,
                VillageOccupation = VillageOccupation.Merchant
            };
        }

        private static FoodStorageFixture PrepareFoodStorageLossWorld(
            ulong seed)
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, seed);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            return new FoodStorageFixture
            {
                World = world,
                Content = content
            };
        }

        private static int ResolveFoodStorageLoss(
            WorldState world,
            ProductionContentRegistry content)
        {
            var scheduler = new FoodStorageLossCommandScheduler(
                new FoodStorageLossSystem(content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            runtime.RegisterEventHandler(
                scheduler.CreateProjectionHandler());
            var scheduled = scheduler.EnsureDueCommands(world, runtime);
            if (scheduled > 0)
            {
                runtime.ProcessDue(world);
            }
            return scheduled;
        }

        private static FamilyState NewFreightFamily(
            PersonState head,
            string locationId,
            string villageId,
            long wealth)
        {
            return new FamilyState
            {
                Id = head.FamilyId,
                DisplayName = head.DisplayName + " Household",
                HeadPersonId = head.Id,
                LocationId = locationId,
                VillageId = villageId,
                Wealth = wealth,
                MemberIds = { head.Id }
            };
        }

        private static VillageFacilityState NewFreightGranary(
            string id,
            string villageId,
            FamilyState owner,
            PersonState manager,
            int capacity)
        {
            return new VillageFacilityState
            {
                Id = id,
                VillageId = villageId,
                Kind = VillageFacilityKind.HouseholdGranary,
                OwnerFamilyId = owner.Id,
                ManagerPersonId = manager.Id,
                Capacity = capacity
            };
        }

        private static InventoryContainerState NewFreightContainer(
            string id,
            string kindId,
            string familyId,
            string organizationId,
            string carrierId,
            string locationId,
            long capacity)
        {
            return new InventoryContainerState
            {
                Id = id,
                KindId = kindId,
                OwnerFamilyId = familyId,
                OwnerOrganizationId = organizationId,
                CarrierPersonId = carrierId,
                LocationId = locationId,
                CapacityWeight = capacity
            };
        }

        private sealed class CivilianFreightFixture
        {
            public WorldState World;
            public ProductionContentRegistry Content;
            public FamilyState Seller;
            public FamilyState Buyer;
            public FamilyState CarrierFamily;
            public PersonState Carrier;
            public VillageFacilityState SellerStorage;
            public VillageFacilityState BuyerStorage;
            public InventoryContainerState Transport;
            public CivilianFreightSystem FreightSystem;
            public CivilianFreightDispatchRequest Request;
        }

        private sealed class FormalMarketCommandFixture
        {
            public WorldState World;
            public ProductionContentRegistry Content;
            public FormalCountyMarketSystem Market;
            public FormalMarketDailyCommandScheduler Scheduler;
            public FamilyState Seller;
            public FamilyState Buyer;
            public FormalMarketOrderState SellOrder;
            public FormalMarketOrderState BuyOrder;
        }

        private sealed class FormalHouseholdFoodCommandFixture
        {
            public WorldState World;
            public ProductionContentRegistry Content;
            public VillageLifeSystem VillageLife;
            public FormalHouseholdFoodMonthlyCommandScheduler Scheduler;
        }

        private sealed class FormalPublicFoodCommandFixture
        {
            public WorldState World;
            public ProductionContentRegistry Content;
            public VillageLifeSystem VillageLife;
            public CountyGovernanceSystem CountyGovernance;
            public FormalPublicFoodMonthlyCommandScheduler Scheduler;
        }

        private sealed class PublicReliefProcurementFixture
        {
            public WorldState World;
            public ProductionContentRegistry Content;
            public string SellerFamilyId;
        }

        private sealed class FormalMarketEventRecorder :
            IWorldRuntimeEventHandler
        {
            public List<string> HandledEventIds { get; } = new List<string>();

            public string HandlerId => "test.handler.formal_market_recorder";

            public string EventTypeId =>
                FormalMarketDailyCommandScheduler.EventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                HandledEventIds.Add(worldEvent.Id);
            }
        }

        private sealed class CivilianFreightPlanningEventRecorder :
            IWorldRuntimeEventHandler
        {
            public List<string> HandledEventIds { get; } = new List<string>();

            public string HandlerId =>
                "test.handler.civilian_freight_planning_recorder";

            public string EventTypeId =>
                CivilianFreightPlanningCommandScheduler.EventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                HandledEventIds.Add(worldEvent.Id);
            }
        }

        private sealed class FormalHouseholdFoodShortfallEventRecorder :
            IWorldRuntimeEventHandler
        {
            public List<string> HandledEventIds { get; } = new List<string>();

            public string HandlerId =>
                "test.handler.formal_household_food_shortfall_recorder";

            public string EventTypeId =>
                FormalHouseholdFoodMonthlyCommandScheduler
                    .ShortfallEventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                HandledEventIds.Add(worldEvent.Id);
            }
        }

        private sealed class FormalPublicFoodEventRecorder :
            IWorldRuntimeEventHandler
        {
            public List<string> HandledEventIds { get; } = new List<string>();

            public string HandlerId =>
                "test.handler.formal_public_food_recorder";

            public string EventTypeId =>
                FormalPublicFoodMonthlyCommandScheduler.EventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                HandledEventIds.Add(worldEvent.Id);
            }
        }

        private static WorldState BuildGuangzongBattleWorld()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 30);
            new ArmySystem().StartMarch(
                world,
                new StableId("person.guo_dian"),
                new StableId("army.han_jizhou_vanguard"),
                new StableId("route.xiaquyang_guangzong"),
                new StableId("location.guangzong"));
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 8);
            return world;
        }

        private static WorldState BuildPatientReturnDeathWorld(
            bool advancePastWaitingPeriod,
            out MilitaryMedicalEvacuationState evacuation,
            out MilitaryRearMedicalAdmissionState admission,
            out MilitaryInjuryEpisodeState injury,
            out ArmyState army,
            out PersonState patient,
            out MilitaryServiceState patientService,
            out List<MilitaryServiceState> teamServices)
        {
            var world = BuildRearMedicalWorld(
                1,
                5,
                out var localArmy,
                out var localPatientService,
                out var localTeamServices,
                out var receiver,
                out var site);
            var localEvacuation = DispatchAndReceiveEvacuation(
                world,
                localArmy,
                localPatientService,
                localTeamServices,
                receiver);
            world.MilitarySurgeryContractActivationDay = checked(
                world.AbsoluteDay + 1);
            var rear = new MilitaryRearMedicalSystem();
            var localAdmission = rear.Admit(
                world,
                new StableId(localEvacuation.Id),
                new StableId(site.Id),
                new StableId(receiver.Id));
            rear.TreatInpatient(
                world, new StableId(localAdmission.Id));
            var localInjury = world.MilitaryInjuryEpisodes.Find(item =>
                item.Id == localAdmission.InjuryEpisodeId);
            var localPatient = world.People.Find(item =>
                item.Id == localAdmission.PatientPersonId);
            var successor = world.People.Find(item =>
                item.Id == localEvacuation.TeamMembers[0].PersonId);
            localPatient.FamilyId = "family.test.patient_return_death";
            successor.FamilyId = localPatient.FamilyId;
            localPatient.Wealth = 75;
            world.Families.Add(new FamilyState
            {
                Id = localPatient.FamilyId,
                DisplayName = "返军伤兵之家",
                HeadPersonId = localPatient.Id,
                Wealth = 1_000,
                LocationId = site.LocationId,
                MemberIds = new List<string>
                {
                    localPatient.Id,
                    successor.Id
                }
            });
            var simulator = new WorldSimulator(world.MasterSeed);
            while (world.Segment != 3)
            {
                simulator.AdvanceSegments(world, 1);
            }
            rear.StartReturn(
                world,
                new StableId(localEvacuation.Id),
                new StableId("route.zhongshan_anping"));
            if (advancePastWaitingPeriod)
            {
                simulator.AdvanceSegments(world, 1);
            }
            world.Validate();

            evacuation = localEvacuation;
            admission = localAdmission;
            injury = localInjury;
            army = localArmy;
            patient = localPatient;
            patientService = localPatientService;
            teamServices = localTeamServices;
            return world;
        }

        private static WorldState BuildPatientArrivalWaitingTeamDeathWorld(
            bool advancePastWaitingPeriod,
            out MilitaryMedicalEvacuationState evacuation,
            out MilitaryRearMedicalAdmissionState admission,
            out MilitaryInjuryEpisodeState injury,
            out ArmyState army,
            out PersonState patient,
            out MilitaryServiceState patientService,
            out List<MilitaryServiceState> teamServices)
        {
            var world = BuildPatientReturnDeathWorld(
                false,
                out var localEvacuation,
                out var localAdmission,
                out var localInjury,
                out var localArmy,
                out var localPatient,
                out var localPatientService,
                out var localTeamServices);
            var patientJourney = world.Journeys.Find(item =>
                item.Id == localEvacuation.PatientReturnJourneyId);
            patientJourney.RemainingKilometers =
                TravelSystem.KilometersPerSegment(TravelMode.Foot);
            for (var i = 0; i < localEvacuation.TeamMembers.Count; i++)
            {
                var teamJourney = world.Journeys.Find(item =>
                    item.Id == localEvacuation.TeamMembers[i].ReturnJourneyId);
                teamJourney.RemainingKilometers = checked(
                    TravelSystem.KilometersPerSegment(TravelMode.Foot) * 2);
            }

            if (advancePastWaitingPeriod)
            {
                new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 1);
            }
            else
            {
                new TravelSystem().AdvanceJourneysOneSegment(world);
            }
            world.Validate();

            evacuation = localEvacuation;
            admission = localAdmission;
            injury = localInjury;
            army = localArmy;
            patient = localPatient;
            patientService = localPatientService;
            teamServices = localTeamServices;
            return world;
        }

        private static WorldState BuildReturnTeamDeathWorld(
            bool advancePastWaitingPeriod,
            out MilitaryMedicalEvacuationState evacuation,
            out MilitaryRearMedicalAdmissionState admission,
            out ArmyState army,
            out PersonState patient,
            out PersonState deceasedMember,
            out MilitaryServiceState deceasedService,
            out List<MilitaryServiceState> teamServices)
        {
            var world = BuildPatientReturnDeathWorld(
                false,
                out var localEvacuation,
                out var localAdmission,
                out var injury,
                out var localArmy,
                out var localPatient,
                out var patientService,
                out var localTeamServices);
            AddReturnTeamMembersToPatientFamily(
                world, localEvacuation, localPatient.FamilyId);
            if (advancePastWaitingPeriod)
            {
                new WorldSimulator(world.MasterSeed)
                    .AdvanceSegments(world, 1);
            }
            world.Validate();

            evacuation = localEvacuation;
            admission = localAdmission;
            army = localArmy;
            patient = localPatient;
            var selectedService = localTeamServices[0];
            deceasedMember = world.People.Find(item =>
                item.Id == selectedService.PersonId);
            deceasedService = selectedService;
            teamServices = localTeamServices;
            return world;
        }

        private static void AddReturnTeamMembersToPatientFamily(
            WorldState world,
            MilitaryMedicalEvacuationState evacuation,
            string familyId)
        {
            var family = world.Families.Find(item => item.Id == familyId);
            for (var i = 0; i < evacuation.TeamMembers.Count; i++)
            {
                var member = world.People.Find(item =>
                    item.Id == evacuation.TeamMembers[i].PersonId);
                member.FamilyId = family.Id;
                if (!family.MemberIds.Contains(member.Id))
                {
                    family.MemberIds.Add(member.Id);
                }
            }
        }

        private static WorldState BuildOriginalEvacuationDeathWorld(
            bool advanceToReception,
            bool advancePastWaitingPeriod,
            out MilitaryMedicalEvacuationState evacuation,
            out ArmyState army,
            out PersonState patient,
            out MilitaryServiceState patientService,
            out List<MilitaryServiceState> teamServices,
            out PersonState receiver)
        {
            var world = BuildMilitaryEvacuationWorld(
                out var localArmy,
                out var localPatientService,
                out var localTeamServices,
                out var localReceiver);
            var localPatient = world.People.Find(item =>
                item.Id == localPatientService.PersonId);
            var successor = world.People.Find(item =>
                item.Id == localTeamServices[0].PersonId);
            localPatient.HealthBasisPoints = 1_000;
            localPatient.Wealth = 75;
            localPatient.FamilyId =
                "family.test.original_evacuation_death";
            successor.FamilyId = localPatient.FamilyId;
            world.Families.Add(new FamilyState
            {
                Id = localPatient.FamilyId,
                DisplayName = "后送伤兵之家",
                HeadPersonId = localPatient.Id,
                Wealth = 1_000,
                LocationId = localArmy.LocationId,
                MemberIds = new List<string>
                {
                    localPatient.Id,
                    successor.Id
                }
            });
            var localEvacuation = new MilitaryMedicalEvacuationSystem()
                .Dispatch(
                    world,
                    new StableId(localArmy.CommanderPersonId),
                    new StableId(localPatientService.Id),
                    localTeamServices.ConvertAll(item =>
                        new StableId(item.Id)),
                    new StableId("route.zhongshan_anping"),
                    new StableId("location.anping"),
                    new StableId(localReceiver.Id));
            var simulator = new WorldSimulator(world.MasterSeed);
            if (advancePastWaitingPeriod)
            {
                simulator.AdvanceDays(world, 1);
            }
            if (advanceToReception)
            {
                for (var i = 0;
                     i < 40 && localEvacuation.Status ==
                         MilitaryMedicalEvacuationStatus.InTransit;
                     i++)
                {
                    simulator.AdvanceSegments(world, 1);
                }
            }
            world.Validate();
            evacuation = localEvacuation;
            army = localArmy;
            patient = localPatient;
            patientService = localPatientService;
            teamServices = localTeamServices;
            receiver = localReceiver;
            return world;
        }

        private static WorldState BuildMilitaryEvacuationWorld(
            out ArmyState army,
            out MilitaryServiceState patientService,
            out List<MilitaryServiceState> teamServices,
            out PersonState receiver)
        {
            var world = PrototypeWorldFactory.Create184World(184_049);
            var sourceArmy = world.Armies.Find(item =>
                item.Id == "army.youzhou_reinforcement");
            var eligible = world.MilitaryServices.FindAll(item =>
                item.ArmyId == sourceArmy.Id &&
                item.Role == MilitaryServiceRole.Soldier &&
                item.Status == MilitaryServiceStatus.Active);
            eligible.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var woundedService = eligible[0];
            woundedService.Status = MilitaryServiceStatus.Wounded;
            woundedService.LastStatusChangeDay = world.AbsoluteDay;
            world.People.Find(item =>
                item.Id == woundedService.PersonId).HealthBasisPoints = 4_000;
            var evacuationTeam = new List<MilitaryServiceState>
            {
                eligible[1],
                eligible[2]
            };
            new MilitaryServiceSystem().SynchronizeArmyCaches(
                world, sourceArmy.Id);

            var receivingPhysician = world.People.Find(item =>
                item.Id == "person.generated.physician_001");
            new PopulationLedgerSystem().MoveIndependentPerson(
                world, receivingPhysician, "location.anping");
            world.Validate();
            army = sourceArmy;
            patientService = woundedService;
            teamServices = evacuationTeam;
            receiver = receivingPhysician;
            return world;
        }

        private static WorldState BuildRearMedicalWorld(
            int bedCapacity,
            int openingMedicineUnits,
            out ArmyState army,
            out MilitaryServiceState patientService,
            out List<MilitaryServiceState> teamServices,
            out PersonState receiver,
            out MilitaryRearMedicalSiteState site)
        {
            var world = BuildMilitaryEvacuationWorld(
                out army,
                out patientService,
                out teamServices,
                out receiver);
            world.Locations.Find(item => item.Id == "location.anping")
                .Features |= LocationFeature.Clinic;
            site = new MilitaryRearMedicalSystem().RegisterExistingClinic(
                world,
                new StableId("location.anping"),
                new StableId("organization.guangzong_relief_camp"),
                new StableId(receiver.Id),
                bedCapacity,
                openingMedicineUnits);
            return world;
        }

        private static WorldState BuildFieldHospitalWorld(
            out ArmyState army,
            out MilitaryFieldHospitalConstructionProjectState project,
            out MilitaryRearMedicalSiteState site,
            out InventoryContainerState materialContainer)
        {
            var world = PrototypeWorldFactory.Create184World(184_050);
            var localArmy = world.Armies.Find(item =>
                item.Id == "army.youzhou_reinforcement");
            var organization = world.Organizations.Find(item =>
                item.Id == localArmy.OrganizationId);
            organization.Treasury = 5_000;
            materialContainer = new InventoryContainerState
            {
                Id = "inventory_container.field_hospital.materials",
                KindId = "inventory_container.military_construction_store",
                OwnerOrganizationId = organization.Id,
                LocationId = localArmy.LocationId,
                CapacityWeight = 1_000
            };
            world.InventoryContainers.Add(materialContainer);
            AddOrganizationProductBatch(
                world,
                "product_batch.field_hospital.timber",
                organization.Id,
                materialContainer.Id,
                localArmy.LocationId,
                CoreProductionContent.TimberMaterialProductId,
                30,
                localArmy.CommanderPersonId);
            AddOrganizationProductBatch(
                world,
                "product_batch.field_hospital.leather",
                organization.Id,
                materialContainer.Id,
                localArmy.LocationId,
                CoreProductionContent.LeatherMaterialProductId,
                10,
                localArmy.CommanderPersonId);
            world.Validate();
            var construction = new MilitaryFieldHospitalSystem();
            var localProject = construction.StartProject(
                world,
                new StableId(localArmy.CommanderPersonId),
                new StableId(localArmy.Id),
                new StableId(localArmy.LocationId),
                new StableId(localArmy.CommanderPersonId),
                new StableId(materialContainer.Id));
            construction.WorkOneDay(
                world,
                new StableId(localProject.Id),
                new StableId(localArmy.CommanderPersonId));
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            construction.WorkOneDay(
                world,
                new StableId(localProject.Id),
                new StableId(localArmy.CommanderPersonId));
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            construction.WorkOneDay(
                world,
                new StableId(localProject.Id),
                new StableId(localArmy.CommanderPersonId));
            site = world.MilitaryRearMedicalSites.Find(item =>
                item.SourceConstructionProjectId == localProject.Id);
            army = localArmy;
            project = localProject;
            world.Validate();
            return world;
        }

        private static WorldState BuildInfectedFieldHospitalAdmission(
            int medicineUnits,
            out MilitaryRearMedicalAdmissionState admission,
            out MilitaryInjuryEpisodeState injury,
            out MilitaryRearMedicalSiteState site,
            MilitaryInjuryProfileDefinitionState additionalProfile = null,
            MilitarySurgicalProcedureDefinitionState additionalProcedure = null)
        {
            var world = BuildFieldHospitalWorld(
                out var army,
                out var project,
                out var localSite,
                out var materialContainer);
            if (additionalProcedure != null)
            {
                world.MilitarySurgicalProcedures.Add(additionalProcedure);
            }
            if (additionalProfile != null)
            {
                world.MilitaryInjuryProfiles.Add(additionalProfile);
            }
            AddOrganizationProductBatch(
                world,
                "product_batch.complex_injury.medicine",
                localSite.OwnerOrganizationId,
                localSite.MedicineInventoryContainerId,
                localSite.LocationId,
                CoreProductionContent.HerbalMedicineMaterialProductId,
                medicineUnits,
                army.CommanderPersonId);
            RelocateArmyForFieldHospitalTest(
                world, army, "location.zhuo");
            var eligible = world.MilitaryServices.FindAll(item =>
                item.ArmyId == army.Id &&
                item.Role == MilitaryServiceRole.Soldier &&
                item.Status == MilitaryServiceStatus.Active);
            eligible.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var patient = eligible[0];
            patient.Status = MilitaryServiceStatus.Wounded;
            patient.LastStatusChangeDay = world.AbsoluteDay;
            world.People.Find(item => item.Id == patient.PersonId)
                .HealthBasisPoints = 1_000;
            var team = new List<MilitaryServiceState>
            {
                eligible[1],
                eligible[2]
            };
            new MilitaryServiceSystem().SynchronizeArmyCaches(world, army.Id);
            var receiver = world.People.Find(item =>
                item.Id == "person.generated.physician_001");
            receiver.MedicalSkillBasisPoints = 7_500;
            receiver.ProfessionalSkills.Medicine = 7_500;
            new PopulationLedgerSystem().MoveIndependentPerson(
                world, receiver, localSite.LocationId);
            var evacuationSystem = new MilitaryMedicalEvacuationSystem();
            var evacuation = evacuationSystem.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(patient.Id),
                team.ConvertAll(item => new StableId(item.Id)),
                new StableId("route.zhuo_zhongshan"),
                new StableId(localSite.LocationId),
                new StableId(receiver.Id));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 28);
            evacuationSystem.Receive(
                world,
                new StableId(evacuation.Id),
                new StableId(receiver.Id));
            var localAdmission = new MilitaryRearMedicalSystem().Admit(
                world,
                new StableId(evacuation.Id),
                new StableId(localSite.Id),
                new StableId(receiver.Id));
            var localInjury = world.MilitaryInjuryEpisodes.Find(item =>
                item.Id == localAdmission.InjuryEpisodeId);
            admission = localAdmission;
            injury = localInjury;
            site = localSite;
            world.Validate();
            return world;
        }

        private static WorldState BuildReadyForReturnWoundDeathWorld(
            bool advancePastWaitingPeriod,
            out MilitaryRearMedicalAdmissionState admission,
            out MilitaryInjuryEpisodeState injury,
            out PersonState patient,
            out PersonState successor,
            out FamilyState family,
            out OrganizationState organization,
            out MilitaryServiceState patientService,
            out ArmyState army)
        {
            var world = BuildInfectedFieldHospitalAdmission(
                10,
                out var localAdmission,
                out var localInjury,
                out var site);
            var rear = new MilitaryRearMedicalSystem();
            rear.TreatInpatient(world, new StableId(localAdmission.Id));
            rear.TreatInpatient(world, new StableId(localAdmission.Id));
            rear.TreatInpatient(world, new StableId(localAdmission.Id));
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            rear.TreatInpatient(world, new StableId(localAdmission.Id));
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == localAdmission.EvacuationId);
            var localArmy = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            var localPatient = world.People.Find(item =>
                item.Id == localAdmission.PatientPersonId);
            var localSuccessor = world.People.Find(item =>
                item.Id == evacuation.TeamMembers[0].PersonId);
            localPatient.FamilyId = "family.test.wound_death";
            localSuccessor.FamilyId = localPatient.FamilyId;
            localPatient.Wealth = 75;
            var localFamily = new FamilyState
            {
                Id = localPatient.FamilyId,
                DisplayName = "伤兵之家",
                HeadPersonId = localPatient.Id,
                Wealth = 1_000,
                LocationId = site.LocationId,
                MemberIds = new List<string>
                {
                    localPatient.Id,
                    localSuccessor.Id
                }
            };
            world.Families.Add(localFamily);
            if (advancePastWaitingPeriod)
            {
                new WorldSimulator(world.MasterSeed).AdvanceDays(world, 1);
            }
            world.Validate();

            admission = localAdmission;
            injury = localInjury;
            patient = localPatient;
            successor = localSuccessor;
            family = localFamily;
            army = localArmy;
            organization = world.Organizations.Find(item =>
                item.Id == localArmy.OrganizationId);
            patientService = world.MilitaryServices.Find(item =>
                item.Id == localAdmission.PatientMilitaryServiceId);
            return world;
        }

        private static void AttachInpatientWoundDeathFamily(
            WorldState world,
            MilitaryRearMedicalAdmissionState admission,
            MilitaryRearMedicalSiteState site,
            out PersonState patient,
            out PersonState successor,
            out FamilyState family,
            out OrganizationState organization,
            out MilitaryServiceState patientService,
            out ArmyState army)
        {
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == admission.EvacuationId);
            patient = world.People.Find(item =>
                item.Id == admission.PatientPersonId);
            successor = world.People.Find(item =>
                item.Id == evacuation.TeamMembers[0].PersonId);
            patient.FamilyId = "family.test.inpatient_wound_death";
            successor.FamilyId = patient.FamilyId;
            patient.Wealth = 75;
            family = new FamilyState
            {
                Id = patient.FamilyId,
                DisplayName = "住院伤兵之家",
                HeadPersonId = patient.Id,
                Wealth = 1_000,
                LocationId = site.LocationId,
                MemberIds = new List<string>
                {
                    patient.Id,
                    successor.Id
                }
            };
            world.Families.Add(family);
            var localArmy = world.Armies.Find(item =>
                item.Id == evacuation.SourceArmyId);
            organization = world.Organizations.Find(item =>
                item.Id == localArmy.OrganizationId);
            army = localArmy;
            patientService = world.MilitaryServices.Find(item =>
                item.Id == admission.PatientMilitaryServiceId);
            world.Validate();
        }

        private static WorldState BuildCompletedRetiredWoundDeathWorld(
            bool advancePastWaitingPeriod,
            out MilitaryRearMedicalAdmissionState admission,
            out MilitaryInjuryEpisodeState injury,
            out PersonState patient,
            out PersonState successor,
            out FamilyState family,
            out OrganizationState organization,
            out MilitaryServiceState patientService,
            out ArmyState army)
        {
            var world = BuildReadyForReturnWoundDeathWorld(
                false,
                out admission,
                out injury,
                out patient,
                out successor,
                out family,
                out organization,
                out patientService,
                out army);
            var localAdmission = admission;
            var evacuation = world.MilitaryMedicalEvacuations.Find(item =>
                item.Id == localAdmission.EvacuationId);
            new MilitaryRearMedicalSystem().StartReturn(
                world,
                new StableId(evacuation.Id),
                new StableId("route.zhuo_zhongshan"));
            var simulator = new WorldSimulator(world.MasterSeed);
            for (var i = 0;
                 i < 40 && evacuation.Status !=
                     MilitaryMedicalEvacuationStatus.Completed;
                 i++)
            {
                simulator.AdvanceSegments(world, 1);
            }
            if (evacuation.Status !=
                MilitaryMedicalEvacuationStatus.Completed)
            {
                throw new InvalidOperationException(
                    "The wound-death fixture could not complete evacuation return.");
            }
            if (advancePastWaitingPeriod)
            {
                simulator.AdvanceDays(world, 1);
            }
            world.Validate();
            return world;
        }

        private static MilitaryRearMedicalSiteState
            BuildMedicalTransferDestination(
                WorldState world,
                string ownerOrganizationId,
                int bedCapacity,
                int openingMedicineUnits,
                out PersonState receiver)
        {
            var targetLocation = world.Locations.Find(item =>
                item.Id == "location.anping");
            targetLocation.Features |= LocationFeature.Clinic;
            receiver = world.People.Find(item =>
                item.Id == "person.generated.farmer_001");
            receiver.MedicalSkillBasisPoints = 7_500;
            receiver.ProfessionalSkills.Medicine = 7_500;
            new PopulationLedgerSystem().MoveIndependentPerson(
                world, receiver, targetLocation.Id);
            var position = world.Positions.Find(item =>
                item.OrganizationId == ownerOrganizationId);
            world.Memberships.Add(new MembershipState
            {
                Id = "membership.medical_transfer.receiver",
                PersonId = receiver.Id,
                OrganizationId = ownerOrganizationId,
                PositionId = position.Id,
                JoinedDay = world.AbsoluteDay,
                LoyaltyBasisPoints = 6_000
            });
            world.Validate();
            return new MilitaryRearMedicalSystem().RegisterExistingClinic(
                world,
                new StableId(targetLocation.Id),
                new StableId(ownerOrganizationId),
                new StableId(receiver.Id),
                bedCapacity,
                openingMedicineUnits);
        }

        private static MilitaryRearMedicalSiteState
            BuildSecondMedicalTransferDestination(
                WorldState world,
                string ownerOrganizationId,
                int bedCapacity,
                int openingMedicineUnits,
                out PersonState receiver)
        {
            var targetLocation = world.Locations.Find(item =>
                item.Id == "location.xiaquyang");
            targetLocation.Features |= LocationFeature.Clinic;
            receiver = world.People.Find(item =>
                item.Id == "person.generated.farmer_002");
            receiver.MedicalSkillBasisPoints = 7_500;
            receiver.ProfessionalSkills.Medicine = 7_500;
            new PopulationLedgerSystem().MoveIndependentPerson(
                world, receiver, targetLocation.Id);
            var position = world.Positions.Find(item =>
                item.OrganizationId == ownerOrganizationId);
            world.Memberships.Add(new MembershipState
            {
                Id = "membership.medical_transfer.second_receiver",
                PersonId = receiver.Id,
                OrganizationId = ownerOrganizationId,
                PositionId = position.Id,
                JoinedDay = world.AbsoluteDay,
                LoyaltyBasisPoints = 6_000
            });
            world.Validate();
            return new MilitaryRearMedicalSystem().RegisterExistingClinic(
                world,
                new StableId(targetLocation.Id),
                new StableId(ownerOrganizationId),
                new StableId(receiver.Id),
                bedCapacity,
                openingMedicineUnits);
        }

        private static void AddOrganizationProductBatch(
            WorldState world,
            string batchId,
            string ownerOrganizationId,
            string containerId,
            string locationId,
            string productDefinitionId,
            long quantity,
            string actorPersonId)
        {
            var transactionId =
                "inventory_transaction." + batchId + ".opening";
            var product = ProductionContentRegistry.CreateCore().GetProduct(
                productDefinitionId);
            var batch = new ProductBatchState
            {
                Id = batchId,
                ProductDefinitionId = product.Id,
                OwnerOrganizationId = ownerOrganizationId,
                InventoryContainerId = containerId,
                OriginLocationId = locationId,
                SourceTransactionId = transactionId,
                UnitId = product.UnitId,
                UnitWeight = product.BaseWeight,
                ProducedDay = world.AbsoluteDay,
                Quantity = quantity,
                QualityBasisPoints = 8_500,
                FreshnessBasisPoints = 9_500,
                QualityDimensions = ProductQualityRules.CreateUniform(
                    product, 8_500)
            };
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = transactionId,
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.OpeningBalance,
                ActorPersonId = actorPersonId,
                Summary = "Field-hospital test opening balance.",
                Lines =
                {
                    new InventoryTransactionLineState
                    {
                        BatchId = batch.Id,
                        ProductDefinitionId = batch.ProductDefinitionId,
                        OwnerOrganizationId = batch.OwnerOrganizationId,
                        InventoryContainerId = batch.InventoryContainerId,
                        UnitId = batch.UnitId,
                        QuantityDelta = quantity
                    }
                }
            });
        }

        private static void RelocateArmyForFieldHospitalTest(
            WorldState world,
            ArmyState army,
            string destinationLocationId)
        {
            var personnel = new List<PersonState>();
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.ArmyId == army.Id &&
                    (service.Status == MilitaryServiceStatus.Active ||
                     service.Status == MilitaryServiceStatus.Mustering ||
                     service.Status == MilitaryServiceStatus.Wounded))
                {
                    personnel.Add(world.People.Find(item =>
                        item.Id == service.PersonId));
                }
            }
            army.LocationId = destinationLocationId;
            world.InventoryContainers.Find(item =>
                item.Id == army.MedicalInventoryContainerId).LocationId =
                    destinationLocationId;
            new PopulationLedgerSystem().MovePeople(
                world, personnel, destinationLocationId, false);
        }

        private static void RelocateSourceArmyWithoutEvacuationParty(
            WorldState world,
            ArmyState army,
            string destinationLocationId)
        {
            var personnel = new List<PersonState>();
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.ArmyId == army.Id &&
                    (service.Status == MilitaryServiceStatus.Active ||
                     service.Status == MilitaryServiceStatus.Mustering))
                {
                    personnel.Add(world.People.Find(item =>
                        item.Id == service.PersonId));
                }
            }
            army.LocationId = destinationLocationId;
            world.InventoryContainers.Find(item =>
                item.Id == army.MedicalInventoryContainerId).LocationId =
                    destinationLocationId;
            new PopulationLedgerSystem().MovePeople(
                world, personnel, destinationLocationId, false);
            world.Validate();
        }

        private static MilitaryMedicalEvacuationState
            DispatchAndReceiveEvacuation(
                WorldState world,
                ArmyState army,
                MilitaryServiceState patientService,
                List<MilitaryServiceState> teamServices,
                PersonState receiver)
        {
            var evacuationSystem = new MilitaryMedicalEvacuationSystem();
            var evacuation = evacuationSystem.Dispatch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(patientService.Id),
                teamServices.ConvertAll(item => new StableId(item.Id)),
                new StableId("route.zhongshan_anping"),
                new StableId("location.anping"),
                new StableId(receiver.Id));
            new WorldSimulator(world.MasterSeed).AdvanceSegments(world, 13);
            evacuationSystem.Receive(
                world,
                new StableId(evacuation.Id),
                new StableId(receiver.Id));
            return evacuation;
        }

        private static NewGameCharacterRequest NewGameRequest(
            StartingIdentity identity)
        {
            return new NewGameCharacterRequest
            {
                DisplayName = "能力测试",
                Age = 20,
                Gender = PersonGender.Male,
                Identity = identity
            };
        }

        private static WorldState PrepareEducationWorld(
            int studentSkill,
            int teacherSkill)
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var student = FindTestStudent(world);
            var teacher = world.People.Find(
                item => item.Id == "person.liu_bei");
            student.Wealth = 10_000;
            student.LifeGoal = LifeGoalKind.WinMerit;
            ProfessionalSkillAccess.Set(
                student.ProfessionalSkills,
                ProfessionalDiscipline.Military,
                studentSkill);
            ProfessionalSkillAccess.Set(
                teacher.ProfessionalSkills,
                ProfessionalDiscipline.Military,
                teacherSkill);
            world.Validate();
            return world;
        }

        private static PersonState FindTestStudent(WorldState world)
        {
            return world.People.Find(
                item => item.Id == "person.generated.farmer_001");
        }

        private static void SetMilitaryAptitude(
            PersonState person,
            int value)
        {
            person.Aptitudes.Reasoning = value;
            person.Aptitudes.Willpower = value;
            person.Aptitudes.Perception = value;
        }

        private static EducationPlanState StartMilitaryStudy(
            WorldState world,
            EducationSystem system,
            string teacherId,
            string practicePositionId = "")
        {
            return system.StartPlan(
                world,
                new StableId(FindTestStudent(world).Id),
                ProfessionalDiscipline.Military,
                10,
                teacherId,
                EducationFundingSource.Personal,
                string.Empty,
                practicePositionId);
        }

        private static void ResolveEducationAtDay(
            WorldState world,
            long day,
            EducationSystem system)
        {
            world.AbsoluteDay = day;
            system.ResolveDuePlans(world);
            world.Validate();
        }

        private static void AddMilitaryPracticeMembership(WorldState world)
        {
            var student = FindTestStudent(world);
            world.Memberships.Add(new MembershipState
            {
                Id = "membership.education_test.youzhou",
                PersonId = student.Id,
                OrganizationId = "organization.youzhou_field_force",
                PositionId = "position.youzhou_soldier",
                JoinedDay = world.AbsoluteDay,
                LoyaltyBasisPoints = 5_000
            });
            world.Validate();
        }

        private static WorldState BuildMinimalWorld()
        {
            var world = WorldState.Create(184);
            world.Locations.Add(new LocationState
            {
                Id = "location.zhuo",
                DisplayName = "涿县",
                Population = 20_000
            });
            world.People.Add(new PersonState
            {
                Id = "person.liu_bei",
                DisplayName = "刘备",
                LocationId = "location.zhuo",
                BirthDay = -5_000
            });
            world.People.Add(new PersonState
            {
                Id = "person.guan_yu",
                DisplayName = "关羽",
                LocationId = "location.zhuo",
                BirthDay = -5_000
            });
            world.Families.Add(new FamilyState
            {
                Id = "family.liu_bei_household",
                DisplayName = "刘备家",
                HeadPersonId = "person.liu_bei",
                Wealth = 1_000,
                MemberIds = { "person.liu_bei" }
            });
            world.Validate();
            return world;
        }

        [Test]
        public void WorldScheduler_OrdersStableIdsAndHonorsCadence()
        {
            var world = WorldState.Create(25);
            var executed = new List<string>();
            var scheduler = new WorldSystemScheduler();
            scheduler.Register(new WorldScheduledSystem(
                "test.scheduler.b",
                WorldSystemPhase.SegmentMovement,
                WorldSystemCadence.EverySegment,
                10,
                context => executed.Add("b")));
            scheduler.Register(new WorldScheduledSystem(
                "test.scheduler.a",
                WorldSystemPhase.SegmentMovement,
                WorldSystemCadence.EverySegment,
                10,
                context => executed.Add("a")));
            scheduler.Register(new WorldScheduledSystem(
                "test.scheduler.daily",
                WorldSystemPhase.DailyCommand,
                WorldSystemCadence.NewDay,
                10,
                context => executed.Add("daily")));

            scheduler.BeginTrace();
            scheduler.ExecutePhase(
                WorldSystemPhase.SegmentMovement,
                new WorldSystemExecutionContext(world, false));
            scheduler.ExecutePhase(
                WorldSystemPhase.DailyCommand,
                new WorldSystemExecutionContext(world, false));

            Assert.That(executed, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(
                scheduler.LastExecutionTrace,
                Is.EqualTo(new[]
                {
                    "test.scheduler.a",
                    "test.scheduler.b"
                }));

            scheduler.ExecutePhase(
                WorldSystemPhase.DailyCommand,
                new WorldSystemExecutionContext(world, true));
            Assert.That(executed, Is.EqualTo(new[] { "a", "b", "daily" }));
            Assert.Throws<InvalidOperationException>(() =>
                scheduler.Register(new WorldScheduledSystem(
                    "test.scheduler.a",
                    WorldSystemPhase.DailySimulation,
                    WorldSystemCadence.NewDay,
                    20,
                    context => { })));
        }

        [Test]
        public void WorldCommandRuntime_CommitsInStableOrderAndDispatchesAfterCommit()
        {
            var firstWorld = WorldState.Create(26);
            firstWorld.AbsoluteDay = 5;
            var firstRuntime = BuildRevisionCommandRuntime(out var firstEvents);
            firstRuntime.Enqueue(firstWorld, BuildRevisionCommand("test.command.b", 2, 5));
            firstRuntime.Enqueue(firstWorld, BuildRevisionCommand("test.command.future", 8, 6));
            firstRuntime.Enqueue(firstWorld, BuildRevisionCommand("test.command.a", 1, 5));

            var firstReport = firstRuntime.ProcessDue(firstWorld);
            firstRuntime.DispatchPublishedEvents();

            Assert.That(firstReport.ProcessedCommands, Is.EqualTo(2));
            Assert.That(firstReport.CommittedTransactions, Is.EqualTo(2));
            Assert.That(firstReport.PublishedEvents, Is.EqualTo(2));
            Assert.That(firstRuntime.PendingCommandCount, Is.EqualTo(1));
            Assert.That(firstWorld.Revision, Is.EqualTo(3));
            Assert.That(firstWorld.WorldCommandBatchResults.Count, Is.EqualTo(1));
            Assert.That(firstWorld.WorldEventOutbox.Count, Is.EqualTo(2));
            Assert.That(
                firstWorld.WorldEventOutbox.TrueForAll(item =>
                    item.DispatchStatus == WorldEventDispatchStatus.Dispatched),
                Is.True);
            Assert.That(
                firstRuntime.PublishedEvents[0].Id,
                Is.EqualTo("test.command.a.applied"));
            Assert.That(
                firstRuntime.PublishedEvents[1].Id,
                Is.EqualTo("test.command.b.applied"));
            Assert.That(
                firstEvents.HandledEventIds,
                Is.EqualTo(new[]
                {
                    "test.command.a.applied",
                    "test.command.b.applied"
                }));

            var secondWorld = WorldState.Create(26);
            secondWorld.AbsoluteDay = 5;
            var secondRuntime = BuildRevisionCommandRuntime(out var ignoredEvents);
            secondRuntime.Enqueue(secondWorld, BuildRevisionCommand("test.command.a", 1, 5));
            secondRuntime.Enqueue(secondWorld, BuildRevisionCommand("test.command.b", 2, 5));
            secondRuntime.ProcessDue(secondWorld);

            Assert.That(secondWorld.Revision, Is.EqualTo(firstWorld.Revision));
            Assert.That(
                secondRuntime.PublishedEvents[0].Id,
                Is.EqualTo(firstRuntime.PublishedEvents[0].Id));
            Assert.That(
                secondRuntime.PublishedEvents[1].Id,
                Is.EqualTo(firstRuntime.PublishedEvents[1].Id));
        }

        [Test]
        public void WorldCommandRuntime_ValidationFailureDoesNotApplyTransactions()
        {
            var world = WorldState.Create(27);
            world.AbsoluteDay = 3;
            var runtime = BuildRevisionCommandRuntime(out var ignoredEvents);
            runtime.Enqueue(world, BuildRevisionCommand("test.command.invalid", -1, 3));

            Assert.Throws<InvalidOperationException>(() => runtime.ProcessDue(world));
            Assert.That(world.Revision, Is.EqualTo(0));
            Assert.That(runtime.PendingCommandCount, Is.EqualTo(1));
            Assert.That(runtime.PublishedEvents.Count, Is.EqualTo(0));
            Assert.That(world.WorldCommandBatchResults.Count, Is.EqualTo(1));
            Assert.That(
                world.WorldCommandBatchResults[0].Outcome,
                Is.EqualTo(WorldCommandBatchOutcome.Rejected));
            Assert.That(world.PersistentWorldCommands[0].AttemptCount, Is.EqualTo(1));
        }

        [Test]
        public void WorldCommandRuntime_ReservationConflictRejectsWholeBatch()
        {
            var world = WorldState.Create(29);
            world.AbsoluteDay = 3;
            var runtime = BuildRevisionCommandRuntime(out var ignoredEvents);
            runtime.Enqueue(world, BuildRevisionCommand("test.command.reserve_a", 6, 3));
            runtime.Enqueue(world, BuildRevisionCommand("test.command.reserve_b", 6, 3));

            Assert.Throws<InvalidOperationException>(() => runtime.ProcessDue(world));
            Assert.That(world.Revision, Is.EqualTo(0));
            Assert.That(runtime.PendingCommandCount, Is.EqualTo(2));
            Assert.That(runtime.PublishedEvents.Count, Is.EqualTo(0));
            Assert.That(world.WorldCommandBatchResults.Count, Is.EqualTo(1));
            Assert.That(
                world.PersistentWorldCommands.TrueForAll(command =>
                    command.AttemptCount == 1 &&
                    command.Status == PersistentWorldCommandStatus.Pending),
                Is.True);
        }

        [Test]
        public void WorldCommandPersistence_FutureCommandRoundTripsAndExecutesOnce()
        {
            var world = WorldState.Create(30);
            world.AbsoluteDay = 5;
            var runtime = BuildRevisionCommandRuntime(out var ignoredEvents);
            runtime.Enqueue(
                world,
                BuildRevisionCommand("test.command.future_persisted", 4, 6));

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            loaded.AbsoluteDay = 6;
            var resumedRuntime = BuildRevisionCommandRuntime(
                out var resumedEvents);

            var report = resumedRuntime.ProcessDue(loaded);
            resumedRuntime.DispatchPublishedEvents(loaded);
            var secondReport = resumedRuntime.ProcessDue(loaded);

            Assert.That(report.ProcessedCommands, Is.EqualTo(1));
            Assert.That(secondReport.ProcessedCommands, Is.EqualTo(0));
            Assert.That(loaded.Revision, Is.EqualTo(4));
            Assert.That(
                loaded.PersistentWorldCommands[0].Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));
            Assert.That(loaded.PersistentWorldCommands[0].AttemptCount, Is.EqualTo(1));
            Assert.That(resumedEvents.HandledEventIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void WorldEventOutbox_SurvivesReloadAndAcknowledgesHandlerOnce()
        {
            var world = WorldState.Create(31);
            world.AbsoluteDay = 5;
            var runtime = BuildRevisionCommandRuntime(out var ignoredEvents);
            runtime.Enqueue(
                world,
                BuildRevisionCommand("test.command.outbox_persisted", 2, 5));
            runtime.ProcessDue(world);

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var resumedRuntime = BuildRevisionCommandRuntime(
                out var resumedEvents);
            resumedRuntime.ProcessDue(loaded);
            resumedRuntime.DispatchPublishedEvents(loaded);
            resumedRuntime.DispatchPublishedEvents(loaded);

            Assert.That(resumedEvents.HandledEventIds.Count, Is.EqualTo(1));
            Assert.That(
                loaded.WorldEventOutbox[0].DispatchStatus,
                Is.EqualTo(WorldEventDispatchStatus.Dispatched));
            Assert.That(
                loaded.WorldEventOutbox[0].DeliveredHandlerIds,
                Is.EqualTo(new[] { "test.handler.revision" }));
            var roundTripped = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(loaded));
            Assert.That(
                roundTripped.WorldEventOutbox[0].DeliveredHandlerIds,
                Is.EqualTo(new[] { "test.handler.revision" }));
        }

        [Test]
        public void WorldEventOutbox_FailedHandlerIsNotAcknowledged()
        {
            var world = WorldState.Create(35);
            world.AbsoluteDay = 5;
            var runtime = BuildRevisionCommandRuntime(out var ignoredEvents);
            runtime.RegisterEventHandler(new ThrowingRevisionEventHandler());
            runtime.Enqueue(
                world,
                BuildRevisionCommand("test.command.failed_dispatch", 1, 5));
            runtime.ProcessDue(world);

            Assert.Throws<InvalidOperationException>(() =>
                runtime.DispatchPublishedEvents(world));

            Assert.That(
                world.WorldEventOutbox[0].DispatchStatus,
                Is.EqualTo(WorldEventDispatchStatus.Pending));
            Assert.That(
                world.WorldEventOutbox[0].DeliveredHandlerIds,
                Is.EqualTo(new[] { "test.handler.revision" }));
            world.Validate();
        }

        [Test]
        public void WorldCommandPersistence_SameFactsProduceSameSnapshot()
        {
            var first = WorldState.Create(32);
            first.AbsoluteDay = 5;
            var firstRuntime = BuildRevisionCommandRuntime(out var ignoredFirst);
            firstRuntime.Enqueue(first, BuildRevisionCommand("test.command.b", 2, 5));
            firstRuntime.Enqueue(first, BuildRevisionCommand("test.command.a", 1, 5));
            firstRuntime.ProcessDue(first);
            firstRuntime.DispatchPublishedEvents(first);

            var second = WorldState.Create(32);
            second.AbsoluteDay = 5;
            var secondRuntime = BuildRevisionCommandRuntime(out var ignoredSecond);
            secondRuntime.Enqueue(second, BuildRevisionCommand("test.command.a", 1, 5));
            secondRuntime.Enqueue(second, BuildRevisionCommand("test.command.b", 2, 5));
            secondRuntime.ProcessDue(second);
            secondRuntime.DispatchPublishedEvents(second);

            Assert.That(
                WorldSnapshotSerializer.Serialize(second),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(first)));
        }

        [Test]
        public void Snapshot_MigratesVersionThirtyTwoToEmptyPersistentExecution()
        {
            var world = WorldState.Create(33);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 32");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.PersistentWorldCommands, Is.Empty);
            Assert.That(loaded.WorldCommandBatchResults, Is.Empty);
            Assert.That(loaded.WorldEventOutbox, Is.Empty);
            Assert.That(loaded.PublicReliefProcurementTrades, Is.Empty);
        }

        [Test]
        public void WorldCommandPersistence_RejectsTamperedOutboxTransactionLink()
        {
            var world = WorldState.Create(34);
            world.AbsoluteDay = 5;
            var runtime = BuildRevisionCommandRuntime(out var ignoredEvents);
            runtime.Enqueue(
                world,
                BuildRevisionCommand("test.command.tampered", 1, 5));
            runtime.ProcessDue(world);
            world.WorldEventOutbox[0].SourceTransactionId =
                "test.transaction.missing";

            Assert.Throws<InvalidOperationException>(() => world.Validate());
        }

        [Test]
        public void WorldSimulator_RunsExistingSystemsThroughRegisteredPhases()
        {
            var world = WorldState.Create(28);
            world.Segment = (byte)DaySegment.Night;
            var simulator = new WorldSimulator(world.MasterSeed);

            simulator.AdvanceSegments(world, 1);

            Assert.That(world.AbsoluteDay, Is.EqualTo(1));
            Assert.That(world.Segment, Is.EqualTo((byte)DaySegment.Dawn));
            Assert.That(
                simulator.Scheduler.LastExecutionTrace[0],
                Is.EqualTo("mandate.runtime.segment.command"));
            Assert.That(
                simulator.Scheduler.LastExecutionTrace[1],
                Is.EqualTo("mandate.runtime.segment.runtime_events"));
            Assert.That(
                simulator.Scheduler.LastExecutionTrace[2],
                Is.EqualTo("mandate.runtime.segment.travel"));
            Assert.That(
                simulator.Scheduler.LastExecutionTrace[3],
                Is.EqualTo("mandate.runtime.segment.army_march"));
            Assert.That(
                simulator.Scheduler.LastExecutionTrace,
                Does.Contain("mandate.runtime.daily.command"));
            Assert.That(
                simulator.Scheduler.LastExecutionTrace,
                Does.Contain("mandate.runtime.daily.runtime_events"));
            Assert.That(
                simulator.CommandRuntime.LastReport.ProcessedCommands,
                Is.EqualTo(0));
        }

        private static WorldCommandRuntime BuildRevisionCommandRuntime(
            out RevisionCommandEventHandler eventHandler)
        {
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(new RevisionCommandHandler());
            eventHandler = new RevisionCommandEventHandler();
            runtime.RegisterEventHandler(eventHandler);
            return runtime;
        }

        private static WorldCommandEnvelope BuildRevisionCommand(
            string id,
            int delta,
            long dueDay)
        {
            return new WorldCommandEnvelope(
                id,
                "test.command.revision",
                "person.test.issuer",
                dueDay,
                DaySegment.Dawn,
                10,
                new Dictionary<string, string>
                {
                    { "delta", delta.ToString() }
                });
        }

        private sealed class RevisionCommandHandler : IWorldCommandHandler
        {
            public string CommandTypeId => "test.command.revision";

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                transactions.Add(new RevisionTransaction(
                    command.Id + ".transaction",
                    int.Parse(command.Arguments["delta"]),
                    command.Id + ".applied"));
            }
        }

        private sealed class RevisionTransaction : IWorldTransaction
        {
            private readonly int _delta;
            private readonly string _eventId;

            public RevisionTransaction(string id, int delta, string eventId)
            {
                Id = id;
                _delta = delta;
                _eventId = eventId;
            }

            public string Id { get; }

            public string KindId => "test.transaction.revision";

            public int Priority => 10;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                if (_delta <= 0)
                {
                    throw new InvalidOperationException(
                        "Revision deltas must be positive.");
                }

                validation.Reserve(
                    "test.resource.revision",
                    _delta,
                    10,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                world.Revision = checked(world.Revision + _delta);
                events.Add(new WorldRuntimeEvent(
                    _eventId,
                    "test.event.revision_applied",
                    Id,
                    world.AbsoluteDay,
                    (DaySegment)world.Segment));
            }
        }

        private sealed class RevisionCommandEventHandler : IWorldRuntimeEventHandler
        {
            public List<string> HandledEventIds { get; } = new List<string>();

            public string HandlerId => "test.handler.revision";

            public string EventTypeId => "test.event.revision_applied";

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                HandledEventIds.Add(worldEvent.Id);
            }
        }

        private sealed class ThrowingRevisionEventHandler :
            IWorldRuntimeEventHandler
        {
            public string HandlerId => "test.handler.throwing";

            public string EventTypeId => "test.event.revision_applied";

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                throw new InvalidOperationException(
                    "Synthetic event-handler failure.");
            }
        }

        private sealed class FoodStorageFixture
        {
            public WorldState World;
            public ProductionContentRegistry Content;
        }

        private static PersonState BuildIncrementalNewPerson()
        {
            return new PersonState
            {
                Id = "person.generated.incremental_newborn",
                DisplayName = "Incremental Newborn",
                LocationId = "location.zhuo",
                BirthLocationId = "location.zhuo",
                BirthDay = 0,
                CountsTowardPopulation = false,
                VillageOccupation = VillageOccupation.Dependent,
                LaborCapacityBasisPoints = 0,
                NextIndependentEventDay = 30,
                NextIndependentEventReason =
                    "monthly_household_settlement"
            };
        }

        private static WorldState PrepareAutomaticHerbalSupplyWorld(
            ProductionContentRegistry content,
            ulong seed)
        {
            var world = VillagePrototypeFactory.Create(200, seed);
            var openingMedicine = world.ProductBatches.Find(item =>
                item.ProductDefinitionId == CoreProductionContent
                    .HerbalMedicineMaterialProductId);
            world.InventoryTransactions.RemoveAll(item =>
                item.Id == openingMedicine.SourceTransactionId);
            world.ProductBatches.Remove(openingMedicine);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            var physician = world.People.Find(item =>
                item.VillageOccupation == VillageOccupation.Physician);
            var family = world.Families.Find(item =>
                item.Id == physician.FamilyId);
            var storage = world.VillageFacilities.Find(item =>
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == family.Id);
            family.Wealth = 10_000;
            storage.Capacity += 1_000;
            world.Validate();
            return world;
        }

        private static void ResolveTwoNutritionDeficitMonths(
            WorldState world,
            string personId)
        {
            var system = new LongTermNutritionSystem();
            for (var day = 30L; day <= 60L; day += 30L)
            {
                world.AbsoluteDay = day;
                system.RecordMonthlySettlement(
                    world,
                    day,
                    new List<FormalHouseholdFoodPersonSettlementResult>
                    {
                        new FormalHouseholdFoodPersonSettlementResult
                        {
                            PersonId = personId,
                            RequiredNutritionBasisUnits = 30_000,
                            MissingNutritionBasisUnits = 30_000
                        }
                    });
            }
        }

        private static long ArmyMedicineQuantity(
            WorldState world,
            ArmyState army)
        {
            long quantity = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId ==
                        army.MedicalInventoryContainerId &&
                    batch.ProductDefinitionId == CoreProductionContent
                        .HerbalMedicineMaterialProductId)
                {
                    quantity = checked(quantity + batch.Quantity);
                }
            }
            return quantity;
        }

        private static void RemoveArmyMedicine(
            WorldState world,
            ArmyState army)
        {
            var transactionIds = new List<string>();
            for (var i = world.ProductBatches.Count - 1; i >= 0; i--)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId !=
                        army.MedicalInventoryContainerId ||
                    batch.ProductDefinitionId != CoreProductionContent
                        .HerbalMedicineMaterialProductId)
                {
                    continue;
                }
                transactionIds.Add(batch.SourceTransactionId);
                world.ProductBatches.RemoveAt(i);
            }
            world.InventoryTransactions.RemoveAll(item =>
                transactionIds.Contains(item.Id));
            world.Validate();
        }

        private static FamilyState MoveMedicalPatientToSeparateHousehold(
            WorldState world,
            long wealth)
        {
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var oldFamily = world.Families.Find(item =>
                item.Id == patient.FamilyId);
            oldFamily.MemberIds.Remove(patient.Id);
            oldFamily.HeadPersonId = oldFamily.MemberIds[0];
            var family = new FamilyState
            {
                Id = "family.test.medical_patient",
                DisplayName = "病患家",
                HeadPersonId = patient.Id,
                Wealth = wealth,
                LocationId = patient.LocationId,
                MemberIds = { patient.Id }
            };
            patient.FamilyId = family.Id;
            world.Families.Add(family);
            world.Validate();
            return family;
        }

        private static string AddCivilianMedicalPatient(
            WorldState world,
            int index)
        {
            var personId = $"person.test.medical_patient_{index}";
            var familyId = $"family.test.medical_patient_{index}";
            world.People.Add(new PersonState
            {
                Id = personId,
                DisplayName = $"病患{index}",
                LocationId = "location.zhuo",
                BirthLocationId = "location.zhuo",
                FamilyId = familyId,
                BirthDay = -7_200,
                HealthBasisPoints = 10_000
            });
            world.Families.Add(new FamilyState
            {
                Id = familyId,
                DisplayName = $"病患{index}家",
                HeadPersonId = personId,
                Wealth = 1_000,
                LocationId = "location.zhuo",
                MemberIds = { personId }
            });
            ResolveTwoNutritionDeficitMonths(world, personId);
            return personId;
        }

        private static WorldState BuildCivilianMedicalWorld(
            bool includeMedicine,
            bool patientIsMinor)
        {
            var world = BuildMinimalWorld();
            var family = world.Families[0];
            var patient = world.People.Find(item => item.Id == "person.liu_bei");
            var physician = world.People.Find(item => item.Id == "person.guan_yu");
            patient.FamilyId = family.Id;
            physician.FamilyId = family.Id;
            physician.VillageOccupation = VillageOccupation.Physician;
            physician.MedicalSkillBasisPoints = 7_500;
            physician.ProfessionalSkills.Medicine = 7_500;
            physician.BirthDay = -10_800;
            family.MemberIds.Add(physician.Id);
            if (patientIsMinor)
            {
                patient.BirthDay = -3_540;
            }
            else
            {
                patient.BirthDay = -7_200;
            }
            ResolveTwoNutritionDeficitMonths(world, patient.Id);
            if (includeMedicine)
            {
                var container = new InventoryContainerState
                {
                    Id = "inventory.test.medical_chest",
                    KindId = "inventory.village_clinic",
                    OwnerFamilyId = family.Id,
                    LocationId = patient.LocationId,
                    CapacityWeight = 10
                };
                world.InventoryContainers.Add(container);
                new ProductInventorySystem()
                    .CreateFamilyContainerOpeningBatch(
                        world,
                        family.Id,
                        container.Id,
                        physician.Id,
                        CoreProductionContent
                            .HerbalMedicineMaterialProductId,
                        5,
                        8_000);
            }
            world.Validate();
            return world;
        }
    }
}
