from __future__ import annotations

import json
from collections import Counter
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
ROOT = REPO / "MapData" / "LuoyangWorld_V1"
HAN = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "HanWorldV1"
UNITY = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "LuoyangWorldV1"
PROFILES = ("low", "recommended", "high")
GRID_SCHEMA = "hanworld.square-grid.v1"


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def read_jsonl(path: Path):
    with path.open("r", encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def validate_profile(profile: str, expected: dict, definitions: dict) -> dict:
    persons = read_jsonl(ROOT / "population" / f"{profile}_persons.jsonl")
    households = read_jsonl(ROOT / "population" / f"{profile}_households.jsonl")
    layout = read_json(ROOT / "layouts" / f"{profile}_layout.json")
    facilities = layout["facilities"]
    forces = layout["forces"]
    person_ids = {item["person_id"] for item in persons}
    household_ids = {item["household_id"] for item in households}
    facility_ids = {item["facility_id"] for item in facilities}
    require(len(person_ids) == len(persons) == expected["total_persons"], f"{profile}: Person count or ID uniqueness")
    require(len(household_ids) == len(households) == expected["total_households"], f"{profile}: Household count or ID uniqueness")
    require(len(facility_ids) == len(facilities), f"{profile}: Facility ID uniqueness")
    require(len({item["cell_id64"] for item in facilities}) == len(facilities), f"{profile}: one Facility per Cell")
    require(len({item["cell_id64"] for item in forces}) == len(forces), f"{profile}: one Force per Cell")
    require(all(item["grid_schema_version"] == GRID_SCHEMA for item in facilities), f"{profile}: Facility grid schema")
    require(all(item["cell_id64"] == item["grid_y"] * 3314 + item["grid_x"] for item in facilities), f"{profile}: CellId64 formula")

    household_by_id = {item["household_id"]: item for item in households}
    facility_by_id = {item["facility_id"]: item for item in facilities}
    for person in persons:
        require(person["household_id"] in household_ids, f"{profile}: dangling Person Household")
        require(person["current_cell_id64"] is not None, f"{profile}: missing Person CurrentCell")
        require(person["residence_facility_id"] in facility_ids, f"{profile}: missing Person residence")
        require(person["profession_id"] and person["current_activity"], f"{profile}: missing profession/activity")
        require(isinstance(person["labor_eligible"], bool), f"{profile}: missing labor eligibility")
        if person["work_facility_id"] is not None:
            require(person["work_facility_id"] in facility_ids, f"{profile}: dangling work Facility")
        for relation in person["parent_person_ids"]:
            require(relation in person_ids, f"{profile}: dangling parent")
        if person["spouse_person_id"]:
            require(person["spouse_person_id"] in person_ids, f"{profile}: dangling spouse")

    residents_by_facility = Counter()
    resident_households_by_facility = Counter()
    for household in households:
        require(household["head_person_id"] in person_ids, f"{profile}: missing household head")
        require(household["member_ids"] and all(item in person_ids for item in household["member_ids"]), f"{profile}: dangling household member")
        require(household["residence_facility_id"] in facility_ids, f"{profile}: missing household residence")
        residence = facility_by_id[household["residence_facility_id"]]
        require(residence["category"] == "residential", f"{profile}: household assigned to non-residence")
        require(household["current_cell_id64"] == residence["cell_id64"], f"{profile}: household location mismatch")
        residents_by_facility[residence["facility_id"]] += len(household["member_ids"])
        resident_households_by_facility[residence["facility_id"]] += 1

    worker_ids = set()
    for facility in facilities:
        definition = definitions[facility["definition_id"]]
        require(facility["owner_id"], f"{profile}: Cell Facility owner missing")
        require(facility["worker_capacity"] == definition["max_workers"], f"{profile}: Facility capacity drift")
        require(len(facility["current_workers"]) <= facility["worker_capacity"], f"{profile}: workforce overload")
        require(all(item in person_ids for item in facility["current_workers"]), f"{profile}: non-Person worker")
        for person_id in facility["current_workers"]:
            require(person_id not in worker_ids, f"{profile}: Person assigned twice")
            worker_ids.add(person_id)
        require(residents_by_facility[facility["facility_id"]] <= facility["residential_capacity_persons"], f"{profile}: Person residence capacity")
        require(resident_households_by_facility[facility["facility_id"]] <= facility["residential_capacity_households"], f"{profile}: Household residence capacity")
        if facility["category"] == "agriculture":
            require(facility["normal_workers"] <= facility["peak_workers"] <= facility["worker_capacity"], f"{profile}: agricultural labor phases")
            require(facility["current_crop_id"], f"{profile}: agricultural crop missing")
            if facility["maturity_percent"] >= 80:
                require(facility["growth_stage"] == "early_harvest_allowed", f"{profile}: maturity rule")
    return {"persons": len(persons), "households": len(households), "facilities": len(facilities), "workers": len(worker_ids)}


def main() -> int:
    manifest = read_json(HAN / "world_manifest.json")
    require(manifest["grid_version"] == "HanWorldV1", "versioned aligned world package missing")
    require(manifest["grid_schema_version"] == GRID_SCHEMA, "grid schema missing")
    require(manifest["columns"] == 3314 and manifest["rows"] == 2176, "2000m dimensions changed")
    alignment = read_json(ROOT / "grid_alignment_v1.json")
    dimensions = alignment["fixed_dimensions"]
    require(dimensions["500"]["columns"] == manifest["columns"] * 4, "500m columns not exact subdivision")
    require(dimensions["500"]["rows"] == manifest["rows"] * 4, "500m rows not exact subdivision")
    require(dimensions["1000"]["columns"] == manifest["columns"] * 2, "1000m columns not exact subdivision")
    require(dimensions["1000"]["rows"] == manifest["rows"] * 2, "1000m rows not exact subdivision")
    require(dimensions["4000"]["columns"] * 2 == manifest["columns"], "4000m columns not exact aggregation")
    require(dimensions["4000"]["rows"] * 2 == manifest["rows"], "4000m rows not exact aggregation")

    facility_payload = read_json(ROOT / "facility_capacity_v0.json")
    definitions = {item["id"]: item for item in facility_payload["facilities"]}
    required_categories = {"residential", "agriculture", "resource", "industry", "commercial", "service", "road", "public", "military"}
    require(required_categories <= {item["category"] for item in definitions.values()}, "Facility category coverage")
    require(all(0 <= item["min_workers"] <= item["recommended_workers"] <= item["max_workers"] for item in definitions.values()), "Facility min/recommended/max")
    require(all(item["normal_workers"] <= item["peak_workers"] <= item["max_workers"] for item in definitions.values()), "Facility normal/peak/max")

    capacity = read_json(ROOT / "profile_capacity_results.json")["profiles"]
    expected = {item["profile_id"]: item for item in capacity}
    results = {profile: validate_profile(profile, expected[profile], definitions) for profile in PROFILES}
    require(all(not expected[profile]["warnings"] for profile in PROFILES), "capacity warnings present")
    require(expected["recommended"]["expansion"]["plus_100"]["remaining_developable_cells"] > 0, "doubling expansion does not fit")
    require(expected["recommended"]["natural_cell_ratio"] >= .70, "natural reserve below task threshold")

    prototype = read_json(UNITY / "luoyang_world.json")
    require(prototype["grid_schema_version"] == GRID_SCHEMA, "Unity prototype grid schema")
    require(prototype["city_anchor_cell_id64"] not in set(prototype["city_footprint_cell_ids"]), "anchor must remain distinct from footprint facts")
    require(len(prototype["city_footprint_cell_ids"]) > 1, "city footprint must contain multiple Cells")
    require(prototype["sample_cases"]["player"]["initial_cell_limit"] == 3, "player Cell limit")
    require(3 <= prototype["sample_cases"]["family"]["facility_count"] < 120, "ordinary-family sample scale")
    require(40 <= prototype["sample_cases"]["gentry"]["facility_count"] <= 150, "gentry sample scale")
    report_names = [f"{index:02d}_" for index in range(1, 11)]
    actual_reports = [item.name for item in (ROOT / "reports").glob("*.md")]
    require(all(any(name.startswith(prefix) for name in actual_reports) for prefix in report_names), "ten required reports")
    print(json.dumps({"status": "PASS", "profiles": results, "facilities": len(definitions),
                      "recommended_scale_m": manifest["cell_size_m"], "reports": len(actual_reports)}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
