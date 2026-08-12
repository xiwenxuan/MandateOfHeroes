import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const artifactEntry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY || "@oai/artifact-tool";
const { FileBlob, SpreadsheetFile } = await import(pathToFileURL(artifactEntry).href);
const repo = "E:/project/gamedevelop/MandateOfHeroes";
const dir = path.join(repo, "Docs/KNOWLEDGE_BASE/REGISTRY");
const previewDir = path.join(repo, "outputs/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1/previews/registries");
const evidence = "Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1";
await fs.mkdir(previewDir, { recursive: true });

const common = {
  Domain: "HistoricalPersons",
  SubDomain: "Luoyang184HistoricalPersonFamilyRuntime",
  AuthorityLevel: "L4",
  Status: "CURRENT",
  CreatedOrKnownDate: "2026-08-11",
  LastKnownRevision: "2026-08-11",
  CanonicalFor: "ImplementationEvidenceOnly",
  RelatedTasks: "LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1",
  RelatedRuntimeSystems: "Mandate.Domain|Mandate.Persistence|Mandate.Presentation",
  HistoricalValue: "HIGH",
  RecommendedReader: "Developer|DataEngineer|HistoricalResearcher",
  ReadPriority: "P0",
  ConflictNotes: "Evidence does not override L1 rules; unresolved Facility claims remain unresolved.",
  ActionRequired: "Use V69 stable IDs and protected population-package contract.",
  CanonicalScope: "Luoyang184HistoricalPersonFamilyRuntimeV1",
};
const docs = [
  ["task", "Docs/TASK_LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1.md", "洛阳184历史人物—家族接入任务书", "TaskRecord"],
  ["report", `${evidence}/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1_REPORT.md`, "洛阳184历史人物—家族正式接入报告", "AcceptanceReport"],
  ["save", `${evidence}/10_LUOYANG_SAVE_COMPATIBILITY_AND_MIGRATION_REPORT.md`, "洛阳184 V69存档兼容与迁移报告", "MigrationReport"],
  ["next", `${evidence}/11_NEXT_LUOYANG_DEVELOPMENT_STAGE.md`, "下一洛阳开发阶段建议", "ImplementationScope"],
  ["validation", `${evidence}/validation_summary.json`, "洛阳184历史人物—家族接入验证汇总", "ValidationEvidence"],
  ...[
    ["person", "01_LUOYANG_HISTORICAL_PERSON_RUNTIME_INTEGRATION.xlsx", "历史人物运行时接入"],
    ["lineage", "02_LUOYANG_CLAN_BRANCH_RUNTIME_MAPPING.xlsx", "Clan与Branch运行时映射"],
    ["organization", "03_LUOYANG_FAMILYORGANIZATION_RUNTIME_MIGRATION.xlsx", "FamilyOrganization迁移"],
    ["center", "04_LUOYANG_FAMILYCENTER_RUNTIME_STATE.xlsx", "FamilyCenter运行时状态"],
    ["household", "05_LUOYANG_HISTORICAL_PERSON_HOUSEHOLD_RESIDENCE_MAPPING.xlsx", "历史人物家户住宅映射"],
    ["asset", "06_LUOYANG_PERSON_FAMILY_ASSET_OWNERSHIP_AUDIT.xlsx", "个人与家族资产权属审计"],
    ["office", "07_LUOYANG_HISTORICAL_OFFICE_WORK_ACTIVITY_MAPPING.xlsx", "历史官职工作活动映射"],
    ["migration", "08_LUOYANG_RUNTIME_MIGRATION_LOG.xlsx", "运行时迁移日志"],
    ["conservation", "09_LUOYANG_POST_INTEGRATION_CONSERVATION_AUDIT.xlsx", "接入后守恒审计"],
  ].map(([id, file, title]) => [id, `${evidence}/${file}`, title, "AuditWorkbook"]),
].map(([id, Path, Title, DocumentType]) => ({ DocumentId: `doc.luoyang184.family-integration.${id}.v1`, Path, Title, DocumentType, ...common }));

