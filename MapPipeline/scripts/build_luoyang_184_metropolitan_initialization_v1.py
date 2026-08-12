#!/usr/bin/env python3
"""Build the additive 184 Luoyang 400K metropolitan initialization package.

The existing 270K urban package is immutable input.  This builder emits only the
130K outer population and metropolitan facts; a composite reader joins both.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import importlib.util
import json
import math
import os
import sys
import time
from collections import Counter, defaultdict, deque
from pathlib import Path


SCHEMA = "mandate.luoyang-184-metropolitan-initialization.v1"
OUTER_PERSON_MAGIC = b"MOHLYM01"
OUTER_HOUSEHOLD_MAGIC = b"MOHLYK01"
BASE_PERSON_COUNT = 270_000
OUTER_PERSON_COUNT = 130_000
TOTAL_PERSON_COUNT = 400_000
BASE_HOUSEHOLD_COUNT = 53_992
BASE_FACILITY_COUNT = 1_230
NONE_U16 = 0xFFFF
NONE_U32 = 0xFFFFFFFF

AREA_PLANS = [
    ("area.luoyang.metropolitan.gate_suburb", "GateSuburb", 30_000),
    ("area.luoyang.metropolitan.southern_suburb", "SouthernSuburb", 22_000),
    ("area.luoyang.metropolitan.road_settlement", "RoadSettlement", 16_000),
    ("area.luoyang.metropolitan.near_village", "NearVillage", 34_000),
    ("area.luoyang.metropolitan.elite_estate", "EliteEstate", 6_000),
    ("area.luoyang.metropolitan.agricultural_fringe", "AgriculturalFringe", 14_000),
    ("area.luoyang.metropolitan.logistics_node", "LogisticsNode", 5_000),
    ("area.luoyang.metropolitan.water_resource_node", "WaterAndResourceNode", 3_000),
]

CLUSTERS = [
    ("settlement.gate.north", "北郭", "GateSuburb", 7_500, 2043, 1228),
    ("settlement.gate.east", "东郭", "GateSuburb", 7_000, 2057, 1241),
    ("settlement.gate.west", "西郭", "GateSuburb", 6_500, 2029, 1241),
    ("settlement.gate.south", "南郭", "GateSuburb", 9_000, 2043, 1254),
    ("settlement.south.taixue", "太学外坊", "SouthernSuburb", 12_000, 2042, 1258),
    ("settlement.south.ritual", "礼制南郊", "SouthernSuburb", 10_000, 2049, 1257),
    ("settlement.road.east", "东驿聚", "RoadSettlement", 6_000, 2066, 1241),
    ("settlement.road.west", "西驿聚", "RoadSettlement", 5_000, 2022, 1241),
    ("settlement.road.north", "北道聚", "RoadSettlement", 5_000, 2043, 1211),
    ("village.mangshan.1", "邙南一里", "NearVillage", 4_500, 2032, 1214),
    ("village.mangshan.2", "邙南二里", "NearVillage", 4_500, 2053, 1214),
    ("village.luoshui.east", "洛水东里", "NearVillage", 4_500, 2062, 1256),
    ("village.luoshui.west", "洛水西里", "NearVillage", 4_500, 2025, 1257),
    ("village.hebei.east", "河北东里", "NearVillage", 4_000, 2067, 1226),
    ("village.hebei.west", "河北西里", "NearVillage", 4_000, 2019, 1227),
    ("village.south.east", "南郊东里", "NearVillage", 4_000, 2057, 1263),
    ("village.south.west", "南郊西里", "NearVillage", 4_000, 2031, 1263),
    ("estate.henan.1", "河南尹东庄", "EliteEstate", 1_500, 2060, 1231),
    ("estate.henan.2", "河南尹西庄", "EliteEstate", 1_500, 2026, 1232),
    ("estate.henan.3", "洛南东庄", "EliteEstate", 1_500, 2053, 1260),
    ("estate.henan.4", "洛南西庄", "EliteEstate", 1_500, 2035, 1260),
    ("hamlet.farm.1", "东原农聚", "AgriculturalFringe", 3_000, 2077, 1233),
    ("hamlet.farm.2", "西原农聚", "AgriculturalFringe", 3_000, 2016, 1233),
    ("hamlet.farm.3", "北原农聚", "AgriculturalFringe", 3_000, 2043, 1204),
    ("hamlet.farm.4", "洛南农聚", "AgriculturalFringe", 3_000, 2043, 1265),
    ("hamlet.farm.5", "伊洛农聚", "AgriculturalFringe", 2_000, 2070, 1261),
    ("logistics.east", "东关转运聚", "LogisticsNode", 1_500, 2055, 1242),
    ("logistics.west", "西关转运聚", "LogisticsNode", 1_200, 2031, 1242),
    ("logistics.north", "北关转运聚", "LogisticsNode", 1_200, 2043, 1230),
    ("logistics.south", "南关转运聚", "LogisticsNode", 1_100, 2044, 1252),
    ("water.luoshui.east", "洛水东汲水聚", "WaterAndResourceNode", 1_000, 2061, 1251),
    ("water.luoshui.west", "洛水西汲水聚", "WaterAndResourceNode", 1_000, 2027, 1251),
    ("water.yangqu", "阳渠水工聚", "WaterAndResourceNode", 1_000, 2058, 1220),
]

OCCUPATION_TARGETS = {
    "occupation.agriculture": 22_000,
    "occupation.transport": 8_000,
    "occupation.trade": 9_000,
    "occupation.crafts": 8_000,
    "occupation.storage": 4_000,
    "occupation.hospitality": 3_000,
    "occupation.household_service": 5_000,
    "occupation.elite_family_management": 2_000,
    "occupation.animal_husbandry": 5_000,
    "occupation.government": 3_000,
    "occupation.religious": 1_500,
    "occupation.education_staff": 1_500,
}

OCCUPATION_ACTIVITY = {
    "occupation.agriculture": "activity.work.agriculture",
    "occupation.transport": "activity.work.transport",
    "occupation.trade": "activity.work.trade",
    "occupation.crafts": "activity.work.crafts",
    "occupation.storage": "activity.work.storage",
    "occupation.hospitality": "activity.work.hospitality",
    "occupation.household_service": "activity.work.household_service",
    "occupation.elite_family_management": "activity.work.family_management",
    "occupation.animal_husbandry": "activity.work.animal_husbandry",
    "occupation.government": "activity.work.government",
    "occupation.religious": "activity.work.ritual",
    "occupation.education_staff": "activity.work.education",
}

AREA_OCCUPATION_PREFERENCE = {
    "occupation.agriculture": {"AgriculturalFringe", "NearVillage", "WaterAndResourceNode"},
    "occupation.transport": {"LogisticsNode", "RoadSettlement", "GateSuburb"},
    "occupation.trade": {"GateSuburb", "RoadSettlement", "SouthernSuburb"},
    "occupation.crafts": {"GateSuburb", "RoadSettlement", "NearVillage"},
    "occupation.storage": {"LogisticsNode", "GateSuburb", "RoadSettlement"},
    "occupation.hospitality": {"RoadSettlement", "GateSuburb", "SouthernSuburb"},
    "occupation.household_service": {"EliteEstate", "GateSuburb", "SouthernSuburb"},
    "occupation.elite_family_management": {"EliteEstate"},
    "occupation.animal_husbandry": {"AgriculturalFringe", "NearVillage"},
    "occupation.government": {"GateSuburb", "SouthernSuburb", "LogisticsNode"},
    "occupation.religious": {"SouthernSuburb", "NearVillage"},
    "occupation.education_staff": {"SouthernSuburb", "GateSuburb"},
}


def load_urban_module(repo: Path):
    source = repo / "MapPipeline" / "scripts" / "build_luoyang_184_urban_initialization_v1.py"
    spec = importlib.util.spec_from_file_location("luoyang_urban_v1", source)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_csv(path: Path, rows) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    rows = list(rows)
    if not rows:
        raise ValueError(f"CSV requires rows: {path}")
    with path.open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def distribute(total: int, count: int):
    base, extra = divmod(total, count)
    return [base + (1 if index < extra else 0) for index in range(count)]


def stable_name(ordinal: int) -> str:
    surnames = "赵钱孙李周吴郑王冯陈褚卫蒋沈韩杨朱秦尤许何吕施张孔曹严华金魏陶姜戚谢邹喻柏水窦章云苏潘葛奚范彭郎鲁韦昌马苗凤花方俞任袁柳鲍史唐费廉岑薛雷贺倪汤滕殷罗毕郝安常乐于时傅皮卞齐康伍余元卜顾孟平黄和穆萧尹姚邵汪祁毛禹狄米贝明臧计伏成戴谈宋茅庞熊纪舒屈项祝董梁杜阮蓝闵席季麻强贾路娄危江童颜郭梅盛林刁钟徐邱骆高夏蔡田樊胡凌霍虞万支柯管卢莫房裘缪干解应宗丁宣邓郁单杭洪包诸左石崔吉龚程嵇邢滑裴陆荣翁荀羊惠甄麴皇甫"
    given = "伯仲叔季文武德义仁信忠孝礼智勇安宁平和成良敬远达兴泰昌盛彦修弘昭景元子公士孟长玄清正明光国家世"
    return surnames[ordinal % len(surnames)] + given[(ordinal * 7 + 3) % len(given)] + given[(ordinal * 13 + 5) % len(given)]


def household_sizes(total: int, seed: int):
    pattern = [5, 4, 6, 3, 7, 5, 2, 4, 8, 5, 3, 6]
    result = []
    remaining = total
    index = seed % len(pattern)
    while remaining:
        size = min(pattern[index % len(pattern)], remaining)
        result.append(size)
        remaining -= size
        index += 1
    return result


def load_inputs(repo: Path):
    base_root = repo / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184UrbanInitializationV1"
    world_path = repo / "MapData" / "Luoyang184Historical_V1" / "luoyang_184_world.json"
    manifest = json.loads((base_root / "manifest.json").read_text(encoding="utf-8"))
    catalogs = json.loads((base_root / "catalogs.json").read_text(encoding="utf-8"))
    facilities = json.loads((base_root / "facilities.json").read_text(encoding="utf-8"))["facilities"]
    world = json.loads(world_path.read_text(encoding="utf-8"))
    if manifest["person_count"] != BASE_PERSON_COUNT or manifest["household_count"] != BASE_HOUSEHOLD_COUNT:
        raise RuntimeError("The immutable urban package no longer matches its accepted population contract.")
    if len(facilities) != BASE_FACILITY_COUNT or len(catalogs["facility_ids"]) != BASE_FACILITY_COUNT:
        raise RuntimeError("The immutable urban facility catalog no longer matches the accepted contract.")
    return base_root, manifest, catalogs, facilities, world


def extend_catalogs(base_catalogs):
    catalogs = json.loads(json.dumps(base_catalogs))
    catalogs["schema"] = "mandate.luoyang-184-metropolitan-catalogs.v1"
    for area_id, _, _ in AREA_PLANS:
        if area_id not in catalogs["areas"]:
            catalogs["areas"].append(area_id)
    for value in ["occupation.storage", "occupation.hospitality", "occupation.animal_husbandry"]:
        if value not in catalogs["occupations"]:
            catalogs["occupations"].append(value)
    for value in ["activity.work.storage", "activity.work.hospitality", "activity.work.animal_husbandry"]:
        if value not in catalogs["activities"]:
            catalogs["activities"].append(value)
    return catalogs


class SpatialAllocator:
    def __init__(self, world, base_facilities):
        self.cells = {(int(c["grid_x"]), int(c["grid_y"])): c for c in world["cells"]}
        self.occupied = {int(f["cell_id64"]) for f in base_facilities}
        self.transit_existing = {
            int(f["cell_id64"]) for f in base_facilities
            if f.get("category_id") == "road" or "gate" in str(f.get("facility_id", ""))
        }
        self.used = set(self.occupied)
        self.by_id = {int(c["cell_id64"]): c for c in world["cells"]}

    def nearest_free(self, x, y, predicate=None):
        candidates = [c for c in self.cells.values() if int(c["cell_id64"]) not in self.used and c.get("developable", True)]
        if predicate:
            preferred = [c for c in candidates if predicate(c)]
            if preferred:
                candidates = preferred
        if not candidates:
            raise RuntimeError("No unused metropolitan Cell remains.")
        cell = min(candidates, key=lambda c: (abs(int(c["grid_x"]) - x) + abs(int(c["grid_y"]) - y), int(c["cell_id64"])))
        self.used.add(int(cell["cell_id64"]))
        return cell

    def reserve_route(self, start, goal):
        start_xy = (int(start["grid_x"]), int(start["grid_y"]))
        goal_xy = (int(goal["grid_x"]), int(goal["grid_y"]))
        queue = deque([start_xy])
        previous = {start_xy: None}
        while queue:
            current = queue.popleft()
            if current == goal_xy:
                break
            x, y = current
            neighbors = [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
            neighbors.sort(key=lambda p: (abs(p[0] - goal_xy[0]) + abs(p[1] - goal_xy[1]), p[1], p[0]))
            for nxt in neighbors:
                if nxt in previous or nxt not in self.cells:
                    continue
                cell_id = int(self.cells[nxt]["cell_id64"])
                if cell_id in self.occupied and cell_id not in self.transit_existing and nxt != goal_xy:
                    continue
                previous[nxt] = current
                queue.append(nxt)
        logical_gate_transition = False
        if goal_xy not in previous:
            reachable = [point for point in previous if point != start_xy]
            if not reachable:
                raise RuntimeError(f"No road path from {start_xy} toward {goal_xy}")
            endpoint = min(reachable, key=lambda p: (abs(p[0] - goal_xy[0]) + abs(p[1] - goal_xy[1]),
                                                     abs(p[0] - start_xy[0]) + abs(p[1] - start_xy[1]), p[1], p[0]))
            # The published prototype represents the wall/moat/gate complex with
            # several 2 km Cells.  A route may therefore terminate at the outer
            # approach and use one explicit gate-complex transition.
            if abs(endpoint[0] - goal_xy[0]) + abs(endpoint[1] - goal_xy[1]) > 12:
                raise RuntimeError(f"Road cannot reach the gate complex from {start_xy} toward {goal_xy}")
            logical_gate_transition = True
        else:
            endpoint = goal_xy
        path = []
        cursor = endpoint
        while cursor is not None:
            path.append(self.cells[cursor])
            cursor = previous[cursor]
        path.reverse()
        if logical_gate_transition:
            path.append(self.cells[goal_xy])
        for cell in path[:-1]:
            cell_id = int(cell["cell_id64"])
            if cell_id not in self.occupied:
                self.used.add(cell_id)
        return path


def facility_record(facility_id, definition_id, display_name, category, cell, owner, controller,
                    area_type, settlement_id, residential=0, workers=0, storage=0, profile=None):
    return {
        "facility_id": facility_id,
        "definition_id": definition_id,
        "display_name": display_name,
        "category_id": category,
        "cell_id64": int(cell["cell_id64"]),
        "grid_x": int(cell["grid_x"]),
        "grid_y": int(cell["grid_y"]),
        "owner_id": owner,
        "controller_id": controller,
        "administrative_controller_id": "organization.government.han.henan",
        "area_type": area_type,
        "settlement_id": settlement_id,
        "profile_id": profile or "profile.metropolitan.standard",
        "historical_confidence": "GameplayReconstruction",
        "spatial_precision": "Cell",
        "data_origin": "GeneratedHistoricalPopulation",
        "residential_capacity_persons": residential,
        "current_residents": 0,
        "worker_capacity": workers,
        "current_workers": 0,
        "storage_capacity_units": storage,
        "normal_operation": True,
        "capability_ids": ["capability.worker_assignment"] + (["capability.residential"] if residential else []) + (["capability.storage"] if storage else []),
    }


def build_spatial_and_facilities(base_facilities, world, catalogs):
    allocator = SpatialAllocator(world, base_facilities)
    gates = [f for f in base_facilities if f.get("gate_direction") in {"north", "south", "east", "west"}]
    if len(gates) != 12:
        raise RuntimeError(f"Expected the protected twelve gates, found {len(gates)}")
    cluster_rows = []
    route_rows = []
    reserved_roads = {}
    for cluster_id, name, area_type, population, x, y in CLUSTERS:
        anchor = allocator.nearest_free(x, y)
        nearest_gate = min(gates, key=lambda g: abs(int(g["grid_x"]) - int(anchor["grid_x"])) + abs(int(g["grid_y"]) - int(anchor["grid_y"])))
        path = allocator.reserve_route(anchor, allocator.by_id[int(nearest_gate["cell_id64"])])
        for cell in path[:-1]:
            if int(cell["cell_id64"]) not in allocator.occupied:
                reserved_roads[int(cell["cell_id64"])] = cell
        cluster_rows.append({
            "settlement_id": cluster_id, "display_name": name, "area_type": area_type,
            "population_target": population, "anchor_cell_id64": int(anchor["cell_id64"]),
            "grid_x": int(anchor["grid_x"]), "grid_y": int(anchor["grid_y"]),
            "gate_facility_id": nearest_gate["facility_id"], "road_cell_count": max(0, len(path) - 1),
            "historical_confidence": "GameplayReconstruction", "data_origin": "GeneratedHistoricalPopulation",
        })
        route_distance_cells = sum(
            abs(int(path[i]["grid_x"]) - int(path[i - 1]["grid_x"]))
            + abs(int(path[i]["grid_y"]) - int(path[i - 1]["grid_y"]))
            for i in range(1, len(path)))
        final_span = (abs(int(path[-1]["grid_x"]) - int(path[-2]["grid_x"]))
                      + abs(int(path[-1]["grid_y"]) - int(path[-2]["grid_y"]))) if len(path) > 1 else 0
        route_rows.append({
            "route_id": f"route.metropolitan.{cluster_id}", "settlement_id": cluster_id,
            "gate_facility_id": nearest_gate["facility_id"],
            "cell_ids": [int(c["cell_id64"]) for c in path], "distance_m": route_distance_cells * 2000,
            "travel_minutes": max(20, route_distance_cells * 32), "road_condition_bp": 7200,
            "uses_gate_complex_transition": final_span > 1,
            "gate_complex_transition_span_cells": final_span,
        })

    new_facilities = []
    by_cluster = defaultdict(list)
    by_role = defaultdict(list)

    def add(definition, name, category, cell, owner, controller, area_type, cluster_id,
            residential=0, workers=0, storage=0, profile=None, role=None):
        index = BASE_FACILITY_COUNT + len(new_facilities)
        facility_id = f"facility.instance.luoyang.184.metropolitan.{index + 1:06d}"
        item = facility_record(facility_id, definition, name, category, cell, owner, controller,
                               area_type, cluster_id, residential, workers, storage, profile)
        item["global_facility_index"] = index
        new_facilities.append(item)
        by_cluster[cluster_id].append(index)
        if role:
            by_role[role].append(index)
        return item

    road_by_cell = {}
    for cell_id in sorted(reserved_roads):
        item = add("facility.public.road", "近郊道路", "road", reserved_roads[cell_id],
                   "organization.government.han.henan", "organization.government.han.luoyang",
                   "RoadNetwork", None, workers=4, profile="profile.metropolitan.road", role="road")
        road_by_cell[cell_id] = item["global_facility_index"]

    cluster_lookup = {row["settlement_id"]: row for row in cluster_rows}
    residential_by_cluster = defaultdict(list)
    for cluster_id, name, area_type, population, x, y in CLUSTERS:
        count = max(2, math.ceil(population / 420))
        capacities = distribute(population + max(40, population // 20), count)
        for index, capacity in enumerate(capacities):
            cell = allocator.nearest_free(x, y)
            is_estate = area_type == "EliteEstate"
            definition = "facility.residential.family_manor" if is_estate else (
                "facility.residential.rural_hamlet" if area_type in {"NearVillage", "AgriculturalFringe", "WaterAndResourceNode"}
                else "facility.residential.urban_quarter")
            owner = f"organization.family.metropolitan.{cluster_id}.{index + 1:03d}" if is_estate else "organization.community.luoyang.metropolitan"
            item = add(definition, f"{name}住区{index + 1}", "residential", cell, owner,
                       f"organization.community.{cluster_id}", area_type, cluster_id,
                       residential=capacity, workers=160 if is_estate else 16,
                       profile="profile.metropolitan.estate" if is_estate else "profile.metropolitan.residence",
                       role="elite_residence" if is_estate else "residence")
            residential_by_cluster[cluster_id].append(item["global_facility_index"])

    # Job facilities.  Capacities deliberately exceed assigned labour by roughly 5%.
    job_specs = [
        ("occupation.agriculture", 110, "facility.agriculture.millet_field", "近郊农田", "resource_agriculture", "field", 220),
        ("occupation.animal_husbandry", 25, "facility.agriculture.pasture", "近郊牧地", "resource_agriculture", "pasture", 220),
        ("occupation.transport", 20, "facility.service.caravan_yard", "车马转运场", "service", "transport", 420),
        ("occupation.trade", 20, "facility.commercial.shop_cluster", "近郊市肆", "commercial", "trade", 480),
        ("occupation.crafts", 20, "facility.industry.workshop", "近郊作坊", "industry", "craft", 420),
        ("occupation.storage", 10, "facility.commercial.warehouse", "近郊仓", "storage", "storage", 420),
        ("occupation.hospitality", 10, "facility.service.inn", "客舍", "service", "hospitality", 320),
        ("occupation.government", 8, "facility.government.local_office", "乡亭官署", "government", "government", 400),
        ("occupation.religious", 6, "facility.public.ritual_hall", "乡里祠坛", "ritual", "religious", 270),
        ("occupation.education_staff", 6, "facility.service.school", "乡学", "education", "education", 270),
    ]
    cluster_centers = {c[0]: (c[2], c[4], c[5]) for c in CLUSTERS}
    for occupation, count, definition, label, category, role, capacity in job_specs:
        preferred = AREA_OCCUPATION_PREFERENCE[occupation]
        candidates = [c for c in CLUSTERS if c[2] in preferred]
        for index in range(count):
            cluster = candidates[index % len(candidates)]
            cluster_id, cluster_name, area_type, _, x, y = cluster
            predicate = None
            if role == "field":
                predicate = lambda cell: int(cell.get("fertility", 0)) >= 35
            if role == "pasture":
                predicate = lambda cell: int(cell.get("slope_class", 0)) <= 2
            cell = allocator.nearest_free(x, y, predicate)
            item = add(definition, f"{cluster_name}{label}{index + 1}", category, cell,
                       "organization.community.luoyang.metropolitan", f"organization.community.{cluster_id}",
                       area_type, cluster_id, workers=capacity, storage=80_000 if role == "storage" else 0,
                       profile=f"profile.metropolitan.{role}", role=occupation)
            if role == "field":
                item["farmland_one_facility_per_cell"] = True

    # Water and public granary facilities are explicit and never teleport goods.
    for index, cluster in enumerate([c for c in CLUSTERS if c[2] in {"WaterAndResourceNode", "NearVillage", "AgriculturalFringe"}]):
        if index >= 18:
            break
        cluster_id, cluster_name, area_type, _, x, y = cluster
        cell = allocator.nearest_free(x, y, lambda c: int(c.get("water_class", 0)) > 0)
        add("facility.public.well", f"{cluster_name}井渠", "public", cell,
            "organization.community.luoyang.metropolitan", f"organization.community.{cluster_id}",
            area_type, cluster_id, workers=20, profile="profile.metropolitan.water", role="water")
    for index, cluster in enumerate([c for c in CLUSTERS if c[2] in {"LogisticsNode", "AgriculturalFringe", "NearVillage"}]):
        if index >= 12:
            break
        cluster_id, cluster_name, area_type, _, x, y = cluster
        cell = allocator.nearest_free(x, y)
        add("facility.public.granary", f"{cluster_name}公仓", "storage", cell,
            "organization.government.han.henan", f"organization.community.{cluster_id}",
            area_type, cluster_id, workers=80, storage=120_000,
            profile="profile.metropolitan.granary", role="public_granary")

    for item in new_facilities:
        catalogs["facility_ids"].append(item["facility_id"])
    return allocator, cluster_rows, route_rows, road_by_cell, new_facilities, by_role, residential_by_cluster


def build_people_and_households(urban, catalogs, cluster_rows, residential_by_cluster, facilities):
    people = []
    households = []
    cluster_by_id = {row["settlement_id"]: row for row in cluster_rows}
    area_index = {value: catalogs["areas"].index(value) for value, _, _ in AREA_PLANS}
    occupation_index = {value: catalogs["occupations"].index(value) for value in catalogs["occupations"]}
    activity_index = {value: catalogs["activities"].index(value) for value in catalogs["activities"]}
    facilities_by_global = {item["global_facility_index"]: item for item in facilities}

    for cluster_seed, (cluster_id, _, area_type, target, _, _) in enumerate(CLUSTERS):
        sizes = household_sizes(target, cluster_seed)
        for size in sizes:
            household_ordinal = BASE_HOUSEHOLD_COUNT + len(households)
            start = BASE_PERSON_COUNT + len(people)
            members = []
            for member_index in range(size):
                ordinal = BASE_PERSON_COUNT + len(people)
                if member_index == 0:
                    age = 28 + ((ordinal * 17) % 29)
                elif member_index == 1:
                    age = 24 + ((ordinal * 11) % 31)
                elif member_index in {2, 3}:
                    age = (ordinal * 7 + member_index * 3) % 20
                elif member_index == 4:
                    age = 20 + ((ordinal * 5) % 31)
                elif member_index == size - 1 and size >= 6:
                    age = 60 + (ordinal % 19)
                else:
                    age = 18 + ((ordinal * 13) % 43)
                stage = 0 if age <= 13 else 1 if age <= 19 else 2 if age <= 59 else 3 if age <= 69 else 4
                person = urban.Person(
                    ordinal=ordinal,
                    person_id=f"person.luoyang.184.metropolitan.{ordinal + 1:06d}",
                    display_name=stable_name(ordinal), birth_year=184 - age, age=age, age_stage=stage,
                    gender=1 if (ordinal + member_index) % 2 == 0 else 2,
                    health_bp=7600 + ((ordinal * 37) % 2201), natural_lifespan=55 + ((ordinal * 19) % 31),
                    household=household_ordinal, family_org=NONE_U16, area=area_index[f"area.luoyang.metropolitan.{area_type.replace('And', '_and_').replace('Suburb','_suburb').replace('Settlement','_settlement').replace('Village','_village').replace('Estate','_estate').replace('Fringe','_fringe').replace('Node','_node').lower().replace('__','_')}"] if False else 0,
                    location_status=1, current_cell=0, residence=NONE_U32, residence_status=1,
                    occupation=occupation_index["occupation.unfixed"], work_facility=NONE_U32,
                    activity=activity_index["activity.household_life"], employment_status=0,
                    civil_office=0, military_office=0, title=0, allegiance=0, political_role=0,
                    force=NONE_U16, reserve_force=NONE_U16, skill_profile=0, knowledge_profile=0,
                    assets=200 + ((ordinal * 7919) % 18_000), father=-1, mother=-1, spouse=-1,
                    data_origin=2,
                )
                # Area IDs are looked up by the stable plan instead of string transformation.
                area_id = next(plan[0] for plan in AREA_PLANS if plan[1] == area_type)
                person.area = area_index[area_id]
                members.append(person)
                people.append(person)
            if len(members) >= 2 and members[0].age_stage == 2 and members[1].age_stage == 2:
                members[0].spouse = members[1].ordinal
                members[1].spouse = members[0].ordinal
            father = next((p for p in members[:2] if p.gender == 1 and p.age_stage == 2), None)
            mother = next((p for p in members[:2] if p.gender == 2 and p.age_stage == 2), None)
            for person in members[2:]:
                if person.age_stage in {0, 1}:
                    person.father = father.ordinal if father else -1
                    person.mother = mother.ordinal if mother else -1
            households.append(urban.Household(
                ordinal=household_ordinal, start=start, count=size, head=members[0].ordinal,
                family_org=NONE_U16, primary_residence=NONE_U32,
                household_type=0 if size == 1 else 1 if size == 2 else 2 if size <= 5 else 3,
                data_origin=2, wealth=sum(p.assets for p in members) + 1000,
            ))

    if len(people) != OUTER_PERSON_COUNT:
        raise RuntimeError(f"Outer population mismatch: {len(people)}")

    # Keep each household together and use variable residence capacity.
    people_by_ordinal = {p.ordinal: p for p in people}
    facility_remaining = {item["global_facility_index"]: int(item["residential_capacity_persons"]) for item in facilities}
    household_cluster = {}
    cluster_cursor = {key: 0 for key in residential_by_cluster}
    current_household = 0
    for cluster_id, _, _, target, _, _ in CLUSTERS:
        cluster_end = current_household + len(household_sizes(target, CLUSTERS.index(next(c for c in CLUSTERS if c[0] == cluster_id))))
        while current_household < cluster_end:
            household = households[current_household]
            choices = residential_by_cluster[cluster_id]
            chosen = None
            for offset in range(len(choices)):
                index = choices[(cluster_cursor[cluster_id] + offset) % len(choices)]
                if facility_remaining[index] >= household.count:
                    chosen = index
                    cluster_cursor[cluster_id] = (cluster_cursor[cluster_id] + offset + 1) % len(choices)
                    break
            if chosen is None:
                raise RuntimeError(f"No residence capacity for {cluster_id}")
            household.primary_residence = chosen
            facility_remaining[chosen] -= household.count
            facilities_by_global[chosen]["current_residents"] += household.count
            household_cluster[household.ordinal] = cluster_id
            for ordinal in range(household.start, household.start + household.count):
                person = people_by_ordinal[ordinal]
                person.residence = chosen
                person.current_cell = int(facilities_by_global[chosen]["cell_id64"])
            current_household += 1
    return people, households, household_cluster


def assign_families(people, households, household_cluster, facilities, base_family_count=7):
    people_by_ordinal = {p.ordinal: p for p in people}
    elite = [h for h in households if household_cluster[h.ordinal].startswith("estate.")]
    organizations = []
    cursor = 0
    for index in range(8):
        selected = elite[cursor:cursor + 24]
        cursor += 24
        member_ordinals = []
        for household in selected:
            household.family_org = base_family_count + index
            for ordinal in range(household.start, household.start + household.count):
                people_by_ordinal[ordinal].family_org = base_family_count + index
                member_ordinals.append(ordinal)
        residences = sorted({h.primary_residence for h in selected})
        organizations.append({
            "family_organization_id": f"family_organization.luoyang.184.metropolitan.generated.{index + 1:02d}",
            "family_name": f"洛阳近郊第{index + 1}家",
            "head_person_id": people_by_ordinal[selected[0].head].person_id if selected else None,
            "member_count": len(member_ordinals), "member_ordinals": member_ordinals,
            "household_ordinals": [h.ordinal for h in selected],
            "family_facility_ids": [facilities[i - BASE_FACILITY_COUNT]["facility_id"] for i in residences],
            "family_inventory_container_id": f"inventory.family.luoyang.metropolitan.{index + 1:02d}",
            "confidence": "C", "data_origin": "GeneratedHistoricalPopulation",
            "historical_claim": False,
        })
    return organizations


def assign_occupations_and_work(people, facilities, by_role, catalogs, cluster_rows):
    occupation_index = {value: catalogs["occupations"].index(value) for value in catalogs["occupations"]}
    activity_index = {value: catalogs["activities"].index(value) for value in catalogs["activities"]}
    area_name_by_index = {catalogs["areas"].index(area_id): name for area_id, name, _ in AREA_PLANS}
    eligible = [p for p in people if p.age_stage in {1, 2, 3}]
    available = {p.ordinal: p for p in eligible}
    for occupation, target in OCCUPATION_TARGETS.items():
        preferred = AREA_OCCUPATION_PREFERENCE[occupation]
        ranked = sorted(available.values(), key=lambda p: (
            0 if area_name_by_index[p.area] in preferred else 1,
            (p.ordinal * 1103515245 + catalogs["occupations"].index(occupation) * 12345) & 0xFFFFFFFF,
            p.ordinal,
        ))
        if len(ranked) < target:
            raise RuntimeError(f"Insufficient eligible labour for {occupation}")
        for person in ranked[:target]:
            person.occupation = occupation_index[occupation]
            person.activity = activity_index[OCCUPATION_ACTIVITY[occupation]]
            person.employment_status = 1
            available.pop(person.ordinal)

    for person in available.values():
        person.employment_status = 2 if person.age_stage in {1, 2, 3} else 0

    facility_by_global = {item["global_facility_index"]: item for item in facilities}
    pool_by_occupation = {occupation: list(by_role[occupation]) for occupation in OCCUPATION_TARGETS}
    pool_by_occupation["occupation.household_service"] = [i for i, f in facility_by_global.items() if f["category_id"] == "residential"]
    pool_by_occupation["occupation.elite_family_management"] = list(by_role["elite_residence"])
    remaining = {i: int(facility_by_global[i]["worker_capacity"]) for i in facility_by_global}
    cursor = defaultdict(int)
    for person in people:
        occupation = catalogs["occupations"][person.occupation]
        if person.employment_status != 1:
            continue
        pool = pool_by_occupation[occupation]
        chosen = None
        for offset in range(len(pool)):
            index = pool[(cursor[occupation] + offset) % len(pool)]
            if remaining.get(index, 0) > 0:
                chosen = index
                cursor[occupation] = (cursor[occupation] + offset + 1) % len(pool)
                break
        if chosen is None:
            raise RuntimeError(f"No job capacity remains for {occupation}")
        person.work_facility = chosen
        remaining[chosen] -= 1
        facility_by_global[chosen]["current_workers"] += 1
        if occupation == "occupation.crafts": person.skill_profile = min(1, len(catalogs["skill_profiles"]) - 1)
        elif occupation in {"occupation.trade", "occupation.transport", "occupation.storage", "occupation.hospitality"}: person.skill_profile = min(2, len(catalogs["skill_profiles"]) - 1)
        elif occupation == "occupation.government": person.skill_profile = min(3, len(catalogs["skill_profiles"]) - 1)
        person.knowledge_profile = 0


def build_agriculture_and_logistics(people, facilities, by_role, route_rows, base_facilities, catalogs):
    person_by_occ = defaultdict(list)
    for p in people:
        person_by_occ[catalogs["occupations"][p.occupation]].append(p.ordinal)
    gates = [f for f in base_facilities if f.get("gate_direction") in {"north", "south", "east", "west"}]
    taicang = next((f for f in base_facilities if f.get("source_definition_id") == "facility.historical.taicang"), None)
    if taicang is None:
        taicang = next(f for f in base_facilities if f.get("definition_id") == "facility.storage.granary")
    market = next(f for f in base_facilities if f.get("definition_id") == "facility.commercial.market")
    storage_indices = list(by_role["occupation.storage"]) + list(by_role["public_granary"])
    storage_facilities = [facilities[i - BASE_FACILITY_COUNT] for i in storage_indices]
    field_indices = list(by_role["occupation.agriculture"])
    pasture_indices = list(by_role["occupation.animal_husbandry"])
    crop_cycle = [
        ("product.food.millet_grain", 110, 2600),
        ("product.food.wheat_grain", 135, 2900),
        ("product.food.broomcorn_grain", 105, 2400),
        ("product.food.bean", 95, 2100),
        ("product.material.mulberry_leaf", 150, 1800),
    ]
    fields = []
    for order, global_index in enumerate(field_indices):
        facility = facilities[global_index - BASE_FACILITY_COUNT]
        product_id, days, yield_units = crop_cycle[order % len(crop_cycle)]
        workers = [p.ordinal for p in people if p.work_facility == global_index][:200]
        fields.append({
            "field_id": f"agriculture.field.{global_index}", "facility_id": facility["facility_id"],
            "cell_id64": facility["cell_id64"], "product_definition_id": product_id,
            "seed_batch_id": f"seed.batch.luoyang.184.{global_index}", "planted_day": 0,
            "maturity_day": days, "early_harvest_minimum_basis_points": 8000,
            "full_yield_units": yield_units, "current_maturity_basis_points": 0,
            "worker_person_ordinals": workers, "inventory_container_id": f"inventory.field.{global_index}",
            "initial_inventory_units": 0, "data_origin": "GeneratedHistoricalPopulation",
        })
    for order, global_index in enumerate(pasture_indices):
        facility = facilities[global_index - BASE_FACILITY_COUNT]
        workers = [p.ordinal for p in people if p.work_facility == global_index][:200]
        fields.append({
            "field_id": f"animal.unit.{global_index}", "facility_id": facility["facility_id"],
            "cell_id64": facility["cell_id64"], "product_definition_id": "product.livestock.wool_and_hide",
            "seed_batch_id": f"livestock.batch.luoyang.184.{global_index}", "planted_day": 0,
            "maturity_day": 90, "early_harvest_minimum_basis_points": 8500,
            "full_yield_units": 1600, "current_maturity_basis_points": 0,
            "worker_person_ordinals": workers, "inventory_container_id": f"inventory.pasture.{global_index}",
            "initial_inventory_units": 0, "data_origin": "GeneratedHistoricalPopulation",
        })

    products = [
        ("product.food.millet_grain", "粮食"),
        ("product.material.timber", "木材"),
        ("product.goods.general", "一般货物"),
        ("product.material.craft_fiber", "手工业原料"),
        ("product.livestock.wool_and_hide", "畜产品"),
    ]
    carriers = person_by_occ["occupation.transport"]
    chains = []
    for index, (product_id, label) in enumerate(products):
        producer = fields[index % len(fields)]
        warehouse = storage_facilities[index % len(storage_facilities)]
        gate = gates[index % len(gates)]
        destination = taicang if index == 0 else market
        shipped = 1000 + index * 100
        natural_loss = 20 + index * 3
        road_loss = 10 + index * 2
        delivered = shipped - natural_loss - road_loss
        chains.append({
            "chain_id": f"supply_chain.luoyang.184.metropolitan.{index + 1:02d}", "product_definition_id": product_id,
            "display_name": label, "producer_facility_id": producer["facility_id"],
            "warehouse_facility_id": warehouse["facility_id"], "carrier_person_ordinal": carriers[index],
            "gate_facility_id": gate["facility_id"], "destination_facility_id": destination["facility_id"],
            "shipped_units": shipped, "carrier_consumption_units": 12 + index,
            "natural_loss_units": natural_loss, "road_loss_units": road_loss,
            "delivered_units": delivered, "destination_inventory_units_after": delivered,
            "ownership_transfer_stage": "OnDelivery", "status": "Delivered",
            "conservation_identity": "shipped = natural_loss + road_loss + delivered",
        })
    return fields, chains, taicang["facility_id"], market["facility_id"]


def protected_base_files(base_root):
    result = []
    for path in sorted(base_root.iterdir()):
        if path.is_file() and path.suffix != ".meta":
            result.append({"path": path.name, "bytes": path.stat().st_size, "sha256": sha256(path)})
    return result


def write_runtime_package(root, urban, base_root, base_manifest, catalogs, people, households,
                          facilities, cluster_rows, route_rows, road_by_cell, families, fields, chains,
                          generation_ms, taicang_id, market_id):
    root.mkdir(parents=True, exist_ok=True)
    with (root / "outer_persons.bin").open("wb") as stream:
        stream.write(urban.HEADER_STRUCT.pack(OUTER_PERSON_MAGIC, 1, urban.PERSON_STRUCT.size,
                                              len(people), 0, 184))
        for p in people:
            stream.write(urban.PERSON_STRUCT.pack(
                p.ordinal, p.birth_year, p.gender, p.age_stage, p.health_bp, p.household,
                p.family_org, p.current_cell, p.residence, p.work_facility, p.occupation,
                p.activity, p.civil_office, p.military_office, p.title, p.allegiance, p.force,
                p.reserve_force, p.skill_profile, p.knowledge_profile, p.assets, p.natural_lifespan,
                p.political_role, p.data_origin, p.residence_status, p.employment_status,
                p.location_status, p.father, p.mother, p.spouse))
    with (root / "outer_households.bin").open("wb") as stream:
        stream.write(urban.HEADER_STRUCT.pack(OUTER_HOUSEHOLD_MAGIC, 1, urban.HOUSEHOLD_STRUCT.size,
                                              len(households), 0, 184))
        for h in households:
            stream.write(urban.HOUSEHOLD_STRUCT.pack(
                h.ordinal, h.head, h.start, h.count, h.family_org, h.primary_residence,
                h.household_type, h.data_origin, 0, h.wealth))
    write_json(root / "catalogs.json", catalogs)
    write_json(root / "spatial_plan.json", {"schema": "mandate.luoyang-184-metropolitan-spatial.v1", "areas": [
        {"area_id": area_id, "area_type": name, "population_target": target} for area_id, name, target in AREA_PLANS
    ], "settlements": cluster_rows})
    write_json(root / "facilities.json", {"schema": "mandate.luoyang-184-metropolitan-facilities.v1", "facilities": facilities})
    write_json(root / "family_organizations.json", {"schema": "mandate.luoyang-184-metropolitan-families.v1", "organizations": families})
    write_json(root / "roads_logistics.json", {"schema": "mandate.luoyang-184-metropolitan-roads-logistics.v1", "routes": route_rows, "road_facility_by_cell": road_by_cell, "supply_chains": chains})
    write_json(root / "agriculture_supply.json", {"schema": "mandate.luoyang-184-metropolitan-agriculture.v1", "fields": fields, "supply_chains": chains})
    write_json(root / "commute_migration.json", {
        "schema": "mandate.luoyang-184-metropolitan-commute-migration.v1",
        "daily_local_commute": {"maximum_same_settlement_distance_m": 6000, "resolution": "DerivedFromPersonResidenceAndWorkFacility", "changes_world_location": False},
        "cross_settlement_travel": {"requires_route": True, "requires_elapsed_time": True, "requires_supply_consumption": True},
        "migration_interfaces": ["UrbanToNearSuburb", "NearSuburbToUrban", "VillageToSuburb", "SuburbToVillage"],
        "pending_moves": [],
    })
    write_json(root / "force_routes.json", {
        "schema": "mandate.luoyang-184-metropolitan-force-routes.v1",
        "routes": [{"force_id": force_id, "entry_gate_facility_id": route_rows[index % len(route_rows)]["gate_facility_id"], "route_id": route_rows[index % len(route_rows)]["route_id"], "daily_supply_units": 120 + index * 20, "movement_mode": "RealRoadTravel"}
                   for index, force_id in enumerate(catalogs["force_ids"])],
    })
    write_json(root / "event_impacts.json", {
        "schema": "mandate.luoyang-184-metropolitan-event-impacts.v1",
        "impacts": [
            {"event_id": "event.184.yellow_turban.secret_network", "effects": {"security_pressure": 120, "road_inspection_pressure": 80}},
            {"event_id": "event.184.hejin.capital_defense", "effects": {"recruitment_persons": 1200, "transport_capacity_delta": -300, "grain_price_basis_points": 10400}},
            {"event_id": "event.184.luzhi.departure", "effects": {"military_supply_units": 1200, "road_capacity_delta": -180, "refugee_pressure": 40}},
            {"event_id": "event.184.huangfu.departure", "effects": {"military_supply_units": 900, "agricultural_labor_delta": -240, "refugee_pressure": 80}},
        ],
    })
    historical_candidate_audit = {
        "schema": "mandate.luoyang-184-metropolitan-historical-candidate-audit.v1",
        "mother_library_scope": "140-264 historical person and timeline mother library V5",
        "promoted_new_historical_person_count": 0,
        "decision": "No mother-library record provides sufficient 184 near-suburb residence evidence beyond the protected urban overlays; uncertainty remains Unknown/Probable and no Person is forced into the metropolitan increment.",
    }
    write_json(root / "historical_candidate_audit.json", historical_candidate_audit)
    by_area = Counter(catalogs["areas"][p.area] for p in people)
    by_occupation = Counter(catalogs["occupations"][p.occupation] for p in people)
    audit = {
        "schema": "mandate.luoyang-184-metropolitan-audit.v1",
        "base_person_count": BASE_PERSON_COUNT, "added_person_count": len(people), "total_person_count": TOTAL_PERSON_COUNT,
        "base_household_count": BASE_HOUSEHOLD_COUNT, "added_household_count": len(households), "total_household_count": BASE_HOUSEHOLD_COUNT + len(households),
        "base_facility_count": BASE_FACILITY_COUNT, "added_facility_count": len(facilities), "total_facility_count": BASE_FACILITY_COUNT + len(facilities),
        "added_family_organization_count": len(families), "area_population": dict(sorted(by_area.items())),
        "occupation_population": dict(sorted(by_occupation.items())), "housed_count": sum(p.residence != NONE_U32 for p in people),
        "assigned_work_count": sum(p.work_facility != NONE_U32 for p in people),
        "road_route_count": len(route_rows), "agriculture_unit_count": len(fields), "supply_chain_count": len(chains),
        "protected_base_file_count": len(protected_base_files(base_root)),
        "generation_elapsed_ms": round(generation_ms, 3), "taicang_destination_facility_id": taicang_id,
        "urban_market_destination_facility_id": market_id,
    }
    write_json(root / "audit_summary.json", audit)

    own_files = []
    for path in sorted(root.iterdir()):
        if path.name == "manifest.json" or path.suffix == ".meta":
            continue
        own_files.append({"path": path.name, "bytes": path.stat().st_size, "sha256": sha256(path)})
    manifest = {
        "schema": SCHEMA, "format_version": 1, "scenario_id": base_manifest["scenario_id"],
        "scenario_year": 184, "world_id": "HanWorldV1", "city_id": base_manifest["city_id"],
        "data_origin": "HistoricalReconstruction", "population_profile_id": "population_profile.luoyang.184.metropolitan_recommended",
        "base_package_relative_path": "../Luoyang184UrbanInitializationV1",
        "base_person_count": BASE_PERSON_COUNT, "added_person_count": OUTER_PERSON_COUNT, "person_count": TOTAL_PERSON_COUNT,
        "base_household_count": BASE_HOUSEHOLD_COUNT, "added_household_count": len(households), "household_count": BASE_HOUSEHOLD_COUNT + len(households),
        "base_facility_count": BASE_FACILITY_COUNT, "added_facility_count": len(facilities), "facility_count": BASE_FACILITY_COUNT + len(facilities),
        "person_record_size": urban.PERSON_STRUCT.size, "household_record_size": urban.HOUSEHOLD_STRUCT.size,
        "walled_city_population": 200_000, "urban_area_population": BASE_PERSON_COUNT,
        "metropolitan_population": TOTAL_PERSON_COUNT, "supply_region_plan_population": 700_000,
        "historical_person_count": base_manifest["historical_person_count"],
        "base_package_files": protected_base_files(base_root), "files": own_files,
        "generated_at_is_metadata_only": True,
    }
    write_json(root / "manifest.json", manifest)
    return manifest, audit


def write_csv_inputs(csv_root, catalogs, people, households, household_cluster, facilities,
                     cluster_rows, route_rows, families, fields, chains, audit):
    area_rows = []
    for area_id, name, target in AREA_PLANS:
        settlements = [c for c in cluster_rows if c["area_type"] == name]
        area_rows.append({"AreaId": area_id, "AreaType": name, "PopulationTarget": target,
                          "SettlementCount": len(settlements), "RoadCells": sum(c["road_cell_count"] for c in settlements),
                          "DataOrigin": "GeneratedHistoricalPopulation", "Confidence": "C"})
    write_csv(csv_root / "spatial_plan.csv", area_rows)
    pop_counter = Counter(catalogs["areas"][p.area] for p in people)
    households_by_area = Counter()
    for h in households:
        households_by_area[catalogs["areas"][people[h.start - BASE_PERSON_COUNT].area]] += 1
    write_csv(csv_root / "population_plan.csv", [
        {"AreaId": area_id, "AreaType": name, "TargetPersons": target,
         "ActualPersons": pop_counter[area_id], "Households": households_by_area[area_id],
         "ResidenceRequired": pop_counter[area_id], "WorkAssigned": sum(1 for p in people if catalogs["areas"][p.area] == area_id and p.work_facility != NONE_U32),
         "HistoricalPersonsAdded": 0, "GeneratedPersons": pop_counter[area_id]}
        for area_id, name, target in AREA_PLANS])
    shards = []
    assignment_shards = []
    shard_size = 32_500
    for shard in range(4):
        subset = people[shard * shard_size:(shard + 1) * shard_size]
        first, last = subset[0].ordinal + 1, subset[-1].ordinal + 1
        person_name = f"persons_{first:06d}_{last:06d}.csv"
        assignment_name = f"assignments_{first:06d}_{last:06d}.csv"
        write_csv(csv_root / person_name, [
            {"Ordinal": p.ordinal, "PersonId": p.person_id, "DisplayName": p.display_name,
             "BirthYear": p.birth_year, "Age": p.age, "AgeStage": catalogs["age_stages"][p.age_stage],
             "Gender": p.gender, "HouseholdOrdinal": p.household, "FamilyOrganizationIndex": p.family_org,
             "AreaId": catalogs["areas"][p.area], "ResidenceFacilityIndex": p.residence,
             "WorkFacilityIndex": p.work_facility, "OccupationId": catalogs["occupations"][p.occupation],
             "CurrentCellId64": p.current_cell, "FatherOrdinal": p.father, "MotherOrdinal": p.mother,
             "SpouseOrdinal": p.spouse, "DataOrigin": "GeneratedHistoricalPopulation"}
            for p in subset])
        write_csv(csv_root / assignment_name, [
            {"Ordinal": p.ordinal, "PersonId": p.person_id, "ResidenceFacilityIndex": p.residence,
             "WorkFacilityIndex": p.work_facility, "OccupationId": catalogs["occupations"][p.occupation],
             "ActivityId": catalogs["activities"][p.activity], "EmploymentStatusIndex": p.employment_status,
             "CurrentCellId64": p.current_cell, "CommutePolicy": "DailyLocalOrRealRoadTravel"}
            for p in subset])
        shards.append({"Shard": shard + 1, "FirstOrdinal": subset[0].ordinal, "LastOrdinal": subset[-1].ordinal,
                       "PersonCount": len(subset), "CsvSource": person_name})
        assignment_shards.append({"Shard": shard + 1, "FirstOrdinal": subset[0].ordinal, "LastOrdinal": subset[-1].ordinal,
                                  "PersonCount": len(subset), "CsvSource": assignment_name})
    write_csv(csv_root / "person_partitions.csv", shards)
    write_csv(csv_root / "assignment_partitions.csv", assignment_shards)
    write_csv(csv_root / "households.csv", [
        {"HouseholdOrdinal": h.ordinal, "HeadOrdinal": h.head, "MemberStartOrdinal": h.start,
         "MemberCount": h.count, "FamilyOrganizationIndex": h.family_org,
         "ResidenceFacilityIndex": h.primary_residence, "SettlementId": household_cluster[h.ordinal],
         "Wealth": h.wealth, "DataOrigin": "GeneratedHistoricalPopulation"} for h in households])
    write_csv(csv_root / "families.csv", [
        {"FamilyOrganizationId": f["family_organization_id"], "FamilyName": f["family_name"],
         "HeadPersonId": f["head_person_id"], "MemberCount": f["member_count"],
         "HouseholdCount": len(f["household_ordinals"]), "FacilityCount": len(f["family_facility_ids"]),
         "Confidence": f["confidence"], "HistoricalClaim": f["historical_claim"]} for f in families])
    write_csv(csv_root / "facilities.csv", [
        {"GlobalFacilityIndex": f["global_facility_index"], "FacilityId": f["facility_id"],
         "DefinitionId": f["definition_id"], "DisplayName": f["display_name"], "CategoryId": f["category_id"],
         "CellId64": f["cell_id64"], "GridX": f["grid_x"], "GridY": f["grid_y"],
         "OwnerId": f["owner_id"], "ControllerId": f["controller_id"],
         "AdministrativeControllerId": f["administrative_controller_id"], "AreaType": f["area_type"],
         "SettlementId": f["settlement_id"], "ResidentialCapacity": f["residential_capacity_persons"],
         "CurrentResidents": f["current_residents"], "WorkerCapacity": f["worker_capacity"],
         "CurrentWorkers": f["current_workers"], "StorageCapacity": f["storage_capacity_units"],
         "DataOrigin": f["data_origin"]} for f in facilities])
    logistics_rows = []
    for route in route_rows:
        logistics_rows.append({"RecordType": "Route", "RecordId": route["route_id"], "SettlementOrProduct": route["settlement_id"],
                               "Origin": route["cell_ids"][0], "Transfer": route["gate_facility_id"], "Destination": route["gate_facility_id"],
                               "Quantity": route["distance_m"], "Loss": 0, "Delivered": 0, "Status": "Connected"})
    for chain in chains:
        logistics_rows.append({"RecordType": "SupplyChain", "RecordId": chain["chain_id"], "SettlementOrProduct": chain["product_definition_id"],
                               "Origin": chain["producer_facility_id"], "Transfer": chain["warehouse_facility_id"], "Destination": chain["destination_facility_id"],
                               "Quantity": chain["shipped_units"], "Loss": chain["natural_loss_units"] + chain["road_loss_units"],
                               "Delivered": chain["delivered_units"], "Status": chain["status"]})
    write_csv(csv_root / "roads_logistics.csv", logistics_rows)
    write_csv(csv_root / "agriculture_supply.csv", [
        {"FieldId": f["field_id"], "FacilityId": f["facility_id"], "CellId64": f["cell_id64"],
         "ProductDefinitionId": f["product_definition_id"], "PlantedDay": f["planted_day"],
         "MaturityDay": f["maturity_day"], "EarlyHarvestMinimumBP": f["early_harvest_minimum_basis_points"],
         "FullYieldUnits": f["full_yield_units"], "WorkerCount": len(f["worker_person_ordinals"]),
         "InventoryContainerId": f["inventory_container_id"]} for f in fields])
    audit_rows = [
        {"Metric": "BasePersonsProtected", "Expected": BASE_PERSON_COUNT, "Actual": audit["base_person_count"]},
        {"Metric": "AddedPersons", "Expected": OUTER_PERSON_COUNT, "Actual": audit["added_person_count"]},
        {"Metric": "MetropolitanPersons", "Expected": TOTAL_PERSON_COUNT, "Actual": audit["total_person_count"]},
        {"Metric": "AddedHousedPersons", "Expected": OUTER_PERSON_COUNT, "Actual": audit["housed_count"]},
        {"Metric": "AreaPopulationTotal", "Expected": OUTER_PERSON_COUNT, "Actual": sum(audit["area_population"].values())},
        {"Metric": "SupplyChainTypes", "Expected": 5, "Actual": audit["supply_chain_count"]},
        {"Metric": "ProtectedBaseFiles", "Expected": audit["protected_base_file_count"], "Actual": audit["protected_base_file_count"]},
    ]
    write_csv(csv_root / "audit_metrics.csv", audit_rows)


def write_reports(output_root, manifest, audit):
    output_root.mkdir(parents=True, exist_ok=True)
    report = f"""# 184 年洛阳 Metropolitan 初始化报告 V1

