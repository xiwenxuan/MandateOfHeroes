from __future__ import annotations

import json
import hashlib
from collections import defaultdict
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "outputs" / "FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1"
DOC = REPO / "Docs" / "FAMILY_ORGANIZATION_REFERENCE_V1"
HIST = REPO / "Assets" / "StreamingAssets" / "HistoricalPersons" / "Han135260V1"
LUOYANG = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184UrbanInitializationV1"
DEEPENING = REPO / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1" / "deepening_workdata.json"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def pipe(values):
    return "|".join(str(value) for value in values if value not in (None, ""))


def write(path: Path, text: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


clans = load(HIST / "clans.json")["clans"]
branches = load(HIST / "branches.json")["branches"]
persons = load(HIST / "persons.json")["persons"]
locations = load(HIST / "person_locations.json")["records"]
urban_people = load(LUOYANG / "historical_persons.json")["people"]
urban_orgs = load(LUOYANG / "family_organizations.json")["organizations"]
deepening = load(DEEPENING)
estates = deepening["estate_references"]

person_by_id = {row["person_id"]: row for row in persons}
clan_by_id = {row["clan_id"]: row for row in clans}
branch_by_id = {row["branch_id"]: row for row in branches}
branches_by_clan = defaultdict(list)
persons_by_clan = defaultdict(list)
for row in branches:
    branches_by_clan[row["clan_id"]].append(row)
for row in persons:
    if row.get("clan_id"):
        persons_by_clan[row["clan_id"]].append(row)

scenario_years = [140, 184, 189, 194, 200, 207, 214, 219, 223, 227, 234, 249, 260]
scenarios = {year: load(HIST / "scenarios" / f"{year}.json") for year in scenario_years}


action_matrix = [
    ("个人购买土地/住宅", "YES", "N/A", "NO", "NO", "N/A", "YES", "NO", "属于Person私人行为；不得自动转为族产。"),
    ("个人修建私人住宅", "YES", "N/A", "NO", "NO", "N/A", "YES", "NO", "受土地权、建设许可、工料和工时约束。"),
    ("家族成员迁居", "YES", "YES_LIMITED", "NO", "NO", "YES", "NO", "NO", "个人可自行迁居；组织资助迁居需真实预算与知情关系。"),
    ("家族组织购买本地资产", "N/A", "NO", "YES", "NO", "NO", "YES", "YES", "必须由本地中心、管理者和组织预算共同授权。"),
    ("家族组织出售本地资产", "N/A", "NO", "YES", "NO", "NO", "YES", "YES", "资产权属和处置权限必须可追溯。"),
    ("家族组织新建/改建本地设施", "N/A", "NO", "YES", "NO", "NO", "YES", "YES", "中心只提供管理能力，设施仍需土地、材料、劳工和时间。"),
    ("家族仓库跨地调拨", "N/A", "NO", "YES", "NO", "YES_LIMITED", "YES", "YES", "命令与账簿需传递，货物需真实运输；REMOTE只能提出有限请求。"),
    ("支用地方预算", "N/A", "NO", "YES", "NO", "YES_LIMITED", "YES", "YES", "地方预算在Local Center结算；越权支出需Primary批准并等待通信。"),
    ("支用全族总库", "N/A", "NO", "NO", "YES", "YES_LIMITED", "YES", "YES", "Primary保管组织根账；远程命令不等于即时到账。"),
    ("任命本地管事", "N/A", "NO", "YES", "NO", "YES_LIMITED", "YES", "YES", "任命对象必须是真实Person并可赴任。"),
    ("任命家主/最高管理者", "N/A", "NO", "NO", "YES", "YES_LIMITED", "YES", "YES", "继承、选立或授权流程另有合法性合同。"),
    ("向本地成员提供组织救助", "YES", "YES_LIMITED", "YES", "NO", "YES_LIMITED", "YES", "YES", "无中心时仍可私人互助；组织性常态救助需本地中心。"),
    ("招募门客/部曲/雇员", "YES_LIMITED", "NO", "YES", "NO", "NO", "YES", "YES", "私人雇佣与组织招募分账；不得凭空生成从属者。"),
    ("组织私军设施建设", "NO", "NO", "YES", "NO", "NO", "YES", "YES", "还需政治许可、兵源、装备和军事权限；中心本身不授予私军合法性。"),
    ("建立LocalFamilyCenter", "NO", "NO", "NO", "YES", "YES_LIMITED", "YES", "YES", "先取得可用Facility，再由Primary正式指定；每管理区最多一个。"),
    ("撤销LocalFamilyCenter", "NO", "NO", "NO", "YES", "YES_LIMITED", "YES", "YES", "撤销不删除当地人物、家户或资产。"),
    ("迁移PrimaryFamilyCenter", "NO", "NO", "NO", "YES", "YES_LIMITED", "YES", "YES", "新中心先具备条件，再完成账簿、职位、库存和档案交接。"),
    ("Local提升为Primary", "NO", "NO", "NO", "YES", "YES_LIMITED", "YES", "YES", "提升后旧Primary必须明确降为Local、撤销或废弃。"),
    ("家族组织分立", "YES", "NO", "NO", "YES", "YES_LIMITED", "YES", "YES", "成员、组织资产、中心和债务逐项分配；不得平均复制。"),
    ("个人继承", "YES", "N/A", "NO", "NO", "YES", "NO", "NO", "只处理Person私人遗产，不触碰FamilyOrganization资产。"),
    ("组织职位继承/交接", "NO", "NO", "NO", "YES", "YES_LIMITED", "YES", "YES", "组织继续存在；家主死亡不使族产进入私人遗产。"),
    ("大宗跨区资产划拨", "NO", "NO", "YES", "YES", "YES_LIMITED", "YES", "YES", "两地中心、Primary授权、通信和真实运输均需满足。"),
]
action_rows = [
    {
        "Action": a,
        "PersonCanDoWithoutCenter": b,
        "FamilyOrganizationCanDoWithoutCenter": c,
        "RequiresLocalCenter": d,
        "RequiresPrimaryCenter": e,
        "CanRemoteOrder": f,
        "RequiresFacility": g,
        "RequiresManagerPerson": h,
        "Notes": i,
    }
    for a, b, c, d, e, f, g, h, i in action_matrix
]


estate_by_clan = defaultdict(list)
for estate in estates:
    if estate.get("clan_id"):
        estate_by_clan[estate["clan_id"]].append(estate)

clan_spatial_rows = []
for clan in clans:
    clan_id = clan["clan_id"]
    clan_estates = estate_by_clan.get(clan_id, [])
    origin_presence = [
        row["presence_id"]
        for row in load(HIST / "clan_presence.json")["records"]
        if row.get("clan_id") == clan_id
    ]
    has_estate = bool(clan_estates)
    clan_spatial_rows.append(
        {
            "ClanId": clan_id,
            "ClanName": clan["canonical_clan_name"],
            "ClanType": clan["clan_type"],
            "NativeCoreRegionId": clan.get("primary_region_id") or clan.get("clan_commandery_region_id"),
            "NativeCountyRegionId": clan.get("clan_county_region_id"),
            "KnownBranchIds": pipe(x["branch_id"] for x in branches_by_clan.get(clan_id, [])),
            "KnownPersonCount": len(persons_by_clan.get(clan_id, [])),
            "KnownOriginPresenceIds": pipe(origin_presence),
            "EstateReferenceIds": pipe(x["estate_reference_id"] for x in clan_estates),
            "SpatialLayerConclusion": "CLAN_ORIGIN_PLUS_ESTATE_REFERENCE" if has_estate else "CLAN_ORIGIN_PRESENCE_ONLY",
            "FamilyOrganizationConclusion": "NOT_AUTOMATICALLY_ESTABLISHED",
            "FamilyCenterConclusion": "RESEARCH_CANDIDATE_AT_ORIGIN" if has_estate else "NO_CENTER_EVIDENCE",
            "CenterEvidenceGrade": "RECONSTRUCTED_CENTER_CANDIDATE" if has_estate else "UNKNOWN",
            "EvidenceGrade": "HISTORICAL" if clan.get("evidence_level") == "A" else "RECONSTRUCTED",
            "SourceIds": "dataset.han135260.clans|dataset.han135260.clan_presence|dataset.deepening.estates",
            "Unknowns": "具体住宅、族产、组织边界、管理者与中心Facility均需逐项研究",
        }
    )


snapshot_rows = []
for year in scenario_years:
    scenario = scenarios[year]
    snap_by_clan = {row["clan_id"]: row for row in scenario.get("clans", [])}
    for clan in clans:
        snap = snap_by_clan.get(clan["clan_id"], {})
        snapshot_rows.append(
            {
                "ScenarioId": scenario["scenario_id"],
                "ScenarioName": scenario["scenario_name"],
                "Year": year,
                "ClanId": clan["clan_id"],
                "ClanName": clan["canonical_clan_name"],
                "ActiveStatus": snap.get("active_status", "UNKNOWN"),
                "CoreRegionId": snap.get("core_region_id") or clan.get("primary_region_id"),
                "KnownLivingMemberCount": len(snap.get("known_living_member_ids", [])),
                "KnownLivingMemberIds": pipe(snap.get("known_living_member_ids", [])),
                "KnownBranchIds": pipe(snap.get("known_branch_ids", [])),
                "MajorPoliticalMemberIds": pipe(snap.get("major_political_member_ids", [])),
                "ClanPresenceState": "CORE_REGION_RECORDED" if (snap.get("core_region_id") or clan.get("primary_region_id")) else "UNKNOWN",
                "FamilyOrganizationState": "NOT_DERIVED_FROM_CLAN_SNAPSHOT",
                "FamilyCenterState": "NOT_DERIVED_FROM_CLAN_SNAPSHOT",
                "EvidenceGrade": "RECONSTRUCTED",
                "InheritanceRule": "未列变化继承上一切片；人物去世不删除Clan，组织状态需独立记录",
            }
        )


scenario_id = {year: scenarios[year]["scenario_id"] for year in scenario_years}
candidate_specs = []


def add_candidates(years, clan_id, branch_id, name, kind, area, level, reason):
    candidate_key = hashlib.sha1(name.encode("utf-8")).hexdigest()[:8]
    for year in years:
        candidate_specs.append(
            {
                "ReferenceId": f"familyorg.init.{year}.{clan_id.split('.')[-1]}.{(branch_id or 'main').split('.')[-1]}.{candidate_key}",
                "ScenarioId": scenario_id[year],
                "Year": year,
                "ClanId": clan_id,
                "BranchId": branch_id,
                "CandidateName": name,
                "CandidateKind": kind,
                "ExpectedManagementAreaId": area,
                "InitializationDecision": "REFERENCE_ONLY_DO_NOT_INSTANTIATE",
                "CandidateLevel": level,
                "RequiredEvidenceBeforeMaterialization": "真实成员边界|组织资产|合法控制Facility|FamilyManagement能力|管理者Person|Primary/Local指定",
                "Reason": reason,
            }
        )


add_candidates([140, 184, 189, 194, 200, 207, 214, 219], "clan.han.v1.f415", "branch.han.v1.f415.eastern_han_mainline", "东汉皇室核心组织候选", "SPECIAL_IMPERIAL_HOUSEHOLD", "city.han.洛阳", "RECONSTRUCTED", "皇室核心家庭不是全体刘氏宗室；宫殿仍属国家/宫廷资产，不能自动当作族产。")
add_candidates([184], "clan.han.v1.f036", "", "南阳何氏洛阳政治家庭组织候选", "LOCAL_CAPITAL_CANDIDATE", "city.han.洛阳", "RECONSTRUCTED", "何进与何皇后在京可证，但组织、族产和中心Facility尚未证实。")
add_candidates([140, 184, 189], "clan.han.v1.f077", "", "弘农杨氏本籍组织候选", "PRIMARY_ORIGIN_CANDIDATE", "admin.han140.sili.hongnong", "RECONSTRUCTED", "本籍与重要成员明确，具体组织资产和中心待研究。")
add_candidates([184, 189], "clan.han.v1.f077", "", "弘农杨氏洛阳地方管理候选", "LOCAL_CAPITAL_CANDIDATE", "city.han.洛阳", "MODELED", "中央任官只证明成员存在；须另证宅第、族产与实际管理核心。")
add_candidates([140, 184, 189], "clan.han.v1.f092", "branch.han.v1.f092.yuan_wei", "汝南袁氏袁隗支组织候选", "PRIMARY_OR_BRANCH_CANDIDATE", "admin.han140.yuzhou.runan.ruyang", "RECONSTRUCTED", "Clan可产生多个FamilyOrganization；袁隗支与袁逢支不可强行合并。")
add_candidates([189, 194, 200], "clan.han.v1.f092", "branch.han.v1.f092.yuan_feng", "袁绍政治家族组织候选", "SEPARATE_BRANCH_ORGANIZATION", "admin.han140.jizhou", "RECONSTRUCTED", "政治分立后可形成独立组织；不得复制原族产。")
add_candidates([189, 194, 200], "clan.han.v1.f092", "branch.han.v1.f092.yuan_feng", "袁术政治家族组织候选", "SEPARATE_BRANCH_ORGANIZATION", "admin.han140.yuzhou", "RECONSTRUCTED", "与袁绍组织须独立建模，成员、资产和中心逐项分配。")
add_candidates([194, 200, 207, 214, 219], "clan.han.v1.f133", "branch.han.v1.f133.cao_song", "曹操核心家族组织候选", "POLITICAL_DYNASTY_ORGANIZATION", "admin.han140.yanzhou", "RECONSTRUCTED", "曹氏Clan不等于曹操政权或全部夏侯氏；组织边界需独立。")
add_candidates([194, 200, 207, 214, 219, 223], "clan.han.v1.f045", "branch.han.v1.f045.sun_jian", "孙氏核心家族组织候选", "POLITICAL_DYNASTY_ORGANIZATION", "admin.han140.yangzhou.wu", "RECONSTRUCTED", "孙氏三个Branch不可只按姓氏合并；中心随实际政治核心迁移。")
add_candidates([194, 200, 207, 214, 219, 223, 227, 234], "clan.han.v1.f126", "", "刘备—蜀汉皇室组织候选", "POLITICAL_DYNASTY_ORGANIZATION", "admin.han140.yizhou", "RECONSTRUCTED", "宗室身份与实际皇室核心组织分离；成员和资产按剧本形成。")
add_candidates([223, 227, 234, 249, 260], "clan.han.v1.f102", "branch.han.v1.f102.sima_yi", "河内司马氏司马懿支组织候选", "BRANCH_ORGANIZATION", "admin.han140.sili.henei.wen", "RECONSTRUCTED", "一Clan多Branch、多组织并存；249后政治权力不自动等于全部族产。")
add_candidates([223, 227, 234, 249, 260], "clan.han.v1.f102", "branch.han.v1.f102.sima_fu", "河内司马氏司马孚支组织候选", "BRANCH_ORGANIZATION", "admin.han140.sili.henei.wen", "MODELED", "与司马懿支独立保留，是否形成正式组织待人物与资产证据。")


asset_rows = []
for estate in estates:
    level = "RECONSTRUCTED_CENTER_CANDIDATE" if estate["evidence_level"] in ("HISTORICAL", "RECONSTRUCTED") else "MODELED_CENTER_CANDIDATE"
    asset_rows.append(
        {
            "ReferenceId": estate["estate_reference_id"],
            "ReferenceKind": "ESTATE_EVIDENCE",
            "PersonId": estate.get("historical_person_ids"),
            "ClanId": estate.get("clan_id"),
            "BranchId": estate.get("branch_id"),
            "LocationScopeId": estate.get("county_id"),
            "FacilityId": "",
            "AssetOwnerId": "UNKNOWN",
            "EvidenceDescription": estate["historical_description"],
            "EvidenceGrade": estate["evidence_level"],
            "CanHostFamilyCenter": "CONDITIONAL_IF_REAL_FACILITY_AND_CAPABILITY",
            "HistoricalManagementEvidence": estate.get("retainer_evidence") or "UNKNOWN",
            "CenterCandidateLevel": level,
            "SourceId": estate["source_id"],
            "Unknowns": estate["unknowns"],
        }
    )
for row in urban_people:
    p = person_by_id.get(row["person_id"], {})
    asset_rows.append(
        {
            "ReferenceId": f"residence.luoyang184.{row['person_id']}",
            "ReferenceKind": "RESIDENCE_OR_PRESENCE_EVIDENCE",
            "PersonId": row["person_id"],
            "ClanId": p.get("clan_id", ""),
            "BranchId": p.get("branch_id", ""),
            "LocationScopeId": "city.han.洛阳",
            "FacilityId": "",
            "AssetOwnerId": row["person_id"],
            "EvidenceDescription": f"{row['location_status']}；{row.get('historical_role') or '角色待校'}",
            "EvidenceGrade": "HISTORICAL" if row["confidence"] == "A" else "RECONSTRUCTED",
            "CanHostFamilyCenter": "NO_INFERENCE_FROM_PERSON_PRESENCE",
            "HistoricalManagementEvidence": "NONE_FROM_THIS_RECORD",
            "CenterCandidateLevel": "NOT_CENTER_EVIDENCE",
            "SourceId": row.get("source", ""),
            "Unknowns": "具体住宅、Facility、产权与同住家户未知",
        }
    )
for org in urban_orgs:
    asset_rows.append(
        {
            "ReferenceId": f"assetclaim.{org['family_organization_id']}",
            "ReferenceKind": "MODELED_FAMILY_ASSET_CLAIM",
            "PersonId": org["head_person_id"],
            "ClanId": f"clan.han.v1.{org['source_family_id'].lower()}",
            "BranchId": "",
            "LocationScopeId": "city.han.洛阳",
            "FacilityId": pipe(org.get("family_facility_ids", [])),
            "AssetOwnerId": org["family_organization_id"],
            "EvidenceDescription": f"模型资产={org['family_assets']}；财库={org['family_treasury']}；Cell={pipe(org.get('family_cells', []))}",
            "EvidenceGrade": "MODELED",
            "CanHostFamilyCenter": "NO_WITHOUT_FACILITY",
            "HistoricalManagementEvidence": "NONE",
            "CenterCandidateLevel": "INSUFFICIENT_NO_FACILITY",
            "SourceId": "dataset.luoyang184.family_organizations",
            "Unknowns": "产权来源、真实设施、管理者、Primary/Local指定均缺失",
        }
    )


status_map = {
    "ConfirmedInLuoyang": "CONFIRMED_LUOYANG",
    "LikelyInLuoyang": "PROBABLE_LUOYANG",
    "TemporaryInLuoyang": "CONFIRMED_LUOYANG",
    "DepartingFromLuoyang": "CONFIRMED_LUOYANG",
}


def spatial_category(row):
    role = row.get("historical_role") or ""
    if row["person_id"] in {"P0037", "P0038", "P0039", "P0040", "P0047", "P0048", "P0049", "P0050", "P0929", "P0932", "P0933"}:
        return "Palace"
    if row["location_status"] in {"TemporaryInLuoyang", "DepartingFromLuoyang"}:
        return "TemporaryPresence"
    if any(word in role for word in ("将军", "中郎将", "骑都尉", "军事")):
        return "Military"
    return "CentralGovernment"


luoyang_person_rows = []
for row in urban_people:
    p = person_by_id.get(row["person_id"], {})
    luoyang_person_rows.append(
        {
            "PersonId": row["person_id"],
            "DisplayName": row["display_name"],
            "ReviewSet": "EXISTING_25_BASELINE",
            "Luoyang184Status": status_map.get(row["location_status"], "UNKNOWN"),
            "PresenceWindow": row["location_status"],
            "SpatialCategory": spatial_category(row),
            "ExactFacilityId": "",
            "ClanId": p.get("clan_id", ""),
            "BranchId": p.get("branch_id", ""),
            "ExistingFamilyAnchor": row.get("family_anchor", ""),
            "FamilySpatialConclusion": "MEMBER_PRESENCE_ONLY_NOT_CENTER_EVIDENCE",
            "HistoricalRole": row.get("historical_role", ""),
            "EvidenceGrade": "HISTORICAL" if row["confidence"] == "A" else "RECONSTRUCTED",
            "Source": row.get("source", ""),
            "Notes": "只定位到洛阳/离京窗口；不得分配精确Facility Cell。",
        }
    )

additional = [
    ("P0011", "PROBABLE_LUOYANG", "CentralGovernment", "弘农杨氏重要成员，184任职与具体住所需专项校核。"),
    ("P0080", "POSSIBLE_LUOYANG", "CentralGovernment", "袁绍早期中央政治活动可作候选，184全年位置未达确认。"),
    ("P0081", "POSSIBLE_LUOYANG", "CentralGovernment", "袁术在京关系可研究，不能由汝南袁氏或后续势力反推184住所。"),
    ("P0016", "NOT_LUOYANG", "TemporaryPresence", "现有位置记录指向州域活动；保留为排除/复核样本。"),
    ("P0017", "NOT_LUOYANG", "UnknownLocation", "现有位置记录为地方；不得因名士身份放入洛阳。"),
    ("P0064", "NOT_LUOYANG", "Military", "184主要为外地军事活动候选，不列入洛阳常住。"),
    ("P0107", "POSSIBLE_LUOYANG", "UrbanResidence", "曹嵩财富与中央关系值得研究，但本轮无精确住宅证据。"),
]
for pid, status, category, notes in additional:
    p = person_by_id[pid]
    luoyang_person_rows.append(
        {
            "PersonId": pid,
            "DisplayName": p["canonical_name"],
            "ReviewSet": "ADDITIONAL_RESEARCH_CANDIDATE",
            "Luoyang184Status": status,
            "PresenceWindow": "YEAR_184_REVIEW",
            "SpatialCategory": category,
            "ExactFacilityId": "",
            "ClanId": p.get("clan_id", ""),
            "BranchId": p.get("branch_id", ""),
            "ExistingFamilyAnchor": "",
            "FamilySpatialConclusion": "NO_ORGANIZATION_OR_CENTER_INFERENCE",
            "HistoricalRole": scenarios[184]["persons"][[x["person_id"] for x in scenarios[184]["persons"]].index(pid)]["historical_role"] if pid in [x["person_id"] for x in scenarios[184]["persons"]] else "",
            "EvidenceGrade": "RECONSTRUCTED" if status == "PROBABLE_LUOYANG" else "UNKNOWN",
            "Source": "dataset.han135260.persons|dataset.han135260.person_locations",
            "Notes": notes,
        }
    )


def expected_clan(org):
    cid = f"clan.han.v1.{org['source_family_id'].lower()}"
    return cid if cid in clan_by_id else ""


audit_rows = []
for org in urban_orgs:
    expected = expected_clan(org)
    historical = org.get("historical_member_person_ids", [])
    matching = [pid for pid in historical if person_by_id.get(pid, {}).get("clan_id") == expected] if expected else []
    unrelated = [pid for pid in historical if expected and person_by_id.get(pid, {}).get("clan_id") != expected]
    if org["source_family_id"] == "F088":
        issue = "CRITICAL_MIXED_IMPERIAL_AND_EUNUCH_MEMBERS"
        severity = "S1"
    elif org["source_family_id"] == "F036":
        issue = "CRITICAL_RANGE_DERIVATION_MISASSIGNED_MEMBERS"
        severity = "S1"
    elif not expected:
        issue = "UNRESOLVED_NONCANONICAL_FAMILY_ID"
        severity = "S2"
    else:
        issue = "NO_FACILITY_OR_CENTER_DESIGNATION"
        severity = "S2"
    audit_rows.append(
        {
            "FamilyOrganizationId": org["family_organization_id"],
            "SourceFamilyId": org["source_family_id"],
            "DisplayName": org["family_name"],
            "HeadPersonId": org["head_person_id"],
            "MemberCount": org["member_count"],
            "HistoricalMemberIds": pipe(historical),
            "ExpectedClanId": expected,
            "MatchingHistoricalMemberIds": pipe(matching),
            "UnrelatedOrUnresolvedHistoricalMemberIds": pipe(unrelated),
            "FamilyCellIds": pipe(org.get("family_cells", [])),
            "FamilyFacilityIds": pipe(org.get("family_facility_ids", [])),
            "HasRealFamilyManagementFacility": "NO",
            "CenterStatus": "NONE",
            "PrimaryOrLocal": "UNASSIGNED",
            "IssueSeverity": severity,
            "AuditConclusion": issue,
            "SafeFollowup": "保留V1运行时原记录；新版本迁移时逐人核对成员、建立真实Facility与指定记录，禁止按序号区间继承历史成员。",
        }
    )


center_rows = [
    ("family_organization.luoyang.184.f088", "汉室主脉", "SPECIAL_IMPERIAL_HOUSEHOLD_RESEARCH", "Palace", "RECONSTRUCTED_CENTER_CANDIDATE", "不指定", "宫殿是国家/宫廷设施，只有独立皇室组织与资产合同完成后才能指定特殊Primary。"),
    ("family_organization.luoyang.184.f036", "南阳何氏", "LOCAL_FAMILY_CENTER_RESEARCH", "UrbanResidence|Estate", "RECONSTRUCTED_CENTER_CANDIDATE", "Local候选", "何氏在京活动可证，但无宅第/族产/管事Facility证据；不得用任官地点代替。"),
    ("family_organization.luoyang.184.f077", "弘农杨氏", "LOCAL_FAMILY_CENTER_RESEARCH", "UrbanResidence", "MODELED_CENTER_CANDIDATE", "Local候选", "Primary更可能研究弘农本籍；洛阳只按任官成员存在，须另证组织管理。"),
    ("family_organization.luoyang.184.f092", "汝南袁氏", "BRANCH_LOCAL_CENTER_RESEARCH", "UrbanResidence", "RECONSTRUCTED_CENTER_CANDIDATE", "Local候选", "先区分袁隗支与袁逢支；不可由汝南袁氏总称直接建立一个洛阳中心。"),
    ("family_organization.luoyang.184.f081", "扶风马氏", "MEMBER_PRESENCE_ONLY", "CentralGovernment", "UNKNOWN", "不指定", "马日磾任官不证明扶风马氏在洛阳有组织、族产或中心。"),
    ("family_organization.luoyang.184.f571", "董氏（灵帝外戚）", "UNRESOLVED_MODELED_ORGANIZATION", "UnknownLocation", "UNKNOWN", "不指定", "家主为程序生成人物且无历史成员；先解决身份和组织来源。"),
    ("family_organization.luoyang.184.f572", "董氏（灵帝母族）", "IMPERIAL_ELDER_HOUSEHOLD_RESEARCH", "Palace|UrbanResidence", "UNKNOWN", "不指定", "董太后在京不等于董氏宗族组织或中心；可能属于皇室/后宫特殊家庭合同。"),
    ("candidate.luoyang184.cao", "谯县曹氏", "TEMPORARY_MEMBER_PRESENCE_ONLY", "TemporaryPresence", "UNKNOWN", "不指定", "曹操离京赴战只证明个人行动；曹氏洛阳FamilyCenter无证据。"),
    ("candidate.luoyang184.familyhall", "标准FamilyHall内容定义", "FUTURE_CONTENT_DEFINITION", "AnyLegalFacility", "MODELED_CENTER_CANDIDATE", "非实例", "允许建立标准候选BaseType，但中心仍由FamilyManagement能力、产权、管理者和正式指定共同成立。"),
    ("candidate.luoyang184.estate", "洛阳近郊家族庄园", "FUTURE_ESTATE_COMPLEX_CANDIDATE", "Estate", "MODELED_CENTER_CANDIDATE", "Primary或Local候选", "庄园只有形成真实Facility且具备FamilyManagement能力时才能承载中心；田地本身不是中心。"),
]
center_candidate_rows = [
    {
        "CandidateId": a,
        "RelatedName": b,
        "CandidateConclusion": c,
        "SpatialCategory": d,
        "CenterCandidateLevel": e,
        "DesignationRecommendation184": f,
        "ExistingFacilityId": "",
        "ManagementAreaId": "city.han.洛阳" if "estate" not in a else "estate_complex.luoyang184.unknown",
        "RequiredBeforeDesignation": "真实Facility|FamilyManagement能力|合法组织产权/控制|真实管理者Person|Primary/Local正式指定",
        "DevelopmentAdvice": g,
    }
    for a, b, c, d, e, f, g in center_rows
]


workdata = {
    "action_matrix": action_rows,
    "clan_spatial": clan_spatial_rows,
    "scenario_snapshots": snapshot_rows,
    "initialization_reference": candidate_specs,
    "residence_estate_assets": asset_rows,
    "luoyang_people": luoyang_person_rows,
    "luoyang_org_audit": audit_rows,
    "luoyang_center_candidates": center_candidate_rows,
}
OUT.mkdir(parents=True, exist_ok=True)
DOC.mkdir(parents=True, exist_ok=True)
(OUT / "family_reference_workdata.json").write_text(json.dumps(workdata, ensure_ascii=False, indent=2), encoding="utf-8")


relation_doc = """# FamilyOrganization、Clan、Branch、Household与FamilyCenter关系规范 V1

## 1. 冻结结论

本规范冻结五类实体，不允许按中文日常用语混用：

| 实体 | 含义 | 可否直接拥有组织资产 | 可否自动产生FamilyCenter |
|---|---|---:|---:|
| `Person` | 永久人物、私人权利与私人资产主体 | 仅私人资产 | 否 |
| `Household` | 共同居住、消费、照护和日常财产的生活家户 | 家户共同资产 | 否 |
| `Clan` | 历史宗族、姓族与谱系认同的长期历史实体 | 否，除非另建组织产权记录 | 否 |
| `Branch` | Clan内部谱系分支；不是管理机构 | 否 | 否 |
| `FamilyOrganization` | 拥有族产、职位、账簿、产业、档案或私军的组织主体 | 是 | 否，仍需真实Facility与指定 |
| `FamilyCenter` | FamilyOrganization指定的正式管理中心状态 | 它不是独立所有者 | 不适用 |

最高原则：家族成员可以在没有FamilyCenter的城市正常居住、任官、经商、买地和发展；FamilyCenter限制的是FamilyOrganization在当地的正式组织管理能力，而不是族人的存在与发展能力。

## 2. 分离规则

1. 一个Clan可以没有FamilyOrganization，也可以在不同年代形成多个FamilyOrganization。
2. 一个Branch可以跨多个家户；一个家户也可包含姻亲、仆役或不同Clan成员。
3. 同姓、同Clan、本籍相同、同城任官、共同住宅或拥有土地，都不能单独证明FamilyOrganization存在。
4. 成员加入组织不自动把私人资产变成族产；组织资产也不因家主死亡进入私人遗产。
5. `FamilyCenter`属于FamilyOrganization，绝不属于Clan或Branch。
6. 历史空间必须分层记录：`ClanPresence`、`BranchPresence`、`MemberPresence`、`ResidenceEvidence`、`EstateEvidence`、`FamilyAssetEvidence`、`FamilyCenterEvidence`。
7. 史料人物在洛阳只证明人物在洛阳；没有住宅证据时不得分配精确Facility或Cell。

## 3. 初始化合同

`FamilyOrganizationInitializationReference`只是剧本候选桥梁：

```text
Scenario + Clan + optional Branch
    -> candidate FamilyOrganization boundary
    -> evidence review
    -> members/assets/authority/facility/manager materialization
```

它不得执行“39个Clan生成39个FamilyOrganization”，也不得把同Clan的竞争政治集团静默合并。真正物化至少需要：明确成员边界、组织资产、合法权力来源、可追溯账簿，以及（若要建立中心）真实Facility、`FamilyManagement`能力、组织产权/控制、管理者Person与正式指定。

## 4. 证据等级

- `HISTORICAL`：史料直接支持该层事实。
- `RECONSTRUCTED`：多条史料共同支持的保守复原。
- `MODELED`：为玩法或数据完整性建立的项目模型。
- `UNKNOWN`：不能确定，禁止静默补全。

FamilyCenter专用等级：`HISTORICAL_CENTER_EVIDENCE`、`RECONSTRUCTED_CENTER_CANDIDATE`、`MODELED_CENTER_CANDIDATE`、`UNKNOWN`。庄园、宅第或祠堂证据不得自动升级为中心证据。

## 5. 存档与迁移

新增普通宗族、Branch、设施类型或中心候选必须使用稳定命名空间ID和数据定义。旧存档里的成员、家户、资产与中心状态必须顺序迁移；缺失ID保留原引用并报告，禁止重新随机、合并或删除永久人物。
"""


center_doc = """# FamilyCenter设计规则 V1

## 1. 成立条件

FamilyCenter不是“加成建筑”，而是某个真实Facility在特定FamilyOrganization下获得的管理指定。以下条件必须同时成立：

1. 真实Facility存在且未被摧毁；
2. Facility具备数据驱动能力 `FamilyManagement`；
3. Facility由该FamilyOrganization合法所有或控制；
4. 组织正式指定它为`PrimaryFamilyCenter`或`LocalFamilyCenter`；
5. 指派真实Person担任管理者并能实际履职。

一个FamilyOrganization最多一个Primary，可以有多个Local；同一ManagementArea内最多一个中心，Primary已覆盖本地时不得再建Local。不要使用`BranchFamilyCenter`，避免与谱系Branch混淆。

## 2. 类型、能力与承载设施

采用能力模型，不把中心锁死为单一BaseType。可以新增标准内容定义`facility.family_hall`作为常见候选，但宅第、庄园、商馆、坞堡等只有显式具备`FamilyManagement`能力并满足全部成立条件时才能承载中心。祠堂/宗庙只提供礼仪能力；住宅只提供居住能力；田地、仓库和工坊也不会自动组成中心。

## 3. 管理范围

中心绑定明确`ManagementAreaId`，可指向Settlement、UrbanArea、County、EstateComplex或其他已定义区域，不使用任意圆形半径。资产必须逐项分配到中心；处在几何范围内不等于受其管理。

状态冻结为：

- `NONE`：组织在当地没有中心，也没有远程管理关系；
- `REMOTE`：由其他中心有限监督，只能传递少量命令和报告；
- `LOCAL`：本地中心可以执行动作矩阵允许的地方组织行为；
- `PRIMARY`：组织根账、最高职位和跨区决策所在地，同时承担本地中心职责；
- `DISABLED`：设施失效、失去控制或管理者缺位导致中心停用。

## 4. 人员、通信与失效

中心指定可在管理者缺位时保留，但立即进入`DISABLED/UNSTAFFED`，除紧急保全和等待任命外不得执行正式管理。远程命令必须等待道路、信使、旅行和信息更新；不得跨城即时共享账簿、库存、军情或职位。

设施被毁、夺取或失去控制时，中心失效，但人物、家户、土地、库存、债务和其他资产各自按真实状态保留。远地资产若仍有人员、材料和运营条件可继续日常运作，但无法凭空获得新的组织预算、建设和任命。

## 5. 迁移、升格、撤销与分立

- 迁移Primary：新Facility先满足条件，完成档案、账簿、职位和必要库存交接后指定；旧Primary必须明确降为Local、撤销或废弃。
- Local升Primary：是同一中心的指定变化，不复制资产。
- 撤销Local：仅撤销管理资格，不删除当地成员或资产。
- FamilyOrganization分立：成员、中心、债务和每项组织资产必须明确分配；个人资产仍归Person，禁止平均复制或按姓氏自动切割。

## 6. 二十项开放问题冻结表

| # | 问题 | 状态 | V1结论 |
|---:|---|---|---|
| 1 | 中心采用能力还是BaseType | FROZEN | 采用`FamilyManagement`能力模型。 |
| 2 | 是否提供标准FamilyHall | FROZEN | 提供数据驱动标准候选，但不是唯一承载类型。 |
| 3 | 是否必须有管理者 | FROZEN | 必须有真实Person；缺位时指定保留但中心停用。 |
| 4 | 是否允许远程监督 | FROZEN | 允许极弱REMOTE，受通信、距离和人员约束。 |
| 5 | Local可做什么 | FROZEN | 仅做动作矩阵中的本地资产、预算、人员和设施管理。 |
| 6 | Primary专属什么 | FROZEN | 根账、最高职位、跨区大宗调拨、建撤Local、迁移和分立。 |
| 7 | 每区域几个中心 | FROZEN | 同一组织在同一ManagementArea最多一个。 |
| 8 | 中心范围如何定义 | FROZEN | 用显式ManagementAreaId和资产分配，不用半径。 |
| 9 | 庄园能否承载 | FROZEN | 可，但必须有真实Facility及完整能力/产权/人员条件。 |
| 10 | 住宅能否承载 | FROZEN | 条件同上；居住能力本身不足。 |
| 11 | 中心摧毁后资产怎样 | FROZEN | 资产独立保留或按真实事件损毁/转移，不随中心删除。 |
| 12 | 无管理者怎样 | FROZEN | `DISABLED/UNSTAFFED`，暂停正式管理。 |
| 13 | Primary怎样迁移 | FROZEN | 先建新中心并交接，再改变唯一Primary指定。 |
| 14 | Local能否升Primary | FROZEN | 可以；旧Primary须明确降格/撤销/废弃。 |
| 15 | 分家怎样处理 | FROZEN | 成员、中心、债务、组织资产逐项分配，禁止复制。 |
| 16 | Clan怎样生成剧本组织 | FROZEN | 只通过InitializationReference候选与证据审核，不自动生成。 |
| 17 | 一个Clan能否多个组织 | FROZEN | 可以，尤其不同Branch或政治集团。 |
| 18 | 住宅何时成为候选 | FROZEN | 具备长期组织管理证据、合法控制和可承载Facility时。 |
| 19 | 只有外地官员在京怎样 | FROZEN | 仅`MemberPresence`，不建立Branch、组织或中心。 |
| 20 | 洛阳184哪些组织应有中心 | OPEN_WITH_RECOMMENDATION | 当前7个均无真实Facility，不正式指定；先修成员边界，再研究皇室特殊中心、何氏及杨/袁地方候选。 |

## 7. 本轮禁止项

本轮不实现全国FamilyOrganization、Household、普通Clan资产、庄园或FamilyCenter Facility；不修改洛阳7组织运行时数据；不实现通信系统。相关表格是开发参考，不是运行时事实。
"""


report_doc = f"""# 135—260家族空间与FamilyCenter开发参考报告 V1

## 1. 结论

本轮完成了家族组织中心的规则冻结、39个Canonical Clan的空间基线、13个剧本切片（{len(snapshot_rows)}条Clan快照）、{len(candidate_specs)}条FamilyOrganization初始化候选、{len(asset_rows)}条住宅/庄园/资产证据、洛阳184年{len(luoyang_person_rows)}名人物复核、7个现有组织审计及{len(center_candidate_rows)}条中心候选建议。

成果只提高后续开发的可判定性，没有批量创建全国组织、家户、资产、庄园或中心Facility。

## 2. 洛阳184关键发现

1. 现有25人不是最终清单：保留原25条，并增加杨彪、袁绍、袁术、王允、蔡邕、董卓、曹嵩7个研究/排除样本。
2. 7个现有FamilyOrganization全部没有`family_facility_ids`，所以全部不能宣称已有Primary或Local Center。
3. `汉室主脉`把宦官并入历史成员，且以何皇后为家主；这混淆皇室核心家庭、后妃、宦官与国家宫廷组织。
4. `南阳何氏`历史成员列表混入马元义、张温、刘陶、唐周，并漏掉何皇后；符合按人口序号区间派生历史成员的错误特征。
5. 两个董氏记录缺少Canonical Clan锚点，其中一条以程序生成人物为家主；暂只能视为MODELED组织。
6. 弘农杨氏、汝南袁氏和扶风马氏在京任官只证明人物存在。杨、袁可进入Local Center研究队列；马氏当前仍是成员存在证据。

这些问题记录为审计缺陷，不在本任务中直接改写运行时数据，避免破坏27万永久人物与家户引用。

## 3. 历史空间方法

历史资料采用七层分离：Clan、Branch、Member、Residence、Estate、FamilyAsset、FamilyCenter。史料只支持到哪一层，就停在哪一层。8个既有庄园锚点均增加条件式承载判断：它们可以成为后续中心研究对象，但没有一项因“有庄园/田地”而自动升级为中心。

场景快照保持Master→稀疏Timeline/Change→Scenario继承。每个Scenario中的Clan活跃和成员信息不能派生FamilyOrganization；初始化参考只列候选边界，并统一标记`REFERENCE_ONLY_DO_NOT_INSTANTIATE`。

## 4. 数据产品

- 关系规范：`01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md`
- 中心规则及20项冻结决策：`02_FamilyCenter设计规则_V1.md`
- 动作矩阵与7份历史/洛阳工作簿：同目录03—10号文件
- 原始机器可读工作数据：`outputs/FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1/family_reference_workdata.json`

## 5. 证据与限制

本轮继承`Han135260V1`的1202人物、39 Clan、15 Branch、54人物地点记录、13 Scenario与深化层8个Estate Reference，并沿用其Primary Historical Text/Source Registry。重点依据包括《后汉书》人物与外戚/宦官记录、《三国志·糜竺传》《三国志·鲁肃传》《后汉书·樊宏传》。古籍索引只能支持其明确陈述，不能提供未知宅第边界、Cell、设施类型、管理面积或组织预算。

## 6. 下一开发阶段

下一步应是“洛阳184历史人物—家族组织—中心安全迁移切片”，顺序为：

1. 冻结7组织的V2成员映射，移除误卷入关系但保留所有Person；
2. 将皇室核心家庭、何氏、宦官/宫廷服务组织和国家资产分开；
3. 为候选组织研究真实住宅/庄园/管理Facility，不足则保持无中心；
4. 建立`FamilyManagement`数据定义、Primary/Local唯一性与Unstaffed状态；
5. 通过顺序存档迁移把V1组织引用升级到V2，做往返和不变量测试；
6. 最后才接入本地资产操作、通信延迟与玩家界面。
"""

write(DOC / "01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md", relation_doc)
write(DOC / "02_FamilyCenter设计规则_V1.md", center_doc)
write(DOC / "11_135-260家族空间与FamilyCenter开发参考报告_V1.md", report_doc)

print(json.dumps({key: len(value) for key, value in workdata.items()}, ensure_ascii=False, indent=2))
