# 任务书：洛阳县域可视化建设交互重构 V1

## 1. 任务目标

把 `PlayableDemo → M 天下 → C 县域 → 洛阳｜县域规划` 从工程验证面板改造成可直接操作的
县域建设规划界面。建设规划继续读取同一份洛阳 512km²、320×640、204,800 个 50m
PlanningCell 与 2,084 项正式 Facility，不建立第二套城市地图。

任务状态只能在自动验收完成后写为
`IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`；最终 `ACCEPTED`
必须由用户在 Unity Game View 中明确确认。

## 2. 本阶段边界

- 只创建非持久化规划草案，不创建或销毁正式 Facility。
- 不扣除钱粮、材料、劳力，不推进日期，不执行施工或审批。
- 不改变人物、人口、家户、产权、库存、市场、道路、水系、城防、导航和行政归属。
- World Schema 保持 V79；旧存档协议不迁移。
- 50m Cell 使用紧凑数组、纹理和空间索引；禁止一格一个 GameObject。
- 规划关注度只影响显示与输入精度，不改变世界事实。

## 3. 玩家界面

### 3.1 地图与图层

- 地图顶部显示行政边界、道路、河流、格网图例与独立开关。
- 地形分析图层可切换；正式道路、水系和行政边界继续来自既有权威布局。
- 行政边界按天下远景、郡国中景、县域近景分层；县域建设时避免高亮线压过道路。
- 右侧信息区显示选中 Cell、建筑/草案与校验原因。

### 3.2 底部建设栏

底部建设栏按道路、建筑、区域、城防、水利、工具分类。任一时刻只有一个主工具激活，
建筑卡显示名称、类型、占地、用途和建造权限。

### 3.3 输入合同

- 左键：放置、拖拽、框选或执行当前工具。
- `R`：旋转建筑；落位后保持建筑工具，支持连续规划。
- 右键：逐层取消当前拖拽/编辑/工具，不删除已完成草案。
- 中键拖拽：平移县域规划窗。
- `Alt + 右键`：旋转县域规划视图。
- 滚轮：缩放。
- `Ctrl+Z / Ctrl+Y`：撤销/重做；保留旧 `Z / X` 兼容。
- `Delete`：删除选中草案，禁止直接删除正式 Facility。

## 4. 草案类型与规则

### 4.1 建筑

建筑 Ghost 随鼠标更新，显示真实 Footprint、朝向、主入口、道路连接和
Valid/Conditional/Invalid 状态；错误原因显示在鼠标附近。支持连续放置、草案移动、复制、
吸管、单删、框选批删与 Undo/Redo。

### 4.2 道路

左键拖拽生成确定性的四向路径预览。不得穿过正式 Facility；跨水需桥梁，穿墙需城门。
提交只生成 `DraftRoadGeometry`。

### 4.3 城墙

沿 Cell Edge 生成 `DraftFortificationSegment`，不得重复覆盖正式城防边；提交只生成
`DraftFortification`。

### 4.4 水渠

按地形高程检查明显逆坡，并提示水源连接；不得穿过正式 Facility。提交只生成
`DraftCanalGeometry`。

### 4.5 区域

支持住宅、商业、农业、手工业矩形刷区。区域只表达规划意图，不自动生成建筑或修改正式用地。

## 5. 架构与性能

- Domain 保存互斥工具状态、草案值对象、历史快照与稳定 ID。
- Simulation 提供确定性几何、规则校验与空间索引查询。
- Presentation 负责 IMGUI、Ghost、图层、镜头和输入，不持有正式世界事实。
- 预览不得逐帧扫描 2,084 项 Facility、全县道路、全县城防或 204,800 个 Cell。
- 记录 Building Ghost、Placement Validation、Road、Wall、Canal、Zone 的 P50/P95，
  以及 Undo、Redo、Overlay switch 和托管内存变化。

## 6. 验收

按以下顺序：全工程编译、定向 Core、冻结清单全量 Core、Unity Project Load、定向
EditMode、图形化 PlayMode、任务范围 `git diff --check`、差异审阅。

正式证据目录为
`Docs/Evidence/LuoyangCountyVisualConstructionInteractionReworkV1/`，包含任务定义的
18 张真实 Game View 图片；`AutomatedStateProof/` 仅存批处理状态图，二者不得混称。

最终应打开并保留 `PlayableDemo` 的洛阳县域建设规划界面，供用户人工操作确认。
