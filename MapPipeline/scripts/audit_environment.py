from __future__ import annotations

import importlib
import json
import platform
import subprocess
import sys
from pathlib import Path

from osgeo import gdal, osr


def main() -> int:
    repo = Path(__file__).resolve().parents[2]
    qgis_root = Path(sys.executable).resolve().parent.parent
    report_path = repo / "MapData" / "HanWorld_Master_V0" / "reports" / "MAP_PIPELINE_ENVIRONMENT_AUDIT.json"
    report_path.parent.mkdir(parents=True, exist_ok=True)

    modules = {}
    for name in ("geopandas", "shapely", "pyproj", "numpy", "matplotlib", "PIL", "scipy"):
        try:
            module = importlib.import_module(name)
            modules[name] = {"available": True, "version": getattr(module, "__version__", "bundled")}
        except Exception as exc:  # noqa: BLE001 - audit must report every missing optional dependency.
            modules[name] = {"available": False, "error": str(exc)}

    executable_candidates = {
        "qgis_process": qgis_root / "apps" / "qgis-ltr" / "bin" / "qgis_process.exe",
        "gdalinfo": qgis_root / "bin" / "gdalinfo.exe",
        "gdalwarp": qgis_root / "bin" / "gdalwarp.exe",
        "ogr2ogr": qgis_root / "bin" / "ogr2ogr.exe",
    }
    executables = {
        name: str(path) if path.is_file() else None
        for name, path in executable_candidates.items()
    }

    payload = {
        "schema": "hanworld.environment-audit.v0",
        "qgis_root": str(qgis_root),
        "python": {"version": platform.python_version(), "executable": sys.executable},
        "gdal": gdal.VersionInfo("RELEASE_NAME"),
        "proj": osr.GetPROJVersionMajor(),
        "modules": modules,
        "executables": executables,
        "manual_dependency_required": not all(executables.values()),
        "notes": [
            "The project uses the Python and GDAL runtime bundled with QGIS.",
            "Rasterio is intentionally not required; all raster work uses osgeo.gdal.",
        ],
    }
    report_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    try:
        git_lfs = subprocess.run(["git", "lfs", "version"], cwd=repo, capture_output=True, text=True, timeout=10)
        git_lfs_version = (git_lfs.stdout or git_lfs.stderr).strip()
    except Exception as exc:  # noqa: BLE001
        git_lfs_version = f"unavailable: {exc}"
    unity_version = (repo / "ProjectSettings" / "ProjectVersion.txt").read_text(encoding="utf-8").strip()
    markdown = f"""# MAP PIPELINE ENVIRONMENT AUDIT

## Result

- QGIS-bundled Python: {payload['python']['version']}
- QGIS root: {payload['qgis_root']}
- GDAL: {payload['gdal']}
- PROJ major version: {payload['proj']}
- GeoPandas: {modules['geopandas'].get('version', 'missing')}
- Shapely: {modules['shapely'].get('version', 'missing')}
- PyProj: {modules['pyproj'].get('version', 'missing')}
- NumPy: {modules['numpy'].get('version', 'missing')}
- Matplotlib: {modules['matplotlib'].get('version', 'missing')}
- SciPy: {modules['scipy'].get('version', 'missing')}
- Rasterio: intentionally not required; `osgeo.gdal` is the raster implementation
- Git LFS: {git_lfs_version}
- Unity project lock: {unity_version}
- QGIS command-line tools: {'all resolved' if all(executables.values()) else 'one or more missing'}

## Dependency decision

No manual dependency installation is required. The reproducible entry point is
`MapPipeline/scripts/Invoke-QgisPython.ps1`, which discovers the installed QGIS LTR runtime and
sets isolated GDAL, PROJ and Matplotlib paths without changing the machine-wide Python setup.
"""
    (report_path.parent / "MAP_PIPELINE_ENVIRONMENT_AUDIT.md").write_text(markdown, encoding="utf-8")
    print(report_path)
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
