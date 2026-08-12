# HAN WORLD MASTER V0 REPORT

## Result

- Working CRS: `hanworld.albers.china.v0` (metres, Albers equal-area)
- Strategic cities: 77 total; 72 positioned; 5 deliberately unresolved
- Han 140 county catalog: 1182 stable `admin.han140.*` identities
- Strategic sites: 31
- Route corridors: 18, including R001-R012
- Fixed experiment regions: 4
- DEM: 3314 x 2176 at 2000 metres

## Historical boundary

The processing envelope is not a Han border. Natural Earth layers and GMTED2010 are modern physical references.
Administrative V0 polygons are technical proxies only: `geometry_status=synthetic_proxy`, `historical_claim=false`.
Unresolved city and county-seat coordinates remain null and are not fabricated to fill a quota.

## Reproducibility

Run `powershell -NoProfile -ExecutionPolicy Bypass -File MapPipeline/scripts/Invoke-QgisPython.ps1 MapPipeline/scripts/build_master_map.py`.
Hashes and source-license metadata are in `HanWorld_Master_V0_manifest.json` and `manifest/external_sources.resolved.json`.
