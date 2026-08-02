# 确定性世界模拟与跨代存档方案

## 1. 目标

本游戏需要连续模拟135年开始、260年以后仍可发展的世界。玩家可能经历本人、子女、
族人和数代继承，因此技术底座必须满足：

- 相同版本、相同初始数据、相同种子与相同玩家指令，得到相同结果。
- 玩家不在场时，家庭、人物、市场、战争和历史事件继续发展。
- 历史被改写后不强行恢复原轨，但保留“发生了什么、为何改变”的记录。
- 游戏升级后尽可能读取旧存档；失败时能说明缺少什么，而不是静默损坏。
- 支持快速模拟几十年，以自动测试跨代系统。

当前工程版本：

```text
Unity Editor: 2022.3.62f3c1
```

版本来源为工程自身的`ProjectSettings/ProjectVersion.txt`。

### 1.1 确定性世界创世

创世是世界模拟的第一次确定性事务，不是玩家观察地点时反复运行的内容生成器。创世配置
至少保存：

```text
WorldCreationProfile
  scenario_id
  start_date
  population_profile_id
  historical_evidence_policy
  resource_abundance_parameters
  geography_rules_version
  resource_rules_version
  content_schema_version
  rng_algorithm_version
  master_seed
```

资源的史料依据与丰度分开设置。每个资源体保存稳定ID、地理锚点、来源等级、生成规则版本
和真实初始状态；人物或组织是否知道它，由独立认知记录决定。相同内容、规则、算法、配置
和种子必须得到相同初始世界。

创世结束后，缩放地图、切换专题视图、进入场景和开始关注只能加载或展开已有实体。新增矿脉、
设施、文档和人物必须来自有明确时间与原因的运行期发现、建设、抄录、出生或其他领域事务，
不能通过再次调用创世规则偷偷替换事实。

## 2. 时间模型

### 2.1 核心时间

模拟核心只保存单调递增的整数：

```text
WorldTime
  absolute_day       // 从世界开局起经过的完整天数，64位整数
  segment            // 0清晨、1白昼、2黄昏、3夜间
```

- **一天**是世界经济、人口、健康与历史条件检查的最小稳定结算单位。
- **时段**用于旅行、会面、战斗和个人行动，不要求所有离屏人物逐时模拟。
- UI显示的年、月、日由`CalendarService`换算，不直接参与规则计算。

### 2.2 历史历法

东汉使用的历法、闰月与现代公历不能简单等同。基础实现采用两层日期，历史标签的内容
精度可以随资料继续扩展：

```text
SimulationDate       // 稳定整数日，用于全部计算
HistoricalLabel      // 年号、年序、月、日、闰月标记、考证状态
```

历史事件可以只知道“184年春”或“十月”，因此时间窗允许不同精度：

```text
DateWindow
  earliest_day
  latest_day
  precision          // exact_day | month | season | year | approximate
```

没有可靠日期时不能伪造精确到日的时间。

### 2.3 推进规则

时间只允许通过统一入口推进：

```text
AdvanceTime(command, duration)
```

一次推进依次执行：

1. 验证玩家行动与当前位置；
2. 消耗时间、食物、金钱和物品；
3. 推进当前时段；
4. 到达日界时结算日任务；
5. 到达旬、月、季、年界时执行对应低频系统；
6. 产生事件候选；
7. 按稳定优先级解决冲突；
8. 写入世界日志；
9. 返回玩家可见结果。

不允许UI、动画或任意组件自行修改世界日期。

## 3. 多频率世界模拟

不对全世界每个人每天运行完整AI，而是按重要性分层：

| 层级 | 对象 | 更新频率 | 内容 |
|---|---|---|---|
| L0现场 | 玩家所在地点、战斗和同行者 | 每时段/即时 | 移动、对话、战斗、任务 |
| L1近区 | 相邻地点、直系家庭、关键组织 | 每日 | 行程、健康、关系、市场 |
| L2区域 | 同州战区、商路、相关势力 | 每旬 | 人口、治安、补给、职位 |
| L3天下 | 远方普通人物与非关键地点 | 每月 | 聚合经济、迁徙、组织决策 |
| L4长期 | 家族人口、土地、气候趋势 | 每季/每年 | 生育、成长、产业、继承 |

历史关键人物和已建立关系的人物最低保持L1或L2，不能因离开镜头便冻结人生。

### 3.1 聚合与展开

远方村庄可以使用“120户、劳力结构、疾病率、财富分布”作为结算缓存，但每个出生人物
从出生起已经拥有永久ID和基础档案。玩家接近时，只展开具体家庭的详细资料和表现；
离开后可以释放详细视图，但永久人物不得删除、合并或重新随机生成。

```text
PopulationSummaryCache
  permanent_person_ids / partition_range
  cohort_counts
  household_ids
  wealth_bands
  health_bands
  occupations
  generation_seed
  summary_revision
```

该缓存用于加速天下规模结算，不能成为人口事实来源。人物事实仍以永久人物档案为准。

