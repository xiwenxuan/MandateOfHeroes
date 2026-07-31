# M13-P2任务书：青州黄河济水—淄潍胶莱—胶东半岛稳定地理骨架

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖青州刺史部济南国、平原郡、乐安国、北海国、东莱郡和齐国
六个人口来源。完成后，青州六郡国全部具有临时稳定地理映射。

稳定地理不使用“青州”或郡国名称作为永久空间身份，而按河流、平原、山前和
半岛建立：

- `geo.region.east.china.loweryellowjieastplain`：黄河下游与济水东部平原；
- `geo.region.east.china.ziweijiaolaiplain`：淄潍水系与胶莱西部平原；
- `geo.region.east.china.jiaodongpeninsula`：胶东半岛与北部海岸。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 平原郡 | `geo.region.east.china.loweryellowjieastplain.northwestplain` | 黄河下游西北部平原地理区 | 黄河下游—济水东部宏区 |
| 济南国 | `geo.region.east.china.loweryellowjieastplain.southwestfoothillplain` | 济水南岸与泰山北麓平原地理区 | 黄河下游—济水东部宏区 |
| 乐安国 | `geo.region.east.china.loweryellowjieastplain.northeastcoastalplain` | 济水下游东北部与滨海平原地理区 | 黄河下游—济水东部宏区 |
| 齐国 | `geo.region.east.china.ziweijiaolaiplain.westernfoothillplain` | 淄水中游与鲁山北麓地理区 | 淄潍水系—胶莱西部宏区 |
| 北海国 | `geo.region.east.china.ziweijiaolaiplain.easternplain` | 潍水流域与胶莱西部平原地理区 | 淄潍水系—胶莱西部宏区 |
| 东莱郡 | `geo.region.east.china.jiaodongpeninsula.northcoastalhills` | 胶莱东部与胶东北岸丘陵地理区 | 胶东半岛—北部海岸宏区 |

每个行政来源使用一条`single_provisional_commandery_bucket_v1`人口覆盖映射，
权重为10,000基点。本批新增3个宏区、6个`commandery_area`和6条映射。
所有坐标留空，`geometry_status=provisional`且`provisional=true`。

## 三、史料、异文与地理约束

- 六个行政来源继续采用卷三十二的已校录户口记录；
- 六项均无人口数值补正，不得因稳定地理映射生成修正值；
- 济南条140年继续按`kingdom`保存，行政置信度保持`medium`；
- “济南郡”继续只作为公开转录异文写入行政备注；
- 平原、济南、北海等游戏城市目录节点不承担整郡国人口事实；
- 古黄河、济水、胶莱水系与海岸线变迁不以现代河道、省界或精确坐标伪装。

## 四、明确不做

- 不绘制精确郡国边界、古河道、海岸线或未经核验的质心坐标；
- 不把现代山东省界或现行黄河河道当作东汉边界与水系；
- 不拆分人口到平原、济南、北海、临淄、掖县等城市和县级节点；
- 不填充`game_location_crosswalk.csv`，该工作仍属于P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计71条，包含19个根宏区和52个郡国尺度子区；
2. 映射表累计52条，覆盖52个唯一行政来源；
3. 本批9个稳定ID和6个行政来源无遗漏、重复或孤立引用；
4. 黄河下游—济水东部宏区有3个直接子区，淄潍—胶莱西部宏区有2个，
   胶东半岛—北部海岸宏区有1个；
5. 青州六个郡国人口来源全部拥有一条P2临时映射；
6. 济南国类型、置信度和“济南郡”转录异文保持可审计；
7. 青州六项人口仍无数值补正；
8. 每个新增来源的映射权重严格等于10,000基点；
9. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
10. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
    `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计71条，其中19个宏区、52个郡国尺度子区
- 140年郡国映射：累计52条，权重错误0
- 冀州映射覆盖：9/9
- 幽州映射覆盖：11/11
- 司隶映射覆盖：7/7
- 豫州映射覆盖：6/6
- 兖州映射覆盖：8/8
- 徐州映射覆盖：5/5
- 青州映射覆盖：6/6
- 游戏地点交叉：0条，保留至P3
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：通过，`regions=71`、`mappings=52`、`crosswalks=0`
- 专项验证测试：通过，32/32
- 全工程编译：通过
- 核心回归：通过，104/104
- Unity测试：未运行；本任务只修改离线CSV、JSON、文档和PowerShell数据测试
- 差异检查：通过
- 下一阶段建议：继续P2第十批，优先建立荆州汉水—江汉平原—洞庭湖地理骨架。
