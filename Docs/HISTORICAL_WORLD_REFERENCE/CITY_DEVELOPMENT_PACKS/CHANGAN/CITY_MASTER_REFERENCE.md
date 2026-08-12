# 长安 City Master Reference

## 01 Identity / Geography

- CanonicalPlace：`place.han140.sili.jingzhao.changan`；战略显示名：`长安`；历史名：长安。
- 行政：`admin.han140.sili` → `admin.han140.sili.jingzhao` → `admin.han140.sili.jingzhao.changan`。
- 地理：渭河平原中部，关中道路与山口网络中心；东汉使用状态须与西汉都城遗址分开。
- 地形/水系/山地：关中平原与渭河阶地；渭水|灞水方向|关中渠系；秦岭北麓|北山方向。
- 道路/邻接：函谷/潼关东向|陈仓西向|武关东南向|北地西北向；潼关|函谷关|武关|陈仓|京兆近郊县。

## 02 Administrative / Political

历史治所只作为HistoricalSeatReference；Runtime Seat由实际Government Facility、Office、Authority和Controller决定，不能写死未来迁治。

## 03 Population

184县人口引用：58183；城墙人口：18799；连续城区：26110；都市圈：36032；供给圈：UNKNOWN。各层为包含关系，不可相加，县人口不等于城市人口。

## 04 Urban Spatial Form

汉长安旧城、城垣、宫殿遗址与东汉/董卓时期重新作为政治中心的城市功能必须分期表达。 城墙/城门：汉长安城垣有考古基础；184实际使用强度和修缮状态需分区重建。 历史城门体系存在，东汉末逐门使用状态不明。 内城/官署：西汉宫殿区遗存不等于184全部在用；190迁都后宫廷/官署需Scenario重建。 近郊/扩展：渭河南北近郊、京兆县邑和农业聚落构成供给层。 190迁都后政治功能骤升，192—195李傕郭汜冲突持续改变城防、人口与供应。

## 05 Facility

共12条Reference，历史锚点与Simulation Completion Requirements分开；历史名称映射统一BaseType，不自创新Facility枚举。

## 06 HistoricalPerson

当前城市切片10条稳定PersonId记录；籍贯不等于当前位置，Confirmed与Probable分开。

## 07 Clan / Family / Estate

当前1条Clan/Branch切片。成员在场、住宅、Estate、FamilyOrganization与FamilyCenter相互独立，均不得自动推导。

## 08 Industry / Agriculture / Resources

- 产业：[{'City': '长安', 'PlaceId': 'place.han140.sili.jingzhao.changan', 'Category': 'Industry', 'Reference': '粟麦加工|车辆木作|皮革畜牧|冶炼锻造|军需|仓储转运', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '长安', 'PlaceId': 'place.han140.sili.jingzhao.changan', 'Category': 'Agriculture', 'Reference': '关中粟麦|豆类|畜牧|近郊菜蔬', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '长安', 'PlaceId': 'place.han140.sili.jingzhao.changan', 'Category': 'Resources', 'Reference': '关中农地|秦岭木材石料|西北畜产与金属输入', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '长安', 'PlaceId': 'place.han140.sili.jingzhao.changan', 'Category': 'OccupationStructure', 'Reference': '官吏|士人|军人|商人|工匠|农户|雇工|仆役|门客|学生|医生|宗教人员|流民', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '长安', 'PlaceId': 'place.han140.sili.jingzhao.changan', 'Category': 'Workforce', 'Reference': '未来Person物化必须按真实岗位、年龄、技能、家庭与住宅约束', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '长安', 'PlaceId': 'place.han140.sili.jingzhao.changan', 'Category': 'ProductionMapping', 'Reference': 'FacilityDefinition + Recipe + real worker + material + time + authority', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}]
- 农业：关中粟麦|豆类|畜牧|近郊菜蔬
- 资源：关中农地|秦岭木材石料|西北畜产与金属输入

所有产出必须来自Facility + Recipe + real worker + material + time + authority。

## 09 Transport / Logistics / Surrounding Settlements

渭河南北近郊、京兆县邑和农业聚落构成供给层。 供应链统一为Producer/Settlement → Storage → Road/Water → Gate/Harbor → Urban Storage/Market → Household/Military/Facility。

## 10 Military

关中门户、旧城城垣、宫廷驻军和多方向道路组成战略纵深；长期内战可切断供给。

## 11 Scenario Snapshot / 12 HistoricalChangePoint

支持5个相关Scenario/TimePoint；已知ChangePoint使用稳定ID，未知变化不伪造Package。

## 13 Development Implication

Pack状态`READY_WITH_MODELED_GAPS`，完整度86/100。该状态只允许后续任务消费Reference，不自动改变DevelopmentDepth或运行时世界。

## Canonical references

- [既有P0/P1核心聚落Master](../../../../Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_长安_place_han140_sili_jingzhao_changan/00_Master.md)
- [Development Pack Standard](../CITY_DEVELOPMENT_PACK_STANDARD_V1.md)
- [Upgrade Protocol](../CITY_DEVELOPMENT_PACK_UPGRADE_PROTOCOL_V1.md)
