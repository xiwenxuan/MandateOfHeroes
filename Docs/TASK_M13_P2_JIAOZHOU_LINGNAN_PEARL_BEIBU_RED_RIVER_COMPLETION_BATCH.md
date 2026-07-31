# M13-P2任务书：交州岭南—珠江—北部湾—红河稳定地理与全国收口批次

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖交州刺史部南海、苍梧、郁林、合浦、交趾、九真、日南七郡。
完成后，交州七项以及P1全国105项郡国、尹、属国人口来源全部具有临时稳定
地理映射，M13-P2全国郡国级覆盖收口。

本批按三角洲、干支流水系、海湾沿岸和中南半岛北部山地海岸建立稳定地理身份：

- `geo.region.south.china.pearlriverdelta`：珠江三角洲与岭南东部河网；
- `geo.region.south.china.xijiangyujiang`：西江—郁江水系与桂中东盆谷；
- `geo.region.south.china.beibugulfcoast`：北部湾沿海与桂南滨海平原；
- `geo.region.southeastasia.redriverdelta`：红河三角洲与越北低地；
- `geo.region.southeastasia.northcentralvietnam`：越南北中部河谷与沿海走廊。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 南海郡 | `geo.region.south.china.pearlriverdelta.eastcentraldeltafoothills` | 珠江三角洲中东部与岭南东部丘陵地理区 | 珠江三角洲与岭南东部河网宏区 |
| 苍梧郡 | `geo.region.south.china.xijiangyujiang.easternxijiangbasin` | 西江中下游与桂东盆谷地理区 | 西江—郁江水系与桂中东盆谷宏区 |
| 郁林郡 | `geo.region.south.china.xijiangyujiang.centralyujiangbasin` | 郁江流域与桂中南盆谷地理区 | 西江—郁江水系与桂中东盆谷宏区 |
| 合浦郡 | `geo.region.south.china.beibugulfcoast.northeastcoastalplain` | 北部湾东北岸与桂南滨海平原地理区 | 北部湾沿海与桂南滨海平原宏区 |
| 交趾郡 | `geo.region.southeastasia.redriverdelta.centralnortherndelta` | 红河中下游与越北三角洲地理区 | 红河三角洲与越北低地宏区 |
| 九真郡 | `geo.region.southeastasia.northcentralvietnam.northmariverbasin` | 马江流域与越北中部盆谷地理区 | 越南北中部河谷与沿海走廊宏区 |
| 日南郡 | `geo.region.southeastasia.northcentralvietnam.centralcoastalstrip` | 越南北中部沿海与山地走廊地理区 | 越南北中部河谷与沿海走廊宏区 |

每个行政来源使用一条`single_provisional_commandery_bucket_v1`人口覆盖映射，
权重为10,000基点。本批新增5个宏区、7个`commandery_area`和7条映射。
所有坐标留空，`geometry_status=provisional`且`provisional=true`。

## 三、史料、地理与人口约束

- 七个行政来源继续使用P1校录的《后汉书》卷三十三户口记录；
- 七郡原始合计为270,769户、1,114,444口，有效合计为412,600户、
  2,066,166口；
- 七项全部继续按`commandery`保存；
- 南海、苍梧、合浦、九真、日南五郡继续使用H级原始户口；
- 郁林、交趾原典户口继续为空，12,415户/71,162口和
  129,416户/880,560口只作为M级显式估算；
- 映射郁林、交趾只表示其有效人口拥有临时空间桶，不把估算伪装为史籍原值；
- 现代地貌只用于临时索引，不表示东汉郡界、县界、海岸线、红河故道或
  中南半岛政治边界已经精确复原。

现代地理交叉核对资料：

- 水利部珠江水利委员会珠江中下游河道与西江、北江、东江、三角洲水系资料：
  <https://www.pearlwater.gov.cn/zwgkcs/slghn/202109/P020210907321608156347.pdf>
- 广西壮族自治区人民政府北部湾、西江、郁江与桂南诸河资料：
  <https://www.gxzf.gov.cn/html/zwgk/zfxxgkzl_84988/fdzdgknr/zdmsxx/zfbz_182764/ydjh915_182772/t19113220.shtml>
- 越南政府国家地理与红河三角洲资料：
  <https://vietnam.gov.vn/geography-68963>
- 越南外交部地形、河流、红河三角洲及北中部沿海资料：
  <https://new.mofa.gov.vn/web/ministry-of-foreign-affairs/geography>

## 四、明确不做

- 不绘制精确郡界、县界、古海岸、珠江与红河故道、港湾或质心坐标；
- 不把现代广东、广西、海南或越南行政边界当作东汉交州边界；
- 不拆分人口到番禺、广信、布山、合浦、龙编、胥浦、西卷等县级或城市节点；
- 不把郁林、交趾M级估算写入原始字段、改成H级或机械缩放到全国锚点；
- 不把交趾、九真、日南合并为同一个中南半岛人口桶；
- 不填入`game_location_crosswalk.csv`，该工作保留至P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不把P2完成描述为县级目录、游戏地点交叉、P4消费接口或M13整体完成。

## 五、验收标准

1. 稳定地理表累计154条，包含49个根宏区和105个郡国尺度子区；
2. 映射表累计105条，覆盖全部105个唯一人口行政来源；
3. 本批12个稳定ID和7个行政来源无遗漏、重复或孤立引用；
4. 五个宏区的直接子区数依次为1、2、1、1、2；
5. 交州七郡全部拥有一条P2临时映射，全国映射覆盖达到105/105；
6. 七郡原始合计仍为270,769户、1,114,444口，有效合计仍为
   412,600户、2,066,166口；
7. 郁林、交趾原始空值、M级估算和五郡H级原值继续可审计；
8. 每个新增来源的映射权重严格等于10,000基点；
9. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
10. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
    `git diff --check`通过。

## 六、完成记录

- 状态：已完成（2026-07-31）
- 存档影响：无
- Unity序列化影响：无
- 数据结果：稳定地理154条（根宏区49、郡国尺度子区105），人口映射105条，
  覆盖105/105个P1人口来源；游戏地点交叉映射仍为0条。
- 交州结果：七郡全部保持`commandery`，原始合计270,769户、1,114,444口，
  有效合计412,600户、2,066,166口；郁林、交趾原始户口继续留空并保留
  M级估算，另五郡继续保留H级原始记录。
- 验证结果：
  - `Validate-Han140PopulationData.ps1`通过：
    `regions=154 mappings=105 crosswalks=0`；
  - `Test-Han140PopulationValidator.ps1`通过：38/38；
  - 全工程编译通过；
  - 核心回归通过：104/104；
  - Unity测试未运行：本批仅修改离线CSV、生成审计JSON、文档和PowerShell测试，
    不涉及Unity场景、序列化或运行时程序集；
  - `git diff --check`通过。
- 下一阶段建议：进入M13-P3第一批，优先建立当前6个运行时地点及其相关
  县级候选、`L###`与`C###`目录的可审计交叉映射。
