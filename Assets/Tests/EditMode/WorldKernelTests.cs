using System;
using System.Collections.Generic;
using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed class WorldKernelTests
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
            Assert.That(
                new TradingSystem().Buy(
                    first,
                    new StableId(firstPhysician.Id),
                    new StableId("commodity.herbs"),
                    5).Success,
                Is.True);
            Assert.That(
                new TradingSystem().Buy(
                    second,
                    new StableId(secondPhysician.Id),
                    new StableId("commodity.herbs"),
                    5).Success,
                Is.True);

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
            var remainingHerbs = first.Inventories.Find(
                item =>
                    item.OwnerPersonId == firstPhysician.Id &&
                    item.CommodityId == "commodity.herbs");
            Assert.That(remainingHerbs, Is.Not.Null);
            Assert.That(
                remainingHerbs.Quantity,
                Is.EqualTo(5 - firstResult.HerbsConsumed));
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
            Assert.That(
                new TradingSystem().Buy(
                    inline,
                    new StableId(inlinePhysician.Id),
                    new StableId("commodity.herbs"),
                    5).Success,
                Is.True);
            Assert.That(
                new TradingSystem().Buy(
                    accessed,
                    new StableId(accessedPhysician.Id),
                    new StableId("commodity.herbs"),
                    5).Success,
                Is.True);
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
                accessedResult.RecoveredTroops));
            Assert.That(changedPeople, Does.Not.Contain(accessedPhysician.Id));
            for (var i = 0; i < changedPeople.Count; i++)
            {
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
            new TradingSystem().Buy(
                world,
                new StableId(physician.Id),
                new StableId("commodity.herbs"),
                5);
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
            Assert.That(player.LocationId, Is.EqualTo("location.zhuo"));
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
                StartingIdentity.Physician
            };
            var expectedLocations = new[]
            {
                "location.zhuo",
                "location.zhuo",
                "location.zhongshan",
                "location.guangzong"
            };
            var expectedPositions = new[]
            {
                "position.youzhou_soldier",
                "position.zhuo_county_clerk",
                "position.zhongshan_trader",
                "position.guangzong_physician"
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
                .Replace("\"SchemaVersion\": 17", "\"SchemaVersion\": 12");
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
            Assert.That(types, Does.Contain(VillageLedgerEntryType.MedicalCare));
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
                "\"SchemaVersion\": 17", "\"SchemaVersion\": 5");

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
                "\"SchemaVersion\": 17", "\"SchemaVersion\": 6");

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
            Assert.That(fromResource.ProductCount, Is.EqualTo(29));
            Assert.That(fromResource.RecipeCount, Is.EqualTo(15));
            Assert.That(fromResource.MethodCount, Is.EqualTo(13));
            Assert.That(fromResource.SkillCount, Is.EqualTo(1));
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
                "\"SchemaVersion\": 17", "\"SchemaVersion\": 7");

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
                .Replace("\"SchemaVersion\": 17", "\"SchemaVersion\": 8")
                .Replace(
                    "\"ContentSchemaVersion\": 2",
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
                Is.EqualTo(2));
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
            Assert.That(world.InventoryTransactions.Count, Is.EqualTo(2));
            world.Validate();
        }

        [Test]
        public void Processing_WheatToDryRationIsBalancedAndRoundTrips()
        {
            var world = VillagePrototypeFactory.Create(200, 22_002);
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
            Assert.That(loaded.InventoryTransactions.Count, Is.EqualTo(5));
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
                .Replace("\"SchemaVersion\": 17", "\"SchemaVersion\": 9")
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
                .Replace("\"SchemaVersion\": 17", "\"SchemaVersion\": 9")
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
                .Replace("\"SchemaVersion\": 17", "\"SchemaVersion\": 10")
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
                .Replace("\"SchemaVersion\": 17", "\"SchemaVersion\": 11")
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
                .Replace("\"SchemaVersion\": 17", "\"SchemaVersion\": 14");

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
                .Replace("\"SchemaVersion\": 17", "\"SchemaVersion\": 15");

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
                .Replace("\"SchemaVersion\": 17", "\"SchemaVersion\": 16");

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
        public void MilitaryProcurement_PrototypeCreatesMappedSupplierStock()
        {
            var world = PrototypeWorldFactory.Create184World(184);

            Assert.That(world.InventoryContainers.Count, Is.EqualTo(2));
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
                .Replace("\"SchemaVersion\": 17", "\"SchemaVersion\": 13");

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
                CategoryTags = new List<string> { "product.seed" }
            });
            package.Products.Add(new ProductDefinition
            {
                Id = "product.mod_test.example_harvest",
                DisplayName = "测试收获物",
                UnitId = CoreProductionContent.GrainUnitId,
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
    }
}
