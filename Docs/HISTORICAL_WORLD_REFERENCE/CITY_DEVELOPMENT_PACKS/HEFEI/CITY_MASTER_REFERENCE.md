# 合肥 City Master Reference

## 01 Identity / Geography

- CanonicalPlace：`place.han140.yangzhou.jiujiang.hefei`；战略显示名：`合肥`；历史名：合肥|合肥县。
- 行政：`admin.han140.yangzhou` → `admin.han140.yangzhou.jiujiang` → `admin.han140.yangzhou.jiujiang.hefei`。
- 地理：淮南丘陵与巢湖水系之间的陆水节点，控制江淮南北交通。
- 地形/水系/山地：丘陵岗地与河湖平原；巢湖水系|淝水方向|江淮水路；大别山东缘方向。
- 道路/邻接：寿春北向|濡须/长江南向|庐江西向|丹阳东南向；寿春|濡须口|庐江|巢湖水网。

## 02 Administrative / Political

历史治所只作为HistoricalSeatReference；Runtime Seat由实际Government Facility、Office、Authority和Controller决定，不能写死未来迁治。

## 03 Population

184县人口引用：31454；城墙人口：UNKNOWN；连续城区：2788；都市圈：UNKNOWN；供给圈：UNKNOWN。各层为包含关系，不可相加，县人口不等于城市人口。

## 04 Urban Spatial Form

县城、曹魏前线城防与230年代合肥新城必须分期；战略名不创造第二座城。 城墙/城门：早期县城和后期新城防线分离；当前精确边界不足。 门名与数量UNKNOWN。 内城/官署：前线军政区、仓储和驻军功能高于宫廷功能。 近郊/扩展：巢湖水网、县域农业聚落与濡须方向军路。 208后长期前线化、215合肥之战、230年代新城建设。

## 05 Facility

共12条Reference，历史锚点与Simulation Completion Requirements分开；历史名称映射统一BaseType，不自创新Facility枚举。

## 06 HistoricalPerson

当前城市切片10条稳定PersonId记录；籍贯不等于当前位置，Confirmed与Probable分开。

## 07 Clan / Family / Estate

当前3条Clan/Branch切片。成员在场、住宅、Estate、FamilyOrganization与FamilyCenter相互独立，均不得自动推导。

## 08 Industry / Agriculture / Resources

- 产业：[{'City': '合肥', 'PlaceId': 'place.han140.yangzhou.jiujiang.hefei', 'Category': 'Industry', 'Reference': '军械|粮食加工|船修|木工|仓储|运输服务', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '合肥', 'PlaceId': 'place.han140.yangzhou.jiujiang.hefei', 'Category': 'Agriculture', 'Reference': '江淮稻麦|豆类|渔业|畜牧', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '合肥', 'PlaceId': 'place.han140.yangzhou.jiujiang.hefei', 'Category': 'Resources', 'Reference': '巢湖水运|江淮农地|木材石料与金属输入', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '合肥', 'PlaceId': 'place.han140.yangzhou.jiujiang.hefei', 'Category': 'OccupationStructure', 'Reference': '官吏|士人|军人|商人|工匠|农户|雇工|仆役|门客|学生|医生|宗教人员|流民', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '合肥', 'PlaceId': 'place.han140.yangzhou.jiujiang.hefei', 'Category': 'Workforce', 'Reference': '未来Person物化必须按真实岗位、年龄、技能、家庭与住宅约束', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '合肥', 'PlaceId': 'place.han140.yangzhou.jiujiang.hefei', 'Category': 'ProductionMapping', 'Reference': 'FacilityDefinition + Recipe + real worker + material + time + authority', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}]
- 农业：江淮稻麦|豆类|渔业|畜牧
- 资源：巢湖水运|江淮农地|木材石料与金属输入

所有产出必须来自Facility + Recipe + real worker + material + time + authority。

## 09 Transport / Logistics / Surrounding Settlements

巢湖水网、县域农业聚落与濡须方向军路。 供应链统一为Producer/Settlement → Storage → Road/Water → Gate/Harbor → Urban Storage/Market → Household/Military/Facility。

## 10 Military

合肥是江淮前线节点；县城、新城、巢湖水路、濡须方向与寿春军路须按Scenario分期。

## 11 Scenario Snapshot / 12 HistoricalChangePoint

支持4个相关Scenario/TimePoint；已知ChangePoint使用稳定ID，未知变化不伪造Package。

## 13 Development Implication

Pack状态`READY_WITH_MODELED_GAPS`，完整度79/100。该状态只允许后续任务消费Reference，不自动改变DevelopmentDepth或运行时世界。

## Canonical references

- [既有P0/P1核心聚落Master](../../../../Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P2_合肥_place_han140_yangzhou_jiujiang_hefei/00_Master.md)
- [Development Pack Standard](../CITY_DEVELOPMENT_PACK_STANDARD_V1.md)
- [Upgrade Protocol](../CITY_DEVELOPMENT_PACK_UPGRADE_PROTOCOL_V1.md)
