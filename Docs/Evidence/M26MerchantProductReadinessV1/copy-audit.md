# Player-facing Copy Audit

本轮清理了商旅普通页面中的“正式库存”“正式批次”“写回同一世界账”“任务系统”等开发表达，并将仓库、车马场、行会馆和官署说明改为玩家语言。投影视图不暴露类型名、稳定 ID、TODO 或 Debug 文本。

开发者观察台保留技术内容，但 `PlayableDemo` 不显示其入口。

自动证据：`MerchantPlayerCopyAuditTests`、`DashboardSceneTests`。
