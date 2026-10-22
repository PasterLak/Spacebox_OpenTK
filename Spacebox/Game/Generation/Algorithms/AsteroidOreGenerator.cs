using OpenTK.Mathematics;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Generation
{
    public class AsteroidOreGenerator
    {
        private readonly AsteroidData asteroidData;
        private readonly Random rng;
        private readonly AsteroidGenerationContext context;
        private readonly Vector3i chunkOrigin;

        private static readonly Vector3i[] neighborOffsets = new Vector3i[]
        {
            new Vector3i(1, 0, 0),
            new Vector3i(-1, 0, 0),
            new Vector3i(0, 1, 0),
            new Vector3i(0, -1, 0),
            new Vector3i(0, 0, 1),
            new Vector3i(0, 0, -1)
        };

        public AsteroidOreGenerator(AsteroidData asteroidData, int seed) : this(asteroidData, seed, AsteroidGenerationContext.Legacy, default)
        {
        }

        public AsteroidOreGenerator(AsteroidData asteroidData, int seed, AsteroidGenerationContext context, Engine.Vector3SByte chunkIndex)
        {
            this.asteroidData = asteroidData;
            this.context = context;
            rng = new Random(seed);
            chunkOrigin = new Vector3i(chunkIndex.X * Spacebox.Game.Generation.Chunk.Size - 1, chunkIndex.Y * Spacebox.Game.Generation.Chunk.Size - 1, chunkIndex.Z * Spacebox.Game.Generation.Chunk.Size - 1);
        }

        public void ApplyOres(ref int[,,] voxelData)
        {
            if (!asteroidData.HasOreDeposits || asteroidData.Layers.Length == 0)
                return;

            int size = voxelData.GetLength(0);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        int blockId = voxelData[x, y, z];
                        if (blockId == 0) continue;

                        foreach (var layer in asteroidData.Layers)
                        {
                            if (blockId == layer.FillBlockID)
                            {
                                ProcessVeinsForLayer(ref voxelData, x, y, z, layer);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ProcessVeinsForLayer(ref int[,,] voxelData, int x, int y, int z, AsteroidLayer layer)
        {
            if (context.UsesRules)
            {
                ProcessVeinsWithRules(ref voxelData, x, y, z, layer);
                return;
            }

            foreach (var vein in layer.Veins)
            {
                if (rng.Next(1_000_000) < vein.SpawnChance)
                {
                    if (!vein.CanSpawnNearVoid && HasVoidNeighbor(voxelData, x, y, z))
                        continue;

                    int veinSize = rng.Next(vein.MinVeinSize, vein.MaxVeinSize + 1);

                    if(veinSize == 0) continue;

                    StartVein(ref voxelData, x, y, z, layer.FillBlockID, vein.BlockId, veinSize);
                    break;
                }
            }
        }

        private void ProcessVeinsWithRules(ref int[,,] voxelData, int x, int y, int z, AsteroidLayer layer)
        {
            var local = chunkOrigin + new Vector3i(x, y, z);
            float depth = context.DepthAt(new Vector3(local.X, local.Y, local.Z));

            foreach (var vein in layer.Veins)
            {
                if (!VeinRules.Allows(vein, depth, context.DistanceFromCenter)) continue;
                if (rng.Next(1_000_000) >= VeinRules.Chance(vein, context.Biome)) continue;
                if (!vein.CanSpawnNearVoid && HasVoidNeighbor(voxelData, x, y, z)) continue;
                if (vein.NearCavesOnly && !HasVoidWithin(voxelData, x, y, z, 2)) continue;

                int veinSize = rng.Next(vein.MinVeinSize, vein.MaxVeinSize + 1);
                if (veinSize == 0) continue;

                StartVein(ref voxelData, x, y, z, layer.FillBlockID, vein.BlockId, veinSize);
                break;
            }
        }

        private static bool HasVoidWithin(int[,,] voxelData, int x, int y, int z, int range)
        {
            int size = voxelData.GetLength(0);
            for (int dx = -range; dx <= range; dx++)
            for (int dy = -range; dy <= range; dy++)
            for (int dz = -range; dz <= range; dz++)
            {
                int nx = x + dx, ny = y + dy, nz = z + dz;
                if (nx < 0 || ny < 0 || nz < 0 || nx >= size || ny >= size || nz >= size) continue;
                if (voxelData[nx, ny, nz] == 0) return true;
            }
            return false;
        }

        private bool HasVoidNeighbor(int[,,] voxelData, int x, int y, int z)
        {
            int size = voxelData.GetLength(0);

            foreach (var offset in neighborOffsets)
            {
                int nx = x + offset.X;
                int ny = y + offset.Y;
                int nz = z + offset.Z;

                if (nx >= 0 && ny >= 0 && nz >= 0 && nx < size && ny < size && nz < size)
                {
                    if (voxelData[nx, ny, nz] == 0)
                        return true;
                }
            }
            return false;
        }

        private void StartVein(ref int[,,] voxelData, int startX, int startY, int startZ, int targetBlockId, int oreId, int maxSize)
        {
            int size = voxelData.GetLength(0);
            var queue = new Queue<Vector3i>();
            var visited = new HashSet<Vector3i>();
            var vein = new List<Vector3i>();
            var start = new Vector3i(startX, startY, startZ);

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0 && vein.Count < maxSize)
            {
                var current = queue.Dequeue();
                if (voxelData[current.X, current.Y, current.Z] == targetBlockId)
                {
                    vein.Add(current);

                    foreach (var offset in neighborOffsets)
                    {
                        var neighbor = current + offset;

                        if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.Z < 0 ||
                            neighbor.X >= size || neighbor.Y >= size || neighbor.Z >= size)
                            continue;

                        if (!visited.Contains(neighbor) && voxelData[neighbor.X, neighbor.Y, neighbor.Z] == targetBlockId)
                        {
                            visited.Add(neighbor);
                            if (rng.NextDouble() < 0.6)
                                queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            if (vein.Count == 0) return;

            foreach (var position in vein)
                voxelData[position.X, position.Y, position.Z] = oreId;
        }
    }
}