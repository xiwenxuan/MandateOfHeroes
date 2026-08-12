# 135—260家族空间与FamilyCenter开发参考报告 V1

## 1. 结论

本轮完成了家族组织中心的规则冻结、39个Canonical Clan的空间基线、13个剧本切片（507条Clan快照）、52条FamilyOrganization初始化候选、40条住宅/庄园/资产证据、洛阳184年32名人物复核、7个现有组织审计及10条中心候选建议。

成果只提高后续开发的可判定性，没有批量创建全国组织、家户、资产、庄园或中心Facility。

## 2. 洛阳184关键发现

1. 现有25人不是最终清单：保留原25条，并增加杨彪、袁绍、袁术、王允、蔡邕、董卓、曹嵩7个研究/排除样本。
2. 7个现有FamilyOrganization全部没有`family_facility_ids`，所以全部不能宣称已有Primary或Local Center。
3. `汉室主脉`把宦官并入历史成员，且以何皇后为家主；这混淆皇室核心家庭、后妃、宦官与国家宫廷组织。
4. `南阳何氏`历史成员列表混入马元义、张温、刘陶、唐周，并漏掉何皇后；符合按人口序号区间派生历史成员的错误特征。
5. 两个董氏记录缺少Canonical Clan锚点，其中一条以程序生成人物为家主；暂只能视为MODELED组织。
6. 弘农杨氏、汝南袁氏和扶风马氏在京任官只证明人物存在。杨、袁可进入Local Center研究队列；马氏当前仍是成员存在证据。

这些问题记录为审计缺陷，不在本任务中直接改写运行时数据，避免破坏27万永久人物与家户引用。

## 3. 历史空间方法

历史资料采用七层分离：Clan、Branch、Member、Residence、Estate、FamilyAsset、FamilyCenter。史料只支持到哪一层，就停在哪一层。8个既有庄园锚点均增加条件式承载判断：它们可以成为后续中心研究对象，但没有一项因“有庄园/田地”而自动升级为中心。

场景快照保持Master→稀疏Timeline/Change→Scenario继承。每个Scenario中的Clan活跃和成员信息不能派生FamilyOrganization；初始化参考只列候选边界，并统一标记`REFERENCE_ONLY_DO_NOT_INSTANTIATE`。

## 4. 数据产品

- 关系规范：`01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md`
- 中心规则及20项冻结决策：`02_FamilyCenter设计规则_V1.md`
- 动作矩阵与7份历史/洛阳工作簿：同目录03—10号文件
- 原始机器可读工作数据：`outputs/FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1/family_reference_workdata.json`

## 5. 证据与限制

本轮继承`Han135260V1`的1202人物、39 Clan、15 Branch、54人物地点记录、13 Scenario与深化层8个Estate Reference，并沿用其Primary Historical Text/Source Registry。重点依据包括《后汉书》人物与外戚/宦官记录、《三国志·糜竺传》《三国志·鲁肃传》《后汉书·樊宏传》。古籍索引只能支持其明确陈述，不能提供未知宅第边界、Cell、设施类型、管理面积或组织预算。

## 6. 下一开发阶段

下一步应是“洛阳184历史人物—家族组织—中心安全迁移切片”，顺序为：

1. 冻结7组织的V2成员映射，移除误卷入关系但保留所有Person；
2. 将皇室核心家庭、何氏、宦官/宫廷服务组织和国家资产分开；
3. 为候选组织研究真实住宅/庄园/管理Facility，不足则保持无中心；
4. 建立`FamilyManagement`数据定义、Primary/Local唯一性与Unstaffed状态；
5. 通过顺序存档迁移把V1组织引用升级到V2，做往返和不变量测试；
6. 最后才接入本地资产操作、通信延迟与玩家界面。
