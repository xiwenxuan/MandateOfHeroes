from __future__ import annotations

import json
from collections import Counter
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
ROOT = REPO / "MapData" / "Luoyang184Historical_V1"
UNITY = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184HistoricalV1"
DELIVERABLES = REPO / "deliverables" / "LUOYANG_184_HISTORICAL_V1"


def read_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def read_jsonl(path):
    with Path(path).open("r", encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def main():
    world = read_json(UNITY / "luoyang_184_world.json")
    definitions = read_json(UNITY / "facility_definitions_184.json")["definitions"]
    jobs = read_json(UNITY / "job_definitions_184.json")["definitions"]
    fortifications = read_json(UNITY / "fortifications_184.json")
    blueprints = read_json(UNITY / "blueprints_184.json")["blueprints"]
    sources = read_json(UNITY / "historical_sources_184.json")
    persons = read_jsonl(ROOT / "population" / "persons_184.jsonl")
    households = read_jsonl(ROOT / "population" / "households_184.jsonl")
    facilities = world["facilities"]
    cells = world["cells"]
    definitions_by_id = {item["id"]: item for item in definitions}
    jobs_by_id = {item["id"]: item for item in jobs}
    persons_by_id = {item["person_id"]: item for item in persons}
    facility_by_id = {item["facility_id"]: item for item in facilities}
    cell_by_id = {item["cell_id64"]: item for item in cells}

    require(world["schema"] == "mandate.luoyang-184-historical-world.v1", "world schema")
    require(world["scenario_year"] == 184 and world["scenario_polity_id"] == "polity.eastern_han", "184 Eastern Han boundary")
    require(world["grid_schema_version"] == "hanworld.square-grid.v1" and world["cell_size_m"] == 2000, "unified HanWorldV1 Cell contract")
    require(len(cells) == len({c["cell_id64"] for c in cells}), "CellId64 unique")
    require(all(c["cell_id64"] == c["grid_y"] * world["columns"] + c["grid_x"] for c in cells), "CellId64 formula")
    require(len(facilities) == len({f["facility_id"] for f in facilities}), "Facility ID unique")
    require(len(facilities) == len({f["cell_id64"] for f in facilities}), "one base Facility per Cell")
    require(all(f["cell_id64"] in cell_by_id for f in facilities), "Facility Cell references")
    require(all(f["definition_id"] in definitions_by_id for f in facilities), "Facility definition references")
    require(all(f["owner_id"] and f["controller_id"] for f in facilities), "every Facility has Owner and Controller")
    require(all(f["purpose_ids"] and f["capability_ids"] for f in facilities), "every visible Facility is functional")
    require(all(f["historical_confidence"] in ("HistoricalAnchor", "HistoricalReconstruction", "GameplayReconstruction") for f in facilities), "confidence levels")
    require(all(f["spatial_precision"] in ("Confirmed", "Probable", "Approximate") for f in facilities), "spatial precision levels")
    require(len(sources["sources"]) >= 5 and sources["known_conflicts"], "source catalog and conflicts")

    require(len(persons) == 20542 and len({p["person_id"] for p in persons}) == 20542, "20,542 permanent Persons")
    require(len(households) == 4498 and len({h["household_id"] for h in households}) == 4498, "4,498 Households")
    all_residents = [pid for f in facilities for pid in f["resident_person_ids"]]
    require(len(all_residents) == len(set(all_residents)), "one permanent residence per housed Person")
    require(all(pid in persons_by_id for pid in all_residents), "resident Person references")
    require(all(len(f["resident_person_ids"]) <= f["residential_capacity_persons"] for f in facilities), "Person housing capacity")
    for facility in facilities:
        definition = definitions_by_id[facility["definition_id"]]
        require(facility["residential_capacity_persons"] == definition["residential_capacity_persons"], "definition/state residence capacity")
        if facility["definition_id"] == "facility.historical.barracks":
            require(facility["allowed_resident_type_ids"] == ["population.active_military"], "barracks resident contract")
            require(all(persons_by_id[pid]["active_military"] for pid in facility["resident_person_ids"]), "barracks only active military")
        elif facility["category_id"] != "residential":
            require(facility["residential_capacity_persons"] == 0 or facility["definition_id"] == "facility.historical.barracks", "non-residential permanent capacity zero")
    housed = sum(1 for p in persons if p.get("residence_facility_id"))
    require(housed == world["population_profile"]["housed_persons"], "housed population total")
    require(world["population_profile"]["unhoused_persons"] == 128, "unhoused people remain explicit")
    require(housed + world["population_profile"]["unhoused_persons"] == len(persons), "housing ledger conservation")

    worker_memberships = [pid for f in facilities for pid in f["worker_person_ids"]]
    require(len(worker_memberships) == len(set(worker_memberships)), "one current Facility job per Person")
    require(all(pid in persons_by_id for pid in worker_memberships), "worker Person references")
    require(all(len(f["worker_person_ids"]) <= f["worker_capacity"] for f in facilities), "worker capacity")
    for facility in facilities:
        require(facility["normal_operation"] == (len(facility["worker_person_ids"]) >= facility["minimum_workers_for_normal_operation"]), "no workers means no normal production")
        for person_id in facility["worker_person_ids"]:
            person = persons_by_id[person_id]
            matching = [jobs_by_id[job_id] for job_id in facility["job_definition_ids"]
                        if jobs_by_id[job_id]["profession_id"] == person["profession_id"]]
            require(matching, "worker profession eligible for Facility job")
            require(any(person["skill_basis_points_by_id"].get(job["primary_skill_id"], 0) >= job["minimum_skill_basis_points"] for job in matching), "worker skill eligible")
    require(world["ai_pressure"]["unhoused_persons"] == 128, "AI reads actual housing pressure")
    require("fixed_residential_job_cell_ratio" not in json.dumps(world["ai_pressure"]), "AI has no fixed residential/job ratio")

    main_gates = [g for g in fortifications["gates"] if g["network_id"] == "fortification.luoyang.main_wall"]
    require(len(main_gates) == 12, "twelve independent main-city gates")
    expected_gate_names = {"谷门", "夏门", "津门", "小苑门", "平城门", "开阳门", "上西门", "雍门", "广阳门", "上东门", "中东门", "旄门"}
    require({facility_by_id[g["facility_id"]]["display_name"] for g in main_gates} == expected_gate_names, "twelve gate names")
    require(all(g["owner_id"] and g["controller_id"] and g["maximum_durability"] > 0 and g["passage_capacity_per_hour"] > 0 for g in fortifications["gates"]), "gate state contract")
    require(all(set(("height_centimetres", "thickness_centimetres", "material_id", "maximum_durability", "current_durability", "defender_person_ids", "wall_state")) <= set(w) for w in fortifications["walls"]), "wall state contract")
    require(all(m["moat_state"] == "Flooded" and m["blocks_ordinary_movement"] for m in fortifications["moats"]), "moat restrictions")
    require(len(fortifications["networks"]) == 3 and all(n["network_id"] for n in fortifications["networks"]), "outer and two independent palace networks")
    require(fortifications["siege_v0"]["wall_blocks_force"] and fortifications["siege_v0"]["breach_creates_passable_crossing"], "siege V0")

    historical = [f for f in facilities if f["facility_id"].startswith("facility.instance.luoyang.184.")]
    required_names = {"北宫", "南宫", "永安宫", "濯龙园", "三公官署西区", "太仓", "武库", "金市", "南市", "马市", "太学", "明堂", "辟雍", "灵台"}
    require(required_names <= {f["display_name"] for f in historical}, "required historical facilities")
    require(all(f["source_ids"] for f in historical), "historical/reconstructed Facility provenance")
    require(all(f["future_hook_ids"] for f in historical), "historical Facility future hooks")

    require(len(blueprints) >= 1, "multi-Cell blueprint exists")
    for blueprint in blueprints:
        require(blueprint["cell_count"] == len(blueprint["cells"]) > 1, "blueprint cell count")
        require(set(blueprint["shared_placement_modes"]) == {"Player", "HistoricalGeneration", "AI"}, "shared blueprint template")
        require(all(set(("relative_x", "relative_y", "facility_definition_id", "orientation", "required_road_connection_ids", "module_ids", "construction_stage", "build_order", "metadata")) <= set(c) for c in blueprint["cells"]), "blueprint Cell contract")
        require(blueprint["metadata"]["instant_construction"] == "false", "blueprint is not instant construction")

    map_path = DELIVERABLES / "LUOYANG_184_HISTORICAL_MAP_V1.png"
    require(map_path.exists() and map_path.stat().st_size > 200_000, "official historical map PNG")
    required_reports = [f"0{index}_" for index in range(1, 8)]
    report_names = [p.name for p in (ROOT / "reports").glob("*.md")]
    require(all(any(name.startswith(prefix) for name in report_names) for prefix in required_reports), "seven reports")
    require(all((UNITY / name).exists() for name in report_names), "Unity package report mirror")

    summary = {
        "status": "PASS", "persons": len(persons), "households": len(households), "facilities": len(facilities),
        "historical_facilities": len(historical), "facility_categories": dict(Counter(f["category_id"] for f in facilities)),
        "main_city_gates": len(main_gates), "walls": len(fortifications["walls"]), "moats": len(fortifications["moats"]),
        "housed": housed, "unhoused": len(persons) - housed, "reports": len(report_names), "map_bytes": map_path.stat().st_size,
    }
    write_path = ROOT / "validation_summary.json"
    write_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False))


if __name__ == "__main__":
    main()
