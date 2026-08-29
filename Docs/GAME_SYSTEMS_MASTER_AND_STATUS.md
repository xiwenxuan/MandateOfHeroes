# 游戏系统总纲、当前状态与生产科研设计

> 2026-08-12：`LUOYANG-PLAYABLE-VERTICAL-SLICE-MAP-PRESENTATION-AND-CONSTRUCTION-ASSET-LIBRARY-V1` 已建立同一洛阳Runtime上的Golden Slice表现投影、正式BuildBlueprint/VisualProfile/VisualAnchor合同、四类玩家/AI/历史初始化复用建设资产、真实Person/Shipment/Crop/Lifecycle视觉绑定及默认可玩场景。当前状态为`LUOYANG_GOLDEN_SLICE_V1_PLAYABLE_WITH_PROCEDURAL_ART_LIMITS`；全国DEM Chunk Terrain、最终3D汉代Prefab、全洛阳A-Tier Composition和完整Force近景仍不得写成已完成。

## Document Governance

## 当前地图审图门禁：全国战略格 LOD V1（2026-08-26）

用户已进一步明确授权全国格子化。当前实施在 WORLD 使用 32×32 Cell、约 64km 的纯视觉 LOD 引导格，以一个合批对象覆盖全部 7,211,264 个既有 Cell；进入任意地区后切换为 1×1 的 2000m 精确格，河南尹审查窗口仍为 24×24、576 格和两个合批对象。32×32 不是新的 Chunk、Region、行政或模拟语义。

正式入口为 `TASK_HAN_WORLD_NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1.md`，河南尹基线见 `TASK_HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1.md`。当前代码状态为 `NATIONWIDE_GRID_IMPLEMENTED_STATIC_CHECKS_PASSED_UNITY_RUNTIME_BLOCKED`；全国表现层已授权，但仍不等于最终 Golden 或全国美术资产量产。下述 Style D V2 文本继续记录其历史交付状态，其中“正常画面不显示格线”的旧门禁已被本次用户决定在战术格视图范围内取代。

## 当前建筑审图门禁：洛阳第一批可建设建筑模型套件 V1（2026-08-26）

用户已接受东汉中原半写实、中低模战略微缩风格，并授权第一批住宅、仓库、工坊、市场、战地医院、
城墙和城门七类模型执行。当前已建立数据驱动程序化模型目录、共享材质、Runtime Binding 与正式
Global Cell 直接摆放接口；`BUILDINGS` 审查视角在洛阳附近七个互异正式 Cell 各摆放一类模型。
预览不是新的 Facility，不改变建设结算、产权、存档或一 Cell 一个基础 Facility 槽规则。

正式入口为 `TASK_LUOYANG_BUILDABLE_FACILITY_MODEL_KIT_V1.md`。当前状态为
`IMPLEMENTED_TARGET_TESTS_PASSED_FORMAL_UNITY2022_VERIFICATION_BLOCKED`：隔离 Unity 6000.5 兼容副本
完成全脚本编译和 11/11 项目标/相关回归，但本机缺少项目锁定的 Unity 2022.3.62f3c1 与脚本要求的
Visual Studio MSBuild，正式版本验证未执行。当前交付仍是可直接摆放的程序化 V1，不得描述为最终
艺术家 FBX、贴图烘焙或正式 LOD 量产完成。

## 当前建筑建设：洛阳设施模型覆盖与A级历史建筑组合 V1（2026-08-26）

用户已授权继续完成洛阳建筑设置。正式入口为
`TASK_LUOYANG_FACILITY_MODEL_COVERAGE_AND_A_TIER_COMPOSITION_V1.md`。本任务保留第一批七项稳定资产，
新增29项程序化模型，使合并目录达到36项；以显式稳定ID数据绑定覆盖正式Urban与Metropolitan组合
世界的2,084项Facility和61种开局Definition，并为运行定义沿用普通仓库的武库保留带历史依据的
Facility实例覆盖。道路、水渠和桥梁属于基础设施表现，不解释为普通院落。

当前实施状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`。2026-08-27 已在项目
锁定的 Unity 2022.3.62f3c1 下完成全工程编译、相关核心测试 4/4、目标 EditMode 5/5 和图形化
PlayMode 1/1；验收中修复了中央官署门楼屋顶超出单 Cell 占地 0.01 的问题，并输出36项模型同屏
Game View。全量核心/Unity 分组回归与用户审图仍是独立门禁。它不修改任何Facility、人口、产权、
建设结算或存档，也不代表最终FBX、贴图、LOD、损毁、全城Streaming或十二城门独立高模已经完成。

## 当前建筑生产：洛阳高频建筑生产模块包 V1（2026-08-27）

覆盖任务之后的正式入口为
`TASK_LUOYANG_PRODUCTION_BUILDING_MODULAR_KIT_AND_HIGH_FREQUENCY_CITY_FABRIC_V1.md`。本任务保持既有
36个稳定Model ID、2,084项Facility和建设权限不变，对住宅、旱田、道路、工坊、园圃、仓库、城墙、
宫墙、客栈驿舍和牧场10类增加独立Production Profile、Asset Variant、入口/放置锚点、八种原创
缓存Mesh与三级LOD。这10类对应开局1,800项Facility，覆盖率约86.4%；宫墙等受限模型不会因进入
生产包而成为普通玩家可建设项。

当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`：全工程编译、相关核心合同
测试1/1、目标EditMode 2/2和图形化PlayMode 1/1已通过，并输出1600×1000实际Game View。全量回归
和最终FBX/贴图仍未完成；A级地标独立轮廓、十二城门/宫门身份化、中频城市肌理和全城LOD2批处理
预算已经由后续专项完成，但这不代表最终平台GPU、Addressables或全城高精资产验收完成。

## 当前建筑生产：洛阳A级历史地标独立轮廓 V1（2026-08-27）

高频生产模块之后的正式入口为
`TASK_LUOYANG_A_TIER_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1.md`。本任务以184年洛阳正式设施
数据为依据，为南宫、北宫、永安宫、太学、明堂、辟雍、灵台、太仓、武库和濯龙园10项历史设施
建立精确Facility ID绑定的独立Asset Variant、Silhouette、历史元数据、锚点和三级LOD；审图入口
直接使用原10个Global Cell，不建立展示专用虚构Cell。三座宫殿不再共用轮廓，明堂与辟雍分别以
方形重台和环水中堂识别。

当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`：全工程编译、相关核心合同
测试1/1、目标EditMode 3/3和图形化PlayMode 1/1已通过，并输出1600×1000实际Game View。新增表现
不移动或创建Facility，不改变建设权限、产权、控制权、结算或存档；宫殿、太仓、武库等仍只允许
政府/军事/历史初始化/事件路径。交付是原创程序化战略轮廓V1，不是最终FBX、贴图烘焙或考古单体
复原。十二城门/宫门身份化、中频城市肌理和全城性能门禁已经由后续专项完成。

## 当前建筑生产：洛阳十二城门与宫门身份化 V1（2026-08-27）

正式入口为 `TASK_LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1.md`。本任务严格绑定洛阳184设施
数据中的十二座大城门和北宫南门、南宫北门两座宫门，明确排除4个 `facility.military.gate`
通用推荐设施。14项分别具有独立Asset Variant、Silhouette、门楼类型、三档LOD和门外/门内通行
锚点，并直接摆放到权威Global Cell。十二城门朝向读取Facility；两座宫门原始`gate_direction`
为空，表现层只由显示名称派生南向/北向，未回写世界事实。

当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`：全工程编译、相关核心合同
测试1/1、目标EditMode 3/3和图形化PlayMode 1/1已通过，并输出1600×1000全城门实际Game View。
门楼预览为战略视距可读性放大，轮廓差异是原创程序化V1，不是最终FBX、考古立面、城门开闭/破坏
动画或完整攻城系统。它不修改Facility、城防状态、建设权限或存档。洛阳中频城市肌理和全城
LOD2批处理性能门禁均已由后续专项完成。

## 当前建筑生产：洛阳中频城市肌理建筑 V1（2026-08-27）

正式入口为 `TASK_LUOYANG_MEDIUM_FREQUENCY_URBAN_FABRIC_V1.md`。本任务依据Urban与Metropolitan
开局数据，为市场/商铺48项、商队院45项、学校39项、地方官署16项和军营10项建立五个差异化
Asset Variant、街面/密度角色、入口/放置锚点和三级LOD，合计158项。与既有高频生产Profile
合并后，生产模型覆盖1,958/2,084项开局Facility，约94.0%；A级地标和城门身份资产不重复计数。

当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`：全工程编译、相关核心
合同1/1、目标EditMode 3/3和图形化PlayMode 1/1已通过，并输出1600×1000的15格代表街坊Game
View。预览Cell只用于Presentation审图，不声称是158项Facility的实际位置；模型升级不增加Facility、
不扩大官署/军营建设权限，不改变结算、产权或存档。水渠、水井、桥梁等仍属基础设施专项。
后续“洛阳建筑全城性能预算与批处理 V1”已经完成目标门禁。

## 当前建筑性能：洛阳建筑全城性能预算与批处理 V1（2026-08-27）

正式入口为 `TASK_LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1.md`。本任务从两份正式
开局Facility数据建立2,084项轻量表现计划，保持2,084个唯一Global Cell和61种Definition；8×8
Global Cell只作为Presentation合批单位，全城共64批。24×24只作为当前审查窗口，最密窗口为
Column 2040—2063、Row 1224—1247，包含549项Facility和9个8×8表现批次，不冻结为最终Streaming
Unit，也不建立新的世界、行政、模拟或存档语义。

当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`：全工程编译、相关核心
合同1/1、目标EditMode 3/3和图形化PlayMode 1/1通过，并输出1600×1000实际Game View和指标JSON。
接入 P0 四件套 LOD2 后的最密窗口1,673个LOD2源模块按空间批次与材质合并为97个
Renderer/Combined Mesh、17,512个顶点，最新本机Editor回归构建22.9509ms，Renderer降幅94.20%，满足≤200 Renderer、
≤250,000顶点、≤3,000ms和
≥85%降幅的冻结预算。切回WORLD后合并对象和Mesh归零。

这些数字只证明Unity 2022当前目标场景的建筑侧LOD2合批预算，不是平台GPU Draw Call、全量回归、
最终高精FBX/贴图、烘焙遮挡或Addressables Streaming完成。后续基础设施生产专项已经完成目标门禁。

## 当前建筑生产：洛阳水渠、水井与桥梁基础设施模型生产化 V1（2026-08-27）

正式入口为 `TASK_LUOYANG_CANAL_WELL_BRIDGE_INFRASTRUCTURE_PRODUCTION_V1.md`。本任务使用Urban与
Metropolitan正式开局数据，为19项水渠、16项水井和2项桥梁建立三个独立Production Profile、
Asset Variant、角色、连接/服务锚点和三级LOD，并把37项全部放回其正式Global Cell审图。19渠＋2桥
按四邻接Cell确定性派生为2条水系、4个端点和17个直线内部节点；16口井保持离散点设施。连接结果
只属于Presentation，不写回Facility或建立新水利/道路事实。

当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`：全工程编译、相关核心
合同1/1、目标EditMode 3/3和图形化PlayMode 1/1通过，已输出37项总览、17格主渠和2桥＋2渠支段
三张1600×1000实际Game View。三级LOD已进入既有全城空间批次＋材质合批路径；生产覆盖由1,958
提升至1,995/2,084。它不修改建设权限、产权、库存、结算、人口或Save Schema，也不代表最终FBX、
贴图、水流动画、碰撞、导航或灌溉/桥梁通行模拟完成。

后续“洛阳低频防御设施生产化 V1”已经完成目标门禁，生产覆盖达到2,023/2,084。

## 当前建筑生产：洛阳低频防御设施生产化 V1（2026-08-27）

正式入口为 `TASK_LUOYANG_LOW_FREQUENCY_DEFENSE_PRODUCTION_V1.md`。本任务使用Urban正式开局数据，
冻结12座历史城门、2座宫门、4座通用军用门、7座坞堡和3座烽燧，共28项正式Facility和28个
唯一Global Cell。14座有名城门继续复用既有身份资产；通用军门、坞堡和烽燧新增3套独立程序化
Profile、Asset Variant、角色、锚点和三级LOD。4座通用军门缺少世界方向，只使用显式
Presentation默认南向，不回写Facility事实。

当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`：全工程编译、相关核心
合同1/1、目标EditMode 3/3和图形化PlayMode 1/1通过，已输出28项总览、7坞堡＋4通用军门和北侧
双烽燧三张1600×1000实际Game View。全城批处理定向回归通过；生产覆盖由1,995提升至
2,023/2,084。它不修改建设权限、产权、库存、结算、人口或Save Schema，也不代表攻城、守军、
城门开闭、损毁维修、烽火传播、最终FBX或考古复原完成。

后续“洛阳资源与农业设施生产化 V1”已经完成目标门禁，生产覆盖达到2,049/2,084。

## 当前建筑生产：洛阳资源与农业设施生产化 V1（2026-08-27）

正式入口为`TASK_LUOYANG_RESOURCE_AND_AGRICULTURE_PRODUCTION_V1.md`。本任务使用Urban与
Metropolitan正式开局数据，冻结9项林场、6项采石场、5项矿场和6项稻田，共26项正式Facility与
26个唯一Global Cell。四类分别使用管理林场/木料堆场、露天阶梯采石、浅层平硐矿场和分埂浅水
稻田轮廓，具有独立Asset Variant、角色、证据边界、锚点和三级LOD。矿场和采石场共享基础Model，
但只按正式Facility/Definition分流；未知绑定不猜测类型。

当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`：全工程编译、相关核心
合同1/1、目标EditMode 3/3和图形PlayMode 1/1通过；受影响的全城批处理EditMode 3/3和图形
PlayMode 1/1回归通过；已输出26项总览、林场/矿场/采石带、南部采石场与六块稻田四张
1600×1000实际Game View。生产覆盖由2,023提升至2,049/2,084。

26项原始历史精度均为`GameplayReconstruction + Approximate + GeneratedForTest + C`且无逐项
`source_ids`，因此只属于当前权威开局位置和汉代中原通用生产形态玩法重建，不是洛阳考古复原。
静态表现不建立资源体、储量、矿脉、采掘/采伐、灌溉、作物生长、库存或产出结算事实。

后续“洛阳剩余低频公共、礼制与医疗设施生产化收口 V1”已经完成目标门禁，生产覆盖达到
2,084/2,084。

## 当前建筑生产：洛阳剩余低频公共、礼制与医疗设施生产化收口 V1（2026-08-27）

正式入口为
`TASK_LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1.md`。本任务冻结最后35项
正式Facility：南宫、北宫、永安宫、太学、明堂、辟雍、灵台、太仓、武库和濯龙园10项继续按精确
Facility ID复用既有A级地标身份资产；9项医馆、6项通用礼制堂、4项公共庭院、4项公共广场和2项
中央官署使用5套新程序化Asset Variant与三级LOD。明堂/辟雍不会与通用礼制堂混淆，庭院与广场
虽然共享基础Model ID，仍按正式Facility/Definition解析为不同视觉变体。

当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`：全工程编译、相关核心
合同1/1、目标EditMode 3/3和图形PlayMode 1/1通过；受影响的全城批处理EditMode 3/3和图形
PlayMode 1/1回归通过；已输出35项总览、9项医馆、8项礼制堂和庭院/广场/中央官署四张
1600×1000实际Game View。生产覆盖由2,049提升至2,084/2,084。

本任务没有新增、移动或改写Facility，不改变建设权限、产权、控制权、医疗/礼制/行政模拟、库存
结算或Save Schema。2,084/2,084只表示现有开局Facility均具有程序化视觉生产或实名身份资产，
不表示最终FBX、考古复原、室内、碰撞、导航、损毁、全量回归或平台性能已经完成。后续全城视觉
验收与最终资产替换优先级清单已进入实施门禁，不再以增加基础覆盖数字为目标。

## 当前建筑审阅：洛阳全城视觉验收与可替换最终资产清单 V1（2026-08-27）

正式入口为
`TASK_LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1.md`。按模型工厂真实解析
顺序审计2,084项开局Facility后，36个基础Model最终落到54个互异实际Asset Variant；防御和最终
收口目录中的9个`REUSE_*`复用声明不是运行时资产，不进入替换槽位。

54项按生产风险冻结为P0实名身份24项、P1高频曝光10项、P2系统可读14项、P3环境支撑6项，分别
影响24、1,800、226和34项Facility。替换时保持Model/Asset/Profile/Facility稳定身份，程序化V1
继续作为回退；任何外部候选必须完成来源和许可证登记后才能进入槽位。

