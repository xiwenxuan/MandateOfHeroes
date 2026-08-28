# TASK：洛阳人物尺度近景地图与局部导航 V1

## 1. 任务状态

```text
Task ID: TASK_LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1
Baseline: e0ab8740d33763d5bb88fd1414a2e224ed8200c3
Branch: codex/m23-p4-quality-artisan-growth
Unity: 2022.3.62f3c1
Save schema: V77
Map version: luoyang.local-map.master.v1
Current status: IMPLEMENTED_CORE_VERIFIED_UNITY_ENVIRONMENT_BLOCKED
Formal acceptance: NOT ACCEPTED
```

本任务把同一正式洛阳世界展开为人物尺度空间表达。它不建立第二张洛阳地图，不建立
SubCell Simulation，也不复制 Person、Facility、Road、Gate、Bridge、Inventory 或
WorldTime。Local 层只保存/派生空间精度、通行拓扑和表现加载信息，正式世界状态仍由既有
Domain、Simulation 与 Persistence 合同管理。

## 2. Existing Spatial Capability Audit

| 分类 | 结论 |
|---|---|
| REUSE | Global Cell、Facility、M26 Player Person、V76 MovePersonCommand、WorldTime、道路/门桥状态、正式存档和重放 |
| GENERALIZE | M26-P5B/V68 城镇坐标、占地和统一 Settlement/Facility 空间身份，通过单一兼容投影合同用于洛阳 |
| EXTEND | Person Location 增加局部精度、局部锚点与厘米坐标；正式移动快照增加局部路段；V76 顺序迁移至 V77 |
| NEW | 只新增派生 LocalSpace、Access、Footprint、Local Navigation、合法跨 Cell Transition、坐标换算和 3×3 表现 Streaming |

冻结结论：`LocalSpace != Cell`，`LocalNode != Cell`，Local Graph 不拥有道路、城门、桥梁或
Facility 的生命周期/状态。Unity Transform 和 NavMesh 也不是 Person Location 或正式距离的
权威来源。

## 3. 已实现范围

- 5,980 个派生 LocalSpace 和稳定 SHA-256 地图摘要；
- 2,084/2,084 Facility 的人物尺度 Capability、Anchor 与 Footprint；
- Building、Road、Gate、Bridge、Wall、Moat/Water、Open Area、Productive Land 八类空间合同；
- Primary Road、Secondary Road、Alley、Facility Access 与显式路口拓扑；
- Gate/Bridge Passage、正式状态实时读取和合法跨 Cell Transition；
- Ground、Road、Facility、Gate、Bridge 到正式 LocalTarget 的解析；
- 既有 `MovePersonCommand` 的局部路线接入、时间/体力/口粮结算和 Person 局部位置提交；
- V76→V77 顺序迁移，不从旧存档虚构局部坐标；
- 局部位置、移动中、跨 Cell、城门等待、桥梁中断的存读档与三次重放覆盖；
- 3×3 Cell Unity 表现 Streaming 实现，包括地形、道路 Mesh/Collider、阻挡占地与点击代理；
- Streaming 只装卸表现对象，不重建或卸载正式世界事实。

## 4. 明确不做

- 全洛阳 NPC GameObject/NavMeshAgent 常驻；
- 室内、最终角色动画、最终 PBR/FBX、美术终验；
- 新库存、生产、物流、市场或外围供应系统；
- 通过 Unity 位置反写世界路线或结算；
- 全国人物尺度地图。

## 5. 验收策略

执行顺序固定为：全工程编译、核心回归、受控 Unity EditMode/PlayMode、
`git diff --check`、差异审阅。Unity 只能通过 `Tools/Run-UnityTestsSafe.ps1`，普通工具保持
300 秒上限；仅两个已批准的历史确定性慢测使用 900 秒独立上限。

当前全工程编译、专项核心 19/19、完整核心 766/766 和差异检查已通过。Unity 进程能够创建，
但在 45 秒启动门禁内没有生成任何非空启动日志；安全入口只终止了各自任务 PID。因此 Unity
测试没有实际执行，不能将本任务标为 ACCEPTED。详细结果见
[`LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1_ACCEPTANCE_REPORT.md`](LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1_ACCEPTANCE_REPORT.md)。

## 6. 后续顺序

1. 排除 Unity 2022.3.62f3c1 无启动日志的本机环境门禁；
2. 重新取得本任务 EditMode、PlayMode、场景/点击/Streaming 和 Unity 性能证据；
3. 只有本任务达到 ACCEPTED 后，才进入“食品库存守恒差额 RCA 与修复”；
4. 食品账重新严格守恒后，再进入“洛阳外围供应区与城市物流 V1”。
