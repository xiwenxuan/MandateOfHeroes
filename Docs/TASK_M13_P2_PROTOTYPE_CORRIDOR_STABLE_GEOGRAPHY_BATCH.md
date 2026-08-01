# M13-P2任务书：原型走廊首批稳定地理与郡国映射

## 一、任务定位

本任务开始执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2稳定地理阶段。首批范围由现有六个运行时地点反推其涉及的五个140年人口来源：

| 运行时地点 | 140年人口来源 | 本批处理 |
|---|---|---|
| `location.zhuo` | 涿郡 | 建立郡国尺度稳定地理桶 |
| `location.zhongshan` | 中山国 | 建立郡国尺度稳定地理桶 |
| `location.anping` | 安平国 | 建立郡国尺度稳定地理桶 |
| `location.xiaquyang` | 钜鹿郡 | 与广宗共用一个郡级人口来源 |
| `location.guangzong` | 钜鹿郡 | 与下曲阳共用一个郡级人口来源 |
| `location.ye` | 魏郡 | 建立郡国尺度稳定地理桶 |

本批只建立稳定地理层和140年郡国人口映射，不写
`game_location_crosswalk.csv`。运行时地点、`L###`与`C###`的正式交叉属于P3。

## 二、目标与交付

1. 建立1个河北宏区父节点和5个`commandery_area`稳定子区；
2. 稳定ID采用物理方位描述，不直接把140年行政名称作为身份；
3. 涿郡、中山国、安平国、钜鹿郡、魏郡各建立一条人口映射；
4. 每个行政来源映射权重均为10,000基点；
5. 所有几何与映射均标记`provisional=true`，不填未经核验的质心坐标；
6. 明确单桶映射只是P2首批守恒基线，不表示古代郡界已经精确复原；
7. 更新数据字典、M13总任务和确定性审计测试。

## 三、稳定ID与分层

```text
geo.region.north.china.hebei
├─ northwestplain
├─ centralwestplain
├─ centralsoutheastplain
├─ southcentralplain
└─ southwestzhangheplain
```

| 稳定地理ID后缀 | 项目显示名 | 140年来源 |
|---|---|---|
| `northwestplain` | 冀北西部平原地理区 | 涿郡 |
| `centralwestplain` | 冀中西部平原地理区 | 中山国 |
| `centralsoutheastplain` | 冀中东南平原地理区 | 安平国 |
| `southcentralplain` | 冀南中部平原地理区 | 钜鹿郡 |
| `southwestzhangheplain` | 漳河下游西部平原地理区 | 魏郡 |

这些ID表示项目内稳定分区身份。后续县级校录可以把一个郡国拆到多个稳定区，
但不得通过重命名现有ID破坏引用；拆分后同一行政来源的全部权重仍须合计10,000。

## 四、映射口径

本批使用`single_provisional_commandery_bucket_v1`方法。它的含义是：

- 在县级证据和边界权重尚未完成时，先把一个郡国的全部有效人口放入一个
  郡国尺度临时稳定桶；
- `weight_basis_points=10000`表示该来源当前没有未分配人口；
- 它不表示所有人口集中在治所，也不表示稳定区与历史郡界精确重合；
- 钜鹿郡只出现一条10,000基点映射，不能因下曲阳、广宗两个运行时地点而
  重复分配两次人口。

## 五、明确不做

- 不填写未经核验的经纬度或精确多边形；
- 不把现代行政区边界当作东汉郡界；
- 不把郡国人口直接写入运行时地点；
- 不拆分钜鹿郡人口给下曲阳、广宗；
- 不写6个`location.*`、`L###`或`C###`交叉表；
- 不修改Unity场景、运行时地点ID、V5存档或永久人物；
- 不宣称P2全国105个郡国映射已经完成。

## 六、验收标准

1. 稳定地理表恰有6条记录：1个宏区、5个郡国尺度子区；
2. 五个子区父级均存在，稳定地理层级无循环；
3. 地理状态均为`provisional`，`provisional=true`且坐标留空；
4. 映射表恰有5条记录，覆盖涿郡、中山国、安平国、钜鹿郡、魏郡；
5. 五个行政来源各自权重严格等于10,000；
6. 映射来源数为5、权重错误数为0；
7. 游戏地点交叉表仍为空；
8. 专用验证、失败样例与确定性审计测试通过；
9. 全工程编译、核心回归和`git diff --check`通过。

## 七、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：6条，其中1个宏区父节点、5个郡国尺度子区
- 140年郡国映射：5条，覆盖5个行政来源，权重错误0
- 游戏地点交叉：0条，保留至P3
- 存档影响：无
- Unity序列化影响：无
- 专用数据验证：`RESULT han140-validation=passed sources=3 admin=119 population=105 regions=6 mappings=5 crosswalks=0`
- 专用验证测试：`RESULT han140-tests passed=24 failed=0`
- 全工程编译：通过
- 核心回归：`RESULT passed=104 failed=0`
- Unity测试：未运行；本任务只修改离线CSV、JSON、文档和PowerShell数据测试
- 差异检查：通过
- 确定性审计：6条稳定地理和5条映射均为临时记录；映射行政来源数5，
  权重错误数0
- 下一阶段建议：继续P2第二批，优先覆盖原型走廊相邻的广阳郡、河间国、
  常山国、清河国、赵国，并为后续道路与人口迁移建立连续稳定地理带