当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`：内容合同、机器清单、
2,084项逐项解析、实例审阅元数据、54项PreviewOnly审阅板和四个固定优先级镜头已经实现；全工程
编译、定向核心1/1、目标EditMode 3/3和图形PlayMode 1/1通过，受影响的全城批处理EditMode 3/3和
图形PlayMode 1/1回归通过，并生成四张1600×1000 Game View。此状态不等于54项最终FBX已制作或
最终美术已经通过；下一阶段受门禁限制为南宫、明堂、广阳门、北宫南门四项P0替换竖切片。

## 当前建筑终模切片：洛阳 P0 四件套 V1（2026-08-27）

正式入口为 `TASK_LUOYANG_P0_FINAL_ASSET_FOUR_PIECE_VERTICAL_SLICE_V1.md`。本轮锁定南宫、明堂、
广阳门、北宫南门四个既有 P0 替换槽位，保留 Facility/Model/Asset/Profile、Global Cell、史料置信度
与原建设权限。每项现有一个项目原创三级 LOD 集成候选、稳定锚点、六材质参数和目标
Resources/FBX 路径；运行时优先校验并加载合规美术 Prefab，缺失时显式使用程序候选。

后续 `TASK_LUOYANG_P0_FOUR_PIECE_NATIVE_PREFAB_ART_DELIVERY_V1.md` 已按冻结 Resources 路径交付
四套实际 Unity 原生 Prefab、六个材质和四个共享网格；其上的
`TASK_LUOYANG_P0_FOUR_PIECE_VISUAL_REFINEMENT_AND_REVIEW_READABILITY_V2.md` 又补强双朝院、三重台、
门扇、短瓮城、双阙、屋脊、台阶与旗杆等远景识别特征，并把近景镜头改为按建筑实际包围盒中心
取景。每项包含三个非空且严格递减的 LOD、全部稳定锚点且无 Collider；V2 四件套合计
LOD Renderer 为 137/37/21。运行时四项均实际加载 Prefab，程序化回退保留但本次未激活。

后续 `TASK_LUOYANG_P0_FOUR_PIECE_MULTI_ANGLE_TURNTABLE_REVIEW_PACK_V1.md` 已在不改模型、材质、LOD、
权威 Cell 或玩法数据的前提下，建立 4 座 × 3 角度、12 个稳定相机 ID 和运行时循环切换控制；四个
前斜视图继续复用 V2 近景，另补后斜与低角视图。当前状态为
`MULTI_ANGLE_REVIEW_PACK_V1_READY_FOR_USER_DECISION_FINAL_APPROVAL_PENDING`：全工程编译、定向核心 1/1、
多角度 EditMode 2/2、13 图图形 PlayMode 1/1、既有 V2 五图 PlayMode 1/1 与 549 Facility 全城批处理
图形 PlayMode 1/1 已通过，共生成一张总览和十二张 1600×1000 多角度近景。完整核心套件上游
300 秒超时事实仍保留，不能记为通过。四项仍无独立 FBX/DCC 源、手绘贴图和用户最终批准，
`FinalArtApproved` 全为 false；下一步只进行用户逐项审图与四件套迭代，未通过前不批量替换其余
50 个槽位。

后续 `TASK_LUOYANG_P0_FOUR_PIECE_REVIEW_DECISION_BOARD_V1.md` 已把十二张已验证近景按建筑整理为
四张 3000×900 前斜/后斜/低角决策板，并生成无时间戳 SHA-256 机器清单。当前审阅状态为
`P0_FOUR_PIECE_REVIEW_DECISION_BOARDS_V1_READY_FOR_USER_DECISION_FINAL_APPROVAL_PENDING`；脚本首次生成、
4 件/12 源/4 板清单核验、五个输出文件重复生成哈希一致和四板人工视觉检查均通过。

用户于 2026-08-27 对整套决策板回复“接受”，按上下文登记为南宫、明堂、广阳门、北宫南门四件
全部接受。正式入口为
`TASK_LUOYANG_P0_FOUR_PIECE_USER_ACCEPTANCE_AND_SOURCE_ARCHIVE_READINESS_V1.md`。该阶段状态为
`LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_UNITY_NATIVE_SOURCE_ARCHIVED_INDEPENDENT_DCC_FBX_REQUIRED_FINAL_ACTIVATION_PENDING`：
静态内容合同已区分用户接受与最终激活，生成器、P0 目录、4 Prefab、6 Material、4 Mesh 及其
`.meta` 共 32 个文件已建立 SHA-256 归档；全工程编译、定向核心 1/1、原生 Prefab EditMode 1/1、
源清单 EditMode 1/1、既有 P0 EditMode 4/4、13 视图图形 PlayMode 1/1 和最密 549 Facility 批处理
图形 PlayMode 1/1 通过。四个冻结 FBX 目标均缺失，且本机没有 Blender、Assimp、FBX 转换器或
Unity FBX Exporter；因此没有伪造源文件，`FinalArtApproved` 仍全为 false。若不明确改变旧门禁，
真实独立 DCC/FBX 到位并完成一致性验证前不执行最终激活，也不批量替换其余 50 个槽位。

后续 `TASK_LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1.md` 已接入 Unity 2022.3
官方发布的 FBX Exporter 4.2.1 与 Autodesk FBX SDK Unity 绑定 4.2.1，按冻结路径生成南宫、明堂、
广阳门、北宫南门四个真实 FBX。Unity 回读已逐件验证 `LOD0/LOD1/LOD2` 渲染器数量、材质、可逆
锚点映射、锚点位置、几何包围盒和零 Collider；最终清单冻结 42 个项目源/`.meta` 文件、2 个工具链
文件和 4 个 FBX 哈希。当前状态为
`LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`，四项静态
`FinalArtApproved=true`。运行时只有真实 Prefab 加载成功时实例批准为真，程序回退实例仍为假。
这关闭四件套战略地图 P0 竖切片，不代表考古复原、手绘/PBR 贴图终稿或其余 50 个槽位获批。

## 当前地图审图门禁：Style D 战略山河 V2（2026-08-16）

当前已建立`presentation.han-world.visual-terrain-detail.v2`：WORLD/REGION/CITY/CLOSE_PREVIEW分别使用1×/2×/4×/8×表现顶点密度，但正式Global Cell仍为2000m、3314×2176、7,211,264个永久空间身份。该实现不建立SubCell、不改变Global Origin、不向Domain或Persistence回写视觉微起伏。

河流已接入自适应采样、限幅Miter/Bevel、统一水面/河岸横断面与地形采样；森林已接入WORLD地表密度、REGION合并树冠簇、CITY合并单树网格。15张核心Game View截图与15份工作簿已形成用户审图包。

当前状态为`STYLE_D_STRATEGIC_LANDSCAPE_V2_READY_FOR_USER_REVIEW`，而非最终Golden。河流源线段端点接缝、汇流junction mesh、CITY低频块状感和连续LOD morph仍为`PARTIAL`。中华三国志候选源码因GitHub 443网络阻断未取得，许可证仍为`UNRESOLVED`，本轮没有复制外部代码或资产。用户确认前禁止全国Style D生产、河南尹正式高精地形和洛阳城市建筑生产。

- Purpose：统一报告各系统当前状态、跨系统关系、技术债与全局建设顺序。
- Authority：L2 CURRENT SYSTEM STATUS。
- Covers：已有原型/底座/已定方案/待研究的当前判定。
- DoesNotCover：替代各领域L1规范或把设计目标证明为实现。
- Supersedes：早期Development Plan的当前顺序职责。
- SupersededBy：无。
- RelatedCanonicalDocs：`KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md`及Domain Map列出的L1。
- Status：CURRENT。

## 0. 总索引与阅读顺序

本文是当前游戏系统思路的主索引，同时保留生产、建设与科研的正式专项规则。
文档中的“已有原型”“已有底座”“已定方案”和“待研究”必须严格区分，
不能把设计目标描述为已经完成的功能。

### 0.1 核心设计文档

| 文档 | 定位 |
|---|---|
| [`AI_PROJECT_BRIEF.md`](AI_PROJECT_BRIEF.md) | 新 AI 会话的快速导览、最小资料包和阅读入口；不替代本总纲或领域规则 |
| [`GPT_HANDOFF/README.md`](GPT_HANDOFF/README.md) | 给网页版ChatGPT使用的轻量对接包、上传组合、启动提示词与决策回传模板；不替代权威规则 |
| [`KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md`](KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md) | 项目文档Authority/Status、Canonical Domain Map、决策、冲突、缺口与城市开发Manifest的统一查询入口；不替代各领域L1正文 |
| [`UNIFIED_WORLD_DESIGN_DOCUMENT_INDEX.md`](UNIFIED_WORLD_DESIGN_DOCUMENT_INDEX.md) | 本轮统一世界设计原始资料、正式归并结果与相关权威文档的集中目录 |
| [`GAME_VISION_AND_GAMEPLAY.md`](GAME_VISION_AND_GAMEPLAY.md) | 游戏愿景、身份道路、一局结构、历史参与、家族与结局 |
| [`WORLD_SIMULATION_FOUNDATION.md`](WORLD_SIMULATION_FOUNDATION.md) | 地图、人口、设施、产业、市场、财政、战争后果与世界守恒 |
| [`UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md`](UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md) | Cell、产权、统一设施、组织职位、官军爵、皇室王国、政权与政治AI的跨系统正式设计 |
| [`REPORT_UNIFIED_WORLD_DESIGN_V2_MERGE_AUDIT.md`](REPORT_UNIFIED_WORLD_DESIGN_V2_MERGE_AUDIT.md) | 四份外部设计资料的指纹、主题映射、冲突裁决与未冻结候选；是来源审计，不替代正式设计 |
| [`GAME_SYSTEMS_MASTER_AND_STATUS.md`](GAME_SYSTEMS_MASTER_AND_STATUS.md) | 当前系统总索引，以及生产、建设、科研的统一规则 |
| [`PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md`](PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md) | 数据驱动作物、产品、配方、生产执行、产业经济与职业成长专项设计 |
| [`CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md`](CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md) | 人物禀赋、能力、性格、志向、词条、教育和家族培养 |
| [`UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md`](UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md) | 个人战、军队、装备、兵种、阵法、权限、战役和世界回写 |
| [`TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md`](TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md) | 全员永久身份、分级模拟、关注演出和超大人口压力验证 |
| [`TASK_LUOYANG_POPULATION_STRESS_V1.md`](TASK_LUOYANG_POPULATION_STRESS_V1.md) | 洛阳20K—500K永久人物、固定设施与AI真实扩建、2,000米Cell容量压力证据 |
| [`TASK_LUOYANG_184_URBAN_INITIALIZATION_V1.md`](TASK_LUOYANG_184_URBAN_INITIALIZATION_V1.md) | 184年洛阳27万正式永久人物、家户、设施容量、岗位、家族、军队与运行事件初始化 |
| [`TASK_LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1.md`](TASK_LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1.md) | V69把洛阳25名历史人物、15个既有家族组织、FamilyCenter合同、官职和活动接入同一40万永久人口世界；0新增Person、0新增Facility |
| [`TASK_FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1.md`](TASK_FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1.md) | Clan、Branch、Household、FamilyOrganization、FamilyCenter分离规则，39宗族×13剧本参考及184洛阳7组织审计 |
| [`TASK_HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1.md`](TASK_HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1.md) | 大规模开发前的全国家族空间资料合并、全项目文档治理、Canonical入口与八城开发Manifest任务合同 |
| [`HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/README.md`](HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/README.md) | 133核心聚落、250重点县、39 Clan、15 Branch与13剧本的可查询空间参考入口；未知证据保持UNKNOWN |
| [`HISTORICAL_POPULATION_135_260.md`](HISTORICAL_POPULATION_135_260.md) | 135—260逐年人口、史料锚点、人口缩尺和空间分布方法 |
| [`TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md`](TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md) | 一体化世界地图、有限认知、信息资产、资源创世和全层级委任的设计整合记录 |
| [`TASK_M23_P0_MILITARY_PROCUREMENT_TRANSPORT_AND_ARMORY_RECEIPT.md`](TASK_M23_P0_MILITARY_PROCUREMENT_TRANSPORT_AND_ARMORY_RECEIPT.md) | V14军械产品、组织商队库存、采购付款、真实运输和军械库入库闭环 |
| [`TASK_M23_P1_EQUIPMENT_MANUFACTURING_REPAIR_AND_WORKSHOP.md`](TASK_M23_P1_EQUIPMENT_MANUFACTURING_REPAIR_AND_WORKSHOP.md) | V15组织工坊、军械制造配方、静态仓装运和耗时维修闭环 |
| [`TASK_M23_P2_UPSTREAM_RESOURCE_EXTRACTION_AND_PRIMARY_PROCESSING.md`](TASK_M23_P2_UPSTREAM_RESOURCE_EXTRACTION_AND_PRIMARY_PROCESSING.md) | V16资源体、真实人物采集、木炭与块炼铁初级加工闭环 |
| [`TASK_M23_P3_LIVESTOCK_SLAUGHTER_TANNING_AND_HORN.md`](TASK_M23_P3_LIVESTOCK_SLAUGHTER_TANNING_AND_HORN.md) | V17普通牲畜批次、繁育、屠宰、制革与角料副产物闭环 |
| [`TASK_M23_P4_MULTIDIMENSIONAL_QUALITY_AND_ARTISAN_GROWTH.md`](TASK_M23_P4_MULTIDIMENSIONAL_QUALITY_AND_ARTISAN_GROWTH.md) | V18多维品质、工单技艺快照、工匠实践成长与审计流水 |
| [`TASK_M23_P5_MILITARY_LOGISTICS_ACQUISITION_PROVISIONS_AND_LOSS.md`](TASK_M23_P5_MILITARY_LOGISTICS_ACQUISITION_PROVISIONS_AND_LOSS.md) | V19军需取得方式、承运责任、自用补给、运输损耗与货运审计 |
| [`TASK_M23_P6_MULTI_LEG_LOGISTICS_HANDOFF_AND_PARTIAL_RECEIPT.md`](TASK_M23_P6_MULTI_LEG_LOGISTICS_HANDOFF_AND_PARTIAL_RECEIPT.md) | V20持久运输分段、中转保管交接、下段口粮预留与最终分批收货 |
| [`TASK_M23_P7_ESCORT_TRANSIT_RISK_AND_CARGO_SEIZURE.md`](TASK_M23_P7_ESCORT_TRANSIT_RISK_AND_CARGO_SEIZURE.md) | V21真实押运人物、确定性途中风险、截粮归属与敌对损失审计 |
| [`TASK_M23_P8_LOGISTICS_CLASH_INJURY_AND_CARGO_RECOVERY.md`](TASK_M23_P8_LOGISTICS_CLASH_INJURY_AND_CARGO_RECOVERY.md) | V22押运局部交战、真实人物伤病与同路线军队夺回截获物资 |
| [`TASK_M23_P9_MILITARY_LOGISTICS_DELEGATION_AND_EXCEPTION_REPORTING.md`](TASK_M23_P9_MILITARY_LOGISTICS_DELEGATION_AND_EXCEPTION_REPORTING.md) | V23军需目标、承运报价、稳定偏好择优、预算限制与异常上报委任 |
| [`TASK_M23_P10_MILITARY_LOGISTICS_SCHEDULING_OFFER_LIFECYCLE_AND_REPORTING.md`](TASK_M23_P10_MILITARY_LOGISTICS_SCHEDULING_OFFER_LIFECYCLE_AND_REPORTING.md) | V24委任到期调度、报价撤回/过期和在途/完成报告 |
| [`TASK_M23_P11_BOUNDED_HIERARCHICAL_MILITARY_LOGISTICS_DELEGATION.md`](TASK_M23_P11_BOUNDED_HIERARCHICAL_MILITARY_LOGISTICS_DELEGATION.md) | V25同军队父子军需目标、权限/预算继承与自底向上完成 |
| [`TASK_M23_P12_FAILED_SUBGOAL_CANCELLATION_REALLOCATION_AND_REASSIGNMENT.md`](TASK_M23_P12_FAILED_SUBGOAL_CANCELLATION_REALLOCATION_AND_REASSIGNMENT.md) | V26未发运子目标取消、数量预算回收、报价关闭与可审计重派 |
| [`TASK_M23_P13_ACTUAL_RECEIPT_SHORTFALL_AND_SUPPLEMENTAL_FREIGHT.md`](TASK_M23_P13_ACTUAL_RECEIPT_SHORTFALL_AND_SUPPLEMENTAL_FREIGHT.md) | V27按实际到货结算缺口、顺序追加补运、累计预算与旧完成语义迁移 |
| [`TASK_M23_P14_CARRIER_LIABILITY_COMPENSATION_AND_REPLACEMENT_AUTHORIZATION.md`](TASK_M23_P14_CARRIER_LIABILITY_COMPENSATION_AND_REPLACEMENT_AUTHORIZATION.md) | V28承运责任结算、真实赔偿/欠款、净预算恢复与截获货物替代采购授权 |
| [`TASK_M24_P0_ONE_MILLION_FIFTY_YEAR_DEMOGRAPHIC_WORLD.md`](TASK_M24_P0_ONE_MILLION_FIFTY_YEAR_DEMOGRAPHIC_WORLD.md) | 100万永久人物人口世界层从140年连续自然演化50年的任务与边界 |
| [`TASK_M24_P1_MILLION_SUBSISTENCE_LAND_AND_PRESSURE_LOOP.md`](TASK_M24_P1_MILLION_SUBSISTENCE_LAND_AND_PRESSURE_LOOP.md) | 百万家户口粮、县级土地承载、疾病/地方冲突压力与具体人物死亡闭环 |
| [`TASK_M24_P5_FORMAL_PRODUCT_BATCH_AND_INVENTORY_TRANSACTION_BRIDGE.md`](TASK_M24_P5_FORMAL_PRODUCT_BATCH_AND_INVENTORY_TRANSACTION_BRIDGE.md) | 百万紧凑粮种检查点无损转换为M19 V10产品批次/库存事务，并建立正式农业完工单批次入口 |
| [`TASK_M24_P6_MULTI_PRODUCT_FOOD_PROVENANCE_AND_FLOW_LEDGER.md`](TASK_M24_P6_MULTI_PRODUCT_FOOD_PROVENANCE_AND_FLOW_LEDGER.md) | 百万世界多食品来源向量、全路径流转、逐产品守恒及正式批次拆分 |
| [`TASK_M24_P7_MULTI_CROP_FOOD_ECOLOGY_AND_NUTRITION.md`](TASK_M24_P7_MULTI_CROP_FOOD_ECOLOGY_AND_NUTRITION.md) | 百万世界五作物、六食品及产量、轮作、营养、体积、价格、损耗与加工差异 |
| [`TASK_M25_P0_UNIFIED_WORLD_EXECUTION_KERNEL.md`](TASK_M25_P0_UNIFIED_WORLD_EXECUTION_KERNEL.md) | 正式世界稳定阶段调度、到期命令、事务预检/预约/提交和提交后事件底座 |
| [`TASK_M25_P1_FORMAL_HAN_FOOD_CONTENT_AND_INVENTORY_CONTRACT.md`](TASK_M25_P1_FORMAL_HAN_FOOD_CONTENT_AND_INVENTORY_CONTRACT.md) | 正式汉代五作物六食品内容扩展、食品属性合同和家庭粮仓批次消费闭环 |
| [`TASK_M25_P2_LEGACY_FOOD_STOCK_AUTHORITY_AND_FORMALIZATION.md`](TASK_M25_P2_LEGACY_FOOD_STOCK_AUTHORITY_AND_FORMALIZATION.md) | V29旧粮/正式批次库存权威、家庭与村县粮仓守恒转换及公共粮仓容器合同 |
| [`TASK_M25_P3_FORMAL_FOOD_RUNTIME_HARVEST_CONSUMPTION_TAX_AND_RELIEF.md`](TASK_M25_P3_FORMAL_FOOD_RUNTIME_HARVEST_CONSUMPTION_TAX_AND_RELIEF.md) | V29正式食品收获、家庭消费、村县税粮汇缴与双层赈济的批次运行闭环 |
| [`TASK_M25_P10_FORMAL_HOUSEHOLD_FOOD_MONTHLY_COMMAND_AND_SHORTFALL_EVENT.md`](TASK_M25_P10_FORMAL_HOUSEHOLD_FOOD_MONTHLY_COMMAND_AND_SHORTFALL_EVENT.md) | V33按村家庭营养需求、正式批次救济/消费、具体人物缺粮后果与持久缺口事件 |
| [`TASK_M25_P11_FORMAL_PUBLIC_FOOD_TAX_REMITTANCE_RELIEF_COMMAND.md`](TASK_M25_P11_FORMAL_PUBLIC_FOOD_TAX_REMITTANCE_RELIEF_COMMAND.md) | V33按县家庭税粮、村仓留存、县仓汇缴/赈济与持久公共粮食事件 |
| [`TASK_M25_P12_PUBLIC_RELIEF_SHORTFALL_PROCUREMENT_DELEGATION.md`](TASK_M25_P12_PUBLIC_RELIEF_SHORTFALL_PROCUREMENT_DELEGATION.md) | V34县仓赈济缺口、次日官府采购委任、本县真实卖单履约与未履约审计 |
| [`TASK_M25_P13_CROSS_COUNTY_PUBLIC_RELIEF_PROCUREMENT_AND_CIVILIAN_FREIGHT.md`](TASK_M25_P13_CROSS_COUNTY_PUBLIC_RELIEF_PROCUREMENT_AND_CIVILIAN_FREIGHT.md) | V35有限知识跨县采购、政府货物、真实承运与预算履约 |
| [`TASK_M25_P14_PUBLIC_RELIEF_ARRIVAL_RECOVERY_AND_BOUNDED_SUPPLEMENTAL_FREIGHT.md`](TASK_M25_P14_PUBLIC_RELIEF_ARRIVAL_RECOVERY_AND_BOUNDED_SUPPLEMENTAL_FREIGHT.md) | V36实际到仓、按村恢复、异常审计与一次补运闭环 |
| [`TASK_M25_P15_FORMAL_STORAGE_ENVIRONMENT_AND_FOOD_LOSS_AUDIT.md`](TASK_M25_P15_FORMAL_STORAGE_ENVIRONMENT_AND_FOOD_LOSS_AUDIT.md) | V37静态仓储环境、新鲜度、实物损耗与逐批审计 |
| [`TASK_M25_P16_HOUSEHOLD_RELIEF_PICKUP_AND_DELIVERY.md`](TASK_M25_P16_HOUSEHOLD_RELIEF_PICKUP_AND_DELIVERY.md) | V38具体家庭月内领取、真实批次交付与开放请求 |
| [`TASK_M25_P17_HOUSEHOLD_RELIEF_CONSUMPTION_AND_RECOVERY.md`](TASK_M25_P17_HOUSEHOLD_RELIEF_CONSUMPTION_AND_RECOVERY.md) | V39救济粮实际进食、逐人物资格与有界饥饿恢复 |
| [`TASK_M25_P18_INDIVIDUAL_RELIEF_RATION_ALLOCATION_AND_RESERVED_SHARES.md`](TASK_M25_P18_INDIVIDUAL_RELIEF_RATION_ALLOCATION_AND_RESERVED_SHARES.md) | V40逐人救济配额、离队份额保留与可追溯备餐余额 |
| [`TASK_M25_P19_VILLAGE_RELIEF_PRIORITY_AND_AUTHORITY_SNAPSHOT.md`](TASK_M25_P19_VILLAGE_RELIEF_PRIORITY_AND_AUTHORITY_SNAPSHOT.md) | V41村内跨家庭救济优先级、县官/紧急授权快照与旧顺序兼容 |
| [`TASK_M25_P20_HOUSEHOLD_RELIEF_CARE_DELIVERY_AUDIT.md`](TASK_M25_P20_HOUSEHOLD_RELIEF_CARE_DELIVERY_AUDIT.md) | V42儿童、老人和重度虚弱者的真实同户送餐、受助人来源与逐笔照护交付审计 |
| [`TASK_M25_P21_LONG_TERM_NUTRITION_AND_CARE_FEEDBACK.md`](TASK_M25_P21_LONG_TERM_NUTRITION_AND_CARE_FEEDBACK.md) | V43稀疏人物营养档案、追加式营养账、营养性疾病风险与真实救济进食反馈 |
| [`TASK_M25_P22_FORMAL_NUTRITION_MEDICAL_CASE_AND_TREATMENT.md`](TASK_M25_P22_FORMAL_NUTRITION_MEDICAL_CASE_AND_TREATMENT.md) | V44营养性疾病病案、患者/家庭授权、合格医者、真实药材批次与治疗审计 |
| [`TASK_M25_P23_FORMAL_HERBAL_SUPPLY_COLLECTION_PROCESSING_AND_LOCAL_RESTOCK.md`](TASK_M25_P23_FORMAL_HERBAL_SUPPLY_COLLECTION_PROCESSING_AND_LOCAL_RESTOCK.md) | V45野生药草资源、家族采集/晾晒、开放商品县内交易与医者家庭补货 |
| [`TASK_M25_P24_FORMAL_MEDICAL_SERVICE_WORK_FEE_PRESCRIPTION_AND_CASE_CLOSURE.md`](TASK_M25_P24_FORMAL_MEDICAL_SERVICE_WORK_FEE_PRESCRIPTION_AND_CASE_CLOSURE.md) | V46处方实例、医者诊疗工时、家庭诊金、实践成长与病例结案 |
| [`TASK_M25_P25_FORMAL_MILITARY_MEDICINE_TRIAGE_SUPPLY_AND_RECOVERY.md`](TASK_M25_P25_FORMAL_MILITARY_MEDICINE_TRIAGE_SUPPLY_AND_RECOVERY.md) | V47具体军役伤员分诊、军队药库批次、军医工时/成长与归队审计 |
| [`TASK_M25_P26_MILITARY_MEDICINE_PROCUREMENT_AND_LOGISTICS_RECEIPT.md`](TASK_M25_P26_MILITARY_MEDICINE_PROCUREMENT_AND_LOGISTICS_RECEIPT.md) | V48军药采购、真实军需运输损耗与随军药库按实收货 |
| [`TASK_M25_P27_BATTLEFIELD_CASUALTY_EVACUATION_AND_REAR_HANDOFF.md`](TASK_M25_P27_BATTLEFIELD_CASUALTY_EVACUATION_AND_REAR_HANDOFF.md) | V49具体伤员、真实救护队、道路后送与指定医者交接 |
| [`TASK_M25_P28_REAR_MEDICAL_CARE_BEDS_RETURN_AND_REJOIN.md`](TASK_M25_P28_REAR_MEDICAL_CARE_BEDS_RETURN_AND_REJOIN.md) | V50既有后方诊疗点、真实床位/药库、住院治疗、返程与归队 |
| [`TASK_M25_P29_FIELD_HOSPITAL_CONSTRUCTION_MAINTENANCE_AND_STAGED_CARE.md`](TASK_M25_P29_FIELD_HOSPITAL_CONSTRUCTION_MAINTENANCE_AND_STAGED_CARE.md) | V51野战医院正式建设、材料/财政/劳动、周期维护与两阶段诊疗 |
| [`TASK_M25_P30_COMPLEX_INJURY_INFECTION_AND_FROZEN_CARE_PLAN.md`](TASK_M25_P30_COMPLEX_INJURY_INFECTION_AND_FROZEN_CARE_PLAN.md) | V52数据定义伤型、永久伤情记录、感染风险与冻结诊疗计划 |
| [`TASK_M25_P31_TRAUMA_SURGERY_PERMANENT_IMPAIRMENT_AND_MEDICAL_RETIREMENT.md`](TASK_M25_P31_TRAUMA_SURGERY_PERMANENT_IMPAIRMENT_AND_MEDICAL_RETIREMENT.md) | V53数据定义手术、永久伤残、医疗退役与救护队独立返程 |
| [`TASK_M25_P32_CROSS_FACILITY_MEDICAL_TRANSFER_AND_RESPONSIBILITY.md`](TASK_M25_P32_CROSS_FACILITY_MEDICAL_TRANSFER_AND_RESPONSIBILITY.md) | V54治疗前跨设施转运、目标床药预留、真实旅行与主治责任交接 |
| [`TASK_M25_P33_POST_TREATMENT_WOUND_DEATH_FAMILY_INHERITANCE_AND_COMPENSATION.md`](TASK_M25_P33_POST_TREATMENT_WOUND_DEATH_FAMILY_INHERITANCE_AND_COMPENSATION.md) | V55治疗后伤后死亡、永久人物保留、家庭继承和组织抚恤 |
| [`TASK_M25_P34_PRE_RETURN_WOUND_DEATH_AND_MEDICAL_RESPONSIBILITY.md`](TASK_M25_P34_PRE_RETURN_WOUND_DEATH_AND_MEDICAL_RESPONSIBILITY.md) | V56返程前伤后死亡、医疗责任快照、遗体留置与救护队独立返程 |
| [`TASK_M25_P35_INPATIENT_WOUND_DETERIORATION_DEATH_AND_RESOURCE_CLOSURE.md`](TASK_M25_P35_INPATIENT_WOUND_DETERIORATION_DEATH_AND_RESOURCE_CLOSURE.md) | V57住院中恶化死亡、未完成疗程结案、床位释放与转院药材解预留 |
| [`TASK_M25_P36_CROSS_FACILITY_TRANSFER_DEATH_AND_TRANSIT_CLOSURE.md`](TASK_M25_P36_CROSS_FACILITY_TRANSFER_DEATH_AND_TRANSIT_CLOSURE.md) | V58跨设施转运中/待接收死亡、床药取消、在途责任与遗体护送结案 |
| [`TASK_M25_P37_ORIGINAL_EVACUATION_DEATH_AND_CORPSE_ESCORT.md`](TASK_M25_P37_ORIGINAL_EVACUATION_DEATH_AND_CORPSE_ESCORT.md) | V59原始战场后送中/待接收死亡、来源军队责任、遗体护送与无住院救护队返军 |
| [`TASK_M25_P38_PATIENT_RETURN_JOURNEY_DEATH_AND_CORPSE_REJOIN.md`](TASK_M25_P38_PATIENT_RETURN_JOURNEY_DEATH_AND_CORPSE_REJOIN.md) | V60患者返军途中死亡、最后照护责任、遗体随队返军与归队结案 |
| [`TASK_M25_P39_PATIENT_ARRIVAL_WAITING_TEAM_DEATH_AND_REJOIN_CLOSURE.md`](TASK_M25_P39_PATIENT_ARRIVAL_WAITING_TEAM_DEATH_AND_REJOIN_CLOSURE.md) | V61患者已抵军等待救护队期间死亡、队员返程快照、遗体留军与归队结案 |
| [`TASK_M25_P40_EVACUATION_TEAM_RETURN_DEATH_AND_CORPSE_REJOIN.md`](TASK_M25_P40_EVACUATION_TEAM_RETURN_DEATH_AND_CORPSE_REJOIN.md) | V62救护队员返军途中死亡、家庭继承/组织抚恤、遗体沿原旅程归军与返程结案 |
| [`TASK_M25_P41_POST_TREATMENT_FIRST_MEDICAL_TRANSFER.md`](TASK_M25_P41_POST_TREATMENT_FIRST_MEDICAL_TRANSFER.md) | V63部分疗程后首次同组织转院、阶段边界快照、剩余药材预留与前后主治责任分账 |
| [`TASK_M25_P42_REPEATED_SAME_ORGANIZATION_MEDICAL_TRANSFER_CHAIN.md`](TASK_M25_P42_REPEATED_SAME_ORGANIZATION_MEDICAL_TRANSFER_CHAIN.md) | V64最多四段同组织连续转院、逐段床药释放/预留、真实旅程与责任链 |

### 0.2 推荐阅读顺序

```text
核心玩法
→ 本文系统总纲
→ 世界模拟
→ 统一世界、设施、权力与政治AI
→ 人物成长 / 统一战争
→ 永久人口任务书
→ 历史人口资料
```

### 0.3 文档职责与冲突处理

- `GAME_VISION_AND_GAMEPLAY.md` 负责产品愿景、身份玩法和长期内容边界；
- 本文负责统一索引、系统状态、跨系统关系、技术债、全局建设顺序，以及生产、
  建设和科研细则；
- `WORLD_SIMULATION_FOUNDATION.md` 负责世界事实、人口经济、财政、设施和地方战争；
- `UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` 负责 Cell、Facility、组织职位、
  官职/军职/爵位、皇室/王国、政权与政治AI之间的跨系统连接；
- 人物与战争总设计分别负责各自领域细则及内部实现依赖；
- M12负责永久人口迁移、分级模拟、关注系统和压力验收约束；
- 历史人口文档负责史料证据、模型口径和结构化数据任务；
- M1至M16任务书记录阶段目标、设计整合和实现历史，编号不自动表示当前优先级；
- 早期开发方案和预制作清单只保留历史，不再决定当前排期。

发生冲突时：

1. 仓库硬规则以根目录 `AGENTS.md` 为准；
2. 当前系统状态与全局建设顺序以本文为准；
3. 具体领域规则以相应核心设计文档为准；
4. 历史数字和证据等级以历史资料文档为准；
5. 如果冲突会改变存档、架构、玩法方向或数据口径，必须明确列出并请示确认。

状态只能依据证据升级：“已有原型”需要可操作结果，“已有底座”需要代码或最小
闭环，“已定方案”只表示设计确认，“待研究”不能被描述为已支持。

## 0A. 当前统一愿景

游戏是一款以135—260年东汉至三国为重点历史范围的开放式家族人生模拟战略游戏。
玩家作为世界中的具体人物生活，可以选择自建人物、历史人物、时代原住民或穿越者，
并通过职业、家庭、产业、组织和战争参与历史。

统一愿景是：

> 一张真正活着的三国地图，加上一张庞大但可理解的人际与组织网络。

> 每个人从出生起拥有永久独立身份；世界持续结算，只有玩家关注的部分才产生
> 详细信息、关系展开和动画演出。

> 世界只在创世阶段初始化一次。缩放地图、切换专题视图、进入场景和开始关注，
> 都只能读取、聚合或展开已有世界事实，不能生成另一套人口、资源、设施或库存。

> 完整系统不通过删减内容来控制操作量。亲自操作、实时派工、工单、目标指令和
> 组织委任使用同一世界账，玩家可以在任意合法层级连续接管或交还控制。

所有玩法共享同一本世界账：

```text
具体人物与家庭
→ 提供劳力、能力、消费和关系
→ 建设并经营具体设施
→ 生产、运输和消费真实物资
→ 形成税收、财政、军需和组织力量
→ 参与政治、战争和历史事件
→ 后果回到人物、家庭、地区和世界
```

## 0B. 系统状态定义

| 状态 | 含义 |
|---|---|
| 已有原型 | 已经能够在Unity原型中操作并观察结果 |
| 已有底座 | 关键数据或最小闭环存在，但内容、规模和表现不完整 |
| 已定方案 | 设计已经确认，尚未形成正式可玩实现 |
| 待研究 | 方向明确，但数据、性能或实现方案仍需验证 |

状态栏只能使用以上四类；括号可以补充“基础”“待扩展”“已有任务书”等说明，
但不能创造含义不明的第五种完成状态。

## 0C. 当前系统全景

### 0C.1 世界底座

| 系统 | 当前状态 | 核心内容 |
|---|---|---|
| 时间与历法 | 已有底座 | 日、旬、月、季、年和多频率结算 |
| 地图与地点 | 已有原型 | MASTER-MAP-V0 已建立开放物理地理母版、77城、1182县目录、战略节点、路线与7211264个稳定方格Cell；MASTER-MAP-V1进一步发布统一对齐的HanWorldV1、洛阳三档共71897个永久人物与家庭、42类Facility容量母表、1057个推荐档真实Facility Cell、动态城市Footprint、洛阳—虎牢战争走廊和人口/设施Unity专题验证场景。LUOYANG-184-URBAN-INITIALIZATION-V1 已在同一世界上正式物化洛阳连续城市区270000名永久人物、53992户、1230项设施审计、7个家族组织、5支军队与10个运行事件；LUOYANG-184-METROPOLITAN-INITIALIZATION-V1 又以不改写旧包的增量合同新增130000名永久人物、26907户、854项近郊设施、33条聚落—城门路线、135个农业/畜牧单元与5类供应链，使都市圈达到400000人。HAN-135-260全国人口母盘已确认该400K为PASS，并将700K供给区判定为可保留的包含式包络；700K仍未物化，不得解释成400K+700K。M26-P5B另有中山世界节点—城镇空间—建筑切片；全国Facility填充、认知地图和完整层级场景衔接仍未实现 |
| Cell产权与占用 | 已有底座 | HanWorldV1保持2000米Cell；领域规则已限制一Cell一个Owner、一个基础Facility槽和一个独立Force槽。Facility与Force为不同占用槽；产权事务、全国运行时接入和完整UI仍待扩展 |
| 世界创世与资源配置 | 已定方案 | 史料锚点、合理推定、地质生态生成、资源丰度、世界种子和来源审计 |
| 认知地图与专题视图 | 已定方案 | 个人/组织知识、比例尺聚合、建设/资源/战争等视图和信息防泄露 |
| 旅行与交通 | 已有原型 | 道路旅行、行程进度、路线和地点变化 |
| 人口守恒 | 已有底座 | 出生、死亡、迁徙和人口审计 |
| 永久人口 | 已有底座（M14村庄切片、M15-P6、M20-P0、M21-P0/P1/P2/P3/P4/P5/P6、M24-P0—P7） | 200—500人生活闭环、累计1000万冷热档案、5000万全部在世基础调度证据、100万具体人物自然演化及家户粮食/土地/市场/官仓/跨县赈运/具体生产/多食品来源与生态差异50年、V7正式分区人口包、关注热计划、人物仓储访问合同与首批模拟迁移；百万完整游戏负载和未驻留人物移出`WorldState.People`尚未完成 |
| 分级模拟 | 已定方案 | 基础人口、活跃人口、历史留名、重要人物和关注人物 |
| 关注与演出 | 已有底座（M20-P0） | V11持久关注原因账、有效等级合并、局部关系热计划与分区驻留触发已建立；关注仍只影响信息、表现和加载，不决定人物是否存在 |
| 确定性模拟 | 已有底座 | 稳定随机、领域事件、日志和快速模拟 |
| 超大人口存储 | 已有底座与实验（M15-P6、M21-P0/P1/P2/P3/P4/P5/P6、M24-P0—P7） | 正式程序集已有稳定分区、完整代际、增量检查点、清单校验和冷热扩展合同；独立人口层已完成100万开局、50年并同时加载家户消费、固定土地、农业劳力、家庭库存、县级市场、官仓赈济、同郡跨县运输、具体农业工单、多食品生态和压力死亡；首批模拟路径已使用人物访问层；完整运行时外置与百万完整游戏负载仍未完成 |
| MOD与版本迁移 | 已有底座 | 稳定ID、V1至V66顺序迁移；V19—V28军需物流/委任/责任账、V29—V42正式食品/市场/赈济链、V43—V64医疗与伤亡责任链、V65正式商旅商品映射、V66战略委任书与命令提案合同均保持顺序迁移 |

史料人口只作为分布、家庭结构和相对密度的校准参考，实际世界始终按玩家选择的
缩尺比例生成。正式技术目标是单局累计永久人物约5,000万，不再提供按史料总数
一比一生成的“历史人口档”。在压力测试达标前，不得宣称已经支持该累计规模。

### 0C.2 人物与人生

| 系统 | 当前状态 | 核心内容 |
|---|---|---|
| 自建与历史人物开局 | 已有原型 | 姓名、年龄、性别、身份和现有人物选择 |
| 135—260历史人物与宗族母库 | 已有底座（V1，待史料扩充） | 保留P0001—P1202共1202个历史人物稳定身份，形成39个确认Clan、15个Branch、327条亲属、37条婚姻、五类人物时间轴、38条宗族地理存在及13个同源剧本切片；运行时可查询人物/宗族/谱系并派生135—260任意历史时间点。205个地点与64条关系仍在研究队列，未生成FamilyOrganization、Household或家族资产，不得解释为全国人物行年研究已经穷尽 |
| 135—260历史世界开发参考库 | 已有深化资料、首批城市开发包、洛阳门禁与Core接入（V1） | `HISTORICAL_WORLD_REFERENCE`已建立Master→稀疏Timeline/Change Event→Scenario结构，覆盖126年、13州、105郡国、1182县、77战略显示名、1202人物、39 Clan与13个Scenario；深化层去重为133个Canonical核心聚落并筛选250个重点县。首批10个City Development Pack已覆盖洛阳、长安、邺、许昌、成都、襄阳、江陵、建业、合肥与南郑。洛阳184门禁后的V69 Core接入已把25名历史人物精确绑定到现有40万Person，并正式投影2,084 Facility、15个FamilyOrganization、Office和活动；0新增Person、0新增Facility。其余Pack完成仍不等于Unity/Runtime实现。 |
| 135—260县域生产、资源、产业与供应参考 | 已有全国参考母版（V1，待选择区域物质化） | 已覆盖1182县、19类商品、22458条县—商品平衡、13个历史切片、15366条县—切片状态、4471条模型运输边和127条184年供应关系，并发布40份审计工作簿。该母版只用于初始化、校准、AI规划和统计；运行时权威仍为Cell、资源、Facility、Worker、Recipe、Inventory和Transport。当前仅68县有已解析县治点，其余1114县使用明确标注的分析点，不能作为历史位置、县界或古道证据。全国人物、设施和库存新增数均为0。 |
| 135—260历史家族空间与FamilyCenter参考 | 已有参考底座；洛阳V69已有运行时合同 | 冻结Clan/Branch/Household/FamilyOrganization/FamilyCenter分离、Primary/Local/REMOTE状态与22项动作矩阵；形成39 Clan×13 Scenario共507条快照、52条只读初始化候选、40条住宅/庄园/资产证据。洛阳V69已保留15个既有FamilyOrganization、纠正f088/f036共10条污染成员关系，并建立15个Deferred FamilyCenter状态；当前0个Facility具备FamilyManagement能力，0个Active Center。该局部实现不得解释为全国组织或中心已经生成。 |
| 项目知识库与文档治理 | 已有治理底座（V1） | 已建立Authority L0—L4、Status、Canonical Domain Map、文档Registry、重大决策、开放问题、冲突、实现/研究缺口与八个P0城市Development Manifest；新开发必须从`KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md`进入，但Registry分类不替代领域正文，未知和冲突不得静默裁决 |
| 禀赋与专业能力 | 已有底座 | 八项禀赋、十类专业和五维评价 |
| 性格、价值观与志向 | 已有底座（待扩展） | 人生目标、选择倾向和人物差异 |
| 教育与实践 | 已有原型 | 自修、教师、费用、时间和岗位实践 |
| 词条与经历 | 已定方案 | 身体、心性、职业、经历、社会和隐秘词条 |
| 健康与医疗 | 已有底座（M25-P21—P42，待扩展） | 长期营养、营养病案、处方/诊疗服务、真实药材供应、民间收费、军医与军药物流、具体伤员后送、既有医馆/野战医院、复杂伤情/感染、手术—永久伤残—医疗退役、最多四段同组织连续跨设施转运与逐段责任/床药账，以及治疗后/返程前/住院中/跨院转运中/原始后送中/患者或救护队员返军期间伤后死亡、家庭继承、组织抚恤、来源与最后照护责任、未完成疗程与药材预留结案、遗体护送、遗体随队归军或留军和救护队独立返程已接通；跨组织转院、防疫、完整手术器具/护理和医者人生任务待扩展 |
| 家庭 | 已有底座 | 家产、债务、成员、生育、死亡和家主继承 |
| 家学与多代培养 | 已定方案 | 幼年、少年、青年培养和知识传承 |
| 人生世录 | 已定方案 | 重要经历、关系、遗愿和跨代记忆 |

### 0C.3 职业与身份

| 主身份 | 核心玩法 | 当前状态 |
|---|---|---|
| 军人、武将 | 编制、训练、行军、补给、战斗和升迁 | 已有原型（基础） |
| 士人 | 读书、游学、著述、清议、举荐和门生 | 已定方案 |
| 官吏 | 户籍、税收、案件、治安、赈灾和升迁 | 已有原型（任务） |
| 商人 | 供需、跑商、商队、产业、分号和商会 | 已有原型（基础） |
| 侠士 | 游历、委托、武艺、同道、门客和私兵 | 已定方案 |
| 医者 | 药材、诊疗、医案、防疫和军医 | 已有底座（民间与军医首段闭环） |
| 家主、世家 | 族产、教育、婚姻、门客、继承和政治投资 | 已有底座（家庭） |
| 农户 | 土地、具体作物、赋税、灾害、债务和乡里 | 已定方案 |
| 工匠 | 材料、订单、制造、作坊、学徒和工艺 | 已有底座（M23-P4工单实践，待扩展） |
| 宗教人士 | 修行、传道、赈济、信众和官府关系 | 已定方案 |
| 密探 | 掩护、线人、情报、策反、破坏和反间 | 已定方案 |

身份决定玩法入口，不是单纯属性加成。角色可以拥有一个主身份、有限社会身份、
隐藏身份和多个真实组织职位。

### 0C.4 家庭、关系与组织

| 系统 | 当前状态 | 核心内容 |
|---|---|---|
| 家庭与家户 | 已有底座 | 人员、住宅、土地、财富、存粮、债务和继承 |
| 家族组织与FamilyCenter | 已有参考规则与洛阳审计（未接运行时） | FamilyOrganization拥有族产和职位；FamilyCenter需真实Facility、FamilyManagement能力、合法控制、管理者和Primary/Local指定。成员在当地活动不依赖中心；洛阳7个旧组织存在成员映射与无Facility问题，待安全迁移 |
| 多维关系 | 已有底座（M20-P0局部展开，待扩展） | 现有亲密、信任、敬重和利益义务关系可与家庭、村庄、组织共同按需展开；畏惧、怨恨和关系词条待扩展 |
| 血缘、婚姻与师徒 | 已定方案 | 永久结构关系 |
| 统一组织 | 已有底座 | 家族、官府、军队、商会、师门、宗教和情报网 |
| 职位与权限 | 已有原型 | 身份不等于权力，职位决定合法控制范围 |
| 官职、军职与爵位分账 | 已定方案 | 官职给行政权、军职给正常指挥资格、爵位给身份和封邑收益；三者不自动授予产权或彼此权限 |
| 皇室、王国与政权状态 | 已定方案 | 皇室主脉、国家/皇室/个人资产、王府/王国政府、太守/国相、自立、奉汉关系和政治主张已归并，尚无正式可玩闭环 |
| 追随、投靠与起义 | 已定方案 | 人物比较关系、利益、志向、合法性和家庭安全 |
| 关系网络界面 | 已有底座（M20-P0，无正式UI） | 已能从人物确定性、有上限地展开家庭、显式关系、村庄和组织且不制造两两关系；县郡、政权与正式界面待扩展 |

### 0C.5 生产、建设、科研与经济

| 系统 | 当前状态 | 核心内容 |
|---|---|---|
| 地方建设 | 已有原型（最小） | 投资、劳动、进度、完工和真实设施 |
| 统一Facility与成长 | 已有底座（目录与通用成长待实现） | Definition/State、住房、岗位、Owner/Controller和洛阳蓝图已有底座；58项候选目录、Profile复用、Capability/Capacity及参数/模块/内容/改造/空间五类成长为已定方案，未完成正式去重与全国内容注册 |
| 生产建设指挥与托管 | 已定方案 | 亲自劳动、实时派工、工单、目标指令、组织委任和连续接管共用同一世界账 |
| 具体农业 | 已有底座（M17/M17-P0/M19-P0） | 小麦工单、仓储守恒、数据驱动作物/品种/产品/配方/方法、核心JSON、内容清单及种子批次转换合同已验证；收获自动批次化和环境生产待扩展 |
| 资源采集与初级加工 | 已有原型（M23-P2/P3） | 中山铁矿、林地、牧草与鞣料树皮资源体保存来源、规则版本、储量、预留、品位和难度；真实组织人物通过耗时采集取得矿石、木料、牧草和鞣料，并可烧炭、块炼铁；完整资源创世、勘探、多矿脉、事故与再生待扩展 |
| 加工制造 | 已有底座（M19-P0、M23-P1/P2/P3/P4） | 家庭粮仓与组织工坊共用批次预留、耗时、结算和库存流水；已打通粮食加工、木炭/块炼铁、羊群繁育、屠宰、制革、角料与六类军械制造配方；V18加入数据驱动多维品质、开工技艺快照、递减实践成长和实践流水；品质衰减、师承、市场与军队效果和更多产业待扩展 |
| 多年生资产 | 已定方案 | 桑园、果园、林场的成长、成熟和毁坏 |
| 产业链 | 已定方案 | 农田—仓储、桑园—丝织、铁矿—铁器、药圃—医馆 |
| 市场和商旅 | 已有底座（M22-P0、M25-P4—P6） | 正式食品卖单批次预留、买单现金托管、同县撮合与价格账，以及跨县货运需求、真实承运登记、有限知识报价选择、途中损耗、分批收货和独立运费已建立；完整NPC自动下单、经营委任、市场认知和商号网络待扩展 |
| 仓储与物流 | 已有底座（M19-P0、M23-P0/P1/P5—P14、M25-P5/P6/P15，待扩展） | 产品/种子批次、通用库存事务、组织静态仓与人物随行容器、军需货运/委任/责任链、民用跨县买方在途所有权、自用口粮分账、运输损耗、容量受限收货，以及家庭/村/县静态仓的环境防护、新鲜度衰减和逐批损耗审计已建立；多承运人中转、动态改道、包装污染、火灾虫灾事件、并行拆分合并和跨组织递归委任待扩展 |
| 科研科技 | 已有底座（M18-P0） | 开放技能/知识/科技定义、负责人、资金、周期、人物掌握、设施级应用和科研账；完整五领域内容待扩展 |
| 原住民科研 | 已有底座（M18-P0） | 人物持有知识后可在真实设施立项，项目随世界日自主推进；传播、失传和组织所有权待实现 |
| 穿越者知识 | 已定方案 | 现代知识线索缩短探索，但不能凭空制造产业 |

本领域的正式详细规则见本文第1节以后。

### 0C.6 治理、财政与地方社会

| 系统 | 当前状态 | 核心内容 |
|---|---|---|
| 户籍与人口治理 | 已有底座（待重构） | 永久人物、家户、登记人口与实际人口 |
| 税收 | 已有底座（M22-P0） | 家户现金税、村级税粮留成和县级上解已有真实账户与守恒账；州郡上缴待扩展 |
| 三级财政 | 已有底座（M22-P0县级） | 家庭、村庄、县仓和县政府已分账；设施、州郡与国库层级待扩展 |
| 案件与治安 | 已有原型（任务） | 地方案件、巡查、豪族与百姓反应 |
| 赈灾与公共工程 | 已定方案 | 仓储、水利、道路、卫生和灾害应对 |
| 豪族与土地兼并 | 已有底座（M22-P0影响与税收遵从） | 豪族影响、税收遵从和减免已进入县级结果；土地、依附人口、庄园和私兵待扩展 |
| 基层执行链 | 已定方案 | 中央—州郡—县—书吏—里正—家庭 |
| 政治AI与势力演化 | 已定方案 | 自立按意愿、可行性与机会低频评估，忠诚、汉室认同、组织惯性、家族利益和下属独立重评共同作用；现有沙盒AI与委任提案仅是底座 |

### 0C.7 战斗与战争

| 系统 | 当前状态 | 核心内容 |
|---|---|---|
| 真实服役 | 已有原型 | 240名具体人物进入三支原型军队 |
| 军队编制 | 已有原型 | 编制树、主将、基层军官和具体士卒 |
| 军令权限 | 已有原型 | 现场权限、越权审计和指挥责任 |
| 行军与军粮 | 已有原型 | 路线、补给、缺粮和逃亡 |
| 简化战斗 | 已有原型 | 士气、训练、军粮和伤亡 |
| 战场医疗 | 已有底座（M25-P25—P42，待扩展） | 具体伤员分诊、主将授权、军医工时、军药物流、道路后送、后方住院/返程、野战医院建设维护、复杂伤情/感染、手术、永久劳动能力损失、医疗退役、最多四段同组织连续跨设施转运与逐段责任/床药账，以及治疗后/返程前/住院中/跨院转运中/原始后送中/患者或救护队员返军期间死亡、家庭继承、组织抚恤、来源与最后照护责任、床药取消、遗体继续护送或留军、遗体归军和救护队独立返程已闭合；跨组织转院与完整术后护理待扩展 |
| 装备与军械库 | 已有原型（M11、M23-P0/P1） | V15六种数据驱动军械产品、三军库存、真实发放、齐整度、损坏、耗材维修、遗失、缴获、采购入库与守恒审计 |
| 军械采购与运输 | 已有原型（M23-P0/P1/P5/P6/P7/P8/P9/P10/P11/P12/P13/P14） | 有军权人物可按五种方式取得真实军粮批次，区分货源、承运与损失责任；V20加入多段中转和分批收货，V21加入真实押运、途中风险和截粮审计，V22加入承运/押运人物局部伤病及有军权军队同路线单次追击夺回，V23加入军队层级军需目标、报价、承运偏好、预算和异常报告，V24加入日界到期调度、报价撤回/过期、在途周期报告和真实到货完成，V25加入同军队两层父子目标与责任/预算继承，V26加入未发运子目标取消、回收与重派，V27加入按实际到货结算、缺口报告、顺序补运和累计预算审计，V28加入承运责任结算、真实赔偿/欠款、净预算恢复和截获货物替代采购授权；跨组织完整递归委任、保险/司法执行、战术遭遇、长期剿匪和后勤网待扩展 |
| 兵种派生 | 已有原型（M11） | 人物能力、职责和实际装备实时派生刀盾、长矛、弓兵、轻兵、徒手与支持职责，不保存第二份兵种事实 |
| 个人战与局部交战 | 已定方案 | 关注时展开，同军团伤亡预算统一 |
| 阵法、战法与计谋 | 已定方案 | 组织执行、有限情报和具体条件 |
| 战役与战争 | 已定方案 | 战斗—战役—战争的统一层级 |
| 战后世界回写 | 已有底座（待扩展） | 人物、家庭、设施、库存、财政和政治后果 |

### 0C.8 任务、历史与消息

| 系统 | 当前状态 | 核心内容 |
|---|---|---|
| 动态任务 | 已有原型（基础） | 地点、身份、职位和世界需求生成任务 |
| 历史事件 | 已有原型（184年） | 史实条件、变体、阻止和历史偏移 |
| 223年历史主题剧本 | 已定方案 | 第一个正式历史主题剧本；以托孤后的季汉为时代入口，剧情服从世界事实，玩家可以参与、拒绝或远离 |
| 消息与有限认知 | 已定方案 | 世界事实、人物所知和人物所信分离 |
| 信息资产与抄录 | 已定方案 | 记忆、文档、家族/组织档案、权限、抄写劳动、载体、时效和副本 |
| 135—260人口基线 | 已有底座（V1全国连续母盘完成） | 126年全国—13州—105郡国等价单位—1182县双人口口径、事件流量、逐级守恒、13剧本Snapshot与洛阳400K/700K一致性审计；尚未全国Person化 |
| 140年郡国人口 | 已有底座（M13完成） | 105项人口来源、1182项县级目录、稳定地点映射、77城交叉和M12消费文件 |
| 260年后发展 | 已定方案 | 人物、家族、组织和动态历史继续运行 |

### 0C.9 穿越者与MOD

| 系统或剧本 | 当前状态 |
|---|---|
| 穿越者出身、专业、历史记忆与执念 | 已定方案（待细化） |
| 现代知识线索与时代适配 | 已定方案 |
| 身份暴露、价值观冲突与蝴蝶效应 | 已定方案（待细化） |
| 一万大学生入蜀 | 待研究（候选综合MOD） |
| 无限白粥 | 待研究（候选资源与治理MOD） |
| 现代医院 | 待研究（候选医疗与耗材MOD） |
| 丞相再活五年 | 待研究（候选历史事件MOD） |
| 街亭救火、子午谷、麦城之前 | 待研究（候选历史意难平MOD） |
| 历史人物保护计划 | 待研究（候选蝴蝶效应MOD） |

这些内容必须建立在永久人口、生产、物流、科研、组织和历史事件底座之上。当前只保留
设计记录，不进入近期开发排期；应先完成223年第一个正式历史主题剧本及其自由世界验收，
再重新评估穿越者框架和大型意难平MOD的建设时间。

## 0D. 当前主要技术债与差异

### 0D.1 人口架构迁移

当前原型使用统计人口批次与独立人物；正式规则要求全员永久人物，统计对象只能作为
汇总缓存。M15-P6已建立V7共存迁移和正式分区人口包，M21-P0/P1/P2/P3/P4/P5/P6已建立统一人物仓储
访问合同，并迁移旅行、关系、任务、生命家庭、村庄生活、人口台账、农业生产、教育、军医康复与军事核心人物访问。其余模拟系统仍有大量直接访问
`WorldState.People`的代码；后续必须按系统逐步迁移，不能直接把旧批次或内联列表扩大
到数千万人。

### 0D.2 超大存档

当前小世界快照不能直接承担单局累计5,000万永久人物。M15已经比较SQLite、分区二进制
和混合方案，M15-P6把分区二进制正式接入为V7侧车人口包；M21-P0/P1新增稳定的出生新增
与既有人物修改集合，并按受影响分区写回增量检查点。尚未迁移的模拟系统、变更日志压缩、代际清理、
完整游戏负载和累计5,000万最终验收仍未完成。

M24-P0进一步建立独立人口世界层：按M13的105项郡国人口来源和1182县目录生成
1,000,000名具体永久人物，从140年连续自然演化50年后形成1,463,934名在世人物和
2,756,134名累计永久人物，11项身份、亲子、事件与物理重载不变量通过。该证据只覆盖
家庭形成、婚育、死亡、县级分配、事件调度和永久档案增长；生产、消费、战争、迁徙、
疾病、教育、技能及完整NPC AI尚未同时加载，不能称为百万完整游戏世界验收。

M24-P1在同一百万世界上继续接入家户成员年龄口粮、县级固定可耕地、具体农业劳力、
收获/田间损耗/腐败/地方冲突征夺、粮食满足度、生育反馈和具体压力死亡。50年后在世
1,275,407人，累计2,694,097名永久人物，粮食与土地账通过守恒，候选耗时35.1秒、峰值
工作集约780.55 MiB。该实现仍使用县级农业与配给缓存，不是具体作物批次、市场、跨县物流、
完整疾病传播或历史战争，因此仍不能称为百万完整游戏负载。

M24-P2继续拆分家庭粮食所有权、现金、县级年度市场、税粮、官仓赈济和同郡跨县有限运输。
50年后在世1,092,574人，累计2,580,446名永久人物，共形成6,772笔跨县赈运；粮食、现金、
市场双边清算和逐笔运输账通过22项不变量，候选耗时40.9秒、峰值工作集约822.57 MiB。
该工具仍使用抽象粮食折算单位和年度市场撮合，不是逐作物/产品批次、道路寻路、商队合同、
完整财政权限或百万完整NPC AI负载。

M24-P3在同一独立世界层把正式生产内容包投影为具体家庭的自有地/租用地、稳定ID农业绑定、
种子批次摘要、收获产品折算和流式年度农业工作单。100万开局连续运行50年形成5,702,528张
工作单，候选耗时26.0秒、峰值工作集约819.55 MiB，土地、种子、产品、现金和逐单分配账通过
验收。该轮最终仅19,379人在世，表明当前生产—消费—压力参数尚未完成玩法/史实校准；本证据
只证明具体家庭生产负载和守恒合同成立，不得解释为人口曲线合理，也不是正式V10批次库存、
多作物轮作或百万完整NPC AI验收。

M24-P4进一步用年度人口—资源反馈证明P3坍缩来自开局种粮/家庭劳力错配、绝户公地未再利用
和过强饥荒响应的正反馈，而非配方负收益。该阶段保留P3基线，新增具体家庭年度公地临时租用
和独立校准候选；前两候选未达到预先声明的人口下限并明确失败，第三候选100万开局50年后
在世777,736人，末5年平均食物满足率89.67%、饥荒死亡率0.20%，通过75%—150%人口区间。

M24-P5在不改变上述人口和生产事实的前提下，把最终家庭/县级紧凑粮种余额导出为426,643个
产品批次和同数库存事务。源粮种312,879,346,725毫口粮由等量负向源余额变动替换，批次、
事务行和物理文件重载守恒；家庭生产、农业工单和年度反馈哈希与P4完全一致。正式
`ProductInventorySystem`也已支持把一张已完成农业工单原子转换为带来源的粮食/种子批次，
但百万桥接仍是顺序二进制检查点，不是把全部批次内联到V28 JSON快照。
这只是无脚本战争技术场景校准，不是东汉史实曲线，也不替代正式租约、劳务、V10批次库存或
历史战争/灾害系统。

M24-P6继续把兼容粮食标量分解为按稳定产品ID保存的来源向量。麦粒与干粮在开局库存、家庭
收获、自用、市场、税粮/地租、官仓赈济、跨县运输、运输损耗/路粮、腐败、冲突征夺、分家和
绝户移交中均保留产品身份，并在每一年度逐产品守恒。100万开局50年后在世777,736人、累计
2,043,436名永久人物，产品来源账和正式桥接源粮同为223,059,110,256毫口粮；P4人口、生产
工单和年度反馈哈希不变。80%麦粒/20%干粮仅是技术验证输入，不是史实作物比例；当前仍未加入
多作物营养、体积、保质期、价格与轮作差异，也没有把来源向量并入正式V28分区快照。

M24-P7在独立压力世界加入小麦、粟、黍、稻、菽五类农业绑定和六类食品，使作物产量、县级
豆科轮作支持、营养满足、运输体积、市场篮子、损耗选择和家庭保存加工进入真实结算。第一
候选期末675,411人，低于预先声明下限并保留为失败证据；第二候选100万开局50年后在世
765,746人、累计2,025,971名永久人物，通过人口包络，峰值工作集约1,018.86 MiB。该扩展包
尚未并入V28正式核心内容，轮作也只是县级有界支持，不能描述为完整多作物农业运行时。

M25-P0建立正式世界统一执行底座。`WorldSimulator`现有行为与命令/事件入口组成的25个时段及
日界步骤已经按稳定系统ID、
阶段、频率和顺序注册；运行时到期命令经过处理器规划，事务批次全部预检并共享资源预约后才
提交，事件只在提交完成后发布和稳定分发。M25-P7现已把命令、批次结果和提交后事件升级为
V33持久领域对象；旧系统仍通过兼容适配器直接修改世界，具体库存/财政/劳动事务迁移和M24
百万世界正式接入仍未完成。

M25-P1完成第一段正式食品接入：汉代食品扩展资源可在不复制核心小麦ID、不增加固定作物或
食品枚举的前提下，把粟、黍、稻、菽及六类食品属性注册到正式内容清单。家庭粮仓可以建立任意
正式产品期初批次，并按稳定优先级、生产日和批次ID消费未预留食品；物理数量、营养、体积、
市场价值和逐批次负向库存事务分别记账。普通内容增加复用既有V28批次与内容清单，因此没有
升级存档结构；县级市场、官仓、旧家庭粮账和跨县运输的全面批次化仍待后续阶段。

M25-P2建立V29食品库存权威与显式正式化适配器。V28存档升级后仍保留家庭、村仓和县仓旧余额，
不推定食品来源；明确执行正式化时，系统才按内容包稳定开局份额把三层旧粮等量替换为产品批次，
清零旧余额，并为村公共粮仓和县仓建立政府组织所有的正式容器。事务保存旧余额负向来源和逐批次
正向写入，领域校验同时检查来源唯一、容器归属和数量守恒。正式化仍是显式玩家/创世选择，不会
在V28迁移或世界调度中自动发生。

M25-P3完成正式食品的第一段地方运行闭环。农业完工把可食收获直接写入保留品种、产地、品质和
工单来源的家庭批次；村庄月结按营养需求从家庭批次消费，并在不足时把村仓真实批次拆分到户。
年度税粮从家庭移入村仓、再按留成率汇缴县仓；县级赈济反向移动县仓批次。所有转移均保护预留量、
受两端容量限制、逐产品和重量守恒，并由领域校验拒绝伪造所有权边界或改变产品身份。旧三类粮食
标量在正式模式的一年长跑中保持为零；跨县民运和静态仓储自然损耗已由后续阶段接入，
多作物种子批次仍未迁移。

M25-P4完成V30正式县级食品市场。卖方家庭以明确产品批次建立卖单并形成真实预留，买方家庭以
真实财富建立买单现金托管；同县撮合按限价、创建日和稳定ID确定顺序，部分成交受批次余量和买方
粮仓容量约束。成交将原批次来源与品质复制到买方批次、支付卖方并写入订单、成交、库存事务和
县—产品价格账；取消与到期释放剩余预留或退还托管。V29迁移只建立空市场集合，不从旧挂牌库存
伪造正式商品或成交。旧库存权威继续使用原市场，跨县商队与完整NPC自动下单仍待后续阶段。

M25-P5完成V31正式跨县民用货运切片。不同县的真实买卖订单以起运地交割：卖方预留批次装入
真实承运人物随行容器时转为买方在途财产，商品托管支付卖方，独立运费留待完成收货后支付承运
家庭。承运人物继续消耗个人口粮，货物按产品易腐与食品腐败敏感度每日产生确定性自然损耗；
到达后按买方粮仓容量分批卸货。旅程、人物、容器、库存事务、成交、货运及流水共同校验数量、
资金、地点和生命周期守恒。V30迁移只建立空货运集合，不倒推历史商队。自动寻路、多段民运、
押运风险和NPC自动下单仍待后续阶段；静态仓内自然损耗已由M25-P15接入。

M25-P6完成V32民用货运规划切片。系统从既有真实跨县买卖订单生成有界货运需求，只扫描活动
订单、需求和显式承运登记，不扫描全部永久人物。承运登记绑定真实人物、家庭移动容器、稳定
计价、最大里程和已知路线；最短已知/最安全已知策略只在该知识子图内规划，未知直达路线不会
泄漏给承运AI。报价按运费、安全度、里程和稳定ID选择，选择结果原子进入M25-P5成交与库存账。
多段货运由同一人物和容器逐段旅行，中途只进入等待下一段，最终地点才收货。V31迁移把既有
直接货运保留为单段计划，不伪造需求、登记或报价。多承运人换手、动态改道和NPC自动创建市场
订单仍待后续阶段。

M25-P7完成V33持久命令、批次执行结果和事件出站箱。待执行命令不再只存在于运行时内存，
而以稳定ID、创建/到期时间、优先级、有序参数、生命周期和尝试次数进入世界存档；非空到期
批次保存成功或拒绝结果、稳定失败代码、有序事务摘要和事件引用。成功提交后的事件先写入
持久出站箱，再按稳定处理器ID分发并逐处理器确认；存档发生在提交后、分发前时，载入后仍可
继续分发，已确认处理器不会被同一运行时重复调用。V32迁移只建立三个空集合，不伪造此前仅
存在于内存的命令、结果或事件。事务处理器和`IWorldTransaction`仍是运行时代码，不被序列化；
旧系统的直接写入也尚未自动获得事务回滚。

M25-P8完成第一条正式经济事务化适配：正式县级市场日界只扫描活动订单，存在到期或同县
可撮合买卖时才建立当日唯一V33持久命令。处理器规划一张显式市场事务，预检正式库存权威、
结算日期和当日共享预约后，复用M25-P4批次转移、现金托管、正式成交和价格账进行提交，并
产生持久市场出站事件。无工作不建立空命令，日期漂移拒绝只形成失败审计，不改变订单、库存
或资金。该阶段没有新增存档字段，也没有把订单创建/取消、跨县货运或其他旧系统一并事务化。

M25-P9完成民用货运日规划的正式事务化适配。正式日界不再直接执行需求生成、承运报价和发运
写入，而只在存在活动货运需求或尚未占用的跨县可成交订单时建立当日唯一V33持久命令。处理器
以稳定上限参数规划一张货运事务，预检正式库存权威、期望日期和同日共享预约后，复用M25-P6
的失效关闭、有限知识报价、稳定选择及M25-P5真实发运。提交后产生持久规划事件；无工作不建
空命令，日期漂移只形成拒绝审计。工作发现只扫描活动订单和货运规划账，不扫描永久人物全表。
该阶段不包含在途损耗、到货结算、承运登记或自动生成家庭订单，也没有升级V33。

M25-P10完成正式家庭食品月结的有界事务化。每个有家庭且到达月界的活动村庄建立一张V33
持久命令；处理器按村庄稳定ID和期望日规划显式事务，只读取该村家庭成员，不扫描永久人物
全表。事务复用M25-P3家庭营养需求、村仓守恒救济、未预留批次消费、食品安全摘要和具体居民
健康/生计后果；至少一户营养不足时发布同村同月唯一持久缺口事件。正式`WorldSimulator`随后
运行工具、医疗、劳役、农业、税粮和迁徙等其余村庄月结，但明确跳过已经提交的第二次食品
消费；兼容标量世界和直接`ResolveMonthly`入口保持原语义。本阶段没有升级V33，也没有自动
创建市场买单、县仓赈济或跨县货运。

M25-P11完成正式公共粮食月结的按县事务化。第十月的家庭税粮从家庭正式粮仓转入村仓，县级
事务只按当日`FoodTaxTransferred`流水计算汇缴并保留地方份额；随后县仓依据已经提交的村庄
食品安全和县市场压力发放真实批次赈济。每个有实际税粮或赈济压力的到期县只建立一张V33
持久命令，日期、县治理与前置村庄结算漂移会在业务写入前拒绝，成功后发布县级持久事件。
正式`WorldSimulator`的其余村县月结跳过已提交的税粮、汇缴和赈济，直接系统入口仍保留完整
兼容语义。本阶段没有自动创建市场买单、跨县救济运输或赈济AI委任，也没有升级V33。

M25-P12完成县仓赈济不足后的首条真实采购委任链。M25-P11现在保存请求、实发与未满足量，
真实未满足时发布县级短缺事件；事件消费者只为次日建立一张保存预算、数量、最高单价和来源
事件的持久命令。事务重新核验真实县政府、存活领袖和县仓，只按单价、创建日、产品与稳定ID
选择本县活动家庭卖单，将其预留食品批次守恒转入县仓，由政府金库支付卖方并同步正式价格账。
V34新增独立公共采购成交账，避免把官府伪装为家庭买方；无卖方、预算或容量不足会留下未履约
县财政审计而不造粮。当前仍不含跨县寻源、承运、途中损耗、审批树或自动创建家庭卖单。

M25-P13完成本县采购仍不足后的有限知识跨县履约。M25-P12仅在仍有未履约量时发布跨县寻源
事件，次日V33持久命令冻结数量、商品预算、运费预算和最高单价。候选只读取外县活动家庭卖单、
活动承运登记及每名承运人的已知路线；没有连续已知路线的市场不会暴露。V35把民用货运扩展为
家庭买方或政府组织买方两种互斥模式，外县预留批次起运后成为目标县政府的在途财产，复用同一
多段旅行、确定性自然损耗、容量受限收货和完成后运费结算，最终进入目标县仓。商品款、运费托管、
卖单、批次、采购成交、财政和货运流水共同守恒；无货源、路线、承运能力或预算时只保存未履约
审计。当前仍不含未知市场侦察、自动创建卖单、多承运人换手、押运战斗或州郡审批树。

M25-P14完成跨县粮食实际到仓后的V36赈济恢复闭环。政府民运完成后，同片段建立唯一持久命令，
只从目标县真实县仓向原短缺村公共粮仓分发，并逐票保存发运、自然损耗、实到、实发、在途日数、
收货等待和异常代码。分村恢复量与总账、库存事务和民运单共同校验。首票仍有真实货源缺口时，
只允许一次补运：数量不超过剩余缺口，商品款与运费分别不突破原跨县采购命令的预算余额，货源和
承运仍受活动卖单、真实登记和承运人已知路线限制。补运再次按实到分配，失败或二次不足保留耗尽
状态而不递归发运。该阶段仍不含到村后具体家庭的月内领取、未知市场侦察或州郡审批树。

M25-P15完成V37正式静态仓储环境与食品损耗审计。家庭粮仓、村仓和县仓保存开放命名空间环境ID、
防护基点和批次下次评估日；每三十日由单个持久批量命令按稳定批次顺序结算食品敏感度、设施状况、
新鲜度衰减和未预留实物损耗。数量损耗以独立库存负事务入账，零数量损耗仍保存环境与新鲜度审计；
预留量不会被仓损穿透。带承运人的移动容器被明确排除，避免与民运途中损耗重复。V36迁移只设置
当前环境和未来评估日并建立空审计集合，不倒推旧世界腐败。该阶段仍不含村仓到具体家庭的月内领取、
火灾虫灾、仓吏侵占、熏蒸配方或仓储建设界面。

M25-P16完成V38具体家庭月内救济领取原型。M25-P10月结不再只留下村级“有人缺粮”：每个实际
短缺家庭保存来源事件、月结日、精确营养缺口、累计实物/营养交付、剩余需求、最近领取人和正式
库存事务。领取按月结日、村庄和家庭稳定排序，只由仍在本村且未服征兵役的真实家庭成员，把村公共
粮仓中的真实食品批次转入有容量的家庭粮仓；无粮、无领取人或无容量时保留开放请求。日界在家庭
月结后、仓损前尝试领取，跨县赈济粮到村后也在同一到达片段尝试领取。领取不回写已经发生的饥饿
健康损失，也不把同一月消费再扣一次；V37迁移只建立空领取账，不猜测旧世界家庭缺口。当前仍不含
救济资格政策、月内实际进食后的健康恢复、腐败截留、排队演出或玩家操作界面。

M25-P17完成V39家庭救济实际进食与饥饿恢复原型。M25-P10在施加缺粮后果时保存本次实际受损
人物及健康/生活压力损失；该资格不会从家庭当前成员、关注精度或食品安全缓存反推。P16领取仍只
增加家庭真实库存，P17随后只扣除该领取单库存事务所形成的未预留食品批次，并按累计实际进食营养
比例有界恢复原受损人物。死亡、离村或服征兵役者不会远程恢复，也不会由迁入者替代。日界与运输
到达段均按“到村→领取→进食”稳定执行；V38迁移只建立空进食账，不伪造旧资格或历史治疗。当前
仍不含资格政策编辑器、照护分餐、特殊病号餐、排队演出和玩家操作界面。

M25-P18完成V40逐人救济配额与离队保留原型。新短缺账按同次月结中每名实际受损人物的年龄营养
需求比例分配家庭缺口，取整余数按人物稳定ID补齐，逐人配额之和必须精确闭合家庭应补营养。人物
离村、死亡或服征兵役时，其未完成份额不会被其他成员代吃或转移；本人重新合格后才可继续进食。
整件食品扣除形成而尚未分给具体人物的营养保存为显式备餐余额，不触发远程恢复，也不凭空补粮。
V39有账存档使用明确旧家庭共享策略和兼容哨兵，不伪造历史逐人配额。当前仍不含村内跨家庭资格
优先级、照护者送餐、长期营养/疾病联动、备餐腐败和玩家操作界面。

M25-P19完成V41村内跨家庭救济排序与授权快照原型。村庄保存开放命名空间优先政策、授权政策和
授权组织；新领取单在建单时冻结短缺严重度、脆弱受损人数、受损总人数、授权日、真实政府组织和
当时领导人物。同日同村请求按严重度、脆弱人数、受损人数降序，最后以家庭稳定ID破同分；月结日
仍优先于后来的请求。没有正式县治理的村庄使用明确紧急系统授权，不伪造官府或人物。V40既有单
保留旧家庭ID顺序，并用兼容哨兵表示不存在历史评分和授权。当前仍不含玩家政策编辑、逐单人工
批准、关系徇私、腐败截留、照护者送餐、长期营养/疾病联动和排队演出。

M25-P20完成V42家庭照护送餐与逐笔交付审计原型。新短缺账在同一月结人物访问中冻结儿童、老人和
短缺后重度虚弱者的照护需求；结算时只从同户永久人物中按稳定ID选择同村、存活、未服役且15至
60岁的照护者。库存事务分别保存实际执行人和受助人，营养只记入原受损人物，并以永久交付记录
闭合照护者、受助人、日期、营养和正式事务来源。没有合格照护者时该人物份额继续保留，不扣粮、
不恢复，也不生成临时NPC。V41历史账使用明确旧自助政策，不倒推照护需求或交付历史。当前仍不含
护理技能、病号餐、长期营养/疾病联动、照护耗时、关系奖励和玩家照护界面。

M25-P21完成V43长期营养与照护反馈原型。正式家庭月结复用同次已经访问的具体居民，为发生过
缺粮的人物稀疏建立营养档案；逐笔追加账保存月度缺口、充足月份恢复和真实救济进食抵扣，连续
缺粮达到阈值后才以稳定内容ID建立营养性疾病发作并写回人物健康。P20照护者只负责送餐，营养债
只在具体受助人实际取得准备营养或可追溯食品时减少；未送达、只领取或仍在备餐余额中的粮食不
降低风险。V42迁移只建立空档案/账/发作集合，不根据健康、旧救济单或关注状态伪造历史。本原型
仍不含传染病传播、诊断、药方、医者治疗、死亡判定、护理工时和玩家医疗界面。

M25-P22完成V44正式营养病案与药物治疗原型。活跃营养性疾病由同地、未服役且具备最低医术的
村庄医者诊断；成年患者本人授权，未成年人必须由同户同地成年人物授权。诊断即形成永久病案，
缺药不会抹去诊断；治疗消耗医者家庭真实 `product.medicine.herbal_material` 批次并写库存事务，
恢复量受医术、药材品质、人物缺失健康和该发作尚未恢复伤害共同约束。治疗前后营养债和风险必须
相等，药物不能代替进食或赈济。村庄月结只访问本村已加载家庭成员和稀疏发作，不扫描全部永久
人物。当前仍不含药材采集/加工/补货链、诊疗工时、复杂方剂、传染病、死亡判定、军医正式库存
迁移和玩家医疗界面。

M25-P23完成V45正式草药供应原型。村庄创世以明确`historical_inference`来源等级建立野生药草
资源体；一个按稳定家庭ID选择的本地农户以真实永久人物、家庭设施能力和耗时采集订单生成原药草
批次，再通过晾晒拣选配方形成可治疗的草药材并积累药草加工技艺。正式县级市场现在接受所有带
`product.market`标签的产品，草药材交割继续保存批次品质、产地、生产日、现金、预留和容量账；
医者家庭库存不足时只在同县建立买单，不暴露未知跨县货源。自动规划只沿已加载村庄—家户—成员
边界运行，不扫描全体永久人口。当前仍不含医者工时、诊金、病案结案、复杂方剂和军医正式库存。

M25-P24完成V46正式民间诊疗服务原型。新病案由合格医者签发稳定内容ID处方；成功治疗生成追加式
服务记录，按每次120分钟占用医者每日480分钟额度，异户诊疗在患者家庭与医者家庭之间守恒转移
诊疗和药材费用，同户照护不制造资金。真实服务同时闭合处方、草药批次、库存事务、健康恢复和
医术实践成长；伤害恢复、营养发作结束或患者死亡后病例明确结案并保留全部历史。V45迁移不倒推
旧处方、旧工时、旧诊金、旧成长或旧结案。本阶段仍不含复杂辨证、医疗欠款/慈善、传染病、未来
预约队列、跨县求医、军医正式库存和玩家医疗界面。

M25-P25完成V47正式军队医疗原型。每支已初始化军队拥有由所属组织持有、随军队驻地移动的唯一
军药容器；原型开局药材以正式草药材产品批次和开局库存事务保存。具体军役伤员按稳定人物/军役
顺序进入开放ID分诊，由同军有效军医或当前主将授权的同地医者执行救治；每人消耗一单位未预留
军药、占用60分钟且与民间诊疗共享每日480分钟上限。成功服务逐人闭合病例、授权、批次、库存
事务、健康、军役归队、军队缓存和医术成长，家庭之间不产生诊金流。旧汇总医疗记录只保留为展示
兼容账。V46迁移不倒推军药库、旧分诊、旧救治、旧工时或旧成长。本阶段仍不含后送、野战医院、
手术、残疾、传染病、军药自动采购和玩家军医界面。

M25-P26完成V48军药真实采购与补运闭环。既有军需货运单新增稳定收货用途和目标容器合同：历史
粮运继续按实际到货量进入军队口粮桥，正式军药单则只能把带`product.medicine`标签的真实批次
送入目标军队唯一药库。付款、承运人物、路线、多段交接、车队自用口粮、自然/敌对损失和夺回均
复用M23物流事实；到货按实时库容分批实收，每次生成新的组织所有药材批次、正向库存事务和货运
交付流水，不增加军队口粮。V47迁移只把旧货运标记为军粮收货，不伪造军药订单或到货历史。当前
仍由明确命令签发，不含自动需求预测、委任报价生成、伤员后送和野战医院。

M25-P27完成V49具体伤员战场后送闭环。有军队级权限的人物可以为同军具体伤员指定既有道路、
后方地点和合格接收医者，并从同地在役人物中派出2—8名具体救护队员。伤员和救护队分别建立真实
个人旅行并承担既有旅行口粮；救护队转入医疗后送勤务并从军队可战兵力缓存扣除。来源军队可以
继续向其他地点行军，但不得把已后送人物带走，随军医疗也不得隔空治疗。全员到达后先进入等待
接收，指定医者交接只保存责任和医术快照，不自动治疗、康复或归队。V48迁移只建立空后送集合，
不倒推旧伤亡或旅行。当前仍不含接收后治疗、救护队返程、转院、野战医院、车辆和自动目的地AI。

M25-P28完成V50后方诊疗、床位、返程与归队闭环。后方诊疗点只能登记在已有`Clinic`能力地点，
并绑定所有组织、独立正式药库和真实床位容量；接收后的具体伤员逐床住院，由指定医者投入与民间/
随军医疗共享的每日工时并消耗具体草药批次。治疗只恢复健康和返程资格，不会隔空恢复军队兵力；
出院后伤员与原救护队沿明确道路分别返程，来源军队在会合前不得再次行军。全员到达后才恢复
`Active`并同步军队缓存。V49迁移只补空集合和空返程字段，不倒推诊疗点、治疗或归队历史。当前
仍不含新建医院、多疗程、手术/感染/残疾/死亡、多段返程、动态追赶和自动目的地AI。

M25-P29完成V51野战医院建设、维护与分阶段诊疗闭环。具有军队级权限的人物可在军队驻地从
所属组织仓库消耗20单位木料、5单位皮革和500钱建立项目，再由具体合格人物按日投入3人日劳动；
完工生成独立于地点`Clinic`标记的4床野战医院和空军药库，不凭空生成药品。设施每10日需要
100钱和1单位木料维护，逾期由日调度停运，补足真实成本后恢复。既有医馆仍沿用单阶段治疗；
野战医院冻结为“稳定伤情（60分钟、1药、健康至少5000）—恢复（120分钟、1药、健康至少
6000）”两阶段，在全部完成前不得取得返程资格。V50迁移只建立空建设/劳动/维护集合并把既有
诊疗历史补为单阶段，不倒推任何野战医院事实。当前仍不含手术、感染、残疾、死亡、转院、设施
毁伤和自动建设/维护AI。

M25-P30完成V52复杂伤情、感染处置与冻结诊疗计划原型。伤型使用稳定命名空间ID和世界内
数据定义，首批软组织伤、骨折伤和穿透伤只是一组可替换内容；新增普通伤型或MOD定义不要求升级
存档结构。每次新住院由接诊健康、真实后送天数确定性生成永久伤情记录，冻结严重度、污染度、
感染风险和本次诊疗协议序列，不受关注层级或以后规则变化重算。达到风险阈值的伤员必须在最终
恢复前完成180分钟、2单位真实草药的感染控制，具体医者、批次、库存事务、健康和感染结案引用
逐项闭合。V51迁移建立核心伤型定义和空旧伤情集合，只为既有住院补回V51等价计划，不倒推旧
伤型、感染或新增疗程。当前仍不含手术、永久残疾、伤后死亡、转院、动态感染传播和自动诊疗AI。

M25-P31完成V53创伤手术、永久伤残与医疗退役闭环。手术方案使用稳定命名空间ID和世界内数据
定义，核心骨折/穿透伤在接诊时冻结“创伤清创复位”阶段；手术要求更高医术、240分钟工时与
3单位真实草药，并继续闭合具体批次、库存事务、健康和医术开闭值。达到冻结严重度阈值的伤员
只在具体手术完成时永久扣减一次劳动能力，伤情保存劳动能力开闭值、惩罚量和治疗引用，不修改
先天禀赋或删除技能/家庭/关系。完整疗程后伤残者留在诊疗地点，原救护队沿真实道路独立返军；
全员到达后患者军役转为退役，救护队恢复现役并同步军队缓存。V52迁移不倒推旧手术、伤残或
退役。当前仍不含伤后死亡、跨院转运、手术器具耐久、助手/麻醉、长期护理和自动诊疗AI。

M25-P32完成V54治疗前跨医疗设施转运闭环。同一伤员、军役、后送、住院、永久伤情和冻结疗程
连续保留，源床位在出发时释放，目标床位在途预留；目标药库一个具体草药批次按全部剩余冻结
阶段正式增加预留量。患者与原救护队沿真实路线分别旅行，到达后只有指定合格医者接收才迁移
当前诊疗点、当前照护地点和主治责任；后续治疗从同一预留批次同时扣减实物和预留量。V53迁移
只建立空转运集合、空引用和旧后送的既有照护地点，不倒推转院历史。本阶段只支持同组织、治疗
开始前、直接路线的一次转运；治疗后/多次/跨组织转院、取消改道、途中恶化死亡和自动选院仍未实现。

M25-P33完成V55治疗后伤后死亡、家庭继承与军队抚恤闭环。第一版只允许已经完成冻结疗程、
救护队返程和医疗退役的重伤员在政策等待期后死亡；政策以稳定命名空间ID数据定义严重度、
治疗后健康、等待天数和按军衔计算的抚恤。死亡保留永久人物与既有伤情/治疗历史，军籍和人口账
只扣减一次；个人财富完整进入原家庭，死亡家主由在世成员按出生日期和稳定ID继任，组织财政与
家庭收入保存相反开闭值。V54迁移只建立核心政策、空历史集合和次日启用边界，不倒推旧死亡、
继承或抚恤。转运/住院/返程途中死亡、遗体运输、抚恤欠款和复杂析产仍待后续扩展。

M25-P34完成V56返程前伤后死亡、医疗责任快照与救护队独立返程闭环。冻结疗程全部完成但尚未
开始返程的医疗退役重伤员，可在政策等待期后于当前诊疗地点死亡；结算复用V55永久人物、人口、
军籍、继承和抚恤原子闭环，同时冻结当前设施、照护组织、主治医者、医术、授权人和权限。患者
遗体保留在诊疗地点且不生成返程旅程，原救护队仍必须沿真实路线独立返军，全部到达后救护队
恢复现役、患者军籍保持死亡、住院和后送结案。V55迁移只为既有死亡补入“医疗退役后死亡”
情境并建立空责任集合与次日启用边界，不倒推旧设施或医者责任。治疗未完成、跨设施转运途中、
患者/救护队返程途中死亡，以及遗体运输、安葬和责任追究仍待后续扩展。

M25-P35完成V57住院中伤情恶化、死亡与医疗资源结案闭环。已接诊但冻结疗程尚未完成的重伤员，
可在满足数据定义恶化政策的住院天数、严重度、健康损失和死亡阈值后原子死亡；结案冻结健康
开闭值、已完成/总疗程阶段、下一协议、当前设施和主治医者。普通未执行疗程不扣药；已完成转院
的患者只通过正式库存事务释放具体药材批次中尚未消耗的预留量，批次数量不回增，已消耗药材
不返还。死亡立即释放床位并让后送进入救护队待返状态，遗体留在诊疗点；救护队仍沿真实道路
独立返军。V56迁移只建立核心恶化政策、空结案集合、空引用和次日启用边界，不倒推旧住院死亡
或药材释放。原始后送、跨设施转运和返程旅程中的死亡仍待后续扩展。

M25-P36完成V58跨设施转运中和到达待接收阶段的死亡闭环。转运尚未完成责任交接时，数据政策
冻结健康开闭值、路线和死亡时剩余里程；目标床位立即取消预留，目标具体药材批次的全部未用
预留通过正式库存事务释放且实物不回增。接收责任尚未发生，责任快照仍指向来源设施与原主治
医者，并同时保存目标设施、指定接收人、授权人与路线。在途死亡不删除患者旅程，该旅程转为
同一救护队继续护送遗体；全员到达后遗体留置目标地点，救护队才可独立返军。V57迁移只建立
空结案集合、空引用与次日启用边界，不倒推旧死亡。原始后送和返军旅程中的死亡、遗体改道、
安葬及事故归责仍待后续扩展。

M25-P37完成V59原始战场后送中和到达待接收阶段的死亡闭环。后方正式诊断尚未发生，因此独立
数据政策以健康开值反推并冻结未诊断伤势、健康损失、闭值、派出日、路线和死亡时剩余里程，
不伪造伤情、住院、床药或接收医者责任。接收前责任冻结在来源军队、所属组织和原后送授权链；
在途死亡保留患者原旅程作为同一救护队护送遗体，抵达原定后方后遗体留置当地，救护队可在没有
住院记录的情况下沿真实道路独立返军。V58迁移只补核心未来政策、空结案集合、空引用和次日
启用边界，不倒推旧后送死亡。返军旅程中的患者/队员死亡、遗体改道安葬和事故归责仍待扩展。

M25-P38完成V60患者返军途中死亡、遗体随队返军和归队结案。完成治疗并释放床位后，患者与
救护队已沿明确道路返军时，独立数据政策冻结伤情、健康开闭值、最后诊疗设施与主治医者、
返程路线、开始日和死亡时剩余里程。死亡继续闭合永久人物、人口、军籍、家庭继承和原军队
组织抚恤；最后照护组织可以与抚恤组织不同。患者原返程旅程继续表示遗体护送，死者不再消耗
行粮，来源军队在遗体和全部队员会合前保持冻结；会合后遗体归抵军队、队员恢复现役、住院和
后送记录结案。V59迁移只建立核心未来政策、空结案集合、空引用和次日启用边界，不倒推旧返军
死亡。该阶段尚未覆盖患者先到后等待队员时死亡；这一边界已由后续 M25-P39 接续。救护队员
返军死亡、遗体改道安葬和事故归责仍待扩展。

M25-P39完成V61患者已抵达来源军队、等待救护队员期间死亡和最终归队结案。患者返程旅程已经
完成且至少一名原救护队员仍有真实返程里程时，独立数据政策冻结患者已抵达事实、原返程路线、
开始日、健康开闭值、最后诊疗设施/医者，以及每名救护队员死亡时的旅程和剩余里程。死亡仍只
闭合一次永久人物、人口、军籍、家庭继承与来源军队组织抚恤；遗体留在来源军队，队员继续原
旅程，来源军队在最后一名队员归队前保持冻结。全员归队后队员恢复现役、住院与后送记录结案。
V60迁移只补未来政策、结案默认字段和次日启用边界，不倒推旧等待阶段死亡。救护队员返军死亡、
遗体改道安葬和事故归责仍待扩展。

M25-P40完成V62救护队员返军途中死亡、遗体沿原旅程归军和返程结案。只有已经建立非零剩余
里程返军旅程的具体救护队员才能进入该死亡合同；数据政策冻结返程路线、起终点、开始日、
死亡时剩余里程与健康开闭值。死亡一次闭合永久人物、人口、军籍、个人财富继承、家主稳定继任
和来源军队组织抚恤，且与患者伤后死亡使用互斥反向引用共享同一本继承/抚恤账。死者原旅程继续
表示同队护送遗体，死者不再消耗行粮，其他患者和队员不重建、不改道旅程；遗体可先于生还者
归军并保存实际到达日，来源军队仍冻结到全部活动旅程结束。结案时死者军籍保持死亡，生还队员
恢复现役，患者按既有存活、返程死亡或已抵军死亡策略结算。V61迁移只补核心未来政策、空死亡
集合、空队员/继承/抚恤引用和次日启用边界，不倒推旧返程死亡。原始后送/转院阶段队员死亡、
队员受伤/逃亡/失踪/被俘、遗体改道安葬和事故归责仍待扩展。

M25-P41完成V63部分疗程后首次同组织转院闭环。住院伤员完成至少一个但尚未完成全部冻结阶段
时，可由军队级权限沿既有直接路线转往同组织另一运行设施；转院冻结出发时已完成阶段数，只按
剩余协议在目标药库具体批次中预留药材。来源阶段继续引用来源设施、来源医者及普通消耗事务，
目标医者接收后才承担剩余阶段责任，后续治疗只消耗本次预留并保持原阶段索引。转运中死亡继续
沿V58结案：已完成治疗不回滚，只释放尚未使用的目标预留。V62迁移只设置次日启用边界并把旧
转院保持为零阶段快照，不倒推部分疗程后转院。多次转院、跨组织结算、取消改道与自动选院仍待扩展。

M25-P42完成V64同组织连续转院责任链。一次住院可在军队级授权下沿既有直接路线执行最多四段
转院；每段保存稳定段序和前后引用，来源设施/医者必须承接上一段目标设施/接收医者。签发下一段
时，上一目标院已用药材保持消耗，未用预留以正式库存事务全部释放，下一目标院只按当前剩余冻结
疗程重新预留具体批次与床位。治疗按阶段边界归入对应段，空中转段不会伪造治疗；当前段转运死亡
只关闭当前预留并保留全部前段责任。V63迁移只把既有单次转院标为第零段并设置次日启用边界。
跨组织费用/药材/责任结算、取消改道、拒收和自动选院仍待扩展。

创建世界时必须显示史料参考人口、实际生成比例、实际开局人口、预计长期累计人物和
硬件压力。人口缩尺必须落实到具体家户、职业、兵源、消费和生产，不能只修改界面总数。
累计人物接近或超过5,000万时可以警告并降低非关键缓存与结算频率，但不得删除人物、
阻止出生、合并身份或改写已经发生的世界事实；超过部分不属于正式性能保证范围。

### 0D.3 生产颗粒度调整

旧设计中的部分“农业、产业、繁荣度”仍偏抽象。正式规则要求具体生产对象，
具体工作必须由人物、设施、材料和时间完成；界面与AI委任尚未实现连续接管，不能把
“玩家暂不逐项操作”误写成世界中不存在这些工作。

M17与M17-P0已经建立小麦农业工单、家庭粮仓、生产审计、稳定内容ID、作物与地方品种、
产品、配方、生产方法、核心JSON和存档内容清单。M19-P0进一步建立V10产品/种子批次、
通用库存事务、旧粮账原子转换、首条麦粒—面粉/麸皮—干粮加工链和市场可售聚合查询。
M25-P1又建立正式汉代五作物六食品扩展、食品营养/体积/腐败敏感度/价值/消费优先级合同和
家庭粮仓批次消费入口；M25-P2又建立V29库存权威和家庭、村仓、县仓的显式守恒转换入口；
M25-P3已接通正式权威下的收获、消费、村县税粮与赈济月度循环，M25-P11进一步把正式税粮、
村仓留存、县仓汇缴和县仓赈济接入按县V33持久命令；M25-P4又建立正式卖方批次预留、
买方现金托管、同县撮合、部分成交和县—产品成交价账，M25-P5进一步建立起运交割、真实承运、
途中自然损耗、容量受限收货和独立运费结算。固定`CropKind`技术债已经移除。
默认兼容世界仍保留家庭单一粮食余额和农业专用账本，正式化也仍须显式执行；多仓库运输、腐败
日程、跨县自动经营与消费/军需全面迁移尚未完成。后续必须沿数据定义和批次事务合同扩展，不能重新
引入固定作物枚举。

### 0D.4 科研底座待扩展

M18-P0已经建立稳定ID技能、知识、科技卡、人物掌握、耗时科研项目、设施级技术应用和
科研账，并把首批三张农业科技卡接入M17田地、配方和生产方法。当前只完成个人负责人、
家庭资金和设施应用的第一条闭环，尚未实现材料批次、研究风险、组织知识所有权、传播、
失传、技术窃取、完整五领域内容和科研委任界面，不能描述为完整科研系统。

### 0D.5 创世、认知与动态设施尚无完整代码

连续比例尺、专题视图、个人/组织认知、信息资产抄录、资源创世、多矿脉、NPC选址和
递归委任已经完成设计整合，但现有地图与建设只达到原型或最小底座，尚不能描述为已经
支持这些目标。后续持久结构必须从稳定ID、来源、时间、权限和确定性创世合同开始实现。

### 0D.6 项目阶段表述

`LUOYANG-POPULATION-STRESS-V1` 已在独立压力包中建立 20,542、50K、100K、250K、500K
五档永久 Person 和 365 日固定/自适应建设证据，并将摘要接入洛阳 Unity 调试场景。它证明
2,000 米 Cell 在 250K 与 500K 帝都压力档会耗尽当前 4,510 个剩余可开发位，属于“已有压力
原型与容量证据”，不表示全国正式世界已经采用这些压力人口，也不授权建立 SubCell、改为
1,000 米格网或把压力参数当作最终平衡值。

项目已经超出纯预制作，当前更准确的阶段是：

> 核心系统原型已经积累，当前进入“可玩 Demo 主循环整合阶段”。

这不是把长期严谨底座改成轻量假数据，也不是缩减完整游戏方案。当前主要缺口是：大量系统已经
能由测试或开发观察台分别调用，却尚未形成普通玩家可理解的“选择人物—进入具体生活落点—作出
行动—推进世界—看见后果—继续人生”闭环。

### 0D.7 玩家主循环整合债

M1 已建立主菜单、自建或选择现有人物、地图旅行、任务和开发观察台；此后建设、生产、贸易、
军队、战斗、医疗、人口与存档底座持续扩展，但玩家入口没有同步形成统一情境行动、事件选择和
结果反馈。开发观察台中的系统按钮不能替代可玩 Demo。

M26-P0 已于 2026-08-06 完成首个可玩主循环竖切片，任务与证据见
[`TASK_M26_P0_PLAYABLE_DEMO_MAIN_LOOP_INTEGRATION.md`](TASK_M26_P0_PLAYABLE_DEMO_MAIN_LOOP_INTEGRATION.md)：

- 玩家可自建永久人物，或从世界全部合法可扮演人物中选择一人；
- 军人/武将、商人、农户、士人作为首批贯通身份，不代表最终身份上限；
- 地图、时间、行动、事件、建设、生产、贸易、任务和局部战斗使用同一本世界账；
- 医疗对普通玩家收束为“受伤—选择治疗或休养—消耗时间与药材—康复、伤残或死亡”；
- M25-P42 保留为后台医疗事实合同，不继续自动排出 M25-P43。

当前已经可以从 `PlayableDemo.unity` 自建或接管人物，通过普通玩家“行动”页贯通首批身份的
任务、建设、农业、贸易、学习、事件、军队行军、局部战斗和简化疗伤。该状态属于“已有原型”，
不表示最终交互、美术、战术表现、全部身份内容或正式历史剧本已经完成。下一步应先进行人工
20—30分钟可玩性冒烟与交互修正，再按本总纲选择下一项世界系统建设。

## 0E. 推荐后续建设顺序

2026-08-29，`TASK_LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1` 已达到
`LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1_ACCEPTED`。同一正式洛阳的5,980个Cell
全部取得North/East/South/West四向端口、内部拓扑、移动能力和人物尺度Traversal Metric，2,084/
2,084 Facility全部取得Access规则；359个Road、18个Gate-type、2个Bridge均从正式数据派生。
跨Cell正式路线权威已从旧LocalNav图收敛到`CellTraversalPlanner + CellRoute`，同一个M26 Person、
`MovePersonCommand`、WorldTime、体力、口粮、V77存档和重放继续使用同一本世界账。完整核心回归
774/774、Unity EditMode 3/3、图形PlayMode 1/1和差异检查均通过；5,980 Profile构建实测60 ms、
Managed Delta 14,006,144 bytes、GameObject 0，Unity 3×3加载92 ms且驻留19对象/9 Mesh/9 Collider。
LocalSpace不是SubCell，旧LocalNav只保留表现几何
与旧V77路段兼容。食品库存守恒差额RCA现已`ACCEPTED`：长期生活、完整核心与适用Unity食品目标
回归均闭合。固定下一阶段为洛阳外围供应区与城市物流V1；不得继续在本阶段扩建第二套局部导航、
库存、Cargo或Route权威。

2026-08-12，`LUOYANG-184-T4-LIVING-WORLD-COMPLETION-MASTER-V1` 已达到
`T4_LIVING_WORLD_V1_COMPLETE_WITH_DEFERRED_ENHANCEMENTS`。受保护的 400,000 Person、
80,899 Household 与 2,084 opening Facility 未重建；产权/建设、真实外部供应、市场、家族、
个人成长、Office/财政、军需、社会压力、189—190 离屏事件、玩家领域指令和洛阳最小 UI 已在
同一世界账接通。657条既有核心回归、27条T4核心测试（含人口压力、真实可开发Cell、玩家产业与v5→v6迁移）、T4 Unity EditMode按18项功能、5项长期/性能/迁移与4×5 Seed Suite拆分，完整覆盖Seed 1—20，并有两条洛阳
PlayMode Smoke 通过。完整证据入口为
`HISTORICAL_WORLD_REFERENCE/LUOYANG_184_T4_LIVING_WORLD_COMPLETION_MASTER_V1/`。
实物税已从真实家庭/市场食物进入政府官仓，精确历史税率仍需深化；逐路线城门延误、完整犯罪/继承/仕途和最终美术交互仍明确 Deferred；这些缺口
不允许被后续任务写成完成。下一大阶段可以洛阳为已验证样板，恢复 HOT/WARM/COLD 永久人物
世界扩张，但不得顺手物化第二个 T4 城市或全国 5,350 万 Runtime。

M14已经完成200—500人的真实村庄与家庭生活闭环。M15-P4/P5形成“100万人分区二进制
完整合同通过”“累计1000万冷热档案”和“5000万全部在世基础索引与日/月/年到期推进”的
证据；M24-P0/P1/P2/P3/P4又依次完成100万具体永久人物自然演化，以及家户粮食、固定土地、农业
劳力、家庭库存、县级市场、官仓赈济、跨县运输、具体家庭土地/种子/生产工单和压力死亡共同
运行50年的独立世界层长跑；
M15-P6已建立V7正式分区人口包与冷热扩展共存合同。M17-P0已经建立数据驱动生产
内容，M18-P0已经完成首批科技卡、人物科研与设施局部应用底座，M19-P0已经完成产品批次、
库存事务和首条加工链原型。M20-P0已经完成V11关注原因账、局部关系网络和人物冷热驻留
触发底座。M21-P0/P1/P2/P3/P4/P5/P6已经完成人物仓储访问合同、旅行/关系/任务/生命家庭/村庄生活/
人口台账/农业生产/教育/军医康复/军事核心路径迁移、按分区增量检查点和出生人物增量写入。
当前从尚未完成的依赖继续：

全国县域生产经济 V1 参考母版已经完成，但不改变全局实现优先级。洛阳外围供应区或其他县域进入运行时前，必须从
`HISTORICAL_WORLD_REFERENCE/HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_REFERENCE/README.md`
选择明确范围，再把参考能力逐项物质化为 Cell 资源、Facility、Worker、Recipe、Inventory 和真实运输；禁止直接把
县级总量当成库存、设施或“魔法进口”。

M22-P0已完成一县财政、豪族、市场和基层治理的守恒账底座；M11随后完成V13人物
装备、三军军械库、兵种派生、战备修正和战后军械回写原型。M23-P0进一步完成V14
军械成品批次、组织商队容器、采购付款、真实人物运输、等待会合和军械库入库闭环；
M23-P1完成V15组织工坊、四类原料、六类军械制造配方、静态仓装运和耗材维修订单；
M23-P2完成V16资源体、真实人物采矿/伐木工单、木炭与块炼铁的第一条上游补给链；
M23-P3完成V17数据驱动羊群批次、草料采集、繁育、屠宰、植物鞣革和角料副产物闭环。
M23-P4完成V18数据驱动品质维度、产品维度合同、方法技艺与维度修正、工单开工快照、
真实结算后的专门技艺成长和可审计实践流水，并把加工人物变化接入统一人物仓储。
M23-P5完成V19军需货运责任切片：取得方式、买方、货源、承运和损失承担分离，托运军粮
与承运队自用补给分账，商人旅行和现役军人随军自运共享真实路线，并按产品易腐快照记录
自然损耗、财政变化、征发/劫掠治安后果及按实到货。M23-P6继续完成V20预先规划的持久
运输分段、承运人物和容器同地交接、下段口粮真实预留/出库与最终分批收货。M23-P7完成
V21真实押运人物同行、稳定坐标途中风险、避开/击退/截粮结果和威胁组织截获归属审计。
M23-P8完成V22承运/押运人物局部交战伤病、在役伤员回写和有军权目标军队同路线单次追击
夺回；尚未形成自动寻路、临时改道、并行分拨、死亡/俘虏、长期剿匪、完整战术遭遇或后勤网。
M23-P9完成V23军队层级军需委任：有军权人物保存目标、数量、期限、预算、最高单价、承运
偏好和风险红线，真实承运人物提交绑定库存、容器和路线的报价；显式评估按最低成本、最安全
路线或本组织优先稳定择优，并复用既有货运系统发运，权限、预算、人员或库存异常进入持久报告。
M23-P10完成V24委任时间闭环：目标按下次评估日和稳定ID在日界自动处理，报价具有有效期并
可由承运人撤回，过期报价自动退出候选；发运后按报告周期读取真实货运累计量，到货后关闭
目标并保存完成日。该切片尚未实现报价信用与违约、自动寻路、多段方案生成或跨组织层级再委任。
M23-P11完成V25同军队有边界的逐级委任：根目标可按稳定受任人ID原子拆为1—8个直接子目标，
数量完整分配、预算合计不超父目标，产品、目的地、风险红线等核心合同向下继承；军级、分队级
与个人级责任最多形成两层子目标，叶目标继续调用真实报价和货运，日界按深度自底向上汇总完成。
根签发军权与具体受任责任分别保存，受任人死亡、离队或失去委任时军职权限会形成持久异常，
不会发运。M23-P12完成V26未发运失败份额恢复：父目标用“活动子数量＋未分配数量”和“活动
子预算＋预算储备”两条等式守恒；父级受任人可取消尚未发运的直接叶目标，关闭有效报价并
完整回收数量/预算，再以新稳定ID在原权限和合同边界内重派。旧目标、取消原因、报价关闭和
替代链全部保留，取消与重派不修改库存、资金或真实货运。M23-P13完成V27按实际到货结算：
叶目标保存累计实收、剩余需求、历次完成货运和累计费用，途中损耗会形成可补运缺口；后续报价
只发运缺口且不能突破原预算，父目标按活动子目标实际实收自底向上完成。V26已完成目标使用明确
旧完成策略迁移，不重开旧目标也不伪造缺失货物。M23-P14完成V28承运责任结算：报价把买方
自担或承运组织担责写入合同，货运终结后按净自然损失与净敌对损失形成唯一结算并真实支付，
资金不足形成可显式追缴欠款；只有实际收到的赔偿恢复目标净预算。敌方仍保管截获物资时默认
暂停补购，有军权原签发人保存原因并显式授权后才可消费定量授权发运，自动日程只报告待授权
异常而不中断世界推进。V27存档不追溯赔偿或授权。跨政权/州郡法定授权树和AI自主拆解仍未实现。
2026-08-11，**184 洛阳 Historical Person / Family Integration V1** 已完成V69实现。40万永久人物、
80,899户、2,084项Facility继续由同一受保护包提供；25名历史人物绑定同一P-ID，15个既有
FamilyOrganization全部保留，f088/f036共10条污染成员关系被纠正，新增Person与Facility均为0。
15个FamilyCenter均诚实保持Deferred；8个都市圈组织的32条冲突Facility主张没有被转换为所有权。
下一候选是建立派生可写检查点，并接通40万人和2,084设施的Residence→Work→Production→Consumption→
Market→Supply生活闭环；未经新任务授权不得自动启动。

2026-08-06 起，M25-P0—P42 保留为已经取得的世界账与医疗底座证据，不再据其编号自动继续
医疗扩展。后续全局顺序为：

1. M26-P0 首个可玩 Demo 主循环竖切片和 M26-P0A 逻辑纠错已经完成。M26-P1
   商旅—家族成熟玩法纵向切片已形成自动验证通过的试玩候选版：已接入明确目标、有限行情、
   两种筹资、采购、旅行、三选一途中事件、交付、行动结果表现、人物记忆及还债/购车长期后果。
   M26-P2 已形成把人物随身旧布帛迁入共享正式商品批次的候选实现：商品通过稳定产品ID映射，购入、车载、
   途中损耗、出售和城市库存回写使用同一世界账，并提供战略舆图/商队行旅双模式。当前仍需完成
   当前全工程编译和定向核心回归已通过，Unity 增量验收因编辑器占用项目尚未运行；此外仍需独立
   20—30分钟人工盲玩。匿名地方市场与完整家庭订单/商号仓库仍是明确边界。在这些
   验收完成前不得把 M26-P1/P2 标记为最终完成。任务合同见
   `TASK_M26_P1_MERCHANT_HOUSEHOLD_GAMEPLAY_VERTICAL_SLICE.md` 与
   `TASK_M26_P2_STRATEGIC_WORLD_AND_CARAVAN_GAMEPLAY_INTEGRATION.md`。M26-P3 已完成
   《中华三国志》Ms-PL 源码与资源边界审计，并形成独立重写的战略委任政策候选底座：三种
   核心政策通过稳定ID定义权限和优先级，候选行动使用确定性评分与稳定ID同分裁决。M26-P4
   已进一步形成 V66 持久委任书和命令提案：政策权限与组织能力求交集，发布者、受任者、职位、
   辖区、期限、预算和报告周期均成为真实快照；越权、离任和超预算候选不会产生提案，提案也
   不会直接执行领域命令。它尚未接通农业、商业、建设和战争处理器，不得描述成完整组织AI或
   整套游戏接入；专项任务见 `TASK_M26_P3_ZHSAN_STRATEGIC_DELEGATION_INTEGRATION.md` 与
   `TASK_M26_P4_STRATEGIC_DELEGATION_MANDATE_AND_COMMAND_PROPOSAL.md`。当前继续 M26-P5 商号成熟
   玩法，其中 M26-P5A 首先把中山从战略节点扩展为可进入城镇：七个建筑使用持久设施事实，
   中山商行总部绑定真实组织、负责人和正式仓库容器，玩家可从主堂、仓库、市场等场所进入
   已有经营准备。全工程编译、五项定向核心测试和五项 Unity EditMode 测试已有通过结果；人工
   试玩仍待完成。该切片不代表招募、工资、载具、全国分号、产业或 NPC 商号竞争已经完成；
   M26-P5B继续把七座设施升级为V68持久街区、坐标和占地事实，并建立世界地图节点—中山空间近览—
   建筑功能的首条全层级路线；它仍不代表全国城镇布局、自由选址建设、室内行走或完整战争迷雾。
   该候选已通过全工程编译、M26-P5B核心与Unity各5项定向测试、524项分组核心回归和Windows
   构建启动冒烟；人工点击路线与可读性验收仍待完成。
   专项任务见 `TASK_M26_P5A_ZHONGSHAN_MERCHANT_TOWN_OPERATION_SLICE.md` 与
   `TASK_M26_P5B_ZHONGSHAN_FULL_SCALE_MAP_VERTICAL_SLICE.md`。不得同时铺开全部身份、建立
   Demo 专用世界账或复制未取得兼容许可证的参考游戏内容。已接入的 M1 玩家入口与建设、生产、贸易、任务、事件、
   局部战斗及简化医疗反馈继续作为可从 Unity 直接点击运行的底座。以下已完成的
   M25-P0统一执行内核、M25-P1正式食品合同、M25-P2库存权威/守恒转换、M25-P3
   地方收获—消费—税粮—赈济闭环、M25-P4正式县级家庭市场、M25-P5跨县民用货运和M25-P6
   有限知识需求/承运/多段规划、M25-P7持久命令/结果/事件出站箱、M25-P8正式县级市场
   每日事务链、M25-P9民用货运规划事务链、M25-P10家庭食品月结事务链、M25-P11按县公共
   粮食事务链、M25-P12本县赈济采购委任、M25-P13有限知识跨县采购—民运链、M25-P14实际到仓
   恢复—异常账—一次补运、M25-P15静态仓储环境—新鲜度—损耗审计、M25-P16具体家庭
   月内领取、M25-P17实际进食—逐人物恢复、M25-P18逐人配额—离队保留—备餐余额、M25-P19
   村内跨家庭优先级—授权快照、M25-P20真实照护送餐—逐笔审计、M25-P21长期营养—营养性
   疾病风险—真实进食反馈、M25-P22正式病案—诊疗授权—真实药物消耗、M25-P23药草
   采集—加工—县内补货、M25-P24处方—诊疗工时—诊金—医术成长—病案结案，以及M25-P25
   军役伤员—分诊—军药批次—军医工时—归队审计、M25-P26军药采购—军需补运—按实
   入库、M25-P27具体伤员—救护队—道路后送—指定医者交接、M25-P28既有诊疗点—床位—
   药库—住院治疗—返程归队、M25-P29野战医院真实建设—周期维护—两阶段诊疗，以及M25-P30
   数据定义伤型—永久伤情—感染控制—冻结诊疗计划、M25-P31数据定义手术—永久伤残—
   医疗退役—救护队独立返程、M25-P32治疗前跨院转运—目标床药预留—主治责任交接，以及
   M25-P33治疗后伤后死亡—家庭继承—组织抚恤、M25-P34返程前伤后死亡—医疗责任快照—
   遗体留置—救护队独立返程、M25-P35住院中恶化死亡—未完成疗程/床位/转院药材预留结案、
   M25-P36转运中死亡—床药取消—来源责任—遗体继续护送、M25-P37原始后送中死亡—来源军队
   责任—遗体护送—无住院救护队返军、M25-P38患者返军途中死亡—最后照护责任—遗体随队
   归军、M25-P39患者已抵军等待队员期间死亡—队员返程快照—遗体留军、M25-P40救护队员
   返军死亡—遗体沿原旅程归军—生还者归队、M25-P41部分疗程后首次转院—剩余床药预留—
   前后主治责任分账、M25-P42最多四段同组织连续转院—逐段责任/床药闭合作为后台事实合同；
   当前不扩展跨组织转院、自动选院或更细住院管理，医疗在 Demo 中只提供玩家可理解的伤病
   选择、成本、时间与结果摘要；
   不得凭空补粮、
   自动暴露未知跨县货源、一次性包装全部旧系统或把
   “已登记调度”误写成“已事务化”；
2. 继续扩大州郡经济、迁徙、军需和战争：在已完成林矿/畜牧采集—多维品质加工—工匠成长—
   制造—采购—货运损耗—多段中转—真实押运风险—局部伤病/同路线单次夺回—入库—维修
   —军需目标/报价/预算委任—到期调度/报价生命周期/履约报告—同军队父子任务拆解闭环上，
   已接入承运责任、赔偿/欠款和截获货物未终结时的替代采购授权；下一步处理追回货物造成的
   超额到货与剩余物资处置，或转入跨组织州郡后勤授权前的正式方案评审，并继续复用真实物流账；
3. 在正式多作物合同之上继续实现地区适生、逐地块轮作、水利、完整加工工单与仓储灾害/改良；
   随后完成1,000万和累计5,000万永久人物完整游戏负载验收；
4. 建设223年第一个正式历史主题剧本，完成托孤后季汉世界状态、条件式历史事件、
   多身份参与和玩家完全不参与时的自主推进；
5. 223年剧本验收后，再评估184年等其他开局与135—260完整历史数据接入；这些内容
   当前滞后，不与223年并行争夺内容建设资源；
6. 穿越者框架和大型意难平MOD继续滞后，只保留接口兼容与设计记录，不进入近期开发。

## 1. 文档定位

本文统一后续生产、建设、科研和穿越者科技玩法，作为
`WORLD_SIMULATION_FOUNDATION.md`、`GAME_VISION_AND_GAMEPLAY.md`与
`TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md`的专项规则。

作物、产品、配方、生产执行、地方经济和职业成长之间的详细合同见
[`PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md`](PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md)。

生产与建设的操作颗粒度参考《吾今有世家》和九州系列所体现的家族产业、
土地设施、人员经营和供应链思想，但程序、数据、界面、文本和数值全部原创。

本项目避免两种割裂：

- 不把“农业”“工业”做成没有具体产品的城市百分比；
- 不把完整世界模拟等同于强迫玩家逐项点击。任何有玩法价值的细节都可以展开，
  同样的工作也可以交给具体负责人和组织委任完成。

核心原则是：

> 生产具体到水稻田、麦田、桑园、矿脉和织坊；科研、建设与信息传播都由具体人物、
> 设施、材料和世界时间完成。玩家选择操作深度，委任不能绕过真实成本。

## 2. 三个系统的边界

### 2.1 生产系统

回答：

- 在哪里生产；
- 由谁负责；
- 使用什么土地、设施、劳力和原料；
- 生产什么具体产品；
- 成本、产量、质量和风险如何。

### 2.2 建设系统

回答：

- 谁拥有土地或取得许可；
- 建造什么设施；
- 消耗多少资金、材料、劳力和时间；
- 建成后提供什么岗位、容量和生产能力。

### 2.3 科研系统

回答：

- 谁掌握或发现知识；
- 研究什么项目；
- 需要什么人才、设施、资金和时间；
- 成果能够应用到哪些具体产业、设施或组织；
- 如何培训、推广、出售或保密。

三者不能混为一项全局加成：

```text
科研取得方法
→ 建设或改造设施
→ 具体产业采用技术
→ 生产成本、产量、质量或风险发生变化
```

## 3. 生产单位

玩家操作的是具体产业单位，而不是抽象“农业值”。

### 3.1 土地生产

- 水稻田；
- 麦田；
- 粟黍田；
- 豆田；
- 桑园；
- 果园；
- 药圃；
- 麻葛田；
- 牧场；
- 林场；
- 矿场。

### 3.2 加工生产

- 磨坊；
- 酿造坊；
- 冶铁坊；
- 铁器坊；
- 木工作坊；
- 缫丝坊；
- 织坊；
- 染坊；
- 药坊；
- 造船坊；
- 食品加工坊。

### 3.3 服务设施

- 市场；
- 商铺；
- 仓库；
- 客栈；
- 医馆；
- 学塾；
- 驿站；
- 车马行；
- 港口。

### 3.4 公共与军事设施

- 水渠；
- 道路；
- 桥梁；
- 堤坝；
- 城墙；
- 军营；
- 武库；
- 坞堡；
- 烽火台。

## 4. 产业数据

每个产业单位保存能够决定世界结果的事实数据；默认界面只展示玩家当前需要理解的
关键字段，展开视图和审计工具可以读取更细环节：

```text
ProductionSite
  id
  type
  location_id
  owner_id
  manager_id
  scale
  worker_capacity
  assigned_workers
  input_inventory
  output_inventory
  facility_level
  applied_technologies[]
  operating_cost
  expected_output
  quality
  current_risks[]
