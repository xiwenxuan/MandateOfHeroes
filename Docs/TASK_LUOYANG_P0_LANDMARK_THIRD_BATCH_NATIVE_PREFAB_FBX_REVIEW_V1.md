# 洛阳 P0 地标第三批原生 Prefab、FBX 与审模候选 V1 任务书

历史状态：`LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_SOURCE_READY_FOR_USER_REVIEW_V1`

后续当前状态：四项已由用户接受，当前以
`TASK_LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`为准。

## 1. 任务目标

承接首批与第二批共8项最终激活结果，按54槽位冻结清单选取最低剩余P0评审序号`6/7/8/9`：
灵台、太仓、武库、濯龙园。为四项制作项目原创三级LOD Unity原生Prefab、真实FBX、稳定锚点、
运行时热替换、程序回退和五视图审模证据。

用户要求“给出下一步任务书，并执行”按既有顺序解释为授权这个有限第三批进入审模候选生产，不等于
用户已接受模型。四项继续为`FinalArtApproved=false`；不得据此启动第四批或批准其他槽位。

## 2. 冻结选择与历史依据

| 顺序 | 建筑 | Facility ID | 替换槽位 | 权威 Cell | 史料边界与战略轮廓 |
|---:|---|---|---|---:|---|
| 6 | 灵台 | `facility.instance.luoyang.184.lingtai` | `HAN_LANDMARK_LINGTAI_STEPPED_OBSERVATORY_A` | 4,161,107 | HistoricalAnchor / Probable；四级收分观象台 |
| 7 | 太仓 | `facility.instance.luoyang.184.taicang` | `HAN_LANDMARK_TAICANG_FOUR_GRANARIES_A` | 4,134,598 | HistoricalAnchor / Approximate；四廪为容量意象，非实测数量 |
| 8 | 武库 | `facility.instance.luoyang.184.arsenal` | `HAN_LANDMARK_ARSENAL_FORTIFIED_YARD_A` | 4,134,604 | HistoricalAnchor / Approximate；封闭围院表现军械保管属性 |
| 9 | 濯龙园 | `facility.instance.luoyang.184.zhuolong_garden` | `HAN_LANDMARK_ZHUOLONG_GARDEN_POND_PAVILION_A` | 4,101,464 | HistoricalReconstruction / Approximate；池台林苑为可撤销游戏复原 |

身份、Cell、史料置信度和来源均逐项复用A级地标目录和54槽位清单；本任务不创造新的历史断言。

## 3. 冻结边界

- 保持 Facility、Model、Asset Variant、Profile、Global Cell、建设权限和史料元数据不变。
- 每件恰好三个非空且Renderer数量严格递减的LOD，具有放置/入口锚点、完整材质和零Collider。
- 运行时优先加载真实Prefab；资源缺失时回退既有地标程序轮廓。
- 无论真实Prefab还是回退，用户审模前实例`FinalArtApproved=false`。
- 全城远景批处理继续使用稳定地标LOD2模块，不将FBX节点变成世界事实。
- 不修改人口、岗位、物资、产权、控制、Simulation、Save Schema或权威Facility位置。
- 不宣称考古单体复原、手绘/PBR终稿、室内、导航、碰撞、损毁或最终美术批准。

## 4. 实施内容

1. 新增第三批Domain/Persistence目录，严格验证ReviewOrder、身份、Cell、来源、Prefab/FBX路径和审批状态。
2. 制作灵台四级观象台、太仓四廪、武库封闭围院、濯龙园池台林苑四套原创原生Prefab。
3. 复用项目首批六种材质、第二批水体/植被材质及五个基础网格，不新增外部素材。
4. 用Unity FBX Exporter 4.2.1导出四个真实FBX，并回读检查LOD层级、Renderer、锚点和零Collider。
5. 在运行时工厂新增第三批独立身份与状态，不借用第二批标志；接入2×2评审板和五个固定镜头。
6. 输出一张总览和四张1600×1000 Unity Game View，并冻结来源文件、工具链和FBX哈希。

## 5. 模型结果

