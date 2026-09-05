# 任务书：洛阳 Golden Block 建筑美术定型与 50m Cell 建设模式 V2

## 一、

`LUOYANG_GOLDEN_BLOCK_BUILDING_ART_AND_50M_BUILD_MODE_V2`

本任务以洛阳县域现有 512km²、320×640 个正式 50m `PlanningCell` 和 2,084 项正式
`Facility` 为唯一空间与设施事实。在 V1 400×400m 黄金街区上完成五类建筑院落的可辨识
美术语言，并使县域建设模式直接复用正式 50m Cell、真实 Footprint、Entrance 和 Placement
Validation。目标是形成可作为后续全洛阳推广基准的 Golden Block，不是复制商业游戏资产，
也不是新增一套 5m/10m 微型格网。

## 不可变边界

- 世界战略层仍使用 2km Cell，县域层仍使用 50m PlanningCell。
- 洛阳县域面积、320×640 格、204,800 个 Cell 和 2,084 项 Facility 不变。
- 不新增、删除或移动正式 Facility、Road、Water、Fortification。
- 不改变人口、永久人物、家户、库存、生产、市场、所有权、控制权、日期或行政归属。
- `Facility != Cell`；Cell 是空间，Facility 以真实物理 Footprint 覆盖一个或多个 Cell。
- Entrance 是 Facility 的独立正式属性，不由 Cell 边口替代。
- 本轮 Draft 只存在于规划会话，不落正式世界、不升级存档，World Schema 保持 V79。
- 不复制《三国志11》《城市：天际线》或其他商业游戏的模型、贴图、UI 和地图素材。

## 一、Golden Block 空间合同

- 正式范围：400×400m，即 8×8 个 50m PlanningCell，共 64 格。
- 当前确定性位置：本地行 168—175、列 232—239。
- 16 个表现 Lot 仅用于院落组织和构图，不是 16 个新 Cell，也不是 16 个新 Facility。
- 所有 Lot、巷道、院墙、配房、摊位、树木均标记为 `derived presentation only`。
- 同一布局包、来源 FacilityId 和 profile salt 必须得到相同街区、模块、屋顶变体与签名。

## 二、BuildingPresentationProfile

建立数据驱动的汉代洛阳建筑表现目录。每个 Profile 至少包含：稳定 ID、适用 Definition/Category、
建筑重要度、模块清单、屋顶族和变体集、台基、院墙、门楼、地面处理、道具、树木、密度、
对称度、道路朝向规则、Far/Mid/Near 表现方式和资产尺度。普通扩展必须新增数据定义，不得继续
在渲染器里按类别堆叠大型 `switch`。

五类正式表现族：

1. 住宅院落：主屋、可选东西配房、生活堆物、庭树；夯土地面、较低围墙、家宅门。
2. 市场院落：临街主厅、开放棚、多个摊位；硬化前场、宽入口、低密植被。
3. 工坊院落：低坡作业棚、长棚、材料堆；开放作业场、宽入口、少树。
4. 仓廪院落：多列长仓、装卸堆物；较高台基、正式围墙、装卸前场。
5. 官署院落：抬高正厅、东西厢、门楼、标志物、成对树木；中轴对称、正式庭院。

屋顶必须在轮廓上可辨识，不得只靠换色：住宅双坡、市场棚檐、工坊低坡、仓廪长脊、官署抬高
庑殿/四坡。统一增加可读屋脊、檐口、台基/踏步、院墙缺口和门楼。材质使用共享的暖瓦、深瓦、
风化瓦、夯土、木构和石/硬化地面组，保持批处理预算。

## 三、50m 建设模式