```

世界模拟可以在后台使用更细参数，但玩家不需要逐日操作。

### 4.1 多层生产指挥与托管

生产必须真实，但操作深度由玩家选择。玩家、管事和NPC设施不得使用彼此割裂的
生产规则；所有控制方式最终都生成相同的生产工单，并由同一套人员、原料、设施、
时间、库存和账本规则结算。

| 控制方式 | 决策者与主要操作 | 典型用途 |
|---|---|---|
| 亲自劳动 | 玩家人物投入自己的世界时间完成劳动 | 农户、工匠、医者和早期创业 |
| 实时派工 | 有权限者安排少量具体人物、工位和优先级 | 玩家深度关注的小作坊、农庄或重点设施 |
| 工单管理 | 指定产品、数量、负责人、期限、质量和缺料策略 | 普通玩家设施的默认经营方式 |
| 目标指令 | 下达目标、期限、预算和合法权限，由负责人拆解工单 | 家族产业、公共工程、军需和官营设施 |
| 方针托管 | 任命负责人并设置库存、利润、质量、风险和扩建方针 | 远程产业、大型组织和绝大多数NPC设施 |

控制权限与关注精度必须分离。玩家关注设施只能展开更详细的人员、进度和原因，
不能凭空获得管理权，也不能改变生产结果。有权管理的设施离开关注后继续按照既定
工单或方针运行；NPC设施保存真实所有者、负责人、人员来源和库存，但普通日常可以
按旬、月或季度批量结算，只有缺料、疾病、离岗、事故、破产和战争等异常进入个人事件。

默认交互采用“工单管理”；远程设施默认使用目标指令或方针托管；实时派工是可选的
深度玩法，不是维持正常产出的强制操作。打开管理界面时可以暂停表现层时间，普通
波动进入摘要，只有超过玩家设定阈值的异常才打断。亲自劳动提供角色成长、关系和
质量控制机会，但占用人物时间，不得成为没有机会成本的永久最优解。

身份和职位决定可用粒度：普通雇工只能决定自己的劳动，工头和管事可以安排授权范围
内的工单，所有者和家主决定产业目标，官吏管理公共或依法征调的设施，太守和君主主要
制定区域方针。身份层级提高后，玩家不需要继续逐个安排普通工人。

组织级委任至少记录受任者、辖区、目标、预算、期限、权限、政策红线、必须上报事项、
报告周期和继任规则。上级目标可以逐级拆解为设施、工单和具体人物任务；玩家能够接管
其中任何仍未结算的节点，处理后再交还，既有人员、库存、建设和行程进度不得重置。

## 5. 农业与具体作物

“农业”是职业和产业大类，不是最终生产对象。首批作物至少区分：

本节名单用于说明内容方向，不构成固定`CropKind`枚举。正式实现使用“作物定义＋地方
品种＋种子批次”，用途分类使用可组合标签；增加普通作物、地方变种、育种结果或MOD
内容不要求修改代码枚举或升级存档结构。

### 粮食作物

- 水稻；
- 小麦；
- 粟；
- 黍；
- 豆类。

### 经济和纤维作物

- 桑；
- 麻；
- 葛；
- 漆树；
- 地方油料和染料植物。

### 园艺和药材

- 桃、李、杏、梨、枣等时代与地区适用果树；
- 地方蔬菜；
- 药材。

具体名单必须进行135—260年的物种、传播年代和地区审核，不默认加入后世传入作物。

### 5.1 作物差异

| 作物或产业 | 主要特点 |
|---|---|
| 水稻 | 产量较高，依赖水利和劳动力 |
| 小麦 | 适应北方，可与地方农时结合 |
| 粟黍 | 耐旱、投入相对较低 |
| 豆类 | 食用，并提供轮作价值 |
| 桑园 | 多年生土地资产，为养蚕提供原料 |
| 果园 | 建设后需要成长，长期产出 |
| 药圃 | 供应医馆和药坊，价值高但市场有限 |
| 麻葛 | 供应纺织、绳索和生活生产 |

### 5.2 简化生产周期

玩家操作：

```text
选择作物
→ 分配土地、负责人、劳力和投入
→ 处理少量关键事件
→ 季末或收获期结算
```

后台可以计算农时、天气、水利、肥力和病虫害，但不要求玩家逐阶段点击。

### 5.3 多年生资产

桑园、果园和林场不是普通年度作物：

- 建成后需要成长时间；
- 进入成熟期后持续产出；
- 战争焚毁会造成多年损失；
- 随土地所有权转移；
- 可以接受嫁接、选育和管理技术。

### 5.4 桑蚕产业链

桑蚕使用清晰但简化的产业链：

```text
桑园
→ 蚕房
→ 缫丝坊
→ 织坊
→ 丝织品
```

玩家管理产业规模、负责人、劳力、原料、技术和市场，不控制每只蚕。

## 6. 建设流程

统一建设流程：

```text
取得土地、所有权或建设许可
→ 支付资金和材料
→ 指定负责人
→ 分配劳动力
→ 消耗世界时间
→ 建成并投入运营
→ 后续升级、改造、扩建或维修
```

建设效果来自真实设施。没有粮仓就不能获得粮仓容量，没有水渠就不能让相应田地
享受灌溉技术，没有合格人员则设施不能满效率运行。

## 7. 科研系统定位

科研是生产、建设、治理和战争的辅助玩法，不独立扩张成实验室模拟器。

玩家只需要进行四项操作：

1. 获得知识或研究方向；
2. 选择科研项目；
3. 分配负责人、参与者、设施、资金和时间；
4. 将成果应用到具体产业、设施或组织。

## 8. 科技领域

一级领域按以下五类组织。这是稳定的顶层分类，不限制每类可以继续扩展的技术、职业实践
和地方变体：

### 8.1 农业与水利

影响作物、桑园、果园、药圃、畜牧、灌溉、土地利用和仓储。

### 8.2 工艺与制造

影响农具、冶铁、纺织、桑蚕、木工、造船和产品质量。

### 8.3 医药与卫生

影响医馆、药品、疫病、战场救治、城市卫生和人口恢复。

### 8.4 交通与建设

影响道路、桥梁、水运、仓库、城防、建设速度和运输损耗。

### 8.5 文化与治理

影响教育、户籍、会计、仓储管理、官府效率、情报和技术传播。

军事技术主要从制造、交通、医药和治理中派生，避免建立一棵脱离经济的独立军事科技树。

## 9. 科技卡

每项技术是一张可阅读的项目卡：

```text
TechnologyDefinition
  id
  name
  field
  description
  prerequisites[]
  required_professions[]
  required_facilities[]
  funding_cost
  material_costs[]
  base_duration
  risks[]
  unlocked_actions[]
  applicable_targets[]
  effects[]
