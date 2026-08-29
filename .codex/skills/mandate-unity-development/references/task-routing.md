# Task and design routing

Use this index to select the smallest relevant set of project documents. Do not read every design file by default.

## Priority and conflicts

- New AI sessions may read `Docs/AI_PROJECT_BRIEF.md` first for orientation, but it is not an authority source and never replaces this routing file, the master status document, or the governing domain design.
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
| Cell ownership/occupancy, unified Facilities, Facility catalog or growth, organization offices, civil/military/title boundaries, imperial household, kingdoms, polities, self-establishment, or political AI | `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` for current status and priority; add `Docs/WORLD_SIMULATION_FOUNDATION.md` for geography/economy, `Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md` for recipes/work orders, `Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md` for combat resolution, `Docs/SANDBOX_NPC_AI.md` for generic AI scheduling, and `Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md` whenever person residency or population scheduling is affected |
| Playable Demo, player session, character selection, identity action loop, or cross-system player integration | `Docs/TASK_M26_P0_PLAYABLE_DEMO_MAIN_LOOP_INTEGRATION.md` | Read `Docs/TASK_M26_P1_MERCHANT_HOUSEHOLD_GAMEPLAY_VERTICAL_SLICE.md` for the current merchant-household goals, feedback, text, animation and long-term consequence slice; add `Docs/GAME_VISION_AND_GAMEPLAY.md`, `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`, and the affected map, profession, production, combat, population, persistence, or content design only when that slice is changed |
| Production, construction, agriculture, industry, technology, or research | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` | `Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md` for data-driven crops, products, recipes, production execution, regional economy, and profession progression; `Docs/TASK_M17_P0_DATA_DRIVEN_PRODUCTION_CONTENT_CONTRACT.md` for the completed M17 content-contract foundation; `Docs/TASK_M18_P0_CHARACTER_SKILL_KNOWLEDGE_RESEARCH_RECIPE_BRIDGE.md` for the V9 skill, knowledge, research, local technology application, and production-order snapshot bridge; `Docs/TASK_M19_P0_PRODUCT_BATCH_INVENTORY_AND_PROCESSING_CHAIN.md` for the V10 product/seed batches, inventory transactions, legacy grain adapter and first processing chain; `Docs/TASK_M23_P0_MILITARY_PROCUREMENT_TRANSPORT_AND_ARMORY_RECEIPT.md` for the V14 procurement slice; `Docs/TASK_M23_P1_EQUIPMENT_MANUFACTURING_REPAIR_AND_WORKSHOP.md` for V15 organization workshops, equipment manufacturing, static-store loading and repair orders; `Docs/TASK_M23_P2_UPSTREAM_RESOURCE_EXTRACTION_AND_PRIMARY_PROCESSING.md` for V16 resource bodies, real-worker extraction, charcoal and bloomery processing; `Docs/TASK_M23_P3_LIVESTOCK_SLAUGHTER_TANNING_AND_HORN.md` for V17 ordinary livestock batches, husbandry, slaughter, tanning and horn byproducts; `Docs/TASK_M23_P4_MULTIDIMENSIONAL_QUALITY_AND_ARTISAN_GROWTH.md` for V18 data-driven quality dimensions, production skill snapshots, artisan practice growth and audit ledger; `Docs/TASK_M23_P5_MILITARY_LOGISTICS_ACQUISITION_PROVISIONS_AND_LOSS.md` for V19 supply acquisition methods, carrier responsibility, separate convoy provisions, natural transit loss and military freight audit; `Docs/TASK_M23_P6_MULTI_LEG_LOGISTICS_HANDOFF_AND_PARTIAL_RECEIPT.md` for V20 persisted route legs, co-located custody handoff, downstream provision reservations and partial final receipt; `Docs/TASK_M23_P7_ESCORT_TRANSIT_RISK_AND_CARGO_SEIZURE.md` for V21 real escorts, deterministic transit incidents, hostile cargo seizure custody and audit; `Docs/TASK_M23_P8_LOGISTICS_CLASH_INJURY_AND_CARGO_RECOVERY.md` for V22 real-person logistics clashes, injuries and same-route army cargo recovery; `Docs/TASK_M23_P9_MILITARY_LOGISTICS_DELEGATION_AND_EXCEPTION_REPORTING.md` for V23 military supply goals, carrier offers, deterministic preference selection, budget limits and exception reports; `Docs/TASK_M23_P10_MILITARY_LOGISTICS_SCHEDULING_OFFER_LIFECYCLE_AND_REPORTING.md` for V24 due scheduling, offer withdrawal/expiry and freight progress/completion reports; `Docs/TASK_M23_P11_BOUNDED_HIERARCHICAL_MILITARY_LOGISTICS_DELEGATION.md` for V25 bounded parent-child goal decomposition, inherited budgets and bottom-up completion; `Docs/TASK_M23_P12_FAILED_SUBGOAL_CANCELLATION_REALLOCATION_AND_REASSIGNMENT.md` for V26 uncommitted subgoal cancellation, allocation recovery, offer closure and replacement history; `Docs/TASK_M23_P13_ACTUAL_RECEIPT_SHORTFALL_AND_SUPPLEMENTAL_FREIGHT.md` for V27 actual receipt, outstanding demand, sequential supplemental freight and cumulative budget audit; `Docs/TASK_M23_P14_CARRIER_LIABILITY_COMPENSATION_AND_REPLACEMENT_AUTHORIZATION.md` for V28 carrier liability settlement, real compensation/arrears, net-budget restoration and seized-cargo replacement authorization; add `Docs/WORLD_SIMULATION_FOUNDATION.md`, then the affected profession or content design |
| World simulation, economy, governance, facilities, finance, or local conflict | `Docs/WORLD_SIMULATION_FOUNDATION.md` | `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` for current status; `Docs/TASK_M22_P0_COUNTY_FISCAL_GENTRY_MARKET_GOVERNANCE.md` for the V12 county fiscal ledger, gentry compliance, market pressure, relief and public-order foundation; `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` for the accepted cross-system map, information, resource-creation, construction, and delegation contract; `Docs/DATA_AND_CONTENT_FOUNDATION.md` for authored data |
| Population, households, permanent identity, attention, event scheduling, partitioned storage | `Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md` | `Docs/TASK_M15_P6_FORMAL_PARTITIONED_POPULATION_ADAPTER_AND_RESIDENCY_CONTRACT.md` for the V7 formal adapter and hot/cold extension contract; `Docs/TASK_M20_P0_ATTENTION_LOCAL_RELATIONSHIP_AND_RESIDENCY.md` for the V11 attention ledger, bounded local relationship network and residency reconciliation; `Docs/TASK_M21_P0_PERSON_REPOSITORY_AND_INCREMENTAL_CHECKPOINT.md` for the first simulation access-layer migration and partition-level incremental checkpoint; `Docs/TASK_M21_P1_POPULATION_HOUSEHOLD_AND_BIRTH_INCREMENT.md` for life/household access migration and newborn incremental persistence; `Docs/TASK_M21_P2_VILLAGE_LIFE_AND_POPULATION_LEDGER_REPOSITORY.md` for village-life and population-ledger repository migration; `Docs/TASK_M21_P3_AGRICULTURE_PRODUCTION_PERSON_REPOSITORY.md` for agriculture read access; `Docs/TASK_M21_P4_EDUCATION_PERSON_REPOSITORY.md` for education read/write tracking; `Docs/TASK_M21_P5_MEDICAL_PERSON_REPOSITORY.md` for military medicine and recovered-patient updates; `Docs/TASK_M21_P6_MILITARY_PERSON_REPOSITORY.md` for prototype enlistment, casualties, desertion and army-march person access; `Docs/TASK_M24_P0_ONE_MILLION_FIFTY_YEAR_DEMOGRAPHIC_WORLD.md` and its report for the one-million-person demographic baseline; `Docs/TASK_M24_P1_MILLION_SUBSISTENCE_LAND_AND_PRESSURE_LOOP.md` and its report for household food need, fixed county land, agricultural labor, food conservation and traceable pressure-death evidence; `Docs/TASK_M24_P2_HOUSEHOLD_STOCK_MARKET_GRANARY_AND_RELIEF_TRANSPORT.md` and its report for household ownership, county markets, granaries and bounded relief transport; `Docs/TASK_M24_P3_HOUSEHOLD_LAND_SEED_PRODUCT_BATCH_AND_FARM_WORK_ORDER.md` and its report for specific household land, stable-ID agricultural bindings, seed inventories and streamed annual farm work orders; `Docs/TASK_M24_P4_POPULATION_RESOURCE_FEEDBACK_DIAGNOSIS_AND_CALIBRATION.md` and its report for annual bottleneck diagnosis, explicit failed candidates, seasonal public-land reuse and the accepted no-scripted-war calibration envelope; `Docs/TASK_M24_P5_FORMAL_PRODUCT_BATCH_AND_INVENTORY_TRANSACTION_BRIDGE.md` and its report for the formal V10-compatible compact-balance checkpoint bridge and completed-agriculture-order materialization entry; `Docs/TASK_M24_P6_MULTI_PRODUCT_FOOD_PROVENANCE_AND_FLOW_LEDGER.md` and its report for stable product-ID food vectors, full-flow provenance, per-product conservation and product-split formal checkpoints; `Docs/TASK_M15_PERMANENT_POPULATION_STORAGE_BENCHMARK.md` for the preceding storage evidence; `Docs/WORLD_SIMULATION_FOUNDATION.md`, `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md`, `Docs/HISTORICAL_POPULATION_135_260.md`; treat `Docs/TASK_M7_POPULATION_LEDGER.md` as an earlier implementation record |
| Historical population data, 135—260 national/province/commandery/county timelines, scenario population snapshots, 140 commanderies, or stable geographic mapping | `Docs/TASK_HAN_135_260_NATIONAL_POPULATION_DISTRIBUTION_V1.md` | Read `Docs/TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md` for the protected 140 source/mapping contract; add `Docs/HISTORICAL_POPULATION_135_260.md`, `Docs/DATA_AND_CONTENT_FOUNDATION.md`, `Docs/HISTORICAL_CITY_LIST.md`, `Docs/CITY_UNION_MASTER.md` as needed |
| Save compatibility, deterministic simulation, stable IDs or random streams | `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md` | [persistence.md](persistence.md) and the affected domain design |
| Combat, armies, authority and warfare | `Docs/UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md` | `Docs/TASK_M10_REAL_MILITARY_SERVICE_AND_COMMAND.md`, `Docs/TASK_M11_EQUIPMENT_ARMORY_AND_TROOP_DERIVATION.md`, `Docs/TASK_M23_P0_MILITARY_PROCUREMENT_TRANSPORT_AND_ARMORY_RECEIPT.md`, `Docs/TASK_M23_P1_EQUIPMENT_MANUFACTURING_REPAIR_AND_WORKSHOP.md`, `Docs/TASK_M23_P2_UPSTREAM_RESOURCE_EXTRACTION_AND_PRIMARY_PROCESSING.md`, `Docs/TASK_M23_P5_MILITARY_LOGISTICS_ACQUISITION_PROVISIONS_AND_LOSS.md`, `Docs/TASK_M23_P6_MULTI_LEG_LOGISTICS_HANDOFF_AND_PARTIAL_RECEIPT.md`, `Docs/TASK_M23_P7_ESCORT_TRANSIT_RISK_AND_CARGO_SEIZURE.md`, `Docs/TASK_M23_P8_LOGISTICS_CLASH_INJURY_AND_CARGO_RECOVERY.md`, `Docs/TASK_M23_P9_MILITARY_LOGISTICS_DELEGATION_AND_EXCEPTION_REPORTING.md`, `Docs/TASK_M23_P10_MILITARY_LOGISTICS_SCHEDULING_OFFER_LIFECYCLE_AND_REPORTING.md`, `Docs/TASK_M23_P11_BOUNDED_HIERARCHICAL_MILITARY_LOGISTICS_DELEGATION.md`, `Docs/TASK_M23_P12_FAILED_SUBGOAL_CANCELLATION_REALLOCATION_AND_REASSIGNMENT.md`, `Docs/TASK_M23_P13_ACTUAL_RECEIPT_SHORTFALL_AND_SUPPLEMENTAL_FREIGHT.md`, and `Docs/TASK_M23_P14_CARRIER_LIABILITY_COMPENSATION_AND_REPLACEMENT_AUTHORIZATION.md` for the persisted extraction/manufacture/procurement/transport/receipt/repair chain through V28 carrier liability, compensation/arrears and seized-cargo replacement authorization; add `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` when dynamic intelligence, map knowledge, recursive theater delegation, or fortification construction is affected |
| Character attributes, traits, family and growth | `Docs/CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md` | `Docs/TASK_M8_CHARACTER_ABILITY_FOUNDATION.md`, `Docs/TASK_M9_EDUCATION_AND_PRACTICE.md`; add `Docs/TASK_M18_P0_CHARACTER_SKILL_KNOWLEDGE_RESEARCH_RECIPE_BRIDGE.md` when stable-ID skills, knowledge mastery, research, or technology application is affected |
| Sandbox NPC AI | `Docs/SANDBOX_NPC_AI.md` | The M12 document whenever attention, scheduling, permanent population, or storage is affected; add `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` for limited knowledge, information work, autonomous site selection, or recursive delegation; add `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` for offices, titles, imperial politics, allegiance, polity creation, self-establishment, or follower reevaluation |
| Maps, cities and geography | `Docs/TASK_MASTER_MAP_V1_LUOYANG_POPULATION_FACILITY_CELL_CAPACITY.md` | Read `Docs/TASK_MASTER_MAP_V0_HISTORICAL_GEOGRAPHY_CELL_UNITY_PIPELINE.md` for the source geography pipeline; use `Docs/PROTOTYPE_MAP_184_ZHUO_GUANGZONG.md`, `Docs/WORLD_SIMULATION_FOUNDATION.md` and `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md` for prototype routes, continuous scale, semantic views, knowledge gating, resource creation, or dynamic facilities; `Docs/MAP_ART_RESOURCE_PLAN.md`, `Docs/HISTORICAL_CITY_LIST.md`, `Docs/CITY_UNION_MASTER.md` for visual and authored geography |
| Historical people, clans, branches, kinship, marriage, person timelines, or historical-person scenario snapshots | `Docs/TASK_HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1.md` | Read `Docs/HISTORICAL_SCENARIOS_TIMELINE_AND_FATE_DECISIONS.md` for Scenario/HistoricalTimePoint/StartPoint/FateDecision boundaries and `Docs/HISTORICAL_INPUT_INTEGRATION_AUDIT_V1.md` for imported-source decisions; add `Docs/TASK_HAN_135_260_NATIONAL_POPULATION_DISTRIBUTION_V1.md` when joining historical people to national population, and the relevant `Docs/HISTORICAL_*.md` research document when extending evidence |
| Historical events and general authored history content | `Docs/DATA_AND_CONTENT_FOUNDATION.md` | The matching `Docs/HISTORICAL_*.md` documents; use the historical-person route above whenever stable Person, Clan, timeline, or scenario-snapshot data is affected |
| External assets and licenses | `Docs/LEGAL_AND_ASSETS.md` | [content-and-data.md](content-and-data.md) |

### Current Luoyang production building kit

For work that changes the 36-model Luoyang Facility coverage, the top-ten high-frequency
production profiles, runtime procedural Mesh primitives, placement/entrance anchors, three-tier
building LOD, or the `LUOYANG KIT` review evidence, read
`Docs/TASK_LUOYANG_PRODUCTION_BUILDING_MODULAR_KIT_AND_HIGH_FREQUENCY_CITY_FABRIC_V1.md`
after `Docs/TASK_LUOYANG_FACILITY_MODEL_COVERAGE_AND_A_TIER_COMPOSITION_V1.md`. The production
kit is Presentation/content data only: it does not grant construction permission, create a
Facility, change a Global Cell, or upgrade the Save Schema.

### Current Luoyang A-tier historical landmark kit

For work that changes the ten Facility-level silhouettes for South Palace, North Palace, Yongan
Palace, Taixue, Mingtang, Biyong, Lingtai, Taicang, Arsenal or Zhuolong Garden, their exact 184
Facility/Global Cell bindings, historical confidence/source metadata, landmark LODs, procedural
landmark Mesh primitives, or the `LANDMARKS` review evidence, read
`Docs/TASK_LUOYANG_A_TIER_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1.md` after the Facility
coverage and production-building-kit tasks. These variants are selected by exact Facility ID and
must not broaden Player/AI construction permissions, move the authoritative Facility, change
ownership/control, or upgrade the Save Schema. They are strategic procedural V1 silhouettes, not
final archaeological reconstructions or artist-authored FBX assets.

### Current Luoyang permanent-population and Cell-capacity stress evidence

For Luoyang work involving 20K–500K permanent Persons, housing/job indexes, fixed versus
adaptive facilities, 2,000m Cell capacity, population LOD, binary random access, save/load
scaling or the stress debug view, read `Docs/TASK_LUOYANG_POPULATION_STRESS_V1.md` after
the M12 population rules and the protected `Docs/TASK_LUOYANG_184_HISTORICAL_V1.md`
baseline. The stress profiles are isolated evidence, not historical population claims, final
balance parameters, a SubCell design, or authority to change the national grid scale.

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

### Current village household-relief priority and authority snapshot contract

For work that changes V41 village relief-priority policy, county-government or emergency authorization,
per-pickup priority/authority snapshots, same-village scarce-food ordering or V40-to-V41 migration, read
`Docs/TASK_M25_P19_VILLAGE_RELIEF_PRIORITY_AND_AUTHORITY_SNAPSHOT.md`. M25-P19 reuses the M25-P16 pickup,
M25-P18 individual ration and existing county-governance facts. It does not create food, redistribute a person's
reserved ration, implement a policy editor, simulate favoritism/corruption, deliver meals through caregivers or
model long-term nutrition and disease.

### Current household-relief caregiver delivery and audit contract

For work that changes V42 dependent-recipient snapshots, stable household caregiver selection, actual relief
recipient provenance, per-meal caregiver delivery records or V41-to-V42 migration, read
`Docs/TASK_M25_P20_HOUSEHOLD_RELIEF_CARE_DELIVERY_AUDIT.md`. M25-P20 reuses the M25-P18 reserved individual
rations and M25-P17 traced consumption/recovery path. It does not add caregiver skills, care-duration bars,
special meal recipes, disease progression, relationship rewards or long-term nutrition history.

### Current long-term nutrition and care-feedback contract

For work that changes V43 sparse person nutrition profiles, append-only nutrition ledgers, malnutrition illness
episodes, adequate-month recovery, actual relief-consumption credits or V42-to-V43 migration, read
`Docs/TASK_M25_P21_LONG_TERM_NUTRITION_AND_CARE_FEEDBACK.md`. M25-P21 reuses the already-loaded residents from
formal household monthly settlement and the V42 per-recipient relief path. It does not scan all permanent people,
infer history during migration, implement contagious disease, diagnosis, prescriptions, physician treatment,
death resolution, caregiver work duration or a player medical interface.

### Current civilian nutrition-medical case and treatment contract

For work that changes V44 civilian medical cases, patient/household authorization, physician eligibility, formal
herbal-medicine batch consumption, nutrition-injury recovery, medical inventory transactions or V43-to-V44 migration,
read `Docs/TASK_M25_P22_FORMAL_NUTRITION_MEDICAL_CASE_AND_TREATMENT.md`. M25-P22 does not let medicine repay
nutrition debt, scan unloaded permanent people, implement contagious disease or prescriptions, formalize military
medicine, add medical work-duration scheduling or provide a player medical interface.

### Current formal herbal collection, processing and local-restock contract

For work that changes V45 village-facility capability tags, family-owned resource extraction, raw medicinal-plant
batches, herbal drying/sorting recipes and practice growth, non-food `product.market` delivery, local physician-family
restocking or V44-to-V45 migration, read
`Docs/TASK_M25_P23_FORMAL_HERBAL_SUPPLY_COLLECTION_PROCESSING_AND_LOCAL_RESTOCK.md`. M25-P23 only automates
loaded-village, same-county supply. It does not expose unknown cross-county sources, schedule physician work time,
charge treatment fees, close medical cases, implement prescriptions or formalize military medicine.

### Current formal civilian medical service contract

For work that changes V46 civilian prescriptions, treatment work minutes, household treatment fees, physician
practice growth, medical service audit, case closure or V45-to-V46 migration, read
`Docs/TASK_M25_P24_FORMAL_MEDICAL_SERVICE_WORK_FEE_PRESCRIPTION_AND_CASE_CLOSURE.md`. M25-P24 keeps
the first prescription to supportive herbal care in loaded villages. It does not implement complex diagnosis,
medical debt or charity, contagious disease, future appointment queues, cross-county care, military medicine or
a player medical interface.

### Current formal military medicine contract

For work that changes V47 army medical inventory containers, concrete wounded-service triage, military herbal-batch
consumption, physician work and skill growth, wounded return-to-duty audit or V46-to-V47 migration, read
`Docs/TASK_M25_P25_FORMAL_MILITARY_MEDICINE_TRIAGE_SUPPLY_AND_RECOVERY.md`. M25-P25 keeps payment inside the
army organization and preserves the legacy aggregate treatment record only as a compatibility display. It does not
implement surgery, disability, contagious disease, evacuation transport, field-hospital construction, automatic
military medicine procurement or a player medical interface.

### Current formal military medicine resupply contract

For work that changes V48 military-logistics delivery purposes, army medical-store receipt, real medicine freight,
medical resupply procurement, receiving capacity, delivery inventory transactions or V47-to-V48 migration, read
`Docs/TASK_M25_P26_MILITARY_MEDICINE_PROCUREMENT_AND_LOGISTICS_RECEIPT.md`. M25-P26 reuses the M23 carrier,
route, convoy-provision, natural/hostile loss and handoff facts. It does not implement automatic demand forecasting,
delegated medicine offers, wounded-person evacuation, field hospitals or a player logistics interface.

### Current battlefield casualty evacuation contract

For work that changes V49 wounded-person evacuation, concrete stretcher teams, medical-evacuation duty, detached
military movement, rear-practitioner handoff or V48-to-V49 migration, read
`Docs/TASK_M25_P27_BATTLEFIELD_CASUALTY_EVACUATION_AND_REAR_HANDOFF.md`. M25-P27 moves the patient and two to
eight real service members through ordinary journeys and excludes them from army movement or remote field treatment.
It does not implement field-hospital facilities, treatment after handoff, team return, transfer, vehicles, automated
destination selection or a player medical interface.

### Current rear medical care and rejoin contract

For work that changes V50 existing-clinic registration, rear medical beds and medicine stores, inpatient treatment,
shared physician work, discharge, patient/team return journeys, rejoin status or V49-to-V50 migration, read
`Docs/TASK_M25_P28_REAR_MEDICAL_CARE_BEDS_RETURN_AND_REJOIN.md`. M25-P28 keeps the wounded service and rescue
team detached until every return journey reaches the source army, and blocks that army from marching while they
return. It does not construct hospitals, implement repeated courses, surgery, infection, disability, death,
multi-leg return, moving-army pursuit, automatic destination selection or a player medical interface.

### Current field hospital construction and staged-care contract

For work that changes V51 field-hospital construction, organization timber/leather/money consumption, specific-person
labor, maintenance due dates, operational status or the field stabilization/recovery stages, read
`Docs/TASK_M25_P29_FIELD_HOSPITAL_CONSTRUCTION_MAINTENANCE_AND_STAGED_CARE.md`. M25-P29 creates a dynamic site
without changing the location `Clinic` feature and keeps existing clinics on their V50 one-stage contract.

### Current complex military injury and infection contract

For work that changes V52 data-defined injury profiles, admission injury episodes, severity/contamination/infection
risk, frozen inpatient protocol plans or infection-control medicine and physician work, read
`Docs/TASK_M25_P30_COMPLEX_INJURY_INFECTION_AND_FROZEN_CARE_PLAN.md`. M25-P30 does not implement surgery,
permanent disability, wound death, transfer, dynamic hospital infection spread or automatic treatment AI.

### Current trauma surgery, permanent impairment, and medical-retirement contract

For work that changes V53 data-defined surgical procedures, frozen surgery stages, permanent labor-capacity
penalties, medical-retirement discharge, patient-at-care-site retention or evacuation-team-only return, read
`Docs/TASK_M25_P31_TRAUMA_SURGERY_PERMANENT_IMPAIRMENT_AND_MEDICAL_RETIREMENT.md`. M25-P31 does not implement
wound death, cross-facility transfer, reusable surgical-tool inventory, assistants, anesthesia, long-term nursing,
prosthetics, survivor compensation or automatic treatment AI.

### Current cross-facility military medical transfer contract

For work that changes V54 pre-treatment cross-facility transfer, current-care location, frozen-plan origin,
destination bed reservation, medicine-batch reservation, real patient/evacuation-team travel or physician
responsibility handoff, read
`Docs/TASK_M25_P32_CROSS_FACILITY_MEDICAL_TRANSFER_AND_RESPONSIBILITY.md`. M25-P32 supports one direct,
same-organization transfer before the first treatment stage; it does not implement post-treatment or repeated
transfer, cross-organization settlement, cancellation/rerouting, transit deterioration/death, vehicles, bed queues
or automatic destination selection.

### Current post-treatment wound death, inheritance, and compensation contract

For work that changes V55 post-treatment wound death policies, permanent-person death, military-service death,
family inheritance, stable household-head succession, organization-funded survivor compensation or V54-to-V55
migration, read
`Docs/TASK_M25_P33_POST_TREATMENT_WOUND_DEATH_FAMILY_INHERITANCE_AND_COMPENSATION.md`. M25-P33 handles only
completed-care, completed-return, medically retired severe casualties. It does not implement death during transfer,
admission or return travel, corpse transport, funeral rites, compensation arrears or multi-heir estate division.

### Current pre-return wound death and medical-responsibility contract

For work that changes V56 ready-for-return wound death, death-context IDs, care-site death discharge/return policy,
medical-death responsibility snapshots, corpse retention at the care site, evacuation-team-only return after patient
death or V55-to-V56 migration, read
`Docs/TASK_M25_P34_PRE_RETURN_WOUND_DEATH_AND_MEDICAL_RESPONSIBILITY.md`. M25-P34 handles only completed frozen
care before return starts. It does not implement death during treatment, cross-facility transfer or return travel,
corpse transport, funeral rites, malpractice judgment, compensation arrears or multi-heir estate division.

### Current inpatient wound deterioration, death, and resource-closure contract

For work that changes V57 data-defined inpatient deterioration, death before a frozen treatment plan completes,
inpatient death closures, immediate bed release, completed-transfer medicine-reservation release, or V56-to-V57
migration, read
`Docs/TASK_M25_P35_INPATIENT_WOUND_DETERIORATION_DEATH_AND_RESOURCE_CLOSURE.md`. M25-P35 requires the patient
to be admitted and not traveling; it preserves consumed medicine and releases only unused reserved quantity. It does
not implement death during original evacuation, cross-facility transfer or return travel, automatic daily death
scheduling, corpse transport, funeral rites, malpractice judgment or extra compensation.

### Current cross-facility transfer death and transit-closure contract

For work that changes V58 death during an active or awaiting-reception medical transfer, route-progress death
snapshots, immediate destination bed/medicine release, source-care responsibility before handoff, continued corpse
escort travel or V57-to-V58 migration, read
`Docs/TASK_M25_P36_CROSS_FACILITY_TRANSFER_DEATH_AND_TRANSIT_CLOSURE.md`. M25-P36 preserves the existing
patient journey as corpse escort after an in-transit death and closes the transfer only when all travelers reach
the destination. It does not implement death during the original evacuation or return journey, rerouting, funeral
rites, malpractice judgment, extra compensation or automatic daily death scheduling.

### Current original battlefield-evacuation death and corpse-escort contract

For work that changes V59 death during an original battlefield evacuation or awaiting rear reception,
health-derived pre-diagnosis severity, source-army responsibility before handoff, continued corpse escort on the
original patient journey, evacuation-team-only return without an admission, or V58-to-V59 migration, read
`Docs/TASK_M25_P37_ORIGINAL_EVACUATION_DEATH_AND_CORPSE_ESCORT.md`. M25-P37 does not fabricate an injury
episode, admission, rear-site responsibility or receiving-physician handoff before reception. It does not implement
death during the return journey, rerouting, funeral rites, malpractice judgment, extra compensation or automatic
daily death scheduling.

### Current patient-return death and corpse-rejoin contract

For work that changes V60 patient death during an established return-to-army journey, the frozen return route and
remaining distance, last-care responsibility, preservation of the patient journey as corpse escort, army movement
blocking until the whole party rejoins, or V59-to-V60 migration, read
`Docs/TASK_M25_P38_PATIENT_RETURN_JOURNEY_DEATH_AND_CORPSE_REJOIN.md`. M25-P38 handles only a patient with an
active return journey after discharge. It does not implement evacuation-team-member death, death after the patient
has arrived while teammates are still traveling, rerouting, burial, malpractice judgment or automatic scheduling.

### Current patient-arrived waiting-team death contract

For work that changes V61 patient death after the patient has completed the return journey while one or more
evacuation-team members are still returning, frozen per-member return snapshots, a corpse remaining at the source
army, continued army movement blocking, or V60-to-V61 migration, read
`Docs/TASK_M25_P39_PATIENT_ARRIVAL_WAITING_TEAM_DEATH_AND_REJOIN_CLOSURE.md`. M25-P39 preserves the last-care
responsibility separately from the source-army compensation organization and closes the admission/evacuation only
after every remaining team member rejoins. It does not implement evacuation-team-member death, rerouting, burial,
malpractice judgment or automatic scheduling.

### Current evacuation-team return death and corpse-rejoin contract

For work that changes V62 death of a concrete evacuation-team member during an established return journey,
the frozen route and remaining distance, permanent-person death, family inheritance, source-army compensation,
preservation of the member journey as corpse return, survivor rejoin status, or V61-to-V62 migration, read
`Docs/TASK_M25_P40_EVACUATION_TEAM_RETURN_DEATH_AND_CORPSE_REJOIN.md`. M25-P40 supports a living patient,
a patient corpse still returning, or a patient corpse already at the army without rewriting the existing patient
death contract. It does not implement team-member death during original evacuation or medical transfer, injury,
desertion, disappearance, capture, rerouting, burial, malpractice judgment or automatic scheduling.

### Current post-treatment first medical-transfer contract

For work that changes V63 first same-organization transfer after one or more frozen treatment stages,
the completed-stage dispatch snapshot, remaining-medicine reservation, preservation of source-care treatment
evidence, destination responsibility for only the remaining stages, or V62-to-V63 migration, read
`Docs/TASK_M25_P41_POST_TREATMENT_FIRST_MEDICAL_TRANSFER.md`. M25-P41 still permits only one direct transfer
and does not implement repeated transfer, cross-organization settlement, cancellation, rerouting, automatic
destination selection or a player medical interface.

### Current repeated same-organization medical-transfer chain

For work that changes V64 repeated same-organization transfer, per-leg sequence/previous/next links,
onward release and re-reservation of remaining medicine, per-leg bed/travel/physician responsibility, or
V63-to-V64 migration, read
`Docs/TASK_M25_P42_REPEATED_SAME_ORGANIZATION_MEDICAL_TRANSFER_CHAIN.md`. M25-P42 permits at most four
direct same-organization transfer legs in one frozen admission. It does not implement cross-organization
settlement, cancellation, rejection, rerouting, automatic destination selection, multi-batch reservation or a
player medical interface.

M25-P42 is a completed backend contract, not the current feature-development direction. The immediate global
priority is the playable player loop in `Docs/TASK_M26_P0_PLAYABLE_DEMO_MAIN_LOOP_INTEGRATION.md`; do not infer
M25-P43 or expose the detailed transfer ledger as ordinary player interaction without a new explicit decision.

### Current playable Demo main-loop integration task

For work that changes the ordinary player entry scene, arbitrary character creation or world-person selection,
identity landing, contextual player actions, map travel, event choice, construction/production/trade/task integration,
local combat presentation, or simplified player-facing injury treatment, read
`Docs/TASK_M26_P0_PLAYABLE_DEMO_MAIN_LOOP_INTEGRATION.md`. It integrates existing systems through the shared
world ledger and does not authorize a second Demo-only economy, population, inventory, combat, or medical truth.

### Current merchant-household gameplay vertical slice

For work that changes the first mature-gameplay benchmark slice, merchant-household goal chain, player-facing
market intelligence, action feedback, authored text, reusable action presentation, family investment consequences,
or the independent 20-30 minute playability acceptance route, read
`Docs/TASK_M26_P1_MERCHANT_HOUSEHOLD_GAMEPLAY_VERTICAL_SLICE.md`. M26-P1 must reuse the formal world ledger
and does not authorize copying proprietary game code or content, a Demo-only economy, all identities at once,
the complete dynasty UI, the 223 historical scenario, or an unreviewed save-version upgrade.

Read `Docs/REPORT_M26_P1_IMPLEMENTATION_AND_ACCEPTANCE.md` before extending this slice. It records the current
automatically verified candidate, the still-missing independent blind-play evidence, and the unresolved boundary
between legacy person-carried cloth trading and the formal family/order/product-batch market. Do not mark M26-P1
complete or extend the legacy cloth path as if that formal-market contract had already been resolved.

For work that changes the shared strategic-atlas/caravan-journey presentation, data-driven commodity-to-product
mapping, formal merchant-carried product batches, merchant purchase/loss/sale inventory ledgers, or the V64-to-V65
content-manifest migration, read
`Docs/TASK_M26_P2_STRATEGIC_WORLD_AND_CARAVAN_GAMEPLAY_INTEGRATION.md`. M26-P2 closes the first concrete-cloth
cargo gap but does not claim complete household order books, merchant-guild warehouses, industries, NPC company
competition, water, vehicle durability or delegation.

For work that changes strategic delegation policy IDs, delegated-order permissions, agriculture/commerce/defense
priority weights, or deterministic policy-based candidate selection, read
`Docs/TASK_M26_P3_ZHSAN_STRATEGIC_DELEGATION_INTEGRATION.md` and
`Docs/ZHSAN_OPEN_SOURCE_LICENSE_AND_INTEGRATION_AUDIT.md`. The first M26-P3 slice is an independent rewrite of
the district/legion delegation problem and does not authorize importing maps, art, music, scenarios, content packs,
trademarks or unisolated Ms-PL source. It also does not claim that real positions, organizations or world commands
already consume the policy contract.

For work that changes V66 strategic delegation mandates, issuer/assignee position snapshots, organization-order
capability intersections, jurisdiction and budget limits, bound command candidates, persisted command proposals,
or the V65-to-V66 migration, read
`Docs/TASK_M26_P4_STRATEGIC_DELEGATION_MANDATE_AND_COMMAND_PROPOSAL.md`. M26-P4 records proposals only; it does
not enqueue or execute agriculture, commerce, construction, recruitment, military movement or campaign commands,
and it does not create a complete organization AI or import any Zhonghua Sanguozhi asset or source file.

For work that changes V67 town facilities, merchant branches, enterable town buildings, merchant-headquarters
and warehouse presentation, town access policy, the Zhongshan merchant-company opening facts, or the V66-to-V67
migration, read `Docs/TASK_M26_P5A_ZHONGSHAN_MERCHANT_TOWN_OPERATION_SLICE.md` together with
`Docs/TASK_M26_P5_MERCHANT_ORGANIZATION_GAMEPLAY_EXPANSION.md` and
`Docs/REFERENCE_JIUZHOU_GAMEPLAY_ANALYSIS.md`. M26-P5A only establishes the first enterable Zhongshan town
and merchant-organization entry slice. It does not claim complete recruitment, wages, caravan roles, vehicles,
water/fatigue, nationwide branches, industries, NPC company competition or automated commerce.

For work that changes V68 persistent town-facility placement, stable district IDs, normalized town-map coordinates,
the Zhongshan world-node-to-town-to-building route, unplaced-facility fallback, or the V67-to-V68 migration, read
`Docs/TASK_M26_P5B_ZHONGSHAN_FULL_SCALE_MAP_VERTICAL_SLICE.md` together with the M26-P5A task, M16 living-map
design and `Docs/MAP_ART_RESOURCE_PLAN.md`. M26-P5B establishes one authored Zhongshan spatial slice only; it does
not claim nationwide town layouts, free construction placement, indoor walking, complete information fog or city siege.

For the read-only opening audit, minute-by-minute blind-play timeline, evidence capture, six-category defect
classification, severity ranking and first-route freeze, also read
`Docs/TASK_M26_P1A_PLAYABILITY_BASELINE_AUDIT.md`. M26-P1A records the baseline and must not mix fixes into
the same run or use developer guidance to overwrite first-player evidence.

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

## Core reading order

For cross-system design or planning, use the order established by the master document:

```text
GAME_VISION_AND_GAMEPLAY
→ GAME_SYSTEMS_MASTER_AND_STATUS
→ WORLD_SIMULATION_FOUNDATION
→ UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI
→ CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH / UNIFIED_COMBAT_WARFARE_AND_AUTHORITY
→ TASK_M12_PERMANENT_POPULATION_AND_ATTENTION
→ HISTORICAL_POPULATION_135_260
```

Do not load all seven documents for a narrow implementation or defect. Start from the relevant route and add the master only when status, priority, or cross-system behavior is in scope.

### Current Luoyang 184 historical-city contract

For work that changes historical Luoyang 184 Facilities, Person-based housing, real Facility jobs, local development
pressure, multi-Cell construction blueprints, the twelve city gates, palace-wall independence, moat movement,
historical source confidence or the `LuoyangWorldValidation` scene, read
`Docs/TASK_LUOYANG_184_HISTORICAL_V1.md`. It owns the first unified-world historical-city prototype on HanWorldV1;
it does not authorize a second city map, SubCells, later-dynasty features, full siege engines, a complete Blueprint UI
or a main-save schema upgrade.

### Current 135—260 historical-world reference contract

For work that researches or changes annual historical-world state, province/commandery/county development
references, strategic-city research tiers, historical person or Clan geography, regional event impacts, Scenario
world references, or the historical source index, read
`Docs/TASK_HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1.md` and start from
`Docs/HISTORICAL_WORLD_REFERENCE/README_历史世界开发参考资料索引.md`. This reference library projects the
existing stable map, population and person/Clan datasets; it does not authorize a second runtime world, nationwide
person/household/Facility generation, synthetic historical boundaries, or silent completion of unknown evidence.

For work on Canonical core-settlement deduplication, province/commandery seat timelines, priority counties,
P0 city research levels, Estate References, historical industry/transport/military development references, or
scenario spatial readiness, additionally read
`Docs/TASK_HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/README_历史世界深化资料索引.md`. The deepening layer remains
inside V1 and does not authorize materializing estates, nationwide Facilities, new permanent people, households,
FamilyOrganizations, or a second population model. After its coverage report passes, route next work to a
Development Readiness Review instead of expanding the historical-reference framework again.

### Current national county production, resource, industry and supply reference contract

For work that changes county land/resource potential, agricultural or industrial capacity, commodity demand,
surplus/deficit, import dependency, export capacity, modeled county corridors, regional production zones, or the
13 scenario production references, read
`Docs/TASK_HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_NETWORK_V1.md` and start from
`Docs/HISTORICAL_WORLD_REFERENCE/HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_REFERENCE/README.md`.
This package is a development reference, not runtime authority. It does not authorize nationwide Person,
Household, Facility, Inventory or route materialization. Unresolved county analytical points must remain marked
MODELED; county aggregates may initialize or calibrate Cell/Resource/Facility/Worker/Recipe/Inventory/Transport
facts but may never replace them. Read the M12 permanent-population contract before any selected-area population
materialization.

### Previous Luoyang Facility selection, collision proxy and road navigation V1 route

For work that changes the 2,084 Facility selection-proxy contract, dense-CITY 549 trigger colliders, Facility
ray picking, selected-building highlight, 359 road nodes, 20 gate/bridge nodes, strict cardinal road edges,
provisional gap connectors, deterministic Facility-path lookup, road overlay or WORLD cleanup, read
`Docs/TASK_LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1/README.md`
together with the actual whole-city composition route.

The current implementation status is
`LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`.
It creates separate Presentation trigger proxies instead of adding Colliders to final-art Prefabs. The static graph
contains 379 nodes and 382 edges: 334 authored cardinal road adjacencies, 28 explicitly provisional inter-component
connectors and 20 gate/bridge-to-road connectors. Provisional connectors are not historical road claims and must be
replaced as road data is refined. This route does not implement character-scale NavMesh, solid unit collision, gate
open/close or siege state, bridge damage/load limits, high-resolution Luoyang terrain, or persistence changes.

### Current Luoyang authored road connectors and dynamic passage traversal V1 route

For work that changes the 28 stable modeled connector records, connector cell waypoints and provenance, the
379-node/402-edge refined graph, 20 two-sided gate/bridge approaches, passage open/closed/damaged/destroyed
Domain session records, deterministic state-aware pathfinding, cyan/amber/red CITY overlays or controller passage
APIs, read `Docs/TASK_LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1/README.md`
together with the previous interaction/navigation task and deterministic save contract.

The current implementation status is
`LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`.
The previous 382-edge graph remains a historical base contract; the current runtime consumes the 402-edge
refinement layer with 334 strict road edges, 28 modeled reconstruction connectors and 40 two-sided passage
approaches. Modeled connectors explicitly use `historical_evidence.gameplay_reconstruction`, cell precision and
`ClaimsHistoricalExactness=false`. Passage state is pure Domain session state and does not persist across save;
do not report WorldState, command/event, snapshot/migration, guard/permission/siege, bridge load/flood/repair,
door animation, character-scale NavMesh or high-resolution road geometry as implemented.

### Current Luoyang passage WorldState, command/event and save V1 route

For work that changes the V74 persisted state of the 20 Luoyang gates/bridges, explicit atomic initialization,
passage transition commands, expected-revision conflict handling, command/result/transaction/outbox provenance,
the V73-to-V74 migration or the map controller's read-only WorldState projection, read
`Docs/TASK_LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1.md` together with the authored connector task,
M25-P7 persistent execution contract and deterministic save specification.

V73-to-V74 initializes an empty collection and never invents prior preview-session state. The 20 opening records
must be created by the explicit frozen initialization command. This route does not implement guards, authority,
siege, bridge load/flood, repair materials/work orders, passage animation, character-scale NavMesh or outer supply
roads. Its current status is
`LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`.
Compilation, targeted core 3/3, target EditMode 5/5, bound-world graphical PlayMode 1/1, previous passage graphical
regression 1/1, whole-city graphical regression 1/1 and diff validation passed. This targeted evidence is not a
complete core, EditMode or PlayMode suite.

### Current Luoyang passage guard, battle-damage and real-repair V1 route

For work that changes V75 passage guard assignment, controller/Army/Person authority, battle-backed damage,
integrity history, real Facility repair projects, timber/iron reservations, construction labor, repair completion,
`SourceFacilityConstructionProjectId` or the V74-to-V75 migration, read
`Docs/TASK_LUOYANG_PASSAGE_GUARD_DAMAGE_AND_REAL_REPAIR_V1.md` together with the V74 passage task,
M12 permanent-population contract, the unified Facility/authority, combat and production specifications, M25-P7,
M25-P29 and the deterministic save specification.

V1 normal open/close accepts only the current controller-organization leader or guard-Army commander after an
explicit guard contract exists. Damage consumes an existing hostile `BattleRecordState`; it does not create or
resolve combat. Repair reuses `FacilityConstructionProjectState(Repair)`, real product batches, inventory
transactions and specific Person labor, and completes to a closed passage. V74-to-V75 initializes empty new
collections and never infers prior guards, damage, battles, materials or labor. Code, compilation and targeted core
verification have passed. The initial sandboxed Unity attempts were blocked before startup-log creation, but the
same safe runner subsequently passed EngineSmoke, the exact EditMode test and the related graphical PlayMode test
outside the restricted workspace; the current target status is
`LUOYANG_PASSAGE_GUARD_DAMAGE_AND_REAL_REPAIR_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`.
This is targeted evidence, not a complete grouped Unity regression. Full siege, siege engines, bridge load/flood,
animation and character-scale NavMesh remain out of scope.

### Current Luoyang passage stateful presentation and pedestrian blocking V1 route

For work that changes the 20-passage read-only pedestrian projection, open/closed/damaged/destroyed runtime
pieces, non-trigger pedestrian blockers, active-repair scaffold projection, approach-axis orientation, CITY/WORLD
lifecycle, passage-presentation metrics or the close-up review evidence, read
`Docs/TASK_LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1/README.md`
together with the V75 guard/damage/repair task and the preceding interaction/navigation and persisted-passage routes.

The current status is
`LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`.
The projection changes no save field and never writes back to V75. `closed` and `destroyed` enable one reusable
non-trigger BoxCollider per resident passage; `open` and `damaged` disable it and retain the existing domain path
rules. An active V75 repair order adds scaffold presentation without inventing a passage transition. The low-poly
pieces and colliders are independent runtime children, not modifications to the 54 final Prefabs, and WORLD view
cleans them with the existing interaction root. Compilation, targeted core 6/6, exact EditMode 1/1, target graphical
PlayMode 1/1, bound-world PlayMode 1/1 and the preceding interaction graphical regression 1/1 passed. This is
targeted evidence, not complete grouped regression, full NavMesh baking, character animation, indoor walking or
final gate/bridge animation art.

### Current Luoyang click-to-walk pedestrian vertical slice V1 route

For work that changes the Luoyang pedestrian corridor widths, stable actor-lane offset, click snapping, focused
pedestrian runtime, route/target presentation, dynamic passage route cancellation, CapsuleCollider safety stop,
CITY/WORLD lifecycle or the walking close-up evidence, read
`Docs/TASK_LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1/README.md` together with
the preceding stateful-passage, refined-road, facility-interaction, M12 permanent-population and M26 playable-loop
routes.

The current status is
`LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`.
The read-only Domain plan freezes 18m road, 12m modeled-connector, 12m gate, 8m bridge, 0.45m personal-clearance
and 1.35m/s walking contracts while retaining the existing passage-aware Dijkstra cost. CITY instantiates one
focused presentation actor, a non-trigger CapsuleCollider, a visible current route and target; a required passage
closing or being destroyed cancels the remaining route in the same refresh. The preview actor is not a new
PermanentPerson, and no per-frame position or route enters V75. This targeted slice is not a whole-city NavMesh,
indoor navigation, crowd/RVO simulation, final character animation or a replacement for formal M26 travel commands.

### Current Luoyang formal player movement and world settlement V1 route

For work that changes formal `PlayerSession` resolution, V76 local Facility/Cell person facts, persistent movement
commands/events, road operational state, passage-aware formal route snapshots, movement time/stamina/food settlement,
segment-boundary resume, deterministic replay or committed-route Unity playback, read
`Docs/TASK_LUOYANG_FORMAL_PLAYER_MOVEMENT_WORLD_SETTLEMENT_V1.md` and
`Docs/LUOYANG_FORMAL_PLAYER_MOVEMENT_V1_ACCEPTANCE_REPORT.md` together with the preceding click-to-walk route,
the V75 passage route, M12 permanent-population contract, M25-P7 persistent execution contract and deterministic
save specification.

`WorldState.PlayerPersonId` remains the only persisted controlled-person reference; `PlayerSession` is a Domain
wrapper and does not create a second player. Settlement reuses `PersonState.LocationId`, food reuses `Provisions`,
and world time advances only through the existing simulator. V75-to-V76 initializes empty movement collections and
never infers formal facts from the preview actor. Presentation may play a committed route but must not write Person,
time, resources, roads or passages. The current status is
`LUOYANG_FORMAL_PLAYER_MOVEMENT_WORLD_SETTLEMENT_V1_ACCEPTED`. Compilation, targeted core 11/11, frozen complete
core 747/747, controlled ProjectLoad, EditMode 11/11, graphical PlayMode 4/4, three identical replay hashes and diff
review passed. Only the exact multi-year food and save/resume determinism tests use the classified 900-second ceiling
(about 503 and 502 seconds); every other core and Unity test retains 300 seconds. A separate opt-in living-evidence
refresh exposed an existing food-conservation mismatch and remains outside this movement task. The next route is
Luoyang character-scale close map and local navigation V1, not automatic movement expansion.

### Current family-organization center and historical-family reference contract

For work that changes Clan/Branch/Household/FamilyOrganization separation, family-organization assets, Primary or
Local FamilyCenter designation, remote family management, family-center action permissions, historical family
spatial evidence, Scenario family-organization initialization candidates, or the seven Luoyang 184 family
organizations, read `Docs/TASK_FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1.md` and start from
`Docs/FAMILY_ORGANIZATION_REFERENCE_V1/README.md`. Also read
`Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md` when people, households, residency or large-scale generation
is involved. The V1 reference set never authorizes mapping all 39 Clans to 39 FamilyOrganizations, treating member
presence or an estate as a center, generating nationwide family assets/Facilities, or rewriting the current seven
Luoyang organizations without a separately validated migration task.

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

### Current Luoyang 184 development-readiness gate

Before changing the formal Luoyang 184 main-world population projection, historical Person binding,
Clan/Branch metadata, the seven urban FamilyOrganizations, the eight metropolitan generated organizations,
FamilyCenter designation, opening residence/job authority, or V68-following persistence, read
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1_REPORT.md`,
`08_LUOYANG_184_INITIALIZATION_REFERENCE.md`, and `11_NEXT_IMPLEMENTATION_TASK_SCOPE.md` in the same directory.
The gate result is Gate A `GO_WITH_BLOCKERS` and Gate B `GO_WITH_DEFERRED_PLACES`. Route implementation to
`LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1`. Hulao, Hangu, the 700K supply envelope, nationwide
materialization, general Facility refactoring, 190 historical-change gameplay, UI, art, scenes, and prefabs remain
out of scope. The 400K formal composite is the only opening population source; never add the 130,169 county model
reference, nested 200K/270K scopes, or the inclusive 700K plan to it.