const resolvedGap = (GapId, Domain, CurrentImplementation) => ({
  GapId, Domain, CurrentImplementation, GapDescription: "Closed by bounded V69 Luoyang integration.", Severity: "INFO",
  BlocksNextDevelopment: "NO", SuggestedFutureTask: "NONE", Evidence: `${evidence}/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1_REPORT.md`,
  RequiredContract: "Stable IDs; no Person/Facility creation; deterministic idempotence; V69 migration.", Blocks: "NONE",
  RecommendedTask: "COMPLETED", Status: "CLOSED", Notes: "Closed 2026-08-11; nationwide/full-living-world work remains separate.",
});
const updates = {
  "PROJECT_DOCUMENT_REGISTRY.xlsx": { key: "DocumentId", rows: docs },
  "PROJECT_CANONICAL_DOMAIN_MAP.xlsx": { key: "Domain", rows: [{
    Domain: "Luoyang184HistoricalPersonFamilyRuntime", L0ProjectConstitution: "AGENTS.md",
    L1CanonicalSpec: "Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md|Docs/FAMILY_ORGANIZATION_REFERENCE_V1/README.md|Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md",
    L2CurrentStatus: "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", L3PrimaryReference: `${evidence}/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1_REPORT.md`,
    CanonicalGap: "Writable derived population checkpoint and full living-world loop are not implemented.", MultipleL1Conflict: "NO",
    ReadingEntry: "Docs/TASK_LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1.md",
    ConflictPolicy: "L1 rules and protected package facts win; 32 Facility claims remain unresolved rather than reassigned.",
  }] },
  "IMPLEMENTATION_GAP_REGISTER.xlsx": { key: "GapId", rows: [
    resolvedGap("gap.luoyang184.main-world-projection", "PopulationPersistence", "V69 attaches the protected 400K package through a formal read-through IPermanentPopulationStore."),
    resolvedGap("gap.luoyang184.historical-person-binding", "HistoricalPersons", "25/25 exact existing P-ID bindings; added Person=0; duplicates=0."),
    resolvedGap("gap.luoyang184.family-organization-migration", "FamilyOrganization", "15 retained; f088/f036 corrected without deleting people."),
    resolvedGap("gap.luoyang184.family-center-runtime", "FamilyCenter", "Persisted activation/lifecycle contract exists; 15 honest Deferred states."),
    resolvedGap("gap.luoyang184.facility-assignment-authority", "FacilityPopulation", "V69 marks the external protected package as the one person-assignment authority."),
    resolvedGap("gap.luoyang184.government-office-projection", "Governance", "Canonical Luoyang government plus eight generic Civil/Military Office assignments are projected."),
    {
      GapId: "gap.luoyang184.derived-population-checkpoint", Domain: "PopulationPersistence",
      CanonicalRequirement: "Long-running Person changes must preserve stable identities and remain traceable without rewriting protected initialization packages.",
      CurrentImplementation: "The formal adapter is intentionally read-only; direct commit is rejected.",
      GapDescription: "No writable derived partition checkpoint/change overlay exists yet.", Severity: "HIGH", BlocksNextDevelopment: "YES",
      SuggestedFutureTask: "LUOYANG-184-LIVING-WORLD-CLOSED-LOOP-V1", Evidence: `${evidence}/11_NEXT_LUOYANG_DEVELOPMENT_STAGE.md`,
      RequiredContract: "Partition transaction|stable PersonId|sequential save migration|round-trip|conservation audit", Blocks: "LongRunningLivingWorldMutation",
      RecommendedTask: "LUOYANG-184-LIVING-WORLD-CLOSED-LOOP-V1", Status: "OPEN", Notes: "Next candidate; not automatically authorized.",
    },
    {
      GapId: "gap.luoyang184.living-world-economic-loop", Domain: "WorldSimulation",
      CanonicalRequirement: "The same 400K persons and 2,084 facilities must drive residence, work, production, consumption, market and supply facts.",
      CurrentImplementation: "Identity, residence/work references, facilities and historical metadata are integrated, but the complete tick loop is not.",
      GapDescription: "Residence→Work→Production→Consumption→Market→Supply is not yet one closed loop.", Severity: "HIGH", BlocksNextDevelopment: "YES",
      SuggestedFutureTask: "LUOYANG-184-LIVING-WORLD-CLOSED-LOOP-V1", Evidence: `${evidence}/11_NEXT_LUOYANG_DEVELOPMENT_STAGE.md`,
      RequiredContract: "Deterministic partitions|ledger conservation|no attention-dependent facts|50-year evidence", Blocks: "LivingWorldAcceptance",
      RecommendedTask: "LUOYANG-184-LIVING-WORLD-CLOSED-LOOP-V1", Status: "OPEN", Notes: "Next candidate; not automatically authorized.",
    },
  ] },
  "OPEN_DECISION_REGISTRY.xlsx": { key: "OpenDecisionId", rows: [
    {
      OpenDecisionId: "open.luoyang184.first-family-center-designation", Domain: "FamilyCenter",
      Question: "Which Luoyang FamilyOrganization, if any, first satisfies all five FamilyCenter activation prerequisites?", Status: "DEFERRED",
      WhyOpen: "V69 contract is implemented, but all 15 organizations still lack a qualified real Facility/manager/designation combination.",
      NeededEvidence: "Real Facility|FamilyManagement capability|legal owner/control|manager Person|Primary/Local designation|active current activity",
      OwnerRole: "GameplayDesign|HistoricalResearch|DomainEngineering", Blocks: "FamilyCenter activation only; not the living-world base loop.",
      SourceDocument: `${evidence}/04_LUOYANG_FAMILYCENTER_RUNTIME_STATE.xlsx`, RecommendedNextReview: "After a real family-management Facility and manager activity exist",
      Notes: "Default remains Deferred/None; do not infer center from residence, estate reference or organization presence.",
    },
    {
      OpenDecisionId: "open.luoyang184.metropolitan-family-facility-claims", Domain: "FamilyOrganization",
      Question: "How should eight generated metropolitan family organizations relate to the four facilities each currently claims?", Status: "OPEN",
      WhyOpen: "All eight source organizations claim the same four facilities, while Facility owner/controller IDs name other estate/community organizations.",
      NeededEvidence: "Correct source ownership|operator agreement|estate organization crosswalk|historical/gameplay intent",
      OwnerRole: "ContentDesign|DomainEngineering", Blocks: "Asset ownership and future center designation; does not block Person integration.",
      SourceDocument: `${evidence}/03_LUOYANG_FAMILYORGANIZATION_RUNTIME_MIGRATION.xlsx`, RecommendedNextReview: "Before granting organization facilities or FamilyCenter status",
      Notes: "V69 retains 32 unresolved claims and performs zero ownership transfer.",
    },
  ] },
  "DOCUMENT_CONFLICT_REGISTER.xlsx": { key: "ConflictId", rows: [
    {
      ConflictId: "conflict.luoyang184.facility-inline-person-lists-vs-binary-index", Domain: "FacilityPopulation",
      DocumentA: "Luoyang184UrbanInitializationV1/facilities.json", DocumentB: "Luoyang184UrbanInitializationV1/persons.bin",
      ConflictDescription: "Legacy inline person arrays disagree with formal binary Person assignment indexes.",
      CurrentPreferredRule: "V69 explicitly makes the protected external population package the assignment authority; inline arrays are not imported.",
      AuthorityReason: "Full 400K binary audit and V69 external-assignment contract.", ResolutionStatus: "RESOLVED_IN_V69",
      RequiredAction: "Keep only one authority in future derived checkpoints.", RiskIfIgnored: "Ghost or duplicate residents/workers.",
    },
    {
      ConflictId: "conflict.luoyang184.metropolitan-family-shared-facility-claims", Domain: "FamilyOrganization",
      DocumentA: "Luoyang184MetropolitanInitializationV1/family_organizations.json", DocumentB: "Luoyang184MetropolitanInitializationV1/facilities.json",
      ConflictDescription: "Eight generated family organizations each claim the same four facilities, whose Owner/Controller IDs refer to other organizations.",
      CurrentPreferredRule: "Do not transfer ownership/control; retain all 32 source claims as unresolved runtime evidence.",
      AuthorityReason: "Facility owner/controller is the legal runtime authority; an inconsistent source claim cannot override it.", ResolutionStatus: "CONTAINED_UNRESOLVED",
      RequiredAction: "Research or author an explicit crosswalk/operator agreement before asset or center activation.", RiskIfIgnored: "Eight organizations simultaneously own the same facilities and create false FamilyCenters.",
    },
  ] },
};

