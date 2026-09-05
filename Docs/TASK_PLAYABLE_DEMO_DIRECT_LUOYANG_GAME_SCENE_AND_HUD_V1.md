# TASK：PlayableDemo 洛阳直接可玩场景与玩家 HUD V1

## 0. 任务定位

- 中文名称：`PlayableDemo 洛阳直接可玩场景与玩家 HUD V1`
- 英文名称：`PlayableDemo Direct Luoyang Game Scene And Player HUD V1`
- 状态：`IMPLEMENTED / PLAYMODE RE-RUN BLOCKED BY OPEN EDITOR`
- 优先级：`当前产品化阻塞项`
- 实现入口：`Assets/Scenes/PlayableDemo.unity`

> 2026-09-01 人工检查确认本 V1 的镜头、建筑资料、人物可读性和地图内玩法不足。当前整改合同与
> 状态以 `TASK_PLAYABLE_DEMO_LUOYANG_PLAYER_CONTROLS_BUILDING_INTERACTION_AND_GAMEPLAY_REMEDIATION_V1.md`
> 为准；本文件原“右键前往”已被右键拖动旋转替代。

本任务承认一个当前事实：既有 `SimulationDashboard` 能验证大量系统，但它仍是
开发者仪表盘，不是普通玩家进入游戏后应看到的主界面。本任务不再扩充仪表盘，
而是把已经验收的洛阳近景地图、正式人物移动和世界结算接成默认玩家会话。

## 1. 玩家结果

启动 `PlayableDemo` 后，玩家应直接看到洛阳人物尺度近景，而不是全国示意图和
成排测试按钮。玩家可以：

1. 看见自己控制的永久人物；
2. 左键选择建筑、右键或按 `E` 前往建筑；
3. 由正式 `MovePersonCommand`、Cell 路由和世界状态结算位置；
4. 用 `WASD` 移动镜头、滚轮缩放、`F` 跟随人物；
5. 用 `M` 打开或关闭全国格子战略地图；
6. 用 `R` 休息一天，使世界时间通过正式玩家行动推进；
7. 用 `S` 保存到现有内存存档，用 `Esc` 暂停或返回主菜单。

普通游戏 HUD 只显示人物、地点、时间、钱财、口粮、体力、移动状态和当前建筑。
全国地图是战略层，不再冒充人物近景场景。原仪表盘只作为兼容的开发/系统原型入口
保留。

## 2. 权威与复用边界

- `WorldState.PlayerPersonId` 是唯一玩家人物引用；不得建立第二套玩家身份。
- 人物移动必须复用：
  `Global Cell → 四向端口 → CellRoute → LuoyangFormalPlayerMovementService
  → MovePersonCommand → WorldState → Unity 播放`。
- Presentation 只能发出命令和读取投影，不得直接改写人物位置或世界库存。
- 洛阳 2,084 个设施必须来自已经生成的正式人物尺度地图计划，不得临时复制一套
  UI 建筑数据。
- 近景只流式保留玩家周围 3×3 Cell；不一次实例化全国或全城所有高细节对象。
- 本任务不改变存档结构，继续使用当前存档版本。

## 3. 实施范围

### 3.1 正式洛阳可玩世界工厂

新增纯 C# 工厂，输入已有 `LuoyangHumanScaleLocalMapPlan`，创建：

- 洛阳正式地点；
- 一个具有稳定 ID 的永久玩家人物；
- 与地图计划一一对应的 2,084 个正式 `FacilityState`；
- 合法人口缓存和可往返存档的初始世界。

工厂不加载 Unity 资源、不访问 Presentation，也不初始化第二套移动规则。

### 3.2 直接游戏壳

新增 `PlayableLuoyangGameController`，直接使用主摄像机呈现
`HanWorldNaturalMapController` 的洛阳人物尺度场景，并负责：

- 简洁玩家 HUD；
- 镜头平移、缩放和人物跟随；
- 近景/全国战略地图切换；
- 休息、内存保存、暂停和返回主菜单；
- 玩家可理解的移动失败与到达提示。

### 3.3 PlayableDemo 接入

- `_playerDemoMode` 开启时，场景自动进入洛阳直接可玩会话；
- 主菜单第一入口为“进入洛阳”；
- 旧自建人物和商旅系统面板继续保留，但明确标为系统原型；
- 进入旧原型时必须停用直接游戏壳，避免双重 UI 和双重输入。

## 4. 明确不在 V1 范围

- 最终角色 FBX、骨骼动画和服装系统；
- 大规模 NPC 人群、室内场景和建筑内部交互；
- 战斗、经营、仕途等全部玩法在近景中的完整交互化；
- 最终 UI Toolkit、美术、音效和新手引导；
- 70 万人口全部以 GameObject 方式常驻；
- 新存档版本或对既有存档的破坏性迁移。

## 5. 验收标准

### 5.1 自动化

- 工厂对同一种子产生稳定世界事实；
- 世界包含 1 个玩家、1 个洛阳地点和 2,084 个正式设施；
- 设施 ID、定义 ID 和 Cell 与人物尺度地图计划完全一致；
- 世界通过 `Validate()` 并可序列化往返；
- PlayMode 能从 `PlayableDemo` 进入直接游戏壳，绑定正式移动并加载 3×3 Cell；
- `M` 可往返全国战略图和洛阳人物近景；
- `R` 通过正式 `PlayerActionService` 推进一天。

### 5.2 人工

- 默认画面不出现开发仪表盘；
- 玩家能在 60 秒内理解移动、镜头、地图和休息操作；
- 人物移动时近景镜头可跟随，HUD 的设施和移动状态与世界事实一致；
- 返回主菜单后不再接收近景移动输入。

## 6. 验证顺序

```text
全工程编译
→ 新增核心测试
→ 受控 Unity EditMode/PlayMode
→ git diff --check
→ 差异与工作区范围审阅
```

若 Unity 编辑器已由用户打开，必须按项目规则报告锁冲突，不得擅自关闭。
