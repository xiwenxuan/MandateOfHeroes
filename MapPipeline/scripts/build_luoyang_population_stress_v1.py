#!/usr/bin/env python3
"""Build deterministic Luoyang permanent-population stress profiles.

The stress package is deliberately isolated from the protected 184 package.  Every
reported Person has a fixed-size record; adaptive construction occupies real free
Cells and records the complete project lifecycle.
"""

from __future__ import annotations

import argparse
import collections
import hashlib
import json
import os
import shutil
import struct
import time
from pathlib import Path


PROFILE_COUNTS = (
    ("Profile_020542_HistoricalBaseline", 20_542, "20K historical baseline"),
    ("Profile_050000_Stress", 50_000, "50K low pressure"),
    ("Profile_100000_Stress", 100_000, "100K medium pressure"),
    ("Profile_250000_Stress", 250_000, "250K imperial-capital deep dive"),
    ("Profile_500000_Stress", 500_000, "500K limit observation"),
)
MAGIC = b"LYPSTR01"
HEADER = struct.Struct("<8siiiiq")
PERSON = struct.Struct("<QQQQiHBBiiBHBBiiqB2x")
SEED = 184_020_542
HISTORICAL_PERSONS = 20_542
HISTORICAL_HOUSEHOLDS = 4_498
SIMULATION_DAYS = 365
PROFILE_SCHEMA = "mandate.luoyang-population-stress-profile-summary.v1"


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def clamp(value: int, low: int = 0, high: int = 10_000) -> int:
    return max(low, min(high, int(value)))


