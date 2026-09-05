# 任务书：三视角路由纠错、县域命名收口与天下水墨地图视觉原型 V1

## 1. 目标

在正式 `PlayableDemo` 中冻结三个玩家主视角：

- `M 天下`：统一世界战略地图；
- `C 县域`：当前或玩家选中的县，内部包含“县域总览 / 主要城区 / 建设规划”；
- `F 人物`：玩家人物实际所在地的近景玩法。

“城市”不得继续作为与天下、县域、人物并列的空间层级。城区是县域内部动态
`UrbanArea` 子视图。同步制作一个基于现有正式地形、水系、道路、行政区、地点和
Facility 数据的原创东汉军政舆图 / 绢本水墨天下原型，并保留“当前地图 / 水墨原型”
即时切换。

## 2. 不变量

- 世界仍使用正式 2 km Cell 和同一个 `WorldState`；洛阳县域详细视图复用同一个
  320×640、204,800 个 50m PlanningCell 布局包及 2,084 个 Facility。
- 三个县域子视图只改变相机、LOD、标签密度和高亮，不复制 Facility，不创建第二张
  县图，不改变时间、人物位置、人口、库存、市场、道路、边界或确定性状态。
- `M` 返回玩家当前县附近的天下图；`F` 返回人物真实位置，不通过切换视角传送玩家。
- 未选县时 `C` 使用玩家当前县；已选县时可观察选中县。远程观察只改变信息与表现。
- 旧 `City` 玩家路由必须删除或兼容重定向到“县域 / 主要城区”，不得再进入旧
  2,084 Facility 的 2 km 抽象城市投影。
- 水墨只属于 Presentation；不得修改 Domain、Simulation 权威事实、存档结构或内容
  数据。World Schema 保持 V79。
- 全国渲染继续使用合批和 LOD，禁止一 Cell 一 GameObject。
- Unity 热重载或场景引用失效时，地图操作必须尝试重建；无法重建时显式报错并安全
  取消，禁止 NullReferenceException 循环。

## 3. 玩家交互与命名

- 顶栏、快捷键和标题统一为 `M 天下 / C 县域 / F 人物`。
- 县域导航统一为“县域总览 / 洛阳城区（其他县为主要城区） / 建设规划”。
- 县域总览显示完整 50m 县域空间；城区只聚焦正式布局包的
  `UrbanAreaCandidate`；建设沿用既有规划工具。
- 天下图允许左键选行政区、滚轮缩放、中键平移、右键旋转；显示州—郡国—县边界、
  聚落地点、河流、正式官路和 Facility 信息入口。
- 水墨原型使用绢纸底、墨色地形层次、朱色行政强调和低饱和道路水系。不得复制任何
  商业游戏素材。

## 4. 验收

自动验收顺序：全工程编译、纯 C# 核心测试、Unity EditMode、正式 PlayMode、
`git diff --check` 和差异审阅。正式 PlayMode 必须从 `PlayableDemo` 走通三主视角与
三个县域子视图，验证视角切换前后完整世界快照一致，并生成：

1. `01_current_world_far.png`
2. `02_ink_world_far.png`
3. `03_current_world_mid.png`
4. `04_ink_world_mid.png`
5. `05_current_world_near.png`
6. `06_ink_world_near.png`
7. `07_m_world_view.png`
8. `08_c_county_view.png`
9. `09_county_overview.png`
10. `10_county_luoyang_urban.png`
11. `11_county_planning.png`
12. `12_f_person_view.png`
13. `13_ink_admin_boundaries.png`
14. `14_ink_river_road.png`

性能结果写入 `performance-comparison.json`。自动验收通过后的工程状态只能写为
`IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`；最终美术选择和
`ACCEPTED` 仍由用户决定。