- 普通县域与 Golden Block Mid 默认隐藏格网。
- 进入“建设规划”后显示现有正式 50m Grid；不显示 5m/10m 子格。
- Grid 贴合地形并限制在当前局部镜头，使用共享批次，不生成逐 Cell GameObject。
- 明确区分 `Normal`、`Hover`、`Selected`、`Covered` 和真实米制 Footprint 轮廓。
- 底部建筑栏只显示住宅、市场、工坊、仓廪、官署五类玩家面对的候选。
- 卡片展示用途、真实尺寸和道路要求；未有正式经济权威时不得伪造材料、钱粮或工期。
- 军用烽燧保留为权限/回归数据，但不混入本轮五类建筑栏。
- 官署可用于规划表现和验证，不因此授予玩家或 AI 正式建设权限。

## 四、Ghost、验证与 Draft

- Ghost 必须复用对应 `BuildingPresentationProfile` 的同一院落模块，而不是单一透明方盒。
- Ghost 显示真实 Footprint、Covered Cells、主入口和道路接入方向。
- `R` 顺时针旋转，旋转后重算 Footprint、入口和 Placement Validation。
- 合法与非法状态必须可辨，非法原因来自现有 Validator，不新造第二套规则。
- 大型市场 110×80m 用作跨 Cell 证据；小型 Facility 可只占 Cell 的一部分。
- 创建只得到会话 Draft；Undo/Redo 只改变 Draft 栈，正式世界序列化结果必须不变。
- 普通右键取消当前建设动作；中键平移、`Alt+右键`旋转、滚轮缩放沿用现有县域相机合同。

## 五、LOD、批处理与性能

- 普通 Golden Block 入口使用 Mid 阅读距离；单院落特写和建设模式使用 Near。
- Far 为聚合轮廓，Mid 可读院落，Near 展开模块、地面和小品。
- Golden Block 使用有限共享材质和合批 Mesh，不为模块增加独立 Update、Animator 或 Collider。
- 目标 Renderer/Material 预算为 8—12 组；记录模块、道具、树木、三角形、FPS 和 GC 指标。
- 不能以性能优化为由改变世界事实，也不能把未加载县域停止结算。

## 六、验证

按以下顺序执行：

1. 全工程编译。
2. Core：Profile 完整性、稳定变体、8×8/16 Lot 语义、布局 Fingerprint、世界不变。
3. Unity EditMode：Golden Block V2 根、模块/材质/Renderer 预算、Build Mode Grid/Ghost/Draft。
4. Unity PlayMode：`PlayableDemo → C 县域 → Golden Block/建设规划`，验证格网、跨 Cell、
   Ghost、旋转、Draft、Undo/Redo 和 WorldSnapshot 不变。
5. `git diff --check` 与范围审阅。

Unity 证据菜单：

`Mandate → Validation → Capture Luoyang Golden Block Build Mode V2 Evidence And Review`

输出目录：

`Docs/Evidence/LuoyangGoldenBlockBuildingArtAnd50mBuildModeV2`

菜单生成 `02—29` 的 28 张当前实现图，并保持最终画面为 `PlayableDemo → C 县域 → Golden
Block Mid`、Grid/Debug 关闭。`01_golden_block_v1_before.png` 必须来自真实历史 V1 同镜头；若仓库
没有该证据，工具只报告缺失，禁止用 V2 画面伪造 Before。

## 七、交付状态

- 编译和定向 Core 通过，但 Unity 因用户编辑器打开而被安全入口阻塞：
  `IMPLEMENTED_COMPILE_AND_TARGETED_CORE_PASSED_UNITY_BLOCKED_BY_OPEN_EDITOR`。
- Unity 门禁和 29 图完成，尚未获用户确认：
  `IMPLEMENTED_AUTOMATION_PASSED_READY_FOR_USER_REVIEW`。
- 只有用户明确接受实际操作和截图后：`ACCEPTED`。

## 八、非目标

- 全洛阳 2,084 Facility 最终建模、PBR 最终材质、考古级复原、室内和人物导航；
- 正式材料、钱粮、工期、施工队、拆除或 AI/NPC 建设；
- 新的微型 Build Cell、存档迁移或世界经济规则；
- 战争、攻城、建筑受损和修复；
- 宣称达到任何商业游戏最终成片质量。
