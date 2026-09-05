# 洛阳 Golden Block 建筑艺术语言与 Compound V3：Step 0—2 基线

## 1. 执行结论与范围

- 交接编号：`MOH-HANDOFF-20260905-2227`。
- 新聊天框于 `2026-09-05 23:14:10 +02:00` 完成实时核验并将交接状态更新为 `CLAIMED`。
- 本文件只完成 V3 `Step 0：开工快照`、`Step 1：固定 V2 Before 镜头`、`Step 2：建立视觉问题清单`。
- Before 取得前没有修改 V3 建筑表现代码；本轮也没有修改 Domain、Simulation、Persistence、正式 Facility、50m PlanningCell、人口、库存、产能或世界时间。
- 历史 V1 同机位 Before 仍不存在；没有伪造历史图。

## 2. Step 0：开工快照

| 项目 | 实时结果 | 说明 |
|---|---|---|
| HEAD | `940c4381da4cbb893c0882fd28e68914397af897` | 与交接单一致 |
| Branch | `codex/m23-p4-quality-artisan-growth` | 与交接单一致 |
| Upstream | behind `0` / ahead `2` | 与交接单一致 |
| Workspace | 开工前默认 `284` 条；`-uall` 展开 `471` 个文件条目，其中 tracked 修改 `61`、逐文件 untracked `410` | 脏工作区基线已冻结；没有删除或回退既有修改 |
| World Schema | `79` | `WorldState.CurrentSchemaVersion` |
| Unity 版本 | `2022.3.62f3c1` (`1623fc0bbb97`) | 与 `ProjectVersion.txt` 一致 |
| 当前 Unity PID | 开工核验时无 `Unity.exe`；正式 V2 复采集使用 PID `8464`，采集完成后保持打开 | Unity Hub 与 Licensing Client 不视为编辑器锁 |
| Golden Block 坐标 | 行 `168—175`、列 `232—239` | 现有正式样板区 |
| Golden Block 尺寸 | `8×8×50m = 400×400m` | `64` 个正式 PlanningCell |
| 派生 Lot | `16` | `IsDerivedPresentationOnly = true`，不创建新世界事实 |
| 正式 Facility 数量 | `2,084` | V2 现有权威布局；Golden Block 只引用正式 Facility |
| 现有模块 | `86` 个可见模块、`21` 个道具、`18` 个植被实例、`6,176` 三角形 | 由当前正式复采集指标记录 |
| 现有 Renderer | `11` | Golden Block 根下 11 个非空共享 Mesh accumulator；现有 EditMode 合同允许 `8—12` |
| Material | `11` | 当前正式复采集指标；每个 Golden Block 批次使用一个共享材质组 |
| Building Registry | `CountyBuildingPresentationProfileCatalog.HanLuoyangV2`，共 `5` 个 Profile | 住宅、市场、工坊、仓廪、官署 |
| Batch Renderer | `Packed Earth Block Ground`、`Courtyard Ground Treatments`、`Street and Alley Network`、`Courtyard Rammed Earth Walls`、`Five-Family Building Bodies`、`Timber Frames and Gates`、三组屋顶、`Market Workshop and Civic Props`、`Courtyard Trees` | 11 组共享 Mesh/Material；没有逐 Cell GameObject |
| 现有 LOD | Profile 声明 `aggregate-silhouette / compound-readable / compound-modules`；实际 Golden Block 只构建一套几何，根节点在 Far/Mid/Near 均常开 | 当前差异只来自相机和全县其他层的显隐，不是 Golden Block 自身的三档裁剪 |
| BuildingPresentation 实现 | 稳定命名空间 Profile ID、数据化屋顶/台基/墙/门/地面/道具/植被/尺度字段；以 `fnv1a64(profile:source:salt)` 派生稳定模块和变化 | Presentation-only，不使用运行时随机，不写回世界事实 |
| 当前 Core 数量 | 当前源码可发现 `984` 个唯一 `WorldKernelTests` 公共 `[Test]` 方法 | 本轮没有执行全量 Core；最近完整但旧源码指纹的汇总为 `956/956`，另有 `960` 项 prepared manifest；V2 历史定向结果为 `4/4` |
| Unity 测试状态 | V3 尚未执行 Project Load、EditMode 或 PlayMode | V2 历史记录为 Project Load 通过、EditMode `1/1`、PlayMode `1/1`；不得替代 V3 结果 |

现有五族 Profile ID：

1. `presentation.building.han.residence.v2`
2. `presentation.building.han.market.v2`
3. `presentation.building.han.workshop.v2`
4. `presentation.building.han.granary.v2`
5. `presentation.building.han.government.v2`

## 3. Step 1：固定 V2 Before 镜头

### 3.1 当前正式复采集

