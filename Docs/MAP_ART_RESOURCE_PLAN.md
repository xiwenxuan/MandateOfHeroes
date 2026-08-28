# 地图美术与开放资源计划

## Document Governance

## 洛阳建筑选择、碰撞代理与道路通行图 V1（2026-08-28）

- 正式入口：`TASK_LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1.md`；
- 2,084项Facility具有独立选择代理合同，最密CITY窗口只实例化549个BoxCollider触发器；
- 最终美术Prefab继续保持零Collider，代理只承担战略选择，不是角色或车辆实体阻挡；
- 359个道路节点、18个门节点和2个桥节点形成379节点、382边的静态通行图；
- 334条边来自严格四邻接，28条断点连接边明确标记`Provisional=true`，20条为桥门接入边；
- CITY提供青色道路叠加与黄色选择高亮，WORLD切换清理运行时代理；
- 当前状态为`TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`，不宣称角色尺度NavMesh、城门开闭、
  桥梁损毁、道路考古细化或高分辨率洛阳Terrain完成。

## 洛阳剩余38项用户预接受最终资产完成 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1.md`。
- 用户明确要求剩余38项无需逐件审批并全部接受；决定冻结为
  `decision.luoyang-remaining-38.preaccepted.2026-08-27.v1`，覆盖评审序号`15—21、23—53`。
- 8个P0、10个P1、14个P2和6个P3槽位影响2,068项Facility；与先前16项共同覆盖54个稳定
  Asset Variant和2,084项正式Facility，不改变任何Facility、Global Cell、建设权限、模拟或存档事实。
- 已生成38个项目原创Unity原生Prefab、22个材质、12个网格和38个真实FBX；每件具有3个非空LOD、
  稳定锚点、有效材质和零Collider，FBX均经Unity ModelImporter回读。
- 来源清单覆盖240个项目源/依赖及`.meta`文件和2个工具链文件，重复生成SHA-256均为
  `19d27e5ac9f287c4ad841fe65db7db300f9a07f873d744d2ad914dd049091612`。
- 运行时真实Prefab批准为true，资源缺失程序回退批准为false；54/54全资产图形PlayMode和最密549
  Facility批处理门禁通过。当前洛阳最终资产槽位已`54/54`激活、剩余`0`。
- 本阶段仍不包含考古复原、手绘/PBR贴图终稿、室内、碰撞、导航或建筑动画。

## 洛阳 P0 命名城门第四批用户接受与最终激活 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`。
- 按54槽位清单评审序号`11/12/13/14`冻结谷门、津门、开阳门、旄门；既有Facility、Model、Asset
  Variant、Global Cell、权威朝向、通行锚点、史料与建设规则均未改变。
- 用户在一张总览和四张近景后表示“上一个接受”，按当前上下文登记四件全部接受，决定记录为
  `decision.luoyang-p0-named-gate-fourth-batch.accepted.2026-08-27.v1`；候选期图片保留原始
  `PENDING/false`历史含义。
- 4个项目原创Unity原生Prefab、4个真实FBX、三级LOD与稳定放置/内外通行锚点保持不变；来源清单
  覆盖56个项目源/依赖及`.meta`文件、2个工具链文件和4个FBX，SHA-256为
  `20c8981a1597314a38a4e211e3a970f22875534d35c48ade33e2b317aaf9c87b`；Unity回读、运行时批准/
  回退、五视图和最密549 Facility批处理通过。
- 当前状态为
  `LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`。四项静态
  `FinalArtApproved=true`，运行时只有真实Prefab加载成功才批准为真，程序回退强制为false。
- 本条保留第四批完成时的历史边界；后续剩余38项预接受完成任务已使洛阳达到54/54激活。

## 洛阳 P0 地标第三批用户接受与最终激活 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`。
- 承接已最终激活的首批与第二批共8项，按54槽位清单最低剩余P0评审序号`6/7/8/9`选择
  灵台、太仓、武库、濯龙园；Facility、Model、Asset Variant、Global Cell、史料与建设规则未改变。
- 用户在五视图审模包后明确回复“接受”，按上下文登记为四件全部接受，决定记录为
  `decision.luoyang-p0-landmark-third-batch.accepted.2026-08-27.v1`；该明确决定关闭原计划的额外
  多角度决策板门禁，不改变历史审图输入。
- 4个项目原创Unity原生Prefab、4个真实FBX、三级LOD与稳定锚点保持不变；来源清单覆盖60个项目
  源/依赖及`.meta`文件、2个工具链文件和4个FBX，当前SHA-256为
  `40e1ccad3af83e9b16119df73b435bc2ae1d9b46c97af9db5087904a53fc50c2`。Unity回读、五视图和最密
  549 Facility批处理回归通过。
