import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const artifactEntry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY || "@oai/artifact-tool";
const { FileBlob, SpreadsheetFile } = await import(pathToFileURL(artifactEntry).href);

const repo = "E:/project/gamedevelop/MandateOfHeroes";
const registryDir = path.join(repo, "Docs/KNOWLEDGE_BASE/REGISTRY");
const previewRoot = path.join(repo, "outputs/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1/previews/registries");
await fs.mkdir(previewRoot, { recursive: true });

const reviewBase = "Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1";
const commonDoc = {
  Domain: "HistoricalWorldGeography",
  SubDomain: "Luoyang184DevelopmentReadiness",
  AuthorityLevel: "L4",
  Status: "CURRENT",
  CreatedOrKnownDate: "2026-08-11",
  LastKnownRevision: "2026-08-11",
  CanonicalFor: "ReviewEvidenceOnly",
  RelatedTasks: "LUOYANG-184-DEVELOPMENT-READINESS-REVIEW-V1|LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1",
  RelatedRuntimeSystems: "Mandate.Domain|Mandate.Simulation|Mandate.Persistence",
  HistoricalValue: "HIGH",
  RecommendedReader: "Developer|HistoricalResearcher|DataEngineer",
  ReadPriority: "P0",
  ConflictNotes: "Review outputs do not override L1 specifications or materialize runtime facts.",
  ActionRequired: "Follow Gate A blockers and deferred-place boundary.",
  CanonicalScope: "Luoyang184ReadinessReview",
};

const documentRows = [
  { DocumentId: "doc.task.luoyang184.development-readiness-review.v1", Path: "Docs/TASK_LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1.md", Title: "LUOYANG-184开发准备度审查执行记录", DocumentType: "TaskRecord", ...commonDoc },
  { DocumentId: "doc.report.luoyang184.development-readiness-review.v1", Path: `${reviewBase}/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1_REPORT.md`, Title: "洛阳184正式开发准备度审查V1", DocumentType: "AcceptanceReport", ...commonDoc },
  { DocumentId: "doc.reference.luoyang184.initialization.v1", Path: `${reviewBase}/08_LUOYANG_184_INITIALIZATION_REFERENCE.md`, Title: "洛阳184初始化权威入口V1", DocumentType: "InitializationReference", ...commonDoc },
  { DocumentId: "doc.scope.luoyang184.historical-person-family-integration.v1", Path: `${reviewBase}/11_NEXT_IMPLEMENTATION_TASK_SCOPE.md`, Title: "洛阳184历史人物—家族集成下一任务冻结范围", DocumentType: "ImplementationScope", ...commonDoc },
  ...[
    ["readiness-matrix", "01_LUOYANG_184_DEVELOPMENT_READINESS_MATRIX.xlsx", "洛阳184开发准备度矩阵"],
    ["runtime-mapping", "02_LUOYANG_RUNTIME_ENTITY_MAPPING_AUDIT.xlsx", "洛阳运行时实体映射审计"],
    ["historical-person-mapping", "03_LUOYANG_HISTORICAL_PERSON_RUNTIME_MAPPING.xlsx", "洛阳历史人物运行时映射"],
    ["family-org-migration", "04_LUOYANG_CLAN_FAMILYORGANIZATION_MIGRATION_PLAN.xlsx", "洛阳Clan与FamilyOrganization迁移计划"],
    ["family-center-readiness", "05_LUOYANG_FAMILYCENTER_IMPLEMENTATION_READINESS.xlsx", "洛阳FamilyCenter实现准备度"],
    ["facility-crosswalk", "06_LUOYANG_FACILITY_HISTORICAL_REFERENCE_RUNTIME_CROSSWALK.xlsx", "洛阳Facility历史参考运行时Crosswalk"],
    ["population-audit", "07_LUOYANG_POPULATION_HOUSEHOLD_RESIDENCE_AUDIT.xlsx", "洛阳人口家户住宅审计"],
    ["future-190", "09_LUOYANG_190_FUTURE_COMPATIBILITY_AUDIT.xlsx", "洛阳190未来兼容审计"],
    ["wave0-dependency", "10_LUOYANG_HULAO_WAVE0_DEPENDENCY_REVIEW.xlsx", "洛阳虎牢函谷Wave0依赖审查"],
  ].map(([id, file, title]) => ({ DocumentId: `doc.workbook.luoyang184.${id}.v1`, Path: `${reviewBase}/${file}`, Title: title, DocumentType: "AuditWorkbook", ...commonDoc })),
];

