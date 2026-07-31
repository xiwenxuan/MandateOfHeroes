# M13-P2任务书：益州汉中—四川盆地—川西山地—云贵高原—横断山南缘稳定地理骨架

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖益州刺史部汉中、巴、广汉、蜀、犍为、牂牁、越巂、益州、
永昌九郡及广汉、蜀郡、犍为三个属国。完成后，益州十二项人口来源全部具有
临时稳定地理映射。

本批按盆地、河谷、山地、高原和纵谷建立稳定地理身份：

- `geo.region.southwest.china.hanzhongqinba`：汉中盆地与秦巴走廊；
- `geo.region.southwest.china.sichuanbasin`：四川盆地与盆周河谷；
- `geo.region.southwest.china.westernsichuanmountains`：川西山地与高原过渡走廊；
- `geo.region.southwest.china.yunguiplateau`：云贵高原与山间盆地；
- `geo.region.southwest.china.hengduansouth`：横断山南缘与滇西纵谷。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 汉中郡 | `geo.region.southwest.china.hanzhongqinba.centralhanriverbasin` | 汉水上游与汉中盆地地理区 | 汉中盆地与秦巴走廊宏区 |
| 广汉郡 | `geo.region.southwest.china.sichuanbasin.northwestchengduplain` | 成都平原北部与沱江上游地理区 | 四川盆地与盆周河谷宏区 |
| 蜀郡 | `geo.region.southwest.china.sichuanbasin.centralchengduplain` | 成都平原中西部与岷江地理区 | 四川盆地与盆周河谷宏区 |
| 巴郡 | `geo.region.southwest.china.sichuanbasin.easternfoldbasin` | 四川盆地东部与长江嘉陵江河谷地理区 | 四川盆地与盆周河谷宏区 |
| 犍为郡 | `geo.region.southwest.china.sichuanbasin.southernriverhills` | 四川盆地南部与岷江下游丘陵地理区 | 四川盆地与盆周河谷宏区 |
| 广汉属国 | `geo.region.southwest.china.westernsichuanmountains.northqiangcorridor` | 岷山南麓与川西北山地走廊地理区 | 川西山地与高原过渡走廊宏区 |
| 蜀郡属国 | `geo.region.southwest.china.westernsichuanmountains.centralplateaucorridor` | 川西高原东缘与大渡河上游走廊地理区 | 川西山地与高原过渡走廊宏区 |
| 犍为属国 | `geo.region.southwest.china.westernsichuanmountains.southmountaincorridor` | 川西南山地与金沙江北缘走廊地理区 | 川西山地与高原过渡走廊宏区 |
| 牂牁郡 | `geo.region.southwest.china.yunguiplateau.northeastkarstplateau` | 黔中高原与乌江南盘江上游地理区 | 云贵高原与山间盆地宏区 |
| 益州郡 | `geo.region.southwest.china.yunguiplateau.centralyunnanbasin` | 滇中高原与山间盆地地理区 | 云贵高原与山间盆地宏区 |
| 越巂郡 | `geo.region.southwest.china.hengduansouth.northeastanningvalley` | 安宁河谷与川西南山地地理区 | 横断山南缘与滇西纵谷宏区 |
| 永昌郡 | `geo.region.southwest.china.hengduansouth.southwestlancangfrontier` | 澜沧江上游与滇西纵谷地理区 | 横断山南缘与滇西纵谷宏区 |

每个行政来源使用一条`single_provisional_commandery_bucket_v1`人口覆盖映射，
权重为10,000基点。本批新增5个宏区、12个`commandery_area`和12条映射。
所有坐标留空，`geometry_status=provisional`且`provisional=true`。

## 三、史料、异文、地理与人口约束

- 十二个行政来源继续使用P1校录的《后汉书》卷三十三户口记录；
- 十二项原始合计为1,525,257户、7,242,028口，且没有显式人口修正；
- 九个郡继续按`commandery`保存，三个属国继续按`other`保存；
- 永昌郡231,897户、1,897,344口保留为原始疑值，不推测替代人口；
- 广汉属国旧注“属蜀郡”的校勘问题只保留在行政备注，不转化为人口修正；
- 四川盆地、川西高原、云贵高原与横断山纵谷的现代地貌关系只用于临时索引，
  不表示东汉边疆、族群分布、属国都尉辖区或道路已经精确复原。

## 四、明确不做

- 不绘制精确郡界、属国界、县道边界、古河道、山路或未校验质心坐标；
- 不把现代陕西、四川、重庆、贵州、云南或国境线当作东汉边界；
- 不拆分人口到南郑、成都、江州、鱼复、邛都、滇池、不韦等城市和县级节点；
- 不把三个属国合并到邻近郡，也不按现代族群重新分配人口；
- 不填入`game_location_crosswalk.csv`，该工作保留至P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计109条，包含32个根宏区和77个郡国尺度子区；
2. 映射表累计77条，覆盖77个唯一行政来源；
3. 本批17个稳定ID和12个行政来源无遗漏、重复或孤立引用；
4. 五个宏区的直接子区数依次为1、4、3、2、2；
5. 益州九郡、三属国人口来源全部拥有一条P2临时映射；
6. 十二项仍恰好保留1,525,257户、7,242,028口，并且不产生人口修正；
7. 永昌原始疑值、九郡三属国类型和广汉属国旧注异文保持可审计；
8. 每个新增来源的映射权重严格等于10,000基点；
9. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
10. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
    `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计109条，其中32个根宏区、77个郡国尺度子区；
- 人口映射：累计77条、错误0条；益州九郡三属国覆盖12/12；
- 游戏地点交叉表：仍为0条，未提前进入P3；
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：通过（稳定地理109、映射77、交叉表0）；
- 专项回归测试：35/35通过；
- 全工程编译：通过；
- 核心回归测试：104/104通过；
- Unity测试：未运行；本批仅修改离线CSV、JSON审计产物、文档和
  PowerShell校验，不涉及Unity运行时、场景或序列化；
- `git diff --check`：通过；
- 下一阶段建议：继续P2第十三批，优先建立凉州陇右—河湟—河西走廊—
  居延边地稳定地理骨架。
