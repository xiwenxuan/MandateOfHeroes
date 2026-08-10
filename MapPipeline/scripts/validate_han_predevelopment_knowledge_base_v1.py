#!/usr/bin/env python3
"""Validate HAN-PREDEVELOPMENT-KNOWLEDGE-BASE-CONSOLIDATION-V1 outputs."""

from __future__ import annotations

import json
import zipfile
from collections import Counter
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "outputs" / "HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1"
FAMILY_DIR = REPO / "Docs" / "HISTORICAL_WORLD_REFERENCE" / "FAMILY_SPATIAL_CONSOLIDATION_V1"
KB_DIR = REPO / "Docs" / "KNOWLEDGE_BASE"
MASTER_DATA = REPO / "outputs" / "HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1" / "data"
DEEPENING = REPO / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1" / "deepening_workdata.json"
WORKDATA = OUT / "knowledge_base_workdata.json"

FAMILY_BOOKS = [
    "A01_135-260重要地点家族空间总索引.xlsx",
    "A02_133核心聚落HistoricalFamilySpatialReference.xlsx",
    "A03_250重点县HistoricalFamilySpatialReference.xlsx",
    "A04_HistoricalClan_135-260_SpatialTimeline.xlsx",
    "A05_HistoricalBranch_135-260_SpatialTimeline.xlsx",
    "A06_13Scenario_FamilySpatialSnapshots.xlsx",
    "A07_HistoricalResidence_Estate_AssetReference.xlsx",
    "A08_FamilyOrganizationInitializationReference_V2.xlsx",
    "A09_FamilyCenterCandidateReference.xlsx",
    "A10_HistoricalFamilySpatialConflictQueue.xlsx",
]
GOV_BOOKS = [
    "PROJECT_DOCUMENT_REGISTRY.xlsx",
    "PROJECT_CANONICAL_DOMAIN_MAP.xlsx",
    "DESIGN_DECISION_REGISTRY.xlsx",
    "OPEN_DECISION_REGISTRY.xlsx",
    "DOCUMENT_CONFLICT_REGISTER.xlsx",
    "IMPLEMENTATION_GAP_REGISTER.xlsx",
    "RESEARCH_GAP_REGISTER.xlsx",
]
MANIFESTS = [
    "LUOYANG_184_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "CHANGAN_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "YE_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "XU_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "CHENGDU_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "XIANGYANG_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "JIANGLING_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "JIANYE_DEVELOPMENT_REFERENCE_MANIFEST.md",
]
CORE_GOVERNED_DOCS = [
    "Docs/GAME_VISION_AND_GAMEPLAY.md",
    "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md",
    "Docs/WORLD_SIMULATION_FOUNDATION.md",
    "Docs/DATA_AND_CONTENT_FOUNDATION.md",
    "Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md",
    "Docs/SANDBOX_NPC_AI.md",
    "Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md",
    "Docs/CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md",
    "Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md",
    "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md",
    "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md",
    "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/02_FamilyCenter设计规则_V1.md",
    "Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md",
    "Docs/HISTORICAL_SCENARIOS_TIMELINE_AND_FATE_DECISIONS.md",
    "Docs/LEGAL_AND_ASSETS.md",
    "Docs/MAP_ART_RESOURCE_PLAN.md",
]
ALLOWED_PRESENCE = {
    "UNKNOWN", "NONE", "MEMBER_PRESENCE", "CLAN_PRESENCE", "RESIDENCE_PRESENCE",
    "BRANCH_PRESENCE", "ASSET_PRESENCE", "ESTATE_PRESENCE",
    "FAMILY_ORGANIZATION_CANDIDATE", "CENTER_CANDIDATE",
}


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def split_ids(value):
    if not value:
        return []
    if isinstance(value, list):
        return [str(item) for item in value if item]
    return [item for item in str(value).split("|") if item]


checks: list[dict] = []
errors: list[str] = []


def check(name: str, condition: bool, details: str):
    checks.append({"name": name, "status": "PASS" if condition else "FAIL", "details": details})
    if not condition:
        errors.append(f"{name}: {details}")


d = load(WORKDATA)
deep = load(DEEPENING)
persons = load(MASTER_DATA / "persons.json")["persons"]
clans = load(MASTER_DATA / "clans.json")["clans"]
branches = load(MASTER_DATA / "branches.json")["branches"]
scenarios = load(MASTER_DATA / "scenario_index.json")["scenarios"]
person_ids = {row["person_id"] for row in persons}
clan_ids = {row["clan_id"] for row in clans}
branch_ids = {row["branch_id"] for row in branches}
scenario_ids = {row["scenario_id"] for row in scenarios}
scenario_years = {int(row["year"]) for row in scenarios}
runtime_family_path = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184UrbanInitializationV1" / "family_organizations.json"
runtime_family_organizations = load(runtime_family_path)["organizations"]
runtime_head_person_ids = {row["head_person_id"] for row in runtime_family_organizations if row.get("head_person_id")}

