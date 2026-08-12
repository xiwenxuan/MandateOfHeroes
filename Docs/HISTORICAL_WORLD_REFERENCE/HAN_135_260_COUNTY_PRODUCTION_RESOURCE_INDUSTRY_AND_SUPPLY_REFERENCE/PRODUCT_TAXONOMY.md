# 商品分类与开放内容 ID

V1 平衡层覆盖：谷物、其他食物折算、盐、燃料、木材、马匹、其他牲畜、皮革、纤维、生丝、织物、铁矿石、铁料、工具、兵器军需、建筑材料、陶器、药材和运输装备。

这些是统计类别，不是固定 `enum`。实际内容使用稳定 ID，例如 `product.wheat_grain`、`product.material.iron`。尚无正式运行时定义的内容使用 `product.reference.*` 并标记 `REFERENCE_MAPPING_REQUIRED`；不得静默映射成其他物品。

作物定义同样数据驱动：`crop.wheat` 已有运行时映射，粟、黍、稻、豆、麻、蔬菜和果树暂为参考 ID。地方品种、种子批次、品质、加工级产品和 MOD 产品应通过数据定义扩展。

统计类别只用于全国/县域平衡汇总。Facility 配方必须引用具体产品 ID，不能直接生产抽象的 `OTHER_FOOD` 或 `BUILDING_MATERIAL`。
