# 06 多Cell建设蓝图设计

当前蓝图：`blueprint.fortification.han_city_gate_segment.v1`，5 Cell。

每个蓝图保存稳定ID、相对Cell、FacilityDefinitionId、方向、道路连接、模块、施工阶段、顺序和元数据。放置器统一校验Cell存在、可开发、Owner、占用和道路连接；玩家、历史生成器与AI共用同一模板。放置成功只建立预约/阶段计划，不代表瞬间完工。本阶段不做完整蓝图UI。
