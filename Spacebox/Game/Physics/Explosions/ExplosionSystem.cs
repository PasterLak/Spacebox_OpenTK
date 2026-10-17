using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Physics.Explosions;

public sealed class ExplosionSystem : IDisposable
{
    private const int MaxExplosionsPerFrame = 2;

    private readonly BlastRayCaster _rayCaster = new BlastRayCaster();
    private readonly ExplosionBlockBreaker _breaker = new ExplosionBlockBreaker();
    private readonly ExplosionForces _forces = new ExplosionForces();
    private readonly ExplosionEffects _effects = new ExplosionEffects();
    private readonly HashSet<Vector3i> _cells = new HashSet<Vector3i>();
    private readonly List<PendingExplosion> _pending = new List<PendingExplosion>();
    private readonly HashSet<(SpaceEntity, Vector3i)> _scheduled = new HashSet<(SpaceEntity, Vector3i)>();
    private readonly Action<Chunk, Vector3Byte, ExplosiveBlockData> _chainExplosive;

    public ExplosionSystem()
    {
        _chainExplosive = (chunk, index, data) => Detonate(chunk, index, data, data.ChainDelay);
    }

    public void Detonate(Chunk chunk, Vector3Byte index, ExplosiveBlockData data, float delay)
    {
        var entity = chunk.SpaceEntity;
        if (entity == null) return;

        var cell = new Vector3i(
            chunk.PositionIndex.X * Chunk.Size + index.X,
            chunk.PositionIndex.Y * Chunk.Size + index.Y,
            chunk.PositionIndex.Z * Chunk.Size + index.Z);

        if (!_scheduled.Add((entity, cell))) return;
        _pending.Add(new PendingExplosion { Entity = entity, Cell = cell, Data = data, Delay = delay });
    }

    public void Update(float deltaTime)
    {
        int processed = 0;
        int i = 0;

        while (i < _pending.Count)
        {
            var pending = _pending[i];
            pending.Delay -= deltaTime;

            if (pending.Delay > 0f || processed >= MaxExplosionsPerFrame)
            {
                _pending[i] = pending;
                i++;
                continue;
            }

            _pending.RemoveAt(i);
            _scheduled.Remove((pending.Entity, pending.Cell));
            Explode(pending);
            processed++;
        }

        _effects.Update();
    }

    public void Render()
    {
        _effects.Render();
    }

    public void Dispose()
    {
        _pending.Clear();
        _scheduled.Clear();
        _effects.Dispose();
    }

    private void Explode(PendingExplosion pending)
    {
        var entity = pending.Entity;
        if (!entity.TryGetBlockLocal(pending.Cell, out var chunk, out var index)) return;
        if (chunk.IsDisposed || chunk.Blocks[index.X, index.Y, index.Z] is not ExplosiveBlock) return;

        var data = pending.Data;
        var centerLocal = new Vector3(pending.Cell.X + 0.5f, pending.Cell.Y + 0.5f, pending.Cell.Z + 0.5f);
        var worldCenter = entity.EntityLocalToWorld(centerLocal);

        _rayCaster.Collect(entity, centerLocal, data.Radius, data.Penetration, _cells);
        _breaker.Break(entity, _cells, pending.Cell, worldCenter, data, _chainExplosive);
        _forces.Apply(worldCenter, data);
        _effects.Play(worldCenter, data.Radius);
    }

    private struct PendingExplosion
    {
        public SpaceEntity Entity;
        public Vector3i Cell;
        public ExplosiveBlockData Data;
        public float Delay;
    }
}
