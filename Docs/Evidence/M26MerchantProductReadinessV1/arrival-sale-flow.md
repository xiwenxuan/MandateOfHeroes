# Arrival and Sale Flow

抵达后界面显示涿县、实际剩余货物、途中货损、旅行天数、人物状态和涿县当前市场价。交付预览显示可售数量、预计货款、采购成本、佣金和预计净收益。实际出售仍由 `TradingSystem.Sell` 消耗正式批次、增加市场库存、改变价格、增加人物钱财并生成库存事务；商行佣金从真实组织金库支付。

自动证据：`MerchantArrivalTests`、`MerchantSaleSettlementTests`。
