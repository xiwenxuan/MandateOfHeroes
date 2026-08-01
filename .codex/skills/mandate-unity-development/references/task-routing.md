# Task and design routing

Use this index to select the smallest relevant set of project documents. Do not read every design file by default.

## Priority and conflicts

- The user's current request and the active development plan determine priority.
- Milestone numbers such as M7, M11, and M12 are identifiers, not automatic priority.
- `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` is the source for current system status, cross-system relationships, the global recommended build order, and production/construction/research rules.
- Domain documents own their detailed rules and internal dependency order. Their `P1`, `P2`, or stage labels do not override the global build order.
- `AGENTS.md` contains hard repository rules. This routing file selects context; it does not override those rules.
- If two design documents materially conflict, stop implementation and report the exact conflict instead of silently choosing one.
- If a referenced document does not exist, report that fact and inspect nearby sources rather than inventing its contents.

## Domain routes

| Work area | Read first | Add when relevant |
| --- | --- | --- |
| Overall vision and gameplay | `Docs/GAME_VISION_AND_GAMEPLAY.md` | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` for current status and cross-system scope |
| System inventory, implementation status, technical debt, or next-milestone planning | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` | The governing domain design; use `Docs/DEVELOPMENT_PLAN.md` and `Docs/PREPRODUCTION_BACKLOG.md` only when auditing historical plans |
| Production, construction, agriculture, industry, technology, or research | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` | `Docs/WORLD_SIMULATION_FOUNDATION.md`, then the affected profession or content design |
| World simulation, economy, governance, facilities, finance, or local conflict | `Docs/WORLD_SIMULATION_FOUNDATION.md` | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` for current status; `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` for the accepted cross-system map, information, resource-creation, construction, and delegation contract; `Docs/DATA_AND_CONTENT_FOUNDATION.md` for authored data |
| Population, households, permanent identity, attention, event scheduling, partitioned storage | `Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md` | `Docs/TASK_M15_PERMANENT_POPULATION_STORAGE_BENCHMARK.md` for the active storage prototype; `Docs/WORLD_SIMULATION_FOUNDATION.md`, `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md`, `Docs/HISTORICAL_POPULATION_135_260.md`; treat `Docs/TASK_M7_POPULATION_LEDGER.md` as an earlier implementation record |
| Historical population data, 140 commanderies, or stable geographic mapping | `Docs/TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md` | `Docs/HISTORICAL_POPULATION_135_260.md`, `Docs/DATA_AND_CONTENT_FOUNDATION.md`, `Docs/HISTORICAL_CITY_LIST.md`, `Docs/CITY_UNION_MASTER.md` |
| Save compatibility, deterministic simulation, stable IDs or random streams | `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md` | [persistence.md](persistence.md) and the affected domain design |
| Combat, armies, authority and warfare | `Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md` | `Docs/TASK_M10_REAL_MILITARY_SERVICE_AND_COMMAND.md`, `Docs/TASK_M11_EQUIPMENT_ARMORY_AND_TROOP_DERIVATION.md`; add `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` when dynamic intelligence, map knowledge, theater delegation, or fortification construction is affected |
| Character attributes, traits, family and growth | `Docs/CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md` | `Docs/TASK_M8_CHARACTER_ABILITY_FOUNDATION.md`, `Docs/TASK_M9_EDUCATION_AND_PRACTICE.md` |
| Sandbox NPC AI | `Docs/SANDBOX_NPC_AI.md` | The M12 document whenever attention, scheduling, permanent population, or storage is affected; add `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` for limited knowledge, information work, autonomous site selection, or recursive delegation |
| Maps, cities and geography | `Docs/PROTOTYPE_MAP_184_ZHUO_GUANGZONG.md` | `Docs/WORLD_SIMULATION_FOUNDATION.md` and `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` for continuous scale, semantic views, knowledge gating, resource creation, or dynamic facilities; `Docs/MAP_ART_RESOURCE_PLAN.md`, `Docs/HISTORICAL_CITY_LIST.md`, `Docs/CITY_UNION_MASTER.md` for visual and authored geography |
| Historical events and people | `Docs/DATA_AND_CONTENT_FOUNDATION.md` | The matching `Docs/HISTORICAL_*.md` documents |
| External assets and licenses | `Docs/LEGAL_AND_ASSETS.md` | [content-and-data.md](content-and-data.md) |

## Task document role

Task documents define a bounded milestone, acceptance criteria, and implementation record. They must link to the governing design instead of copying repository-wide rules. When a completed task becomes stale, preserve it as history and update the governing design or this routing index.

## Core reading order

For cross-system design or planning, use the order established by the master document:

```text
GAME_VISION_AND_GAMEPLAY
→ GAME_SYSTEMS_MASTER_AND_STATUS
→ WORLD_SIMULATION_FOUNDATION
→ CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH / UNIFIED_COMBAT_WARFARE_AND_AUTHORITY
→ TASK_M12_PERMANENT_POPULATION_AND_ATTENTION
→ HISTORICAL_POPULATION_135_260
```

Do not load all seven documents for a narrow implementation or defect. Start from the relevant route and add the master only when status, priority, or cross-system behavior is in scope.
