# FAMILY-ORGANIZATION-CENTER-AND-HISTORICAL-FAMILY-REFERENCE-V1 任务书

## 1. 任务状态

状态：**已完成（参考资料与规则阶段）**
完成日期：2026-08-11

本任务建立家族组织中心规则、135—260历史家族空间参考与184洛阳审计。它不授权全国生成
`FamilyOrganization`、`Household`、普通Clan资产、庄园或FamilyCenter Facility，也不修改现有27万
洛阳永久人物运行时包。

## 2. 最高原则

> 家族成员可以在没有FamilyCenter的城市正常居住、任官、经商、买地和发展；FamilyCenter限制的是
> FamilyOrganization在当地的正式组织管理能力，而不是族人的存在与发展能力。

## 3. 必须完成的范围

- 冻结Person、Household、Clan、Branch、FamilyOrganization、FamilyCenter关系；
- 冻结Primary/Local/REMOTE/NONE/DISABLED中心状态及20项开放问题；
- 建立FamilyManagement动作权限矩阵；
- 建立39 Clan空间参考和13个Scenario家族空间快照；
- 建立只读FamilyOrganization初始化候选，不执行物化；
- 扩展184洛阳历史人物空间审查，不把既有25人当作最终清单；
- 审计洛阳7个现有FamilyOrganization及FamilyCenter候选；
- 继承8个Estate Reference并区分“可承载”与“已有中心”。

## 4. 交付物

正式成果位于[`FAMILY_ORGANIZATION_REFERENCE_V1`](FAMILY_ORGANIZATION_REFERENCE_V1/)：

1. `01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md`
2. `02_FamilyCenter设计规则_V1.md`
3. `03_FamilyManagement_Action_Matrix_V1.xlsx`
4. `04_135-260重要HistoricalClan空间状态参考.xlsx`
5. `05_13Scenario_HistoricalFamilySpatialSnapshots.xlsx`
6. `06_FamilyOrganizationInitializationReference.xlsx`
7. `07_HistoricalResidence_Estate_FamilyAsset_Reference.xlsx`
8. `08_184洛阳历史人物与家族空间参考.xlsx`
9. `09_184洛阳现有FamilyOrganization一致性审计.xlsx`
10. `10_184洛阳FamilyCenter候选与开发建议.xlsx`
11. `11_135-260家族空间与FamilyCenter开发参考报告_V1.md`

机器可读工作数据、渲染预览、检查记录与验证报告位于
`outputs/FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1/`。

## 5. 验收结果

- 22项动作权限：完成；
- 39个Clan空间基线：完成；
- 39×13=507条Scenario快照：完成；
- 52条初始化候选：完成，全部为`REFERENCE_ONLY_DO_NOT_INSTANTIATE`；
- 40条住宅/庄园/家族资产证据：完成；
- 184洛阳人物：原25人加7个研究/排除候选，共32人；
- 现有7组织审计：完成，全部因无真实FamilyManagement Facility而保持CenterStatus=`NONE`；
- 20项问题：19项`FROZEN`，第20项`OPEN_WITH_RECOMMENDATION`；
- 工作簿渲染、结构检查和公式错误扫描：完成；
- 运行时代码、存档版本、Unity场景：未修改，因此按纯文档模式验收。

## 6. 后续入口

下一阶段是“洛阳184历史人物—家族组织—中心安全迁移切片”：先修复V1成员映射，再建立真实Facility、
能力、产权、管理者和指定记录，最后通过顺序存档迁移接入运行时。未经单独任务书不得直接开始全国物化。
