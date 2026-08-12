# LUOYANG-POPULATION-STRESS-V1 完成报告

## 1. 最终结论

**PASS WITH KNOWN LIMITATIONS**

五档真实永久 Person、365 日固定/自适应推演、身份合法住房、岗位索引、真实 Facility 建设、二进制随机访问和 Unity 调试入口均完成。限制是项目既有全套核心与全套 EditMode 在规定的 300 秒内未完成；250K/500K 均暴露当前洛阳范围土地耗尽。

## 2. Profile总表

| Profile | Person | Facility | Added | Housing | Housed/Unhoused | Jobs | Employed | Cell use | RSS MB | Daily tick ms | Save/Load ms |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 20K | 20,542 | 1,257 | 27 | 25,464 | 20,414 / 128 | 37,628 | 11,582 | 21.90% | 100.8 | 0.006 | 49.917 / 12.285 |
| 50K | 50,000 | 1,548 | 318 | 46,104 | 46,104 / 3,896 | 51,638 | 19,579 | 26.97% | 102.4 | 0.012 | 97.752 / 20.362 |
| 100K | 100,000 | 2,163 | 933 | 92,064 | 92,064 / 7,936 | 76,448 | 20,673 | 37.68% | 103.9 | 0.023 | 172.526 / 34.395 |
| 250K | 250,000 | 5,740 | 4,510 | 225,384 | 225,384 / 24,616 | 218,728 | 45,252 | 100.00% | 110.3 | 0.058 | 408.644 / 79.303 |
| 500K | 500,000 | 5,740 | 4,510 | 298,464 | 298,464 / 201,536 | 202,158 | 63,452 | 100.00% | 110.8 | 0.115 | 779.049 / 151.471 |

## 3. Fixed Infrastructure Mode

不扩建时未安置人口依次为 128、24,536、74,536、224,536、474,536。原有设施与 Cell 完全不变；住房、就业、粮食、仓储、市场等缺口作为世界事实保留。

## 4. Adaptive Construction Mode

| Person | Residential | Agriculture | Industry | Commercial | Warehouse | Military | Other | Total added |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 20,542 | 0 | 0 | 0 | 27 | 0 | 0 | 0 | 27 |
| 50,000 | 172 | 0 | 0 | 143 | 3 | 0 | 0 | 318 |
| 100,000 | 555 | 3 | 0 | 341 | 34 | 0 | 0 | 933 |
| 250,000 | 1,666 | 562 | 0 | 898 | 126 | 0 | 1,258 public | 4,510 |
| 500,000 | 2,275 | 1,017 | 20 | 1,003 | 195 | 0 | 0 | 4,510 |

每栋新增建筑都有唯一 Facility、唯一未占用 Cell、Owner/Controller，以及 Planned → Approved → UnderConstruction → Completed 记录。

## 5. AI建设原因

原因来自每次建设前重算的压力。250K：Housing 1,666、Food 562、Market 898、Storage 126、Skill 1,258；500K：Housing 2,275、Food 1,017、Market 1,003、Storage 195、Employment 20。MilitaryPressure 未超过阈值，故没有按人口机械补军营。

## 6. 250K重点结果

- Person 250,000；Facility 5,740；新增 4,510。
- HousingCapacity 225,384（民居 224,184、军营 1,200）；Housed/Unhoused 225,384 / 24,616；军营居民 1,200。
- EligibleWorkers/Jobs/Employed/OpenJobs/Unemployed：152,088 / 218,728 / 45,252 / 173,476 / 106,836。岗位与失业并存来自职业/技能不匹配，不做无资格塞岗。
- Food demand/production/deficit：91,250,000 / 83,970,000 / 7,280,000；Storage 21,560,000。
- Residential/Agriculture/Industry/Commercial/Military Cells：1,906 / 933 / 74 / 953 / 57；Total 5,740，利用率 100%。
- Person binary 17.17 MiB；RSS 110.3 MiB；daily tick 0.058 ms；save/load 408.644 / 79.303 ms。

## 7. 500K极限结果

- Person 500,000；Facility 5,740；新增 4,510；365 日完整完成，未 Deferred。
- HousingCapacity 298,464（民居 297,264、军营 1,200）；Housed/Unhoused 298,464 / 201,536；军营居民 1,200。
- EligibleWorkers/Jobs/Employed/OpenJobs/Unemployed：304,527 / 202,158 / 63,452 / 138,706 / 241,075。
- Food demand/production/deficit：182,500,000 / 124,920,000 / 57,580,000；Storage 31,220,000。
- Residential/Agriculture/Industry/Commercial/Military Cells：2,515 / 1,388 / 94 / 1,058 / 57；Total 5,740，利用率 100%。
- Person binary 34.33 MiB；RSS 110.8 MiB；daily tick 0.115 ms；save/load 779.049 / 151.471 ms。

