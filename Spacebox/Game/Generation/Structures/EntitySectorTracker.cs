using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;

namespace Spacebox.Game.Generation;

public sealed class EntitySectorTracker
{
    private const float OverlapMargin = 2f;

    private readonly World _world;
    private readonly HashSet<SpaceEntity> _pendingSet = new HashSet<SpaceEntity>();
    private readonly List<SpaceEntity> _pending = new List<SpaceEntity>();
    private readonly List<SpaceEntity> _processing = new List<SpaceEntity>();

    public EntitySectorTracker(World world)
    {
        _world = world;
    }

    public void Request(SpaceEntity entity)
    {
        if (entity != null && _pendingSet.Add(entity)) _pending.Add(entity);
    }

    public void Forget(SpaceEntity entity)
    {
        if (_pendingSet.Remove(entity)) _pending.Remove(entity);

        foreach (var sector in _world.LoadedSectors)
            sector.RemoveVisitor(entity);
    }

    public void ForgetSector(Sector sector)
    {
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            if (_pending[i].Sector != sector) continue;
            _pendingSet.Remove(_pending[i]);
            _pending.RemoveAt(i);
        }
    }

    public void OnSectorLoaded(Sector loaded)
    {
        foreach (var sector in _world.LoadedSectors)
        {
            var entities = sector.Entities;
            if (entities == null) continue;

            for (int i = 0; i < entities.Count; i++)
                Request(entities[i]);
        }
    }

    public void Process()
    {
        if (_pending.Count == 0) return;

        _processing.Clear();
        _processing.AddRange(_pending);
        _pending.Clear();
        _pendingSet.Clear();

        for (int i = 0; i < _processing.Count; i++)
        {
            var entity = _processing[i];
            if (entity.IsDisposed || entity.Sector == null) continue;

            TransferIfNeeded(entity);
            RefreshVisitors(entity);
        }

        _processing.Clear();
    }

    private void TransferIfNeeded(SpaceEntity entity)
    {
        var source = entity.Sector;
        var index = SpaceMath.Sector.GetSectorIndex(entity.PositionWorld);
        if (source.PositionIndex == index) return;

        var target = _world.GetOrLoadSectorFor(entity.PositionWorld);
        if (target == null || target == source) return;

        source.ReleaseEntity(entity);
        target.AcceptEntity(entity);
        Debug.Log($"[World] Entity {entity.EntityID} moved from sector {source.PositionIndex} to {target.PositionIndex}");
    }

    private void RefreshVisitors(SpaceEntity entity)
    {
        bool hasBounds = entity.Chunks.Count > 0;
        Vector3 min = default, max = default;
        if (hasBounds)
        {
            var box = entity.GeometryBoundingBox;
            EntityWorldBounds.Compute(box.Min, box.Max, entity.PositionWorld, entity.Orientation, out min, out max);
            min -= new Vector3(OverlapMargin);
            max += new Vector3(OverlapMargin);
        }

        foreach (var sector in _world.LoadedSectors)
        {
            if (sector == entity.Sector || !hasBounds || !EntityWorldBounds.Overlaps(min, max, sector.BoundingBox.Min, sector.BoundingBox.Max))
                sector.RemoveVisitor(entity);
            else
                sector.AddVisitor(entity);
        }
    }
}
