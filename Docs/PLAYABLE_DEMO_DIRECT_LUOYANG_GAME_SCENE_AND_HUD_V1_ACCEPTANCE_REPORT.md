# PlayableDemo 洛阳直接可玩场景与玩家 HUD V1 验收报告

## 1. 结论

当前结论：

`IMPLEMENTED / AUTOMATED PLAYMODE RE-RUN BLOCKED BY OPEN EDITOR`

2026-09-01 后续人工检查未接受原候选：中键平移、右键旋转、建筑信息、人物移动可读性和地图内
玩法均不足。整改实现与后续验收改由
`PLAYABLE_DEMO_LUOYANG_PLAYER_CONTROLS_BUILDING_INTERACTION_AND_GAMEPLAY_REMEDIATION_V1_ACCEPTANCE_REPORT.md`
负责；本报告不得再被引用为玩家体验已通过。

实现、全工程编译、新增核心测试和差异检查已经通过。受控 Unity PlayMode 复验由安全入口检测到
用户 Unity 编辑器 PID `21736` 正在运行而停止；没有关闭用户程序。因此本任务已经形成可运行候选，
但在取得明确 PlayMode 结果和人工操作确认前不标记最终 `ACCEPTED`。

## 2. 本轮完成内容

### 2.1 直接玩家会话

- `PlayableDemo` 的 `_playerDemoMode` 现在会自动进入洛阳人物近景；
- 主菜单第一入口改为“进入洛阳（人物近景可玩场景）”；
- 原自建人物、商旅和系统面板保留为旧系统原型，不再冒充默认游戏画面；
- 进入旧系统原型时会停用近景游戏壳，避免双 UI 和双输入。

### 2.2 正式世界与移动

- 新增纯 C# `PlayableLuoyangWorldFactory`；
- 建立 1 个洛阳地点、1 个稳定永久玩家人物和 2,084 个正式 Facility；
- 设施 ID、Definition、Cell 和 Settlement 直接取自既有
  `LuoyangHumanScaleLocalMapPlan`；
- 绑定既有门桥世界状态、正式玩家移动、世界命令运行时和 3×3 Cell Streaming；
- Unity 人物只播放正式世界已接受的路线，不直接改写人物位置。

### 2.3 玩家操作

- 左键选择建筑；
- 右键或 `E` 前往建筑；
- `WASD` 平移镜头，滚轮缩放，`F` 跟随人物；
- `M` 往返洛阳人物近景和全国正式格子战略地图；
- `R` 通过 `PlayerActionService` 休息一天；
- `S` 保存到既有内存存档；
- `Esc` 暂停、保存或返回主菜单。

HUD 只显示玩家需要的人物、时间、地点、钱财、口粮、体力、移动状态和建筑目标。
`HanWorldNaturalMapController` 的审图工具条在普通玩家会话中隐藏，但保留独立的 HUD 输入防穿透。

## 3. 权威与兼容

- `WorldState.PlayerPersonId` 保持唯一玩家引用；
- 世界移动继续使用既有 Global Cell、四向端口、CellRoute、持久命令和世界结算；
- 没有建立第二套人物、设施、路线、库存或经济权威；
- 没有升级存档版本；
- 全国地图和 HUD 均为可重建 Presentation 投影；
- 旧开发和商旅入口继续兼容。

## 4. 自动验证

2026-09-01 13:19（Europe/Berlin）最终复验：

- 全工程编译：`PASS`
- 新增核心测试：`2/2 PASS`
  - `PlayableDirectLuoyangWorld_CoversFormalMapAndRoundTrips`
  - `PlayableDirectLuoyangWorld_IsDeterministicAndCanRest`
- `git diff --check`：`PASS`
- Unity 项目内脚本编译：用户已打开编辑器的 `Editor.log` 显示本轮程序集重载成功，
  无本轮 C# 编译错误；这不替代测试。
- 目标 PlayMode：`BLOCKED`，安全入口明确报告
  `Unity test blocked: an editor is already running (PID: 21736)`。

证据摘要：
[`Evidence/PlayableDirectLuoyangGameV1/verification-summary.md`](Evidence/PlayableDirectLuoyangGameV1/verification-summary.md)。

## 5. 未完成与下一门禁

1. 用户关闭 Unity 编辑器后，以安全入口执行目标 PlayMode 1/1；
2. 在 Game View 人工确认默认不是仪表盘，人物可选建筑、移动、跟随和切换地图；
3. 记录至少一张洛阳人物近景截图；
4. 根据人工结果修复 S0/S1 后再决定是否标记 `ACCEPTED`。

最终角色 FBX、骨骼动画、NPC 人群、室内和完整职业/战斗交互不属于本 V1。
