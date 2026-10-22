using Engine;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System;

namespace Spacebox.Game.Generation
{
    public class LightManager
    {
        private const byte Size = Chunk.Size;
        private static readonly int Reach = ComputeReach();
        public static bool EnableLighting = true;
        private readonly Chunk _chunk;

        public LightManager(Chunk chunk)
        {
            _chunk = chunk;
        }

        private void ClearPropagatedLight()
        {
            var blocks = _chunk.Blocks;
            for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
            for (int z = 0; z < Size; z++)
            {
                var block = blocks[x, y, z];
                if (block == null || block.LightLevel <= 0f || block.LightLevel >= 15f) continue;
                block.LightLevel = 0f;
                block.LightColor = Color3Byte.Zero;
            }
        }

        public void PropagateLight()
        {
            if (!EnableLighting)
            {
                ClearPropagatedLight();
                return;
            }

            var scratch = LightPropagationScratch.Pool.Rent();
            try
            {
                PropagateLight(scratch);
            }
            finally
            {
                LightPropagationScratch.Pool.Return(scratch);
            }
        }

        private void PropagateLight(LightPropagationScratch scratch)
        {
            CollectRegionChunks(scratch.RegionChunks);
            ClearRegionAndEnqueueSources(scratch);
            EnqueueRegionBorder(scratch);
            SpreadLight(scratch);

            var changedChunks = scratch.ChangedChunks;
            changedChunks.Remove(_chunk);

            foreach (var c in changedChunks)
            {
                c.QueueMeshUpdate(UpdateReason.Lighting);
            }
        }

        private void CollectRegionChunks(Chunk[] region)
        {
            for (int ox = -1; ox <= 1; ox++)
            for (int oy = -1; oy <= 1; oy++)
            for (int oz = -1; oz <= 1; oz++)
            {
                region[RegionSlot(ox, oy, oz)] = FindRelativeChunk(_chunk, ox, oy, oz);
            }
        }

        private static void ClearRegionAndEnqueueSources(LightPropagationScratch scratch)
        {
            var region = scratch.RegionChunks;

            for (int ox = -1; ox <= 1; ox++)
            for (int oy = -1; oy <= 1; oy++)
            for (int oz = -1; oz <= 1; oz++)
            {
                var c = region[RegionSlot(ox, oy, oz)];
                if (c == null) continue;

                int index = GetChunkIndex(scratch, c);
                GetRegionRange(ox, out int x0, out int x1);
                GetRegionRange(oy, out int y0, out int y1);
                GetRegionRange(oz, out int z0, out int z1);
                bool chunkChanged = false;

                for (int x = x0; x < x1; x++)
                for (int y = y0; y < y1; y++)
                for (int z = z0; z < z1; z++)
                {
                    var b = c.Blocks[x, y, z];
                    if (b.LightLevel <= 0f) continue;

                    if (b.LightLevel < 15f)
                    {
                        b.LightLevel = 0f;
                        b.LightColor = Color3Byte.Zero;
                        chunkChanged = true;
                    }
                    else
                    {
                        scratch.LightQueue.Enqueue(Pack(index, x, y, z));
                    }
                }

                if (chunkChanged)
                {
                    scratch.ChangedChunks.Add(c);
                }
            }
        }

        private static void EnqueueRegionBorder(LightPropagationScratch scratch)
        {
            int low = -Reach - 1;
            int high = Size + Reach;

            for (int u = -Reach; u < high; u++)
            for (int v = -Reach; v < high; v++)
            {
                EnqueueLitCell(scratch, low, u, v);
                EnqueueLitCell(scratch, high, u, v);
                EnqueueLitCell(scratch, u, low, v);
                EnqueueLitCell(scratch, u, high, v);
                EnqueueLitCell(scratch, u, v, low);
                EnqueueLitCell(scratch, u, v, high);
            }
        }