- 当前状态为
  `LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`。四项静态
  `FinalArtApproved=true`，运行时只有真实Prefab加载成功才批准为真，程序回退强制为false；第四批
  后来已由独立有限任务完成接受与最终激活，剩余38个槽位仍未获授权。

## 洛阳 P0 地标第二批用户接受与最终激活 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`。
- 用户在北宫、永安宫、太学、辟雍四件多角度决策板后回复“全部接受”，决定已记录为
  `decision.luoyang-p0-landmark-second-batch.accepted.2026-08-27.v1`。
- 四件既有项目原创原生 Prefab 和真实 FBX 已通过 Unity 回读；来源清单覆盖54个项目源/依赖文件、
  2个工具链文件和4个FBX，当前SHA-256为
  `9b380964802400ef7a96838b758b68be48df8063e0380d7b3712c1301baa3142`。
- 当前状态为
  `LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`；四项静态
  `FinalArtApproved=true`，运行时只有真实Prefab加载成功时批准为真，程序回退实例强制为false。
- 本节激活当时未改模型、Prefab、FBX、材质、LOD、锚点、Collider、Facility、Cell、建设规则、模拟
  或存档；第三批、第四批后来分别由独立有限任务完成接受与最终激活，剩余38个槽位仍未授权。

## 洛阳 P0 地标第二批多角度审模与决策对照板 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1.md`。
- 北宫、永安宫、太学、辟雍已建立 4×3 固定相机合同，并输出一张总览、十二张 1600×1000
  Unity Game View 和四张 3000×900 无裁剪、无调色决策板。
- PreviewOnly 审模实例已移到既有平缓评审 Cell，太学与辟雍主体被地形线遮挡的问题已经关闭；
  权威 Facility、Global Cell、模型、Prefab、FBX、材质、LOD、锚点和来源清单均未改变。
- 决策板机器清单覆盖 12 个输入和 4 个输出；脚本重复生成的四板与清单共 5 个 SHA-256 全部一致，
  四板人工视觉检查通过。
- 本节记录生成决策板时的历史状态
  `LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_DECISION_BOARDS_READY_FOR_USER_DECISION_V1`；板内
  `PENDING/false`不回写。用户随后已全部接受，当前状态以上述最终激活任务为准；第三批后来已由
  独立有限任务完成最终激活。

## 洛阳 P0 地标第二批原生 Prefab、FBX 与审模候选 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md`。
- 按54槽位清单最低剩余P0评审序号选取北宫、永安宫、太学、辟雍，保持Facility、Model、Asset
  Variant、Global Cell、史料与建设规则不变。
- 已生成4个项目原创Unity原生Prefab、4个真实FBX、三级LOD、稳定锚点与5张1600×1000 Game View；
  运行时真实Prefab加载成功且未启用程序回退。
- 来源清单覆盖54个源/依赖及元数据文件、2个工具链文件和4个FBX，Unity回读与最密549 Facility
  批处理回归通过。
- 本节记录接受前的原生源状态
  `LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_SOURCE_READY_FOR_USER_REVIEW_V1`。四项现已由用户
  全部接受并完成最终激活；当前来源清单和批准状态以上述最终激活任务为准。

## 洛阳 P0 四件套 FBX 源冻结与最终激活 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1.md`。
- 使用 Unity FBX Exporter 4.2.1 按四个冻结路径生成真实 FBX，并由 Unity 重新导入验证三级 LOD、
  材质、可逆锚点映射、锚点位置、几何包围盒和零 Collider。
- 最终清单覆盖 42 个项目源/`.meta` 文件、2 个工具链文件和 4 个 FBX，路径、长度与 SHA-256
  均已冻结；官方包及 Autodesk FBX SDK Unity 绑定均为 4.2.1、Unity Companion License。
- 当前状态为
  `LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`；四项静态
  `FinalArtApproved=true`，但运行时程序回退实例强制为 false。
- 本批准只关闭用户已接受的战略地图四件套 V2，不代表考古复原、手绘/PBR 贴图或其余 50 个
  最终资产槽位完成。

## 洛阳 P0 四件套用户接受登记与源资产归档就绪 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_FOUR_PIECE_USER_ACCEPTANCE_AND_SOURCE_ARCHIVE_READINESS_V1.md`。
- 用户在审阅四张三视图决策板后回复“接受”，按上下文登记为南宫、明堂、广阳门、北宫南门
  四件全部接受；静态内容和运行时已明确显示 `USER DECISION: ACCEPTED`。
