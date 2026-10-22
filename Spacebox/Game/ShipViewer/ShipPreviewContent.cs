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
    private readonly List<(Mesh Mesh, Matrix4 Model)> _staged = new List<(Mesh, Matrix4)>();
    private readonly BlockMaterial _material;
    private readonly ShipViewerEffects _effects = new ShipViewerEffects();
    private readonly object _remeshLock = new object();
    private volatile bool _cancelled;
    private volatile bool _powered = true;
    private volatile string _error = "";
    private volatile ShipViewerParts _parts;
    private volatile Dictionary<Vector3SByte, Block[,,]> _chunks;
    private volatile MeshPass _pass;
    private bool _effectsReady;
    private int _pendingJobs;
    private int _totalChunks = -1;
    private int _meshedChunks;
    private Matrix4 _view = Matrix4.Identity;

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
    public bool Powered => _powered;
    public bool EnginesOn { get; set; }
    public bool AutoFire { get; set; }
    public bool HasEngines => _effectsReady && _effects.HasEngines;
    public bool HasCannons => _effectsReady && _effects.HasCannons;

    public void SetPowered(bool powered)
    {
        if (_powered == powered) return;

        _powered = powered;
        var chunks = _chunks;
        if (chunks == null || _cancelled) return;

        Interlocked.Increment(ref _pendingJobs);
        WorkerPoolManager.Enqueue(_ => Remesh(chunks), WorkerPoolManager.Priority.Medium);
    }

    public void FireWeapons()
    {
        if (_effectsReady) _effects.FireAll();
    }

    public void Update(float deltaTime)
    {
        ConfigureEffects();
        UploadMeshes();
        _effects.Update(deltaTime, EnginesOn, AutoFire);
    }

    public void Render()
    {
        for (int i = 0; i < _meshes.Count; i++)
        {
            _material.Apply(_meshes[i].Model);
            _meshes[i].Mesh.Render();
        }

        _effects.Render();
    }

    public void Dispose()
    {
        _cancelled = true;
        DisposeMeshes(_meshes);
        DisposeMeshes(_staged);
        while (_ready.TryDequeue(out _)) { }
        _effects.Dispose();
    }

    private void ConfigureEffects()
    {
        if (_effectsReady) return;

        var parts = _parts;
        if (parts == null) return;

        _effects.SetParts(parts, _view, Radius);
        _effectsReady = true;
    }

    private void UploadMeshes()
    {
        var current = _pass;
        int budget = UploadsPerFrame;
        while (budget-- > 0 && _ready.TryDequeue(out var data))
        {
            if (!ReferenceEquals(data.Pass, current)) continue;

            var mesh = new Mesh(data.Vertices, data.Indices, BuffersData.CreateBlockBuffer());
            var origin = new Vector3(data.Index.X, data.Index.Y, data.Index.Z) * Chunk.Size;
            (data.Pass.Progressive ? _meshes : _staged).Add((mesh, Matrix4.CreateTranslation(origin) * _view));
        }

        if (current == null || current.Progressive || current.Remaining > 0 || !_ready.IsEmpty) return;

        DisposeMeshes(_meshes);
        _meshes.AddRange(_staged);
        _staged.Clear();
        current.Progressive = true;
    }

    private static void DisposeMeshes(List<(Mesh Mesh, Matrix4 Model)> meshes)
    {
        for (int i = 0; i < meshes.Count; i++) meshes[i].Mesh.Dispose();
        meshes.Clear();
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

            if (LightManager.EnableLighting) ShipLightFlood.Propagate(chunks);
            var parts = ShipViewerParts.Scan(chunks);
            _view = BuildView(parts.Orientation.Resolve(parts.Extent));
            _parts = parts;
            Volatile.Write(ref _totalChunks, chunks.Count);
            _chunks = chunks;
            Remesh(chunks, true);
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

    private void Remesh(Dictionary<Vector3SByte, Block[,,]> chunks)
    {
        try
        {
            Remesh(chunks, false);
        }
        finally
        {
            Interlocked.Decrement(ref _pendingJobs);
        }
    }

    private void Remesh(Dictionary<Vector3SByte, Block[,,]> chunks, bool progressive)
    {
        lock (_remeshLock)
        {
            if (_cancelled) return;

            SetPower(chunks, _powered);
            var pass = new MeshPass(chunks.Count, progressive);
            _pass = pass;
            var air = GameAssets.CreateBlockFromId(0);
            foreach (var index in chunks.Keys)
            {
                Interlocked.Increment(ref _pendingJobs);
                var chunkIndex = index;
                WorkerPoolManager.Enqueue(_ => MeshChunk(chunks, chunkIndex, air, pass), WorkerPoolManager.Priority.Medium);
            }
        }
    }

    private static void SetPower(Dictionary<Vector3SByte, Block[,,]> chunks, bool powered)
    {
        foreach (var blocks in chunks.Values)
        {
            foreach (var block in blocks)
            {
                if (block is ElectricalBlock electrical) electrical.SetActiveVisual(powered);
            }
        }
    }

    private Matrix4 BuildView(Matrix3 rotation)
    {
        return Matrix4.CreateTranslation(-Center) * new Matrix4(rotation) * Matrix4.CreateTranslation(Center);
    }

    private void MeshChunk(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Vector3SByte index, Block air, MeshPass pass)
    {
        try
        {
            if (_cancelled || !ReferenceEquals(pass, _pass)) return;

            var padded = ShipChunkPadding.Build(chunks, index, air);
            try
            {
                var meshData = new MeshGenerator(index, padded, false).GenerateMeshData();
                try
                {
                    if (!_cancelled && meshData.VertexCount > 0 && meshData.IndexCount > 0)
                        _ready.Enqueue(new ChunkMeshData(index, meshData.Vertices.AsSpan(0, meshData.VertexCount).ToArray(), meshData.Indices.AsSpan(0, meshData.IndexCount).ToArray(), pass));
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

            if (pass.Progressive) Interlocked.Increment(ref _meshedChunks);
        }
        catch (Exception ex)
        {
            Debug.Error($"[ShipViewer] Chunk {index} could not be meshed: {ex.Message}");
        }
        finally
        {
            pass.Complete();
            Interlocked.Decrement(ref _pendingJobs);
        }
    }

    private sealed class MeshPass
    {
        private int _remaining;

        public MeshPass(int chunks, bool progressive)
        {
            _remaining = chunks;
            Progressive = progressive;
        }

        public bool Progressive { get; set; }
        public int Remaining => Volatile.Read(ref _remaining);

        public void Complete()
        {
            Interlocked.Decrement(ref _remaining);
        }
    }

    private readonly record struct ChunkMeshData(Vector3SByte Index, float[] Vertices, uint[] Indices, MeshPass Pass);
}
