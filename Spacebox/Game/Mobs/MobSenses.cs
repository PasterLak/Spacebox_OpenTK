using OpenTK.Mathematics;
using Spacebox.Game.Mobs.Navigation;

namespace Spacebox.Game.Mobs;

public sealed class MobSenses
{
    private const float ScanInterval = 0.25f;
    private const float AlertRangeMultiplier = 1.75f;
    private const float LoseRangeMultiplier = 1.5f;

    private readonly Mob _mob;
    private readonly MobDefinition _definition;
    private readonly MobNavigationGrid _grid;
    private readonly float _halfFovCos;
    private float _scanTimer;
    private float _alertTimer;

    public bool HasTarget { get; private set; }
    public int TargetPeer { get; private set; }
    public Vector3 TargetPosition { get; private set; }
    public bool CanSeeTarget { get; private set; }
    public float TimeSinceSeen { get; private set; }
    public bool IsAlerted => _alertTimer > 0f;

    public MobSenses(Mob mob, MobDefinition definition, MobNavigationGrid grid, Random random)
    {
        _mob = mob;
        _definition = definition;
        _grid = grid;
        _halfFovCos = MathF.Cos(MathHelper.DegreesToRadians(definition.FieldOfView * 0.5f));
        _scanTimer = (float)random.NextDouble() * ScanInterval;
    }

    public float DistanceToTarget => HasTarget ? Vector3.Distance(_mob.Position, TargetPosition) : float.MaxValue;

    public void Alert(float duration)
    {
        _alertTimer = MathF.Max(_alertTimer, duration);
        _scanTimer = 0f;
    }

    public void Forget()
    {
        HasTarget = false;
        CanSeeTarget = false;
        TimeSinceSeen = 0f;
    }

    public void Update(float deltaTime)
    {
        if (_alertTimer > 0f) _alertTimer -= deltaTime;

        if (HasTarget)
        {
            TimeSinceSeen += deltaTime;
            if (CanSeeTarget && MobTargets.TryGet(TargetPeer, out var live)) TargetPosition = live.Position;
        }

        _scanTimer -= deltaTime;
        if (_scanTimer > 0f) return;
        _scanTimer = ScanInterval;

        if (HasTarget) Track();
        else Acquire();
    }

    private void Track()
    {
        if (!MobTargets.TryGet(TargetPeer, out var target) || !target.IsVulnerable)
        {
            Forget();
            return;
        }

        float loseRange = RangeFor(target) * LoseRangeMultiplier;
        float distanceSquared = Vector3.DistanceSquared(_mob.Position, target.Position);
        CanSeeTarget = distanceSquared <= loseRange * loseRange && _grid.HasLineOfSight(_mob.Position, target.Position);

        if (CanSeeTarget)
        {
            TargetPosition = target.Position;
            TimeSinceSeen = 0f;
            return;
        }

        if (TimeSinceSeen > _definition.ForgetTime) Forget();
    }

    private void Acquire()
    {
        var targets = MobTargets.All;
        float range = SightRange;
        float bestDistanceSquared = range * range;
        int bestIndex = -1;

        for (int i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            if (!target.IsVulnerable) continue;

            var offset = target.Position - _mob.Position;
            float distanceSquared = offset.LengthSquared;
            if (distanceSquared >= bestDistanceSquared) continue;
            float targetRange = RangeFor(target);
            if (distanceSquared >= targetRange * targetRange) continue;
            if (!IsNoticeable(offset, distanceSquared)) continue;
            if (!_grid.HasLineOfSight(_mob.Position, target.Position)) continue;

            bestDistanceSquared = distanceSquared;
            bestIndex = i;
        }

        if (bestIndex < 0) return;

        HasTarget = true;
        CanSeeTarget = true;
        TimeSinceSeen = 0f;
        TargetPeer = targets[bestIndex].Peer;
        TargetPosition = targets[bestIndex].Position;
    }

    private bool IsNoticeable(Vector3 offset, float distanceSquared)
    {
        float hearing = _definition.HearingRange;
        if (IsAlerted || distanceSquared <= hearing * hearing) return true;
        if (distanceSquared < 1e-4f) return true;

        float cos = Vector3.Dot(_mob.Forward, offset / MathF.Sqrt(distanceSquared));
        return cos >= _halfFovCos;
    }

    private float RangeFor(MobTarget target) => MobVision.NoticeRange(SightRange, _definition.DarkSightRange, target.HasLight, _mob.IsProvoked);

    private float SightRange => IsAlerted ? _definition.SightRange * AlertRangeMultiplier : _definition.SightRange;
}
