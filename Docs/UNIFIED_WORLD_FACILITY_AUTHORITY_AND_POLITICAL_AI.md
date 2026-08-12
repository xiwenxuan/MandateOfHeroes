# 统一世界、设施、权力、皇室与政治 AI 设计

## Document Governance

- Purpose：定义Cell占用、Facility能力、产权、组织职位、官军爵、皇室、政权与政治AI。
- Authority：L1 CANONICAL SYSTEM SPEC。
- Covers：统一Facility/Authority/Political边界。
- DoesNotCover：FamilyCenter历史候选、当前实现完成度或具体城市史料。
- Supersedes：单Headquarters、固定建筑Buff和职位即产权等早期简化。
- SupersededBy：无。
- RelatedCanonicalDocs：`FAMILY_ORGANIZATION_REFERENCE_V1/README.md`、`GAME_SYSTEMS_MASTER_AND_STATUS.md`。
- Status：CANONICAL。

> 文档状态：跨系统正式设计（已定方案；各项实现状态见第 15 节）
> 最近归并：2026-08-09
> 适用范围：Cell、Facility、人物劳动、家族/皇室资产、官职、军职、爵位、政权、政治 AI 与统一战争接口

## 1. 文档定位与权威边界

本文把统一东汉世界、设施目录与成长、人口劳动、行政与军事权力、皇室与王国、
自立和政治 AI 归并成一套可实施合同。它负责这些对象之间的交界，不替代各领域已有主文档：

- 当前状态和全局开发顺序：`GAME_SYSTEMS_MASTER_AND_STATUS.md`；
- 世界、地图、人口经济和守恒：`WORLD_SIMULATION_FOUNDATION.md`；
- 生产、配方、品质与职业成长：`PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md`；
- 人物属性、教育、词条和家族培养：`CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md`；
- 战斗结算、装备、伤亡和战争回写：`UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md`；
- 永久人物、冷热档案与关注：`TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md`；
- 有限认知、地图视角和递归委任：`TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md`。

当本文与上述文档发生交界时，采用以下分工：

1. 本文决定 Cell—Facility—Organization—Office—Force—Polity 如何连接；
2. 领域文档决定本领域内部算法、内容与实现依赖；
3. 总纲决定当前究竟实现到什么程度和下一步先做什么；
4. 现有存档、代码和测试事实不得被纯设计文字静默推翻。

## 2. 统一世界宪法

### 2.1 一本世界账

人物、土地、资源、设施、库存、财政、职位和军队都只有一份权威事实。地图层级、
建筑界面、经营报表和战斗表现只是不同读取方式，不得产生平行世界状态。

### 2.2 权利必须拆开

以下关系不得互相自动推导：

```text
土地所有权
设施控制与运营权
行政管辖权
组织职位权限
军事指挥权
爵位与封邑权益
个人、家族和国家资产
```

县令管理一县，不等于拥有全县土地；家主管理族产，不等于族产属于家主个人；
将军指挥官军，不等于官军成为私人资产；诸侯王获得封国，不等于拥有封国内全部 Cell。

### 2.3 真实复杂、分层操作

底层保留真实人物、设施、物资、时间、权限和后果；玩家可以选择亲自操作、实时派工、
工单、目标指令、职位委任或只看异常。自动化只生成并执行合法命令，不以“托管加成”
直接修改结果。

## 3. Cell、行政区域与空间占用

### 3.1 Cell 尺度

正式世界沿用 `HanWorldV1` 的稳定网格与 **2000 米 Cell**。这是战略交互和数据索引尺度，
不是单体建筑的实测占地。外部讨论中“Cell 不指定现实米数”的旧口径只作为早期设想保留，
不得覆盖已经发布的网格、CellId 或地图数据。

### 3.2 Cell 世界事实

每个 Cell 至少可关联：

```text
CellId / GridSchemaVersion
地形、高程、坡度和水文
肥力、林木、草场与自然资源体
县、郡国、州和政权行政归属
OwnerId
基础 Facility 占用
独立 Force 占用
道路、桥梁、河流和其他地理连接
历史来源与空间精度
```

行政归属、法理归属、实际控制和产权分别保存。县域即使县府缺失或瘫痪仍然存在；
县府则是政府组织、职位和实际官署 Facility 共同形成的运行能力。

### 3.3 一 Cell 一产权主体

