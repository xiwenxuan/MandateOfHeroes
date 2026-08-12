# AI 确定性与存档报告

## 确定性合同

- 决策随机坐标固定为 `MasterSeed + AgentStableId + DecisionSequence + AbsoluteDay + ActionId`。
- 不使用 `DateTime`、进程状态或不稳定集合顺序。
- Personality、LifeGoal、PolicyProfile 和稳定 Seed 只影响评分/近似候选选择，不重随机历史初始化事实。
- 相同初始世界、相同 Seed 与相同决策序列产生相同 Decision Trace；不同 Seed 允许未来分化，但必须继续满足守恒和权限规则。

## V72 持久合同

V72 新增并保存 Agent `ModelId`、`PolicyProfileId`、`PrimaryGoalId/Weight` 与最多 32 条 DecisionMemory。V71→V72 迁移只补缺省策略/Profile/Goal/Memory，不删除、合并或重随机已有永久人物和世界实体。模型权重作为版本化只读资产存在；运行状态保存模型 ID/版本，不保存在线学习权重，因为 V1 禁止运行时训练。

已建立的测试覆盖 AgentMemory、PolicyVersion、DecisionSequence、ModelVersion 往返，V71→V72 顺序迁移，以及模型缺失/NaN/Schema 不匹配回退。回退链为 `Neural → Utility → Rule → 合法 NoAction`，模型损坏不会阻断存档加载或世界推进。

## 边界

当前报告证明策略状态的合同级往返与确定性，不等同于 400K 洛阳全明细存档重新写入。大型人口仍遵守既有分区/冷热档案与派生检查点合同。
