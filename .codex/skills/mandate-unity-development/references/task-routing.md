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
| Production, construction, agriculture, industry, technology, or research | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` | `Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md` for data-driven crops, products, recipes, production execution, regional economy, and profession progression; `Docs/TASK_M17_P0_DATA_DRIVEN_PRODUCTION_CONTENT_CONTRACT.md` for the completed M17 content-contract foundation; `Docs/TASK_M18_P0_CHARACTER_SKILL_KNOWLEDGE_RESEARCH_RECIPE_BRIDGE.md` for the V9 skill, knowledge, research, local technology application, and production-order snapshot bridge; `Docs/TASK_M19_P0_PRODUCT_BATCH_INVENTORY_AND_PROCESSING_CHAIN.md` for the V10 product/seed batches, inventory transactions, legacy grain adapter and first processing chain; `Docs/TASK_M23_P0_MILITARY_PROCUREMENT_TRANSPORT_AND_ARMORY_RECEIPT.md` for the V14 procurement slice; `Docs/TASK_M23_P1_EQUIPMENT_MANUFACTURING_REPAIR_AND_WORKSHOP.md` for V15 organization workshops, equipment manufacturing, static-store loading and repair orders; `Docs/TASK_M23_P2_UPSTREAM_RESOURCE_EXTRACTION_AND_PRIMARY_PROCESSING.md` for V16 resource bodies, real-worker extraction, charcoal and bloomery processing; `Docs/TASK_M23_P3_LIVESTOCK_SLAUGHTER_TANNING_AND_HORN.md` for V17 ordinary livestock batches, husbandry, slaughter, tanning and horn byproducts; `Docs/TASK_M23_P4_MULTIDIMENSIONAL_QUALITY_AND_ARTISAN_GROWTH.md` for V18 data-driven quality dimensions, production skill snapshots, artisan practice growth and audit ledger; `Docs/TASK_M23_P5_MILITARY_LOGISTICS_ACQUISITION_PROVISIONS_AND_LOSS.md` for V19 supply acquisition methods, carrier responsibility, separate convoy provisions, natural transit loss and military freight audit; `Docs/TASK_M23_P6_MULTI_LEG_LOGISTICS_HANDOFF_AND_PARTIAL_RECEIPT.md` for V20 persisted route legs, co-located custody handoff, downstream provision reservations and partial final receipt; `Docs/TASK_M23_P7_ESCORT_TRANSIT_RISK_AND_CARGO_SEIZURE.md` for V21 real escorts, deterministic transit incidents, hostile cargo seizure custody and audit; `Docs/TASK_M23_P8_LOGISTICS_CLASH_INJURY_AND_CARGO_RECOVERY.md` for V22 real-person logistics clashes, injuries and same-route army cargo recovery; `Docs/TASK_M23_P9_MILITARY_LOGISTICS_DELEGATION_AND_EXCEPTION_REPORTING.md` for V23 military supply goals, carrier offers, deterministic preference selection, budget limits and exception reports; `Docs/TASK_M23_P10_MILITARY_LOGISTICS_SCHEDULING_OFFER_LIFECYCLE_AND_REPORTING.md` for V24 due scheduling, offer withdrawal/expiry and freight progress/completion reports; `Docs/TASK_M23_P11_BOUNDED_HIERARCHICAL_MILITARY_LOGISTICS_DELEGATION.md` for V25 bounded parent-child goal decomposition, inherited budgets and bottom-up completion; `Docs/TASK_M23_P12_FAILED_SUBGOAL_CANCELLATION_REALLOCATION_AND_REASSIGNMENT.md` for V26 uncommitted subgoal cancellation, allocation recovery, offer closure and replacement history; `Docs/TASK_M23_P13_ACTUAL_RECEIPT_SHORTFALL_AND_SUPPLEMENTAL_FREIGHT.md` for V27 actual receipt, outstanding demand, sequential supplemental freight and cumulative budget audit; `Docs/TASK_M23_P14_CARRIER_LIABILITY_COMPENSATION_AND_REPLACEMENT_AUTHORIZATION.md` for V28 carrier liability settlement, real compensation/arrears, net-budget restoration and seized-cargo replacement authorization; add `Docs/WORLD_SIMULATION_FOUNDATION.md`, then the affected profession or content design |
| World simulation, economy, governance, facilities, finance, or local conflict | `Docs/WORLD_SIMULATION_FOUNDATION.md` | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` for current status; `Docs/TASK_M22_P0_COUNTY_FISCAL_GENTRY_MARKET_GOVERNANCE.md` for the V12 county fiscal ledger, gentry compliance, market pressure, relief and public-order foundation; `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` for the accepted cross-system map, information, resource-creation, construction, and delegation contract; `Docs/DATA_AND_CONTENT_FOUNDATION.md` for authored data |
| Population, households, permanent identity, attention, event scheduling, partitioned storage | `Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md` | `Docs/TASK_M15_P6_FORMAL_PARTITIONED_POPULATION_ADAPTER_AND_RESIDENCY_CONTRACT.md` for the V7 formal adapter and hot/cold extension contract; `Docs/TASK_M20_P0_ATTENTION_LOCAL_RELATIONSHIP_AND_RESIDENCY.md` for the V11 attention ledger, bounded local relationship network and residency reconciliation; `Docs/TASK_M21_P0_PERSON_REPOSITORY_AND_INCREMENTAL_CHECKPOINT.md` for the first simulation access-layer migration and partition-level incremental checkpoint; `Docs/TASK_M21_P1_POPULATION_HOUSEHOLD_AND_BIRTH_INCREMENT.md` for life/household access migration and newborn incremental persistence; `Docs/TASK_M21_P2_VILLAGE_LIFE_AND_POPULATION_LEDGER_REPOSITORY.md` for village-life and population-ledger repository migration; `Docs/TASK_M21_P3_AGRICULTURE_PRODUCTION_PERSON_REPOSITORY.md` for agriculture read access; `Docs/TASK_M21_P4_EDUCATION_PERSON_REPOSITORY.md` for education read/write tracking; `Docs/TASK_M21_P5_MEDICAL_PERSON_REPOSITORY.md` for military medicine and recovered-patient updates; `Docs/TASK_M21_P6_MILITARY_PERSON_REPOSITORY.md` for prototype enlistment, casualties, desertion and army-march person access; `Docs/TASK_M24_P0_ONE_MILLION_FIFTY_YEAR_DEMOGRAPHIC_WORLD.md` and its report for the one-million-person demographic baseline; `Docs/TASK_M24_P1_MILLION_SUBSISTENCE_LAND_AND_PRESSURE_LOOP.md` and its report for household food need, fixed county land, agricultural labor, food conservation and traceable pressure-death evidence; `Docs/TASK_M24_P2_HOUSEHOLD_STOCK_MARKET_GRANARY_AND_RELIEF_TRANSPORT.md` and its report for household ownership, county markets, granaries and bounded relief transport; `Docs/TASK_M24_P3_HOUSEHOLD_LAND_SEED_PRODUCT_BATCH_AND_FARM_WORK_ORDER.md` and its report for specific household land, stable-ID agricultural bindings, seed inventories and streamed annual farm work orders; `Docs/TASK_M24_P4_POPULATION_RESOURCE_FEEDBACK_DIAGNOSIS_AND_CALIBRATION.md` and its report for annual bottleneck diagnosis, explicit failed candidates, seasonal public-land reuse and the accepted no-scripted-war calibration envelope; `Docs/TASK_M24_P5_FORMAL_PRODUCT_BATCH_AND_INVENTORY_TRANSACTION_BRIDGE.md` and its report for the formal V10-compatible compact-balance checkpoint bridge and completed-agriculture-order materialization entry; `Docs/TASK_M24_P6_MULTI_PRODUCT_FOOD_PROVENANCE_AND_FLOW_LEDGER.md` and its report for stable product-ID food vectors, full-flow provenance, per-product conservation and product-split formal checkpoints; `Docs/TASK_M15_PERMANENT_POPULATION_STORAGE_BENCHMARK.md` for the preceding storage evidence; `Docs/WORLD_SIMULATION_FOUNDATION.md`, `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md`, `Docs/HISTORICAL_POPULATION_135_260.md`; treat `Docs/TASK_M7_POPULATION_LEDGER.md` as an earlier implementation record |
| Historical population data, 140 commanderies, or stable geographic mapping | `Docs/TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md` | `Docs/HISTORICAL_POPULATION_135_260.md`, `Docs/DATA_AND_CONTENT_FOUNDATION.md`, `Docs/HISTORICAL_CITY_LIST.md`, `Docs/CITY_UNION_MASTER.md` |
| Save compatibility, deterministic simulation, stable IDs or random streams | `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md` | [persistence.md](persistence.md) and the affected domain design |
| Combat, armies, authority and warfare | `Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md` | `Docs/TASK_M10_REAL_MILITARY_SERVICE_AND_COMMAND.md`, `Docs/TASK_M11_EQUIPMENT_ARMORY_AND_TROOP_DERIVATION.md`, `Docs/TASK_M23_P0_MILITARY_PROCUREMENT_TRANSPORT_AND_ARMORY_RECEIPT.md`, `Docs/TASK_M23_P1_EQUIPMENT_MANUFACTURING_REPAIR_AND_WORKSHOP.md`, `Docs/TASK_M23_P2_UPSTREAM_RESOURCE_EXTRACTION_AND_PRIMARY_PROCESSING.md`, `Docs/TASK_M23_P5_MILITARY_LOGISTICS_ACQUISITION_PROVISIONS_AND_LOSS.md`, `Docs/TASK_M23_P6_MULTI_LEG_LOGISTICS_HANDOFF_AND_PARTIAL_RECEIPT.md`, `Docs/TASK_M23_P7_ESCORT_TRANSIT_RISK_AND_CARGO_SEIZURE.md`, `Docs/TASK_M23_P8_LOGISTICS_CLASH_INJURY_AND_CARGO_RECOVERY.md`, `Docs/TASK_M23_P9_MILITARY_LOGISTICS_DELEGATION_AND_EXCEPTION_REPORTING.md`, `Docs/TASK_M23_P10_MILITARY_LOGISTICS_SCHEDULING_OFFER_LIFECYCLE_AND_REPORTING.md`, `Docs/TASK_M23_P11_BOUNDED_HIERARCHICAL_MILITARY_LOGISTICS_DELEGATION.md`, `Docs/TASK_M23_P12_FAILED_SUBGOAL_CANCELLATION_REALLOCATION_AND_REASSIGNMENT.md`, `Docs/TASK_M23_P13_ACTUAL_RECEIPT_SHORTFALL_AND_SUPPLEMENTAL_FREIGHT.md`, and `Docs/TASK_M23_P14_CARRIER_LIABILITY_COMPENSATION_AND_REPLACEMENT_AUTHORIZATION.md` for the persisted extraction/manufacture/procurement/transport/receipt/repair chain through V28 carrier liability, compensation/arrears and seized-cargo replacement authorization; add `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` when dynamic intelligence, map knowledge, recursive theater delegation, or fortification construction is affected |
| Character attributes, traits, family and growth | `Docs/CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md` | `Docs/TASK_M8_CHARACTER_ABILITY_FOUNDATION.md`, `Docs/TASK_M9_EDUCATION_AND_PRACTICE.md`; add `Docs/TASK_M18_P0_CHARACTER_SKILL_KNOWLEDGE_RESEARCH_RECIPE_BRIDGE.md` when stable-ID skills, knowledge mastery, research, or technology application is affected |
| Sandbox NPC AI | `Docs/SANDBOX_NPC_AI.md` | The M12 document whenever attention, scheduling, permanent population, or storage is affected; add `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` for limited knowledge, information work, autonomous site selection, or recursive delegation |
| Maps, cities and geography | `Docs/PROTOTYPE_MAP_184_ZHUO_GUANGZONG.md` | `Docs/WORLD_SIMULATION_FOUNDATION.md` and `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` for continuous scale, semantic views, knowledge gating, resource creation, or dynamic facilities; `Docs/MAP_ART_RESOURCE_PLAN.md`, `Docs/HISTORICAL_CITY_LIST.md`, `Docs/CITY_UNION_MASTER.md` for visual and authored geography |
| Historical events and people | `Docs/DATA_AND_CONTENT_FOUNDATION.md` | The matching `Docs/HISTORICAL_*.md` documents |
| External assets and licenses | `Docs/LEGAL_AND_ASSETS.md` | [content-and-data.md](content-and-data.md) |

