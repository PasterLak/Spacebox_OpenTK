using OpenTK.Mathematics;
using Engine;
using Spacebox.Generation;

namespace Spacebox.Game.Generation
{
    public class AsteroidMedium : Asteroid
    {
        public const int ChunkCount = 2;

        private int[,,] _voxelData;
        private int _gridSize;

        public AsteroidMedium(ulong id, Vector3 positionWorld, Sector sector)
            : base(id, positionWorld, sector) { }

        public override void OnGenerate()
        {
            GenerateVoxelData();
            int halfCount = ChunkCount / 2;
            for (int x = -halfCount; x < halfCount; x++)
                for (int y = -halfCount; y < halfCount; y++)
                    for (int z = -halfCount; z < halfCount; z++)
                    {
                        var idx = new Vector3SByte((sbyte)x, (sbyte)y, (sbyte)z);
                        var chunk = new Chunk(idx, this, true);
                        WriteChunkData(chunk);
                        AddChunk(chunk, false);
                    }
            IsGenerated = true;
        }

        private void GenerateVoxelData()
        {
            float diameter = ChunkCount * ChunkSize;
            var gen = new AsteroidVoxelDataGenerator(
                asteroidDiameter: diameter,
                blockSize: 1f,
                threshold: 33,
                noiseOctaves: 3,
                noiseScale: 1f,
                seed: (int)Seed,
                type: AsteroidType.Medium
            );
            gen.GenerateData();
            _voxelData = gen.voxelData;
            _gridSize = gen.gridSize;
            var oreParams = new AsteroidOreGeneratorParameters(
                outerOreVeinChance: 0.03f, outerOreMaxVeinSize: 10, outerOreIds: new[] { 4, 5, 6, 7 },
                middleOreVeinChance: 0.01f, middleOreMaxVeinSize: 8, middleOreIds: new[] { 8, 9, 11 },
                deepOreVeinChance: 0.005f, deepOreMaxVeinSize: 1, deepOreIds: new[] { 10 },
                oreSeed: (int)Seed

            );
            new AsteroidOreGenerator(oreParams).ApplyOres(ref _voxelData, Spacebox.Generation.AsteroidType.Medium);
        }

        private void WriteChunkData(Chunk chunk)
        {
            int half = _gridSize / 2;
            int ox = chunk.PositionIndex.X * ChunkSize + half;
            int oy = chunk.PositionIndex.Y * ChunkSize + half;
            int oz = chunk.PositionIndex.Z * ChunkSize + half;
            for (int x = 0; x < Chunk.Size; x++)
                for (int y = 0; y < Chunk.Size; y++)
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        int gx = x + ox, gy = y + oy, gz = z + oz;
                        if (gx < 0 || gy < 0 || gz < 0 || gx >= _gridSize || gy >= _gridSize || gz >= _gridSize)
                            chunk.Blocks[x, y, z] = GameAssets.CreateBlockFromId(0);
                        else
                            chunk.Blocks[x, y, z] = GameAssets.CreateBlockFromId((short)_voxelData[gx, gy, gz]);
                    }
        }
    }
}