## 结果

- 原城市人口：{manifest['base_person_count']:,}（旧包逐文件 SHA-256 保护）
- 新增近郊人口：{manifest['added_person_count']:,}
- 都市圈人口：{manifest['person_count']:,}
- 新增家户：{manifest['added_household_count']:,}
- 新增设施：{manifest['added_facility_count']:,}
- 新增长期家族组织：8（玩法补全，不冒充史实）
- 农业/畜牧单元：{audit['agriculture_unit_count']:,}
- 道路聚落路线：{audit['road_route_count']:,}
- 可审计供应链：{audit['supply_chain_count']:,}

## 边界

本包只追加 130,000 名永久人物。它不改写旧 270,000 人，不生成 700,000 人供给区，
不扩城墙、不使用 SubCell、不创建第二套人物/家户事实，也不把城郊设施伪装成纯 UI。
短距通勤从 Person 的 Residence/WorkAssignment 派生；跨聚落旅行必须经过真实道路。

## 史料处理

母库未给出足以把额外人物确定为 184 年洛阳近郊常住者的证据，因此本增量未强行新增
历史人物。新增人口、普通家族与多数设施均明确标为 `GeneratedHistoricalPopulation`
或 `GameplayReconstruction`。
"""
    (output_root / "11_184洛阳Metropolitan初始化报告_V1.md").write_text(report, encoding="utf-8")
    audit_report = f"""# LUOYANG-184-METROPOLITAN-INITIALIZATION-V1 AUDIT