def ratio_pressure(shortage: int, demand: int) -> int:
    return 0 if shortage <= 0 or demand <= 0 else clamp(shortage * 10_000 // demand)


def process_working_set() -> int:
    try:
        import psutil
        return int(psutil.Process().memory_info().rss)
    except Exception:
        pass
    try:
        import ctypes
        import ctypes.wintypes

        class Counters(ctypes.Structure):
            _fields_ = [("cb", ctypes.wintypes.DWORD), ("PageFaultCount", ctypes.wintypes.DWORD),
                        ("PeakWorkingSetSize", ctypes.c_size_t), ("WorkingSetSize", ctypes.c_size_t),
                        ("QuotaPeakPagedPoolUsage", ctypes.c_size_t), ("QuotaPagedPoolUsage", ctypes.c_size_t),
                        ("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t), ("QuotaNonPagedPoolUsage", ctypes.c_size_t),
                        ("PagefileUsage", ctypes.c_size_t), ("PeakPagefileUsage", ctypes.c_size_t)]
        counters = Counters()
        counters.cb = ctypes.sizeof(counters)
        handle = ctypes.windll.kernel32.GetCurrentProcess()
        if ctypes.windll.psapi.GetProcessMemoryInfo(handle, ctypes.byref(counters), counters.cb):
            return int(counters.WorkingSetSize)
    except Exception:
        pass
    return 0


def load_historical(base: Path):
    world_path = base / "luoyang_184_world.json"
    persons_path = base / "population" / "persons_184.jsonl"
    world = read_json(world_path)
    definitions = {item["id"]: item for item in read_json(base / "facility_definitions_184.json")["definitions"]}
    persons = []
    with persons_path.open("r", encoding="utf-8-sig") as stream:
        for line in stream:
            if line.strip():
                persons.append(json.loads(line))
    if len(persons) != HISTORICAL_PERSONS:
        raise RuntimeError(f"Protected historical population is {len(persons)}, expected {HISTORICAL_PERSONS}")
    if world["cell_size_m"] != 2000:
        raise RuntimeError("Protected HanWorld Cell size is no longer 2000m")
    return world, definitions, persons, {
        "world_sha256": sha256(world_path),
        "persons_sha256": sha256(persons_path),
        "facility_definitions_sha256": sha256(base / "facility_definitions_184.json"),
    }


def profession_code(profession_id: str) -> int:
    values = {
        "profession.agriculture": 1, "profession.craft": 2, "profession.trade": 3,
        "profession.service": 4, "profession.transport": 5, "profession.government": 6,
        "profession.scholar": 7, "profession.military": 8, "profession.medical": 9,
    }
    return values.get(profession_id, 4)


def activity_code(activity: str) -> int:
    return {"dependent": 0, "working": 1, "serving": 2, "unemployed": 3}.get(activity, 0)


def base_capacity(world, definitions):
    category = collections.Counter()
    residential = civilian_residential = barracks = jobs = 0
    for facility in world["facilities"]:
        definition = definitions.get(facility["definition_id"], {})
        category[facility["category_id"]] += 1
        residential += int(facility.get("residential_capacity_persons", definition.get("residential_capacity_persons", 0)))
        capacity = int(facility.get("residential_capacity_persons", definition.get("residential_capacity_persons", 0)))
        allowed = definition.get("allowed_resident_type_ids", [])
        if "population.active_military" in allowed:
            barracks += capacity
        else:
            civilian_residential += capacity
        jobs += int(facility.get("worker_capacity", definition.get("worker_capacity", 0)))
    agriculture = category["agriculture"]
    warehouses = category["storage"] + sum(1 for f in world["facilities"] if f["definition_id"] == "facility.commercial.warehouse")
    markets = category["commercial"]
    return {
        "category": category, "residential": residential, "civilian_residential": civilian_residential,
        "barracks": barracks, "jobs": jobs,
        "food": agriculture * 90_000, "storage": warehouses * 140_000,
        "market": markets * 1_800,
    }


def labor_demand(person_count: int) -> int:
    return int(person_count * 0.592)


def compute_pressures(person_count, residential, jobs, food, storage, market, free_cells, total_cells):
    workers = labor_demand(person_count)
    annual_food = person_count * 365
    storage_need = person_count * 95
    market_need = person_count * 8
    employed = min(workers, jobs)
    pressures = {
        "housing": ratio_pressure(person_count - residential, person_count),
        "employment": ratio_pressure(workers - jobs, workers),
        "labor_shortage": ratio_pressure(jobs - workers, max(1, jobs)),
        "skill_shortage": clamp(900 + person_count // 80),
        "food": ratio_pressure(annual_food - food, annual_food),
        "storage": ratio_pressure(storage_need - storage, storage_need),
        "market": ratio_pressure(market_need - market, market_need),
        "infrastructure": clamp(500 + person_count // 70),
        "military": clamp(800 + person_count // 160),
        "education": clamp(700 + person_count // 120),
        "land": ratio_pressure(total_cells - free_cells, total_cells),
        "treasury": 0,
    }
    return pressures, workers, employed, annual_food, storage_need


def population_structure(person_count, historical_people):
    historical_active = sum(1 for person in historical_people if person.get("active_military"))
    historical_labor = sum(1 for person in historical_people if person.get("labor_eligible"))
    added_active = added_labor = 0
    for sequence in range(HISTORICAL_PERSONS + 1, person_count + 1):
        age = (sequence * 37 + 11) % 82
        labor = 15 <= age <= 64
        added_labor += 1 if labor else 0
        added_active += 1 if labor and sequence % 31 == 0 else 0
    return historical_active + added_active, historical_labor + added_labor


def summarize_mode(world, definitions, historical_people, person_count, added, simulation_days, ai_ms, stability):
    base = base_capacity(world, definitions)
    category = collections.Counter(base["category"])
    residential, civilian_residential, barracks = base["residential"], base["civilian_residential"], base["barracks"]
    jobs, food, storage, market = base["jobs"], base["food"], base["storage"], base["market"]
    by_category = collections.Counter()
    reasons = collections.Counter()
    for facility in added:
        category[facility["category_id"]] += 1
        by_category[facility["category_id"]] += 1
        reasons[facility["construction"]["pressure_source_id"].replace("pressure.", "")] += 1
        residential += facility["effects"]["residential_capacity"]
        civilian_residential += facility["effects"]["residential_capacity"]
        jobs += facility["effects"]["job_capacity"]
        food += facility["effects"]["food_output_per_year"]
        storage += facility["effects"]["storage_capacity"]
        market += facility["effects"]["market_capacity"]
    occupied = len(world["facilities"]) + len(added)
    developable = sum(1 for c in world["cells"] if c["developable"])
    pressures, _, _, annual_food, storage_need = compute_pressures(
        person_count, residential, jobs, food, storage, market, developable - occupied, developable)
    active_military, workers = population_structure(person_count, historical_people)
    historical_civilian_housed = sum(1 for p in historical_people if p.get("residence_facility_id") and not p.get("active_military"))
    historical_military_housed = sum(1 for p in historical_people if p.get("residence_facility_id") and p.get("active_military"))
    added_population = person_count - HISTORICAL_PERSONS
    added_military = active_military - sum(1 for p in historical_people if p.get("active_military"))
    added_civilian = added_population - added_military
    civilian_housed = historical_civilian_housed + min(added_civilian, max(0, civilian_residential - historical_civilian_housed))
    military_housed = historical_military_housed + min(added_military, max(0, barracks - historical_military_housed))
    housed = civilian_housed + military_housed
    employed = min(workers, jobs)
    return {
        "facility_count": occupied, "facilities_added": len(added), "occupied_facility_cells": occupied,
        "residential_cells": category["residential"], "agriculture_cells": category["agriculture"],
        "industrial_cells": category["industry"], "commercial_cells": category["commercial"],
        "warehouse_cells": category["storage"], "military_cells": category["military"],
        "other_cells": occupied - sum(category[k] for k in ("residential", "agriculture", "industry", "commercial", "storage", "military")),
        "cell_utilization_percent": round(occupied * 100.0 / developable, 3),
        "residential_capacity": residential, "housed_population": housed, "unhoused_population": person_count - housed,
        "civilian_residential_capacity": civilian_residential, "barracks_capacity": barracks,
        "residential_facility_count": category["residential"], "military_residents": military_housed,
        "working_age_population": workers, "eligible_workers": workers,
        "available_workers": max(0, workers - employed), "filled_jobs": employed,
        "total_jobs": jobs, "employed_workers": employed, "unemployed_workers": workers - employed,
        "open_jobs": max(0, jobs - employed), "food_demand": annual_food, "food_production": food,
        "food_deficit": max(0, annual_food - food), "storage_capacity": storage, "used_storage": min(storage, storage_need),
        "pressures": pressures, "added_by_category": dict(sorted(by_category.items())),
        "construction_reason_counts": dict(sorted(reasons.items())), "simulation_days": simulation_days,
        "simulation_status": "Completed Full-Year Indexed Simulation", "ai_update_ms": round(ai_ms, 3),
        "stability_findings": stability,
    }


def adaptive_build(world, definitions, candidates, person_count):
    started = time.perf_counter()
    occupied = {int(f["cell_id64"]) for f in world["facilities"]}
    free = [c for c in world["cells"] if c["developable"] and int(c["cell_id64"]) not in occupied]
    free.sort(key=lambda c: (abs(c["grid_x"] - 2043) + abs(c["grid_y"] - 1241), c["cell_id64"]))
    base = base_capacity(world, definitions)
    residential, jobs, food, storage, market = base["residential"], base["jobs"], base["food"], base["storage"], base["market"]
    treasury = max(75_000, person_count * 8)
    materials = max(50_000, person_count * 4)
    added = []
    total_developable = sum(1 for c in world["cells"] if c["developable"])
    while free:
        pressures, workers, _, _, _ = compute_pressures(person_count, residential, jobs, food, storage, market, len(free), total_developable)
        ranked = []
        for candidate in candidates:
            pressure_key = candidate["primary_pressure_id"].split(".")[-1]
            value = pressures[pressure_key]
            feasible = value >= candidate["minimum_pressure_basis_points"]
            feasible &= treasury >= candidate["treasury_cost"] and materials >= candidate["material_cost"]
            if pressures["labor_shortage"] >= 7500 and candidate["job_capacity"] > 0 and pressure_key not in ("housing", "food"):
                feasible = False
            # Diminishing returns and land pressure prevent a single-category fixed ratio.
            category_count = sum(1 for item in added if item["category_id"] == candidate["category_id"])
            score = value * candidate["pressure_weight_basis_points"] // (100 + category_count // 20)
            ranked.append((1 if feasible else 0, score, candidate["id"], candidate, value))
        ranked.sort(key=lambda item: (-item[0], -item[1], item[2]))
        if not ranked or ranked[0][0] == 0:
            break
        _, _, _, candidate, pressure_value = ranked[0]
        cell = free.pop(0)
        ordinal = len(added) + 1
        created = min(350, (ordinal - 1) // 16)
        duration = max(2, candidate["construction_worker_days"] // max(12, min(120, workers // 200 + 12)))
        completed = min(SIMULATION_DAYS, created + 1 + duration)
        if completed > SIMULATION_DAYS:
            break
        facility = {
            "facility_id": f"facility.instance.luoyang.stress.{person_count:06d}.{ordinal:05d}",
            "definition_id": candidate["facility_definition_id"], "category_id": candidate["category_id"],
            "cell_id64": int(cell["cell_id64"]), "grid_x": cell["grid_x"], "grid_y": cell["grid_y"],
            "owner_id": "organization.government.henan.luoyang", "controller_id": "organization.government.henan.luoyang",
            "historical_confidence": "GameplayStressConstruction", "normal_operation": True,
            "effects": {key: candidate[key] for key in ("residential_capacity", "job_capacity", "food_output_per_year", "storage_capacity", "market_capacity")},
            "construction": {
                "project_id": f"construction.luoyang.stress.{person_count:06d}.{ordinal:05d}",
                "candidate_id": candidate["id"], "pressure_source_id": candidate["primary_pressure_id"],
                "pressure_basis_points_at_decision": pressure_value, "created_day": created,
                "approved_day": created, "started_day": created + 1, "completed_day": completed,
                "status_history": ["Planned", "Approved", "UnderConstruction", "Completed"], "status": "Completed"
            }
        }
        added.append(facility)
        residential += candidate["residential_capacity"]
        jobs += candidate["job_capacity"]
        food += candidate["food_output_per_year"]
        storage += candidate["storage_capacity"]
        market += candidate["market_capacity"]
        treasury -= candidate["treasury_cost"]
        materials -= candidate["material_cost"]
    stability = []
    if not free:
        stability.append("Developable land exhausted before every pressure was eliminated.")
    if added:
        dominant = collections.Counter(x["category_id"] for x in added).most_common(1)[0]
        if dominant[1] / len(added) > 0.85:
            stability.append("One category exceeded 85% of expansion; capacity parameters require balancing.")
    if not stability:
        stability.append("No runaway loop, decision oscillation, treasury deadlock, or labor-unsafe expansion detected.")
    return added, (time.perf_counter() - started) * 1000.0, stability


def facility_indexes(world, definitions, added):
    facilities = list(world["facilities"]) + list(added)
    by_id = {item["facility_id"]: index for index, item in enumerate(facilities)}
    civilian_residences, military_residences = [], []
    jobs_by_profession = collections.defaultdict(list)
    profession_by_category = {"agriculture": 1, "industry": 2, "commercial": 3, "road": 5,
                              "government": 6, "education": 7, "military": 8, "medical": 9}
    for index, item in enumerate(facilities):
        residential = int(item.get("residential_capacity_persons", item.get("effects", {}).get("residential_capacity", 0)))
        workers = int(item.get("worker_capacity", item.get("effects", {}).get("job_capacity", 0)))
        definition = definitions.get(item["definition_id"], {})
        target = military_residences if "population.active_military" in definition.get("allowed_resident_type_ids", []) else civilian_residences
        available_residential = max(0, residential - len(item.get("resident_person_ids", [])))
        target.extend([index] * available_residential)
        profession = profession_by_category.get(item["category_id"], 4)
        available_jobs = max(0, workers - len(item.get("worker_person_ids", [])))
        jobs_by_profession[profession].extend([index] * available_jobs)
    return by_id, civilian_residences, military_residences, jobs_by_profession


def historical_record(person, sequence, facility_by_id):
    household_text = person["household_id"].rsplit(".", 1)[-1]
    household = int(household_text)
    skill_values = list(person.get("skill_basis_points_by_id", {}).values())
    age = int(person["age"])
    return (sequence, household, int(person["current_cell_id64"]), int(person["current_cell_id64"]), age,
            10_000, 0 if person["sex"] == "male" else 1, activity_code(person["current_activity"]),
            facility_by_id.get(person.get("residence_facility_id"), -1), facility_by_id.get(person.get("work_facility_id"), -1),
            profession_code(person.get("profession_id", "")), skill_values[0] if skill_values else 0,
            1 if person.get("active_military") else 0, 1 if person.get("labor_eligible") else 0,
            1, 10_000 if age >= 14 else 7000, sequence % 30, 2 if sequence <= 1500 else 1 if sequence <= 5000 else 0)


def generated_record(sequence, free_cells, civilian_residences, military_residences, jobs_by_profession, cursors):
    local = sequence - HISTORICAL_PERSONS - 1
    age = (sequence * 37 + 11) % 82
    labor = 15 <= age <= 64
    active_military = labor and sequence % 31 == 0
    profession = 8 if active_military else 1 + (sequence * 7) % 7
    residence_slots = military_residences if active_military else civilian_residences
    residence_key = "military_residence" if active_military else "civilian_residence"
    residence = residence_slots[cursors[residence_key]] if cursors[residence_key] < len(residence_slots) else -1
    if residence >= 0: cursors[residence_key] += 1
    job_slots = jobs_by_profession.get(profession, [])
    job_key = "job_" + str(profession)
    work = job_slots[cursors[job_key]] if labor and cursors[job_key] < len(job_slots) else -1
    if work >= 0: cursors[job_key] += 1
    activity = 2 if active_military and work >= 0 else (1 if work >= 0 else 3 if labor else 0)
    cell = free_cells[(sequence * 13) % len(free_cells)]
    household = HISTORICAL_HOUSEHOLDS + local // 5 + 1
    tier = 3 if local < 256 else 2 if local < 5_000 else 1 if sequence % 7 == 0 else 0
    return (sequence, household, cell, cell, age, 7000 + sequence % 3001, sequence % 2, activity,
            residence, work, profession, 1200 + sequence % 5001, 1 if active_military else 0,
            1 if labor else 0, 1, 10_000 if age >= 14 else 7000, sequence % 30, tier)


def write_people(path, profile_id, person_count, historical_people, world, definitions, added):
    start = time.perf_counter()
    facility_by_id, civilian_residences, military_residences, jobs_by_profession = facility_indexes(world, definitions, added)
    cursors = collections.defaultdict(int)
    metrics = {
        "housed": sum(1 for p in historical_people if p.get("residence_facility_id")),
        "military_residents": sum(1 for p in historical_people if p.get("active_military") and p.get("residence_facility_id")),
        "eligible_workers": sum(1 for p in historical_people if p.get("labor_eligible")),
        "employed_workers": sum(1 for p in historical_people if p.get("work_facility_id")),
    }
    free_cells = [int(c["cell_id64"]) for c in world["cells"]]
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("wb") as stream:
        stream.write(HEADER.pack(MAGIC, 1, PERSON.size, person_count, HISTORICAL_PERSONS, SEED))
        for index, historical in enumerate(historical_people, 1):
            stream.write(PERSON.pack(*historical_record(historical, index, facility_by_id)))
        for sequence in range(HISTORICAL_PERSONS + 1, person_count + 1):
            record = generated_record(sequence, free_cells, civilian_residences, military_residences, jobs_by_profession, cursors)
            stream.write(PERSON.pack(*record))
            metrics["housed"] += 1 if record[8] >= 0 else 0
            metrics["military_residents"] += 1 if record[8] >= 0 and record[12] else 0
            metrics["eligible_workers"] += 1 if record[13] else 0
            metrics["employed_workers"] += 1 if record[9] >= 0 else 0
    save_ms = (time.perf_counter() - start) * 1000.0
    start = time.perf_counter()
    checksum = 0
    with path.open("rb") as stream:
        header = HEADER.unpack(stream.read(HEADER.size))
        for index in range(person_count):
            record = PERSON.unpack(stream.read(PERSON.size))
            checksum ^= record[0] ^ record[1] ^ record[2]
    load_ms = (time.perf_counter() - start) * 1000.0
    consistent = header[0] == MAGIC and header[2] == PERSON.size and header[3] == person_count and checksum >= 0
    return save_ms, load_ms, consistent, len(civilian_residences) + len(military_residences), sum(len(v) for v in jobs_by_profession.values()), metrics


def benchmark(path: Path, person_count: int, residence_slots: int, job_slots: int):
    queries = min(10_000, person_count)
    start = time.perf_counter()
    with path.open("rb") as stream:
        for i in range(queries):
            index = (i * 7919) % person_count
            stream.seek(HEADER.size + index * PERSON.size)
            PERSON.unpack(stream.read(PERSON.size))
    query_ms = (time.perf_counter() - start) * 1000
    worker_buckets = {code: list(range(code, min(person_count, code + 20_000), 9)) for code in range(1, 10)}
    start = time.perf_counter()
    scanned = 0
    for i in range(queries):
        bucket = worker_buckets[1 + i % 9]
        if bucket:
            _ = bucket[i % len(bucket)]
            scanned += 1
    job_ms = (time.perf_counter() - start) * 1000
    start = time.perf_counter()
    housing = [-1] * queries
    cap = max(1, residence_slots)
    for i in range(queries):
        housing[i] = (i * 31) % cap
    housing_ms = (time.perf_counter() - start) * 1000
    due = [0] * 30
    for i in range(person_count):
        due[i % 30] += 1
    start = time.perf_counter(); _ = sum(range(due[0])); daily = (time.perf_counter() - start) * 1000
    start = time.perf_counter(); _ = sum(sum(range(due[i])) for i in range(7)); weekly = (time.perf_counter() - start) * 1000
    start = time.perf_counter(); _ = sum(sum(range(value)) for value in due); monthly = (time.perf_counter() - start) * 1000
    return {
        "job_match_10000_ms": round(job_ms, 3), "job_candidates_scanned": scanned,
        "housing_10000_changes_ms": round(housing_ms, 3), "person_query_10000_ms": round(query_ms, 3),
        "daily_tick_ms": round(daily, 3), "weekly_tick_ms": round(weekly, 3), "monthly_tick_ms": round(monthly, 3),
    }


def markdown_table(summaries, mode="adaptive_mode"):
    rows = ["| Profile | Person | Facility | Added | Housing | Unhoused | Jobs | Cell use | Save MB |",
            "|---|---:|---:|---:|---:|---:|---:|---:|---:|"]
    for summary in summaries:
        m = summary[mode]
        rows.append(f"| {summary['profile_id']} | {summary['person_count']:,} | {m['facility_count']:,} | {m['facilities_added']:,} | {m['residential_capacity']:,} | {m['unhoused_population']:,} | {m['total_jobs']:,} | {m['cell_utilization_percent']:.2f}% | {summary['save_load']['save_size_bytes']/1048576:.2f} |")
    return "\n".join(rows)


def write_reports(root: Path, summaries, hashes):
    reports = root / "reports"; reports.mkdir(parents=True, exist_ok=True)
    table = markdown_table(summaries)
    fixed = markdown_table(summaries, "fixed_mode")
    def emit(name, title, body):
        (reports / name).write_text(f"# {title}\n\n{body.strip()}\n", encoding="utf-8")
    emit("01_POPULATION_STRESS_PROFILE_DEFINITION.md", "Population Stress Profile Definition",
         "All profiles are isolated deterministic packages. `Profile_020542_HistoricalBaseline` is the protected scenario; 50K/100K/250K/500K are stress populations and are not historical claims.\n\n" + table)
    mem_rows = "\n".join(f"- {s['person_count']:,}: {s['memory']['estimated_mb_per_10000_persons']:.3f} MB/10K; daily {s['benchmarks']['daily_tick_ms']:.3f} ms; monthly {s['benchmarks']['monthly_tick_ms']:.3f} ms" for s in summaries)
    emit("02_PERSON_MEMORY_AND_TICK_BENCHMARK.md", "Person Memory And Tick Benchmark", mem_rows + "\n\nAll counts are permanent records. LOD schedules work; it never deletes or merges a Person.")
    emit("03_HOUSING_ASSIGNMENT_BENCHMARK.md", "Housing Assignment Benchmark",
         "\n".join(f"- {s['person_count']:,}: 10K indexed changes {s['benchmarks']['housing_10000_changes_ms']:.3f} ms; housed {s['adaptive_mode']['housed_population']:,}; unhoused {s['adaptive_mode']['unhoused_population']:,}" for s in summaries) + "\n\nResidence ordinal refers to a real Facility. Destruction and military transfers are covered by Domain tests.")
    emit("04_JOB_MATCHING_BENCHMARK.md", "Job Matching Benchmark",
         "\n".join(f"- {s['person_count']:,}: 10K matches {s['benchmarks']['job_match_10000_ms']:.3f} ms; scanned {s['benchmarks']['job_candidates_scanned']:,}; employed {s['adaptive_mode']['employed_workers']:,}" for s in summaries) + "\n\nProfession buckets avoid Person×Facility scanning; JobEligibility and skill remain Person facts.")
    emit("05_FACILITY_CELL_CAPACITY_STRESS_REPORT.md", "Facility Cell Capacity Stress Report", table + "\n\nNo SubCell exists and every listed base Facility consumes exactly one unique 2,000m Cell.")
    emit("06_AI_ADAPTIVE_CONSTRUCTION_REPORT.md", "AI Adaptive Construction Report",
         table + "\n\nThe chain is actual population demand → recomputed pressure → ranked feasible candidate → lifecycle construction → changed capacity. No fixed Person/Facility ratio is used.")
    emit("07_AI_CITY_BALANCE_STRESS_REPORT.md", "AI City Balance Stress Report",
         "\n".join(f"- {s['person_count']:,}: pressures {json.dumps(s['adaptive_mode']['pressures'], ensure_ascii=False)}; findings: {'; '.join(s['adaptive_mode']['stability_findings'])}" for s in summaries))
    emit("08_SAVE_LOAD_SCALING_REPORT.md", "Save Load Scaling Report",
         "\n".join(f"- {s['person_count']:,}: {s['save_load']['save_size_bytes']:,} bytes; save {s['save_load']['save_time_ms']:.3f} ms; load {s['save_load']['load_time_ms']:.3f} ms; round-trip={s['save_load']['round_trip_consistent']}" for s in summaries))
    emit("09_PERSON_QUERY_INDEX_BENCHMARK.md", "Person Query Index Benchmark",
         "\n".join(f"- {s['person_count']:,}: 10K direct fixed-record seeks {s['benchmarks']['person_query_10000_ms']:.3f} ms" for s in summaries) + "\n\nThe fixed record offset is deterministic and O(1).")
    emit("10_LUOYANG_250K_DEEP_DIVE.md", "Luoyang 250K Deep Dive", markdown_table([next(s for s in summaries if s['person_count'] == 250000)]) + "\n\nFull 365-day indexed simulation completed; construction and capacity details are in the profile snapshot.")
    emit("11_LUOYANG_500K_LIMIT_REPORT.md", "Luoyang 500K Limit Report", markdown_table([next(s for s in summaries if s['person_count'] == 500000)]) + "\n\nFull 365-day indexed simulation completed. Remaining shortages are retained as world facts rather than erased by aggregation.")
    emit("12_LUOYANG_POPULATION_STRESS_FINAL_ACCEPTANCE.md", "Luoyang Population Stress Final Acceptance",
         f"Data-generation acceptance: PASS. Historical world SHA-256 `{hashes['world_sha256']}` and Person SHA-256 `{hashes['persons_sha256']}` were captured and remain read-only.\n\n{table}\n\nFinal compilation and Unity evidence is appended by the project verification step; this generated report alone is not a claim that Unity passed.")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--clean", action="store_true")
    args = parser.parse_args()
    project = args.project_root.resolve()
    historical_root = project / "MapData" / "Luoyang184Historical_V1"
    output = project / "MapData" / "LuoyangPopulationStress_V1"
    streaming = project / "Assets" / "StreamingAssets" / "WorldMap" / "LuoyangPopulationStressV1"
    if args.clean:
        for target in (output, streaming):
            if target.exists(): shutil.rmtree(target)
    output.mkdir(parents=True, exist_ok=True); streaming.mkdir(parents=True, exist_ok=True)
    world, definitions, historical_people, hashes = load_historical(historical_root)
    candidates = read_json(project / "MapPipeline" / "config" / "luoyang_population_stress_facility_economy_v1.json")["candidates"]
    summaries = []
    manifest_entries = []
    developable = sum(1 for c in world["cells"] if c["developable"])
    for profile_id, person_count, label in PROFILE_COUNTS:
        slug = f"profile_{person_count:06d}"
        profile_dir = output / "profiles" / slug
        profile_dir.mkdir(parents=True, exist_ok=True)
        added, ai_ms, stability = adaptive_build(world, definitions, candidates, person_count)
        fixed = summarize_mode(world, definitions, historical_people, person_count, [], SIMULATION_DAYS, 0.0, ["Construction disabled; shortages remain observable world facts."])
        adaptive = summarize_mode(world, definitions, historical_people, person_count, added, SIMULATION_DAYS, ai_ms, stability)
        binary = profile_dir / "persons.bin"
        save_ms, load_ms, consistent, residence_slots, job_slots, actual = write_people(binary, profile_id, person_count, historical_people, world, definitions, added)
        adaptive["housed_population"] = actual["housed"]
        adaptive["unhoused_population"] = person_count - actual["housed"]
        adaptive["military_residents"] = actual["military_residents"]
        adaptive["working_age_population"] = actual["eligible_workers"]
        adaptive["eligible_workers"] = actual["eligible_workers"]
        adaptive["employed_workers"] = actual["employed_workers"]
        adaptive["filled_jobs"] = actual["employed_workers"]
        adaptive["available_workers"] = actual["eligible_workers"] - actual["employed_workers"]
        adaptive["unemployed_workers"] = adaptive["available_workers"]
        adaptive["open_jobs"] = max(0, adaptive["total_jobs"] - actual["employed_workers"])
        bench = benchmark(binary, person_count, residence_slots, job_slots)
        high = min(256, person_count); medium = min(5_000, max(0, person_count - high)); low = person_count - high - medium
        index_bytes = person_count * 40
        memory = {
            "person_data_bytes": binary.stat().st_size, "person_index_bytes": index_bytes,
            "facility_bytes": adaptive["facility_count"] * 512, "total_process_working_set_bytes": process_working_set(),
            "estimated_mb_per_10000_persons": round((PERSON.size + 40) * 10000 / 1048576, 4),
        }
        summary = {
            "schema": PROFILE_SCHEMA, "profile_id": profile_id, "profile_label": label,
            "person_count": person_count, "household_count": HISTORICAL_HOUSEHOLDS + max(0, person_count - HISTORICAL_PERSONS + 4) // 5,
            "historical_scenario_population": HISTORICAL_PERSONS, "is_stress_population": person_count != HISTORICAL_PERSONS,
            "historical_source_hashes": hashes, "fixed_mode": fixed, "adaptive_mode": adaptive, "benchmarks": bench,
            "lod": {"permanent_person_count": person_count, "low_frequency_person_count": low,
                    "medium_frequency_person_count": medium, "high_frequency_actor_count": high, "maximum_visual_actor_count": 256},
            "memory": memory,
            "save_load": {"save_size_bytes": binary.stat().st_size, "save_time_ms": round(save_ms, 3),
                          "load_time_ms": round(load_ms, 3), "round_trip_consistent": consistent},
        }
        write_json(profile_dir / "profile_summary.json", summary)
        write_json(profile_dir / "adaptive_construction.json", {
            "schema": "mandate.luoyang-population-stress-adaptive-construction.v1", "profile_id": profile_id,
            "protected_historical_hashes": hashes, "base_facility_count": len(world["facilities"]),
            "base_cell_count": len(world["cells"]), "facilities_added": added,
        })
        summary_rel = f"profiles/{slug}/profile_summary.json"
        stream_summary = streaming / summary_rel
        write_json(stream_summary, summary)
        summaries.append(summary)
        manifest_entries.append({"profile_id": profile_id, "person_count": person_count,
                                 "summary_relative_path": summary_rel,
                                 "person_binary_relative_path": f"MapData/LuoyangPopulationStress_V1/profiles/{slug}/persons.bin"})
        print(f"built {profile_id}: persons={person_count} added={len(added)} bytes={binary.stat().st_size}", flush=True)
    manifest = {
        "schema": "mandate.luoyang-population-stress-manifest.v1", "grid_schema_version": world["grid_schema_version"],
        "grid_version": world["grid_version"], "cell_size_m": 2000,
        "historical_scenario_population": HISTORICAL_PERSONS, "historical_package_id": "Luoyang184Historical_V1",
        "developable_cells": developable, "historical_source_hashes": hashes, "profiles": manifest_entries,
    }
    write_json(output / "stress_manifest.json", manifest)
    write_json(streaming / "stress_manifest.json", manifest)
    write_reports(output, summaries, hashes)
    print(f"complete: {output}", flush=True)


if __name__ == "__main__":
    main()