一个 Cell 同一时刻只有一个 `OwnerId`。V1 不把百分比共有作为常态产权模型。
买卖、赠与、继承、征收、没收、划拨和战争夺取都必须形成可审计产权事务。

直接建设以 Cell Owner 为建设主体。承包人、雇工和受任官员可以代表 Owner 执行项目，
但不会因此取得产权；若另一主体要以自己的名义取得设施资产，须先完成合法产权转移。

### 3.4 空间占用不变量

```text
一个 Cell 最多存在一个基础 Facility
一个 Cell 最多存在一支独立 Force
Facility 与 Force 使用不同占用槽，可以在同一 Cell 共存
```

一支 Force 占一格是大地图表示与通行规则，不表示所有士卒挤在一个物理点。
一个 Facility 占一格同样表示其主要功能和土地占用；显示层可以画出院落、街巷和建筑群。

大型城市、宫城、庄园、矿区、产业区和防线使用 `Complex / FacilityGroup / Area` 聚合多个
真实 Cell 与 Facility，不建立第二套 SubCell 世界事实。

## 4. 统一 Facility 模型

### 4.1 定义、配置与实例

开放内容使用稳定命名空间 ID，不使用固定 `FacilityKind` 枚举：

```text
FacilityDefinition（BaseType）
→ FacilityProfile / Variant
→ FacilityState（世界实例）
```

- `Definition` 决定核心机制和基本能力；
- `Profile / Variant` 表达宫廷、官府、家族、民间、军用、规模与历史配置差异；
- `State` 保存某个具体设施现在的 Cell、权属、人员、库存、耐久和运行情况。

历史名称、所有者或使用场景不同，不足以创建新的 BaseType。例如御厨、家族厨房与军营伙房
都优先复用厨房定义；宫门、城门与庄园门优先复用门定义。

### 4.2 建议数据边界

```text
FacilityDefinition
  id
  primary_category_id
  capability_ids[]
  capacity_definitions[]
  eligibility_rules[]
  allowed_operation_ids[]
  required_terrain_or_resource_rules[]
  construction_rules
  maintenance_rules
  development_rules

FacilityState
  facility_id
  definition_id
  profile_id
  cell_id
  owner_id
  controller_id
  operator_organization_id
  beneficiary_policy_id
  access_policy_id
  durability / condition
  resident_person_ids[]
  worker_assignment_ids[]
  service_assignment_ids[]
  inventory_container_ids[]
  equipment_and_modules[]
  unlocked_operation_ids[]
  active_orders[]
  construction_or_refit_state
  historical_source_and_precision
```

`Owner`、`Controller`、`Operator` 和受益者必须分开。例如国家拥有一座仓库，地方官署控制，
具体仓曹组织运营，军队依据调拨单受益。任何字段都不能由 UI 临时推断成永久事实。

### 4.3 属性、状态、动作和知识分离

- 属性/容量：最大能做什么；
- 状态：当前人员、库存、耐久和运行情况；
- 动作/工单：谁在什么时间执行什么；
- 配方/课程/治疗/训练：所需知识、输入、设备、人员、时间与输出。

掌握配方不等于能够执行。实际操作至少要求：

```text
Facility Capability
+ 已知 Recipe / Process / Course / Treatment / TrainingProgram
+ 合格人物
+ 所需设备
+ 真实材料
+ 可用时间与权限
```

### 4.4 Capability 与 Capacity

能力标签回答“能做什么”，容量字段回答“同时或累计能处理多少”。第一批通用能力族包括：

```text
Residential, Agriculture, Extraction, Production, Processing,
Storage, Logistics, Trade, Service, Hospitality, Medical,
Administration, Education, Library, AcademicResearch, Training,
Ritual, Assembly, Observation, Calendar, Military, Garrison,
Fortification, Blocking, Passage, Transport, WaterSupply,
WaterControl, Drainage, Recreation, Reception
```

第一批容量族包括：

```text
ResidentialCapacityPersons
WorkerCapacity
StudentCapacity
ServiceCapacity
StorageCapacity
AssemblyCapacity
GarrisonCapacity
TrainingCapacity
AnimalCapacity
VehicleCapacity
ParallelProductionCapacity
LibraryCapacity
```

没有对应能力时容量为零。住房始终以具体 Person 计数；岗位始终由永久 Person ID 填充。

