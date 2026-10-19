using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Ships;

public sealed class CannonTurretRenderer : IDisposable
{
    private const string MovingBlockShader = "Resources/Shaders/movingBlock";

    private readonly SpaceEntity _entity;
    private readonly Dictionary<CannonBlock, Turret> _turrets = new Dictionary<CannonBlock, Turret>();
    private readonly List<CannonBlock> _finished = new List<CannonBlock>();
    private readonly List<MultiblockCell> _layoutCells = new List<MultiblockCell>();
    private readonly List<Vector3i> _cells = new List<Vector3i>();
    private readonly HashSet<Vector3i> _volume = new HashSet<Vector3i>();

    public CannonTurretRenderer(SpaceEntity entity)
    {
        _entity = entity;
    }

    public void Update(IReadOnlyList<CannonBlock> cannons)
    {
        if (Spacebox.Network.GameSession.IsDedicated) return;

        for (int i = 0; i < cannons.Count; i++)
        {
            var cannon = cannons[i];
            bool needed = cannon.IsAiming || !cannon.IsAtRest;
            bool exists = _turrets.TryGetValue(cannon, out var turret);

            if (needed && !exists) Activate(cannon);
            else if (needed && turret.IsRestoring) Hide(cannon, turret);
            else if (!needed && exists && !turret.IsRestoring) Restore(cannon, turret);
        }

        _finished.Clear();
        foreach (var pair in _turrets)
        {
            if (pair.Value.IsRestoring && AreChunksMeshed(pair.Value)) _finished.Add(pair.Key);
        }

        for (int i = 0; i < _finished.Count; i++)
            Forget(_finished[i]);
    }

    public void Render()
    {
        foreach (var pair in _turrets)
        {
            var cannon = pair.Key;
            var turret = pair.Value;
            if (cannon.Entity != _entity) continue;

            turret.Visual.Position = _entity.EntityLocalToWorld(cannon.PivotLocal);
            turret.Visual.SetBlockTransform(BuildTransform(cannon, turret.PivotModel));
            turret.Visual.Render();
        }
    }

    public void Forget(CannonBlock cannon)
    {
        if (!_turrets.Remove(cannon, out var turret)) return;
        turret.Visual.Destroy();
    }

    public void Dispose()
    {
        foreach (var turret in _turrets.Values)
            turret.Visual.Destroy();
        _turrets.Clear();
    }

    public static Vector3 GetPivotModel(BlockData data)
    {
        var size = new Vector3(data.Size.X, data.Size.Y, data.Size.Z);
        var front = BlockRotationHelper.GetDirectionVector(data.BaseFrontDirection);
        float extent = (MathF.Abs(front.X) * size.X + MathF.Abs(front.Y) * size.Y + MathF.Abs(front.Z) * size.Z) * 0.5f;
        return (size - Vector3.One) * 0.5f - front * extent;
    }

    public static Matrix4 GetAimRotation(Vector3 restDirection, Vector3 aimDirection)
    {
        var from = ShipAiming.SafeNormalize(restDirection, Vector3.UnitY);
        var to = ShipAiming.SafeNormalize(aimDirection, from);
        var axis = Vector3.Cross(from, to);
        float angle = ShipAiming.AngleBetween(from, to);
        if (axis.LengthSquared < 1e-10f || angle < 1e-5f) return Matrix4.Identity;

        var rotation = Quaternion.FromAxisAngle(axis.Normalized(), angle);
        var x = Vector3.Transform(Vector3.UnitX, rotation);
        var y = Vector3.Transform(Vector3.UnitY, rotation);
        var z = Vector3.Transform(Vector3.UnitZ, rotation);
        return new Matrix4(new Vector4(x, 0f), new Vector4(y, 0f), new Vector4(z, 0f), Vector4.UnitW);
    }

    private Matrix4 BuildTransform(CannonBlock cannon, Vector3 pivotModel)
    {
        var layout = MultiblockLayout.GetTransform(cannon.Data, cannon.Direction, cannon.Rotation);
        return Matrix4.CreateTranslation(-pivotModel) * layout * GetAimRotation(cannon.Frame.Front, cannon.AimDirection) * _entity.RotationMatrix;
    }

    private void Activate(CannonBlock cannon)
    {
        var (vertices, indices) = MultiblockPreviewMesh.Build(cannon.Data, BlockState.Active);
        var shader = Resources.Load<Shader>(MovingBlockShader, true);
        var visual = new SimpleBlock(new TextureMaterial(GameAssets.BlocksTexture, shader), Vector3.Zero) { DisposesTexture = false };
        visual.SetCustomMesh(vertices, indices);

        var turret = new Turret(visual, GetPivotModel(cannon.Data));
        _turrets[cannon] = turret;

        CollectCells(cannon);
        _volume.Clear();
        _volume.UnionWith(_cells);
        visual.ExtraLight = BlockLightSampler.AverageAroundCells(_entity, _cells, _volume);

        Hide(cannon, turret);
    }

    private void Hide(CannonBlock cannon, Turret turret)
    {
        turret.IsRestoring = false;
        SetHidden(cannon, turret, true);
    }

    private void Restore(CannonBlock cannon, Turret turret)
    {
        turret.IsRestoring = true;
        SetHidden(cannon, turret, false);
    }

    private void SetHidden(CannonBlock cannon, Turret turret, bool hidden)
    {
        CollectCells(cannon);
        turret.Chunks.Clear();

        for (int i = 0; i < _cells.Count; i++)
        {
            if (!_entity.TryGetBlockLocal(_cells[i], out var chunk, out var index)) continue;

            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (block is not CannonBlock) continue;

            block.IsMeshHidden = hidden;
            if (!turret.Chunks.Contains(chunk)) turret.Chunks.Add(chunk);
        }

        for (int i = 0; i < turret.Chunks.Count; i++)
            turret.Chunks[i].QueueMeshUpdate(UpdateReason.PlayerAction);
    }

    private void CollectCells(CannonBlock cannon)
    {
        _cells.Clear();
        MultiblockLayout.GetCells(cannon.Data, cannon.Direction, cannon.Rotation, _layoutCells);
        for (int i = 0; i < _layoutCells.Count; i++)
            _cells.Add(cannon.LocalCell + _layoutCells[i].Offset);
    }

    private static bool AreChunksMeshed(Turret turret)
    {
        for (int i = 0; i < turret.Chunks.Count; i++)
        {
            var chunk = turret.Chunks[i];
            if (!chunk.IsDisposed && chunk.IsQueuedForMeshUpdate) return false;
        }
        return true;
    }

    private sealed class Turret
    {
        public readonly SimpleBlock Visual;
        public readonly Vector3 PivotModel;
        public readonly List<Chunk> Chunks = new List<Chunk>();
        public bool IsRestoring;

        public Turret(SimpleBlock visual, Vector3 pivotModel)
        {
            Visual = visual;
            PivotModel = pivotModel;
        }
    }
}
