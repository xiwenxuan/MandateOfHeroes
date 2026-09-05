# Route Flow

## 中山盲玩主路径

中山—涿县继续读取既有正式 `RouteState/JourneyState` 的连接关系和治安，并由全国
`HanWorldV1/locations/road_edges.json` 的 `R003` 生成现有 `CellRoute`。全国底图允许八向 authored
邻接；运行合同把对角步确定性拆成两个四向相邻 Cell，并保持 2 km 方格的对角几何距离。每段引用
`route.zhuo_zhongshan`，缺路时在启程前显示道路不可用，且不移动人物、不推进时间。

## 洛阳供给路径

洛阳“登记商车 / 小批运粮”继续通过 `LuoyangFormalCellSupplyRouteAccess` 读取正式 `CellTraversalPlanner`，Gate / Road / Bridge 的状态来自世界事实。

## 已知边界

中山端点使用全国城市 `C012` 的 Cell `3352589`；涿县端点使用已登记的临时玩法节点
`geo.site.zhuo_office` Cell `3160413`。后者仍为低置信、临时玩法代理，不能宣称是精确历史县城坐标。
Acceptance Gate F 的正式 CellRoute 自动化条目为 `PASS`，历史定位精度仍保留上述明确边界。

自动证据：`MerchantFormalCellRouteTests_DepartureUsesR003FreightAuthority`、
`MerchantCellRouteSaveContinuationTests_MidRouteResumesSameCellLedger`、`MerchantTravelWorldTimeTests`、
`MerchantRouteFailureTests`、`M26P2_FormalCaravanCargoSurvivesSnapshotAndAuditsLossAndSale`与三次重放。
