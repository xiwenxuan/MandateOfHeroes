# MAP PIPELINE ENVIRONMENT AUDIT

## Result

- QGIS-bundled Python: 3.12.13
- QGIS root: C:\Program Files\QGIS 3.44.12
- GDAL: 3.13.1
- PROJ major version: 9
- GeoPandas: 1.1.3
- Shapely: 2.1.2
- PyProj: 3.7.2
- NumPy: 2.4.6
- Matplotlib: 3.10.9
- SciPy: 1.17.1
- Rasterio: intentionally not required; `osgeo.gdal` is the raster implementation
- Git LFS: git-lfs/3.7.1 (GitHub; windows amd64; go 1.25.1; git b84b3384)
- Unity project lock: m_EditorVersion: 2022.3.62f3c1
m_EditorVersionWithRevision: 2022.3.62f3c1 (1623fc0bbb97)
- QGIS command-line tools: all resolved

## Dependency decision

No manual dependency installation is required. The reproducible entry point is
`MapPipeline/scripts/Invoke-QgisPython.ps1`, which discovers the installed QGIS LTR runtime and
sets isolated GDAL, PROJ and Matplotlib paths without changing the machine-wide Python setup.
