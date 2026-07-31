#!/usr/bin/env python3
"""Build the M13 county catalog from locally cached public-domain source pages.

The source pages are development inputs only. The committed CSV files are the
offline runtime/content contract. Run with:

    python Tools/Build-Han140CountyCatalog.py --source-dir Temp/M13Sources
"""

from __future__ import annotations

import argparse
import csv
import ctypes
import re
from collections import defaultdict
from pathlib import Path

from bs4 import BeautifulSoup
from unidecode import unidecode


SOURCE_ID = "source.hou_han_shu.jun_guo_zhi"
PROJECT_SOURCE_ID = "source.project.prototype_location_catalog.v1"
EXPECTED_ITEMIZED_COUNT = 1182

CITY_SPECS = {
    "C014": ("北海国", "剧"),
    "C015": ("济南国", "东平陵"),
    "C016": ("济北国", "卢"),
    "C017": ("琅邪国", "开阳"),
    "C018": ("东郡", "濮阳"),
    "C019": ("陈留郡", "陈留"),
    "C020": ("沛国", "沛"),
    "C021": ("下邳国", "下邳"),
    "C022": ("广陵郡", "广陵"),
    "C023": ("陈国", "陈"),
    "C024": ("沛国", "谯"),
    "C025": ("颍川郡", "许"),
    "C026": ("汝南郡", "平舆"),
    "C027": ("河南尹", "雒阳"),
    "C028": ("弘农郡", "弘农"),
    "C029": ("河内郡", "怀"),
    "C030": ("河东郡", "安邑"),
    "C031": ("京兆尹", "长安"),
    "C032": ("安定郡", "临泾"),
    "C033": ("汉阳郡", "冀"),
    "C034": ("金城郡", "允吾"),
    "C035": ("金城郡", None),
    "C036": ("武威郡", "姑臧"),
    "C037": ("武都郡", "下辨"),
    "C038": ("南阳郡", "宛"),
    "C039": ("汉中郡", "上庸"),
    "C040": ("南阳郡", "新野"),
    "C041": ("南郡", "襄阳"),
    "C042": ("江夏郡", None),
    "C043": ("南郡", "江陵"),
    "C044": ("武陵郡", "孱陵"),
    "C045": ("江夏郡", "鄂"),
    "C046": ("武陵郡", "临沅"),
    "C047": ("长沙郡", "临湘"),
    "C048": ("桂阳郡", "郴"),
    "C049": ("零陵郡", "泉陵"),
    "C050": ("豫章郡", "柴桑"),
    "C051": ("豫章郡", "南昌"),
    "C052": ("九江郡", "寿春"),
    "C053": ("九江郡", "合肥"),
    "C054": ("庐江郡", None),
    "C055": ("庐江郡", "皖"),
    "C056": ("丹阳郡", "秣陵"),
    "C057": ("吴郡", "吴"),
    "C058": ("会稽郡", "山阴"),
    "C059": ("豫章郡", "鄱阳"),
    "C060": ("豫章郡", "庐陵"),
    "C061": (None, None),
    "C062": ("南海郡", "番禺"),
    "C063": ("合浦郡", "合浦"),
    "C064": ("交趾郡", "龙编"),
    "C065": ("汉中郡", "南郑"),
    "C066": ("广汉郡", "梓潼"),
    "C067": ("蜀郡", "成都"),
    "C068": ("巴郡", "江州"),
    "C069": ("巴郡", "鱼复"),
    "C070": ("永昌郡", "不韦"),
    "C071": ("益州郡", "味"),
    "C072": ("永昌郡", "云南"),
    "C073": ("牂牁郡", "故且兰"),
    "C074": ("越巂郡", "邛都"),
    "C075": ("犍为属国", "朱提"),
    "C076": ("广汉郡", "涪"),
    "C077": ("汉中郡", "西城"),
}


