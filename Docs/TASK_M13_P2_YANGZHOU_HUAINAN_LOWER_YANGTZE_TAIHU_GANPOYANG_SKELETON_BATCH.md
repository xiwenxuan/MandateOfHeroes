# M13-P2任务书：扬州淮南—长江下游—太湖—钱塘浙东—赣鄱稳定地理骨架

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖扬州刺史部九江郡、庐江郡、丹阳郡、吴郡、会稽郡和豫章郡
六个人口来源。完成后，扬州六郡全部具有临时稳定地理映射。

本批按平原、河流、湖泊、盆地和丘陵建立稳定地理身份，不用“扬州”或郡名
替代物理边界：

- `geo.region.east.china.huainanyangtzenorth`：淮南与长江北岸；
- `geo.region.east.china.loweryangtzetaihu`：长江下游与太湖水网；
- `geo.region.southeast.china.qiantangzhejianghills`：钱塘江与浙东丘陵；
- `geo.region.southeast.china.ganpoyang`：赣江与鄱阳湖盆地。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 九江郡 | `geo.region.east.china.huainanyangtzenorth.northcentralplain` | 淮河以南与巢湖以北平原地理区 | 淮南与长江北岸宏区 |
| 庐江郡 | `geo.region.east.china.huainanyangtzenorth.southwestfoothillriver` | 大别山东南麓与皖江西部地理区 | 淮南与长江北岸宏区 |
| 丹阳郡 | `geo.region.east.china.loweryangtzetaihu.westernriverhills` | 皖南丘陵与长江下游西段地理区 | 长江下游与太湖水网宏区 |
| 吴郡 | `geo.region.east.china.loweryangtzetaihu.easterntaihuplain` | 太湖平原与长江下游东段地理区 | 长江下游与太湖水网宏区 |
| 会稽郡 | `geo.region.southeast.china.qiantangzhejianghills.eastcoastalriverhills` | 钱塘江南岸与浙东沿海丘陵地理区 | 钱塘江与浙东丘陵宏区 |
| 豫章郡 | `geo.region.southeast.china.ganpoyang.centralriverlakebasin` | 赣江中下游与鄱阳湖盆地地理区 | 赣江与鄱阳湖盆地宏区 |

每个行政来源使用一条`single_provisional_commandery_bucket_v1`人口覆盖映射，
权重为10,000基点。本批新增4个宏区、6个`commandery_area`和6条映射。
所有坐标留空，`geometry_status=provisional`且`provisional=true`。

## 三、史料、地理与人口约束

- 六个行政来源继续使用P1已经校录的《后汉书》卷三十二户口记录；
- 六郡原始合计为1,021,096户、4,338,538口，且没有显式人口修正；
- 淮南、长江下游、太湖平原、钱塘江与浙东丘陵、赣江与鄱阳湖分别作为
  相邻但不等同的物理系统，不能直接套用一张现代省界图；
- 太湖与长江三角洲、鄱阳湖与赣江水系的历史岸线和河网持续变化，本批不
  伪造古湖岸、古河道或精确质心；
- 会稽郡140年范围远大于单一会稽城市节点，豫章郡人口也不能只归南昌或
  柴桑城市节点。

## 四、明确不做

- 不绘制精确郡界、县界、古河道、古湖岸、海岸线或未校验质心坐标；
- 不把现代安徽、江苏、浙江、江西、上海边界当作东汉行政或自然地理边界；
- 不拆分人口到寿春、合肥、宛陵、秣陵、吴、会稽、南昌、柴桑、鄱阳等节点；
- 不填入`game_location_crosswalk.csv`，该工作保留至P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计92条，包含27个根宏区和65个郡国尺度子区；
2. 映射表累计65条，覆盖65个唯一行政来源；
3. 本批10个稳定ID和6个行政来源无遗漏、重复或孤立引用；
4. 淮南—长江北岸与长江下游—太湖宏区各有2个直接子区，钱塘浙东和
   赣鄱宏区各有1个直接子区；
5. 扬州六郡人口来源全部拥有一条P2临时映射；
6. 六郡仍恰好保留1,021,096户、4,338,538口，并且不产生人口修正；
7. 每个新增来源的映射权重严格等于10,000基点；
8. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
9. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
   `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计92条，其中27个宏区、65个郡国尺度子区
- 140年郡国映射：累计65条，权重错误0
- 扬州映射覆盖：6/6
- 游戏地点交叉：0条，保留至P3
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：通过；`regions=92`、`mappings=65`、`crosswalks=0`
- 专项验证测试：通过；34/34
- 全工程编译：通过
- 核心回归：通过；104/104
- Unity测试：未运行；本任务只修改离线CSV、JSON、文档和PowerShell数据测试
- 差异检查：通过
- 下一阶段建议：继续P2第十二批，优先建立益州汉中—四川盆地—云贵高原—
  横断山南缘稳定地理骨架。
