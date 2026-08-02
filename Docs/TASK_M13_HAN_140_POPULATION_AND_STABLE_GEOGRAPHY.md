# M13任务书：140年郡国人口校录与稳定地理映射

## 一、任务定位

本任务落实系统总纲推荐建设顺序的第一项：

> 校录140年郡国人口，并建立不随行政区改名、分合和治所迁移而失效的稳定地理映射。

M13是M12永久人物存储压力原型的数据前置。编号只用于标识任务，不表示必须先完成
M12的实现；本任务完成后，M12才能获得可信的历史人口规模和空间分布输入。

本任务属于历史数据、地理数据和确定性校验任务，不直接改变当前V5存档。

## 二、必须读取的设计

执行前必须读取：

1. `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`
2. `Docs/HISTORICAL_POPULATION_135_260.md`
3. `Docs/WORLD_SIMULATION_FOUNDATION.md`
4. `Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md`
5. `Docs/DATA_AND_CONTENT_FOUNDATION.md`
6. `Docs/HISTORICAL_CITY_LIST.md`
7. `Docs/CITY_UNION_MASTER.md`
8. `Docs/PROTOTYPE_MAP_184_ZHUO_GUANGZONG.md`
9. `Docs/LEGAL_AND_ASSETS.md`

历史数字、现代定位和游戏映射出现分歧时，必须分别保存，不能用一个“最终值”覆盖争议。

## 三、当前事实

- 140年全国史籍锚点为9,698,630户、49,150,220口。
- 当前历史人口文档只有全国曲线和空间方法，尚无完整的140年郡国结构化原表。
- 目标资料规模约为105个郡国和1180个县级单位，最终数量以校录结果和缺项说明为准。
- 当前运行时原型只有6个`location.*`地点ID：
  - `location.zhuo`
  - `location.zhongshan`
  - `location.anping`
  - `location.xiaquyang`
  - `location.guangzong`
  - `location.ye`
- 184年原型地图另有`L001`等设计ID，城市并集另有`C001`至`C077`目录ID。
- 这些ID承担不同职责，不能通过批量重命名强行合并。

## 四、任务目标

完成后必须能够回答：

1. 140年每个已知郡国的原文户数、口数、修正值和证据来源是什么？
2. 缺项、讹误、异名和争议是如何记录的？
3. 每个郡国和县级单位对应哪个稳定地理单元？
4. 140年行政单位如何映射到184年原型地点、77城目录和未来城市圈？
5. 同一稳定地理单元在不同年份改名、改属或迁治时，如何保持身份不变？
6. 原始合计、修正合计与全国锚点之间存在多少差额，原因是否可审计？
7. M12按玩家缩尺创建实际人口时应读取哪些史料参考文件和映射权重？

## 五、明确不做

本任务不包括：

- 按史料人口一比一生成永久人物，或验证累计5,000万正式目标；
- 决定SQLite、分区二进制或混合存储方案；
- 修改`WorldState.CurrentSchemaVersion`或V5存档；
- 把约1180个县全部制作成可进入的Unity场景；
- 精确绘制所有东汉县界多边形；
- 完成135—260全部年度地区人口；
- 将推定人口伪装成史籍原始数字；
- 抄录受版权保护的现代数据库、地图或现代作者完整表述；
- 修改现有`location.*`、`L###`或`C###`公开ID。

## 六、ID与映射模型

四类ID必须分开：

```text
geo.region.*
  不随年代改变的稳定地理单元

admin.han140.*
  140年行政单位

location.*
  当前Unity运行时地点

L### / C###
  原型地图目录ID与77城设计目录ID
```

### 6.1 稳定地理ID

稳定地理ID表示相对固定的地理范围或聚落位置，不直接使用某一年的行政名称作为身份。

最低规则：

- 全小写ASCII；
- 使用`.`分层；
- 一旦进入存档或公开数据不得改名；
- 显示名、历史名和别名独立保存；
- 不确定边界可以标记为`provisional`，不能伪造精确多边形；
- 郡治迁移时新增时期关系，不替换稳定地理ID。

### 6.2 行政ID

`admin.han140.*`只表示140年的行政截面，记录：

