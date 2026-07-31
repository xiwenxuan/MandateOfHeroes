# M13-P2任务书：兖州黄河下游—济汶泗水—泰沂山前稳定地理骨架

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖兖州刺史部陈留郡、东郡、东平国、任城国、泰山郡、济北国、
山阳郡和济阴郡八个人口来源。完成后，兖州八郡国全部具有临时稳定地理映射。

稳定地理不使用“兖州”行政名称作为永久空间身份，而按河流、湖泽、平原与山前建立：

- `geo.region.central.china.loweryellowjishui`：黄河下游与济水西部平原；
- `geo.region.east.china.wensishuiriverplain`：汶泗水系与鲁西南平原；
- `geo.region.east.china.taiyifoothill`：泰沂山地与西部山前。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 陈留郡 | `geo.region.central.china.loweryellowjishui.southwestplain` | 黄河下游西南部平原地理区 | 黄河下游—济水西部宏区 |
| 东郡 | `geo.region.central.china.loweryellowjishui.northcentralplain` | 黄河下游北中部平原地理区 | 黄河下游—济水西部宏区 |
| 济阴郡 | `geo.region.central.china.loweryellowjishui.southeastplain` | 济水西部与菏泽平原地理区 | 黄河下游—济水西部宏区 |
| 东平国 | `geo.region.east.china.wensishuiriverplain.northplain` | 汶水下游与东平湖平原地理区 | 汶泗水系—鲁西南宏区 |
| 任城国 | `geo.region.east.china.wensishuiriverplain.centralplain` | 汶泗交汇与济宁平原地理区 | 汶泗水系—鲁西南宏区 |
| 山阳郡 | `geo.region.east.china.wensishuiriverplain.westplain` | 巨野泽与鲁西南西部平原地理区 | 汶泗水系—鲁西南宏区 |
| 泰山郡 | `geo.region.east.china.taiyifoothill.centralbasin` | 泰沂中部山地盆地地理区 | 泰沂山地—西部山前宏区 |
| 济北国 | `geo.region.east.china.taiyifoothill.northwestplain` | 泰山西北麓与济水东部平原地理区 | 泰沂山地—西部山前宏区 |

每个行政来源使用一条`single_provisional_commandery_bucket_v1`人口覆盖映射，
权重为10,000基点。本批新增3个宏区、8个`commandery_area`和8条映射。
所有坐标留空，`geometry_status=provisional`且`provisional=true`。

## 三、史料与修正约束

- 八个行政来源继续采用卷三十一的已校录户口记录；
- 泰山郡原文8,929户与修正108,929户分别保存；
- 泰山郡人口437,317口不增加无关修正；
- 户数修正继续使用`suspected_missing_leading_digit`，稳定地理映射不得覆盖人口字段；
- 陈留、濮阳、济北等系列城市和治所不承担整郡国人口事实；
- 古济水、巨野泽、黄河故道的不确定性不以现代河道或省界伪装为精确古界。

## 四、明确不做

- 不绘制精确郡国边界、古河道复原线或未经核验的质心坐标；
- 不把现代河南、山东省界或现行河道当作东汉边界与水系；
- 不拆分郡国人口到陈留、濮阳、济北等游戏城市和县级节点；
- 不填充`game_location_crosswalk.csv`，该工作仍属于P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计54条，包含13个根宏区和41个郡国尺度子区；
2. 映射表累计41条，覆盖41个唯一行政来源；
3. 本批11个稳定ID和8个行政来源无遗漏、重复或孤立引用；
4. 黄河下游—济水西部宏区与汶泗水系—鲁西南宏区各有3个直接子区，
   泰沂山地—西部山前宏区有2个；
5. 兖州八个郡国人口来源全部拥有一条P2临时映射；
6. 泰山郡的原始户数、修正户数、人口原值与修正码保持不变；
7. 每个新增来源的映射权重严格等于10,000基点；
8. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
9. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
   `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计54条，其中13个宏区、41个郡国尺度子区
- 140年郡国映射：累计41条，权重错误0
- 冀州映射覆盖：9/9
- 幽州映射覆盖：11/11
- 司隶映射覆盖：7/7
- 豫州映射覆盖：6/6
- 兖州映射覆盖：8/8
- 游戏地点交叉：0条，保留至P3
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：通过，`regions=54`、`mappings=41`、`crosswalks=0`
- 专项验证测试：通过，30/30
- 全工程编译：通过
- 核心回归：通过，104/104
- Unity测试：未运行；本任务只修改离线CSV、JSON、文档和PowerShell数据测试
- 差异检查：通过
- 下一阶段建议：继续P2第八批，优先建立徐州沂沭泗水—淮海—江淮东部地理骨架。
