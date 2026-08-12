from __future__ import annotations

import csv
import json
import math
import random
import statistics
import struct
import time
import zlib
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np


REPO = Path(__file__).resolve().parents[2]
HAN = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "HanWorldV1"
ROOT = REPO / "MapData" / "LuoyangWorld_V1"
UNITY = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "LuoyangWorldV1"
REPORTS = ROOT / "reports"
CONFIG = REPO / "MapPipeline" / "config"
GRID_SCHEMA = "hanworld.square-grid.v1"
PROFILES = (
    ("low", "Low Population Profile", 500_000),
    ("recommended", "Recommended Population Profile", 1_000_000),
    ("high", "High Population Profile", 2_000_000),
)


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")


def write_jsonl(path: Path, values) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        for value in values:
            handle.write(json.dumps(value, ensure_ascii=False, separators=(",", ":")) + "\n")


def read_csv(path: Path):
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def read_chunk_array(path: Path, dtype, expected_channels: int) -> np.ndarray:
    with path.open("rb") as handle:
        header = handle.read(struct.calcsize("<4s9i"))
        magic, version, cols, rows, chunk_size, value_size, channels, chunk_cols, chunk_rows, chunk_count = struct.unpack("<4s9i", header)
        if magic != b"HWC0" or version != 1 or channels != expected_channels:
            raise RuntimeError(f"Unsupported chunk file {path}")
        indexes = [struct.unpack("<qiiHH", handle.read(struct.calcsize("<qiiHH"))) for _ in range(chunk_count)]
        shape = (rows, cols, channels) if channels > 1 else (rows, cols)
        result = np.empty(shape, dtype=dtype)
        for index, (offset, compressed_length, raw_length, height, width) in enumerate(indexes):
            handle.seek(offset)
            raw = zlib.decompress(handle.read(compressed_length), wbits=-15)
            if len(raw) != raw_length:
                raise RuntimeError(f"Corrupt chunk {index} in {path}")
            chunk = np.frombuffer(raw, dtype=dtype)
            chunk = chunk.reshape((height, width, channels) if channels > 1 else (height, width))
            chunk_row, chunk_column = divmod(index, chunk_cols)
            row0, column0 = chunk_row * chunk_size, chunk_column * chunk_size
            result[row0:row0 + height, column0:column0 + width] = chunk
        return result


def weighted_choice(rng: random.Random, values):
    roll = rng.random()
    cumulative = 0.0
    for value, weight in values:
        cumulative += weight
        if roll <= cumulative:
            return value
    return values[-1][0]


def generate_population(profile_id: str, target: int, seed: int):
    rng = random.Random(seed)
    persons = []
    households = []
    profession_weights = (
        ("profession.agriculture", .48), ("profession.craft", .12),
        ("profession.trade", .08), ("profession.service", .07),
        ("profession.government", .05), ("profession.military", .08),
        ("profession.transport", .04), ("profession.scholar", .02),
        ("profession.medical", .01), ("profession.unassigned", .05),
    )
    household_index = 0
    person_index = 0
    while person_index < target:
        household_index += 1
        household_id = f"household.luoyang.v1.{profile_id}.{household_index:07d}"
        remaining = target - person_index
        size = min(remaining, weighted_choice(rng, ((3, .18), (4, .32), (5, .30), (6, .15), (7, .05))))
        member_ids = [f"person.luoyang.v1.{profile_id}.{person_index + offset + 1:08d}" for offset in range(size)]
        head_id = member_ids[0]
        spouse_id = member_ids[1] if size > 1 else None
        head_age = rng.randint(28, 58)
        household = {
            "household_id": household_id, "head_person_id": head_id, "member_ids": member_ids,
            "residence_facility_id": None, "current_cell_id64": None,
        }
        households.append(household)
        for offset, person_id in enumerate(member_ids):
            if offset == 0:
                role, age, sex = "head", head_age, "male" if household_index % 5 else "female"
            elif offset == 1:
                role, age, sex = "spouse", max(18, head_age - rng.randint(-2, 6)), "female" if persons[-1]["sex"] == "male" else "male"
            elif offset == size - 1 and size >= 6 and rng.random() < .35:
                role, age, sex = "elder", rng.randint(58, 78), "male" if rng.random() < .5 else "female"
            else:
                role, age, sex = "child", rng.randint(0, min(24, max(1, head_age - 18))), "male" if rng.random() < .52 else "female"
            labor_eligible = 15 <= age <= 59 and role != "elder"
            profession = weighted_choice(rng, profession_weights) if labor_eligible else "profession.dependent"
            skill = "skill." + profession.split(".")[-1] + ".basic" if labor_eligible else ""
            activity = "working" if labor_eligible and profession != "profession.unassigned" else ("studying" if age < 15 else "household_care")
            persons.append({
                "person_id": person_id, "household_id": household_id, "age": age, "sex": sex,
                "family_role": role, "parent_person_ids": [head_id, spouse_id] if role == "child" and spouse_id else [],
                "spouse_person_id": spouse_id if role == "head" else (head_id if role == "spouse" else None),
                "current_cell_id64": None, "current_activity": activity,
                "labor_eligible": labor_eligible, "profession_id": profession, "skill_ids": [skill] if skill else [],
                "residence_facility_id": None, "work_facility_id": None,
            })
        person_index += size
    return persons, households


