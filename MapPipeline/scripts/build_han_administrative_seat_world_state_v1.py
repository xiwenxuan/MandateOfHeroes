from __future__ import annotations

import hashlib
import json
from collections import Counter, defaultdict
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
DOCS = REPO / "Docs"
HIST_ROOT = DOCS / "HISTORICAL_WORLD_REFERENCE"
TASK_ROOT = HIST_ROOT / "ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1"
LUOYANG_ROOT = TASK_ROOT / "11_LUOYANG_MAJOR_HISTORICAL_WORLD_STATES"
P0_ROOT = TASK_ROOT / "12_P0_PLACE_CHANGEPOINT_CANDIDATES"
OUT = REPO / "outputs" / "HAN_135_260_ADMINISTRATIVE_SEAT_CANONICAL_PLACE_AND_HISTORICAL_WORLD_STATE_V1"

BASE_PATH = REPO / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1" / "historical_world_reference_workdata.json"
DEEP_PATH = REPO / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1" / "deepening_workdata.json"
KB_PATH = REPO / "outputs" / "HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1" / "knowledge_base_workdata.json"
PERSON_ROOT = REPO / "Assets" / "StreamingAssets" / "HistoricalPersons" / "Han135260V1"
LUOYANG_RUNTIME = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184UrbanInitializationV1"
HAN_WORLD = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "HanWorldV1"

SCENARIO_YEARS = [140, 184, 189, 194, 200, 207, 214, 219, 223, 227, 234, 249, 260]
TODAY = "2026-08-11"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, value: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(value.rstrip() + "\n", encoding="utf-8")


def pipe(values):
    return "|".join(str(value) for value in values if value not in (None, ""))


def unique_pipe(values):
    return pipe(sorted({str(value) for value in values if value not in (None, "")}))


def stable_hash(value: str):
    return hashlib.sha1(value.encode("utf-8")).hexdigest()[:12]


def unit_type(name: str):
    if name in ("右扶风", "左冯翊"):
        return "CAPITAL_REGION"
    if name.endswith("属国"):
        return "DEPENDENCY"
    if name.endswith("国"):
        return "KINGDOM"
    if name.endswith("尹"):
        return "YIN"
    if name.endswith("郡"):
        return "COMMANDERY"
    if name.endswith("州") or "司隶" in name:
        return "PROVINCE_OR_CENTRAL"
    return "ADMINISTRATIVE_REGION"


base = load(BASE_PATH)
deep = load(DEEP_PATH)
kb = load(KB_PATH)
persons = load(PERSON_ROOT / "persons.json")["persons"]
person_locations = load(PERSON_ROOT / "person_locations.json")["records"]
luoyang_people = load(LUOYANG_RUNTIME / "historical_persons.json")["people"]
luoyang_facilities = load(LUOYANG_RUNTIME / "facilities.json")["facilities"]
strategic_sites = load(HAN_WORLD / "locations" / "strategic_sites.json")["features"]

core = deep["core_settlements"]
seat_timeline = deep["seat_timeline"]
priority_counties = deep["priority_counties"]
sources = []
for source in base["sources"] + deep["sources"]:
    if not any(existing["source_id"] == source["source_id"] for existing in sources):
        sources.append(source)
if not any(source["source_id"] == "source.project.master_map.v0" for source in sources):
    sources.append(
        {
            "source_id": "source.project.master_map.v0",
            "source_type": "project_reference",
            "title": "MASTER-MAP-V0 project-derived transport and strategic-site reference",
            "author_or_editor": "MandateOfHeroes project",
            "edition_or_host": "Repository MapPipeline/Assets references",
            "url_or_locator": "Docs/TASK_MASTER_MAP_V0_HISTORICAL_GEOGRAPHY_CELL_UNITY_PIPELINE.md",
            "access_date": TODAY,
            "reliability_class": "PROJECT_REFERENCE",
            "evidence_scope": "Reconstructed transport links and provisional strategic-site candidates",
            "license_note": "Project-authored reference; provisional geography does not claim historical precision",
            "notes": "Not an external commercial-game dataset.",
        }
    )
sources_by_id = {row["source_id"]: row for row in sources}
core_by_place = {row["place_id"]: row for row in core}
core_by_county = {row["county_id"]: row for row in core}
priority_by_county = {row["county_id"]: row for row in priority_counties}
city_by_id = {row["city_id"]: row for row in base["cities"]}
core_by_city = {}
for row in core:
    for city_id in str(row.get("city_ids") or "").split("|"):
        if city_id:
            core_by_city[city_id] = row

province_ids = sorted({row["province_id"] for row in base["commanderies"]})
province_name_by_id = {}
for row in base["commanderies"]:
    province_name_by_id[row["province_id"]] = row["province_name"]

family_by_place = {row["PlaceId"]: row for row in kb["a01_important_places"] if row.get("PlaceId")}
person_by_id = {row["person_id"]: row for row in persons}


# ---------------------------------------------------------------------------
# Administrative regions and sparse seat references
# ---------------------------------------------------------------------------

administrative_seats = []
for row in seat_timeline:
    valid_years = [year for year in SCENARIO_YEARS if row["valid_from_year"] <= year <= row["valid_to_year"]]
    administrative_seats.append(
        {
            "AdministrativeUnitId": row["admin_unit_id"],
            "UnitType": unit_type(row["admin_unit_name"]),
            "HistoricalName": row["admin_unit_name"],
            "ScenarioYear/ValidRange": f'{row["valid_from_year"]}-{row["valid_to_year"]}',
            "ScenarioCoverageYears": pipe(valid_years),
            "SeatCountyId": row.get("seat_county_id") or "",
            "SeatPlaceId": row.get("seat_place_id") or "",
            "SeatHistoricalName": core_by_place.get(row.get("seat_place_id"), {}).get("display_name", "UNKNOWN"),
            "SeatRole": row["role_type"],
            "EvidenceLevel": row["evidence_type"],
            "Confidence": row["confidence"],
            "Sources": row["source_id"],
            "InheritedState": "YES_WITHIN_VALID_RANGE",
            "RuntimePolicy": "HISTORICAL_REFERENCE_INITIALIZES_SCENARIO_ONLY",
            "Notes": row["method_notes"],
        }
    )


def seat_at(admin_id: str, year: int):
    candidates = [
        row for row in seat_timeline
        if row["admin_unit_id"] == admin_id and row["valid_from_year"] <= year <= row["valid_to_year"]
    ]
    return candidates[-1] if candidates else None


province_scenario_seats = []
for province_id in province_ids:
    for year in SCENARIO_YEARS:
        row = seat_at(province_id, year)
        province_scenario_seats.append(
            {
                "ProvinceId": province_id,
                "HistoricalProvinceName": province_name_by_id[province_id],
                "ScenarioYear": year,
                "SeatCountyId": row.get("seat_county_id", "") if row else "",
                "SeatPlaceId": row.get("seat_place_id", "") if row else "",
                "SeatHistoricalName": core_by_place.get(row.get("seat_place_id") if row else "", {}).get("display_name", "UNKNOWN"),
                "SeatRole": row.get("role_type", "UNKNOWN") if row else "UNKNOWN",
                "EvidenceLevel": row.get("evidence_type", "UNKNOWN") if row else "UNKNOWN",
                "Source": row.get("source_id", "") if row else "",
                "InheritedFromSparseTimeline": "YES",
                "RuntimeSeatMayDiverge": "YES",
            }
        )


# ---------------------------------------------------------------------------
# Canonical physical places and name timelines
# ---------------------------------------------------------------------------

name_timeline_seed = {
    "place.han140.sili.henan.luoyang": [
        ("雒阳", "HISTORICAL_NAME", 135, 219, "RECONSTRUCTED", "source.hou_han_shu.jun_guo_zhi"),
        ("洛阳", "CANONICAL_DISPLAY_VARIANT", 220, 260, "RECONSTRUCTED", "source.primary.san_guo_zhi.wei"),
    ],
    "place.han140.yuzhou.yingchuan.xu": [
        ("许", "HISTORICAL_NAME", 135, 220, "HISTORICAL", "source.primary.hou_han_shu.xiandi"),
        ("许昌", "RENAMED_PLACE", 221, 260, "RECONSTRUCTED", "source.primary.san_guo_zhi.wei"),
    ],
    "place.han140.jingzhou.jiangxia.e": [
        ("鄂", "HISTORICAL_NAME", 135, 220, "HISTORICAL", "source.hou_han_shu.jun_guo_zhi"),
        ("武昌", "RENAMED_PLACE", 221, 260, "HISTORICAL", "source.primary.san_guo_zhi.wu"),
    ],
    "place.han140.yangzhou.danyang.moling": [
        ("秣陵", "HISTORICAL_NAME", 135, 211, "HISTORICAL", "source.hou_han_shu.jun_guo_zhi"),
        ("建业", "RENAMED_PLACE", 212, 260, "RECONSTRUCTED", "source.primary.san_guo_zhi.wu"),
    ],
    "place.han140.yizhou.ba.yufu": [
        ("鱼复", "HISTORICAL_NAME", 135, 221, "HISTORICAL", "source.hou_han_shu.jun_guo_zhi"),
        ("永安", "RENAMED_PLACE", 222, 260, "RECONSTRUCTED", "source.primary.san_guo_zhi.shu"),
    ],
}

place_name_timeline = []
for place_id, records in name_timeline_seed.items():
    for name, name_type, valid_from, valid_to, evidence, source_id in records:
        place_name_timeline.append(
            {
                "PlaceId": place_id,
                "Name": name,
                "NameType": name_type,
                "ValidFrom": valid_from,
                "ValidTo": valid_to,
                "Language/ScriptVariant": "zh-Hant/zh-Hans canonicalized display",
                "Evidence": evidence,
                "Source": source_id,
                "PermanentIdChanged": "NO",
            }
        )