const updates = {
  "PROJECT_DOCUMENT_REGISTRY.xlsx": {
    key: "DocumentId",
    rows: documentRows,
  },
  "PROJECT_CANONICAL_DOMAIN_MAP.xlsx": {
    key: "Domain",
    rows: [{
      Domain: "Luoyang184DevelopmentReadiness",
      L0ProjectConstitution: "AGENTS.md",
      L1CanonicalSpec: "Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md|Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md",
      L2CurrentStatus: "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md",
      L3PrimaryReference: `${reviewBase}/08_LUOYANG_184_INITIALIZATION_REFERENCE.md`,
      CanonicalGap: "Implementation task must close five bounded High blockers; review is not runtime authority.",
      MultipleL1Conflict: "NO",
      ReadingEntry: `${reviewBase}/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1_REPORT.md`,
      ConflictPolicy: "L1 wins; review freezes evidence, gate and implementation scope only.",
    }],
  },
  "IMPLEMENTATION_GAP_REGISTER.xlsx": {
    key: "GapId",
    rows: [
      ["gap.luoyang184.main-world-projection", "PopulationPersistence", "Project formal 400K composite into one NewGame/WorldState/save population source.", "HIGH"],
      ["gap.luoyang184.historical-person-binding", "HistoricalPersons", "Idempotently bind 25 P-IDs to existing persons and reject duplicate materialization.", "HIGH"],
      ["gap.luoyang184.family-organization-migration", "FamilyOrganization", "Migrate seven urban organizations with stable IDs; repair f088/f036 without deleting people.", "HIGH"],
      ["gap.luoyang184.family-center-runtime", "FamilyCenter", "Persist the five-prerequisite FamilyCenter designation/capability contract.", "HIGH"],
      ["gap.luoyang184.facility-assignment-authority", "FacilityPopulation", "Make binary Facility indexes authoritative and migrate/de-authorize stale inline person lists.", "HIGH"],
      ["gap.luoyang184.government-office-projection", "Governance", "Project government/office references into general runtime state.", "MEDIUM"],
      ["gap.luoyang184.historical-change-runtime", "HistoricalState", "Implement same-ID 184-to-190 HistoricalChange execution in a later task.", "MEDIUM"],
    ].map(([GapId, Domain, GapDescription, Severity]) => ({
      GapId, Domain,
      CanonicalRequirement: GapDescription,
      CurrentImplementation: "Reference/source package exists; main runtime integration is absent or partial.",
      GapDescription, Severity,
      BlocksNextDevelopment: Severity === "HIGH" ? "YES" : "NO",
      SuggestedFutureTask: Severity === "HIGH" ? "LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1" : "POST_LUOYANG_CORE_SPECIALIST_TASK",
      Evidence: `${reviewBase}/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1_REPORT.md`,
      RequiredContract: "Stable IDs; deterministic idempotence; no person deletion/merge/rerandomization; sequential save migration when persisted.",
      Blocks: Severity === "HIGH" ? "GateACompletion" : "LaterSpecialistSystem",
      RecommendedTask: Severity === "HIGH" ? "LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1" : "DEFERRED",
      Status: "OPEN",
      Notes: "Registered by LUOYANG-184-DEVELOPMENT-READINESS-REVIEW-V1.",
    })),
  },
  "RESEARCH_GAP_REGISTER.xlsx": {
    key: "GapId",
    rows: [
      {
        GapId: "gap.luoyang184.hulao-cell-facility-scope", Domain: "HistoricalWorldGeography",
        ResearchGap: "虎牢最终CanonicalPlace/Cell范围与分期Facility范围未关闭。", Priority: "HIGH",
        CurrentEvidence: "T3/FDRP reference exists; DPB-017 remains open.", RequiredSources: "Historical geography|terrain|route|period facility evidence",
        DoNotInfer: "Do not infer a permanent settlement or 184 complex from battle fame.", SuggestedResearchAction: "Close as an independent Place task after Luoyang Core.",
        Question: "What exact Cells and period-specific Facilities belong to Hulao?", EvidenceNeeded: "Auditable Cell extent and phased Facility inventory.",
        Blocks: "WAVE_0B_HULAO", Status: "OPEN", Notes: "Does not block Luoyang Core Gate A.",
      },
      {
        GapId: "gap.luoyang184.hangu-cell-facility-population-scope", Domain: "HistoricalWorldGeography",
        ResearchGap: "函谷最终Cell范围、184 Facility组成和即时人口/军力范围未关闭。", Priority: "HIGH",
        CurrentEvidence: "T2/FDRP reference exists; runtime not started.", RequiredSources: "Historical geography|pass facilities|population|military evidence",
        DoNotInfer: "Do not copy later-dynasty pass composition into 184.", SuggestedResearchAction: "Create an independent Hangu readiness task after Luoyang Core.",
        Question: "What exact 184 scope can be initialized without anachronism?", EvidenceNeeded: "Cell extent, phased Facility inventory and bounded population/force scope.",
        Blocks: "WAVE_0B_HANGU", Status: "OPEN", Notes: "Does not block Luoyang Core Gate A.",
      },
    ],
  },
  "OPEN_DECISION_REGISTRY.xlsx": {
    key: "OpenDecisionId",
    rows: [{
      OpenDecisionId: "open.luoyang184.first-family-center-designation",
      Domain: "FamilyCenter",
      Question: "Which Luoyang FamilyOrganization, if any, first satisfies all five FamilyCenter prerequisites after migration?",
      Status: "DEFERRED",
      WhyOpen: "Current candidates are reference proposals; none has a complete persisted runtime contract.",
      NeededEvidence: "Real Facility|FamilyManagement capability|legal control|manager Person|Primary/Local designation",
      OwnerRole: "GameplayDesign|HistoricalResearch|DomainEngineering",
      Blocks: "FamilyCenter designation only; does not block person/family integration.",
      SourceDocument: `${reviewBase}/05_LUOYANG_FAMILYCENTER_IMPLEMENTATION_READINESS.xlsx`,
      RecommendedNextReview: "After LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1 migration dry run",
      Notes: "Default remains NONE; do not infer center from residence or estate reference.",
    }],
  },
  "DOCUMENT_CONFLICT_REGISTER.xlsx": {
    key: "ConflictId",
    rows: [
      {
        ConflictId: "conflict.luoyang184.population-scope-130169-vs-400k", Domain: "HistoricalPopulation",
        DocumentA: "HistoricalPopulation/Han135260V1/year_184.json", DocumentB: "Luoyang184MetropolitanInitializationV1/manifest.json",
        ConflictDescription: "130,169 county model reference may be misread as additive to the 400K formal metropolitan population.",
        CurrentPreferredRule: "400K is the unique materialized opening baseline; 130,169 is a national model reference and 700K is an inclusive unmaterialized envelope.",
        AuthorityReason: "Explicit scope hierarchy and package audit.", ResolutionStatus: "RESOLVED_BY_SCOPE_CONTRACT",
        RequiredAction: "Implement one population source and validate non-addition.", RiskIfIgnored: "Duplicate permanent people and broken national conservation.",
      },
      {
        ConflictId: "conflict.luoyang184.facility-inline-person-lists-vs-binary-index", Domain: "FacilityPopulation",
        DocumentA: "Luoyang184UrbanInitializationV1/facilities.json", DocumentB: "Luoyang184UrbanInitializationV1/persons.bin",
        ConflictDescription: "1,116 Facility list fields retain old generated person IDs while formal persons use binary Facility indexes.",
        CurrentPreferredRule: "Binary person residence/work Facility indexes are the opening assignment authority; inline lists must be migrated or made explicitly non-authoritative.",
        AuthorityReason: "Full 400K binary reference/capacity audit passes; old inline IDs do not exist in formal person set.", ResolutionStatus: "RESOLVED_RULE_PENDING_IMPLEMENTATION",
        RequiredAction: "Close in LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1.", RiskIfIgnored: "Ghost residents/workers or overwritten formal assignments.",
      },
    ],
  },
};

