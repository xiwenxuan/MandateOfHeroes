import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const artifactEntry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY || "@oai/artifact-tool";
const { FileBlob, SpreadsheetFile } = await import(pathToFileURL(artifactEntry).href);
const repo = "E:/project/gamedevelop/MandateOfHeroes";
const registryDir = path.join(repo, "Docs/KNOWLEDGE_BASE/REGISTRY");
const evidenceDir = "Docs/HISTORICAL_WORLD_REFERENCE/HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_REFERENCE";
const taskPath = "Docs/TASK_HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_NETWORK_V1.md";
const validationDir = path.join(repo, evidenceDir, "VALIDATION/registry_updates");
await fs.mkdir(validationDir, { recursive: true });

const taskId = "HAN-135-260-COUNTY-PRODUCTION-RESOURCE-INDUSTRY-TRADE-AND-SUPPLY-NETWORK-V1";
const commonDoc = { Domain: "WorldSimulation", SubDomain: "CountyProductionEconomyReference", AuthorityLevel: "L3", Status: "CURRENT", CreatedOrKnownDate: "2026-08-11", LastKnownRevision: "2026-08-11", CanonicalFor: "DevelopmentReferenceOnly", RelatedTasks: taskId, RelatedRuntimeSystems: "Mandate.Domain|Mandate.Simulation|Mandate.Persistence", HistoricalValue: "HIGH", RecommendedReader: "HistoricalResearch|DataEngineer|WorldDesigner|SimulationEngineer", ReadPriority: "P0", ConflictNotes: "Does not replace L1 rules or Cell/Facility/Worker/Inventory runtime facts.", ActionRequired: "Materialize only after county-specific readiness review.", CanonicalScope: "Han135260CountyProductionEconomyReferenceV1" };
const docs = [
  ["task", taskPath, "National county production economy task record", "TaskRecord"],
  ["readme", `${evidenceDir}/README.md`, "National county production economy reference entry", "ReferenceEntry"],
  ["report", `${evidenceDir}/HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_NETWORK_V1_REPORT.md`, "National county production economy final report", "AcceptanceReport"],
  ["standard", `${evidenceDir}/COUNTY_PRODUCTION_ECONOMY_STANDARD.md`, "County production economy standard", "CanonicalReference"],
  ["taxonomy", `${evidenceDir}/PRODUCT_TAXONOMY.md`, "Product taxonomy and open content IDs", "DataStandard"],
  ["units", `${evidenceDir}/HISTORICAL_UNIT_CONVERSION.md`, "Historical unit conversion policy", "DataStandard"],
  ["master", `${evidenceDir}/COUNTY_PACKS/county_economy_master_v1.json`, "1182 county economy machine-readable master", "DataMaster"],
  ["validation", `${evidenceDir}/VALIDATION/data_validation_report.json`, "County economy data validation report", "ValidationEvidence"],
  ...Array.from({ length: 40 }, (_, i) => {
    const n = String(i + 1).padStart(2, "0");
    const names = ["COUNTY_IDENTITY_GEOGRAPHY_MASTER","COUNTY_POPULATION_WORKFORCE_MASTER","COUNTY_LAND_AGRICULTURAL_POTENTIAL","COUNTY_WATER_IRRIGATION_REFERENCE","COUNTY_CROP_MIX_AND_YIELD","COUNTY_AGRICULTURAL_OUTPUT","COUNTY_LIVESTOCK_REFERENCE","COUNTY_FORESTRY_FUEL_REFERENCE","COUNTY_FISHERY_GATHERING_REFERENCE","COUNTY_MINERAL_REFERENCE","COUNTY_SALT_REFERENCE","COUNTY_RAW_MATERIAL_OUTPUT","COUNTY_FOOD_PROCESSING_CAPACITY","COUNTY_BREWING_CAPACITY","COUNTY_METALLURGY_CAPACITY","COUNTY_METALWORKING_CAPACITY","COUNTY_TEXTILE_SILK_CAPACITY","COUNTY_LEATHER_WOODWORK_CAPACITY","COUNTY_POTTERY_BUILDING_MATERIAL_CAPACITY","COUNTY_MEDICINE_SPECIAL_CRAFT","COUNTY_CART_AND_SHIPBUILDING","COUNTY_MILITARY_PRODUCTION_CAPACITY","COUNTY_FACILITY_REFERENCE","COUNTY_STORAGE_CAPACITY","COUNTY_MARKET_AND_SERVICE_CAPACITY","COUNTY_TRANSPORT_CAPACITY","COUNTY_LOCAL_DEMAND","COUNTY_PRODUCT_PRODUCTION_BALANCE","COUNTY_PRODUCT_SURPLUS_DEFICIT","COUNTY_IMPORT_DEPENDENCY","COUNTY_EXPORT_CAPACITY","COUNTY_SUPPLY_RELATION_MASTER","COUNTY_PROCESSING_CHAIN_DEPENDENCY","REGIONAL_PRODUCTION_ZONE_MASTER","SCENARIO_PRODUCTION_STATE_MASTER","HISTORICAL_PRODUCTION_CHANGEPOINTS","COUNTY_RUNTIME_MAPPING_REFERENCE","EVIDENCE_AND_SOURCE_REGISTRY","COUNTY_UNKNOWNS_AND_RESEARCH_GAPS","NATIONAL_184_PRODUCTION_BALANCE"];
    const file = `${n}_${names[i]}.xlsx`;
    return [`workbook-${n}`, `${evidenceDir}/MASTER_WORKBOOKS/${file}`, file.replace(/\.xlsx$/i, ""), "ReferenceWorkbook"];
  }),
].map(([id, Path, Title, DocumentType]) => ({ DocumentId: `doc.han135260.county-economy.${id}.v1`, Path, Title, DocumentType, ...commonDoc }));

