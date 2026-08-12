# 洛阳184历史人物—家族接入：存档兼容与迁移报告

## 结论

存档合同已由V68顺序升级到V69。旧存档迁移只初始化新增集合，不会凭空物化洛阳40万人、历史人物、家族组织或Facility；正式洛阳接入由显式、幂等Bootstrap完成。

## V69新增持久内容

- CanonicalPlace crosswalk
- Historical identity与PersonLineage
- FamilyOrganization profile/member与Organization asset
- FamilyCenter
- Civil/Military Office definition/assignment
- Person primary activity
- Generic Facility definition/state和外部人口包接入元数据
- Historical-person-family integration receipt

## 兼容原则

1. V68→V69只有一条顺序迁移路径。
2. 受保护400K初始化包由包ID、规模和SHA-256组合摘要校验；不内联复制40万Person。
3. `PersonId`、`HouseholdId`、`FacilityId`、`CellId64`和15个组织ID保持稳定。
4. 缺失内容ID不得静默改指；Facility主张冲突保留为未决记录。
5. Bootstrap二次执行只验证并返回`WasAlreadyIntegrated=true`，不重复追加。
6. 未来变更写入派生检查点/覆盖层，不反写受保护开局包。

## 验证覆盖

- V69序列化→反序列化→再序列化字节级JSON等价。
- V68世界迁移到V69且不虚构新集合内容。
- 25项历史映射、15个组织、15个中心、官职和活动往返保留。
- 相同输入重复接入结果确定，第二次接入幂等。
- 包引用校验保证40万Person、80,899 Household、2,084 Facility仍是同一事实源。

## 已知后续工作

当前适配器有意只读。要让同一40万人在长期运行中出生、死亡、迁居、换岗、消费和持产，下一阶段必须实现按分区持久的派生变化日志/检查点，并继续遵守永久身份、可追溯变化和存档迁移规则。
