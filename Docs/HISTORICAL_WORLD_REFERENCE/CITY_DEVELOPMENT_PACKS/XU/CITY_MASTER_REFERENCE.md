# 许昌 City Master Reference

## 01 Identity / Geography

- CanonicalPlace：`place.han140.yuzhou.yingchuan.xu`；战略显示名：`许昌`；历史名：许|许昌。
- 行政：`admin.han140.yuzhou` → `admin.han140.yuzhou.yingchuan` → `admin.han140.yuzhou.yingchuan.xu`。
- 地理：颍川平原的中原交通节点，连接洛阳、陈留、汝颍与淮河方向。
- 地形/水系/山地：平原河网；颍水水系；西北嵩山余脉方向。
- 道路/邻接：洛阳西北向|陈留东北向|汝南东南向|南阳西南向；颍川县邑|陈留|洛阳|汝南。

## 02 Administrative / Political

历史治所只作为HistoricalSeatReference；Runtime Seat由实际Government Facility、Office、Authority和Controller决定，不能写死未来迁治。

## 03 Population

184县人口引用：67086；城墙人口：22573；连续城区：31351；都市圈：43264；供给圈：UNKNOWN。各层为包含关系，不可相加，县人口不等于城市人口。

## 04 Urban Spatial Form

184县级城镇、196献帝都许后的宫廷/官署与曹操政权设施必须分期表达。 城墙/城门：县城城防存在，196后强化程度与边界待重建。 具体门名和数量UNKNOWN。 内城/官署：196后宫廷、司空府及中央官署区采用RECONSTRUCTED。 近郊/扩展：颍川农业县邑与交通聚落构成供给圈。 196迎献帝都许是核心变化；220魏代汉后政治功能重新配置。

## 05 Facility

共12条Reference，历史锚点与Simulation Completion Requirements分开；历史名称映射统一BaseType，不自创新Facility枚举。

## 06 HistoricalPerson

当前城市切片10条稳定PersonId记录；籍贯不等于当前位置，Confirmed与Probable分开。

## 07 Clan / Family / Estate

当前4条Clan/Branch切片。成员在场、住宅、Estate、FamilyOrganization与FamilyCenter相互独立，均不得自动推导。

## 08 Industry / Agriculture / Resources

- 产业：[{'City': '许昌', 'PlaceId': 'place.han140.yuzhou.yingchuan.xu', 'Category': 'Industry', 'Reference': '粟麦加工|纺织|车辆木作|军械|仓储|文书服务', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '许昌', 'PlaceId': 'place.han140.yuzhou.yingchuan.xu', 'Category': 'Agriculture', 'Reference': '颍川粟麦|豆类|桑麻|近郊菜蔬', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '许昌', 'PlaceId': 'place.han140.yuzhou.yingchuan.xu', 'Category': 'Resources', 'Reference': '中原农地|木材石料输入|金属与军需跨区输入', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '许昌', 'PlaceId': 'place.han140.yuzhou.yingchuan.xu', 'Category': 'OccupationStructure', 'Reference': '官吏|士人|军人|商人|工匠|农户|雇工|仆役|门客|学生|医生|宗教人员|流民', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '许昌', 'PlaceId': 'place.han140.yuzhou.yingchuan.xu', 'Category': 'Workforce', 'Reference': '未来Person物化必须按真实岗位、年龄、技能、家庭与住宅约束', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '许昌', 'PlaceId': 'place.han140.yuzhou.yingchuan.xu', 'Category': 'ProductionMapping', 'Reference': 'FacilityDefinition + Recipe + real worker + material + time + authority', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}]
- 农业：颍川粟麦|豆类|桑麻|近郊菜蔬
- 资源：中原农地|木材石料输入|金属与军需跨区输入

所有产出必须来自Facility + Recipe + real worker + material + time + authority。

## 09 Transport / Logistics / Surrounding Settlements

颍川农业县邑与交通聚落构成供给圈。 供应链统一为Producer/Settlement → Storage → Road/Water → Gate/Harbor → Urban Storage/Market → Household/Military/Facility。

## 10 Military

中原交通枢纽和汉廷所在地；城防、军粮、通往官渡/陈留/洛阳的道路是主要军事空间。

## 11 Scenario Snapshot / 12 HistoricalChangePoint

支持5个相关Scenario/TimePoint；已知ChangePoint使用稳定ID，未知变化不伪造Package。

## 13 Development Implication

Pack状态`READY_WITH_MODELED_GAPS`，完整度88/100。该状态只允许后续任务消费Reference，不自动改变DevelopmentDepth或运行时世界。

## Canonical references

- [既有P0/P1核心聚落Master](../../../../Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_许昌_place_han140_yuzhou_yingchuan_xu/00_Master.md)
- [Development Pack Standard](../CITY_DEVELOPMENT_PACK_STANDARD_V1.md)
- [Upgrade Protocol](../CITY_DEVELOPMENT_PACK_UPGRADE_PROTOCOL_V1.md)