## 5. Facility Catalog V1 的处理方式

外部 `FACILITY_CATALOG_V1` 作为**第一批内容候选目录**纳入，而不是固定代码枚举或永远不变的
唯一字典。目录按领域分组如下：

| 领域 | 候选 BaseType |
|---|---|
| 住宅 | Residence |
| 农林牧 | Farmland、Plantation、HerbField、Pasture、Forestry |
| 采掘 | Mine、Quarry |
| 生产加工 | Mill、Brewery、Smelter、Smithy、Carpentry、SilkwormHouse、SilkReelingWorkshop、WeavingWorkshop、DyeWorkshop、MedicineWorkshop、Shipyard、Kitchen |
| 仓储物流 | Warehouse、Granary、Stable、CarriageYard、CourierStation、Harbor |
| 商业服务 | Market、Shop、Inn、Clinic、GuildHall、MerchantHall |
| 行政 | GovernmentOffice、CourtHall |
| 教育知识礼制 | School、Academy、Library、RitualHall、Observatory、TrainingHall |
| 军事 | Barracks、TrainingGround、FieldHospital |
| 城防 | Wall、Gate、Moat、Fort、BeaconTower |
| 基础设施 | Road、Bridge、Canal、Well、WaterIntake、Drainage、Dike |
| 公共空间 | Garden、Plaza、Courtyard |

正式进入内容包前必须做机制去重审计，尤其检查：

- `Warehouse / Granary` 是否应为同一 BaseType 的仓储 Profile；
- `Clinic / FieldHospital` 是否只是医疗设施的民用/军用 Profile；
- `School / Academy` 是能力层级差异还是核心机制差异；
- `Fort` 是独立综合设施还是 Wall、Gate、Barracks 等组成的蓝图；
- `Plaza / Courtyard` 是否具有足够独立的人员、权限和世界操作。

目录可以增加，但新增 BaseType 必须证明现有 Definition + Profile + Capability + Operation 无法表达。

## 6. Facility 五种成长方式

Facility 不使用单一 `Lv.1 → Lv.2 → Lv.3` 作为世界事实。UI 可以显示综合等级摘要，实际成长分为：

1. **参数升级**：提高合理上限内的岗位、容量、耐久、品质、效率和并行能力；
2. **设备/内部模块**：添加炉具、风箱、书房、防火、排水等不值得独立占 Cell 的功能；
3. **内容扩展**：获得新配方、工艺、课程、治疗、训练或服务操作；
4. **改造转型**：以材料、人员和时间将现有 Profile 或用途合法转换；
5. **空间扩张**：达到单 Cell 上限后取得新 Cell，建设新 Facility 并加入 Complex。

若功能具有独立人员、库存、生产、损毁、占领、访问规则或战略意义，应成为独立 Facility；
否则作为 Module。任何升级都不能绕过土地、材料、工时、知识、财政、权限和施工阶段。

## 7. 生产、农业、矿业与知识设施

### 7.1 并行生产

生产设施可按 `ParallelProductionCapacity` 同时执行多条工单。约束来自人员、设备、原料、
知识、时间、库存和安全条件，不是“设施等级自动产出”。

### 7.2 农业

农业继续使用数据驱动作物、地方品种和种子批次。一个农业 Facility Cell 的同一主生产周期
只保留一种主作物；混作、轮作、林下经济和辅助作业若要支持，必须以明确方法和面积/能力分配
建模，不能偷偷绕过主作物占用。80%成熟抢收属于可配置作物/方法规则，不写死为所有作物常量。

### 7.3 矿业

矿产属于 `ResourceBody / Deposit` 世界事实，不由 Mine 生成。同一 Cell 可关联多个矿体；
Mine 的工作面、排水、支护、运输、人员和设备决定可开采对象与并行能力。

### 7.4 太学与藏书

太学使用 `Academy` 的 Imperial Profile，并通过真实教师、学生、课程、藏书和学习时间培养人物。
典籍最终应落为 `BookDefinition` 与具体副本/馆藏资产，可支持课程、知识、配方与研究；抄写、
损毁和传播遵循信息资产合同。太学不是全城文化百分比加成。

## 8. 永久人物、家庭与劳动

### 8.1 永久身份不变

所有参与居住、工作、学习、服役和政治的人都是永久 Person。关注和冷热只改变加载、AI频率、
信息与表现，不得删除、合并、替代或重随机人物。

