using System.Collections.Concurrent;
using Engine;
using Engine.Multithreading;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Game.Ships.Sharing;

public sealed class ShipModelMeshes : IDisposable
{
    private readonly ConcurrentQueue<(Vector3SByte Index, float[] Vertices, uint[] Indices)> _ready = new ConcurrentQueue<(Vector3SByte, float[], uint[])>();
    private readonly List<(Mesh Mesh, Matrix4 Offset)> _meshes = new List<(Mesh, Matrix4)>();
    private volatile bool _cancelled;
    private volatile bool _failed;
    private volatile bool _hasFrame;
    private Quaternion _frame = Quaternion.Identity;
    private int _total = -1;
    private int _received;

    public ShipModelMeshes(CompoundTag entity)
    {
        var air = GameAssets.CreateBlockFromId(0);
        WorkerPoolManager.Enqueue(_ => Load(entity, air), WorkerPoolManager.Priority.Medium);
    }

    public bool IsFailed => _failed;

    public bool TryGetFrame(out Quaternion frame)
    {
        frame = _hasFrame ? _frame : Quaternion.Identity;
        return _hasFrame;
    }

    public bool IsComplete
    {
        get
        {
            int total = Volatile.Read(ref _total);
            return total >= 0 && _received == total && !_failed;
        }
    }

    public void Upload(int budget)
    {
        while (budget-- > 0 && _ready.TryDequeue(out var data))
        {
            _received++;
            if (data.Vertices == null) continue;
            var mesh = new Mesh(data.Vertices, data.Indices, BuffersData.CreateBlockBuffer());
            _meshes.Add((mesh, Matrix4.CreateTranslation(new Vector3(data.Index.X, data.Index.Y, data.Index.Z) * Chunk.Size)));
        }
    }

    public void Render(BlockMaterial material, Matrix4 transform)
    {
        for (int i = 0; i < _meshes.Count; i++)
        {
            material.Apply(_meshes[i].Offset * transform);
            _meshes[i].Mesh.Render();
        }
    }

    public void Dispose()
    {
        _cancelled = true;
        foreach (var entry in _meshes) entry.Mesh.Dispose();
        _meshes.Clear();
        while (_ready.TryDequeue(out _)) { }
    }

    private void Load(CompoundTag entity, Block air)
    {
        try
        {
            var chunks = ShipChunkMesher.ReadChunks(entity, () => _cancelled);
            if (_cancelled) return;
            var parts = ShipViewerParts.Scan(chunks);
            _frame = ShipPlacementRotation.FromViewFrame(parts.Orientation.Resolve(parts.Extent));
            _hasFrame = true;
            if (LightManager.EnableLighting) ShipLightFlood.Propagate(chunks);
            Volatile.Write(ref _total, chunks.Count);
            foreach (var index in chunks.Keys)
            {
                var chunkIndex = index;
                WorkerPoolManager.Enqueue(_ => MeshChunk(chunks, chunkIndex, air), WorkerPoolManager.Priority.Medium);
            }
        }
        catch (Exception ex)
        {
            _failed = true;
            Debug.Error($"[ShipPlacement] Ship model could not be loaded: {ex.Message}");
        }
    }

    private void MeshChunk(Dictionary<Vector3SByte, Block[,,]> chunks, Vector3SByte index, Block air)
    {
        if (_cancelled) return;
        try
        {
            var mesh = ShipChunkMesher.Mesh(chunks, index, air);
            if (!_cancelled) _ready.Enqueue((index, mesh.Vertices, mesh.Indices));
        }
        catch (Exception ex)
        {
            _failed = true;
            Debug.Error($"[ShipPlacement] Ship chunk {index} could not be meshed: {ex.Message}");
        }
    }
}
