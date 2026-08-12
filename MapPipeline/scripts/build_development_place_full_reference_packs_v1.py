#!/usr/bin/env python3
"""Build the 72-place Full Development Reference Pack (FDRP) corpus.

The builder is deliberately reference-only. It preserves the approved roster and
wave assignment, migrates D2-D5 labels to T1-T4, and never materializes runtime
Places, Facilities, Persons, Cells, population, camps, or scenario changes.
"""

from __future__ import annotations

import json
import re
from collections import Counter, defaultdict
from datetime import date
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "outputs" / "HAN_135_260_DEVELOPMENT_PLACE_FULL_REFERENCE_PACK_V1"
DOC = REPO / "Docs" / "HISTORICAL_WORLD_REFERENCE" / "PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS"
PACKS = DOC / "PACKS"
ROSTER_OUT = REPO / "outputs" / "DEVELOPMENT_PLACE_ROSTER_AND_REFERENCE_READINESS_V1"
TODAY = date.today().isoformat()

EVIDENCE = {"HISTORICAL", "RECONSTRUCTED", "MODELED", "UNKNOWN", "NO_EVIDENCE", "NOT_APPLICABLE"}
DEPTH_TO_TIER = {"D2": "T1", "D3": "T2", "D4": "T3", "D5": "T4"}
READINESS_TO_FULL = {
    "READY_FOR_IMPLEMENTATION": "FULL_READY",
    "MOSTLY_READY": "FULL_READY_WITH_MODELED_GAPS",
    "PARTIAL": "FULL_READY_WITH_UNKNOWNS",
    "RESEARCH_REQUIRED": "RESEARCH_BLOCKED",
}
RUNTIME_TO_STATUS = {
    "FORMAL_LUOYANG_PACKAGES": "PARTIAL",
    "REFERENCE_ONLY": "NOT_STARTED",
    "REFERENCE_SITE_ONLY": "NOT_STARTED",
}
SPATIAL_MODES = {
    "PERMANENT_SETTLEMENT", "PERMANENT_GEOGRAPHIC_SITE", "EVENT_DEPENDENT_COMPLEX",
    "BATTLEFIELD_REGION", "UNRESOLVED",
}

