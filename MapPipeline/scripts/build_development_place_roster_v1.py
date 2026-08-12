#!/usr/bin/env python3
"""Build the curated Development Place roster and readiness work data.

This consumes existing project references.  It does not create runtime Places,
Facilities, Persons, families, saves, or historical truth outside those inputs.
"""

from __future__ import annotations

import json
from collections import Counter, defaultdict
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
SOURCE_ROOT = REPO / "outputs" / "HAN_135_260_ADMINISTRATIVE_SEAT_CANONICAL_PLACE_AND_HISTORICAL_WORLD_STATE_V1"
DEEPENING_ROOT = REPO / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1"
KB_ROOT = REPO / "outputs" / "HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1"
OUTPUT_ROOT = REPO / "outputs" / "DEVELOPMENT_PLACE_ROSTER_AND_REFERENCE_READINESS_V1"
DOC_ROOT = REPO / "Docs" / "HISTORICAL_WORLD_REFERENCE" / "DEVELOPMENT_PLACE_ROSTER_V1"
MANIFEST_ROOT = REPO / "Docs" / "KNOWLEDGE_BASE" / "DEVELOPMENT_MANIFESTS"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


admin = load(SOURCE_ROOT / "administrative_seat_world_state_workdata.json")
deep = load(DEEPENING_ROOT / "deepening_workdata.json")
kb = load(KB_ROOT / "knowledge_base_workdata.json")

canonical_by_id = {row["CanonicalPlaceId"]: row for row in admin["canonical_places"]}
strategic_by_name = {row["StrategicDisplayName"]: row for row in admin["strategic_crosswalk"]}
strategic_by_place = defaultdict(list)
for row in admin["strategic_crosswalk"]:
    strategic_by_place[row["CanonicalPlaceId"]].append(row)
core_by_place = defaultdict(list)
for row in admin["core_seat_crosswalk"]:
    core_by_place[row["CanonicalPlaceId"]].append(row)
priority_by_place = {
    row["CountySeatPlaceId"]: row for row in admin["priority_places"] if row.get("CountySeatPlaceId")
}
transport_by_id = {row["transport_id"]: row for row in deep["transport_nodes"]}
military_by_id = {row["military_space_id"]: row for row in deep["military_spaces"]}
scenario_years = sorted({int(row["ScenarioYear"]) for row in admin["scenario_snapshots"]})
change_by_target = defaultdict(list)
for row in admin["change_points"]:
    change_by_target[row["PlaceId / RegionId"]].append(row)


URBAN_DEPTHS = {
    "洛阳": ("D5", "P0", "WAVE_0"),
    "长安": ("D4", "P1", "WAVE_1"),
    "邺": ("D4", "P1", "WAVE_1"),
    "许昌": ("D4", "P1", "WAVE_1"),
    "成都": ("D4", "P1", "WAVE_2"),
    "襄阳": ("D4", "P1", "WAVE_1"),
    "江陵": ("D4", "P1", "WAVE_2"),
    "建业": ("D4", "P1", "WAVE_2"),
    "合肥": ("D4", "P1", "WAVE_2"),
    "汉中": ("D4", "P1", "WAVE_2"),
    "蓟": ("D3", "P2", "WAVE_3"),
    "晋阳": ("D3", "P2", "WAVE_3"),
    "宛": ("D3", "P2", "WAVE_3"),
    "陈留": ("D3", "P2", "WAVE_2"),
    "濮阳": ("D3", "P2", "WAVE_3"),
    "汝南": ("D3", "P2", "WAVE_3"),
    "下邳": ("D3", "P2", "WAVE_3"),
    "寿春": ("D3", "P2", "WAVE_2"),
    "江州": ("D3", "P2", "WAVE_3"),
    "永安": ("D3", "P2", "WAVE_3"),
    "武昌": ("D3", "P2", "WAVE_2"),
    "吴": ("D3", "P2", "WAVE_3"),
    "会稽": ("D3", "P2", "WAVE_3"),
    "柴桑": ("D3", "P2", "WAVE_2"),
    "江夏": ("D3", "P2", "WAVE_2"),
    "天水": ("D3", "P2", "WAVE_3"),
    "河东": ("D3", "P2", "WAVE_3"),
    "南海": ("D3", "P3", "WAVE_4"),
    "钜鹿": ("D3", "P2", "WAVE_3"),
    "襄平": ("D3", "P3", "WAVE_4"),
    "广陵": ("D3", "P2", "WAVE_3"),
    "交趾": ("D3", "P3", "WAVE_4"),
    "金城": ("D3", "P3", "WAVE_4"),
    "谯": ("D3", "P2", "WAVE_3"),
    "中山": ("D3", "P2", "WAVE_3"),
    "涿": ("D3", "P2", "WAVE_3"),
    "乐浪": ("D2", "P4", "RESERVE"),
    "北平": ("D2", "P3", "WAVE_4"),
    "上党": ("D2", "P3", "WAVE_4"),
    "南皮": ("D2", "P3", "WAVE_4"),
    "平原": ("D2", "P3", "WAVE_4"),
    "甘陵": ("D2", "P3", "WAVE_4"),
    "北海": ("D2", "P3", "WAVE_4"),
    "济南": ("D2", "P3", "WAVE_4"),
    "济北": ("D2", "P3", "WAVE_4"),
    "琅琊": ("D2", "P3", "WAVE_4"),
    "小沛": ("D2", "P3", "WAVE_3"),
    "陈": ("D2", "P3", "WAVE_4"),
    "弘农": ("D2", "P2", "WAVE_2"),
    "河内": ("D2", "P3", "WAVE_3"),
    "安定": ("D2", "P4", "RESERVE"),
    "武威": ("D2", "P3", "WAVE_4"),
    "武都": ("D2", "P3", "WAVE_4"),
    "新野": ("D2", "P2", "WAVE_2"),
    "长沙": ("D2", "P3", "WAVE_3"),
    "豫章": ("D2", "P3", "WAVE_4"),
    "皖": ("D2", "P3", "WAVE_3"),
    "鄱阳": ("D2", "P3", "WAVE_4"),
    "涪": ("D2", "P3", "WAVE_3"),
}


