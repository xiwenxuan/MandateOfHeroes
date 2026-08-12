# 洛阳 184 正式开发准备度审查 V1

## 1. 最终结论

| 门禁 | 结论 | 含义 |
|---|---|---|
| Gate A：洛阳核心是否可以开始正式实现 | `GO_WITH_BLOCKERS` | 40 万正式包的基础身份、引用与容量合同有效；下一实现任务必须同时关闭五项有界高优先级阻断。 |
| Gate B：Wave 0 是否可以整体进入实现 | `GO_WITH_DEFERRED_PLACES` | 洛阳 Core 可进入 Wave 0A；虎牢、函谷继续作为独立 Place 延后到 Wave 0B，不得伪造其 Cell/Facility 范围。 |

本结论不是“洛阳已经接入主游戏”。当前 40 万都市圈是有效、可重复审计的正式来源包，但尚未投影到 `NewGameSetup`、主 `WorldState`/存档和通用世界推进入口。

## 2. 机器证据摘要

| 项目 | 结果 |
|---|---:|
| 永久人物 | 400,000 |
| 家户 | 80,899 |
| Facility | 2,084 |
| 精确历史人物覆盖 | 25 |
| 家族组织 | 15（城市旧组织 7 + 近郊生成组织 8） |
| 军队/Force | 5 |
| 顺序初始化事件 | 10 |
| 受保护包文件 | 24 / 24 哈希与字节数通过 |
| 人物、家户、亲属、住宅、岗位引用错误 | 0 |
| 重复 PersonId / FacilityId / Facility Cell | 0 / 0 / 0 |
| 住宅、岗位、学生容量溢出 | 0 |
| 旧 Facility 内联人物列表字段 | 1,116 个字段需要去权威化或迁移 |

人口口径为包含关系：城内 20 万 ⊂ 连续城市区 27 万 ⊂ 都市圈 40 万 ⊂ 规划供给区 70 万。70 万尚未物化；全国人口母盘中的洛阳县 130,169 人是另一统计模型参考，均不得再加到 40 万正式人物上。

## 3. 三十项强制问题答复

1. **洛阳 Core 能否开始？** 能，以 `GO_WITH_BLOCKERS` 进入下一项集成任务。
2. **Gate A？** `GO_WITH_BLOCKERS`。
3. **区域开发能否开始？** 只能先做洛阳 Core，不能把 Wave 0 三地同时物化。
4. **Gate B？** `GO_WITH_DEFERRED_PLACES`。
5. **虎牢为何延后？** `geo.site.hulao` 的最终 CanonicalPlace/Cell 范围和分期 Facility 范围仍为研究阻断（DPB-017）。
6. **函谷为何延后？** `geo.site.hangu` 尚缺最终 Cell 范围、184 分期 Facility 组成及即时人口/军力范围。
7. **二者是否阻断洛阳 Core？** 不阻断；它们保持独立 Place、独立证据与独立后续验收。
8. **正式 184 初始化现在是什么？** 一个通过自身验证的 40 万都市圈来源包，加上 25 人覆盖、15 家族组织、5 Force、10 顺序事件；尚非主世界初始化。
9. **真实永久人物范围？** 270,000 城市人物加 130,000 近郊人物，合计 400,000，出生即有永久稳定身份。
10. **是否有重复人口风险？** 有集成风险但当前包内无重复；13 万洛阳县统计参考、27 万子集、70 万包含式规划都不得再次生成。
11. **历史人物怎样映射？** 25 个覆盖记录用 `Pxxxx` 精确替换相应人口 ordinal 的生成 ID，均能命中 1,202 人母库和 184 剧本。
12. **会不会重复生成历史人物？** 当前包不会；若历史人物母库由另一入口再次物化则会。下一任务必须以 PersonId 幂等绑定并拒绝二次创建。
13. **现有 Household 能否复用？** 能；80,899 户的 ordinal、成员连续段、户主和人物反向引用均通过。
14. **现有住宅能否复用？** 能；40 万人均绑定真实 Facility，正式容量无溢出，但必须以二进制 Facility index 为开局分配权威。
15. **现有 FamilyOrganization 是否保留？** 保留稳定组织 ID 和所有永久人物；7 个城市旧组织接受显式迁移，8 个近郊生成组织保持非历史主张。
16. **怎样迁移？** 只纠正组织语义与历史成员绑定；不删除、合并、重随机人物。`f088` 的皇室/宦官混入和 `f036` 的无关 ordinal 段必须显式拆解或改绑并留迁移记录。
17. **建议新增哪些组织？** 只在 Clan/Branch 证据和游戏内法权成立时新增；不得把 39 Clan 自动生成成 39 FamilyOrganization。
18. **每个家族都需要中心吗？** 不需要。成员在当地活动不以 FamilyCenter 为前提。
19. **FamilyCenter 缠缺什么？** 真实 Facility、`FamilyManagement` 能力、组织合法所有/控制、真实管理者 Person、Primary/Local 正式指定五项合同均未持久化。
20. **要不要先重构整个 Facility？** 不要。下一任务只增加必要的映射、能力和指定合同，通用 Facility 目录重构另案处理。
21. **历史参考如何映射运行时？** 以稳定 FacilityId、DefinitionId、CellId64、OwnerId、ControllerId 和可选生命周期参考交叉映射；Reference 不直接创造运行时事实。
22. **Cell/Owner 是否安全？** 2,084 个 FacilityId 和 Cell 无重复，Owner/Controller 可作为输入；仍需主世界投影验证权限对象存在。
23. **政府/官职准备好了吗？** 参考和少量人物 office 覆盖存在，但通用政府/职位状态投影未完成，不属于下一任务主体。
24. **军事是否阻断？** 五支军队来源数据有效，但尚非通用 Army/Force 存档投影；不阻断人物—家族集成，必须保持 OUT_OF_SCOPE。
25. **70 万供给区现在做吗？** 不做。它是包含 40 万的计划包络，不是新增 70 万人物。
26. **190 兼容性如何？** 设施生命周期、皇室/官府、城防、市场仓储和家族空间有前后参考；运行时 HistoricalChange 尚未实现。必须保留同一 Person/Household/Facility/Cell ID。
27. **是否需要存档迁移？** 若下一任务持久化 Person 历史绑定、FamilyOrganization profile 或 FamilyCenter 状态，必须从 V68 顺序升级并做旧版与往返测试。
28. **下一个 Domain/System 是什么？** 人物历史身份、Clan/Branch 与 FamilyOrganization/FamilyCenter 的主世界持久集成，同时接入 40 万唯一人口来源。
29. **什么明确不做？** 虎牢/函谷、70 万物化、全国人物化、全国家族组织、通用 Facility 重构、完整官府/军事/补给/历史变化、190 玩法、UI/美术/场景和新增史料研究。
30. **最终门禁？** Gate A=`GO_WITH_BLOCKERS`；Gate B=`GO_WITH_DEFERRED_PLACES`；下一任务固定为 `LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1`。

