using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class PartitionedPopulationStore : IPermanentPopulationStore
    {
        private const int CoreFormatVersion = 1;
        private const int DetailFormatVersion = 1;
        private const string PointerFilename = "current.json";
        private static readonly byte[] CoreMagic = Encoding.ASCII.GetBytes("MOHPC001");
        private static readonly byte[] DetailMagic = Encoding.ASCII.GetBytes("MOHPD001");

        private static readonly JsonSerializerSettings JsonSettings =
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Include
            };

        private readonly string rootDirectory;
        private PopulationPackageManifest currentManifest;

        public PartitionedPopulationStore(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException(
                    "Population package root cannot be empty.",
                    nameof(rootDirectory));
            }

            this.rootDirectory = Path.GetFullPath(rootDirectory);
        }

        public PopulationPackageManifest CommitCheckpoint(
            PopulationCheckpoint checkpoint)
        {
            ValidateCheckpoint(checkpoint);
            Directory.CreateDirectory(rootDirectory);

            PopulationPackageManifest previous = null;
            var pointerPath = Path.Combine(rootDirectory, PointerFilename);
            if (File.Exists(pointerPath))
            {
                previous = OpenCurrent();
                if (!string.Equals(
                        previous.PackageId,
                        checkpoint.PackageId,
                        StringComparison.Ordinal) ||
                    previous.PartitionCount != checkpoint.PartitionCount)
                {
                    throw new InvalidOperationException(
                        "Population package identity and partition count are immutable.");
                }

                if (checkpoint.StorageRevision <= previous.StorageRevision)
                {
                    throw new InvalidOperationException(
                        "Population storage revision must increase monotonically.");
                }
            }

            var generationName = "generation-" +
                checkpoint.StorageRevision.ToString("D20");
            var generationsRoot = Path.Combine(rootDirectory, "generations");
            Directory.CreateDirectory(generationsRoot);
            var finalGeneration = Path.Combine(generationsRoot, generationName);
            if (Directory.Exists(finalGeneration))
            {
                throw new InvalidOperationException(
                    $"Population generation already exists: {generationName}.");
            }

            var stagingGeneration = Path.Combine(
                generationsRoot,
                ".staging-" + generationName + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stagingGeneration);

            try
            {
                var cores = CreateCoreBuckets(
                    checkpoint.People,
                    checkpoint.PartitionCount);
                var details = CreateDetailBuckets(
                    checkpoint.DetailExtensions,
                    checkpoint.PartitionCount);
                var manifest = new PopulationPackageManifest
                {
                    PackageId = checkpoint.PackageId,
                    PartitionCount = checkpoint.PartitionCount,
                    StorageRevision = checkpoint.StorageRevision,
                    PermanentPersonCount = checkpoint.People.Count,
                    DetailExtensionCount = checkpoint.DetailExtensions.Count
                };

                for (var partitionIndex = 0;
                     partitionIndex < checkpoint.PartitionCount;
                     partitionIndex++)
                {
                    cores[partitionIndex].Sort(CoreComparer.Instance);
                    details[partitionIndex].Sort(DetailComparer.Instance);
                    var coreFilename = $"core-{partitionIndex:D5}.bin";
                    var detailFilename = $"detail-{partitionIndex:D5}.bin";
                    var stagedCorePath = Path.Combine(stagingGeneration, coreFilename);
                    var stagedDetailPath = Path.Combine(stagingGeneration, detailFilename);
                    WriteCorePartition(
                        stagedCorePath,
                        partitionIndex,
                        cores[partitionIndex]);
                    WriteDetailPartition(
                        stagedDetailPath,
                        partitionIndex,
                        details[partitionIndex]);

                    var livingCount = 0;
                    for (var personIndex = 0;
                         personIndex < cores[partitionIndex].Count;
                         personIndex++)
                    {
                        if (cores[partitionIndex][personIndex].IsAlive)
                        {
                            livingCount++;
                        }
                    }

                    manifest.LivingPersonCount += livingCount;
                    manifest.Partitions.Add(new PopulationPartitionManifestEntry
                    {
                        PartitionIndex = partitionIndex,
                        PersonCount = cores[partitionIndex].Count,
                        LivingPersonCount = livingCount,
                        DetailExtensionCount = details[partitionIndex].Count,
                        CoreRelativePath = RelativeGenerationPath(
                            generationName,
                            coreFilename),
                        CoreLength = new FileInfo(stagedCorePath).Length,
                        CoreSha256 = ComputeSha256(stagedCorePath),
                        DetailRelativePath = RelativeGenerationPath(
                            generationName,
                            detailFilename),
                        DetailLength = new FileInfo(stagedDetailPath).Length,
                        DetailSha256 = ComputeSha256(stagedDetailPath)
                    });
                }

                var stagedManifestPath = Path.Combine(
                    stagingGeneration,
                    "manifest.json");
                File.WriteAllText(
                    stagedManifestPath,
                    JsonConvert.SerializeObject(manifest, JsonSettings),
                    new UTF8Encoding(false));
                var manifestSha256 = ComputeSha256(stagedManifestPath);

                Directory.Move(stagingGeneration, finalGeneration);
                var relativeManifestPath = RelativeGenerationPath(
                    generationName,
                    "manifest.json");
                CommitPointer(relativeManifestPath, manifestSha256);
                manifest.ManifestSha256 = manifestSha256;
                currentManifest = manifest;
                return manifest;
            }
            catch
            {
                if (Directory.Exists(stagingGeneration))
                {
                    Directory.Delete(stagingGeneration, true);
                }

                throw;
            }
        }

        public PopulationPackageManifest OpenCurrent()
        {
            if (currentManifest != null)
            {
                return currentManifest;
            }

            var pointerPath = Path.Combine(rootDirectory, PointerFilename);
            if (!File.Exists(pointerPath))
            {
                throw new InvalidOperationException(
                    "Population package has no committed current pointer.");
            }

            var pointer = JsonConvert.DeserializeObject<PopulationCurrentPointer>(
                File.ReadAllText(pointerPath, Encoding.UTF8),
                JsonSettings) ?? throw new InvalidOperationException(
                    "Population current pointer is empty.");
            if (pointer.FormatVersion != PopulationCurrentPointer.CurrentFormatVersion ||
                string.IsNullOrWhiteSpace(pointer.ManifestRelativePath) ||
                !IsSha256(pointer.ManifestSha256))
            {
                throw new InvalidOperationException(
                    "Population current pointer is invalid.");
            }

            var manifestPath = ResolvePackagePath(pointer.ManifestRelativePath);
            if (!File.Exists(manifestPath) ||
                !HashEquals(pointer.ManifestSha256, ComputeSha256(manifestPath)))
            {
                throw new InvalidOperationException(
                    "Population manifest is missing or failed checksum validation.");
            }

            var manifest = JsonConvert.DeserializeObject<PopulationPackageManifest>(
                File.ReadAllText(manifestPath, Encoding.UTF8),
                JsonSettings) ?? throw new InvalidOperationException(
                    "Population manifest is empty.");
            manifest.ManifestSha256 = pointer.ManifestSha256;
            ValidateManifest(manifest);
            currentManifest = manifest;
            return manifest;
        }

        public bool TryReadCore(
            string personId,
            out PermanentPersonCoreRecord person)
        {
            ValidatePersonId(personId);
            var manifest = OpenCurrent();
            var partitionIndex = PartitionFor(personId, manifest.PartitionCount);
            var entry = manifest.Partitions[partitionIndex];
            var path = ResolvePackagePath(entry.CoreRelativePath);
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                ReadAndValidateHeader(
                    reader,
                    CoreMagic,
                    CoreFormatVersion,
                    partitionIndex,
                    entry.PersonCount);
                for (var i = 0; i < entry.PersonCount; i++)
                {
                    var candidate = ReadCore(reader);
                    var comparison = string.CompareOrdinal(candidate.PersonId, personId);
                    if (comparison == 0)
                    {
                        person = candidate;
                        return true;
                    }

                    if (comparison > 0)
                    {
                        break;
                    }
                }
            }

            person = null;
            return false;
        }

        public bool TryReadDetail(string personId, out PersonState person)
        {
            ValidatePersonId(personId);
            var manifest = OpenCurrent();
            var partitionIndex = PartitionFor(personId, manifest.PartitionCount);
            var entry = manifest.Partitions[partitionIndex];
            var path = ResolvePackagePath(entry.DetailRelativePath);
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                ReadAndValidateHeader(
                    reader,
                    DetailMagic,
                    DetailFormatVersion,
                    partitionIndex,
                    entry.DetailExtensionCount);
                for (var i = 0; i < entry.DetailExtensionCount; i++)
                {
                    var id = reader.ReadString();
                    var extensionVersion = reader.ReadInt32();
                    _ = reader.ReadInt64();
                    var payloadLength = reader.ReadInt32();
                    if (extensionVersion !=
                            PersonDetailExtensionRecord.CurrentExtensionVersion ||
                        payloadLength < 0 ||
                        payloadLength > stream.Length - stream.Position - 32)
                    {
                        throw new InvalidOperationException(
                            $"Invalid detail extension for {id}.");
                    }

                    var expectedPayloadHash = reader.ReadBytes(32);
                    var payload = reader.ReadBytes(payloadLength);
                    if (expectedPayloadHash.Length != 32 ||
                        payload.Length != payloadLength ||
                        !ByteArraysEqual(expectedPayloadHash, ComputeSha256(payload)))
                    {
                        throw new InvalidOperationException(
                            $"Detail extension checksum failed for {id}.");
                    }

                    var comparison = string.CompareOrdinal(id, personId);
                    if (comparison == 0)
                    {
                        person = DeserializePerson(payload);
                        if (!string.Equals(person.Id, id, StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                $"Detail extension identity mismatch for {id}.");
                        }

                        return true;
                    }

                    if (comparison > 0)
                    {
                        break;
                    }
                }
            }

            person = null;
            return false;
        }

        public IReadOnlyList<PermanentPersonCoreRecord> LoadCorePartition(
            int partitionIndex)
        {
            var manifest = OpenCurrent();
            if (partitionIndex < 0 || partitionIndex >= manifest.PartitionCount)
            {
                throw new ArgumentOutOfRangeException(nameof(partitionIndex));
            }

            var entry = manifest.Partitions[partitionIndex];
            var result = new List<PermanentPersonCoreRecord>(entry.PersonCount);
            var path = ResolvePackagePath(entry.CoreRelativePath);
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                ReadAndValidateHeader(
                    reader,
                    CoreMagic,
                    CoreFormatVersion,
                    partitionIndex,
                    entry.PersonCount);
                for (var i = 0; i < entry.PersonCount; i++)
                {
                    result.Add(ReadCore(reader));
                }

                if (stream.Position != stream.Length)
                {
                    throw new InvalidOperationException(
                        $"Population core partition {partitionIndex} has trailing data.");
                }
            }

            return result;
        }

        internal static string SerializeDetailPerson(PersonState person)
        {
            return JsonConvert.SerializeObject(person, Formatting.None, JsonSettings);
        }

        private void ValidateManifest(PopulationPackageManifest manifest)
        {
            if (manifest.FormatVersion !=
                    PopulationPackageManifest.CurrentFormatVersion ||
                string.IsNullOrWhiteSpace(manifest.PackageId) ||
                manifest.PartitionCount <= 0 ||
                manifest.StorageRevision < 0 ||
                manifest.PermanentPersonCount < 0 ||
                manifest.LivingPersonCount < 0 ||
                manifest.LivingPersonCount > manifest.PermanentPersonCount ||
                manifest.DetailExtensionCount < 0 ||
                manifest.DetailExtensionCount > manifest.PermanentPersonCount ||
                manifest.Partitions == null ||
                manifest.Partitions.Count != manifest.PartitionCount)
            {
                throw new InvalidOperationException(
                    "Population package manifest metadata is invalid.");
            }

            long people = 0;
            long living = 0;
            long details = 0;
            for (var i = 0; i < manifest.Partitions.Count; i++)
            {
                var entry = manifest.Partitions[i];
                if (entry == null ||
                    entry.PartitionIndex != i ||
                    entry.PersonCount < 0 ||
                    entry.LivingPersonCount < 0 ||
                    entry.LivingPersonCount > entry.PersonCount ||
                    entry.DetailExtensionCount < 0 ||
                    entry.DetailExtensionCount > entry.PersonCount)
                {
                    throw new InvalidOperationException(
                        $"Population partition manifest entry {i} is invalid.");
                }

                ValidateManifestFile(
                    entry.CoreRelativePath,
                    entry.CoreLength,
                    entry.CoreSha256,
                    CoreMagic,
                    CoreFormatVersion,
                    i,
                    entry.PersonCount);
                ValidateManifestFile(
                    entry.DetailRelativePath,
                    entry.DetailLength,
                    entry.DetailSha256,
                    DetailMagic,
                    DetailFormatVersion,
                    i,
                    entry.DetailExtensionCount);
                people += entry.PersonCount;
                living += entry.LivingPersonCount;
                details += entry.DetailExtensionCount;
            }

            if (people != manifest.PermanentPersonCount ||
                living != manifest.LivingPersonCount ||
                details != manifest.DetailExtensionCount)
            {
                throw new InvalidOperationException(
                    "Population package manifest counts do not balance.");
            }
        }

        private void ValidateManifestFile(
            string relativePath,
            long expectedLength,
            string expectedHash,
            byte[] magic,
            int formatVersion,
            int partitionIndex,
            int expectedCount)
        {
            var path = ResolvePackagePath(relativePath);
            if (!File.Exists(path) ||
                new FileInfo(path).Length != expectedLength ||
                !IsSha256(expectedHash) ||
                !HashEquals(expectedHash, ComputeSha256(path)))
            {
                throw new InvalidOperationException(
                    $"Population partition file failed validation: {relativePath}.");
            }

            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                ReadAndValidateHeader(
                    reader,
                    magic,
                    formatVersion,
                    partitionIndex,
                    expectedCount);
            }
        }

        private string ResolvePackagePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) ||
                Path.IsPathRooted(relativePath))
            {
                throw new InvalidOperationException(
                    "Population package path must be relative.");
            }

            var normalizedRelative = relativePath.Replace('/', Path.DirectorySeparatorChar);
            var resolved = Path.GetFullPath(Path.Combine(rootDirectory, normalizedRelative));
            var rootWithSeparator = rootDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Population package path escapes its root.");
            }

            return resolved;
        }

        private void CommitPointer(string manifestRelativePath, string manifestSha256)
        {
            var pointer = new PopulationCurrentPointer
            {
                ManifestRelativePath = manifestRelativePath,
                ManifestSha256 = manifestSha256
            };
            var pointerPath = Path.Combine(rootDirectory, PointerFilename);
            var nextPath = Path.Combine(rootDirectory, "current.next.json");
            var previousPath = Path.Combine(rootDirectory, "current.previous.json");
            File.WriteAllText(
                nextPath,
                JsonConvert.SerializeObject(pointer, JsonSettings),
                new UTF8Encoding(false));
            using (var stream = new FileStream(
                       nextPath,
                       FileMode.Open,
                       FileAccess.ReadWrite,
                       FileShare.None))
            {
                stream.Flush(true);
            }

            if (!File.Exists(pointerPath))
            {
                File.Move(nextPath, pointerPath);
                return;
            }

            if (File.Exists(previousPath))
            {
                File.Delete(previousPath);
            }

            File.Replace(nextPath, pointerPath, previousPath, true);
        }

        private static void ValidateCheckpoint(PopulationCheckpoint checkpoint)
        {
            if (checkpoint == null)
            {
                throw new ArgumentNullException(nameof(checkpoint));
            }

            if (string.IsNullOrWhiteSpace(checkpoint.PackageId) ||
                checkpoint.PartitionCount <= 0 ||
                checkpoint.StorageRevision < 0 ||
                checkpoint.People == null ||
                checkpoint.DetailExtensions == null)
            {
                throw new InvalidOperationException(
                    "Population checkpoint metadata is invalid.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var coresById = new Dictionary<
                string,
                PermanentPersonCoreRecord>(StringComparer.Ordinal);
            for (var i = 0; i < checkpoint.People.Count; i++)
            {
                var person = checkpoint.People[i];
                ValidateCore(person);
                if (!ids.Add(person.PersonId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate permanent person {person.PersonId}.");
                }

                coresById.Add(person.PersonId, person);
            }

            var extensionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < checkpoint.DetailExtensions.Count; i++)
            {
                var extension = checkpoint.DetailExtensions[i];
                if (extension == null ||
                    extension.ExtensionVersion !=
                        PersonDetailExtensionRecord.CurrentExtensionVersion ||
                    extension.StorageRevision < 0 ||
                    extension.Person == null ||
                    !ids.Contains(extension.Person.Id) ||
                    !extensionIds.Add(extension.Person.Id))
                {
                    throw new InvalidOperationException(
                        "Population detail extension is invalid or duplicated.");
                }

                if (!coresById.TryGetValue(extension.Person.Id, out var core) ||
                    !core.Matches(extension.Person))
                {
                    throw new InvalidOperationException(
                        $"Detail extension core mismatch for {extension.Person.Id}.");
                }
            }
        }

        private static void ValidateCore(PermanentPersonCoreRecord person)
        {
            if (person == null)
            {
                throw new InvalidOperationException(
                    "Permanent person core cannot be null.");
            }

            _ = new StableId(person.PersonId);
            if (person.HealthBasisPoints < 0 || person.HealthBasisPoints > 10_000 ||
                person.LaborCapacityBasisPoints < 0 ||
                person.LaborCapacityBasisPoints > 10_000 ||
                person.NextIndependentEventDay < -1 ||
                person.LocalDutyUntilDay < -1 ||
                !Enum.IsDefined(typeof(PersonGender), person.Gender) ||
                !Enum.IsDefined(typeof(VillageOccupation), person.VillageOccupation) ||
                !Enum.IsDefined(typeof(LocalDutyKind), person.LocalDuty))
            {
                throw new InvalidOperationException(
                    $"Invalid permanent person core {person.PersonId}.");
            }
        }

        private static List<PermanentPersonCoreRecord>[] CreateCoreBuckets(
            List<PermanentPersonCoreRecord> people,
            int partitionCount)
        {
            var result = new List<PermanentPersonCoreRecord>[partitionCount];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new List<PermanentPersonCoreRecord>();
            }

            for (var i = 0; i < people.Count; i++)
            {
                var person = people[i];
                result[PartitionFor(person.PersonId, partitionCount)].Add(person);
            }

            return result;
        }

        private static List<PersonDetailExtensionRecord>[] CreateDetailBuckets(
            List<PersonDetailExtensionRecord> extensions,
            int partitionCount)
        {
            var result = new List<PersonDetailExtensionRecord>[partitionCount];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new List<PersonDetailExtensionRecord>();
            }

            for (var i = 0; i < extensions.Count; i++)
            {
                var extension = extensions[i];
                result[PartitionFor(extension.Person.Id, partitionCount)].Add(extension);
            }

            return result;
        }

        private static void WriteCorePartition(
            string path,
            int partitionIndex,
            List<PermanentPersonCoreRecord> people)
        {
            using (var stream = new FileStream(
                       path,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
            {
                WriteHeader(
                    writer,
                    CoreMagic,
                    CoreFormatVersion,
                    partitionIndex,
                    people.Count);
                for (var i = 0; i < people.Count; i++)
                {
                    WriteCore(writer, people[i]);
                }

                writer.Flush();
                stream.Flush(true);
            }
        }

        private static void WriteDetailPartition(
            string path,
            int partitionIndex,
            List<PersonDetailExtensionRecord> details)
        {
            using (var stream = new FileStream(
                       path,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
            {
                WriteHeader(
                    writer,
                    DetailMagic,
                    DetailFormatVersion,
                    partitionIndex,
                    details.Count);
                for (var i = 0; i < details.Count; i++)
                {
                    var detail = details[i];
                    var payload = Encoding.UTF8.GetBytes(
                        SerializeDetailPerson(detail.Person));
                    writer.Write(detail.Person.Id);
                    writer.Write(detail.ExtensionVersion);
                    writer.Write(detail.StorageRevision);
                    writer.Write(payload.Length);
                    writer.Write(ComputeSha256(payload));
                    writer.Write(payload);
                }

                writer.Flush();
                stream.Flush(true);
            }
        }

        private static void WriteCore(
            BinaryWriter writer,
            PermanentPersonCoreRecord person)
        {
            writer.Write(person.PersonId);
            WriteNullable(writer, person.DisplayName);
            WriteNullable(writer, person.CurrentLocationId);
            WriteNullable(writer, person.BirthLocationId);
            WriteNullable(writer, person.FamilyId);
            writer.Write(person.BirthDay);
            writer.Write(person.IsAlive);
            writer.Write(person.HealthBasisPoints);
            writer.Write((byte)person.Gender);
            WriteNullable(writer, person.FatherPersonId);
            WriteNullable(writer, person.MotherPersonId);
            WriteNullable(writer, person.SpousePersonId);
            writer.Write(person.CountsTowardPopulation);
            WriteNullable(writer, person.PopulationOriginLocationId);
            writer.Write((byte)person.VillageOccupation);
            writer.Write(person.LaborCapacityBasisPoints);
            writer.Write(person.NextIndependentEventDay);
            WriteNullable(writer, person.NextIndependentEventReason);
            writer.Write((byte)person.LocalDuty);
            writer.Write(person.LocalDutyUntilDay);
        }

        private static PermanentPersonCoreRecord ReadCore(BinaryReader reader)
        {
            var result = new PermanentPersonCoreRecord
            {
                PersonId = reader.ReadString(),
                DisplayName = ReadNullable(reader),
                CurrentLocationId = ReadNullable(reader),
                BirthLocationId = ReadNullable(reader),
                FamilyId = ReadNullable(reader),
                BirthDay = reader.ReadInt64(),
                IsAlive = reader.ReadBoolean(),
                HealthBasisPoints = reader.ReadInt32(),
                Gender = (PersonGender)reader.ReadByte(),
                FatherPersonId = ReadNullable(reader),
                MotherPersonId = ReadNullable(reader),
                SpousePersonId = ReadNullable(reader),
                CountsTowardPopulation = reader.ReadBoolean(),
                PopulationOriginLocationId = ReadNullable(reader),
                VillageOccupation = (VillageOccupation)reader.ReadByte(),
                LaborCapacityBasisPoints = reader.ReadInt32(),
                NextIndependentEventDay = reader.ReadInt64(),
                NextIndependentEventReason = ReadNullable(reader),
                LocalDuty = (LocalDutyKind)reader.ReadByte(),
                LocalDutyUntilDay = reader.ReadInt64()
            };
            ValidateCore(result);
            return result;
        }

        private static void WriteHeader(
            BinaryWriter writer,
            byte[] magic,
            int formatVersion,
            int partitionIndex,
            int count)
        {
            writer.Write(magic);
            writer.Write(formatVersion);
            writer.Write(partitionIndex);
            writer.Write(count);
        }

        private static void ReadAndValidateHeader(
            BinaryReader reader,
            byte[] expectedMagic,
            int expectedVersion,
            int expectedPartition,
            int expectedCount)
        {
            var magic = reader.ReadBytes(expectedMagic.Length);
            var version = reader.ReadInt32();
            var partition = reader.ReadInt32();
            var count = reader.ReadInt32();
            if (!ByteArraysEqual(magic, expectedMagic) ||
                version != expectedVersion ||
                partition != expectedPartition ||
                count != expectedCount)
            {
                throw new InvalidOperationException(
                    $"Population partition {expectedPartition} header is invalid.");
            }
        }

        private static void WriteNullable(BinaryWriter writer, string value)
        {
            writer.Write(value != null);
            if (value != null)
            {
                writer.Write(value);
            }
        }

        private static string ReadNullable(BinaryReader reader)
        {
            return reader.ReadBoolean() ? reader.ReadString() : null;
        }

        private static PersonState DeserializePerson(byte[] payload)
        {
            return JsonConvert.DeserializeObject<PersonState>(
                Encoding.UTF8.GetString(payload),
                JsonSettings) ?? throw new InvalidOperationException(
                    "Population detail payload did not contain a person.");
        }

        private static int PartitionFor(string personId, int partitionCount)
        {
            ValidatePersonId(personId);
            unchecked
            {
                var hash = 1469598103934665603UL;
                var bytes = Encoding.UTF8.GetBytes(personId);
                for (var i = 0; i < bytes.Length; i++)
                {
                    hash ^= bytes[i];
                    hash *= 1099511628211UL;
                }

                return (int)(hash % (ulong)partitionCount);
            }
        }

        private static void ValidatePersonId(string personId)
        {
            _ = new StableId(personId);
        }

        private static string RelativeGenerationPath(
            string generationName,
            string filename)
        {
            return "generations/" + generationName + "/" + filename;
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(stream));
            }
        }

        private static byte[] ComputeSha256(byte[] payload)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(payload);
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var result = new StringBuilder(bytes.Length * 2);
            for (var i = 0; i < bytes.Length; i++)
            {
                result.Append(bytes[i].ToString("x2"));
            }

            return result.ToString();
        }

        private static bool HashEquals(string expected, string actual)
        {
            return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
            {
                return false;
            }

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (!((c >= '0' && c <= '9') ||
                      (c >= 'a' && c <= 'f') ||
                      (c >= 'A' && c <= 'F')))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ByteArraysEqual(byte[] first, byte[] second)
        {
            if (first == null || second == null || first.Length != second.Length)
            {
                return false;
            }

            var difference = 0;
            for (var i = 0; i < first.Length; i++)
            {
                difference |= first[i] ^ second[i];
            }

            return difference == 0;
        }

        private sealed class CoreComparer : IComparer<PermanentPersonCoreRecord>
        {
            public static readonly CoreComparer Instance = new CoreComparer();

            public int Compare(
                PermanentPersonCoreRecord x,
                PermanentPersonCoreRecord y)
            {
                return string.CompareOrdinal(x.PersonId, y.PersonId);
            }
        }

        private sealed class DetailComparer : IComparer<PersonDetailExtensionRecord>
        {
            public static readonly DetailComparer Instance = new DetailComparer();

            public int Compare(
                PersonDetailExtensionRecord x,
                PersonDetailExtensionRecord y)
            {
                return string.CompareOrdinal(x.Person.Id, y.Person.Id);
            }
        }

        [Serializable]
        private sealed class PopulationCurrentPointer
        {
            public const int CurrentFormatVersion = 1;

            public int FormatVersion = CurrentFormatVersion;
            public string ManifestRelativePath;
            public string ManifestSha256;
        }
    }

    public static class PopulationStorageWorldAdapter
    {
        public static PopulationPackageManifest CommitInlineWorld(
            WorldState world,
            IPermanentPopulationStore store,
            string packageId,
            int partitionCount,
            long storageRevision)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (world.PopulationStorage.Mode ==
                    PopulationStorageMode.PartitionedPackage &&
                world.People.Count < world.PopulationStorage.PermanentPersonCount)
            {
                throw new InvalidOperationException(
                    "A partially materialized partitioned world cannot be recommitted " +
                    "through the inline coexistence adapter.");
            }

            world.Validate();
            var checkpoint = PopulationCheckpoint.FromInlineWorld(
                world,
                packageId,
                partitionCount,
                storageRevision);
            var manifest = store.CommitCheckpoint(checkpoint);
            world.PopulationStorage = manifest.ToDomainState();
            world.Validate();
            return manifest;
        }

        public static void ValidateAttachedPackage(
            WorldState world,
            IPermanentPopulationStore store)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            world.Validate();
            if (world.PopulationStorage.Mode !=
                PopulationStorageMode.PartitionedPackage)
            {
                throw new InvalidOperationException(
                    "World does not reference a partitioned population package.");
            }

            var manifest = store.OpenCurrent();
            var state = world.PopulationStorage;
            if (!string.Equals(state.PackageId, manifest.PackageId, StringComparison.Ordinal) ||
                state.PartitionCount != manifest.PartitionCount ||
                state.StorageRevision != manifest.StorageRevision ||
                state.PermanentPersonCount != manifest.PermanentPersonCount ||
                state.LivingPersonCount != manifest.LivingPersonCount ||
                state.DetailExtensionCount != manifest.DetailExtensionCount ||
                !string.Equals(
                    state.ManifestSha256,
                    manifest.ManifestSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "World population metadata does not match the attached package.");
            }

            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (!store.TryReadCore(person.Id, out var core) ||
                    !core.Matches(person) ||
                    !store.TryReadDetail(person.Id, out var detail) ||
                    !string.Equals(
                        PartitionedPopulationStore.SerializeDetailPerson(person),
                        PartitionedPopulationStore.SerializeDetailPerson(detail),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Materialized person {person.Id} does not match the package.");
                }
            }
        }

        public static void ReturnToInlineMode(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.PopulationStorage.Mode ==
                    PopulationStorageMode.PartitionedPackage &&
                world.People.Count != world.PopulationStorage.PermanentPersonCount)
            {
                throw new InvalidOperationException(
                    "Cannot return to inline mode without materializing every person.");
            }

            world.PopulationStorage = PopulationStorageState.CreateInline(world.People);
            world.Validate();
        }
    }

    public sealed class PopulationResidencySession
    {
        private readonly IPermanentPopulationStore store;
        private readonly Dictionary<string, PersonState> hotPeople =
            new Dictionary<string, PersonState>(StringComparer.Ordinal);

        public PopulationResidencySession(IPermanentPopulationStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            _ = store.OpenCurrent();
        }

        public int HotCount => hotPeople.Count;

        public PersonState Promote(string personId)
        {
            if (hotPeople.TryGetValue(personId, out var existing))
            {
                return existing;
            }

            if (!store.TryReadCore(personId, out var core))
            {
                throw new InvalidOperationException(
                    $"Unknown permanent person {personId}.");
            }

            if (!store.TryReadDetail(personId, out var detail))
            {
                throw new InvalidOperationException(
                    $"Person {personId} has no persisted detail extension.");
            }

            if (!core.Matches(detail))
            {
                throw new InvalidOperationException(
                    $"Person {personId} detail does not match permanent core.");
            }

            hotPeople.Add(personId, detail);
            return detail;
        }

        public bool TryGetHot(string personId, out PersonState person)
        {
            return hotPeople.TryGetValue(personId, out person);
        }

        public void DemoteUnchanged(string personId)
        {
            if (!hotPeople.TryGetValue(personId, out var hot))
            {
                return;
            }

            if (!store.TryReadDetail(personId, out var persisted) ||
                !string.Equals(
                    PartitionedPopulationStore.SerializeDetailPerson(hot),
                    PartitionedPopulationStore.SerializeDetailPerson(persisted),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Person {personId} changed while hot; commit a new checkpoint " +
                    "before demotion.");
            }

            hotPeople.Remove(personId);
        }
    }
}