### Current M24 food-ecology extension

For multi-crop population pressure work after M24-P6, also read
`Docs/TASK_M24_P7_MULTI_CROP_FOOD_ECOLOGY_AND_NUTRITION.md` and its report. They own the
five-crop/six-food scenario extension, separate physical and nutritional ledgers, bounded county legume
rotation support, food volume/value/perishability effects and household preservation processing. The extension
is pressure-test evidence and does not by itself upgrade V28 formal partitioned content or persistence status.

### Current unified world execution kernel

For work that changes world-system ordering, due commands, cross-system transaction planning, shared resource
reservation or post-commit runtime events, read `Docs/TASK_M25_P0_UNIFIED_WORLD_EXECUTION_KERNEL.md`.
M25-P0 owns the non-persisted execution contract and the compatibility registration of existing V28 systems;
it does not by itself migrate legacy direct writes or make M24 pressure-world data part of the formal save.

### Current persistent command, result, and event-outbox contract

For work that changes commands which survive saves, command attempt/completion history, batch transaction summaries,
stable execution failure codes, post-commit event recovery or per-handler event acknowledgement, read
`Docs/TASK_M25_P7_PERSISTENT_COMMAND_RESULTS_AND_EVENT_OUTBOX.md`. M25-P7 owns the V33 persistence boundary;
it does not serialize handlers, delegates or `IWorldTransaction` objects, guarantee exactly-once external side
effects, or automatically migrate every legacy direct world write into a transaction.

