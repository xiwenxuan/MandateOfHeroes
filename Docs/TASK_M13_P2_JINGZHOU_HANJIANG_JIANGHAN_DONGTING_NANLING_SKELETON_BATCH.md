# M13-P2任务书：荆州汉水—江汉平原—洞庭湖—南岭北麓稳定地理骨架

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖荆州刺史部南阳郡、南郡、江夏郡、武陵郡、长沙郡、零陵郡和
桂阳郡七个人口来源。完成后，荆州七郡全部具有临时稳定地理映射。

本批按盆地、河流、湖群和平原建立稳定地理身份，不用“荆州”或郡名替代物理
地理边界：

- `geo.region.central.china.hanjianguppermiddle`：汉水中上游与南阳盆地；
- `geo.region.central.china.jianghanplain`：汉水下游与江汉平原；
- `geo.region.south.china.dongtingxiangziyuanli`：洞庭湖与湘资沅澧水系；
- `geo.region.south.china.nanlingnorth`：南岭北麓与湘水上游。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 南阳郡 | `geo.region.central.china.hanjianguppermiddle.northeastbasin` | 南阳盆地与汉水上游地理区 | 汉水中上游与南阳盆地宏区 |
| 南郡 | `geo.region.central.china.jianghanplain.westernplain` | 江汉平原西部与长江中游地理区 | 汉水下游与江汉平原宏区 |
| 江夏郡 | `geo.region.central.china.jianghanplain.easternriverlake` | 江汉平原东部与江湖交汇地理区 | 汉水下游与江汉平原宏区 |
| 武陵郡 | `geo.region.south.china.dongtingxiangziyuanli.northwestbasin` | 沅澧流域与洞庭湖西部地理区 | 洞庭湖与湘资沅澧水系宏区 |
| 长沙郡 | `geo.region.south.china.dongtingxiangziyuanli.northeastplain` | 湘水下游与洞庭湖南部平原地理区 | 洞庭湖与湘资沅澧水系宏区 |
| 零陵郡 | `geo.region.south.china.nanlingnorth.southwestbasin` | 湘水上游与零陵盆地地理区 | 南岭北麓与湘水上游宏区 |
| 桂阳郡 | `geo.region.south.china.nanlingnorth.southeastfoothill` | 南岭北麓与湘东南地理区 | 南岭北麓与湘水上游宏区 |

每个行政来源使用一条`single_provisional_commandery_bucket_v1`人口覆盖映射，
权重为10,000基点。本批新增4个宏区、7个`commandery_area`和7条映射。
所有坐标留空，`geometry_status=provisional`且`provisional=true`。

## 三、史料、地理与人口约束

- 七个行政来源继续使用P1已经校录的《后汉书》卷三十二户口记录；
- 七郡原始合计为1,399,394户、6,265,952口，且没有显式人口修正；
- 江汉—洞庭盆地作为长江中游相连地理系统，但本批仍以汉水下游、江汉平原、
  洞庭盆地和湘资沅澧水系区分人口桶；
- 南阳盆地通过汉水河谷与江汉地区相连，但南阳郡行政边界不能直接用作物理宏区；
- 湘水上游与南岭北麓只作临时稳定地理索引，不表示已复原东汉县界或交通路线；
- 历史河道、湖岸和洲滩变化不使用现代水系边界或精确坐标伪装。

## 四、明确不做

- 不绘制精确郡界、县界、古河道、古湖岸或未校验质心坐标；
- 不把现代河南、湖北、湖南省界当作东汉行政或自然地理边界；
- 不拆分人口到宛、江陵、襄阳、长沙、临沅、泉陵、郴等城市或县级节点；
- 不填入`game_location_crosswalk.csv`，该工作保留至P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计82条，包含23个根宏区和59个郡国尺度子区；
2. 映射表累计59条，覆盖59个唯一行政来源；
3. 本批11个稳定ID和7个行政来源无遗漏、重复或孤立引用；
4. 四个宏区的直接子区数依次为1、2、2、2；
5. 荆州七郡人口来源全部拥有一条P2临时映射；
6. 七郡仍恰好保留1,399,394户、6,265,952口，并且不产生人口修正；
7. 每个新增来源的映射权重严格等于10,000基点；
8. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
9. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
   `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计82条，其中23个宏区、59个郡国尺度子区
- 140年郡国映射：累计59条，权重错误0
- 荆州映射覆盖：7/7
- 游戏地点交叉：0条，保留至P3
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：通过；`regions=82`、`mappings=59`、`crosswalks=0`
- 专项验证测试：通过；33/33
- 全工程编译：通过
- 核心回归：通过；104/104
- Unity测试：未运行；本任务只修改离线CSV、JSON、文档和PowerShell数据测试
- 差异检查：通过
- 下一阶段建议：继续P2第十一批，优先建立扬州长江下游—淮南—江南丘陵—
  鄱阳湖稳定地理骨架。
