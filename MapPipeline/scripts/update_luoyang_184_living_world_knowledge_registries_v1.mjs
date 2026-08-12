import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const artifactEntry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY || "@oai/artifact-tool";
const { FileBlob, SpreadsheetFile } = await import(pathToFileURL(artifactEntry).href);
const repo = "E:/project/gamedevelop/MandateOfHeroes";
const registryDir = path.join(repo, "Docs/KNOWLEDGE_BASE/REGISTRY");
const previewDir = path.join(repo, "outputs/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1/previews/registries");
const evidenceDir = "Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1";
const workbookDir = "outputs/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1";
await fs.mkdir(previewDir, { recursive: true });

const common = {
  Domain: "WorldSimulation", SubDomain: "Luoyang184LivingWorldClosure", AuthorityLevel: "L4",
  Status: "CURRENT", CreatedOrKnownDate: "2026-08-11", LastKnownRevision: "2026-08-11",
  CanonicalFor: "ImplementationEvidenceOnly", RelatedTasks: "LUOYANG-184-PERSON-WORK-PRODUCTION-CONSUMPTION-CLOSURE-V1",
  RelatedRuntimeSystems: "Mandate.Domain|Mandate.Simulation|Mandate.Persistence|Mandate.Presentation",
  HistoricalValue: "HIGH", RecommendedReader: "Developer|DataEngineer|GameplayDesigner", ReadPriority: "P0",
  ConflictNotes: "Evidence does not override L1 rules or protected initialization facts.",
  ActionRequired: "Preserve 400K/80899/2084 counts and replace transitional supply only with real supply-region facts.",
  CanonicalScope: "Luoyang184LivingWorldClosureV1",
};
const docs = [
  ["task", "Docs/TASK_LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1.md", "洛阳184人物工作生产消费闭环任务书", "TaskRecord"],
  ["report", `${evidenceDir}/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1_REPORT.md`, "洛阳184生活经济闭环正式报告", "AcceptanceReport"],
  ["simulation", `${evidenceDir}/09_LUOYANG_365_DAY_LIVING_WORLD_SIMULATION_REPORT.md`, "洛阳184生活世界365日模拟报告", "SimulationReport"],
  ["performance", `${evidenceDir}/11_LUOYANG_SIMULATION_PERFORMANCE_REPORT.md`, "洛阳184生活世界性能报告", "PerformanceReport"],
  ["next", `${evidenceDir}/12_NEXT_LUOYANG_DEVELOPMENT_STAGE.md`, "下一洛阳开发阶段建议", "ImplementationScope"],
  ["validation", `${evidenceDir}/validation_summary.json`, "洛阳184生活经济闭环验证汇总", "ValidationEvidence"],
  ...[
    "01_LUOYANG_WORKFORCE_RUNTIME_AUDIT.xlsx",
    "02_LUOYANG_FACILITY_PRODUCTION_RUNTIME_STATE.xlsx",
    "03_LUOYANG_AGRICULTURE_CROP_CYCLE_AUDIT.xlsx",
    "04_LUOYANG_INVENTORY_FLOW_AUDIT.xlsx",
    "05_LUOYANG_HOUSEHOLD_CONSUMPTION_AUDIT.xlsx",
    "06_LUOYANG_MARKET_SUPPLY_DEMAND_AUDIT.xlsx",
    "07_LUOYANG_FOOD_AND_BASIC_GOODS_BALANCE.xlsx",
    "08_LUOYANG_SHORTAGE_AND_RESPONSE_AUDIT.xlsx",
    "10_LUOYANG_PRODUCTION_CONSUMPTION_CONSERVATION_AUDIT.xlsx",
  ].map((file, index) => [`workbook-${index + 1}`, `${workbookDir}/${file}`, file.replace(/\.xlsx$/i, ""), "AuditWorkbook"]),
].map(([id, Path, Title, DocumentType]) => ({ DocumentId: `doc.luoyang184.living-world.${id}.v1`, Path, Title, DocumentType, ...common }));

