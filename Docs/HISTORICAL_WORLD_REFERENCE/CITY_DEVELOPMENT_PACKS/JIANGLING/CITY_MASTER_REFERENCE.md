# 江陵 City Master Reference

## 01 Identity / Geography

- CanonicalPlace：`place.han140.jingzhou.nan.jiangling`；战略显示名：`江陵`；历史名：江陵|江陵县。
- 行政：`admin.han140.jingzhou` → `admin.han140.jingzhou.nan` → `admin.han140.jingzhou.nan.jiangling`。
- 地理：长江中游江汉平原核心，连接襄阳、江夏、夷陵和洞庭湖区。
- 地形/水系/山地：冲积平原与湖沼；长江|江汉水网；荆山西北方向。
- 道路/邻接：襄阳北向|夷陵西向|江夏东向|武陵/长沙南向；夷陵|公安方向|江夏|江汉平原县邑。

## 02 Administrative / Political

历史治所只作为HistoricalSeatReference；Runtime Seat由实际Government Facility、Office、Authority和Controller决定，不能写死未来迁治。

## 03 Population

184县人口引用：90234；城墙人口：32376；连续城区：44966；都市圈：62053；供给圈：UNKNOWN。各层为包含关系，不可相加，县人口不等于城市人口。

## 04 Urban Spatial Form

南郡治所、长江港运、城防和广阔近郊农业共同组成荆州中枢。 城墙/城门：江陵城防历史明确，具体门名和逐期修筑需重建。 逐门资料不足。 内城/官署：南郡官署及208后多方争夺下的军政中心。 近郊/扩展：江汉平原农业聚落、港渡和县邑形成宽广供给圈。 208赤壁后争夺、210前后吴蜀控制变化、219荆州易手。

## 05 Facility

共12条Reference，历史锚点与Simulation Completion Requirements分开；历史名称映射统一BaseType，不自创新Facility枚举。

## 06 HistoricalPerson

当前城市切片10条稳定PersonId记录；籍贯不等于当前位置，Confirmed与Probable分开。

## 07 Clan / Family / Estate

当前5条Clan/Branch切片。成员在场、住宅、Estate、FamilyOrganization与FamilyCenter相互独立，均不得自动推导。

## 08 Industry / Agriculture / Resources

- 产业：[{'City': '江陵', 'PlaceId': 'place.han140.jingzhou.nan.jiangling', 'Category': 'Industry', 'Reference': '粮食加工|造船修船|木工|纺织|军械|仓储', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '江陵', 'PlaceId': 'place.han140.jingzhou.nan.jiangling', 'Category': 'Agriculture', 'Reference': '江汉平原稻麦|渔业|桑麻|畜牧', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '江陵', 'PlaceId': 'place.han140.jingzhou.nan.jiangling', 'Category': 'Resources', 'Reference': '长江水运|荆楚木材|湖沼渔业|农地', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '江陵', 'PlaceId': 'place.han140.jingzhou.nan.jiangling', 'Category': 'OccupationStructure', 'Reference': '官吏|士人|军人|商人|工匠|农户|雇工|仆役|门客|学生|医生|宗教人员|流民', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '江陵', 'PlaceId': 'place.han140.jingzhou.nan.jiangling', 'Category': 'Workforce', 'Reference': '未来Person物化必须按真实岗位、年龄、技能、家庭与住宅约束', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '江陵', 'PlaceId': 'place.han140.jingzhou.nan.jiangling', 'Category': 'ProductionMapping', 'Reference': 'FacilityDefinition + Recipe + real worker + material + time + authority', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}]
- 农业：江汉平原稻麦|渔业|桑麻|畜牧
- 资源：长江水运|荆楚木材|湖沼渔业|农地

所有产出必须来自Facility + Recipe + real worker + material + time + authority。

## 09 Transport / Logistics / Surrounding Settlements

江汉平原农业聚落、港渡和县邑形成宽广供给圈。 供应链统一为Producer/Settlement → Storage → Road/Water → Gate/Harbor → Urban Storage/Market → Household/Military/Facility。

## 10 Military

长江中游港城、南郡城防、夷陵方向与襄阳方向道路组成荆州军需核心。

## 11 Scenario Snapshot / 12 HistoricalChangePoint

支持4个相关Scenario/TimePoint；已知ChangePoint使用稳定ID，未知变化不伪造Package。

## 13 Development Implication

Pack状态`READY_WITH_MODELED_GAPS`，完整度84/100。该状态只允许后续任务消费Reference，不自动改变DevelopmentDepth或运行时世界。

## Canonical references

- [既有P0/P1核心聚落Master](../../../../Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_江陵_place_han140_jingzhou_nan_jiangling/00_Master.md)
- [Development Pack Standard](../CITY_DEVELOPMENT_PACK_STANDARD_V1.md)
- [Upgrade Protocol](../CITY_DEVELOPMENT_PACK_UPGRADE_PROTOCOL_V1.md)