The bounded `LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1` implementation is recorded in
`Docs/TASK_LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1/`. Read those files before
changing V69 historical identity, lineage, the 15 retained FamilyOrganizations, FamilyCenter activation,
Civil/Military Office mapping, the protected 400K adapter, or the 32 unresolved metropolitan Facility claims.
The next candidate is a writable derived population checkpoint plus the Luoyang living-world economic loop; it is
not automatically authorized. Hulao, Hangu, the inclusive 700K envelope and 190 event gameplay remain deferred.

### Current Luoyang 184 formal urban initialization contract

For work that changes the formal 270,000-person Luoyang urban package, its 53,992 households, Person-based
residential occupancy, real Facility work/student capacity, seven FamilyOrganizations, five initialized Forces,
ordered 184 runtime events, binary record format, spreadsheet audit shards or package integrity validation, read
`Docs/TASK_LUOYANG_184_URBAN_INITIALIZATION_V1.md`. The 200,000 walled-city and 270,000 continuous-urban
targets are materialized; 400,000 metropolitan and 700,000 supply-region targets remain plans only. This task does
not authorize SubCells, a second city map, automatic 700,000-person generation, merging permanent people, or
loading engineering/stress profiles into the formal scenario.

### Current Development Place full-reference-pack contract

For any work that develops, researches, upgrades, compares, implements, or audits one of the 72 formal
Development Places, read `Docs/TASK_HAN_135_260_DEVELOPMENT_PLACE_FULL_REFERENCE_PACK_V1.md` and start from
`Docs/HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md`. The current tiers are
T1/T2/T3/T4 (23/33/15/1), losslessly mapped from historical D2/D3/D4/D5 evidence. Existing waves are frozen.
D0/D1 are no longer special Development Place tiers, and non-roster locations do not receive a T0 tier.

