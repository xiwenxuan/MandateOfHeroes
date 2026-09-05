# TASK：PlayableDemo 洛阳玩家镜头、建筑交互与首条玩法闭环整改 V1

## 0. 任务定位

- 英文名称：`PlayableDemo Luoyang Player Controls, Building Interaction And Gameplay Remediation V1`
- 状态：`IMPLEMENTED / LIVE EDITOR VERIFIED / AUTOMATED PLAYMODE RE-RUN BLOCKED BY OPEN EDITOR`
- 触发原因：2026-09-01 人工检查确认原直接场景仍是“可移动技术切片”，不满足普通玩家操作标准。
- 基线：`TASK_PLAYABLE_DEMO_DIRECT_LUOYANG_GAME_SCENE_AND_HUD_V1.md`

本任务直接整改五项 S0/S1 体验缺口：中键平移、右键旋转、建筑资料、可见人物移动，以及在地图内
真实可执行的首条生活玩法。原“右键点击地面直接移动”输入被取消，避免和右键拖动旋转冲突；人物
前往统一由建筑卡、“前往目标”或 `E` 发起。

## 1. 玩家结果

进入 `PlayableDemo` 洛阳近景后，玩家可以：

1. 按住鼠标中键拖动，平移人物尺度镜头；
2. 按住鼠标右键拖动，围绕当前关注点旋转并限制俯仰角；
3. 左键选择建筑，查看正式名称、类型、运作状态、完好度、史料置信度、空间精度、Cell 与通行要求；
4. 点击“前往此处”后，在近景镜头中明确看见玩家人物沿已结算路线行走；
5. 按目标卡完成“市集交易—官署接差—推进差事”的第一条洛阳生活闭环。

## 2. 权威边界

- 中键、右键、镜头关注点和建筑卡均为可重建 Presentation 状态，不进入存档。
- 人物前往继续使用 `CellTraversalPlanner → LuoyangFormalPlayerMovementService → MovePersonCommand → WorldState`；
  Unity 只播放已提交结果。
- 建筑资料来自 2,084 项正式开局 Facility 数据和人物尺度 Capability，不以 UI 临时造建筑。
- 市集和官署行动通过既有 `PlayerActionService`、`TradingSystem`、`TaskSystem`、Person、Family、
  Organization、MarketListing、InventoryStack、TradeRecord 与 TaskInstance 结算；不增加玩家专属钱包、
  背包、任务进度或第二套经济。
- 本任务不升级存档结构，不改 2,084 Facility 的 ID、Cell、定义或最终模型绑定。

## 3. 首条玩法闭环

```text
当前目标提示洛阳市集
→ 选择或一键定位正式市场 Facility
→ 点击前往并看见人物行走
→ 买入或卖出布帛，钱财、市场库存、随身货物与交易记录回写
→ 当前目标切换为市场市曹
→ 在同一正式市场的市曹办公点接受“协助市曹核验商籍”
→ 每次投入一天推进任务与共享世界时间
→ 完成后获得钱财和口粮，并转入自由探索
```

这是首条可验证生活循环，不等于完整商号、正式食品批次交易、全部职业、室内、战斗或家族代际玩法。
M26 的 20—30 分钟独立人类盲玩门禁继续有效。

## 4. 实施项

### 4.1 镜头

- 本地近景默认正交尺寸为 8，兼顾人物和一座 120 米级正式 Facility；
- 人物表现根在直接玩家场景使用 2.4 倍 Presentation-only 可读缩放，并显示“◆我/◆我·行走中”世界标记；
- 正式移动仍按原世界结果结算，近景回放使用 24 单位/秒的纯表现压缩，确保玩家能看见起步、途中和到达；
- 中键拖动按当前正交尺寸换算每像素世界位移，并停止人物跟随；
- 右键拖动修改 yaw/pitch，pitch 限制在 28—78 度；
- UI 区域不接收镜头拖动，`F` 可恢复人物跟随。

### 4.2 人物尺度场景合成

- 洛阳人物尺度视图隐藏旧全国/区域战略地形，返回全国地图时恢复，避免战略层遮住本地建筑；
- 正式 Facility 仍使用同一模块化 Prefab，但在人物尺度下只扩大 X/Z 占地，Y 轴保持可读高度；
- 正式人物、建筑和本地路线统一使用人物尺度 Floating Origin；
- 本地 3×3 Cell Streaming 跟随正在回放的人物位置，而不是提前跳到正式世界终点；
- 红色 Facility Footprint 调试面只保留选择/碰撞代理，不在普通玩家画面渲染。

### 4.3 建筑卡与情境行动

- 建筑开局数据加载时保留 `display_name/category/historical_confidence/spatial_precision`；
- 建筑卡只在人物到达后开放该建筑可用行动；
- 市集过滤交易和街坊事件，官署过滤任务/建设/地方事件，客舍/住宅、医馆、学舍、农田和工坊
  分别显示其已有 PlayerAction；
- 不可用行动显示既有服务返回的原因，不由 UI 猜测成功。

## 5. 验收

### 5.1 自动化

- 直接世界保留 2,084 Facility，并具有真实展示名、玩家家庭、商人职位、洛阳市场和本地任务；
- 相同种子保持确定性并可存档往返；
- 市集买入生成正式 TradeRecord，官署接受生成正式 TaskInstance；
- PlayMode 验证人物可读尺度、镜头平移、镜头旋转、建筑选择、前往后的行走完成和市集行动；
- 全工程编译、目标核心、Unity PlayMode、`git diff --check` 分别报告。

### 5.2 人工

- 不阅读开发说明，也能在两分钟内理解中键、右键、建筑选择和“前往此处”；
- 人物从起点到市场的位移在 1080p Game View 中可见；
- 建筑卡与所选 Facility 一致，切换建筑不会沿用旧信息；
- 完成交易后目标自动切到官署，接受并推进差事后可说明钱财、货物、时间和任务变化。

## 6. 当前证据与阻塞

- 全工程编译：通过；
- 目标核心：4/4 通过；
- `git diff --check`：通过；
- Unity PlayMode：待用户关闭当前 Unity 编辑器后由 `Tools/Run-UnityTestsSafe.ps1` 复验，禁止关闭用户进程；
- 已打开 Unity Game View 人工验收：市场建筑资料显示、前往状态/路线/玩家标记、到达状态、市场行动按钮和
  正式买入均通过，Console 为 0 Error；中键/右键对应代码与 PlayMode 用例已就绪，自动化复验仍受同一编辑器锁阻塞。
