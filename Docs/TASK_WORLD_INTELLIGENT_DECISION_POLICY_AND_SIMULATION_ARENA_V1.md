# WORLD-INTELLIGENT-DECISION-POLICY-AND-SIMULATION-ARENA-V1 任务书

- 状态：实现与任务相关核心验收完成；Unity 启动环境阻断已如实记录
- 世界存档版本：V72
- 权威输入：V71 智能人口驱动世界与条件历史事件合同、M12 永久人口规则
- 交付目录：`HISTORICAL_WORLD_REFERENCE/WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1/`

## 目标

在同一 `WorldState` 上为 Household、FamilyOrganization、Merchant、Settlement、Government 建立可运行、可解释、可回退的决策策略；用 Rule、Utility、Randomized Utility 与离线训练 Neural Scorer 进行多种子 Arena 对照；保留结构化历史事件前提和世界守恒边界。

## 实施范围

1. 建立候选动作生成、分项效用评分、稳定随机、人格/目标权重和有界决策记忆。
2. 所有策略只选择 `ActionIntent`，统一通过 Validator 与正式 Executor；不得直接改库存、人口、设施、产权或历史事件结果。
3. 建立五类 Agent 的 Profile/Goal 映射；Family 与 Merchant 当前共用 Organization 运行实体，但使用不同业务候选和权限合同。
4. 建立 12 维特征、1×8 ReLU MLP 离线训练、版本化模型清单和 Rule/Utility 安全回退。
5. 建立 10 个 Benchmark、100 Seed、1/5/10 年检查点、8 个成对反事实和 189/190 条件历史事件分布证据。
6. 因新增 Agent 模型、策略 Profile、Goal 与 DecisionMemory 持久字段，将 Schema 从 V71 顺序迁移至 V72，并验证往返兼容。

## 验收标准

- 全工程编译、核心回归、Unity EditMode/PlayMode 受控测试分别报告。
- 任务书点名的 Utility、五类 Agent、Neural Safety、Seed、Event、Arena、Save 和守恒测试存在并通过。
- 4,000 次 Arena 运行、176,000 次决策及独立 trace/metrics 证据可审计。
- 模型缺失、NaN 或 Schema 不匹配时不阻断世界运行。
- 不把 Neural 设置为全国默认，不在线训练，不实现全国 HOT/WARM/COLD。
- 知识库、总纲、任务路由及登记表同步更新；不自动提交或推送。

## 结论边界

本任务形成“可运行决策底座与实验场”，不是成熟全国 AI。当前最强证据是动作合法性、确定性、回退和历史事件分布；Facility、产业、贸易网络、政府财政与 400K 混合热度性能尚未在 Arena 中形成完整世界差异，必须作为下一阶段门禁，而不能据此宣称已完成全国智能世界。

## 最终验证记录（2026-08-12）

- 全工程编译：通过。
- 任务相关核心测试：51/51 通过，含全部 Policy/Arena/Save/Seed/Event/守恒测试及一项 V70 旧断言兼容修正。
- 全量核心回归：在 300 秒硬超时前执行到中段，曾发现 `LuoyangLiving_V69MigratesToEmptyV70Contract` 仍写死 70；已改为当前 Schema 并在任务相关集合中复验通过。因超时未得到全库最终汇总，不声称全量通过。
- Unity EditMode：安全运行器在 120 秒内未获得任何启动日志，PID 18008 已终止且无残留，状态码 125；因此 EditMode/PlayMode/Smoke 不声称通过。
- `git diff --check`：以最终命令结果和 `validation_summary.json` 为准。
