# 洛阳 189—190 重大事件 Runtime 报告

- 184 Snapshot 只负责开局，190 年不会加载另一份 Snapshot。
- 事件通过运行时世界日、社会压力、政府政策与城市防御前置条件求值。
- 189 宫廷危机支持 `canonical / variant / transformed`；本次六年种子得到 `variant`。
- 190 迁都与破坏支持 `canonical / delayed / prevented`；本次六年种子得到 `canonical`。
- 非 prevented 结果会降低真实政府设施 Condition，令其进入 Maintenance，并将首批 1,000 户 Residence 设为流离状态；官员、军役人物、Force、政府与军队库存通过30日旅行抵达长安。
- 两个事件均以 `AppliedOffscreen=true` 结算；不依赖玩家是否在洛阳。
- 后续 AI 每次决策重新读取已改变的世界事实，不强制沿历史剧本继续。
