# Cell ID and grid schema migration

- GridSchemaVersion: `hanworld.square-grid.v1`.
- GridX is west-to-east column; GridY is north-to-south row.
- CellId64 is `ulong(GridY * Columns + GridX)` and is interpreted only inside the same GridSchemaVersion.
- Person, Household, Family, City, County, Facility, Force and Road ObjectIDs remain independent; relations use ObjectID -> CurrentCellID.
- C027 stores only CityAnchorCellId; Luoyang extent is the mutable occupied Facility Cell set.
- V1 is an independent structural prototype. Formal main-save adoption requires a sequential migration.
