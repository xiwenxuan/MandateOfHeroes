# 三视角路由纠错、县域命名收口与天下水墨地图视觉原型 V1 实施报告

> 兼容说明（2026-09-03）：用户已明确否决水墨舆图作为普通玩家天下地图的正式方向。
> 本报告只保留已完成的三视角路由、县域命名和历史验收事实；水墨 Profile 现仅作开发
> 对照。普通玩家入口由
> `TASK_HAN_WORLD_COLORED_3D_STRATEGIC_DIORAMA_PROTOTYPE_V1` 接管。

## 1. 当前结论

正式玩家路由已经收口为 `M 天下 / C 县域 / F 人物`。“城市”不再是主视角；
县域内部使用“总览 / 城区 / 建设”三个子视图，共享同一份 50m 县域布局。
天下图新增原创绢本水墨原型、正式官路合批与地点印记，可与当前地图即时切换。

当前工程状态：

`IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`

这表示实现、受控 Unity 自动验收和证据产出均已完成；水墨方向的最终美术选择仍由
用户审图决定，当前不写成 `ACCEPTED`。

## 2. 实现范围

- `LuoyangPlayableViewState` 只保留 Person、County、World；`C` 映射 County。
- `PlayableLuoyangGameController` 将县域目标解析为已选县或人物当前县，三主视角切换
  不移动人物；标题、快捷键、按钮和操作提示统一改名。
- `LuoyangCountyPlanningPresentationController` 在同一个 320×640 布局上提供全县域、
  `UrbanAreaCandidate` 中尺度聚焦和 48×24 建设窗口，三者共享平移、旋转和缩放。
  当前候选区包含触及县界的外围设施锚点，因此城区镜头只在表现层取候选区中心的
  160×320 窗口；不修改正式布局包或 2,084 个 Facility 的位置。
- 旧 `ShowCityView` 与 `ShowPlayableLuoyangCityOverview` 仅保留废弃兼容入口，并重定向
  到县域城区，不再由正式玩家路由调用旧 2 km 抽象城市投影。
- `HanWorldNaturalMapController` 增加运行时引用自修复、玩家当前 Cell 世界聚焦、地点
  标记和正式 `road_edges.json` 官路合批；全国道路仍为单个 Mesh。
- 玩家当前县优先由永久地点 `place.han140.sili.henan.luoyang` 解析为对应行政县；旧
  2 km 临时设施锚点仍可作为兼容回退，但不会再把 `C` 错误导向陕县，也不会移动人物。
- `InkLandscapePrototype` 使用已有 `NaturalTerrainV2` shader 和项目自有程序绢纸纹理，
  由正式地形特征掩码形成墨色坡脊、雾染与纸纹。水系、道路、行政边界和选择色随风格
  切换，但不写回世界。

## 3. 世界与存档边界

- 正式世界仍为 3314×2176、7,211,264 个 2 km Cell。
- 洛阳县域详细包仍为 512 km²、320×640、204,800 个 50m PlanningCell、2,084 个
  Facility。
- 未增加 World、Person、Facility、Inventory、Market 或行政事实；没有推进日期。
- World Schema 保持 V79；水墨参数、相机和当前子视图都是可重建 Presentation 状态。
- 旧 `LuoyangCityViewProjection` 暂留兼容与测试基础，不是正式玩家入口，后续可在独立
  清理任务中移除。

## 4. 验证记录

| 阶段 | 结果 | 证据 |
| --- | --- | --- |
| 全工程编译 | 通过 | `tmp/skill-verification/compile-20260903-135915-180.out.log` |
| 本任务 Core | 3/3 通过 | `tmp/skill-verification/core-tests-20260903-135959-391.out.log` |
| Unity EditMode | 2/2 通过 | `tmp/unity-validation/unity-EditMode-20260903-134611-015.summary.json` |
| Unity PlayMode 正式流程 | 1/1 通过，198.078 秒测试时间 | `tmp/unity-validation/unity-PlayMode-20260903-135225-870.summary.json` |
| Unity PlayMode 旧入口兼容 | 1/1 通过，38.088 秒测试时间 | `tmp/unity-validation/unity-PlayMode-20260903-135622-659.summary.json` |
| 图形证据 | 14/14 已生成并逐张目检 | `Docs/Evidence/MapViewRoutingCountyRenameAndInkWorldMapPrototypeV1/` |
| 性能证据 | 已生成 | `Docs/Evidence/MapViewRoutingCountyRenameAndInkWorldMapPrototypeV1/performance-comparison.json` |
| 差异检查 | 本任务范围通过；全仓被任务外既有差异阻断 | 四个 P0Final `.fbx.meta` 含 Unity 生成的空值尾随空格 |

正式流程同时验证切换前后完整 `WorldSnapshot` 一致、人物 Cell/Facility 不变。两种天下
风格的结构指标相同：4 Draw Calls、5 Material、2 Shader Variant、1 Terrain Mesh、
1 River Mesh、1 Strategic Road Mesh，常驻地形约 6.48 MiB；水墨地形生成 1962.27 ms，
当前风格 1956.11 ms，差异约 0.32%。批处理测试未提供有效 GPU 时间，`deltaTime` 被固定
为 0.1 ms，因此 JSON 中 10,000 FPS 只作环境记录，不作为真实性能结论。

## 5. 用户审图重点

- `M` 是否明显是全国战略舆图，`C` 是否是可规划县域，`F` 是否回到真实人物近景；
- 洛阳城区是否只是完整洛阳县域中的一个聚焦区，而不是第二张独立城市地图；
- 水墨远景、中景、近景的地形、水系、官路、边界和聚落是否同时可读；
- 朱色是否只承担选择、行政重点和印记，不压过地形信息；
- 当前地图与水墨原型之间是否只改变视觉而不改变玩法事实。

## 6. 自动目检结论

暂定为 **B：方向可用，继续作为视觉原型迭代**。

- 优点：远景已经从彩色地形明显切换为绢纸灰墨层次；山脊、河流、官路和朱色行政重点
  在远、中、近三档均存在；M/C/F 与三个县域子视图可以区分。
- 待用户判断：全国 2 km 网格和密集县界在中近景仍抢视觉；聚落印记在自动截图中偏小；
  灰墨地形尚需更自然的留白、墨韵层次和标签排版。
- 建议：若用户接受 B 方向，下一任务只做边界/网格 LOD、聚落题签和墨色层次细化；不再
  改三视角架构、世界数据或县域 50m 规划底座。
