# M13-P2任务书：勃海—燕山—辽西走廊第三批稳定映射

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段。在河北连续地理带基础上，补入冀州尚未映射的勃海郡，并向幽州
东部扩展渔阳郡、右北平郡和辽西郡。

勃海郡继续归入河北平原物理宏区。渔阳、右北平和辽西不硬塞入河北宏区，
而是新建`geo.region.north.china.yanshanliaoxi`宏区，表达燕山山前至辽西
走廊的连续地理关系。该宏区是项目稳定索引，不是任何朝代的行政区。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 勃海郡 | `geo.region.north.china.hebei.eastcoastalplain` | 冀东滨海平原地理区 | 河北宏区 |
| 渔阳郡 | `geo.region.north.china.yanshanliaoxi.southwestplain` | 燕山西南麓平原地理区 | 燕山—辽西宏区 |
| 右北平郡 | `geo.region.north.china.yanshanliaoxi.centralfoothillplain` | 燕山中东段山前平原地理区 | 燕山—辽西宏区 |
| 辽西郡 | `geo.region.north.china.yanshanliaoxi.northeastcorridor` | 辽西走廊地理区 | 燕山—辽西宏区 |

每个行政来源使用一条
`single_provisional_commandery_bucket_v1`人口覆盖映射，权重为10,000基点。
本批共新增1个宏区、4个`commandery_area`和4条映射。所有坐标留空，
`geometry_status=provisional`且`provisional=true`。

## 三、设计约束

- `admin.han140.jizhou.bohai`保留史料录入的“勃海郡”行政身份；
- 稳定地理显示可使用现代“渤海西岸”作近似定位，但不得反向改名行政ID；
- 蓟、土垠、南皮等治所或游戏节点不承担郡级人口事实；
- 燕山—辽西宏区只表达物理连续性，不表示三个郡拥有相同边界或地貌；
- 后续县级拆分可以把一个行政来源映射到多个稳定子区，但权重仍须合计10,000。

## 四、明确不做

- 不绘制精确古郡界、县界或海岸线，不填写未经核验的质心坐标；
- 不把现代北京、天津、河北或辽宁边界当作东汉边界；
- 不拆分郡级人口到蓟、土垠、南皮或其他县级节点；
- 不填写`game_location_crosswalk.csv`，该工作仍属于P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称幽州或P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计16条，包含2个根宏区和14个郡国尺度子区；
2. 映射表累计14条，覆盖14个唯一行政来源；
3. 本批5个稳定ID和4个行政来源无遗漏、重复或孤立引用；
4. 燕山—辽西宏区有3个直接子区，勃海子区仍归河北宏区；
5. 冀州九个郡国人口来源全部拥有一条P2临时映射；
6. 每个新增来源权重严格等于10,000基点；
7. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
8. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
   `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计16条，其中2个宏区、14个郡国尺度子区
- 140年郡国映射：累计14条，覆盖14个行政来源，权重错误0
- 冀州映射覆盖：9/9
- 游戏地点交叉：0条，保留至P3
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：`RESULT han140-validation=passed sources=3 admin=119 population=105 regions=16 mappings=14 crosswalks=0`
- 专项验证测试：`RESULT han140-tests passed=26 failed=0`
- 全工程编译：通过
- 核心回归：`RESULT passed=104 failed=0`
- Unity测试：未运行；本任务只修改离线CSV、JSON、文档和PowerShell数据测试
- 差异检查：通过
- 下一阶段建议：继续P2第四批，完成幽州北部与东北剩余人口来源的临时映射。
