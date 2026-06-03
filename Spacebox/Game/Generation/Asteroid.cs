using Engine;
using Engine.Multithreading;
using Engine.Physics;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Structures;
using Spacebox.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation
{
    public class Asteroid : SpaceEntity
    {
        private int diameterBlocks;
        private readonly AsteroidVoxelDataGenerator voxelGen;
        private readonly object _genLock = new();
        private readonly HashSet<Vector3SByte> pendingChunks = new HashSet<Vector3SByte>();
        private readonly HashSet<Vector3SByte> generatingChunks = new();
        private readonly HashSet<Vector3SByte> loadedChunks = new HashSet<Vector3SByte>();

        public NotGeneratedEntity NotGeneratedEntity;
        protected readonly int Seed;

        public Asteroid(NotGeneratedEntity data, Sector sector) : this(data, sector, true) { }

        public Asteroid(NotGeneratedEntity data, Sector sector, bool generate) : base(data.Id, data.positionWorld, sector)
        {
            Seed = SeedHelper.ToIntSeed(data.Id);
            NotGeneratedEntity = data;

            if (!generate) return;

            var asteroidData = data.asteroid;
            diameterBlocks = data.radiusBlocks + data.radiusBlocks;
            Vector3 diameter = new Vector3(diameterBlocks);

            voxelGen = new AsteroidVoxelDataGenerator(
                asteroidDimensions: diameter,
                blockSize: 1f,
                asteroidData,
                seed: (int)Seed
            );
        }

        private static float ComputeStep(byte wormDiameter, float overlapK = 0.75f)
        {
            float step = (wormDiameter * 0.5f) * overlapK;
            return step < 1f ? 1f : step;
        }

        public void OnGenerate()
        {
            float halfSize = diameterBlocks * 0.5f;
            int minChunk = (int)MathF.Floor(-halfSize / Chunk.Size);
            int maxChunk = (int)MathF.Floor(halfSize / Chunk.Size);

            for (int x = minChunk; x <= maxChunk; x++)
                for (int y = minChunk; y <= maxChunk; y++)
                    for (int z = minChunk; z <= maxChunk; z++)
                        pendingChunks.Add(new Vector3SByte((sbyte)x, (sbyte)y, (sbyte)z));

            IsGenerated = true;
        }

        public override void Update()
        {
            var cam = Camera.Main;
            var frustum = cam.Frustum;

            float visR = Settings.CHUNK_VISIBLE_RADIUS;
            float visR2 = visR * visR;

            Vector3SByte[] toCheck;
            lock (_genLock)
            {
                toCheck = pendingChunks.ToArray();
            }

            foreach (var idx in toCheck)
            {
                var bounds = ComputeChunkBounds(idx);
                if ((bounds.Center - cam.Position).LengthSquared > visR2 || !frustum.IsInFrustum(bounds))
                    continue;

                lock (_genLock)
                {
                    if (generatingChunks.Contains(idx) || loadedChunks.Contains(idx))
                        continue;
                    generatingChunks.Add(idx);
                }

                GenerateChunkAsync(idx);
            }
            base.Update();
        }

        private void GenerateChunkAsync(Vector3SByte idx)
        {
            WorkerPoolManager.Enqueue(token =>
            {
                try
                {
                    var result = CreateChunk(idx);

                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (result.meshData.Mass > 0)
                        {
                            AddChunk(result.chunk, false);
                            result.chunk.ApplyMeshData(result.meshData);
                        }
                        else
                        {
                            result.chunk.Dispose();
                        }

                        lock (_genLock)
                        {
                            pendingChunks.Remove(idx);
                            generatingChunks.Remove(idx);
                            loadedChunks.Add(idx);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.Error($"Asteroid chunk {idx} generation failed: {ex}");
                    lock (_genLock)
                    {
                        generatingChunks.Remove(idx);
                    }
                }
            }, WorkerPoolManager.Priority.High);
        }

        private BoundingBox ComputeChunkBounds(Vector3SByte idx)
        {
            Vector3 min = PositionWorld + new Vector3(idx.X * Chunk.Size, idx.Y * Chunk.Size, idx.Z * Chunk.Size);
            Vector3 max = min + new Vector3(Chunk.Size);
            Vector3 center = (min + max) * 0.5f;
            return new BoundingBox(center, new Vector3(Chunk.Size));
        }

        private (Chunk chunk, MeshData meshData) CreateChunk(Vector3SByte idx)
        {
            var chunk = new Chunk(idx, this, true);
            var paddedData = voxelGen.GeneratePaddedDataForChunk(idx);
            var chunkSeed = SeedHelper.GetChunkIdInt(EntityID, idx);

            if (NotGeneratedEntity.asteroid.UsePerlinWorms)
            {
                var wormParams = new WormParameters(
                    NotGeneratedEntity.asteroid.WormSettings.MinCount,
                    NotGeneratedEntity.asteroid.WormSettings.MaxCount,
                    NotGeneratedEntity.asteroid.WormSettings.DiameterInBlocks,
                    NotGeneratedEntity.asteroid.WormSettings.PathDeviation,
                    NotGeneratedEntity.asteroid.WormSettings.MaxTunnelLength,
                    ComputeStep(NotGeneratedEntity.asteroid.WormSettings.DiameterInBlocks, NotGeneratedEntity.asteroid.WormSettings.StepSize),
                    Seed);

                PerlinWorms.CarvePaddedChunk(paddedData, idx, Seed, wormParams);
            }

            var oreGen = new AsteroidOreGenerator(NotGeneratedEntity.asteroid, chunkSeed);
            oreGen.ApplyOres(ref paddedData);

            Block[,,] paddedBlocks = new Block[34, 34, 34];
            for (int x = 0; x < 34; x++)
            {
                for (int y = 0; y < 34; y++)
                {
                    for (int z = 0; z < 34; z++)
                    {
                        paddedBlocks[x, y, z] = GameAssets.CreateBlockFromId((short)paddedData[x, y, z]);
                    }
                }
            }

            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        chunk.Blocks[x, y, z] = paddedBlocks[x + 1, y + 1, z + 1];
                    }
                }
            }

            var tempGenerator = new MeshGenerator(chunk.PositionIndex, paddedBlocks, Chunk.MeasureGenerationTime);
            var meshData = tempGenerator.GenerateMeshData();

            return (chunk, meshData);
        }
    }
}