### Current formal-market command and transaction adapter

For work that changes V33 scheduling of formal-market expiry or same-county matching, the daily market command,
its reconstructed transaction, shared daily reservation or committed market event, read
`Docs/TASK_M25_P8_FORMAL_MARKET_COMMAND_TRANSACTION_AND_EVENT.md`. M25-P8 reuses the M25-P4 inventory,
escrow, trade and price ledgers without a schema upgrade; it does not commandize order creation/cancellation,
cross-county freight, NPC order generation or every legacy economic write.

### Current civilian-freight planning command and transaction adapter

For work that changes daily V33 scheduling of civilian freight demand generation, registered-carrier offers,
stable offer selection, dispatch triggering, the shared daily planning reservation or the committed planning
event, read `Docs/TASK_M25_P9_CIVILIAN_FREIGHT_PLANNING_COMMAND_TRANSACTION_AND_EVENT.md`. M25-P9 reuses
the M25-P5/P6 market, inventory, demand, offer, known-route and freight facts without a schema upgrade; it does
not commandize carrier registration, transit loss, route-leg movement, arrival settlement, NPC market-order
generation or permanent-person discovery.

### Current formal household-food monthly command and shortfall contract

For work that changes V33 monthly household nutrition demand, village-granary relief, formal family-batch
consumption, household food-security summaries, concrete resident shortfall consequences or the committed
village shortfall event, read
`Docs/TASK_M25_P10_FORMAL_HOUSEHOLD_FOOD_MONTHLY_COMMAND_AND_SHORTFALL_EVENT.md`. M25-P10 reuses the
M25-P3 inventory and village-life facts and preserves the direct/legacy monthly entry; it does not commandize
tools, medicine, corvee, agriculture, tax/remittance, county relief, market-order generation or unloaded-world
person scans.

