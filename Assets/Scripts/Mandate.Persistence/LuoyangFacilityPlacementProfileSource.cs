using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Newtonsoft.Json.Linq;

namespace Mandate.Persistence
{
    public sealed class LuoyangFacilityPlacementProfileSource
    {
        public LuoyangFacilityPlacementProfileSource(string worldMapRoot)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("World map root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            PackagePath = Path.Combine(Root,
                LuoyangCountyPlanningIds.ProfileDirectoryName,
                LuoyangCountyPlanningIds.ProfileFileName);
            if (!File.Exists(PackagePath))
                throw new FileNotFoundException(
                    "Luoyang Facility placement profile package is missing.",
                    PackagePath);

            var root = JObject.Parse(File.ReadAllText(PackagePath));
            Catalog = new FacilityPlacementProfileCatalog(
                Text(root, "schema_id"), Text(root, "package_id"),
                Text(root, "source_layout_package_id"),
                Text(root, "status_id"), Array(root, "profiles")
                    .Select(ReadProfile).ToArray());
            ValidateAgainstExistingModels();
        }

        public string Root { get; }
        public string PackagePath { get; }
        public FacilityPlacementProfileCatalog Catalog { get; }

        private FacilityPlacementProfile ReadProfile(JToken item)
        {
            return new FacilityPlacementProfile(
                Text(item, "profile_id"),
                Text(item, "facility_definition_id"),
                Text(item, "blueprint_id"), Text(item, "model_id"),
                Text(item, "display_name"),
                Integer(item, "footprint_width_centimetres"),
                Integer(item, "footprint_length_centimetres"),
                Integer(item, "height_centimetres"),
                Array(item, "allowed_rotation_quarter_turns")
                    .Select(value => value.Value<int>()).ToArray(),
                Array(item, "entrance_offsets").Select(value =>
                    new FacilityEntranceOffsetDefinition(
                        Text(value, "entrance_id"),
                        Integer(value, "east_offset_centimetres"),
                        Integer(value, "north_offset_centimetres"),
                        EnumValue<PlanningCellDirection>(value,
                            "outward_direction_id"),
                        Boolean(value, "primary"))).ToArray(),
                EnumArray<PlanningTerrainClass>(item,
                    "allowed_terrain_ids"),
                EnumArray<PlanningTerrainClass>(item,
                    "forbidden_terrain_ids"),
                checked((byte)Integer(item, "maximum_slope_basis")),
                EnumValue<FacilityRoadAccessRequirement>(item,
                    "road_access_requirement_id"),
                Text(item, "minimum_road_class_id"),
                Integer(item,
                    "maximum_entrance_to_road_distance_centimetres"),
                Boolean(item, "allow_water_overlap"),
                Boolean(item, "allow_fortification_overlap"),
                Boolean(item, "allow_existing_facility_overlap"),
                Integer(item, "required_clearance_centimetres"),
                Text(item, "placement_category_id"),
                Array(item, "availability_ids").Select(value =>
                    value.Value<string>() ?? string.Empty).ToArray(),
                Text(item, "provenance_id"));
        }

        private void ValidateAgainstExistingModels()
        {
            if (Catalog.Profiles.Count != 6)
                throw new InvalidDataException(
                    "Luoyang planning V2 requires six audited candidates: " +
                    "five player-facing families plus the retained beacon " +
                    "authority regression fixture.");
            var models = new LuoyangFacilityModelCoverageSource(Root)
                .CombinedCatalog.Models.ToDictionary(value => value.ModelId,
                    StringComparer.Ordinal);
            foreach (var profile in Catalog.Profiles)
            {
                if (!models.TryGetValue(profile.ModelId, out var model) ||
                    !string.Equals(model.FacilityDefinitionId,
                        profile.FacilityDefinitionId,
                        StringComparison.Ordinal) ||
                    !string.Equals(model.SourceBuildContractId,
                        profile.BlueprintId, StringComparison.Ordinal) ||
                    !new HashSet<string>(model.AvailabilityIds,
                        StringComparer.Ordinal).SetEquals(
                            profile.AvailabilityIds))
                    throw new InvalidDataException(
                        "Placement profile is not backed by its existing " +
                        "Facility/model/build contract: " + profile.ProfileId);
            }
        }

        private static IReadOnlyList<T> EnumArray<T>(JToken item,
            string property) where T : struct => Array(item, property)
            .Select(value => EnumText<T>(value.Value<string>())).ToArray();

        private static T EnumValue<T>(JToken item, string property)
            where T : struct => EnumText<T>(Text(item, property));

        private static T EnumText<T>(string value) where T : struct
        {
            if (!Enum.TryParse(value, false, out T result) ||
                !Enum.IsDefined(typeof(T), result))
                throw new InvalidDataException("Unknown " + typeof(T).Name +
                    " value: " + value);
            return result;
        }

        private static JArray Array(JToken item, string property) =>
            item[property] as JArray ?? throw new InvalidDataException(
                "Missing array: " + property);

        private static string Text(JToken item, string property)
        {
            var value = item[property]?.Value<string>();
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Missing text: " + property);
            return value;
        }

        private static int Integer(JToken item, string property) =>
            item[property]?.Value<int>() ?? throw new InvalidDataException(
                "Missing integer: " + property);

        private static bool Boolean(JToken item, string property) =>
            item[property]?.Value<bool>() ?? throw new InvalidDataException(
                "Missing boolean: " + property);
    }
}
