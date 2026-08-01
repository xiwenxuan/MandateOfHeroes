# M13-P2任务书：并州太行—汾河—河套—阴山稳定地理骨架

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖并州刺史部上党、太原、上、西河、五原、云中、定襄、雁门、
朔方九郡。完成后，并州九项人口来源全部具有临时稳定地理映射。

本批按盆地、山地、黄土高原、黄河冲积平原和山前走廊建立稳定地理身份：

- `geo.region.north.china.taihangshangdang`：太行山西麓与上党盆地；
- `geo.region.north.china.fenriverluliang`：汾河谷地与吕梁山东麓；
- `geo.region.northwest.china.northshaanxiloess`：陕北黄土高原与无定河；
- `geo.region.north.china.hetaoyellowbend`：黄河河套平原；
- `geo.region.north.china.yinshansouth`：阴山南麓与土默川；
- `geo.region.north.china.yanmensanggan`：雁门山地与桑干河盆地。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 上党郡 | `geo.region.north.china.taihangshangdang.southwestbasin` | 上党盆地与太行山西麓地理区 | 太行山西麓与上党盆地宏区 |
| 太原郡 | `geo.region.north.china.fenriverluliang.northcentralbasin` | 汾河中上游与太原盆地地理区 | 汾河谷地与吕梁山东麓宏区 |
| 西河郡 | `geo.region.north.china.fenriverluliang.westernluliangyellowvalleys` | 吕梁山西麓与黄河东岸河谷地理区 | 汾河谷地与吕梁山东麓宏区 |
| 上郡 | `geo.region.northwest.china.northshaanxiloess.northeastwudinghills` | 无定河流域与陕北黄土丘陵地理区 | 陕北黄土高原与无定河宏区 |
| 五原郡 | `geo.region.north.china.hetaoyellowbend.northcentraloasisplain` | 河套北部与黄河最北段平原地理区 | 黄河河套平原宏区 |
| 朔方郡 | `geo.region.north.china.hetaoyellowbend.southwestyellowriverplain` | 河套西南部与黄河沿岸平原地理区 | 黄河河套平原宏区 |
| 云中郡 | `geo.region.north.china.yinshansouth.southeasttumochuanplain` | 土默川平原与阴山东南麓地理区 | 阴山南麓与土默川宏区 |
| 定襄郡 | `geo.region.north.china.yinshansouth.southcentralfoothillplain` | 阴山南麓中部与山前平原地理区 | 阴山南麓与土默川宏区 |
| 雁门郡 | `geo.region.north.china.yanmensanggan.centraldatongbasin` | 雁门山北部与桑干河大同盆地地理区 | 雁门山地与桑干河盆地宏区 |

每个行政来源使用一条`single_provisional_commandery_bucket_v1`人口覆盖映射，
权重为10,000基点。本批新增6个宏区、9个`commandery_area`和9条映射。
所有坐标留空，`geometry_status=provisional`且`provisional=true`。

## 三、史料、地理与人口约束

- 九个行政来源继续使用P1校录的《后汉书》卷三十三户口记录；
- 九郡原始、有效合计均为115,011户、696,765口；
- 九项全部继续按`commandery`保存，不新增属国或其他行政类型；
- 两份公开古籍转录户口值一致，本批不生成任何人口修正；
- 雁门郡31,862户、249,000口原值继续完整保留；
- 现代地貌只用于临时索引，不表示东汉边界、移治范围、塞垣、屯田区、
  黄河故道或山前平原范围已经精确复原。

现代地理交叉核对资料：

- 山西省自然资源厅太行山地质地貌资料：
  <https://zrzyt.shanxi.gov.cn/ztzx/sxdzyjw/hydt/kpky/202201/t20220105_4380840.shtml>
- 中国地质调查局黄河“几”字湾、山西盆地与河套平原资料：
  <https://www.xian.cgs.gov.cn/kpzs/dlqg/202106/t20210617_673604.html>
- 巴彦淖尔市人民政府河套平原与五原资料：
  <https://www.bynr.gov.cn/zjbs/>
- 国家林业和草原局桑干河与大同盆地资料：
  <https://www.forestry.gov.cn/c/www/zhzs/658703.jhtml>
- 榆林市人民政府陕北黄土高原、毛乌素南缘与无定河资料：
  <https://yl.gov.cn/mlyl/zjyl/fjms/201902/t20190213_17547.html>

## 四、明确不做

- 不绘制精确郡界、县邑边界、古黄河河道、塞垣、屯田区或质心坐标；
- 不把现代山西、陕西、内蒙古、宁夏边界当作东汉并州边界；
- 不拆分人口到长子、晋阳、肤施、离石、九原、云中、善无、阴馆、
  临戎等县级或城市节点；
- 不把五原、朔方、云中、定襄四郡合并为同一北疆人口桶；
- 不为九郡生成现代估算或人口修正；
- 不填入`game_location_crosswalk.csv`，该工作保留至P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计142条，包含44个根宏区和98个郡国尺度子区；
2. 映射表累计98条，覆盖98个唯一行政来源；
3. 本批15个稳定ID和9个行政来源无遗漏、重复或孤立引用；
4. 六个宏区的直接子区数依次为1、2、1、2、2、1；
5. 并州九郡人口来源全部拥有一条P2临时映射；
6. 九郡仍恰好保留115,011户、696,765口，并且不产生人口修正；
7. 雁门郡31,862户、249,000口原值保持可审计；
8. 每个新增来源的映射权重严格等于10,000基点；
9. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
10. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
    `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计142条，其中44个根宏区、98个郡国尺度子区；
- 人口映射：累计98条、错误0条；并州九郡覆盖9/9；
- 游戏地点交叉表：仍为0条，未提前进入P3；
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：通过（稳定地理142、映射98、交叉表0）；
- 专项回归测试：37/37通过；
- 全工程编译：通过；
- 核心回归测试：104/104通过；
- Unity测试：未运行；本批仅修改离线CSV、JSON审计产物、文档和
  PowerShell校验，不涉及Unity运行时、场景或序列化；
- `git diff --check`：通过；
- 下一阶段建议：继续P2第十五批，建立交州岭南沿海—珠江水系—
  红河三角洲—中南半岛北部稳定地理骨架，并完成P2全国105项郡国覆盖。