- 生成器、P0 目录、4 Prefab、6 Material、4 Mesh 及其 `.meta` 共 32 个文件已完成路径、长度、
  SHA-256 归档；原生 Prefab 重建后清单连续生成哈希一致。
- 当前状态为
  `LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_UNITY_NATIVE_SOURCE_ARCHIVED_INDEPENDENT_DCC_FBX_REQUIRED_FINAL_ACTIVATION_PENDING`；
  编译、定向核心、三个 EditMode 门禁、13 视图 PlayMode 和 549 Facility 批处理回归通过。
- 四个冻结 FBX 目标均缺失，本机也没有可用 DCC/FBX 工具链；依据既有门禁，
  `FinalArtApproved=false`，不得以空文件或未验证导出物替代真实源。

## 洛阳 P0 四件套审模决策对照板 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_FOUR_PIECE_REVIEW_DECISION_BOARD_V1.md`。
- 将南宫、明堂、广阳门、北宫南门各自的前斜、后斜、低角三张已验证 Unity Game View 无裁剪、
  无调色排成四张 3000×900 决策板，并用无时间戳 JSON 记录 12 个输入和 4 个输出的尺寸与 SHA-256。
- 当前状态为 `P0_FOUR_PIECE_REVIEW_DECISION_BOARDS_V1_READY_FOR_USER_DECISION_FINAL_APPROVAL_PENDING`；
  首次生成、机器清单核验、五个生成文件重复哈希一致和四板人工视觉检查通过。
- 本步骤不改变四件套资产或批准标志；用户已在后续回复中接受四件套，当前转入上述源资产归档
  就绪门禁。独立 DCC/FBX 到位前仍不激活最终批准或批量替换其余 50 槽位。

## 洛阳 P0 四件套多角度转台审查包 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_FOUR_PIECE_MULTI_ANGLE_TURNTABLE_REVIEW_PACK_V1.md`。
- 在不修改四件套几何、材质、Prefab、LOD、锚点、权威 Cell、玩法或存档的前提下，固定南宫、
  明堂、广阳门、北宫南门各自的前斜、后斜、低角视图，共 12 个稳定相机 ID，并增加运行时逐件、
  逐角度循环切换控制。
- 当前状态为 `MULTI_ANGLE_REVIEW_PACK_V1_READY_FOR_USER_DECISION_FINAL_APPROVAL_PENDING`；全工程编译、
  定向核心 1/1、多角度 EditMode 2/2、13 图 PlayMode 1/1、既有 V2 五图 PlayMode 1/1 和最密
  549 Facility 批处理 PlayMode 1/1 均通过。
- 已生成一张总览和十二张 1600×1000 Game View。四项仍为 `FinalArtApproved=false`；下一步是用户
  逐件给出“接受 / 修改 / 否决”，不是直接进入最终 DCC/FBX 归档或其余 50 槽位批量替换。

## 洛阳 P0 四件套视觉精修与审图可读性 V2（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_FOUR_PIECE_VISUAL_REFINEMENT_AND_REVIEW_READABILITY_V2.md`。
- 南宫、明堂、广阳门、北宫南门的项目原创 Unity 原生 Prefab 已补强屋脊、檐带、门扇、铺地、
  四向阶道、短瓮城角楼、双阙和旗杆等战略识别细节；稳定身份、Global Cell、史料与建设规则不变。
- 四件套仍共用 6 个 Material 和 4 个 Mesh，LOD Renderer 合计为 `137 / 37 / 21`；每项三级 LOD
  严格递减、锚点完整、无 Collider，运行时没有激活程序化回退。
- 四个近景已按建筑真实 Renderer 包围盒中心取景，并以安全画幅自动合同防止裁切；一张总览和四张
  1600×1000 近景已经生成，549 Facility 密集窗口批处理图形回归通过。
- 当前状态为 `REFINED_NATIVE_PREFAB_V2_READY_FOR_USER_REVIEW_FINAL_APPROVAL_PENDING`。
  四项 `FinalArtApproved=false`；用户逐项接受前，不建立最终批准，也不批量替换其余 50 项。

## 洛阳 P0 四件套原生 Prefab 美术交付 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_FOUR_PIECE_NATIVE_PREFAB_ART_DELIVERY_V1.md`。
- 南宫、明堂、广阳门、北宫南门的冻结 Resources 路径现已有实际 Unity 原生 Prefab；共用 6 个
  Material 与 4 个项目原创 Mesh，每项均有 3 个非空 LOD、稳定锚点且不含 Collider。
