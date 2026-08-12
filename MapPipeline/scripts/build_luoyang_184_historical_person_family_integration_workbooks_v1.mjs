import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const artifactEntry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY || "@oai/artifact-tool";
const { FileBlob, SpreadsheetFile, Workbook } = await import(pathToFileURL(artifactEntry).href);

const repo = "E:/project/gamedevelop/MandateOfHeroes";
const sourcePath = path.join(repo, "outputs/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1/integration_workdata.json");
const inputDir = path.join(repo, "Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1");
const outputDir = path.join(repo, "Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1");
const previewDir = path.join(repo, "outputs/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1/previews/workbooks");
const data = JSON.parse(await fs.readFile(sourcePath, "utf8"));
await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const sourceFiles = [
  "01_LUOYANG_184_DEVELOPMENT_READINESS_MATRIX.xlsx",
  "02_LUOYANG_RUNTIME_ENTITY_MAPPING_AUDIT.xlsx",
  "03_LUOYANG_HISTORICAL_PERSON_RUNTIME_MAPPING.xlsx",
  "04_LUOYANG_CLAN_FAMILYORGANIZATION_MIGRATION_PLAN.xlsx",
  "05_LUOYANG_FAMILYCENTER_IMPLEMENTATION_READINESS.xlsx",
  "06_LUOYANG_FACILITY_HISTORICAL_REFERENCE_RUNTIME_CROSSWALK.xlsx",
  "07_LUOYANG_POPULATION_HOUSEHOLD_RESIDENCE_AUDIT.xlsx",
  "09_LUOYANG_190_FUTURE_COMPATIBILITY_AUDIT.xlsx",
  "10_LUOYANG_HULAO_WAVE0_DEPENDENCY_REVIEW.xlsx",
];
const sourceInspection = [];
for (const file of sourceFiles) {
  const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(path.join(inputDir, file)));
  const inspection = await workbook.inspect({ kind: "sheet", include: "id,name", maxChars: 10000 });
  sourceInspection.push({ file, inspection: inspection.ndjson });
}
await fs.writeFile(path.join(previewDir, "source_workbook_inspection.json"), JSON.stringify(sourceInspection, null, 2), "utf8");

const specs = [
  ["01_LUOYANG_HISTORICAL_PERSON_RUNTIME_INTEGRATION.xlsx", "洛阳184历史人物运行时接入", "25名历史人物绑定到同一永久Person，不新增、不复制。", [["人物映射", data.historical_runtime]]],
  ["02_LUOYANG_CLAN_BRANCH_RUNTIME_MAPPING.xlsx", "洛阳184 Clan / Branch 运行时映射", "证明Clan、Branch、Household、FamilyOrganization是彼此独立的关系层。", [["宗族支系", data.lineage]]],
  ["03_LUOYANG_FAMILYORGANIZATION_RUNTIME_MIGRATION.xlsx", "洛阳184 FamilyOrganization迁移", "保留15个组织稳定ID，纠正污染成员，保留未决设施主张而不偷换所有权。", [["组织迁移", data.organization_migration]]],
  ["04_LUOYANG_FAMILYCENTER_RUNTIME_STATE.xlsx", "洛阳184 FamilyCenter运行时状态", "FamilyCenter必须依赖真实Facility、能力、权属、指定与管理者活动；当前15个均Deferred。", [["家族中心", data.family_centers]]],
  ["05_LUOYANG_HISTORICAL_PERSON_HOUSEHOLD_RESIDENCE_MAPPING.xlsx", "洛阳184历史人物家户与住宅映射", "25名历史人物沿用400K人口包中的真实Household和Residence。", [["家户住宅", data.household_residence]]],
  ["06_LUOYANG_PERSON_FAMILY_ASSET_OWNERSHIP_AUDIT.xlsx", "洛阳184个人与家族资产权属审计", "个人资产保持个人账；组织资产独立；未决设施主张不转换为所有权。", [["资产审计", data.assets]]],
  ["07_LUOYANG_HISTORICAL_OFFICE_WORK_ACTIVITY_MAPPING.xlsx", "洛阳184历史官职、工作与活动映射", "历史官职接入通用Civil/Military Office，并绑定辖区、既有Facility和当前活动。", [["官职工作", data.offices]]],
  ["08_LUOYANG_RUNTIME_MIGRATION_LOG.xlsx", "洛阳184运行时迁移日志", "逐对象记录本阶段投影、迁移与兼容处理；不把400K Person重写为内联对象。", [["迁移日志", data.migration_log]]],
  ["09_LUOYANG_POST_INTEGRATION_CONSERVATION_AUDIT.xlsx", "洛阳184接入后守恒审计", "核对Person、Household、Facility、Residence、Work、Cell、Ownership、Kinship和家族系统。", [["守恒审计", data.conservation]]],
];

