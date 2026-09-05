# 统一世界行政边界与县域规划视角 V1 实施报告

## 1. 当前结论

本轮已经把州—郡国等价区—县接入同一个 `HanWorldV1` / Global Cell 世界，
没有建立 `WorldMap2`、独立县地图或第二套 Facility。正式玩家地图已具备行政
边界 LOD、Cell 内点选区、行政信息、县域高亮、县域规划、连续缩放、中键平移
和右键旋转。县域规划是可重建的 Presentation/ViewState，不写入 `WorldState`。

当前交付状态：

`IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`

这是准确的工程状态，不等于完整历史县界、县域建设工具或洛阳空间恢复完成。

## 2. 开工快照

- HEAD：`940c4381da4cbb893c0882fd28e68914397af897`
- 分支：`codex/m23-p4-quality-artisan-growth`
- Unity：`2022.3.62f3c1`
- 正式玩家入口：`Assets/Scenes/PlayableDemo.unity`
- 世界存档 Schema：V79，本任务没有升级
- 正式 Cell：3314 × 2176 = 7,211,264，边长 2 km
- 开工行政资料：13 州、105 郡国等价单位、1182 县
- 稳定人口地理记录：1336
- 地点交叉记录：95
- 开工 Core：894；EditMode 源测试属性：1102；PlayMode：55
- 工作区开工时已有大量用户修改；本轮未还原、覆盖或提交这些修改

## 3. 行政数据审计

| 项目 | 数量 | 说明 |
| --- | ---: | --- |
| 州级 Region | 13 | 全部可解析 |
| 郡国等价 Region | 105 | 郡、国、属国、京畿等统一为中间层 |
| 县级 Region | 1182 | 全部目录和父级可解析 |
| 总 Region | 1300 | RegionId 唯一 |
| 已进入 Cell 栅格的 Region | 1273 | 由 `admin.bin` 实际使用代码决定 |
| 无 Cell 几何的 Region | 27 | 明确保留为 unresolved，未伪造边界 |
| 运行时 approximate | 1300 | `admin.bin` 只作为近似玩法几何 |
| 运行时 provisional | 1300 | 全部显式暂定 |
| verified | 0 | 不冒充考证完成 |
| 源 stable geography = provisional | 119 | 105 中间层 + 14 县 |
| 源 stable geography = none | 1181 | 包括 13 州与 1168 县 |
| 开局名称时期记录 | 1287 | 现有时间线覆盖 105 + 1182 |

派生文件为
`Assets/StreamingAssets/WorldMap/HanWorldV1/metadata/administrative_regions_v1.json`，
生成入口为 `Tools/Build-HanAdministrativeGeographyRuntime.ps1`。它只整合现有
CSV、`admin_catalog.json` 和 `administrative_timeline.json`，不新增历史判断。

## 4. Cell 行政权威与边界

`WorldMapDataReader.ReadAdministrativeCodes` 直接读取现有 `admin.bin` 的三个
`ushort` 通道。`HanAdministrativeGeographySource` 将代码解析为唯一的：

```text
Cell
→ CountyRegionId
→ CommanderyEquivalentRegionId
→ ProvinceRegionId
```

任何半映射 Cell、目录越界代码或父级链不一致都会显式失败。行政身份与
Owner、Controller、势力控制、占领和产权完全分离。

边界不是 Scene 手描线。`AdministrativeBoundaryTopologyBuilder` 逐行读取 Cell，
只比较东边和南边的共享边；县、郡国、州差异可同时落在同一边上。Renderer 按
州 > 郡国 > 县选择最高视觉层，避免重复叠线。结果按正式 64×64 存储 Chunk
分组并在控制器生命周期内缓存，缩放/平移不会重新生成行政事实。

正式全图实测：

- 已映射 Cell：4,647,051
- 唯一行政边界段：105,116
- 州差异边：21,097
- 郡国差异边：93,688
- 县差异边：105,116
- 非空边界 Chunk：650
- 确定性摘要：`1944afa3c0901f33017234b296863cb83c107d86b58f2a46cbdfc282c89c05c6`
- Core/Mono 全图构建：5,931 ms（最终目标测试单次实测）
- Unity 图形 PlayMode 全图构建：3,379.866 ms

## 5. 玩家地图与 County Picking

正式 `SimulationDashboard`、`PlayableFormalWorldMapController` 和默认洛阳
`PlayableLuoyangGameController` 共同复用同一个
`HanWorldNaturalMapController`。默认玩家从洛阳按 `M 天下` 即进入本系统，不再
只有旧商旅面板能够看到行政交互：

- 天下远景：州界、州名；
- 中景：郡国界、郡国名；
- 近景：县界、县名；
- 县域规划：当前县高亮、当前县与邻县标签、连续地形/道路/水系；
- 标签采用确定性优先级、80 标签上限和矩形碰撞避让；
- 左键从屏幕点投射到地形/统一地平面，再由 Global Cell 解码行政区；
- 县级选择点击县域内部即可，不要求点击边界线；
- 信息面板显示冻结名称、层级、父级、RegionType、精度、Cell 数、边界数、
  公开道路格和公开主要聚落；未知控制情报不会泄露。

## 6. 县域规划 ViewMode

