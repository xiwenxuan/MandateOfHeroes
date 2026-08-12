# 南郑 City Master Reference

## 01 Identity / Geography

- CanonicalPlace：`place.han140.yizhou.hanzhong.nanzheng`；战略显示名：`汉中`；历史名：南郑|汉中（战略显示名/郡名）。
- 行政：`admin.han140.yizhou` → `admin.han140.yizhou.hanzhong` → `admin.han140.yizhou.hanzhong.nanzheng`。
- 地理：汉中盆地核心治所，北接关中、南连蜀地，控制秦巴山地通道。
- 地形/水系/山地：盆地河谷与山地关隘；汉水上游；秦岭|大巴山。
- 道路/邻接：阳平关北西向|褒斜/傥骆方向|金牛道南向|米仓道东南向；阳平关|西城|上庸|剑阁方向|汉中县邑。

## 02 Administrative / Political

历史治所只作为HistoricalSeatReference；Runtime Seat由实际Government Facility、Office、Authority和Controller决定，不能写死未来迁治。

## 03 Population

184县人口引用：48208；城墙人口：UNKNOWN；连续城区：8721；都市圈：UNKNOWN；供给圈：UNKNOWN。各层为包含关系，不可相加，县人口不等于城市人口。

## 04 Urban Spatial Form

CanonicalPhysicalPlace是南郑；汉中是战略/行政显示层。郡治城市、张鲁政权和魏蜀争夺分期。 城墙/城门：南郑城防历史价值明确，精确城垣和门名不足。 具体门名UNKNOWN。 内城/官署：汉中郡官署、张鲁政权管理区和后续军政设施。 近郊/扩展：汉中盆地农业聚落、山口驿站和军粮节点。 194张鲁控制、215曹操入汉中、219刘备夺取形成关键变化。

## 05 Facility

共12条Reference，历史锚点与Simulation Completion Requirements分开；历史名称映射统一BaseType，不自创新Facility枚举。

## 06 HistoricalPerson

当前城市切片10条稳定PersonId记录；籍贯不等于当前位置，Confirmed与Probable分开。

## 07 Clan / Family / Estate

当前4条Clan/Branch切片。成员在场、住宅、Estate、FamilyOrganization与FamilyCenter相互独立，均不得自动推导。

## 08 Industry / Agriculture / Resources

- 产业：[{'City': '南郑', 'PlaceId': 'place.han140.yizhou.hanzhong.nanzheng', 'Category': 'Industry', 'Reference': '粮食加工|木工|药材|军械|仓储|山地运输', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '南郑', 'PlaceId': 'place.han140.yizhou.hanzhong.nanzheng', 'Category': 'Agriculture', 'Reference': '汉中盆地稻麦|粟豆|桑麻|山地林产', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '南郑', 'PlaceId': 'place.han140.yizhou.hanzhong.nanzheng', 'Category': 'Resources', 'Reference': '盆地农地|秦巴木材药材|山地矿产候选', 'EvidenceLevel': 'RECONSTRUCTED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '南郑', 'PlaceId': 'place.han140.yizhou.hanzhong.nanzheng', 'Category': 'OccupationStructure', 'Reference': '官吏|士人|军人|商人|工匠|农户|雇工|仆役|门客|学生|医生|宗教人员|流民', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '南郑', 'PlaceId': 'place.han140.yizhou.hanzhong.nanzheng', 'Category': 'Workforce', 'Reference': '未来Person物化必须按真实岗位、年龄、技能、家庭与住宅约束', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}, {'City': '南郑', 'PlaceId': 'place.han140.yizhou.hanzhong.nanzheng', 'Category': 'ProductionMapping', 'Reference': 'FacilityDefinition + Recipe + real worker + material + time + authority', 'EvidenceLevel': 'MODELED', 'FacilityRecipeMapping': 'REQUIRED', 'DevelopmentImplication': '不产生抽象产能；未来落到Facility、工单、库存和永久人物。'}]
- 农业：汉中盆地稻麦|粟豆|桑麻|山地林产
- 资源：盆地农地|秦巴木材药材|山地矿产候选

所有产出必须来自Facility + Recipe + real worker + material + time + authority。

## 09 Transport / Logistics / Surrounding Settlements

汉中盆地农业聚落、山口驿站和军粮节点。 供应链统一为Producer/Settlement → Storage → Road/Water → Gate/Harbor → Urban Storage/Market → Household/Military/Facility。

## 10 Military

南郑、阳平关、秦岭诸道和南向蜀道共同构成山地战区；行政Region、战略Label与PhysicalPlace必须分离。

## 11 Scenario Snapshot / 12 HistoricalChangePoint

支持4个相关Scenario/TimePoint；已知ChangePoint使用稳定ID，未知变化不伪造Package。

## 13 Development Implication

Pack状态`READY_WITH_MODELED_GAPS`，完整度80/100。该状态只允许后续任务消费Reference，不自动改变DevelopmentDepth或运行时世界。

## Canonical references

- [既有P0/P1核心聚落Master](../../../../Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P1_南郑_place_han140_yizhou_hanzhong_nanzheng/00_Master.md)
- [Development Pack Standard](../CITY_DEVELOPMENT_PACK_STANDARD_V1.md)
- [Upgrade Protocol](../CITY_DEVELOPMENT_PACK_UPGRADE_PROTOCOL_V1.md)
