using OpenTK.Mathematics;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Effects.Machines;

public sealed class MachineSoundTracker
{
    private sealed class ChunkSources
    {
        public int Version = int.MinValue;
        public int Stamp;
        public readonly List<MachineSoundSource> Sources = new List<MachineSoundSource>();
    }

    private static readonly float ChunkReach = Chunk.Size * 0.87f;
    private static readonly Vector3 ChunkHalf = new Vector3(Chunk.SizeHalf, Chunk.SizeHalf, Chunk.SizeHalf);

    private readonly MachineActivities _activities;
    private readonly float _radius;
    private readonly Dictionary<Chunk, ChunkSources> _chunks = new Dictionary<Chunk, ChunkSources>();
    private readonly List<Chunk> _stale = new List<Chunk>();
    private readonly List<MachineSoundSource> _working = new List<MachineSoundSource>();
    private int _stamp;

    public MachineSoundTracker(MachineActivities activities, float radius)
    {
        _activities = activities;
        _radius = radius;
    }

    public IReadOnlyList<MachineSoundSource> Working => _working;

    public void Refresh(Sector sector, Vector3 listener)
    {
        _stamp++;
        _working.Clear();

        var entities = sector?.Entities;
        if (entities != null)
        {
            for (int i = 0; i < entities.Count; i++) Visit(entities[i], listener);
        }

        Prune();
    }

    public void Clear()
    {
        foreach (var pair in _chunks) Release(pair.Value);
        _chunks.Clear();
        _working.Clear();
    }

    private void Visit(SpaceEntity entity, Vector3 listener)
    {
        if (entity == null || entity.IsDisposed) return;

        var local = entity.WorldToEntityLocal(listener);
        float reach = _radius + ChunkReach;
        float reachSquared = reach * reach;
        float radiusSquared = _radius * _radius;
        var chunks = entity.Chunks;

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            if (chunk == null) continue;

            var origin = SpaceMath.Entity.ChunkIndexToLocal(chunk.PositionIndex);
            if (Vector3.DistanceSquared(origin + ChunkHalf, local) > reachSquared) continue;

            var cache = GetSources(chunk, entity, origin);
            cache.Stamp = _stamp;

            for (int s = 0; s < cache.Sources.Count; s++)
            {
                var source = cache.Sources[s];
                if (!source.HasLoop) continue;

                float distanceSquared = Vector3.DistanceSquared(source.Local, local);
                if (distanceSquared > radiusSquared || !source.IsWorking) continue;

                source.DistanceSquared = distanceSquared;
                _working.Add(source);
            }
        }
    }

    private ChunkSources GetSources(Chunk chunk, SpaceEntity entity, Vector3 origin)
    {
        if (!_chunks.TryGetValue(chunk, out var cache))
        {
            cache = new ChunkSources();
            _chunks[chunk] = cache;
        }

        if (cache.Version != chunk.DataVersion)
        {
            Release(cache);
            Scan(chunk, entity, origin, cache.Sources);
            cache.Version = chunk.DataVersion;
        }

        return cache;
    }

    private void Scan(Chunk chunk, SpaceEntity entity, Vector3 origin, List<MachineSoundSource> sources)
    {
        var blocks = chunk.Blocks;
        if (blocks == null) return;

        for (int x = 0; x < Chunk.Size; x++)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int z = 0; z < Chunk.Size; z++)
                {
                    var block = blocks[x, y, z];
                    if (block is not InteractiveBlock || block.HasMultiblockOffset) continue;

                    var data = GameAssets.GetBlockDataById(block.Id);
                    if (data == null || (string.IsNullOrEmpty(data.WorkingSound) && string.IsNullOrEmpty(data.FinishedSound))) continue;

                    var activity = _activities.Find(block);
                    if (activity == null) continue;

                    var local = origin + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    sources.Add(new MachineSoundSource(block, entity, local, activity, data.WorkingSound, data.FinishedSound));
                }
            }
        }
    }

    private void Prune()
    {
        _stale.Clear();
        foreach (var pair in _chunks)
        {
            if (pair.Value.Stamp != _stamp) _stale.Add(pair.Key);
        }

        for (int i = 0; i < _stale.Count; i++)
        {
            Release(_chunks[_stale[i]]);
            _chunks.Remove(_stale[i]);
        }
        _stale.Clear();
    }

    private static void Release(ChunkSources cache)
    {
        for (int i = 0; i < cache.Sources.Count; i++) cache.Sources[i].Dispose();
        cache.Sources.Clear();
    }
}
