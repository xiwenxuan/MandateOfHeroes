using System;
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
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            var secondOutcome = new BattleResolver(second.MasterSeed).Resolve(
                second,
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
                new StableId("army.han_jizhou_vanguard"),
                new StableId("army.yellow_turban_guangzong"));
            new BattleResolver(second.MasterSeed).Resolve(
                second,
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
            Assert.That(first.Inventories.Count, Is.EqualTo(0));
        }

        [Test]
        public void MedicalTreatment_WithoutHerbsLeavesWoundedUnchanged()
        {
            var world = BuildGuangzongBattleWorld();
            new BattleResolver(world.MasterSeed).Resolve(
                world,
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
            army.WoundedTroops = 240;
            army.Troops -= 240;

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
            Assert.That(medicine.PrimaryMetric, Is.EqualTo("伤病241"));
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

        private static WorldState BuildGuangzongBattleWorld()
        {
            var world = PrototypeWorldFactory.Create184World(184);
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 30);
            new ArmySystem().StartMarch(
                world,
                new StableId("army.han_jizhou_vanguard"),
                new StableId("route.xiaquyang_guangzong"),
                new StableId("location.guangzong"));
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 8);
            return world;
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
