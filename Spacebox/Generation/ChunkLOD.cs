
using Engine;
using OpenTK.Mathematics;



namespace Spacebox.Game.Generation
{
    public struct MeshData
    {
        public float[] Vertices { get; }
        public uint[] Indices { get; }
        public MeshData(float[] vertices, uint[] indices)
        {
            Vertices = vertices;
            Indices = indices;
        }
    }

    public class ChunkLODMeshGenerator
    {
        public static bool DebugQuadsCount = false;
        public static bool[,,] ConvertBlocksToBool(Block[,,] blocks)
        {
            int sx = blocks.GetLength(0), sy = blocks.GetLength(1), sz = blocks.GetLength(2);
            var data = new bool[sx, sy, sz];
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                        data[x, y, z] = !blocks[x, y, z].IsAir;
            return data;
        }

        public Task<MeshData> GenerateFromBlocksAsync(Block[,,] blocks, int downscale, Vector2[] customUV = null)
        {
            bool[,,] fullData = ConvertBlocksToBool(blocks);
            return Task.Run(() =>
            {
                bool[,,] lodData = (downscale > 1) ? CreateDownscaledData(fullData, downscale) : fullData;

                RemoveInternalCavities(ref lodData);
                return GenerateGreedyMesh(lodData, downscale, customUV);
            });
        }

