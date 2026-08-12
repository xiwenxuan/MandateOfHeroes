# HAN-135-260-CORE-CITY-DEVELOPMENT-PACK-AND-UPGRADE-PROTOCOL-V1

## 1. 任务定位

本任务把首批十个核心城市整理为可直接供后续开发任务消费的`City Development Pack`，并冻结任意地点未来做细、补包和调整`DevelopmentDepth`时必须遵守的升级协议。

本任务是历史开发参考资料建设，不是Unity运行时实现。任务不得生成第二套Place、Cell、Facility、PermanentPerson、Population、Clan、FamilyOrganization或Scenario事实，不修改存档版本，不自动改变任何地点的D级。

## 2. 输入与权威边界

必须复用：

- 140年行政区、治所与CanonicalPlace母版；
- 135—260历史世界参考库与深化层；
- Development Place Roster、Readiness Matrix和既有D4/D5 Manifest；
- 1202名历史人物、39个Clan、15个Branch与13个Scenario稳定ID；
- 历史人口、统一Facility类型、家族空间参考和知识库Registry。

证据使用`HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN`。人物籍贯不等于实际在场，Clan成员在场不自动证明住宅、Estate、FamilyOrganization或FamilyCenter；无精确空间证据时不得伪造Cell。

## 3. 交付范围

首批城市：洛阳、长安、邺、许昌、成都、襄阳、江陵、建业、合肥、南郑。汉中作为战略显示名，物理地点统一落到`place.han140.yizhou.hanzhong.nanzheng`。

每城必须提供：

1. 城市身份与行政归属；
2. 历史状态与变化点；
3. 人口层级；
4. 城市形态；
5. Facility参考；
6. 历史人物在场；
7. Clan、家庭与Estate候选；
8. 产业农业；
9. 交通、周边聚落和供给腹地；
10. 军事空间；
11. Scenario切片；
12. 开发映射；
13. 来源与未知项。

每个城市目录交付`README.md`、`CITY_MASTER_REFERENCE.md`、`CITY_DEVELOPMENT_DATA.xlsx`、`DEVELOPMENT_READINESS.md`和`SOURCES_AND_UNKNOWNS.md`。总目录交付标准、升级协议、完整度报告和八份汇总工作簿。

## 4. 升档协议

- 72项Roster是当前专项制作计划，不是永久白名单；
- D0/D1地点未来可以补包并申请升档；
- 用户要求某地做细时，先解析既有CanonicalPlace，再创建或升级Pack；
- Pack经过来源、ID、未知项和运行时边界审计后，才可提出D级调整；
- `Pack Ready ≠ 自动升档 ≠ Runtime已实现`；最终调整由用户或正式开发计划决定；
- 升档只能增加资料与表现/实现精度，不得删除、合并、替代或重随机既有世界事实；
- 运行时Cell、Facility、人口物化、Family组织、HistoricalChangePackage和存档迁移必须另立任务。

## 5. 执行结果

- 已建立10/10城市Pack；洛阳为`DEVELOPMENT_READY`，其余9城为`READY_WITH_MODELED_GAPS`，0城为`RESEARCH_REQUIRED`；
- 已建立100条历史人物城市在场切片、123条Facility参考和89条未来升级登记；
- 已建立八份总工作簿与十份逐城16工作表数据簿；
- 已把Pack字段回写72项Roster，并保持D5=1、D4=15、D3=33、D2=23、D1=0，未自动升降档；
- 已把首批10城入口回写对应Development Manifest；
- 已更新系统总纲、历史资料入口、Roster入口、知识库入口、任务路由以及文档/领域/决策/缺口Registry；
- 已隔离成都既有`major_city_timeline`错误交叉引用，没有把`admin.han140.jingzhou.nanyang.chengdu`当作益州成都；
- 本任务运行时改动为0，Save Schema改动为0，Unity场景/程序集改动为0。

## 6. 验收标准

- 十城均解析到唯一CanonicalPlace；
- PersonId、ClanId、BranchId、ScenarioId、SourceId与Facility BaseType均可回查；
- 每城具有13类模块和规定的16张工作表；
- 非洛阳资料未伪造精确Cell，未知信息保持`UNKNOWN`；
- 72项Roster与75个战略标签所指向的CanonicalPlace均进入升级登记；
- 工作簿完成公式错误扫描和逐表渲染；
- 文档链接、UTF-8、重复ID、`git diff --check`和项目文档模式校验通过；
- 编译、核心测试和Unity测试不适用，因为本任务不修改代码、运行时内容、程序集、场景或存档。

## 7. 下一门禁

下一项历史世界工作为`LUOYANG-184-DEVELOPMENT-READINESS-REVIEW-V1`。在用户明确改变顺序前，不自动启动第二批城市Pack，也不把本任务的参考结果直接物化到Runtime。