### 8.2 一人一项主要耗时活动

每个 Person 同一世界时段只能拥有一项占用主要时间的活动，例如生产、学习、旅行、服役、
治疗或休息。关系、职位、所有权和被动状态可以并存，但不能借此让同一人同时贡献两份劳动。

本地正常通勤包含在主要活动中，不逐 Cell 计算；跨区域旅行、迁居、赴任、商旅和行军必须真实耗时。

### 8.3 Person、Household、FamilyOrganization

- `Person`：人物和私人产权主体；
- `Household`：共同居住、消费、照护和日常财产的生活家庭；
- `FamilyOrganization`：拥有族产、职位、产业、档案和私军的长期家族组织。

加入家族不自动把私人资产转为族产。家主死亡只改变家主职位；FamilyOrganization 资产不进入
家主私人遗产。私人遗产与主继承人规则由人物/家庭领域设计负责。

Clan、Branch、Household、FamilyOrganization与FamilyCenter的正式分离规则，以
[`FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md`](FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md)
和[`02_FamilyCenter设计规则_V1.md`](FAMILY_ORGANIZATION_REFERENCE_V1/02_FamilyCenter设计规则_V1.md)为准。
FamilyCenter属于FamilyOrganization，不属于Clan；它必须由真实Facility、数据驱动
`FamilyManagement`能力、组织合法产权或控制、Primary/Local正式指定和真实管理者Person共同成立。
成员、住宅、庄园、祠堂、资产或同城任官均不能单独证明中心存在。一个组织最多一个Primary，
同一`ManagementAreaId`最多一个中心；管理者缺位时指定保留但进入`DISABLED/UNSTAFFED`。

## 9. 组织、职位与委任

### 9.1 组织、职位、人物分离

```text
Organization
  拥有资产、财政、档案、职位表和命令能力

Office / Position
  保存辖区、领域、权利、义务、属官和委任边界

Appointment
  把具体 Person 在明确时间内放入职位
```

官署 Facility 是工作地点，不等于政府组织；职位属于组织，人物只是任职者。
异地任命需要真实赴任。空缺不产生简单效率 Debuff，而是事务无人处理、转交代理或上级。

### 9.2 官职是权限，不是 Buff

职位至少应拥有实际权力、分工、任命、监察、政治安置或仕途价值之一。建议合同：

```text
OfficeDefinition
  jurisdiction_scope
  domain_ids[]
  view_rights[]
  action_rights[]
  subordinate_office_ids[]
  delegatable_task_types[]
  approval_only_task_types[]
  vacancy_policy
  automation_presets[]
```

县令、太守/国相、州级主官按辖区取得综合治理权；中央官通常在天下范围管理特定领域。
不设置“县令最多管理十万人”一类固定人口上限，能力差异通过信息质量、执行链、人员、财政、
报告延迟、腐败和积压体现。

### 9.3 委任合同

所有层级支持亲管、部分委任、全部委任和只报异常。委任至少保存目标、辖区、预算、期限、
权限、红线、报告周期、发布者、受任者和职位快照。受任者只能提交领域命令；命令仍需校验
人员、知识、地点、产权、资金、库存与时间。

## 10. 官职、军职与爵位

三者可以同时存在，但作用不同：

| 类型 | 给予什么 | 不自动给予什么 |
|---|---|---|
| CivilOffice | 行政辖区、领域权限、公共资源调度和属官委任 | 土地产权、私人财富、永久兵权 |
| MilitaryOffice | 正规军指挥资格、正常统兵范围、军事命令与下属军职任命 | 实际部队所有权、无限兵力 |
| NobleTitle | 身份、爵禄、封邑收益、继承和政治地位 | 行政治理权、Cell 产权、军队指挥权 |

军职的具体名称与最大正常统兵数仍是历史内容和平衡候选，不能因外部 V1 表格直接冻结。
实际兵权始终来自当前指挥链内的真实 Force 与具体服役人物。允许有职无兵和有期限的超编，
但超编后果必须通过后勤、财政、补员、军政审计和上级命令产生，不直接套战斗力惩罚。

## 11. 皇室、王国与资产边界

### 11.1 皇室组织

