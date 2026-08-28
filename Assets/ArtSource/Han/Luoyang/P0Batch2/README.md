# 洛阳 P0 地标第二批最终源说明

- 来源血统任务：`LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1`
- 最终激活任务：`LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1`
- 当前状态：北宫、永安宫、太学、辟雍四件已经用户全部接受并完成最终激活。
- 建筑：北宫、永安宫、太学、辟雍；按全城 P0 评审序号 `1/2/3/5` 选取。
- 来源：项目原创，由仓库内 Unity Editor 参数化生成器创建。
- 生成器：`Assets/Editor/Mandate.Editor/LuoyangP0LandmarkSecondBatchArtBuilder.cs`
- 导出器：`Assets/Editor/Mandate.Editor/LuoyangP0LandmarkSecondBatchFbxExporter.cs`
- 运行时资产：`Assets/Resources/Art/Han/Luoyang/P0Batch2/`
- 许可：项目原创；未复制、转换或仿制任何商业游戏模型、贴图或材质。
- 形态：每件含三档非空 LOD、稳定放置/入口锚点、无碰撞体；FBX 已通过 Unity 回读一致性检查。
- 工具链：Unity FBX Exporter 4.2.1 与 Autodesk FBX SDK Unity 绑定 4.2.1，Unity Companion License。
- 锚点映射：FBX 节点名按官方导出行为将点号转换为下划线，来源清单保留可逆映射。
- 来源清单：`luoyang_p0_landmark_second_batch_source_manifest_v1.json` 记录 54 个候选源/依赖文件及元数据的路径、长度与 SHA-256，并另记 2 个工具链锁定文件。
- 用户决定：`ACCEPTED_ALL_FOUR`，记录为
  `decision.luoyang-p0-landmark-second-batch.accepted.2026-08-27.v1`。
- 审批边界：静态目录四项 `FinalArtApproved=true`；运行时只有真实 Prefab 成功加载时批准为真，
  程序回退实例强制为 `false`。
- 当前来源清单 SHA-256：
  `9b380964802400ef7a96838b758b68be48df8063e0380d7b3712c1301baa3142`。
- 本批准不授权第三批或其余 46 个最终资产槽位。
- 尚未交付：手绘/PBR 贴图、考古复原级细节、室内、碰撞、导航、损毁和最终地形构图。

重新生成 Prefab 时使用 Unity 菜单 `Mandate/Luoyang/Build P0 Landmark Second Batch V1`；重新导出 FBX 时使用 `Mandate/Luoyang/Export P0 Landmark Second Batch FBX V1`。完成后必须重跑 Unity 回读测试并重新生成来源清单。
