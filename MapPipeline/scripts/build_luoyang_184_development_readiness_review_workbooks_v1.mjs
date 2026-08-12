import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const artifactEntry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY || "@oai/artifact-tool";
const { SpreadsheetFile, Workbook } = await import(pathToFileURL(artifactEntry).href);

const repo = "E:/project/gamedevelop/MandateOfHeroes";
const sourcePath = path.join(repo, "outputs/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1/readiness_review_workdata.json");
const outputDir = path.join(repo, "Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1");
const previewDir = path.join(repo, "outputs/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1/previews/review_workbooks");
const workdata = JSON.parse(await fs.readFile(sourcePath, "utf8"));

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const specs = [
  {
    file: "01_LUOYANG_184_DEVELOPMENT_READINESS_MATRIX.xlsx",
    title: "洛阳184开发准备度矩阵",
    purpose: "汇总所有领域的准备度、严重度、门禁影响、机器发现和受保护包文件合同。",
    sheets: [
      ["准备度矩阵", workdata.readiness_matrix],
      ["审计发现", workdata.audit_findings],
      ["包文件合同", workdata.package_file_audit],
    ],
  },
  {
    file: "02_LUOYANG_RUNTIME_ENTITY_MAPPING_AUDIT.xlsx",
    title: "洛阳运行时实体映射审计",
    purpose: "审查 CanonicalPlace、人口、家户、Facility、组织、军队与事件从来源包到主运行时的映射状态。",
    sheets: [["实体映射", workdata.runtime_mapping]],
  },
  {
    file: "03_LUOYANG_HISTORICAL_PERSON_RUNTIME_MAPPING.xlsx",
    title: "洛阳历史人物运行时映射",
    purpose: "逐人证明25个历史PersonId与40万人口ordinal、母库和184剧本的精确唯一绑定。",
    sheets: [["历史人物映射", workdata.historical_person_mapping]],
  },
  {
    file: "04_LUOYANG_CLAN_FAMILYORGANIZATION_MIGRATION_PLAN.xlsx",
    title: "洛阳Clan与FamilyOrganization迁移计划",
    purpose: "冻结7个城市旧组织的稳定ID保留、成员纠错和不删除永久人物的迁移边界。",
    sheets: [["组织迁移", workdata.family_organization_migration]],
  },
  {
    file: "05_LUOYANG_FAMILYCENTER_IMPLEMENTATION_READINESS.xlsx",
    title: "洛阳FamilyCenter实现准备度",
    purpose: "逐候选核对真实Facility、FamilyManagement能力、合法控制、管理者和Primary/Local指定五要件。",
    sheets: [["中心准备度", workdata.family_center_readiness]],
  },
  {
    file: "06_LUOYANG_FACILITY_HISTORICAL_REFERENCE_RUNTIME_CROSSWALK.xlsx",
    title: "洛阳Facility历史参考—运行时Crosswalk",
    purpose: "审计2084项Facility的稳定ID、Cell、Owner/Controller、容量、190参考和旧内联人物列表冲突。",
    sheets: [
      ["FacilityCrosswalk", workdata.facility_crosswalk],
      ["旧人物列表", workdata.stale_facility_person_lists],
    ],
  },
  {
    file: "07_LUOYANG_POPULATION_HOUSEHOLD_RESIDENCE_AUDIT.xlsx",
    title: "洛阳人口—家户—住宅口径审计",
    purpose: "冻结20万、27万、40万、70万与全国统计参考之间的包含关系和防重复物化规则。",
    sheets: [["人口口径", workdata.population_audit]],
  },
  {
    file: "09_LUOYANG_190_FUTURE_COMPATIBILITY_AUDIT.xlsx",
    title: "洛阳190未来兼容审计",
    purpose: "冻结184到190必须复用的稳定身份、参考状态和未实现HistoricalChange边界。",
    sheets: [["190兼容", workdata.future_190]],
  },
  {
    file: "10_LUOYANG_HULAO_WAVE0_DEPENDENCY_REVIEW.xlsx",
    title: "洛阳—虎牢—函谷Wave0依赖审查",
    purpose: "将洛阳Core与虎牢、函谷的独立Place依赖和延后原因做成可审计门禁。",
    sheets: [["Wave0依赖", workdata.wave0_dependency]],
  },
];

function excelColumn(index) {
  let value = index + 1;
  let result = "";
  while (value > 0) {
    const remainder = (value - 1) % 26;
    result = String.fromCharCode(65 + remainder) + result;
    value = Math.floor((value - 1) / 26);
  }
  return result;
}

function scalar(value) {
  if (value === undefined || value === null) return "";
  if (Array.isArray(value)) return value.join("|");
  if (typeof value === "object") return JSON.stringify(value);
  return value;
}

function unionHeaders(rows) {
  const headers = [];
  for (const row of rows) {
    for (const key of Object.keys(row)) if (!headers.includes(key)) headers.push(key);
  }
  return headers.length ? headers : ["Status"];
}

function safeSheetName(name, used) {
  let result = name.slice(0, 31);
  let suffix = 2;
  while (used.has(result)) {
    const tail = `_${suffix++}`;
    result = name.slice(0, 31 - tail.length) + tail;
  }
  used.add(result);
  return result;
}