NON_URBAN = {
    "geo.site.hulao": ("虎牢", "D4", "P0", "WAVE_0", "PassArea|FortifiedPoint|TransportHub|MilitaryHub"),
    "geo.site.fancheng": ("樊城", "D4", "P1", "WAVE_1", "FortifiedTown|RiverCrossing|MilitaryHub"),
    "geo.site.xiakou": ("夏口", "D4", "P1", "WAVE_2", "HarborSettlement|NavalHub|TransportHub"),
    "geo.site.yangping": ("阳平关", "D4", "P1", "WAVE_2", "PassArea|FortifiedPoint|LogisticsHub"),
    "geo.site.jiange": ("剑阁", "D4", "P1", "WAVE_2", "PassArea|FortifiedPoint|LogisticsHub"),
    "geo.site.ruxukou": ("濡须口", "D4", "P1", "WAVE_2", "RiverFort|NavalHub|MilitaryHub"),
    "geo.site.hangu": ("函谷关", "D3", "P2", "WAVE_0", "PassArea|FortifiedPoint|TransportHub"),
    "geo.site.tongguan": ("潼关", "D3", "P2", "WAVE_2", "PassArea|FortifiedPoint|TransportHub|MilitaryHub"),
    "geo.site.wuguan": ("武关", "D3", "P3", "WAVE_3", "PassArea|FortifiedPoint|TransportHub"),
    "geo.site.chencang": ("陈仓", "D3", "P2", "WAVE_3", "PassArea|FortifiedPoint|LogisticsHub"),
    "geo.site.jiameng": ("葭萌", "D3", "P3", "WAVE_3", "PassArea|FortifiedPoint|LogisticsHub"),
    "geo.site.yiling": ("夷陵", "D3", "P2", "WAVE_2", "PassArea|RiverCorridor|BattlefieldArea"),
    "geo.site.chibi": ("赤壁", "D3", "P2", "WAVE_2", "RiverBattlefield|NavalHub|ScenarioCritical"),
}


STATE_PLANS = {
    "洛阳": [(140, "S2", "H2"), (184, "S4", "H4"), (189, "S4", "H4"), (190, "S4", "H4"), (194, "S3", "H3"), (249, "S3", "H3")],
    "长安": [(184, "S2", "H2"), (190, "S3", "H3"), (194, "S4", "H4"), (200, "S2", "H2"), (214, "S2", "H2")],
    "邺": [(184, "S2", "H2"), (189, "S2", "H2"), (200, "S4", "H3"), (207, "S3", "H3"), (220, "S4", "H4")],
    "许昌": [(184, "S2", "H2"), (194, "S2", "H2"), (196, "S4", "H4"), (200, "S4", "H3"), (220, "S3", "H3")],
    "成都": [(184, "S2", "H2"), (194, "S2", "H2"), (214, "S4", "H4"), (223, "S4", "H3")],
    "襄阳": [(184, "S2", "H2"), (194, "S3", "H3"), (208, "S4", "H4"), (219, "S4", "H4")],
    "江陵": [(184, "S2", "H2"), (208, "S4", "H4"), (219, "S3", "H3"), (222, "S3", "H3")],
    "建业": [(184, "S2", "H2"), (194, "S2", "H2"), (212, "S4", "H4"), (229, "S4", "H3")],
    "合肥": [(184, "S1", "H1"), (208, "S2", "H2"), (214, "S4", "H3"), (219, "S3", "H3")],
    "汉中": [(184, "S2", "H2"), (214, "S3", "H3"), (219, "S4", "H4"), (227, "S3", "H3")],
    "虎牢": [(184, "S2", "H2"), (189, "S4", "H4"), (190, "S3", "H3")],
    "樊城": [(184, "S1", "H1"), (208, "S2", "H2"), (219, "S4", "H4")],
    "夏口": [(184, "S1", "H1"), (208, "S4", "H4"), (219, "S2", "H2")],
    "阳平关": [(184, "S1", "H1"), (214, "S2", "H2"), (219, "S4", "H4"), (227, "S3", "H3")],
    "剑阁": [(184, "S1", "H1"), (214, "S2", "H2"), (223, "S4", "H3")],
    "濡须口": [(184, "S1", "H1"), (214, "S4", "H3"), (219, "S3", "H3")],
}


def resolve_urban(label: str):
    row = strategic_by_name.get(label)
    if row:
        return row["CanonicalPlaceId"], row
    base = next((candidate for candidate in admin["canonical_places"] if candidate["CanonicalName"] == label), None)
    if not base:
        raise KeyError(f"No strategic crosswalk or canonical place row for {label}")
    return base["CanonicalPlaceId"], {
        "ConflictStatus": "RESOLVED_TO_EXISTING_PLACE",
        "StrategicDisplayName": "",
        "ActualHistoricalSeatName": label,
    }


def roles_for(place_id: str, label: str) -> tuple[str, str, str]:
    base = canonical_by_id[place_id]
    physical = "UrbanSettlement" if label in {"洛阳", "长安", "邺", "许昌", "成都", "襄阳", "江陵", "建业"} else "Settlement"
    administrative = base.get("AdministrativeRolesSummary", "")
    strategic = []
    if label in {"洛阳", "长安", "邺", "许昌", "成都", "建业"}:
        strategic.extend(["PoliticalCenter", "TradeHub", "ScenarioCritical"])
    if label in {"襄阳", "江陵", "合肥", "汉中", "寿春", "柴桑", "武昌"}:
        strategic.extend(["MilitaryHub", "TransportHub", "ScenarioCritical"])
    if label in {"江陵", "合肥", "柴桑", "武昌", "广陵", "会稽", "吴"}:
        strategic.append("TradeHub")
    return physical, administrative, "|".join(dict.fromkeys(strategic)) or "RegionalGameplayHub"


def system_value(label: str, nonurban: bool = False) -> str:
    if nonurban:
        mapping = {
            "虎牢": "Pass|Military|Logistics|HistoricalEvent",
            "樊城": "Military|River|Urban|HistoricalEvent",
            "夏口": "Naval|Trade|Logistics|HistoricalEvent",
            "阳平关": "Pass|Military|Logistics",
            "剑阁": "Pass|Military|Logistics",
            "濡须口": "Naval|Military|Logistics|HistoricalEvent",
            "赤壁": "Naval|Military|HistoricalEvent",
        }
        return mapping.get(label, "Pass|Military|Logistics")
    mapping = {
        "洛阳": "Urban|Administrative|Family|Trade|Military|HistoricalEvent|Political|Education|Clan|Estate",
        "长安": "Urban|Administrative|Trade|Military|Logistics|HistoricalEvent|Political",
        "邺": "Urban|Administrative|Family|Trade|Military|HistoricalEvent|Political",
        "许昌": "Urban|Administrative|Family|Trade|Military|HistoricalEvent|Political",
        "成都": "Urban|Administrative|Agriculture|Family|Trade|Military|Political",
        "襄阳": "Urban|Administrative|Trade|Military|Naval|HistoricalEvent",
        "江陵": "Urban|Administrative|Trade|Military|Naval|Logistics|HistoricalEvent",
        "建业": "Urban|Administrative|Trade|Naval|Political|HistoricalEvent",
        "合肥": "Urban|Military|Naval|Logistics|HistoricalEvent",
        "汉中": "Urban|Agriculture|Military|Pass|Logistics|HistoricalEvent",
    }
    return mapping.get(label, "Urban|Administrative|Trade|Military")