- 单位类型；
- 上级单位；
- 当时名称；
- 治所；
- 生效年份；
- 户口原始值和修正值；
- 来源与争议；
- 对稳定地理单元的映射。

### 6.3 交叉映射

一个行政单位可以映射到多个稳定地理单元；一个游戏城市节点也可以汇总多个历史县域。

映射必须记录：

```text
source_id
target_id
relation_type
valid_from_year
valid_to_year
weight_basis_points
mapping_method
confidence
provisional
notes
```

同一来源单位用于人口分配的`weight_basis_points`之和必须严格等于10,000。

## 七、交付目录与文件

新增版本控制目录：

```text
Data/HistoricalPopulation/
├─ han_140_sources.json
├─ han_140_administrative_units.csv
├─ han_140_population_records.csv
├─ stable_population_regions.csv
├─ han_140_region_mapping.csv
├─ game_location_crosswalk.csv
└─ han_140_audit_report.json
```

同时新增：

```text
Docs/HAN_140_POPULATION_DATA_DICTIONARY.md
Tools/Validate-Han140PopulationData.ps1
```

若实施时发现仓库已有更稳定的通用内容目录，允许调整目录，但必须先更新本任务书和
`DATA_AND_CONTENT_FOUNDATION.md`，不得产生第二套并行事实来源。

## 八、数据结构

### 8.1 来源表

`han_140_sources.json`每条至少包含：

```text
source_id
source_type
title
author_or_editor
edition_or_host
publication_or_access_date
url_or_bibliographic_locator
license_or_public_domain_note
evidence_scope
notes
```

### 8.2 行政单位表

`han_140_administrative_units.csv`至少包含：

```text
admin_unit_id
parent_admin_unit_id
unit_type
name_140
canonical_name
seat_admin_unit_id
valid_from_year
valid_to_year
source_ids
confidence
notes
```

### 8.3 人口记录表

`han_140_population_records.csv`至少包含：

```text
admin_unit_id
registered_households_raw
registered_population_raw
registered_households_corrected
registered_population_corrected
correction_code
correction_note
evidence_grade
source_ids
source_locator
model_version
```

规则：

- 原文缺失使用空值和明确缺项码，不得填0；
- 原文值与修正值分别保存；
- 修正必须拥有理由和来源；
- 数字字段不得包含逗号、中文单位或说明文字；
- `evidence_grade`沿用H、R、M、I体系，本任务原始史籍记录通常使用H。

### 8.4 稳定地理单元表

`stable_population_regions.csv`至少包含：

```text
stable_region_id
parent_stable_region_id
region_type
canonical_name
modern_reference
centroid_latitude
centroid_longitude
geometry_status
confidence
provisional
notes
```

坐标只作为近似索引；存在古城址争议时必须保留多个候选或降低可信度。

### 8.5 游戏地点交叉表

`game_location_crosswalk.csv`至少覆盖：

- 当前6个`location.*`运行时地点；
- 184年原型地图`L###`目录；
- 77城`C###`目录；
- 与其相关的稳定地理单元和140年行政单位。

不得要求每个历史县都对应一个主城。

## 九、实施阶段

### P0：数据合同与来源登记（已完成）

实施记录见
[`TASK_M13_P0_HAN_140_DATA_CONTRACT_AND_VALIDATOR.md`](TASK_M13_P0_HAN_140_DATA_CONTRACT_AND_VALIDATOR.md)。
P0完成只表示数据合同和验证机制可用；郡国、县级单位和稳定地理记录数仍为0。

- 建立目录、字段字典和空模板；
- 登记史籍、校录说明和必要的现代研究来源；
- 明确许可、公共领域和引用边界；
- 完成验证器的字段、编码、ID和重复检查。

验收门：

- 空模板可通过结构检查；
- 故意制造的重复ID、非法年份、负人口和缺失来源会被拒绝。

### P1：郡国级人口校录（已完成）

- 首批实施记录见
  [`TASK_M13_P1_FIRST_COMMANDERY_BATCH.md`](TASK_M13_P1_FIRST_COMMANDERY_BATCH.md)：
  已完成涿郡、广阳郡、魏郡、钜鹿郡、中山国、安平国6条原始户口校录；
  该记录只代表P1首批完成。