canonical_places = []
for row in core:
    timeline_ref = f'name.timeline.{stable_hash(row["place_id"])}' if row["place_id"] in name_timeline_seed else ""
    role_summary = pipe(
        role for role, flag in [
            ("CountySeat", row["is_county_seat"]),
            ("CommanderySeat", row["is_commandery_seat"]),
            ("KingdomSeat", row["is_kingdom_seat"]),
            ("ProvinceSeat", row["is_province_seat"]),
            ("Capital", row["is_capital"]),
        ] if flag
    )
    known_facility = "GovernmentFacilityReferenceRequired" if role_summary else "UNKNOWN"
    if row["place_id"] == "place.han140.sili.henan.luoyang":
        known_facility = "Luoyang184 unified Facility set: 1230 instances / 173 historical-definition baseline"
    canonical_places.append(
        {
            "CanonicalPlaceId": row["place_id"],
            "CanonicalName": row["display_name"],
            "PlaceNameTimelineRef": timeline_ref,
            "PrimaryCountyId": row["county_id"],
            "StableGeographyRef": pipe([row["longitude"], row["latitude"], row["coordinate_status"]]),
            "PhysicalSettlementCharacter": "UrbanSettlementCandidate" if row["priority"] in ("P0", "P1") else "Settlement",
            "AdministrativeRolesSummary": role_summary,
            "KnownFacilitiesSummary": known_facility,
            "HistoricalImportance": row["reference_level"],
            "StrategicImportance": pipe(row["city_ids"].split("|")) if row["city_ids"] else "NONE_RECORDED",
            "DevelopmentImportance": row["priority"],
            "Evidence": row["evidence_type"],
            "Sources": row["source_ids"],
            "RuntimeMaterializationStatus": "REFERENCE_ONLY",
            "Notes": "Administrative regions and Seat roles do not create additional Places.",
        }
    )


# ---------------------------------------------------------------------------
# 77 strategic display labels -> physical places
# ---------------------------------------------------------------------------

ADMIN_LABELS = set("C002 C003 C006 C012 C013 C014 C015 C016 C017 C026 C029 C030 C032 C033 C034 C035 C036 C046 C047 C048 C049 C051 C058 C060 C062 C064 C065 C070 C071 C073 C074".split())
MOVING_LABELS = {"C042", "C054"}
NAME_TIMELINE_LABELS = {"C025", "C027", "C045", "C056", "C069"}
STRATEGIC_SETTLEMENT_LABELS = set("C020 C040 C044 C050 C053 C055 C059 C061 C066 C076 C077".split())
CONFLICT_LABELS = {
    "C013": "Existing legacy mapping shares Beihai/Ju with C014; Chengyang physical resolution remains open.",
    "C035": "Existing legacy mapping shares Jincheng/Yunwu with C034; Xiping physical resolution remains open.",
    "C042": "Jiangxia is a moving-seat regional label and cannot bind one permanent seat for all scenarios.",
    "C044": "Gong'an strategic label is provisionally attached to the Chanling county reference.",
    "C054": "Lujiang is a moving-seat regional label; Shu/Wan and later seats require a dedicated timeline.",
    "C061": "Jian'an strategic label currently resolves through the Kuaiji eastern-region proxy; exact place remains open.",
    "C066": "Zitong/Fu naming in series references must not create duplicate Places.",
}


def strategic_relation(city_id: str):
    if city_id in ADMIN_LABELS:
        return "ADMIN_REGION_AS_STRATEGIC_LABEL"
    if city_id in MOVING_LABELS:
        return "MOVING_SEAT_REGION_LABEL"
    if city_id in NAME_TIMELINE_LABELS:
        return "PLACE_RENAME_TIMELINE"
    if city_id in STRATEGIC_SETTLEMENT_LABELS:
        return "STRATEGIC_SETTLEMENT_NOT_MAJOR_SEAT"
    return "PLACE_NAME_DIRECT"


strategic_crosswalk = []
for city in base["cities"]:
    place = core_by_city[city["city_id"]]
    relation = strategic_relation(city["city_id"])
    administrative_region_id = city.get("admin_reference") or place["commandery_id"] if relation in ("ADMIN_REGION_AS_STRATEGIC_LABEL", "MOVING_SEAT_REGION_LABEL") else ""
    timeline_rows = [row for row in place_name_timeline if row["PlaceId"] == place["place_id"]]
    seat_timeline_note = ""
    if city["city_id"] == "C042":
        seat_timeline_note = "西陵基线候选；沙羡/石阳等阶段待专项校核"
    elif city["city_id"] == "C054":
        seat_timeline_note = "舒/皖及后续治所关系待专项校核"
    strategic_crosswalk.append(
        {
            "StrategicLabelId": city["city_id"],
            "StrategicDisplayName": city["display_name"],
            "RelationType": relation,
            "AdministrativeRegionId": administrative_region_id,
            "CanonicalPlaceId": place["place_id"],
            "ActualHistoricalSeatName": place["display_name"],
            "ScenarioDependent": "YES" if city["city_id"] in MOVING_LABELS | NAME_TIMELINE_LABELS else "NO",
            "NameTimeline": pipe(f'{r["Name"]}:{r["ValidFrom"]}-{r["ValidTo"]}' for r in timeline_rows),
            "SeatTimeline": seat_timeline_note,
            "ConflictStatus": "OPEN_MAPPING_CONFLICT" if city["city_id"] in CONFLICT_LABELS else "RESOLVED_TO_EXISTING_PLACE",
            "DevelopmentInterpretation": "Strategic display label only; the physical world uses the referenced CanonicalPlace.",
            "Evidence": place["evidence_type"],
            "Sources": city["source_ids"],
            "ConflictNotes": CONFLICT_LABELS.get(city["city_id"], ""),
        }
    )


# ---------------------------------------------------------------------------
# 133 Core Settlement x 13 Scenario role crosswalk
# ---------------------------------------------------------------------------


def roles_for_place(place, year: int):
    roles = []
    if place["is_county_seat"]:
        roles.append("CountySeat")
    if place["is_commandery_seat"]:
        roles.append("CommanderySeat")
    if place["is_kingdom_seat"]:
        roles.append("KingdomSeat")
    province_row = seat_at(place["province_id"], year)
    if province_row and province_row.get("seat_place_id") == place["place_id"]:
        roles.append("ProvinceSeat/PoliticalCenter")
    if place["is_strategic_city"]:
        roles.append("StrategicDisplayReference")
    if place["place_id"] == "place.han140.sili.henan.luoyang" and year in (140, 184, 189, 223, 227, 234, 249, 260):
        roles.append("CapitalReference" if year <= 189 else "CaoWeiCapitalReference")
    if place["place_id"] == "place.han140.sili.jingzhao.changan" and year in (194,):
        roles.append("ImperialCapitalReference")
    if place["place_id"] == "place.han140.yuzhou.yingchuan.xu" and year in (200, 207, 214, 219):
        roles.append("ImperialCapitalReference")
    return roles


core_seat_crosswalk = []
for place in core:
    for year in SCENARIO_YEARS:
        roles = roles_for_place(place, year)
        core_seat_crosswalk.append(
            {
                "CanonicalPlaceId": place["place_id"],
                "CoreSettlementName": place["display_name"],
                "CountyId": place["county_id"],
                "CommanderyEquivalentId": place["commandery_id"],
                "ProvinceId": place["province_id"],
                "ScenarioYear": year,
                "SeatRolesByScenario": pipe(roles),
                "StrategicLabelIds": place["city_ids"],
                "HistoricalImportance": place["reference_level"],
                "DevelopmentImportance": place["priority"],
                "RoleEvidence": place["evidence_type"],
                "RoleInheritance": "BASELINE_ROLE_INHERITED_UNLESS_CHANGEPOINT_OVERRIDES",
                "RuntimePolicy": "REFERENCE_DOES_NOT_FORCE_RUNTIME_SEAT",
            }
        )


# ---------------------------------------------------------------------------
# 250 priority counties and important place references
# ---------------------------------------------------------------------------

transport_by_name = defaultdict(list)
for row in deep["transport_nodes"]:
    transport_by_name[row["name"]].append(row["transport_id"])
military_by_city = defaultdict(list)
for row in deep["military_spaces"]:
    for city_id in str(row.get("related_city_ids") or "").split("|"):
        if city_id:
            military_by_city[city_id].append(row["military_space_id"])

priority_place_rows = []
for county in priority_counties:
    place = core_by_county.get(county["county_id"])
    city_ids = place["city_ids"].split("|") if place and place["city_ids"] else []
    priority_place_rows.append(
        {
            "CountyId": county["county_id"],
            "CountyName": county["display_name"],
            "ProvinceId": county["province_id"],
            "CommanderyEquivalentId": county["commandery_id"],
            "Priority": county["priority"],
            "CountySeatPlaceId": place["place_id"] if place else "",
            "CountySeatName": place["display_name"] if place else "UNKNOWN_NOT_RESEARCHED",
            "ImportantSecondSettlement": "UNKNOWN_NOT_RESEARCHED",
            "ImportantPass/Ford/Harbor": unique_pipe(transport_by_name.get(county["display_name"], [])),
            "ImportantEstate": unique_pipe(row["estate_reference_id"] for row in deep["estate_references"] if row["county_id"] == county["county_id"]),
            "BattlefieldReference": unique_pipe(item for city_id in city_ids for item in military_by_city.get(city_id, [])),
            "StrategicLabelIds": pipe(city_ids),
            "SeatReferenceStatus": "RESOLVED_TO_CORE_SETTLEMENT" if place else "UNKNOWN_NOT_RESEARCHED",
            "Evidence": county["evidence_type"],
            "DevelopmentStatus": county["development_status"],
            "NoPlaceEqualsCounty": "TRUE",
        }
    )


# ---------------------------------------------------------------------------
# Historical major change points and reference packages
# ---------------------------------------------------------------------------

P = {
    "LUOYANG": "place.han140.sili.henan.luoyang",
    "CHANGAN": "place.han140.sili.jingzhao.changan",
    "YE": "place.han140.jizhou.wei.ye",
    "XU": "place.han140.yuzhou.yingchuan.xu",
    "CHENGDU": "place.han140.yizhou.shu.chengdu",
    "XIANGYANG": "place.han140.jingzhou.nan.xiangyang",
    "JIANGLING": "place.han140.jingzhou.nan.jiangling",
    "JIANYE": "place.han140.yangzhou.danyang.moling",
    "HANZHONG": "place.han140.yizhou.hanzhong.nanzheng",
    "WAN": "place.han140.jingzhou.nanyang.wan",
    "SHOUCHUN": "place.han140.yangzhou.jiujiang.shouchun",
    "HEFEI": "place.han140.yangzhou.jiujiang.hefei",
    "JIANGXIA": "place.han140.jingzhou.jiangxia.xiling",
    "WUCHANG": "place.han140.jingzhou.jiangxia.e",
    "YONGAN": "place.han140.yizhou.ba.yufu",
}


