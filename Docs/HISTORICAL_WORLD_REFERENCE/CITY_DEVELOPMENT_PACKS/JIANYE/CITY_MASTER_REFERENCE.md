# 建业 City Master Reference

## 01 Identity / Geography

- CanonicalPlace：`place.han140.yangzhou.danyang.moling`；战略显示名：`建业`；历史名：秣陵|建业。
- 行政：`admin.han140.yangzhou` → `admin.han140.yangzhou.danyang` → `admin.han140.yangzhou.danyang.moling`。
- 地理：长江下游南岸的山水港城，连接丹阳腹地、江东水网和长江航道。
- 地形/水系/山地：沿江丘陵与冲积地；长江|秦淮水系方向；钟山|石头山方向。
- 道路/邻接：吴郡东向|丹阳南向|皖/濡须西向|长江水路；石头城|丹阳郡县邑|吴郡方向|濡须口方向。

## 02 Administrative / Political

历史治所只作为HistoricalSeatReference；Runtime Seat由实际Government Facility、Office、Authority和Controller决定，不能写死未来迁治。

## 03 Population

184县人口引用：41832；城墙人口：14867；连续城区：20649；都市圈：28496；供给圈：UNKNOWN。各层为包含关系，不可相加，县人口不等于城市人口。

## 04 Urban Spatial Form

184仍以秣陵县城和沿江聚落为主；211石头城、212改建业、229吴都形成分期跃迁。 城墙/城门：184县城边界与211后石头城/都城防务不得混为一体。 各阶段门名和边界需分期研究。 内城/官署：229后吴宫廷和中央官署区；184不应提前生成。 近郊/扩展：沿江港聚落、丹阳农业县邑与山丘防御节点。 211石头城建设、212改名建业、221/229都城迁移与建设。

## 05 Facility

共12条Reference，历史锚点与Simulation Completion Requirements分开；历史名称映射统一BaseType，不自创新Facility枚举。

## 06 HistoricalPerson

当前城市切片10条稳定PersonId记录；籍贯不等于当前位置，Confirmed与Probable分开。

## 07 Clan / Family / Estate

当前5条Clan/Branch切片。成员在场、住宅、Estate、FamilyOrganization与FamilyCenter相互独立，均不得自动推导。

## 08 Industry / Agriculture / Resources

- 产业：[{'City': '建业', 'PlaceId': 'place.han140.yangzhou.danyang.moling', 'Category': 'Industry', 'Reference': '造船|木工|冶炼锻造|纺织|粮食加工|军需|港运服务', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '建业', 'PlaceId': 'place.han140.yangzhou.danyang.moling', 'Category': 'Agriculture', 'Reference': '丹阳稻作|桑麻|渔业|山地林产', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '建业', 'PlaceId': 'place.han140.yangzhou.danyang.moling', 'Category': 'Resources', 'Reference': '长江航运|木材|铁料输入|农地与渔业', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '建业', 'PlaceId': 'place.han140.yangzhou.danyang.moling', 'Category': 'OccupationStructure', 'Reference': '官吏|士人|军人|商人|工匠|农户|雇工|仆役|门客|学生|医生|宗教人员|流民', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '建业', 'PlaceId': 'place.han140.yangzhou.danyang.moling', 'Category': 'Workforce', 'Reference': '未来Person物化必须按真实岗位、年龄、技能、家庭与住宅约束', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '建业', 'PlaceId': 'place.han140.yangzhou.danyang.moling', 'Category': 'ProductionMapping', 'Reference': 'FacilityDefinition + Recipe + real worker + material + time + authority', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}]
- 农业：丹阳稻作|桑麻|渔业|山地林产
- 资源：长江航运|木材|铁料输入|农地与渔业

所有产出必须来自Facility + Recipe + real worker + material + time + authority。

## 09 Transport / Logistics / Surrounding Settlements

沿江港聚落、丹阳农业县邑与山丘防御节点。 供应链统一为Producer/Settlement → Storage → Road/Water → Gate/Harbor → Urban Storage/Market → Household/Military/Facility。

## 10 Military

长江港运、石头城、江东内线和水军设施形成防御核心；不同Scenario不得提前出现后期都城设施。

## 11 Scenario Snapshot / 12 HistoricalChangePoint

支持4个相关Scenario/TimePoint；已知ChangePoint使用稳定ID，未知变化不伪造Package。

## 13 Development Implication

Pack状态`READY_WITH_MODELED_GAPS`，完整度83/100。该状态只允许后续任务消费Reference，不自动改变DevelopmentDepth或运行时世界。

## Canonical references

- [既有P0/P1核心聚落Master](../../../../Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_建业_place_han140_yangzhou_danyang_moling/00_Master.md)
- [Development Pack Standard](../CITY_DEVELOPMENT_PACK_STANDARD_V1.md)
- [Upgrade Protocol](../CITY_DEVELOPMENT_PACK_UPGRADE_PROTOCOL_V1.md)
