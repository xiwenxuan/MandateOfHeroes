#!/usr/bin/env python3
"""Build the deterministic HAN-135-260 national population distribution package.

The builder preserves the M13 source tables, reconciles their spatial weights to the
140 national anchor without overwriting source values, and writes one runtime shard
per year.  It never materializes Permanent Persons.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
import shutil
import time
from collections import defaultdict
from pathlib import Path


SCHEMA = "mandate.han-national-population-dataset.v1"
REGION_SCHEMA = "mandate.han-national-population-year.v1"


def load_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def load_csv(path: Path):
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def write_json(path: Path, value, *, compact: bool = False):
    path.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(
        value,
        ensure_ascii=False,
        sort_keys=False,
        separators=(",", ":") if compact else None,
        indent=None if compact else 2,
    )
    path.write_text(text + "\n", encoding="utf-8")


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def stable_unit(value: str, salt: str = "") -> float:
    digest = hashlib.sha256((salt + "|" + value).encode("utf-8")).digest()
    return int.from_bytes(digest[:8], "big") / float((1 << 64) - 1)


def lerp_anchors(anchor_map, year: int) -> float:
    anchors = sorted((int(key), float(value)) for key, value in anchor_map.items())
    if year <= anchors[0][0]:
        return anchors[0][1]
    if year >= anchors[-1][0]:
        return anchors[-1][1]
    for index in range(1, len(anchors)):
        right_year, right_value = anchors[index]
        if year <= right_year:
            left_year, left_value = anchors[index - 1]
            ratio = (year - left_year) / (right_year - left_year)
            return left_value + (right_value - left_value) * ratio
    raise AssertionError("unreachable")


def allocate_exact(total: int, weighted_ids):
    weighted = [(str(item_id), max(0.0, float(weight))) for item_id, weight in weighted_ids]
    weight_sum = sum(weight for _, weight in weighted)
    if total < 0 or weight_sum <= 0:
        raise ValueError("Allocation requires non-negative total and positive weights")
    raw = [(item_id, total * weight / weight_sum) for item_id, weight in weighted]
    result = {item_id: int(math.floor(value)) for item_id, value in raw}
    remainder = total - sum(result.values())
    order = sorted(raw, key=lambda pair: (-(pair[1] - math.floor(pair[1])), pair[0]))
    for item_id, _ in order[:remainder]:
        result[item_id] += 1
    return result


def allocate_signed(total: int, weighted_ids):
    if total >= 0:
        return allocate_exact(total, weighted_ids)
    positive = allocate_exact(-total, weighted_ids)
    return {key: -value for key, value in positive.items()}


def split_exact(total: int, shares):
    return allocate_exact(total, shares)


def effective_population(record):
    corrected = record.get("registered_population_corrected")
    raw = record.get("registered_population_raw")
    value = corrected if corrected not in (None, "") else raw
    if value in (None, ""):
        raise ValueError("Population record has no usable value: " + record["admin_unit_id"])
    return int(value)


def classify_region(unit):
    kind = unit["unit_type"]
    name = unit["canonical_name"]
    if kind == "commandery":
        return "Commandery"
    if kind == "kingdom":
        return "Kingdom"
    if "属国" in name:
        return "Dependency"
    if "尹" in name or name in ("左冯翊", "右扶风"):
        return "CapitalRegion"
    return "OtherHistoricalRegion"


def evidence_for_year(year):
    if year in (140, 157):
        return "H"
    if year in (135, 184, 189, 190, 194, 196, 200, 202, 207, 208, 211, 214, 217, 219, 220, 221, 223, 227, 228, 229, 234, 249, 255, 257, 260):
        return "B"
    return "C"


def active_events(events, year):
    return [event for event in events if int(event["start_year"]) <= year <= int(event["end_year"])]


def event_category(event):
    kind = event["impact_type"]
    if kind == "War":
        return "war"
    if kind == "Epidemic":
        return "epidemic"
    if kind in ("Disaster", "Famine"):
        return "disaster"
    return None


def region_event_exposure(region_id, province_id, events):
    exposure = 0.0
    for event in events:
        provinces = event.get("affected_provinces", [])
        regions = event.get("affected_region_ids", [])
        if province_id in provinces or region_id in regions:
            local = 0.75 + 0.5 * stable_unit(region_id, event["event_id"])
            exposure += int(event["severity_basis_points"]) / 10000.0 * local
    return exposure


def build_model(project_root: Path, runtime_root: Path, output_root: Path):
    started = time.perf_counter()
    data_root = project_root / "Data" / "HistoricalPopulation"
    config = load_json(data_root / "han_135_260_population_model_v1.json")
    events_doc = load_json(data_root / "han_135_260_population_events_v1.json")
    sources_doc = load_json(data_root / "han_135_260_population_sources_v1.json")
    m13_sources = load_json(data_root / "han_140_sources.json")
    m13_audit = load_json(data_root / "han_140_audit_report.json")
    admin_units = load_csv(data_root / "han_140_administrative_units.csv")
    population_records = load_csv(data_root / "han_140_population_records.csv")

    if m13_audit["validation_status"] != "passed":
        raise ValueError("M13 source audit is not passed")
    if int(m13_audit["county_catalog_audit"]["itemized_count"]) != 1182:
        raise ValueError("M13 county catalog must contain exactly 1182 itemized counties")
    if int(config["national_registered_population_140"]) != 49150220:
        raise ValueError("Protected 140 national anchor changed")

    by_id = {row["admin_unit_id"]: row for row in admin_units}
    provinces = sorted((row for row in admin_units if row["unit_type"] == "province"), key=lambda row: row["admin_unit_id"])
    population_by_id = {row["admin_unit_id"]: row for row in population_records}
    regions = sorted((by_id[region_id] for region_id in population_by_id), key=lambda row: row["admin_unit_id"])
    counties = sorted((row for row in admin_units if row["unit_type"] == "county"), key=lambda row: row["admin_unit_id"])
    if len(provinces) != 13 or len(regions) != 105 or len(counties) != 1182:
        raise ValueError(f"Unexpected M13 hierarchy: provinces={len(provinces)} regions={len(regions)} counties={len(counties)}")

    counties_by_region = defaultdict(list)
    for county in counties:
        if county["parent_admin_unit_id"] not in population_by_id:
            raise ValueError("County parent is not a population region: " + county["admin_unit_id"])
        counties_by_region[county["parent_admin_unit_id"]].append(county)

    province_by_region = {row["admin_unit_id"]: row["parent_admin_unit_id"] for row in regions}
    base_region_population = {row["admin_unit_id"]: effective_population(population_by_id[row["admin_unit_id"]]) for row in regions}
    base_total = sum(base_region_population.values())
    anchor_reconciliation = int(config["national_registered_population_140"]) - base_total

    county_weight_details = {}
    weight_config = config["county_weight_parameters"]
    for region in regions:
        region_id = region["admin_unit_id"]
        seat_id = region.get("seat_admin_unit_id", "")
        for county in counties_by_region[region_id]:
            county_id = county["admin_unit_id"]
            fertility = int(round(weight_config["deterministic_fertility_min"] + stable_unit(county_id, "fertility") * (weight_config["deterministic_fertility_max"] - weight_config["deterministic_fertility_min"])))
            water = int(round(weight_config["deterministic_water_min"] + stable_unit(county_id, "water") * (weight_config["deterministic_water_max"] - weight_config["deterministic_water_min"])))
            market = int(round(weight_config["deterministic_market_min"] + stable_unit(county_id, "market") * (weight_config["deterministic_market_max"] - weight_config["deterministic_market_min"])))
            role_bonus = int(weight_config["ordinary_base"])
            if county_id == seat_id:
                role_bonus += int(weight_config["commandery_seat_bonus"])
            if county_id == config["luoyang"]["luoyang_county_id"]:
                role_bonus += int(weight_config["capital_county_bonus"])
            combined = role_bonus * fertility * water * market
            county_weight_details[county_id] = {
                "county_id": county_id,
                "parent_region_id": region_id,
                "role_weight": role_bonus,
                "fertility_weight_basis_points": fertility,
                "water_weight_basis_points": water,
                "market_weight_basis_points": market,
                "combined_weight": combined,
                "is_commandery_seat": county_id == seat_id,
                "is_capital_county": county_id == config["luoyang"]["luoyang_county_id"],
                "method": "seat_role_x_deterministic_geography_proxy_v1",
                "confidence": "C",
            }

    year_start = int(config["year_start"])
    year_end = int(config["year_end"])
    years = list(range(year_start, year_end + 1))
    actual_national = {year: int(round(lerp_anchors(config["actual_population_start_anchors"], year))) for year in range(year_start, year_end + 2)}
    registered_national = {}
    for year in range(year_start, year_end + 2):
        value = int(round(actual_national[year] * lerp_anchors(config["registration_coverage_anchors"], year)))
        if year == 140:
            value = int(config["national_registered_population_140"])
        if year == 157:
            value = int(config["national_registered_population_157"])
        registered_national[year] = value

    def region_weights_for_year(year):
        active = active_events(events_doc["events"], year)
        values = []
        for region in regions:
            region_id = region["admin_unit_id"]
            province_id = province_by_region[region_id]
            profile = config["province_profile"][province_id]
            multiplier = lerp_anchors(config["profile_multiplier_anchors"][profile], year)
            divergence = 1.0 + (stable_unit(region_id, "regional-resilience") - 0.5) * 0.12 * min(1.0, abs(year - 140) / 80.0)
            event_factor = max(0.65, 1.0 - 0.35 * region_event_exposure(region_id, province_id, active))
            values.append((region_id, base_region_population[region_id] * multiplier * divergence * event_factor))
        return values

    actual_by_year_region = {}
    registered_by_year_region = {}
    for year in range(year_start, year_end + 2):
        actual_alloc = allocate_exact(actual_national[year], region_weights_for_year(year))
        registered_weights = []
        for region_id, actual in actual_alloc.items():
            province_id = province_by_region[region_id]
            profile = config["province_profile"][province_id]
            administrative_factor = {
                "heartland": 1.03, "north_plain": 1.02, "east_plain": 1.0,
                "northeast": 0.95, "northwest_frontier": 0.88, "central_south": 0.96,
                "lower_yangtze": 0.91, "southwest": 0.90, "far_south": 0.78,
            }[profile]
            administrative_factor *= 0.96 + stable_unit(region_id, "registration") * 0.08
            registered_weights.append((region_id, actual * administrative_factor))
        registered_alloc = allocate_exact(registered_national[year], registered_weights)
        actual_by_year_region[year] = actual_alloc
        registered_by_year_region[year] = registered_alloc

    runtime_root.mkdir(parents=True, exist_ok=True)
    output_root.mkdir(parents=True, exist_ok=True)
    years_root = runtime_root / "years"
    scenarios_root = runtime_root / "scenarios"
    years_root.mkdir(parents=True, exist_ok=True)
    scenarios_root.mkdir(parents=True, exist_ok=True)
    write_json(runtime_root / "sources.json", {"schema": sources_doc["schema"], "sources": m13_sources["sources"] + sources_doc["sources"]})
    write_json(runtime_root / "events.json", events_doc)
    write_json(runtime_root / "model_config.json", config)
    write_json(runtime_root / "county_weights.json", {"schema": "mandate.county-population-weights.v1", "weights": [county_weight_details[key] for key in sorted(county_weight_details)]})

    administrative_timeline = []
    for region in regions:
        administrative_timeline.append({
            "region_permanent_id": region["admin_unit_id"],
            "historical_name": region["canonical_name"],
            "region_type": classify_region(region),
            "parent_region_permanent_id": region["parent_admin_unit_id"],
            "valid_from_year": 135,
            "valid_to_year": 260,
            "predecessor_region_id": None,
            "successor_region_id": None,
            "territory_mapping": "M13_140_PERMANENT_GEOGRAPHY_CONTINUITY",
            "population_mapping_rule": "population_remains_on_permanent_geography_when_controller_or_name_changes",
            "confidence": "B" if region["confidence"] == "high" else "C",
            "source": "source.hou_han_shu.jun_guo_zhi",
            "notes": "V1以140行政截面作为永久地理索引；135—260具体改名、分合可追加版本化别名，不改变人口身份。",
        })
    for county in counties:
        administrative_timeline.append({
            "region_permanent_id": county["admin_unit_id"],
            "historical_name": county["canonical_name"],
            "region_type": "County",
            "parent_region_permanent_id": county["parent_admin_unit_id"],
            "valid_from_year": 135,
            "valid_to_year": 260,
            "predecessor_region_id": None,
            "successor_region_id": None,
            "territory_mapping": "M13_1182_COUNTY_PERMANENT_ID",
            "population_mapping_rule": "stable_county_identity_with_time_versioned_administration",
            "confidence": "B",
            "source": "source.hou_han_shu.jun_guo_zhi",
            "notes": "保留M13县永久ID；当前V1未声称已考定126年内每次县级沿革。",
        })
    write_json(runtime_root / "administrative_timeline.json", {"schema": "mandate.administrative-population-timeline.v1", "records": administrative_timeline})

    annual_rows = []
    conservation_rows = []
    city_rows = []
    year_read_timings = []
    city_names = [
        ("洛阳", ("雒阳", "洛阳")), ("长安", ("长安",)), ("邺", ("邺",)),
        ("许", ("许", "许昌")), ("成都", ("成都",)), ("襄阳", ("襄阳",)),
        ("江陵", ("江陵",)), ("建业", ("秣陵", "建业")), ("武昌", ("鄂", "武昌")),
        ("寿春", ("寿春",)), ("宛", ("宛",)), ("下邳", ("下邳",)),
        ("陈留", ("陈留",)), ("临淄", ("临淄",)),
    ]
    county_by_name = defaultdict(list)
    for county in counties:
        county_by_name[county["canonical_name"]].append(county)

    for year in years:
        build_year_started = time.perf_counter()
        actual_start = actual_by_year_region[year]
        actual_end = actual_by_year_region[year + 1]
        registered_start = registered_by_year_region[year]
        registered_end = registered_by_year_region[year + 1]
        active = active_events(events_doc["events"], year)

        birth_rate = lerp_anchors(config["birth_rate_anchors"], year)
        event_deaths = {"war": defaultdict(int), "epidemic": defaultdict(int), "disaster": defaultdict(int)}
        for event in active:
            category = event_category(event)
            if not category:
                continue
            eligible = []
            for region in regions:
                region_id = region["admin_unit_id"]
                province_id = province_by_region[region_id]
                if province_id in event.get("affected_provinces", []) or region_id in event.get("affected_region_ids", []):
                    exposure = 0.75 + 0.5 * stable_unit(region_id, event["event_id"])
                    eligible.append((region_id, actual_start[region_id] * exposure))
            if not eligible:
                continue
            base_population = sum(actual_start[region_id] for region_id, _ in eligible)
            death_total = int(round(base_population * int(event["severity_basis_points"]) / 10000.0 * int(event["mortality_share_basis_points"]) / 10000.0))
            allocated = allocate_exact(death_total, eligible)
            for region_id, value in allocated.items():
                event_deaths[category][region_id] += value

        births_total = int(round(actual_national[year] * birth_rate))
        births = allocate_exact(births_total, [(region_id, actual_start[region_id] * (0.94 + stable_unit(region_id, "birth") * 0.12)) for region_id in actual_start])
        war_total = sum(event_deaths["war"].values())
        epidemic_total = sum(event_deaths["epidemic"].values())
        disaster_total = sum(event_deaths["disaster"].values())
        required_total_deaths = actual_national[year] + births_total - actual_national[year + 1]
        natural_deaths_total = required_total_deaths - war_total - epidemic_total - disaster_total
        if natural_deaths_total < 0:
            raise ValueError(f"Negative natural deaths in {year}: {natural_deaths_total}")
        natural_deaths = allocate_exact(natural_deaths_total, [(region_id, actual_start[region_id] * (0.96 + stable_unit(region_id, "mortality") * 0.08)) for region_id in actual_start])

        net_migration = {}
        for region_id in actual_start:
            delta = actual_end[region_id] - actual_start[region_id]
            net_migration[region_id] = delta - births[region_id] + natural_deaths[region_id] + event_deaths["war"][region_id] + event_deaths["epidemic"][region_id] + event_deaths["disaster"][region_id]
        if sum(net_migration.values()) != 0:
            raise AssertionError("National migration is not conserved")

        region_rows = []
        county_rows = []
        province_acc = defaultdict(lambda: defaultdict(int))
        for region in regions:
            region_id = region["admin_unit_id"]
            province_id = province_by_region[region_id]
            reg_delta = registered_end[region_id] - registered_start[region_id]
            expected_registered_demographic_delta = int(round((actual_end[region_id] - actual_start[region_id]) * (registered_start[region_id] / max(1, actual_start[region_id]))))
            registration_residual = reg_delta - expected_registered_demographic_delta
            registration_loss = max(0, -registration_residual)
            registration_recovery = max(0, registration_residual)
            urban_share = lerp_anchors(config["urban_share_anchors"], year)
            profile = config["province_profile"][province_id]
            profile_urban_factor = {
                "heartland": 1.12, "north_plain": 1.02, "east_plain": 1.03,
                "northeast": 0.84, "northwest_frontier": 0.76, "central_south": 0.95,
                "lower_yangtze": 1.08, "southwest": 0.90, "far_south": 0.72,
            }[profile]
            urban = min(actual_start[region_id], int(round(actual_start[region_id] * urban_share * profile_urban_factor)))
            rural = actual_start[region_id] - urban
            male = int(round(actual_start[region_id] * int(config["male_share_basis_points"]) / 10000.0))
            female = actual_start[region_id] - male
            ages = split_exact(actual_start[region_id], config["age_distribution_basis_points"].items())
            military = int(round(actual_start[region_id] * (0.007 + 0.006 * min(1.0, region_event_exposure(region_id, province_id, active)))))
            row = {
                "year": year,
                "region_permanent_id": region_id,
                "historical_name": region["canonical_name"],
                "region_type": classify_region(region),
                "province_permanent_id": province_id,
                "registered_population": registered_start[region_id],
                "registered_population_end": registered_end[region_id],
                "modeled_actual_population": actual_start[region_id],
                "modeled_actual_population_end": actual_end[region_id],
                "urban_population": urban,
                "rural_population": rural,
                "population_density": round(actual_start[region_id] / max(1.0, 9000.0 + stable_unit(region_id, "area") * 21000.0), 3),
                "births": births[region_id],
                "natural_deaths": natural_deaths[region_id],
                "war_deaths": event_deaths["war"][region_id],
                "epidemic_deaths": event_deaths["epidemic"][region_id],
                "disaster_deaths": event_deaths["disaster"][region_id],
                "net_migration": net_migration[region_id],
                "registration_loss": registration_loss,
                "registration_recovery": registration_recovery,
                "male_population": male,
                "female_population": female,
                "children_0_13": ages["children_0_13"],
                "youth_14_19": ages["youth_14_19"],
                "main_adult_20_59": ages["main_adult_20_59"],
                "elder_adult_60_69": ages["elder_adult_60_69"],
                "old_age_70_plus": ages["old_age_70_plus"],
                "civilian_population": actual_start[region_id] - military,
                "military_active_population": military,
                "historical_anchor": effective_population(population_by_id[region_id]) if year == 140 else None,
                "national_anchor_reconciliation": (registered_start[region_id] - effective_population(population_by_id[region_id])) if year == 140 else 0,
                "active_event_ids": [event["event_id"] for event in active if province_id in event.get("affected_provinces", []) or region_id in event.get("affected_region_ids", [])],
                "model_method": "M13_140_spatial_anchor_x_regional_profile_x_event_demography_v1",
                "confidence": evidence_for_year(year),
                "notes": "140 HistoricalAnchor字段保留源值；RegisteredPopulation为国家锚点调和后的时间线值。" if year == 140 else "区域模型值，不冒充同年史籍普查。",
            }
            region_rows.append(row)
            for key in ("registered_population", "registered_population_end", "modeled_actual_population", "modeled_actual_population_end", "births", "natural_deaths", "war_deaths", "epidemic_deaths", "disaster_deaths", "net_migration", "registration_loss", "registration_recovery", "urban_population", "rural_population"):
                province_acc[province_id][key] += row[key]

            county_list = counties_by_region[region_id]
            county_weights = [(county["admin_unit_id"], county_weight_details[county["admin_unit_id"]]["combined_weight"]) for county in county_list]
            county_actual = allocate_exact(actual_start[region_id], county_weights)
            county_registered = allocate_exact(registered_start[region_id], county_weights)
            county_births = allocate_exact(births[region_id], [(county_id, county_actual[county_id]) for county_id in county_actual])
            county_deaths = allocate_exact(natural_deaths[region_id] + event_deaths["war"][region_id] + event_deaths["epidemic"][region_id] + event_deaths["disaster"][region_id], [(county_id, county_actual[county_id]) for county_id in county_actual])
            county_migration = allocate_signed(net_migration[region_id], [(county_id, county_actual[county_id]) for county_id in county_actual])
            for county in county_list:
                county_id = county["admin_unit_id"]
                actual = county_actual[county_id]
                seat = county_weight_details[county_id]["is_commandery_seat"]
                capital = county_weight_details[county_id]["is_capital_county"]
                urban_ratio = min(0.62, urban_share * (1.75 if seat else 0.75) * (1.55 if capital else 1.0))
                settlement_shares = {
                    "urban_settlement_population": max(100, int(round(urban_ratio * 10000))),
                    "town_population": 650 if not seat else 900,
                    "village_population": 4200,
                    "estate_population": 900,
                    "dispersed_agricultural_population": 2800,
                    "pastoral_forest_population": 700 if profile in ("northeast", "northwest_frontier", "far_south") else 350,
                    "special_population": 200,
                }
                parts = split_exact(actual, settlement_shares.items())
                county_rows.append({
                    "year": year,
                    "county_permanent_id": county_id,
                    "historical_county_name": county["canonical_name"],
                    "parent_region_permanent_id": region_id,
                    "province_permanent_id": province_id,
                    "registered_population": county_registered[county_id],
                    "modeled_actual_population": actual,
                    "population_density": round(actual / max(1.0, 420.0 + stable_unit(county_id, "county-area") * 2300.0), 3),
                    **parts,
                    "births": county_births[county_id],
                    "deaths": county_deaths[county_id],
                    "migration": county_migration[county_id],
                    "historical_events": "|".join(row["active_event_ids"]),
                    "county_weight": county_weight_details[county_id]["combined_weight"],
                    "confidence": evidence_for_year(year) if year != 140 else "C",
                    "notes": "县级人口为非平均权重分配模型；县永久ID来自M13。",
                })

        province_rows = []
        for province in provinces:
            province_id = province["admin_unit_id"]
            values = province_acc[province_id]
            province_rows.append({
                "year": year,
                "province_permanent_id": province_id,
                "historical_province_name": province["canonical_name"],
                "registered_population": values["registered_population"],
                "registered_population_end": values["registered_population_end"],
                "modeled_actual_population": values["modeled_actual_population"],
                "modeled_actual_population_end": values["modeled_actual_population_end"],
                "national_share": round(values["modeled_actual_population"] / actual_national[year], 8),
                "births": values["births"],
                "natural_deaths": values["natural_deaths"],
                "war_deaths": values["war_deaths"],
                "epidemic_deaths": values["epidemic_deaths"],
                "disaster_deaths": values["disaster_deaths"],
                "net_migration": values["net_migration"],
                "registration_change": values["registration_recovery"] - values["registration_loss"],
                "population_density": round(values["modeled_actual_population"] / max(1.0, 65000.0 + stable_unit(province_id, "province-area") * 180000.0), 3),
                "urban_population": values["urban_population"],
                "rural_population": values["rural_population"],
                "confidence": evidence_for_year(year),
                "notes": "州级汇总来自105项郡国级时间线。",
            })

        national_row = {
            "year": year,
            "registered_population_start": registered_national[year],
            "registered_population_end": registered_national[year + 1],
            "modeled_actual_population_start": actual_national[year],
            "modeled_actual_population_end": actual_national[year + 1],
            "births": births_total,
            "natural_deaths": natural_deaths_total,
            "war_deaths": war_total,
            "epidemic_deaths": epidemic_total,
            "disaster_deaths": disaster_total,
            "net_migration": 0,
            "registration_loss": sum(row["registration_loss"] for row in region_rows),
            "registration_recovery": sum(row["registration_recovery"] for row in region_rows),
            "annual_change": actual_national[year + 1] - actual_national[year],
            "annual_change_rate": round((actual_national[year + 1] - actual_national[year]) / actual_national[year], 8),
            "historical_anchors": "140_HOU_HAN_SHU" if year == 140 else ("157_JIN_SHU" if year == 157 else ""),
            "evidence_level": evidence_for_year(year),
            "notes": "184_START与184_END分离；快照读取年初人口。" if year == 184 else "同一连续时间线；模型人口不冒充史籍普查。",
        }
        annual_rows.append(national_row)

        province_actual_sum = sum(row["modeled_actual_population"] for row in province_rows)
        region_actual_sum = sum(row["modeled_actual_population"] for row in region_rows)
        county_actual_sum = sum(row["modeled_actual_population"] for row in county_rows)
        province_registered_sum = sum(row["registered_population"] for row in province_rows)
        region_registered_sum = sum(row["registered_population"] for row in region_rows)
        county_registered_sum = sum(row["registered_population"] for row in county_rows)
        settlement_sum = sum(sum(row[key] for key in ("urban_settlement_population", "town_population", "village_population", "estate_population", "dispersed_agricultural_population", "pastoral_forest_population", "special_population")) for row in county_rows)
        conservation = {
            "year": year,
            "national_actual": actual_national[year],
            "province_actual": province_actual_sum,
            "region_actual": region_actual_sum,
            "county_actual": county_actual_sum,
            "settlement_actual": settlement_sum,
            "actual_error": actual_national[year] - county_actual_sum,
            "national_registered": registered_national[year],
            "province_registered": province_registered_sum,
            "region_registered": region_registered_sum,
            "county_registered": county_registered_sum,
            "registered_error": registered_national[year] - county_registered_sum,
            "migration_error": sum(row["net_migration"] for row in region_rows),
            "negative_population_count": sum(1 for row in county_rows if row["modeled_actual_population"] < 0 or row["registered_population"] < 0),
            "duplicate_county_count": len(county_rows) - len({row["county_permanent_id"] for row in county_rows}),
            "status": "PASS" if actual_national[year] == province_actual_sum == region_actual_sum == county_actual_sum == settlement_sum and registered_national[year] == province_registered_sum == region_registered_sum == county_registered_sum and sum(row["net_migration"] for row in region_rows) == 0 else "FAIL",
        }
        conservation_rows.append(conservation)
        if conservation["status"] != "PASS":
            raise AssertionError("Population conservation failed for " + str(year))

        for city_name, historical_names in city_names:
            matches = []
            for historical_name in historical_names:
                matches.extend(county_by_name.get(historical_name, []))
            if not matches:
                continue
            county = sorted(matches, key=lambda row: row["admin_unit_id"])[0]
            county_row = next(row for row in county_rows if row["county_permanent_id"] == county["admin_unit_id"])
            city_ratio = 0.32 + stable_unit(county["admin_unit_id"], "city") * 0.18
            urban_area = int(round(county_row["modeled_actual_population"] * city_ratio))
            walled = int(round(urban_area * 0.72))
            metro = int(round(urban_area * 1.38))
            confidence = "C"
            source = "source.project.historical_population.v0_1"
            method = "major_city_county_capacity_model_v1"
            if city_name == "洛阳" and year == 184:
                walled = int(config["luoyang"]["walled_city_population_184"])
                urban_area = int(config["luoyang"]["urban_area_population_184"])
                metro = int(config["luoyang"]["metropolitan_population_184"])
                confidence = "B"
                source = "source.project.luoyang184.metropolitan.v1"
                method = "protected_luoyang_local_calibration_v1"
            city_rows.append({
                "city_permanent_id": "city.han." + city_name,
                "city_name": city_name,
                "year": year,
                "year_range": str(year),
                "walled_city_population": walled,
                "urban_area_population": urban_area,
                "metropolitan_population": metro,
                "county_population": county_row["modeled_actual_population"],
                "county_permanent_id": county["admin_unit_id"],
                "evidence": "HistoricalLocalCalibration" if city_name == "洛阳" and year == 184 else "ModelEstimate",
                "source": source,
                "confidence": confidence,
                "model_method": method,
                "notes": "都市圈人口包含城墙内与连续城区，不可与县人口重复相加。",
            })

        year_payload = {
            "schema": REGION_SCHEMA,
            "year": year,
            "snapshot_moment": "YEAR_START",
            "national": national_row,
            "provinces": province_rows,
            "regions": region_rows,
            "counties": county_rows,
            "major_cities": [row for row in city_rows if row["year"] == year],
            "conservation": conservation,
        }
        write_json(years_root / f"year_{year}.json", year_payload, compact=True)
        year_read_timings.append({"year": year, "build_ms": round((time.perf_counter() - build_year_started) * 1000.0, 3)})

    write_json(runtime_root / "annual_population.json", {"schema": "mandate.annual-population-timeline.v1", "records": annual_rows})
    write_json(runtime_root / "major_city_timeline.json", {"schema": "mandate.major-city-population-timeline.v1", "records": city_rows})
    write_json(runtime_root / "conservation_audit.json", {"schema": "mandate.population-conservation-audit.v1", "records": conservation_rows})

    scenario_index = []
    for index, year in enumerate(config["scenario_years"], start=1):
        source_path = years_root / f"year_{year}.json"
        scenario = {
            "scenario_id": f"S{index:02d}_{year}",
            "year": year,
            "snapshot_moment": "YEAR_START",
            "source_year_file": f"../years/year_{year}.json",
            "source_year_sha256": sha256_file(source_path),
            "derivation": "direct_reference_to_annual_population_timeline",
        }
        write_json(scenarios_root / f"S{index:02d}_{year}.json", scenario)
        scenario_index.append(scenario)
    write_json(runtime_root / "scenario_index.json", {"schema": "mandate.scenario-population-snapshot-index.v1", "scenarios": scenario_index})

    # Luoyang consistency uses the same 184 year shard, never the local package as a population source.
    year184 = load_json(years_root / "year_184.json")
    henan = next(row for row in year184["regions"] if row["region_permanent_id"] == config["luoyang"]["henan_yin_region_id"])
    luoyang_county = next(row for row in year184["counties"] if row["county_permanent_id"] == config["luoyang"]["luoyang_county_id"])
    supply_ids = set(config["luoyang"]["supply_region_county_ids"])
    represented_supply_population = sum(row["modeled_actual_population"] for row in year184["counties"] if row["county_permanent_id"] in supply_ids)
    metro = int(config["luoyang"]["metropolitan_population_184"])
    candidate = int(config["luoyang"]["supply_region_candidate_population_184"])
    metro_share = metro / henan["modeled_actual_population"]
    local_conclusion = "PASS" if metro <= represented_supply_population and metro_share <= 0.55 else ("PASS_WITH_ADJUSTMENT" if metro_share <= 0.70 else "FAIL")
    supply_conclusion = "KEEP_700K" if represented_supply_population >= candidate and candidate < henan["modeled_actual_population"] else ("ADJUST_TO_NEW_VALUE" if represented_supply_population >= metro else "REJECT_MODEL")
    luoyang_audit = {
        "schema": "mandate.luoyang-national-population-consistency.v1",
        "year": 184,
        "henan_yin_registered_population": henan["registered_population"],
        "henan_yin_modeled_actual_population": henan["modeled_actual_population"],
        "luoyang_county_modeled_actual_population": luoyang_county["modeled_actual_population"],
        "luoyang_walled_population": int(config["luoyang"]["walled_city_population_184"]),
        "luoyang_urban_population": int(config["luoyang"]["urban_area_population_184"]),
        "luoyang_metropolitan_population": metro,
        "luoyang_metropolitan_share_of_henan_yin": round(metro_share, 8),
        "metropolitan_conclusion": local_conclusion,
        "supply_region_candidate_population": candidate,
        "supply_region_county_ids": sorted(supply_ids),
        "supply_region_represented_population": represented_supply_population,
        "supply_region_conclusion": supply_conclusion,
        "double_counting_rule": "700K is an inclusive supply-region envelope containing the 400K metropolitan population; it is not 400K+700K.",
        "notes": "河南尹采用全国时间线值；既有400K Person包未被修改。",
    }
    write_json(runtime_root / "luoyang_consistency.json", luoyang_audit)

    # Copy compact formal reports/data used by the workbook builder.
    report_data_root = output_root / "data"
    if report_data_root.exists():
        shutil.rmtree(report_data_root)
    shutil.copytree(runtime_root, report_data_root)

    total_build_ms = round((time.perf_counter() - started) * 1000.0, 3)
    sample_read = []
    for sample_year in (184, 219, 260):
        before = time.perf_counter()
        payload = load_json(years_root / f"year_{sample_year}.json")
        sample_read.append({"year": sample_year, "read_ms": round((time.perf_counter() - before) * 1000.0, 3), "county_count": len(payload["counties"])})
    performance = {
        "schema": "mandate.han-national-population-performance.v1",
        "total_generation_ms": total_build_ms,
        "year_shard_build_ms_min": min(row["build_ms"] for row in year_read_timings),
        "year_shard_build_ms_max": max(row["build_ms"] for row in year_read_timings),
        "year_shard_build_ms_average": round(sum(row["build_ms"] for row in year_read_timings) / len(year_read_timings), 3),
        "single_year_reads": sample_read,
        "year_count": len(years),
        "province_year_records": len(years) * len(provinces),
        "region_year_records": len(years) * len(regions),
        "county_year_records": len(years) * len(counties),
        "scenario_snapshot_count": len(scenario_index),
    }
    write_json(output_root / "performance_report.json", performance)

    validation = {
        "schema": "mandate.han-national-population-validation.v1",
        "status": "PASS",
        "years": len(years),
        "provinces": len(provinces),
        "regions": len(regions),
        "counties": len(counties),
        "county_year_records": len(years) * len(counties),
        "scenario_snapshots": len(scenario_index),
        "historical_time_points": len(config["historical_time_point_years"]),
        "anchor_140_registered": registered_national[140],
        "anchor_157_registered": registered_national[157],
        "m13_effective_population_total": base_total,
        "national_anchor_reconciliation": anchor_reconciliation,
        "all_conservation_passed": all(row["status"] == "PASS" for row in conservation_rows),
        "luoyang_metropolitan_conclusion": local_conclusion,
        "luoyang_supply_region_conclusion": supply_conclusion,
        "permanent_persons_generated": 0,
    }
    write_json(output_root / "validation_summary.json", validation)

    report = build_report(config, annual_rows, year184, luoyang_audit, sources_doc, performance, regions, province_by_region, runtime_root)
    (output_root / "11_135-260全国人口分布研究报告_V1.md").write_text(report, encoding="utf-8")

    # Manifest is written last and hashes every immutable runtime payload except itself.
    files = []
    for path in sorted(runtime_root.rglob("*")):
        if path.is_file() and path.name != "manifest.json":
            files.append({"path": path.relative_to(runtime_root).as_posix(), "bytes": path.stat().st_size, "sha256": sha256_file(path)})
    manifest = {
        "schema": SCHEMA,
        "format_version": 1,
        "model_version": config["model_version"],
        "year_start": year_start,
        "year_end": year_end,
        "year_count": len(years),
        "province_count": len(provinces),
        "region_count": len(regions),
        "county_count": len(counties),
        "county_year_record_count": len(years) * len(counties),
        "scenario_count": len(scenario_index),
        "national_anchor_140_registered": registered_national[140],
        "national_anchor_157_registered": registered_national[157],
        "permanent_persons_generated": 0,
        "snapshot_path_template": "years/year_{year}.json",
        "files": files,
    }
    write_json(runtime_root / "manifest.json", manifest)
    return validation


def polity_for_region_223(region_row):
    province_id = region_row["province_permanent_id"]
    if province_id == "admin.han140.yizhou":
        return "蜀汉"
    if province_id in ("admin.han140.yangzhou", "admin.han140.jiaozhou"):
        return "孙吴"
    if province_id == "admin.han140.jingzhou":
        return "孙吴" if stable_unit(region_row["region_permanent_id"], "jingzhou-223") >= 0.35 else "曹魏"
    return "曹魏"


def build_report(config, annual_rows, year184, luoyang, sources_doc, performance, regions, province_by_region, runtime_root):
    annual = {row["year"]: row for row in annual_rows}
    year223 = load_json(runtime_root / "years" / "year_223.json")
    polity = defaultdict(int)
    for region in year223["regions"]:
        polity[polity_for_region_223(region)] += region["modeled_actual_population"]
    province184 = sorted(year184["provinces"], key=lambda row: row["modeled_actual_population"], reverse=True)
    region184 = sorted(year184["regions"], key=lambda row: row["modeled_actual_population"], reverse=True)
    minimum = min(annual_rows, key=lambda row: row["modeled_actual_population_start"])
    fastest = sorted((row for row in year223["provinces"]), key=lambda row: row["modeled_actual_population"], reverse=True)[:3]
    source_lines = "\n".join(f"- {item['title']}：{item['url']}（{item['reliability']}）" for item in sources_doc["sources"])
    province_lines = "\n".join(f"- {row['historical_province_name']}：登记 {row['registered_population']:,}，推定实际 {row['modeled_actual_population']:,}" for row in province184)
    top_region_lines = "\n".join(f"- {row['historical_name']}（{row['region_type']}）：登记 {row['registered_population']:,}，推定实际 {row['modeled_actual_population']:,}" for row in region184[:20])
    return f"""# 135—260全国人口分布研究报告 V1

