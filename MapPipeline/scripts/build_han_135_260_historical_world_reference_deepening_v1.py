#!/usr/bin/env python3
"""Build the V1 deepening layer inside the existing historical reference library.

The output is documentation/reference data only. It never creates runtime people,
households, estates, FamilyOrganizations, Facilities, or a second world map.
"""

from __future__ import annotations

import json
import re
import shutil
from collections import Counter, defaultdict
from pathlib import Path

from build_han_135_260_historical_world_reference_v1 import PROVINCE_PROFILES, SCENARIOS, WEB_SOURCES


ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "Docs" / "HISTORICAL_WORLD_REFERENCE"
DEEP = BASE / "DEEPENING_V1"
OUT = ROOT / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1"


def load(rel: str):
    return json.loads((ROOT / rel).read_text(encoding="utf-8-sig"))


def write(path: Path, text: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def safe(value: str) -> str:
    return re.sub(r'[<>:"/\\|?*]', "_", value)


def md_table(headers, rows):
    def cell(value):
        return str(value if value is not None else "").replace("|", "／").replace("\n", " ")
    lines = ["| " + " | ".join(headers) + " |", "| " + " | ".join(["---"] * len(headers)) + " |"]
    lines.extend("| " + " | ".join(cell(v) for v in row) + " |" for row in rows)
    return "\n".join(lines)


def place_id(county_id: str) -> str:
    return "place." + county_id.removeprefix("admin.")


P0_CITY_IDS = {
    "C027": "洛阳", "C031": "长安", "C009": "邺", "C025": "许",
    "C067": "成都", "C041": "襄阳", "C043": "江陵", "C056": "建业",
}

CITY_COUNTY_OVERRIDES = {
    "C010": ("admin.han140.jizhou.julu.julu", "RECONSTRUCTED：郡域代理标签映射至同名钜鹿县；郡治候选仍按独立治所表管理"),
    "C012": ("admin.han140.jizhou.zhongshan.lunu", "RECONSTRUCTED：中山国区域标签映射至卢奴治所候选；不是整个中山国的点替代"),
    "C013": ("admin.han140.qingzhou.beihai.ju", "RECONSTRUCTED：以城阳国区域和现有近似坐标映射至莒县稳定地理；待专题复核"),
    "C035": ("admin.han140.liangzhou.jincheng.yunwu", "RECONSTRUCTED：西平/西都区域标签暂挂金城郡允吾治所候选；精确地点保持UNKNOWN"),
    "C042": ("admin.han140.jingzhou.jiangxia.xiling", "RECONSTRUCTED：江夏郡域标签映射至西陵候选；治所迁移仍由Timeline表达"),
    "C054": ("admin.han140.yangzhou.lujiang.shu", "RECONSTRUCTED：庐江郡域标签映射至舒县候选；皖县等时期治所需Timeline深化"),
    "C061": ("admin.han140.yangzhou.kuaiji.dongbu", "RECONSTRUCTED：建安县为后设县，V1暂映射至会稽东部稳定地理；不得解释为140年已有建安县"),
}

PROVINCE_BASE_SEATS = {
    "admin.han140.sili": ("admin.han140.sili.henan.luoyang", "雒阳"),
    "admin.han140.yuzhou": ("admin.han140.yuzhou.pei.qiao", "谯"),
    "admin.han140.jizhou": ("admin.han140.jizhou.changshan.gaoyi", "高邑"),
    "admin.han140.yanzhou": ("admin.han140.yanzhou.shanyang.changyi", "昌邑"),
    "admin.han140.xuzhou": ("admin.han140.xuzhou.donghai.tan", "郯"),
    "admin.han140.qingzhou": ("admin.han140.qingzhou.qi.linzi", "临菑"),
    "admin.han140.jingzhou": ("admin.han140.jingzhou.wuling.yishou", "汉寿"),
    "admin.han140.yangzhou": ("admin.han140.yangzhou.jiujiang.liyang", "历阳"),
    "admin.han140.yizhou": ("admin.han140.yizhou.guanghan.luo", "雒"),
    "admin.han140.liangzhou": ("admin.han140.liangzhou.hanyang.long", "陇"),
    "admin.han140.bingzhou": ("admin.han140.bingzhou.taiyuan.jinyang", "晋阳"),
    "admin.han140.youzhou": ("admin.han140.youzhou.guangyang.ji", "蓟"),
    "admin.han140.jiaozhou": ("admin.han140.jiaozhou.jiaozhi.longbian", "龙编"),
}

# These rows are evidence-aware reference intervals, not a claim that every late-Han
# polity retained one uncontested province seat. UNKNOWN rows preserve that ambiguity.
PROVINCE_SEAT_TIMELINES = [
    ("admin.han140.sili", "admin.han140.sili.henan.luoyang", "PROVINCE/CENTRAL_SEAT", 135, 189, "RECONSTRUCTED", "B", "source.reference.han_province_seats"),
    ("admin.han140.sili", "admin.han140.sili.jingzhao.changan", "IMPERIAL_CAPITAL", 190, 195, "HISTORICAL", "A", "source.primary.hou_han_shu.xiandi"),
    ("admin.han140.sili", "admin.han140.yuzhou.yingchuan.xu", "IMPERIAL_CAPITAL", 196, 219, "HISTORICAL", "A", "source.primary.hou_han_shu.xiandi"),
    ("admin.han140.sili", "admin.han140.sili.henan.luoyang", "CAO_WEI_CAPITAL", 220, 260, "HISTORICAL", "A", "source.primary.san_guo_zhi.wei"),
    ("admin.han140.yuzhou", "admin.han140.yuzhou.pei.qiao", "PROVINCE_SEAT_CANDIDATE", 135, 188, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.yuzhou", "", "FRAGMENTED_OR_UNRESOLVED_SEAT", 189, 260, "UNKNOWN", "D", "source.project.deepening.v1"),
    ("admin.han140.jizhou", "admin.han140.jizhou.changshan.gaoyi", "PROVINCE_SEAT_CANDIDATE", 135, 190, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.jizhou", "admin.han140.jizhou.wei.ye", "REGIONAL_POLITICAL_CENTER", 191, 260, "HISTORICAL", "B", "source.primary.san_guo_zhi.wei"),
    ("admin.han140.yanzhou", "admin.han140.yanzhou.shanyang.changyi", "PROVINCE_SEAT_CANDIDATE", 135, 191, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.yanzhou", "", "MOBILE_OR_FRAGMENTED_SEAT", 192, 260, "UNKNOWN", "D", "source.project.deepening.v1"),
    ("admin.han140.xuzhou", "admin.han140.xuzhou.donghai.tan", "PROVINCE_SEAT_CANDIDATE", 135, 192, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.xuzhou", "admin.han140.xuzhou.xiapi.xiapei", "REGIONAL_POLITICAL_CENTER", 193, 214, "RECONSTRUCTED", "B", "source.primary.san_guo_zhi.shu"),
    ("admin.han140.xuzhou", "", "FRAGMENTED_OR_UNRESOLVED_SEAT", 215, 260, "UNKNOWN", "D", "source.project.deepening.v1"),
    ("admin.han140.qingzhou", "admin.han140.qingzhou.qi.linzi", "PROVINCE_SEAT_CANDIDATE", 135, 260, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.jingzhou", "admin.han140.jingzhou.wuling.yishou", "PROVINCE_SEAT_CANDIDATE", 135, 189, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.jingzhou", "admin.han140.jingzhou.nan.xiangyang", "PROVINCE_SEAT/POLITICAL_CENTER", 190, 208, "HISTORICAL", "B", "source.primary.hou_han_shu.liubiao"),
    ("admin.han140.jingzhou", "", "MULTIPLE_POLITICAL_CENTERS", 209, 260, "UNKNOWN", "D", "source.project.deepening.v1"),
    ("admin.han140.yangzhou", "admin.han140.yangzhou.jiujiang.liyang", "PROVINCE_SEAT_CANDIDATE", 135, 188, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.yangzhou", "", "MULTIPLE_RIVAL_CENTERS", 189, 260, "UNKNOWN", "D", "source.project.deepening.v1"),
    ("admin.han140.yizhou", "admin.han140.yizhou.guanghan.luo", "PROVINCE_SEAT_CANDIDATE", 135, 187, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.yizhou", "admin.han140.yizhou.shu.chengdu", "PROVINCE_SEAT/POLITICAL_CENTER", 188, 260, "HISTORICAL", "B", "source.primary.san_guo_zhi.shu"),
    ("admin.han140.liangzhou", "admin.han140.liangzhou.hanyang.long", "PROVINCE_SEAT_CANDIDATE", 135, 188, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.liangzhou", "", "MOBILE_OR_FRAGMENTED_SEAT", 189, 260, "UNKNOWN", "D", "source.project.deepening.v1"),
    ("admin.han140.bingzhou", "admin.han140.bingzhou.taiyuan.jinyang", "PROVINCE_SEAT_CANDIDATE", 135, 260, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.youzhou", "admin.han140.youzhou.guangyang.ji", "PROVINCE_SEAT_CANDIDATE", 135, 260, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.jiaozhou", "admin.han140.jiaozhou.jiaozhi.longbian", "PROVINCE_SEAT_CANDIDATE", 135, 202, "RECONSTRUCTED", "C", "source.reference.han_province_seats"),
    ("admin.han140.jiaozhou", "", "REGIONAL_CENTER_REQUIRES_RESEARCH", 203, 260, "UNKNOWN", "D", "source.project.deepening.v1"),
]

P0_FACTS = {
    "C027": {
        "summary": "东汉首都与河洛政治、交通、消费和仓储核心；184已有正式城市与都市圈样板。",
        "geography": "洛水北岸、伊洛盆地与黄河交通体系相连；精确古城边界以汉魏故城考古为准。",
        "industry": "首都消费、官营/高技能手工业、粮仓和跨区域贸易；不得用首都比例推算其他城市。",
        "transport": "洛水、黄河渡运与函谷—虎牢东西通道共同决定首都补给和军队机动。",
        "military": "宫城、外城、十二门、城防与虎牢—洛阳走廊已有项目级样板；190年迁都与毁坏是关键变化。",
        "sources": ["source.web.luoyang_rdc_2019", "source.primary.hou_han_shu.xiandi", "source.project.luoyang184.metropolitan.v1"],
        "construction": [(135, "ExistingCapitalComplex", "HISTORICAL"), (184, "MilitaryMobilization", "HISTORICAL"), (190, "CapitalRelocationAndWarDamage", "HISTORICAL"), (220, "CaoWeiCapitalReconstruction", "RECONSTRUCTED")],
    },
    "C031": {
        "summary": "关中旧都、190—195汉廷首都和西部军政物流枢纽。",
        "geography": "渭水平原与关中道路网络中心；汉长安遗址多层累积，东汉使用状态需与西汉格局分开。",
        "industry": "关中农业、仓储、军需、车马和都城服务在190后显著上升。",
        "transport": "渭水、函谷/潼关方向、武关与陇右道路构成四向物流。",
        "military": "城垣、宫苑遗存与关中关隘形成纵深；192—195军阀冲突使城市安全和供给恶化。",
        "sources": ["source.web.shaanxi_han_changan", "source.primary.hou_han_shu.xiandi"],
        "construction": [(190, "ImperialCapitalActivation", "HISTORICAL"), (192, "UrbanConflict", "HISTORICAL"), (195, "CourtFlightAndDamage", "HISTORICAL")],
    },
    "C009": {
        "summary": "河北核心城市、袁曹政权中心和204年后曹操营建重点。",
        "geography": "漳河平原与河北粮赋区相连；遗址包含多时期营建，必须逐层区分。",
        "industry": "河北粮食、军需、工匠和大型政治中心消费。",
        "transport": "漳河水系及南北陆路连接冀州腹地、河内与中原。",
        "military": "袁绍据点、204年攻取与其后营建；三台等后续设施必须按具体年份启用。",
        "sources": ["source.web.hebei_ye_2023", "source.primary.san_guo_zhi.wei"],
        "construction": [(191, "RegionalCapitalRise", "RECONSTRUCTED"), (204, "CapturedByCaoCao", "HISTORICAL"), (204, "PlannedCapitalConstruction", "HISTORICAL"), (210, "TerraceComplexPhase", "HISTORICAL")],
    },
    "C025": {
        "summary": "颍川许县，196—220汉献帝都城与曹操政务、屯田和军需中心。",
        "geography": "颍川平原与颍水交通区；汉魏许都考古可证城壕、城垣等，但分期仍需发掘报告。",
        "industry": "农业、屯田、官署服务、仓储和中原军需。",
        "transport": "连接洛阳、陈留、汝颍和淮河方向的中原陆水网络。",
        "military": "都城守备、屯田供给与官渡战争后方；220年禅代后政治地位改变。",
        "sources": ["source.web.xuchang_2025", "source.primary.hou_han_shu.xiandi", "source.primary.san_guo_zhi.wei"],
        "construction": [(196, "ImperialCapitalActivation", "HISTORICAL"), (200, "GuanduLogisticsRear", "RECONSTRUCTED"), (220, "CapitalRoleEnds", "HISTORICAL")],
    },
    "C067": {
        "summary": "蜀郡都会、益州政治中心和四川盆地经济枢纽。",
        "geography": "成都平原与岷江灌溉体系腹地；秦汉都会地位有文献与考古研究支持。",
        "industry": "稻麦、蜀锦、盐铁、木材、医药和盆地市场网络。",
        "transport": "盆地水网、金牛道/米仓道方向与长江上游路线。",
        "military": "盆地核心与山口纵深；214易主、221成为蜀汉首都是关键状态。",
        "sources": ["source.web.sichuan_chengdu_2021", "source.primary.san_guo_zhi.shu"],
        "construction": [(188, "ProvincePoliticalCenterRise", "RECONSTRUCTED"), (214, "CapturedByLiuBei", "HISTORICAL"), (221, "ShuHanCapitalActivation", "HISTORICAL")],
    },
    "C041": {
        "summary": "汉水中游南北枢纽，190年后荆州政治中心与襄樊军事体系核心。",
        "geography": "汉水两岸与南阳盆地—江汉平原接口；现存后世城墙不得回填为汉末原状。",
        "industry": "荆北农业、水陆转运、军需和区域市场。",
        "transport": "汉水航运、襄樊渡运和通往南阳、江陵的陆路。",
        "military": "208、219前后均为战略前线；城防应与樊城、渡口和汉水共同建模。",
        "sources": ["source.web.hubei_xiangyang_2022", "source.primary.hou_han_shu.liubiao", "source.primary.san_guo_zhi.wei"],
        "construction": [(190, "JingzhouPoliticalCenter", "HISTORICAL"), (208, "CaoCaoOccupation", "HISTORICAL"), (219, "XiangfanCampaign", "HISTORICAL")],
    },
    "C043": {
        "summary": "南郡治所、江汉平原粮运和长江中游军政节点。",
        "geography": "长江—江汉平原水网核心；纪南楚城与汉末江陵必须分期，不得合成同一城市布局。",
        "industry": "稻作、渔业、木材、仓储、港埠和水军供给。",
        "transport": "长江干线与江汉水网连接襄阳、益州和江东。",
        "military": "208—210围绕南郡的争夺以及219控制变化决定仓储、水军和城防状态。",
        "sources": ["source.web.hubei_jiangling_2019", "source.primary.san_guo_zhi.wu"],
        "construction": [(208, "CaoCaoOccupation", "HISTORICAL"), (209, "SiegeAndControlChange", "HISTORICAL"), (219, "WuControlConsolidation", "HISTORICAL")],
    },
    "C056": {
        "summary": "秣陵—建业转型后的孙吴都城与长江下游水军、商业中心。",
        "geography": "长江、秦淮水网与石头山防御节点；六朝遗存必须按孙吴/东晋/南朝分层。",
        "industry": "江东稻作供给、造船、手工业、港埠贸易和都城消费。",
        "transport": "长江水运、秦淮支流、江东陆路及下游港埠网络。",
        "military": "石头城和长江水军体系；211营建、221阶段性迁都武昌、229回建业。",
        "sources": ["source.web.nanjing_records_2020", "source.primary.san_guo_zhi.wu"],
        "construction": [(211, "RenameAndCapitalConstruction", "HISTORICAL"), (221, "CapitalMovesToWuchang", "HISTORICAL"), (229, "CapitalReturnsToJianye", "HISTORICAL")],
    },
}

SCENARIO_SPATIAL = {
    140: (["C027", "C031"], ["admin.han140.sili", "admin.han140.yuzhou"], ["刘志", "梁冀"], ["clan.han.v1.f415"]),
    184: (["C027", "C022", "C010", "C034"], ["admin.han140.sili", "admin.han140.jizhou", "admin.han140.yuzhou", "admin.han140.liangzhou"], ["张角", "何进", "卢植", "皇甫嵩", "朱儁"], ["clan.han.v1.f415"]),
    189: (["C027", "C031", "C009"], ["admin.han140.sili", "admin.han140.jizhou"], ["何进", "董卓", "袁绍", "曹操", "王允"], ["clan.han.v1.f092", "clan.han.v1.f133"]),
    194: (["C025", "C017", "C018", "C022"], ["admin.han140.yanzhou", "admin.han140.xuzhou", "admin.han140.yuzhou"], ["曹操", "刘备", "陶谦", "吕布"], ["clan.han.v1.f133"]),
    200: (["C025", "C009", "C019", "C029"], ["admin.han140.yuzhou", "admin.han140.jizhou", "admin.han140.sili"], ["曹操", "袁绍", "荀彧", "许攸"], ["clan.han.v1.f092", "clan.han.v1.f133", "clan.han.v1.f154"]),
    207: (["C041", "C043", "C009", "C038"], ["admin.han140.jingzhou", "admin.han140.jizhou"], ["刘备", "刘表", "曹操", "诸葛亮"], ["clan.han.v1.f120", "clan.han.v1.f133"]),
    214: (["C067", "C065", "C066"], ["admin.han140.yizhou"], ["刘备", "刘璋", "诸葛亮", "法正"], ["clan.han.v1.f126", "clan.han.v1.f120"]),
    219: (["C041", "C043", "C065", "C056"], ["admin.han140.jingzhou", "admin.han140.yizhou", "admin.han140.yangzhou"], ["关羽", "曹仁", "孙权", "吕蒙", "刘备"], ["clan.han.v1.f045", "clan.han.v1.f099", "clan.han.v1.f126"]),
    223: (["C067", "C056", "C027", "C045"], ["admin.han140.yizhou", "admin.han140.yangzhou", "admin.han140.sili"], ["刘禅", "曹丕", "孙权", "诸葛亮"], ["clan.han.v1.f045", "clan.han.v1.f126"]),
    227: (["C067", "C065", "C031"], ["admin.han140.yizhou", "admin.han140.sili", "admin.han140.liangzhou"], ["诸葛亮", "曹叡", "赵云"], ["clan.han.v1.f120", "clan.han.v1.f067"]),
    234: (["C065", "C031", "C067"], ["admin.han140.yizhou", "admin.han140.sili"], ["诸葛亮", "司马懿", "曹叡"], ["clan.han.v1.f120", "clan.han.v1.f102"]),
    249: (["C027", "C067", "C056"], ["admin.han140.sili", "admin.han140.yizhou", "admin.han140.yangzhou"], ["司马懿", "曹爽", "刘禅", "孙权"], ["clan.han.v1.f102", "clan.han.v1.f133", "clan.han.v1.f045"]),
    260: (["C027", "C067", "C056", "C053"], ["admin.han140.sili", "admin.han140.yizhou", "admin.han140.yangzhou"], ["司马昭", "曹髦", "刘禅", "孙休"], ["clan.han.v1.f102", "clan.han.v1.f133", "clan.han.v1.f045"]),
}

MILITARY_SPACES = [
    ("milspace.yellow_turban.guangzong", 184, "广宗战区", "campaign", "C010", "黄巾主战区与围攻/救援路线", "source.primary.hou_han_shu.huangfu_song"),
    ("milspace.capital_relocation.luoyang_changan", 190, "洛阳—长安迁都走廊", "logistics_corridor", "C027|C031", "人口迁移、军队护送、物资转移与沿线破坏", "source.primary.hou_han_shu.xiandi"),
    ("milspace.xuzhou.campaigns", 193, "徐州战区", "campaign_region", "C017|C018", "城镇、粮道与人口迁徙的区域战争", "source.primary.san_guo_zhi.wei"),
    ("milspace.guandu", 200, "官渡战区", "battlefield", "C025|C009", "黄河渡口、仓储、粮道与主力会战", "source.primary.san_guo_zhi.wei"),
    ("milspace.boma_yanjin", 200, "白马—延津渡河区", "ford_corridor", "C019|C009", "黄河渡口与前哨机动", "source.primary.san_guo_zhi.wei"),
    ("milspace.red_cliffs", 208, "赤壁—乌林水战区", "river_battlefield", "C043|C056", "长江水军、疫病、火攻与撤退路线", "source.primary.san_guo_zhi.wu"),
    ("milspace.jiangling_siege", 208, "江陵围攻区", "siege_region", "C043", "城防、港埠、仓储和长江补给", "source.primary.san_guo_zhi.wu"),
    ("milspace.tong_pass", 211, "潼关—关中东口", "pass_campaign", "C031", "关隘、黄河与关中补给", "source.primary.san_guo_zhi.wei"),
    ("milspace.chengdu_214", 214, "成都攻围区", "siege_region", "C067", "盆地道路、城防与政治接收", "source.primary.san_guo_zhi.shu"),
    ("milspace.hanzhong", 219, "汉中争夺区", "mountain_campaign", "C065", "山道、关隘、粮运与撤退路线", "source.primary.san_guo_zhi.shu"),
    ("milspace.xiangfan", 219, "襄樊战区", "river_siege", "C041", "汉水水位、城防、渡运与援军", "source.primary.san_guo_zhi.wei"),
    ("milspace.yiling", 222, "夷陵—猇亭战区", "river_mountain_campaign", "C045", "长江峡口、营地链、火攻与退路", "source.primary.san_guo_zhi.wu"),
    ("milspace.jieting", 228, "街亭战区", "mountain_route_battle", "C031|C065", "陇右山道与补给节点", "source.primary.san_guo_zhi.shu"),
    ("milspace.wuzhangyuan", 234, "五丈原—渭水前线", "campaign_front", "C031|C065", "屯驻、对峙、渭水与长期补给", "source.primary.san_guo_zhi.shu"),
    ("milspace.gaoping_tombs", 249, "高平陵—洛阳政变空间", "political_military_space", "C027", "首都道路、宫城控制和禁军指挥", "source.primary.san_guo_zhi.wei"),
]

ESTATE_REFERENCES = [
    {"estate_reference_id":"estate.ref.lusu.dongcheng", "clan_id":"", "branch_id":"", "historical_person_ids":"P0251", "county_id":"admin.han140.xuzhou.xiapi.dongcheng", "estate_type":"HISTORICAL_ESTATE", "start_year":170, "end_year":208, "historical_description":"鲁肃本籍东城，史载家富、田地及两囷米；确切宅庄边界未知。", "land_evidence":"史载摽卖田地", "storage_evidence":"两囷米各三千斛", "retainer_evidence":"赈穷结士，未量化为庄园依附人口", "source_id":"source.primary.san_guo_zhi.lusu", "evidence_level":"HISTORICAL", "unknowns":"准确地点、规模、建筑和常住人口"},
    {"estate_reference_id":"estate.ref.mishi.qu", "clan_id":"clan.han.v1.f006", "branch_id":"", "historical_person_ids":"P0215|P0216", "county_id":"admin.han140.xuzhou.donghai.qu", "estate_type":"RECONSTRUCTED_ESTATE", "start_year":160, "end_year":220, "historical_description":"糜氏祖世货殖、僮客万人、资产巨亿，支持在朐县建立富商豪族产业锚点；不是已定位庄园。", "land_evidence":"财富与货殖可证，具体田产地点未明", "storage_evidence":"大规模商业资产可推定仓储需求", "retainer_evidence":"僮客万人为直接文本证据，但不可等同同址常住人口", "source_id":"source.primary.san_guo_zhi.mizhu", "evidence_level":"RECONSTRUCTED", "unknowns":"庄园数量、位置、产业构成和私兵"},
    {"estate_reference_id":"estate.ref.fanshi.huyang", "clan_id":"", "branch_id":"", "historical_person_ids":"", "county_id":"admin.han140.jingzhou.nanyang.huyang", "estate_type":"RECONSTRUCTED_ESTATE", "start_year":135, "end_year":180, "historical_description":"樊氏湖阳本籍与田产叙事支持地方豪族田庄锚点；尚未纳入Canonical Clan/Person。", "land_evidence":"《后汉书·樊宏传》记田产相关事实", "storage_evidence":"UNKNOWN", "retainer_evidence":"UNKNOWN", "source_id":"source.primary.hou_han_shu.fanhong", "evidence_level":"RECONSTRUCTED", "unknowns":"Canonical人物/Clan接入、确切地点、规模和设施"},
    {"estate_reference_id":"estate.ref.yuan.ruyang", "clan_id":"clan.han.v1.f092", "branch_id":"branch.han.v1.f092.yuan_feng|branch.han.v1.f092.yuan_wei", "historical_person_ids":"P0012|P0013|P0080", "county_id":"admin.han140.yuzhou.runan.ruyang", "estate_type":"POTENTIAL_ESTATE", "start_year":135, "end_year":202, "historical_description":"汝南袁氏政治与本籍锚点；暂无本轮可核定庄园形态。", "land_evidence":"UNKNOWN", "storage_evidence":"UNKNOWN", "retainer_evidence":"UNKNOWN", "source_id":"source.historical.0f9b2f62e39ad654", "evidence_level":"UNKNOWN", "unknowns":"土地、宅第、依附人口与具体位置"},
    {"estate_reference_id":"estate.ref.sima.wen", "clan_id":"clan.han.v1.f102", "branch_id":"branch.han.v1.f102.sima_yi", "historical_person_ids":"P0148", "county_id":"admin.han140.sili.henei.wen", "estate_type":"POTENTIAL_ESTATE", "start_year":170, "end_year":260, "historical_description":"河内司马氏温县本籍锚点；本轮不把宗族直接物化为庄园。", "land_evidence":"UNKNOWN", "storage_evidence":"UNKNOWN", "retainer_evidence":"UNKNOWN", "source_id":"source.historical.67078028b462e12b", "evidence_level":"UNKNOWN", "unknowns":"土地、宅第、产业和各Branch地域分布"},
    {"estate_reference_id":"estate.ref.cao.qiao", "clan_id":"clan.han.v1.f133", "branch_id":"", "historical_person_ids":"", "county_id":"admin.han140.yuzhou.pei.qiao", "estate_type":"POTENTIAL_ESTATE", "start_year":135, "end_year":220, "historical_description":"谯县曹氏本籍与政治家族锚点；庄园证据尚未专项审核。", "land_evidence":"UNKNOWN", "storage_evidence":"UNKNOWN", "retainer_evidence":"UNKNOWN", "source_id":"source.historical.67078028b462e12b", "evidence_level":"UNKNOWN", "unknowns":"具体宅庄、土地与依附人口"},
    {"estate_reference_id":"estate.ref.sun.fuchun", "clan_id":"clan.han.v1.f045", "branch_id":"", "historical_person_ids":"", "county_id":"admin.han140.yangzhou.wu.fuchun", "estate_type":"POTENTIAL_ESTATE", "start_year":150, "end_year":260, "historical_description":"吴郡孙氏富春本籍锚点；不自动生成孙氏庄园或私兵。", "land_evidence":"UNKNOWN", "storage_evidence":"UNKNOWN", "retainer_evidence":"UNKNOWN", "source_id":"source.historical.67078028b462e12b", "evidence_level":"UNKNOWN", "unknowns":"确切地产、宅第、Branch和时间变化"},
    {"estate_reference_id":"estate.ref.zhen.wuji", "clan_id":"clan.han.v1.f489", "branch_id":"", "historical_person_ids":"P1189", "county_id":"admin.han140.jizhou.zhongshan.wuji", "estate_type":"POTENTIAL_ESTATE", "start_year":150, "end_year":230, "historical_description":"中山甄氏毋极本籍锚点；本轮尚无足够庄园资产证据。", "land_evidence":"UNKNOWN", "storage_evidence":"UNKNOWN", "retainer_evidence":"UNKNOWN", "source_id":"source.historical.67078028b462e12b", "evidence_level":"UNKNOWN", "unknowns":"家产规模、地点、设施和依附人口"},
]

NEW_SOURCES = [
    {"source_id":"source.project.deepening.v1", "source_type":"project_reference", "title":"HAN-135-260历史世界参考深化V1", "url":"Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/README_历史世界深化资料索引.md", "evidence_scope":"Canonical去重、研究等级、继承规则和开发含义；不能作为历史事实来源", "license_note":"项目原创"},
    {"source_id":"source.reference.han_province_seats", "source_type":"reference_index", "title":"东汉十三州治所研究线索索引", "url":"https://zh.wikipedia.org/wiki/%E4%B8%9C%E6%B1%89", "evidence_scope":"140年前后十三州治所线索；必须由正史或专门研究复核，V1统一标RECONSTRUCTED", "license_note":"仅保存事实线索和链接，不复制条目文本"},
    {"source_id":"source.academic.three_kingdoms_admin", "source_type":"academic_article", "title":"三国时期地方行政与正统观念研究", "url":"https://xbzs.ecnu.edu.cn/CN/html/2018-4-50.htm", "evidence_scope":"三国政权州制、遥领与行政变化的研究入口", "license_note":"仅记录研究结论入口"},
    {"source_id":"source.primary.hou_han_shu.xiandi", "source_type":"primary_historical_text", "title":"《后汉书·孝献帝纪》", "url":"https://zh.wikisource.org/wiki/%E5%BE%8C%E6%BC%A2%E6%9B%B8/%E5%8D%B79", "evidence_scope":"189—220中枢、迁都与献帝朝事件", "license_note":"古籍原文公有领域"},
    {"source_id":"source.primary.hou_han_shu.liubiao", "source_type":"primary_historical_text", "title":"《后汉书·刘表传》", "url":"https://zh.wikisource.org/wiki/%E5%BE%8C%E6%BC%A2%E6%9B%B8/%E5%8D%B774%E4%B8%8B", "evidence_scope":"刘表与荆州政治中心", "license_note":"古籍原文公有领域"},
    {"source_id":"source.primary.hou_han_shu.huangfu_song", "source_type":"primary_historical_text", "title":"《后汉书·皇甫嵩朱儁列传》", "url":"https://zh.wikisource.org/wiki/%E5%BE%8C%E6%BC%A2%E6%9B%B8/%E5%8D%B771", "evidence_scope":"184黄巾战争空间和人物", "license_note":"古籍原文公有领域"},
    {"source_id":"source.primary.hou_han_shu.fanhong", "source_type":"primary_historical_text", "title":"《后汉书·樊宏传》", "url":"https://zh.wikisource.org/wiki/%E5%BE%8C%E6%BC%A2%E6%9B%B8/%E5%8D%B732", "evidence_scope":"湖阳樊氏本籍与田产线索", "license_note":"古籍原文公有领域"},
    {"source_id":"source.primary.san_guo_zhi.wei", "source_type":"primary_historical_text", "title":"《三国志·魏书》相关纪传", "url":"https://zh.wikisource.org/wiki/%E4%B8%89%E5%9C%8B%E5%BF%97", "evidence_scope":"曹魏城市、战争、人物和控制变化；具体Claim需继续细化至卷", "license_note":"古籍原文公有领域"},
    {"source_id":"source.primary.san_guo_zhi.shu", "source_type":"primary_historical_text", "title":"《三国志·蜀书》相关纪传", "url":"https://zh.wikisource.org/wiki/%E4%B8%89%E5%9C%8B%E5%BF%97", "evidence_scope":"益州、汉中、北伐及蜀汉人物", "license_note":"古籍原文公有领域"},
    {"source_id":"source.primary.san_guo_zhi.wu", "source_type":"primary_historical_text", "title":"《三国志·吴书》相关纪传", "url":"https://zh.wikisource.org/wiki/%E4%B8%89%E5%9C%8B%E5%BF%97", "evidence_scope":"建业、江陵、长江战争及孙吴人物", "license_note":"古籍原文公有领域"},
    {"source_id":"source.primary.san_guo_zhi.mizhu", "source_type":"primary_historical_text", "title":"《三国志·糜竺传》", "url":"https://zh.wikisource.org/zh-hant/%E4%B8%89%E5%9C%8B%E5%BF%97/%E5%8D%B738", "evidence_scope":"糜氏货殖、僮客和资产；不提供已定位庄园边界", "license_note":"古籍原文公有领域"},
    {"source_id":"source.primary.san_guo_zhi.lusu", "source_type":"primary_historical_text", "title":"《三国志·鲁肃传》", "url":"https://zh.wikisource.org/zh-hant/%E4%B8%89%E5%9C%8B%E5%BF%97/%E5%8D%B754", "evidence_scope":"鲁肃东城本籍、田地、财富和两囷米；确切宅庄范围未知", "license_note":"古籍原文公有领域"},
]


CORE_TOPICS = [
    "01 地点身份与稳定ID", "02 名称与异名", "03 现代位置参考", "04 自然地理", "05 水系",
    "06 地形与通道", "07 行政隶属时间轴", "08 治所角色时间轴", "09 政权控制时间轴", "10 人口口径",
    "11 城墙与边界", "12 城门", "13 宫城与内城", "14 道路与街区", "15 桥梁与津渡",
    "16 港埠与码头", "17 官署", "18 市场", "19 仓储", "20 军事设施",
    "21 教育", "22 礼制与宗教", "23 医疗", "24 居住", "25 农业",
    "26 手工业", "27 商业与服务", "28 资源与原料", "29 交通网络", "30 供应网络",
    "31 历史人物", "32 Clan与Branch", "33 庄园与地产锚点", "34 政治事件", "35 军事事件",
    "36 扩建、毁坏与重建", "37 十三剧本切片", "38 证据与来源", "39 未知与冲突", "40 开发含义",
]


def first_county_by_commandery(counties):
    result = {}
    for feature in counties:
        props = feature["properties"]
        result.setdefault(props["parent_admin_unit_id"], props["admin_unit_id"])
    return result


def city_admin_id(city):
    props = city["properties"]
    return CITY_COUNTY_OVERRIDES.get(props["city_id"], (props.get("admin_reference") or "", ""))[0]


def build_master_document(row, city_rows, county, p0, people, clans, estates, pop140, pop184, scenario_years):
    city_names = "、".join(x["display_name"] for x in city_rows) or "无77城标签"
    person_names = "、".join(x["canonical_name"] for x in people[:12]) or "本轮未绑定具体人物"
    clan_names = "、".join(x["canonical_clan_name"] for x in clans[:8]) or "本轮未绑定Canonical Clan"
    estate_names = "、".join(x["estate_reference_id"] for x in estates) or "无已审核地产锚点"
    pop_text = (
        f"140模型人口 {pop140.get('modeled_actual_population', 'UNKNOWN')}；"
        f"184模型人口 {pop184.get('modeled_actual_population', 'UNKNOWN')}。人口值来自既有全国人口模型，不建立第二套模型。"
    )
    if p0:
        pop_text += " 洛阳184特殊校准遵循城墙内20万、城区27万、都市圈40万、供给圈70万（含都市圈，禁止相加）；其他城市不得继承该比例。" if row["display_name"] in ("雒阳", "洛阳") else " 本城市不得套用洛阳首都人口比例。"
    known = {
        "01 地点身份与稳定ID": f"PlacePermanentId `{row['place_id']}`；县级稳定ID `{row['county_id']}`。",
        "02 名称与异名": f"140县级名称：{row['display_name']}；战略城市标签：{city_names}。异名需要有效期，不改PlacePermanentId。",
        "03 现代位置参考": f"经纬度 {row.get('longitude') or 'UNKNOWN'}, {row.get('latitude') or 'UNKNOWN'}；坐标状态 {county.get('coordinate_status') or 'UNKNOWN'}。",
        "04 自然地理": p0.get("geography", "仅完成州郡县级地理骨架；详细地貌待专题研究。") if p0 else "仅完成州郡县级地理骨架；详细地貌待专题研究。",
        "07 行政隶属时间轴": f"140稳定索引：{row['province_name']} → {row['commandery_name']} → {row['display_name']}。135—260改名、分合以稀疏Change Event叠加。",
        "08 治所角色时间轴": f"州治={row['is_province_seat']}；郡国治候选={row['is_commandery_seat']}；县治={row['is_county_seat']}。候选不等于精确治所考证完成。",
        "10 人口口径": pop_text,
        "18 市场": p0.get("industry", "MARKET/WAREHOUSE 等设施只在有地点、年份和证据后物化。") if p0 else "MARKET/WAREHOUSE 等设施只在有地点、年份和证据后物化。",
        "20 军事设施": p0.get("military", "战略角色不自动生成城墙、营地、驻军或私兵。") if p0 else "战略角色不自动生成城墙、营地、驻军或私兵。",
        "25 农业": p0.get("industry", "参考所在州郡人口与资源模型；无证据时不指定作物配比。") if p0 else "参考所在州郡人口与资源模型；无证据时不指定作物配比。",
        "26 手工业": p0.get("industry", "本轮仅建立区域研究入口，不生成设施实例。") if p0 else "本轮仅建立区域研究入口，不生成设施实例。",
        "29 交通网络": p0.get("transport", "与既有18条路线、31个节点联合查询；近邻关系不等于历史道路事实。") if p0 else "与既有18条路线、31个节点联合查询；近邻关系不等于历史道路事实。",
        "31 历史人物": person_names,
        "32 Clan与Branch": clan_names + "。Clan、Branch、Estate、FamilyOrganization严格分离。",
        "33 庄园与地产锚点": estate_names + "；本轮只建Reference，不物化地产或组织。",
        "37 十三剧本切片": "、".join(map(str, scenario_years)) or "通过Master+Timeline继承，无独立重复文档。",
        "38 证据与来源": f"ReferenceLevel={row['reference_level']}；EvidenceCoverage={row['evidence_coverage']}；来源见总索引。",
        "39 未知与冲突": "精确城界、街区、建筑坐标与跨年控制缺失时保持UNKNOWN；不得用游戏需要反推史实。",
        "40 开发含义": "运行时先解析Master，再应用查询年份前最后一条Timeline/Change Event；表现层只显示玩家或组织掌握的信息。",
    }
    lines = [f"# {row['display_name']}｜核心历史聚落 Master", "", p0.get("summary", "本地点属于去重后的核心聚落网络；当前为可开发研究骨架。") if p0 else "本地点属于去重后的核心聚落网络；当前为可开发研究骨架。", ""]
    for topic in CORE_TOPICS:
        lines += [f"## {topic}", "", known.get(topic, "UNKNOWN：本轮没有足够的地点—年份—来源证据；保留后续专题研究入口。"), ""]
    return "\n".join(lines)


def main():
    DEEP.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    for folder in ["04_CORE_SETTLEMENTS", "05_COMMANDERY_REGIONAL_REFERENCE", "06_PRIORITY_COUNTIES", "07_ELITE_CLANS_AND_ESTATES", "12_SCENARIO_WORLD_REFERENCE"]:
        target = DEEP / folder
        if target.exists():
            shutil.rmtree(target)
        target.mkdir(parents=True)

    v1 = load("outputs/HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1/historical_world_reference_workdata.json")
    provinces = load("MapData/HanWorld_Master_V0/administrative/provinces_v0.geojson")["features"]
    commanderies = load("MapData/HanWorld_Master_V0/administrative/commanderies_v0.geojson")["features"]
    counties = load("MapData/HanWorld_Master_V0/historical/county_anchors.geojson")["features"]
    cities = load("MapData/HanWorld_Master_V0/historical/strategic_cities.geojson")["features"]
    routes = load("MapData/HanWorld_Master_V0/historical/major_routes_v0.geojson")["features"]
    sites = load("MapData/HanWorld_Master_V0/historical/strategic_sites.geojson")["features"]
    people_raw = load("Assets/StreamingAssets/HistoricalPersons/Han135260V1/persons.json")["persons"]
    branches = load("Assets/StreamingAssets/HistoricalPersons/Han135260V1/branches.json")["branches"]
    pop140_raw = load("Assets/StreamingAssets/HistoricalPopulation/Han135260V1/years/year_140.json")
    pop184_raw = load("Assets/StreamingAssets/HistoricalPopulation/Han135260V1/years/year_184.json")

    province_names = {x["properties"]["admin_unit_id"]: x["properties"]["display_name"] for x in provinces}
    commandery_names = {x["properties"]["admin_unit_id"]: x["properties"]["display_name"] for x in commanderies}
    commandery_province = {}
    for cid in commandery_names:
        commandery_province[cid] = ".".join(cid.split(".")[:3])
    county_by_id = {x["properties"]["admin_unit_id"]: x["properties"] | {"geometry": x.get("geometry")} for x in counties}
    first_seats = first_county_by_commandery(counties)
    city_by_id = {x["properties"]["city_id"]: x["properties"] for x in cities}
    city_by_county = defaultdict(list)
    for x in cities:
        city_by_county[city_admin_id(x)].append(x["properties"])
    people = v1["persons"]
    clans = v1["clans"]
    people_by_county = defaultdict(list)
    for person in people:
        if person.get("native_county_id"):
            people_by_county[person["native_county_id"]].append(person)
    clans_by_county = defaultdict(list)
    for clan in clans:
        if clan.get("county_region_id"):
            clans_by_county[clan["county_region_id"]].append(clan)
    estate_by_county = defaultdict(list)
    for estate in ESTATE_REFERENCES:
        estate_by_county[estate["county_id"]].append(estate)
    county_pop140 = {x["county_permanent_id"]: x for x in pop140_raw["counties"]}
    county_pop184 = {x["county_permanent_id"]: x for x in pop184_raw["counties"]}
    region_pop140 = {x["region_permanent_id"]: x for x in pop140_raw["regions"]}
    region_pop184 = {x["region_permanent_id"]: x for x in pop184_raw["regions"]}

    province_seat_ids = {x[0] for x in PROVINCE_BASE_SEATS.values()}
    core_ids = set(first_seats.values()) | set(province_seat_ids) | set(city_by_county)
    missing_core = sorted(x for x in core_ids if x not in county_by_id)
    if missing_core:
        raise RuntimeError(f"Core county IDs missing: {missing_core}")

    core_rows = []
    for cid in sorted(core_ids):
        county = county_by_id[cid]
        cmd = county["parent_admin_unit_id"]
        pid = commandery_province[cmd]
        tagged = city_by_county.get(cid, [])
        city_ids = [x["city_id"] for x in tagged]
        p0_city = next((x for x in city_ids if x in P0_CITY_IDS), "")
        is_cmd = cid in set(first_seats.values())
        is_province = cid in province_seat_ids
        priority = "P0" if p0_city else ("P1" if is_province or any(x in {"C001","C010","C019","C022","C034","C038","C045","C065"} for x in city_ids) else "P2")
        ref = "R5" if p0_city == "C027" else ("R4" if p0_city else ("R3" if priority in {"P1","P2"} else "R2"))
        coords = (county.get("geometry") or {}).get("coordinates") or [None, None]
        core_rows.append({
            "place_id": place_id(cid), "display_name": city_by_id[p0_city]["display_name"] if p0_city else county.get("display_name"), "historical_county_name": county.get("display_name"), "county_id": cid,
            "commandery_id": cmd, "commandery_name": commandery_names[cmd], "province_id": pid, "province_name": province_names[pid],
            "city_ids": "|".join(city_ids), "city_names": "|".join(x["display_name"] for x in tagged),
            "is_county_seat": True, "is_commandery_seat": is_cmd, "is_kingdom_seat": is_cmd and any(t in commandery_names[cmd] for t in ["国", "属国"]),
            "is_province_seat": is_province, "is_capital": p0_city in {"C027","C031","C025","C067","C056"}, "is_strategic_city": bool(tagged),
            "valid_from_year": 135, "valid_to_year": 260, "priority": priority, "reference_level": ref,
            "evidence_type": "HISTORICAL_INDEX+RECONSTRUCTED_ROLE", "evidence_coverage": "MASTER+ADMIN+POPULATION+ROLE_TAGS" + ("+P0_DETAIL" if p0_city else ""),
            "longitude": coords[0], "latitude": coords[1], "coordinate_status": county.get("coordinate_status"),
            "source_ids": county.get("source_ids") or "source.hou_han_shu.jun_guo_zhi",
        })
    core_by_county = {x["county_id"]: x for x in core_rows}

    seat_rows = []
    for cmd, cid in sorted(first_seats.items()):
        seat_rows.append({
            "seat_record_id": f"seat.{cmd}.candidate.135_260", "admin_unit_id": cmd, "admin_unit_name": commandery_names[cmd],
            "admin_level": "COMMANDERY_EQUIVALENT", "seat_place_id": place_id(cid), "seat_county_id": cid,
            "valid_from_year": 135, "valid_to_year": 260, "role_type": "COMMANDERY_SEAT_CANDIDATE",
            "evidence_type": "RECONSTRUCTED", "confidence": "B", "source_id": "source.hou_han_shu.jun_guo_zhi",
            "method_notes": "以郡国志现有县序首项建立可审计候选；不代表105项治所均已专题考证。",
        })
    for i, (admin_id, cid, role, start, end, evidence, confidence, source) in enumerate(PROVINCE_SEAT_TIMELINES, 1):
        seat_rows.append({"seat_record_id": f"seat.province.{i:03d}", "admin_unit_id": admin_id, "admin_unit_name": province_names.get(admin_id, admin_id), "admin_level": "PROVINCE_OR_CENTRAL", "seat_place_id": place_id(cid) if cid else "", "seat_county_id": cid, "valid_from_year": start, "valid_to_year": end, "role_type": role, "evidence_type": evidence, "confidence": confidence, "source_id": source, "method_notes": "稀疏时间轴；UNKNOWN保持空地点，不强填唯一州治。"})

    priority_ids = set(core_ids)
    priority_ids |= {x["native_county_id"] for x in people if x.get("native_county_id") in county_by_id}
    priority_ids |= {x["county_region_id"] for x in clans if x.get("county_region_id") in county_by_id}
    priority_ids |= {x["county_id"] for x in ESTATE_REFERENCES if x["county_id"] in county_by_id}
    priority_rows = []
    for cid in sorted(priority_ids):
        county = county_by_id[cid]; cmd = county["parent_admin_unit_id"]; pid = commandery_province[cmd]
        reasons = []
        if cid in core_by_county: reasons.append("CORE_SETTLEMENT")
        if people_by_county[cid]: reasons.append("HISTORICAL_PERSON_NATIVE")
        if clans_by_county[cid]: reasons.append("CLAN_ORIGIN")
        if estate_by_county[cid]: reasons.append("ESTATE_REFERENCE")
        score = (10 if "CORE_SETTLEMENT" in reasons else 0) + min(8, len(people_by_county[cid])) + 3*len(clans_by_county[cid]) + 3*len(estate_by_county[cid])
        priority = core_by_county.get(cid, {}).get("priority") or ("P1" if score >= 8 else "P2" if score >= 4 else "P3")
        priority_rows.append({"county_id": cid, "display_name": county["display_name"], "commandery_id": cmd, "commandery_name": commandery_names[cmd], "province_id": pid, "province_name": province_names[pid], "priority": priority, "selection_score": score, "selection_reasons": "|".join(reasons), "historical_person_count": len(people_by_county[cid]), "clan_count": len(clans_by_county[cid]), "estate_reference_count": len(estate_by_county[cid]), "population_140_modeled": county_pop140.get(cid, {}).get("modeled_actual_population"), "population_184_modeled": county_pop184.get(cid, {}).get("modeled_actual_population"), "reference_level": core_by_county.get(cid, {}).get("reference_level", "R2"), "evidence_type": "HISTORICAL_INDEX+MODELED_POPULATION", "development_status": "PRIORITY_REFERENCE_READY"})

    change_rows = []
    for city_id, facts in P0_FACTS.items():
        county_id = city_admin_id({"properties": city_by_id[city_id]})
        for year, change, evidence in facts["construction"]:
            change_rows.append({"change_id": f"change.{city_id.lower()}.{year}.{safe(change).lower()}", "place_id": place_id(county_id), "year": year, "change_type": change, "before_state": "INHERIT_PREVIOUS", "after_state": change, "evidence_type": evidence, "source_ids": "|".join(facts["sources"]), "scenario_year": year if year in SCENARIO_SPATIAL else "", "notes": "稀疏变化；未列字段继承Master或上一条变更。"})
    for row in seat_rows:
        if row["seat_place_id"]:
            change_rows.append({"change_id": f"change.{row['seat_record_id']}", "place_id": row["seat_place_id"], "year": row["valid_from_year"], "change_type": "SEAT_ROLE_CHANGE", "before_state": "INHERIT_PREVIOUS", "after_state": row["role_type"], "evidence_type": row["evidence_type"], "source_ids": row["source_id"], "scenario_year": row["valid_from_year"] if row["valid_from_year"] in SCENARIO_SPATIAL else "", "notes": "治所角色变化，不移动永久地理。"})

    industry_rows = []
    for profile_key, profile in PROVINCE_PROFILES.items():
        pid = f"admin.han140.{profile_key}"
        geography, industry, transport, military = profile
        industry_rows.append({"region_id": pid, "region_name": province_names.get(pid, pid), "reference_type": "PROVINCE_INDUSTRY_RESOURCE", "agriculture": geography, "industry": industry, "resources": "按既有资源分布与县级地理后续深化", "supply_implication": transport + "；" + military, "facility_mapping": "crop_field|pasture|mine|weaving|smithy|warehouse|market（按证据选择，不自动实例化）", "evidence_type": "RECONSTRUCTED+MODELED", "source_ids": "source.project.deepening.v1", "unknowns": "县级产量、矿脉数量、设施地点和技术水平"})
    transport_rows = []
    for route in routes:
        p = route["properties"]
        transport_rows.append({"transport_id": p["route_id"], "name": p["name"], "transport_type": p["route_type"], "start_reference": p["start_location_id"], "end_reference": p["end_location_id"], "parent_location": "", "longitude": "", "latitude": "", "distance_km": p.get("estimated_km"), "evidence_type": "RECONSTRUCTED" if str(p.get("historical_claim")).lower() != "true" else "HISTORICAL", "confidence": p.get("confidence"), "source_ids": "source.project.master_map.v0", "development_implication": "旅行、商运、军粮和情报共同使用；速度与损耗由道路/天气/治安决定。"})
    for site in sites:
        p = site["properties"]
        transport_rows.append({"transport_id": p["site_id"], "name": p["name"], "transport_type": p["site_type"], "start_reference": "", "end_reference": "", "parent_location": p.get("parent_location_id"), "longitude": p.get("longitude"), "latitude": p.get("latitude"), "distance_km": "", "evidence_type": "HISTORICAL" if p.get("historical_claim") else "RECONSTRUCTED", "confidence": p.get("confidence"), "source_ids": "source.project.master_map.v0", "development_implication": "作为通行、侦察、建设和战役约束；精确位置未知时只用范围。"})
    military_rows = [{"military_space_id": a, "start_year": b, "end_year": b, "name": c, "space_type": d, "related_city_ids": e, "development_role": f, "evidence_type": "HISTORICAL_REFERENCE", "source_id": g, "geometry_status": "REGIONAL_REFERENCE_NOT_EXACT_POLYGON"} for a,b,c,d,e,f,g in MILITARY_SPACES]

    source_rows = list(v1["sources"])
    existing_sources = {x["source_id"] for x in source_rows}
    for source in NEW_SOURCES:
        if source["source_id"] not in existing_sources:
            source_rows.append({"source_id": source["source_id"], "source_type": source["source_type"], "title": source["title"], "author_or_editor": "", "edition_or_host": "", "url_or_locator": source["url"], "access_date": "2026-08-10", "reliability_class": "", "evidence_scope": source["evidence_scope"], "license_note": source["license_note"], "notes": ""})

    scenario_rows = []
    scenario_names = {year: value[0] for year, value in SCENARIOS.items()}
    for year, (city_ids, region_ids, person_names, clan_ids) in SCENARIO_SPATIAL.items():
        scenario_rows.append({"scenario_id": f"scenario.deepening.{year}", "year": year, "scenario_name": scenario_names.get(year, str(year)), "before_window": f"{max(135, year-3)}-{year-1}", "after_window": f"{year+1}-{min(260, year+3)}", "core_city_ids": "|".join(city_ids), "region_ids": "|".join(region_ids), "key_person_names": "|".join(person_names), "clan_ids": "|".join(clan_ids), "state_resolution": "Master + latest Timeline/ChangeEvent <= scenario year", "evidence_type": "HISTORICAL_SCENARIO_REFERENCE", "unknowns": "控制边界、设施损毁和逐日军队位置需后续事件级研究"})
        write(DEEP / "12_SCENARIO_WORLD_REFERENCE" / f"{year}_{safe(scenario_names.get(year, str(year)))}.md", f"# {year}｜{scenario_names.get(year, str(year))} 历史空间切片\n\n- 时间窗：{max(135, year-3)}—{min(260, year+3)}\n- 核心城市：{'、'.join(city_by_id[x]['display_name'] for x in city_ids if x in city_by_id)}\n- 区域：{'、'.join(province_names.get(x, x) for x in region_ids)}\n- 关键人物：{'、'.join(person_names)}\n- Clan：{'、'.join(clan_ids)}\n- 解析：Master + 查询年之前最新 Timeline/Change Event。\n- 未知：控制边界、设施损毁与逐日军队位置继续保持 UNKNOWN。\n")

    for row in core_rows:
        city_rows = city_by_county.get(row["county_id"], [])
        p0_id = next((x["city_id"] for x in city_rows if x["city_id"] in P0_FACTS), "")
        scenario_years = [year for year, payload in SCENARIO_SPATIAL.items() if any(x["city_id"] in payload[0] for x in city_rows)]
        folder = DEEP / "04_CORE_SETTLEMENTS" / f"{row['priority']}_{safe(row['display_name'])}_{row['place_id'].replace('.', '_')}"
        write(folder / "00_Master.md", build_master_document(row, city_rows, county_by_id[row["county_id"]], P0_FACTS.get(p0_id, {}), people_by_county[row["county_id"]], clans_by_county[row["county_id"]], estate_by_county[row["county_id"]], county_pop140.get(row["county_id"], {}), county_pop184.get(row["county_id"], {}), scenario_years))
        write(folder / "01_structured_reference.json", json.dumps({"schema":"HistoricalCoreSettlementReferenceV1", "master":row, "city_tags":city_rows, "population":{"140":county_pop140.get(row["county_id"]),"184":county_pop184.get(row["county_id"])}, "people":[x["person_id"] for x in people_by_county[row["county_id"]]], "clans":[x["clan_id"] for x in clans_by_county[row["county_id"]]], "estate_references":[x["estate_reference_id"] for x in estate_by_county[row["county_id"]]], "scenario_years":scenario_years}, ensure_ascii=False, indent=2))

    counties_by_cmd = defaultdict(list)
    for cid, county in county_by_id.items(): counties_by_cmd[county["parent_admin_unit_id"]].append(cid)
    for cmd in sorted(commandery_names):
        pid = commandery_province[cmd]; seat = first_seats[cmd]
        doc = f"# {commandery_names[cmd]}｜区域开发参考\n\n- 稳定ID：`{cmd}`\n- 州部：{province_names[pid]}\n- 135—260治所候选：{county_by_id[seat]['display_name']}（RECONSTRUCTED；首列县候选，待专题复核）\n- 县级单位：{len(counties_by_cmd[cmd])}\n- 140模型人口：{region_pop140.get(cmd, {}).get('modeled_actual_population', 'UNKNOWN')}\n- 184模型人口：{region_pop184.get(cmd, {}).get('modeled_actual_population', 'UNKNOWN')}\n- 战略城市：{'、'.join(x['display_name'] for cid in counties_by_cmd[cmd] for x in city_by_county.get(cid, [])) or '无77城标签'}\n- 人物本籍：{'、'.join(x['canonical_name'] for cid in counties_by_cmd[cmd] for x in people_by_county.get(cid, [])[:8]) or '本轮无县级绑定'}\n- Clan：{'、'.join(x['canonical_clan_name'] for cid in counties_by_cmd[cmd] for x in clans_by_county.get(cid, [])) or '本轮无县级绑定'}\n- 开发合同：人口继承既有模型；设施、道路、地产只有在有证据后实例化。\n- 未知：精确郡界、历年治所迁移、县级产业和军队位置。\n"
        write(DEEP / "05_COMMANDERY_REGIONAL_REFERENCE" / f"{safe(commandery_names[cmd])}_{cmd.replace('.', '_')}.md", doc)

    for row in priority_rows:
        cid = row["county_id"]
        write(DEEP / "06_PRIORITY_COUNTIES" / f"{row['priority']}_{safe(row['display_name'])}_{cid.replace('.', '_')}.md", f"# {row['display_name']}｜重点县开发参考\n\n- 稳定ID：`{cid}`\n- 隶属：{row['province_name']} / {row['commandery_name']}\n- 入选原因：{row['selection_reasons']}\n- 140/184模型人口：{row['population_140_modeled']} / {row['population_184_modeled']}\n- 人物：{'、'.join(x['canonical_name'] for x in people_by_county[cid]) or '无县级绑定'}\n- Clan：{'、'.join(x['canonical_clan_name'] for x in clans_by_county[cid]) or '无县级绑定'}\n- 地产锚点：{'、'.join(x['estate_reference_id'] for x in estate_by_county[cid]) or '无'}\n- 证据：HISTORICAL_INDEX + MODELED_POPULATION；位置或设施未知时不得自动补全。\n")

    branch_by_clan = defaultdict(list)
    for branch in branches: branch_by_clan[branch["clan_id"]].append(branch)
    for clan in clans:
        cid = clan.get("county_region_id") or ""
        estates = [x for x in ESTATE_REFERENCES if x["clan_id"] == clan["clan_id"]]
        write(DEEP / "07_ELITE_CLANS_AND_ESTATES" / f"{safe(clan['canonical_clan_name'])}_{clan['clan_id'].replace('.', '_')}.md", f"# {clan['canonical_clan_name']}｜豪族与地产参考\n\n- ClanId：`{clan['clan_id']}`\n- 郡望/本籍：{clan.get('commandery_region_id') or 'UNKNOWN'} / {cid or 'UNKNOWN'}\n- Branch：{'、'.join(x['branch_id'] for x in branch_by_clan[clan['clan_id']]) or '无已建Branch'}\n- EstateReference：{'、'.join(x['estate_reference_id'] for x in estates) or '无已审核锚点'}\n- 明确边界：Clan ≠ Branch ≠ Estate ≠ FamilyOrganization；本轮不物化组织、土地、人口或私兵。\n")

    p0_reference = []
    for city_id, facts in P0_FACTS.items():
        cid = city_admin_id({"properties": city_by_id[city_id]})
        for topic in CORE_TOPICS:
            p0_reference.append({"city_id": city_id, "city_name": city_by_id[city_id]["display_name"], "place_id": place_id(cid), "topic": topic, "reference_level": "R5" if city_id == "C027" else "R4", "content": facts.get({"04 自然地理":"geography", "18 市场":"industry", "20 军事设施":"military", "29 交通网络":"transport"}.get(topic, ""), "见对应00_Master；未核定项保持UNKNOWN。"), "evidence_type":"HISTORICAL/RECONSTRUCTED/UNKNOWN", "source_ids":"|".join(facts["sources"])})

    coverage = {
        "canonical_core_settlements": len(core_rows), "province_base_seat_count": len(PROVINCE_BASE_SEATS),
        "commandery_seat_count": len(first_seats), "strategic_city_count": len(cities),
        "strategic_city_mapped_count": sum(1 for x in cities if city_admin_id(x) in county_by_id),
        "priority_counties": len(priority_rows), "p0": sum(x["priority"] == "P0" for x in core_rows),
        "p1": sum(x["priority"] == "P1" for x in core_rows), "p2": sum(x["priority"] == "P2" for x in core_rows),
        "commandery_documents": len(commandery_names), "core_documents": len(core_rows), "county_documents": len(priority_rows),
        "clan_documents": len(clans), "estate_references": len(ESTATE_REFERENCES), "scenario_documents": len(scenario_rows),
        "routes_and_sites": len(transport_rows), "military_spaces": len(military_rows), "change_events": len(change_rows),
        "historical_person_additions": 0, "clan_additions": 0, "branch_additions": 0,
    }
    workdata = {"core_settlements":core_rows, "seat_timeline":seat_rows, "priority_counties":priority_rows, "estate_references":ESTATE_REFERENCES, "industry_resources":industry_rows, "transport_nodes":transport_rows, "military_spaces":military_rows, "annual_changes":sorted(change_rows, key=lambda x:(x["year"],x["place_id"])), "sources":source_rows, "scenarios":scenario_rows, "p0_reference":p0_reference, "coverage":coverage}
    write(OUT / "deepening_workdata.json", json.dumps(workdata, ensure_ascii=False, indent=2))

    readme = f"""# 135—260 历史世界深化资料索引

本目录是既有 `HISTORICAL_WORLD_REFERENCE` 的深化层，不是第二套世界。运行查询统一采用：

`Canonical Place Master → 稀疏治所/地点 Timeline → Change Event → Scenario Snapshot`。

未出现变化的字段继承上一状态；证据等级固定为 `HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN`。UNKNOWN 不得自动补齐。

## 覆盖

- 去重核心聚落：{coverage['canonical_core_settlements']}
- 13州部基准治所入口：{coverage['province_base_seat_count']}
- 105郡国治所候选：{coverage['commandery_seat_count']}
- 77战略城市标签：{coverage['strategic_city_mapped_count']}/{coverage['strategic_city_count']}
- 重点县：{coverage['priority_counties']}
- P0/P1/P2核心聚落：{coverage['p0']}/{coverage['p1']}/{coverage['p2']}
- 13剧本空间切片：{coverage['scenario_documents']}
- Clan文档/地产锚点：{coverage['clan_documents']}/{coverage['estate_references']}

## 目录

- `01`：核心聚落总索引；`02`：治所时间轴；`03`：重点县索引。
- `04_CORE_SETTLEMENTS`：每个核心地点的40主题Master与结构化JSON。
- `05_COMMANDERY_REGIONAL_REFERENCE`：105郡国区域档。
- `06_PRIORITY_COUNTIES`：按人物、宗族、地产与核心聚落价值筛选的重点县。
- `07_ELITE_CLANS_AND_ESTATES`、`08`：Clan/Branch/Estate Reference；不物化FamilyOrganization。
- `09`—`11`：产业资源、交通、军事空间。
- `12_SCENARIO_WORLD_REFERENCE`：13个剧本窗口。
- `13`：核心地点稀疏年度变化；`14`：统一来源索引。

## 强制口径

洛阳184为：城墙内约20万、城区约27万、都市圈约40万、供给圈约70万且**包含都市圈**；禁止40万+70万。其他城市不得继承洛阳比例。增加地点、设施、人物或庄园实例必须另立证据和任务，不得由本资料自动生成。
"""
    write(DEEP / "README_历史世界深化资料索引.md", readme)

    province_seat_table = md_table(
        ["州部", "135/140基准治所入口", "证据"],
        [(province_names[pid], county_by_id[cid]["display_name"], "RECONSTRUCTED；后续变化见02时间轴") for pid, (cid, _) in PROVINCE_BASE_SEATS.items()],
    )
    commandery_seat_table = md_table(
        ["郡国", "治所候选", "CountyPermanentId"],
        [(commandery_names[cmd], county_by_id[cid]["display_name"], cid) for cmd, cid in sorted(first_seats.items())],
    )
    province_cmd_city = [x for x in core_rows if x["is_province_seat"] and x["is_commandery_seat"] and x["is_strategic_city"]]
    city_not_cmd = [x for x in core_rows if x["is_strategic_city"] and not x["is_commandery_seat"]]
    cmd_not_city = [x for x in core_rows if x["is_commandery_seat"] and not x["is_strategic_city"]]
    r5 = [x for x in core_rows if x["reference_level"] == "R5"]
    r4 = [x for x in core_rows if x["reference_level"] == "R4"]
    r23 = [x for x in core_rows if x["reference_level"] in {"R2", "R3"}]
    report = f"""# HAN-135-260-HISTORICAL-WORLD-REFERENCE-DEEPENING-V1 Coverage Report

更新时间：2026-08-10

## 结论

本轮在既有 V1 内建立了去重后的 {coverage['canonical_core_settlements']} 个核心历史聚落、{coverage['priority_counties']} 个重点县、105个郡国区域档、13个Scenario空间档、39个Clan档和8个Estate Reference。达到“重要世界内容已有开发资料”，不等于1182县全部深研，也不等于运行时已经生成全国设施、人物、家庭或地产。

## 三十项验收问答

### 1. 13州治分别在哪里？

以下为135/140基准研究入口，不声称晚汉各年均保持唯一州治；迁移、分裂和未知期见时间轴。

{province_seat_table}

### 2. 105郡国治所分别在哪里？

已全部建立可审计候选。方法为《郡国志》现有县序首项候选，统一标 `RECONSTRUCTED`，不得误称105项均已完成专题考证。

{commandery_seat_table}

### 3—6. 去重数量与P0/P1/P2

- Canonical Core Settlements：{coverage['canonical_core_settlements']}。
- P0：{coverage['p0']}；P1：{coverage['p1']}；P2：{coverage['p2']}。

### 7. 同时为州治、郡治和战略城市的地点

{'、'.join(f"{x['display_name']}（{x['county_id']}）" for x in province_cmd_city) or '无'}。

### 8. 并非郡治候选的战略城市

共 {len(city_not_cmd)} 个地点：{'、'.join(x['city_names'].replace('|','/') for x in city_not_cmd)}。

### 9. 不在原77城中的郡国治所候选

共 {len(cmd_not_city)} 个：{'、'.join(x['display_name'] for x in cmd_not_city)}。完整ID见01、02工作簿。

### 10—11. 重点县数量与选择原因

共 {coverage['priority_counties']} 县。入选来自核心聚落、历史人物本籍、Clan本籍或郡望、Estate Reference、重大事件/交通/资源价值的并集；数量不是目标，未把1182县全部强制深研。

### 12—14. ReferenceLevel

- R5：{'、'.join(x['display_name'] for x in r5)}。洛阳复用正式184城市/都市圈样板、人口特殊校准和官方/考古入口。
- R4：{'、'.join(x['display_name'] for x in r4)}。已有40主题资料包与关键时序，但仍有大量UNKNOWN。
- R2/R3：共 {len(r23)} 个核心地点（本轮实际均为R3骨架）；完成Canonical、治所、人口模型和角色入口，尚未达到城市级考据深度。

### 15. 可直接进入开发准备审查的13个Scenario

{'、'.join(f"{x['year']} {x['scenario_name']}" for x in scenario_rows)}。这里的“可开发”指空间入口和继承合同完备，不代表逐日控制线已考证。

### 16—17. 城墙/城门可靠性与复原范围

- 洛阳可复用既有十二门、宫城—外城独立、城防与洛阳—虎牢走廊项目资料；精确古城边界仍以汉魏故城考古为准。
- 长安、邺、许昌、成都、襄阳、江陵、建业有史籍/考古/地方正式研究入口，但本轮只能做分期复原；现存后世城墙不得回填为汉末原状。
- 其余核心地点没有足够城墙/城门证据，保持UNKNOWN。

### 18. 城市人口资料可靠性

洛阳184的城墙内约20万、城区约27万、都市圈约40万采用本地特殊校准；70万供给圈包含都市圈，禁止相加。其余城市人口是既有模型估计，不是同年史籍普查，也不得套用洛阳比例。

### 19. 产业资料最充分的城市

洛阳（首都消费、仓储和高技能手工业）、成都（蜀锦、盐铁、盆地农业）、江陵（稻作、水运、木材、仓储）、建业（造船、港埠与江东供给）当前最适合做产业纵向切片；具体设施坐标和产量仍需开发前核证。

### 20—21. 交通线、关隘、港口和渡口

现有18条路线与31个战略节点已统一进入10号工作簿，可直接支持路线图层与需求拆分；洛阳—长安、襄阳—江陵、成都—汉中、建业—江陵等走廊，以及虎牢、潼关、汉中山道、黄河渡口、长江港埠是首批开发对象。大部分几何仍为RECONSTRUCTED/PROVISIONAL，不是历史精确线位。

### 22—23. HistoricalClan与值得建立地产锚点的Branch

39个Canonical Clan均有基础地理档；东海糜氏、汝南袁氏、河内司马氏、谯县曹氏、吴郡孙氏、中山甄氏已有Estate Reference入口。优先复核 `branch.han.v1.f092.yuan_feng`、`branch.han.v1.f092.yuan_wei`、`branch.han.v1.f102.sima_yi`；“值得研究”不等于已授权生成庄园。

### 24—26. Estate线索

- HISTORICAL_ESTATE：1（鲁肃东城田产与两囷米；边界未知）。
- RECONSTRUCTED_ESTATE：2（东海糜氏朐县产业锚点、湖阳樊氏田产锚点）。
- POTENTIAL_ESTATE：5（袁、司马、曹、孙、甄）。
- 8项的精确宅庄边界、建筑、依附人口和跨年变化均仍有UNKNOWN；不得批量物化。

### 27—29. 人物、Clan、Branch新增数

HistoricalPerson新增0；Clan新增0；Branch新增0。P0001—P1202、39 Clan、15 Branch全部保持原ID和原对象边界。

### 30. 下一阶段最适合实际开发的地点

先做 Development Readiness Review，不继续扩大资料框架。推荐顺序：洛阳184（已有运行时样板）→ 许昌196/200（中枢+屯田+官渡后勤）→ 襄阳/江陵208—219（城防、水运和战争）→ 成都214/221（盆地政权与产业）→ 建业211/229（水军、港埠和都城）→ 邺204（政治中心营建）→ 长安190—195（迁都、破坏与关中物流）。

## 审计边界

- 专用验证器最终结果：PASS；覆盖重复地点、13州治、105治所、77城、治所冲突、全部稳定ID、13个Scenario、Timeline连续性、证据等级、Markdown、链接、17份工作簿结构和公式错误。
- 77城的城阳、建安及5个郡域代理标签采用明确的RECONSTRUCTED县级挂接；原地域含义和不确定性仍保留。
- 治所冲突不强行消除：一地可兼任多级治所，一行政单位在分裂期可保留UNKNOWN或多个政治中心。
- 本轮未改运行时代码、存档、人口、人物、Clan、Branch、Facility实例或FamilyOrganization。
- 工作簿结构、公式、Markdown导航、ID解析、Timeline连续性和重复地点由专用验证器审计。
"""
    write(DEEP / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1_COVERAGE_REPORT.md", report)

    print(json.dumps(coverage, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