- 运行时四项均优先加载 Prefab；程序化三级 LOD 继续只作资源缺失回退和全城远景合批来源。
- 当前状态为 `READY_FOR_USER_REVIEW_FINAL_ART_APPROVAL_PENDING`。编译、定向核心 1/1、生成器合同
  EditMode 1/1、既有 P0 EditMode 4/4、图形 PlayMode 1/1、受影响全城批处理图形 PlayMode 1/1 已
  通过，5 张 1600×1000 实机截图已生成；最新批处理构建为 24.0398ms，仍在冻结预算内。
- 这些是项目原创战略地图候选，不是考古复原；独立 FBX/DCC 源、手绘贴图和用户最终批准仍未完成，
  四项 `FinalArtApproved=false`。用户审图前不批量替换其余 50 项。

## 洛阳 P0 最终资产四件套垂直切片 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_P0_FINAL_ASSET_FOUR_PIECE_VERTICAL_SLICE_V1.md`。
- 首批只锁定南宫、明堂、广阳门、北宫南门；稳定设施、模型、Asset Variant、Global Cell、史料来源
  与建设权限均沿用现有洛阳数据。
- 已实现六材质参数、每项三级 LOD、稳定锚点、世界合批 LOD2、Resources Prefab 优先加载和严格
  Prefab 合同；缺少或未交付 Prefab 时使用项目原创程序候选。
- 本阶段状态已由后续“原生 Prefab 美术交付 V1”取代；原程序候选继续保留为回退和同机位对照。
- 完整核心套件本轮在 300 秒内未结束；全工程编译、定向核心 1/1、目标 EditMode 4/4、图形
  PlayMode 1/1、受影响全城批处理图形 PlayMode 1/1 已通过。最新最密窗口为 1,673 个 LOD2
  源模块、97 个 Renderer、17,512 顶点、22.9509ms，仍在冻结预算内。真实四件套资产获批前，
  不批量替换其余 50 项。

## 洛阳全城建筑视觉验收与最终资产清单 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1.md`。
- 2,084项开局Facility按实际工厂优先级落到54个互异Asset Variant，替换粒度不再误用36个基础Model。
- P0/P1/P2/P3分别为24/10/14/6个槽位，影响24/1,800/226/34项Facility；其中南宫、明堂、
  广阳门、北宫南门已有项目原创 Unity 原生 Prefab 候选，其余槽位仍为程序化 V1。
- 替换必须保留稳定Model/Asset/Profile/Facility身份和程序回退；外部素材进入候选前必须完成来源、
  作者、版本、许可证、修改与再分发登记。
- 已实现54项PreviewOnly审阅板和四个固定优先级镜头；编译、定向核心1/1、目标EditMode 3/3、
  图形PlayMode 1/1及全城批处理回归均通过，四张1600×1000 Game View已经生成。当前状态为
  `IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`。
- 下一阶段只允许先做南宫、明堂、广阳门、北宫南门四项P0最终资产替换竖切片，不批量替换其余
  50项。

## 洛阳建筑全城性能预算与批处理 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1.md`。
- 从2,084项真实开局Facility建立64个8×8纯Presentation批次；当前最密24×24审查窗口包含549项、
  9个批次，不创建Chunk、Region、行政、模拟或存档事实。
- 接入P0四件套LOD2后的最密窗口1,673个LOD2源模块按“空间批次＋材质”合并为97个
  Renderer/Combined Mesh和17,512个顶点；最新本机Editor回归构建24.0398ms，Renderer降幅94.20%，
  冻结预算通过。
- 当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`；全工程编译、相关核心
  1/1、目标EditMode 3/3和图形化PlayMode 1/1通过，并生成1600×1000实际Game View与指标JSON。
- 该结果不是最终平台GPU、Addressables、烘焙遮挡或全城高精资产验收。基础设施、低频防御、资源
  农业和最后公共/礼制/医疗生产均已完成，当前转入54项最终资产槽位审阅。

## 洛阳中频城市肌理建筑 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_MEDIUM_FREQUENCY_URBAN_FABRIC_V1.md`。
- 根据2,084项开局Facility真实统计，为市场/商铺48、商队院45、学校39、地方官署16和军营10项
  制作五套原创程序化战略院落，总计158项；与高频包合计覆盖1,958/2,084项，约94.0%。
