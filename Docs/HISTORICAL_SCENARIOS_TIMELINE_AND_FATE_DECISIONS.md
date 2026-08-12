# 历史主剧本、时间点、开局点与命运抉择统一规范

## Document Governance

- Purpose：定义Scenario、HistoricalTimePoint、StartPoint与FateDecision的正式边界。
- Authority：L1 CANONICAL SYSTEM SPEC。
- Covers：历史剧本和时间切片语义。
- DoesNotCover：具体历史事实穷尽或运行时世界初始化完成度。
- Supersedes：早期按单一剧本/年份混用的定义。
- SupersededBy：无。
- RelatedCanonicalDocs：`HISTORICAL_WORLD_REFERENCE/README_历史世界开发参考资料索引.md`、`GAME_SYSTEMS_MASTER_AND_STATUS.md`。
- Status：CANONICAL。

## 1. 文档定位

本文是历史剧本层的统一治理文档，吸收并取代外部研究稿中的：

- `02_历史剧本切片总索引_140-264.md`；
- `03_历史剧本_ScenarioSnapshot_数据规范.md`；
- `04_HistoricalTimePoint_StartPoint_FateDecision_规范.md`。

它规定剧本数量、连续时间轴、完整世界快照、开局增量和历史分歧的边界。人物与宗族事实由
`TASK_HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1.md`及其运行时数据拥有；人口数值由
`TASK_HAN_135_260_NATIONAL_POPULATION_DISTRIBUTION_V1.md`拥有。本文件不复制这些母库。

核心原则：

> 主剧本少而稳定，历史时间轴细而连续；历史提供起点、条件和倾向，不强制已经被世界改变的结果。

## 2. 五类历史对象

| 对象 | 是否完整世界状态 | 用途 | 数据来源 |
| --- | --- | --- | --- |
| `Scenario` | 是 | 菜单可直接选择的正式主剧本 | 各系统母库在指定日期的完整投影 |
| `HistoricalTimePoint` | 否 | 连续历史时间轴中的重要锚点 | Timeline查询结果 |
| `StartPoint` | 增量 | 主剧本内部值得直接接管的阶段 | Scenario + Timeline + HistoricalDelta |
| `HistoricalEvent` | 否 | 满足条件时可能发生的历史过程 | 事件定义与当前世界事实 |
| `FateDecision` | 否 | 人物在真实约束下做出的历史分歧决策 | 决策者、权限、资源、关系与世界状态 |

禁止为每个精彩瞬间复制一套人物、家族、地图、Facility或Force主表。只有基础政治格局长期改变、
确需独立完整初始化、且不能由既有Scenario与Timeline表达时，才考虑新增主剧本。

## 3. 冻结的13个正式主剧本

| ScenarioId | 年份 | 名称 | 核心定位 |
| --- | ---: | --- | --- |
| `scenario.han.140.peace` | 140 | 汉室承平 | 正常东汉社会基线 |
| `scenario.han.184.yellow_turban` | 184 | 黄巾起义 | 帝国危机正式爆发 |
| `scenario.han.189.luoyang_coup` | 189 | 洛阳政变 | 宫廷政变、董卓入京与皇帝控制权变化 |
| `scenario.han.194.warlords` | 194 | 群雄割据 | 独立军政集团全面形成 |
| `scenario.han.200.guandu_eve` | 200 | 官渡前夜 | 曹袁北方决战阶段 |
| `scenario.han.207.longzhong` | 207 | 三顾茅庐 | 北方统一与刘备战略转型 |
| `scenario.han.214.yizhou_settled` | 214 | 益州初定·三分渐成 | 三国主要集团基本成形 |
| `scenario.han.219.hanzhong_king` | 219 | 汉中王·荆州危局 | 刘备集团高峰与荆州存亡 |
| `scenario.han.223.baidicheng` | 223 | 白帝托孤 | 蜀汉继承与战后重建 |
| `scenario.han.227.northern_expedition` | 227 | 出师北伐 | 第一次北伐时代开启 |
| `scenario.han.234.wuzhang` | 234 | 五丈原 | 诸葛亮最后一次大规模改变格局的机会 |
| `scenario.han.249.gaopingling` | 249 | 高平陵之变 | 曹魏中枢向司马氏转移 |
| `scenario.han.260.endgame` | 260 | 曹髦之死·三国终局 | 覆盖至263灭蜀和264直接余波 |

正式剧本当前固定为13个；135—139用于连续历史母库和前推校核，不单列主剧本。

## 4. 重要HistoricalTimePoint归属

