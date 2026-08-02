using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class LivestockProductionSystem
    {
        public const string PrototypePastureSiteId =
            "production_site.zhongshan_merchants.sheep_pasture";
        public const string PrototypeSlaughterYardSiteId =
            "production_site.zhongshan_merchants.slaughter_yard";
        public const string PrototypeTannerySiteId =
            "production_site.zhongshan_merchants.tannery";
        public const string PrototypeHornWorkshopSiteId =
            "production_site.zhongshan_merchants.horn_workshop";
        public const string PrototypeOpeningFlockBatchId =
            "product_batch.prototype_livestock.sheep";

        private const string MerchantOrganizationId =
            "organization.zhongshan_merchants";
        private const string ZhongshanLocationId = "location.zhongshan";
        private readonly ProcessingProductionSystem _processing;

        public LivestockProductionSystem(
            ProductionContentRegistry content = null)
        {
            _processing = new ProcessingProductionSystem(content);
        }

        public static void InitializePrototype(WorldState world)
        {
            ProductInventorySystem.RequireWorld(world);
            if (world.ProductionSites.Exists(item =>
                    item.Id == PrototypePastureSiteId))
            {
                return;
            }

            _ = ProcessingProductionSystem.FindContainer(
                world,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId);
            AddSite(
                world,
                PrototypePastureSiteId,
                "production_site_kind.sheep_pasture",
                "person.zhang_shiping",
                CoreProductionContent.PastureFacilityTag);
            AddSite(
                world,
                PrototypeSlaughterYardSiteId,
                "production_site_kind.slaughter_yard",
                "person.su_shuang",
                CoreProductionContent.SlaughterYardFacilityTag);
            AddSite(
                world,
                PrototypeTannerySiteId,
                "production_site_kind.tannery",
                "person.zhang_shiping",
                CoreProductionContent.TanneryFacilityTag);
            AddSite(
                world,
                PrototypeHornWorkshopSiteId,
                "production_site_kind.horn_workshop",
                "person.su_shuang",
                CoreProductionContent.HornWorkshopFacilityTag);
            AddOpeningFlock(world, 30);
        }

        public ProcessingWorkOrderState CreateHusbandryOrder(
            WorldState world,
            string managerPersonId,
            ProductionControlMode controlMode,
            int runCount)
        {
            return Create(
                world,
                CoreProductionContent.BreedSheepRecipeId,
                CoreProductionContent.PastureBreedingMethodId,
                PrototypePastureSiteId,
                managerPersonId,
                controlMode,
                runCount);
        }

        public ProcessingWorkOrderState CreateSlaughterOrder(
            WorldState world,
            string managerPersonId,
            ProductionControlMode controlMode,
            int runCount)
        {
            return Create(
                world,
                CoreProductionContent.SlaughterSheepRecipeId,
                CoreProductionContent.ManualSlaughterMethodId,
                PrototypeSlaughterYardSiteId,
                managerPersonId,
                controlMode,
                runCount);
        }

        public ProcessingWorkOrderState CreateTanningOrder(
            WorldState world,
            string managerPersonId,
            ProductionControlMode controlMode,
            int runCount)
        {
            return Create(
                world,
                CoreProductionContent.VegetableTanHideRecipeId,
                CoreProductionContent.VegetableTanningMethodId,
                PrototypeTannerySiteId,
                managerPersonId,
                controlMode,
                runCount);
        }

        public ProcessingWorkOrderState CreateHornFinishingOrder(
            WorldState world,
            string managerPersonId,
            ProductionControlMode controlMode,
            int runCount)
        {
            return Create(
                world,
                CoreProductionContent.FinishHornRecipeId,
                CoreProductionContent.HornFinishingMethodId,
                PrototypeHornWorkshopSiteId,
                managerPersonId,
                controlMode,
                runCount);
        }

        private ProcessingWorkOrderState Create(
            WorldState world,
            string recipeId,
            string methodId,
            string siteId,
            string managerPersonId,
            ProductionControlMode controlMode,
            int runCount)
        {
            return _processing.CreateOrganizationOrder(
                world,
                recipeId,
                methodId,
                MerchantOrganizationId,
                siteId,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                managerPersonId,
                controlMode,
                runCount);
        }

        private static void AddOpeningFlock(WorldState world, long quantity)
        {
            var content = ProductionContentRegistry.CreateCore();
            var product = content.GetProduct(
                CoreProductionContent.LiveSheepProductId);
            var container = ProcessingProductionSystem.FindContainer(
                world,
                MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId);
            const string transactionId =
                "inventory_transaction.prototype_livestock.sheep";
            var batch = ProductInventorySystem.NewOrganizationBatch(
                world,
                product,
                container,
                transactionId,
                string.Empty,
                quantity,
                8_000);
            batch.Id = PrototypeOpeningFlockBatchId;
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = transactionId,
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.OpeningBalance,
                ActorPersonId = "person.zhang_shiping",
                Summary = "Prototype merchant opening breeding flock.",
                Lines =
                {
                    ProductInventorySystem.Line(batch, quantity, 0)
                }
            });
        }

        private static void AddSite(
            WorldState world,
            string id,
            string kindId,
            string managerPersonId,
            string facilityTag)
        {
            world.ProductionSites.Add(new ProductionSiteState
            {
                Id = id,
                KindId = kindId,
                OwnerOrganizationId = MerchantOrganizationId,
                LocationId = ZhongshanLocationId,
                ManagerPersonId = managerPersonId,
                InventoryContainerId =
                    MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                ConcurrentOrderCapacity = 1,
                ConditionBasisPoints = 8_000,
                FacilityTags = new List<string> { facilityTag }
            });
        }
    }
}
