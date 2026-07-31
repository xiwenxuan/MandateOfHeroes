# M13-P2任务书：豫州中原东南—淮颍—泗沂稳定地理骨架

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖豫州刺史部颍川郡、汝南郡、梁国、沛国、陈国和鲁国六个人口来源。
完成后，豫州六郡国全部具有临时稳定地理映射。

稳定地理不使用“豫州”行政名称作为永久空间身份，而按平原、河流和山前过渡建立：

- `geo.region.central.china.yingruhuai`：颍汝与淮河北岸；
- `geo.region.central.china.suihuainorth`：睢水与淮北北部平原；
- `geo.region.east.china.siyifoothill`：泗沂西缘与山前。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 颍川郡 | `geo.region.central.china.yingruhuai.northwestplain` | 颍水上游与许昌平原地理区 | 颍汝—淮河北岸宏区 |
| 陈国 | `geo.region.central.china.yingruhuai.centralplain` | 颍水中下游平原地理区 | 颍汝—淮河北岸宏区 |
| 汝南郡 | `geo.region.central.china.yingruhuai.southplain` | 汝水下游与淮河北岸平原地理区 | 颍汝—淮河北岸宏区 |
| 梁国 | `geo.region.central.china.suihuainorth.northwestplain` | 睢水上中游平原地理区 | 睢水—淮北北部宏区 |
| 沛国 | `geo.region.central.china.suihuainorth.southeastplain` | 淮北北部与泗水西岸平原地理区 | 睢水—淮北北部宏区 |
| 鲁国 | `geo.region.east.china.siyifoothill.westernplain` | 泗水上游西部山前平原地理区 | 泗沂西缘—山前宏区 |

每个行政来源使用一条`single_provisional_commandery_bucket_v1`人口覆盖映射，
权重为10,000基点。本批新增3个宏区、6个`commandery_area`和6条映射。
所有坐标留空，`geometry_status=provisional`且`provisional=true`。

## 三、史料与修正约束

- 六个行政来源继续采用卷三十的已校录户口记录；
- 沛国原文251,393口与修正1,251,393口分别保存；
- 陈国原文1,547,572口与修正547,572口分别保存；
- 两项修正继续使用`suspected_transposed_million_digit`，稳定地理映射不得覆盖人口字段；
- 许县、平舆、睢阳、谯、沛、陈县和鲁县等城市或治所不承担整郡国人口事实；
- 历史州郡边界、现代省界、系列游戏城市节点和本批物理地理桶保持分层。

## 四、明确不做

- 不绘制精确郡国边界、河道复原线或未经核验的质心坐标；
- 不把现代河南、安徽、江苏、山东省界当作东汉边界；
- 不拆分郡国人口到许昌、汝南、陈、谯、小沛等游戏城市和县级节点；
- 不填充`game_location_crosswalk.csv`，该工作仍属于P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计43条，包含10个根宏区和33个郡国尺度子区；
2. 映射表累计33条，覆盖33个唯一行政来源；
3. 本批9个稳定ID和6个行政来源无遗漏、重复或孤立引用；
4. 颍汝—淮河北岸宏区有3个直接子区，睢水—淮北北部宏区有2个，
   泗沂西缘—山前宏区有1个；
5. 豫州六个郡国人口来源全部拥有一条P2临时映射；
6. 沛国、陈国的原值、修正值和配对修正码保持不变；
7. 每个新增来源的映射权重严格等于10,000基点；
8. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
9. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
   `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计43条，其中10个宏区、33个郡国尺度子区
- 140年郡国映射：累计33条，权重错误0
- 冀州映射覆盖：9/9
- 幽州映射覆盖：11/11
- 司隶映射覆盖：7/7
- 豫州映射覆盖：6/6
- 游戏地点交叉：0条，保留至P3
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：通过，`regions=43`、`mappings=33`、`crosswalks=0`
- 专项验证测试：通过，29/29
- 全工程编译：通过
- 核心回归：通过，104/104
- Unity测试：未运行；本任务只修改离线CSV、JSON、文档和PowerShell数据测试
- 差异检查：通过
- 下一阶段建议：继续P2第七批，优先建立兖州黄河下游—济水—泰沂地理骨架。