DevelopmentTier, ReferencePackCompleteness and RuntimeImplementationStatus are orthogonal. Every T1-T4 Place
uses the same 25-module full-pack standard; FULL means every question was audited, and UNKNOWN, NO_EVIDENCE or
NOT_APPLICABLE are valid answers. A Pack never automatically creates Cells, Places, Facilities, population,
persons, FamilyOrganizations, camps, historical events, save migrations, scenes or prefabs, and never upgrades
a tier by itself. The historical D-depth roster and the first ten City Development Packs remain evidence inputs.

For battlefield and event-dependent locations, distinguish PERMANENT_SETTLEMENT,
PERMANENT_GEOGRAPHIC_SITE, EVENT_DEPENDENT_COMPLEX, BATTLEFIELD_REGION and UNRESOLVED. Historical battle fame
does not prove a permanent settlement. Event facilities use normal Facility definitions and may be established
only when the event actually occurs; later direct scenarios may initialize a supported historical post-event
state, while continuous simulation must preserve its actual branch.

### Current Luoyang 184 person-work-production-consumption closure contract

For work that changes the protected 400,000-person Luoyang workforce projection, current-activity occupation,
facility minimum/optimal staffing, production cycles, crop maturity/harvest/seed recovery, household food
consumption, living-world inventory, shortage response, V70 summary or derived living-world checkpoint, read
`Docs/TASK_LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1.md` and the report directory
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1/`. Also read M12.
The protected counts remain exactly 400,000 Person, 80,899 Household and 2,084 Facility; this route never
authorizes rewriting the initialization package, materializing the planned outer 300K population, inventing daily
imports, or treating presentation objects as world facts. The 365-day result is `SUPPLY_REGION_DEPENDENCY`, so the
next candidate is Luoyang supply-region/agricultural-hinterland materialization before full commerce depth.

### Current intelligent-population-driven world and conditional historical-event contract

For work that changes WorldSignal, DecisionContext, ActionIntent, action validation, Decision Policy, World Seed,
Simulation Arena, HOT/WARM/COLD scheduling, structured historical-event preconditions, HistoricalChangePackage,
event outcome persistence, or the boundary between national historical reference data and runtime supply, read
`Docs/TASK_WORLD_INTELLIGENT_POPULATION_DRIVEN_SIMULATION_AND_HISTORICAL_EVENT_CONTRACT_V1.md` and the delivery
directory `Docs/HISTORICAL_WORLD_REFERENCE/WORLD_INTELLIGENT_POPULATION_DRIVEN_SIMULATION_AND_HISTORICAL_EVENT_CONTRACT_V1/`.
Also read M12 for permanent persons and `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md` for V71 migration.

Reference datasets never become a second runtime world. AI proposes intents and cannot bypass the common validator
or existing command/transaction/event execution. Major historical events require at least one non-time condition;
year only opens a window. LOD changes cadence only and never deletes, merges, replaces or rerandomizes permanent
facts. Neural policies are scorer adapters only; V1 forbids online training in production runtime.

### Current intelligent decision policy and simulation-arena contract (V72)

For work that changes candidate generation, Utility score components, personality/goal policy profiles,
Randomized Utility, neural candidate scoring, decision memory, Arena benchmarks/counterfactuals, multi-seed
decision divergence, or V72 agent policy persistence, read
`Docs/TASK_WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1.md` and the delivery directory
`Docs/HISTORICAL_WORLD_REFERENCE/WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1/`. Also read the
V71 intelligent-population-driven contract, M12, and the persistence reference.

Utility remains the primary interpretable production baseline. Neural may score already-generated candidates but
never mutates world facts, bypasses validation, or decides major historical-event truth. Runtime online learning is
prohibited; missing, invalid, NaN or schema-mismatched models must fall back safely. Current Arena evidence is a
bounded contract fixture, not proof of mature national Facility, industry, trade, government, 400K Luoyang, or
HOT/WARM/COLD performance. The next candidate is
`WORLD-HOT-WARM-COLD-PERMANENT-PERSON-SIMULATION-V1` after an explicit task authorizes it.

### Current Region Cell boundary and technical-block semantic contract

For work that changes Global Region membership, Region boundary edges, Region polygons or bounds, cross-Region
neighbor queries, the retained 16×16 technical index, 64×64 storage blocks, Terrain Tile sizing, Streaming Unit
sizing, or the boundary between technical Regions and historical AdministrativeRegions, read
`Docs/TASK_WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1_REPORT.md`.

Global Cell is the final authoritative world-space partition. Region authority is `IncludedGlobalCellIds`, and
Region boundaries are derived Cell edges that never cut Cells or create seam/border Cells. The retained 16×16
IDs are technical spatial/simulation aggregation indices, not frozen Terrain or Streaming units; 64×64 remains
storage/compression only. The benchmark has now been completed by
`Docs/TASK_HAN_WORLD_NATURAL_TERRAIN_AND_LANDSCAPE_BASEMAP_V1.md`: Terrain Tile is frozen at 8x8 Global Cells,
while the 24x24-Cell Streaming Unit remains provisional. For Global DEM sampling, natural surfaces, Terrain
generation, river/vegetation presentation, Cell picking, Floating Origin or background-independent maps, read
`Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_TERRAIN_AND_LANDSCAPE_BASEMAP_V1/HAN_WORLD_NATURAL_TERRAIN_AND_LANDSCAPE_BASEMAP_V1_REPORT.md`
and its machine/visual evidence before changing code or data.

### Current Han world natural-map visual-presentation V2 route

For work that changes natural-map rendering, terrain LOD presentation, terrain surface blending, river banks and
width, forest density/vegetation batching, fixed map cameras, WORLD-to-REGION transitions, Cell-grid visibility,
background independence, visual screenshots or Golden-map approval, read
`Docs/TASK_HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2/HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2_REPORT.md`.

The current state is `PLAYABLE_WITH_ART_LIMITS`, not final art. One Global DEM and one Global Cell world remain
authoritative. WORLD draws one sampled continuous surface; REGION draws one continuous 2km Cell surface. The nine
resident 8x8 Terrain Tiles retain collision/streaming meaning but do not draw duplicate overlapping rectangles.
The 14 Game View screenshots remain Golden candidates until the user explicitly approves the visual direction.

### Current Han world natural-map art-direction and rendering V1 route

For work that changes Style A/B/C terrain profiles, natural-map palette, relief shading, lighting, atmosphere,
water/forest presentation tint, fixed art sample cameras, candidate screenshots, style performance evidence or
the final style decision gate, read `Docs/TASK_HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1/HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1_REPORT.md`.

The current status is `HAN_WORLD_ART_DIRECTION_V1_CANDIDATES_READY`, not finalized art. STYLE A/B/C use the
same authoritative DEM, Global Cells, river/forest inputs and camera per sample/view. `USER_SELECTED_STYLE`
remains `PENDING`; nationwide rollout, Henan Yin high-detail terrain and Luoyang city art are blocked until the
user explicitly chooses a style or asks for a revision. Codex's STYLE B recommendation is not user approval.

### Current Zhonghua-Sanguozhi-inspired Style D prototype route

For work that changes the clean-room Style D profile, derived ridge/valley/mountain/plain/forest/river-valley
feature channels, Style D shader fusion, its fixed cameras, source/license audit or its ten visual evidence
captures, read `Docs/TASK_HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1/README.md`.

The Unity status is `STYLE_D_ZHONGHUA_SANGUOZHI_FUSION_PROTOTYPE_READY`, not selected final art. The candidate
repository was pinned and statically audited through the GitHub API, but full Git clone was hard-blocked by
GitHub 443 failures; do not claim source-research COMPLETE and do not copy candidate code/assets. Nationwide
rollout, Henan Yin high detail and Luoyang city work remain blocked pending explicit user approval.

### Current Style D strategic-landscape V2 review route

For work that changes presentation-only terrain detail, adaptive river bend/join meshes, synchronized river
banks, WORLD/REGION/CITY forest LOD, Style D V2 cameras, visual acceptance, performance evidence or the
finite Zhonghua source-clone recovery audit, read
`Docs/TASK_HAN_WORLD_STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_AND_ZHONGHUA_SOURCE_RECOVERY_V2.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_V2/README.md`.

The current gate is `STYLE_D_STRATEGIC_LANDSCAPE_V2_READY_FOR_USER_REVIEW`, not final art. Global Cell
resolution remains 2000m and must not be confused with presentation vertex density. River source-endpoint
stitching/confluence, close terrain low-frequency blocks and continuous LOD morph remain PARTIAL. Source clone
is `SOURCE_CLONE_BLOCKED_BY_NETWORK_V2`; license is unresolved and no candidate code/assets may be copied.
Do not start nationwide rollout, Henan Yin production terrain or Luoyang city assets without explicit user approval.

### Current explicit strategic-cell map V1 route

For work that changes visible tactical Cell faces/edges, Cell hover or selection, strategic Cell shaders,
the Henan Yin 24x24 review window, its fixed review cameras, Cell-overlay batching or its three screenshot
captures, read `Docs/TASK_HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1/README.md` together with the natural-map
V2 and Style D V2 routes above.

The user explicitly accepted this presentation direction on 2026-08-26. It displays the existing 2000m
`hanworld.square-grid.v1` cells and preserves stable IDs, eight-neighbor behavior, Global Origin, Region
membership and persistence. It creates no SubCells and does not authorize a six-neighbor hex migration.
That initial gate limited scope to the Henan Yin greybox. Nationwide presentation was subsequently authorized
by the explicit route below; final Golden art still requires a separate gate.

### Current nationwide strategic-cell grid LOD V1 route

For work that changes nationwide Cell-grid coverage, WORLD grid LOD, the 32x32 visual guide step,
arbitrary-Cell focus, nationwide grid performance or the nationwide overview screenshot, read
`Docs/TASK_HAN_WORLD_NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1/README.md` together with the
explicit strategic-cell V1 route above.

The user explicitly authorized nationwide grid presentation on 2026-08-26. WORLD uses a 32x32-Cell visual
guide LOD in one batch; REGION/CITY uses exact 1x1 2000m Cells. The 32x32 step is presentation-only and must
never become a Chunk, Region, administrative, simulation, persistence or identity boundary. All 7,211,264
stable Cell IDs and eight-neighbor behavior remain authoritative.

### Current Luoyang buildable Facility model kit V1 route

For work that changes the accepted first-batch residence, warehouse, workshop, market, field-hospital,
city-wall or city-gate models, their stable Model/Asset IDs, shared Han material palette, procedural module
catalog, direct Global Cell placement, BUILDINGS review camera or visual evidence, read
`Docs/TASK_LUOYANG_BUILDABLE_FACILITY_MODEL_KIT_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_BUILDABLE_FACILITY_MODEL_KIT_V1/README.md` together with the
explicit strategic-cell and playable-vertical-slice routes.

These seven assets are presentation bindings over existing Facility/build contracts. Preview instances are
not authoritative Facilities and cannot complete construction, create ownership, consume materials or alter
saves. Nangong remains historical-only. WORLD creates no building models. The current V1 uses original Unity
primitive compositions and a limited palette; it is directly placeable but is not final artist-authored FBX,
baked textures or production LOD art.

### Current Luoyang Facility model coverage and A-tier composition V1 route

For work that changes the complete 36-model Luoyang procedural catalog, the explicit opening
`FacilityDefinitionId -> ModelId` bindings, historical Facility-instance overrides, palace/office/academy/
ritual/public/resource/service/agriculture model families, the `LUOYANG KIT` review camera, or its full-catalog
visual evidence, read `Docs/TASK_LUOYANG_FACILITY_MODEL_COVERAGE_AND_A_TIER_COMPOSITION_V1.md` together with
the first-batch model-kit, Luoyang historical-city and playable-vertical-slice routes.

The coverage catalog is presentation-only over the protected 2,084 opening Facilities. It does not create or
move Facilities, change ownership/construction/save facts, introduce SubCells, authorize ordinary reproduction
of historical-only palace assets, or claim final FBX, baked textures, production LOD, damage or city streaming.

### Current Luoyang production-building modular kit V1 route

For work that changes the ten high-frequency production profiles, cached procedural meshes, placement/entrance
anchors, three-tier LODs or the production-building visual evidence, read
`Docs/TASK_LUOYANG_PRODUCTION_BUILDING_MODULAR_KIT_AND_HIGH_FREQUENCY_CITY_FABRIC_V1.md` together with the
Facility model coverage and first-batch buildable-model routes.

The kit reuses stable Model IDs and does not change Facility facts, construction settlement, ownership, saves or
restricted availability. It covers 1,800 of the 2,084 opening Facilities through ten high-frequency definitions;
representative-block performance and final artist assets remain deferred.

### Current Luoyang A-tier historical-landmark silhouettes V1 route

For work that changes the ten Facility-bound silhouettes for South Palace, North Palace, Yongan Palace, Taixue,
Mingtang, Biyong, Lingtai, Taicang, Arsenal or Zhuolong Garden, their historical metadata, authoritative Cell
placements, three LODs, LANDMARKS review camera or evidence, read
`Docs/TASK_LUOYANG_A_TIER_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1.md` together with the historical-city,
Facility-coverage and production-building routes.

These are presentation profiles over existing Facilities. They cannot move/create Facilities, broaden historical
availability or claim archaeological reconstruction. The silhouettes are original strategic V1 assets, not final
FBX or baked textures.

### Current Luoyang twelve city and palace gate identity V1 route

For work that changes the twelve named city-gate identities, North Palace South Gate, South Palace North Gate,
their Facility IDs, authoritative Cells, source/derived directions, rotations, gatehouse silhouettes, passage
anchors, three LODs, GATES camera or evidence, read
`Docs/TASK_LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1/README.md` together with the
historical-city and Facility-model routes.

The scope is exactly twelve `facility.fortification.city_gate` Facilities plus two
`facility.fortification.palace_gate` Facilities; four generic recommended `facility.military.gate` Facilities are
excluded. City-gate facing comes from authoritative `gate_direction`. Both palace-gate directions are null in the
source data, so presentation derives south/north facing only from their display names and never writes it back as
world truth. Identity availability is restricted to Government/Military/HistoricalInit/Event and does not inherit
Player/Ai construction permission from the generic city-gate base model. The strategic silhouettes and 1.65x
review-only scale are not archaeological dimensions. The next building route is medium-frequency urban fabric,
then whole-city draw-call/LOD/occlusion/streaming performance acceptance.

### Current Luoyang medium-frequency urban-fabric V1 route

For work that changes the five market/shopfront, caravan-yard, school, local-office or military-camp production
profiles, their exact opening Definition counts, urban-fabric roles, street interfaces, three LODs, the 15-Cell
`FABRIC` review camera or evidence, read
`Docs/TASK_LUOYANG_MEDIUM_FREQUENCY_URBAN_FABRIC_V1.md` together with the Facility-coverage and
production-building routes.

This Presentation/content-data kit covers 158 actual opening Facilities and raises high-plus-medium production
profile coverage to 1,958/2,084. Its review Cells are PreviewOnly and do not claim Facility positions. It cannot
create or move Facilities, broaden the base model availability, change ownership/construction/save facts, or
authorize whole-city per-object instantiation. Canals, wells and bridges remain infrastructure work. The subsequent
whole-city performance route below has passed its targeted acceptance.

### Current Luoyang whole-city building performance and batching V1 route

For work that changes the 2,084-Facility lightweight presentation plan, 8x8 building spatial batches, the densest
24x24 review window, LOD2 module extraction, material-grouped combined meshes, the `BATCH` camera/button, building
renderer/vertex/build-time budgets, cleanup behavior, metrics JSON or visual evidence, read
`Docs/TASK_LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1.md` together with the Facility-coverage,
production-building, landmark, gate and medium-frequency routes.

The status is `IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`. The whole-city plan contains exactly
2,084 actual opening Facilities in 64 presentation batches; the densest review window contains 549 Facilities in
9 batches. After the final civic/ritual/medical production closure, its 1,669 LOD2 source modules combine into 93
single-material Renderers/Meshes and 17,476 vertices in 27.0894ms in the latest targeted Unity Editor regression, a 94.43%
renderer reduction. The 8x8 batch and 24x24 review window are
Presentation-only and must not become world, Region, administrative, simulation, persistence or final streaming
boundaries. This acceptance is not final platform GPU, Addressables, baked occlusion or final-art proof. The next
building route, infrastructure production for the 19 canals, 16 wells and 2 bridges, has now passed targeted
acceptance.

### Current Luoyang canal, well and bridge infrastructure production V1 route

For work that changes the 19 actual canals, 16 wells or 2 bridges, their infrastructure profiles, permissions,
four-neighbor waterway topology, connection/service anchors, three LODs, whole-city batch integration, the three
`INFRA` review cameras or evidence, read
`Docs/TASK_LUOYANG_CANAL_WELL_BRIDGE_INFRASTRUCTURE_PRODUCTION_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_CANAL_WELL_BRIDGE_INFRASTRUCTURE_PRODUCTION_V1/README.md` together
with the Facility-model and whole-city performance routes.

The status is `IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`. The scope is exactly 37 actual
opening Facilities on 37 authoritative Cells. The 19 canals plus 2 bridges form exactly two four-neighbor waterway
components with four endpoints and seventeen straight interior nodes; the 16 wells remain isolated point
facilities. Derived topology is Presentation-only and cannot become water-flow, irrigation, road, ownership,
construction, simulation or persistence truth. Production-profile coverage is 1,995/2,084. The subsequent
low-frequency defensive route below has passed targeted acceptance.

### Current Luoyang low-frequency defense production V1 route

For work that changes the 12 named city gates, 2 palace gates, 4 generic military gates, 7 fortified manors or
3 beacons, their identity-reuse boundary, defense profiles, permissions, Presentation-only generic-gate facing,
three LODs, whole-city batch integration, the three `DEFENSE` review cameras or evidence, read
`Docs/TASK_LUOYANG_LOW_FREQUENCY_DEFENSE_PRODUCTION_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_LOW_FREQUENCY_DEFENSE_PRODUCTION_V1/README.md` together with the gate,
Facility-model and whole-city performance routes.

The status is `IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`. The scope is exactly 28 actual
opening Facilities on 28 authoritative Cells. Fourteen named gates reuse their existing identity assets; only the
4 generic military gates, 7 fortified manors and 3 beacons use the three new procedural production profiles.
The generic-gate south facing and static signal fire are Presentation-only and cannot become direction, alarm,
combat, garrison, construction, simulation or persistence truth. Production-profile coverage is 2,023/2,084.
The resource/agriculture route and final civic/ritual/medical closure below have now passed targeted acceptance.

### Current Luoyang resource and agriculture production V1 route

For work that changes the 9 forestry sites, 6 quarries, 5 mines or 6 rice fields, their exact Facility/Definition
disambiguation, production profiles, permissions, evidence boundary, three LODs, whole-city batch integration,
the four `RESOURCES` review cameras or evidence, read
`Docs/TASK_LUOYANG_RESOURCE_AND_AGRICULTURE_PRODUCTION_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_RESOURCE_AND_AGRICULTURE_PRODUCTION_V1/README.md` together with
the Facility-model and whole-city performance routes.

The status is `IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`. The scope is exactly 26 actual
opening Facilities on 26 authoritative Cells. Quarry and mine Facilities share one base Model ID but use distinct
Facility/Definition-bound variants; unknown Model-only bindings must not guess. Their generic Han production form
is Gameplay Reconstruction, not archaeological proof and not a resource-body, extraction, irrigation, crop-growth,
inventory or settlement fact. Production-profile coverage is 2,049/2,084. The subsequent final closure route has
passed targeted acceptance.

### Current Luoyang final civic, ritual and medical production closure V1 route

For work that changes the final 35 opening Facilities, the 10 historical-landmark reuse bindings, the 9 clinics,
6 generic ritual halls, 4 courtyards, 4 plazas or 2 central offices, their five procedural variants, permissions,
three LODs, whole-city batch integration, the four `CIVIC` review cameras or evidence, read
`Docs/TASK_LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1/README.md`
together with the landmark, Facility-model and whole-city performance routes.

The status is `IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`. Exactly 10 named Facilities reuse
their existing landmark geometry and metadata by Facility ID; 25 ordinary Facilities use five new procedural
profiles. Mingtang/Biyong remain distinct from six generic ritual halls. Courtyard and plaza share a base Model ID
but resolve to separate variants by Facility/Definition; unknown Model-only bindings must not guess. Production
coverage is 2,084/2,084. This is visual production coverage only and cannot become medical, ritual, administrative,
ownership, construction, simulation or persistence truth, nor claim final FBX, archaeological reconstruction,
platform performance or complete art acceptance. The next building route is whole-city visual review and a
replaceable final-asset priority manifest below, not another base-coverage increment.

### Current Luoyang whole-city visual review and replaceable final-asset manifest V1 route

For work that changes the 54 actual runtime Asset Variant replacement slots, their P0/P1/P2/P3 priority,
visual-readiness audit groups, representative Facilities, stable replacement identity, license intake boundary,
the `ASSET QA` review board, its four cameras or evidence, read
`Docs/TASK_LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1/README.md`
together with the whole-city performance and final production-closure routes.

The current status is `IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`. Runtime resolution maps all 2,084 opening
Facilities to 54 distinct actual Asset Variants: 24 P0 identity slots, 10 P1 high-exposure slots, 14 P2
system-readable slots and 6 P3 supporting-context slots. Nine `REUSE_*` declarations are intentionally excluded
because the runtime selects the underlying named gate or landmark Asset Variant. Replacement must keep the stable
Model/Asset/Profile/Facility identity and the procedural fallback, and any external candidate requires a complete
license/source record. Compilation, targeted core 1/1, target EditMode 3/3 and graphical PlayMode 1/1 passed;
affected whole-city batching EditMode 3/3 and graphical PlayMode 1/1 also passed, with four 1600x1000 review images.
This manifest is not proof that final FBX, textures or art acceptance already exist. The next route is a four-asset
P0 replacement vertical slice for South Palace, Mingtang, Guangyangmen and North Palace South Gate rather than bulk
 replacement.

### Current Luoyang P0 four-asset final-art vertical slice V1 route

For work that changes South Palace, Mingtang, Guangyangmen or North Palace South Gate final-art candidates,
their six-material set, three LODs, stable anchors, Resources prefab intake paths, FBX targets, procedural fallback,
world-batch LOD2 modules, `P0 SLICE` review board, five fixed cameras or evidence, read
`Docs/TASK_LUOYANG_P0_FINAL_ASSET_FOUR_PIECE_VERTICAL_SLICE_V1.md` and the screenshot README under
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FINAL_ASSET_FOUR_PIECE_VERTICAL_SLICE_V1/` together with the
whole-city final-asset manifest and performance routes.

