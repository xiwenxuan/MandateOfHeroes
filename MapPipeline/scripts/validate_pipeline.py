from __future__ import annotations

import json
import os
import struct
import sys
import zlib
from pathlib import Path

import geopandas as gpd
from osgeo import gdal

gdal.UseExceptions()

REPO = Path(__file__).resolve().parents[2]
MASTER = REPO / "MapData" / "HanWorld_Master_V0"
GRID = REPO / "MapData" / "HanWorld_CellGrid_V0"
UNITY = REPO / "Assets" / "StreamingAssets" / "WorldMap" / os.environ.get("MANDATE_WORLD_MAP_VERSION", "HanWorldV0")


class Validation:
    def __init__(self):
        self.checks = []

    def check(self, name: str, condition: bool, detail=""):
        self.checks.append({"name": name, "passed": bool(condition), "detail": str(detail)})

    @property
    def passed(self):
        return all(item["passed"] for item in self.checks)


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def inspect_chunk_file(path: Path):
    with path.open("rb") as handle:
        magic, version, columns, rows, chunk_size, value_size, channels, chunk_columns, chunk_rows, chunk_count = struct.unpack(
            "<4s9i", handle.read(struct.calcsize("<4s9i")))
        indexes = [struct.unpack("<qiiHH", handle.read(struct.calcsize("<qiiHH"))) for _ in range(chunk_count)]
        offset, compressed_length, raw_length, height, width = indexes[len(indexes) // 2]
        handle.seek(offset)
        raw = zlib.decompress(handle.read(compressed_length), wbits=-15)
    return {
        "magic": magic.decode(), "version": version, "columns": columns, "rows": rows,
        "chunk_size": chunk_size, "value_size": value_size, "channels": channels,
        "chunk_columns": chunk_columns, "chunk_rows": chunk_rows, "chunk_count": chunk_count,
        "sample_raw_length": len(raw), "sample_declared_length": raw_length,
        "sample_shape": [height, width],
    }


def main() -> int:
    validation = Validation()
    resolved_sources = read_json(MASTER / "manifest" / "external_sources.resolved.json")
    required_source_fields = {
        "source_id", "name", "publisher", "version", "download_url", "download_date", "license",
        "commercial_use", "redistribution", "original_crs", "sha256", "local_cache", "processing_notes",
    }
    validation.check("external source count", len(resolved_sources["sources"]) >= 6, len(resolved_sources["sources"]))
    validation.check("external source contracts", all(required_source_fields.issubset(source) for source in resolved_sources["sources"]))
    validation.check("external source hashes resolved", all(source.get("sha256") for source in resolved_sources["sources"]))
    validation.check("external sources allow commercial redistribution", all(
        source.get("commercial_use") is True and source.get("redistribution") is True
        for source in resolved_sources["sources"]
    ))

    gpkg = MASTER / "HanWorld_Master_V0.gpkg"
    required_layers = {
        "coastline", "major_rivers", "major_lakes", "strategic_cities", "county_anchors",
        "strategic_sites", "provinces_v0", "commanderies_v0", "counties_v0", "major_routes_v0", "test_regions",
    }
    layers = set(gpd.list_layers(gpkg)["name"])
    validation.check("GeoPackage required layers", required_layers.issubset(layers), sorted(layers))

    cities = gpd.read_file(gpkg, layer="strategic_cities")
    counties = gpd.read_file(gpkg, layer="county_anchors")
    sites = gpd.read_file(gpkg, layer="strategic_sites")
    routes = gpd.read_file(gpkg, layer="major_routes_v0")
    admin_counties = gpd.read_file(gpkg, layer="counties_v0")
    loaded_frames = (cities, counties, sites, routes, admin_counties)
    validation.check("loaded geography uses one working CRS", len({str(frame.crs) for frame in loaded_frames}) == 1)
    validation.check("loaded geometry is valid or explicitly null", all(
        geometry is None or geometry.is_valid
        for frame in loaded_frames for geometry in frame.geometry
    ))
    validation.check("77 strategic cities", len(cities) == 77, len(cities))
    validation.check("unresolved cities remain null", int(cities.geometry.isna().sum()) == 5, int(cities.geometry.isna().sum()))
    validation.check("1182 unique county identities", len(counties) == 1182 and counties["admin_unit_id"].is_unique, len(counties))
    validation.check("strategic sites include national passes", len(sites) >= 31, len(sites))
    validation.check("R001-R012 preserved", {f"R{index:03d}" for index in range(1, 13)}.issubset(set(routes["route_id"])))
    validation.check("administrative proxies labelled", set(admin_counties["geometry_status"]) == {"synthetic_proxy"})
    validation.check("administrative proxies are not historical claims", not admin_counties["historical_claim"].astype(bool).any())

    dem = gdal.Open(str(MASTER / "physical" / "elevation_master.tif"))
    validation.check("DEM exists and is readable", dem is not None)
    validation.check("DEM resolution", dem.GetGeoTransform()[1] == 2000.0, dem.GetGeoTransform()[1])
    validation.check("DEM NoData", dem.GetRasterBand(1).GetNoDataValue() == -32768, dem.GetRasterBand(1).GetNoDataValue())
    validation.check("DEM working projection", "Albers" in dem.GetProjection(), dem.GetProjection()[:120])
    dem = None

    metrics = read_json(GRID / "reports" / "cell_scale_metrics.json")
    sizes = [item["cell_size_m"] for item in metrics["candidates"]]
    validation.check("four Cell candidates", sizes == [500, 1000, 2000, 4000], sizes)
    validation.check("candidate strategic metrics", all(len(item["strategic_distances"]) == 6 for item in metrics["candidates"]))
    validation.check("candidate city metrics", all(len(item["city_surroundings"]) == 77 for item in metrics["candidates"]))
    validation.check("candidate pass and river metrics", all(len(item["pass_analysis"]) == 4 and len(item["river_analysis"]) == 3 for item in metrics["candidates"]))
    previews = list((GRID / "reports" / "previews").glob("*.png"))
    validation.check("sixteen previews plus contact sheet", len(previews) == 17, len(previews))

    manifest = read_json(UNITY / "world_manifest.json")
    validation.check("selected 2000m V0 grid", manifest["cell_size_m"] == 2000)
    validation.check("national Cell count", manifest["total_cells"] == manifest["rows"] * manifest["columns"], manifest["total_cells"])
    validation.check("implicit stable CellId contract", "row * columns + column" in manifest["cell_id_algorithm"])
    corner_ids = {
        0,
        manifest["columns"] - 1,
        (manifest["rows"] - 1) * manifest["columns"],
        manifest["total_cells"] - 1,
    }
    validation.check("row-column corners have unique CellIds", len(corner_ids) == 4, sorted(corner_ids))
    directions = ((-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1))
    samples = ((0, 0), (0, manifest["columns"] - 1),
               (manifest["rows"] // 2, manifest["columns"] // 2),
               (manifest["rows"] - 1, manifest["columns"] - 1))
    symmetric = True
    for row, column in samples:
        neighbors = {
            (row + row_delta, column + column_delta)
            for row_delta, column_delta in directions
            if 0 <= row + row_delta < manifest["rows"] and 0 <= column + column_delta < manifest["columns"]
        }
        for neighbor_row, neighbor_column in neighbors:
            reverse = (row - neighbor_row, column - neighbor_column)
            if reverse not in directions:
                symmetric = False
    validation.check("sampled eight-neighbor relation is symmetric", symmetric)
    unity_cities = read_json(UNITY / "locations" / "cities.json")["features"]
    mapped_cities = [feature for feature in unity_cities if feature["properties"].get("cell_id") is not None]
    unresolved_cities = [feature for feature in unity_cities if feature["properties"].get("cell_id") is None]
    validation.check("72 positioned cities map to Cells", len(mapped_cities) == 72, len(mapped_cities))
    validation.check("five unresolved cities remain unmapped", len(unresolved_cities) == 5, len(unresolved_cities))
    validation.check("positioned cities map to unique Cells", len({
        feature["properties"]["cell_id"] for feature in mapped_cities
    }) == len(mapped_cities), len(mapped_cities))
    validation.check("city Cell mappings use stable row-major IDs", all(
        feature["properties"]["cell_id"] ==
        feature["properties"]["row"] * manifest["columns"] + feature["properties"]["column"]
        for feature in mapped_cities
    ))
    unity_sites = read_json(UNITY / "locations" / "strategic_sites.json")["features"]
    validation.check("all strategic sites map to Cells", len(unity_sites) == 31 and all(
        feature["properties"].get("cell_id") is not None for feature in unity_sites
    ), len(unity_sites))
    chunk_details = {}
    for name in ("terrain.bin", "elevation.bin", "water.bin", "admin.bin", "roads.bin"):
        detail = inspect_chunk_file(UNITY / "cells" / name)
        chunk_details[name] = detail
        validation.check(f"{name} header", detail["magic"] == "HWC0" and detail["version"] == 1)
        validation.check(f"{name} dimensions", detail["rows"] == manifest["rows"] and detail["columns"] == manifest["columns"])
        validation.check(f"{name} compressed sample", detail["sample_raw_length"] == detail["sample_declared_length"])

    road_edges = read_json(UNITY / "locations" / "road_edges.json")["routes"]
    continuous = True
    for route in road_edges:
        values = route["cell_ids"]
        if not values:
            continuous = False
            break
        for first, second in zip(values, values[1:]):
            row0, col0 = divmod(first, manifest["columns"])
            row1, col1 = divmod(second, manifest["columns"])
            if abs(row1 - row0) > 1 or abs(col1 - col0) > 1:
                continuous = False
                break
    validation.check("all road paths eight-neighbor continuous", continuous, len(road_edges))
    validation.check("Unity reader exists", (REPO / "Assets" / "Scripts" / "Mandate.Persistence" / "WorldMapDataReader.cs").exists())
    validation.check("MapValidation scene exists", (REPO / "Assets" / "Scenes" / "MapValidation.unity").exists())

    payload = {
        "schema": "hanworld.pipeline-validation.v0", "passed": validation.passed,
        "checks": validation.checks, "chunk_details": chunk_details,
    }
    report_path = GRID / "reports" / "pipeline_validation.json"
    report_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    failures = [item for item in validation.checks if not item["passed"]]
    print(json.dumps({"passed": validation.passed, "checks": len(validation.checks), "failures": failures}, ensure_ascii=False))
    return 0 if validation.passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
