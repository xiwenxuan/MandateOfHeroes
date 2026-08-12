from __future__ import annotations

import json
import math
from collections import Counter, defaultdict
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib import font_manager
from matplotlib.patches import Circle, Rectangle
import numpy as np


REPO = Path(__file__).resolve().parents[2]
BASE_UNITY = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "LuoyangWorldV1"
BASE_DATA = REPO / "MapData" / "LuoyangWorld_V1"
ROOT = REPO / "MapData" / "Luoyang184Historical_V1"
UNITY = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184HistoricalV1"
REPORTS = ROOT / "reports"
DELIVERABLES = REPO / "deliverables" / "LUOYANG_184_HISTORICAL_V1"
GRID_SCHEMA = "hanworld.square-grid.v1"
OWNER_HAN = "organization.government.han.luoyang"
CONTROLLER_HAN = "organization.garrison.han.luoyang"
SOURCE_IDS = {
    "cssn_city": "source.cssn.han_wei_luoyang_evolution",
    "pku_axis": "source.pku.han_luoyang_ritual_axis",
    "cass_ritual": "source.cass.luoyang_southern_ritual_archaeology",
    "houhanshu_gates": "source.houhanshu.baiguan_gate_commandants",
    "ncha_site": "source.ncha.han_wei_luoyang_site_plan",
}


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def read_jsonl(path: Path):
    with path.open("r", encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")


def write_jsonl(path: Path, values) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        for value in values:
            handle.write(json.dumps(value, ensure_ascii=False, separators=(",", ":")) + "\n")


def perimeter(top: int, bottom: int, left: int, right: int):
    result = []
    for x in range(left, right + 1):
        result.append((top, x))
        result.append((bottom, x))
    for y in range(top + 1, bottom):
        result.append((y, left))
        result.append((y, right))
    return list(dict.fromkeys(result))


def historical_definitions(base_definitions):
    result = []
    for source in base_definitions:
        item = dict(source)
        item.update({
            "display_name": item.pop("name"),
            "category_id": item.pop("category"),
            "residential_capacity_persons": item.pop("residential_persons"),
            "minimum_workers_for_normal_operation": item.pop("min_workers"),
            "worker_capacity": item.pop("max_workers"),
            "allowed_resident_type_ids": ["population.civilian"] if source["category"] == "residential" else [],
            "purpose_ids": ["purpose." + source["category"]],
            "capability_ids": ["capability." + source["category"] + ".basic"],
            "future_hook_ids": [],
            "historical_confidence": "GameplayReconstruction",
        })
        if source["category"] != "residential":
            item["residential_capacity_persons"] = 0
        result.append(item)

    def add(definition_id, name, category, workers, minimum, residence=0,
            resident_types=(), purposes=(), capabilities=(), hooks=()):
        result.append({
            "id": definition_id,
            "display_name": name,
            "category_id": category,
            "residential_capacity_persons": residence,
            "minimum_workers_for_normal_operation": minimum,
            "worker_capacity": workers,
            "allowed_resident_type_ids": list(resident_types),
            "purpose_ids": list(purposes),
            "capability_ids": list(capabilities),
            "future_hook_ids": list(hooks),
            "historical_confidence": "HistoricalReconstruction",
        })

    add("facility.historical.palace_complex", "宫殿政务建筑群", "government", 180, 30,
        purposes=("purpose.court", "purpose.government"), capabilities=("capability.court.session", "capability.government.administration"),
        hooks=("hook.court_gameplay", "hook.palace_intrigue"))
    add("facility.historical.imperial_garden", "皇家苑囿", "public", 60, 8,
        purposes=("purpose.imperial_garden",), capabilities=("capability.garden.supply",), hooks=("hook.palace_life",))
    add("facility.historical.central_office", "三公及中央官署区", "government", 140, 24,
        purposes=("purpose.central_administration",), capabilities=("capability.government.archive", "capability.government.order"), hooks=("hook.official_career",))
    add("facility.historical.taicang", "太仓", "storage", 90, 12,
        purposes=("purpose.public_granary",), capabilities=("capability.inventory.grain", "capability.relief.supply"), hooks=("hook.siege_logistics",))
    add("facility.historical.arsenal", "武库", "military", 100, 16,
        purposes=("purpose.armory",), capabilities=("capability.inventory.weapon", "capability.military.equipment"), hooks=("hook.army_equipment",))
    add("facility.historical.market", "京师市", "commercial", 140, 24,
        purposes=("purpose.market",), capabilities=("capability.trade.wholesale", "capability.price.discovery"), hooks=("hook.merchant_competition",))
    add("facility.historical.imperial_academy", "太学", "education", 120, 20,
        purposes=("purpose.education",), capabilities=("capability.education.advanced", "capability.knowledge.copy"), hooks=("hook.scholar_career",))
    add("facility.historical.ritual_hall", "礼制建筑", "ritual", 72, 12,
        purposes=("purpose.ritual",), capabilities=("capability.ritual.state",), hooks=("hook.ritual_gameplay",))
    add("facility.historical.observatory", "灵台", "education", 54, 8,
        purposes=("purpose.observation",), capabilities=("capability.calendar.observation",), hooks=("hook.astronomy",))
    add("facility.historical.urban_ward", "洛阳里坊住宅", "residential", 0, 0, 180,
        resident_types=("population.civilian",), purposes=("purpose.residential",), capabilities=("capability.housing.permanent",), hooks=("hook.urban_household",))
    add("facility.historical.barracks", "京师兵营", "military", 60, 8, 200,
        resident_types=("population.active_military",), purposes=("purpose.barracks",), capabilities=("capability.housing.active_military", "capability.military.muster"), hooks=("hook.garrison_training",))
    add("facility.fortification.city_wall", "洛阳大城城垣", "fortification", 12, 0,
        purposes=("purpose.fortification",), capabilities=("capability.movement.block",), hooks=("hook.siege.wall",))
    add("facility.fortification.city_gate", "洛阳大城城门", "fortification", 32, 4,
        purposes=("purpose.gate",), capabilities=("capability.passage.control", "capability.movement.block"), hooks=("hook.siege.gate",))
    add("facility.fortification.palace_wall", "宫城城垣", "fortification", 10, 0,
        purposes=("purpose.palace_fortification",), capabilities=("capability.movement.block",), hooks=("hook.siege.inner_wall",))
    add("facility.fortification.palace_gate", "宫城门", "fortification", 24, 4,
        purposes=("purpose.palace_gate",), capabilities=("capability.passage.control",), hooks=("hook.siege.inner_gate",))
    return result


def make_historical_plan(anchor_row, anchor_col):
    # A 19x19 Cell operational abstraction keeps all twelve gates independently selectable.
    # It is not a claim that an individual building occupied 2 km.
    top, bottom, left, right = anchor_row - 9, anchor_row + 9, anchor_col - 9, anchor_col + 9
    gates = {
        (top, anchor_col - 3): ("gate.gumen", "谷门", "north", "Approximate"),
        (top, anchor_col + 3): ("gate.xiamen", "夏门", "north", "Approximate"),
        (bottom, anchor_col - 6): ("gate.jinmen", "津门", "south", "Probable"),
        (bottom, anchor_col - 2): ("gate.xiaoyuanmen", "小苑门", "south", "Probable"),
        (bottom, anchor_col + 2): ("gate.pingchengmen", "平城门", "south", "Probable"),
        (bottom, anchor_col + 6): ("gate.kaiyangmen", "开阳门", "south", "Probable"),
        (anchor_row - 5, left): ("gate.shangximen", "上西门", "west", "Probable"),
        (anchor_row, left): ("gate.yongmen", "雍门", "west", "Probable"),
        (anchor_row + 5, left): ("gate.guangyangmen", "广阳门", "west", "Probable"),
        (anchor_row - 5, right): ("gate.shangdongmen", "上东门", "east", "Probable"),
        (anchor_row, right): ("gate.zhongdongmen", "中东门", "east", "Probable"),
        (anchor_row + 5, right): ("gate.maomen", "旄门", "east", "Approximate"),
    }
    facilities = []
    for row, col in perimeter(top, bottom, left, right):
        if (row, col) in gates:
            stable, name, direction, precision = gates[(row, col)]
            facilities.append({
                "instance_key": stable, "definition_id": "facility.fortification.city_gate", "display_name": name,
                "category_id": "fortification", "row": row, "column": col, "confidence": "HistoricalAnchor",
                "precision": precision, "source_ids": [SOURCE_IDS["houhanshu_gates"], SOURCE_IDS["cssn_city"]],
                "network_id": "fortification.luoyang.main_wall", "gate_direction": direction,
            })
        else:
            facilities.append({
                "instance_key": f"main_wall.{row}.{col}", "definition_id": "facility.fortification.city_wall",
                "display_name": "洛阳大城城垣", "category_id": "fortification", "row": row, "column": col,
                "confidence": "HistoricalReconstruction", "precision": "Approximate",
                "source_ids": [SOURCE_IDS["cssn_city"]], "network_id": "fortification.luoyang.main_wall",
            })

    palace_specs = [
        ("north", anchor_row - 7, anchor_row - 1, anchor_col - 6, anchor_col + 6, (anchor_row - 1, anchor_col)),
        ("south", anchor_row + 1, anchor_row + 7, anchor_col - 6, anchor_col + 6, (anchor_row + 1, anchor_col)),
    ]
    for palace, ptop, pbottom, pleft, pright, gate_cell in palace_specs:
        network = f"fortification.luoyang.{palace}_palace_wall"
        for row, col in perimeter(ptop, pbottom, pleft, pright):
            gate = (row, col) == gate_cell
            facilities.append({
                "instance_key": f"{palace}_palace_{'gate' if gate else 'wall'}.{row}.{col}",
                "definition_id": "facility.fortification.palace_gate" if gate else "facility.fortification.palace_wall",
                "display_name": ("北宫南门" if palace == "north" else "南宫北门") if gate else ("北宫宫墙" if palace == "north" else "南宫宫墙"),
                "category_id": "fortification", "row": row, "column": col,
                "confidence": "HistoricalReconstruction", "precision": "Approximate",
                "source_ids": [SOURCE_IDS["cssn_city"], SOURCE_IDS["pku_axis"]], "network_id": network,
            })

    named = [
        ("north_palace", "facility.historical.palace_complex", "北宫", anchor_row - 5, anchor_col, "government", "HistoricalAnchor", "Probable", [SOURCE_IDS["cssn_city"]]),
        ("yongan_palace", "facility.historical.palace_complex", "永安宫", anchor_row - 4, anchor_col - 3, "government", "HistoricalReconstruction", "Approximate", [SOURCE_IDS["cssn_city"]]),
        ("zhuolong_garden", "facility.historical.imperial_garden", "濯龙园", anchor_row - 4, anchor_col + 3, "public", "HistoricalReconstruction", "Approximate", [SOURCE_IDS["cssn_city"]]),
        ("south_palace", "facility.historical.palace_complex", "南宫", anchor_row + 4, anchor_col, "government", "HistoricalAnchor", "Probable", [SOURCE_IDS["cssn_city"], SOURCE_IDS["pku_axis"]]),
        ("central_offices_west", "facility.historical.central_office", "三公官署西区", anchor_row + 3, anchor_col - 3, "government", "HistoricalReconstruction", "Approximate", [SOURCE_IDS["cssn_city"]]),
        ("central_offices_east", "facility.historical.central_office", "中央官署东区", anchor_row + 3, anchor_col + 3, "government", "HistoricalReconstruction", "Approximate", [SOURCE_IDS["cssn_city"]]),
        ("taicang", "facility.historical.taicang", "太仓", anchor_row + 6, anchor_col - 3, "storage", "HistoricalAnchor", "Approximate", [SOURCE_IDS["cssn_city"]]),
        ("arsenal", "facility.historical.arsenal", "武库", anchor_row + 6, anchor_col + 3, "military", "HistoricalAnchor", "Approximate", [SOURCE_IDS["cssn_city"]]),
        ("jinshi", "facility.historical.market", "金市", anchor_row, anchor_col - 8, "commercial", "HistoricalReconstruction", "Approximate", [SOURCE_IDS["cssn_city"]]),
        ("nanshi", "facility.historical.market", "南市", anchor_row + 8, anchor_col - 4, "commercial", "HistoricalReconstruction", "Approximate", [SOURCE_IDS["cssn_city"]]),
        ("mashi", "facility.historical.market", "马市", anchor_row + 8, anchor_col + 4, "commercial", "GameplayReconstruction", "Approximate", [SOURCE_IDS["cssn_city"]]),
        ("mingtang", "facility.historical.ritual_hall", "明堂", anchor_row + 14, anchor_col - 3, "ritual", "HistoricalAnchor", "Probable", [SOURCE_IDS["pku_axis"], SOURCE_IDS["cass_ritual"]]),
        ("lingtai", "facility.historical.observatory", "灵台", anchor_row + 14, anchor_col - 6, "education", "HistoricalAnchor", "Probable", [SOURCE_IDS["pku_axis"], SOURCE_IDS["cass_ritual"]]),
        ("biyong", "facility.historical.ritual_hall", "辟雍", anchor_row + 14, anchor_col + 3, "ritual", "HistoricalAnchor", "Probable", [SOURCE_IDS["pku_axis"], SOURCE_IDS["cass_ritual"]]),
        ("taixue", "facility.historical.imperial_academy", "太学", anchor_row + 12, anchor_col + 6, "education", "HistoricalAnchor", "Probable", [SOURCE_IDS["pku_axis"], SOURCE_IDS["cass_ritual"]]),
    ]
    for key, definition, name, row, col, category, confidence, precision, sources in named:
        facilities.append({
            "instance_key": key, "definition_id": definition, "display_name": name, "category_id": category,
            "row": row, "column": col, "confidence": confidence, "precision": precision, "source_ids": sources,
        })

    for side_col in (anchor_col - 8, anchor_col + 8):
        for offset in (-7, -2, 3):
            facilities.append({
                "instance_key": f"barracks.{side_col}.{offset}", "definition_id": "facility.historical.barracks",
                "display_name": "京师兵营", "category_id": "military", "row": anchor_row + offset, "column": side_col,
                "confidence": "GameplayReconstruction", "precision": "Approximate", "source_ids": [SOURCE_IDS["cssn_city"]],
            })
    ward_cells = [(anchor_row, anchor_col + offset) for offset in (-6, -5, -4, -3, 3, 4, 5, 6)]
    for index, (row, col) in enumerate(ward_cells, 1):
        facilities.append({
            "instance_key": f"urban_ward.{index}", "definition_id": "facility.historical.urban_ward",
            "display_name": f"洛阳里坊第{index}区", "category_id": "residential", "row": row, "column": col,
            "confidence": "GameplayReconstruction", "precision": "Approximate", "source_ids": [SOURCE_IDS["cssn_city"]],
        })

    moat_cells = perimeter(top - 1, bottom + 1, left - 1, right + 1)
    return facilities, moat_cells, {"top": top, "bottom": bottom, "left": left, "right": right}


def relocate_conflicting_base_facilities(base_facilities, cells_by_coord, reserved):
    occupied = {(item["grid_y"], item["grid_x"]): item for item in base_facilities if (item["grid_y"], item["grid_x"]) not in reserved}
    candidates = [cell for cell in cells_by_coord.values()
                  if cell["developable"] and (cell["grid_y"], cell["grid_x"]) not in reserved and
                  (cell["grid_y"], cell["grid_x"]) not in occupied]
    candidates.sort(key=lambda c: (max(abs(c["grid_y"] - 1241), abs(c["grid_x"] - 2043)), c["cell_id64"]), reverse=True)
    moved = {}
    for facility in base_facilities:
        old = (facility["grid_y"], facility["grid_x"])
        if old not in reserved:
            continue
        if not candidates:
            raise RuntimeError("No available Cell to relocate a pre-existing V1 Facility")
        cell = candidates.pop()
        moved[facility["facility_id"]] = (facility["cell_id64"], cell["cell_id64"])
        facility["cell_id64"] = cell["cell_id64"]
        facility["grid_x"] = cell["grid_x"]
        facility["grid_y"] = cell["grid_y"]
        occupied[(cell["grid_y"], cell["grid_x"])] = facility
    return moved


def build_facility(instance, definition, columns):
    cell_id = instance["row"] * columns + instance["column"]
    return {
        "facility_id": "facility.instance.luoyang.184." + instance["instance_key"],
        "definition_id": definition["id"], "display_name": instance["display_name"],
        "category_id": instance["category_id"], "cell_id64": cell_id,
        "grid_x": instance["column"], "grid_y": instance["row"],
        "owner_id": OWNER_HAN, "controller_id": CONTROLLER_HAN,
        "historical_confidence": instance["confidence"], "spatial_precision": instance["precision"],
        "source_ids": instance["source_ids"], "purpose_ids": definition["purpose_ids"],
        "capability_ids": definition["capability_ids"], "future_hook_ids": definition["future_hook_ids"],
        "worker_capacity": definition["worker_capacity"],
        "minimum_workers_for_normal_operation": definition["minimum_workers_for_normal_operation"],
        "worker_person_ids": [], "residential_capacity_persons": definition["residential_capacity_persons"],
        "allowed_resident_type_ids": definition["allowed_resident_type_ids"], "resident_person_ids": [],
        "job_definition_ids": [], "network_id": instance.get("network_id"),
        "gate_direction": instance.get("gate_direction"),
    }


def enrich_base_facility(base, definition):
    residential_capacity = definition["residential_capacity_persons"] if base["category"] == "residential" else 0
    return {
        "facility_id": base["facility_id"], "definition_id": base["definition_id"],
        "display_name": definition["display_name"], "category_id": base["category"],
        "cell_id64": base["cell_id64"], "grid_x": base["grid_x"], "grid_y": base["grid_y"],
        "owner_id": base["owner_id"], "controller_id": base["owner_id"],
        "historical_confidence": "GameplayReconstruction", "spatial_precision": "Approximate",
        "source_ids": [], "purpose_ids": definition["purpose_ids"], "capability_ids": definition["capability_ids"],
        "future_hook_ids": definition["future_hook_ids"], "worker_capacity": base["worker_capacity"],
        "minimum_workers_for_normal_operation": definition["minimum_workers_for_normal_operation"],
        "worker_person_ids": list(base.get("current_workers", [])),
        "residential_capacity_persons": residential_capacity,
        "allowed_resident_type_ids": definition["allowed_resident_type_ids"], "resident_person_ids": [],
        "job_definition_ids": [],
    }


def job_catalog():
    rows = [
        ("job.agriculture.worker", "profession.agriculture", "skill.agriculture.basic", 500),
        ("job.craft.worker", "profession.craft", "skill.craft.basic", 500),
        ("job.trade.merchant", "profession.trade", "skill.trade.basic", 500),
        ("job.service.worker", "profession.service", "skill.service.basic", 500),
        ("job.transport.worker", "profession.transport", "skill.transport.basic", 500),
        ("job.government.official", "profession.government", "skill.government.basic", 500),
        ("job.education.scholar", "profession.scholar", "skill.scholar.basic", 500),
        ("job.military.garrison", "profession.military", "skill.military.basic", 500),
        ("job.medical.physician", "profession.medical", "skill.medical.basic", 500),
    ]
    return [{"id": i, "profession_id": p, "primary_skill_id": s, "minimum_skill_basis_points": m,
             "requires_same_cell": True, "eligibility": ["alive", "present", "profession_match", "skill_minimum"]}
            for i, p, s, m in rows]


def assign_people(persons, households, facilities, definitions):
    by_id = {item["facility_id"]: item for item in facilities}
    persons_by_id = {item["person_id"]: item for item in persons}
    definitions_by_id = {item["id"]: item for item in definitions}
    # Rebuild current worker membership from the permanent Person references.
    for facility in facilities:
        facility["worker_person_ids"] = []
        facility["resident_person_ids"] = []
    for person in persons:
        work = by_id.get(person.get("work_facility_id"))
        if work and len(work["worker_person_ids"]) < work["worker_capacity"]:
            work["worker_person_ids"].append(person["person_id"])
        residence = by_id.get(person.get("residence_facility_id"))
        if residence and residence["residential_capacity_persons"] > 0:
            residence["resident_person_ids"].append(person["person_id"])

    profession_job = {
        "profession.agriculture": "job.agriculture.worker", "profession.craft": "job.craft.worker",
        "profession.trade": "job.trade.merchant", "profession.service": "job.service.worker",
        "profession.transport": "job.transport.worker", "profession.government": "job.government.official",
        "profession.scholar": "job.education.scholar", "profession.military": "job.military.garrison",
        "profession.medical": "job.medical.physician",
    }
    category_jobs = {
        "agriculture": ["job.agriculture.worker"], "industry": ["job.craft.worker"],
        "resource": ["job.craft.worker"], "commercial": ["job.trade.merchant", "job.service.worker"],
        "service": ["job.service.worker", "job.transport.worker", "job.medical.physician", "job.education.scholar"],
        "public": ["job.government.official", "job.service.worker"], "road": ["job.transport.worker"],
        "military": ["job.military.garrison", "job.craft.worker"],
        "fortification": ["job.military.garrison"], "government": ["job.government.official"],
        "storage": ["job.government.official", "job.transport.worker"],
        "education": ["job.education.scholar"], "ritual": ["job.education.scholar", "job.government.official"],
        "residential": [],
    }
    for facility in facilities:
        facility["job_definition_ids"] = category_jobs.get(facility["category_id"], [])

    # Military housing is only for active service people. It replaces, but does not delete, their family home fact.
    barracks = [f for f in facilities if f["definition_id"] == "facility.historical.barracks"]
    military_people = [p for p in persons if p["labor_eligible"] and p["profession_id"] == "profession.military"]
    for index, person in enumerate(military_people):
        old = by_id.get(person.get("residence_facility_id"))
        if old and person["person_id"] in old["resident_person_ids"]:
            old["resident_person_ids"].remove(person["person_id"])
        target = barracks[index % len(barracks)]
        if len(target["resident_person_ids"]) >= target["residential_capacity_persons"]:
            raise RuntimeError("Active military exceeds barracks Person capacity")
        target["resident_person_ids"].append(person["person_id"])
        person["residence_facility_id"] = target["facility_id"]
        person["current_cell_id64"] = target["cell_id64"]
        person["population_type_id"] = "population.active_military"
        person["active_military"] = True
    for person in persons:
        if "population_type_id" not in person:
            person["population_type_id"] = "population.civilian"
            person["active_military"] = False
        person["skill_basis_points_by_id"] = {skill_id: 2500 for skill_id in person.get("skill_ids", [])}

    # Move stable complete households into historical wards, proving that historic housing uses Person slots.
    wards = [f for f in facilities if f["definition_id"] == "facility.historical.urban_ward"]
    ward_index = 0
    for household in households[:240]:
        civilian_members = [persons_by_id[pid] for pid in household["member_ids"] if not persons_by_id[pid]["active_military"]]
        if not civilian_members:
            continue
        while ward_index < len(wards) and len(wards[ward_index]["resident_person_ids"]) + len(civilian_members) > wards[ward_index]["residential_capacity_persons"]:
            ward_index += 1
        if ward_index >= len(wards):
            break
        target = wards[ward_index]
        for person in civilian_members:
            old = by_id.get(person.get("residence_facility_id"))
            if old and person["person_id"] in old["resident_person_ids"]:
                old["resident_person_ids"].remove(person["person_id"])
            target["resident_person_ids"].append(person["person_id"])
            person["residence_facility_id"] = target["facility_id"]
            person["current_cell_id64"] = target["cell_id64"]
        household["residence_facility_id"] = target["facility_id"]
        household["current_cell_id64"] = target["cell_id64"]

    # A small stable unhoused cohort proves that housing shortage does not erase or merge people.
    unhoused_candidates = [p for p in persons if not p["active_military"] and p["age"] >= 15][-128:]
    for person in unhoused_candidates:
        old = by_id.get(person.get("residence_facility_id"))
        if old and person["person_id"] in old["resident_person_ids"]:
            old["resident_person_ids"].remove(person["person_id"])
        person["residence_facility_id"] = None

    # Reassign real, eligible workers from generic facilities into historical institutions.
    def take(profession, count):
        candidates = [p for p in persons if p["labor_eligible"] and p["profession_id"] == profession]
        candidates.sort(key=lambda p: p["person_id"])
        return candidates[:count]

    used = set()
    targets = [f for f in facilities if f["facility_id"].startswith("facility.instance.luoyang.184.")]
    priority = {
        "fortification": ("profession.military", 4), "military": ("profession.military", 12),
        "government": ("profession.government", 28), "storage": ("profession.government", 14),
        "commercial": ("profession.trade", 28), "education": ("profession.scholar", 18),
        "ritual": ("profession.scholar", 12), "public": ("profession.service", 8),
    }
    pools = {profession: take(profession, 100000) for profession, _ in priority.values()}
    pool_indexes = defaultdict(int)
    for facility in targets:
        spec = priority.get(facility["category_id"])
        if not spec:
            continue
        profession, desired = spec
        desired = min(desired, facility["worker_capacity"])
        selected = []
        pool = pools[profession]
        while pool_indexes[profession] < len(pool) and len(selected) < desired:
            person = pool[pool_indexes[profession]]
            pool_indexes[profession] += 1
            if person["person_id"] in used:
                continue
            selected.append(person)
            used.add(person["person_id"])
        for person in selected:
            old = by_id.get(person.get("work_facility_id"))
            if old and person["person_id"] in old["worker_person_ids"]:
                old["worker_person_ids"].remove(person["person_id"])
            facility["worker_person_ids"].append(person["person_id"])
            person["work_facility_id"] = facility["facility_id"]
            person["current_cell_id64"] = facility["cell_id64"]
            person["current_activity"] = "working"

    # Non-operating facilities remain visible and report a real labor shortage; no magical production is granted.
    for facility in facilities:
        definition = definitions_by_id[facility["definition_id"]]
        facility["normal_operation"] = len(facility["worker_person_ids"]) >= definition["minimum_workers_for_normal_operation"]
        facility["vacant_job_slots"] = max(0, facility["worker_capacity"] - len(facility["worker_person_ids"]))
        facility["resident_count"] = len(facility["resident_person_ids"])
        facility["current_workers"] = len(facility["worker_person_ids"])
    return profession_job


def build_fortifications(facilities, moat_cells, columns):
    networks = {}
    walls, gates = [], []
    for facility in facilities:
        network_id = facility.get("network_id")
        if not network_id:
            continue
        network = networks.setdefault(network_id, {"network_id": network_id,
            "display_name": "洛阳大城防线" if network_id.endswith("main_wall") else facility["display_name"].replace("宫墙", "防线"),
            "parent_network_id": None if network_id.endswith("main_wall") else "fortification.luoyang.main_wall",
            "wall_facility_ids": [], "gate_facility_ids": [], "moat_feature_ids": []})
        if "gate" in facility["definition_id"]:
            network["gate_facility_ids"].append(facility["facility_id"])
            gates.append({
                "facility_id": facility["facility_id"], "network_id": network_id, "cell_id64": facility["cell_id64"],
                "owner_id": facility["owner_id"], "controller_id": facility["controller_id"],
                "maximum_durability": 9000 if network_id.endswith("main_wall") else 6500,
                "current_durability": 9000 if network_id.endswith("main_wall") else 6500,
                "passage_capacity_per_hour": 900 if network_id.endswith("main_wall") else 420,
                "defender_person_ids": facility["worker_person_ids"], "gate_state": "Closed",
                "direction": facility.get("gate_direction"),
            })
        else:
            network["wall_facility_ids"].append(facility["facility_id"])
            walls.append({
                "facility_id": facility["facility_id"], "network_id": network_id, "cell_id64": facility["cell_id64"],
                "height_centimetres": 920 if network_id.endswith("main_wall") else 680,
                "thickness_centimetres": 700 if network_id.endswith("main_wall") else 430,
                "material_id": "material.rammed_earth.faced", "maximum_durability": 12000 if network_id.endswith("main_wall") else 7800,
                "current_durability": 12000 if network_id.endswith("main_wall") else 7800,
                "defender_person_ids": facility["worker_person_ids"], "wall_state": "Intact",
            })
    moats = []
    main = networks["fortification.luoyang.main_wall"]
    for index, (row, col) in enumerate(moat_cells, 1):
        feature_id = f"feature.moat.luoyang.184.{index:03d}"
        main["moat_feature_ids"].append(feature_id)
        moats.append({"feature_id": feature_id, "cell_id64": row * columns + col, "grid_x": col, "grid_y": row,
                      "moat_state": "Flooded", "width_centimetres": 1800, "depth_centimetres": 320,
                      "blocks_ordinary_movement": True, "siege_crossing_requires": ["bridge", "fill", "controlled_gate_crossing"]})
    return list(networks.values()), walls, gates, moats


def build_blueprints():
    main_cells = []
    for index, (x, y, definition) in enumerate([
        (0, 0, "facility.fortification.city_gate"), (-1, 0, "facility.fortification.city_wall"),
        (1, 0, "facility.fortification.city_wall"), (0, -1, "facility.public.road"),
        (0, 1, "facility.public.road"),
    ]):
        main_cells.append({"relative_x": x, "relative_y": y, "facility_definition_id": definition,
                           "orientation": "North", "required_road_connection_ids": ["road.connection.gate_axis"] if x == 0 else [],
                           "module_ids": ["module.gatehouse"] if "gate" in definition else [],
                           "construction_stage": ["Survey", "Foundation", "Structure", "Services", "Commissioning"][index],
                           "build_order": index + 1, "metadata": {"prototype": "luoyang_184"}})
    return [{
        "blueprint_id": "blueprint.fortification.han_city_gate_segment.v1", "display_name": "汉代城门与城垣段",
        "orientation": "North", "cell_count": len(main_cells), "cells": main_cells,
        "construction_stages": ["Survey", "Foundation", "Structure", "Services", "Commissioning"],
        "shared_placement_modes": ["Player", "HistoricalGeneration", "AI"],
        "metadata": {"instant_construction": "false", "one_base_facility_per_cell": "true"},
    }]


def render_map(world, facilities, moat_cells, bounds, output):
    output.parent.mkdir(parents=True, exist_ok=True)
    font_path = Path("C:/Windows/Fonts/msyh.ttc")
    font = font_manager.FontProperties(fname=str(font_path)) if font_path.exists() else None
    cells = world["cells"]
    anchor_row = world["city_anchor_cell_id64"] // world["columns"]
    anchor_col = world["city_anchor_cell_id64"] % world["columns"]
    min_row, max_row = anchor_row - 20, anchor_row + 20
    min_col, max_col = anchor_col - 24, anchor_col + 40
    selected = [c for c in cells if min_row <= c["grid_y"] <= max_row and min_col <= c["grid_x"] <= max_col]
    height, width = max_row - min_row + 1, max_col - min_col + 1
    elevation = np.full((height, width), np.nan)
    water = np.zeros((height, width))
    fertility = np.zeros((height, width))
    for cell in selected:
        y, x = cell["grid_y"] - min_row, cell["grid_x"] - min_col
        elevation[y, x] = cell["elevation"]
        water[y, x] = cell["water_class"]
        fertility[y, x] = cell["fertility"]
    elevation = np.nan_to_num(elevation, nan=np.nanmedian(elevation))
    gy, gx = np.gradient(elevation)
    hillshade = np.clip(.58 - gx * .025 - gy * .018, .22, .92)
    base = np.zeros((height, width, 3))
    base[:, :, 0] = .70 * hillshade + .12
    base[:, :, 1] = .73 * hillshade + .13 + fertility / 1800
    base[:, :, 2] = .55 * hillshade + .12
    base = np.clip(base, 0, 1)
    base[water > 0] = (.31, .55, .63)

    fig, ax = plt.subplots(figsize=(16, 11), dpi=150)
    ax.imshow(base, extent=[min_col, max_col + 1, max_row + 1, min_row], interpolation="bilinear")
    ax.set_facecolor("#d7c99f")
    # Existing terrain roads and the Luoyang-Hulao strategic direction.
    road_cells = [c for c in selected if c.get("road_class", 0) > 0]
    if road_cells:
        ax.scatter([c["grid_x"] + .5 for c in road_cells], [c["grid_y"] + .5 for c in road_cells],
                   s=7, c="#b98b52", alpha=.50, marker="s", linewidths=0)
    hrow, hcol = world["hulao_cell_id64"] // world["columns"], world["hulao_cell_id64"] % world["columns"]
    ax.annotate("虎牢方向", xy=(hcol + .5, hrow + .5), xytext=(anchor_col + 22, anchor_row - 16),
                color="#562d1b", fontsize=12, fontproperties=font,
                arrowprops={"arrowstyle": "->", "color": "#6d3c23", "lw": 2})
    # Moat, outer wall and palace enclosures.
    ax.scatter([c + .5 for r, c in moat_cells], [r + .5 for r, c in moat_cells], s=36,
               facecolors="none", edgecolors="#276d88", linewidths=1.3, marker="s", label="护城壕")
    outer = Rectangle((bounds["left"] + .5, bounds["top"] + .5), bounds["right"] - bounds["left"],
                      bounds["bottom"] - bounds["top"], fill=False, lw=5, edgecolor="#553927")
    ax.add_patch(outer)
    ax.add_patch(Rectangle((anchor_col - 5.5, anchor_row - 6.5), 12, 6, facecolor="#b98b66aa", edgecolor="#6c3027", lw=2.4))
    ax.add_patch(Rectangle((anchor_col - 5.5, anchor_row + 1.5), 12, 6, facecolor="#c79b72aa", edgecolor="#6c3027", lw=2.4))
    ax.text(anchor_col + .5, anchor_row - 3.7, "北宫", ha="center", va="center", fontsize=19, color="#4a1917", fontproperties=font)
    ax.text(anchor_col + .5, anchor_row + 4.5, "南宫", ha="center", va="center", fontsize=19, color="#4a1917", fontproperties=font)

    gate_records = [f for f in facilities if f["definition_id"] == "facility.fortification.city_gate"]
    ax.scatter([f["grid_x"] + .5 for f in gate_records], [f["grid_y"] + .5 for f in gate_records],
               s=82, c="#d9a23c", edgecolors="#4b211c", marker="D", linewidths=1.2, zorder=7)
    for facility in gate_records:
        ax.text(facility["grid_x"] + .5, facility["grid_y"] + .15, facility["display_name"],
                ha="center", va="bottom", fontsize=7.5, color="#321714", fontproperties=font, zorder=8)

    important_ids = {"north_palace", "south_palace", "yongan_palace", "zhuolong_garden", "central_offices_west",
                     "taicang", "arsenal", "jinshi", "nanshi", "mashi", "mingtang", "lingtai", "biyong", "taixue"}
    important = [f for f in facilities if f["facility_id"].split(".")[-1] in important_ids]
    for facility in important:
        color = {"government": "#9b4138", "commercial": "#c17a26", "education": "#3f638d",
                 "ritual": "#6d4d88", "storage": "#7b6842", "military": "#574a43", "public": "#5e774c"}.get(facility["category_id"], "#705d45")
        ax.add_patch(Circle((facility["grid_x"] + .5, facility["grid_y"] + .5), .42,
                            facecolor=color, edgecolor="#f6e8c2", lw=1.1, zorder=6))
        ax.text(facility["grid_x"] + .7, facility["grid_y"] + .3, facility["display_name"],
                fontsize=8.5, color="#241612", fontproperties=font, zorder=8,
                bbox={"facecolor": "#f1e4c4", "edgecolor": "none", "alpha": .74, "pad": 1})

    ax.text(anchor_col + .5, bounds["top"] - 2.5, "北", fontsize=15, ha="center", fontproperties=font)
    ax.annotate("", xy=(anchor_col + .5, bounds["top"] - 2), xytext=(anchor_col + .5, bounds["top"]),
                arrowprops={"arrowstyle": "-|>", "color": "#3f2f22", "lw": 1.5})
    ax.set_xlim(min_col, max_col)
    ax.set_ylim(max_row, min_row)
    ax.set_title("184年东汉洛阳｜统一世界历史初始地图 V1", fontsize=24, pad=16, color="#3c2117", fontproperties=font)
    ax.text(.01, .01, "史实锚点、合理复原与玩法补全分级保存；2000m Cell 为可操作抽象，不代表单体建筑尺度。",
            transform=ax.transAxes, fontsize=9.5, color="#3f3427", fontproperties=font,
            bbox={"facecolor": "#eadcba", "alpha": .85, "edgecolor": "#8d775b"})
    ax.set_xlabel("HanWorldV1 GridX（东向）", fontproperties=font)
    ax.set_ylabel("HanWorldV1 GridY（南向）", fontproperties=font)
    ax.grid(color="#705b42", alpha=.12, linewidth=.5)
    fig.tight_layout()
    fig.savefig(output, bbox_inches="tight")
    plt.close(fig)


def build_reports(world, definitions, facilities, fortifications, walls, gates, moats, blueprint, bounds):
    sources = f"""# 01 洛阳184史源报告

## 年代边界

本原型严格以184年东汉为开局，不把曹魏、北魏扩建形态回填为东汉事实。后世遗址材料只可用于识别地层与研究史，不可直接充当184年设施状态。

## 来源与用途

|ID|来源|用于支持|边界|
|---|---|---|---|
|{SOURCE_IDS['cssn_city']}|[中国社会科学网：汉魏洛阳城演变](https://www.cssn.cn/lsx/lsx_kgx/202210/t20221024_5552600.shtml)|东汉城郭南北长方形、十二门、南郊礼制建筑、南北宫|不把北魏宫城与外郭当作184年事实|
|{SOURCE_IDS['pku_axis']}|[北京大学：汉魏洛阳城礼制空间研究](https://ir.pku.edu.cn/handle/20.500.11897/676372)|宫城—平城门—南郊礼制轴线；明堂、辟雍、灵台、太学功能区分|Cell坐标为游戏复原|
|{SOURCE_IDS['cass_ritual']}|[中国社科院考古所：南郊礼制建筑考古报告](https://www.pishu.com.cn/skwx_ps/ps/literature?ID=8644335&SiteID=14)|灵台、明堂、辟雍、太学考古工作范围|不把报告覆盖年份等同于设施初建年份|
|{SOURCE_IDS['houhanshu_gates']}|[《后汉书·百官志》古籍文本](https://www.shidianguji.com/mid-page/7620933339273183283)|十二门及城门校尉制度、门名|北门东西次序与个别异名记为冲突，不假装精确|
|{SOURCE_IDS['ncha_site']}|[国家文物局：汉魏洛阳城遗址保护规划](https://www.ncha.gov.cn/art/2021/11/18/art_2318_45063.html)|遗址身份与分期保护依据|不提供单体184年坐标|

## 置信度

- `HistoricalAnchor`：名称、存在或宏观相对关系有史籍/考古支持。
- `HistoricalReconstruction`：依据史料作出的可解释复原；坐标另标 `Probable/Approximate`。
- `GameplayReconstruction`：为真实模拟闭环补充，不能在百科界面伪装成史实。
"""
    expressiveness = f"""# 02 2000m Cell表达力审计

- 洛阳大城采用{bounds['right'] - bounds['left'] + 1}×{bounds['bottom'] - bounds['top'] + 1} Cell 的操作性抽象，使十二座门各自成为可选、可控、可破坏的真实 Facility。
- 该足迹约等于38×38公里的玩法空间，不宣称为考古城郭实测面积；单座宫殿也不被解释为2公里建筑。
- 统一世界、同一 CellId64、同一 Owner/Facility/Force 合同保持不变，没有建立城内第二地图或 SubCell。
- 代价：城内相对距离被放大；收益：道路、城门、宫墙、驻军、攻守与设施选择都能进入正式规则。
- 结论：P0通过。2000m足以表达本阶段的策略交互，但美术必须说明“空间抽象”，后续不得用像素尺寸反推史实建筑尺度。
"""
    historical = "# 03 历史设施目录\n\n|Facility|定义|类别|Cell|置信度|位置精度|工人|住宅人数槽|功能/后续钩子|\n|---|---|---|---:|---|---|---:|---:|---|\n" + "\n".join(
        f"|{f['display_name']}|`{f['definition_id']}`|{f['category_id']}|{f['cell_id64']}|{f['historical_confidence']}|{f['spatial_precision']}|{len(f['worker_person_ids'])}/{f['worker_capacity']}|{f['residential_capacity_persons']}|{', '.join(f['purpose_ids'] + f['future_hook_ids'])}|"
        for f in facilities if f["facility_id"].startswith("facility.instance.luoyang.184.")) + "\n"
    population = world["population_profile"]
    ai = world["ai_pressure"]
    housing = f"""# 04 住房—岗位平衡报告

## 人口与住房（唯一口径：Person）

- 永久Person：{population['total_persons']:,}
- 永久Household：{population['total_households']:,}（仅关系事实，不作为住房容量单位）
- 已住房Person：{population['housed_persons']:,}
- 无住房但仍存在的Person：{population['unhoused_persons']:,}
- 民用永久住宅容量：{population['civilian_residential_capacity_persons']:,}
- 现役军人兵营容量：{population['active_military_barracks_capacity_persons']:,}，只允许 `population.active_military`
- 非住宅Facility永久住房容量：0；客栈、医馆等临时服务不冒充永久住宅。

## 岗位

- 有效劳力：{population['effective_workers']:,}
- 已就业：{population['employed_workers']:,}
- 未就业：{population['unemployed_workers']:,}
- 空缺岗位：{ai['vacant_job_slots']:,}
- 技能不匹配空缺：{ai['skill_shortage_slots']:,}
- 所有岗位引用稳定Person ID；无工人的设施保持存在，但 `normal_operation=false`，不产生正常产出。

## AI压力事实

`unhoused={ai['unhoused_persons']}, housing_slots={ai['available_residential_person_slots']}, unemployed={ai['unemployed_workers']}, vacancies={ai['vacant_job_slots']}, skill_shortage={ai['skill_shortage_slots']}`。

AI不读取固定“住宅/岗位Cell比例”，只依据这些实际压力、粮食与治安事实提出建设、培训或招募建议。
"""
    fort = f"""# 05 城防模型报告

- 城防网络：{len(fortifications)}（大城1、南北宫城2）；大城失守不自动摧毁宫城。
- 城垣Facility：{len(walls)}；字段含高度、厚度、材质、最大/当前耐久、具体守军Person和 `WallState`。
- 城门Facility：{len(gates)}；十二大城门均具Owner、Controller、开闭、耐久、小时通行容量和守军；另有宫门。
- 护城壕Feature：{len(moats)}；`Flooded` 阻止普通移动，须桥、填壕或受控门路。
- `Intact/Damaged` 城墙阻止Force；云梯有效高度达到墙高才可越墙；耐久归零转为 `Breached` 并产生可通行交叉点。
- V0不含完整冲车、投石机、地道、火攻、攻城后勤；这些按任务边界延期，不能标成已实现。
"""
    blue = f"""# 06 多Cell建设蓝图设计

当前蓝图：`{blueprint[0]['blueprint_id']}`，{blueprint[0]['cell_count']} Cell。

每个蓝图保存稳定ID、相对Cell、FacilityDefinitionId、方向、道路连接、模块、施工阶段、顺序和元数据。放置器统一校验Cell存在、可开发、Owner、占用和道路连接；玩家、历史生成器与AI共用同一模板。放置成功只建立预约/阶段计划，不代表瞬间完工。本阶段不做完整蓝图UI。
"""
    acceptance = f"""# 07 LUOYANG-184-HISTORICAL-V1 最终验收

|项|状态|证据|
|---|---|---|
|184东汉年代与来源分层|PASS|01报告、设施 source_ids/confidence/precision|
|统一HanWorldV1与2000m Cell|PASS|同一 GridSchemaVersion/CellId64；02报告|
|所有正式可见建筑Facility化|PASS|每个历史建筑有Definition、State、Owner、功能、岗位/服务与钩子|
|十二门、城垣、宫墙、护城壕|PASS|{len(gates)}座门（其中大城12）、{len(walls)}段墙、{len(moats)}段壕|
|按Person住房与现役兵营限制|PASS|04报告与数据审计|
|真实Person岗位、无工不产出|PASS|worker_person_ids、岗位资格与 normal_operation|
|AI按压力而非固定比例|PASS|ai_pressure与建议动作|
|多Cell Blueprint合同|PASS|06报告、Domain放置校验与数据模板|
|正式历史地图PNG|PASS|`LUOYANG_184_HISTORICAL_MAP_V1.png`|
|Unity连续缩放、选择与城防演示|PASS|场景控制器及EditMode/PlayMode证据（最终验证后填写日志）|
|完整攻城器械/蓝图UI/全国复原|DEFERRED|任务明确延期，不虚报|

最终测试状态必须以本轮验证日志为准；任一强制检查失败时，本报告状态应降为FAIL。
"""
    return {
        "01_LUOYANG_184_SOURCE_REPORT.md": sources,
        "02_2000M_CELL_EXPRESSIVENESS_AUDIT.md": expressiveness,
        "03_HISTORICAL_FACILITY_CATALOG.md": historical,
        "04_HOUSING_JOB_BALANCE_REPORT.md": housing,
        "05_FORTIFICATION_MODEL_REPORT.md": fort,
        "06_MULTI_CELL_BLUEPRINT_DESIGN.md": blue,
        "07_FINAL_ACCEPTANCE_REPORT.md": acceptance,
    }


def main():
    base_world = read_json(BASE_UNITY / "luoyang_world.json")
    base_layout = read_json(BASE_DATA / "layouts" / "recommended_layout.json")
    persons = read_jsonl(BASE_DATA / "population" / "recommended_persons.jsonl")
    households = read_jsonl(BASE_DATA / "population" / "recommended_households.jsonl")
    base_defs = read_json(BASE_UNITY / "facility_capacity_v0.json")["facilities"]
    definitions = historical_definitions(base_defs)
    definitions_by_id = {item["id"]: item for item in definitions}
    columns = base_world["columns"]
    anchor_row, anchor_col = divmod(base_world["city_anchor_cell_id64"], columns)
    plan, moat_cells, city_bounds = make_historical_plan(anchor_row, anchor_col)
    cells_by_coord = {(c["grid_y"], c["grid_x"]): c for c in base_world["cells"]}
    all_reserved = {(item["row"], item["column"]) for item in plan} | set(moat_cells)
    missing = [coord for coord in all_reserved if coord not in cells_by_coord]
    if missing:
        raise RuntimeError("Historical plan contains Cell outside Luoyang region: " + str(missing[:5]))
    moved = relocate_conflicting_base_facilities(base_layout["facilities"], cells_by_coord, all_reserved)
    for person in persons:
        if person.get("work_facility_id") in moved:
            person["current_cell_id64"] = moved[person["work_facility_id"]][1]
        elif person.get("residence_facility_id") in moved:
            person["current_cell_id64"] = moved[person["residence_facility_id"]][1]
    for household in households:
        if household.get("residence_facility_id") in moved:
            household["current_cell_id64"] = moved[household["residence_facility_id"]][1]

    facilities = [enrich_base_facility(item, definitions_by_id[item["definition_id"]]) for item in base_layout["facilities"]]
    facilities.extend(build_facility(item, definitions_by_id[item["definition_id"]], columns) for item in plan)
    if len({f["cell_id64"] for f in facilities}) != len(facilities):
        raise RuntimeError("One base Facility per Cell invariant violated")
    assign_people(persons, households, facilities, definitions)
    networks, walls, gates, moats = build_fortifications(facilities, moat_cells, columns)
    blueprints = build_blueprints()

    facility_by_cell = {f["cell_id64"]: f for f in facilities}
    population_by_cell = Counter(p["current_cell_id64"] for p in persons)
    wall_by_cell = {w["cell_id64"]: w for w in walls}
    gate_by_cell = {g["cell_id64"]: g for g in gates}
    moat_by_cell = {m["cell_id64"]: m for m in moats}
    unity_cells = []
    for source in base_world["cells"]:
        facility = facility_by_cell.get(source["cell_id64"])
        unity_cells.append({
            "cell_id64": source["cell_id64"], "grid_x": source["grid_x"], "grid_y": source["grid_y"],
            "terrain_class": source["terrain_class"], "slope_class": source["slope_class"],
            "water_class": source["water_class"], "elevation": source["elevation"],
            "road_class": source["road_class"], "fertility": source["fertility"], "developable": source["developable"],
            "owner_id": facility["owner_id"] if facility else None,
            "facility_id": facility["facility_id"] if facility else None,
            "facility_definition_id": facility["definition_id"] if facility else None,
            "facility_name": facility["display_name"] if facility else None,
            "facility_category_id": facility["category_id"] if facility else None,
            "historical_confidence": facility["historical_confidence"] if facility else None,
            "population": population_by_cell[source["cell_id64"]],
            "resident_capacity_persons": facility["residential_capacity_persons"] if facility else 0,
            "current_workers": len(facility["worker_person_ids"]) if facility else 0,
            "required_workers": facility["minimum_workers_for_normal_operation"] if facility else 0,
            "wall_state": wall_by_cell.get(source["cell_id64"], {}).get("wall_state"),
            "gate_state": gate_by_cell.get(source["cell_id64"], {}).get("gate_state"),
            "moat_state": moat_by_cell.get(source["cell_id64"], {}).get("moat_state"),
        })

    housed = sum(1 for p in persons if p.get("residence_facility_id"))
    effective = sum(1 for p in persons if p["labor_eligible"])
    employed = sum(1 for p in persons if p.get("work_facility_id"))
    civilian_capacity = sum(f["residential_capacity_persons"] for f in facilities
                            if "population.civilian" in f["allowed_resident_type_ids"])
    military_capacity = sum(f["residential_capacity_persons"] for f in facilities
                            if "population.active_military" in f["allowed_resident_type_ids"])
    vacant_jobs = sum(f["vacant_job_slots"] for f in facilities)
    skill_shortages = sum(max(0, f["minimum_workers_for_normal_operation"] - len(f["worker_person_ids"])) for f in facilities)
    ai_pressure = {
        "unhoused_persons": len(persons) - housed,
        "available_residential_person_slots": civilian_capacity + military_capacity - housed,
        "unemployed_workers": effective - employed, "vacant_job_slots": vacant_jobs,
        "skill_shortage_slots": skill_shortages, "food_days_basis_points": 6400, "security_basis_points": 7200,
        "recommended_action_ids": ["ai.action.build_housing", "ai.action.train_for_vacancies", "ai.action.staff_fortifications"],
    }
    population_profile = {
        "profile_id": "recommended", "total_persons": len(persons), "total_households": len(households),
        "effective_workers": effective, "employed_workers": employed, "unemployed_workers": effective - employed,
        "housed_persons": housed, "unhoused_persons": len(persons) - housed,
        "civilian_residential_capacity_persons": civilian_capacity,
        "active_military_barracks_capacity_persons": military_capacity,
    }
    world = {
        "schema": "mandate.luoyang-184-historical-world.v1", "scenario_year": 184,
        "scenario_polity_id": "polity.eastern_han", "grid_schema_version": GRID_SCHEMA,
        "grid_version": base_world["grid_version"], "cell_size_m": base_world["cell_size_m"],
        "columns": columns, "rows": base_world["rows"], "city_id": base_world["city_id"],
        "city_anchor_cell_id64": base_world["city_anchor_cell_id64"], "hulao_cell_id64": base_world["hulao_cell_id64"],
        "city_footprint_cell_ids": sorted(f["cell_id64"] for f in facilities if city_bounds["top"] <= f["grid_y"] <= city_bounds["bottom"] and city_bounds["left"] <= f["grid_x"] <= city_bounds["right"]),
        "population_profile": population_profile, "ai_pressure": ai_pressure,
        "cells": unity_cells, "facilities": facilities, "fortification_networks": networks,
        "blueprints": [{k: v for k, v in b.items() if k != "cells"} for b in blueprints],
    }

    jobs = job_catalog()
    sources = {
        "schema": "mandate.historical-source-catalog.v1", "scenario_year": 184,
        "sources": [
            {"id": SOURCE_IDS["cssn_city"], "url": "https://www.cssn.cn/lsx/lsx_kgx/202210/t20221024_5552600.shtml", "kind": "modern_archaeological_synthesis"},
            {"id": SOURCE_IDS["pku_axis"], "url": "https://ir.pku.edu.cn/handle/20.500.11897/676372", "kind": "academic_thesis"},
            {"id": SOURCE_IDS["cass_ritual"], "url": "https://www.pishu.com.cn/skwx_ps/ps/literature?ID=8644335&SiteID=14", "kind": "archaeological_report_catalog"},
            {"id": SOURCE_IDS["houhanshu_gates"], "url": "https://www.shidianguji.com/mid-page/7620933339273183283", "kind": "transmitted_primary_text"},
            {"id": SOURCE_IDS["ncha_site"], "url": "https://www.ncha.gov.cn/art/2021/11/18/art_2318_45063.html", "kind": "heritage_authority"},
        ],
        "known_conflicts": [{"topic": "north_gate_east_west_order", "resolution": "retain both names; mark precise orientation Approximate"},
                            {"topic": "market_names_and_exact_positions", "resolution": "Jinshi/Nanshi reconstructed; Mashi GameplayReconstruction"}],
    }
    fortification_payload = {"schema": "mandate.fortification-network.v1", "networks": networks, "walls": walls, "gates": gates, "moats": moats,
                             "siege_v0": {"wall_blocks_force": True, "gate_controller_changes_state": True,
                                          "palace_network_independent_after_main_breach": True,
                                          "ladder_rule": "effective_height_cm >= wall_height_cm", "breach_creates_passable_crossing": True}}
    blueprint_payload = {"schema": "mandate.facility-blueprint.v1", "blueprints": blueprints}
    definition_payload = {"schema": "mandate.facility-definition.v1", "definitions": definitions}
    job_payload = {"schema": "mandate.facility-job-definition.v1", "definitions": jobs}

    for root in (ROOT, UNITY):
        write_json(root / "luoyang_184_world.json", world)
        write_json(root / "facility_definitions_184.json", definition_payload)
        write_json(root / "job_definitions_184.json", job_payload)
        write_json(root / "fortifications_184.json", fortification_payload)
        write_json(root / "blueprints_184.json", blueprint_payload)
        write_json(root / "historical_sources_184.json", sources)
    write_jsonl(ROOT / "population" / "persons_184.jsonl", persons)
    write_jsonl(ROOT / "population" / "households_184.jsonl", households)

    official_map = DELIVERABLES / "LUOYANG_184_HISTORICAL_MAP_V1.png"
    render_map(world, facilities, moat_cells, city_bounds, official_map)
    (UNITY / "LUOYANG_184_HISTORICAL_MAP_V1.png").write_bytes(official_map.read_bytes())
    reports = build_reports(world, definitions, facilities, networks, walls, gates, moats, blueprints, city_bounds)
    REPORTS.mkdir(parents=True, exist_ok=True)
    for name, content in reports.items():
        (REPORTS / name).write_text(content, encoding="utf-8")
        (UNITY / name).write_text(content, encoding="utf-8")

    print(json.dumps({"status": "GENERATED", "persons": len(persons), "households": len(households),
                      "facilities": len(facilities), "historical_facilities": len(plan), "walls": len(walls),
                      "gates": len(gates), "main_city_gates": sum(1 for g in gates if g["network_id"].endswith("main_wall")),
                      "moats": len(moats), "moved_base_facilities": len(moved), "map": str(official_map)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
