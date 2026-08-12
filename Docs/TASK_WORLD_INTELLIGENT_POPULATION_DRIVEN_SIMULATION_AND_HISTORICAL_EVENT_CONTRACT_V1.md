# WORLD-INTELLIGENT-POPULATION-DRIVEN-SIMULATION-AND-HISTORICAL-EVENT-CONTRACT-V1

## 任务定位

本任务建立统一 Living World 运行宪法：历史资料和 Scenario Snapshot 只初始化开局；游戏开始后，由真实 Person、Household、Organization、Cell、Facility、Inventory、Market、Office、Force、Route、Order、Shipment 与事件运行状态驱动连续世界；重大历史事件继续以结构化前提检查，并通过真实 ChangePackage 对同一世界施加冲击。

最高层运行模型：

> 历史快照初始化 + 人口/需求/资源驱动的智能自演化 + 条件式重大历史事件冲击。

> 历史有惯性，但未来不是注定的。

## 强制合同

- `REFERENCE != RUNTIME DRIVER`；未来历史快照不得覆盖连续游戏。
- Signal 是从真实事实重算的决策输入，不是第二套人口、库存或产能。
- AI 只提出 Action/Intent；统一确定性规则拥有最终验证和执行权。
- World Seed、Agent ID、决策序号、世界时间和 Action ID 共同定位稳定随机。
- Supply Relation 只作参考；实际供给必须来自真实需求、库存、主体决策、订单和运输。
- Hot/Warm/Cold 只改变调度频率，不改变事实、不重建人物或设施。
- 重大事件不得只按年份触发，必须支持 Canonical、Variant、Delayed、Transformed、Prevented，并可离屏运行。
- 保留洛阳 400,000 Person、80,899 Household、2,084 Facility 基线，不重新生成；1182县资料继续只作参考。

## 架构交付

- Domain：V71 决策主体状态、策略/模型版本、决策序号、LOD 状态、Signal/Action/Validation 合同及扩展历史事件状态。
- Simulation：Signal 重算、Rule/Utility/Neural Adapter/稳定随机包装策略、行动验证、条件求值、ChangePackage、洛阳189/190原型、Simulation Arena。
- Persistence：V70→V71 顺序迁移与往返保存；事件已应用操作、决策序号、策略/模型版本和 LOD 状态可恢复。
- Supply：复用 V30—V34 正式市场、V31/V32 CivilianFreight、V19—V28 MilitaryLogistics 和 V33 命令内核，不建立第二套库存或货物账。
- Documentation：运行/参考审计、九份核心合同、AI训练数据合同、事件/竞技场/存档/性能报告和下一阶段门禁。

## 验收

1. 全工程编译、44项针对性核心测试、受控 Unity EditMode/PlayMode、洛阳40万闭环回归与`git diff --check`。
2. 年份单独触发、魔法进口、AI直接写世界、Cold重生事实均有自动化拒绝证据。
3. 洛阳事件原型覆盖 Canonical、Prevented、Delayed、Variant/Transformed、离屏和保存后不重复。
4. Arena 支持固定 Scenario/Seed/Policy/Duration、指标和决策轨迹。
5. 本任务不物化全国5350万人、不训练正式神经网络、不实现完整189/190内容包和洛阳外部供应区。

## 执行状态

状态：已执行并通过本任务范围验收。正式结果见同名交付目录、主报告及`validation_summary.json`；未提交、未推送。
