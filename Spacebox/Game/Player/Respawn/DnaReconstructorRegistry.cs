using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Respawn;

public static class DnaReconstructorRegistry
{
    private static readonly List<DnaReconstructorBlock> _blocks = new List<DnaReconstructorBlock>();
    private static readonly ReconstructorRecords _records = new ReconstructorRecords();
    private static readonly List<ReconstructorCandidate> _candidates = new List<ReconstructorCandidate>();
    private static readonly HashSet<ReconstructorKey> _liveKeys = new HashSet<ReconstructorKey>();
    private static readonly object _lock = new object();
    private static bool _keepsRecords;

    static DnaReconstructorRegistry()
    {
        Sector.EntityDestroyedPermanently += (_, entityId) => ForgetEntity(entityId);
        Sector.EntityAdded += OnEntityLoaded;
    }

    public static int RecordCount
    {
        get
        {
            lock (_lock) return _records.Count;
        }
    }

    public static void Load(string worldFolder)
    {
        var records = ReconstructorRecordsFile.Read(worldFolder);
        lock (_lock)
        {
            _records.Clear();
            for (int i = 0; i < records.Count; i++) _records.Upsert(records[i]);
            _keepsRecords = true;
        }

        if (records.Count > 0) Debug.Log($"[Reconstructors] Loaded {records.Count} DNA reconstructors");
    }

    public static void Save(string worldFolder)
    {
        ReconstructorRecord[] records;
        lock (_lock)
        {
            if (!_keepsRecords) return;

            for (int i = 0; i < _blocks.Count; i++) RememberLocked(_blocks[i]);
            records = _records.Values.ToArray();
        }

        try
        {
            ReconstructorRecordsFile.Write(worldFolder, records);
        }
        catch (Exception ex)
        {
            Debug.Error($"[Reconstructors] Failed to write {ReconstructorRecordsFile.FileName}: {ex.Message}");
        }
    }

    public static void Register(DnaReconstructorBlock block)
    {
        lock (_lock)
        {
            if (!_blocks.Contains(block)) _blocks.Add(block);
            RememberLocked(block);
        }
    }

    public static void Unregister(DnaReconstructorBlock block)
    {
        lock (_lock)
        {
            _blocks.Remove(block);
            if (_keepsRecords && TryGetKey(block, out var key)) _records.Remove(key);
        }
    }

    public static void UnregisterEntity(SpaceEntity entity)
    {
        lock (_lock)
        {
            for (int i = _blocks.Count - 1; i >= 0; i--)
            {
                var block = _blocks[i];
                if (block.Entity != entity) continue;

                RememberLocked(block);
                _blocks.RemoveAt(i);
            }
        }
    }

    public static void NotifyChanged(DnaReconstructorBlock block)
    {
        lock (_lock)
        {
            if (_blocks.Contains(block)) RememberLocked(block);
        }
    }

    public static void ForgetEntity(long entityId)
    {
        lock (_lock) _records.RemoveEntity(entityId);
    }

    public static void Forget(ReconstructorKey key)
    {
        lock (_lock) _records.Remove(key);
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _blocks.Clear();
            _records.Clear();
            _keepsRecords = false;
        }
    }

    public static bool TryFind(long entityId, Vector3i localCell, out DnaReconstructorBlock found)
    {
        lock (_lock)
        {
            for (int i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                if (block.Entity == null || block.Entity.EntityID != entityId || block.LocalCell != localCell) continue;

                found = block;
                return true;
            }
        }

        found = null;
        return false;
    }

    public static bool TryFind(ReconstructorKey key, out DnaReconstructorBlock found) => TryFind(key.EntityId, key.Cell, out found);

    public static bool TryFindNearestReady(Vector3 worldPosition, IReadOnlySet<ReconstructorKey> excluded, out ReconstructorCandidate nearest)
    {
        lock (_lock)
        {
            CollectCandidatesLocked();
            return ReconstructorSelector.TryFindNearestReady(_candidates, worldPosition, excluded, out nearest);
        }
    }

    public static string GetBlocker()
    {
        lock (_lock)
        {
            CollectCandidatesLocked();
            return ReconstructorSelector.GetBlocker(_candidates);
        }
    }

    private static void OnEntityLoaded(SpaceEntity entity)
    {
        lock (_lock)
        {
            if (!_keepsRecords) return;

            int removed = _records.RemoveEntity(entity.EntityID, key => HasLiveLocked(entity, key.Cell));
            if (removed > 0) Debug.Log($"[Reconstructors] Forgot {removed} DNA reconstructors that are no longer on entity {entity.EntityID}");
        }
    }

    private static bool HasLiveLocked(SpaceEntity entity, Vector3i cell)
    {
        for (int i = 0; i < _blocks.Count; i++)
        {
            if (_blocks[i].Entity == entity && _blocks[i].LocalCell == cell) return true;
        }
        return false;
    }

    private static void CollectCandidatesLocked()
    {
        _candidates.Clear();
        _liveKeys.Clear();

        for (int i = 0; i < _blocks.Count; i++)
        {
            var block = _blocks[i];
            if (!TryGetKey(block, out var key)) continue;

            _liveKeys.Add(key);
            _candidates.Add(new ReconstructorCandidate(key, block.WorldCenter, block.HasDna, block.IsActive));
        }

        _records.CollectCandidates(_candidates, _liveKeys);
    }

    private static void RememberLocked(DnaReconstructorBlock block)
    {
        if (!_keepsRecords || !TryGetKey(block, out var key)) return;
        _records.Upsert(ReconstructorRecord.At(key, block.WorldCenter, block.DnaCount, block.IsActive));
    }

    private static bool TryGetKey(DnaReconstructorBlock block, out ReconstructorKey key)
    {
        if (block.Entity == null)
        {
            key = default;
            return false;
        }

        key = new ReconstructorKey(block.Entity.EntityID, block.LocalCell);
        return true;
    }
}