- 五套模型具备独立Asset Variant、城市肌理角色、街面接口、锚点和三级LOD；基础权限集合保持不变。
- 15格审图每类3座并轮换方向，全部是PreviewOnly正式Global Cell，不伪造Facility位置。
- 当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`；目标EditMode 3/3、
  图形化PlayMode 1/1通过并生成1600×1000实际Game View。
- 后续全城建筑性能预算与批处理门禁已经通过；2,084项仍保持轻量计划，不逐对象常驻实例化。

## 洛阳高频建筑生产模块包 V1（2026-08-27）

- 正式入口：`TASK_LUOYANG_PRODUCTION_BUILDING_MODULAR_KIT_AND_HIGH_FREQUENCY_CITY_FABRIC_V1.md`。
- 已把使用量最高的住宅、旱田、道路、工坊、园圃、仓库、城墙、宫墙、客栈驿舍和牧场10类接入
  Production Profile、Asset Variant、入口/放置锚点、八种原创缓存Mesh与三级LOD。
- 10类覆盖184年洛阳开局1,800/2,084项Facility，约86.4%；其余26类继续使用已验收的程序化V1，
  不得写成已经完成生产美术。
- 当前状态为`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`；Unity 2022.3.62f3c1
  目标EditMode 2/2、图形化PlayMode 1/1通过并生成实际Game View。
- 生产配置不改变建设权限。宫墙等政府/历史向模型不会因为存在生产资产而进入普通玩家建设菜单。
- A级地标、十二城门/宫门、中频城市肌理和全城性能预算门禁均已由后续专项完成。

## 洛阳设施模型覆盖 V1 用户授权（2026-08-26）

- 正式入口：`TASK_LUOYANG_FACILITY_MODEL_COVERAGE_AND_A_TIER_COMPOSITION_V1.md`。
- 用户已接受第一批建筑风格并明确授权继续完成洛阳建筑设置；该决定解除下述旧Style D门禁中对
  洛阳程序化建筑V1的阻断，但不等于授权全国建筑量产或最终美术定稿。
- 当前范围为7项既有模型＋29项补充模型、2,084项Facility显式绑定和36格审图；预览不改变世界事实。
- 资产继续采用项目原创Unity基础几何和自有调色板，不复制商业游戏模型、贴图或布局。

## 全国显式战略格 LOD V1 用户决定（2026-08-26）

- 正式入口：`TASK_HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1.md` 与 `HISTORICAL_WORLD_REFERENCE/HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1/`。
- 用户已接受类似经典三国题材回合制战略地图的“明确可读战术格”体验，但实现必须净室原创，不复制商业游戏画面、布局、材质、模型、图标、字体、代码或数据。
- 显式格直接显示既有 2000m 方形 Global Cell；它不是 SubCell，也不把八邻接改成六邻接。
- 用户已进一步授权全国铺开：WORLD 使用 32×32 Cell 的视觉 LOD 引导格，REGION/CITY 使用 1×1 的 2000m 精确格；正式入口为 `TASK_HAN_WORLD_NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1.md`。
- 32×32 只控制总览可读性和网格预算，不形成新世界事实或聚合语义；全国总览不实例化七百万个格面。
- 当前状态：`NATIONWIDE_GRID_IMPLEMENTED_STATIC_CHECKS_PASSED_UNITY_RUNTIME_BLOCKED`；概念预演图不得登记为 Game View 或 Golden。

## Style D 战略山河 V2 用户审图门禁（2026-08-16）

- 正式入口：`TASK_HAN_WORLD_STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_AND_ZHONGHUA_SOURCE_RECOVERY_V2.md`与`HISTORICAL_WORLD_REFERENCE/HAN_WORLD_STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_V2/`。
- `Global Cell resolution != visual terrain resolution`：REGION、CITY和近景只细化表现顶点，不建立SubCell或第二套地图。
- 河流使用曲率自适应采样、受限Miter与Bevel回退，河岸与水面共享中心线和宽度；锐弯源线段端点与汇流仍为`PARTIAL`。
- 森林使用WORLD地表密度、REGION合并树冠簇、CITY合并单树网格；密度和位置使用全局确定性坐标。
- 当前状态为`STYLE_D_STRATEGIC_LANDSCAPE_V2_READY_FOR_USER_REVIEW`，不是最终Golden。
- 中华三国志候选源码本轮因GitHub 443阻断未克隆，许可证`UNRESOLVED`；不得复制候选代码或资产。
- 用户审图前禁止全国推广、河南尹全量高精生产和洛阳城市建筑资产生产。

- Purpose：定义地图美术原创方向、视角需求和开放资源选择边界。
- Authority：L1 CURRENT DESIGN SPEC。
- Covers：地图美术与资源策略。
- DoesNotCover：统一Cell世界事实、历史地理数据或素材许可证最终审计。
- Supersedes：早期地图视觉讨论摘要。
- SupersededBy：无。
- RelatedCanonicalDocs：`WORLD_SIMULATION_FOUNDATION.md`、`LEGAL_AND_ASSETS.md`。
- Status：CURRENT。

## 原创方向

项目地图采用“东汉军府舆图＋绢本设色＋封泥军棋”的原创方向。

可以借鉴历史战略游戏中“地图即战场、山河清楚、缩放自由”的抽象玩法，但不临摹任何商业游戏的地图画面、笔触、界面布局、字体、图标或模型。

## 统一地理底层

所有地图视角共用地点编号、坐标、道路、河流、地形和历史状态：

```text
开放地形数据与自研历史数据
        ↓
