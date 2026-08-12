# HAN 135—260 Administrative Seat / Canonical Place / Historical World State V1 Report

## Outcome

本轮完成了行政区—治所角色—物理地点—历史状态的Reference层交叉；没有修改运行时代码、Save Schema或Unity场景。

## 30项交接回答

1. 13州×13 Scenario形成169条稀疏时间轴解析记录；未知/割据阶段保持UNKNOWN，不强填唯一州治。
2. 105/105郡国等价单位均有候选治所映射；它们主要来自郡国志县序的保守重建，不等于105项均已专题考证。
3. 250重点县中133项治所已解析到既有Core Settlement；其余保持UNKNOWN。
4. 77战略名称中`PLACE_NAME_DIRECT`为28项。
5. `ADMIN_REGION_AS_STRATEGIC_LABEL`为31项。
6. `PLACE_RENAME_TIMELINE`为5项。
7. `MOVING_SEAT_REGION_LABEL`为2项。
8. `STRATEGIC_SETTLEMENT_NOT_MAJOR_SEAT`为11项。
9. 77项当前交叉到75个既有CanonicalPlace；城阳/北海与金城/西平暴露两组重复映射风险，不新增Place掩盖冲突。
10. 133 Core Settlements全部进入13 Scenario交叉，共1729条角色记录；覆盖全部105郡国候选治所。
11. 汉中、北海、汝南、会稽、河内、河东、天水、南海、交趾等首先是行政/战略显示名，不应自动理解为同名固定城市。
12. 城阳/北海、金城/西平、江夏、庐江、公安、建安、梓潼/涪最容易造成重复或错误建城。
13. 133个重要Place×13 Scenario共1729条Snapshot索引；数据为Reference，不是复制地图。
14. 共识别32个Major Historical ChangePoint候选。
15. 最高地图开发价值包括洛阳190、长安190—196、许196、襄阳/江陵208—223、成都214/221、建业211/229、武昌221和永安222—223。
16. 洛阳需准备184基线/动员、189政变前后、190迁都焚毁前后、220—223恢复/魏都状态。
17. 长安需准备190朝廷迁入、192—195控制危机、195—196朝廷东归状态。
18. 邺需准备204控制中心、210大型设施建设、220政权转换状态。
19. 许需准备196朝廷迁入与220—221汉魏转换/名称时间线状态。
20. 成都需准备188—190州治转移、214易主、221—223政权首都状态。
21. 襄阳需准备190州治中心、208接管、219襄樊战区状态。
22. 江陵需准备208—209控制转换、219接管、222—223夷陵后区域军事状态。
23. 建业需准备211—212秣陵/建业时间线和政治中心、229吴都状态。
24. 洛阳190、长安迁都期、许196、邺大型建设、成都/建业/武昌首都建设、襄樊战事等需要Cell/Facility/Transport状态评估。
25. 单纯官职、人物在场、政权称号或没有空间后果的事件只使用普通Event/Person/Office变化，不创建地图ChangePoint。
26. 每代系列出现标志尚无许可兼容的逐作来源；77个既有战略标签已全部保留交叉槽，禁止凭记忆伪填。
27. DevelopmentRelevantPlaceCandidate共236项，包含既有Core、交通、军事空间、战略点和Estate候选；不等于最终Roster。
28. 争议集中于7个战略映射、移动治所、105郡国候选治所证据等级、洛阳190具体设施结果与系列逐代出现情况。
29. 后续运行时需要CanonicalPlace/AdministrativeRegion分离、RuntimeSeat、历史事件前提、事务ChangePackage、离屏结算及存档恢复。
30. 已具备进入`DEVELOPMENT-PLACE-ROSTER-AND-REFERENCE-READINESS-V1`的资料条件，但最终城市/据点数量和A/B/C分级仍必须在该任务决定。

## Validation targets

- 13州、105郡国、1182县ID、77标签、133聚落、250重点县与13 Scenario全部纳入审计；
- 32个ChangePoint与160条Reference Package交叉；
- 洛阳关键Facility保持原ID，普通设施结果不伪装为史实；
- 系列参考未导入商业数据库、坐标、数值、UI、美术或剧本文本；
- 运行时代码与存档均未改变。
