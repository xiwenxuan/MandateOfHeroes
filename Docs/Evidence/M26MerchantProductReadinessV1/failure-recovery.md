# Failure Recovery

覆盖并验证：资金不足、市场现货不足、容量超限、缺少道路、口粮不足、尚未抵达、没有货物和商行佣金不足。预检给出玩家语言原因与当前可用的恢复建议；拒绝路径不扣钱、不丢货、不推进时间、不重复生成事务，行动页仍可继续使用。

目的仓满、Gate Closed 等洛阳正式物流失败继续由现有正式供给/货运测试覆盖；中山布帛市场没有目的仓容量合同，因此不伪造该限制。

自动证据：`MerchantPurchaseFailureTests`、`MerchantFailureRecoveryTests`、`MerchantRouteFailureTests`、既有洛阳玩家货运失败测试。
