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
| Production, construction, agriculture, industry, technology, or research | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` | `Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md` for data-driven crops, products, recipes, production execution, regional economy, and profession progression; `Docs/TASK_M17_P0_DATA_DRIVEN_PRODUCTION_CONTENT_CONTRACT.md` for the completed M17 content-contract foundation; `Docs/TASK_M18_P0_CHARACTER_SKILL_KNOWLEDGE_RESEARCH_RECIPE_BRIDGE.md` for the V9 skill, knowledge, research, local technology application, and production-order snapshot bridge; `Docs/TASK_M19_P0_PRODUCT_BATCH_INVENTORY_AND_PROCESSING_CHAIN.md` for the V10 product/seed batches, inventory transactions, legacy grain adapter and first processing chain; `Docs/TASK_M23_P0_MILITARY_PROCUREMENT_TRANSPORT_AND_ARMORY_RECEIPT.md` for the V14 equipment products, organization caravan inventory, procurement payment, transport and armory receipt slice; add `Docs/WORLD_SIMULATION_FOUNDATION.md`, then the affected profession or content design |
| World simulation, economy, governance, facilities, finance, or local conflict | `Docs/WORLD_SIMULATION_FOUNDATION.md` | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` for current status; `Docs/TASK_M22_P0_COUNTY_FISCAL_GENTRY_MARKET_GOVERNANCE.md` for the V12 county fiscal ledger, gentry compliance, market pressure, relief and public-order foundation; `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` for the accepted cross-system map, information, resource-creation, construction, and delegation contract; `Docs/DATA_AND_CONTENT_FOUNDATION.md` for authored data |
| Population, households, permanent identity, attention, event scheduling, partitioned storage | `Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md` | `Docs/TASK_M15_P6_FORMAL_PARTITIONED_POPULATION_ADAPTER_AND_RESIDENCY_CONTRACT.md` for the V7 formal adapter and hot/cold extension contract; `Docs/TASK_M20_P0_ATTENTION_LOCAL_RELATIONSHIP_AND_RESIDENCY.md` for the V11 attention ledger, bounded local relationship network and residency reconciliation; `Docs/TASK_M21_P0_PERSON_REPOSITORY_AND_INCREMENTAL_CHECKPOINT.md` for the first simulation access-layer migration and partition-level incremental checkpoint; `Docs/TASK_M21_P1_POPULATION_HOUSEHOLD_AND_BIRTH_INCREMENT.md` for life/household access migration and newborn incremental persistence; `Docs/TASK_M21_P2_VILLAGE_LIFE_AND_POPULATION_LEDGER_REPOSITORY.md` for village-life and population-ledger repository migration; `Docs/TASK_M21_P3_AGRICULTURE_PRODUCTION_PERSON_REPOSITORY.md` for agriculture read access; `Docs/TASK_M21_P4_EDUCATION_PERSON_REPOSITORY.md` for education read/write tracking; `Docs/TASK_M21_P5_MEDICAL_PERSON_REPOSITORY.md` for military medicine and recovered-patient updates; `Docs/TASK_M21_P6_MILITARY_PERSON_REPOSITORY.md` for prototype enlistment, casualties, desertion and army-march person access; `Docs/TASK_M15_PERMANENT_POPULATION_STORAGE_BENCHMARK.md` for the preceding storage evidence; `Docs/WORLD_SIMULATION_FOUNDATION.md`, `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md`, `Docs/HISTORICAL_POPULATION_135_260.md`; treat `Docs/TASK_M7_POPULATION_LEDGER.md` as an earlier implementation record |
| Historical population data, 140 commanderies, or stable geographic mapping | `Docs/TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md` | `Docs/HISTORICAL_POPULATION_135_260.md`, `Docs/DATA_AND_CONTENT_FOUNDATION.md`, `Docs/HISTORICAL_CITY_LIST.md`, `Docs/CITY_UNION_MASTER.md` |
| Save compatibility, deterministic simulation, stable IDs or random streams | `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md` | [persistence.md](persistence.md) and the affected domain design |
| Combat, armies, authority and warfare | `Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md` | `Docs/TASK_M10_REAL_MILITARY_SERVICE_AND_COMMAND.md`, `Docs/TASK_M11_EQUIPMENT_ARMORY_AND_TROOP_DERIVATION.md`, and `Docs/TASK_M23_P0_MILITARY_PROCUREMENT_TRANSPORT_AND_ARMORY_RECEIPT.md` for the first persisted procurement/transport/receipt chain; add `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` when dynamic intelligence, map knowledge, theater delegation, or fortification construction is affected |
| Character attributes, traits, family and growth | `Docs/CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md` | `Docs/TASK_M8_CHARACTER_ABILITY_FOUNDATION.md`, `Docs/TASK_M9_EDUCATION_AND_PRACTICE.md`; add `Docs/TASK_M18_P0_CHARACTER_SKILL_KNOWLEDGE_RESEARCH_RECIPE_BRIDGE.md` when stable-ID skills, knowledge mastery, research, or technology application is affected |
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
