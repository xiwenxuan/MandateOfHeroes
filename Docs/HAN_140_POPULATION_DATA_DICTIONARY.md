# 140年郡国人口数据字典

## 1. 适用范围

本字典定义`Data/HistoricalPopulation/`中140年行政、人口和稳定地理数据的合同。
静态校录数据与运行时存档分离；CSV和JSON不直接成为永久人物或当前V5存档状态。

本字典只描述格式。史实数字、校勘判断、现代定位和游戏映射必须分别保存，不能以
一个“最终值”覆盖争议。

## 2. 通用编码与表示

| 项目 | 规则 |
|---|---|
| 文件编码 | UTF-8，无效UTF-8拒绝 |
| CSV | 第一行固定表头，逗号分隔；含逗号、引号或换行的值必须按CSV转义 |
| 空值 | CSV空字段；原文缺失不得写成`0`、`unknown`或`null`字符串 |
| 整数 | 十进制ASCII数字，不带千位分隔符、单位和小数 |
| 布尔 | 仅`true`或`false`，小写 |
| 多值引用 | 使用`|`分隔稳定ID；不得以显示名作为引用 |
| 年份 | 公元纪年正整数；起始年不得晚于终止年 |
| 置信度 | `high`、`medium`、`low`或`unknown` |
| 证据等级 | `H`、`R`、`M`、`I`或组合`R/M` |
| 模型版本 | `han140.p<阶段>[.<批次>].v<版本>`；当前使用`han140.p1.batch1.v1`至`han140.p1.batch14.v1` |

## 3. ID命名空间

| 类型 | 格式 | 示例 | 职责 |
|---|---|---|---|
| 来源 | `source.*` | `source.hou_han_shu.jun_guo_zhi` | 文献、研究或项目模型 |
| 140年行政单位 | `admin.han140.*` | `admin.han140.youzhou.zhuo` | 140年行政截面 |
| 稳定地理 | `geo.region.*` | `geo.region.youzhou.zhuo_core` | 跨年代稳定空间 |
| 运行时地点 | `location.*` | `location.zhuo` | Unity运行时地点 |
| 原型目录 | `L###` | `L001` | 184年原型地图目录 |
| 城市目录 | `C###` | `C012` | 77城设计目录 |

ID使用小写ASCII字母、数字、点、下划线和连字符；公开后不得因显示名或行政沿革改名。
`L###`和`C###`仅在游戏地点交叉表中使用，不能成为人口事实的来源ID。

## 4. `han_140_sources.json`

根对象：

| 字段 | 类型 | 必需 | 说明 |
|---|---|---:|---|
| `schema_version` | string | 是 | 固定为`han140.sources.v1` |
| `dataset_year` | integer | 是 | 固定为140 |
| `national_anchor` | object | 是 | 全国史籍锚点 |
| `national_anchor.registered_households` | integer | 是 | 固定为9698630 |
| `national_anchor.registered_population` | integer | 是 | 固定为49150220 |
| `national_anchor.source_ids` | string[] | 是 | 锚点来源，必须存在于`sources` |
| `sources` | object[] | 是 | 来源登记 |

每条来源：

| 字段 | 类型 | 必需 | 说明 |
|---|---|---:|---|
| `source_id` | string | 是 | 唯一`source.*` ID |
| `source_type` | string | 是 | `primary_text`、`modern_research`、`project_model`或`reference_index` |
| `title` | string | 是 | 题名 |
| `author_or_editor` | string | 是 | 作者、编者或责任主体 |
| `edition_or_host` | string | 是 | 版本、载体或仓库 |
| `publication_or_access_date` | string | 是 | 原始年代、出版年或访问日期 |
| `url_or_bibliographic_locator` | string | 是 | URL、卷次页码或仓库路径 |
| `license_or_public_domain_note` | string | 是 | 公共领域、项目许可证或只引用事实的边界 |
| `evidence_scope` | string | 是 | 支持哪些字段或判断 |
| `notes` | string | 是 | 校录限制与补充说明，可写`none` |

未知或不兼容许可的现代材料可以登记为线索，但不得复制其受保护的完整表格、注释或表达。

## 5. `han_140_administrative_units.csv`

固定表头：

```text
admin_unit_id,parent_admin_unit_id,unit_type,name_140,canonical_name,seat_admin_unit_id,valid_from_year,valid_to_year,source_ids,confidence,notes
```

