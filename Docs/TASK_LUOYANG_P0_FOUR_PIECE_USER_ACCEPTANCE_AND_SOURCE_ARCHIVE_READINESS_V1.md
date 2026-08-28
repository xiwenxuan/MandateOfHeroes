# 洛阳 P0 四件套用户接受登记与源资产归档就绪 V1 任务书

> 后续状态：`TASK_LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1.md` 已生成并回读验证四个真实 FBX，四件套现已进入 `FinalArtApproved=true`；本任务保留为最终激活前的源缺口历史记录。

## 1. 用户决定与任务目标

2026-08-27，用户在审阅南宫、明堂、广阳门、北宫南门四张三视图决策板后回复“接受”。结合上一轮明确要求逐件判断四件套的上下文，本任务将该回复登记为四件全部接受。

本任务承接用户接受决定，建立可审计的 Unity 原生源资产归档清单，并准确区分：

- `UserReviewDecision=ACCEPTED_ALL_FOUR`：视觉审模已经通过；
- `FinalArtApproved=false`：旧任务书要求的独立 DCC/FBX 源仍未到位，最终运行时批准尚不能激活。

## 2. 固定范围

- 只处理南宫、明堂、广阳门、北宫南门四个既有 P0 槽位。
- 保持 Facility、Model、Asset Variant、Profile、Global Cell、史料元数据、建设权限、Simulation 和 Save 不变。
- 保持四套 V2 Unity 原生 Prefab、六材质、四共享网格、三级 LOD、稳定锚点和程序化回退不变。
- 在静态内容合同中登记用户已接受、接受日期、决策记录 ID 和源归档状态。
- 对生成器、目录、Prefab、Material、Mesh 及其 `.meta` GUID 文件建立 SHA-256 清单。
- 审计四个冻结 FBX 目标路径；不存在时必须记录 `MISSING_REQUIRED_FOR_FINAL_ART_ACTIVATION`，禁止生成空文件或冒充 DCC 源。
- 不修改存档版本，不开始其余 50 个最终资产槽位。

## 3. 状态分层

| 状态 | 本任务结果 |
|---|---|
| 用户审模决定 | 四件全部接受 |
| Unity 原生源资产 | 在位并建立哈希归档 |
| 项目原创许可 | 在位 |
| 独立 DCC/FBX | 缺失 |
| 运行时 `FinalArtApproved` | 保持 `false` |
| 其余 50 槽位批量替换 | 未授权 |

## 4. 交付物

1. 用户接受与源归档状态字段及严格内容校验。
2. 可重复生成的源资产 SHA-256 清单脚本。
3. Unity 原生资产、`.meta` GUID 与四个 FBX 目标的机器清单。
4. 运行时和审图 UI 的“用户已接受 / 最终批准待源文件”状态。
5. EditMode/PlayMode 回归，以及总纲、资源计划、旧任务兼容说明和任务路由更新。

## 5. 自动验收

1. 内容合同固定四项用户决定为接受，日期与决策记录 ID 非空。
2. 四项 `CandidateStatusId` 进入用户已接受、源归档待完成状态。
3. 四项 `ArtistPrefabPresent=true`、`FinalArtApproved=false`。
4. 源清单恰好覆盖生成器、P0 目录、4 Prefab、6 Material、4 Mesh 及各自 `.meta`，共 32 个在位文件；路径唯一、哈希与磁盘一致。
5. 四个冻结 FBX 目标均被明确审计，当前缺失数量为 4；不得生成伪 FBX。
6. Runtime 四套真实 Prefab 继续加载，程序化回退不激活，界面显示用户已接受但最终批准仍为 false。
7. 全工程编译、定向核心、目标 Unity 测试、批处理回归、`git diff --check` 与范围审阅通过。

## 6. 状态门禁

实施和自动验证完成后，状态只能进入：

`LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_UNITY_NATIVE_SOURCE_ARCHIVED_INDEPENDENT_DCC_FBX_REQUIRED_FINAL_ACTIVATION_PENDING`

