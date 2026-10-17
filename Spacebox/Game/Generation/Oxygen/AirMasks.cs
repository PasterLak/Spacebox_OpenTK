namespace Spacebox.Game.Generation.Oxygen;

public sealed class AirMasks
{
    private const int WordsPerChunk = Chunk.Size * Chunk.Size * Chunk.Size / 64;

    private readonly Dictionary<int, ulong[]> _masks = new Dictionary<int, ulong[]>();
    private readonly Stack<ulong[]> _pool = new Stack<ulong[]>();

    public bool Contains(int x, int y, int z)
    {
        if (!AirGrid.IsInsideEntity(x, y, z)) return false;
        if (!_masks.TryGetValue(AirGrid.ChunkKey(x, y, z), out var mask)) return false;

        int bit = AirGrid.CellIndex(x, y, z);
        return (mask[bit >> 6] & (1UL << (bit & 63))) != 0;
    }

    public void Add(int x, int y, int z)
    {
        int key = AirGrid.ChunkKey(x, y, z);
        if (!_masks.TryGetValue(key, out var mask))
        {
            mask = _pool.Count > 0 ? _pool.Pop() : new ulong[WordsPerChunk];
            _masks.Add(key, mask);
        }

        int bit = AirGrid.CellIndex(x, y, z);
        mask[bit >> 6] |= 1UL << (bit & 63);
    }

    public void Clear()
    {
        foreach (var mask in _masks.Values)
        {
            Array.Clear(mask);
            _pool.Push(mask);
        }
        _masks.Clear();
    }
}