        private static bool[,,] CreateDownscaledData(bool[,,] data, int downscale)
        {
            int orig = data.GetLength(0);
            int res = orig / downscale;
            var result = new bool[res, res, res];
            int cellVolume = downscale * downscale * downscale;
            int threshold = cellVolume / 2;
            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    for (int z = 0; z < res; z++)
                    {
                        int count = 0;
                        for (int i = 0; i < downscale; i++)
                        {
                            for (int j = 0; j < downscale; j++)
                            {
                                for (int k = 0; k < downscale; k++)
                                {
                                    if (data[x * downscale + i, y * downscale + j, z * downscale + k])
                                        count++;
                                }
                            }
                        }
                        result[x, y, z] = count >= threshold;
                    }
                }
            }
            return result;
        }

        private static MeshData GenerateGreedyMesh(bool[,,] data, int downscale, Vector2[] customUV)
        {
            int nx = data.GetLength(0), ny = data.GetLength(1), nz = data.GetLength(2);
            float scale = downscale;
            var vertices = new List<float>();
            var indices = new List<uint>();
            uint vertCount = 0;
            int[] dims = { nx, ny, nz };
            int quad = 0;
            for (int d = 0; d < 3; d++)
            {
                int u, v;
                if (d == 0) { u = 1; v = 2; }
                else if (d == 1) { u = 0; v = 2; }
                else { u = 0; v = 1; }
                int dimD = dims[d], dimU = dims[u], dimV = dims[v];
                for (int slice = 0; slice <= dimD; slice++)
                {
                    int[,] mask = CreateMask(data, d, slice, dimD, dimU, dimV, u, v);
                    bool[,] used = new bool[dimU, dimV];
                    for (int i = 0; i < dimU; i++)
                    {
                        for (int j = 0; j < dimV; j++)
                        {
                            int c = mask[i, j];
                            if (c != 0 && !used[i, j])
                            {
                                int width = ComputeWidth(mask, used, i, j, dimV);
                                int height = ComputeHeight(mask, used, i, j, width, dimU, dimV);
                                MarkUsed(used, i, j, width, height);
                                if (d == 1)
                                {
                                    c = -c;
                                }
                                (Vector3 quadOrigin, Vector3 duVec, Vector3 dvVec) = ComputeQuadVectors(d, i, j, slice, scale, height, width);
                                Vector3 normal = d switch
                                {
                                    0 => new Vector3(c, 0, 0),
                                    1 => new Vector3(0, c, 0),
                                    _ => new Vector3(0, 0, c)
                                };
                                AddQuad(vertices, indices, ref vertCount, quadOrigin, duVec, dvVec, normal, c, customUV);
                                if (DebugQuadsCount)
                                    quad++;
                            }
                        }
                    }
                }
            }
            if (DebugQuadsCount)
                Debug.Log("Quads " + quad);
            return new MeshData(vertices.ToArray(), indices.ToArray());
        }

        private static int[,] CreateMask(bool[,,] data, int d, int slice, int dimD, int dimU, int dimV, int u, int v)
        {
            int[,] mask = new int[dimU, dimV];
            for (int i = 0; i < dimU; i++)
            {
                for (int j = 0; j < dimV; j++)
                {
                    bool voxelA = (slice < dimD) ? GetVoxel(data, d, slice, i, j, u, v) : false;
                    bool voxelB = (slice > 0) ? GetVoxel(data, d, slice - 1, i, j, u, v) : false;
                    mask[i, j] = (voxelA != voxelB) ? (voxelA ? 1 : -1) : 0;
                }
            }
            return mask;
        }

        private static bool GetVoxel(bool[,,] data, int d, int slice, int i, int j, int u, int v)
        {
            return d switch
            {
                0 => data[slice, i, j],
                1 => data[i, slice, j],
                _ => data[i, j, slice]
            };
        }

        private static int ComputeWidth(int[,] mask, bool[,] used, int i, int j, int maxV)
        {
            int c = mask[i, j];
            int width = 1;
            while (j + width < maxV && mask[i, j + width] == c && !used[i, j + width])
                width++;
            return width;
        }

        private static int ComputeHeight(int[,] mask, bool[,] used, int i, int j, int width, int maxU, int maxV)
        {
            int c = mask[i, j];
            int height = 1;
            while (i + height < maxU)
            {
                bool valid = true;
                for (int k = 0; k < width; k++)
                {
                    if (mask[i + height, j + k] != c || used[i + height, j + k])
                    {
                        valid = false;
                        break;
                    }
                }
                if (!valid) break;
                height++;
            }
            return height;
        }

        private static void MarkUsed(bool[,] used, int i, int j, int width, int height)
        {
            for (int a = 0; a < height; a++)
                for (int b = 0; b < width; b++)
                    used[i + a, j + b] = true;
        }

        private static (Vector3 quadOrigin, Vector3 duVec, Vector3 dvVec) ComputeQuadVectors(int d, int i, int j, int slice, float scale, int height, int width)
        {
            float dPos = slice * scale;
            Vector3 quadOrigin, duVec, dvVec;
            if (d == 0)
            {
                quadOrigin = new Vector3(dPos, i * scale, j * scale);
                duVec = new Vector3(0, height * scale, 0);
                dvVec = new Vector3(0, 0, width * scale);
            }
            else if (d == 1)
            {
                quadOrigin = new Vector3(i * scale, dPos, j * scale);
                duVec = new Vector3(height * scale, 0, 0);
                dvVec = new Vector3(0, 0, width * scale);
            }
            else
            {
                quadOrigin = new Vector3(i * scale, j * scale, dPos);
                duVec = new Vector3(height * scale, 0, 0);
                dvVec = new Vector3(0, width * scale, 0);
            }
            return (quadOrigin, duVec, dvVec);
        }

        private static void AddQuad(List<float> verts, List<uint> inds, ref uint vertCount, Vector3 quadOrigin, Vector3 duVec, Vector3 dvVec, Vector3 normal, int faceSign, Vector2[] customUV)
        {
            Vector3 v0 = quadOrigin;
            Vector3 v1 = quadOrigin + dvVec;
            Vector3 v2 = quadOrigin + duVec + dvVec;
            Vector3 v3 = quadOrigin + duVec;
            Vector2 uv0, uv1, uv2, uv3;
            if (customUV != null && customUV.Length >= 4)
            {
                uv0 = customUV[0];
                uv1 = customUV[1];
                uv2 = customUV[2];
                uv3 = customUV[3];
            }
            else
            {
                uv0 = new Vector2(0, 0);
                uv1 = new Vector2(0, 1);
                uv2 = new Vector2(1, 1);
                uv3 = new Vector2(1, 0);
            }
            AddVertex(verts, v0, uv0, normal);
            AddVertex(verts, v1, uv1, normal);
            AddVertex(verts, v2, uv2, normal);
            AddVertex(verts, v3, uv3, normal);
            if (faceSign > 0)
            {
                inds.Add(vertCount + 0);
                inds.Add(vertCount + 1);
                inds.Add(vertCount + 2);
                inds.Add(vertCount + 2);
                inds.Add(vertCount + 3);
                inds.Add(vertCount + 0);
            }
            else
            {
                inds.Add(vertCount + 0);
                inds.Add(vertCount + 3);
                inds.Add(vertCount + 2);
                inds.Add(vertCount + 2);
                inds.Add(vertCount + 1);
                inds.Add(vertCount + 0);
            }
            vertCount += 4;
        }

        private static void AddVertex(List<float> verts, Vector3 pos, Vector2 uv, Vector3 normal)
        {
            verts.Add(pos.X); verts.Add(pos.Y); verts.Add(pos.Z);
            verts.Add(uv.X); verts.Add(uv.Y);
            verts.Add(1); verts.Add(1); verts.Add(1);
            verts.Add(normal.X); verts.Add(normal.Y); verts.Add(normal.Z);
            verts.Add(1f); verts.Add(0f);
        }

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
}
