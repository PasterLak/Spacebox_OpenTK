using Engine;
using Engine.Multithreading;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Rendering;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Ships.Sharing;

public sealed class ShipPlacementPreview : Node3D
{
    private const string ShaderPath = "Resources/Shaders/blockPreview";
    private const int UploadsPerFrame = 8;

    private static readonly Color4 Tint = new Color4(0.8f, 0.95f, 1f, 1f);

    private SimpleBlock _block;
    private ShipModelMeshes _model;
    private BlockMaterial _modelMaterial;
    private Matrix4 _transform = Matrix4.Identity;
    private int _version;
    private bool _hasMesh;
    private bool _visible;

    public void Show(CompoundTag entity)
    {
        int version = ++_version;
        _hasMesh = false;
        _visible = false;
        if (_block != null) _block.Enabled = false;
        DisposeModel();
        if (entity == null || GameAssets.BlocksTexture == null) return;
        _model = new ShipModelMeshes(entity);

        var kinds = new List<BlockData>();
        var byId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        WorkerPoolManager.Enqueue<(float[] Vertices, uint[] Indices, int Scale)>(_ =>
        {
            var cells = ShipPreviewCells.Read(entity, id => KindOf(id, kinds, byId));
            int scale = ShipPreviewMesh.ScaleFor(cells.Count);
            var mesh = ShipPreviewMesh.Build(ShipPreviewMesh.Downscale(cells, scale), (kind, face) => kinds[kind].GetFaceUV(face, BlockState.Inactive), scale);
            return (mesh.Vertices, mesh.Indices, scale);
        }, WorkerPoolManager.Priority.Medium).ContinueWith(task =>
        {
            if (task.Status != TaskStatus.RanToCompletion) return;
            var result = task.Result;
            MainThreadDispatcher.Instance.Enqueue(() => Apply(version, result.Vertices, result.Indices));
        });
    }

    public void Hide()
    {
        _version++;
        _hasMesh = false;
        _visible = false;
        if (_block != null) _block.Enabled = false;
        DisposeModel();
    }

    public void SetPose(Vector3 localCenter, Quaternion rotation, Vector3 center)
    {
        _visible = true;
        _transform = Matrix4.CreateTranslation(-localCenter) * Matrix4.CreateFromQuaternion(rotation) * Matrix4.CreateTranslation(center);
        _model?.Upload(UploadsPerFrame);
        if (_block == null) return;
        _block.Enabled = _hasMesh && !ModelReady;
        _block.SetBlockTransform(_transform);
    }

    public override void Render()
    {
        base.Render();
        if (!_visible || !ModelReady) return;
        _modelMaterial ??= new BlockMaterial(GameAssets.BlocksTexture, GameAssets.EmissionBlocks);
        _model.Render(_modelMaterial, _transform * Matrix4.CreateTranslation(-RenderSpace.Origin));
    }

    public override void Destroy()
    {
        DisposeModel();
        base.Destroy();
    }

    private bool ModelReady => _model != null && _model.IsComplete;

    private void DisposeModel()
    {
        _model?.Dispose();
        _model = null;
    }

    private static int KindOf(string id, List<BlockData> kinds, Dictionary<string, int> byId)
    {
        if (byId.TryGetValue(id, out int kind)) return kind;
        kind = GameAssets.TryGetBlockByFullID(id, out var data) && data != null ? kinds.Count : -1;
        if (kind >= 0) kinds.Add(data);
        byId[id] = kind;
        return kind;
    }

    private void Apply(int version, float[] vertices, uint[] indices)
    {
        if (version != _version || vertices.Length == 0) return;
        EnsureBlock();
        _block.SetCustomMesh(vertices, indices);
        _hasMesh = true;
    }

    private void EnsureBlock()
    {
        if (_block != null) return;
        var material = new TextureMaterial(GameAssets.BlocksTexture, Resources.Load<Shader>(ShaderPath)) { Color = Tint };
        _block = new SimpleBlock(material, Vector3.Zero) { DisposesTexture = false, Enabled = false };
        AddChild(_block);
    }
}
