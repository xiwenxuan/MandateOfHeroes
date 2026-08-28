# 洛阳可玩纵向切片、统一地图表现与建设素材库 V1 执行报告

状态：`LUOYANG_GOLDEN_SLICE_V1_PLAYABLE_WITH_PROCEDURAL_ART_LIMITS`

## 结果

洛阳验证场景默认不再显示Cell色块，而是进入原创汉代连续城市背景上的Golden Slice。玩家可切换世界/区域/城市/街区四级LOD，点击真实Facility、PermanentPerson、Shipment，查看真实Crop阶段，并在建设模式选择住宅、仓库、工坊和市场蓝图，使用现有产权、建材、4名永久人物劳工、资金、时间和Facility完成逻辑。

四类玩家蓝图现已逐类完成端到端验证。缺料时从既有洛阳 T4 供应商建立真实 SupplyOrder/Shipment，计算随行消耗、自然损耗和风险损耗，到货进入建设者自有库存后才允许 Blueprint 开工；AI 同样通过 Blueprint 下单，并在材料全部到货后续接 Construction，不直接生成 Facility。历史旧玩家命令保留原城市公共现货兼容合同，新 Blueprint 使用严格所有者库存合同。

## 架构

- Simulation Cell仍是空间权威；VisualAnchor仅提供Cell内部表现坐标，不创建SubCell。
- Facility可以对应多个模块、庭院、道路连接和活动锚点，但Simulation仍是一项Facility事实。
- FacilityDefinition、BuildBlueprint、FacilityVisualProfile和具体模块资产四层分离。
- 历史初始化、AI建设和玩家建设复用同一`StartFromBlueprint`入口及`Luoyang184PropertyConstructionRuntimeSystem`。
- 普通住宅、市场、仓库、工坊可三用；历史南宫Composition禁止普通玩家复制，但宫殿通用模块可复用。
- 2,084项开局Facility通过Definition分类稳定取得VisualProfile；未知类型进入公共设施通用Profile，不成为无绑定Scene摆件。
- Person Actor、Shipment车辆和Crop标记均为临时表现；销毁表现不会删除Runtime事实。

## Golden Slice空间链

城门 → 绑定RouteId的主路 → 市场 → 仓储 → 住宅 → 工坊 → 公共设施/农田。Golden Slice从当前Runtime稳定选择代表Facility，没有虚构第二套洛阳街区数据。

## 资产来源

`luoyang-golden-slice-v1.png`由OpenAI内置图像生成工具按项目原创提示生成，作为非权威背景；未使用商业三国游戏素材。建筑标记、Spline、Actor、Crop与Build Mode由项目代码程序化生成。详细来源见14号登记表。

## 限制

本轮是程序化/绘制艺术V1，不是最终3D全洛阳。全国DEM Chunk Terrain Mesh、最终汉代Prefab、完整Force近景和全城Addressable Streaming仍为后续工作。背景图不能用于判定Cell、道路、河流、设施或历史位置；这些始终来自Runtime绑定。

## 最终验证

- 全工程编译：通过。
- 全量核心回归：698/698，通过；聚合指纹 `56647EAC3DBC39DAAA186ADFBF39980DD718DC5C656EF2F4E5A5C19D52D0D3AD`。
- Unity EditMode（本阶段精确前缀）：14/14，通过。
- Golden Slice PlayMode：2/2，通过。
- 旧洛阳 T4 场景 PlayMode：1/1，通过。
- Clean Presentation 证据 PlayMode：1/1，通过；输出13张1024×640 PNG。
- `git diff --check`：通过。

视觉证据位于 `outputs/luoyang-playable-v1/screenshots/`。该目录属于本地可再生成验收证据，不是 Simulation 权威，也不替代正式 Runtime、数据表或资产来源登记。

验证过程中曾出现两类已关闭问题：一是过宽/错误命名空间的 Unity 过滤器导致超时或“未执行测试”，随后以精确过滤器重新执行并取得完整 XML；二是批处理 GameView 的 `ScreenCapture` 帧阶段不可用，已改为从同一 Runtime 投影导出无 Debug 面板的 Clean Presentation PNG。失败尝试未计入通过结果。

## 50个核心问题结论

1—4：只有一套世界；没有第二套城市事实；Cell仍是最小空间权威；未引入SubCell。
5—7：VisualAnchor以CellId+局部表现坐标工作；2000m Cell由模块群表现；一项Facility可有多栋视觉建筑。
8—21：已建立HAN_BUILDING_MODULAR_KIT_V1合同、四类可建蓝图、BuildAvailability和VisualProfile；普通住宅/市场/仓库/工坊可由玩家、AI和历史初始化复用；南宫不能普通复制但模块可复用。
22—23：2,084项开局Facility全部可按规则取得正式V1 Profile；最终独特3D Prefab仍Deferred。
24：正常视图已去除Cell棋盘；建设/Debug模式才显示。
25：DEM/GIS权威保留，但正式Chunk Terrain Mesh未完成。
26—27：河流/道路已有连续且带RuntimeBindingId的V1 Spline；最终GIS精细Spline待全城扩展。
28—29：Golden Slice可识别城门、城墙与汉代城市空间；全洛阳A-Tier独特组合待扩展。
30—35：Actor来自真实Person；Crowd有预算；Shipment对应真实Cargo/Route；Crop反映Runtime成熟度并含80%可早收。
36—39：施工有Ghost至Complete阶段；玩家可使用四类蓝图；AI走相同蓝图入口；材料、工人、资金、时间真实消耗。
40—41：Damage/Ruin由Facility状态投影；189/190事件无需第二地图，运行时变化会触发视觉重建。
42—43：区域风格接口已建立，成都/凉州可更换VisualProfile而不更换FacilityDefinition。
44：Golden Slice达到可玩V1，但有程序化艺术限制。
45：表现采用有界Actor/Facility/Shipment/Crop预算，不遍历生成40万GameObject。
46：无未授权资产。
47：Save/Load后视觉由Runtime重新构建。
48—49：Blueprint/Profile/程序化模块是真正建设资产合同；背景氛围、树木和道具属于Presentation Decoration。
50：下一阶段可从Golden Slice扩展全洛阳，但必须先完成DEM Chunk与最终Prefab替换。
