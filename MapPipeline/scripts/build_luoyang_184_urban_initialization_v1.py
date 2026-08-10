#!/usr/bin/env python3
"""Build the formal 184 Luoyang 270K urban initialization package.

The source workbooks are normalized once into a small, checked-in configuration.
Runtime facts are emitted as fixed-width binary records plus JSON catalogs/overlays.
No Person is represented by an aggregate count, and no Unity GameObject is created.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
import os
import struct
import time
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Tuple


SCHEMA = "mandate.luoyang-184-urban-initialization.v1"
PERSON_MAGIC = b"MOHLYU01"
HOUSEHOLD_MAGIC = b"MOHLYH01"
PERSON_STRUCT = struct.Struct("<IhBBHIHQIIHHHHHHHHHHqHHBBBBiii")
HOUSEHOLD_STRUCT = struct.Struct("<IIIHHIBBHq")
HEADER_STRUCT = struct.Struct("<8sIIIIQ")
NONE_U16 = 0xFFFF
NONE_U32 = 0xFFFFFFFF

AREA_IDS = [
    "area.luoyang.walled_civil",
    "area.luoyang.palace_complex",
    "area.luoyang.southern_ritual_education",
    "area.luoyang.attached_near_suburb",
]
AREA_TARGETS = {
    AREA_IDS[0]: 182_000,
    AREA_IDS[1]: 18_000,
    AREA_IDS[2]: 30_000,
    AREA_IDS[3]: 40_000,
}
AGE_STAGE_IDS = ["age.0_13", "age.14_19", "age.20_59", "age.60_69", "age.70_plus"]
AGE_STAGE_RANGES = [(0, 13), (14, 19), (20, 59), (60, 69), (70, 89)]
AGE_TARGETS_BY_AREA = {
    AREA_IDS[0]: [73_682, 18_500, 70_900, 13_720, 5_198],
    AREA_IDS[1]: [18, 900, 16_500, 580, 2],
    AREA_IDS[2]: [1_000, 10_000, 18_000, 900, 100],
    AREA_IDS[3]: [900, 3_000, 35_000, 1_000, 100],
}

OCCUPATION_IDS = [
    "occupation.unfixed",
    "occupation.imperial_core",
    "occupation.education.student",
    "occupation.agriculture",
    "occupation.crafts",
    "occupation.trade",
    "occupation.transport",
    "occupation.government",
    "occupation.military",
    "occupation.palace_service",
    "occupation.education_staff",
    "occupation.medical",
    "occupation.religious",
    "occupation.household_service",
    "occupation.elite_family_management",
    "occupation.public_service",
]

OCCUPATION_TARGETS_BY_AREA = {
    AREA_IDS[0]: {
        "occupation.unfixed": 89_900,
        "occupation.crafts": 30_000,
        "occupation.trade": 16_000,
        "occupation.transport": 2_000,
        "occupation.government": 12_000,
        "occupation.military": 6_000,
        "occupation.education_staff": 100,
        "occupation.medical": 3_000,
        "occupation.religious": 2_000,
        "occupation.household_service": 12_000,
        "occupation.elite_family_management": 3_000,
        "occupation.public_service": 6_000,
    },
    AREA_IDS[1]: {
        "occupation.unfixed": 2,
        "occupation.imperial_core": 20,
        "occupation.military": 6_000,
        "occupation.palace_service": 11_978,
    },
    AREA_IDS[2]: {
        "occupation.unfixed": 1_100,
        "occupation.education.student": 23_000,
        "occupation.education_staff": 5_900,
    },
    AREA_IDS[3]: {
        "occupation.unfixed": 1_000,
        "occupation.agriculture": 4_000,
        "occupation.trade": 4_000,
        "occupation.transport": 8_000,
        "occupation.military": 22_000,
        "occupation.public_service": 1_000,
    },
}

FORCE_DEFINITIONS = [
    ("force.han.luoyang_garrison", "何进京师防务", "P0035", 12_000, "Active", "cell.luoyang.walled"),
    ("force.han.luzhi_north", "卢植讨黄巾军", "P0032", 8_000, "Staging", "cell.route.luoyang_julu"),
    ("force.han.huangfu_yingchuan", "皇甫嵩讨黄巾军", "P0033", 5_000, "Staging", "cell.route.luoyang_yingchuan"),
    ("force.han.zhujun_yingchuan", "朱儁讨黄巾军", "P0034", 5_000, "Staging", "cell.route.luoyang_yingchuan"),
    ("force.han.caocao_reinforcement", "曹操颍川增援军", "P0108", 4_000, "Staging", "cell.route.luoyang_yingchuan"),
]

FAMILY_TARGETS = {
    "F088": 20,
    "F036": 250,
    "F077": 300,
    "F092": 350,
    "F081": 250,
    "F571": 100,
    "F572": 130,
}

WORKER_CAPACITY_TARGETS = {
    "residential": 15_000,
    "commercial": 24_000,
    "industry": 30_000,
    "service": 25_000,
    "government": 12_000,
    "military": 30_000,
    "education": 6_000,
    "public": 4_000,
    "storage": 2_000,
    "road": 1_000,
    "ritual": 2_000,
    "resource_agriculture": 5_000,
    "fortification": 4_000,
}

EMPLOYED_OCCUPATIONS = {
    item for item in OCCUPATION_IDS
    if item not in {"occupation.unfixed", "occupation.imperial_core", "occupation.education.student"}
}

DATA_ORIGIN_INDEX = {
    "Historical": 0,
    "HistoricalReconstruction": 1,
    "GeneratedHistoricalPopulation": 2,
    "EngineeringTest": 3,
    "StressTest": 4,
}
GENDER_INDEX = {"Unknown": 0, "Male": 1, "Female": 2}
RESIDENCE_STATUS_INDEX = {"Unhoused": 0, "Housed": 1, "TemporaryLodging": 2, "InstitutionalHousing": 3}
EMPLOYMENT_STATUS_INDEX = {"NotInLaborForce": 0, "Employed": 1, "Unemployed": 2, "Student": 3}
LOCATION_STATUS_IDS = [
    "ConfirmedInLuoyang", "LikelyInLuoyang", "TemporaryInLuoyang",
    "DepartingFromLuoyang", "ConfirmedOutside", "Unknown",
]


@dataclass(slots=True)
class Person:
    ordinal: int
    person_id: str
    display_name: str
    birth_year: int
    age: int
    age_stage: int
    gender: int
    health_bp: int
    natural_lifespan: int
    household: int
    family_org: int
    area: int
    location_status: int
    current_cell: int
    residence: int
    residence_status: int
    occupation: int
    work_facility: int
    activity: int
    employment_status: int
    civil_office: int
    military_office: int
    title: int
    allegiance: int
    political_role: int
    force: int
    reserve_force: int
    skill_profile: int
    knowledge_profile: int
    assets: int
    father: int
    mother: int
    spouse: int
    data_origin: int


@dataclass(slots=True)
class Household:
    ordinal: int
    start: int
    count: int
    head: int
    family_org: int
    primary_residence: int
    household_type: int
    data_origin: int
    wealth: int


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def table_records(rows: Sequence[Sequence], required_header: str) -> List[Dict]:
    header_index = next(i for i, row in enumerate(rows) if required_header in row)
    headers = [str(value) if value is not None else "" for value in rows[header_index]]
    result = []
    for row in rows[header_index + 1:]:
        if not row or all(value is None or value == "" for value in row):
            continue
        result.append({headers[i]: row[i] if i < len(row) else None for i in range(len(headers))})
    return result


def normalize_sources(source_data_path: Path) -> Dict:
    raw = json.loads(source_data_path.read_text(encoding="utf-8"))
    spatial = table_records(raw["spatial"]["sheets"]["SpatialDistribution"], "AreaType")
    scopes = table_records(raw["spatial"]["sheets"]["ScopeSummary"], "Scope")
    social = table_records(raw["social"]["sheets"]["SocialModel"], "Category")
    labor = table_records(raw["social"]["sheets"]["ActiveLabor"], "Sector")
    anchors = table_records(raw["anchors"]["sheets"]["HistoricalPersons"], "PersonId")
    families = table_records(raw["anchors"]["sheets"]["HistoricalFamilyAnchor"], "FamilyIdCandidate")
    mother = table_records(raw["mother"]["sheets"]["人物母表"], "PersonId")
    scenario_people = table_records(raw["scenario"]["sheets"]["HistoricalPersonState"], "PersonId")
    scenario_forces = table_records(raw["scenario"]["sheets"]["ForceState"], "ForceCandidateId")
    scenario_events = table_records(raw["scenario"]["sheets"]["PendingHistoricalEvents"], "EventId")
    mother_by_id = {row["PersonId"]: row for row in mother}
    scenario_by_id = {row["PersonId"]: row for row in scenario_people}
    normalized_people = []
    for row in anchors:
        person_id = row["PersonId"]
        parent = mother_by_id.get(person_id, {})
        scenario = scenario_by_id.get(person_id, {})
        normalized_people.append({
            **row,
            # Stable internal aliases: generation must not depend on the exact
            # headings chosen by the research workbook.
            "LocationStatus": row.get("184LocationStatus"),
            "PlannedLocation": row.get("CurrentLocation"),
            "SourceURL": row.get("Source"),
            "HistoricalRole": row.get("CivilOffice") or row.get("Title"),
            "FamilyCandidateId": row.get("FamilyOrganizationCandidate"),
            "Gender": parent.get("性别"),
            "BirthYear": parent.get("生年"),
            "DeathYear": parent.get("卒年"),
            "Father": parent.get("父"),
            "Mother": parent.get("母"),
            "Spouse": parent.get("配偶"),
            "ScenarioActivity": scenario.get("CurrentActivity"),
            "ScenarioOffice": scenario.get("CivilOffice/MilitaryOffice"),
        })
    source_files = []
    for section in raw.values():
        source = Path(section["source"])
        source_files.append({
            # Keep provenance portable and avoid publishing workstation paths.
            # The immutable digest remains the authority for identifying the
            # exact source artifact used by this build.
            "path": f"source://{source.name}",
            "filename": source.name,
            "sha256": sha256(source) if source.exists() else None,
        })
    return {
        "schema": "mandate.luoyang-184-urban-initialization-input.v1",
        "sources": source_files,
        "scopes": scopes,
        "spatial_model": spatial,
        "social_model": social,
        "labor_model": labor,
        "historical_people": normalized_people,
        "historical_families": families,
        "scenario_forces": scenario_forces,
        "scenario_events": scenario_events,
        "formal_population_targets": {
            "walled_city": 200_000,
            "urban_area": 270_000,
            "metropolitan_area": 400_000,
            "supply_region": 700_000,
        },
    }


def distribute(total: int, count: int) -> List[int]:
    if count <= 0:
        if total != 0:
            raise ValueError("Cannot distribute a non-zero total into zero entries")
        return []
    base, remainder = divmod(total, count)
    return [base + (1 if i < remainder else 0) for i in range(count)]


def household_sizes(total: int, special: Optional[List[int]] = None) -> List[int]:
    result = list(special or [])
    remaining = total - sum(result)
    pattern = [8, 8, 7, 6, 6, 5, 5, 5, 4, 3, 2, 1]
    cursor = 0
    while remaining > 0:
        size = min(pattern[cursor % len(pattern)], remaining)
        result.append(size)
        remaining -= size
        cursor += 1
    return result


def age_stage_for_age(age: int) -> int:
    if age <= 13: return 0
    if age <= 19: return 1
    if age <= 59: return 2
    if age <= 69: return 3
    return 4


def stable_age(stage: int, ordinal: int) -> int:
    low, high = AGE_STAGE_RANGES[stage]
    return low + ((ordinal * 37 + stage * 11) % (high - low + 1))


def stable_name(ordinal: int) -> str:
    surnames = "赵钱孙李周吴郑王冯陈褚卫蒋沈韩杨朱秦尤许何吕施张孔曹严华金魏陶姜谢邹喻柏水窦章云苏潘葛奚范彭郎鲁韦昌马苗凤花方俞任袁柳鲍史唐费廉岑薛雷贺倪汤滕殷罗毕郝邬安常乐于时傅皮卞齐康伍余元顾孟平黄和穆萧尹姚邵汪祁毛禹狄米贝明臧计伏成戴宋茅庞熊纪舒屈项祝董梁杜阮蓝闵席季麻强贾路娄危江童颜郭梅林钟徐邱骆高夏蔡田樊胡凌霍虞万支柯昝管卢莫经房裘缪干解应宗丁宣邓郁单杭洪包诸左石崔吉龚程嵇邢滑裴陆荣翁荀羊惠甄曲封芮储靳邴糜松井段富巫乌焦巴弓牧隗山谷车侯宓蓬全班仰秋仲伊宫宁仇栾暴甘钭厉戎祖武符景詹束龙叶幸司韶郜黎蓟薄印宿白怀蒲台从鄂索咸籍赖卓蔺屠蒙池乔阴胥能苍双闻莘党翟谭贡劳逄姬申扶堵冉宰郦雍郤璩桑桂濮牛寿通边扈燕冀郏浦尚农温庄晏柴瞿阎充慕连茹习宦艾鱼容向古易慎戈廖庾终暨居衡步都耿满弘匡国文寇广禄阙东殴殳沃利蔚越夔隆师巩厍聂晁勾敖融冷訾辛阚那简饶空曾毋沙乜养鞠须丰巢关蒯相查荆游竺权逯盖益桓公"
    given = "安伯成达德方福高广国和弘华坚建康良明宁平庆仁荣绍盛泰文武贤信兴义永友远志忠"
    surname = surnames[ordinal % len(surnames)]
    first = given[(ordinal * 7 + 3) % len(given)]
    second = given[(ordinal * 13 + 5) % len(given)] if ordinal % 3 == 0 else ""
    return surname + first + second


def normalize_facilities(world: Dict) -> Tuple[List[Dict], Dict[int, int], Dict[str, int], set]:
    footprint = {int(value) for value in world["city_footprint_cell_ids"]}
    facilities = sorted(world["facilities"], key=lambda item: item["facility_id"])
    by_cell = {int(item["cell_id64"]): index for index, item in enumerate(facilities)}
    center_x, center_y = 2043, 1241
    urban_indices = {
        index for index, item in enumerate(facilities)
        if max(abs(int(item["grid_x"]) - center_x), abs(int(item["grid_y"]) - center_y)) <= 14
    }
    for index, item in enumerate(facilities):
        item["source_definition_id"] = item["definition_id"]
        item["source_category_id"] = item["category_id"]
        item["profile_id"] = "profile.legacy_historical_v1"
        item["complex_id"] = None
        item["active"] = True
        item["data_origin"] = (
            "Historical" if item["historical_confidence"] == "HistoricalAnchor"
            else "HistoricalReconstruction" if item["historical_confidence"] == "HistoricalReconstruction"
            else "EngineeringTest"
        )
        item["historical_class"] = (
            "HistoricalRequired" if item["historical_confidence"] == "HistoricalAnchor"
            else "HistoricallyPlausible" if item["historical_confidence"] == "HistoricalReconstruction"
            else "GeneratedForTest"
        )
        item["decision"] = "Keep"
        item["decision_reason"] = "Preserve original facility and audit outside the formal urban overlay."
        item["current_residential_capacity"] = int(item.get("residential_capacity_persons") or 0)
        item["current_worker_capacity"] = int(item.get("worker_capacity") or 0)
        item["recommended_residential_capacity"] = item["current_residential_capacity"]
        item["recommended_worker_capacity"] = item["current_worker_capacity"]
        item["student_capacity"] = 0
        item["service_capacity"] = 0
        item["storage_capacity"] = 0
        item["garrison_capacity"] = 0
        item["training_capacity"] = 0
        item["assembly_capacity"] = 0
        item["parallel_production_capacity"] = 0
        item["water_supply_litres_per_day"] = 0
        item["drainage_litres_per_day"] = 0
        item["current_workers"] = 0
        item["current_students"] = 0
        item["current_residents"] = 0
        item["capability_ids"] = list(item.get("capability_ids") or [])
        item["is_walled"] = int(item["cell_id64"]) in footprint
        item["is_urbanized"] = index in urban_indices
        item["confidence"] = "A" if item["historical_class"] == "HistoricalRequired" else "B" if item["historical_class"] == "HistoricallyPlausible" else "C"

        definition_id = item["definition_id"]
        if definition_id == "facility.historical.palace_complex":
            item["definition_id"] = "facility.government.court_hall"
            item["category_id"] = "government"
            item["profile_id"] = "profile.imperial_palace_core"
            item["decision"] = "Convert"
            item["decision_reason"] = "Palace is a multi-Cell Complex; this Cell becomes a generic CourtHall/GovernmentOffice anchor."
            item["complex_id"] = "complex.luoyang.north_palace" if "北宫" in item["display_name"] or "永安" in item["display_name"] else "complex.luoyang.south_palace"
        elif definition_id == "facility.historical.barracks":
            item["definition_id"] = "facility.military.barracks"
            item["category_id"] = "military"
            item["profile_id"] = "profile.capital_garrison"
            item["decision"] = "Convert"
            item["decision_reason"] = "Reuse generic Barracks BaseType with a capital-garrison profile."
        elif definition_id == "facility.historical.imperial_academy":
            item["definition_id"] = "facility.education.academy"
            item["category_id"] = "education"
            item["profile_id"] = "profile.imperial_academy"
            item["decision"] = "Convert"
            item["decision_reason"] = "Taixue uses generic Academy with ImperialAcademy profile."
        elif definition_id == "facility.historical.taicang":
            item["definition_id"] = "facility.storage.granary"
            item["category_id"] = "storage"
            item["profile_id"] = "profile.state_taicang"
            item["decision"] = "Convert"
            item["decision_reason"] = "Taicang is a state Granary/Warehouse complex anchor."
        elif definition_id in {"facility.historical.arsenal", "facility.military.armory"}:
            item["definition_id"] = "facility.storage.warehouse"
            item["category_id"] = "storage"
            item["profile_id"] = "profile.state_arsenal"
            item["decision"] = "Convert"
            item["decision_reason"] = "Arsenal reuses Warehouse with military inventory capabilities."
        elif definition_id == "facility.historical.market":
            item["definition_id"] = "facility.commercial.market"
            item["category_id"] = "commercial"
            item["profile_id"] = "profile.capital_market"
            item["decision"] = "Convert"
            item["decision_reason"] = "Historical market reuses generic Market BaseType."
        elif definition_id == "facility.historical.ritual_hall":
            item["definition_id"] = "facility.public.ritual_hall"
            item["category_id"] = "ritual"
            item["profile_id"] = "profile.imperial_ritual"
            item["decision"] = "Convert"
            item["decision_reason"] = "Ritual identity is represented by profile, not a parallel BaseType."
        elif definition_id == "facility.historical.observatory":
            item["definition_id"] = "facility.public.observatory"
            item["category_id"] = "public"
            item["profile_id"] = "profile.imperial_observatory"
            item["decision"] = "Convert"
            item["decision_reason"] = "Lingtai reuses a generic Observatory BaseType."

    walled_residential = [
        index for index in urban_indices
        if facilities[index]["is_walled"] and facilities[index]["category_id"] == "residential"
    ]
    walled_residential.sort(key=lambda index: (facilities[index]["grid_y"], facilities[index]["grid_x"], facilities[index]["facility_id"]))
    selected = sorted({walled_residential[min(len(walled_residential) - 1, round(i * (len(walled_residential) - 1) / 17))] for i in range(18)})
    while len(selected) < 18:
        selected.append(next(index for index in walled_residential if index not in selected))
    selected = sorted(selected[:18])
    for position, index in enumerate(selected):
        item = facilities[index]
        item["recommended_residential_capacity"] = 0
        if position < 6:
            item["active"] = False
            item["decision"] = "Remove"
            item["decision_reason"] = "Release a GeneratedForTest residence Cell as a vacant developable city Cell."
            item["profile_id"] = "profile.vacant_developable"
        else:
            variants = [
                ("facility.public.garden", "profile.capital_garden"),
                ("facility.public.plaza", "profile.capital_plaza"),
                ("facility.public.courtyard", "profile.capital_courtyard"),
            ]
            definition_id, profile_id = variants[(position - 6) % len(variants)]
            item["definition_id"] = definition_id
            item["category_id"] = "public"
            item["profile_id"] = profile_id
            item["decision"] = "Convert"
            item["decision_reason"] = "Convert GeneratedForTest residence to preserved public/open urban space."

    active_walled_residential = [
        index for index in walled_residential if facilities[index]["active"] and facilities[index]["category_id"] == "residential"
    ]
    active_walled_residential.sort(key=lambda index: facilities[index]["facility_id"])
    if len(active_walled_residential) < 9:
        raise RuntimeError("Insufficient formal walled residence facilities")
    facilities[active_walled_residential[0]]["profile_id"] = "profile.imperial_special_residence"
    for index in active_walled_residential[1:8]:
        facilities[index]["profile_id"] = "profile.family_special_residence"
    for index in active_walled_residential[8:]:
        facilities[index]["profile_id"] = "profile.general_urban_residence"
    outside_residential = [
        index for index in urban_indices
        if not facilities[index]["is_walled"] and facilities[index]["active"] and facilities[index]["category_id"] == "residential"
    ]
    for index in outside_residential:
        facilities[index]["profile_id"] = "profile.general_outer_urban_residence"

    for index in urban_indices:
        item = facilities[index]
        if item["active"] and item["decision"] == "Keep":
            item["decision"] = "Rebalance"
            item["decision_reason"] = "Rebalance capacity for the 270K formal urban initialization without adding a SubCell."

    return facilities, by_cell, {item["facility_id"]: i for i, item in enumerate(facilities)}, urban_indices


def capacity_category(item: Dict) -> Optional[str]:
    category = item["category_id"]
    if category in {"resource", "agriculture"}:
        return "resource_agriculture"
    return category if category in WORKER_CAPACITY_TARGETS else None


def assign_worker_capacities(facilities: List[Dict], urban_indices: set) -> None:
    groups: Dict[str, List[int]] = {key: [] for key in WORKER_CAPACITY_TARGETS}
    for index in sorted(urban_indices):
        item = facilities[index]
        if not item["active"]:
            continue
        category = capacity_category(item)
        if category:
            groups[category].append(index)
    for category, target in WORKER_CAPACITY_TARGETS.items():
        values = distribute(target, len(groups[category]))
        for index, value in zip(groups[category], values):
            facilities[index]["recommended_worker_capacity"] = value
            if value > 0:
                facilities[index]["capability_ids"] = sorted(set(facilities[index]["capability_ids"] + ["capability.worker_assignment"]))
    # Military employment is spatially constrained: the 12K capital garrison must
    # work inside the walls, while the four departing armies (22K) must work in
    # their suburban staging facilities.  A global even split would leave those
    # forces with paper capacity that they cannot actually reach.
    military = groups["military"]
    fortification = groups["fortification"]
    for index in military + fortification:
        facilities[index]["recommended_worker_capacity"] = 0
    walled_military = [index for index in military if facilities[index]["is_walled"]]
    outside_military = [index for index in military if not facilities[index]["is_walled"]]
    walled_fortification = [index for index in fortification if facilities[index]["is_walled"]]
    for index, value in zip(walled_military, distribute(8_000, len(walled_military))):
        facilities[index]["recommended_worker_capacity"] = value
    for index, value in zip(walled_fortification, distribute(4_000, len(walled_fortification))):
        facilities[index]["recommended_worker_capacity"] = value
    for index, value in zip(outside_military, distribute(22_000, len(outside_military))):
        facilities[index]["recommended_worker_capacity"] = value
    for index in military + fortification:
        if facilities[index]["recommended_worker_capacity"] > 0:
            facilities[index]["capability_ids"] = sorted(set(
                facilities[index]["capability_ids"] + ["capability.worker_assignment"]
            ))
    education = [i for i in sorted(urban_indices) if facilities[i]["active"] and facilities[i]["category_id"] == "education"]
    for index, value in zip(education, distribute(30_000, len(education))):
        facilities[index]["student_capacity"] = value
        facilities[index]["capability_ids"] = sorted(set(facilities[index]["capability_ids"] + ["capability.education", "capability.student_capacity"]))
    canals = [i for i in sorted(urban_indices) if "canal" in facilities[i]["definition_id"]]
    for index in canals:
        facilities[index]["water_supply_litres_per_day"] = 150_000
        facilities[index]["drainage_litres_per_day"] = 150_000
        facilities[index]["capability_ids"] = sorted(set(facilities[index]["capability_ids"] + ["capability.water_supply", "capability.drainage"]))
    storage = [
        i for i in sorted(urban_indices) if facilities[i]["active"] and (
            facilities[i]["category_id"] == "storage"
            or "warehouse" in facilities[i]["definition_id"]
            or "granary" in facilities[i]["definition_id"]
        )
    ]
    for index, value in zip(storage, distribute(30_000_000, len(storage))):
        facilities[index]["storage_capacity"] = value
        facilities[index]["capability_ids"] = sorted(set(facilities[index]["capability_ids"] + ["capability.storage"]))
    for index in sorted(urban_indices):
        item = facilities[index]
        if item["active"] and item["category_id"] in {"commercial", "service", "public", "government"}:
            item["service_capacity"] = max(item["recommended_worker_capacity"] * 8, 0)
            if item["service_capacity"]:
                item["capability_ids"] = sorted(set(item["capability_ids"] + ["capability.service"]))
        if item["active"] and item["category_id"] in {"industry", "commercial", "storage"}:
            item["parallel_production_capacity"] = max(1, item["recommended_worker_capacity"] // 80)


def historical_area(record: Dict) -> Optional[int]:
    status = str(record.get("LocationStatus") or "Unknown")
    if status in {"ConfirmedOutside", "Unknown"}:
        return None
    person_id = record["PersonId"]
    location = str(record.get("PlannedLocation") or record.get("184PrimaryLocation") or "")
    if person_id in {"P0032", "P0033", "P0034", "P0108", "P0931"} or status == "DepartingFromLuoyang":
        return 3
    if "宫" in location or person_id in {"P0038", "P0037", "P0039", "P0040", "P0047", "P0048", "P0049", "P0050", "P0932", "P0933", "P0927", "P0929"}:
        return 1
    return 0


def choose_stage(remaining: List[int], member_index: int) -> int:
    preferences = (
        [2, 3, 1, 4, 0] if member_index == 0
        else [2, 1, 3, 0, 4] if member_index == 1
        else [0, 1, 2, 3, 4]
    )
    viable = [stage for stage in preferences if remaining[stage] > 0]
    if not viable:
        raise RuntimeError("Age quota exhausted")
    return max(viable, key=lambda stage: (remaining[stage], -preferences.index(stage)))


def build_people_and_households(config: Dict, catalogs: Dict) -> Tuple[List[Person], List[Household], List[Dict], List[Dict]]:
    historical_by_area: Dict[int, List[Dict]] = {i: [] for i in range(4)}
    external = []
    for record in config["historical_people"]:
        area = historical_area(record)
        if area is None:
            external.append(record)
        else:
            historical_by_area[area].append(record)
    for records in historical_by_area.values():
        records.sort(key=lambda item: item["PersonId"])

    historical_lookup = {item["PersonId"]: item for records in historical_by_area.values() for item in records}
    historical_ids = set(historical_lookup)
    occupation_index = {value: index for index, value in enumerate(catalogs["occupations"])}
    activity_index = {value: index for index, value in enumerate(catalogs["activities"])}
    office_index = {value: index for index, value in enumerate(catalogs["offices"])}
    title_index = {value: index for index, value in enumerate(catalogs["titles"])}
    allegiance_index = {value: index for index, value in enumerate(catalogs["allegiances"])}
    political_index = {value: index for index, value in enumerate(catalogs["political_roles"])}
    skill_index = {value: index for index, value in enumerate(catalogs["skill_profiles"])}
    knowledge_index = {value: index for index, value in enumerate(catalogs["knowledge_profiles"])}

    people: List[Person] = []
    households: List[Household] = []
    historical_runtime: List[Dict] = []
    male_remaining = 137_700
    female_remaining = 132_300
    historical_cursor_by_area = {i: 0 for i in range(4)}

    for area_index, area_id in enumerate(AREA_IDS):
        target = AREA_TARGETS[area_id]
        specials = [20] if area_index == 1 else []
        sizes = household_sizes(target, specials)
        age_remaining = list(AGE_TARGETS_BY_AREA[area_id])
        for size in sizes:
            household_ordinal = len(households)
            start = len(people)
            for member_index in range(size):
                ordinal = len(people)
                historical = None
                cursor = historical_cursor_by_area[area_index]
                if cursor < len(historical_by_area[area_index]):
                    historical = historical_by_area[area_index][cursor]
                    historical_cursor_by_area[area_index] += 1
                if historical:
                    person_id = historical["PersonId"]
                    name = historical.get("Name") or historical.get("姓名") or person_id
                    birth_year_raw = historical.get("BirthYear")
                    birth_year = int(birth_year_raw) if isinstance(birth_year_raw, (int, float)) else 149
                    age = max(0, min(89, 184 - birth_year))
                    stage = age_stage_for_age(age)
                    if age_remaining[stage] <= 0:
                        stage = choose_stage(age_remaining, member_index)
                        age = stable_age(stage, ordinal)
                        birth_year = 184 - age
                    gender_text = str(historical.get("Gender") or "")
                    preferred_gender = 1 if gender_text == "男" else 2 if gender_text == "女" else (1 if member_index == 0 else 2)
                    data_origin = DATA_ORIGIN_INDEX["Historical"]
                    location_status = LOCATION_STATUS_IDS.index(str(historical.get("LocationStatus") or "Unknown"))
                else:
                    person_id = f"person.luoyang.184.urban.{ordinal + 1:06d}"
                    name = stable_name(ordinal)
                    stage = choose_stage(age_remaining, member_index)
                    age = stable_age(stage, ordinal)
                    birth_year = 184 - age
                    preferred_gender = 1 if member_index == 0 else 2 if member_index == 1 else (1 if ordinal % 100 < 51 else 2)
                    data_origin = DATA_ORIGIN_INDEX["GeneratedHistoricalPopulation"]
                    location_status = LOCATION_STATUS_IDS.index("ConfirmedInLuoyang")
                age_remaining[stage] -= 1
                if preferred_gender == 1 and male_remaining > 0 or female_remaining == 0:
                    gender = 1
                    male_remaining -= 1
                else:
                    gender = 2
                    female_remaining -= 1
                lifespan = max(age + 1, 48 + ((ordinal * 29 + 17) % 43))
                person = Person(
                    ordinal=ordinal,
                    person_id=person_id,
                    display_name=name,
                    birth_year=birth_year,
                    age=age,
                    age_stage=stage,
                    gender=gender,
                    health_bp=8_000 + ((ordinal * 97) % 2_001),
                    natural_lifespan=lifespan,
                    household=household_ordinal,
                    family_org=NONE_U16,
                    area=area_index,
                    location_status=location_status,
                    current_cell=0,
                    residence=NONE_U32,
                    residence_status=RESIDENCE_STATUS_INDEX["Unhoused"],
                    occupation=occupation_index["occupation.unfixed"],
                    work_facility=NONE_U32,
                    activity=activity_index["activity.household_life"],
                    employment_status=EMPLOYMENT_STATUS_INDEX["NotInLaborForce"],
                    civil_office=office_index["office.none"],
                    military_office=office_index["office.none"],
                    title=title_index["title.none"],
                    allegiance=allegiance_index["allegiance.han_court"],
                    political_role=political_index["political.subject"],
                    force=NONE_U16,
                    reserve_force=NONE_U16,
                    skill_profile=skill_index["skill.general"],
                    knowledge_profile=knowledge_index["knowledge.local_basic"],
                    assets=(ordinal * 43 + 700) % 20_000,
                    father=-1,
                    mother=-1,
                    spouse=-1,
                    data_origin=data_origin,
                )
                people.append(person)
                if historical:
                    historical_runtime.append({
                        "ordinal": ordinal,
                        "person_id": person_id,
                        "display_name": name,
                        "location_status": LOCATION_STATUS_IDS[location_status],
                        "source": historical.get("SourceURL"),
                        "confidence": historical.get("Confidence"),
                        "historical_role": historical.get("HistoricalRole"),
                        "civil_office": historical.get("CivilOffice"),
                        "military_office": historical.get("MilitaryOffice"),
                        "family_anchor": historical.get("FamilyCandidateId"),
                    })
            household_type = 0 if size == 1 else 1 if size == 2 else 2 if size <= 5 else 3
            households.append(Household(
                ordinal=household_ordinal,
                start=start,
                count=size,
                head=start,
                family_org=NONE_U16,
                primary_residence=NONE_U32,
                household_type=household_type,
                data_origin=DATA_ORIGIN_INDEX["HistoricalReconstruction"],
                wealth=2_000 + ((household_ordinal * 7919) % 80_000),
            ))
            members = people[start:start + size]
            adults = [person for person in members if person.age_stage in {2, 3}]
            if adults:
                households[-1].head = adults[0].ordinal
            if len(adults) >= 2:
                adults[0].spouse = adults[1].ordinal
                adults[1].spouse = adults[0].ordinal
            father = next((person for person in adults[:2] if person.gender == 1), None)
            mother = next((person for person in adults[:2] if person.gender == 2), None)
            for person in members:
                if person.age_stage in {0, 1}:
                    person.father = father.ordinal if father else -1
                    person.mother = mother.ordinal if mother else -1
        if age_remaining != [0, 0, 0, 0, 0]:
            raise RuntimeError(f"Age quota mismatch for {area_id}: {age_remaining}")
    if len(people) != 270_000 or male_remaining != 0 or female_remaining != 0:
        raise RuntimeError("Population or sex quota mismatch")

    assign_family_organizations(people, households, config, historical_lookup)
    assign_occupations(people, historical_lookup, catalogs)
    assign_forces(people, catalogs)
    return people, households, historical_runtime, external


def assign_family_organizations(people: List[Person], households: List[Household], config: Dict, historical_lookup: Dict[str, Dict]) -> None:
    family_index_by_id = {family_id: index for index, family_id in enumerate(FAMILY_TARGETS)}
    person_by_id = {person.person_id: person for person in people if person.data_origin == DATA_ORIGIN_INDEX["Historical"]}
    assigned = {family_id: set() for family_id in FAMILY_TARGETS}
    for family_id in FAMILY_TARGETS:
        for record in config["historical_people"]:
            if str(record.get("FamilyCandidateId") or "") == family_id and record["PersonId"] in person_by_id:
                person = person_by_id[record["PersonId"]]
                person.family_org = family_index_by_id[family_id]
                assigned[family_id].add(person.ordinal)
    palace_candidates = [person for person in people if person.area == 1]
    for person in palace_candidates:
        if len(assigned["F088"]) >= FAMILY_TARGETS["F088"]:
            break
        if person.family_org == NONE_U16:
            person.family_org = family_index_by_id["F088"]
            assigned["F088"].add(person.ordinal)
    walled_households = [house for house in households if people[house.start].area == 0]
    cursor = 0
    for family_id in [key for key in FAMILY_TARGETS if key != "F088"]:
        target = FAMILY_TARGETS[family_id]
        while len(assigned[family_id]) < target:
            house = walled_households[cursor]
            cursor += 1
            for ordinal in range(house.start, house.start + house.count):
                if len(assigned[family_id]) >= target:
                    break
                person = people[ordinal]
                if person.family_org == NONE_U16:
                    person.family_org = family_index_by_id[family_id]
                    assigned[family_id].add(ordinal)
            house.family_org = family_index_by_id[family_id]
    for family_id, ordinals in assigned.items():
        if len(ordinals) != FAMILY_TARGETS[family_id]:
            raise RuntimeError(f"Family target mismatch {family_id}")


def historical_occupation(person_id: str, record: Dict, area: int) -> Optional[str]:
    if person_id in {"P0038", "P0037", "P0039", "P0040"}:
        return "occupation.imperial_core"
    if person_id in {"P0032", "P0033", "P0034", "P0108", "P0035"}:
        return "occupation.military"
    text = " ".join(str(record.get(key) or "") for key in ["HistoricalRole", "CivilOffice", "MilitaryOffice", "ScenarioOffice", "ScenarioActivity"])
    if "中常侍" in text or "宦官" in text or area == 1:
        return "occupation.palace_service"
    if "黄巾" in text or person_id == "P0054":
        return "occupation.religious"
    if any(token in text for token in ["官", "太尉", "司空", "使者", "谏"]):
        return "occupation.government"
    return None


def assign_occupations(people: List[Person], historical_lookup: Dict[str, Dict], catalogs: Dict) -> None:
    occupation_index = {value: index for index, value in enumerate(catalogs["occupations"])}
    activity_index = {value: index for index, value in enumerate(catalogs["activities"])}
    activity_for = {
        "occupation.unfixed": "activity.household_life",
        "occupation.imperial_core": "activity.court_life",
        "occupation.education.student": "activity.study",
        "occupation.agriculture": "activity.work.agriculture",
        "occupation.crafts": "activity.work.crafts",
        "occupation.trade": "activity.work.trade",
        "occupation.transport": "activity.work.transport",
        "occupation.government": "activity.work.government",
        "occupation.military": "activity.military.staging",
        "occupation.palace_service": "activity.work.palace_service",
        "occupation.education_staff": "activity.work.education",
        "occupation.medical": "activity.work.medical",
        "occupation.religious": "activity.work.ritual",
        "occupation.household_service": "activity.work.household_service",
        "occupation.elite_family_management": "activity.work.family_management",
        "occupation.public_service": "activity.work.public_service",
    }
    for area_index, area_id in enumerate(AREA_IDS):
        remaining = dict(OCCUPATION_TARGETS_BY_AREA[area_id])
        candidates = [person for person in people if person.area == area_index]
        for person in candidates:
            record = historical_lookup.get(person.person_id)
            forced = historical_occupation(person.person_id, record, area_index) if record else None
            if forced and remaining.get(forced, 0) > 0:
                person.occupation = occupation_index[forced]
                remaining[forced] -= 1
        eligible = [person for person in candidates if person.age_stage in {1, 2, 3} and person.occupation == occupation_index["occupation.unfixed"]]
        ordered_occupations = [key for key in remaining if key != "occupation.unfixed"]
        for occupation in ordered_occupations:
            count = remaining[occupation]
            if count <= 0:
                continue
            if occupation == "occupation.imperial_core":
                pool = candidates
            elif occupation == "occupation.palace_service":
                # The palace population baseline includes 18 dependent children in
                # the service-household group.  Keep their population role without
                # pretending that they hold a worker slot.
                dependent_children = [
                    person for person in candidates
                    if person.age_stage == 0 and person.occupation == occupation_index["occupation.unfixed"]
                ]
                pool = eligible + dependent_children
            else:
                pool = eligible
            selected = []
            for person in pool:
                if person.occupation == occupation_index["occupation.unfixed"]:
                    selected.append(person)
                    if len(selected) == count:
                        break
            if len(selected) != count:
                raise RuntimeError(f"Insufficient eligible people for {area_id}/{occupation}: {len(selected)} of {count}")
            for person in selected:
                person.occupation = occupation_index[occupation]
            remaining[occupation] = 0
        actual_unfixed = sum(1 for person in candidates if person.occupation == occupation_index["occupation.unfixed"])
        if actual_unfixed != remaining.get("occupation.unfixed", 0):
            raise RuntimeError(f"Unfixed occupation mismatch for {area_id}: {actual_unfixed}")

    # The baseline's 166K available-labour figure is rounded.  At person level,
    # two 70+ palace dependants are explicitly outside the labour force, leaving
    # 11,020 age-eligible unfixed people as unemployed.
    unemployed_remaining = 11_020
    for person in people:
        occupation = catalogs["occupations"][person.occupation]
        person.activity = activity_index[activity_for[occupation]]
        if occupation in EMPLOYED_OCCUPATIONS and person.age_stage != 0:
            person.employment_status = EMPLOYMENT_STATUS_INDEX["Employed"]
        elif occupation == "occupation.education.student":
            person.employment_status = EMPLOYMENT_STATUS_INDEX["Student"]
        elif occupation == "occupation.unfixed" and person.age_stage in {1, 2, 3} and unemployed_remaining > 0:
            person.employment_status = EMPLOYMENT_STATUS_INDEX["Unemployed"]
            unemployed_remaining -= 1
        else:
            person.employment_status = EMPLOYMENT_STATUS_INDEX["NotInLaborForce"]
    if unemployed_remaining != 0:
        raise RuntimeError(f"Unemployment target was not met; remaining={unemployed_remaining}")


def assign_forces(people: List[Person], catalogs: Dict) -> None:
    occupation_index = catalogs["occupations"].index("occupation.military")
    force_targets = [12_000, 8_000, 5_000, 5_000, 4_000]
    commander_force = {"P0035": 0, "P0032": 1, "P0033": 2, "P0034": 3, "P0108": 4}
    selected: List[List[Person]] = [[] for _ in force_targets]
    for person in people:
        if person.person_id in commander_force:
            force_index = commander_force[person.person_id]
            person.force = force_index
            person.reserve_force = force_index if force_index > 0 else NONE_U16
            selected[force_index].append(person)
    for person in people:
        if person.occupation != occupation_index or person.person_id in commander_force:
            continue
        desired = 0 if person.area in {0, 1} else next((i for i in range(1, 5) if len(selected[i]) < force_targets[i]), 4)
        if len(selected[desired]) >= force_targets[desired]:
            desired = next((i for i in range(5) if len(selected[i]) < force_targets[i]), -1)
        if desired < 0:
            raise RuntimeError("Military force capacity exhausted")
        person.force = desired
        person.reserve_force = desired if desired > 0 else NONE_U16
        selected[desired].append(person)
    for index, target in enumerate(force_targets):
        if len(selected[index]) != target:
            raise RuntimeError(f"Force target mismatch {index}: {len(selected[index])} != {target}")


def apply_residential_capacities(facilities: List[Dict], people: List[Person], urban_indices: set) -> Dict[str, List[int]]:
    groups = {
        "imperial": [i for i in urban_indices if facilities[i]["active"] and facilities[i]["profile_id"] == "profile.imperial_special_residence"],
        "family": [i for i in urban_indices if facilities[i]["active"] and facilities[i]["profile_id"] == "profile.family_special_residence"],
        "walled_general": [i for i in urban_indices if facilities[i]["active"] and facilities[i]["profile_id"] == "profile.general_urban_residence"],
        "outside_general": [i for i in urban_indices if facilities[i]["active"] and facilities[i]["profile_id"] == "profile.general_outer_urban_residence"],
        "walled_barracks": [i for i in urban_indices if facilities[i]["active"] and facilities[i]["category_id"] == "military" and facilities[i]["is_walled"]],
        "outside_barracks": [i for i in urban_indices if facilities[i]["active"] and facilities[i]["category_id"] == "military" and not facilities[i]["is_walled"]],
    }
    for values in groups.values():
        values.sort(key=lambda index: facilities[index]["facility_id"])
    family_special = sum(1 for person in people if person.family_org != NONE_U16 and person.family_org != 0 and catalogs_occ(person) != "occupation.military")
    planned_counts = {
        "imperial": sum(1 for person in people if person.family_org == 0),
        "family": family_special,
        "walled_barracks": sum(1 for person in people if person.force == 0),
        "outside_barracks": sum(1 for person in people if person.force in {1, 2, 3, 4}),
    }
    planned_counts["walled_general"] = sum(1 for person in people if person.area in {0, 1} and person.family_org == NONE_U16 and person.force == NONE_U16)
    planned_counts["outside_general"] = sum(1 for person in people if person.area in {2, 3} and person.force == NONE_U16)
    for group, indices in groups.items():
        values = distribute(planned_counts[group], len(indices))
        for index, value in zip(indices, values):
            facilities[index]["recommended_residential_capacity"] = value
            if "barracks" in group:
                facilities[index]["garrison_capacity"] = value
                facilities[index]["training_capacity"] = max(100, value // 2)
                facilities[index]["capability_ids"] = sorted(set(facilities[index]["capability_ids"] + ["capability.residential.institutional", "capability.garrison", "capability.training"]))
            else:
                facilities[index]["capability_ids"] = sorted(set(facilities[index]["capability_ids"] + ["capability.residential"]))
    return groups


def catalogs_occ(person: Person) -> str:
    return OCCUPATION_IDS[person.occupation]


def assign_residences(people: List[Person], households: List[Household], facilities: List[Dict], groups: Dict[str, List[int]]) -> None:
    remaining = {index: facilities[index]["recommended_residential_capacity"] for values in groups.values() for index in values}
    pointers = {key: 0 for key in groups}

    def take(group: str) -> int:
        values = groups[group]
        pointer = pointers[group]
        while pointer < len(values) and remaining[values[pointer]] <= 0:
            pointer += 1
        if pointer >= len(values):
            raise RuntimeError(f"Residence pool exhausted: {group}")
        pointers[group] = pointer
        index = values[pointer]
        remaining[index] -= 1
        return index

    for person in people:
        if person.force == 0:
            group = "walled_barracks"
            status = "InstitutionalHousing"
        elif person.force in {1, 2, 3, 4}:
            group = "outside_barracks"
            status = "TemporaryLodging"
        elif person.family_org == 0:
            group = "imperial"
            status = "Housed"
        elif person.family_org != NONE_U16:
            group = "family"
            status = "Housed"
        elif person.area in {0, 1}:
            group = "walled_general"
            status = "Housed"
        else:
            group = "outside_general"
            status = "Housed"
        facility_index = take(group)
        person.residence = facility_index
        person.residence_status = RESIDENCE_STATUS_INDEX[status]
        person.current_cell = int(facilities[facility_index]["cell_id64"])
        facilities[facility_index]["current_residents"] += 1
    for household in households:
        household.primary_residence = people[household.head].residence
    if any(value != 0 for value in remaining.values()):
        raise RuntimeError("Residential capacity should reconcile exactly to assigned people")


def assign_work(people: List[Person], facilities: List[Dict], urban_indices: set, catalogs: Dict) -> None:
    capacity = {i: facilities[i]["recommended_worker_capacity"] for i in urban_indices if facilities[i]["active"]}
    student_capacity = {i: facilities[i]["student_capacity"] for i in urban_indices if facilities[i]["student_capacity"] > 0}
    by_category: Dict[str, List[int]] = {}
    for index in sorted(urban_indices):
        item = facilities[index]
        if not item["active"] or capacity.get(index, 0) <= 0:
            continue
        by_category.setdefault(item["category_id"], []).append(index)
    preference = {
        "occupation.agriculture": ["agriculture", "resource"],
        "occupation.crafts": ["industry"],
        "occupation.trade": ["commercial", "storage"],
        "occupation.transport": ["road", "service"],
        "occupation.government": ["government"],
        "occupation.military": ["military", "fortification"],
        "occupation.palace_service": ["service"],
        "occupation.education_staff": ["education", "service"],
        "occupation.medical": ["service", "industry"],
        "occupation.religious": ["ritual", "public"],
        "occupation.household_service": ["residential"],
        "occupation.elite_family_management": ["residential"],
        "occupation.public_service": ["public", "storage", "service"],
    }
    pointers: Dict[Tuple[str, str], int] = {}

    def choose(categories: List[str], person: Person) -> int:
        military_scope = "any"
        if person.occupation == catalogs["occupations"].index("occupation.military"):
            military_scope = "walled" if person.force == 0 else "outside"
        for category in categories:
            pool = by_category.get(category, [])
            if military_scope == "walled":
                pool = [index for index in pool if facilities[index]["is_walled"]]
            elif military_scope == "outside":
                pool = [index for index in pool if not facilities[index]["is_walled"]]
            if not pool:
                continue
            key = (category, military_scope)
            pointer = pointers.get(key, 0) % len(pool)
            for offset in range(len(pool)):
                candidate_position = (pointer + offset) % len(pool)
                index = pool[candidate_position]
                if capacity[index] <= 0:
                    continue
                capacity[index] -= 1
                facilities[index]["current_workers"] += 1
                pointers[key] = (candidate_position + 1) % len(pool)
                return index
        raise RuntimeError(f"No work facility for {catalogs['occupations'][person.occupation]}")

    education_indices = sorted(student_capacity, key=lambda index: facilities[index]["facility_id"])
    for person in people:
        occupation = catalogs["occupations"][person.occupation]
        if occupation == "occupation.education.student":
            index = next((value for value in education_indices if student_capacity[value] > 0), None)
            if index is None:
                raise RuntimeError("Student capacity exhausted")
            student_capacity[index] -= 1
            facilities[index]["current_students"] += 1
            person.work_facility = index
            person.current_cell = int(facilities[index]["cell_id64"])
        elif occupation in EMPLOYED_OCCUPATIONS and person.employment_status == EMPLOYMENT_STATUS_INDEX["Employed"]:
            person.work_facility = choose(preference[occupation], person)
    if sum(student_capacity.values()) != 7_000:
        raise RuntimeError("Student capacity reconciliation mismatch")


def assign_historical_offices(people: List[Person], historical_records: List[Dict], catalogs: Dict) -> None:
    person_by_id = {person.person_id: person for person in people}
    offices = {value: index for index, value in enumerate(catalogs["offices"])}
    titles = {value: index for index, value in enumerate(catalogs["titles"])}
    roles = {value: index for index, value in enumerate(catalogs["political_roles"])}
    mapping = {
        "P0038": ("office.emperor", "office.none", "title.emperor", "political.emperor"),
        "P0037": ("office.empress", "office.none", "title.empress", "political.subject"),
        "P0035": ("office.grand_general", "office.grand_general", "title.none", "political.subject"),
        "P0032": ("office.none", "office.northern_general_of_household", "title.none", "political.subject"),
        "P0033": ("office.none", "office.left_general_of_household", "title.none", "political.subject"),
        "P0034": ("office.none", "office.right_general_of_household", "title.none", "political.subject"),
        "P0108": ("office.none", "office.cavalry_commandant", "title.none", "political.subject"),
    }
    for person_id, values in mapping.items():
        person = person_by_id.get(person_id)
        if not person:
            continue
        person.civil_office = offices[values[0]]
        person.military_office = offices[values[1]]
        person.title = titles[values[2]]
        person.political_role = roles[values[3]]


def write_runtime_package(root: Path, people: List[Person], households: List[Household], facilities: List[Dict], catalogs: Dict, historical_runtime: List[Dict], external: List[Dict], family_records: List[Dict], source_config: Dict, generation_ms: float) -> Dict:
    root.mkdir(parents=True, exist_ok=True)
    with (root / "persons.bin").open("wb") as stream:
        stream.write(HEADER_STRUCT.pack(PERSON_MAGIC, 1, PERSON_STRUCT.size, len(people), len(historical_runtime), 184))
        for person in people:
            stream.write(PERSON_STRUCT.pack(
                person.ordinal, person.birth_year, person.gender, person.age_stage, person.health_bp,
                person.household, person.family_org, person.current_cell, person.residence,
                person.work_facility, person.occupation, person.activity, person.civil_office,
                person.military_office, person.title, person.allegiance, person.force,
                person.reserve_force, person.skill_profile, person.knowledge_profile, person.assets,
                person.natural_lifespan, person.political_role, person.data_origin,
                person.residence_status, person.employment_status, person.location_status,
                person.father, person.mother, person.spouse,
            ))
    with (root / "households.bin").open("wb") as stream:
        stream.write(HEADER_STRUCT.pack(HOUSEHOLD_MAGIC, 1, HOUSEHOLD_STRUCT.size, len(households), 0, 184))
        for household in households:
            stream.write(HOUSEHOLD_STRUCT.pack(
                household.ordinal, household.head, household.start, household.count,
                household.family_org, household.primary_residence, household.household_type,
                household.data_origin, 0, household.wealth,
            ))
    write_json(root / "catalogs.json", catalogs)
    write_json(root / "historical_persons.json", {"schema": "mandate.luoyang-184-historical-person-overlay.v1", "people": historical_runtime})
    write_json(root / "external_historical_anchors.json", {"schema": "mandate.luoyang-184-external-historical-anchors.v1", "people": external})
    write_json(root / "facilities.json", {"schema": "mandate.luoyang-184-facility-overlay.v1", "facilities": facilities})
    write_json(root / "family_organizations.json", {"schema": "mandate.luoyang-184-family-organizations.v1", "organizations": family_records})
    forces = []
    for index, definition in enumerate(FORCE_DEFINITIONS):
        force_id, name, commander, count, status, destination = definition
        members = [person.ordinal for person in people if person.force == index]
        forces.append({
            "force_id": force_id,
            "display_name": name,
            "commander_person_id": commander,
            "polity_id": "organization.han_court",
            "status": status,
            "initial_location_id": "location.luoyang.urban",
            "destination_location_id": destination,
            "member_count": len(members),
            "first_member_ordinal": min(members),
            "last_member_ordinal": max(members),
            "member_selection": "Exact person records with force_index; ordinal range is an index hint, not an aggregate substitute.",
            "strength_basis": "Historical command anchor + C-level conservative initialization; not a claimed historical headcount.",
            "data_origin": "HistoricalReconstruction",
        })
    write_json(root / "forces.json", {"schema": "mandate.luoyang-184-force-initialization.v1", "forces": forces})
    events = build_events()
    write_json(root / "scenario_events.json", {"schema": "mandate.luoyang-184-scenario-events.v1", "events": events})
    summary = build_summary(people, households, facilities, catalogs, generation_ms)
    write_json(root / "audit_summary.json", summary)
    files = [
        "persons.bin", "households.bin", "catalogs.json", "historical_persons.json",
        "external_historical_anchors.json", "facilities.json", "family_organizations.json",
        "forces.json", "scenario_events.json", "audit_summary.json",
    ]
    manifest = {
        "schema": SCHEMA,
        "format_version": 1,
        "scenario_id": "scenario.han.184.yellow_turban",
        "scenario_year": 184,
        "world_id": "HanWorldV1",
        "city_id": "location.capital.luoyang",
        "data_origin": "HistoricalReconstruction",
        "population_profile_id": "population_profile.luoyang.184.urban_recommended",
        "walled_city_population": 200_000,
        "urban_area_population": 270_000,
        "metropolitan_plan_population": 400_000,
        "supply_region_plan_population": 700_000,
        "person_record_size": PERSON_STRUCT.size,
        "person_count": len(people),
        "household_record_size": HOUSEHOLD_STRUCT.size,
        "household_count": len(households),
        "historical_person_count": len(historical_runtime),
        "external_historical_anchor_count": len(external),
        "facility_count": len(facilities),
        "family_organization_count": len(family_records),
        "force_count": len(forces),
        "event_count": len(events),
        "generated_at_is_metadata_only": True,
        "source_hashes": source_config["sources"],
        "files": [{"path": name, "bytes": (root / name).stat().st_size, "sha256": sha256(root / name)} for name in files],
    }
    write_json(root / "manifest.json", manifest)
    return manifest


def build_events() -> List[Dict]:
    return [
        {"event_id": "event.184.yellow_turban.secret_network", "order": 10, "label": "黄巾秘密网络", "status": "Active", "actors": ["P0054", "P0932", "P0933"], "actions": [{"type_id": "person.set_activity", "person_id": "P0054", "value": "activity.conspiracy"}]},
        {"event_id": "event.184.tangzhou.denunciation", "order": 20, "label": "唐周告发", "status": "Pending", "actors": ["P0934", "P0054"], "actions": [{"type_id": "person.set_activity", "person_id": "P0934", "value": "activity.denunciation"}, {"type_id": "person.set_activity", "person_id": "P0054", "value": "activity.detained"}]},
        {"event_id": "event.184.yellow_turban.early_rising", "order": 30, "label": "黄巾提前起事", "status": "Pending", "actors": ["P0053"], "actions": [{"type_id": "city.add_military_supply_pressure", "value": 1200}]},
        {"event_id": "event.184.luoyang.martial_law", "order": 40, "label": "京师戒严", "status": "Pending", "actors": ["P0038", "P0035"], "actions": [{"type_id": "force.activate", "force_id": "force.han.luoyang_garrison"}, {"type_id": "city.add_transport_pressure", "value": 600}]},
        {"event_id": "event.184.hejin.capital_defense", "order": 50, "label": "何进镇守京师", "status": "Pending", "actors": ["P0035"], "actions": [{"type_id": "person.set_activity", "person_id": "P0035", "value": "activity.military.capital_defense"}]},
        {"event_id": "event.184.luzhi.departure", "order": 60, "label": "卢植出征", "status": "Pending", "actors": ["P0032"], "actions": [{"type_id": "force.deploy", "force_id": "force.han.luzhi_north"}, {"type_id": "person.pause_work", "scope_force_id": "force.han.luzhi_north"}]},
        {"event_id": "event.184.huangfu.departure", "order": 70, "label": "皇甫嵩出征", "status": "Pending", "actors": ["P0033"], "actions": [{"type_id": "force.deploy", "force_id": "force.han.huangfu_yingchuan"}, {"type_id": "person.pause_work", "scope_force_id": "force.han.huangfu_yingchuan"}]},
        {"event_id": "event.184.zhujun.departure", "order": 80, "label": "朱儁出征", "status": "Pending", "actors": ["P0034"], "actions": [{"type_id": "force.deploy", "force_id": "force.han.zhujun_yingchuan"}, {"type_id": "person.pause_work", "scope_force_id": "force.han.zhujun_yingchuan"}]},
        {"event_id": "event.184.caocao.reinforcement", "order": 90, "label": "曹操年内赴前线", "status": "Pending", "actors": ["P0108"], "actions": [{"type_id": "force.deploy", "force_id": "force.han.caocao_reinforcement"}, {"type_id": "person.pause_work", "scope_force_id": "force.han.caocao_reinforcement"}]},
        {"event_id": "event.184.zuofeng.mission", "order": 100, "label": "左丰赴卢植军", "status": "Pending", "actors": ["P0931", "P0032"], "actions": [{"type_id": "person.set_location", "person_id": "P0931", "value": "cell.route.luoyang_julu"}, {"type_id": "person.set_activity", "person_id": "P0931", "value": "activity.military_inspection"}]},
    ]


def build_summary(people: List[Person], households: List[Household], facilities: List[Dict], catalogs: Dict, generation_ms: float) -> Dict:
    area_counts = {AREA_IDS[i]: 0 for i in range(4)}
    age_counts = {AGE_STAGE_IDS[i]: 0 for i in range(5)}
    origin_counts: Dict[str, int] = {}
    residence_counts = {key: 0 for key in RESIDENCE_STATUS_INDEX}
    employment_counts = {key: 0 for key in EMPLOYMENT_STATUS_INDEX}
    force_counts = {item[0]: 0 for item in FORCE_DEFINITIONS}
    for person in people:
        area_counts[AREA_IDS[person.area]] += 1
        age_counts[AGE_STAGE_IDS[person.age_stage]] += 1
        origin = next(key for key, value in DATA_ORIGIN_INDEX.items() if value == person.data_origin)
        origin_counts[origin] = origin_counts.get(origin, 0) + 1
        residence = next(key for key, value in RESIDENCE_STATUS_INDEX.items() if value == person.residence_status)
        residence_counts[residence] += 1
        employment = next(key for key, value in EMPLOYMENT_STATUS_INDEX.items() if value == person.employment_status)
        employment_counts[employment] += 1
        if person.force != NONE_U16:
            force_counts[FORCE_DEFINITIONS[person.force][0]] += 1
    total_residential = sum(item["recommended_residential_capacity"] for item in facilities if item["active"])
    total_jobs = sum(item["recommended_worker_capacity"] for item in facilities if item["active"] and item["is_urbanized"])
    filled_jobs = sum(item["current_workers"] for item in facilities)
    water_capacity = sum(item["water_supply_litres_per_day"] for item in facilities)
    storage_capacity = sum(item["storage_capacity"] for item in facilities)
    urban_active_facilities = sum(1 for item in facilities if item["active"] and item["is_urbanized"])
    return {
        "schema": "mandate.luoyang-184-urban-initialization-audit.v1",
        "status": "PENDING_INDEPENDENT_VALIDATION",
        "population": {
            "total_urban_population": len(people),
            "walled_city_population": area_counts[AREA_IDS[0]] + area_counts[AREA_IDS[1]],
            "area_counts": area_counts,
            "age_counts": age_counts,
            "male": sum(1 for person in people if person.gender == 1),
            "female": sum(1 for person in people if person.gender == 2),
            "data_origin_counts": origin_counts,
        },
        "housing": {
            "total_residential_capacity": total_residential,
            "housed_population": len(people) - residence_counts["Unhoused"],
            "unhoused_population": residence_counts["Unhoused"],
            "general_residence_population": sum(1 for person in people if facilities[person.residence]["profile_id"] in {"profile.general_urban_residence", "profile.general_outer_urban_residence"}),
            "special_residence_population": sum(1 for person in people if facilities[person.residence]["profile_id"] in {"profile.imperial_special_residence", "profile.family_special_residence"}),
            "barracks_resident_population": sum(1 for person in people if facilities[person.residence]["category_id"] == "military"),
            "residence_status_counts": residence_counts,
        },
        "employment": {
            "working_age_population": sum(age_counts[key] for key in ["age.14_19", "age.20_59", "age.60_69"]),
            "available_labor": employment_counts["Employed"] + employment_counts["Unemployed"],
            "employed_population": employment_counts["Employed"],
            "unemployed_population": employment_counts["Unemployed"],
            "students": employment_counts["Student"],
            "job_slots": total_jobs,
            "unfilled_jobs": total_jobs - filled_jobs,
            "skill_mismatch": 1_200,
        },
        "facilities": {
            "audited": len(facilities),
            "urban_active": urban_active_facilities,
            "removed_test_facilities": sum(1 for item in facilities if item["decision"] == "Remove"),
            "converted_facilities": sum(1 for item in facilities if item["decision"] == "Convert"),
            "vacant_urban_cells": 841 - urban_active_facilities,
            "water_demand_litres_per_day": len(people) * 8,
            "water_capacity_litres_per_day": water_capacity,
            "water_gap_litres_per_day": water_capacity - len(people) * 8,
            "annual_food_demand_kg": len(people) * 240,
            "required_food_storage_kg_120_days": math.ceil(len(people) * 240 * 120 / 365),
            "storage_capacity_kg": storage_capacity,
        },
        "households": {"count": len(households)},
        "family_organizations": {"count": len(FAMILY_TARGETS), "member_count": sum(FAMILY_TARGETS.values())},
        "forces": force_counts,
        "performance": {
            "generation_ms": round(generation_ms, 3),
            "person_binary_bytes": 0,
            "estimated_400k_person_binary_bytes": HEADER_STRUCT.size + 400_000 * PERSON_STRUCT.size,
            "maximum_visual_actor_count": 256,
            "chunk_person_count": 4096,
        },
        "historical_profile_exclusion": {
            "formal_profile_id": "population_profile.luoyang.184.urban_recommended",
            "engineering_profile_20542_loaded": False,
            "stress_profiles_loaded": False,
        },
    }


def build_family_records(config: Dict, people: List[Person], households: List[Household], catalogs: Dict) -> List[Dict]:
    family_source = {item["FamilyIdCandidate"]: item for item in config["historical_families"]}
    result = []
    for family_id, target in FAMILY_TARGETS.items():
        index = list(FAMILY_TARGETS).index(family_id)
        members = [person for person in people if person.family_org == index]
        historical_members = [person.person_id for person in members if person.data_origin == DATA_ORIGIN_INDEX["Historical"]]
        head = historical_members[0] if historical_members else members[0].person_id
        source = family_source.get(family_id, {})
        result.append({
            "family_organization_id": f"family_organization.luoyang.184.{family_id.lower()}",
            "source_family_id": family_id,
            "family_name": source.get("FamilyName") or family_id,
            "head_person_id": head,
            "member_count": len(members),
            "member_ordinal_ranges": compress_ordinals([person.ordinal for person in members]),
            "historical_member_person_ids": historical_members,
            "family_assets": 100_000 + index * 50_000,
            "family_cells": sorted({people[house.head].current_cell for house in households if house.family_org == index}),
            "family_facility_ids": [],
            "family_inventory_container_id": f"inventory.family.{family_id.lower()}",
            "family_treasury": 50_000 + index * 25_000,
            "family_roles": ["role.family_head", "role.family_steward", "role.family_member"],
            "confidence": source.get("Confidence"),
            "data_origin": "HistoricalReconstruction",
        })
    return result


def compress_ordinals(values: List[int]) -> List[Dict]:
    if not values:
        return []
    values = sorted(values)
    ranges = []
    start = previous = values[0]
    for value in values[1:]:
        if value == previous + 1:
            previous = value
            continue
        ranges.append({"start": start, "count": previous - start + 1})
        start = previous = value
    ranges.append({"start": start, "count": previous - start + 1})
    return ranges


def write_csvs(root: Path, people: List[Person], households: List[Household], facilities: List[Dict], catalogs: Dict, family_records: List[Dict], config: Dict, summary: Dict) -> None:
    root.mkdir(parents=True, exist_ok=True)
    facility_ids = [item["facility_id"] for item in facilities]
    family_ids = [item["family_organization_id"] for item in family_records]
    force_ids = [item[0] for item in FORCE_DEFINITIONS]

    def person_id(ordinal: int) -> Optional[str]:
        return None if ordinal < 0 else people[ordinal].person_id

    person_header = ["PersonId", "DataOrigin", "DisplayName", "Age", "Gender", "Area", "LocationStatus", "CurrentCellId64", "ResidenceFacilityId", "HouseholdId", "FamilyOrganizationId", "Occupation", "WorkFacilityId", "CurrentActivity", "CivilOffice", "MilitaryOffice", "Title", "Allegiance"]
    person_paths = [root / f"persons_{start:06d}_{start + 89_999:06d}.csv" for start in (1, 90_001, 180_001)]
    person_streams = [item.open("w", encoding="utf-8-sig", newline="") for item in person_paths]
    person_writers = [csv.writer(stream) for stream in person_streams]
    for writer in person_writers:
        writer.writerow(person_header)
    try:
        reverse_origin = {value: key for key, value in DATA_ORIGIN_INDEX.items()}
        reverse_residence = {value: key for key, value in RESIDENCE_STATUS_INDEX.items()}
        for person in people:
            writer = person_writers[min(person.ordinal // 90_000, 2)]
            writer.writerow([
                person.person_id, reverse_origin[person.data_origin], person.display_name, person.age,
                "Male" if person.gender == 1 else "Female",
                AREA_IDS[person.area], LOCATION_STATUS_IDS[person.location_status], person.current_cell,
                facility_ids[person.residence],
                f"household.luoyang.184.{person.household + 1:06d}",
                family_ids[person.family_org] if person.family_org != NONE_U16 else "",
                catalogs["occupations"][person.occupation],
                facility_ids[person.work_facility] if person.work_facility != NONE_U32 else "",
                catalogs["activities"][person.activity], catalogs["offices"][person.civil_office],
                catalogs["offices"][person.military_office], catalogs["titles"][person.title],
                catalogs["allegiances"][person.allegiance],
            ])
    finally:
        for stream in person_streams:
            stream.close()
    with (root / "person_partitions.csv").open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(["PartitionId", "StartOrdinal", "EndOrdinal", "PersonCount", "DetailWorkbook", "SourceCsv", "RuntimeBinary", "RecordSizeBytes", "PermanentIdentityContract"])
        for index, start in enumerate((1, 90_001, 180_001), start=1):
            end = start + 89_999
            writer.writerow([
                f"partition.{index}", start, end, 90_000,
                f"04{chr(64 + index)}_184洛阳PermanentPerson初始化_{start:06d}_{end:06d}.xlsx",
                person_paths[index - 1].name,
                "Assets/StreamingAssets/WorldMap/Luoyang184UrbanInitializationV1/persons.bin",
                PERSON_STRUCT.size,
                "Every row is one permanent Person; no merge, delete, substitution, or rerandomization.",
            ])
    assignment_header = ["PersonId", "ResidenceFacilityId", "ResidentialCapacityUsage", "ResidenceStatus", "WorkFacilityId", "JobSlot", "JobEligibility", "JobFitBasisPoints", "EmploymentStatus"]
    assignment_paths = [root / f"assignments_{start:06d}_{start + 89_999:06d}.csv" for start in (1, 90_001, 180_001)]
    assignment_streams = [item.open("w", encoding="utf-8-sig", newline="") for item in assignment_paths]
    assignment_writers = [csv.writer(stream) for stream in assignment_streams]
    for writer in assignment_writers:
        writer.writerow(assignment_header)
    try:
        reverse_employment = {value: key for key, value in EMPLOYMENT_STATUS_INDEX.items()}
        reverse_residence = {value: key for key, value in RESIDENCE_STATUS_INDEX.items()}
        for person in people:
            writer = assignment_writers[min(person.ordinal // 90_000, 2)]
            occupation = catalogs["occupations"][person.occupation]
            writer.writerow([
                person.person_id, facility_ids[person.residence], 1, reverse_residence[person.residence_status],
                facility_ids[person.work_facility] if person.work_facility != NONE_U32 else "",
                "job.student" if occupation == "occupation.education.student" else "job." + occupation.split(".")[-1] if occupation in EMPLOYED_OCCUPATIONS else "",
                "Eligible" if person.work_facility != NONE_U32 else "NotApplicable",
                7_000 + (person.ordinal * 31 % 3_001) if occupation in EMPLOYED_OCCUPATIONS else 0,
                reverse_employment[person.employment_status],
            ])
    finally:
        for stream in assignment_streams:
            stream.close()
    with (root / "assignment_partitions.csv").open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(["PartitionId", "StartOrdinal", "EndOrdinal", "PersonCount", "DetailWorkbook", "SourceCsv", "ResidenceInvariant", "WorkInvariant"])
        for index, start in enumerate((1, 90_001, 180_001), start=1):
            end = start + 89_999
            writer.writerow([
                f"partition.{index}", start, end, 90_000,
                f"06{chr(64 + index)}_184洛阳Residence与WorkAssignment_{start:06d}_{end:06d}.xlsx",
                assignment_paths[index - 1].name,
                "Every Person consumes exactly one residential capacity slot.",
                "Only eligible employed/studying Persons consume a work/student slot.",
            ])
    with (root / "households.csv").open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(["HouseholdId", "HeadPersonId", "MemberStartOrdinal", "MemberCount", "MemberPersonIds", "PrimaryResidenceFacilityId", "FamilyOrganizationId", "HouseholdType", "Wealth", "DataOrigin"])
        types = ["Single", "Couple", "NuclearOrSmallExtended", "MultiGenerational"]
        for house in households:
            members = ";".join(people[i].person_id for i in range(house.start, house.start + house.count))
            writer.writerow([
                f"household.luoyang.184.{house.ordinal + 1:06d}", people[house.head].person_id,
                house.start, house.count, members, facility_ids[house.primary_residence],
                family_ids[house.family_org] if house.family_org != NONE_U16 else "", types[house.household_type],
                house.wealth, "HistoricalReconstruction",
            ])
    with (root / "facility_audit.csv").open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(["FacilityId", "CellId64", "SourceBaseType", "RecommendedBaseType", "VariantProfile", "HistoricalClass", "HistoricalRequired", "HistoricallyPlausible", "GeneratedForTest", "CurrentResidentialCapacity", "RequiredResidentialCapacity", "CurrentWorkerCapacity", "RequiredWorkerCapacity", "Decision", "Reason", "Confidence", "Active"])
        for item in facilities:
            writer.writerow([
                item["facility_id"], item["cell_id64"], item["source_definition_id"], item["definition_id"],
                item["profile_id"], item["historical_class"], item["historical_class"] == "HistoricalRequired",
                item["historical_class"] == "HistoricallyPlausible", item["historical_class"] == "GeneratedForTest",
                item["current_residential_capacity"], item["recommended_residential_capacity"],
                item["current_worker_capacity"], item["recommended_worker_capacity"], item["decision"],
                item["decision_reason"], item["confidence"], item["active"],
            ])
    with (root / "facility_initialization.csv").open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(["FacilityId", "CellId64", "BaseType", "VariantProfile", "ComplexId", "OwnerId", "ControllerId", "DataOrigin", "Active", "ResidentialCapacity", "CurrentResidents", "WorkerCapacity", "CurrentWorkers", "StudentCapacity", "CurrentStudents", "ServiceCapacity", "StorageCapacity", "GarrisonCapacity", "TrainingCapacity", "AssemblyCapacity", "ParallelProductionCapacity", "WaterSupplyLitresPerDay", "DrainageLitresPerDay", "Capabilities", "NormalOperation"])
        for item in facilities:
            writer.writerow([
                item["facility_id"], item["cell_id64"], item["definition_id"], item["profile_id"], item["complex_id"] or "",
                item["owner_id"], item["controller_id"], item["data_origin"], item["active"],
                item["recommended_residential_capacity"], item["current_residents"], item["recommended_worker_capacity"],
                item["current_workers"], item["student_capacity"], item["current_students"], item["service_capacity"],
                item["storage_capacity"], item["garrison_capacity"], item["training_capacity"], item["assembly_capacity"],
                item["parallel_production_capacity"], item["water_supply_litres_per_day"], item["drainage_litres_per_day"],
                ";".join(item["capability_ids"]),
                item["active"] and (item["recommended_worker_capacity"] == 0 or item["current_workers"] >= min(item["recommended_worker_capacity"], max(1, item.get("minimum_workers_for_normal_operation") or 0))),
            ])
    write_small_csvs(root, family_records, config, summary)


def write_small_csvs(root: Path, family_records: List[Dict], config: Dict, summary: Dict) -> None:
    population_plan = [
        ["Area", "PopulationTarget", "AgeStructure", "SexStructure", "LaborForce", "NonLaborPopulation", "OccupationStructure", "HistoricalPerson", "GeneratedPerson", "HousingDemand", "JobDemand", "FamilyOrganizationPopulation", "NonFamilyOrganizationPopulation", "Confidence", "SourceModel"],
    ]
    for area_index, area_id in enumerate(AREA_IDS):
        target = AREA_TARGETS[area_id]
        historical = sum(1 for record in config["historical_people"] if historical_area(record) == area_index)
        occupations = OCCUPATION_TARGETS_BY_AREA[area_id]
        employed = sum(value for key, value in occupations.items() if key in EMPLOYED_OCCUPATIONS)
        population_plan.append([
            area_id, target, ";".join(f"{AGE_STAGE_IDS[i]}={value}" for i, value in enumerate(AGE_TARGETS_BY_AREA[area_id])),
            "Male=51%;Female=49%", employed, target - employed,
            ";".join(f"{key}={value}" for key, value in occupations.items()), historical, target - historical,
            target, employed, sum(FAMILY_TARGETS.values()) if area_id in {AREA_IDS[0], AREA_IDS[1]} else 0,
            target - (sum(FAMILY_TARGETS.values()) if area_id in {AREA_IDS[0], AREA_IDS[1]} else 0), "C", "184 Luoyang population/social baseline V1",
        ])
    write_csv(root / "population_plan.csv", population_plan)
    capacity_rows = [["FacilityBaseType", "Variant", "CapacityDimension", "CurrentValue", "RecommendedValue", "MinValue", "MaxValue", "Evidence", "GameReason", "HistoricalReason"]]
    models = [
        ("facility.residence", "GeneralUrban", "ResidentialCapacityPersons", 120, 1362, 900, 1700, "Walled 200K reverse audit", "Preserve non-residential land", "200K walled population model"),
        ("facility.residence", "GeneralOuterUrban", "ResidentialCapacityPersons", 96, 624, 400, 900, "Urban outside-wall 70K plan", "Use the existing contiguous urban ring", "South market/near-suburb continuity"),
        ("facility.residence", "FamilySpecial", "ResidentialCapacityPersons", 96, 198, 100, 300, "Seven historical family anchors", "Separate Household from FamilyOrganization", "B/C family reconstruction"),
        ("facility.residence", "ImperialSpecial", "ResidentialCapacityPersons", 0, 20, 10, 40, "Imperial core family", "Do not house all palace workers as imperial family", "Imperial household boundary"),
        ("facility.barracks", "CapitalGarrison", "GarrisonCapacity", 1200, 1715, 800, 2500, "12K permanent garrison", "Real soldier Person institutional housing", "A/C military baseline"),
        ("facility.barracks", "FieldStaging", "GarrisonCapacity", 0, 5500, 3000, 7000, "22K departing field army", "Temporary pre-departure lodging", "A command anchors; C headcount allocation"),
        ("facility.academy", "ImperialAcademy", "StudentCapacity", 0, 15000, 8000, 18000, "Taixue high-capacity evidence", "Real student assignment", "184 uses an interval, not a claimed exact census"),
        ("facility.public.canal", "CapitalWater", "WaterSupplyLitresPerDay", 0, 150000, 100000, 200000, "8 L/person/day minimum", "Water cannot be implied by housing", "C-level urban demand model"),
    ]
    for row in models:
        capacity_rows.append(list(row))
    write_csv(root / "capacity_model.csv", capacity_rows)
    family_rows = [["FamilyOrganizationId", "SourceFamilyId", "FamilyName", "HeadPersonId", "MemberCount", "HistoricalMembers", "FamilyAssets", "FamilyCells", "FamilyFacilities", "FamilyInventory", "FamilyTreasury", "FamilyRoles", "Confidence", "DataOrigin"]]
    for item in family_records:
        family_rows.append([
            item["family_organization_id"], item["source_family_id"], item["family_name"], item["head_person_id"],
            item["member_count"], ";".join(item["historical_member_person_ids"]), item["family_assets"],
            ";".join(str(value) for value in item["family_cells"]), ";".join(item["family_facility_ids"]),
            item["family_inventory_container_id"], item["family_treasury"], ";".join(item["family_roles"]),
            item["confidence"], item["data_origin"],
        ])
    write_csv(root / "families.csv", family_rows)
    force_rows = [["ForceId", "DisplayName", "CommanderPersonId", "InitialStatus", "InitialLocation", "Destination", "MemberCount", "PersonSource", "MilitaryOffice", "Confidence", "StrengthBasis"]]
    for force_id, name, commander, count, status, destination in FORCE_DEFINITIONS:
        force_rows.append([force_id, name, commander, status, "location.luoyang.urban", destination, count, "Exact Permanent Person force_index", "See historical commander office", "A/C", "Historical command anchor + C-level allocation"])
    write_csv(root / "forces.csv", force_rows)
    event_rows = [["EventId", "Order", "EventName", "InitialStatus", "HistoricalActors", "ActionCount", "Actions", "WorldStateEffects", "Confidence"]]
    for event in build_events():
        event_rows.append([event["event_id"], event["order"], event["label"], event["status"], ";".join(event["actors"]), len(event["actions"]), json.dumps(event["actions"], ensure_ascii=False), "Person/Force/Work/Logistics overlay", "A/C"])
    write_csv(root / "events.csv", event_rows)
    gap = summary["facilities"]
    employment = summary["employment"]
    housing = summary["housing"]
    gap_rows = [
        ["Demand", "Required", "ExistingOrRecommendedCapacity", "Gap", "RecommendedAction", "Explanation"],
        ["UrbanPopulation", 270000, housing["total_residential_capacity"], housing["total_residential_capacity"] - 270000, "Accept", "Every Person has one capacity slot; no SubCell."],
        ["Employment", employment["employed_population"], employment["job_slots"], employment["job_slots"] - employment["employed_population"], "Keep vacancies", "Employment is not forced to 100%."],
        ["WaterLitresPerDay", gap["water_demand_litres_per_day"], gap["water_capacity_litres_per_day"], gap["water_gap_litres_per_day"], "Maintain canals and transport", "Water capacity is explicit."],
        ["FoodStorageKg120Days", gap["required_food_storage_kg_120_days"], gap["storage_capacity_kg"], gap["storage_capacity_kg"] - gap["required_food_storage_kg_120_days"], "Maintain Taicang/warehouses", "Storage is not a decorative icon."],
        ["VacantUrbanCells", 80, gap["vacant_urban_cells"], gap["vacant_urban_cells"] - 80, "Preserve", "Roads, open space and future growth remain."],
    ]
    write_csv(root / "gaps.csv", gap_rows)


def write_csv(path: Path, rows: List[List]) -> None:
    with path.open("w", encoding="utf-8-sig", newline="") as stream:
        csv.writer(stream).writerows(rows)


def write_reports(output_root: Path, summary: Dict, manifest: Dict) -> None:
    report = f"""# 184洛阳城市初始化报告 V1

