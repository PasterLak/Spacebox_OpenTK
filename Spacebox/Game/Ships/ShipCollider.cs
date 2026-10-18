using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Ships;

public struct ShipContact
{
    public SpaceEntity Other;
    public Vector3 PointWorld;
    public Vector3 NormalWorld;
    public int Count;
}

public sealed class ShipCollider
{
    private const float QueryMargin = 1f;
    private const float PushOutDistance = 0.05f;

    private readonly SpaceEntity _entity;
    private readonly ShipHull _hull = new ShipHull();
    private readonly List<SpaceEntity> _nearby = new List<SpaceEntity>();
    private readonly Func<Vector3i, bool> _isOwnSolid;
    private int _hullVersion = -1;

    public float Radius => _hull.Radius;

    public ShipCollider(SpaceEntity entity)
    {
        _entity = entity;
        _isOwnSolid = cell => IsSolid(_entity, cell);
    }

    public void RefreshHull(Vector3 centerOfMassLocal)
    {
        if (_entity.MassVersion == _hullVersion) return;
        _hullVersion = _entity.MassVersion;

        _hull.Clear();
        var chunks = _entity.Chunks;
        for (int c = 0; c < chunks.Count; c++)
        {
            var chunk = chunks[c];
            if (chunk.IsDisposed || chunk.Mass <= 0) continue;

            var origin = new Vector3i(chunk.PositionIndex.X, chunk.PositionIndex.Y, chunk.PositionIndex.Z) * Chunk.Size;
            var blocks = chunk.Blocks;
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
            {
                if (IsSolid(blocks[x, y, z])) _hull.AddCell(origin + new Vector3i(x, y, z), _isOwnSolid);
            }
        }

        _hull.Finish(centerOfMassLocal);
    }

    public bool TryFindContact(Vector3 centerOfMassWorld, out ShipContact contact)
    {
        contact = default;
        if (_hull.Samples.Count == 0 || !GatherNearby(centerOfMassWorld)) return false;

        var samples = _hull.Samples;
        var pointSum = Vector3.Zero;
        var normalSum = Vector3.Zero;

        for (int i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            var world = _entity.EntityLocalToWorld(sample.Point);

            for (int e = 0; e < _nearby.Count; e++)
            {
                var other = _nearby[e];
                var local = other.WorldToEntityLocal(world);
                var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
                if (!IsSolid(other, cell)) continue;

                contact.Other ??= other;
                contact.Count++;
                pointSum += world;
                normalSum -= _entity.DirectionToWorldSpace(sample.Normal);
                break;
            }
        }

        if (contact.Count == 0) return false;

        contact.PointWorld = pointSum / contact.Count;
        contact.NormalWorld = normalSum.LengthSquared > 1e-8f ? normalSum.Normalized() : Vector3.Zero;
        return contact.NormalWorld != Vector3.Zero;
    }

    public void PushOut(in ShipContact contact)
    {
        _entity.SetPose(_entity.PositionWorld + contact.NormalWorld * PushOutDistance, _entity.Orientation);
    }

    private bool GatherNearby(Vector3 centerOfMassWorld)
    {
        _nearby.Clear();
        var sector = _entity.Sector;
        if (sector == null) return false;

        float reach = _hull.Radius + QueryMargin;
        var entities = sector.Entities;
        for (int i = 0; i < entities.Count; i++)
        {
            var other = entities[i];
            if (other == _entity || other.IsDisposed || other.Chunks.Count == 0) continue;
            if (DistanceSquaredToBounds(other, centerOfMassWorld) <= reach * reach) _nearby.Add(other);
        }

        return _nearby.Count > 0;
    }

    private static float DistanceSquaredToBounds(SpaceEntity entity, Vector3 worldPoint)
    {
        var box = entity.GeometryBoundingBox;
        var point = entity.ToEntitySpace(worldPoint);
        var closest = Vector3.Clamp(point, box.Min, box.Max);
        return (point - closest).LengthSquared;
    }

    private static bool IsSolid(SpaceEntity entity, Vector3i cell)
    {
        return entity.TryGetBlockLocal(cell, out var chunk, out var index) && IsSolid(chunk.Blocks[index.X, index.Y, index.Z]);
    }

    private static bool IsSolid(Generation.Blocks.Block block)
    {
        return block != null && !block.IsAir && !block.IsMeshHidden;
    }
}