- 冀州完成批次见
  [`TASK_M13_P1_JIZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_JIZHOU_COMPLETION_BATCH.md)：
  新增常山国、河间国、清河国、赵国、勃海郡，至此冀州九郡国完成；
  P1全国全量和M13整体仍未完成。
- 幽州完成批次见
  [`TASK_M13_P1_YOUZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_YOUZHOU_COMPLETION_BATCH.md)：
  新增代郡、上谷郡、渔阳郡、右北平郡、辽西郡、辽东郡、玄菟郡、
  乐浪郡、辽东属国，至此幽州十一郡、属国完成；辽东属国原文户口缺项及
  三项现代校录补正均显式保存，P1全国全量和M13整体仍未完成。
- 司隶完成批次见
  [`TASK_M13_P1_SILI_COMPLETION_BATCH.md`](TASK_M13_P1_SILI_COMPLETION_BATCH.md)：
  新增河南尹、河内郡、河东郡、弘农郡、京兆尹、左冯翊、右扶风，
  至此司隶七郡、尹完成；河南尹与弘农郡的现代ODS异录已明确记录但未覆盖
  卷二十九原典值，P1全国全量和M13整体仍未完成。
- 豫州完成批次见
  [`TASK_M13_P1_YUZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_YUZHOU_COMPLETION_BATCH.md)：
  新增颍川郡、汝南郡、梁国、沛国、陈国、鲁国，至此豫州六郡国完成；
  沛国、陈国原文人口和百万位移置补正分别保存，P1全国全量和M13整体仍未完成。
- 兖州完成批次见
  [`TASK_M13_P1_YANZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_YANZHOU_COMPLETION_BATCH.md)：
  新增陈留郡、东郡、东平国、任城国、泰山郡、济北国、山阳郡、济阴郡，
  至此兖州八郡国完成；泰山郡原文户数和十万位脱漏补正分别保存，
  P1全国全量和M13整体仍未完成。
- 徐州完成批次见
  [`TASK_M13_P1_XUZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_XUZHOU_COMPLETION_BATCH.md)：
  新增东海郡、琅邪国、彭城国、广陵郡、下邳国，至此徐州五郡国完成；
  琅邪国原文户数和十万位脱漏补正分别保存，P1全国全量和M13整体仍未完成。
- 青州完成批次见
  [`TASK_M13_P1_QINGZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_QINGZHOU_COMPLETION_BATCH.md)：
  新增济南国、平原郡、乐安国、北海国、东莱郡、齐国，至此青州六郡国完成；
  济南郡国转录异文独立记录，140年按国保存，P1全国全量和M13整体仍未完成。
- 荆州完成批次见
  [`TASK_M13_P1_JINGZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_JINGZHOU_COMPLETION_BATCH.md)：
  新增南阳郡、南郡、江夏郡、零陵郡、桂阳郡、武陵郡、长沙郡，至此荆州七郡完成；
  两份公开转录户口值一致，本批不设数值补正，P1全国全量和M13整体仍未完成。
- 扬州完成批次见
  [`TASK_M13_P1_YANGZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_YANGZHOU_COMPLETION_BATCH.md)：
  新增九江郡、丹阳郡、庐江郡、会稽郡、吴郡、豫章郡，至此扬州六郡完成；
  两份公开转录户口值一致，本批不设数值补正，P1全国全量和M13整体仍未完成。
- 益州完成批次见
  [`TASK_M13_P1_YIZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_YIZHOU_COMPLETION_BATCH.md)：
  新增汉中郡、巴郡、广汉郡、蜀郡、犍为郡、牂牁郡、越巂郡、益州郡、
  永昌郡及三个属国，至此益州十二郡与属国完成；永昌原文疑值不设虚构补正，
  广汉属国旧注讹文独立记录，P1全国全量和M13整体仍未完成。
- 凉州完成批次见
  [`TASK_M13_P1_LIANGZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_LIANGZHOU_COMPLETION_BATCH.md)：
  新增陇西郡、汉阳郡、武都郡、金城郡、安定郡、北地郡、武威郡、张掖郡、
  酒泉郡、敦煌郡及两个属国，至此凉州十二郡与属国完成；酒泉缺失口数和
  敦煌疑脱千位分别保存原值与现代校录补正，P1全国全量和M13整体仍未完成。
