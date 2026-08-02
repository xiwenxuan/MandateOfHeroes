using System;
using Mandate.Domain;

namespace Mandate.Persistence
{
    public static class WorldSnapshotMigrator
    {
        public static WorldState MigrateToCurrent(
            WorldState world,
            ProductionContentRegistry productionContent = null)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.SchemaVersion <= 0 ||
                world.SchemaVersion > WorldState.CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Unsupported world schema {world.SchemaVersion}.");
            }

            var content = productionContent ??
                          ProductionContentRegistry.CreateCore();

            while (world.SchemaVersion < WorldState.CurrentSchemaVersion)
            {
                switch (world.SchemaVersion)
                {
                    case 1:
                        MigrateVersionOneToTwo(world);
                        break;
                    case 2:
                        MigrateVersionTwoToThree(world);
                        break;
                    case 3:
                        MigrateVersionThreeToFour(world);
                        break;
                    case 4:
                        MigrateVersionFourToFive(world);
                        break;
                    case 5:
                        MigrateVersionFiveToSix(world);
                        break;
                    case 6:
                        MigrateVersionSixToSeven(world);
                        break;
                    case 7:
                        MigrateVersionSevenToEight(world);
                        break;
                    case 8:
                        MigrateVersionEightToNine(world);
                        break;
                    case 9:
                        MigrateVersionNineToTen(world, content);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"No migration path from schema {world.SchemaVersion}.");
                }
            }

            return world;
        }

        private static void MigrateVersionOneToTwo(WorldState world)
        {
            PopulationLedgerBootstrap.Initialize(world);
            world.SchemaVersion = 2;
        }

        private static void MigrateVersionTwoToThree(WorldState world)
        {
            CharacterAbilityBootstrap.InitializeWorld(world);
            world.SchemaVersion = 3;
        }

        private static void MigrateVersionThreeToFour(WorldState world)
        {
            world.EducationPlans ??= new System.Collections.Generic.List<
                EducationPlanState>();
            world.LearningRecords ??= new System.Collections.Generic.List<
                LearningRecordState>();
            world.SchemaVersion = 4;
        }

        private static void MigrateVersionFourToFive(WorldState world)
        {
            world.MilitaryFormations ??= new System.Collections.Generic.List<
                MilitaryFormationState>();
            world.MilitaryServices ??= new System.Collections.Generic.List<
                MilitaryServiceState>();
            world.MilitaryOrders ??= new System.Collections.Generic.List<
                MilitaryOrderState>();
            world.MilitaryServiceInitialized = false;
            world.SchemaVersion = 5;
        }

        private static void MigrateVersionFiveToSix(WorldState world)
        {
            world.Villages ??= new System.Collections.Generic.List<
                VillageState>();
            world.VillageFacilities ??= new System.Collections.Generic.List<
                VillageFacilityState>();
            world.VillageLedgerEntries ??= new System.Collections.Generic.List<
                VillageLedgerEntryState>();

            for (var personIndex = 0;
                 personIndex < world.People.Count;
                 personIndex++)
            {
                var person = world.People[personIndex];
                if (string.IsNullOrEmpty(person.BirthLocationId))
                {
                    person.BirthLocationId = person.PopulationOriginLocationId;
                    if (string.IsNullOrEmpty(person.BirthLocationId))
                    {
                        person.BirthLocationId = person.LocationId;
                    }
                }

                person.FamilyId = string.Empty;
                person.NextIndependentEventDay = -1;
                person.NextIndependentEventReason = string.Empty;
                person.LocalDuty = LocalDutyKind.None;
                person.LocalDutyUntilDay = -1;
            }

            for (var familyIndex = 0;
                 familyIndex < world.Families.Count;
                 familyIndex++)
            {
                var family = world.Families[familyIndex];
                var head = FindPerson(world, family.HeadPersonId);
                family.LocationId = head.LocationId;
                family.VillageId = string.Empty;
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    FindPerson(world, family.MemberIds[memberIndex]).FamilyId =
                        family.Id;
                }
            }

            world.SchemaVersion = 6;
        }

        private static void MigrateVersionSixToSeven(WorldState world)
        {
            world.PopulationStorage =
                PopulationStorageState.CreateInline(world.People);
            world.SchemaVersion = 7;
        }

        private static void MigrateVersionSevenToEight(WorldState world)
        {
            world.AgricultureWorkOrders ??=
                new System.Collections.Generic.List<AgricultureWorkOrderState>();
            world.ProductionLedgerEntries ??=
                new System.Collections.Generic.List<ProductionLedgerEntryState>();
            world.ProductionContentManifest =
                ProductionContentRegistry.CreateCore().CreateManifest();
            world.SchemaVersion = 8;
        }

        private static void MigrateVersionEightToNine(WorldState world)
        {
            world.ResearchProjects ??=
                new System.Collections.Generic.List<ResearchProjectState>();
            world.TechnologyApplications ??=
                new System.Collections.Generic.List<TechnologyApplicationState>();
            world.ResearchLedgerEntries ??=
                new System.Collections.Generic.List<ResearchLedgerEntryState>();
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                person.SkillMasteries ??=
                    new System.Collections.Generic.List<SkillMasteryState>();
                person.KnowledgeMasteries ??=
                    new System.Collections.Generic.List<KnowledgeMasteryState>();
                person.TechnologyMasteries ??=
                    new System.Collections.Generic.List<TechnologyMasteryState>();
            }

            world.ProductionContentManifest =
                ProductionContentRegistry.CreateCore().CreateManifest();
            world.SchemaVersion = 9;
        }

        private static void MigrateVersionNineToTen(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.ProductBatches ??=
                new System.Collections.Generic.List<ProductBatchState>();
            world.InventoryTransactions ??=
                new System.Collections.Generic.List<InventoryTransactionState>();
            world.ProcessingWorkOrders ??=
                new System.Collections.Generic.List<ProcessingWorkOrderState>();
            world.ProductionContentManifest =
                productionContent.CreateManifest();
            world.SchemaVersion = 10;
        }

        private static PersonState FindPerson(WorldState world, string personId)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == personId)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing person {personId} during world migration.");
        }
    }
}