## 4. 确定性随机数

### 4.1 禁止全局随机流

若所有系统共用一个随机数生成器，增加一条无关的市场事件就可能改变某场战争结果。
因此采用**命名随机流**：

```text
RandomKey
  master_seed
  rng_algorithm_version
  system_id
  entity_id
  absolute_day
  purpose_id
  draw_index
```

示例：

```text
market_price / L001 / day_180 / grain / 0
pregnancy     / P023 / day_912 / conception / 0
battle_hit    / B104 / day_302 / unit_C013 / 17
```

某个系统增加抽签次数，不应改变其他系统的结果。

### 4.2 算法要求

- 自行封装一个版本固定、跨平台结果明确的整数算法；候选为`xoshiro256**`。
- 不直接使用`UnityEngine.Random`进行世界模拟。
- 不使用系统时间、对象内存地址或集合遍历顺序作为种子。
- 随机流算法一旦发布不得原地替换；升级时增加`rng_algorithm_version`。
- 战略计算尽量使用整数、定点数和万分比，避免不同平台浮点误差。

### 4.3 随机与命运

随机结果不是提前写死全部人生。结果由“稳定随机流 + 当时世界状态”共同决定：

```text
result = Rule(world_state, keyed_random_value)
```

玩家改善卫生、粮食和医术会改变死亡概率区间；即使随机值不变，结果也可能改变。
存档读档不能通过反复刷新同一随机事件，但玩家可以通过更早的真实决策改变条件。

## 5. 命令、事件与世界日志

### 5.1 三种对象分离

```text
Command
  玩家或AI准备做什么

DomainEvent
  世界确认发生了什么

NarrativeEntry
  不同身份知道或相信发生了什么
```

例如：

```text
Command:      商队尝试穿越封锁
DomainEvent:  货物被官军征用30石
Narrative:    官文称“依例借粮”，商队账簿称“强征”
```

事实层与叙事层分离，正好支持史实、官方记录、民间传闻和密探情报。

### 5.2 稳定处理顺序

同一天的命令按以下键排序：

```text
absolute_day
segment
phase_priority
organization_id
actor_id
command_sequence
```

所有字典、集合在影响结果前必须按稳定ID排序。不能依赖C#字典的枚举顺序。

### 5.3 日志用途

世界日志保存重要领域事件：

- 出生、死亡、婚姻、收养、继承；
- 任职、离职、投靠、叛离、组织兴亡；
- 财产转移、债务、契约、土地与产业；
- 战争、迁徙、灾害、疾病与历史事件；
- 玩家选择和不可逆结果。

普通的每日饥饿数值不需要永久逐条保存，可由快照承载。

## 6. 历史事件与时间线分歧

每个历史锚点拥有状态：

```text
HistoricalAnchorState
  anchor_id
  status              // dormant | eligible | active | resolved | prevented | transformed
  canonical_outcome
  actual_outcome
  divergence_score
  causal_event_ids[]
```

处理原则：

- 条件未被改变时，AI倾向作出符合史实性格、利益和局势的选择。
- 关键人物死亡、地点易手、组织不存在时，事件可以转化或被阻止。
- 不凭空复活人物、不瞬移军队、不强行制造同名势力来修正历史。
- 历史分歧必须记录原因，并影响后续人物目标与合法性叙事。

例如玩家提前救走卢植，不代表广宗自动陷落；系统重新计算官军统帅、围城进度、
朝廷问责和董卓的去向。

## 7. 存档格式

### 7.1 存档包

基础存档实现使用一个目录或压缩包，逻辑上包含；未来可以替换物理存储，不能削弱同一
兼容合同：

```text
save/
  manifest.json
  world_snapshot.json
  event_journal.jsonl
  player_profile.json
  mods.json
  thumbnail.png
  checksum.json
```

原型期优先使用可读JSON，方便调试和社区检查。性能成为问题后，可把世界快照换成二进制，
但`manifest`、版本和迁移规则仍保持公开。

### 7.2 Manifest

```text
SaveManifest
  save_format_version
  game_version
  engine_version
  content_schema_version
  world_rules_version
  rng_algorithm_version
  master_seed
  world_creation_profile_id
  world_creation_profile_hash
  knowledge_schema_version
  current_time
  player_person_id
  active_heir_id
  timeline_id
  created_at_utc
  saved_at_utc
  required_mods[]
  content_hash
```

现实世界保存时间只写入元数据，绝不参与模拟随机数。

世界快照还必须保存资源体、设施网络、建设委任、人物观察、文档资产、档案访问权及其稳定
引用。观察记录的过时不允许回头修改当时保存的观察内容；加载后依据当前世界时间计算时效。

V8开始保存`ProductionContentManifestState`，记录生产内容包ID、版本、加载顺序、包哈希和
最终解析哈希。普通新增内容不升级世界存档结构；加载时内容清单或工单定义引用不匹配必须
显式拒绝并报告原ID，不能静默替换产品、删除库存或重随机生产结果。

