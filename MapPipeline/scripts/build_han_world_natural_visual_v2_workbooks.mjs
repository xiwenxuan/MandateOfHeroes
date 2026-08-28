import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const entry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY;
if (!entry) throw new Error("MANDATE_ARTIFACT_TOOL_ENTRY is required");
const { SpreadsheetFile, Workbook } = await import(pathToFileURL(entry).href);
const repo = process.env.MANDATE_REPO_ROOT;
if (!repo) throw new Error("MANDATE_REPO_ROOT is required");
const task = "HAN-WORLD-NATURAL-MAP-VISUAL-PRESENTATION-V2";
const out = path.join(repo, "Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2");
const qa = path.join(repo, "outputs/HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2/workbooks");
await fs.mkdir(out, { recursive: true });
await fs.mkdir(qa, { recursive: true });

const performance = JSON.parse((await fs.readFile(path.join(out, "natural_map_performance_v2.json"), "utf8")).replace(/^\uFEFF/, ""));
const screenshots = [
  ["01_WORLD_FULL_CLEAN.png","CAM_WORLD_FULL","完整全国自然地表","PASS"],
  ["02_WORLD_MOUNTAIN_PLAIN_READABILITY.png","CAM_WORLD_NORTH_CHINA","山地与平原肉眼可分","PASS"],
  ["03_WORLD_MAJOR_RIVER_CLEAN.png","CAM_WORLD_NORTH_CHINA","全国主要水系与地表共存","PASS"],
  ["04_HENAN_YIN_REGION_CLEAN.png","CAM_HENAN_YIN_REGION","河南尹连续区域地表","PASS"],
  ["05_HENAN_YIN_TERRAIN_RELIEF.png","CAM_HENAN_MOUNTAIN","河南尹山地起伏","PASS"],
  ["06_RIVER_CLOSE_PRESENTATION.png","CAM_HENAN_RIVER","平滑河道、河岸与宽度","PASS_WITH_ART_LIMITS"],
  ["07_FOREST_CLOSE_PRESENTATION.png","CAM_HENAN_FOREST","连续密度森林","PASS_WITH_ART_LIMITS"],
  ["08_SURFACE_BLEND_CLOSE.png","CAM_HENAN_MOUNTAIN","连续地表混合","PASS"],
  ["09_TILE_BOUNDARY_STRESS_TEST.png","CAM_TILE_SEAM_TEST","Tile接缝压力","PASS"],
  ["10_GRID_OFF_CLEAN.png","CAM_HENAN_YIN_REGION","Cell格网关闭","PASS"],
  ["11_BACKGROUND_OFF_WORLD.png","CAM_WORLD_FULL","不依赖背景图","PASS"],
  ["12_WORLD_TO_REGION_START.png","TRANSITION_0.00","全国到区域起点","PASS"],
  ["13_WORLD_TO_REGION_MID.png","TRANSITION_0.56","全国到区域中段","PASS"],
  ["14_WORLD_TO_REGION_FINAL.png","TRANSITION_1.00","全国到区域终点","PASS"]
].map(([file,camera,purpose,status])=>({file,camera,purpose,status,
  game_view:"YES",cell_grid_visible:"NO",tile_boundary_visible:"NO",blank_world_area:"NO",
  rectangular_loading_block:"NO",mountain_readable:file.startsWith("01_")||file.startsWith("02_")||file.startsWith("04_")||file.startsWith("05_")||file.startsWith("07_")||file.startsWith("08_")?"YES":"NOT_TARGET",
  valley_readable:file.startsWith("04_")||file.startsWith("05_")||file.startsWith("08_")?"YES":"NOT_TARGET",
  river_natural:file.startsWith("03_")||file.startsWith("04_")||file.startsWith("06_")?"YES_WITH_SIMPLIFIED_BANK":"NOT_TARGET",
  forest_natural:file.startsWith("04_")||file.startsWith("05_")||file.startsWith("07_")?"PARTIAL_PROCEDURAL_CANOPY":"NOT_TARGET",
  surface_cell_blocks:"NO",formal_game_world:"PLAYABLE_PROTOTYPE_WITH_ART_LIMITS",
  evidence_path:`Screenshots/${file}`}));