只有四个真实、可编辑且与当前获批 Prefab 对应的独立 DCC/FBX 源到位并通过一致性验证，才允许另开最终激活任务，把四项 `FinalArtApproved` 改为 `true`。若项目决定永久以 Unity 原生生成器作为最终源而取消 DCC/FBX 强制要求，必须由用户另行明确改变该旧门禁。

## 7. 实施结果

当前状态为：

`LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_UNITY_NATIVE_SOURCE_ARCHIVED_INDEPENDENT_DCC_FBX_REQUIRED_FINAL_ACTIVATION_PENDING`

- P0 静态内容合同新增用户决定状态、决定记录 ID、决定日期和源归档状态；四个候选均进入 `candidate.native_prefab_refined_v2.user_accepted.source_archive_pending`。
- 运行时和 P0 审图条显示 `USER DECISION: ACCEPTED`，四项 `FinalArtApproved` 继续为 `false`。
- `build_luoyang_p0_source_archive_manifest_v1.ps1` 已归档生成器、P0 目录、4 Prefab、6 Material、4 Mesh 及其 `.meta`，共 32 个文件的路径、长度与 SHA-256。
- 四个冻结 FBX 目标路径均不存在，机器清单逐项记录 `MISSING_REQUIRED_FOR_FINAL_ART_ACTIVATION`；本任务没有创建空 FBX 或把 Unity Prefab 冒充 DCC 源。
- 原生 Prefab 重建后重新生成清单，连续两次清单 SHA-256 一致。
- Facility、Model、Asset Variant、Profile、Global Cell、史料、权限、Simulation 和 Save 均未改变。

## 8. 验收记录

| 门禁 | 结果 |
|---|---|
| 全工程 C# 编译 | 通过 |
| 定向核心合同 | 1/1 通过 |
| 原生 Prefab 重建合同 EditMode | 1/1 通过 |
| 用户接受与源清单 EditMode | 1/1 通过 |
| 既有 P0 身份/LOD/回退 EditMode | 4/4 通过 |
| 13 视图运行时加载图形 PlayMode | 1/1 通过 |
| 最密 549 Facility 批处理图形 PlayMode | 1/1 通过 |
| 源清单确定性 | 连续生成哈希一致 |
| `git diff --check`、尾随空白与范围审阅 | 通过 |

完整核心套件未在本任务中重新宣称通过；这里只有直接相关的定向核心合同。

## 9. 证据与复现

- 交付索引：`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_USER_ACCEPTANCE_AND_SOURCE_ARCHIVE_READINESS_V1/README.md`。
- 源归档脚本：`MapPipeline/scripts/build_luoyang_p0_source_archive_manifest_v1.ps1`。
- 源归档清单：`Assets/ArtSource/Han/Luoyang/P0Final/luoyang_p0_source_archive_manifest_v1.json`。
- 编译日志：`tmp/skill-verification/compile-20260827-145112-133.out.log`。
- 核心日志：`tmp/skill-verification/core-tests-20260827-145200-430.out.log`。
- 原生 Prefab EditMode：`tmp/unity-validation/unity-EditMode-20260827-145321-215.summary.json`。
- 源清单 EditMode：`tmp/unity-validation/unity-EditMode-20260827-145516-184.summary.json`。
- 既有 P0 EditMode：`tmp/unity-validation/unity-EditMode-20260827-145609-849.summary.json`。
- 多角度 PlayMode：`tmp/unity-validation/unity-PlayMode-20260827-145648-566.summary.json`。
- 批处理 PlayMode：`tmp/unity-validation/unity-PlayMode-20260827-145839-487.summary.json`。

## 10. 下一步

当前唯一剩余门禁是四个独立 DCC/FBX 源文件。可选择由具备 Blender/Maya/3ds Max 或 Unity FBX Exporter 的环境，按冻结路径制作、导出并执行 Prefab/FBX 几何、材质、LOD、锚点一致性验证；本机当前未发现 Blender、Assimp、FBX 转换器或 Unity FBX Exporter 包。
