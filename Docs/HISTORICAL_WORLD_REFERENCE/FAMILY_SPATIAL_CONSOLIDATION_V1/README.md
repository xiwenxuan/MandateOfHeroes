# Historical Family Spatial Consolidation V1

## Document Governance

- Purpose：提供133核心聚落、250重点县、重要Clan/Branch、13 Scenario及家族资产/组织/中心候选的统一查询入口。
- Authority：L3 Historical / Content Reference
- Covers：A01—A11家族空间参考及证据等级。
- DoesNotCover：运行时FamilyOrganization、Facility或Active Center状态。
- Supersedes：无
- SupersededBy：无
- RelatedCanonicalDocs：../../FAMILY_ORGANIZATION_REFERENCE_V1/README.md|../../GAME_SYSTEMS_MASTER_AND_STATUS.md
- Status：HISTORICAL_REFERENCE

## Reading order

1. 先读`Docs/FAMILY_ORGANIZATION_REFERENCE_V1/README.md`确认Canonical Family规则。
2. A01按地点反查；A04/A05按Clan/Branch正查；A06按Scenario查询。
3. A07分离Residence、Estate与Asset；A08/A09只是初始化候选；A10保存争议。
4. `ACTIVE_CENTER`永远不能由本资料库决定。

## Query contract

- `GetFamilySpatialReference(placeId, year)`由A01/A02/A03+A06组合回答。
- `GetClanSpatialTimeline(clanId)`由A04回答。
- `GetBranchSpatialTimeline(branchId)`由A05回答。
- UNKNOWN表示资料不足；NONE只在有反证时使用。本轮不以空白覆盖率虚构Presence。