## 1. 结论与口径

本报告由同一条135—260人口时间线生成，共126个年度、13州级单位、105个郡国等价单位和1182个永久县ID。140与157为H级全国登记人口锚点；其他年份多为B/C级历史重建或模型估算。`ModeledActualPopulation`是游戏世界人口母盘，`RegisteredPopulation`是当时行政体系可掌握的户籍人口，两者不得混同。

本任务没有生成全国Permanent Person。洛阳既有400,000人局部包也没有被修改。

## 2. Historical Sources

{source_lines}

140年105项分项有效合计与篇末49,150,220口锚点并不完全相等。V1保留M13原值，并用独立、逐项可见的`NationalAnchorReconciliation`调和国家总量，未覆盖史籍字段。

## 3. 方法

1. 以M13的105项人口来源和1182县永久ID为唯一空间入口。
2. 135—139由140锚点反向连续推算，不在140年强制跳变。
3. 140—260按区域人口恢复档、出生、自然死亡、战争、疫病、灾害、迁徙和户籍覆盖变化连续计算。
4. 州、郡国、县使用最大余数调和，逐年误差严格为0。
5. 县级权重由治所/首都角色和确定性土地、水源、市场代理权重共同构成，禁止平均分。
6. 13个Scenario只引用同一年度分片，未另算第二套人口。

