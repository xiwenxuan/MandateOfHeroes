#!/usr/bin/env python3
"""Build deterministic research-report metrics from the V1 runtime package."""

from __future__ import annotations

import json
from collections import Counter, defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
OUTPUT_ROOT = ROOT / "outputs" / "HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1"
DATA = OUTPUT_ROOT / "data"
SCENARIOS = OUTPUT_ROOT / "12_135-260_HistoricalScenarioSnapshots"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def province_from_region_id(region_id: str | None) -> str:
    if not region_id:
        return "Unresolved"
    parts = region_id.split(".")
    return parts[2] if len(parts) > 2 and parts[0] == "admin" else "Other"


def main() -> None:
    persons = load(DATA / "persons.json")["persons"]
    clans = load(DATA / "clans.json")["clans"]
    branches = load(DATA / "branches.json")["branches"]
    kinship = load(DATA / "kinship.json")["relations"]
    marriages = load(DATA / "marriages.json")["marriages"]
    locations = load(DATA / "person_locations.json")["records"]
    summary = load(DATA / "summary.json")
    scenario_184 = load(SCENARIOS / "184.json")

    person_by_id = {person["person_id"]: person for person in persons}
    clan_by_id = {clan["clan_id"]: clan for clan in clans}
    branch_by_id = {branch["branch_id"]: branch for branch in branches}

    def effective_clan(person):
        if person.get("clan_id"):
            return person["clan_id"]
        branch = branch_by_id.get(person.get("lineage_branch_id"))
        return branch.get("clan_id") if branch else None

    clan_members = Counter()
    for person in persons:
        clan_id = effective_clan(person)
        if clan_id:
            clan_members[clan_id] += 1

    parent_ids = set()
    child_ids = set()
    for relation in kinship:
        relation_type = relation.get("relation_type", "")
        if "Parent" in relation_type:
            parent_ids.add(relation["person_a_id"])
            child_ids.add(relation["person_b_id"])

    clan_kinship_edges = Counter()
    for relation in kinship:
        clan_a = effective_clan(person_by_id[relation["person_a_id"]])
        clan_b = effective_clan(person_by_id[relation["person_b_id"]])
        if clan_a and clan_a == clan_b:
            clan_kinship_edges[clan_a] += 1

    clan_marriage_edges = Counter()
    interclan_pairs = Counter()
    for marriage in marriages:
        clan_a = effective_clan(person_by_id[marriage["person_a_id"]])
        clan_b = effective_clan(person_by_id[marriage["person_b_id"]])
        if clan_a:
            clan_marriage_edges[clan_a] += 1
        if clan_b and clan_b != clan_a:
            clan_marriage_edges[clan_b] += 1
        if clan_a and clan_b and clan_a != clan_b:
            interclan_pairs[tuple(sorted((clan_a, clan_b)))] += 1

    known_location_by_person = {}
    for record in locations:
        if record.get("region_id") and record.get("start_year", 135) <= 184 <= (record.get("end_year") or 260):
            known_location_by_person.setdefault(record["person_id"], record["region_id"])

    alive_184 = [person_by_id[item["person_id"]] for item in scenario_184["persons"]]
    current_184 = Counter()
    native_184 = Counter()
    for snapshot_person in scenario_184["persons"]:
        current_184[province_from_region_id(snapshot_person.get("current_region_id"))] += 1
    for person in alive_184:
        native_184[province_from_region_id(person.get("native_place_region_id"))] += 1

    largest_networks = []
    for clan_id, member_count in clan_members.most_common():
        clan = clan_by_id.get(clan_id, {})
        largest_networks.append(
            {
                "clan_id": clan_id,
                "name": clan.get("canonical_clan_name", clan_id),
                "member_count": member_count,
                "kinship_edge_count": clan_kinship_edges[clan_id],
                "marriage_edge_count": clan_marriage_edges[clan_id],
            }
        )

    complex_marriage_clans = sorted(
        (
            {
                "clan_id": clan_id,
                "name": clan_by_id.get(clan_id, {}).get("canonical_clan_name", clan_id),
                "marriage_edge_count": count,
            }
            for clan_id, count in clan_marriage_edges.items()
        ),
        key=lambda item: (-item["marriage_edge_count"], item["clan_id"]),
    )

    result = {
        "schema": "mandate.historical-person-clan-report-metrics.v1",
        "summary": summary,
        "people_with_child_relation": len(parent_ids),
        "people_recorded_as_child": len(child_ids),
        "alive_184_current_location_by_province": dict(sorted(current_184.items())),
        "alive_184_native_place_by_province": dict(sorted(native_184.items())),
        "alive_184_known_current_location_count": sum(v for k, v in current_184.items() if k != "Unresolved"),
        "alive_184_known_native_place_count": sum(v for k, v in native_184.items() if k != "Unresolved"),
        "largest_clan_networks": largest_networks[:15],
        "most_complex_marriage_clans": complex_marriage_clans[:15],
        "interclan_marriage_pairs": [
            {
                "clan_a": clan_by_id.get(pair[0], {}).get("canonical_clan_name", pair[0]),
                "clan_b": clan_by_id.get(pair[1], {}).get("canonical_clan_name", pair[1]),
                "count": count,
            }
            for pair, count in interclan_pairs.most_common(20)
        ],
    }
    output = OUTPUT_ROOT / "report_metrics.json"
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