- 并州完成批次见
  [`TASK_M13_P1_BINGZHOU_COMPLETION_BATCH.md`](TASK_M13_P1_BINGZHOU_COMPLETION_BATCH.md)：
  新增上党郡、太原郡、上郡、西河郡、五原郡、云中郡、定襄郡、雁门郡、
  朔方郡，至此并州九郡完成；两份公开古籍转录户口值一致，本批不设数值
  补正，P1全国全量和M13整体仍未完成。
- 交州及P1全国收口批次见
  [`TASK_M13_P1_JIAOZHOU_AND_NATIONAL_COMPLETION_BATCH.md`](TASK_M13_P1_JIAOZHOU_AND_NATIONAL_COMPLETION_BATCH.md)：
  新增南海郡、苍梧郡、郁林郡、合浦郡、交趾郡、九真郡、日南郡，至此
  交州七郡及P1全国105条郡国级人口记录完成；郁林、交趾原典户口留空，
  现代区域估算只写修正字段并标M级，稳定地理、县级目录、游戏映射和
  M13整体仍未完成。
- 已分十四批录入105个郡国、尹或属国；
- 各批完成后均运行校验并保存确定性审计结果；
- 已分别保存原文值、修正值、缺项、讹误和修正理由；
- 已计算原始合计、有效合计、全国锚点及差额。

验收门：

- 不通过强行缩放让修正合计等于全国锚点；
- 所有差额均在审计报告中可见；
- 每条人口记录都能追溯到来源定位。

### P2：稳定地理单元与郡国映射（已完成）

- 首批实施记录见
  [`TASK_M13_P2_PROTOTYPE_CORRIDOR_STABLE_GEOGRAPHY_BATCH.md`](TASK_M13_P2_PROTOTYPE_CORRIDOR_STABLE_GEOGRAPHY_BATCH.md)：
  已为当前六个运行时地点涉及的涿郡、中山国、安平国、钜鹿郡、魏郡建立
  1个河北宏区、5个郡国尺度稳定子区和5条10,000基点守恒映射；所有边界均
  标为临时，运行时地点交叉仍留至P3。
- 河北连续地理带第二批实施记录见
  [`TASK_M13_P2_HEBEI_CONTIGUOUS_GEOGRAPHY_BATCH.md`](TASK_M13_P2_HEBEI_CONTIGUOUS_GEOGRAPHY_BATCH.md)：
  沿用河北宏区，为广阳郡、河间国、常山国、清河国、赵国新增5个郡国尺度
  稳定子区和5条10,000基点守恒映射；P2当前累计11个稳定节点、10条映射，
  全部仍为临时记录，游戏地点交叉仍留至P3。
- 勃海—燕山—辽西走廊第三批实施记录见
  [`TASK_M13_P2_BOHAI_YANSHAN_LIAOXI_CORRIDOR_BATCH.md`](TASK_M13_P2_BOHAI_YANSHAN_LIAOXI_CORRIDOR_BATCH.md)：
  为勃海郡新增河北宏区东部子区，并新建燕山与辽西走廊宏区，承接渔阳郡、
  右北平郡和辽西郡；P2当前累计16个稳定节点、14条映射，冀州九郡国已全部
  拥有临时映射，幽州与全国覆盖仍未完成。
- 幽州北部与东北收口第四批实施记录见
  [`TASK_M13_P2_YOUZHOU_NORTH_AND_NORTHEAST_COMPLETION_BATCH.md`](TASK_M13_P2_YOUZHOU_NORTH_AND_NORTHEAST_COMPLETION_BATCH.md)：
  新建燕北山地与长城走廊、辽东与朝鲜半岛北部两个宏区，承接代郡、上谷郡、
  辽东郡、玄菟郡、乐浪郡和辽东属国；P2当前累计24个稳定节点、20条映射，
  幽州十一郡、属国已全部拥有临时映射，全国覆盖仍未完成。
