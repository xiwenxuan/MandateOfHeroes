# HAN-135-260-HISTORICAL-PERSON-CLAN-MASTER-V1

## 1. 状态

状态：**V1 已完成（结构与运行时验收通过；史料研究缺口保留）**
完成日期：2026-08-10

本任务把既有 `01_140-264历史人物与时间轴母库_V5.xlsx` 的 1202 个稳定人物，升级为
135—260 年统一历史人物、宗族、支系、亲属、婚姻、位置、官职、爵位、势力与剧本切片母库。
“完成”表示 V1 数据合同、交付物、运行时读取、查询接口和审计链完成，不表示所有东汉末至
三国人物及全部行年已经穷尽。

## 2. 任务边界

- 保留 `P0001`—`P1202`，不自动重编号、删除、合并或重新随机。
- 同名人物不自动合并；`P0182` 与 `P0239` 两条“孙夫人”记录继续独立存在。
- `Clan`、`Branch`、`FamilyOrganization`、`Household` 分层；本任务不生成后两者及其资产。
- 籍贯、郡望、出生地、历史当前位置分别存储；籍贯不得回填为当前位置。
- 史料冲突与模糊项必须进入审计队列，不以玩法推断静默覆盖。
- 184 洛阳既有 25 名实名历史人物作为兼容回归基线，不重复建立第二套人物表。
- 正式剧本切片来自同一人物与时间轴母库，不维护互相漂移的剧本副本。

## 3. 输入整理结论

详细逐项判断见
[历史输入整合审计](HISTORICAL_INPUT_INTEGRATION_AUDIT_V1.md)。

| 输入 | 处理结论 |
|---|---|
| `01_140-264历史人物与时间轴母库_V5.xlsx` | 已处理；作为 1202 人稳定身份基线导入，不重复另造名单 |
| `02_历史剧本切片总索引_140-264.md` | 未统一治理部分已合并到正式剧本规范 |
| `03_历史剧本_ScenarioSnapshot_数据规范.md` | 未统一治理部分已合并到正式剧本规范 |
| `04_HistoricalTimePoint_StartPoint_FateDecision_规范.md` | 未统一治理部分已合并到正式剧本规范 |
| `05_184黄巾起义世界切片_V1.xlsx` | 已有洛阳包部分落地；本任务仅作 184 兼容回归和剧本锚点，不重复导入 |

统一后的治理文件为
[历史剧本、时间点、开局点与命运决策规范](HISTORICAL_SCENARIOS_TIMELINE_AND_FATE_DECISIONS.md)。

## 4. 已完成实现

### 4.1 数据管线

- 建立可重复生成脚本：
  `MapPipeline/scripts/build_han_135_260_historical_person_clan_master_v1.py`。
- 建立深度校验脚本：
  `MapPipeline/scripts/validate_han_135_260_historical_person_clan_master_v1.py`。
- 建立研究报告指标脚本：
  `MapPipeline/scripts/report_han_135_260_historical_person_clan_master_v1.py`。
- 导入基线保存在
  `Data/HistoricalPersons/han_135_260_historical_person_clan_existing_v5.json`。

### 4.2 正式运行时包

运行时包位于：
`Assets/StreamingAssets/HistoricalPersons/Han135260V1/`。

包内包含人物、别名、宗族、支系、亲属、婚姻、人物位置、文武官职、爵位、势力、
宗族地理存在、来源、引文、审计、剧本索引以及 13 个正式剧本 JSON。清单记录每个文件
的 SHA-256；读取时同时校验路径、散列和引用完整性。

### 4.3 代码边界

- Domain：`HanHistoricalPersonClanState.cs` 定义纯 C# 数据合同。
- Persistence：`HanHistoricalPersonClanDatasetReader.cs` 读取、校验正式包，并可从同一时间轴派生
  135—260 任意年份的 `HistoricalTimePoint`。
- Simulation：`HanHistoricalPersonClanQuerySystem.cs` 提供人物、父母、子女、配偶、兄弟姐妹、
  祖先、后裔、宗族成员、支系、地理存在和历史状态查询。
- Tests：`HanHistoricalPersonClanMasterV1Tests.cs` 覆盖身份稳定、同名隔离、收养隔离、婚姻不改
  出生宗族、籍贯不冒充当前位置、13 剧本同源、任意年份派生、洛阳兼容和包散列。

## 5. V1 量化结果

| 指标 | 结果 |
|---|---:|
| Active Historical Person | 1202 |
| S / A / B / C | 82 / 539 / 513 / 68 |
| 女性人物 | 61 |
| Anonymous Historical Person | 30 |
| 确认 Clan / Branch | 39 / 15 |
| 亲属 / 婚姻关系 | 327 / 37 |
| 已知父亲 / 母亲 / 有配偶人物 | 148 / 21 / 57 |
| 人物位置 / 文官 / 武官 / 爵位 / 势力时间轴 | 54 / 14 / 10 / 7 / 24 |
| Clan Geographic Presence | 38 |
| Source / Citation | 30 / 1311 |
| 争议 / 未解析地点 / 未解析关系 | 3 / 205 / 64 |
| 祖先环 / 死后时间轴警告 | 0 / 0 |
| FamilyOrganization / Household / 家族资产 | 0 / 0 / 0 |

13 个正式剧本年份为：140、184、189、194、200、207、214、219、223、227、234、249、260。

## 6. 验收与限制

### 6.1 已通过

- 1202 个既有 ID 全部保留且唯一。
- 数据深度校验通过；无悬空人物、宗族、支系、时间轴或剧本引用。
- 13 个剧本可由同一母库直接生成；184 洛阳 25 人兼容差异为 0。
- 39 个 Clan 与 15 个 Branch 可运行时查询。
- 未生成 FamilyOrganization、Household 或家族资产。
- 11 个 Excel 工作簿共 68 个工作表通过公式扫描与渲染检查。

### 6.2 明确保留

- `P0175` 在 219 年有一组重叠地点记录，状态为 `MANUAL_REVIEW_REQUIRED`。
- 205 个地点文本尚不能安全映射到全国稳定地理 ID。
- 64 条原始关系仍因同名或缺名无法安全解析。
- 184 年仅 8 人有可定位到州级 ID 的同时期当前位置；其余人物只能报告籍贯分布，不能把
  籍贯伪装成 184 年所在位置。
- 599 个宗族候选中 39 个确认为 Clan、1 个确认为 Branch、559 个证据不足；不按同姓自动建族。

因此下一阶段可以**安全启动版本化、保守的**全国 FamilyOrganization 分布设计，但必须继续
携带未解析队列，不得宣称全国历史人物行年研究已经完成，也不得立刻随机生成全国宗族资产。

## 7. 交付目录

- 研究交付：`outputs/HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1/`
- 运行时包：`Assets/StreamingAssets/HistoricalPersons/Han135260V1/`
- 正式研究报告：
  `outputs/HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1/11_135-260历史人物与宗族研究报告_V1.md`