| 字段 | 规则 |
|---|---|
| `admin_unit_id` | 唯一`admin.han140.*` |
| `parent_admin_unit_id` | 空或同表已存在ID；不得形成循环 |
| `unit_type` | `empire`、`province`、`commandery`、`kingdom`、`county`或`other` |
| `name_140` | 140年显示名 |
| `canonical_name` | 项目规范名，不承担稳定身份 |
| `seat_admin_unit_id` | 空或同表县级/治所行政ID |
| `valid_from_year` | 有效起年，必须覆盖140年 |
| `valid_to_year` | 有效止年，必须覆盖140年 |
| `source_ids` | 至少一个有效来源 |
| `confidence` | 通用置信度枚举 |
| `notes` | 可空 |

## 6. `han_140_population_records.csv`

固定表头：

```text
admin_unit_id,registered_households_raw,registered_population_raw,registered_households_corrected,registered_population_corrected,correction_code,correction_note,evidence_grade,source_ids,source_locator,model_version
```

| 字段 | 规则 |
|---|---|
| `admin_unit_id` | 同表唯一；引用行政单位 |
| `registered_households_raw` | 空或非负整数 |
| `registered_population_raw` | 空或非负整数 |
| `registered_households_corrected` | 空或非负整数 |
| `registered_population_corrected` | 空或非负整数 |
| `correction_code` | 发生任一修正时必填；未修正时留空 |
| `correction_note` | 发生任一修正时必填 |
| `evidence_grade` | 必需证据等级 |
| `source_ids` | 至少一个有效来源 |
| `source_locator` | 卷、页、行、表或URL定位 |
| `model_version` | 使用`han140.p<阶段>[.<批次>].v<版本>`格式；当前按录入批次使用`han140.p1.batch1.v1`至`han140.p1.batch14.v1` |

修正值不覆盖原始值。审计报告同时输出原始合计、显式修正合计，以及
“有修正取修正、无修正取原始”的有效合计。

## 7. `stable_population_regions.csv`

固定表头：

```text
stable_region_id,parent_stable_region_id,region_type,canonical_name,modern_reference,centroid_latitude,centroid_longitude,geometry_status,confidence,provisional,notes
```

| 字段 | 规则 |
|---|---|
| `stable_region_id` | 唯一`geo.region.*` |
| `parent_stable_region_id` | 空或同表ID；不得循环 |
| `region_type` | `macroregion`、`province_area`、`commandery_area`、`county_area`、`city_circle`或`other` |
| `canonical_name` | 项目稳定显示名 |
| `modern_reference` | 现代近似定位，不作为历史边界结论 |
| `centroid_latitude` | 空或-90至90的小数 |
| `centroid_longitude` | 空或-180至180的小数 |
| `geometry_status` | `none`、`approximate`、`provisional`或`verified` |
| `confidence` | 通用置信度枚举 |
| `provisional` | `true`或`false` |
| `notes` | 可空 |

## 8. `han_140_region_mapping.csv`

固定表头：

```text
source_id,target_id,relation_type,valid_from_year,valid_to_year,weight_basis_points,mapping_method,confidence,provisional,notes
```

`source_id`引用140年行政单位，`target_id`引用稳定地理单元。
同一`source_id`参与人口分配的全部行，其`weight_basis_points`合计必须严格等于10000。
空映射表在P0合法；一旦某来源出现一条映射，就必须完整守恒。

## 9. `game_location_crosswalk.csv`

固定表头：

```text
game_location_id,game_location_kind,stable_region_id,admin_unit_id,mapping_status,relation_type,valid_from_year,valid_to_year,source_ids,confidence,provisional,notes
```

| 字段 | 规则 |
|---|---|
| `game_location_id` | `location.*`、`L###`或`C###` |
| `game_location_kind` | `runtime`、`prototype_catalog`或`city_catalog` |
| `stable_region_id` | 空或稳定地理ID |
| `admin_unit_id` | 空或140年行政ID |
| `mapping_status` | `exact`、`aggregate`、`approximate`或`unresolved` |
| `relation_type` | 映射关系说明枚举或稳定代码 |
| `valid_from_year` / `valid_to_year` | 空或有效年份范围 |
| `source_ids` | `unresolved`可空；其他状态至少一个有效来源 |
| `confidence` | 通用置信度枚举 |
| `provisional` | `true`或`false` |
| `notes` | 可空 |

P0允许只有表头；M13-P3必须覆盖现有6个`location.*`和77个`C###`目录。
P3第一、二批使用以下稳定关系代码：