The predecessor status was `INTEGRATION_CANDIDATE_VERIFICATION_PASSED_ARTIST_SOURCE_PENDING_USER_REVIEW` and
has been superseded by the native-prefab delivery route below.
Facility/Model/Asset/Profile identity, Global Cell, historical confidence, availability and simulation remain frozen.
Runtime first attempts the contracted artist prefab and otherwise uses a project-original three-LOD candidate;
invalid present prefabs fail explicitly. Compilation, targeted core 1/1, target EditMode 4/4, graphical PlayMode
1/1 and affected whole-city batching graphical PlayMode 1/1 passed, with five 1600x1000 review images. The complete core suite did not finish within the 300-second gate and
must not be reported as passed. At that stage no approved FBX/prefab/texture source was present. The native-prefab
delivery below now supplies review prefabs, while all four `FinalArtApproved` flags remain false. Bulk replacement
of the other 50 slots is still not authorized.

### Current Luoyang P0 four-piece native prefab art delivery V1 route

For work that changes the project-original Unity-native prefabs, six material assets, four shared mesh assets,
deterministic editor builder, three populated LODs, stable anchors, prefab-first runtime status or the five new
review images for South Palace, Mingtang, Guangyangmen and North Palace South Gate, read
`Docs/TASK_LUOYANG_P0_FOUR_PIECE_NATIVE_PREFAB_ART_DELIVERY_V1.md` together with the preceding P0 vertical-slice,
whole-city final-asset manifest and performance routes.

