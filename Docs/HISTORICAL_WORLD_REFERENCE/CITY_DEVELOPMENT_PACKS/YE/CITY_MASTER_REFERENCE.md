# 邺 City Master Reference

## 01 Identity / Geography

- CanonicalPlace：`place.han140.jizhou.wei.ye`；战略显示名：`邺`；历史名：邺|邺城。
- 行政：`admin.han140.jizhou` → `admin.han140.jizhou.wei` → `admin.han140.jizhou.wei.ye`。
- 地理：漳水流域、河北平原南缘的政治军事中心，连接太行山口与黄河北岸。
- 地形/水系/山地：平原河谷；漳水|黄河北岸水网；太行山东麓。
- 道路/邻接：邯郸/常山北向|黎阳/黄河南向|太行山口西向|青州东向；邯郸|黎阳方向|官渡走廊|太行山口。

## 02 Administrative / Political

历史治所只作为HistoricalSeatReference；Runtime Seat由实际Government Facility、Office、Authority和Controller决定，不能写死未来迁治。

## 03 Population

184县人口引用：63368；城墙人口：22129；连续城区：30735；都市圈：42414；供给圈：UNKNOWN。各层为包含关系，不可相加，县人口不等于城市人口。

## 04 Urban Spatial Form

早期郡县城、袁绍政权中心与曹魏都城前身分期叠加；210年铜雀台等建设形成重大空间变化。 城墙/城门：邺城城防可考，分期边界与具体城门需保守处理。 门名和逐期状态不完整，按CITY_LEVEL_ONLY。 内城/官署：袁绍/曹操政务区与210年后台苑工程分期。 近郊/扩展：漳水灌溉农业、县邑和军屯构成供给圈。 200官渡前后、204曹操入邺、210铜雀台营建、220魏政权转换。

## 05 Facility

共12条Reference，历史锚点与Simulation Completion Requirements分开；历史名称映射统一BaseType，不自创新Facility枚举。

## 06 HistoricalPerson

当前城市切片10条稳定PersonId记录；籍贯不等于当前位置，Confirmed与Probable分开。

## 07 Clan / Family / Estate

当前4条Clan/Branch切片。成员在场、住宅、Estate、FamilyOrganization与FamilyCenter相互独立，均不得自动推导。

## 08 Industry / Agriculture / Resources

- 产业：[{'City': '邺', 'PlaceId': 'place.han140.jizhou.wei.ye', 'Category': 'Industry', 'Reference': '河北粮食加工|纺织|军械|车辆|营建|仓储', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '邺', 'PlaceId': 'place.han140.jizhou.wei.ye', 'Category': 'Agriculture', 'Reference': '漳水农业|粟麦|豆类|畜牧|军屯候选', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '邺', 'PlaceId': 'place.han140.jizhou.wei.ye', 'Category': 'Resources', 'Reference': '河北粮食|太行木石|北方畜产与金属输入', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '邺', 'PlaceId': 'place.han140.jizhou.wei.ye', 'Category': 'OccupationStructure', 'Reference': '官吏|士人|军人|商人|工匠|农户|雇工|仆役|门客|学生|医生|宗教人员|流民', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '邺', 'PlaceId': 'place.han140.jizhou.wei.ye', 'Category': 'Workforce', 'Reference': '未来Person物化必须按真实岗位、年龄、技能、家庭与住宅约束', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '邺', 'PlaceId': 'place.han140.jizhou.wei.ye', 'Category': 'ProductionMapping', 'Reference': 'FacilityDefinition + Recipe + real worker + material + time + authority', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}]
- 农业：漳水农业|粟麦|豆类|畜牧|军屯候选
- 资源：河北粮食|太行木石|北方畜产与金属输入

所有产出必须来自Facility + Recipe + real worker + material + time + authority。

## 09 Transport / Logistics / Surrounding Settlements

漳水灌溉农业、县邑和军屯构成供给圈。 供应链统一为Producer/Settlement → Storage → Road/Water → Gate/Harbor → Urban Storage/Market → Household/Military/Facility。

## 10 Military

河北政权中枢、漳水和黄河通道、城防与军粮仓储共同支撑袁曹战争和魏初防务。

## 11 Scenario Snapshot / 12 HistoricalChangePoint

支持5个相关Scenario/TimePoint；已知ChangePoint使用稳定ID，未知变化不伪造Package。

## 13 Development Implication

Pack状态`READY_WITH_MODELED_GAPS`，完整度85/100。该状态只允许后续任务消费Reference，不自动改变DevelopmentDepth或运行时世界。

## Canonical references

- [既有P0/P1核心聚落Master](../../../../Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_邺_place_han140_jizhou_wei_ye/00_Master.md)
- [Development Pack Standard](../CITY_DEVELOPMENT_PACK_STANDARD_V1.md)
- [Upgrade Protocol](../CITY_DEVELOPMENT_PACK_UPGRADE_PROTOCOL_V1.md)