统一地理数据库
        ↓
天下战略图 / 州郡舆图 / 县乡近览 / 战场地形
        ↓
势力 / 战争 / 商业 / 治安 / 身份专属信息
```

不把城市、道路和可交互信息永久画进一张背景图，避免更换年代或发生战争后无法更新。

## 推荐来源

| 来源 | 用途 | 许可与处理原则 |
| --- | --- | --- |
| Natural Earth | 海岸线、基础世界轮廓 | 公共领域；保留来源记录 |
| USGS高程产品 | 高度图、山地与河流辅助 | 一般为公共领域；逐项核对产品元数据 |
| Poly Haven | 岩石、土壤、树皮、天空原料 | CC0；进入项目前统一风格化 |
| Kenney | 原型自然与场景资源 | 以具体资源页许可为准，优先CC0 |
| Quaternius | 原型3D自然和道具 | CC0；正式资源需汉代化与统一材质 |
| 自制Krita纹理 | 绢纹、笔刷、遮罩、图标 | 项目原创资源 |
| QGIS处理结果 | 裁切、投影、高程、河流遮罩 | 工具输出；底层输入数据许可仍需记录 |

## 谨慎或禁止来源

- CHGIS V3限制商业使用与再分发，不进入公开游戏数据；
- OpenStreetMap采用ODbL，且现代道路不等于汉代道路，使用前必须单独评估数据库义务；
- Unity Asset Store的免费素材不等于开源素材，不将标准许可源文件直接提交到公开仓库；
- 不使用从《三国志》系列、无双系列或其他商业游戏中提取的地图、模型、图标、字体、音乐和数据；
- 不把AI生成的概念图当作历史地理证据或直接交互底图。

## 城镇表现分层

城镇页允许使用原创或许可兼容的全景插画营造时代与地点气氛，但必须与动态世界事实分层：

```text
装饰全景（不可作为建筑事实）
        ↓
动态空间建筑（设施稳定ID、街区、坐标、占地、所有者、负责人、权限、运营状态）
        ↓
