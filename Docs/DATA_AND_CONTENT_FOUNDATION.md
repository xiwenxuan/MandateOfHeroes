# 数据与内容底座

## Document Governance

- Purpose：定义稳定ID、历史/重建/模型来源、内容数据与运行时状态分离。
- Authority：L1 CANONICAL SYSTEM SPEC（部分章节已被替代）。
- Covers：数据来源、内容合同、校验与兼容原则。
- DoesNotCover：当前Family/Clan/Branch/Household/FamilyOrganization/FamilyCenter结构。
- Supersedes：早期零散数据约定。
- SupersededBy：Family结构部分由`FAMILY_ORGANIZATION_REFERENCE_V1/01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md`替代。
- RelatedCanonicalDocs：`DETERMINISTIC_SIMULATION_AND_SAVE.md`、`KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md`。
- Status：PARTIALLY_SUPERSEDED。

## 1. 目标

所有人物、家族、地点、身份、组织、任务和事件均采用数据驱动。游戏代码负责解释规则，
内容数据负责描述具体世界。这样才能支持135—260年重点史实、260年后的动态发展以及社区MOD。

## 2. 通用规则

每条数据必须包含：

- 永久稳定ID，不使用显示名称作为引用。
- 简体中文显示名及可选别名。
- 生效和失效时间。
- 史实、演义、原创或动态生成标记。
- 来源、可信度和争议说明。
- 数据版本。

ID一旦进入公开存档不得更改；名称可以随年代变化。

### 2.1 开放内容集合与封闭协议

预期持续增加、允许资料包扩展或由MOD定义的内容集合，统一采用稳定命名空间ID、
数据定义和引用校验，不以固定枚举作为公开内容合同。该原则适用于作物、地方品种、
产品、配方、设施、技艺、词条、科技、政策、装备、兵种、任务和事件等领域。

只有工单生命周期、校验结果、存档状态等真正封闭的程序协议可以使用枚举。新增普通
内容不升级存档结构；只有持久结构变化才升级存档版本。缺失内容必须保留原ID并报告，
不得静默映射为另一个定义、删除玩家资产或重新随机已有世界事实。生产领域的详细合同见
[`PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md`](PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md)。

## 3. 人物

```text
Person
  id
  names[]
  gender
  birth_date / death_date
  origin_location_id
  culture
  historical_status
  attributes
  aptitudes
  traits[]
  values
  skills[]
  health
  reputation
  family_id
  parents[]
  spouse_ids[]
  children_ids[]
  mentor_ids[]
  organization_memberships[]
  public_identities[]
  hidden_identities[]
  relationships[]
  historical_event_ids[]
  sources[]
```

历史人物和生成角色使用同一结构。生成角色额外记录生成种子和生成规则版本。
每个出生人物立即取得永久稳定ID。普通人物可以只保存紧凑基础档案并降低模拟频率，
但不得因玩家未关注而删除、合并为无名人口或重新随机生成。

## 4. 家族

```text
Family
  id
  name
  origin_location_id
  founder_person_id
  current_head_id
  branches[]
  members[]
  properties[]
  shared_wealth
  family_reputation
  family_values
  inherited_knowledge[]
  political_connections[]
  allies[]
  rivals[]
  succession_rule
  secrets[]
  chronicle_entries[]
```

家族与宗族不是完全相同的概念。大型宗族可包含多个可独立经营的家族分支。

## 5. 地点

```text
Location
  id
  names_by_period[]
  location_type
  parent_location_id
  map_position
  adjacent_routes[]
  terrain
  population
  controller
  administrator
  organizations[]
  resources[]
  markets[]
  facilities[]
  public_order
  disease_state
  historical_event_ids[]
  sources[]
```

地点类型：

- capital
- major_city
- county_city
- administrative_seat
- settlement
- village
- pass
- port
- camp
- battlefield
- estate
- personal_base

## 6. 路线

```text
Route
  id
  from_location_id
  to_location_id
  route_type
  distance
  base_travel_days
  terrain
  capacity
  seasonal_effects
  security
  controller
  supply_enabled
```

路线类型包括陆路、水路、栈道、山道和海路。战争、灾害、盗匪和地方关系会改变通行状况。