- 司隶河洛—河东—关中骨架第五批实施记录见
  [`TASK_M13_P2_SILI_HELUO_HEDONG_GUANZHONG_SKELETON_BATCH.md`](TASK_M13_P2_SILI_HELUO_HEDONG_GUANZHONG_SKELETON_BATCH.md)：
  新建河洛与黄河北岸、汾河南段与黄河东岸、关中渭河平原三个宏区，承接
  河南尹、河内郡、河东郡、弘农郡、京兆尹、左冯翊和右扶风；P2当前累计
  34个稳定节点、27条映射，司隶七郡、尹已全部拥有临时映射。
- 豫州中原东南—淮颍—泗沂骨架第六批实施记录见
  [`TASK_M13_P2_YUZHOU_CENTRAL_PLAINS_HUAIYING_SKELETON_BATCH.md`](TASK_M13_P2_YUZHOU_CENTRAL_PLAINS_HUAIYING_SKELETON_BATCH.md)：
  新建颍汝与淮河北岸、睢水与淮北北部平原、泗沂西缘与山前三个宏区，
  承接颍川郡、汝南郡、梁国、沛国、陈国和鲁国；P2当前累计43个稳定节点、
  33条映射，豫州六郡国已全部拥有临时映射。
- 兖州黄河下游—济汶泗水—泰沂山前骨架第七批实施记录见
  [`TASK_M13_P2_YANZHOU_YELLOW_JI_WENSI_TAIYI_SKELETON_BATCH.md`](TASK_M13_P2_YANZHOU_YELLOW_JI_WENSI_TAIYI_SKELETON_BATCH.md)：
  新建黄河下游与济水西部平原、汶泗水系与鲁西南平原、泰沂山地与西部山前
  三个宏区，承接陈留郡、东郡、东平国、任城国、泰山郡、济北国、山阳郡和
  济阴郡；P2当前累计54个稳定节点、41条映射，兖州八郡国已全部拥有临时映射。
- 徐州沂沭水系—泗水—江淮东部骨架第八批实施记录见
  [`TASK_M13_P2_XUZHOU_YISHU_SISHUI_JIANGHUAI_SKELETON_BATCH.md`](TASK_M13_P2_XUZHOU_YISHU_SISHUI_JIANGHUAI_SKELETON_BATCH.md)：
  新建沂沭水系与淮海北部、泗水中下游平原、江淮东部与下游水网三个宏区，
  承接东海郡、琅邪国、彭城国、广陵郡和下邳国；P2当前累计62个稳定节点、
  46条映射，徐州五郡国已全部拥有临时映射。琅邪国原始户数、十万位脱漏
  修正与未修正人口继续分字段保存。
- 青州黄河济水—淄潍胶莱—胶东半岛骨架第九批实施记录见
  [`TASK_M13_P2_QINGZHOU_LOWER_YELLOW_JI_ZIWEI_JIAODONG_SKELETON_BATCH.md`](TASK_M13_P2_QINGZHOU_LOWER_YELLOW_JI_ZIWEI_JIAODONG_SKELETON_BATCH.md)：
  新建黄河下游与济水东部平原、淄潍水系与胶莱西部平原、胶东半岛与
  北部海岸三个宏区，承接济南国、平原郡、乐安国、北海国、东莱郡和齐国；
  P2当前累计71个稳定节点、52条映射，青州六郡国已全部拥有临时映射。
  济南条140年仍按国保存，济南郡转录异文不转化为人口修正。
- 荆州汉水—江汉平原—洞庭湖—南岭北麓骨架第十批实施记录见
  [`TASK_M13_P2_JINGZHOU_HANJIANG_JIANGHAN_DONGTING_NANLING_SKELETON_BATCH.md`](TASK_M13_P2_JINGZHOU_HANJIANG_JIANGHAN_DONGTING_NANLING_SKELETON_BATCH.md)：
  新建汉水中上游与南阳盆地、汉水下游与江汉平原、洞庭湖与湘资沅澧水系、
  南岭北麓与湘水上游四个宏区，承接南阳郡、南郡、江夏郡、武陵郡、长沙郡、
  零陵郡和桂阳郡；P2当前累计82个稳定节点、59条映射，荆州七郡已全部拥有
  临时映射。本批七郡人口不生成修正，游戏地点交叉继续留至P3。
