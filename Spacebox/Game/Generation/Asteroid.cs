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
        private readonly List<Vector3SByte> _chunksToCheck = new List<Vector3SByte>();
        private readonly HashSet<Vector3SByte> _removedChunks = new HashSet<Vector3SByte>();
        private bool _savedWithoutGenerator;

        public NotGeneratedEntity NotGeneratedEntity;
        protected readonly int Seed;

        public Asteroid(NotGeneratedEntity data, Sector sector) : this(data, sector, true) { }

        public Asteroid(NotGeneratedEntity data, Sector sector, bool generate) : base(data.Id, data.positionWorld, sector)
        {
            Seed = SeedHelper.ToIntSeed(data.Id);
            NotGeneratedEntity = data;

            if (!generate) return;

            SetOrientation(CreateOrientation(Seed));

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

        public static Quaternion CreateOrientation(int seed)
        {
            var random = new Random(seed ^ 0x2F6B1A3D);
            float u1 = (float)random.NextDouble();
            float u2 = (float)random.NextDouble() * MathF.Tau;
            float u3 = (float)random.NextDouble() * MathF.Tau;

            float a = MathF.Sqrt(1f - u1);
            float b = MathF.Sqrt(u1);

            return new Quaternion(a * MathF.Sin(u2), a * MathF.Cos(u2), b * MathF.Sin(u3), b * MathF.Cos(u3));
        }

        public bool CanRegenerate => voxelGen != null;
        public override bool SavesOnlyChangedChunks => CanRegenerate || _savedWithoutGenerator;
        public override IReadOnlyCollection<Vector3SByte> RemovedChunks => _removedChunks;

        public override bool IsChunkGenerated(Vector3SByte index)
        {
            if (!CanRegenerate) return true;
            lock (_genLock) return loadedChunks.Contains(index);
        }

        protected override bool CanBeDeletedWhenEmpty
        {
            get
            {
                if (!CanRegenerate) return true;
                lock (_genLock) return pendingChunks.Count == 0 && generatingChunks.Count == 0;
            }
        }

        public void MarkSavedWithoutGenerator()
        {
            _savedWithoutGenerator = true;
        }

        public void RestoreSavedState(IEnumerable<Vector3SByte> completeChunks, IEnumerable<Vector3SByte> removedChunks)
        {
            lock (_genLock)
            {
                foreach (var index in completeChunks)
                {
                    pendingChunks.Remove(index);
                    loadedChunks.Add(index);
                }

                foreach (var index in removedChunks)
                {
                    _removedChunks.Add(index);
                    pendingChunks.Remove(index);
                    loadedChunks.Add(index);
                }
            }
        }

        protected override void OnChunkAdded(Chunk chunk)
        {
            _removedChunks.Remove(chunk.PositionIndex);
        }

        protected override void OnChunkRemoved(Chunk chunk)
        {
            if (SavesOnlyChangedChunks && IsChunkGenerated(chunk.PositionIndex))
                _removedChunks.Add(chunk.PositionIndex);
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

            _chunksToCheck.Clear();
            lock (_genLock)
            {
                foreach (var idx in pendingChunks)
                {
                    if (!generatingChunks.Contains(idx) && !loadedChunks.Contains(idx))
                        _chunksToCheck.Add(idx);
                }
            }

            if (_chunksToCheck.Count > 0)
            {
                ScheduleChunks(cam.Position, visR2, frustum, true);
                ScheduleChunks(cam.Position, visR2, frustum, false);
            }
            base.Update();
        }

        private void ScheduleChunks(Vector3 cameraPosition, float radiusSquared, CameraFrustum frustum, bool visibleOnly)
        {
            for (int i = _chunksToCheck.Count - 1; i >= 0; i--)
            {
                var idx = _chunksToCheck[i];
                var bounds = ToWorldBounds(ComputeChunkBounds(idx));
                if ((bounds.Center - cameraPosition).LengthSquared > radiusSquared) continue;
                if (visibleOnly && !frustum.IsInFrustum(bounds)) continue;

                _chunksToCheck.RemoveAt(i);
                lock (_genLock)
                {
                    if (!generatingChunks.Add(idx)) continue;
                }

                GenerateChunkAsync(idx);
            }
        }

        public override void CollectStreamingWork(Vector3 cameraPosition, ref StreamingWork work)
        {
            base.CollectStreamingWork(cameraPosition, ref work);
            float radiusSquared = Settings.CHUNK_VISIBLE_RADIUS * Settings.CHUNK_VISIBLE_RADIUS;

            lock (_genLock)
            {
                work.Chunks += generatingChunks.Count;
                foreach (var idx in pendingChunks)
                {
                    if (loadedChunks.Contains(idx) || generatingChunks.Contains(idx)) continue;
                    var bounds = ToWorldBounds(ComputeChunkBounds(idx));
                    if ((bounds.Center - cameraPosition).LengthSquared <= radiusSquared) work.Chunks++;
                }
            }
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
                        if (TryGetChunk(idx, out var existing))
                        {
                            MergeGeneratedBlocks(existing, result.chunk);
                            result.chunk.Dispose();
                        }
                        else if (result.meshData.Mass > 0)
                        {
                            AddChunk(result.chunk, false);
                            result.chunk.ApplyMeshData(result.meshData);
                            RefreshEditedNeighbors(result.chunk);
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

        private static void MergeGeneratedBlocks(Chunk existing, Chunk generated)
        {
            bool changed = false;

            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        var current = existing.Blocks[x, y, z];
                        var natural = generated.Blocks[x, y, z];
                        if ((current == null || current.IsAir) && natural != null && !natural.IsAir)
                        {
                            existing.Blocks[x, y, z] = natural;
                            changed = true;
                        }
                    }
                }
            }

            if (!changed) return;

            existing.IsModified = true;
            existing.CalculateLighting();
            existing.QueueMeshUpdate(UpdateReason.Generation);
        }

        private static void RefreshEditedNeighbors(Chunk chunk)
        {
            bool hasEditedNeighbor = false;

            foreach (var neighbor in chunk.Neighbors.Values)
            {
                if (neighbor == null || !neighbor.ShouldBeSaved) continue;

                hasEditedNeighbor = true;
                neighbor.QueueMeshUpdate(UpdateReason.Generation);
            }

            if (hasEditedNeighbor)
                chunk.QueueMeshUpdate(UpdateReason.Generation);
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