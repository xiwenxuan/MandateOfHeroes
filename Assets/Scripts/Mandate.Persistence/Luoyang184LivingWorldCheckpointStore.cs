using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class Luoyang184LivingWorldCheckpointResult
    {
        public string CheckpointPath;
        public string ManifestPath;
        public string Sha256;
        public long Bytes;
    }

    /// <summary>
    /// Writable derived overlay for the protected Luoyang source package.
    /// The gzip JSON format is deliberately explicit and migration-gated; the
    /// protected population/facility files are never opened for writing.
    /// </summary>
    public sealed class Luoyang184LivingWorldCheckpointStore
    {
        public const string CheckpointFileName = "living_world_v1.json.gz";
        public const string ManifestFileName = "manifest.json";

        public Luoyang184LivingWorldCheckpointResult Save(
            Luoyang184LivingWorldRuntimeState runtime,
            string generationDirectory)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (string.IsNullOrWhiteSpace(generationDirectory))
                throw new ArgumentException("A generation directory is required.",
                    nameof(generationDirectory));
            var root = Path.GetFullPath(generationDirectory);
            Directory.CreateDirectory(root);
            var checkpoint = Path.Combine(root, CheckpointFileName);
            var temporary = checkpoint + ".tmp";
            if (File.Exists(temporary)) File.Delete(temporary);
            using (var file = new FileStream(temporary, FileMode.CreateNew,
                       FileAccess.Write, FileShare.None))
            using (var gzip = new GZipStream(file, CompressionLevel.Optimal,
                       false))
            using (var text = new StreamWriter(gzip, new UTF8Encoding(false)))
            using (var json = new JsonTextWriter(text))
            {
                var serializer = JsonSerializer.Create(new JsonSerializerSettings
                {
                    Formatting = Formatting.None,
                    NullValueHandling = NullValueHandling.Include
                });
                serializer.Serialize(json, runtime);
            }
            if (File.Exists(checkpoint)) File.Delete(checkpoint);
            File.Move(temporary, checkpoint);
            var digest = Sha256(checkpoint);
            var manifest = new
            {
                schema = "mandate.luoyang-184.living-world-checkpoint.v1",
                format_version = runtime.Version,
                source_package_id = runtime.SourcePackageId,
                protected_package_digest = runtime.ProtectedPackageDigest,
                absolute_day = runtime.AbsoluteDay,
                person_count = runtime.Workforce.Count,
                household_count = runtime.Households.Count,
                facility_count = runtime.Facilities.Count,
                crop_count = runtime.Crops.Count,
                inventory_count = runtime.Inventories.Count,
                checkpoint_file = CheckpointFileName,
                checkpoint_bytes = new FileInfo(checkpoint).Length,
                checkpoint_sha256 = digest
            };
            var manifestPath = Path.Combine(root, ManifestFileName);
            File.WriteAllText(manifestPath,
                JsonConvert.SerializeObject(manifest, Formatting.Indented),
                new UTF8Encoding(false));
            return new Luoyang184LivingWorldCheckpointResult
            {
                CheckpointPath = checkpoint,
                ManifestPath = manifestPath,
                Sha256 = digest,
                Bytes = new FileInfo(checkpoint).Length
            };
        }

        public Luoyang184LivingWorldRuntimeState Load(string checkpointPath)
        {
            if (string.IsNullOrWhiteSpace(checkpointPath))
                throw new ArgumentException("A checkpoint path is required.",
                    nameof(checkpointPath));
            using (var file = File.OpenRead(Path.GetFullPath(checkpointPath)))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress,
                       false))
            using (var text = new StreamReader(gzip, Encoding.UTF8, true))
            using (var json = new JsonTextReader(text))
            {
                var serializer = JsonSerializer.CreateDefault();
                return serializer.Deserialize<Luoyang184LivingWorldRuntimeState>(json)
                       ?? throw new InvalidDataException(
                           "Living-world checkpoint contained no runtime state.");
            }
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
            {
                var digest = hash.ComputeHash(stream);
                var builder = new StringBuilder(digest.Length * 2);
                foreach (var value in digest) builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
        }
    }

    public static class Luoyang184LivingWorldEvidenceStore
    {
        public static void Write(string path, object evidence)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("An evidence path is required.",
                    nameof(path));
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
            File.WriteAllText(fullPath,
                JsonConvert.SerializeObject(evidence, Formatting.Indented),
                new UTF8Encoding(false));
        }
    }
}
