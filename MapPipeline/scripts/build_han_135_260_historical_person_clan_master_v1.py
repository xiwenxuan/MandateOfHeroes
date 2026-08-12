#!/usr/bin/env python3
"""Build the conservative V1 historical person/clan master from the protected V5 workbook import.

The builder preserves every existing PersonId, refuses name-only identity merges, keeps unresolved
historical locations explicit, and never creates Household or FamilyOrganization instances.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import time
from collections import Counter, defaultdict
from datetime import date
from pathlib import Path


SCENARIOS = [
    ("scenario.han.140.peace", 140, "汉室承平"),
    ("scenario.han.184.yellow_turban", 184, "黄巾起义"),
    ("scenario.han.189.luoyang_coup", 189, "洛阳政变"),
    ("scenario.han.194.warlords", 194, "群雄割据"),
    ("scenario.han.200.guandu_eve", 200, "官渡前夜"),
    ("scenario.han.207.longzhong", 207, "三顾茅庐"),
    ("scenario.han.214.yizhou_settled", 214, "益州初定·三分渐成"),
    ("scenario.han.219.hanzhong_king", 219, "汉中王·荆州危局"),
    ("scenario.han.223.baidicheng", 223, "白帝托孤"),
    ("scenario.han.227.northern_expedition", 227, "出师北伐"),
    ("scenario.han.234.wuzhang", 234, "五丈原"),
    ("scenario.han.249.gaopingling", 249, "高平陵之变"),
    ("scenario.han.260.endgame", 260, "曹髦之死·三国终局"),
]

MANUAL_CONFIRMED_CLANS = {
    "F006", "F036", "F045", "F046", "F047", "F048", "F077", "F081", "F092",
    "F102", "F120", "F132", "F133", "F136", "F154", "F156", "F157", "F177",
    "F270", "F272", "F301", "F362", "F415",
}

MANUAL_BRANCHES = [
    ("F415", "eastern_han_mainline", "东汉帝系", "刘志", None),
    ("F092", "yuan_feng", "袁逢支", "袁逢", None),
    ("F092", "yuan_wei", "袁隗支", "袁隗", None),
    ("F102", "sima_yi", "司马懿支", "司马懿", None),
    ("F102", "sima_fu", "司马孚支", "司马孚", None),
    ("F120", "zhuge_jin", "诸葛瑾支", "诸葛瑾", None),
    ("F120", "zhuge_liang", "诸葛亮支", "诸葛亮", None),
    ("F120", "zhuge_dan", "诸葛诞支", "诸葛诞", None),
    ("F133", "cao_song", "曹嵩—曹操支", "曹嵩", None),
    ("F133", "cao_ren", "曹仁支", "曹仁", None),
    ("F133", "cao_hong", "曹洪支", "曹洪", None),
    ("F045", "sun_jian", "孙坚支", "孙坚", None),
    ("F045", "sun_jing", "孙静支", "孙静", None),
    ("F045", "sun_qiang", "孙羌支", "孙羌", None),
    ("F154", "xun_shu", "荀淑后裔支", "荀淑", None),
]

COMPOUND_SURNAMES = (
    "司马", "诸葛", "夏侯", "皇甫", "公孙", "毌丘", "淳于", "令狐", "东方", "长孙", "欧阳"
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=str(Path(__file__).resolve().parents[2]))
    return parser.parse_args()


def dump(path: Path, payload: object, compact: bool = False) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if compact:
        text = json.dumps(payload, ensure_ascii=False, separators=(",", ":"))
    else:
        text = json.dumps(payload, ensure_ascii=False, indent=2)
    path.write_text(text + "\n", encoding="utf-8")


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def stable_token(prefix: str, value: str) -> str:
    return f"{prefix}.{hashlib.sha1(value.encode('utf-8')).hexdigest()[:16]}"


def as_int(value):
    if value is None or value == "":
        return None
    try:
        return int(value)
    except (TypeError, ValueError):
        return None


def records(sheet: dict) -> list[dict]:
    values = sheet["values"]
    headers = [str(value or "").strip() for value in values[0]]
    result = []
    for row in values[1:]:
        if not any(value not in (None, "") for value in row):
            continue
        result.append({headers[index]: row[index] if index < len(row) else None for index in range(len(headers))})
    return result


def split_list(value) -> list[str]:
    if not value:
        return []
    return [item.strip() for item in re.split(r"[、,，;/；]+", str(value)) if item.strip()]


def parse_name(name: str, anonymous: bool) -> tuple[str | None, str | None]:
    if anonymous or not name:
        return None, None
    for surname in COMPOUND_SURNAMES:
        if name.startswith(surname) and len(name) > len(surname):
            return surname, name[len(surname):]
    if 2 <= len(name) <= 4 and all("\u3400" <= ch <= "\u9fff" for ch in name):
        return name[0], name[1:]
    return None, None


def is_anonymous_name(name: str, courtesy: str | None) -> bool:
    if not name:
        return True
    if "某" in name or "子女" in name or name.endswith("氏女"):
        return True
    return not courtesy and (name.endswith("氏") or "夫人" in name or "皇后" in name) and len(name) <= 6


def evidence_from_status(status: str | None) -> str:
    text = str(status or "")
    if text.startswith("A"):
        return "A"
    if text.startswith("B"):
        return "B"
    if text.startswith("C"):
        return "C"
    return "D"


def clean_geo_name(value: str) -> str:
    return re.sub(r"(郡|国|尹|州|校尉部)$", "", value or "")


def build_geo_resolver(root: Path):
    admin = json.loads((root / "Assets/StreamingAssets/HistoricalPopulation/Han135260V1/administrative_timeline.json").read_text(encoding="utf-8"))
    year = json.loads((root / "Assets/StreamingAssets/HistoricalPopulation/Han135260V1/years/year_184.json").read_text(encoding="utf-8"))
    regions = []
    for item in admin["records"]:
        name = item["historical_name"]
        regions.append((name, clean_geo_name(name), item["region_permanent_id"], item["parent_region_permanent_id"]))
    counties = [
        (item["historical_county_name"], item["county_permanent_id"], item["parent_region_permanent_id"], item["province_permanent_id"])
        for item in year["counties"]
    ]
    valid_regions = {item[2] for item in regions}
    valid_counties = {item[1] for item in counties}

    def resolve(text_value) -> dict:
        text = str(text_value or "").strip()
        if not text:
            return {"text": text, "region_id": None, "county_id": None, "city_id": None, "confidence": "D", "method": "Unknown"}
        region_matches = []
        for full, base, region_id, province_id in regions:
            if (len(base) >= 2 and base in text) or full in text:
                region_matches.append((max(len(full), len(base)), full, region_id, province_id))
        region_matches.sort(reverse=True)
        region_id = None
        province_id = None
        if region_matches:
            best_length = region_matches[0][0]
            best = [item for item in region_matches if item[0] == best_length]
            if len({item[2] for item in best}) == 1:
                region_id = best[0][2]
                province_id = best[0][3]
        county_matches = []
        for county_name, county_id, parent_region, county_province in counties:
            if len(county_name) < 2 and not text.endswith(county_name):
                continue
            if county_name and county_name in text and (region_id is None or parent_region == region_id):
                county_matches.append((len(county_name), county_id, parent_region, county_province))
        county_matches.sort(reverse=True)
        county_id = None
        if county_matches:
            best_length = county_matches[0][0]
            best = [item for item in county_matches if item[0] == best_length]
            if len({item[1] for item in best}) == 1:
                county_id = best[0][1]
                region_id = best[0][2]
                province_id = best[0][3]
        city_id = "city.han.洛阳" if "洛阳" in text else None
        confidence = "B" if county_id else ("C" if region_id else "D")
        method = "CountyAndRegionNameMatch" if county_id else ("RegionNameMatch" if region_id else "Unresolved")
        return {"text": text, "province_id": province_id, "region_id": region_id, "county_id": county_id, "city_id": city_id, "confidence": confidence, "method": method}

    return resolve, valid_regions, valid_counties


def contains_year(record: dict, year: int) -> bool:
    start = as_int(record.get("start_year"))
    end = as_int(record.get("end_year"))
    return (start is None or start <= year) and (end is None or year <= end)


def life_state(person: dict, year: int) -> str:
    birth = person.get("birth_year")
    death = person.get("death_year")
    if birth is not None and year < birth:
        return "NotBorn"
    if death is not None and year > death:
        return "Dead"
    if birth is not None and death is not None:
        return "Alive"
    if birth is not None or death is not None:
        return "PossiblyAlive"
    return "Unknown"


def write_meta(path: Path, root: Path) -> None:
    rel = path.relative_to(root).as_posix()
    guid = hashlib.md5(("mandate:" + rel).encode("utf-8")).hexdigest()
    meta = path.with_name(path.name + ".meta")
    meta.write_text(f"fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n", encoding="utf-8")


def main() -> None:
    started = time.perf_counter()
    args = parse_args()
    root = Path(args.root).resolve()
    baseline_path = root / "Data/HistoricalPersons/han_135_260_historical_person_clan_existing_v5.json"
    baseline = json.loads(baseline_path.read_text(encoding="utf-8"))
    sheets = baseline["personMasterV5"]["sheets"]
    person_rows = records(sheets["人物母表"])
    relation_rows = records(sheets["家族关系"])
    candidate_rows = records(sheets["家族组织候选"])
    resolve_geo, valid_region_ids, valid_county_ids = build_geo_resolver(root)

    names_to_ids = defaultdict(list)
    for row in person_rows:
        names_to_ids[str(row["姓名"]).strip()].append(str(row["PersonId"]).strip())

    source_candidates = []
    for row in person_rows:
        if row.get("史料入口"):
            source_candidates.append((str(row.get("主要史料") or "历史来源"), str(row["史料入口"])))
    for sheet_name in ("V3来源索引", "V4来源索引"):
        for row in records(sheets[sheet_name]):
            url = row.get("URL")
            title = row.get("来源/卷目") or row.get("来源") or "历史来源"
            if url:
                for item in str(url).split(" / "):
                    source_candidates.append((str(title), item.strip()))
    for sheet_name in ("LocationTimeline", "CivilOfficeTimeline", "MilitaryOfficeTimeline", "TitleTimeline", "AllegianceTimeline"):
        for row in records(sheets[sheet_name]):
            if row.get("史料URL"):
                source_candidates.append((str(row.get("史料") or "历史来源"), str(row["史料URL"])))

    sources_by_url = {}
    for title, url in source_candidates:
        if not url:
            continue
        sources_by_url.setdefault(url, {
            "source_id": stable_token("source.historical", url),
            "source_title": title,
            "author": None,
            "period": "AncientOrModern",
            "source_type": "PrimaryHistoricalText" if "wikisource" in url or "ctext" in url else "StructuredReference",
            "edition": None,
            "volume": None,
            "chapter": None,
            "url": url,
            "access_date": str(date.today()),
            "language": "zh",
            "reliability_class": "PrimaryTextIndex" if "wikisource" in url or "ctext" in url else "Reference",
            "notes": "继承V5来源索引；具体断言仍由Citation和Evidence等级约束。",
        })
    sources = sorted(sources_by_url.values(), key=lambda item: item["source_id"])

    candidate_by_name = {str(row["家族/宗族名称"]).strip(): row for row in candidate_rows}
    relation_pairs_by_name = set()
    for row in relation_rows:
        a = str(row.get("PersonA") or "").strip()
        b = str(row.get("PersonB") or "").strip()
        if a and b:
            relation_pairs_by_name.add(tuple(sorted((a, b))))

    candidate_migrations = []
    confirmed_candidate_to_clan = {}
    branch_candidate_to_branch = {}
    for row in candidate_rows:
        candidate_id = str(row["FamilyCandidateId"]).strip()
        name = str(row["家族/宗族名称"]).strip()
        region_text = str(row.get("郡望/主要地域") or "")
        members = [item for item in split_list(row.get("关键成员（本V1）")) if item in names_to_ids]
        has_internal_relation = any(tuple(sorted((members[i], members[j]))) in relation_pairs_by_name for i in range(len(members)) for j in range(i + 1, len(members)))
        explicit_priority = "优先作为FamilyOrganization候选" in str(row.get("初始化建议") or "")
        if candidate_id == "F088":
            status = "ConfirmedBranch"
            canonical_clan_id = "clan.han.v1.f415"
            branch_id = "branch.han.v1.f415.eastern_han_mainline"
            reason = "输入明确为当朝皇帝核心主脉，属于汉室宗室内的谱系支系。"
            branch_candidate_to_branch[candidate_id] = branch_id
        elif candidate_id in MANUAL_CONFIRMED_CLANS or (explicit_priority and region_text != "待核" and len(members) >= 2 and has_internal_relation):
            status = "ConfirmedClan"
            canonical_clan_id = f"clan.han.v1.{candidate_id.lower()}"
            branch_id = None
            reason = "明确郡望/传统且存在可解析成员与亲属骨架；保守确认为HistoricalClan，不自动创建FamilyOrganization。"
            confirmed_candidate_to_clan[candidate_id] = canonical_clan_id
        else:
            status = "InsufficientEvidence"
            canonical_clan_id = None
            branch_id = None
            reason = "现有记录主要为同姓或研究聚类候选，证据不足以确认同一宗族。"
        candidate_migrations.append({
            "candidate_id": candidate_id,
            "candidate_name": name,
            "classification": status,
            "canonical_clan_id": canonical_clan_id,
            "branch_id": branch_id,
            "member_count_in_v5": len(members),
            "has_internal_kinship_evidence": has_internal_relation,
            "reason": reason,
            "migration_rule": "preserve_candidate_id_and_never_materialize_family_organization",
            "evidence_level": "B" if status != "InsufficientEvidence" else "C",
        })

    clans = []
    candidate_id_to_row = {str(row["FamilyCandidateId"]).strip(): row for row in candidate_rows}
    for candidate_id, clan_id in sorted(confirmed_candidate_to_clan.items()):
        row = candidate_id_to_row[candidate_id]
        origin = resolve_geo(row.get("郡望/主要地域"))
        members = split_list(row.get("关键成员（本V1）"))
        surname = str(row["家族/宗族名称"]).replace("氏", "")[-2:]
        clan_type = "ImperialClan" if candidate_id == "F415" else ("AristocraticClan" if "大族" in str(row.get("性质/说明") or "") or "名门" in str(row.get("性质/说明") or "") else "LocalGentryClan")
        founder_id = names_to_ids[members[0]][0] if members and len(names_to_ids.get(members[0], [])) == 1 else None
        clans.append({
            "clan_id": clan_id,
            "canonical_clan_name": row["家族/宗族名称"],
            "surname": surname,
            "clan_type": clan_type,
            "clan_commandery_region_id": origin.get("region_id"),
            "clan_county_region_id": origin.get("county_id"),
            "native_origin_description": row.get("郡望/主要地域"),
            "traditional_origin": row.get("性质/说明"),
            "earliest_known_ancestor_person_id": founder_id,
            "founder_person_id": founder_id,
            "start_year": 135,
            "end_year": None,
            "historical_status": "Established",
            "major_clan": candidate_id in MANUAL_CONFIRMED_CLANS,
            "primary_region_id": origin.get("region_id"),
            "evidence_level": "B",
            "research_status": "ConfirmedConservativeV1",
            "source_candidate_id": candidate_id,
            "notes": "由V5候选经保守分类确认；Clan不等于FamilyOrganization。",
        })
    clan_by_id = {item["clan_id"]: item for item in clans}

    branches = []
    for candidate_id, slug, branch_name, founder_name, parent_slug in MANUAL_BRANCHES:
        clan_id = confirmed_candidate_to_clan.get(candidate_id)
        if clan_id is None:
            continue
        founder_ids = names_to_ids.get(founder_name, [])
        if len(founder_ids) != 1:
            continue
        branch_id = f"branch.han.v1.{candidate_id.lower()}.{slug}"
        branches.append({
            "branch_id": branch_id,
            "clan_id": clan_id,
            "parent_branch_id": f"branch.han.v1.{candidate_id.lower()}.{parent_slug}" if parent_slug else None,
            "branch_name": branch_name,
            "founder_person_id": founder_ids[0],
            "origin_region_id": clan_by_id[clan_id]["primary_region_id"],
            "start_year": None,
            "end_year": None,
            "branch_description": "依据V5明确谱系骨架建立的保守支系；不代表运行时共同生活或共同资产。",
            "evidence_level": "B",
            "source": "Existing Historical Dataset V5",
            "notes": None,
        })
    # The former imperial-mainline candidate is a branch under the confirmed imperial clan.
    if "F415" in confirmed_candidate_to_clan and not any(item["branch_id"].endswith("eastern_han_mainline") for item in branches):
        founder_ids = names_to_ids.get("刘志", [])
        if len(founder_ids) == 1:
            branches.append({
                "branch_id": "branch.han.v1.f415.eastern_han_mainline",
                "clan_id": confirmed_candidate_to_clan["F415"],
                "parent_branch_id": None,
                "branch_name": "东汉帝系",
                "founder_person_id": founder_ids[0],
                "origin_region_id": clan_by_id[confirmed_candidate_to_clan["F415"]]["primary_region_id"],
                "start_year": 135,
                "end_year": None,
                "branch_description": "当朝皇帝核心主脉，区别于全部刘氏宗室。",
                "evidence_level": "A",
                "source": "Existing Historical Dataset V5",
                "notes": None,
            })
    branch_by_id = {item["branch_id"]: item for item in branches}

    person_candidate_id = {}
    for row in person_rows:
        candidate = candidate_by_name.get(str(row.get("家族/宗族候选") or "").strip())
        person_candidate_id[str(row["PersonId"]).strip()] = str(candidate["FamilyCandidateId"]).strip() if candidate else None

    people = []
    aliases = []
    citations = []
    unresolved_locations = []
    duplicate_name_groups = []
    for name, ids in sorted(names_to_ids.items()):
        if len(ids) > 1:
            duplicate_name_groups.append({"canonical_name": name, "person_ids": ids, "status": "SameNameDifferentPerson", "action": "NoAutomaticMerge"})
    for row in person_rows:
        person_id = str(row["PersonId"]).strip()
        name = str(row["姓名"]).strip()
        courtesy = str(row.get("字") or "").strip() or None
        anonymous = is_anonymous_name(name, courtesy)
        surname, given = parse_name(name, anonymous)
        native = resolve_geo(row.get("籍贯/主要地域"))
        if native["method"] == "Unresolved" and row.get("籍贯/主要地域"):
            unresolved_locations.append({
                "queue_id": stable_token("unresolved.location.person", person_id + str(row.get("籍贯/主要地域"))),
                "historical_location_text": row.get("籍贯/主要地域"),
                "possible_region_ids": [],
                "person_ids_affected": [person_id],
                "clan_ids_affected": [],
                "evidence": row.get("主要史料"),
                "resolution_status": "Unresolved",
            })
        candidate_id = person_candidate_id[person_id]
        clan_id = confirmed_candidate_to_clan.get(candidate_id)
        branch_id = branch_candidate_to_branch.get(candidate_id)
        source_url = str(row.get("史料入口") or "")
        source_id = sources_by_url[source_url]["source_id"] if source_url in sources_by_url else None
        tier = str(row.get("重要度") or "B").strip()
        if tier not in {"S", "A", "B", "C"}:
            tier = "B"
        person = {
            "person_id": person_id,
            "canonical_name": name,
            "surname": surname,
            "given_name": given,
            "courtesy_name": courtesy,
            "style_name": None,
            "alternate_names": [],
            "posthumous_name": None,
            "temple_name": None,
            "titles_text": None,
            "gender": "Female" if row.get("性别") == "女" else ("Male" if row.get("性别") == "男" else "Unknown"),
            "birth_year": as_int(row.get("生年")),
            "birth_year_low": as_int(row.get("生年")),
            "birth_year_high": as_int(row.get("生年")),
            "birth_date_precision": "ExactYear" if as_int(row.get("生年")) is not None else "Unknown",
            "death_year": as_int(row.get("卒年")),
            "death_year_low": as_int(row.get("卒年")),
            "death_year_high": as_int(row.get("卒年")),
            "death_date_precision": "ExactYear" if as_int(row.get("卒年")) is not None else "Unknown",
            "is_anonymous": anonymous,
            "anonymous_description": name if anonymous else None,
            "historical_person_tier": tier,
            "birth_clan_id": clan_id,
            "clan_id": clan_id,
            "lineage_branch_id": branch_id,
            "native_place_region_id": native.get("region_id"),
            "native_place_county_id": native.get("county_id"),
            "native_place_text": row.get("籍贯/主要地域"),
            "birth_location_region_id": None,
            "clan_commandery_region_id": clan_by_id[clan_id]["clan_commandery_region_id"] if clan_id else None,
            "primary_historical_region_id": native.get("region_id"),
            "father_person_id": None,
            "mother_person_id": None,
            "historical_importance": tier,
            "primary_identity": row.get("人物类别"),
            "historical_role_tags": split_list(str(row.get("人物类别") or "").replace("/", "、")),
            "primary_allegiance_text": row.get("主要政治归属/活动集团"),
            "evidence_level": evidence_from_status(row.get("校核状态")),
            "research_status": row.get("校核状态") or "Unreviewed",
            "source_id": source_id,
            "timeline_coverage_level": "T1" if (row.get("生年") or row.get("卒年") or row.get("籍贯/主要地域")) else "T0",
            "notes": row.get("家族备注"),
        }
        people.append(person)
        aliases.append({"alias_id": f"alias.{person_id}.canonical", "person_id": person_id, "name": name, "alias_type": "Canonical", "source_id": source_id, "confidence": person["evidence_level"]})
        if courtesy:
            aliases.append({"alias_id": f"alias.{person_id}.courtesy", "person_id": person_id, "name": courtesy, "alias_type": "CourtesyName", "source_id": source_id, "confidence": person["evidence_level"]})
        if source_id:
            citations.append({
                "citation_id": f"citation.person.{person_id}", "source_id": source_id,
                "page_or_section": row.get("主要史料"), "original_text_reference": None,
                "claim_type": "HistoricalPersonIdentity", "person_id": person_id,
                "clan_id": clan_id, "relation_id": None, "timeline_record_id": None,
                "researcher_note": "继承V5人物本体来源索引；具体细节仍需逐条精校。",
                "evidence_level": person["evidence_level"],
            })
    people_by_id = {item["person_id"]: item for item in people}

    kinship = []
    marriages = []
    derived_kinship = []
    relation_conflicts = []
    relation_seen = set()
    marriage_seen = set()
    unresolved_relations = []
    for index, row in enumerate(relation_rows, 1):
        name_a = str(row.get("PersonA") or "").strip()
        name_b = str(row.get("PersonB") or "").strip()
        ids_a = names_to_ids.get(name_a, [])
        ids_b = names_to_ids.get(name_b, [])
        if len(ids_a) != 1 or len(ids_b) != 1:
            unresolved_relations.append({
                "source_row": index, "person_a_text": name_a, "person_b_text": name_b,
                "relation_text": row.get("关系"), "person_a_candidates": ids_a,
                "person_b_candidates": ids_b, "status": "AmbiguousOrMissingNameResolution",
            })
            continue
        a, b = ids_a[0], ids_b[0]
        relation_text = str(row.get("关系") or "")
        note = str(row.get("备注") or "")
        evidence = str(row.get("可信度") or "B")
        if a == b:
            relation_conflicts.append({"source_row": index, "person_id": a, "type": relation_text, "status": "SelfRelation"})
            continue
        if relation_text in {"父", "母", "子女"}:
            if relation_text == "父":
                parent, child, relation_type = b, a, "AdoptiveFather" if "养父" in note else "BiologicalFather"
            elif relation_text == "母":
                parent, child, relation_type = b, a, "AdoptiveMother" if "养母" in note else "BiologicalMother"
            else:
                parent, child, relation_type = a, b, "BiologicalParentUnspecified"
            key = (parent, child, relation_type)
            if key in relation_seen:
                continue
            relation_seen.add(key)
            relation_id = f"kinship.{len(kinship)+1:05d}"
            kinship.append({
                "relation_id": relation_id, "person_a_id": parent, "person_b_id": child,
                "relation_type": relation_type, "start_year": None, "end_year": None,
                "biological": relation_type.startswith("Biological"), "adoptive": relation_type.startswith("Adoptive"),
                "legal": relation_type.startswith("Adoptive"), "evidence_level": evidence,
                "source_id": people_by_id[a].get("source_id"), "confidence": evidence,
                "notes": note or None,
            })
        elif relation_text == "配偶":
            pair = tuple(sorted((a, b)))
            if pair in marriage_seen:
                continue
            marriage_seen.add(pair)
            marriage_id = f"marriage.{len(marriages)+1:05d}"
            marriages.append({
                "marriage_id": marriage_id, "person_a_id": pair[0], "person_b_id": pair[1],
                "marriage_type": "HistoricalSpouse", "start_year": None, "end_year": None,
                "known_children": [], "political_significance": None,
                "clan_alliance_significance": None, "evidence_level": evidence,
                "source_id": people_by_id[a].get("source_id"), "notes": note or None,
            })
        elif relation_text == "兄弟姐妹":
            pair = tuple(sorted((a, b)))
            key = (pair[0], pair[1], "Sibling")
            if key in relation_seen:
                continue
            relation_seen.add(key)
            relation_id = f"kinship.{len(kinship)+1:05d}"
            kinship.append({
                "relation_id": relation_id, "person_a_id": pair[0], "person_b_id": pair[1],
                "relation_type": "Sibling", "start_year": None, "end_year": None,
                "biological": True, "adoptive": False, "legal": False,
                "evidence_level": evidence, "source_id": people_by_id[a].get("source_id"),
                "confidence": evidence, "notes": note or None,
            })
            derived_kinship.append({"relation_id": relation_id, "status": "SourceExplicitSiblingRetained", "reason": "V5父母数据不足以完全推导；保留明确史料关系并标记为可复核。"})

    # Populate direct parent pointers only when there is one unambiguous parent of that type.
    father_candidates = defaultdict(list)
    mother_candidates = defaultdict(list)
    for relation in kinship:
        if relation["relation_type"] in {"BiologicalFather", "AdoptiveFather"}:
            father_candidates[relation["person_b_id"]].append(relation["person_a_id"])
        if relation["relation_type"] in {"BiologicalMother", "AdoptiveMother"}:
            mother_candidates[relation["person_b_id"]].append(relation["person_a_id"])
    for person in people:
        fathers = sorted(set(father_candidates.get(person["person_id"], [])))
        mothers = sorted(set(mother_candidates.get(person["person_id"], [])))
        if len(fathers) == 1:
            person["father_person_id"] = fathers[0]
        if len(mothers) == 1:
            person["mother_person_id"] = mothers[0]
        if len(fathers) > 1 or len(mothers) > 1:
            relation_conflicts.append({"person_id": person["person_id"], "type": "MultipleParentCandidates", "father_ids": fathers, "mother_ids": mothers, "status": "MANUAL_REVIEW_REQUIRED"})

    def timeline(sheet_name: str, id_key: str, prefix: str, field_map: dict) -> list[dict]:
        result = []
        for source in records(sheets[sheet_name]):
            person_id = str(source.get("PersonId") or "").strip()
            if person_id not in people_by_id:
                continue
            record = {
                "record_id": str(source.get(id_key) or f"{prefix}{len(result)+1:05d}"),
                "person_id": person_id,
                "start_year": as_int(source.get("开始年")), "start_month": as_int(source.get("开始月")), "start_day": None,
                "end_year": as_int(source.get("结束年")), "end_month": as_int(source.get("结束月")), "end_day": None,
                "date_precision": source.get("时间精度") or "Unknown",
                "evidence_level": source.get("置信度") or "D",
                "historical_event_id": source.get("事件锚点"),
                "source_id": sources_by_url.get(str(source.get("史料URL") or ""), {}).get("source_id"),
                "source_title": source.get("史料"),
                "source_url": source.get("史料URL"),
                "notes": source.get("备注"),
            }
            for target, source_key in field_map.items():
                record[target] = source.get(source_key)
            result.append(record)
            if record["source_id"]:
                citations.append({
                    "citation_id": f"citation.timeline.{sheet_name.lower()}.{record['record_id']}",
                    "source_id": record["source_id"], "page_or_section": record["source_title"],
                    "original_text_reference": None, "claim_type": sheet_name,
                    "person_id": person_id, "clan_id": people_by_id[person_id].get("clan_id"),
                    "relation_id": None, "timeline_record_id": record["record_id"],
                    "researcher_note": record.get("historical_event_id"), "evidence_level": record["evidence_level"],
                })
        return result

    locations = timeline("LocationTimeline", "LocationRecordId", "L", {
        "historical_location_text": "地点/活动区", "location_type": "空间类型",
        "location_reason": "状态", "confidence": "置信度",
    })
    for item in locations:
        resolved = resolve_geo(item["historical_location_text"])
        item.update({
            "region_permanent_id": resolved.get("region_id"), "county_permanent_id": resolved.get("county_id"),
            "city_id": resolved.get("city_id"), "resolution_method": resolved["method"],
            "model_fallback_location": None,
        })
        if resolved["method"] == "Unresolved":
            unresolved_locations.append({
                "queue_id": stable_token("unresolved.location.timeline", item["record_id"]),
                "historical_location_text": item["historical_location_text"], "possible_region_ids": [],
                "person_ids_affected": [item["person_id"]], "clan_ids_affected": [],
                "evidence": item.get("source_id"), "resolution_status": "Unresolved",
            })
    civil = timeline("CivilOfficeTimeline", "CivilRecordId", "C", {
        "office_definition_id": "CivilOffice", "historical_office_name": "CivilOffice",
        "variant": "PoliticalSystem", "jurisdiction_text": "Jurisdiction",
        "appointment_authority": "所属政治体系",
    })
    military = timeline("MilitaryOfficeTimeline", "MilitaryRecordId", "M", {
        "military_office_definition_id": "MilitaryOffice", "historical_office_name": "MilitaryOffice",
        "jurisdiction": "指挥范围", "command_scope": "兵力/建制说明", "political_system": "所属政治体系",
    })
    titles = timeline("TitleTimeline", "TitleRecordId", "T", {
        "title_definition_id": "TitleType", "historical_title_name": "爵位/称号",
        "title_type": "TitleType", "fief_text": "封邑/对象", "grantor": "授予者/来源",
    })
    allegiances = timeline("AllegianceTimeline", "AllegianceRecordId", "A", {
        "political_role": "PoliticalRole", "allegiance_target": "AllegianceTarget",
        "han_relation": "HanRelation", "sovereign_claim": "SovereignClaim", "polity_id": "Polity",
    })

    timeline_counts = Counter()
    for collection in (locations, civil, military, titles, allegiances):
        for item in collection:
            timeline_counts[item["person_id"]] += 1
    for person in people:
        count = timeline_counts[person["person_id"]]
        if count >= 5:
            person["timeline_coverage_level"] = "T4"
        elif count >= 2:
            person["timeline_coverage_level"] = "T3"
        elif person["clan_id"] or person["father_person_id"] or person["mother_person_id"]:
            person["timeline_coverage_level"] = "T2"

    branch_member_map = defaultdict(list)
    manual_branch_by_founder = {item["founder_person_id"]: item["branch_id"] for item in branches}
    for person in people:
        if person["lineage_branch_id"] is None and person["person_id"] in manual_branch_by_founder:
            person["lineage_branch_id"] = manual_branch_by_founder[person["person_id"]]
        if person["lineage_branch_id"]:
            branch_member_map[person["lineage_branch_id"]].append(person["person_id"])

    clan_presence = []
    for clan in clans:
        if clan["primary_region_id"]:
            clan_presence.append({
                "presence_id": f"presence.{clan['clan_id']}.origin", "clan_id": clan["clan_id"],
                "branch_id": None, "start_year": 135, "end_year": 260,
                "region_permanent_id": clan["primary_region_id"], "county_permanent_id": clan["clan_county_region_id"],
                "presence_type": "ClanCommandery", "known_member_count": sum(1 for p in people if p["clan_id"] == clan["clan_id"]),
                "major_members": [p["person_id"] for p in people if p["clan_id"] == clan["clan_id"] and p["historical_person_tier"] in {"S", "A"}][:20],
                "evidence_level": "B", "source": "Existing Historical Dataset V5 clan origin",
                "confidence": "B", "notes": "郡望/本籍Presence，不等于运行时地产或FamilyOrganization。",
            })

    # Basic integrity audits.
    parent_edges = [(item["person_a_id"], item["person_b_id"]) for item in kinship if item["relation_type"] in {"BiologicalFather", "BiologicalMother", "AdoptiveFather", "AdoptiveMother", "BiologicalParentUnspecified"}]
    adjacency = defaultdict(list)
    for parent, child in parent_edges:
        adjacency[parent].append(child)
    cycles = []
    visiting, visited = set(), set()
    def visit(node, stack):
        if node in visiting:
            cycles.append(stack[stack.index(node):] + [node])
            return
        if node in visited:
            return
        visiting.add(node)
        for child in adjacency.get(node, []):
            visit(child, stack + [child])
        visiting.remove(node)
        visited.add(node)
    for person_id in people_by_id:
        visit(person_id, [person_id])

    post_death_timeline = []
    for collection_name, collection in (("Location", locations), ("CivilOffice", civil), ("MilitaryOffice", military), ("Title", titles), ("Allegiance", allegiances)):
        for item in collection:
            death = people_by_id[item["person_id"]]["death_year"]
            if death is not None and item["start_year"] is not None and item["start_year"] > death:
                post_death_timeline.append({"person_id": item["person_id"], "record_id": item["record_id"], "timeline": collection_name, "death_year": death, "start_year": item["start_year"], "status": "Warning"})

    location_conflicts = []
    by_person = defaultdict(list)
    for item in locations:
        by_person[item["person_id"]].append(item)
    for person_id, items in by_person.items():
        for i in range(len(items)):
            for j in range(i + 1, len(items)):
                a, b = items[i], items[j]
                if a["region_permanent_id"] and b["region_permanent_id"] and a["region_permanent_id"] != b["region_permanent_id"]:
                    start = max(a["start_year"] or 135, b["start_year"] or 135)
                    end = min(a["end_year"] or 260, b["end_year"] or 260)
                    if start <= end:
                        location_conflicts.append({"person_id": person_id, "record_a": a["record_id"], "record_b": b["record_id"], "overlap_years": [start, end], "status": "MANUAL_REVIEW_REQUIRED"})

    runtime = root / "Assets/StreamingAssets/HistoricalPersons/Han135260V1"
    runtime.mkdir(parents=True, exist_ok=True)
    scenario_dir = runtime / "scenarios"
    scenario_dir.mkdir(parents=True, exist_ok=True)

    scenario_index = []
    scenario_counts = {}
    for scenario_id, year, scenario_name in SCENARIOS:
        person_snapshots = []
        for person in people:
            state = life_state(person, year)
            if state in {"NotBorn", "Dead"}:
                continue
            person_locations = [item for item in locations if item["person_id"] == person["person_id"] and contains_year(item, year)]
            current_location = person_locations[0] if len(person_locations) == 1 else None
            current_civil = [item for item in civil if item["person_id"] == person["person_id"] and contains_year(item, year)]
            current_military = [item for item in military if item["person_id"] == person["person_id"] and contains_year(item, year)]
            current_titles = [item for item in titles if item["person_id"] == person["person_id"] and contains_year(item, year)]
            current_allegiance = [item for item in allegiances if item["person_id"] == person["person_id"] and contains_year(item, year)]
            person_snapshots.append({
                "person_id": person["person_id"], "alive_state": state,
                "current_location_record_id": current_location["record_id"] if current_location else None,
                "current_region_id": current_location["region_permanent_id"] if current_location else None,
                "current_county_id": current_location["county_permanent_id"] if current_location else None,
                "current_city_id": current_location["city_id"] if current_location else None,
                "current_civil_office_record_ids": [item["record_id"] for item in current_civil],
                "current_military_office_record_ids": [item["record_id"] for item in current_military],
                "current_title_record_ids": [item["record_id"] for item in current_titles],
                "current_allegiance_record_ids": [item["record_id"] for item in current_allegiance],
                "clan_id": person["clan_id"], "branch_id": person["lineage_branch_id"],
                "historical_role": person["primary_identity"], "confidence": person["evidence_level"],
                "location_conflict": len(person_locations) > 1,
            })
        clan_snapshots = []
        living_ids = {item["person_id"] for item in person_snapshots}
        for clan in clans:
            members = [p["person_id"] for p in people if p["clan_id"] == clan["clan_id"] and p["person_id"] in living_ids]
            clan_snapshots.append({
                "clan_id": clan["clan_id"], "active_status": "Active" if members else "NoKnownLivingMember",
                "core_region_id": clan["primary_region_id"],
                "known_branch_ids": [b["branch_id"] for b in branches if b["clan_id"] == clan["clan_id"]],
                "known_living_member_ids": members,
                "known_regional_presence_ids": [p["presence_id"] for p in clan_presence if p["clan_id"] == clan["clan_id"] and p["start_year"] <= year <= p["end_year"]],
                "major_political_member_ids": [p["person_id"] for p in people if p["clan_id"] == clan["clan_id"] and p["person_id"] in living_ids and p["historical_person_tier"] in {"S", "A"}],
                "marriage_ids": [m["marriage_id"] for m in marriages if people_by_id[m["person_a_id"]]["clan_id"] == clan["clan_id"] or people_by_id[m["person_b_id"]]["clan_id"] == clan["clan_id"]],
                "evidence_coverage": "ConservativeV1",
            })
        payload = {
            "schema": "mandate.historical-person-clan-scenario-snapshot.v1", "scenario_id": scenario_id,
            "scenario_name": scenario_name, "year": year, "source_timeline_version": "han135260-person-clan-v1",
            "persons": person_snapshots, "clans": clan_snapshots,
        }
        scenario_path = scenario_dir / f"{year}.json"
        dump(scenario_path, payload, compact=True)
        digest = sha256_file(scenario_path)
        scenario_index.append({"scenario_id": scenario_id, "scenario_name": scenario_name, "year": year, "path": f"scenarios/{year}.json", "sha256": digest, "person_count": len(person_snapshots), "clan_count": len(clan_snapshots)})
        scenario_counts[str(year)] = len(person_snapshots)

    luoyang_path = root / "Assets/StreamingAssets/WorldMap/Luoyang184UrbanInitializationV1/historical_persons.json"
    luoyang = json.loads(luoyang_path.read_text(encoding="utf-8"))
    luoyang_mismatches = []
    for item in luoyang["people"]:
        person = people_by_id.get(item["person_id"])
        if person is None or person["canonical_name"] != item["display_name"]:
            luoyang_mismatches.append({"person_id": item["person_id"], "luoyang_name": item["display_name"], "master_name": person["canonical_name"] if person else None})

    classification_counts = Counter(item["classification"] for item in candidate_migrations)
    tier_counts = Counter(item["historical_person_tier"] for item in people)
    gender_counts = Counter(item["gender"] for item in people)
    summary = {
        "schema": "mandate.historical-person-clan-summary.v1",
        "total_active_historical_persons": len(people), "existing_person_count": len(person_rows), "new_person_count": len(people) - len(person_rows),
        "tier_counts": dict(sorted(tier_counts.items())), "female_count": gender_counts["Female"],
        "anonymous_historical_count": sum(1 for item in people if item["is_anonymous"]),
        "confirmed_clan_count": len(clans), "confirmed_branch_count": len(branches),
        "candidate_classification_counts": dict(sorted(classification_counts.items())),
        "source_relation_row_count": len(relation_rows), "kinship_relation_count": len(kinship),
        "marriage_relation_count": len(marriages),
        "known_father_count": sum(1 for item in people if item["father_person_id"]),
        "known_mother_count": sum(1 for item in people if item["mother_person_id"]),
        "known_spouse_person_count": len({person_id for item in marriages for person_id in (item["person_a_id"], item["person_b_id"])}),
        "known_parent_person_count": len({item["person_a_id"] for item in kinship if item["relation_type"] in {"BiologicalFather", "BiologicalMother", "AdoptiveFather", "AdoptiveMother", "BiologicalParentUnspecified"}}),
        "location_timeline_count": len(locations), "civil_office_timeline_count": len(civil),
        "military_office_timeline_count": len(military), "title_timeline_count": len(titles),
        "allegiance_timeline_count": len(allegiances), "clan_presence_count": len(clan_presence),
        "source_count": len(sources), "citation_count": len(citations),
        "dispute_count": len(relation_conflicts) + len(location_conflicts) + len(duplicate_name_groups),
        "unresolved_location_count": len(unresolved_locations), "unresolved_relation_count": len(unresolved_relations),
        "scenario_person_counts": scenario_counts,
        "luoyang_184_named_historical_person_count": len(luoyang["people"]),
        "luoyang_regression_mismatch_count": len(luoyang_mismatches),
        "family_organizations_generated": 0, "households_generated": 0, "family_assets_generated": 0,
        "ancestor_cycle_count": len(cycles), "post_death_timeline_warning_count": len(post_death_timeline),
        "location_conflict_count": len(location_conflicts), "person_id_preserved_count": len(people),
        "safe_for_family_organization_next_stage": len(cycles) == 0 and len(luoyang_mismatches) == 0,
    }

    datasets = {
        "persons.json": {"schema": "mandate.historical-person-master.v1", "persons": people},
        "person_aliases.json": {"schema": "mandate.historical-person-aliases.v1", "aliases": aliases},
        "clans.json": {"schema": "mandate.historical-clan-master.v1", "clans": clans},
        "branches.json": {"schema": "mandate.historical-lineage-branch-master.v1", "branches": branches, "branch_members": dict(branch_member_map)},
        "kinship.json": {"schema": "mandate.historical-kinship.v1", "relations": kinship, "derived_kinship_audit": derived_kinship},
        "marriages.json": {"schema": "mandate.historical-marriage.v1", "marriages": marriages},
        "person_locations.json": {"schema": "mandate.historical-person-location-timeline.v1", "records": locations},
        "civil_offices.json": {"schema": "mandate.historical-civil-office-timeline.v1", "records": civil},
        "military_offices.json": {"schema": "mandate.historical-military-office-timeline.v1", "records": military},
        "titles.json": {"schema": "mandate.historical-title-timeline.v1", "records": titles},
        "allegiances.json": {"schema": "mandate.historical-allegiance-timeline.v1", "records": allegiances},
        "clan_presence.json": {"schema": "mandate.historical-clan-geographic-presence.v1", "records": clan_presence},
        "sources.json": {"schema": "mandate.historical-source-master.v1", "sources": sources},
        "citations.json": {"schema": "mandate.historical-source-citations.v1", "citations": citations},
        "audits.json": {
            "schema": "mandate.historical-person-clan-audits.v1", "candidate_migrations": candidate_migrations,
            "person_identity_merge_audit": [], "same_name_different_person": duplicate_name_groups,
            "unresolved_relations": unresolved_relations, "relation_conflicts": relation_conflicts,
            "unresolved_locations": unresolved_locations, "location_conflicts": location_conflicts,
            "ancestor_cycles": cycles, "post_death_timeline_warnings": post_death_timeline,
            "luoyang_regression_mismatches": luoyang_mismatches,
        },
        "scenario_index.json": {"schema": "mandate.historical-person-clan-scenario-index.v1", "scenarios": scenario_index},
        "summary.json": summary,
    }
    for filename, payload in datasets.items():
        dump(runtime / filename, payload, compact=filename in {"persons.json", "citations.json"})

    immutable_files = sorted(path for path in runtime.rglob("*.json") if path.name != "manifest.json")
    manifest = {
        "schema": "mandate.historical-person-clan-package.v1", "format_version": 1,
        "dataset_id": "han135260-person-clan-v1", "year_start": 135, "year_end": 260,
        "source_baseline": "01_140-264历史人物与时间轴母库_V5.xlsx",
        "person_count": len(people), "clan_count": len(clans), "branch_count": len(branches),
        "scenario_count": len(SCENARIOS), "family_organization_count": 0, "household_count": 0,
        "files": [{"path": path.relative_to(runtime).as_posix(), "bytes": path.stat().st_size, "sha256": sha256_file(path)} for path in immutable_files],
    }
    dump(runtime / "manifest.json", manifest)

    for path in sorted(runtime.rglob("*.json")):
        write_meta(path, root)
    for directory in (runtime, scenario_dir, runtime.parent, runtime.parent.parent):
        if directory != root and not directory.with_name(directory.name + ".meta").exists():
            write_meta(directory, root)

    output_data = root / "outputs/HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1/data"
    output_data.mkdir(parents=True, exist_ok=True)
    for filename, payload in datasets.items():
        dump(output_data / filename, payload, compact=False)
    dump(output_data / "manifest.json", manifest)
    dump(root / "outputs/HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1/validation_summary.json", summary)
    performance = {"elapsed_ms": round((time.perf_counter() - started) * 1000, 3), "person_count": len(people), "scenario_count": len(SCENARIOS), "runtime_bytes": sum(path.stat().st_size for path in runtime.rglob("*.json"))}
    dump(root / "outputs/HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1/performance_report.json", performance)
    print(json.dumps({"status": "PASS", **summary, **performance}, ensure_ascii=False))


if __name__ == "__main__":
    main()