- 扬州淮南—长江下游—太湖—钱塘浙东—赣鄱骨架第十一批实施记录见
  [`TASK_M13_P2_YANGZHOU_HUAINAN_LOWER_YANGTZE_TAIHU_GANPOYANG_SKELETON_BATCH.md`](TASK_M13_P2_YANGZHOU_HUAINAN_LOWER_YANGTZE_TAIHU_GANPOYANG_SKELETON_BATCH.md)：
  新建淮南与长江北岸、长江下游与太湖水网、钱塘江与浙东丘陵、赣江与
  鄱阳湖盆地四个宏区，承接九江郡、庐江郡、丹阳郡、吴郡、会稽郡和豫章郡；
  P2当前累计92个稳定节点、65条映射，扬州六郡已全部拥有临时映射。本批
  六郡人口不生成修正，游戏地点交叉继续留至P3。
- 益州汉中—四川盆地—川西山地—云贵高原—横断山南缘骨架第十二批实施记录见
  [`TASK_M13_P2_YIZHOU_HANZHONG_SICHUAN_YUNGUI_HENGDUAN_SKELETON_BATCH.md`](TASK_M13_P2_YIZHOU_HANZHONG_SICHUAN_YUNGUI_HENGDUAN_SKELETON_BATCH.md)：
  新建汉中盆地与秦巴走廊、四川盆地与盆周河谷、川西山地与高原过渡走廊、
  云贵高原与山间盆地、横断山南缘与滇西纵谷五个宏区，承接益州九郡和三属国；
  P2当前累计109个稳定节点、77条映射，益州十二项人口来源已全部拥有临时映射。
  永昌疑值与广汉属国旧注异文不转化为人口修正，游戏地点交叉继续留至P3。
- 凉州陇右—河湟—河西走廊—居延边地骨架第十三批实施记录见
  [`TASK_M13_P2_LIANGZHOU_LONGYOU_HEHUANG_HEXI_JUYAN_SKELETON_BATCH.md`](TASK_M13_P2_LIANGZHOU_LONGYOU_HEHUANG_HEXI_JUYAN_SKELETON_BATCH.md)：
  新建陇右黄土高原与渭水上游、陇南山地与秦巴北缘、河湟谷地与黄河上游、
  陇东宁南黄土高原与鄂尔多斯南缘、河西走廊与祁连山北麓绿洲、黑河下游与
  居延绿洲六个宏区，承接凉州十郡和两属国；P2当前累计127个稳定节点、
  89条映射，凉州十二项人口来源已全部拥有临时映射。酒泉缺口、敦煌疑户及
  北地武威转录异文继续分别审计，游戏地点交叉继续留至P3。
- 并州太行—汾河—河套—阴山骨架第十四批实施记录见
  [`TASK_M13_P2_BINGZHOU_TAIHANG_FENHE_HETAO_YINSHAN_SKELETON_BATCH.md`](TASK_M13_P2_BINGZHOU_TAIHANG_FENHE_HETAO_YINSHAN_SKELETON_BATCH.md)：
  新建太行山西麓与上党盆地、汾河谷地与吕梁山东麓、陕北黄土高原与无定河、
  黄河河套平原、阴山南麓与土默川、雁门山地与桑干河盆地六个宏区，承接并州
  九郡；P2当前累计142个稳定节点、98条映射，并州九项人口来源已全部拥有
  临时映射。九郡不生成修正，雁门郡原始31,862户、249,000口继续完整保留，
  游戏地点交叉继续留至P3。
- 交州岭南—珠江—北部湾—红河全国收口第十五批实施记录见
  [`TASK_M13_P2_JIAOZHOU_LINGNAN_PEARL_BEIBU_RED_RIVER_COMPLETION_BATCH.md`](TASK_M13_P2_JIAOZHOU_LINGNAN_PEARL_BEIBU_RED_RIVER_COMPLETION_BATCH.md)：
  新建珠江三角洲与岭南东部河网、西江—郁江水系与桂中东盆谷、北部湾沿海与
  桂南滨海平原、红河三角洲与越北低地、越南北中部河谷与沿海走廊五个宏区，
  承接交州七郡；P2最终累计154个稳定节点、105条映射，全国105项郡国、尹、
  属国人口来源全部拥有临时守恒映射。郁林、交趾原典户口仍为空，M级有效估算
  不覆盖原值。P2完成不表示县级目录、游戏地点交叉、P4消费接口或M13整体完成。