## 8. Job Matching

10,000 次职业索引匹配：20K 1.041 ms、100K 1.073 ms、250K 1.089 ms、500K 1.054 ms；500K/20K 1.01。每次只访问职业桶，不执行 Person×Facility；实际 Person 工作引用按设施容量审计。

## 9. Housing Assignment

10,000 次住房索引变更：20K 0.465 ms、100K 0.593 ms、250K 0.482 ms、500K 1.676 ms；500K/20K 3.60。平民与现役军人使用分离的合法 Facility 槽；新增住宅、毁坏、入营与退伍由 Domain 测试覆盖。

## 10. Person Query

10,000 次二进制定址读取：20K 18.946 ms、100K 34.968 ms、250K 38.995 ms、500K 40.883 ms。记录偏移为 Header + index × 72，O(1) 定址。

## 11. Simulation LOD

| Person | PermanentPerson | LowFrequency | MediumFrequency | HighFrequency |
|---:|---:|---:|---:|---:|
| 20,542 | 20,542 | 15,286 | 5,000 | 256 |
| 100,000 | 100,000 | 94,744 | 5,000 | 256 |
| 250,000 | 250,000 | 244,744 | 5,000 | 256 |
| 500,000 | 500,000 | 494,744 | 5,000 | 256 |

PermanentPerson 是全量持久记录；后三列是调度/表现层级。Unity 最大可视 Actor 为 256，不改变人物事实。

## 12. Facility增长曲线

20,542→1,257、50K→1,548、100K→2,163、250K→5,740、500K→5,740。FacilityCount 等于 OccupiedFacilityCells；250K 起达到当前 5,740 个 Facility Cell 上限。

## 13. AI稳定性

没有无限循环、决策震荡、负财政、无劳力仍无限扩张或覆盖历史设施。250K/500K 土地耗尽；500K 仍有住房、粮食、仓储、市场、技能和就业压力。岗位空缺与失业并存会推动训练/职业结构响应，而不会忽略资格强行就业。

## 14. 住宅和Facility Capacity参数

当前参数适合验证算法，不是最终平衡。120 人/城市住宅 Cell 在 250K 已占用大量土地，军营固定 1,200 容量很快饱和；后续需结合 2,000 米 Cell 内部容量语义、复合设施和城区范围校准。

## 15. 2000m Cell结论

**Valid With Capacity / Algorithm Rebalancing**。

2,000 米格网仍可表达唯一占地、历史设施保护和跨层定位；硬约束来自当前洛阳范围的 4,510 个剩余开发位与容量参数。证据不支持立即改为 1,000 米或建立 SubCell。

## 16. 内存

紧凑 Person＋索引估计 1.0681 MiB/10K、10.681 MiB/100K，线性估算 1M 为 106.81 MiB；进程 RSS 从 20K 的 100.8 MiB 到 500K 的 110.8 MiB。RSS 包含 Python、世界 JSON 与生成器，不等于 Person 独占内存。

## 17. Save / Load

固定 72 字节记录：20K 1,479,056 B，50K 3,600,032 B，100K 7,200,032 B，250K 18,000,032 B，500K 36,000,032 B。五档顺序全量读回，计数、唯一序列、历史核心事实、住房身份、设施容量和岗位容量一致。

## 18. Unity

洛阳场景可切换五档与 Fixed/Adaptive；250K/500K 地图、LOD 与 Debug 统计由 PlayMode 验证。专项 EditMode 4/4、洛阳 PlayMode 1/1 通过；全量 EditMode 在 300 秒内未完成。

## 19. 回归测试

- 全工程编译：PASS。
- Python：PASS；五档全量二进制、历史事实、Facility/Cell、建设生命周期、住房/岗位容量和十二报告审计通过。
- 原核心测试：INCOMPLETE；300 秒边界，无完整结果。
- 新增核心测试：PASS 5/5。
- EditMode：专项 PASS 4/4；全量 INCOMPLETE（300 秒）。
- PlayMode：洛阳专项 PASS 1/1。
- `git diff --check`：PASS；Unity 残留进程：0。

## 20. 已知瓶颈

土地、住宅、粮食、仓储与市场在 250K 后成为约束；职业/技能不匹配造成空岗与失业并存。全量项目回归超过 300 秒，需要既有测试分组。压力二进制仍是独立原型包，尚未升级正式世界存档或实现 500K 人全部同日高频 AI。

## 21. 下一步建议

先校准 2,000 米 Cell 内住宅、农业、市场、仓储和军营容量及城区边界，再把压力包适配正式分区人口仓储；同时用现有分组入口补齐全量回归。不得在没有新证据前改全国格网或建立 SubCell。
