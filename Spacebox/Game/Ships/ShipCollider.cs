using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

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

    private static readonly Vector3SByte[] NeighborOffsets =
    {
        new Vector3SByte(1, 0, 0), new Vector3SByte(-1, 0, 0),
        new Vector3SByte(0, 1, 0), new Vector3SByte(0, -1, 0),
        new Vector3SByte(0, 0, 1), new Vector3SByte(0, 0, -1)
    };

    private readonly SpaceEntity _entity;
    private readonly Dictionary<Chunk, ChunkHull> _hulls = new Dictionary<Chunk, ChunkHull>();
    private readonly List<ChunkHull> _hullList = new List<ChunkHull>();
    private readonly HashSet<Vector3SByte> _dirty = new HashSet<Vector3SByte>();
    private readonly List<Chunk> _removed = new List<Chunk>();
    private readonly List<SpaceEntity> _nearby = new List<SpaceEntity>();
    private readonly Func<Vector3i, bool> _isOwnSolid;
    private int _hullVersion = -1;

    public float Radius { get; private set; }
    public IReadOnlyList<ChunkHull> Hulls => _hullList;

    public ShipCollider(SpaceEntity entity)
    {
        _entity = entity;
        _isOwnSolid = cell => IsSolid(_entity, cell);
    }

    public void RefreshHull(Vector3 centerOfMassLocal)
    {
        if (_entity.MassVersion == _hullVersion) return;
        _hullVersion = _entity.MassVersion;

        CollectDirtyChunks();
        RebuildDirtyChunks();
        UpdateRadius(centerOfMassLocal);
    }

    public bool TryFindContact(Vector3 centerOfMassWorld, out ShipContact contact)
    {
        contact = default;
        if (_hullList.Count == 0 || !GatherNearby(centerOfMassWorld)) return false;

        var pointSum = Vector3.Zero;
        var normalSum = Vector3.Zero;

        for (int e = 0; e < _nearby.Count; e++)
        {
            var other = _nearby[e];
            TestHulls(_entity, _hullList, other, other, -1f, ref contact, ref pointSum, ref normalSum);

            var otherBody = other.Ship;
            if (otherBody == null) continue;

            otherBody.RefreshMassProperties();
            TestHulls(other, otherBody.Collider.Hulls, _entity, other, 1f, ref contact, ref pointSum, ref normalSum);
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

    private void CollectDirtyChunks()
    {
        _dirty.Clear();
        var chunks = _entity.Chunks;
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            if (chunk.IsDisposed) continue;
            if (_hulls.TryGetValue(chunk, out var hull) && hull.Version == chunk.DataVersion) continue;
            MarkDirty(chunk.PositionIndex);
        }

        _removed.Clear();
        foreach (var pair in _hulls)
        {
            if (pair.Key.IsDisposed || pair.Key.SpaceEntity != _entity) _removed.Add(pair.Key);
        }

        for (int i = 0; i < _removed.Count; i++)
        {
            MarkDirty(_removed[i].PositionIndex);
            _hulls.Remove(_removed[i]);
        }
    }

    private void MarkDirty(Vector3SByte index)
    {
        _dirty.Add(index);
        for (int i = 0; i < NeighborOffsets.Length; i++)
            _dirty.Add(index + NeighborOffsets[i]);
    }

    private void RebuildDirtyChunks()
    {
        foreach (var index in _dirty)
        {
            if (!_entity.TryGetChunk(index, out var chunk) || chunk.IsDisposed) continue;

            if (!_hulls.TryGetValue(chunk, out var hull))
            {
                hull = new ChunkHull();
                _hulls[chunk] = hull;
            }

            BuildChunkHull(chunk, hull);
        }

        _hullList.Clear();
        foreach (var hull in _hulls.Values)
        {
            if (hull.Hull.Samples.Count > 0) _hullList.Add(hull);
        }
    }

    private void BuildChunkHull(Chunk chunk, ChunkHull hull)
    {
        hull.Version = chunk.DataVersion;
        hull.Hull.Clear();

        var origin = new Vector3i(chunk.PositionIndex.X, chunk.PositionIndex.Y, chunk.PositionIndex.Z) * Chunk.Size;
        var blocks = chunk.Blocks;
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        for (int z = 0; z < Chunk.Size; z++)
        {
            if (IsSolid(blocks[x, y, z])) hull.Hull.AddCell(origin + new Vector3i(x, y, z), _isOwnSolid);
        }

        hull.Center = new Vector3(origin.X, origin.Y, origin.Z) + new Vector3(Chunk.Size * 0.5f);
        hull.Hull.Finish(hull.Center);
    }

    private void UpdateRadius(Vector3 centerOfMassLocal)
    {
        float radius = 0f;
        for (int i = 0; i < _hullList.Count; i++)
        {
            var hull = _hullList[i];
            radius = MathF.Max(radius, (hull.Center - centerOfMassLocal).Length + hull.Hull.Radius);
        }
        Radius = radius;
    }

    private static void TestHulls(SpaceEntity owner, IReadOnlyList<ChunkHull> hulls, SpaceEntity target, SpaceEntity other, float normalSign,
        ref ShipContact contact, ref Vector3 pointSum, ref Vector3 normalSum)
    {
        for (int h = 0; h < hulls.Count; h++)
        {
            var hull = hulls[h];
            float reach = hull.Hull.Radius + QueryMargin;
            if (DistanceSquaredToBounds(target, owner.EntityLocalToWorld(hull.Center)) > reach * reach) continue;

            var samples = hull.Hull.Samples;
            for (int i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                var world = owner.EntityLocalToWorld(sample.Point);
                if (!IsInsideSolid(target, world)) continue;

                contact.Other ??= other;
                contact.Count++;
                pointSum += world;
                normalSum += owner.DirectionToWorldSpace(sample.Normal) * normalSign;
            }
        }
    }

    private bool GatherNearby(Vector3 centerOfMassWorld)
    {
        _nearby.Clear();
        var world = World.Instance;
        if (world == null) return false;

        float reach = Radius + QueryMargin;
        foreach (var sector in world.LoadedSectors)
        {
            if (sector.Entities == null || DistanceSquaredToBox(sector.BoundingBox, centerOfMassWorld) > reach * reach) continue;

            AddNearby(sector.Entities, centerOfMassWorld, reach);
            if (sector.Visitors != null) AddNearby(sector.Visitors, centerOfMassWorld, reach);
        }

        return _nearby.Count > 0;
    }

    private void AddNearby(IReadOnlyList<SpaceEntity> entities, Vector3 centerOfMassWorld, float reach)
    {
        for (int i = 0; i < entities.Count; i++)
        {
            var other = entities[i];
            if (other == _entity || other.IsDisposed || other.Chunks.Count == 0) continue;
            if (DistanceSquaredToBounds(other, centerOfMassWorld) <= reach * reach && !_nearby.Contains(other)) _nearby.Add(other);
        }
    }

    private static bool IsInsideSolid(SpaceEntity entity, Vector3 world)
    {
        var box = entity.GeometryBoundingBox;
        var point = entity.ToEntitySpace(world);
        var min = box.Min;
        var max = box.Max;
        if (point.X < min.X || point.Y < min.Y || point.Z < min.Z || point.X > max.X || point.Y > max.Y || point.Z > max.Z) return false;

        var local = entity.WorldToEntityLocal(world);
        var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
        return BlockCollision.TryGetSolidBounds(entity, cell, out var boxMin, out var boxMax) && BoxMath.Contains(boxMin, boxMax, local);
    }

    private static float DistanceSquaredToBounds(SpaceEntity entity, Vector3 worldPoint)
    {
        return DistanceSquaredToBox(entity.GeometryBoundingBox, entity.ToEntitySpace(worldPoint));
    }

    private static float DistanceSquaredToBox(BoundingBox box, Vector3 point)
    {
        var closest = Vector3.Clamp(point, box.Min, box.Max);
        return (point - closest).LengthSquared;
    }

    private static bool IsSolid(SpaceEntity entity, Vector3i cell)
    {
        return entity.TryGetBlockLocal(cell, out var chunk, out var index) && IsSolid(chunk.Blocks[index.X, index.Y, index.Z]);
    }

    private static bool IsSolid(Generation.Blocks.Block block)
    {
        return BlockCollision.IsSolid(block) && !block.HasCustomShape;
    }

    public sealed class ChunkHull
    {
        public readonly ShipHull Hull = new ShipHull();
        public Vector3 Center;
        public int Version = -1;
    }
}