```

示例：

```text
名称：改良灌溉
领域：农业与水利
前置：基础水利
负责人：农业或水利人才
消耗：资金、木材、石料
周期：180日
解锁：
- 水渠改造
- 接入改良水渠的水田产量上限提高
- 相应田地的旱灾损失降低
```

## 10. 科技效果

允许使用清晰的数值加成，但必须绑定具体对象和环节。

禁止：

> 研究轮作，全国粮食产量+10%。

允许：

> 完成轮作技术培训并采用该工艺的指定农田，肥力衰减降低；
> 采用改良灌溉且接入有效水渠的水田，产量上限提高并降低旱灾损失。

科技可以改变：

- 单位产量；
- 劳动力需求；
- 原料需求；
- 建设或生产时间；
- 产品质量；
- 储运损耗；
- 事故、疫病或灾害风险；
- 新建筑、新工艺和新产品；
- 人员培训与传播速度。

科技不直接创造土地、人口、材料、产品或财政。

## 11. 穿越者到来前的科研

原住民社会能够独立研究和改良技术。知识来源包括：

- 长期职业实践；
- 农户、工匠和医者经验；
- 师徒和家学；
- 典籍；
- 官府工程；
- 异地商旅；
- 仿制器械；
- 战争缴获；
- 职业任务和重大事件。

原住民研究流程同样使用科技卡：

```text
发现或取得研究方向
→ 满足前置
→ 指定人才和设施
→ 投入资源和时间
→ 成功、延期、部分成功或失败
→ 应用和传播
```

世界在没有穿越者时也能出现技术进步、地方改良、技术垄断和失传。

## 12. 穿越者到来后的科研

穿越者新增资源：

> 现代知识线索

知识线索可以：

- 直接发现一项研究方向；
- 减少部分前置探索；
- 缩短研究周期；
- 降低失败和事故风险；
- 提高培训与传播效率。

知识线索不能：

- 凭空产生材料和设备；
- 自动让全国掌握技术；
- 直接跨越全部前置产业；
- 把听说过的知识视为可实际制造。

现代技术按时代适配难度分为：

1. 可直接采用的方法和制度；
2. 需要古代工艺适配的技术；
3. 需要建设前置产业的技术；
4. 当前时代条件下基本不可实现的技术。

## 13. 大学生专业知识

一万大学生不是统一科技包。不同专业提供不同领域的知识线索和研究优势：

| 专业群体 | 主要作用 |
|---|---|
| 农学 | 作物、选种、水利、肥料和仓储线索 |
| 医学、护理 | 医药、卫生、防疫和军医线索 |
| 土木 | 道路、桥梁、水利和建筑线索 |
| 机械、材料 | 工具、冶铁、机械和制造线索 |
| 化学 | 医药、材料、肥料和加工线索 |
| 食品 | 储存、发酵、加工和卫生线索 |
| 师范 | 教材、学校和技术传播 |
| 管理、会计 | 财政、仓储、统计和组织制度 |
| 历史、文科 | 历史认知、政治和文化适应 |
| 数学、计算机 | 统计、算法、密码和流程管理 |

人物对知识的贡献受到专业、学习程度、实践经历、记忆准确度和教学能力影响。

## 14. 科技状态

技术掌握至少具有以下三个核心状态；后续可以在不破坏存档合同的前提下增加验证、
失传、受限传播等细分状态：

### 未知

没有研究方向，不能立项。

### 已知或研究中

知道方向，可以投入人才、设施、资金和时间。

### 已掌握

允许在符合条件的产业、设施或组织中应用。

“已掌握”不等于全国自动生效。

## 15. 科技应用

科技成果必须选择应用对象：

- 改造一座设施；
- 为一种产业采用新工艺；
- 为一个组织颁布并维持制度；
- 培训某地区负责人；
- 建设科技要求的新设施。

示例：

| 科技 | 应用位置 | 效果 |
|---|---|---|
| 改良灌溉 | 接入改良水渠的水田 | 提高产量稳定性、降低旱灾损失 |
| 选种法 | 指定作物和种子田 | 提高基础产量与灾害稳定性 |
| 改良粮仓 | 指定仓库 | 降低霉变、鼠害和库存损耗 |
| 改良铁器 | 指定铁器坊和采用农具的田地 | 降低部分劳动需求 |
| 基础防疫 | 指定城市、医馆或军营 | 降低疫病发生与传播风险 |
| 分类账与盘点 | 指定组织和仓库 | 降低账实差异与管理损耗 |

## 16. 技术传播与所有权

技术掌握范围：

```text
个人
→ 设施
→ 家族或组织
→ 地区
→ 政权
```

传播需要时间、教师和费用。玩家可以：

- 自用；
- 家族内部传承；
- 授予特定作坊；
- 交给官府或军队；
- 出售给商人；
- 公开传播；
- 保密；
- 用于外交交换。

技术可能被垄断、窃取、误传或失传。是否形成更复杂的行会、官营垄断和知识权利制度，
由相应历史与治理设计继续细化，不作为删减知识传播的理由。

## 17. 科研结果

科研只保留少量清晰结果：

- 成功；
- 延期；
- 超支；
- 部分成功；
- 失败但保留部分进度；
- 事故。

失败不默认清空全部进度。高风险项目可能造成材料损失、设施损坏、伤病或声望影响。

## 18. 科研界面

完整界面至少包括：

### 科技目录

查看五大领域、科技状态、前置和应用对象。

### 研究项目

查看负责人、参与者、设施、费用、周期、风险和预计成果。

### 技术应用

查看哪些产业、设施、组织和地区已经应用某项技术。

### 人才

查看能够研究、教学或负责技术应用的人物。

不提供需要玩家手动配置实验参数的界面。

## 19. 与职业系统的关系

- 农户和农学人才推动农业技术；
- 工匠负责制造技术落地；
- 医者推动医药卫生；
- 商人传播材料、器械和异地技术；
- 官吏组织公共工程和制度应用；
- 士人负责记录、教育和传播；
- 军人提出装备、运输和军医需求；
- 家主决定家族产业投资和技术保密；
- 密探可以窃取或保护技术。

职业决定人物如何参与科研，但科研不是一个必须单独选择的主职业。

## 20. 内容建设批次与完整目标

下列内容用于形成首个可验证实现批次，不是最终设计上限，也不能据此删除已经确认的
其他作物、产业、科技、身份或操作层级：

- 5个科技领域；
- 每领域5—8项科技；
- 总计约30项科技卡；
- 水稻、小麦、粟黍、豆类、桑园、果园、药圃和麻葛等首批农业对象；
- 农田—仓储、桑园—丝织、铁矿—铁器、药圃—医馆四条验证产业链；
- 每项技术至少拥有一个明确应用对象和一个可审计效果。

## 21. 验收标准

1. 农业不再只是单一“农业值”，能够建设并经营具体作物产业。
2. 桑园、果园等多年生资产具有成长和毁坏后果。
3. 科研不要求玩家逐日操作实验和作物生长。
4. 原住民世界在没有穿越者时也能研究、改良、传播和失传技术。
5. 穿越者只提供知识线索和优势，不能凭空完成产业。
6. 每项科技效果绑定具体设施、产业、组织或地区。
7. 科技不直接创造人口、土地、物资或财政。
8. 研究和应用消耗真实人才、资金、材料、设施和时间。
9. 生产结果进入真实库存，并参与市场、税收、家庭消费和军需。
10. 生产、建设和科研可以脱离动画与场景运行。

## 22. 操作深度、事实颗粒度与性能边界

完整目标不等于所有对象使用同一种最细粒度，也不等于玩家必须逐日点击每道工序：

- 播种、插秧、除草、炉温、样品和研究阶段可以成为具体工作的真实组成，并由人物、
  时间、工具和材料结算；玩家是否逐项干预由关注和委任决定；
- 普通人物保留永久身份，但不为所有人逐时运行完整科研、生产或战斗AI；
- 牲畜、作物、蚕、粮食和普通材料按能够影响所有权、用途、质量、损耗和历史的最小
  有意义单位保存，不要求无玩法价值的逐个体或逐粒模拟；
- 科研完成不能自动给予全国性百分比，必须经过教学、建设、改造和具体应用；
- 穿越者和现代专业只提供知识线索，不能绕过材料、工艺、组织和时代条件。

每个新领域必须写明最小永久实体、独立ID条件、计量单位、行动者、时间、材料、知识
前置、账本变化、聚合方式和存档方式。汇总缓存与托管只降低计算和操作成本，不能删除
会独立改变所有权、用途、历史、消耗或世界结果的事实。

## 23. 统一设施、住房、岗位与建设蓝图正式规则（LUOYANG-184）

以下规则自 `LUOYANG-184-HISTORICAL-V1` 起成为跨系统合同，后续城市、村庄、庄园、军营和
历史场景必须沿用；本节的“已有原型”仅指洛阳184验证区，不代表全国内容已经填充。
Facility Definition/Profile/Instance、候选目录、五种成长方式及其与产权、职位、皇室、王国和
政治AI的详细交界，以
[`UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md`](UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md)
为准。该设计归并不改变本节已有代码和验证状态，也不把候选目录误报为全国正式内容。

### RULE-LY-001：正式人工建筑必须设施化

所有能够被选择、进入、拥有、控制、建设、运营、破坏或参与世界结算的人工建筑，必须同时拥有
稳定 `FacilityDefinition` 与持久 `FacilityState`。历史宫殿、官署、市场、仓库、学校、军营、城墙、
城门和工坊不得只作为装饰POI或贴图存在。表现层可画装饰，但不得用装饰对象替代世界事实。

### RULE-HOUSING-001：住房容量只按Person计

永久住房容量的唯一单位是 `Person`，一人占一个永久住房槽。`Household` 只保存亲属、共同财产、
照料与家居关系，不再作为第二套容量口径。兵营永久居住槽只允许处于真实现役关系的Person；客栈、
医馆、官署、仓库和工坊的临时床位或值宿不计入永久住房。住房不足时人物仍保留永久身份、当前位置、
家庭和历史，只进入无住房状态，绝不删除、合并或重新随机。

### RULE-JOB-001：设施岗位必须由真实人物承担

岗位以数据驱动的稳定ID定义资格、主要技能、最低能力、位置、权限和容量；任职以永久Person ID引用。
`JobEligibility` 判断能否任职，`JobFit` 只评价合适程度。没有达到最低真实工人数的设施不得进行正常
生产或完整服务；表现层、汇总缓存和委任不能伪造工人。

### RULE-AI-BALANCE-001：建设AI读取事实压力

禁止设定固定“住宅Cell：岗位Cell”配比。玩家与AI共同读取实际在世人口、已住房人数、永久住房空位、
有效劳力、已填岗位、空缺岗位、技能短缺、粮食、治安、财政、土地、道路和威胁，再提出建设、培训、
招募、迁居或停建方案。不同年代与城市可因此形成不同结构。

### RULE-BUILD-002：多Cell建设使用共享蓝图

跨Cell工程必须使用稳定 `FacilityBlueprintDefinition`，至少保存相对Cell、`FacilityDefinitionId`、方向、
道路连接、模块、施工阶段、顺序和元数据。玩家放置、历史初始生成与AI建设共用同一模板和校验器，均须
验证Cell存在、可开发、Owner、占用和道路条件。合法放置只建立预约与分阶段施工事实，不代表瞬间完工。

### 当前原型状态

- 已有原型：184年东汉洛阳在 `HanWorldV1` 同一2000m Cell世界上的173个历史/复原设施、十二座大城门、
  南北宫独立防线、护城壕、20,542名永久人物住房与岗位投影、建设蓝图和Unity验证场景。
- 已有底座：设施、住房、岗位适配、AI压力、蓝图校验和城防通行的纯C#领域合同。
- 待扩展：正式主存档迁移、全国城市初始化、完整施工资源账、完整攻城器械与蓝图UI。

## 24. 行政区、治所、Canonical Place与历史世界状态（ADMINISTRATIVE-SEAT-CANONICAL-PLACE-V1）

本节冻结行政地理与历史状态的跨系统边界；详细Reference见
[`HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md`](HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md)。

- 州、郡、国、尹、属国和县都是`AdministrativeRegion`，表示管辖空间，不天然等于城市、聚落或地图节点；
- `ProvinceSeat / CommanderySeat / KingdomSeat / CountySeat / Capital`是绑定到真实`CanonicalPlace`的行政或政治Role，不是Place类型；
- 一个Place可同时承担多个Seat Role，但只保留一个PlaceId；县与县治、郡名战略标签与真实治所必须分开；
- `HistoricalSeatReference`只初始化直接历史开局或提供参考，`RuntimeAdministrativeSeat`由Government Facility、合法Office/Authority和Controller构成并可自然迁移；
- 历史资料采用`Scenario Snapshot + Major Historical ChangePoint + Inherited State`，不建立126套逐年地图；
- 重大历史事件属于整个世界，玩家不在场时也可在后台结算；表现层是否加载不影响人口、Facility、组织、库存、控制权和行政Role事实；
- 历史事件必须检查运行世界前提并允许Canonical、Variant、Delayed、Prevented或Transformed结果；标准PostState不得强制覆盖已经分歧的世界；
- 如果事件确实发生，其`HistoricalChangePackage`必须作用于同一Cell、Place、Facility、PermanentPerson和Family事实，不能等玩家进入当地才生成结果；
- 当前状态为“已有Reference母版、待运行时实现”：本轮没有修改Save Schema、Unity Scene或正式事件执行器。

现有77个名称正式解释为战略Place/Region显示标签，不保证是77座同层级城市；133 Core Settlements、250重点县和1182 CountyPermanentId继续复用。下一资料阶段可以进行`DEVELOPMENT-PLACE-ROSTER-AND-REFERENCE-READINESS-V1`，但不能由行政级别单独决定开发优先度。

## 25. Development Place Depth、Roster与Wave（DEVELOPMENT-PLACE-ROSTER-V1）

正式入口为
[`HISTORICAL_WORLD_REFERENCE/DEVELOPMENT_PLACE_ROSTER_V1/README.md`](HISTORICAL_WORLD_REFERENCE/DEVELOPMENT_PLACE_ROSTER_V1/README.md)。

`D0 WORLD_BACKGROUND / D1 SIMULATED_PLACE / D2 ACCESSIBLE_PLACE / D3 IMPORTANT_DEVELOPED_PLACE / D4 FULL_DEVELOPMENT_PLACE / D5 FLAGSHIP_LIVING_WORLD`只表示项目准备投入的制作深度，不是历史行政等级、人口等级、城池等级或互斥PlaceType。Settlement、CountySeat、PassArea、HarborSettlement、BattlefieldArea等物理与历史Role可以叠加；非城市Place可以因战争、交通和系统验证价值达到D4。

V1正式Roster为72个专项地点：D5=1、D4=15、D3=33、D2=23、D1=0。D1为0仅表示没有把普通模拟地点塞入专项制作名册；其余统一世界地点仍以D0/D1事实与Simulation存在。77 Strategic Labels、133 Core Settlements、105治所和1182县都不是Roster替代品，也不会自动抬升开发深度。

`DevelopmentPriority(P0—P4)`与`DevelopmentWave`独立于Depth和Historical Importance。Wave 0为`LUOYANG_HULAO`：洛阳D5、虎牢D4、函谷D3共同作为开发工作包，但保持独立Place/参考。洛阳已经以Gate A=`GO_WITH_BLOCKERS`通过正式Implementation Readiness Review并先进入Core集成；虎牢、函谷仍需按Manifest关闭Cell、分期Facility、人口或军力范围阻断后再进入Wave 0B。

历史状态支持逐Place采用S0—S4与H0—H5分级，不为所有Place机械复制13个完整Scenario。当前状态为“Roster、Readiness、Manifest与Wave已有正式V1；洛阳运行时扩展已通过有阻断门禁”。下一阶段停止扩大全国地点资料库，进入`LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1`。

## 26. City Development Pack与升档协议（CORE-CITY-DEVELOPMENT-PACK-V1）

正式入口为
[`HISTORICAL_WORLD_REFERENCE/CITY_DEVELOPMENT_PACKS/README_CORE_CITY_DEVELOPMENT_PACKS.md`](HISTORICAL_WORLD_REFERENCE/CITY_DEVELOPMENT_PACKS/README_CORE_CITY_DEVELOPMENT_PACKS.md)。

City Development Pack是把某个`CanonicalPlaceId`的历史身份、分期状态、人口层、城市形态、Facility参考、人物在场、Clan/Family候选、产业农业、交通腹地、军事、Scenario、ChangePoint、来源与未知项整理为可审计开发输入的资料合同。它只引用现有Canonical母库，不复制第二套世界事实，不自动生成Cell、Facility、PermanentPerson、FamilyOrganization、FamilyCenter或人口。

首批10城已经形成标准Pack：洛阳为`DEVELOPMENT_READY`，长安、邺、许昌、成都、襄阳、江陵、建业、合肥和南郑为`READY_WITH_MODELED_GAPS`。汉中是战略显示名，落到物理`CanonicalPlaceId=place.han140.yizhou.hanzhong.nanzheng`。成都既有`major_city_timeline`错误交叉引用已隔离，未把南阳郡同名县数据写入成都Pack。

72项Roster不是永久白名单，D0/D1地点未来允许补包和申请升档；但`Pack Ready ≠ DevelopmentDepth自动变化 ≠ Runtime已实现`。以后用户要求把任意地点做细时，必须先解析既有CanonicalPlace、建立或升级Pack、审计来源和未知项，再由用户或正式开发计划决定是否改变D级。升档不得删除、替代、合并或重随机已有Place、Cell、人物、人口、Facility和组织事实。

洛阳开发门`LUOYANG-184-DEVELOPMENT-READINESS-REVIEW-V1`已经完成；当前只推进其冻结的历史人物—家族集成范围，未获得新的明确计划前不自动扩充第二批城市Pack。

## 27. Development Place 完整参考包与 T1—T4 当前合同（FDRP V1）

当前权威入口为 [`HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md`](HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md)。本合同更新第 25、26 节中的现行术语，但不删除旧工作簿和报告：旧 `D2/D3/D4/D5` 仅作为历史映射保留，后续开发使用 `T1/T2/T3/T4`，数量仍为 23/33/15/1，72 个地点和既有 Wave 均未改变。D0/D1 不再属于特殊 Development Place 档位，名册外地点没有 T0。

必须分别记录 `DevelopmentTier`、`ReferencePackCompleteness` 和 `RuntimeImplementationStatus`。所有 T1—T4 地点采用同一套 25 模块完整参考标准；“完整”表示问题已经审计，可以诚实结论为 `UNKNOWN`、`NO_EVIDENCE` 或 `NOT_APPLICABLE`，不要求伪造肯定答案。72 份参考包已经建立，但本轮只形成资料合同：除洛阳已有部分原型外，其余地点不因此成为运行时 Place、Facility、人口、家庭中心或历史事件状态。

官渡、街亭、五丈原、赤壁、祁山等必须区分永久聚落、永久地理地点、事件依赖复合体、战场区域与未解析空间。战役名望不等于永久聚落；事件设施只有在事件真实发生时才通过统一 Facility 类型与建造规则建立。直接进入较晚历史剧本可以初始化史实后状态，连续世界则必须尊重实际分支，不用 Canonical 未来覆盖玩家和 NPC 已经形成的事实。

## 28. 洛阳184人物—工作—生产—消费闭环（V70）

正式任务与证据入口为
[`TASK_LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1.md`](TASK_LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1.md)和
[`HISTORICAL_WORLD_REFERENCE/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1/`](HISTORICAL_WORLD_REFERENCE/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1/)。

当前状态为“已有可运行原型”：受保护的400,000个PermanentPerson、80,899个Household和2,084个Facility已经通过只读适配器进入同一生活经济运行时；Person当前活动决定工作占用，设施受最低工人、真实输入、周期和仓容约束，135条正式农业记录支持成熟度、80%早收、收割、留种和再播种，家户从真实库存消费并形成短缺。V70只把小型摘要和派生检查点引用放入WorldState，40万逐人明细不内联进主存档，也不写回受保护初始化包。

365日证据表明洛阳当前物理供给严重不足：年末粮食为0，全部80,899户经历短缺并输出`SUPPLY_REGION_DEPENDENCY`。因此下一优先候选改为洛阳外围供应区与农业腹地物化；市场、商业和物流深化在真实外围产出与运输节点建立后继续。本状态不表示全国生活世界、成熟市场、税收或战争已经实现。

## 29. 智能人口驱动世界与条件历史事件合同（World Schema V71）

《群雄志：仕途》的统一世界采用“历史快照初始化 + 人口/需求/资源驱动的智能自演化 + 条件式重大历史事件冲击”运行模型。历史有惯性，但未来不是注定的。

正式入口为
[`TASK_WORLD_INTELLIGENT_POPULATION_DRIVEN_SIMULATION_AND_HISTORICAL_EVENT_CONTRACT_V1.md`](TASK_WORLD_INTELLIGENT_POPULATION_DRIVEN_SIMULATION_AND_HISTORICAL_EVENT_CONTRACT_V1.md)
及其[交付目录](HISTORICAL_WORLD_REFERENCE/WORLD_INTELLIGENT_POPULATION_DRIVEN_SIMULATION_AND_HISTORICAL_EVENT_CONTRACT_V1/)。

当前状态为“已有可运行底座”：`WorldSignal → DecisionContext → ActionIntent → Validation → Command/Transaction/Event` 已形成统一合同；规则、效用、历史约束、稳定随机与神经网络适配器共享同一验证门。AI只能提出意图，不能绕过库存、Cell、设施、权限、人员和正式命令账。全国县域供应关系继续只是校准资料，实际采购、调拨、运输和军需必须复用既有市场、民运和军需实体。

重大历史事件已支持结构化非时间前提、Canonical/Variant/Delayed/Prevented/Transformed结果、离屏应用与幂等ChangePackage；年份只开启观察窗口，不能单独触发重大事件。V71保存World Decision Agent、LOD调度、策略/模型版本、决策序列、事件规则版本和已应用操作ID。HOT/WARM/COLD只改变结算频率，不得删除、合并或重随机永久人物及其他世界事实。

本阶段未宣称全世界已经自主运行，也未宣称神经网络模型已经训练或上线。下一阶段仍以永久人物HOT/WARM/COLD调度和真实动作执行接入为主，不另建第二套世界模拟器。

## 30. 智能决策 Policy 与 Simulation Arena（World Schema V72）

正式入口为
[`TASK_WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1.md`](TASK_WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1.md)
及其[交付目录](HISTORICAL_WORLD_REFERENCE/WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1/)。

当前状态为“已有可运行底座与实验证据”：Household、FamilyOrganization、Merchant、Settlement、Government 已接入候选动作、人格/目标权重、Rule、Utility、Randomized Utility 和 Neural Scorer；所有策略只能选择意图，仍须经过统一 Validator 与正式动作执行。离线训练的 12 维小型 MLP 已形成版本化模型资产，Runtime 禁止在线学习，模型缺失或非法时按 Neural→Utility→Rule→NoAction 安全回退。

Simulation Arena 已完成 10 Benchmark × 100 Seed × 4 Policy 的 4,000 次合同级运行，并输出 176,000 条决策、检查点、独立决策/事件 Trace 和 189/190 条件事件多分支证据。V72 只因新增 Agent Profile、Goal、Model 与有界 DecisionMemory 持久字段而升级，并提供 V71→V72 顺序迁移。

本证据没有证明 400K 洛阳完整性能、成熟 Facility/产业/贸易/政府差异或全国 AI 已完成。下一全局候选为 `WORLD-HOT-WARM-COLD-PERMANENT-PERSON-SIMULATION-V1`：先对 400K 洛阳执行混合热度与全 HOT 压力门禁，再扩全国；Rule/Utility 仍是生产基线，Merchant Neural 只作为版本化候选实验。

## 31. 全国统一空间基础 V1（ONE WORLD / ONE GLOBAL GRID）

正式合同与验收入口为
[`GLOBAL_SPATIAL_FOUNDATION_CONTRACT_V1.md`](HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1/GLOBAL_SPATIAL_FOUNDATION_CONTRACT_V1.md)
、[总报告](HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1_REPORT.md)
和[空间起点摘要](HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1/SPATIAL_ORIGIN_SUMMARY.md)。

当前状态为 `GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN`：全国唯一 CRS 继续采用
`hanworld.albers.china.v0`，唯一原点为 `(-3417344.395965772, 6199580.451937504)`；
该点严格表示规则母格网及 Cell(0,0) 的西北/左上角，行号由北向南增加，列号由西向东增加；
3314×2176、2000m、7,211,264 个 0 基 row-major Cell 及其既有 ID 全部保留。后续
[`WORLD-REGION-CELL-BOUNDARY-AND-TECHNICAL-BLOCK-SEMANTICS-CORRECTION-V1`](TASK_WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1.md)
已把 Region 与技术分块语义正式收口：16×16 的 28,288 个既有 ID 和索引行为继续保留，但当前含义是
`SIMULATION_AGGREGATION_BLOCK_16`，不是世界事实、Terrain Tile 或 Streaming Unit。旧地图二进制中的
64×64 继续仅是 `STORAGE_COMPRESSION_ONLY`。该语义修正不涉及 Cell、Place、Facility、Person、Force
或存档重编号。

`HENAN_YIN_REGION` 是第一块地图生产 Region，其权威范围严格由 58,368 个
`IncludedGlobalCellIds` 决定；当前矩形和 228 个 16×16 索引只是已有数据形状与派生技术索引，不构成
未来 Region 必须按完整 16×16 Block 划分的规则。Region 边界由成员 Cell 外边派生，允许阶梯状，不切
Cell，不建立 Seam/Border/Transition Cell，也不需要第二套权威 Polygon。河南尹行政 Overlay 的 7,763
个 Cell 继续是独立历史行政信息。Region Local、技术块 Local 与 Unity Floating Origin 都只是可逆转换
或表现坐标。

空间骨架、Region Cell 边界合同、DEM 全局采样合同、河路地点锚点与转换服务保持冻结。后续
`HAN-WORLD-NATURAL-TERRAIN-AND-LANDSCAPE-BASEMAP-V1` 已完成 4×4、8×8、16×16 真实 DEM 与
3×3/5×5 Unity 基准，Terrain Tile 因此冻结为 8×8 Cell（16km）；24×24 Cell Streaming Unit 仅为
V1 暂定运行参数。该完成状态只代表全国自然地貌基线，不代表河南尹最终高精 Terrain、汉代河道精修
或全国最终美术已经完成。

## 32. 全国自然地貌统一底图 V1（HAN WORLD NATURAL BASEMAP）

正式入口为[实施与验收报告](HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_TERRAIN_AND_LANDSCAPE_BASEMAP_V1/HAN_WORLD_NATURAL_TERRAIN_AND_LANDSCAPE_BASEMAP_V1_REPORT.md)。

- 已有原型：Unity `HanWorldNaturalBasemap` 场景在不加载旧背景图时显示全国低 LOD 自然地形，并可切换河南尹/洛阳 3×3 Terrain Tile 区域视角；河流和植被为批处理 Mesh。
- 已有底座：真实 `HanWorldV1` DEM、233 个许可河流参考 Feature、稳定 Surface ID、共享边 Terrain Generator、Global↔Terrain↔Cell 绑定和 Floating Origin 往返。
- 已定方案：Terrain Tile = 8×8 Global Cell（16km）；全国 112,880 个 Tile 是可派生表现索引，不是世界身份或 GameObject 清单。
- 暂定方案：Streaming Unit = 24×24 Cell / 3×3 Terrain Tile；后续压力测试可调整，不得改写 Global Cell 或 Terrain Tile 身份。
- 待研究：高精 DEM、汉代河道与湖岸复核、洛水许可折线、季节/天气/雾、最终水墨材质、河岸湿地细化、GPU 时间戳和大范围异步加载。
- 兼容规则：16×16 继续只用于模拟聚合，64×64 继续只用于二进制压缩；Region 继续是完整 Global Cell 成员集合。
- 下一阶段允许进入“河南尹高精自然地形与历史水系 V1”，但不得把 2km 全国底图误报为城市近景最终资产。

## 33. 东汉全国自然地图视觉表现 V2

正式入口为 [`TASK_HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2.md`](TASK_HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2.md) 与 [`HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2/HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2_REPORT.md`](HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2/HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2_REPORT.md)。当前状态为“已有可操作原型与正式验证证据”，最终标记是 `HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2_PLAYABLE_WITH_ART_LIMITS`，不得写成最终美术 COMPLETE。

V2 继续使用唯一 `hanworld.albers.china.v0`、Global Origin、3314×2176、2000m Global Cell、7,211,264 Cell ID、河南尹 58,368 成员 Cell 和冻结的 8×8 Terrain Tile。WORLD 为同源 DEM 的连续远景网格；REGION 为连续 2km Cell 显示网格，3×3 Terrain Tile 只承担驻留、碰撞和未来流式边界，不重复绘制地表。格网和行政信息只属可切换信息层；旧背景图不参与自然地图事实或正式截图。

当前河流已具备平滑中心线、宽度变化和河岸，森林已具备连续密度与合并批次，但树冠、河岸细节、2km 近景低多边形、全国调色与抗锯齿仍是明确美术债。14 张 Game View 截图只是 Golden 候选，下一阶段必须等待用户确认自然地图方向。

## 34. 东汉全国自然地图美术方向与渲染 V1

正式入口为 [`TASK_HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1.md`](TASK_HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1.md) 与 [`HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1/HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1_REPORT.md`](HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1/HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1_REPORT.md)。当前状态是 `HAN_WORLD_ART_DIRECTION_V1_CANDIDATES_READY`。

同一真实 DEM、Global Cell、河流、森林、Floating Origin 和固定 Camera 已产生 STYLE A 半写实自然、STYLE B 国风半写实战略沙盘、STYLE C 强化战略可读性沙盘三套可切换候选，并有18张独立 Game View、3张三联图和12份工作簿证据。三套只改变 Presentation Profile；112,880个8×8 Terrain Tile、河南尹58,368 Cell、洛阳及人口设施事实均未改变。

本状态只表示“候选可供用户审美决策”，不是全国最终美术完成。`USER_SELECTED_STYLE=PENDING`，Codex 推荐 STYLE B 但无权代替用户决定；全国风格推广、河南尹高精 Terrain、洛阳城市/城墙/建筑/道路全部保持 `BLOCKED_PENDING_USER_APPROVAL`。

## 35. 《中华三国志》启发 Style D 地图原型 V1

正式入口为 [`TASK_HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1.md`](TASK_HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1.md) 与 [`HISTORICAL_WORLD_REFERENCE/HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1/README.md`](HISTORICAL_WORLD_REFERENCE/HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1/README.md)。

本阶段在用户明确要求下增加第四套 `STYLE_D_ZHONGHUA_SANGUOZHI_FUSION` 候选，不改变此前 A/B/C 的同源比较合同。Style D 在同一 DEM、Global Cell、河流、森林与 Floating Origin 上派生坡度、局部起伏、脊、谷、山体、平原、连续森林面和河谷权重，并以 Presentation UV 通道驱动 Shader；不建立第二套地图、不复制外部地图或贴图、不修改任何历史地理事实。

当前状态为 `STYLE_D_ZHONGHUA_SANGUOZHI_FUSION_PROTOTYPE_READY`，已有10张固定 Game View、14份工作簿、EditMode 2/2 与 PlayMode 1/1。候选仓库固定 HEAD 的 API 静态审计已经完成，但完整 Git clone 因 GitHub 443 连接失败/重置硬阻断，故不得声称 `ZHONGHUA_SANGUOZHI_SOURCE_RESEARCH_V1_COMPLETE`。`USER_SELECTED_STYLE` 仍为 `PENDING`，全国推广、河南尹高精与洛阳城市继续阻断。

## 36. 洛阳全城建筑最终资产分批审模状态

正式清单入口为
[`TASK_LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1.md`](TASK_LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1.md)。
54个稳定Asset Variant替换槽位、2,084项Facility映射和P0/P1/P2/P3优先级继续冻结；最终艺术替换不得
改变Facility、Model、Asset Variant、Global Cell、建设权限、模拟或Save Schema。

首批南宫、明堂、广阳门、北宫南门已经完成用户接受、真实FBX回读和
`FinalArtApproved=true`激活。第二批原生源入口为
[`TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md`](TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md)：
按最低剩余P0评审序号1/2/3/5选择北宫、永安宫、太学、辟雍，已完成4个项目原创原生Prefab、4个
真实FBX、三级LOD、稳定锚点、运行时回退和来源哈希清单。

第二批审图输入为
[`TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1.md`](TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1.md)：
固定4件×3角度相机合同，生成一张总览、十二张1600×1000 Game View与四张3000×900决策板；
PreviewOnly审模实例改用平缓评审Cell后，太学与辟雍主体地形线遮挡项已关闭。决策板中的
`PENDING/false`保留为用户决定前的历史证据。

用户随后对北宫、永安宫、太学、辟雍明确回复“全部接受”。当前正式入口为
[`TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`](TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md)，
状态为
`LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`。四项现均为
`FinalArtApproved=true`，真实Prefab/FBX、三级LOD、材质、锚点、包围盒、零Collider和来源哈希已
通过门禁；运行时只有真实Prefab加载成功才报告批准，程序回退实例仍为false。全工程编译、定向
核心1/1、接受/回退与FBX回读EditMode各1/1、真实Prefab五视图PlayMode 1/1和最密549 Facility
批处理PlayMode 1/1通过。

第三批原生源血统入口为
[`TASK_LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md`](TASK_LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md)：
按最低剩余P0评审序号6/7/8/9选择灵台、太仓、武库、濯龙园，已完成4个项目原创原生Prefab、4个
真实FBX、三级LOD、稳定锚点、运行时独立身份与回退、来源哈希清单，以及一张总览和四张1600×1000
Unity Game View。

用户随后在该五视图审模上下文中明确回复“接受”，按四件全部接受登记。当前正式入口为
[`TASK_LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`](TASK_LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md)，
状态为
`LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`。四项现均为
`FinalArtApproved=true`，运行时只有真实Prefab加载成功才报告批准，程序回退仍为false。全工程编译、
定向核心1/1、FBX回读与接受/回退EditMode各1/1、真实Prefab五视图PlayMode 1/1和最密549 Facility
批处理PlayMode 1/1通过。

洛阳54个最终资产槽位中，首批、第二批、第三批各4项，共12项先行最终激活。用户随后以“给出下一步
任务书，并执行”单独授权有限第四批候选生产；候选血统入口为
[`TASK_LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md`](TASK_LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md)。
本批跳过首批已激活的评审序号10广阳门，选择最低剩余P0序号11/12/13/14：谷门、津门、开阳门、
旄门。
四件项目原创原生Prefab和四个Unity回读FBX已经完成，具有三级严格递减LOD、稳定放置/内外通行
锚点、权威朝向、真实Prefab优先与程序回退；五张1600×1000 Game View已形成候选审图输入。

用户随后在该上下文中表示“上一个接受”，按第四批四件全部接受登记。当前正式入口为
[`TASK_LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`](TASK_LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md)，
状态为
`LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`。四项静态
`FinalArtApproved=true`；运行时只有真实Prefab成功加载才报告批准，程序回退实例仍为false。至此
首批至第四批共16/54项最终激活，剩余38项未最终批准；第五批未启动。来源清单哈希与本轮编译、
核心、Unity和批处理验收结果以第四批最终激活任务书的执行记录为准。

用户随后明确要求“直接开发剩下的38个，不用审批直接接受”，因此另建
[`TASK_LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1.md`](TASK_LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1.md)
一次关闭评审序号`15—21、23—53`。本轮冻结8个P0、10个P1、14个P2和6个P3槽位，影响
2,068项Facility；与既有16项合计覆盖54个稳定替换槽位和2,084项正式Facility。38个项目原创
Unity原生Prefab、38个真实FBX、22个材质、12个网格、三级LOD、稳定锚点和零Collider已完成
Unity重载/回读，来源清单覆盖240个源/元数据文件并逐项冻结SHA-256。运行时真实Prefab加载时批准
为true，程序回退强制为false；全54项真实Prefab图形场景与最密549 Facility批处理均通过定向门禁。
当前状态为
`LUOYANG_REMAINING_38_USER_PREACCEPTED_NATIVE_PREFAB_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`，至此
洛阳最终资产槽位为`54/54`激活、剩余`0`。这不代表考古复原、手绘/PBR贴图、室内、碰撞、导航或
建筑动画终稿完成，也不改变Facility、建设、模拟或存档事实。

## 37. 洛阳实际全城构图与地形融合 V1（2026-08-27）

正式入口为
[`TASK_LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1.md`](TASK_LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1.md)。
本阶段不再增加最终资产槽位，而是在同一2,000米Global Cell世界中，为2,084项Facility及其54项最终
Asset Variant逐项建立确定性Visual Local Anchor。构图形成宫城政务、里坊住宅、市肆工坊、城防、
交通水利和农业资源六个Presentation构图区；道路、沟渠与墙体从相邻真实Facility推导中心线连接，
普通建筑朝向最近的真实道路Facility。局部偏移不超过420米且不创建SubCell。

现有最密24×24窗口的549项Facility使用偏移后的全局坐标重新采样同一Terrain高度，并继续进入原
8×8空间＋材质合批路径。该状态只表示全城构图合同和目标窗口运行时接地已实施；全国自然Style仍
未冻结，洛阳高分辨率DEM、碰撞、导航、桥门通行、室内、损毁动画、外围供给实体化及发布级Streaming
仍不在本任务完成声明内。

当前状态为
`LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`。
六区计数为农业资源746、城防184、市肆工坊258、宫城政务100、里坊住宅324、交通水利472；524项
道路/沟渠/墙体保持Cell中心线。定向核心1/1、目标EditMode 3/3、目标图形PlayMode 1/1和既有549
批处理图形回归1/1通过；完整回归未运行。

## 38. 洛阳建筑选择、碰撞代理与道路通行图 V1（2026-08-28）

正式入口为
[`TASK_LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1.md`](TASK_LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1.md)。
本阶段为2,084项Facility建立独立选择代理合同，CITY最密窗口只常驻549个轻量BoxCollider触发器，
不向54项最终美术Prefab添加Collider。射线命中可回读稳定Facility ID并显示选择高亮，切回WORLD后
交互根和选择状态全部清理。

静态通行图包含359个道路节点、18个城门/宫门/军门节点和2个桥节点。源数据严格四邻接形成334条
道路边和29个连通片；当前以28条`Provisional=true`临时边连接断点，并以20条桥门接入边完成379节点、
382边的确定性可查询图。临时边不表示史实道路，后续道路数据细化必须替换；本V1也不等于角色尺度
NavMesh、实体阻挡、城门开闭、桥梁损毁或攻城通行。

当前状态为
`LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`。
全工程编译、定向核心1/1、目标EditMode 3/3与目标图形PlayMode 1/1已通过；图形证据新增像素方差门禁，
Null Graphics纯背景图不能被登记为通过。最终统一门禁为编译通过、核心1/1、EditMode 3/3、
`git diff --check`通过；既有全城构图图形回归1/1也通过。完整回归未运行。

## 39. 洛阳身份化道路连接与动态门桥通行 V1（2026-08-28）

正式入口为
[`TASK_LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1.md`](TASK_LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1.md)。
上一阶段379节点/382边基础图保持历史兼容；当前运行时新增402边精化层：334条严格四邻接道路边、
28条带稳定ID/来源边/逐格折线/玩法重建证据的身份化连接，以及20个门桥各两条、合计40条道路接近边。
28条连接统一标记`historical_evidence.gameplay_reconstruction`、Cell精度和
`ClaimsHistoricalExactness=false`，不得写成汉代精确道路。

20个城门/宫门/军门/桥现具有纯C# Domain会话态，支持开放、关闭、受损和毁坏；状态变更使用稳定
原因ID、单调时间和Revision，确定性Dijkstra会跳过关闭/毁坏节点并提高受损节点代价。CITY用青色、
橙色和红/橙黄状态层显示，549个独立选择Trigger和WORLD清理保持不变。

当前状态为
`LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`。
目标核心1/1、EditMode 3/3、图形PlayMode 1/1和非空白截图已经通过；最终统一门禁与相关图形回归
以任务书完成记录为准。当前门桥状态没有进入WorldState、命令/事件账、快照或迁移，明确不跨读档；
人物尺度NavMesh、守军/权限/围城、桥梁载重/洪水/维修和动画门扇均未实现。

## 40. 洛阳门桥 WorldState、命令事件与存档 V1（2026-08-28）

正式入口为
[`TASK_LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1.md`](TASK_LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1.md)。
本阶段将上一节20项门桥Domain会话态提升为V74正式世界事实，复用M25-P7持久命令、批次结果、
事务摘要和事件Outbox。一个显式初始化命令冻结完整Facility/Definition清单并原子建立20项开放
状态；逐门桥转换命令保存expected revision、目标状态和稳定原因，同门桥同Revision的同批冲突
在写入前拒绝。

V73→V74迁移只建立空集合，不把此前Presentation会话倒推为历史事实。地图控制器绑定正式
`WorldState`后只读取只读投影，并通过命令改变状态；未绑定审图模式继续保留会话态兼容。当前
实现不包含守军/权限/围城、桥梁载重/洪水、维修材料/劳动工单、门扇/损毁动画、人物尺度NavMesh
或城外供应道路。

当前状态为
`LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`。
最终全工程编译、定向核心3/3、目标EditMode 5/5、正式世界绑定图形PlayMode 1/1、上一门桥图形
回归1/1、全城构图图形回归1/1和`git diff --check`均通过；以上为目标验收，不替代完整回归。

## 41. 洛阳门桥守军权限、战斗损坏与真实维修 V1（2026-08-28）

正式入口为
[`TASK_LUOYANG_PASSAGE_GUARD_DAMAGE_AND_REAL_REPAIR_V1.md`](TASK_LUOYANG_PASSAGE_GUARD_DAMAGE_AND_REAL_REPAIR_V1.md)。
本阶段把 V74 的无权限门桥转换收紧为 V75 可审计操作：每项启用的门桥显式绑定既有
`Facility` Controller、真实组织、真实 `Army`、主将和具体永久 `Person`。建立守军后，正常开闭
只接受控制组织领袖或守军 Army 级主将；地图表现层身份不能作为权限来源。

战损不创建战斗，只消费既有 `BattleRecordState`：战斗必须发生在门桥所属地点、守军必须是防守方、
攻击军必须敌对且由其主将确认。损坏记录追加保存完整度开闭值、门桥 Revision、命令、事务与事件；
同一战斗不能重复损坏同一门桥。维修复用 V73 既有 `FacilityConstructionProjectState(Repair)`、
组织库存中的木料/铁料批次、带工程来源的库存事务和具体人物每日劳动。门类为8木料、2铁料、
960分钟、至少2日和100钱；桥梁为12木料、2铁料、1,440分钟、至少3日和100钱。完工恢复
Facility与10,000完整度，但门桥保持关闭，必须另发守军授权命令开启。

V74→V75只建立空守军、战损和维修集合，并把旧库存事务新增工程来源字段置空；不倒推旧守军、
战斗、损坏程度、材料或劳动历史。当前代码、全工程编译、定向核心闭环与建设回归已经通过；
受限工作区内的首次Unity尝试在创建启动日志前超时，但同一安全脚本随后在工作区外重跑通过：
EngineSmoke通过、目标EditMode 1/1通过、相关图形PlayMode 1/1通过。当前状态为
`LUOYANG_PASSAGE_GUARD_DAMAGE_AND_REAL_REPAIR_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`；
以上是定向验收，不替代完整分组回归。完整攻城、攻城器械、桥梁载重/洪水、门扇/瓦砾动画和人物尺度 NavMesh仍未实现。

## 42. 洛阳门桥状态化表现与人物尺度通行阻断 V1（2026-08-28）

正式入口为
[`TASK_LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1.md`](TASK_LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1.md)。
本阶段不增加新的世界事实或存档字段，只从 V75 的20项门桥状态、真实完整度和活动维修工单生成
只读、确定性人物通行投影。`closed`与`destroyed`在CITY当前驻留窗口启用独立非Trigger
`BoxCollider`；`open`与`damaged`关闭阻断，并继续服从既有路径规则，其中受损通行代价保持1,800‰。
活动维修只增加脚手表现，不自行开闭门桥。

每项当前驻留门桥最多建立一个可复用运行时实例，使用共享低多边形网格表现开放门叶、关闭门叶、
受损残片、毁坏瓦砾和维修脚手；朝向由既有两侧道路接近边确定。所有对象独立于54项最终Prefab、
FBX、LOD、锚点和`FinalArtApproved`，绑定/解绑、命令刷新、会话重置和WORLD切换复用同一生命周期，
不会把Presentation状态写回世界账。

当前状态为
`LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`。
全工程编译、定向核心6/6、目标EditMode 1/1、目标图形PlayMode 1/1、正式世界绑定PlayMode 1/1、
上一交互导航图形回归1/1和`git diff --check`均通过；门桥近景已人工确认可同时辨认门楼、道路接近线、
选择框、关闭标记和闭门构件。以上为定向验收，不替代完整分组回归，也不表示完整NavMesh、角色动画、
室内行走、最终门桥动画、围城或攻城器械已经实现。全局推荐顺序仍由0E节决定，本洛阳连续任务不自动
改写M26玩家玩法主线。

## 43. 洛阳可点击道路步行与动态门桥阻断竖切片 V1（2026-08-28）

正式入口为
[`TASK_LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1.md`](TASK_LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1.md)。
本阶段继续只读复用 379 节点、402 边精化道路图和 V75 门桥状态，建立一名玩家关注范围人物的 CITY
步行表现。Domain 冻结普通道路 18m、玩法重建连接 12m、城门 12m、桥梁 8m、人物净空半径 0.45m
和步速 1.35m/s；相同稳定角色、起终节点和门桥状态产生相同路线、侧移、距离与预计时长。

CITY 当前驻留窗口建立一名低多边形人物、非 Trigger CapsuleCollider、亮黄色当前路线和洋红目标；
右键落点或显式目标只吸附正式道路/门桥节点，左键 Facility 选择保持不变。移动中的必要门桥关闭或
毁坏后，同一刷新周期取消路线；启用的门桥阻断体仍是最终碰撞安全门。地图的 2km Cell 与角色近景
可读性存在表现比例差异，人物和路线带的画面尺寸不得解释为 1:1 历史测绘。

当前状态为
`LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`。
本任务不创建或复制 PermanentPerson，不改变正式 Person Location、世界时间、体力或旅行事实，不升级
V75，也不保存逐帧坐标或路线。它只证明单关注角色的点击步行、稳定侧移与动态门桥停止，不代表全城
NavMesh、室内、多人物 RVO/拥堵、最终角色 FBX/动画或 M26 正式旅行命令已经完成。全局推荐顺序仍由
0E 节决定，本连续洛阳任务不自动改写 M26 玩家玩法主线。

## 44. 洛阳正式玩家人物移动与世界结算 V1（2026-08-28）

正式入口为
[`TASK_LUOYANG_FORMAL_PLAYER_MOVEMENT_WORLD_SETTLEMENT_V1.md`](TASK_LUOYANG_FORMAL_PLAYER_MOVEMENT_WORLD_SETTLEMENT_V1.md)，
最终门禁见
[`LUOYANG_FORMAL_PLAYER_MOVEMENT_V1_ACCEPTANCE_REPORT.md`](LUOYANG_FORMAL_PLAYER_MOVEMENT_V1_ACCEPTANCE_REPORT.md)。
本阶段把第43节只读演示步行升级为 V76 正式世界行动：`WorldState.PlayerPersonId` 继续作为唯一受控人物
引用，`PlayerSession` 只读解析现有 PermanentPerson；人物的 Settlement、Global Cell、Facility、
体力和既有 `Provisions`，402条道路状态、门桥状态、固定路线快照及Segment边界进度共同进入世界账。

点击现在先生成持久 Movement Command；Simulation 使用当前人物、起点、道路和门桥事实重新规划并
计算时间、体力与口粮，然后通过既有 `WorldSimulator` 推进共享世界时间并提交人物位置。Unity 仅播放
已经成功提交的路线，未绑定正式世界的旧人物仍只作为审图测试工具。道路、城门或桥梁在下一Segment前
失效会产生 RouteInvalidated 和 MovementInterrupted；V75→V76 不从旧演示坐标虚构正式位置。

当前状态为
`LUOYANG_FORMAL_PLAYER_MOVEMENT_WORLD_SETTLEMENT_V1_ACCEPTED`。全工程编译、目标核心 11/11、冻结完整
核心 747/747、Unity ProjectLoad、目标 EditMode 11/11、图形 PlayMode 4/4、三次相同重放哈希及
`git diff --check` 均通过。两个多年确定性用例经明确分类后使用 900 秒专属上限，分别约 503 秒与 502 秒
通过；其余核心和 Unity 继续使用 300 秒上限。一次可选生活证据刷新另行暴露既有食品守恒差额，未由本次
移动引入且未在本任务越权修复。固定下一阶段是“洛阳人物尺度近景地图与局部导航 V1”，而不是继续
扩充移动功能、建筑资产、NPC 群体寻路或外围供应区。

## 45. 洛阳人物尺度近景地图与局部导航 V1（2026-08-29）

正式入口为
[`TASK_LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1.md`](TASK_LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1.md)，
当前门禁见
[`LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1_ACCEPTANCE_REPORT.md`](LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1_ACCEPTANCE_REPORT.md)。
本阶段不建立第二张洛阳或微型Simulation Cell，而是泛化V68/M26-P5B既有城镇坐标与占地合同，将
同一2km Global Cell内的正式Facility投影为人物尺度Anchor、Footprint、Access与导航拓扑。
正式Road、Gate、Bridge和Facility状态继续由世界账唯一持有；Local Graph实时读取状态，Unity只
表现已经由Domain规划和Simulation结算的路线。

V77为同一Person Location增加LocalSpace、Local Anchor及厘米整数坐标，并让既有持久
`MovePersonCommand`保存局部路段快照；V76迁移只标记战略精度，不虚构局部位置。全城派生计划包含
5,980 LocalSpace、2,084 Facility Capability/Footprint、1,580 Access Point、1,959节点、1,976边、
4,920个连续跨Cell Transition，18项Gate-type Facility和2座Bridge均映射。3×3表现Streaming只
装卸地形、道路Mesh/Collider、阻挡占地与点击代理，不创建或删除永久人物、设施或库存。

当前状态为`LUOYANG_HUMAN_SCALE_LOCAL_MAP_V1_ACCEPTED_PRESENTATION_SCOPE`：全工程编译、专项核心、
完整核心774/774、受控Unity EditMode 3/3、图形PlayMode 1/1和差异检查均通过，原`blocked/125`
环境门禁已解除。LocalSpace、Anchor、Footprint、入口、近景几何与3×3 Streaming正式保留；后续
Cell Traversal任务已明确将LocalNav图降为表现/旧V77兼容资料，不再把它作为跨Cell正式路线权威。

## 46. 洛阳人物尺度 Cell 四向通行、正式移动与近景表现 V1（2026-08-29）

正式入口为
[`TASK_LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1.md`](TASK_LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1.md)，
验收见
[`LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1_ACCEPTANCE_REPORT.md`](LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1_ACCEPTANCE_REPORT.md)，
迁移审计见
[`Evidence/LuoyangCellTraversalV1/existing-spatial-audit.md`](Evidence/LuoyangCellTraversalV1/existing-spatial-audit.md)。

本任务坚持一个战略格等于一个Cell、一个Cell最多一个Facility占位；Road、Alley、Gate和Bridge也是
占Cell的Facility。每个Cell固定拥有四个潜在端口，正式规划同时检查相邻反向端口、内部拓扑、人物/
载具能力、建筑出入规则及正式道路/门桥动态状态。建筑可以作为目的地进入，但不得成为穿楼捷径；
正式人物距离来自Traversal Metric，不以`CellCount × 2000m`或Unity坐标作为结算权威。

洛阳5,980/5,980 Profile和2,084/2,084 Facility全部覆盖。Access审计以现有道路为依据：已有道路
正面的7个仓储仓库、4个公共官仓和7个坞堡使用`RoadRequired`；37个商业仓库、10个仓储仓库、
28个公共官仓和1个仓储官仓因正式数据无道路正面而保持`Optional`，没有凭空补路。V77路段字段已
足以表达新CellRoute，因此不升级存档版本；旧V77路段条件继续兼容。

当前状态为`LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1_ACCEPTED`：全工程编译、
CellTraversal专项核心8/8、洛阳局部移动专项17/17、固定指纹完整核心774/774、Unity EditMode 3/3、
图形PlayMode 1/1、性能边界和差异检查全部通过，Introduced Regression为0。固定下一阶段是食品
库存守恒差额RCA与修复；其后才进入洛阳外围供应区与城市物流V1。

## 47. 正式食品库存守恒差额 RCA 与长期生活闭环 V1（2026-08-29）

正式报告见
[`FORMAL_FOOD_INVENTORY_CONSERVATION_RCA_AND_CLOSURE_REPORT.md`](FORMAL_FOOD_INVENTORY_CONSERVATION_RCA_AND_CLOSURE_REPORT.md)。
原洛阳365日证据差额绝对值为12,724,917 milliunits，三次独立复现完全一致。首次差异发生在
Day 0初始化：970,000粟米从太仓库存内部转入`household.compact_reserves`，旧证据只统计库存表而
遗漏家户紧凑储备；Day 12起又遗漏`product.reference.food_equivalent`兼容食品。实际世界没有重复
消费或错误产粮，根因是两个测试各自维护的过期食品allow-list与不完整Closing边界。

本阶段新增只读正式/洛阳食品守恒审计器。正式食品集合来自Content Registry，逐Product、Owner/
Inventory、Batch和InventoryTransaction重放，检查未知物理变化、内部转移、Reservation、重复ID、
负批次和缺失引用；洛阳紧凑审计另记录Day、Phase、Flow和旧边界差额。修复后Day 0/1/7/30/90/
180/365均严格为0，30日连续/Save-Load续跑一致，365日三次权威状态哈希一致，正式产品/库存/批次/
事务审计JSON三次哈希一致。完整Core为781/781，失败0；审计246个食品批次与42笔食品事务耗时8 ms，
不改变World Snapshot。

World Save继续为V77，洛阳派生Checkpoint继续为v6；manifest只增加排除Performance遥测的
`deterministic_state_sha256`，原文件SHA仍用于gzip完整性，不修改旧库存或重建历史事务。受控图形
EngineSmoke通过；适用Unity食品独立fixture为1/1 PASS。一次无筛选全EditMode在300秒继续执行全项目
无关测试时超时，保留为`blocked/124`历史诊断，不冒充全量PASS，也不影响本任务无Presentation变更的
适用门禁。当前正式状态是`TASK_FORMAL_FOOD_INVENTORY_CONSERVATION_RCA_AND_CLOSURE_V1_ACCEPTED`，
固定下一阶段为“洛阳外围供应区与城市物流V1”。
