# Purchase Flow

采购预览读取中山当前 `MarketListingState`，显示商品、计划补足数量、单价、总价、现有现金、买后余额、市场现货、当前载重、新增载重及装车后容量。成交仍由 `TradingSystem.Buy` 扣钱、减库存、改变价格并生成正式布帛批次与库存事务。

资金、库存、地点或容量失败不会提交交易。普通界面不显示批次 ID。

自动证据：`MerchantPurchasePreviewTests`、`MerchantPurchaseFailureTests`、`MerchantCarrierCapacityTests`、既有 `M26P2_ClothPurchaseCreatesFormalBatchInMovingCaravan`。
