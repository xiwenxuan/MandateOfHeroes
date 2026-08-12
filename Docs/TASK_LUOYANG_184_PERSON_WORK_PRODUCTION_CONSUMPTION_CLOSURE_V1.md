# LUOYANG-184-PERSON-WORK-PRODUCTION-CONSUMPTION-CLOSURE-V1

## 任务定位

本任务在既有184年洛阳受保护初始化包之上，建立第一条可持续运行的生活经济闭环：

`PermanentPerson → CurrentActivity / Work → Facility Production → Inventory → Distribution / Market → Household → Person Consumption`

固定基线为400,000个永久人物、80,899个家户、2,084个设施。不得新增、删除、合并、替换或重新随机这些对象；40万都市之外约30万人规模的供应区域本轮不得物化。

## 强制合同

- Person是唯一劳动力主体；军役、官职、学习和家族管理占用当前活动，不能同时作为普通设施全时工人。
- 设施必须读取真实人物岗位，低于最低用工即停产；最低与最优用工分离。
- 加工必须消耗真实输入、经过GameTime、受仓储容量约束并记录产出与损耗。
- 作物必须记录播种、生长、80%早收门槛、真实收割工人、种子回收和下一周期。
- Household按真实成员需求消费；批量结算只能压缩事务数量，不能压缩或删除人物。
- 市场和分配只能移动已有库存；供应不足必须形成短缺和AI响应。
- 现有5条外围供应链只能以`TRANSITIONAL_REFERENCE_SUPPLY`进入；不得创建每日魔法进口。
- 运行至少覆盖1、7、30、365日，并验证无负库存、资源守恒、确定性与Save/Load。
- 运行时明细进入派生检查点；受保护初始化二进制保持只读。

## 架构交付

- `Mandate.Domain`：劳动力、设施生产、作物、家户、库存、市场、短缺、日快照、性能和V70摘要合同。
- `Mandate.Persistence`：受保护包只读适配器、gzip派生检查点、V69→V70顺序迁移。
- `Mandate.Simulation`：人物扫描、工作判定、非线性用工门槛、生产/作物/消费/短缺日推进。
- `Mandate.Presentation`：洛阳验证场景中的生活世界摘要及人物、家户、设施调试选择器。

## 验收矩阵

1. 全工程编译。
2. 核心回归和本任务核心测试。
3. Unity EditMode：15项闭环测试。
4. Unity PlayMode：验证场景初始化、调试选择器、无40万GameObject。
5. 1/7/30/365日证据、检查点往返、相同种子确定性。
6. 九份xlsx审计、三份专题报告、主报告和`validation_summary.json`。
7. `git diff --check`与范围审阅。

## 执行记录（2026-08-11）

- 状态：代码、运行证据和工作簿已完成；最终全量验证结果见正式报告与`validation_summary.json`。
- 世界Schema：69升级为70；旧存档迁移只建立空的生活世界摘要集合，不编造运行状态。
- 保护数量变化：Person=0、Household=0、Facility=0。
- 真实结果：洛阳全年保持严重供粮缺口，明确输出`SUPPLY_REGION_DEPENDENCY`；下一阶段由证据选择外围供应区物化，而不是在本阶段补假库存。

## 最终验收（2026-08-11）

- 全工程编译：通过。
- 核心回归：按当前源码指纹分为12个受控组，563/563通过；聚合证据位于`tmp/core-test-groups/luoyang_living_final_current_20260811/aggregate.json`。
- Unity EditMode：15/15通过；Unity PlayMode：1/1通过，均通过`Tools/Run-UnityTestsSafe.ps1`执行。
- 365日资源守恒、无负库存、派生检查点往返与确定性证据：通过。
- `git diff --check`与最终范围审阅：通过；未提交、未推送。
