using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MilitaryReturnTeamDeathSystem
    {
        private readonly IPersonRepository _people;

        public MilitaryReturnTeamDeathSystem(IPersonRepository people = null)
        {
            _people = people;
        }

        public MilitaryReturnTeamDeathState ResolveReturnJourneyDeath(
            WorldState world,
            StableId evacuationId,
            StableId teamMemberPersonId,
            StableId authorizingPersonId,
            StableId policyId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var evacuation = FindEvacuation(world, evacuationId.Value);
            var member = FindTeamMember(
                evacuation, teamMemberPersonId.Value);
            var service = FindService(world, member.MilitaryServiceId);
            var army = FindArmy(world, evacuation.SourceArmyId);
            var organization = FindOrganization(
                world, army.OrganizationId);
            var policy = FindPolicy(world, policyId.Value);
            var journey = FindJourney(world, member.ReturnJourneyId);
            var people = _people ?? new WorldStatePersonRepository(world);
            var person = people.GetRequired(member.PersonId);
            var family = FindFamily(world, person.FamilyId);
            var authority = new MilitaryAuthoritySystem().GetAuthority(
                world, authorizingPersonId, new StableId(army.Id));
            var closingHealth = Math.Max(
                0,
                person.HealthBasisPoints - policy.HealthLossBasisPoints);
            var validReturnStatus = evacuation.Status ==
                    MilitaryMedicalEvacuationStatus.ReturningToArmy ||
                evacuation.Status == MilitaryMedicalEvacuationStatus
                    .PatientDeceasedReturningToArmy ||
                evacuation.Status == MilitaryMedicalEvacuationStatus
                    .PatientDeceasedAwaitingTeamRejoin;
            if (world.AbsoluteDay <
                    world.MilitaryReturnTeamDeathContractActivationDay ||
                !validReturnStatus ||
                string.IsNullOrEmpty(evacuation.ReturnRouteId) ||
                evacuation.ReturnStartedDay < 0 ||
                world.AbsoluteDay < checked(
                    evacuation.ReturnStartedDay +
                    policy.MinimumDaysAfterReturnStart) ||
                !string.IsNullOrEmpty(member.ReturnDeathId) ||
                service.PersonId != person.Id ||
                service.ArmyId != army.Id ||
                service.Status !=
                    MilitaryServiceStatus.MedicalEvacuationDuty ||
                !person.IsAlive ||
                journey == null ||
                journey.PersonId != person.Id ||
                journey.RouteId != evacuation.ReturnRouteId ||
                journey.OriginLocationId !=
                    evacuation.CurrentCareLocationId ||
                journey.DestinationLocationId !=
                    evacuation.ReturnDestinationLocationId ||
                journey.Mode != TravelMode.Foot ||
                journey.RemainingKilometers <= 0 ||
                person.LocationId != evacuation.CurrentCareLocationId ||
                army.LocationId != evacuation.ReturnDestinationLocationId ||
                closingHealth > policy.MaximumClosingHealthBasisPoints ||
                authority < MilitaryAuthorityLevel.Army ||
                !family.MemberIds.Contains(person.Id) ||
                HasPermanentMilitaryDeath(world, person.Id))
            {
                throw new InvalidOperationException(
                    "The evacuation team member is not eligible for a " +
                    "return-journey death.");
            }

            var formerHead = family.HeadPersonId;
            var headChanged = formerHead == person.Id;
            var successor = headChanged
                ? SelectSuccessor(family, person.Id, people)
                : people.GetRequired(formerHead);
            if (successor == null || !successor.IsAlive)
            {
                throw new InvalidOperationException(
                    "The family has no living successor for this death.");
            }

            var compensationAmount = checked(
                policy.BaseCompensationMoney +
                policy.CompensationPerRankMoney * service.Rank);
            if (compensationAmount < 0 ||
                organization.Treasury < compensationAmount ||
                person.Wealth < 0)
            {
                throw new InvalidOperationException(
                    "The organization cannot fund the survivor compensation.");
            }

            var familyAfterInheritance = checked(
                family.Wealth + person.Wealth);
            var familyAfterCompensation = checked(
                familyAfterInheritance + compensationAmount);
            var organizationAfter = checked(
                organization.Treasury - compensationAmount);
            var index = world.MilitaryReturnTeamDeaths.Count;
            var deathId = $"military_return_team_death." +
                $"{world.AbsoluteDay}.{index:D6}";
            var inheritanceId = $"military_family_inheritance.return_team." +
                $"{world.AbsoluteDay}.{index:D6}";
            var compensationId =
                $"military_survivor_compensation.return_team." +
                $"{world.AbsoluteDay}.{index:D6}";
            var deathLifeEventId = $"life_event.{deathId}.death";
            var successionLifeEventId = headChanged
                ? $"life_event.{deathId}.succession"
                : string.Empty;

            var inheritance = new MilitaryFamilyInheritanceState
            {
                Id = inheritanceId,
                Day = world.AbsoluteDay,
                WoundDeathId = string.Empty,
                ReturnTeamDeathId = deathId,
                FamilyId = family.Id,
                DeceasedPersonId = person.Id,
                FormerHeadPersonId = formerHead,
                SuccessorPersonId = successor.Id,
                HeadChanged = headChanged,
                DeceasedWealthBefore = person.Wealth,
                DeceasedWealthAfter = 0,
                FamilyWealthBefore = family.Wealth,
                FamilyWealthAfter = familyAfterInheritance
            };
            var compensation = new MilitarySurvivorCompensationState
            {
                Id = compensationId,
                Day = world.AbsoluteDay,
                WoundDeathId = string.Empty,
                ReturnTeamDeathId = deathId,
                PolicyId = policy.Id,
                ArmyId = army.Id,
                OrganizationId = organization.Id,
                FamilyId = family.Id,
                DeceasedPersonId = person.Id,
                AuthorizingPersonId = authorizingPersonId.Value,
                AuthorizingAuthority = authority,
                MilitaryRankAtDeath = service.Rank,
                Amount = compensationAmount,
                OrganizationTreasuryBefore = organization.Treasury,
                OrganizationTreasuryAfter = organizationAfter,
                FamilyWealthBefore = familyAfterInheritance,
                FamilyWealthAfter = familyAfterCompensation
            };
            var death = new MilitaryReturnTeamDeathState
            {
                Id = deathId,
                Day = world.AbsoluteDay,
                PolicyId = policy.Id,
                CorpsePolicyId = MilitaryReturnTeamCorpsePolicyIds
                    .ContinueExistingJourneyToSourceArmy,
                EvacuationId = evacuation.Id,
                PersonId = person.Id,
                MilitaryServiceId = service.Id,
                SourceArmyId = army.Id,
                OrganizationId = organization.Id,
                ReturnJourneyId = journey.Id,
                ReturnRouteId = evacuation.ReturnRouteId,
                ReturnOriginLocationId =
                    evacuation.CurrentCareLocationId,
                ReturnDestinationLocationId =
                    evacuation.ReturnDestinationLocationId,
                ReturnStartedDay = evacuation.ReturnStartedDay,
                RemainingKilometersAtDeath = journey.RemainingKilometers,
                OpeningHealthBasisPoints = person.HealthBasisPoints,
                HealthLossBasisPoints = policy.HealthLossBasisPoints,
                ClosingHealthBasisPoints = closingHealth,
                AuthorizingPersonId = authorizingPersonId.Value,
                AuthorizingAuthority = authority,
                FamilyId = family.Id,
                FamilyInheritanceId = inheritance.Id,
                SurvivorCompensationId = compensation.Id,
                DeathLifeEventId = deathLifeEventId,
                SuccessionLifeEventId = successionLifeEventId,
                CorpseArrivedDay = -1
            };

            var writablePerson = people.GetRequiredForUpdate(person.Id);
            writablePerson.HealthBasisPoints = closingHealth;
            writablePerson.Wealth = 0;
            family.Wealth = familyAfterCompensation;
            if (headChanged)
            {
                family.HeadPersonId = successor.Id;
            }
            organization.Treasury = organizationAfter;
            service.Status = MilitaryServiceStatus.Dead;
            service.LastStatusChangeDay = world.AbsoluteDay;
            member.ReturnDeathId = death.Id;
            world.MilitaryFamilyInheritances.Add(inheritance);
            world.MilitarySurvivorCompensations.Add(compensation);
            world.MilitaryReturnTeamDeaths.Add(death);
            world.LifeEvents.Add(new LifeEventRecordState
            {
                Id = deathLifeEventId,
                Type = LifeEventType.Death,
                Day = world.AbsoluteDay,
                PrimaryPersonId = person.Id,
                SecondaryPersonId = string.Empty,
                FamilyId = family.Id,
                Summary = $"{person.DisplayName} died while returning from " +
                    "medical evacuation duty."
            });
            if (headChanged)
            {
                world.LifeEvents.Add(new LifeEventRecordState
                {
                    Id = successionLifeEventId,
                    Type = LifeEventType.Succession,
                    Day = world.AbsoluteDay,
                    PrimaryPersonId = successor.Id,
                    SecondaryPersonId = person.Id,
                    FamilyId = family.Id,
                    Summary = $"{successor.DisplayName} succeeded as head " +
                        $"of {family.DisplayName}."
                });
            }

            new PopulationLedgerSystem(people).RecordDeaths(
                world, new[] { writablePerson }, false);
            new MilitaryServiceSystem(people).SynchronizeArmyCaches(
                world, army.Id);
            return death;
        }

        private static bool HasPermanentMilitaryDeath(
            WorldState world, string personId)
        {
            if (world.MilitaryWoundDeaths.Exists(item =>
                    item.PatientPersonId == personId))
            {
                return true;
            }
            return world.MilitaryReturnTeamDeaths.Exists(item =>
                item.PersonId == personId);
        }

        private static PersonState SelectSuccessor(
            FamilyState family,
            string deceasedPersonId,
            IPersonRepository people)
        {
            PersonState selected = null;
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var candidateId = family.MemberIds[i];
                if (candidateId == deceasedPersonId)
                {
                    continue;
                }
                var candidate = people.GetRequired(candidateId);
                if (!candidate.IsAlive)
                {
                    continue;
                }
                if (selected == null ||
                    candidate.BirthDay < selected.BirthDay ||
                    candidate.BirthDay == selected.BirthDay &&
                    string.CompareOrdinal(candidate.Id, selected.Id) < 0)
                {
                    selected = candidate;
                }
            }
            return selected;
        }

        private static MilitaryMedicalEvacuationState FindEvacuation(
            WorldState world, string id)
        {
            var result = world.MilitaryMedicalEvacuations.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing military medical evacuation {id}.");
        }

        private static MilitaryMedicalEvacuationTeamMemberState FindTeamMember(
            MilitaryMedicalEvacuationState evacuation, string personId)
        {
            var result = evacuation.TeamMembers.Find(
                item => item.PersonId == personId);
            return result ?? throw new InvalidOperationException(
                $"Person {personId} is not an evacuation team member.");
        }

        private static MilitaryServiceState FindService(
            WorldState world, string id)
        {
            var result = world.MilitaryServices.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing military service {id}.");
        }

        private static ArmyState FindArmy(WorldState world, string id)
        {
            var result = world.Armies.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing army {id}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world, string id)
        {
            var result = world.Organizations.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing organization {id}.");
        }

        private static FamilyState FindFamily(WorldState world, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException(
                    "A return-team death requires a permanent family.");
            }
            var result = world.Families.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing family {id}.");
        }

        private static MilitaryReturnTeamDeathPolicyDefinitionState FindPolicy(
            WorldState world, string id)
        {
            var result = world.MilitaryReturnTeamDeathPolicies.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing military return-team death policy {id}.");
        }

        private static JourneyState FindJourney(WorldState world, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            return world.Journeys.Find(item => item.Id == id);
        }
    }
}
