# 下一实现任务冻结范围

## 任务名称

`LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1`

## 目标

把已经通过基础不变量审计的洛阳 184 都市圈 400,000 永久人物，作为唯一人口来源接入主世界、新游戏和存档边界；以幂等方式绑定历史 Person、Clan/Branch、FamilyOrganization 与可选 FamilyCenter 合同，关闭本审查的五项 High 阻断。

## IN_SCOPE

1. 建立 CanonicalPlace、行政县、战略城市、场景地点和 Region 的显式运行时 crosswalk。
2. 将 25 个 `Pxxxx` 历史人物幂等绑定到现有 40 万人物，禁止二次生成和静默改指。
3. 绑定历史人物的 Clan/Branch 元数据，但不自动生成全国 FamilyOrganization。
4. 对 7 个城市旧组织做保留稳定 ID 的显式迁移；纠正 `f088`、`f036` 的历史成员污染；保留全部永久人物。
5. 保留 8 个近郊生成组织为开局家庭/庄园组织，除非有明确证据，不赋予历史 Clan 主张。
6. 增加数据驱动的 FamilyOrganization profile 与可选 FamilyCenter 指定合同；当前所有中心默认 `NONE`。
7. 把 Property/Facility 引用、Owner、Controller、manager Person 和 `FamilyManagement` 能力映射到统一 Facility 合同。
8. 明确开局居住/岗位以二进制 Facility index 为权威，迁移或去权威化 1,116 个含旧人物 ID 的内联列表字段。
9. 将 40 万复合包投影到主人口仓库、NewGame、WorldState 与存档边界，保证无第二人口账。
10. 若引入持久字段，执行从当前 V68 开始的顺序迁移、旧版本读取、往返保存与不变量测试。
11. 加入跨包校验器与 190 同 ID 兼容钩子；只冻结接口，不实现 190 事件。
12. 按项目规则完成全工程编译、核心测试、受控 Unity 测试、`git diff --check` 和差异审阅。

## OUT_OF_SCOPE

- 虎牢、函谷 Cell/Facility/人口/军力物化；
- 70 万供给区和全国永久人物物化；
- 全国 FamilyOrganization/FamilyCenter 生成；
- 通用 Facility 目录、全部能力或全国设施的重构；
- 完整官府、官职、军队、补给、战争和 HistoricalChange 执行；
- 190 焚毁、迁都、迁徙与控制变化玩法；
- UI、美术、场景、Prefab 和玩家交互；
- 新的历史研究或把 UNKNOWN 猜成确定事实；
- 删除、合并、重随机任何永久人物；
- 自动提交或推送仓库。

## 强制验收

- 初始化运行两次后仍为 400,000 人、80,899 户、2,084 Facility、25 个唯一历史 Person 绑定；
- 旧人物/家户/亲属/居住/岗位/Cell 不变量全为 0 异常；
- `f088`、`f036` 迁移有明确前后记录且无人被删除；
- 15 个组织均可保存/重载，FamilyCenter 未满足五要件时保持 `NONE`；
- 主世界不存在第二套洛阳人口或 190 复制对象；
- 旧 V68 存档顺序迁移、当前版本往返和缺失内容 ID 失败策略均有测试证据。