| 建筑 | 原生Prefab | FBX | LOD0重点 |
|---|---|---|---|
| 灵台 | `P0Batch3/Lingtai.prefab` | `Lingtai.fbx` | 四层收分台、南阶、观测杆、角柱 |
| 太仓 | `P0Batch3/Taicang.prefab` | `Taicang.fbx` | 四个独立仓廪、束带、仓门和南门 |
| 武库 | `P0Batch3/Arsenal.prefab` | `Arsenal.fbx` | 闭合围墙、北库、门楼、军械架和中道 |
| 濯龙园 | `P0Batch3/ZhuolongGarden.prefab` | `ZhuolongGarden.fbx` | 池、桥、亭、园门、路径与三组乔木 |

- 四件LOD Renderer总数为`62 / 33 / 17`，每件均严格递减。
- 四个FBX均大于1 KiB并由Unity重新导入。
- 四项`ArtistPrefabPresent=true`、`FinalArtApproved=false`。

## 6. 来源归档

- 来源清单：
  `Assets/ArtSource/Han/Luoyang/P0Batch3/luoyang_p0_landmark_third_batch_source_manifest_v1.json`。
- 覆盖60个项目源/依赖及`.meta`文件、2个工具链文件和4个真实FBX。
- 清单SHA-256：`8d286a6013c9c83c111c2c57b8e9f3fac071de5d82acdaee8c71cf0243a5d444`。
- 清单连续生成两次哈希一致。
- 模型、材质和网格均为项目原创；FBX工具链为Unity Companion License。

## 7. 验收门禁与执行记录

| 门禁 | 结果 |
|---|---|
| 全工程C#编译 | 通过；`tmp/skill-verification/compile-20260827-184557-367.out.log` |
| 定向核心身份合同 | 1/1通过；`tmp/skill-verification/core-tests-20260827-184647-706.out.log` |
| ProjectLoadSmoke | 首次沙箱启动无日志被安全终止；按规则外部重试通过，`unity-ProjectLoadSmoke-20260827-184520-080.summary.json` |
| 原生Prefab生成与三级LOD合同EditMode | 1/1通过；`unity-EditMode-20260827-184716-732.summary.json` |
| 四FBX导出与Unity回读EditMode | 1/1通过；`unity-EditMode-20260827-184807-407.summary.json` |
| 真实Prefab加载且批准保持false EditMode | 1/1通过；`unity-EditMode-20260827-184847-343.summary.json` |
| 五视图图形PlayMode | 1/1通过；`unity-PlayMode-20260827-184921-687.summary.json` |
| 最密549 Facility批处理图形PlayMode | 1/1通过；`unity-PlayMode-20260827-185549-357.summary.json` |
| 五图人工检查 | 通过；主体完整、身份可辨、无主体裁切；地图Cell线只作比例背景 |
| 来源清单重复生成 | 通过；哈希保持`8d286a6013c9c83c111c2c57b8e9f3fac071de5d82acdaee8c71cf0243a5d444` |
| 来源逐文件哈希、审批边界与`git diff --check` | 通过；60个源/依赖、2个工具链文件逐项匹配，4项均为待审/false |

完整核心、完整EditMode和完整PlayMode套件未运行；上述均为直接相关的定向回归，不得扩写为全量测试通过。

## 8. 证据入口

- 证据索引：
  `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1/README.md`。
- 生成器：`Assets/Editor/Mandate.Editor/LuoyangP0LandmarkThirdBatchArtBuilder.cs`。
- FBX导出器：`Assets/Editor/Mandate.Editor/LuoyangP0LandmarkThirdBatchFbxExporter.cs`。
- 来源说明：`Assets/ArtSource/Han/Luoyang/P0Batch3/README.md`。

## 9. 下一门禁

本任务原计划下一步补强多角度审模证据。用户随后在五视图上下文中明确回复“接受”，按四项全部接受
登记并完成`FinalArtApproved=true`激活；用户的明确决定关闭了这四项的额外审图门禁。第四批和未触及
的42个槽位没有随该决定获得授权。
