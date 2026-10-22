namespace Spacebox.Game.Player.Respawn;

public sealed class ReconstructorRecords
{
    private readonly Dictionary<ReconstructorKey, ReconstructorRecord> _records = new Dictionary<ReconstructorKey, ReconstructorRecord>();
    private readonly List<ReconstructorKey> _removeBuffer = new List<ReconstructorKey>();

    public int Count => _records.Count;
    public IEnumerable<ReconstructorRecord> Values => _records.Values;

    public void Upsert(ReconstructorRecord record)
    {
        _records[record.Key] = record;
    }

    public bool Remove(ReconstructorKey key)
    {
        return _records.Remove(key);
    }

    public bool TryGet(ReconstructorKey key, out ReconstructorRecord record)
    {
        return _records.TryGetValue(key, out record);
    }

    public int RemoveEntity(long entityId, Func<ReconstructorKey, bool> keep = null)
    {
        _removeBuffer.Clear();
        foreach (var key in _records.Keys)
        {
            if (key.EntityId == entityId && (keep == null || !keep(key))) _removeBuffer.Add(key);
        }

        for (int i = 0; i < _removeBuffer.Count; i++) _records.Remove(_removeBuffer[i]);
        return _removeBuffer.Count;
    }

    public void CollectCandidates(List<ReconstructorCandidate> output, IReadOnlySet<ReconstructorKey> skip)
    {
        foreach (var record in _records.Values)
        {
            if (skip != null && skip.Contains(record.Key)) continue;
            output.Add(record.ToCandidate());
        }
    }

    public void Clear()
    {
        _records.Clear();
    }
}
