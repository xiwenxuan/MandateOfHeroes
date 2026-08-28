import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const entry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY;
if (!entry) throw new Error("MANDATE_ARTIFACT_TOOL_ENTRY is required");
const { FileBlob, SpreadsheetFile } = await import(pathToFileURL(entry).href);
const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repo = process.env.MANDATE_REPO_ROOT || path.resolve(scriptDirectory, "../..");
const registryDir = path.join(repo, "Docs/KNOWLEDGE_BASE/REGISTRY");
const output = path.join(repo, "outputs/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1/registry_previews");
await fs.mkdir(output, { recursive: true });

const task = "Docs/TASK_WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1.md";
const base = "Docs/HISTORICAL_WORLD_REFERENCE/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1";
const report = `${base}/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1_REPORT.md`;
const validation = `${base}/validation_summary.json`;
const contract = "Docs/HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1/GLOBAL_SPATIAL_FOUNDATION_CONTRACT_V1.md";

const decisions = [
  "Global Cell Grid is the only authoritative spatial partition of the world.",
  "Region is defined by membership of complete Global Cells.",
  "Region boundary is derived from member Cell outer edges.",
  "Region boundaries never split Global Cells.",
  "Region Polygon is derived or reference-only.",
  "Technical Region is independent from AdministrativeRegion.",
  "Chunk is a technical concept, not an authoritative world-space entity.",
  "16x16 remains a spatial or simulation aggregation scale but is not automatically the Terrain or Streaming unit.",
  "Terrain Tile, Streaming Unit, Simulation Aggregation Block and Storage Block may use different technical sizes.",
  "No technical block may modify Stable Global Cell identity.",
];

const specs = {
  "PROJECT_DOCUMENT_REGISTRY.xlsx": { key: "DocumentId", rows: [
    { DocumentId: "doc.region-cell-boundary.task.v1", Path: task, Title: "World Region Cell Boundary And Technical Block Semantics Correction V1", DocumentType: "TaskRecord", Domain: "GlobalSpatialFoundation", Status: "CURRENT", CanonicalFor: "ImplementationScope", ReadPriority: "P0" },
    { DocumentId: "doc.region-cell-boundary.report.v1", Path: report, Title: "Region Cell Boundary And Technical Block Semantics Acceptance Report", DocumentType: "AcceptanceReport", Domain: "GlobalSpatialFoundation", Status: "CURRENT", CanonicalFor: "REGION_CELL_BOUNDARY_CONTRACT_FROZEN", ReadPriority: "P0" },
    { DocumentId: "doc.region-cell-boundary.validation.v1", Path: validation, Title: "Region Cell Boundary Semantics Machine Validation", DocumentType: "MachineValidation", Domain: "GlobalSpatialFoundation", Status: "CURRENT", CanonicalFor: "RegionMembership|Boundary|TechnicalBlocks", ReadPriority: "P0" },
  ]},
  "PROJECT_CANONICAL_DOMAIN_MAP.xlsx": { key: "Domain", rows: [
    { Domain: "GlobalSpatialFoundation", DomainId: "domain.global-spatial-foundation.v1", DomainName: "One World One Global Grid", L0ProjectConstitution: "AGENTS.md", L1CanonicalSpec: contract, L2CurrentStatus: "Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", L3PrimaryReference: report, CanonicalGap: "Terrain Tile and Streaming Unit sizes require real Unity benchmark; 河南尹 final Terrain remains unproduced.", MultipleL1Conflict: "NO", ReadingEntry: contract, ConflictPolicy: "Global Cell membership defines Region; polygons and technical blocks cannot create, cut or renumber Cells.", CurrentStatus: "REGION_CELL_BOUNDARY_CONTRACT_FROZEN", Status: "CURRENT" },
  ]},
  "DESIGN_DECISION_REGISTRY.xlsx": { key: "DecisionId", rows: decisions.map((Decision, index) => ({
    DecisionId: `decision.region-cell-boundary.${String(index + 1).padStart(2, "0")}.v1`,
    Domain: "GlobalSpatialFoundation",
    Title: `Region and technical-block semantic decision ${index + 1}`,
    Decision,
    Status: "ACCEPTED",
    EffectiveFrom: "2026-08-15",
    SourceDocument: contract,
    AffectedSystems: "Map|GIS|Cell|Region|Simulation|Terrain|Streaming|Storage|Persistence|Presentation",
    ReasonSummary: "Keep Global Cell as the final world-space fact while allowing benchmark-driven technical partition sizes.",
  }))},
  "IMPLEMENTATION_GAP_REGISTER.xlsx": { key: "GapId", rows: [
    { GapId: "gap.global-spatial.henan-high-detail-terrain.v1", Domain: "GlobalSpatialFoundation", CanonicalRequirement: "Final 河南尹 Terrain must use benchmarked Terrain and Streaming sizes over frozen Global Cells.", CurrentImplementation: "Global Cell and Region membership contracts are frozen; the former direct-to-terrain step is superseded by a required block-size benchmark.", GapDescription: "Historical entry preserved and rerouted through the benchmark gate.", Severity: "HIGH", BlocksNextDevelopment: "YES", RecommendedTask: "MAP-TERRAIN-STREAMING-BLOCK-SIZE-BENCHMARK-V1", Status: "SUPERSEDED", Evidence: report },
    { GapId: "gap.global-spatial.terrain-streaming-block-benchmark.v1", Domain: "GlobalSpatialFoundation", CanonicalRequirement: "Benchmark 4x4, 8x8 and 16x16 candidates independently for Terrain Tile and Streaming Unit.", CurrentImplementation: "16x16 is retained only as a technical simulation aggregation block; Terrain and Streaming sizes are NOT_YET_FROZEN.", GapDescription: "Real Unity DEM, memory, LOD and loading evidence is required before 河南尹 Terrain production.", Severity: "HIGH", BlocksNextDevelopment: "YES", RecommendedTask: "MAP-TERRAIN-STREAMING-BLOCK-SIZE-BENCHMARK-V1", Status: "OPEN", Evidence: validation },
  ]},
};