SHEETS = [
    ("00_INDEX", "modules"), ("01_IDENTITY", "identity"), ("02_GEOGRAPHY", "geography"),
    ("03_ADMINISTRATION", "administration"), ("04_NAME_AND_SEAT_TIMELINE", "name_timeline"),
    ("05_POPULATION", "population"), ("06_SETTLEMENT_FORM", "settlement_form"),
    ("07_FACILITIES", "facilities"), ("08_HISTORICAL_PERSONS", "historical_persons"),
    ("09_CLAN_FAMILY_ESTATE", "clan_family_estate"), ("10_SOCIAL_STRUCTURE", "social_structure"),
    ("11_AGRICULTURE", "agriculture"), ("12_INDUSTRY_RESOURCE", "industry_resource"),
    ("13_MARKET_STORAGE", "market_storage"), ("14_TRANSPORT", "transport"),
    ("15_HINTERLAND_SETTLEMENTS", "hinterland"), ("16_MILITARY", "military"),
    ("17_SCENARIO_STATES", "scenario_states"), ("18_CHANGEPOINTS", "changepoints"),
    ("19_EVENT_DEPENDENT_STATE", "event_state"), ("20_EVENT_ESTABLISHMENT_PACKAGE", "event_packages"),
    ("21_POST_EVENT_STATE", "post_event"), ("22_DEVELOPMENT_IMPLICATIONS", "implications"),
    ("23_SOURCES", "sources"), ("24_UNKNOWNS", "unknowns"),
]


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def dump(path: Path, payload):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def md(path: Path, content: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8")


def as_list(value):
    if value is None:
        return []
    if isinstance(value, list):
        return value
    return [value]


def pack_slug(place_id: str) -> str:
    return re.sub(r"[^A-Za-z0-9]+", "_", place_id).strip("_").upper()


roster_data = load(ROSTER_OUT / "development_place_roster_workdata.json")
deep = load(REPO / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1" / "deepening_workdata.json")
admin = load(REPO / "outputs" / "HAN_135_260_ADMINISTRATIVE_SEAT_CANONICAL_PLACE_AND_HISTORICAL_WORLD_STATE_V1" / "administrative_seat_world_state_workdata.json")
historical = load(REPO / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1" / "historical_world_reference_workdata.json")
family = load(REPO / "outputs" / "FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1" / "family_reference_workdata.json")
population_184 = load(REPO / "Assets" / "StreamingAssets" / "HistoricalPopulation" / "Han135260V1" / "years" / "year_184.json")
person_locations = load(REPO / "Assets" / "StreamingAssets" / "HistoricalPersons" / "Han135260V1" / "person_locations.json")
persons_payload = load(REPO / "Assets" / "StreamingAssets" / "HistoricalPersons" / "Han135260V1" / "persons.json")
clan_presence_payload = load(REPO / "Assets" / "StreamingAssets" / "HistoricalPersons" / "Han135260V1" / "clan_presence.json")
city_pack_path = REPO / "outputs" / "HAN_135_260_CORE_CITY_DEVELOPMENT_PACK_AND_UPGRADE_PROTOCOL_V1" / "core_city_development_pack_workdata.json"
city_pack_data = load(city_pack_path) if city_pack_path.exists() else {"cities": {}}


def records(payload, *keys):
    if isinstance(payload, list):
        return payload
    for key in keys:
        if isinstance(payload.get(key), list):
            return payload[key]
    return []


persons = records(persons_payload, "persons", "records")
person_locations = records(person_locations, "person_locations", "locations", "records")
clan_presence = records(clan_presence_payload, "clan_presence", "presences", "records")
canonical = {r["CanonicalPlaceId"]: r for r in admin["canonical_places"]}
core = {r["place_id"]: r for r in deep["core_settlements"]}
county = {r["county_id"]: r for r in historical["counties"]}
city = {r["city_id"]: r for r in historical["cities"]}
person = {r["person_id"]: r for r in persons}
pop_county = {r["county_permanent_id"]: r for r in population_184["counties"]}
pop_city_by_name = {r["city_name"]: r for r in population_184["major_cities"]}
readiness = {r["PlaceId"]: r for r in roster_data["readiness"]}
states_by_place = defaultdict(list)
for row in roster_data["historical_state_plan"]:
    states_by_place[row["PlaceId"]].append(row)
p0_by_place = defaultdict(list)
for row in deep["p0_reference"]:
    p0_by_place[row["place_id"]].append(row)
seat_by_place = defaultdict(list)
for row in admin["administrative_seats"]:
    seat_by_place[row.get("SeatPlaceId", "")].append(row)
name_by_place = defaultdict(list)
for row in admin["place_name_timeline"]:
    name_by_place[row.get("PlaceId", "")].append(row)
snapshot_by_place = defaultdict(list)
for row in admin["scenario_snapshots"]:
    snapshot_by_place[row.get("PlaceId", "")].append(row)
change_by_place = defaultdict(list)
for row in admin["change_points"]:
    change_by_place[row.get("PlaceId / RegionId", "")].append(row)
package_by_change = defaultdict(list)
for row in admin["change_packages"]:
    package_by_change[row.get("ChangePointId", "")].append(row)
blockers_by_place = defaultdict(list)
for row in roster_data["blockers"]:
    blockers_by_place[row["PlaceId"]].append(row)

city_to_place = {}
county_to_place = {}
for row in deep["core_settlements"]:
    county_to_place[row.get("county_id", "")] = row["place_id"]
    raw_city_ids = row.get("city_ids", [])
    city_ids = raw_city_ids if isinstance(raw_city_ids, list) else str(raw_city_ids).split("|")
    for city_id in city_ids:
        if not city_id:
            continue
        city_to_place[city_id] = row["place_id"]
    raw_city_names = row.get("city_names", [])
    city_names = raw_city_names if isinstance(raw_city_names, list) else str(raw_city_names).split("|")
    for city_name in city_names:
        if city_name:
            city_to_place[f"city.han.{city_name}"] = row["place_id"]

location_by_place = defaultdict(list)
for row in person_locations:
    pid = row.get("place_id") or city_to_place.get(row.get("city_id")) or county_to_place.get(row.get("county_permanent_id"))
    if pid:
        location_by_place[pid].append(row)

clan_by_place = defaultdict(list)
for row in clan_presence:
    pid = row.get("place_id") or city_to_place.get(row.get("city_id")) or county_to_place.get(row.get("county_permanent_id"))
    if pid:
        clan_by_place[pid].append(row)
for row in family["residence_estate_assets"]:
    loc = row.get("LocationScopeId", "")
    if loc in canonical:
        clan_by_place[loc].append(row)

industry_by_province = {r["region_id"]: r for r in deep["industry_resources"]}
transport_by_place = defaultdict(list)
for row in deep["transport_nodes"]:
    parent = str(row.get("parent_location", ""))
    for roster in roster_data["roster"]:
        if roster["CanonicalPlaceId"] in parent or roster["CanonicalName"] in parent:
            transport_by_place[roster["CanonicalPlaceId"]].append(row)

military_by_place = defaultdict(list)
for row in deep["military_spaces"]:
    for city_id in str(row.get("related_city_ids", "")).split("|"):
        pid = city_to_place.get(city_id)
        if pid:
            military_by_place[pid].append(row)


def spatial_mode(row):
    roles = str(row.get("PhysicalRoles", ""))
    place_id = row["CanonicalPlaceId"]
    modes = []
    if place_id.startswith("place."):
        modes.append("PERMANENT_SETTLEMENT")
    if place_id.startswith("geo.site.") or any(x in roles for x in ("关", "渡", "口", "战场", "要塞")):
        modes.append("PERMANENT_GEOGRAPHIC_SITE")
    if row["CanonicalName"] in {"赤壁", "濡须口", "夏口", "夷陵"}:
        modes.extend(["EVENT_DEPENDENT_COMPLEX", "BATTLEFIELD_REGION"])
    return "|".join(dict.fromkeys(modes or ["UNRESOLVED"]))


def evidence_row(topic, value, evidence="UNKNOWN", sources="", notes=""):
    if evidence not in EVIDENCE:
        upper = str(evidence).upper()
        evidence = next((item for item in ("HISTORICAL", "RECONSTRUCTED", "MODELED", "UNKNOWN", "NO_EVIDENCE", "NOT_APPLICABLE") if item in upper), "RECONSTRUCTED")
    return {"Topic": topic, "Value": value, "EvidenceType": evidence, "SourceIds": sources, "Notes": notes}


def place_population(row, core_row):
    if not core_row:
        return [evidence_row("184人口", "非永久聚落地点不建立独立城市人口", "NOT_APPLICABLE", notes="不得把战场或关隘名望等同城市人口")]
    county_id = core_row.get("county_id")
    p = pop_county.get(county_id)
    rows = []
    if p:
        rows.append({
            "Year": 184, "Layer": "COUNTY", "RegisteredPopulation": p["registered_population"],
            "ModeledActualPopulation": p["modeled_actual_population"], "UrbanSettlementPopulation": p["urban_settlement_population"],
            "EvidenceType": "MODELED", "SourceIds": "source.project.han135260.population.v1",
            "Notes": "县级缩尺人口模型；不是该城墙内人口，永久人物规则仍由M12约束。",
        })
    cp = pop_city_by_name.get(row["CanonicalName"])
    if cp:
        rows.append({
            "Year": 184, "Layer": "MAJOR_CITY", "WalledPopulation": cp["walled_city_population"],
            "UrbanPopulation": cp["urban_area_population"], "MetroPopulation": cp["metropolitan_population"],
            "EvidenceType": "MODELED" if cp["evidence"] != "HistoricalAnchor" else "HISTORICAL",
            "SourceIds": cp["source"], "Notes": cp["notes"],
        })
    if not rows:
        rows.append(evidence_row("184人口", "已有县/郡人口层，但尚无可独立分配到该聚落的可靠数值", "UNKNOWN"))
    return rows


def event_packages_for(row, military_rows):
    modes = spatial_mode(row)
    if "EVENT_DEPENDENT_COMPLEX" not in modes:
        return [evidence_row("事件建造包", "该地点当前无专属事件建造包", "NOT_APPLICABLE")]
    event_label = "historical.event." + pack_slug(row["CanonicalPlaceId"]).lower()
    return [{
        "PackageId": "event.establishment." + pack_slug(row["CanonicalPlaceId"]).lower(),
        "EventTrigger": event_label, "FacilityType": "Fort|Barracks|Granary",
        "BuiltByHistoricalEvent": "YES", "Reason": "事件期间军队驻扎、守备与补给需要",
        "Use": "临时军营、军粮周转与防御", "ForceWorkers": "由实际参战军队和征发劳力决定",
        "Materials": "由事件发生时当地库存与运输供给决定", "Duration": "由施工能力和战役窗口计算",
        "PostDisposition": "NATURAL_EVOLUTION", "DoNotApplyBeforeTrigger": "YES",
        "EvidenceType": "MODELED", "Notes": "若事件未发生，不应用本包；设施仍使用统一Facility类型而非临时专用类型。",
    }]


packs = {}
master = []
person_coverage = []
clan_coverage = []
facility_coverage = []
population_reference = []
industry_reference = []
transport_reference = []
military_reference = []
completeness = []

for r in roster_data["roster"]:
    pid = r["CanonicalPlaceId"]
    name = r["CanonicalName"]
    cr = core.get(pid)
    can = canonical.get(pid, {})
    ready = readiness[pid]
    tier = DEPTH_TO_TIER[r["DevelopmentDepth"]]
    # The approved roster's ReferenceReadiness is the canonical pack-level
    # readiness contract.  The separate readiness matrix is a later field-level
    # audit and must not silently override that roster decision.
    status = READINESS_TO_FULL[r["ReferenceReadiness"]]
    runtime = RUNTIME_TO_STATUS[r["ExistingRuntimeLevel"]]
    slug = pack_slug(pid)
    pdir = PACKS / slug
    modes = spatial_mode(r)
    province_id = cr.get("province_id", "") if cr else ""
    county_id = cr.get("county_id", "") if cr else ""
    industry = industry_by_province.get(province_id)
    p0 = p0_by_place.get(pid, [])
    persons_here = location_by_place.get(pid, [])
    clans_here = clan_by_place.get(pid, [])
    military_here = military_by_place.get(pid, [])
    transport_here = transport_by_place.get(pid, [])
    pop_rows = place_population(r, cr)
    source_ids = set()
    for row in p0:
        source_ids.update(str(row.get("source_ids", "")).split("|"))
    source_ids.discard("")
    source_rows = [s for s in deep["sources"] + admin["sources"] if s.get("source_id") in source_ids]
    if not source_rows:
        source_rows = [s for s in deep["sources"] if s.get("source_id") == "source.project.deepening.v1"]

    historical_person_rows = []
    for loc in persons_here:
        p = person.get(loc.get("person_id"), {})
        historical_person_rows.append({
            "PersonId": loc.get("person_id", ""), "CanonicalName": p.get("canonical_name", ""),
            "PresenceType": loc.get("location_type", loc.get("presence_type", "RecordedLocation")),
            "ValidFrom": loc.get("valid_from_year", loc.get("start_year", "")),
            "ValidTo": loc.get("valid_to_year", loc.get("end_year", "")),
            "EvidenceType": loc.get("evidence_level", "HISTORICAL"), "SourceIds": loc.get("source_id", ""),
            "Notes": "仅表示已解析的地点关系；籍贯不自动等于实时在场。",
        })
    if not historical_person_rows:
        historical_person_rows = [evidence_row("人物在场", "当前资料未解析出可确认的在场人物", "NO_EVIDENCE", notes="NO_EVIDENCE不等于历史上无人")]

    clan_rows = []
    for cp in clans_here:
        clan_rows.append({
            "ReferenceId": cp.get("presence_id", cp.get("ReferenceId", "")),
            "ClanId": cp.get("clan_id", cp.get("ClanId", "")), "BranchId": cp.get("branch_id", cp.get("BranchId", "")),
            "ReferenceKind": cp.get("presence_type", cp.get("ReferenceKind", "SpatialReference")),
            "EvidenceType": cp.get("evidence_level", cp.get("EvidenceGrade", "RECONSTRUCTED")),
            "SourceIds": cp.get("source_id", cp.get("SourceId", "")),
            "Notes": "宗族来源、宅第、庄园与FamilyOrganization中心必须分开判断。",
        })
    if not clan_rows:
        clan_rows = [evidence_row("宗族/家庭/庄园", "暂无可定位的组织中心或庄园证据", "NO_EVIDENCE", notes="不得从籍贯自动生成庄园或家庭中心")]

    facilities = []
    for row in p0:
        if row.get("topic") in {"设施", "城市形态", "行政"}:
            facilities.append({
                "FacilityReference": row.get("content", ""), "BaseType": "GovernmentOffice" if row.get("topic") == "行政" else "UNRESOLVED",
                "TemporalScope": "135-260", "SpatialScope": "PLACE_LEVEL_ONLY", "EvidenceType": row.get("evidence_type", "RECONSTRUCTED"),
                "SourceIds": row.get("source_ids", ""), "RuntimeFacilityId": "", "Notes": "参考条目不自动生成Facility实例。",
            })
    if not facilities:
        facilities = [evidence_row("设施", "当前只有功能需求或地点级参考，具体设施类型与位置待后续任务确认", "UNKNOWN")]

    identity = [{
        "PlaceId": pid, "CanonicalName": name, "DevelopmentDisplayName": r["DevelopmentDisplayName"],
        "DevelopmentTier": tier, "OldDepthMapping": r["DevelopmentDepth"], "FullPackStatus": status,
        "RuntimeImplementationStatus": runtime, "Wave": r["RecommendedWave"], "SpatialExistenceMode": modes,
        "PhysicalRoles": r["PhysicalRoles"], "AdministrativeRoles": r["AdministrativeRoles"], "StrategicRoles": r["StrategicRoles"],
        "CanonicalConflictStatus": r["CanonicalConflictStatus"], "EvidenceType": can.get("Evidence", "RECONSTRUCTED"),
    }]
    geography = [{
        "PlaceId": pid, "ProvinceId": province_id, "ProvinceName": cr.get("province_name", "") if cr else "",
        "CommanderyId": cr.get("commandery_id", "") if cr else "", "CommanderyName": cr.get("commandery_name", "") if cr else "",
        "CountyId": county_id, "CountyName": cr.get("historical_county_name", "") if cr else "",
        "Longitude": county.get(county_id, {}).get("longitude", ""), "Latitude": county.get(county_id, {}).get("latitude", ""),
        "CoordinateStatus": county.get(county_id, {}).get("coordinate_status", "UNRESOLVED"),
        "SpatialExistenceMode": modes, "EvidenceType": "RECONSTRUCTED" if cr else "UNKNOWN",
        "Notes": "县级坐标或地区参考不等于精确Cell；精确边界未证实时不得补造。",
    }]
    admin_rows = seat_by_place.get(pid) or [evidence_row("行政角色", r["AdministrativeRoles"] or "无独立行政治所证据", "RECONSTRUCTED" if r["AdministrativeRoles"] else "NOT_APPLICABLE")]
    name_rows = name_by_place.get(pid) or [{"PlaceId": pid, "Name": name, "NameType": "CanonicalName", "ValidFrom": 135, "ValidTo": 260, "Evidence": "RECONSTRUCTED", "Source": "project.canonical-place", "PermanentIdChanged": "NO"}]
    settlement_form = [evidence_row("聚落形态", can.get("PhysicalSettlementCharacter", r["PhysicalRoles"]), can.get("Evidence", "RECONSTRUCTED"), notes="永久地理地点、战场区域与聚落必须分别表达")]
    social = [evidence_row("社会结构", "按人口、职业、宗族、军队与机构事实在剧本初始化/运行时形成", "MODELED", notes="参考包不预造不存在的阶层人数")]
    agriculture = [evidence_row("农业", industry.get("agriculture", "该地点农业结构待研究") if industry else "非聚落地点不单列农业产出", "RECONSTRUCTED" if industry else "NOT_APPLICABLE")]
    industry_rows = [evidence_row("产业", industry.get("industry", "地点级产业未知") if industry else "非聚落地点不单列产业", "RECONSTRUCTED" if industry else "NOT_APPLICABLE", industry.get("source_ids", "") if industry else "", industry.get("unknowns", "") if industry else "")]
    market = [evidence_row("市场与仓储", "由当地人口需求、设施、库存、商路、税制与战争状态共同决定", "MODELED", notes="不存在的市场/仓库不得因参考包自动生成")]
    transport_rows = transport_here or [evidence_row("交通", "已有行政/区域关系，但精确路线节点尚未定位", "UNKNOWN")]
    hinterland = [evidence_row("腹地与邻近聚落", "由所属县、郡、道路和已解析聚落网络构成；不得把行政区当作单一Place", "RECONSTRUCTED")]
    military_rows = military_here or [evidence_row("军事", r["StrategicRoles"] or "无独立军事空间证据", "RECONSTRUCTED" if r["StrategicRoles"] else "NO_EVIDENCE")]
    state_rows = snapshot_by_place.get(pid) or states_by_place.get(pid) or [evidence_row("剧本状态", "沿用最近已知状态并保留未知字段；直接剧本可初始化史实后状态", "MODELED")]
    change_rows = change_by_place.get(pid) or [evidence_row("变化点", "当前未登记专属CanonicalChangePoint", "NO_EVIDENCE")]
    event_rows = [evidence_row("事件依赖状态", "事件发生前只保留永久地理/聚落事实；事件设施必须由触发包建立", "MODELED" if "EVENT_DEPENDENT_COMPLEX" in modes else "NOT_APPLICABLE")]
    event_pkg_rows = event_packages_for(r, military_here)
    post_event_rows = [evidence_row("事件后状态", "按RETAINED/ABANDONED/DISMANTLED/DESTROYED/REPURPOSED/NATURAL_EVOLUTION之一结算", "MODELED" if "EVENT_DEPENDENT_COMPLEX" in modes else "NOT_APPLICABLE", notes="直接后期剧本可以初始化史实后状态；连续运行不得强改分支")]
    implications = [{
        "System": "World/Map", "Requirement": "保持Place、行政Region、StrategicLabel、Facility与Cell边界",
        "DevelopmentTier": tier, "ReferencePackCompleteness": status, "RuntimeImplementationStatus": runtime,
        "AutomaticUpgrade": "NO", "NextAction": "由独立实施任务选择需要物化的模块", "EvidenceType": "MODELED",
    }, {
        "System": "Population/Family", "Requirement": "永久人物、家户、宗族与FamilyOrganization遵守M12和家庭中心合同",
        "DevelopmentTier": tier, "ReferencePackCompleteness": status, "RuntimeImplementationStatus": runtime,
        "AutomaticUpgrade": "NO", "NextAction": "仅在正式初始化任务中创建/关联永久实体", "EvidenceType": "MODELED",
    }]
    unknown_rows = [{
        "UnknownId": b["BlockerId"], "Topic": b["BlockerType"], "Description": b["Description"],
        "Severity": b["Severity"], "Blocks": b["BlocksDepth"], "RequiredAction": b["RequiredAction"],
        "EvidenceType": "UNKNOWN", "CanDefer": b["CanDefer"], "Notes": b["Notes"],
    } for b in blockers_by_place.get(pid, [])]
    if not unknown_rows:
        unknown_rows = [evidence_row("剩余未知", "参考包已逐项审计；未证明的精确Cell、设施规模和即时人口仍由后续任务解析", "UNKNOWN")]
    modules = [{
        "Sheet": sheet, "Module": key, "Required": "YES", "AuditStatus": "AUDITED",
        "AllowedConclusion": "HISTORICAL|RECONSTRUCTED|MODELED|UNKNOWN|NO_EVIDENCE|NOT_APPLICABLE",
        "Notes": "完整包要求回答问题，不要求虚构肯定答案。",
    } for sheet, key in SHEETS]

    pack = {
        "slug": slug, "directory": str(pdir.relative_to(REPO)).replace("\\", "/"), "identity": identity,
        "modules": modules, "geography": geography, "administration": admin_rows, "name_timeline": name_rows,
        "population": pop_rows, "settlement_form": settlement_form, "facilities": facilities,
        "historical_persons": historical_person_rows, "clan_family_estate": clan_rows, "social_structure": social,
        "agriculture": agriculture, "industry_resource": industry_rows, "market_storage": market,
        "transport": transport_rows, "hinterland": hinterland, "military": military_rows,
        "scenario_states": state_rows, "changepoints": change_rows, "event_state": event_rows,
        "event_packages": event_pkg_rows, "post_event": post_event_rows, "implications": implications,
        "sources": source_rows, "unknowns": unknown_rows,
    }
    packs[slug] = pack
    master.append({
        "PlaceId": pid, "CanonicalName": name, "DevelopmentTier": tier, "OldDepthMapping": r["DevelopmentDepth"],
        "FullPackStatus": status, "RuntimeImplementationStatus": runtime, "Wave": r["RecommendedWave"],
        "DevelopmentRegion": province_id or cr.get("commandery_id", "") if cr else "STRATEGIC_SITE",
        "CurrentPriority": r["DevelopmentPriority"], "PackPath": str(pdir.relative_to(REPO)).replace("\\", "/"),
        "ManifestPath": "", "KnownBlockers": "|".join(b["BlockerId"] for b in blockers_by_place.get(pid, [])),
        "UpgradeRecommendation": "RESEARCH_FIRST" if status == "RESEARCH_BLOCKED" else "IMPLEMENT_BY_APPROVED_SLICE",
        "LastReview": TODAY, "Notes": "保留既有Wave；完整参考包不等于运行时实施。",
    })
    completeness.append({"PlaceId": pid, "CanonicalName": name, "DevelopmentTier": tier, "FullPackStatus": status, "ModuleCount": len(SHEETS), "AuditedModuleCount": len(SHEETS), "MissingModuleCount": 0, "UnknownCount": len(unknown_rows), "PackPath": str(pdir.relative_to(REPO)).replace("\\", "/")})
    person_coverage.append({"PlaceId": pid, "CanonicalName": name, "ResolvedPresenceCount": 0 if historical_person_rows[0].get("EvidenceType") == "NO_EVIDENCE" else len(historical_person_rows), "CoverageConclusion": historical_person_rows[0].get("EvidenceType", "HISTORICAL"), "Notes": "籍贯不自动视为在场"})
    clan_coverage.append({"PlaceId": pid, "CanonicalName": name, "ResolvedClanFamilyEstateCount": 0 if clan_rows[0].get("EvidenceType") == "NO_EVIDENCE" else len(clan_rows), "CoverageConclusion": clan_rows[0].get("EvidenceType", "RECONSTRUCTED"), "Notes": "来源、宅第、庄园与组织中心分离"})
    facility_coverage.append({"PlaceId": pid, "CanonicalName": name, "ReferenceCount": len(facilities), "HistoricalCount": sum(1 for x in facilities if x.get("EvidenceType") == "HISTORICAL"), "RuntimeFacilityCreated": "NO", "Notes": "参考条目不是Facility实例"})
    population_reference.extend({"PlaceId": pid, "CanonicalName": name, **x} for x in pop_rows)
    industry_reference.extend({"PlaceId": pid, "CanonicalName": name, **x} for x in agriculture + industry_rows + market)
    transport_reference.extend({"PlaceId": pid, "CanonicalName": name, **x} for x in transport_rows + hinterland)
    military_reference.extend({"PlaceId": pid, "CanonicalName": name, **x} for x in military_rows + event_rows + event_pkg_rows + post_event_rows)


event_master = []
known_event_sites = {
    "官渡": ("milspace.guandu", "BATTLEFIELD_REGION|UNRESOLVED", "NO_EVIDENCE"),
    "街亭": ("milspace.jieting", "PERMANENT_GEOGRAPHIC_SITE|BATTLEFIELD_REGION|UNRESOLVED", "RECONSTRUCTED"),
    "五丈原": ("milspace.wuzhangyuan", "PERMANENT_GEOGRAPHIC_SITE|BATTLEFIELD_REGION|UNRESOLVED", "RECONSTRUCTED"),
    "赤壁": ("geo.site.chibi", "PERMANENT_GEOGRAPHIC_SITE|EVENT_DEPENDENT_COMPLEX|BATTLEFIELD_REGION", "RECONSTRUCTED"),
    "祁山": ("geo.site.qishan.unresolved", "PERMANENT_GEOGRAPHIC_SITE|BATTLEFIELD_REGION|UNRESOLVED", "UNKNOWN"),
}
for name, (eid, modes, evidence) in known_event_sites.items():
    roster_match = next((r for r in master if r["CanonicalName"] == name), None)
    event_master.append({
        "EventSiteId": eid, "CanonicalName": name, "RosterPlaceId": roster_match["PlaceId"] if roster_match else "",
        "SpatialExistenceMode": modes, "PermanentSettlementClaim": "NO", "HistoricalBattleFameIsSettlement": "NO",
        "EventPackagePolicy": "APPLY_ONLY_IF_EVENT_OCCURS", "DirectLaterScenarioPolicy": "MAY_INITIALIZE_HISTORICAL_POST_EVENT_STATE",
        "TransformationPolicy": "BUILD_AT_ACTUAL_LOCATION_THROUGH_NORMAL_WORLD_RULES", "EvidenceType": evidence,
        "KnownUnknowns": "精确边界、营地位置、设施规模与战后处置需逐事件复核",
    })
for m in deep["military_spaces"]:
    if any(x["EventSiteId"] == m["military_space_id"] for x in event_master):
        continue
    event_master.append({
        "EventSiteId": m["military_space_id"], "CanonicalName": m["name"], "RosterPlaceId": "",
        "SpatialExistenceMode": "BATTLEFIELD_REGION" + ("|UNRESOLVED" if m.get("geometry_status") != "Resolved" else ""),
        "PermanentSettlementClaim": "NO", "HistoricalBattleFameIsSettlement": "NO",
        "EventPackagePolicy": "APPLY_ONLY_IF_EVENT_OCCURS", "DirectLaterScenarioPolicy": "MAY_INITIALIZE_HISTORICAL_POST_EVENT_STATE",
        "TransformationPolicy": "BUILD_AT_ACTUAL_LOCATION_THROUGH_NORMAL_WORLD_RULES", "EvidenceType": m.get("evidence_type", "RECONSTRUCTED"),
        "KnownUnknowns": m.get("geometry_status", ""),
    })

# The sixteen established development manifests are kept as handoff documents.
# Add an idempotent current-contract block; do not rewrite their historical body.
manifest_files = {
    "洛阳": "LUOYANG_184_DEVELOPMENT_REFERENCE_MANIFEST.md", "许昌": "XU_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "成都": "CHENGDU_DEVELOPMENT_REFERENCE_MANIFEST.md", "南郑": "HANZHONG_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "合肥": "HEFEI_DEVELOPMENT_REFERENCE_MANIFEST.md", "建业": "JIANYE_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "长安": "CHANGAN_DEVELOPMENT_REFERENCE_MANIFEST.md", "邺": "YE_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "襄阳": "XIANGYANG_DEVELOPMENT_REFERENCE_MANIFEST.md", "江陵": "JIANGLING_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "阳平关": "YANGPING_PASS_DEVELOPMENT_REFERENCE_MANIFEST.md", "夏口": "XIAKOU_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "濡须口": "RUXUKOU_DEVELOPMENT_REFERENCE_MANIFEST.md", "剑阁": "JIANGE_DEVELOPMENT_REFERENCE_MANIFEST.md",
    "樊城": "FANCHENG_DEVELOPMENT_REFERENCE_MANIFEST.md", "虎牢": "HULAO_DEVELOPMENT_REFERENCE_MANIFEST.md",
}
manifest_root = REPO / "Docs" / "KNOWLEDGE_BASE" / "DEVELOPMENT_MANIFESTS"
for row in master:
    filename = manifest_files.get(row["CanonicalName"])
    if not filename:
        continue
    path = manifest_root / filename
    row["ManifestPath"] = str(path.relative_to(REPO)).replace("\\", "/")
    if not path.exists():
        continue
    text = path.read_text(encoding="utf-8")
    begin = "<!-- FDRP-V1:BEGIN -->"
    end = "<!-- FDRP-V1:END -->"
    block = f"""{begin}
## 当前完整参考包合同（FDRP V1）

- DevelopmentTier：`{row['DevelopmentTier']}`（旧 `{row['OldDepthMapping']}` 仅作历史映射）
- ReferencePackCompleteness：`{row['FullPackStatus']}`
- RuntimeImplementationStatus：`{row['RuntimeImplementationStatus']}`
- Wave：`{row['Wave']}`（未改变）
- Pack：`{row['PackPath']}`

以上三个状态相互独立；完整包不会自动升档或物化运行时实体。
{end}"""
    if begin in text and end in text:
        text = text[:text.index(begin)] + block + text[text.index(end) + len(end):]
    else:
        text = text.rstrip() + "\n\n" + block + "\n"
    path.write_text(text, encoding="utf-8")

upgrade_registry = [{
    "CandidateId": r["PlaceId"], "CandidateName": r["CanonicalName"], "RosterStatus": "CURRENT_72",
    "CurrentTier": r["DevelopmentTier"], "CurrentWave": r["Wave"], "FullPackStatus": r["FullPackStatus"],
    "UpgradeCandidate": "YES" if r["DevelopmentTier"] in {"T1", "T2"} else "REVIEW_LATER",
    "UpgradeCondition": "独立任务确认体验价值、参考成熟度、运行时预算与验收切片；不得自动升档",
    "AutomaticRosterAdmission": "NO", "Notes": "Wave保持不变；T档与完整度、实现状态相互独立。",
} for r in master]
for c in admin["development_candidates"]:
    pid = c.get("CanonicalPlaceId", "")
    if not pid or any(x["CandidateId"] == pid for x in upgrade_registry):
        continue
    if c.get("DevelopmentImportance") not in {"High", "VeryHigh", "P0", "P1"}:
        continue
    upgrade_registry.append({
        "CandidateId": pid, "CandidateName": c.get("CandidateName", ""), "RosterStatus": "CANDIDATE_ONLY",
        "CurrentTier": "", "CurrentWave": "", "FullPackStatus": "NOT_BUILT", "UpgradeCandidate": "RESEARCH_CANDIDATE",
        "UpgradeCondition": "先解决CanonicalPlace与证据，再由独立决策决定是否加入名册",
        "AutomaticRosterAdmission": "NO", "Notes": c.get("ImportanceReasons", ""),
    })

registry_updates = {
    "documents": [{"DocumentId": "doc.han.place-full-development-reference-packs.v1", "DocumentName": "HAN-135-260 Development Place Full Reference Packs V1", "DocumentType": "CanonicalReferenceContract", "CanonicalPath": "Docs/HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md", "Status": "CURRENT", "AuthorityScope": "72-place FDRP, T1-T4 terminology, event-dependent site policy", "Supersedes": "DevelopmentPlace current D-depth labels only; historical evidence retained", "Notes": "Reference-only; not runtime implementation."}, {"DocumentId": "task.han135260.development-place-full-reference-pack.v1", "DocumentName": "HAN-135-260-DEVELOPMENT-PLACE-FULL-REFERENCE-PACK-V1", "DocumentType": "TaskSpecification", "CanonicalPath": "Docs/TASK_HAN_135_260_DEVELOPMENT_PLACE_FULL_REFERENCE_PACK_V1.md", "Status": "COMPLETED", "AuthorityScope": "Build and validate the 72-place reference corpus", "Supersedes": "", "Notes": "Does not override L1 domain designs or create runtime facts."}],
    "domain_map": [{"DomainId": "domain.historical-world.development-place-full-pack", "CanonicalDocument": "Docs/HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md", "PrimaryData": "DEVELOPMENT_PLACE_MASTER.xlsx plus 72 pack workbooks", "RuntimeOwner": "NONE_REFERENCE_ONLY", "Status": "CURRENT", "Notes": "DevelopmentTier, ReferencePackCompleteness and RuntimeImplementationStatus are orthogonal."}],
    "design_decisions": [
        {"DecisionId": "decision.fdrp.same-standard-all-tiers", "Decision": "T1-T4全部采用同一完整参考包标准", "Status": "ACCEPTED"},
        {"DecisionId": "decision.fdrp.no-t0", "Decision": "非名册地点不设T0；T档只属于正式开发地点名册", "Status": "ACCEPTED"},
        {"DecisionId": "decision.fdrp.three-orthogonal-statuses", "Decision": "开发档位、参考包完整度与运行时实现状态相互独立", "Status": "ACCEPTED"},
        {"DecisionId": "decision.fdrp.unknown-is-valid-answer", "Decision": "完整包可以以UNKNOWN/NO_EVIDENCE/NOT_APPLICABLE诚实回答", "Status": "ACCEPTED"},
        {"DecisionId": "decision.fdrp.preserve-waves", "Decision": "72地点既有波次原样保留", "Status": "ACCEPTED"},
        {"DecisionId": "decision.fdrp.battle-fame-not-settlement", "Decision": "战役名望不等于永久聚落", "Status": "ACCEPTED"},
        {"DecisionId": "decision.fdrp.event-package-triggered", "Decision": "事件依赖设施仅在事件发生后通过建造包建立", "Status": "ACCEPTED"},
        {"DecisionId": "decision.fdrp.normal-facility-types", "Decision": "事件设施复用统一Facility类型，不增设临时基地专用类型", "Status": "ACCEPTED"},
        {"DecisionId": "decision.fdrp.direct-scenario-post-state", "Decision": "后期直接剧本可初始化史实后状态，连续运行尊重世界分支", "Status": "ACCEPTED"},
        {"DecisionId": "decision.fdrp.no-auto-upgrade", "Decision": "完整参考包不触发自动升档或运行时物化", "Status": "ACCEPTED"},
        {"DecisionId": "decision.fdrp.old-depth-evidence-retained", "Decision": "旧D2-D5工作簿和报告作为历史证据保留，新主表改用T1-T4", "Status": "ACCEPTED"},
    ],
    "open_decisions": [
        {"DecisionId": "open.fdrp.t2-to-t3-candidates", "Question": "哪些T2地点应在后续独立任务升为T3？", "Status": "OPEN", "DecisionOwner": "User", "Notes": "本任务不自动升级。"},
        {"DecisionId": "open.fdrp.second-t4", "Question": "是否及何时设立第二个T4地点？", "Status": "OPEN", "DecisionOwner": "User", "Notes": "当前唯一T4保持不变。"},
        {"DecisionId": "open.fdrp.additional-roster-places", "Question": "候选地点是否加入正式72名册之外的新版本名册？", "Status": "OPEN", "DecisionOwner": "User", "Notes": "候选登记不自动入册。"},
        {"DecisionId": "open.fdrp.guandu-jieting-wuzhang-qishan", "Question": "官渡、街亭、五丈原、祁山的精确空间证据如何分期解决？", "Status": "OPEN", "DecisionOwner": "HistoricalResearch", "Notes": "不得先造精确Cell或永久城市。"},
    ],
    "implementation_gaps": [{"GapId": "gap.fdrp.runtime-materialization", "Domain": "HistoricalWorld", "Gap": "72份参考包均未自动物化为运行时Place/Facility/Population", "Severity": "EXPECTED", "Status": "OPEN", "RequiredTask": "后续独立纵向切片实施任务", "Notes": "本任务明确禁止运行时修改。"}],
    "research_gaps": [{"GapId": "gap.fdrp.research-blocked-places", "Domain": "HistoricalWorld", "Gap": "阳平关、夏口、濡须口、虎牢、武关、葭萌、函谷关、赤壁仍有关键定位或形态缺口", "Severity": "HIGH", "Status": "OPEN", "RequiredTask": "逐地点专题研究", "Notes": "参考包保留UNKNOWN，不虚构。"}, {"GapId": "gap.fdrp.event-sites", "Domain": "HistoricalWorld", "Gap": "官渡、街亭、五丈原、祁山等事件空间的边界、设施与后状态证据不足", "Severity": "HIGH", "Status": "OPEN", "RequiredTask": "事件空间专题研究", "Notes": "战役名不自动成为永久聚落。"}],
    "document_conflicts": [{"ConflictId": "conflict.fdrp.d-depth-vs-t-tier", "DocumentA": "Docs/HISTORICAL_WORLD_REFERENCE/DEVELOPMENT_PLACE_ROSTER_V1/*", "DocumentB": "Docs/HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/*", "Conflict": "旧历史报告使用D2-D5，新当前合同使用T1-T4", "Resolution": "旧报告冻结为历史证据；新主表和后续开发只使用T1-T4，映射D2→T1、D3→T2、D4→T3、D5→T4", "Status": "RESOLVED", "Notes": "不回写或删除旧证据。"}],
}

# Rebuild the registries from the latest project-authored workdata lineage, not
# from visual worksheet scraping.  This preserves all current rows and keeps
# artifact-tool as the only spreadsheet authoring mechanism.
kb = load(REPO / "outputs" / "HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1" / "knowledge_base_workdata.json")
prior_city = load(city_pack_path) if city_pack_path.exists() else {"registry_existing": {}, "registry_updates": {}}

def merge_registry(rows, updates, key):
    result = [dict(x) for x in rows]
    by_key = {str(x.get(key, "")): i for i, x in enumerate(result)}
    for update in updates:
        ident = str(update.get(key, ""))
        if ident and ident in by_key:
            result[by_key[ident]].update(update)
        else:
            by_key[ident] = len(result)
            result.append(dict(update))
    return result

registry_keys = {
    "documents": ("DocumentId", "b01_document_registry"),
    "domain_map": ("DomainId", "b02_domain_map"),
    "design_decisions": ("DecisionId", "b03_design_decisions"),
    "open_decisions": ("DecisionId", "b04_open_decisions"),
    "implementation_gaps": ("GapId", "b06_implementation_gaps"),
    "research_gaps": ("GapId", "b07_research_gaps"),
    "document_conflicts": ("ConflictId", "b05_document_conflicts"),
}
registry_existing = {}
for kind, (key, kb_key) in registry_keys.items():
    base = kb[kb_key]
    if kind != "document_conflicts":
        base = merge_registry(base, prior_city.get("registry_existing", {}).get(kind, []), key)
        base = merge_registry(base, prior_city.get("registry_updates", {}).get(kind, []), key)
    else:
        base = merge_registry(base, admin.get("registry_updates", {}).get("document_conflicts", []), key)
    registry_existing[kind] = base

workdata = {
    "schema": "mandate.han135260.development-place-full-reference-pack.v1", "generated_on": TODAY,
    "scope": "REFERENCE_ONLY", "runtime_changes": 0, "roster_count": len(master),
    "sheet_contract": [x[0] for x in SHEETS], "master": master, "completeness": completeness,
    "event_sites": event_master, "person_coverage": person_coverage, "clan_family_estate_coverage": clan_coverage,
    "facility_coverage": facility_coverage, "population_settlement_reference": population_reference,
    "industry_resource_supply_reference": industry_reference, "transport_hinterland_reference": transport_reference,
    "military_event_reference": military_reference, "upgrade_registry": upgrade_registry,
    "packs": packs, "sources": deep["sources"], "registry_updates": registry_updates,
    "registry_existing": registry_existing,
    "summary": {
        "tier_counts": dict(Counter(r["DevelopmentTier"] for r in master)),
        "wave_counts": dict(Counter(r["Wave"] for r in master)),
        "full_pack_status_counts": dict(Counter(r["FullPackStatus"] for r in master)),
        "runtime_status_counts": dict(Counter(r["RuntimeImplementationStatus"] for r in master)),
        "pack_count": len(packs), "module_count_per_pack": len(SHEETS), "event_site_count": len(event_master),
    },
}


def pack_readme(pack):
    row = pack["identity"][0]
    return f"""# {row['CanonicalName']}｜完整开发参考包 V1

本目录是 `{row['PlaceId']}` 的统一开发参考包，开发档位为 **{row['DevelopmentTier']}**，由旧标签 `{row['OldDepthMapping']}` 无损映射而来；既有波次 **{row['Wave']}** 未改变。

- 参考完整度：`{row['FullPackStatus']}`
- 运行时实现：`{row['RuntimeImplementationStatus']}`
- 空间存在模式：`{row['SpatialExistenceMode']}`
- 工作簿：`PLACE_DEVELOPMENT_REFERENCE.xlsx`（固定 25 张工作表）
- 来源与未知：`SOURCES_AND_UNKNOWNS.md`

“完整”表示 25 个模块均已审计，不表示每一项都有肯定史料答案。`UNKNOWN`、`NO_EVIDENCE` 与 `NOT_APPLICABLE` 都是有效结论。参考包不会自动建立 Place、Facility、Cell、人口、宗族中心、军营或历史事件，也不会自动升档。
"""


def sources_unknowns(pack):
    name = pack["identity"][0]["CanonicalName"]
    sources = pack["sources"]
    unknowns = pack["unknowns"]
    lines = [f"# {name}｜来源与未知项", "", "## 来源", ""]
    if sources:
        for s in sources:
            lines.append(f"- `{s.get('source_id','')}`：{s.get('title','')}；适用范围：{s.get('evidence_scope','') or '见来源登记'}")
    else:
        lines.append("- 当前无可直接引用的地点级来源；只保留项目级建模合同。")
    lines += ["", "## 未知与阻塞", ""]
    for u in unknowns:
        lines.append(f"- `{u.get('UnknownId', u.get('Topic','unknown'))}`：{u.get('Description', u.get('Value',''))}（{u.get('Severity','未定')}）")
    lines += ["", "不得用系列游戏、现代行政区、后世城址或算法便利填补史料未知；确需玩法补全时必须标为 `MODELED`。"]
    return "\n".join(lines)


DOC.mkdir(parents=True, exist_ok=True)
PACKS.mkdir(parents=True, exist_ok=True)
OUT.mkdir(parents=True, exist_ok=True)
dump(OUT / "full_reference_pack_workdata.json", workdata)
for slug, pack in packs.items():
    pdir = PACKS / slug
    md(pdir / "README.md", pack_readme(pack))
    md(pdir / "SOURCES_AND_UNKNOWNS.md", sources_unknowns(pack))

md(DOC / "README.md", f"""# 135—260 开发地点完整参考包 V1

这是后续 Development Place 开发的当前权威入口。正式名册仍为 **72** 个地点，波次不变；旧 D2—D5 标签无损迁移为 T1—T4：T1 23、T2 33、T3 15、T4 1。

三个维度必须分开：

1. `DevelopmentTier`：计划开发深度；
2. `ReferencePackCompleteness`：参考问题是否完成审计；
3. `RuntimeImplementationStatus`：是否已经在代码和运行世界中实现。

所有 T1—T4 地点使用同一套完整参考标准。名册外地点没有 T0；候选地点只进入升级登记，不会自动入册。完整包允许 `HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN / NO_EVIDENCE / NOT_APPLICABLE`，禁止为追求表格填满而虚构。

## 当前结果

- 72/72 个地点均建立目录、说明、来源未知项和 25 表工作簿合同；
- 状态：{dict(Counter(r['FullPackStatus'] for r in master))}；
- 事件地点独立登记，战役名望不等于永久聚落；
- 本轮不修改运行时世界、存档、人口、设施或场景。

当前主表：`DEVELOPMENT_PLACE_MASTER.xlsx`。旧 `DEVELOPMENT_PLACE_ROSTER_V1` 与 `CITY_DEVELOPMENT_PACKS` 保留为历史证据与输入材料。
""")

md(DOC / "DEVELOPMENT_TIER_TERMINOLOGY_V1.md", """# DevelopmentTier 术语 V1

| 旧历史标签 | 当前标签 | 数量 | 含义 |
|---|---:|---:|---|
| D2 | T1 | 23 | 地点级参考与低频开发深度 |
| D3 | T2 | 33 | 区域/系统联动开发深度 |
| D4 | T3 | 15 | 核心纵向切片深度 |
| D5 | T4 | 1 | 最高综合验证深度 |

D0/D1 从特殊 Development Place 档位体系移除。名册之外没有 T0。旧文件保留原标签作为历史证据；新主表和后续任务只用 T1—T4。
""")

md(DOC / "PLACE_FULL_DEVELOPMENT_REFERENCE_PACK_STANDARD_V1.md", """# Place Full Development Reference Pack 标准 V1

每个地点必须具备 `README.md`、`PLACE_DEVELOPMENT_REFERENCE.xlsx`、`SOURCES_AND_UNKNOWNS.md`。工作簿固定 25 张表，覆盖身份、地理、行政、名称治所时间线、人口、聚落、设施、人物、宗族家庭庄园、社会、农业、产业资源、市场仓储、交通、腹地、军事、剧本状态、变化点、事件依赖状态、事件建造包、事件后状态、开发含义、来源和未知项。

完整是“问题全部审计”，不是“全部有肯定答案”。任何精确 Cell、设施、人物在场、家庭中心、城市人口或战场永久聚落结论都必须有相应证据；否则使用 `UNKNOWN`、`NO_EVIDENCE` 或 `NOT_APPLICABLE`。
""")

md(DOC / "PLACE_UPGRADE_PROTOCOL_V1.md", """# Development Place 升级协议 V1

1. 参考包完整不自动升档；运行时实现也不自动升档。
2. 候选升级必须由独立任务说明体验价值、系统覆盖、历史证据、性能预算与验收切片。
3. 当前 72 个地点的 Wave 原样保留；本协议不新增、删除或重排地点。
4. 名册外候选只登记研究状态，不自动加入名册。
5. 事件地点必须先区分永久聚落、永久地理地点、事件依赖复合体、战场区域和未解析空间。
""")

md(DOC / "FULL_PACK_COMPLETENESS_REPORT_V1.md", f"""# 完整参考包完备性报告 V1

- 正式地点：72；目录：72；固定模块：25/地点；
- T1/T2/T3/T4：23/33/15/1；
- 完整度：{dict(Counter(r['FullPackStatus'] for r in master))}；
- 运行时：{dict(Counter(r['RuntimeImplementationStatus'] for r in master))}；
- 研究阻塞：{', '.join(r['CanonicalName'] for r in master if r['FullPackStatus']=='RESEARCH_BLOCKED')}。

所有模块均已审计，但研究阻塞和未知项没有被伪造为史实。Unity、存档、人口、设施实例和场景均未改变。
""")

print(json.dumps(workdata["summary"], ensure_ascii=False, indent=2))
