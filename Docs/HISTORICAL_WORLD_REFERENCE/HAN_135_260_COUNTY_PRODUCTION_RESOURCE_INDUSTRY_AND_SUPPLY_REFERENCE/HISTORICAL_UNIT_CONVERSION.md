# 历史单位与规范单位 V1

## 原则

母版采用规范单位：质量用 `kg`，粮食等价用 `kg_food_equivalent`，牲畜用 `head`，织物用 `bolt`，器具用 `piece`，车船设备用 `unit`，距离用 `km`，面积用 `ha`/`sq_km`，运输能力用 `tonne_km`。

汉代斛、石、斗、升、斤、两、匹等单位随时代、地点和物类变化，不能写死成唯一换算常数。史料原值必须保留 `original_value`、`original_unit`、时间、地点、来源和解释；进入模型时另写 `normalized_low`、`normalized_recommended`、`normalized_high`、`conversion_method_id` 与置信度。

V1 没有把不确定历史单位直接灌入县级产量。当前数字来自显式人口、土地、区域单产、种耗、收获/加工/仓储损耗和加工能力模型。后续史料单位校准只能修改公开参数或县级 ChangePoint，禁止使用隐藏全国倍率把结果强行平衡。
