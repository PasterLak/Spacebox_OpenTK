using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Oxygen;

public sealed class AirFloodFill
{
    private const int Radius = OxygenGeneratorBlock.Radius;
    private const int RadiusSquared = Radius * Radius;
    private const int Side = Radius * 2 + 1;
    private const int BoxCells = Side * Side * Side;

    private static readonly int[] _dx = { 1, -1, 0, 0, 0, 0 };
    private static readonly int[] _dy = { 0, 0, 1, -1, 0, 0 };
    private static readonly int[] _dz = { 0, 0, 0, 0, 1, -1 };

    private readonly ulong[] _visited = new ulong[(BoxCells + 63) / 64];
    private readonly int[] _queue = new int[BoxCells];
    private readonly Chunk[] _chunks = new Chunk[AirGrid.ChunkSlots];

    public void BeginPass(List<Chunk> chunks)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            _chunks[AirGrid.ChunkKey(chunks[i].PositionIndex)] = chunks[i];
        }
    }

    public void EndPass(List<Chunk> chunks)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            _chunks[AirGrid.ChunkKey(chunks[i].PositionIndex)] = null;
        }
    }

    public bool TryGetBlock(int x, int y, int z, out Block block)
    {
        block = null;
        if (!AirGrid.IsInsideEntity(x, y, z)) return false;

        var chunk = _chunks[AirGrid.ChunkKey(x, y, z)];
        if (chunk == null || chunk.IsDisposed) return false;

        block = chunk.Blocks[x & 31, y & 31, z & 31];
        return true;
    }

    public bool FillFrom(int gx, int gy, int gz, AirMasks reached, AirMasks oxygen)
    {
        bool hasAir = false;
        bool sealedRoom = false;

        for (int d = 0; d < 6; d++)
        {
            int sx = gx + _dx[d];
            int sy = gy + _dy[d];
            int sz = gz + _dz[d];

            if (reached.Contains(sx, sy, sz))
            {
                hasAir = true;
                sealedRoom |= oxygen.Contains(sx, sy, sz);
                continue;
            }

            if (!TryGetBlock(sx, sy, sz, out var start) || !IsPassable(start)) continue;

            hasAir = true;
            sealedRoom |= Flood(gx, gy, gz, sx - gx + Radius, sy - gy + Radius, sz - gz + Radius, reached, oxygen);
        }

        return !hasAir || sealedRoom;
    }

    private bool Flood(int gx, int gy, int gz, int startX, int startY, int startZ, AirMasks reached, AirMasks oxygen)
    {
        int count = 0;
        int head = 0;
        bool leak = false;

        int startIndex = Encode(startX, startY, startZ);
        MarkVisited(startIndex);
        _queue[count++] = startIndex;

        while (head < count && !leak)
        {
            Decode(_queue[head++], out int bx, out int by, out int bz);

            for (int d = 0; d < 6; d++)
            {
                int dx = bx + _dx[d] - Radius;
                int dy = by + _dy[d] - Radius;
                int dz = bz + _dz[d] - Radius;

                if (dx * dx + dy * dy + dz * dz > RadiusSquared)
                {
                    leak = true;
                    break;
                }

                int index = Encode(dx + Radius, dy + Radius, dz + Radius);
                if (IsVisited(index)) continue;

                if (!TryGetBlock(gx + dx, gy + dy, gz + dz, out var block))
                {
                    leak = true;
                    break;
                }

                if (!IsPassable(block)) continue;

                MarkVisited(index);
                _queue[count++] = index;
            }
        }

        for (int i = 0; i < count; i++)
        {
            int index = _queue[i];
            Decode(index, out int bx, out int by, out int bz);

            int x = gx + bx - Radius;
            int y = gy + by - Radius;
            int z = gz + bz - Radius;

            reached.Add(x, y, z);
            if (!leak) oxygen.Add(x, y, z);

            _visited[index >> 6] &= ~(1UL << (index & 63));
        }

        return !leak;
    }

    private static bool IsPassable(Block block)
    {
        return block == null || block.IsAir;
    }

    private static int Encode(int x, int y, int z)
    {
        return (x * Side + y) * Side + z;
    }

    private static void Decode(int index, out int x, out int y, out int z)
    {
        z = index % Side;
        index /= Side;
        y = index % Side;
        x = index / Side;
    }

    private bool IsVisited(int index)
    {
        return (_visited[index >> 6] & (1UL << (index & 63))) != 0;
    }

    private void MarkVisited(int index)
    {
        _visited[index >> 6] |= 1UL << (index & 63);
    }
}
