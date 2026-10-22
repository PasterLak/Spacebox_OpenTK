using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.ShipViewer;

public static class ShipChunkPadding
{
    public static Block[,,] Build(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Vector3SByte index, Block air)
    {
        var neighbors = new Block[27][,,];
        for (int ox = -1; ox <= 1; ox++)
        for (int oy = -1; oy <= 1; oy++)
        for (int oz = -1; oz <= 1; oz++)
            neighbors[(ox + 1) * 9 + (oy + 1) * 3 + oz + 1] = Neighbor(chunks, index, ox, oy, oz);

        var padded = PaddedArrayPools.Blocks.Rent();
        const int size = ShipTagReader.ChunkSize;
        const int last = size - 1;

        for (int x = -1; x <= size; x++)
        {
            int cx = x < 0 ? 0 : x > last ? 2 : 1;
            int lx = x - (cx - 1) * size;
            for (int y = -1; y <= size; y++)
            {
                int cy = y < 0 ? 0 : y > last ? 2 : 1;
                int ly = y - (cy - 1) * size;
                for (int z = -1; z <= size; z++)
                {
                    int cz = z < 0 ? 0 : z > last ? 2 : 1;
                    int lz = z - (cz - 1) * size;
                    var source = neighbors[cx * 9 + cy * 3 + cz];
                    padded[x + 1, y + 1, z + 1] = source?[lx, ly, lz] ?? air;
                }
            }
        }

        return padded;
    }

    private static Block[,,] Neighbor(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Vector3SByte index, int ox, int oy, int oz)
    {
        int nx = index.X + ox;
        int ny = index.Y + oy;
        int nz = index.Z + oz;
        if (nx < sbyte.MinValue || nx > sbyte.MaxValue || ny < sbyte.MinValue || ny > sbyte.MaxValue || nz < sbyte.MinValue || nz > sbyte.MaxValue) return null;

        return chunks.TryGetValue(new Vector3SByte((sbyte)nx, (sbyte)ny, (sbyte)nz), out var blocks) ? blocks : null;
    }
}
