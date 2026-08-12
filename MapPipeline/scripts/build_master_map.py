from __future__ import annotations

import csv
import hashlib
import json
import math
import re
import shutil
import time
import urllib.request
from collections import defaultdict
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

import geopandas as gpd
import numpy as np
from osgeo import gdal
from pyproj import CRS, Transformer
from shapely.geometry import LineString, Point, box, mapping
from shapely.ops import unary_union

gdal.UseExceptions()


REPO = Path(__file__).resolve().parents[2]
CONFIG = REPO / "MapPipeline" / "config"
CACHE = REPO / "MapPipeline" / "sources" / "cache"
MASTER = REPO / "MapData" / "HanWorld_Master_V0"
PHYSICAL = MASTER / "physical"
HISTORICAL = MASTER / "historical"
ADMIN = MASTER / "administrative"
REPORTS = MASTER / "reports"
MANIFEST = MASTER / "manifest"


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def rows(path: Path):
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")


def write_geojson(frame: gpd.GeoDataFrame, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    # RFC 7946 GeoJSON coordinates are WGS84. Projected working geometry is retained in the GeoPackage.
    frame.to_crs("EPSG:4326").to_file(path, driver="GeoJSON")


def read_natural_earth(zip_name: str, bbox_wgs84) -> gpd.GeoDataFrame:
    frame = gpd.read_file(f"zip://{CACHE / zip_name}", bbox=bbox_wgs84)
    if frame.crs is None:
        frame = frame.set_crs("EPSG:4326")
    return frame


def parse_city_names() -> dict[str, tuple[str, str]]:
    result = {}
    pattern = re.compile(r"^\|\s*(C\d{3})\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|")
    text = (REPO / "Docs" / "CITY_UNION_MASTER.md").read_text(encoding="utf-8")
    for line in text.splitlines():
        match = pattern.match(line)
        if match:
            result[match.group(1)] = (match.group(2).strip(), match.group(3).strip())
    return result


def hierarchy_key(admin_id: str) -> str:
    parts = admin_id.split(".")
    return parts[2] if len(parts) > 2 else "unknown"


PROVINCE_BOXES = {
    "sili": (109.0, 33.0, 114.5, 36.5), "youzhou": (115.0, 38.0, 126.0, 42.5),
    "jizhou": (113.0, 36.0, 118.5, 40.0), "yuzhou": (112.0, 31.0, 117.0, 35.5),
    "yanzhou": (114.0, 34.0, 119.0, 37.5), "xuzhou": (116.0, 31.0, 121.0, 35.5),
    "qingzhou": (117.0, 35.0, 121.5, 38.5), "jingzhou": (108.0, 24.0, 114.5, 34.0),
    "yangzhou": (114.0, 23.0, 122.0, 33.5), "yizhou": (98.0, 22.0, 110.5, 34.5),
    "liangzhou": (94.0, 32.0, 108.0, 41.0), "bingzhou": (110.0, 35.0, 114.5, 41.0),
    "jiaozhou": (104.0, 18.0, 113.5, 25.5),
}


def deterministic_proxy_point(stable_id: str) -> Point:
    west, south, east, north = PROVINCE_BOXES.get(hierarchy_key(stable_id), (73.0, 18.0, 135.0, 54.0))
    raw = hashlib.sha256(stable_id.encode("utf-8")).digest()
    u = int.from_bytes(raw[:8], "little") / float(2**64 - 1)
    v = int.from_bytes(raw[8:16], "little") / float(2**64 - 1)
    margin_x = (east - west) * 0.06
    margin_y = (north - south) * 0.06
    return Point(west + margin_x + u * (east - west - 2 * margin_x), south + margin_y + v * (north - south - 2 * margin_y))


def create_dem(crs_proj: str, projected_bounds: tuple[float, float, float, float]) -> dict:
    output = PHYSICAL / "elevation_master.tif"
    tile_cache = CACHE / "mapzen_z6"
    tile_cache.mkdir(parents=True, exist_ok=True)

    def tile_x(longitude: float, zoom: int) -> int:
        return int(math.floor((longitude + 180.0) / 360.0 * (1 << zoom)))

    def tile_y(latitude: float, zoom: int) -> int:
        radians = math.radians(latitude)
        return int(math.floor((1.0 - math.asinh(math.tan(radians)) / math.pi) / 2.0 * (1 << zoom)))

    zoom = 6
    extent = read_json(CONFIG / "world_extent.json")
    x_min, x_max = tile_x(extent["west"], zoom), tile_x(extent["east"], zoom)
    y_min, y_max = tile_y(extent["north"], zoom), tile_y(extent["south"], zoom)
    requests = []
    for x in range(x_min, x_max + 1):
        for y in range(y_min, y_max + 1):
            path = tile_cache / f"{x}_{y}.tif"
            url = f"https://s3.amazonaws.com/elevation-tiles-prod/geotiff/{zoom}/{x}/{y}.tif"
            requests.append((x, y, url, path))

    def download(item):
        x, y, url, path = item
        if path.exists() and path.stat().st_size > 1024:
            return x, y, url, path
        temporary = path.with_suffix(".partial")
        for attempt in range(3):
            try:
                request = urllib.request.Request(url, headers={"User-Agent": "MandateOfHeroes-MapPipeline/0"})
                with urllib.request.urlopen(request, timeout=30) as response, temporary.open("wb") as handle:
                    shutil.copyfileobj(response, handle)
                temporary.replace(path)
                return x, y, url, path
            except Exception:
                temporary.unlink(missing_ok=True)
                if attempt == 2:
                    raise
                time.sleep(1 + attempt)

    completed = []
    with ThreadPoolExecutor(max_workers=12) as pool:
        futures = [pool.submit(download, item) for item in requests]
        for future in as_completed(futures):
            completed.append(future.result())
    completed.sort()

    tile_manifest = {
        "schema": "hanworld.elevation-tiles.v0", "zoom": zoom,
        "attribution": "Mapzen; SRTM/GMTED2010 data courtesy of the U.S. Geological Survey",
        "tiles": [{"x": x, "y": y, "url": url, "bytes": path.stat().st_size, "sha256": sha256(path)} for x, y, url, path in completed],
    }
    write_json(MANIFEST / "elevation_tiles.resolved.json", tile_manifest)
    vrt = MASTER / "working_elevation.vrt"
    dataset = gdal.BuildVRT(str(vrt), [str(path) for _, _, _, path in completed])
    if dataset is None:
        raise RuntimeError("GDAL failed to mosaic Mapzen elevation tiles")
    dataset.FlushCache()
    dataset = None
    options = gdal.WarpOptions(
        format="GTiff", dstSRS=crs_proj, outputBounds=projected_bounds,
        xRes=2000.0, yRes=2000.0, resampleAlg="bilinear",
        outputType=gdal.GDT_Int16, srcNodata=-32768, dstNodata=-32768,
        creationOptions=["TILED=YES", "COMPRESS=DEFLATE", "PREDICTOR=2", "BIGTIFF=IF_SAFER"],
        multithread=True,
    )
    result = gdal.Warp(str(output), str(vrt), options=options)
    if result is None:
        raise RuntimeError("GDAL failed to create elevation_master.tif")
    result.FlushCache()
    width, height = result.RasterXSize, result.RasterYSize
    result = None
    vrt.unlink(missing_ok=True)
    return {"path": str(output.relative_to(REPO)), "width": width, "height": height, "resolution_m": 2000, "source_tile_count": len(completed)}


def main() -> int:
    for directory in (PHYSICAL, HISTORICAL, ADMIN, REPORTS, MANIFEST):
        directory.mkdir(parents=True, exist_ok=True)

    extent = read_json(CONFIG / "world_extent.json")
    crs_config = read_json(CONFIG / "coordinate_system.json")
    west, south, east, north = (extent[key] for key in ("west", "south", "east", "north"))
    bbox_wgs = box(west, south, east, north)
    crs_wgs = CRS.from_epsg(4326)
    crs_work = CRS.from_user_input(crs_config["proj_string"])
    to_work = Transformer.from_crs(crs_wgs, crs_work, always_xy=True)
    # A conic projection can reach its extrema between the four geographic corners.
    # Densify the envelope so southern/eastern locations are not clipped from the Cell grid.
    p_bounds = to_work.transform_bounds(west, south, east, north, densify_pts=41)

    physical_sources = {
        "land": ("ne_10m_land.zip", PHYSICAL / "terrain_reference.geojson"),
        "coastline": ("ne_10m_coastline.zip", PHYSICAL / "coastline.geojson"),
        "lakes": ("ne_10m_lakes.zip", PHYSICAL / "major_lakes.geojson"),
        "rivers": ("ne_10m_rivers_lake_centerlines.zip", PHYSICAL / "major_rivers.geojson"),
    }
    physical_frames = {}
    for key, (zip_name, output) in physical_sources.items():
        frame = read_natural_earth(zip_name, bbox_wgs)
        frame = gpd.clip(frame, gpd.GeoSeries([bbox_wgs], crs="EPSG:4326"))
        frame = frame.to_crs(crs_work)
        frame["historical_claim"] = False
        frame["geometry_status"] = "modern_physical_reference"
        frame["source_id"] = f"source.natural_earth.{key}.10m"
        physical_frames[key] = frame
        write_geojson(frame, output)

    crosswalk = {r["game_location_id"]: r for r in rows(REPO / "Data" / "HistoricalPopulation" / "game_location_crosswalk.csv")}
    admin_to_stable = {r["source_id"]: r["target_id"] for r in rows(REPO / "Data" / "HistoricalPopulation" / "han_140_region_mapping.csv")}
    names = parse_city_names()
    city_features = []
    coord_by_location = {}
    for item in rows(CONFIG / "city_coordinates_v0.csv"):
        city_id = item["city_id"]
        longitude = float(item["longitude"]) if item["longitude"] else None
        latitude = float(item["latitude"]) if item["latitude"] else None
        geometry = Point(longitude, latitude) if longitude is not None and latitude is not None else None
        if geometry is not None:
            coord_by_location[city_id] = geometry
        display, historical = names.get(city_id, (city_id, city_id))
        linked = crosswalk.get(city_id, {})
        city_features.append({
            "city_id": city_id, "display_name": display, "historical_name": historical,
            "site_type": "strategic_city", "admin_reference": linked.get("admin_unit_id", ""),
            "stable_region_id": linked.get("stable_region_id", ""),
            "longitude": longitude, "latitude": latitude,
            "coordinate_status": item["coordinate_status"], "confidence": item["confidence"],
            "source_ids": linked.get("source_ids", "source.project.city_union_master"),
            "historical_claim": item["coordinate_status"] != "unresolved",
            "notes": item["notes"], "geometry": geometry,
        })
    cities = gpd.GeoDataFrame(city_features, crs=crs_wgs)
    write_geojson(cities.to_crs(crs_work), HISTORICAL / "strategic_cities.geojson")

    prototype_rows = rows(CONFIG / "prototype_locations_v0.csv")
    for item in prototype_rows:
        coord_by_location[item["location_id"]] = Point(float(item["longitude"]), float(item["latitude"]))

    admin_rows = rows(REPO / "Data" / "HistoricalPopulation" / "han_140_administrative_units.csv")
    admin_by_id = {r["admin_unit_id"]: r for r in admin_rows}
    city_by_admin = {}
    for feature in city_features:
        if feature["geometry"] is not None and feature["admin_reference"]:
            city_by_admin.setdefault(feature["admin_reference"], feature["geometry"])

    county_features = []
    proxy_points = {}
    for item in (r for r in admin_rows if r["unit_type"] == "county"):
        point = city_by_admin.get(item["admin_unit_id"])
        status = "located_from_project_city" if point is not None else "unresolved"
        county_features.append({
            "admin_unit_id": item["admin_unit_id"], "stable_region_id": admin_to_stable.get(item["admin_unit_id"], ""),
            "display_name": item["canonical_name"], "parent_admin_unit_id": item["parent_admin_unit_id"],
            "coordinate_status": status, "confidence": item["confidence"] if point is not None else "unresolved",
            "historical_claim": point is not None, "source_ids": item["source_ids"], "geometry": point,
        })
        proxy_points[item["admin_unit_id"]] = point or deterministic_proxy_point(item["admin_unit_id"])
    county_anchors = gpd.GeoDataFrame(county_features, crs=crs_wgs)
    write_geojson(county_anchors.to_crs(crs_work), HISTORICAL / "county_anchors.geojson")

    site_features = []
    for item in rows(CONFIG / "strategic_sites_v0.csv"):
        point = Point(float(item["longitude"]), float(item["latitude"]))
        coord_by_location[item["site_id"]] = point
        site_features.append({**item, "historical_claim": item["historical_claim"].lower() == "true", "geometry": point})
    sites = gpd.GeoDataFrame(site_features, crs=crs_wgs)
    write_geojson(sites.to_crs(crs_work), HISTORICAL / "strategic_sites.geojson")

    route_features = []
    for item in rows(CONFIG / "routes_v0.csv"):
        start, end = coord_by_location.get(item["start_location_id"]), coord_by_location.get(item["end_location_id"])
        geometry = LineString([start, end]) if start is not None and end is not None else None
        route_features.append({**item, "historical_claim": False, "geometry_status": "approximate_corridor", "geometry": geometry})
    routes = gpd.GeoDataFrame(route_features, crs=crs_wgs)
    write_geojson(routes.to_crs(crs_work), HISTORICAL / "major_routes_v0.geojson")

    proxy_frame = gpd.GeoDataFrame(
        [{"admin_unit_id": key, "geometry": value} for key, value in proxy_points.items()], crs=crs_wgs
    ).to_crs(crs_work)
    proxy_lookup = dict(zip(proxy_frame["admin_unit_id"], proxy_frame.geometry))
    county_polygons = []
    by_parent = defaultdict(list)
    by_province = defaultdict(list)
    for item in (r for r in admin_rows if r["unit_type"] == "county"):
        point = proxy_lookup[item["admin_unit_id"]]
        county_polygons.append({
            "admin_unit_id": item["admin_unit_id"], "parent_admin_unit_id": item["parent_admin_unit_id"],
            "geometry_status": "synthetic_proxy", "historical_claim": False, "confidence": "technical_only",
            "geometry": point.buffer(12000),
        })
        by_parent[item["parent_admin_unit_id"]].append(point)
        by_province[hierarchy_key(item["admin_unit_id"])].append(point)
    counties_v0 = gpd.GeoDataFrame(county_polygons, crs=crs_work)

    commandery_polygons = []
    for parent_id, points in by_parent.items():
        geometry = unary_union(points).convex_hull.buffer(35000)
        commandery_polygons.append({
            "admin_unit_id": parent_id, "display_name": admin_by_id.get(parent_id, {}).get("canonical_name", parent_id),
            "geometry_status": "synthetic_proxy", "historical_claim": False, "confidence": "technical_only", "geometry": geometry,
        })
    commanderies_v0 = gpd.GeoDataFrame(commandery_polygons, crs=crs_work)

    province_polygons = []
    for province, points in by_province.items():
        province_id = f"admin.han140.{province}"
        geometry = unary_union(points).convex_hull.buffer(85000)
        province_polygons.append({
            "admin_unit_id": province_id, "display_name": admin_by_id.get(province_id, {}).get("canonical_name", province_id),
            "geometry_status": "synthetic_proxy", "historical_claim": False, "confidence": "technical_only", "geometry": geometry,
        })
    provinces_v0 = gpd.GeoDataFrame(province_polygons, crs=crs_work)
    write_geojson(provinces_v0, ADMIN / "provinces_v0.geojson")
    write_geojson(commanderies_v0, ADMIN / "commanderies_v0.geojson")
    write_geojson(counties_v0, ADMIN / "counties_v0.geojson")

    test_region_features = []
    for item in read_json(CONFIG / "test_regions.json")["regions"]:
        test_region_features.append({**item, "geometry": box(item["west"], item["south"], item["east"], item["north"])})
    test_regions = gpd.GeoDataFrame(test_region_features, crs=crs_wgs).to_crs(crs_work)
    write_geojson(test_regions, HISTORICAL / "test_regions.geojson")

    gpkg = MASTER / "HanWorld_Master_V0.gpkg"
    if gpkg.exists():
        gpkg.unlink()
    layers = {
        "land_reference": physical_frames["land"], "coastline": physical_frames["coastline"], "major_rivers": physical_frames["rivers"],
        "major_lakes": physical_frames["lakes"], "strategic_cities": cities.to_crs(crs_work),
        "county_anchors": county_anchors.to_crs(crs_work), "strategic_sites": sites.to_crs(crs_work),
        "provinces_v0": provinces_v0, "commanderies_v0": commanderies_v0, "counties_v0": counties_v0,
        "major_routes_v0": routes.to_crs(crs_work), "test_regions": test_regions,
    }
    for layer_name, frame in layers.items():
        frame.to_file(gpkg, layer=layer_name, driver="GPKG")

    dem_meta = create_dem(crs_config["proj_string"], p_bounds)

    source_manifest = read_json(CONFIG / "external_sources.json")
    for source in source_manifest["sources"]:
        local = REPO / source["local_cache"]
        if local.is_file():
            source["sha256"] = sha256(local)
            source["verification"] = "complete_source_archive_sha256"
        elif source["source_id"] == "source.mapzen.terrain_tiles.geotiff.v1_1":
            tile_manifest_path = MANIFEST / "elevation_tiles.resolved.json"
            source["sha256"] = sha256(tile_manifest_path)
            source["verification"] = "per-tile SHA256 values in elevation_tiles.resolved.json"
        elif local.is_dir():
            relevant = sorted(local.glob("*.csv")) + sorted(local.glob("*.json"))
            digest = hashlib.sha256("".join(f"{p.relative_to(REPO)}:{sha256(p)}\n" for p in relevant).encode()).hexdigest()
            source["sha256"] = digest
            source["verification"] = "composite_repository_input_sha256"
    write_json(MANIFEST / "external_sources.resolved.json", source_manifest)

    output_files = [p for p in MASTER.rglob("*") if p.is_file() and p.name != "HanWorld_Master_V0_manifest.json"]
    manifest = {
        "schema": "hanworld.master-manifest.v0", "crs_id": crs_config["crs_id"],
        "projected_bounds": {"min_x": p_bounds[0], "min_y": p_bounds[1], "max_x": p_bounds[2], "max_y": p_bounds[3]},
        "counts": {"strategic_cities": len(cities), "located_cities": int(cities.geometry.notna().sum()),
                   "county_catalog": len(county_anchors), "located_county_anchors": int(county_anchors.geometry.notna().sum()),
                   "strategic_sites": len(sites), "routes": len(routes), "test_regions": len(test_regions)},
        "dem": dem_meta,
        "files": [{"path": str(p.relative_to(REPO)).replace("\\", "/"), "bytes": p.stat().st_size, "sha256": sha256(p)} for p in output_files],
        "historical_uncertainty": "All synthetic administrative geometries are geometry_status=synthetic_proxy and historical_claim=false.",
    }
    write_json(MASTER / "HanWorld_Master_V0_manifest.json", manifest)

    report = f"""# HAN WORLD MASTER V0 REPORT

## Result

- Working CRS: `{crs_config['crs_id']}` (metres, Albers equal-area)
- Strategic cities: {len(cities)} total; {int(cities.geometry.notna().sum())} positioned; {int(cities.geometry.isna().sum())} deliberately unresolved
- Han 140 county catalog: {len(county_anchors)} stable `admin.han140.*` identities
- Strategic sites: {len(sites)}
- Route corridors: {len(routes)}, including R001-R012
- Fixed experiment regions: {len(test_regions)}
- DEM: {dem_meta['width']} x {dem_meta['height']} at {dem_meta['resolution_m']} metres

## Historical boundary

The processing envelope is not a Han border. Natural Earth layers and GMTED2010 are modern physical references.
Administrative V0 polygons are technical proxies only: `geometry_status=synthetic_proxy`, `historical_claim=false`.
Unresolved city and county-seat coordinates remain null and are not fabricated to fill a quota.

## Reproducibility

Run `powershell -NoProfile -ExecutionPolicy Bypass -File MapPipeline/scripts/Invoke-QgisPython.ps1 MapPipeline/scripts/build_master_map.py`.
Hashes and source-license metadata are in `HanWorld_Master_V0_manifest.json` and `manifest/external_sources.resolved.json`.
"""
    (REPORTS / "HAN_WORLD_MASTER_V0_REPORT.md").write_text(report, encoding="utf-8")
    print(json.dumps(manifest["counts"], ensure_ascii=False))
    print(gpkg)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
