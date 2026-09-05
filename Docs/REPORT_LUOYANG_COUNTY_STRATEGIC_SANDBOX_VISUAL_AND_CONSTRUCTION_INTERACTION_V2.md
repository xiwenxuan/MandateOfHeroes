# 洛阳县域战略沙盘视觉纠错与城市建设交互分层重构 V2 实施报告

## 1. 结论

本轮已完成代码侧 V2 分层重构，当前状态为：

`IMPLEMENTED_COMPILE_AND_TARGETED_CORE_PASSED_RUNTIME_EVIDENCE_READY_FOR_USER_REVIEW`

这不是用户视觉验收通过，也不标记为 `ACCEPTED`。当前正式 Unity Editor 已执行证据菜单并
生成完整 27 图 After 证据和运行时性能快照；独立 Unity Project Load、EditMode、PlayMode
仍因用户编辑器占用项目而未执行，未关闭用户程序。

## 2. 开工快照

- 基线提交：`940c4381da4cbb893c0882fd28e68914397af897`。
- 分支：`codex/m23-p4-quality-artisan-growth`。
- 工作区开工前已有大量其他任务修改，本轮未清理、还原或暂存这些内容。
- Unity：`2022.3.62f3c1`；开工时主编辑器 PID `13396` 已运行。
- World Schema：V79，本轮未升级。
- 县域权威布局：320×640、204,800 个 50m PlanningCell、512km²。
- Facility：2,084；道路节点/边 359/334；水渠节点/边 19/17；城防边 144；Portal 4。
- 布局 SHA-256：`C486AF5CFA75335CCEEF4C0738357CF4DE0A6F24ED8E8A34C76E5EA1F1A63A58`。
- 声明布局指纹：`851858dce31b849166be9dc7e496a9283baf9bc68fc8e25f4a8a14d14ed4a358`。
- V1 Before 图已保存为
  `Docs/Evidence/LuoyangCountyStrategicSandboxVisualAndConstructionInteractionV2/01_v1_far_before.png`。

## 3. 根因与修复

### 3.1 黑色地形

县域行号增加时世界 Z 轴减小，原两个三角形的顶点顺序使法线朝向负 Y。V2 将绕序收口为
`CountyWorldSpacePresentationPlan.AppendUpwardTerrainQuadTriangles`，Renderer 统一调用该规则，
并增加稳定索引测试。Terrain Shader 仍是受光 Lambert，不改成纯 Unlit；只增加 0.18 的低强度
战略图环境补光，防止 Far 级暗部丢失地类信息。

### 3.2 中央灰白矩形

旧 `BuildFacilityAggregates` 虽名为 Aggregate，实际仍为每个 Far Facility 增加两个 Box。
V2 明确分离：

- 158 个现有 Major Facility 作为 Far Landmark，继续通过现有模型解析器和批处理模块显示；
- 898 个普通、非农业、非基础设施 Facility 在 Far 不创建单体 Detail 对象；
- 普通 Facility 按固定 8×8 PlanningCell 分为 606 个确定性街坊 Aggregate；
- 每个 Aggregate 保留完整 FacilityId 清单、主导功能、密度、最高高度、朝向、城乡属性和稳定签名；
- Aggregate 生成有道路留白、院落间距和坡屋顶的微缩建筑肌理，不生成覆盖整个街坊的大盒子；
- Aggregate 再按 64×64 Cell Terrain Chunk 与功能类别合批，支持视锥裁剪并限制 Renderer 数量。

因此 Far 普通 Facility Detail Request 数固定为 0，但 898 项正式 Facility 仍全部可由 Aggregate
反查；没有删除、合并或替换权威 Facility。

### 3.3 Far / Mid / Near

- Far：地形、河流、R0/R1 官道、城墙城门、地标、城区肌理、村落、农田和植被；普通
  Facility 不逐个显示，R0/R1 路宽提高以适应完整县域镜头。
- Mid：只使用既有 `MidFacilities` 的 8×8 代表与 Major，而不再为全部 2,084 项生成代理盒；
  继续保留正式模型批次、道路和可选择对象。
- Near/Planning：保持 24×48 个 50m Cell 的相机窗口（2.4×1.2km 仅是镜头范围），可继续
  平移到县域任意位置；具体 Facility 由空间裁剪展开，局部 Grid 不铺满全县。

### 3.4 建设交互

沿用并复核 V1 已实现的底部六类工具栏、建筑连续放置、道路/墙/水渠拖拽、四类 Zone、
草案选择/移动/复制/吸管/删除和 Undo/Redo。本轮进一步：

- Building Ghost 使用 `FacilityPlacementProfile.ModelId` 和正式模型工厂的同一批处理模块；
  仅模型解析失败时才使用有坡屋顶的显式 fallback。