const specs = [
  ["01_NATURAL_MAP_V1_VISUAL_GAP_AUDIT.xlsx","V1视觉差距审计",[
    ["中央绿色矩形Terrain块","FAIL","已由全国连续地表替代","PASS"],
    ["Terrain外围空白","FAIL","全国3314×2176母格网下采样连续覆盖","PASS"],
    ["GRID OFF仍见方格","FAIL","格网独立Debug层且默认关闭","PASS"],
    ["河流粗蓝折线","FAIL","Chaikin平滑、宽度变化、水体与河岸合并网格","PASS_WITH_ART_LIMITS"],
    ["山地起伏弱","FAIL","同一DEM分级夸张、坡向光照与固定相机","PASS"],
    ["森林规则点阵","FAIL","全局密度场、确定性抖动、单批网格","PARTIAL"],
    ["单一绿色地表","FAIL","稳定Surface ID混合＋连续坐标噪声","PASS"]
  ].map(([gap,v1,remedy,v2])=>({gap,v1_status:v1,v2_remedy:remedy,v2_status:v2}))],
  ["02_WORLD_TERRAIN_LOD_PRESENTATION_CONTRACT.xlsx","全国地形LOD表现合同",[
    {level:"WORLD",sample_step_cells:8,cell_resolution_m:16000,draw_surface:"ONE_CONTINUOUS_MESH",resident_tiles:0,role:"全国战略远景"},
    {level:"REGION_CONTINUOUS",sample_step_cells:1,cell_resolution_m:2000,draw_surface:"ONE_CONTINUOUS_MESH",resident_tiles:9,role:"区域可玩地表"},
    {level:"FORMAL_TILE",sample_step_cells:1,cell_resolution_m:2000,draw_surface:"HIDDEN_DUPLICATE_SURFACE",resident_tiles:9,role:"8×8 Cell驻留、碰撞、流式技术单元"}
  ]],
  ["03_TERRAIN_RELIEF_PRESENTATION_RULES.xlsx","地形起伏表现规则",[
    {scope:"WORLD",dem_source:"HanWorldV1/elevation.bin",vertical_exaggeration:2.10,lighting:"Directional Lambert",fog:"2200-5200 units",status:"PASS"},
    {scope:"REGION",dem_source:"SAME_AUTHORITATIVE_DEM",vertical_exaggeration:1.48,lighting:"Directional Lambert",fog:"90-260 units",status:"PASS"},
    {scope:"CELL_IDENTITY",rule:"Presentation height never changes Cell identity or elevation fact",status:"FROZEN"}
  ]],
  ["04_NATURAL_SURFACE_BLEND_RULES.xlsx","自然地表混合规则",[
    {rule:"PRIMARY_SECONDARY_BLEND",input:"NaturalSurfaceClassification",coordinate:"Global projected metres",effect:"stable continuous palette blend",status:"PASS"},
    {rule:"BROAD_NOISE",input:"96km continuous value noise",coordinate:"Global projected metres",effect:"breaks large uniform colour",status:"PASS"},
    {rule:"FINE_NOISE",input:"26km continuous value noise",coordinate:"Global projected metres",effect:"subtle local variation",status:"PASS"},
    {rule:"MISSING_CONTENT",input:"stable namespace surface ID",coordinate:"N/A",effect:"report; never silently remap",status:"CONTRACT"}
  ]],
  ["05_RIVER_PRESENTATION_V2_RULES.xlsx","河流V2表现规则",[
    {feature:"centerline",rule:"Chaikin smoothing × 2",source:"licensed projected polyline",status:"PASS"},
    {feature:"width",rule:"display tier + longitudinal modulation + stable phase",source:"river definition",status:"PASS"},
    {feature:"bank",rule:"outer bank + water-left/right + outer bank",source:"derived presentation",status:"PASS_WITH_ART_LIMITS"},
    {feature:"terrain_conform",rule:"sample authoritative DEM presentation height",source:"same world",status:"PASS"},
    {feature:"Luoshui",rule:"no reliable licensed unique source found in V1 input",source:"none",status:"NOT_PROVEN_SOURCE_GAP"}
  ]],
  ["06_FOREST_PRESENTATION_V2_RULES.xlsx","森林V2表现规则",[
    {feature:"density",rule:"bilinear Global Forest Density + continuous noise",status:"PASS"},
    {feature:"placement",rule:"global lattice candidates + deterministic jitter",status:"PASS"},
    {feature:"batching",rule:"one combined mesh per region; no GameObject per tree",status:"PASS"},
    {feature:"canopy",rule:"seven-sided procedural cone",status:"PARTIAL_ART_LIMIT"},
    {feature:"world_lod",rule:"forest carried by surface colour; no full tree residency",status:"PASS"}
  ]],
  ["07_TILE_VISUAL_CONTINUITY_AUDIT.xlsx","Tile视觉连续性审计",[
    {check:"Global Tile definition",expected:"8×8 Cells / 16km",actual:"8×8 Cells / 16km",status:"PASS"},
    {check:"Region visible surface",expected:"No overlapping duplicate terrain",actual:"one continuous 2km Cell mesh",status:"PASS"},
    {check:"Formal Tile residency",expected:"3×3 around focus",actual:"9 hidden surface meshes with colliders",status:"PASS"},
    {check:"Rectangular colour block",expected:"not visible",actual:"not visible in screenshot 09",status:"PASS"},
    {check:"Crack/background leak",expected:"none",actual:"none in screenshot 09",status:"PASS"}
  ]],
  ["08_WORLD_REGION_CAMERA_AND_LOD_CONTRACT.xlsx","世界—区域相机与LOD合同",[
    {camera:"CAM_WORLD_FULL",row:1088,column:1657,size:1160,pitch:68,yaw:0,mode:"WORLD"},
    {camera:"CAM_WORLD_NORTH_CHINA",row:1110,column:2090,size:520,pitch:66,yaw:-5,mode:"WORLD"},
    {camera:"CAM_HENAN_YIN_REGION",row:1247,column:1992,size:34,pitch:58,yaw:-12,mode:"REGION"},
    {camera:"CAM_HENAN_MOUNTAIN",row:1390,column:1710,size:26,pitch:56,yaw:-18,mode:"REGION"},
    {camera:"CAM_HENAN_RIVER",row:1209,column:2148,size:22,pitch:58,yaw:-10,mode:"REGION"},
    {camera:"CAM_HENAN_FOREST",row:1460,column:1970,size:22,pitch:55,yaw:12,mode:"REGION"},
    {camera:"CAM_TILE_SEAM_TEST",row:1241,column:2043,size:17,pitch:55,yaw:-16,mode:"REGION"}
  ]],
  ["09_GRID_DEBUG_VISIBILITY_AUDIT.xlsx","格网Debug可见性审计",[
    {mode:"WORLD",default_grid:"OFF",clean_screenshot:"01_WORLD_FULL_CLEAN.png",status:"PASS"},
    {mode:"REGION",default_grid:"OFF",clean_screenshot:"10_GRID_OFF_CLEAN.png",status:"PASS"},
    {mode:"DEBUG",activation:"explicit SetCellOverlayVisible(true)",world_fact_effect:"NONE",status:"PASS"},
    {mode:"BACKGROUND",legacy_image_required:"NO",clean_screenshot:"11_BACKGROUND_OFF_WORLD.png",status:"PASS"}
  ]],
  ["10_WORLD_NATURAL_MAP_VISUAL_ACCEPTANCE.xlsx","全国自然地图视觉验收",screenshots],
  ["11_NATURAL_MAP_PERFORMANCE_AUDIT.xlsx","自然地图性能审计",Object.entries(performance).map(([metric,value])=>({metric,value,status:metric.includes("frame_time")?"NOT_PROVEN_BATCHMODE_TIMING":metric.includes("generation")&&value>2000?"PARTIAL":"RECORDED",note:"Unity 2022.3 PlayMode controlled evidence"}))],
  ["12_GOLDEN_SCREENSHOT_REGISTRY.xlsx","Golden截图登记",screenshots.map(s=>({file:s.file,camera:s.camera,evidence_path:s.evidence_path,capture:"Unity Game View / PlayMode",review_status:"CANDIDATE_PENDING_USER_APPROVAL",golden_status:"NOT_GOLDEN_UNTIL_USER_APPROVES",visual_status:s.status}))]
];

