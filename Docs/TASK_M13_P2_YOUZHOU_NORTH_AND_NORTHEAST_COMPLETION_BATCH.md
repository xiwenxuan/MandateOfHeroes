# M13-P2任务书：幽州北部与东北稳定地理收口

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖幽州尚未映射的代郡、上谷郡、辽东郡、玄菟郡、乐浪郡和
辽东属国。完成后，140年幽州十一郡、属国人口来源全部具有临时稳定地理映射。

本批建立的是人口守恒与跨年代身份底座，不复原精确边界。为避免用“幽州”
行政名称承担永久地理身份，新增两个物理宏区：

- `geo.region.north.china.yanbeigreatwall`：燕北山地与长城走廊；
- `geo.region.northeast.asia.liaodongkoreanorth`：辽东与朝鲜半岛北部。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 代郡 | `geo.region.north.china.yanbeigreatwall.westernbasin` | 燕北西部盆地地理区 | 燕北—长城宏区 |
| 上谷郡 | `geo.region.north.china.yanbeigreatwall.easternmountainbasin` | 燕北东部山间盆地地理区 | 燕北—长城宏区 |
| 辽东郡 | `geo.region.northeast.asia.liaodongkoreanorth.liaoheriverplain` | 辽河下游与辽东平原地理区 | 辽东—半岛北部宏区 |
| 玄菟郡 | `geo.region.northeast.asia.liaodongkoreanorth.easternmountainfrontier` | 辽东东部山地边缘地理区 | 辽东—半岛北部宏区 |
| 乐浪郡 | `geo.region.northeast.asia.liaodongkoreanorth.koreanorthwestplain` | 朝鲜半岛西北部平原地理区 | 辽东—半岛北部宏区 |
| 辽东属国 | `geo.region.northeast.asia.liaodongkoreanorth.westernfrontier` | 辽东西部边缘地理区 | 辽东—半岛北部宏区 |

每个行政来源使用一条
`single_provisional_commandery_bucket_v1`人口覆盖映射，权重为10,000基点。
本批新增2个宏区、6个`commandery_area`和6条映射。所有坐标留空，
`geometry_status=provisional`且`provisional=true`。

## 三、缺项与不确定性

- 辽东郡和玄菟郡继续分别保留原文值与现代校录修正值；
- 辽东属国原文户数、口数继续保持空值，不得写成0；
- 辽东属国映射只承接现有`M`级有效估算，不把估算冒充史籍原文；
- 玄菟郡治迁徙、乐浪郡县界和辽东属国六城位置均不在本批裁定；
- 跨现代国界的宏区只表示历史空间索引，不表达现代主权或边界结论。

## 四、明确不做

- 不绘制精确郡界、县界、长城线、海岸线或未经核验的质心坐标；
- 不把现代省界、国界或城市边界当作东汉边界；
- 不拆分郡级人口到襄平、乐浪或其他城市与县级节点；
- 不填写`game_location_crosswalk.csv`，该工作仍属于P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计24条，包含4个根宏区和20个郡国尺度子区；
2. 映射表累计20条，覆盖20个唯一行政来源；
3. 本批8个稳定ID和6个行政来源无遗漏、重复或孤立引用；
4. 燕北—长城宏区有2个直接子区，辽东—半岛北部宏区有4个直接子区；
5. 幽州十一郡、属国人口来源全部拥有一条P2临时映射；
6. 辽东属国原文户口仍为空、证据等级仍为`M`；
7. 每个新增来源权重严格等于10,000基点；
8. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
9. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
   `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计24条，其中4个宏区、20个郡国尺度子区
- 140年郡国映射：累计20条，覆盖20个行政来源，权重错误0
- 冀州映射覆盖：9/9
- 幽州映射覆盖：11/11
- 游戏地点交叉：0条，保留至P3
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：`RESULT han140-validation=passed sources=3 admin=119 population=105 regions=24 mappings=20 crosswalks=0`
- 专项验证测试：`RESULT han140-tests passed=27 failed=0`
- 全工程编译：通过
- 核心回归：`RESULT passed=104 failed=0`
- Unity测试：未运行；本任务只修改离线CSV、JSON、文档和PowerShell数据测试
- 差异检查：通过
- 下一阶段建议：继续P2第五批，优先建立司隶河洛与关中稳定地理骨架。