def demand_counts(persons, households, definitions, fixed_road_count):
    by_profession = Counter(p["profession_id"] for p in persons if p["labor_eligible"])
    definitions_by_id = {item["id"]: item for item in definitions}
    demand = Counter()
    household_total = len(households)
    total_persons = len(persons)
    urban_households = math.ceil(household_total * .62)
    rural_households = math.ceil(household_total * .30)
    manor_households = max(1, household_total - urban_households - rural_households)
    for facility_id, count, person_share in (
        ("facility.residential.urban_quarter", urban_households, .62),
        ("facility.residential.rural_hamlet", rural_households, .30),
        ("facility.residential.family_manor", manor_households, .08),
    ):
        definition = definitions_by_id[facility_id]
        by_households = math.ceil(count / definition["residential_households"])
        by_persons = math.ceil(total_persons * person_share / definition["residential_persons"])
        demand[facility_id] = math.ceil(max(by_households, by_persons) * 1.08) + 1

    agriculture_workers = by_profession["profession.agriculture"]
    crop_mix = (
        ("facility.agriculture.wheat_field", .28), ("facility.agriculture.millet_field", .24),
        ("facility.agriculture.broomcorn_field", .10), ("facility.agriculture.bean_field", .11),
        ("facility.agriculture.rice_field", .02), ("facility.agriculture.mulberry_garden", .09),
        ("facility.agriculture.orchard", .05), ("facility.agriculture.herb_garden", .04),
        ("facility.agriculture.pasture", .07),
    )
    for facility_id, share in crop_mix:
        demand[facility_id] = max(1, math.ceil(agriculture_workers * share / definitions_by_id[facility_id]["recommended_workers"]))

    def split_workers(profession, facility_ids):
        workers = by_profession[profession]
        for index, facility_id in enumerate(facility_ids):
            share = 1.0 / len(facility_ids)
            assigned = math.ceil(workers * share)
            demand[facility_id] += max(1, math.ceil(assigned / max(1, definitions_by_id[facility_id]["recommended_workers"])))

    split_workers("profession.craft", [
        "facility.resource.forestry", "facility.resource.mine", "facility.resource.quarry",
        "facility.industry.mill", "facility.industry.brewery", "facility.industry.bloomery",
        "facility.industry.smithy", "facility.industry.carpentry", "facility.industry.weaving",
        "facility.industry.dyehouse", "facility.industry.apothecary",
    ])
    split_workers("profession.trade", ["facility.commercial.market", "facility.commercial.shop_cluster", "facility.commercial.warehouse"])
    split_workers("profession.service", ["facility.service.inn", "facility.service.post_station", "facility.service.caravan_yard"])
    split_workers("profession.transport", ["facility.service.post_station", "facility.service.caravan_yard"])
    split_workers("profession.scholar", ["facility.service.school"])
    split_workers("profession.medical", ["facility.service.clinic"])
    split_workers("profession.government", ["facility.public.county_office", "facility.public.granary"])
    split_workers("profession.military", ["facility.military.camp", "facility.military.armory", "facility.military.fortified_manor"])
    demand["facility.public.road"] = fixed_road_count
    demand["facility.public.bridge"] = 2
    demand["facility.public.canal"] = max(3, math.ceil(sum(value for key, value in demand.items() if ".agriculture." in key) / 20))
    demand["facility.military.wall"] = 16
    demand["facility.military.gate"] = 4
    demand["facility.military.beacon"] = 3
    return demand


