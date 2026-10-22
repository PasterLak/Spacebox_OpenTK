namespace Spacebox.Game.Generation;

public enum EntityLoadResult
{
    Loaded,
    Missing,
    TimedOut
}

public static class EntityLoadRequests
{
    private sealed class Request
    {
        public long EntityId;
        public float TimeLeft;
        public Action<EntityLoadResult, SpaceEntity> Done;
    }

    private static readonly List<Request> _pending = new List<Request>();

    static EntityLoadRequests()
    {
        Sector.EntityAdded += OnEntityAdded;
    }

    public static int PendingCount => _pending.Count;

    public static void Load(World world, long entityId, float timeout, Action<EntityLoadResult, SpaceEntity> done)
    {
        if (world == null)
        {
            done(EntityLoadResult.Missing, null);
            return;
        }

        if (world.TryFindEntity(entityId, out var loaded))
        {
            done(EntityLoadResult.Loaded, loaded);
            return;
        }

        if (!WorldPersistenceManager.TryLocate(entityId, out var record))
        {
            done(EntityLoadResult.Missing, null);
            return;
        }

        var sector = world.GetOrLoadSector(record.Sector);
        _pending.Add(new Request { EntityId = entityId, TimeLeft = timeout, Done = done });

        if (!sector.RequestEntityLoad(entityId) && !world.TryFindEntity(entityId, out _))
            Complete(entityId, EntityLoadResult.Missing, null);
    }

    public static void Update(float deltaTime)
    {
        if (_pending.Count == 0) return;

        List<Request> expired = null;
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var request = _pending[i];
            request.TimeLeft -= deltaTime;
            if (request.TimeLeft > 0f) continue;

            _pending.RemoveAt(i);
            (expired ??= new List<Request>()).Add(request);
        }

        if (expired == null) return;
        for (int i = 0; i < expired.Count; i++) expired[i].Done(EntityLoadResult.TimedOut, null);
    }

    public static void Clear()
    {
        _pending.Clear();
    }

    private static void OnEntityAdded(SpaceEntity entity)
    {
        if (_pending.Count > 0) Complete(entity.EntityID, EntityLoadResult.Loaded, entity);
    }

    private static void Complete(long entityId, EntityLoadResult result, SpaceEntity entity)
    {
        List<Request> completed = null;
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var request = _pending[i];
            if (request.EntityId != entityId) continue;

            _pending.RemoveAt(i);
            (completed ??= new List<Request>()).Add(request);
        }

        if (completed == null) return;
        for (int i = 0; i < completed.Count; i++) completed[i].Done(result, entity);
    }
}
