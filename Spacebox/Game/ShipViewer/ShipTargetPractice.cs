using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipTargetPractice
{
    private const float TracerStart = 1f;
    private const float TracerLead = 40f;

    private readonly ShipViewerEffects _effects;
    private readonly List<PendingHit> _pending = new List<PendingHit>();

    public ShipTargetPractice(ShipViewerEffects effects)
    {
        _effects = effects;
    }

    public void Fire(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Matrix4 view, float range, Vector3 origin, Vector3 direction, in ProjectileParameters projectile)
    {
        var inverse = Matrix4.Invert(view);
        var grid = new ShipBlockGrid(chunks, () => GameAssets.CreateBlockFromId(0));
        var shot = ShipShot.Fire(grid, Vector3.TransformPosition(origin, inverse), Vector3.TransformVector(direction, inverse), range, projectile);

        var affected = new HashSet<Vector3SByte>();
        grid.CollectAffectedChunks(shot.ChangedCells, affected);

        float firstStop = (shot.Path[1] - shot.Path[0]).Length;
        float lead = Math.Clamp(firstStop - TracerLead, MathF.Min(TracerStart, firstStop), firstStop);
        float speed = MathF.Max(1f, projectile.Speed);
        _effects.Shoot(origin + direction * lead, direction, firstStop - lead, projectile);
        _pending.Add(new PendingHit((shot.Length - lead) / speed, Vector3.TransformPosition(shot.End, view), shot.Hit, projectile, affected));
    }

    public void Update(float deltaTime, Action<HashSet<Vector3SByte>> remesh)
    {
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var hit = _pending[i];
            hit.TimeLeft -= deltaTime;
            if (hit.TimeLeft > 0f) continue;

            _pending.RemoveAt(i);
            if (hit.Hit) _effects.Impact(hit.Impact, hit.Projectile);
            if (hit.Chunks.Count > 0) remesh(hit.Chunks);
        }
    }

    public void Clear()
    {
        _pending.Clear();
    }

    private sealed class PendingHit
    {
        public PendingHit(float timeLeft, Vector3 impact, bool hit, ProjectileParameters projectile, HashSet<Vector3SByte> chunks)
        {
            TimeLeft = timeLeft;
            Impact = impact;
            Hit = hit;
            Projectile = projectile;
            Chunks = chunks;
        }

        public float TimeLeft { get; set; }
        public Vector3 Impact { get; }
        public bool Hit { get; }
        public ProjectileParameters Projectile { get; }
        public HashSet<Vector3SByte> Chunks { get; }
    }
}
