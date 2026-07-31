# M13-P2任务书：河北连续地理带第二批稳定映射

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段。在首批原型走廊五个郡国映射之外，沿用
`geo.region.north.china.hebei`宏区，向相邻区域扩展广阳郡、河间国、常山国、
清河国和赵国的稳定地理子区。

本批目的是形成更连续的河北人口地理带，为后续道路、迁徙、地方战争和县级
交叉提供不会随行政沿革失效的身份入口；它不宣称已经复原精确古郡界。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 |
|---|---|---|
| 广阳郡 | `geo.region.north.china.hebei.northcentralplain` | 冀北中部平原地理区 |
| 河间国 | `geo.region.north.china.hebei.centraleastplain` | 冀中东部平原地理区 |
| 常山国 | `geo.region.north.china.hebei.centralfoothill` | 太行山东麓中段地理区 |
| 清河国 | `geo.region.north.china.hebei.southeastplain` | 冀东南平原地理区 |
| 赵国 | `geo.region.north.china.hebei.southwesttaihangplain` | 太行山东麓南段平原地理区 |

每个行政来源使用一条
`single_provisional_commandery_bucket_v1`人口覆盖映射，权重为10,000基点。
所有新增子区以河北宏区为父级，坐标留空，`geometry_status=provisional`且
`provisional=true`。

## 三、明确不做

- 不绘制或推断精确古郡界、县界和质心坐标；
- 不把现代省市边界当作东汉行政边界；
- 不拆分郡国人口到治所、县或运行时地点；
- 不填写`game_location_crosswalk.csv`，该工作仍属于P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 四、验收标准

1. 稳定地理表累计11条记录，其中河北宏区有10个直接子区；
2. 映射表累计10条记录，覆盖10个唯一行政来源；
3. 本批五个稳定ID和五个行政来源无遗漏、重复或孤立引用；
4. 每个新增来源的映射权重严格等于10,000基点；
5. 新增几何与映射全部标为临时，坐标全部留空；
6. 游戏地点交叉表仍为空；
7. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
   `git diff --check`通过。

## 五、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计11条，其中1个宏区、10个郡国尺度子区
- 140年郡国映射：累计10条，覆盖10个行政来源，权重错误0
- 游戏地点交叉：0条，保留至P3
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：`RESULT han140-validation=passed sources=3 admin=119 population=105 regions=11 mappings=10 crosswalks=0`
- 专项验证测试：`RESULT han140-tests passed=25 failed=0`
- 全工程编译：通过
- 核心回归：`RESULT passed=104 failed=0`
- Unity测试：未运行；本任务只修改离线CSV、JSON、文档和PowerShell数据测试
- 差异检查：通过
- 下一阶段建议：继续P2第三批，优先扩展勃海郡与幽州东部走廊，连接渔阳、
  右北平和辽西等区域。
