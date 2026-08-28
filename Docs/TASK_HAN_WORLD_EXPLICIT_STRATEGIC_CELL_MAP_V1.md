# HAN-WORLD-EXPLICIT-STRATEGIC-CELL-MAP-V1

## 任务定位

用户于 2026-08-26 明确接受“类似经典三国题材回合制战略地图的显式战术格体验”方案。本任务只建设净室原创的表现层灰盒：在现有自然地形上直接显示正式 Global Cell 的格面、边线、悬停和选中状态，不复制任何商业游戏的画面、素材、界面、代码或地图数据。

## 已接受的架构决策

- 保留 `hanworld.square-grid.v1`、2000m Cell、3314×2176 与 7,211,264 个稳定 Cell ID。
- 保留既有八邻接、Global Origin、Region 成员关系、地形绑定和存档合同。
- 显式格是 `Mandate.Presentation` 投影；悬停、选中与颜色不进入 Domain、Simulation 或 Persistence。
- 本阶段不建立六邻接六边形、不建立 SubCell、不迁移存档，也不把视觉微地形回写世界事实。
- 若未来必须采用真正六邻接六边形，将另立架构与存档迁移任务，不在本任务中静默更换。

## V1 实施范围

1. 河南尹 24×24 Cell 固定审查窗口。
2. 每格贴地半透明格面、地形贴合格边、悬停青色与选中金色反馈。
3. 576 格合并为格面与格边两个渲染对象，不创建 576 个 GameObject。
4. 河南尹总览、洛阳选中近景、河南山地贴地格三组固定相机。
5. 继续使用 `TryPickGlobalCell` 将 Unity 局部坐标映射回正式 `WorldMapCellId`。
6. EditMode 几何合同测试、PlayMode 场景状态测试与三张 Game View 证据图。

## 不在范围内

- 全国铺开、正式 Golden、美术资产生产、城市建筑、道路移动代价或战斗规则。
- AI 寻路、军团行动范围、攻击范围、建设规则和 ZOC；这些只能在后续任务中读取同一 Global Cell 事实。
- 对旧 Style D V2 截图进行覆盖或重渲染。

## 验收口径

- 运行时显示 24×24、共 576 个正式 Cell，且网格只有两个合批渲染对象。
- 原点格拾取得到相机焦点对应的稳定 `WorldMapCellId`。
- 山地格边读取同一表现层地形高度，不使用固定悬空高度。
- 悬停与选中颜色可区分，退出网格视图不改变世界事实。
- 全工程编译、核心测试、目标 EditMode、目标 PlayMode、`git diff --check` 分别记录；缺少正确 Unity 版本时必须报告环境阻塞，不得写成通过。

## 当前状态

代码与测试已建立，离线几何与语法检查已通过；当前为 `IMPLEMENTED_STATIC_CHECKS_PASSED_UNITY_RUNTIME_BLOCKED`。本机缺少项目指定 Unity 2022.3.62f3c1，现有 Unity 6 又无有效 Editor 许可证，因此不能声称 Unity 编译、EditMode、PlayMode 或新 Game View 截图通过。河南尹 V1 灰盒不等于全国推广授权，也不等于最终美术确认。

## 后续兼容说明

用户随后明确授权 `TASK_HAN_WORLD_NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1.md`。本任务继续保留河南尹精确格基线；全国任务增加 WORLD 32×32 视觉引导格和任意 Cell 精确进入，不改写本任务的 1×1 Cell 合同。