The current status is `READY_FOR_USER_REVIEW_FINAL_ART_APPROVAL_PENDING`. All four frozen Resources paths now
contain real Unity prefabs; runtime verification loads all four without activating the procedural fallback. Each
prefab has exactly three populated LODs, materials, every catalog anchor and no Collider. Compilation, targeted
core 1/1, native asset builder EditMode 1/1, existing P0 EditMode 4/4, graphical PlayMode 1/1 and affected
whole-city batching graphical PlayMode 1/1 passed, with five new 1600x1000 images. The latest batching result keeps
1,673 source modules at 97 renderers, 17,512 vertices and 24.0398ms. Identity, Global Cell, history, availability,
simulation and saves remain unchanged. These
assets are project-original review candidates, not archaeological reconstructions or user-approved final art;
there are no independent FBX/DCC sources or hand-authored textures yet, and all `FinalArtApproved` flags remain
false. The next route is user review and four-piece iteration/final-source archival, not bulk replacement of the
remaining 50 slots.

### Current Luoyang P0 four-piece visual refinement and review readability V2 route

For work that changes the refined South Palace, Mingtang, Guangyangmen or North Palace South Gate silhouettes,
their roof-ridge/eave/gate/stair/paving/barbican/que/banner details, V2 native-prefab recipe, strict LOD reduction,
close-up building-bounds framing, five V2 review images or user-review gate, read
`Docs/TASK_LUOYANG_P0_FOUR_PIECE_VISUAL_REFINEMENT_AND_REVIEW_READABILITY_V2.md` and the README under
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_VISUAL_REFINEMENT_AND_REVIEW_READABILITY_V2/`
together with the V1 native-prefab, P0 vertical-slice, whole-city manifest and batching routes.

The current status is `REFINED_NATIVE_PREFAB_V2_READY_FOR_USER_REVIEW_FINAL_APPROVAL_PENDING`. The deterministic
builder produces 4 prefabs, 6 materials and 4 shared meshes with 137/37/21 total LOD renderers. Targeted core 1/1,
V2 EditMode 2/2, native-prefab contract EditMode 1/1, existing P0 EditMode 4/4, graphical five-view PlayMode 1/1
and the 549-facility batching graphical PlayMode 1/1 passed. Runtime loads all four prefabs without fallback, while
all `FinalArtApproved` flags remain false. Identity, Global Cell, history, permissions, simulation and saves remain
frozen. The next route is explicit per-piece user review; final approval/DCC-FBX archival and bulk replacement of
the remaining 50 slots are not authorized yet.

### Current Luoyang P0 four-piece multi-angle turntable review pack V1 route

For work that changes the four-piece review-camera matrix, front/rear/low-oblique framing, runtime piece/angle
controls, stable review camera IDs, 13-image evidence pack or the explicit per-piece user-decision gate, read
`Docs/TASK_LUOYANG_P0_FOUR_PIECE_MULTI_ANGLE_TURNTABLE_REVIEW_PACK_V1.md` and the README under
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_MULTI_ANGLE_TURNTABLE_REVIEW_PACK_V1/` together with
the V2 refinement, native-prefab, P0 vertical-slice, whole-city manifest and batching routes.