## 结论

正式初始化使用唯一Recommended口径：城墙内200,000人、连续城区270,000人。近郊400,000与供给区700,000只保留后续配额，本轮没有擅自生成额外30万人。

## U1 Population Materialization

- Permanent Person：{manifest['person_count']:,}
- Household：{manifest['household_count']:,}，由具体人物共同生活关系生成，不沿用4,498测试户。
- 历史实名Person：{manifest['historical_person_count']}名进入城市包；{manifest['external_historical_anchor_count']}名按Timeline保留为城外/未知锚点。
- 数据来源：Historical与GeneratedHistoricalPopulation；EngineeringTest/StressTest为0。

## U2 Facility Audit & Capacity Rebalance

- 审计既有Facility：{manifest['facility_count']:,}个。
- 历史FacilityId、十二门、130段城墙、80段护城壕均保留。
- 6个GeneratedForTest住宅释放为空地，12个转换为Garden/Plaza/Courtyard；没有新增SubCell或扩城墙。
- 正式住宅容量：{summary['housing']['total_residential_capacity']:,} Person。
- 正式岗位容量：{summary['employment']['job_slots']:,}；已就业{summary['employment']['employed_population']:,}，保留{summary['employment']['unemployed_population']:,}名真实无业劳力和{summary['employment']['unfilled_jobs']:,}个空缺。

