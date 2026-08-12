# Grid alignment root cause

```json
{
  "old_1000_cells": 28845056,
  "old_500_expected": 115380224,
  "old_500_actual": 115366968,
  "old_difference": 13256,
  "root_cause": "Each candidate independently applied ceil to projected bounds. The 500m height became 8703 instead of the exact 4x subdivision height 8704, losing one full 13256-Cell row.",
  "fixed_dimensions": {
    "500": {
      "columns": 13256,
      "rows": 8704
    },
    "1000": {
      "columns": 6628,
      "rows": 4352
    },
    "2000": {
      "columns": 3314,
      "rows": 2176
    },
    "4000": {
      "columns": 1657,
      "rows": 1088
    }
  },
  "common_origin": [
    -3417344.395965772,
    6199580.451937504
  ],
  "grid_schema_version": "hanworld.square-grid.v1"
}
```

All 500/1000/2000/4000m candidates share one CRS and origin and use integer subdivision or aggregation only.
