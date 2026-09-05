using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class WorldMapDataReader : IDisposable
    {
        private readonly ChunkedBinaryFile _terrain;
        private readonly ChunkedBinaryFile _elevation;
        private readonly ChunkedBinaryFile _water;
        private readonly ChunkedBinaryFile _admin;
        private readonly ChunkedBinaryFile _roads;

        public WorldMapDataReader(string packageRoot)
        {
            if (string.IsNullOrWhiteSpace(packageRoot))
            {
                throw new ArgumentException("World map package root is required.", nameof(packageRoot));
            }

            PackageRoot = Path.GetFullPath(packageRoot);
            Manifest = JsonConvert.DeserializeObject<WorldMapManifest>(
                File.ReadAllText(Path.Combine(PackageRoot, "world_manifest.json")))
                ?? throw new InvalidDataException("World map manifest is empty.");
            AdminCatalog = JsonConvert.DeserializeObject<WorldMapAdminCatalog>(
                File.ReadAllText(Path.Combine(PackageRoot, "metadata", "admin_catalog.json")))
                ?? throw new InvalidDataException("World map admin catalog is empty.");
            Cities = JsonConvert.DeserializeObject<WorldMapLocationFeatureCollection>(
                File.ReadAllText(Path.Combine(PackageRoot, "locations", "cities.json")))
                ?? throw new InvalidDataException("World map city catalog is empty.");
            Grid = new CellGridIndex(
                Manifest.Rows, Manifest.Columns, Manifest.OriginX, Manifest.OriginY, Manifest.CellSizeMetres,
                Manifest.GridSchemaVersion ?? "hanworld.square-grid.v1");
            Neighbors = new CellNeighborService(Grid);

            var cells = Path.Combine(PackageRoot, "cells");
            _terrain = new ChunkedBinaryFile(Path.Combine(cells, "terrain.bin"), Manifest, 1, 2);
            _elevation = new ChunkedBinaryFile(Path.Combine(cells, "elevation.bin"), Manifest, 2, 1);
            _water = new ChunkedBinaryFile(Path.Combine(cells, "water.bin"), Manifest, 1, 1);
            _admin = new ChunkedBinaryFile(Path.Combine(cells, "admin.bin"), Manifest, 2, 3);
            _roads = new ChunkedBinaryFile(Path.Combine(cells, "roads.bin"), Manifest, 1, 1);
            ValidateImplicitFiles(cells);
        }

        public string PackageRoot { get; }
        public WorldMapManifest Manifest { get; }
        public WorldMapAdminCatalog AdminCatalog { get; }
        public WorldMapLocationFeatureCollection Cities { get; }
        public CellGridIndex Grid { get; }
        public CellNeighborService Neighbors { get; }

        public WorldMapCellRecord ReadCell(WorldMapCellId id)
        {
            if (!Grid.TryDecode(id, out var row, out var column))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return ReadCell(row, column);
        }

        public WorldMapCellRecord ReadCell(int row, int column)
        {
            if (!Grid.Contains(row, column))
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            Grid.GetCenter(row, column, out var centerX, out var centerY);
            return new WorldMapCellRecord(
                Grid.ToCellId(row, column), row, column, centerX, centerY,
                _elevation.ReadInt16(row, column, 0),
                _terrain.ReadByte(row, column, 0), _terrain.ReadByte(row, column, 1),
                _water.ReadByte(row, column, 0),
                _admin.ReadUInt16(row, column, 0), _admin.ReadUInt16(row, column, 1),
                _admin.ReadUInt16(row, column, 2), _roads.ReadByte(row, column, 0),
                Grid.GridSchemaVersion);
        }

        public CellAdministrativeCodes ReadAdministrativeCodes(int row,
            int column)
        {
            if (!Grid.Contains(row, column))
                throw new ArgumentOutOfRangeException(nameof(row));
            return new CellAdministrativeCodes(
                _admin.ReadUInt16(row, column, 0),
                _admin.ReadUInt16(row, column, 1),
                _admin.ReadUInt16(row, column, 2));
        }

        public byte ReadRoadClass(int row, int column)
        {
            if (!Grid.Contains(row, column))
                throw new ArgumentOutOfRangeException(nameof(row));
            return _roads.ReadByte(row, column, 0);
        }

        public WorldMapCellRecord[] ReadChunk(int chunkRow, int chunkColumn)
        {
            if (chunkRow < 0 || chunkColumn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkRow));
            }

            var rowStart = chunkRow * Manifest.ChunkSize;
            var columnStart = chunkColumn * Manifest.ChunkSize;
            if (!Grid.Contains(rowStart, columnStart))
            {
                throw new ArgumentOutOfRangeException(nameof(chunkRow));
            }

            var rowEnd = Math.Min(Grid.Rows, rowStart + Manifest.ChunkSize);
            var columnEnd = Math.Min(Grid.Columns, columnStart + Manifest.ChunkSize);
            var result = new WorldMapCellRecord[(rowEnd - rowStart) * (columnEnd - columnStart)];
            var index = 0;
            for (var row = rowStart; row < rowEnd; row++)
            {
                for (var column = columnStart; column < columnEnd; column++)
                {
                    result[index++] = ReadCell(row, column);
                }
            }

            return result;
        }

        public WorldMapChunkSnapshot ReadChunkSnapshot(int chunkRow, int chunkColumn)
        {
            var cells = ReadChunk(chunkRow, chunkColumn);
            var rowStart = chunkRow * Manifest.ChunkSize;
            var columnStart = chunkColumn * Manifest.ChunkSize;
            var rowCount = Math.Min(Grid.Rows, rowStart + Manifest.ChunkSize) - rowStart;
            var columnCount = Math.Min(Grid.Columns, columnStart + Manifest.ChunkSize) - columnStart;
            return new WorldMapChunkSnapshot(chunkRow, chunkColumn, rowCount, columnCount, cells);
        }

        public WorldMapChunkSnapshot ReadCanonicalGlobalChunk(int chunkRow, int chunkColumn,
            int canonicalCellsPerSide = GlobalSpatialFoundationV1.CanonicalChunkSizeCells)
        {
            if (canonicalCellsPerSide <= 0) throw new ArgumentOutOfRangeException(nameof(canonicalCellsPerSide));
            var rowStart = checked(chunkRow * canonicalCellsPerSide);
            var columnStart = checked(chunkColumn * canonicalCellsPerSide);
            if (!Grid.Contains(rowStart, columnStart)) throw new ArgumentOutOfRangeException(nameof(chunkRow));
            var rowCount = Math.Min(canonicalCellsPerSide, Grid.Rows - rowStart);
            var columnCount = Math.Min(canonicalCellsPerSide, Grid.Columns - columnStart);
            var cells = new WorldMapCellRecord[rowCount * columnCount];
            var index = 0;
            for (var row = rowStart; row < rowStart + rowCount; row++)
                for (var column = columnStart; column < columnStart + columnCount; column++)
                    cells[index++] = ReadCell(row, column);
            return new WorldMapChunkSnapshot(chunkRow, chunkColumn, rowCount, columnCount, cells);
        }

        public string ResolveProvince(ushort code) => Resolve(AdminCatalog.Provinces, code);
        public string ResolveCommandery(ushort code) => Resolve(AdminCatalog.Commanderies, code);
        public string ResolveCounty(ushort code) => Resolve(AdminCatalog.Counties, code);

        public void Dispose()
        {
            _terrain.Dispose();
            _elevation.Dispose();
            _water.Dispose();
            _admin.Dispose();
            _roads.Dispose();
        }

        private static string Resolve(IReadOnlyList<string> values, ushort code)
        {
            return code < values.Count ? values[code] : null;
        }

        private void ValidateImplicitFiles(string cellsRoot)
        {
            using (var reader = new BinaryReader(File.OpenRead(Path.Combine(cellsRoot, "cells.bin"))))
            {
                if (new string(reader.ReadChars(4)) != "HCI0" || reader.ReadInt32() != 1 ||
                    reader.ReadInt32() != Manifest.Columns || reader.ReadInt32() != Manifest.Rows ||
                    reader.ReadInt32() != Manifest.CellSizeMetres)
                {
                    throw new InvalidDataException("cells.bin does not match world_manifest.json.");
                }
            }

            using (var reader = new BinaryReader(File.OpenRead(Path.Combine(cellsRoot, "neighbors.bin"))))
            {
                if (new string(reader.ReadChars(4)) != "HNB0" || reader.ReadInt32() != 1 || reader.ReadInt32() != 8)
                {
                    throw new InvalidDataException("neighbors.bin does not declare stable eight-direction adjacency.");
                }
            }
        }

        private sealed class ChunkedBinaryFile : IDisposable
        {
            private const int MaximumCachedChunks = 64;
            private readonly FileStream _stream;
            private readonly BinaryReader _reader;
            private readonly ChunkIndex[] _indexes;
            private readonly Dictionary<int, byte[]> _cache = new Dictionary<int, byte[]>();
            private readonly Queue<int> _cacheOrder = new Queue<int>();
            private readonly int _columns;
            private readonly int _rows;
            private readonly int _chunkSize;
            private readonly int _chunkColumns;
            private readonly int _valueSize;
            private readonly int _channels;

            public ChunkedBinaryFile(string path, WorldMapManifest manifest, int expectedValueSize, int expectedChannels)
            {
                _stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                _reader = new BinaryReader(_stream);
                if (new string(_reader.ReadChars(4)) != "HWC0" || _reader.ReadInt32() != 1)
                {
                    throw new InvalidDataException($"Unsupported chunk file: {path}");
                }

                _columns = _reader.ReadInt32();
                _rows = _reader.ReadInt32();
                _chunkSize = _reader.ReadInt32();
                _valueSize = _reader.ReadInt32();
                _channels = _reader.ReadInt32();
                _chunkColumns = _reader.ReadInt32();
                var chunkRows = _reader.ReadInt32();
                var chunkCount = _reader.ReadInt32();
                if (_columns != manifest.Columns || _rows != manifest.Rows || _chunkSize != manifest.ChunkSize ||
                    _valueSize != expectedValueSize || _channels != expectedChannels ||
                    chunkCount != _chunkColumns * chunkRows)
                {
                    throw new InvalidDataException($"Chunk header does not match manifest: {path}");
                }

                _indexes = new ChunkIndex[chunkCount];
                for (var index = 0; index < chunkCount; index++)
                {
                    _indexes[index] = new ChunkIndex(
                        _reader.ReadInt64(), _reader.ReadInt32(), _reader.ReadInt32(),
                        _reader.ReadUInt16(), _reader.ReadUInt16());
                }
            }

            public byte[] ReadCell(int row, int column)
            {
                if (row < 0 || row >= _rows || column < 0 || column >= _columns)
                {
                    throw new ArgumentOutOfRangeException(nameof(row));
                }

                var chunkRow = row / _chunkSize;
                var chunkColumn = column / _chunkSize;
                var chunkIndex = chunkRow * _chunkColumns + chunkColumn;
                var index = _indexes[chunkIndex];
                var data = GetChunk(chunkIndex, index);
                var localRow = row - chunkRow * _chunkSize;
                var localColumn = column - chunkColumn * _chunkSize;
                var stride = _valueSize * _channels;
                var offset = (localRow * index.Width + localColumn) * stride;
                var result = new byte[stride];
                Buffer.BlockCopy(data, offset, result, 0, stride);
                return result;
            }

            public byte ReadByte(int row, int column, int channel)
            {
                GetValueLocation(row, column, channel, out var data, out var offset);
                return data[offset];
            }

            public short ReadInt16(int row, int column, int channel)
            {
                GetValueLocation(row, column, channel, out var data, out var offset);
                return unchecked((short)(data[offset] | data[offset + 1] << 8));
            }

            public ushort ReadUInt16(int row, int column, int channel)
            {
                GetValueLocation(row, column, channel, out var data, out var offset);
                return unchecked((ushort)(data[offset] | data[offset + 1] << 8));
            }

            public void Dispose()
            {
                _reader.Dispose();
                _stream.Dispose();
            }

            private byte[] GetChunk(int key, ChunkIndex index)
            {
                if (_cache.TryGetValue(key, out var cached))
                {
                    return cached;
                }

                _stream.Position = index.Offset;
                var compressed = _reader.ReadBytes(index.CompressedLength);
                var raw = new byte[index.RawLength];
                using (var input = new MemoryStream(compressed, false))
                using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
                {
                    var read = 0;
                    while (read < raw.Length)
                    {
                        var count = deflate.Read(raw, read, raw.Length - read);
                        if (count == 0)
                        {
                            throw new InvalidDataException("Compressed chunk ended before its declared raw length.");
                        }

                        read += count;
                    }
                }

                if (_cache.Count >= MaximumCachedChunks)
                {
                    _cache.Remove(_cacheOrder.Dequeue());
                }

                _cache[key] = raw;
                _cacheOrder.Enqueue(key);
                return raw;
            }

            private void GetValueLocation(int row, int column, int channel, out byte[] data, out int offset)
            {
                if (row < 0 || row >= _rows || column < 0 || column >= _columns)
                    throw new ArgumentOutOfRangeException(nameof(row));
                if (channel < 0 || channel >= _channels)
                    throw new ArgumentOutOfRangeException(nameof(channel));
                var chunkRow = row / _chunkSize;
                var chunkColumn = column / _chunkSize;
                var chunkIndex = chunkRow * _chunkColumns + chunkColumn;
                var index = _indexes[chunkIndex];
                data = GetChunk(chunkIndex, index);
                var localRow = row - chunkRow * _chunkSize;
                var localColumn = column - chunkColumn * _chunkSize;
                offset = ((localRow * index.Width + localColumn) * _channels + channel) * _valueSize;
            }

            private readonly struct ChunkIndex
            {
                public ChunkIndex(long offset, int compressedLength, int rawLength, ushort height, ushort width)
                {
                    Offset = offset;
                    CompressedLength = compressedLength;
                    RawLength = rawLength;
                    Height = height;
                    Width = width;
                }

                public long Offset { get; }
                public int CompressedLength { get; }
                public int RawLength { get; }
                public ushort Height { get; }
                public ushort Width { get; }
            }
        }
    }
}