## 4. 分期研究摘要

- 135—139：从140锚点反推，人口平缓增长并自然收敛。
- 140—157：以农业与社会稳定恢复为主，157锚点校准为56,486,856口。
- 158—183：疫病、羌乱和财政压力使增长停滞并区域分化。
- 184—189：184_START与184_END分开，黄巾冲击只在184年度流量中扣除。
- 190—196：迁都、关东和中原战争造成真实死亡、迁徙与户籍崩坏。
- 197—207：北方兼并仍有损失，屯田与重新编户开始恢复。
- 208—219：荆州重分布、江南吸收迁民、汉中与关中战争并存。
- 220—234：三国政权恢复生产与登记，但实际人口仍远高于残缺户籍。
- 235—260：北方和江南总体恢复，淮南、寿春与西北战区保持局部冲击。

## 5. 20个最终问题

1. **135年全国人口模型值**：登记 {annual[135]['registered_population_start']:,}，推定实际 {annual[135]['modeled_actual_population_start']:,}。
2. **140年全国史籍人口**：9,698,630户、49,150,220口，H级，源自《后汉书》郡国志篇末。
3. **157年前后锚点**：10,677,960户、56,486,856口，H级，见《晋书》卷十四《地理志上》；V1把它作为全国登记人口锚点，不伪造同年逐郡普查。
4. **184_START全国RegisteredPopulation**：{annual[184]['registered_population_start']:,}。
5. **184_START全国ModeledActualPopulation**：{annual[184]['modeled_actual_population_start']:,}；184_END为{annual[184]['modeled_actual_population_end']:,}。
6. **184各州人口**：见下节及`02_135-260州级人口年度分布.xlsx`。
7. **184各郡国人口**：见下节前20项及`03_135-260郡国人口年度分布.xlsx`全部105项。
8. **184的1182县人口**：全部位于同一184年度分片和县级Excel分卷，无缺县。
9. **184河南尹人口**：登记 {luoyang['henan_yin_registered_population']:,}，推定实际 {luoyang['henan_yin_modeled_actual_population']:,}。
10. **184洛阳40万是否合理**：{luoyang['metropolitan_conclusion']}；它是县人口内的都市圈校准，不额外加到河南尹。
11. **40万占河南尹实际人口比例**：{luoyang['luoyang_metropolitan_share_of_henan_yin']:.2%}。
12. **70万SupplyRegion结论**：{luoyang['supply_region_conclusion']}；700K是包含400K都市圈的包络，不是400K+700K。
13. **190全国人口**：登记 {annual[190]['registered_population_start']:,}，推定实际 {annual[190]['modeled_actual_population_start']:,}。
14. **200全国人口**：登记 {annual[200]['registered_population_start']:,}，推定实际 {annual[200]['modeled_actual_population_start']:,}。
15. **220全国人口**：登记 {annual[220]['registered_population_start']:,}，推定实际 {annual[220]['modeled_actual_population_start']:,}。
16. **223三政权控制区推定实际人口**：曹魏约{polity['曹魏']:,}、蜀汉约{polity['蜀汉']:,}、孙吴约{polity['孙吴']:,}；这是稳定地理区域的C级控制投影，不是史籍户籍表。
17. **234三国人口潜力**：全国推定实际{annual[234]['modeled_actual_population_start']:,}，登记{annual[234]['registered_population_start']:,}；差额代表漏籍、依附、流民及边地等潜在人口，不可直接等同可征兵人口。
18. **249全国分布**：全国推定实际{annual[249]['modeled_actual_population_start']:,}；完整州郡县分布由`LoadPopulationSnapshot(249)`读取。
19. **260全国人口**：登记 {annual[260]['registered_population_start']:,}，推定实际 {annual[260]['modeled_actual_population_start']:,}。
20. **最低点、损失与恢复**：全国最低点为{minimum['year']}年、推定实际{minimum['modeled_actual_population_start']:,}；中原心脏区在184—220损失最大，江南、交州与益州恢复/份额增长最快。此处是B/C级模型结论。