### Current formal public-food tax, remittance, and relief command

For work that changes V33 monthly family grain tax, village retention, same-day village-to-county remittance,
county-granary relief, the per-county persistent command or its committed projection event, read
`Docs/TASK_M25_P11_FORMAL_PUBLIC_FOOD_TAX_REMITTANCE_RELIEF_COMMAND.md`. M25-P11 reuses the M25-P3
formal inventory flow, M25-P10 committed household food summaries and M25-P7 execution contract without a
schema upgrade. It does not create market buy orders, cross-county relief freight, relief-approval AI, storage
spoilage or unloaded-world person scans.

### Current public-relief shortfall procurement contract

For work that changes V34 county-relief shortfall evidence, the next-day government procurement command,
county authority and treasury limits, local formal sell-order fulfillment, county-granary receipt, public
procurement trades or unfilled procurement audit, read
`Docs/TASK_M25_P12_PUBLIC_RELIEF_SHORTFALL_PROCUREMENT_DELEGATION.md`. M25-P12 reuses M25-P4 sell
reservations and prices, M25-P7 persistent execution and M25-P11 shortfall events. It does not discover
unknown cross-county supply, create household sell orders, dispatch civilian freight or implement a full
relief approval hierarchy.

### Current cross-county public relief procurement and freight contract

