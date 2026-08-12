using System;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class NeuralPolicyModelReader
    {
        private readonly JsonSerializerSettings _settings =
            new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Include
            };

        public NeuralPolicyModelDefinition Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "Neural model path is required.", nameof(path));
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Neural policy model was not found.", path);
            }

            var model = JsonConvert.DeserializeObject<
                NeuralPolicyModelDefinition>(
                    File.ReadAllText(path), _settings) ??
                throw new InvalidDataException(
                    "Neural policy model JSON is empty.");
            LivingWorldRuntimeRules.ValidateNeuralModel(model);
            return model;
        }
    }
}
