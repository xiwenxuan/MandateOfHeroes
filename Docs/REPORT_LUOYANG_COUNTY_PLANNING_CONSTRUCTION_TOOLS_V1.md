# 洛阳县域规划建设工具 V1 实施与验收报告

## 1. 当前结论

状态：

`INPUT_REMEDIATION_IMPLEMENTED_COMPILE_AND_CORE_PASSED_UNITY_RETEST_PENDING`

洛阳县域规划工具已接入正式 `PlayableDemo` 玩家路线。玩家能够在 48×24 个 50m Cell
的局部规划窗中选择五类现有 Facility 候选、点击地块、查看实体占地与入口、四向旋转、
读取落位原因、创建多个草案并 Undo/Redo。草案不进入正式世界账、导航或存档。

该状态不代表用户已验收，不代表正式施工、材料、劳力、产权、审批、AI 建设或存档协议
已经实现。

## 2. 实现内容

### 2.1 数据与领域

- 新增数据驱动 Placement Profile 包，绑定住宅、仓库、工坊、烽燧和大型市集组的
  现有 Facility 定义、模型和 build contract。
- 新增实体 Footprint、旋转入口、道路接入结果、稳定问题码、校验结果和草案会话。
- `CountyPlanningSession` 支持多草案、Undo/Redo 和确定性哈希；没有序列化入口。
- `CountySpatialPartition` 补充只读坡度、buildability 和灌溉访问器，未改变持久结构。

### 2.2 校验与索引

- 中央 `FacilityPlacementValidator` 校验地形、坡度、水、道路占用、城墙、Portal、
  既有 Facility、草案、县界和道路接入。
- 主要入口随建筑四向旋转；道路接入返回稳定状态并生成实际连接点和经过 Cell。
- 2,084 项 Facility 只在初始化时建立 occupancy index；预览查询读取覆盖 Cell 候选。
- 水体、道路、城防、Portal 和草案均建立独立索引；没有逐预览全表扫描。

### 2.3 正式玩家表现

- `PlayableLuoyangGameController` 的洛阳县域入口现在会打开实际建设工具，不再只是
  行政相机状态。
- HUD 提供候选选择、旋转、创建草案、撤销、重做和真实布局验收样例。
- 右侧面板显示 Cell 高程、地形、坡度、用地、水体、四向通行、实体尺寸、覆盖格、
  道路状态、草案数和玩家可理解的原因；玩家界面不显示冲突对象内部 ID。
- 县域以一张纹理绘制，逐 Cell GameObject 为 0。显示窗为 2.4×1.2km，保持完整
  512km² 数据权威不变。
- 战略视角中的休息快捷键被关闭，防止规划时误推进世界日期。
- 规划地图拥有独立导航：中键拖动平移 48×24 Cell 规划窗，右键拖动旋转地图视角，
  `Tab` 只旋转建筑；旋转视图后的左键点选会反算到正确 PlanningCell，地图点选不再
  强制把视窗跳回中心。

## 3. 权威与不变量

| 项目 | 结果 |
|---|---:|
| 正式 Facility | 保持 2,084 |
| World Schema | 保持 V79 |
| 规划层 Facility 新增 | 0 |
| PlanningCell GameObject | 0 |
| 县域底图渲染对象 | 1 张纹理 |
| 草案持久化 | 否 |
| 规划导致日期/人物/库存变化 | 0 |

核心测试在创建草案前后对比正式 World 状态和县域空间哈希；PlayMode 对整个正式世界
JSON 快照逐字比较，结果完全相同。

## 4. 自动验证结果

### 4.1 编译与 Core

- 全工程 MSBuild 编译：通过。
- 定向 Core：10/10 通过。
- 日志：`tmp/skill-verification/core-tests-20260903-110455-054.out.log`。

### 4.2 Unity EditMode

- `Mandate.Tests.LuoyangCountyPlanningToolsV1UnityTests`：1/1 通过。
- 最终复验结果：`tmp/unity-validation/unity-EditMode-20260903-112317-355.xml`。
- 最终复验汇总：`tmp/unity-validation/unity-EditMode-20260903-112317-355.summary.json`。
- 首次沙箱内启动在 45 秒首行日志门禁处返回 125；按既有安全流程在沙箱外重跑后通过，
  没有关闭用户程序。

