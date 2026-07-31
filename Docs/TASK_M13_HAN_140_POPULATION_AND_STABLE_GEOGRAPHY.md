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
7. M12创建历史人口档时应读取哪些结构化文件和映射权重？

## 五、明确不做

本任务不包括：

- 生成5,000万或5,650万永久人物；
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

### P1：郡国级人口校录

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
- 分批录入约105个郡国；
- 每批建议10至20个单位，完成后立即运行校验并保存审计结果；
- 保存原文值、修正值、缺项、讹误和修正理由；
- 计算原始合计、修正合计、全国锚点及差额。

验收门：

- 不通过强行缩放让修正合计等于全国锚点；
- 所有差额均在审计报告中可见；
- 每条人口记录都能追溯到来源定位。

### P2：稳定地理单元与郡国映射

- 建立州、郡国、县域或城市圈所需的稳定地理层级；
- 将140年行政单位映射到稳定地理单元；
- 权重总和严格守恒；
- 记录边界不确定、治所争议和一对多关系。

验收门：

- 每个有数据的郡国至少拥有一个稳定地理映射；
- 不存在循环父级、孤立引用或重复稳定ID；
- 映射权重总和校验通过。

### P3：县级目录与现有游戏节点交叉

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

验收门：

- 相同输入重复验证产生字节一致或语义一致的审计结果；
- 数据文件顺序变化不改变统计结果；
- M12无需依赖显示名称匹配地点。

## 十、验证器要求

`Tools/Validate-Han140PopulationData.ps1`必须：

- 支持`-DataRoot`和`-OutputPath`参数；
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

## 十三、防卡死与执行批次

- 不允许用一个无边界命令一次抓取、清洗和写入全部数据。
- 每次校录控制在10至20个郡国或一个明确地区，完成即校验和保存。
- 网络资料检索、转换和验证命令均遵守300秒硬超时。
- 任一来源不可访问时记录阻塞和替代来源，不持续等待。
- 验证器只读取明确的数据目录，禁止递归扫描用户磁盘。
- 每批报告已完成单位、缺项、争议和下一批范围。

## 十四、后续任务

M13完成后的直接下一项是：

> M12永久人物存储压力原型：比较SQLite、分区二进制和混合方案，并从10万人阶梯
> 推进到100万、1,000万、5,000万和5,650万人。

M13提供人口规模、稳定地理单元和映射权重；M12负责永久人物、事件调度、存储性能
和关注展开。两项任务不得互相复制事实来源。
