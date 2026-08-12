# HAN-135-260-ADMINISTRATIVE-SEAT-CANONICAL-PLACE-AND-HISTORICAL-WORLD-STATE-V1

## Document Governance

- Purpose：建立行政区、治所角色、Canonical Physical Place、历史世界状态与开发候选的统一Reference。
- Authority：L3 Historical / Content Reference Task。
- Covers：13州、105郡国等价单位、1182县、77战略名称、133 Core Settlements、250重点县、13 Scenario、重大ChangePoint与Reference ChangePackage。
- DoesNotCover：运行时Place重构、Save Schema、Unity Scene、正式HistoricalChangePackage执行、最终开发Roster。
- RelatedCanonicalDocs：`GAME_SYSTEMS_MASTER_AND_STATUS.md`、`DATA_AND_CONTENT_FOUNDATION.md`、`DETERMINISTIC_SIMULATION_AND_SAVE.md`。
- Status：COMPLETED_REFERENCE_V1。

## 目标

把项目既有稳定ID放入下列统一关系，不创建第二套县、聚落或地图：

```text
AdministrativeRegion（州/郡/国/尹/属国/县）
        └─ SeatRole（历史治所或运行时治所）
                └─ CanonicalPlace（物理地点）
                        └─ Cell + Facility + Person + Organization + Owner/Controller
```

历史时间资料采用：

```text
Scenario Snapshot + Major Historical ChangePoint + Inherited State
```

直接Scenario开局读取历史Snapshot；连续游玩读取实际运行世界。重大事件只有在前提成立时才在离屏世界结算，结果允许Canonical、Variant、Prevented或Transformed，未来史实不得覆盖已经分歧的世界。

## 冻结规则

1. `AdministrativeRegion != CanonicalPlace`。
2. `Seat`是行政/政治角色，不是物理地点类型。
3. `County != CountySeat`；一个县可以包含多个聚落、设施和地理节点。
4. `HistoricalSeatReference != RuntimeAdministrativeSeat`。
5. 一个Place可以同时承担县治、郡治、州治和首都等多个Role，但始终只有一个PlaceId。
6. 改名不改变PlaceId；使用`PlaceNameTimeline`。
7. 不同Scenario复用同一Cell、Place、Facility、Person和Family ID。
8. 重大历史事件属于整个世界；玩家不在场不阻止其结算。
9. 史料只证明大范围后果时，具体设施使用`MODELED`或`UNKNOWN`，不得伪标`HISTORICAL_DESTROYED`。
10. 《三国志》系列只作为合法抽象的重要性参考，不导入商业地图、坐标、数据库、美术、UI、数值或剧本文本。

## 交付物

- `HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/`：10份主工作簿、洛阳专项、7个P0地点候选、README与总报告。
- `MapPipeline/scripts/build_han_administrative_seat_world_state_v1.py`：从既有稳定数据生成交叉Reference。
- `MapPipeline/scripts/build_han_administrative_seat_world_state_workbooks_v1.mjs`：使用`@oai/artifact-tool`生成工作簿、来源页、公式摘要和预览。
- `MapPipeline/scripts/validate_han_administrative_seat_world_state_v1.py`：覆盖、ID、重复、引用、工作簿、预览和文档回写验收。
- Knowledge Base七个Registry增量更新。

## 完成口径

- 13州×13 Scenario治所关系可查询；
- 105郡国等价单位候选治所全覆盖，并明确候选证据不等于专项考证；
- 1182 CountyPermanentId完整且未重建；
- 77战略显示名全部重新解释并交叉到既有Place；
- 133 Core Settlements×13 Scenario角色交叉完整；
- 250重点县县治/其他地点研究状态可查询；
- 重大ChangePoint与Reference ChangePackage具有有效稳定目标；
- 洛阳190不使用单一“破坏度”，也不制造第二套人口、Facility或地图；
- 核心设计和Knowledge Base已回写；
- 工作簿无公式错误并具有逐Sheet渲染证据；
- 文档模式验证和`git diff --check`通过。

## 运行时边界

本任务只记录Implementation Gap。正式`CanonicalPlace`运行时合同、`RuntimeAdministrativeSeat`、历史事件前提检查、事务ChangePackage、离屏结算和存档恢复必须另开代码任务，不得由本Reference任务顺手实现。

## 后续

下一资料阶段为`DEVELOPMENT-PLACE-ROSTER-AND-REFERENCE-READINESS-V1`，届时才决定哪些城市型聚落、县城、关隘、港渡、水军节点、军事据点、战场和特殊地点进入具体开发批次。
