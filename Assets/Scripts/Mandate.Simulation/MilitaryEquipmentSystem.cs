using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class TroopDerivationResult
    {
        public string TroopTypeId;
        public bool CountsTowardCombatReadiness;
        public bool MeetsMinimumEquipment;
    }

    public sealed class MilitaryEquipmentReadinessReport
    {
        public string ArmyId;
        public string FormationId;
        public int CombatMembers;
        public int ReadyMembers;
        public int ReadinessBasisPoints;
        public readonly Dictionary<string, int> TroopCounts =
            new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class MilitaryEquipmentAudit
    {
        public int Opening;
        public int Available;
        public int Damaged;
        public int Issued;
        public int Lost;
        public int CapturedIn;
        public int CapturedOut;
        public int TransferredIn;
        public int TransferredOut;

        public int CurrentTracked => Available + Damaged + Issued;
        public int ExpectedTracked =>
            Opening + CapturedIn - CapturedOut +
            TransferredIn - TransferredOut - Lost;
        public bool IsBalanced => CurrentTracked == ExpectedTracked;
    }

    public sealed class MilitaryEquipmentSystem
    {
        public const string RingSwordId = "equipment.han_ring_sword";
        public const string WoodenShieldId = "equipment.wooden_shield";
        public const string LongSpearId = "equipment.long_spear";
        public const string HornBowId = "equipment.horn_bow";
        public const string ArrowBundleId = "equipment.arrow_bundle";
        public const string LamellarArmorId = "equipment.lamellar_armor";

        public const string UnarmedTroopId = "troop.unarmed";
        public const string SwordShieldTroopId = "troop.sword_shield";
        public const string SpearTroopId = "troop.spearman";
        public const string ArcherTroopId = "troop.archer";
        public const string LightInfantryTroopId = "troop.light_infantry";
        public const string CommandTroopId = "troop.command";
        public const string MedicTroopId = "troop.support.medic";
        public const string QuartermasterTroopId =
            "troop.support.quartermaster";
        public const string MessengerTroopId = "troop.support.messenger";

        private readonly IPersonRepository _people;
        private WorldState _fallbackWorld;
        private IPersonRepository _fallbackPeople;

        public MilitaryEquipmentSystem(IPersonRepository people = null)
        {
            _people = people;
        }

        public void InitializePrototype(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            if (!world.MilitaryServiceInitialized)
            {
                throw new InvalidOperationException(
                    "Military service must be initialized before equipment.");
            }

            if (world.MilitaryEquipmentInitialized)
            {
                return;
            }

            AddCoreDefinitions(world);
            var armies = new List<ArmyState>(world.Armies);
            armies.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < armies.Count; i++)
            {
                AddOpeningStocks(world, armies[i]);
                IssueArmyEquipment(world, armies[i]);
            }

            world.MilitaryEquipmentInitialized = true;
            world.Validate();
        }

        public TroopDerivationResult DeriveTroop(
            WorldState world,
            MilitaryServiceState service)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            switch (service.Role)
            {
                case MilitaryServiceRole.Commander:
                case MilitaryServiceRole.Officer:
                    return SupportResult(CommandTroopId);
                case MilitaryServiceRole.Medic:
                    return SupportResult(MedicTroopId);
                case MilitaryServiceRole.Quartermaster:
                    return SupportResult(QuartermasterTroopId);
                case MilitaryServiceRole.Messenger:
                    return SupportResult(MessengerTroopId);
            }

            var result = new TroopDerivationResult
            {
                CountsTowardCombatReadiness = true,
                TroopTypeId = UnarmedTroopId
            };
            var person = PeopleFor(world).GetRequired(service.PersonId);
            var hasSword = HasIssue(world, service.Id, RingSwordId);
            var hasShield = HasIssue(world, service.Id, WoodenShieldId);
            var hasSpear = HasIssue(world, service.Id, LongSpearId);
            var hasBow = HasIssue(world, service.Id, HornBowId);
            var hasArrows = HasIssue(world, service.Id, ArrowBundleId);
            var strengthFitness =
                (person.Aptitudes.Strength + person.Aptitudes.Constitution) / 2;
            var archeryFitness =
                (person.Aptitudes.Dexterity + person.Aptitudes.Perception) / 2;
            if (hasBow && hasArrows && archeryFitness >= 4_500)
            {
                result.TroopTypeId = ArcherTroopId;
                result.MeetsMinimumEquipment = true;
            }
            else if (hasSpear && strengthFitness >= 4_500)
            {
                result.TroopTypeId = SpearTroopId;
                result.MeetsMinimumEquipment = true;
            }
            else if (hasSword && hasShield && strengthFitness >= 4_000)
            {
                result.TroopTypeId = SwordShieldTroopId;
                result.MeetsMinimumEquipment = true;
            }
            else if (hasSword || hasSpear || hasBow)
            {
                result.TroopTypeId = LightInfantryTroopId;
            }

            return result;
        }

        public MilitaryEquipmentReadinessReport BuildReadinessReport(
            WorldState world,
            string armyId,
            string formationId = "")
        {
            var result = new MilitaryEquipmentReadinessReport
            {
                ArmyId = armyId,
                FormationId = formationId
            };
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.ArmyId != armyId ||
                    !string.IsNullOrEmpty(formationId) &&
                    service.FormationId != formationId ||
                    service.Status != MilitaryServiceStatus.Active &&
                    service.Status != MilitaryServiceStatus.Mustering)
                {
                    continue;
                }

                var troop = DeriveTroop(world, service);
                AddCount(result.TroopCounts, troop.TroopTypeId);
                if (!troop.CountsTowardCombatReadiness)
                {
                    continue;
                }

                result.CombatMembers++;
                if (troop.MeetsMinimumEquipment)
                {
                    result.ReadyMembers++;
                }
            }

            result.ReadinessBasisPoints = result.CombatMembers == 0
                ? 10_000
                : result.ReadyMembers * 10_000 / result.CombatMembers;
            return result;
        }

        public MilitaryEquipmentAudit AuditArmy(
            WorldState world,
            string armyId)
        {
            var audit = new MilitaryEquipmentAudit();
            for (var i = 0; i < world.MilitaryArmoryStocks.Count; i++)
            {
                var stock = world.MilitaryArmoryStocks[i];
                if (stock.ArmyId == armyId)
                {
                    audit.Opening += stock.OpeningQuantity;
                    audit.Available += stock.AvailableQuantity;
                    audit.Damaged += stock.DamagedQuantity;
                }
            }

            for (var i = 0; i < world.MilitaryEquipmentIssues.Count; i++)
            {
                if (world.MilitaryEquipmentIssues[i].ArmyId == armyId)
                {
                    audit.Issued += world.MilitaryEquipmentIssues[i].Quantity;
                }
            }

            for (var i = 0;
                 i < world.MilitaryEquipmentTransactions.Count;
                 i++)
            {
                var transaction = world.MilitaryEquipmentTransactions[i];
                if (transaction.Type == MilitaryEquipmentTransactionType.Loss &&
                    transaction.FromArmyId == armyId)
                {
                    audit.Lost += transaction.Quantity;
                }
                else if (transaction.Type ==
                         MilitaryEquipmentTransactionType.Capture)
                {
                    if (transaction.FromArmyId == armyId)
                    {
                        audit.CapturedOut += transaction.Quantity;
                    }

                    if (transaction.ToArmyId == armyId)
                    {
                        audit.CapturedIn += transaction.Quantity;
                    }
                }
                else if (transaction.Type ==
                         MilitaryEquipmentTransactionType.Transfer)
                {
                    if (transaction.FromArmyId == armyId)
                    {
                        audit.TransferredOut += transaction.Quantity;
                    }

                    if (transaction.ToArmyId == armyId)
                    {
                        audit.TransferredIn += transaction.Quantity;
                    }
                }
            }

            return audit;
        }

        public void ResolveBattleEquipment(
            WorldState world,
            BattleRecordState battle,
            IList<MilitaryServiceState> attackerCasualties,
            IList<MilitaryServiceState> defenderCasualties)
        {
            if (!world.MilitaryEquipmentInitialized)
            {
                return;
            }

            ResolveDeadEquipment(
                world, battle, attackerCasualties, battle.WinnerArmyId);
            ResolveDeadEquipment(
                world, battle, defenderCasualties, battle.WinnerArmyId);
        }

        public void ResolveCasualtiesWithoutBattle(
            WorldState world,
            IList<MilitaryServiceState> casualties,
            long sequence)
        {
            if (!world.MilitaryEquipmentInitialized)
            {
                return;
            }

            for (var i = 0; i < casualties.Count; i++)
            {
                if (casualties[i].Status != MilitaryServiceStatus.Dead)
                {
                    continue;
                }

                ResolveServiceIssues(
                    world,
                    casualties[i],
                    string.Empty,
                    string.Empty,
                    sequence,
                    "casualty");
            }
        }

        public void ResolveDesertionLoss(
            WorldState world,
            IList<MilitaryServiceState> deserters,
            long sequence)
        {
            if (!world.MilitaryEquipmentInitialized)
            {
                return;
            }

            for (var i = 0; i < deserters.Count; i++)
            {
                var issues = IssuesForService(world, deserters[i].Id);
                for (var issueIndex = 0; issueIndex < issues.Count; issueIndex++)
                {
                    var issue = issues[issueIndex];
                    world.MilitaryEquipmentIssues.Remove(issue);
                    AddTransaction(
                        world,
                        MilitaryEquipmentTransactionType.Loss,
                        issue.EquipmentDefinitionId,
                        issue.Quantity,
                        issue.ArmyId,
                        string.Empty,
                        issue.MilitaryServiceId,
                        string.Empty,
                        $"Deserter equipment left army assets ({sequence}).");
                }
            }
        }

        private void ResolveDeadEquipment(
            WorldState world,
            BattleRecordState battle,
            IList<MilitaryServiceState> casualties,
            string winnerArmyId)
        {
            for (var i = 0; i < casualties.Count; i++)
            {
                if (casualties[i].Status != MilitaryServiceStatus.Dead)
                {
                    continue;
                }

                ResolveServiceIssues(
                    world,
                    casualties[i],
                    battle.Id,
                    winnerArmyId,
                    battle.Day,
                    "battle");
            }
        }

        private void ResolveServiceIssues(
            WorldState world,
            MilitaryServiceState service,
            string battleId,
            string winnerArmyId,
            long coordinate,
            string purpose)
        {
            var issues = IssuesForService(world, service.Id);
            var random = new NamedRandom(world.MasterSeed);
            for (var i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                var roll = random.Range(
                    "military_equipment",
                    new StableId(issue.Id),
                    coordinate,
                    purpose + "_disposition_" + battleId,
                    0,
                    10_000);
                world.MilitaryEquipmentIssues.Remove(issue);
                if (roll < 4_500)
                {
                    var stock = FindStock(
                        world, issue.ArmyId, issue.EquipmentDefinitionId);
                    stock.AvailableQuantity += issue.Quantity;
                    AddTransaction(
                        world,
                        MilitaryEquipmentTransactionType.Return,
                        issue.EquipmentDefinitionId,
                        issue.Quantity,
                        string.Empty,
                        issue.ArmyId,
                        issue.MilitaryServiceId,
                        battleId,
                        "Equipment recovered after casualty.");
                }
                else if (roll < 7_000)
                {
                    var stock = FindStock(
                        world, issue.ArmyId, issue.EquipmentDefinitionId);
                    stock.DamagedQuantity += issue.Quantity;
                    AddTransaction(
                        world,
                        MilitaryEquipmentTransactionType.Damage,
                        issue.EquipmentDefinitionId,
                        issue.Quantity,
                        issue.ArmyId,
                        string.Empty,
                        issue.MilitaryServiceId,
                        battleId,
                        "Equipment recovered damaged after casualty.");
                }
                else if (roll < 8_500 &&
                         !string.IsNullOrEmpty(winnerArmyId) &&
                         winnerArmyId != issue.ArmyId)
                {
                    var capturedStock = FindStock(
                        world, winnerArmyId, issue.EquipmentDefinitionId);
                    capturedStock.AvailableQuantity += issue.Quantity;
                    AddTransaction(
                        world,
                        MilitaryEquipmentTransactionType.Capture,
                        issue.EquipmentDefinitionId,
                        issue.Quantity,
                        issue.ArmyId,
                        winnerArmyId,
                        issue.MilitaryServiceId,
                        battleId,
                        "Equipment captured by the victorious army.");
                }
                else
                {
                    AddTransaction(
                        world,
                        MilitaryEquipmentTransactionType.Loss,
                        issue.EquipmentDefinitionId,
                        issue.Quantity,
                        issue.ArmyId,
                        string.Empty,
                        issue.MilitaryServiceId,
                        battleId,
                        "Equipment permanently lost after casualty.");
                }
            }
        }

        private void IssueArmyEquipment(WorldState world, ArmyState army)
        {
            var services = ActiveServices(world, army.Id);
            for (var i = 0; i < services.Count; i++)
            {
                if (services[i].Role == MilitaryServiceRole.Commander ||
                    services[i].Role == MilitaryServiceRole.Officer ||
                    services[i].Role == MilitaryServiceRole.Messenger)
                {
                    TryIssue(world, services[i], RingSwordId);
                }
            }

            for (var i = 0; i < services.Count; i++)
            {
                if (services[i].Role == MilitaryServiceRole.Commander ||
                    services[i].Role == MilitaryServiceRole.Officer)
                {
                    TryIssue(world, services[i], LamellarArmorId);
                }
            }

            var soldiers = CombatCandidates(world, services, "archery");
            for (var i = 0; i < soldiers.Count; i++)
            {
                var person = PeopleFor(world).GetRequired(soldiers[i].PersonId);
                var fitness =
                    (person.Aptitudes.Dexterity + person.Aptitudes.Perception) / 2;
                if (fitness < 4_500 ||
                    !CanIssue(world, soldiers[i], HornBowId) ||
                    !CanIssue(world, soldiers[i], ArrowBundleId))
                {
                    continue;
                }

                TryIssue(world, soldiers[i], HornBowId);
                TryIssue(world, soldiers[i], ArrowBundleId);
            }

            soldiers = CombatCandidates(world, services, "spear");
            for (var i = 0; i < soldiers.Count; i++)
            {
                var person = PeopleFor(world).GetRequired(soldiers[i].PersonId);
                var fitness =
                    (person.Aptitudes.Strength + person.Aptitudes.Constitution) / 2;
                if (fitness >= 4_500)
                {
                    TryIssue(world, soldiers[i], LongSpearId);
                }
            }

            soldiers = CombatCandidates(world, services, "sword_shield");
            for (var i = 0; i < soldiers.Count; i++)
            {
                var person = PeopleFor(world).GetRequired(soldiers[i].PersonId);
                var fitness =
                    (person.Aptitudes.Strength + person.Aptitudes.Constitution) / 2;
                if (fitness < 4_000 ||
                    !CanIssue(world, soldiers[i], RingSwordId) ||
                    !CanIssue(world, soldiers[i], WoodenShieldId))
                {
                    continue;
                }

                TryIssue(world, soldiers[i], RingSwordId);
                TryIssue(world, soldiers[i], WoodenShieldId);
            }

            soldiers = CombatCandidates(world, services, "remaining_melee");
            for (var i = 0; i < soldiers.Count; i++)
            {
                TryIssue(world, soldiers[i], RingSwordId);
            }

            var armorCandidates = CombatCandidates(world, services, "armor");
            for (var i = 0; i < armorCandidates.Count; i++)
            {
                TryIssue(world, armorCandidates[i], LamellarArmorId);
            }
        }

        private List<MilitaryServiceState> CombatCandidates(
            WorldState world,
            List<MilitaryServiceState> services,
            string purpose)
        {
            var result = new List<MilitaryServiceState>();
            for (var i = 0; i < services.Count; i++)
            {
                if (services[i].Role == MilitaryServiceRole.Soldier)
                {
                    result.Add(services[i]);
                }
            }

            result.Sort((left, right) =>
            {
                var leftPerson = PeopleFor(world).GetRequired(left.PersonId);
                var rightPerson = PeopleFor(world).GetRequired(right.PersonId);
                var leftScore = EquipmentCandidateScore(leftPerson, purpose);
                var rightScore = EquipmentCandidateScore(rightPerson, purpose);
                var score = rightScore.CompareTo(leftScore);
                return score != 0
                    ? score
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            return result;
        }

        private static int EquipmentCandidateScore(
            PersonState person,
            string purpose)
        {
            switch (purpose)
            {
                case "archery":
                    return person.Aptitudes.Dexterity +
                           person.Aptitudes.Perception +
                           person.ProfessionalSkills.MartialArts;
                case "spear":
                case "sword_shield":
                    return person.Aptitudes.Strength +
                           person.Aptitudes.Constitution +
                           person.ProfessionalSkills.MartialArts;
                case "armor":
                    return person.Aptitudes.Constitution +
                           person.HealthBasisPoints;
                default:
                    return person.ProfessionalSkills.MartialArts +
                           person.HealthBasisPoints;
            }
        }

        private static void AddCoreDefinitions(WorldState world)
        {
            AddDefinition(
                world, RingSwordId, "环首刀", "equipment_category.melee",
                "equipment_slot.main_hand", 3, 6_000, 0, 0, 3_500, 3_000);
            AddDefinition(
                world, WoodenShieldId, "木盾", "equipment_category.shield",
                "equipment_slot.off_hand", 5, 1_500, 0, 5_000, 3_000, 2_000);
            AddDefinition(
                world, LongSpearId, "长矛", "equipment_category.melee",
                "equipment_slot.main_hand", 5, 7_000, 0, 0, 4_500, 3_000);
            AddDefinition(
                world, HornBowId, "角弓", "equipment_category.ranged",
                "equipment_slot.main_hand", 2, 500, 7_000, 0, 2_500, 4_500);
            AddDefinition(
                world, ArrowBundleId, "箭束", "equipment_category.ammunition",
                "equipment_slot.ammunition", 2, 0, 2_000, 0, 0, 2_000,
                HornBowId);
            AddDefinition(
                world, LamellarArmorId, "札甲", "equipment_category.armor",
                "equipment_slot.armor", 10, 0, 0, 6_000, 5_000, 2_500);
        }

        private static void AddDefinition(
            WorldState world,
            string id,
            string displayName,
            string categoryId,
            string slotId,
            int weight,
            int melee,
            int ranged,
            int protection,
            int strength,
            int dexterity,
            string compatibleEquipmentId = "")
        {
            world.MilitaryEquipmentDefinitions.Add(
                new MilitaryEquipmentDefinitionState
                {
                    Id = id,
                    DisplayName = displayName,
                    CategoryId = categoryId,
                    SlotId = slotId,
                    UnitWeight = weight,
                    MaximumConditionBasisPoints = 10_000,
                    MeleePowerBasisPoints = melee,
                    RangedPowerBasisPoints = ranged,
                    ProtectionBasisPoints = protection,
                    RequiredStrengthBasisPoints = strength,
                    RequiredDexterityBasisPoints = dexterity,
                    CompatibleEquipmentId = compatibleEquipmentId
                });
        }

        private static void AddOpeningStocks(WorldState world, ArmyState army)
        {
            var quantities = OpeningQuantities(army.Id);
            for (var i = 0; i < world.MilitaryEquipmentDefinitions.Count; i++)
            {
                var definition = world.MilitaryEquipmentDefinitions[i];
                quantities.TryGetValue(definition.Id, out var quantity);
                world.MilitaryArmoryStocks.Add(new MilitaryArmoryStockState
                {
                    Id = $"armory_stock.{army.Id}.{definition.Id}",
                    ArmyId = army.Id,
                    EquipmentDefinitionId = definition.Id,
                    AvailableQuantity = quantity,
                    OpeningQuantity = quantity,
                    AverageConditionBasisPoints = 9_000
                });
                if (quantity > 0)
                {
                    AddTransaction(
                        world,
                        MilitaryEquipmentTransactionType.OpeningStock,
                        definition.Id,
                        quantity,
                        string.Empty,
                        army.Id,
                        string.Empty,
                        string.Empty,
                        "Prototype opening armory stock.");
                }
            }
        }

        private static Dictionary<string, int> OpeningQuantities(string armyId)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (armyId == "army.han_jizhou_vanguard")
            {
                SetQuantities(result, 28, 24, 32, 12, 12, 18);
            }
            else if (armyId == "army.youzhou_reinforcement")
            {
                SetQuantities(result, 16, 12, 38, 24, 24, 10);
            }
            else
            {
                SetQuantities(result, 18, 8, 20, 8, 8, 4);
            }

            return result;
        }

        private static void SetQuantities(
            Dictionary<string, int> result,
            int swords,
            int shields,
            int spears,
            int bows,
            int arrows,
            int armor)
        {
            result[RingSwordId] = swords;
            result[WoodenShieldId] = shields;
            result[LongSpearId] = spears;
            result[HornBowId] = bows;
            result[ArrowBundleId] = arrows;
            result[LamellarArmorId] = armor;
        }

        private static List<MilitaryServiceState> ActiveServices(
            WorldState world,
            string armyId)
        {
            var result = new List<MilitaryServiceState>();
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.ArmyId == armyId &&
                    (service.Status == MilitaryServiceStatus.Active ||
                     service.Status == MilitaryServiceStatus.Mustering))
                {
                    result.Add(service);
                }
            }

            result.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static bool CanIssue(
            WorldState world,
            MilitaryServiceState service,
            string equipmentId)
        {
            var definition = FindDefinition(world, equipmentId);
            return FindStock(world, service.ArmyId, equipmentId)
                       .AvailableQuantity > 0 &&
                   !HasSlotIssue(world, service.Id, definition.SlotId);
        }

        private static bool TryIssue(
            WorldState world,
            MilitaryServiceState service,
            string equipmentId)
        {
            if (!CanIssue(world, service, equipmentId))
            {
                return false;
            }

            var definition = FindDefinition(world, equipmentId);
            var stock = FindStock(world, service.ArmyId, equipmentId);
            stock.AvailableQuantity--;
            world.MilitaryEquipmentIssues.Add(new MilitaryEquipmentIssueState
            {
                Id = $"equipment_issue.{service.Id}.{equipmentId}",
                MilitaryServiceId = service.Id,
                PersonId = service.PersonId,
                ArmyId = service.ArmyId,
                EquipmentDefinitionId = equipmentId,
                SlotId = definition.SlotId,
                Quantity = 1,
                ConditionBasisPoints = stock.AverageConditionBasisPoints,
                IssuedDay = world.AbsoluteDay,
                LastChangedDay = world.AbsoluteDay
            });
            AddTransaction(
                world,
                MilitaryEquipmentTransactionType.Issue,
                equipmentId,
                1,
                service.ArmyId,
                string.Empty,
                service.Id,
                string.Empty,
                "Equipment issued to a real service member.");
            return true;
        }

        private static void AddTransaction(
            WorldState world,
            MilitaryEquipmentTransactionType type,
            string equipmentId,
            int quantity,
            string fromArmyId,
            string toArmyId,
            string serviceId,
            string battleId,
            string summary)
        {
            var sequence = world.MilitaryEquipmentTransactions.Count;
            world.MilitaryEquipmentTransactions.Add(
                new MilitaryEquipmentTransactionState
                {
                    Id = $"equipment_transaction.{world.AbsoluteDay}." +
                         $"{sequence:000000}",
                    Day = world.AbsoluteDay,
                    Type = type,
                    EquipmentDefinitionId = equipmentId,
                    Quantity = quantity,
                    FromArmyId = fromArmyId,
                    ToArmyId = toArmyId,
                    MilitaryServiceId = serviceId,
                    BattleId = battleId,
                    Summary = summary
                });
        }

        private static List<MilitaryEquipmentIssueState> IssuesForService(
            WorldState world,
            string serviceId)
        {
            var result = new List<MilitaryEquipmentIssueState>();
            for (var i = 0; i < world.MilitaryEquipmentIssues.Count; i++)
            {
                if (world.MilitaryEquipmentIssues[i].MilitaryServiceId ==
                    serviceId)
                {
                    result.Add(world.MilitaryEquipmentIssues[i]);
                }
            }

            result.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static bool HasIssue(
            WorldState world,
            string serviceId,
            string equipmentId)
        {
            for (var i = 0; i < world.MilitaryEquipmentIssues.Count; i++)
            {
                var issue = world.MilitaryEquipmentIssues[i];
                if (issue.MilitaryServiceId == serviceId &&
                    issue.EquipmentDefinitionId == equipmentId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasSlotIssue(
            WorldState world,
            string serviceId,
            string slotId)
        {
            for (var i = 0; i < world.MilitaryEquipmentIssues.Count; i++)
            {
                var issue = world.MilitaryEquipmentIssues[i];
                if (issue.MilitaryServiceId == serviceId &&
                    issue.SlotId == slotId)
                {
                    return true;
                }
            }

            return false;
        }

        private static MilitaryEquipmentDefinitionState FindDefinition(
            WorldState world,
            string equipmentId)
        {
            for (var i = 0; i < world.MilitaryEquipmentDefinitions.Count; i++)
            {
                if (world.MilitaryEquipmentDefinitions[i].Id == equipmentId)
                {
                    return world.MilitaryEquipmentDefinitions[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military equipment definition {equipmentId}.");
        }

        private static MilitaryArmoryStockState FindStock(
            WorldState world,
            string armyId,
            string equipmentId)
        {
            for (var i = 0; i < world.MilitaryArmoryStocks.Count; i++)
            {
                var stock = world.MilitaryArmoryStocks[i];
                if (stock.ArmyId == armyId &&
                    stock.EquipmentDefinitionId == equipmentId)
                {
                    return stock;
                }
            }

            throw new InvalidOperationException(
                $"Missing armory stock {armyId}/{equipmentId}.");
        }

        private static TroopDerivationResult SupportResult(string troopTypeId)
        {
            return new TroopDerivationResult
            {
                TroopTypeId = troopTypeId,
                CountsTowardCombatReadiness = false,
                MeetsMinimumEquipment = true
            };
        }

        private static void AddCount(
            Dictionary<string, int> counts,
            string key)
        {
            counts.TryGetValue(key, out var count);
            counts[key] = count + 1;
        }

        private IPersonRepository PeopleFor(WorldState world)
        {
            if (_people != null)
            {
                return _people;
            }

            if (!ReferenceEquals(_fallbackWorld, world))
            {
                _fallbackWorld = world;
                _fallbackPeople = new WorldStatePersonRepository(world);
            }

            return _fallbackPeople;
        }
    }
}