V9增加人物开放技能、知识与技术掌握扩展，以及科研项目、设施级技术应用、科研账和农业
工单技术快照。技能、知识和科技使用稳定内容ID；增加普通定义不升级世界版本，只有新增或
改变持久状态结构时才继续顺序迁移。人物详细扩展可以随M15-P6冷热档案保存，但不能替代
永久人物核心身份。

### 7.3 身份与引用

- 所有实体采用稳定字符串ID或128位ID。
- 历史内容使用人工分配ID，如`person.liu_bei`。
- 动态实体ID由世界ID、生成批次和序号稳定派生。
- 存档不直接保存Unity场景对象引用、实例ID或资源内存地址。
- 关联对象通过ID解析；缺失引用进入错误报告，不静默指向其他对象。

### 7.4 原子保存

保存流程：

1. 写入临时存档目录；
2. 计算各文件校验值；
3. 重新读取关键头信息；
4. 标记临时包完成；
5. 保留旧存档备份；
6. 以替换方式提交新存档。

自动存档至少保留最近3个轮换版本。崩溃时优先恢复最后一个校验通过的包。

## 8. 版本迁移

当前正式世界模式已推进到V9：V6人物通过顺序迁移进入内联人口模式；V7可以附着带
SHA-256清单的分区人口包；V8增加数据驱动生产工单、产品账本与生产内容清单；V9增加
人物技能/知识/技术掌握、科研项目、设施应用、科研账和生产工单技术快照。当前共存
阶段仍保留全部内联人物，后续只有在模拟系统改用人物仓储访问层并完成迁移验收后，才允许
卸载未驻留人物。生产内容迁移和冷热切换均不得改变永久身份或重新生成已经固化的详细扩展。

版本采用单调整数：

```text
save_format_version: 1
content_schema_version: 1
world_rules_version: 1
rng_algorithm_version: 1
```

每次升级提供单向迁移：

```text
V1 -> V2 -> V3
```

规则：

- 加字段必须有明确默认值。
- 改名通过迁移完成，不能让读取器猜测。
- 删除字段前至少经过一个弃用版本。
- 稳定内容ID发布后不得复用给其他对象。
- 迁移前复制原存档；迁移失败不覆盖原件。
- 新版本可以读取支持范围内的旧存档，但不承诺旧版本读取新存档。

规则版本升级可能使未来模拟与旧版本不同，但已经发生的事实必须保留。

## 9. MOD与存档

存档记录每个MOD的：

```text
mod_id
version
load_order
content_hash
required
```

缺失MOD时：

- 仅含外观资源：警告后允许替代。
- 含地点、人物、事件或规则：默认阻止直接载入，提供“复制存档并尝试修复”。
- 修复时生成占位记录并列出损失，绝不悄悄删除玩家家族成员或财产。

MOD加载顺序必须固定；冲突解析结果写入日志。

## 10. 快速模拟

开发模式提供无画面的快速推进：

```text
Simulate(seed, start_date, years, command_script)
```

至少输出：

- 年末世界状态哈希；
- 人口、家庭、势力、市场和战争摘要；
- 历史锚点结果；
- 无效引用、负数资源和死循环报告；
- 同种子重复运行的一致性结果。

基础自动测试批次：

1. 同种子运行10年两次，年度哈希完全一致。
2. 改变市场随机流，不改变无关的生育与战斗抽签。
3. 从第5年存档再载入，跑到第10年与连续运行一致。
4. V1存档迁移到V2后，人物、家族、财产和历史事实不丢失。
5. 模拟135—300年，无ID重复、无无效继承人、无负人口。
6. 玩家本人死亡后，指定继承人能继续推进时间。
7. 相同创世配置和种子生成相同资源体、设施、人口与初始认知哈希。
8. 切换地图比例尺或专题视图前后，世界事实哈希保持不变。
9. 组织档案抄录为家族副本后，原档案更新不自动修改副本，往返存档保持来源与时间。

## 11. Unity工程边界

建议程序集：

```text
Mandate.Domain       // 纯C#世界规则，不依赖UnityEngine
Mandate.Simulation   // 调度、AI、事件和随机流
Mandate.Persistence  // 存档、迁移、校验
Mandate.Content      // 数据加载、验证、MOD覆盖
Mandate.Presentation // 场景、UI、动画、音频
Mandate.Tests        // 编辑器与纯逻辑测试
```

核心模拟必须能够脱离场景和画面运行。Unity组件只负责显示和输入，不能成为人物、家庭、
战争或市场的唯一真相来源。

## 12. 实施顺序

1. 建立纯C#的`WorldTime`、稳定ID和命名随机流。
2. 建立最小`WorldState`：6个地点、10个人物、2个家庭、2个组织。
3. 实现每日推进与事件日志。
4. 实现V1 JSON快照、保存校验和重新载入。
5. 做“连续运行=中途存读”的一致性测试。
6. 加入离屏家庭、旅行和市场。
7. 加入历史锚点状态及184年黄巾事件。
8. 最后连接地图UI与战争表现。