- 建立州、郡国、县域或城市圈所需的稳定地理层级；
- 将140年行政单位映射到稳定地理单元；
- 权重总和严格守恒；
- 记录边界不确定、治所争议和一对多关系。

验收门：

- 每个有数据的郡国至少拥有一个稳定地理映射；
- 不存在循环父级、孤立引用或重复稳定ID；
- 映射权重总和校验通过。

### P3：县级目录与现有游戏节点交叉

- 第一批实施记录见
  [`TASK_M13_P3_RUNTIME_PROTOTYPE_CITY_CROSSWALK_FIRST_BATCH.md`](TASK_M13_P3_RUNTIME_PROTOTYPE_CITY_CROSSWALK_FIRST_BATCH.md)：
  新增涿、下曲阳、广宗、邺四个140年县级行政候选与四个临时县级稳定地理身份；
  建立当前6个`location.*`、对应6个`L###`和相关3个`C###`共15条交叉映射。
  中山、安平继续作为区域代理，钜鹿郡人口不会因下曲阳、广宗或`C010`重复计算。
  本批完成只表示当前运行时走廊闭环，不表示其余县级目录、77城或P3完成。
- 第二批实施记录见
  [`TASK_M13_P3_REMAINING_PROTOTYPE_CATALOG_CROSSWALK_SECOND_BATCH.md`](TASK_M13_P3_REMAINING_PROTOTYPE_CATALOG_CROSSWALK_SECOND_BATCH.md)：
  新增蓟、廮陶两个140年县级行政候选及两个临时县级稳定地理身份，并补齐
  `L002/L003/L005/L009/L010/L012`六个原型目录节点。至此`L001`至`L012`
  均有明确交叉状态；广阳战区、钜鹿郡治保持区域代理，博陵中继与黄河—洛阳
  出口保持待考，不虚构行政归属或重复郡国人口。77城其余节点及全国县级目录
  仍未完成。
- 第三批实施记录见
  [`TASK_M13_P3_NORTHERN_CITY_CATALOG_FIRST_BATCH.md`](TASK_M13_P3_NORTHERN_CITY_CATALOG_FIRST_BATCH.md)：
  开始77城目录扩展，为`C001-C008`、`C011`、`C013`十个北方节点建立交叉状态；
  新增襄平、朝鲜、土垠、晋阳、壶关、南皮、平原、甘陵八个140年县级行政候选
  和八个临时县级稳定地理身份。`C004`复用蓟县身份，`C013`城阳保持待考；
  至此`C001-C013`全部拥有显式状态，但`C014-C077`与全国县级目录仍未完成。
- 正式收尾实施记录见
  [`TASK_M13_COMPLETION_COUNTY_CATALOG_CITY_CROSSWALK_AND_M12_INTERFACE.md`](TASK_M13_COMPLETION_COUNTY_CATALOG_CITY_CROSSWALK_AND_M12_INTERFACE.md)：
  逐项校录1182条县级列项，为其建立行政、稳定地理和一对一身份映射，补齐
  105项郡国治所引用以及`C014-C077`。常见约数1180、州部小计1181、逐项数
  1182及巴郡标题十四城/正文十五项的差异全部进入审计，不删除冲突事实。
- 校录约1180个县级单位的名称、隶属和治所信息；
- 先完成当前6地点相关区域，再扩展77城及全国目录；
- 建立`location.*`、`L###`、`C###`和稳定地理ID交叉表；
- 不确定县界仅记录归属与近似位置，不伪造精确边界。

验收门：

- 当前6个运行时地点全部映射；
- 77城目录全部拥有映射状态：精确、聚合、近似、待考之一；
- 未映射县和争议县在审计报告中明确列出。

### P4：审计报告与消费接口

- 生成确定性的`han_140_audit_report.json`；
- 固定字段顺序和排序规则；
- 输出总户数、总人口、修正差额、缺项、争议、映射覆盖率和权重错误；
- 为M12说明如何读取全国总量、地区权重和不确定性。
- 已生成`han140.audit.v2`审计报告及`han140.m12-input.v1`消费文件；后者只包含
  105条人口来源、有效人口、证据、治所和稳定地理权重，1182县不重复成为人口源。

