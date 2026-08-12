#!/usr/bin/env python3
"""Validate the additive Luoyang 184 metropolitan initialization package."""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import struct
import sys
import time
import tracemalloc
from collections import Counter
from pathlib import Path


BASE_PERSON_COUNT = 270_000
OUTER_PERSON_COUNT = 130_000
TOTAL_PERSON_COUNT = 400_000
BASE_HOUSEHOLD_COUNT = 53_992
BASE_FACILITY_COUNT = 1_230
NONE_U16 = 0xFFFF
NONE_U32 = 0xFFFFFFFF


def load_urban_module(repo: Path):
    source = repo / "MapPipeline" / "scripts" / "build_luoyang_184_urban_initialization_v1.py"
    spec = importlib.util.spec_from_file_location("luoyang_urban_validation_contract", source)
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


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def validate_header(path: Path, header_struct, magic: bytes, record_size: int, count: int):
    with path.open("rb") as stream:
        header = header_struct.unpack(stream.read(header_struct.size))
    assert header[0] == magic, (path, header[0])
    assert header[1] == 1 and header[2] == record_size and header[3] == count, (path, header)
    assert path.stat().st_size == header_struct.size + record_size * count, path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    repo = args.repo.resolve()
    urban = load_urban_module(repo)
    root = repo / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184MetropolitanInitializationV1"
    base_root = repo / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184UrbanInitializationV1"
    output_root = repo / "outputs" / "LUOYANG_184_METROPOLITAN_INITIALIZATION_V1"
    manifest = read_json(root / "manifest.json")
    catalogs = read_json(root / "catalogs.json")
    facilities = read_json(root / "facilities.json")["facilities"]
    base_facilities = read_json(base_root / "facilities.json")["facilities"]
    spatial = read_json(root / "spatial_plan.json")
    roads = read_json(root / "roads_logistics.json")
    agriculture = read_json(root / "agriculture_supply.json")
    families = read_json(root / "family_organizations.json")["organizations"]

    assert manifest["schema"] == "mandate.luoyang-184-metropolitan-initialization.v1"
    assert manifest["base_person_count"] == BASE_PERSON_COUNT
    assert manifest["added_person_count"] == OUTER_PERSON_COUNT
    assert manifest["person_count"] == TOTAL_PERSON_COUNT
    assert manifest["urban_area_population"] == BASE_PERSON_COUNT
    assert manifest["metropolitan_population"] == TOTAL_PERSON_COUNT
    assert manifest["supply_region_plan_population"] == 700_000
    assert manifest["base_facility_count"] == BASE_FACILITY_COUNT
    assert len(facilities) == manifest["added_facility_count"]
    assert len(catalogs["facility_ids"]) == manifest["facility_count"]

    for item in manifest["base_package_files"]:
        path = base_root / item["path"]
        assert path.exists() and path.stat().st_size == item["bytes"], item["path"]
        assert sha256(path) == item["sha256"], item["path"]
    for item in manifest["files"]:
        path = root / item["path"]
        assert path.exists() and path.stat().st_size == item["bytes"], item["path"]
        assert sha256(path) == item["sha256"], item["path"]

    validate_header(root / "outer_persons.bin", urban.HEADER_STRUCT, b"MOHLYM01", urban.PERSON_STRUCT.size, OUTER_PERSON_COUNT)
    validate_header(root / "outer_households.bin", urban.HEADER_STRUCT, b"MOHLYK01", urban.HOUSEHOLD_STRUCT.size, manifest["added_household_count"])

    base_cells = {int(f["cell_id64"]) for f in base_facilities}
    outer_cells = [int(f["cell_id64"]) for f in facilities]
    assert len(set(outer_cells)) == len(outer_cells)
    assert not (base_cells & set(outer_cells))
    assert [f["global_facility_index"] for f in facilities] == list(range(BASE_FACILITY_COUNT, manifest["facility_count"]))
    assert all(f["owner_id"] and f["administrative_controller_id"] for f in facilities)
    assert all(f["current_residents"] <= f["residential_capacity_persons"] for f in facilities)
    assert all(f["current_workers"] <= f["worker_capacity"] for f in facilities)

    household_count = manifest["added_household_count"]
    household_members = 0
    expected_household_ordinal = BASE_HOUSEHOLD_COUNT
    expected_member_start = BASE_PERSON_COUNT
    household_residences = {}
    household_sizes = {}
    monthly_checksum = 0
    monthly_started = time.perf_counter()
    with (root / "outer_households.bin").open("rb") as stream:
        stream.seek(urban.HEADER_STRUCT.size)
        for _ in range(household_count):
            record = urban.HOUSEHOLD_STRUCT.unpack(stream.read(urban.HOUSEHOLD_STRUCT.size))
            ordinal, head, start, count, family, residence = record[:6]
            assert ordinal == expected_household_ordinal
            assert start == expected_member_start
            assert count > 0 and start <= head < start + count
            assert BASE_FACILITY_COUNT <= residence < manifest["facility_count"]
            assert family == NONE_U16 or 7 <= family < 7 + len(families)
            household_residences[ordinal] = residence
            household_sizes[ordinal] = count
            household_members += count
            expected_member_start += count
            expected_household_ordinal += 1
            monthly_checksum = (monthly_checksum * 31 + ordinal + head + record[-1]) & 0xFFFFFFFFFFFFFFFF
    monthly_ms = (time.perf_counter() - monthly_started) * 1000.0
    assert household_members == OUTER_PERSON_COUNT
    assert expected_member_start == TOTAL_PERSON_COUNT

    occupied_residents = Counter()
    occupied_workers = Counter()
    occupation_counts = Counter()
    expected_ordinal = BASE_PERSON_COUNT
    housed = assigned = family_person_count = 0
    person_checksum = 0
    persons_started = time.perf_counter()
    tracemalloc.start()
    with (root / "outer_persons.bin").open("rb") as stream:
        stream.seek(urban.HEADER_STRUCT.size)
        for _ in range(OUTER_PERSON_COUNT):
            record = urban.PERSON_STRUCT.unpack(stream.read(urban.PERSON_STRUCT.size))
            ordinal, household, family, cell_id, residence, work, occupation = (
                record[0], record[5], record[6], record[7], record[8], record[9], record[10])
            assert ordinal == expected_ordinal
            assert BASE_HOUSEHOLD_COUNT <= household < manifest["household_count"]
            assert household_residences[household] == residence
            assert BASE_FACILITY_COUNT <= residence < manifest["facility_count"]
            assert work == NONE_U32 or BASE_FACILITY_COUNT <= work < manifest["facility_count"]
            assert family == NONE_U16 or 7 <= family < 7 + len(families)
            if family != NONE_U16:
                family_person_count += 1
            assert 0 <= occupation < len(catalogs["occupations"])
            assert record[23] == 2
            assert record[24] != 0
            for relation in record[27:30]:
                assert relation == -1 or BASE_PERSON_COUNT <= relation < TOTAL_PERSON_COUNT
            assert cell_id == int(facilities[residence - BASE_FACILITY_COUNT]["cell_id64"])
            occupied_residents[residence] += 1
            housed += 1
            if work != NONE_U32:
                occupied_workers[work] += 1
                assigned += 1
            occupation_counts[catalogs["occupations"][occupation]] += 1
            person_checksum = (person_checksum * 31 + ordinal + record[4] + cell_id) & 0xFFFFFFFFFFFFFFFF
            expected_ordinal += 1
    current, peak = tracemalloc.get_traced_memory()
    tracemalloc.stop()
    person_scan_ms = (time.perf_counter() - persons_started) * 1000.0
    assert expected_ordinal == TOTAL_PERSON_COUNT and housed == OUTER_PERSON_COUNT
    assert assigned == sum(occupation_counts[key] for key in occupation_counts if key != "occupation.unfixed")
    for facility in facilities:
        index = facility["global_facility_index"]
        assert occupied_residents[index] == facility["current_residents"]
        assert occupied_workers[index] == facility["current_workers"]
    for occupation, target in {
        "occupation.agriculture": 22000, "occupation.transport": 8000, "occupation.trade": 9000,
        "occupation.crafts": 8000, "occupation.storage": 4000, "occupation.hospitality": 3000,
        "occupation.household_service": 5000, "occupation.elite_family_management": 2000,
        "occupation.animal_husbandry": 5000, "occupation.government": 3000,
        "occupation.religious": 1500, "occupation.education_staff": 1500,
    }.items():
        assert occupation_counts[occupation] == target, (occupation, occupation_counts[occupation])

    family_member_total = sum(f["member_count"] for f in families)
    assert len({f["family_organization_id"] for f in families}) == len(families)
    assert all(f["historical_claim"] is False and f["confidence"] == "C" for f in families)
    assert family_member_total == family_person_count and family_member_total > 0

    cell_lookup = {int(c["cell_id64"]): (int(c["grid_x"]), int(c["grid_y"])) for c in read_json(repo / "MapData" / "Luoyang184Historical_V1" / "luoyang_184_world.json")["cells"]}
    route_ids = set()
    for route in roads["routes"]:
        route_ids.add(route["route_id"])
        cells = route["cell_ids"]
        assert len(cells) >= 2 and cells[-1] in base_cells
        for index in range(1, len(cells)):
            x1, y1 = cell_lookup[cells[index - 1]]
            x2, y2 = cell_lookup[cells[index]]
            distance = abs(x1 - x2) + abs(y1 - y2)
            if index == len(cells) - 1 and route["uses_gate_complex_transition"]:
                assert 1 < distance <= 12
                assert distance == route["gate_complex_transition_span_cells"]
            else:
                assert distance == 1
    assert len(route_ids) == len(spatial["settlements"]) == 33

    agriculture_started = time.perf_counter()
    field_cells = set()
    full_yield = early_yield = 0
    for field in agriculture["fields"]:
        assert field["cell_id64"] not in field_cells
        field_cells.add(field["cell_id64"])
        assert field["maturity_day"] > field["planted_day"]
        assert 8000 <= field["early_harvest_minimum_basis_points"] <= 10000
        assert field["worker_person_ordinals"]
        assert all(BASE_PERSON_COUNT <= value < TOTAL_PERSON_COUNT for value in field["worker_person_ordinals"])
        full_yield += field["full_yield_units"]
        early_yield += field["full_yield_units"] * field["early_harvest_minimum_basis_points"] // 10000
    agriculture_ms = (time.perf_counter() - agriculture_started) * 1000.0

    logistics_started = time.perf_counter()
    products = set()
    delivered = losses = shipped = 0
    for chain in roads["supply_chains"]:
        products.add(chain["product_definition_id"])
        assert BASE_PERSON_COUNT <= chain["carrier_person_ordinal"] < TOTAL_PERSON_COUNT
        assert chain["shipped_units"] == chain["natural_loss_units"] + chain["road_loss_units"] + chain["delivered_units"]
        assert chain["destination_inventory_units_after"] == chain["delivered_units"]
        shipped += chain["shipped_units"]
        losses += chain["natural_loss_units"] + chain["road_loss_units"]
        delivered += chain["delivered_units"]
    logistics_ms = (time.perf_counter() - logistics_started) * 1000.0
    assert len(products) == 5 and shipped == losses + delivered

    manifest_text = json.dumps(manifest, ensure_ascii=False)
    assert ":\\" not in manifest_text and "E:/" not in manifest_text and "C:/" not in manifest_text
    performance = {
        "schema": "mandate.luoyang-184-metropolitan-performance.v1",
        "outer_person_binary_bytes": (root / "outer_persons.bin").stat().st_size,
        "outer_household_binary_bytes": (root / "outer_households.bin").stat().st_size,
        "full_400k_contract_load_ms": round(person_scan_ms + monthly_ms, 3),
        "outer_person_daily_audit_ms": round(person_scan_ms, 3),
        "outer_household_monthly_audit_ms": round(monthly_ms, 3),
        "agriculture_tick_ms": round(agriculture_ms, 3),
        "logistics_tick_ms": round(logistics_ms, 3),
        "python_validation_peak_traced_mib": round(peak / 1048576.0, 3),
        "person_checksum": person_checksum, "household_checksum": monthly_checksum,
        "full_yield_units": full_yield, "early_yield_units_at_threshold": early_yield,
    }
    summary = {
        "status": "PASS", "persons": TOTAL_PERSON_COUNT, "added_persons": OUTER_PERSON_COUNT,
        "added_households": household_count, "added_facilities": len(facilities),
        "routes": len(route_ids), "agriculture_units": len(agriculture["fields"]),
        "supply_chain_types": len(products), "protected_base_files": len(manifest["base_package_files"]),
        "performance": performance,
    }
    output_root.mkdir(parents=True, exist_ok=True)
    (output_root / "performance_report.json").write_text(json.dumps(performance, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (output_root / "validation_summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False))


if __name__ == "__main__":
    main()
