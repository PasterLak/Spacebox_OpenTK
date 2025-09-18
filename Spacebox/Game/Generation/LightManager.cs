
using Engine;

namespace Spacebox.Game.Generation
{
    public class LightManager
    {
        private const byte Size = Chunk.Size;
        public static bool EnableLighting = true;
        private readonly Chunk _chunk;

        public LightManager(Chunk chunk)
        {
            _chunk = chunk;
        }

        public void PropagateLight()
        {
            if (!EnableLighting) return;

            var visited = new HashSet<Chunk>();
            var chunksQueue = new Queue<Chunk>();
            chunksQueue.Enqueue(_chunk);
            visited.Add(_chunk);

            while (chunksQueue.Count > 0)
            {
                var c = chunksQueue.Dequeue();
                foreach (var neighbor in c.Neighbors.Values)
                {
                    if (neighbor != null && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        chunksQueue.Enqueue(neighbor);
                    }
                }
            }

            var allChunks = new List<Chunk>(visited);

            foreach (var c in allChunks)
                ResetLightLevels(c);

            var lightQueue = new Queue<(Chunk, byte, byte, byte)>();

            foreach (var c in allChunks)
                EnqueueLightSources(c, lightQueue);

            var changedChunks = new HashSet<Chunk>();

            while (lightQueue.Count > 0)
            {
                var (chunk, x, y, z) = lightQueue.Dequeue();
                var block = chunk.Blocks[x, y, z];
                var lvl = block.LightLevel;
                if (lvl <= 0.1f) continue;

                for (int i = 0; i < 6; i++)
                {
                    var nx = x + AdjacentOffsets[i].X;
                    var ny = y + AdjacentOffsets[i].Y;
                    var nz = z + AdjacentOffsets[i].Z;
                    var (nChunk, bx, by, bz) = GetBlockInNeighborChunk(chunk, nx, ny, nz);
                    if (nChunk == null) continue;

                    var nb = nChunk.Blocks[bx, by, bz];
                    if (!(nb.IsAir || nb.IsTransparent)) continue;

                    const float att = 0.8f;
                    var newLvl = lvl * att;
                    var newClr = block.LightColor.ToVector3() * att;

                    if (newLvl > nb.LightLevel + 0.01f)
                    {
                        nb.LightLevel = newLvl;
                        nb.LightColor = new Color3Byte(newClr );
                        nChunk.Blocks[bx, by, bz] = nb;
                        changedChunks.Add(nChunk);
                        lightQueue.Enqueue((nChunk, bx, by, bz));
                    }
                    else if (System.MathF.Abs(newLvl - nb.LightLevel) < 0.01f)
                    {
                        nb.LightColor = new Color3Byte((nb.LightColor.ToVector3() + newClr) * 0.5f );
                        nChunk.Blocks[bx, by, bz] = nb;
                        changedChunks.Add(nChunk);
                    }
                }
            }



            changedChunks.Remove(_chunk);

            foreach (var c in changedChunks)
            {
                //c.GenerateMesh(false);
                c.NeedsToRegenerateMesh = true;
            }
        }

        private void ResetLightLevels(Chunk c)
        {
            for (byte x = 0; x < Size; x++)
            {
                for (byte y = 0; y < Size; y++)
                {
                    for (byte z = 0; z < Size; z++)
                    {
                        var b = c.Blocks[x, y, z];
                        if (b.LightLevel < 15f)
                        {
                            b.LightLevel = 0f;
                            b.LightColor = Color3Byte.Zero;
                            c.Blocks[x, y, z] = b;
                        }
                    }
                }
            }
        }

        private void EnqueueLightSources(Chunk c, Queue<(Chunk, byte, byte, byte)> q)
        {
            for (byte x = 0; x < Size; x++)
            {
                for (byte y = 0; y < Size; y++)
                {
                    for (byte z = 0; z < Size; z++)
                    {
                        if (c.Blocks[x, y, z].LightLevel > 0f)
                        {
                            q.Enqueue((c, x, y, z));
                        }
                    }
                }
            }
        }

        private (Chunk chunk, byte x, byte y, byte z) GetBlockInNeighborChunk(Chunk c, int nx, int ny, int nz)
        {
            if (nx >= 0 && nx < Size && ny >= 0 && ny < Size && nz >= 0 && nz < Size)
            {
                return (c, (byte)nx, (byte)ny, (byte)nz);
            }

            sbyte ox = 0, oy = 0, oz = 0;
            int lx = nx, ly = ny, lz = nz;
            if (lx < 0) { ox = -1; lx += Size; }
            else if (lx >= Size) { ox = 1; lx -= Size; }
            if (ly < 0) { oy = -1; ly += Size; }
            else if (ly >= Size) { oy = 1; ly -= Size; }
            if (lz < 0) { oz = -1; lz += Size; }
            else if (lz >= Size) { oz = 1; lz -= Size; }

            var off = new Vector3SByte(ox, oy, oz);
            if (c.Neighbors.TryGetValue(off, out var n) && n != null)
            {
                return (n, (byte)lx, (byte)ly, (byte)lz);
            }

            return (null, 0, 0, 0);
        }

        private static readonly Vector3SByte[] AdjacentOffsets =
        {
            new Vector3SByte( 1,  0,  0),
            new Vector3SByte(-1,  0,  0),
            new Vector3SByte( 0,  1,  0),
            new Vector3SByte( 0, -1,  0),
            new Vector3SByte( 0,  0,  1),
            new Vector3SByte( 0,  0, -1),
        };
    }
}