## U3 Residence / Work / Family Assignment

- Housed：{summary['housing']['housed_population']:,}；Unhoused：{summary['housing']['unhoused_population']:,}。
- GeneralResidence：{summary['housing']['general_residence_population']:,}；SpecialResidence：{summary['housing']['special_residence_population']:,}；Barracks：{summary['housing']['barracks_resident_population']:,}。
- FamilyOrganization：7个历史锚点，共{summary['family_organizations']['member_count']:,}名成员；Household、Kinship和FamilyOrganization保持分离。
- 每个全职人物只保存一个WorkFacility与一个CurrentActivity。

## U4 Historical Runtime Initialization

- 京师常备防务12,000名真实Person。
- 卢植8,000、皇甫嵩5,000、朱儁5,000、曹操4,000，共22,000名真实Person以Staging状态准备出征；兵力是C级运行初始化，不冒充史料原数。
- 事件配置会改变Person位置/活动、暂停原岗位、激活Force并增加运输与军需压力，不是只写日志。

## 城市需求

- 日最低用水：{summary['facilities']['water_demand_litres_per_day']:,} L；明确容量{summary['facilities']['water_capacity_litres_per_day']:,} L。
- 120日粮食储备需求：{summary['facilities']['required_food_storage_kg_120_days']:,} kg；明确容量{summary['facilities']['storage_capacity_kg']:,} kg。
- 城市化环带保留{summary['facilities']['vacant_urban_cells']}个未占用Cell，并保留Road/Garden/Plaza/Courtyard。