const closedGap = (GapId, Domain, CurrentImplementation) => ({
  GapId, Domain, CanonicalRequirement: "Same protected Persons and Facilities must drive traceable living-world facts.",
  CurrentImplementation, GapDescription: "Closed by bounded V70 Luoyang living-world closure prototype.", Severity: "INFO",
  BlocksNextDevelopment: "NO", SuggestedFutureTask: "NONE", Evidence: `${evidenceDir}/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1_REPORT.md`,
  RequiredContract: "Stable Person IDs|real inventory|derived checkpoint|determinism|conservation", Blocks: "NONE",
  RecommendedTask: "COMPLETED", Status: "CLOSED", Notes: "Closed 2026-08-11; mature commerce and supply region remain separate.",
});
const updates = {
  "PROJECT_DOCUMENT_REGISTRY.xlsx": { key: "DocumentId", rows: docs },
  "PROJECT_CANONICAL_DOMAIN_MAP.xlsx": { key: "Domain", rows: [{
    Domain: "Luoyang184LivingWorldClosure", L0ProjectConstitution: "AGENTS.md",
    L1CanonicalSpec: "Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md|Docs/PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md|Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md",
    L2CurrentStatus: "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", L3PrimaryReference: `${evidenceDir}/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1_REPORT.md`,
    CanonicalGap: "Real Luoyang supply-region materialization and mature market/logistics contracts remain open.", MultipleL1Conflict: "NO",
    ReadingEntry: "Docs/TASK_LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1.md",
    ConflictPolicy: "L1 rules and protected package facts win; shortages remain facts rather than receiving magic imports.",
  }] },
  "IMPLEMENTATION_GAP_REGISTER.xlsx": { key: "GapId", rows: [
    closedGap("gap.luoyang184.derived-population-checkpoint", "PopulationPersistence", "V70 writes a gzip derived checkpoint and keeps the protected initialization package read-only."),
    closedGap("gap.luoyang184.living-world-economic-loop", "WorldSimulation", "400K Persons, 80,899 Households and 2,084 Facilities now drive work, production, inventory, crops, consumption and shortage facts."),
    {
      GapId: "gap.luoyang184.supply-region-materialization", Domain: "WorldSimulation",
      CanonicalRequirement: "Luoyang external food and basic goods must originate in real same-world hinterland production and logistics.",
      CurrentImplementation: "Five bounded transitional reference supplies exist; 365-day run ends with all 80,899 households in shortage.",
      GapDescription: "The planned supply region and agricultural hinterland are not materialized.", Severity: "CRITICAL", BlocksNextDevelopment: "YES",
      SuggestedFutureTask: "LUOYANG-184-SUPPLY-REGION-AND-AGRICULTURAL-HINTERLAND-MATERIALIZATION-V1",
      Evidence: `${evidenceDir}/12_NEXT_LUOYANG_DEVELOPMENT_STAGE.md`, RequiredContract: "Same Cell world|real Persons|land|seed|harvest|warehouse|carrier|loss|delivery",
      Blocks: "FoodSecurity|MatureCommerce", RecommendedTask: "LUOYANG-184-SUPPLY-REGION-AND-AGRICULTURAL-HINTERLAND-MATERIALIZATION-V1",
      Status: "OPEN", Notes: "Selected by measured SUPPLY_REGION_DEPENDENCY; do not add magic imports or stack plan population onto the 400K city.",
    },
    {
      GapId: "gap.luoyang184.market-commerce-logistics-depth", Domain: "Economy",
      CanonicalRequirement: "Trade must include ownership, purchase power, orders, prices, carriers, contracts and settlement.",
      CurrentImplementation: "V70 has physical inventory, demand, failed demand, price pressure and acquisition-source records.",
      GapDescription: "Mature merchant competition and contractual market/logistics settlement remain unimplemented.", Severity: "HIGH", BlocksNextDevelopment: "NO",
      SuggestedFutureTask: "LUOYANG-184-MARKET-COMMERCE-LOGISTICS-ECONOMY-V1", Evidence: `${evidenceDir}/12_NEXT_LUOYANG_DEVELOPMENT_STAGE.md`,
      RequiredContract: "Real stock|ownership|money|orders|transport|loss|determinism", Blocks: "MatureMerchantGameplay",
      RecommendedTask: "After supply-region materialization", Status: "OPEN", Notes: "Do not use price depth to hide a physical supply deficit.",
    },
  ] },
  "OPEN_DECISION_REGISTRY.xlsx": { key: "OpenDecisionId", rows: [{
    OpenDecisionId: "open.luoyang184.supply-region-materialization-scope", Domain: "WorldSimulation",
    Question: "Which same-world counties, villages, fields, waterways, roads and warehouse nodes form the first formal Luoyang supply region?",
    Status: "OPEN", WhyOpen: "V70 proves physical supply dependency but does not authorize a geographic or population boundary.",
    NeededEvidence: "HanWorld cells|county/place references|agricultural potential|transport routes|population scaling|ownership",
    OwnerRole: "HistoricalResearch|WorldDesign|SimulationEngineering", Blocks: "Supply-region implementation scope",
    SourceDocument: `${evidenceDir}/12_NEXT_LUOYANG_DEVELOPMENT_STAGE.md`, RecommendedNextReview: "At the start of the supply-region task",
    Notes: "Do not assume the old inclusive 700K plan is a ready materialization target.",
  }] },
};

function col(index) { let n = index + 1, out = ""; while (n > 0) { const r = (n - 1) % 26; out = String.fromCharCode(65 + r) + out; n = Math.floor((n - 1) / 26); } return out; }
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
async function render(workbook, file, prefix) {
  for (const sheet of workbook.worksheets.items) {
    const values = sheet.getUsedRange(true)?.values ?? [[]];
    const columns = Math.max(1, ...values.slice(0, 60).map(row => row.length));
    const rows = Math.max(1, Math.min(values.length, 60));
    const preview = await workbook.render({ sheetName: sheet.name, range: `A1:${col(columns - 1)}${rows}`, autoCrop: "all", scale: 0.65, format: "png" });
    await fs.writeFile(path.join(previewDir, `${prefix}__${file.replace(/\.xlsx$/i, "")}__${sheet.name}.png`), new Uint8Array(await preview.arrayBuffer()));
  }
}

const results = [];
for (const [file, spec] of Object.entries(updates)) {
  const filePath = path.join(registryDir, file);
  const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(filePath));
  const before = await workbook.inspect({ kind: "sheet", include: "id,name", maxChars: 10000 });
  await fs.writeFile(path.join(previewDir, `before__${file}.inspect.txt`), before.ndjson, "utf8");
  await render(workbook, file, "before");
  const found = locate(workbook, spec.key);
  const keyIndex = found.headers.indexOf(spec.key);
  const existing = new Map();
  for (let r = found.headerRow + 1; r < found.values.length; r++) {
    const key = String(found.values[r][keyIndex] ?? "").trim();
    if (key) existing.set(key, r);
  }
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
  const errors = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 200 }, summary: "formula errors" });
  await fs.writeFile(path.join(previewDir, `after__${file}.inspect.txt`), errors.ndjson, "utf8");
  await render(workbook, file, "after");
  results.push({ file, added, updated });
}
await fs.writeFile(path.join(previewDir, "registry_update_summary.json"), JSON.stringify(results, null, 2), "utf8");
console.log(JSON.stringify({ status: "PASS", results }, null, 2));