def read_csv(path: Path) -> tuple[list[str], list[dict[str, str]]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        return list(reader.fieldnames or []), list(reader)


def write_csv(path: Path, headers: list[str], rows: list[dict[str, str]]) -> None:
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=headers, lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def simplify(value: str) -> str:
    if not value:
        return value
    flag = 0x02000000  # LCMAP_SIMPLIFIED_CHINESE
    function = ctypes.windll.kernel32.LCMapStringEx
    length = function("zh-CN", flag, value, len(value), None, 0, None, None, 0)
    if length <= 0:
        raise RuntimeError("Windows Chinese simplification failed")
    buffer = ctypes.create_unicode_buffer(length)
    function("zh-CN", flag, value, len(value), buffer, length, None, None, 0)
    return buffer.value


def base_name(value: str) -> str:
    for suffix in ("县", "縣"):
        if value.endswith(suffix):
            return value[: -len(suffix)]
    return value


def stable_slug(value: str) -> str:
    slug = re.sub(r"[^a-z0-9]+", "", unidecode(value).lower())
    if slug:
        return slug
    return "u" + "-".join(f"{ord(character):x}" for character in value)


def clean_paragraph(paragraph) -> str:
    clone = BeautifulSoup(str(paragraph), "html.parser")
    for note in clone.select(".style7"):
        note.decompose()
    return clone.get_text("", strip=False).replace("\r", "").replace("\n", "")


def replace_chunk(chunks: list[str], prefix: str, replacements: list[str]) -> None:
    index = next((i for i, chunk in enumerate(chunks) if chunk.startswith(prefix)), None)
    if index is None:
        raise ValueError(f"Missing source chunk prefix: {prefix}")
    chunks[index : index + 1] = replacements


def normalize_chunks(parent_name: str, chunks: list[str]) -> list[str]:
    chunks = list(chunks)
    if parent_name == "河南尹":
        replace_chunk(chunks, "河南周公", [
            "河南周公时所城雒邑也，春秋时谓之王城。",
            "梁故国，伯翳后。",
            "荥阳有鸿沟水。",
        ])
        replace_chunk(chunks, "开封菀陵", ["开封", "菀陵，有棐林。"])
    elif parent_name == "河东郡":
        replace_chunk(chunks, "猗氏垣", ["猗氏", "垣有王屋山。"])
    elif parent_name == "魏郡":
        index = next(i for i, chunk in enumerate(chunks) if chunk.startswith("五鹿墟"))
        chunks[index - 1 : index + 1] = [chunks[index - 1] + "。" + chunks[index]]
    elif parent_name == "河间国":
        replace_chunk(chunks, "成平", ["成平，故属勃海。", "东平舒，故属勃海。"])
    elif parent_name == "济阴郡":
        index = next(i for i, chunk in enumerate(chunks) if chunk.startswith("故属东郡"))
        chunks[index - 1 : index + 1] = [chunks[index - 1] + "。" + chunks[index]]
    elif parent_name == "彭城国":
        replace_chunk(chunks, "傅阳", ["傅阳，有柤水。", "吕"])
        replace_chunk(chunks, "留梧", ["留", "梧"])
    elif parent_name == "乐安国":
        replace_chunk(chunks, "博昌", ["博昌，有薄姑城。", "蓼城，侯国。"])
    elif parent_name == "南阳郡":
        for prefix in ("桐柏", "西，有断蛇"):
            index = next(i for i, chunk in enumerate(chunks) if chunk.startswith(prefix))
            chunks[index - 1 : index + 1] = [chunks[index - 1] + "。" + chunks[index]]
    elif parent_name == "丹阳郡":
        replace_chunk(chunks, "句容江乘", ["句容", "江乘"])
    elif parent_name == "豫章郡":
        index = next(i for i, chunk in enumerate(chunks) if chunk.startswith("彭蠡"))
        chunks[index - 1 : index + 1] = [chunks[index - 1] + "。" + chunks[index]]
    elif parent_name == "犍为郡":
        replace_chunk(chunks, "南安有鱼涪津。僰道", ["南安有鱼涪津。", "僰道"])
    elif parent_name == "犍为属国":
        replace_chunk(chunks, "朱提山出银、铜。汉阳", ["朱提山出银、铜。", "汉阳"])
    elif parent_name == "越巂郡":
        replace_chunk(chunks, "三缝会无", ["三缝", "会无，出铁。"])
    elif parent_name == "武都郡":
        replace_chunk(chunks, "河池沮沔水出东狼谷。", ["河池", "沮，沔水出东狼谷。"])
    elif parent_name == "涿郡":
        replace_chunk(chunks, "乃，侯国。故安易水出", ["迺，侯国。", "故安，易水出。"])
    elif parent_name == "郁林郡":
        replace_chunk(chunks, "定周增食", ["定周", "增食"])
    elif parent_name == "交趾郡":
        replace_chunk(chunks, "北带 稽徐", ["北带", "稽徐"])
    elif parent_name == "汉阳郡":
        index = next(i for i, chunk in enumerate(chunks) if chunk.startswith("豲坻聚"))
        chunks[index - 1 : index + 1] = [chunks[index - 1] + "。" + chunks[index]]
        replace_chunk(chunks, "豲道兰干", ["豲道", "兰干"])
    return chunks


NAME_OVERRIDES = {
    "河南周公": "河南",
    "梁故国": "梁",
    "温苏子所都": "温",
    "朝歌纣所都居": "朝歌",
    "新郑": "新郑",
    "故且兰": "故且兰",
    "山阴会稽山": "山阴",
    "湔氐道岷山": "湔氐道",
    "杜陵酆在西南": "杜陵",
    "绦邑": "绛邑",
    "鲁国": "鲁",
    "阴安邑": "阴安",
    "朱提山": "朱提",
    "兾": "冀",
    "晥": "皖",
    "寻阳南": "寻阳",
    "緜竹": "绵竹",
    "襃中": "褒中",
    "秣陵南": "秣陵",
    "朴\ud841": "朴\U000207FC",
    "羸\ud863": "羸\U00028EFB",
}

DESCRIPTION_MARKERS = (
    "有", "故", "本", "周时", "诗", "苏子", "纣所", "尧都", "高帝", "世祖",
    "王莽", "安帝", "章帝", "和帝", "殇帝", "永平", "永元", "建武",
    "阳嘉", "延平", "熹平", "出铁", "出铜", "出银", "水出", "山出",
    "南有", "北有", "东有", "西有", "刺史治",
)


def extract_name(chunk: str) -> str:
    chunk = chunk.lstrip("，。； ")
    for prefix, result in NAME_OVERRIDES.items():
        if chunk.startswith(prefix):
            return result
    candidate = re.split(r"[，。；〔]", chunk, maxsplit=1)[0].strip()
    for marker in DESCRIPTION_MARKERS:
        index = candidate.find(marker, 1)
        if index > 0:
            candidate = candidate[:index]
    return candidate.strip()


def extract_counties(source_dir: Path, admin_rows: list[dict[str, str]]) -> list[dict[str, str]]:
    parent_rows = {
        row["canonical_name"]: row
        for row in admin_rows
        if row["unit_type"] in {"commandery", "kingdom", "other"}
        and row["admin_unit_id"].count(".") >= 3
    }
    source_name_aliases = {
        "河间国": "河闲国",
        "雁门郡": "鴈门郡",
        "犍为郡": "犍爲郡",
        "犍为属国": "犍爲属国",
    }
    lookup = {source_name_aliases.get(name, name): row for name, row in parent_rows.items()}
    results: list[dict[str, str]] = []

    for source_path in sorted(source_dir.glob("*.htm")):
        soup = BeautifulSoup(source_path.read_text(encoding="utf-8"), "html.parser")
        paragraphs = soup.find_all("p")
        for index, paragraph in enumerate(paragraphs[:-1]):
            header = simplify("".join(clean_paragraph(paragraph).split()))
            matches = [(source_name, row) for source_name, row in lookup.items() if header.startswith(source_name)]
            match = max(matches, key=lambda value: len(value[0])) if matches else None
            if match is None:
                continue
            source_name, parent = match
            suffix = header[len(source_name) :]
            if (
                "户" not in header
                and header != "辽东属国"
                and re.match(r"^[一二三四五六七八九十百]+城", suffix) is None
            ):
                continue
            body = clean_paragraph(paragraphs[index + 1])
            chunks = [simplify(value.strip()) for value in body.split("\u3000") if value.strip()]
            chunks = normalize_chunks(parent["canonical_name"], chunks)
            for order, chunk in enumerate(chunks, start=1):
                name = extract_name(chunk)
                if not name or len(name) > 6:
                    raise ValueError(f"Suspicious county name under {parent['canonical_name']}: {name!r} from {chunk!r}")
                results.append({
                    "parent_admin_unit_id": parent["admin_unit_id"],
                    "parent_name": parent["canonical_name"],
                    "name_140": name,
                    "canonical_name": name,
                    "source_page": source_path.stem,
                    "source_order": str(order),
                    "raw_item": chunk,
                })

    if len(results) != EXPECTED_ITEMIZED_COUNT:
        found_parents = {row["parent_name"] for row in results}
        missing_parents = sorted(set(parent_rows) - found_parents)
        raise ValueError(
            f"Expected {EXPECTED_ITEMIZED_COUNT} itemized county rows, found {len(results)}; "
            f"parents={len(found_parents)} missing={missing_parents}"
        )
    return results


def build_catalog(data_root: Path, counties: list[dict[str, str]]) -> None:
    admin_path = data_root / "han_140_administrative_units.csv"
    region_path = data_root / "stable_population_regions.csv"
    mapping_path = data_root / "han_140_region_mapping.csv"
    crosswalk_path = data_root / "game_location_crosswalk.csv"

    admin_headers, admin_rows = read_csv(admin_path)
    region_headers, region_rows = read_csv(region_path)
    mapping_headers, mapping_rows = read_csv(mapping_path)
    crosswalk_headers, crosswalk_rows = read_csv(crosswalk_path)

    old_counties = [row for row in admin_rows if row["unit_type"] == "county"]
    old_by_parent_name = {
        (row["parent_admin_unit_id"], base_name(row["canonical_name"])): row for row in old_counties
    }
    old_crosswalk_region = {
        row["admin_unit_id"]: row["stable_region_id"]
        for row in crosswalk_rows
        if row["admin_unit_id"] and row["stable_region_id"]
    }
    region_by_id = {row["stable_region_id"]: row for row in region_rows}
    parent_stable = {
        row["source_id"]: row["target_id"]
        for row in mapping_rows
        if row["relation_type"] == "population_coverage"
    }
    parent_by_id = {row["admin_unit_id"]: row for row in admin_rows}
    used_admin_ids: set[str] = set()
    used_region_ids: set[str] = set()
    generated_admin: list[dict[str, str]] = []
    generated_regions: list[dict[str, str]] = []
    generated_mappings: list[dict[str, str]] = []
    county_index: dict[tuple[str, str], tuple[dict[str, str], dict[str, str]]] = {}
    seat_by_parent: dict[str, str] = {}

    for item in counties:
        key = (item["parent_admin_unit_id"], item["canonical_name"])
        previous = old_by_parent_name.get(key)
        slug = stable_slug(item["canonical_name"])
        previous_admin_id = previous["admin_unit_id"] if previous else ""
        admin_id = (
            previous_admin_id
            if re.fullmatch(r"admin\.han140\.[a-z0-9]+(?:\.[a-z0-9]+)*", previous_admin_id)
            else f"{item['parent_admin_unit_id']}.{slug}"
        )
        if admin_id in used_admin_ids:
            admin_id += ".u" + "".join(f"{ord(c):x}" for c in item["canonical_name"])
        used_admin_ids.add(admin_id)

        parent_region_id = parent_stable[item["parent_admin_unit_id"]]
        previous_region_id = old_crosswalk_region.get(admin_id, "")
        region_id = (
            previous_region_id
            if re.fullmatch(r"geo\.region\.[a-z0-9]+(?:\.[a-z0-9]+)*", previous_region_id)
            else f"{parent_region_id}.county.{slug}"
        )
        if region_id in used_region_ids:
            region_id += ".u" + "".join(f"{ord(c):x}" for c in item["canonical_name"])
        used_region_ids.add(region_id)

        discrepancy = item["parent_name"] == "巴郡" and item["canonical_name"] == "汉昌"
        confidence = "medium" if discrepancy else "high"
        note = (
            f"郡国志卷{item['source_page']}县级列项第{item['source_order']}项；"
            "原文首列为郡国治所" if item["source_order"] == "1" else
            f"郡国志卷{item['source_page']}县级列项第{item['source_order']}项"
        )
        if discrepancy:
            note += "；巴郡标题称十四城但正文列出十五项，本项保留并进入数量争议审计"

        admin_row = {
            "admin_unit_id": admin_id,
            "parent_admin_unit_id": item["parent_admin_unit_id"],
            "unit_type": "county",
            "name_140": item["name_140"],
            "canonical_name": item["canonical_name"],
            "seat_admin_unit_id": "",
            "valid_from_year": "140",
            "valid_to_year": "140",
            "source_ids": SOURCE_ID,
            "confidence": confidence,
            "notes": note,
        }
        previous_region = region_by_id.get(previous_region_id)
        region_row = previous_region or {
            "stable_region_id": region_id,
            "parent_stable_region_id": parent_region_id,
            "region_type": "county_area",
            "canonical_name": f"{item['canonical_name']}县级区域",
            "modern_reference": "",
            "centroid_latitude": "",
            "centroid_longitude": "",
            "geometry_status": "none",
            "confidence": "unknown",
            "provisional": "true",
            "notes": "140年县级列项的稳定身份；未校定现代位置和县界",
        }
        region_row = dict(region_row)
        region_row["stable_region_id"] = region_id
        region_row["parent_stable_region_id"] = parent_region_id
        generated_admin.append(admin_row)
        generated_regions.append(region_row)
        generated_mappings.append({
            "source_id": admin_id,
            "target_id": region_id,
            "relation_type": "county_identity",
            "valid_from_year": "140",
            "valid_to_year": "140",
            "weight_basis_points": "10000",
            "mapping_method": "source_itemized_county_identity",
            "confidence": confidence,
            "provisional": "true",
            "notes": "县级身份映射；不拆分或重复郡国人口",
        })
        county_index[key] = (admin_row, region_row)
        seat_by_parent.setdefault(item["parent_admin_unit_id"], admin_id)

    non_county_admin = [dict(row) for row in admin_rows if row["unit_type"] != "county"]
    for row in non_county_admin:
        if row["admin_unit_id"] in seat_by_parent:
            row["seat_admin_unit_id"] = seat_by_parent[row["admin_unit_id"]]

    retained_regions = [row for row in region_rows if row["region_type"] != "county_area"]
    retained_mappings = [row for row in mapping_rows if row["relation_type"] != "county_identity"]

    new_crosswalk = [row for row in crosswalk_rows if not (row["game_location_kind"] == "city_catalog" and int(row["game_location_id"][1:]) >= 14)]
    parent_by_name = {row["canonical_name"]: row for row in non_county_admin}
    for city_id, (parent_name, county_name) in CITY_SPECS.items():
        if parent_name is None:
            new_crosswalk.append({
                "game_location_id": city_id,
                "game_location_kind": "city_catalog",
                "stable_region_id": "",
                "admin_unit_id": "",
                "mapping_status": "unresolved",
                "relation_type": "city_catalog_unresolved",
                "valid_from_year": "",
                "valid_to_year": "",
                "source_ids": PROJECT_SOURCE_ID,
                "confidence": "unknown",
                "provisional": "true",
                "notes": "跨时代城市身份尚无足够140年行政与稳定地理证据，保留待考",
            })
            continue
        parent = parent_by_name[parent_name]
        if county_name is None:
            new_crosswalk.append({
                "game_location_id": city_id,
                "game_location_kind": "city_catalog",
                "stable_region_id": parent_stable[parent["admin_unit_id"]],
                "admin_unit_id": parent["admin_unit_id"],
                "mapping_status": "aggregate",
                "relation_type": "city_catalog_regional_proxy",
                "valid_from_year": "",
                "valid_to_year": "",
                "source_ids": f"{SOURCE_ID}|{PROJECT_SOURCE_ID}",
                "confidence": "low",
                "provisional": "true",
                "notes": "目录显示名在140年只安全对应郡域级战略代理；不伪装为单一县城",
            })
            continue
        county_key = (parent["admin_unit_id"], county_name)
        if county_key not in county_index:
            raise KeyError(f"City {city_id} cannot find {parent_name}/{county_name}")
        county, region = county_index[county_key]
        relation = "city_catalog_cross_era_precursor" if city_id in {"C033", "C044", "C045", "C056", "C069", "C071"} else "city_catalog_county_identity"
        new_crosswalk.append({
            "game_location_id": city_id,
            "game_location_kind": "city_catalog",
            "stable_region_id": region["stable_region_id"],
            "admin_unit_id": county["admin_unit_id"],
            "mapping_status": "approximate",
            "relation_type": relation,
            "valid_from_year": "",
            "valid_to_year": "",
            "source_ids": f"{SOURCE_ID}|{PROJECT_SOURCE_ID}",
            "confidence": "low" if relation.endswith("precursor") else "medium",
            "provisional": "true",
            "notes": "77城目录对应140年县级列项及未校定县域；不承接整郡人口",
        })

    new_crosswalk.sort(key=lambda row: (row["game_location_kind"], row["game_location_id"]))
    write_csv(admin_path, admin_headers, non_county_admin + generated_admin)
    write_csv(region_path, region_headers, retained_regions + generated_regions)
    write_csv(mapping_path, mapping_headers, retained_mappings + generated_mappings)
    write_csv(crosswalk_path, crosswalk_headers, new_crosswalk)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", type=Path, required=True)
    parser.add_argument("--data-root", type=Path, default=Path("Data/HistoricalPopulation"))
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()

    _, admin_rows = read_csv(args.data_root / "han_140_administrative_units.csv")
    counties = extract_counties(args.source_dir, admin_rows)
    grouped = defaultdict(int)
    for county in counties:
        grouped[county["parent_name"]] += 1
    print(f"RESULT han140-county-extraction=passed parents={len(grouped)} counties={len(counties)}")
    if not args.check_only:
        build_catalog(args.data_root, counties)
        print("RESULT han140-county-build=passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
