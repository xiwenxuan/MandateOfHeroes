using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum NpcMonthlyFocus
    {
        MaintainLivelihood,
        CareForFamily,
        ImproveStatus,
        AccumulateWealth,
        MaintainRelationships,
        RespondToWar
    }

    public readonly struct NpcFocusScore
    {
        public NpcMonthlyFocus Focus { get; }
        public int Score { get; }
        public string Reason { get; }

        public NpcFocusScore(NpcMonthlyFocus focus, int score, string reason)
        {
            Focus = focus;
            Score = score;
            Reason = reason;
        }
    }

    public sealed class NpcDecision
    {
        public StableId PersonId { get; }
        public long MonthIndex { get; }
        public NpcMonthlyFocus SelectedFocus { get; }
        public IReadOnlyList<NpcFocusScore> RankedScores { get; }

        public NpcDecision(
            StableId personId,
            long monthIndex,
            NpcMonthlyFocus selectedFocus,
            IReadOnlyList<NpcFocusScore> rankedScores)
        {
            PersonId = personId;
            MonthIndex = monthIndex;
            SelectedFocus = selectedFocus;
            RankedScores = rankedScores;
        }
    }

    public sealed class NpcDecisionSystem
    {
        private readonly NamedRandom _random;

        public NpcDecisionSystem(ulong masterSeed)
        {
            _random = new NamedRandom(masterSeed);
        }

        public NpcDecision ChooseMonthlyFocus(PersonState person, long monthIndex)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (!person.IsAlive)
            {
                throw new InvalidOperationException("A deceased person cannot choose a focus.");
            }

            if (monthIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(monthIndex));
            }

            var personId = new StableId(person.Id);
            var scores = new List<NpcFocusScore>
            {
                Score(
                    personId,
                    monthIndex,
                    NpcMonthlyFocus.MaintainLivelihood,
                    person.Needs.Livelihood + ScarcityPressure(person),
                    $"生计需要{person.Needs.Livelihood}，物资压力{ScarcityPressure(person)}"),
                Score(
                    personId,
                    monthIndex,
                    NpcMonthlyFocus.CareForFamily,
                    person.Needs.Family + person.Personality.FamilyDuty / 2,
                    $"家庭需要{person.Needs.Family}，顾家倾向{person.Personality.FamilyDuty / 2}"),
                Score(
                    personId,
                    monthIndex,
                    NpcMonthlyFocus.ImproveStatus,
                    person.Needs.Status + person.Personality.Ambition / 2,
                    $"身份需要{person.Needs.Status}，野心倾向{person.Personality.Ambition / 2}"),
                Score(
                    personId,
                    monthIndex,
                    NpcMonthlyFocus.AccumulateWealth,
                    person.Needs.Wealth + person.Personality.Ambition / 4,
                    $"财富需要{person.Needs.Wealth}，野心倾向{person.Personality.Ambition / 4}"),
                Score(
                    personId,
                    monthIndex,
                    NpcMonthlyFocus.MaintainRelationships,
                    person.Needs.Relationships + person.Personality.Sociability / 2,
                    $"关系需要{person.Needs.Relationships}，社交倾向{person.Personality.Sociability / 2}"),
                Score(
                    personId,
                    monthIndex,
                    NpcMonthlyFocus.RespondToWar,
                    person.Needs.WarPressure + WarPersonalityModifier(person),
                    $"战争压力{person.Needs.WarPressure}，应战倾向{WarPersonalityModifier(person)}")
            };

            scores.Sort(CompareScores);
            return new NpcDecision(personId, monthIndex, scores[0].Focus, scores);
        }

        private NpcFocusScore Score(
            StableId personId,
            long monthIndex,
            NpcMonthlyFocus focus,
            int baseScore,
            string reason)
        {
            var variation = _random.Range(
                "npc_ai",
                personId,
                monthIndex,
                "monthly_focus_" + focus,
                0,
                201);
            return new NpcFocusScore(focus, baseScore + variation, reason + $"，月度扰动{variation}");
        }

        private static int ScarcityPressure(PersonState person)
        {
            if (person.Provisions <= 0)
            {
                return 6_000;
            }

            if (person.Provisions <= 3)
            {
                return 3_000;
            }

            return person.Provisions <= 7 ? 1_000 : 0;
        }

        private static int WarPersonalityModifier(PersonState person)
        {
            // Both the bold and the cautious respond to war, but later select different actions.
            var distanceFromNeutral = Math.Abs(person.Personality.RiskTolerance - 5_000);
            return 1_000 + distanceFromNeutral / 4;
        }

        private static int CompareScores(NpcFocusScore left, NpcFocusScore right)
        {
            var scoreComparison = right.Score.CompareTo(left.Score);
            return scoreComparison != 0
                ? scoreComparison
                : left.Focus.CompareTo(right.Focus);
        }
    }
}