roster = []
for label, (depth, priority, wave) in URBAN_DEPTHS.items():
    place_id, cross = resolve_urban(label)
    base = canonical_by_id[place_id]
    physical, admin_roles, strategic_roles = roles_for(place_id, label)
    conflict = cross["ConflictStatus"]
    readiness = "READY_FOR_IMPLEMENTATION" if label == "洛阳" else (
        "MOSTLY_READY" if depth == "D4" and label in {"长安", "邺", "许昌", "成都", "襄阳", "江陵", "建业"} else
        "PARTIAL"
    )
    status = "FROZEN_TARGET" if conflict == "RESOLVED_TO_EXISTING_PLACE" else "FROZEN_TARGET_WITH_MAPPING_BLOCKER"
    roster.append({
        "CanonicalPlaceId": place_id,
        "CanonicalName": base["CanonicalName"],
        "DevelopmentDisplayName": label,
        "PhysicalRoles": physical,
        "AdministrativeRoles": admin_roles,
        "StrategicRoles": strategic_roles,
        "DevelopmentDepth": depth,
        "DevelopmentPriority": priority,
        "SeriesRecognition": "HIGH" if label in strategic_by_name else "NONE_RECORDED",
        "HistoricalImportance": base["HistoricalImportance"],
        "ScenarioImportance": "FLAGSHIP" if depth == "D5" else ("HIGH" if depth in {"D4", "D3"} else "FOCUSED"),
        "SystemValidationValue": system_value(label),
        "ExistingRuntimeLevel": "FORMAL_LUOYANG_PACKAGES" if label == "洛阳" else "REFERENCE_ONLY",
        "ReferenceReadiness": readiness,
        "DevelopmentStatus": status,
        "RecommendedWave": wave,
        "CanonicalConflictStatus": conflict,
        "SourceCandidateKinds": "CanonicalCoreSettlement|StrategicCrosswalk|PriorityCounty",
        "Notes": "DevelopmentDepth is production scope, not administrative rank or physical type.",
    })

for place_id, (label, depth, priority, wave, roles) in NON_URBAN.items():
    source = transport_by_id[place_id]
    roster.append({
        "CanonicalPlaceId": place_id,
        "CanonicalName": label,
        "DevelopmentDisplayName": label,
        "PhysicalRoles": roles,
        "AdministrativeRoles": "NONE_NON_ADMINISTRATIVE_PLACE",
        "StrategicRoles": roles,
        "DevelopmentDepth": depth,
        "DevelopmentPriority": priority,
        "SeriesRecognition": "ABSTRACT_STRATEGIC_RECOGNITION_ONLY",
        "HistoricalImportance": "R3" if depth == "D4" else "R2",
        "ScenarioImportance": "HIGH" if depth == "D4" else "FOCUSED",
        "SystemValidationValue": system_value(label, True),
        "ExistingRuntimeLevel": "REFERENCE_SITE_ONLY",
        "ReferenceReadiness": "RESEARCH_REQUIRED" if source["confidence"] == "low" else "PARTIAL",
        "DevelopmentStatus": "FROZEN_TARGET_REQUIRES_CANONICAL_SITE_REVIEW",
        "RecommendedWave": wave,
        "CanonicalConflictStatus": "SITE_REFERENCE_REQUIRES_RUNTIME_CANONICALIZATION",
        "SourceCandidateKinds": "ExistingTransportSiteReference|MilitarySpaceReference",
        "Notes": "Existing geo.site stable reference is reused; no second Place or fake administrative region is created.",
    })

seen = set()
deduped = []
for row in roster:
    if row["CanonicalPlaceId"] in seen:
        continue
    seen.add(row["CanonicalPlaceId"])
    deduped.append(row)
roster = sorted(deduped, key=lambda row: (int(row["DevelopmentDepth"][1]), row["DevelopmentPriority"], row["CanonicalPlaceId"]), reverse=True)


def support_rows(row):
    label = row["DevelopmentDisplayName"]
    plans = STATE_PLANS.get(label)
    if not plans:
        plans = [(184, "S2" if row["DevelopmentDepth"] == "D3" else "S1", "H2" if row["DevelopmentDepth"] == "D3" else "H1")]
    output = []
    for year, support, depth in plans:
        formal = year in scenario_years
        cp = next((cp for cp in change_by_target.get(row["CanonicalPlaceId"], []) if str(year) in cp["TimeWindow"]), None)
        output.append({
            "PlaceId": row["CanonicalPlaceId"],
            "PlaceName": label,
            "ScenarioYear": year,
            "TimePointType": "FORMAL_SCENARIO" if formal else "MAJOR_CHANGEPOINT_OR_REFERENCE_YEAR",
            "HistoricalStateId": f"devstate.{row['CanonicalPlaceId'].replace('.', '_')}.{year}",
            "SupportLevel": support,
            "MajorChangePointId": cp["ChangePointId"] if cp else "",
            "RequiredSnapshotDepth": depth,
            "ChangePackageRequired": "YES" if depth in {"H4", "H5"} else "NO_REFERENCE_ONLY",
            "ExistingReference": "ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1",
            "MissingReference": "Runtime ChangePackage" if depth in {"H4", "H5"} else "Detailed local state as development begins",
            "DevelopmentPriority": row["DevelopmentPriority"],
        })
    return output


historical_state_plan = [state for row in roster for state in support_rows(row)]