- `runtime_county_identity`：运行时县级地点对应临时县级稳定身份；
- `runtime_regional_proxy`：运行时区域代理对应郡国尺度稳定身份；
- `prototype_catalog_alias`：`L###`县级目录与运行时地点共用身份；
- `prototype_catalog_county_identity`：`L###`县级目录直接对应临时县级稳定身份；
- `prototype_catalog_regional_proxy`：`L###`区域代理；
- `prototype_catalog_unresolved`：`L###`尚不能安全归入行政或稳定地理身份；
- `city_catalog_alias`：`C###`城市目录与县级地点共用身份；
- `city_catalog_county_identity`：`C###`显示节点对应140年县级候选与临时县域；
- `city_catalog_regional_proxy`：`C###`郡域级战略代理；
- `city_catalog_unresolved`：`C###`尚不能安全归入140年行政或稳定地理身份。

关系代码不表示边界已经精确复原；空间精度仍由`mapping_status`、`confidence`和
`provisional`共同表达。

## 10. `han_140_audit_report.json`

审计报告由验证器生成，固定包含：

- 合同和数据年份；
- `validation_status`；
- 全国户、口锚点；
- 六个输入表的记录数；
- 原始、显式修正和有效人口合计；
- 有效合计与全国锚点差额；
- 缺失原值、修正记录和争议记录数量；
- 人口覆盖映射与县级身份映射的来源数、权重总和错误数；
- 1180常见参考数、1181州部小计、1182逐项校录数及巴郡标题/正文差异；
- `runtime`、`prototype_catalog`和`city_catalog`覆盖及各映射状态数量；
- 已登记来源ID。

报告不写生成时间、机器路径、用户名或随机值。相同语义输入必须产生相同统计结果。

### 10.1 `han_140_m12_population_input.json`

该文件由同一验证器确定性生成，是M13交给M12的只读消费合同：

- `schema_version=han140.m12-input.v1`；
- 只含105条郡国、尹、属国人口来源，不把1182县重复计算为人口；
- 每项保存原始值、显式修正值、有效值、证据等级、来源、治所县ID和稳定地理权重；
- 每个人口来源的`mappings`权重合计必须为10,000基点；
- `county_catalog_count=1182`仅声明可用县级身份目录规模。

M12必须按稳定ID读取，不得用中文显示名连接表，也不得因关注等级重新随机人口事实。

## 11. 阶段边界与当前批次

