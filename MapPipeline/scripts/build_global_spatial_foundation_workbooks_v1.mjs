import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const entry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY;
if (!entry) throw new Error("MANDATE_ARTIFACT_TOOL_ENTRY is required");
const { SpreadsheetFile, Workbook } = await import(pathToFileURL(entry).href);
const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repo = process.env.MANDATE_REPO_ROOT || path.resolve(scriptDirectory, "../..");
const output = path.join(repo, "outputs/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1");
const destination = path.join(repo, "Docs/HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1");
const previews = path.join(output, "previews");
const inspections = path.join(output, "inspections");
const outputWorkbooks = path.join(output, "workbooks");
await fs.mkdir(destination, { recursive: true });
await fs.mkdir(previews, { recursive: true });
await fs.mkdir(inspections, { recursive: true });
await fs.mkdir(outputWorkbooks, { recursive: true });
const data = JSON.parse(await fs.readFile(path.join(output, "workbook_workdata.json"), "utf8"));

const specs = [
  ["01","01_GLOBAL_SPATIAL_COORDINATE_MASTER.xlsx","Global Spatial Coordinate Master"],
  ["02","02_GLOBAL_ORIGIN_AND_GRID_DEFINITION.xlsx","Global Origin And Grid Definition"],
  ["03","03_GLOBAL_CELL_GRID_AUDIT.xlsx","Global Cell Grid Audit"],
  ["04","04_GLOBAL_CELL_ID_AND_INDEX_MASTER.xlsx","Global Cell ID And Index Master"],
  ["05","05_GLOBAL_CHUNK_GRID_MASTER.xlsx","Global Chunk Grid Master"],
  ["06","06_GLOBAL_DEM_SAMPLING_CONTRACT.xlsx","Global DEM Sampling Contract"],
  ["07","07_GLOBAL_RIVER_ROAD_SPATIAL_ANCHOR_AUDIT.xlsx","Global River Road Spatial Anchor Audit"],
  ["08","08_GLOBAL_PLACE_TO_CELL_CROSSWALK.xlsx","Global Place To Cell Crosswalk"],
  ["09","09_REGION_LOCAL_COORDINATE_CONTRACT.xlsx","Region Local Coordinate Contract"],
  ["10","10_HENAN_YIN_REGION_SPATIAL_MASTER.xlsx","Henan Yin Region Spatial Master"],
  ["11","11_GIS_UNITY_COORDINATE_TRANSFORM_AUDIT.xlsx","GIS Unity Coordinate Transform Audit"],
  ["12","12_FLOATING_ORIGIN_CONTRACT.xlsx","Floating Origin Contract"],
  ["13","13_LUOYANG_GLOBAL_CELL_BINDING_AUDIT.xlsx","Luoyang Global Cell Binding Audit"],
  ["14","14_GLOBAL_SPATIAL_CONTINUITY_VALIDATION.xlsx","Global Spatial Continuity Validation"],
  ["15","15_GLOBAL_AND_REGION_ORIGIN_SUMMARY.xlsx","Global And Region Origin Summary"],
];

function colName(index) { let n=index+1,s=""; while(n){const r=(n-1)%26;s=String.fromCharCode(65+r)+s;n=Math.floor((n-1)/26);} return s; }
function headers(rows) { const out=[]; for(const row of rows) for(const key of Object.keys(row)) if(!out.includes(key)) out.push(key); return out; }
function normal(value) { if(value===null||value===undefined) return null; if(typeof value==="object") return JSON.stringify(value); return value; }
function safe(value) { return value.replace(/[^A-Za-z0-9_-]/g,"_").slice(0,70); }
function findValue(rows, section, field) {
  const row=rows.find(value=>value.Section===section&&value.Field===field);
  if(!row) throw new Error(`Missing ${section}/${field}`);
  return row.Value;
}
function writeData(sheet, rows, tableName) {
  const hs=headers(rows), end=colName(hs.length-1), matrix=[hs,...rows.map(r=>hs.map(h=>normal(r[h])))];
  sheet.showGridLines=false; sheet.getRange(`A1:${end}${matrix.length}`).values=matrix;
  sheet.freezePanes.freezeRows(1); sheet.freezePanes.freezeColumns(Math.min(3,hs.length));
  sheet.getRange(`A1:${end}1`).format={fill:"#304F43",font:{bold:true,color:"#FFFFFF",size:10},wrapText:true,rowHeight:34,verticalAlignment:"center"};
  sheet.getRange(`A2:${end}${matrix.length}`).format={font:{color:"#232B27",size:9},wrapText:true,verticalAlignment:"top"};
  for(let r=2;r<=Math.min(matrix.length,30000);r++) if(r%2===0) sheet.getRange(`A${r}:${end}${r}`).format.fill="#F6F1E6";
  sheet.tables.add(`A1:${end}${matrix.length}`,true,tableName);
  for(let c=0;c<hs.length;c++) sheet.getRange(`${colName(c)}:${colName(c)}`).format.columnWidth = /Note|Formula|Source|Value|Rule|Derivation|Status|Id/.test(hs[c]) ? 32 : 18;
  return {rows:rows.length,columns:hs.length,end};
}

