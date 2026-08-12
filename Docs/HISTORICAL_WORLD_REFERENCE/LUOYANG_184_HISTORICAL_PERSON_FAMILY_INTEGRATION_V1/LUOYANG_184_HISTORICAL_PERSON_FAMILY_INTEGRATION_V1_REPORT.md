# 洛阳184历史人物—家族正式接入报告 V1

## 1. 结论

本阶段已把25名历史人物绑定到现有40万永久人物中的同一P-ID，并把Clan、Branch、Household、FamilyOrganization、FamilyCenter、Civil/Military Office和当前活动接入V69统一世界状态。开局规模保持`400000 Person / 80899 Household / 2084 Facility`，新增Person=0，新增Facility=0，重复历史人物=0。

15个既有FamilyOrganization全部保留。`f088`移除6条错误历史成员关系，`f036`移除4条错误历史成员关系；被移除的只是错误组织隶属，10名人物、亲属、家户、住宅和个人资产均未删除或重随机。8个都市圈生成组织对同一4项Facility的32条源主张与真实Owner/Controller冲突，运行时没有抢占或转移资产，而是保留为`UnresolvedFacilityClaimIds`。

## 2. 按架构层的实现

### Domain

- 新增历史身份、PersonLineage、FamilyOrganizationProfile/Member、OrganizationAsset、FamilyCenter、Civil/Military Office、PersonPrimaryActivity和CanonicalPlaceCrosswalk持久合同。
- 新增`HistoricalPersonFamilyRuntimeIndex`，按Person、Clan、组织和Facility建立索引，避免每次查询扫描40万人。
- 增加统一验证：精确P-ID、外部人口包已校验、中心激活五条件、唯一活动、官职引用和组织资产边界。
- Facility补充生命周期、外部人物分配权威和外部计数；没有新建第二套Facility事实。

### Simulation

本阶段未实现189政变、190迁都焚毁、婚育、继承、FamilySplit或全国Clan扩展。V69结构允许Facility进入Disabled/Destroyed/Abandoned，FamilyCenter进入Lost/Disabled，而FamilyOrganization继续存在。

### Persistence

- 新增`Luoyang184MetropolitanPopulationStore`，以32分区只读接入40万永久人物；不把人物复制进`WorldState.People`。
- 新增幂等Bootstrap，验证受保护包哈希后投影Place、Facility、组织、身份、Lineage、官职、活动和Deferred中心。
- 存档版本从V68顺序迁移至V69；新增集合由迁移器初始化，旧世界不凭空获得洛阳事实。
- 初始化包不可直接Commit；后续出生、死亡、迁居、就业和资产变化必须进入派生检查点/覆盖层。

### Content

- 未改写`Luoyang184UrbanInitializationV1`或`Luoyang184MetropolitanInitializationV1`。
- 25名人物沿用历史人物母库与184覆盖包；2,084项Facility沿用稳定ID、Cell、Owner/Controller和容量。
- `capability.family_management`成为稳定数据ID合同，但本阶段没有给任何Facility强行添加该能力。

### Presentation

洛阳世界验证页可选择刘宏、何进、曹操，并显示同一世界中的历史身份、Clan/Branch、Household/Residence、FamilyOrganization、Office、Workplace、Activity和FamilyCenter状态。界面没有生成40万Person或2,084个Facility GameObject。

### Validation

- 数据审计：PASS，受保护规模与哈希合同保持。
- Unity EditMode：10/10通过；PlayMode：1/1通过。
- 完整核心回归和最终差异校验以本目录`validation_summary.json`为唯一最终证据。

## 3. 迁移结果

| 项目 | 结果 |
|---|---:|
| 历史人物精确映射 | 25 |
| 新增Person / 重复历史人物 | 0 / 0 |
| 保留FamilyOrganization | 15 |
| 城内迁移纠正 / 都市圈保留 | 7 / 8 |
| 移除错误组织成员关系 | 10 |
| FamilyOrganizationMember | 2,318 |
| Primary / Local Active Center | 0 / 0 |
| Deferred Center | 15 |
| 未决Facility主张 | 32 |
| Civil/Military Office Assignment | 8 |
| 新增Facility | 0 |

