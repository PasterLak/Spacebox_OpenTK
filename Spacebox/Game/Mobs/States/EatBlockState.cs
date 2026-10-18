using Engine.AI;

namespace Spacebox.Game.Mobs.States;

public sealed class EatBlockState : State<MobContext>
{
    protected override void OnEnter()
    {
        Context.Locomotion.Stop();
        Context.Mob.Action = MobAction.Eating;
        Context.Mob.BeginEating(Context.Food);
    }

    protected override void OnUpdate(float deltaTime)
    {
        var food = Context.Food;
        if (!food.IsValid) return;

        Context.Mob.LookAt(food.BlockWorld);
        if (TimeInState < Context.Definition.EatTime) return;

        if (Context.Mob.EatBlock(food)) Context.Hunger = 0f;
        Context.Food = default;
    }

    protected override void OnExit()
    {
        Context.Mob.Action = MobAction.Idle;
    }
}
