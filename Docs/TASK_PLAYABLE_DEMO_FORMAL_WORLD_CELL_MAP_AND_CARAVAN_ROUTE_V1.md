# TASK：PlayableDemo 正式全国格子地图替换与商旅路线可视化 V1

## 0. 任务定位

本任务将 `PlayableDemo` 普通玩家“地图”页从旧版涿县—中山—广宗绢帛示意图切换到既有
`HanWorldV1` 全国自然地图、全国战略格 LOD 与区域精确 2km Cell，并把中山—涿县商旅正在使用的
正式 `R003 / CellRoute / CivilianFreight` 直接投影为路线和商队位置。

任务只替换 Presentation，不建立第二套地图、路线、人物位置或经济权威，不升级 Save Schema。

## 1. 当前问题

- `SimulationDashboard.DrawPlayerMap()` 仍使用 `ProceduralSilkMapArt`、`Location.MapXBasisPoints` 和
  `RouteState` 两端直线绘制旧原型地图。
- 商旅底层已经由全国 `R003` 生成连续四向 `CellRoute`，但普通玩家看不到这些 Cell。
- `HanWorldNaturalMapController`、`ExplicitStrategicCellMapV1` 和洛阳人物尺度 Streaming 已经存在，
  但尚未接入 `PlayableDemo` 普通玩家地图页。
- 页首标题仍硬编码为“184年涿县至广宗”，不能反映玩家、身份、当前位置和当前行程。

## 2. 权威与架构

```text
WorldState / Person / Journey / CivilianFreight / CellRoute
                         |
                         v
           PlayableWorldMapProjection（只读、无 Unity）
                         |
                         v
       PlayableFormalWorldMapController（Unity Presentation）
                         |
             +-----------+-----------+
             |                       |
   HanWorldNaturalMapController   路线/人物标记
             |
   ExplicitStrategicCellMapV1
```

- Domain 与 Simulation 继续持有正式事实和结算。
- Persistence 继续读取 `HanWorldV1` 与 `road_edges.json`。
- Presentation 只生成可丢弃、可重建的摄像机、RenderTexture、路线和标记。
- 地图刷新、缩放或切换视角不得改变世界 Revision。

## 3. V1 范围

### 3.1 必须完成

1. 普通玩家“地图”页显示全国自然地形，而不是旧绢帛图。
2. 全国层显示 32×32 Cell 引导 LOD；区域层显示相机附近精确 2km Cell。
3. 商旅启程前读取 `R003` 计划路线；启程后读取同一 `CivilianFreight.CellRouteSegments`。
4. 显示起点、终点、玩家/商队当前 Cell、已走段和剩余段。
5. “全国概览”“跟随商队”“显示/隐藏格子”均只控制表现。
6. 保存/读取或推进世界后，地图从正式世界重新构建，不保存 UI 派生状态。
7. 普通玩家页首使用动态日期、身份、位置与行程文案。
8. 旧地图方法暂时保留为兼容代码，但普通玩家路径不得再调用。

### 3.2 明确不做

- 不把中山商旅剧情迁往洛阳。
- 不伪造中山或涿县人物尺度街区、建筑和考古坐标。
- 不在 V1 内实现全国人物尺度 Streaming、自由点击寻路或完整认知地图。
- 不删除旧地图代码；待正式玩家地图验收稳定后再单独清理。
- 不修改 `CellRoute`、`CivilianFreight`、库存、市场、人口或存档结构。

### 3.3 后续阶段

玩家进入洛阳时，由后续任务把同一地图控制器接到现有
`LuoyangHumanScaleStreamingPresentation` 的 3×3 Cell 近景；这不属于本 V1。

## 4. 验收门禁

1. `PlayableWorldMapProjection` 的计划路线与 `R003` 连续 Cell 完全一致。
2. 启程后投影使用正式 Freight 当前 Cell 和剩余距离，不重新规划假路线。
3. 地图投影构建前后 World Snapshot 完全一致。
4. 全国层使用 LOD32，区域层使用精确 Cell，不允许全国一次生成全部精确格网。
5. `PlayableDemo` 普通玩家地图控制器初始化成功，来源为 `HanWorldV1`。
6. 普通玩家地图不调用旧 `DrawRegionMap`。
7. 全工程编译、目标核心、受控 Unity EditMode/PlayMode、`git diff --check` 通过。

## 5. 交付物

- 任务书与项目总纲状态。
- 无 Unity 依赖的正式玩家地图路线投影。
- Unity 正式地图 RenderTexture 控制器与路线/标记表现。
- `PlayableDemo` 普通玩家地图入口和动态标题。
- Core、EditMode、PlayMode 测试和验收证据。
