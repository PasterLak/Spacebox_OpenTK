using Spacebox.Game.Mobs.Feeding;
using Spacebox.Game.Mobs.Navigation;

namespace Spacebox.Game.Mobs;

public sealed class MobContext
{
    public MobContext(Mob mob, Random random)
    {
        Mob = mob;
        Random = random;
        FoodFinder = new FoodFinder(random);
    }

    public Mob Mob { get; }
    public Random Random { get; }
    public FoodFinder FoodFinder { get; }
    public MobDefinition Definition => Mob.Definition;
    public MobLocomotion Locomotion => Mob.Locomotion;
    public MobSenses Senses => Mob.Senses;
    public MobNavigationGrid Grid => Mob.Navigation;
    public bool IsTimid => Mob.IsTimid;

    public FoodTarget Food { get; set; }
    public bool FoodSearchFailed { get; set; }
    public float AttackCooldown { get; set; }

    public bool IsTargetInAttackRange(float multiplier = 1f)
    {
        return Senses.HasTarget && Senses.DistanceToTarget <= Definition.AttackRange * multiplier;
    }

    public void Tick(float deltaTime)
    {
        if (AttackCooldown > 0f) AttackCooldown -= deltaTime;
    }
}
