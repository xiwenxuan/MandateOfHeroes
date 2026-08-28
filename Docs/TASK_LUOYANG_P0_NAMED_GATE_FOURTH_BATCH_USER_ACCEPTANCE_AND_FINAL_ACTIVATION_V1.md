# 洛阳 P0 命名城门第四批用户接受与最终激活 V1 任务书

状态：`LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`

## 1. 用户决定与任务目标

2026-08-27，用户在谷门、津门、开阳门、旄门的总览和四张近景审模之后明确表示“上一个接受”。
按当前第四批四件上下文，本任务统一登记为`ACCEPTED_ALL_FOUR`，冻结决定记录，并在既有真实FBX
已通过Unity回读的前提下激活四项`FinalArtApproved=true`。

原候选任务计划继续制作多角度审图材料，但用户已基于现有五视图明确作出决定；该决定关闭这四项
的额外审图门禁，不改变候选期图片和`PENDING/false`历史记录，也不授权第五批。

## 2. 接受范围

| 顺序 | 城门 | Facility ID | 替换槽位 | 决定 |
|---:|---|---|---|---|
| 11 | 谷门 | `facility.instance.luoyang.184.gate.gumen` | `HAN_LUOYANG_GATE_GUMEN_A` | 接受 |
| 12 | 津门 | `facility.instance.luoyang.184.gate.jinmen` | `HAN_LUOYANG_GATE_JINMEN_A` | 接受 |
| 13 | 开阳门 | `facility.instance.luoyang.184.gate.kaiyangmen` | `HAN_LUOYANG_GATE_KAIYANGMEN_A` | 接受 |
| 14 | 旄门 | `facility.instance.luoyang.184.gate.maomen` | `HAN_LUOYANG_GATE_MAOMEN_A` | 接受 |

决定记录固定为：

- `user_review.luoyang-p0-named-gate-fourth-batch.accepted.v1`
- `decision.luoyang-p0-named-gate-fourth-batch.accepted.2026-08-27.v1`
- 日期：`2026-08-27`

## 3. 冻结边界

- 不改变Facility、Model、Asset Variant、Profile、Global Cell、史料来源、城门朝向或建设权限。
- 不修改四个Prefab、FBX、Mesh、Material、三级LOD、锚点、Collider或模型外观。
- 不改变人口、岗位、产权、控制、库存、Simulation、Save Schema或全城批处理语义。
- 四项静态目录为`FinalArtApproved=true`；运行时只有真实Prefab成功加载时实例批准才为真。
- 资源缺失时继续使用项目原创程序轮廓回退，但该实例必须为`FinalArtApproved=false`。
- 不自动选择、制作或批准第五批，剩余38个最终资产槽位继续未授权。

## 4. 实施内容

1. 在第四批Domain与机器目录中记录用户决定ID、决定日期、最终批准和源归档状态。
2. 将谷门、津门、开阳门、旄门四项登记为用户接受并最终激活。
3. 保持原候选任务ID作为源血统，新增最终激活任务ID供来源清单与审计记录使用。
4. 重新导出并回读4个真实FBX，冻结56个源/依赖文件、2个工具链文件和4个FBX的SHA-256。
5. 验证运行时真实Prefab批准、程序回退否决与最密549 Facility批处理合同。
6. 同步任务书、证据索引、总纲、资源计划、许可登记和任务路由。

## 5. 最终源与批准状态

- 最终激活任务：`LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1`。
- 用户决定：`ACCEPTED_ALL_FOUR`。
- 最终批准状态：`final_art.user_accepted.fbx_source_validated.approved.v1`。
- 源归档状态：`source_archive.unity_native_and_fbx_complete.v1`。
- 来源清单：
  `Assets/ArtSource/Han/Luoyang/P0Batch4/luoyang_p0_named_gate_fourth_batch_source_manifest_v1.json`。
- 清单SHA-256：`20c8981a1597314a38a4e211e3a970f22875534d35c48ade33e2b317aaf9c87b`。
- 工具链：Unity FBX Exporter与Autodesk FBX SDK Unity绑定均为`4.2.1`，Unity Companion License。

## 6. 验收门禁

1. 目录只含ReviewOrder`11/12/13/14`，决定记录与四项批准状态一致。
2. 四个Prefab和四个大于1 KiB的FBX均存在。
3. Unity回读验证三级LOD、Renderer、材质、锚点映射和零Collider。
4. 来源清单覆盖56个源/依赖文件、2个工具链文件及4个FBX，磁盘哈希一致。
5. 真实Prefab实例加载成功、无程序回退且最终批准为真；强制回退实例最终批准为假。
6. 最密549 Facility窗口继续满足批处理预算。
7. 全工程编译、定向核心、受控Unity测试、`git diff --check`和范围审阅分别记录。

## 7. 执行与验证记录

| 门禁 | 结果 |
|---|---|
| 全工程C#编译 | 通过；`tmp/skill-verification/compile-20260827-205614-090.out.log` |
| 定向核心接受合同 | 1/1通过；`tmp/skill-verification/core-tests-20260827-205704-384.out.log` |
| ProjectLoadSmoke | 通过；`tmp/unity-validation/unity-ProjectLoadSmoke-20260827-205545-572.summary.json` |
| 四FBX重新导出与Unity回读EditMode | 1/1通过；`unity-EditMode-20260827-205733-629.summary.json` |
| 真实Prefab批准且不回退EditMode | 1/1通过；`unity-EditMode-20260827-205812-253.summary.json` |
| 程序回退不得继承最终批准EditMode | 1/1通过；`unity-EditMode-20260827-205835-573.summary.json` |
| 接受记录、56源文件哈希与四FBX门禁EditMode | 1/1通过；`unity-EditMode-20260827-205859-103.summary.json` |
| 既有多角度相机合同EditMode | 2/2通过；`unity-EditMode-20260827-205937-560.summary.json` |
| 四个真实批准Prefab五视图PlayMode | 1/1通过；`unity-PlayMode-20260827-210026-430.summary.json` |
| 最密549 Facility批处理PlayMode | 1/1通过；`unity-PlayMode-20260827-210138-108.summary.json` |
| 来源清单重复生成 | 通过；哈希三次保持`20c8981a1597314a38a4e211e3a970f22875534d35c48ade33e2b317aaf9c87b` |

多角度相机合同测试已产生完整XML且2/2通过；Unity未在15秒自然退出宽限期内结束，安全包装器只终止了
本次启动的进程树，未留下Unity进程。完整核心、完整EditMode和完整PlayMode套件未运行。

## 8. 证据入口

- 最终激活证据：
  `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1/README.md`。
- 历史审图输入：
  `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1/README.md`。
- 来源说明：`Assets/ArtSource/Han/Luoyang/P0Batch4/README.md`。

## 9. 下一步边界

本任务只关闭第四批四座命名城门。洛阳首批、第二批、第三批、第四批共16个槽位最终激活，剩余
38个槽位仍未最终批准。第五批必须另开有限选择任务，重新走史料、建模、来源、审图、用户决定和
运行时门禁。
