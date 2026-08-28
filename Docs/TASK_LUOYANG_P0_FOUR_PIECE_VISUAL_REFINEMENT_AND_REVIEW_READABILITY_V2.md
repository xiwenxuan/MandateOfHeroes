# 洛阳 P0 四件套视觉精修与审图可读性 V2 任务书

> 后续状态：用户已接受四件套，四个真实 FBX 已完成 Unity 回读验证并由 `TASK_LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1.md` 激活 `FinalArtApproved=true`。

## 1. 任务目标

在 V1 四套 Unity 原生 Prefab 已能稳定加载的基础上，修正实机审图暴露的三个问题：建筑占画面比例偏小、镜头过度俯视、屋檐/门洞/台基层次不足。目标是形成可供用户逐项判断的 V2 审图候选，而不是提前批准最终美术。

## 2. 固定范围

- 只处理南宫、明堂、广阳门、北宫南门四个既有 P0 槽位。
- 保持 Facility、Model、Asset Variant、Profile、Global Cell、史料元数据、建设权限和存档不变。
- 保持三个非空 LOD、稳定锚点、零 Collider、Resources Prefab 优先和程序化回退合同。
- 不开始其余 50 个槽位，不声称考古复原、独立 FBX/DCC 源、手绘贴图或最终美术批准。

## 3. 精修内容

| 对象 | V2 重点 |
|---|---|
| 南宫 | 强化双朝院轴线、前门、院坪、屋脊与檐口层次 |
| 明堂 | 强化方形三重台、四向阶道、重檐礼殿和中心制高点 |
| 广阳门 | 保留贯通门道，强化双墙、门扇、短瓮城与两侧角楼 |
| 北宫南门 | 强化中央门楼、双阙高差、门扇、屋脊和礼仪前阶 |
| 审图镜头 | 近景改为更低俯角、更紧构图，并保留格子关系 |

## 4. 自动验收

1. 四套 Prefab 均由 V2 生成器可重复生成。
2. 每项 LOD0/LOD1/LOD2 Renderer 数量严格递减，LOD0 含该建筑专属细节标记。
3. 每项有三个非空 LOD、完整材质、稳定锚点且无 Collider。
4. 四个近景机位 `Size <= 1.45`、`Pitch` 位于 38—44 度，避免过远和近乎顶视。
5. 运行时四项 `ArtistPrefabLoaded=true`、程序化回退未激活、`FinalArtApproved=false`。
6. 生成一张总览和四张 1600×1000 V2 近景，并完成全城批处理回归。

## 5. 状态门禁

实施与自动验证完成后，状态只能进入：

`REFINED_NATIVE_PREFAB_V2_READY_FOR_USER_REVIEW_FINAL_APPROVAL_PENDING`

只有用户明确接受四张近景，才能另开最终批准与 DCC/FBX 源归档任务。

## 6. 实施结果

当前状态为：

`REFINED_NATIVE_PREFAB_V2_READY_FOR_USER_REVIEW_FINAL_APPROVAL_PENDING`

- 生成器修订已固定为 `luoyang.p0.native-prefab.visual-refinement.v2`，可重复生成 4 个 Prefab、6 个 Material 与 4 个共享 Mesh。
- 四件套 LOD Renderer 总量为 `137 / 37 / 21`，每项均严格递减；专属屋脊、门扇、台阶、铺地、角楼与旗杆标记均由 EditMode 合同锁定。
- 近景相机不再只对准零高度格心，而是按目标建筑实际 Renderer 包围盒中心取景；PlayMode 同时校验包围盒中心位于画面中心、八个边界角位于 2%—98% 安全画幅。
- 运行时四项均为 `ArtistPrefabLoaded=true`、`ProceduralFallbackActive=false`、`FinalArtApproved=false`。
- 已生成一张总览与四张 1600×1000 近景；人工检查确认四项均完整入镜。
- 549 Facility 最密窗口批处理图形回归通过；本轮没有改变全城 LOD2 合批来源与冻结预算。

## 7. 验收记录

| 门禁 | 结果 |
|---|---|
| 全工程编译 | 通过 |
| 洛阳定向核心合同 | 1/1 通过 |
| V2 精修与镜头 EditMode | 2/2 通过 |
| 原生 Prefab 合同 EditMode | 1/1 通过 |
| 既有 P0 数据合同 EditMode | 4/4 通过 |
| 五图与运行时加载 PlayMode | 1/1 通过 |
| 全城批处理图形 PlayMode | 1/1 通过 |
| `git diff --check` | 通过 |

全量核心套件没有在本任务中重新宣称通过；上游 300 秒超时事实继续保留。

## 8. 证据与复现

- 生成菜单：`Mandate/Luoyang/Build P0 Native Prefab Art V2`。
- 运行时资产：`Assets/Resources/Art/Han/Luoyang/P0Final/`。
- 实机图片：`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_VISUAL_REFINEMENT_AND_REVIEW_READABILITY_V2/Screenshots/`。
- V2 EditMode 汇总：`tmp/unity-validation/unity-EditMode-20260827-135336-682.summary.json`。
- 五图 PlayMode 汇总：`tmp/unity-validation/unity-PlayMode-20260827-134938-683.summary.json`。
- 批处理 PlayMode 汇总：`tmp/unity-validation/unity-PlayMode-20260827-135112-707.summary.json`。

## 9. 下一步

后续多角度审查包与四张决策板已经完成，用户于 2026-08-27 接受四件套。`TASK_LUOYANG_P0_FOUR_PIECE_USER_ACCEPTANCE_AND_SOURCE_ARCHIVE_READINESS_V1.md` 已登记用户决定并归档 Unity 原生源；由于四个独立 DCC/FBX 仍未到位，`FinalArtApproved` 继续为 `false`，也不得批量替换其余 50 个槽位。