For work that changes V35 external relief sourcing commands, government-owned civilian cargo, source-county
formal sell orders, carrier-known route selection, freight-fee escrow, county-granary receipt or V34-to-V35
migration, read
`Docs/TASK_M25_P13_CROSS_COUNTY_PUBLIC_RELIEF_PROCUREMENT_AND_CIVILIAN_FREIGHT.md`. M25-P13 extends
the M25-P5/P6 freight lifecycle with a mutually exclusive government buyer mode and never fabricates a buyer
family. It does not discover unknown markets, create household sell orders, implement multi-carrier handoff,
escort combat, insurance, storage spoilage or a prefectural approval hierarchy.

### Current public relief arrival recovery and bounded supplemental freight contract

For work that changes V36 completed government-freight reconciliation, village-level recovery allocation,
actual arrival and transit-loss reports, same-segment recovery commands, one-attempt supplemental sourcing or
V35-to-V36 migration, read
`Docs/TASK_M25_P14_PUBLIC_RELIEF_ARRIVAL_RECOVERY_AND_BOUNDED_SUPPLEMENTAL_FREIGHT.md`. M25-P14 distributes
only real county-granary stock, caps supplemental quantity by the actual remaining shortfall and caps its money
by the original external-procurement budget remainder. It does not implement a second automatic supplement,
unknown-market discovery, multi-carrier handoff, storage spoilage or household-level intramonth collection.

### Current formal Han food and inventory contract

For work that changes the formal five-crop/six-food scenario package, food nutrition/volume/value/consumption
definitions, family opening product batches or batch-backed household food consumption, read
`Docs/TASK_M25_P1_FORMAL_HAN_FOOD_CONTENT_AND_INVENTORY_CONTRACT.md`. M25-P1 keeps the world schema at V28
because ordinary content reuses stable product IDs, the content manifest and existing product batches. It does
not claim that legacy family grain, county markets, public granaries or cross-county transport have all been
migrated to formal batches.

### Current legacy food stock authority and formalization contract

For work that changes the V29 food-inventory authority mode, V28-to-V29 migration, legacy family food,
village public granary or county granary formalization, read
`Docs/TASK_M25_P2_LEGACY_FOOD_STOCK_AUTHORITY_AND_FORMALIZATION.md`. M25-P2 owns the explicit conservative
conversion and formal public-granary container references. It does not automatically enable formal mode in the
world scheduler or migrate harvest, tax, relief, market and cross-county freight runtime behavior; those remain
the M25-P3 boundary.

### Current formal food runtime contract

For work that changes batch-backed agriculture harvest, household consumption, family-to-village food tax,
village-to-county remittance, village relief or county relief, read
`Docs/TASK_M25_P3_FORMAL_FOOD_RUNTIME_HARVEST_CONSUMPTION_TAX_AND_RELIEF.md`. M25-P3 owns the dual-authority
runtime dispatch, closed food-transfer transactions and preservation of product/quality/provenance across local
public granary boundaries. It does not migrate county markets, civilian cross-county freight, storage spoilage or
multi-crop seed batches; those require later tasks.

### Current formal county market contract