## 7. 组织

所有势力、官府、军队、商会、家族、师门、宗教和情报网使用统一组织模型。

```text
Organization
  id
  type
  name
  leader_id
  headquarters_location_id
  parent_organization_id
  members[]
  offices[]
  resources
  goals[]
  policies[]
  allies[]
  enemies[]
  secrets[]
```

角色在组织中的职位决定权限和义务，身份本身不直接授予组织控制权。

## 8. 身份

```text
Identity
  id
  category
  name
  recognition_level
  acquired_date
  granted_by
  activity_history
  privileges[]
  obligations[]
  exposed
```

身份分为：

- main：当前核心玩法身份。
- social：公开社会身份。
- office：组织职位。
- hidden：隐藏身份。

## 9. 商品与产业

本节早期`Commodity`与`Industry`结构保留为现状摘要。正式生产内容采用“产品定义、
产品批次、作物定义、地方品种、配方、方法、设施、工单与账本”分层；市场商品是产品的
交易视图，不再与生产材料维护互不相通的固定类型表。

M17-P0已经实现首批作物、地方品种、产品、配方和方法定义、核心JSON资源、稳定注册表、
内容清单及缺失MOD拒绝。当前`commodity.grain`仍是既有地区市场的聚合视图，家庭
`Grain`/`SeedGrain`仍是小麦链兼容余额；二者尚未成为通用产品批次库存，也不得与生产账本
重复计为两份物资。后续通用库存迁移必须保持同一批物资的来源和守恒。

```text
Commodity
  id
  category
  base_value
  weight
  perishability
  legal_status
  production_requirements
  consumption_tags[]

Industry
  id
  type
  owner
  location_id
  workers
  inputs[]
  outputs[]
  capacity
  condition
```

首版商品：

- 粮食
- 布帛
- 木材
- 铁料
- 药材
- 马匹
- 酒
- 盐

货币以钱为主要计价单位，部分地区和危机时期允许以粮、布等实物结算。

## 10. 军队

```text
MilitaryUnit
  id
  organization_id
  commander_id
  officer_ids[]
  soldier_count
  troop_type
  training
  morale
  equipment
  food_supply
  carried_money
  fatigue
  injuries
  current_order
  route
```

军队属于势力、军团、地方组织或个人私兵。士兵、低级军官与主将体验不同层级的战争权限。

## 11. 事件

```text
EventDefinition
  id
  title
  event_class
  historical_mode
  date_window
  location_scope
  participants[]
  prerequisites[]
  blockers[]
  public_information
  hidden_information
  participation_slots[]
  choices[]
  effects[]
  followup_events[]
  cooldown
  repeat_policy
  sources[]
  confidence
```

事件分类：

- era：时代事件
- regional：区域事件
- local：地方事件
- professional：身份事件
- family：家庭事件
- personal：个人事件
- generated：动态事件

历史模式：

- historical：史实方向
- romance：演义方向
- variant：历史变体
- dynamic：动态原创

## 12. 事件参与席位

```text
ParticipationSlot
  id
  role
  required_identity
  required_office
  attribute_requirements
  relationship_requirements
  location_requirement
  capacity
  actions[]
```

席位角色：

- core_actor
- military
- political
- logistics
- intelligence
- medical
- commercial
- local
- witness
- victim

同一历史事件可以向不同身份开放完全不同的任务。

## 13. 任务

```text
Mission
  id
  source_event_id
  issuer_id
  assignee_id
  identity_route
  objective
  deadline
  target_people[]
  target_locations[]
  resource_budget
  approaches[]
  progress
  risks[]
  outcomes
  world_effects[]
```

任务结果必须尽量回写人物、家族、地点、组织、经济或战争状态。

## 14. 家族世录

```text
ChronicleEntry
  date
  category
  involved_people[]
  involved_locations[]
  event_id
  summary
  significance
  historical_context
  player_involvement
```

世录既记录著名历史，也记录家族自己的重大事件。

## 15. 存档原则

- 配置数据与运行时状态分离。
- 存档只记录发生变化的状态和稳定ID。
- 保存随机种子，保证问题可以复现。
- 所有长期系统支持版本迁移。
- 260年后的生成角色、政权和事件必须完整写入存档。