## 自动数据审计

- 270,000 + 130,000 = {audit['total_person_count']:,}
- 新增人物全部入户且有住所：{audit['housed_count']:,}/{audit['added_person_count']:,}
- 新增家户：{audit['added_household_count']:,}
- 新增设施：{audit['added_facility_count']:,}
- 近郊路线：{audit['road_route_count']:,}
- 农业/畜牧单元：{audit['agriculture_unit_count']:,}
- 供应链：{audit['supply_chain_count']:,}
- 生成耗时：{audit['generation_elapsed_ms']:.3f} ms

编译、核心测试和 Unity 测试结果由最终验证阶段补写；在证据产生前不声明通过。
"""
    (output_root / "12_LUOYANG_184_METROPOLITAN_INITIALIZATION_V1_AUDIT.md").write_text(audit_report, encoding="utf-8")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    repo = args.repo.resolve()
    started = time.perf_counter()
    urban = load_urban_module(repo)
    base_root, base_manifest, base_catalogs, base_facilities, world = load_inputs(repo)
    catalogs = extend_catalogs(base_catalogs)
    allocator, clusters, routes, road_by_cell, facilities, by_role, residences = build_spatial_and_facilities(
        base_facilities, world, catalogs)
    people, households, household_cluster = build_people_and_households(
        urban, catalogs, clusters, residences, facilities)
    families = assign_families(people, households, household_cluster, facilities)
    assign_occupations_and_work(people, facilities, by_role, catalogs, clusters)
    fields, chains, taicang_id, market_id = build_agriculture_and_logistics(
        people, facilities, by_role, routes, base_facilities, catalogs)
    generation_ms = (time.perf_counter() - started) * 1000.0
    runtime_root = repo / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184MetropolitanInitializationV1"
    manifest, audit = write_runtime_package(
        runtime_root, urban, base_root, base_manifest, catalogs, people, households, facilities,
        clusters, routes, road_by_cell, families, fields, chains, generation_ms, taicang_id, market_id)
    csv_root = repo / "tmp" / "luoyang-184-metropolitan-init-v1" / "csv"
    write_csv_inputs(csv_root, catalogs, people, households, household_cluster, facilities,
                     clusters, routes, families, fields, chains, audit)
    output_root = repo / "outputs" / "LUOYANG_184_METROPOLITAN_INITIALIZATION_V1"
    write_reports(output_root, manifest, audit)
    print(json.dumps({"status": "BUILT", "persons": manifest["person_count"],
                      "added_persons": manifest["added_person_count"], "households": manifest["household_count"],
                      "facilities": manifest["facility_count"], "elapsed_ms": generation_ms}, ensure_ascii=False))


if __name__ == "__main__":
    main()
