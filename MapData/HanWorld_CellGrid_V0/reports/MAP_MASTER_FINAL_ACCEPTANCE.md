# MAP MASTER FINAL ACCEPTANCE

## Historical geography

- PASS: Natural Earth land/coastline/major lakes/major rivers integrated with source and license metadata.
- PASS: open terrain DEM exists in the project Albers work CRS with 2,000m pixels and `-32768` NoData.
- PASS: C001-C077 are structured; 72 have approximate/provisional coordinates and five remain deliberately null.
- PASS: 1,182 unique Han-140 county identities are present; 68 unique county anchors are currently supported by
  located strategic-city mappings and the remainder are not given fabricated historical coordinates.
- PASS: 31 strategic sites include the required national passes, ports, crossings and the existing prototype sites.
- PASS: 18 route corridors include R001-R012 and six national corridors.
- PASS: every generated administrative polygon is explicitly `synthetic_proxy` and `historical_claim=false`.

## Cell experiment and selected grid

- PASS: 500m, 1,000m, 2,000m and 4,000m candidates use one CRS, origin, square orientation and source mother map.
- PASS: each candidate reports total/land/water/passable counts, county proxy density, all 77 city surroundings,
  six strategic distances, four pass widths, three major-river representations, chunks and memory estimates.
- PASS: four fixed regions × four scales produced 16 previews plus a contact sheet.
- PASS: the weighted comparison selects 2,000m as the coarsest candidate that retains the current gameplay needs.
- PASS: the nationwide grid contains 7,211,264 deterministic row-major Cells and implicit stable eight-neighbor data.
- PASS: terrain, elevation, water, administrative and continuous road paths are Cellized in chunked binary files.

## Unity and engineering evidence

- PASS: full C# solution compilation completed on 2026-08-08.
- PASS: related `MapPerspective` core regression filter passed. The unrelated full legacy core suite was attempted,
  exceeded the mandatory 300-second boundary while still progressing, and is not falsely reported as complete.
- PASS: `WorldMapPipelineTests` EditMode run: 6/6 passed; summary
  `tmp/unity-validation/unity-EditMode-20260808-193016-655.summary.json`.
- PASS: `MapValidationPlayModeTests` graphics run: 1/1 passed; summary
  `tmp/unity-validation/unity-PlayMode-20260808-193049-571.summary.json`.
- PASS: GIS/package validation: 51/51 checks passed in `pipeline_validation.json`, including 72 positioned
  city-to-Cell mappings and five deliberately unmapped unresolved cities.
- PASS: the complete `MapPipeline/Build-HanWorldMap.ps1` rebuild passed all four stages in 43.8 seconds;
  raw download caches are ignored.

## Explicit uncertainty and next-layer boundary

This V0 does not claim exact Han boundaries, reconstructed ancient river channels, exact historical roads, or
coordinates for unresolved county seats. It also does not implement personal knowledge fog, dynamic facilities,
runtime ownership, forces, resources, town-entry scenes or map art. Those systems consume this stable map base in
later tasks without changing its evidence labels or conflating attention with world facts.
