# LUOYANG-PLAYABLE-VERTICAL-SLICE-MAP-PRESENTATION-AND-CONSTRUCTION-ASSET-LIBRARY-V1

状态：`LUOYANG_GOLDEN_SLICE_V1_PLAYABLE_WITH_PROCEDURAL_ART_LIMITS`

最终验收：2026-08-12 已完成程序化美术 V1 口径；全量核心回归 698/698、Unity EditMode 14/14、Golden Slice PlayMode 2/2、旧洛阳场景 1/1、视觉证据 1/1。最终 DEM/3D/全城 Streaming 仍按本任务明确边界进入下一阶段，不得将本状态改写为最终全洛阳美术完成。

本任务源自用户提供的端到端任务书。权威交付包：

`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_PLAYABLE_VERTICAL_SLICE_MAP_PRESENTATION_AND_CONSTRUCTION_ASSET_LIBRARY_V1/`

## 完成口径

- 同一洛阳 Runtime 上建立视觉投影，不建立第二套城市事实，不引入 SubCell。
- 正常视图隐藏 Cell；建设模式显示 Cell、合法用地与 Ghost。
- `FacilityDefinition → BuildBlueprint → FacilityVisualProfile → 程序化模块资产` 四层分离。
- 住宅、仓库、工坊和市场由玩家、AI、Family/Government与历史初始化复用同一 Blueprint；历史南宫仅允许历史初始化/事件。
- Facility、Person、Shipment、Crop、Road、River均携带既有 Runtime 绑定。
- 视觉可由 v6 Runtime 存档重建，视觉局部坐标不进入存档权威。

## 明确边界

- 本轮建立的是可玩的程序化/绘制 Golden Slice V1，不将原创背景图冒充地理权威。
- 全国 DEM Chunk Terrain Mesh、最终3D汉代Prefab、完整Force近景、全洛阳街区仍属于下一阶段。
- 不引入外部商业游戏资产；AI辅助原创背景及项目程序化代码均登记来源。
- 未授权提交或推送。
