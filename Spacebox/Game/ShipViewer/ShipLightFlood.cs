using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.ShipViewer;

public static class ShipLightFlood
{
    private const int Size = Chunk.Size;

    private static readonly (int X, int Y, int Z)[] Offsets =
    {
        (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)
    };

    public static void Propagate(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks)
    {
        var queue = new Queue<(Block[,,] Blocks, Vector3SByte Chunk, int X, int Y, int Z)>();
        foreach (var pair in chunks)
        {
            var blocks = pair.Value;
            for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
            for (int z = 0; z < Size; z++)
            {
                var block = blocks[x, y, z];
                if (block != null && block.LightLevel >= LightFalloff.SourceLevel) queue.Enqueue((blocks, pair.Key, x, y, z));
            }
        }

        while (queue.Count > 0)
        {
            var (blocks, chunk, x, y, z) = queue.Dequeue();
            if (!LightFalloff.TryDecay(blocks[x, y, z].LightColor, out int r, out int g, out int b)) continue;

            for (int i = 0; i < Offsets.Length; i++)
            {
                if (!TryResolve(chunks, blocks, chunk, x + Offsets[i].X, y + Offsets[i].Y, z + Offsets[i].Z, out var target)) continue;

                var neighbor = target.Blocks[target.X, target.Y, target.Z];
                if (neighbor != null && LightFalloff.TrySpreadInto(neighbor, r, g, b)) queue.Enqueue(target);
            }
        }
    }

    private static bool TryResolve(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Block[,,] blocks, Vector3SByte chunk, int x, int y, int z,
        out (Block[,,] Blocks, Vector3SByte Chunk, int X, int Y, int Z) target)
    {
        int ox = x < 0 ? -1 : x >= Size ? 1 : 0;
        int oy = y < 0 ? -1 : y >= Size ? 1 : 0;
        int oz = z < 0 ? -1 : z >= Size ? 1 : 0;

        if (ox == 0 && oy == 0 && oz == 0)
        {
            target = (blocks, chunk, x, y, z);
            return true;
        }

        var neighborChunk = new Vector3SByte((sbyte)(chunk.X + ox), (sbyte)(chunk.Y + oy), (sbyte)(chunk.Z + oz));
        if (!chunks.TryGetValue(neighborChunk, out var neighborBlocks))
        {
            target = default;
            return false;
        }

        target = (neighborBlocks, neighborChunk, x - ox * Size, y - oy * Size, z - oz * Size);
        return true;
    }
}