皇帝核心家庭是特殊 `FamilyOrganization / ImperialHousehold`，不是全体刘氏宗室。
它主要包含皇帝、后妃、继承人、尚未外居子女和必要的上一代核心成员。

中央继承线通过明确 `ImperialHeirId` 管理，支持立、废、改立与争议。非继承皇子在封王并收到
就国命令后，才离开宫廷并建立自己的王府组织；不得只按年龄自动搬出。

### 11.2 三类资产

```text
State Assets                 国家公共资产
Imperial Household Assets    皇室/宫廷组织资产
Emperor Personal Assets      皇帝私人资产
```

皇帝死亡时，国家资产不进入遗产，皇室组织资产继续属于组织，只有私人资产进入个人继承。

### 11.3 王国

王是爵位体系中的特殊高爵；“国”与郡处于同一行政层级：

```text
AdministrativeLevel = CommanderyEquivalent
RegionType = Commandery | Kingdom
```

王拥有王爵、封国身份、制度收益和王府资产；国相承担与太守相当的郡级行政职责。
王府不等于王国政府，封国关系不改变区域内各 Cell 的产权。

食邑优先通过地方真实征收后再转移支付，财政不足可形成拖欠，不凭空生成收入。

## 12. 政权、自立与汉廷关系

政治状态至少分成四条轴，不能压成单一等级：

```text
PoliticalRole: Subject | Ruler | Emperor
AllegianceTarget: HanCourt | Person/Polity | Self
HanRelation: HanSubject | HanAutonomous | HanSeparatist | IndependentFromHan
SovereignClaim: None | King | Emperor
```

自立是脱离当前上级并建立 `Polity`，不是必然反汉、称王或称帝。成为 Ruler 后取得自身政权的
任命、财政、军队、建设、行政、外交和战争权限，但仍可选择奉汉自治。

汉廷命令逐条评估，可能执行、部分执行、延迟或拒绝。占领洛阳不自动等于控制皇帝；必须控制
皇帝本人、其当前所在地并使其无法自由脱离。保留/废立皇帝与交权/不交权是两个独立选择。

“交权”必须通过组织、财政、公共军队、官职和公共 Facility 控制链的真实移交完成；私人土地、
个人财富、家族资产和合法爵位不自动没收。

## 13. 军队与统一大地图战争接口

### 13.1 军队来源

县兵、郡兵、州兵、正规军和私军都由永久人物、真实装备、库存、马匹、车辆和指挥关系组成。
它们使用共同的服役、Force、装备、补给、伤亡与复员规则；私军不建立第三套虚构官制。

### 13.2 Force

一支独立 Force 有稳定身份、指挥者、具体成员、装备、库存、补给、位置、朝向和任务，并占据
一个 Cell 的 Force 槽。规模不由地图单位类型写死，军职只检查正常统兵边界。

分兵、合军、增援、驻扎、转隶和复员都通过双边事务转移具体人物、完整建制、装备、马匹、
车辆与库存。驻城不等于转为地方军；武将和兵员可以分别去留，但同一武将不能在两支异地 Force
中同时实际指挥。

### 13.3 阵型、方向与持续战斗

大地图站位决定接敌方向；Force 的 `FormationFacing` 把相邻 Cell 攻击映射到正面、两翼或后方。
阵型知识属于人物，阵型效果来自真实部署、兵种、装备、训练、军官、地形、疲劳和情报，
不使用固定硬相克。

战斗可以持续数小时或数日，允许增援、换阵、撤退、补给变化和多方向攻击。内部可聚合结算，
最终伤亡、被俘、溃逃、装备损坏和物资损失必须回写具体人物与世界账。

## 14. 政治与组织 AI

### 14.1 同规则、低频重大决策

玩家和 AI 共用相同命令、权限、知识与成本。自立、废立、重大任命、分家和战争等行为只在重大
事件或条件跨阈值时重新评估，不能让所有 NPC 每月掷一次造反骰子。

### 14.2 多因素决策

政治决策至少读取：

```text
PersonalLoyaltyToLord
HanLegitimacyAffinity
OrganizationCohesion
FamilyInterest
Ambition
RiskTolerance
AllegianceInertia
现实兵力、地盘、财政、班底、家属与资产安全
已知周边威胁和机会
```

自立采用“意愿 × 可行性 × 机会”，高野心不等于必然自立。不满时通常先争取利益、消极服从、
改投强主，最后才考虑独立。主君死亡、继承危机、惨败、夺职、逮捕威胁、中央崩溃和军力暴涨
等事件才是重点重评时机。