| 年份 | 时间点 | 所属Scenario |
| ---: | --- | --- |
| 190 | 反董卓联盟 | 189 |
| 196 | 奉迎献帝至许 | 194 |
| 202 | 袁绍死后河北继承危机 | 200 |
| 208 | 赤壁之战 | 207 |
| 211 | 潼关之战 | 207/214历史链 |
| 217 | 汉中争夺 | 214 |
| 220 | 汉亡魏立 | 219 |
| 221—222 | 东征与夷陵 | 223前置链 |
| 228 | 第一次北伐与街亭 | 227 |
| 229 | 孙权称帝 | 227 |
| 255 | 洮西大捷 | 249 |
| 257 | 寿春之乱 | 249 |
| 263—264 | 魏灭蜀、成都抉择及后续 | 260 |

任意年份仍应通过统一Timeline查询，不为这些时间点维护第二套人物或宗族表。

## 5. ScenarioSnapshot边界

完整主剧本快照包含以下投影：

```text
ScenarioSnapshot
├─ ScenarioMeta
├─ WorldState
├─ ImperialState
├─ PolityState
├─ AdministrativeState
├─ PersonState
├─ HouseholdState
├─ FamilyOrganizationState
├─ CellState
├─ FacilityState
├─ ForceState
├─ EconomyState
├─ DiplomacyState
├─ HistoricalTimelineState
└─ Validation
```

地图地理本体、历史人物母库、Clan母库和内容定义均不复制。Snapshot只记录当日运行状态和对
母库的稳定ID引用。`Polity`控制不等于Cell产权；王爵不等于封地全部资产；Household不等于
FamilyOrganization；Clan不等于二者。

历史人物与普通永久人物在运行时使用同一Person结构。历史人物必须占人口母盘中的人口槽，
不得作为人口总量之外的附加数据幽灵。

## 6. StartPoint合同

```text
StartPoint
{
    StartPointId
    ScenarioId
    Date
    DisplayName
    RequiredHistoricalState
    HistoricalDelta
    PlayerRecommendedRole
    Difficulty
    PendingEvents
    FateDecisions
}
```

StartPoint从Scenario、连续Timeline和增量生成。增量必须可审计，不能偷偷复制和改写主母库。
难度来自真实军力、地理、物资、关系和政治状态，不使用无来源数值作弊。

## 7. FateDecision合同

FateDecision不是脱离世界的剧情选项框。决策能否提出、执行和成功，必须检查：

- 决策者是否存活、在场并拥有相应权限；
- 人物认知、关系、历史倾向和既有承诺；
- 真实军力、粮食、道路、设施、财政和通信；
- 已发生事实与尚未发生的候选事件；
- 决策后的持久世界后果、记忆和审计链。

219荆州、221/222夷陵、228街亭、255洮西、263蜀汉存亡是高优先级命运抉择链，但不会增加
主剧本数量。

## 8. 历史结果不强制

若前置事实已变化，后续史实事件只能失效、改写或产生新分支。例如关羽未北伐时不得强制
播放“威震华夏”；街亭由其他将领守住时不得强制马谡败退；刘禅未投降时不得强制执行以投降为
前提的历史结局。

历史数据只提供：已发生事实、候选事件、AI倾向、关系和来源。运行结果由同一世界规则产生。

## 9. 验收

1. 13个ScenarioId唯一且来源于统一母库；
2. StartPoint和HistoricalTimePoint不复制人物、Clan或地图母表；
3. Snapshot中的PersonId、RegionId、CountyId、FacilityId和ForceId都能解析；
4. 历史事件只在前置条件成立时进入待触发集合；
5. FateDecision不能绕过人物权限和真实资源；
6. 玩家改写历史后，失效史实不会被强制恢复；
7. 260主剧本可承载263—264增量，但260不是世界模拟终点。

## 10. 条件历史事件运行合同（V71）

重大历史事件进入时间窗口后仍须满足人物存活/位置、组织控制、设施状态、军队、道路或前置事件等至少一个非时间条件。解析器按稳定Rule ID和优先级选择Canonical、Variant、Prevented或Transformed；条件尚可能成立时为Delayed/Watching，超过窗口仍不成立时Expired。年份本身不能单独触发重大事件。

事件可在玩家离屏时应用，但只能通过幂等`HistoricalChangePackage`改变当前世界真实对象。直接选择较晚Scenario允许按该剧本Snapshot初始化；从较早年代连续运行则永远保留实际分支，不使用未来Snapshot纠偏。
