#!/usr/bin/env python3
"""Build the additive Luoyang outer-supply population closure package.

The accepted 400K metropolitan package remains immutable.  This package only
materializes the inclusive-target gap as permanent persons, variable-size
households, and one-Cell residential facilities distributed over the existing
33 outer settlements.
"""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import math
import sys
import time
from collections import defaultdict
from pathlib import Path


SCHEMA = "mandate.luoyang-outer-supply-remediation.v1"
PERSON_MAGIC = b"MOHLYR01"
HOUSEHOLD_MAGIC = b"MOHLYS01"
NONE_U16 = 0xFFFF
NONE_U32 = 0xFFFFFFFF
RESIDENTS_PER_FACILITY = 440


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n",
                    encoding="utf-8")


def distribute(total: int, count: int):
    base, extra = divmod(total, count)
    return [base + (1 if index < extra else 0) for index in range(count)]


def proportional(total: int, weights):
    weight_total = sum(weights)
    raw = [total * weight / weight_total for weight in weights]
    result = [int(value) for value in raw]
    remainder = total - sum(result)
    order = sorted(range(len(weights)),
                   key=lambda i: (-(raw[i] - result[i]), i))
    for index in order[:remainder]:
        result[index] += 1
    return result


def read_facilities(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))["facilities"]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    repo = args.repo.resolve()
    output = (args.output or repo / "Assets" / "StreamingAssets" / "WorldMap" /
              "LuoyangOuterSupplyRemediationV1").resolve()

    urban = load_module(
        "luoyang_urban_v1_for_outer_supply_remediation",
        repo / "MapPipeline" / "scripts" /
        "build_luoyang_184_urban_initialization_v1.py")
    metro = load_module(
        "luoyang_metro_v1_for_outer_supply_remediation",
        repo / "MapPipeline" / "scripts" /
        "build_luoyang_184_metropolitan_initialization_v1.py")

    started = time.perf_counter()
    metro_root = (repo / "Assets" / "StreamingAssets" / "WorldMap" /
                  "Luoyang184MetropolitanInitializationV1")
    urban_root = (repo / "Assets" / "StreamingAssets" / "WorldMap" /
                  "Luoyang184UrbanInitializationV1")
    world_path = (repo / "Assets" / "StreamingAssets" / "WorldMap" /
                  "LuoyangWorldV1" / "luoyang_world.json")
    metro_manifest = json.loads((metro_root / "manifest.json").read_text(
        encoding="utf-8-sig"))
    catalogs = json.loads((metro_root / "catalogs.json").read_text(
        encoding="utf-8-sig"))
    spatial = json.loads((metro_root / "spatial_plan.json").read_text(
        encoding="utf-8-sig"))
    world = json.loads(world_path.read_text(encoding="utf-8-sig"))

    base_person_count = int(metro_manifest["person_count"])
    target_person_count = int(metro_manifest["supply_region_plan_population"])
    gap = target_person_count - base_person_count
    if gap <= 0:
        raise RuntimeError("The inclusive population target has no positive gap.")
    base_household_count = int(metro_manifest["household_count"])
    base_facility_count = int(metro_manifest["facility_count"])

    settlements = list(spatial["settlements"])
    if len(settlements) != 33:
        raise RuntimeError(f"Expected 33 formal outer settlements, found {len(settlements)}")
    settlement_populations = proportional(
        gap, [int(item["population_target"]) for item in settlements])

    occupied = {
        int(item["cell_id64"])
        for item in read_facilities(urban_root / "facilities.json") +
        read_facilities(metro_root / "facilities.json")
    }
    cells = [item for item in world["cells"]
             if item.get("developable") and int(item["cell_id64"]) not in occupied]
    used = set(occupied)

    def nearest_free(x: int, y: int):
        candidates = [item for item in cells if int(item["cell_id64"]) not in used]
        if not candidates:
            raise RuntimeError("No unused developable Cell remains for residence capacity.")
        selected = min(candidates, key=lambda item: (
            abs(int(item["grid_x"]) - x) + abs(int(item["grid_y"]) - y),
            int(item["cell_id64"])))
        used.add(int(selected["cell_id64"]))
        return selected

    facilities = []
    facilities_by_settlement = defaultdict(list)
    settlement_audit = []
    for settlement, added_population in zip(settlements, settlement_populations):
        facility_count = max(1, math.ceil(added_population / RESIDENTS_PER_FACILITY))
        capacity_total = added_population + max(40, added_population // 20)
        capacities = distribute(capacity_total, facility_count)
        for local_index, capacity in enumerate(capacities):
            cell = nearest_free(int(settlement["grid_x"]), int(settlement["grid_y"]))
            global_index = base_facility_count + len(facilities)
            facility_id = (
                f"facility.instance.luoyang.184.outer_supply.{len(facilities) + 1:06d}")
            item = {
                "global_facility_index": global_index,
                "facility_id": facility_id,
                "definition_id": "facility.residential.rural_hamlet",
                "display_name": f"{settlement['display_name']}扩充住区{local_index + 1}",
                "category_id": "residential",
                "cell_id64": int(cell["cell_id64"]),
                "grid_x": int(cell["grid_x"]),
                "grid_y": int(cell["grid_y"]),
                "owner_id": "organization.community.luoyang.metropolitan",
                "controller_id": f"organization.community.{settlement['settlement_id']}",
                "administrative_controller_id": "organization.government.han.henan",
                "area_type": settlement["area_type"],
                "settlement_id": settlement["settlement_id"],
                "profile_id": "profile.metropolitan.outer_supply_residence",
                "historical_confidence": "GameplayReconstruction",
                "spatial_precision": "Cell",
                "data_origin": "GeneratedHistoricalPopulation",
                "residential_capacity_persons": capacity,
                "current_residents": 0,
                "worker_capacity": 16,
                "current_workers": 0,
                "storage_capacity_units": 0,
                "normal_operation": True,
                "capability_ids": ["capability.worker_assignment", "capability.residential"],
            }
            facilities.append(item)
            facilities_by_settlement[settlement["settlement_id"]].append(item)
        settlement_audit.append({
            "settlement_id": settlement["settlement_id"],
            "display_name": settlement["display_name"],
            "added_population": added_population,
            "added_residential_facilities": facility_count,
            "added_residence_capacity": capacity_total,
            "existing_route_id": f"route.metropolitan.{settlement['settlement_id']}",
        })

    area_by_type = {name: area_id for area_id, name, _ in metro.AREA_PLANS}
    area_indexes = {value: catalogs["areas"].index(value) for value in catalogs["areas"]}
    occupation_unfixed = catalogs["occupations"].index("occupation.unfixed")
    activity_household = catalogs["activities"].index("activity.household_life")
    people = []
    households = []
    people_by_ordinal = {}

    for settlement_index, (settlement, population) in enumerate(
            zip(settlements, settlement_populations)):
        sizes = metro.household_sizes(population, settlement_index + 37)
        residence_pool = facilities_by_settlement[settlement["settlement_id"]]
        remaining = {item["global_facility_index"]:
                     int(item["residential_capacity_persons"])
                     for item in residence_pool}
        cursor = 0
        for size in sizes:
            household_ordinal = base_household_count + len(households)
            start = base_person_count + len(people)
            members = []
            for member_index in range(size):
                ordinal = base_person_count + len(people)
                if member_index == 0:
                    age = 28 + ((ordinal * 17) % 29)
                elif member_index == 1:
                    age = 24 + ((ordinal * 11) % 31)
                elif member_index in {2, 3}:
                    age = (ordinal * 7 + member_index * 3) % 20
                elif member_index == size - 1 and size >= 6:
                    age = 60 + (ordinal % 19)
                else:
                    age = 18 + ((ordinal * 13) % 43)
                stage = 0 if age <= 13 else 1 if age <= 19 else 2 if age <= 59 else 3 if age <= 69 else 4
                person = urban.Person(
                    ordinal=ordinal,
                    person_id=f"person.luoyang.184.outer_supply.{ordinal + 1:06d}",
                    display_name=metro.stable_name(ordinal),
                    birth_year=184 - age,
                    age=age,
                    age_stage=stage,
                    gender=1 if (ordinal + member_index) % 2 == 0 else 2,
                    health_bp=7600 + ((ordinal * 37) % 2201),
                    natural_lifespan=55 + ((ordinal * 19) % 31),
                    household=household_ordinal,
                    family_org=NONE_U16,
                    area=area_indexes[area_by_type[settlement["area_type"]]],
                    location_status=1,
                    current_cell=0,
                    residence=NONE_U32,
                    residence_status=1,
                    occupation=occupation_unfixed,
                    work_facility=NONE_U32,
                    activity=activity_household,
                    employment_status=2 if 14 <= age < 70 else 0,
                    civil_office=0,
                    military_office=0,
                    title=0,
                    allegiance=0,
                    political_role=0,
                    force=NONE_U16,
                    reserve_force=NONE_U16,
                    skill_profile=0,
                    knowledge_profile=0,
                    assets=200 + ((ordinal * 7919) % 18_000),
                    father=-1,
                    mother=-1,
                    spouse=-1,
                    data_origin=2,
                )
                members.append(person)
                people.append(person)
                people_by_ordinal[ordinal] = person
            if len(members) >= 2 and members[0].age_stage == 2 and members[1].age_stage == 2:
                members[0].spouse = members[1].ordinal
                members[1].spouse = members[0].ordinal
            father = next((item for item in members[:2]
                           if item.gender == 1 and item.age_stage == 2), None)
            mother = next((item for item in members[:2]
                           if item.gender == 2 and item.age_stage == 2), None)
            for person in members[2:]:
                if person.age_stage in {0, 1}:
                    person.father = father.ordinal if father else -1
                    person.mother = mother.ordinal if mother else -1

            selected = None
            for offset in range(len(residence_pool)):
                candidate = residence_pool[(cursor + offset) % len(residence_pool)]
                facility_index = candidate["global_facility_index"]
                if remaining[facility_index] >= size:
                    selected = candidate
                    cursor = (cursor + offset + 1) % len(residence_pool)
                    break
            if selected is None:
                raise RuntimeError(f"No residence capacity for {settlement['settlement_id']}")
            selected_index = selected["global_facility_index"]
            remaining[selected_index] -= size
            selected["current_residents"] += size
            for person in members:
                person.residence = selected_index
                person.current_cell = int(selected["cell_id64"])

            households.append(urban.Household(
                ordinal=household_ordinal,
                start=start,
                count=size,
                head=members[0].ordinal,
                family_org=NONE_U16,
                primary_residence=selected_index,
                household_type=0 if size == 1 else 1 if size == 2 else 2 if size <= 5 else 3,
                data_origin=2,
                wealth=sum(item.assets for item in members) + 1000,
            ))

    if len(people) != gap:
        raise RuntimeError(f"Population gap mismatch: expected {gap}, generated {len(people)}")
    if sum(item["current_residents"] for item in facilities) != gap:
        raise RuntimeError("Residence assignment did not conserve added population.")
    if len({item["cell_id64"] for item in facilities}) != len(facilities):
        raise RuntimeError("More than one remediation Facility occupies a Cell.")

    output.mkdir(parents=True, exist_ok=True)
    with (output / "persons.bin").open("wb") as stream:
        stream.write(urban.HEADER_STRUCT.pack(
            PERSON_MAGIC, 1, urban.PERSON_STRUCT.size, len(people), 0, 184))
        for person in people:
            stream.write(urban.PERSON_STRUCT.pack(
                person.ordinal, person.birth_year, person.gender, person.age_stage,
                person.health_bp, person.household, person.family_org,
                person.current_cell, person.residence, person.work_facility,
                person.occupation, person.activity, person.civil_office,
                person.military_office, person.title, person.allegiance,
                person.force, person.reserve_force, person.skill_profile,
                person.knowledge_profile, person.assets, person.natural_lifespan,
                person.political_role, person.data_origin,
                person.residence_status, person.employment_status,
                person.location_status, person.father, person.mother,
                person.spouse))
    with (output / "households.bin").open("wb") as stream:
        stream.write(urban.HEADER_STRUCT.pack(
            HOUSEHOLD_MAGIC, 1, urban.HOUSEHOLD_STRUCT.size,
            len(households), 0, 184))
        for household in households:
            stream.write(urban.HOUSEHOLD_STRUCT.pack(
                household.ordinal, household.head, household.start,
                household.count, household.family_org,
                household.primary_residence, household.household_type,
                household.data_origin, 0, household.wealth))

    write_json(output / "facilities.json", {
        "schema": "mandate.luoyang-outer-supply-remediation-facilities.v1",
        "facilities": facilities,
    })
    write_json(output / "settlements.json", {
        "schema": "mandate.luoyang-outer-supply-remediation-settlements.v1",
        "settlements": settlement_audit,
    })
    audit = {
        "schema": "mandate.luoyang-outer-supply-remediation-audit.v1",
        "inclusive_population_target": target_person_count,
        "base_person_count": base_person_count,
        "computed_population_gap": gap,
        "added_person_count": len(people),
        "total_person_count": base_person_count + len(people),
        "base_household_count": base_household_count,
        "added_household_count": len(households),
        "total_household_count": base_household_count + len(households),
        "base_facility_count": base_facility_count,
        "added_facility_count": len(facilities),
        "total_facility_count": base_facility_count + len(facilities),
        "settlement_count": len(settlements),
        "residence_capacity": sum(item["residential_capacity_persons"] for item in facilities),
        "assigned_residents": sum(item["current_residents"] for item in facilities),
        "labor_capable_person_count": sum(1 for item in people if 14 <= 184 - item.birth_year < 70),
        "unassigned_labor_capable_person_count": sum(
            1 for item in people if 14 <= 184 - item.birth_year < 70 and
            item.work_facility == NONE_U32),
        "one_facility_per_cell": True,
    }
    write_json(output / "audit_summary.json", audit)

    files = []
    for path in sorted(output.iterdir()):
        if path.name == "manifest.json" or path.suffix == ".meta":
            continue
        files.append({"path": path.name, "bytes": path.stat().st_size,
                      "sha256": sha256(path)})
    manifest = {
        "schema": SCHEMA,
        "format_version": 1,
        "scenario_id": metro_manifest["scenario_id"],
        "world_id": metro_manifest["world_id"],
        "city_id": metro_manifest["city_id"],
        "base_package_relative_path": "../Luoyang184MetropolitanInitializationV1",
        "base_manifest_sha256": sha256(metro_root / "manifest.json"),
        "inclusive_population_target": target_person_count,
        "base_person_count": base_person_count,
        "added_person_count": len(people),
        "person_count": base_person_count + len(people),
        "base_household_count": base_household_count,
        "added_household_count": len(households),
        "household_count": base_household_count + len(households),
        "base_facility_count": base_facility_count,
        "added_facility_count": len(facilities),
        "facility_count": base_facility_count + len(facilities),
        "settlement_count": len(settlements),
        "person_record_size": urban.PERSON_STRUCT.size,
        "household_record_size": urban.HOUSEHOLD_STRUCT.size,
        "files": files,
        "generated_at_is_metadata_only": True,
    }
    write_json(output / "manifest.json", manifest)
    print(json.dumps({
        **audit,
        "generation_elapsed_ms": round((time.perf_counter() - started) * 1000, 3),
    }, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