For work that changes V30 formal food orders, sell-batch reservations, buy-order cash escrow, same-county stable
matching, household-to-household batch delivery, formal trade records or county-product transaction prices, read
`Docs/TASK_M25_P4_FORMAL_COUNTY_MARKET_AND_HOUSEHOLD_TRADE.md`. M25-P4 preserves the legacy market for
legacy inventory authority and does not implement civilian cross-county freight, storage spoilage, credit, market
UI or complete NPC order-generation policy.

### Current formal cross-county civilian freight contract

For work that changes V31 cross-county food-order fulfillment, origin handoff, buyer-owned in-transit batches,
real civilian carriers, moving family containers, deterministic transit loss, capacity-limited receipt or separate
freight-fee settlement, read `Docs/TASK_M25_P5_FORMAL_CROSS_COUNTY_CIVILIAN_FREIGHT.md`. M25-P5 supports
explicit direct routes only; it does not implement automatic pathfinding, multi-leg civilian handoff, escort combat,
insurance, credit, storage spoilage or complete NPC order/carrier selection policy.

### Current civilian freight planning contract

For work that changes V32 cross-county freight-demand generation, real carrier registration, stable carrier offers,
known-route pathfinding, deterministic offer selection or same-carrier multi-leg travel, read
`Docs/TASK_M25_P6_CIVILIAN_FREIGHT_DEMAND_CARRIER_SELECTION_AND_MULTI_LEG_ROUTING.md`. M25-P6 never scans
all permanent people for carriers and never exposes unknown routes to planning. It does not create market orders from
abstract household needs, implement multi-carrier handoff, dynamic rerouting, escort combat, insurance or storage loss.

## Task document role

Task documents define a bounded milestone, acceptance criteria, and implementation record. They must link to the governing design instead of copying repository-wide rules. When a completed task becomes stale, preserve it as history and update the governing design or this routing index.

### Current Unity batchmode startup and EditMode reliability task

For work that diagnoses or changes Unity command-line startup, startup logs, project-load smoke tests, EditMode
test filtering, result XML generation, process cleanup or grouped Unity verification, read
`Docs/TASK_TOOLING_UNITY_BATCHMODE_STARTUP_AND_EDITMODE_RELIABILITY.md`. This task may improve project tooling
and diagnostics, but it does not authorize closing user applications, changing licenses, deleting Unity caches,
weakening the 300-second hard timeout or treating missing XML as a passing result.

The completed execution record is
`Docs/TASK_TOOLING_UNITY_BATCHMODE_RECOVERY_EXECUTION.md`. It owns the bounded runner enhancement,
three-stage smoke evidence, slow-test isolation and grouped EditMode recovery required to close the reliability task.
Its current evidence summary is `Docs/UNITY_BATCHMODE_RECOVERY_REPORT.md`.

### Current M25-P14 integration baseline and core grouping task

For work that prepares, executes or aggregates the complete pure-C# core regression suite, or that audits and
collects the accumulated M23-P5 through M25-P14 working tree into a remote integration baseline, read
`Docs/TASK_TOOLING_M25_P14_INTEGRATION_BASELINE_AND_CORE_TEST_GROUPING.md`. The task owns exact assembly-based
core-test discovery, source and binary fingerprints, bounded group execution and aggregate coverage evidence.
It does not authorize a schema upgrade or implementation of M25-P15 storage spoilage.

### Current predevelopment knowledge-base consolidation contract

For any new cross-system design, historical-world development, family-spatial development or large implementation
task, start from `Docs/KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md`, then use the Canonical Domain Map to read
the matching L1 specification and the current status document before consulting L3 research or L4 task history.
Read `Docs/TASK_HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1.md` for the consolidation boundary and
`Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/README.md` for the queryable 133-settlement,
250-county, Clan/Branch and Scenario family-spatial reference. A task or acceptance report never overrides an L1
specification; `UNKNOWN` evidence must not be silently converted to `NONE`, and reference candidates must not be
materialized as Facilities, assets, FamilyOrganizations or FamilyCenters. The next historical implementation gate
is a Development Readiness Review for 184 Luoyang; only after it passes should work route to
`LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1`.

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
