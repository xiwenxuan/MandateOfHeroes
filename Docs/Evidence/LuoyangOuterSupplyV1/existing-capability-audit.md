# 洛阳外围供应 V1：既有能力审计

审计基线：`ab65b72`；执行日期：2026-08-29。分类含义：`REUSE` 直接复用，`EXTEND` 在既有
权威上增加接线或持久字段，`GENERALIZE` 去除既有食品专用限制，`NEW` 仅增加不存在的查询或表现。

| # | 能力 | 当前正式入口与结论 | 分类 |
|---:|---|---|---|
| 1 | 农业生产入口 | `AgricultureProductionSystem` 结算 `AgricultureProductionOrderState`，使用真实家庭、人物、设施、种子批次与工单。 | REUSE |
| 2 | 作物生长精度 | 以世界日、播种日、成熟日和 basis points 表达；不是 Unity 帧或表现对象。 | REUSE |
| 3 | 80% 收割 | 洛阳 V70 紧凑运行时已有，统一正式农业原先没有；本任务把版本化阈值和产量曲线加入统一农业入口。 | EXTEND |
| 4 | 收获批次 | 正式农业通过 `ProductInventorySystem` 创建带工单、产地、产品和品质来源的 `ProductBatch`。 | REUSE |
| 5 | 收获目的库存 | 进入工单指定的家庭/设施正式存储，不直接增加洛阳市场或城市总量。 | REUSE |
| 6 | 家庭/组织存储 | `InventoryContainerState`、`VillageFacilityState`、`ProductBatch` 与 `InventoryTransactionState` 是唯一权威。 | REUSE |
| 7 | 民运生成 | `CivilianFreightSystem` 从跨县正式买卖单、货运 Demand/Offer 和登记承运人派车。 | REUSE |
| 8 | 运输需求来源 | 正式市场订单、公共粮采购/救济恢复及既有补货流程；没有新增全知扫描器。 | REUSE |
| 9 | 承运登记 | `CivilianCarrierRegistrationState` 保存费用、里程、已知路线、人物和移动容器。 | REUSE |
| 10 | 真实人物 | 登记和派运均引用 `PersonState`，检查存活、位置、家庭与进行中旅程。 | REUSE |
| 11 | 移动容器 | `InventoryContainerState` 的 Carrier、Owner、Location 和重量容量承载真实批次。 | REUSE |
| 12 | 既有路线 | `RouteState`、多段 `JourneyState` 和既有最短/安全有限认知规划仍负责商业路线层。 | REUSE |
| 13 | CellTraversal 接入 | 将一次连续市场路线腿绑定为持久 `CellRoute` 快照；不新增 TransportTask。 | EXTEND |
| 14 | Gate 接入 | CellRoute 路段读取正式 Passage Facility 状态；关闭时等待或按能力重算。 | EXTEND |
| 15 | 到货卸载 | 继续调用既有 `ResolveArrivals` 与正式批次转移；仓满时 Carrier 保留余货并进入 `AwaitingReceipt`。 | REUSE |
| 16 | 家庭消费 | 正式家户月结从家庭持有的食品批次扣除并生成实际消费事务。 | REUSE |
| 17 | 食品短缺 | 家庭结算在物理库存不足时形成短缺账与事件；城市投影只汇总它。 | REUSE |
| 18 | 官仓/市场库存 | 官仓、家庭、组织与在途批次仍是库存；订单只预留/交易，不产生 MarketStock。 | REUSE |
| 19 | 木材链 | `ResourceBodyState`、`UpstreamResourceProductionSystem`、木料产品、工单、批次和正式库存已存在。 | REUSE |
| 20 | 只缺的空间能力 | 货运 CellRoute、动态门桥/道路复验、只读供应投影、只读供应区查询和轻量 Unity 标记。 | EXTEND / NEW |

架构结论：没有新增 Cargo、TransportTask、SupplyInventory、CityFoodDemand、MarketStock 或第二个
Freight Planner。`LuoyangSupplyCatchment` 和 `LuoyangCitySupplyProjection` 均为只读选择/汇总。

内容兼容审计另发现受保护包仍使用 `product.food.wheat_grain`，正式内容为
`product.wheat_grain`。本任务以
`content-migration.luoyang-outer-supply.wheat-grain.v1` 显式桥接；木料 ID 一致。豆、黍、粟三个
旧食品 ID 尚无正式内容定义，保留原引用并报告为未解析，不静默改指。
