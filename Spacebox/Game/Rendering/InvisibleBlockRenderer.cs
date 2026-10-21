using Engine;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Rendering;

public sealed class InvisibleBlockRenderer : IDisposable
{
    public const float ViewDistance = 96f;
    private const string ShaderPath = "Resources/Shaders/movingBlock";

    private sealed class ChunkCells
    {
        public int Version;
        public bool Seen;
        public readonly List<(Vector3i Cell, short Id)> Cells = new List<(Vector3i, short)>();
    }

    private readonly SpaceEntity _entity;
    private readonly Dictionary<Chunk, ChunkCells> _chunks = new Dictionary<Chunk, ChunkCells>();
    private readonly List<Chunk> _stale = new List<Chunk>();
    private readonly HashSet<Vector3i> _occupied = new HashSet<Vector3i>();
    private readonly List<float> _vertices = new List<float>();
    private readonly List<uint> _indices = new List<uint>();
    private MeshBuffer _buffer;
    private TextureMaterial _material;
    private int _indexCount;
    private bool _dirty = true;

    public InvisibleBlockRenderer(SpaceEntity entity)
    {
        _entity = entity;
    }

    public void Render()
    {
        if (Application.IsHeadless || Camera.Main == null || !IsNear(Camera.Main.PositionWorld)) return;

        Refresh();
        if (_dirty) Rebuild();

        var texture = GameAssets.BlocksTexture;
        if (_indexCount == 0 || texture == null) return;

        _material ??= new TextureMaterial(texture, Resources.Load<Shader>(ShaderPath, true)) { WritesGBuffer = true, RenderFace = RenderFace.Both };
        _material.MainTexture = texture;
        _material.ExtraLight = Vector3.One;
        _material.Apply(_entity.RotationMatrix * Matrix4.CreateTranslation(RenderSpace.ToRender(_entity.PositionWorld)));

        GL.BindVertexArray(_buffer.VAO);
        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        _buffer?.Dispose();
        _buffer = null;
        _chunks.Clear();
        _indexCount = 0;
    }

    private bool IsNear(Vector3 cameraWorld)
    {
        var box = _entity.GeometryBoundingBox;
        float radius = box.Size.Length * 0.5f;
        return Vector3.Distance(_entity.ToWorldSpace(box.Center), cameraWorld) - radius <= ViewDistance;
    }

    private void Refresh()
    {
        foreach (var cached in _chunks.Values) cached.Seen = false;

        var chunks = _entity.Chunks;
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            if (!_chunks.TryGetValue(chunk, out var cached))
            {
                cached = new ChunkCells { Version = -1 };
                _chunks[chunk] = cached;
            }

            cached.Seen = true;
            if (cached.Version == chunk.DataVersion) continue;

            Scan(chunk, cached);
            _dirty = true;
        }

        _stale.Clear();
        foreach (var pair in _chunks)
        {
            if (!pair.Value.Seen) _stale.Add(pair.Key);
        }

        for (int i = 0; i < _stale.Count; i++)
        {
            if (_chunks[_stale[i]].Cells.Count > 0) _dirty = true;
            _chunks.Remove(_stale[i]);
        }
    }

    private static void Scan(Chunk chunk, ChunkCells cached)
    {
        cached.Version = chunk.DataVersion;
        cached.Cells.Clear();

        var blocks = chunk.Blocks;
        for (byte x = 0; x < Chunk.Size; x++)
        for (byte y = 0; y < Chunk.Size; y++)
        for (byte z = 0; z < Chunk.Size; z++)
        {
            if (blocks[x, y, z] is InvisibleBlock block) cached.Cells.Add((chunk.CellInEntity(x, y, z), block.Id));
        }
    }

    private void Rebuild()
    {
        _dirty = false;
        _occupied.Clear();
        foreach (var cached in _chunks.Values)
        {
            for (int i = 0; i < cached.Cells.Count; i++) _occupied.Add(cached.Cells[i].Cell);
        }

        _vertices.Clear();
        _indices.Clear();
        foreach (var cached in _chunks.Values)
        {
            for (int i = 0; i < cached.Cells.Count; i++)
                InvisibleBlockMesh.AddCell(cached.Cells[i].Cell, GameAssets.GetBlockDataById(cached.Cells[i].Id), _occupied, _vertices, _indices);
        }

        _indexCount = _indices.Count;
        if (_indexCount == 0) return;

        if (_buffer == null)
        {
            _buffer = TextureMaterial.GetMeshBuffer();
            Resources.RemoveResourceFromControll(_buffer);
        }

        var vertices = _vertices.ToArray();
        var indices = _indices.ToArray();
        _buffer.BindBuffer(ref vertices, ref indices);
        _buffer.SetAttributes();
    }
}