function excelColumn(index) {
  let n = index + 1;
  let out = "";
  while (n > 0) {
    const r = (n - 1) % 26;
    out = String.fromCharCode(65 + r) + out;
    n = Math.floor((n - 1) / 26);
  }
  return out;
}

function findHeader(workbook, key) {
  for (const sheet of workbook.worksheets.items) {
    const used = sheet.getUsedRange(true);
    if (!used) continue;
    const values = used.values ?? [];
    for (let rowIndex = 0; rowIndex < Math.min(values.length, 20); rowIndex++) {
      const row = values[rowIndex].map((value) => String(value ?? ""));
      const keyIndex = row.indexOf(key);
      if (keyIndex >= 0) return { sheet, values, headerRow: rowIndex, headers: row };
    }
  }
  throw new Error(`Header ${key} not found`);
}

async function renderWorkbook(workbook, label, phase) {
  const dir = path.join(previewRoot, phase);
  await fs.mkdir(dir, { recursive: true });
  for (const sheet of workbook.worksheets.items) {
    const used = sheet.getUsedRange(true);
    const values = used?.values ?? [[]];
    const cols = Math.max(1, ...values.slice(0, 60).map((row) => row.length));
    const rows = Math.max(1, Math.min(values.length, 60));
    const range = `A1:${excelColumn(cols - 1)}${rows}`;
    const preview = await workbook.render({ sheetName: sheet.name, range, autoCrop: "all", scale: 0.75, format: "png" });
    const safeSheet = sheet.name.replace(/[^a-zA-Z0-9\u4e00-\u9fff_-]+/g, "_");
    await fs.writeFile(path.join(dir, `${label.replace(/\.xlsx$/i, "")}__${safeSheet}.png`), new Uint8Array(await preview.arrayBuffer()));
  }
}