- 正式入口：`Mandate/Validation/Capture Luoyang Golden Block Build Mode V2 Evidence And Review`。
- 执行方法：`Mandate.Editor.LuoyangGoldenBlockBuildModeV2FinalReviewMenu.CaptureAndOpenForReview`。
- 正式场景：`Assets/Scenes/PlayableDemo.unity`。
- Unity 日志：`tmp/handoff-v3-step1-unity-review.log`。
- 完成标记：`LUOYANG_GOLDEN_BLOCK_BUILD_MODE_V2_EVIDENCE_READY`。
- 结果：`02—29` 共 `28` 张当前截图，全部为 `1920×1080`，共 `28` 个唯一 SHA-256；采集时间为 `2026-09-05 23:23 +02:00`。
- 当前运行指标：`5` Profile、`64` Cell、`16` Lot、`86` 模块、`21` 道具、`18` 植被、`6,176` 三角形、`11` 材质、近似 `99.64 FPS`、Schema `79`、`derived_presentation_only = true`。

### 3.2 V3 冻结基线

| 镜头 | V3 冻结文件 | SHA-256 | 字节数 |
|---|---|---|---:|
| 当前 V2 Mid | `01_golden_block_v2_before.png` | `A93945FE21AAAE19401D9E087F59EC96CEBB39706D2478859D6B8C1D78A92C71` | `790,736` |
| 当前 V2 Near | `01b_golden_block_v2_near_before.png` | `649CCEE6B6569AB5B72F2AB14C8EE56F21778C8F0A931477B5542F2D966C4580` | `413,803` |
| 当前指标 | `v2_before_metrics.json` | `177F862057E6D469C389CCA298E4D3A298B92BB577D8BF9C1DD016238B54F55F` | 以文件为准 |

旧截图摄于 `2026-09-04 20:20 +02:00`，而共享 Profile 与世界空间表现文件之后仍有修改，不能冒充当前 V2。为保证历史证据不丢失，旧图也原样保留：

| 历史镜头 | 文件 | SHA-256 |
|---|---|---|
| 旧 V2 Mid | `00_legacy_v2_mid_20260904.png` | `BF8FF6925E207E258DDE89A3E585B1E09308B6DB20934B96285FF7BFC7A03944` |
| 旧 V2 Near | `00b_legacy_v2_near_20260904.png` | `75D8DBEC7455B17F4C85D1A9C024990BB474B274C4930EA2F11DFEBBD6ACE241` |

## 4. Step 2：视觉问题清单

严重度定义：`S1` 会阻止灰模/Mid 人工视觉门；`S2` 是必须在 V3 收口但不单独决定门禁的问题。

| 类别 | 严重度 | 当前 V2 可验证问题 | V3 可验证收口条件 |
|---|---|---|---|
| 轮廓 | S1 | Mid 使用 `64×128` Cell 视窗，8×8 Golden Block 只占画面很小区域；16 个院落呈规则 4×4 小格盘。关闭标签后，除三列长屋顶的仓廪外，住宅、市场、工坊、官署不能靠第一轮廓可靠区分。 | 在正式 Mid 同镜头、关闭标签并弱化材质后，五族仍能由体量、开合、轴线和屋顶主轮廓区分；大型建筑不能只是放大住宅。 |
| 屋顶 | S1 | Profile 名义上有 DomesticGable、MarketCanopy、WorkshopLowGable、GranaryLongGable、CivicRaisedHip，但几何最终主要落为普通 Gable/Low-or-Long Gable/Hip，檐口与屋脊均为细 Box；Mid 主要读到颜色，不足以读到建筑等级和跨度。 | 同一弱材质镜头中可读出民居短脊、市场连续檐面、工坊低长坡、仓廪平行长脊、官署抬高的主殿屋顶层级。 |
| Compound | S1 | 所有 Lot 使用近似相同的 `1.30×1.30` 院地和 `1.36×1.36` 方形围合，只以少量参数和内部模块变化区分。市场开放面、工坊作业院、仓廪装卸边、官署门—庭—堂轴线均未形成强烈平面类型。 | 五族分别形成可读的居住内院、临街开放市场、偏置作业院、平行仓廪与装卸边、正式轴线官署；不依赖文字说明。 |
| 台基 | S2 | 台基/高度差在现有 Mid 与 Near 中均很弱；官署和仓廪没有足够的承重、抬升与入口层级，墙体和屋身在平底板上呈“摆放”感。 | Near 可见台基厚度、入口踏步或坡接、墙根收口；官署与仓廪的台基层级高于普通住宅且没有悬浮/穿地。 |
| 地面 | S1 | Golden Block 是突兀的整块矩形底板；16 个院地进入同一个 accumulator/共享材质，Profile 的 `GroundTreatment` 没有形成五族地面语义。街巷与院门、全县道路/周边地表缺少连续过渡。 | 市场硬地、工坊作业地、仓廪装卸面、官署庭地与住宅生活院能读出差异；主巷—支巷—院门连续，底板边界不再像贴片。 |
| 道具 | S2 | `21` 个道具分布在 `16` 个 Lot，Mid 基本不可读；名为市场/工坊/装卸/生活细节的 `11—14` 图仍显示整个街区且目标未高亮，不能证明各自的功能故事。 | 道具先服务于大形与动线，再补生活层；正式 Near 证据必须锁定单院落并明确看见摊位、材料、装卸或居住细节，同时保持批量化。 |
| 比例 | S1 | 当前 V2 复采集中，Golden Block 与周边城市建筑语言/屏幕尺度严重脱节：样板区是高密小院落岛，周边多数正式 Facility 在同镜头退成稀疏小点。统一 Lot 尺度也压平了住宅与官署、仓廪的等级差。 | Golden Block 模块与 50m Cell、道路、城墙和周边 Mid 建筑保持一致比例；官署/仓廪拥有明确但不过度的体量层级，样板区不再像独立微缩模型。 |
| LOD | S1 | Profile 虽声明 Far/Mid/Near 模式，但 `BuildGoldenBlockPrototype()` 只构建一套几何，`_goldenBlockRoot` 在三档均常开。Mid/Near 主要只改相机；Near 仍是 `18×36` Cell 视窗，并非单 Compound 近景。 | Golden Block 具有可量测的 Far/Mid/Near 模块裁剪或替换；Mid 保留类型轮廓与院落，Near 才展开檐口/台基/道具；转换不改变 Facility、Lot 或稳定变体身份。 |

