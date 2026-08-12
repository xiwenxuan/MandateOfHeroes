# V71 保存、确定性与模型版本报告

V71 在 V70 洛阳生活经济检查点之上只新增两个持久集合：`WorldDecisionAgents` 与 `WorldSimulationLodStates`。前者保存 Agent Kind、Policy ID/Version、Model Version、DecisionSequence、最近决策日与 Action；后者保存 Target、HOT/WARM/COLD、上次与下次调度日。

World Seed 继续使用既有 `WorldState.MasterSeed`，不建立第二个种子。稳定决策随机键为：World Seed + AlgorithmVersion + `mandate.living_world.decision` + Agent Stable ID + AbsoluteDay + Action ID + DecisionSequence。固定起点、Seed、策略/模型版本和调用序列可以复现；不同 Seed 允许产生不同未来。

V70→V71 迁移只建立空集合，不伪造 AI、事件、订单、运输或 LOD 事实。结构化事件 Anchor 保存 Outcome、Rule、ChangePackageVersion、已应用 Operation IDs、失败条件、评估次数和离屏标志，防止读取存档后重复冲击。

V1 禁止在线训练。模型更新必须显式改变 ModelVersion，并通过兼容与复现实验；模型不得成为权威世界事实。
