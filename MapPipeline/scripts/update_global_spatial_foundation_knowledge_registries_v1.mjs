import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
const entry=process.env.MANDATE_ARTIFACT_TOOL_ENTRY; if(!entry) throw new Error("MANDATE_ARTIFACT_TOOL_ENTRY required");
const {FileBlob,SpreadsheetFile}=await import(pathToFileURL(entry).href);
const scriptDirectory=path.dirname(fileURLToPath(import.meta.url));
const repo=process.env.MANDATE_REPO_ROOT||path.resolve(scriptDirectory,"../..");
const dir=path.join(repo,"Docs/KNOWLEDGE_BASE/REGISTRY"), out=path.join(repo,"outputs/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1/registry_previews");
await fs.mkdir(out,{recursive:true});
const task="Docs/TASK_WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1.md";
const base="Docs/HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1";
const report=`${base}/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1_REPORT.md`, contract=`${base}/GLOBAL_SPATIAL_FOUNDATION_CONTRACT_V1.md`;
const decisionTexts=[
 "Global CRS is the only authoritative geographic coordinate system.",
 "Global Origin and Global Cell Grid are one unified spatial contract.",
 "All regions reuse the same Global Cell Grid.",
 "Region Local Coordinates are reversible presentation/editor coordinates, not new world coordinates.",
 "Global Chunk Grid is derived from Global Cell Grid and is never re-cut by Region.",
 "DEM sampling is globally aligned before regional high-detail production.",
 "Visual Local Anchors do not create Facility SubCells.",
 "Floating Origin changes Unity presentation coordinates only.",
 "Administrative boundaries do not define Cell geometry.",
 "Map production is national-space-first and region-detail-second.",
 "Canonical Global Chunk is 16x16 Cells; legacy 64x64 blocks are compression storage only.",
 "Stable Cell IDs outrank cosmetic origin, row or chunk renumbering."
];
const specs={
 "PROJECT_DOCUMENT_REGISTRY.xlsx":{key:"DocumentId",rows:[
  {DocumentId:"doc.global-spatial-foundation.task.v1",Path:task,Title:"World Global Origin Cell Grid And Spatial Continuity V1",DocumentType:"TaskRecord",Domain:"GlobalSpatialFoundation",Status:"CURRENT",CanonicalFor:"ImplementationScope",ReadPriority:"P0"},
  {DocumentId:"doc.global-spatial-foundation.contract.v1",Path:contract,Title:"Global Spatial Foundation Contract V1",DocumentType:"CanonicalContract",Domain:"GlobalSpatialFoundation",Status:"CURRENT",CanonicalFor:"CRS|Origin|Cell|Chunk|Region|DEM|FloatingOrigin",ReadPriority:"P0"},
  {DocumentId:"doc.global-spatial-foundation.report.v1",Path:report,Title:"Global Spatial Foundation V1 Acceptance Report",DocumentType:"AcceptanceReport",Domain:"GlobalSpatialFoundation",Status:"CURRENT",CanonicalFor:"GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN",ReadPriority:"P0"}
 ]},
 "PROJECT_CANONICAL_DOMAIN_MAP.xlsx":{key:"Domain",rows:[{Domain:"GlobalSpatialFoundation",DomainId:"domain.global-spatial-foundation.v1",DomainName:"One World One Global Grid",L0ProjectConstitution:"AGENTS.md",L1CanonicalSpec:contract,L2CurrentStatus:"Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md",L3PrimaryReference:report,CanonicalGap:"High-detail Henan Yin terrain and historically reconstructed local river/road geometry remain next-stage work.",MultipleL1Conflict:"NO",ReadingEntry:contract,ConflictPolicy:"Stable global Cell IDs and global coordinates always win; Region and visual coordinates are derived.",CurrentStatus:"GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN",Status:"CURRENT"}]},
 "DESIGN_DECISION_REGISTRY.xlsx":{key:"DecisionId",rows:decisionTexts.map((Decision,i)=>({DecisionId:`decision.global-spatial.${String(i+1).padStart(2,"0")}.v1`,Domain:"GlobalSpatialFoundation",Title:`Global spatial foundation decision ${i+1}`,Decision,Status:"ACCEPTED",EffectiveFrom:"2026-08-15",SourceDocument:contract,AffectedSystems:"Map|GIS|Cell|Chunk|Terrain|Road|River|Place|Persistence|Presentation",ReasonSummary:"Preserve one continuous national world and stable spatial identities."}))},
 "OPEN_DECISION_REGISTRY.xlsx":{key:"OpenDecisionId",rows:[
  {OpenDecisionId:"open.global-spatial.henan-terrain-detail-resolution.v1",Domain:"GlobalSpatialFoundation",Question:"Which licensed source and production resolution will drive the first high-detail Henan Yin terrain?",Status:"OPEN",WhyOpen:"V1 freezes alignment, not final high-detail terrain content.",NeededEvidence:"DEM source/license/accuracy/performance benchmark",SourceDocument:report,Blocks:"HENAN-YIN-REGION-TERRAIN-AND-LUOYANG-BUILDABLE-MAP-V1"},
  {OpenDecisionId:"open.global-spatial.region-production-boundaries.v1",Domain:"GlobalSpatialFoundation",Question:"Which future chunk-aligned production Regions follow Henan Yin?",Status:"OPEN",WhyOpen:"Region is an optimization/art unit, not an administrative layer.",NeededEvidence:"Terrain/streaming/art workload and gameplay corridor priorities",SourceDocument:contract,Blocks:"Does not block Henan Yin"}
 ]},
 "IMPLEMENTATION_GAP_REGISTER.xlsx":{key:"GapId",rows:[
  {GapId:"gap.global-spatial.henan-high-detail-terrain.v1",Domain:"GlobalSpatialFoundation",CanonicalRequirement:"Region terrain must sample the frozen global DEM lattice and bind existing Cells.",CurrentImplementation:"Global alignment, 16x16 chunks and Henan Yin slice are frozen; final terrain mesh is not produced.",GapDescription:"High-detail terrain, visual rivers/roads and buildable-surface binding remain next-stage work.",Severity:"HIGH",BlocksNextDevelopment:"YES",RecommendedTask:"HENAN-YIN-REGION-TERRAIN-AND-LUOYANG-BUILDABLE-MAP-V1",Status:"OPEN",Evidence:report},
  {GapId:"gap.global-spatial.vector-anchor-runtime-index.v1",Domain:"GlobalSpatialFoundation",CanonicalRequirement:"River and road visual segments retain canonical global feature identity.",CurrentImplementation:"Global GeoJSON/raster/route anchors are audited; runtime visual segment index is deferred.",GapDescription:"No final Region terrain spline/mesh segment cache yet.",Severity:"MEDIUM",BlocksNextDevelopment:"NO",RecommendedTask:"HENAN-YIN-REGION-TERRAIN-AND-LUOYANG-BUILDABLE-MAP-V1",Status:"OPEN",Evidence:report}
 ]},
 "RESEARCH_GAP_REGISTER.xlsx":{key:"GapId",rows:[
  {GapId:"research.global-spatial.han-river-course.v1",Domain:"GlobalSpatialFoundation",ResearchGap:"Han-period local river courses and changed channels remain less certain than modern physical reference.",Priority:"HIGH",CurrentEvidence:"Natural Earth modern generalized rivers + project historical references",RequiredSources:"Historical geography|archaeology|palaeohydrology",DoNotInfer:"Modern river geometry as exact Han-period fact",Status:"OPEN"},
  {GapId:"research.global-spatial.han-road-corridor.v1",Domain:"GlobalSpatialFoundation",ResearchGap:"Detailed Han road widths, alignments, ferries and seasonal passability require regional evidence.",Priority:"HIGH",CurrentEvidence:"R001-R018 modeled strategic corridors",RequiredSources:"Historical texts|archaeology|regional transport studies",DoNotInfer:"Modeled route Cell path as excavated road geometry",Status:"OPEN"}
 ]}
};
function find(wb,key){for(const sh of wb.worksheets.items){const vals=sh.getUsedRange(true)?.values??[];for(let r=0;r<Math.min(20,vals.length);r++){const h=vals[r].map(v=>String(v??""));if(h.includes(key))return{sh,vals,row:r,h};}}throw new Error("missing "+key)}
function col(n){let s="",v=n+1;while(v){const r=(v-1)%26;s=String.fromCharCode(65+r)+s;v=Math.floor((v-1)/26);}return s;}
const results=[];
for(const [file,spec] of Object.entries(specs)){
 const p=path.join(dir,file),wb=await SpreadsheetFile.importXlsx(await FileBlob.load(p));
 const f=find(wb,spec.key),ki=f.h.indexOf(spec.key),existing=new Set(f.vals.slice(f.row+1).map(r=>String(r[ki]??""))); let next=f.vals.length,added=0;
 for(const row of spec.rows){if(existing.has(String(row[spec.key])))continue;f.sh.getRangeByIndexes(next++,0,1,f.h.length).values=[f.h.map(h=>row[h]??"")];added++;}
 const preview=await wb.render({sheetName:f.sh.name,range:`A${Math.max(1,next-12)}:${col(Math.min(f.h.length,12)-1)}${next}`,scale:0.7,format:"png"});
 await fs.writeFile(path.join(out,file.replace(/\.xlsx$/i,".png")),new Uint8Array(await preview.arrayBuffer()));
 const errors=await wb.inspect({kind:"match",searchTerm:"#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",options:{useRegex:true,maxResults:100},summary:"formula errors"});
 await fs.writeFile(path.join(out,file.replace(/\.xlsx$/i,".inspect.ndjson")),errors.ndjson,"utf8");
 if(/#REF|#DIV|#VALUE|#NAME|#N\/A/.test(errors.ndjson))throw new Error(file+" formula error");
 await(await SpreadsheetFile.exportXlsx(wb)).save(p); results.push({file,added});
}
await fs.writeFile(path.join(out,"registry_update_summary.json"),JSON.stringify({status:"PASS",results},null,2)+"\n","utf8");
console.log(JSON.stringify({status:"PASS",results},null,2));