### 4.1 五类 Facility 的当前辨识度

| 类型 | 当前无标签辨识度 | 主要证据问题 | V3 首要轮廓目标 |
|---|---|---|---|
| 住宅 | 低 | 方院、主屋与侧屋同市场/工坊高度相近；生活树和堆物太小 | 紧凑内院、清晰正房/厢房层级、较低且连续的生活尺度 |
| 市场 | 低 | 摊位和开放棚太小；围合仍接近住宅方院，临街开放面不强 | 面向街道的宽开口、连续棚檐、中央交易空地 |
| 工坊 | 低 | 长棚与材料堆在当前视距不足以改变第一轮廓 | 偏置主作业棚、可读作业院与材料边，不做对称住宅院 |
| 仓廪 | 中（Near），低（Mid） | 三列平行长屋顶是唯一较强特征，但装卸面与抬升感弱 | 平行长体、明确装卸边、较高台基和受控围合 |
| 官署 | 中（Near），低（Mid） | 主殿、双翼、门楼已有轴线意图，但与普通方院同尺度，权威层级不足 | 门楼—前庭—抬高主殿的强轴线、对称翼房与更清楚的屋顶等级 |

### 4.2 现有证据缺口

- `03—07` 与 `09—14` 虽按单类 Compound、屋顶、台基和道具命名，实际仍包含整个街区，目标没有高亮或隔离；不能作为单项细节已达标的证明。
- `08/15`、`09/13`、`14/16` 在当前结果中分别形成语义重复或近似机位，文件名数量不能替代真实的屋顶、装卸、生活细节与 LOD 证明。
- `FocusGoldenBlockLot(..., near: true)` 使用 `18×36` 个 50m Cell，即约 `900×1,800m` 的视窗范围；应为 V3 增加真正的单院落审阅镜头。
- 当前没有 V3 要求的 neutral silhouette/弱材质证据；该证据必须在 Step 10 人工门前补齐。
- 当前复采集近似 FPS 只作为单次基线，不与 2026-09-04 的旧采集值直接比较，也不据此声称性能回归或通过。

## 5. Step 3 前的范围门禁

总纲已记录 V2 五族表现曾推广到全洛阳，且全县 rollout 当前消费共享 `CountyBuildingPresentationProfileCatalog.HanLuoyangV2`；V3 最新任务又明确在 Golden Block 人工通过前不得继续全县推广。因此 Step 3 不能直接修改共享 V2 Profile 并让全县同步变化。

后续应先把 V3 Profile/selector 限定为 Golden Block 的版本化路径，或用等价隔离方式确保全县继续消费既有 V2。完成该范围审计前不进入 Step 3 实现；本轮没有把任务标记为 `ACCEPTED`。

## 6. 本轮验证

- 正式 Unity 复采集：`PASS`，日志包含 `LUOYANG_GOLDEN_BLOCK_BUILD_MODE_V2_EVIDENCE_READY`；28/28 张截图存在、均为 `1920×1080` 且 SHA-256 互不重复。
- V3 Before 专项校验：`PASS`，四张 Mid/Near 当前/历史图片的 SHA-256 与本文件记录一致，尺寸均为 `1920×1080`；指标 JSON 可解析且与正式采集源文件字节一致。
- 交接与 Step 0—2 文档专项校验：`PASS`，`CURRENT.md`、`INDEX.md` 和本文件均无行尾空格、使用 LF 且有末尾换行。
- `verify-project.ps1 -DocumentationOnly`：退出码 `1`。失败仍只来自任务开始前已有的 `Guangyangmen.fbx.meta`、`Mingtang.fbx.meta`、`NorthPalaceSouthGate.fbx.meta`、`SouthPalace.fbx.meta` 行尾空格；本轮未修改这些资产。
- 编译、Core、Unity EditMode、Unity PlayMode：本轮均未执行；正式截图采集不等同于上述测试。
