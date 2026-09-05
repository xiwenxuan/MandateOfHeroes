# 洛阳三层视角、全城构图恢复与人物近景视觉纠错 V1

## 任务目标

把 `Assets/Scenes/PlayableDemo.unity` 的洛阳入口从技术验证画面升级为同一世界事实上的三层视角：

```text
M 天下战略视角
C 洛阳城市视角
F 玩家人物视角
```

三个视角必须读取同一 `WorldState`、`FacilityId`、永久人物、库存、时间、所有权和控制权。视角状态、相机、选择与表现对象均为可丢弃 Presentation 状态，不进入正式存档，也不得触发模拟或人物瞬移。

## P0-1 范围

- 复用正式天下地图和洛阳地点。
- 用既有 2,084 个 Facility、54 个城市资产变体、六类构图区和历史城市骨架恢复全城构图。
- 城市左键拾取得到正式 `FacilityId`，显示名称、类型、区域、Owner、Controller、开放与 Access 摘要。
- 城市进入近景只改变 `ViewFocusFacilityId`；按 F 返回玩家真实位置。
- 默认人物视图隐藏战略 Cell 地板、棋盘、Footprint Debug 和放大战略模型。
- 用正确人物尺度的连续地面、道路、占地、入口及确定性建筑轮廓占位建立 P0-2 挂点。
- 建立编译、Core、Unity EditMode/PlayMode、截图、分辨率和性能验收证据。

## 硬边界

- 不创建第二套洛阳、Facility、人物、库存、市场或时间账。
- 不修改历史 Facility 坐标、身份、Owner 或 Controller。
- 不升级 Save Schema。
- 不制作正式市场 UI、完整 Facility 功能面板、室内、正式建筑群或全城 NPC 生活表现。
- 不用战略模型暴力缩放冒充人物尺度建筑。
- 不删除合法 Facility 换取性能。
- 不关闭用户已打开的 Unity 编辑器。

## 验收合同

1. M/C/F 使用单一 `LuoyangPlayableViewState`，文本输入时不误触发。
2. 任意视角切换前后完整世界快照一致，`WorldTimeDelta=0`，人物位置不变。
3. 城市投影覆盖 2,084/2,084 Facility、54/54 资产变体、6/6 构图区和十二门、城墙、南北宫、市场、官署、国家仓储、南郊礼制区。
4. 城市选择、近景观察和世界事实使用相同正式 `FacilityId`。
5. 默认人物视图不存在 `LOCAL_TERRAIN_*` 战略 Cell 方块，不加载全城战略建筑模型；近景生成结果由稳定 ID 确定。
6. 正式入口编译、Core、Unity EditMode/PlayMode、`git diff --check` 通过，无 S0/S1。
7. 交付六张真实 Unity Game View：天下、全城、中景、Facility 选中、人物近景、近景城市尺度。
8. 在 1280×720 与 1920×1080 检查 HUD、标签、选择和人物近景；记录 World/City/Person FPS、进入耗时、GameObject 和 GC 基线。

## 后续任务门

只有本任务完成真实 Unity 验收且不存在 S0/S1 后，下一阶段才是 `P0-2：洛阳人物尺度建筑群 V1`。P0-2 可替换 `NearfieldVisualProfile` 的表现模块，但不得修改本任务冻结的同一世界与跨视角 `FacilityId` 合同。