function styleCover(sheet, spec) {
  sheet.showGridLines = false;
  sheet.getRange("A1:H2").merge();
  sheet.getRange("A1").values = [[spec.title]];
  sheet.getRange("A1:H2").format = {
    fill: "#173B4D",
    font: { bold: true, color: "#FFFFFF", size: 20 },
    verticalAlignment: "center",
    horizontalAlignment: "left",
  };
  sheet.getRange("A4:B10").values = [
    ["字段", "值"],
    ["Gate A", workdata.summary.gate_a],
    ["Gate B", workdata.summary.gate_b],
    ["状态", workdata.summary.status],
    ["下一任务", workdata.summary.next_task],
    ["用途", spec.purpose],
    ["证据生成", "MapPipeline/scripts/audit_luoyang_184_development_readiness_v1.py"],
  ];
  sheet.getRange("A4:B4").format = { fill: "#2F687A", font: { bold: true, color: "#FFFFFF" } };
  sheet.getRange("A5:A10").format = { fill: "#DDEBF2", font: { bold: true, color: "#173B4D" } };
  sheet.getRange("A4:B10").format.borders = { preset: "all", style: "thin", color: "#B7C9D1" };
  sheet.getRange("A4:B10").format.wrapText = true;
  sheet.getRange("A4:A10").format.columnWidth = 19;
  sheet.getRange("B4:B10").format.columnWidth = 82;
  sheet.getRange("A12:H14").merge();
  sheet.getRange("A12").values = [["使用边界：本工作簿是审查视图，不是运行时事实。历史Reference不得直接物化；所有永久人物不得删除、合并或重新随机。"]];
  sheet.getRange("A12:H14").format = { fill: "#FFF2CC", font: { color: "#7F6000", italic: true }, wrapText: true, verticalAlignment: "center" };
}

function writeDataSheet(sheet, rows, tableName) {
  sheet.showGridLines = false;
  const headers = unionHeaders(rows);
  const matrix = [headers, ...rows.map((row) => headers.map((header) => scalar(row[header])))];
  const endCol = excelColumn(headers.length - 1);
  const endRow = matrix.length;
  sheet.getRangeByIndexes(0, 0, matrix.length, headers.length).values = matrix;
  sheet.getRange(`A1:${endCol}1`).format = {
    fill: "#173B4D",
    font: { bold: true, color: "#FFFFFF" },
    wrapText: true,
    verticalAlignment: "center",
  };
  if (rows.length) {
    sheet.tables.add(`A1:${endCol}${endRow}`, true, tableName);
    sheet.getRange(`A2:${endCol}${endRow}`).format = {
      wrapText: true,
      verticalAlignment: "top",
      borders: { preset: "all", style: "thin", color: "#D9E2E6" },
    };
  }
  sheet.freezePanes.freezeRows(1);
  for (let index = 0; index < headers.length; index++) {
    const header = headers[index].toLowerCase();
    const col = excelColumn(index);
    let width = 20;
    if (header.includes("reason") || header.includes("evidence") || header.includes("finding") || header.includes("action") || header.includes("policy") || header.includes("reference")) width = 44;
    if (header.includes("sha256")) width = 34;
    if (header.includes("id") || header.includes("path")) width = 34;
    if (header.includes("count") || header.includes("capacity") || header.includes("population") || header.includes("ordinal") || header.includes("index")) width = 15;
    sheet.getRange(`${col}:${col}`).format.columnWidth = width;
  }
  sheet.getRange(`1:1`).format.rowHeight = 34;
}

const renderManifest = [];
for (let workbookIndex = 0; workbookIndex < specs.length; workbookIndex++) {
  const spec = specs[workbookIndex];
  const workbook = Workbook.create();
  const usedNames = new Set();
  const cover = workbook.worksheets.add("说明");
  usedNames.add("说明");
  styleCover(cover, spec);
  const renderRanges = new Map([["说明", "A1:H14"]]);
  for (let sheetIndex = 0; sheetIndex < spec.sheets.length; sheetIndex++) {
    const [wantedName, rows] = spec.sheets[sheetIndex];
    const name = safeSheetName(wantedName, usedNames);
    const sheet = workbook.worksheets.add(name);
    writeDataSheet(sheet, rows, `T${workbookIndex + 1}_${sheetIndex + 1}`);
    const headers = unionHeaders(rows);
    renderRanges.set(name, `A1:${excelColumn(headers.length - 1)}${Math.min(rows.length + 1, 60)}`);
  }
  const output = await SpreadsheetFile.exportXlsx(workbook);
  const outputPath = path.join(outputDir, spec.file);
  await output.save(outputPath);

  const sheetInspection = await workbook.inspect({ kind: "sheet", include: "id,name", maxChars: 10000 });
  const formulaInspection = await workbook.inspect({ kind: "formula", maxChars: 10000, options: { maxResults: 200 } });
  const inspectPath = path.join(previewDir, `${spec.file}.inspect.txt`);
  await fs.writeFile(inspectPath, `${sheetInspection.ndjson}\n${formulaInspection.ndjson}\n`, "utf8");
  for (const sheet of workbook.worksheets.items) {
    const preview = await workbook.render({ sheetName: sheet.name, range: renderRanges.get(sheet.name), autoCrop: "all", scale: 0.8, format: "png" });
    const safe = sheet.name.replace(/[^a-zA-Z0-9\u4e00-\u9fff_-]+/g, "_");
    const previewPath = path.join(previewDir, `${spec.file.replace(/\.xlsx$/i, "")}__${safe}.png`);
    await fs.writeFile(previewPath, new Uint8Array(await preview.arrayBuffer()));
    renderManifest.push({ workbook: spec.file, sheet: sheet.name, preview: previewPath.replaceAll("\\", "/") });
  }
}

await fs.writeFile(path.join(previewDir, "render_manifest.json"), JSON.stringify(renderManifest, null, 2), "utf8");
console.log(JSON.stringify({ status: "PASS", workbooks: specs.length, renderedSheets: renderManifest.length, outputDir, previewDir }, null, 2));
