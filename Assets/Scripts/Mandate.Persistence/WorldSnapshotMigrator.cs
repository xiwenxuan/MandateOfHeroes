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
                    case 10:
                        MigrateVersionTenToEleven(world);
                        break;
                    case 11:
                        MigrateVersionElevenToTwelve(world);
                        break;
                    case 12:
                        MigrateVersionTwelveToThirteen(world);
                        break;
                    case 13:
                        MigrateVersionThirteenToFourteen(world, content);
                        break;
                    case 14:
                        MigrateVersionFourteenToFifteen(world, content);
                        break;
                    case 15:
                        MigrateVersionFifteenToSixteen(world, content);
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

        private static void MigrateVersionTenToEleven(WorldState world)
        {
            world.AttentionFocuses ??=
                new System.Collections.Generic.List<AttentionFocusState>();
            world.AttentionLedgerEntries ??=
                new System.Collections.Generic.List<AttentionLedgerEntryState>();
            world.SchemaVersion = 11;
        }

        private static void MigrateVersionElevenToTwelve(WorldState world)
        {
            world.CountyGovernances ??=
                new System.Collections.Generic.List<CountyGovernanceState>();
            world.CountyGentryHouses ??=
                new System.Collections.Generic.List<CountyGentryHouseState>();
            world.CountyHouseholdTaxes ??=
                new System.Collections.Generic.List<CountyHouseholdTaxState>();
            world.CountyFiscalLedgerEntries ??=
                new System.Collections.Generic.List<CountyFiscalLedgerEntryState>();
            world.SchemaVersion = 12;
        }

        private static void MigrateVersionTwelveToThirteen(WorldState world)
        {
            world.MilitaryEquipmentInitialized = false;
            world.MilitaryEquipmentDefinitions =
                new System.Collections.Generic.List<
                    MilitaryEquipmentDefinitionState>();
            world.MilitaryArmoryStocks =
                new System.Collections.Generic.List<MilitaryArmoryStockState>();
            world.MilitaryEquipmentIssues =
                new System.Collections.Generic.List<MilitaryEquipmentIssueState>();
            world.MilitaryEquipmentTransactions =
                new System.Collections.Generic.List<
                    MilitaryEquipmentTransactionState>();
            world.SchemaVersion = 13;
        }

        private static void MigrateVersionThirteenToFourteen(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.InventoryContainers =
                new System.Collections.Generic.List<InventoryContainerState>();
            world.MilitaryProcurementOrders =
                new System.Collections.Generic.List<
                    MilitaryProcurementOrderState>();
            world.MilitaryProcurementLedgerEntries =
                new System.Collections.Generic.List<
                    MilitaryProcurementLedgerEntryState>();

            world.ProductBatches.RemoveAll(batch =>
                !string.IsNullOrEmpty(batch.OwnerOrganizationId) ||
                !string.IsNullOrEmpty(batch.InventoryContainerId));
            world.InventoryTransactions.RemoveAll(transaction =>
                !string.IsNullOrEmpty(
                    transaction.SourceMilitaryProcurementId) ||
                transaction.Type == InventoryTransactionType.OpeningBalance ||
                transaction.Type ==
                    InventoryTransactionType.MilitaryProcurementDispatched);

            for (var i = 0;
                 i < world.MilitaryEquipmentDefinitions.Count;
                 i++)
            {
                var definition = world.MilitaryEquipmentDefinitions[i];
                definition.ProductDefinitionId =
                    ProductForEquipment(definition.Id);
            }

            world.ProductionContentManifest = productionContent.CreateManifest();
            world.SchemaVersion = 14;
        }

        private static string ProductForEquipment(string equipmentId)
        {
            switch (equipmentId)
            {
                case "equipment.han_ring_sword":
                    return CoreProductionContent.RingSwordProductId;
                case "equipment.wooden_shield":
                    return CoreProductionContent.WoodenShieldProductId;
                case "equipment.long_spear":
                    return CoreProductionContent.LongSpearProductId;
                case "equipment.horn_bow":
                    return CoreProductionContent.HornBowProductId;
                case "equipment.arrow_bundle":
                    return CoreProductionContent.ArrowBundleProductId;
                case "equipment.lamellar_armor":
                    return CoreProductionContent.LamellarArmorProductId;
                default:
                    return "product.equipment." +
                           equipmentId.Substring(equipmentId.IndexOf('.') + 1);
            }
        }

        private static void MigrateVersionFourteenToFifteen(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.ProductionSites =
                new System.Collections.Generic.List<ProductionSiteState>();
            world.MilitaryEquipmentRepairOrders =
                new System.Collections.Generic.List<
                    MilitaryEquipmentRepairOrderState>();
            for (var i = 0;
                 i < world.MilitaryEquipmentDefinitions.Count;
                 i++)
            {
                ConfigureRepair(world.MilitaryEquipmentDefinitions[i]);
            }

            for (var i = 0; i < world.MilitaryArmoryStocks.Count; i++)
            {
                world.MilitaryArmoryStocks[i].ReservedDamagedQuantity = 0;
            }

            world.ProductionContentManifest = productionContent.CreateManifest();
            world.SchemaVersion = 15;
        }

        private static void ConfigureRepair(
            MilitaryEquipmentDefinitionState definition)
        {
            switch (definition.Id)
            {
                case "equipment.han_ring_sword":
                    SetRepair(
                        definition,
                        CoreProductionContent.IronMaterialProductId,
                        1,
                        3,
                        CoreProductionContent.BlacksmithFacilityTag);
                    break;
                case "equipment.wooden_shield":
                    SetRepair(
                        definition,
                        CoreProductionContent.TimberMaterialProductId,
                        2,
                        2,
                        CoreProductionContent.WoodworkingFacilityTag);
                    break;
                case "equipment.long_spear":
                    SetRepair(
                        definition,
                        CoreProductionContent.IronMaterialProductId,
                        1,
                        2,
                        CoreProductionContent.BlacksmithFacilityTag);
                    break;
                case "equipment.horn_bow":
                    SetRepair(
                        definition,
                        CoreProductionContent.HornMaterialProductId,
                        1,
                        3,
                        CoreProductionContent.BowmakingFacilityTag);
                    break;
                case "equipment.arrow_bundle":
                    SetRepair(
                        definition,
                        CoreProductionContent.TimberMaterialProductId,
                        1,
                        1,
                        CoreProductionContent.WoodworkingFacilityTag);
                    break;
                case "equipment.lamellar_armor":
                    SetRepair(
                        definition,
                        CoreProductionContent.IronMaterialProductId,
                        2,
                        5,
                        CoreProductionContent.ArmoringFacilityTag);
                    break;
                default:
                    return;
            }
        }

        private static void MigrateVersionFifteenToSixteen(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.ResourceBodies =
                new System.Collections.Generic.List<ResourceBodyState>();
            world.ResourceExtractionOrders =
                new System.Collections.Generic.List<
                    ResourceExtractionOrderState>();
            world.ResourceExtractionLedgerEntries =
                new System.Collections.Generic.List<
                    ResourceExtractionLedgerEntryState>();
            world.ProductionContentManifest = productionContent.CreateManifest();
            world.SchemaVersion = 16;
        }

        private static void SetRepair(
            MilitaryEquipmentDefinitionState definition,
            string productId,
            int quantity,
            int durationDays,
            string facilityTag)
        {
            definition.RepairMaterialProductDefinitionId = productId;
            definition.RepairMaterialQuantityPerUnit = quantity;
            definition.RepairDurationDays = durationDays;
            definition.RepairFacilityTag = facilityTag;
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
