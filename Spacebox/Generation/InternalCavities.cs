
namespace Spacebox.Generation
{
    public interface IInternalCavitiesRemover
    {
        void RemoveInternalCavities(ref bool[,,] data);
    }
    public static class InternalCavitiesUnity
    {
        public static void RemoveInternalCavities(ref bool[,,] data)
        {
            int sx = data.GetLength(0), sy = data.GetLength(1), sz = data.GetLength(2);
            bool[,,] mask = new bool[sx + 2, sy + 2, sz + 2];
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                        mask[x + 1, y + 1, z + 1] = data[x, y, z];
            int px = sx + 2, py = sy + 2, pz = sz + 2;
            Queue<(int, int, int)> queue = new Queue<(int, int, int)>();
            for (int x = 0; x < px; x++)
                for (int y = 0; y < py; y++)
                {
                    if (!mask[x, y, 0]) { mask[x, y, 0] = true; queue.Enqueue((x, y, 0)); }
                    if (!mask[x, y, pz - 1]) { mask[x, y, pz - 1] = true; queue.Enqueue((x, y, pz - 1)); }
                }
            for (int x = 0; x < px; x++)
                for (int z = 0; z < pz; z++)
                {
                    if (!mask[x, 0, z]) { mask[x, 0, z] = true; queue.Enqueue((x, 0, z)); }
                    if (!mask[x, py - 1, z]) { mask[x, py - 1, z] = true; queue.Enqueue((x, py - 1, z)); }
                }
            for (int y = 0; y < py; y++)
                for (int z = 0; z < pz; z++)
                {
                    if (!mask[0, y, z]) { mask[0, y, z] = true; queue.Enqueue((0, y, z)); }
                    if (!mask[px - 1, y, z]) { mask[px - 1, y, z] = true; queue.Enqueue((px - 1, y, z)); }
                }
            int[] dx = { 1, -1, 0, 0, 0, 0 };
            int[] dy = { 0, 0, 1, -1, 0, 0 };
            int[] dz = { 0, 0, 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                var (cx, cy, cz) = queue.Dequeue();
                for (int i = 0; i < 6; i++)
                {
                    int nx = cx + dx[i], ny = cy + dy[i], nz = cz + dz[i];
                    if (nx >= 0 && nx < px && ny >= 0 && ny < py && nz >= 0 && nz < pz && !mask[nx, ny, nz])
                    {
                        mask[nx, ny, nz] = true;
                        queue.Enqueue((nx, ny, nz));
                    }
                }
            }
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                        if (!mask[x + 1, y + 1, z + 1])
                            data[x, y, z] = true;
        }
    }


    public static class InternalCavitiesBits
    {

        public static void FillInternalCavities(ref bool[,,] volume)
        {
            int width = volume.GetLength(0);
            int height = volume.GetLength(1);
            int depth = volume.GetLength(2);
            uint[,] occupancy = new uint[depth, height];
            uint[,] empties = new uint[depth, height];
            uint[,] accessible = new uint[depth, height];
            uint maskAll = width == 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    uint bits = 0u;
                    for (int x = 0; x < width; x++)
                    {
                        if (volume[x, y, z])
                        {
                            bits |= (1u << x);
                        }
                    }
                    occupancy[z, y] = bits;
                    empties[z, y] = (~bits) & maskAll;
                }
            }
            uint maskX0 = 1u;
            uint maskXn = 1u << (width - 1);
            uint edgeMask = maskX0 | maskXn;
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    uint e = empties[z, y];
                    accessible[z, y] = e & edgeMask;
                }
            }
            for (int z = 0; z < depth; z++)
            {
                accessible[z, 0] |= empties[z, 0];
                accessible[z, height - 1] |= empties[z, height - 1];
            }
            for (int y = 0; y < height; y++)
            {
                accessible[0, y] |= empties[0, y];
                accessible[depth - 1, y] |= empties[depth - 1, y];
            }
            bool changed;
            do
            {
                changed = false;
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        uint curAcc = accessible[z, y];
                        if (curAcc == 0u) continue;
                        uint exp = curAcc;
                        uint newExp;
                        uint empt = empties[z, y];
                        do
                        {
                            newExp = exp | (((exp << 1) | (exp >> 1)) & empt);
                            if (newExp == exp) break;
                            exp = newExp;
                        } while (true);
                        if (exp != curAcc)
                        {
                            accessible[z, y] = exp;
                            curAcc = exp;
                            changed = true;
                        }
                        if (y > 0)
                        {
                            uint newAccYminus = curAcc & empties[z, y - 1] & ~accessible[z, y - 1];
                            if (newAccYminus != 0u)
                            {
                                accessible[z, y - 1] |= newAccYminus;
                                changed = true;
                            }
                        }
                        if (y < height - 1)
                        {
                            uint newAccYplus = curAcc & empties[z, y + 1] & ~accessible[z, y + 1];
                            if (newAccYplus != 0u)
                            {
                                accessible[z, y + 1] |= newAccYplus;
                                changed = true;
                            }
                        }
                        if (z > 0)
                        {
                            uint newAccZminus = curAcc & empties[z - 1, y] & ~accessible[z - 1, y];
                            if (newAccZminus != 0u)
                            {
                                accessible[z - 1, y] |= newAccZminus;
                                changed = true;
                            }
                        }
                        if (z < depth - 1)
                        {
                            uint newAccZplus = curAcc & empties[z + 1, y] & ~accessible[z + 1, y];
                            if (newAccZplus != 0u)
                            {
                                accessible[z + 1, y] |= newAccZplus;
                                changed = true;
                            }
                        }
                    }
                }
            } while (changed);
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    uint internalMask = empties[z, y] & ~accessible[z, y];
                    if (internalMask != 0u)
                    {
                        occupancy[z, y] |= internalMask;
                    }
                    uint occ = occupancy[z, y];
                    for (int x = 0; x < width; x++)
                    {
                        volume[x, y, z] = ((occ >> x) & 1u) != 0u;
                    }
                }
            }
        }

    }
}