function normalize(value){ if(value===null||value===undefined)return ""; if(typeof value==="object")return JSON.stringify(value); return value; }
function columns(rows){ const result=[]; for(const row of rows)for(const key of Object.keys(row))if(!result.includes(key))result.push(key); return result; }
function colName(i){let n=i+1,s="";while(n){const r=(n-1)%26;s=String.fromCharCode(65+r)+s;n=Math.floor((n-1)/26);}return s;}

const report=[];
for(let i=0;i<specs.length;i++){
  const [file,title,rows]=specs[i];
  const wb=Workbook.create();
  const readme=wb.worksheets.add("README"); readme.showGridLines=false;
  readme.getRange("A1:H1").merge(); readme.getRange("A1").values=[[title]];
  readme.getRange("A1:H1").format={fill:"#24483E",font:{bold:true,color:"#FFFFFF",size:17},rowHeight:42};
  readme.getRange("A3:B10").values=[["Task",task],["Final state","PLAYABLE_WITH_ART_LIMITS"],["Global grid","3314×2176 / 7,211,264 Cells"],["Cell size","2000m"],["Terrain Tile","8×8 Cells / 16km"],["Evidence","Unity Game View / PlayMode"],["Rows",rows.length],["Generated","2026-08-16"]];
  readme.getRange("A3:A10").format={fill:"#DCE9E1",font:{bold:true,color:"#25372F"}};
  readme.getRange("B3:B10").format={fill:"#F7F2E7",wrapText:true}; readme.getRange("A:A").format.columnWidth=25; readme.getRange("B:B").format.columnWidth=76;
  const data=wb.worksheets.add("DATA"); data.showGridLines=false;
  const hs=columns(rows); const end=colName(hs.length-1); const matrix=[hs,...rows.map(r=>hs.map(h=>normalize(r[h])))];
  data.getRange(`A1:${end}${matrix.length}`).values=matrix; data.freezePanes.freezeRows(1); data.freezePanes.freezeColumns(Math.min(2,hs.length));
  data.getRange(`A1:${end}1`).format={fill:"#294F45",font:{bold:true,color:"#FFFFFF",size:10},wrapText:true,rowHeight:36};
  data.getRange(`A2:${end}${matrix.length}`).format={font:{color:"#25312C",size:9},verticalAlignment:"top",wrapText:true};
  for(let c=0;c<hs.length;c++)data.getRange(`${colName(c)}:${colName(c)}`).format.columnWidth=/rule|purpose|path|note|actual|expected|effect|role/i.test(hs[c])?38:22;
  data.tables.add(`A1:${end}${matrix.length}`,true,`TV2${String(i+1).padStart(2,"0")}`);
  const blob=await SpreadsheetFile.exportXlsx(wb); await blob.save(path.join(out,file));
  const preview=await wb.render({sheetName:"DATA",range:`A1:${end}${Math.min(matrix.length,18)}`,scale:.7,format:"png"});
  await fs.writeFile(path.join(qa,`${String(i+1).padStart(2,"0")}.png`),new Uint8Array(await preview.arrayBuffer()));
  const inspect=await wb.inspect({kind:"workbook,sheet,table",maxChars:6000,tableMaxRows:5,tableMaxCols:14});
  const errors=await wb.inspect({kind:"match",searchTerm:"#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",options:{useRegex:true,maxResults:50},summary:"formula errors"});
  await fs.writeFile(path.join(qa,`${String(i+1).padStart(2,"0")}.inspect.ndjson`),inspect.ndjson,"utf8");
  await fs.writeFile(path.join(qa,`${String(i+1).padStart(2,"0")}.errors.ndjson`),errors.ndjson,"utf8");
  if(/#REF!|#DIV\/0!|#VALUE!|#NAME\?|#N\/A/.test(errors.ndjson))throw new Error(`${file}: formula error`);
  report.push({file,rows:rows.length,columns:hs.length,preview:`${String(i+1).padStart(2,"0")}.png`});
}
await fs.writeFile(path.join(qa,"workbook_build_report.json"),JSON.stringify({status:"PASS",count:report.length,workbooks:report},null,2)+"\n","utf8");
console.log(JSON.stringify({status:"PASS",count:report.length,totalRows:report.reduce((sum,x)=>sum+x.rows,0)},null,2));