def cp(key, place, event, window, change_type, scenario, map_impact, population, facility, admin, person, family, military, transport, evidence, priority):
    return {
        "ChangePointId": key,
        "PlaceId / RegionId": place,
        "EventId": event,
        "TimeWindow": window,
        "ChangeType": change_type,
        "PreStateRef": f"pre.{key}",
        "CanonicalChangePackageRef": f"package.{key}",
        "PostStateRef": f"post.{key}",
        "ScenarioRelevance": scenario,
        "MapImpact": map_impact,
        "PopulationImpact": population,
        "FacilityImpact": facility,
        "AdministrativeImpact": admin,
        "PersonImpact": person,
        "FamilyImpact": family,
        "MilitaryImpact": military,
        "TransportImpact": transport,
        "Evidence": evidence,
        "Priority": priority,
        "RuntimePolicy": "PRECONDITIONED_CANONICAL_VARIANT_PREVENTED_TRANSFORMED",
    }


change_points = [
    cp("change.luoyang.184.mobilization", P["LUOYANG"], "event.yellow_turban.capital_mobilization", "184", "MILITARY_MOBILIZATION", "184", "LOW", "LOW", "LOW", "LOW", "MEDIUM", "LOW", "HIGH", "MEDIUM", "source.primary.hou_han_shu.huangfu_song", "P1"),
    cp("change.luoyang.189.court_crisis", P["LUOYANG"], "event.189.court_coup", "189", "COURT_CONTROL_CRISIS", "189|194", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "HIGH", "HIGH", "HIGH", "LOW", "source.primary.hou_han_shu.xiandi", "P0"),
    cp("change.luoyang.190.relocation_burning", P["LUOYANG"], "event.190.capital_relocation_burning", "190 (exact facility impacts vary by evidence)", "CAPITAL_RELOCATION_FORCED_MIGRATION_URBAN_DESTRUCTION", "194|200|later", "VERY_HIGH", "VERY_HIGH", "VERY_HIGH", "VERY_HIGH", "VERY_HIGH", "VERY_HIGH", "HIGH", "HIGH", "source.primary.hou_han_shu.xiandi", "P0"),
    cp("change.luoyang.220.wei_capital", P["LUOYANG"], "event.220.wei_capital_restoration", "220-223", "CAPITAL_RESTORATION", "223", "HIGH", "HIGH", "HIGH", "HIGH", "HIGH", "MEDIUM", "HIGH", "HIGH", "source.primary.san_guo_zhi.wei", "P0"),
    cp("change.changan.190.court_arrival", P["CHANGAN"], "event.190.capital_relocation", "190", "CAPITAL_AND_COURT_ARRIVAL", "194", "HIGH", "VERY_HIGH", "HIGH", "VERY_HIGH", "VERY_HIGH", "HIGH", "HIGH", "HIGH", "source.primary.hou_han_shu.xiandi", "P0"),
    cp("change.changan.192.control_crisis", P["CHANGAN"], "event.192.dong_zhuo_death", "192-195", "CONTROL_AND_URBAN_CRISIS", "194|200", "HIGH", "HIGH", "HIGH", "HIGH", "HIGH", "MEDIUM", "VERY_HIGH", "HIGH", "source.primary.hou_han_shu.xiandi", "P0"),
    cp("change.changan.195_196.court_departure", P["CHANGAN"], "event.195_196.court_eastward", "195-196", "COURT_DEPARTURE", "200", "MEDIUM", "HIGH", "MEDIUM", "VERY_HIGH", "VERY_HIGH", "MEDIUM", "HIGH", "HIGH", "source.primary.hou_han_shu.xiandi", "P0"),
    cp("change.ye.204.cao_control", P["YE"], "event.204.ye_control", "204", "CONTROL_AND_GOVERNMENT_CENTER", "207|214", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "HIGH", "MEDIUM", "HIGH", "MEDIUM", "source.primary.san_guo_zhi.wei", "P0"),
    cp("change.ye.210.tongque_construction", P["YE"], "event.210.tongque_construction", "210-214", "MAJOR_FACILITY_CONSTRUCTION", "214|219", "HIGH", "MEDIUM", "VERY_HIGH", "MEDIUM", "MEDIUM", "LOW", "MEDIUM", "MEDIUM", "source.primary.san_guo_zhi.wei", "P0"),
    cp("change.ye.220.wei_transition", P["YE"], "event.220.wei_transition", "220", "POLITY_AND_CAPITAL_ROLE_CHANGE", "223", "MEDIUM", "LOW", "MEDIUM", "HIGH", "HIGH", "MEDIUM", "HIGH", "LOW", "source.primary.san_guo_zhi.wei", "P1"),
    cp("change.xu.196.court_arrival", P["XU"], "event.196.imperial_court_arrival", "196", "CAPITAL_AND_COURT_ARRIVAL", "200|207|214|219", "HIGH", "HIGH", "HIGH", "VERY_HIGH", "VERY_HIGH", "HIGH", "HIGH", "HIGH", "source.primary.hou_han_shu.xiandi", "P0"),
    cp("change.xu.220.han_wei_transition", P["XU"], "event.220.han_wei_transition", "220-221", "CAPITAL_ROLE_AND_RENAME", "223", "MEDIUM", "LOW", "MEDIUM", "HIGH", "HIGH", "LOW", "MEDIUM", "LOW", "source.primary.san_guo_zhi.wei", "P0"),
    cp("change.chengdu.188.yizhou_seat", P["CHENGDU"], "event.188.yizhou_seat_shift", "188-190", "PROVINCE_SEAT_AND_GOVERNMENT_MOVE", "189|194", "MEDIUM", "MEDIUM", "HIGH", "HIGH", "MEDIUM", "MEDIUM", "MEDIUM", "MEDIUM", "source.reference.han_province_seats", "P1"),
    cp("change.chengdu.214.takeover", P["CHENGDU"], "event.214.chengdu_takeover", "214", "CONTROL_TRANSFER", "214|219", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "HIGH", "HIGH", "HIGH", "LOW", "source.primary.san_guo_zhi.shu", "P0"),
    cp("change.chengdu.221.shu_capital", P["CHENGDU"], "event.221.shu_han_capital", "221-223", "CAPITAL_AND_POLITY_CENTER", "223|227", "HIGH", "MEDIUM", "HIGH", "VERY_HIGH", "HIGH", "HIGH", "HIGH", "MEDIUM", "source.primary.san_guo_zhi.shu", "P0"),
    cp("change.xiangyang.190.liu_biao_seat", P["XIANGYANG"], "event.190.jingzhou_seat_xiangyang", "190", "PROVINCE_SEAT_AND_REGIONAL_CENTER", "194|200|207", "HIGH", "MEDIUM", "HIGH", "HIGH", "HIGH", "MEDIUM", "HIGH", "HIGH", "source.primary.hou_han_shu.liubiao", "P0"),
    cp("change.xiangyang.208.takeover", P["XIANGYANG"], "event.208.jingzhou_takeover", "208", "CONTROL_AND_FORCE_TRANSITION", "214", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "HIGH", "MEDIUM", "VERY_HIGH", "HIGH", "source.primary.san_guo_zhi.wei", "P0"),
    cp("change.xiangyang.219.fancheng_campaign", P["XIANGYANG"], "event.219.fancheng_campaign", "219", "SIEGE_REGION_AND_TRANSPORT_DISRUPTION", "219|223", "HIGH", "HIGH", "HIGH", "MEDIUM", "HIGH", "MEDIUM", "VERY_HIGH", "VERY_HIGH", "source.primary.san_guo_zhi.wei", "P0"),
    cp("change.jiangling.208_209.control", P["JIANGLING"], "event.208_209.jiangling_control", "208-209", "CONTROL_AND_GARRISON_TRANSITION", "214", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "HIGH", "MEDIUM", "VERY_HIGH", "HIGH", "source.primary.san_guo_zhi.wu", "P0"),
    cp("change.jiangling.219.takeover", P["JIANGLING"], "event.219.jingzhou_takeover", "219", "CONTROL_AND_LOGISTICS_TRANSITION", "219|223", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "VERY_HIGH", "HIGH", "VERY_HIGH", "HIGH", "source.primary.san_guo_zhi.wu", "P0"),
    cp("change.jiangling.222.yiling_after", P["JIANGLING"], "event.222.yiling_after", "222-223", "REGIONAL_MILITARY_POSTSTATE", "223", "MEDIUM", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "MEDIUM", "VERY_HIGH", "HIGH", "source.primary.san_guo_zhi.wu", "P1"),
    cp("change.jianye.211_212.rename_center", P["JIANYE"], "event.211_212.moling_jianye", "211-212", "NAME_TIMELINE_AND_POLITICAL_CENTER", "214|219", "HIGH", "HIGH", "HIGH", "HIGH", "HIGH", "HIGH", "HIGH", "HIGH", "source.primary.san_guo_zhi.wu", "P0"),
    cp("change.jianye.229.wu_capital", P["JIANYE"], "event.229.wu_capital", "229", "CAPITAL_AND_POLITY_CENTER", "234|249|260", "HIGH", "HIGH", "HIGH", "VERY_HIGH", "HIGH", "HIGH", "HIGH", "HIGH", "source.primary.san_guo_zhi.wu", "P0"),
    cp("change.hanzhong.215.control", P["HANZHONG"], "event.215.hanzhong_control", "215", "CONTROL_AND_LOGISTICS_TRANSITION", "219", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "HIGH", "MEDIUM", "VERY_HIGH", "VERY_HIGH", "source.primary.san_guo_zhi.wei", "P1"),
    cp("change.hanzhong.219.takeover", P["HANZHONG"], "event.219.hanzhong_takeover", "219", "CONTROL_AND_FORCE_TRANSITION", "219|223", "MEDIUM", "HIGH", "MEDIUM", "HIGH", "HIGH", "MEDIUM", "VERY_HIGH", "VERY_HIGH", "source.primary.san_guo_zhi.shu", "P0"),
    cp("change.wan.197_199.war", P["WAN"], "event.197_199.wan_war", "197-199", "URBAN_WAR_AND_CONTROL", "200", "HIGH", "HIGH", "HIGH", "HIGH", "HIGH", "MEDIUM", "VERY_HIGH", "HIGH", "source.primary.san_guo_zhi.wei", "P1"),
    cp("change.shouchun.197.zhong_regime", P["SHOUCHUN"], "event.197.zhong_regime", "197-199", "POLITY_CENTER_AND_WAR", "200", "MEDIUM", "HIGH", "MEDIUM", "HIGH", "HIGH", "MEDIUM", "VERY_HIGH", "HIGH", "source.primary.san_guo_zhi.wei", "P1"),
    cp("change.hefei.208_215.fortification", P["HEFEI"], "event.208_215.hefei_defense", "208-215", "FORTIFICATION_AND_GARRISON", "214|219", "HIGH", "MEDIUM", "HIGH", "MEDIUM", "MEDIUM", "LOW", "VERY_HIGH", "HIGH", "source.primary.san_guo_zhi.wei", "P1"),
    cp("change.jiangxia.208.seat_control", P["JIANGXIA"], "event.208.jiangxia_fragmentation", "208-210", "MOVING_SEAT_AND_CONTROL_FRAGMENTATION", "214", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "HIGH", "MEDIUM", "HIGH", "VERY_HIGH", "source.primary.san_guo_zhi.wu", "P1"),
    cp("change.wuchang.221.capital", P["WUCHANG"], "event.221.wuchang_capital", "221", "RENAME_AND_CAPITAL", "223|227", "HIGH", "HIGH", "HIGH", "VERY_HIGH", "HIGH", "HIGH", "HIGH", "HIGH", "source.primary.san_guo_zhi.wu", "P0"),
    cp("change.wuchang.229.capital_departure", P["WUCHANG"], "event.229.capital_to_jianye", "229", "CAPITAL_ROLE_TRANSFER", "234", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "HIGH", "MEDIUM", "HIGH", "HIGH", "source.primary.san_guo_zhi.wu", "P1"),
    cp("change.yongan.222_223.court", P["YONGAN"], "event.222_223.yongan_court", "222-223", "RENAME_IMPERIAL_RESIDENCE_AND_SUCCESSION", "223", "HIGH", "HIGH", "HIGH", "HIGH", "VERY_HIGH", "HIGH", "HIGH", "VERY_HIGH", "source.primary.san_guo_zhi.shu", "P0"),
]

