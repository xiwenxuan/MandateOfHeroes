# 洛阳 P0 四件套多角度转台审查包 V1 任务书

> 后续状态：用户已接受四件套，四个真实 FBX 已完成 Unity 回读验证并由 `TASK_LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1.md` 激活 `FinalArtApproved=true`。

## 1. 任务目标

承接四件套视觉精修 V2，在不推定用户已经批准美术的前提下，为南宫、明堂、广阳门、北宫南门建立可重复、可逐项切换的多角度审查入口。每座建筑固定提供前斜、后斜、低角三个机位，形成能够检查正面识别、背面轮廓、屋顶层次和门道通透性的实机证据。

## 2. 固定范围

- 只扩展 `Mandate.Presentation` 审查相机、即时 GUI 和测试证据。
- 继续使用四个既有 P0 Review Board Cell，不移动权威 Facility 或历史 Global Cell。
- 不修改模型几何、六材质、Prefab 路径、LOD、锚点、Collider、建设权限、Simulation 或 Save。
- 不把审查动作等同于用户批准；四项 `FinalArtApproved` 必须保持 `false`。
- 不创建独立 FBX/DCC 源，不开始其余 50 个最终资产槽位。

## 3. 审查矩阵

| 建筑 | 前斜视角 | 后斜视角 | 低角视角 |
|---|---|---|---|
| 南宫 | 双朝院正面轴线 | 后殿、侧廊和院落闭合 | 门阶、柱列和重叠檐口 |
| 明堂 | 南阶与三重台 | 后侧台基与重檐轮廓 | 礼殿层高和中心制高点 |
| 广阳门 | 门道、门扇和短瓮城 | 城墙背面与角楼关系 | 门楼、贯通门道和墙体高差 |
| 北宫南门 | 中央门楼、双阙和前阶 | 宫门背面与双阙轮廓 | 门洞、旗杆和屋脊层次 |

## 4. 交互要求

- `P0 SLICE` 继续进入四件套总览。
- P0 审查状态下显示专用控制条：总览、上一/下一建筑、上一/下一角度。
- 建筑与角度索引循环切换，并暴露稳定相机 ID、英文审查标签和当前索引供测试读取。
- 控制条明确显示 `USER DECISION: PENDING` 与 `FINAL ART APPROVAL: FALSE`。

## 5. 自动验收

1. 审查合同固定为 4 座 × 3 角度，共 12 个互异相机 ID。
2. 每座三个机位绑定同一 Review Board Cell；前后斜机位相差 180 度，低角俯角为 28—34 度。
3. 每个机位运行时仍加载四套真实 Prefab，回退未激活，批准标志为 false。
4. 每张近景的建筑包围盒中心位于画面中心，八个角位于 2%—98% 安全画幅。
5. 生成 1 张总览和 12 张 1600×1000 多角度实机截图。
6. 保持既有 V2 五图合同和 549 Facility 全城批处理预算回归通过。

## 6. 状态门禁

自动验收完成后，状态只能进入：

`MULTI_ANGLE_REVIEW_PACK_V1_READY_FOR_USER_DECISION_FINAL_APPROVAL_PENDING`

用户仍需对南宫、明堂、广阳门、北宫南门逐项给出“接受 / 修改 / 否决”。本任务不得代替该决定。

## 7. 实施结果

当前状态为：

`MULTI_ANGLE_REVIEW_PACK_V1_READY_FOR_USER_DECISION_FINAL_APPROVAL_PENDING`

- 建立 `presentation.luoyang.p0-four-piece.multi-angle-review.v1` 审查合同，固定 4 座 × 3 角度、12 个互异稳定相机 ID。
- 四个既有近景机位作为前斜视角保留；新增后斜和低角各 4 个机位，全部继续绑定原 Review Board Cell。
- `P0 SLICE` 总览增加总览、上一/下一建筑、上一/下一角度控制，并在画面中持续显示用户决定与最终批准仍为待定/否。
- 运行时四项均加载真实 Prefab，程序化回退未激活，`FinalArtApproved=false`。
- 已生成 1 张总览和 12 张 1600×1000 近景；逐张人工检查未发现建筑裁切或脱离安全画幅。
- 既有 V2 五视图和最密 549 Facility 全城批处理图形回归保持通过。

## 8. 验收记录

| 门禁 | 结果 |
|---|---|
| 全工程编译 | 通过 |
| 洛阳定向核心合同 | 1/1 通过 |
| 多角度相机合同 EditMode | 2/2 通过 |
| 13 图与运行时加载 PlayMode | 1/1 通过 |
| 既有 V2 五图 PlayMode 回归 | 1/1 通过 |
| 全城批处理图形 PlayMode 回归 | 1/1 通过 |
| `git diff --check` 与范围审阅 | 通过 |

全量核心套件未在本任务中重新宣称通过；这里只运行了直接相关的定向核心合同。

## 9. 证据与复现

- 实机图片与索引：`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_MULTI_ANGLE_TURNTABLE_REVIEW_PACK_V1/README.md`。
- 多角度 EditMode 汇总：`tmp/unity-validation/unity-EditMode-20260827-140508-350.summary.json`。
- 13 图 PlayMode 汇总：`tmp/unity-validation/unity-PlayMode-20260827-140609-570.summary.json`。
- 既有 V2 五图 PlayMode 回归汇总：`tmp/unity-validation/unity-PlayMode-20260827-140937-112.summary.json`。
- 批处理 PlayMode 回归汇总：`tmp/unity-validation/unity-PlayMode-20260827-141044-794.summary.json`。
- 编译日志：`tmp/skill-verification/compile-20260827-140355-659.out.log`。
- 定向核心日志：`tmp/skill-verification/core-tests-20260827-140443-975.out.log`。

## 10. 下一步

后续 `TASK_LUOYANG_P0_FOUR_PIECE_REVIEW_DECISION_BOARD_V1.md` 已形成四张无裁剪决策板，用户于 2026-08-27 回复“接受”并按上下文登记为四件全部接受。`TASK_LUOYANG_P0_FOUR_PIECE_USER_ACCEPTANCE_AND_SOURCE_ARCHIVE_READINESS_V1.md` 已进一步归档 Unity 原生源；独立 DCC/FBX 仍缺失，因此 `FinalArtApproved` 继续为 `false`，其余 50 个槽位也未获得批量替换授权。