`AdministrativeMapViewState` 与 `WorldState` 分离，保存选择、规划县、标签层和
相机范围。进入县域规划使用县 Cell 包围盒加邻县上下文拟合相机；退出回到统一
天下阅读。滚轮可以继续缩放到聚落、城区、街区和 Facility 表现尺度，中间不会
加载 City Scene 或生成第二套世界。

核心测试在进入/退出规划前后比较完整 `WorldSnapshotSerializer` 结果；快照完全
一致，因此 WorldTime、Player、Location、人口、Facility、Inventory、Market 和
确定性世界状态均未变化。正式 PlayMode 也从 `PlayableDemo` 自动创建的洛阳世界
进入 `M 天下`，并通过同样的完整快照断言；测试不再额外创建第二个商旅世界。

## 7. 名称冻结

`HistoricalDisplayNameResolver` 按剧本开局年份从现有时期记录解析显示名，
`FrozenWorldDisplayNameCatalog` 在地图世界建立时冻结结果。稳定引用始终使用
RegionId；显示名不参与查询。缺少对应时期记录时使用正式 fallback。当前
PlayableDemo 以 184 年初始化；初始化入口已允许传入其他开局年份。

现有 `administrative_timeline.json` V1 对每个行政 ID 只有一段 135—260 名称，
因此代码用合成双时期数据验证“早/晚开局名称不同”，但没有凭记忆补造项目尚未
整理的秣陵/建业等时期记录。

## 8. 验证结果

| 阶段 | 结果 | 证据 |
| --- | --- | --- |
| 全工程编译 | 通过 | `tmp/skill-verification/compile-20260902-111408-576.out.log` |
| 新增 Core 逻辑 | 5/5 通过 | `tmp/skill-verification/core-tests-20260902-111436-896.out.log` |
| 适用 Core 全量 | 899/899 通过 | `tmp/core-test-groups/admin-boundary-v1-20260902/aggregate.json` |
| Unity Engine Smoke | 通过，64.193 秒 | `tmp/skill-verification/unity-EngineSmoke-20260902-104439-982.summary.json` |
| Unity Project Load | 通过，12.197 秒 | `tmp/skill-verification/unity-ProjectLoadSmoke-20260902-112217-056.summary.json` |
| Unity EditMode | 5/5 通过，28.268 秒 | `tmp/skill-verification/unity-EditMode-20260902-111504-321.summary.json` |
| Unity PlayMode | 1/1 通过，88.262 秒 | `tmp/skill-verification/unity-PlayMode-20260902-112455-713.summary.json` |
| task-scope diff check | 通过 | 全仓检查仍被 4 个任务外 P0Final FBX `.meta` 尾随空格阻塞 |

新增 PlayMode 已从正式 `PlayableDemo` 默认洛阳入口验证世界不变、洛阳、涿县与
广宗、LOD、连续缩放，并生成：

`Docs/Evidence/WorldAdministrativeBoundariesAndCountyPlanningV1/`

- `01_world_province_boundaries.png`（1280×720）
- `02_world_commandery_boundaries.png`（1280×720）
- `03_world_county_boundaries.png`（1280×720）
- `04_county_selected.png`（1280×720，涿县）
- `05_county_planning_overview.png`（1920×1080，雒阳）
- `06_county_planning_neighbor_context.png`（1920×1080，广宗）
- `administrative_boundary_performance_v1.json`

这些文件由 Unity 图形 PlayMode 的正式 Main Camera 显式渲染；测试同时采样
非黑场景像素，避免空 BackBuffer 产生的黑图被误判为通过。六图已人工复核：
州/郡国/县线级差异可见，涿县整体高亮，洛阳和广宗县域图保留邻县、地形与水系。

性能基线为：World Map 观测 576.502 FPS，Boundary Build 3,289.425 ms，县域
规划进入 648.625 ms，缓存 36,966,400 bytes，渲染构建 34.45 ms，GC delta
798,720 bytes，5 个边界对象 / 4 个当前渲染 Chunk / 2,023 条当前边界段；权威
拓扑仍保留全部 105,116 条唯一边界段。

Unity 环境排障也已收口：受限 Codex 沙箱内四次均停在首行日志之前；同一安全
脚本在沙箱外 EngineSmoke、Project Load 和目标测试全部成功。许可证日志显示
entitlement 正常，因此后续 Unity 验收应继续通过 `Tools/Run-UnityTestsSafe.ps1`
受控执行，并在需要启动 Unity 原生进程时使用沙箱外权限。

## 9. 历史精度与已知限制

1. 这些边界是正式玩法 Cell 归属，但不是经过逐县考证的东汉行政多边形。
2. 27 个目录 Region 尚无 Cell 几何；仍可解析 ID 与父级，但不能选择或画界。
3. 现有时期名称资料没有多时期更名段，代码能力已就绪，内容需后续史料任务补充。
4. 当前县域规划只有地图空间、选择与相机合同；没有道路拖建、分区、建筑蓝图、
   劳力/材料/资金/工期结算。
5. 人物近景、最终城市美术、正式市场 UI 与 Facility 全功能不属于本任务。

## 10. 下一阶段

本任务自动验收已通过。下一正式任务为：

`洛阳县域空间恢复与规划基底 V1`

它应在同一雒阳县 Region 内整理 UrbanArea、城墙/城门、宫区、城郊、村庄、农田、
道路、水系、产业与 Facility 空间布局。之后才进入“县域规划建设工具 V1”。
