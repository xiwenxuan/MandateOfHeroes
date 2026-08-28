import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const entry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY;
const repo = process.env.MANDATE_REPO_ROOT;
if (!entry || !repo) throw new Error("MANDATE_ARTIFACT_TOOL_ENTRY and MANDATE_REPO_ROOT are required");
const { FileBlob, SpreadsheetFile, Workbook } = await import(pathToFileURL(entry).href);

const taskId = "HAN-WORLD-STYLE-D-STRATEGIC-LANDSCAPE-VISUAL-REFINEMENT-AND-ZHONGHUA-SOURCE-RECOVERY-V2";
const outputDir = path.join(repo, "Docs", "HISTORICAL_WORLD_REFERENCE",
  "HAN_WORLD_STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_V2");
const qaDir = path.join(repo, "outputs", "HAN_WORLD_STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_V2", "workbooks");
const performancePath = path.join(outputDir, "outputs", "20260816-1910-style-d-v2", "style_d_v2_performance.json");
await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(qaDir, { recursive: true });
const performance = JSON.parse((await fs.readFile(performancePath, "utf8")).replace(/^\uFEFF/, ""));

const colours = {
  navy: "#263B45", slate: "#526E78", pale: "#E9F0F2", cyan: "#B7DCEB",
  green: "#DDEAD5", amber: "#F2E2B8", red: "#F4CCCC", border: "#CAD5D8", white: "#FFFFFF"
};

function normalizeRows(rows) {
  const headers = [];
  for (const row of rows) for (const key of Object.keys(row)) if (!headers.includes(key)) headers.push(key);
  return { headers, matrix: rows.map(row => headers.map(key => row[key] ?? null)) };
}