const results = [];
for (const [file, spec] of Object.entries(updates)) {
  const filePath = path.join(registryDir, file);
  const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(filePath));
  await renderWorkbook(workbook, file, "before");
  const located = findHeader(workbook, spec.key);
  const keyColumn = located.headers.indexOf(spec.key);
  const existing = new Map();
  for (let rowIndex = located.headerRow + 1; rowIndex < located.values.length; rowIndex++) {
    const key = String(located.values[rowIndex][keyColumn] ?? "").trim();
    if (key) existing.set(key, rowIndex);
  }
  let added = 0;
  let updated = 0;
  for (const patch of spec.rows) {
    const key = String(patch[spec.key]);
    const values = located.headers.map((header) => patch[header] ?? "");
    if (existing.has(key)) {
      const rowIndex = existing.get(key);
      const current = located.values[rowIndex] ?? [];
      const merged = located.headers.map((header, col) => Object.hasOwn(patch, header) ? patch[header] : (current[col] ?? ""));
      located.sheet.getRangeByIndexes(rowIndex, 0, 1, located.headers.length).values = [merged];
      updated++;
    } else {
      const tables = located.sheet.tables.items;
      if (tables.length) {
        tables[0].rows.add(null, [values]);
      } else {
        const rowIndex = located.values.length;
        located.sheet.getRangeByIndexes(rowIndex, 0, 1, located.headers.length).values = [values];
      }
      existing.set(key, located.values.length + added);
      added++;
    }
  }
  const output = await SpreadsheetFile.exportXlsx(workbook);
  await output.save(filePath);
  const formulaInspection = await workbook.inspect({ kind: "formula", maxChars: 5000, options: { maxResults: 100 } });
  await fs.writeFile(path.join(previewRoot, `${file}.formula-inspect.ndjson`), formulaInspection.ndjson, "utf8");
  await renderWorkbook(workbook, file, "after");
  results.push({ file, added, updated });
}

await fs.writeFile(path.join(previewRoot, "registry_update_summary.json"), JSON.stringify(results, null, 2), "utf8");
console.log(JSON.stringify({ status: "PASS", registries: results }, null, 2));
