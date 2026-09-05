# PlayableDemo 洛阳玩家控制、建筑交互与玩法整改 V1 验证摘要

验证日期：2026-09-01

## 自动化

- 全工程编译：`PASS`
  - 日志：`tmp/skill-verification/compile-20260901-145713-270.out.log`
- 目标核心测试 `PlayableDirectLuoyangWorld`：`4/4 PASS`
  - 日志：`tmp/skill-verification/core-tests-20260901-145721-492.out.log`
  - `CoversFormalMapAndRoundTrips`
  - `GameplayFacilitiesShareWalkableNetwork`
  - `IsDeterministicAndCanRest`
  - `ProvidesTradeAndLocalTaskLoop`
- `git diff --check`：`PASS`
- 目标 PlayMode：`BLOCKED / CODE 120`
  - 安全入口检测到用户正在使用 Unity 编辑器 PID `21736`，按规则未关闭用户进程、未启动第二实例。
  - 阻塞日志：`tmp/skill-verification/playable-luoyang-gameplay-remediation-playmode/unity-PlayMode-20260901-135750-145.log`
  - 因无测试结果文件，不声称 PlayMode 通过。

## 已打开 Unity Game View 人工操作

1. 进入“洛阳·人物近景”，本地人物、建筑与 UI 正常加载，Console 为 0 Error、0 Warning。
2. 选中正式市场后，信息卡显示名称、类型、运作状态、完好度、史料/空间依据、Cell、Access 和人物位置。
3. 按 `E` 前往，状态显示“行走中”，世界标记显示“◆我·行走中”，黄色路线与人物位置在画面中更新。
4. 约 10 秒后到达市场，状态恢复“可行动”，信息卡显示“人物位置：已到达”。
5. 到达后开放“接受本地任务 / 买入匹布 / 卖出随身布料”。
6. 点击“买入匹布”后，钱财 `200 → 40`、世界时间 `第1日 → 第2日`，底部日志显示
   “买入2单位布帛，支出160钱”，当前目标推进为市场市曹差事。

## 边界

- 中键平移和右键旋转已有实现及 PlayMode 断言，但由于同一编辑器锁，尚缺安全入口生成的自动结果文件。
- 24 单位/秒只用于 Unity 路线回放；正式人物位置仍由既有 CellRoute、MovePersonCommand 与 WorldState 决定。
- 本摘要不是 M26 的 20—30 分钟独立人类盲玩封存，不能替代其最终验收门禁。