### 4.3 Unity PlayMode

- `Mandate.Tests.LuoyangCountyPlanningToolsV1PlayModeTests`：1/1 通过。
- 正式路线：`PlayableDemo → DirectGame → 天下 → 洛阳县域规划`。
- 结果：`tmp/unity-validation/unity-PlayMode-20260903-111805-122.xml`。
- 汇总：`tmp/unity-validation/unity-PlayMode-20260903-111805-122.summary.json`。
- 早期两次失败分别来自过严的异步表现对象计数断言和 Editor batch 不生成
  `ScreenCapture` 文件；两者均未暴露领域或规划规则故障。最终证据改由同一正式规划
  状态直接无头合成，最终 PlayMode 已通过。

### 4.4 差异检查

- 本任务新增/修改文件的 `git diff --check`：通过。
- 全工作区 `git diff --check`：仍被本任务开始前已有的四个
  `Assets/ArtSource/Han/Luoyang/P0Final/*.fbx.meta` 尾随空格阻挡；本任务没有改动或清理
  这些用户工作区文件。

## 5. 性能记录

正式 PlayMode 64 样本记录位于
`Docs/Evidence/LuoyangCountyPlanningToolsV1/planning_performance_v1.json`：

| 指标 | 结果 |
|---|---:|
| 首次进入规划 | 3649.701 ms |
| Cell Pick P50/P95 | 0 / 0 ms（低于计时分辨率） |
| Validator P50/P95 | 0.009 / 0.011 ms |
| 创建两个草案 | 2.155 ms |
| Undo | 0.370 ms |
| 初始化至验收阶段 GC delta | 20,172,800 bytes |
| 最后一次 Facility 候选 | 1 / 2,084 |
| 最后一次道路候选 | 47 |

首次进入包含读取现有洛阳地图/模型数据、建立全部空间索引和生成底图纹理；后续预览
只在 Cell、旋转或建筑类型变化时执行。

## 6. 图形证据

证据目录：`Docs/Evidence/LuoyangCountyPlanningToolsV1/`。

1. `01_luoyang_planning_mode.png`
2. `02_cell_selection.png`
3. `03_residential_valid_preview.png`
4. `04_large_facility_multicell_preview.png`
5. `05_existing_facility_collision.png`
6. `06_water_blocking.png`
7. `07_fortification_blocking.png`
8. `08_road_access_valid.png`
9. `09_road_access_invalid.png`
10. `10_draft_blueprints.png`
11. `11_undo_blueprint.png`
12. `12_arrow_tower_preview_near_wall.png`

自动图是从正式规划状态、正式 640×320 县域纹理、Footprint、入口、道路结果和草案
直接生成的无头证据，不是假 TestScene。完整中文交互面板由最终 Unity Game View 人工
验收。

## 7. 人工验收说明

最终应停留在 `PlayableDemo` 的 `洛阳｜县域规划`，建设工具已打开、普通住处已选择、
地图上显示绿色有效 Footprint。用户可自行测试旋转、阻挡样例、两个草案和 Undo。
只有用户明确确认后，任务状态才可更新为 `ACCEPTED`。

最终人工验收实例已启动并保持运行：Unity PID `48052`；启动日志：
`tmp/unity-validation/unity-LuoyangCountyPlanning-FinalReview-20260903-112613-405.log`。
日志已记录 `LUOYANG_COUNTY_PLANNING_FINAL_REVIEW_READY`。

### 7.1 输入缺陷修复复验

用户人工验收发现县域规划地图无法平移或旋转。根因是规划模式在
`UpdateCameraControls()` 中提前返回，且规划纹理没有独立拖拽输入。现已补齐中键平移、
右键地图旋转、旋转点选反算和对应 PlayMode 回归断言。修复后全工程编译通过，定向
Core 10/10 通过；当前 Unity 实例占用项目且热重载后运行时地图引用失效，因此尚未把
本次补丁标记为 Unity 自动复验通过。重新启动受控 PlayMode 验证后才能恢复
`IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`。

## 8. 后续门禁

下一阶段若进入正式施工，必须另立建设事务与存档兼容任务，处理用地权、权限、材料、
劳力、工期、施工阶段、取消/退款、正式 Facility 创建、AI 建设和顺序迁移。本任务没有
暗中预先实现这些行为。
