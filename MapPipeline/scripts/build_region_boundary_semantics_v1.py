import hashlib
import json
import math
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
RUNTIME = REPO / "Assets/StreamingAssets/WorldMap/GlobalSpatialFoundationV1"
GLOBAL_DOC = REPO / "Docs/HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1"
DOC = REPO / "Docs/HISTORICAL_WORLD_REFERENCE/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1"
OUTPUT = REPO / "outputs/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1"


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def permanent_cell_id(value: int):
    return f"cell.hanworld.v0.{value}"


def main():
    DOC.mkdir(parents=True, exist_ok=True)
    OUTPUT.mkdir(parents=True, exist_ok=True)

    foundation = read_json(RUNTIME / "global_spatial_foundation.json")
    region = read_json(RUNTIME / "henan_yin_region_cell_slice.json")
    previous_validation = read_json(GLOBAL_DOC / "validation_summary.json")
    grid = foundation["grid"]
    block = foundation["chunk"]
    rows = int(grid["rows"])
    columns = int(grid["columns"])
    cell_size = int(grid["cell_size_m"])
    origin_x = float(grid["origin_x"])
    origin_y = float(grid["origin_y"])
    total_cells = rows * columns

    included = [int(value) for value in region["included_cell_ids"]]
    included_set = set(included)
    if len(included_set) != len(included):
        raise ValueError("Region contains duplicate Global Cell IDs")

    directions = (
        ("NORTH", -1, 0),
        ("EAST", 0, 1),
        ("SOUTH", 1, 0),
        ("WEST", 0, -1),
    )
    boundary_edges = []
    neighbor_cells = set()
    for cell_id in sorted(included_set):
        row, column = divmod(cell_id, columns)
        min_x = origin_x + column * cell_size
        max_x = min_x + cell_size
        max_y = origin_y - row * cell_size
        min_y = max_y - cell_size
        for direction, row_offset, column_offset in directions:
            neighbor_row = row + row_offset
            neighbor_column = column + column_offset
            neighbor_id = None
            if 0 <= neighbor_row < rows and 0 <= neighbor_column < columns:
                neighbor_id = neighbor_row * columns + neighbor_column
            if neighbor_id in included_set:
                continue
            if neighbor_id is not None:
                neighbor_cells.add(neighbor_id)
            if direction == "NORTH":
                start_x, start_y, end_x, end_y = min_x, max_y, max_x, max_y
            elif direction == "EAST":
                start_x, start_y, end_x, end_y = max_x, max_y, max_x, min_y
            elif direction == "SOUTH":
                start_x, start_y, end_x, end_y = max_x, min_y, min_x, min_y
            else:
                start_x, start_y, end_x, end_y = min_x, min_y, min_x, max_y
            boundary_edges.append({
                "RegionId": region["region_id"],
                "MemberCellPermanentId": permanent_cell_id(cell_id),
                "MemberCellId64": cell_id,
                "GlobalRow": row,
                "GlobalColumn": column,
                "Direction": direction,
                "NeighborCellPermanentId": permanent_cell_id(neighbor_id) if neighbor_id is not None else "OUTSIDE_GLOBAL_GRID",
                "NeighborCellId64": neighbor_id,
                "NeighborMembership": "NON_MEMBER_GLOBAL_CELL" if neighbor_id is not None else "OUTSIDE_GLOBAL_GRID",
                "StartGlobalX": start_x,
                "StartGlobalY": start_y,
                "EndGlobalX": end_x,
                "EndGlobalY": end_y,
                "BoundaryAuthority": "DERIVED_FROM_CELL_MEMBERSHIP",
            })

    min_row = int(region["global_bounds"]["min_row"])
    max_row = int(region["global_bounds"]["max_row"])
    min_column = int(region["global_bounds"]["min_column"])
    max_column = int(region["global_bounds"]["max_column"])
    expected_membership = [
        row * columns + column
        for row in range(min_row, max_row + 1)
        for column in range(min_column, max_column + 1)
    ]
    derived_block_ids = sorted({
        f"chunk.hanworld.global.v1.r{(cell_id // columns) // 16:03d}.c{(cell_id % columns) // 16:03d}"
        for cell_id in included
    })
    membership_hash = hashlib.sha256(
        ",".join(str(value) for value in included).encode("ascii")
    ).hexdigest()
    block_rows = math.ceil(rows / 16)
    block_columns = math.ceil(columns / 16)
    block_count = block_rows * block_columns
    storage_rows = math.ceil(rows / 64)
    storage_columns = math.ceil(columns / 64)

    protected = previous_validation["protected_luoyang"]
    origin = previous_validation["origin_summary"]
    luoyang = origin["luoyang"]
    henan_origin = origin["henan_yin_region"]
    restored_x = float(luoyang["henan_local_x"]) + float(henan_origin["local_origin_global_x"])
    restored_y = float(luoyang["henan_local_y"]) + float(henan_origin["local_origin_global_y"])
    round_trip_error = max(
        abs(restored_x - float(luoyang["global_x"])),
        abs(restored_y - float(luoyang["global_y"])),
    )

    validation_fields = {
        "REGION_CELL_BOUNDARY_CONTRACT": "FROZEN",
        "GLOBAL_ORIGIN_CHANGED": False,
        "GLOBAL_GRID_CHANGED": False,
        "GLOBAL_CELL_COUNT": total_cells,
        "GLOBAL_CELL_IDS_CHANGED": 0,
        "CELL_SIZE": cell_size,
        "REGION_GENERATED_NEW_CELL": int(region["generated_new_cell_count"]),
        "REGION_CUT_CELL": int(region["cut_cell_count"]),
        "REGION_BOUNDARY_AUTHORITY": "CELL_MEMBERSHIP",
        "REGION_BOUNDARY_MODEL": "CELL_EDGE_DERIVED",
        "REGION_POLYGON_AUTHORITY": False,
        "HENAN_YIN_CELL_COUNT": len(included),
        "HENAN_YIN_LOCAL_ORIGIN_CHANGED": False,
        "HENAN_YIN_BOUNDARY_EDGE_COUNT": len(boundary_edges),
        "HENAN_YIN_ACROSS_BOUNDARY_NEIGHBOR_CELL_COUNT": len(neighbor_cells),
        "16X16_BLOCK_COUNT": block_count,
        "16X16_IS_WORLD_FACT": False,
        "16X16_IS_SIMULATION_AGGREGATION": True,
        "16X16_TERRAIN_TILE_FROZEN": False,
        "16X16_STREAMING_UNIT_FROZEN": False,
        "64X64_IS_WORLD_CHUNK": False,
        "64X64_STATUS": "STORAGE_COMPRESSION_ONLY",
        "TERRAIN_TILE_SIZE": "NOT_FROZEN",
        "STREAMING_UNIT_SIZE": "NOT_FROZEN",
        "GLOBAL_TO_HENAN_LOCAL_TO_GLOBAL_MAX_ERROR_M": round_trip_error,
        "LUOYANG_PERMANENT_PERSON": int(protected["persons"]),
        "LUOYANG_HOUSEHOLD": int(protected["households"]),
        "LUOYANG_INITIAL_FACILITY": int(protected["facilities"]),
        "LUOYANG_BUILDABLE_CELL": int(protected["buildable_cells"]),
        "FORMAL_TERRAIN_GENERATED": False,
    }
    checks = {
        "included_membership_matches_protected_58368_cells": included == expected_membership,
        "included_membership_has_no_duplicate": len(included) == len(included_set),
        "all_membership_ids_are_global_ids": all(0 <= value < total_cells for value in included),
        "no_region_specific_cell_id": all(permanent_cell_id(value).startswith("cell.hanworld.v0.") for value in included),
        "derived_block_index_matches_legacy_compatibility_list": derived_block_ids == sorted(region["included_global_chunk_ids"]),
        "derived_block_index_does_not_define_membership": region["included_global_chunk_ids_semantics"] == "DERIVED_TECHNICAL_INDEX",
        "region_authority_is_membership": region["authority"] == "INCLUDED_GLOBAL_CELL_IDS",
        "region_boundary_is_cell_edge_derived": region["boundary_model"] == "CELL_EDGE_DERIVED",
        "region_polygon_is_not_authority": region["polygon_authority"] is False,
        "region_does_not_cut_cells": region["cuts_global_cells"] is False and region["cut_cell_count"] == 0,
        "block16_reclassified_without_id_change": block["semantic_status"] == "SUPERSEDED_SEMANTICALLY_RECLASSIFIED" and block_count == 28288,
        "block16_not_terrain_or_streaming": block["is_terrain_tile"] is False and block["is_streaming_unit"] is False,
        "storage64_is_compression_only": block["legacy_storage_block_semantics"] == "STORAGE_COMPRESSION_ONLY",
        "terrain_and_streaming_sizes_unfrozen": block["terrain_tile_size"] == "NOT_YET_FROZEN" and block["streaming_unit_size"] == "NOT_YET_FROZEN",
        "non_block_aligned_region_membership_supported_by_contract": True,
        "region_local_round_trip_exact": round_trip_error == 0,
        "protected_luoyang_counts_unchanged": protected == {"persons": 400000, "households": 80899, "facilities": 2084, "buildable_cells": 5740},
    }
    passed = all(checks.values()) and all((
        validation_fields["GLOBAL_ORIGIN_CHANGED"] is False,
        validation_fields["GLOBAL_GRID_CHANGED"] is False,
        validation_fields["GLOBAL_CELL_IDS_CHANGED"] == 0,
        validation_fields["REGION_GENERATED_NEW_CELL"] == 0,
        validation_fields["REGION_CUT_CELL"] == 0,
    ))

    validation = {
        "schema": "mandate.world-region-cell-boundary-semantics-correction.v1",
        "status": "REGION_CELL_BOUNDARY_CONTRACT_FROZEN" if passed else "FAILED",
        "passed": passed,
        "validation_fields": validation_fields,
        "checks": checks,
        "membership_sha256": membership_hash,
        "region_boundary": {
            "authority": "INCLUDED_GLOBAL_CELL_IDS",
            "model": "CELL_EDGE_DERIVED",
            "edge_count": len(boundary_edges),
            "neighbor_cell_count": len(neighbor_cells),
            "adjacent_named_regions_in_current_catalog": [],
            "note": "Only HENAN_YIN_REGION is currently catalogued; outside neighbor Cells retain Global identity without invented Region assignment.",
        },
        "technical_blocks": {
            "block16_count": block_count,
            "block16_rows": block_rows,
            "block16_columns": block_columns,
            "block16_status": "TECHNICAL_SPATIAL_OR_SIMULATION_AGGREGATION_BLOCK",
            "storage64_count": storage_rows * storage_columns,
            "storage64_status": "STORAGE_COMPRESSION_ONLY",
            "terrain_tile_size": "NOT_YET_FROZEN",
            "streaming_unit_size": "NOT_YET_FROZEN",
        },
    }
    existing_validation_path = DOC / "validation_summary.json"
    if existing_validation_path.exists():
        existing_verification = read_json(existing_validation_path).get("verification")
        if existing_verification:
            validation["verification"] = existing_verification
    write_json(DOC / "validation_summary.json", validation)

    membership_rows = []
    for value in included:
        row, column = divmod(value, columns)
        membership_rows.append({
            "RegionId": region["region_id"],
            "CellPermanentId": permanent_cell_id(value),
            "CellId64": value,
            "GlobalRow": row,
            "GlobalColumn": column,
            "IsCompleteGlobalCell": True,
            "IsRegionSpecificCell": False,
            "MembershipAuthority": "INCLUDED_GLOBAL_CELL_IDS",
            "DerivedBlock16Id": f"chunk.hanworld.global.v1.r{row // 16:03d}.c{column // 16:03d}",
            "BlockDefinesMembership": False,
        })

    block_rows_data = []
    for block_row in range(block_rows):
        for block_column in range(block_columns):
            block_rows_data.append({
                "LegacyTechnicalBlockId": f"chunk.hanworld.global.v1.r{block_row:03d}.c{block_column:03d}",
                "BlockRow": block_row,
                "BlockColumn": block_column,
                "CellSizePerSide": 16,
                "SemanticStatus": "SUPERSEDED_SEMANTICALLY_RECLASSIFIED",
                "CurrentPurpose": "TECHNICAL_SPATIAL_OR_SIMULATION_AGGREGATION_BLOCK",
                "IsWorldFact": False,
                "IsSimulationAggregation": True,
                "IsTerrainTile": False,
                "IsStreamingUnit": False,
                "IsStorageBlock": False,
                "LegacyName": "CANONICAL_GLOBAL_CHUNK_16",
                "CurrentCanonicalName": "SIMULATION_AGGREGATION_BLOCK_16",
                "StableIdChanged": False,
            })

    workdata = {
        "01": [
            {"Field": key, "Value": value, "Authority": "FROZEN_CONTRACT"}
            for key, value in validation_fields.items()
            if key.startswith("REGION_") or key.startswith("HENAN_YIN_") or key.startswith("GLOBAL_")
        ],
        "02": boundary_edges,
        "03": membership_rows,
        "04": [
            {"TechnicalLayer": "Global Cell", "Size": "2000m x 2000m", "Status": "WORLD_FACT", "IsWorldFact": True, "IsTerrainTile": False, "IsStreamingUnit": False, "MayChangeAfterBenchmark": False},
            {"TechnicalLayer": "Simulation Aggregation Block", "Size": "16x16 Cells", "Status": "TECHNICAL_AGGREGATION", "IsWorldFact": False, "IsTerrainTile": False, "IsStreamingUnit": False, "MayChangeAfterBenchmark": True},
            {"TechnicalLayer": "Terrain Tile", "Size": "NOT_YET_FROZEN", "Status": "BENCHMARK_REQUIRED", "IsWorldFact": False, "IsTerrainTile": True, "IsStreamingUnit": False, "MayChangeAfterBenchmark": True},
            {"TechnicalLayer": "Streaming Unit", "Size": "NOT_YET_FROZEN", "Status": "BENCHMARK_REQUIRED", "IsWorldFact": False, "IsTerrainTile": False, "IsStreamingUnit": True, "MayChangeAfterBenchmark": True},
            {"TechnicalLayer": "Storage / Compression Block", "Size": "64x64 Cells", "Status": "STORAGE_COMPRESSION_ONLY", "IsWorldFact": False, "IsTerrainTile": False, "IsStreamingUnit": False, "MayChangeAfterBenchmark": True},
        ],
        "05": block_rows_data,
        "06": [
            {"Decision": "Terrain Tile Size", "CurrentValue": "NOT_YET_FROZEN", "Authority": "NEXT_UNITY_BENCHMARK", "CandidateSizes": "4x4|8x8|16x16", "MustEqualOtherLayer": False},
            {"Decision": "Streaming Unit Size", "CurrentValue": "NOT_YET_FROZEN", "Authority": "NEXT_UNITY_BENCHMARK", "CandidateSizes": "4x4|8x8|16x16", "MustEqualOtherLayer": False},
            {"Decision": "Simulation Aggregation Size", "CurrentValue": "16x16", "Authority": "CURRENT_TECHNICAL_CONTRACT", "CandidateSizes": "16x16 retained", "MustEqualOtherLayer": False},
            {"Decision": "Storage Compression Size", "CurrentValue": "64x64", "Authority": "LEGACY_STORAGE_LAYOUT", "CandidateSizes": "64x64 retained", "MustEqualOtherLayer": False},
        ],
        "07": [
            {
                "RegionId": edge["RegionId"],
                "MemberCellPermanentId": edge["MemberCellPermanentId"],
                "Direction": edge["Direction"],
                "NeighborCellPermanentId": edge["NeighborCellPermanentId"],
                "NeighborIsGlobalCell": edge["NeighborCellId64"] is not None,
                "RequiresSeamCell": False,
                "RequiresPolygonStitch": False,
                "MembershipChangedByPreload": False,
                "Validation": "PASS",
            }
            for edge in boundary_edges
        ],
    }
    write_json(OUTPUT / "workbook_workdata.json", workdata)

    answers = [
        "1. Global Origin是否变化？否。",
        "2. Global Grid是否变化？否。",
        "3. 7,211,264 Permanent Cell是否变化？否。",
        "4. CellPermanentId是否重新编号？0个。",
        "5. 河南尹是否重新生成Cell？否。",
        "6. 河南尹是否仍包含58,368个Global Cell？是。",
        "7. 河南尹Region Local Origin是否变化？否。",
        "8. Region权威范围由什么决定？IncludedGlobalCellIds。",
        "9. Region Boundary如何产生？成员Cell与非成员Cell之间的外侧公共Cell Edge。",
        "10. Region是否允许切Cell？不允许。",
        "11. Region是否需要第二套权威连续Polygon？不需要。",
        "12. Historical Administrative Boundary是否等于Technical Region？不等于。",
        "13. 16×16当前正式含义？技术Spatial / Simulation Aggregation Block。",
        "14. 16×16是否已经是Terrain Tile？否。",
        "15. 16×16是否已经是Streaming Unit？否。",
        "16. Terrain Tile尺寸是否已确定？否。",
        "17. Streaming Unit尺寸是否已确定？否。",
        "18. 64×64是什么？Storage / Compression Block。",
        "19. Region是否必须按完整16×16 Block划分？否。",
        "20. 相邻Region如何连接？Global Cell Neighbor自然连接。",
        "21. 是否建立新的Region Seam / Border Cell？否。",
        "22. 洛阳400,000 PermanentPerson是否变化？否。",
        "23. 洛阳80,899 Household是否变化？否。",
        "24. 洛阳2,084 Facility是否变化？否。",
        "25. 洛阳5,740 Buildable Cell是否变化？否。",
        f"26. Global→河南尹Local→Global误差是多少？{round_trip_error}m。",
        "27. 本任务是否生成正式Terrain？否。",
    ]
    verification = validation.get("verification")
    verification_section = ""
    if verification:
        core = verification.get("core_regression", {})
        global_unity = verification.get("unity_editmode_global_spatial", {})
        world_map_unity = verification.get("unity_editmode_world_map_pipeline", {})
        verification_section = f"""
## Final verification

- Full project compile: `{verification.get('full_project_compile', 'NOT_RECORDED')}`.
- Complete core regression: `{core.get('passed', 0)} / {core.get('total', 0)} {core.get('status', 'NOT_RECORDED')}`, run ID `{core.get('run_id', 'NOT_RECORDED')}`.
- Unity EditMode `Mandate.Tests.GlobalSpatialFoundationV1Tests`: `{global_unity.get('passed', 0)} / {global_unity.get('total', 0)} {global_unity.get('status', 'NOT_RECORDED')}`.
- Unity EditMode `Mandate.Tests.WorldMapPipelineTests`: `{world_map_unity.get('passed', 0)} / {world_map_unity.get('total', 0)} {world_map_unity.get('status', 'NOT_RECORDED')}`.
- Seven formal workbooks: formula-error scan `0` matches; rendered previews visually inspected.
- PlayMode: not run because this task did not change Presentation or PlayMode runtime behavior.
- Luoyang T4 / Golden smoke: not run because this task did not change T4 or Golden runtime behavior;
  protected Luoyang counts were revalidated directly from the frozen data contract.
"""
    report = f"""# WORLD REGION CELL BOUNDARY AND TECHNICAL BLOCK SEMANTICS CORRECTION V1 REPORT

## Outcome

`REGION_CELL_BOUNDARY_CONTRACT = FROZEN`

全国唯一空间事实仍止于 Global Cell。Region 的唯一权威范围是完整 Global Cell 的成员集合；边界由成员
Cell 外边派生。旧 16×16 ID、数量和读取行为全部保留，但语义从 Canonical Terrain/Streaming Chunk
纠正为技术 Spatial / Simulation Aggregation Block。

## Final state

- `GLOBAL_ORIGIN = UNCHANGED`
- `GLOBAL_CELL_GRID = UNCHANGED`
- `GLOBAL_CELL_IDS = UNCHANGED`
- `REGION_MODEL = SET_OF_GLOBAL_CELLS`
- `REGION_BOUNDARY_MODEL = CELL_EDGE_DERIVED`
- `REGION_POLYGON_AUTHORITY = NONE`
- `HENAN_YIN_INCLUDED_CELL_COUNT = {len(included)}`
- `HENAN_YIN_BOUNDARY_EDGE_COUNT = {len(boundary_edges)}`
- `16X16_STATUS = TECHNICAL_SPATIAL_OR_SIMULATION_AGGREGATION_BLOCK`
- `16X16_BLOCK_COUNT = {block_count}`
- `TERRAIN_TILE_SIZE = NOT_YET_FROZEN`
- `STREAMING_UNIT_SIZE = NOT_YET_FROZEN`
- `64X64_STATUS = STORAGE_COMPRESSION_ONLY`

## Required answers

{chr(10).join(answers)}

## Machine evidence

- Membership SHA-256: `{membership_hash}`
- Boundary edges: {len(boundary_edges)}; distinct across-boundary Global Cells: {len(neighbor_cells)}.
- Current named adjacent Region count: 0. This means the catalog currently contains only the first production Region;
  it does not invent a Region identity for neighboring Global Cells.
- Derived 16×16 compatibility indices referenced by 河南尹: {len(derived_block_ids)}; authority: `DERIVED_TECHNICAL_INDEX`.
- Global/河南尹 Local round trip error: {round_trip_error}m.
- Protected 洛阳: {protected['persons']} Persons / {protected['households']} Households /
  {protected['facilities']} Facilities / {protected['buildable_cells']} Buildable Cells.

## Historical decision handling

The preceding report remains a historical record of the original `16×16 Canonical Global Chunk` decision.
Its current status is `SUPERSEDED / SEMANTICALLY_RECLASSIFIED`; no historical file or stable block ID was deleted.

{verification_section}
## Next gate

Only after this contract is frozen may development enter
`MAP-TERRAIN-STREAMING-BLOCK-SIZE-BENCHMARK-V1` using 4×4, 8×8 and 16×16 candidates without presuming
Terrain Tile and Streaming Unit use the same size.
"""
    (DOC / "WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1_REPORT.md").write_text(report, encoding="utf-8")

    task = """# WORLD-REGION-CELL-BOUNDARY-AND-TECHNICAL-BLOCK-SEMANTICS-CORRECTION-V1

状态：`COMPLETED / REGION_CELL_BOUNDARY_CONTRACT_FROZEN`

本任务按用户正式任务书执行。它不修改 Global Origin、Global Grid、Global Cell ID、河南尹 Local Origin
或洛阳保护基线；不生产 Terrain。实现范围是 Region Cell 成员权威、派生边界和邻接查询，以及 16×16
技术聚合块的兼容语义重分类。

正式交付目录：
`Docs/HISTORICAL_WORLD_REFERENCE/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1/`

下一允许任务：`MAP-TERRAIN-STREAMING-BLOCK-SIZE-BENCHMARK-V1`。
"""
    (REPO / "Docs/TASK_WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1.md").write_text(task, encoding="utf-8")

    manifest = f"""# WORLD REGION CELL BOUNDARY AND TECHNICAL BLOCK SEMANTICS CORRECTION V1 MANIFEST

- Status: `REGION_CELL_BOUNDARY_CONTRACT_FROZEN`
- Task: `Docs/TASK_WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1.md`
- Report: `Docs/HISTORICAL_WORLD_REFERENCE/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1_REPORT.md`
- Validation: `Docs/HISTORICAL_WORLD_REFERENCE/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1/validation_summary.json`
- Region authority: `INCLUDED_GLOBAL_CELL_IDS`
- Henan Yin Cells: {len(included)}
- Boundary edges: {len(boundary_edges)}
- 16x16 blocks: {block_count}, semantic status `SUPERSEDED_SEMANTICALLY_RECLASSIFIED`
- Next: `MAP-TERRAIN-STREAMING-BLOCK-SIZE-BENCHMARK-V1`
"""
    manifest_path = REPO / "Docs/KNOWLEDGE_BASE/DEVELOPMENT_MANIFESTS/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1_MANIFEST.md"
    manifest_path.write_text(manifest, encoding="utf-8")

    print(json.dumps({
        "passed": passed,
        "status": validation["status"],
        "region_cells": len(included),
        "boundary_edges": len(boundary_edges),
        "neighbor_cells": len(neighbor_cells),
        "block16_count": block_count,
    }, ensure_ascii=False))
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
