# 洛阳 P0 地标第二批用户接受与最终激活 V1 证据索引

## 状态

`LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`

2026-08-27，用户在第二批多角度决策板之后回复“全部接受”。该决定按上下文登记为北宫、永安宫、
太学、辟雍四项全部接受。四个项目原创 Prefab 与真实 FBX 已通过 Unity 回读门禁，静态
`FinalArtApproved=true` 已激活；程序回退实例继续强制为 false。

## 决定记录

- 决定：`ACCEPTED_ALL_FOUR`
- 状态：`user_review.luoyang-p0-landmark-second-batch.accepted.v1`
- 记录：`decision.luoyang-p0-landmark-second-batch.accepted.2026-08-27.v1`
- 日期：`2026-08-27`

| 建筑 | Prefab | FBX | 最终批准 |
|---|---|---|---|
| 北宫 | `Assets/Resources/Art/Han/Luoyang/P0Batch2/NorthPalace.prefab` | `Assets/ArtSource/Han/Luoyang/P0Batch2/NorthPalace.fbx` | `true` |
| 永安宫 | `Assets/Resources/Art/Han/Luoyang/P0Batch2/YonganPalace.prefab` | `Assets/ArtSource/Han/Luoyang/P0Batch2/YonganPalace.fbx` | `true` |
| 太学 | `Assets/Resources/Art/Han/Luoyang/P0Batch2/Taixue.prefab` | `Assets/ArtSource/Han/Luoyang/P0Batch2/Taixue.fbx` | `true` |
| 辟雍 | `Assets/Resources/Art/Han/Luoyang/P0Batch2/Biyong.prefab` | `Assets/ArtSource/Han/Luoyang/P0Batch2/Biyong.fbx` | `true` |

## 机器证据

- 静态目录：
  `Assets/StreamingAssets/WorldMap/LuoyangP0LandmarkSecondBatchV1/luoyang_p0_landmark_second_batch_v1.json`。
- 最终来源清单：
  `Assets/ArtSource/Han/Luoyang/P0Batch2/luoyang_p0_landmark_second_batch_source_manifest_v1.json`。
- 清单 SHA-256：
  `9b380964802400ef7a96838b758b68be48df8063e0380d7b3712c1301baa3142`。
- 来源清单覆盖 54 个项目源/依赖文件、2 个工具链文件和 4 个真实 FBX。
- 既有多角度图片和决策板保留为接受决定的输入证据：
  `../LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1/README.md`。

## 验证结果

- 全工程编译通过；定向核心 1/1 通过。
- ProjectLoadSmoke 通过。
- 回退批准合同 EditMode 1/1 通过。
- 来源清单、接受合同和四 FBX Unity 回读 EditMode 1/1 通过。
- 四个真实 Prefab 五视图 PlayMode 1/1 通过。
- 最密 549 Facility 合批 PlayMode 1/1 通过。
- 完整核心与完整 Unity 套件未运行，不据此声称全量回归通过。

## 边界

- 本次未改模型、Prefab、FBX、Mesh、Material、LOD、锚点、Collider、Facility、Cell、建设规则、
  Simulation 或 Save Schema。
- 最终批准依赖真实 Prefab；任何程序回退实例均不得继承批准。
- 本批准不代表考古复原或手绘/PBR 终稿，也不授权第三批或其余 46 个槽位。
