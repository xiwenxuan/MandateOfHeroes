# 襄阳 City Master Reference

## 01 Identity / Geography

- CanonicalPlace：`place.han140.jingzhou.nan.xiangyang`；战略显示名：`襄阳`；历史名：襄阳|襄阳县。
- 行政：`admin.han140.jingzhou` → `admin.han140.jingzhou.nan` → `admin.han140.jingzhou.nan.xiangyang`。
- 地理：汉水中游襄樊渡运节点，连接南阳盆地、江陵和汉中方向。
- 地形/水系/山地：河谷平原与丘陵；汉水|襄樊渡运；岘山|荆山方向。
- 道路/邻接：南阳北向|江陵南向|汉中西向|江夏东向；樊城|新野|江陵|隆中方向。

## 02 Administrative / Political

历史治所只作为HistoricalSeatReference；Runtime Seat由实际Government Facility、Office、Authority和Controller决定，不能写死未来迁治。

## 03 Population

184县人口引用：41933；城墙人口：12951；连续城区：17988；都市圈：24823；供给圈：UNKNOWN。各层为包含关系，不可相加，县人口不等于城市人口。

## 04 Urban Spatial Form

襄阳城、汉水岸线、对岸樊城与近郊士族庄园共同形成跨河城市网络。 城墙/城门：襄阳城防历史价值明确，门名和184逐段状态需重建。 具体门名UNKNOWN。 内城/官署：南郡北部行政与刘表时期荆州政治中心功能按Scenario表达。 近郊/扩展：樊城、汉水两岸聚落、隆中/岘山近郊与农业区。 190s刘表治荆州、208曹军南下、219襄樊战役改变城防和人口。

## 05 Facility

共12条Reference，历史锚点与Simulation Completion Requirements分开；历史名称映射统一BaseType，不自创新Facility枚举。

## 06 HistoricalPerson

当前城市切片10条稳定PersonId记录；籍贯不等于当前位置，Confirmed与Probable分开。

## 07 Clan / Family / Estate

当前3条Clan/Branch切片。成员在场、住宅、Estate、FamilyOrganization与FamilyCenter相互独立，均不得自动推导。

## 08 Industry / Agriculture / Resources

- 产业：[{'City': '襄阳', 'PlaceId': 'place.han140.jingzhou.nan.xiangyang', 'Category': 'Industry', 'Reference': '粮食加工|木工|船修|纺织|军械|商旅服务', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '襄阳', 'PlaceId': 'place.han140.jingzhou.nan.xiangyang', 'Category': 'Agriculture', 'Reference': '汉水谷地稻麦|桑麻|渔业|近郊园圃', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '襄阳', 'PlaceId': 'place.han140.jingzhou.nan.xiangyang', 'Category': 'Resources', 'Reference': '汉水运输|荆山木材|农地与渔业', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '襄阳', 'PlaceId': 'place.han140.jingzhou.nan.xiangyang', 'Category': 'OccupationStructure', 'Reference': '官吏|士人|军人|商人|工匠|农户|雇工|仆役|门客|学生|医生|宗教人员|流民', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '襄阳', 'PlaceId': 'place.han140.jingzhou.nan.xiangyang', 'Category': 'Workforce', 'Reference': '未来Person物化必须按真实岗位、年龄、技能、家庭与住宅约束', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '襄阳', 'PlaceId': 'place.han140.jingzhou.nan.xiangyang', 'Category': 'ProductionMapping', 'Reference': 'FacilityDefinition + Recipe + real worker + material + time + authority', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}]
- 农业：汉水谷地稻麦|桑麻|渔业|近郊园圃
- 资源：汉水运输|荆山木材|农地与渔业

所有产出必须来自Facility + Recipe + real worker + material + time + authority。

## 09 Transport / Logistics / Surrounding Settlements

樊城、汉水两岸聚落、隆中/岘山近郊与农业区。 供应链统一为Producer/Settlement → Storage → Road/Water → Gate/Harbor → Urban Storage/Market → Household/Military/Facility。

## 10 Military

襄阳与樊城保持独立Place，通过汉水渡运和军路形成同一战区；219是最高优先变化节点。

## 11 Scenario Snapshot / 12 HistoricalChangePoint

支持4个相关Scenario/TimePoint；已知ChangePoint使用稳定ID，未知变化不伪造Package。

## 13 Development Implication

Pack状态`READY_WITH_MODELED_GAPS`，完整度85/100。该状态只允许后续任务消费Reference，不自动改变DevelopmentDepth或运行时世界。

## Canonical references

- [既有P0/P1核心聚落Master](../../../../Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_襄阳_place_han140_jingzhou_nan_xiangyang/00_Master.md)
- [Development Pack Standard](../CITY_DEVELOPMENT_PACK_STANDARD_V1.md)
- [Upgrade Protocol](../CITY_DEVELOPMENT_PACK_UPGRADE_PROTOCOL_V1.md)
