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

    public MobContext(Mob mob, Random random)
    {
        Mob = mob;
        Random = random;
        FoodFinder = new FoodFinder(random);
        Hunger = MobRandom.Range(random, MinStartHunger, 1f);
        _appetite = MobRandom.Range(random, 1f - AppetiteSpread, 1f + AppetiteSpread);
        _distanceTimer = MobRandom.Range(random, 0f, DistanceCheckInterval);
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

    public bool IsTargetInAttackRange(float multiplier = 1f)
    {
        return Senses.HasTarget && Senses.DistanceToTarget <= Definition.AttackRange * multiplier;
    }

    public void Tick(float deltaTime)
    {
        if (AttackCooldown > 0f) AttackCooldown -= deltaTime;

        _distanceTimer -= deltaTime;
        if (_distanceTimer <= 0f)
        {
            _distanceTimer = DistanceCheckInterval;
            NearestPlayerDistance = FindNearestPlayerDistance();
        }

        if (!IsHungry) Hunger += Definition.HungerRate(NearestPlayerDistance) * _appetite * deltaTime;
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
