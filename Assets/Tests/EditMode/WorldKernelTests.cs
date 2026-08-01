using System;
using System.Collections.Generic;
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

            Assert.That(loaded.SchemaVersion, Is.EqualTo(6));
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
        public void Snapshot_MigratesVersionFiveFamilyReferencesToVersionSix()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": 6", "\"SchemaVersion\": 5");

            var loaded = WorldSnapshotSerializer.Deserialize(json);
            var family = loaded.Families[0];
            var member = loaded.People.Find(
                person => person.Id == family.MemberIds[0]);

            Assert.That(loaded.SchemaVersion, Is.EqualTo(6));
            Assert.That(loaded.Villages, Is.Not.Null);
            Assert.That(loaded.VillageFacilities, Is.Not.Null);
            Assert.That(loaded.VillageLedgerEntries, Is.Not.Null);
            Assert.That(member.FamilyId, Is.EqualTo(family.Id));
            Assert.That(member.BirthLocationId, Is.Not.Empty);
            loaded.Validate();
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
    }
}
