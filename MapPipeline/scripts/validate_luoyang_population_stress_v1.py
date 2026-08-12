#!/usr/bin/env python3
"""Independent invariant audit for LUOYANG-POPULATION-STRESS-V1."""

from __future__ import annotations

import argparse
import hashlib
import json
import struct
from pathlib import Path


EXPECTED = [20_542, 50_000, 100_000, 250_000, 500_000]
MAGIC = b"LYPSTR01"
HEADER = struct.Struct("<8siiiiq")
PERSON = struct.Struct("<QQQQiHBBiiBHBBiiqB2x")
REPORTS = [
    "01_POPULATION_STRESS_PROFILE_DEFINITION.md", "02_PERSON_MEMORY_AND_TICK_BENCHMARK.md",
    "03_HOUSING_ASSIGNMENT_BENCHMARK.md", "04_JOB_MATCHING_BENCHMARK.md",
    "05_FACILITY_CELL_CAPACITY_STRESS_REPORT.md", "06_AI_ADAPTIVE_CONSTRUCTION_REPORT.md",
    "07_AI_CITY_BALANCE_STRESS_REPORT.md", "08_SAVE_LOAD_SCALING_REPORT.md",
    "09_PERSON_QUERY_INDEX_BENCHMARK.md", "10_LUOYANG_250K_DEEP_DIVE.md",
    "11_LUOYANG_500K_LIMIT_REPORT.md", "12_LUOYANG_POPULATION_STRESS_FINAL_ACCEPTANCE.md",
]


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(chunk)
    return value.hexdigest()


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def validate_binary(path, count, historical_people, facility_by_id, military_residence_ordinals, civilian_residence_ordinals, facility_capacities):
    require(path.stat().st_size == HEADER.size + count * PERSON.size, f"binary length mismatch: {path}")
    households = set(); resident_counts = {}; worker_counts = {}
    metrics = {"housed": 0, "military_residents": 0, "eligible_workers": 0, "employed_workers": 0}
    with path.open("rb") as stream:
        magic, version, size, actual, historical, _ = HEADER.unpack(stream.read(HEADER.size))
        require((magic, version, size, actual, historical) == (MAGIC, 1, PERSON.size, count, 20_542), "invalid binary header")
        for index in range(count):
            record = PERSON.unpack(stream.read(PERSON.size))
            require(record[0] == index + 1, f"non-permanent or duplicate sequence at {index}")
            require(record[1] > 0, f"missing household at {index}")
            require(0 <= record[4] <= 130 and 0 <= record[5] <= 10_000, f"invalid Person facts at {index}")
            require(record[17] in (0, 1, 2, 3), f"invalid LOD tier at {index}")
            if record[8] >= 0:
                eligible = military_residence_ordinals if record[12] else civilian_residence_ordinals
                require(record[8] in eligible, f"resident assigned to ineligible Facility at {index}")
                resident_counts[record[8]] = resident_counts.get(record[8], 0) + 1
                metrics["housed"] += 1
                metrics["military_residents"] += 1 if record[12] else 0
            if record[9] >= 0:
                worker_counts[record[9]] = worker_counts.get(record[9], 0) + 1
                metrics["employed_workers"] += 1
            metrics["eligible_workers"] += 1 if record[13] else 0
            if index < 20_542:
                source = historical_people[index]
                expected_activity = {"dependent": 0, "working": 1, "serving": 2, "unemployed": 3}.get(source["current_activity"], 0)
                expected_profession = {
                    "profession.agriculture": 1, "profession.craft": 2, "profession.trade": 3,
                    "profession.service": 4, "profession.transport": 5, "profession.government": 6,
                    "profession.scholar": 7, "profession.military": 8, "profession.medical": 9,
                }.get(source.get("profession_id"), 4)
                skills = list(source.get("skill_basis_points_by_id", {}).values())
                expected = (int(source["current_cell_id64"]), int(source["age"]),
                            0 if source["sex"] == "male" else 1, expected_activity,
                            facility_by_id.get(source.get("residence_facility_id"), -1),
                            facility_by_id.get(source.get("work_facility_id"), -1), expected_profession,
                            skills[0] if skills else 0, 1 if source.get("active_military") else 0,
                            1 if source.get("labor_eligible") else 0)
                actual = (record[2], record[4], record[6], record[7], record[8], record[9],
                          record[10], record[11], record[12], record[13])
                require(actual == expected, f"historical Person core facts changed at {index}")
            households.add(record[1])
    for ordinal, amount in resident_counts.items():
        require(amount <= facility_capacities[ordinal][0], f"residential Person capacity exceeded at Facility {ordinal}")
    for ordinal, amount in worker_counts.items():
        require(amount <= facility_capacities[ordinal][1], f"worker capacity exceeded at Facility {ordinal}")
    metrics["households"] = len(households)
    return metrics


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, required=True)
    args = parser.parse_args()
    project = args.project_root.resolve()
    historical = project / "MapData" / "Luoyang184Historical_V1"
    root = project / "MapData" / "LuoyangPopulationStress_V1"
    stream_root = project / "Assets" / "StreamingAssets" / "WorldMap" / "LuoyangPopulationStressV1"
    manifest = read_json(root / "stress_manifest.json")
    stream_manifest = read_json(stream_root / "stress_manifest.json")
    require(manifest == stream_manifest, "runtime manifest differs from audit manifest")
    require(manifest["schema"] == "mandate.luoyang-population-stress-manifest.v1", "wrong manifest schema")
    require(manifest["cell_size_m"] == 2000, "Cell size changed")
    require(manifest["historical_scenario_population"] == 20_542, "historical population changed")
    hashes = manifest["historical_source_hashes"]
    require(hashes["world_sha256"] == digest(historical / "luoyang_184_world.json"), "historical world was modified")
    require(hashes["persons_sha256"] == digest(historical / "population" / "persons_184.jsonl"), "historical Persons were modified")
    world = read_json(historical / "luoyang_184_world.json")
    historical_people = [json.loads(line) for line in
                         (historical / "population" / "persons_184.jsonl").read_text(encoding="utf-8-sig").splitlines()
                         if line.strip()]
    facility_by_id = {facility["facility_id"]: index for index, facility in enumerate(world["facilities"])}
    definitions = {item["id"]: item for item in read_json(historical / "facility_definitions_184.json")["definitions"]}
    require(world["cell_size_m"] == 2000, "historical Cell size regression")
    require(len(world["facilities"]) == 1230 and len({f["cell_id64"] for f in world["facilities"]}) == 1230, "historical Facility uniqueness regression")
    fortifications = read_json(historical / "fortifications_184.json")
    gates = sum(1 for gate in fortifications["gates"] if gate["network_id"] == "fortification.luoyang.main_wall")
    walls = len(fortifications["walls"])
    moats = len(fortifications["moats"])
    require((gates, walls, moats) == (12, 130, 80), f"fortification regression: {(gates, walls, moats)}")
    base_cells = {int(f["cell_id64"]) for f in world["facilities"]}
    require([p["person_count"] for p in manifest["profiles"]] == EXPECTED, "profile order/count mismatch")
    results = []
    for entry in manifest["profiles"]:
        count = entry["person_count"]
        slug = f"profile_{count:06d}"
        profile_dir = root / "profiles" / slug
        summary = read_json(profile_dir / "profile_summary.json")
        runtime_summary = read_json(stream_root / entry["summary_relative_path"])
        require(summary == runtime_summary, f"runtime summary mismatch: {count}")
        require(summary["person_count"] == count and summary["lod"]["permanent_person_count"] == count, "Person count summary mismatch")
        require(summary["fixed_mode"]["facilities_added"] == 0, "fixed mode created Facility")
        require(summary["save_load"]["round_trip_consistent"], "save/load round trip failed")
        construction = read_json(profile_dir / "adaptive_construction.json")
        added = construction["facilities_added"]
        all_facilities = list(world["facilities"]) + list(added)
        military_ordinals = set(); civilian_ordinals = set(); capacities = []
        for ordinal, facility in enumerate(all_facilities):
            definition = definitions.get(facility["definition_id"], {})
            residence = int(facility.get("residential_capacity_persons", facility.get("effects", {}).get("residential_capacity", 0)))
            workers = int(facility.get("worker_capacity", facility.get("effects", {}).get("job_capacity", 0)))
            capacities.append((residence, workers))
            if residence > 0:
                (military_ordinals if "population.active_military" in definition.get("allowed_resident_type_ids", []) else civilian_ordinals).add(ordinal)
        person_metrics = validate_binary(profile_dir / "persons.bin", count, historical_people, facility_by_id,
                                         military_ordinals, civilian_ordinals, capacities)
        require(summary["adaptive_mode"]["housed_population"] == person_metrics["housed"], "housing summary differs from permanent Persons")
        require(summary["adaptive_mode"]["military_residents"] == person_metrics["military_residents"], "military residence summary mismatch")
        require(summary["adaptive_mode"]["eligible_workers"] == person_metrics["eligible_workers"], "eligible worker summary mismatch")
        require(summary["adaptive_mode"]["employed_workers"] == person_metrics["employed_workers"], "employment summary mismatch")
        ids = set(); cells = set()
        for item in added:
            require(item["facility_id"] not in ids, "duplicate adaptive Facility ID")
            require(item["cell_id64"] not in cells and item["cell_id64"] not in base_cells, "Cell contains multiple base Facilities")
            require(item["owner_id"] and item["controller_id"], "adaptive Facility lacks owner/controller")
            require(item["construction"]["status_history"] == ["Planned", "Approved", "UnderConstruction", "Completed"], "construction bypassed lifecycle")
            require(item["construction"]["completed_day"] <= 365, "construction outside simulated year")
            require(not item["facility_id"].startswith("facility.instance.luoyang.184."), "adaptive construction overwrote historical ID")
            ids.add(item["facility_id"]); cells.add(item["cell_id64"])
        require(len(added) == summary["adaptive_mode"]["facilities_added"], "adaptive Facility summary mismatch")
        require(summary["adaptive_mode"]["facility_count"] == 1230 + len(added), "Facility total mismatch")
        require(summary["adaptive_mode"]["occupied_facility_cells"] == summary["adaptive_mode"]["facility_count"], "one Cell/Facility invariant failed")
        require(summary["lod"]["high_frequency_actor_count"] <= summary["lod"]["maximum_visual_actor_count"] <= 256, "visual Actor pool is unbounded")
        results.append({"persons": count, "households_seen": person_metrics["households"], "facilities_added": len(added)})
    for report in REPORTS:
        require((root / "reports" / report).is_file(), f"missing report: {report}")
    require(len({entry["summary_relative_path"] for entry in manifest["profiles"]}) == 5, "profiles share mutable output")
    output = {"status": "PASS", "profiles": results, "historical_hashes": hashes,
              "cell_size_m": 2000, "subcells": 0, "gates": gates, "walls": walls, "moats": moats, "reports": len(REPORTS)}
    (root / "validation_summary.json").write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(output, ensure_ascii=False))


if __name__ == "__main__":
    main()
