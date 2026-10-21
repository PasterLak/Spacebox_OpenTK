using Spacebox.Game.Rules;
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
    private readonly List<Vector3i> _removedCells = new List<Vector3i>();
    private readonly List<Vector3i> _sourceCells = new List<Vector3i>();

    public static event Action<SpaceEntity, Vector3i, short, IReadOnlyList<Vector3i>, ExplosionRules> Exploded;

    private int _currentCauser = GameplayRules.NoPeer;

    public void ApplyReplica(SpaceEntity entity, Vector3i cell, ExplosiveBlockData data, IReadOnlyList<Vector3i> cells, ExplosionRules rules)
    {
        var worldCenter = entity.EntityLocalToWorld(GetCenterLocal(entity, cell));

        _breaker.Break(entity, cells, cell, worldCenter, data, _chainExplosive, true, null, rules.RespectClaims);
        _forces.Apply(worldCenter, data, rules);
        _effects.Play(worldCenter, data.Radius);
    }

    public void PlayEffect(Vector3 worldCenter, float radius)
    {
        if (radius > 0f) _effects.Play(worldCenter, radius);
    }

    public ExplosionSystem()
    {
        _chainExplosive = (chunk, index, data) => Detonate(chunk, index, data, data.ChainDelay, _currentCauser);
    }

    public void Detonate(Chunk chunk, Vector3Byte index, ExplosiveBlockData data, float delay, int causerPeer = GameplayRules.NoPeer)
    {
        var entity = chunk.SpaceEntity;
        if (entity == null) return;

        var cell = new Vector3i(
            chunk.PositionIndex.X * Chunk.Size + index.X,
            chunk.PositionIndex.Y * Chunk.Size + index.Y,
            chunk.PositionIndex.Z * Chunk.Size + index.Z);

        if (!_scheduled.Add((entity, cell))) return;
        _pending.Add(new PendingExplosion { Entity = entity, Cell = cell, Data = data, Delay = delay, CauserPeer = causerPeer });
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
        if (chunk.IsDisposed || chunk.Blocks[index.X, index.Y, index.Z] is not IExplosiveBlock { Explosion: not null }) return;

        var data = pending.Data;
        var centerLocal = GetCenterLocal(entity, pending.Cell);
        var worldCenter = entity.EntityLocalToWorld(centerLocal);

        short bombId = chunk.Blocks[index.X, index.Y, index.Z].Id;
        var rules = GameplayRules.ForExplosion(ServerRules.Current, pending.CauserPeer);

        if (rules.BreakBlocks)
        {
            _rayCaster.Collect(entity, centerLocal, data.Radius, data.Penetration, _cells, _sourceCells);
        }
        else
        {
            _cells.Clear();
            for (int i = 0; i < _sourceCells.Count; i++) _cells.Add(_sourceCells[i]);
        }

        _currentCauser = pending.CauserPeer;
        _breaker.Break(entity, _cells, pending.Cell, worldCenter, data, _chainExplosive, false, _removedCells, rules.RespectClaims);
        _currentCauser = GameplayRules.NoPeer;
        _forces.Apply(worldCenter, data, rules);
        _effects.Play(worldCenter, data.Radius);
        Exploded?.Invoke(entity, pending.Cell, bombId, _removedCells, rules);
    }

    private Vector3 GetCenterLocal(SpaceEntity entity, Vector3i cell)
    {
        _sourceCells.Clear();
        if (entity.TryGetBlockLocal(cell, out var chunk, out var index)) entity.GetStructureCells(chunk, index, _sourceCells);
        if (_sourceCells.Count == 0) _sourceCells.Add(cell);

        var sum = Vector3.Zero;
        for (int i = 0; i < _sourceCells.Count; i++)
            sum += new Vector3(_sourceCells[i].X + 0.5f, _sourceCells[i].Y + 0.5f, _sourceCells[i].Z + 0.5f);
        return sum / _sourceCells.Count;
    }

    private struct PendingExplosion
    {
        public SpaceEntity Entity;
        public Vector3i Cell;
        public ExplosiveBlockData Data;
        public float Delay;
        public int CauserPeer;
    }
}
