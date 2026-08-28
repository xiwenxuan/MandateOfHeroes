# 洛阳 P0 地标第二批原生 Prefab、FBX 与审模证据 V1

状态：`LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_SOURCE_READY_FOR_USER_REVIEW_V1`

> 本索引记录用户接受前的源就绪证据。用户已于2026-08-27回复“全部接受”，当前最终状态见
> `../LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1/README.md`；下文false状态
> 与旧哈希按历史时点保留。

本证据包对应北宫、永安宫、太学、辟雍四个项目原创战略地图候选。它们按全城最终资产清单中最低的
剩余P0评审序号`1/2/3/5`选取，不改变既有Facility、Model、Asset Variant、Global Cell、历史来源、
建设权限、模拟或存档。

## 交付

- 任务书：`Docs/TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md`
- 机器目录：`Assets/StreamingAssets/WorldMap/LuoyangP0LandmarkSecondBatchV1/luoyang_p0_landmark_second_batch_v1.json`
- 原生Prefab：`Assets/Resources/Art/Han/Luoyang/P0Batch2/`
- FBX与来源清单：`Assets/ArtSource/Han/Luoyang/P0Batch2/`
- 总览：`Screenshots/luoyang_p0_landmark_batch2_overview_v1.png`
- 近景：`Screenshots/luoyang_p0_north_palace_candidate_v1.png`、
  `luoyang_p0_yongan_palace_candidate_v1.png`、`luoyang_p0_taixue_candidate_v1.png`、
  `luoyang_p0_biyong_candidate_v1.png`

## 验证结论

- 四个Prefab与四个FBX均具备三个非空LOD、材质、可逆锚点映射和零Collider；Unity重新导入后
  层级、Renderer数量、锚点位置和包围盒与原生Prefab一致。
- 运行时四件均加载真实Prefab，程序化替身未激活；`FinalArtApproved=false`。
- 五张截图均为Unity实际Game View，1600×1000，无生成式重绘。后续多角度决策板任务已在平缓
  PreviewOnly评审Cell重新生成五图，关闭太学与辟雍主体的地形线遮挡项；四件仍未获最终批准。
- 来源清单记录54个候选源/依赖文件、2个工具链锁定文件与4个FBX；清单SHA-256为
  `3adea5941eea4bda596040a13eb10f42215807a844655db7a0fbaec73fbd5eba`。
- 全工程编译、定向核心、目标EditMode、第二批图形PlayMode、最密549 Facility批处理回归及首批
  最终资产运行时回归均通过；详细结果路径记录在任务书执行记录与`tmp/unity-validation/`。

## 用户门禁

当前请从相邻证据包
`../LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1/README.md`逐件给出
“接受 / 修改 / 否决”。只有明确接受的项才允许进入最终源冻结和`FinalArtApproved=true`激活；
本证据包不授权第三批或其余46槽位。

该用户门禁现已关闭：四项全部接受并完成最终激活。第三批仍未授权。