function col(index) {
  let n = index + 1, out = "";
  while (n > 0) { const r = (n - 1) % 26; out = String.fromCharCode(65 + r) + out; n = Math.floor((n - 1) / 26); }
  return out;
}
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
async function render(workbook, file) {
  for (const sheet of workbook.worksheets.items) {
    const values = sheet.getUsedRange(true)?.values ?? [[]];
    const columns = Math.max(1, ...values.slice(0, 60).map(row => row.length));
    const rows = Math.max(1, Math.min(values.length, 60));
    const preview = await workbook.render({ sheetName: sheet.name, range: `A1:${col(columns - 1)}${rows}`, autoCrop: "all", scale: 0.7, format: "png" });
    await fs.writeFile(path.join(previewDir, `${file.replace(/\.xlsx$/i, "")}__${sheet.name}.png`), new Uint8Array(await preview.arrayBuffer()));
  }
}

const results = [];
for (const [file, spec] of Object.entries(updates)) {
  const filePath = path.join(dir, file);
  const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(filePath));
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
  await render(workbook, file);
  results.push({ file, added, updated });
}
await fs.writeFile(path.join(previewDir, "registry_update_summary.json"), JSON.stringify(results, null, 2), "utf8");
console.log(JSON.stringify({ status: "PASS", results }, null, 2));
