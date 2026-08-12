# 洛阳 City Master Reference

## 01 Identity / Geography

- CanonicalPlace：`place.han140.sili.henan.luoyang`；战略显示名：`洛阳`；历史名：雒阳|洛阳。
- 行政：`admin.han140.sili` → `admin.han140.sili.henan` → `admin.han140.sili.henan.luoyang`。
- 地理：河洛盆地，洛水穿行，北接黄河，东西关隘控制首都走廊。
- 地形/水系/山地：盆地平原与河谷台地；洛水|黄河方向|护城壕；邙山|嵩山方向。
- 道路/邻接：虎牢—洛阳东向走廊|函谷—长安西向走廊|孟津北向通道|南阳方向；虎牢|函谷关|孟津方向|河南尹近郊县。

## 02 Administrative / Political

历史治所只作为HistoricalSeatReference；Runtime Seat由实际Government Facility、Office、Authority和Controller决定，不能写死未来迁治。

## 03 Population

184县人口引用：130169；城墙人口：200000；连续城区：270000；都市圈：400000；供给圈：700000。各层为包含关系，不可相加，县人口不等于城市人口。

## 04 Urban Spatial Form

东汉首都宫城、外城、十二门、市场、太学、官署、住宅和近郊共同组成多层都市。 城墙/城门：外城墙、十二门、宫墙与护城壕已有项目级历史/复原资料。 十二门已有稳定Facility引用。 内城/官署：南宫、北宫及中央官署区；宫墙与外城墙独立。 近郊/扩展：400,000都市圈已正式包；700,000供给圈仅为计划且包含都市圈。 189—190政治危机、迁都与毁坏改变城市功能；后续状态不得覆盖运行世界分歧。

## 05 Facility

共15条Reference，历史锚点与Simulation Completion Requirements分开；历史名称映射统一BaseType，不自创新Facility枚举。

## 06 HistoricalPerson

当前城市切片10条稳定PersonId记录；籍贯不等于当前位置，Confirmed与Probable分开。

## 07 Clan / Family / Estate

当前2条Clan/Branch切片。成员在场、住宅、Estate、FamilyOrganization与FamilyCenter相互独立，均不得自动推导。

## 08 Industry / Agriculture / Resources

- 产业：[{'City': '洛阳', 'PlaceId': 'place.han140.sili.henan.luoyang', 'Category': 'Industry', 'Reference': '粟麦粮食加工|丝织与官营手工业|冶炼锻造|车辆木作|酿造食品|文书与教育服务', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '洛阳', 'PlaceId': 'place.han140.sili.henan.luoyang', 'Category': 'Agriculture', 'Reference': '河洛粟麦与近郊菜蔬|畜牧|桑蚕；供给圈通过仓储和道路进入都市', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '洛阳', 'PlaceId': 'place.han140.sili.henan.luoyang', 'Category': 'Resources', 'Reference': '河谷农地|木材与石料由外围输入|金属与奢侈品跨区输入', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '洛阳', 'PlaceId': 'place.han140.sili.henan.luoyang', 'Category': 'OccupationStructure', 'Reference': '官吏|士人|军人|商人|工匠|农户|雇工|仆役|门客|学生|医生|宗教人员|流民', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '洛阳', 'PlaceId': 'place.han140.sili.henan.luoyang', 'Category': 'Workforce', 'Reference': '未来Person物化必须按真实岗位、年龄、技能、家庭与住宅约束', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '洛阳', 'PlaceId': 'place.han140.sili.henan.luoyang', 'Category': 'ProductionMapping', 'Reference': 'FacilityDefinition + Recipe + real worker + material + time + authority', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}]
- 农业：河洛粟麦与近郊菜蔬|畜牧|桑蚕；供给圈通过仓储和道路进入都市
- 资源：河谷农地|木材与石料由外围输入|金属与奢侈品跨区输入

所有产出必须来自Facility + Recipe + real worker + material + time + authority。

## 09 Transport / Logistics / Surrounding Settlements

400,000都市圈已正式包；700,000供给圈仅为计划且包含都市圈。 供应链统一为Producer/Settlement → Storage → Road/Water → Gate/Harbor → Urban Storage/Market → Household/Military/Facility。

## 10 Military

首都城防、宫城独立防线、十二门、虎牢/函谷走廊、孟津渡运与驻军共同组成防务空间。

## 11 Scenario Snapshot / 12 HistoricalChangePoint

支持6个相关Scenario/TimePoint；已知ChangePoint使用稳定ID，未知变化不伪造Package。

## 13 Development Implication

Pack状态`DEVELOPMENT_READY`，完整度96/100。该状态只允许后续任务消费Reference，不自动改变DevelopmentDepth或运行时世界。

## Canonical references

- [既有P0/P1核心聚落Master](../../../../Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_洛阳_place_han140_sili_henan_luoyang/00_Master.md)
- [Development Pack Standard](../CITY_DEVELOPMENT_PACK_STANDARD_V1.md)
- [Upgrade Protocol](../CITY_DEVELOPMENT_PACK_UPGRADE_PROTOCOL_V1.md)