READINESS_COLUMNS = ["Geography", "Seat", "Population", "Urban", "Facility", "Transport", "Industry", "Person", "Clan", "Family", "Military", "Scenario", "ChangePoint", "Cell", "Art", "Runtime"]
readiness = []
for row in roster:
    depth = row["DevelopmentDepth"]
    nonurban = row["CanonicalPlaceId"].startswith("geo.site.")
    luoyang = row["DevelopmentDisplayName"] == "洛阳"
    p0_city = depth in {"D4", "D5"} and not nonurban
    values = {
        "Geography": "PARTIAL" if nonurban else "READY",
        "Seat": "NOT_APPLICABLE" if nonurban else "READY",
        "Population": "DATA_REQUIRED" if nonurban else "MOSTLY_READY",
        "Urban": "NOT_APPLICABLE" if nonurban else ("READY" if luoyang else ("MOSTLY_READY" if p0_city else "PARTIAL")),
        "Facility": "READY" if luoyang else ("PARTIAL" if depth in {"D4", "D5"} else "DESIGN_REQUIRED"),
        "Transport": "MOSTLY_READY" if depth in {"D4", "D5"} else "PARTIAL",
        "Industry": "PARTIAL" if not nonurban else "NOT_APPLICABLE",
        "Person": "MOSTLY_READY" if p0_city else ("PARTIAL" if not nonurban else "DATA_REQUIRED"),
        "Clan": "MOSTLY_READY" if p0_city else ("PARTIAL" if not nonurban else "NOT_APPLICABLE"),
        "Family": "PARTIAL" if not nonurban else "NOT_APPLICABLE",
        "Military": "MOSTLY_READY" if depth in {"D4", "D5"} else "PARTIAL",
        "Scenario": "READY" if luoyang else ("MOSTLY_READY" if depth == "D4" else "PARTIAL"),
        "ChangePoint": "MOSTLY_READY" if row["DevelopmentDisplayName"] in STATE_PLANS else "RESEARCH_REQUIRED",
        "Cell": "READY" if luoyang else "DATA_REQUIRED",
        "Art": "PARTIAL" if luoyang else "DESIGN_REQUIRED",
        "Runtime": "MOSTLY_READY" if luoyang else "IMPLEMENTATION_REQUIRED",
    }
    overall = "READY_FOR_IMPLEMENTATION" if luoyang else (
        "MOSTLY_READY" if p0_city else ("RESEARCH_REQUIRED" if nonurban and depth == "D4" else "PARTIAL")
    )
    readiness.append({"PlaceId": row["CanonicalPlaceId"], "PlaceName": row["DevelopmentDisplayName"], **values, "OverallReadiness": overall, "ReadinessDecision": "Proceed to readiness review" if luoyang else "Retain in planned wave; close blockers before implementation"})


blockers = []
blocker_seq = 1


def add_blocker(place_id, kind, description, severity, depth, scenario, action, defer, notes=""):
    global blocker_seq
    blockers.append({
        "BlockerId": f"DPB-{blocker_seq:03d}", "PlaceId": place_id, "BlockerType": kind,
        "Description": description, "Severity": severity, "BlocksDepth": depth,
        "BlocksScenario": scenario, "RequiredAction": action, "CanDefer": defer, "Notes": notes,
    })
    blocker_seq += 1


for row in roster:
    label = row["DevelopmentDisplayName"]
    pid = row["CanonicalPlaceId"]
    if label == "洛阳":
        add_blocker(pid, "IMPLEMENTATION_BLOCKER", "Formal family/branch spatial references are not yet migrated into the Luoyang runtime package.", "S1", "D5", "184", "Run LUOYANG-184-DEVELOPMENT-READINESS-REVIEW-V1, then implement the approved migration.", "NO")
        add_blocker(pid, "DESIGN_BLOCKER", "Some historical building appearances remain UNKNOWN; art readiness is independent from historical readiness.", "S2", "D5_ART", "184|189|190", "Freeze an evidence-labelled art brief without inventing historical facts.", "YES")
    elif row["DevelopmentDepth"] == "D4" and pid.startswith("geo.site."):
        add_blocker(pid, "DATA_MAPPING_BLOCKER", "Existing geo.site reference needs final CanonicalPlace/Cell extent review before runtime materialization.", "S1", "D4", "|".join(str(x[0]) for x in STATE_PLANS[label]), "Resolve physical extent and stable Cell binding; retain current stable reference meanwhile.", "NO" if row["RecommendedWave"] == "WAVE_0" else "YES")
    elif row["DevelopmentDepth"] == "D4":
        add_blocker(pid, "IMPLEMENTATION_BLOCKER", "No formal local Cell/Facility/runtime initialization package exists.", "S1", "D4", "|".join(str(x[0]) for x in STATE_PLANS[label]), "Build only after the place-specific readiness review.", "YES")
    if row["CanonicalConflictStatus"] == "OPEN_MAPPING_CONFLICT":
        add_blocker(pid, "HISTORICAL_RESEARCH_BLOCKER", "Strategic label mapping remains open and may vary by Scenario.", "S1" if row["DevelopmentPriority"] in {"P0", "P1"} else "S2", row["DevelopmentDepth"], "ALL_RELEVANT", "Resolve the existing crosswalk conflict without creating a duplicate Place.", "NO" if row["DevelopmentPriority"] in {"P0", "P1"} else "YES")


region_slices = [
    ("LUOYANG_HULAO", "place.han140.sili.henan.luoyang|geo.site.hulao|geo.site.hangu", "洛阳—虎牢—函谷走廊", "184|189|190", "Urban|Political|Pass|Military|Logistics|HistoricalEvent", "WAVE_0"),
    ("XUCHANG_CHENLIU", "place.han140.yuzhou.yingchuan.xu|place.han140.yanzhou.chenliu.chenliu", "许—陈留走廊", "194|196|200", "Urban|Political|Trade|Military|Logistics", "WAVE_1"),
    ("XIANGYANG_FANCHENG", "place.han140.jingzhou.nan.xiangyang|geo.site.fancheng", "襄阳—樊城—汉水", "208|219", "Urban|River|Military|Siege|Logistics", "WAVE_1"),
    ("JIANGLING_YILING", "place.han140.jingzhou.nan.jiangling|geo.site.yiling|geo.site.chibi", "江陵—夷陵—赤壁水陆走廊", "208|219|222", "Urban|Naval|Military|HistoricalEvent|Logistics", "WAVE_2"),
    ("HANZHONG_YANGPING", "place.han140.yizhou.hanzhong.nanzheng|geo.site.yangping", "汉中—阳平关", "214|219|227", "Pass|Military|Agriculture|Logistics", "WAVE_2"),
    ("CHENGDU_JIANGE", "place.han140.yizhou.shu.chengdu|geo.site.jiange|geo.site.jiameng", "成都—剑阁—葭萌", "214|223", "Urban|Pass|Military|Logistics|Political", "WAVE_2"),
    ("HEFEI_RUXU", "place.han140.yangzhou.jiujiang.hefei|geo.site.ruxukou", "合肥—濡须口", "208|214|219", "Urban|Naval|Military|Logistics", "WAVE_2"),
    ("YE_HEBEI_CORE", "place.han140.jizhou.wei.ye|place.han140.jizhou.julu.julu|place.han140.jizhou.zhongshan.lunu", "邺—钜鹿—中山", "184|189|200|207", "Urban|Military|Trade|Political|HistoricalEvent", "WAVE_1"),
]
region_rows = [{
    "RegionSliceId": rid, "IncludedPlaces": places, "IncludedRoutes": routes,
    "CoreScenario": scenarios, "SystemsValidated": systems, "HistoricalValue": "HIGH",
    "DevelopmentValue": "HIGH", "Readiness": "MOSTLY_READY" if wave in {"WAVE_0", "WAVE_1"} else "PARTIAL",
    "EstimatedComplexity": "VERY_HIGH" if rid == "LUOYANG_HULAO" else "HIGH", "RecommendedWave": wave,
    "IsWorldEntity": "NO_DEVELOPMENT_WORK_PACKAGE_ONLY",
} for rid, places, routes, scenarios, systems, wave in region_slices]