The current status is `MULTI_ANGLE_REVIEW_PACK_V1_READY_FOR_USER_DECISION_FINAL_APPROVAL_PENDING`. The review
contract fixes 4 pieces x 3 angles as 12 distinct stable cameras, keeps the four existing front-oblique views and
adds rear-oblique and low-oblique views. Runtime exposes overview, piece and angle cycling without changing geometry,
materials, prefabs, LODs, anchors, authoritative Cells, gameplay or saves. Compilation, targeted core 1/1,
multi-angle EditMode 2/2, graphical 13-view PlayMode 1/1, existing V2 five-view PlayMode 1/1 and the densest
549-facility batching graphical PlayMode 1/1 passed. All four `FinalArtApproved` flags remain false. The next route
is explicit accept/change/reject review for each piece; final approval/DCC-FBX archival and bulk replacement of the
remaining 50 slots are not authorized yet.

### Current Luoyang P0 four-piece review decision board V1 route

For work that changes the lossless three-view comparison boards, their source/output SHA-256 manifest, per-piece
review criteria, reply template or the explicit pending decision gate, read
`Docs/TASK_LUOYANG_P0_FOUR_PIECE_REVIEW_DECISION_BOARD_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_REVIEW_DECISION_BOARD_V1/README.md` together with the
multi-angle review pack and V2 refinement routes.

