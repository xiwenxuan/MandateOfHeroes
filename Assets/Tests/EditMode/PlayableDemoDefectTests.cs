using System;
using System.Linq;
using Mandate.Domain;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed class PlayableDemoDefectTests
    {
        [Test]
        public void ExistingFarmerTakeoverPreservesWorldFacts()
        {
            var baseline = PrototypeWorldFactory.Create184World(184);
            var baselineFamily = baseline.Families.Find(item =>
                item.Id == "family.zhuo_farm_household");
            var world = new NewGameSetupService().CreateExisting184World(
                "person.generated.farmer_001",
                184);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var family = world.Families.Find(item => item.Id == player.FamilyId);

            Assert.That(world.People.Count, Is.EqualTo(baseline.People.Count));
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
        }

        [Test]
        public void SoldierStartOnlyAcceptsArmyAssemblyLocation()
        {
            var setup = new NewGameSetupService();
            var preview = PrototypeWorldFactory.Create184World(184);
            var army = preview.Armies.Find(item =>
                item.Id == "army.youzhou_reinforcement");

            Assert.That(
                setup.GetLegalStartingLocationIds(preview, StartingIdentity.Soldier),
                Is.EqualTo(new[] { army.LocationId }));
            Assert.Throws<ArgumentException>(() => setup.CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "错地新卒",
                    Age = 20,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Soldier,
                    StartingLocationId = "location.guangzong"
                },
                184));
        }

        [Test]
        public void MerchantBuyAvailabilityUsesLiveMarketCost()
        {
            var world = CreateCharacter(
                "试价布商",
                StartingIdentity.Merchant,
                "location.guangzong");
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
        }

        [Test]
        public void HistoricalRumorRequiresRelevantLocation()
        {
            var remote = CreateCharacter(
                "涿县书佐",
                StartingIdentity.CountyClerk,
                "location.zhuo");
            remote.AbsoluteDay = 10;
            var remoteActions = new PlayerActionService(
                new WorldSimulator(remote.MasterSeed));
            Assert.That(remoteActions.QueryActions(
                    remote, remote.PlayerPersonId).Any(item =>
                        item.Id == PlayerActionIds.HistoricalReport),
                Is.False);

            var local = CreateCharacter(
                "广宗访客",
                StartingIdentity.CountyClerk,
                "location.guangzong");
            local.AbsoluteDay = 10;
            var localActions = new PlayerActionService(
                new WorldSimulator(local.MasterSeed));
            Assert.That(localActions.QueryActions(
                    local, local.PlayerPersonId).Any(item =>
                        item.Id == PlayerActionIds.HistoricalReport),
                Is.True);
        }

        [Test]
        public void FieldCareWithoutPhysicianConsumesNoDay()
        {
            var world = CreateCharacter(
                "待治新卒",
                StartingIdentity.Soldier,
                string.Empty);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var service = world.MilitaryServices.Find(item =>
                item.PersonId == player.Id);
            service.Status = MilitaryServiceStatus.Wounded;
            player.HealthBasisPoints = 4_000;
            for (var i = 0; i < world.People.Count; i++)
            {
                world.People[i].MedicalSkillBasisPoints = 0;
                world.People[i].ProfessionalSkills.Medicine = 0;
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
        }

        [Test]
        public void FieldCareTargetsControlledPerson()
        {
            var world = CreateCharacter(
                "受伤新卒",
                StartingIdentity.Soldier,
                string.Empty);
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

            var result = actions.Execute(
                world, player.Id, PlayerActionIds.FieldCare);

            Assert.That(result.Success, Is.True, result.Summary);
            Assert.That(result.DaysAdvanced, Is.EqualTo(1));
            Assert.That(service.Status, Is.EqualTo(MilitaryServiceStatus.Active));
            Assert.That(other.Status, Is.EqualTo(MilitaryServiceStatus.Wounded));
            Assert.That(player.HealthBasisPoints, Is.GreaterThan(4_000));
        }

        private static WorldState CreateCharacter(
            string displayName,
            StartingIdentity identity,
            string startingLocationId)
        {
            return new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = displayName,
                    Age = 24,
                    Gender = PersonGender.Male,
                    Identity = identity,
                    StartingLocationId = startingLocationId
                },
                184);
        }
    }
}
