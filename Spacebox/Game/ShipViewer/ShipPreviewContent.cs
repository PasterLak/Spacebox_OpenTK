using System.Collections.Concurrent;
using Engine;
using Engine.Multithreading;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Editor.Preview;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipPreviewContent : IPreviewContent
{
    private const int UploadsPerFrame = 8;

    private readonly ConcurrentQueue<ChunkMeshData> _ready = new ConcurrentQueue<ChunkMeshData>();
    private readonly List<(Mesh Mesh, Matrix4 Model)> _meshes = new List<(Mesh, Matrix4)>();
    private readonly BlockMaterial _material;
    private volatile bool _cancelled;
    private volatile string _error = "";
    private int _pendingJobs;
    private int _totalChunks = -1;
    private int _meshedChunks;

    public ShipPreviewContent(Func<CompoundTag> loadTag, ShipFraming framing)
    {
        Center = framing.Center;
        Radius = framing.Radius;
        _material = new BlockMaterial(GameAssets.BlocksTexture, GameAssets.EmissionBlocks);

        Interlocked.Increment(ref _pendingJobs);
        WorkerPoolManager.Enqueue(_ => Load(loadTag), WorkerPoolManager.Priority.Medium);
    }

    public Vector3 Center { get; }
    public float Radius { get; }
    public Vector2 ViewAngles => new Vector2(40f, 25f);
    public bool IsBusy => Volatile.Read(ref _pendingJobs) > 0;
    public bool IsReady => _meshes.Count > 0 || !IsBusy;
    public bool IsFinished => !IsBusy && _ready.IsEmpty;
    public string Error => _error;
    public int TotalChunks => Volatile.Read(ref _totalChunks);
    public int MeshedChunks => Volatile.Read(ref _meshedChunks);

    public void Update(float deltaTime)
    {
        int budget = UploadsPerFrame;
        while (budget-- > 0 && _ready.TryDequeue(out var data))
        {
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

    private void Load(Func<CompoundTag> loadTag)
    {
        try
        {
            if (_cancelled) return;

            var tag = loadTag();
            var chunks = new Dictionary<Vector3SByte, Block[,,]>();
            foreach (var chunkTag in ChunkTagAccess.Chunks(tag))
            {
                if (_cancelled) return;
                var blocks = NBTHelper.TagToChunkBlocks(chunkTag, out var index);
                chunks[index] = blocks;
            }

            Volatile.Write(ref _totalChunks, chunks.Count);
            var air = GameAssets.CreateBlockFromId(0);
            foreach (var index in chunks.Keys)
            {
                if (_cancelled) return;
                Interlocked.Increment(ref _pendingJobs);
                var chunkIndex = index;
                WorkerPoolManager.Enqueue(_ => MeshChunk(chunks, chunkIndex, air), WorkerPoolManager.Priority.Medium);
            }
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            Debug.Error($"[ShipViewer] Ship could not be loaded: {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref _pendingJobs);
        }
    }

    private void MeshChunk(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Vector3SByte index, Block air)
    {
        try
        {
            if (_cancelled) return;

            var padded = ShipChunkPadding.Build(chunks, index, air);
            try
            {
                var meshData = new MeshGenerator(index, padded, false).GenerateMeshData();
                try
                {
                    if (!_cancelled && meshData.VertexCount > 0 && meshData.IndexCount > 0)
                        _ready.Enqueue(new ChunkMeshData(index, meshData.Vertices.AsSpan(0, meshData.VertexCount).ToArray(), meshData.Indices.AsSpan(0, meshData.IndexCount).ToArray()));
                }
                finally
                {
                    meshData.Release();
                }
            }
            finally
            {
                PaddedArrayPools.Blocks.Return(padded);
            }

            Interlocked.Increment(ref _meshedChunks);
        }
        catch (Exception ex)
        {
            Debug.Error($"[ShipViewer] Chunk {index} could not be meshed: {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref _pendingJobs);
        }
    }

    private readonly record struct ChunkMeshData(Vector3SByte Index, float[] Vertices, uint[] Indices);
}
