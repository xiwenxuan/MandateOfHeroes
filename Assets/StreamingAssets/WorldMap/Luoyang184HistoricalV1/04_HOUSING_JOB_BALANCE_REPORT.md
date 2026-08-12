# 04 住房—岗位平衡报告

## 人口与住房（唯一口径：Person）

- 永久Person：20,542
- 永久Household：4,498（仅关系事实，不作为住房容量单位）
- 已住房Person：20,414
- 无住房但仍存在的Person：128
- 民用永久住宅容量：24,264
- 现役军人兵营容量：1,200，只允许 `population.active_military`
- 非住宅Facility永久住房容量：0；客栈、医馆等临时服务不冒充永久住宅。

## 岗位

- 有效劳力：12,175
- 已就业：11,582
- 未就业：593
- 空缺岗位：22,806
- 技能不匹配空缺：96
- 所有岗位引用稳定Person ID；无工人的设施保持存在，但 `normal_operation=false`，不产生正常产出。

## AI压力事实

`unhoused=128, housing_slots=5050, unemployed=593, vacancies=22806, skill_shortage=96`。

AI不读取固定“住宅/岗位Cell比例”，只依据这些实际压力、粮食与治安事实提出建设、培训或招募建议。