## 4. 未解决问题

1. 8个都市圈生成组织共享4项Facility的32条源主张与Facility权属不一致；需要独立资料修正或Estate/社区组织归属协调，不能在本任务中猜测。
2. 15个组织均没有同时满足能力、权属、指定、Manager和活动的真实FamilyCenter；这不是失败，而是诚实的Deferred状态。
3. 当前正式人口包是只读初始化源。长期生活模拟需要派生可写检查点/覆盖层，保持同一永久PersonId并记录变更来源。
4. 40万人与2,084设施尚未进入完整的Residence→Work→Production→Consumption→Market→Supply日常闭环。

## 5. 30项必答

1. 25名HistoricalPerson是否全部接入现有Person：是，25/25精确绑定同一P-ID。
2. 是否新增Person：否，0。
3. 是否出现重复历史人物：否，0。
4. Clan是否正式接入：是，作为PersonLineage独立关系接入。
5. Branch是否正式接入：是；有证据则绑定，无证据保持空值。
6. 旧Clan/Family冲突如何迁移：只纠正组织成员关系；f088移除6条、f036移除4条，人物、家户、亲属和资产不变；Clan/Branch不再与Household/Organization混用。
7. 现有FamilyOrganization保留多少：15。
8. 迁移多少：15个均进入V69；其中7个城内组织执行稳定ID纠正迁移，8个都市圈组织保留并登记未决Facility主张。
9. 新增多少FamilyOrganization：0。
10. 哪些拥有Primary Center：无。
11. 哪些拥有Local Center：无。
12. 哪些仍无Center：全部15个，状态为Deferred/None。
13. FamilyCenter是否依赖真实Facility：是，验证器强制。
14. 是否新增FamilyManagement Capability：新增稳定能力ID合同；当前实际Facility赋予数量为0。
15. 是否修改Facility核心架构：扩展了通用FacilityState的生命周期、外部人员分配权威和计数元数据；没有重建或复制Facility事实。
16. 是否新增Facility：否，0。
17. HistoricalPerson Household是否全部合法：是，25/25解析到现有80,899户。
18. Residence是否全部合法：是，25/25解析到现有2,084项Facility。
19. 住宅容量是否守恒：是；40万人仍全部按原二进制索引安置，容量未改写。
20. 个人资产是否错误转族产：否。
21. FamilyOrganization资产是否独立Ledger：是；32条冲突设施主张未转换成资产。
22. Historical Office是否接入Civil/Military Office：是，共8项有效任命。
23. Office是否有真实Jurisdiction/Facility关系：是；辖区为Canonical洛阳，工作场所均为既有Facility。源工作索引为空时只采用同类既有宫殿/官署/营房的明确重建回退。
24. Family Manager是否遵守CurrentActivity：是，Active Center验证必须由同一Manager的唯一Active活动支撑；当前无满足条件者。
25. Save/Load是否保持映射：是，V69往返、V68迁移、确定性和二次接入幂等均有自动测试。
26. 400K/80,899/2,084是否保持：是。
27. 184新结构是否兼容未来190：结构上兼容Facility毁坏/废弃和Center失效/丢失，组织可继续存在；190事件执行器尚未实现。
28. 虎牢/函谷是否仍Deferred：是，未处理。
29. 真实Blocker：32条都市圈设施权属冲突、15个中心缺少五条件、缺少可写派生人口检查点，以及尚未接通40万人生活经济闭环。
30. 下一阶段优先开发：建立可写派生检查点/覆盖层，并把40万Person与2,084 Facility接入Residence→Work→Production→Consumption→Market→Supply的确定性闭环。
