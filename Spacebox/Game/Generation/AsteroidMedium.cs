using Engine;
using Engine.Multithreading;
using Engine.Physics;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Structures;
using Spacebox.Generation;

namespace Spacebox.Game.Generation;

public class AsteroidMedium : Asteroid
{
    private int ChunkCount = 2;
    private readonly AsteroidVoxelDataGenerator voxelGen;
    private readonly object _genLock = new();
    private readonly HashSet<Vector3SByte> pendingChunks = new HashSet<Vector3SByte>();
    private readonly HashSet<Vector3SByte> generatingChunks = new();
    private readonly HashSet<Vector3SByte> loadedChunks = new HashSet<Vector3SByte>();

    private readonly Dictionary<Vector3SByte, List<Vector3i>> _worm = new ();
    private readonly bool useWorms = false;
    private readonly AsteroidData asteroidData;
    public AsteroidMedium(NotGeneratedEntity data, Sector sector)
        : base(data.Id, data.positionWorld, sector)
    {
        asteroidData = data.asteroid;
        ChunkCount = asteroidData.SizeInChunks;
        Vector3 diameter = new Vector3(ChunkCount * Chunk.Size);

 

        voxelGen = new AsteroidVoxelDataGenerator( // 1 33 4 1 seed
            asteroidDimensions: diameter,
            blockSize: 1f,
           asteroidData,
            seed: (int)Seed
        );
   
        int worldVox = Chunk.Size * ChunkCount;

        useWorms = asteroidData.UsePerlinWorms;
        if (useWorms)
        {

            // asteroid 7 max
            var wormParams = new WormParameters((byte)(3 * ChunkCount),
                asteroidData.WormSettings.Count,
                asteroidData.WormSettings.PathDeviation,
                (byte)(MathHelper.Min(ChunkCount * Chunk.Size, 255)), 
                ComputeStep(asteroidData.WormSettings.DiameterInBlocks, asteroidData.WormSettings.StepSize), (int)Seed);
            // 4 worms 0.5 deviation  4 diameter 1 overlapK(step size)
            // var tunnelMap = new Dictionary<Vector3SByte, List<Vector3i>>(128);
            foreach (var (c, v) in PerlinWorms.Voxels(wormParams, ChunkCount, Chunk.Size))
            {
                if (!_worm.TryGetValue(c, out var lst))
                    _worm[c] = lst = new List<Vector3i>(32);
                lst.Add(v);
            }

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
        Vector3 min = PositionWorld + new Vector3(idx.X * Chunk.Size, idx.Y * Chunk.Size, idx.Z * Chunk.Size);
        Vector3 max = min + new Vector3(Chunk.Size);
        Vector3 center = (min + max) * 0.5f;
        return new BoundingBox(center, new Vector3(Chunk.Size));
    }

    private Chunk CreateChunk(Vector3SByte idx)
    {
        var chunk = new Chunk(idx, this, true);
        var data = voxelGen.GenerateDataForChunk(idx);

        var chunkSeed = SeedHelper.GetChunkIdInt(EntityID, idx);

        if (useWorms && _worm != null && _worm.TryGetValue(idx, out var list))
            foreach (var v in list) data[v.X, v.Y, v.Z] = 0;


        var oreGen = new AsteroidOreGenerator(asteroidData,chunkSeed );

        oreGen.ApplyOres(ref data);

        for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
                for (int z = 0; z < Chunk.Size; z++)
                    chunk.Blocks[x, y, z] = GameAssets.CreateBlockFromId((short)data[x, y, z]);
        return chunk;
    }
}
