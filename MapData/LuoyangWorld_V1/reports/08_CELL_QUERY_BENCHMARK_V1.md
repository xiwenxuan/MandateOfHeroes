# Cell query benchmark V1

```json
{
  "warm_random_5000_ms": 5.292,
  "sequential_10000_ms": 2.325,
  "cached_chunk_100000_ms": 22.998,
  "checksum": 1
}
```

Unity EditMode adds ColdRandom, WarmRandom, Sequential, Batch and CachedChunk evidence. Chunks are cache/batch units, never ownership units.

## Unity C# reader evidence (2026-08-09)

|Case|Operations|Duration|
|---|---:|---:|
|Cold random|500|942.375 ms|
|Warm random|5,000|906.553 ms|
|Sequential, warm neighboring Cells|10,000|2.970 ms|
|Read one 64×64 Chunk snapshot|1|1.615 ms|
|Read cached Chunk array|100,000|0.163 ms|

Evidence: `tmp/unity-validation/cell-query-benchmark-v1.json`. Cross-Chunk random access is the expensive path;
visualization and simulation scans should retain Chunk snapshots or use batch queries. A Chunk remains only an
I/O/cache unit and does not become a gameplay, ownership or Facility unit.