## 16. 首个原型内容包

### 时间

182年1月至190年12月，允许继续发展。

### 地区

- 幽州南部
- 冀州北部
- 司隶部分地区

### 主要地点

- 蓟
- 涿县
- 广宗
- 邺
- 洛阳
- 附属县邑、村庄、道路和关口

### 身份

- 军人：乡勇至校尉
- 官吏：县吏至县令
- 商人：小贩至商队主
- 家主：基础家庭、产业和继承

### 历史内容

- 黄巾传播与地方响应
- 官府搜捕
- 地方募兵
- 桃园结义史实与变体
- 黄巾起义
- 朝廷与地方军队动员
- 洛阳政局变化
- 讨董前置事件

### 普通生活内容

- 婚姻、生育、疾病和死亡
- 土地、粮价、债务和赋税
- 商队运输和盗匪
- 征兵、逃亡和难民
- 拜师、举荐和地方关系
- 家产继承与家族冲突

## 17. 资料整理流程

1. 记录事件或人物条目。
2. 区分正史、演义和后世传说。
3. 添加来源及可信度。
4. 只摘录事实，不复制受版权保护的现代文字表达。
5. 转化为原创的游戏条件、参与席位和结果。
6. 由数据校验器检查缺失引用和时间冲突。
7. 通过快速模拟检查事件是否能在合理条件下发生。

## 18. 行政地理与历史世界状态内容合同（ADMINISTRATIVE-SEAT-CANONICAL-PLACE-V1）

早期由一个`Location`同时表示行政区、城市、战略节点和治所的粗模型标记为`PARTIALLY_SUPERSEDED`。新内容必须明确区分：

- `AdministrativeRegion`：州、郡、国、尹、属国、县等管辖空间；
- `CanonicalPlace`：真实物理地点，引用稳定Cell、道路/水系、Facility、Person、Owner与Controller；
- `PlaceNameTimeline`：同一Place在不同时段的名称，不改变PlacePermanentId；
- `SeatRole`：县治、郡国治、州治、首都等绑定Place的行政/政治Role；
- `HistoricalSeatReference`：Scenario或重要时点的历史治所参考；
- `RuntimeAdministrativeSeat`：当前运行世界中由真实Government Facility、Office/Authority和Controller形成的办公中心；
- `StrategicDisplayLabel`：面向战略视图的熟悉名称，可指Place或AdministrativeRegion，不据此复制城市；
- `HistoricalWorldStateSnapshot`：直接Scenario开局所用的同ID状态集合；
- `HistoricalChangePoint`：重大事件的Pre-State、Canonical ChangePackage和Post-State Reference。

所有历史地理内容使用`HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN`证据层。普通内容新增不得改变稳定ID或要求复制存档结构；证据不足时保留UNKNOWN和研究问题。

当前正式Reference入口为
[`HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md`](HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md)。

## 19. 参考资料、运行时事实与重大事件内容边界（V71）

- 历史人口、县域供需、路线潜力、城市Pack和Scenario Snapshot是Reference或初始化输入；运行后不能作为实时库存、订单、运输、设施或人口真相。
- `WorldSignal`只保存稳定命名空间ID、0—10000值和证据摘要，可随世界事实重算；新增普通信号、动作、条件和ChangeOperation使用数据ID，不升级存档封闭枚举。
- 重大事件内容必须提供稳定Event/Rule/Condition/Operation ID、规则包版本、时间窗口和至少一个非时间前提。仅配置年份的重大事件在校验期被拒绝。
- ChangePackage引用现有Permanent Person、Organization、Location、Facility、Route、Army等ID；引用缺失不得静默改指或临时生成替代对象。
- Canonical结果只是满足当前世界前提时的一个优先规则。Variant、Delayed、Prevented与Transformed均是一等可保存结果。

详细字段合同见[Living World交付目录](HISTORICAL_WORLD_REFERENCE/WORLD_INTELLIGENT_POPULATION_DRIVEN_SIMULATION_AND_HISTORICAL_EVENT_CONTRACT_V1/)。
