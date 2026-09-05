# 实施报告：M/C/F 三主视角尺度收口与县域主沙盘 Presentation LOD 重构 V1

## 1. 当前结论

状态：`IMPLEMENTED / FULL CORE PASSED / LIVE UNITY LOAD PASSED / SAFE UNITY TESTS BLOCKED`

本轮已经完成代码实施、全工程编译、定向 Core 与 956 项完整 Core 回归。2026-09-04 又修复了
验收菜单的 Editor 程序集编译错误，并在用户当前 Unity 实例中确认 PlayMode 成功加载洛阳人物
近景、Console 为 0 错误。受控 Unity 测试和正式截图仍未完成，因此当前不能标记 `ACCEPTED`。

## 2. 开工快照

- 分支：`codex/m23-p4-quality-artisan-growth`；
- 开工 HEAD：`940c4381da4cbb893c0882fd28e68914397af897`；
- Unity：`2022.3.62f3c1`；
- World Schema：V79（本任务未修改）；
- 洛阳县域：320×640、50m、204,800 Cell、512km²；
- Facility：2,084；道路节点/边：359/334；水渠节点/边：19/17；城防边：144；Portal：4；
- 开工工作区已有大量其他任务修改，本轮未还原、未提交、未推送。

## 3. 根因

旧县域底图在一张 320×640 纹理上同时逐 Cell 绘制全部道路，并把全部 Facility 中心绘制成亮黄色
像素，同时叠加城防点。县域全览把 50m 细节压缩到屏幕空间后，就形成了用户看到的密集黄色线网和
点阵。世界数据本身没有新增错误，错误位于 Presentation 信息层级。

现场首次加载失败另有一个独立编译根因：`McfViewScaleCountySandboxFinalReviewMenu` 直接引用了
`Mandate.Simulation` 与 `CountySubViewMode`，但 `Mandate.Editor.asmdef` 没有引用 Simulation。
修复方式是在 Presentation 增加明确的城区/规划子视图包装接口，Editor 菜单只调用 Presentation，
没有扩大 Editor 程序集依赖。

## 4. 已实施

- 固定 `M 天下 / C 县域 / F 人物`，城区与建设继续作为县域子视图；
- 新增只读 `CountyMapPresentationStack`，不写 WorldState 或正式布局包；
- 新增带滞回的 Far/Mid/Near LOD；
- 道路按 R0—R3 分级，使用屏幕空间宽度和当前视窗裁剪；
- Facility 按地标、8×8 Cell 聚合代表、Near实体占地/入口分级；
- Far/Mid 使用城区、农业和六分区低频底色，停止全量黄色 Facility 点云；
- 城防按 Far 轮廓、Mid/Near 墙边绘制，城门独立标识；
- 50m Grid 限制为 `Planning + Near`；
- 县域补齐行政、道路、河流、城防、格网、规划六类 Overlay；
- 天下默认关闭战略格并只显示战略骨架，“交通详图”展开全部正式路线；
- 增加 16 张正式截图的 Editor 自动取证入口，完成后自动停留县域全览并保持 PlayMode。

## 5. 世界权威与兼容

- `WorldState.CurrentSchemaVersion` 保持 79；
- 未修改洛阳布局 JSON、Facility、Road、Fortification、Portal 或人口数据；
- LOD、聚合、视窗裁剪和 Overlay 都是只读派生表现；
- 未建立第二套道路/设施/建设/人物权威；
- 旧黄色问题截图从既有证据复制保存，仅用于 Before/After 对照。

## 6. 验证记录

### 6.1 已通过

- 全工程编译：通过；
- 定向 Core：4/4 通过，包含真实 320×640 布局包索引断言；
- 完整 Core：32/32 组、956/956 项通过，0 失败；
- 完整 Core 清单指纹：`425337CA5EEB975512069ABB81C051A411316BDCFC05DC5B0F9288095F884473`；
- 聚合结果：`tmp/core-test-groups/mcf-lod-final-20260904/aggregate.json`；
- 本任务文件 `git diff --check`：通过。

### 6.2 已知非本任务阻断

- 无筛选完整 Core 单进程在 300 秒达到项目既有上限；后续使用项目分组入口完成全部 956 项；
- `Simulation_SameSeedAndDurationProducesSameSnapshot` 在并发压力下约 320 秒完成并输出 PASS，已按
  确定性长测精确归类到 900 秒窗口；普通测试仍保持 300 秒上限；
- 全局 `git diff --check` 被开工前已有的四个 `Assets/ArtSource/Han/Luoyang/P0Final/*.fbx.meta`
  尾随空格拦截，本任务文件没有该问题；
- Unity 用户实例 PID 45936 占用项目，安全入口不得关闭它。Project Load、EditMode、PlayMode 均
  在启动前准确返回 `blocked/120`，不是测试断言失败：
  - `tmp/unity-validation/unity-ProjectLoadSmoke-20260904-004834-311.summary.json`；
  - `tmp/unity-validation/unity-EditMode-20260904-004836-463.summary.json`；
  - `tmp/unity-validation/unity-PlayMode-20260904-004838-583.summary.json`。

### 6.3 现场加载修复验证

- Unity AssetDatabase 重新编译后 Console：0 Error、0 Warning；
- `PlayableDemo` 已成功进入 PlayMode；
- Game View 已加载洛阳人物近景、建筑群与玩家标记；
- 修复后全工程编译通过；
- 定向 Core 再次通过：`RESULT passed=4 failed=0`；
- 当前 Unity 保持运行，未被自动关闭。

## 7. 证据与人工验收

自动入口：

`Mandate / Validation / Capture MCF County LOD Evidence And Review`

目标目录：`Docs/Evidence/McfViewScaleCountySandboxPresentationLodV1/`。当前尚未生成 16 张正式
Game View 截图；这属于同一 Unity 编辑器占用阻断，不是代码验收通过。
完成时 Unity 应保持 `PlayableDemo / C 县域 / 洛阳 / 县域全览 / PlayMode`，用户可直接滚轮检查
Far→Mid→Near，并依次检查六类 Overlay 与 F 人物无规划格。

## 8. 性能与人工验收状态

- LOD 切换、可见元素统计和视窗裁剪接口已经实现；
- 最终 Far/Mid/Near FPS、GC 与截图必须由正式 Game View 记录，当前环境下准确记为 `BLOCKED`；
- 用户未进行本轮现场验收，状态不是 `ACCEPTED`。

## 9. 剩余门禁

1. 用户保存工作后自行关闭当前 Unity 编辑器，或在当前编辑器中手动执行自动取证菜单；
2. 执行 Project Load、目标 EditMode、目标 PlayMode；
3. 生成并核对 16 张截图与 1280×720、1920×1080布局；
4. 记录最终 Far/Mid/Near FPS、可见元素数、切换耗时和 GC；
5. 保持正式 Game View 打开，等待用户明确验收。

## 10. 下一阶段建议

Unity 门禁与现场视觉通过后，继续“正式建设事务、资源劳力与存档闭环 V1”，把现有规划 Draft
接到审批、土地、资金、材料、劳力、运输、ConstructionProject 与正式 Facility/Road/Wall/Canal。