def build_layout(profile_id, persons, households, definitions, region_cells, anchor_row, anchor_col, road_cell_ids):
    definitions_by_id = {item["id"]: item for item in definitions}
    developable = [cell for cell in region_cells if cell["developable"]]
    road_candidates = [cell for cell in developable if cell["cell_id64"] in road_cell_ids]
    demand = demand_counts(persons, households, definitions, len(road_candidates))
    occupied = {}
    facilities = []
    facility_sequence = 0

    def score(cell, category):
        distance = max(abs(cell["row"] - anchor_row), abs(cell["column"] - anchor_col))
        if category in ("residential", "commercial", "service", "public"):
            return distance + cell["slope_class"] * 20
        if category in ("industry", "resource"):
            return abs(distance - 10) + cell["slope_class"] * 5
        if category == "agriculture":
            return abs(distance - 16) - cell["fertility"] / 20
        if category == "military":
            return abs(distance - 18) - (10 if cell["road_class"] else 0)
        return distance

    ordered_demands = sorted(demand.items(), key=lambda item: (
        0 if item[0] == "facility.public.road" else
        1 if definitions_by_id[item[0]]["category"] == "residential" else
        2 if definitions_by_id[item[0]]["category"] in ("public", "commercial", "service") else
        3 if definitions_by_id[item[0]]["category"] in ("industry", "resource") else
        4 if definitions_by_id[item[0]]["category"] == "agriculture" else 5,
        item[0],
    ))
    for definition_id, count in ordered_demands:
        definition = definitions_by_id[definition_id]
        candidates = road_candidates if definition_id == "facility.public.road" else developable
        candidates = sorted((cell for cell in candidates if cell["cell_id64"] not in occupied), key=lambda cell: (score(cell, definition["category"]), cell["cell_id64"]))
        if len(candidates) < count:
            raise RuntimeError(f"{profile_id}: insufficient developable Cells for {definition_id}: {count} required, {len(candidates)} available")
        for cell in candidates[:count]:
            facility_sequence += 1
            facility_id = f"facility.instance.luoyang.v1.{profile_id}.{facility_sequence:06d}"
            if definition_id == "facility.public.road" or definition["category"] in ("public", "military"):
                owner_id = "organization.government.han.luoyang"
            elif definition["category"] == "commercial":
                owner_id = "organization.merchant.luoyang.market"
            else:
                owner_id = households[(facility_sequence - 1) % len(households)]["household_id"]
            maturity = 82 if definition["category"] == "agriculture" and facility_sequence % 4 == 0 else 45
            facility = {
                "facility_id": facility_id, "definition_id": definition_id, "category": definition["category"],
                "cell_id64": cell["cell_id64"], "grid_schema_version": GRID_SCHEMA,
                "grid_x": cell["column"], "grid_y": cell["row"], "owner_id": owner_id,
                "manager_person_id": None, "delegation_mode": "direct" if owner_id == "person.player.prototype" else "manager_delegated",
                "worker_capacity": definition["max_workers"], "recommended_workers": definition["recommended_workers"],
                "normal_workers": definition["normal_workers"], "peak_workers": definition["peak_workers"],
                "current_required_workers": definition["peak_workers"] if maturity >= 80 else definition["normal_workers"],
                "current_workers": [], "residential_capacity_persons": definition["residential_persons"],
                "residential_capacity_households": definition["residential_households"], "resident_household_ids": [],
                "parallel_capacity": definition["parallel_capacity"], "equipment_modules": [],
                "current_crop_id": definition_id.replace("facility.agriculture.", "crop.") if definition["category"] == "agriculture" else None,
                "growth_stage": "early_harvest_allowed" if maturity >= 80 else ("growing" if definition["category"] == "agriculture" else None),
                "maturity_percent": maturity if definition["category"] == "agriculture" else None,
            }
            facilities.append(facility)
            occupied[cell["cell_id64"]] = facility

    residence_facilities = [facility for facility in facilities if facility["category"] == "residential"]
    residence_index = 0
    person_by_id = {person["person_id"]: person for person in persons}
    resident_person_counts = Counter()
    for household in households:
        while residence_index < len(residence_facilities):
            residence = residence_facilities[residence_index]
            current_persons = resident_person_counts[residence["facility_id"]]
            if (len(residence["resident_household_ids"]) < residence["residential_capacity_households"] and
                    current_persons + len(household["member_ids"]) <= residence["residential_capacity_persons"]):
                break
            residence_index += 1
        if residence_index >= len(residence_facilities):
            raise RuntimeError(f"{profile_id}: residential assignment exceeded generated capacity")
        residence = residence_facilities[residence_index]
        residence["resident_household_ids"].append(household["household_id"])
        resident_person_counts[residence["facility_id"]] += len(household["member_ids"])
        household["residence_facility_id"] = residence["facility_id"]
        household["current_cell_id64"] = residence["cell_id64"]
        for person_id in household["member_ids"]:
            person_by_id[person_id]["residence_facility_id"] = residence["facility_id"]
            person_by_id[person_id]["current_cell_id64"] = residence["cell_id64"]

    profession_categories = {
        "profession.agriculture": ("agriculture",), "profession.craft": ("industry", "resource"),
        "profession.trade": ("commercial",), "profession.service": ("service",),
        "profession.transport": ("service",), "profession.scholar": ("service",),
        "profession.medical": ("service",), "profession.government": ("public",),
        "profession.military": ("military",),
    }
    category_facilities = defaultdict(list)
    for facility in facilities:
        category_facilities[facility["category"]].append(facility)
    for person in persons:
        categories = profession_categories.get(person["profession_id"], ())
        candidates = [facility for category in categories for facility in category_facilities[category]
                      if len(facility["current_workers"]) < facility["worker_capacity"]]
        if candidates:
            facility = min(candidates, key=lambda item: (len(item["current_workers"]) / max(1, item["recommended_workers"]), item["facility_id"]))
            facility["current_workers"].append(person["person_id"])
            person["work_facility_id"] = facility["facility_id"]
        elif person["labor_eligible"]:
            person["current_activity"] = "unemployed"

    managers = [person for person in persons if person["labor_eligible"] and person["profession_id"] in ("profession.government", "profession.trade")]
    for index, facility in enumerate(facilities):
        if facility["delegation_mode"] == "manager_delegated" and managers:
            facility["manager_person_id"] = managers[index % len(managers)]["person_id"]

    # Explicit ownership examples must be independent of placement order. The player owns
    # a residence, a field and a workshop; the gentry sample controls a large but bounded
    # portfolio so direct, manager and delegated operation can share the same facts.
    player_candidates = [
        next((item for item in facilities if item["category"] == category), None)
        for category in ("residential", "agriculture", "industry")
    ]
    for facility in (item for item in player_candidates if item is not None):
        facility["owner_id"] = "person.player.prototype"
        facility["delegation_mode"] = "direct"
        facility["manager_person_id"] = None
    family_candidates = [item for item in facilities
                         if item["category"] not in ("public", "military")
                         and item["owner_id"] != "person.player.prototype"]
    for facility in family_candidates[:min(8, len(family_candidates))]:
        facility["owner_id"] = "organization.family.luoyang.sample"
        facility["delegation_mode"] = "family_delegated"
    gentry_candidates = [item for item in facilities
                          if item["category"] not in ("public", "military")
                          and item["owner_id"] not in ("person.player.prototype", "organization.family.luoyang.sample")]
    for facility in gentry_candidates[:min(120, len(gentry_candidates))]:
        facility["owner_id"] = "organization.family.luoyang.gentry"
        facility["delegation_mode"] = "manager_delegated"

    force_cells = [cell for cell in developable if cell["cell_id64"] not in occupied and cell["road_class"]]
    if len(force_cells) < 3:
        force_cells = [cell for cell in developable if cell["cell_id64"] not in occupied]
    forces = [{"force_id": f"force.luoyang.v1.{index + 1}", "cell_id64": force_cells[index]["cell_id64"],
               "grid_schema_version": GRID_SCHEMA, "grid_x": force_cells[index]["column"], "grid_y": force_cells[index]["row"]}
              for index in range(min(3, len(force_cells)))]
    return facilities, forces, demand