### 14.3 下属独立判断

上级自立时，辖下官员、将领、军队和组织不会自动全部跟随。每个关键下属根据个人忠诚、汉室
认同、关系、利益、风险和实际控制重新选择跟随旧主、新主、第三方或自己自立。

### 14.4 可解释与有限认知

AI 只读取个人记忆、合法档案、职位授权和已知情报。每项重要选择需保存或可重建候选、关键原因、
拒绝码与使用的信息时间戳；生成式模型未来只能润色表达，不能直接修改世界事实。

## 15. 当前实现状态

| 领域 | 当前状态 | 证据与边界 |
|---|---|---|
| 2000米 Cell、稳定地址与地图数据 | 已有原型 | MASTER-MAP-V0/V1 与洛阳验证数据；不能改回无尺度 Cell |
| 一 Cell 一个基础 Facility、一个独立 Force 槽 | 已有底座 | `WorldCellOccupancyState` 已有不变量；完整运行时接入仍不等于完成 |
| FacilityDefinition/State、Owner/Controller、住房和岗位 | 已有底座 | 洛阳历史设施领域合同与压力原型；全国正式迁移未完成 |
| 洛阳184历史 Facility 与多 Cell 蓝图 | 已有原型 | 173个历史/复原设施、住房岗位投影和验证场景；非全国内容 |
| 数据驱动产品、配方、工单、库存和部分设施应用 | 已有底座 | M17—M25 多个生产与世界账切片 |
| Facility 五种成长与完整 Catalog | 已定方案 | 目录和规则已归并；尚未完成去重、注册表与通用升级运行时 |
| Person/Household/FamilyOrganization 完整资产边界 | 已有底座（部分） | 永久人物、家庭和统一组织存在；正式家族组织产权与分宗待扩展 |
| 官职/军职/爵位统一权限合同 | 已定方案 | 现有基础职位与军令原型不足以证明完整体系 |
| 皇室主脉、王国、交权与控制皇帝 | 已定方案 | 未形成正式数据、存档和可玩闭环 |
| Force 方向阵型与持续战斗 | 已定方案 | 现有简化战斗不是本设计的完整实现 |
| 政治 AI、自立与下属重评 | 已定方案 | 当前沙盒 AI 和委任提案仅是底座，不得误报完整政治 AI |

## 16. 后续实施顺序建议

本节只表示本领域内部依赖，是否立即执行仍由总纲第 0E 节决定。

1. **Facility 映射审计**：把现有 FacilityDefinition 与 Catalog 候选逐项交叉，识别重复 BaseType、
   Profile、Capability 和缺失稳定 ID；
2. **Facility 通用状态扩展**：在不破坏现有存档的前提下补齐 Operator、访问策略、通用容量、模块、
   耐久、改造和 Complex 引用；
3. **成长纵向切片**：选择住宅、仓库、铁坊、太学和军营各一例，跑通五种成长方式中的适用子集；
4. **组织—职位—Facility 权限切片**：用一县官府和一个家族/商号验证产权、控制、运营、委任、
   空缺与赴任；
5. **郡/国与皇室切片**：验证太守/国相、王府/王国政府、国家/皇室/个人资产分账；
6. **政治状态和 AI 切片**：实现奉汉自治、逐条诏令响应、自立及少量关键下属重评；
7. **军职—Force—阵型切片**：把真实职位、统兵边界、一军一格、方向接敌和世界回写接入现有战争；
8. 每个阶段都必须完成顺序存档迁移、往返测试、守恒测试、确定性测试与可玩交互验收。

## 17. 禁止事项

- 不得因建筑名称、所属阵营或历史场景不同复制平行 Facility 系统；
- 不得使用固定枚举封死可扩展 Facility、职位、爵位、阵型或政治状态内容；
- 不得把综合等级、职位或爵位做成凭空资源加成；
- 不得让委任、AI 或关注系统绕过人物、知识、产权、材料、库存、财政和时间；
- 不得用 Facility 目录文字宣称全国设施已经实现；
- 不得以外部 V1 候选军职数值、年龄阈值或 AI 权重直接升级为正式平衡；
- 不得建立另一套城市、宫廷、家族、战斗或政治世界账。
