# 洛阳189/190重大历史事件运行合同原型

## 原型目的

本原型验证重大历史事件是持续世界中的条件式结构冲击，而不是年份剧情播放器。它绑定现有 Emperor Person、汉政府 Organization、洛阳 Place、宫城 Facility，以及可选的城门、FamilyCenter、Office、Route 和 Army；不会重新生成洛阳人口或设施。

## 分支

- Canonical：核心人物存活且仍在洛阳、政府存在、宫城可运行。
- Transformed：核心人物存活但已离开洛阳，政治冲击迁移/转形。
- Variant：核心人物已死亡但政府结构仍存在，产生替代危机。
- Prevented：宫城等核心条件已经失效，正史型冲击被阻止。
- Delayed：时间窗已进入但没有任何终局规则满足，继续 WATCHING/DELAYED；超过窗后 EXPIRED。

## ChangePackage

操作可原位摧毁 Facility、迁移 Person、使 FamilyCenter 失效但不删除 Organization、迁移 Office、改变 Route 安全和动员 Army。缺失或已摧毁目标按幂等规则安全处理；每个操作 ID 只应用一次并持久化。

## 验证

自动测试覆盖 Canonical、Prevented、Delayed、Transformed、离屏执行、设施真实变化、已毁目标安全处理及保存后不重复。该原型不是完整189/190史实人物、军队、火灾、迁都、市场和家庭后果内容包。
