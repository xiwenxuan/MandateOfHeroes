# 洛阳门桥守军权限、战斗损坏与真实维修 V1 任务书

任务 ID：`LUOYANG_PASSAGE_GUARD_DAMAGE_AND_REAL_REPAIR_V1`

状态：`TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

日期：2026-08-28

## 一、任务目标

承接 V74 的 20 项洛阳门桥正式通行状态，为每一次后续状态变化补齐可审计的世界原因链：

1. 由真实 Facility 的 Controller、真实组织、真实军队和永久 Person 建立守卫控制合同；
2. 正常开闭必须由当前控制组织领袖或守军军级指挥者授权，地图按钮不再是权限来源；
3. 损坏必须引用既有真实战斗记录、敌对军队和有军权指挥者，并保存损坏前后完整度；
4. 维修复用 V73 已有 Facility 修复项目、真实产品批次预留/消耗和具体人物劳动，不建立第二套库存或劳动账；
5. 维修完工后门桥进入“已修复但关闭”，必须另由守卫授权开启；
6. 世界模式由 V74 顺序升级到 V75，旧档不倒推守军、损坏百分比、战斗或维修历史。

## 二、权威输入与范围边界

- `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md`；
- `Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md`；
- `Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md`；
- `Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md`；
- `Docs/TASK_M25_P7_PERSISTENT_COMMAND_RESULTS_AND_EVENT_OUTBOX.md`；
- `Docs/TASK_M25_P29_FIELD_HOSPITAL_CONSTRUCTION_MAINTENANCE_AND_STAGED_CARE.md`；
- `Docs/TASK_LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1.md`。

本任务只处理“已经由战斗系统确认发生的战斗”如何损坏某一门桥，以及损坏后的真实维修。
它不计算攻城器械伤害，不创建战斗，不模拟冲车、投石机、火攻、地道、城防 AI 或战术动画。

## 三、冻结合同

| 合同项 | 冻结值 |
|---|---|
| 旧/新世界模式 | V74 → V75 |
| 守卫控制记录 | 按门桥显式建立，最多每项一条 V1 记录 |
| 守卫来源 | 同控制组织、同地点真实 Army 与在役永久 Person |
| 正常开闭授权 | 控制组织领袖或守军 Army 级指挥权 |
| 损坏来源 | 既有 `BattleRecordState`，攻击方 Army 级指挥权 |
| 损坏计量 | 0—10,000 完整度基点；每次命令保存前后值 |
| 维修状态 | 通用 `FacilityConstructionProjectState(Repair)` + 门桥维修关联记录 |
| 维修材料 | 城门/宫门/军门：8 木料＋2 铁料；桥梁：12 木料＋2 铁料 |
| 维修劳动与最短工期 | 门类 960 分钟/2 日；桥梁 1,440 分钟/3 日 |
| 维修资金 | 100 钱，由 Facility Owner 的真实账户支付 |
| 完工状态 | 完整度 10,000、Facility 恢复运行、门桥 `closed`，另行授权开启 |
| V74→V75 迁移 | 新集合为空；旧库存事务新增来源字段置空，不推定历史 |

以上材料、工时与资金是 V1 可验证玩法参数，不是东汉工程定额或考古结论。普通内容以后应以
稳定 Profile ID 扩展；增加普通维修 Profile 不应再次升级存档结构。

## 四、实施方案

1. 新增门桥守卫控制、损坏流水与维修关联状态；守卫记录冻结 Facility、Controller Organization、
   Guard Army、Commander、具体守军 Person、初始完整度、授权依据及命令/事件来源。
2. 扩展现有门桥转换命令的原因类型：
   - V74 兼容转换；
   - 守卫授权开闭；
   - 战斗损坏；
   - 维修完工。
   守卫合同建立后的转换不再允许退回无原因的兼容路径。
3. 新增守卫建立和维修开工持久命令；所有成功结果继续复用 M25-P7 事务摘要与 Outbox。
4. 维修开工调用现有 `PropertyConstructionSystem`，按稳定产品 ID 跨批次预留木料和铁料；劳动继续
   写入 `FacilityConstructionLaborState`，同一人物同日不得重复贡献主要建设劳动。
5. 维修完工事务原子消耗已预留材料、完成通用修复项目、恢复 Facility、关闭门桥并发布转换事件。
6. V75 校验闭合守卫人物、军权快照、战斗引用、完整度链、通用工程、材料事务、劳动与命令事件引用。

## 五、验收标准

1. 无真实 Facility、Controller Organization、同地 Army、军级权限或具体在役守军时，守卫建立原子拒绝。
2. 守卫建立后，地图系统身份、无关人物和普通成员不能开闭；合法组织领袖/守军主将可以开闭。
3. 战斗损坏必须引用同地、守军为防守方的既有战斗；同一战斗不能对同一门桥重复记损。
4. 损坏同步更新门桥完整度、Facility condition/lifecycle、通行状态、Revision 与追加式损坏流水。
5. 维修材料不足、错误所有者/地点/容器、无权限或无真实负责人时不产生项目或部分预留。
6. 真实 Person 劳动和最短工期满足后才可完工；完工真实消耗批次并留下库存事务来源。
7. 修复后仍关闭；另一次合法守卫命令开启后恢复正常通行。
8. V75 当前快照往返、V74 空迁移、篡改引用/守恒拒绝和相同输入确定性通过。
9. 全工程编译、定向核心、目标 EditMode、相关 PlayMode、`git diff --check` 与范围审阅分别记录。

## 六、明确不在范围

- 不实现完整城防网络重构、城墙缺口传播、钥匙、宵禁、通行证或逐人门禁队列；
- 不创建或结算战斗，不实现冲车、投石机、云梯、火攻、地道和攻城 AI；
- 不实现桥梁载重、洪水、船撞、逐构件损坏、施工事故、工资或自动采购；
- 不实现门扇、吊桥、瓦砾、施工和损毁动画，也不生成角色尺度 NavMesh；
- 不倒推 V74 旧档中不存在的守军、损坏程度、战斗、材料或劳动历史；
- 不提交、不推送、不关闭用户程序。

## 七、执行清单

- [x] 冻结任务书、兼容边界和 V1 参数。
- [x] 实现 V75 领域状态、不变量和顺序迁移。
- [x] 实现守卫、战斗损坏、维修开工/劳动/完工命令链。
- [x] 增加核心/Unity可发现的EditMode测试与存档回归；相关PlayMode沿用V74正式世界绑定回归。
- [x] 完成编译、核心、diff和范围审阅并回填证据。
- [x] Unity EditMode/PlayMode执行：按受控脚本在工作区外完成同命令重跑。

## 八、实施结果与证据

### 8.1 已实现

- `WorldState`正式模式推进到V75，并新增守军控制、追加式战损和维修关联集合；
- V74→V75是顺序空迁移，旧库存事务的工程来源字段只置空，不推定历史；
- 守军建立冻结真实Facility、Controller Organization、Army、Commander、在役永久Person及权限依据；
- 守军建立后的普通转换只接受组织领袖或守军主将，Presentation身份不能直接开闭；
- 战损只接受同地点、守军为防守方、攻击组织敌对的既有Battle，并保存完整度与Revision开闭值；
- 维修复用通用Facility修复工程、真实木料/铁料批次、工程来源库存事务及逐人物逐日劳动；
- 修复完工原子恢复Facility和10,000完整度，但通行保持关闭，另一次守军命令才可开启；
- 存档校验可拒绝伪造战斗、重复战损、错误工程来源、断裂完整度链和伪造修复完成事件。

### 8.2 验证结果

| 门禁 | 结果 | 证据 |
|---|---|---|
| 全工程编译 | 通过 | 最终记录：`tmp/skill-verification/compile-20260828-115300-202.out.log` |
| 定向核心 | 9/9通过 | 最终记录：`tmp/skill-verification/core-tests-20260828-115330-490.out.log` |
| Unity EditMode | 1/1通过 | `tmp/unity-validation/unity-EditMode-20260828-120229-047.summary.json`与非空NUnit XML |
| Unity EngineSmoke | 通过 | 受限工作区内的无日志启动边界由同一安全脚本在工作区外重跑排除：`tmp/unity-validation/unity-EngineSmoke-20260828-120149-149.summary.json` |
| 相关图形PlayMode | 1/1通过 | V74正式世界绑定回归：`tmp/unity-validation/unity-PlayMode-20260828-120301-170.summary.json`与非空NUnit XML |
| `git diff --check` | 通过 | 两次`verify-project.ps1 -SkipUnity`汇总均为`diff-check=passed` |

Unity工程声明与安装版本均为`2022.3.62f3c1`。首次在Codex受限工作区内运行时，目标
EditMode与无项目EngineSmoke均未生成启动日志；按项目测试规则使用同一安全脚本在工作区外
重跑后，EngineSmoke、目标EditMode和相关图形PlayMode均产生有效日志/XML并通过。这证实初始
阻塞是宿主沙箱启动边界，不是项目代码、测试筛选、项目锁或许可证失效。以上仍是定向验收，
不替代完整核心、EditMode或PlayMode分组回归。

## 九、后续候选

本任务完成后，下一实现候选应在以下方向中单独立项，不得从本任务自动扩大：

1. 完整围城与攻城器械对城墙/门桥的伤害结算；
2. 守军换防、失控、占领和组织控制权变更工作流；
3. 门扇、桥面、瓦砾与维修施工表现，以及人物尺度导航阻断；
4. 桥梁载重、洪水、船撞和逐构件状态。
