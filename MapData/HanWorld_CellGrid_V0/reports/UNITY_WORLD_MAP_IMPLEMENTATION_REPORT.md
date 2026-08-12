# UNITY WORLD MAP IMPLEMENTATION REPORT

## Outcome

`HanWorldV0` is a real, chunked Unity map package rather than a mock dashboard. It contains 7,211,264
stable square Cells at the V0 working default of 2,000 metres, stored in 1,768 chunks of at most 64×64 Cells.
The generated StreamingAssets package is approximately 10.7 MB.

## Architecture

- `Mandate.Domain`: `WorldMapCellId`, `CellGridIndex`, `CellNeighborService`, `WorldMapCellRecord`.
- `Mandate.Persistence`: manifest contracts and `WorldMapDataReader`; raw-deflate chunks are loaded on demand
  with a bounded 64-chunk cache per channel.
- `Mandate.Presentation`: `MapValidationController` builds one bounded texture from queried Cell facts and
  supports nationwide view, pan, zoom and click inspection.
- `Assets/Scenes/MapValidation.unity`: runnable validation scene with a camera and controller.

There is no one-Cell-one-GameObject implementation. Owner, Facility, Resource and Force remain empty runtime
fields so mutable simulation/save facts are not baked into the authored map.

## Cell data available on click

The scene draws all 72 positioned strategic cities as independent markers; five unresolved city locations remain
unmapped instead of receiving invented coordinates. CellId, row, column, projected center, elevation, terrain, slope, water, province, commandery, county, road,
buildability and passability are read from the actual package. Province/commandery/county values are numeric
package indexes resolved through `admin_catalog.json` back to existing `admin.han140.*` identities.

## Performance evidence

Latest controlled Unity EditMode evidence (`tmp/unity-validation/world-map-performance.json`):

- package open and header validation: 3 ms;
- 10,000 real Cell queries: 1,594 ms;
- 1,000 eight-neighbor queries: below the millisecond counter resolution;
- four full 64×64 Chunk reads: 5 ms;
- measured managed-memory delta: 30,404,608 bytes.

These are smoke-test measurements on the current development machine, not final shipping hardware guarantees.

## How to inspect

Open `Assets/Scenes/MapValidation.unity` in Unity and press Play. Use the arrow buttons to pan, the zoom buttons
to change Cell sampling, and click the map to inspect the real selected Cell. The scene is also present in
Editor Build Settings.
