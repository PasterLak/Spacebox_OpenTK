using OpenTK.Mathematics;
using Spacebox.Game.Mobs.Feeding;
using Spacebox.Game.Mobs.Navigation;

namespace Spacebox.Game.Mobs;

public sealed class MobContext
{
    private const float DistanceCheckInterval = 1f;
    private const float MinStartHunger = 0.5f;
    private const float AppetiteSpread = 0.3f;

    private readonly float _appetite;
    private float _distanceTimer;
    private bool _hadTarget;
    private int _rolledPeer = -1;

    public MobContext(Mob mob, Random random)
    {
        Mob = mob;
        Random = random;
        FoodFinder = new FoodFinder(random);
        Hunger = MobRandom.Range(random, MinStartHunger, 1f);
        _appetite = MobRandom.Range(random, 1f - AppetiteSpread, 1f + AppetiteSpread);
        _distanceTimer = MobRandom.Range(random, 0f, DistanceCheckInterval);
        Home = mob.Position;
    }

    public Mob Mob { get; }
    public Random Random { get; }
    public FoodFinder FoodFinder { get; }
    public MobDefinition Definition => Mob.Definition;
    public MobLocomotion Locomotion => Mob.Locomotion;
    public MobSenses Senses => Mob.Senses;
    public MobNavigationGrid Grid => Mob.Navigation;
    public bool IsTimid => Mob.IsTimid;
    public bool IsHostile => MobRules.IsHostile(MobRules.FriendlyMobs, Mob.IsProvoked);

    public FoodTarget Food { get; set; }
    public bool FoodSearchFailed { get; set; }
    public float AttackCooldown { get; set; }
    public float Hunger { get; set; }
    public bool IsHungry => Hunger >= 1f;
    public bool WantsToEat => IsHungry && NearestPlayerDistance < Definition.MealFarDistance;
    public float NearestPlayerDistance { get; private set; } = float.MaxValue;
    public Vector3 Home { get; private set; }
    public bool IsWatching { get; private set; }
    public bool WillAttack => IsHostile && (!IsWatching || Mob.IsProvoked);
    public bool IsFarFromHome => Definition.ReturnDistance > 0f && Vector3.DistanceSquared(Mob.Position, Home) > Definition.ReturnDistance * Definition.ReturnDistance;

    public bool IsTargetInAttackRange(float multiplier = 1f)
    {
        return Senses.HasTarget && Senses.DistanceToTarget <= Definition.AttackRange * multiplier;
    }

    public void Tick(float deltaTime)
    {
        if (AttackCooldown > 0f) AttackCooldown -= deltaTime;
        UpdateWatching();

        _distanceTimer -= deltaTime;
        if (_distanceTimer <= 0f)
        {
            _distanceTimer = DistanceCheckInterval;
            NearestPlayerDistance = FindNearestPlayerDistance();
            if (Navigation.SurfaceFinder.IsOnSurface(Grid, Mob.Position)) Home = Mob.Position;
        }

        if (!IsHungry) Hunger += Definition.HungerRate(NearestPlayerDistance) * _appetite * deltaTime;
    }

    private void UpdateWatching()
    {
        bool hasTarget = Senses.HasTarget;
        if (hasTarget && (!_hadTarget || Senses.TargetPeer != _rolledPeer))
        {
            _rolledPeer = Senses.TargetPeer;
            IsWatching = MobVision.RollWatch(Definition.WatchChance, Random);
        }
        if (Mob.IsProvoked) IsWatching = false;
        _hadTarget = hasTarget;
    }

    private float FindNearestPlayerDistance()
    {
        var targets = MobTargets.All;
        float best = float.MaxValue;
        for (int i = 0; i < targets.Count; i++)
            best = MathF.Min(best, Vector3.DistanceSquared(targets[i].Position, Mob.Position));
        return best == float.MaxValue ? best : MathF.Sqrt(best);
    }
}