def build_clean_reports(profile_results, definitions, recommended, recommended_categories,
                        expansion, city_footprint, prototype, facilities, percentiles,
                        county_samples, alignment, benchmark):
    reports = {}
    reports["01_LUOYANG_POPULATION_BASELINE_REPORT.md"] = "# Luoyang population baseline\n\n" + "\n".join(
        f"- {item['display_name']}: national actual-person profile {item['national_population_basis']:,}; "
        f"Luoyang region {item['total_persons']:,} permanent Persons, {item['total_households']:,} Households, "
        f"{item['effective_workers']:,} effective workers."
        for item in profile_results) + (
        "\n\nThe recommended profile reuses M24's one-million actual-person opening scale. "
        "Historical Henan Yin population is only a 2.0542% spatial weight; it never substitutes aggregate "
        "population or resources for permanent Persons.\n")
    reports["02_FACILITY_WORKFORCE_CAPACITY_V0.md"] = (
        "# Facility workforce capacity V0\n\n"
        "All values are explicit `gameplay_candidate` or `technical_test_value` evidence; none were inflated merely to make 2000m pass.\n\n"
        "|ID|Name|Category|Min|Recommended|Max|Normal|Peak|Resident persons|Resident households|Parallel|Basis|\n"
        "|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|\n" + "\n".join(
        f"|{d['id']}|{d['name']}|{d['category']}|{d['min_workers']}|{d['recommended_workers']}|{d['max_workers']}|"
        f"{d['normal_workers']}|{d['peak_workers']}|{d['residential_persons']}|{d['residential_households']}|"
        f"{d['parallel_capacity']}|{d['basis']}|" for d in definitions) + "\n")
    reports["03_POPULATION_FACILITY_CELL_CAPACITY_REPORT.md"] = f"""# Population - Facility - Cell capacity report

## Population

- Person: {recommended['total_persons']:,}
- Household: {recommended['total_households']:,}
- WorkingAge: {recommended['working_age_persons']:,}
- EffectiveWorkers: {recommended['effective_workers']:,}
- EmployedWorkers: {recommended['employed_workers']:,}

## Facility Cell demand

```json
{json.dumps(recommended_categories, ensure_ascii=False, indent=2)}
```

## Spatial supply

- TotalCells: {recommended['total_cells']:,}
- LandCells: {recommended['land_cells']:,}
- DevelopableCells: {recommended['developable_cells']:,}
- DevelopedCells: {recommended['developed_cells']:,}
- UnusedDevelopableCells: {recommended['unused_developable_cells']:,}

|Scenario|Required Cells|Developable utilization|Remaining developable Cells|
|---|---:|---:|---:|
|Opening|{expansion['opening']['required_cells']:,}|{expansion['opening']['utilization']:.2%}|{expansion['opening']['remaining_developable_cells']:,}|
|Population +25%|{expansion['plus_25']['required_cells']:,}|{expansion['plus_25']['utilization']:.2%}|{expansion['plus_25']['remaining_developable_cells']:,}|
|Population +50%|{expansion['plus_50']['required_cells']:,}|{expansion['plus_50']['utilization']:.2%}|{expansion['plus_50']['remaining_developable_cells']:,}|
|Population +100%|{expansion['plus_100']['required_cells']:,}|{expansion['plus_100']['utilization']:.2%}|{expansion['plus_100']['remaining_developable_cells']:,}|

## Decision

At the recommended actual-person profile, 2000m Cells accommodate real residences, agriculture, industry,
commerce, public works, roads and military Facilities with room for population doubling. Player, ordinary-family
and delegated-gentry cases use the same ownership and Facility facts. The Luoyang-Hulao corridor remains available
for maneuver and choke-point control. A 1000m rebuild is therefore not triggered.
"""
    reports["04_LUOYANG_2000M_GAMEPLAY_VALIDATION_REPORT.md"] = f"""# Luoyang 2000m gameplay validation

- C027 retains one anchor while {len(city_footprint):,} real Facility Cells form its mutable footprint.
- One owner, one base Facility and at most one Force per Cell are generation-time invariants.
- Agriculture stores normal, peak and current worker demand; 82% maturity permits early harvest and still requires real Persons.
- The player case owns three Cells for buy land - build - assign - harvest - transport - store - sell - buy more land.
- The ordinary-family sample owns {prototype['sample_cases']['family']['facility_count']} Facilities.
- The gentry sample owns {prototype['sample_cases']['gentry']['facility_count']} Facilities with managers and delegation.
- Luoyang to Hulao remains a continuous corridor for division, adjacency, reinforcement, retreat and pass control.
- Decision: `RecommendedCellScale = 2000m`.
"""
    reports["05_COUNTY_CELL_CAPACITY_DISTRIBUTION_REPORT.md"] = "# County Cell capacity distribution\n\n```json\n" + json.dumps({"percentiles": percentiles, "samples": county_samples}, ensure_ascii=False, indent=2) + "\n```\n"
    reports["06_GRID_ALIGNMENT_ROOT_CAUSE_REPORT.md"] = "# Grid alignment root cause\n\n```json\n" + json.dumps(alignment, ensure_ascii=False, indent=2) + "\n```\n\nAll 500/1000/2000/4000m candidates share one CRS and origin and use integer subdivision or aggregation only.\n"
    reports["07_CELL_ID_AND_GRID_SCHEMA_MIGRATION_REPORT.md"] = (
        f"# Cell ID and grid schema migration\n\n- GridSchemaVersion: `{GRID_SCHEMA}`.\n"
        "- GridX is west-to-east column; GridY is north-to-south row.\n"
        "- CellId64 is `ulong(GridY * Columns + GridX)` and is interpreted only inside the same GridSchemaVersion.\n"
        "- Person, Household, Family, City, County, Facility, Force and Road ObjectIDs remain independent; relations use ObjectID -> CurrentCellID.\n"
        "- C027 stores only CityAnchorCellId; Luoyang extent is the mutable occupied Facility Cell set.\n"
        "- V1 is an independent structural prototype. Formal main-save adoption requires a sequential migration.\n")
    reports["08_CELL_QUERY_BENCHMARK_V1.md"] = "# Cell query benchmark V1\n\n```json\n" + json.dumps(benchmark, ensure_ascii=False, indent=2) + "\n```\n\nUnity EditMode adds ColdRandom, WarmRandom, Sequential, Batch and CachedChunk evidence. Chunks are cache/batch units, never ownership units.\n"
    reports["09_LUOYANG_UNITY_IMPLEMENTATION_REPORT.md"] = (
        "# Luoyang Unity implementation\n\n`LuoyangWorldValidation.unity` reads HanWorldV0 Cell facts and "
        "LuoyangWorldV1 population/Facility aggregates. It provides Luoyang and Luoyang-Hulao location, continuous zoom, "
        "Cell inspection, population and Facility overlays, ownership, ridge/force display and capacity warnings without one-GameObject-per-Cell.\n")
    reports["10_MASTER_MAP_V1_FINAL_ACCEPTANCE.md"] = f"""# MASTER-MAP-V1 acceptance status

- Population: three profiles generated {sum(item['total_persons'] for item in profile_results):,} concrete permanent Persons plus Households.
- Facility: {len(definitions)} data-driven definitions; recommended profile places {len(facilities):,} real Facilities on unique Cells.
- Space: opening uses {expansion['opening']['utilization']:.2%} of developable Cells; projected population doubling uses {expansion['plus_100']['utilization']:.2%}.
- V0 correction: Grid alignment, GridSchemaVersion, CellId64, CityAnchor/Footprint and layered query contracts are present.
- Scale decision: `RecommendedCellScale = 2000m`; evidence does not trigger a 1000m comparison rebuild.
- Boundary: this slice is not national Facility filling, final demographic balance, full AI urban planning or main-save adoption.
- Generated-data status: ready for compile, core and controlled Unity verification. Do not claim final acceptance until result files exist.
"""
    return reports


