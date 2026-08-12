using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class HanNationalPopulationDatasetReader : IHanNationalPopulationSnapshotSource
    {
        public const string ExpectedSchema = "mandate.han-national-population-dataset.v1";
        public const string ExpectedYearSchema = "mandate.han-national-population-year.v1";
        private readonly string rootPath;
        private int cachedYear = int.MinValue;
        private HanPopulationYearSnapshot cachedSnapshot;

        public HanNationalPopulationDatasetReader(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("A dataset root is required.", nameof(rootPath));
            this.rootPath = Path.GetFullPath(rootPath);
            Manifest = JsonConvert.DeserializeObject<HanNationalPopulationManifest>(
                File.ReadAllText(Path.Combine(this.rootPath, "manifest.json"), Encoding.UTF8));
            ValidateManifest();
        }

        public HanNationalPopulationManifest Manifest { get; }

        public HanPopulationYearSnapshot LoadPopulationSnapshot(int year)
        {
            if (year < Manifest.YearStart || year > Manifest.YearEnd) throw new ArgumentOutOfRangeException(nameof(year));
            if (cachedSnapshot != null && cachedYear == year) return cachedSnapshot;
            var relative = Manifest.SnapshotPathTemplate.Replace("{year}", year.ToString(CultureInfo.InvariantCulture));
            var fullPath = ResolveInsideRoot(relative);
            var snapshot = JsonConvert.DeserializeObject<HanPopulationYearSnapshot>(File.ReadAllText(fullPath, Encoding.UTF8));
            ValidateSnapshot(snapshot, year);
            cachedYear = year;
            cachedSnapshot = snapshot;
            return snapshot;
        }

        public HanPopulationYearSnapshot LoadScenarioSnapshot(string scenarioId)
        {
            if (string.IsNullOrWhiteSpace(scenarioId)) throw new ArgumentException("A scenario ID is required.", nameof(scenarioId));
            var fileName = scenarioId + ".json";
            var path = ResolveInsideRoot(Path.Combine("scenarios", fileName));
            var scenario = JsonConvert.DeserializeObject<ScenarioReference>(File.ReadAllText(path, Encoding.UTF8));
            if (!string.Equals(scenario.ScenarioId, scenarioId, StringComparison.Ordinal)
                || !string.Equals(scenario.Derivation, "direct_reference_to_annual_population_timeline", StringComparison.Ordinal))
                throw new InvalidDataException("Scenario population snapshot does not reference the formal annual timeline.");
            return LoadPopulationSnapshot(scenario.Year);
        }

        public IReadOnlyList<string> ValidatePackageFiles()
        {
            var failures = new List<string>();
            foreach (var item in Manifest.Files)
            {
                var path = ResolveInsideRoot(item.Path);
                if (!File.Exists(path)) { failures.Add(item.Path + ":missing"); continue; }
                if (new FileInfo(path).Length != item.Bytes) { failures.Add(item.Path + ":size"); continue; }
                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                {
                    var actual = string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
                    if (!string.Equals(actual, item.Sha256, StringComparison.Ordinal)) failures.Add(item.Path + ":sha256");
                }
            }
            return failures;
        }

        private void ValidateManifest()
        {
            if (Manifest == null
                || !string.Equals(Manifest.Schema, ExpectedSchema, StringComparison.Ordinal)
                || Manifest.FormatVersion != 1
                || Manifest.YearStart != 135 || Manifest.YearEnd != 260 || Manifest.YearCount != 126
                || Manifest.ProvinceCount != 13 || Manifest.RegionCount != 105 || Manifest.CountyCount != 1182
                || Manifest.CountyYearRecordCount != 148932 || Manifest.ScenarioCount != 13
                || Manifest.NationalAnchor140Registered != 49150220
                || Manifest.NationalAnchor157Registered != 56486856
                || Manifest.PermanentPersonsGenerated != 0
                || string.IsNullOrWhiteSpace(Manifest.SnapshotPathTemplate))
                throw new InvalidDataException("Unsupported HAN-135-260 population dataset contract.");
        }

        private void ValidateSnapshot(HanPopulationYearSnapshot snapshot, int expectedYear)
        {
            if (snapshot == null || !string.Equals(snapshot.Schema, ExpectedYearSchema, StringComparison.Ordinal)
                || snapshot.Year != expectedYear || !string.Equals(snapshot.SnapshotMoment, "YEAR_START", StringComparison.Ordinal)
                || snapshot.National == null || snapshot.Conservation == null
                || snapshot.Provinces == null || snapshot.Provinces.Count != Manifest.ProvinceCount
                || snapshot.Regions == null || snapshot.Regions.Count != Manifest.RegionCount
                || snapshot.Counties == null || snapshot.Counties.Count != Manifest.CountyCount
                || !string.Equals(snapshot.Conservation.Status, "PASS", StringComparison.Ordinal))
                throw new InvalidDataException("Population year shard violates the formal contract.");

            var actual = snapshot.National.ModeledActualPopulationStart;
            var registered = snapshot.National.RegisteredPopulationStart;
            if (snapshot.Provinces.Sum(row => row.ModeledActualPopulation) != actual
                || snapshot.Regions.Sum(row => row.ModeledActualPopulation) != actual
                || snapshot.Counties.Sum(row => row.ModeledActualPopulation) != actual
                || snapshot.Provinces.Sum(row => row.RegisteredPopulation) != registered
                || snapshot.Regions.Sum(row => row.RegisteredPopulation) != registered
                || snapshot.Counties.Sum(row => row.RegisteredPopulation) != registered
                || snapshot.Regions.Sum(row => row.NetMigration) != 0)
                throw new InvalidDataException("Population year shard does not conserve people.");
        }

        private string ResolveInsideRoot(string relativePath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
            var prefix = rootPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? rootPath : rootPath + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Population package path escapes its root.");
            return fullPath;
        }

        [Serializable]
        private sealed class ScenarioReference
        {
            [JsonProperty("scenario_id")] public string ScenarioId { get; set; }
            [JsonProperty("year")] public int Year { get; set; }
            [JsonProperty("derivation")] public string Derivation { get; set; }
        }
    }
}
