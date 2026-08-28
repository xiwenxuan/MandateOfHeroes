# 洛阳 P0 最终资产四件套垂直切片 V1 任务书

> 后续状态：四套 Unity 原生 Prefab、V2 精修、多角度审查和用户接受均已完成；`TASK_LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1.md` 已生成并回读验证四个真实 FBX，四件套现为 `FinalArtApproved=true`。

## 1. 任务结论

本任务把洛阳建筑阶段从“54 个可替换槽位清单”推进到首批四项 P0 实物集成候选：南宫、明堂、广阳门、北宫南门。四项沿用既有设施、模型、史料档案、Global Cell 与 `AssetVariantId`，不修改建造权限、世界模拟或存档。

当前交付状态为：

`INTEGRATION_CANDIDATE_VERIFICATION_PASSED_ARTIST_SOURCE_PENDING_USER_REVIEW`

这表示项目原创的运行时替代候选、三级 LOD、六材质、锚点和美术 Prefab 热替换合同已经实施；项目内仍没有四项经过用户确认的最终 FBX/Prefab/贴图，因此不得称为“最终美术完成”。

## 2. 固定范围

| 建筑 | 设施身份 | 模型身份 | 可替换槽位 | 权威格 |
|---|---|---|---|---|
| 南宫 | `facility.instance.luoyang.184.south_palace` | `model.han.luoyang.palace.complex.v1` | `HAN_LANDMARK_SOUTH_PALACE_DOUBLE_COURT_A` | `(2043,1245)` |
| 明堂 | `facility.instance.luoyang.184.mingtang` | `model.han.luoyang.ritual.hall.v1` | `HAN_LANDMARK_MINGTANG_SQUARE_ALTAR_A` | `(2040,1255)` |
| 广阳门 | `facility.instance.luoyang.184.gate.guangyangmen` | `model.han.buildable.city_gate.segment.v1` | `HAN_LUOYANG_GATE_GUANGYANGMEN_A` | `(2034,1246)` |
| 北宫南门 | `facility.instance.luoyang.184.north_palace_gate.1240.2043` | `model.han.luoyang.fortification.palace_gate.v1` | `HAN_LUOYANG_NORTH_PALACE_SOUTH_GATE_A` | `(2043,1240)` |

## 3. 本轮实施

- 新增机器可读四件套清单，冻结史料置信度、来源、设施权限、模型和替换槽位。
- 每项提供项目原创的高辨识度程序化集成候选、LOD0/LOD1/LOD2 和无 Collider 战略表现。
- 提供夯土、朱红、灰绿瓦、石、木、青铜六种当前运行时材质；它们是候选参数，不是最终贴图资产。
- 运行时优先查找 `Resources/Art/Han/Luoyang/P0Final/*` 美术 Prefab；只有满足三级 LOD、材质、锚点齐全且无 Collider 才允许替换候选。
- 美术替换不改变 `FacilityId`、`ModelId`、`AssetVariantId`、Global Cell 或权限；全城远景合批继续使用已验证的候选 LOD2 模块。
- 新增四件套审图板、总览和四个固定近景机位，并生成可复核截图。

## 4. 美术交付合同

每个最终 Prefab 必须：

1. 放入清单指定的 Resources 路径；源 FBX 存放目标路径也在清单中冻结。
2. 至少包含一个 `LODGroup`，且恰有三个均非空的 LOD。
3. 每个 Renderer 必须绑定材质，不得带 Collider。
4. 包含清单列出的全部稳定锚点名称。
5. 不修改运行时替换槽位；修改身份必须另开数据迁移任务。

## 5. 验收门禁

- 全工程编译通过。
- 定向核心/Unity EditMode 合同测试通过。
- PlayMode 场景集成通过，四项均可渲染、三级 LOD、无 Collider。
- 总览与四项近景截图存在且非空。
- `git diff --check` 通过并完成范围审阅。
- 只有真实美术源到位、Prefab 合同通过并经用户审图接受后，才可将 `FinalArtApproved` 改为 `true`。

## 6. 下一步

美术人员按四个冻结槽位制作或导入真实 FBX、贴图与 Prefab；先逐项通过自动 Prefab 合同，再由用户对总览和四张近景作视觉验收。未完成这一门禁前，不开始其余 50 个槽位的最终资产批量替换。

## 7. 本轮验证记录

- 全工程编译：通过。
- 定向核心合同：1/1 通过。
- 目标 Unity EditMode：4/4 通过。
- 目标 Unity PlayMode（图形）：1/1 通过。
- 受影响全城批处理 Unity PlayMode（图形）：1/1 通过；最密窗口 1,673 个 LOD2 源模块合并为 97 个 Renderer、17,512 顶点，22.9509ms，预算通过。
- 证据：一张总览和四张单体近景，均为 1600×1000。
- `git diff --check`：通过。
- 完整核心套件：在 300 秒硬门禁内未结束，已只清理本轮拥有的超时测试进程；不计为通过，也不影响上述定向结果。
