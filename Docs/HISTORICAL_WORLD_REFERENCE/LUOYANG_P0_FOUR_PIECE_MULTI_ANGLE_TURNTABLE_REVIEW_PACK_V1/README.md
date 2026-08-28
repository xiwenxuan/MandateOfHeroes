# 洛阳 P0 四件套多角度转台审查包 V1

本目录保存 Unity 2022.3.62f3c1 图形 PlayMode 从真实运行时 Prefab 生成的 1600×1000 Game View。图片用于用户逐件审模；该阶段尚未批准。后续用户已经接受四件套，四个 FBX 已通过 Unity 回读验证并激活 `FinalArtApproved=true`，当前入口为 `../LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1/README.md`。

## 总览

- `Screenshots/luoyang_p0_multi_angle_overview_v1.png`

## 南宫

- 前斜：`Screenshots/luoyang_p0_south_palace_front_oblique_v1.png`
- 后斜：`Screenshots/luoyang_p0_south_palace_rear_oblique_v1.png`
- 低角：`Screenshots/luoyang_p0_south_palace_low_oblique_v1.png`

## 明堂

- 前斜：`Screenshots/luoyang_p0_mingtang_front_oblique_v1.png`
- 后斜：`Screenshots/luoyang_p0_mingtang_rear_oblique_v1.png`
- 低角：`Screenshots/luoyang_p0_mingtang_low_oblique_v1.png`

## 广阳门

- 前斜：`Screenshots/luoyang_p0_guangyangmen_front_oblique_v1.png`
- 后斜：`Screenshots/luoyang_p0_guangyangmen_rear_oblique_v1.png`
- 低角：`Screenshots/luoyang_p0_guangyangmen_low_oblique_v1.png`

## 北宫南门

- 前斜：`Screenshots/luoyang_p0_north_palace_south_gate_front_oblique_v1.png`
- 后斜：`Screenshots/luoyang_p0_north_palace_south_gate_rear_oblique_v1.png`
- 低角：`Screenshots/luoyang_p0_north_palace_south_gate_low_oblique_v1.png`

## 验证边界

- PlayMode 校验运行时加载四套真实 Prefab，程序化回退未激活。
- 12 个近景均校验建筑包围盒中心处于画面中央、八个边界角位于 2%—98% 安全画幅。
- 本包没有修改建筑几何、材质、Prefab、LOD、锚点、Collider、Facility、Global Cell、建设权限、Simulation 或 Save。
- 下一门禁是用户对四件套逐项给出“接受 / 修改 / 否决”，不是自动把候选提升为最终资产。

为便于逐件横向比较，后续决策板位于 `../LUOYANG_P0_FOUR_PIECE_REVIEW_DECISION_BOARD_V1/README.md`；它只对本目录十二张近景做无裁剪排版，不改变原始证据。