change_by_place = defaultdict(list)
for row in change_points:
    change_by_place[row["PlaceId / RegionId"]].append(row["ChangePointId"])

change_packages = []
for change in change_points:
    place_id = change["PlaceId / RegionId"]
    base_ops = [
        ("PLACE", place_id, "APPLY_WORLD_STATE_OVERLAY_REFERENCE", "Use the existing Place/Cell IDs; no duplicate map."),
    ]
    if change["AdministrativeImpact"] not in ("LOW", "NONE"):
        base_ops.append(("PLACE", place_id, "REVIEW_RUNTIME_ADMINISTRATIVE_SEAT", "Historical seat reference may initialize a direct Scenario only."))
    if change["PopulationImpact"] not in ("LOW", "NONE"):
        base_ops.append(("PLACE", place_id, "APPLY_POPULATION_AND_HOUSEHOLD_LEDGER_REFERENCE", "Reuse permanent Persons and Households; do not initialize a second population."))
    if change["FacilityImpact"] not in ("LOW", "NONE"):
        base_ops.append(("PLACE", place_id, "APPLY_VERIFIED_FACILITY_LIFECYCLE_REFERENCE", "Only verified Facility IDs can receive concrete lifecycle changes."))
    if change["TransportImpact"] not in ("LOW", "NONE"):
        base_ops.append(("PLACE", place_id, "APPLY_TRANSPORT_STATE_REFERENCE", "Road/bridge/ford/harbor targets require stable IDs before runtime implementation."))
    for index, (entity_type, entity_id, operation, note) in enumerate(base_ops, 1):
        change_packages.append(
            {
                "PackageId": f'{change["CanonicalChangePackageRef"]}.{index:02d}',
                "ChangePointId": change["ChangePointId"],
                "TargetEntityType": entity_type,
                "TargetEntityId": entity_id,
                "ChangeOperation": operation,
                "PreconditionReference": f'precondition.{change["ChangePointId"]}',
                "CanonicalChange": note,
                "PostExpectedState": change["PostStateRef"],
                "EvidenceLevel": "RECONSTRUCTED_REFERENCE",
                "ModelingRequired": "YES",
                "RuntimeImplementationStatus": "REFERENCE_ONLY_NOT_IMPLEMENTED",
                "Notes": "Canonical/Variant/Prevented/Transformed; never force overwrite a divergent runtime world.",
            }
        )

luoyang_190_key_facilities = [
    "facility.instance.luoyang.184.north_palace",
    "facility.instance.luoyang.184.south_palace",
    "facility.instance.luoyang.184.central_offices_east",
    "facility.instance.luoyang.184.central_offices_west",
    "facility.instance.luoyang.184.taicang",
    "facility.instance.luoyang.184.arsenal",
    "facility.instance.luoyang.184.jinshi",
    "facility.instance.luoyang.184.nanshi",
    "facility.instance.luoyang.184.taixue",
]
for index, facility_id in enumerate(luoyang_190_key_facilities, 20):
    change_packages.append(
        {
            "PackageId": f"package.change.luoyang.190.relocation_burning.{index:02d}",
            "ChangePointId": "change.luoyang.190.relocation_burning",
            "TargetEntityType": "FACILITY",
            "TargetEntityId": facility_id,
            "ChangeOperation": "FACILITY_LIFECYCLE_EVIDENCE_REVIEW",
            "PreconditionReference": "precondition.change.luoyang.190.relocation_burning",
            "CanonicalChange": "Do not mark a specific Facility destroyed unless evidence supports it; otherwise use MODELED/UNKNOWN state.",
            "PostExpectedState": "post.change.luoyang.190.relocation_burning",
            "EvidenceLevel": "HISTORICAL_EVENT+MODELED_TARGET",
            "ModelingRequired": "YES",
            "RuntimeImplementationStatus": "REFERENCE_ONLY_NOT_IMPLEMENTED",
            "Notes": "Same FacilityPermanentId where lifecycle continuity is plausible; never force overwrite a divergent runtime world.",
        }
    )


# ---------------------------------------------------------------------------
# Scenario snapshot index
# ---------------------------------------------------------------------------

scenario_snapshots = []
for place in core:
    family = family_by_place.get(place["place_id"], {})
    for year in SCENARIO_YEARS:
        priority = priority_by_county.get(place["county_id"], {})
        pop_reference = "UNKNOWN"
        if year == 140 and priority.get("population_140_modeled") is not None:
            pop_reference = f'MODELED_COUNTY_POPULATION:{priority["population_140_modeled"]}'
        elif year == 184 and priority.get("population_184_modeled") is not None:
            pop_reference = f'MODELED_COUNTY_POPULATION:{priority["population_184_modeled"]}'
        facility_ref = "LUOYANG184_FORMAL_FACILITY_SET" if place["place_id"] == P["LUOYANG"] and year == 184 else "REFERENCE_REQUIRED_NO_RUNTIME_INSTANCE"
        scenario_snapshots.append(
            {
                "ScenarioYear": year,
                "PlaceId": place["place_id"],
                "SnapshotId": f'snapshot.{stable_hash(place["place_id"])}.{year}',
                "AdministrativeState": pipe(roles_for_place(place, year)),
                "PopulationReference": pop_reference,
                "FacilityStateReference": facility_ref,
                "PersonReference": family.get("HistoricalPersonIds", ""),
                "FamilyReference": family.get("HistoricalClanIds", ""),
                "MilitaryReference": unique_pipe(item for city_id in place["city_ids"].split("|") if city_id for item in military_by_city.get(city_id, [])),
                "TransportReference": "INHERIT_EXISTING_TRANSPORT_REFERENCE",
                "UrbanStateReference": "SAME_CANONICAL_CELLS_STATE_OVERLAY_ONLY",
                "ChangePointReferences": unique_pipe(change_by_place.get(place["place_id"], [])),
                "EvidenceCoverage": place["evidence_coverage"],
                "DevelopmentReadiness": "R5_READY" if place["reference_level"] == "R5" else "R4_ADVANCED" if place["reference_level"] == "R4" else "R3_INDEX",
                "DirectScenarioOnly": "YES",
                "ContinuousPlayPolicy": "USE_RUNTIME_WORLD_NOT_THIS_SNAPSHOT",
            }
        )


# ---------------------------------------------------------------------------
# Series importance reference (legal abstract level only)
# ---------------------------------------------------------------------------

series_cross = []
for strategic in strategic_crosswalk:
    city_id = strategic["StrategicLabelId"]
    city = city_by_id[city_id]
    series_cross.append(
        {
            "ReferenceName": strategic["StrategicDisplayName"],
            "VII": "NOT_LICENSE_COMPATIBLY_AUDITED",
            "VIII": "NOT_LICENSE_COMPATIBLY_AUDITED",
            "X": "NOT_LICENSE_COMPATIBLY_AUDITED",
            "XI": "NOT_LICENSE_COMPATIBLY_AUDITED",
            "XII": "NOT_LICENSE_COMPATIBLY_AUDITED",
            "XIII": "NOT_LICENSE_COMPATIBLY_AUDITED",
            "XIV": "NOT_LICENSE_COMPATIBLY_AUDITED",
            "SeriesAppearanceCount": 0,
            "ReferenceRole": strategic["RelationType"],
            "ProjectCanonicalPlace": strategic["CanonicalPlaceId"],
            "ProjectAdministrativeRegion": strategic["AdministrativeRegionId"],
            "ProjectInterpretation": strategic["DevelopmentInterpretation"],
            "SeriesImportance": "LEGACY_77_STRATEGIC_LABEL_REFERENCE",
            "HistoricalImportance": core_by_city[city_id]["reference_level"],
            "DevelopmentValue": core_by_city[city_id]["priority"],
            "NeedsFurtherResearch": "YES_PER_TITLE_APPEARANCE_REQUIRES_LEGAL_PUBLIC_SOURCE",
            "LegalBoundary": "No commercial coordinates, database, numbers, UI, assets or script text imported.",
            "ProjectSource": city["source_ids"],
        }
    )


# ---------------------------------------------------------------------------
# Development-relevant candidate master (candidate != approved roster)
# ---------------------------------------------------------------------------

candidates = []
for place in core:
    candidates.append(
        {
            "CandidateId": place["place_id"],
            "CandidateName": place["display_name"],
            "CandidateType": "Settlement",
            "CanonicalPlaceId": place["place_id"],
            "AdministrativeRegionId": place["county_id"],
            "StableGeographyReference": pipe([place["longitude"], place["latitude"], place["coordinate_status"]]),
            "DevelopmentImportance": place["priority"],
            "ImportanceReasons": unique_pipe(["AdministrativeSeat" if place["is_commandery_seat"] else "", "StrategicLabel" if place["is_strategic_city"] else "", "CoreSettlement"]),
            "HistoricalEvidence": place["evidence_type"],
            "Map/FacilityResearchNeed": "Facility composition, urban extent, transport and lifecycle",
            "RosterDecision": "OPEN_NOT_DECIDED_IN_THIS_TASK",
            "SourceIds": place["source_ids"],
        }
    )
