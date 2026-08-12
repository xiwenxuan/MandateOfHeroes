# 成都 City Master Reference

## 01 Identity / Geography

- CanonicalPlace：`place.han140.yizhou.shu.chengdu`；战略显示名：`成都`；历史名：成都。
- 行政：`admin.han140.yizhou` → `admin.han140.yizhou.shu` → `admin.han140.yizhou.shu.chengdu`。
- 地理：成都平原与岷江灌溉体系腹地，益州/蜀汉政治经济中心。
- 地形/水系/山地：冲积平原；岷江水系|都江堰灌溉网络；成都平原西缘山地|北向剑门山系。
- 道路/邻接：金牛道北向|米仓道东北向|江州东向|南中南向；雒县方向|广汉|剑阁通道|江州方向。

## 02 Administrative / Political

历史治所只作为HistoricalSeatReference；Runtime Seat由实际Government Facility、Office、Authority和Controller决定，不能写死未来迁治。

## 03 Population

184县人口引用：173229；城墙人口：UNKNOWN；连续城区：31338；都市圈：UNKNOWN；供给圈：UNKNOWN。各层为包含关系，不可相加，县人口不等于城市人口。

## 04 Urban Spatial Form

秦汉以来成都城、益州州治与214后刘备政权、221蜀汉都城分期叠加。 城墙/城门：城垣历史存在，东汉末/蜀汉分期边界与门名需进一步考古对照。 具体门名和逐期位置不在当前母库，保持UNKNOWN。 内城/官署：益州官署、州牧府和221后宫廷区按分期重建。 近郊/扩展：成都平原密集农业聚落与水利网络构成强供给圈。 194刘璋时期、214易主、221蜀汉建国、263后状态需分期。

## 05 Facility

共12条Reference，历史锚点与Simulation Completion Requirements分开；历史名称映射统一BaseType，不自创新Facility枚举。

## 06 HistoricalPerson

当前城市切片10条稳定PersonId记录；籍贯不等于当前位置，Confirmed与Probable分开。

## 07 Clan / Family / Estate

当前3条Clan/Branch切片。成员在场、住宅、Estate、FamilyOrganization与FamilyCenter相互独立，均不得自动推导。

## 08 Industry / Agriculture / Resources

- 产业：[{'City': '成都', 'PlaceId': 'place.han140.yizhou.shu.chengdu', 'Category': 'Industry', 'Reference': '蜀锦丝织|盐业及加工|冶炼锻造|木工车辆|酿造食品|药材加工|军需', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '成都', 'PlaceId': 'place.han140.yizhou.shu.chengdu', 'Category': 'Agriculture', 'Reference': '水稻|小麦|粟黍|桑蚕|蔬果|畜牧；依托岷江水利', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '成都', 'PlaceId': 'place.han140.yizhou.shu.chengdu', 'Category': 'Resources', 'Reference': '成都平原农地|蜀地盐井输入|木竹药材|金属与石料', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '成都', 'PlaceId': 'place.han140.yizhou.shu.chengdu', 'Category': 'OccupationStructure', 'Reference': '官吏|士人|军人|商人|工匠|农户|雇工|仆役|门客|学生|医生|宗教人员|流民', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '成都', 'PlaceId': 'place.han140.yizhou.shu.chengdu', 'Category': 'Workforce', 'Reference': '未来Person物化必须按真实岗位、年龄、技能、家庭与住宅约束', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '成都', 'PlaceId': 'place.han140.yizhou.shu.chengdu', 'Category': 'ProductionMapping', 'Reference': 'FacilityDefinition + Recipe + real worker + material + time + authority', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}]
- 农业：水稻|小麦|粟黍|桑蚕|蔬果|畜牧；依托岷江水利
- 资源：成都平原农地|蜀地盐井输入|木竹药材|金属与石料

所有产出必须来自Facility + Recipe + real worker + material + time + authority。

## 09 Transport / Logistics / Surrounding Settlements

成都平原密集农业聚落与水利网络构成强供给圈。 供应链统一为Producer/Settlement → Storage → Road/Water → Gate/Harbor → Urban Storage/Market → Household/Military/Facility。

## 10 Military

盆地纵深、北向剑阁/汉中通道、州都城防和区域粮仓共同构成蜀地战略核心。

## 11 Scenario Snapshot / 12 HistoricalChangePoint

支持4个相关Scenario/TimePoint；已知ChangePoint使用稳定ID，未知变化不伪造Package。

## 13 Development Implication

Pack状态`READY_WITH_MODELED_GAPS`，完整度84/100。该状态只允许后续任务消费Reference，不自动改变DevelopmentDepth或运行时世界。

## Canonical references

- [既有P0/P1核心聚落Master](../../../../Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_成都_place_han140_yizhou_shu_chengdu/00_Master.md)
- [Development Pack Standard](../CITY_DEVELOPMENT_PACK_STANDARD_V1.md)
- [Upgrade Protocol](../CITY_DEVELOPMENT_PACK_UPGRADE_PROTOCOL_V1.md)
