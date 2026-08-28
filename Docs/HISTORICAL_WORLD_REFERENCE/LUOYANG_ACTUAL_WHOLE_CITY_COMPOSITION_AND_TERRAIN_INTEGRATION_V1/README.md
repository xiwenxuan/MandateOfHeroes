# 洛阳实际全城构图与地形融合 V1 证据索引

状态：`TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

## 合同

- 全城：2,084个Facility Visual Local Anchor；
- 资产：54个稳定Asset Variant；
- 构图区：宫城政务、里坊住宅、市肆工坊、城防、交通水利、农业资源；
- 最密审查窗口：24×24 Global Cell、549个Terrain Grounded Facility；
- 权威边界：不创建SubCell，不修改Facility、Global Cell、建设、人口或Save Schema。

## 代码

- `Assets/Scripts/Mandate.Domain/LuoyangWholeCityCompositionState.cs`；
- `Assets/Scripts/Mandate.Presentation/LuoyangBuildingPerformanceBatchRenderer.cs`；
- `Assets/Scripts/Mandate.Presentation/HanWorldNaturalMapController.BuildableFacilities.cs`；
- `Assets/Tests/EditMode/LuoyangWholeCityCompositionV1Tests.cs`；
- `Assets/Tests/PlayMode/LuoyangWholeCityCompositionV1PlayModeTests.cs`。

## 图形证据

- `Screenshots/01_DENSE_549_COMPOSED_FINAL_ASSET_TERRAIN_WINDOW.png`：1600×1000 Unity实际Game View，
  1,371,107字节。
- `luoyang_whole_city_composition_metrics_v1.json`：构图区、锚点、走廊、接地与合批指标。

## 验证

- 全工程编译：通过；
- 定向核心：1/1通过；
- 目标EditMode：3/3通过；
- 目标图形PlayMode：1/1通过；
- 受影响的既有549 Facility批处理图形回归：1/1通过；
- 目标验证不能扩大为全量核心、EditMode或PlayMode回归结论。