for row in deep["transport_nodes"]:
    candidates.append(
        {
            "CandidateId": f'candidate.transport.{row["transport_id"]}',
            "CandidateName": row["name"],
            "CandidateType": "TransportNodeOrCorridor",
            "CanonicalPlaceId": "",
            "AdministrativeRegionId": row.get("parent_location") or "",
            "StableGeographyReference": pipe([row.get("longitude"), row.get("latitude"), row.get("geometry_status")]),
            "DevelopmentImportance": "P1" if row["transport_type"] in ("pass", "harbor", "ford") else "P2",
            "ImportanceReasons": pipe([row["transport_type"], row["development_implication"]]),
            "HistoricalEvidence": row["evidence_type"],
            "Map/FacilityResearchNeed": "Determine whether this is a stable Place, multi-Cell corridor, or Facility-bearing node.",
            "RosterDecision": "OPEN_NOT_DECIDED_IN_THIS_TASK",
            "SourceIds": row["source_ids"],
        }
    )
for row in deep["military_spaces"]:
    candidates.append(
        {
            "CandidateId": f'candidate.military.{row["military_space_id"]}',
            "CandidateName": row["name"],
            "CandidateType": "BattlefieldRegion" if row["space_type"] == "campaign" else "MilitarySpace",
            "CanonicalPlaceId": "",
            "AdministrativeRegionId": row["related_city_ids"],
            "StableGeographyReference": row["geometry_status"],
            "DevelopmentImportance": "P1",
            "ImportanceReasons": row["development_role"],
            "HistoricalEvidence": row["evidence_type"],
            "Map/FacilityResearchNeed": "Resolve settlement/place/region classification; do not create a fictional city.",
            "RosterDecision": "OPEN_NOT_DECIDED_IN_THIS_TASK",
            "SourceIds": row["source_id"],
        }
    )
for feature in strategic_sites:
    prop = feature["properties"]
    candidates.append(
        {
            "CandidateId": prop["site_id"],
            "CandidateName": prop["name"],
            "CandidateType": prop["site_type"],
            "CanonicalPlaceId": "",
            "AdministrativeRegionId": prop.get("parent_location_id") or "",
            "StableGeographyReference": pipe([prop.get("longitude"), prop.get("latitude"), prop.get("coordinate_status")]),
            "DevelopmentImportance": "P1",
            "ImportanceReasons": prop.get("notes") or "Strategic geography candidate",
            "HistoricalEvidence": "MODELED" if prop.get("provisional") == "true" else "RECONSTRUCTED",
            "Map/FacilityResearchNeed": "Terrain/road/chokepoint/facility evidence before CanonicalPlace promotion.",
            "RosterDecision": "OPEN_NOT_DECIDED_IN_THIS_TASK",
            "SourceIds": "source.project.master_map.v0",
        }
    )
for row in deep["estate_references"]:
    candidates.append(
        {
            "CandidateId": f'candidate.estate.{row["estate_reference_id"]}',
            "CandidateName": row["historical_description"][:32],
            "CandidateType": "EstateCandidate",
            "CanonicalPlaceId": "",
            "AdministrativeRegionId": row["county_id"],
            "StableGeographyReference": "UNKNOWN_WITHIN_COUNTY",
            "DevelopmentImportance": "P2",
            "ImportanceReasons": "Historical estate/asset evidence",
            "HistoricalEvidence": row["evidence_level"],
            "Map/FacilityResearchNeed": row["unknowns"],
            "RosterDecision": "OPEN_NOT_DECIDED_IN_THIS_TASK",
            "SourceIds": row["source_id"],
        }
    )


# ---------------------------------------------------------------------------
# Luoyang historical-state reference package
# ---------------------------------------------------------------------------

luoyang_timeline = [
    {"StateId": "state.luoyang.184.baseline", "TimeWindow": "184 pre-crisis baseline", "PlaceId": P["LUOYANG"], "AdministrativeRole": "ImperialCapital/central government", "PopulationState": "Use 184 permanent population initialization reference", "FacilityState": "Luoyang184 same-ID Facility baseline", "ControllerState": "Han central government/garrison reference", "UrbanState": "Urbanized baseline", "InheritedFrom": "140 reference + verified 184 changes", "Evidence": "HISTORICAL+RECONSTRUCTED+MODELED"},
    {"StateId": "state.luoyang.184.mobilized", "TimeWindow": "184 Yellow Turban crisis", "PlaceId": P["LUOYANG"], "AdministrativeRole": "ImperialCapital", "PopulationState": "No second initialization; military/security movements only", "FacilityState": "Gates/barracks/arsenal operational pressure", "ControllerState": "Han central government", "UrbanState": "No general destruction assumed", "InheritedFrom": "state.luoyang.184.baseline", "Evidence": "HISTORICAL_EVENT+RECONSTRUCTED"},
    {"StateId": "state.luoyang.189.pre_coup", "TimeWindow": "189 before court crisis", "PlaceId": P["LUOYANG"], "AdministrativeRole": "ImperialCapital", "PopulationState": "Inherited, adjusted by real world evolution", "FacilityState": "Same facility IDs", "ControllerState": "Court factions", "UrbanState": "Inherited", "InheritedFrom": "latest valid prior state", "Evidence": "RECONSTRUCTED"},
    {"StateId": "state.luoyang.189.post_coup", "TimeWindow": "189 after court crisis", "PlaceId": P["LUOYANG"], "AdministrativeRole": "ImperialCapital under changed control", "PopulationState": "Person/household consequences; no re-randomization", "FacilityState": "Palace/government control changes; exact damage by evidence", "ControllerState": "Historical/variant controller", "UrbanState": "No blanket city damage value", "InheritedFrom": "state.luoyang.189.pre_coup + change package", "Evidence": "HISTORICAL+RECONSTRUCTED"},
    {"StateId": "state.luoyang.190.pre_relocation", "TimeWindow": "190 before relocation/burning", "PlaceId": P["LUOYANG"], "AdministrativeRole": "ImperialCapital pending relocation", "PopulationState": "Existing Persons/Households", "FacilityState": "Same Facility IDs before event", "ControllerState": "Event precondition dependent", "UrbanState": "Pre-event state", "InheritedFrom": "state.luoyang.189.post_coup", "Evidence": "RECONSTRUCTED"},
    {"StateId": "state.luoyang.190.post_relocation", "TimeWindow": "190 after canonical relocation/burning", "PlaceId": P["LUOYANG"], "AdministrativeRole": "Capital role removed in canonical outcome", "PopulationState": "Large forced migration/flight/death/stayers; exact quantities not fabricated", "FacilityState": "Verified targets damaged/destroyed/abandoned/looted; ordinary structures modeled or unknown", "ControllerState": "Changed military/political control", "UrbanState": "Mixed ruined/abandoned/surviving Cells", "InheritedFrom": "state.luoyang.190.pre_relocation + canonical/variant package", "Evidence": "HISTORICAL_EVENT+MODELED_DETAIL"},
    {"StateId": "state.luoyang.220_223.wei_capital", "TimeWindow": "220-223 Wei capital restoration", "PlaceId": P["LUOYANG"], "AdministrativeRole": "Cao Wei capital", "PopulationState": "Runtime/history snapshot population, not a recycled 184 cohort", "FacilityState": "Rebuilt/repurposed/new facilities require lifecycle decisions", "ControllerState": "Cao Wei", "UrbanState": "Partial restoration/renewed urbanization", "InheritedFrom": "post-190 state + intervening runtime/history", "Evidence": "HISTORICAL+RECONSTRUCTED"},
]

luoyang_prepost = [
    {"Domain": "Population", "Pre190Reference": "Existing permanent Persons and Households", "CanonicalPost190Reference": "Large-scale forced migration, voluntary flight, deaths and remaining population", "EvidenceLevel": "HISTORICAL_EVENT; magnitude UNKNOWN", "AuthoritativeTarget": P["LUOYANG"], "DoNot": "Do not generate a second population or hardcode unsupported exact percentages"},
    {"Domain": "ImperialHousehold", "Pre190Reference": "Imperial household present in Luoyang", "CanonicalPost190Reference": "Movement toward Chang'an according to surviving Persons/organizations", "EvidenceLevel": "HISTORICAL", "AuthoritativeTarget": "PermanentPersonId/FamilyOrganizationId", "DoNot": "Do not duplicate people or convert palace ownership into clan ownership"},
    {"Domain": "CentralGovernment", "Pre190Reference": "Central offices in Luoyang", "CanonicalPost190Reference": "Government/court relocation; runtime seat changes only if event conditions are met", "EvidenceLevel": "HISTORICAL", "AuthoritativeTarget": "Government Facility + Office + Controller", "DoNot": "Do not force a seat change by year alone"},
    {"Domain": "Palaces", "Pre190Reference": "North/South palace Facility IDs exist", "CanonicalPost190Reference": "Damage/destruction/abandonment at component level only where supported", "EvidenceLevel": "HISTORICAL_EVENT+MODELED_COMPONENT", "AuthoritativeTarget": "FacilityPermanentId", "DoNot": "Do not set all palace components to HISTORICAL_DESTROYED"},
    {"Domain": "Markets/Warehouses", "Pre190Reference": "Markets, Taicang and arsenal exist", "CanonicalPost190Reference": "Inventory transfer/looting plus facility condition review", "EvidenceLevel": "RECONSTRUCTED", "AuthoritativeTarget": "FacilityPermanentId + Inventory ledger", "DoNot": "Do not use one aggregate damage percentage"},
    {"Domain": "Walls/Gates/Roads", "Pre190Reference": "Same Cell and fortification/road IDs", "CanonicalPost190Reference": "Surviving/damaged/blocked states remain itemized and evidence-bounded", "EvidenceLevel": "UNKNOWN/MODELED unless specific evidence", "AuthoritativeTarget": "Cell/Facility/Road IDs", "DoNot": "Do not duplicate a 190 map"},
    {"Domain": "FamilySpatialState", "Pre190Reference": "Existing family presence/assets/center candidates", "CanonicalPost190Reference": "Local center lost/disabled only when real Facility ownership/control changes", "EvidenceLevel": "RECONSTRUCTED", "AuthoritativeTarget": "FamilyOrganization + Facility", "DoNot": "Do not delete clans, members or all assets"},
]