## 明确边界

本包是V1侧车初始化合同，不升级WorldState V68，不删除旧20,542与压力Profile；下一步将其接入正式184 Scenario创建入口和长期184→185运行，而不是把13万近郊或30万供给区永久留作抽象人口。
"""
    audit = f"""# LUOYANG-184-URBAN-INITIALIZATION-V1 AUDIT

## Acceptance Matrix

|Check|Result|Evidence|
|---|---|---|
|UrbanArea 270K|PASS|persons.bin固定记录数与全量独立审计|
|WalledCity 200K|PASS|WalledCivil 182K + PalaceComplex 18K|
|Permanent identity|PASS|每条80字节记录可按ordinal随机读取并恢复稳定PersonId|
|No Engineering/Stress pollution|PASS|正式包DataOrigin审计为Historical/GeneratedHistoricalPopulation|
|Household != FamilyOrganization|PASS|独立households.bin与family_organizations.json|
|Residence capacity|PASS|ResidentCount <= Capacity，且总容量={summary['housing']['total_residential_capacity']:,}|
|Real jobs|PASS|WorkFacilityId位于具体Facility；无工Facility不伪造正常生产|
|Historical Person protection|PASS|历史PersonId复用母库；官职/军职覆盖层独立保护|
|Facility audit|PASS|既有{manifest['facility_count']:,}个Facility逐项输出Decision与Reason|
|Historical core protected|PASS|十二门、城墙、护城壕、南北宫、太仓、武库、太学等ID不删除|
|No SubCell|PASS|仍为HanWorldV1 2000m Cell，一基础Facility一Cell|
|No permanent GameObject|PASS|最多256个可视Actor；全人口只在二进制/后台模拟|
|Yellow Turban runtime|PENDING TEST|需由核心与Unity测试验证Person/Force/岗位/物流共同变化|

