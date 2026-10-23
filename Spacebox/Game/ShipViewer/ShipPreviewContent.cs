using System.Collections.Concurrent;
using Engine;
using Engine.Multithreading;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Editor.Preview;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipPreviewContent : IPreviewContent, IPreviewClickTarget
{
    private const int UploadsPerFrame = 8;
    private const float ShotRangeInRadii = 2f;

    private readonly ConcurrentQueue<ChunkMeshData> _ready = new ConcurrentQueue<ChunkMeshData>();
    private readonly Dictionary<Vector3SByte, (Mesh Mesh, Matrix4 Model)> _meshes = new Dictionary<Vector3SByte, (Mesh, Matrix4)>();
    private readonly Dictionary<Vector3SByte, (Mesh Mesh, Matrix4 Model)> _staged = new Dictionary<Vector3SByte, (Mesh, Matrix4)>();
    private readonly Func<CompoundTag> _loadTag;
    private readonly BlockMaterial _material;
    private readonly ShipViewerEffects _effects = new ShipViewerEffects();
    private readonly ShipTargetPractice _targetPractice;
    private readonly object _remeshLock = new object();
    private volatile bool _cancelled;
    private volatile bool _powered = true;
    private volatile string _error = "";
    private volatile ShipViewerParts _parts;
    private volatile Dictionary<Vector3SByte, Block[,,]> _chunks;
    private volatile MeshPass _stagedPass;
    private bool _effectsReady;
    private int _generation;
    private int _pendingJobs;
    private int _totalChunks = -1;
    private int _meshedChunks;
    private Matrix4 _view = Matrix4.Identity;

    public ShipPreviewContent(Func<CompoundTag> loadTag, ShipFraming framing)
    {
        Center = framing.Center;
        Radius = framing.Radius;
        _loadTag = loadTag;
        _material = new BlockMaterial(GameAssets.BlocksTexture, GameAssets.EmissionBlocks);
        _targetPractice = new ShipTargetPractice(_effects);

        Interlocked.Increment(ref _pendingJobs);
        WorkerPoolManager.Enqueue(_ => Load(MeshPassKind.Initial), WorkerPoolManager.Priority.Medium);
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
    public ProjectileParameters? ShotProjectile { get; set; }
    public bool HasEngines => _effectsReady && _effects.HasEngines;
    public bool HasCannons => _effectsReady && _effects.HasCannons;
    public bool CanRestore => _chunks != null;
    public bool AcceptsClicks => ShotProjectile.HasValue && _chunks != null;

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

    public void Click(Vector3 origin, Vector3 direction)
    {
        var chunks = _chunks;
        if (chunks == null || !ShotProjectile.HasValue) return;

        float range = (origin - Center).Length + Radius * ShotRangeInRadii;
        _targetPractice.Fire(chunks, _view, range, origin, direction, ShotProjectile.Value);
    }

    public void Restore()
    {
        _targetPractice.Clear();
        if (_chunks == null || _cancelled) return;

        Interlocked.Increment(ref _pendingJobs);
        WorkerPoolManager.Enqueue(_ => Load(MeshPassKind.Staged), WorkerPoolManager.Priority.Medium);
    }

    public void Update(float deltaTime)
    {
        ConfigureEffects();
        UploadMeshes();
        _targetPractice.Update(deltaTime, RemeshDamaged);
        _effects.Update(deltaTime, EnginesOn, AutoFire);
    }

    public void Render()
    {
        foreach (var entry in _meshes.Values)
        {
            _material.Apply(entry.Model);
            entry.Mesh.Render();
        }

        _effects.Render();
    }

    public void Dispose()
    {
        _cancelled = true;
        DisposeMeshes(_meshes);
        DisposeMeshes(_staged);
        while (_ready.TryDequeue(out _)) { }
        _targetPractice.Clear();
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
        int generation = Volatile.Read(ref _generation);
        int budget = UploadsPerFrame;
        while (budget-- > 0 && _ready.TryDequeue(out var data))
        {
            if (data.Pass.Generation != generation) continue;

            var target = data.Pass.Kind == MeshPassKind.Staged && !data.Pass.Swapped ? _staged : _meshes;
            if (target.Remove(data.Index, out var old)) old.Mesh.Dispose();
            if (data.Vertices == null) continue;

            var mesh = new Mesh(data.Vertices, data.Indices, BuffersData.CreateBlockBuffer());
            var origin = new Vector3(data.Index.X, data.Index.Y, data.Index.Z) * Chunk.Size;
            target[data.Index] = (mesh, Matrix4.CreateTranslation(origin) * _view);
        }

        var staged = _stagedPass;
        if (staged == null || staged.Generation != generation || staged.Remaining > 0 || !_ready.IsEmpty) return;

        DisposeMeshes(_meshes);
        foreach (var pair in _staged) _meshes[pair.Key] = pair.Value;
        _staged.Clear();
        staged.Swapped = true;
        _stagedPass = null;
    }

    private static void DisposeMeshes(Dictionary<Vector3SByte, (Mesh Mesh, Matrix4 Model)> meshes)
    {
        foreach (var entry in meshes.Values) entry.Mesh.Dispose();
        meshes.Clear();
    }

    private void Load(MeshPassKind kind)
    {
        try
        {
            if (_cancelled) return;

            var chunks = ShipChunkMesher.ReadChunks(_loadTag(), () => _cancelled);
            if (_cancelled) return;

            if (LightManager.EnableLighting) ShipLightFlood.Propagate(chunks);
            if (kind == MeshPassKind.Initial)
            {
                var parts = ShipViewerParts.Scan(chunks);
                _view = BuildView(parts.Orientation.Resolve(parts.Extent));
                _parts = parts;
                Volatile.Write(ref _totalChunks, chunks.Count);
            }

            _chunks = chunks;
            Remesh(chunks, kind);
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
            Remesh(chunks, MeshPassKind.Staged);
        }
        finally
        {
            Interlocked.Decrement(ref _pendingJobs);
        }
    }

    private void Remesh(Dictionary<Vector3SByte, Block[,,]> chunks, MeshPassKind kind)
    {
        lock (_remeshLock)
        {
            if (_cancelled) return;

            SetPower(chunks, _powered);
            var pass = new MeshPass(chunks.Count, Interlocked.Increment(ref _generation), kind);
            if (kind == MeshPassKind.Staged) _stagedPass = pass;
            EnqueueMeshJobs(chunks, chunks.Keys, pass);
        }
    }

    private void RemeshDamaged(HashSet<Vector3SByte> affected)
    {
        var chunks = _chunks;
        if (chunks == null || _cancelled) return;

        Interlocked.Increment(ref _pendingJobs);
        WorkerPoolManager.Enqueue(_ => RemeshDamaged(chunks, affected), WorkerPoolManager.Priority.Medium);
    }

    private void RemeshDamaged(Dictionary<Vector3SByte, Block[,,]> chunks, HashSet<Vector3SByte> affected)
    {
        try
        {
            lock (_remeshLock)
            {
                if (_cancelled || !ReferenceEquals(chunks, _chunks)) return;

                if (LightManager.EnableLighting) ShipLightFlood.Propagate(chunks, affected);
                EnqueueMeshJobs(chunks, affected, new MeshPass(affected.Count, Volatile.Read(ref _generation), MeshPassKind.Partial));
            }
        }
        finally
        {
            Interlocked.Decrement(ref _pendingJobs);
        }
    }

    private void EnqueueMeshJobs(Dictionary<Vector3SByte, Block[,,]> chunks, IEnumerable<Vector3SByte> indices, MeshPass pass)
    {
        var air = GameAssets.CreateBlockFromId(0);
        foreach (var index in indices)
        {
            Interlocked.Increment(ref _pendingJobs);
            var chunkIndex = index;
            WorkerPoolManager.Enqueue(_ => MeshChunk(chunks, chunkIndex, air, pass), WorkerPoolManager.Priority.Medium);
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
            if (_cancelled || pass.Generation != Volatile.Read(ref _generation)) return;

            var mesh = ShipChunkMesher.Mesh(chunks, index, air);
            if (!_cancelled) _ready.Enqueue(new ChunkMeshData(index, mesh.Vertices, mesh.Indices, pass));

            if (pass.Kind == MeshPassKind.Initial) Interlocked.Increment(ref _meshedChunks);
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

    private enum MeshPassKind
    {
        Initial,
        Staged,
        Partial
    }

    private sealed class MeshPass
    {
        private int _remaining;

        public MeshPass(int chunks, int generation, MeshPassKind kind)
        {
            _remaining = chunks;
            Generation = generation;
            Kind = kind;
        }

        public int Generation { get; }
        public MeshPassKind Kind { get; }
        public bool Swapped { get; set; }
        public int Remaining => Volatile.Read(ref _remaining);

        public void Complete()
        {
            Interlocked.Decrement(ref _remaining);
        }
    }

    private readonly record struct ChunkMeshData(Vector3SByte Index, float[] Vertices, uint[] Indices, MeshPass Pass);
}
