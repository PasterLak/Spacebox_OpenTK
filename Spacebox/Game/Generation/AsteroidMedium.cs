using Engine;
using Engine.Multithreading;
using Engine.Physics;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Generation;

namespace Spacebox.Game.Generation
{
    public class AsteroidMedium : Asteroid
    {
        public const int ChunkCount = 4;
        private readonly AsteroidVoxelDataGenerator voxelGen;
        private readonly int chunkSize;
        private readonly object _genLock = new();
        private readonly HashSet<Vector3SByte> pendingChunks = new HashSet<Vector3SByte>();
        private readonly HashSet<Vector3SByte> generatingChunks = new();
        private readonly HashSet<Vector3SByte> loadedChunks = new HashSet<Vector3SByte>();

        readonly Dictionary<Vector3SByte, List<Vector3i>> _worm = new Dictionary<Vector3SByte, List<Vector3i>>();
        public AsteroidMedium(ulong id, Vector3 positionWorld, Sector sector)
            : base(id, positionWorld, sector)
        {
            Vector3 diameter = new Vector3(ChunkCount * ChunkSize);
            voxelGen = new AsteroidVoxelDataGenerator(
                asteroidDimensions: diameter,
                blockSize: 1f,
                threshold: 33,
                noiseOctaves: 3,
                noiseScale: 1f,
                seed: (int)Seed,
                type: AsteroidType.Medium
            );
            chunkSize = Chunk.Size;


            int worldVox = Chunk.Size * AsteroidMedium.ChunkCount;
            var wormParams = new WormParameters(3 * ChunkCount, 4, 0.5f, ChunkCount * ChunkSize, ComputeStep(4, 1), (int)Seed);

           // var tunnelMap = new Dictionary<Vector3SByte, List<Vector3i>>(128);
            foreach (var (c, v) in PerlinWorms.Voxels(wormParams, ChunkCount, Chunk.Size))
            {
                if (!_worm.TryGetValue(c, out var lst)) 
                    _worm[c] = lst = new List<Vector3i>(32);
                lst.Add(v);
            }
         
           // _worm = tunnelMap;
        }
        static float ComputeStep(byte wormDiameter, float overlapK = 0.75f) // 1=  fast, big steps| 0 = slow, smooth steps
        {
            float step = (wormDiameter * 0.5f) * overlapK;
            return step < 1f ? 1f : step;
        }

        public override void OnGenerate()
        {
            int half = ChunkCount / 2;
            for (int x = -half; x < half; x++)
                for (int y = -half; y < half; y++)
                    for (int z = -half; z < half; z++)
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
                if ((bounds.Center - cam.Position).LengthSquared > visR2 ||
                    !frustum.IsInFrustum(bounds))
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
            WorkerPoolManager
                .Enqueue(token =>
                {
                    try
                    {
                        var chunk = CreateChunk(idx);

                        MainThreadDispatcher.Instance.Enqueue(() =>
                        {
                            AddChunk(chunk, false);
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

                        Debug.Error($"AsteroidMedium chunk {idx} generation failed: {ex}");
                        lock (_genLock)
                        {
                            generatingChunks.Remove(idx);
                        }
                    }
                },
                WorkerPoolManager.Priority.High
                );
        }


        private BoundingBox ComputeChunkBounds(Vector3SByte idx)
        {
            Vector3 min = PositionWorld + new Vector3(idx.X * chunkSize, idx.Y * chunkSize, idx.Z * chunkSize);
            Vector3 max = min + new Vector3(chunkSize);
            Vector3 center = (min + max) * 0.5f;
            return new BoundingBox(center, new Vector3(chunkSize));
        }

        private Chunk CreateChunk(Vector3SByte idx)
        {
            var chunk = new Chunk(idx, this, true);
            var data = voxelGen.GenerateDataForChunk(idx, chunkSize);

            var chunkSeed = SeedHelper.GetChunkIdInt(EntityID, idx);

            if (_worm != null && _worm.TryGetValue(idx, out var list))
                foreach (var v in list) data[v.X, v.Y, v.Z] = 0;


            var oreGen = new AsteroidOreGenerator(new AsteroidOreGeneratorParameters(
                outerOreVeinChance: 0.02f,
                outerOreMaxVeinSize: 10,
                outerOreIds: new[] { 4, 5, 6, 7 },
                middleOreVeinChance: 0.01f,
                middleOreMaxVeinSize: 6,
                middleOreIds: new[] { 8, 9, 11 },
                deepOreVeinChance: 0.008f,
                deepOreMaxVeinSize: 1,
                deepOreIds: new[] { 10 },
                oreSeed: chunkSeed
            ));
            oreGen.ApplyOresToChunk(ref data, Spacebox.Generation.AsteroidType.Medium);
            for (int x = 0; x < chunkSize; x++)
                for (int y = 0; y < chunkSize; y++)
                    for (int z = 0; z < chunkSize; z++)
                        chunk.Blocks[x, y, z] = GameAssets.CreateBlockFromId((short)data[x, y, z]);
            return chunk;
        }
    }
}
