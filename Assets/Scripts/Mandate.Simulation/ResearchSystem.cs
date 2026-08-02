using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class ResearchSystem
    {
        private readonly ProductionContentRegistry _content;

        public ResearchSystem(ProductionContentRegistry content = null)
        {
            _content = content ?? ProductionContentRegistry.CreateCore();
        }

        public KnowledgeMasteryState GrantKnowledge(
            WorldState world,
            string personId,
            string knowledgeDefinitionId,
            int masteryBasisPoints,
            string sourceId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            _content.ValidateManifest(world.ProductionContentManifest);
            _content.GetKnowledge(knowledgeDefinitionId);
            if (masteryBasisPoints <= 0 || masteryBasisPoints > 10_000)
            {
                throw new ArgumentOutOfRangeException(nameof(masteryBasisPoints));
            }

            var person = FindPerson(world, personId);
            EnsureProgressionCollections(person);
            for (var i = 0; i < person.KnowledgeMasteries.Count; i++)
            {
                var existing = person.KnowledgeMasteries[i];
                if (existing.KnowledgeDefinitionId != knowledgeDefinitionId)
                {
                    continue;
                }

                if (masteryBasisPoints <= existing.MasteryBasisPoints)
                {
                    return existing;
                }

                var gain = masteryBasisPoints - existing.MasteryBasisPoints;
                existing.MasteryBasisPoints = masteryBasisPoints;
                existing.LearnedDay = world.AbsoluteDay;
                existing.SourceId = sourceId;
                AddLedger(
                    world,
                    ResearchLedgerEntryType.KnowledgeLearned,
                    null,
                    null,
                    knowledgeDefinitionId,
                    null,
                    person.Id,
                    null,
                    0,
                    gain,
                    $"{person.DisplayName} improved {knowledgeDefinitionId} knowledge.");
                return existing;
            }

            var mastery = new KnowledgeMasteryState
            {
                KnowledgeDefinitionId = knowledgeDefinitionId,
                MasteryBasisPoints = masteryBasisPoints,
                LearnedDay = world.AbsoluteDay,
                SourceId = sourceId
            };
            person.KnowledgeMasteries.Add(mastery);
            person.KnowledgeMasteries.Sort((left, right) =>
                string.CompareOrdinal(
                    left.KnowledgeDefinitionId,
                    right.KnowledgeDefinitionId));
            AddLedger(
                world,
                ResearchLedgerEntryType.KnowledgeLearned,
                null,
                null,
                knowledgeDefinitionId,
                null,
                person.Id,
                null,
                0,
                masteryBasisPoints,
                $"{person.DisplayName} learned {knowledgeDefinitionId}.");
            return mastery;
        }

        public ResearchProjectState StartProject(
            WorldState world,
            string technologyDefinitionId,
            string leadPersonId,
            string researchFacilityId,
            ResearchControlMode controlMode)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (!Enum.IsDefined(typeof(ResearchControlMode), controlMode))
            {
                throw new ArgumentOutOfRangeException(nameof(controlMode));
            }

            _content.ValidateManifest(world.ProductionContentManifest);
            var technology = _content.GetTechnology(technologyDefinitionId);
            var lead = FindPerson(world, leadPersonId);
            var facility = FindFacility(world, researchFacilityId);
            var village = FindVillage(world, facility.VillageId);
            if (!lead.IsAlive || lead.LocationId != village.LocationId ||
                !technology.ResearchFacilityTags.Contains(
                    VillageFacilityTags.FromKind(facility.Kind)))
            {
                throw new InvalidOperationException(
                    "Research lead or facility is unavailable.");
            }

            if (SkillMasteryAccess.Get(
                    lead, technology.RequiredSkillDefinitionId) <
                technology.RequiredSkillBasisPoints)
            {
                throw new InvalidOperationException(
                    $"{lead.Id} lacks required skill for {technology.Id}.");
            }

            for (var i = 0;
                 i < technology.RequiredKnowledgeDefinitionIds.Count;
                 i++)
            {
                var knowledgeId =
                    technology.RequiredKnowledgeDefinitionIds[i];
                if (SkillMasteryAccess.GetKnowledgeMastery(lead, knowledgeId) <
                    technology.RequiredKnowledgeMasteryBasisPoints)
                {
                    throw new InvalidOperationException(
                        $"{lead.Id} lacks required knowledge {knowledgeId}.");
                }
            }

            for (var i = 0; i < world.ResearchProjects.Count; i++)
            {
                var active = world.ResearchProjects[i];
                if (active.Status == ResearchProjectStatus.Active &&
                    active.LeadPersonId == lead.Id)
                {
                    throw new InvalidOperationException(
                        $"{lead.Id} already leads active research.");
                }
            }

            var family = FindFamily(world, lead.FamilyId);
            if (family.Wealth < technology.FundingCost)
            {
                throw new InvalidOperationException(
                    $"{family.Id} lacks research funding.");
            }

            var project = new ResearchProjectState
            {
                Id = $"research.{world.AbsoluteDay}." +
                     $"{world.ResearchProjects.Count:D6}",
                TechnologyDefinitionId = technology.Id,
                LeadPersonId = lead.Id,
                ResearchFacilityId = facility.Id,
                ControlMode = controlMode,
                Status = ResearchProjectStatus.Active,
                StartedDay = world.AbsoluteDay,
                RequiredResearchPoints = technology.ResearchPointsRequired,
                FundingCommitted = technology.FundingCost
            };
            family.Wealth -= technology.FundingCost;
            world.ResearchProjects.Add(project);
            AddLedger(
                world,
                ResearchLedgerEntryType.FundingCommitted,
                project.Id,
                null,
                null,
                technology.Id,
                lead.Id,
                facility.Id,
                -technology.FundingCost,
                0,
                $"{family.DisplayName} funded research {technology.DisplayName}.");
            return project;
        }

        public void ResolveDailyProjects(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            _content.ValidateManifest(world.ProductionContentManifest);
            var active = new List<ResearchProjectState>();
            for (var i = 0; i < world.ResearchProjects.Count; i++)
            {
                if (world.ResearchProjects[i].Status ==
                    ResearchProjectStatus.Active)
                {
                    active.Add(world.ResearchProjects[i]);
                }
            }

            active.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < active.Count; i++)
            {
                ResolveProjectDay(world, active[i]);
            }
        }

        public TechnologyApplicationState ApplyTechnology(
            WorldState world,
            string technologyDefinitionId,
            string targetFacilityId,
            string applicantPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            _content.ValidateManifest(world.ProductionContentManifest);
            var technology = _content.GetTechnology(technologyDefinitionId);
            var applicant = FindPerson(world, applicantPersonId);
            var facility = FindFacility(world, targetFacilityId);
            var village = FindVillage(world, facility.VillageId);
            var facilityTag = VillageFacilityTags.FromKind(facility.Kind);
            if (!applicant.IsAlive || applicant.LocationId != village.LocationId ||
                !SkillMasteryAccess.HasTechnology(
                    applicant, technologyDefinitionId))
            {
                throw new InvalidOperationException(
                    "Technology applicant is unavailable or has not mastered it.");
            }

            var hasApplicableEffect = false;
            for (var i = 0; i < technology.Effects.Count; i++)
            {
                if (technology.Effects[i].TargetFacilityTag == facilityTag)
                {
                    hasApplicableEffect = true;
                    break;
                }
            }

            if (!hasApplicableEffect)
            {
                throw new InvalidOperationException(
                    $"Technology {technology.Id} cannot apply to {facility.Id}.");
            }

            for (var i = 0; i < world.TechnologyApplications.Count; i++)
            {
                var existing = world.TechnologyApplications[i];
                if (existing.IsActive &&
                    existing.TechnologyDefinitionId == technology.Id &&
                    existing.TargetFacilityId == facility.Id)
                {
                    throw new InvalidOperationException(
                        $"Technology {technology.Id} is already active on {facility.Id}.");
                }
            }

            var family = FindFamily(world, applicant.FamilyId);
            if (family.Wealth < technology.ApplicationFundingCost)
            {
                throw new InvalidOperationException(
                    $"{family.Id} lacks technology application funding.");
            }

            var application = new TechnologyApplicationState
            {
                Id = $"technology_application.{world.AbsoluteDay}." +
                     $"{world.TechnologyApplications.Count:D6}",
                TechnologyDefinitionId = technology.Id,
                TargetFacilityId = facility.Id,
                AppliedByPersonId = applicant.Id,
                AppliedDay = world.AbsoluteDay,
                IsActive = true
            };
            family.Wealth -= technology.ApplicationFundingCost;
            world.TechnologyApplications.Add(application);
            AddLedger(
                world,
                ResearchLedgerEntryType.TechnologyApplied,
                null,
                application.Id,
                null,
                technology.Id,
                applicant.Id,
                facility.Id,
                -technology.ApplicationFundingCost,
                0,
                $"{technology.DisplayName} applied to {facility.Id}.");
            return application;
        }

        public ProductionTechnologyFactors ResolveProductionFactors(
            WorldState world,
            string facilityId,
            string recipeDefinitionId,
            string methodDefinitionId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var facility = FindFacility(world, facilityId);
            var facilityTag = VillageFacilityTags.FromKind(facility.Kind);
            var applications = new List<TechnologyApplicationState>();
            for (var i = 0; i < world.TechnologyApplications.Count; i++)
            {
                var application = world.TechnologyApplications[i];
                if (application.IsActive &&
                    application.TargetFacilityId == facility.Id)
                {
                    applications.Add(application);
                }
            }

            applications.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var yieldFactor = 10_000;
            var laborFactor = 10_000;
            var technologyIds = new List<string>();
            for (var applicationIndex = 0;
                 applicationIndex < applications.Count;
                 applicationIndex++)
            {
                var technology = _content.GetTechnology(
                    applications[applicationIndex].TechnologyDefinitionId);
                var applied = false;
                for (var effectIndex = 0;
                     effectIndex < technology.Effects.Count;
                     effectIndex++)
                {
                    var effect = technology.Effects[effectIndex];
                    if (effect.TargetFacilityTag != facilityTag ||
                        effect.RecipeDefinitionId != recipeDefinitionId ||
                        effect.MethodDefinitionId != methodDefinitionId)
                    {
                        continue;
                    }

                    yieldFactor = CombineFactors(
                        yieldFactor, effect.YieldBasisPoints);
                    laborFactor = CombineFactors(
                        laborFactor, effect.LaborBasisPoints);
                    applied = true;
                }

                if (applied)
                {
                    technologyIds.Add(technology.Id);
                }
            }

            return new ProductionTechnologyFactors(
                yieldFactor,
                laborFactor,
                technologyIds);
        }

        private void ResolveProjectDay(
            WorldState world,
            ResearchProjectState project)
        {
            if (project.LastProgressDay == world.AbsoluteDay)
            {
                return;
            }

            var technology = _content.GetTechnology(
                project.TechnologyDefinitionId);
            var lead = FindPerson(world, project.LeadPersonId);
            var facility = FindFacility(world, project.ResearchFacilityId);
            var village = FindVillage(world, facility.VillageId);
            project.LastProgressDay = world.AbsoluteDay;
            if (!lead.IsAlive || lead.LocationId != village.LocationId)
            {
                return;
            }

            var skill = SkillMasteryAccess.Get(
                lead, technology.RequiredSkillDefinitionId);
            var aptitude = (
                lead.Aptitudes.Reasoning * 2 +
                lead.Aptitudes.Memory +
                lead.Aptitudes.Perception) / 4;
            var progress = Math.Max(1, (skill * 3 + aptitude * 2) / 50);
            progress = Math.Max(
                1,
                progress * facility.ConditionBasisPoints / 10_000);
            var remaining = project.RequiredResearchPoints -
                project.ProgressResearchPoints;
            progress = Math.Min(progress, remaining);
            project.ProgressResearchPoints += progress;
            AddLedger(
                world,
                ResearchLedgerEntryType.ProgressAdded,
                project.Id,
                null,
                null,
                technology.Id,
                lead.Id,
                facility.Id,
                0,
                progress,
                $"{lead.DisplayName} added {progress} research points.");

            if (project.ProgressResearchPoints < project.RequiredResearchPoints)
            {
                return;
            }

            project.Status = ResearchProjectStatus.Completed;
            project.CompletedDay = world.AbsoluteDay;
            EnsureProgressionCollections(lead);
            if (!SkillMasteryAccess.HasTechnology(lead, technology.Id))
            {
                lead.TechnologyMasteries.Add(new TechnologyMasteryState
                {
                    TechnologyDefinitionId = technology.Id,
                    MasteredDay = world.AbsoluteDay,
                    ResearchProjectId = project.Id,
                    SourceId = project.Id
                });
                lead.TechnologyMasteries.Sort((left, right) =>
                    string.CompareOrdinal(
                        left.TechnologyDefinitionId,
                        right.TechnologyDefinitionId));
            }

            AddLedger(
                world,
                ResearchLedgerEntryType.TechnologyMastered,
                project.Id,
                null,
                null,
                technology.Id,
                lead.Id,
                facility.Id,
                0,
                0,
                $"{lead.DisplayName} mastered {technology.DisplayName}.");
        }

        private static int CombineFactors(int left, int right)
        {
            return (int)Math.Min(30_000L, left * (long)right / 10_000L);
        }

        private static void EnsureProgressionCollections(PersonState person)
        {
            person.SkillMasteries ??= new List<SkillMasteryState>();
            person.KnowledgeMasteries ??= new List<KnowledgeMasteryState>();
            person.TechnologyMasteries ??= new List<TechnologyMasteryState>();
        }

        private static void AddLedger(
            WorldState world,
            ResearchLedgerEntryType type,
            string projectId,
            string applicationId,
            string knowledgeId,
            string technologyId,
            string personId,
            string facilityId,
            long fundingDelta,
            int progressDelta,
            string summary)
        {
            world.ResearchLedgerEntries.Add(new ResearchLedgerEntryState
            {
                Id = $"research_ledger.{world.AbsoluteDay}." +
                     $"{world.ResearchLedgerEntries.Count:D6}",
                Type = type,
                Day = world.AbsoluteDay,
                ResearchProjectId = projectId,
                TechnologyApplicationId = applicationId,
                KnowledgeDefinitionId = knowledgeId,
                TechnologyDefinitionId = technologyId,
                PersonId = personId,
                FacilityId = facilityId,
                FundingDelta = fundingDelta,
                ProgressDelta = progressDelta,
                Summary = summary
            });
        }

        private static PersonState FindPerson(WorldState world, string id)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == id)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {id}.");
        }

        private static FamilyState FindFamily(WorldState world, string id)
        {
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].Id == id)
                {
                    return world.Families[i];
                }
            }

            throw new InvalidOperationException($"Missing family {id}.");
        }

        private static VillageState FindVillage(WorldState world, string id)
        {
            for (var i = 0; i < world.Villages.Count; i++)
            {
                if (world.Villages[i].Id == id)
                {
                    return world.Villages[i];
                }
            }

            throw new InvalidOperationException($"Missing village {id}.");
        }

        private static VillageFacilityState FindFacility(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                if (world.VillageFacilities[i].Id == id)
                {
                    return world.VillageFacilities[i];
                }
            }

            throw new InvalidOperationException($"Missing facility {id}.");
        }
    }
}