wave_plan = [
    {"Wave": "WAVE_0", "Place / RegionSlice": "LUOYANG_HULAO", "TargetDepth": "洛阳D5 + 虎牢D4 + 函谷D3", "CoreScenario": "184|189|190", "WhyNow": "Only place with formal population/facility/runtime packages; validates flagship living-world and historical transition.", "Dependencies": "Luoyang readiness review; family migration plan; Hulao Cell extent", "ExpectedSystemCoverage": "Urban|Family|Political|Trade|Military|Pass|HistoricalEvent", "Readiness": "READY_FOR_REVIEW", "Blockers": "DPB-001|DPB-002|Hulao mapping blocker"},
    {"Wave": "WAVE_1", "Place / RegionSlice": "XIANGYANG_FANCHENG|XUCHANG_CHENLIU|YE_HEBEI_CORE|长安", "TargetDepth": "D4 anchors + D3 supporting places", "CoreScenario": "189|194|196|200|208|219", "WhyNow": "Maximum reuse after Luoyang while adding river siege, imperial court and northern political centers.", "Dependencies": "Wave 0 contracts; per-place Cell/Facility packages", "ExpectedSystemCoverage": "Urban|Political|River|Military|Trade|Logistics", "Readiness": "MOSTLY_READY_REFERENCE_ONLY", "Blockers": "Runtime initialization absent"},
    {"Wave": "WAVE_2", "Place / RegionSlice": "JIANGLING_YILING|HANZHONG_YANGPING|CHENGDU_JIANGE|HEFEI_RUXU|建业", "TargetDepth": "D4 regional anchors + D3 corridors", "CoreScenario": "208|214|219|223|229", "WhyNow": "Adds naval, mountain-pass and southern polity validation after core runtime patterns stabilize.", "Dependencies": "Travel/logistics network and regional water/pass design", "ExpectedSystemCoverage": "Naval|Pass|Logistics|Military|Agriculture|Political", "Readiness": "PARTIAL", "Blockers": "Research, Cell, art and runtime packages"},
    {"Wave": "WAVE_3", "Place / RegionSlice": "Remaining D3 and high-value D2 places", "TargetDepth": "D3/D2", "CoreScenario": "Place-specific", "WhyNow": "Broadens accessible world after deep-place production pipeline is proven.", "Dependencies": "Reusable D2/D3 templates", "ExpectedSystemCoverage": "Regional trade|local administration|missions|travel", "Readiness": "PARTIAL", "Blockers": "Place-specific gaps"},
    {"Wave": "WAVE_4", "Place / RegionSlice": "Long-range D3/D2 places", "TargetDepth": "D3/D2", "CoreScenario": "Place-specific", "WhyNow": "Long-term geographic breadth, not a historical-importance ranking.", "Dependencies": "Earlier waves", "ExpectedSystemCoverage": "Frontier and peripheral systems", "Readiness": "RESEARCH_REQUIRED", "Blockers": "Reference and implementation cost"},
    {"Wave": "RESERVE", "Place / RegionSlice": "Selected D2 reserve", "TargetDepth": "D2", "CoreScenario": "Inherited baseline", "WhyNow": "Retain as explicit future candidate without inflating active scope.", "Dependencies": "Future priority decision", "ExpectedSystemCoverage": "Accessible place", "Readiness": "PARTIAL", "Blockers": "Not scheduled"},
]


nonurban_master = []
for row in roster:
    if row["CanonicalPlaceId"].startswith("geo.site."):
        source = transport_by_id[row["CanonicalPlaceId"]]
        nonurban_master.append({
            "CanonicalPlaceId": row["CanonicalPlaceId"], "CanonicalName": row["CanonicalName"],
            "PhysicalRoles": row["PhysicalRoles"], "DevelopmentDepth": row["DevelopmentDepth"],
            "DevelopmentPriority": row["DevelopmentPriority"], "RecommendedWave": row["RecommendedWave"],
            "EvidenceType": source["evidence_type"], "Confidence": source["confidence"],
            "ParentLocationReference": source["parent_location"], "RosterDecision": "IN_ROSTER",
            "Blocker": "CanonicalPlace/Cell extent review required before runtime implementation",
        })
for source in deep["military_spaces"]:
    if source["military_space_id"] in {"milspace.red_cliffs", "milspace.xiangfan", "milspace.yiling", "milspace.tong_pass", "milspace.hanzhong"}:
        continue
    nonurban_master.append({
        "CanonicalPlaceId": "UNRESOLVED_PHYSICAL_PLACE", "CanonicalName": source["name"],
        "PhysicalRoles": source["space_type"], "DevelopmentDepth": "UNASSIGNED",
        "DevelopmentPriority": "P2" if int(source["start_year"]) <= 234 else "P3", "RecommendedWave": "DEFERRED",
        "EvidenceType": source["evidence_type"], "Confidence": source["geometry_status"],
        "ParentLocationReference": source["related_city_ids"], "RosterDecision": "DEFER_PENDING_CANONICAL_PLACE",
        "Blocker": "MilitarySpace/region is not automatically a CanonicalPlace; resolve battlefield/ford/corridor physical target first.",
    })


gaps = []
for row in blockers:
    if row["BlockerType"] in {"HISTORICAL_RESEARCH_BLOCKER", "DATA_MAPPING_BLOCKER"}:
        gaps.append({
            "Place": row["PlaceId"], "Gap": row["Description"], "Importance": row["Severity"],
            "BlocksWave": next((r["RecommendedWave"] for r in roster if r["CanonicalPlaceId"] == row["PlaceId"]), "DEFERRED"),
            "ResearchCost": "MEDIUM" if row["BlockerType"] == "DATA_MAPPING_BLOCKER" else "HIGH",
            "SuggestedResolution": row["RequiredAction"],
        })
for source in nonurban_master:
    if source["RosterDecision"] == "DEFER_PENDING_CANONICAL_PLACE":
        gaps.append({
            "Place": source["CanonicalName"], "Gap": source["Blocker"], "Importance": "S2",
            "BlocksWave": "DEFERRED", "ResearchCost": "MEDIUM", "SuggestedResolution": "Resolve one physical Place/Cell target only when the battlefield enters an approved wave.",
        })