const report=[];
for(const [key,file,title] of specs){
  const rows=data[key]; if(!rows?.length) throw new Error(`${key} has no rows`);
  const wb=Workbook.create();
  const intro=wb.worksheets.add("说明"); intro.showGridLines=false;
  intro.getRange("A1:H1").merge(); intro.getRange("A1").values=[[title]];
  intro.getRange("A1:H1").format={fill:"#294539",font:{bold:true,color:"#FFFFFF",size:17},rowHeight:38,verticalAlignment:"center"};
  const notes=[["Status","GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN"],["Authority","Canonical Spatial Foundation V1"],["Reuse conclusion","B_REUSABLE_WITH_NON_ID_MIGRATION"],["Stable ID","No CellPermanentId was renumbered"],["Boundary","16x16 is a technical aggregation block; Terrain/Streaming sizes remain unfrozen; 64x64 is storage only"],["Source","HanWorldV1 + Luoyang protected packages + machine audit"]];
  intro.getRange("A3:B8").values=notes; intro.getRange("A3:A8").format={fill:"#DCE7DE",font:{bold:true,color:"#26372E"}}; intro.getRange("B3:B8").format={fill:"#F7F2E7",wrapText:true}; intro.getRange("A:A").format.columnWidth=24; intro.getRange("B:B").format.columnWidth=65;
  const sheet=wb.worksheets.add("数据"); const meta=writeData(sheet,rows,`TGlobalSpatial${key}`);
  const extraPreviewSheets=[];
  if(key==="15"){
    sheet.getRange("A:A").format.columnWidth=30;
    sheet.getRange("B:B").format.columnWidth=48;
    sheet.getRange("C:C").format.columnWidth=72;
    sheet.getRange("D:D").format.columnWidth=26;
    const parameter=wb.worksheets.add("参数"); parameter.showGridLines=false;
    parameter.getRange("A1:B8").values=[
      ["Parameter","Value"],
      ["GlobalOriginX",findValue(rows,"GLOBAL","GLOBAL_ORIGIN_X")],
      ["GlobalOriginY",findValue(rows,"GLOBAL","GLOBAL_ORIGIN_Y")],
      ["CellSizeM",findValue(rows,"GLOBAL","CELL_SIZE_M")],
      ["HenanOriginX",findValue(rows,"HENAN_YIN_REGION","LOCAL_ORIGIN_GLOBAL_X")],
      ["HenanOriginY",findValue(rows,"HENAN_YIN_REGION","LOCAL_ORIGIN_GLOBAL_Y")],
      ["GridColumns",findValue(rows,"GLOBAL","GLOBAL_GRID_COLUMNS")],
      ["GridRows",findValue(rows,"GLOBAL","GLOBAL_GRID_ROWS")],
    ];
    parameter.getRange("A1:B1").format={fill:"#304F43",font:{bold:true,color:"#FFFFFF"}};
    parameter.getRange("A2:A8").format={fill:"#DCE7DE",font:{bold:true,color:"#26372E"}};
    parameter.getRange("B2:B8").format.numberFormat="0.000000000000";
    parameter.getRange("A:A").format.columnWidth=28; parameter.getRange("B:B").format.columnWidth=24;
    const audit=wb.worksheets.add("公式核验"); audit.showGridLines=false;
    const sampleSections=["LUOYANG_URBAN_CANONICAL_ANCHOR_CELL","LUOYANG_OUTER_SUBURB_CELL","HENAN_YIN_FAR_OVERLAY_CELL"];
    const auditValues=[["Sample","Row","Column","StoredCenterX","StoredCenterY","FormulaCenterX","FormulaCenterY","CenterXError","CenterYError","HenanLocalX","HenanLocalY","RoundTripXError","RoundTripYError"]];
    for(const section of sampleSections){
      auditValues.push([section,findValue(rows,section,"ROW"),findValue(rows,section,"COLUMN"),findValue(rows,section,"CENTER_X"),findValue(rows,section,"CENTER_Y"),null,null,null,null,null,null,null,null]);
    }
    audit.getRange("A1:M4").values=auditValues;
    for(let row=2;row<=4;row++){
      audit.getRange(`F${row}:M${row}`).formulas=[[
        `='参数'!$B$2+(C${row}+0.5)*'参数'!$B$4`,
        `='参数'!$B$3-(B${row}+0.5)*'参数'!$B$4`,
        `=F${row}-D${row}`,`=G${row}-E${row}`,
        `=D${row}-'参数'!$B$5`,`=E${row}-'参数'!$B$6`,
        `=J${row}+'参数'!$B$5-D${row}`,`=K${row}+'参数'!$B$6-E${row}`,
      ]];
    }
    audit.getRange("A1:M1").format={fill:"#304F43",font:{bold:true,color:"#FFFFFF",size:9},wrapText:true,rowHeight:38};
    audit.getRange("A2:M4").format={font:{color:"#232B27",size:9},numberFormat:"0.000000000000"};
    audit.getRange("A:A").format.columnWidth=42;
    audit.getRange("B2:C4").format.numberFormat="0";
    for(const column of ["B","C"]) audit.getRange(`${column}:${column}`).format.columnWidth=16;
    for(const column of ["D","E","F","G","H","I","J","K","L","M"]) audit.getRange(`${column}:${column}`).format.columnWidth=21;
    audit.freezePanes.freezeRows(1); audit.tables.add("A1:M4",true,"TGlobalSpatial15FormulaAudit");
    extraPreviewSheets.push(["说明","15_intro.png","A1:H8"],["参数","15_parameters.png","A1:B8"],["公式核验","15_formula_audit.png","A1:M4"]);
  }
  const blob=await SpreadsheetFile.exportXlsx(wb); await blob.save(path.join(destination,file));
  await fs.copyFile(path.join(destination,file),path.join(outputWorkbooks,file));
  const preview=await wb.render({sheetName:"数据",range:`A1:${meta.end}${Math.min(meta.rows+1,30)}`,scale:0.8,format:"png"});
  await fs.writeFile(path.join(previews,`${key}_${safe(title)}.png`),new Uint8Array(await preview.arrayBuffer()));
  for(const [sheetName,previewName,range] of extraPreviewSheets){
    const extra=await wb.render({sheetName,range,scale:0.9,format:"png"});
    await fs.writeFile(path.join(previews,previewName),new Uint8Array(await extra.arrayBuffer()));
  }
  const inspect=await wb.inspect({kind:"workbook,sheet,table",maxChars:6000,tableMaxRows:5,tableMaxCols:12});
  const errors=await wb.inspect({kind:"match",searchTerm:"#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",options:{useRegex:true,maxResults:100},summary:"formula errors"});
  await fs.writeFile(path.join(inspections,`${key}.inspect.ndjson`),inspect.ndjson,"utf8");
  await fs.writeFile(path.join(inspections,`${key}.errors.ndjson`),errors.ndjson,"utf8");
  if(/#REF!|#DIV\/0!|#VALUE!|#NAME\?|#N\/A/.test(errors.ndjson)) throw new Error(`${file} formula error`);
  report.push({file,rows:meta.rows,columns:meta.columns,preview:`outputs/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1/previews/${key}_${safe(title)}.png`});
}
await fs.writeFile(path.join(output,"workbook_build_report.json"),JSON.stringify({status:"PASS",workbooks:report},null,2)+"\n","utf8");
console.log(JSON.stringify({status:"PASS",count:report.length,rows:report.reduce((a,b)=>a+b.rows,0)},null,2));
