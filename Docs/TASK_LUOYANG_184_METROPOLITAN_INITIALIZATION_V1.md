# LUOYANG-184-METROPOLITAN-INITIALIZATION-V1

## 1. 目标

在既有 `LUOYANG-184-URBAN-INITIALIZATION-V1` 的 270,000 名永久人物基础上，
以只追加、不重排的方式初始化 184 年洛阳近郊生活圈，新增恰好 130,000 名永久人物，
使洛阳都市圈总人口达到 400,000。原有 Person、Household、FamilyOrganization、
Facility、Force、历史人物和事件文件必须逐字节保持不变。

本任务不生成 700,000 人供给区，不扩张洛阳城墙，不引入 SubCell，也不建立第二套
人物或家户模型。

## 2. 正式交付合同

- 新增人口按 GateSuburb、SouthernSuburb、RoadSettlement、NearVillage、
  EliteEstate、AgriculturalFringe、LogisticsNode、WaterAndResourceNode 八类空间落地。
- 每名新增人物持有永久 ID、真实 Household、Residence Facility、职业、岗位或明确的
  非就业状态、父母/配偶关系和当前位置。
- Household 与 FamilyOrganization 分离；仅少量证据锚定或玩法补全家族形成长期组织。
- 一个 Cell 同时只有一个 Owner 和一个基础 Facility；行政控制与产权分别记录。
- 村落、庄园、农田、道路、仓储、驿站、客舍、水利和物流节点均使用既有 2,000 米 Cell。
- 农田一 Cell 一 Facility，保存播种日、成熟日、早收阈值、产量、库存和真实劳动力。
- 粮食、木材、一般货物、手工业原料和畜产品至少各有一条可审计的
  生产—仓储—承运—城门—城市目的地链路。
- 城内短距通勤可以按明确范围聚合，但跨聚落移动必须进入真实道路和旅行接口。
- 既有五支军队可从城门接入道路，消耗补给；本阶段不实现完整围城 AI。
- 黄巾事件对招募、运力、价格、道路、劳力和流民压力产生近郊影响。

## 3. 运行时架构

运行时包位于：

`Assets/StreamingAssets/WorldMap/Luoyang184MetropolitanInitializationV1`

新增包保存 130,000 人和新增家户的增量二进制、空间/设施/道路/农业/物流事实及旧包
文件哈希。组合读取器实现既有 `ILuoyang184UrbanPopulationSource`，0—269,999 号人物
和原家户直接委托旧读取器，后续序号从增量包读取；因此不会复制或改写旧人口事实。

## 4. 验收

1. 总人口恰好 400,000；新增人口恰好 130,000。
2. 原 270,000 名人物、53,992 户与旧包受保护文件 SHA-256 不变。
3. PersonId、HouseholdId、FacilityId、FamilyOrganizationId 唯一且引用有效。
4. 新增人物全部有 Household 和 Residence；岗位容量不超限。
5. Cell 不重复占用，新增道路可把全部聚落连接至至少一座既有洛阳城门。
6. 农业成熟、早收、库存和物流损耗守恒；城市仓储确实收到货物。
7. 组合读取器分块读取 400,000 人，不为每人创建 GameObject。
8. 全工程编译、核心测试、受控 Unity EditMode/PlayMode、`git diff --check` 和差异审阅
   分别报告；任何未运行项不得写成通过。

## 5. 交付物

正式审计表和报告输出到：

`outputs/LUOYANG_184_METROPOLITAN_INITIALIZATION_V1`

包括空间规划、人口物化、人物分卷、家户、家族组织、居住岗位分卷、设施、道路物流、
农业供应链、总审计、初始化报告和最终审计报告。

## 6. 状态

状态：已完成（2026-08-10）。第 4 节数据、编译、核心测试、Unity EditMode/PlayMode、
差异与工作簿证据均已产生，详见
`outputs/LUOYANG_184_METROPOLITAN_INITIALIZATION_V1/12_LUOYANG_184_METROPOLITAN_INITIALIZATION_V1_AUDIT.md`。
本任务未提交、未推送。
