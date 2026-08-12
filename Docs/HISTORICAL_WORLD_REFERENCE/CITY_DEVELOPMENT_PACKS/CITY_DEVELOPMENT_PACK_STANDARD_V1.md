# Development Pack Standard V1

## 定位

Development Pack是任何Place进入更细开发前的资料门。城市使用City Development Profile；县城、关隘、港渡、聚落、战场或Estate Complex沿用同一标准并按物理类型裁剪不适用项。

## 十三个必备模块

1. Identity / Geography
2. Administrative / Political
3. Population
4. Urban Spatial Form
5. Facility
6. HistoricalPerson
7. Clan / Family / Estate
8. Industry / Agriculture / Resources
9. Transport / Logistics / Surrounding Settlements
10. Military
11. Scenario Snapshot
12. HistoricalChangePoint
13. Development Readiness / Unknowns / Development Implications

## 证据与空间精度

- `HISTORICAL`：正史、考古或正式资料直接支撑。
- `RECONSTRUCTED`：多项证据保守复原，保留推理与来源。
- `MODELED`：为运行容量和玩法补足，不冒充史实。
- `UNKNOWN`：证据不足；不等于不存在。
- Facility空间精度只使用`EXACT_SITE / APPROXIMATE_ZONE / CITY_LEVEL_ONLY / UNKNOWN`。不知道位置时禁止硬塞Cell。

## 文件结构

每个Pack至少包含`README.md`、`CITY_MASTER_REFERENCE.md`、`CITY_DEVELOPMENT_DATA.xlsx`、`DEVELOPMENT_READINESS.md`和`SOURCES_AND_UNKNOWNS.md`。工作簿固定16个工作表，从`00_INDEX`至`15_UNKNOWNS`。

## Ready标准

- `DEVELOPMENT_READY`：Canonical身份、关键人口层、城市形态、核心Facility、人物/家族切片、产业、交通、军事、Scenario和ChangePoint足以进入正式Readiness Review。
- `READY_WITH_MODELED_GAPS`：核心开发方向稳定，普通住宅/工坊/街巷或局部人口层仍需MODELED/UNKNOWN补全；可排期，但进入具体Runtime前必须关闭对应最小缺口。
- `READY_WITH_MIGRATION`：资料已通过，但修正既有Scenario/Save需要独立迁移任务。
- `RESEARCH_REQUIRED`：Canonical身份、位置、治所、人口量级或关键历史状态存在真正阻塞。
- `BLOCKED`：冲突导致无法建立稳定世界对象。

存在UNKNOWN不自动阻塞；CanonicalPlace未解析、稳定ID冲突、人口量级完全未知或关键历史状态互相矛盾才是Blocker。

## 数据引用原则

Pack只保存城市视角切片与开发解释。Person引用`PersonId`，Clan/Branch引用稳定ID，人口引用Han135260V1，Scenario引用ScenarioId，Facility引用统一BaseType/Profile/Capability。不得复制第二套母表。

## Runtime边界

Pack不创建Place、Cell、Facility实例、PermanentPerson、FamilyOrganization、FamilyCenter、Force或Save迁移。历史锚点与Simulation Completion Requirements必须分开；后者由人口、产业、行政、军需和物流推导。

## 升格关系

Pack通过仅意味着资料门通过。DevelopmentDepth是否改变，由用户和开发计划另行决定。升级必须保留所有既有稳定ID和世界事实；正在运行的存档不得因制作深度变化凭空增加建筑或人口。
