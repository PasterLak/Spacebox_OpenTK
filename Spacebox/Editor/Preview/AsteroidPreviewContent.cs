using System.Collections.Concurrent;
using Engine;
using Engine.Multithreading;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Resource;

namespace Spacebox.Editor.Preview;

public sealed class AsteroidPreviewContent : IPreviewContent
{
    private readonly ConcurrentQueue<ChunkMeshData> _ready = new ConcurrentQueue<ChunkMeshData>();
    private readonly List<(Mesh Mesh, Matrix4 Model)> _meshes = new List<(Mesh, Matrix4)>();
    private readonly Dictionary<short, int> _blockCounts = new Dictionary<short, int>();
    private readonly object _countsLock = new object();
    private AsteroidGenerationContext _context = AsteroidGenerationContext.Legacy;
    private readonly Lazy<Game.Generation.Shapes.Asteroids.AsteroidSolidMask> _mask;

    public string ShapeDescription { get; }

    private static string Describe(Game.Generation.Shapes.Asteroids.AsteroidShape shape)
    {
        if (shape == null) return "Shape: classic (sphere noise)";

        string text = $"Shape: {shape.Kind}";
        if (shape.LobeCount > 0) text += $", {shape.LobeCount} lobes";
        if (shape.HasBite) text += ", bite";
        if (shape.CraterCount > 0) text += $", {shape.CraterCount} craters";
        return text;
    }
    private readonly BlockMaterial _material;
    private volatile bool _cancelled;
    private int _pendingChunks;

    public Vector3 Center => Vector3.Zero;
    public float Radius { get; }
    public bool IsReady => _meshes.Count > 0 || Volatile.Read(ref _pendingChunks) == 0;
    public bool IsFinished => Volatile.Read(ref _pendingChunks) == 0 && _ready.IsEmpty;
    public int TotalChunks { get; }
    public int GeneratedChunks => TotalChunks - Volatile.Read(ref _pendingChunks);

    public AsteroidPreviewContent(AsteroidData asteroid, int radius, int seed)
    {
        radius = Math.Max(2, radius);
        Radius = radius * 1.15f;
        _material = new BlockMaterial(GameAssets.BlocksTexture, GameAssets.EmissionBlocks);

        int diameter = radius * 2;
        AsteroidChunkBuilder.GetChunkRange(diameter, out int minChunk, out int maxChunk);
        var shape = AsteroidGenerationContext.CreateShape(GeneratorVersions.Current, asteroid, seed);
        ShapeDescription = Describe(shape);
        _context = new AsteroidGenerationContext(GeneratorVersions.Current, null, 0f, radius, shape);
        var generator = new AsteroidVoxelDataGenerator(new Vector3(diameter), 1f, asteroid, seed, _context);
        var context = _context;
        _mask = new Lazy<Game.Generation.Shapes.Asteroids.AsteroidSolidMask>(() => Game.Generation.Shapes.Asteroids.AsteroidSolidMaskBuilder.TryBuild(generator, asteroid, seed, context, diameter));

        for (int x = minChunk; x <= maxChunk; x++)
        for (int y = minChunk; y <= maxChunk; y++)
        for (int z = minChunk; z <= maxChunk; z++)
        {
            var index = new Vector3SByte((sbyte)x, (sbyte)y, (sbyte)z);
            TotalChunks++;
            Interlocked.Increment(ref _pendingChunks);
            WorkerPoolManager.Enqueue(_ => BuildChunk(generator, asteroid, seed, index), WorkerPoolManager.Priority.Medium);
        }
    }

    public IReadOnlyDictionary<short, int> GetBlockCounts()
    {
        lock (_countsLock) return new Dictionary<short, int>(_blockCounts);
    }

    public void Update(float deltaTime)
    {
        int budget = 8;
        while (budget-- > 0 && _ready.TryDequeue(out var data))
        {
            if (data.Vertices.Length == 0 || data.Indices.Length == 0) continue;
            var mesh = new Mesh(data.Vertices, data.Indices, BuffersData.CreateBlockBuffer());
            var origin = new Vector3(data.Index.X, data.Index.Y, data.Index.Z) * Chunk.Size;
            _meshes.Add((mesh, Matrix4.CreateTranslation(origin)));
        }
    }

    public void Render()
    {
        for (int i = 0; i < _meshes.Count; i++)
        {
            _material.Apply(_meshes[i].Model);
            _meshes[i].Mesh.Render();
        }
    }

    public void Dispose()
    {
        _cancelled = true;
        for (int i = 0; i < _meshes.Count; i++) _meshes[i].Mesh.Dispose();
        _meshes.Clear();
        while (_ready.TryDequeue(out _)) { }
    }

    private void BuildChunk(AsteroidVoxelDataGenerator generator, AsteroidData asteroid, int seed, Vector3SByte index)
    {
        try
        {
            if (_cancelled) return;

            var padded = AsteroidChunkBuilder.BuildPaddedBlocks(generator, asteroid, seed, seed, index, _context, _mask.Value);
            CountBlocks(padded);
            var meshData = new MeshGenerator(index, padded, false).GenerateMeshData();
            PaddedArrayPools.Blocks.Return(padded);
            if (!_cancelled) _ready.Enqueue(new ChunkMeshData(index, meshData.Vertices.AsSpan(0, meshData.VertexCount).ToArray(), meshData.Indices.AsSpan(0, meshData.IndexCount).ToArray()));
            meshData.Release();
        }
        catch (Exception ex)
        {
            Debug.Error($"[Editor] Asteroid preview chunk {index} failed: {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref _pendingChunks);
        }
    }

    private void CountBlocks(Game.Generation.Blocks.Block[,,] padded)
    {
        var local = new Dictionary<short, int>();
        for (int x = 1; x <= Chunk.Size; x++)
        for (int y = 1; y <= Chunk.Size; y++)
        for (int z = 1; z <= Chunk.Size; z++)
        {
            var block = padded[x, y, z];
            if (block == null || block.IsAir) continue;
            local.TryGetValue(block.Id, out int count);
            local[block.Id] = count + 1;
        }

        lock (_countsLock)
        {
            foreach (var pair in local)
            {
                _blockCounts.TryGetValue(pair.Key, out int count);
                _blockCounts[pair.Key] = count + pair.Value;
            }
        }
    }

    private readonly record struct ChunkMeshData(Vector3SByte Index, float[] Vertices, uint[] Indices);
}
