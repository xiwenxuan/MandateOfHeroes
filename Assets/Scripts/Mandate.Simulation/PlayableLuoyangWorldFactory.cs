using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public static class PlayableLuoyangWorldContractIds
    {
        public const string ContractId =
            "playable_luoyang_direct_game_world_v1";
        public const string PlayerPersonId = "person.player.luoyang";
        public const string PlayerFamilyId = "family.player.luoyang";
        public const string MerchantOrganizationId =
            "organization.luoyang.market_merchants";
        public const string MerchantPositionId =
            "position.luoyang_trader";
        public const string LocalTaskDefinitionId =
            "task_definition.luoyang.market_registers";
        public const string StartingFacilityId =
            "facility.instance.luoyang.v1.recommended.000149";
        public const string MarketFacilityId =
            "facility.instance.luoyang.v1.recommended.000325";
        public const string OfficeFacilityId = MarketFacilityId;
    }

    public static class PlayableLuoyangWorldFactory
    {
        public static WorldState Create(
            LuoyangHumanScaleLocalMapPlan localMap,
            ulong masterSeed = 184_001UL,
            string playerDisplayName = "沈衡") =>
            Create(localMap, null, masterSeed, playerDisplayName);

        public static WorldState Create(
            LuoyangHumanScaleLocalMapPlan localMap,
            LuoyangBuildingPerformancePlan performancePlan,
            ulong masterSeed = 184_001UL,
            string playerDisplayName = "沈衡")
        {
            if (localMap == null)
                throw new ArgumentNullException(nameof(localMap));
            if (localMap.FacilityCapabilities == null ||
                localMap.FacilityCapabilities.Count == 0)
                throw new ArgumentException(
                    "洛阳人物尺度地图没有正式设施。", nameof(localMap));
            var normalizedName = (playerDisplayName ?? string.Empty).Trim();
            if (normalizedName.Length == 0 || normalizedName.Length > 16)
                throw new ArgumentException(
                    "玩家姓名必须为1至16个字符。",
                    nameof(playerDisplayName));

            var duplicateFacility = localMap.FacilityCapabilities
                .GroupBy(item => item.FacilityId, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateFacility != null)
                throw new InvalidOperationException(
                    $"洛阳人物尺度地图包含重复设施 {duplicateFacility.Key}。");

            var world = WorldState.Create(masterSeed);
            world.Locations.Add(new LocationState
            {
                Id = LuoyangHumanScaleLocalMapIds.SettlementLocationId,
                DisplayName = "洛阳",
                Kind = LocationKind.RegionalSeat,
                Terrain = TerrainKind.Plains,
                Features = LocationFeature.Government |
                    LocationFeature.Market |
                    LocationFeature.Garrison |
                    LocationFeature.Workshop |
                    LocationFeature.Clinic |
                    LocationFeature.Temple |
                    LocationFeature.Fortification,
                StrategicImportance = 5,
                Population = 1,
                PublicOrderBasisPoints = 6_200,
                GrainPrice = 100,
                MapXBasisPoints = 5_250,
                MapYBasisPoints = 4_750
            });

            var player = new PersonState
            {
                Id = PlayableLuoyangWorldContractIds.PlayerPersonId,
                DisplayName = normalizedName,
                LocationId = LuoyangHumanScaleLocalMapIds
                    .SettlementLocationId,
                BirthLocationId = LuoyangHumanScaleLocalMapIds
                    .SettlementLocationId,
                BirthDay = -24L * 360L,
                Gender = PersonGender.Male,
                IsAlive = true,
                HealthBasisPoints = 10_000,
                StaminaBasisPoints = 10_000,
                Wealth = 200,
                Provisions = 20,
                CargoCapacity = 30,
                CountsTowardPopulation = true
            };
            CharacterAbilityBootstrap.InitializePerson(
                masterSeed, player, CharacterBackgroundKind.Merchant);
            world.People.Add(player);
            world.PlayerPersonId = player.Id;
            InitializeCityLife(world, player);

            var presentationByFacilityId = performancePlan?.Facilities?
                .ToDictionary(item => item.FacilityId,
                    StringComparer.Ordinal) ??
                new Dictionary<string, LuoyangBuildingPerformanceFacility>(
                    StringComparer.Ordinal);

            var orderedCapabilities = localMap.FacilityCapabilities
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                .ToArray();
            for (var index = 0; index < orderedCapabilities.Length; index++)
            {
                var capability = orderedCapabilities[index] ??
                    throw new InvalidOperationException(
                        "洛阳人物尺度地图包含空设施能力记录。");
                if (!string.Equals(capability.SettlementLocationId,
                        LuoyangHumanScaleLocalMapIds.SettlementLocationId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"设施 {capability.FacilityId} 不属于洛阳。");
                presentationByFacilityId.TryGetValue(capability.FacilityId,
                    out var presentation);
                world.Facilities.Add(new FacilityState
                {
                    Id = capability.FacilityId,
                    DisplayName = string.IsNullOrWhiteSpace(
                            presentation?.DisplayName)
                        ? BuildFacilityDisplayName(capability, index)
                        : presentation.DisplayName,
                    DefinitionId = capability.FacilityDefinitionId,
                    CellId64 = capability.CellId64,
                    SettlementId = capability.SettlementLocationId,
                    HistoricalConfidence = ResolveHistoricalConfidence(
                        presentation?.HistoricalConfidenceId),
                    SpatialPrecision = ResolveSpatialPrecision(
                        presentation?.SpatialPrecisionId),
                    SourceNote =
                        PlayableLuoyangWorldContractIds.ContractId,
                    LifecycleStatus = FacilityLifecycleStatus.Operational,
                    ConditionBasisPoints = 10_000
                });
            }

            world.PopulationStorage.SynchronizeInlineCounts(world.People);
            world.Validate();
            return world;
        }

        private static void InitializeCityLife(WorldState world,
            PersonState player)
        {
            var settlementId = LuoyangHumanScaleLocalMapIds
                .SettlementLocationId;
            world.Families.Add(new FamilyState
            {
                Id = PlayableLuoyangWorldContractIds.PlayerFamilyId,
                DisplayName = player.DisplayName + "之家",
                HeadPersonId = player.Id,
                LocationId = settlementId,
                Wealth = 600,
                Debt = 120,
                Grain = 20,
                MemberIds = { player.Id }
            });
            player.FamilyId = PlayableLuoyangWorldContractIds.PlayerFamilyId;

            world.Organizations.Add(new OrganizationState
            {
                Id = PlayableLuoyangWorldContractIds.MerchantOrganizationId,
                DisplayName = "洛阳市商",
                Type = OrganizationType.Merchant,
                HeadquartersLocationId = settlementId,
                LeaderPersonId = player.Id,
                Treasury = 2_000
            });
            world.Positions.Add(new PositionState
            {
                Id = PlayableLuoyangWorldContractIds.MerchantPositionId,
                OrganizationId =
                    PlayableLuoyangWorldContractIds.MerchantOrganizationId,
                DisplayName = "行商",
                Rank = 0,
                Capacity = 20
            });
            world.Memberships.Add(new MembershipState
            {
                Id = "membership.person.player.luoyang.merchant",
                PersonId = player.Id,
                OrganizationId =
                    PlayableLuoyangWorldContractIds.MerchantOrganizationId,
                PositionId =
                    PlayableLuoyangWorldContractIds.MerchantPositionId,
                JoinedDay = world.AbsoluteDay,
                LoyaltyBasisPoints = 5_500
            });

            world.Commodities.Add(new CommodityState
            {
                Id = "commodity.cloth",
                DisplayName = "布帛",
                BasePrice = 80,
                UnitWeight = 5
            });
            world.MarketListings.Add(new MarketListingState
            {
                Id = "market.luoyang.commodity.cloth",
                LocationId = settlementId,
                CommodityId = "commodity.cloth",
                Price = 80,
                EquilibriumPrice = 80,
                Stock = 220,
                TargetStock = 220
            });
            world.TaskDefinitions.Add(new TaskDefinitionState
            {
                Id = PlayableLuoyangWorldContractIds.LocalTaskDefinitionId,
                DisplayName = "协助市曹核验商籍",
                Kind = TaskKind.LocalWork,
                IssuerOrganizationId =
                    PlayableLuoyangWorldContractIds.MerchantOrganizationId,
                RequiredPositionId =
                    PlayableLuoyangWorldContractIds.MerchantPositionId,
                OriginLocationId = settlementId,
                RequiredProgress = 3,
                DurationDays = 10,
                RewardMoney = 120,
                RewardProvisions = 2,
                RequiresMembership = true,
                IsAvailable = true
            });
        }


        private static HistoricalConfidenceLevel ResolveHistoricalConfidence(
            string value)
        {
            return Enum.TryParse(value, true,
                out HistoricalConfidenceLevel parsed)
                ? parsed
                : HistoricalConfidenceLevel.GameplayReconstruction;
        }

        private static HistoricalSpatialPrecision ResolveSpatialPrecision(
            string value)
        {
            return Enum.TryParse(value, true,
                out HistoricalSpatialPrecision parsed)
                ? parsed
                : HistoricalSpatialPrecision.Approximate;
        }

        private static string BuildFacilityDisplayName(
            LuoyangFacilitySpatialCapability capability, int index)
        {
            if (capability.FacilityId.IndexOf("gate",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return "洛阳城门";
            if (capability.FacilityId.IndexOf("bridge",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return "洛阳桥梁";
            if (capability.FacilityId.IndexOf("market",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return "洛阳市集";
            if (capability.FacilityId.IndexOf("palace",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return "洛阳宫署";
            return $"洛阳设施 {index + 1:0000}";
        }
    }
}
