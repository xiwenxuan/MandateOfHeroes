# City Development Pack Upgrade Protocol V1

## Canonical规则

当用户要求“把Place X做细”或“升级某城市”时，第一步必须创建或升级Development Pack，不能直接写Unity代码、摆Facility、生成人口、画城市、创建FamilyOrganization/FamilyCenter或生成AI。

## 标准流程

0. 接收用户的地点细化请求。
1. Resolve Canonical Place：分开StrategicLabel、AdministrativeRegion与CanonicalPhysicalPlace；显示名不得直接创造新Place。
2. Check Existing Pack：没有则CREATE PACK，已有则UPGRADE PACK。
3. 提出目标深度候选D3/D4/D5，但不更改Roster。
4. 运行人口、空间、人物、Clan/Family、Facility、产业、交通、军事、Scenario、ChangePoint资料缺口审计。
5. 补最小必要历史资料，保留HISTORICAL/RECONSTRUCTED/MODELED/UNKNOWN。
6. 生成或升级Pack。
7. 按Standard验收Pack。
8. 更新DevelopmentPlaceRoster、Development Manifest与Knowledge Base；仅登记建议。
9. 用户/开发计划明确确认DevelopmentDepth变化。
10. 才允许进入独立Runtime / Cell / Facility / Population / HistoricalPerson / FamilyOrganization / Unity任务。

## D0/D1与Roster

任何合法CanonicalPlace原则上`EligibleForUpgrade=true`。D0/D1只表示当前无专项制作计划；72个Roster是V1计划，不是永久白名单。未解析为CanonicalPlace的战略Label或`geo.site`参考，须先完成物理Place解析。

## 稳定世界与存档

- 升格不得换PlaceId、重生人口、重随机Person、重建已有Facility或改写历史行政关系。
- 既有Person、Household、Facility、Cell、Inventory继续存在；只增加经审计的Reference和开发内容。
- Scenario创世缺失的历史事实须另开Initialization Correction / Migration任务。
- 游戏运行中新增建设必须来自真实Construction，不得因DevelopmentDepth变化凭空出现。
- Pack升级不自动修改已有存档。

## 决策边界

Pack Ready与Depth Upgrade是两个独立门。Pack完成不自动升格，深度建议不自动改变Wave；二者均等待用户/开发计划确认。