async function createWorkbook(fileName, title, subtitle, rows, options = {}) {
  const wb = Workbook.create();
  const sheet = wb.worksheets.add("Summary");
  sheet.showGridLines = false;
  const { headers, matrix } = normalizeRows(rows);
  const lastColumn = String.fromCharCode(64 + Math.min(headers.length, 26));
  sheet.getRange(`A1:${lastColumn}1`).merge();
  sheet.getRange("A1").values = [[title]];
  sheet.getRange(`A2:${lastColumn}2`).merge();
  sheet.getRange("A2").values = [[subtitle]];
  sheet.getRange(`A4:${lastColumn}4`).values = [headers];
  if (matrix.length) sheet.getRangeByIndexes(4, 0, matrix.length, headers.length).values = matrix;
  sheet.getRange(`A1:${lastColumn}1`).format = {
    fill: colours.navy, font: { bold: true, color: colours.white, fontSize: 15 },
    rowHeight: 28, verticalAlignment: "center"
  };
  sheet.getRange(`A2:${lastColumn}2`).format = {
    fill: colours.pale, font: { color: colours.navy, italic: true }, wrapText: true, rowHeight: 32
  };
  sheet.getRange(`A4:${lastColumn}4`).format = {
    fill: colours.slate, font: { bold: true, color: colours.white }, wrapText: true,
    borders: { preset: "outside", style: "thin", color: colours.border }
  };
  if (matrix.length) {
    const body = sheet.getRangeByIndexes(4, 0, matrix.length, headers.length);
    body.format = { wrapText: true, verticalAlignment: "top",
      borders: { insideHorizontal: { style: "thin", color: colours.border } } };
    body.conditionalFormats.add("containsText", { text: "PASS", format: { fill: colours.green } });
    body.conditionalFormats.add("containsText", { text: "PARTIAL", format: { fill: colours.amber } });
    body.conditionalFormats.add("containsText", { text: "FAIL", format: { fill: colours.red } });
    body.conditionalFormats.add("containsText", { text: "BLOCKED", format: { fill: colours.red } });
  }
  sheet.freezePanes.freezeRows(4);
  sheet.getRange(`A1:${lastColumn}${Math.max(5, matrix.length + 4)}`).format.autofitColumns();
  for (let col = 0; col < headers.length; col++) {
    const range = sheet.getRangeByIndexes(0, col, Math.max(5, matrix.length + 4), 1);
    range.format.columnWidth = Math.min(34, Math.max(12, options.widths?.[col] ?? 20));
  }
  if (options.numberFormats) {
    for (const [header, format] of Object.entries(options.numberFormats)) {
      const col = headers.indexOf(header);
      if (col >= 0 && matrix.length) sheet.getRangeByIndexes(4, col, matrix.length, 1).format.numberFormat = format;
    }
  }
  if (options.formulas) options.formulas({ wb, sheet, headers, rowCount: matrix.length });
  const filePath = path.join(outputDir, fileName);
  const xlsx = await SpreadsheetFile.exportXlsx(wb);
  await xlsx.save(filePath);
  const preview = await wb.render({ sheetName: "Summary", autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(path.join(qaDir, `${fileName}.png`), new Uint8Array(await preview.arrayBuffer()));
  const inspection = await wb.inspect({ kind: "sheet,region,formula", sheetId: "Summary",
    range: `A1:${lastColumn}${Math.max(5, matrix.length + 4)}`, maxChars: 6000, tableMaxRows: 12, tableMaxCols: 20 });
  await fs.writeFile(path.join(qaDir, `${fileName}.inspect.ndjson`), inspection.ndjson, "utf8");
  return filePath;
}

const gapRows = [
  { Area: "River sharp bend", V1: "FAIL", V2: "PARTIAL", Evidence: "08_STYLE_D_V2_RIVER_SHARP_BEND.png", Finding: "Adaptive ribbon and bounded miter implemented; one canonical source-segment endpoint seam remains visible." },
  { Area: "River bank", V1: "FAIL", V2: "PASS_WITH_ART_LIMITS", Evidence: "07_STYLE_D_V2_RIVER_GENTLE.png", Finding: "Bank and water share centerline, sampling and width; material transition remains prototype-grade." },
  { Area: "Forest WORLD", V1: "PARTIAL", V2: "PASS", Evidence: "09_STYLE_D_V2_FOREST_WORLD.png", Finding: "Macro forest is carried by the terrain surface without individual-tree residency." },
  { Area: "Forest REGION", V1: "FAIL", V2: "PASS_WITH_ART_LIMITS", Evidence: "10_STYLE_D_V2_FOREST_REGION.png", Finding: "Deterministic canopy clusters follow density and terrain; cluster art remains procedural." },
  { Area: "Forest CITY", V1: "FAIL", V2: "PASS_WITH_ART_LIMITS", Evidence: "11_STYLE_D_V2_FOREST_CITY.png", Finding: "Deterministic individual-tree batch and clearing are visible." },
  { Area: "City terrain detail", V1: "FAIL", V2: "PARTIAL", Evidence: "13_STYLE_D_V2_TERRAIN_DETAIL.png", Finding: "Presentation-only 8x refinement works, while low-frequency source blocks remain visible." },
  { Area: "Mountain system", V1: "PARTIAL", V2: "PARTIAL", Evidence: "06_STYLE_D_V2_MOUNTAIN.png", Finding: "Ridge/valley separation improved; formal mountain-chain semantics are still inferred from DEM." },
  { Area: "Global grid facts", V1: "PASS", V2: "PASS", Evidence: "15_STYLE_D_V2_BACKGROUND_GRID_OFF.png", Finding: "3314x2176, 2000m Cell contract unchanged; no simulation SubCell introduced." }
];

const riverContract = [
  { Contract: "Canonical river identity", Rule: "GlobalRiverId, anchor and source geometry are read-only", Implementation: "GlobalRiverVisualGenerator consumes catalog only", Status: "PASS" },
  { Contract: "Adaptive tessellation", Rule: "More samples on curvature and long segments", Implementation: "AdaptiveSample(max segment by visual LOD)", Status: "PASS" },
  { Contract: "Join", Rule: "Bounded miter with bevel fallback", Implementation: "MiterLimit 1.55-1.72", Status: "PASS" },
  { Contract: "Bank synchronization", Rule: "Water and bank share centerline, widths and samples", Implementation: "One four-vertex cross-section", Status: "PASS" },
  { Contract: "Terrain conform", Rule: "Sample the same presentation-height provider", Implementation: "Water and both banks sample lateral terrain", Status: "PASS_WITH_ART_LIMITS" },
  { Contract: "Confluence", Rule: "Avoid visible independent-mesh overlap", Implementation: "No union/junction mesh in V2", Status: "PARTIAL" }
];

const riverValidation = [
  { Metric: "Invalid triangles", Actual: 0, Tolerance: 0, Result: "PASS" },
  { Metric: "NaN vertices", Actual: 0, Tolerance: 0, Result: "PASS" },
  { Metric: "Extreme miter", Actual: 0, Tolerance: 0, Result: "PASS" },
  { Metric: "Machine topology holes", Actual: 0, Tolerance: 0, Result: "PASS" },
  { Metric: "Detected self intersection", Actual: 0, Tolerance: 0, Result: "PASS_BOUNDED_DIAGNOSTIC" },
  { Metric: "Visible source endpoint seam", Actual: 1, Tolerance: 0, Result: "PARTIAL" },
  { Metric: "Confluence union rule", Actual: 0, Tolerance: 1, Result: "PARTIAL" }
];

const forestLod = [
  { LOD: "WORLD", Representation: "Terrain surface density/tint", StableCoordinate: "Global projected metres", GameObjectsPerTree: 0, Status: "PASS" },
  { LOD: "REGION", Representation: "Combined canopy clusters", StableCoordinate: "Global lattice + deterministic jitter", GameObjectsPerTree: 0, Status: "PASS_WITH_ART_LIMITS" },
  { LOD: "CITY", Representation: "Combined individual-tree mesh", StableCoordinate: "Global lattice + deterministic jitter", GameObjectsPerTree: 0, Status: "PASS_WITH_ART_LIMITS" }
];

const forestRules = [
  { Rule: "Density", Input: "Authoritative surface + moisture neighborhood", Scale: "global continuous", Result: "Repeatable forest amount", Status: "PASS" },
  { Rule: "Cluster acceptance", Input: "stable hash < 0.19", Scale: "REGION", Result: "sparse strategic canopy clusters", Status: "PASS" },
  { Rule: "Individual acceptance", Input: "stable hash < 0.62", Scale: "CITY", Result: "denser visible trees", Status: "PASS" },
  { Rule: "Clearing", Input: "global-coordinate clearing noise", Scale: "REGION/CITY", Result: "non-uniform open areas", Status: "PASS" },
  { Rule: "Tile continuity", Input: "global candidate lattice", Scale: "all resident tiles", Result: "same coordinate yields same vegetation", Status: "PASS" }
];

const terrainContract = [
  { Detail: "WORLD", SubdivisionFactor: 1, MicroReliefMetres: 0, CreatesSimulationSubCells: "NO", FormalCellMetres: 2000, Status: "PASS" },
  { Detail: "REGION", SubdivisionFactor: 2, MicroReliefMetres: 5, CreatesSimulationSubCells: "NO", FormalCellMetres: 2000, Status: "PASS" },
  { Detail: "CITY", SubdivisionFactor: 4, MicroReliefMetres: 12, CreatesSimulationSubCells: "NO", FormalCellMetres: 2000, Status: "PASS" },
  { Detail: "CLOSE_PREVIEW", SubdivisionFactor: 8, MicroReliefMetres: 18, CreatesSimulationSubCells: "NO", FormalCellMetres: 2000, Status: "PASS_WITH_VISUAL_GAP" }
];

const perfRows = performance.samples.map((sample, index) => ({
  Sample: index + 1, View: sample.view, Detail: sample.detail, FrameMs: sample.frame_ms,
  TerrainGenerationMs: sample.terrain_generation_ms, TerrainVertices: sample.terrain_vertices,
  TerrainMeshBytes: sample.terrain_mesh_bytes, TerrainMeshMiB: null, RiverSamples: sample.river_adaptive_samples,
  DrawCalls: sample.draw_calls, VegetationBatches: sample.vegetation_batches
}));

const performanceOptions = {
  numberFormats: { FrameMs: "0.000", TerrainGenerationMs: "0.000", TerrainMeshBytes: "#,##0",
    TerrainMeshMiB: "0.00", TerrainVertices: "#,##0" },
  formulas: ({ sheet, headers, rowCount }) => {
    const bytesColumn = String.fromCharCode(65 + headers.indexOf("TerrainMeshBytes"));
    const mibColumn = String.fromCharCode(65 + headers.indexOf("TerrainMeshMiB"));
    sheet.getRange(`${mibColumn}5`).formulas = [[`=${bytesColumn}5/1048576`]];
    if (rowCount > 1) sheet.getRange(`${mibColumn}5:${mibColumn}${4 + rowCount}`).fillDown();
  }
};

const mountainRows = [
  { Feature: "Mountain chains", Rule: "DEM continuity + restrained vertical exaggeration", Evidence: "06_STYLE_D_V2_MOUNTAIN.png", Status: "PARTIAL" },
  { Feature: "Ridges", Rule: "slope/curvature/ridge shader separation", Evidence: "06_STYLE_D_V2_MOUNTAIN.png", Status: "PASS_WITH_DATA_LIMIT" },
  { Feature: "Valleys", Rule: "valley tint + shared terrain geometry", Evidence: "04_STYLE_D_V2_REGION.png", Status: "PASS_WITH_DATA_LIMIT" },
  { Feature: "Foothills", Rule: "presentation micro-relief and macro variation", Evidence: "13_STYLE_D_V2_TERRAIN_DETAIL.png", Status: "PARTIAL" }
];

const plainRows = [
  { Feature: "Strategic readability", Rule: "low relief with route-scale texture", Evidence: "12_STYLE_D_V2_PLAIN.png", Status: "PASS" },
  { Feature: "Surface variation", Rule: "global-coordinate macro/fine colour variation", Evidence: "12_STYLE_D_V2_PLAIN.png", Status: "PASS_WITH_ART_LIMITS" },
  { Feature: "Cell concealment", Rule: "Cell overlay remains debug-only", Evidence: "15_STYLE_D_V2_BACKGROUND_GRID_OFF.png", Status: "PASS" },
  { Feature: "Development semantics", Rule: "visual variation does not create facilities or resources", Evidence: "Runtime contract", Status: "PASS" }
];

const lodRows = [
  { Scale: "WORLD", Camera: "CAM_STYLE_D_WORLD", Terrain: "Authoritative sampled world mesh", River: "WORLD width + coarse adaptive ribbon", Forest: "surface density", Status: "PASS" },
  { Scale: "REGION", Camera: "CAM_STYLE_D_REGION", Terrain: "2x presentation detail", River: "region adaptive ribbon", Forest: "canopy clusters", Status: "PASS_WITH_ART_LIMITS" },
  { Scale: "CITY_DISTANCE", Camera: "CAM_STYLE_D_CITY_DISTANCE", Terrain: "4x presentation detail", River: "city adaptive ribbon", Forest: "individual trees", Status: "PASS_WITH_ART_LIMITS" },
  { Scale: "CLOSE_PREVIEW", Camera: "CAM_STYLE_D_TERRAIN_DETAIL", Terrain: "8x presentation detail", River: "close adaptive ribbon", Forest: "individual trees", Status: "PARTIAL" }
];

const comparisonRows = gapRows.map(row => ({
  Area: row.Area, V1Status: row.V1, V2Status: row.V2, SameCameraEvidence: row.Evidence,
  Improvement: row.V2.startsWith("PASS") ? "YES" : row.V2 === "PARTIAL" && row.V1 === "FAIL" ? "PARTIAL" : "NOT_PROVEN"
}));

const acceptanceRows = [
  { Item: "MOUNTAIN_SYSTEM_STATUS", Status: "PARTIAL", Evidence: "06_STYLE_D_V2_MOUNTAIN.png", NextAction: "User review; later semantic mountain-chain data" },
  { Item: "RIVER_MESH_STATUS", Status: "PARTIAL", Evidence: "07/08 screenshots + machine diagnostics", NextAction: "Remove canonical endpoint seam and add junction mesh" },
  { Item: "RIVER_BANK_STATUS", Status: "PASS_WITH_ART_LIMITS", Evidence: "07_STYLE_D_V2_RIVER_GENTLE.png", NextAction: "Material/art pass after approval" },
  { Item: "FOREST_WORLD_STATUS", Status: "PASS", Evidence: "09_STYLE_D_V2_FOREST_WORLD.png", NextAction: "User review" },
  { Item: "FOREST_REGION_STATUS", Status: "PASS_WITH_ART_LIMITS", Evidence: "10_STYLE_D_V2_FOREST_REGION.png", NextAction: "Replace prototype canopy art later" },
  { Item: "FOREST_CITY_STATUS", Status: "PASS_WITH_ART_LIMITS", Evidence: "11_STYLE_D_V2_FOREST_CITY.png", NextAction: "Replace prototype tree art later" },
  { Item: "PLAIN_STATUS", Status: "PASS", Evidence: "12_STYLE_D_V2_PLAIN.png", NextAction: "User review" },
  { Item: "TERRAIN_CITY_DETAIL_STATUS", Status: "PARTIAL", Evidence: "13_STYLE_D_V2_TERRAIN_DETAIL.png", NextAction: "Reduce low-frequency blocks without changing Cell facts" },
  { Item: "WORLD_REGION_TRANSITION_STATUS", Status: "PARTIAL", Evidence: "14_STYLE_D_V2_WORLD_TO_CITY_MID.png", NextAction: "Add continuous morph/cross-fade" }
];

const sourceAttempts = [
  { Attempt: "Existing prior target", Mode: "preserve-only", Target: "_external_reference/ZhongHuaSanGuoZhi-New-Code", DurationSeconds: null, Result: "FAILED_EMPTY_GIT_PRESERVED", Evidence: "Only incomplete .git; no working tree" },
  { Attempt: "Standard sandbox clone", Mode: "standard", Target: "...-v2-standard", DurationSeconds: 0, Result: "BLOCKED_SANDBOX_PROXY", Evidence: "127.0.0.1 proxy denied" },
  { Attempt: "Standard escalated clone", Mode: "standard", Target: "...-v2-standard-escalated", DurationSeconds: 130, Result: "NO_TRANSFER_TERMINATED", Evidence: "19 tiny .git files; task-owned process stopped" },
  { Attempt: "HTTP/1.1 clone", Mode: "http.version=HTTP/1.1", Target: "...-v2-http11", DurationSeconds: 21.081, Result: "NETWORK_BLOCKED", Evidence: "cannot connect github.com:443" },
  { Attempt: "Shallow clone", Mode: "--depth 1", Target: "...-v2-shallow", DurationSeconds: 21.112, Result: "NETWORK_BLOCKED", Evidence: "cannot connect github.com:443" },
  { Attempt: "Connectivity diagnostics", Mode: "ls-remote + curl + DNS", Target: "github.com", DurationSeconds: 21.074, Result: "NETWORK_BLOCKED", Evidence: "DNS 20.205.243.166; TCP/TLS 443 unavailable; WinHTTP direct" }
];

const localSource = [
  { Check: "ZHONGHUA_SOURCE_CLONED", Value: "NO", Authority: "Local filesystem audit", Status: "SOURCE_CLONE_BLOCKED_BY_NETWORK_V2" },
  { Check: "SOURCE_RESEARCH_STATUS", Value: "API_STATIC_RESEARCH_WITH_NETWORK_BLOCKER", Authority: "Prior API audit only", Status: "PARTIAL" },
  { Check: "Candidate repository", Value: "kpxp/ZhongHuaSanGuoZhi", Authority: "Prior API metadata", Status: "NOT_LOCAL_CONFIRMED" },
  { Check: "Prior observed HEAD", Value: "50f00168e005f7e5d8576e5adc215b1fbe2f8fa5", Authority: "Prior API metadata", Status: "NOT_LOCAL_CONFIRMED" },
  { Check: "License", Value: "UNRESOLVED", Authority: "No local LICENSE file", Status: "BLOCKED" },
  { Check: "External source/assets copied", Value: "NO", Authority: "Clean-room implementation audit", Status: "PASS" }
];

const specs = [
  ["01_STYLE_D_V2_VISUAL_GAP_AUDIT.xlsx", "Style D V2 Visual Gap Audit", "Frozen V1 evidence compared with the same-world V2 prototype; PARTIAL is intentionally not upgraded to PASS.", gapRows],
  ["02_RIVER_MESH_V2_CONTRACT.xlsx", "River Mesh V2 Contract", "Presentation-only mesh contract; canonical river identity and geometry remain unchanged.", riverContract],
  ["03_RIVER_BEND_AND_JOIN_VALIDATION.xlsx", "River Bend and Join Validation", "Machine checks plus visual inspection. A visible canonical source-segment seam remains a declared gap.", riverValidation],
  ["04_FOREST_PRESENTATION_LOD_CONTRACT.xlsx", "Forest Presentation LOD Contract", "WORLD / REGION / CITY representations share authoritative world facts and deterministic global coordinates.", forestLod],
  ["05_FOREST_DENSITY_AND_CLUSTER_RULES.xlsx", "Forest Density and Cluster Rules", "Presentation density does not create resources, facilities, people or simulation SubCells.", forestRules],
  ["06_VISUAL_TERRAIN_DETAIL_RESOLUTION_CONTRACT.xlsx", "Visual Terrain Detail Resolution Contract", "Global Cell resolution is not visual terrain resolution; every level keeps the formal 2000m Cell.", terrainContract],
  ["07_TERRAIN_DETAIL_PERFORMANCE_BENCHMARK.xlsx", "Terrain Detail Performance Benchmark", "Controlled Unity PlayMode evidence; GPU timing is unavailable in batch mode.", perfRows, performanceOptions],
  ["08_MOUNTAIN_SYSTEM_PRESENTATION_V2.xlsx", "Mountain System Presentation V2", "Strategic-landscape rendering derived from the authoritative DEM; no invented mountain-world facts.", mountainRows],
  ["09_PLAIN_STRATEGIC_PRESENTATION_V2.xlsx", "Plain Strategic Presentation V2", "Readable plains for routes, settlement and warfare without exposing Cell-board visuals.", plainRows],
  ["10_WORLD_REGION_CITY_LOD_STYLE_CONTRACT.xlsx", "WORLD / REGION / CITY LOD Style Contract", "One world, one spatial identity, multiple presentation resolutions.", lodRows],
  ["11_STYLE_D_V1_V2_VISUAL_COMPARISON.xlsx", "Style D V1 / V2 Visual Comparison", "V1 screenshots are copied unchanged; V2 screenshots use the corresponding frozen cameras.", comparisonRows],
  ["12_STYLE_D_V2_VISUAL_ACCEPTANCE.xlsx", "Style D V2 Visual Acceptance", "Independent status per requested visual dimension. This package stops at user-review readiness.", acceptanceRows],
  ["13_STYLE_D_V2_PERFORMANCE_AUDIT.xlsx", "Style D V2 Performance Audit", "Measured terrain generation, geometry, river sampling and draw-call observations.", perfRows, performanceOptions],
  ["14_ZHONGHUA_SOURCE_CLONE_RECOVERY_AUDIT.xlsx", "Zhonghua Source Clone Recovery Audit", "Finite retry sequence completed. Existing failed target was preserved; no indefinite retries were made.", sourceAttempts],
  ["15_ZHONGHUA_LOCAL_SOURCE_CONFIRMATION.xlsx", "Zhonghua Local Source Confirmation", "No local source tree or license was obtained; API metadata is not represented as local confirmation.", localSource]
];

for (const [file, title, subtitle, rows, options] of specs) await createWorkbook(file, title, subtitle, rows, options);

async function updateRegistry(relativePath, dataRow, note) {
  const absolute = path.join(repo, relativePath);
  const wb = await SpreadsheetFile.importXlsx(await FileBlob.load(absolute));
  const notes = wb.worksheets.getItem("说明");
  const data = wb.worksheets.getItem("数据");
  const notesUsed = notes.getUsedRange(true);
  const dataUsed = data.getUsedRange(true);
  const notesRows = notesUsed.values.length;
  const dataRows = dataUsed.values.length;
  const dataCols = dataUsed.values[2].length;
  const existingNoteIndex = notesUsed.values.findIndex((row, index) => index >= 3 && row[1] === taskId);
  const noteTargetRow = existingNoteIndex >= 0 ? existingNoteIndex : notesRows;
  if (existingNoteIndex < 0)
    notes.getRangeByIndexes(noteTargetRow, 0, 1, 5).copyFrom(notes.getRangeByIndexes(notesRows - 1, 0, 1, 5), "all");
  notes.getRangeByIndexes(noteTargetRow, 0, 1, 5).values = [[path.basename(relativePath), taskId,
    String(dataRows - 3), "1", note]];
  notes.getRange("B2").values = [[Math.max(notesRows, noteTargetRow + 1) - 2]];
  const existingDataIndex = dataUsed.values.findIndex((row, index) => index >= 3 && row[0] === dataRow[0]);
  const dataTargetRow = existingDataIndex >= 0 ? existingDataIndex : dataRows;
  if (existingDataIndex < 0)
    data.getRangeByIndexes(dataTargetRow, 0, 1, dataCols).copyFrom(data.getRangeByIndexes(dataRows - 1, 0, 1, dataCols), "all");
  const row = new Array(dataCols).fill(null);
  for (let col = 0; col < Math.min(dataRow.length, dataCols); col++) row[col] = dataRow[col];
  data.getRangeByIndexes(dataTargetRow, 0, 1, dataCols).values = [row];
  data.getRange("B2").values = [[Math.max(dataRows, dataTargetRow + 1) - 2]];
  const xlsx = await SpreadsheetFile.exportXlsx(wb);
  await xlsx.save(absolute);
  const preview = await wb.render({ sheetName: "说明", autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(path.join(qaDir, `${path.basename(relativePath)}.png`), new Uint8Array(await preview.arrayBuffer()));
  const inspection = await wb.inspect({ kind: "sheet,region", maxChars: 6500, tableMaxRows: 8, tableMaxCols: 30 });
  await fs.writeFile(path.join(qaDir, `${path.basename(relativePath)}.inspect.ndjson`), inspection.ndjson, "utf8");
}

await updateRegistry("Docs/KNOWLEDGE_BASE/REGISTRY/DESIGN_DECISION_REGISTRY.xlsx", [
  "DEC-MAP-STYLE-D-V2-001", "MapPresentation", "Global Cell resolution is not visual terrain resolution",
  "Style D may refine presentation vertices at REGION/CITY/CLOSE levels without creating simulation SubCells or changing 2000m Cell facts.",
  "FROZEN", "2026-08-16", "Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_V2/STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_V2_REPORT.md",
  null, "GLOBAL_SPATIAL_FOUNDATION_CONTRACT_V1.md|MAP_ART_RESOURCE_PLAN.md|GAME_SYSTEMS_MASTER_AND_STATUS.md",
  "Map|Presentation|SpatialFoundation", "User task and V2 implementation contract", "User visual review pending", "No nationwide rollout in this task"
], "登记Style D V2表现分辨率与正式Cell分辨率分离的冻结决策；不改世界事实。" );

await updateRegistry("Docs/KNOWLEDGE_BASE/REGISTRY/IMPLEMENTATION_GAP_REGISTER.xlsx", [
  "IMP-GAP-STYLE-D-V2-001", "MapPresentation", "Style D V2 river/terrain visual acceptance",
  "Adaptive river ribbon, forest LOD and presentation terrain detail prototype implemented",
  "One river source-segment endpoint seam, confluence union, low-frequency city terrain blocks and continuous LOD morph remain PARTIAL",
  "S2", "NO", "HAN-WORLD-STYLE-D-V3-USER-REVIEW-FOLLOWUP", "08/13/14 V2 screenshots and VISUAL_ACCEPTANCE_REPORT.md",
  "Preserve canonical geometry and 2000m Cell facts", null, null, "User review follow-up", "OPEN", "No nationwide rollout before review"
], "登记V2视觉仍为PARTIAL的锐弯接缝、汇流、城市块状感和连续过渡缺口。" );

await updateRegistry("Docs/KNOWLEDGE_BASE/REGISTRY/PROJECT_DOCUMENT_REGISTRY.xlsx", [
  "doc.hanworld.style-d-v2", "Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_V2/STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_V2_REPORT.md",
  "Style D Strategic Landscape Visual Refinement V2 Report", "Map", "Presentation", "DevelopmentEvidence",
  "L3", "CURRENT", "2026-08-16", "2026-08-16", "Style D V2 implementation and acceptance evidence", null, null, null,
  "Docs/MAP_ART_RESOURCE_PLAN.md|Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md", taskId,
  "Mandate.Presentation", "NORMAL", "Codex|Developer|Designer", "3", null, "User visual review required", "V2 prototype only", "No national rollout"
], "登记Style D V2任务书、实现报告、验收证据和15份工作簿入口。" );

const formalFiles = await fs.readdir(outputDir);
const expected = specs.map(item => item[0]);
for (const name of expected) if (!formalFiles.includes(name)) throw new Error(`Missing workbook: ${name}`);
console.log(JSON.stringify({ taskId, created: expected.length, registriesUpdated: 3, outputDir }, null, 2));
