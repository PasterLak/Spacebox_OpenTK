using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Game.Generation
{
    public abstract class Asteroid : SpaceEntity
    {
        public Asteroid(int id, Vector3 positionWorld, Sector sector)
            : base(id, positionWorld, sector) { }

        public virtual void OnGenerate() { }
    }

    public class AsteroidLight : Asteroid
    {
        public const int ChunkCount = 1;
        public const int ChunkSize = 32;

        public AsteroidLight(int id, Vector3 positionWorld, Sector sector)
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
                        AddChunk(chunk, false);
                    }
            IsGenerated = true;
        }

        void FillChunkNoise(Chunk chunk)
        {
            float diameter = ChunkCount * ChunkSize;
            var generator = new AsteroidVoxelDataGenerator(
                asteroidDiameter: diameter,
                blockSize: 1f,
                threshold: 90,
                noiseOctaves: 4,
                noiseScale: 0.03f,
                seed: World.Seed,
                type: AsteroidType.Light
            );
            generator.GenerateData();
            var data = generator.voxelData;
            int size = generator.gridSize;

            int offsetX = chunk.PositionIndex.X * ChunkSize - size / 2;
            int offsetY = chunk.PositionIndex.Y * ChunkSize - size / 2;
            int offsetZ = chunk.PositionIndex.Z * ChunkSize - size / 2;

            for (int x = 0; x < Chunk.Size; x++)
                for (int y = 0; y < Chunk.Size; y++)
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        int dataX = x + offsetX;
                        int dataY = y + offsetY;
                        int dataZ = z + offsetZ;
                        if (dataX < 0 || dataY < 0 || dataZ < 0 || dataX >= size || dataY >= size || dataZ >= size)
                        {
                            chunk.Blocks[x, y, z] = GameBlocks.CreateBlockFromId(0);
                        }
                        else
                        {
                            int blockId = data[dataX, dataY, dataZ];
                            chunk.Blocks[x, y, z] = GameBlocks.CreateBlockFromId((short)blockId);
                        }
                    }
        }
    }

    public class AsteroidMedium : Asteroid
    {
        public const int ChunkCount = 2;
        public const int ChunkSize = 32;

        public AsteroidMedium(int id, Vector3 positionWorld, Sector sector)
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
                        AddChunk(chunk, false);
                    }
            IsGenerated = true;
        }

        void FillChunkNoise(Chunk chunk)
        {
            float diameter = ChunkCount * ChunkSize;
            var generator = new AsteroidVoxelDataGenerator(
                asteroidDiameter: diameter,
                blockSize: 1f,
                threshold: 85,
                noiseOctaves: 4,
                noiseScale: 0.03f,
                seed: World.Seed,
                type: AsteroidType.Medium
            );
            generator.GenerateData();
            var data = generator.voxelData;
            int size = generator.gridSize;

            int offsetX = chunk.PositionIndex.X * ChunkSize - size / 2;
            int offsetY = chunk.PositionIndex.Y * ChunkSize - size / 2;
            int offsetZ = chunk.PositionIndex.Z * ChunkSize - size / 2;

            for (int x = 0; x < Chunk.Size; x++)
                for (int y = 0; y < Chunk.Size; y++)
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        int dataX = x + offsetX;
                        int dataY = y + offsetY;
                        int dataZ = z + offsetZ;
                        if (dataX < 0 || dataY < 0 || dataZ < 0 || dataX >= size || dataY >= size || dataZ >= size)
                        {
                            chunk.Blocks[x, y, z] = GameBlocks.CreateBlockFromId(0);
                        }
                        else
                        {
                            int blockId = data[dataX, dataY, dataZ];
                            chunk.Blocks[x, y, z] = GameBlocks.CreateBlockFromId((short)blockId);
                        }
                    }
        }
    }

    public class AsteroidHeavy : Asteroid
    {
        public const int ChunkCount = 4;
        public const int ChunkSize = 32;

        public AsteroidHeavy(int id, Vector3 positionWorld, Sector sector)
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
                        AddChunk(chunk, false);
                    }
            IsGenerated = true;
        }

        void FillChunkNoise(Chunk chunk)
        {
            float diameter = ChunkCount * ChunkSize;
            var generator = new AsteroidVoxelDataGenerator(
                asteroidDiameter: diameter,
                blockSize: 1f,
                threshold: 80,
                noiseOctaves: 4,
                noiseScale: 0.03f,
                seed: World.Seed,
                type: AsteroidType.Heavy
            );
            generator.GenerateData();
            var data = generator.voxelData;
            int size = generator.gridSize;

            int offsetX = chunk.PositionIndex.X * ChunkSize - size / 2;
            int offsetY = chunk.PositionIndex.Y * ChunkSize - size / 2;
            int offsetZ = chunk.PositionIndex.Z * ChunkSize - size / 2;

            for (int x = 0; x < Chunk.Size; x++)
                for (int y = 0; x < Chunk.Size && y < Chunk.Size; y++)
                    for (int z = 0; x < Chunk.Size && y < Chunk.Size && z < Chunk.Size; z++)
                    {
                        int dataX = x + offsetX;
                        int dataY = y + offsetY;
                        int dataZ = z + offsetZ;
                        if (dataX < 0 || dataY < 0 || dataZ < 0 || dataX >= size || dataY >= size || dataZ >= size)
                        {
                            chunk.Blocks[x, y, z] = GameBlocks.CreateBlockFromId(0);
                        }
                        else
                        {
                            int blockId = data[dataX, dataY, dataZ];
                            chunk.Blocks[x, y, z] = GameBlocks.CreateBlockFromId((short)blockId);
                        }
                    }
        }
    }
}
