using Engine;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System;

namespace Spacebox.Game.Generation
{
    public class LightManager
    {
        private const byte Size = Chunk.Size;
        private const int MinChannel = 3;
        private const float MaxPropagatedLevel = 14.9f;
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
            visited.Add(_chunk);

            foreach (var neighbor in _chunk.Neighbors.Values)
            {
                if (neighbor != null)
                {
                    visited.Add(neighbor);
                }
            }

            var chunkToIndex = new Dictionary<Chunk, int>();
            var indexToChunk = new List<Chunk>();

            foreach (var c in visited)
            {
                chunkToIndex[c] = indexToChunk.Count;
                indexToChunk.Add(c);
            }

            var lightQueue = new Queue<uint>(2048);
            var changedChunks = new HashSet<Chunk>();

            for (int i = 0; i < indexToChunk.Count; i++)
            {
                Chunk c = indexToChunk[i];
                bool chunkChanged = false;

                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        for (int z = 0; z < Size; z++)
                        {
                            var b = c.Blocks[x, y, z];
                            if (b.LightLevel > 0f)
                            {
                                if (b.LightLevel < 15f)
                                {
                                    b.LightLevel = 0f;
                                    b.LightColor = Color3Byte.Zero;
                                    c.Blocks[x, y, z] = b;
                                    chunkChanged = true;
                                }
                                else
                                {
                                    uint packed = (uint)((i << 15) | (x << 10) | (y << 5) | z);
                                    lightQueue.Enqueue(packed);
                                }
                            }
                        }
                    }
                }

                if (chunkChanged)
                {
                    changedChunks.Add(c);
                }
            }

            while (lightQueue.Count > 0)
            {
                uint packed = lightQueue.Dequeue();
                int z = (int)(packed & 0x1F);
                int y = (int)((packed >> 5) & 0x1F);
                int x = (int)((packed >> 10) & 0x1F);
                int cIndex = (int)(packed >> 15);

                Chunk chunk = indexToChunk[cIndex];
                var block = chunk.Blocks[x, y, z];
                var sourceColor = block.LightColor;

                int r = sourceColor.R * 8 / 10;
                int g = sourceColor.G * 8 / 10;
                int b_clr = sourceColor.B * 8 / 10;

                if (Math.Max(r, Math.Max(g, b_clr)) < MinChannel) continue;

                for (int i = 0; i < 6; i++)
                {
                    int nx = x + AdjacentOffsets[i].X;
                    int ny = y + AdjacentOffsets[i].Y;
                    int nz = z + AdjacentOffsets[i].Z;

                    Chunk nChunk = chunk;
                    int bx = nx, by = ny, bz = nz;

                    if (nx < 0 || nx >= Size || ny < 0 || ny >= Size || nz < 0 || nz >= Size)
                    {
                        GetNeighborCoord(chunk, nx, ny, nz, out nChunk, out bx, out by, out bz);
                    }

                    if (nChunk == null) continue;

                    var nb = nChunk.Blocks[bx, by, bz];
                    if (!(nb.IsAir || nb.IsTransparent)) continue;
                    if (nb.LightLevel >= 15f) continue;

                    var current = nb.LightColor;
                    int nr = Math.Max(current.R, r);
                    int ng = Math.Max(current.G, g);
                    int nbl = Math.Max(current.B, b_clr);

                    if (nr == current.R && ng == current.G && nbl == current.B) continue;

                    nb.LightColor = new Color3Byte((byte)nr, (byte)ng, (byte)nbl);
                    nb.LightLevel = MathF.Min(MaxPropagatedLevel, Math.Max(nr, Math.Max(ng, nbl)) / 255f * 15f);
                    nChunk.Blocks[bx, by, bz] = nb;
                    changedChunks.Add(nChunk);

                    if (!chunkToIndex.TryGetValue(nChunk, out int nIndex))
                    {
                        nIndex = indexToChunk.Count;
                        chunkToIndex[nChunk] = nIndex;
                        indexToChunk.Add(nChunk);
                    }

                    uint nPacked = (uint)((nIndex << 15) | (bx << 10) | (by << 5) | bz);
                    lightQueue.Enqueue(nPacked);
                }
            }

            changedChunks.Remove(_chunk);

            foreach (var c in changedChunks)
            {
                c.QueueMeshUpdate(UpdateReason.Lighting);
            }
        }

        private void GetNeighborCoord(Chunk c, int nx, int ny, int nz, out Chunk nChunk, out int bx, out int by, out int bz)
        {
            sbyte ox = 0, oy = 0, oz = 0;
            bx = nx; by = ny; bz = nz;

            if (nx < 0) { ox = -1; bx += Size; }
            else if (nx >= Size) { ox = 1; bx -= Size; }

            if (ny < 0) { oy = -1; by += Size; }
            else if (ny >= Size) { oy = 1; by -= Size; }

            if (nz < 0) { oz = -1; bz += Size; }
            else if (nz >= Size) { oz = 1; bz -= Size; }

            var off = new Vector3SByte(ox, oy, oz);
            c.Neighbors.TryGetValue(off, out nChunk);
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