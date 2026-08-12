# 洛阳历史世界状态的开发含义

- 184、189、190前、190后、220—223不是不同Unity场景，而是同一Place/Cell/Facility集合上的状态投影。
- 190迁都与焚毁若在运行世界满足前提并发生，必须后台提交人口、家庭、官署、库存、设施和控制权变化。
- 史料只证明大规模变化时，普通设施不得标记为`HISTORICAL_DESTROYED`；应使用`MODELED`或`UNKNOWN`。
- 北宫、南宫、中央官署、太仓、武库、市场、城门等先做Facility级证据审计，再决定Damage/Destroyed/Abandoned/Rebuilt。
- 直接194/223开局可使用相应历史Snapshot；从184连续游玩则保留玩家与AI造成的真实分歧。
- 本目录只提供Reference，不修改现有洛阳184运行时数据或Save Schema。
