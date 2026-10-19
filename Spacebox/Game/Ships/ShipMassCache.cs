using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Ships;

public sealed class ShipMassCache
{
    private readonly Dictionary<Chunk, Entry> _entries = new Dictionary<Chunk, Entry>();
    private readonly List<Chunk> _stale = new List<Chunk>();
    private int _stamp;

    public ShipMassProperties Compute(IReadOnlyList<Chunk> chunks)
    {
        _stamp++;
        var total = new ShipMassProperties();

        for (int c = 0; c < chunks.Count; c++)
        {
            var chunk = chunks[c];
            if (chunk.IsDisposed) continue;

            if (!_entries.TryGetValue(chunk, out var entry) || entry.Version != chunk.DataVersion)
                entry = new Entry { Version = chunk.DataVersion, Properties = Measure(chunk) };

            entry.Stamp = _stamp;
            _entries[chunk] = entry;
            total.Merge(entry.Properties);
        }

        RemoveStale();
        return total;
    }

    private void RemoveStale()
    {
        _stale.Clear();
        foreach (var pair in _entries)
        {
            if (pair.Value.Stamp != _stamp) _stale.Add(pair.Key);
        }

        for (int i = 0; i < _stale.Count; i++)
            _entries.Remove(_stale[i]);
    }

    private static ShipMassProperties Measure(Chunk chunk)
    {
        var properties = new ShipMassProperties();

        var origin = new Vector3(chunk.PositionIndex.X, chunk.PositionIndex.Y, chunk.PositionIndex.Z) * Chunk.Size;
        var blocks = chunk.Blocks;
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        for (int z = 0; z < Chunk.Size; z++)
        {
            var block = blocks[x, y, z];
            if (block == null || block.IsAir) continue;
            properties.Add(origin + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), Math.Max((byte)1, block.Mass));
        }

        return properties;
    }

    private struct Entry
    {
        public int Version;
        public int Stamp;
        public ShipMassProperties Properties;
    }
}
