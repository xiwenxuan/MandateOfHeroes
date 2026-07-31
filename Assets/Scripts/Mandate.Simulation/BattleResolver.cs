using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class BattleOutcome
    {
        public BattleRecordState Record { get; }
        public string Summary => Record.Summary;

        public BattleOutcome(BattleRecordState record)
        {
            Record = record;
        }
    }

    public sealed class BattleResolver
    {
        private readonly NamedRandom _random;

        public BattleResolver(ulong masterSeed)
        {
            _random = new NamedRandom(masterSeed);
        }

        public BattleOutcome Resolve(
            WorldState world,
            StableId issuerPersonId,
            StableId attackerArmyId,
            StableId defenderArmyId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var attacker = FindArmy(world, attackerArmyId.Value);
            var defender = FindArmy(world, defenderArmyId.Value);
            var order = new MilitaryAuthoritySystem().IssueOrder(
                world,
                issuerPersonId,
                attackerArmyId,
                MilitaryOrderType.Engage,
                MilitaryAuthorityLevel.Army,
                targetArmyId: defenderArmyId.Value);
            if (order.Result == MilitaryOrderResult.Rejected)
            {
                throw new InvalidOperationException(order.Summary);
            }

            ValidateBattle(world, attacker, defender);

            var attackerInitial = attacker.Troops;
            var defenderInitial = defender.Troops;
            var sequence = world.Battles.Count;
            var attackerPower = CalculatePower(attacker, sequence, "attacker");
            var defenderPower = CalculatePower(defender, sequence, "defender");
            var largestPower = Math.Max(attackerPower, defenderPower);
            var closeBattle = Math.Abs(attackerPower - defenderPower) <= largestPower / 20;

            BattleResultType result;
            string winnerArmyId;
            int attackerRate;
            int defenderRate;
            if (closeBattle)
            {
                result = BattleResultType.Stalemate;
                winnerArmyId = string.Empty;
                attackerRate = RollCasualtyRate(attacker, sequence, "stalemate", 1_000, 2_001);
                defenderRate = RollCasualtyRate(defender, sequence, "stalemate", 1_000, 2_001);
                attacker.MoraleBasisPoints = Math.Max(
                    0, attacker.MoraleBasisPoints - 200);
                defender.MoraleBasisPoints = Math.Max(
                    0, defender.MoraleBasisPoints - 200);
            }
            else if (attackerPower > defenderPower)
            {
                result = BattleResultType.AttackerVictory;
                winnerArmyId = attacker.Id;
                attackerRate = RollCasualtyRate(attacker, sequence, "victory", 500, 1_201);
                defenderRate = RollCasualtyRate(defender, sequence, "defeat", 1_500, 3_001);
                ApplyMoraleResult(attacker, defender);
            }
            else
            {
                result = BattleResultType.DefenderVictory;
                winnerArmyId = defender.Id;
                attackerRate = RollCasualtyRate(attacker, sequence, "defeat", 1_500, 3_001);
                defenderRate = RollCasualtyRate(defender, sequence, "victory", 500, 1_201);
                ApplyMoraleResult(defender, attacker);
            }

            var attackerCasualties = CalculateCasualties(attacker.Troops, attackerRate);
            var defenderCasualties = CalculateCasualties(defender.Troops, defenderRate);
            var attackerWounded = CalculateWounded(
                attacker, attackerCasualties, sequence);
            var defenderWounded = CalculateWounded(
                defender, defenderCasualties, sequence);
            var militaryService = new MilitaryServiceSystem();
            militaryService.ApplyCasualties(
                world,
                attackerArmyId,
                attackerCasualties,
                attackerWounded,
                sequence);
            militaryService.ApplyCasualties(
                world,
                defenderArmyId,
                defenderCasualties,
                defenderWounded,
                sequence);
            UpdateMobilization(attacker);
            UpdateMobilization(defender);
            ReduceLocalOrder(
                world,
                attacker.LocationId,
                attackerCasualties + defenderCasualties);

            var record = new BattleRecordState
            {
                Id = $"battle.{world.AbsoluteDay}.{attacker.Id}.{defender.Id}.{sequence}",
                Day = world.AbsoluteDay,
                LocationId = attacker.LocationId,
                AttackerArmyId = attacker.Id,
                DefenderArmyId = defender.Id,
                AttackerInitialTroops = attackerInitial,
                DefenderInitialTroops = defenderInitial,
                AttackerCasualties = attackerCasualties,
                DefenderCasualties = defenderCasualties,
                AttackerWounded = attackerWounded,
                DefenderWounded = defenderWounded,
                Result = result,
                WinnerArmyId = winnerArmyId,
                Summary =
                    $"{attacker.DisplayName}与{defender.DisplayName}交战：" +
                    $"{result}；双方减员{attackerCasualties}/{defenderCasualties}人，" +
                    $"其中伤兵{attackerWounded}/{defenderWounded}人。"
            };
            world.Battles.Add(record);
            world.Validate();
            return new BattleOutcome(record);
        }

        private long CalculatePower(
            ArmyState army,
            long sequence,
            string role)
        {
            var basePower =
                (long)army.Troops *
                (5_000 + army.MoraleBasisPoints) *
                (5_000 + army.TrainingBasisPoints) /
                100_000_000L;
            var variation = _random.Range(
                "battle",
                new StableId(army.Id),
                sequence,
                role + "_power",
                9_000,
                11_001);
            return basePower * variation / 10_000;
        }

        private int RollCasualtyRate(
            ArmyState army,
            long sequence,
            string purpose,
            int minimum,
            int maximum)
        {
            return _random.Range(
                "battle",
                new StableId(army.Id),
                sequence,
                purpose + "_casualties",
                minimum,
                maximum);
        }

        private static int CalculateCasualties(int troops, int rate)
        {
            return Math.Min(troops, Math.Max(1, troops * rate / 10_000));
        }

        private int CalculateWounded(
            ArmyState army,
            int casualties,
            long sequence)
        {
            var woundedShare = _random.Range(
                "battle",
                new StableId(army.Id),
                sequence,
                "wounded_share",
                5_500,
                7_501);
            return casualties * woundedShare / 10_000;
        }

        private static void ApplyMoraleResult(ArmyState winner, ArmyState loser)
        {
            winner.MoraleBasisPoints = Math.Min(
                10_000, winner.MoraleBasisPoints + 400);
            loser.MoraleBasisPoints = Math.Max(
                0, loser.MoraleBasisPoints - 800);
        }

        private static void UpdateMobilization(ArmyState army)
        {
            if (army.Troops <= army.MaximumTroops / 10)
            {
                army.IsMobilized = false;
            }
        }

        private static void ReduceLocalOrder(
            WorldState world,
            string locationId,
            int casualties)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                var location = world.Locations[i];
                if (location.Id == locationId)
                {
                    location.PublicOrderBasisPoints = Math.Max(
                        0,
                        location.PublicOrderBasisPoints -
                        Math.Min(2_000, casualties / 5));
                    return;
                }
            }
        }

        private static void ValidateBattle(
            WorldState world,
            ArmyState attacker,
            ArmyState defender)
        {
            if (attacker.Id == defender.Id ||
                attacker.OrganizationId == defender.OrganizationId)
            {
                throw new InvalidOperationException(
                    "A battle requires armies from opposing organizations.");
            }

            if (!attacker.IsMobilized ||
                !defender.IsMobilized ||
                attacker.Troops <= 0 ||
                defender.Troops <= 0)
            {
                throw new InvalidOperationException(
                    "Both armies must be mobilized and have troops.");
            }

            if (attacker.LocationId != defender.LocationId)
            {
                throw new InvalidOperationException(
                    "Both armies must be at the same location.");
            }

            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].ArmyId == attacker.Id ||
                    world.ArmyMarches[i].ArmyId == defender.Id)
                {
                    throw new InvalidOperationException(
                        "Marching armies cannot resolve a field battle.");
                }
            }
        }

        private static ArmyState FindArmy(WorldState world, string armyId)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == armyId)
                {
                    return world.Armies[i];
                }
            }

            throw new InvalidOperationException($"Missing army {armyId}.");
        }
    }
}
