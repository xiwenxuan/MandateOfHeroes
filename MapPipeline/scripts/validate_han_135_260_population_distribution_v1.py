#!/usr/bin/env python3
"""Deep validation for the HAN-135-260 national population package."""

from __future__ import annotations

import argparse
import hashlib
import json
import time
from pathlib import Path


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8"))


def sha256_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def validate(runtime_root: Path, output_root: Path):
    started = time.perf_counter()
    manifest = read_json(runtime_root / "manifest.json")
    assert manifest["schema"] == "mandate.han-national-population-dataset.v1"
    assert manifest["year_start"] == 135 and manifest["year_end"] == 260 and manifest["year_count"] == 126
    assert manifest["province_count"] == 13 and manifest["region_count"] == 105 and manifest["county_count"] == 1182
    assert manifest["county_year_record_count"] == 148932 and manifest["scenario_count"] == 13
    assert manifest["national_anchor_140_registered"] == 49150220
    assert manifest["national_anchor_157_registered"] == 56486856
    assert manifest["permanent_persons_generated"] == 0

    for item in manifest["files"]:
        path = runtime_root / item["path"]
        assert path.is_file(), item["path"]
        assert path.stat().st_size == item["bytes"], item["path"]
        assert sha256_file(path) == item["sha256"], item["path"]

    previous_end_actual = None
    previous_end_registered = None
    county_ids = None
    region_ids = None
    province_ids = None
    national_rows = []
    for year in range(135, 261):
        payload = read_json(runtime_root / "years" / f"year_{year}.json")
        assert payload["schema"] == "mandate.han-national-population-year.v1"
        assert payload["year"] == year and payload["snapshot_moment"] == "YEAR_START"
        assert len(payload["provinces"]) == 13 and len(payload["regions"]) == 105 and len(payload["counties"]) == 1182
        current_provinces = {row["province_permanent_id"] for row in payload["provinces"]}
        current_regions = {row["region_permanent_id"] for row in payload["regions"]}
        current_counties = {row["county_permanent_id"] for row in payload["counties"]}
        assert len(current_provinces) == 13 and len(current_regions) == 105 and len(current_counties) == 1182
        province_ids = province_ids or current_provinces
        region_ids = region_ids or current_regions
        county_ids = county_ids or current_counties
        assert province_ids == current_provinces and region_ids == current_regions and county_ids == current_counties
        national = payload["national"]
        national_rows.append(national)
        if previous_end_actual is not None:
            assert national["modeled_actual_population_start"] == previous_end_actual
            assert national["registered_population_start"] == previous_end_registered
        previous_end_actual = national["modeled_actual_population_end"]
        previous_end_registered = national["registered_population_end"]
        actual = national["modeled_actual_population_start"]
        registered = national["registered_population_start"]
        assert actual >= 0 and registered >= 0
        assert sum(row["modeled_actual_population"] for row in payload["provinces"]) == actual
        assert sum(row["modeled_actual_population"] for row in payload["regions"]) == actual
        assert sum(row["modeled_actual_population"] for row in payload["counties"]) == actual
        assert sum(row["registered_population"] for row in payload["provinces"]) == registered
        assert sum(row["registered_population"] for row in payload["regions"]) == registered
        assert sum(row["registered_population"] for row in payload["counties"]) == registered
        assert sum(row["net_migration"] for row in payload["regions"]) == 0
        for county in payload["counties"]:
            assert county["parent_region_permanent_id"] in current_regions
            assert county["modeled_actual_population"] >= 0 and county["registered_population"] >= 0
            settlement_total = sum(county[key] for key in (
                "urban_settlement_population", "town_population", "village_population", "estate_population",
                "dispersed_agricultural_population", "pastoral_forest_population", "special_population"))
            assert settlement_total == county["modeled_actual_population"]
        assert payload["conservation"]["status"] == "PASS"

    assert national_rows[140 - 135]["registered_population_start"] == 49150220
    assert national_rows[157 - 135]["registered_population_start"] == 56486856
    assert national_rows[184 - 135]["modeled_actual_population_start"] == 53500000
    assert national_rows[184 - 135]["modeled_actual_population_end"] == 51500000
    assert national_rows[139 - 135]["modeled_actual_population_end"] == national_rows[140 - 135]["modeled_actual_population_start"]
    assert national_rows[139 - 135]["registered_population_end"] == 49150220

    scenario_index = read_json(runtime_root / "scenario_index.json")
    expected_scenarios = [140, 184, 189, 194, 200, 207, 214, 219, 223, 227, 234, 249, 260]
    assert [item["year"] for item in scenario_index["scenarios"]] == expected_scenarios
    for item in scenario_index["scenarios"]:
        year_path = runtime_root / "years" / f"year_{item['year']}.json"
        assert item["source_year_sha256"] == sha256_file(year_path)
        scenario_path = runtime_root / "scenarios" / f"{item['scenario_id']}.json"
        assert read_json(scenario_path)["derivation"] == "direct_reference_to_annual_population_timeline"

    administrative = read_json(runtime_root / "administrative_timeline.json")["records"]
    assert len(administrative) == 1287
    assert len({row["region_permanent_id"] for row in administrative}) == 1287
    assert all(row["valid_from_year"] <= row["valid_to_year"] for row in administrative)
    weights = read_json(runtime_root / "county_weights.json")["weights"]
    assert len(weights) == 1182 and len({row["county_id"] for row in weights}) == 1182
    assert all(row["combined_weight"] > 0 for row in weights)
    assert len({row["combined_weight"] for row in weights}) > 1000

    events = read_json(runtime_root / "events.json")["events"]
    assert any(row["impact_type"] == "War" for row in events)
    assert any(row["impact_type"] == "Epidemic" for row in events)
    assert any(row["impact_type"] in ("MassMigration", "ForcedRelocation") for row in events)
    assert any(row["impact_type"] in ("Tuntian", "PopulationRecovery", "Colonization") for row in events)

    luoyang = read_json(runtime_root / "luoyang_consistency.json")
    assert luoyang["luoyang_metropolitan_population"] == 400000
    assert luoyang["metropolitan_conclusion"] == "PASS"
    assert luoyang["supply_region_conclusion"] == "KEEP_700K"
    assert luoyang["supply_region_represented_population"] >= 700000
    assert len(luoyang["supply_region_county_ids"]) == len(set(luoyang["supply_region_county_ids"]))

    result = {
        "schema": "mandate.han-national-population-deep-validation.v1",
        "status": "PASS",
        "elapsed_ms": round((time.perf_counter() - started) * 1000.0, 3),
        "years": 126,
        "provinces": 13,
        "regions": 105,
        "counties": 1182,
        "county_year_records": 148932,
        "scenarios": 13,
        "anchor_140_registered": 49150220,
        "anchor_157_registered": 56486856,
        "luoyang_400k": "PASS",
        "luoyang_supply_700k": "KEEP_700K",
        "permanent_persons_generated": 0,
    }
    output_root.mkdir(parents=True, exist_ok=True)
    (output_root / "deep_validation_summary.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    project_root = args.project_root.resolve()
    result = validate(
        project_root / "Assets" / "StreamingAssets" / "HistoricalPopulation" / "Han135260V1",
        project_root / "outputs" / "HAN_135_260_NATIONAL_POPULATION_DISTRIBUTION_V1",
    )
    print(json.dumps(result, ensure_ascii=False, separators=(",", ":")))


if __name__ == "__main__":
    main()
