# 洛阳 P0 地标第二批用户接受与最终激活 V1 任务书

状态：`LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`

## 1. 用户决定与任务目标

2026-08-27，用户在北宫、永安宫、太学、辟雍四件多角度决策板之后明确回复“全部接受”。按当前
审模上下文，本任务将四项统一登记为 `ACCEPTED_ALL_FOUR`，冻结决定记录，并在既有真实 FBX 已通过
Unity 回读门禁的前提下激活四项 `FinalArtApproved=true`。

本批准只表示四件项目原创战略地图资产已通过用户审模、源资产和运行时门禁，不把它们写成考古
单体复原、手绘/PBR 贴图终稿、室内、碰撞、导航或损毁资产，也不授权第三批或其余 46 个槽位。

## 2. 接受范围

| 顺序 | 建筑 | Facility ID | 替换槽位 | 决定 |
|---:|---|---|---|---|
| 1 | 北宫 | `facility.instance.luoyang.184.north_palace` | `HAN_LANDMARK_NORTH_PALACE_TWIN_TOWER_A` | 接受 |
| 2 | 永安宫 | `facility.instance.luoyang.184.yongan_palace` | `HAN_LANDMARK_YONGAN_PALACE_GARDEN_COURT_A` | 接受 |
| 3 | 太学 | `facility.instance.luoyang.184.taixue` | `HAN_LANDMARK_TAIXUE_LECTURE_ROWS_A` | 接受 |
| 5 | 辟雍 | `facility.instance.luoyang.184.biyong` | `HAN_LANDMARK_BIYONG_RING_WATER_A` | 接受 |

决定记录固定为：

- `user_review.luoyang-p0-landmark-second-batch.accepted.v1`
- `decision.luoyang-p0-landmark-second-batch.accepted.2026-08-27.v1`
- 日期：`2026-08-27`

## 3. 冻结边界

- 不改变 Facility、Model、Asset Variant、Profile、Global Cell、史料来源或建设权限。
- 不修改四个 Prefab、FBX、Mesh、Material、三级 LOD、锚点、Collider 或模型外观。
- 不改变人口、岗位、产权、控制、库存、Simulation、Save Schema 或全城批处理语义。
- 四项静态目录可为 `FinalArtApproved=true`；运行时只有真实 Prefab 成功加载时实例批准才为真。
- 资源缺失时继续使用项目原创程序轮廓回退，但该实例必须为 `FinalArtApproved=false`。
- 不自动选择、制作或批准第三批，不修改其余 46 个最终资产槽位。

## 4. 实施内容

1. 在第二批 Domain 与机器目录中记录用户决定 ID、决定日期、最终批准和源归档状态。
2. 将北宫、永安宫、太学、辟雍四项登记为用户接受并最终激活。
3. 保持原任务 ID 作为目录血统，同时新增最终激活任务 ID 供源清单和审计记录使用。
4. 重新生成 54 个源/依赖文件、2 个工具链文件和 4 个 FBX 的来源清单及 SHA-256。
5. 增加运行时回退否决合同、真实 Prefab 最终批准合同和最密 549 Facility 批处理回归。
6. 同步任务书、证据索引、总纲、资源计划、许可登记和任务路由。

## 5. 最终源与批准状态

- 最终激活任务：`LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1`。
- 用户决定：`ACCEPTED_ALL_FOUR`。
- 最终批准状态：`final_art.user_accepted.fbx_source_validated.approved.v1`。
- 源归档状态：`source_archive.unity_native_and_fbx_complete.v1`。
- 工具链：Unity FBX Exporter `4.2.1` 与 Autodesk FBX SDK Unity 绑定 `4.2.1`。
- 来源许可：项目原创模型与 Unity Companion License 工具链；未复制或转换商业游戏资产。
- 最终来源清单：
  `Assets/ArtSource/Han/Luoyang/P0Batch2/luoyang_p0_landmark_second_batch_source_manifest_v1.json`。
- 当前来源清单 SHA-256：
  `9b380964802400ef7a96838b758b68be48df8063e0380d7b3712c1301baa3142`。

## 6. 验收门禁

1. 目录只含冻结的 ReviewOrder `1/2/3/5`，决定记录和四项批准状态一致。
2. 四个 Prefab 和四个大于 1 KiB 的 FBX 均存在。
3. Unity 回读验证三级 LOD、Renderer、材质、锚点映射/位置、几何包围盒和零 Collider。
4. 来源清单覆盖 54 个项目源/依赖文件、2 个工具链文件及 4 个 FBX，磁盘哈希一致。
5. 真实 Prefab 实例加载成功、无程序回退且最终批准为真；强制回退实例最终批准为假。
6. 最密 549 Facility 窗口继续满足批处理预算。
7. 全工程编译、定向核心、受控 Unity 测试、`git diff --check` 和范围审阅分别记录。

## 7. 执行与验证记录

| 门禁 | 结果 |
|---|---|
| 全工程 C# 编译 | 通过；`tmp/skill-verification/compile-20260827-173557-218.out.log` |
| 定向核心合同 | 1/1 通过；`tmp/skill-verification/core-tests-20260827-173603-436.out.log` |
| ProjectLoadSmoke | 通过；`tmp/unity-validation/unity-ProjectLoadSmoke-20260827-173624-285.summary.json` |
| 回退实例不得继承最终批准 EditMode | 1/1 通过；`tmp/unity-validation/unity-EditMode-20260827-173701-837.summary.json` |
| 接受状态、来源哈希与四 FBX 回读 EditMode | 1/1 通过；`tmp/unity-validation/unity-EditMode-20260827-173730-173.summary.json` |
| 四个真实批准 Prefab 五视图 PlayMode | 1/1 通过；`tmp/unity-validation/unity-PlayMode-20260827-173920-061.summary.json` |
| 最密 549 Facility 合批 PlayMode | 1/1 通过；`tmp/unity-validation/unity-PlayMode-20260827-174020-785.summary.json` |
| 来源清单重复生成 | 通过；54 源文件、2 工具链文件、4 FBX，清单哈希保持 `9b380964802400ef7a96838b758b68be48df8063e0380d7b3712c1301baa3142` |
| JSON、56个清单条目哈希与4个FBX大小回验 | 通过；四项批准、56/56磁盘条目一致、FBX均大于1 KiB |
| `git diff --check`、目标文件尾随空白与范围审阅 | 通过；仅报告工作区既有两条换行格式提示 |

首次编译发现新增 EditMode 回退测试缺少 `Mandate.Presentation` 命名空间引用；补齐后重新执行全工程
编译和上述门禁均通过。完整核心套件、完整 EditMode 套件和完整 PlayMode 套件未在本任务中运行，
不得把定向结果扩写为全量回归通过。

## 8. 证据入口

- 本任务证据索引：
  `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1/README.md`。
- 审图与历史决策板：
  `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1/README.md`。
- 第二批源说明：`Assets/ArtSource/Han/Luoyang/P0Batch2/README.md`。
- 来源清单生成器：
  `MapPipeline/scripts/build_luoyang_p0_landmark_second_batch_source_manifest_v1.ps1`。

## 9. 下一步边界

本任务关闭第二批四件套。洛阳现有首批 4 项与第二批 4 项、共 8 个槽位已完成最终激活；剩余 46 个
槽位仍未最终批准。下一批必须另开有限选择任务，重新走史料、建模、来源、审图、用户决定和运行时
门禁，不得由“全部接受”自动推出第三批授权。
