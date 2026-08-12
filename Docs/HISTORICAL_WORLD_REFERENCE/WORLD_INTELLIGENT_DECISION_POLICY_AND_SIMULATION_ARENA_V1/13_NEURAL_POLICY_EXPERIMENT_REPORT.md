# Neural Policy 实验报告

## 实验设计

V1 Neural 不是自治世界模型，而是合法候选动作的只读 Scorer。模型为 12 维输入、单隐藏层 8 单元 ReLU MLP；特征不含年份。训练数据为 2,400 条确定性 Utility Teacher 候选评分样本，按 Scenario Seed 分组切分：训练 1,920 行/320 Seed，测试 480 行/80 Seed；训练 Seed 固定，可复现。

## 结果

- 测试 RMSE：696.64 basis points
- 测试 MAE：557.07 basis points
- 测试集 Top Action Agreement：91.25%
- 4,000 次 Arena 中 Neural Adapter 运行 1,000 次、44,000 次决策、9,900 次改变世界，平均每次最小 Fixture 运行 0.162 ms

NN 的确改善了“逼近 Utility Teacher 的候选排序”和固定小网络的推理速度/可部署性；但它没有证明比 Utility 带来更好的十年世界质量。相反，可解释性弱于 Utility，约 8.75% 测试样本的首选动作不一致，而且当前 Fixture 的建设、贸易、产业与政府指标没有足够真实闭环来验证长期收益。

## 推荐

- Household：Rule/Utility；高频基础生存不优先用 Neural。
- FamilyOrganization：Utility。
- Merchant：Neural Candidate，仅用于离线版本化实验；商旅候选丰富，最适合继续比较。
- Settlement：Utility。
- Government：Utility，重大历史事件真假仍由 Event Engine 决定。

因此本阶段判定为：Utility 是正式可解释主基线；Neural 对 Merchant 有继续实验价值，但不得成为全国默认。模型缺失、NaN、输出非法或 Schema 不匹配时必须安全回退，且任何 Neural 输出都必须重走 Validator/Executor。