check("core_settlement_count", len(d["a02_core_settlements"]) == len(deep["core_settlements"]) == 133,
      f"generated={len(d['a02_core_settlements'])}; source={len(deep['core_settlements'])}")
check("priority_county_count", len(d["a03_priority_counties"]) == len(deep["priority_counties"]) == 250,
      f"generated={len(d['a03_priority_counties'])}; source={len(deep['priority_counties'])}")
check("master_identity_counts", (len(person_ids), len(clan_ids), len(branch_ids)) == (1202, 39, 15),
      f"persons={len(person_ids)}; clans={len(clan_ids)}; branches={len(branch_ids)}")
check("scenario_contract", len(scenario_ids) == 13 and scenario_years == {140, 184, 189, 194, 200, 207, 214, 219, 223, 227, 234, 249, 260},
      f"count={len(scenario_ids)}; years={sorted(scenario_years)}")

place_rows = d["a01_important_places"] + d["a02_core_settlements"] + d["a03_priority_counties"]
check("query_status_explicit", all(row.get("QueryStatus") for row in place_rows),
      f"rows={len(place_rows)}; missing={sum(not row.get('QueryStatus') for row in place_rows)}")
levels = {row.get("HighestPresenceLevel") for row in place_rows}
timeline_levels = {row.get("PresenceLevel") for row in d["a04_clan_timeline"] + d["a05_branch_timeline"]}
check("presence_level_vocabulary", (levels | timeline_levels) <= ALLOWED_PRESENCE,
      f"observed={sorted(levels | timeline_levels)}")
check("unknown_not_collapsed_to_none", any(row.get("HighestPresenceLevel") == "UNKNOWN" for row in place_rows),
      f"unknown={sum(row.get('HighestPresenceLevel') == 'UNKNOWN' for row in place_rows)}")

referenced_clans = set()
referenced_branches = set()
referenced_persons = set()
for rows in (d["a01_important_places"], d["a04_clan_timeline"], d["a05_branch_timeline"],
             d["a06_scenario_snapshots"], d["a07_residence_estate_assets"], d["a08_initialization_v2"]):
    for row in rows:
        referenced_clans.update(split_ids(row.get("ClanId")))
        referenced_clans.update(split_ids(row.get("HistoricalClanIds")))
        referenced_branches.update(split_ids(row.get("BranchId")))
        referenced_branches.update(split_ids(row.get("BranchIds")))
        referenced_persons.update(split_ids(row.get("PersonId")))
        referenced_persons.update(split_ids(row.get("PersonIds")))
        referenced_persons.update(split_ids(row.get("HistoricalPersonIds")))
        referenced_persons.update(split_ids(row.get("AliveImportantPersonIds")))
        referenced_persons.update(split_ids(row.get("FounderPersonId")))
check("clan_references_resolve", referenced_clans <= clan_ids,
      f"references={len(referenced_clans)}; unknown={sorted(referenced_clans - clan_ids)[:10]}")
check("branch_references_resolve", referenced_branches <= branch_ids,
      f"references={len(referenced_branches)}; unknown={sorted(referenced_branches - branch_ids)[:10]}")
valid_person_ids = person_ids | runtime_head_person_ids
check("person_references_resolve", referenced_persons <= valid_person_ids,
      f"references={len(referenced_persons)}; historical={len(referenced_persons & person_ids)}; runtime_heads={len(referenced_persons & runtime_head_person_ids)}; unknown={sorted(referenced_persons - valid_person_ids)[:10]}")

for key in ("a04_clan_timeline", "a05_branch_timeline"):
    rows = d[key]
    ids = [row["TimelineRecordId"] for row in rows]
    check(f"{key}_record_ids_unique", len(ids) == len(set(ids)), f"rows={len(ids)}; unique={len(set(ids))}")
    check(f"{key}_year_ranges_valid", all(135 <= int(row["StartYear"]) <= int(row["EndYear"]) <= 260 for row in rows),
          f"invalid={sum(not (135 <= int(row['StartYear']) <= int(row['EndYear']) <= 260) for row in rows)}")

snapshot_rows = d["a06_scenario_snapshots"]
check("scenario_snapshots_resolve", all(row["ScenarioId"] in scenario_ids and int(row["Year"]) in scenario_years for row in snapshot_rows),
      f"rows={len(snapshot_rows)}")
check("all_clans_have_all_scenarios", all(
    {(row["ClanId"], row["ScenarioId"]) for row in snapshot_rows if row.get("ClanId")} >=
    {(clan_id, scenario_id) for clan_id in clan_ids for scenario_id in scenario_ids}
    for _ in [0]), f"expected_pairs={len(clan_ids) * len(scenario_ids)}")
