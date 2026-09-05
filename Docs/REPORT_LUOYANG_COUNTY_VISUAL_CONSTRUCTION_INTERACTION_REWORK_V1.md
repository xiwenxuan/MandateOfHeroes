# 洛阳县域可视化建设交互重构 V1 实施与验收报告

## 1. 当前结论

状态：`IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`

正式建设规划界面、统一工具状态、五类草案、图层控制、相机输入、草案编辑与空间索引已实现。
全工程编译、定向 Core、冻结清单全量 Core、Unity Project Load、EditMode、PlayMode 与
可视 Unity 真实 Game View 证据均已通过并完成目检。因此已进入用户审阅阶段，但当前
仍不等于用户 `ACCEPTED`。

## 2. 实现范围

- 地图图例与行政、道路、河流、格网、地形分析开关。
- 底部分类建设栏：道路、建筑、区域、城防、水利、工具。
- 互斥 `PlanningToolState`、建筑 Ghost、真实 Footprint/入口/道路接入与近鼠标原因。
- 建筑连续放置；道路、墙边、水渠拖拽；住宅/商业/农业/手工业分区刷。
- 草案选择、移动、复制、吸管、删除、框选批删、Undo/Redo。
- 中键平移、`Alt+右键` 旋转、滚轮缩放；普通右键保留取消语义。
- 行政边界降低视觉权重，县域模式显示郡国/县边界，正式道路保持主视觉层级。

## 3. 草案与正式世界边界

| 项目 | 结果 |
| --- | ---: |
| World Schema | V79（未改变） |
| 正式 Facility | 2,084（未改变） |
| PlanningCell | 204,800（未改变） |
| 规划 Cell GameObject | 0 |
| 规划导致日期变化 | 0 |
| 规划导致人物/人口/库存变化 | 0 |
| 草案持久化 | 否 |

PlayMode 在进入规划前保存完整正式世界 JSON；完成建筑、道路、墙、水渠、分区、编辑、
Undo/Redo 和相机操作后逐字比较，结果一致。

## 4. 数据与架构

- `CountyPlanningSession` 统一持有 Building/Road/Fortification/Canal/Zone 草案和快照历史。
- `CountyPlanningDraftGeometryService` 使用稳定的四向路径规则，并复用 Facility、道路、水、
  城防和 Draft 索引。
- `LuoyangCountyPlanningPresentationController` 只编排输入、预览和表现；正式世界状态仍在
  Domain/Simulation 权威层。
- 图层切换重建单张 640×320 县域纹理；没有为 Cell 创建对象。

## 5. 自动验证

### 5.1 编译与定向 Core

- 全工程 MSBuild：通过。
- 定向 Core `CountyVisualPlanning_`：4/4 通过。
- 编译日志：`tmp/skill-verification/compile-20260903-205831-447.out.log`。
- 定向 Core 日志：`tmp/skill-verification/core-tests-20260903-205925-639.out.log`。

### 5.2 冻结清单全量 Core

- 清单：953 项，32 组。
- Run ID：`luoyang-visual-construction-v1-final-20260903`。
- 结果：953/953 通过，0 失败。
- 汇总：`tmp/core-test-groups/luoyang-visual-construction-v1-final-20260903/aggregate.json`。

### 5.3 Unity

- Project Load：通过；汇总
  `tmp/unity-validation/unity-ProjectLoadSmoke-20260903-210009-623.summary.json`。
- EditMode：1/1 通过；汇总
  `tmp/unity-validation/unity-EditMode-20260903-210103-279.summary.json`。
- PlayMode：1/1 通过；汇总
  `tmp/unity-validation/unity-PlayMode-20260903-210134-083.summary.json`。
- 首次沙箱内 Project Load 在启动日志前被宿主隔离阻塞；同一安全入口在桌面会话外通过。
- 一次真实截图试验因 batch 模式不推进 `WaitForEndOfFrame` 被本任务主动终止；修正后自动
  PlayMode 恢复通过。batch 模式不伪造真实 Game View，结构化图片已隔离存放。

## 6. 性能

性能原始记录：
`Docs/Evidence/LuoyangCountyVisualConstructionInteractionReworkV1/planning_interaction_performance_v1.json`。

64 样本中，Building Ghost update P50/P95 为 0.026/0.039 ms，Placement Validation
P50/P95 为 0.011/0.014 ms；Road、Wall、Canal、Zone 预览 P95 分别为
0.013/0.095/0.015/0.001 ms。Overlay 重建 9.115 ms；本次证据流程记录托管内存变化
720,896 bytes，规划 Cell GameObject 为 0。批处理模式未限帧，记录的 5601.718 FPS 仅是
自动诊断值，不代表玩家设备帧率。

## 7. 图形证据

- 自动状态证据：
  `Docs/Evidence/LuoyangCountyVisualConstructionInteractionReworkV1/AutomatedStateProof/`（隔离保存，
  不作为真实 Game View 结论）。
- 真实 Game View：根目录 01—18 共 18 张，均为精确 1280×720；另有
  `19_recommended_1920x1080_layout.png`，为精确 1920×1080。
- 已目检图例、底栏、合法/非法建筑 Ghost、连续放置、道路/墙边/水渠拖拽、分区刷、
  草案编辑、Undo/Redo、图层与相机输入。最终推荐画面已恢复北向并停在合法绿色建筑 Ghost，
  不存在旧的错误红色落点。
- 可视 Unity 日志：
  `tmp/unity-validation/visible-luoyang-planning-evidence-final-20260903.log`；启动进程 PID 45936，
  验收交付时保持编辑器打开。

## 8. 人工验收状态

`PENDING_USER_REVIEW`。只有用户查看并明确接受后，才能写成 `ACCEPTED`。

## 9. 下一阶段建议

用户接受交互后，另立“正式建设事务、工期、材料劳力、审批与 V80 存档迁移”任务；不得把
本阶段草案直接静默提升为正式 Facility。