P0模板中的行政、人口、稳定地理、映射和交叉表可以只有表头。记录数为0不是完成，
只是允许后续按批次添加数据。P1至P3每批新增数据后都必须运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  Tools/Validate-Han140PopulationData.ps1
```

M13正式收尾已满足总任务书完成条件。当前机器可审计口径为：4项来源、1301项
行政单位（1帝国、13州、105郡国/尹/属国、1182县级列项）、105条人口记录、
1336个稳定地理节点（49宏区、105郡国区、1182县区）、1287条映射（105人口覆盖、
1182县级身份）和95条地点交叉（6运行时、12原型、77城市）。城市映射状态为
`approximate=80`、`aggregate=11`、`unresolved=4`；没有为追求完成率伪造精确对应。
审计与M12输入均可重复生成并逐字节一致。

以下内容保留各批次实现历史；其中“仍未完成”只描述当时批次边界，不代表当前状态。

当前P1十四批已录入汉及司隶、豫州、兖州、徐州、青州、荆州、扬州、益州、
凉州、并州、幽州、冀州、交州层级，共105条郡国、尹或属国人口记录，P1全国
郡国级校录已经收口。辽东属国、郁林郡、交趾郡原文户口为空；相关估算与
辽东郡人口、玄菟郡户数修正分别保留
原始值与修正值。司隶河南尹、弘农郡的现代ODS异录只作说明，不覆盖卷029原典值。
豫州沛国、陈国的百万位移置判断及兖州泰山郡、徐州琅邪国户数十万位脱漏判断只写入修正字段，
原文值继续保留。P2十五批已建立154个临时稳定地理节点及105条郡国人口守恒映射，
全国105项郡国、尹、属国人口来源已全部覆盖；
游戏地点交叉表仍为空，保留至P3；
青州济南条的郡国转录异文写入行政备注，140年按国保存；荆州七郡和扬州六郡
两份公开转录数值一致。益州永昌郡保留异常原值但不虚构修正，广汉属国旧注
所属讹文写入行政备注。凉州酒泉郡缺失口数和敦煌郡疑脱千位分别保留原值与
现代校录补正；现代说明中的数值矛盾按其方法和公开修正总计审计。并州九郡
两份公开古籍转录数值一致。交州郁林、交趾原值留空，现代区域估算只进入
修正字段并标M级。P1完成不表示稳定地理、县级目录、游戏映射或M13整体完成。

P2首批覆盖当前六个运行时地点涉及的涿郡、中山国、安平国、钜鹿郡和魏郡。
每个行政来源暂映射到一个`provisional`郡国尺度地理桶，权重均为10,000基点。
钜鹿郡只计算一次，不能因下曲阳、广宗两个运行时地点重复人口。该批次只建立
人口守恒基线，不表示古郡界、县界、治所或游戏地点交叉已经考定。

P2第二批沿用河北宏区，新增广阳郡、河间国、常山国、清河国、赵国五个
`commandery_area`稳定子区及五条10,000基点守恒映射。现代地名只作近似定位，
坐标继续留空，全部边界与映射均为`provisional`；游戏地点交叉仍留至P3。

P2第三批为勃海郡新增河北宏区东部子区，并新建“燕山与辽西走廊宏区”，
承接渔阳郡、右北平郡和辽西郡三个稳定子区。该批新增5个稳定节点和4条
10,000基点守恒映射，至此冀州九个郡国人口来源全部拥有P2临时映射；
幽州、全国稳定地理和游戏地点交叉仍未完成。

P2第四批新建“燕北山地与长城走廊”和“辽东与朝鲜半岛北部”两个宏区，
新增代郡、上谷郡、辽东郡、玄菟郡、乐浪郡及辽东属国六个稳定子区和六条
10,000基点守恒映射，至此幽州十一郡、属国人口来源全部拥有P2临时映射。
辽东属国映射只承接显式M级有效估算，原文户口仍保持缺项；P2全国覆盖与
游戏地点交叉仍未完成。

P2第五批新建“河洛与黄河北岸”“汾河南段与黄河东岸”“关中渭河平原”
三个宏区，为河南尹、河内郡、河东郡、弘农郡、京兆尹、左冯翊和右扶风
新增七个稳定子区与七条10,000基点守恒映射，至此司隶七个郡、尹全部拥有
P2临时映射。河南尹、弘农郡的ODS异录仍只作说明，没有生成修正值；
P2全国覆盖与游戏地点交叉仍未完成。

P2第六批新建“颍汝与淮河北岸”“睢水与淮北北部平原”“泗沂西缘与山前”
三个宏区，为颍川郡、汝南郡、梁国、沛国、陈国和鲁国新增六个稳定子区与
六条10,000基点守恒映射，至此豫州六郡国全部拥有P2临时映射。沛国、陈国
仍分别保存史籍原值与配对百万位移置修正；P2全国覆盖与游戏地点交叉仍未完成。

P2第七批新建“黄河下游与济水西部平原”“汶泗水系与鲁西南平原”
“泰沂山地与西部山前”三个宏区，为陈留郡、东郡、东平国、任城国、泰山郡、
济北国、山阳郡和济阴郡新增八个稳定子区与八条10,000基点守恒映射，至此
兖州八郡国全部拥有P2临时映射。泰山郡仍分别保存原始户数与十万位脱漏修正；
P2全国覆盖与游戏地点交叉仍未完成。

P2第八批新建“沂沭水系与淮海北部”“泗水中下游平原”
“江淮东部与下游水网”三个宏区，为东海郡、琅邪国、彭城国、广陵郡和
下邳国新增五个稳定子区与五条10,000基点守恒映射，至此徐州五郡国全部拥有
P2临时映射。琅邪国仍分别保存20,804原始户数与120,804修正户数，570,967口
不增加无关修正；P2全国覆盖与游戏地点交叉仍未完成。

P2第九批新建“黄河下游与济水东部平原”“淄潍水系与胶莱西部平原”
“胶东半岛与北部海岸”三个宏区，为济南国、平原郡、乐安国、北海国、
东莱郡和齐国新增六个稳定子区与六条10,000基点守恒映射，至此青州六郡国
全部拥有P2临时映射。济南条140年仍按国保存，“济南郡”继续只作为转录异文，
不生成不存在的人口修正；P2全国覆盖与游戏地点交叉仍未完成。

P2第十批新建“汉水中上游与南阳盆地”“汉水下游与江汉平原”
“洞庭湖与湘资沅澧水系”“南岭北麓与湘水上游”四个宏区，为南阳郡、南郡、
江夏郡、武陵郡、长沙郡、零陵郡和桂阳郡新增七个稳定子区与七条10,000基点
守恒映射，至此荆州七郡全部拥有P2临时映射。本批不修改七郡原始户口值，
不把州、郡边界或现代省界伪装成稳定物理边界；P2全国覆盖与游戏地点交叉仍未完成。

P2第十一批新建“淮南与长江北岸”“长江下游与太湖水网”
“钱塘江与浙东丘陵”“赣江与鄱阳湖盆地”四个宏区，为九江郡、庐江郡、
丹阳郡、吴郡、会稽郡和豫章郡新增六个稳定子区与六条10,000基点守恒映射，
至此扬州六郡全部拥有P2临时映射。本批不修改六郡原始户口值，不将现代河道、
湖岸或省界伪装为东汉郡界；P2全国覆盖与游戏地点交叉仍未完成。

P2第十二批新建“汉中盆地与秦巴走廊”“四川盆地与盆周河谷”
“川西山地与高原过渡走廊”“云贵高原与山间盆地”“横断山南缘与滇西纵谷”
五个宏区，为益州九郡和三属国新增十二个稳定子区与十二条10,000基点守恒映射，
至此益州十二项人口来源全部拥有P2临时映射。永昌郡原始疑值和广汉属国旧注
归属异文继续原样保留，不生成地理映射修正；P2全国覆盖与游戏地点交叉仍未完成。

P2第十三批新建“陇右黄土高原与渭水上游”“陇南山地与秦巴北缘”
“河湟谷地与黄河上游”“陇东宁南黄土高原与鄂尔多斯南缘”
“河西走廊与祁连山北麓绿洲”“黑河下游与居延绿洲”六个宏区，为凉州十郡
和两属国新增十二个稳定子区与十二条10,000基点守恒映射，至此凉州十二项
人口来源全部拥有P2临时映射。酒泉原始口数仍为空，敦煌原始748户仍保留，
北地、武威公开转录异文仍只作审计说明；P2全国覆盖与游戏地点交叉仍未完成。

P2第十四批新建“太行山西麓与上党盆地”“汾河谷地与吕梁山东麓”
“陕北黄土高原与无定河”“黄河河套平原”“阴山南麓与土默川”
“雁门山地与桑干河盆地”六个宏区，为并州九郡新增九个稳定子区与九条
10,000基点守恒映射，至此并州九项人口来源全部拥有P2临时映射。九郡继续
保留115,011户、696,765口且不生成修正，雁门郡31,862户、249,000口原值
保持不变；P2全国覆盖与游戏地点交叉仍未完成。

P2第十五批新建“珠江三角洲与岭南东部河网”“西江—郁江水系与桂中东盆谷”
“北部湾沿海与桂南滨海平原”“红河三角洲与越北低地”
“越南北中部河谷与沿海走廊”五个宏区，为交州七郡新增七个稳定子区与七条
10,000基点守恒映射，至此全国105项郡国、尹、属国人口来源全部拥有P2临时
映射。郁林、交趾原典户口继续为空，M级有效估算不覆盖原值；游戏地点交叉表
仍为空，P3县级目录与`location.*`、`L###`、`C###`交叉尚未开始。

