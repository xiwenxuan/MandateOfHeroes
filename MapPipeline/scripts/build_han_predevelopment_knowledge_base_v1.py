from __future__ import annotations

import hashlib
import json
import re
from collections import Counter, defaultdict
from datetime import date, datetime
from pathlib import Path
from urllib.parse import unquote


REPO = Path(__file__).resolve().parents[2]
DOCS = REPO / "Docs"
OUT = REPO / "outputs" / "HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1"
FAMILY_OUT = DOCS / "HISTORICAL_WORLD_REFERENCE" / "FAMILY_SPATIAL_CONSOLIDATION_V1"
KB_OUT = DOCS / "KNOWLEDGE_BASE"
REGISTRY_OUT = KB_OUT / "REGISTRY"
MANIFEST_OUT = KB_OUT / "DEVELOPMENT_MANIFESTS"
HIST = REPO / "Assets" / "StreamingAssets" / "HistoricalPersons" / "Han135260V1"
DEEPENING_PATH = REPO / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1" / "deepening_workdata.json"
FAMILY_V1_PATH = REPO / "outputs" / "FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1" / "family_reference_workdata.json"
TODAY = "2026-08-11"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def write(path: Path, text: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def pipe(values):
    return "|".join(str(value) for value in values if value not in (None, ""))


def sorted_pipe(values):
    return pipe(sorted({str(value) for value in values if value not in (None, "")}))


def posix(path: Path):
    return path.relative_to(REPO).as_posix()


def sha12(text: str):
    return hashlib.sha1(text.encode("utf-8")).hexdigest()[:12]


deep = load(DEEPENING_PATH)
family_v1 = load(FAMILY_V1_PATH)
clans = load(HIST / "clans.json")["clans"]
branches = load(HIST / "branches.json")["branches"]
persons = load(HIST / "persons.json")["persons"]
clan_presence = load(HIST / "clan_presence.json")["records"]
person_locations = load(HIST / "person_locations.json")["records"]
scenario_years = [140, 184, 189, 194, 200, 207, 214, 219, 223, 227, 234, 249, 260]
scenarios = {year: load(HIST / "scenarios" / f"{year}.json") for year in scenario_years}
core = deep["core_settlements"]
priority_counties = deep["priority_counties"]
estates = deep["estate_references"]

clan_by_id = {row["clan_id"]: row for row in clans}
branch_by_id = {row["branch_id"]: row for row in branches}
person_by_id = {row["person_id"]: row for row in persons}
core_by_county = {row["county_id"]: row for row in core}
priority_by_county = {row["county_id"]: row for row in priority_counties}
core_by_place = {row["place_id"]: row for row in core}
core_by_city_name = {}
for row in core:
    core_by_city_name[row["display_name"]] = row
    for name in str(row.get("city_names") or "").split("|"):
        if name:
            core_by_city_name[name] = row


def resolve_scope(scope_id: str | None):
    if not scope_id:
        return {"scope_id": "", "place_id": "", "county_id": "", "status": "UNRESOLVED"}
    if scope_id in core_by_place:
        row = core_by_place[scope_id]
        return {"scope_id": scope_id, "place_id": row["place_id"], "county_id": row["county_id"], "status": "CORE_PLACE_RESOLVED"}
    if scope_id in core_by_county:
        row = core_by_county[scope_id]
        return {"scope_id": scope_id, "place_id": row["place_id"], "county_id": scope_id, "status": "CORE_COUNTY_RESOLVED"}
    if scope_id in priority_by_county:
        return {"scope_id": scope_id, "place_id": "", "county_id": scope_id, "status": "PRIORITY_COUNTY_RESOLVED"}
    if scope_id.startswith("city.han."):
        name = scope_id.split(".")[-1]
        row = core_by_city_name.get(name) or core_by_city_name.get("许昌" if name == "许" else name)
        if row:
            return {"scope_id": scope_id, "place_id": row["place_id"], "county_id": row["county_id"], "status": "CITY_TO_CORE_RESOLVED"}
    return {"scope_id": scope_id, "place_id": "", "county_id": "", "status": "REGION_OR_UNKNOWN_SCOPE"}


LEVEL_RANK = {
    "NONE": 0,
    "UNKNOWN": 0,
    "MEMBER_PRESENCE": 1,
    "CLAN_PRESENCE": 2,
    "RESIDENCE_PRESENCE": 3,
    "BRANCH_PRESENCE": 4,
    "ASSET_PRESENCE": 5,
    "ESTATE_PRESENCE": 6,
    "FAMILY_ORGANIZATION_CANDIDATE": 7,
    "CENTER_CANDIDATE": 8,
}
ALLOWED_LEVELS = set(LEVEL_RANK)


evidence_by_county = defaultdict(list)
region_only_evidence = []


def add_evidence(scope_id, level, clan_id="", branch_id="", person_ids=None, reference_id="", grade="UNKNOWN", notes=""):
    resolution = resolve_scope(scope_id)
    raw_clan_id = clan_id or ""
    canonical_clan_id = raw_clan_id if raw_clan_id in clan_by_id else ""
    row = {
        "ScopeId": scope_id or "",
        "PlaceId": resolution["place_id"],
        "CountyId": resolution["county_id"],
        "ResolutionStatus": resolution["status"],
        "PresenceLevel": level,
        "ClanId": canonical_clan_id,
        "UnresolvedRuntimeClanId": raw_clan_id if raw_clan_id and not canonical_clan_id else "",
        "BranchId": branch_id or "",
        "PersonIds": sorted_pipe(person_ids or []),
        "ReferenceId": reference_id,
        "EvidenceGrade": grade,
        "Notes": notes,
    }
    if resolution["county_id"]:
        evidence_by_county[resolution["county_id"]].append(row)
    else:
        region_only_evidence.append(row)
    return row


for row in clan_presence:
    add_evidence(
        row.get("county_permanent_id") or row.get("region_permanent_id"),
        "CLAN_PRESENCE",
        row["clan_id"],
        row.get("branch_id") or "",
        row.get("major_members") or [],
        row["presence_id"],
        "HISTORICAL" if row.get("evidence_level") == "A" else "RECONSTRUCTED",
        row.get("notes") or "",
    )

for row in person_locations:
    person = person_by_id.get(row["person_id"], {})
    if not person.get("clan_id"):
        continue
    scope = row.get("city_id") or row.get("county_id") or row.get("region_id")
    add_evidence(
        scope,
        "MEMBER_PRESENCE",
        person.get("clan_id", ""),
        person.get("branch_id", ""),
        [row["person_id"]],
        row["record_id"],
        "HISTORICAL" if row.get("confidence") == "A" else "RECONSTRUCTED",
        f"{row.get('location_type', '')}; {row.get('start_year')}—{row.get('end_year') or 'open'}",
    )

for row in estates:
    add_evidence(
        row.get("county_id"),
        "ESTATE_PRESENCE",
        row.get("clan_id", ""),
        row.get("branch_id", ""),
        str(row.get("historical_person_ids") or "").split("|"),
        row["estate_reference_id"],
        row.get("evidence_level", "UNKNOWN"),
        row.get("historical_description", ""),
    )

for row in family_v1["residence_estate_assets"]:
    kind = row["ReferenceKind"]
    if kind == "ESTATE_EVIDENCE":
        continue
    level = "RESIDENCE_PRESENCE" if "RESIDENCE" in kind else "ASSET_PRESENCE"
    add_evidence(
        row.get("LocationScopeId"),
        level,
        row.get("ClanId", ""),
        row.get("BranchId", ""),
        str(row.get("PersonId") or "").split("|"),
        row["ReferenceId"],
        row.get("EvidenceGrade", "UNKNOWN"),
        row.get("EvidenceDescription", ""),
    )

for row in family_v1["initialization_reference"]:
    add_evidence(
        row.get("ExpectedManagementAreaId"),
        "FAMILY_ORGANIZATION_CANDIDATE",
        row.get("ClanId", ""),
        row.get("BranchId", ""),
        [],
        row["ReferenceId"],
        row.get("CandidateLevel", "MODELED"),
        row.get("Reason", ""),
    )

for row in family_v1["luoyang_center_candidates"]:
    if row["CenterCandidateLevel"] == "UNKNOWN" or row["DesignationRecommendation184"] == "不指定":
        continue
    add_evidence(
        row.get("ManagementAreaId"),
        "CENTER_CANDIDATE",
        "",
        "",
        [],
        row["CandidateId"],
        row["CenterCandidateLevel"],
        row["DevelopmentAdvice"],
    )


def aggregate_place(row, scope_kind):
    county_id = row["county_id"]
    evidence = evidence_by_county.get(county_id, [])
    levels = [item["PresenceLevel"] for item in evidence]
    highest = max(levels, key=lambda value: LEVEL_RANK[value]) if levels else "UNKNOWN"
    by_level = defaultdict(list)
    for item in evidence:
        by_level[item["PresenceLevel"]].append(item)
    return {
        "PlaceId": row.get("place_id", ""),
        "CountyId": county_id,
        "DisplayName": row["display_name"],
        "ScopeKind": scope_kind,
        "ProvinceId": row.get("province_id", ""),
        "CommanderyId": row.get("commandery_id", ""),
        "Priority": row.get("priority", ""),
        "ReferenceLevel": row.get("reference_level", ""),
        "HighestPresenceLevel": highest,
        "HistoricalClanIds": sorted_pipe(item["ClanId"] for item in evidence),
        "UnresolvedRuntimeClanIds": sorted_pipe(item["UnresolvedRuntimeClanId"] for item in evidence),
        "BranchIds": sorted_pipe(item["BranchId"] for item in evidence),
        "HistoricalPersonIds": sorted_pipe(pid for item in evidence for pid in item["PersonIds"].split("|") if pid),
        "MemberPresenceCount": len(by_level["MEMBER_PRESENCE"]),
        "ResidenceEvidenceCount": len(by_level["RESIDENCE_PRESENCE"]),
        "BranchOrClanPresenceCount": len(by_level["BRANCH_PRESENCE"]) + len(by_level["CLAN_PRESENCE"]),
        "EstateEvidenceCount": len(by_level["ESTATE_PRESENCE"]),
        "FamilyAssetEvidenceCount": len(by_level["ASSET_PRESENCE"]),
        "FamilyOrganizationCandidateCount": len(by_level["FAMILY_ORGANIZATION_CANDIDATE"]),
        "CenterCandidateCount": len(by_level["CENTER_CANDIDATE"]),
        "ReferenceIds": sorted_pipe(item["ReferenceId"] for item in evidence),
        "EvidenceGrades": sorted_pipe(item["EvidenceGrade"] for item in evidence),
        "QueryStatus": "REFERENCE_AVAILABLE" if evidence else "QUERYABLE_UNKNOWN",
        "Unknowns": "具体住宅、资产、组织与中心需继续研究" if evidence else "尚无家族空间证据；UNKNOWN不等于NONE",
    }


a02_core = [aggregate_place(row, "CORE_SETTLEMENT") for row in core]
a03_counties = []
for row in priority_counties:
    source = dict(row)
    source["place_id"] = core_by_county.get(row["county_id"], {}).get("place_id", "")
    a03_counties.append(aggregate_place(source, "PRIORITY_COUNTY"))

county_union = {}
for row in a03_counties + a02_core:
    key = row["CountyId"]
    if key not in county_union or row["ScopeKind"] == "CORE_SETTLEMENT":
        county_union[key] = row
a01_places = sorted(county_union.values(), key=lambda row: (row["Priority"], row["ProvinceId"], row["CommanderyId"], row["CountyId"]))
for row in region_only_evidence:
    a01_places.append(
        {
            "PlaceId": "",
            "CountyId": "",
            "DisplayName": row["ScopeId"],
            "ScopeKind": "REGION_ONLY_EVIDENCE",
            "ProvinceId": "",
            "CommanderyId": row["ScopeId"],
            "Priority": "RESEARCH_RELEVANT",
            "ReferenceLevel": "R2",
            "HighestPresenceLevel": row["PresenceLevel"],
            "HistoricalClanIds": row["ClanId"],
            "UnresolvedRuntimeClanIds": row["UnresolvedRuntimeClanId"],
            "BranchIds": row["BranchId"],
            "HistoricalPersonIds": row["PersonIds"],
            "MemberPresenceCount": 1 if row["PresenceLevel"] == "MEMBER_PRESENCE" else 0,
            "ResidenceEvidenceCount": 0,
            "BranchOrClanPresenceCount": 1 if row["PresenceLevel"] in ("CLAN_PRESENCE", "BRANCH_PRESENCE") else 0,
            "EstateEvidenceCount": 0,
            "FamilyAssetEvidenceCount": 0,
            "FamilyOrganizationCandidateCount": 1 if row["PresenceLevel"] == "FAMILY_ORGANIZATION_CANDIDATE" else 0,
            "CenterCandidateCount": 0,
            "ReferenceIds": row["ReferenceId"],
            "EvidenceGrades": row["EvidenceGrade"],
            "QueryStatus": "REGION_ONLY_REQUIRES_PLACE_RESEARCH",
            "Unknowns": "只有郡/州级空间证据，禁止强填具体核心聚落或县",
        }
    )


a04_clan_timeline = []
for row in clan_presence:
    resolution = resolve_scope(row.get("county_permanent_id") or row.get("region_permanent_id"))
    a04_clan_timeline.append(
        {
            "TimelineRecordId": f"family.timeline.clan.{sha12(row['presence_id'])}",
            "ClanId": row["clan_id"],
            "ClanName": clan_by_id[row["clan_id"]]["canonical_clan_name"],
            "BranchId": row.get("branch_id") or "",
            "StartYear": row.get("start_year", 135),
            "EndYear": row.get("end_year", 260),
            "ChangeType": "ORIGIN_BASELINE",
            "PresenceLevel": "CLAN_PRESENCE",
            "ScopeId": row.get("county_permanent_id") or row.get("region_permanent_id"),
            "PlaceId": resolution["place_id"],
            "CountyId": resolution["county_id"],
            "ResolutionStatus": resolution["status"],
            "PersonIds": sorted_pipe(row.get("major_members") or []),
            "EvidenceGrade": "HISTORICAL" if row.get("evidence_level") == "A" else "RECONSTRUCTED",
            "SourceReference": row["presence_id"],
            "StateInheritance": "有效期内继承，直到明确Change Record覆盖",
            "Unknowns": "不得由本籍Presence推导组织资产或FamilyCenter",
        }
    )
for row in person_locations:
    person = person_by_id.get(row["person_id"], {})
    if not person.get("clan_id"):
        continue
    scope = row.get("city_id") or row.get("county_id") or row.get("region_id")
    resolution = resolve_scope(scope)
    a04_clan_timeline.append(
        {
            "TimelineRecordId": f"family.timeline.member.{row['record_id'].lower()}",
            "ClanId": person["clan_id"],
            "ClanName": clan_by_id[person["clan_id"]]["canonical_clan_name"],
            "BranchId": person.get("branch_id", ""),
            "StartYear": row.get("start_year"),
            "EndYear": row.get("end_year"),
            "ChangeType": "MEMBER_LOCATION_CHANGE",
            "PresenceLevel": "MEMBER_PRESENCE",
            "ScopeId": scope,
            "PlaceId": resolution["place_id"],
            "CountyId": resolution["county_id"],
            "ResolutionStatus": resolution["status"],
            "PersonIds": row["person_id"],
            "EvidenceGrade": "HISTORICAL" if row.get("confidence") == "A" else "RECONSTRUCTED",
            "SourceReference": row["record_id"],
            "StateInheritance": "只在记录时间窗内有效，不反推Branch或组织迁入",
            "Unknowns": "住宅、资产和同族常住规模未知",
        }
    )
for row in estates:
    if not row.get("clan_id"):
        continue
    resolution = resolve_scope(row.get("county_id"))
    a04_clan_timeline.append(
        {
            "TimelineRecordId": f"family.timeline.estate.{sha12(row['estate_reference_id'])}",
            "ClanId": row["clan_id"],
            "ClanName": clan_by_id[row["clan_id"]]["canonical_clan_name"],
            "BranchId": row.get("branch_id", ""),
            "StartYear": row.get("start_year"),
            "EndYear": row.get("end_year"),
            "ChangeType": "ESTATE_EVIDENCE_CHANGE",
            "PresenceLevel": "ESTATE_PRESENCE",
            "ScopeId": row.get("county_id"),
            "PlaceId": resolution["place_id"],
            "CountyId": resolution["county_id"],
            "ResolutionStatus": resolution["status"],
            "PersonIds": row.get("historical_person_ids", ""),
            "EvidenceGrade": row.get("evidence_level", "UNKNOWN"),
            "SourceReference": row["estate_reference_id"],
            "StateInheritance": "有效期内继承；边界和设施不得自行补全",
            "Unknowns": row.get("unknowns", ""),
        }
    )
for row in family_v1["initialization_reference"]:
    resolution = resolve_scope(row.get("ExpectedManagementAreaId"))
    a04_clan_timeline.append(
        {
            "TimelineRecordId": f"family.timeline.init.{sha12(row['ReferenceId'])}",
            "ClanId": row["ClanId"],
            "ClanName": clan_by_id.get(row["ClanId"], {}).get("canonical_clan_name", row["ClanId"]),
            "BranchId": row.get("BranchId", ""),
            "StartYear": row["Year"],
            "EndYear": row["Year"],
            "ChangeType": "SCENARIO_ORGANIZATION_CANDIDATE",
            "PresenceLevel": "FAMILY_ORGANIZATION_CANDIDATE",
            "ScopeId": row["ExpectedManagementAreaId"],
            "PlaceId": resolution["place_id"],
            "CountyId": resolution["county_id"],
            "ResolutionStatus": resolution["status"],
            "PersonIds": "",
            "EvidenceGrade": row["CandidateLevel"],
            "SourceReference": row["ReferenceId"],
            "StateInheritance": "仅为该Scenario初始化候选，不自动跨年继承为运行时组织",
            "Unknowns": row["RequiredEvidenceBeforeMaterialization"],
        }
    )


a05_branch_timeline = []
clan_presence_by_clan = {row["clan_id"]: row for row in clan_presence}
for branch in branches:
    origin = clan_presence_by_clan.get(branch["clan_id"], {})
    scope = origin.get("county_permanent_id") or origin.get("region_permanent_id")
    resolution = resolve_scope(scope)
    a05_branch_timeline.append(
        {
            "TimelineRecordId": f"family.timeline.branch.{sha12(branch['branch_id'] + '.baseline')}",
            "BranchId": branch["branch_id"],
            "BranchName": branch["branch_name"],
            "ClanId": branch["clan_id"],
            "FounderPersonId": branch.get("founder_person_id", ""),
            "StartYear": origin.get("start_year", 135),
            "EndYear": origin.get("end_year", 260),
            "ChangeType": "BRANCH_RESEARCH_BASELINE_AT_CLAN_ORIGIN",
            "PresenceLevel": "BRANCH_PRESENCE",
            "ScopeId": scope,
            "PlaceId": resolution["place_id"],
            "CountyId": resolution["county_id"],
            "ResolutionStatus": resolution["status"],
            "EvidenceGrade": "RECONSTRUCTED",
            "SourceReference": branch["branch_id"],
            "Unknowns": "Branch具体分出年、迁徙年、住宅、资产与组织边界待专项研究",
        }
    )
for row in a04_clan_timeline:
    if not row["BranchId"] or row["ChangeType"] == "ORIGIN_BASELINE":
        continue
    branch = branch_by_id.get(row["BranchId"])
    if not branch:
        continue
    a05_branch_timeline.append(
        {
            "TimelineRecordId": f"family.timeline.branch.{sha12(row['TimelineRecordId'])}",
            "BranchId": row["BranchId"],
            "BranchName": branch["branch_name"],
            "ClanId": row["ClanId"],
            "FounderPersonId": branch.get("founder_person_id", ""),
            "StartYear": row["StartYear"],
            "EndYear": row["EndYear"],
            "ChangeType": row["ChangeType"],
            "PresenceLevel": row["PresenceLevel"],
            "ScopeId": row["ScopeId"],
            "PlaceId": row["PlaceId"],
            "CountyId": row["CountyId"],
            "ResolutionStatus": row["ResolutionStatus"],
            "EvidenceGrade": row["EvidenceGrade"],
            "SourceReference": row["SourceReference"],
            "Unknowns": row["Unknowns"],
        }
    )


snapshot_aggregate = {}
for year in scenario_years:
    scenario = scenarios[year]
    scenario_clans = {row["clan_id"]: row for row in scenario.get("clans", [])}
    for clan in clans:
        origin = clan_presence_by_clan.get(clan["clan_id"], {})
        scope = origin.get("county_permanent_id") or origin.get("region_permanent_id") or clan.get("primary_region_id")
        key = (year, clan["clan_id"], scope)
        snap = scenario_clans.get(clan["clan_id"], {})
        snapshot_aggregate[key] = {
            "ScenarioId": scenario["scenario_id"],
            "ScenarioName": scenario["scenario_name"],
            "Year": year,
            "ClanId": clan["clan_id"],
            "ClanName": clan["canonical_clan_name"],
            "BranchIds": set(snap.get("known_branch_ids", [])),
            "ScopeId": scope,
            "AliveImportantPersonIds": set(snap.get("major_political_member_ids", [])),
            "MemberPresence": False,
            "BranchPresence": True,
            "ResidenceEvidence": False,
            "EstateEvidence": False,
            "FamilyAssetEvidence": False,
            "PoliticalPresence": bool(snap.get("major_political_member_ids")),
            "FamilyOrganizationCandidateIds": set(),
            "PrimaryCenterCandidate": "NO_CENTER_RECOMMENDED",
            "LocalCenterCandidate": "NO_CENTER_RECOMMENDED",
            "EvidenceGrades": {"RECONSTRUCTED"},
            "Unknowns": {"本籍/郡望快照不证明运行时组织、资产或中心"},
        }


def snapshot_row(year, clan_id, scope):
    key = (year, clan_id, scope)
    if key not in snapshot_aggregate:
        scenario = scenarios[year]
        snapshot_aggregate[key] = {
            "ScenarioId": scenario["scenario_id"], "ScenarioName": scenario["scenario_name"], "Year": year,
            "ClanId": clan_id, "ClanName": clan_by_id.get(clan_id, {}).get("canonical_clan_name", clan_id),
            "BranchIds": set(), "ScopeId": scope, "AliveImportantPersonIds": set(), "MemberPresence": False,
            "BranchPresence": False, "ResidenceEvidence": False, "EstateEvidence": False, "FamilyAssetEvidence": False,
            "PoliticalPresence": False, "FamilyOrganizationCandidateIds": set(),
            "PrimaryCenterCandidate": "NO_CENTER_RECOMMENDED", "LocalCenterCandidate": "NO_CENTER_RECOMMENDED",
            "EvidenceGrades": set(), "Unknowns": set(),
        }
    return snapshot_aggregate[key]


for year in scenario_years:
    for loc in person_locations:
        if loc.get("start_year", year) > year or (loc.get("end_year") is not None and loc["end_year"] < year):
            continue
        person = person_by_id.get(loc["person_id"], {})
        clan_id = person.get("clan_id")
        if not clan_id:
            continue
        scope = loc.get("city_id") or loc.get("county_id") or loc.get("region_id")
        target = snapshot_row(year, clan_id, scope)
        target["MemberPresence"] = True
        target["AliveImportantPersonIds"].add(loc["person_id"])
        if person.get("branch_id"):
            target["BranchIds"].add(person["branch_id"])
        target["EvidenceGrades"].add("HISTORICAL" if loc.get("confidence") == "A" else "RECONSTRUCTED")
        target["Unknowns"].add("个人位置不自动升级为Branch、组织或中心")
    for estate in estates:
        if estate.get("start_year", year) > year or (estate.get("end_year") is not None and estate["end_year"] < year) or not estate.get("clan_id"):
            continue
        target = snapshot_row(year, estate["clan_id"], estate.get("county_id"))
        target["EstateEvidence"] = True
        target["EvidenceGrades"].add(estate.get("evidence_level", "UNKNOWN"))
        target["Unknowns"].add("Estate可承载中心候选但不等于Active Center")
for ref in family_v1["initialization_reference"]:
    target = snapshot_row(ref["Year"], ref["ClanId"], ref["ExpectedManagementAreaId"])
    target["FamilyOrganizationCandidateIds"].add(ref["ReferenceId"])
    if ref.get("BranchId"):
        target["BranchIds"].add(ref["BranchId"])
    kind = ref["CandidateKind"]
    if "PRIMARY" in kind or "DYNASTY" in kind or "IMPERIAL" in kind:
        target["PrimaryCenterCandidate"] = "RESEARCH_REQUIRED"
    if "LOCAL" in kind or "CAPITAL" in kind:
        target["LocalCenterCandidate"] = "RESEARCH_REQUIRED"
    target["EvidenceGrades"].add(ref["CandidateLevel"])
    target["Unknowns"].add(ref["RequiredEvidenceBeforeMaterialization"])

a06_snapshots = []
for target in snapshot_aggregate.values():
    resolution = resolve_scope(target["ScopeId"])
    a06_snapshots.append(
        {
            "ScenarioId": target["ScenarioId"], "ScenarioName": target["ScenarioName"], "Year": target["Year"],
            "ClanId": target["ClanId"], "ClanName": target["ClanName"], "BranchIds": sorted_pipe(target["BranchIds"]),
            "ScopeId": target["ScopeId"], "PlaceId": resolution["place_id"], "CountyId": resolution["county_id"],
            "ResolutionStatus": resolution["status"], "AliveImportantPersonIds": sorted_pipe(target["AliveImportantPersonIds"]),
            "MemberPresence": "YES" if target["MemberPresence"] else "NO", "BranchPresence": "YES" if target["BranchPresence"] else "NO",
            "ResidenceEvidence": "YES" if target["ResidenceEvidence"] else "NO", "EstateEvidence": "YES" if target["EstateEvidence"] else "NO",
            "FamilyAssetEvidence": "YES" if target["FamilyAssetEvidence"] else "NO", "PoliticalPresence": "YES" if target["PoliticalPresence"] else "NO",
            "FamilyOrganizationCandidateIds": sorted_pipe(target["FamilyOrganizationCandidateIds"]),
            "PrimaryCenterCandidate": target["PrimaryCenterCandidate"], "LocalCenterCandidate": target["LocalCenterCandidate"],
            "ActiveCenter": "REFERENCE_CANNOT_DECIDE", "EvidenceGrades": sorted_pipe(target["EvidenceGrades"]),
            "Unknowns": sorted_pipe(target["Unknowns"]),
        }
    )
a06_snapshots.sort(key=lambda row: (row["Year"], row["ClanId"], row["ScopeId"] or ""))

a07_assets = []
for row in family_v1["residence_estate_assets"]:
    resolution = resolve_scope(row.get("LocationScopeId"))
    copied = dict(row)
    raw_clan_id = copied.get("ClanId") or ""
    if raw_clan_id and raw_clan_id not in clan_by_id:
        copied["ClanId"] = ""
        copied["UnresolvedRuntimeClanId"] = raw_clan_id
        copied["Unknowns"] = "|".join(filter(None, [copied.get("Unknowns", ""), "运行时FamilyId尚未映射Canonical ClanId；不得伪造Clan"])).strip("|")
    else:
        copied["UnresolvedRuntimeClanId"] = ""
    copied.update({"ResolvedPlaceId": resolution["place_id"], "ResolvedCountyId": resolution["county_id"], "ResolutionStatus": resolution["status"], "ActiveCenter": "NO_REFERENCE_INFERENCE"})
    a07_assets.append(copied)

a08_initialization = []
for row in family_v1["initialization_reference"]:
    resolution = resolve_scope(row.get("ExpectedManagementAreaId"))
    copied = dict(row)
    copied.update({
        "ResolvedPlaceId": resolution["place_id"], "ResolvedCountyId": resolution["county_id"], "ResolutionStatus": resolution["status"],
        "MaterializationPolicy": "REFERENCE_ONLY_DO_NOT_INSTANTIATE", "ClanEqualsFamilyOrganization": "NO",
        "RuntimeMigrationRequired": "NO_UNTIL_SELECTED_FOR_IMPLEMENTATION",
    })
    a08_initialization.append(copied)

a09_centers = []
for row in family_v1["luoyang_center_candidates"]:
    copied = dict(row)
    copied.update({"CandidateType": "LOCAL_CENTER_CANDIDATE" if "Local" in row["DesignationRecommendation184"] else ("PRIMARY_CENTER_CANDIDATE" if "Primary" in row["DesignationRecommendation184"] else "NO_CENTER_RECOMMENDED"), "ActiveCenter": "NO", "RuntimeDecisionRequired": "YES"})
    a09_centers.append(copied)
for estate in estates:
    resolution = resolve_scope(estate.get("county_id"))
    a09_centers.append(
        {
            "CandidateId": f"center.candidate.{estate['estate_reference_id']}", "RelatedName": estate["historical_description"],
            "CandidateConclusion": "ESTATE_CAN_HOST_IF_REAL_FACILITY", "SpatialCategory": "Estate",
            "CenterCandidateLevel": "RECONSTRUCTED_CENTER_CANDIDATE" if estate["evidence_level"] in ("HISTORICAL", "RECONSTRUCTED") else "MODELED_CENTER_CANDIDATE",
            "DesignationRecommendation184": "非洛阳专项；按Scenario研究", "ExistingFacilityId": "", "ManagementAreaId": estate["county_id"],
            "RequiredBeforeDesignation": "真实Facility|FamilyManagement|合法产权/控制|管理者Person|正式Primary/Local指定",
            "DevelopmentAdvice": "CanHostFamilyCenter不等于存在Center", "CandidateType": "PRIMARY_OR_LOCAL_RESEARCH_CANDIDATE",
            "ActiveCenter": "NO", "RuntimeDecisionRequired": "YES", "ResolvedPlaceId": resolution["place_id"], "ResolvedCountyId": resolution["county_id"],
        }
    )

a10_conflicts = []
for row in family_v1["luoyang_org_audit"]:
    status = "MIGRATION_REQUIRED" if row["IssueSeverity"] == "S1" else "RESEARCH_REQUIRED"
    a10_conflicts.append(
        {
            "ConflictId": f"family.conflict.{sha12(row['FamilyOrganizationId'])}", "Domain": "Luoyang184RuntimeFamily",
            "RelatedRecordId": row["FamilyOrganizationId"], "ConflictType": row["AuditConclusion"], "Status": status,
            "Description": f"历史成员/Clan/Facility审计：{row['AuditConclusion']}", "HistoricalReferenceConclusion": "Reference不能静默删除运行时组织",
            "RuntimeImpact": "现有27万洛阳包引用保持不变", "RequiredAction": row["SafeFollowup"], "Evidence": "FAMILY_ORGANIZATION_REFERENCE_V1/09审计",
        }
    )
for row in a04_clan_timeline:
    if row["ResolutionStatus"] == "REGION_OR_UNKNOWN_SCOPE":
        a10_conflicts.append(
            {
                "ConflictId": f"family.conflict.scope.{sha12(row['TimelineRecordId'])}", "Domain": "HistoricalFamilySpatial",
                "RelatedRecordId": row["TimelineRecordId"], "ConflictType": "PLACE_RESOLUTION_REQUIRED", "Status": "RESEARCH_REQUIRED",
                "Description": f"{row['ScopeId']}只有区域级或未知空间定位", "HistoricalReferenceConclusion": "保留原Scope，不强填CoreSettlement/County",
                "RuntimeImpact": "不能用于精确Facility或Center初始化", "RequiredAction": "专项地点研究后追加Change Record", "Evidence": row["SourceReference"],
            }
        )


family_counts = {
    "important_clans": len(clans), "important_branches": len(branches),
    "core_with_presence": sum(1 for row in a02_core if row["QueryStatus"] == "REFERENCE_AVAILABLE"),
    "places_with_branch_or_clan": sum(1 for row in a01_places if row["BranchOrClanPresenceCount"] > 0),
    "places_with_residence": sum(1 for row in a01_places if row["ResidenceEvidenceCount"] > 0),
    "places_with_estate": sum(1 for row in a01_places if row["EstateEvidenceCount"] > 0),
    "places_with_assets": sum(1 for row in a01_places if row["FamilyAssetEvidenceCount"] > 0),
    "organization_candidates": len(a08_initialization),
    "primary_candidates": sum(1 for row in a09_centers if "PRIMARY" in row["CandidateType"]),
    "local_candidates": sum(1 for row in a09_centers if "LOCAL" in row["CandidateType"]),
    "scenario_snapshots": len(a06_snapshots), "conflicts": len(a10_conflicts),
}


FAMILY_XLSX = [
    "A01_135-260重要地点家族空间总索引.xlsx", "A02_133核心聚落HistoricalFamilySpatialReference.xlsx",
    "A03_250重点县HistoricalFamilySpatialReference.xlsx", "A04_HistoricalClan_135-260_SpatialTimeline.xlsx",
    "A05_HistoricalBranch_135-260_SpatialTimeline.xlsx", "A06_13Scenario_FamilySpatialSnapshots.xlsx",
    "A07_HistoricalResidence_Estate_AssetReference.xlsx", "A08_FamilyOrganizationInitializationReference_V2.xlsx",
    "A09_FamilyCenterCandidateReference.xlsx", "A10_HistoricalFamilySpatialConflictQueue.xlsx",
]
GOV_XLSX = [
    "PROJECT_DOCUMENT_REGISTRY.xlsx", "PROJECT_CANONICAL_DOMAIN_MAP.xlsx", "DESIGN_DECISION_REGISTRY.xlsx",
    "OPEN_DECISION_REGISTRY.xlsx", "DOCUMENT_CONFLICT_REGISTER.xlsx", "IMPLEMENTATION_GAP_REGISTER.xlsx", "RESEARCH_GAP_REGISTER.xlsx",
]


def governance_header(purpose, authority, covers, does_not_cover, supersedes="无", superseded_by="无", related="GAME_SYSTEMS_MASTER_AND_STATUS.md", status="CURRENT"):
    return f"""## Document Governance

- Purpose：{purpose}
- Authority：{authority}
- Covers：{covers}
- DoesNotCover：{does_not_cover}
- Supersedes：{supersedes}
- SupersededBy：{superseded_by}
- RelatedCanonicalDocs：{related}
- Status：{status}
"""


FAMILY_OUT.mkdir(parents=True, exist_ok=True)
KB_OUT.mkdir(parents=True, exist_ok=True)
REGISTRY_OUT.mkdir(parents=True, exist_ok=True)
MANIFEST_OUT.mkdir(parents=True, exist_ok=True)

write(FAMILY_OUT / "README.md", """# Historical Family Spatial Consolidation V1

""" + governance_header(
    "提供133核心聚落、250重点县、重要Clan/Branch、13 Scenario及家族资产/组织/中心候选的统一查询入口。",
    "L3 Historical / Content Reference",
    "A01—A11家族空间参考及证据等级。",
    "运行时FamilyOrganization、Facility或Active Center状态。",
    related="../../FAMILY_ORGANIZATION_REFERENCE_V1/README.md|../../GAME_SYSTEMS_MASTER_AND_STATUS.md",
    status="HISTORICAL_REFERENCE",
) + """
## Reading order

1. 先读`Docs/FAMILY_ORGANIZATION_REFERENCE_V1/README.md`确认Canonical Family规则。
2. A01按地点反查；A04/A05按Clan/Branch正查；A06按Scenario查询。
3. A07分离Residence、Estate与Asset；A08/A09只是初始化候选；A10保存争议。
4. `ACTIVE_CENTER`永远不能由本资料库决定。

## Query contract

- `GetFamilySpatialReference(placeId, year)`由A01/A02/A03+A06组合回答。
- `GetClanSpatialTimeline(clanId)`由A04回答。
- `GetBranchSpatialTimeline(branchId)`由A05回答。
- UNKNOWN表示资料不足；NONE只在有反证时使用。本轮不以空白覆盖率虚构Presence。
""")


cross_city = defaultdict(set)
for row in a04_clan_timeline:
    if row["PlaceId"] or row["CountyId"] or row["ScopeId"]:
        cross_city[row["ClanId"]].add(row["PlaceId"] or row["CountyId"] or row["ScopeId"])
cross_city_clans = [clan_by_id[cid]["canonical_clan_name"] for cid, scopes in cross_city.items() if len(scopes) > 1]
member_only_places = [row["DisplayName"] for row in a01_places if row["MemberPresenceCount"] > 0 and row["BranchOrClanPresenceCount"] == 0 and row["ResidenceEvidenceCount"] == 0 and row["EstateEvidenceCount"] == 0 and row["FamilyAssetEvidenceCount"] == 0 and row["FamilyOrganizationCandidateCount"] == 0]
weak_places = [row["DisplayName"] for row in a02_core if row["QueryStatus"] == "QUERYABLE_UNKNOWN"]
strong_centers = [row["RelatedName"] for row in a09_centers if row["CenterCandidateLevel"] == "RECONSTRUCTED_CENTER_CANDIDATE"]
no_centers = [row["RelatedName"] for row in a09_centers if row["CandidateType"] == "NO_CENTER_RECOMMENDED"]

a11 = f"""# 全国重要地点家族空间开发参考 V1

{governance_header('总结Family Spatial Consolidation V1覆盖、证据边界、冲突与下一开发输入。','L3 Historical / Content Reference','133核心聚落、250重点县、Clan/Branch Timeline、13 Scenario及候选统计。','运行时FamilyOrganization或FamilyCenter实现。',related='README.md|../../FAMILY_ORGANIZATION_REFERENCE_V1/README.md',status='HISTORICAL_REFERENCE')}
## Family问题验收答复

1. 当前重要Clan：**{family_counts['important_clans']}**。
2. 重要Branch：**{family_counts['important_branches']}**。
3. 133核心聚落中有直接Family Reference的地点：**{family_counts['core_with_presence']}**；其余仍为可查询UNKNOWN。
4. 具有Clan/Branch Presence的地点：**{family_counts['places_with_branch_or_clan']}**。
5. 具有Residence Evidence的地点：**{family_counts['places_with_residence']}**。
6. 具有Estate Evidence的地点：**{family_counts['places_with_estate']}**。
7. 具有Family Asset Evidence的地点：**{family_counts['places_with_assets']}**。
8. FamilyOrganization Candidate：**{family_counts['organization_candidates']}**条Scenario候选，全部Reference Only。
9. PrimaryCenter Candidate：**{family_counts['primary_candidates']}**条候选记录。
10. LocalCenter Candidate：**{family_counts['local_candidates']}**条候选记录。
11. 目前只有Member Presence而无更强证据的地点：{pipe(member_only_places[:20]) or '无'}。
12. 具有跨地点网络的重要Clan：{pipe(cross_city_clans[:20]) or '尚无足够证据'}。
13. 明显迁移/跨地变化以A04的`MEMBER_LOCATION_CHANGE`、`ESTATE_EVIDENCE_CHANGE`和`SCENARIO_ORGANIZATION_CANDIDATE`稀疏记录表达；不逐年复制。
14. 资料最完整Scenario仍是184洛阳；A06共**{family_counts['scenario_snapshots']}**条稀疏快照，其他场景以本籍、人物、Estate和候选证据逐步深化。
15. Family资料不足的核心地点共**{len(weak_places)}**个，示例：{pipe(weak_places[:20])}。
16. 证据较强的Center候选：{pipe(strong_centers[:12]) or '没有可直接物化者'}；“较强”仍不等于Active Center。
17. 明确NO_CENTER建议：{pipe(no_centers[:15]) or '见A09'}。
18. 有争议的FamilyOrganization映射集中在袁氏多政治组织、皇室核心家庭边界、董氏身份与洛阳旧7组织成员映射。
19. 184洛阳运行时冲突包括汉室混入宦官、何氏混入无关人物、两个董氏缺少Canonical Clan锚点，以及7组织均无真实FamilyManagement Facility。
20. 下一阶段宜先落地“皇室特殊家庭边界审计→何氏成员安全迁移→杨/袁Local候选研究”；没有真实Facility前保持CenterStatus=NONE。

## 结论

133核心聚落和250重点县已全部进入同一查询框架，但只有有证据的地点获得Presence；其他保持UNKNOWN。
本资料是Development Input，不是运行时事实。下一动作必须是184洛阳Development Readiness Review，随后才可建立新的运行时整合任务。
"""
write(FAMILY_OUT / "A11_全国重要地点家族空间开发参考_V1.md", a11)


manifest_specs = {
    "LUOYANG_184_DEVELOPMENT_REFERENCE_MANIFEST.md": ("洛阳", "184 / scenario.han.184.yellow_turban", "P0_洛阳_place_han140_sili_henan_luoyang", "Formal 270,000 urban + 400,000 metropolitan packages exist; family V2 migration not started."),
    "CHANGAN_DEVELOPMENT_REFERENCE_MANIFEST.md": ("长安", "Scenario-selected", "P0_长安_place_han140_sili_jingzhao_changan", "Historical reference only; no formal city runtime initialization."),
    "YE_DEVELOPMENT_REFERENCE_MANIFEST.md": ("邺", "Scenario-selected", "P0_邺_place_han140_jizhou_wei_ye", "Historical reference only; no formal city runtime initialization."),
    "XU_DEVELOPMENT_REFERENCE_MANIFEST.md": ("许/许昌", "Scenario-selected", "P0_许昌_place_han140_yuzhou_yingchuan_xu", "Historical reference only; no formal city runtime initialization."),
    "CHENGDU_DEVELOPMENT_REFERENCE_MANIFEST.md": ("成都", "Scenario-selected", "P0_成都_place_han140_yizhou_shu_chengdu", "Historical reference only; no formal city runtime initialization."),
    "XIANGYANG_DEVELOPMENT_REFERENCE_MANIFEST.md": ("襄阳", "Scenario-selected", "P0_襄阳_place_han140_jingzhou_nan_xiangyang", "Historical reference only; no formal city runtime initialization."),
    "JIANGLING_DEVELOPMENT_REFERENCE_MANIFEST.md": ("江陵", "Scenario-selected", "P0_江陵_place_han140_jingzhou_nan_jiangling", "Historical reference only; no formal city runtime initialization."),
    "JIANYE_DEVELOPMENT_REFERENCE_MANIFEST.md": ("建业", "Scenario-selected", "P0_建业_place_han140_yangzhou_danyang_moling", "Historical reference only; no formal city runtime initialization."),
}
for filename, (target, target_year, p0_dir, existing) in manifest_specs.items():
    luoyang_extra = "|Docs/TASK_LUOYANG_184_HISTORICAL_V1.md|Docs/TASK_LUOYANG_184_URBAN_INITIALIZATION_V1.md|Docs/TASK_LUOYANG_184_METROPOLITAN_INITIALIZATION_V1.md" if target == "洛阳" else ""
    manifest = f"""# {target} Development Reference Manifest

{governance_header(f'为{target}后续开发提供唯一资料入口。','L2 Current Development Input Manifest',f'{target}的Canonical、历史、人口、人物、宗族、设施、交通、军事与实现输入。','新的历史结论或运行时实现。',related='../README_PROJECT_KNOWLEDGE_BASE.md',status='CURRENT')}
| Field | Reference |
|---|---|
| TargetPlace | {target} |
| TargetYear / Scenario | {target_year} |
| CanonicalSystemDocs | `Docs/GAME_VISION_AND_GAMEPLAY.md` → `Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01...` → `02...` → `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` |
| HistoricalReferenceDocs | `Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/{p0_dir}/00_Master.md` + `Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/` |
| PopulationDataset | `Assets/StreamingAssets/HistoricalPopulation/Han135260V1/`与`Docs/HISTORICAL_POPULATION_135_260.md` |
| PersonDataset | `Assets/StreamingAssets/HistoricalPersons/Han135260V1/persons.json` |
| ClanDataset | `clans.json`、`branches.json`、`clan_presence.json` |
| FacilityReference | `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md`{luoyang_extra} |
| TransportReference | `Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/10_135-260重点交通节点开发参考.xlsx` |
| MilitaryReference | `Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/11_135-260重要军事空间与战役开发参考.xlsx` |
| ExistingImplementation | {existing} |
| KnownConflicts | 见A10与Knowledge Base的Document Conflict Register；Reference不得冒充实现。 |
| KnownResearchGaps | 精确住宅、Estate边界、族产、Branch迁入、Facility位置和Center证据。 |
| KnownImplementationGaps | FamilyOrganization/FamilyCenter正式运行时、资产权限、通信、存档迁移和UI。 |
| DoNotUseDocs | 旧Task/Report、参考游戏分析和Benchmark不得单独作为当前Canonical Spec。 |
| RecommendedReadingOrder | AGENTS → Game Vision → Domain L1 → Master Status → 本Manifest → P0 Master → Family Spatial → 相关Task/Report。 |
"""
    write(MANIFEST_OUT / filename, manifest)


write(KB_OUT / "DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md", """# Document Authority and Status Specification

""" + governance_header(
    "冻结项目文档Authority、Status、替代关系和冲突处理规则。", "L1 Canonical System Spec / Project Governance",
    "REPO_HARD_RULE、L0—L4、CURRENT/CANONICAL/FROZEN/REFERENCE/ARCHIVED/SUPERSEDED状态。",
    "具体游戏Domain设计与运行时实现。", related="README_PROJECT_KNOWLEDGE_BASE.md|../GAME_SYSTEMS_MASTER_AND_STATUS.md", status="FROZEN",
) + """
## Authority order

```text
User current instruction
→ Repository Hard Rule (AGENTS.md)
→ L0 Project Constitution
→ matching L1 Canonical System Spec
→ L2 Current System Status
→ L3 Historical / Content / Research Reference
→ L4 Task / Implementation / Acceptance History
```

文件日期不决定权威；L4不会因“更新”自动覆盖L1。无法按既有确认设计裁决的冲突必须进入`MANUAL_REVIEW_REQUIRED`。

## Status

- `CURRENT`：当前有效入口；不必然是最高权威。
- `CANONICAL`：对应Domain当前正式规范。
- `FROZEN`：已确认且不应在普通实现任务中改变。
- `IMPLEMENTED_REFERENCE`：实现/验收证据，只证明报告明确覆盖的范围。
- `RESEARCH_REFERENCE`：研究或参考作品分析，不是实现证据。
- `HISTORICAL_REFERENCE`：历史资料或旧工程上下文。
- `ARCHIVED`：保留追溯，不代表当前顺序。
- `SUPERSEDED`：全部规则已由指定文件替代。
- `PARTIALLY_SUPERSEDED`：必须按章节说明继续使用。
- `DRAFT`、`OPEN`：尚未冻结。
- `INVALID / DO_NOT_USE`：仅用于确证错误或危险文件，不因陈旧滥用。

## Core document boundary header

L0/L1/L2文件顶部必须声明Purpose、Authority、Covers、DoesNotCover、Supersedes、SupersededBy、RelatedCanonicalDocs与Status。
旧Task和Report原则上不改写正文；通过Registry保存状态与替代关系。
""")

write(KB_OUT / "CODING_TASK_REFERENCE_PROTOCOL.md", """# Coding Task Reference Protocol

""" + governance_header(
    "规定未来Codex/开发人员开始任务时的最小权威读取顺序。", "L1 Canonical System Spec / Development Protocol",
    "任务开工阅读、Source of Truth声明、冲突升级和交付分类。", "具体Domain设计。",
    related="DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md|README_PROJECT_KNOWLEDGE_BASE.md", status="FROZEN",
) + """
## Required sequence

1. 读取`AGENTS.md`与项目Skill。
2. 读取L0 `GAME_VISION_AND_GAMEPLAY.md`。
3. 在`PROJECT_CANONICAL_DOMAIN_MAP.xlsx`选择任务Domain的L1。
4. 读取L2 `GAME_SYSTEMS_MASTER_AND_STATUS.md`确认实现状态与当前顺序。
5. 读取相关L3历史/内容/研究资料；涉及城市先读对应Development Manifest。
6. 最后读取直接相关L4 Task/Report，不能反向覆盖L1。

## Every new task must declare

- `CANONICAL REFERENCES`
- `CURRENT STATE REFERENCES`
- `HISTORICAL REFERENCES`
- `IMPLEMENTATION HISTORY REFERENCES`

若旧Task与Canonical冲突，停止并记录冲突；若Canonical规则明确但代码未实现，登记Implementation Gap；史料不足登记Research Gap。禁止把三者混在一起。
""")


DOMAIN_ROWS = [
    ("Vision", "Docs/GAME_VISION_AND_GAMEPLAY.md", "", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "", ""),
    ("World", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/WORLD_SIMULATION_FOUNDATION.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_WORLD_REFERENCE/README_历史世界开发参考资料索引.md", ""),
    ("Geography", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/WORLD_SIMULATION_FOUNDATION.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/README_历史世界深化资料索引.md", ""),
    ("Cell", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_MASTER_MAP_V1_LUOYANG_POPULATION_FACILITY_CELL_CAPACITY.md", ""),
    ("Population", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_POPULATION_135_260.md", ""),
    ("Person", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md|Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1.md", ""),
    ("Household", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/WORLD_SIMULATION_FOUNDATION.md|Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_M14_REAL_VILLAGE_AND_HOUSEHOLD_LOOP.md", ""),
    ("Clan", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/README.md", ""),
    ("FamilyOrganization", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/11_135-260家族空间与FamilyCenter开发参考报告_V1.md", ""),
    ("FamilyCenter", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/02_FamilyCenter设计规则_V1.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/A11_全国重要地点家族空间开发参考_V1.md", ""),
    ("Ownership", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/LEGAL_AND_ASSETS.md", ""),
    ("Residence", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md|Docs/WORLD_SIMULATION_FOUNDATION.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/A11_全国重要地点家族空间开发参考_V1.md", ""),
    ("Estate", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/02_FamilyCenter设计规则_V1.md|Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/08_135-260历史豪族与庄园锚点总索引.xlsx", ""),
    ("Facility", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_LUOYANG_184_HISTORICAL_V1.md", ""),
    ("Construction", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md|Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/WORLD_SIMULATION_FOUNDATION.md", ""),
    ("Production", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_M17_P0_DATA_DRIVEN_PRODUCTION_CONTENT_CONTRACT.md", ""),
    ("Agriculture", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_POPULATION_135_260.md", ""),
    ("Inventory", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_M19_P0_PRODUCT_BATCH_INVENTORY_AND_PROCESSING_CHAIN.md", ""),
    ("Market", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/WORLD_SIMULATION_FOUNDATION.md|Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/REFERENCE_JIUZHOU_GAMEPLAY_ANALYSIS.md", "No single consolidated market L1; current rules span World and Production."),
    ("Logistics", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/WORLD_SIMULATION_FOUNDATION.md|Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_M23_P5_MILITARY_LOGISTICS_ACQUISITION_PROVISIONS_AND_LOSS.md", "No single cross-civilian/military logistics L1."),
    ("Office", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "", ""),
    ("Government", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_M22_P0_COUNTY_FISCAL_GENTRY_MARKET_GOVERNANCE.md", ""),
    ("Politics", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/SANDBOX_NPC_AI.md", ""),
    ("Military", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/11_135-260重要军事空间与战役开发参考.xlsx", ""),
    ("Force", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_M10_REAL_MILITARY_SERVICE_AND_COMMAND.md", ""),
    ("AI", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/SANDBOX_NPC_AI.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md", ""),
    ("HistoricalScenario", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/HISTORICAL_SCENARIOS_TIMELINE_AND_FATE_DECISIONS.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_WORLD_REFERENCE/README_历史世界开发参考资料索引.md", ""),
    ("HistoricalPerson", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/DATA_AND_CONTENT_FOUNDATION.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1.md", ""),
    ("HistoricalClan", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md|Docs/DATA_AND_CONTENT_FOUNDATION.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/README.md", ""),
    ("Save", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "", ""),
    ("UI", "Docs/GAME_VISION_AND_GAMEPLAY.md", "", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/TASK_M26_P0_PLAYABLE_DEMO_MAIN_LOOP_INTEGRATION.md", "Missing consolidated L1 UI/interaction specification."),
    ("ArtAssets", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/MAP_ART_RESOURCE_PLAN.md|Docs/LEGAL_AND_ASSETS.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/SERIES_REFERENCE_AUDIT.md", ""),
    ("LegalLicense", "Docs/GAME_VISION_AND_GAMEPLAY.md", "Docs/LEGAL_AND_ASSETS.md", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "Docs/ZHSAN_OPEN_SOURCE_LICENSE_AND_INTEGRATION_AUDIT.md", ""),
]
domain_rows = [
    {"Domain": domain, "L0ProjectConstitution": l0, "L1CanonicalSpec": l1, "L2CurrentStatus": l2, "L3PrimaryReference": l3,
     "CanonicalGap": gap, "MultipleL1Conflict": "NO" if l1 else "N/A", "ReadingEntry": l1 or l0,
     "ConflictPolicy": "Follow explicit preferred L1; unresolved contradictions require MANUAL_REVIEW_REQUIRED"}
    for domain, l0, l1, l2, l3, gap in DOMAIN_ROWS
]


DECISIONS = [
    ("DEC-WORLD-001", "World", "统一世界账", "所有身份、地图视角与系统读写同一世界事实", "FROZEN", "Docs/GAME_VISION_AND_GAMEPLAY.md"),
    ("DEC-MAP-001", "Cell", "稳定方格Cell", "统一世界使用稳定ID方格Cell；不得创建第二套地图身份", "FROZEN", "Docs/WORLD_SIMULATION_FOUNDATION.md"),
    ("DEC-POP-001", "Population", "Permanent Person", "每个人从出生起具有永久身份，不删除、合并、替代或重随机", "FROZEN", "Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md"),
    ("DEC-POP-002", "Population", "人口缩尺", "史料人口只校准分布；实际开局按硬件缩尺并落实到家户/生产/消费/兵源", "FROZEN", "Docs/HISTORICAL_POPULATION_135_260.md"),
    ("DEC-DATA-001", "Content", "开放内容数据驱动", "可扩展内容使用稳定命名空间ID和数据定义，普通新增内容不升级存档结构", "FROZEN", "AGENTS.md"),
    ("DEC-SAVE-001", "Save", "顺序迁移", "持久结构变化必须顺序迁移、往返测试并保持不变量", "FROZEN", "Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md"),
    ("DEC-FAMILY-001", "Clan", "Clan不等于FamilyOrganization", "历史宗族/谱系认同不自动产生组织", "FROZEN", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md"),
    ("DEC-FAMILY-002", "Household", "Household不等于FamilyOrganization", "共同生活单位与族产组织分离", "FROZEN", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md"),
    ("DEC-FAMILY-003", "FamilyCenter", "Primary与Local", "一个FamilyOrganization最多一个Primary，可以多个Local", "FROZEN", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/02_FamilyCenter设计规则_V1.md"),
    ("DEC-FAMILY-004", "FamilyCenter", "真实Facility条件", "中心需要真实Facility、FamilyManagement、合法控制、正式指定与管理者", "FROZEN", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/02_FamilyCenter设计规则_V1.md"),
    ("DEC-FAMILY-005", "FamilyCenter", "无中心不限制个人", "成员可在无中心地点居住、任官、经商、买地、结婚、参军与迁徙", "FROZEN", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/02_FamilyCenter设计规则_V1.md"),
    ("DEC-FAMILY-006", "FamilyCenter", "Presence分离", "Member/Residence/Estate/Asset/Candidate均不等于Active Center", "FROZEN", "Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/README.md"),
    ("DEC-FAMILY-007", "FamilyCenter", "显式管理区", "中心以ManagementAreaId和资产分配确定范围，不用任意半径", "FROZEN", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/02_FamilyCenter设计规则_V1.md"),
    ("DEC-FACILITY-001", "Facility", "能力模型", "Facility由BaseType/Variant/Capability/Operation组成，功能不锁死为单一枚举", "FROZEN", "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md"),
    ("DEC-OWN-001", "Ownership", "产权分账", "Person、Household、FamilyOrganization、Government与Imperial资产分离", "FROZEN", "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md"),
    ("DEC-MIL-001", "Military", "Soldier是Person", "兵员变化必须追溯到真实永久人物", "FROZEN", "Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md"),
    ("DEC-OFFICE-001", "Office", "职位是权限", "Office授予合法职责和控制范围，不是抽象数值Buff", "FROZEN", "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md"),
    ("DEC-SCENARIO-001", "HistoricalScenario", "13正式Scenario", "高优先历史切片为140/184/189/194/200/207/214/219/223/227/234/249/260", "FROZEN", "Docs/HISTORICAL_SCENARIOS_TIMELINE_AND_FATE_DECISIONS.md"),
    ("DEC-HISTORY-001", "HistoricalReference", "证据等级", "Historical/Reconstructed/Modeled/Unknown必须保留，争议不得整理成确定事实", "FROZEN", "Docs/DATA_AND_CONTENT_FOUNDATION.md"),
    ("DEC-KB-001", "ProjectGovernance", "文档权威层级", "Repository Hard Rule→L0→L1→L2→L3→L4", "FROZEN", "Docs/KNOWLEDGE_BASE/DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md"),
    ("DEC-KB-002", "ProjectGovernance", "Task不自动Canonical", "Task/Report保留工程历史但不因较新自动覆盖L1", "FROZEN", "Docs/KNOWLEDGE_BASE/DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md"),
]
decision_rows = [
    {"DecisionId": did, "Domain": domain, "Title": title, "Decision": decision, "Status": status, "EffectiveFrom": TODAY,
     "SourceDocument": source, "SupersedesDecisionId": "", "AffectedDocuments": "See Project Document Registry",
     "AffectedSystems": domain, "ReasonSummary": "用户确认设计、Canonical规范与已实现不变量共同约束", "OpenQuestions": "", "Notes": "不得由L4旧Task覆盖"}
    for did, domain, title, decision, status, source in DECISIONS
]

OPEN_DECISIONS = [
    ("OPEN-FAMILY-001", "FamilyCenter", "184洛阳哪些组织最终具备Primary/Local Center", "需要住宅、资产、Facility、管理者与历史证据", "Blocks Luoyang family runtime V2"),
    ("OPEN-FAMILY-002", "FamilyOrganization", "皇室核心家庭、宫廷组织与全体宗室的运行时边界", "需结合宫廷资产、后妃、宦官和国家机构", "Blocks imperial family materialization"),
    ("OPEN-FAMILY-003", "HistoricalClan", "两个董氏组织的Canonical Clan归属", "现有运行时记录缺少稳定Clan锚点", "Blocks safe migration"),
    ("OPEN-FAMILY-004", "FamilyCenter", "Remote管理的通信延迟与最低权限", "规则已限制为弱监督，数值和消息机制未实现", "Does not block reference"),
    ("OPEN-HISTORY-001", "HistoricalPerson", "205个未解析地点", "人物母库研究队列", "Blocks precise city placement"),
    ("OPEN-HISTORY-002", "HistoricalPerson", "64条未解析亲属关系", "人物母库研究队列", "Blocks exact branch/household reconstruction"),
    ("OPEN-HISTORY-003", "Estate", "8个Estate锚点的精确边界、设施和人口", "现有证据只到Reference", "Blocks estate materialization"),
    ("OPEN-MAP-001", "Geography", "普通核心地点坐标与考古边界深化", "多数R3地点只有研究骨架", "Does not block query framework"),
    ("OPEN-UI-001", "UI", "统一玩家交互与地图层级L1规范", "目前主要存在Demo Task和表现原型", "Blocks mature demo UX"),
    ("OPEN-LOG-001", "Logistics", "民用与军用物流统一L1", "规则分布于World、Combat及M23/M25实施Task", "Blocks large-scale consolidation"),
    ("OPEN-MARKET-001", "Market", "统一市场、商号与城市经营L1", "现有原型与参考分析尚未收口", "Blocks merchant maturity"),
]
open_rows = [
    {"OpenDecisionId": oid, "Domain": domain, "Question": question, "Status": "OPEN", "WhyOpen": reason,
     "NeededEvidence": "Canonical design review + targeted historical/runtime evidence", "OwnerRole": "Future task owner",
     "Blocks": blocks, "SourceDocument": "Docs/KNOWLEDGE_BASE/DOCUMENT_GOVERNANCE_REPORT_V1.md", "RecommendedNextReview": "Development Readiness Review", "Notes": "不得静默补全"}
    for oid, domain, question, reason, blocks in OPEN_DECISIONS
]

CONFLICTS = [
    ("DOC-CONFLICT-001", "FamilyOrganization", "旧Family/branches/members/properties摘要", "Canonical Family关系规范", "粗粒度Family混淆Clan/Household/Organization", "PARTIALLY_RESOLVED"),
    ("DOC-CONFLICT-002", "FamilyCenter", "Organization.headquarters_location_id", "Primary/Local FamilyCenter", "单总部旧字段不能覆盖多中心模型", "PARTIALLY_RESOLVED"),
    ("DOC-CONFLICT-003", "Geography", "早期场景/区域地图抽象", "统一Cell世界", "旧地图可能被误读为第二套世界", "RESOLVED_BY_AUTHORITY"),
    ("DOC-CONFLICT-004", "Population", "旧聚合人口假设", "Permanent Person", "汇总缓存不能替代具体人物", "RESOLVED_BY_AUTHORITY"),
    ("DOC-CONFLICT-005", "Facility", "旧固定设施枚举/建筑Buff", "能力与Operation模型", "早期设施描述不能作为当前内容合同", "RESOLVED_BY_AUTHORITY"),
    ("DOC-CONFLICT-006", "Military", "抽象兵力数字", "Soldier=Person", "兵力变化必须回写永久人物", "RESOLVED_BY_AUTHORITY"),
    ("DOC-CONFLICT-007", "Office", "官职数值加成", "Office=Real Authority", "职位不能凭空授予产权或世界资源", "RESOLVED_BY_AUTHORITY"),
    ("DOC-CONFLICT-008", "HistoricalScenario", "旧剧本/年份入口", "13 Scenario+HistoricalTimePoint", "旧任务切片不可替代当前规范", "RESOLVED_BY_AUTHORITY"),
    ("DOC-CONFLICT-009", "Ownership", "旧家族资产合并描述", "Person/Household/Organization/Government分账", "资产不得按关系自动转移", "RESOLVED_BY_AUTHORITY"),
    ("DOC-CONFLICT-010", "Population", "5000万压力报告扩大解释", "性能证据边界", "基础索引/调度不等于5000万完整NPC AI", "RESOLVED_BY_SCOPE_NOTE"),
    ("DOC-CONFLICT-011", "ReferenceAnalysis", "参考游戏分析", "Canonical System Spec", "玩法启发不能当规则或实现证明", "RESOLVED_BY_CLASSIFICATION"),
    ("DOC-CONFLICT-012", "Luoyang184RuntimeFamily", "现有7组织成员/Clan/Facility", "Family V1审计", "运行时事实与新历史Reference存在迁移冲突", "MIGRATION_REQUIRED"),
]
conflict_rows = [
    {"ConflictId": cid, "Domain": domain, "DocumentA": doca, "DocumentB": docb, "ConflictDescription": desc,
     "CurrentPreferredRule": docb, "AuthorityReason": "用户确认的Canonical规则优先于旧摘要/任务历史",
     "ResolutionStatus": status, "RequiredAction": "Use Registry status and do not rewrite historical implementation evidence",
     "RiskIfIgnored": "未来Codex可能按旧模型新增不兼容代码或历史数据"}
    for cid, domain, doca, docb, desc, status in CONFLICTS
]

IMPLEMENTATION_GAPS = [
    ("IMP-GAP-001", "FamilyOrganization", "Canonical FamilyOrganization资产/成员/职位边界", "洛阳7个粗粒度组织", "正式V2运行时与迁移未实现", "S1", "YES", "LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1"),
    ("IMP-GAP-002", "FamilyCenter", "真实Facility+能力+产权+指定+管理者", "无正式FamilyCenter运行时", "Primary/Local/Disabled/Remote未接Domain/Persistence", "S1", "YES", "Luoyang FamilyCenter runtime slice"),
    ("IMP-GAP-003", "HistoricalFamilySpatial", "按place/year和clan timeline查询", "本轮为文档/表格Reference", "尚无正式运行时Reader/API", "S2", "NO", "Historical family reference reader"),
    ("IMP-GAP-004", "FamilyOrganization", "安全修复洛阳错误成员", "旧包含混组", "需顺序迁移且保留全部Person", "S1", "YES", "Luoyang family V2 migration"),
    ("IMP-GAP-005", "Communication", "中心间非即时通信", "设计已定，系统未实现", "命令、账簿和报告传播缺失", "S2", "NO", "Organization communication slice"),
    ("IMP-GAP-006", "Facility", "全国Facility正式填充", "洛阳/中山原型", "133核心聚落和250县没有完整运行时设施", "S2", "NO", "Region-by-region facility initialization"),
    ("IMP-GAP-007", "Population", "5000万累计永久人物正式能力", "基础压力和百万世界证据", "5000万完整NPC AI负载未证明", "S2", "NO", "Full-system population benchmark"),
    ("IMP-GAP-008", "UI", "可玩城市/建筑/家庭/商旅主循环", "Demo底座和文本界面", "成熟地图与建筑交互未完成", "S1", "YES", "Luoyang playable vertical slice"),
    ("IMP-GAP-009", "HistoricalScenario", "Scenario初始化Family候选审核", "Reference only", "没有按证据物化的初始化器", "S2", "YES", "Scenario family initialization"),
    ("IMP-GAP-010", "Ownership", "Person/Household/Organization资产事务", "部分领域账", "正式家族产权和分立未完成", "S2", "YES", "Family asset ledger"),
    ("IMP-GAP-011", "Market", "成熟商号/分号/产业竞争", "商人切片底座", "完整组织经营仍缺", "S2", "NO", "Merchant organization vertical slice"),
    ("IMP-GAP-012", "Politics", "皇室/王国/政权AI", "设计已定", "完整运行时未实现", "S2", "NO", "Political AI implementation"),
]
implementation_rows = [
    {"GapId": gid, "Domain": domain, "CanonicalRequirement": requirement, "CurrentImplementation": current,
     "GapDescription": desc, "Severity": severity, "BlocksNextDevelopment": blocks, "SuggestedFutureTask": task,
     "Evidence": "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md|Docs/KNOWLEDGE_BASE/PROJECT_CANONICAL_DOMAIN_MAP.xlsx"}
    for gid, domain, requirement, current, desc, severity, blocks, task in IMPLEMENTATION_GAPS
]

RESEARCH_GAPS = [
    ("RES-GAP-001", "HistoricalPerson", "205 unresolved historical place references", "HIGH", "Person master unresolved queue"),
    ("RES-GAP-002", "Kinship", "64 unresolved kinship links", "HIGH", "Person master unresolved queue"),
    ("RES-GAP-003", "HistoricalClan", "Clan/Branch数量不是最终上限", "MEDIUM", "Discover only with reliable sources"),
    ("RES-GAP-004", "Residence", "重要人物城市住宅/宅第证据稀少", "HIGH", "Do not assign precise Facility without evidence"),
    ("RES-GAP-005", "Estate", "8 Estate锚点边界、设施、人口和产权未明", "HIGH", "Estate reference queue"),
    ("RES-GAP-006", "FamilyAsset", "Person/Household/Organization资产所有权证据不足", "HIGH", "Ownership research"),
    ("RES-GAP-007", "FamilyCenter", "184洛阳实际中心候选仍需证据", "HIGH", "Luoyang readiness review"),
    ("RES-GAP-008", "CoreSettlement", f"{len(weak_places)}个核心聚落尚无直接Family Presence证据", "MEDIUM", "Prioritize P0/P1 and Scenario relevance"),
    ("RES-GAP-009", "PriorityCounty", "250重点县资料深度不均", "MEDIUM", "Deepen only high-value locations"),
    ("RES-GAP-010", "BranchMigration", "15 Branch分出年和迁徙轨迹多为保守基线", "HIGH", "Sparse change research"),
    ("RES-GAP-011", "Luoyang184", "汉室/何氏/董氏映射冲突", "HIGH", "RuntimeMigrationRequired"),
    ("RES-GAP-012", "P0Cities", "长安、邺、许、成都、襄阳、江陵、建业尚未达到洛阳R5 Family精度", "MEDIUM", "Use manifests; do not restart discovery"),
]
research_rows = [
    {"GapId": gid, "Domain": domain, "ResearchGap": gap, "Priority": priority, "CurrentEvidence": evidence,
     "RequiredSources": "Primary historical text|academic/archaeological research|existing stable datasets",
     "DoNotInfer": "Do not fabricate people, estates, assets, organizations, centers or precise Cells",
     "SuggestedResearchAction": "Append evidence/change records without renumbering stable IDs"}
    for gid, domain, gap, priority, evidence in RESEARCH_GAPS
]


NEW_EXPECTED = [
    *(FAMILY_OUT / name for name in FAMILY_XLSX), FAMILY_OUT / "README.md", FAMILY_OUT / "A11_全国重要地点家族空间开发参考_V1.md",
    *(REGISTRY_OUT / name for name in GOV_XLSX),
    KB_OUT / "README_PROJECT_KNOWLEDGE_BASE.md", KB_OUT / "CODING_TASK_REFERENCE_PROTOCOL.md",
    KB_OUT / "DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md", KB_OUT / "DOCUMENT_GOVERNANCE_REPORT_V1.md",
    *(MANIFEST_OUT / name for name in manifest_specs),
]


CORE_AUTHORITY = {
    "Docs/GAME_VISION_AND_GAMEPLAY.md": ("L0", "CANONICAL", "Vision|UnifiedWorld|Identity|Dynasty"),
    "Docs/WORLD_SIMULATION_FOUNDATION.md": ("L1", "CANONICAL", "World|Geography|Population|Economy"),
    "Docs/DATA_AND_CONTENT_FOUNDATION.md": ("L1", "PARTIALLY_SUPERSEDED", "Content|HistoricalData|StableId"),
    "Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md": ("L1", "CANONICAL", "Save|Determinism|Migration"),
    "Docs/SANDBOX_NPC_AI.md": ("L1", "CANONICAL", "AI|Scheduling|Knowledge"),
    "Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md": ("L1", "CANONICAL", "Production|Agriculture|Industry|Research"),
    "Docs/CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md": ("L1", "CANONICAL", "Person|Attributes|Skills|FamilyGrowth"),
    "Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md": ("L1", "CANONICAL", "Military|Combat|Force|Authority"),
    "Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md": ("L1", "CANONICAL", "Cell|Facility|Ownership|Office|Politics"),
    "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md": ("L1", "FROZEN", "Clan|Branch|Household|FamilyOrganization"),
    "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/02_FamilyCenter设计规则_V1.md": ("L1", "FROZEN", "FamilyCenter"),
    "Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md": ("L1", "FROZEN", "PermanentPerson|Population|Attention"),
    "Docs/HISTORICAL_SCENARIOS_TIMELINE_AND_FATE_DECISIONS.md": ("L1", "CANONICAL", "HistoricalScenario|HistoricalTimePoint|FateDecision"),
    "Docs/LEGAL_AND_ASSETS.md": ("L1", "CANONICAL", "Legal|License|ExternalAssets"),
    "Docs/MAP_ART_RESOURCE_PLAN.md": ("L1", "CURRENT", "ArtAssets|MapArt"),
    "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md": ("L2", "CURRENT", "CurrentStatus|BuildOrder"),
    "Docs/KNOWLEDGE_BASE/DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md": ("L1", "FROZEN", "ProjectGovernance"),
    "Docs/KNOWLEDGE_BASE/CODING_TASK_REFERENCE_PROTOCOL.md": ("L1", "FROZEN", "DevelopmentProtocol"),
    "Docs/KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md": ("L2", "CURRENT", "KnowledgeBaseIndex"),
}


def infer_title(path: Path):
    if path.suffix.lower() == ".md" and path.exists():
        try:
            for line in path.read_text(encoding="utf-8").splitlines()[:80]:
                if line.startswith("# "):
                    return line[2:].strip()
        except UnicodeDecodeError:
            pass
    return path.stem


def infer_domain(path_text, title):
    text = (path_text + " " + title).upper()
    rules = [
        ("FamilyCenter", ["FAMILYCENTER", "FAMILY_CENTER"]), ("FamilyOrganization", ["FAMILYORGANIZATION", "FAMILY_ORGANIZATION"]),
        ("HistoricalClan", ["CLAN", "宗族", "家族空间"]), ("Population", ["POPULATION", "人口"]),
        ("HistoricalScenario", ["SCENARIO", "TIMELINE", "FATE", "剧本", "时间轴"]),
        ("Facility", ["FACILITY", "设施"]), ("Military", ["COMBAT", "WAR", "MILITARY", "ARMY", "军", "战斗"]),
        ("Production", ["PRODUCTION", "AGRICULTURE", "CROP", "INVENTORY", "FOOD", "生产", "农业", "粮"]),
        ("Save", ["SAVE", "PERSIST", "DETERMIN", "存档"]), ("AI", [" AI", "SANDBOX", "DELEGATION", "NPC"]),
        ("Geography", ["MAP", "CITY", "COUNTY", "GEOGRAPH", "LUOYANG", "地图", "城市", "洛阳"]),
        ("Person", ["CHARACTER", "PERSON", "ATTRIBUTE", "人物"]), ("LegalLicense", ["LEGAL", "LICENSE", "ASSET"]),
        ("ProjectGovernance", ["KNOWLEDGE_BASE", "PROJECT_", "DEVELOPMENT_PLAN", "README"]),
    ]
    for domain, terms in rules:
        if any(term in text for term in terms):
            return domain
    return "CrossSystem"


def classify_document(path: Path):
    p = posix(path)
    title = infer_title(path)
    if p == "AGENTS.md":
        return title, "ProjectGovernance", "RepositoryHardRule", "REPO_HARD_RULE", "FROZEN", "Repository execution and safety"
    if p in CORE_AUTHORITY:
        level, status, canonical = CORE_AUTHORITY[p]
        dtype = "ProjectConstitution" if level == "L0" else ("CurrentStatus" if level == "L2" else "CanonicalSystemSpec")
        return title, infer_domain(p, title), dtype, level, status, canonical
    name = path.name.upper()
    domain = infer_domain(p, title)
    if p.startswith("Docs/KNOWLEDGE_BASE/DEVELOPMENT_MANIFESTS/"):
        return title, "DevelopmentInput", "DevelopmentManifest", "L2", "CURRENT", "PlaceDevelopmentInput"
    if p.startswith("Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/"):
        return title, "HistoricalFamilySpatial", "HistoricalReference", "L3", "HISTORICAL_REFERENCE", "FamilySpatialReference"
    if p.startswith("Docs/KNOWLEDGE_BASE/REGISTRY/"):
        return title, "ProjectGovernance", "Registry", "L2", "CURRENT", "KnowledgeBaseRegistry"
    if "REFERENCE_" in name or "REFERENCE ANALYSIS" in title.upper() or "参考分析" in title:
        return title, domain, "ReferenceAnalysis", "L3", "RESEARCH_REFERENCE", ""
    if name.startswith("TASK_") or "/TASK_" in p.upper():
        status = "IMPLEMENTED_REFERENCE" if "HAN_PREDEVELOPMENT" in name else "HISTORICAL_REFERENCE"
        return title, domain, "Task", "L4", status, ""
    if "REPORT" in name or "AUDIT" in name or "验收" in title or "报告" in title:
        return title, domain, "ImplementationOrAcceptanceReport", "L4", "IMPLEMENTED_REFERENCE", ""
    if "DEVELOPMENT_PLAN" in name or "PREPRODUCTION_BACKLOG" in name:
        return title, "ProjectGovernance", "HistoricalPlan", "L4", "ARCHIVED", ""
    if "HISTORICAL" in name or "历史" in title or "HISTORICAL_WORLD_REFERENCE" in p:
        return title, domain, "HistoricalReference", "L3", "HISTORICAL_REFERENCE", ""
    if path.suffix.lower() in (".xlsx", ".docx"):
        return title, domain, "StructuredReference", "L3", "RESEARCH_REFERENCE", ""
    if name.startswith("README") or path.name == "README.md":
        return title, domain, "IndexOrOrientation", "L3", "CURRENT", ""
    return title, domain, "DesignOrReference", "L3", "CURRENT", ""


source_paths = []
for path in DOCS.rglob("*"):
    if path.is_file() and path.suffix.lower() in (".md", ".xlsx", ".docx") and not path.name.endswith(".inspect.ndjson"):
        source_paths.append(path)
for path in (REPO / "outputs").rglob("*.md"):
    if "tmp" not in path.parts:
        source_paths.append(path)
source_paths.extend([REPO / "README.md", REPO / "AGENTS.md"])
source_paths.extend(NEW_EXPECTED)
unique_paths = {str(path.resolve()).lower(): path for path in source_paths}
source_paths = sorted(unique_paths.values(), key=lambda path: posix(path).lower())

superseded_map = {
    "Docs/DEVELOPMENT_PLAN.md": ("", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "全部开发顺序"),
    "Docs/PREPRODUCTION_BACKLOG.md": ("", "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "全部当前优先级"),
    "Docs/DATA_AND_CONTENT_FOUNDATION.md": ("", "Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md", "旧Family/branches/members/properties摘要"),
}

document_rows = []
for path in source_paths:
    p = posix(path)
    title, domain, dtype, level, status, canonical = classify_document(path)
    supersedes, superseded_by, partial = superseded_map.get(p, ("", "", ""))
    if path in NEW_EXPECTED:
        known_date, revision = TODAY, "GENERATED_V1_2026-08-11"
    elif path.exists():
        stat = path.stat()
        known_date = datetime.fromtimestamp(stat.st_mtime).date().isoformat()
        revision = hashlib.sha256(path.read_bytes()).hexdigest()[:16]
    else:
        known_date, revision = TODAY, "PENDING_GENERATION"
    action = ""
    if status == "ARCHIVED":
        action = "Retain for history; do not use for current development order"
    elif status == "PARTIALLY_SUPERSEDED":
        action = "Read PartiallySupersededSections before use"
    elif dtype == "Task":
        action = "Read matching L1/L2 first; task is historical execution context"
    elif dtype == "ReferenceAnalysis":
        action = "Use only for design inspiration; never implementation evidence"
    document_rows.append(
        {
            "DocumentId": f"doc.{sha12(p.lower())}", "Path": p, "Title": title, "Domain": domain, "SubDomain": "",
            "DocumentType": dtype, "AuthorityLevel": level, "Status": status, "CreatedOrKnownDate": known_date,
            "LastKnownRevision": revision, "CanonicalFor": canonical, "Supersedes": supersedes, "SupersededBy": superseded_by,
            "PartiallySupersededSections": partial, "RelatedDocuments": "Docs/KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md",
            "RelatedTasks": "", "RelatedRuntimeSystems": domain, "HistoricalValue": "HIGH" if level == "L4" else "NORMAL",
            "RecommendedReader": "Codex|Developer|Designer", "ReadPriority": {"REPO_HARD_RULE": 0, "L0": 1, "L1": 2, "L2": 3, "L3": 4, "L4": 5}.get(level, 6),
            "ConflictNotes": "", "ActionRequired": action,
        }
    )


def scan_markdown_links(paths):
    link_re = re.compile(r"\[[^\]]*\]\(([^)]+)\)")
    broken = []
    for path in paths:
        if path.suffix.lower() != ".md" or not path.exists():
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError as exc:
            broken.append({"Path": posix(path), "Link": "", "Reason": f"ENCODING_ERROR:{exc}"})
            continue
        for match in link_re.finditer(text):
            raw = match.group(1).strip().strip("<>")
            if not raw or raw.startswith(("http://", "https://", "mailto:", "#", "app://", "file://")):
                continue
            target_text = unquote(raw.split("#", 1)[0])
            target_text = re.sub(r":\d+$", "", target_text)
            target = Path(target_text)
            if not target.is_absolute():
                target = (path.parent / target).resolve()
            if not target.exists():
                broken.append({"Path": posix(path), "Link": raw, "Reason": "TARGET_NOT_FOUND"})
    return broken


link_audit = scan_markdown_links(source_paths)
canonical_paths = {row["Path"] for row in document_rows if row["AuthorityLevel"] in ("L0", "L1", "L2", "REPO_HARD_RULE")}
canonical_broken = [row for row in link_audit if row["Path"] in canonical_paths]
if link_audit:
    conflict_rows.append(
        {"ConflictId": "DOC-CONFLICT-LINKS", "Domain": "ProjectGovernance", "DocumentA": "Repository markdown links", "DocumentB": "Project Document Registry",
         "ConflictDescription": f"扫描发现{len(link_audit)}条现有内部链接/编码问题，其中核心L0-L2为{len(canonical_broken)}条",
         "CurrentPreferredRule": "新Knowledge Base与核心Canonical链接必须可解析；历史L3/L4问题进入队列", "AuthorityReason": "Document Governance validation",
         "ResolutionStatus": "MANUAL_REVIEW_REQUIRED" if link_audit else "PASS", "RequiredAction": "按link_audit.json分批修复，不批量移动旧文件",
         "RiskIfIgnored": "未来Codex可能无法追溯引用"}
    )
for row in document_rows:
    count = sum(1 for issue in link_audit if issue["Path"] == row["Path"])
    if count:
        row["ConflictNotes"] = f"BROKEN_LINK_COUNT={count}"
        if not row["ActionRequired"]:
            row["ActionRequired"] = "Review broken links in link_audit.json"


authority_counts = Counter(row["AuthorityLevel"] for row in document_rows)
status_counts = Counter(row["Status"] for row in document_rows)
missing_domains = [row["Domain"] for row in domain_rows if row["CanonicalGap"]]
high_risk_docs = [row["Path"] for row in document_rows if row["Status"] in ("ARCHIVED", "PARTIALLY_SUPERSEDED")]

write(KB_OUT / "README_PROJECT_KNOWLEDGE_BASE.md", """# Project Knowledge Base

""" + governance_header(
    "作为Codex和开发人员寻找项目Source of Truth的第一入口。", "L2 Current System Status / Knowledge Index",
    "Canonical Domain Map、文档Registry、决策/开放问题、冲突、实现/研究缺口和城市Manifest。",
    "替代各Domain的L1正文。", related="DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md|../GAME_SYSTEMS_MASTER_AND_STATUS.md", status="CURRENT",
) + """
## Start here

1. Repository规则：`../../AGENTS.md`与项目Skill。
2. 项目愿景：`../GAME_VISION_AND_GAMEPLAY.md`。
3. 文档权威：`DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md`。
4. Domain入口：`REGISTRY/PROJECT_CANONICAL_DOMAIN_MAP.xlsx`。
5. 当前完成度：`../GAME_SYSTEMS_MASTER_AND_STATUS.md`。
6. 历史/内容资料：`../HISTORICAL_WORLD_REFERENCE/README_历史世界开发参考资料索引.md`。
7. 旧Task/Report：只通过`REGISTRY/PROJECT_DOCUMENT_REGISTRY.xlsx`检索，不直接当Canonical。

## Fast routes

| Question | Read |
|---|---|
| 游戏愿景 | `../GAME_VISION_AND_GAMEPLAY.md` |
| 世界、地图、人口、经济 | `../WORLD_SIMULATION_FOUNDATION.md` + Master Status |
| 人物、能力、成长 | `../CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md` |
| Family | `../FAMILY_ORGANIZATION_REFERENCE_V1/README.md` → Family Spatial Consolidation |
| Facility、产权、职位、政治 | `../UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` |
| 生产、建设、农业、科研 | `../PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md` |
| 军事与战争 | `../UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md` |
| Scenario | `../HISTORICAL_SCENARIOS_TIMELINE_AND_FATE_DECISIONS.md` |
| 存档和确定性 | `../DETERMINISTIC_SIMULATION_AND_SAVE.md` |
| 洛阳或其他P0开发 | `DEVELOPMENT_MANIFESTS/` |

## End of consolidation

本知识库完成后暂停扩大资料治理。下一动作是`DEVELOPMENT READINESS REVIEW`，目标为184洛阳；通过后进入`LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1`。
""")

governance_report = f"""# Document Governance Report V1

{governance_header('报告全项目文档登记、Authority/Status、冲突、缺口与后续读取方式。','L2 Current System Status / Governance Report','本轮扫描与分类结果。','具体Domain规则或运行时完成证明。',related='README_PROJECT_KNOWLEDGE_BASE.md|REGISTRY/PROJECT_DOCUMENT_REGISTRY.xlsx',status='CURRENT')}
## 文档治理问题验收答复

1. 登记长期文档/表格：**{len(document_rows)}**。
2. L0：**{authority_counts['L0']}**。
3. L1：**{authority_counts['L1']}**。
4. L2：**{authority_counts['L2']}**。
5. L3：**{authority_counts['L3']}**。
6. L4：**{authority_counts['L4']}**；另有REPO_HARD_RULE **{authority_counts['REPO_HARD_RULE']}**。
7. CURRENT：**{status_counts['CURRENT']}**。
8. ARCHIVED：**{status_counts['ARCHIVED']}**。
9. SUPERSEDED：**{status_counts['SUPERSEDED']}**。
10. PARTIALLY_SUPERSEDED：**{status_counts['PARTIALLY_SUPERSEDED']}**。
11. 缺少单一Canonical Spec的Domain：{pipe(missing_domains) or '无'}。
12. 未裁决的多个L1冲突：0；旧规则冲突通过Preferred L1与Conflict Register表达，无法裁决者保留MANUAL_REVIEW_REQUIRED。
13. 最易误导的旧文档：{pipe(high_risk_docs[:12]) or '见Registry'}，以及所有被脱离L1/L2上下文单独读取的旧Task。
14. 只需Header/Registry即可继续保留的主要是早期Task、Report、Benchmark和Reference Analysis；不重写历史正文。
15. 最小修订对象：Game Vision、Master Status、Data Foundation、World/Character/Production/Combat/AI/Save/Facility/Family等核心入口的职责边界与Cross Reference。
16. Document Conflict：**{len(conflict_rows)}**。
17. Implementation Gap：**{len(implementation_rows)}**。
18. Research Gap：**{len(research_rows)}**。
19. Family读取顺序：Game Vision → Family关系规范 → FamilyCenter规则 → Master Status → Family Spatial → 相关L4。
20. 洛阳读取顺序：Repository规则 → Domain L1 → Master → `LUOYANG_184_DEVELOPMENT_REFERENCE_MANIFEST`。
21. 其他城市：选择对应P0 Manifest，再读P0 Master、Family Spatial和相关Scenario，不重新搜旧Task拼规则。
22. 可以从`README_PROJECT_KNOWLEDGE_BASE.md`找到主要Source of Truth；Document Registry提供完整路径、状态和权威等级。

## 审计边界

扫描发现**{len(link_audit)}**条既有Markdown内部链接/编码问题，其中核心L0/L1/L2为**{len(canonical_broken)}**条。新Knowledge Base链接必须在验收时为零错误；历史L3/L4问题保留在`link_audit.json`，不得靠批量移动文件掩盖。

本轮为Documentation / Reference Only：没有修改Unity运行时代码、Scene、Prefab、Save Schema或Domain Model。
"""
write(KB_OUT / "DOCUMENT_GOVERNANCE_REPORT_V1.md", governance_report)


workdata = {
    "a01_important_places": a01_places,
    "a02_core_settlements": a02_core,
    "a03_priority_counties": a03_counties,
    "a04_clan_timeline": a04_clan_timeline,
    "a05_branch_timeline": a05_branch_timeline,
    "a06_scenario_snapshots": a06_snapshots,
    "a07_residence_estate_assets": a07_assets,
    "a08_initialization_v2": a08_initialization,
    "a09_center_candidates": a09_centers,
    "a10_family_conflicts": a10_conflicts,
    "b01_document_registry": document_rows,
    "b02_domain_map": domain_rows,
    "b03_design_decisions": decision_rows,
    "b04_open_decisions": open_rows,
    "b05_document_conflicts": conflict_rows,
    "b06_implementation_gaps": implementation_rows,
    "b07_research_gaps": research_rows,
}
OUT.mkdir(parents=True, exist_ok=True)
(OUT / "knowledge_base_workdata.json").write_text(json.dumps(workdata, ensure_ascii=False, indent=2), encoding="utf-8")
(OUT / "link_audit.json").write_text(json.dumps(link_audit, ensure_ascii=False, indent=2), encoding="utf-8")
(OUT / "generation_summary.json").write_text(json.dumps({
    "family_counts": family_counts,
    "document_count": len(document_rows),
    "authority_counts": authority_counts,
    "status_counts": status_counts,
    "document_conflicts": len(conflict_rows),
    "implementation_gaps": len(implementation_rows),
    "research_gaps": len(research_rows),
    "broken_links": len(link_audit),
    "canonical_broken_links": len(canonical_broken),
}, ensure_ascii=False, indent=2), encoding="utf-8")

print(json.dumps({key: len(value) for key, value in workdata.items()}, ensure_ascii=False, indent=2))