建筑内部行动（读取正式市场、库存、组织、人物和任务账）
```

- 全景插画不得把动态设施数量、归属、军情或价格永久画死；
- 可进入建筑必须由世界存档事实生成，并能表现开放、受限、停运和已进入状态；
- AI辅助插画必须记录生成工具、日期、输入来源和项目位置，且不作为历史考据结论；
- 没有专属插画的地点使用统一绢本程序化底图，不得因此伪造当地设施。

## 资源登记字段

每项外部素材或数据必须记录：

- 名称与版本；
- 作者或发布机构；
- 原始页面；
- 下载日期；
- 许可及版本；
- 是否修改；
- 项目内文件位置；
- 对应的署名文字；
- 是否允许源文件随公开仓库再分发。

## 后续制作顺序

1. 完成中山世界节点—城镇空间近览—建筑功能的首条全层级竖切片；
2. 完成涿县—广宗地图美术竖切片；
3. 选取洛阳—虎牢关—陈留制作第一块真实地理验证区；
4. 建立城市、关隘、渡口、港口、村庄的原创图标规范；
5. 接入经过许可审核的高程与河流数据；
6. 让县乡近览的农田、市场、医馆、驿站和匪寨成为真实模拟地点；
7. 从同一高程与地貌数据生成战场基础地形。

## 洛阳184历史地图表现规则

- 正式地图只能表现已有 `FacilityDefinition + FacilityState`、道路、防线和自然地物；不存在“看得见但世界账
  不知道”的可交互宫殿、城门或市场。
- 历史标签必须可区分史实锚点、合理复原与玩法补全；位置还须另标确证、很可能或近似，不用画面精致度掩盖
  史料不确定性。
- 2000m Cell是统一世界的操作性抽象。城郭、宫城和十二门可以为可玩性放大，但图例与报告必须明确其不是
  考古实测比例，禁止按Cell边长反推单体建筑尺寸。
- 官方 `LUOYANG_184_HISTORICAL_MAP_V1.png` 使用项目自生成地形、道路、城郭和标注，不复制任何商业游戏
  地图、美术、图标或界面；它是可追溯的数据成图，不替代Unity中的动态设施状态。
- 同一场景支持连续缩放、Cell/Facility选择、历史置信度、人口住房、岗位和城防专题；视角变化不复制世界。

## 历史状态美术与同一地图规则（ADMINISTRATIVE-SEAT-CANONICAL-PLACE-V1）

- 不为184、189、190、194或其他年份复制相互独立的模拟地图；所有历史空间使用同一Canonical Cell World，并通过State Snapshot/Overlay表现差异；
- Facility和Urban Cell美术至少支持`Normal / Damaged / Destroyed / Ruined / Rebuilt / Repurposed`，具体状态必须读取世界账，不能由历史概念图决定；
- 治所、州郡县标签和战略显示名是信息图层，不能因为文字相同而生成第二座城市模型；
- 史料只证明大范围毁坏时，美术可以表达区域性废墟气氛，但具体普通Facility必须标记为MODELED或UNKNOWN，不能伪称史实精确复原；
- 历史状态图、概念原画和美术Overlay都不是第二套模拟事实，最终仍以Cell、Facility、Population、Organization及Owner/Controller为准。

## 全国统一空间与 Region 美术生产规则（GLOBAL SPATIAL FOUNDATION V1）

- 美术生产必须读取 `HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1/GLOBAL_SPATIAL_FOUNDATION_CONTRACT_V1.md`；
- 全国只有一个 Albers CRS、固定 Global Origin 和 2000m Global Cell 格网；Global Cell 是空间事实的终点；
- 河南尹、关中、成都平原等 Region 只是逐块提高 Terrain、道路、河流和建筑表现精度，不是独立地图；
- Region 权威范围来自完整 Global Cell 的成员集合；视觉边界由成员 Cell 外边派生，不切 Cell，也不要求按完整16×16技术块划分；
- 16×16 仅保留为技术 Spatial / Simulation Aggregation Block；Terrain Tile 与 Streaming Unit 尺寸必须经过Unity实测，可彼此不同；
- 64×64 仅为旧 Storage / Compression Block；边缘 Terrain/河路仍必须共享全局采样与父级 ID；
- Facility 的 Cell 内视觉位置、旋转、入口和装饰是 Visual Local Anchor，不是 SubCell，不参与土地、军队或产权结算；
- Unity Floating Origin 只平移表现对象，不得写回 Cell、Place、Facility、Person、Force 或 Route 的全局坐标；
- 旧洛阳背景图正式降级为构图和风格参考，后续真实地图主体来自同一 Global DEM、Cell、Facility 和运行时状态。

## 全国自然视觉基线 V1

- `HanWorldNaturalBasemap` 是程序化自然底图入口；全国视角使用同源 DEM 降采样 Mesh，区域视角使用 8×8 Cell Terrain Tile。
- Terrain Tile 的 16km 尺寸来自 4×4、8×8、16×16 真实地图实测，不继承旧 16×16 聚合语义；相邻 Tile 按全局顶点采样，当前最大共享边误差为 0m。
- 河流使用独立投影 Ribbon Mesh，森林使用合并 Vegetation Mesh；蓝色 Cell 与逐树 GameObject 都不能成为正式表现。
- 地表 ID 使用开放的稳定命名空间，不用固定枚举封死森林、湿地、河岸、沙地、草地、裸地和岩石等扩展内容。
- 全国 V1 的 2km DEM 只适合 WORLD/REGION 基线。河南尹、洛阳和战场近景必须在不改变 Global Cell 的前提下补充更高分辨率视觉采样与历史水系证据。
- 旧背景全部为参考资产；`BACKGROUND_REQUIRED=FALSE` 是后续地图回归的强制条件。

## 全国自然地图视觉表现 V2 现行规则

- 正式报告：`HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2/HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2_REPORT.md`。
- WORLD 与 REGION 必须来自同一权威 DEM 与 Global Cell；不可用第二张手绘背景替代地形事实。
- WORLD 使用降采样连续网格；REGION 使用连续 2km Cell 地表。8×8 Terrain Tile 是驻留、碰撞和流式单元，不得以重叠矩形表面暴露在画面中。
- 自然 Surface 以稳定命名空间 ID、主次混合和全局连续坐标变化表现；增加材质或地方变体不应修改存档结构。
- 河流美术接下来优先补充洲滩、支汊、水面法线、桥渡与河岸植被；不得为填空伪造洛水几何。
- 森林美术接下来优先替换程序化锥体树冠，增加树种、季相、层次和风动；密度与位置继续服从确定性全局密度场。
- Cell Grid、Region、行政、道路、资源、情报等都属于基于玩家知识的可切换信息模式，不可烘焙进自然底图。
- 当前 14 张截图状态为 `CANDIDATE_PENDING_USER_APPROVAL`；用户确认前不得将其登记为最终 Golden，也不得开始下一 Region 的高细节生产。

## 全国自然地图 Art Direction V1 候选门禁

- 正式入口：`TASK_HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1.md` 与 `HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1/`；
- 当前已在同一 DEM、Global Cell、河流、森林和固定相机上建立 A 半写实自然、B 国风半写实、C 战略沙盘三套 Profile；
- 三套均为项目自产 Shader/程序表现，不含商业游戏资产和背景贴图；
- 当前状态是 `HAN_WORLD_ART_DIRECTION_V1_CANDIDATES_READY`，不是 `STYLE_FINALIZED`；
- Codex 推荐 STYLE B，但 `USER_SELECTED_STYLE=PENDING`；用户选择前禁止全国推广、河南尹高精 Terrain 和洛阳城市美术。

### Style D 《中华三国志》启发净室原型

- 正式入口：`TASK_HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1.md` 与 `HISTORICAL_WORLD_REFERENCE/HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1/`；
- Style D 借鉴的只是战略山河地图需要解决的图层、视域、山河骨架和道路独立语义问题；没有复制 XNA 代码、tile、地图、贴图或其他资产；
- 山体、平原、森林和河谷均由本项目权威 DEM/自然地表实时派生；Style D REGION 以连续森林面替代树点 canopy batch；
- 当前状态是 `STYLE_D_ZHONGHUA_SANGUOZHI_FUSION_PROTOTYPE_READY`，不是最终 Golden；
- 已识别的美术债包括河流急弯/接缝锯齿、森林中近景层次不足和 CITY 距离的 2km mesh 粗糙；
- 全国推广、河南尹高精和洛阳城市继续要求用户明确批准。

## 洛阳实际全城构图与地形融合 V1

- 正式入口：`TASK_LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1.md`；
- 54/54最终建筑槽位不再扩充；2,084项Facility全部获得稳定Visual Local Anchor；
- 六类构图区使用稳定命名空间ID，不建立第二套城市、行政区、Region或SubCell；
- 道路、沟渠和墙体按相邻真实Facility确定中心线连接，其他建筑朝向最近真实道路；
- 最密549 Facility窗口按Cell内偏移后的全局位置重新采样同一Terrain高度；
- 当前Style D只作为审查Profile，不因此冻结全国自然地图Golden；
- 洛阳高分辨率DEM、最终水系、碰撞/导航和外围供给区仍按后续专项推进。
- 后续`LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1`已完成战略选择触发器与静态道路图；
  本历史条目所指的实体碰撞、角色尺度导航和外围供给仍未完成。

## 洛阳身份化道路连接与动态门桥通行 V1

- 正式入口：`TASK_LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1.md`；
- 当前运行时在旧382边基础图上使用402边精化层，不原地篡改旧验收合同；
- 28条道路断点连接为项目自产格级玩法重建数据，具有稳定ID、来源边、逐格折线、
  `historical_evidence.gameplay_reconstruction`和`ClaimsHistoricalExactness=false`；
- 20个门桥各有两条道路接近边；开放、关闭、受损和毁坏状态来自Domain会话态，Presentation只读取；
- 青色表示严格道路/门桥接近，橙色表示玩法重建连接，红色表示关闭/毁坏，橙黄色表示受损；
- 当前状态不跨读档，不代表门扇动画、守军权限、攻城、桥梁载重/洪水/维修或人物尺度NavMesh完成；
- 下一步先将门桥状态接入WorldState、命令/事件和顺序存档迁移，再建设城外道路与供应运输节点。

## 洛阳门桥正式世界状态与存档 V1

- 正式入口：`TASK_LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1.md`；
- World Schema由V73升至V74，保存20项门桥状态、Revision、世界时间、原因、命令和事件引用；
- 一个显式初始化命令原子建立完整20项，逐门桥转换命令复用M25-P7结果/事务/Outbox；
- V73→V74只建立空集合，不能把旧预览会话倒推成历史事实；
- 地图控制器绑定正式WorldState后只读取只读投影，未绑定审图模式继续使用非持久会话；
- 下一步建立守军/权限/破坏/维修原因与真实资源工单，再进入城外道路和外围供应运输节点。