## 4. Gate A 必须随下一任务关闭的阻断

| ID | 严重度 | 阻断 | 完成条件 |
|---|---|---|---|
| LYR-IMP-001 | High | 40 万包未进入主 NewGame/WorldState/Save 路径 | 只有一个 40 万人口来源，主世界加载、保存、重载均保持同一 ID 与人数。 |
| LYR-IMP-002 | High | 历史 Person 覆盖可能被另一初始化入口重复物化 | 25 个 `Pxxxx` 幂等绑定，二次执行无新增人物，重复 ID 明确报错。 |
| LYR-IMP-003 | High | 7 个旧家族组织含污染成员且缺标准 profile | 稳定 ID 迁移、人物不删不并、错误历史绑定纠正、8 个近郊组织保留。 |
| LYR-IMP-004 | High | FamilyCenter 五要件无持久运行时合同 | 数据驱动能力、合法控制、管理者和 Primary/Local 指定可存档；默认仍为 NONE。 |
| LYR-IMP-005 | High | 旧 Facility 内联人物列表与正式二进制索引冲突 | 明确二进制索引为开局权威；旧字段迁移、移除或标记非权威，验证不再读出幽灵 ID。 |

## 5. 不构成重做基础的调整

- `place.han140.sili.henan.luoyang`、`admin.han140.sili.henan.luoyang`、`C027`、`location.capital.luoyang` 和稳定 Region 指向同一物理洛阳，但尚缺一个可持久的显式 crosswalk。
- `OrganizationState` 当前字段不足以表达历史 Clan/Branch profile 和 FamilyCenter；扩展应最小化且数据驱动。
- 五 Force 与十事件继续视为来源包数据，不能在本任务中被宣称为完整战争/历史执行系统。

## 6. 190 与稳定身份合同

190 状态必须在同一世界上以变化事件更新，不得创建第二张洛阳地图、第二套 190 人物、第二套家户或替换 Facility ID。现阶段只冻结兼容接口：稳定 ID、前后参考、迁移/焚毁/控制权变化的待执行记录；实际事件执行另立任务。

## 7. 验收声明

本审查证明“可进入有界集成”，不证明集成已经完成。所有机器结果可由 `MapPipeline/scripts/audit_luoyang_184_development_readiness_v1.py` 重放；工作簿只是可读审查视图，JSON/正式运行包和未来迁移后的 WorldState 才分别承担来源与运行时事实。