const decisions = [
  ["runtime-authority", "County aggregate is initialization/calibration/AI/statistics reference; runtime authority remains Cell, Resource, Facility, Worker, Recipe, Inventory and Transport."],
  ["no-mandatory-self-sufficiency", "Counties and cities are not required to be self-sufficient; deficits and dependencies remain explicit."],
  ["potential-not-production", "Resource potential, historical exploitation, processing capacity, actual output and exportable surplus are separate facts."],
  ["no-hidden-balance-multiplier", "National balance cannot be forced with an undisclosed global multiplier; only documented regional/county parameters may change."],
  ["modeled-location-disclosure", "Unresolved county locations use explicit analytical fallback points and cannot be presented as historical county-seat locations."],
  ["transport-loss", "Supply ships an origin quantity, delivers a smaller destination quantity and records the difference as transport loss."],
  ["unmet-demand", "Stocks never become negative; unsatisfied consumption or input is retained as unmet_demand, not magic supply."],
  ["scenario-inheritance", "184 is the highest-detail reference; 13 scenarios inherit snapshots and explicit ChangePoints and may diverge after runtime start."],
  ["open-content-ids", "Products, crops, facilities, resources and recipes use stable namespaced data IDs; missing mappings remain explicit."],
  ["license-gate", "External data is imported only when commercial use and redistribution are compatible; CHGIS V3 remains locator-only."],
].map(([suffix, Decision]) => ({ DecisionId: `decision.han135260.county-economy.${suffix}.v1`, Domain: "WorldSimulation", Title: suffix, Decision, Status: "ACCEPTED", EffectiveFrom: "2026-08-11", SourceDocument: `${evidenceDir}/COUNTY_PRODUCTION_ECONOMY_STANDARD.md`, AffectedDocuments: `${evidenceDir}/README.md`, AffectedSystems: "WorldInitialization|Production|Economy|Transport|AI|ContentData", ReasonSummary: "Preserve one traceable world and distinguish evidence from model completion.", OpenQuestions: "See OPEN_DECISION_REGISTRY", Notes: "Reference-layer decision; runtime implementation remains separate." }));

const open = [
  ["county-location-resolution", "Which compatible-license historical geography sources can resolve the remaining 1114 county-seat locations?", "Historical gazetteers, archaeology and compatible-license GIS crosswalk", "County-specific Cell materialization"],
  ["county-boundaries", "How should historical county polygons or uncertain influence areas be reconstructed without fabricating exact borders?", "Historical geography studies and explicit uncertainty geometry", "Area and Cell allocation"],
  ["historical-unit-calibration", "What low/recommended/high conversions should be adopted for Han measures by period, place and commodity?", "Primary texts, excavated measures and specialist metrology research", "Historical quantity imports"],
  ["historical-route-network", "Which modeled corridors can be upgraded to evidenced ancient road, river, canal and sea routes?", "Route texts, archaeology, pass/port evidence and hydrology", "Runtime logistics materialization"],
  ["county-industry-quantities", "Which county or commandery industries have evidence strong enough to replace the regional model?", "County archaeology, production sites, inscriptions and specialist studies", "County development packs"],
].map(([suffix, Question, NeededEvidence, Blocks]) => ({ OpenDecisionId: `open.han135260.county-economy.${suffix}.v1`, Domain: "HistoricalWorldReference", Question, Status: "OPEN", WhyOpen: "Current project evidence is insufficient for a historical claim.", NeededEvidence, OwnerRole: "HistoricalResearch|GIS|WorldDesign", Blocks, SourceDocument: `${evidenceDir}/README.md`, RecommendedNextReview: "Before affected county materialization", Notes: "Do not infer a historical fact from the V1 analytical fallback." }));