        private static void EnqueueLitCell(LightPropagationScratch scratch, int x, int y, int z)
        {
            int ox = x < 0 ? -1 : x >= Size ? 1 : 0;
            int oy = y < 0 ? -1 : y >= Size ? 1 : 0;
            int oz = z < 0 ? -1 : z >= Size ? 1 : 0;

            var c = scratch.RegionChunks[RegionSlot(ox, oy, oz)];
            if (c == null) return;

            int lx = x - ox * Size;
            int ly = y - oy * Size;
            int lz = z - oz * Size;

            if (c.Blocks[lx, ly, lz].LightLevel <= 0f) return;

            scratch.LightQueue.Enqueue(Pack(GetChunkIndex(scratch, c), lx, ly, lz));
        }

        private void SpreadLight(LightPropagationScratch scratch)
        {
            var lightQueue = scratch.LightQueue;
            var indexToChunk = scratch.IndexToChunk;
            var changedChunks = scratch.ChangedChunks;

            while (lightQueue.Count > 0)
            {
                uint packed = lightQueue.Dequeue();
                int z = (int)(packed & 0x1F);
                int y = (int)((packed >> 5) & 0x1F);
                int x = (int)((packed >> 10) & 0x1F);
                int cIndex = (int)(packed >> 15);

                Chunk chunk = indexToChunk[cIndex];
                var block = chunk.Blocks[x, y, z];
                if (!LightFalloff.TryDecay(block.LightColor, out int r, out int g, out int b_clr)) continue;

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

                    if (!LightFalloff.TrySpreadInto(nChunk.Blocks[bx, by, bz], r, g, b_clr)) continue;
                    changedChunks.Add(nChunk);

                    lightQueue.Enqueue(Pack(GetChunkIndex(scratch, nChunk), bx, by, bz));
                }
            }
        }

        private static int GetChunkIndex(LightPropagationScratch scratch, Chunk c)
        {
            if (scratch.ChunkToIndex.TryGetValue(c, out int index)) return index;

            index = scratch.IndexToChunk.Count;
            scratch.ChunkToIndex[c] = index;
            scratch.IndexToChunk.Add(c);
            return index;
        }

        private static uint Pack(int chunkIndex, int x, int y, int z)
        {
            return (uint)((chunkIndex << 15) | (x << 10) | (y << 5) | z);
        }

        private static int RegionSlot(int ox, int oy, int oz)
        {
            return (ox + 1) * 9 + (oy + 1) * 3 + (oz + 1);
        }

        private static void GetRegionRange(int offset, out int start, out int end)
        {
            if (offset < 0)
            {
                start = Size - Reach;
                end = Size;
            }
            else if (offset > 0)
            {
                start = 0;
                end = Reach;
            }
            else
            {
                start = 0;
                end = Size;
            }
        }

        private static Chunk FindRelativeChunk(Chunk from, int ox, int oy, int oz)
        {
            if (from == null) return null;
            if (ox == 0 && oy == 0 && oz == 0) return from;

            Chunk found = null;
            if (ox != 0) found = FindRelativeChunk(Step(from, ox, 0, 0), 0, oy, oz);
            if (found == null && oy != 0) found = FindRelativeChunk(Step(from, 0, oy, 0), ox, 0, oz);
            if (found == null && oz != 0) found = FindRelativeChunk(Step(from, 0, 0, oz), ox, oy, 0);
            return found;
        }

        private static Chunk Step(Chunk from, int dx, int dy, int dz)
        {
            from.Neighbors.TryGetValue(new Vector3SByte((sbyte)dx, (sbyte)dy, (sbyte)dz), out var next);
            return next != null && !next.IsDisposed ? next : null;
        }

        private static int ComputeReach()
        {
            int value = byte.MaxValue;
            int distance = 0;

            while (value * 8 / 10 >= LightFalloff.MinChannel)
            {
                value = value * 8 / 10;
                distance++;
            }

            return distance;
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