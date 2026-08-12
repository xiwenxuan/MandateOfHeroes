# Administrative Seat / Canonical Place / Historical World State V1

## 定位

本目录把项目已有的13州、105郡国等价单位、1182县、77战略显示名、133 Core Settlements、
250重点县和13个Scenario放入同一套可查询关系。它是历史与开发Reference，不是第二套运行时世界。

## 冻结关系

```text
AdministrativeRegion（州/郡/国/尹/属国/县）
        └─ HistoricalSeatReference / RuntimeAdministrativeSeat（角色）
                    └─ CanonicalPlace（真实物理地点）
                              └─ Cell + Facility + Person + Organization + Owner/Controller
```

- 县不等于县城，郡国不等于城市，治所不是Place类型；
- 一个Place可同时承担县治、郡治、州治、首都等角色，但只保留一个PlaceId；
- 直接选择Scenario时使用Snapshot；连续游玩使用运行世界，不用未来Snapshot校正；
- 重大事件按前提后台结算Canonical/Variant/Prevented/Transformed结果；
- 全部历史状态复用同一Cell、Place、Facility和PermanentPerson ID。

## 方法与覆盖

- 时间：13个Scenario（140|184|189|194|200|207|214|219|223|227|234|249|260）+ 32个重大ChangePoint候选 + 状态继承；
- 行政：13州×13切片共169条；105郡国等价单位治所候选全覆盖；
- 地点：133个既有Core Settlement，不创建第二套ID；
- 战略名：77项逐条交叉到75个既有CanonicalPlace，7项保留开放冲突；
- 县治：250重点县中133项已有Core Settlement治所，其他保持UNKNOWN；
- Snapshot：1729条Place×Scenario索引，不复制Unity地图；
- 运行时：未实现，见Implementation Gap。

## 工作簿

1. `01_135-260行政单位与重要历史治所总表.xlsx`
2. `02_135-260_CanonicalPhysicalPlace_Master.xlsx`
3. `03_77战略名称与CanonicalPlace关系表.xlsx`
4. `04_133CoreSettlement_SeatRole_Crosswalk.xlsx`
5. `05_250PriorityCounty_ImportantPlace_And_SeatReference.xlsx`
6. `06_13Scenario_ImportantPlace_WorldStateSnapshot_Index.xlsx`
7. `07_HistoricalMajorChangePoint_Master.xlsx`
8. `08_HistoricalChangePackage_Reference.xlsx`
9. `09_三国志系列重要地点名称交叉参考.xlsx`
10. `10_DevelopmentRelevantPlaceCandidateMaster.xlsx`

洛阳专项位于`11_LUOYANG_MAJOR_HISTORICAL_WORLD_STATES/`；其他P0地点候选位于
`12_P0_PLACE_CHANGEPOINT_CANDIDATES/`。

## 证据与法律边界

`HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN`保持分离。系列游戏只保留抽象重要性研究槽；
本轮没有导入商业游戏地图、坐标、数据库、美术、UI、数值或剧本文本。
