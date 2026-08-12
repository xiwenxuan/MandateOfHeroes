# 135—260历史世界开发参考资料索引

## 定位

本目录是《群雄志：仕途》的**历史开发参考库**，不是第二套运行时世界，也不是对不确定史实的自动补写。运行时继续复用稳定世界地理、永久人物、人口账与ScenarioSnapshot。本库负责回答开发者“某年、某地、某人、某设施应查什么证据，以及哪些仍属推定”。

## 当前覆盖

| 对象 | 数量 | 状态 |
| --- | --- | --- |
| 逐年索引 | 126 | 完整骨架 |
| 州部 | 13 | 区域参考 |
| 郡国 | 105 | 索引 |
| 县级单位 | 1182 | 索引 |
| 战略城市 | 77 | 全量骨架；8个CITY-S详档；首批10个Development Pack |
| 历史人物 | 1202 | 地理分布索引；不是人数上限 |
| Clan/Branch | 39/15 | Clan地理索引；不等于运行时家族组织 |
| Scenario | 13 | 开发参考切片 |

## 证据标签

- `HISTORICAL`：有明确史籍、考古或正式资料支撑的断言。
- `RECONSTRUCTED`：由多项证据保守复原，必须保留推理链。
- `MODELED`：为游戏运行或容量规划建立的项目模型，不冒充史实。
- `UNKNOWN`：证据不足，保留空缺和研究问题。

## 阅读顺序

1. [历史世界总参考](00_WORLD/00_135-260历史世界开发总参考_V1.md)
2. [V1深化层入口](DEEPENING_V1/README_历史世界深化资料索引.md)：Canonical核心聚落、治所时间轴、重点县、地产锚点、产业、交通、军事与13个Scenario空间档
3. 各对象索引工作簿（位于本目录根部）
4. [州部参考](02_PROVINCES)、[城市参考](05_CITIES)与[Scenario参考](14_SCENARIOS)
5. Facility、产业、交通、军事、行政专题
6. [首批核心城市Development Pack](CITY_DEVELOPMENT_PACKS/README_CORE_CITY_DEVELOPMENT_PACKS.md)：洛阳、长安、邺、许昌、成都、襄阳、江陵、建业、合肥与南郑
7. [来源总索引](历史资料来源总索引.xlsx)、[V1覆盖报告](HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1_最终覆盖报告.md)与[深化层覆盖报告](DEEPENING_V1/HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1_COVERAGE_REPORT.md)

## 不变量

- 135—260是主要剧本研究范围，260不是模拟终点。
- 史料人口是参考；实际开局按硬件缩尺，永久人物不得合并、删除或重随机。
- 140年行政截面是稳定地理索引，不代表126年间行政名称从未变化。
- 代理多边形只用于技术定位；不得据此声称真实历史边界。
- 洛阳供给圈70万人包含都市圈40万人，二者不可相加。
- 未解决项继续保留：205个地点、64条关系，以及P0175在219年切片的重叠问题。

## 行政治所、Canonical Place与历史状态入口（ADMINISTRATIVE-SEAT-CANONICAL-PLACE-V1）

[`ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md`](ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md)正式采用：

```text
Scenario Snapshot + Major Historical ChangePoint + Inherited State
```

本库不再把126年逐年人工复原作为目标。州、郡国和县是AdministrativeRegion；治所是绑定真实CanonicalPlace的Role；战略显示名可以与物理Place名称不同。直接Scenario开局使用历史Snapshot，连续游玩使用运行世界；未来史实不强制覆盖分歧结果，但已满足前提并真实发生的重大事件会在离屏世界结算同一Cell/Facility/Person事实。

## 开发地点Roster与资料准备度入口

[`DEVELOPMENT_PLACE_ROSTER_V1/README.md`](DEVELOPMENT_PLACE_ROSTER_V1/README.md)回答“哪些真实地点值得专项开发、做多深、何时做、哪些资料或实现仍阻塞”。

正式Roster为72个地点：D5=1、D4=15、D3=33、D2=23、D1=0。未进入Roster的县、聚落和地点仍在统一世界中以D0/D1事实与模拟存在。D级是项目制作深度，不是州治/郡治/县治等级，也不是City/Pass/Port类型；非城市Place同样可以成为D4。

以后开发任意地点的详细内容，先查Roster、Readiness Matrix和对应City Development Pack；没有Pack时先建包，D4/D5再进入`KNOWLEDGE_BASE/DEVELOPMENT_MANIFESTS/`的独立Manifest。72项Roster可以扩展，D0/D1可以申请升档，但Pack完成不自动改变DevelopmentDepth。首批10城Pack完成后，下一阶段转入`LUOYANG-184-DEVELOPMENT-READINESS-REVIEW-V1`，不自动启动第二批城市。

## 当前 Development Place 完整参考包入口

后续地点开发应先读 [`PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md`](PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md)。该入口保持 72 地点与既有 Wave 不变，并用 T1—T4 替代现行 D2—D5 术语；旧 `DEVELOPMENT_PLACE_ROSTER_V1` 和 `CITY_DEVELOPMENT_PACKS` 作为历史证据与输入材料保留。

72 个地点均按同一 25 模块标准审计。“完整参考包”允许 `UNKNOWN / NO_EVIDENCE / NOT_APPLICABLE`，不表示运行时已经实现，也不自动建立 Cell、Facility、人口、人物在场、家族中心、军营或历史事件。事件地点另行区分永久地理事实、战场区域和事件依赖设施。

## 智能决策与 Simulation Arena V1

[`WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1/`](WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1/)保存 V72 智能决策底座的总报告、十份工作簿、离线 MLP 模型、4,000 次多种子 Arena、决策/事件 Trace 与阶段结论。该目录是运行时实验和开发证据，不新增第二套历史世界资料；历史快照不随 Seed 随机，重大事件仍由结构化前提、运行事实与 ChangePackage 决定。