check("snapshots_never_activate_center", all(str(row.get("ActiveCenter", "")).upper() not in {"YES", "TRUE", "ACTIVE"} for row in snapshot_rows),
      f"active={sum(str(row.get('ActiveCenter', '')).upper() in {'YES', 'TRUE', 'ACTIVE'} for row in snapshot_rows)}")

reference_kinds = {row["ReferenceKind"] for row in d["a07_residence_estate_assets"]}
check("residence_estate_asset_separated", len(reference_kinds) >= 3 and any("RESIDENCE" in x for x in reference_kinds) and
      any("ESTATE" in x for x in reference_kinds) and any("ASSET" in x for x in reference_kinds),
      f"kinds={sorted(reference_kinds)}")
check("reference_assets_never_active_center", all(str(row.get("ActiveCenter", "")).upper() not in {"YES", "TRUE", "ACTIVE"} for row in d["a07_residence_estate_assets"]),
      f"rows={len(d['a07_residence_estate_assets'])}")
check("initialization_reference_only", all(row.get("MaterializationPolicy") == "REFERENCE_ONLY_DO_NOT_INSTANTIATE" and
      row.get("InitializationDecision") == "REFERENCE_ONLY_DO_NOT_INSTANTIATE" for row in d["a08_initialization_v2"]),
      f"rows={len(d['a08_initialization_v2'])}")
check("clan_not_family_organization", all(str(row.get("ClanEqualsFamilyOrganization", "")).upper() == "NO" for row in d["a08_initialization_v2"]),
      f"rows={len(d['a08_initialization_v2'])}")
check("center_candidates_not_designated", all(str(row.get("ActiveCenter", "")).upper() == "NO" and not row.get("ExistingFacilityId") for row in d["a09_center_candidates"]),
      f"rows={len(d['a09_center_candidates'])}")
check("family_conflicts_are_actionable", all(row.get("Status") and row.get("RequiredAction") for row in d["a10_family_conflicts"]),
      f"rows={len(d['a10_family_conflicts'])}")

registry = d["b01_document_registry"]
paths = [row["Path"] for row in registry]
doc_ids = [row["DocumentId"] for row in registry]
missing_registry_paths = [p for p in paths if not (REPO / p).exists()]
check("document_registry_unique", len(paths) == len(set(paths)) and len(doc_ids) == len(set(doc_ids)),
      f"rows={len(paths)}; unique_paths={len(set(paths))}; unique_ids={len(set(doc_ids))}")
check("document_registry_paths_exist", not missing_registry_paths,
      f"missing={missing_registry_paths[:10]}")
check("domain_map_complete", len(d["b02_domain_map"]) >= 33 and all(row.get("L0ProjectConstitution") or row.get("L1CanonicalSpec") or row.get("CanonicalGap") for row in d["b02_domain_map"]),
      f"domains={len(d['b02_domain_map'])}; incomplete={sum(not (r.get('L0ProjectConstitution') or r.get('L1CanonicalSpec') or r.get('CanonicalGap')) for r in d['b02_domain_map'])}")
check("tasks_are_l4_except_m12", all(row["AuthorityLevel"] == "L4" for row in registry
      if row["DocumentType"] == "Task" and row["Path"] != "Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md"),
      f"tasks={sum(row['DocumentType'] == 'Task' for row in registry)}")
check("reports_not_canonical", all(row["AuthorityLevel"] not in {"L0", "L1"} and not row.get("CanonicalFor") for row in registry
      if row["DocumentType"] == "ImplementationOrAcceptanceReport"),
      f"reports={sum(row['DocumentType'] == 'ImplementationOrAcceptanceReport' for row in registry)}")
check("reference_analysis_not_implementation", all(row["Status"] == "RESEARCH_REFERENCE" for row in registry if row["DocumentType"] == "ReferenceAnalysis"),
      f"references={sum(row['DocumentType'] == 'ReferenceAnalysis' for row in registry)}")

edges = {row["Path"]: row.get("SupersededBy") for row in registry if row.get("SupersededBy")}
cycle_nodes = []
for start in edges:
    seen = set()
    node = start
    while node in edges:
        if node in seen:
            cycle_nodes.append(node)
            break
        seen.add(node)
        node = edges[node]
check("superseded_graph_acyclic", not cycle_nodes, f"cycle_nodes={cycle_nodes}")
archived_paths = {row["Path"] for row in registry if row["Status"] == "ARCHIVED"}
canonical_refs = "|".join(str(row.get("L1CanonicalSpec", "")) for row in d["b02_domain_map"])
check("archived_not_domain_canonical", all(path not in canonical_refs for path in archived_paths), f"archived={len(archived_paths)}")

