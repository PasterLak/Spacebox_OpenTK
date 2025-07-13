using OpenTK.Mathematics;
using Engine;
using Spacebox.Generation;

namespace Spacebox.Game.Generation
{
    public class AsteroidLight : Asteroid
    {
        public const int ChunkCount = 1;

        private int[,,] _voxelData;
        private int _gridSize;

        public AsteroidLight(ulong id, Vector3 positionWorld, Sector sector)
            : base(id, positionWorld, sector) { }

        public override void OnGenerate()
        {
            GenerateAllChunks();
            IsGenerated = true;
        }

        public void GenerateAllChunks()
        {
            GenerateAllVoxelData();

            for (int x = 0; x < ChunkCount; x++)
                for (int y = 0; y < ChunkCount; y++)
                    for (int z = 0; z < ChunkCount; z++)
                    {
                        var idx = new Vector3SByte((sbyte)x, (sbyte)y, (sbyte)z);
                        var chunk = GenerateChunk(idx);
                        AddChunk(chunk, false);
                    }
        }

        private void GenerateAllVoxelData()
        {
            Vector3 diameter = new Vector3(ChunkCount * ChunkSize, ChunkCount * ChunkSize, ChunkCount * ChunkSize); 
            var generator = new AsteroidVoxelDataGenerator(
                diameter,
                1f,
                33,
                3,
                1f,
                Seed,
                AsteroidType.Light
            );
            generator.GenerateData();
            _voxelData = generator.voxelData;
            _gridSize = generator.gridSize;

            var oreParams = new AsteroidOreGeneratorParameters(
                outerOreVeinChance: 0.03f,
                outerOreMaxVeinSize: 8,
                outerOreIds: new[] { 4, 5, 6, 7 },
                middleOreVeinChance: 0.03f,
                middleOreMaxVeinSize: 6,
                middleOreIds: new[] { 8, 9, 11 },
                deepOreVeinChance: 0.0f,
                deepOreMaxVeinSize: 0,
                deepOreIds: System.Array.Empty<int>(),
                oreSeed: Seed
            );
            var oreGen = new AsteroidOreGenerator(oreParams);
            oreGen.ApplyOres(ref _voxelData, Spacebox.Generation.AsteroidType.Light);
        }

        public Chunk GenerateChunk(Vector3SByte idx)
        {
            if (_voxelData == null)
                GenerateAllVoxelData();

            var chunk = new Chunk(idx, this, true);
            int half = _gridSize / 2;
            int ox = idx.X * ChunkSize - half;
            int oy = idx.Y * ChunkSize - half;
            int oz = idx.Z * ChunkSize - half;

            for (int x = 0; x < Chunk.Size; x++)
                for (int y = 0; y < Chunk.Size; y++)
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        int gx = x ;
                        int gy = y ;
                        int gz = z ;

                        if (gx < 0 || gy < 0 || gz < 0 || gx >= _gridSize || gy >= _gridSize || gz >= _gridSize)
                            chunk.Blocks[x, y, z] = GameAssets.CreateBlockFromId(0);
                        else
                            chunk.Blocks[x, y, z] = GameAssets.CreateBlockFromId((short)_voxelData[gx, gy, gz]);
                    }

            return chunk;
        }
    }
}