## 6. 184州级人口

{province_lines}

## 7. 184郡国级人口（按实际人口前20）

{top_region_lines}

## 8. 城市、城乡、年龄与性别

每个郡国年度记录均保存城乡、男女、五档年龄和军民拆分；军事人口包含在总人口内。县级记录进一步拆分县城、镇、村、庄园、分散农业、牧林与特殊人口，七项严格等于县人口。除184洛阳外，主要城市精确人口多为C级容量模型，不伪装为史籍数字。

## 9. 战争、疫病、灾害与迁徙

所有冲击以稳定`PopulationImpactEventId`进入年度结算。战争死亡只在本人口母盘扣一次，并为未来Person物化预留事件关联；迁徙在全国范围严格守恒。恢复事件允许出生、返迁、屯田和重新编户推动人口回升。

## 10. 洛阳专项结论

- 184河南尹推定实际人口：{luoyang['henan_yin_modeled_actual_population']:,}。
- 洛阳城墙内/连续城区/都市圈：200,000 / 270,000 / 400,000。
- 都市圈一致性：{luoyang['metropolitan_conclusion']}。
- 70万供给区：{luoyang['supply_region_conclusion']}，所列县级包络可表示{luoyang['supply_region_represented_population']:,}人；700K必须被解释为其中的供给联系人口，不是新加人口。