function findTableHeader(workbook, key) {
  for (const sheet of workbook.worksheets.items) {
    const values = sheet.getUsedRange(true)?.values ?? [];
    for (let row = 0; row < Math.min(20, values.length); row++) {
      const headers = values[row].map(value => String(value ?? ""));
      if (headers.includes(key)) return { sheet, values, headerRow: row, headers };
    }
  }
  throw new Error(`Missing key column ${key}`);
}

function columnName(index) {
  let result = "";
  let value = index + 1;
  while (value) {
    const remainder = (value - 1) % 26;
    result = String.fromCharCode(65 + remainder) + result;
    value = Math.floor((value - 1) / 26);
  }
  return result;
}

const results = [];
for (const [file, spec] of Object.entries(specs)) {
  const workbookPath = path.join(registryDir, file);
  const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(workbookPath));
  const found = findTableHeader(workbook, spec.key);
  const keyIndex = found.headers.indexOf(spec.key);
  let nextRow = found.values.length;
  let added = 0;
  let updated = 0;
  for (const item of spec.rows) {
    let rowIndex = -1;
    for (let index = found.headerRow + 1; index < found.values.length; index++) {
      if (String(found.values[index][keyIndex] ?? "") === String(item[spec.key])) {
        rowIndex = index;
        break;
      }
    }
    if (rowIndex < 0) {
      rowIndex = nextRow++;
      found.values[rowIndex] = new Array(found.headers.length).fill("");
      added++;
    } else {
      updated++;
    }
    const row = [...found.values[rowIndex]];
    while (row.length < found.headers.length) row.push("");
    for (let column = 0; column < found.headers.length; column++) {
      const header = found.headers[column];
      if (Object.prototype.hasOwnProperty.call(item, header)) row[column] = item[header];
    }
    found.sheet.getRangeByIndexes(rowIndex, 0, 1, found.headers.length).values = [row];
    found.values[rowIndex] = row;
  }
  const lastColumn = columnName(Math.min(found.headers.length, 12) - 1);
  const previewStart = Math.max(found.headerRow + 1, nextRow - 15);
  const preview = await workbook.render({
    sheetName: found.sheet.name,
    range: `A${previewStart + 1}:${lastColumn}${nextRow}`,
    scale: 0.7,
    format: "png",
  });
  await fs.writeFile(path.join(output, file.replace(/\.xlsx$/i, ".png")), new Uint8Array(await preview.arrayBuffer()));
  const errors = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 100 }, summary: "formula errors" });
  await fs.writeFile(path.join(output, file.replace(/\.xlsx$/i, ".errors.ndjson")), errors.ndjson, "utf8");
  if (/#REF|#DIV|#VALUE|#NAME|#N\/A/.test(errors.ndjson)) throw new Error(`${file} formula error`);
  await (await SpreadsheetFile.exportXlsx(workbook)).save(workbookPath);
  results.push({ file, added, updated });
}

await fs.writeFile(path.join(output, "registry_update_summary.json"), JSON.stringify({ status: "PASS", results }, null, 2) + "\n", "utf8");
console.log(JSON.stringify({ status: "PASS", results }, null, 2));
