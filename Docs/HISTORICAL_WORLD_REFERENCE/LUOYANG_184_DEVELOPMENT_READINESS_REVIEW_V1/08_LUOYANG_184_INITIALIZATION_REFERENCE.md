# 洛阳 184 初始化权威入口 V1

## 1. 唯一物理地点与 ID crosswalk

| 语义 | 稳定 ID | 规则 |
|---|---|---|
| CanonicalPlace | `place.han140.sili.henan.luoyang` | 人类可识别的唯一物理洛阳地点。 |
| 行政县 | `admin.han140.sili.henan.luoyang` | 历史行政单位，不是第二个 Place。 |
| 战略城市 | `C027` | 战略目录显示/引用 ID，不是第二个 Place。 |
| 场景地点 | `location.capital.luoyang` | 场景/事件兼容引用；主世界投影时必须绑定 CanonicalPlace。 |
| 稳定区域 | `geo.region.central.china.heluo.luoyangbasin.county.luoyang` | 连续世界 Region 引用。 |
| 世界格网 | `HanWorldV1` / 2,000m Cell | 所有 Facility 使用同一格网；不得建第二张城市地图。 |
| 城市锚点 | `4114717` | 已有洛阳核心锚点，不表示整座城市只有一个 Cell。 |

下一实现任务必须把上述关系做成显式、可校验的运行时 crosswalk，不得靠 UI 名称猜测。

## 2. 唯一人口与空间口径

| 范围 | 人口 | 是否已物化 | 解释 |
|---|---:|---|---|
| 城内 | 200,000 | 是 | 27 万城市区的子集。 |
| 连续城市区 | 270,000 | 是 | 城市正式包。 |
| 都市圈 | 400,000 | 是 | 正式开局唯一人口基线；27 万 + 13 万近郊增量。 |
| 供给区 | 700,000 | 否 | 包含 40 万的规划包络，不得加法生成。 |
| 全国模型洛阳县 | 130,169 | 否 | 全国缩尺模型参考，不是需要额外生成的人口。 |
| 全国模型河南尹 | 1,070,779 | 否 | 上层包含范围。 |

正式开局必须读取 400,000 个既有人物和 80,899 个既有家户。任何重复执行都应保持人数、PersonId、Household ordinal、亲属和住宅不变。

## 3. 运行来源优先级

1. 都市圈 manifest 与受保护文件合同；
2. 城市 `persons.bin`、`households.bin`、`facilities.json`；
3. 近郊增量 `outer_persons.bin`、`outer_households.bin`、`facilities.json`；
4. 城市历史人物 overlay 的 25 个精确 `Pxxxx` 绑定；
5. 历史人物/Clan/Branch/Scenario 母库提供历史语义，不再次生成人物；
6. 全国人口母盘只用于尺度一致性和上层人口约束。

住宅和岗位的开局人物分配以二进制记录中的 Facility index 为权威。城市设施 JSON 中旧 `worker_person_ids` / `resident_person_ids` 含有旧生成 ID，下一任务必须迁移或去权威化，禁止以其覆盖正式人物分配。

## 4. FamilyOrganization 与 FamilyCenter

- 7 个城市旧组织和 8 个近郊生成组织均保留稳定 ID；
- 组织成员纠错不能删除、合并或重随机人物；
- Clan、Branch、Household、FamilyOrganization、FamilyCenter 是不同对象；
- 当前 15 个组织的 FamilyCenter 均为 `NONE`；
- 只有真实 Facility、`FamilyManagement` 能力、合法所有/控制、管理者 Person、Primary/Local 指定同时成立时才能设中心。

## 5. 184 与 190

开局快照时点固定为 `YEAR_START`。184 初始化后的历史变化必须作用于同一 Person、Household、Facility 和 Cell。190 参考只预留同 ID 的前后状态与变化接口，本入口不授权实现焚毁、迁都、人口迁徙、家族转移或控制权变化。

## 6. 失败策略

缺失稳定 ID、重复历史 Person、未知 Facility/Cell、人口范围重复物化、旧内联人物列表被误当权威、或存档版本未顺序迁移时，初始化必须明确失败并输出审计记录；不得静默改指、合并人物或重新随机。
