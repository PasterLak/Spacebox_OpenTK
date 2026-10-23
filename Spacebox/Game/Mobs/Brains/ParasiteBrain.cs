using Engine.AI;
using Spacebox.Game.Mobs.States;

namespace Spacebox.Game.Mobs.Brains;

public sealed class ParasiteBrain : IMobBrain
{
    private const float WanderTime = 5f;
    private const float AttackRangeHysteresis = 1.25f;
    private const float FleeCalmTime = 2f;

    private readonly StateMachine<MobContext> _machine;

    public ParasiteBrain(MobContext context)
    {
        var forage = BuildForaging();
        var chase = new ChaseState();
        var attack = new AttackState();
        var flee = new FleeState();
        var watch = new WatchState();
        var home = new ReturnHomeState();

        _machine = new StateMachine<MobContext>(context);
        _machine.SetInitial(forage);
        _machine
            .AddTransition(forage, flee, c => c.Senses.HasTarget && c.IsTimid)
            .AddTransition(forage, chase, c => c.Senses.HasTarget && c.WillAttack)
            .AddTransition(forage, watch, c => c.Senses.HasTarget && c.IsHostile && c.IsWatching)
            .AddTransition(watch, chase, c => c.Senses.HasTarget && c.WillAttack)
            .AddTransition(watch, forage, c => !c.Senses.HasTarget || !c.IsHostile)
            .AddTransition(chase, attack, c => c.IsTargetInAttackRange())
            .AddTransition(chase, forage, c => !c.Senses.HasTarget || !c.IsHostile)
            .AddTransition(attack, forage, c => !c.Senses.HasTarget || !c.IsHostile)
            .AddTransition(attack, chase, c => !c.IsTargetInAttackRange(AttackRangeHysteresis))
            .AddTransition(flee, home, c => !c.Senses.HasTarget && flee.TimeInState > FleeCalmTime && c.IsFarFromHome)
            .AddTransition(flee, forage, c => !c.Senses.HasTarget && flee.TimeInState > FleeCalmTime)
            .AddTransition(home, flee, c => c.Senses.HasTarget && c.IsTimid)
            .AddTransition(home, chase, c => c.Senses.HasTarget && c.WillAttack)
            .AddTransition(home, forage, _ => home.IsDone);
    }

    public string StateName => Describe(_machine);

    public void Update(float deltaTime)
    {
        _machine.Update(deltaTime);
    }

    private static StateMachine<MobContext> BuildForaging()
    {
        var rest = new RestState();
        var seek = new SeekFoodState();
        var approach = new ApproachFoodState();
        var eat = new EatBlockState();
        var wander = new WanderState();

        var forage = new StateMachine<MobContext>();
        forage.SetInitial(rest);
        forage
            .AddTransition(rest, seek, c => c.WantsToEat)
            .AddTransition(seek, approach, c => c.Food.IsValid)
            .AddTransition(seek, wander, c => c.FoodSearchFailed)
            .AddTransition(approach, eat, _ => approach.IsNearFood)
            .AddTransition(approach, seek, _ => approach.HasFailed)
            .AddTransition(eat, rest, c => !c.Food.IsValid)
            .AddTransition(wander, seek, c => c.WantsToEat && wander.TimeInState > WanderTime)
            .AddTransition(wander, rest, c => !c.WantsToEat);

        return forage;
    }

    private static string Describe(StateMachine<MobContext> machine)
    {
        var current = machine.Current;
        if (current is StateMachine<MobContext> nested && nested.Current != null) return nested.Current.Name;
        return current?.Name ?? "";
    }
}