The current status is `P0_FOUR_PIECE_REVIEW_DECISION_BOARDS_V1_READY_FOR_USER_DECISION_FINAL_APPROVAL_PENDING`.
The deterministic PowerShell builder arranges the 12 verified 1600x1000 Game Views into four 3000x900 per-piece
boards using proportional scaling only: no crop, color change, repaint or generative modification. Its timestamp-free
manifest records all input and output paths, dimensions and SHA-256 values. Initial generation, 4-piece/12-source/
4-board manifest verification, identical hashes across a repeated build and visual inspection of all four boards
passed. This route changes no Unity asset or world fact. The user subsequently accepted the complete four-piece set;
the acceptance and source-readiness route below now owns current status.

### Current Luoyang P0 four-piece user acceptance and source archive readiness V1 route

For work that changes the recorded all-four user acceptance, decision/status IDs, Unity-native source/GUID hash
archive, four frozen FBX target audits, runtime accepted-versus-final distinction or the remaining source gate, read
`Docs/TASK_LUOYANG_P0_FOUR_PIECE_USER_ACCEPTANCE_AND_SOURCE_ARCHIVE_READINESS_V1.md`,
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_USER_ACCEPTANCE_AND_SOURCE_ARCHIVE_READINESS_V1/README.md`
and `Assets/ArtSource/Han/Luoyang/P0Final/README.md` together with the decision-board, multi-angle, V2 refinement,
native-prefab and original P0 vertical-slice routes.

The historical pre-FBX status was
`LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_UNITY_NATIVE_SOURCE_ARCHIVED_INDEPENDENT_DCC_FBX_REQUIRED_FINAL_ACTIVATION_PENDING`.
The user's 2026-08-27 acceptance is recorded for South Palace, Mingtang, Guangyangmen and North Palace South Gate.
The deterministic archive covers the generator, P0 catalog, four prefabs, six materials, four meshes and all matching
`.meta` files: 32 existing files with paths, lengths and SHA-256 values. Compilation, targeted core 1/1, native-prefab
EditMode 1/1, archive EditMode 1/1, existing P0 EditMode 4/4, graphical 13-view PlayMode 1/1 and graphical densest-
549 batching PlayMode 1/1 passed. All four frozen FBX targets are absent and no local Blender, Assimp, FBX converter
or Unity FBX Exporter is available. The task does not fabricate source files: all `FinalArtApproved` flags remain
false until genuine independent DCC/FBX sources pass consistency validation, unless the user explicitly changes that
older gate. Bulk replacement of the other 50 slots remains unauthorized.

### Current Luoyang P0 four-piece FBX source freeze and final activation V1 route

For work that changes the four frozen FBX sources, Unity FBX Exporter package/version, export-only anchor markers,
dot-to-underscore anchor-name mapping, Unity reimport consistency validation, final source hash manifest, runtime
approval fallback behavior or the four `FinalArtApproved` flags, read
`Docs/TASK_LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1.md`,
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1/README.md`
and `Assets/ArtSource/Han/Luoyang/P0Final/README.md` together with the preceding acceptance, V2 refinement,
native-prefab, original P0 vertical-slice and whole-city batching routes.

The current status is `LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`.
Unity FBX Exporter 4.2.1 and its Autodesk FBX SDK Unity binding 4.2.1 are frozen in the package manifest/lock under
the Unity Companion License. South Palace, Mingtang, Guangyangmen and North Palace South Gate now have real FBX
sources at the four frozen paths. Unity reimport verifies all three named LOD hierarchies, renderer counts, materials,
reversible anchor mappings/positions, geometry bounds and no Colliders. The final archive covers 42 project source/
`.meta` files, two package toolchain files and four FBX hashes. All four catalog flags are `FinalArtApproved=true`,
but a runtime instance reports approval only when the real prefab loads; procedural fallback remains unapproved.
This closes only the accepted strategic-map four-piece V2 slice and does not claim archaeological reconstruction,
hand-authored/PBR textures or authorization to bulk-replace the remaining 50 slots.

### Current Luoyang P0 landmark second-batch native Prefab/FBX review V1 route

For work that changes the next lowest-order remaining P0 landmark selection, North Palace/Yongan Palace/Taixue/
Biyong candidate geometry, P0Batch2 Prefab or FBX paths, second-batch review cameras, pending approval state,
source manifest or runtime fallback, read
`Docs/TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md`,
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1/README.md` and
`Assets/ArtSource/Han/Luoyang/P0Batch2/README.md` together with the whole-city final-asset manifest, A-tier landmark,
first-batch final activation and whole-city batching routes.

The historical source-ready status was
`LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_SOURCE_READY_FOR_USER_REVIEW_V1`. The frozen selection is
review order 1/2/3/5. Four project-original native Prefabs and four reimport-validated FBX sources have three nonempty
LODs, stable anchors, complete materials and no Colliders. Runtime loads all four without fallback; five graphical
review views, the densest 549-facility batching regression and the approved first-batch regression passed. The source
manifest froze 54 source/dependency files plus two toolchain lock files. The user later accepted all four, so current
approval and source-manifest status is owned by the final-activation route below. Third-batch work and the remaining
46 slots are not authorized.

### Current Luoyang P0 landmark second-batch multi-angle review and decision boards V1 route

For work that changes the North Palace/Yongan Palace/Taixue/Biyong 4x3 camera matrix, PreviewOnly review cells,
runtime piece/angle controls, 13-image review evidence, lossless per-piece boards, board SHA-256 manifest or explicit
pending user-decision gate, read
`Docs/TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1/README.md`
together with the second-batch native Prefab/FBX, whole-city manifest and batching routes.

The historical decision-input status is
`LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_DECISION_BOARDS_READY_FOR_USER_DECISION_V1`. The review contract
fixes 4 pieces x 3 angles as 12 distinct camera IDs and exposes overview, piece and angle cycling. PreviewOnly review
instances use the already validated flatter review cells; authoritative Facilities and Global Cells are unchanged.
One overview, twelve 1600x1000 Unity Game Views and four 3000x900 no-crop/no-color-change boards passed automated
safe-framing/clear-sightline checks and visual review. Compilation, targeted core 1/1, EditMode 2/2, graphical
13-view PlayMode 1/1, existing second-batch five-view PlayMode 1/1 and densest-549 batching PlayMode 1/1 passed.
Repeated board generation produced identical hashes for all four PNGs and the timestamp-free manifest. The boards
retain their pre-decision `PENDING/false` labels as immutable historical inputs. The user later replied “accept all”; the
current state is the final-activation route below. Third-batch work and the remaining 46 slots are not authorized.

### Current Luoyang P0 landmark second-batch user acceptance and final activation V1 route

For work that changes the accepted North Palace/Yongan Palace/Taixue/Biyong decision record, the four static
`FinalArtApproved` flags, accepted-source manifest, real-Prefab runtime approval, procedural-fallback denial or the
remaining-slot boundary, read
`Docs/TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`,
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1/README.md`
and `Assets/ArtSource/Han/Luoyang/P0Batch2/README.md` together with the two preceding second-batch routes and the
whole-city final-asset manifest.