- 右侧面板增加当前位置正式 Facility 的名称、类别、DefinitionId、占地和“只读”标识。
- 选中正式 Facility 时除 Cell 外增加其 Footprint 高亮。
- 输入合同保持：中键平移，`Alt+右键`旋转镜头，普通右键取消工具，`R` 旋转建筑，滚轮缩放。
- 所有 Building/Road/Wall/Canal/Zone 仍只是非持久化 Draft，不扣钱粮、不推进日期、不新增
  Facility，也不改道路、水系、城防和世界结算。

### 3.5 现场截图复核后的第二轮视觉收口

首轮 After 图暴露了三个不会由自动计数发现的视觉问题：城区镜头被县域级候选 Hull 拉离
洛阳、稀疏城墙锚点未形成连续城廓、模型目录的归一化 Y 轴被再次按物理高度缩放而显著压扁。
第二轮修正后：

- Far/Mid 的城区判定与镜头中心改由正式洛阳主城墙和城门锚点推导，县域边缘供给点不再扩大
  城区 Presentation 范围；
- 主城墙锚点生成只读、非世界实体的连续 Far/Mid 城廓，Near 仍展开正式分段和城门；
- Far 街坊肌理同时作为 Mid/Near 的非交互背景城市肌理，Mid 只叠加高价值正式 Facility，
  不再把全部对象画成技术点阵；
- 模型只对目录归一化 Y 轴做高度补偿，X/Z 仍保持正式 Facility Footprint；
- Near LOD 强制详细模型 LOD0，防止转台用 LOD 阈值在县域镜头中错误剔除建筑。

## 4. 自动化验收

### 4.1 已通过

- 全工程 MSBuild 编译：通过。
- 既有 `CountyWorldSpacePresentation` 定向 Core：2/2 通过。
- 新增 V2 定向 Core：2/2 通过：
  - 898 个普通 Facility 被且仅被一个 Aggregate 覆盖；
  - Landmark 与既有 Far Major 集合一致；
  - Aggregate 稳定签名可复现；
  - Terrain 三角形索引为正向规则；
  - V2 至少保留四类功能肌理。

### 4.2 未完成门禁

- Core 全量：单进程运行超过 300 秒后安全测试子进程已退出，但外层输出读取未返回总结；
  随后按“普通测试 300 秒、已分类慢测试 900 秒”准备了 12 组、960 项的安全分组清单，
  但该入口逐测试启动进程，完成全量所需时间明显不适合作为本轮交付门禁，故在第 1 组中止。
  最后将全量套件作为 `aggregate-suite` 单进程按 900 秒分类门禁复跑：前 178 项均通过、
  没有失败行，随后 `FoodRuntime_FormalWorldIsDeterministicForOneYear` 单项持续运行至门禁。
  到时只终止本轮 Mono PID，当前无 `CoreTestRunner` / Mono 遗留；没有全量总结，因此不能
  声称 Core 全量通过。完整 stdout 保存在
  `tmp/skill-verification/core-tests-full-classified-20260904-113021-864.out.log`。
- Unity Project Load / EditMode / PlayMode：独立批处理门禁未运行。原因是已有 Unity 编辑器占用
  项目，未经授权不得关闭用户程序；当前编辑器内的正式证据菜单已成功完成运行时路由和截图。
- `git diff --check` 全工作区仍被开工前四个 P0Final FBX `.meta` 尾随空格阻挡；本轮文件需
  另做范围检查，不把旧问题算作本轮失败。

## 5. 图形证据入口

新增菜单：

`Mandate/Validation/Capture Luoyang County Strategic Sandbox V2 Evidence And Review`

它从 `PlayableDemo` 正式入口依次执行 Far、Mid、Near、Planning，生成任务书指定的
`01`—`27` 文件、1080p 主证据、720p 干净 Far 图和指标 JSON，最后保持 Unity 在干净
Far 县域视图。2026-09-04 的最新运行已生成完整 27 图；指标记录 2,084 个 Facility、
606 个 Far Aggregate、0 个 Far 普通 Facility Detail、1,162 个 Renderer、约 79 FPS，
并标明 Aggregate 为 `derived_presentation_only=true`。

## 6. 不变量与后续门

- 同一 2km 世界 / 50m 县域空间合同未变。
- 2,084 Facility、人口、人物、家庭、库存、市场、日期、行政归属均未改变。
- World Schema 保持 V79。
- 本轮是 Presentation 与非持久 Draft 交互收口，不是正式 ConstructionProject、施工队、
  材料运输、工期或竣工交易。
- 用户完成视觉审阅前状态保持 `READY_FOR_USER_REVIEW`；只有用户明确接受后才能写 `ACCEPTED`。
