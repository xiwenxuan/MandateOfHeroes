# 洛阳 P0 地标第二批多角度审模与决策对照板 V1

状态：`LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_DECISION_BOARDS_READY_FOR_USER_DECISION_V1`

> 本证据包是用户决定前的历史输入。用户已于2026-08-27回复“全部接受”，当前最终状态见
> `../LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1/README.md`。四张板和机器
> 清单继续保留生成时的`PENDING/false`，不回写历史证据。

本证据包为北宫、永安宫、太学、辟雍四个第二批候选提供总览、每件前斜/后斜/低角三视图和逐件
决策板。所有图像均来自 Unity 1600×1000 Game View；决策板只作等比例缩放和排版，没有裁剪、
调色、补画或生成式修改。

## 审图入口

- [四件总览](Screenshots/luoyang_p0_batch2_multi_angle_overview_v1.png)
- [北宫决策板](Boards/luoyang_p0_batch2_north_palace_review_decision_board_v1.png)
- [永安宫决策板](Boards/luoyang_p0_batch2_yongan_palace_review_decision_board_v1.png)
- [太学决策板](Boards/luoyang_p0_batch2_taixue_review_decision_board_v1.png)
- [辟雍决策板](Boards/luoyang_p0_batch2_biyong_review_decision_board_v1.png)
- [12 图与 4 板机器清单](Machine/luoyang_p0_landmark_second_batch_review_decision_board_manifest_v1.json)

## 视觉检查结论

- 四件主体在全部十二个近景中完整落入安全画幅，包围盒中心保持在画面中央 48%—52%。
- 中心视线没有先撞到地形；原五视图中太学、辟雍主体被地形线遮挡的问题已经关闭。
- 北宫双阙高台、永安宫园池偏院、太学列堂院落、辟雍环水桥轴均能在前后与低角视图中辨认。
- 地图 Cell 线仍作为审模比例背景保留，但不穿过建筑主体；这不等于最终地表美术批准。

## 机器与测试证据

- 多角度合同：`presentation.luoyang.p0-landmark-second-batch.multi-angle-review.v1`，4 件 × 3 角度，
  12 个互异稳定相机 ID。
- 决策板合同：`presentation.luoyang.p0-landmark-second-batch.review-decision-board.v1`，12 个输入、
  4 个 3000×900 输出，四项均为 `PENDING` 且 `final_art_approved=false`。
- 决策板脚本连续生成两遍后，4 张 PNG 与 JSON 清单共 5 个文件的 SHA-256 全部一致。
- 决策板机器清单 SHA-256 为
  `8e56e063b483f09c80869f6b473c85b0791dfd9d924a5294578d18a4cb3518a7`。
- 全工程编译、定向核心 1/1、多角度 EditMode 2/2、13 图 PlayMode 1/1、既有第二批五视图
  PlayMode 1/1 和最密 549 Facility 批处理 PlayMode 1/1 通过。
- 第二批原模型来源清单 SHA-256 仍为
  `3adea5941eea4bda596040a13eb10f42215807a844655db7a0fbaec73fbd5eba`；本任务没有修改模型、
  Prefab、FBX、材质、LOD、锚点或 Collider。

## 用户裁决模板

```text
北宫：接受 / 修改 / 否决（修改意见：）
永安宫：接受 / 修改 / 否决（修改意见：）
太学：接受 / 修改 / 否决（修改意见：）
辟雍：接受 / 修改 / 否决（修改意见：）
```

只有明确接受的项目才允许进入最终批准登记。当前四项仍为 `FinalArtApproved=false`，本证据包不
授权第三批或其余 46 个槽位。

上述“当前”指本证据包生成时点；后续用户已全部接受，四项已在最终激活任务中进入
`FinalArtApproved=true`。第三批和其余46个槽位仍未授权。