d4_d5 = [row for row in roster if row["DevelopmentDepth"] in {"D4", "D5"}]
d2_d3 = [row for row in roster if row["DevelopmentDepth"] in {"D2", "D3"}]


manifest_specs = {}
for row in d4_d5:
    label = row["DevelopmentDisplayName"]
    states = [s for s in historical_state_plan if s["PlaceId"] == row["CanonicalPlaceId"]]
    blockers_for_place = [b["BlockerId"] for b in blockers if b["PlaceId"] == row["CanonicalPlaceId"]]
    manifest_specs[label] = {
        "CanonicalPlaceId": row["CanonicalPlaceId"],
        "DevelopmentDepth": row["DevelopmentDepth"],
        "DevelopmentPriority": row["DevelopmentPriority"],
        "RecommendedWave": row["RecommendedWave"],
        "HistoricalStatePlan": ", ".join(f"{s['ScenarioYear']}:{s['SupportLevel']}/{s['RequiredSnapshotDepth']}" for s in states),
        "SupportedScenarios": ", ".join(str(s["ScenarioYear"]) for s in states),
        "ReferenceReadiness": next(r["OverallReadiness"] for r in readiness if r["PlaceId"] == row["CanonicalPlaceId"]),
        "Blockers": "|".join(blockers_for_place) or "NONE_RECORDED",
        "RecommendedDevelopmentScope": row["SystemValidationValue"],
    }


depth_counts = Counter(row["DevelopmentDepth"] for row in roster)
priority_counts = Counter(row["DevelopmentPriority"] for row in roster)
wave_counts = Counter(row["RecommendedWave"] for row in roster)


registry_updates = {
    "document_registry": [{
        "DocumentId": "DOC-HIST-DEV-PLACE-ROSTER-V1", "Path": "Docs/HISTORICAL_WORLD_REFERENCE/DEVELOPMENT_PLACE_ROSTER_V1/README.md",
        "Title": "Development Place Roster and Reference Readiness V1", "AuthorityLevel": "L2", "DocumentType": "Current Development Input",
        "Status": "CURRENT", "Domain": "HistoricalWorldDevelopment", "CanonicalScope": "DevelopmentDepth|Roster|Readiness|Wave",
        "Supersedes": "", "SupersededBy": "", "Notes": "Curated production scope; not historical rank, Place type, runtime implementation or world fact.",
    }],
    "domain_map": [{
        "DomainId": "DOMAIN-DEVELOPMENT-PLACE", "DomainName": "Development Place Planning", "L0Authority": "AGENTS.md",
        "L1CanonicalSpec": "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", "L2CurrentInput": "Docs/HISTORICAL_WORLD_REFERENCE/DEVELOPMENT_PLACE_ROSTER_V1/README.md",
        "L3Reference": "Docs/HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md",
        "L4History": "Docs/TASK_DEVELOPMENT_PLACE_ROSTER_AND_REFERENCE_READINESS_V1.md", "CurrentStatus": "ROSTER_FROZEN_V1",
        "CanonicalGap": "Runtime implementation remains place/wave specific.", "Notes": "D0-D5 is project production depth only.",
    }],
    "design_decisions": [
        ("DEC-DEVPLACE-001", "DevelopmentDepth != AdministrativeRank", "Development depth does not follow province/commandery/county rank."),
        ("DEC-DEVPLACE-002", "DevelopmentDepth != PhysicalType", "Settlement, pass, harbor or battlefield identity does not determine depth."),
        ("DEC-DEVPLACE-003", "Non-urban D4/D5 eligibility", "Non-urban places may be D4/D5 when history, systems and gameplay justify it."),
        ("DEC-DEVPLACE-004", "77 labels are not the roster", "Strategic display labels contribute recognition only and are not automatically DevelopmentPlaces."),
        ("DEC-DEVPLACE-005", "133 core settlements are not the roster", "Core settlement coverage does not automatically imply D3+ production scope."),
        ("DEC-DEVPLACE-006", "Roster is curated subset", "DevelopmentPlaceRoster is a curated production subset of unified-world Places and site references."),
        ("DEC-DEVPLACE-007", "Per-place historical-state support", "Scenario and ChangePoint support is prioritized per place instead of copied across all 13 scenarios."),
        ("DEC-DEVPLACE-008", "D5 is rare flagship", "D5 is a rare full living-world target; V1 freezes only Luoyang as D5."),
        ("DEC-DEVPLACE-009", "Wave is planning only", "DevelopmentWave is project order, not world hierarchy or historical importance."),
    ],
    "open_decisions": [
        {"DecisionId": "OPEN-DEVPLACE-001", "Domain": "DevelopmentPlace", "Question": "Should low-confidence Xiakou remain D4 after dedicated physical-place research?", "Status": "OPEN", "DecisionOwner": "HistoricalWorld+Map", "NeededEvidence": "Canonical extent, Cell and harbor evidence", "Impact": "WAVE_2 scope", "NextReview": "Before JIANGLING_YILING implementation", "Notes": "Does not affect Wave 0."},
        {"DecisionId": "OPEN-DEVPLACE-002", "Domain": "DevelopmentPlace", "Question": "Which battlefield regions become independent CanonicalPlaces instead of region slices?", "Status": "OPEN", "DecisionOwner": "HistoricalWorld+Military", "NeededEvidence": "Physical target and Cell extent", "Impact": "D3/D4 non-urban roster", "NextReview": "Only when an affected wave starts", "Notes": "Guandu/Jieting/Wuzhangyuan remain deferred."},
    ],
    "implementation_gaps": [{
        "GapId": "IMP-GAP-DEVPLACE-001", "Domain": "DevelopmentPlace", "RequiredContract": "Per-place Cell/Facility/runtime initialization behind approved readiness review",
        "CurrentImplementation": "Only Luoyang has formal urban/metropolitan packages; other roster entries are references.", "Severity": "HIGH",
        "Blocks": "D4/D5 runtime implementation", "RecommendedTask": "LUOYANG-184-DEVELOPMENT-READINESS-REVIEW-V1", "Status": "OPEN",
        "Notes": "This task intentionally does not implement places or upgrade saves.",
    }],
    "research_gaps": [{
        "GapId": "RES-GAP-DEVPLACE-001", "Domain": "DevelopmentPlace", "Question": "Resolve physical Place/Cell extents for approved non-urban D4 candidates.",
        "EvidenceNeeded": "Historical geography + route + archaeological/topographic evidence", "Priority": "P0_FOR_HULAO_P1_FOR_LATER_WAVES",
        "Blocks": "Non-urban D4 runtime materialization", "Status": "OPEN", "Notes": "Do not research all deferred military spaces now.",
    }],
}
registry_updates["design_decisions"] = [{
    "DecisionId": did, "Domain": "DevelopmentPlace", "Title": title, "Decision": decision,
    "Status": "FROZEN", "EffectiveFrom": "2026-08-11",
    "SourceDocument": "Docs/TASK_DEVELOPMENT_PLACE_ROSTER_AND_REFERENCE_READINESS_V1.md",
    "SupersedesDecisionId": "", "AffectedDocuments": "Master|HistoricalWorldReference|KnowledgeBase|DevelopmentManifests",
    "AffectedSystems": "World|Map|Scenario|Presentation|ProjectPlanning",
} for did, title, decision in registry_updates["design_decisions"]]


