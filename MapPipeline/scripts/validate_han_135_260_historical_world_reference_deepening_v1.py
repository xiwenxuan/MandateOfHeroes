#!/usr/bin/env python3
"""Validate the historical-world reference deepening deliverable without Unity."""

from __future__ import annotations

import json
import re
import sys
import zipfile
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEEP = ROOT / "Docs" / "HISTORICAL_WORLD_REFERENCE" / "DEEPENING_V1"
OUT = ROOT / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main():
    data = load(OUT / "deepening_workdata.json")
    checks = []

    def check(name, condition, detail=""):
        checks.append({"name": name, "passed": bool(condition), "detail": detail})

    core = data["core_settlements"]
    seats = data["seat_timeline"]
    priority = data["priority_counties"]
    estates = data["estate_references"]
    scenarios = data["scenarios"]
    coverage = data["coverage"]
    v1 = load(ROOT / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1" / "historical_world_reference_workdata.json")
    valid_counties = {x["county_id"] for x in v1["counties"]}
    valid_regions = {x["commandery_id"] for x in v1["commanderies"]} | {x["province_id"] for x in v1["commanderies"]}
    valid_cities = {x["city_id"] for x in v1["cities"]}
    valid_people = {x["person_id"] for x in v1["persons"]}
    valid_clans = {x["clan_id"] for x in v1["clans"]}
    branch_data = load(ROOT / "Assets" / "StreamingAssets" / "HistoricalPersons" / "Han135260V1" / "branches.json")
    valid_branches = {x["branch_id"] for x in branch_data["branches"]}

    check("Core Settlement Duplicate Audit", len({x["place_id"] for x in core}) == len(core) and len({x["county_id"] for x in core}) == len(core), f"{len(core)} rows")
    check("Canonical Core Count", len(core) == 133, str(len(core)))
    province_admins = {x["admin_unit_id"] for x in seats if x["admin_level"] == "PROVINCE_OR_CENTRAL"}
    check("Province Seat Coverage", len(province_admins) == 13, f"{len(province_admins)}/13")
    cmd_seats = [x for x in seats if x["admin_level"] == "COMMANDERY_EQUIVALENT"]
    check("105 Commandery Seat Coverage", len(cmd_seats) == 105 and len({x["admin_unit_id"] for x in cmd_seats}) == 105, str(len(cmd_seats)))
    city_tags = {city_id for row in core for city_id in row["city_ids"].split("|") if city_id}
    check("77 Strategic City Coverage", city_tags == valid_cities, f"{len(city_tags)}/77")

    overlaps = []
    by_admin = defaultdict(list)
    for row in seats:
        if row["admin_level"] == "PROVINCE_OR_CENTRAL": by_admin[row["admin_unit_id"]].append(row)
    for admin, rows in by_admin.items():
        for previous, current in zip(sorted(rows, key=lambda x: x["valid_from_year"]), sorted(rows, key=lambda x: x["valid_from_year"])[1:]):
            if current["valid_from_year"] <= previous["valid_to_year"]:
                overlaps.append((admin, previous["seat_record_id"], current["seat_record_id"]))
    check("Seat Timeline Conflict Audit", not overlaps, str(overlaps))

    referenced_counties = {x["county_id"] for x in core} | {x["county_id"] for x in priority} | {x["seat_county_id"] for x in seats if x["seat_county_id"]} | {x["county_id"] for x in estates}
    check("CountyPermanentId Resolution", referenced_counties <= valid_counties, str(sorted(referenced_counties - valid_counties)))
    referenced_regions = {x["commandery_id"] for x in core} | {x["province_id"] for x in core} | {x["admin_unit_id"] for x in seats}
    check("RegionPermanentId Resolution", referenced_regions <= valid_regions, str(sorted(referenced_regions - valid_regions)))
    check("CityId Resolution", city_tags <= valid_cities, str(sorted(city_tags - valid_cities)))

    estate_people = {pid for x in estates for pid in x["historical_person_ids"].split("|") if pid}
    estate_clans = {x["clan_id"] for x in estates if x["clan_id"]}
    estate_branches = {bid for x in estates for bid in x["branch_id"].split("|") if bid}
    check("HistoricalPersonId Resolution", estate_people <= valid_people, str(sorted(estate_people - valid_people)))
    check("ClanId Resolution", estate_clans <= valid_clans, str(sorted(estate_clans - valid_clans)))
    check("BranchId Resolution", estate_branches <= valid_branches, str(sorted(estate_branches - valid_branches)))
    check("Scenario Reference Coverage", {x["year"] for x in scenarios} == {140,184,189,194,200,207,214,219,223,227,234,249,260}, str(len(scenarios)))

    continuity_errors = []
    for admin, rows in by_admin.items():
        years = set()
        for row in rows: years.update(range(row["valid_from_year"], row["valid_to_year"] + 1))
        if years != set(range(135, 261)): continuity_errors.append(admin)
    check("Timeline Continuity Audit", not continuity_errors, str(continuity_errors))
    allowed_evidence = {"HISTORICAL", "RECONSTRUCTED", "MODELED", "UNKNOWN", "HISTORICAL_INDEX+RECONSTRUCTED_ROLE", "HISTORICAL_INDEX+MODELED_POPULATION", "RECONSTRUCTED+MODELED", "HISTORICAL_REFERENCE", "HISTORICAL/RECONSTRUCTED/UNKNOWN"}
    evidence_values = {x.get("evidence_type") for key in ["core_settlements","seat_timeline","priority_counties","industry_resources","transport_nodes","military_spaces","annual_changes","p0_reference"] for x in data[key]}
    check("Evidence Grade Audit", all(v in allowed_evidence for v in evidence_values), str(sorted(v for v in evidence_values if v not in allowed_evidence)))

    core_dirs = list((DEEP / "04_CORE_SETTLEMENTS").glob("*/00_Master.md"))
    cmd_docs = list((DEEP / "05_COMMANDERY_REGIONAL_REFERENCE").glob("*.md"))
    county_docs = list((DEEP / "06_PRIORITY_COUNTIES").glob("*.md"))
    clan_docs = list((DEEP / "07_ELITE_CLANS_AND_ESTATES").glob("*.md"))
    scenario_docs = list((DEEP / "12_SCENARIO_WORLD_REFERENCE").glob("*.md"))
    check("Markdown Reference Audit", len(core_dirs) == 133 and len(cmd_docs) == 105 and len(county_docs) == 250 and len(clan_docs) == 39 and len(scenario_docs) == 13, f"core={len(core_dirs)},cmd={len(cmd_docs)},county={len(county_docs)},clan={len(clan_docs)},scenario={len(scenario_docs)}")
    check("Core 40-topic Structure", all(sum(1 for line in p.read_text(encoding="utf-8").splitlines() if line.startswith("## ")) == 40 for p in core_dirs), "all masters require 40 sections")

    broken_links = []
    markdown_files = [DEEP / "README_历史世界深化资料索引.md", ROOT / "Docs" / "HISTORICAL_WORLD_REFERENCE" / "README_历史世界开发参考资料索引.md"]
    for md in markdown_files:
        text = md.read_text(encoding="utf-8")
        for target in re.findall(r"\[[^\]]+\]\(([^)]+)\)", text):
            if target.startswith(("http://", "https://", "#")): continue
            path = (md.parent / target).resolve()
            if not path.exists(): broken_links.append(f"{md.name}->{target}")
    check("Broken Link Audit", not broken_links, str(broken_links))

    build_report = load(OUT / "workbook_build_report.json")
    workbook_errors = []
    for item in build_report:
        workbook = ROOT / item["file"]
        if not workbook.exists(): workbook_errors.append(f"missing:{item['file']}"); continue
        try:
            with zipfile.ZipFile(workbook) as archive:
                if "xl/workbook.xml" not in archive.namelist() or len([n for n in archive.namelist() if n.startswith("xl/worksheets/sheet") and n.endswith(".xml")]) < 2:
                    workbook_errors.append(f"structure:{item['file']}")
        except zipfile.BadZipFile:
            workbook_errors.append(f"badzip:{item['file']}")
    check("Workbook Structure Audit", len(build_report) == 17 and not workbook_errors, f"workbooks={len(build_report)} errors={workbook_errors}")
    check("Formula Error Audit", all(x.get("formulaErrors") == 0 for x in build_report), "artifact-tool inspection reports zero errors")
    check("P0 Package Coverage", len(list((DEEP / "04_CORE_SETTLEMENTS").glob("P0_*/02-12_结构化时间轴与开发参考.xlsx"))) == 8, "8 merged P0 workbooks")
    check("No Runtime Entity Additions", coverage["historical_person_additions"] == coverage["clan_additions"] == coverage["branch_additions"] == 0, str({k:coverage[k] for k in ["historical_person_additions","clan_additions","branch_additions"]}))

    report = {"task":"HAN-135-260-HISTORICAL-WORLD-REFERENCE-DEEPENING-V1", "passed":all(x["passed"] for x in checks), "checks":checks, "coverage":coverage}
    (OUT / "validation_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    for item in checks: print(("PASS" if item["passed"] else "FAIL"), item["name"], item["detail"])
    print("RESULT", "PASS" if report["passed"] else "FAIL")
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