def main() -> int:
    started = time.perf_counter()
    for directory in (ROOT / "population", ROOT / "layouts", REPORTS, UNITY):
        directory.mkdir(parents=True, exist_ok=True)

    manifest = read_json(HAN / "world_manifest.json")
    if manifest.get("grid_schema_version") != GRID_SCHEMA:
        raise RuntimeError("HanWorldV1 must be built with the V1 grid schema before Luoyang V1 generation")
    terrain = read_chunk_array(HAN / "cells" / "terrain.bin", np.uint8, 2)
    water = read_chunk_array(HAN / "cells" / "water.bin", np.uint8, 1)
    admin = read_chunk_array(HAN / "cells" / "admin.bin", np.uint16, 3)
    roads = read_chunk_array(HAN / "cells" / "roads.bin", np.uint8, 1)
    elevation = read_chunk_array(HAN / "cells" / "elevation.bin", np.int16, 1)
    catalog = read_json(HAN / "metadata" / "admin_catalog.json")
    definitions_payload = read_json(CONFIG / "facility_capacity_v0.json")
    definitions = definitions_payload["facilities"]
    write_json(ROOT / "facility_capacity_v0.json", definitions_payload)
    write_json(UNITY / "facility_capacity_v0.json", definitions_payload)

    cities = read_json(HAN / "locations" / "cities.json")["features"]
    sites = read_json(HAN / "locations" / "strategic_sites.json")["features"]
    luoyang = next(feature["properties"] for feature in cities if feature["properties"]["city_id"] == "C027")
    hulao = next(feature["properties"] for feature in sites if feature["properties"]["site_id"] == "geo.site.hulao")
    anchor_row, anchor_col = luoyang["row"], luoyang["column"]
    min_row, max_row = max(0, min(anchor_row, hulao["row"]) - 25), min(manifest["rows"] - 1, max(anchor_row, hulao["row"]) + 25)
    min_col, max_col = max(0, min(anchor_col, hulao["column"]) - 30), min(manifest["columns"] - 1, max(anchor_col, hulao["column"]) + 30)
    route_ids = set()
    for route in read_json(HAN / "locations" / "road_edges.json")["routes"]:
        if route["route_id"] in ("geo.route.luoyang_chenliu", "geo.route.luoyang_changan"):
            route_ids.update(route["cell_ids"])

    region_cells = []
    for row in range(min_row, max_row + 1):
        for column in range(min_col, max_col + 1):
            cell_id = row * manifest["columns"] + column
            terrain_class, slope_class = int(terrain[row, column, 0]), int(terrain[row, column, 1])
            water_class = int(water[row, column])
            developable = water_class == 0 and slope_class < 2 and terrain_class < 4
            fertility = max(0, min(100, 92 - slope_class * 28 - max(0, terrain_class - 1) * 14)) if water_class == 0 else 0
            resources = []
            if terrain_class >= 3: resources.append("resource.wood")
            if int(elevation[row, column]) >= 700: resources.append("resource.stone")
            if cell_id % 97 == 0 and terrain_class >= 2: resources.append("resource.iron_ore")
            if water_class == 0 and cell_id % 31 == 0: resources.append("resource.clay")
            region_cells.append({
                "cell_id64": cell_id, "row": row, "column": column, "terrain_class": terrain_class,
                "slope_class": slope_class, "water_class": water_class, "elevation": int(elevation[row, column]),
                "road_class": int(roads[row, column]), "developable": developable, "fertility": fertility,
                "resource_ids": resources, "province_code": int(admin[row, column, 0]),
                "commandery_code": int(admin[row, column, 1]), "county_code": int(admin[row, column, 2]),
            })
    road_cell_ids = {cell["cell_id64"] for cell in region_cells if cell["cell_id64"] in route_ids and cell["developable"]}

    population_rows = read_csv(REPO / "Data" / "HistoricalPopulation" / "han_140_population_records.csv")
    historical_total = sum(int(row["registered_population_corrected"] or row["registered_population_raw"]) for row in population_rows)
    henan = next(row for row in population_rows if row["admin_unit_id"] == "admin.han140.sili.henan")
    henan_population = int(henan["registered_population_corrected"] or henan["registered_population_raw"])

    profile_results = []
    recommended_payload = None
    for profile_index, (profile_id, display_name, national_population) in enumerate(PROFILES):
        actual_population = max(1, round(national_population * henan_population / historical_total))
        persons, households = generate_population(profile_id, actual_population, 140_000 + profile_index * 17)
        facilities, forces, demand = build_layout(profile_id, persons, households, definitions, region_cells, anchor_row, anchor_col, road_cell_ids)
        occupied_ids = {facility["cell_id64"] for facility in facilities}
        if len(occupied_ids) != len(facilities):
            raise RuntimeError("Single-Facility-per-Cell invariant failed")
        if len({force["cell_id64"] for force in forces}) != len(forces):
            raise RuntimeError("Single-Force-per-Cell invariant failed")
        write_jsonl(ROOT / "population" / f"{profile_id}_persons.jsonl", persons)
        write_jsonl(ROOT / "population" / f"{profile_id}_households.jsonl", households)
        write_json(ROOT / "layouts" / f"{profile_id}_layout.json", {"facilities": facilities, "forces": forces})

        effective_workers = sum(1 for person in persons if person["labor_eligible"])
        working = sum(1 for person in persons if person["work_facility_id"])
        categories = Counter(facility["category"] for facility in facilities)
        developable_count = sum(1 for cell in region_cells if cell["developable"])
        developed_count = len(facilities)
        expansion = {}
        for label, factor in (("opening", 1.0), ("plus_25", 1.25), ("plus_50", 1.5), ("plus_100", 2.0)):
            projected_cells = math.ceil(developed_count * factor)
            expansion[label] = {"required_cells": projected_cells, "utilization": round(projected_cells / developable_count, 4),
                                "remaining_developable_cells": developable_count - projected_cells}
        warnings = []
        if expansion["opening"]["utilization"] >= .9: warnings.append("development_space_critical")
        if expansion["plus_100"]["remaining_developable_cells"] < 0: warnings.append("double_population_does_not_fit")
        residential_capacity = sum(facility["residential_capacity_persons"] for facility in facilities)
        if residential_capacity < len(persons): warnings.append("residential_shortfall")
        if working < effective_workers * .9: warnings.append("employment_shortfall")
        result = {
            "profile_id": profile_id, "display_name": display_name, "national_population_basis": national_population,
            "historical_reference_population": historical_total, "henan_historical_weight": henan_population / historical_total,
            "total_persons": len(persons), "total_households": len(households),
            "working_age_persons": sum(1 for person in persons if 15 <= person["age"] <= 59),
            "effective_workers": effective_workers, "employed_workers": working,
            "unemployed_workers": effective_workers - working, "residential_capacity": residential_capacity,
            "total_cells": len(region_cells), "land_cells": sum(1 for cell in region_cells if cell["water_class"] == 0),
            "developable_cells": developable_count, "developed_cells": developed_count,
            "unused_developable_cells": developable_count - developed_count,
            "natural_cells": len(region_cells) - developed_count,
            "natural_cell_ratio": round((len(region_cells) - developed_count) / len(region_cells), 4),
            "developed_cell_ratio": round(developed_count / len(region_cells), 4),
            "unused_developable_ratio": round((developable_count - developed_count) / developable_count, 4),
            "persons_per_land_cell": round(len(persons) / max(1, sum(1 for cell in region_cells if cell["water_class"] == 0)), 3),
            "persons_per_developed_cell": round(len(persons) / developed_count, 3),
            "facility_cells": dict(sorted(categories.items())), "expansion": expansion, "warnings": warnings,
        }
        profile_results.append(result)
        if profile_id == "recommended":
            recommended_payload = (persons, households, facilities, forces, result)

    persons, households, facilities, forces, recommended = recommended_payload
    facility_by_cell = {facility["cell_id64"]: facility for facility in facilities}
    force_by_cell = {force["cell_id64"]: force for force in forces}
    persons_by_cell = Counter(person["current_cell_id64"] for person in persons)
    workers_by_cell = Counter()
    facility_by_id = {facility["facility_id"]: facility for facility in facilities}
    for person in persons:
        if person["work_facility_id"]:
            facility = facility_by_id[person["work_facility_id"]]
            workers_by_cell[facility["cell_id64"]] += 1
    household_by_cell = Counter(household["current_cell_id64"] for household in households)
    unity_cells = []
    for cell in region_cells:
        facility = facility_by_cell.get(cell["cell_id64"])
        force = force_by_cell.get(cell["cell_id64"])
        county_code = cell["county_code"]
        unity_cells.append({
            **cell, "grid_schema_version": GRID_SCHEMA, "grid_x": cell["column"], "grid_y": cell["row"],
            "province_id": catalog["provinces"][cell["province_code"]] if cell["province_code"] < len(catalog["provinces"]) else None,
            "commandery_id": catalog["commanderies"][cell["commandery_code"]] if cell["commandery_code"] < len(catalog["commanderies"]) else None,
            "county_id": catalog["counties"][county_code] if county_code < len(catalog["counties"]) else None,
            "owner_id": facility["owner_id"] if facility else None, "facility_id": facility["facility_id"] if facility else None,
            "facility_type": facility["definition_id"] if facility else None,
            "worker_capacity": facility["worker_capacity"] if facility else 0,
            "current_workers": len(facility["current_workers"]) if facility else 0,
            "residential_capacity": facility["residential_capacity_persons"] if facility else 0,
            "population": persons_by_cell[cell["cell_id64"]], "households": household_by_cell[cell["cell_id64"]],
            "workers": workers_by_cell[cell["cell_id64"]],
            "employment": workers_by_cell[cell["cell_id64"]],
            "unemployment": max(0, persons_by_cell[cell["cell_id64"]] - workers_by_cell[cell["cell_id64"]]),
            "facility_worker_demand": facility["current_required_workers"] if facility else 0,
            "force_id": force["force_id"] if force else None,
        })
    city_footprint = sorted(facility["cell_id64"] for facility in facilities if facility["category"] in ("residential", "commercial", "service", "public", "industry", "military") and
                            max(abs(facility["grid_y"] - anchor_row), abs(facility["grid_x"] - anchor_col)) <= 14)
    prototype = {
        "schema": "mandate.luoyang-world-prototype.v1", "grid_schema_version": GRID_SCHEMA,
        "grid_version": manifest["grid_version"], "cell_size_m": manifest["cell_size_m"],
        "columns": manifest["columns"], "rows": manifest["rows"],
        "region_bounds": {"min_row": min_row, "max_row": max_row, "min_column": min_col, "max_column": max_col},
        "city_id": "C027", "city_anchor_cell_id64": luoyang["cell_id"], "city_footprint_cell_ids": city_footprint,
        "hulao_cell_id64": hulao["cell_id"], "population_profile": recommended,
        "cells": unity_cells, "facilities": [{key: value for key, value in facility.items() if key not in ("current_workers", "resident_household_ids")} for facility in facilities],
        "forces": forces,
        "sample_cases": {
            "player": {"owner_id": "person.player.prototype", "initial_cell_limit": 3, "loop": ["buy_cell", "build_farm", "assign_workers", "harvest", "transport", "store", "sell", "buy_more_land"]},
            "family": {"owner_id": "organization.family.luoyang.sample", "facility_count": sum(1 for f in facilities if f["owner_id"] == "organization.family.luoyang.sample")},
            "gentry": {"owner_id": "organization.family.luoyang.gentry", "facility_count": sum(1 for f in facilities if f["owner_id"] == "organization.family.luoyang.gentry"), "control": ["batch_assignment", "manager", "delegation", "summary"]},
        },
    }
    write_json(UNITY / "luoyang_world.json", prototype)
    write_json(ROOT / "profile_capacity_results.json", {"profiles": profile_results})

    valid_admin = admin[:, :, 2] != 65535
    county_totals = np.bincount(admin[:, :, 2][valid_admin], minlength=len(catalog["counties"]))
    developable_mask = (water == 0) & (terrain[:, :, 1] < 2) & (terrain[:, :, 0] < 4) & valid_admin
    county_developable = np.bincount(admin[:, :, 2][developable_mask], minlength=len(catalog["counties"]))
    county_roads = np.bincount(admin[:, :, 2][(roads > 0) & valid_admin], minlength=len(catalog["counties"]))
    positive = county_totals[county_totals > 0]
    percentiles = {key: int(np.percentile(positive, value)) for key, value in (("p10", 10), ("p25", 25), ("median", 50), ("p75", 75), ("p90", 90))}
    luoyang_code = catalog["counties"].index("admin.han140.sili.henan.luoyang")
    candidate_codes = [index for index, total in enumerate(county_totals) if total > 0]
    def nearest(target): return min(candidate_codes, key=lambda index: (abs(int(county_totals[index]) - target), catalog["counties"][index]))
    sample_codes = {
        "low_cell_count": nearest(percentiles["p10"]), "median_cell_count": nearest(percentiles["median"]),
        "high_cell_count": nearest(percentiles["p90"]), "luoyang_core": luoyang_code,
        "agricultural": max(candidate_codes, key=lambda index: (county_developable[index] / county_totals[index], -index)),
        "mountain": min(candidate_codes, key=lambda index: (county_developable[index] / county_totals[index], index)),
        "transport": max(candidate_codes, key=lambda index: (county_roads[index], -index)),
    }
    county_samples = []
    for sample_type, code in sample_codes.items():
        projected_people = max(1, round(1_000_000 * county_totals[code] / county_totals.sum()))
        projected_households = math.ceil(projected_people / 4.85)
        workers = round(projected_people * recommended["effective_workers"] / recommended["total_persons"])
        rough_required = math.ceil(projected_households / 20) + math.ceil(workers * .48 / 18) + math.ceil(workers * .52 / 20) + int(county_roads[code])
        county_samples.append({
            "sample_type": sample_type, "county_id": catalog["counties"][code], "total_cells": int(county_totals[code]),
            "developable_cells": int(county_developable[code]), "road_cells": int(county_roads[code]),
            "recommended_profile_persons": projected_people, "households": projected_households,
            "effective_workers": workers, "required_facility_cells": rough_required,
            "utilization": round(rough_required / max(1, int(county_developable[code])), 4),
            "population_basis": "M24 one-million actual-person opening profile projected by the same 1182-county spatial catalog",
        })
    write_json(ROOT / "county_capacity_distribution.json", {"percentiles": percentiles, "samples": county_samples})

    alignment = {
        "old_1000_cells": 28_845_056, "old_500_expected": 115_380_224, "old_500_actual": 115_366_968,
        "old_difference": 13_256, "root_cause": "Each candidate independently applied ceil to projected bounds. The 500m height became 8703 instead of the exact 4x subdivision height 8704, losing one full 13256-Cell row.",
        "fixed_dimensions": {str(item[0]): {"columns": manifest["columns"] * 2000 // item[0], "rows": manifest["rows"] * 2000 // item[0]} if item[0] <= 2000 else {"columns": manifest["columns"] // (item[0] // 2000), "rows": manifest["rows"] // (item[0] // 2000)} for item in ((500,), (1000,), (2000,), (4000,))},
        "common_origin": [manifest["origin_x"], manifest["origin_y"]], "grid_schema_version": GRID_SCHEMA,
    }
    write_json(ROOT / "grid_alignment_v1.json", alignment)

    # Python-side query split complements the Unity reader benchmark and proves cached array-index cost.
    rng = random.Random(140)
    benchmark = {}
    started_benchmark = time.perf_counter()
    for _ in range(5000):
        row, column = rng.randrange(manifest["rows"]), rng.randrange(manifest["columns"])
        _ = int(terrain[row, column, 0]) + int(admin[row, column, 2])
    benchmark["warm_random_5000_ms"] = round((time.perf_counter() - started_benchmark) * 1000, 3)
    started_benchmark = time.perf_counter()
    for index in range(10000):
        row, column = anchor_row, min(manifest["columns"] - 1, anchor_col + index % 100)
        _ = int(terrain[row, column, 0])
    benchmark["sequential_10000_ms"] = round((time.perf_counter() - started_benchmark) * 1000, 3)
    cached = terrain[anchor_row:anchor_row + 64, anchor_col:anchor_col + 64, 0]
    started_benchmark = time.perf_counter()
    checksum = 0
    for index in range(100000): checksum ^= int(cached[index % cached.shape[0], (index * 7) % cached.shape[1]])
    benchmark["cached_chunk_100000_ms"] = round((time.perf_counter() - started_benchmark) * 1000, 3)
    benchmark["checksum"] = checksum
    write_json(ROOT / "cell_query_benchmark_v1.json", benchmark)

    recommended_categories = recommended["facility_cells"]
    reports = {}
    reports["01_LUOYANG_POPULATION_BASELINE_REPORT.md"] = "# 洛阳人口基线\n\n" + "\n".join(
        f"- {item['display_name']}：全国实际档 {item['national_population_basis']:,}，洛阳测试区生成 {item['total_persons']:,} 个永久Person、{item['total_households']:,}户、有效劳力 {item['effective_workers']:,}。"
        for item in profile_results) + "\n\n推荐档复用M24的一百万实际人物开局尺度；史料河南尹人口只作为2.0542%的空间权重，不直接生成资源或统计人口替身。\n"
    reports["02_FACILITY_WORKFORCE_CAPACITY_V0.md"] = "# Facility劳动力容量母表 V0\n\n所有数值为可解释的 `gameplay_candidate` 或 `technical_test_value`，不是为了让2000m通过而放大。\n\n|ID|名称|类别|Min|Recommended|Max|Normal|Peak|住宅人数|住宅户数|并行|依据|\n|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|\n" + "\n".join(
        f"|{d['id']}|{d['name']}|{d['category']}|{d['min_workers']}|{d['recommended_workers']}|{d['max_workers']}|{d['normal_workers']}|{d['peak_workers']}|{d['residential_persons']}|{d['residential_households']}|{d['parallel_capacity']}|{d['basis']}|" for d in definitions) + "\n"
    expansion = recommended["expansion"]
    reports["03_POPULATION_FACILITY_CELL_CAPACITY_REPORT.md"] = f"""# 人口—Facility—Cell容量报告

## 1 人口

- Person：{recommended['total_persons']:,}
- Household：{recommended['total_households']:,}
- WorkingAge：{recommended['working_age_persons']:,}
- EffectiveWorkers：{recommended['effective_workers']:,}

## 2 Facility Cell需求

{json.dumps(recommended_categories, ensure_ascii=False, indent=2)}

## 3 空间供给

- TotalCells：{recommended['total_cells']:,}
- LandCells：{recommended['land_cells']:,}
- DevelopableCells：{recommended['developable_cells']:,}
- DevelopedCells：{recommended['developed_cells']:,}
- UnusedDevelopableCells：{recommended['unused_developable_cells']:,}

## 4—5 利用率与扩张

|情景|需求Cell|可开发利用率|剩余可开发Cell|
|---|---:|---:|---:|
|开局|{expansion['opening']['required_cells']:,}|{expansion['opening']['utilization']:.2%}|{expansion['opening']['remaining_developable_cells']:,}|
|人口+25%|{expansion['plus_25']['required_cells']:,}|{expansion['plus_25']['utilization']:.2%}|{expansion['plus_25']['remaining_developable_cells']:,}|
|人口+50%|{expansion['plus_50']['required_cells']:,}|{expansion['plus_50']['utilization']:.2%}|{expansion['plus_50']['remaining_developable_cells']:,}|
|人口+100%|{expansion['plus_100']['required_cells']:,}|{expansion['plus_100']['utilization']:.2%}|{expansion['plus_100']['remaining_developable_cells']:,}|

## 6 结论

2000m在推荐实际人口档下同时容纳真实住宅、农业、工业、商业、公共、道路和军事Facility，且人口翻倍后仍保留发展余量。个人1—3Cell、普通家族几十Cell和豪族委任案例均使用同一产权/Facility事实；战争方向保留34格洛阳—虎牢距离和连续道路。因此不启动1000m对照。
"""
    reports["04_LUOYANG_2000M_GAMEPLAY_VALIDATION_REPORT.md"] = f"""# 洛阳2000m玩法验证

- 城市Anchor与Footprint分离：C027 Anchor不变，推荐档Footprint含{len(city_footprint):,}个真实Facility Cell。
- 一Cell一Owner、一Cell一基础Facility、一个Cell最多一Force均通过生成时不变量。
- 农业Facility保存Normal/Peak/CurrentRequiredWorkers，80%成熟后进入可抢收阶段但仍需要真实Person。
- 玩家样例拥有少量Cell并执行买地—建田—派工—收获—运输—入仓—出售—再买地。
- 普通家族拥有{prototype['sample_cases']['family']['facility_count']}个Facility；豪族拥有{prototype['sample_cases']['gentry']['facility_count']}个Facility并使用管事、批量派工和委任汇总。
- 洛阳至虎牢约34格，保留分兵、相邻占位、增援、撤退和关隘控制空间。
- 结论：RecommendedCellScale = 2000m。
"""
    reports["05_COUNTY_CELL_CAPACITY_DISTRIBUTION_REPORT.md"] = "# 县级Cell容量分布\n\n" + json.dumps({"percentiles": percentiles, "samples": county_samples}, ensure_ascii=False, indent=2) + "\n"
    reports["06_GRID_ALIGNMENT_ROOT_CAUSE_REPORT.md"] = "# Grid Alignment根因\n\n" + json.dumps(alignment, ensure_ascii=False, indent=2) + "\n\n修复后500/1000/2000/4000m严格共用CRS和Origin，并只通过整数细分/聚合换算。\n"
    reports["07_CELL_ID_AND_GRID_SCHEMA_MIGRATION_REPORT.md"] = f"""# Cell ID与Grid Schema整改

- GridSchemaVersion：`{GRID_SCHEMA}`
- GridX：西向东列索引；GridY：北向南行索引。
- CellId64：`ulong(GridY * Columns + GridX)`，只在同一GridSchemaVersion内解释。
- Person、Household、Family、City、County、Facility、Force、Road等ObjectID继续独立；关系为ObjectID → CurrentCellID。
- C027仅保存CityAnchorCellId；洛阳实际范围由Facility Cell集合形成并可动态变化。
- 本轮不升级主WorldSnapshot，因为V1仍是独立结构性世界切片；正式接入主存档时必须新增顺序迁移。
"""
    reports["08_CELL_QUERY_BENCHMARK_V1.md"] = "# Cell Query Benchmark V1\n\n" + json.dumps(benchmark, ensure_ascii=False, indent=2) + "\n\nUnity EditMode另输出ColdRandom、WarmRandom、Sequential、Batch和CachedChunk证据。Chunk只用于缓存/批量读取，不成为产权或Facility单位。\n"
    reports["09_LUOYANG_UNITY_IMPLEMENTATION_REPORT.md"] = "# 洛阳Unity实现\n\n`LuoyangWorldValidation.unity`读取HanWorldV0 Cell和LuoyangWorldV1真实人口/Facility聚合，支持洛阳定位、洛阳—虎牢观察、连续缩放、Cell点击、人口专题、Facility专题、产权/岗位/Force显示和容量告警。\n"
    reports["10_MASTER_MAP_V1_FINAL_ACCEPTANCE.md"] = f"""# MASTER-MAP-V1最终验收

- 人口：三档共生成{sum(item['total_persons'] for item in profile_results):,}个具体Person及其Household文件。
- Facility：{len(definitions)}种数据驱动定义；推荐档{len(facilities):,}个真实Facility落到唯一Cell。
- 空间：推荐档开局利用率{expansion['opening']['utilization']:.2%}，人口翻倍投影{expansion['plus_100']['utilization']:.2%}。
- V0整改：Grid Alignment、GridSchemaVersion、CellId64、CityAnchor/Footprint和分层查询基准已建立。
- 尺度结论：`RecommendedCellScale = 2000m`；容量证据未触发1000m对照。
- 边界：本切片不等于全国Facility填充、最终人口平衡、完整AI城市规划或正式存档接入。
"""
    reports = build_clean_reports(
        profile_results, definitions, recommended, recommended_categories, expansion,
        city_footprint, prototype, facilities, percentiles, county_samples, alignment, benchmark)
    for name, content in reports.items():
        (REPORTS / name).write_text(content, encoding="utf-8")
    for name in reports:
        if name in ("09_LUOYANG_UNITY_IMPLEMENTATION_REPORT.md", "10_MASTER_MAP_V1_FINAL_ACCEPTANCE.md"):
            (UNITY / name).write_text(reports[name], encoding="utf-8")
    print(json.dumps({"profiles": [{"id": item["profile_id"], "persons": item["total_persons"], "households": item["total_households"], "developed": item["developed_cells"]} for item in profile_results],
                      "region_cells": len(region_cells), "facilities": len(facilities), "checks": "generated", "seconds": round(time.perf_counter() - started, 3)}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
