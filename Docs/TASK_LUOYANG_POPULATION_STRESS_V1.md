# LUOYANG-POPULATION-STRESS-V1 执行任务书

状态：已完成，带已知验证限制（2026-08-09）。

## 目标

在不修改 `MapData/Luoyang184Historical_V1` 的前提下，建立 20,542、50K、100K、250K、500K 五档永久人物压力包；分别验证固定设施与 AI 自适应真实建设，测量住房、岗位、查询、LOD、内存、存取和 2,000 米 Cell 容量。

## 不变量

- 每一人口计数对应一个可寻址永久 Person；不得以汇总数替代。
- 20,542 名历史基线人物的 ID 与核心事实保持不变。
- 仍使用 `HanWorldV1`、2,000 米 Cell；不建立 SubCell，一个 Cell 最多一个基础 Facility。
- AI 新建筑必须经历计划、批准、施工、完工状态，具有 Owner、Controller、Cell 和原因。
- 压力 Profile 相互隔离；历史设施、十二城门、城墙、护城壕及宫城不得被覆盖。
- 500K 不生成同量 GameObject；表现层只读取摘要并采用有上限的高关注 Actor。

## 交付

1. 数据驱动建设候选配置、确定性生成器和独立审计器。
2. `MapData/LuoyangPopulationStress_V1` 五档二进制 Person 数据、摘要、建设记录及十二份报告。
3. `Assets/StreamingAssets/WorldMap/LuoyangPopulationStressV1` 轻量运行摘要。
4. Domain 压力/索引/建设合同，Persistence 二进制随机访问合同，Presentation Profile/模式切换。
5. 核心、EditMode、PlayMode 回归，以及编译、Python 审计、Unity、`git diff --check` 证据。

## 执行顺序

P1 基线哈希与约束审计 → P2 五档永久 Person 生成 → P3 固定/自适应 365 日推演 → P4 Unity 适配 → P5 自动化测试 → P6 十二报告与最终验收。

## 完成口径

只有上述输出实际存在、五档数量与二进制长度一致、历史数据哈希未变、测试结果文件明确通过且无本任务 Unity 残留进程时，才可标记完成。任何 500K 长期推演、Unity 环境或容量限制均须在最终报告中逐项披露。

## 实际验收

- 五档永久 Person、固定/自适应 365 日摘要、真实建设记录及十二份报告已生成；独立 Python 全量审计通过。
- 全工程 MSBuild 通过；本阶段核心测试 5/5、EditMode 4/4、洛阳 PlayMode 1/1 通过。
- 项目既有全套核心与全套 EditMode 均在 300 秒硬边界内未完成，不能声称全量通过；没有失败断言证据，限制详见最终报告。
- 250K 与 500K 自适应推演均耗尽 4,510 个剩余可开发 Cell，证明当前 2,000 米 Cell 在帝都高密度档需要容量/算法再平衡；没有据此建立 SubCell 或修改全国格网。
- 受控 Unity 测试完成后无残留 Unity 进程；`git diff --check` 通过。