facility_by_id = {row["facility_id"]: row for row in luoyang_facilities}
key_facility_ids = luoyang_190_key_facilities + [
    row["facility_id"] for row in luoyang_facilities
    if row["facility_id"].startswith("facility.instance.luoyang.184.gate.")
]
luoyang_facility_lifecycle = []
for facility_id in key_facility_ids:
    facility = facility_by_id.get(facility_id)
    if not facility:
        continue
    if facility["definition_id"] in ("facility.government.court_hall", "facility.historical.central_office"):
        post = "MODELED_DAMAGED_DESTROYED_OR_ABANDONED; component evidence required"
    elif facility["definition_id"] in ("facility.storage.granary", "facility.storage.warehouse", "facility.commercial.market"):
        post = "INVENTORY_TRANSFER/LOOTING_REFERENCE; physical condition UNKNOWN"
    else:
        post = "CONDITION_UNKNOWN; do not assume all gates destroyed"
    luoyang_facility_lifecycle.append(
        {
            "FacilityPermanentId": facility_id,
            "DefinitionId": facility["definition_id"],
            "DisplayName": facility["display_name"],
            "184State": "EXISTING",
            "190CanonicalPostReference": post,
            "LifecycleContinuity": "PRESERVE_SAME_ID_UNLESS_TRUE_REBUILD_IS_PROVEN",
            "HistoricalConfidence184": facility["historical_confidence"],
            "Post190Evidence": "HISTORICAL_EVENT+MODELED_OR_UNKNOWN_TARGET",
            "RuntimeImplementationStatus": "REFERENCE_ONLY",
        }
    )

luoyang_population_migration = [
    {"MovementReferenceId": "migration.luoyang.190.imperial_household", "PopulationGroup": "Imperial household", "OriginPlaceId": P["LUOYANG"], "DestinationReference": P["CHANGAN"], "MovementType": "FORCED/COURT_RELOCATION", "Magnitude": "PERSON_LEVEL_REFERENCE", "Evidence": "HISTORICAL", "PermanentIdentityRule": "Reuse PermanentPersonId", "Unknowns": "Exact household and retinue membership"},
    {"MovementReferenceId": "migration.luoyang.190.central_government", "PopulationGroup": "Central officials and service households", "OriginPlaceId": P["LUOYANG"], "DestinationReference": P["CHANGAN"], "MovementType": "GOVERNMENT_RELOCATION", "Magnitude": "LARGE_UNQUANTIFIED", "Evidence": "HISTORICAL+RECONSTRUCTED", "PermanentIdentityRule": "Reuse Persons/Households/Organizations", "Unknowns": "Exact roster and routes"},
    {"MovementReferenceId": "migration.luoyang.190.urban_households", "PopulationGroup": "Urban households", "OriginPlaceId": P["LUOYANG"], "DestinationReference": "Multiple westward and refuge destinations", "MovementType": "FORCED_MIGRATION/FLIGHT", "Magnitude": "LARGE_UNQUANTIFIED", "Evidence": "HISTORICAL_EVENT", "PermanentIdentityRule": "Move existing households; no replacement population", "Unknowns": "Counts, household selection and destinations"},
    {"MovementReferenceId": "migration.luoyang.190.deaths", "PopulationGroup": "Civilian and military deaths", "OriginPlaceId": P["LUOYANG"], "DestinationReference": "N/A", "MovementType": "DEATH_LEDGER", "Magnitude": "UNKNOWN", "Evidence": "UNKNOWN_AT_PERSON_LEVEL", "PermanentIdentityRule": "Death archives remain permanent", "Unknowns": "Exact persons and causes"},
    {"MovementReferenceId": "migration.luoyang.190.stayers", "PopulationGroup": "Remaining/returning residents", "OriginPlaceId": P["LUOYANG"], "DestinationReference": P["LUOYANG"], "MovementType": "STAY/RETURN", "Magnitude": "UNKNOWN", "Evidence": "MODELED_REQUIRED", "PermanentIdentityRule": "Continue same Persons/Households", "Unknowns": "Neighborhood-level survival and return timing"},
]

location_by_person = defaultdict(list)
for row in person_locations:
    location_by_person[row["person_id"]].append(row)
luoyang_person_family = []
for item in luoyang_people:
    person = person_by_id.get(item["person_id"], {})
    later = [row for row in location_by_person.get(item["person_id"], []) if row["start_year"] >= 189]
    luoyang_person_family.append(
        {
            "PermanentPersonId": item["person_id"],
            "CanonicalName": person.get("canonical_name", item.get("canonical_name", "")),
            "184Reference": "Luoyang historical-person initialization",
            "189_196MovementReferences": unique_pipe(f'{row["start_year"]}:{row["historical_location_text"]}' for row in later),
            "ClanId": person.get("clan_id") or "",
            "BranchId": person.get("branch_id") or "",
            "FamilyOrganizationRule": "Do not duplicate or infer organization membership from surname alone",
            "Evidence": unique_pipe(row["evidence_level"] for row in later) or "UNKNOWN_AFTER_184",
            "RuntimePolicy": "Same PermanentPersonId across all Scenarios",
        }
    )


# ---------------------------------------------------------------------------
# Knowledge-base registry updates
# ---------------------------------------------------------------------------

registry_document_paths = [
    "Docs/TASK_HAN_135_260_ADMINISTRATIVE_SEAT_CANONICAL_PLACE_AND_HISTORICAL_WORLD_STATE_V1.md",
    "Docs/HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md",
    "Docs/HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/HAN_135_260_ADMINISTRATIVE_SEAT_CANONICAL_PLACE_AND_HISTORICAL_WORLD_STATE_V1_REPORT.md",
] + [
    f"Docs/HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/{name}"
    for name in [
        "01_135-260行政单位与重要历史治所总表.xlsx",
        "02_135-260_CanonicalPhysicalPlace_Master.xlsx",
        "03_77战略名称与CanonicalPlace关系表.xlsx",
        "04_133CoreSettlement_SeatRole_Crosswalk.xlsx",
        "05_250PriorityCounty_ImportantPlace_And_SeatReference.xlsx",
        "06_13Scenario_ImportantPlace_WorldStateSnapshot_Index.xlsx",
        "07_HistoricalMajorChangePoint_Master.xlsx",
        "08_HistoricalChangePackage_Reference.xlsx",
        "09_三国志系列重要地点名称交叉参考.xlsx",
        "10_DevelopmentRelevantPlaceCandidateMaster.xlsx",
    ]
]

document_updates = []
for path in registry_document_paths:
    document_updates.append(
        {
            "DocumentId": f'doc.{stable_hash(path)}',
            "Path": path,
            "Title": Path(path).stem,
            "Domain": "HistoricalWorldGeography",
            "SubDomain": "AdministrativeSeatCanonicalPlaceHistoricalWorldState",
            "DocumentType": "Task" if "/TASK_" in path else "Report" if path.endswith("REPORT.md") else "ReferenceWorkbook" if path.endswith(".xlsx") else "ReferenceIndex",
            "AuthorityLevel": "L3",
            "Status": "HISTORICAL_REFERENCE",
            "CreatedOrKnownDate": TODAY,
            "LastKnownRevision": "V1",
            "CanonicalFor": "Administrative region/seat/place/world-state reference; not runtime implementation",
            "Supersedes": "",
            "SupersededBy": "",
            "PartiallySupersededSections": "",
            "RelatedDocuments": "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md|Docs/DATA_AND_CONTENT_FOUNDATION.md|Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md",
            "RelatedTasks": "HAN-135-260-ADMINISTRATIVE-SEAT-CANONICAL-PLACE-AND-HISTORICAL-WORLD-STATE-V1",
            "RelatedRuntimeSystems": "Future HistoricalWorldState runtime",
            "HistoricalValue": "HIGH",
            "RecommendedReader": "Designer|Historian|Developer|Codex",
            "ReadPriority": 3,
            "ConflictNotes": "Reference does not force runtime state and does not create duplicate Places.",
            "ActionRequired": "Use stable IDs and preserve evidence levels.",
        }
    )

domain_updates = [
    {
        "Domain": "HistoricalWorldGeography",
        "L0ProjectConstitution": "Docs/GAME_VISION_AND_GAMEPLAY.md",
        "L1CanonicalSpec": "Docs/DATA_AND_CONTENT_FOUNDATION.md|Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md",
        "L2CurrentStatus": "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md",
        "L3PrimaryReference": "Docs/HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md",
        "CanonicalGap": "Runtime AdministrativeSeat/HistoricalChangePackage implementation intentionally deferred",
        "MultipleL1Conflict": "NO",
        "ReadingEntry": "Docs/HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md",
        "ConflictPolicy": "AdministrativeRegion != Place; HistoricalReference never overwrites divergent runtime state",
    }
]

decision_texts = [
    ("DEC-PLACE-001", "AdministrativeRegion != Place"),
    ("DEC-PLACE-002", "Seat is Role, not Place type"),
    ("DEC-PLACE-003", "County != CountySeat"),
    ("DEC-PLACE-004", "HistoricalSeatReference != RuntimeAdministrativeSeat"),
    ("DEC-HISTORY-001", "Scenario Snapshot initializes historical starts"),
    ("DEC-HISTORY-002", "History does not force future runtime"),
    ("DEC-HISTORY-003", "Major historical events are world events"),
    ("DEC-HISTORY-004", "Offscreen locations continue historical/world simulation"),
    ("DEC-HISTORY-005", "Historical events require runtime preconditions"),
    ("DEC-HISTORY-006", "Historical event canonical outcome uses ChangePackage"),
    ("DEC-HISTORY-007", "Canonical PostState cannot overwrite divergent runtime"),
    ("DEC-HISTORY-008", "Historical place states reuse same Cell/Facility/Person IDs"),
    ("DEC-SERIES-001", "Strategic game-series labels are importance references only"),
    ("DEC-SERIES-002", "77 strategic names are not necessarily 77 same-level cities"),
]
decision_updates = [
    {
        "DecisionId": decision_id,
        "Domain": "HistoricalWorldGeography",
        "Title": title,
        "Decision": title,
        "Status": "FROZEN",
        "EffectiveFrom": TODAY,
        "SourceDocument": "Docs/TASK_HAN_135_260_ADMINISTRATIVE_SEAT_CANONICAL_PLACE_AND_HISTORICAL_WORLD_STATE_V1.md",
        "SupersedesDecisionId": "",
        "AffectedDocuments": "Master|Data|Determinism|Vision|MapArt|HistoricalWorldReference",
        "AffectedSystems": "World|Map|Scenario|Persistence|Presentation",
        "ReasonSummary": "Preserve one physical world and allow historical starts without scripted future overwrite.",
        "OpenQuestions": "See OPEN-PLACE/HISTORY entries",
        "Notes": "Reference-stage decision; runtime implementation is a separate task.",
    }
    for decision_id, title in decision_texts
]