const research = [
  ["county-locations", "1114 county locations remain unresolved; V1 uses disclosed modeled analytical points.", "CRITICAL", "Compatible-license historical gazetteers, archaeological reports and coordinate crosswalks", "Exact historical coordinates"],
  ["county-boundaries-area", "All historical county polygons and most areas remain unknown.", "HIGH", "Historical geography reconstruction with uncertainty bands", "Exact borders from population-density proxies"],
  ["county-production-evidence", "1123 county profiles are primarily modeled and only 59 have reconstructed development-place support.", "HIGH", "County/commandery archaeology and industry studies", "County-specific mines, workshops or output quantities"],
  ["historical-routes", "4471 graph edges and 127 supply relations are analytical, not ancient-route claims.", "HIGH", "Road, river, canal, pass, port and political-control evidence", "Straight GeoJSON lines as actual roads"],
  ["unit-conversion", "Historical unit conversions are not yet accepted by commodity, period and place.", "MEDIUM", "Metrology research and excavated standards", "One timeless conversion constant"],
].map(([suffix, ResearchGap, Priority, RequiredSources, DoNotInfer]) => ({ GapId: `research.han135260.county-economy.${suffix}.v1`, Domain: "HistoricalWorldReference", ResearchGap, Priority, CurrentEvidence: `${evidenceDir}/COUNTY_PACKS/county_economy_master_v1.json`, RequiredSources, DoNotInfer, SuggestedResearchAction: "Create evidence-backed county or commandery ChangePoints and retain source IDs.", Blocks: "Historical claim upgrade", Status: "OPEN", Notes: "Does not block use as MODELED development reference." }));

const implementation = [
  ["content-mapping", "All reference product, crop and facility IDs must be registered as data definitions before runtime use.", "Reference master marks missing mappings explicitly.", "Reference IDs are not all formal runtime content.", "HIGH", "Content catalog implementation"],
  ["cell-resource-materialization", "Selected counties require Cell-level resource patches and land use.", "No national runtime resources were created.", "V1 county potential is not materialized on Cells.", "CRITICAL", "County readiness and Cell materialization task"],
  ["facility-worker-materialization", "Production must occur through facilities, assigned workers and recipes.", "No national facilities or people were created.", "County capacity is only a reference ceiling.", "CRITICAL", "Selected vertical-slice county materialization"],
  ["runtime-supply-network", "Trade requires real inventories, carriers, routes, ownership, orders, loss and settlement.", "127 reference relations and 4471 modeled graph edges exist.", "Reference relations are not runtime transactions.", "HIGH", "Luoyang supply network/hinterland materialization V2"],
  ["scenario-economy-bootstrap", "Scenario bootstrap must apply population, facilities, inventories and ChangePoints without overwriting later world facts.", "13 reference states exist, no runtime bootstrap added.", "Scenario production state is not yet consumed by runtime.", "HIGH", "Scenario economy bootstrap after selected-region materialization"],
].map(([suffix, CanonicalRequirement, CurrentImplementation, GapDescription, Severity, SuggestedFutureTask]) => ({ GapId: `gap.han135260.county-economy.${suffix}.v1`, Domain: "WorldSimulation", CanonicalRequirement, CurrentImplementation, GapDescription, Severity, BlocksNextDevelopment: suffix === "cell-resource-materialization" || suffix === "facility-worker-materialization" ? "YES" : "NO", SuggestedFutureTask, Evidence: `${evidenceDir}/README.md`, RequiredContract: "Stable IDs|determinism|conservation|evidence grading|save compatibility", Blocks: "Runtime materialization", RecommendedTask: SuggestedFutureTask, Status: "OPEN", Notes: "Reference package complete; implementation deliberately separate." }));

const conflict = [{ ConflictId: "conflict.han135260.county-economy.gis-completeness-v1", Domain: "HistoricalWorldReference", DocumentA: taskPath, DocumentB: "Assets/StreamingAssets/WorldMap/HanWorldV1/locations/counties.json", ConflictDescription: "The task requires a complete nationwide analytical map, while HanWorldV1 resolves only 68 county points and has no county polygons.", CurrentPreferredRule: "Use stable IDs and explicit modeled analytical fallback points; historical location and boundaries remain UNKNOWN until compatible evidence is acquired.", AuthorityReason: "AGENTS evidence and license rules prohibit fabricating or importing incompatible historical geography.", ResolutionStatus: "RESOLVED_WITH_DISCLOSED_MODEL", RequiredAction: "Replace individual fallback points only through source-backed county ChangePoints.", RiskIfIgnored: "Modeled points could be mistaken for historical geography and drive false routes or Cell placement.", Status: "MONITORED", Notes: "1114 fallback points remain a research gap, not a blocked reference deliverable." }];