function excelColumn(index) {
  let value = index + 1, result = "";
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

function headersFor(rows) {
  const headers = [];
  for (const row of rows) for (const key of Object.keys(row)) if (!headers.includes(key)) headers.push(key);
  return headers.length ? headers : ["Status"];
}

function writeCover(sheet, title, purpose) {
  sheet.showGridLines = false;
  sheet.getRange("A1:H2").merge();
  sheet.getRange("A1").values = [[title]];
  sheet.getRange("A1:H2").format = { fill: "#173B4D", font: { bold: true, color: "#FFFFFF", size: 20 }, verticalAlignment: "center" };
  sheet.getRange("A4:B12").values = [
    ["字段", "值"], ["状态", data.summary.status], ["Schema", data.summary.schema_version],
    ["Person / Household / Facility", `${data.summary.person_count} / ${data.summary.household_count} / ${data.summary.facility_count}`],
    ["历史人物 / 家族组织", `${data.summary.historical_person_count} / ${data.summary.family_organization_count}`],
    ["新增Person / Facility", `${data.summary.added_person_count} / ${data.summary.added_facility_count}`],
    ["FamilyCenter", `active=${data.summary.active_family_center_count}; deferred=${data.summary.family_center_count}`],
    ["用途", purpose], ["证据生成器", "audit_luoyang_184_historical_person_family_integration_v1.py"],
  ];
  sheet.getRange("A4:B4").format = { fill: "#2F687A", font: { bold: true, color: "#FFFFFF" } };
  sheet.getRange("A5:A12").format = { fill: "#DDEBF2", font: { bold: true, color: "#173B4D" } };
  sheet.getRange("A4:B12").format.borders = { preset: "all", style: "thin", color: "#B7C9D1" };
  sheet.getRange("A4:B12").format.wrapText = true;
  sheet.getRange("A4:A12").format.columnWidth = 24;
  sheet.getRange("B4:B12").format.columnWidth = 86;
  sheet.getRange("A14:H16").merge();
  sheet.getRange("A14").values = [["边界：本工作簿是V69运行时接入审计视图。永久人物、家户、设施和Cell仍以受保护初始化包为权威；不得从表格反向生成、合并、删除或重新随机世界事实。"]];
  sheet.getRange("A14:H16").format = { fill: "#FFF2CC", font: { color: "#7F6000", italic: true }, wrapText: true, verticalAlignment: "center" };
}

function writeRows(sheet, rows, tableName) {
  sheet.showGridLines = false;
  const headers = headersFor(rows);
  const matrix = [headers, ...rows.map(row => headers.map(header => scalar(row[header])))];
  const endCol = excelColumn(headers.length - 1);
  sheet.getRangeByIndexes(0, 0, matrix.length, headers.length).values = matrix;
  sheet.getRange(`A1:${endCol}1`).format = { fill: "#173B4D", font: { bold: true, color: "#FFFFFF" }, wrapText: true, verticalAlignment: "center" };
  if (rows.length) {
    sheet.tables.add(`A1:${endCol}${matrix.length}`, true, tableName);
    sheet.getRange(`A2:${endCol}${matrix.length}`).format = { wrapText: true, verticalAlignment: "top", borders: { preset: "all", style: "thin", color: "#D9E2E6" } };
  }
  sheet.freezePanes.freezeRows(1);
  headers.forEach((header, index) => {
    const lower = header.toLowerCase();
    let width = 20;
    if (lower.includes("id") || lower.includes("reference")) width = 34;
    if (lower.includes("notes") || lower.includes("evidence") || lower.includes("before") || lower.includes("after") || lower.includes("action")) width = 44;
    if (lower.includes("count") || lower.includes("ordinal") || lower.includes("delta") || lower.includes("quantity")) width = 15;
    sheet.getRange(`${excelColumn(index)}:${excelColumn(index)}`).format.columnWidth = width;
  });
  sheet.getRange("1:1").format.rowHeight = 34;
  return `A1:${endCol}${Math.min(matrix.length, 60)}`;
}

const renderManifest = [];
for (let i = 0; i < specs.length; i++) {
  const [file, title, purpose, sheetSpecs] = specs[i];
  const workbook = Workbook.create();
  const cover = workbook.worksheets.add("说明");
  writeCover(cover, title, purpose);
  const ranges = new Map([["说明", "A1:H16"]]);
  for (let j = 0; j < sheetSpecs.length; j++) {
    const [name, rows] = sheetSpecs[j];
    const sheet = workbook.worksheets.add(name);
    ranges.set(name, writeRows(sheet, rows, `T${i + 1}_${j + 1}`));
  }
  const outputPath = path.join(outputDir, file);
  await (await SpreadsheetFile.exportXlsx(workbook)).save(outputPath);
  const sheets = await workbook.inspect({ kind: "sheet", include: "id,name", maxChars: 10000 });
  const formulas = await workbook.inspect({ kind: "formula", maxChars: 10000, options: { maxResults: 200 } });
  await fs.writeFile(path.join(previewDir, `${file}.inspect.txt`), `${sheets.ndjson}\n${formulas.ndjson}\n`, "utf8");
  for (const sheet of workbook.worksheets.items) {
    const preview = await workbook.render({ sheetName: sheet.name, range: ranges.get(sheet.name), autoCrop: "all", scale: 0.8, format: "png" });
    const previewPath = path.join(previewDir, `${file.replace(/\.xlsx$/i, "")}__${sheet.name}.png`);
    await fs.writeFile(previewPath, new Uint8Array(await preview.arrayBuffer()));
    renderManifest.push({ workbook: file, sheet: sheet.name, preview: previewPath.replaceAll("\\", "/") });
  }
}
await fs.writeFile(path.join(previewDir, "render_manifest.json"), JSON.stringify(renderManifest, null, 2), "utf8");
console.log(JSON.stringify({ status: "PASS", sourceWorkbooksInspected: sourceFiles.length, outputWorkbooks: specs.length, renderedSheets: renderManifest.length, outputDir, previewDir }, null, 2));
