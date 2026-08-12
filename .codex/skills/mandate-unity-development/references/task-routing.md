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
