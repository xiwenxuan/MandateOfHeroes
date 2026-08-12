# LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1 任务书

## 1. 目标与完成口径

把184年洛阳初始化包中的25名历史人物、Clan/Branch、15个既有FamilyOrganization、FamilyCenter合同、历史官职和个人/组织资产，安全接入同一个V69 `WorldState`。正式开局仍唯一使用40万永久人物、80,899户和2,084项既有Facility；本任务不得新增或复制Person、Household、Facility、Cell，也不得删除、合并或重新随机永久人物。

## 2. 冻结边界

- 只处理洛阳184 Core，不物化虎牢、函谷或70万供给区。
- `Clan`、`Branch`、`Household`、`FamilyOrganization`、`FamilyCenter`分别建模。
- FamilyCenter只有在真实Facility、`capability.family_management`、合法Owner/Controller、Primary/Local指定、真实Manager及其当前活动全部成立时才能Active。
- 历史资料不明确时保留空值、Deferred或未决主张，不伪造所有权和中心。
- 个人资产保持个人账，组织资产使用独立Ledger。
- 受保护人口包只读；未来变化写入派生检查点/覆盖层，不反写初始化包。

## 3. 实施分解

| 编号 | 工作 | 验收 |
|---|---|---|
| P1 | 永久人口存储适配器 | 40万稳定ID可按分区读取；不物化40万Unity对象；初始化包只读 |
| P2 | 历史人物、Clan与Branch | 25/25精确绑定同一P-ID；0新增Person；Lineage独立于家户和组织 |
| P3 | FamilyOrganization迁移 | 保留15个稳定组织ID；纠正f088/f036共10条污染成员关系；人物与个人资产不受损 |
| P4 | FamilyCenter与Facility合同 | 增加数据驱动能力ID和持久状态；当前15个中心全部Deferred；0新增Facility |
| P5 | 官职、工作与活动 | Civil/Military Office绑定辖区、既有Facility、Person及CurrentActivity |
| P6 | V69存档迁移 | V68→V69顺序迁移、往返、确定性和二次接入幂等通过 |
| P7 | Presentation验证入口 | 洛阳验证页可选择历史人物，显示身份、宗族、家户、组织、官职、活动和中心状态 |
| P8 | 审计与交付 | 9份工作簿、报告、存档报告、validation summary、下一阶段建议及知识库更新 |

## 4. 架构合同

- `Mandate.Domain`：稳定关系、状态、验证规则和查询索引。
- `Mandate.Persistence`：受保护包适配器、跨包投影、V69迁移和序列化。
- `Mandate.Simulation`：本阶段不新增历史事件执行器；后续生活闭环通过正式世界调度器消费V69事实。
- `Mandate.Presentation`：只查询世界事实，不持有第二套人物、家族或设施真相。
- Content：原400K/80,899/2,084源包不改写；审计脚本只读。

## 5. 验证

顺序为：全工程编译→完整核心回归→Unity EditMode→Unity PlayMode→`git diff --check`→范围审阅。Unity只能通过`Tools/Run-UnityTestsSafe.ps1`，单次硬超时300秒。

## 6. 状态

实施完成。最终证据见同名交付目录中的报告、九份工作簿、存档兼容报告和`validation_summary.json`。本任务未授权提交或推送。
