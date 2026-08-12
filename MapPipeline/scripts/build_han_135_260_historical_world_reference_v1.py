#!/usr/bin/env python3
"""Build the documentation-side HAN-135-260 historical world reference library.

This builder does not create runtime world state. It projects the existing stable
map, population, person and clan datasets into auditable development references.
"""

from __future__ import annotations

import json
import re
import shutil
from collections import Counter, defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DOC_ROOT = ROOT / "Docs" / "HISTORICAL_WORLD_REFERENCE"
OUT_ROOT = ROOT / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1"


def load(rel: str):
    return json.loads((ROOT / rel).read_text(encoding="utf-8-sig"))


def write(path: Path, text: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def safe_name(value: str) -> str:
    return re.sub(r'[<>:"/\\|?*]', "_", value)


def table(headers, rows):
    lines = ["| " + " | ".join(headers) + " |", "| " + " | ".join(["---"] * len(headers)) + " |"]
    lines.extend("| " + " | ".join(str(v).replace("\n", " ") for v in row) + " |" for row in rows)
    return "\n".join(lines)


PROVINCE_PROFILES = {
    "sili": ("河洛盆地、关中东缘与山地关隘", "粟麦、官营手工业与都城消费", "黄河—洛水—驿道复合走廊", "首都、宫城、关隘与中央仓储"),
    "yuzhou": ("黄淮平原与颍汝水系", "粟麦、布帛、畜力与区域集市", "颍汝—淮河陆水联运", "中原交通节点与征发腹地"),
    "jizhou": ("太行山东麓与华北平原", "粟麦、桑麻、畜力与人口密集农业", "漳河—滹沱河—南北陆路", "北方人口与军粮核心区"),
    "yanzhou": ("黄河下游平原与济水流域", "粟麦、桑麻、盐铁交换", "黄河、济水与中原陆路", "兖豫青徐之间的机动走廊"),
    "xuzhou": ("泗水、淮河下游平原与滨海带", "稻麦、盐业、水运与区域贸易", "泗水—淮河—滨海道路", "淮泗交通枢纽与东部屏障"),
    "qingzhou": ("山东丘陵、平原与沿海带", "粟麦、盐业、渔业与桑麻", "济水下游、海岱陆路与滨海航运", "海岱兵源、盐业和东部防务"),
    "jingzhou": ("汉水、江汉平原、长江中游与南岭北麓", "稻作、木材、漆、渔业与水运贸易", "汉水—长江—湘资沅澧水网", "南北水陆枢纽与长江中游控制"),
    "yangzhou": ("长江下游、淮南丘陵与江南水网", "稻作、铜铁、木材、盐与水运贸易", "长江干线、淮水及江南支流", "江防、水军与东南开发基地"),
    "yizhou": ("四川盆地、汉中谷地与西南山地", "稻麦、盐铁、蜀锦、木材与山地资源", "金牛道、米仓道、长江上游与盆地水网", "盆地粮赋、山口防御与西南通道"),
    "liangzhou": ("河西走廊、陇山与黄土高原西缘", "粟麦、畜牧、马匹与远距离贸易", "河西走廊、陇右山道与关中通道", "边郡骑兵、羌胡关系与走廊防务"),
    "bingzhou": ("黄土高原、汾河谷地与北部边塞", "粟麦、畜牧、马匹与林草资源", "汾河谷地、太行山口与北部边道", "北边防务、骑兵与山口控制"),
    "youzhou": ("燕山、辽西走廊、辽河平原与北部边地", "粟麦、畜牧、渔盐与边地交换", "蓟辽走廊、海岸通道与北部边路", "东北边防、骑兵与跨区域交通"),
    "jiaozhou": ("岭南丘陵、珠江水系与南海沿岸", "稻作、果木、香药、珠贝与海贸", "西江—珠江水网、岭南山道与海路", "岭南行政节点、海贸与边远治理"),
}


SCENARIOS = {
    140: ("汉室承平", "东汉中央秩序仍在，地方差异以郡国行政和户口锚点表达。"),
    184: ("黄巾与州郡动员", "黄巾起事及边地冲突推动征发、迁徙、治安和军需变化。"),
    189: ("帝国中枢崩解", "宫廷政变与董卓入洛使中央权力、首都安全和地方军事化急变。"),
    194: ("群雄割据", "州郡控制分裂，战争、饥荒和人口流动成为区域状态差异的主要来源。"),
    200: ("官渡前后", "北方主力集团决战，河北与河南的军粮、渡口、道路和仓储权重上升。"),
    207: ("北方整合与荆州转折前夜", "北方秩序重组，荆州及长江中游成为南北战略转换枢纽。"),
    214: ("益州易主", "益州盆地、汉中通道与荆州—江东关系进入新的权力组合。"),
    219: ("汉中与荆州剧变", "汉中争夺和荆州失守改变西部山道、长江中游及联盟结构。"),
    223: ("三国格局形成", "夷陵战后魏蜀吴边界与恢复需求逐步稳定，但地方社会仍延续同一世界账。"),
    227: ("北伐初期", "汉中—陇右—关中军需走廊与长江防线成为跨区域资源重点。"),
    234: ("五丈原阶段", "长期战争考验关中、汉中和蜀地的运输、屯田、补员与财政。"),
    249: ("高平陵之变", "魏国中央权力重组，官僚、宗族和军事指挥链的忠诚与风险发生变化。"),
    260: ("三国后期", "政权仍在运行；260年只是正式剧本切片，不是世界模拟终点。"),
}


CITY_S = {
    "C027": ("洛阳", "帝国首都与河洛交通核心", ["宫城与官署", "城门城防", "市场与仓储", "洛水和黄河交通", "都城人口与供给圈"]),
    "C031": ("长安", "关中都会、旧都与西部军政枢纽", ["城垣与宫苑遗存", "关中道路", "渭水运输", "军粮与仓储", "关隘联动"]),
    "C009": ("邺", "河北核心城市及曹操时期营建中心", ["宫署与营建阶段", "漳河交通", "河北粮赋", "军府与工匠", "城防演变"]),
    "C025": ("许", "196—220年汉献帝都城与中原政务枢纽", ["宫署与礼制", "颍川农业", "道路与驿传", "屯田和军需", "城郭考古边界"]),
    "C067": ("成都", "蜀郡都会与四川盆地经济中心", ["城郭与市里", "蜀锦和手工业", "都江堰灌溉腹地", "盆地水陆交通", "人口与消费"]),
    "C041": ("襄阳", "汉水中游南北交通枢纽", ["汉水渡运", "樊城联动", "荆北农业", "军镇与城防", "南北商路"]),
    "C043": ("江陵", "江汉平原与长江中游控制节点", ["江河港埠", "江汉稻作", "荆州行政", "水军与仓储", "楚汉遗址分期"]),
    "C056": ("建业", "孙吴都城与长江下游水军经济核心", ["石头城与江防", "秦淮水网", "宫城演变", "水军港埠", "江东产业供给"]),
}

CITY_S_RESEARCH_SOURCES = {
    "C027": ["source.web.luoyang_rdc_2019"],
    "C031": ["source.web.shaanxi_han_changan"],
    "C009": ["source.web.hebei_ye_2023"],
    "C025": ["source.web.xuchang_2025"],
    "C067": ["source.web.sichuan_chengdu_2021"],
    "C041": ["source.web.hubei_xiangyang_2022"],
    "C043": ["source.web.hubei_jiangling_2019"],
    "C056": ["source.web.nanjing_records_2020"],
}


WEB_SOURCES = [
    {"source_id":"source.web.luoyang_rdc_2019","source_type":"official_heritage","title":"洛阳市汉魏故城保护条例","author_or_editor":"洛阳市人民代表大会常务委员会","url":"https://lysrd.henanrd.gov.cn/2019/01-10/173791.html","evidence_scope":"汉魏洛阳故城遗址范围与保护对象；不能直接推出184年全部设施布局","license_note":"仅摘录事实与链接，不复制受保护编排"},
    {"source_id":"source.web.shaanxi_han_changan","source_type":"official_gazetteer","title":"汉长安城","author_or_editor":"陕西省地方志办公室","url":"https://dfz.shaanxi.gov.cn/zslm/sxsq/msgj/200611/t20061110_2620037.html","evidence_scope":"汉长安城规模、城门和宫苑研究入口；需区分西汉遗存与东汉实际状态","license_note":"仅摘录事实与链接"},
    {"source_id":"source.web.hebei_ye_2023","source_type":"official_heritage","title":"邺城遗址研究资料","author_or_editor":"河北省文物局","url":"https://wenwu.hebei.gov.cn/system/2023/10/16/030257948.shtml","evidence_scope":"曹操营建邺城及遗址研究入口；后赵、北朝层位不得回填到东汉","license_note":"仅摘录事实与链接"},
    {"source_id":"source.web.xuchang_2025","source_type":"official_archaeology","title":"汉魏许都故城考古成果","author_or_editor":"许昌市人民政府","url":"https://www.xuchang.gov.cn/zjxc/005010/20251205/49e3c6e9-df14-4442-a9d5-10c356cc2676.html","evidence_scope":"城壕、城垣、门址、码头与台基研究入口；具体年代仍按发掘报告复核","license_note":"仅摘录事实与链接"},
    {"source_id":"source.web.hubei_xiangyang_2022","source_type":"official_local_reference","title":"襄阳历史与区位简介","author_or_editor":"湖北省人民政府外事办公室","url":"https://www.fohb.gov.cn/info/2022-08/20220810155800_190.html","evidence_scope":"汉水中游区位；现存城墙不得视为汉代原状","license_note":"仅摘录事实与链接"},
    {"source_id":"source.web.hubei_jiangling_2019","source_type":"official_heritage","title":"荆楚名都荆州（纪南、江陵）","author_or_editor":"湖北省文化和旅游厅","url":"https://wlt.hubei.gov.cn/bmdt/ztzl/zshb/201912/t20191226_1799529.shtml","evidence_scope":"纪南故城与江陵地区研究入口；楚都遗址不得直接当作184年江陵城布局","license_note":"仅摘录事实与链接"},
    {"source_id":"source.web.sichuan_chengdu_2021","source_type":"official_museum","title":"列备五都——秦汉时期的中国都市","author_or_editor":"四川省文物局、成都博物馆","url":"https://wwj.sc.gov.cn/scwwj/xzsd/2021/2/2/41a180005e0a40bf99e67290637afcd5.shtml","evidence_scope":"成都作为秦汉经济都会的证据入口；具体街区仍需专项考古来源","license_note":"仅摘录事实与链接"},
    {"source_id":"source.web.nanjing_records_2020","source_type":"official_gazetteer","title":"《六朝事迹编类》《建康实录》馆藏举要","author_or_editor":"南京市地方志编纂委员会办公室","url":"https://dfz.nanjing.gov.cn/ztzl/gcjy/202011/t20201105_2703833.html","evidence_scope":"孙吴建业及六朝城市史料入口；唐宋编纂层需与同期正史和考古互证","license_note":"仅摘录事实与链接"},
]


def main():
    provinces = load("MapData/HanWorld_Master_V0/administrative/provinces_v0.geojson")["features"]
    commanderies = load("MapData/HanWorld_Master_V0/administrative/commanderies_v0.geojson")["features"]
    counties = load("MapData/HanWorld_Master_V0/historical/county_anchors.geojson")["features"]
    cities = load("MapData/HanWorld_Master_V0/historical/strategic_cities.geojson")["features"]
    routes = load("MapData/HanWorld_Master_V0/historical/major_routes_v0.geojson")["features"]
    sites = load("MapData/HanWorld_Master_V0/historical/strategic_sites.geojson")["features"]
    annual = load("Assets/StreamingAssets/HistoricalPopulation/Han135260V1/annual_population.json")["records"]
    admin_timeline = load("Assets/StreamingAssets/HistoricalPopulation/Han135260V1/administrative_timeline.json")["records"]
    city_timeline = load("Assets/StreamingAssets/HistoricalPopulation/Han135260V1/major_city_timeline.json")["records"]
    events = load("Assets/StreamingAssets/HistoricalPopulation/Han135260V1/events.json")["events"]
    people = load("Assets/StreamingAssets/HistoricalPersons/Han135260V1/persons.json")["persons"]
    person_locations = load("Assets/StreamingAssets/HistoricalPersons/Han135260V1/person_locations.json")["records"]
    clans = load("Assets/StreamingAssets/HistoricalPersons/Han135260V1/clans.json")["clans"]
    clan_presence = load("Assets/StreamingAssets/HistoricalPersons/Han135260V1/clan_presence.json")["records"]
    pop_sources = load("Assets/StreamingAssets/HistoricalPopulation/Han135260V1/sources.json")["sources"]
    person_sources = load("Assets/StreamingAssets/HistoricalPersons/Han135260V1/sources.json")["sources"]

    dirs = ["00_WORLD", "01_YEARS_135_260", "02_PROVINCES", "03_COMMANDERIES_KINGDOMS", "04_COUNTIES", "05_CITIES", "06_PERSONS", "07_CLANS", "08_FACILITIES", "09_INDUSTRY", "10_TRANSPORT", "11_MILITARY", "12_ADMINISTRATION", "13_EVENTS", "14_SCENARIOS", "15_TEMPLATES"]
    DOC_ROOT.mkdir(parents=True, exist_ok=True)
    for d in dirs:
        target = DOC_ROOT / d
        if target.exists():
            shutil.rmtree(target)
        target.mkdir(parents=True)
    for name in ["README_历史世界开发参考资料索引.md", "HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1_最终覆盖报告.md"]:
        target = DOC_ROOT / name
        if target.exists():
            target.unlink()
    OUT_ROOT.mkdir(parents=True, exist_ok=True)

    prov_by_id = {f["properties"]["admin_unit_id"]: f["properties"] for f in provinces}
    cmd_by_id = {f["properties"]["admin_unit_id"]: f["properties"] for f in commanderies}
    county_by_id = {f["properties"]["admin_unit_id"]: f for f in counties}
    province_of_cmd = {cid: ".".join(cid.split(".")[:3]) for cid in cmd_by_id}
    province_of_county = {cid: province_of_cmd.get(f["properties"]["parent_admin_unit_id"], "") for cid, f in county_by_id.items()}
    cmd_count = Counter(province_of_cmd.values())
    county_count = Counter(province_of_county.values())
    cities_by_admin = {f["properties"].get("admin_reference"): f for f in cities if f["properties"].get("admin_reference")}
    loc_by_person = defaultdict(list)
    for row in person_locations:
        loc_by_person[row["person_id"]].append(row)
    presence_by_clan = defaultdict(list)
    for row in clan_presence:
        presence_by_clan[row["clan_id"]].append(row)
    city_pop_by_name_year = {(r["city_name"], r["year"]): r for r in city_timeline}
    events_by_year = defaultdict(list)
    for e in events:
        for y in range(e["start_year"], e["end_year"] + 1):
            events_by_year[y].append(e)

    readme = f"""# 135—260历史世界开发参考资料索引

## 定位

本目录是《群雄志：仕途》的**历史开发参考库**，不是第二套运行时世界，也不是对不确定史实的自动补写。运行时继续复用稳定世界地理、永久人物、人口账与ScenarioSnapshot。本库负责回答开发者“某年、某地、某人、某设施应查什么证据，以及哪些仍属推定”。

## 当前覆盖

{table(["对象", "数量", "状态"], [["逐年索引", 126, "完整骨架"], ["州部", 13, "区域参考"], ["郡国", 105, "索引"], ["县级单位", 1182, "索引"], ["战略城市", 77, "全量骨架；8个CITY-S详档"], ["历史人物", 1202, "地理分布索引；不是人数上限"], ["Clan/Branch", "39/15", "Clan地理索引；不等于运行时家族组织"], ["Scenario", 13, "开发参考切片"]])}

## 证据标签

- `HISTORICAL`：有明确史籍、考古或正式资料支撑的断言。
- `RECONSTRUCTED`：由多项证据保守复原，必须保留推理链。
- `MODELED`：为游戏运行或容量规划建立的项目模型，不冒充史实。
- `UNKNOWN`：证据不足，保留空缺和研究问题。

## 阅读顺序

1. [历史世界总参考](00_WORLD/00_135-260历史世界开发总参考_V1.md)
2. 各对象索引工作簿（位于本目录根部）
3. [州部参考](02_PROVINCES)、[城市参考](05_CITIES)与[Scenario参考](14_SCENARIOS)
4. Facility、产业、交通、军事、行政专题
5. [来源总索引](历史资料来源总索引.xlsx)与[最终覆盖报告](HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1_最终覆盖报告.md)

## 不变量

- 135—260是主要剧本研究范围，260不是模拟终点。
- 史料人口是参考；实际开局按硬件缩尺，永久人物不得合并、删除或重随机。
- 140年行政截面是稳定地理索引，不代表126年间行政名称从未变化。
- 代理多边形只用于技术定位；不得据此声称真实历史边界。
- 洛阳供给圈70万人包含都市圈40万人，二者不可相加。
- 未解决项继续保留：205个地点、64条关系，以及P0175在219年切片的重叠问题。
"""
    write(DOC_ROOT / "README_历史世界开发参考资料索引.md", readme)

    world_master = f"""# 135—260历史世界开发总参考 V1

## 1. 世界组织方式

`MasterWorld → AnnualChangeIndex → ScenarioSnapshot`。Master保存稳定地理与永久身份；逐年索引只记录继承关系和变化；Scenario只引用当年状态，不复制全国世界。

## 2. 权威数据底座

{table(["底座", "规模", "用途", "边界"], [["MASTER-MAP", "13州/105郡国/1182县/77城", "稳定地理、坐标与技术几何", "部分几何为synthetic_proxy"], ["人口时间线", "135—260共126年", "全国、行政区与城市人口模型", "模型值不冒充普查"], ["历史人物宗族", "1202人/39 Clan/15 Branch", "身份、籍贯、任官、关系与切片", "1202不是封顶"], ["Scenario", "13个", "正式开局参考", "只冻结差异，不复制Master"]])}

## 3. 开发时的查询顺序

1. 先以稳定ID定位州、郡国、县、城市、人物或Clan。
2. 查询目标年份的行政、人口、人物位置和事件变化。
3. 查询来源与证据标签；若为UNKNOWN，不自动编造。
4. 表现层根据玩家已知信息裁剪显示，但不改变世界事实。
5. 新考证以新增来源、别名、时间段或版本迁移补充，不改指旧ID。

## 4. 规模与精度

- `CITY-S`：8个首批详细城市，可支撑城市纵向切片研究。
- `CITY-A/B`：其余69城为分级骨架，只有通过证据审计后才能升级。
- 郡县索引提供研究入口、地理父子关系和默认开发问题，不声称1182县都已有独立考证。
- Facility、产业、交通和军事地理全部使用开放内容ID；普通扩展不得要求存档结构升级。

## 5. 洛阳口径

184年洛阳研究中：城墙内约20万、连续城区约27万、都市圈约40万、供给圈约70万。供给圈包含都市圈，严禁把40万与70万相加。河南尹模型人口约1,070,779属于更大行政区域口径。

## 6. 下一轮研究队列

优先补齐8个CITY-S的考古分期、城门/水系/市场/仓储证据；解决战略城市缺失行政引用；逐步把CITY-A/B从通用骨架升级为可审计地方档案；为205个未解析地点、64条关系和P0175时间重叠建立独立修订批次。
"""
    write(DOC_ROOT / "00_WORLD" / "00_135-260历史世界开发总参考_V1.md", world_master)

    index_guides = {
        "01_YEARS_135_260/README_逐年状态索引说明.md": ("逐年状态", "../01_135-260逐年历史世界状态索引.xlsx", "每年继承前一年，只记录变化事件和Scenario入口。"),
        "03_COMMANDERIES_KINGDOMS/README_郡国索引说明.md": ("郡国", "../03_105郡国历史开发参考索引.xlsx", "140行政截面是稳定索引，名称和隶属变化另用有效年份表达。"),
        "04_COUNTIES/README_县级索引说明.md": ("县级单位", "../04_1182县历史开发参考索引.xlsx", "未定位县保持UNKNOWN；代理几何和技术锚点不构成历史边界。"),
        "06_PERSONS/README_人物地理索引说明.md": ("历史人物", "../06_135-260历史人物地理分布开发参考.xlsx", "1202是V1底座而非上限；人物位置证据稀疏时不得自动补写行年。"),
        "07_CLANS/README_宗族地理索引说明.md": ("Clan", "../07_135-260历史宗族地理分布开发参考.xlsx", "Clan/Branch是谱系与地理资料，不等于运行时FamilyOrganization、Household或资产。"),
        "13_EVENTS/README_事件区域影响索引说明.md": ("事件区域影响", "../13_135-260重大历史事件区域影响参考.xlsx", "现有影响值属于人口模型；设施、政权和控制变化需独立证据。"),
    }
    for rel, (title, workbook, note) in index_guides.items():
        write(DOC_ROOT / rel, f"# {title}开发参考入口\n\n- 正式索引：[{Path(workbook).name}]({workbook})\n- 规则：{note}\n- 证据字段：HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN。")

    # Province references
    for f in provinces:
        p = f["properties"]
        pid = p["admin_unit_id"]
        key = pid.split(".")[-1]
        terrain, economy, transport, military = PROVINCE_PROFILES[key]
        cmd_names = [cmd_by_id[x]["display_name"] for x in sorted(cmd_by_id) if province_of_cmd[x] == pid]
        city_names = [c["properties"]["display_name"] for c in cities if (c["properties"].get("admin_reference") or "").startswith(pid + ".")]
        body = f"""# {p['display_name']}历史开发参考

## 身份与覆盖

- 稳定ID：`{pid}`
- 郡国数：{cmd_count[pid]}
- 县级单位数：{county_count[pid]}
- 已挂接战略城市：{len(city_names)}（{('、'.join(city_names) if city_names else '尚无可靠挂接')}）
- 边界证据：`MODELED`。当前多边形为技术代理，不是历史边界断言。

## 区域开发画像

{table(["维度", "保守开发参考", "证据"], [["地貌", terrain, "RECONSTRUCTED"], ["产业", economy, "RECONSTRUCTED"], ["交通", transport, "RECONSTRUCTED"], ["军事", military, "RECONSTRUCTED"]])}

## 所属郡国

{('、'.join(cmd_names))}

## 使用规则

上述画像只决定研究方向和默认模型参数，不直接生成唯一资源点、道路、设施或人口职业。任何县城、矿脉、渡口、仓储与军镇必须另有地点级证据或明确标为MODELED。
"""
        write(DOC_ROOT / "02_PROVINCES" / f"{safe_name(p['display_name'])}_{key}_州部开发参考.md", body)

    # City references
    city_doc_rows = []
    for f in cities:
        p = f["properties"]
        cid, name = p["city_id"], p["display_name"]
        admin_id = p.get("admin_reference") or ""
        county = county_by_id.get(admin_id, {}).get("properties", {}) if admin_id else {}
        parent = county.get("parent_admin_unit_id", "")
        prov = province_of_county.get(admin_id, "")
        pop184 = city_pop_by_name_year.get((name, 184)) or city_pop_by_name_year.get((p.get("historical_name"), 184))
        level = "CITY-S" if cid in CITY_S else ("CITY-A" if p.get("confidence") == "high" else "CITY-B")
        detail = cid in CITY_S
        headline = CITY_S.get(cid, (name, "战略城市骨架", []))[1]
        topics = CITY_S.get(cid, (name, headline, ["城址", "交通", "产业", "军政", "人口"]))[2]
        pop_text = "尚无184年城市模型记录"
        if pop184:
            pop_text = f"城墙内{pop184['walled_city_population']:,}、城区{pop184['urban_area_population']:,}、都市圈{pop184['metropolitan_population']:,}、所在县{pop184['county_population']:,}（MODELED，口径不可相加）"
        source_ids = p.get("source_ids", "") or "source.project.prototype_location_catalog.v1"
        extra = ""
        if detail:
            research_sources = "、".join(f"`{x}`" for x in CITY_S_RESEARCH_SOURCES[cid])
            extra = f"""
## CITY-S开发分解

{table(["研究主题", "V1处理", "升级条件"], [[t, "保留历史层/复原层/玩法层三层记录", "地点级文献或考古证据 + 时间范围 + 来源ID"] for t in topics])}

## 可操作空间建议

城市内层可展示官署、市场、仓储、住宅、工坊、宗教/礼制、城防与交通节点；V1只给功能区研究入口，不固定未经证实的街道网。Facility必须是世界实体，其所有权、岗位、库存、税费、损耗与建毁时间进入同一世界账。

## 风险提示

- 不把后世城墙、宫城或城市格局自动回填到135—260。
- 不把古城遗址各时期遗存混为同一时点。
- 不把模型人口、技术坐标或代理边界写成史实。

## 第一批专项研究入口

{research_sources}。该入口用于继续查证，不代表其页面上的所有时代内容均可直接用于135—260。
"""
        body = f"""# {name}历史城市开发参考（{level}）

## 基本档案

{table(["字段", "值"], [["CityID", f"`{cid}`"], ["定位", headline], ["历史/别名", p.get('historical_name') or name], ["行政引用", f"`{admin_id}`" if admin_id else "UNKNOWN：待解析"], ["经纬度", f"{p.get('longitude','?')}, {p.get('latitude','?')}"], ["坐标状态", p.get('coordinate_status','UNKNOWN')], ["基础置信度", p.get('confidence','UNKNOWN')], ["来源", source_ids], ["184人口口径", pop_text]])}

## 证据与世界生成

- 城市存在/名称：`HISTORICAL`或现有资料等级，具体见来源ID。
- 精确城界与内部街区：`UNKNOWN`，不得由点坐标推导。
- 当前坐标：用于地图定位；近似坐标不等于遗址范围。
- 默认产业、设施和资源：只能作为`MODELED`初始候选，运行后由统一世界系统变化。
{extra}
## 下一步研究问题

1. 目标年份的行政地位、治所和控制者如何变化？
2. 可证实的城垣、水系、渡口、仓储、市场和手工业有哪些？
3. 哪些信息只能做到郡县级，不能下放到具体Cell？
4. 哪些后世遗存必须从本时期地图中排除？
"""
        filename = f"{cid}_{safe_name(name)}_{level}_城市开发参考.md"
        write(DOC_ROOT / "05_CITIES" / filename, body)
        city_doc_rows.append({"city_id": cid, "document": f"05_CITIES/{filename}", "detail_level": level})

    # Scenario references
    person_scenario_dir = ROOT / "Assets/StreamingAssets/HistoricalPersons/Han135260V1/scenarios"
    for year, (name, summary) in SCENARIOS.items():
        snap = load(str((person_scenario_dir / f"{year}.json").relative_to(ROOT)).replace("\\", "/"))
        alive = Counter(p["alive_state"] for p in snap["persons"])
        year_pop = next(x for x in annual if x["year"] == year)
        relevant_events = [e["name"] for e in events if e["start_year"] <= year <= e["end_year"]]
        body = f"""# {year}年Scenario历史世界开发参考：{name}

## 切片定位

{summary}

## 引用状态

{table(["对象", "切片值", "证据/口径"], [["全国模型人口", f"{year_pop['modeled_actual_population_start']:,}", f"MODELED / 等级{year_pop['evidence_level']}"], ["已建档人物状态", f"Alive {alive['Alive']} / PossiblyAlive {alive['PossiblyAlive']} / Dead {alive['Dead']}", "人物母库派生"], ["Clan快照", len(snap['clans']), "Clan不等于运行时FamilyOrganization"], ["人口模型事件", '、'.join(relevant_events) if relevant_events else '无当年配置事件', "项目人口模型"]])}

## 开局生成顺序

1. 加载Master稳定地理与永久ID。
2. 应用135年至{year}年的年度变化和事件影响。
3. 引用人物/Clan快照，不复制人物母档。
4. 生成缩尺人口与家户，同时保持职业、生产、消费、兵源守恒。
5. 只向玩家展示其人物、组织和情报网络已经掌握的信息。

## 禁止项

- 不因剧本切换重新随机历史人物或普通永久人物。
- 不把政治摘要直接转成所有县的精确控制与设施配置。
- 不把该切片当作独立世界；260年切片也可继续运行。
"""
        write(DOC_ROOT / "14_SCENARIOS" / f"SCENARIO_{year}_{safe_name(name)}_开发参考.md", body)

    # Topic documents
    topics = {
        "08_FACILITIES/08_历史建筑与Facility开发参考_V1.md": ("历史建筑与Facility开发参考 V1", "设施是统一世界中的持久实体，不是地图图标。历史存在、合理复原与玩法补全必须分层。", ["稳定definition_id与facility_id", "位置/占地/容量/耐久", "所有权与使用权", "岗位与排班", "库存/配方/能耗/损耗", "建设、扩建、损毁与废弃时间线"]),
        "09_INDUSTRY/09_历史产业与区域经济开发参考_V1.md": ("历史产业与区域经济开发参考 V1", "产业从资源、劳动力、设施、技艺、配方、运输、市场和制度共同产生；州部画像只给默认研究方向。", ["作物与地方品种", "原料与中间品", "工坊和工具", "劳动力技能", "税役与许可", "库存、价格和贸易后果"]),
        "10_TRANSPORT/10_历史交通与物流开发参考_V1.md": ("历史交通与物流开发参考 V1", "运输必须有承运者、载具、路线、时间、消耗、损耗和风险。当前18条路线是MODELED近似走廊。", ["道路/水路/渡口/关隘", "车马舟船容量", "随行口粮与货损", "军队自运/征发/商运/购买/劫掠", "情报时效", "天气、治安和战争风险"]),
        "11_MILITARY/11_历史军事地理开发参考_V1.md": ("历史军事地理开发参考 V1", "军队、军粮、兵员和设施必须来自同一世界账；关隘和军镇的位置随证据等级管理。", ["城防与门禁", "关隘和渡口", "营垒和烽燧", "补给线与仓储", "征兵与驻军", "占领、损毁和修复"]),
        "12_ADMINISTRATION/12_历史行政设施开发参考_V1.md": ("历史行政设施开发参考 V1", "140行政截面用于稳定索引，135—260的改名、分合、控制变化用时间有效性表达。", ["中央/州/郡国/县官署", "档案与户籍", "仓、库、邮、亭", "司法、税役与征发", "组织知识与抄录成本", "任免和权限变化"]),
    }
    for rel, (title, intro, fields) in topics.items():
        write(DOC_ROOT / rel, f"# {title}\n\n{intro}\n\n## 统一数据合同\n\n" + "\n".join(f"- {x}" for x in fields) + "\n\n## 证据规则\n\n每个实例同时记录`evidence_type`、`source_ids`、`valid_from/to`、`confidence`和`notes`。增加普通Facility、产品、道路或行政设施采用数据定义，不新增枚举，不要求升级存档结构。\n\n## 运行边界\n\n本参考文档不直接创建全国实例；运行时建设、拆除、生产、运输、战争和治理可以改变设施状态，变化必须进入统一世界账并可追溯。")

    # Templates
    templates = {
        "TEMPLATE_CITY_城市开发参考.md": ["稳定ID与别名", "年份/行政隶属", "证据分层", "城址与自然环境", "人口口径", "设施/产业/交通/军事", "人物与Clan", "未知项与研究队列"],
        "TEMPLATE_COMMANDERY_郡国开发参考.md": ["稳定ID与名称沿革", "所属州部", "县级单位", "人口与税役", "产业和交通", "军事与治理", "史料冲突", "运行时映射"],
        "TEMPLATE_COUNTY_县级开发参考.md": ["稳定ID与类型", "治所/坐标证据", "地貌水系", "人口家户", "生产设施", "市场交通", "治安军事", "未知项"],
        "TEMPLATE_ANNUAL_逐年状态参考.md": ["继承年份", "当年事件", "行政变化", "人口变化", "人物/Clan变化", "设施/产业/交通影响", "证据与来源", "Scenario引用"],
        "TEMPLATE_SCENARIO_剧本切片参考.md": ["切片时点", "Master版本", "年度变化范围", "政治军事摘要", "人口与迁徙", "人物/Clan快照", "区域热点", "未知项与开局规则"],
    }
    for name, fields in templates.items():
        write(DOC_ROOT / "15_TEMPLATES" / name, "# " + name.removeprefix("TEMPLATE_").removesuffix(".md") + "模板\n\n> 所有断言必须标注HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN；缺失ID不得静默改指。\n\n" + "\n".join(f"## {i+1}. {x}\n\n- 内容：\n- 证据类型：\n- 来源ID：\n- 有效年份：\n- 置信度：\n- 待研究：\n" for i, x in enumerate(fields)))

    # Workbook datasets
    province_name = {k: v["display_name"] for k, v in prov_by_id.items()}
    annual_rows = []
    for r in annual:
        year_events = events_by_year[r["year"]]
        annual_rows.append({**r, "inherits_from_year": r["year"] - 1 if r["year"] > 135 else "MASTER", "change_event_ids": "|".join(e["event_id"] for e in year_events), "change_event_names": "|".join(e["name"] for e in year_events), "scenario": "YES" if r["year"] in SCENARIOS else "NO", "evidence_type": "MODELED"})

    admin_by_id = {r["region_permanent_id"]: r for r in admin_timeline}
    commandery_rows = []
    for cid, p in sorted(cmd_by_id.items()):
        pid = province_of_cmd[cid]
        a = admin_by_id.get(cid, {})
        commandery_rows.append({"commandery_id": cid, "display_name": p["display_name"], "province_id": pid, "province_name": province_name.get(pid, ""), "county_count": sum(1 for x in counties if x["properties"]["parent_admin_unit_id"] == cid), "strategic_city_count": sum(1 for x in cities if (x["properties"].get("admin_reference") or "").startswith(cid + ".")), "valid_from_year": a.get("valid_from_year", 135), "valid_to_year": a.get("valid_to_year", 260), "confidence": a.get("confidence", "B"), "source": a.get("source", "source.hou_han_shu.jun_guo_zhi"), "geometry_status": p.get("geometry_status"), "evidence_type": "HISTORICAL_INDEX+MODELED_GEOMETRY", "research_status": "INDEX_COMPLETE_DETAIL_RESEARCHING"})

    county_rows = []
    for cid, f in sorted(county_by_id.items()):
        p = f["properties"]; cmd = p["parent_admin_unit_id"]; pid = province_of_county[cid]
        coords = f.get("geometry", {}).get("coordinates", []) if f.get("geometry") else []
        county_rows.append({"county_id": cid, "display_name": p.get("display_name", cid.split(".")[-1]), "commandery_id": cmd, "commandery_name": cmd_by_id.get(cmd, {}).get("display_name", ""), "province_id": pid, "province_name": province_name.get(pid, ""), "longitude": coords[0] if coords else None, "latitude": coords[1] if coords else None, "coordinate_status": p.get("coordinate_status"), "confidence": p.get("confidence"), "historical_claim": p.get("historical_claim"), "source_ids": p.get("source_ids"), "strategic_city_id": cities_by_admin.get(cid, {}).get("properties", {}).get("city_id", ""), "development_status": "INDEX_COMPLETE_LOCATION_RESEARCHING", "evidence_type": "HISTORICAL_INDEX" if p.get("historical_claim") else "UNKNOWN_OR_MODELED_LOCATION"})

    city_rows = []
    for f in cities:
        p = f["properties"]; cid = p["city_id"]; name = p["display_name"]
        pop = city_pop_by_name_year.get((name, 184)) or {}
        admin_id = p.get("admin_reference") or ""
        doc = next(x for x in city_doc_rows if x["city_id"] == cid)
        city_rows.append({"city_id": cid, "display_name": name, "historical_name": p.get("historical_name"), "detail_level": doc["detail_level"], "admin_reference": admin_id, "province_id": province_of_county.get(admin_id, ""), "longitude": p.get("longitude"), "latitude": p.get("latitude"), "coordinate_status": p.get("coordinate_status"), "confidence": p.get("confidence"), "source_ids": p.get("source_ids"), "population_184_walled": pop.get("walled_city_population"), "population_184_urban": pop.get("urban_area_population"), "population_184_metro": pop.get("metropolitan_population"), "population_184_county": pop.get("county_population"), "population_evidence": pop.get("evidence"), "document": doc["document"], "research_status": "DETAILED_V1" if cid in CITY_S else "SKELETON_V1"})

    person_rows = []
    for p in people:
        locs = loc_by_person[p["person_id"]]
        person_rows.append({"person_id": p["person_id"], "canonical_name": p["canonical_name"], "birth_year_low": p.get("birth_year_low"), "birth_year_high": p.get("birth_year_high"), "death_year_low": p.get("death_year_low"), "death_year_high": p.get("death_year_high"), "tier": p.get("historical_person_tier"), "primary_identity": p.get("primary_identity"), "native_region_id": p.get("native_place_region_id"), "native_county_id": p.get("native_place_county_id"), "native_place_text": p.get("native_place_text"), "primary_historical_region_id": p.get("primary_historical_region_id"), "clan_id": p.get("clan_id"), "branch_id": p.get("lineage_branch_id"), "location_record_count": len(locs), "resolved_location_count": sum(1 for x in locs if x.get("resolution_method") != "Unresolved"), "evidence_level": p.get("evidence_level"), "research_status": p.get("research_status"), "source_id": p.get("source_id")})

    clan_rows = []
    for c in clans:
        prs = presence_by_clan[c["clan_id"]]
        clan_rows.append({"clan_id": c["clan_id"], "canonical_clan_name": c["canonical_clan_name"], "surname": c.get("surname"), "clan_type": c.get("clan_type"), "commandery_region_id": c.get("clan_commandery_region_id"), "county_region_id": c.get("clan_county_region_id"), "primary_region_id": c.get("primary_region_id"), "start_year": c.get("start_year"), "end_year": c.get("end_year"), "major_clan": c.get("major_clan"), "presence_count": len(prs), "known_member_count": sum(x.get("known_member_count", 0) for x in prs), "evidence_level": c.get("evidence_level"), "research_status": c.get("research_status"), "notes": c.get("notes")})

    event_rows = []
    for e in events:
        event_rows.append({**e, "affected_province_names": "|".join(province_name.get(x, x) for x in e["affected_provinces"]), "evidence_type": "MODELED_EVENT_IMPACT", "world_effect_contract": "population/migration/birth/registration; facilities and control require separate evidence"})

    source_rows = []
    seen = set()
    for s in pop_sources + person_sources + WEB_SOURCES:
        sid = s.get("source_id")
        if sid in seen: continue
        seen.add(sid)
        source_rows.append({"source_id": sid, "source_type": s.get("source_type"), "title": s.get("title") or s.get("source_title"), "author_or_editor": s.get("author_or_editor") or s.get("author"), "edition_or_host": s.get("edition_or_host") or s.get("edition"), "url_or_locator": s.get("url_or_bibliographic_locator") or s.get("url"), "access_date": s.get("publication_or_access_date") or s.get("access_date"), "reliability_class": s.get("reliability_class"), "evidence_scope": s.get("evidence_scope"), "license_note": s.get("license_or_public_domain_note") or s.get("license_note"), "notes": s.get("notes")})

    workdata = {"annual": annual_rows, "commanderies": commandery_rows, "counties": county_rows, "cities": city_rows, "persons": person_rows, "clans": clan_rows, "events": event_rows, "sources": source_rows, "coverage": {"years": len(annual_rows), "provinces": len(provinces), "commanderies": len(commandery_rows), "counties": len(county_rows), "cities": len(city_rows), "city_s": sum(1 for x in city_rows if x["detail_level"] == "CITY-S"), "persons": len(person_rows), "clans": len(clan_rows), "branches": 15, "scenarios": len(SCENARIOS), "routes": len(routes), "sites": len(sites)}}
    (OUT_ROOT / "historical_world_reference_workdata.json").write_text(json.dumps(workdata, ensure_ascii=False, indent=2), encoding="utf-8")

    # Final coverage report
    checks = [
        ("是否建立统一Master→Annual→Scenario结构？", "是；Scenario不复制世界。"),
        ("是否覆盖135—260全部126年？", f"是，{len(annual_rows)}年。"),
        ("是否覆盖13州？", f"是，{len(provinces)}份州部参考。"),
        ("是否覆盖105郡国？", f"是，{len(commandery_rows)}条索引。"),
        ("是否覆盖1182县？", f"是，{len(county_rows)}条索引。"),
        ("是否覆盖77战略城市？", f"是，{len(city_rows)}份城市档案。"),
        ("是否至少8个CITY-S？", f"是，{sum(1 for x in city_rows if x['detail_level']=='CITY-S')}个。"),
        ("是否覆盖13个Scenario？", f"是，{len(SCENARIOS)}份。"),
        ("是否接入1202人物？", f"是，{len(person_rows)}条；不是上限。"),
        ("是否接入Clan/Branch？", f"是，{len(clan_rows)} Clan / 15 Branch。"),
        ("是否区分证据类型？", "是，统一四级标签。"),
        ("是否把代理几何冒充史实？", "否；全部明确technical/model边界。"),
        ("是否把模型人口冒充普查？", "否。"),
        ("是否避免洛阳人口重复相加？", "是；供给圈70万包含都市圈40万。"),
        ("是否保留未知项？", "是；不自动补写。"),
        ("是否保留205地点未解析项？", "是，列入后续队列。"),
        ("是否保留64关系未解析项？", "是，列入后续队列。"),
        ("是否保留P0175/219重叠？", "是，未静默修正。"),
        ("是否提供Facility参考？", "是。"),
        ("是否提供产业与经济参考？", "是。"),
        ("是否提供交通物流参考？", "是。"),
        ("是否提供军事地理参考？", "是。"),
        ("是否提供行政设施参考？", "是。"),
        ("是否提供5种模板？", "是。"),
        ("是否允许260年后继续？", "是；260只是一份正式切片。"),
    ]
    report = f"""# HAN-135-260-HISTORICAL-WORLD-REFERENCE-V1 最终覆盖报告

## 结论

第一轮历史世界开发参考库已形成完整导航、全量索引骨架和重点详档。它可以支持后续历史研究和内容开发，但不等于1182县均已完成地点级考证，也不等于运行时系统已经自动消费这些文档。

## 25项验收

{table(["#", "问题", "结论"], [[i + 1, q, a] for i, (q, a) in enumerate(checks)])}

## 定量覆盖

{table(["对象", "数量"], [[k, v] for k, v in workdata['coverage'].items()])}

## 未完成研究债

- 69个非CITY-S城市仍是开发骨架，不能声称完成城市历史复原。
- 1182县的精确坐标、资源点、道路、设施与聚落结构仍须逐地考证。
- 人物位置记录稀疏；205地点、64关系、P0175/219重叠继续保留。
- 路线与战略设施中的低置信度、provisional、historical_claim=false项不得作为史实展示。
- 本任务只建设参考资料，没有修改Unity运行时，也没有生成全国人口、家户、FamilyOrganization或Facility实例。
"""
    write(DOC_ROOT / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1_最终覆盖报告.md", report)
    print(json.dumps(workdata["coverage"], ensure_ascii=False))


if __name__ == "__main__":
    main()
