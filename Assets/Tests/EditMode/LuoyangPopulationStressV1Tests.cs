using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class LuoyangPopulationStressV1Tests
    {
        private static string RuntimeRoot => Path.Combine(Application.dataPath, "StreamingAssets", "WorldMap", "LuoyangPopulationStressV1");
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        [Test]
        public void ManifestContainsFiveIsolatedProfilesAndProtectedScale()
        {
            var reader = new LuoyangPopulationStressPrototypeReader(RuntimeRoot);
            Assert.That(reader.Manifest.CellSizeMetres, Is.EqualTo(2_000));
            Assert.That(reader.Manifest.HistoricalScenarioPopulation, Is.EqualTo(20_542));
            Assert.That(reader.Profiles.Select(profile => profile.PersonCount).OrderBy(value => value),
                Is.EqualTo(new[] { 20_542, 50_000, 100_000, 250_000, 500_000 }));
            Assert.That(reader.Manifest.Profiles.Select(entry => entry.SummaryRelativePath).Distinct().Count(), Is.EqualTo(5));
        }

        [Test]
        public void FiveHundredThousandProfileHasAddressablePermanentPersons()
        {
            var path = Path.Combine(ProjectRoot, "MapData", "LuoyangPopulationStress_V1", "profiles", "profile_500000", "persons.bin");
            using var reader = new LuoyangStressPersonBinaryReader(path, "Profile_500000_Stress");
            Assert.That(reader.PersonCount, Is.EqualTo(500_000));
            Assert.That(reader.Read(0).PersonId, Is.EqualTo("person.luoyang.v1.recommended.00000001"));
            Assert.That(reader.Read(20_541).PersonId, Is.EqualTo("person.luoyang.v1.recommended.00020542"));
            Assert.That(reader.Read(249_999).PersonId, Does.Contain("Profile_500000_Stress"));
            Assert.That(Enum.IsDefined(typeof(StressSimulationTier), reader.Read(499_999).SimulationTier), Is.True);
        }

        [Test]
        public void AdaptiveModeUsesRealCellsAndBoundedPresentationLod()
        {
            var reader = new LuoyangPopulationStressPrototypeReader(RuntimeRoot);
            Assert.That(reader.TryGetProfile("Profile_250000_Stress", out var profile), Is.True);
            Assert.That(profile.AdaptiveMode.FacilitiesAdded, Is.GreaterThan(0));
            Assert.That(profile.AdaptiveMode.FacilityCount,
                Is.EqualTo(profile.AdaptiveMode.OccupiedFacilityCells));
            Assert.That(profile.AdaptiveMode.SimulationDays, Is.EqualTo(365));
            Assert.That(profile.AdaptiveMode.SimulationStatus, Does.StartWith("Completed"));
            Assert.That(profile.Lod.PermanentPersonCount, Is.EqualTo(250_000));
            Assert.That(profile.Lod.HighFrequencyActorCount, Is.LessThanOrEqualTo(256));
            Assert.That(profile.FixedMode.FacilitiesAdded, Is.Zero);
        }

        [Test]
        public void SaveLoadMetricsAndHistoricalBaselineRemainExplicit()
        {
            var reader = new LuoyangPopulationStressPrototypeReader(RuntimeRoot);
            foreach (var profile in reader.Profiles)
            {
                Assert.That(profile.SaveLoad.RoundTripConsistent, Is.True);
                Assert.That(profile.SaveLoad.SaveSizeBytes, Is.EqualTo(32L + profile.PersonCount * 72L));
                Assert.That(profile.HistoricalScenarioPopulation, Is.EqualTo(20_542));
                Assert.That(profile.FixedMode.HousedPopulation + profile.FixedMode.UnhousedPopulation,
                    Is.EqualTo(profile.PersonCount));
                Assert.That(profile.AdaptiveMode.HousedPopulation + profile.AdaptiveMode.UnhousedPopulation,
                    Is.EqualTo(profile.PersonCount));
            }
        }
    }
}
