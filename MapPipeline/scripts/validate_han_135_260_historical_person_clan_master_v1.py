#!/usr/bin/env python3
"""Deep validation for HAN-135-260-HISTORICAL-PERSON-CLAN-MASTER-V1."""

from __future__ import annotations

import argparse
import hashlib
import json
import time
from pathlib import Path


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def records(sheet: dict) -> list[dict]:
    values = sheet["values"]
    headers = [str(value or "").strip() for value in values[0]]
    return [
        {headers[index]: row[index] if index < len(row) else None for index in range(len(headers))}
        for row in values[1:]
        if any(value not in (None, "") for value in row)
    ]


def unique(items, key, label):
    values = [item[key] for item in items]
    assert len(values) == len(set(values)), f"duplicate {label}"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=str(Path(__file__).resolve().parents[2]))
    args = parser.parse_args()
    started = time.perf_counter()
    root = Path(args.root).resolve()
    package = root / "Assets/StreamingAssets/HistoricalPersons/Han135260V1"
    manifest = json.loads((package / "manifest.json").read_text(encoding="utf-8"))
    assert manifest["schema"] == "mandate.historical-person-clan-package.v1"
    for item in manifest["files"]:
        path = package / item["path"]
        assert path.exists(), item["path"]
        assert path.stat().st_size == item["bytes"], item["path"]
        assert sha256_file(path) == item["sha256"], item["path"]

    persons = json.loads((package / "persons.json").read_text(encoding="utf-8"))["persons"]
    aliases = json.loads((package / "person_aliases.json").read_text(encoding="utf-8"))["aliases"]
    clans = json.loads((package / "clans.json").read_text(encoding="utf-8"))["clans"]
    branches_payload = json.loads((package / "branches.json").read_text(encoding="utf-8"))
    branches = branches_payload["branches"]
    kinship = json.loads((package / "kinship.json").read_text(encoding="utf-8"))["relations"]
    marriages = json.loads((package / "marriages.json").read_text(encoding="utf-8"))["marriages"]
    locations = json.loads((package / "person_locations.json").read_text(encoding="utf-8"))["records"]
    civil = json.loads((package / "civil_offices.json").read_text(encoding="utf-8"))["records"]
    military = json.loads((package / "military_offices.json").read_text(encoding="utf-8"))["records"]
    titles = json.loads((package / "titles.json").read_text(encoding="utf-8"))["records"]
    allegiances = json.loads((package / "allegiances.json").read_text(encoding="utf-8"))["records"]
    presence = json.loads((package / "clan_presence.json").read_text(encoding="utf-8"))["records"]
    sources = json.loads((package / "sources.json").read_text(encoding="utf-8"))["sources"]
    citations = json.loads((package / "citations.json").read_text(encoding="utf-8"))["citations"]
    audits = json.loads((package / "audits.json").read_text(encoding="utf-8"))
    summary = json.loads((package / "summary.json").read_text(encoding="utf-8"))
    scenario_index = json.loads((package / "scenario_index.json").read_text(encoding="utf-8"))["scenarios"]

    unique(persons, "person_id", "PersonId")
    unique(aliases, "alias_id", "AliasId")
    unique(clans, "clan_id", "ClanId")
    unique(branches, "branch_id", "BranchId")
    unique(kinship, "relation_id", "RelationId")
    unique(marriages, "marriage_id", "MarriageId")
    unique(sources, "source_id", "SourceId")
    unique(citations, "citation_id", "CitationId")

    person_ids = {item["person_id"] for item in persons}
    clan_ids = {item["clan_id"] for item in clans}
    branch_ids = {item["branch_id"] for item in branches}
    source_ids = {item["source_id"] for item in sources}
    assert len(persons) == 1202
    assert all(item["birth_clan_id"] == item["clan_id"] for item in persons)
    assert all(item["clan_id"] is None or item["clan_id"] in clan_ids for item in persons)
    assert all(item["lineage_branch_id"] is None or item["lineage_branch_id"] in branch_ids for item in persons)
    assert all(item["clan_id"] in clan_ids for item in branches)
    assert all(item["parent_branch_id"] is None or item["parent_branch_id"] in branch_ids for item in branches)
    assert all(item["person_a_id"] in person_ids and item["person_b_id"] in person_ids for item in kinship)
    assert all(item["person_a_id"] != item["person_b_id"] for item in kinship)
    assert all(item["person_a_id"] in person_ids and item["person_b_id"] in person_ids for item in marriages)
    assert all(item["source_id"] is None or item["source_id"] in source_ids for item in citations)
    assert len(audits["candidate_migrations"]) == 599
    assert not audits["person_identity_merge_audit"]
    assert not audits["ancestor_cycles"]
    assert not audits["luoyang_regression_mismatches"]

    baseline = json.loads((root / "Data/HistoricalPersons/han_135_260_historical_person_clan_existing_v5.json").read_text(encoding="utf-8"))
    baseline_people = records(baseline["personMasterV5"]["sheets"]["人物母表"])
    baseline_ids = {str(item["PersonId"]).strip() for item in baseline_people}
    assert person_ids == baseline_ids, "Existing PersonId regression"
    assert len(persons) == len(baseline_people)

    admin = json.loads((root / "Assets/StreamingAssets/HistoricalPopulation/Han135260V1/administrative_timeline.json").read_text(encoding="utf-8"))
    valid_regions = {item["region_permanent_id"] for item in admin["records"]}
    year184 = json.loads((root / "Assets/StreamingAssets/HistoricalPopulation/Han135260V1/years/year_184.json").read_text(encoding="utf-8"))
    valid_counties = {item["county_permanent_id"] for item in year184["counties"]}
    for person in persons:
        assert person["native_place_region_id"] is None or person["native_place_region_id"] in valid_regions
        assert person["native_place_county_id"] is None or person["native_place_county_id"] in valid_counties
    for location in locations:
        assert location["region_permanent_id"] is None or location["region_permanent_id"] in valid_regions
        assert location["county_permanent_id"] is None or location["county_permanent_id"] in valid_counties
    for item in presence:
        assert item["region_permanent_id"] in valid_regions
        assert item["county_permanent_id"] is None or item["county_permanent_id"] in valid_counties

    # Branch hierarchy cycle test.
    parent = {item["branch_id"]: item["parent_branch_id"] for item in branches}
    for branch_id in branch_ids:
        seen = set()
        current = branch_id
        while current is not None:
            assert current not in seen, f"branch cycle {branch_id}"
            seen.add(current)
            current = parent.get(current)

    # Marriage must never rewrite birth clan.
    person_by_id = {item["person_id"]: item for item in persons}
    for marriage in marriages:
        assert person_by_id[marriage["person_a_id"]]["birth_clan_id"] == person_by_id[marriage["person_a_id"]]["clan_id"]
        assert person_by_id[marriage["person_b_id"]]["birth_clan_id"] == person_by_id[marriage["person_b_id"]]["clan_id"]

    # All five timeline collections point to existing people and never fabricate fallback locations.
    for collection in (locations, civil, military, titles, allegiances):
        assert all(item["person_id"] in person_ids for item in collection)
    assert all(item["model_fallback_location"] is None for item in locations)

    assert len(scenario_index) == 13
    assert [item["year"] for item in scenario_index] == [140, 184, 189, 194, 200, 207, 214, 219, 223, 227, 234, 249, 260]
    for item in scenario_index:
        path = package / item["path"]
        assert sha256_file(path) == item["sha256"]
        snapshot = json.loads(path.read_text(encoding="utf-8"))
        assert snapshot["scenario_id"] == item["scenario_id"]
        assert snapshot["year"] == item["year"]
        assert len(snapshot["persons"]) == item["person_count"]
        assert len(snapshot["clans"]) == item["clan_count"]
        assert all(person["person_id"] in person_ids for person in snapshot["persons"])
        assert all(clan["clan_id"] in clan_ids for clan in snapshot["clans"])

    luoyang = json.loads((root / "Assets/StreamingAssets/WorldMap/Luoyang184UrbanInitializationV1/historical_persons.json").read_text(encoding="utf-8"))
    assert len(luoyang["people"]) == 25
    assert all(item["person_id"] in person_ids for item in luoyang["people"])
    assert all(person_by_id[item["person_id"]]["canonical_name"] == item["display_name"] for item in luoyang["people"])

    assert summary["family_organizations_generated"] == 0
    assert summary["households_generated"] == 0
    assert summary["family_assets_generated"] == 0
    assert summary["person_id_preserved_count"] == 1202
    assert summary["ancestor_cycle_count"] == 0
    assert summary["luoyang_regression_mismatch_count"] == 0

    result = {
        "status": "PASS", "elapsed_ms": round((time.perf_counter() - started) * 1000, 3),
        "persons": len(persons), "clans": len(clans), "branches": len(branches),
        "kinship": len(kinship), "marriages": len(marriages), "scenarios": len(scenario_index),
        "candidate_migrations": len(audits["candidate_migrations"]), "citations": len(citations),
        "unresolved_locations": len(audits["unresolved_locations"]),
        "unresolved_relations": len(audits["unresolved_relations"]),
        "luoyang_persons": len(luoyang["people"]), "family_organizations_generated": 0,
    }
    out = root / "outputs/HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1/deep_validation_summary.json"
    out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