The current status is
`LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`. On 2026-08-27
the user replied “accept all” after the four multi-angle decision boards; this is frozen as
`decision.luoyang-p0-landmark-second-batch.accepted.2026-08-27.v1`. All four project-original native Prefabs and
real FBX sources were already reimport-validated for three nonempty LODs, materials, reversible anchors, geometry
bounds and zero Colliders, so activation required no model rebuild. The regenerated manifest freezes 54 project
source/dependency files, two toolchain files and four FBX sources; its SHA-256 is
`9b380964802400ef7a96838b758b68be48df8063e0380d7b3712c1301baa3142`. All four static flags are
`FinalArtApproved=true`, while a runtime instance is approved only if the real Prefab loads; procedural fallback is
always unapproved. Compilation, targeted core 1/1, the targeted fallback and accepted-FBX EditMode tests, real-Prefab
five-view PlayMode and densest-549 batching PlayMode passed. The eight first- and second-batch slots are activated;
the third batch was later separately authorized and is now owned by the routes below. The other 42 slots remain
unauthorized.

### Current Luoyang P0 landmark third-batch native Prefab/FBX review V1 route

For work that changes the Lingtai/Taicang/Arsenal/Zhuolong Garden candidate geometry, P0Batch3 Prefab or FBX
paths, review-order 6/7/8/9 identities, third-batch review cameras, pending approval state, source manifest or runtime
fallback, read `Docs/TASK_LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md`,
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1/README.md` and
`Assets/ArtSource/Han/Luoyang/P0Batch3/README.md` together with the whole-city final-asset manifest, A-tier landmark,
second-batch final-activation and whole-city batching routes.

The current status is `LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_SOURCE_READY_FOR_USER_REVIEW_V1`.
The frozen selection is review order 6/7/8/9: Lingtai, Taicang, Arsenal and Zhuolong Garden. Four project-original
native Prefabs and four Unity-reimported FBX sources have three nonempty strictly decreasing LODs, stable anchors,
complete materials and no Colliders. Runtime loads the four real Prefabs under a separate third-batch identity while
keeping both real and fallback instances unapproved. One overview and four 1600x1000 graphical review views passed
visual inspection; compilation, targeted core 1/1, targeted EditMode tests, five-view PlayMode 1/1 and densest-549
batching PlayMode 1/1 passed. The source manifest freezes 60 project source/dependency files, two toolchain files and
four FBX sources; its SHA-256 is `8d286a6013c9c83c111c2c57b8e9f3fac071de5d82acdaee8c71cf0243a5d444`.
All four flags were `FinalArtApproved=false` in this historical review input. The user subsequently accepted all four
from the five-view package; current approval and source-manifest status is owned by the final-activation route below.
The fourth batch and the other 42 untouched slots are not authorized.

### Current Luoyang P0 landmark third-batch user acceptance and final activation V1 route

For work that changes the accepted Lingtai/Taicang/Arsenal/Zhuolong Garden decision record, the four static
`FinalArtApproved` flags, accepted-source manifest, real-Prefab runtime approval, procedural-fallback denial or the
remaining-slot boundary, read
`Docs/TASK_LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`,
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1/README.md`
and `Assets/ArtSource/Han/Luoyang/P0Batch3/README.md` together with the preceding third-batch review route and the
whole-city final-asset manifest.

The current status is
`LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`. On 2026-08-27 the
user replied “accept” after the five-view review package; in that four-piece context this is frozen as
`decision.luoyang-p0-landmark-third-batch.accepted.2026-08-27.v1` and `ACCEPTED_ALL_FOUR`. The explicit user decision
supersedes the planned additional multi-angle evidence gate for these four pieces only. All four static flags are
`FinalArtApproved=true`, while runtime approval still requires a real Prefab and procedural fallback is always
unapproved. The accepted manifest freezes 60 project source/dependency files, two toolchain files and four FBX
sources; its SHA-256 is `40e1ccad3af83e9b16119df73b435bc2ae1d9b46c97af9db5087904a53fc50c2`.
The first three four-piece batches now activate 12 of 54 slots; the fourth batch and the other 42 slots require a
separately authorized finite selection.

### Historical Luoyang P0 named-gate fourth-batch native Prefab/FBX review V1 route

For work that changes the Gumen/Jinmen/Kaiyangmen/Maomen candidate geometry, P0Batch4 Prefab or FBX paths,
review-order 11/12/13/14 identities, authoritative gate facings and passage anchors, fourth-batch cameras, pending
approval state, source manifest or runtime fallback, read
`Docs/TASK_LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1.md`,
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1/README.md`
and `Assets/ArtSource/Han/Luoyang/P0Batch4/README.md` together with the gate identity kit, whole-city final-asset
manifest, third-batch final activation and whole-city batching routes.

The historical candidate status is
`LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_SOURCE_READY_FOR_USER_REVIEW_V1`. The user separately
authorized this finite fourth-batch candidate production after the first three four-piece batches were activated.
Review order 10 Guangyangmen is skipped because it is already activated in the first batch; the frozen selection is
11/12/13/14. These four project-original native Prefabs and real FBX sources must preserve the existing gate identity,
Global Cell, visual facing and placement/outer/inner passage anchors. Both real and fallback instances remain
unapproved until a later explicit user decision. This route does not authorize a fifth batch or the other 38 untouched
slots.
The four native Prefabs and four Unity-reimported FBX sources now have three nonempty strictly decreasing LODs,
stable placement/outer/inner passage anchors, complete materials and no Colliders. Runtime real-Prefab and forced
fallback tests both preserve `FinalArtApproved=false`. Five graphical views and the densest-549 batching regression
passed. The manifest freezes 56 project source/dependency files, two toolchain files and four FBX sources; its
SHA-256 was `a709f0b53267a0630fcb8fb207fca908484db13b6c3aedf898d2608878d40785` before the
accepted-source manifest replaced it. Use the final-activation route below for current approval and archive facts.

### Current Luoyang P0 named-gate fourth-batch final activation V1 route

For work that changes the accepted Gumen/Jinmen/Kaiyangmen/Maomen decision record, their four static
`FinalArtApproved` flags, accepted-source manifest, real-Prefab runtime approval, procedural-fallback denial or the
remaining-slot boundary, read
`Docs/TASK_LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`,
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1/README.md`
and `Assets/ArtSource/Han/Luoyang/P0Batch4/README.md` together with the preceding fourth-batch candidate route,
gate identity kit, whole-city final-asset manifest and batching route.

The current status is
`LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`. On 2026-08-27
the user said “accept the previous one” in the four-piece fourth-batch context; this is frozen as
`decision.luoyang-p0-named-gate-fourth-batch.accepted.2026-08-27.v1` and `ACCEPTED_ALL_FOUR`. That explicit
decision supersedes the planned additional multi-angle review gate for these four pieces only. All four static flags
are true; runtime real-Prefab instances are approved while procedural fallback instances remain false. First through
fourth batches now total 16/54 activated slots, with 38 unapproved. This route does not authorize a fifth batch.
The accepted manifest freezes 56 project source/dependency files, two toolchain files and four reimport-validated
FBX sources; its SHA-256 is `20c8981a1597314a38a4e211e3a970f22875534d35c48ade33e2b317aaf9c87b`.

This 16/54 boundary is now historical. The user subsequently authorized and preaccepted all remaining 38 slots;
use the completion route below for the current whole-city final-asset state.

### Current Luoyang remaining-38 preaccepted final-asset completion V1 route

For work that changes review orders 15-21 or 23-53, the remaining-38 decision record, their native Prefabs,
materials, meshes, FBX sources, three-LOD/stable-anchor contract, real-Prefab approval, procedural-fallback denial,
source manifest or the current 54/54 completion boundary, read
`Docs/TASK_LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1.md`,
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1/README.md`
and `Assets/ArtSource/Han/Luoyang/FinalRemaining/README.md` together with the whole-city final-asset manifest,
all eight source-kit routes and the whole-city batching route.

The current status is
`LUOYANG_REMAINING_38_USER_PREACCEPTED_NATIVE_PREFAB_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`. On
2026-08-27 the user explicitly requested direct development of all remaining 38 slots without per-piece approval;
this is frozen as `decision.luoyang-remaining-38.preaccepted.2026-08-27.v1` and
`PREACCEPTED_ALL_REMAINING_38`. The selection contains 8 P0, 10 P1, 14 P2 and 6 P3 slots covering 2,068
Facilities. All 38 have project-original native Prefabs, three populated LODs, stable anchors, real Unity-reimported
FBX sources and static approval. Runtime approval remains conditional on successful real-Prefab loading; procedural
fallback is always unapproved. Together with the preceding 16 assets, the current completion state is 54/54 slots
and 2,084/2,084 opening Facilities, with zero remaining final-asset slots. Do not invent a fifth batch. This route
does not claim archaeological reconstruction, PBR texture finals, interiors, collision, navigation or animation.
The source manifest freezes 240 project source/metadata records and 38 FBX sources; its SHA-256 is
`19d27e5ac9f287c4ad841fe65db7db300f9a07f873d744d2ad914dd049091612`.

### Current Luoyang actual whole-city composition and terrain integration V1 route

For work that changes the 2,084 Facility Visual Local Anchors, six presentation district IDs, nearest-road frontage,
road/canal/wall adjacency, terrain-grounded dense-549 window, composition scale matrix, CITY review status or the
no-SubCell boundary, read
`Docs/TASK_LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1.md` and
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1/README.md`
together with the whole-city batching, final-asset manifest and remaining-38 completion routes.

The current implementation status is
`LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`.
It deterministically maps all 2,084 opening Facilities and all 54 final Asset Variants into six presentation
districts without changing Facility, Global Cell, ownership, construction, population or Save Schema. Corridor
Facilities remain on Cell centers and derive four-way connection shapes from adjacent real Facilities; ordinary
Facilities receive a bounded Cell-local frontage toward the nearest real road. The dense 549-Facility window uses
the offset global coordinate for the existing terrain height sampler and remains inside the established 8x8
presentation batching budget. This route does not freeze the nationwide art style, supply a high-resolution Luoyang
DEM, create simulation SubCells, or implement collision, navigation, interiors, damage animation or supply-region
materialization.

### Current Luoyang Cell traversal ports and human-scale movement V1 route

For work that changes the authoritative four-way Cell traversal contract, internal Cell topology, movement
capabilities, Facility access requirements, traversal metrics, CellRoute planning, MovePersonCommand integration,
V77 local-segment compatibility, or Unity expansion of CellRoute into a presentation path, read
`Docs/TASK_LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1.md`,
`Docs/LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1_ACCEPTANCE_REPORT.md` and
`Docs/Evidence/LuoyangCellTraversalV1/existing-spatial-audit.md` together with the human-scale local-map task,
formal player-movement task and deterministic save contract.

The current status is `LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1_ACCEPTED`. All 5,980 Luoyang
Cells have four potential cardinal ports and all 2,084 Facilities have a traversal/access profile. The formal data
contains 359 Road Facilities, 18 Gate-type Facilities and 2 Bridges. Existing road frontage supports 18
`RoadRequired` Facilities; unserved warehouse/granary records remain `Optional` rather than inventing roads.
`CellTraversalPlanner + CellRoute` is now the cross-Cell movement authority. The previous LocalNav graph remains
only for presentation geometry and old V77 segment compatibility. The Save Schema stays V77 because existing
segment fields already preserve formal-object conditions, Cells and centimetre coordinates. Verification passed
targeted core 8/8 and 17/17, frozen complete core 774/774, Unity EditMode 3/3 and graphical PlayMode 1/1 with zero
introduced regressions. The fixed next order is food-inventory conservation RCA/fix, followed by Luoyang external
supply area and city logistics V1; do not continue by creating a second local spatial authority.