header_tokens = ("## Document Governance", "Purpose", "Authority", "Covers", "DoesNotCover", "Supersedes", "SupersededBy", "Status")
bad_headers = []
for rel in CORE_GOVERNED_DOCS:
    text = (REPO / rel).read_text(encoding="utf-8")
    if not all(token in text[:5000] for token in header_tokens):
        bad_headers.append(rel)
check("core_governance_headers", not bad_headers, f"bad={bad_headers}")

manifest_required = ("TargetPlace", "CanonicalSystemDocs", "HistoricalReferenceDocs", "PopulationDataset", "PersonDataset",
                     "ClanDataset", "FacilityReference", "TransportReference", "MilitaryReference", "ExistingImplementation",
                     "KnownConflicts", "KnownResearchGaps", "KnownImplementationGaps", "DoNotUseDocs", "RecommendedReadingOrder")
bad_manifests = []
for name in MANIFESTS:
    path = KB_DIR / "DEVELOPMENT_MANIFESTS" / name
    if not path.exists():
        bad_manifests.append(f"{name}:missing")
        continue
    text = path.read_text(encoding="utf-8")
    missing = [token for token in manifest_required if token not in text]
    if missing:
        bad_manifests.append(f"{name}:{','.join(missing)}")
check("eight_city_manifests", not bad_manifests, f"bad={bad_manifests}")

required_md = [
    FAMILY_DIR / "README.md", FAMILY_DIR / "A11_全国重要地点家族空间开发参考_V1.md",
    KB_DIR / "README_PROJECT_KNOWLEDGE_BASE.md", KB_DIR / "DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md",
    KB_DIR / "CODING_TASK_REFERENCE_PROTOCOL.md", KB_DIR / "DOCUMENT_GOVERNANCE_REPORT_V1.md",
]
check("required_markdown_outputs", all(path.exists() and path.stat().st_size > 500 for path in required_md),
      f"files={len(required_md)}")
link_audit = load(OUT / "link_audit.json")
check("markdown_link_audit", len(link_audit) == 0, f"issues={len(link_audit)}")

workbooks = [FAMILY_DIR / name for name in FAMILY_BOOKS] + [KB_DIR / "REGISTRY" / name for name in GOV_BOOKS]
invalid_books = [str(path.relative_to(REPO)) for path in workbooks if not path.exists() or not zipfile.is_zipfile(path)]
check("xlsx_outputs_valid", not invalid_books and len(workbooks) == 17, f"invalid={invalid_books}; count={len(workbooks)}")
build_report = load(OUT / "workbook_build_report.json")
check("workbook_formula_scan", len(build_report) == 17 and all(row.get("formulaErrors") == 0 for row in build_report),
      f"entries={len(build_report)}; errors={sum(row.get('formulaErrors', 1) for row in build_report)}")
check("workbook_render_evidence", len(list((OUT / "previews").glob("*.png"))) == 34,
      f"previews={len(list((OUT / 'previews').glob('*.png')))}")
check("workbook_inspection_evidence", len(list((OUT / "inspections").glob("*.ndjson"))) == 34 and
      len(list((OUT / "artifact_tool_sidecars").glob("*.ndjson"))) == 17,
      f"inspections={len(list((OUT / 'inspections').glob('*.ndjson')))}; sidecars={len(list((OUT / 'artifact_tool_sidecars').glob('*.ndjson')))}")

all_md = [REPO / row["Path"] for row in registry if row["Path"].lower().endswith(".md") and (REPO / row["Path"]).exists()]
encoding_errors = []
for path in all_md:
    try:
        path.read_text(encoding="utf-8")
    except UnicodeDecodeError as exc:
        encoding_errors.append(f"{path.relative_to(REPO)}:{exc}")
check("registered_markdown_utf8", not encoding_errors, f"files={len(all_md)}; errors={encoding_errors[:5]}")

summary = {
    "task": "HAN-PREDEVELOPMENT-KNOWLEDGE-BASE-CONSOLIDATION-V1",
    "status": "PASS" if not errors else "FAIL",
    "checks": checks,
    "metrics": {
        "persons": len(person_ids), "clans": len(clan_ids), "branches": len(branch_ids),
        "core_settlements": len(d["a02_core_settlements"]), "priority_counties": len(d["a03_priority_counties"]),
        "scenario_snapshots": len(snapshot_rows), "documents": len(registry), "domains": len(d["b02_domain_map"]),
        "workbooks": len(workbooks), "manifests": len(MANIFESTS),
        "authority_counts": dict(Counter(row["AuthorityLevel"] for row in registry)),
        "status_counts": dict(Counter(row["Status"] for row in registry)),
    },
    "errors": errors,
}
(OUT / "validation_summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(summary, ensure_ascii=False, indent=2))
raise SystemExit(0 if not errors else 1)
