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
| 模型版本 | `han140.p<阶段>[.<批次>].v<版本>`；当前使用`han140.p1.batch1.v1`至`han140.p1.batch5.v1` |

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
| `model_version` | 使用`han140.p<阶段>[.<批次>].v<版本>`格式；当前按录入批次使用`han140.p1.batch1.v1`至`han140.p1.batch5.v1` |

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

## 10. `han_140_audit_report.json`

审计报告由验证器生成，固定包含：

- 合同和数据年份；
- `validation_status`；
- 全国户、口锚点；
- 六个输入表的记录数；
- 原始、显式修正和有效人口合计；
- 有效合计与全国锚点差额；
- 缺失原值、修正记录和争议记录数量；
- 映射来源数、权重总和错误数；
- 已登记来源ID。

报告不写生成时间、机器路径、用户名或随机值。相同语义输入必须产生相同统计结果。

## 11. 阶段边界与当前批次

P0模板中的行政、人口、稳定地理、映射和交叉表可以只有表头。记录数为0不是完成，
只是允许后续按批次添加数据。P1至P3每批新增数据后都必须运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  Tools/Validate-Han140PopulationData.ps1
```

只有M13总任务书的完整条件全部满足，才能把140年郡国人口状态从“待研究”升级。

当前P1前五批已录入汉、司隶、豫州、幽州、冀州层级及33条人口记录，其中司隶
七郡尹、豫州六郡国、冀州九郡国、幽州十一郡与属国已经完成。辽东属国原文户口
为空；辽东郡人口、玄菟郡户数和辽东属国估算分别保留原始值与修正值。司隶
河南尹、弘农郡的现代ODS异录只作说明，不覆盖卷029原典值。豫州沛国、陈国的
百万位移置判断只写入修正字段，原文人口继续保留。稳定地理、映射和游戏地点
交叉表仍为空，分别保留至P2、P3；不得把四个区域完成描述为全国P1完成。
