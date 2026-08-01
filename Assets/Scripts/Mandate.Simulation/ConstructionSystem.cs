using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class ConstructionContributionResult
    {
        public int ProgressAdded;
        public bool Completed;
        public string Summary;
    }

    public sealed class ConstructionSystem
    {
        public static LocationFeature RecommendFeature(
            LocationState location,
            MapPerspective perspective)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            switch (perspective)
            {
                case MapPerspective.Military:
                    return FirstMissingFeature(
                        location,
                        LocationFeature.Fortification,
                        LocationFeature.Garrison);
                case MapPerspective.Administration:
                    return FirstMissingFeature(
                        location,
                        LocationFeature.Government,
                        LocationFeature.RelayStation,
                        LocationFeature.Farmland,
                        LocationFeature.Workshop,
                        LocationFeature.Clinic);
                case MapPerspective.Commerce:
                    return FirstMissingFeature(
                        location,
                        LocationFeature.Market,
                        LocationFeature.Workshop,
                        LocationFeature.RelayStation,
                        location.Kind == LocationKind.Port ||
                        location.Terrain == TerrainKind.Riverland
                            ? LocationFeature.Harbor
                            : LocationFeature.None);
                case MapPerspective.Medicine:
                    return FirstMissingFeature(
                        location,
                        LocationFeature.Clinic,
                        LocationFeature.RelayStation,
                        LocationFeature.Temple);
                default:
                    return FirstMissingFeature(
                        location,
                        LocationFeature.Market,
                        LocationFeature.RelayStation,
                        LocationFeature.Workshop,
                        LocationFeature.Clinic);
            }
        }

        public ConstructionProjectState StartProject(
            WorldState world,
            StableId sponsorPersonId,
            StableId locationId,
            LocationFeature targetFeature)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            ValidateConstructibleFeature(targetFeature);
            var sponsor = FindPerson(world, sponsorPersonId.Value);
            var location = FindLocation(world, locationId.Value);
            EnsurePersonCanWorkAtLocation(world, sponsor, location);
            if ((location.Features & targetFeature) != 0)
            {
                throw new InvalidOperationException(
                    $"{location.DisplayName}已经拥有{FeatureName(targetFeature)}。");
            }

            for (var i = 0; i < world.ConstructionProjects.Count; i++)
            {
                var existing = world.ConstructionProjects[i];
                if (existing.LocationId == location.Id &&
                    existing.TargetFeature == targetFeature)
                {
                    throw new InvalidOperationException(
                        $"{location.DisplayName}已有相同建设项目。");
                }
            }

            var project = new ConstructionProjectState
            {
                Id =
                    $"construction.{location.Id}.{(ushort)targetFeature}." +
                    $"{world.ConstructionProjects.Count}",
                DisplayName =
                    $"{location.DisplayName}·{FeatureName(targetFeature)}建设",
                LocationId = location.Id,
                TargetFeature = targetFeature,
                SponsorPersonId = sponsor.Id,
                StartedDay = world.AbsoluteDay,
                RequiredProgress = RequiredProgress(targetFeature),
                Progress = 0,
                MoneyInvested = 0,
                IsCompleted = false,
                CompletedDay = -1
            };
            world.ConstructionProjects.Add(project);
            world.Validate();
            return project;
        }

        public ConstructionContributionResult Contribute(
            WorldState world,
            StableId projectId,
            StableId personId,
            int money,
            int labor)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            if (money < 0 || labor < 0 || money == 0 && labor == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(money),
                    "投入的资金和劳力不能均为零或为负数。");
            }

            var project = FindProject(world, projectId.Value);
            if (project.IsCompleted)
            {
                throw new InvalidOperationException("建设项目已经完成。");
            }

            var person = FindPerson(world, personId.Value);
            var location = FindLocation(world, project.LocationId);
            EnsurePersonCanWorkAtLocation(world, person, location);
            if (person.Wealth < money)
            {
                throw new InvalidOperationException("人物财富不足以完成本次投入。");
            }

            var progressAdded = checked(labor + money / 5);
            if (progressAdded <= 0)
            {
                throw new InvalidOperationException("本次投入不足以产生建设进度。");
            }

            person.Wealth -= money;
            project.MoneyInvested = checked(project.MoneyInvested + money);
            var remaining = project.RequiredProgress - project.Progress;
            progressAdded = Math.Min(progressAdded, remaining);
            project.Progress += progressAdded;
            if (project.Progress == project.RequiredProgress)
            {
                project.IsCompleted = true;
                project.CompletedDay = world.AbsoluteDay;
                location.Features |= project.TargetFeature;
            }

            world.Validate();
            return new ConstructionContributionResult
            {
                ProgressAdded = progressAdded,
                Completed = project.IsCompleted,
                Summary = project.IsCompleted
                    ? $"{project.DisplayName}已经完工。"
                    : $"{project.DisplayName}增加{progressAdded}点进度。"
            };
        }

        public static int RequiredProgress(LocationFeature feature)
        {
            switch (feature)
            {
                case LocationFeature.Farmland:
                case LocationFeature.RelayStation:
                    return 80;
                case LocationFeature.Market:
                case LocationFeature.Clinic:
                case LocationFeature.Temple:
                    return 100;
                case LocationFeature.Government:
                case LocationFeature.Workshop:
                    return 120;
                case LocationFeature.Garrison:
                    return 140;
                case LocationFeature.Harbor:
                    return 160;
                case LocationFeature.Fortification:
                    return 180;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(feature),
                        feature,
                        "不支持的建设设施。");
            }
        }

        public static string FeatureName(LocationFeature feature)
        {
            switch (feature)
            {
                case LocationFeature.Government:
                    return "官署";
                case LocationFeature.Market:
                    return "市场";
                case LocationFeature.Garrison:
                    return "驻军设施";
                case LocationFeature.Farmland:
                    return "农田";
                case LocationFeature.Workshop:
                    return "工坊";
                case LocationFeature.Clinic:
                    return "医馆";
                case LocationFeature.Temple:
                    return "寺观";
                case LocationFeature.RelayStation:
                    return "驿站";
                case LocationFeature.Harbor:
                    return "港池";
                case LocationFeature.Fortification:
                    return "城防";
                default:
                    return feature.ToString();
            }
        }

        private static void EnsurePersonCanWorkAtLocation(
            WorldState world,
            PersonState person,
            LocationState location)
        {
            if (person.LocationId != location.Id)
            {
                throw new InvalidOperationException("人物必须在建设地点才能投入。");
            }

            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].PersonId == person.Id)
                {
                    throw new InvalidOperationException("旅途中不能参与地方建设。");
                }
            }
        }

        private static LocationFeature FirstMissingFeature(
            LocationState location,
            params LocationFeature[] features)
        {
            for (var i = 0; i < features.Length; i++)
            {
                if (features[i] != LocationFeature.None &&
                    (location.Features & features[i]) == 0)
                {
                    return features[i];
                }
            }

            return LocationFeature.None;
        }

        private static void ValidateConstructibleFeature(LocationFeature feature)
        {
            var value = (ushort)feature;
            if (value == 0 ||
                (value & (value - 1)) != 0 ||
                (feature & ~LocationFeature.All) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(feature),
                    feature,
                    "建设项目必须对应一种有效设施。");
            }
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

            throw new InvalidOperationException($"Missing person {personId}.");
        }

        private static LocationState FindLocation(
            WorldState world,
            string locationId)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == locationId)
                {
                    return world.Locations[i];
                }
            }

            throw new InvalidOperationException($"Missing location {locationId}.");
        }

        private static ConstructionProjectState FindProject(
            WorldState world,
            string projectId)
        {
            for (var i = 0; i < world.ConstructionProjects.Count; i++)
            {
                if (world.ConstructionProjects[i].Id == projectId)
                {
                    return world.ConstructionProjects[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing construction project {projectId}.");
        }
    }
}
