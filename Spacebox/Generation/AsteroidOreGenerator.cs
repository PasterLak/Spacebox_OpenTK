using OpenTK.Mathematics;



namespace Spacebox.Generation
{
    public enum AsteroidType
    {
        Light,
        Medium,
        Heavy
    }
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

        public void ApplyOres(ref int[,,] voxelData, AsteroidType asteroidType)
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
                                StartVein(ref voxelData, x, y, z, 1, parameters.OuterOreIds, parameters.OuterOreMaxVeinSize);
                        }
                        else if (blockId == 2)
                        {
                            if (rng.NextDouble() < parameters.MiddleOreVeinChance)
                                StartVein(ref voxelData, x, y, z, 2, parameters.MiddleOreIds, parameters.MiddleOreMaxVeinSize);
                        }
                        else if (blockId == 3 && asteroidType != AsteroidType.Light)
                        {
                            if (rng.NextDouble() < parameters.DeepOreVeinChance)
                                StartVein(ref voxelData, x, y, z, 3, parameters.DeepOreIds, parameters.DeepOreMaxVeinSize);
                        }
                    }
                }
            }
        }

        public void ApplyOresToChunk(ref int[,,] voxelData, AsteroidType asteroidType)
        {
            int size = voxelData.GetLength(0);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    for (int z = 0; z < size; z++)
                    {
                        int id = voxelData[x, y, z];
                        if (id == 1 && rng.NextDouble() < parameters.OuterOreVeinChance)
                            StartVein(ref voxelData, x, y, z, 1, parameters.OuterOreIds, parameters.OuterOreMaxVeinSize);
                        else if (id == 2 && rng.NextDouble() < parameters.MiddleOreVeinChance)
                            StartVein(ref voxelData, x, y, z, 2, parameters.MiddleOreIds, parameters.MiddleOreMaxVeinSize);
                        else if (id == 3 && asteroidType != AsteroidType.Light && rng.NextDouble() < parameters.DeepOreVeinChance)
                            StartVein(ref voxelData, x, y, z, 3, parameters.DeepOreIds, parameters.DeepOreMaxVeinSize);
                    }
        }

        private void StartVein(ref int[,,] voxelData, int startX, int startY, int startZ, int targetLayer, int[] ores, int maxSize)
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
                var cur = queue.Dequeue();
                if (voxelData[cur.X, cur.Y, cur.Z] == targetLayer)
                {
                    vein.Add(cur);
                    foreach (var off in neighborOffsets)
                    {
                        var n = cur + off;
                        if (n.X < 0 || n.Y < 0 || n.Z < 0 || n.X >= size || n.Y >= size || n.Z >= size)
                            continue;
                        if (!visited.Contains(n) && voxelData[n.X, n.Y, n.Z] == targetLayer)
                        {
                            visited.Add(n);
                            if (rng.NextDouble() < 0.5)
                                queue.Enqueue(n);
                        }
                    }
                }
            }

            if (vein.Count == 0)
                return;

            int oreId = ores[rng.Next(ores.Length)];
            foreach (var v in vein)
                voxelData[v.X, v.Y, v.Z] = oreId;
        }

    }
}