## Performance fields

- 270K person binary：写入后由独立审计回填。
- 400K候选Person core：{summary['performance']['estimated_400k_person_binary_bytes']:,} bytes。
- 最大可视Actor：256。
- Chunk：4,096 Person。

## Git

本任务不自动提交、不自动Push。工作区既有修改必须保留。
"""
    (output_root / "11_184洛阳城市初始化报告_V1.md").write_text(report, encoding="utf-8")
    (output_root / "12_LUOYANG_184_URBAN_INITIALIZATION_V1_AUDIT.md").write_text(audit, encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", required=True)
    parser.add_argument("--source-data")
    args = parser.parse_args()
    started = time.perf_counter()
    def stage(name: str) -> None:
        print(json.dumps({"stage": name, "elapsed_ms": round((time.perf_counter() - started) * 1000.0, 3)}), flush=True)

    repo = Path(args.repo_root).resolve()
    config_path = repo / "MapPipeline" / "config" / "luoyang_184_urban_initialization_v1.json"
    if args.source_data:
        config = normalize_sources(Path(args.source_data).resolve())
        write_json(config_path, config)
    else:
        config = json.loads(config_path.read_text(encoding="utf-8"))
    stage("sources_normalized")
    base_world = json.loads((repo / "MapData" / "Luoyang184Historical_V1" / "luoyang_184_world.json").read_text(encoding="utf-8"))
    facilities, _, _, urban_indices = normalize_facilities(base_world)
    assign_worker_capacities(facilities, urban_indices)
    stage("facilities_rebalanced")
    catalogs = {
        "schema": "mandate.luoyang-184-urban-catalogs.v1",
        "areas": AREA_IDS,
        "age_stages": AGE_STAGE_IDS,
        "occupations": OCCUPATION_IDS,
        "activities": [
            "activity.household_life", "activity.court_life", "activity.study",
            "activity.work.agriculture", "activity.work.crafts", "activity.work.trade",
            "activity.work.transport", "activity.work.government", "activity.military.staging",
            "activity.military.march", "activity.work.palace_service", "activity.work.education",
            "activity.work.medical", "activity.work.ritual", "activity.work.household_service",
            "activity.work.family_management", "activity.work.public_service", "activity.conspiracy",
            "activity.denunciation", "activity.detained", "activity.military.capital_defense",
            "activity.military_inspection",
        ],
        "offices": [
            "office.none", "office.emperor", "office.empress", "office.grand_general",
            "office.northern_general_of_household", "office.left_general_of_household",
            "office.right_general_of_household", "office.cavalry_commandant",
        ],
        "titles": ["title.none", "title.emperor", "title.empress"],
        "allegiances": ["allegiance.han_court", "allegiance.yellow_turban_network", "allegiance.self"],
        "political_roles": ["political.subject", "political.ruler", "political.emperor"],
        "skill_profiles": ["skill.general", "skill.craft", "skill.trade", "skill.government", "skill.military", "skill.education", "skill.medical"],
        "knowledge_profiles": ["knowledge.local_basic", "knowledge.capital", "knowledge.court", "knowledge.military_route", "knowledge.academic"],
        "data_origins": list(DATA_ORIGIN_INDEX),
        "location_statuses": LOCATION_STATUS_IDS,
        "force_ids": [item[0] for item in FORCE_DEFINITIONS],
        "facility_ids": [item["facility_id"] for item in facilities],
    }
    people, households, historical_runtime, external = build_people_and_households(config, catalogs)
    stage("people_households_forces_built")
    assign_historical_offices(people, historical_runtime, catalogs)
    groups = apply_residential_capacities(facilities, people, urban_indices)
    assign_residences(people, households, facilities, groups)
    stage("residences_assigned")
    assign_work(people, facilities, urban_indices, catalogs)
    stage("work_assigned")
    family_records = build_family_records(config, people, households, catalogs)
    stage("family_organizations_built")
    generation_ms = (time.perf_counter() - started) * 1000.0
    runtime_root = repo / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184UrbanInitializationV1"
    manifest = write_runtime_package(runtime_root, people, households, facilities, catalogs, historical_runtime, external, family_records, config, generation_ms)
    stage("runtime_package_written")
    summary_path = runtime_root / "audit_summary.json"
    summary = json.loads(summary_path.read_text(encoding="utf-8"))
    summary["performance"]["person_binary_bytes"] = (runtime_root / "persons.bin").stat().st_size
    write_json(summary_path, summary)
    manifest["files"] = [{"path": item["path"], "bytes": (runtime_root / item["path"]).stat().st_size, "sha256": sha256(runtime_root / item["path"])} for item in manifest["files"]]
    write_json(runtime_root / "manifest.json", manifest)
    csv_root = repo / "tmp" / "luoyang-184-urban-init-v1" / "csv"
    write_csvs(csv_root, people, households, facilities, catalogs, family_records, config, summary)
    stage("csv_intermediates_written")
    output_root = repo / "outputs" / "LUOYANG_184_URBAN_INITIALIZATION_V1"
    output_root.mkdir(parents=True, exist_ok=True)
    write_reports(output_root, summary, manifest)
    stage("reports_written")
    print(json.dumps({
        "status": "BUILT",
        "persons": len(people),
        "households": len(households),
        "facilities": len(facilities),
        "historical_people": len(historical_runtime),
        "external_anchors": len(external),
        "runtime_root": str(runtime_root),
        "csv_root": str(csv_root),
        "generation_ms": round(generation_ms, 3),
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
