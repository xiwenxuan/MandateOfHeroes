# M13-P2任务书：司隶河洛—河东—关中稳定地理骨架

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖司隶校尉部河南尹、河内郡、河东郡、弘农郡、京兆尹、左冯翊
和右扶风七个人口来源。完成后，司隶七个郡、尹全部具有临时稳定地理映射。

稳定地理不使用“司隶”行政名称作为永久空间身份，而按物理格局建立三个宏区：

- `geo.region.central.china.heluo`：河洛与黄河北岸；
- `geo.region.north.china.southfenheyellowriver`：汾河南段与黄河东岸；
- `geo.region.northwest.china.guanzhong`：关中渭河平原。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 河南尹 | `geo.region.central.china.heluo.luoyangbasin` | 洛阳盆地地理区 | 河洛宏区 |
| 河内郡 | `geo.region.central.china.heluo.northyellowriverplain` | 黄河北岸河内平原地理区 | 河洛宏区 |
| 弘农郡 | `geo.region.central.china.heluo.westyellowrivercorridor` | 河洛西部黄河走廊地理区 | 河洛宏区 |
| 河东郡 | `geo.region.north.china.southfenheyellowriver.centralbasin` | 汾河南段与运城盆地地理区 | 汾河—黄河宏区 |
| 京兆尹 | `geo.region.northwest.china.guanzhong.centralweiriverplain` | 渭河中部平原地理区 | 关中宏区 |
| 左冯翊 | `geo.region.northwest.china.guanzhong.easternweiriverplain` | 渭河东部平原地理区 | 关中宏区 |
| 右扶风 | `geo.region.northwest.china.guanzhong.westernweiriverplain` | 渭河西部平原地理区 | 关中宏区 |

每个行政来源使用一条
`single_provisional_commandery_bucket_v1`人口覆盖映射，权重为10,000基点。
本批新增3个宏区、7个`commandery_area`和7条映射。所有坐标留空，
`geometry_status=provisional`且`provisional=true`。

## 三、史料与异录约束

- 河南尹、弘农郡继续采用卷二十九公开原典转录人口；
- 现代ODS中的两处不同数字只作异录说明，不建立修正值；
- 河南尹、京兆尹、左冯翊和右扶风继续保持京畿、三辅行政类型，不改成普通郡；
- 洛阳、长安、怀、安邑等城市或治所不承担整个行政来源的人口事实；
- 函谷关、潼关、陈仓等关隘和节点留至P3县级、游戏地点交叉。

## 四、明确不做

- 不绘制精确郡界、京畿界、三辅界、关隘线或未经核验的质心坐标；
- 不把现代省市边界当作东汉边界；
- 不拆分郡、尹人口到洛阳、长安或其他城市与县级节点；
- 不填写`game_location_crosswalk.csv`，该工作仍属于P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计34条，包含7个根宏区和27个郡国尺度子区；
2. 映射表累计27条，覆盖27个唯一行政来源；
3. 本批10个稳定ID和7个行政来源无遗漏、重复或孤立引用；
4. 河洛宏区有3个直接子区，汾河—黄河宏区有1个，关中宏区有3个；
5. 司隶七个郡、尹人口来源全部拥有一条P2临时映射；
6. 河南尹原文口数仍为1,010,827，弘农郡仍为199,113，均无修正口数；
7. 每个新增来源权重严格等于10,000基点；
8. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
9. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
   `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计34条，其中7个宏区、27个郡国尺度子区
- 140年郡国映射：累计27条，权重错误0
- 冀州映射覆盖：9/9
- 幽州映射覆盖：11/11
- 司隶映射覆盖：7/7
- 游戏地点交叉：0条，保留至P3
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：通过，`regions=34`、`mappings=27`、`crosswalks=0`
- 专项验证测试：通过，28/28
- 全工程编译：通过
- 核心回归：通过，104/104
- Unity测试：计划不运行；本任务只修改离线CSV、JSON、文档和PowerShell数据测试
- 差异检查：通过
- 下一阶段建议：继续P2第六批，优先建立豫州中原东南与淮颍地理骨架。
