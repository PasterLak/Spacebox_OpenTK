using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Tools;

public sealed class EntityCatalog
{
    private readonly Dictionary<long, EntityRecord> _byId = new Dictionary<long, EntityRecord>();
    private readonly Dictionary<Vector3i, HashSet<long>> _bySector = new Dictionary<Vector3i, HashSet<long>>();
    private readonly object _lock = new object();

    public int Count
    {
        get
        {
            lock (_lock) return _byId.Count;
        }
    }

    public void Upsert(EntityRecord record)
    {
        lock (_lock)
        {
            if (_byId.TryGetValue(record.Id, out var old)) RemoveFromSector(old);

            _byId[record.Id] = record;
            if (!_bySector.TryGetValue(record.Sector, out var ids))
            {
                ids = new HashSet<long>();
                _bySector[record.Sector] = ids;
            }
            ids.Add(record.Id);
        }
    }

    public bool Remove(long id)
    {
        lock (_lock)
        {
            if (!_byId.Remove(id, out var old)) return false;

            RemoveFromSector(old);
            return true;
        }
    }

    public bool RemoveByName(string name)
    {
        lock (_lock)
        {
            foreach (var record in _byId.Values)
            {
                if (!string.Equals(record.Name, name, StringComparison.Ordinal)) continue;

                _byId.Remove(record.Id);
                RemoveFromSector(record);
                return true;
            }
        }
        return false;
    }

    public bool TryGet(long id, out EntityRecord record)
    {
        lock (_lock) return _byId.TryGetValue(id, out record);
    }

    public bool ContainsName(string name)
    {
        lock (_lock)
        {
            foreach (var record in _byId.Values)
            {
                if (string.Equals(record.Name, name, StringComparison.Ordinal)) return true;
            }
        }
        return false;
    }

    public void CollectInSector(Vector3i sector, List<EntityRecord> output)
    {
        lock (_lock)
        {
            if (!_bySector.TryGetValue(sector, out var ids)) return;

            foreach (var id in ids) output.Add(_byId[id]);
        }
    }

    public EntityRecord[] Snapshot()
    {
        lock (_lock)
        {
            var records = new EntityRecord[_byId.Count];
            _byId.Values.CopyTo(records, 0);
            return records;
        }
    }

    private void RemoveFromSector(EntityRecord record)
    {
        if (!_bySector.TryGetValue(record.Sector, out var ids)) return;

        ids.Remove(record.Id);
        if (ids.Count == 0) _bySector.Remove(record.Sector);
    }
}