open_questions = [
    ("OPEN-PLACE-001", "How many Places receive full deep development?", "Development roster and readiness review"),
    ("OPEN-PLACE-002", "What are final A/B/C place development bands?", "Cross-domain development cost and gameplay value"),
    ("OPEN-PLACE-003", "Which non-city passes, harbors and battlefields enter the first batch?", "Stable geography and Facility evidence"),
    ("OPEN-PLACE-004", "What are the final display rules for disputed strategic labels?", "C013/C035/C042/C044/C054/C061/C066 research"),
    ("OPEN-HISTORY-001", "Which exact Luoyang Facilities were damaged, destroyed, abandoned or rebuilt in 190?", "Primary/archaeological evidence by Facility"),
    ("OPEN-HISTORY-002", "When does rebuilding preserve a Facility ID versus create a new lifecycle entity?", "Lifecycle identity rules and site continuity evidence"),
]
open_updates = [
    {
        "OpenDecisionId": question_id,
        "Domain": "HistoricalWorldGeography",
        "Question": question,
        "Status": "OPEN",
        "WhyOpen": "Evidence or cross-system decision is insufficient; do not freeze by convenience.",
        "NeededEvidence": needed,
        "OwnerRole": "Future DEVELOPMENT-PLACE-ROSTER task owner",
        "Blocks": "Final deep-development roster or runtime change package",
        "SourceDocument": "Docs/HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md",
        "RecommendedNextReview": "DEVELOPMENT-PLACE-ROSTER-AND-REFERENCE-READINESS-V1",
        "Notes": "Candidate/reference state remains queryable without inventing facts.",
    }
    for question_id, question, needed in open_questions
]

implementation_updates = [
    {"GapId": "IMP-GAP-HISTORY-001", "Domain": "HistoricalWorldState", "CanonicalRequirement": "Separate HistoricalSeatReference and RuntimeAdministrativeSeat", "CurrentImplementation": "Reference workbooks only", "GapDescription": "Formal runtime entities/commands and persistence are not implemented", "Severity": "S1", "BlocksNextDevelopment": "NO_FOR_RESEARCH_YES_FOR_RUNTIME", "SuggestedFutureTask": "HISTORICAL-WORLD-STATE-RUNTIME-CONTRACT-V1", "Evidence": "Administrative Seat and World State V1 report"},
    {"GapId": "IMP-GAP-HISTORY-002", "Domain": "HistoricalWorldState", "CanonicalRequirement": "Preconditioned Canonical/Variant/Prevented/Transformed change packages", "CurrentImplementation": "Reference package only", "GapDescription": "No scheduler, transaction, command/event or save recovery adapter", "Severity": "S1", "BlocksNextDevelopment": "NO_FOR_ROSTER_YES_FOR_RUNTIME", "SuggestedFutureTask": "HISTORICAL-CHANGE-PACKAGE-RUNTIME-V1", "Evidence": "08_HistoricalChangePackage_Reference.xlsx"},
    {"GapId": "IMP-GAP-PLACE-001", "Domain": "CanonicalPlace", "CanonicalRequirement": "AdministrativeRegion, CanonicalPlace and SeatRole are separate runtime concepts", "CurrentImplementation": "Legacy Location model remains coarse", "GapDescription": "Runtime Place model cannot yet express all reference relations", "Severity": "S1", "BlocksNextDevelopment": "NO_FOR_RESEARCH_YES_FOR_RUNTIME", "SuggestedFutureTask": "CANONICAL-PLACE-RUNTIME-CONTRACT-V1", "Evidence": "DATA_AND_CONTENT_FOUNDATION canonical note"},
]

research_updates = [
    {"GapId": "RES-GAP-PLACE-001", "Domain": "StrategicPlace", "ResearchGap": "7 strategic labels have open/provisional Canonical mapping conflicts", "Priority": "HIGH", "CurrentEvidence": "03_77 strategic crosswalk", "RequiredSources": "Primary historical geography|archaeology|reliable local gazetteer", "DoNotInfer": "Do not create duplicate cities or silently repoint IDs", "SuggestedResearchAction": "Resolve C013/C035/C042/C044/C054/C061/C066 individually"},
    {"GapId": "RES-GAP-SEAT-001", "Domain": "AdministrativeSeat", "ResearchGap": "105 commandery-equivalent seats are complete as conservative candidates, not 105 specialist proofs", "Priority": "HIGH", "CurrentEvidence": "Hou Han Shu county order + reconstructed candidate method", "RequiredSources": "Primary historical geography and dedicated seat studies", "DoNotInfer": "Do not upgrade RECONSTRUCTED candidates to HISTORICAL without evidence", "SuggestedResearchAction": "Prioritize P0/P1 and moving-seat regions"},
    {"GapId": "RES-GAP-SERIES-001", "Domain": "SeriesImportance", "ResearchGap": "Per-title VII/VIII/X/XI/XII/XIII/XIV appearance flags lack license-compatible public-source audit", "Priority": "MEDIUM", "CurrentEvidence": "Existing 77 legacy strategic labels only", "RequiredSources": "Official/publicly licensed manuals or pages", "DoNotInfer": "Do not extract commercial game databases, coordinates, numbers, UI or script text", "SuggestedResearchAction": "Audit name presence only"},
    {"GapId": "RES-GAP-LUOYANG-190", "Domain": "HistoricalWorldState", "ResearchGap": "Specific 190 Luoyang Facility and neighborhood outcomes remain uncertain", "Priority": "HIGH", "CurrentEvidence": "Historical large-scale burning/relocation; facility-level details mixed", "RequiredSources": "Primary text|archaeology|academic urban history", "DoNotInfer": "Do not mark ordinary facilities HISTORICAL_DESTROYED without evidence", "SuggestedResearchAction": "Facility-by-Facility evidence review"},
]

conflict_updates = [
    {"ConflictId": "DOC-CONFLICT-PLACE-001", "Domain": "HistoricalWorldGeography", "DocumentA": "Legacy Location/City shorthand", "DocumentB": "Administrative Seat and World State V1", "ConflictDescription": "Legacy text can conflate province/commandery/county with physical city", "CurrentPreferredRule": "AdministrativeRegion != CanonicalPlace; Seat is a role", "AuthorityReason": "Current user task and updated L1 canonical documents", "ResolutionStatus": "PARTIALLY_RESOLVED", "RequiredAction": "Mark legacy Location sections PARTIALLY_SUPERSEDED and use crosswalks", "RiskIfIgnored": "Duplicate cities, broken IDs and incorrect runtime seats"},
    {"ConflictId": "DOC-CONFLICT-HISTORY-001", "Domain": "HistoricalWorldState", "DocumentA": "Future historical anchor shorthand", "DocumentB": "Scenario Snapshot + ChangePoint + inherited-state contract", "ConflictDescription": "Historical snapshots could be misread as forced future corrections", "CurrentPreferredRule": "Direct starts use snapshots; continuous play uses runtime evolution and preconditioned events", "AuthorityReason": "Current canonical task", "ResolutionStatus": "RESOLVED_BY_CANONICAL_NOTE", "RequiredAction": "Keep snapshot and runtime paths distinct", "RiskIfIgnored": "Player actions overwritten and duplicated world maps"},
]


# ---------------------------------------------------------------------------
# Generated documentation and report
# ---------------------------------------------------------------------------

relation_counts = Counter(row["RelationType"] for row in strategic_crosswalk)
distinct_strategic_places = len({row["CanonicalPlaceId"] for row in strategic_crosswalk})
commandery_seat_count = len({row["AdministrativeUnitId"] for row in administrative_seats if row["UnitType"] in ("COMMANDERY", "KINGDOM", "YIN", "DEPENDENCY", "CAPITAL_REGION")})
resolved_priority_seats = sum(1 for row in priority_place_rows if row["CountySeatPlaceId"])
moving_count = sum(1 for row in strategic_crosswalk if row["RelationType"] == "MOVING_SEAT_REGION_LABEL")
conflict_count = sum(1 for row in strategic_crosswalk if row["ConflictStatus"] != "RESOLVED_TO_EXISTING_PLACE")

summary = {
    "province_count": len(province_ids),
    "province_scenario_records": len(province_scenario_seats),
    "commandery_equivalent_count": commandery_seat_count,
    "county_count": len(base["counties"]),
    "core_settlement_count": len(core),
    "priority_county_count": len(priority_counties),
    "resolved_priority_county_seats": resolved_priority_seats,
    "strategic_label_count": len(strategic_crosswalk),
    "strategic_relation_counts": dict(relation_counts),
    "strategic_distinct_place_count": distinct_strategic_places,
    "strategic_open_conflicts": conflict_count,
    "moving_seat_labels": moving_count,
    "scenario_snapshot_records": len(scenario_snapshots),
    "scenario_year_count": len(SCENARIO_YEARS),
    "major_change_point_count": len(change_points),
    "change_package_record_count": len(change_packages),
    "development_candidate_count": len(candidates),
    "luoyang_facility_lifecycle_records": len(luoyang_facility_lifecycle),
    "runtime_code_changed": False,
    "save_schema_changed": False,
}