workdata = {
    "roster": roster,
    "historical_state_plan": historical_state_plan,
    "readiness": readiness,
    "blockers": blockers,
    "region_slices": region_rows,
    "wave_plan": wave_plan,
    "d4_d5": d4_d5,
    "d2_d3": d2_d3,
    "nonurban": nonurban_master,
    "reference_gaps": gaps,
    "manifest_specs": manifest_specs,
    "registry_updates": registry_updates,
    "sources": admin["sources"],
    "summary": {
        "roster_count": len(roster), "depth_counts": dict(sorted(depth_counts.items())),
        "priority_counts": dict(sorted(priority_counts.items())), "wave_counts": dict(sorted(wave_counts.items())),
        "d4_d5_count": len(d4_d5), "d2_d3_count": len(d2_d3), "nonurban_in_roster": len(NON_URBAN),
        "deferred_nonurban": sum(1 for row in nonurban_master if row["RosterDecision"] != "IN_ROSTER"),
        "historical_state_count": len(historical_state_plan), "blocker_count": len(blockers),
        "ready_for_implementation": [row["PlaceName"] for row in readiness if row["OverallReadiness"] == "READY_FOR_IMPLEMENTATION"],
        "formal_scenario_years": scenario_years,
    },
}


OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
DOC_ROOT.mkdir(parents=True, exist_ok=True)
MANIFEST_ROOT.mkdir(parents=True, exist_ok=True)
(OUTPUT_ROOT / "development_place_roster_workdata.json").write_text(json.dumps(workdata, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUTPUT_ROOT / "generation_summary.json").write_text(json.dumps(workdata["summary"], ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


readme = """# Development Place Roster V1

本目录冻结项目第一次可执行的重点地点开发路线图。

## 核心边界

- `DevelopmentDepth`是制作深度，不是历史行政等级、人口等级或物理类型。
- `DevelopmentPriority / Wave`是项目开发顺序，不是世界层级。
- 77个战略显示名、133个Core Settlement、105个治所和1182县都不自动等于Roster。
- 世界事实仍由`CanonicalPlace + Cell + Facility + Population + Organization`组成。
- `geo.site.*`沿用既有非城市稳定参考；进入运行时前仍需完成CanonicalPlace/Cell范围评审。
- 本轮只冻结资料与开发范围，不实现新Place、Facility、HistoricalChangePackage或存档升级。

## 入口

- `01_DEVELOPMENT_PLACE_ROSTER.xlsx`：正式Roster。
- `02_DEVELOPMENT_PLACE_HISTORICAL_STATE_PLAN.xlsx`：逐Place历史状态支持计划。
- `03_DEVELOPMENT_PLACE_REFERENCE_READINESS_MATRIX.xlsx`：资料/数据/设计/实现准备度。
- `04_DEVELOPMENT_PLACE_BLOCKER_REGISTER.xlsx`：阻塞分类。
- `05_DEVELOPMENT_REGION_SLICE_CANDIDATES.xlsx`：区域开发工作包候选。
- `06_DEVELOPMENT_WAVE_PLAN_V1.xlsx`：开发波次。
- `07_D4_D5_PLACE_MASTER.xlsx`：深度开发名册。
- `08_D2_D3_ACCESSIBLE_PLACE_MASTER.xlsx`：可访问及地区中心名册。
- `09_NON_URBAN_STRATEGIC_PLACE_MASTER.xlsx`：非城市重要地点与暂缓项。
- `10_DEVELOPMENT_PLACE_REFERENCE_GAP_PRIORITY.xlsx`：只影响开发的资料缺口。
- `DEVELOPMENT_PLACE_ROSTER_AND_REFERENCE_READINESS_V1_REPORT.md`：验收结论。

下一阶段固定为`LUOYANG-184-DEVELOPMENT-READINESS-REVIEW-V1`，不再继续扩大全国地点资料库。
"""
(DOC_ROOT / "README.md").write_text(readme, encoding="utf-8")


def names(depth):
    return "、".join(row["DevelopmentDisplayName"] for row in roster if row["DevelopmentDepth"] == depth) or "无"


report = f"""# DEVELOPMENT-PLACE-ROSTER-AND-REFERENCE-READINESS-V1 完成报告

## 1. 冻结结论

正式DevelopmentPlaceRoster共 **{len(roster)}** 个地点：

- D5：{depth_counts.get('D5', 0)}（{names('D5')}）
- D4：{depth_counts.get('D4', 0)}（{names('D4')}）
- D3：{depth_counts.get('D3', 0)}
- D2：{depth_counts.get('D2', 0)}
- D1：{depth_counts.get('D1', 0)}；本轮没有为了数量把普通模拟地点塞入专项Roster。

其余统一世界地点继续以D0/D1底层事实和模拟存在，不因未进入Roster而消失。

## 2. 完整城市/聚落开发对象

D5完整Living World：洛阳。

D4城市型重点Place：长安、邺、许/许昌、成都、襄阳、江陵、建业、合肥、汉中实际治所南郑。

这些名称表示项目重点开发Place，不是新的底层City等级；州治、郡治、县治仍是Scenario相关AdministrativeRole。

## 3. D4非城市地点

虎牢、樊城、夏口、阳平关、剑阁、濡须口被冻结为D4目标。它们验证关隘、河流、港渡、水军、城防和补给；D4不等于“大城市”。除虎牢属于Wave 0外，其余必须在所属波次前完成物理范围和Cell评审。

函谷关、潼关、武关、陈仓、葭萌、夷陵、赤壁当前为D3。官渡、白马—延津、街亭、五丈原等仍是MilitarySpace/Region Reference，未解析成独立Physical Place前不进入正式Roster。

## 4. Strategic Label排除规则

77个Strategic Label不等于77个开发地点。`ADMIN_REGION_AS_STRATEGIC_LABEL`和`MOVING_SEAT_REGION_LABEL`只提供玩家认知与Scenario语义；真正开发目标是交叉表指向的CanonicalPlace。城阳、西平、江夏、公安、庐江、建安、梓潼的既有映射冲突保持OPEN，没有制造第二套Place。

## 5. 历史状态计划

共规划 **{len(historical_state_plan)}** 条Place状态支持。D2普通可访问地点通常只有184继承基础状态；D3按战役/区域价值增加；D4/D5才拥有多Scenario或Major ChangePoint专项状态。

洛阳重点状态：140、184、189、190、194、249；其中184/189/190进入旗舰级支持。其他P0地点按迁都、政权中心形成、围城、关隘和水战节点选择状态，不机械复制13个Scenario。

## 6. 准备度与暂缓

明确`READY_FOR_IMPLEMENTATION`的地点只有 **洛阳**，含义是可进入正式Readiness Review，并不等于D5全部实现完成。长安、邺、许、成都、襄阳、江陵、建业等资料较成熟，但仍缺正式Cell/Facility/runtime初始化包。

应暂缓：未解析CanonicalPlace的战场/走廊、低置信非城市范围、存在战略映射冲突且影响具体状态的地点，以及没有Cell或运行时初始化底座的后续D4。暂缓是开发门禁，不是删除世界地点。

## 7. 洛阳第一轮边界

Wave 0采用`LUOYANG_HULAO`开发工作包：

- 核心CanonicalPlace：`place.han140.sili.henan.luoyang`（D5）。
- 独立周边Place：`geo.site.hulao`（虎牢，D4）和`geo.site.hangu`（函谷，D3）；它们不与洛阳合并。
- 县域/连续区域：洛阳县核心、河南尹首都生活圈，以及已存在的270,000城市与400,000近郊包；不自动生成700,000供应区。
- 状态：140、184、189、190、194、249；重点处理184动员、189宫廷危机、190迁都/焚毁和249高平陵政治军事空间。
- Person/Clan/Family：复用既有HistoricalPerson、七个洛阳FamilyOrganization候选和Family Spatial引用，不复制人物或家族。
- Facility：复用宫城、南北宫、太学、官署、市场、仓储、城墙、十二门、道路、住宅与军政设施稳定ID。
- 交通：洛水、黄河/孟津方向、虎牢东向、函谷—长安西向走廊。

虎牢明确属于洛阳第一开发波次的Region Slice，但仍是独立Place；其Cell范围是Wave 0阻塞项。

## 8. 开发波次

- Wave 0：洛阳—虎牢—函谷。
- Wave 1：襄阳—樊城、许—陈留、邺—河北核心、长安。
- Wave 2：江陵—夷陵—赤壁、汉中—阳平、成都—剑阁、合肥—濡须、建业。
- Wave 3/4：按可复用模板扩展D3/D2；Reserve不承诺开工。

Wave只表示项目顺序，不表示历史价值和世界层级。

## 9. 任务边界

本轮没有实现D4/D5、没有生成新城市/Facility/FamilyCenter、没有实现HistoricalChangePackage、没有修改Unity或Save。运行时缺口已进入Blocker/Implementation Gap。

## 10. 下一阶段

停止继续扩大地点资料库，进入`LUOYANG-184-DEVELOPMENT-READINESS-REVIEW-V1`。Review通过后再进入`LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1`及洛阳Living World实际开发。
"""
(DOC_ROOT / "DEVELOPMENT_PLACE_ROSTER_AND_REFERENCE_READINESS_V1_REPORT.md").write_text(report, encoding="utf-8")


slug_map = {
    "洛阳": "LUOYANG_184", "长安": "CHANGAN", "邺": "YE", "许昌": "XU", "成都": "CHENGDU",
    "襄阳": "XIANGYANG", "江陵": "JIANGLING", "建业": "JIANYE", "合肥": "HEFEI", "汉中": "HANZHONG",
    "虎牢": "HULAO", "樊城": "FANCHENG", "夏口": "XIAKOU", "阳平关": "YANGPING_PASS",
    "剑阁": "JIANGE", "濡须口": "RUXUKOU",
}
existing_labels = {"洛阳", "长安", "邺", "许昌", "成都", "襄阳", "江陵", "建业"}
for label, spec in manifest_specs.items():
    filename = f"{slug_map[label]}_DEVELOPMENT_REFERENCE_MANIFEST.md"
    path = MANIFEST_ROOT / filename
    section = f"""\n\n## Development Place Roster V1\n\n| Field | Frozen value |\n|---|---|\n| CanonicalPlaceId | `{spec['CanonicalPlaceId']}` |\n| DevelopmentDepth | {spec['DevelopmentDepth']} |\n| DevelopmentPriority | {spec['DevelopmentPriority']} |\n| RecommendedWave | {spec['RecommendedWave']} |\n| HistoricalStatePlan | {spec['HistoricalStatePlan']} |\n| SupportedScenarios / TimePoints | {spec['SupportedScenarios']} |\n| ReferenceReadiness | {spec['ReferenceReadiness']} |\n| Blockers | {spec['Blockers']} |\n| RecommendedDevelopmentScope | {spec['RecommendedDevelopmentScope']} |\n| RuntimeBoundary | 这是开发目标与资料入口，不表示运行时已经实现。 |\n"""
    marker = "## Development Place Roster V1"
    if path.exists():
        text = path.read_text(encoding="utf-8")
        if marker in text:
            text = text[:text.index(marker)].rstrip() + "\n"
        path.write_text(text.rstrip() + section, encoding="utf-8")
    else:
        title = label + " Development Reference Manifest"
        text = f"""# {title}\n\n## Document Governance\n\n- Purpose：为{label}后续开发提供唯一资料入口。\n- Authority：L2 Current Development Input Manifest\n- Covers：CanonicalPlace、历史状态、资料准备度、阻塞项与建议开发范围。\n- DoesNotCover：新的历史事实、运行时Place/Facility实现或存档升级。\n- RelatedCanonicalDocs：../README_PROJECT_KNOWLEDGE_BASE.md\n- Status：CURRENT\n\n| Field | Reference |\n|---|---|\n| TargetPlace | {label} |\n| HistoricalReferenceDocs | `Docs/HISTORICAL_WORLD_REFERENCE/DEVELOPMENT_PLACE_ROSTER_V1/README.md` |\n| ExistingImplementation | Reference only; no formal runtime initialization. |\n| DoNotInfer | D级不是历史城市等级；Reference不是Implementation。 |\n""" + section
        path.write_text(text, encoding="utf-8")


print(json.dumps(workdata["summary"], ensure_ascii=False, indent=2))
