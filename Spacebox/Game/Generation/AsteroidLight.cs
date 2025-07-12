using OpenTK.Mathematics;
using Engine;
using Spacebox.Generation;


namespace Spacebox.Game.Generation
{
    public class AsteroidLight : Asteroid
    {
        public const int ChunkCount = 1;

        public AsteroidLight(ulong id, Vector3 positionWorld, Sector sector)
            : base(id, positionWorld, sector) { }

        public override void OnGenerate()
        {
            for (int x = 0; x < ChunkCount; x++)
                for (int y = 0; y < ChunkCount; y++)
                    for (int z = 0; z < ChunkCount; z++)
                    {
                        var idx = new Vector3SByte((sbyte)x, (sbyte)y, (sbyte)z);
                        var chunk = new Chunk(idx, this, true);
                        FillChunkNoise(chunk);
                        WriteChunkData(chunk);
                        AddChunk(chunk, false);
                    }
            IsGenerated = true;
        }

        private int[,,] _voxelData;
        private int _gridSize;

        public override void FillChunkNoise(Chunk chunk)
        {
            float diameter = ChunkCount * ChunkSize;
            var generator = new AsteroidVoxelDataGenerator(
                asteroidDiameter: diameter,
                blockSize: 1f,
                threshold: 33,
                noiseOctaves: 3,
                noiseScale: 1f,
                seed: Seed,
                type: AsteroidType.Light
            );
            generator.GenerateData();
            _voxelData = generator.voxelData;
            _gridSize = generator.gridSize;

            var oreParams = new AsteroidOreGeneratorParameters(
                outerOreVeinChance: 0.03f, outerOreMaxVeinSize: 8, outerOreIds: new[] { 4,5,6,7 },
                middleOreVeinChance: 0.03f, middleOreMaxVeinSize: 6, middleOreIds: new[] { 8,9,11,13 },
                deepOreVeinChance: 0.0f, deepOreMaxVeinSize: 0, deepOreIds: System.Array.Empty<int>(),
                oreSeed: Seed
            );
            var oreGen = new AsteroidOreGenerator(oreParams);
            oreGen.ApplyOres(ref _voxelData, Spacebox.Generation.AsteroidType.Light);
        }

        private void WriteChunkData(Chunk chunk)
        {
            int half = _gridSize / 2;
            int ox = chunk.PositionIndex.X * ChunkSize - half;
            int oy = chunk.PositionIndex.Y * ChunkSize - half;
            int oz = chunk.PositionIndex.Z * ChunkSize - half;

            for (int x = 0; x < Chunk.Size; x++)
                for (int y = 0; y < Chunk.Size; y++)
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        int gx = x , gy = y , gz = z ;
                        if (gx < 0 || gy < 0 || gz < 0 || gx >= _gridSize || gy >= _gridSize || gz >= _gridSize)
                            chunk.Blocks[x, y, z] = GameAssets.CreateBlockFromId(0);
                        else
                        {
                            int id = _voxelData[gx, gy, gz];
                            chunk.Blocks[x, y, z] = GameAssets.CreateBlockFromId((short)id);
                        }
                    }
        }
    }
}