const updates = {
  "PROJECT_DOCUMENT_REGISTRY.xlsx": { key: "DocumentId", rows: docs },
  "PROJECT_CANONICAL_DOMAIN_MAP.xlsx": { key: "Domain", rows: [{ Domain: "Han135260CountyProductionEconomyReference", L0ProjectConstitution: "AGENTS.md", L1CanonicalSpec: "Docs/WORLD_SIMULATION_FOUNDATION.md|Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md|Docs/DATA_AND_CONTENT_FOUNDATION.md", L2CurrentStatus: "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", L3PrimaryReference: `${evidenceDir}/README.md`, CanonicalGap: "Runtime Cell/resource/facility/worker/inventory/logistics materialization remains open.", MultipleL1Conflict: "NO", ReadingEntry: taskPath, ConflictPolicy: "L1 world-fact, population, persistence, data-ID and license rules win; reference aggregates never replace runtime facts.", DomainId: "domain.han135260.county-production-economy-reference.v1", DomainName: "Han 135-260 County Production Economy Reference", CurrentStatus: "REFERENCE_COMPLETE_IMPLEMENTATION_OPEN", Status: "CURRENT" }] },
  "DESIGN_DECISION_REGISTRY.xlsx": { key: "DecisionId", rows: decisions },
  "OPEN_DECISION_REGISTRY.xlsx": { key: "OpenDecisionId", rows: open },
  "RESEARCH_GAP_REGISTER.xlsx": { key: "GapId", rows: research },
  "IMPLEMENTATION_GAP_REGISTER.xlsx": { key: "GapId", rows: implementation },
  "DOCUMENT_CONFLICT_REGISTER.xlsx": { key: "ConflictId", rows: conflict },
};

const col = index => { let n = index + 1, out = ""; while (n > 0) { const r = (n - 1) % 26; out = String.fromCharCode(65 + r) + out; n = Math.floor((n - 1) / 26); } return out; };
function locate(workbook, key) {
  for (const sheet of workbook.worksheets.items) {
    const values = sheet.getUsedRange(true)?.values ?? [];
    for (let r = 0; r < Math.min(values.length, 20); r++) {
      const headers = values[r].map(value => String(value ?? ""));
      if (headers.includes(key)) return { sheet, values, headerRow: r, headers };
    }
  }
  throw new Error(`Header ${key} not found`);
}

const results = [];
for (const [file, spec] of Object.entries(updates)) {
  const filePath = path.join(registryDir, file);
  const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(filePath));
  const found = locate(workbook, spec.key);
  const keyIndex = found.headers.indexOf(spec.key);
  const existing = new Map();
  for (let r = found.headerRow + 1; r < found.values.length; r++) {
    const key = String(found.values[r][keyIndex] ?? "").trim();
    if (key) existing.set(key, r);
  }
  const beforeCount = existing.size;
  let added = 0, updated = 0;
  for (const patch of spec.rows) {
    const key = String(patch[spec.key]);
    if (existing.has(key)) {
      const r = existing.get(key), current = found.values[r] ?? [];
      found.sheet.getRangeByIndexes(r, 0, 1, found.headers.length).values = [found.headers.map((header, c) => Object.hasOwn(patch, header) ? patch[header] : (current[c] ?? ""))];
      updated++;
    } else {
      const values = found.headers.map(header => patch[header] ?? "");
      if (found.sheet.tables.items.length) found.sheet.tables.items[0].rows.add(null, [values]);
      else found.sheet.getRangeByIndexes(found.values.length + added, 0, 1, found.headers.length).values = [values];
      added++;
    }
  }
  await (await SpreadsheetFile.exportXlsx(workbook)).save(filePath);
  const errors = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 100 }, summary: "formula error scan" });
  const after = locate(workbook, spec.key);
  const afterCount = (after.sheet.getUsedRange(true)?.values ?? []).slice(after.headerRow + 1).filter(row => String(row[keyIndex] ?? "").trim()).length;
  const preview = await workbook.render({ sheetName: after.sheet.name, range: `A1:${col(Math.min(after.headers.length, 14) - 1)}${Math.min(after.headerRow + 1 + afterCount, 60)}`, autoCrop: "all", scale: .65, format: "png" });
  await fs.writeFile(path.join(validationDir, `${file.replace(/\.xlsx$/i, "")}.png`), new Uint8Array(await preview.arrayBuffer()));
  await fs.writeFile(path.join(validationDir, `${file}.inspect.ndjson`), `${errors.ndjson}\n`, "utf8");
  results.push({ file, key: spec.key, before_count: beforeCount, requested_rows: spec.rows.length, added, updated, after_count: afterCount });
  console.log(`UPDATED ${file} before=${beforeCount} added=${added} updated=${updated} after=${afterCount}`);
}
await fs.writeFile(path.join(validationDir, "registry_update_summary.json"), `${JSON.stringify({ status: "PASS", task_id: taskId, results }, null, 2)}\n`, "utf8");
console.log(`RESULT registry-update status=passed registries=${results.length}`);