readme = f"""# Administrative Seat / Canonical Place / Historical World State V1

## 定位

本目录把项目已有的13州、105郡国等价单位、1182县、77战略显示名、133 Core Settlements、
250重点县和13个Scenario放入同一套可查询关系。它是历史与开发Reference，不是第二套运行时世界。

## 冻结关系

```text
AdministrativeRegion（州/郡/国/尹/属国/县）
        └─ HistoricalSeatReference / RuntimeAdministrativeSeat（角色）
                    └─ CanonicalPlace（真实物理地点）
                              └─ Cell + Facility + Person + Organization + Owner/Controller
```

- 县不等于县城，郡国不等于城市，治所不是Place类型；
- 一个Place可同时承担县治、郡治、州治、首都等角色，但只保留一个PlaceId；
- 直接选择Scenario时使用Snapshot；连续游玩使用运行世界，不用未来Snapshot校正；
- 重大事件按前提后台结算Canonical/Variant/Prevented/Transformed结果；
- 全部历史状态复用同一Cell、Place、Facility和PermanentPerson ID。

## 方法与覆盖

- 时间：13个Scenario（{pipe(SCENARIO_YEARS)}）+ {len(change_points)}个重大ChangePoint候选 + 状态继承；
- 行政：{len(province_ids)}州×13切片共{len(province_scenario_seats)}条；{commandery_seat_count}郡国等价单位治所候选全覆盖；
- 地点：{len(core)}个既有Core Settlement，不创建第二套ID；
- 战略名：77项逐条交叉到{distinct_strategic_places}个既有CanonicalPlace，{conflict_count}项保留开放冲突；
- 县治：250重点县中{resolved_priority_seats}项已有Core Settlement治所，其他保持UNKNOWN；
- Snapshot：{len(scenario_snapshots)}条Place×Scenario索引，不复制Unity地图；
- 运行时：未实现，见Implementation Gap。

## 工作簿

1. `01_135-260行政单位与重要历史治所总表.xlsx`
2. `02_135-260_CanonicalPhysicalPlace_Master.xlsx`
3. `03_77战略名称与CanonicalPlace关系表.xlsx`
4. `04_133CoreSettlement_SeatRole_Crosswalk.xlsx`
5. `05_250PriorityCounty_ImportantPlace_And_SeatReference.xlsx`
6. `06_13Scenario_ImportantPlace_WorldStateSnapshot_Index.xlsx`
7. `07_HistoricalMajorChangePoint_Master.xlsx`
8. `08_HistoricalChangePackage_Reference.xlsx`
9. `09_三国志系列重要地点名称交叉参考.xlsx`
10. `10_DevelopmentRelevantPlaceCandidateMaster.xlsx`

洛阳专项位于`11_LUOYANG_MAJOR_HISTORICAL_WORLD_STATES/`；其他P0地点候选位于
`12_P0_PLACE_CHANGEPOINT_CANDIDATES/`。

## 证据与法律边界

`HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN`保持分离。系列游戏只保留抽象重要性研究槽；
本轮没有导入商业游戏地图、坐标、数据库、美术、UI、数值或剧本文本。
"""
write_text(TASK_ROOT / "README.md", readme)

luoyang_implication = """# 洛阳历史世界状态的开发含义

- 184、189、190前、190后、220—223不是不同Unity场景，而是同一Place/Cell/Facility集合上的状态投影。
- 190迁都与焚毁若在运行世界满足前提并发生，必须后台提交人口、家庭、官署、库存、设施和控制权变化。
- 史料只证明大规模变化时，普通设施不得标记为`HISTORICAL_DESTROYED`；应使用`MODELED`或`UNKNOWN`。
- 北宫、南宫、中央官署、太仓、武库、市场、城门等先做Facility级证据审计，再决定Damage/Destroyed/Abandoned/Rebuilt。
- 直接194/223开局可使用相应历史Snapshot；从184连续游玩则保留玩家与AI造成的真实分歧。
- 本目录只提供Reference，不修改现有洛阳184运行时数据或Save Schema。
"""
write_text(LUOYANG_ROOT / "LuoyangDevelopmentImplication.md", luoyang_implication)

p0_names = {
    "CHANGAN": "长安", "YE": "邺", "XU": "许", "CHENGDU": "成都",
    "XIANGYANG": "襄阳", "JIANGLING": "江陵", "JIANYE": "建业",
}
write_text(P0_ROOT / "README.md", "# P0地点重大历史世界状态候选\n\n以下文件只识别需要不同世界状态的时间窗，不决定最终开发Roster，也不实现运行时ChangePackage。\n")
for key, display_name in p0_names.items():
    rows = change_by_place[P[key]]
    details = [row for row in change_points if row["ChangePointId"] in rows]
    lines = [f"# {display_name}重大历史世界状态候选", "", f"CanonicalPlaceId：`{P[key]}`", "", "| ChangePoint | 时间窗 | 类型 | 地图影响 | 重点准备 |", "|---|---|---|---|---|"]
    for row in details:
        focus = pipe([f'人口:{row["PopulationImpact"]}', f'设施:{row["FacilityImpact"]}', f'行政:{row["AdministrativeImpact"]}', f'军事:{row["MilitaryImpact"]}', f'交通:{row["TransportImpact"]}'])
        lines.append(f'| `{row["ChangePointId"]}` | {row["TimeWindow"]} | {row["ChangeType"]} | {row["MapImpact"]} | {focus} |')
    lines += ["", "这些状态必须复用同一Place/Cell/Facility ID；事件只有在运行时前提成立时才应用，未来史实不得强制覆盖已经分歧的世界。"]
    write_text(P0_ROOT / f"{key}_MAJOR_CHANGEPOINT_CANDIDATES.md", "\n".join(lines))

report = f"""# HAN 135—260 Administrative Seat / Canonical Place / Historical World State V1 Report

## Outcome

本轮完成了行政区—治所角色—物理地点—历史状态的Reference层交叉；没有修改运行时代码、Save Schema或Unity场景。

## 30项交接回答

1. 13州×13 Scenario形成{len(province_scenario_seats)}条稀疏时间轴解析记录；未知/割据阶段保持UNKNOWN，不强填唯一州治。
2. {commandery_seat_count}/105郡国等价单位均有候选治所映射；它们主要来自郡国志县序的保守重建，不等于105项均已专题考证。
3. 250重点县中{resolved_priority_seats}项治所已解析到既有Core Settlement；其余保持UNKNOWN。
4. 77战略名称中`PLACE_NAME_DIRECT`为{relation_counts['PLACE_NAME_DIRECT']}项。
5. `ADMIN_REGION_AS_STRATEGIC_LABEL`为{relation_counts['ADMIN_REGION_AS_STRATEGIC_LABEL']}项。
6. `PLACE_RENAME_TIMELINE`为{relation_counts['PLACE_RENAME_TIMELINE']}项。
7. `MOVING_SEAT_REGION_LABEL`为{moving_count}项。
8. `STRATEGIC_SETTLEMENT_NOT_MAJOR_SEAT`为{relation_counts['STRATEGIC_SETTLEMENT_NOT_MAJOR_SEAT']}项。
9. 77项当前交叉到{distinct_strategic_places}个既有CanonicalPlace；城阳/北海与金城/西平暴露两组重复映射风险，不新增Place掩盖冲突。
10. 133 Core Settlements全部进入13 Scenario交叉，共{len(core_seat_crosswalk)}条角色记录；覆盖全部105郡国候选治所。
11. 汉中、北海、汝南、会稽、河内、河东、天水、南海、交趾等首先是行政/战略显示名，不应自动理解为同名固定城市。
12. 城阳/北海、金城/西平、江夏、庐江、公安、建安、梓潼/涪最容易造成重复或错误建城。
13. 133个重要Place×13 Scenario共{len(scenario_snapshots)}条Snapshot索引；数据为Reference，不是复制地图。
14. 共识别{len(change_points)}个Major Historical ChangePoint候选。
15. 最高地图开发价值包括洛阳190、长安190—196、许196、襄阳/江陵208—223、成都214/221、建业211/229、武昌221和永安222—223。
16. 洛阳需准备184基线/动员、189政变前后、190迁都焚毁前后、220—223恢复/魏都状态。
17. 长安需准备190朝廷迁入、192—195控制危机、195—196朝廷东归状态。
18. 邺需准备204控制中心、210大型设施建设、220政权转换状态。
19. 许需准备196朝廷迁入与220—221汉魏转换/名称时间线状态。
20. 成都需准备188—190州治转移、214易主、221—223政权首都状态。
21. 襄阳需准备190州治中心、208接管、219襄樊战区状态。
22. 江陵需准备208—209控制转换、219接管、222—223夷陵后区域军事状态。
23. 建业需准备211—212秣陵/建业时间线和政治中心、229吴都状态。
24. 洛阳190、长安迁都期、许196、邺大型建设、成都/建业/武昌首都建设、襄樊战事等需要Cell/Facility/Transport状态评估。
25. 单纯官职、人物在场、政权称号或没有空间后果的事件只使用普通Event/Person/Office变化，不创建地图ChangePoint。
26. 每代系列出现标志尚无许可兼容的逐作来源；77个既有战略标签已全部保留交叉槽，禁止凭记忆伪填。
27. DevelopmentRelevantPlaceCandidate共{len(candidates)}项，包含既有Core、交通、军事空间、战略点和Estate候选；不等于最终Roster。
28. 争议集中于{conflict_count}个战略映射、移动治所、105郡国候选治所证据等级、洛阳190具体设施结果与系列逐代出现情况。
29. 后续运行时需要CanonicalPlace/AdministrativeRegion分离、RuntimeSeat、历史事件前提、事务ChangePackage、离屏结算及存档恢复。
30. 已具备进入`DEVELOPMENT-PLACE-ROSTER-AND-REFERENCE-READINESS-V1`的资料条件，但最终城市/据点数量和A/B/C分级仍必须在该任务决定。

## Validation targets

- 13州、105郡国、1182县ID、77标签、133聚落、250重点县与13 Scenario全部纳入审计；
- {len(change_points)}个ChangePoint与{len(change_packages)}条Reference Package交叉；
- 洛阳关键Facility保持原ID，普通设施结果不伪装为史实；
- 系列参考未导入商业数据库、坐标、数值、UI、美术或剧本文本；
- 运行时代码与存档均未改变。
"""
write_text(TASK_ROOT / "HAN_135_260_ADMINISTRATIVE_SEAT_CANONICAL_PLACE_AND_HISTORICAL_WORLD_STATE_V1_REPORT.md", report)


workdata = {
    "administrative_seats": administrative_seats,
    "province_scenario_seats": province_scenario_seats,
    "canonical_places": canonical_places,
    "place_name_timeline": place_name_timeline,
    "strategic_crosswalk": strategic_crosswalk,
    "core_seat_crosswalk": core_seat_crosswalk,
    "priority_places": priority_place_rows,
    "scenario_snapshots": scenario_snapshots,
    "change_points": change_points,
    "change_packages": change_packages,
    "series_cross": series_cross,
    "development_candidates": candidates,
    "luoyang_timeline": luoyang_timeline,
    "luoyang_changepoints": [row for row in change_points if row["PlaceId / RegionId"] == P["LUOYANG"]],
    "luoyang_prepost": luoyang_prepost,
    "luoyang_facility_lifecycle": luoyang_facility_lifecycle,
    "luoyang_population_migration": luoyang_population_migration,
    "luoyang_person_family": luoyang_person_family,
    "sources": sources,
    "registry_updates": {
        "document_registry": document_updates,
        "domain_map": domain_updates,
        "design_decisions": decision_updates,
        "open_decisions": open_updates,
        "implementation_gaps": implementation_updates,
        "research_gaps": research_updates,
        "document_conflicts": conflict_updates,
    },
    "summary": summary,
}
write_json(OUT / "administrative_seat_world_state_workdata.json", workdata)
write_json(OUT / "generation_summary.json", summary)

print(json.dumps(summary, ensure_ascii=False, indent=2))
