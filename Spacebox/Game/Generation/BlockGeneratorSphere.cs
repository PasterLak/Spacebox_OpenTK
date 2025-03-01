using OpenTK.Mathematics;
using Engine;
using Engine.Utils;
namespace Spacebox.Game.Generation
{
    public class BlockGeneratorSphere : BlockGenerator
    {
        public BlockGeneratorSphere(Chunk chunk, Vector3 position) : base(chunk, position) { }
        public override void Generate()
        {
            GenerateSphereBlocks();
            CarveWormTunnels(3);
            GenerateOreVeins();
        }
        private void GenerateSphereBlocks()
        {
            Vector3 center = new Vector3(Size / 2f);
            float radius = Size / 2f;
            float radiusSq = radius * radius;
            Random random = World.Random;
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        Vector3 blockCenter = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                        float distSq = Vector3.DistanceSquared(blockCenter, center);
                        if (distSq <= radiusSq)
                        {
                            if (distSq < radiusSq / 4f)
                                _blocks[x, y, z] = GameAssets.CreateBlockFromId(3);
                            else if (distSq < radiusSq / 2f)
                                _blocks[x, y, z] = GameAssets.CreateBlockFromId(2);
                            else
                                _blocks[x, y, z] = GameAssets.CreateBlockFromId(1);
                        }
                        else
                        {
                            _blocks[x, y, z] = GameAssets.CreateBlockFromId(0);
                        }
                    }
        }
        private void CarveWormTunnels(int wormCount)
        {
            FastRandom fastRandom = new FastRandom();
            for (int i = 0; i < wormCount; i++)
                CarveSingleWormTunnel(fastRandom);
        }
        private void CarveSingleWormTunnel(FastRandom fastRandom)
        {
            float tunnelRadius = 2f;
            int tunnelLength = (int)(Size * 0.8f);
            Vector3 start = new Vector3(Size / 2f);
            Vector3 dir;
            do
            {
                dir = new Vector3(
                    (float)(fastRandom.NextDouble() * 2 - 1),
                    (float)(fastRandom.NextDouble() * 2 - 1),
                    (float)(fastRandom.NextDouble() * 2 - 1)
                );
            } while (dir.LengthSquared < 0.0001f);
            dir = Vector3.Normalize(dir);
            Vector3 end = start + dir * tunnelLength;
            Vector3 bbMin = Vector3.ComponentMin(start, end) - new Vector3(tunnelRadius);
            Vector3 bbMax = Vector3.ComponentMax(start, end) + new Vector3(tunnelRadius);
            int minX = Math.Max(0, (int)Math.Floor(bbMin.X));
            int minY = Math.Max(0, (int)Math.Floor(bbMin.Y));
            int minZ = Math.Max(0, (int)Math.Floor(bbMin.Z));
            int maxX = Math.Min(Size - 1, (int)Math.Ceiling(bbMax.X));
            int maxY = Math.Min(Size - 1, (int)Math.Ceiling(bbMax.Y));
            int maxZ = Math.Min(Size - 1, (int)Math.Ceiling(bbMax.Z));
            float tunnelRadiusSq = tunnelRadius * tunnelRadius;
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        Vector3 blockCenter = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                        float distSq = DistancePointToSegmentSquared(blockCenter, start, end);
                        if (distSq <= tunnelRadiusSq)
                            _blocks[x, y, z] = GameAssets.CreateBlockFromId(0);
                    }
        }
        private static float DistancePointToSegmentSquared(Vector3 point, Vector3 segA, Vector3 segB)
        {
            Vector3 v = segB - segA;
            Vector3 w = point - segA;
            float c1 = Vector3.Dot(w, v);
            if (c1 <= 0)
                return Vector3.DistanceSquared(point, segA);
            float c2 = Vector3.Dot(v, v);
            if (c2 <= c1)
                return Vector3.DistanceSquared(point, segB);
            float b = c1 / c2;
            Vector3 pb = segA + b * v;
            return Vector3.DistanceSquared(point, pb);
        }
        private void GenerateOreVeins()
        {
            FastRandom fastRandom = new FastRandom();
            double baseChanceId1 = 0.1;
            double baseChanceId2 = 0.07;
            double baseChanceId3 = 0.02;
            int noiseSeed = 12345;
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        short id = _blocks[x, y, z].BlockId;
                        float noise = (Noise3D(x, y, z, noiseSeed) + 1f) / 2f;
                        if (id == 1)
                        {
                            double chance = baseChanceId1 * (1 - noise);
                            if (fastRandom.NextDouble() < chance)
                            {
                                int[] arr = { 4, 5, 6, 7 };
                                short resId = (short)arr[fastRandom.Next(arr.Length)];
                                int veinSize = fastRandom.Next(2, 6);
                                GenerateOreVein(x, y, z, fastRandom, resId, veinSize);
                            }
                        }
                        else if (id == 2)
                        {
                            double chance = baseChanceId2 * (1 - noise);
                            if (fastRandom.NextDouble() < chance)
                            {
                                int[] arr = { 8, 9, 11 };
                                short resId = (short)arr[fastRandom.Next(arr.Length)];
                                int veinSize = fastRandom.Next(2, 6);
                                GenerateOreVein(x, y, z, fastRandom, resId, veinSize);
                            }
                        }
                        else if (id == 3)
                        {
                            double chance = baseChanceId3 * (1 - noise);
                            if (fastRandom.NextDouble() < chance)
                            {
                                double choice = fastRandom.NextDouble();
                                short resId = (short)(choice < 0.8 ? 10 : (choice < 0.9 ? 8 : 11));
                                _blocks[x, y, z] = GameAssets.CreateBlockFromId(resId);
                            }
                        }
                    }
        }
        private void GenerateOreVein(int startX, int startY, int startZ, FastRandom fastRandom, short resourceId, int veinSize)
        {
            int currentX = startX, currentY = startY, currentZ = startZ;
            for (int i = 0; i < veinSize; i++)
            {
                if (currentX < 0 || currentX >= Size || currentY < 0 || currentY >= Size || currentZ < 0 || currentZ >= Size)
                    break;
                short currentId = _blocks[currentX, currentY, currentZ].BlockId;
                if (currentId == 1 || currentId == 2 || currentId == 3 || currentId == resourceId)
                    _blocks[currentX, currentY, currentZ] = GameAssets.CreateBlockFromId(resourceId);
                int dx = fastRandom.Next(0, 3) - 1;
                int dy = fastRandom.Next(0, 3) - 1;
                int dz = fastRandom.Next(0, 3) - 1;
                currentX += dx;
                currentY += dy;
                currentZ += dz;
            }
        }
        float Noise3D(int x, int y, int z, int seed)
        {
            int n = x + y * 57 + z * 131 + seed * 999983;
            n = (n << 13) ^ n;
            return 1f - (((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f);
        }
    }
}