P3第一批新增涿、下曲阳、广宗、邺四个140年县级行政候选和四个
`county_area`稳定地理身份，并建立当前6个运行时地点、对应6个184年原型目录
节点及相关3个77城节点共15条交叉映射。当前累计4项来源、123项行政单位、
158项稳定地理和15条地点交叉；人口记录与人口映射仍分别保持105项和105条。
中山、安平继续作为区域代理，`C010`钜鹿不等同于下曲阳或广宗，P3全国县级目录
及其余74个`C###`状态仍未完成。

P3第二批新增蓟、廮陶两个140年县级行政候选与两个`county_area`稳定地理身份，
并补齐其余6个184年原型目录节点。当前累计4项来源、125项行政单位、160项稳定
地理和21条地点交叉，其中`runtime=6`、`prototype_catalog=12`、
`city_catalog=3`；状态为`approximate=11`、`aggregate=8`、`unresolved=2`。
至此`L001`至`L012`全部具备显式状态。广阳战区、钜鹿郡治只作郡域代理；
博陵中继与黄河—洛阳出口保持待考。人口记录与人口映射仍分别为105项和105条，
77城其余74个`C###`及全国县级目录仍未完成。

P3第三批开始扩展77城目录，为`C001-C008`、`C011`、`C013`十个北方节点
建立显式状态；新增襄平、朝鲜、土垠、晋阳、壶关、南皮、平原、甘陵八个
140年县级行政候选与八个`county_area`稳定地理身份。当前累计4项来源、
133项行政单位、168项稳定地理和31条地点交叉，其中`runtime=6`、
`prototype_catalog=12`、`city_catalog=13`；状态为`approximate=20`、
`aggregate=8`、`unresolved=3`。人口记录与人口映射仍分别为105项和105条。
`C001-C013`现已全部覆盖，`C013`城阳保持待考；`C014-C077`与全国县级目录
仍未完成。
