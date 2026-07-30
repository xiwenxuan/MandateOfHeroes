# 数据与内容底座

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

