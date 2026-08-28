# 洛阳 P0 地标第三批用户接受与最终激活 V1 任务书

状态：`LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`

## 1. 用户决定与任务目标

2026-08-27，用户在灵台、太仓、武库、濯龙园的一张总览和四张近景之后明确回复“接受”。按当前
四件审模上下文，本任务统一登记为`ACCEPTED_ALL_FOUR`，冻结决定记录，并在既有真实FBX已经通过
Unity回读的前提下激活四项`FinalArtApproved=true`。

原候选任务计划继续制作多角度决策板，但用户已基于现有五视图明确作出决定；该明确决定关闭这四项
的额外审图门禁，不改变候选期图片和`PENDING/false`历史记录，也不授权第四批。

## 2. 接受范围

| 顺序 | 建筑 | Facility ID | 替换槽位 | 决定 |
|---:|---|---|---|---|
| 6 | 灵台 | `facility.instance.luoyang.184.lingtai` | `HAN_LANDMARK_LINGTAI_STEPPED_OBSERVATORY_A` | 接受 |
| 7 | 太仓 | `facility.instance.luoyang.184.taicang` | `HAN_LANDMARK_TAICANG_FOUR_GRANARIES_A` | 接受 |
| 8 | 武库 | `facility.instance.luoyang.184.arsenal` | `HAN_LANDMARK_ARSENAL_FORTIFIED_YARD_A` | 接受 |
| 9 | 濯龙园 | `facility.instance.luoyang.184.zhuolong_garden` | `HAN_LANDMARK_ZHUOLONG_GARDEN_POND_PAVILION_A` | 接受 |

决定记录固定为：

- `user_review.luoyang-p0-landmark-third-batch.accepted.v1`
- `decision.luoyang-p0-landmark-third-batch.accepted.2026-08-27.v1`
- 日期：`2026-08-27`

## 3. 冻结边界

- 不改变Facility、Model、Asset Variant、Profile、Global Cell、史料来源或建设权限。
- 不修改四个Prefab、FBX、Mesh、Material、三级LOD、锚点、Collider或模型外观。
- 不改变人口、岗位、产权、控制、库存、Simulation、Save Schema或全城批处理语义。
- 四项静态目录为`FinalArtApproved=true`；运行时只有真实Prefab成功加载时实例批准才为真。
- 资源缺失时继续使用项目原创程序轮廓回退，但该实例必须为`FinalArtApproved=false`。
- 不自动选择、制作或批准第四批，剩余42个最终资产槽位继续未授权。

## 4. 实施内容

1. 在第三批Domain与机器目录中记录用户决定ID、决定日期、最终批准和源归档状态。
2. 将灵台、太仓、武库、濯龙园四项登记为用户接受并最终激活。
3. 保持原任务ID作为源血统，新增最终激活任务ID供来源清单与审计记录使用。
4. 重新导出并回读4个真实FBX，冻结60个源/依赖文件、2个工具链文件和4个FBX的SHA-256。
5. 验证运行时真实Prefab批准、程序回退否决与最密549 Facility批处理合同。
6. 同步任务书、证据索引、总纲、资源计划、许可登记和任务路由。

## 5. 最终源与批准状态

- 最终激活任务：`LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1`。
- 用户决定：`ACCEPTED_ALL_FOUR`。
- 最终批准状态：`final_art.user_accepted.fbx_source_validated.approved.v1`。
- 源归档状态：`source_archive.unity_native_and_fbx_complete.v1`。
- 来源清单：
  `Assets/ArtSource/Han/Luoyang/P0Batch3/luoyang_p0_landmark_third_batch_source_manifest_v1.json`。
- 清单SHA-256：`40e1ccad3af83e9b16119df73b435bc2ae1d9b46c97af9db5087904a53fc50c2`。
- 工具链：Unity FBX Exporter与Autodesk FBX SDK Unity绑定均为`4.2.1`，Unity Companion License。

## 6. 验收门禁

1. 目录只含ReviewOrder`6/7/8/9`，决定记录与四项批准状态一致。
2. 四个Prefab和四个大于1 KiB的FBX均存在。
3. Unity回读验证三级LOD、Renderer、材质、锚点映射和零Collider。
4. 来源清单覆盖60个源/依赖文件、2个工具链文件及4个FBX，磁盘哈希一致。
5. 真实Prefab实例加载成功、无程序回退且最终批准为真；强制回退实例最终批准为假。
6. 最密549 Facility窗口继续满足批处理预算。
7. 全工程编译、定向核心、受控Unity测试、`git diff --check`和范围审阅分别记录。

## 7. 执行与验证记录

| 门禁 | 结果 |
|---|---|
| 全工程C#编译 | 通过；`tmp/skill-verification/compile-20260827-193144-829.out.log` |
| 定向核心接受合同 | 1/1通过；`tmp/skill-verification/core-tests-20260827-193150-957.out.log` |
| ProjectLoadSmoke | 通过；`tmp/unity-validation/unity-ProjectLoadSmoke-20260827-193232-418.summary.json` |
| 四FBX重新导出与Unity回读EditMode | 1/1通过；`unity-EditMode-20260827-193321-237.summary.json` |
| 程序回退不得继承最终批准EditMode | 1/1通过；`unity-EditMode-20260827-193416-772.summary.json` |
| 接受记录、60源文件哈希与四FBX门禁EditMode | 1/1通过；`unity-EditMode-20260827-193451-302.summary.json` |
| 四个真实批准Prefab五视图PlayMode | 1/1通过；`unity-PlayMode-20260827-193607-877.summary.json` |
| 最密549 Facility批处理PlayMode | 1/1通过；`unity-PlayMode-20260827-193725-111.summary.json` |
| 来源清单重复生成 | 通过；当前哈希保持`40e1ccad3af83e9b16119df73b435bc2ae1d9b46c97af9db5087904a53fc50c2` |
| 62个清单条目、FBX大小、审批边界与`git diff --check` | 通过；62/62磁盘哈希一致，4项接受/批准，目标文件无尾随空白 |

首次定向核心调用误把类型全名加入只接受方法名的精确过滤器，得到`passed=0 failed=0`，已用实际方法名
重跑1/1通过；首次五视图调用漏写`.PlayMode`命名空间，结果为零执行测试，已用完整名称重跑1/1通过。
两次零执行结果均不计为通过。完整核心、完整EditMode和完整PlayMode套件未运行。

## 8. 证据入口

- 最终激活证据：
  `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1/README.md`。
- 历史审图输入：
  `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1/README.md`。
- 来源说明：`Assets/ArtSource/Han/Luoyang/P0Batch3/README.md`。

## 9. 下一步边界

本任务关闭第三批四件套。洛阳首批、第二批、第三批共12个槽位已最终激活，剩余42个槽位仍未最终
批准。第四批必须另开有限选择任务，重新走史料、建模、来源、审图、用户决定和运行时门禁。
