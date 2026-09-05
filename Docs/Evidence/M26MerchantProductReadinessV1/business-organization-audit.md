# Business Organization Audit

中山商行信息直接读取正式 `OrganizationState`、`MembershipState`、`PositionState`、`MerchantBranchState` 和仓库容器。普通玩家可以看到商号资金、声望、负责人、成员姓名/职位、自己的身份、仓库状态和当前长期经营方向。

仓库与随行商队分开显示。安全转存入口和正式商业委任尚不存在，分别记录为 `MISSING / NEXT PHASE`，本轮未建立假组织或假委任状态。

自动证据：`MerchantBusinessOrganizationViewTests`、`MerchantStorageViewTests`。
