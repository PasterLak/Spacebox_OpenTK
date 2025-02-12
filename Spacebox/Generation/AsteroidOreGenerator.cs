using OpenTK.Mathematics;

public enum AsteroidType
{
    Light,
    Medium,
    Heavy
}

namespace Spacebox.Generation
{
    public struct AsteroidOreGeneratorParameters
    {
        public float OuterOreVeinChance;
        public int OuterOreMaxVeinSize;
        public int[] OuterOreIds;
        public float MiddleOreVeinChance;
        public int MiddleOreMaxVeinSize;
        public int[] MiddleOreIds;
        public float DeepOreVeinChance;
        public int DeepOreMaxVeinSize;
        public int[] DeepOreIds;
        public int OreSeed;

        public AsteroidOreGeneratorParameters(
            float outerOreVeinChance,
            int outerOreMaxVeinSize,
            int[] outerOreIds,
            float middleOreVeinChance,
            int middleOreMaxVeinSize,
            int[] middleOreIds,
            float deepOreVeinChance,
            int deepOreMaxVeinSize,
            int[] deepOreIds,
            int oreSeed)
        {
            OuterOreVeinChance = outerOreVeinChance;
            OuterOreMaxVeinSize = outerOreMaxVeinSize;
            OuterOreIds = outerOreIds;
            MiddleOreVeinChance = middleOreVeinChance;
            MiddleOreMaxVeinSize = middleOreMaxVeinSize;
            MiddleOreIds = middleOreIds;
            DeepOreVeinChance = deepOreVeinChance;
            DeepOreMaxVeinSize = deepOreMaxVeinSize;
            DeepOreIds = deepOreIds;
            OreSeed = oreSeed;
        }
    }

    public class AsteroidOreGenerator
    {
        private readonly AsteroidOreGeneratorParameters parameters;
        private readonly Random rng;

        private static readonly Vector3i[] neighborOffsets = new Vector3i[]
        {
            new Vector3i(1, 0, 0),
            new Vector3i(-1, 0, 0),
            new Vector3i(0, 1, 0),
            new Vector3i(0, -1, 0),
            new Vector3i(0, 0, 1),
            new Vector3i(0, 0, -1)
        };

        public AsteroidOreGenerator(AsteroidOreGeneratorParameters parameters)
        {
            this.parameters = parameters;
            rng = new Random(parameters.OreSeed);
        }

        public void ApplyOres(int[,,] voxelData, AsteroidType asteroidType)
        {
            int gridSize = voxelData.GetLength(0);
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    for (int z = 0; z < gridSize; z++)
                    {
                        int blockId = voxelData[x, y, z];
                        if (blockId == 1)
                        {
                            if (rng.NextDouble() < parameters.OuterOreVeinChance)
                                StartOreVein(voxelData, x, y, z, 1, parameters.OuterOreIds, parameters.OuterOreMaxVeinSize);
                        }
                        else if (blockId == 2)
                        {
                            if (rng.NextDouble() < parameters.MiddleOreVeinChance)
                                StartOreVein(voxelData, x, y, z, 2, parameters.MiddleOreIds, parameters.MiddleOreMaxVeinSize);
                        }
                        else if (blockId == 3 && asteroidType != AsteroidType.Light)
                        {
                            if (rng.NextDouble() < parameters.DeepOreVeinChance)
                                StartOreVein(voxelData, x, y, z, 3, parameters.DeepOreIds, parameters.DeepOreMaxVeinSize);
                        }
                    }
                }
            }
        }

        private void StartOreVein(int[,,] voxelData, int startX, int startY, int startZ, int targetLayerId, int[] allowedOreIds, int maxVeinSize)
        {
            int gridSize = voxelData.GetLength(0);
            Queue<Vector3i> queue = new Queue<Vector3i>();
            HashSet<Vector3i> visited = new HashSet<Vector3i>();
            List<Vector3i> veinBlocks = new List<Vector3i>();
            Vector3i start = new Vector3i(startX, startY, startZ);
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0 && veinBlocks.Count < maxVeinSize)
            {
                Vector3i current = queue.Dequeue();
                if (voxelData[current.X, current.Y, current.Z] == targetLayerId)
                {
                    veinBlocks.Add(current);
                    for (int i = 0; i < neighborOffsets.Length; i++)
                    {
                        Vector3i n = current + neighborOffsets[i];
                        if (n.X < 0 || n.Y < 0 || n.Z < 0 || n.X >= gridSize || n.Y >= gridSize || n.Z >= gridSize)
                            continue;
                        if (!visited.Contains(n) && voxelData[n.X, n.Y, n.Z] == targetLayerId)
                        {
                            visited.Add(n);
                            if (rng.NextDouble() < 0.5)
                                queue.Enqueue(n);
                        }
                    }
                }
            }

            for (int i = 0; i < veinBlocks.Count; i++)
            {
                Vector3i pos = veinBlocks[i];
                bool skip = false;
                if (targetLayerId == 3 && allowedOreIds.Length == 1 && allowedOreIds[0] == 11)
                {
                    for (int j = 0; j < neighborOffsets.Length; j++)
                    {
                        Vector3i n = pos + neighborOffsets[j];
                        if (n.X < 0 || n.Y < 0 || n.Z < 0 || n.X >= gridSize || n.Y >= gridSize || n.Z >= gridSize)
                            continue;
                        if (voxelData[n.X, n.Y, n.Z] == 0)
                        {
                            skip = true;
                            break;
                        }
                    }
                }
                if (!skip)
                {
                    int oreId = allowedOreIds[rng.Next(0, allowedOreIds.Length)];
                    voxelData[pos.X, pos.Y, pos.Z] = oreId;
                }
            }
        }
    }
}