验收门：

- 相同输入重复验证产生字节一致或语义一致的审计结果；
- 数据文件顺序变化不改变统计结果；
- M12无需依赖显示名称匹配地点。

## 十、验证器要求

`Tools/Validate-Han140PopulationData.ps1`必须：

- 支持`-DataRoot`、`-OutputPath`和`-M12OutputPath`参数；
- 默认只读取任务目录，不扫描整个硬盘；
- UTF-8读写；
- 非零退出码表示失败；
- 检查必需字段、ID格式、重复ID、非法引用和父级循环；
- 检查年份、非负整数、空值与0的区别；
- 检查来源引用；
- 检查映射权重守恒；
- 输出机器可读审计报告和简短终端摘要；
- 在300秒内完成当前数据集校验；
- 不访问网络，不启动Unity，不修改输入文件。

## 十一、测试与验收

至少验证：

1. 全国史籍锚点保存为9,698,630户、49,150,220口。
2. 原始分项、修正分项和全国锚点分别汇总，不互相覆盖。
3. 缺项不会被当作0人口。
4. 每项修正都有修正码、说明和来源。
5. 所有行政、地理、游戏地点和来源引用有效。
6. 稳定地理ID不包含年份行政名称的隐式替换规则。
7. 当前6个`location.*`运行时ID不被重命名。
8. `L###`和`C###`只作为交叉目录ID，不成为人口事实来源。
9. 每个用于人口分配的映射权重总和为10,000。
10. 相同输入产生相同审计结果。
11. 数据验证失败时不会生成“通过”报告。
12. 资料来源和许可证说明满足开源发布要求。

## 十二、完成条件

只有同时满足以下条件，M13才能标记完成：

- 约105个郡国完成原值、修正值、来源和争议校录；
- 县级目录达到任务确认的完整数量，并解释与“约1180”之间的差异；
- 当前6地点和77城目录完成交叉映射状态；
- 原始合计、修正合计、全国锚点和差额可重复审计；
- 所有映射权重、引用、ID和父级关系验证通过；
- 数据字典与实际文件完全一致；
- 验证器具有成功和失败样例证据；
- `git diff --check`通过；
- 文档、数据和验证器范围经过人工复核。

完成后在系统总纲中将“140年郡国人口”从“待研究”更新为实际证据支持的状态，
并将下一项切换为M12永久人物存储压力原型。

### 完成记录（2026-07-31）

M13已达到上述完成条件：105条人口记录、1182条县级目录、105条人口覆盖映射、
1182条县级身份映射、6个运行时地点、`L001-L012`和`C001-C077`均有可验证记录。
审计报告明确保存1180/1181/1182三种数量口径和巴郡差异，M12消费文件对每个人口
来源保持10,000基点守恒。验证器回归包含成功、失败、确定性和检入产物时效性检查。
历史批次段落中的“仍未完成”只描述当时范围，不再代表M13当前状态。

## 十三、防卡死与执行批次

- 不允许用一个无边界命令一次抓取、清洗和写入全部数据。
- 每次校录控制在10至20个郡国或一个明确地区，完成即校验和保存。
- 网络资料检索、转换和验证命令均遵守300秒硬超时。
- 任一来源不可访问时记录阻塞和替代来源，不持续等待。
- 验证器只读取明确的数据目录，禁止递归扫描用户磁盘。
- 每批报告已完成单位、缺项、争议和下一批范围。

## 十四、后续任务

M13完成后的直接下一项是：

> M12永久人物存储压力原型：比较SQLite、分区二进制和混合方案，并验证缩尺开局、
> 长期出生死亡与累计永久人物压力。157年前后约5,650万只作为史料参考。

M13提供人口规模、稳定地理单元和映射权重；M12负责永久人物、事件调度、存储性能
和关注展开。两项任务不得互相复制事实来源。

兼容说明：本任务早期所称“历史人口档”已被后续正式口径取代。实际开局始终按玩家与
硬件选择缩尺；正式性能指标为单局`CumulativePersonCount`，不是任一史料年份的同时
在世人口。