## 11. 性能与运行时

- 生成耗时：{performance['total_generation_ms']:.3f} ms。
- 年分片平均构建：{performance['year_shard_build_ms_average']:.3f} ms。
- 数据规模：126年、{performance['province_year_records']:,}州年、{performance['region_year_records']:,}郡国年、{performance['county_year_records']:,}县年记录。
- 任意年份按单分片读取，不要求126年全部常驻内存。

## 12. 已知不确定性

- 140年分项与篇末合计存在史料缺项/讹误差，V1以显式调和字段处理。
- 157年只有全国总量，没有同年完整郡国表。
- 158—260的多数地方数字为B/C级历史重建，不能称为史书记载。
- 行政时间轴V1保护永久地理连续性，但没有声称穷尽126年内每次县郡改置。
- 主要城市除洛阳外多数缺乏同口径人口史料，允许C级模型或留待后续校准。
"""


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--runtime-root", type=Path)
    parser.add_argument("--output-root", type=Path)
    args = parser.parse_args()
    project_root = args.project_root.resolve()
    runtime_root = (args.runtime_root or project_root / "Assets" / "StreamingAssets" / "HistoricalPopulation" / "Han135260V1").resolve()
    output_root = (args.output_root or project_root / "outputs" / "HAN_135_260_NATIONAL_POPULATION_DISTRIBUTION_V1").resolve()
    validation = build_model(project_root, runtime_root, output_root)
    print(json.dumps(validation, ensure_ascii=False, separators=(",", ":")))


if __name__ == "__main__":
    main()
